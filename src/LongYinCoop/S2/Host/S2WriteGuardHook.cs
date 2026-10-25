namespace LongYinCoop.S2.Host
{
    /// <summary>
    /// กลุ่มจุด entry ของ write path ตาม `S2-plan.md §13.2` ข้อ 5(a)
    /// **ชื่อของเราเอง** — ห้ามอ้างชนิดของเกมหรือเมธอดของ mod ที่เป็นเป้า hook โดยตรง
    /// (ข้อกำหนด W4(c) ของแผน S2a-4 rev4)
    /// </summary>
    public enum S2WriteGuardGroup
    {
        /// <summary>จุดเริ่มคำสั่งเดิน (เมธอดของ mod)</summary>
        MoveEntry = 0,
        /// <summary>จุดเริ่มคำสั่งโจมตี (เมธอดของ mod)</summary>
        AttackEntry = 1,
        /// <summary>ปุ่มหยุดศึก (UI ของเกม)</summary>
        PauseButtonEntry = 2,
        /// <summary>เส้นทางเซฟ/โหลดของเกม — กลุ่มความเสี่ยงสูงสุด (§8.2 แถวที่ 4)</summary>
        SaveLoadEntry = 3,
    }

    /// <summary>การกระทับที่ตัว guard จะทำเมื่อ entry ถูกเรียกขณะ ambient flag เปิด</summary>
    public enum S2WriteGuardAction
    {
        /// <summary>ไม่ทำอะไรนอกนับ counter ราย method (hook ติดตั้งแต่ ambient flag ปิด — §8.5)</summary>
        None = 0,
        /// <summary>บันทึก log + นับ + marker — ไม่ขวางการทำงานของเกม</summary>
        LogCount = 1,
        /// <summary>โยน exception (ใช้ได้เฉพาะกลุ่มที่มติ M5 อนุญาต และห้ามใช้กับกลุ่มเซฟเด็ดขาด)</summary>
        Throw = 2,
    }

    /// <summary>
    /// W4(c) ของ S2a-4 (แผน rev4 §4/§8): **ตรรกะ flag แบบ pure** ของ WriteGuard hook
    ///
    /// ข้อกำหนดเฉพาะ (บังคับโดย `tools/s2_static_check.py`):
    ///   - ห้ามอ้าง `HarmonyLib` และห้ามอ้างชนิดเป้าหมายของ hook
    ///   - **ห้ามถูกเรียกจากโค้ดทำงานจริง** — call site ได้เฉพาะใน tests จนกว่า W4(a)
    ///     (การติดตั้ง hook จริงเกิดได้หลังมติ M5 เท่านั้น — ก่อนหน้านั้นร่างนี้เป็นของอยู่เฉย ๆ)
    ///
    /// กฎการตัดสิน (§8.5/§8.6 rev4):
    ///   - ambientActive=false → `None` (นับ counter เฉย ๆ ไม่เปลี่ยนพฤติกรรมเกม)
    ///   - กลุ่ม SaveLoad → `LogCount` **ทุกกรณี แม้ throwPolicy เปิด** (§8.2 แถวที่ 4: ห้าม throw ใน flow เซฟ)
    ///   - กลุ่มอื่น → ตามนโยบายที่มติ M5 เลือก (ข้อเสนอค่าเริ่มต้น = log+count ไม่ throw ทุกกลุ่ม)
    /// </summary>
    public static class S2WriteGuardHook
    {
        /// <summary>
        /// ตัดสินการกระทำเมื่อจุด entry ใน `group` ถูกเรียก
        /// · `ambientActive` = S2 code path กำลังทำงานอยู่หรือไม่
        /// · `throwPolicy` = มติ M5 อนุญาตให้กลุ่มที่ไม่ใช่เซฟ throw ได้หรือไม่
        /// </summary>
        public static S2WriteGuardAction Decide(S2WriteGuardGroup group, bool ambientActive, bool throwPolicy)
        {
            if (!ambientActive) return S2WriteGuardAction.None;
            if (group == S2WriteGuardGroup.SaveLoadEntry) return S2WriteGuardAction.LogCount;
            return throwPolicy ? S2WriteGuardAction.Throw : S2WriteGuardAction.LogCount;
        }

        /// <summary>false เฉพาะกลุ่มเซฟ/โหลด — กลุ่มนี้ห้าม throw ทุกกรณีตาม §8.2/§8.6</summary>
        public static bool MayEverThrow(S2WriteGuardGroup group)
        {
            return group != S2WriteGuardGroup.SaveLoadEntry;
        }
    }
}
