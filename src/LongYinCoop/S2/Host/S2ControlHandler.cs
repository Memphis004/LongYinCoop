using System;
using System.Globalization;
using LongYinCoop.S2.Protocol;

namespace LongYinCoop.S2.Host
{
    public enum S2ControlAction
    {
        None = 0,
        RepliedPong = 1,
        ResyncAccepted = 2,
        ResyncThrottled = 3,
        Closing = 4,
        Rejected = 5,
        Ignored = 6,
    }

    /// <summary>
    /// ตัวจัดการช่อง guest→host (แผน S2a-3 §3.3): **ไม่มี write path ไปยังเกมเลย**
    ///
    /// รับเฉพาะ 4 ชนิดที่ allowlist ผ่านมาแล้ว (Auth/Ping/Resync/Bye) และทำได้แค่:
    ///   Ping  → Pong{t1,t2,t3} (ให้ guest คำนวณ clock offset แบบ NTP — §6.1)
    ///   Resync→ ResyncAck{fromSeq,nextSeq,state} (ทันที, seq=0) + ตั้งธงให้ snapshot ใบถัดไป
    ///           ติด `resync=true` และข้าม pacing หนึ่งครั้ง · จำกัด 5 ครั้ง/วินาที
    ///   Bye   → ตั้งธงให้ปิด connection
    ///   Auth  → ปฏิเสธ (authenticated แล้ว) + นับ (ไม่ใช่คำสั่งเกม)
    ///
    /// ชนิดอื่นไม่เคยมาถึงที่นี่ (gate ทิ้ง + นับ `forbiddenTypeDrops`) และ `commandsAccepted` ยังไม่มีทางเพิ่ม
    /// </summary>
    public sealed class S2ControlHandler
    {
        public const int ResyncPerSecond = 5;
        public const int PingPayloadBytes = 8;
        public const int ResyncAckStateIdle = 0;
        public const int ResyncAckStateBattle = 1;

        private readonly S2Counters _counters;
        private readonly S2RateLimiter _resync;

        private bool _resyncPending;
        private uint _resyncFromSeq;

        public S2ControlHandler(S2Counters counters)
        {
            _counters = counters ?? new S2Counters();
            _resync = new S2RateLimiter(ResyncPerSecond, 1000);
        }

        public bool CloseRequested { get; private set; }

        public bool ResyncPending { get { return _resyncPending; } }

        /// <summary>เฟรมถัดไปต้องติดธง resync (ใช้ครั้งเดียว)</summary>
        public bool ConsumeResyncFlag(out uint fromSeq)
        {
            fromSeq = _resyncFromSeq;
            if (!_resyncPending) return false;
            _resyncPending = false;
            return true;
        }

        public void Reset()
        {
            CloseRequested = false;
            _resyncPending = false;
            _resyncFromSeq = 0;
            _resync.Reset();
        }

        public S2ControlAction Handle(in AssembledMessage msg, long nowMs, bool inBattle, uint nextSeq,
                                      out byte[] reply, out string reason)
        {
            reply = null;
            reason = null;

            switch (msg.Type)
            {
                case MessageType.Ping:
                    return HandlePing(msg, nowMs, out reply, out reason);
                case MessageType.Resync:
                    return HandleResync(msg, nowMs, inBattle, nextSeq, out reply, out reason);
                case MessageType.Bye:
                    _counters.ByeHandled++;
                    CloseRequested = true;
                    reason = "guest bye";
                    return S2ControlAction.Closing;
                case MessageType.Auth:
                    // authenticated แล้ว: ไม่ใช่คำสั่งเกม — นับแล้วเมิน (ตัวเรียกนับ violation เอง)
                    _counters.AuthAfterHandshake++;
                    reason = "auth after handshake";
                    return S2ControlAction.Rejected;
                default:
                    reason = "unexpected type " + msg.Type;
                    return S2ControlAction.Ignored;
            }
        }

        private S2ControlAction HandlePing(in AssembledMessage msg, long nowMs,
                                           out byte[] reply, out string reason)
        {
            reply = null;
            reason = null;
            ulong t1;
            if (!TryReadPing(msg.Payload, out t1))
            {
                _counters.MalformedDrops++;
                reason = "ping payload must be 8 B";
                return S2ControlAction.Rejected;
            }

            ulong t2 = NowUs();
            CompactWriter w = new CompactWriter(32);
            w.WriteVarULong(t1);
            w.WriteVarULong(t2);
            w.WriteVarULong(NowUs());
            reply = FrameCodec.Encode(MessageType.Pong, S2Protocol.ProtoVer, 0, w.ToArray());
            _counters.PingsHandled++;
            _counters.ControlFramesHandled++;
            return S2ControlAction.RepliedPong;
        }

