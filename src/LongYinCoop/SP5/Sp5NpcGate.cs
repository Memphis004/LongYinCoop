using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Il2Cpp;
using LongYinCoop.S2;

namespace LongYinCoop
{
    /// <summary>
    /// SP5-NPC gate — hook shells G1/G4/G5 (แผน plans/sp5-npc-reduce-plan-draft.md §3/§5.2,
    /// stage N2 — **ห้าม deploy จนกว่ามติอนุมัติ stage ถัดไป**)
    ///
    /// · G1 = prefix/finalizer บน `GameController.GenerateHero()`: ย่อ `heroNum` ใน RAM ตาม
    ///   Sp5NpcRules.ShrinkHeroNum แล้ว**คืนค่าเดิมท้ายรอบ** (บอบช่อง exception — finalizer restore
    ///   เสมอ) ⇒ การเขียนถูกจำกัดใน window ของ creation round เท่านั้น
    /// · G4 = postfix บน `GetRecommendHeroNum()`/`GetMaxHeroNum()`: clamp เพดาน (เฉพาะ Mid)
    /// · G5 = prefix บน `WorldAddNewHero(int,int,bool,bool=True)`: คืน null ปฏิเสธการเติม
    ///   (เฉพาะ Strong — caller 2 site มี null-check OBSERVED; พฤติกรรม null-branch ต้องยืนยันตอน
    ///   runtime test ก่อนใช้จริง)
    /// · **ไม่มี G2/A2** (budget ที่ factory — แผน §7 กำหนด "+G2 ถ้าอนุมัติ" = ยังไม่อนุมัติ)
    /// · ตัดสินทุก invocation ด้วย `Sp5NpcPolicy.Decide` จาก net.cfg (poll ≤1 s) — fail-closed:
    ///   อ่านไม่ได้/ค่าแปลก ⇒ Off = เกมเดิม 100% · ตรรกะทั้งหมดอยู่ใน Sp5NpcRules (pure)
    /// · idiom เดิม: AccessTools resolve ไม่ครบ = ไม่ติดทั้งชุด, try/catch กลืน + นับ error,
    ///   Uninstall ตรวจ owned patches คืน 0
    /// </summary>
    internal static class Sp5NpcGate
    {
        private const string HarmonyId = "longyincoop.sp5npc.gate";
        private static HarmonyLib.Harmony _harmony;
        private static bool _installed;

        private static MethodInfo _tGenerateHero;
        private static MethodInfo _tGetRecommend;
        private static MethodInfo _tGetMax;
        private static MethodInfo _tWorldAdd;

        private static long _err;
        private static long _shrinkRounds;
        private static long _clamped;
        private static long _denied;

        // G1 state: ค่าเดิมของ heroNum ที่กำลัง shrink อยู่ (restore ท้ายรอบ) — main thread เดียว
        private static List<int[]> _originals;
        private static bool _shrunk;

        /// <summary>ผูกแหล่ง snapshot (enabled/role/formatOk/npcPolicy) — โดย default อ่าน net.cfg
        /// ผ่าน S2NetConfig (poll ≤1 s); เทสต์/ผู้เรียกอื่น inject ได้</summary>
        internal static Func<Sp5NpcSnapshot> SnapshotProvider { get; set; }

        internal static bool Installed { get { return _installed; } }
        internal static long Errors { get { return _err; } }

        internal static Sp5NpcLevel CurrentLevel()
        {
            try
            {
                Func<Sp5NpcSnapshot> p = SnapshotProvider ?? DefaultSnapshot;
                Sp5NpcSnapshot s = p();
                return Sp5NpcPolicy.Decide(s.Enabled, s.Role, s.FormatOk, s.NpcPolicy);
            }
            catch
            {
                Interlocked.Increment(ref _err);
                return Sp5NpcLevel.Off;   // fail-closed
            }
        }

        private static S2NetConfig _cfg;

        private static Sp5NpcSnapshot DefaultSnapshot()
        {
            if (_cfg == null) _cfg = new S2NetConfig(CoopPaths.NetCfgPath, 1000);
            S2NetSettings s = _cfg.ForceRead(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond);
            return new Sp5NpcSnapshot(s.Enabled, s.Role, s.FormatOk, s.NpcPolicy);
        }

