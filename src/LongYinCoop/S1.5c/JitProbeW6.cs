using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace LongYinCoop
{
    /// <summary>
    /// S1.5-c W6 — probe อ่านอย่างเดียวสำหรับหาสาเหตุที่ `spike move` ไม่เกิด path
    ///
    /// ต่างจาก W1/W2 ตรงที่:
    ///   - log ทั้ง state "ก่อน" (prefix) และ "หลัง" (postfix) ของ method เดียวกัน
    ///   - log ตัวตนของ GameObject (name + instanceID + pointer) เพื่อเทียบว่า UI จริงส่งตัวไหน
    ///   - log stack ย่อ (Environment.StackTrace) เฉพาะ N ครั้งแรกต่อ method -> รู้ว่าใครเรียกใคร
    ///   - hook ครบทั้ง 5 จุดของ chain: MoveButtonClicked / BattleGridClicked / OnClick / GenerateMovePath / CancelMove
    ///
    /// กติกา: ห้ามเรียก method ที่เปลี่ยน state · ห้ามเขียน field · ห้ามแตะ __result
    ///        ห่อ try/catch ทุก hook · เขียนเฉพาะ ndjson ของ mod · rate limit ต่อ method
    /// </summary>
    internal static class JitProbeW6
    {
        private const string HarmonyId = "longyincoop.jitprobe.w6";
        private const int MaxStacksPerMethod = 40;
        private const int MaxStackLines = 16;

        private static HarmonyLib.Harmony _harmony;
        private static readonly ProbeSink Sink = new ProbeSink();
        private static long _lastAttemptMs = -2000;

        private static readonly Dictionary<string, int> StackBudget = new Dictionary<string, int>(8);

        public static volatile bool Installed;
        public static int Hits;
        public static int Missing;
        public static int PrefixHits;
        public static int StacksLogged;

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
                Dictionary<string, MethodInfo> found = new Dictionary<string, MethodInfo>(8);

                MethodInfo gridClicked = AccessTools.Method(typeof(BattleController), "BattleGridClicked",
                                                             new Type[] { typeof(GameObject) });
                MethodInfo moveButton = AccessTools.Method(typeof(BattleController), "MoveButtonClicked", Type.EmptyTypes);
                MethodInfo genPath = AccessTools.Method(typeof(BattleController), "GenerateMovePath",
                                                        new Type[] { typeof(GridUnitData) });
                MethodInfo cancelMove = AccessTools.Method(typeof(BattleController), "CancelMove", Type.EmptyTypes);
                MethodInfo onClick = AccessTools.Method(typeof(GridUnitController), "OnClick", Type.EmptyTypes);

                if (gridClicked != null) found["BattleGridClicked"] = gridClicked; else Missing++;
                if (moveButton != null) found["MoveButtonClicked"] = moveButton; else Missing++;
                if (genPath != null) found["GenerateMovePath"] = genPath; else Missing++;
                if (cancelMove != null) found["CancelMove"] = cancelMove; else Missing++;
                if (onClick != null) found["OnClick"] = onClick; else Missing++;

                if (found.Count == 0)
                {
                    CoopLog.Warn("JitProbe W6: no target resolved (missing=" + Missing + ") — not installed");
                    return false;
                }

                Sink.Open("jitprobe-w6");
                _harmony = new HarmonyLib.Harmony(HarmonyId);

                MethodInfo pGridPre = Hook("GridClickedPrefix");
                MethodInfo pGridPost = Hook("GridClickedPostfix");
                MethodInfo pMovePre = Hook("MoveButtonClickedPrefix");
                MethodInfo pMovePost = Hook("MoveButtonClickedPostfix");
                MethodInfo pPathPre = Hook("GenerateMovePathPrefix");
                MethodInfo pPathPost = Hook("GenerateMovePathPostfix");
                MethodInfo pCancel = Hook("CancelMovePostfix");
                MethodInfo pOnClick = Hook("OnClickPostfix");

                if (found.ContainsKey("BattleGridClicked"))
                {
                    _harmony.Patch(found["BattleGridClicked"], prefix: new HarmonyMethod(pGridPre),
                                                                 postfix: new HarmonyMethod(pGridPost));
                }
                if (found.ContainsKey("MoveButtonClicked"))
                {
                    _harmony.Patch(found["MoveButtonClicked"], prefix: new HarmonyMethod(pMovePre),
                                                                 postfix: new HarmonyMethod(pMovePost));
                }
                if (found.ContainsKey("GenerateMovePath"))
                {
                    _harmony.Patch(found["GenerateMovePath"], prefix: new HarmonyMethod(pPathPre),
                                                                 postfix: new HarmonyMethod(pPathPost));
                }
                if (found.ContainsKey("CancelMove"))
                    _harmony.Patch(found["CancelMove"], postfix: new HarmonyMethod(pCancel));
                if (found.ContainsKey("OnClick"))
                    _harmony.Patch(found["OnClick"], postfix: new HarmonyMethod(pOnClick));

                Installed = true;
                CoopLog.Msg("JitProbe W6 installed: " + string.Join(",", new List<string>(found.Keys).ToArray())
                            + " missing=" + Missing + " -> " + Sink.Path);
                return true;
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                return false;
            }
        }

        private static MethodInfo Hook(string name)
        {
            return typeof(JitProbeW6).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        }

        public static void Uninstall()
        {
            if (!Installed) return;
            try
            {
                int before = CompatInventory.CountOwnedPatches(HarmonyId);
                if (_harmony != null) _harmony.UnpatchSelf();
                Installed = false;
                CoopLog.Msg(string.Format("JitProbe W6 removed: patches {0}->{1} hits={2} prefix={3} stacks={4} dropped={5}",
                                          before, CompatInventory.CountOwnedPatches(HarmonyId),
                                          Hits, PrefixHits, StacksLogged, Sink.Dropped));
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
        }

        /// <summary>state ที่อ่านได้ ณ ขณะนั้น (อ่านอย่างเดียว) — ใช้ทั้ง prefix และ postfix</summary>
        private static string StateFields(BattleController bc)
        {
            try
            {
                if (bc == null) return ",\"state\":null";
                StringBuilder sb = new StringBuilder(128);
                sb.Append(",\"state\":").Append((int)bc.nowActiveState);
                sb.Append(",\"paused\":").Append(bc.battlePaused ? "true" : "false");
                BattleUnit u = bc.nowActiveUnit;
                int row = -1, col = -1;
                bool moved = false;
                try
                {
                    if (u != null)
                    {
                        GridUnitData g = u.mapGrid;
                        if (g != null) { row = g.row; col = g.column; }
                        moved = u.moved;
                    }
                }
                catch { }
                sb.Append(",\"activeRow\":").Append(row);
                sb.Append(",\"activeCol\":").Append(col);
                sb.Append(",\"moved\":").Append(moved ? "true" : "false");
                return sb.ToString();
            }
            catch { return ""; }
        }

        /// <summary>ตัวตนของ GameObject: ชื่อ + instanceID + pointer (อ่านเท่านั้น)</summary>
        private static string ObjectFields(GameObject go)
        {
            if (go == null) return ",\"obj\":null";
            string name = null;
            int instanceId = 0;
            long ptr = 0;
            int row = -1, col = -1;
            try { name = go.name; } catch { }
            try { instanceId = go.GetInstanceID(); } catch { }
            try { ptr = go.Pointer.ToInt64(); } catch { }
            try
            {
                GridUnitController gu = go.GetComponent<GridUnitController>();
                if (gu != null)
                {
                    GridUnitData d = gu.gridData;
                    if (d != null) { row = d.row; col = d.column; }
                }
            }
            catch { }
            StringBuilder sb = new StringBuilder(160);
            sb.Append(",\"obj\":\"").Append(ProbeSink.Esc(name)).Append('"');
            sb.Append(",\"objId\":").Append(instanceId);
            sb.Append(",\"objPtr\":").Append(ptr);
            sb.Append(",\"objRow\":").Append(row);
            sb.Append(",\"objCol\":").Append(col);
            return sb.ToString();
        }

        private static string StackFields(string method)
        {
            try
            {
                int left;
                if (!StackBudget.TryGetValue(method, out left)) left = MaxStacksPerMethod;
                if (left <= 0) return "";
                StackBudget[method] = left - 1;
                StacksLogged++;

                string st = Environment.StackTrace;
                if (string.IsNullOrEmpty(st)) return "";
                string[] lines = st.Split('\n');
                StringBuilder sb = new StringBuilder(512);
                int used = 0;
                for (int i = 0; i < lines.Length && used < MaxStackLines; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0) continue;
                    if (line.IndexOf("Environment.get_StackTrace", StringComparison.Ordinal) >= 0) continue;
                    if (line.IndexOf("JitProbeW6", StringComparison.Ordinal) >= 0) continue;
                    if (line.IndexOf("HarmonyLib", StringComparison.Ordinal) >= 0) continue;
                    if (used > 0) sb.Append(" | ");
                    sb.Append(ProbeSink.Esc(line));
                    used++;
                }
                if (used == 0) return "";
                return ",\"stack\":\"" + sb + "\"";
            }
            catch { return ""; }
        }

        // ---- BattleGridClicked ----

        private static void GridClickedPrefix(BattleController __instance, GameObject clickedGrid)
        {
            try
            {
                if (!Sink.Allow("BattleGridClicked.pre")) return;
                PrefixHits++;
                Sink.Write("BattleGridClicked.pre",
                           ObjectFields(clickedGrid) + StateFields(__instance) + StackFields("BattleGridClicked"));
            }
            catch { }
        }

        private static void GridClickedPostfix(BattleController __instance, GameObject clickedGrid)
        {
            try
            {
                if (!Sink.Allow("BattleGridClicked")) return;
                Hits++;
                Sink.Write("BattleGridClicked.post",
                           ObjectFields(clickedGrid) + StateFields(__instance));
            }
            catch { }
        }

        // ---- MoveButtonClicked ----

        private static void MoveButtonClickedPrefix(BattleController __instance)
        {
            try
            {
                if (!Sink.Allow("MoveButtonClicked.pre")) return;
                PrefixHits++;
                Sink.Write("MoveButtonClicked.pre", StateFields(__instance) + StackFields("MoveButtonClicked"));
            }
            catch { }
        }

        private static void MoveButtonClickedPostfix(BattleController __instance)
        {
            try
            {
                if (!Sink.Allow("MoveButtonClicked")) return;
                Hits++;
                Sink.Write("MoveButtonClicked.post", StateFields(__instance));
            }
            catch { }
        }

        // ---- GenerateMovePath ----

        private static void GenerateMovePathPrefix(BattleController __instance, GridUnitData targetGrid)
        {
            try
            {
                if (!Sink.Allow("GenerateMovePath.pre")) return;
                PrefixHits++;
                int row = -1, col = -1;
                try
                {
                    if (targetGrid != null) { row = targetGrid.row; col = targetGrid.column; }
                }
                catch { }
                Sink.Write("GenerateMovePath.pre",
                           ",\"targetRow\":" + row + ",\"targetCol\":" + col
                           + StateFields(__instance) + StackFields("GenerateMovePath"));
            }
            catch { }
        }

        private static void GenerateMovePathPostfix(BattleController __instance, GridUnitData targetGrid)
        {
            try
            {
                if (!Sink.Allow("GenerateMovePath")) return;
                Hits++;
                int row = -1, col = -1;
                try
                {
                    if (targetGrid != null) { row = targetGrid.row; col = targetGrid.column; }
                }
                catch { }
                Sink.Write("GenerateMovePath.post",
                           ",\"targetRow\":" + row + ",\"targetCol\":" + col + StateFields(__instance));
            }
            catch { }
        }

        // ---- CancelMove / OnClick ----

        private static void CancelMovePostfix(BattleController __instance)
        {
            try
            {
                if (!Sink.Allow("CancelMove")) return;
                Hits++;
                Sink.Write("CancelMove.post", StateFields(__instance) + StackFields("CancelMove"));
            }
            catch { }
        }

        private static void OnClickPostfix(GridUnitController __instance)
        {
            try
            {
                if (!Sink.Allow("OnClick")) return;
                Hits++;
                string fields = "";
                try
                {
                    if (__instance != null)
                    {
                        GameObject go = __instance.gameObject;
                        fields = ObjectFields(go);
                    }
                }
                catch { }
                BattleController bc = null;
                try { bc = BattleController.Instance; } catch { }
                Sink.Write("OnClick.post", fields + StateFields(bc) + StackFields("OnClick"));
            }
            catch { }
        }
    }
}
