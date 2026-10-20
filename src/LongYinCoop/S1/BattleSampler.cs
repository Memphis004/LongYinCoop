using System;
using System.Collections.Generic;
using Il2Cpp;

namespace LongYinCoop
{
    /// <summary>
    /// อ่าน battle state ผ่าน typed interop ที่ ~60 Hz
    /// - ใช้ ring buffer เต็มเฟรม (capacity 900) เขียนทับ ไม่ allocate ต่อเฟรม
    /// - marker เริ่ม/จบศึกสร้างจาก state ใน snapshot (ไม่ patch เพิ่ม)
    /// </summary>
    internal static class BattleSampler
    {
        private const int SampleIntervalMs = 16;

        private static readonly BattleRing Ring = new BattleRing();
        private static BattleFrame _latest;
        private static long _lastSampleTicks;
        private static bool _active;
        private static int _turnIndex;
        private static long _seq;
        private static bool _endMarkerDue;
        private static string _lastActiveId;
        private static readonly HashSet<string> UsedIds = new HashSet<string>();

        public static int BattlesStarted;
        public static int BattlesEnded;
        public static int MaxUnitsPerSide;
        public static int LastFrameUnitCount;
        public static int LatestAgeMs;

        public static bool InBattle { get { return _active; } }
        /// <summary>ตัวนับเทิร์นของ mod (จำนวนครั้งที่ activeId เปลี่ยน) — ใช้ใน stateHash ของ S1.5</summary>
        public static int TurnIndex { get { return _turnIndex; } }
        public static int BattleCapacity { get { return Ring.Capacity; } }
        public static int BattleCount { get { return Ring.Count; } }

        public static BattleView LatestView()
        {
            if (_latest == null) return null;
            return ToView(_latest);
        }

        public static void Tick()
        {
            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            if (_lastSampleTicks != 0 &&
                (now - _lastSampleTicks) * 1000.0 / System.Diagnostics.Stopwatch.Frequency < SampleIntervalMs) return;
            _lastSampleTicks = now;

            try
            {
                BattleController bc = BattleController.Instance;
                if (bc == null)
                {
                    if (_active) EndBattle(null);
                    ClearLatest();
                    return;
                }

                int state = (int)bc.battleState;
                bool inBattle = state == 1 || state == 2 || state == 3; // Prepare / Ready / Fighting

                // นอกศึกไม่ต้อง sample/record — กัน capture บวมและกัน snapshot ค้างข้ามศึก
                if (!_active && !inBattle)
                {
                    ClearLatest();
                    return;
                }

                if (!_active && inBattle) StartBattle(bc, state);

                BattleFrame f = Sample(bc);
                _latest = f;
                LatestAgeMs = 0;

                if (_active && !inBattle && state != 3)
                {
                    // End / Exception / Loot / None — จบศึก
                    if (state == 4 || state == 5 || state == 6 || state == 0)
                    {
                        _endMarkerDue = true;
                    }
                }

                if (_active && _endMarkerDue && inBattle == false)
                {
                    EndBattle(bc);
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
        }

        private static void StartBattle(BattleController bc, int state)
        {
            _active = true;
            _turnIndex = 0;
            _lastActiveId = null;
            _endMarkerDue = false;
            BattlesStarted++;

            BattleLogHook.ResetBaseline(bc.infoCount);

            Marker("battle:start", bc, state);
        }

        private static void EndBattle(BattleController bc)
        {
            if (!_active) return;
            _active = false;
            _endMarkerDue = false;
            BattlesEnded++;

            Marker("battle:end", bc, bc == null ? 0 : (int)bc.battleState);
            BattleLogHook.ResetBaseline(-1);
            ClearLatest();
        }

        private static void ClearLatest()
        {
            _latest = null;
            LatestAgeMs = -1;
        }

        private static void Marker(string kind, BattleController bc, int state)
        {
            BattleMarker m = new BattleMarker();
            m.kind = kind;
            m.t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            m.frame = UnityEngine.Time.frameCount;
            m.battleState = state;
            m.battleStateName = StateName(state);
            m.turnIndex = _turnIndex;

            if (bc != null)
            {
                m.battleType = (int)bc.battleType;
                m.battleTypeName = bc.battleType.ToString();
                m.battleTime = Math.Round(bc.battleTime, 3);
                CountSides(bc, out m.unitsPlayer, out m.unitsEnemy);
            }

            string line = string.Format(UIStrings.BattleMarker, kind, m.battleTypeName, m.battleStateName,
                                        m.unitsPlayer, m.unitsEnemy, m.turnIndex, m.battleTime);
            CoopLog.Msg(line);
            if (CaptureWriter.WantsCapture) CaptureWriter.Record("marker", m);
        }

        private static BattleFrame Sample(BattleController bc)
        {
            BattleFrame f = Ring.Next();
            f.seq = ++_seq;
            f.frame = UnityEngine.Time.frameCount;
            f.tMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            f.battleType = (int)bc.battleType;
            f.battleTypeName = bc.battleType.ToString();
            f.battleState = (int)bc.battleState;
            f.battleStateName = bc.battleState.ToString();
            f.nowActiveState = (int)bc.nowActiveState;
            f.nowActiveStateName = bc.nowActiveState.ToString();
            f.battlePaused = bc.battlePaused;
            f.battleTime = Math.Round(bc.battleTime, 3);

            BattleMapData map = bc.battleMapData;
            f.gridW = map == null ? -1 : map.mapWidth;
            f.gridH = map == null ? -1 : map.mapHeight;

            BattleUnit active = bc.nowActiveUnit;
            f.activeId = null;

            int count = 0;
            UsedIds.Clear();
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

                    UnitState st = ReadUnit(u, team.ID, t, i, count, UsedIds);
                    f.units[count] = st;
                    if (active != null && u == active) f.activeId = st.stableId;
                    count++;
                }
            }

            f.unitCount = count;
            LastFrameUnitCount = count;

            CountSides(bc, out int player, out int enemy);
            if (player > MaxUnitsPerSide) MaxUnitsPerSide = player;

            // turnIndex = จำนวนครั้งที่ active unit เปลี่ยน (เกมไม่มี field round/turn index)
            if (f.activeId != null)
            {
                if (_lastActiveId != null && _lastActiveId != f.activeId)
                {
                    _turnIndex++;
                    f.turnIndex = _turnIndex;
                }
                else
                {
                    f.turnIndex = _turnIndex;
                }
                _lastActiveId = f.activeId;
            }
            else
            {
                f.turnIndex = _turnIndex;
            }

            if (CaptureWriter.WantsCapture) CaptureWriter.Record("battle", ToView(f));
            return f;
        }

