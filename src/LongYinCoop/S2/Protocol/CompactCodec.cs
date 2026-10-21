using System;
using System.Buffers.Binary;
using System.Text;

namespace LongYinCoop.S2.Protocol
{
    /// <summary>Little-endian compact writer (varint for sizes/ids, float32 as-is).</summary>
    public sealed class CompactWriter
    {
        private byte[] _buf;
        private int _len;

        public CompactWriter(int capacity = 256)
        {
            _buf = new byte[Math.Max(16, capacity)];
        }

        public int Length => _len;

        public void WriteByte(byte value)
        {
            Ensure(1);
            _buf[_len++] = value;
        }

        public void WriteVarUInt(uint value)
        {
            Ensure(5);
            while (value >= 0x80)
            {
                _buf[_len++] = (byte)(value | 0x80);
                value >>= 7;
            }
            _buf[_len++] = (byte)value;
        }

        public void WriteVarULong(ulong value)
        {
            Ensure(10);
            while (value >= 0x80)
            {
                _buf[_len++] = (byte)(value | 0x80);
                value >>= 7;
            }
            _buf[_len++] = (byte)value;
        }

        public void WriteFloat(float value)
        {
            Ensure(4);
            BinaryPrimitives.WriteSingleLittleEndian(_buf.AsSpan(_len), value);
            _len += 4;
        }

        public void WriteBool(bool value) => WriteByte(value ? (byte)1 : (byte)0);

        /// <summary>UTF-8, u16 length prefix.</summary>
        public void WriteString(string value)
        {
            if (value == null) value = string.Empty;
            byte[] utf8 = Encoding.UTF8.GetBytes(value);
            if (utf8.Length > ushort.MaxValue) throw new ArgumentException("string too long");
            Ensure(2 + utf8.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(_buf.AsSpan(_len), (ushort)utf8.Length);
            _len += 2;
            Buffer.BlockCopy(utf8, 0, _buf, _len, utf8.Length);
            _len += utf8.Length;
        }

        public void WriteBytes(ReadOnlySpan<byte> value)
        {
            WriteVarUInt((uint)value.Length);
            WriteRaw(value);
        }

        public void WriteRaw(ReadOnlySpan<byte> value)
        {
            Ensure(value.Length);
            value.CopyTo(_buf.AsSpan(_len));
            _len += value.Length;
        }

        public byte[] ToArray()
        {
            byte[] outBytes = new byte[_len];
            Buffer.BlockCopy(_buf, 0, outBytes, 0, _len);
            return outBytes;
        }

        private void Ensure(int extra)
        {
            int need = _len + extra;
            if (need <= _buf.Length) return;
            int size = _buf.Length;
            while (size < need) size *= 2;
            Array.Resize(ref _buf, size);
        }
    }

    /// <summary>Reader that mirrors <see cref="CompactWriter"/> and fails closed.</summary>
    public struct CompactReader
    {
        private readonly byte[] _buf;
        private int _pos;

        public CompactReader(byte[] buffer, int offset = 0, int count = -1)
        {
            _buf = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _pos = offset;
            int end = count < 0 ? buffer.Length : offset + count;
            if (offset < 0 || end < offset || end > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            End = end;
        }

        public int Position => _pos;
        public int Remaining => End - _pos;
        private int End { get; }

        public byte ReadByte()
        {
            Need(1);
            return _buf[_pos++];
        }

        public bool ReadBool() => ReadByte() != 0;

        public uint ReadVarUInt()
        {
            uint result = 0;
            int shift = 0;
            for (int i = 0; i < 5; i++)
            {
                Need(1);
                byte b = _buf[_pos++];
                result |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) return result;
                shift += 7;
            }
            throw new FormatException("varint u32 too long");
        }

        public ulong ReadVarULong()
        {
            ulong result = 0;
            int shift = 0;
            for (int i = 0; i < 10; i++)
            {
                Need(1);
                byte b = _buf[_pos++];
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) return result;
                shift += 7;
            }
            throw new FormatException("varint u64 too long");
        }

        public float ReadFloat()
        {
            Need(4);
            float value = BinaryPrimitives.ReadSingleLittleEndian(_buf.AsSpan(_pos));
            _pos += 4;
            return value;
        }

        public string ReadString()
        {
            Need(2);
            int len = BinaryPrimitives.ReadUInt16LittleEndian(_buf.AsSpan(_pos));
            _pos += 2;
            Need(len);
            string value = Encoding.UTF8.GetString(_buf, _pos, len);
            _pos += len;
            return value;
        }

        public byte[] ReadBytes(int maxBytes)
        {
            uint len = ReadVarUInt();
            if (len > (uint)maxBytes) throw new FormatException("byte blob over cap");
            Need((int)len);
            byte[] value = new byte[len];
            Buffer.BlockCopy(_buf, _pos, value, 0, (int)len);
            _pos += (int)len;
            return value;
        }

        public ReadOnlySpan<byte> ReadRaw(int count)
        {
            Need(count);
            ReadOnlySpan<byte> span = _buf.AsSpan(_pos, count);
            _pos += count;
            return span;
        }

        private void Need(int count)
        {
            if (count < 0 || _pos + count > End) throw new FormatException("read past end of frame");
        }
    }
}
