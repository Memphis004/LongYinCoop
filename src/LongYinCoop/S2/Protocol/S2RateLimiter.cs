using System;

namespace LongYinCoop.S2.Protocol
{
    /// <summary>
    /// sliding-window rate limiter (pure, เวลาเข้ามาจากผู้เรียก)
    /// ใช้กับ: RESYNC (5 ครั้ง/วินาที ต่อ connection) และ handshake ที่ล้มเหลว (1 ครั้ง/5 วินาที)
    /// </summary>
    public sealed class S2RateLimiter
    {
        private readonly long[] _stamps;
        private int _next;
        private int _count;

        public S2RateLimiter(int maxPerWindow, int windowMs)
        {
            MaxPerWindow = maxPerWindow < 1 ? 1 : maxPerWindow;
            WindowMs = windowMs < 1 ? 1 : windowMs;
            _stamps = new long[MaxPerWindow];
        }

        public int MaxPerWindow { get; private set; }

        public int WindowMs { get; private set; }

        /// <summary>จำนวนครั้งที่บันทึกไว้ในหน้าต่างปัจจุบัน</summary>
        public int Used { get { return _count; } }

        /// <summary>ขออนุญาตใช้ 1 ครั้ง ณ เวลานี้</summary>
        public bool Allow(long nowMs)
        {
            Purge(nowMs);
            if (_count >= MaxPerWindow) return false;
            // เก็บ nowMs+1 เพื่อให้ค่า 0 (เวลาเริ่มต้นของเทสต์) ไม่ถูกอ่านว่า "ช่องว่าง"
            _stamps[_next] = nowMs + 1;
            _next = (_next + 1) % MaxPerWindow;
            _count++;
            return true;
        }

        public void Reset()
        {
            _next = 0;
            _count = 0;
            Array.Clear(_stamps, 0, _stamps.Length);
        }

        private void Purge(long nowMs)
        {
            if (_count == 0) return;
            int kept = 0;
            for (int i = 0; i < _stamps.Length; i++)
            {
                if (_stamps[i] == 0) continue;
                if (nowMs + 1 - _stamps[i] < WindowMs) kept++;
                else _stamps[i] = 0;
            }
            _count = kept;
        }
    }
}
