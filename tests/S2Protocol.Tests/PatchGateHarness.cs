using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// PatchGate harness (รอบ 6 งาน 2 — มติ R6-6): พิสูจน์ "นิยามการนับ" ของ gate patch-count ตาม
    /// patchcount-audit.md บน artifact จริง + 0Harmony runtime จริง (โหลดจากโฟลเดอร์เกมผ่าน
    /// AssemblyResolve — idiom เดียวกับ TimeV1PurityHarness · ไม่เพิ่ม compile-time dependency):
    ///
    ///   (1) static check ซอร์ส: gate หลังติดของ TimeV1/Sp5T0/Sp5Npc ใช้ CompatInventory.OwnedPatchTargetsMatch
    ///       + ค่าคาด entries ต่อเป้าตรงโครง Patch() (TimeV1 2×3=6 · Sp5T0 7×3=21 · Sp5Npc 2+1+1+1=5)
    ///       + ไม่เหลือเงื่อนไขเทียบจำนวนแบบเก่า + log-only probe (JitProbe*/HarmonyProbe) ไม่ถูกแตะ
    ///   (2) semantic test บน 0Harmony จริง (เป้า = method ของ test assembly — ไม่แตะเกม):
    ///       method เดียวหลาย patch / ติดครึ่งชุด (ขาด method) / ติดครึ่งชุด (entries ไม่ครบ) /
    ///       เกินเป้า / ครบชุด / cleanup คืน 0
    ///   (3) สาธิตความต่างนิยาม: CountOwnedPatches (method-count) vs CountOwnedPatchEntries (entries)
    ///       — กรณีที่ method-count มองไม่เห็นคือกรณีที่ทำให้ TC1-r1 false-fail
    ///
    /// ข้อจำกัด (ประกาศตามขอบเขต): เป้า patch เป็น method ธรรมดาใน test process ไม่ใช่ interop เกม —
    /// พฤติกรรมบนเกมจริงยังต้อง runtime session ตาม RUNBOOK (เช่น ML log patchMethods/patchEntries)
    /// </summary>
    internal static class PatchGateHarness
    {
        private const string Owner = "longyincoop.tests.patchsetgate";

        private static int _pass;
        private static int _fail;

        private static void Check(string name, bool ok)
        {
            Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] (patchgate) {name}");
            if (ok) _pass++; else _fail++;
        }

        private static void FailWith(string name, string reason)
        {
            Console.WriteLine($"[FAIL] (patchgate) {name} — {reason}");
            _fail++;
        }

        public static int Run()
        {
            _pass = 0;
            _fail = 0;
            Console.WriteLine();
            Console.WriteLine("== PatchGate harness (นิยามการนับ patch-count — รอบ 6 งาน 2) ==");
            InstallResolver();
            RunSourceChecks();
            RunRuntimeChecks();
            Console.WriteLine($"PatchGate harness: {_pass} PASS / {_fail} FAIL");
            return _fail;
        }

        // ---- resolve 0Harmony/MonoMod จากโฟลเดอร์เกม (อ่าน dll — ไม่แตะเกม/ไม่เปิดเกม) ----
        private static void InstallResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string simple = new AssemblyName(args.Name).Name;
                string game = GameDir();
                string[] roots =
                {
                    Path.Combine(game, "MelonLoader", "net6"),
                    Path.Combine(game, "MelonLoader", "Il2CppAssemblies"),
                };
                foreach (string root in roots)
                {
                    string cand = Path.Combine(root, simple + ".dll");
                    if (File.Exists(cand))
                    {
                        try { return Assembly.LoadFrom(cand); }
                        catch { return null; }
                    }
                }
                return null;
            };
        }

        private static string GameDir()
        {
            string env = Environment.GetEnvironmentVariable("LONGYIN_GAME_DIR");
            return string.IsNullOrEmpty(env)
                ? @"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan"
                : env;
        }

        private static string FindRepoRoot()
        {
            for (string dir = AppContext.BaseDirectory; dir != null; dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar)))
            {
                if (File.Exists(Path.Combine(dir, "src", "LongYinCoop", "CompatInventory.cs")))
                    return dir;
            }
            return null;
        }

        private static string ReadSource(string root, params string[] parts)
        {
            string p = Path.Combine(new[] { root, "src", "LongYinCoop" }.Concat(parts).ToArray());
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }

        // ---------------- (1) static check ซอร์ส ----------------
        private static void RunSourceChecks()
        {
            Console.WriteLine("-- (1) static check ซอร์ส (gate ใช้ set+entries matcher) --");
            string root = FindRepoRoot();
            if (root == null)
            {
                FailWith("หา repo root (src/LongYinCoop/CompatInventory.cs)", "รันจาก build output ปกติเท่านั้น");
                return;
            }

            string compat = ReadSource(root, "CompatInventory.cs");
            Check("CompatInventory: มี CountOwnedPatchEntries/CollectOwnedPatchSet/OwnedPatchTargetsMatch/ComparePatchSets",
                  compat != null && compat.Contains("CountOwnedPatchEntries") && compat.Contains("CollectOwnedPatchSet")
                  && compat.Contains("OwnedPatchTargetsMatch") && compat.Contains("ComparePatchSets"));
            Check("CompatInventory: นิยามเดิม CountOwnedPatches คงอยู่พร้อมคอมเมนต์ (method-count — ห้ามใช้ตรวจติดครบชุด)",
                  compat != null && compat.Contains("public static int CountOwnedPatches(string owner)")
                  && compat.Contains("ไม่ใช่จำนวน patch objects") && compat.Contains("ห้ามใช้ตรวจ \"ติดครบชุด\""));

            string timeV1 = ReadSource(root, "TimeV1", "TimeV1Probe.cs");
            Check("TimeV1Probe: gate หลังติดใช้ OwnedPatchTargetsMatch + คาด { 3, 3 }",
                  timeV1 != null && timeV1.Contains("OwnedPatchTargetsMatch") && timeV1.Contains("new int[] { 3, 3 }"));
            Check("TimeV1Probe: ไม่เหลือเงื่อนไขเทียบ method-count เดิม (ownedAfter != ExpectedPatchCount)",
                  timeV1 != null && !timeV1.Contains("ownedAfter != ExpectedPatchCount"));

            string sp5T0 = ReadSource(root, "SP5", "Sp5T0Probe.cs");
            Check("Sp5T0Probe: gate หลังติดใช้ OwnedPatchTargetsMatch + คาด { 3, 3, 3, 3, 3, 3, 3 }",
                  sp5T0 != null && sp5T0.Contains("OwnedPatchTargetsMatch") && sp5T0.Contains("new int[] { 3, 3, 3, 3, 3, 3, 3 }"));
            Check("Sp5T0Probe: ไม่เหลือเงื่อนไขเทียบ method-count เดิม (ownedAfter != 7)",
                  sp5T0 != null && !sp5T0.Contains("ownedAfter != 7"));

            string sp5Npc = ReadSource(root, "SP5", "Sp5NpcGate.cs");
            Check("Sp5NpcGate: gate หลังติดใช้ OwnedPatchTargetsMatch + คาด { 2, 1, 1, 1 }",
                  sp5Npc != null && sp5Npc.Contains("OwnedPatchTargetsMatch") && sp5Npc.Contains("new int[] { 2, 1, 1, 1 }"));
            Check("Sp5NpcGate: ไม่เหลือเงื่อนไขเทียบ delta method-count เดิม (ownedAfter != ownedBefore + 4)",
                  sp5Npc != null && !sp5Npc.Contains("ownedAfter != ownedBefore + 4"));

            string jitW6 = ReadSource(root, "S1.5c", "JitProbeW6.cs");
            Check("log-only probe ไม่ถูกแตะ (JitProbeW6 ยัง log ด้วย CountOwnedPatches แบบเดิม)",
                  jitW6 != null && jitW6.Contains("CountOwnedPatches") && !jitW6.Contains("OwnedPatchTargetsMatch"));
        }

        // ---------------- (2)+(3) semantic test บน 0Harmony จริง ----------------

        // เป้า dummy (static — Harmony patch ได้ใน test process โดยไม่แตะเกม)
        private static void DummyTargetOne(int v) { }
        private static void DummyTargetTwo(int v) { }
        private static void DummyOverload(int v) { }
        private static void DummyOverload(int v, int w) { }

        // hook dummy (signature ตรง contract เดียวกับ hook ของ probe)
        private static void DummyPrefix() { }
        private static void DummyPostfix() { }
        private static void DummyFinalizer(Exception __exception) { }

        private static Assembly _harmonyAsm;
        private static Type _harmonyType;
        private static Type _harmonyMethodType;

        private static MethodBase FindTarget(string name, Type[] sig)
        {
            return typeof(PatchGateHarness).GetMethod(name,
                BindingFlags.Static | BindingFlags.NonPublic, null, sig, null);
        }

        private static void RunRuntimeChecks()
        {
            Console.WriteLine("-- (2)+(3) semantic test บน 0Harmony จริง (เป้า = method ของ test assembly) --");

            // โหลด artifact จริง (idiom TimeV1PurityHarness — Release ก่อนแล้ว Debug)
            string dll = null;
            for (string dir = AppContext.BaseDirectory; dir != null && dll == null; dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar)))
            {
                foreach (string cfg in new[] { "Release", "Debug" })
                {
                    string cand = Path.Combine(dir, "src", "LongYinCoop", "bin", cfg, "LongYinCoop.dll");
                    if (File.Exists(cand)) { dll = cand; break; }
                }
            }
            if (dll == null)
            {
                FailWith("artifact LongYinCoop.dll พบ", "หาไม่เจอทั้ง Release/Debug — ต้อง dotnet build src/LongYinCoop ก่อนรัน suite นี้");
                return;
            }
            Assembly asm;
            try { asm = Assembly.LoadFrom(dll); }
            catch (Exception ex) { FailWith("โหลด artifact ได้", ex.GetType().Name + ": " + ex.Message); return; }
            Type compat = asm.GetType("LongYinCoop.CompatInventory", false);
            if (compat == null) { FailWith("พบชนิด CompatInventory ใน artifact", "artifact เก่ากว่าซอร์ส"); return; }

            const BindingFlags SP = BindingFlags.Static | BindingFlags.Public;
            MethodInfo countMethodsFn = compat.GetMethod("CountOwnedPatches", SP);
            MethodInfo countEntriesFn = compat.GetMethod("CountOwnedPatchEntries", SP);
            MethodInfo matchFn = compat.GetMethod("OwnedPatchTargetsMatch", SP);
            MethodInfo describeFn = compat.GetMethod("DescribeKey", SP);
            MethodInfo buildFn = compat.GetMethod("BuildExpectedSet", SP);
            MethodInfo compareFn = compat.GetMethod("ComparePatchSets", SP);
            Check("artifact มี API ครบ (CountOwnedPatches/CountOwnedPatchEntries/OwnedPatchTargetsMatch/DescribeKey/BuildExpectedSet/ComparePatchSets)",
                  countMethodsFn != null && countEntriesFn != null && matchFn != null
                  && describeFn != null && buildFn != null && compareFn != null);
            if (countMethodsFn == null || countEntriesFn == null || matchFn == null
                || describeFn == null || buildFn == null || compareFn == null) return;

            Func<string, int> countMethods = o => (int)countMethodsFn.Invoke(null, new object[] { o });
            Func<string, int> countEntries = o => (int)countEntriesFn.Invoke(null, new object[] { o });
            Func<MethodBase, string> describe = m => (string)describeFn.Invoke(null, new[] { m });

            try
            {
                _harmonyAsm = Assembly.Load("0Harmony");
            }
            catch (Exception ex)
            {
                FailWith("โหลด 0Harmony (runtime จากโฟลเดอร์เกม) ได้", ex.GetType().Name + ": " + ex.Message);
                return;
            }
            Check("โหลด 0Harmony (runtime จากโฟลเดอร์เกม) ได้", _harmonyAsm != null);
            _harmonyType = _harmonyAsm.GetType("HarmonyLib.Harmony", false);
            _harmonyMethodType = _harmonyAsm.GetType("HarmonyLib.HarmonyMethod", false);
            if (_harmonyType == null || _harmonyMethodType == null)
            {
                FailWith("พบ HarmonyLib.Harmony/HarmonyMethod", "0Harmony ใน net6 ของเกมไม่ตรงรูปแบบ");
                return;
            }

            MethodBase t1 = FindTarget("DummyTargetOne", new[] { typeof(int) });
            MethodBase t2 = FindTarget("DummyTargetTwo", new[] { typeof(int) });
            MethodBase o1 = FindTarget("DummyOverload", new[] { typeof(int) });
            MethodBase o2 = FindTarget("DummyOverload", new[] { typeof(int), typeof(int) });
            MethodInfo prefix = typeof(PatchGateHarness).GetMethod("DummyPrefix", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo postfix = typeof(PatchGateHarness).GetMethod("DummyPostfix", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo finalizer = typeof(PatchGateHarness).GetMethod("DummyFinalizer", BindingFlags.Static | BindingFlags.NonPublic);
            Check("เป้า/hook dummy ครบ", t1 != null && t2 != null && o1 != null && o2 != null
                  && prefix != null && postfix != null && finalizer != null);
            if (t1 == null || t2 == null || o1 == null || o2 == null
                || prefix == null || postfix == null || finalizer == null) return;

            object harmony;
            try { harmony = Activator.CreateInstance(_harmonyType, Owner); }
            catch (Exception ex)
            {
                FailWith("สร้าง Harmony instance ได้", ex.GetType().Name + ": " + ex.Message);
                return;
            }

            try
            {
                // ---- (A) method เดียวหลาย patch + สาธิตความต่างนิยาม ----
                Patch(harmony, t1, prefix, postfix, finalizer);
                Check("(A) method เดียวหลาย patch: CountOwnedPatches = 1 (method-count)",
                      countMethods(Owner) == 1);
                Check("(A) method เดียวหลาย patch: CountOwnedPatchEntries = 3 (entries)",
                      countEntries(Owner) == 3);
                Check("(A) สาธิตความต่างนิยาม: 1 method แต่ 3 entries — method-count มองไม่เห็นจำนวนราย patch",
                      countMethods(Owner) != countEntries(Owner));
                Check("(A) ครบชุด 1 เป้า: Match([t1→3]) = true",
                      MatchCore(matchFn, new[] { t1 }, new[] { 3 }, out _));
                Check("(A) ติดครึ่งชุด (ขาด method): Match([t1→3, t2→3]) = false + report มี 'ขาด:' + ชื่อเป้าที่ขาด",
                      !MatchCore(matchFn, new[] { t1, t2 }, new[] { 3, 3 }, out string r1)
                      && r1.Contains("ขาด:") && r1.Contains(describe(t2)));

                // ---- (B) ติดครึ่งชุดระดับ entries (ถอด postfix จริงด้วย Harmony) ----
                Unpatch(harmony, t1, "Postfix");
                Check("(B) ถอด postfix: CountOwnedPatchEntries = 2",
                      countEntries(Owner) == 2);
                Check("(B) ติดครึ่งชุด (entries ไม่ครบ): Match([t1→3]) = false + report มี 'จำนวนไม่ตรง'",
                      !MatchCore(matchFn, new[] { t1 }, new[] { 3 }, out string r2)
                      && r2.Contains("จำนวนไม่ตรง"));
                Check("(B) Match ตามจำนวนจริง [t1→2] = true",
                      MatchCore(matchFn, new[] { t1 }, new[] { 2 }, out _));

                // ---- (C) เกินเป้า ----
                Patch(harmony, t2, prefix, null, null);
                Check("(C) เกินเป้า: Match([t1→2]) = false + report มี 'เกิน:' + ชื่อเป้าเกิน",
                      !MatchCore(matchFn, new[] { t1 }, new[] { 2 }, out string r3)
                      && r3.Contains("เกิน:") && r3.Contains(describe(t2)));
                Check("(C) ครบชุด 2 เป้า: Match([t1→2, t2→1]) = true",
                      MatchCore(matchFn, new[] { t1, t2 }, new[] { 2, 1 }, out _));

                // ---- (D) DescribeKey แยก overload ชื่อเดียว (GenerateHeroData ×4 ของ Sp5T0 = กรณีจริง) ----
                Check("(D) DescribeKey แยก overload ชื่อเดียว (signature ต่าง) ได้",
                      describe(o1) != describe(o2));
                Dictionary<string, int> expected = (Dictionary<string, int>)buildFn.Invoke(null,
                    new object[] { new MethodBase[] { o1, o2 }, new int[] { 1, 1 } });
                Dictionary<string, int> actual = new Dictionary<string, int>(StringComparer.Ordinal);
                actual[describe(o1)] = 1;
                actual[describe(o2)] = 1;
                object[] cargs = { expected, actual, null };
                bool pureOk = (bool)compareFn.Invoke(null, cargs);
                string pureReport = (string)cargs[2];
                Check("(D) ComparePatchSets (pure) ชุด 2 key ตรง = true + report รูปแบบ 'คาด:/จริง:'",
                      pureOk && pureReport != null && pureReport.Contains("คาด:") && pureReport.Contains("จริง:"));
            }
            catch (Exception ex)
            {
                FailWith("semantic test รันครบ", ex.GetType().Name + ": " + ex.Message
                         + (ex.InnerException != null ? " / inner: " + ex.InnerException.Message : ""));
            }
            finally
            {
                // cleanup — ห้ามทิ้ง patch ค้างใน test process (harness อื่นนับเฉพาะ owner ของตัวเอง)
                try { UnpatchAll(harmony); }
                catch (Exception ex) { Console.WriteLine("      cleanup UnpatchAll ล้ม: " + ex.Message); }
            }

            Check("(cleanup) UnpatchAll แล้ว: method-count = 0 + entries = 0",
                  countMethods(Owner) == 0 && countEntries(Owner) == 0);
        }

        // ---- Harmony runtime ผ่าน reflection (ไม่มี compile-time reference — ตามข้อจำกัด "ห้ามเพิ่ม dependency") ----

        private static object HarmonyMethod(MethodInfo m)
        {
            return Activator.CreateInstance(_harmonyMethodType, m);
        }

        private static void Patch(object harmony, MethodBase target, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer)
        {
            // Harmony 2.10.2 มี overload 5/6 พารามิเตอร์ — เลือกตัว param เยอะสุด (มี ilmanipulator)
            MethodInfo patchFn = _harmonyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == "Patch"
                            && m.GetParameters().Length >= 1
                            && m.GetParameters()[0].ParameterType == typeof(MethodBase)
                            && m.GetParameters().Skip(1).All(p => p.ParameterType == _harmonyMethodType))
                .OrderByDescending(m => m.GetParameters().Length)
                .First();
            ParameterInfo[] ps = patchFn.GetParameters();
            object[] args = new object[ps.Length];
            args[0] = target;
            for (int i = 1; i < ps.Length; i++)
            {
                switch (ps[i].Name)
                {
                    // HarmonyMethod(null) จะ throw ArgumentNullException — hook ที่ไม่ใช้ส่ง null ตรง ๆ
                    case "prefix": args[i] = prefix != null ? HarmonyMethod(prefix) : null; break;
                    case "postfix": args[i] = postfix != null ? HarmonyMethod(postfix) : null; break;
                    case "finalizer": args[i] = finalizer != null ? HarmonyMethod(finalizer) : null; break;
                    default: args[i] = null; break; // transpiler/ilmanipulator
                }
            }
            patchFn.Invoke(harmony, args);
        }

        private static void Unpatch(object harmony, MethodBase target, string patchTypeName)
        {
            Type enumType = _harmonyAsm.GetType("HarmonyLib.HarmonyPatchType", true);
            object val = Enum.Parse(enumType, patchTypeName);
            MethodInfo unpatchFn = _harmonyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Single(m => m.Name == "Unpatch"
                             && m.GetParameters().Length == 3
                             && m.GetParameters()[0].ParameterType == typeof(MethodBase)
                             && m.GetParameters()[1].ParameterType == enumType);
            unpatchFn.Invoke(harmony, new[] { target, val, Owner });
        }

        private static void UnpatchAll(object harmony)
        {
            MethodInfo fn = _harmonyType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Single(m => m.Name == "UnpatchAll"
                             && m.GetParameters().Length == 1
                             && m.GetParameters()[0].ParameterType == typeof(string));
            fn.Invoke(harmony, new object[] { Owner });
        }

        private static bool MatchCore(MethodInfo matchFn, MethodBase[] targets, int[] entries, out string report)
        {
            object[] args = { Owner, targets, entries, null };
            bool ok;
            try
            {
                ok = (bool)matchFn.Invoke(null, args);
            }
            catch (Exception ex)
            {
                report = "invoke ล้ม: " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return false;
            }
            report = (string)args[3];
            return ok;
        }
    }
}
