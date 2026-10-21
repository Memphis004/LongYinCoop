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

        /// <summary>
        /// S2 (งาน A, มติ 2026-10-22): `coop.net.*` อยู่ในไฟล์ของเราเอง — **MelonLoader ไม่แตะไฟล์นี้**
        /// (ห้ามย้ายกลับไป MelonPreferences.cfg: ML เขียนทับและแปลง dotted key เป็นตารางซ้อนตอนออกเกม)
        /// </summary>
        public static string NetCfgPath
        {
            get { return Path.Combine(UserDataDir, S2.S2NetConfig.FileName); }
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
