using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace LongYinCoop.TimeV1
{
    /// <summary>
    /// V1 — probe log-only บน activity pump (มติ [Gate 3] 2026-10-31 ตามแผน time-plan-draft §D.1 ข้อ 5)
    ///
    /// เป้า hook 2 method (prefix + postfix + finalizer แต่ละตัว = 6 patches ต้องครบทั้งชุดหรือไม่ติดเลย):
    ///   - `WorkingUIController.StopWorking(bool finished)` (RVA 0x9CB220 — dump.cs:392739)
    ///       enter: เห็น `finished` arg + pre-state ครบ (ก่อน body เขียน workFinished/working)
    ///       exit : post-state (ยืนยัน workFinished == finished, working == 0 — ตาม static §B.3)
    ///       finalizer: ชื่อ exception (void finalizer = Harmony คืน exception เดิม — ไม่กลืน/ไม่แทนที่)
    ///   - `PlotController.MiddleStopWork()` (RVA 0x54EEA0 — dump.cs:384077)
    ///       enter/exit/finalizer เหมือนกัน (arg ไม่มี) — ใช้แยกเส้นทาง (a)/(b) ของ §B.3 และ verify
    ///       dispatch จาก plot-choice (UNKNOWN เดิม) ร่วมกับบันทึกการกระทำของผู้ใช้ราย TC
    ///
    /// "จุดยิง finishFuc" 0x9CB2C2/0x9CB2FA = คำสั่งกลาง body — แพตช์ขอบเขตไม่ได้ (errata E3):
    ///   stopworking.enter บันทึก `finishFuc` (ชื่อ handler ที่กำลังจะถูกยิง — field 0x38) และ
    ///   `fireWorkFinished` = ค่า arg `finished` (SPEC จากลำดับ static เขียน 0x9CB25F ก่อนยิง)
    ///
    /// ข้อบังคับมติ [Gate 3]:
    ///   (ก) pref `TimeV1a` default false — Apply(false) ณ โหลด = no-op (ไม่สร้าง Harmony/ไม่ติด patch/
    ///       ไม่เปิดไฟล์/ไม่เปิดเธรด) · (ข) Uninstall คืน patch count = 0 (ตรวจหลังถอนทุกครั้ง) ·
    ///   (ค) log-only 100% — ไม่แก้ args/ไม่เขียน __result/ไม่เขียน state เกม/ไม่เรียก method เกม;
    ///       prefix ทุกตัวคืน void (Harmony จึงห้าม skip original) และไม่ประกาศพารามิเตอร์ __result ·
    ///       อ่าน state ผ่าน typed interop เท่านั้น · (ง) try/catch กลืนทุก hook + err counter +
    ///       buffered writer · ไฟล์ปลายทางผ่าน pointer file (ไม่มี pointer = ไม่เขียน) ·
    ///   (จ) ไม่แก้ S2/** SP5/** TimeV2a/** — ไฟล์ใหม่ทั้งหมดอยู่ใต้ TimeV1/ + จุด wiring 3 บรรทัด
    ///       ใน CoopPrefs.cs/CoopMod.cs
    ///
    /// fail-closed: Apply(true) ต้องผ่าน gate (pref เปิด + pointer ชี้ .ndjson) **ก่อน**สร้าง Harmony —
    /// gate ไม่ผ่าน = ไม่ติด patch เลย (โครงเดียวกับ Sp5T0Probe §2.5 "ครบทุกเป้าหรือไม่ติดเลย")
    /// </summary>
    internal static class TimeV1Probe
    {
        private const string HarmonyId = "longyincoop.timev1.probe";
        public const string PointerFileName = "time-v1.out";
        // 2 เป้า × (prefix + postfix + finalizer)
        internal const int ExpectedPatchCount = 6;
        // ขีดจำกัดความปลอดภัย: hook ถี่ผิดปกติ (เช่น per-day handler วิ่งรัว) = ปิดตัวเอง
        private const long MaxLines = 20000;

        private static HarmonyLib.Harmony _harmony;
        private static TimeV1NdjsonWriter _writer;
        private static readonly object StateLock = new object();
        private static readonly Stopwatch Clock = Stopwatch.StartNew();
        private static long _lastAttemptMs = -2000;
        private static long _lastCapWarnMs;

        public static volatile bool Installed;
        public static long Events;
        public static long Dropped;
        public static int Errors;          // exception ที่ hook กลืน (ข้อ (ง))
        public static int ReadFails;       // typed read ล้มบางส่วน (event ยังออกโดย hasState/hasNames = false)
        public static string LastError;
        public static bool WriterOpen { get { return _writer != null; } }
        public static string OutputPath { get { return _writer == null ? null : _writer.Path; } }

        /// <summary>อ่าน pref แบบกัน null (ก่อน CoopPrefs.Init = false เสมอ — fail-closed)</summary>
        private static bool PrefEnabled()
        {
            MelonPreferences_Entry<bool> e = CoopPrefs.TimeV1a;
            return e != null && e.Value;
        }

        // =====================================================================
        // ติดตั้ง / ถอน
        // =====================================================================

        /// <summary>เรียกจาก CoopMod (OnInitializeMelon + Tick) — false = ไม่ติดอะไรเลย</summary>
        public static bool Apply(bool wanted)
        {
            if (!wanted)
            {
                // ข้อ (ก): Apply(false) = Uninstall ของสิ่งที่ไม่มีอยู่ = no-op ล้วน
                Uninstall();
                return false;
            }
            if (Installed) return true;

            // กัน retry ถี่เมื่อ target resolve ไม่ได้ / ยังไม่มี pointer (idiom JitProbe/Sp5T0)
            long now = Clock.ElapsedMilliseconds;
            if (now - _lastAttemptMs < 1000) return false;
            _lastAttemptMs = now;

            try
            {
                MethodInfo stop = AccessTools.Method(typeof(WorkingUIController), "StopWorking",
                                                             new Type[] { typeof(bool) });
                MethodInfo msw = AccessTools.Method(typeof(PlotController), "MiddleStopWork",
                                                             Type.EmptyTypes);
                if (stop == null || msw == null)
                {
                    CoopLog.Warn("TimeV1: target resolve ไม่ครบ (stop=" + (stop != null)
                                 + " msw=" + (msw != null) + ") — ไม่ติดทั้งชุด");
                    return false;
                }

                if (!OpenWriter()) return false;   // fail-closed: ไม่มี pointer = ไม่สร้าง Harmony

                int ownedBefore = CompatInventory.CountOwnedPatches(HarmonyId);
                if (ownedBefore != 0)
                {
                    CoopLog.Warn("TimeV1: พบ patch ของ probe อยู่ก่อนติด = " + ownedBefore + " (ต้องเป็น 0) — ไม่ติดซ้ำ");
                    return false;
                }

                _harmony = new HarmonyLib.Harmony(HarmonyId);
                Type self = typeof(TimeV1Probe);
                _harmony.Patch(stop,
                    prefix: new HarmonyMethod(M(self, "StopWorkingPrefix")),
                    postfix: new HarmonyMethod(M(self, "StopWorkingPostfix")),
                    finalizer: new HarmonyMethod(M(self, "StopWorkingFinalizer")));
                _harmony.Patch(msw,
                    prefix: new HarmonyMethod(M(self, "MiddleStopWorkPrefix")),
                    postfix: new HarmonyMethod(M(self, "MiddleStopWorkPostfix")),
                    finalizer: new HarmonyMethod(M(self, "MiddleStopWorkFinalizer")));

                // gate หลังติด = เทียบ "ชุด method เป้าหมาย + จำนวน patch-entries ต่อ method"
                // (patchcount-audit.md — CountOwnedPatches นิยาม method-count จับ partial-install ไม่ได้
                //  = สาเหตุ false-fail "2 (คาด 6)" ของ TC1-r1)
                int ownedAfter = CompatInventory.CountOwnedPatches(HarmonyId);
                int entriesAfter = CompatInventory.CountOwnedPatchEntries(HarmonyId);
                string patchMismatch;
                if (!CompatInventory.OwnedPatchTargetsMatch(HarmonyId,
                        new MethodBase[] { stop, msw }, new int[] { 3, 3 }, out patchMismatch))
                {
                    CoopLog.Warn("TimeV1: patch set หลังติดไม่ตรง (คาด " + ExpectedPatchCount
                                 + " patch-methods บน 2 เป้า) — ถอนทั้งชุด" + Environment.NewLine + patchMismatch);
                    Uninstall();
                    return false;
                }

                Installed = true;
                EmitSession("session.begin");
                CoopLog.Msg("TimeV1 installed (log-only): patchMethods=" + ownedAfter
                            + " patchEntries=" + entriesAfter
                            + " out=" + (OutputPath ?? "null"));
                return true;
            }
            catch (Exception e)
            {
                Errors++;
                LastError = e.GetType().Name + ": " + e.Message;
                CoopLog.Error(e);
                try { Uninstall(); } catch { }
                return false;
            }
        }

        /// <summary>ถอนทั้งชุด — ตรวจ patch count ต้องกลับเป็น 0 (ข้อ (ข) ของ [Gate 3])</summary>
        public static void Uninstall()
        {
            lock (StateLock)
            {
                try
                {
                    if (Installed)
                    {
                        EmitSession("session.end");
                    }
                    if (_harmony != null)
                    {
                        _harmony.UnpatchSelf();
                        _harmony = null;
                    }
                    bool wasInstalled = Installed;
                    Installed = false;

                    int owned = CompatInventory.CountOwnedPatches(HarmonyId);
                    if (owned != 0)
                    {
                        // ข้อ (ข): หลังถอนต้องคืน 0 — ไม่เป็น 0 = รายงานทันที (ไม่ถอนค้างเงียบ ๆ)
                        CoopLog.Warn("TimeV1: หลัง UnpatchSelf ยังเหลือ patch ของ probe = " + owned + " (ต้องเป็น 0)");
                    }
                    if (wasInstalled)
                    {
                        CoopLog.Msg(string.Format(
                            "TimeV1 removed: events={0} dropped={1} errors={2} readfail={3} ownedLeft={4}",
                            Interlocked.Read(ref Events), Interlocked.Read(ref Dropped),
                            Errors, ReadFails, owned));
                    }
                }
                catch (Exception e)
                {
                    Installed = false;
                    Errors++;
                    LastError = e.GetType().Name + ": " + e.Message;
                    CoopLog.Error(e);
                }
                finally
                {
                    CloseWriter();
                }
            }
        }

        private static MethodInfo M(Type t, string name)
        {
            return t.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        }

        // =====================================================================
        // pointer file → writer (fail-closed — กติกาเดียวกับ TimeV2aSampler)
        // =====================================================================

        private static bool OpenWriter()
        {
            string pointerPath = Path.Combine(CoopPaths.EnsureUserDataDir(), PointerFileName);
            string firstLine = null;
            try
            {
                if (!File.Exists(pointerPath))
                {
                    CoopLog.Warn("[TimeV1] pointer file absent: " + pointerPath + " — ไม่เปิด writer (fail-closed)");
                    return false;
                }
                foreach (string raw in File.ReadAllLines(pointerPath))
                {
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        firstLine = raw;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Errors++;
                LastError = e.GetType().Name + ": " + e.Message;
                CoopLog.Error(e);
                return false;
            }

            TimeV1Gate.Verdict verdict = TimeV1Gate.Evaluate(PrefEnabled(), firstLine);
            if (verdict != TimeV1Gate.Verdict.Ok)
            {
                CoopLog.Warn("[TimeV1] pointer gate = " + verdict + " — ไม่เปิด writer (fail-closed)");
                return false;
            }

            string target = TimeV1Gate.PathOf(firstLine);
            _writer = new TimeV1NdjsonWriter(target, 10);
            CoopLog.Msg("[TimeV1] writer open -> " + target);
            return true;
        }

        private static void CloseWriter()
        {
            TimeV1NdjsonWriter w = _writer;
            _writer = null;
            if (w != null)
            {
                try
                {
                    w.Dispose();
                    CoopLog.Msg("[TimeV1] writer closed lines=" + w.Lines.ToString(CultureInfo.InvariantCulture));
                }
                catch (Exception e)
                {
                    Errors++;
                    LastError = e.GetType().Name + ": " + e.Message;
                    CoopLog.Error(e);
                }
            }
        }

        /// <summary>เรียกจาก CoopMod.Tick — ขีดจำกัดบรรทัด (กัน spam ผิดปกติ)</summary>
        public static void Tick()
        {
            if (!Installed) return;
            if (Interlocked.Read(ref Events) >= MaxLines)
            {
                long now = Clock.ElapsedMilliseconds;
                if (now - _lastCapWarnMs > 60000)
                {
                    _lastCapWarnMs = now;
                    CoopLog.Warn("[TimeV1] เกิน cap " + MaxLines + " lines — ถอน probe (cap)");
                }
                Uninstall();
            }
        }

        // =====================================================================
        // hooks — log-only: อ่าน typed, กลืน exception, ไม่แตะ args/state (ข้อ (ค)(ง))
        // =====================================================================

        private static void StopWorkingPrefix(WorkingUIController __instance, bool finished)
        {
            try { Emit("stopworking.enter", finished, withFire: true); }
            catch { Swallow(); }
        }

        private static void StopWorkingPostfix(WorkingUIController __instance, bool finished)
        {
            try { Emit("stopworking.exit", finished, withFire: false); }
            catch { Swallow(); }
        }

        private static void StopWorkingFinalizer(Exception __exception)
        {
            // void finalizer = Harmony คืน exception เดิมเสมอ — ที่นี่เขียน log อย่างเดียว
            try { EmitFinalizer("stopworking.finalizer", __exception); }
            catch { Swallow(); }
        }

        private static void MiddleStopWorkPrefix(PlotController __instance)
        {
            try { Emit("middlestopwork.enter", null, withFire: false); }
            catch { Swallow(); }
        }

        private static void MiddleStopWorkPostfix(PlotController __instance)
        {
            try { Emit("middlestopwork.exit", null, withFire: false); }
            catch { Swallow(); }
        }

        private static void MiddleStopWorkFinalizer(Exception __exception)
        {
            try { EmitFinalizer("middlestopwork.finalizer", __exception); }
            catch { Swallow(); }
        }

        // =====================================================================
        // emit — อ่าน state ผ่าน typed interop (อ่านอย่างเดียวทุกจุด)
        // =====================================================================

        private static void Emit(string ev, bool? finished, bool withFire)
        {
            TimeV1Event e = new TimeV1Event();
            e.Ev = ev;
            e.TMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            e.ErrN = Errors;
            try { e.Frame = Time.frameCount; }
            catch { e.Frame = -1; }

            if (finished.HasValue)
            {
                e.WithFinished = true;
                e.Finished = finished.Value;
            }
            if (withFire)
            {
                // errata E3: ค่า "workFinished ขณะยิง" = arg finished ตามลำดับ static
                // (StopWorking เขียน workFinished=finished ที่ 0x9CB25F ก่อนยิง finishFuc
                //  ที่ 0x9CB2C2/0x9CB2FA) — ป้าย SPEC/[OBSERVED เชิงโค้ด] ไม่ใช่การอ่าน ณ คำสั่งยิง
                e.WithFire = true;
                e.FireWorkFinished = finished == true;
            }

            // pump state — อ่านผ่าน typed interop อย่างเดียว; ล้ม = hasState=false (ไม่ปล่อยค่าปลอม)
            try
            {
                WorkingUIController w = WorkingUIController.Instance;
                if (w != null)
                {
                    e.HasState = true;
                    e.Working = w.working;
                    e.WorkFinished = w.workFinished;
                    e.LeftWorkingDay = w.leftWorkingDay;
                    e.TotalWorkingDay = w.totalWorkingDay;
                    e.NextDayTime = w.nextDayTime;
                    e.LeftPauseTime = w.leftPauseTime;
                    e.ResourceNum = w.resourceNum;
                    e.NoCancel = w.noCancel;
                    e.Skipping = w.skipping;
                }
            }
            catch (Exception ex)
            {
                ReadFails++;
                LastError = ex.GetType().Name + ": " + ex.Message;
            }

            // ชื่องาน/handler — อ่านแยกชั้น (string marshaling ล้มได้โดยไม่กระทบ state)
            try
            {
                WorkingUIController w = WorkingUIController.Instance;
                if (w != null)
                {
                    e.HasNames = true;
                    e.WorkName = w.workName;
                    e.WorkingFuc = w.workingFuc;
                    e.FinishFuc = w.finishFuc;
                }
            }
            catch (Exception ex)
            {
                ReadFails++;
                LastError = ex.GetType().Name + ": " + ex.Message;
            }

            Commit(e);
        }

        private static void EmitFinalizer(string ev, Exception __exception)
        {
            TimeV1Event e = new TimeV1Event();
            e.Ev = ev;
            e.TMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            e.ErrN = Errors;
            try { e.Frame = Time.frameCount; }
            catch { e.Frame = -1; }
            e.WithEx = true;
            e.Ex = __exception != null ? __exception.GetType().Name : null;
            Commit(e);
        }

        private static void EmitSession(string ev)
        {
            TimeV1Event e = new TimeV1Event();
            e.Ev = ev;
            e.TMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            e.ErrN = Errors;
            try { e.Frame = Time.frameCount; }
            catch { e.Frame = -1; }
            Commit(e);
        }

        /// <summary>เขียน event ลง writer (writer ไม่มี = นับ Dropped — fail-closed ไม่เขียนไฟล์นอกขอบเขต)</summary>
        private static void Commit(TimeV1Event e)
        {
            TimeV1NdjsonWriter w = _writer;
            if (w == null)
            {
                Interlocked.Increment(ref Dropped);
                return;
            }
            w.Write(e.ToNdjsonLine());
            Interlocked.Increment(ref Events);
        }

        private static void Swallow()
        {
            Interlocked.Increment(ref Errors);
        }
    }
}
