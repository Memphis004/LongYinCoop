using System;
using System.Collections.Generic;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// N4-follow-up ร่าง 1 (มติผู้ใช้ 2026-10-30 อนุมัติตามร่าง) — เทสต์ pure ของกติกา match
    /// ชื่อ T2 ข้อ loveable (Sp5T0NameMatch) — ไม่ใช้ Il2Cpp/Unity:
    /// ชื่อใน list = present ก็ต่อเมื่อ heroName ตรง หรือ heroFamilyName+heroName ตรง (Ordinal)
    /// · ชื่อที่ match ไม่ได้ต้องถูก log ออกมาทุกชื่อ (missing ตามลำดับ list, cap 64 + truncated)
    /// </summary>
    internal static class Sp5T0NameMatchTests
    {
        private static int _pass;
        private static int _fail;

        private static void Check(string name, bool ok)
        {
            Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] (nmatch) {name}");
            if (ok) _pass++; else _fail++;
        }

        public static int Run()
        {
            _pass = 0;
            _fail = 0;
            Console.WriteLine();
            Console.WriteLine("== SP5-NPC name-match T2 loveable (N4-follow-up — pure) ==");

            HashSet<string> given = new HashSet<string>(StringComparer.Ordinal) { "甲", "乙", "清风" };
            HashSet<string> full = new HashSet<string>(StringComparer.Ordinal) { "林云裳", "上官凤", "皇甫青衫" };

            // 1) match ด้วย heroName ตรง
            Sp5T0NameMatch.MatchResult r = Sp5T0NameMatch.Match(
                new List<string> { "甲" }, given, full);
            Check("match ด้วย heroName ตรง → present", r.Present == 1 && r.Total == 1 && r.Missing.Count == 0 && !r.MissingTruncated);

            // 2) match ด้วย family+given fallback (given เดี่ยวไม่มีใน set)
            r = Sp5T0NameMatch.Match(new List<string> { "林云裳", "上官凤" }, given, full);
            Check("match ด้วย family+given fallback → present 2/2", r.Present == 2 && r.Missing.Count == 0);

            // 3) missing ต้อง log ออกมาทุกชื่อ ตามลำดับ list
            r = Sp5T0NameMatch.Match(new List<string> { "甲", "ไม่มีจริง", "乙", "หาไม่เจอ" }, given, full);
            Check("missing log ครบตามลำดับ (2 ชื่อ)", r.Present == 2 && r.Total == 4 &&
                  r.Missing.Count == 2 && r.Missing[0] == "ไม่มีจริง" && r.Missing[1] == "หาไม่เจอ");

            // 4) null/empty ใน list = missing (log เป็น null ได้)
            r = Sp5T0NameMatch.Match(new List<string> { null, "", "甲" }, given, full);
            Check("null/empty = missing (Present=1, Missing=[null,\"\"])", r.Present == 1 && r.Total == 3 &&
                  r.Missing.Count == 2 && r.Missing[0] == null && r.Missing[1] == "");

            // 5) set ว่าง / null → ทุกชื่อ missing
            r = Sp5T0NameMatch.Match(new List<string> { "甲", "乙" }, new HashSet<string>(), full);
            Check("set ว่าง → ทุกชื่อ missing", r.Present == 0 && r.Missing.Count == 2 && r.Total == 2);
            r = Sp5T0NameMatch.Match(new List<string> { "甲" }, null, null);
            Check("set null → ทุกชื่อ missing (ไม่โยน)", r.Present == 0 && r.Missing.Count == 1);

            // 6) list null → result ว่าง ไม่โยน
            r = Sp5T0NameMatch.Match(null, given, full);
            Check("list null → Total=0 ไม่โยน", r.Total == 0 && r.Present == 0 && r.Missing.Count == 0);

            // 7) cap 64: 70 ชื่อ missing → Missing=64 + truncated=true
            List<string> big = new List<string>();
            for (int i = 0; i < 70; i++) big.Add("หาไม่เจอ" + i);
            r = Sp5T0NameMatch.Match(big, given, full);
            Check("cap 64 + MissingTruncated (70 ชื่อ missing)", r.Missing.Count == Sp5T0NameMatch.MissingCap &&
                  r.MissingTruncated && r.Present == 0 && r.Total == 70);

            // 8) ไม่ truncated เมื่อไม่เกิน cap (โครงจริง 57 ชื่อ — 55 present 2 missing)
            List<string> real = new List<string>();
            for (int i = 0; i < 55; i++) real.Add("甲");
            real.Add("หาไม่เจอ1"); real.Add("หาไม่เจอ2");
            r = Sp5T0NameMatch.Match(real, given, full);
            Check("57 ชื่อ (55 present / 2 missing) ไม่ truncated", r.Present == 55 && r.Missing.Count == 2 && !r.MissingTruncated);

            // 9) ชื่อซ้ำใน list นับราย entry + case-sensitive (Ordinal)
            r = Sp5T0NameMatch.Match(new List<string> { "甲", "甲" }, given, full);
            Check("ชื่อซ้ำ นับราย entry (Present=2)", r.Present == 2 && r.Missing.Count == 0);
            r = Sp5T0NameMatch.Match(new List<string> { "清风 " }, given, full);
            Check("Ordinal — ต่างช่องว่าง = missing", r.Present == 0 && r.Missing.Count == 1);

            // 10) โครงสร้างผลลัพธ์คงเส้นคงวา (Missing list ไม่ null เสมอ)
            r = Sp5T0NameMatch.Match(new List<string>(), given, full);
            Check("list ว่าง → Total=0, Missing list ไม่ null", r.Total == 0 && r.Missing != null);

            Console.WriteLine($"SP5-NPC name-match tests: {_pass} PASS / {_fail} FAIL");
            return _fail;
        }
    }
}
