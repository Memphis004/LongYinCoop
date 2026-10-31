using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// V1 purity harness (มติ [Gate 3] 2026-10-31 ข้อ (ก)(ข)(ค)(ง)) — พิสูจน์ "กลไก log-only" ของ
    /// TimeV1Probe บน **artifact จริง** (โหลด LongYinCoop.dll ที่ build แล้วด้วย reflection — ตามแบบ
    /// Sp5T0LinkageHarness; ไม่ re-implement ไม่แตะซอร์ส):
    ///
    ///   - ข้อ (ก) pref off = no-op: Apply(false) → false + ไม่สร้าง Harmony (_harmony ยัง null) +
    ///     patch count ของ probe = 0 · Apply(true) เมื่อไม่มี pointer = fail-closed ก่อนสร้าง Harmony
    ///   - ข้อ (ข) Uninstall คืน patch count = 0 (offline: เรียก Uninstall บน probe ที่ไม่เคยติด —
    ///     ต้องปลอดภัยและคืน 0; ฝั่ง runtime จริงครอบด้วย log "ownedLeft=0" + RUNBOOK ขั้น 8)
    ///   - ข้อ (ค) hook ไม่เปลี่ยน args/result: ทั้ง 6 hook คืน **void** (prefix void = Harmony ห้าม
    ///     skip original), ไม่มีพารามิเตอร์ ByRef, ไม่ประกาศ __result, ชื่อพารามิเตอร์ตรงชุดที่อนุมัติ
    ///   - ข้อ (ง) exception ถูกกลืน: invoke hook ด้วย __instance = null บน artifact (interop ยังไม่มี
    ///     runtime) — ต้องไม่มี exception หลุดออกจาก hook + ReadFails เดิน (กลืนภายใน) + Dropped เดิน
    ///     (writer ยังไม่เปิด — fail-closed)
    ///
    /// สิ่งที่ harness นี้พิสูจน์ "ไม่ได้" (ประกาศตามขอบเขต): พฤติกรรมจริงบนเกม (ต้อง runtime —
    /// RUNBOOK-V1 ขั้น 4: ML log "TimeV1 installed (log-only): patchMethods=2 patchEntries=6" + ไม่มี
    /// Harmony ใดนอกจากนี้ — รูปแบบ log แก้ตาม patch-count gate fix รอบ 6 งาน 2)
    /// · การอ่านค่าจริงของ interop getter · ลำดับ prefix/postfix บนเป้าเกม
    /// </summary>
    internal static class TimeV1PurityHarness
    {
        private static int _pass;
        private static int _fail;

        private static void Check(string name, bool ok)
        {
            Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] (timev1h) {name}");
            if (ok) _pass++; else _fail++;
        }

        private static void FailWith(string name, string reason)
        {
            Console.WriteLine($"[FAIL] (timev1h) {name} — {reason}");
            _fail++;
        }

        private static string GameDir()
        {
            string env = Environment.GetEnvironmentVariable("LONGYIN_GAME_DIR");
            return string.IsNullOrEmpty(env)
                ? @"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan"
                : env;
        }

        public static int Run()
        {
            _pass = 0;
            _fail = 0;
            Console.WriteLine();
            Console.WriteLine("== TimeV1 purity harness (log-only กลไก — artifact จริง) ==");

            InstallResolver();
            RunCore();
            Console.WriteLine($"TimeV1 purity harness: {_pass} PASS / {_fail} FAIL");
            return _fail;
        }

        // ---- resolve reference ของ artifact จากโฟลเดอร์เกม (อ่าน dll — ไม่แตะเกม/ไม่เปิดเกม) ----
        private static void InstallResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string simple = new AssemblyName(args.Name).Name;
                string game = GameDir();
                string[] roots =
                {
                    Path.Combine(game, "MelonLoader", "Il2CppAssemblies"),
                    Path.Combine(game, "MelonLoader", "net6"),
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

        private static void RunCore()
        {
            // ---- โหลด artifact จริง (Release ก่อน แล้ว Debug) + sha256 ผูกหลักฐาน ----
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
                FailWith("artifact LongYinCoop.dll พบ",
                         "หาไม่เจอทั้ง Release/Debug — ต้อง dotnet build src/LongYinCoop ก่อนรัน suite นี้");
                return;
            }
            string sha;
            using (FileStream fs = File.OpenRead(dll))
            using (SHA256 h = SHA256.Create())
                sha = BitConverter.ToString(h.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
            Console.WriteLine($"      artifact: {dll}");
            Console.WriteLine($"      sha256  : {sha}");
            Check("artifact LongYinCoop.dll พบ", true);

            Assembly asm;
            try { asm = Assembly.LoadFrom(dll); }
            catch (Exception ex) { FailWith("โหลด artifact ได้", ex.GetType().Name + ": " + ex.Message); return; }
            Check("โหลด artifact ได้", asm != null);

            Type probe = asm.GetType("LongYinCoop.TimeV1.TimeV1Probe", false);
            if (probe == null)
            {
                FailWith("พบชนิด TimeV1Probe ใน artifact",
                         "artifact เก่ากว่าซอร์ส (ไม่มี TimeV1) — ต้อง dotnet build src/LongYinCoop ใหม่");
                return;
            }
            Check("พบชนิด TimeV1Probe ใน artifact", true);

            const BindingFlags SP = BindingFlags.Static | BindingFlags.Public;
            const BindingFlags SN = BindingFlags.Static | BindingFlags.NonPublic;

            // ---- ค่าคงที่ของขอบเขต: HarmonyId + patch count คาดหมาย = 6 (2 เป้า × 3 หมวด) ----
            FieldInfo idF = probe.GetField("HarmonyId", SN);
            string harmonyId = idF != null ? idF.GetValue(null) as string : null;
            Check("HarmonyId = longyincoop.timev1.probe", harmonyId == "longyincoop.timev1.probe");
            FieldInfo cntF = probe.GetField("ExpectedPatchCount", SN | BindingFlags.Public);
            int expected = cntF != null ? (int)cntF.GetValue(null) : -1;
            Check("ExpectedPatchCount = 6 (prefix/postfix/finalizer × 2 เป้า)", expected == 6);

            // ---- ข้อ (ค): ทั้ง 6 hook ต้องเป็น void + ไม่มี ByRef + ไม่มี __result + ชื่อพารามิเตอร์ตรงชุดอนุมัติ ----
            string[] hookNames =
            {
                "StopWorkingPrefix", "StopWorkingPostfix", "StopWorkingFinalizer",
                "MiddleStopWorkPrefix", "MiddleStopWorkPostfix", "MiddleStopWorkFinalizer",
            };
            MethodInfo[] hooks = new MethodInfo[hookNames.Length];
            bool allHooks = true;
            for (int i = 0; i < hookNames.Length; i++)
            {
                hooks[i] = probe.GetMethod(hookNames[i], SN);
                if (hooks[i] == null) allHooks = false;
            }
            Check("hook ครบ 6 ตัวตามชื่อ", allHooks);
            if (!allHooks) return;

            bool purity = true;
            for (int i = 0; i < hooks.Length; i++)
            {
                MethodInfo m = hooks[i];
                if (m.ReturnType != typeof(void))
                {
                    Console.WriteLine($"      {hookNames[i]}: return {m.ReturnType.Name} (ต้องเป็น void — prefix bool = skip original ได้)");
                    purity = false;
                }
                ParameterInfo[] ps = m.GetParameters();
                foreach (ParameterInfo p in ps)
                {
                    if (p.ParameterType.IsByRef)
                    {
                        Console.WriteLine($"      {hookNames[i]}: พารามิเตอร์ ByRef {p.Name}");
                        purity = false;
                    }
                    if (p.Name == "__result")
                    {
                        Console.WriteLine($"      {hookNames[i]}: ประกาศ __result (ห้าม — log-only)");
                        purity = false;
                    }
                }
            }
            Check("ข้อ (ค): hook ทั้ง 6 = void + ไม่มี ByRef + ไม่มี __result (log-only ตาม Harmony contract)", purity);

            // ชื่อ/ชนิดพารามิเตอร์รายตัว (ป้องกันการแก้ signature โดยไม่รีวิว)
            Check("StopWorkingPrefix(_, bool finished)",
                  Params(hooks[0], "__instance", "finished", "Boolean"));
            Check("StopWorkingPostfix(_, bool finished)",
                  Params(hooks[1], "__instance", "finished", "Boolean"));
            Check("StopWorkingFinalizer(Exception __exception)",
                  Params(hooks[2], "__exception", null, null));
            Check("MiddleStopWorkPrefix(__instance)",
                  Params(hooks[3], "__instance", null, null));
            Check("MiddleStopWorkPostfix(__instance)",
                  Params(hooks[4], "__instance", null, null));
            Check("MiddleStopWorkFinalizer(Exception __exception)",
                  Params(hooks[5], "__exception", null, null));

            // ---- helper: ตัวนับ + CompatInventory.CountOwnedPatches ผ่าน reflection ----
            FieldInfo installedF = probe.GetField("Installed", SP);
            FieldInfo harmonyF = probe.GetField("_harmony", SN);
            FieldInfo errorsF = probe.GetField("Errors", SP);
            FieldInfo readFailsF = probe.GetField("ReadFails", SP);
            FieldInfo droppedF = probe.GetField("Dropped", SP);
            MethodInfo ownedFn = asm.GetType("LongYinCoop.CompatInventory", false)
                                  ?.GetMethod("CountOwnedPatches", SP);
            Func<int> owned = () => ownedFn != null ? (int)ownedFn.Invoke(null, new[] { harmonyId }) : -1;
            MethodInfo applyFn = probe.GetMethod("Apply", SP);
            MethodInfo uninstallFn = probe.GetMethod("Uninstall", SP);

            Check("โครงสถานะครบ (Installed/_harmony/Errors/ReadFails/Dropped/Apply/Uninstall/CountOwnedPatches)",
                  installedF != null && harmonyF != null && errorsF != null && readFailsF != null
                  && droppedF != null && applyFn != null && uninstallFn != null && ownedFn != null);
            if (installedF == null || harmonyF == null || applyFn == null || uninstallFn == null || ownedFn == null) return;

            // เริ่มจากสถานะสะอาด (test process ไม่เคยติด)
            uninstallFn.Invoke(null, null);
            Check("ข้อ (ข): Uninstall บน probe ที่ไม่เคยติด = ปลอดภัย + patch count คืน 0",
                  installedF.GetValue(null) is bool b0 && !b0 && owned() == 0 && harmonyF.GetValue(null) == null);

            // ---- ข้อ (ก): pref off = no-op — ไม่สร้าง Harmony/ไม่เปิดไฟล์/ไม่เปิดเธรด ----
            object rOff = applyFn.Invoke(null, new object[] { false });
            Check("ข้อ (ก): Apply(false) คืน false",
                  rOff is bool boff && !boff);
            Check("ข้อ (ก): Apply(false) แล้ว Installed=false + _harmony ยัง null + owned=0",
                  installedF.GetValue(null) is bool b1 && !b1
                  && harmonyF.GetValue(null) == null && owned() == 0);

            // ---- ข้อ (ก): Apply(true) เมื่อไม่มี pointer = fail-closed ก่อนสร้าง Harmony ----
            // (ใน test process CoopPrefs.TimeV1a == null → PrefEnabled()=false → gate PrefOff แม้ pointer หลงเหลือ)
            object rOn = applyFn.Invoke(null, new object[] { true });
            Check("ข้อ (ก): Apply(true) ไม่มี pointer/pref → false (fail-closed)",
                  rOn is bool bon && !bon);
            Check("ข้อ (ก): Apply(true) fail-closed → _harmony ยัง null + owned=0 (ไม่มี patch ค้าง)",
                  harmonyF.GetValue(null) == null && owned() == 0);

            // ---- ข้อ (ง): exception ถูกกลืน — invoke hook ด้วย __instance=null (interop ไม่มี runtime) ----
            int err0 = (int)errorsF.GetValue(null);
            int rf0 = (int)readFailsF.GetValue(null);
            long dr0 = (long)droppedF.GetValue(null);
            bool swallowOk = InvokeQuietly(hooks[0], null, true)                       // StopWorkingPrefix(null, true)
                          && InvokeQuietly(hooks[1], null, true)                       // StopWorkingPostfix(null, true)
                          && InvokeQuietly(hooks[2], new object[] { null })            // StopWorkingFinalizer(null)
                          && InvokeQuietly(hooks[3], new object[] { null })            // MiddleStopWorkPrefix(null)
                          && InvokeQuietly(hooks[4], new object[] { null })            // MiddleStopWorkPostfix(null)
                          && InvokeQuietly(hooks[5], new object[] { null });           // MiddleStopWorkFinalizer(null)
            Check("ข้อ (ง): invoke hook ทั้ง 6 ด้วย instance=null — ไม่มี exception หลุดออกจาก hook", swallowOk);
            Check("ข้อ (ง): typed read ล้มถูกกลืนภายใน (ReadFails เดิน)", (int)readFailsF.GetValue(null) > rf0);
            Check("ข้อ (ง): writer ยังไม่เปิด → event ถูกนับ Dropped (fail-closed ไม่เขียนไฟล์)",
                  (long)droppedF.GetValue(null) > dr0);

            // เคลียร์สถานะท้าย harness — ต้องจบที่ patch count 0 เสมอ
            uninstallFn.Invoke(null, null);
            Check("ข้อ (ข): ปิดท้าย Uninstall → patch count = 0", owned() == 0
                  && installedF.GetValue(null) is bool b2 && !b2
                  && harmonyF.GetValue(null) == null);
        }

        private static bool Params(MethodInfo m, string p0, string p1, string p1TypeName)
        {
            ParameterInfo[] ps = m.GetParameters();
            if (ps.Length == 0 || ps[0].Name != p0) return false;
            if (p1 == null) return ps.Length == 1;
            return ps.Length == 2 && ps[1].Name == p1 && ps[1].ParameterType.Name == p1TypeName;
        }

        /// <summary>invoke แล้วคืน true เมื่อไม่มี exception ใดหลุดออกจาก hook (TargetInvocationException = fail)</summary>
        private static bool InvokeQuietly(MethodInfo m, params object[] args)
        {
            try
            {
                m.Invoke(null, args);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      hook {m.Name} หลุด exception: {ex.InnerException?.GetType().Name ?? ex.GetType().Name}");
                return false;
            }
        }
    }
}
