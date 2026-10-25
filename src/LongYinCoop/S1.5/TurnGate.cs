using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HarmonyLib;
using Il2Cpp;

namespace LongYinCoop
{
    /// <summary>
    /// S1.5-b TurnGate — อ่านสถานะศึกอย่างเดียว (ไม่เรียก method ใดที่เปลี่ยน state)
    ///
    /// กติกา:
    /// - ทุกฟิลด์อ่านผ่าน Il2CppInterop แบบมีชนิด ไม่มี raw offset / pointer arithmetic
    /// - ต้องถูกเรียกบน Unity main thread เท่านั้น (IpcServer ส่งผ่าน MainThreadDispatcher)
    /// - ไม่มี Harmony patch ในคลาสนี้; ตรวจเฉพาะ patch ที่มีอยู่แล้วของ mod อื่น (อ่านอย่างเดียว)
    /// - stateHash ตาม S1.5-plan §4: battleState, nowActiveState, battlePaused, turnIndex,
    ///   activeId, infoCount + ต่อยูนิต stableId|side|teamIndex|row|col|hp(3)|alive|moved|attacked
    /// </summary>
    internal static class TurnGate
    {
        /// <summary>ขึ้นต้น canonical text — เปลี่ยนเมื่อนิยาม hash เปลี่ยน (hash เก่าจะไม่ชนกัน)
        /// v2 (S3 prerequisite 2026-10-25): alive ต่อยูนิตเปลี่ยนจากการอ่าน HeroData.dead
        /// (ซึ่งไม่ใช่ธงตายในศึก) เป็น IsAlive+hp
        /// หมายเหตุประวัติ: evidence เก่า (S1.5c jit-dump-analysis §8) เคย "เสนอ" v2 คนละนิยาม
        /// (turn flags แทน moved/attacked) — ข้อเสนอนั้นไม่เคยถูกใช้จริง (โค้ดที่ ship ทุกรุ่นเป็น v1)
        /// v2 ตัวนี้จึงไม่ชนกับ hash จริงตัวใด</summary>
        public const string HashVersion = "battleframe/v2";

        private const int CostCapacity = 512;

        private static readonly double[] CostSamples = new double[CostCapacity];
        private static readonly object CostLock = new object();
        private static int _costCount;
        private static int _costNext;

        private static readonly HashSet<string> UsedIds = new HashSet<string>();
        private static readonly StringBuilder HashText = new StringBuilder(4096);

        // private bool บน BattleController — Il2CppInterop สร้างเป็น property (ยังเป็น typed ไม่ใช่ offset)
        // resolve แบบ lazy บน main thread เท่านั้น: static initializer ต้องไม่แตะ type ของ IL2CPP
        private static PropertyInfo _turnMovedProperty;
        private static PropertyInfo _turnAttackedProperty;
        private static bool _turnFlagsResolved;

        private static int _actionPatchCount = -1;
        private static string _actionPatchSummary = null;

        public static long Calls;
        public static long Errors;
        public static string LastHash;

        /// <summary>method กลุ่ม action ที่ S1.5 ห้าม hook (S1.5-plan §2/§6 J2)</summary>
        private static readonly string[] ActionMethodNames =
        {
            "BattleGridClicked", "MoveButtonClicked", "GenerateMovePath", "CancelMove", "RegretMove",
            "ChangeActiveSkill", "ChangeNowActiveAttackSkill", "PlayBattleUnitMove", "PlayBattleUnitAttack",
            "BattlePauseButtonClicked"
        };

        /// <summary>
        /// อ่านสถานะปัจจุบัน — เรียกบน main thread เท่านั้น (IpcServer.OnMain)
        /// ไม่มี side effect ต่อเกม; เขียนเฉพาะสถิติของ mod เอง
        /// </summary>
        public static object ReadStatus()
        {
            long t0 = Stopwatch.GetTimestamp();
            bool ok = false;
            try
            {
                ResolveTurnFlags();
                object status = Build(t0);
                ok = true;
                return status;
            }
            catch (Exception e)
            {
                Errors++;
                CoopLog.Error(e);
                throw;
            }
            finally
            {
                double ms = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
                AddCost(ms);
                Calls++;
                if (!ok) CoopLog.Warn(string.Format(UIStrings.SpikeStatusFailed, Calls, Errors));
            }
        }

