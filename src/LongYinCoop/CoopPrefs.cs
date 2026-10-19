using MelonLoader;

namespace LongYinCoop
{
    /// <summary>pref ทั้งหมดห่อไว้ชั้นเดียว (กัน ML API เปลี่ยน) — หมวด [LongYinCoop]</summary>
    internal static class CoopPrefs
    {
        public static MelonPreferences_Category Category;
        public static MelonPreferences_Entry<bool> Enabled;
        public static MelonPreferences_Entry<bool> Probe;
        public static MelonPreferences_Entry<bool> Ipc;

        public static void Init()
        {
            Category = MelonPreferences.CreateCategory("LongYinCoop");
            // kill switch: false = mod ไม่ทำอะไรเลย (ไม่ติด probe ไม่เปิด IPC ไม่ drain queue)
            Enabled = Category.CreateEntry("Enabled", false);
            // Harmony probe ชั่วคราวบน GameController.Update — วัด thread เท่านั้น, ถอดได้ด้วย pref นี้
            Probe = Category.CreateEntry("Probe", true);
            // named pipe IPC อ่านอย่างเดียว
            Ipc = Category.CreateEntry("Ipc", true);
        }
    }
}
