using System;
using System.Globalization;
using System.Threading;
using LongYinCoop.S2.Host;
using LongYinCoop.S2.Protocol;
using LongYinCoop.S2.Transport;

namespace LongYinCoop.S2.Guest
{
    /// <summary>สถิติของ overlay client (อ่านจาก main thread ได้ — คัดลอกภายใต้ lock)</summary>
    public sealed class S2OverlayClientStats
    {
        public bool Connected;
        public bool Authenticated;
        public long Sessions;
        public uint SessionId;
        public uint LastSeq;
        public uint ExpectedSeq;
        public long Snapshots;
        public long Gaps;
        public long Duplicates;
        public long ResyncsSent;
        public long ResyncAcks;
        public long GapClosed;
        public long DivAChecked;
        public long DivAMismatch;
        public long Pings;
        public long Pongs;
        public bool HasOffset;
        public long OffsetUsAtMinRtt;
        public long LastRttUs;
        public DenyCode LastDeny = DenyCode.Unknown;
        public long Denies;
        public string LastError;
        public long Reconnects;
    }

    /// <summary>snapshot ที่รับไว้ พร้อมเวลารับ (สำหรับ render smoothing)</summary>
    public struct GuestTimedSnapshot
    {
        public uint Seq;
        public long ReceivedAtMs;
        public GuestSnapshotDisplay Display;
    }

    /// <summary>
    /// W2 (แผน S2a-4 §4): in-process guest client ของ overlay — แพตเทิร์นเดียวกับ `S2TcpSelfTest`
    /// แต่เป็น long-lived: handshake → snapshot → gap→RESYNC → PING offset → reconnect
    ///
    /// ทำหน้าที่เหมือน `tools/viewer-client.py` ฝั่ง C# (seq gap/dup, Div-A, NTP offset แบบ min-RTT)
    /// · **spectator อ่านอย่างเดียว**: ส่งได้เฉพาะ AUTH/PING/RESYNC/BYE ตาม allowlist §13.2
    /// · เธรดของตัวเองทั้งหมด — main thread แค่อ่าน `Latest`/`CopyRing`/`SnapshotStats`
    /// · ไม่มี IL2CPP/Unity/MelonLoader ในไฟล์นี้ (รันใน unit test ได้)
    /// </summary>
    public sealed class S2OverlayClient : IDisposable
    {
        public const int DefaultPingEveryMs = 2000;
        public const int DefaultReconnectMs = 1500;
        public const int RingSize = 8;

        private readonly string _ip;
        private readonly int _port;
        private readonly byte[] _secret;
        private readonly string _fieldHashHex;
        private readonly int _pingEveryMs;
        private readonly int _reconnectMs;
        private readonly Thread _thread;
        private readonly object _lock = new object();

        private readonly GuestTimedSnapshot[] _ring = new GuestTimedSnapshot[RingSize];
        private int _ringCount;
        private int _ringNext;

        private S2OverlayClientStats _stats = new S2OverlayClientStats();
        private volatile bool _running;
        private bool _disposed;

        public S2OverlayClient(string ip, int port, byte[] secret, string fieldHashHex,
                               int pingEveryMs = DefaultPingEveryMs, int reconnectMs = DefaultReconnectMs)
        {
            if (secret == null || secret.Length == 0) throw new ArgumentException("secret required");
            _ip = string.IsNullOrEmpty(ip) ? "127.0.0.1" : ip;
            _port = port;
            _secret = secret;
            _fieldHashHex = fieldHashHex ?? string.Empty;
            _pingEveryMs = pingEveryMs < 100 ? 100 : pingEveryMs;
            _reconnectMs = reconnectMs < 200 ? 200 : reconnectMs;
            _thread = new Thread(Loop) { IsBackground = true, Name = "S2OverlayClient" };
        }

        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException("S2OverlayClient");
            if (_running) return;
            _running = true;
            _thread.Start();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _running = false;
            try { if (_thread.IsAlive) _thread.Join(800); } catch (ThreadStateException) { }
        }

        public S2OverlayClientStats SnapshotStats()
        {
            lock (_lock) { return Copy(_stats); }
        }

        /// <summary>ภาพล่าสุด (null = ยังไม่มี) — main thread อ่านได้</summary>
        public GuestSnapshotDisplay Latest()
        {
            lock (_lock)
            {
                if (_ringCount == 0) return null;
                int last = (_ringNext - 1 + RingSize) % RingSize;
                return _ring[last].Display;
            }
        }

