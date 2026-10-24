namespace LongYinCoop
{
    /// <summary>
    /// S3 prerequisite (มติผู้ใช้ 2026-10-25 ส่วน D + รีวิวรอบ 3): นิยาม alive แบบ pure
    /// ไม่อ้างอิงชนิดของเกมใด ๆ เพื่อให้ unit test เรียกตรงได้ (link เข้า S2Protocol.Tests)
    ///
    /// ข้อเท็จจริงที่อ้าง [RUNTIME, S1.5f §3]:
    /// - HeroData.dead ไม่ใช่ธงตายในศึก (ศพ: dead=false, hp=0, BattleUnit.IsAlive=false)
    /// - IsAlive ยัง true ในเฟรมที่ hp→0 ⇒ ต้องเช็ค hp&gt;0 เสริม
    /// ⇒ นิยาม = isAlive &amp;&amp; hp&gt;0 · NaN ⇒ false (ไม่มีหลักฐานว่า alive — fail-closed)
    ///
    /// ผู้ใช้นิยาม: stateHash v2 (TurnGate) และ guard เป้าหมายของ spike attack (AttackCommand)
    /// — เคสขอบ isAlive=true แต่ hp&lt;=0 ให้คำตอบเดียวกันทั้งสองที่ (= ไม่ alive / ไม่ให้โจมตี)
    /// </summary>
    internal static class AliveRule
    {
        /// <summary>Pure alive rule of S3 prerequisite: IsAlive &amp;&amp; hp&gt;0 (NaN → false)</summary>
        internal static bool AliveForS3(bool isAlive, float hp)
        {
            return isAlive && hp > 0f;
        }

        /// <summary>Completion ของ spike attack (รีวิวรอบ 4, 2026-10-25): pure —
        /// คืน known=true เฉพาะเมื่ออ่าน isAlive และ hp ได้ครบทั้งคู่
        /// · known=true ⇒ aliveAfter = AliveForS3 (predicate เดียวกับ stateHash และ attack guard)
        /// · known=false ⇒ unknown — ผู้เรียกต้องใช้ Known=false แสดงสถานะ
        ///   ห้ามตีความ aliveAfter=false ว่า dead (การไม่แยก TARGET_UNREADABLE ออกค้างเป็นงาน validator ของ S3)</summary>
        internal static bool CompletionAlive(bool isAliveRead, bool isAlive, bool hpRead, float hp, out bool aliveAfter)
        {
            if (!isAliveRead || !hpRead)
            {
                aliveAfter = false;
                return false;
            }
            aliveAfter = AliveForS3(isAlive, hp);
            return true;
        }
    }
}
