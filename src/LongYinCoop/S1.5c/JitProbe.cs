using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace LongYinCoop
{
    /// <summary>
    /// S1.5-c W1 — postfix อ่านอย่างเดียวบน 2 method เท่านั้น:
    ///   GridUnitController.OnClick()                 -> ยืนยันว่า "คลิกช่อง" มาทางนี้จริง
    ///   BattleController.BattleGridClicked(GameObject) -> entry หลักของ move/attack + argument
    /// กติกา: ห้ามเรียก method เกม, ห้ามเขียน field, ห้ามแก้ __result/__instance,
    ///        ห่อ try/catch ทั้งก้อน, เขียนเฉพาะไฟล์ ndjson ของ mod, มี rate limit ต่อ method
    /// </summary>
    internal static class JitProbe
    {
        private const string HarmonyId = "longyincoop.jitprobe.w1";
        private const int MaxPerMethodPerSecond = 200;

        private static HarmonyLib.Harmony _harmony;
        private static string _path;
        private static readonly object FileSync = new object();
        private static readonly Stopwatch Clock = Stopwatch.StartNew();

        private static long _windowStartMs;
        private static int _onClickWindow;
        private static int _gridWindow;
        private static long _lastAttemptMs = -2000;   // กัน retry ถี่เมื่อ target resolve ไม่ได้

        public static volatile bool Installed;
        public static int OnClickHits;
        public static int GridClickedHits;
        public static int Dropped;

        public static string FilePath { get { return _path; } }

        public static bool Apply(bool wanted)
        {
            if (!wanted)
            {
                Uninstall();
                return false;
            }
            if (Installed) return true;

            // ถ้า resolve ไม่ได้ อย่าให้ Tick เรียกซ้ำทุกเฟรม (เตือนซ้ำไม่เกิน 1 ครั้ง/วินาที)
            long now = Clock.ElapsedMilliseconds;
            if (now - _lastAttemptMs < 1000) return false;
            _lastAttemptMs = now;

            try
            {
                MethodInfo onClick = AccessTools.Method(typeof(GridUnitController), "OnClick");
                MethodInfo gridClicked = AccessTools.Method(typeof(BattleController), "BattleGridClicked",
                                                             new Type[] { typeof(GameObject) });
                if (onClick == null || gridClicked == null)
                {
                    CoopLog.Warn("JitProbe: target missing (OnClick=" + (onClick != null) +
                                 " BattleGridClicked=" + (gridClicked != null) + ")");
                    return false;
                }

                EnsureFile();

                _harmony = new HarmonyLib.Harmony(HarmonyId);
                MethodInfo pOnClick = typeof(JitProbe).GetMethod("OnClickPostfix",
                                          BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo pGrid = typeof(JitProbe).GetMethod("GridClickedPostfix",
                                       BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(onClick, postfix: new HarmonyMethod(pOnClick));
                _harmony.Patch(gridClicked, postfix: new HarmonyMethod(pGrid));

                Installed = true;
                CoopLog.Msg("JitProbe W1 installed: GridUnitController.OnClick + BattleController.BattleGridClicked -> " + _path);
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
                if (_harmony != null) _harmony.UnpatchSelf();
                Installed = false;
                CoopLog.Msg(string.Format(
                    "JitProbe W1 removed: onClick={0} battleGridClicked={1} dropped={2}",
                    OnClickHits, GridClickedHits, Dropped));
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
        }

        private static void EnsureFile()
        {
            if (_path != null) return;
            string dir = Path.Combine(CoopPaths.EnsureUserDataDir(), "CoopSpike");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "jitprobe-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".ndjson");
        }

        /// <summary>postfix ของ GridUnitController.OnClick — อ่าน gridData.row/column + ชื่อ GameObject เท่านั้น</summary>
        private static void OnClickPostfix(GridUnitController __instance)
        {
            try
            {
                Interlocked.Increment(ref OnClickHits);
                if (!Allow(ref _onClickWindow)) { Interlocked.Increment(ref Dropped); return; }

                int row = -1, col = -1;
                string objName = null;
                if (__instance != null)
                {
                    GridUnitData grid = __instance.gridData;
                    if (grid != null)
                    {
                        row = grid.row;
                        col = grid.column;
                    }
                    GameObject go = __instance.gameObject;
                    if (go != null) objName = go.name;
                }
                WriteEvent("GridUnitController.OnClick", row, col, objName);
            }
            catch
            {
                // ห้ามให้ exception หลุดเข้าเกม
            }
        }

        /// <summary>postfix ของ BattleController.BattleGridClicked(GameObject) — อ่าน argument เท่านั้น</summary>
        private static void GridClickedPostfix(GameObject clickedGrid)
        {
            try
            {
                Interlocked.Increment(ref GridClickedHits);
                if (!Allow(ref _gridWindow)) { Interlocked.Increment(ref Dropped); return; }

                string objName = null;
                try
                {
                    if (clickedGrid != null) objName = clickedGrid.name;
                }
                catch { }
                WriteEvent("BattleController.BattleGridClicked", -1, -1, objName);
            }
            catch
            {
            }
        }

        private static bool Allow(ref int window)
        {
            long now = Clock.ElapsedMilliseconds;
            if (now - _windowStartMs >= 1000)
            {
                _windowStartMs = now;
                _onClickWindow = 0;
                _gridWindow = 0;
            }
            if (window >= MaxPerMethodPerSecond) return false;
            window++;
            return true;
        }

        private static void WriteEvent(string method, int row, int col, string objName)
        {
            string path = _path;
            if (path == null) return;

            StringBuilder sb = new StringBuilder(192);
            sb.Append("{\"t\":").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            sb.Append(",\"frame\":").Append(Time.frameCount);
            sb.Append(",\"method\":\"").Append(method).Append('"');
            sb.Append(",\"row\":").Append(row);
            sb.Append(",\"col\":").Append(col);
            sb.Append(",\"obj\":\"").Append(Escape(objName)).Append('"');
            sb.Append('}');

            lock (FileSync)
            {
                try { File.AppendAllText(path, sb.ToString() + Environment.NewLine, Encoding.UTF8); }
                catch { }
            }
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else if (c < ' ') sb.Append(' ');
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
