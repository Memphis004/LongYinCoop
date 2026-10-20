using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Il2Cpp;
using UnityEngine;

namespace LongYinCoop
{
    /// <summary>
    /// S1.5-d — คำสั่งเปลี่ยน state คำสั่งแรกของ mod: `spike move`
    ///
    /// กติกาที่บังคับในโค้ดนี้ (S1.5-plan §0/§3):
    /// - ต้อง arm ก่อนทุกครั้ง และ **arm ถูกใช้ไป 1 ครั้งต่อ 1 คำสั่ง** (สำเร็จหรือล้มเหลวก็หลุด)
    /// - เรียก method เดียวเท่านั้น: `BattleController.BattleGridClicked(GameObject)` = entry เดียวกับที่ UI จริงใช้
    ///   (W1: คลิกกริดของผู้เล่น 39 ครั้งพายูนิตไปยืนช่องนั้นจริง — ไม่มี call อื่นที่ mod ต้องประดิษฐ์)
    /// - ไม่เขียน field ของเกมเลย, ไม่เรียก Save/Load, ไม่ retry, ไม่วนลูป
    /// - ต้องรันบน Unity main thread เท่านั้น (IpcServer ส่งผ่าน MainThreadDispatcher)
    /// </summary>
    internal static class MoveCommand
    {
        /// <summary>เพดานเวลา arm ตามแผน §3 (วินาที)</summary>
        public const int MaxArmSeconds = 300;
        public const int DefaultArmSeconds = 120;

        private static long _armedUntilTicks;   // Environment.TickCount64 ms; 0 = disarmed
        private static readonly object StateLock = new object();

        private static readonly Stopwatch Clock = Stopwatch.StartNew();

        public static bool IsArmed
        {
            get
            {
                long until = Interlocked.Read(ref _armedUntilTicks);
                return until > 0 && Clock.ElapsedMilliseconds < until;
            }
        }

        public static long RemainingMs
        {
            get
            {
                long until = Interlocked.Read(ref _armedUntilTicks);
                long left = until - Clock.ElapsedMilliseconds;
                return left > 0 ? left : 0;
            }
        }

        public static object Arm(int seconds)
        {
            if (seconds <= 0) seconds = DefaultArmSeconds;
            if (seconds > MaxArmSeconds) seconds = MaxArmSeconds;

            long until = Clock.ElapsedMilliseconds + seconds * 1000L;
            Interlocked.Exchange(ref _armedUntilTicks, until);
            CoopLog.Msg("spike arm: " + seconds + "s");
            return new
            {
                armed = true,
                seconds = seconds,
                remainingMs = RemainingMs
            };
        }

        public static object Disarm()
        {
            Interlocked.Exchange(ref _armedUntilTicks, 0);
            CoopLog.Msg("spike disarm");
            return new { armed = false, remainingMs = 0L };
        }

        /// <summary>ใช้สิทธิ์ arm หนึ่งครั้ง (เรียกทันทีที่เริ่มประมวลผลคำสั่ง — ไม่ว่าผลจะออกมาอย่างไร)</summary>
        private static void ConsumeArm()
        {
            Interlocked.Exchange(ref _armedUntilTicks, 0);
        }

        /// <summary>
        /// ผลของคำสั่ง — ใช้ property (ไม่ใช่ field) เพราะ System.Text.Json serialize เฉพาะ property
        /// ถ้าใช้ field จะได้ `{}` กลับไปหา client
        /// </summary>
        public sealed class MoveResult
        {
            public bool Ok { get; set; }
            public string Error { get; set; }
            public string StableId { get; set; }
            public int TargetRow { get; set; }
            public int TargetCol { get; set; }
            public string GridName { get; set; }
            public string ActiveIdBefore { get; set; }
            public int StateBefore { get; set; }
            public string StateBeforeName { get; set; }
            public bool MovedFlagBefore { get; set; }
            public string Call { get; set; }
            public int StateAfterSelfClick { get; set; }
            public string StateAfterSelfClickName { get; set; }
            public int StateAfterButton { get; set; }
            public string StateAfterButtonName { get; set; }
            public int StateAfterPreview { get; set; }
            public string StateAfterPreviewName { get; set; }
            public int UnitRowAfter { get; set; }
            public int UnitColAfter { get; set; }
            public int StateAfterPath { get; set; }
            public string StateAfterPathName { get; set; }
            public bool RollbackApplied { get; set; }
            public long CostMs { get; set; }
        }