        /// <summary>สำเนา ring เรียงเวลารับจากเก่า→ใหม่ (ให้ render model เลือกเฟรมตามอายุ)</summary>
        public GuestTimedSnapshot[] CopyRing()
        {
            lock (_lock)
            {
                GuestTimedSnapshot[] copy = new GuestTimedSnapshot[_ringCount];
                for (int i = 0; i < _ringCount; i++)
                {
                    copy[i] = _ring[(_ringNext - _ringCount + i + RingSize * 2) % RingSize];
                }
                return copy;
            }
        }

        public long LatestReceivedAtMs()
        {
            lock (_lock)
            {
                if (_ringCount == 0) return 0;
                int last = (_ringNext - 1 + RingSize) % RingSize;
                return _ring[last].ReceivedAtMs;
            }
        }

        // ------------------------------------------------------------------ worker thread

        private void Loop()
        {
            while (_running)
            {
                try
                {
                    Session();
                }
                catch (Exception e)
                {
                    SetError(e.GetType().Name + ": " + e.Message);
                }
                if (!_running) break;
                // host อาจยังไม่เปิด listener / slot ถูก viewer ครอง — ลองใหม่ตามรอบ
                MutateStats(delegate (S2OverlayClientStats s) { s.Reconnects++; });
                SleepInterruptible(_reconnectMs);
            }
        }

        /// <summary>1 connection: connect → handshake → snapshot จนกว่าจะตาย/หยุด</summary>
        private void Session()
        {
            string connectError;
            TcpTransport client = TcpTransport.Connect(_ip, _port, out connectError, 3000);
            if (client == null)
            {
                SetError(connectError);
                return;
            }

            try
            {
                MutateStats(delegate (S2OverlayClientStats s) { s.Connected = true; });
                if (!Handshake(client)) return;

                long sessionStartMs = NowMs();
                long lastPingMs = sessionStartMs - _pingEveryMs; // ยิง PING ทันทีที่เข้า session
                SessionState state = new SessionState();
                ChunkReassembler reassembler = new ChunkReassembler();

                while (_running && client.Connected)
                {
                    long now = NowMs();
                    if (now - lastPingMs >= _pingEveryMs)
                    {
                        lastPingMs = now;
                        if (client.Send(S2ControlHandler.BuildPing(NowUs())))
                        {
                            MutateStats(delegate (S2OverlayClientStats s) { s.Pings++; });
                        }
                    }

                    int guard = 0;
                    while (guard++ < 64 && client.TryReceive(out byte[] raw))
                    {
                        FrameHeader header;
                        if (!FrameCodec.TryReadHeader(raw, out header)) continue;
                        byte[] body = Slice(raw, header.PayloadOffset, header.PayloadLength);
                        if (header.Type == MessageType.Chunk)
                        {
                            AssembledMessage done;
                            if (reassembler.Accept(header, body, now, out done)) HandleMessage(client, done, state);
                            continue;
                        }
                        HandleMessage(client, new AssembledMessage(header.Type, header.Ver, header.Seq, body), state);
                    }

                    reassembler.PurgeExpired(S2Protocol.ReassemblyTimeoutMs + 1);
                    Thread.Sleep(2);
                }
            }
            finally
            {
                try { client.Close(); } catch { }
                MutateStats(delegate (S2OverlayClientStats s) { s.Connected = false; s.Authenticated = false; });
            }
        }

        private bool Handshake(TcpTransport client)
        {
            byte[] hello = ReceiveWithin(client, 3000);
            if (hello == null)
            {
                SetError("no hello");
                return false;
            }

            AssembledMessage message = AssembleSingle(hello);
            if (message.Type == MessageType.Deny)
            {
                // slot ถูก guest อื่นครอง (Deny{busy}) — จดรหัสแล้วรอรอบ reconnect
                return NoteDeny(message);
            }
            if (message.Type != MessageType.Hello)
            {
                SetError("expected Hello, got " + message.Type);
                return false;
            }

            S2HandshakeClient handshake = new S2HandshakeClient(_secret, _fieldHashHex);
            byte[] auth;
            string error;
            if (!handshake.OnHello(message, out auth, out error))
            {
                SetError("hello: " + error);
                return false;
            }
            client.Send(auth);

            long deadline = NowMs() + 3000;
            while (NowMs() < deadline && _running)
            {
                byte[] raw = ReceiveWithin(client, 200);
                if (raw == null) continue;
                message = AssembleSingle(raw);
                if (message.Type == MessageType.Deny)
                {
                    return NoteDeny(message);
                }
                if (message.Type != MessageType.AuthResult) continue;
                if (!handshake.OnReply(message, out error))
                {
                    SetError(error);
                    return false;
                }
                uint sessionId = handshake.SessionId;
                MutateStats(delegate (S2OverlayClientStats s)
                {
                    s.Authenticated = true;
                    s.Sessions++;
                    s.SessionId = sessionId;
                    s.LastSeq = 0;
                    s.ExpectedSeq = 1;
                    s.LastError = null;
                });
                return true;
            }
            SetError("auth timeout");
            return false;
        }

