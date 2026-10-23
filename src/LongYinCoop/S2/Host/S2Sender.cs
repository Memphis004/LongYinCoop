using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        // X6 วัด p99 ของ (sampleMs + serializeMs) ต่อเฟรม — เก็บผลรวมแยก ไม่ใช่ผลรวมของ p99
        private readonly double[] _combined = new double[Capacity];
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
                _combined[_next] = sampleMs + serializeMs;
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

        public double CombinedPercentile(double p) { return Percentile(_combined, p); }

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
                combinedMsP99 = Math.Round(CombinedPercentile(0.99), 4),
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

    /// <summary>
    /// wire encode/decode ของ S2Frame (LE; ใช้ codec เดียวกับ Protocol)
    ///
    /// ProtoVer 2 (S2a-3): เพิ่ม **sessionId / hostApplyUs / replicaHash / resync flag / resyncFromSeq**
    /// ไว้ต้น envelope เพื่อให้วัด latency + divergence ได้ (§3.5) — ฟิลด์เดิมไม่เปลี่ยนลำดับ/ความหมาย
    /// (ยูนิตยังเป็นชุดเดิม + `unitAlive` ที่ต้องคงไว้ตามสเปก hash)
    /// </summary>
    public static class S2FrameCodec
    {
        public const int MaxUnits = 64;
        public const int ReplicaHashBytes = 32;

        public static byte[] Encode(in S2Frame frame)
        {
            string hash = frame.ReplicaHash;
            if (string.IsNullOrEmpty(hash)) hash = ComputeReplicaHash(frame);
            return Encode(frame, hash);
        }

        public static byte[] Encode(in S2Frame frame, string replicaHash)
        {
            byte[] hashBytes = S2Auth.BytesFromHex(replicaHash);
            if (hashBytes == null || hashBytes.Length != ReplicaHashBytes)
                throw new ArgumentException("replicaHash must be 64 hex chars");

            CompactWriter w = new CompactWriter(256 + frame.Units.Length * 64);
            w.WriteByte(S2Protocol.ProtoVer);
            w.WriteVarUInt(frame.SessionId);
            w.WriteVarULong(frame.HostApplyUs);
            w.WriteRaw(hashBytes);
            w.WriteByte(frame.Resync ? (byte)1 : (byte)0);
            w.WriteVarUInt(frame.ResyncFromSeq);
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

        /// <summary>hash ตามสเปก `s2|2` ของ message นี้ (host คำนวณก่อนส่ง, guest คำนวณใหม่หลัง decode)</summary>
        public static string ComputeReplicaHash(in S2Frame frame)
        {
            ReplicaHeader header = new ReplicaHeader
            {
                BattleKey = frame.Header.BattleKey,
                TurnIndex = frame.Header.TurnIndex,
                ActiveUnitId = frame.Header.ActiveUnitId,
                Paused = frame.Header.Paused,
            };

            ReplicaUnit[] units = new ReplicaUnit[frame.Units.Length];
            for (int i = 0; i < units.Length; i++)
            {
                S2Unit u = frame.Units[i];
                units[i] = new ReplicaUnit
                {
                    StableId = u.StableId,
                    Team = u.Team,
                    Row = u.Row,
                    Column = u.Column,
                    Hp = u.Hp,
                    IsAlive = u.IsAlive,
                    UnitAlive = u.UnitAlive,
                    Moved = u.Moved,
                    Attacked = u.Attacked,
                };
            }
            return ReplicaHash.Compute(header, units);
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
                decoded.SessionId = r.ReadVarUInt();
                decoded.HostApplyUs = r.ReadVarULong();
                decoded.ReplicaHash = S2Auth.Hex(r.ReadRaw(ReplicaHashBytes).ToArray());
                decoded.Resync = r.ReadByte() != 0;
                decoded.ResyncFromSeq = r.ReadVarUInt();
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
    /// ตัวเลือกของ <see cref="S2SerializingSender"/> (S2a-3)
    ///   `SendHz = 0` / `Dedup = false` = พฤติกรรม S2a-2 เดิม (ส่งทุกเฟรม) — ใช้ในเทสต์ regression
    /// </summary>
    public sealed class S2SenderOptions
    {
        public int QueueCapacity = 2;
        /// <summary>เพดานอัตราส่ง snapshot (Hz) · 0 = ไม่จำกัด</summary>
        public int SendHz = 30;
        /// <summary>ส่งซ้ำแม้ state ไม่เปลี่ยนเมื่อครบกำหนด (ms) · 0 = ปิด heartbeat</summary>
        public int HeartbeatMs = 1000;
        /// <summary>ข้าม snapshot ที่ replicaHash ไม่เปลี่ยน</summary>
        public bool Dedup = true;
        public uint SessionId;
        /// <summary>นาฬิกา monotonic (ms) — เทสต์ฉีดเวลาปลอมได้</summary>
        public Func<long> NowMs;

        public int MinIntervalMs { get { return SendHz <= 0 ? 0 : 1000 / SendHz; } }

        public Func<long> Clock
        {
            get { return NowMs ?? DefaultNow; }
        }

        public static long DefaultNow()
        {
            return System.Diagnostics.Stopwatch.GetTimestamp() * 1000L
                   / System.Diagnostics.Stopwatch.Frequency;
        }
    }

    /// <summary>
    /// serialize บน thread อื่น (ตาม §13.2 ข้อ 4): main thread แค่ TryPost (ไม่บล็อก)
    ///
    /// S2a-3 เปลี่ยน 3 อย่าง (แผน §1 A1/A2/A7):
    ///   1) **seq กำหนดตอน send** (ไม่ใช่ `frame.FrameId` ของ sampler) ⇒ คิวเต็ม/pacing ไม่ทำให้ seq ขาด
    ///   2) **dedup + pacing + heartbeat** แทน "ส่งทุกเฟรม" (§3.2) — heartbeat 1 Hz กัน viewer ค้างค่าเก่า
    ///   3) envelope v2 + `replicaHash` ต่อ message (Div-A ที่ guest คำนวณใหม่ได้)
    /// </summary>
    public sealed class S2SerializingSender : IDisposable
    {
        private readonly BlockingCollection<S2Frame> _queue;
        private readonly ITransport _transport;
        private readonly S2Counters _counters;
        private readonly S2Metrics _metrics;
        private readonly S2NdjsonWriter _writer;
        private readonly S2SenderOptions _options;
        private readonly Func<long> _clock;
        private readonly Thread _worker;
        private ushort _msgId;
        private uint _seq;
        private volatile bool _stop;

        private string _lastSentHash;
        private long _lastSendMs = long.MinValue / 2;
        private int _resyncFlag;
        private int _resyncFromSeq;

        public S2SerializingSender(ITransport transport, S2Counters counters, S2Metrics metrics,
                                   S2NdjsonWriter writer = null, S2SenderOptions options = null)
        {
            _transport = transport;
            _counters = counters ?? new S2Counters();
            _metrics = metrics ?? new S2Metrics();
            _writer = writer;
            _options = options ?? new S2SenderOptions();
            _clock = _options.Clock;
            int capacity = _options.QueueCapacity < 1 ? 1 : _options.QueueCapacity;
            _queue = new BlockingCollection<S2Frame>(capacity);

            _worker = new Thread(Loop) { IsBackground = true, Name = "S2Serialize" };
            _worker.Start();
        }

        public long Posted;
        public long Sent;
        public long Dropped;
        public long Errors;
        public long Deduped;
        public long Paced;
        public long Heartbeats;
        /// <summary>seq ล่าสุดที่ส่งสำเร็จ (0 = ยังไม่ส่ง)</summary>
        public uint LastSeq { get { return _seq; } }

        public S2Metrics Metrics { get { return _metrics; } }

        public S2SenderOptions Options { get { return _options; } }

        /// <summary>ให้ snapshot ใบถัดไปติดธง resync=true (เรียกได้จาก main thread)</summary>
        public void FlagResync(uint fromSeq)
        {
            System.Threading.Interlocked.Exchange(ref _resyncFromSeq, unchecked((int)fromSeq));
            System.Threading.Interlocked.Exchange(ref _resyncFlag, 1);
        }

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
                        SendOne(frame);
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

        private void SendOne(S2Frame frame)
        {
            long now = _clock();

            bool resync = System.Threading.Interlocked.Exchange(ref _resyncFlag, 0) == 1;
            uint resyncFrom = unchecked((uint)System.Threading.Interlocked.Exchange(ref _resyncFromSeq, 0));

            // 1) hash ของ message นี้ (ใช้ตัดสิน dedup + ส่งให้ guest ตรวจ Div-A)
            string hash = S2FrameCodec.ComputeReplicaHash(frame);
            frame.ReplicaHash = hash;
            frame.SessionId = _options.SessionId;

            bool changed = _lastSentHash == null || !string.Equals(hash, _lastSentHash, StringComparison.Ordinal);
            bool heartbeat = false;

            if (!resync)
            {
                if (_options.Dedup && !changed)
                {
                    // 2) ไม่เปลี่ยน: ส่งเฉพาะเมื่อครบ heartbeat
                    if (_options.HeartbeatMs <= 0 || now - _lastSendMs < _options.HeartbeatMs)
                    {
                        Deduped++;
                        _counters.SnapshotsDeduped++;
                        return;
                    }
                    heartbeat = true;
                }
                else if (_options.Dedup && now - _lastSendMs < _options.MinIntervalMs)
                {
                    // 3) เปลี่ยนแต่ยังไม่ถึงรอบ: **ไม่** อัปเดต _lastSentHash ⇒ รอบถัดไปส่งเอง
                    //    (การเปลี่ยนที่สั้นกว่า sendHz จะถูกรวมไว้ ไม่ทำให้ seq ขาด)
                    Paced++;
                    _counters.SnapshotsPaced++;
                    return;
                }
            }

            // 0) BUGFIX 2026-10-24: the envelope carries the resync flag/fromSeq, so they must be
            //    set BEFORE Encode. Before this fix the payload was encoded with the previous
            //    frame's values (always 0/false) while the ndjson log showed true — the guest
            //    never saw `resync=true`, so a gap window could never close (B3b: gaps=1,
            //    resync 3/3 served, closed=0, giveUp=1).
            frame.Resync = resync;
            frame.ResyncFromSeq = resync ? resyncFrom : 0;

            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            byte[] payload = S2FrameCodec.Encode(frame, hash);
            frame.PayloadBytes = payload.Length;
            double encodeMs = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0
                              / System.Diagnostics.Stopwatch.Frequency;

            frame.Heartbeat = heartbeat;

            // 4) seq ตอน send เท่านั้น (A1) — ถ้า send ล้มบางส่วน seq ยังเดิน ⇒ guest เห็น gap จริง = ถูกต้อง
            uint seq = unchecked(_seq + 1);
            _seq = seq;

            ushort id = _msgId++;
            List<byte[]> frames = ChunkCodec.Encode(id, MessageType.Snapshot, S2Protocol.ProtoVer, seq, payload);

            bool allSent = true;
            int wireBytes = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                wireBytes += frames[i].Length;
                if (!_transport.Send(frames[i])) allSent = false;
            }

            frame.Seq = seq;
            frame.WireBytes = wireBytes;
            frame.SerializeMs = encodeMs;
            _metrics.AddFrame(frame.SampleMs, encodeMs, payload.Length);

            if (allSent)
            {
                Sent++;
                _counters.FramesSent++;
                _counters.BytesSent += payload.Length;
                _counters.WireBytesSent += wireBytes;
                if (heartbeat)
                {
                    Heartbeats++;
                    _counters.HeartbeatsSent++;
                }
                _lastSentHash = hash;
                _lastSendMs = now;
            }

            if (_writer != null) _writer.Write(frame);
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
