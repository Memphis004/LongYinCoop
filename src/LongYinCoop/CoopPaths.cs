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

        /// <summary>S2: อ่านค่า coop.net.* จากไฟล์นี้ตรง ๆ เพื่อสลับ pref กลางรันได้ (D2-i)</summary>
        public static string MelonPreferencesPath
        {
            get { return Path.Combine(GameRoot, "UserData", "MelonPreferences.cfg"); }
        }

        public static string S2EvidenceDir
        {
            get { return Path.Combine(UserDataDir, "S2"); }
        }

        public static string EnsureUserDataDir()
        {
            string dir = UserDataDir;
            try { Directory.CreateDirectory(dir); } catch { }
            return dir;
        }
    }
}
