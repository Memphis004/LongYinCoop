using System;

namespace LongYinCoop
{
    /// <summary>ระดับการลด NPC เริ่มโลก (แผน plans/sp5-npc-reduce-plan-draft.md §4 — ค่าตาม policy ใน net.cfg)</summary>
    internal enum Sp5NpcLevel
    {
        Off = 0,
        Light = 1,
        Mid = 2,
        Strong = 3,
    }

    /// <summary>
    /// SP5-NPC gate predicate (design §5.3 — pure ล้วน ไม่แตะ state เกม ไม่มี Unity/Il2Cpp)
    ///
    /// fail-closed ทุกชุด: ไม่ enabled / ไม่ใช่ host / ไฟล์ net.cfg เสีย / ค่า policy ไม่รู้จัก
    /// ⇒ Off = เกมเดิม 100% (hook ใด ๆ ที่จะมาใน stage N2 ตัดสินจากฟังก์ชันนี้เท่านั้น)
    /// · opt-in สองชั้น: co-op enabled + `npcPolicy` ตั้งชัด — net.cfg เดิมไม่มี key = Off
    /// </summary>
    internal static class Sp5NpcPolicy
    {
        /// <summary>
        /// แปลงค่า key เป็นระดับ — ตัวพิมพ์ผสมได้ · ว่าง/null/ค่าไม่รู้จัก (รวม off/none/0/false)
        /// = Off · **ห้ามโยน exception** (ค่าแปลกไม่ทำไฟล์พัง — แค่ไม่ลด)
        /// </summary>
        internal static Sp5NpcLevel ParseValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return Sp5NpcLevel.Off;
            switch (value.Trim().ToLowerInvariant())
            {
                case "light": return Sp5NpcLevel.Light;
                case "mid": return Sp5NpcLevel.Mid;
                case "strong": return Sp5NpcLevel.Strong;
                default: return Sp5NpcLevel.Off;
            }
        }

        /// <summary>
        /// predicate เดียวของ gate ทุกจุด (G1/G2/G4/G5 ในแผน §3) — truth-table เทสต์ได้
        /// (แนว S2WriteGuard.Decide): เปิดได้เฉพาะ enabled && role=host && ไฟล์ format ถูก
        /// และค่า policy แปลงเป็นระดับได้ — เงื่อนไขใดขาด = Off
        /// </summary>
        internal static Sp5NpcLevel Decide(bool enabled, string role, bool formatOk, string policyValue)
        {
            if (!enabled) return Sp5NpcLevel.Off;
            if (!formatOk) return Sp5NpcLevel.Off;
            if (!string.Equals(role, "host", StringComparison.OrdinalIgnoreCase)) return Sp5NpcLevel.Off;
            return ParseValue(policyValue);
        }
    }

    /// <summary>snapshot ของ net.cfg ที่ gate อ่านต่อ invocation (pure — inject ได้ สไตล์ provider)</summary>
    internal struct Sp5NpcSnapshot
    {
        public bool Enabled;
        public string Role;
        public bool FormatOk;
        public string NpcPolicy;

        public Sp5NpcSnapshot(bool enabled, string role, bool formatOk, string npcPolicy)
        {
            Enabled = enabled;
            Role = role;
            FormatOk = formatOk;
            NpcPolicy = npcPolicy;
        }
    }
}