        /// <summary>
        /// S1.5-d: stableId ของ "ยูนิตที่ถึงตา" คำนวณด้วยอัลกอริทึมเดียวกับ Build() (ตัวเดียว ไม่แยกสำเนา)
        /// main thread เท่านั้น; คืน null เมื่อไม่มีศึก/ยูนิต
        /// </summary>
        public static string ActiveStableId(BattleController bc)
        {
            try
            {
                if (bc == null) return null;
                BattleUnit active = bc.nowActiveUnit;
                if (active == null) return null;

                Il2CppSystem.Collections.Generic.List<BattleTeam> teams = bc.teams;
                if (teams == null) return null;

                HashSet<string> used = new HashSet<string>();
                for (int t = 0; t < teams.Count; t++)
                {
                    BattleTeam team = teams[t];
                    if (team == null) continue;
                    Il2CppSystem.Collections.Generic.List<BattleUnit> units = team.battleUnits;
                    if (units == null) continue;
                    for (int i = 0; i < units.Count; i++)
                    {
                        BattleUnit u = units[i];
                        if (u == null) continue;
                        string id = BattleSampler.StableId(team.ID, u.heroData, i, used);
                        if (u == active) return id;
                    }
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
            return null;
        }

        /// <summary>
        /// S1.5-e: หายูนิตจาก stableId ด้วยอัลกอริทึมเดียวกับ Build()/ActiveStableId
        /// main thread เท่านั้น; คืน null เมื่อไม่พบ (teamId = -1)
        /// </summary>
        public static BattleUnit ResolveUnit(BattleController bc, string stableId, out int teamId)
        {
            teamId = -1;
            if (bc == null || string.IsNullOrEmpty(stableId)) return null;
            try
            {
                Il2CppSystem.Collections.Generic.List<BattleTeam> teams = bc.teams;
                if (teams == null) return null;

                HashSet<string> used = new HashSet<string>();
                for (int t = 0; t < teams.Count; t++)
                {
                    BattleTeam team = teams[t];
                    if (team == null) continue;
                    Il2CppSystem.Collections.Generic.List<BattleUnit> units = team.battleUnits;
                    if (units == null) continue;
                    for (int i = 0; i < units.Count; i++)
                    {
                        BattleUnit u = units[i];
                        if (u == null) continue;
                        string id = BattleSampler.StableId(team.ID, u.heroData, i, used);
                        if (string.Equals(id, stableId, StringComparison.Ordinal))
                        {
                            teamId = team.ID;
                            return u;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
            return null;
        }

        /// <summary>S1.5-e: team ID ของยูนิต (เทียบฝั่งศัตรู/พวกพ้อง) — คืน -1 เมื่อไม่พบ</summary>
        public static int ResolveTeamId(BattleController bc, BattleUnit unit)
        {
            if (bc == null || unit == null) return -1;
            try
            {
                Il2CppSystem.Collections.Generic.List<BattleTeam> teams = bc.teams;
                if (teams == null) return -1;
                for (int t = 0; t < teams.Count; t++)
                {
                    BattleTeam team = teams[t];
                    if (team == null) continue;
                    Il2CppSystem.Collections.Generic.List<BattleUnit> units = team.battleUnits;
                    if (units == null) continue;
                    for (int i = 0; i < units.Count; i++)
                    {
                        if (units[i] == unit) return team.ID;
                    }
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
            return -1;
        }

        /// <summary>S1.5-d: อ่าน flag nowActiveUnitMoved (typed property เดียวกับที่ hash ใช้)</summary>
        public static bool ReadActiveMovedFlag(BattleController bc)
        {
            try
            {
                ResolveTurnFlags();
                return _turnMovedProperty != null && ReadBool(bc, _turnMovedProperty);
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                return false;
            }
        }

        private static object Build(long t0)
        {
            BattleController bc = BattleController.Instance;
            if (bc == null)
            {
                LastHash = null;
                return NotInBattle(t0);
            }

            int battleState = (int)bc.battleState;
            if (!IsBattlePhase(battleState))
            {
                LastHash = null;
                return NotInBattle(t0);
            }

            int nowActiveState = (int)bc.nowActiveState;
            bool paused = bc.battlePaused;
            bool playingAnim = bc.playingAnim;
            int infoCount = bc.infoCount;
            // S1.5-c: BattleController.nowScale (0xE0) — ตัวชี้ความเร็วศึก (อ่านอย่างเดียว)
            float nowScale = SafeNowScale(bc);
            // S1.5-c: WorldData.battleTimeScale (0x1D8) — ตัวชี้ความเร็วศึกตัวจริง (อ่านอย่างเดียว)
            float battleTimeScale = SafeBattleTimeScale();

            BattleMapData map = bc.battleMapData;
            int gridW = map == null ? -1 : map.mapWidth;
            int gridH = map == null ? -1 : map.mapHeight;

            BattleUnit active = bc.nowActiveUnit;
            string activeId = null;
            int activeHeroId = -1;
            string activeName = null;
            bool activePlayerControl = false;
            bool activeAutoFight = false;
            bool activeMoved = false;
            bool activeAttacked = false;
            int activeRow = -1;
            int activeColumn = -1;

            UsedIds.Clear();
            HashText.Length = 0;
            HashText.Append(HashVersion).Append('\n');
            HashText.Append("battleState=").Append(battleState).Append('\n');
            HashText.Append("nowActiveState=").Append(nowActiveState).Append('\n');
            HashText.Append("battlePaused=").Append(paused ? 1 : 0).Append('\n');
            HashText.Append("turnIndex=").Append(BattleSampler.TurnIndex).Append('\n');

            // activeId ต้องรู้ก่อนเขียนบรรทัด activeId — จึงเก็บหน่วยเป็น list ชั่วคราว
            List<string> unitLines = new List<string>(S1Limits.MaxUnits);
            // S1.5-c W5: แถวต่อยูนิตสำหรับรายงาน hp (ไม่รวมใน stateHash)
            List<object> unitRows = new List<object>(S1Limits.MaxUnits);

            int count = 0;
            Il2CppSystem.Collections.Generic.List<BattleTeam> teams = bc.teams;
            int teamCount = teams == null ? 0 : teams.Count;
            for (int t = 0; t < teamCount && count < S1Limits.MaxUnits; t++)
            {
                BattleTeam team = teams[t];
                if (team == null) continue;
                Il2CppSystem.Collections.Generic.List<BattleUnit> units = team.battleUnits;
                if (units == null) continue;

                for (int i = 0; i < units.Count && count < S1Limits.MaxUnits; i++)
                {
                    BattleUnit u = units[i];
                    if (u == null) continue;

                    HeroData hero = u.heroData;
                    GridUnitData grid = u.mapGrid;
                    string stableId = BattleSampler.StableId(team.ID, hero, i, UsedIds);
                    int heroId = hero == null ? -1 : hero.heroID;
                    int row = grid == null ? -1 : grid.row;
                    int column = grid == null ? -1 : grid.column;
                    float hp = hero == null ? 0f : hero.hp;
                    // S3 prerequisite (2026-10-25): HeroData.dead ไม่ใช่ธงตายในศึก [RUNTIME S1.5f §3]
                    // — ศพมี dead=false, hp=0, IsAlive=false ⇒ alive ต้องมาจาก IsAlive + hp
                    //   (IsAlive ยัง true ในเฟรมที่ hp→0 ⇒ เช็ค hp>0 เสริม) — นิยามเดียวกับ AttackCommand
                    bool alive = AliveForS3(u, hp);

                    unitLines.Add(CanonicalUnit(stableId, team.ID, t, row, column, hp, alive,
                                                u.moved, u.attacked));
                    unitRows.Add(new
                    {
                        id = stableId,
                        team = team.ID,
                        teamIndex = t,
                        heroId = heroId,
                        row = row,
                        col = column,
                        hp = Math.Round(hp, 3),
                        alive = alive,
                        playerControl = u.playerControl,
                        // S1.5-c ตัดสินใจ: ตัววัดดาเมจที่เป็นไปได้ (อ่านอย่างเดียว)
                        originHp = SafeOriginHp(u),
                        unitAlive = SafeUnitAlive(u)
                    });

                    if (active != null && u == active)
                    {
                        activeId = stableId;
                        activeHeroId = heroId;
                        activeName = hero == null ? null : hero.heroName;
                        activePlayerControl = u.playerControl;
                        activeAutoFight = u.autoFight;
                        activeMoved = u.moved;
                        activeAttacked = u.attacked;
                        activeRow = row;
                        activeColumn = column;
                    }
                    count++;
                }
            }

            HashText.Append("activeId=").Append(activeId ?? "-").Append('\n');
            HashText.Append("infoCount=").Append(infoCount).Append('\n');
            for (int i = 0; i < unitLines.Count; i++) HashText.Append(unitLines[i]).Append('\n');

            string canonical = HashText.ToString();
            string hash = Sha256(canonical);
            LastHash = hash;

            bool turnMovedAvailable = _turnMovedProperty != null;
            bool turnMoved = turnMovedAvailable && ReadBool(bc, _turnMovedProperty);
            bool turnAttacked = _turnAttackedProperty != null && ReadBool(bc, _turnAttackedProperty);

            int waitPlayerInput = (int)ActiveUnitBattleState.WaitPlayerInput;
            int fighting = (int)BattleState.Fighting;

            return new
            {
                inBattle = true,
                battleState = battleState,
                battleStateName = bc.battleState.ToString(),
                nowActiveState = nowActiveState,
                nowActiveStateName = bc.nowActiveState.ToString(),
                // S1.5-e: ข้อมูลประกอบเท่านั้น — list นี้ค้างข้ามเทิร์น (วัดแล้ว 2026-10-21)
                attackRangeCount = SafeListCount(bc, 0),
                damageRangeCount = SafeListCount(bc, 1),
                // NEG20 follow-up (2026-10-25): ช่องระยะโจมตีจาก list เดียวกับ attack path
                // (bc.attackRangeGridUnits — เทียบ AttackCommand.ListHas) — อ่านอย่างเดียว
                // null = ไม่มีข้อมูล · [] = list ว่าง · อื่น ๆ = [[row,col],...]
                attackRangeCells = SafeAttackRangeCells(bc),
                active = activeId,
                activeHeroId = activeHeroId,
                activeName = activeName,
                activePlayerControl = activePlayerControl,
                activeAutoFight = activeAutoFight,
                activeRow = activeRow,
                activeColumn = activeColumn,
                isPlayerTurn = activeId != null && activePlayerControl
                               && nowActiveState == waitPlayerInput && battleState == fighting,
                moved = activeMoved,
                attacked = activeAttacked,
                turnMoved = turnMoved,
                turnAttacked = turnAttacked,
                turnFlagsAvailable = turnMovedAvailable,
                paused = paused,
                playingAnim = playingAnim,
                infoCount = infoCount,
                turnIndex = BattleSampler.TurnIndex,
                unitCount = count,
                gridW = gridW,
                gridH = gridH,
                frame = UnityEngine.Time.frameCount,
                // S1.5-c W5/J7: อ่านอย่างเดียว — hp ต่อยูนิต + นาฬิกาศึก (ATB detector)
                units = unitRows,
                battleTime = SafeFloat(bc, 0),
                timeLimit = SafeFloat(bc, 1),
                aiWaitTime = SafeFloat(bc, 2),
                timeScale = UnityEngine.Time.timeScale,
                nowScale = nowScale,
                battleTimeScale = battleTimeScale,
                deltaTime = UnityEngine.Time.deltaTime,
                unscaledDeltaTime = UnityEngine.Time.unscaledDeltaTime,
                stateHash = hash,
                hashText = canonical,
                actionPatches = ActionPatchAudit(),
                costMs = ElapsedMs(t0),
                cost = CostSnapshot()
            };
        }

        private static object NotInBattle(long t0)
        {
            return new
            {
                inBattle = false,
                battleState = -1,
                battleStateName = (string)null,
                nowActiveState = -1,
                nowActiveStateName = (string)null,
                active = (string)null,
                activeHeroId = -1,
                activeName = (string)null,
                activePlayerControl = false,
                activeAutoFight = false,
                activeRow = -1,
                activeColumn = -1,
                isPlayerTurn = false,
                moved = false,
                attacked = false,
                turnMoved = false,
                turnAttacked = false,
                turnFlagsAvailable = _turnMovedProperty != null,
                paused = false,
                playingAnim = false,
                infoCount = -1,
                turnIndex = -1,
                unitCount = 0,
                gridW = -1,
                gridH = -1,
                frame = UnityEngine.Time.frameCount,
                units = (object)null,
                battleTime = -1f,
                timeLimit = -1f,
                aiWaitTime = -1f,
                timeScale = UnityEngine.Time.timeScale,
                nowScale = -1f,
                battleTimeScale = -1f,
                deltaTime = UnityEngine.Time.deltaTime,
                unscaledDeltaTime = UnityEngine.Time.unscaledDeltaTime,
                stateHash = (string)null,
                hashText = (string)null,
                actionPatches = ActionPatchAudit(),
                costMs = ElapsedMs(t0),
                cost = CostSnapshot()
            };
        }

        private static bool IsBattlePhase(int battleState)
        {
            return battleState == (int)BattleState.Prepare
                   || battleState == (int)BattleState.Ready
                   || battleState == (int)BattleState.Fighting;
        }

        /// <summary>WorldData.battleTimeScale — อ่านอย่างเดียว (ฟิลด์ที่ SetTimeScaleTab() เทียบกับเลขบนแท็บ)</summary>
        private static float SafeBattleTimeScale()
        {
            try
            {
                GameController gc = GameController.Instance;
                if (gc == null) return -1f;
                WorldData world = gc.worldData;
                return world == null ? -1f : (float)Math.Round(world.battleTimeScale, 4);
            }
            catch { return -1f; }
        }

        /// <summary>BattleController.nowScale — อ่านอย่างเดียว (ตัวชี้ความเร็วศึก; ยังไม่ยืนยันว่าปุ่ม x5 เขียนฟิลด์นี้)</summary>
        private static float SafeNowScale(BattleController bc)
        {
            try { return bc == null ? -1f : (float)Math.Round(bc.nowScale, 4); }
            catch { return -1f; }
        }

        /// <summary>BattleUnit.originHp — อ่านอย่างเดียว (ยังไม่ยืนยันว่าเป็น hp ปัจจุบันของศึก)</summary>
        private static float SafeOriginHp(BattleUnit u)
        {
            try { return u == null ? -1f : (float)Math.Round(u.originHp, 3); }
            catch { return -1f; }
        }

        /// <summary>BattleUnit.IsAlive — property getter (อ่านอย่างเดียว)
        /// internal: AttackCommand ใช้ตัวเดียวกันเป็น guard ของเป้าหมาย (S3 prerequisite)</summary>
        internal static bool SafeUnitAlive(BattleUnit u)
        {
            try { return u != null && u.IsAlive; }
            catch { return false; }
        }

        /// <summary>นิยาม alive ร่วมของ S3 prerequisite (รีวิวรอบ 1+3, 2026-10-25):
        /// ห่อ AliveRule.AliveForS3 (pure) สำหรับ BattleUnit — ใช้นิยามเดียวกันทั้ง stateHash (ที่นี่)
        /// และ guard เป้าหมายของ spike attack (AttackCommand) · เคสขอบ IsAlive=true แต่ hp&lt;=0
        /// (เฟรมที่ hp→0) ⇒ ไม่นับ alive ทั้งสองที่</summary>
        internal static bool AliveForS3(BattleUnit u, float hp)
        {
            return AliveRule.AliveForS3(SafeUnitAlive(u), hp);
        }

        /// <summary>อ่าน float ของ BattleController แบบ typed ตาม index (0=battleTime, 1=timeLimit, 2=AIWaitTime)</summary>
        private static float SafeFloat(BattleController bc, int which)
        {
            try
            {
                if (bc == null) return -1f;
                if (which == 0) return bc.battleTime;
                if (which == 1) return bc.timeLimit;
                return bc.AIWaitTime;
            }
            catch { return -1f; }
        }

        /// <summary>
        /// S1.5-e: จำนวนสมาชิกของ list เป้าหมายที่เล็ง (0=attackRangeGridUnits, 1=damageRangeGridUnits)
        /// อ่านอย่างเดียว — ค่าที่ได้อาจค้างข้ามเทิร์น ใช้เป็นข้อมูลประกอบเท่านั้น
        /// </summary>
        private static int SafeListCount(BattleController bc, int which)
        {
            try
            {
                if (bc == null) return -1;
                Il2CppSystem.Collections.Generic.List<GridUnitData> list =
                    which == 0 ? bc.attackRangeGridUnits : bc.damageRangeGridUnits;
                return list == null ? -1 : list.Count;
            }
            catch { return -1; }
        }

        /// <summary>
        /// NEG20 follow-up (2026-10-25): อ่านช่องระยะโจมตีเพื่อ expose ใน status —
        /// ใช้ list เดียวกับ attack path (`bc.attackRangeGridUnits` — AttackCommand.ListHas)
        /// อ่านอย่างเดียว ไม่เขียน state ไม่เปลี่ยน guard/turn logic ·
        /// คืน null = ไม่มีข้อมูล (bc/list null หรืออ่าน error) · [] = list ว่าง ·
        /// อื่น ๆ = [[row,col],...] (ผ่าน RangeCells.Payload — pure, test ได้ offline)
        /// </summary>
        internal static System.Collections.Generic.List<int[]> SafeAttackRangeCells(BattleController bc)
        {
            try
            {
                if (bc == null) return null;
                Il2CppSystem.Collections.Generic.List<GridUnitData> list = bc.attackRangeGridUnits;
                if (list == null) return null;
                var pairs = new System.Collections.Generic.List<(int row, int col)>(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    GridUnitData g = list[i];
                    if (g == null) continue;
                    pairs.Add((g.row, g.column));
                }
                return RangeCells.Payload(pairs);
            }
            catch { return null; }
        }

        private static string CanonicalUnit(string stableId, int side, int teamIndex, int row, int column,
                                            float hp, bool alive, bool moved, bool attacked)
        {
            return string.Concat(
                stableId, "|", side.ToString(CultureInfo.InvariantCulture), "|",
                teamIndex.ToString(CultureInfo.InvariantCulture), "|",
                row.ToString(CultureInfo.InvariantCulture), "|",
                column.ToString(CultureInfo.InvariantCulture), "|",
                Math.Round(hp, 3).ToString("0.000", CultureInfo.InvariantCulture), "|",
                alive ? "1" : "0", "|", moved ? "1" : "0", "|", attacked ? "1" : "0");
        }

        private static void ResolveTurnFlags()
        {
            if (_turnFlagsResolved) return;
            _turnFlagsResolved = true;
            _turnMovedProperty = typeof(BattleController).GetProperty("nowActiveUnitMoved",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            _turnAttackedProperty = typeof(BattleController).GetProperty("nowActiveUnitAttacked",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_turnMovedProperty == null || _turnAttackedProperty == null)
            {
                CoopLog.Warn(UIStrings.SpikeTurnFlagsMissing);
            }
        }

        private static bool ReadBool(BattleController bc, PropertyInfo p)
        {
            object v = p.GetValue(bc);
            return v is bool && (bool)v;
        }

        private static string Sha256(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                byte[] digest = sha.ComputeHash(bytes);
                StringBuilder sb = new StringBuilder(digest.Length * 2);
                for (int i = 0; i < digest.Length; i++) sb.Append(digest[i].ToString("x2"));
                return sb.ToString();
            }
        }

        // ------------------------------------------------------------ cost stats

        private static void AddCost(double ms)
        {
            lock (CostLock)
            {
                CostSamples[_costNext] = ms;
                _costNext = (_costNext + 1) % CostSamples.Length;
                if (_costCount < CostSamples.Length) _costCount++;
            }
        }

        private static double ElapsedMs(long t0)
        {
            return Math.Round((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency, 4);
        }

        private static object CostSnapshot()
        {
            double[] copy;
            lock (CostLock)
            {
                copy = new double[_costCount];
                Array.Copy(CostSamples, copy, _costCount);
            }
            if (copy.Length == 0)
                return new { p50 = 0.0, p99 = 0.0, max = 0.0, samples = 0, errors = Errors, calls = Calls };
            Array.Sort(copy);
            double p99 = copy[(int)Math.Ceiling(0.99 * copy.Length) - 1];
            double p50 = copy[(int)Math.Ceiling(0.50 * copy.Length) - 1];
            return new
            {
                p50 = Math.Round(p50, 4),
                p99 = Math.Round(p99, 4),
                max = Math.Round(copy[copy.Length - 1], 4),
                samples = copy.Length,
                errors = Errors,
                calls = Calls
            };
        }

        // ------------------------------------------------------- patch inventory

        /// <summary>
        /// นับ patch ที่ติดอยู่บน method กลุ่ม action (ของ mod ใดก็ได้) — อ่านอย่างเดียว
        /// S1.5-b ไม่เพิ่ม patch เอง; ตัวเลขนี้ต้องเป็น 0 จึงจะยืนยันว่า "ไม่มี postfix call"
        /// </summary>
        public static object ActionPatchAudit()
        {
            if (_actionPatchCount >= 0)
                return new { count = _actionPatchCount, summary = _actionPatchSummary, cached = true };

            int count = 0;
            StringBuilder summary = new StringBuilder();
            try
            {
                foreach (MethodBase m in HarmonyLib.Harmony.GetAllPatchedMethods())
                {
                    if (!IsActionMethod(m)) continue;
                    Patches info = HarmonyLib.Harmony.GetPatchInfo(m);
                    count++;
                    if (summary.Length > 0) summary.Append("; ");
                    summary.Append(m.DeclaringType == null ? "?" : m.DeclaringType.Name).Append('.')
                           .Append(m.Name).Append(" owners=")
                           .Append(info == null || info.Owners == null ? "" : string.Join(",", info.Owners));
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                _actionPatchSummary = "audit failed: " + e.Message;
                return new { count = -1, summary = _actionPatchSummary, cached = false };
            }

            _actionPatchCount = count;
            _actionPatchSummary = count == 0 ? "none" : summary.ToString();
            return new { count = count, summary = _actionPatchSummary, cached = false };
        }

        private static bool IsActionMethod(MethodBase m)
        {
            string name = m.Name;
            for (int i = 0; i < ActionMethodNames.Length; i++)
            {
                if (name.StartsWith(ActionMethodNames[i], StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
