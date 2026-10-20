using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace LongYinCoop
{
    /// <summary>
    /// sink ร่วมของ probe wave: เปิดไฟล์เดียวต่อ wave, rate limit ต่อ method, เขียนแบบ append
    /// ห้ามใช้เขียนอย่างอื่นนอกจากไฟล์ ndjson ของ mod
    /// </summary>
    internal sealed class ProbeSink
    {
        private const int MaxPerMethodPerSecond = 200;

        private readonly object _sync = new object();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>(8);
        private long _windowStartMs;
        private string _path;
        private int _dropped;

        public string Path { get { return _path; } }
        public int Dropped { get { return _dropped; } }

        public void Open(string prefix)
        {
            if (_path != null) return;
            string dir = System.IO.Path.Combine(CoopPaths.EnsureUserDataDir(), "CoopSpike");
            Directory.CreateDirectory(dir);
            _path = System.IO.Path.Combine(dir, prefix + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".ndjson");
        }

        public bool Allow(string method)
        {
            long now = _clock.ElapsedMilliseconds;
            if (now - _windowStartMs >= 1000)
            {
                _windowStartMs = now;
                _counts.Clear();
            }
            int n;
            _counts.TryGetValue(method, out n);
            if (n >= MaxPerMethodPerSecond)
            {
                _dropped++;
                return false;
            }
            _counts[method] = n + 1;
            return true;
        }

        /// <summary>เขียน 1 event: t/frame/method + fields ที่ประกอบมาแล้ว (ขึ้นต้นด้วย comma)</summary>
        public void Write(string method, string fields)
        {
            string path = _path;
            if (path == null) return;

            StringBuilder sb = new StringBuilder(256);
            sb.Append("{\"t\":").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            sb.Append(",\"frame\":").Append(Time.frameCount);
            sb.Append(",\"method\":\"").Append(method).Append('"');
            sb.Append(fields);
            sb.Append('}');

            lock (_sync)
            {
                try { File.AppendAllText(path, sb.ToString() + Environment.NewLine, Encoding.UTF8); }
                catch { }
            }
        }

        public static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c < ' ') sb.Append(' ');
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// S1.5-c W2 — postfix อ่านอย่างเดียว 4 method ฝั่ง move (ลำดับ wave ตาม jit-dump-analysis §6.2):
    ///   BattleController.MoveButtonClicked()
    ///   BattleController.GenerateMovePath(GridUnitData targetGrid)
    ///   BattleController.CancelMove()
    ///   BattleController.RegretMove()
    /// ห้ามเรียก method เกม · ห้ามเขียน field · ห้ามแตะ __result · exception ต้องไม่หลุด
    /// </summary>
    internal static class JitProbeW2
    {
        private const string HarmonyId = "longyincoop.jitprobe.w2";

        private static HarmonyLib.Harmony _harmony;
        private static readonly ProbeSink Sink = new ProbeSink();
        private static long _lastAttemptMs = -2000;

        public static volatile bool Installed;
        public static int Hits;
        public static int Missing;

        public static string FilePath { get { return Sink.Path; } }
        public static int Dropped { get { return Sink.Dropped; } }

        public static bool Apply(bool wanted)
        {
            if (!wanted)
            {
                Uninstall();
                return false;
            }
            if (Installed) return true;

            long now = Environment.TickCount;
            if (now - _lastAttemptMs < 1000) return false;
            _lastAttemptMs = now;

            try
            {
                // resolve ทีละตัว: ตัวที่ resolve ไม่ได้ = ข้าม (ห้ามเดา ห้ามใช้ offset)
                Dictionary<string, MethodInfo> found = new Dictionary<string, MethodInfo>();
                string[] names = { "MoveButtonClicked", "GenerateMovePath", "CancelMove", "RegretMove" };
                for (int i = 0; i < names.Length; i++)
                {
                    MethodInfo m = names[i] == "GenerateMovePath"
                        ? AccessTools.Method(typeof(BattleController), names[i], new Type[] { typeof(GridUnitData) })
                        : AccessTools.Method(typeof(BattleController), names[i], Type.EmptyTypes);
                    if (m != null) found[names[i]] = m;
                    else Missing++;
                }

                if (found.Count == 0)
                {
                    CoopLog.Warn("JitProbe W2: no target resolved (missing=" + Missing + ") — not installed");
                    return false;
                }

                Sink.Open("jitprobe-w2");

                _harmony = new HarmonyLib.Harmony(HarmonyId);
                MethodInfo pMove = typeof(JitProbeW2).GetMethod("MoveButtonClickedPostfix",
                                       BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo pPath = typeof(JitProbeW2).GetMethod("GenerateMovePathPostfix",
                                       BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo pCancel = typeof(JitProbeW2).GetMethod("CancelMovePostfix",
                                       BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo pRegret = typeof(JitProbeW2).GetMethod("RegretMovePostfix",
                                       BindingFlags.Static | BindingFlags.NonPublic);

                if (found.ContainsKey("MoveButtonClicked"))
                    _harmony.Patch(found["MoveButtonClicked"], postfix: new HarmonyMethod(pMove));
                if (found.ContainsKey("GenerateMovePath"))
                    _harmony.Patch(found["GenerateMovePath"], postfix: new HarmonyMethod(pPath));
                if (found.ContainsKey("CancelMove"))
                    _harmony.Patch(found["CancelMove"], postfix: new HarmonyMethod(pCancel));
                if (found.ContainsKey("RegretMove"))
                    _harmony.Patch(found["RegretMove"], postfix: new HarmonyMethod(pRegret));

                Installed = true;
                CoopLog.Msg("JitProbe W2 installed: " + string.Join(",", new List<string>(found.Keys).ToArray())
                            + " missing=" + Missing + " -> " + Sink.Path);
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
                CoopLog.Msg(string.Format("JitProbe W2 removed: patches {0}->{1} hits={2} dropped={3}",
                                          before, CompatInventory.CountOwnedPatches(HarmonyId),
                                          Hits, Sink.Dropped));
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
        }

        private static string State(BattleController bc)
        {
            try
            {
                if (bc == null) return "";
                BattleUnit u = bc.nowActiveUnit;
                int row = -1, col = -1;
                if (u != null)
                {
                    GridUnitData g = u.mapGrid;
                    if (g != null) { row = g.row; col = g.column; }
                }
                StringBuilder sb = new StringBuilder(96);
                sb.Append(",\"nowActiveState\":").Append((int)bc.nowActiveState);
                sb.Append(",\"paused\":").Append(bc.battlePaused ? "true" : "false");
                sb.Append(",\"row\":").Append(row);
                sb.Append(",\"col\":").Append(col);
                return sb.ToString();
            }
            catch { return ""; }
        }

        private static void MoveButtonClickedPostfix(BattleController __instance)
        {
            try
            {
                if (!Sink.Allow("MoveButtonClicked")) return;
                Hits++;
                Sink.Write("BattleController.MoveButtonClicked", State(__instance));
            }
            catch { }
        }

        private static void GenerateMovePathPostfix(BattleController __instance, GridUnitData targetGrid)
        {
            try
            {
                if (!Sink.Allow("GenerateMovePath")) return;
                Hits++;
                int tRow = -1, tCol = -1;
                try
                {
                    if (targetGrid != null) { tRow = targetGrid.row; tCol = targetGrid.column; }
                }
                catch { }
                Sink.Write("BattleController.GenerateMovePath",
                           ",\"targetRow\":" + tRow + ",\"targetCol\":" + tCol + State(__instance));
            }
            catch { }
        }

        private static void CancelMovePostfix(BattleController __instance)
        {
            try
            {
                if (!Sink.Allow("CancelMove")) return;
                Hits++;
                Sink.Write("BattleController.CancelMove", State(__instance));
            }
            catch { }
        }

        private static void RegretMovePostfix(BattleController __instance)
        {
            try
            {
                if (!Sink.Allow("RegretMove")) return;
                Hits++;
                Sink.Write("BattleController.RegretMove", State(__instance));
            }
            catch { }
        }
    }

    /// <summary>
    /// S1.5-c W4 — postfix อ่านอย่างเดียวบน BattleController.BattlePauseButtonClicked(GameObject)
    /// อ่านเพิ่ม: battlePaused, Time.timeScale, battleTime, AIWaitTime, nowActiveState (ค่าหลัง call)
    /// เพื่อดูว่าใครเป็นคน toggle pause และมีค่าอะไรเปลี่ยนตอนกดจริง (J6)
    /// </summary>
    internal static class JitProbeW4
    {
        private const string HarmonyId = "longyincoop.jitprobe.w4";

        private static HarmonyLib.Harmony _harmony;
        private static readonly ProbeSink Sink = new ProbeSink();
        private static long _lastAttemptMs = -2000;

        public static volatile bool Installed;
        public static int Hits;
        public static int Missing;

        public static string FilePath { get { return Sink.Path; } }
        public static int Dropped { get { return Sink.Dropped; } }

        public static bool Apply(bool wanted)
        {
            if (!wanted)
            {
                Uninstall();
                return false;
            }
            if (Installed) return true;

            long now = Environment.TickCount;
            if (now - _lastAttemptMs < 1000) return false;
            _lastAttemptMs = now;

            try
            {
                MethodInfo target = AccessTools.Method(typeof(BattleController), "BattlePauseButtonClicked",
                                                        new Type[] { typeof(GameObject) });
                if (target == null)
                {
                    Missing++;
                    CoopLog.Warn("JitProbe W4: BattlePauseButtonClicked(GameObject) not resolved — not installed");
                    return false;
                }

                Sink.Open("jitprobe-w4");
                _harmony = new HarmonyLib.Harmony(HarmonyId);
                MethodInfo postfix = typeof(JitProbeW4).GetMethod("PauseButtonClickedPostfix",
                                        BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, postfix: new HarmonyMethod(postfix));

                Installed = true;
                CoopLog.Msg("JitProbe W4 installed: BattleController.BattlePauseButtonClicked -> " + Sink.Path);
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
                CoopLog.Msg(string.Format("JitProbe W4 removed: patches {0}->{1} hits={2} dropped={3}",
                                          before, CompatInventory.CountOwnedPatches(HarmonyId),
                                          Hits, Sink.Dropped));
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
        }

        private static void PauseButtonClickedPostfix(BattleController __instance, GameObject buttonClicked)
        {
            try
            {
                if (!Sink.Allow("BattlePauseButtonClicked")) return;
                Hits++;

                string objName = null;
                try { if (buttonClicked != null) objName = buttonClicked.name; }
                catch { }

                StringBuilder sb = new StringBuilder(160);
                sb.Append(",\"obj\":\"").Append(ProbeSink.Esc(objName)).Append('"');
                try
                {
                    sb.Append(",\"timeScale\":").Append(UnityEngine.Time.timeScale.ToString("0.###"));
                    sb.Append(",\"unscaledDelta\":").Append(UnityEngine.Time.unscaledDeltaTime.ToString("0.###"));
                }
                catch { }
                try
                {
                    if (__instance != null)
                    {
                        sb.Append(",\"paused\":").Append(__instance.battlePaused ? "true" : "false");
                        sb.Append(",\"nowActiveState\":").Append((int)__instance.nowActiveState);
                        sb.Append(",\"battleTime\":").Append(__instance.battleTime.ToString("0.###"));
                        sb.Append(",\"aiWaitTime\":").Append(__instance.AIWaitTime.ToString("0.###"));
                    }
                }
                catch { }
                Sink.Write("BattleController.BattlePauseButtonClicked", sb.ToString());
            }
            catch { }
        }
    }
}
