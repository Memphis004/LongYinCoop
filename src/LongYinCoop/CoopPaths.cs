using System;
using System.IO;

namespace LongYinCoop
{
    internal static class CoopPaths
    {
        public static string GameRoot
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        public static string UserDataDir
        {
            get { return Path.Combine(GameRoot, "UserData", "LongYinCoop"); }
        }

        public static string EnsureUserDataDir()
        {
            string dir = UserDataDir;
            try { Directory.CreateDirectory(dir); } catch { }
            return dir;
        }
    }
}
