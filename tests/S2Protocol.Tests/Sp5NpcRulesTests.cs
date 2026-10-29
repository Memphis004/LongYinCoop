using System;
using LongYinCoop.S2;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// SP5-NPC N2 tests (แผน plans/sp5-npc-reduce-plan-draft.md §3/§5 S1/S2 + negative tests) —
    /// pure rules ของ gate: กฎย่อ heroNum (G1), กฎ clamp เพดาน (G4), เงื่อนไข deny (G5),
    /// การ activate ต่อระดับ และ pipeline Decide→activate (hook shells เป็น Il2Cpp ไม่ลิงก์เทสต์ —
    /// ครอบด้วย build + static check + code audit ตาม idiom S2WriteGuardHook)
    /// </summary>
    internal static class Sp5NpcRulesTests
    {
        private static int _pass;
        private static int _fail;

        private static void Check(string name, bool ok)
        {
            Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] (npcreg) {name}");
            if (ok) _pass++; else _fail++;
        }

        public static int Run()
        {
            _pass = 0;
            _fail = 0;
            Console.WriteLine();
            Console.WriteLine("== SP5-NPC gate rules (N2 — pure) ==");

            RunShrink();
            RunClamp();
            RunActivation();
            RunPipeline();

            Console.WriteLine($"SP5-NPC rules tests: {_pass} PASS / {_fail} FAIL");
            return _fail;
        }

        // ---- G1: ShrinkHeroNum — round half-up, fail-closed ----
        private static void RunShrink()
        {
            // scale 0.5 (Light) — ค่าที่พบจริงในตาราง heroNum ของรอบ T0 (1..6)
            Check("Shrink 0.5: 4→2 · 5→3 (2.5 half-up) · 3→2 (1.5 half-up) · 2→1 · 1→1 (คง band เล็ก)",
                  R(4, 0.5) == 2 && R(5, 0.5) == 3 && R(3, 0.5) == 2 && R(2, 0.5) == 1 && R(1, 0.5) == 1);
            Check("Shrink 0.5: 6→3 · 0→0", R(6, 0.5) == 3 && R(0, 0.5) == 0);
            // scale 0.25 (Mid)
            Check("Shrink 0.25: 4→1 · 6→2 (1.5 half-up) · 5→1 (1.25→1) · 3→1 (0.75 half-up) · 1→0 (0.25→0)",
                  R(4, 0.25) == 1 && R(6, 0.25) == 2 && R(5, 0.25) == 1 && R(3, 0.25) == 1 && R(1, 0.25) == 0);
            // scale 0.05 (Strong)
            Check("Shrink 0.05: ทุกค่า 1..6 → 1 (round 0.05–0.3 → 0 ปัดเป็น 0? — 0.05×6=0.3→0)",
                  R(6, 0.05) == 0 && R(4, 0.05) == 0 && R(20, 0.05) == 1);
            Check("Shrink fail-closed: ค่าลบ→0 · scale ≥1 = คงเดิม · scale 0 → 0 · scale แปลก (NaN) → 0",
                  R(-3, 0.5) == 0 && R(4, 1.0) == 4 && R(4, 1.5) == 4 && R(4, 0.0) == 0 && R(4, double.NaN) == 0);
            Check("Shrink: ตาราง heroNum รอบ T0 ย่อ 0.5 ทั้ง 4 แถว → 88→50 (round half-up เอ่อเหนือ linear 44 = พารามิเตอร์ที่ T4 ต้องวัด)",
                  ShrinkTableSum() == 50);
        }

        // ---- G4: ClampCeiling — ห้ามเพิ่มเหนือของเดิม ----
        private static void RunClamp()
        {
            Check("Clamp 0.5: 900→450 · 1200+25y ที่ 1225→613 (612.5 half-up) · 901→451",
                  C(900, 0.5) == 450 && C(1225, 0.5) == 613 && C(901, 0.5) == 451);
            Check("Clamp 0.5: ค่า 1→1 · 0→0 · ค่าลบ→คงเดิม (fail-closed)",
                  C(1, 0.5) == 1 && C(0, 0.5) == 0 && C(-5, 0.5) == -5);
            Check("Clamp fail-closed: scale ≥1 = คงเดิม · scale 0 → 0 · ผลไม่ข้ามของเดิม",
                  C(900, 1.0) == 900 && C(900, 0.0) == 0 && C(3, 0.5) == 2);
        }

        // ---- เงื่อนไข activate ต่อระดับ (design §4 ตาราง L1/L2/L3) ----
        private static void RunActivation()
        {
            Check("ShrinkActive: Light/Mid/Strong = true · Off = false",
                  A(Sp5NpcLevel.Light) && A(Sp5NpcLevel.Mid) && A(Sp5NpcLevel.Strong) && !A(Sp5NpcLevel.Off));
            Check("ClampCeilingActive: เฉพาะ Mid (G4)", CA(Sp5NpcLevel.Mid) && !CA(Sp5NpcLevel.Light) && !CA(Sp5NpcLevel.Strong) && !CA(Sp5NpcLevel.Off));
            Check("DenyRefillActive: เฉพาะ Strong (G5)", D(Sp5NpcLevel.Strong) && !D(Sp5NpcLevel.Light) && !D(Sp5NpcLevel.Mid) && !D(Sp5NpcLevel.Off));
            Check("HeroScaleFor: Light 0.5 · Mid 0.25 · Strong 0.05 · Off 1.0 (ไม่เปลี่ยน)",
                  S(Sp5NpcLevel.Light) == 0.5 && S(Sp5NpcLevel.Mid) == 0.25 && S(Sp5NpcLevel.Strong) == 0.05 && S(Sp5NpcLevel.Off) == 1.0);
            Check("ตัวคูณ = ค่าที่ประกาศใน Sp5NpcScales (คงที่ — แก้ได้ก่อน T4 เท่านั้น)",
                  Sp5NpcScales.LightHeroScale == 0.5 && Sp5NpcScales.MidHeroScale == 0.25 &&
                  Sp5NpcScales.StrongHeroScale == 0.05 && Sp5NpcScales.MidCeilingScale == 0.5);
        }

        // ---- pipeline: snapshot → Decide → activate (แบบเดียวกับที่ hook shells เรียก) ----
        private static void RunPipeline()
        {
            // net.cfg จริงปัจจุบัน: enabled=false + ไม่มี npcPolicy ⇒ ทุก gate ปิด = เกมเดิม 100%
            Sp5NpcSnapshot real = new Sp5NpcSnapshot(false, "off", true, "off");
            Sp5NpcLevel lv = Sp5NpcPolicy.Decide(real.Enabled, real.Role, real.FormatOk, real.NpcPolicy);
            Check("pipeline: net.cfg จริงปัจจุบัน ⇒ Off ⇒ ไม่ shrink/ไม่ clamp/ไม่ deny",
                  lv == Sp5NpcLevel.Off && !A(lv) && !CA(lv) && !D(lv));

            // host + light: shrink เท่านั้น
            Sp5NpcSnapshot light = new Sp5NpcSnapshot(true, "host", true, "light");
            lv = Sp5NpcPolicy.Decide(light.Enabled, light.Role, light.FormatOk, light.NpcPolicy);
            Check("pipeline: host+light ⇒ shrink เฉพาะ G1 (ไม่ clamp ไม่ deny)",
                  lv == Sp5NpcLevel.Light && A(lv) && !CA(lv) && !D(lv));

            // host + mid: shrink + clamp
            Sp5NpcSnapshot mid = new Sp5NpcSnapshot(true, "host", true, "mid");
            lv = Sp5NpcPolicy.Decide(mid.Enabled, mid.Role, mid.FormatOk, mid.NpcPolicy);
            Check("pipeline: host+mid ⇒ G1 + G4 (ไม่ deny)",
                  lv == Sp5NpcLevel.Mid && A(lv) && CA(lv) && !D(lv));

            // host + strong: shrink + deny (ไม่ clamp)
            Sp5NpcSnapshot strong = new Sp5NpcSnapshot(true, "host", true, "strong");
            lv = Sp5NpcPolicy.Decide(strong.Enabled, strong.Role, strong.FormatOk, strong.NpcPolicy);
            Check("pipeline: host+strong ⇒ G1 + G5 (ไม่ clamp)",
                  lv == Sp5NpcLevel.Strong && A(lv) && !CA(lv) && D(lv));

            // guest เปิด policy = ไม่ทำงาน (design: opt-in สองชั้น — เฉพาะ host)
            Sp5NpcSnapshot guest = new Sp5NpcSnapshot(true, "guest", true, "strong");
            lv = Sp5NpcPolicy.Decide(guest.Enabled, guest.Role, guest.FormatOk, guest.NpcPolicy);
            Check("pipeline: guest+strong ⇒ Off (ห้ามลดบนเครื่อง guest)",
                  lv == Sp5NpcLevel.Off && !A(lv) && !D(lv));

            // ไฟล์เสีย ⇒ Off แม้ทุกอย่างอื่นถูก
            Sp5NpcSnapshot broken = new Sp5NpcSnapshot(true, "host", false, "light");
            lv = Sp5NpcPolicy.Decide(broken.Enabled, broken.Role, broken.FormatOk, broken.NpcPolicy);
            Check("pipeline: ไฟล์เสีย ⇒ Off (fail-closed)", lv == Sp5NpcLevel.Off);
        }

        private static int R(int old, double scale) { return Sp5NpcRules.ShrinkHeroNum(old, scale); }
        private static int C(int orig, double scale) { return Sp5NpcRules.ClampCeiling(orig, scale); }
        private static bool A(Sp5NpcLevel l) { return Sp5NpcRules.ShrinkActive(l); }
        private static bool CA(Sp5NpcLevel l) { return Sp5NpcRules.ClampCeilingActive(l); }
        private static bool D(Sp5NpcLevel l) { return Sp5NpcRules.DenyRefillActive(l); }
        private static double S(Sp5NpcLevel l) { return Sp5NpcRules.HeroScaleFor(l); }

        private static int ShrinkTableSum()
        {
            int[][] table = { new[] { 4, 4, 3, 3, 2, 1 }, new[] { 5, 4, 4, 3, 3, 2 },
                              new[] { 5, 5, 4, 4, 3, 2 }, new[] { 6, 5, 5, 4, 4, 3 } };
            int sum = 0;
            foreach (int[] row in table)
                foreach (int v in row)
                    sum += Sp5NpcRules.ShrinkHeroNum(v, 0.5);
            return sum;
        }
    }
}
