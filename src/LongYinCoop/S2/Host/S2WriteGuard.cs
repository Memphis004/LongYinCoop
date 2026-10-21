using System.Text;

namespace LongYinCoop.S2.Host
{
    /// <summary>
    /// S2a-2 rev4: ตรรกะ "write path ปิดอยู่" แบบ **pure** (ไม่มี IL2CPP/Unity/pref I/O, ไม่มี side effect)
    ///
    /// มติผู้ใช้ 2026-10-21 (ก): **`DevCommands` ไม่ใช่ส่วนหนึ่งของการตัดสิน**
    /// (`spike status` = เครื่องมือวัดของ exit 4 ต้อง `Enabled+Ipc+DevCommands`; IpcServer.cs:333-335)
    /// และคำสั่งที่ **เขียน state เกม** (`spike arm/move/attack`) ถูกกันด้วย `allowRemoteInput` อีกชั้น
    /// (IpcServer.cs:344/355/381 — ตาราง audit §8 ของ `S2a-2-plan.md` rev4)
    ///
    /// ปฏิเสธ (false) ถ้า pref ตัวใดตัวหนึ่งต่อไปนี้เป็น true:
    ///   `allowRemoteInput` — เปิดทางรับ input จาก IPC เข้าเกม
    ///   `jitProbe` `jitProbeW2` `jitProbeW4` `jitProbeW6` `jitProbeW7` — Harmony patch probes ของ S1.5
    ///     (เปิดไว้จะปนการวัด frame time ของ exit 3)
    /// ⇒ ผ่าน (true) เมื่อ **ทั้ง 6 ตัวเป็น false** เท่านั้น
    /// </summary>
    public static class S2WriteGuard
    {
        /// <summary>true = ไม่มี write path เปิดอยู่ (ทั้ง 6 pref ปิด) — pure, ไม่แตะ pref เอง</summary>
        public static bool WritePathsClosed(bool allowRemoteInput, bool jitProbe, bool jitProbeW2,
                                            bool jitProbeW4, bool jitProbeW6, bool jitProbeW7)
        {
            return !allowRemoteInput && !jitProbe && !jitProbeW2 && !jitProbeW4 && !jitProbeW6 && !jitProbeW7;
        }

        /// <summary>รายชื่อ pref ที่เปิดอยู่ (true) สำหรับข้อความเตือน/idle — ไม่มีตัวไหนเปิด = "none"</summary>
        public static string Describe(bool allowRemoteInput, bool jitProbe, bool jitProbeW2,
                                      bool jitProbeW4, bool jitProbeW6, bool jitProbeW7)
        {
            StringBuilder sb = new StringBuilder(72);
            Append(sb, "AllowRemoteInput", allowRemoteInput);
            Append(sb, "JitProbe", jitProbe);
            Append(sb, "JitProbeW2", jitProbeW2);
            Append(sb, "JitProbeW4", jitProbeW4);
            Append(sb, "JitProbeW6", jitProbeW6);
            Append(sb, "JitProbeW7", jitProbeW7);
            return sb.Length == 0 ? "none" : sb.ToString();
        }

        private static void Append(StringBuilder sb, string name, bool on)
        {
            if (!on) return;
            if (sb.Length > 0) sb.Append('+');
            sb.Append(name);
        }
    }
}