        /// <summary>จด Deny ลงสถิติ (เช่น Busy = slot ถูกครอง) แล้วให้ session จบไปรอรอบ reconnect</summary>
        private bool NoteDeny(AssembledMessage message)
        {
            DenyCode code;
            string reason;
            if (!HandshakeCodec.TryDecodeDeny(message.Payload, out code, out reason, out _))
            {
                code = DenyCode.Unknown;
                reason = "undecodable deny";
            }
            SetError("denied " + code + ": " + reason);
            DenyCode captured = code;
            MutateStats(delegate (S2OverlayClientStats s) { s.Denies++; s.LastDeny = captured; });
            return false;
        }

        private void HandleMessage(TcpTransport client, AssembledMessage message, SessionState state)
        {
            switch (message.Type)
            {
                case MessageType.Snapshot:
                    HandleSnapshot(client, message, state);
                    break;
                case MessageType.Pong:
                    HandlePong(message, state);
                    break;
                case MessageType.ResyncAck:
                    uint fromSeq, nextSeq;
                    int ackState;
                    if (S2ControlHandler.TryReadResyncAck(message.Payload, out fromSeq, out nextSeq, out ackState))
                    {
                        MutateStats(delegate (S2OverlayClientStats s) { s.ResyncAcks++; });
                    }
                    break;
                case MessageType.Hello:
                    // host ต่อใหม่/session ใหม่กลางคัน — ทำ handshake ใหม่ใน session ถัดไป
                    SetError("unexpected hello mid-session");
                    client.RequestClose();
                    break;
                case MessageType.Deny:
                    DenyCode code;
                    string reason;
                    if (HandshakeCodec.TryDecodeDeny(message.Payload, out code, out reason, out _))
                    {
                        MutateStats(delegate (S2OverlayClientStats s) { s.Denies++; s.LastDeny = code; });
                        SetError("denied " + code + ": " + reason);
                    }
                    client.RequestClose();
                    break;
                case MessageType.Bye:
                    SetError("host bye");
                    client.RequestClose();
                    break;
            }
        }

        private void HandleSnapshot(TcpTransport client, AssembledMessage message, SessionState state)
        {
            GuestSnapshotDisplay display;
            string error;
            if (!S2GuestDecoder.TryDecode(message.Payload, message.Seq, out display, out error))
            {
                SetError("snapshot decode: " + error);
                return;
            }

            uint seq = message.Seq;
            if (display.SessionId != state.SessionId)
            {
                // session ใหม่ (host เปิด connection ใหม่) — ล้าง seq tracking
                state.SessionId = display.SessionId;
                state.ExpectedSeq = 1;
            }

            bool gap = false;
            uint gapFrom = 0;
            bool duplicate = seq < state.ExpectedSeq;
            if (duplicate)
            {
                // ซ้ำ: นับแล้วข้าม (ไม่อัปเดต expected)
            }
            else if (seq > state.ExpectedSeq)
            {
                gap = true;
                gapFrom = state.ExpectedSeq;
            }

            if (!duplicate)
            {
                state.LastSeq = seq;
                state.ExpectedSeq = seq == 0xFFFFFFFF ? 1u : seq + 1;
                PushRing(seq, display);
            }

            bool divA = !display.ReplicaHashMatch;
            bool gapClosed = display.Resync && display.ReplicaHashMatch;
            uint sessionId = state.SessionId;
            uint lastSeq = state.LastSeq;
            uint expectedSeq = state.ExpectedSeq;
            MutateStats(delegate (S2OverlayClientStats s)
            {
                s.SessionId = sessionId;
                s.LastSeq = lastSeq;
                s.ExpectedSeq = expectedSeq;
                s.Snapshots++;
                if (gap) s.Gaps++;
                if (duplicate) s.Duplicates++;
                s.DivAChecked++;
                if (divA) s.DivAMismatch++;
                if (gapClosed) s.GapClosed++;
            });

            if (gap)
            {
                client.Send(S2ControlHandler.BuildResync(gapFrom));
                MutateStats(delegate (S2OverlayClientStats s) { s.ResyncsSent++; });
            }
        }

