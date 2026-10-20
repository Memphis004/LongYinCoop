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
    /// S1.5e W7 — probe อ่านอย่างเดียวสำหรับวัด "chain ของการโจมตี" จาก UI จริง
    ///
    /// เหตุผล: S1.5-c W1/W2/W4/W6 ไม่เคยติด hook บน method ของ attack เลย
    ///         ลำดับ call จริงของ attack จึงยังไม่มีหลักฐาน (ห้ามเดาจาก move)
    ///
    /// กติกา:
    /// - prefix/postfix อ่าน argument + อ่าน field เท่านั้น (typed interop ทั้งหมด)
    /// - ห้ามเรียก method ของเกม · ห้ามเขียน field · ห้ามแก้ __result
    /// - ทุก hook ห่อ try/catch · เขียนเฉพาะ ndjson ของ mod · rate limit ต่อ method
    /// - method ที่ resolve ไม่ได้ = ข้าม (ห้ามเดา offset)
    /// </summary>
    internal static class JitProbeW7
    {
        private const string HarmonyId = "longyincoop.jitprobe.w7";
        private const int MaxStacksPerMethod = 30;
        private const int MaxStackLines = 14;
        private const int MaxRangeSample = 12;

        private static HarmonyLib.Harmony _harmony;
        private static readonly ProbeSink Sink = new ProbeSink();
        private static long _lastAttemptMs = -2000;
        private static readonly Dictionary<string, int> StackBudget = new Dictionary<string, int>(16);

        private static PropertyInfo _confirmedProp;
        private static FieldInfo _confirmedField;
        private static bool _confirmedResolved;
        private static PropertyInfo _turnAttackedProp;
        private static bool _turnAttackedResolved;

        public static volatile bool Installed;
        public static int Hits;
        public static int PrefixHits;
        public static int Missing;
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
                Dictionary<string, MethodInfo> found = new Dictionary<string, MethodInfo>(16);
                Add(found, typeof(BattleController), "BattleGridClicked", new Type[] { typeof(GameObject) });
                Add(found, typeof(BattleController), "ChangeActiveSkill", new Type[] { typeof(KungfuSkillLvData), typeof(Vector3) });
                Add(found, typeof(BattleController), "ChangeNowActiveAttackSkill", new Type[] { typeof(KungfuSkillLvData), typeof(Vector3) });
                Add(found, typeof(BattleController), "RefreshAttackRange", Type.EmptyTypes);
                Add(found, typeof(BattleController), "CancelAttack", Type.EmptyTypes);
                Add(found, typeof(BattleController), "PlayBattleUnitAttack", Type.EmptyTypes);
                Add(found, typeof(BattleController), "BattleUnitAttackStart", Type.EmptyTypes);
                Add(found, typeof(BattleController), "BattleUnitAttackHappen", Type.EmptyTypes);
                Add(found, typeof(BattleController), "BattleUnitAttackHit", new Type[] { typeof(GridUnitData), typeof(float) });
                Add(found, typeof(BattleController), "BattleUnitAttackFinish", Type.EmptyTypes);
                Add(found, typeof(BattleController), "BattleUnitAttackEnd", new Type[] { typeof(float) });
                Add(found, typeof(BattleController), "ManageDamage", new Type[] { typeof(BattleUnit), typeof(BattleUnit), typeof(bool) });
                Add(found, typeof(GridUnitController), "OnClick", Type.EmptyTypes);

                if (found.Count == 0)
                {
                    CoopLog.Warn("JitProbe W7: no target resolved (missing=" + Missing + ") — not installed");
                    return false;
                }

                Sink.Open("jitprobe-w7");
                _harmony = new HarmonyLib.Harmony(HarmonyId);

                PatchBoth(found, "BattleGridClicked", "GridClickedPrefix", "GridClickedPostfix");
                PatchBoth(found, "ChangeActiveSkill", "ChangeActiveSkillPrefix", "ChangeActiveSkillPostfix");
                PatchBoth(found, "ChangeNowActiveAttackSkill", "ChangeNowAttackSkillPrefix", "ChangeNowAttackSkillPostfix");
                PatchBoth(found, "RefreshAttackRange", "RefreshAttackRangePrefix", "RefreshAttackRangePostfix");
                PatchBoth(found, "ManageDamage", "ManageDamagePrefix", "ManageDamagePostfix");
                PatchPost(found, "CancelAttack", "CancelAttackPostfix");
                PatchPost(found, "PlayBattleUnitAttack", "PlayBattleUnitAttackPostfix");
                PatchPost(found, "BattleUnitAttackStart", "StartPostfix");
                PatchPost(found, "BattleUnitAttackHappen", "HappenPostfix");
                PatchPost(found, "BattleUnitAttackHit", "HitPostfix");
                PatchPost(found, "BattleUnitAttackFinish", "FinishPostfix");
                PatchPost(found, "BattleUnitAttackEnd", "EndPostfix");
                PatchPost(found, "OnClick", "OnClickPostfix");

                Installed = true;
                CoopLog.Msg("JitProbe W7 installed: " + string.Join(",", new List<string>(found.Keys).ToArray())
                            + " missing=" + Missing + " -> " + Sink.Path);

                BattleController bc = null;
                try { bc = BattleController.Instance; } catch { }
                Sink.Write("W7.start", Snapshot(bc));
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
                CoopLog.Msg(string.Format(
                    "JitProbe W7 removed: patches {0}->{1} hits={2} prefix={3} stacks={4} dropped={5}",
                    before, CompatInventory.CountOwnedPatches(HarmonyId), Hits, PrefixHits, StacksLogged, Sink.Dropped));
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
        }

        // ------------------------------------------------------------ install helpers

        private static void Add(Dictionary<string, MethodInfo> found, Type type, string name, Type[] args)
        {
            try
            {
                MethodInfo m = AccessTools.Method(type, name, args);
                if (m == null)
                {
                    Missing++;
                    CoopLog.Warn("JitProbe W7: missing " + type.Name + "." + name);
                    return;
                }
                found[name] = m;
            }
            catch (Exception e)
            {
                Missing++;
                CoopLog.Warn("JitProbe W7: resolve failed " + type.Name + "." + name + ": " + e.Message);
            }
        }

        private static void PatchBoth(Dictionary<string, MethodInfo> found, string key, string prefix, string postfix)
        {
            if (!found.ContainsKey(key)) return;
            _harmony.Patch(found[key],
                           prefix: new HarmonyMethod(Hook(prefix)),
                           postfix: new HarmonyMethod(Hook(postfix)));
        }

        private static void PatchPost(Dictionary<string, MethodInfo> found, string key, string postfix)
        {
            if (!found.ContainsKey(key)) return;
            _harmony.Patch(found[key], postfix: new HarmonyMethod(Hook(postfix)));
        }

        private static MethodInfo Hook(string name)
        {
            return typeof(JitProbeW7).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        }

        // ------------------------------------------------------------ field readers (typed, read-only)

        /// <summary>BattleController.attackComfirmed (private bool) — อ่านทาง interop แบบ typed</summary>
        private static string ConfirmedText(BattleController bc)
        {
            if (!_confirmedResolved)
            {
                _confirmedResolved = true;
                try
                {
                    _confirmedProp = typeof(BattleController).GetProperty("attackComfirmed",
                                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (_confirmedProp == null)
                    {
                        _confirmedField = typeof(BattleController).GetField("attackComfirmed",
                                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                }
                catch (Exception e) { CoopLog.Warn("JitProbe W7: attackComfirmed resolve failed: " + e.Message); }
                if (_confirmedProp == null && _confirmedField == null)
                    CoopLog.Warn("JitProbe W7: attackComfirmed not exposed");
            }
            try
            {
                object v = _confirmedProp != null ? _confirmedProp.GetValue(bc)
                         : (_confirmedField != null ? _confirmedField.GetValue(bc) : null);
                if (v is bool) return ((bool)v) ? "true" : "false";
            }
            catch { }
            return "?";
        }

        private static string TurnAttackedText(BattleController bc)
        {
            if (!_turnAttackedResolved)
            {
                _turnAttackedResolved = true;
                try
                {
                    _turnAttackedProp = typeof(BattleController).GetProperty("nowActiveUnitAttacked",
                                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }
                catch { }
            }
            try
            {
                object v = _turnAttackedProp == null ? null : _turnAttackedProp.GetValue(bc);
                if (v is bool) return ((bool)v) ? "true" : "false";
            }
            catch { }
            return "?";
        }

        // ------------------------------------------------------------ snapshot

        /// <summary>อ่าน state ของศึก ณ ขณะนั้น (ไม่เรียก method ที่เปลี่ยน state)</summary>
        private static string Snapshot(BattleController bc)
        {
            StringBuilder sb = new StringBuilder(384);
            int frame = -1;
            try { frame = Time.frameCount; } catch { }
            sb.Append(",\"frame\":").Append(frame);

            int turnIndex = -1;
            try { turnIndex = BattleSampler.TurnIndex; } catch { }
            sb.Append(",\"turnIndex\":").Append(turnIndex);

            if (bc == null)
            {
                sb.Append(",\"bc\":null");
                return sb.ToString();
            }

            try
            {
                sb.Append(",\"battleState\":").Append((int)bc.battleState);
                sb.Append(",\"state\":").Append((int)bc.nowActiveState);
                sb.Append(",\"stateName\":\"").Append(ProbeSink.Esc(bc.nowActiveState.ToString())).Append('"');
                sb.Append(",\"paused\":").Append(bc.battlePaused ? "true" : "false");
                sb.Append(",\"playingAnim\":").Append(bc.playingAnim ? "true" : "false");
                sb.Append(",\"attackConfirmed\":").Append(ConfirmedText(bc));
                sb.Append(",\"turnAttacked\":").Append(TurnAttackedText(bc));
            }
            catch (Exception e)
            {
                sb.Append(",\"snapErr\":\"").Append(ProbeSink.Esc(e.GetType().Name)).Append('"');
            }

            // selectedGrid (public field)
            try
            {
                GameObject sg = bc.selectedGrid;
                if (sg == null) sb.Append(",\"selectedGrid\":null");
                else
                {
                    int r = -1, c = -1;
                    try
                    {
                        GridUnitController gu = sg.GetComponent<GridUnitController>();
                        if (gu != null && gu.gridData != null) { r = gu.gridData.row; c = gu.gridData.column; }
                    }
                    catch { }
                    sb.Append(",\"selectedGrid\":\"").Append(ProbeSink.Esc(sg.name)).Append('"');
                    sb.Append(",\"selectedRow\":").Append(r);
                    sb.Append(",\"selectedCol\":").Append(c);
                }
            }
            catch { sb.Append(",\"selectedGrid\":\"?\""); }

            // list ที่เกี่ยวกับการโจมตี (อ่านอย่างเดียว)
            try
            {
                Il2CppSystem.Collections.Generic.List<GridUnitData> attackRange = null;
                Il2CppSystem.Collections.Generic.List<GridUnitData> damageRange = null;
                Il2CppSystem.Collections.Generic.List<GridUnitData> moveRange = null;
                Il2CppSystem.Collections.Generic.List<GridUnitData> movePath = null;
                try { attackRange = bc.attackRangeGridUnits; } catch { }
                try { damageRange = bc.damageRangeGridUnits; } catch { }
                try { moveRange = bc.moveRangeGridUnits; } catch { }
                try { movePath = bc.movePath; } catch { }

                sb.Append(",\"attackRangeCount\":").Append(Count(attackRange));
                sb.Append(",\"damageRangeCount\":").Append(Count(damageRange));
                sb.Append(",\"moveRangeCount\":").Append(Count(moveRange));
                sb.Append(",\"movePathCount\":").Append(Count(movePath));
                sb.Append(",\"attackRange\":").Append(RangeSample(attackRange));
            }
            catch { }

            // ยูนิตที่ถึงตา
            try
            {
                sb.Append(",\"active\":\"").Append(ProbeSink.Esc(TurnGate.ActiveStableId(bc) ?? "-")).Append('"');
                sb.Append(",\"turnMoved\":").Append(TurnGate.ReadActiveMovedFlag(bc) ? "true" : "false");
                BattleUnit a = bc.nowActiveUnit;
                if (a == null) sb.Append(",\"activeUnit\":null");
                else
                {
                    int row = -1, col = -1;
                    try { if (a.mapGrid != null) { row = a.mapGrid.row; col = a.mapGrid.column; } } catch { }
                    sb.Append(",\"activeRow\":").Append(row);
                    sb.Append(",\"activeCol\":").Append(col);
                    sb.Append(",\"activePlayerControl\":").Append(a.playerControl ? "true" : "false");
                    sb.Append(",\"activeAutoFight\":").Append(a.autoFight ? "true" : "false");
                    AppendUnitHero(sb, "activeHero", a);
                    // สกิลที่ถูกเลือกอยู่ (private field nowOnAttackSkill — อ่านอย่างเดียว)
                    try
                    {
                        PropertyInfo p = typeof(BattleUnit).GetProperty("nowOnAttackSkill",
                                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        KungfuSkillLvData sk = p == null ? null : p.GetValue(a) as KungfuSkillLvData;
                        sb.Append(",\"activeSkillId\":").Append(SkillId(sk));
                    }
                    catch { }
                }
            }
            catch { }

            return sb.ToString();
        }

        private static void AppendUnitHero(StringBuilder sb, string prefix, BattleUnit u)
        {
            if (u == null) { sb.Append(",\"").Append(prefix).Append("\":null"); return; }
            try
            {
                HeroData h = u.heroData;
                int heroId = h == null ? -1 : h.heroID;
                float hp = h == null ? -1f : (float)Math.Round(h.hp, 3);
                int teamId = -1;
                try { if (u.battleTeam != null) teamId = u.battleTeam.ID; } catch { }
                bool alive = false;
                try { alive = u.IsAlive; } catch { }
                sb.Append(",\"").Append(prefix).Append("HeroId\":").Append(heroId);
                sb.Append(",\"").Append(prefix).Append("Team\":").Append(teamId);
                sb.Append(",\"").Append(prefix).Append("Hp\":").Append(hp.ToString(System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(",\"").Append(prefix).Append("Alive\":").Append(alive ? "true" : "false");
            }
            catch { }
        }

        /// <summary>object ของกริด + ยูนิตที่ยืนอยู่ (อ่านอย่างเดียว)</summary>
        private static string ObjectFields(GameObject go)
        {
            if (go == null) return ",\"obj\":null";
            string name = null;
            int instanceId = 0;
            int row = -1, col = -1;
            try { name = go.name; } catch { }
            try { instanceId = go.GetInstanceID(); } catch { }
            StringBuilder sb = new StringBuilder(192);
            sb.Append(",\"obj\":\"").Append(ProbeSink.Esc(name)).Append('"');
            sb.Append(",\"objId\":").Append(instanceId);
            try
            {
                GridUnitController gu = go.GetComponent<GridUnitController>();
                if (gu != null && gu.gridData != null)
                {
                    GridUnitData d = gu.gridData;
                    row = d.row;
                    col = d.column;
                    sb.Append(",\"objRow\":").Append(row);
                    sb.Append(",\"objCol\":").Append(col);
                    AppendUnitHero(sb, "objUnit", d.battleUnit);
                }
            }
            catch { }
            return sb.ToString();
        }

        private static int Count(Il2CppSystem.Collections.Generic.List<GridUnitData> list)
        {
            try { return list == null ? -1 : list.Count; } catch { return -1; }
        }

        private static string RangeSample(Il2CppSystem.Collections.Generic.List<GridUnitData> list)
        {
            StringBuilder sb = new StringBuilder(96);
            sb.Append('"');
            try
            {
                if (list != null)
                {
                    int n = list.Count;
                    if (n > MaxRangeSample) n = MaxRangeSample;
                    for (int i = 0; i < n; i++)
                    {
                        GridUnitData g = null;
                        try { g = list[i]; } catch { }
                        if (g == null) continue;
                        if (sb.Length > 1) sb.Append(',');
                        sb.Append(g.row).Append(':').Append(g.column);
                    }
                }
            }
            catch { }
            sb.Append('"');
            return sb.ToString();
        }

        private static int SkillId(KungfuSkillLvData sk)
        {
            try { return sk == null ? -1 : sk.skillID; } catch { return -1; }
        }

        private static string Stack(string method)
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
                    if (line.IndexOf("JitProbeW7", StringComparison.Ordinal) >= 0) continue;
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

        // ------------------------------------------------------------ hooks

        private static void GridClickedPrefix(BattleController __instance, GameObject clickedGrid)
        {
            try
            {
                if (!Sink.Allow("BattleGridClicked.pre")) return;
                PrefixHits++;
                Sink.Write("BattleGridClicked.pre",
                           ObjectFields(clickedGrid) + Snapshot(__instance) + Stack("BattleGridClicked"));
            }
            catch { }
        }

        private static void GridClickedPostfix(BattleController __instance, GameObject clickedGrid)
        {
            try
            {
                if (!Sink.Allow("BattleGridClicked.post")) return;
                Hits++;
                Sink.Write("BattleGridClicked.post", ObjectFields(clickedGrid) + Snapshot(__instance));
            }
            catch { }
        }

        private static void ChangeActiveSkillPrefix(BattleController __instance, KungfuSkillLvData skillLvData)
        {
            try
            {
                if (!Sink.Allow("ChangeActiveSkill.pre")) return;
                PrefixHits++;
                Sink.Write("ChangeActiveSkill.pre",
                           ",\"skillId\":" + SkillId(skillLvData) + ",\"skillLv\":" + SkillLv(skillLvData)
                           + Snapshot(__instance) + Stack("ChangeActiveSkill"));
            }
            catch { }
        }

        private static void ChangeActiveSkillPostfix(BattleController __instance, KungfuSkillLvData skillLvData)
        {
            try
            {
                if (!Sink.Allow("ChangeActiveSkill.post")) return;
                Hits++;
                Sink.Write("ChangeActiveSkill.post",
                           ",\"skillId\":" + SkillId(skillLvData) + ",\"skillLv\":" + SkillLv(skillLvData)
                           + Snapshot(__instance));
            }
            catch { }
        }

        private static void ChangeNowAttackSkillPrefix(BattleController __instance, KungfuSkillLvData skillLvData)
        {
            try
            {
                if (!Sink.Allow("ChangeNowActiveAttackSkill.pre")) return;
                PrefixHits++;
                Sink.Write("ChangeNowActiveAttackSkill.pre",
                           ",\"skillId\":" + SkillId(skillLvData) + ",\"skillLv\":" + SkillLv(skillLvData)
                           + Snapshot(__instance) + Stack("ChangeNowActiveAttackSkill"));
            }
            catch { }
        }

        private static void ChangeNowAttackSkillPostfix(BattleController __instance, KungfuSkillLvData skillLvData)
        {
            try
            {
                if (!Sink.Allow("ChangeNowActiveAttackSkill.post")) return;
                Hits++;
                Sink.Write("ChangeNowActiveAttackSkill.post",
                           ",\"skillId\":" + SkillId(skillLvData) + ",\"skillLv\":" + SkillLv(skillLvData)
                           + Snapshot(__instance));
            }
            catch { }
        }

        private static void RefreshAttackRangePrefix(BattleController __instance)
        {
            try
            {
                if (!Sink.Allow("RefreshAttackRange.pre")) return;
                PrefixHits++;
                Sink.Write("RefreshAttackRange.pre", Snapshot(__instance) + Stack("RefreshAttackRange"));
            }
            catch { }
        }

        private static void RefreshAttackRangePostfix(BattleController __instance)
        {
            try
            {
                if (!Sink.Allow("RefreshAttackRange.post")) return;
                Hits++;
                Sink.Write("RefreshAttackRange.post", Snapshot(__instance));
            }
            catch { }
        }

        private static void CancelAttackPostfix(BattleController __instance)
        {
            try
            {
                if (!Sink.Allow("CancelAttack")) return;
                Hits++;
                Sink.Write("CancelAttack.post", Snapshot(__instance) + Stack("CancelAttack"));
            }
            catch { }
        }

        private static void PlayBattleUnitAttackPostfix(BattleController __instance)
        {
            try
            {
                if (!Sink.Allow("PlayBattleUnitAttack")) return;
                Hits++;
                Sink.Write("PlayBattleUnitAttack.post", Snapshot(__instance) + Stack("PlayBattleUnitAttack"));
            }
            catch { }
        }

        private static void StartPostfix(BattleController __instance)
        {
            try
            {
                if (!Sink.Allow("BattleUnitAttackStart")) return;
                Hits++;
                Sink.Write("BattleUnitAttackStart.post", Snapshot(__instance) + Stack("BattleUnitAttackStart"));
            }
            catch { }
        }

        private static void HappenPostfix(BattleController __instance)
        {
            try
            {
                if (!Sink.Allow("BattleUnitAttackHappen")) return;
                Hits++;
                Sink.Write("BattleUnitAttackHappen.post", Snapshot(__instance) + Stack("BattleUnitAttackHappen"));
            }
            catch { }
        }

        private static void HitPostfix(BattleController __instance, GridUnitData targetGridUnit, float startDelay)
        {
            try
            {
                if (!Sink.Allow("BattleUnitAttackHit")) return;
                Hits++;
                int row = -1, col = -1;
                try { if (targetGridUnit != null) { row = targetGridUnit.row; col = targetGridUnit.column; } } catch { }
                string extra = ",\"targetRow\":" + row + ",\"targetCol\":" + col
                               + ",\"startDelay\":" + startDelay.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (targetGridUnit != null)
                {
                    try
                    {
                        BattleUnit tu = targetGridUnit.battleUnit;
                        StringBuilder sb = new StringBuilder(96);
                        AppendUnitHero(sb, "target", tu);
                        extra += sb.ToString();
                    }
                    catch { }
                }
                Sink.Write("BattleUnitAttackHit.post", extra + Snapshot(__instance) + Stack("BattleUnitAttackHit"));
            }
            catch { }
        }

        private static void FinishPostfix(BattleController __instance)
        {
            try
            {
                if (!Sink.Allow("BattleUnitAttackFinish")) return;
                Hits++;
                Sink.Write("BattleUnitAttackFinish.post", Snapshot(__instance) + Stack("BattleUnitAttackFinish"));
            }
            catch { }
        }

        private static void EndPostfix(BattleController __instance, float delayTime)
        {
            try
            {
                if (!Sink.Allow("BattleUnitAttackEnd")) return;
                Hits++;
                Sink.Write("BattleUnitAttackEnd.post",
                           ",\"delayTime\":" + delayTime.ToString(System.Globalization.CultureInfo.InvariantCulture)
                           + Snapshot(__instance) + Stack("BattleUnitAttackEnd"));
            }
            catch { }
        }

        private static void ManageDamagePrefix(BattleController __instance, BattleUnit source, BattleUnit target, bool noDead)
        {
            try
            {
                if (!Sink.Allow("ManageDamage.pre")) return;
                PrefixHits++;
                StringBuilder sb = new StringBuilder(128);
                AppendUnitHero(sb, "src", source);
                AppendUnitHero(sb, "tgt", target);
                sb.Append(",\"noDead\":").Append(noDead ? "true" : "false");
                Sink.Write("ManageDamage.pre", sb.ToString() + Snapshot(__instance) + Stack("ManageDamage"));
            }
            catch { }
        }

        private static void ManageDamagePostfix(BattleController __instance, BattleUnit source, BattleUnit target, bool noDead)
        {
            try
            {
                if (!Sink.Allow("ManageDamage.post")) return;
                Hits++;
                StringBuilder sb = new StringBuilder(160);
                AppendUnitHero(sb, "src", source);
                AppendUnitHero(sb, "tgt", target);
                sb.Append(",\"noDead\":").Append(noDead ? "true" : "false");
                Sink.Write("ManageDamage.post", sb.ToString() + Snapshot(__instance));
            }
            catch { }
        }

        private static void OnClickPostfix(GridUnitController __instance)
        {
            try
            {
                if (!Sink.Allow("OnClick")) return;
                Hits++;
                GameObject go = null;
                try { if (__instance != null) go = __instance.gameObject; } catch { }
                BattleController bc = null;
                try { bc = BattleController.Instance; } catch { }
                Sink.Write("OnClick.post", ObjectFields(go) + Snapshot(bc) + Stack("OnClick"));
            }
            catch { }
        }

        private static int SkillLv(KungfuSkillLvData sk)
        {
            try { return sk == null ? -1 : sk.lv; } catch { return -1; }
        }
    }
}
