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
        // S1.5-c W2: postfix ฝั่ง move (MoveButtonClicked/GenerateMovePath/CancelMove/RegretMove) — default false
        public static MelonPreferences_Entry<bool> JitProbeW2;
        // S1.5-c W4: postfix บน BattlePauseButtonClicked + อ่าน timeScale/battleTime — default false
        public static MelonPreferences_Entry<bool> JitProbeW4;
        // S1.5-c W6: prefix+postfix บน chain ของ move (state ก่อน/หลัง + ตัวตน GameObject + stack) — default false
        public static MelonPreferences_Entry<bool> JitProbeW6;
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
            // S1.5-c W2/W4: wave แยก pref (default false) — เปิด/ปิดระหว่างรันได้, unpatch ได้ทั้ง wave
            JitProbeW2 = Category.CreateEntry("JitProbeW2", false);
            JitProbeW4 = Category.CreateEntry("JitProbeW4", false);
            // S1.5-c W6: hook อ่าน chain ทั้ง 5 จุด พร้อม state ก่อน/หลัง — false = ไม่ติด patch
            JitProbeW6 = Category.CreateEntry("JitProbeW6", false);
            // S1 kill switch: false = hook ยังติดแต่ไม่บันทึก event ใหม่ (เกมเดินต่อ)
            Capture = Category.CreateEntry("Capture", true);
            // world snapshot rate (10 Hz ตามแผน) — ใช้ทั้ง IPC และ sampling
            WorldHz = Category.CreateEntry("WorldHz", 10);
            // อัตราเขียน world stream ลงดิสก์ (ตัด npcs ออก; snapshot เต็มขอผ่าน IPC)
            WorldStreamHz = Category.CreateEntry("WorldStreamHz", 2);
        }
    }
}