        private static UnitState ReadUnit(BattleUnit u, int teamId, int teamIndex, int index, int ordinal,
                                          HashSet<string> usedIds)
        {
            UnitState st = new UnitState();
            st.teamIndex = teamIndex;
            st.index = index;
            st.side = teamId;

            HeroData hero = u.heroData;
            st.heroId = hero == null ? -1 : hero.heroID;
            st.name = hero == null ? null : hero.heroName;
            st.stableId = StableId(teamId, hero, index, usedIds);

            GridUnitData grid = u.mapGrid;
            st.row = grid == null ? -1 : grid.row;
            st.column = grid == null ? -1 : grid.column;

            if (hero != null)
            {
                st.hp = hero.hp;
                st.maxHp = hero.maxhp;
                st.realMaxHp = hero.realMaxHp;
                st.alive = !hero.dead;
            }

            st.autoFight = u.autoFight;
            st.playerControl = u.playerControl;
            st.moved = u.moved;
            st.attacked = u.attacked;
            return st;
        }

        /// <summary>identity เดียวกับที่ capture S1 ใช้ — S1.5 เรียกใช้เพื่อ join audit ได้ (ไม่ตั้งใจซ้ำโค้ด)</summary>
        internal static string StableId(int teamId, HeroData hero, int index, HashSet<string> usedIds)
        {
            // identity ถาวร: side + heroID (+ summonID) — กันชนด้วย suffix เฉพาะเมื่อซ้ำในศึกเดียวกัน
            string baseId = hero == null
                ? teamId + ":nil:" + index
                : teamId + ":" + hero.heroID + (hero.isSummon ? ":s" + hero.summonID : "");

            string id = baseId;
            int n = 1;
            while (!usedIds.Add(id))
            {
                id = baseId + "#" + n;
                n++;
            }
            return id;
        }

        private static void CountSides(BattleController bc, out int player, out int enemy)
        {
            player = 0;
            enemy = 0;
            Il2CppSystem.Collections.Generic.List<BattleTeam> teams = bc.teams;
            if (teams == null) return;
            for (int t = 0; t < teams.Count; t++)
            {
                BattleTeam team = teams[t];
                if (team == null || team.battleUnits == null) continue;
                int n = team.battleUnits.Count;
                if (t == 0) player += n; else enemy += n;
            }
        }

        private static string StateName(int state)
        {
            switch (state)
            {
                case 0: return "None";
                case 1: return "Prepare";
                case 2: return "Ready";
                case 3: return "Fighting";
                case 4: return "End";
                case 5: return "Exception";
                case 6: return "Loot";
                default: return "?" + state;
            }
        }

        private static BattleView ToView(BattleFrame f)
        {
            BattleView v = new BattleView();
            v.seq = f.seq;
            v.frame = f.frame;
            v.tMs = f.tMs;
            v.battleType = f.battleType;
            v.battleTypeName = f.battleTypeName;
            v.battleState = f.battleState;
            v.battleStateName = f.battleStateName;
            v.nowActiveState = f.nowActiveState;
            v.nowActiveStateName = f.nowActiveStateName;
            v.battlePaused = f.battlePaused;
            v.turnIndex = f.turnIndex;
            v.activeId = f.activeId;
            v.gridW = f.gridW;
            v.gridH = f.gridH;
            v.unitCount = f.unitCount;
            v.battleTime = f.battleTime;
            v.phase = f.nowActiveStateName;
            v.units = new List<UnitView>(f.unitCount);
            for (int i = 0; i < f.unitCount; i++)
            {
                UnitState st = f.units[i];
                UnitView uv = new UnitView();
                uv.stableId = st.stableId;
                uv.heroId = st.heroId;
                uv.side = st.side;
                uv.teamIndex = st.teamIndex;
                uv.index = st.index;
                uv.name = st.name;
                uv.row = st.row;
                uv.column = st.column;
                uv.cell = new Cell { x = st.column, y = st.row };
                uv.hp = st.hp;
                uv.maxHp = st.maxHp;
                uv.realMaxHp = st.realMaxHp;
                uv.alive = st.alive;
                uv.autoFight = st.autoFight;
                uv.playerControl = st.playerControl;
                uv.moved = st.moved;
                uv.attacked = st.attacked;
                v.units.Add(uv);
                if (uv.stableId != null && uv.stableId == f.activeId) v.nowActiveUnit = uv;
            }
            return v;
        }
    }
}
