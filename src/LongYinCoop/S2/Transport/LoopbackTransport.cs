using System;
using System.Collections.Concurrent;

namespace LongYinCoop.S2.Transport
{
    /// <summary>
    /// S2a-2: in-process pair, ไม่มี socket/ไม่มี dependency ใหม่.
    /// Send() ฝั่งหนึ่งไปโผล่ที่ inbox ของอีกฝั่ง. คิวมีขอบเขต — คิวเต็ม = ทิ้ง
    /// (นับ SendDrops) ไม่โตเรื่อย ๆ และห้ามบล็อก main thread.
    /// </summary>
    public sealed class LoopbackTransport : ITransport
    {
        private readonly ConcurrentQueue<byte[]> _inbox = new ConcurrentQueue<byte[]>();
        private readonly TransportStats _stats = new TransportStats();
        private readonly int _capacity;
        private LoopbackTransport _peer;
        private volatile bool _open;

        public LoopbackTransport(int capacity = 256)
        {
            _capacity = capacity < 1 ? 1 : capacity;
        }

        public TransportKind Kind { get { return TransportKind.Loopback; } }

        public string Describe { get { return "loopback(in-proc)"; } }

        public bool IsOpen { get { return _open; } }

        public TransportStats Stats { get { return _stats; } }

        /// <summary>จำนวน frame ที่รออยู่ใน inbox ปลายทาง (ยังไม่ถูกอ่าน)</summary>
        public int Queued { get { return _inbox.Count; } }

        /// <summary>จำนวน frame ที่ทิ้งเพราะคิวเต็ม</summary>
        public long FullDrops { get; private set; }

        public void Open()
        {
            _open = true;
            _stats.OpenCount++;
        }

        public void Close()
        {
            _open = false;
            _stats.CloseCount++;
        }

        public bool Send(byte[] frame)
        {
            if (frame == null) return false;
            if (!_open || _peer == null || !_peer._open)
            {
                // refuse = counted (closed link is a drop, never an exception on the main thread)
                _stats.SendDrops++;
                return false;
            }

            if (_peer._inbox.Count >= _capacity)
            {
                _stats.SendDrops++;
                FullDrops++;
                return false;
            }

            _peer._inbox.Enqueue(frame);
            _stats.FramesSent++;
            _stats.BytesSent += frame.Length;
            return true;
        }

        public bool TryReceive(out byte[] frame)
        {
            if (_inbox.TryDequeue(out frame))
            {
                _stats.FramesReceived++;
                _stats.BytesReceived += frame.Length;
                return true;
            }
            frame = null;
            return false;
        }

        public void ResetStats()
        {
            _stats.Reset();
            FullDrops = 0;
        }

        /// <summary>สร้างคู่ที่ต่อกัน (host, guest) — ทั้งคู่เปิดให้อัตโนมัติ</summary>
        public static void CreatePair(out LoopbackTransport host, out LoopbackTransport guest,
                                      int capacity = 256)
        {
            LoopbackTransport a = new LoopbackTransport(capacity);
            LoopbackTransport b = new LoopbackTransport(capacity);
            a._peer = b;
            b._peer = a;
            a.Open();
            b.Open();
            host = a;
            guest = b;
        }

        /// <summary>สร้างคู่แบบไม่เปิด — สำหรับทดสอบเส้นทาง fail-closed</summary>
        public static void CreatePairClosed(out LoopbackTransport host, out LoopbackTransport guest,
                                            int capacity = 256)
        {
            LoopbackTransport a = new LoopbackTransport(capacity);
            LoopbackTransport b = new LoopbackTransport(capacity);
            a._peer = b;
            b._peer = a;
            host = a;
            guest = b;
        }
    }
}
