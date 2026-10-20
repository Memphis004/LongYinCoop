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
        public static MelonPreferences_Entry<bool> DevCommands;
        // S1.5: declared ตามแผน §10 แต่ S1.5-b ไม่ใช้ — `spike status` เป็น read-only จึงไม่ต้อง arm
        // (คำสั่งเปลี่ยน state ใน S1.5-d ขึ้นไปจะต้องมี pref นี้ + armed)
        public static MelonPreferences_Entry<bool> AllowRemoteInput;
        // S1.5-c W1: probe hook อ่านอย่างเดียว (default false = ไม่ติด patch เลย)
        public static MelonPreferences_Entry<bool> JitProbe;
        public static MelonPreferences_Entry<bool> Capture;
        public static MelonPreferences_Entry<int> WorldHz;
        public static MelonPreferences_Entry<int> WorldStreamHz;

        public static void Init()
        {
            Category = MelonPreferences.CreateCategory("LongYinCoop");
            // kill switch: false = mod ไม่ทำอะไรเลย (ไม่ติด probe ไม่เปิด IPC ไม่ drain queue)
            Enabled = Category.CreateEntry("Enabled", false);
            // Harmony probe ชั่วคราวบน GameController.Update — วัด thread เท่านั้น, ถอดได้ด้วย pref นี้
            Probe = Category.CreateEntry("Probe", true);
            // named pipe IPC อ่านอย่างเดียว
            Ipc = Category.CreateEntry("Ipc", true);
            // dev-only: bench_dispatch (ไม่แตะ state เกม) — ปิดได้ใน build ปกติ
            DevCommands = Category.CreateEntry("DevCommands", true);
            // S1.5 panic switch: false = คำสั่งเปลี่ยน state ทั้งหมดต้องตอบ DISARMED (default false)
            AllowRemoteInput = Category.CreateEntry("AllowRemoteInput", false);
            // S1.5-c W1: postfix อ่าน argument ของ OnClick/BattleGridClicked — false = ไม่ติด hook
            JitProbe = Category.CreateEntry("JitProbe", false);
            // S1 kill switch: false = hook ยังติดแต่ไม่บันทึก event ใหม่ (เกมเดินต่อ)
            Capture = Category.CreateEntry("Capture", true);
            // world snapshot rate (10 Hz ตามแผน) — ใช้ทั้ง IPC และ sampling
            WorldHz = Category.CreateEntry("WorldHz", 10);
            // อัตราเขียน world stream ลงดิสก์ (ตัด npcs ออก; snapshot เต็มขอผ่าน IPC)
            WorldStreamHz = Category.CreateEntry("WorldStreamHz", 2);
        }
    }
}
