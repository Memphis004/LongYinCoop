using System;
using System.IO;
using System.Text;

namespace LongYinCoop
{
    /// <summary>ไฟล์ log แยกของ mod: UserData/LongYinCoop/logs/coop-YYYYMMDD.log</summary>
    internal static class CoopLog
    {
        private static readonly object Sync = new object();
        private static string _path;

        public static string LogPath { get { return _path; } }

        public static void Init(string tag)
        {
            try
            {
                string root = AppDomain.CurrentDomain.BaseDirectory;
                string dir = Path.Combine(root, "UserData", "LongYinCoop", "logs");
                Directory.CreateDirectory(dir);
                _path = Path.Combine(dir, "coop-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                Write(UIStrings.LogLevelInfo, string.Format(UIStrings.LogStart, tag, Environment.ProcessId,
                             DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format(UIStrings.LogInitFailed, e));
            }
        }

        public static void Msg(string msg) { Write(UIStrings.LogLevelInfo, msg); }
        public static void Warn(string msg) { Write(UIStrings.LogLevelWarn, msg); }
        public static void Error(string msg) { Write(UIStrings.LogLevelError, msg); }
        public static void Error(Exception e) { Write(UIStrings.LogLevelError, e == null ? "null" : e.ToString()); }

        private static void Write(string level, string msg)
        {
            string line = DateTime.Now.ToString("HH:mm:ss.fff") + " [" + level + "] " + msg;

            try { MelonLoader.MelonLogger.Msg(msg); } catch { }

            string path = _path;
            if (path == null) return;
            lock (Sync)
            {
                try { File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8); }
                catch { }
            }
        }
    }
}
