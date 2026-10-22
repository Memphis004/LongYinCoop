using System;
using System.Buffers.Binary;
using LongYinCoop.S2.Protocol;

namespace LongYinCoop.S2.Transport
{
    /// <summary>
    /// ตัวประกอบ frame จาก byte stream ของ TCP (pure — ไม่มี socket, ไม่มี thread)
    ///
    /// TCP ให้ byte ไหลต่อเนื่อง ไม่มีขอบ message: 1 ก้อนอาจมี 3 frame, 1 frame อาจมา 7 ก้อน
    /// จึงต้อง buffer + ตัดตาม `[u16 BE bodyLen][type][ver][u32 seq]` (header เดิม 8 B)
    ///
    /// Fail-closed (แผน S2a-3 §3.1): `bodyLen &lt; 6` · `2+bodyLen &gt; 1100` · type ไม่รู้จัก · ver ไม่ตรง
    /// ⇒ คืน error ให้ผู้เรียก **ปิด connection** ทันที (ไม่พยายามเดา/ข้าม)
    ///
    /// `TryTakeFrame` คืน `false` + `error == null` = "ยังไม่ครบ frame" (รอ byte เพิ่ม)
    /// </summary>
    public sealed class TcpFrameStream
    {
        /// <summary>เพดาน buffer: frame ใหญ่สุด 1100 B + เศษของ frame ถัดไป</summary>
        public const int MaxBuffered = 4096;

        private byte[] _buf = new byte[2048];
        private int _len;

        public int Buffered { get { return _len; } }

        public bool Feed(ReadOnlySpan<byte> data, out string error)
        {
            error = null;
            if (data.Length == 0) return true;
            if (_len + data.Length > MaxBuffered)
            {
                error = "stream buffer over " + MaxBuffered + " B";
                return false;
            }
            if (_len + data.Length > _buf.Length)
            {
                int size = _buf.Length;
                while (size < _len + data.Length) size *= 2;
                Array.Resize(ref _buf, size);
            }
            data.CopyTo(_buf.AsSpan(_len));
            _len += data.Length;
            return true;
        }

        /// <summary>ตัด frame หน้าสุดออก (ถ้าครบและถูกต้อง)</summary>
        public bool TryTakeFrame(out byte[] frame, out string error, byte ver = 0)
        {
            frame = null;
            error = null;
            byte expected = ver == 0 ? S2Protocol.ProtoVer : ver;

            if (_len < 2) return false;
            int bodyLen = BinaryPrimitives.ReadUInt16BigEndian(_buf);
            if (bodyLen < 6)
            {
                error = "bodyLen " + bodyLen + " < 6";
                return false;
            }
            int frameBytes = 2 + bodyLen;
            if (frameBytes > S2Protocol.MaxFrameBytes)
            {
                error = "frame " + frameBytes + " B over cap " + S2Protocol.MaxFrameBytes;
                return false;
            }
            if (_len < frameBytes) return false;

            // frameBytes >= 8 ⇒ header ครบแล้ว
            MessageType type = S2Protocol.ReassembledTypeOrUnknown(_buf[2]);
            if (type == MessageType.Unknown)
            {
                error = "unknown type 0x" + _buf[2].ToString("x2");
                return false;
            }
            if (_buf[3] != expected)
            {
                error = "proto ver " + _buf[3] + " (expected " + expected + ")";
                return false;
            }

            frame = new byte[frameBytes];
            Buffer.BlockCopy(_buf, 0, frame, 0, frameBytes);
            _len -= frameBytes;
            if (_len > 0) Buffer.BlockCopy(_buf, frameBytes, _buf, 0, _len);
            return true;
        }

        public void Reset()
        {
            _len = 0;
        }
    }
}
