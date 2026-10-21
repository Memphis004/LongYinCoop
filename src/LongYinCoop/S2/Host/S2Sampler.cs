using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Il2Cpp;

namespace LongYinCoop.S2.Host
{
    /// <summary>
    /// S2a-2 sampler: อ่าน state ศึก **บน main thread เท่านั้น** แล้วคืน POCO (S2Frame)
    /// - interop แบบมีชนิดทั้งหมด (ไม่ใช้ raw offset/pointer)
    /// - ไม่เรียก method ใดที่เปลี่ยน state ของเกม
    /// - `alive` (hero.dead) ไม่อยู่ใน replica เด็ดขาด (§13.2 ข้อ 1)
    /// </summary>
    internal sealed class S2Sampler
    {
        /// <summary>ตัวชี้ศึกปัจจุบัน (ใช้สร้าง battleKey แบบ candidate — ต้องวัดความเสถียรก่อนใช้ resync)</summary>
        private string _battleKey;
        private string _battleKeyTeams;
        private long _battleFirstFrame;

        public long Frames;
        public long EmptyReads;
        public long Errors;
        public string LastError;
        public string LastBattleKey;

        /// <summary>main thread only</summary>
        public S2Frame Read(int maxUnits = 64)
        {
            long t0 = Stopwatch.GetTimestamp();
            try
            {
                BattleController bc = BattleController.Instance;
                if (bc == null)
                {
                    NewBattleNextRead();
                    EmptyReads++;
                    return null;
                }

                int battleState = (int)bc.battleState;
                if (battleState != (int)BattleState.Prepare
                    && battleState != (int)BattleState.Ready
                    && battleState != (int)BattleState.Fighting)
                {
                    NewBattleNextRead();
                    EmptyReads++;
                    return null;
                }

                Il2CppSystem.Collections.Generic.List<BattleTeam> teams = bc.teams;
                if (teams == null)
                {
                    EmptyReads++;
                    return null;
                }

                BattleUnit active = bc.nowActiveUnit;
                List<S2Unit> units = new List<S2Unit>(maxUnits);
                List<string> ids = new List<string>(maxUnits);
                HashSet<string> used = new HashSet<string>();
                int activeIndex = -1;
                int count = 0;

                int teamCount = teams.Count;
                for (int t = 0; t < teamCount && count < maxUnits; t++)
                {
                    BattleTeam team = teams[t];
                    if (team == null) continue;
                    Il2CppSystem.Collections.Generic.List<BattleUnit> list = team.battleUnits;
                    if (list == null) continue;

                    int unitCount = list.Count;
                    for (int i = 0; i < unitCount && count < maxUnits; i++)
                    {
                        BattleUnit u = list[i];
                        if (u == null) continue;
                        HeroData hero = u.heroData;
                        GridUnitData grid = u.mapGrid;

                        string stableId = BattleSampler.StableId(team.ID, hero, i, used);
                        S2Unit su = new S2Unit
                        {
                            StableId = stableId,
                            Team = team.ID,
                            HeroId = hero == null ? -1 : hero.heroID,
                            Row = grid == null ? -1 : grid.row,
                            Column = grid == null ? -1 : grid.column,
                            Hp = hero == null ? 0f : hero.hp,
                            MaxHp = hero == null ? 0f : hero.maxhp,
                            IsAlive = u.IsAlive,
                            // rev4: UnitAlive = สำเนาของ IsAlive (มติ 2026-10-21 ข้อ 2 = (ii))
                            // ผ่าน S2UnitRule เท่านั้น — ไม่มีการอ่านค่าที่สองจากเกม
                            UnitAlive = S2UnitRule.MirrorUnitAlive(u.IsAlive),
                            Moved = u.moved,
                            Attacked = u.attacked,
                            PlayerControl = u.playerControl,
                        };
                        units.Add(su);
                        ids.Add(stableId);
                        if (active != null && u == active) activeIndex = units.Count - 1;
                        count++;
                    }
                }

                if (_battleKey == null) _battleFirstFrame = Frames;

                S2Header header = new S2Header
                {
                    BattleKey = EnsureBattleKey(teams),
                    TurnIndex = BattleSampler.TurnIndex,
                    ActiveUnitId = activeIndex >= 0 ? ids[activeIndex] : null,
                    Paused = bc.battlePaused,
                    NowActiveState = (int)bc.nowActiveState,
                    BattleTime = bc.battleTime,
                    UnitCount = units.Count,
                };

                S2Frame frame = new S2Frame
                {
                    FrameId = ++Frames,
                    Header = header,
                    Units = units.ToArray(),
                };
                frame.Audit = S2AuditUtil.Compute(header, frame.Units);
                frame.SampleMs = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
                LastBattleKey = header.BattleKey;
                return frame;
            }
            catch (Exception e)
            {
                Errors++;
                LastError = e.GetType().Name + ": " + e.Message;
                CoopLog.Error(e);
                return null;
            }
        }

        private void NewBattleNextRead()
        {
            _battleKey = null;
            _battleKeyTeams = null;
        }

        /// <summary>
        /// §13.2 ข้อ 2 (candidate): battleKey = sha256("s2battle|" + teamIDs เรียง + "|" + frame แรกที่เห็น)
        /// ต้องวัดความเสถียรก่อนใช้เป็นกุญแจ resync — ถ้าไม่คงที่ ให้ใช้ sessionId แทน
        /// </summary>
        private string EnsureBattleKey(Il2CppSystem.Collections.Generic.List<BattleTeam> teams)
        {
            StringBuilder teamText = new StringBuilder(32);
            List<int> teamIds = new List<int>();
            for (int t = 0; t < teams.Count; t++)
            {
                BattleTeam team = teams[t];
                if (team == null) continue;
                teamIds.Add(team.ID);
            }
            teamIds.Sort();
            for (int i = 0; i < teamIds.Count; i++)
            {
                if (i > 0) teamText.Append(',');
                teamText.Append(teamIds[i].ToString(CultureInfo.InvariantCulture));
            }

            string keyText = teamText.ToString();
            if (_battleKey == null || !string.Equals(keyText, _battleKeyTeams, StringComparison.Ordinal))
            {
                _battleKeyTeams = keyText;
                _battleKey = Hex(SHA256.HashData(Encoding.UTF8.GetBytes(
                    string.Format(CultureInfo.InvariantCulture, "s2battle|{0}|{1}", keyText,
                                  _battleFirstFrame))));
            }
            return _battleKey;
        }

        private static string Hex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }
}
