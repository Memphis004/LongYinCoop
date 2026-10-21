using System;
using System.Globalization;
using LongYinCoop.S2.Protocol;

namespace LongYinCoop.S2.Host
{
    /// <summary>
    /// ตัวนับของ S2 host (S2a-2). `CommandsAccepted` **ไม่มีทางเพิ่มได้เลย** เพราะ S2 ไม่มี
    /// write path ไปยัง state ของเกม — ถ้าอนาคตมีคนเพิ่ม path เขียน ค่าตัวนี้ต้องยังเป็น 0
    /// และ exit 6 จะจับได้
    /// </summary>
    public sealed class S2Counters
    {
        public long GuestFramesSeen;
        public long GuestChunkFrames;
        public long GuestFramesReassembled;
        public long GuestFramesAwaiting;
        public long ForbiddenTypeDrops;
        public long MalformedDrops;
        public long ControlFramesHandled;
        public long CommandsAccepted;
        public long FramesSent;
        public long FramesQueuedDropped;
        public long BytesSent;
        public string LastDropReason;

        public object Snapshot()
        {
            return new
            {
                guestFramesSeen = GuestFramesSeen,
                guestChunkFrames = GuestChunkFrames,
                guestFramesReassembled = GuestFramesReassembled,
                guestFramesAwaiting = GuestFramesAwaiting,
                forbiddenTypeDrops = ForbiddenTypeDrops,
                malformedDrops = MalformedDrops,
                controlFramesHandled = ControlFramesHandled,
                commandsAccepted = CommandsAccepted,
                framesSent = FramesSent,
                framesQueuedDropped = FramesQueuedDropped,
                bytesSent = BytesSent,
                lastDropReason = LastDropReason,
            };
        }

        public bool NoForbiddenActivity
        {
            get { return ForbiddenTypeDrops == 0 && CommandsAccepted == 0; }
        }
    }

    public enum S2GateOutcome
    {
        Await = 0,
        Control = 1,
        Forbidden = 2,
        Malformed = 3,
    }

    /// <summary>
    /// allowlist ช่อง guest→host (§13.2 ข้อ 7): รับเฉพาะ Auth/Ping/Resync/Bye;
    /// ทุกอย่างอื่น (รวม frame ที่ถูก chunk) = DROP + นับ + ไม่แตะ audit
    /// </summary>
    public sealed class S2GuestGate
    {
        private readonly ChunkReassembler _reassembler = new ChunkReassembler();
        private readonly S2Counters _counters;

        public S2GuestGate(S2Counters counters)
        {
            _counters = counters ?? new S2Counters();
        }

        public S2Counters Counters { get { return _counters; } }

        public int PendingChunks { get { return _reassembler.PendingCount; } }

        public S2GateOutcome Accept(byte[] raw, long nowMs, out AssembledMessage message,
                                    out string reason)
        {
            message = default(AssembledMessage);
            reason = null;
            if (raw == null || raw.Length == 0)
            {
                _counters.MalformedDrops++;
                reason = "empty frame";
                return S2GateOutcome.Malformed;
            }

            FrameHeader header;
            if (!FrameCodec.TryReadHeader(raw, out header))
            {
                _counters.MalformedDrops++;
                reason = "undecodable header";
                return S2GateOutcome.Malformed;
            }

            if (raw.Length != header.FrameBytes)
            {
                _counters.MalformedDrops++;
                reason = string.Format(CultureInfo.InvariantCulture,
                    "trailing bytes: got {0}, header says {1}", raw.Length, header.FrameBytes);
                return S2GateOutcome.Malformed;
            }

            _counters.GuestFramesSeen++;

            if (header.Type == MessageType.Chunk)
            {
                _counters.GuestChunkFrames++;
                int pendingBefore = _reassembler.PendingCount;
                AssembledMessage assembled;
                bool complete = _reassembler.Accept(header, payload: raw.AsSpan(header.PayloadOffset,
                    header.PayloadLength), nowMs: nowMs, message: out assembled);
                if (!complete)
                {
                    if (_reassembler.PendingCount > pendingBefore)
                    {
                        _counters.GuestFramesAwaiting++;
                        reason = "chunk awaiting completion";
                        return S2GateOutcome.Await;
                    }
                    _counters.MalformedDrops++;
                    reason = "invalid chunk (duplicate/out-of-range/timeout/type mismatch)";
                    return S2GateOutcome.Malformed;
                }
                _counters.GuestFramesReassembled++;
                message = assembled;
                return GateOnType(assembled.Type, ref message, ref reason);
            }

            message = new AssembledMessage(header.Type, header.Ver, header.Seq,
                                           Slice(raw, header.PayloadOffset, header.PayloadLength));
            return GateOnType(header.Type, ref message, ref reason);
        }

        private S2GateOutcome GateOnType(MessageType type, ref AssembledMessage message,
                                         ref string reason)
        {
            if (!S2Protocol.IsGuestToHostAllowed(type))
            {
                _counters.ForbiddenTypeDrops++;
                _counters.LastDropReason = "forbidden type " + type;
                reason = _counters.LastDropReason;
                message = default(AssembledMessage);
                return S2GateOutcome.Forbidden;
            }

            _counters.ControlFramesHandled++;
            reason = "control " + type;
            return S2GateOutcome.Control;
        }

        private static byte[] Slice(byte[] raw, int offset, int length)
        {
            if (length <= 0) return Array.Empty<byte>();
            byte[] copy = new byte[length];
            Buffer.BlockCopy(raw, offset, copy, 0, length);
            return copy;
        }
    }
}
