using System;
using System.Diagnostics;
using System.Threading;

namespace LongYinCoop
{
    /// <summary>ตัววัดต้นทุนของ mod เอง (Stopwatch ในโค้ดเรา ไม่ใช่ frame time ของเกม)</summary>
    internal static class S1Metrics
    {
        private const int Capacity = 4096;

        private static readonly double[] TickMs = new double[Capacity];
        private static int _tickNext;
        private static int _tickCount;

        private static readonly long[] PostfixTicks = new long[Capacity];
        private static int _postNext;
        private static int _postCount;

        private static readonly long StartTicks = Stopwatch.GetTimestamp();
        private static readonly long HeapBaseline = GC.GetTotalMemory(false);

        public static double LastTickMs;
        public static long PostfixCalls;
        public static long PostfixTotalTicks;

        public static double HeapMb { get { return GC.GetTotalMemory(false) / 1048576.0; } }

        public static double TickPercentile(double p)
        {
            lock (TickMs)
            {
                double[] copy = new double[_tickCount];
                Array.Copy(TickMs, copy, _tickCount);
                return Percentile(copy, p);
            }
        }

        public static void AddTick(double ms)
        {
            LastTickMs = ms;
            lock (TickMs)
            {
                TickMs[_tickNext] = ms;
                _tickNext = (_tickNext + 1) % Capacity;
                if (_tickCount < Capacity) _tickCount++;
            }
        }

        public static void AddPostfix(long ticks)
        {
            Interlocked.Increment(ref PostfixCalls);
            Interlocked.Add(ref PostfixTotalTicks, ticks);
            lock (PostfixTicks)
            {
                PostfixTicks[_postNext] = ticks;
                _postNext = (_postNext + 1) % Capacity;
                if (_postCount < Capacity) _postCount++;
            }
        }

        public static object Snapshot()
        {
            double[] ticks;
            int tickN;
            lock (TickMs)
            {
                tickN = _tickCount;
                ticks = new double[tickN];
                Array.Copy(TickMs, ticks, tickN);
            }

            long[] post;
            int postN;
            lock (PostfixTicks)
            {
                postN = _postCount;
                post = new long[postN];
                Array.Copy(PostfixTicks, post, postN);
            }

            double[] postMs = new double[postN];
            for (int i = 0; i < postN; i++) postMs[i] = post[i] * 1000.0 / Stopwatch.Frequency;

            double elapsedSec = (Stopwatch.GetTimestamp() - StartTicks) / (double)Stopwatch.Frequency;
            long calls = Interlocked.Read(ref PostfixCalls);
            long totalTicks = Interlocked.Read(ref PostfixTotalTicks);
            long heapNow = GC.GetTotalMemory(false);

            long sumDelta = Interlocked.Read(ref BattleLogHook.TotalDelta);
            long captured = Interlocked.Read(ref BattleLogHook.Calls);
            double rate = sumDelta > 0 ? captured / (double)sumDelta : (captured > 0 ? 1.0 : 0.0);

            return new
            {
                tick = new
                {
                    count = tickN,
                    p50Ms = Math.Round(Percentile(ticks, 0.50), 4),
                    p99Ms = Math.Round(Percentile(ticks, 0.99), 4),
                    maxMs = Math.Round(Max(ticks), 4)
                },
                postfix = new
                {
                    installed = BattleLogHook.Installed,
                    patchApplied = BattleLogHook.PatchApplied,
                    calls = calls,
                    callsPerSec = elapsedSec > 0 ? Math.Round(calls / elapsedSec, 3) : 0,
                    meanNs = calls > 0 ? Math.Round(totalTicks * 1e9 / Stopwatch.Frequency / calls, 1) : 0,
                    p99Ns = Math.Round(Percentile(postMs, 0.99) * 1e6, 1),
                    gaps = Interlocked.Read(ref BattleLogHook.Gaps),
                    captured = captured,
                    sumInfoDelta = sumDelta,
                    captureRate = Math.Round(rate, 5),
                    patchSummary = BattleLogHook.PatchSummary
                },
                heap = new
                {
                    nowMb = Math.Round(heapNow / 1048576.0, 2),
                    baselineMb = Math.Round(HeapBaseline / 1048576.0, 2),
                    growthMb = Math.Round((heapNow - HeapBaseline) / 1048576.0, 2)
                },
                rings = new
                {
                    log = new { count = BattleLogHook.Log.Count, capacity = BattleLogHook.Log.Capacity },
                    battle = new { count = BattleSampler.BattleCount, capacity = BattleSampler.BattleCapacity }
                },
                battle = new
                {
                    inBattle = BattleSampler.InBattle,
                    started = BattleSampler.BattlesStarted,
                    ended = BattleSampler.BattlesEnded,
                    maxUnitsPerSide = BattleSampler.MaxUnitsPerSide,
                    lastFrameUnits = BattleSampler.LastFrameUnitCount
                },
                capture = new
                {
                    active = CaptureWriter.Active,
                    enabled = CoopPrefs.Capture.Value,
                    dir = CaptureWriter.Dir,
                    written = Interlocked.Read(ref CaptureWriter.Written),
                    dropped = Interlocked.Read(ref CaptureWriter.Dropped)
                },
                elapsedSec = Math.Round(elapsedSec, 1)
            };
        }

        private static double Percentile(double[] values, double p)
        {
            if (values.Length == 0) return 0;
            double[] copy = (double[])values.Clone();
            Array.Sort(copy);
            int idx = (int)Math.Ceiling(p * copy.Length) - 1;
            if (idx < 0) idx = 0;
            return copy[idx];
        }

        private static double Max(double[] values)
        {
            double m = 0;
            for (int i = 0; i < values.Length; i++) if (values[i] > m) m = values[i];
            return m;
        }
    }
}
