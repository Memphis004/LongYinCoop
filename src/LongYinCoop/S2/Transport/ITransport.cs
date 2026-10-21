using System;

namespace LongYinCoop.S2.Transport
{
    public enum TransportKind
    {
        Loopback = 0,
        Tcp = 1,
    }

    /// <summary>
    /// S2a-2: ช่องทางส่ง/รับ frame (byte buffer เท่านั้น — ห้ามถือ IL2CPP object).
    /// Implementations must be thread-safe for Send from the sender thread and
    /// TryReceive from the reader thread.
    /// </summary>
    public interface ITransport
    {
        TransportKind Kind { get; }

        /// <summary>ข้อความสำหรับ log — ห้ามมี secret</summary>
        string Describe { get; }

        bool IsOpen { get; }

        void Open();

        void Close();

        /// <summary>ส่ง 1 frame ที่ encode แล้ว; ห้ามบล็อกเกิน 1 ms; คืน false เมื่อปิด/คิวเต็ม</summary>
        bool Send(byte[] frame);

        /// <summary>ดึง 1 frame; คืน false เมื่อว่าง (ห้ามบล็อก)</summary>
        bool TryReceive(out byte[] frame);

        TransportStats Stats { get; }
    }

    /// <summary>ตัวนับของ transport — ใช้รายงาน p95/bytes ใน evidence</summary>
    public sealed class TransportStats
    {
        public long FramesSent;
        public long FramesReceived;
        public long BytesSent;
        public long BytesReceived;
        public long SendDrops;
        public long ReceiveDrops;
        public long OpenCount;
        public long CloseCount;

        public void Reset()
        {
            FramesSent = FramesReceived = BytesSent = BytesReceived = 0;
            SendDrops = ReceiveDrops = OpenCount = CloseCount = 0;
        }
    }
}
