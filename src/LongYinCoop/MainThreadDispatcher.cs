using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace LongYinCoop
{
    /// <summary>
    /// คิวเดียวสำหรับทุกงานที่แตะ IL2CPP — drain ใน MelonEvents.OnUpdate เท่านั้น
    /// network/IPC thread ห้ามเรียก API เกมตรง ๆ ให้ใช้ Invoke()
    /// </summary>
    internal sealed class MainThreadDispatcher
    {
        private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
        private readonly object _sampleLock = new object();
        private readonly double[] _samples = new double[512];
        private int _sampleCount;
        private int _sampleNext;

        public long Queued;
        public long Ran;
        public long Deferred;
        public long Errors;

        /// <summary>ManagedThreadId ของ thread ที่เรียก Tick() — ใช้เทียบกับ thread ของ Harmony probe</summary>
        public int OnUpdateThreadId;

        public int QueuedCount { get { return _queue.Count; } }

        public void Enqueue(Action action)
        {
            if (action == null) return;
            _queue.Enqueue(action);
            Interlocked.Increment(ref Queued);
        }

        /// <summary>drain แบบมี budget ต่อเฟรม; งานที่เหลือค้างไว้ให้เฟรมถัดไป</summary>
        public void Tick(double budgetMs)
        {
            OnUpdateThreadId = Environment.CurrentManagedThreadId;

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < budgetMs)
            {
                Action action;
                if (!_queue.TryDequeue(out action)) break;
                try
                {
                    action();
                    Interlocked.Increment(ref Ran);
                }
                catch (Exception e)
                {
                    Interlocked.Increment(ref Errors);
                    CoopLog.Error(e);
                }
            }
            sw.Stop();

            if (!_queue.IsEmpty) Interlocked.Increment(ref Deferred);
            AddSample(sw.Elapsed.TotalMilliseconds);
        }

        /// <summary>เรียกจาก thread อื่น: enqueue แล้วรอผลบน main thread</summary>
        public bool Invoke<T>(Func<T> func, int timeoutMs, out T result)
        {
            T local = default(T);
            Exception error = null;
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                Enqueue(delegate
                {
                    try { local = func(); }
                    catch (Exception e) { error = e; }
                    finally { done.Set(); }
                });

                if (!done.Wait(timeoutMs))
                {
                    result = default(T);
                    return false;
                }
            }

            if (error != null)
            {
                CoopLog.Error(error);
                result = default(T);
                return false;
            }

            result = local;
            return true;
        }

        private void AddSample(double ms)
        {
            lock (_sampleLock)
            {
                _samples[_sampleNext] = ms;
                _sampleNext = (_sampleNext + 1) % _samples.Length;
                if (_sampleCount < _samples.Length) _sampleCount++;
            }
        }

        public double Percentile(double p)
        {
            double[] copy;
            lock (_sampleLock)
            {
                copy = new double[_sampleCount];
                Array.Copy(_samples, copy, _sampleCount);
            }
            if (copy.Length == 0) return 0;
            Array.Sort(copy);
            int idx = (int)Math.Ceiling(p * copy.Length) - 1;
            if (idx < 0) idx = 0;
            return copy[idx];
        }

        public double MaxDrainMs
        {
            get
            {
                double m = 0;
                lock (_sampleLock)
                {
                    for (int i = 0; i < _sampleCount; i++) if (_samples[i] > m) m = _samples[i];
                }
                return m;
            }
        }

        public object Snapshot()
        {
            return new
            {
                onUpdateThreadId = OnUpdateThreadId,
                queued = Interlocked.Read(ref Queued),
                ran = Interlocked.Read(ref Ran),
                deferred = Interlocked.Read(ref Deferred),
                errors = Interlocked.Read(ref Errors),
                pending = _queue.Count,
                p50Ms = Math.Round(Percentile(0.50), 4),
                p99Ms = Math.Round(Percentile(0.99), 4),
                maxMs = Math.Round(MaxDrainMs, 4),
                samples = _sampleCount
            };
        }
    }
}