        /// <summary>
        /// เวลาที่ยอมให้เกมเปลี่ยน state 6 (ShowMovingRage) -> 7 (WaitMoving) ก่อนสั่ง preview
        /// วัดจริง (W6 22:49): คลิกช่องตัวเอง f28538 state=6 -> เพิ่มอีก 1 เฟรม f28539 state=7
        /// </summary>
        public const int PhaseTimeoutMs = 1500;

        private sealed class Job
        {
            public string StableId;
            public int Row;
            public int Col;
            public GridUnitController Target;
            public MoveResult Result;
            public long StartedMs;
            public bool Done;
        }

        private static Job _job;
        private static readonly object JobLock = new object();
        private static readonly ManualResetEventSlim DoneEvent = new ManualResetEventSlim(false);

        /// <summary>
        /// เริ่มคำสั่งเดิน — ต้องเรียกบน Unity main thread
        /// คืน MoveResult ทันทีเมื่อล้มเหลวก่อน/ระหว่างแตะเกม (ขั้นที่ 1), คืน null เมื่อเริ่มแล้ว (ผลมาทาง WaitDone)
        ///
        /// ลำดับที่วัดได้ (W6 22:49):
        ///   f28538 GridUnitController.OnClick -> BattleGridClicked(self) : state 2 -> 6 (ShowMovingRage)
        ///   f28539 (เฟรมถัดไป) เกมตั้ง state 7 (WaitMoving) แล้ว hover เรียก GenerateMovePath(target)
        ///   commit: BattleGridClicked(target)
        /// ขั้นที่ 2-3 จึงต้องข้ามเฟรม ไม่ใช่เรียกต่อกันในสแต็กเดียว (รอบ 5 ล้มเพราะเรียกในเฟรมเดียวกับขั้นที่ 1)
        /// </summary>
        public static MoveResult StartMove(string stableId, int row, int col)
        {
            Stopwatch sw = Stopwatch.StartNew();
            MoveResult r = new MoveResult
            {
                StableId = stableId,
                TargetRow = row,
                TargetCol = col,
                Call = null
            };

            if (!IsArmed)
            {
                r.Error = UIStrings.SpikeNeedsArm;
                return r;
            }

            lock (JobLock)
            {
                if (_job != null && !_job.Done)
                {
                    r.Error = UIStrings.SpikeMoveBusy;
                    return r;
                }
            }

            // ใช้สิทธิ์ทันที — คำสั่งนี้จะไม่สามารถ retry ได้ถ้าล้มเหลว
            ConsumeArm();

            try
            {
                BattleController bc = BattleController.Instance;
                if (bc == null)
                {
                    r.Error = UIStrings.SpikeMoveNotInBattle;
                    return r;
                }

                r.StateBefore = (int)bc.nowActiveState;
                r.StateBeforeName = bc.nowActiveState.ToString();
                r.MovedFlagBefore = TurnGate.ReadActiveMovedFlag(bc);

                if (bc.battleState != BattleState.Fighting)
                {
                    r.Error = string.Format(UIStrings.SpikeMoveNotFighting, bc.battleState.ToString());
                    return r;
                }
                if (bc.battlePaused)
                {
                    r.Error = UIStrings.SpikeMovePaused;
                    return r;
                }
                if (bc.nowActiveState != ActiveUnitBattleState.WaitPlayerInput)
                {
                    r.Error = string.Format(UIStrings.SpikeMoveNotPlayerTurn, bc.nowActiveState.ToString());
                    return r;
                }

                BattleUnit active = bc.nowActiveUnit;
                if (active == null || !active.playerControl)
                {
                    r.Error = UIStrings.SpikeMoveNoPlayerUnit;
                    return r;
                }

                string activeId = TurnGate.ActiveStableId(bc);
                r.ActiveIdBefore = activeId;
                if (!string.Equals(activeId, stableId, StringComparison.Ordinal))
                {
                    r.Error = string.Format(UIStrings.SpikeMoveWrongUnit, stableId, activeId ?? "-");
                    return r;
                }

                int activeRow = -1, activeCol = -1;
                try
                {
                    GridUnitData activeGrid = active.mapGrid;
                    if (activeGrid != null) { activeRow = activeGrid.row; activeCol = activeGrid.column; }
                }
                catch { }
                if (activeRow < 0 || activeCol < 0)
                {
                    r.Error = UIStrings.SpikeMoveNoPlayerUnit;
                    return r;
                }

                Transform rootA = null;
                Transform rootB = null;
                try { rootA = bc.gridUnitsRoot; } catch { }
                try { rootB = bc.gridUnitsRootParent == null ? null : bc.gridUnitsRootParent.transform; } catch { }

                GridUnitController cell = Scan(rootA, row, col);
                if (cell == null) cell = Scan(rootB, row, col);
                if (cell == null || cell.gridData == null)
                {
                    r.Error = string.Format(UIStrings.SpikeMoveNoGrid, row, col);
                    return r;
                }
                r.GridName = cell.gameObject.name;

                GridUnitController selfCell = Scan(rootA, activeRow, activeCol);
                if (selfCell == null) selfCell = Scan(rootB, activeRow, activeCol);
                if (selfCell == null)
                {
                    r.Error = string.Format(UIStrings.SpikeMoveNoGrid, activeRow, activeCol);
                    return r;
                }

                // ขั้นที่ 1 — คลิกช่องที่ยูนิตยืน (ทางเข้าโหมดเดินเดียวกับที่ผู้เล่นใช้)
                r.Call = "BattleGridClicked(self)";
                bc.BattleGridClicked(selfCell.gameObject);
                r.StateAfterSelfClick = (int)bc.nowActiveState;
                r.StateAfterSelfClickName = bc.nowActiveState.ToString();
                r.StateAfterButton = r.StateAfterSelfClick;
                r.StateAfterButtonName = r.StateAfterSelfClickName;

                if (r.StateAfterSelfClick == (int)ActiveUnitBattleState.WaitPlayerInput)
                {
                    r.Error = string.Format(UIStrings.SpikeMoveButtonIgnored, r.StateAfterSelfClickName);
                    return r;
                }

                // ขั้นที่ 2-3 ต้องข้ามเฟรม — ส่งงานให้ Tick()
                Job job = new Job
                {
                    StableId = stableId,
                    Row = row,
                    Col = col,
                    Target = cell,
                    Result = r,
                    StartedMs = Clock.ElapsedMilliseconds
                };
                DoneEvent.Reset();
                lock (JobLock) { _job = job; }

                CoopLog.Msg(string.Format("spike move start id={0} -> ({1},{2}) selfClick={3} (deferred)",
                                          stableId, row, col, r.StateAfterSelfClickName));
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

        /// <summary>เรียกทุกเฟรมบน main thread (CoopMod.Tick) — เดินขั้นที่ 2 และ 3 เมื่อเกมพร้อม</summary>
        public static void Tick()
        {
            Job job;
            lock (JobLock) { job = _job; }
            if (job == null || job.Done) return;

            BattleController bc = null;
            try { bc = BattleController.Instance; } catch { }

            try
            {
                if (bc == null)
                {
                    Finish(job, UIStrings.SpikeMoveNotInBattle, false);
                    return;
                }

                MoveResult r = job.Result;
                if (r.StateAfterPreview != 0 || r.StateAfterPath != 0) return;

                bool timedOut = Clock.ElapsedMilliseconds - job.StartedMs > PhaseTimeoutMs;

                // ขั้นที่ 2 — รอ state 7 (WaitMoving) แล้วสั่ง preview
                if (bc.nowActiveState == ActiveUnitBattleState.WaitMoving)
                {
                    bc.GenerateMovePath(job.Target.gridData);
                    r.StateAfterPreview = (int)bc.nowActiveState;
                    r.StateAfterPreviewName = bc.nowActiveState.ToString();

                    if (r.StateAfterPreview == (int)ActiveUnitBattleState.ShowMovingRage
                        || r.StateAfterPreview == (int)ActiveUnitBattleState.WaitPlayerInput)
                    {
                        bc.CancelMove();
                        r.RollbackApplied = true;
                        Finish(job, string.Format(UIStrings.SpikeMovePathFailed, r.StateAfterPreviewName), false);
                        return;
                    }

                    // ขั้นที่ 3 — commit ในเฟรมเดียวกับ preview (เหมือนเกมจริง f43357)
                    r.Call = "BattleGridClicked(self) + GenerateMovePath + BattleGridClicked(target)";
                    bc.BattleGridClicked(job.Target.gameObject);
                    r.StateAfterPath = (int)bc.nowActiveState;
                    r.StateAfterPathName = bc.nowActiveState.ToString();

                    if (r.StateAfterPath == (int)ActiveUnitBattleState.ShowMovingRage)
                    {
                        bc.CancelMove();
                        r.RollbackApplied = true;
                        Finish(job, string.Format(UIStrings.SpikeMovePathFailed, r.StateAfterPathName), false);
                        return;
                    }

                    try
                    {
                        BattleUnit active = bc.nowActiveUnit;
                        GridUnitData nowGrid = active == null ? null : active.mapGrid;
                        if (nowGrid != null)
                        {
                            r.UnitRowAfter = nowGrid.row;
                            r.UnitColAfter = nowGrid.column;
                        }
                    }
                    catch { }

                    Finish(job, null, true);
                    return;
                }

                if (timedOut)
                {
                    // ยังไม่ถึง state 7 ภายในเวลา — ถอยออกจากโหมดและรายงาน
                    try { bc.CancelMove(); r.RollbackApplied = true; } catch { }
                    Finish(job, string.Format(UIStrings.SpikeMovePathFailed, bc.nowActiveState.ToString()), false);
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                Finish(job, "exception: " + e.GetType().Name + ": " + e.Message, false);
            }
        }

        /// <summary>ปิดงานและปลุก thread ที่รอผล (IPC) — เรียกบน main thread</summary>
        private static void Finish(Job job, string error, bool ok)
        {
            MoveResult r = job.Result;
            r.Ok = ok && error == null;
            if (error != null) r.Error = error;
            r.CostMs = Clock.ElapsedMilliseconds - job.StartedMs;

            lock (JobLock) { job.Done = true; }
            try { DoneEvent.Set(); } catch { }

            CoopLog.Msg(string.Format(
                "spike move id={0} -> ({1},{2}) ok={3} self={4} preview={5} after={6} unitAfter=({7},{8}) rollback={9} cost={10}ms err={11}",
                job.StableId, job.Row, job.Col, r.Ok, r.StateAfterSelfClickName, r.StateAfterPreviewName,
                r.StateAfterPathName, r.UnitRowAfter, r.UnitColAfter, r.RollbackApplied, r.CostMs, r.Error ?? "-"));
        }

        /// <summary>รอผลจาก Tick (เรียกจาก thread ของ IPC ไม่ใช่ main thread)</summary>
        public static MoveResult WaitDone(int timeoutMs)
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

        /// <summary>
        /// หา GameObject ของช่อง (row, col) จาก observable grid — ใช้ GridUnitController.gridData
        /// (typed; ไม่มี raw offset) โดยสแกนลูกของ root ที่ให้มา
        /// </summary>
        private static GridUnitController Scan(Transform root, int row, int col)
        {
            if (root == null) return null;
            try
            {
                int n = root.childCount;
                for (int i = 0; i < n; i++)
                {
                    Transform child = root.GetChild(i);
                    if (child == null) continue;
                    GridUnitController gu = child.GetComponent<GridUnitController>();
                    if (gu == null) continue;
                    GridUnitData data = gu.gridData;
                    if (data == null) continue;
                    if (data.row == row && data.column == col) return gu;
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
            return null;
        }
    }
}
