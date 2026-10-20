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
        /// เดินยูนิตที่ถึงตาไปช่อง (row, col) ด้วยการเรียก entry เดียวกับ UI: BattleGridClicked(gridGameObject)
        /// </summary>
        public static MoveResult Move(string stableId, int row, int col)
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

                // ขั้นที่ 1 — คลิกช่องที่ยูนิตยืน = ทางเข้าโหมดเดินที่ผู้เล่นใช้จริง
                // หลักฐาน W1/W2 22:26 (f43218): BattleGridClicked(6_0 = ช่องของยูนิต) -> OnClick(6,0)
                // ในรอบ 4 พบว่า MoveButtonClicked() ก็ขึ้น state 6 ได้ แต่ GenerateMovePath จาก state 6
                // ไม่เกิด path ("no path generated (state ShowMovingRage)") -> ปุ่มย้ายไม่ใช่ทางเข้า
                GridUnitController selfCell = Scan(rootA, activeRow, activeCol);
                if (selfCell == null) selfCell = Scan(rootB, activeRow, activeCol);
                if (selfCell == null)
                {
                    r.Error = string.Format(UIStrings.SpikeMoveNoGrid, activeRow, activeCol);
                    return r;
                }
                r.Call = "BattleGridClicked(self)";
                bc.BattleGridClicked(selfCell.gameObject);
                r.StateAfterSelfClick = (int)bc.nowActiveState;
                r.StateAfterSelfClickName = bc.nowActiveState.ToString();
                // เก็บไว้เพื่อเทียบกับรุ่นก่อน (ปุ่มย้าย) — รอบนี้ไม่เรียกแล้ว
                r.StateAfterButton = r.StateAfterSelfClick;
                r.StateAfterButtonName = r.StateAfterSelfClickName;

                if (r.StateAfterSelfClick == (int)ActiveUnitBattleState.WaitPlayerInput)
                {
                    r.Error = string.Format(UIStrings.SpikeMoveButtonIgnored, r.StateAfterSelfClickName);
                    return r;
                }

                // ขั้นที่ 2 — preview path (ของจริงเกิดตอนเมาส์เลื่อนผ่านช่อง: W1/W2 22:26)
                bc.GenerateMovePath(cell.gridData);
                r.StateAfterPreview = (int)bc.nowActiveState;
                r.StateAfterPreviewName = bc.nowActiveState.ToString();

                if (r.StateAfterPreview == (int)ActiveUnitBattleState.WaitPlayerInput
                    || r.StateAfterPreview == (int)ActiveUnitBattleState.ShowMovingRage)
                {
                    // preview ไม่เกิด = ไม่มี path ให้ commit — ถอยออกจากโหมด selelect และรายงาน
                    bc.CancelMove();
                    r.RollbackApplied = true;
                    r.Error = string.Format(UIStrings.SpikeMovePathFailed, r.StateAfterPreviewName);
                    return r;
                }

                // ขั้นที่ 3 — คลิกช่องปลายทางเพื่อ commit (เกมจะเรียก GenerateMovePath ซ้ำ + OnClick เอง)
                bc.BattleGridClicked(cell.gameObject);
                r.StateAfterPath = (int)bc.nowActiveState;
                r.StateAfterPathName = bc.nowActiveState.ToString();
                r.Call = "BattleGridClicked(self) + GenerateMovePath + BattleGridClicked(target)";

                // ล้มเหลวเฉพาะเมื่อยังอยู่โหมดเลือกช่อง (ShowMovingRage) — ถ้าเกมเดินแล้ว/จบเทิร์นแล้วถือว่ารับคำสั่ง
                if (r.StateAfterPath == (int)ActiveUnitBattleState.ShowMovingRage)
                {
                    bc.CancelMove();
                    r.RollbackApplied = true;
                    r.Error = string.Format(UIStrings.SpikeMovePathFailed, r.StateAfterPathName);
                    return r;
                }

                // ยืนยันด้วยตำแหน่งจริง (อ่านซ้ำ 1 ครั้ง ไม่วนรอ)
                try
                {
                    GridUnitData nowGrid = bc.nowActiveUnit == null ? null : bc.nowActiveUnit.mapGrid;
                    if (nowGrid != null)
                    {
                        r.UnitRowAfter = nowGrid.row;
                        r.UnitColAfter = nowGrid.column;
                    }
                }
                catch { }

                r.Ok = true;
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                r.Ok = false;
                r.Error = "exception: " + e.GetType().Name + ": " + e.Message;
            }
            finally
            {
                sw.Stop();
                r.CostMs = sw.ElapsedMilliseconds;
            }

            CoopLog.Msg(string.Format("spike move id={0} -> ({1},{2}) ok={3} call={4} cost={5}ms",
                stableId, row, col, r.Ok, r.Call ?? "-", r.CostMs));
            return r;
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
