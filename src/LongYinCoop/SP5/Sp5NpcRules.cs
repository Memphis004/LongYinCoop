using System;

namespace LongYinCoop
{
    /// <summary>
    /// ตัวคูณระดับของ SP5-NPC gate (แผน plans/sp5-npc-reduce-plan-draft.md §4 — ค่าเสนอจาก design
    /// SP5design §4 ตาราง L1/L2/L3) — **ค่า implement จริงและกฎ rounding เป็นพารามิเตอร์ที่ยัง
    /// ไม่ผ่านการวัดจริง (T4)** · ผลลดจริงระดับใดต้องรอ runtime test ก่อนอ้าง
    /// </summary>
    internal static class Sp5NpcScales
    {
        public const double LightHeroScale = 0.5;   // L1: heroScale ≈ 0.5
        public const double MidHeroScale = 0.25;    // L2: heroScale ≈ 0.25
        public const double StrongHeroScale = 0.05; // L3: heroScale ≈ 0.05
        public const double MidCeilingScale = 0.5;  // L2 (C2): เพดานเติม ≈ ครึ่ง (design §4)
    }

    /// <summary>
    /// กฎคำนวณของ gate SP5-NPC — **pure ล้วน** (เทสต์ offline ได้) · hook shells ใน Sp5NpcGate.cs
    /// เรียกฟังก์ชันนี้เท่านั้น ห้ามมีตรรกะซ้ำใน shell · ทุกกฎ fail-closed: ค่า/เงื่อนไขแปลก
    /// ⇒ คืนค่าที่ไม่เปลี่ยนพฤติกรรมเกม (เกมเดิม)
    /// </summary>
    internal static class Sp5NpcRules
    {
        /// <summary>กฎ rounding ของการย่อ — **round half-up (AwayFromZero)**: ค่า band 1 ที่
        /// scale 0.5 ยังคงเป็น 1 (ไม่ zero ทั้ง band) · ยังไม่ผ่านการวัด — เปลี่ยนได้ก่อน T4</summary>
        internal static int ShrinkHeroNum(int oldValue, double scale)
        {
            if (oldValue <= 0) return 0;
            if (scale >= 1.0) return oldValue;
            if (scale <= 0.0) return 0;
            int v = (int)Math.Round(oldValue * scale, MidpointRounding.AwayFromZero);
            return v < 0 ? 0 : v;
        }

        /// <summary>G4 (C2): เพดานใหม่ = round(old × ceilingScale) — **ห้ามเพิ่มเหนือค่าเดิม**
        /// (fail-closed: ค่าแปลก ⇒ คืนของเดิม)</summary>
        internal static int ClampCeiling(int originalValue, double ceilingScale)
        {
            if (originalValue <= 0) return originalValue;
            if (ceilingScale >= 1.0) return originalValue;
            if (ceilingScale <= 0.0) return 0;
            int v = (int)Math.Round(originalValue * ceilingScale, MidpointRounding.AwayFromZero);
            if (v < 0) return originalValue;
            return v > originalValue ? originalValue : v;
        }

        /// <summary>G1 active เมื่อ level ใดก็ตามที่ไม่ใช่ Off (ทุกระดับย่อ heroNum ต่างกันด้วย scale)</summary>
        internal static bool ShrinkActive(Sp5NpcLevel level) { return level != Sp5NpcLevel.Off; }

        /// <summary>G4 (C2) active เฉพาะ Mid (design §4: L1 คงเพดานเดิม, L3 ใช้ deny แทน)</summary>
        internal static bool ClampCeilingActive(Sp5NpcLevel level) { return level == Sp5NpcLevel.Mid; }

        /// <summary>G5 (C1) active เฉพาะ Strong (deny ที่ WorldAddNewHero — auto-quit ทำงานต่อ)</summary>
        internal static bool DenyRefillActive(Sp5NpcLevel level) { return level == Sp5NpcLevel.Strong; }

        /// <summary>ตัวคูณ heroNum ของแต่ละระดับ — Off คืน 1.0 (ไม่เปลี่ยน)</summary>
        internal static double HeroScaleFor(Sp5NpcLevel level)
        {
            switch (level)
            {
                case Sp5NpcLevel.Light: return Sp5NpcScales.LightHeroScale;
                case Sp5NpcLevel.Mid: return Sp5NpcScales.MidHeroScale;
                case Sp5NpcLevel.Strong: return Sp5NpcScales.StrongHeroScale;
                default: return 1.0;
            }
        }
    }
}
