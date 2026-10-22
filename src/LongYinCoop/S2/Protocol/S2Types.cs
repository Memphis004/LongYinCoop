using System;

namespace LongYinCoop.S2.Protocol
{
    /// <summary>
    /// S2 wire message types (rev2 §13, approved 2026-10-21).
    /// Chunk is a transport-level wrapper and is allowed in both directions; the
    /// reassembled type is what the allowlists are applied to.
    /// </summary>
    public enum MessageType : byte
    {
        Unknown = 0,

        // host -> guest
        Hello = 1,
        World = 2,
        BattleEvent = 3,
        BattleLog = 4,
        Snapshot = 5,
        Pong = 6,
        Bye = 7,
        AuthResult = 8,
        Deny = 9,

        // transport
        Chunk = 10,

        // host -> guest (S2a-3, ProtoVer 2): ตอบรับ RESYNC ของ guest
        ResyncAck = 11,

        // guest -> host (allowlist)
        Auth = 32,
        Ping = 33,
        Resync = 34,
    }

    public static class S2Protocol
    {
        /// <summary>
        /// S2a-3 = 2 (เพิ่ม sessionId/hostApplyUs/replicaHash/resync flag ใน envelope + type ResyncAck=11)
        /// ไม่ลบ/เปลี่ยนความหมายฟิลด์เดิม และ **ไม่แก้ `ReplicaHash` spec `s2|2`** (มติ 2026-10-21 ข้อ 2(ii))
        /// </summary>
        public const byte ProtoVer = 2;

        /// <summary>u16 BE body length + type + version + u32 BE seq.</summary>
        public const int HeaderBytes = 8;
        public const int MaxFrameBytes = 1100;
        public const int MaxPayloadBytes = MaxFrameBytes - HeaderBytes;

        /// <summary>Reassembled message cap (chunking), per plan §3.2.</summary>
        public const int MaxMessageBytes = 64 * 1024;
        public const int ChunkHeaderBytes = 7; // msgId u16 + idx u16 + total u16 + type u8
        public const int ChunkDataBytes = MaxPayloadBytes - ChunkHeaderBytes;
        public const int ReassemblyTimeoutMs = 2000;
        public const int MaxPendingMessages = 8;

        /// <summary>Guest -> host channel: control frames only, never a game action.</summary>
        public static bool IsGuestToHostAllowed(MessageType type)
        {
            switch (type)
            {
                case MessageType.Auth:
                case MessageType.Ping:
                case MessageType.Resync:
                case MessageType.Bye:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsHostToGuest(MessageType type)
        {
            switch (type)
            {
                case MessageType.Hello:
                case MessageType.World:
                case MessageType.BattleEvent:
                case MessageType.BattleLog:
                case MessageType.Snapshot:
                case MessageType.Pong:
                case MessageType.Bye:
                case MessageType.AuthResult:
                case MessageType.Deny:
                case MessageType.ResyncAck:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>ชนิดที่เป็นข้อมูลสำเนา (นับ seq) — S2a-3 มี Snapshot ตัวเดียว</summary>
        public static bool IsDataMessage(MessageType type)
        {
            return type == MessageType.Snapshot;
        }

        public static MessageType ReassembledTypeOrUnknown(byte raw)
        {
            return Enum.IsDefined(typeof(MessageType), raw) ? (MessageType)raw : MessageType.Unknown;
        }
    }
}
