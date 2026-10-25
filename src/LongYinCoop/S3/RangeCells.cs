using System.Collections.Generic;

namespace LongYinCoop
{
    /// <summary>
    /// NEG20 follow-up (2026-10-25): pure helpers สำหรับ expose ช่องระยะโจมตีใน
    /// `spike status` (field `attackRangeCells`) จาก list เดียวกับที่ attack path ใช้จริง
    /// (`bc.attackRangeGridUnits` — เทียบ AttackCommand.ListHas/ListCount) — read-only
    /// instrumentation ล้วน ไม่แตะ attack guard / target validation / turn logic
    ///
    /// รูปแบบ payload:
    ///   null  = ไม่มีข้อมูล (bc/list null หรืออ่าน error → runner ต้อง fail-closed NOT-RUN)
    ///   []    = มี list แต่ว่าง (ทุกช่องอยู่นอกระยะ)
    ///   อื่น ๆ = [[row, col], ...] เรียงตามลำดับสมาชิกใน list ของเกม
    /// </summary>
    internal static class RangeCells
    {
        /// <summary>สร้าง payload จากคู่ (row, col) — source null = ไม่มีข้อมูล (คืน null)</summary>
        public static List<int[]> Payload(IEnumerable<(int row, int col)> pairs)
        {
            if (pairs == null) return null;
            var cells = new List<int[]>();
            foreach (var p in pairs) cells.Add(new[] { p.row, p.col });
            return cells;
        }

        /// <summary>
        /// ตรวจว่า (row, col) อยู่ใน payload หรือไม่ — payload null = ไม่อยู่ (ผู้เรียกต้องเช็ค
        /// availability ด้วย payload == null ก่อน เพื่อแยก "ไม่มีข้อมูล" ออกจาก "ไม่อยู่ในระยะ")
        /// · entry ที่รูปร่างไม่ตรง (null / ยาว ≠ 2) ข้าม ไม่ใช้ตัดสิน
        /// </summary>
        public static bool Contains(List<int[]> cells, int row, int col)
        {
            if (cells == null) return false;
            foreach (var c in cells)
            {
                if (c != null && c.Length == 2 && c[0] == row && c[1] == col) return true;
            }
            return false;
        }
    }
}
