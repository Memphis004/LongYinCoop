using System;
using System.Globalization;

namespace LongYinCoop.S2.Host
{
    /// <summary>
    /// W5 (แผน S2a-4 §7): gate ของคำสั่ง `set_pref` ผ่าน IPC — ปิดช่อง K9/K10
    ///
    /// กติกา: ขณะ `S2HostService.Active == true` **ปฏิเสธ** คำสั่งที่ตั้ง write-path pref เป็น true
    /// (`allowRemoteInput`, `jitProbe`, `jitProbeW2/W4/W6/W7` — ชุดเดียวกับ `S2WriteGuard`)
    /// · การ**ปิด** (false) อนุญาตทุกสถานะ
    /// · pref นอกชุด (เช่น `capture`) ไม่ถูก gate นี้บังคับ
    ///
    /// pure: ไม่แตะ pref จริง/IPC — `IpcServer.SetPref` เป็นผู้เรียกแล้วตอบ error เอง
    /// (ผลข้างเคียงเดียวที่ยอมให้มีในฝั่งผู้เรียก: log + ตัวนับ `IpcServer.SetPrefRejections` เป็น evidence)
    /// </summary>
    public static class S2SetPrefGate
    {
        /// <summary>write-path pref ทั้ง 6 ตัว (เทียบแบบ lower-case — IPC ใช้หลายรูปการสะกด)</summary>
        public static readonly string[] WritePathKeys =
        {
            "allowremoteinput",
            "jitprobe",
            "jitprobew2",
            "jitprobew4",
            "jitprobew6",
            "jitprobew7",
        };

        public const string RejectErrorArgs =
            "rejected: cannot set {0}=true while S2 is active (write-path pref stays unchanged; setting false is always allowed)";

        public static bool IsWritePathKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            string normalized = key.ToLowerInvariant();
            return Array.IndexOf(WritePathKeys, normalized) >= 0;
        }

        /// <summary>true = ต้องปฏิเสธคำสั่งนี้ (active + เป็น write-path key + ตั้งเป็น true)</summary>
        public static bool ShouldReject(bool s2Active, string key, bool value)
        {
            return s2Active && value && IsWritePathKey(key);
        }

        public static string RejectError(string key)
        {
            return string.Format(CultureInfo.InvariantCulture, RejectErrorArgs, key);
        }
    }
}
