using System;
using System.Globalization;

namespace LongYinCoop.S2.Host
{
    /// <summary>
    /// เครื่องวัด frame time ต่อเฟรม (แผน S2a-3 §6 exit X5) — **pure**: รับ `double` เข้ามา
    ///
    /// เหตุผลที่ต้องมี (RUNTIME 2026-10-22 · `evidence/S2a2-.../frame-time-measurability-2345.txt`):
    /// recorder ภายนอกอ่านได้ 2 Hz ⇒ วัด p99 ที่แท้จริง (33.5 ms) ไม่ได้ และ noise ระหว่างบล็อก 30 s
    /// พอ ๆ กับ band ±0.5 ms ⇒ ต้องนับทุกเฟรมในมอดแล้วรายงาน percentile จากหน้าต่างที่นับครบ
    ///
    /// ตัวอ่าน `UnityEngine.Time.unscaledDeltaTime` อยู่ใน adapter ของมอด (`S2HostService`)
    /// **ไม่อยู่ในไฟล์นี้** เพื่อให้ไฟล์นี้ถูก link เข้าเทสต์ (T0) ได้โดยไม่มี Unity
    /// </summary>
    public sealed class S2FrameMeter
    {
        public const int Capacity = 4096;

        private readonly double[] _samples = new double[Capacity];
        private readonly object _lock = new object();
        private int _count;
        private int _next;
        private long _total;

        /// <summary>เพดานที่ถือว่า "ค่าเสีย" (เฟรมค้าง/หยุดชั่วคราว) — ไม่นับเข้า percentile</summary>
        public const double MaxPlausibleMs = 5000.0;

        public long Total { get { lock (_lock) { return _total; } } }

        public int Count { get { lock (_lock) { return _count; } } }

        public bool Add(double deltaMs)
        {
            if (double.IsNaN(deltaMs) || double.IsInfinity(deltaMs)) return false;
            if (deltaMs <= 0 || deltaMs > MaxPlausibleMs) return false;
            lock (_lock)
            {
                _samples[_next] = deltaMs;
                _next = (_next + 1) % Capacity;
                if (_count < Capacity) _count++;
                _total++;
            }
            return true;
        }

        public void Reset()
        {
            lock (_lock)
            {
                _count = 0;
                _next = 0;
            }
        }

        public double Percentile(double p)
        {
            double[] copy;
            lock (_lock)
            {
                if (_count == 0) return 0;
                copy = new double[_count];
                Array.Copy(_samples, copy, _count);
            }
            Array.Sort(copy);
            int idx = (int)Math.Ceiling(p * copy.Length) - 1;
            if (idx < 0) idx = 0;
            if (idx >= copy.Length) idx = copy.Length - 1;
            return copy[idx];
        }

        public double Mean()
        {
            lock (_lock)
            {
                if (_count == 0) return 0;
                double sum = 0;
                for (int i = 0; i < _count; i++) sum += _samples[i];
                return sum / _count;
            }
        }

        public object Snapshot()
        {
            return new
            {
                frames = Total,
                window = Count,
                p50 = Math.Round(Percentile(0.50), 3),
                p95 = Math.Round(Percentile(0.95), 3),
                p99 = Math.Round(Percentile(0.99), 3),
                max = Math.Round(Percentile(1.0), 3),
                mean = Math.Round(Mean(), 3),
            };
        }

        public string ToMarker(string extraFields = null)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "framemeter.window frames={0} n={1} p50={2:F3} p95={3:F3} p99={4:F3} max={5:F3} mean={6:F3}{7}",
                Total, Count, Math.Round(Percentile(0.50), 3), Math.Round(Percentile(0.95), 3),
                Math.Round(Percentile(0.99), 3), Math.Round(Percentile(1.0), 3),
                Math.Round(Mean(), 3), extraFields == null ? string.Empty : " " + extraFields);
        }
    }

    /// <summary>
    /// เกณฑ์ X5 (แผน §6.2): p99 ของบล็อกที่ guest หน่วง/หลุด เทียบบล็อก baseline (S2 ปิด)
    ///   delta = p99(B) − p99(A) · margin 1.0 ms · band ±0.5 ms (กติกาเดียวกับ rev5)
    ///   |delta − 1| ≤ 0.5 ⇒ INCONCLUSIVE · delta > 1.5 ⇒ FAIL
    /// </summary>
    public static class S2FrameRule
    {
        public const double MarginMs = 1.0;
        public const double BandMs = 0.5;

        public static double Delta(double baselineP99, double loadedP99)
        {
            return loadedP99 - baselineP99;
        }

        public static S2Exit3Rule.Verdict Decide(double baselineP99, double loadedP99)
        {
            return S2Exit3Rule.Decide(baselineP99, loadedP99, MarginMs, BandMs);
        }

        public static string Report(double baselineP99, double loadedP99)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "exit5 p99(A)={0:F3}ms p99(B)={1:F3}ms delta={2:F3}ms marginMs={3:F1} bandMs={4:F1} verdict={5}",
                baselineP99, loadedP99, Delta(baselineP99, loadedP99), MarginMs, BandMs,
                S2Exit3Rule.Text(Decide(baselineP99, loadedP99)));
        }
    }
}