        internal static bool Install()
        {
            if (_installed) return true;
            try
            {
                MethodInfo h1 = AccessTools.Method(typeof(GameController), "GenerateHero");
                MethodInfo h4a = AccessTools.Method(typeof(GameController), "GetRecommendHeroNum");
                MethodInfo h4b = AccessTools.Method(typeof(GameController), "GetMaxHeroNum");
                MethodInfo h5 = AccessTools.Method(typeof(GameController), "WorldAddNewHero",
                        new Type[] { typeof(int), typeof(int), typeof(bool), typeof(bool) });

                MethodInfo missing = FirstNull(h1, h4a, h4b, h5);
                if (missing != null)
                {
                    CoopLog.Warn("Sp5Npc: target resolve ไม่ครบ (Missing++) — ไม่ติดทั้งชุด: " + missing.Name);
                    return false;
                }
                _tGenerateHero = h1; _tGetRecommend = h4a; _tGetMax = h4b; _tWorldAdd = h5;

                int ownedBefore = CompatInventory.CountOwnedPatches(HarmonyId);
                _harmony = new HarmonyLib.Harmony(HarmonyId);
                Type self = typeof(Sp5NpcGate);
                _harmony.Patch(_tGenerateHero,
                    prefix: new HarmonyMethod(M(self, "GenerateHeroPrefix")),
                    finalizer: new HarmonyMethod(M(self, "GenerateHeroFinalizer")));
                _harmony.Patch(_tGetRecommend,
                    postfix: new HarmonyMethod(M(self, "RecommendPostfix")));
                _harmony.Patch(_tGetMax,
                    postfix: new HarmonyMethod(M(self, "MaxPostfix")));
                _harmony.Patch(_tWorldAdd,
                    prefix: new HarmonyMethod(M(self, "WorldAddPrefix")));

                int ownedAfter = CompatInventory.CountOwnedPatches(HarmonyId);
                if (ownedAfter != ownedBefore + 4)
                {
                    CoopLog.Warn("Sp5Npc: patch count หลังติด = " + (ownedAfter - ownedBefore) + " (คาด 4) — ถอนทั้งชุด");
                    Uninstall();
                    return false;
                }
                _installed = true;
                CoopLog.Msg("Sp5Npc gate installed (fail-closed; ย่อ/คลำ/deny ตาม net.cfg npcPolicy)");
                return true;
            }
            catch (Exception e)
            {
                CoopLog.Warn("Sp5Npc: install ล้มเหลว — " + e.GetType().Name + ": " + e.Message);
                try { Uninstall(); } catch { }
                return false;
            }
        }

        internal static void Uninstall()
        {
            try
            {
                RestoreOriginals();
                if (_harmony != null) _harmony.UnpatchSelf();
                int owned = CompatInventory.CountOwnedPatches(HarmonyId);
                if (owned != 0)
                    CoopLog.Warn("Sp5Npc: หลัง UnpatchSelf ยังเหลือ patch ของ gate = " + owned + " (ต้องเป็น 0)");
            }
            catch (Exception e)
            {
                Interlocked.Increment(ref _err);
                CoopLog.Warn("Sp5Npc: Uninstall ล้มเหลว — " + e.GetType().Name);
            }
            finally
            {
                _harmony = null;
                _installed = false;
            }
        }

        // ---------------- G1: heroNum shrink (ย่อที่ prefix + คืนค่าที่ finalizer) ----------------

