using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Il2Cpp;
using UnityEngine;

namespace LongYinCoop
{
    /// <summary>
    /// S1.5-e — คำสั่งเปลี่ยน state ตัวที่สองของ mod: `spike attack`
    ///
    /// ลำดับที่วัดได้จาก UI จริง (W7, 2026-10-21 — evidence/S1.5e-20261021-0101/S1.5e-exit1-chain.md):
    ///   state WaitAttacking + คลิกช่องศัตรู 1 ครั้ง -> `BattleGridClicked(go)`
    ///     -> `attackComfirmed` = true (ภายในคลิกเดียวกัน)
    ///     -> เกมเรียก `BattleUnitAttackStart` -> AttackHappen -> AttackHit xN -> ManageDamage -> Finish -> End
    ///
    /// กติกาที่บังคับในโค้ดนี้:
    /// - arm ใบเดียวกับ `spike move` ใช้ได้ 1 ครั้งต่อ 1 คำสั่ง (สำเร็จหรือล้มเหลวก็หลุด)
    /// - เรียก method เดียว: `BattleGridClicked(grid ของเป้าหมาย)` — ไม่เขียน field ของเกม
    /// - ไม่เรียก `ChangeActiveSkill`/`ChangeNowActiveAttackSkill` เอง (ต้องรอ state `WaitAttacking`)
    /// - G6 (มติ 2026-10-21): ให้ **เกมเป็นผู้ตรวจระยะ** ผ่าน `attackComfirmed`
    ///   ส่วน `attackRangeGridUnits`/`damageRangeGridUnits` เป็นข้อมูลประกอบเท่านั้น (ค้างข้ามเทิร์น — วัดแล้ว)
    /// - `attackComfirmed` เป็นค่า transient: อ่าน **ทันที** ที่ `BattleGridClicked` คืนค่า
    ///   (ห้ามอ่านผ่าน `spike status` เพราะ `BattleUnitAttackStart` จะรีเซ็ตเป็น false)
    /// - สำเร็จ = **hp ของเป้าลดจริง** (อ่านซ้ำข้ามเฟรมจนครบหรือหมดเวลา) ไม่ใช่ `attackComfirmed` อย่างเดียว
    ///   และ **ไม่ใช้** hp ผู้โจมตีเป็นเงื่อนไข (วัดแล้วว่าผู้โจมตีเสีย hp ใน ManageDamage เดียวกัน)
    /// - ไม่มี retry, ไม่วนลูปไม่จำกัด, ไม่มี Save/Load
    /// </summary>
    internal static class AttackCommand
    {
        /// <summary>เวลาที่ยอมให้เกมประมวลผล ManageDamage แล้วค่า hp เป้าเปลี่ยน (ms)</summary>
        public const int HpTimeoutMs = 3000;

        /// <summary>เวลาทั้งหมดที่ฝั่ง IPC รอก่อนตอบ timeout</summary>
        public const int WaitTimeoutMs = HpTimeoutMs + 800;

        private const float HpEpsilon = 0.0005f;

        private static readonly Stopwatch Clock = Stopwatch.StartNew();

        /// <summary>ผลของคำสั่ง — ใช้ property เพราะ System.Text.Json serialize เฉพาะ property</summary>
        public sealed class AttackResult
        {
            public bool Ok { get; set; }
            public string Error { get; set; }
            public string StableId { get; set; }
            public string TargetId { get; set; }
            public string ActiveIdBefore { get; set; }
            public int StateBefore { get; set; }
            public string StateBeforeName { get; set; }
            public int StateAfter { get; set; }
            public string StateAfterName { get; set; }
            public string GridName { get; set; }
            public int TargetRow { get; set; }
            public int TargetCol { get; set; }
            public int ActiveRow { get; set; }
            public int ActiveCol { get; set; }
            public int TargetTeam { get; set; }
            public float TargetHpBefore { get; set; }
            public float TargetHpAfter { get; set; }
            public float TargetHpDrop { get; set; }
            public bool TargetAliveBefore { get; set; }
            public bool TargetAliveAfter { get; set; }
            public float ActiveHpBefore { get; set; }
            /// <summary>สัญญาณยืนยันจากเกม (field `attackComfirmed`) อ่านทันทีหลังคลิก — transient</summary>
            public bool AttackConfirmed { get; set; }
            /// <summary>ข้อมูลประกอบ (advisory) — list นี้ค้างข้ามเทิร์น ใช้ตัดสินไม่ได้</summary>
            public int AttackRangeCount { get; set; }
            public int DamageRangeCount { get; set; }
            public bool TargetInAttackRange { get; set; }
            public string Call { get; set; }
            /// <summary>เวลาที่บล็อก Unity main thread (เฟส 1) — ใช้เทียบเกณฑ์ p99 ≤ 8 ms</summary>
            public long CostMs { get; set; }
            /// <summary>เวลารวมถึงตอนยืนยัน hp (รวมแอนิเมชัน) — ไม่ใช่ต้นทุน main thread</summary>
            public long ElapsedMs { get; set; }
        }

