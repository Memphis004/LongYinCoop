using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using LongYinCoop.S2.Protocol;
using LongYinCoop.S2.Transport;

namespace LongYinCoop.S2.Host
{
    /// <summary>สถิติต่อเฟรม (ring buffer, thread-safe แบบ lock) สำหรับ exit 2</summary>
    public sealed class S2Metrics
    {
        private const int Capacity = 2048;

        private readonly object _lock = new object();
        private readonly double[] _sample = new double[Capacity];
        private readonly double[] _serialize = new double[Capacity];
        private readonly int[] _bytes = new int[Capacity];
        private int _count;
        private int _next;
        private long _total;

        public void AddFrame(double sampleMs, double serializeMs, int payloadBytes)
        {
            lock (_lock)
            {
                _sample[_next] = sampleMs;
                _serialize[_next] = serializeMs;
                _bytes[_next] = payloadBytes;
                _next = (_next + 1) % Capacity;
                if (_count < Capacity) _count++;
                _total++;
            }
        }

        public long TotalFrames { get { lock (_lock) { return _total; } } }

        public int WindowSamples { get { lock (_lock) { return _count; } } }

        public double SamplePercentile(double p) { return Percentile(_sample, p); }

        public double SerializePercentile(double p) { return Percentile(_serialize, p); }

        public double BytesPercentile(double p)
        {
            double[] copy;
            lock (_lock) { copy = Copy(_bytes, _count); }
            if (copy.Length == 0) return 0;
            Array.Sort(copy);
            int idx = IndexOf(p, copy.Length);
            return copy[idx];
        }

        public double SampleMean()
        {
            lock (_lock)
            {
                if (_count == 0) return 0;
                double sum = 0;
                for (int i = 0; i < _count; i++) sum += _sample[i];
                return Math.Round(sum / _count, 4);
            }
        }

        public object Snapshot()
        {
            return new
            {
                frames = TotalFrames,
                window = WindowSamples,
                sampleMsMean = SampleMean(),
                sampleMsP50 = Math.Round(SamplePercentile(0.50), 4),
                sampleMsP99 = Math.Round(SamplePercentile(0.99), 4),
                serializeMsP50 = Math.Round(SerializePercentile(0.50), 4),
                serializeMsP99 = Math.Round(SerializePercentile(0.99), 4),
                payloadBytesP99 = Math.Round(BytesPercentile(0.99), 1),
            };
        }

        private double Percentile(double[] source, double p)
        {
            double[] copy;
            lock (_lock) { copy = Copy(source, _count); }
            if (copy.Length == 0) return 0;
            Array.Sort(copy);
            return Math.Round(copy[IndexOf(p, copy.Length)], 4);
        }

        private static double[] Copy(double[] source, int count)
        {
            double[] copy = new double[count];
            Array.Copy(source, copy, count);
            return copy;
        }

        private static double[] Copy(int[] source, int count)
        {
            double[] copy = new double[count];
            for (int i = 0; i < count; i++) copy[i] = source[i];
            return copy;
        }

        private static int IndexOf(double p, int length)
        {
            int idx = (int)Math.Ceiling(p * length) - 1;
            if (idx < 0) idx = 0;
            if (idx >= length) idx = length - 1;
            return idx;
        }
    }

    /// <summary>wire encode/decode ของ S2Frame (LE; ใช้ codec เดียวกับ Protocol)</summary>
    public static class S2FrameCodec
    {
        public const int MaxUnits = 64;

