using System;
using LongYinCoop.S2;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// SP5-NPC N1-5 tests (แผน plans/sp5-npc-reduce-plan-draft.md §2 N1-5 + §5 S1/S2) —
    /// pure predicate/parser ไม่ใช้เกม: truth-table ของ Sp5NpcPolicy.Decide/ParseValue
    /// (fail-closed ครบทุกชุด แนว S2WriteGuard) + การต่อ key `npcPolicy` ใน S2NetConfig.Parse
    /// · ไม่มี code path ใดเขียนไฟล์ — Parse อ่าน string ล้วน
    /// </summary>
    internal static class Sp5NpcPolicyTests
    {
        private static int _pass;
        private static int _fail;

        private static void Check(string name, bool ok)
        {
            Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] (npc) {name}");
            if (ok) _pass++; else _fail++;
        }

        public static int Run()
        {
            _pass = 0;
            _fail = 0;
            Console.WriteLine();
            Console.WriteLine("== SP5-NPC policy predicate/parser (N1-5 — pure) ==");

            RunParseValue();
            RunDecideTruthTable();
            RunParserIntegration();

            Console.WriteLine($"SP5-NPC policy tests: {_pass} PASS / {_fail} FAIL");
            return _fail;
        }

        // ---- ParseValue: ค่ารู้จัก/ตัวพิมพ์ผสม/ค่าแปลก → fail-closed Off ----
        private static void RunParseValue()
        {
            Check("ParseValue: light/Light/LIGHT → Light",
                  P("light") == Sp5NpcLevel.Light && P("Light") == Sp5NpcLevel.Light && P("LIGHT") == Sp5NpcLevel.Light);
            Check("ParseValue: mid/Mid → Mid · strong/STRONG → Strong",
                  P("mid") == Sp5NpcLevel.Mid && P("Mid") == Sp5NpcLevel.Mid &&
                  P("strong") == Sp5NpcLevel.Strong && P("STRONG") == Sp5NpcLevel.Strong);
            Check("ParseValue: off/none/0/false → Off (fail-closed)",
                  P("off") == Sp5NpcLevel.Off && P("none") == Sp5NpcLevel.Off &&
                  P("0") == Sp5NpcLevel.Off && P("false") == Sp5NpcLevel.Off);
            Check("ParseValue: null/empty/whitespace → Off",
                  P(null) == Sp5NpcLevel.Off && P("") == Sp5NpcLevel.Off && P("   ") == Sp5NpcLevel.Off);
            Check("ParseValue: ค่าแปลก (typos/แปลกปลอม) → Off ไม่โยน",
                  P("Ligth") == Sp5NpcLevel.Off && P("light ") == Sp5NpcLevel.Light &&
                  P(" strong") == Sp5NpcLevel.Strong && P("hacker=true") == Sp5NpcLevel.Off &&
                  P("<script>") == Sp5NpcLevel.Off);
        }

        // ---- Decide: truth-table เต็ม 2×4×2×5 = 80 ชุด — เปิดได้เฉพาะชุดเดียวของเงื่อนไข ----
        private static void RunDecideTruthTable()
        {
            string[] roles = { "host", "guest", "off", null };
            string[] policies = { "light", "mid", "strong", "off", "unknown-value" };
            int okCount = 0, total = 0;
            foreach (bool enabled in new[] { false, true })
                foreach (string role in roles)
                    foreach (bool formatOk in new[] { false, true })
                        foreach (string pol in policies)
                        {
                            total++;
                            Sp5NpcLevel want;
                            if (!enabled || !formatOk || role != "host" || pol == "off" || pol == "unknown-value")
                                want = Sp5NpcLevel.Off;
                            else
                                want = pol == "light" ? Sp5NpcLevel.Light
                                     : pol == "mid" ? Sp5NpcLevel.Mid
                                     : Sp5NpcLevel.Strong;
                            if (D(enabled, role, formatOk, pol) == want) okCount++;
                            else Console.WriteLine($"  [FAIL] grid enabled={enabled} role={role} formatOk={formatOk} policy={pol}");
                        }
            Check($"Decide truth-table เต็ม {total} ชุด: fail-closed ครบ (เปิดเฉพาะ enabled+host+formatOk+policy รู้จัก)",
                  okCount == total && total == 80);

            Check("Decide: role ตัวพิมพ์ผสม 'Host' ยังนับเป็น host (OrdinalIgnoreCase)",
                  D(true, "Host", true, "light") == Sp5NpcLevel.Light);
            Check("Decide: net.cfg ปัจจุบันจริง (enabled=false + ไม่มี key) = Off",
                  D(false, "host", true, null) == Sp5NpcLevel.Off);
            Check("Decide: ไฟล์เสีย (FormatOk=false) = Off แม้ค่าทุกอย่างถูก",
                  D(true, "host", false, "strong") == Sp5NpcLevel.Off);
        }

        // ---- parser: ต่อ S2NetConfig.Parse เดิม — key ใหม่ไม่ทำไฟล์เดิมพัง ----
        private static void RunParserIntegration()
        {
            const string path = "test://npc";
            string host = "enabled = true\nrole = host\nsecret = 0123456789abcdef0123456789abcdef\n" +
                          "transport = loopback\nbind = 127.0.0.1\nport = 47800\n";

            S2NetSettings s = S2NetConfig.Parse(host, path, 0);
            Check("parse: net.cfg ไม่มี npcPolicy → default 'off' + IsValid เดิม",
                  s.NpcPolicy == "off" && s.IsValid && s.Enabled && s.Role == "host");

            s = S2NetConfig.Parse(host + "npcPolicy = Light\n", path, 0);
            Check("parse: npcPolicy=Light (ตัวพิมพ์ผสม) → เก็บ raw + IsValid คงเดิม + Decide=Light",
                  s.NpcPolicy == "Light" && s.IsValid &&
                  Sp5NpcPolicy.Decide(s.Enabled, s.Role, s.FormatOk, s.NpcPolicy) == Sp5NpcLevel.Light);

            s = S2NetConfig.Parse(host + "npcPolicy = strong\n", path, 0);
            Check("parse: npcPolicy=strong → Decide=Strong", Sp5NpcPolicy.Decide(s.Enabled, s.Role, s.FormatOk, s.NpcPolicy) == Sp5NpcLevel.Strong);

            s = S2NetConfig.Parse(host + "npcPolicy = Ligth\n", path, 0);
            Check("parse: ค่าแปลก → Decide=Off **และไฟล์ยัง valid ไม่พัง** (Error ว่าง)",
                  Sp5NpcPolicy.Decide(s.Enabled, s.Role, s.FormatOk, s.NpcPolicy) == Sp5NpcLevel.Off &&
                  s.IsValid && string.IsNullOrEmpty(s.Error));

            s = S2NetConfig.Parse(host + "npcPolicy = light\nnpcPolicy = off\n", path, 0);
            Check("parse: key ซ้ำ = ตัวหลังชนะ (สไตล์เดิม) → Decide=Off",
                  Sp5NpcPolicy.Decide(s.Enabled, s.Role, s.FormatOk, s.NpcPolicy) == Sp5NpcLevel.Off);

            s = S2NetConfig.Parse(host + "unknownKey = whatever\nnpcPolicy = mid\n", path, 0);
            Check("parse: key ไม่รู้จักถูกข้าม + npcPolicy ยังอ่านได้",
                  s.NpcPolicy == "mid" && Sp5NpcPolicy.Decide(s.Enabled, s.Role, s.FormatOk, s.NpcPolicy) == Sp5NpcLevel.Mid);

            s = S2NetConfig.Parse("npcPolicy = light\n[LongYinCoop]\nenabled = true\n", path, 0);
            Check("parse: [section] ยัง fail-closed เหมือนเดิม (FormatOk=false ⇒ Decide=Off แม้ npcPolicy=light)",
                  !s.FormatOk && Sp5NpcPolicy.Decide(s.Enabled, s.Role, s.FormatOk, s.NpcPolicy) == Sp5NpcLevel.Off);

            s = S2NetConfig.Parse("", path, 0);
            Check("parse: ไฟล์ว่าง → NpcPolicy default 'off' (Blank)",
                  s.NpcPolicy == "off");
        }

        private static Sp5NpcLevel P(string v) { return Sp5NpcPolicy.ParseValue(v); }

        private static Sp5NpcLevel D(bool enabled, string role, bool formatOk, string policyValue)
        {
            return Sp5NpcPolicy.Decide(enabled, role, formatOk, policyValue);
        }
    }
}
