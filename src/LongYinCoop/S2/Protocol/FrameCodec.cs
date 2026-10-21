using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace LongYinCoop.S2.Protocol
{
    public readonly struct FrameHeader
    {
        public FrameHeader(MessageType type, byte ver, uint seq, int payloadOffset, int payloadLength, int frameBytes)
        {
            Type = type;
            Ver = ver;
            Seq = seq;
            PayloadOffset = payloadOffset;
            PayloadLength = payloadLength;
            FrameBytes = frameBytes;
        }

        public MessageType Type { get; }
        public byte Ver { get; }
        public uint Seq { get; }
        public int PayloadOffset { get; }
        public int PayloadLength { get; }
        public int FrameBytes { get; }
    }

    public readonly struct AssembledMessage
    {
        public AssembledMessage(MessageType type, byte ver, uint seq, byte[] payload)
        {
            Type = type;
            Ver = ver;
            Seq = seq;
            Payload = payload;
        }

        public MessageType Type { get; }
        public byte Ver { get; }
        public uint Seq { get; }
        public byte[] Payload { get; }
    }

    /// <summary>[u16 BE bodyLen][u8 type][u8 ver][u32 BE seq][payload], bodyLen = 6 + payload.</summary>
    public static class FrameCodec
    {
        public static byte[] Encode(MessageType type, byte ver, uint seq, ReadOnlySpan<byte> payload)
        {
            int bodyLen = 6 + payload.Length;
            if (bodyLen > ushort.MaxValue) throw new ArgumentException("frame body over u16");
            if (2 + bodyLen > S2Protocol.MaxFrameBytes)
                throw new ArgumentException($"frame {2 + bodyLen} B over cap {S2Protocol.MaxFrameBytes}");

            byte[] frame = new byte[2 + bodyLen];
            BinaryPrimitives.WriteUInt16BigEndian(frame, (ushort)bodyLen);
            frame[2] = (byte)type;
            frame[3] = ver;
            BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(4), seq);
            payload.CopyTo(frame.AsSpan(S2Protocol.HeaderBytes));
            return frame;
        }

        public static bool TryReadHeader(ReadOnlySpan<byte> buffer, out FrameHeader header)
        {
            header = default;
            if (buffer.Length < S2Protocol.HeaderBytes) return false;

            int bodyLen = BinaryPrimitives.ReadUInt16BigEndian(buffer);
            if (bodyLen < 6) return false;
            int frameBytes = 2 + bodyLen;
            if (frameBytes > S2Protocol.MaxFrameBytes) return false;
            if (frameBytes > buffer.Length) return false;

            MessageType type = S2Protocol.ReassembledTypeOrUnknown(buffer[2]);
            if (type == MessageType.Unknown) return false;

            header = new FrameHeader(
                type,
                buffer[3],
                BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(4)),
                S2Protocol.HeaderBytes,
                bodyLen - 6,
                frameBytes);
            return true;
        }

        public static bool TryDecode(ReadOnlySpan<byte> buffer, out FrameHeader header, out byte[] payload)
        {
            payload = Array.Empty<byte>();
            if (!TryReadHeader(buffer, out header)) return false;
            payload = buffer.Slice(header.PayloadOffset, header.PayloadLength).ToArray();
            return true;
        }

        /// <summary>Byte offset of the next frame, or -1 when the header is not readable yet.</summary>
        public static int NextFrameOffset(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length < 2) return -1;
            int bodyLen = BinaryPrimitives.ReadUInt16BigEndian(buffer);
            if (bodyLen < 6) return -1;
            return 2 + bodyLen;
        }
    }

    /// <summary>Splits oversized messages into Chunk frames and reassembles them fail-closed.</summary>
    public static class ChunkCodec
    {
        public static List<byte[]> Encode(ushort msgId, MessageType type, byte ver, uint seq,
                                          ReadOnlySpan<byte> payload)
        {
            if (type == MessageType.Chunk) throw new ArgumentException("cannot chunk a chunk");
            if (payload.Length > S2Protocol.MaxMessageBytes)
                throw new ArgumentException("message over 64 KB");
            if (2 + 6 + payload.Length <= S2Protocol.MaxFrameBytes)
                return new List<byte[]> { FrameCodec.Encode(type, ver, seq, payload) };

            int total = (payload.Length + S2Protocol.ChunkDataBytes - 1) / S2Protocol.ChunkDataBytes;
            if (total > ushort.MaxValue) throw new ArgumentException("too many chunks");

            List<byte[]> frames = new List<byte[]>(total);
            for (int idx = 0; idx < total; idx++)
            {
                int offset = idx * S2Protocol.ChunkDataBytes;
                int len = Math.Min(S2Protocol.ChunkDataBytes, payload.Length - offset);
                byte[] body = new byte[S2Protocol.ChunkHeaderBytes + len];
                BinaryPrimitives.WriteUInt16BigEndian(body, msgId);
                BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(2), (ushort)idx);
                BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(4), (ushort)total);
                body[6] = (byte)type;
                payload.Slice(offset, len).CopyTo(body.AsSpan(S2Protocol.ChunkHeaderBytes));
                frames.Add(FrameCodec.Encode(MessageType.Chunk, ver, seq, body));
            }
            return frames;
        }
    }

    public sealed class ChunkReassembler
    {
        private sealed class Pending
        {
            public MessageType Type;
            public byte Ver;
            public uint Seq;
            public byte[][] Parts;
            public bool[] Seen;
            public int Received;
            public int TotalBytes;
            public long FirstAtMs;
        }

        private readonly Dictionary<ushort, Pending> _pending = new Dictionary<ushort, Pending>();

        public int PendingCount => _pending.Count;

        public void PurgeExpired(long nowMs) => Purge(nowMs);

        public bool Accept(FrameHeader header, ReadOnlySpan<byte> payload, long nowMs,
                           out AssembledMessage message)
        {
            message = default;
            PurgeExpired(nowMs);

            if (header.Type != MessageType.Chunk)
            {
                message = new AssembledMessage(header.Type, header.Ver, header.Seq, payload.ToArray());
                return true;
            }

            if (payload.Length < S2Protocol.ChunkHeaderBytes) return false;
            ushort msgId = BinaryPrimitives.ReadUInt16BigEndian(payload);
            int idx = BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(2));
            int total = BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(4));
            MessageType original = S2Protocol.ReassembledTypeOrUnknown(payload[6]);
            if (original == MessageType.Unknown || original == MessageType.Chunk) return false;
            if (total <= 0 || idx >= total) { _pending.Remove(msgId); return false; }

            if (!_pending.TryGetValue(msgId, out Pending p))
            {
                if (_pending.Count >= S2Protocol.MaxPendingMessages) return false;
                p = new Pending
                {
                    Type = original,
                    Ver = header.Ver,
                    Seq = header.Seq,
                    Parts = new byte[total][],
                    Seen = new bool[total],
                    FirstAtMs = nowMs,
                };
                _pending[msgId] = p;
            }

            if (p.Parts.Length != total || p.Type != original || p.Ver != header.Ver || p.Seq != header.Seq)
            {
                _pending.Remove(msgId);
                return false;
            }
            if (p.Seen[idx]) { _pending.Remove(msgId); return false; }

            byte[] chunk = payload.Slice(S2Protocol.ChunkHeaderBytes).ToArray();
            p.Parts[idx] = chunk;
            p.Seen[idx] = true;
            p.Received++;
            p.TotalBytes += chunk.Length;
            if (p.TotalBytes > S2Protocol.MaxMessageBytes) { _pending.Remove(msgId); return false; }
            if (p.Received < p.Parts.Length) return false;

            byte[] full = new byte[p.TotalBytes];
            int offset = 0;
            for (int i = 0; i < p.Parts.Length; i++)
            {
                Buffer.BlockCopy(p.Parts[i], 0, full, offset, p.Parts[i].Length);
                offset += p.Parts[i].Length;
            }
            _pending.Remove(msgId);
            message = new AssembledMessage(p.Type, p.Ver, p.Seq, full);
            return true;
        }

        private void Purge(long nowMs)
        {
            if (_pending.Count == 0) return;
            List<ushort> stale = null;
            foreach (KeyValuePair<ushort, Pending> kv in _pending)
            {
                if (nowMs - kv.Value.FirstAtMs > S2Protocol.ReassemblyTimeoutMs)
                    (stale ??= new List<ushort>()).Add(kv.Key);
            }
            if (stale == null) return;
            foreach (ushort id in stale) _pending.Remove(id);
        }
    }
}
