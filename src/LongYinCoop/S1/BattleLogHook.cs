using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using HarmonyLib;
using Il2Cpp;

namespace LongYinCoop
{
    /// <summary>
    /// hook ถาวรตัวเดียวของ S1: postfix บน BattleController.AddInfoText ทุก overload
    /// อ่าน argument เท่านั้น — ไม่แก้ค่า, ไม่เรียก method ที่เปลี่ยน state, ไม่เขียน field ของเกม
    /// </summary>
    internal static class BattleLogHook
    {
        private const string HarmonyId = "longyin.coop.s1.battlelog";

        private static HarmonyLib.Harmony _harmony;
        private static readonly List<MethodInfo> Targets = new List<MethodInfo>();

        public static bool Installed;
        public static int PatchApplied;
        public static long Calls;
        public static long TotalDelta;
        public static long Gaps;
        public static long Seq;
        public static string PatchSummary = "";

        private static int _lastInfoCount = -1;

        public static readonly LogRing Log = new LogRing();

        public static bool Apply()
        {
            if (Installed) return true;
            try
            {
                MethodInfo[] methods = typeof(BattleController).GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                List<MethodInfo> overloads = new List<MethodInfo>();
                for (int i = 0; i < methods.Length; i++)
                {
                    if (methods[i].Name == "AddInfoText") overloads.Add(methods[i]);
                }
                if (overloads.Count == 0)
                {
                    CoopLog.Warn(UIStrings.BattleLogTargetMissing);
                    return false;
                }

                MethodInfo postfix = typeof(BattleLogHook).GetMethod(
                    "Postfix", BindingFlags.Static | BindingFlags.NonPublic);
                _harmony = new HarmonyLib.Harmony(HarmonyId);

                StringBuilder summary = new StringBuilder();
                for (int i = 0; i < overloads.Count; i++)
                {
                    MethodInfo m = overloads[i];
                    try
                    {
                        _harmony.Patch(m, postfix: new HarmonyMethod(postfix));
                        Targets.Add(m);
                        PatchApplied++;
                        string line = Describe(m);
                        CoopLog.Msg(string.Format(UIStrings.BattleLogPatchApplied, line));
                        summary.Append(line).Append(" owns=").Append(Owners(m)).Append("; ");
                    }
                    catch (Exception e)
                    {
                        CoopLog.Error(e);
                    }
                }

                PatchSummary = summary.ToString();
                Installed = PatchApplied > 0;
                CoopLog.Msg(string.Format(UIStrings.BattleLogPatchCount, PatchApplied, overloads.Count));
                return Installed;
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
                if (_harmony != null) _harmony.UnpatchSelf();
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
            Installed = false;
        }

        public static void ResetBaseline(int infoCount)
        {
            _lastInfoCount = infoCount;
        }

        // Harmony postfix: อ่าน argument + infoCount หลัง original เท่านั้น
        private static void Postfix(BattleController __instance, string addInfo, bool newLine)
        {
            long t0 = Stopwatch.GetTimestamp();
            try
            {
                int infoCount = __instance == null ? -1 : __instance.infoCount;
                int prev = _lastInfoCount;
                int delta = 0;
                if (prev >= 0 && infoCount >= prev) delta = infoCount - prev;
                if (infoCount >= 0) _lastInfoCount = infoCount;

                LogEvent ev = new LogEvent();
                ev.seq = Interlocked.Increment(ref Seq);
                ev.frame = UnityEngine.Time.frameCount;
                ev.tMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                ev.source = "hook";
                ev.text = addInfo;
                ev.infoCount = infoCount;
                ev.infoDelta = delta;
                ev.battleTime = __instance == null ? 0 : Math.Round(__instance.battleTime, 3);

                Interlocked.Increment(ref Calls);
                if (delta > 0) Interlocked.Add(ref TotalDelta, delta);
                if (delta > 1) Interlocked.Increment(ref Gaps);

                Log.Add(ev);
                if (CaptureWriter.WantsCapture) CaptureWriter.Record("log", ev);
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
            finally
            {
                S1Metrics.AddPostfix(Stopwatch.GetTimestamp() - t0);
            }
        }

        private static string Describe(MethodInfo m)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m.DeclaringType == null ? "?" : m.DeclaringType.FullName).Append('.').Append(m.Name).Append('(');
            ParameterInfo[] ps = m.GetParameters();
            for (int i = 0; i < ps.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(ps[i].ParameterType.Name);
            }
            sb.Append(')');
            return sb.ToString();
        }

        private static string Owners(MethodInfo m)
        {
            try
            {
                Patches info = HarmonyLib.Harmony.GetPatchInfo(m);
                if (info == null || info.Owners == null) return "none";
                return string.Join(",", info.Owners);
            }
            catch
            {
                return "?";
            }
        }
    }
}