        // ------------------------------------------------------------ attackComfirmed (typed, read-only)

        private static PropertyInfo _confirmedProp;
        private static FieldInfo _confirmedField;
        private static bool _confirmedResolved;

        /// <summary>อ่าน `BattleController.attackComfirmed` (private bool; เกมสะกดผิด) แบบ typed</summary>
        private static bool ReadConfirmed(BattleController bc)
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
                catch (Exception e) { CoopLog.Warn("spike attack: attackComfirmed resolve failed: " + e.Message); }
                if (_confirmedProp == null && _confirmedField == null)
                    CoopLog.Warn("spike attack: attackComfirmed not exposed");
            }
            try
            {
                object v = _confirmedProp != null ? _confirmedProp.GetValue(bc)
                         : (_confirmedField != null ? _confirmedField.GetValue(bc) : null);
                if (v is bool) return (bool)v;
            }
            catch { }
            return false;
        }

        // ------------------------------------------------------------ job (เฟส 2: ยืนยัน hp)

        private sealed class Job
        {
            public BattleUnit Target;
            public HeroData TargetHero;
            public AttackResult Result;
            public long StartedMs;
            public bool Done;
        }

        private static Job _job;
        private static readonly object JobLock = new object();
        private static readonly ManualResetEventSlim DoneEvent = new ManualResetEventSlim(false);

        /// <summary>
        /// สั่งโจมตี 1 ครั้ง — ต้องเรียกบน Unity main thread
        /// คืน AttackResult ทันทีเมื่อล้มเหลวก่อน/ระหว่างคลิก, คืน null เมื่อคลิกถูกเกมรับแล้ว (ผล hp มาทาง WaitDone)
        /// </summary>
        public static AttackResult StartAttack(string stableId, string targetStableId)
        {
            Stopwatch sw = Stopwatch.StartNew();
            AttackResult r = new AttackResult
            {
                StableId = stableId,
                TargetId = targetStableId,
                TargetRow = -1,
                TargetCol = -1,
                ActiveRow = -1,
                ActiveCol = -1
            };

            // ใช้สิทธิ์ arm ทันที — คำสั่งนี้จะไม่สามารถ retry ได้ถ้าล้มเหลว
            if (!MoveCommand.TryConsumeArm())
            {
                r.Error = UIStrings.SpikeNeedsArm;
                return r;
            }

            lock (JobLock)
            {
                if (_job != null && !_job.Done)
                {
                    r.Error = UIStrings.SpikeAttackBusy;
                    return r;
                }
            }

            try
            {
                BattleController bc = BattleController.Instance;
                if (bc == null)
                {
                    r.Error = UIStrings.SpikeAttackNotInBattle;
                    return r;
                }

                r.StateBefore = (int)bc.nowActiveState;
                r.StateBeforeName = bc.nowActiveState.ToString();

                if (bc.battleState != BattleState.Fighting)
                {
                    r.Error = string.Format(UIStrings.SpikeAttackNotFighting, bc.battleState.ToString());
                    return r;
                }
                if (bc.battlePaused)
                {
                    r.Error = UIStrings.SpikeAttackPaused;
                    return r;
                }

                // มติ 2026-10-21: ไม่เรียก ChangeNowActiveAttackSkill เอง — ต้องรอ state ที่เกมตั้งไว้แล้ว
                if (bc.nowActiveState != ActiveUnitBattleState.WaitAttacking)
                {
                    r.Error = string.Format(UIStrings.SpikeAttackNotAiming, bc.nowActiveState.ToString());
                    return r;
                }

                BattleUnit active = bc.nowActiveUnit;
                if (active == null || !active.playerControl)
                {
                    r.Error = UIStrings.SpikeAttackNoPlayerUnit;
                    return r;
                }

                string activeId = TurnGate.ActiveStableId(bc);
                r.ActiveIdBefore = activeId;
                if (!string.Equals(activeId, stableId, StringComparison.Ordinal))
                {
                    r.Error = string.Format(UIStrings.SpikeAttackWrongUnit, stableId, activeId ?? "-");
                    return r;
                }

                int targetTeam = -1;
                BattleUnit target = TurnGate.ResolveUnit(bc, targetStableId, out targetTeam);
                if (target == null)
                {
                    r.Error = string.Format(UIStrings.SpikeAttackTargetNotFound, targetStableId);
                    return r;
                }
                if (target == active)
                {
                    r.Error = string.Format(UIStrings.SpikeAttackTargetSelf, targetStableId);
                    return r;
                }

                int activeTeam = TurnGate.ResolveTeamId(bc, active);
                r.TargetTeam = targetTeam;
                if (activeTeam >= 0 && targetTeam >= 0 && activeTeam == targetTeam)
                {
                    r.Error = string.Format(UIStrings.SpikeAttackTargetFriendly, targetStableId, targetTeam);
                    return r;
                }

                HeroData targetHero = null;
                try { targetHero = target.heroData; } catch { }
                r.TargetHpBefore = targetHero == null ? 0f : targetHero.hp;
                r.TargetAliveBefore = targetHero != null && !targetHero.dead;
                if (!r.TargetAliveBefore)
                {
                    r.Error = string.Format(UIStrings.SpikeAttackTargetDead, targetStableId);
                    return r;
                }

                HeroData activeHero = null;
                try { activeHero = active.heroData; } catch { }
                r.ActiveHpBefore = activeHero == null ? 0f : activeHero.hp;

                int targetRow = -1, targetCol = -1;
                try
                {
                    GridUnitData g = target.mapGrid;
                    if (g != null) { targetRow = g.row; targetCol = g.column; }
                }
                catch { }
                if (targetRow < 0 || targetCol < 0)
                {
                    r.Error = string.Format(UIStrings.SpikeAttackNoGrid, targetRow, targetCol);
                    return r;
                }
                r.TargetRow = targetRow;
                r.TargetCol = targetCol;

                try
                {
                    GridUnitData ag = active.mapGrid;
                    if (ag != null) { r.ActiveRow = ag.row; r.ActiveCol = ag.column; }
                }
                catch { }

                // ข้อมูลประกอบเท่านั้น (ห้ามใช้ตัดสิน — list ค้างข้ามเทิร์น)
                r.AttackRangeCount = ListCount(bc.attackRangeGridUnits);
                r.DamageRangeCount = ListCount(bc.damageRangeGridUnits);
                r.TargetInAttackRange = ListHas(bc.attackRangeGridUnits, targetRow, targetCol);

                Transform rootA = null;
                Transform rootB = null;
                try { rootA = bc.gridUnitsRoot; } catch { }
                try { rootB = bc.gridUnitsRootParent == null ? null : bc.gridUnitsRootParent.transform; } catch { }

                GridUnitController cell = MoveCommand.Scan(rootA, targetRow, targetCol);
                if (cell == null) cell = MoveCommand.Scan(rootB, targetRow, targetCol);
                if (cell == null || cell.gridData == null)
                {
                    r.Error = string.Format(UIStrings.SpikeAttackNoGrid, targetRow, targetCol);
                    return r;
                }
                r.GridName = cell.gameObject.name;

                // คลิกช่องเป้าหมาย 1 ครั้ง — เส้นทางเดียวกับ UI ที่วัดได้
                r.Call = "BattleGridClicked(target)";
                bc.BattleGridClicked(cell.gameObject);

                r.StateAfter = (int)bc.nowActiveState;
                r.StateAfterName = bc.nowActiveState.ToString();

                // อ่านทันทีในเฟรมเดียวกับคลิก — ค่านี้จะถูก BattleUnitAttackStart รีเซ็ตเป็น false ในเฟรมถัด ๆ ไป
                r.AttackConfirmed = ReadConfirmed(bc);
                if (!r.AttackConfirmed)
                {
                    r.Error = UIStrings.SpikeAttackRejected;
                    return r;
                }

                // เฟส 2: ยืนยันด้วย hp ของเป้า (ต้องลดจริง) — ตรวจใน Tick
                Job job = new Job
                {
                    Target = target,
                    TargetHero = targetHero,
                    Result = r,
                    StartedMs = Clock.ElapsedMilliseconds
                };
                DoneEvent.Reset();
                lock (JobLock) { _job = job; }

                CoopLog.Msg(string.Format("spike attack id={0} target={1} confirmed=true (deferred hp check)",
                                          stableId, targetStableId));
                return null;
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                r.Ok = false;
                r.Error = "exception: " + e.GetType().Name + ": " + e.Message;
                return r;
            }
            finally
            {
                sw.Stop();
                r.CostMs = sw.ElapsedMilliseconds;
            }
        }

        /// <summary>เรียกทุกเฟรมบน main thread (CoopMod.Tick) — เฟส 2 รอ hp เป้าลด</summary>
        public static void Tick()
        {
            Job job;
            lock (JobLock) { job = _job; }
            if (job == null || job.Done) return;

            try
            {
                AttackResult r = job.Result;

                float hp = -1f;
                bool dead = false;
                BattleUnit t = job.Target;

                if (t != null)
                {
                    HeroData h = job.TargetHero;
                    if (h == null) { try { h = t.heroData; } catch { } }
                    if (h != null)
                    {
                        try { hp = h.hp; } catch { hp = -1f; }
                        try { dead = h.dead; } catch { }
                    }
                }

                // สำเร็จ = hp ลดจริง หรือตาย (hp ถึง 0) — ไม่ใช้ hp ผู้โจมตีเป็นเงื่อนไข
                if (hp >= 0f && (hp < r.TargetHpBefore - HpEpsilon || dead))
                {
                    r.TargetHpAfter = hp;
                    r.TargetHpDrop = r.TargetHpBefore - hp;
                    r.TargetAliveAfter = !dead;
                    Finish(job, null, true);
                    return;
                }

                if (Clock.ElapsedMilliseconds - job.StartedMs > HpTimeoutMs)
                {
                    r.TargetHpAfter = hp;
                    r.TargetHpDrop = hp >= 0f ? r.TargetHpBefore - hp : 0f;
                    r.TargetAliveAfter = !dead;
                    Finish(job, string.Format(UIStrings.SpikeAttackHpNotConfirmed,
                                              r.TargetHpBefore, hp), false);
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                Finish(job, "exception: " + e.GetType().Name + ": " + e.Message, false);
            }
        }

        private static void Finish(Job job, string error, bool ok)
        {
            AttackResult r = job.Result;
            r.Ok = ok && error == null;
            if (error != null) r.Error = error;
            r.ElapsedMs = Clock.ElapsedMilliseconds - job.StartedMs;

            lock (JobLock) { job.Done = true; }
            try { DoneEvent.Set(); } catch { }

            CoopLog.Msg(string.Format(
                "spike attack id={0} target={1} ok={2} confirmed={3} state={4}->{5} hp={6}->{7} drop={8} alive={9} tgt@({10},{11}) range={12}/{13} cost={14}ms total={15}ms err={16}",
                r.StableId, r.TargetId, r.Ok, r.AttackConfirmed, r.StateBeforeName, r.StateAfterName,
                r.TargetHpBefore, r.TargetHpAfter, r.TargetHpDrop, r.TargetAliveAfter,
                r.TargetRow, r.TargetCol, r.AttackRangeCount, r.DamageRangeCount,
                r.CostMs, r.ElapsedMs, r.Error ?? "-"));
        }

        /// <summary>รอผลจาก Tick (เรียกจาก thread ของ IPC ไม่ใช่ main thread)</summary>
        public static AttackResult WaitDone(int timeoutMs)
        {
            if (timeoutMs < 1) timeoutMs = 1;
            if (!DoneEvent.Wait(timeoutMs)) return null;

            Job job;
            lock (JobLock)
            {
                job = _job;
                _job = null;
            }
            return job == null ? null : job.Result;
        }

        // ------------------------------------------------------------ list helpers (advisory only)

        private static int ListCount(Il2CppSystem.Collections.Generic.List<GridUnitData> list)
        {
            try { return list == null ? -1 : list.Count; } catch { return -1; }
        }

        private static bool ListHas(Il2CppSystem.Collections.Generic.List<GridUnitData> list, int row, int col)
        {
            try
            {
                if (list == null) return false;
                for (int i = 0; i < list.Count; i++)
                {
                    GridUnitData g = list[i];
                    if (g == null) continue;
                    if (g.row == row && g.column == col) return true;
                }
            }
            catch { }
            return false;
        }
    }
}