        private S2ControlAction HandleResync(in AssembledMessage msg, long nowMs, bool inBattle, uint nextSeq,
                                             out byte[] reply, out string reason)
        {
            reply = null;
            reason = null;
            uint lastSeq;
            if (!TryReadResync(msg.Payload, out lastSeq))
            {
                _counters.MalformedDrops++;
                reason = "resync payload must be a varu32";
                return S2ControlAction.Rejected;
            }

            _counters.ResyncRequests++;
            _counters.ControlFramesHandled++;

            if (!_resync.Allow(nowMs))
            {
                _counters.ResyncThrottled++;
                reason = "resync throttled";
                return S2ControlAction.ResyncThrottled;
            }

            _resyncPending = true;
            _resyncFromSeq = lastSeq;
            _counters.ResyncServed++;

            CompactWriter w = new CompactWriter(16);
            w.WriteVarUInt(lastSeq);
            w.WriteVarUInt(nextSeq);
            w.WriteByte(inBattle ? (byte)ResyncAckStateBattle : (byte)ResyncAckStateIdle);
            reply = FrameCodec.Encode(MessageType.ResyncAck, S2Protocol.ProtoVer, 0, w.ToArray());
            reason = string.Format(CultureInfo.InvariantCulture, "resync fromSeq={0} nextSeq={1}", lastSeq, nextSeq);
            return S2ControlAction.ResyncAccepted;
        }

        public static bool TryReadPing(byte[] payload, out ulong t1)
        {
            t1 = 0;
            if (payload == null || payload.Length != PingPayloadBytes) return false;
            t1 = BitConverter.ToUInt64(payload, 0);
            return true;
        }

        public static bool TryReadResync(byte[] payload, out uint lastSeq)
        {
            lastSeq = 0;
            if (payload == null || payload.Length == 0) return false;
            try
            {
                CompactReader r = new CompactReader(payload);
                lastSeq = r.ReadVarUInt();
                return r.Remaining == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>guest อ่าน ResyncAck (ใช้ใน client ทดสอบ)</summary>
        public static bool TryReadResyncAck(byte[] payload, out uint fromSeq, out uint nextSeq,
                                            out int state)
        {
            fromSeq = 0;
            nextSeq = 0;
            state = 0;
            try
            {
                CompactReader r = new CompactReader(payload);
                fromSeq = r.ReadVarUInt();
                nextSeq = r.ReadVarUInt();
                state = r.ReadByte();
                return r.Remaining == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Pong: t1 (guest) · t2 (host รับ) · t3 (host mono ms) — ใช้คำนวณ offset</summary>
        public static bool TryReadPong(byte[] payload, out ulong t1, out ulong t2, out ulong t3)
        {
            t1 = t2 = t3 = 0;
            try
            {
                CompactReader r = new CompactReader(payload);
                t1 = r.ReadVarULong();
                t2 = r.ReadVarULong();
                t3 = r.ReadVarULong();
                return r.Remaining == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static byte[] BuildPing(ulong t1Us)
        {
            byte[] payload = new byte[PingPayloadBytes];
            BitConverter.GetBytes(t1Us).CopyTo(payload, 0);
            return FrameCodec.Encode(MessageType.Ping, S2Protocol.ProtoVer, 0, payload);
        }

        public static byte[] BuildResync(uint lastSeq)
        {
            CompactWriter w = new CompactWriter(8);
            w.WriteVarUInt(lastSeq);
            return FrameCodec.Encode(MessageType.Resync, S2Protocol.ProtoVer, 0, w.ToArray());
        }

        public static byte[] BuildBye()
        {
            return FrameCodec.Encode(MessageType.Bye, S2Protocol.ProtoVer, 0, Array.Empty<byte>());
        }

        private static ulong NowUs()
        {
            return (ulong)(System.Diagnostics.Stopwatch.GetTimestamp() * 1000000L
                           / System.Diagnostics.Stopwatch.Frequency);
        }
    }
}
