using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Il2Cpp;

namespace LongYinCoop
{
    /// <summary>
    /// probe ชั่วคราว: prefix no-op บน GameController.Update
    /// วัตถุประสงค์เดียว = วัด ManagedThreadId และนับ hit — ห้ามแก้ argument ห้ามแตะ state เกม
    /// </summary>
    internal static class HarmonyProbe
    {
        private const string HarmonyId = "longyin.coop.probe";

        private static HarmonyLib.Harmony _harmony;
        private static MethodInfo _target;
        private static readonly ConcurrentDictionary<int, int> Threads = new ConcurrentDictionary<int, int>();

        public static volatile bool Installed;
        public static volatile int ProbeThreadId;
        public static int Hits;

        public static MethodInfo Target { get { return _target; } }

        public static bool Apply(bool wanted)
        {
            if (!wanted)
            {
                Uninstall();
                return false;
            }
            if (Installed) return true;

            try
            {
                _target = AccessTools.Method(typeof(GameController), "Update");
                if (_target == null) _target = AccessTools.Method("GameController:Update");
                if (_target == null)
                {
                    CoopLog.Warn("probe: GameController.Update not found");
                    return false;
                }

                _harmony = new HarmonyLib.Harmony(HarmonyId);
                MethodInfo prefix = typeof(HarmonyProbe).GetMethod(
                    "Prefix", BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(_target, new HarmonyMethod(prefix));
                Installed = true;
                CoopLog.Msg("probe: patched " + _target.DeclaringType.FullName + "." + _target.Name +
                            " (thread measurement only)");
                return true;
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                return false;
            }
        }

        public static void Uninstall()
        {
            if (!Installed) return;
            try
            {
                int before = CompatInventory.CountOwnedPatches(HarmonyId);
                if (_harmony != null) _harmony.UnpatchSelf();
                Installed = false;
                int after = CompatInventory.CountOwnedPatches(HarmonyId);
                CoopLog.Msg("probe: unpatched (owned patches before=" + before + " after=" + after + ")");
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
        }

        // Harmony prefix: ห้ามแก้ argument, ห้ามคืนค่า false (ต้องให้ Update ทำงานตามปกติ)
        private static void Prefix()
        {
            Interlocked.Increment(ref Hits);
            int tid = Environment.CurrentManagedThreadId;
            ProbeThreadId = tid;
            Threads.TryAdd(tid, 0);
        }

        public static string ThreadIdsCsv()
        {
            return string.Join(",", Threads.Keys);
        }
    }
}