        private void HandlePong(AssembledMessage message, SessionState state)
        {
            ulong t1, t2, t3;
            if (!S2ControlHandler.TryReadPong(message.Payload, out t1, out t2, out t3)) return;
            ulong t4 = NowUs();
            long offset = ((long)(t2 - t1) + (long)(t3 - t4)) / 2;
            long rtt = (long)(t4 - t1) - (long)(t3 - t2);
            bool isMin = rtt < state.MinRttUs;
            if (isMin) state.MinRttUs = rtt;
            MutateStats(delegate (S2OverlayClientStats s)
            {
                s.Pongs++;
                s.LastRttUs = rtt;
                if (isMin || !s.HasOffset)
                {
                    // NTP min-RTT filter (เหมือน viewer): sample ที่ RTT น้อยที่สุด bias น้อยที่สุด
                    s.HasOffset = true;
                    s.OffsetUsAtMinRtt = offset;
                }
            });
        }

        private void PushRing(uint seq, GuestSnapshotDisplay display)
        {
            lock (_lock)
            {
                _ring[_ringNext] = new GuestTimedSnapshot
                {
                    Seq = seq,
                    ReceivedAtMs = NowMs(),
                    Display = display,
                };
                _ringNext = (_ringNext + 1) % RingSize;
                if (_ringCount < RingSize) _ringCount++;
            }
        }

        // ------------------------------------------------------------------ small helpers

        /// <summary>state ของ session ปัจจุบัน — เป็นของเธรด client ล้วน ๆ (ไม่ต้อง lock)</summary>
        private sealed class SessionState
        {
            public uint SessionId;
            public uint LastSeq;
            public uint ExpectedSeq = 1;
            public long MinRttUs = long.MaxValue;
        }

        private static AssembledMessage AssembleSingle(byte[] raw)
        {
            FrameHeader header;
            if (!FrameCodec.TryReadHeader(raw, out header))
            {
                return new AssembledMessage(MessageType.Unknown, 0, 0, Array.Empty<byte>());
            }
            return new AssembledMessage(header.Type, header.Ver, header.Seq,
                                        Slice(raw, header.PayloadOffset, header.PayloadLength));
        }

        private static byte[] ReceiveWithin(TcpTransport transport, int waitMs)
        {
            long until = NowMs() + waitMs;
            while (NowMs() < until)
            {
                byte[] frame;
                if (transport.TryReceive(out frame)) return frame;
                Thread.Sleep(2);
            }
            return null;
        }

        private void SetError(string error)
        {
            MutateStats(delegate (S2OverlayClientStats s) { s.LastError = error; });
        }

        private void MutateStats(Action<S2OverlayClientStats> mutate)
        {
            lock (_lock)
            {
                mutate(_stats);
            }
        }

        private void SleepInterruptible(int ms)
        {
            int step = 50;
            long end = NowMs() + ms;
            while (_running && NowMs() < end) Thread.Sleep(step);
        }

        private static byte[] Slice(byte[] raw, int offset, int length)
        {
            if (length <= 0) return Array.Empty<byte>();
            byte[] copy = new byte[length];
            Buffer.BlockCopy(raw, offset, copy, 0, length);
            return copy;
        }

        private static S2OverlayClientStats Copy(S2OverlayClientStats source)
        {
            return new S2OverlayClientStats
            {
                Connected = source.Connected,
                Authenticated = source.Authenticated,
                Sessions = source.Sessions,
                SessionId = source.SessionId,
                LastSeq = source.LastSeq,
                ExpectedSeq = source.ExpectedSeq,
                Snapshots = source.Snapshots,
                Gaps = source.Gaps,
                Duplicates = source.Duplicates,
                ResyncsSent = source.ResyncsSent,
                ResyncAcks = source.ResyncAcks,
                GapClosed = source.GapClosed,
                DivAChecked = source.DivAChecked,
                DivAMismatch = source.DivAMismatch,
                Pings = source.Pings,
                Pongs = source.Pongs,
                HasOffset = source.HasOffset,
                OffsetUsAtMinRtt = source.OffsetUsAtMinRtt,
                LastRttUs = source.LastRttUs,
                LastDeny = source.LastDeny,
                Denies = source.Denies,
                LastError = source.LastError,
                Reconnects = source.Reconnects,
            };
        }

        private static long NowMs()
        {
            return Environment.TickCount64;
        }

        private static ulong NowUs()
        {
            return (ulong)(System.Diagnostics.Stopwatch.GetTimestamp() * 1000000L
                           / System.Diagnostics.Stopwatch.Frequency);
        }
    }
}
