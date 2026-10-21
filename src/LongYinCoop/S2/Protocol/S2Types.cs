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

        // guest -> host (allowlist)
        Auth = 32,
        Ping = 33,
        Resync = 34,
    }

    public static class S2Protocol
    {
        public const byte ProtoVer = 1;

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
                    return true;
                default:
                    return false;
            }
        }

        public static MessageType ReassembledTypeOrUnknown(byte raw)
        {
            return Enum.IsDefined(typeof(MessageType), raw) ? (MessageType)raw : MessageType.Unknown;
        }
    }
}
