using System;
using System.Globalization;

namespace LongYinCoop.S2.Host
{
    /// <summary>
    /// ตัวสะสมต่อเฟส (A/B ของ exit 3) — ใช้บน main thread เท่านั้น
    /// exit 3 ตัดสินจาก **ค่าเฉลี่ยของทุกเฟส A เทียบทุกเฟส B** ไม่ใช่รอบเดียว
    /// </summary>
    public sealed class S2PhaseStats
    {
        public S2PhaseStats(string name)
        {
            Name = name ?? "ms";
        }

        public string Name { get; private set; }

        public long Added { get; private set; }

        public double Sum { get; private set; }

        public double Min { get; private set; }

        public double Max { get; private set; }

        public double Last { get; private set; }

        public double Mean
        {
            get { return Added == 0 ? 0 : Sum / Added; }
        }

        public double MeanRounded
        {
            get { return Math.Round(Mean, 4); }
        }

        public void Add(double value)
        {
            if (Added == 0)
            {
                Min = value;
                Max = value;
            }
            else
            {
                if (value < Min) Min = value;
                if (value > Max) Max = value;
            }
            Sum += value;
            Last = value;
            Added++;
        }

        public void Reset()
        {
            Added = 0;
            Sum = 0;
            Min = 0;
            Max = 0;
            Last = 0;
        }

        public object Snapshot()
        {
            return new
            {
                name = Name,
                samples = Added,
                meanMs = MeanRounded,
                minMs = Math.Round(Min, 4),
                maxMs = Math.Round(Max, 4),
                lastMs = Math.Round(Last, 4),
            };
        }

        public string ToText()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "{0}: n={1} meanMs={2:F4} minMs={3:F4} maxMs={4:F4}", Name, Added,
                Mean, Min, Max);
        }
    }

    /// <summary>
    /// เกณฑ์ exit 3 (rev3, มติผู้ใช้ 2026-10-21): เทียบเป็น**บล็อกสลับ A-B-A-B** ในศึกเดียว
    ///   A = `coop.net.enabled=false` (baseline, S2 idle) · B = `true` (S2 ทำงาน)
    ///   delta = mean(B) − mean(A)  — **ดูเครื่องหมาย ไม่ใช่ค่าสัมบูรณ์**
    ///   PASS         : delta ≤ marginMs        (B แพงกว่า A ไม่เกิน 1 ms, ติดลบก็ผ่าน)
    ///   INCONCLUSIVE : |delta − marginMs| ≤ bandMs  (ใกล้ 1 ms → สรุปไม่ได้ ห้ามฟันธงผ่านจากรอบเดียว)
    ///   FAIL         : delta > marginMs + bandMs
    /// </summary>
    public static class S2Exit3Rule
    {
        public const double MarginMs = 1.0;
        public const double BandMs = 0.5;

        public enum Verdict
        {
            Pass = 0,
            Inconclusive = 1,
            Fail = 2,
        }

        public static double Delta(double aMean, double bMean)
        {
            return bMean - aMean;
        }

        public static Verdict Decide(double aMean, double bMean,
                                     double marginMs = MarginMs, double bandMs = BandMs)
        {
            double delta = Delta(aMean, bMean);
            if (Math.Abs(delta - marginMs) <= bandMs) return Verdict.Inconclusive;
            return delta <= marginMs ? Verdict.Pass : Verdict.Fail;
        }

        public static string Text(Verdict verdict)
        {
            switch (verdict)
            {
                case Verdict.Pass: return "PASS";
                case Verdict.Fail: return "FAIL";
                default: return "INCONCLUSIVE";
            }
        }

        public static string Report(double aMean, double bMean)
        {
            Verdict verdict = Decide(aMean, bMean);
            return string.Format(CultureInfo.InvariantCulture,
                "exit3 A(meanMs)={0:F4} B(meanMs)={1:F4} delta(B-A)={2:F4} marginMs={3:F2} bandMs={4:F2} verdict={5}",
                aMean, bMean, Delta(aMean, bMean), MarginMs, BandMs, Text(verdict));
        }
    }
}