        private static void GenerateHeroPrefix()
        {
            try
            {
                Sp5NpcLevel level = CurrentLevel();
                if (!Sp5NpcRules.ShrinkActive(level)) return;
                if (_shrunk) return;   // กัน re-entry ซ้อน (GenerateHero ไม่ควร nested — ป้องกันไว้ก่อน)

                GameDataController gdc = GameDataController.Instance;
                if (gdc == null || gdc.forceLevelData == null) return;   // ยังไม่พร้อม = ไม่แตะอะไร

                double scale = Sp5NpcRules.HeroScaleFor(level);
                List<int[]> originals = new List<int[]>();
                foreach (ForceLevelData entry in gdc.forceLevelData)
                {
                    if (entry == null || entry.heroNum == null) continue;
                    int n = entry.heroNum.Count;
                    int[] old = new int[n];
                    for (int i = 0; i < n; i++)
                    {
                        old[i] = entry.heroNum[i];
                        entry.heroNum[i] = Sp5NpcRules.ShrinkHeroNum(old[i], scale);
                    }
                    originals.Add(old);
                }
                _originals = originals;
                _shrunk = true;
                _shrinkRounds++;
            }
            catch
            {
                Interlocked.Increment(ref _err);
                RestoreOriginals();   // ย่อค้างไม่ได้ — คืนทันทีถ้าทำครึ่งทางแล้วพัง
            }
        }

        private static void GenerateHeroFinalizer(Exception __exception)
        {
            try
            {
                RestoreOriginals();
            }
            catch
            {
                Interlocked.Increment(ref _err);
            }
            // ห้ามกลืน/แก้ exception ของเกม — คืนตาม Harmony contract (void finalizer)
        }

        private static void RestoreOriginals()
        {
            if (!_shrunk) return;
            try
            {
                GameDataController gdc = GameDataController.Instance;
                if (gdc != null && gdc.forceLevelData != null && _originals != null)
                {
                    int idx = 0;
                    foreach (ForceLevelData entry in gdc.forceLevelData)
                    {
                        if (entry == null || entry.heroNum == null) continue;
                        int[] old = _originals[idx];
                        for (int i = 0; i < old.Length && i < entry.heroNum.Count; i++)
                            entry.heroNum[i] = old[i];
                        idx++;
                    }
                }
            }
            finally
            {
                _originals = null;
                _shrunk = false;
            }
        }

        // ---------------- G4: clamp เพดานเติม (เฉพาะ Mid) ----------------

        private static void RecommendPostfix(ref int __result)
        {
            try
            {
                Sp5NpcLevel level = CurrentLevel();
                if (!Sp5NpcRules.ClampCeilingActive(level)) return;
                int clamped = Sp5NpcRules.ClampCeiling(__result, Sp5NpcScales.MidCeilingScale);
                if (clamped != __result) _clamped++;
                __result = clamped;
            }
            catch
            {
                Interlocked.Increment(ref _err);
                // __result คงเดิม = เกมเดิม
            }
        }

        private static void MaxPostfix(ref int __result)
        {
            try
            {
                Sp5NpcLevel level = CurrentLevel();
                if (!Sp5NpcRules.ClampCeilingActive(level)) return;
                int clamped = Sp5NpcRules.ClampCeiling(__result, Sp5NpcScales.MidCeilingScale);
                if (clamped != __result) _clamped++;
                __result = clamped;
            }
            catch
            {
                Interlocked.Increment(ref _err);
            }
        }

        // ---------------- G5: deny เติมรายเดือน (เฉพาะ Strong) ----------------
        // หมายเหตุ contract: prefix ที่คืน reference type จะ skip original **เมื่อคืน non-null
        // เท่านั้น** (คืน null = ผ่านต่อ) — จึงใช้ pattern bool + ref __result สำหรับ "skip ด้วย null"

        private static bool WorldAddPrefix(ref HeroData __result)
        {
            try
            {
                Sp5NpcLevel level = CurrentLevel();
                if (!Sp5NpcRules.DenyRefillActive(level)) return true;   // รัน original ตามปกติ
                _denied++;
                __result = null;   // skip original — caller 2 site มี null-check (OBSERVED)
                return false;
            }
            catch
            {
                Interlocked.Increment(ref _err);
                return true;   // fail-closed: ไม่ปฏิเสธ — รัน original ตามปกติ
            }
        }

        private static MethodInfo FirstNull(params MethodInfo[] ms)
        {
            foreach (MethodInfo m in ms)
                if (m == null) return m;
            return null;
        }

        private static MethodInfo M(Type t, string name)
        {
            return AccessTools.Method(t, name);
        }
    }
}
