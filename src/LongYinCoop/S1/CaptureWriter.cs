using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace LongYinCoop
{
    /// <summary>
    /// เขียน NDJSON ลง UserData/CoopCapture/&lt;ts&gt;/
    /// main thread serialize ทันที (ไม่ถือ wrapper ข้ามเฟรม) แล้วส่ง byte[] ให้ writer thread เขียน
    /// queue มีขอบเขตคงที่ — ถ้าเต็มจะ drop และนับ (ไม่โตไม่จำกัด)
    /// </summary>
    internal static class CaptureWriter
    {
        private const int QueueCapacity = 16384;
        private static readonly string[] Streams = { "world", "battle", "log", "marker" };

        private sealed class Item
        {
            public int Stream;
            public byte[] Bytes;
        }

        private static BlockingCollection<Item> _queue;
        private static Thread _thread;
        private static FileStream[] _files;
        private static string _dir;

        public static volatile bool Active;
        public static long Written;
        public static long Dropped;

        public static string Dir { get { return _dir; } }

        public static bool WantsCapture
        {
            get { return Active && CoopPrefs.Capture.Value; }
        }

        public static string Start()
        {
            if (Active) return _dir;

            try
            {
                string ts = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                _dir = Path.Combine(CoopPaths.GameRoot, "UserData", "CoopCapture", ts);
                Directory.CreateDirectory(_dir);

                _files = new FileStream[Streams.Length];
                for (int i = 0; i < Streams.Length; i++)
                {
                    _files[i] = new FileStream(Path.Combine(_dir, Streams[i] + ".ndjson"),
                                               FileMode.Create, FileAccess.Write, FileShare.Read, 16384);
                }

                string meta = JsonSerializer.Serialize(new
                {
                    startedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    pid = Environment.ProcessId,
                    stage = CoopMod.Stage,
                    modVersion = CoopMod.Version,
                    streams = Streams,
                    worldHz = CoopPrefs.WorldHz.Value,
                    worldStreamHz = CoopPrefs.WorldStreamHz.Value,
                    worldStreamNote = "no npcs; full snapshot via IPC 'snapshot world'"
                }, S1Json.Options);
                File.WriteAllText(Path.Combine(_dir, "meta.json"), meta);

                _queue = new BlockingCollection<Item>(QueueCapacity);
                Active = true;
                _thread = new Thread(Loop);
                _thread.IsBackground = true;
                _thread.Name = "LongYinCoop.Capture";
                _thread.Start();

                CoopLog.Msg(string.Format(UIStrings.CaptureStarted, _dir));
                return _dir;
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                CoopLog.Warn(string.Format(UIStrings.CaptureStartFailed, e.Message));
                Active = false;
                return null;
            }
        }

        public static void Stop()
        {
            if (!Active) return;
            Active = false;
            try
            {
                if (_queue != null) _queue.CompleteAdding();
                if (_thread != null) _thread.Join(3000);
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
            finally
            {
                CloseFiles();
                _queue = null;
                _thread = null;
                CoopLog.Msg(string.Format(UIStrings.CaptureStopped, Interlocked.Read(ref Written),
                                           Interlocked.Read(ref Dropped)));
            }
        }

        public static void Record(string stream, object payload)
        {
            if (!WantsCapture) return;

            BlockingCollection<Item> queue = _queue;
            if (queue == null) return;

            try
            {
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload, S1Json.Options);
                Item item = new Item();
                item.Stream = StreamId(stream);
                item.Bytes = bytes;
                if (!queue.TryAdd(item))
                {
                    Interlocked.Increment(ref Dropped);
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
        }

        private static void Loop()
        {
            int sinceFlush = 0;
            try
            {
                foreach (Item item in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        FileStream fs = _files[item.Stream];
                        fs.Write(item.Bytes, 0, item.Bytes.Length);
                        fs.WriteByte((byte)'\n');
                        Interlocked.Increment(ref Written);

                        sinceFlush++;
                        if (sinceFlush >= 50)
                        {
                            sinceFlush = 0;
                            for (int i = 0; i < _files.Length; i++) _files[i].Flush();
                        }
                    }
                    catch (Exception e)
                    {
                        CoopLog.Error(e);
                    }
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
            finally
            {
                CloseFiles();
            }
        }

        private static int StreamId(string name)
        {
            for (int i = 0; i < Streams.Length; i++)
            {
                if (Streams[i] == name) return i;
            }
            return Streams.Length - 1;
        }

        private static void CloseFiles()
        {
            FileStream[] files = _files;
            _files = null;
            if (files == null) return;
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    if (files[i] == null) continue;
                    files[i].Flush();
                    files[i].Dispose();
                }
                catch { }
            }
        }
    }
}