        public static byte[] Encode(in S2Frame frame)
        {
            CompactWriter w = new CompactWriter(256 + frame.Units.Length * 64);
            w.WriteByte(S2Protocol.ProtoVer);
            w.WriteString(frame.Header.BattleKey);
            w.WriteVarUInt((uint)frame.Header.TurnIndex);
            w.WriteString(frame.Header.ActiveUnitId);
            w.WriteBool(frame.Header.Paused);
            w.WriteVarUInt((uint)frame.Header.NowActiveState);
            w.WriteFloat(frame.Header.BattleTime);
            w.WriteVarUInt((uint)frame.Units.Length);
            for (int i = 0; i < frame.Units.Length; i++)
            {
                S2Unit u = frame.Units[i];
                w.WriteString(u.StableId);
                w.WriteVarUInt((uint)u.Team);
                w.WriteVarUInt((uint)u.HeroId);
                w.WriteVarUInt((uint)u.Row);
                w.WriteVarUInt((uint)u.Column);
                w.WriteFloat(u.Hp);
                w.WriteFloat(u.MaxHp);
                w.WriteBool(u.IsAlive);
                w.WriteBool(u.UnitAlive);
                w.WriteBool(u.Moved);
                w.WriteBool(u.Attacked);
                w.WriteBool(u.PlayerControl);
            }
            return w.ToArray();
        }

        public static bool TryDecode(byte[] payload, out S2Frame frame, out string error)
        {
            frame = null;
            error = null;
            if (payload == null || payload.Length == 0)
            {
                error = "empty payload";
                return false;
            }

            try
            {
                CompactReader r = new CompactReader(payload);
                byte ver = r.ReadByte();
                if (ver != S2Protocol.ProtoVer)
                {
                    error = "proto version " + ver;
                    return false;
                }

                S2Frame decoded = new S2Frame();
                decoded.Header.BattleKey = r.ReadString();
                decoded.Header.TurnIndex = (int)r.ReadVarUInt();
                decoded.Header.ActiveUnitId = r.ReadString();
                decoded.Header.Paused = r.ReadBool();
                decoded.Header.NowActiveState = (int)r.ReadVarUInt();
                decoded.Header.BattleTime = r.ReadFloat();
                uint count = r.ReadVarUInt();
                if (count > MaxUnits)
                {
                    error = "unit count " + count + " over cap " + MaxUnits;
                    return false;
                }

                S2Unit[] units = new S2Unit[count];
                for (int i = 0; i < count; i++)
                {
                    S2Unit u = new S2Unit();
                    u.StableId = r.ReadString();
                    u.Team = (int)r.ReadVarUInt();
                    u.HeroId = (int)r.ReadVarUInt();
                    u.Row = (int)r.ReadVarUInt();
                    u.Column = (int)r.ReadVarUInt();
                    u.Hp = r.ReadFloat();
                    u.MaxHp = r.ReadFloat();
                    u.IsAlive = r.ReadBool();
                    u.UnitAlive = r.ReadBool();
                    u.Moved = r.ReadBool();
                    u.Attacked = r.ReadBool();
                    u.PlayerControl = r.ReadBool();
                    units[i] = u;
                }

                decoded.Units = units;
                decoded.Header.UnitCount = (int)count;
                if (r.Remaining != 0)
                {
                    error = "trailing bytes: " + r.Remaining;
                    return false;
                }
                decoded.Audit = S2AuditUtil.Compute(decoded.Header, units);
                frame = decoded;
                return true;
            }
            catch (Exception e)
            {
                error = e.GetType().Name + ": " + e.Message;
                frame = null;
                return false;
            }
        }
    }

    /// <summary>
    /// serialize บน thread อื่น (ตาม §13.2 ข้อ 4): main thread แค่ TryPost (ไม่บล็อก)
    /// single-slot-ish queue; คิวเต็ม = ทิ้ง + นับ (ไม่โตเรื่อย ๆ)
    /// </summary>
    public sealed class S2SerializingSender : IDisposable
    {
        private readonly BlockingCollection<S2Frame> _queue;
        private readonly ITransport _transport;
        private readonly S2Counters _counters;
        private readonly S2Metrics _metrics;
        private readonly S2NdjsonWriter _writer;
        private readonly Thread _worker;
        private ushort _msgId;
        private volatile bool _stop;

        public S2SerializingSender(ITransport transport, S2Counters counters, S2Metrics metrics,
                                   S2NdjsonWriter writer = null, int queueCapacity = 2)
        {
            _transport = transport;
            _counters = counters ?? new S2Counters();
            _metrics = metrics ?? new S2Metrics();
            _writer = writer;
            _queue = new BlockingCollection<S2Frame>(queueCapacity < 1 ? 1 : queueCapacity);

            _worker = new Thread(Loop) { IsBackground = true, Name = "S2Serialize" };
            _worker.Start();
        }

        public long Posted;
        public long Sent;
        public long Dropped;
        public long Errors;

        public S2Metrics Metrics { get { return _metrics; } }

        /// <summary>เรียกจาก main thread เท่านั้น — ห้ามบล็อก</summary>
        public bool TryPost(S2Frame frame)
        {
            if (frame == null || _stop) return false;
            Posted++;
            if (!_queue.TryAdd(frame))
            {
                Dropped++;
                _counters.FramesQueuedDropped++;
                return false;
            }
            return true;
        }

        public void Stop(int joinMs = 500)
        {
            if (_stop) return;
            _stop = true;
            _queue.CompleteAdding();
            try { _worker.Join(joinMs); } catch (ThreadStateException) { }
        }

        public void Dispose()
        {
            Stop();
            try { _queue.Dispose(); } catch (ObjectDisposedException) { }
        }

        private void Loop()
        {
            try
            {
                foreach (S2Frame frame in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
                        byte[] payload = S2FrameCodec.Encode(frame);
                        frame.PayloadBytes = payload.Length;

                        ushort id = _msgId++;
                        System.Collections.Generic.List<byte[]> frames =
                            ChunkCodec.Encode(id, MessageType.Snapshot, S2Protocol.ProtoVer,
                                              (uint)frame.FrameId, payload);

                        bool allSent = true;
                        for (int i = 0; i < frames.Count; i++)
                        {
                            if (!_transport.Send(frames[i])) allSent = false;
                        }

                        double serializeMs = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0
                                             / System.Diagnostics.Stopwatch.Frequency;
                        frame.SerializeMs = serializeMs;
                        _metrics.AddFrame(frame.SampleMs, serializeMs, payload.Length);

                        if (allSent)
                        {
                            Sent++;
                            _counters.FramesSent++;
                            _counters.BytesSent += payload.Length;
                        }

                        if (_writer != null) _writer.Write(frame);
                    }
                    catch (Exception)
                    {
                        Errors++;
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }
    }

    /// <summary>ตัวเขียน evidence NDJSON (`s2-snapshots.ndjson`) — เรียกจาก sender thread ได้</summary>
    public sealed class S2NdjsonWriter : IDisposable
    {
        private readonly object _lock = new object();
        private readonly StreamWriter _writer;
        private readonly int _flushEvery;
        private bool _closed;

        public S2NdjsonWriter(string path, int flushEvery = 20, bool append = false)
        {
            Path = path;
            _flushEvery = flushEvery < 1 ? 1 : flushEvery;
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            _writer = new StreamWriter(path, append: append, new UTF8Encoding(false));
            _writer.AutoFlush = false;
        }

        public string Path { get; private set; }
        public long Lines { get; private set; }

        public void Write(in S2Frame frame)
        {
            string line = S2Ndjson.ToLine(frame);
            lock (_lock)
            {
                if (_closed) return;
                _writer.WriteLine(line);
                Lines++;
                if (Lines % _flushEvery == 0) _writer.Flush();
            }
        }

        /// <summary>บรรทัดอื่นที่ต้องอยู่ในหลักฐานชุดเดียวกัน (เช่น marker เปลี่ยนเฟส A/B ของ exit 3)</summary>
        public void WriteRawLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            lock (_lock)
            {
                if (_closed) return;
                _writer.WriteLine(line);
                Lines++;
                _writer.Flush();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_closed) return;
                _closed = true;
                try { _writer.Flush(); } catch (ObjectDisposedException) { }
                _writer.Dispose();
            }
        }
    }
}
