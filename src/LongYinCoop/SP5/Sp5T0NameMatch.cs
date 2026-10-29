using System;
using System.Collections.Generic;

namespace LongYinCoop
{
    /// <summary>
    /// กติกา match ชื่อของ T2 ข้อ loveable (N4-follow-up ร่าง 1 — มติผู้ใช้ 2026-10-30 อนุมัติตามร่าง)
    /// pure ล้วน ไม่แตะ Il2Cpp/Unity ⇒ นำเทสต์ offline ได้ (Sp5T0Probe.cs ไม่ลิงก์ใน tests —
    /// คลาสนี้ถูกลิงก์แยกตาม convention ของ tests/S2Protocol.Tests.csproj)
    ///
    /// นิยาม (แทนข้อสมมติเดิม "flag loveAble = สมาชิก list" ซึ่งถูกหักล้าง — 744/883, 631/632):
    /// ชื่อใน `loveableSpeHeroList` 1 ชื่อ = "present" ก็ต่อเมื่อมี HeroData ใน Heros อย่างน้อย 1 ตัว
    /// ที่ `heroName` ตรงชื่อนั้น **หรือ** `heroFamilyName + heroName` ตรงชื่อนั้น (เทียบแบบ Ordinal)
    /// · ชื่อที่ไม่ match = "missing" — probe ต้อง log ออกมาทุกชื่อตามมติ
    /// · กติกา match ยังไม่ RUNTIME-confirmed — **รอบแรกที่ใช้บนเกม = การยืนยันกติกา**
    /// (มติ: ถ้า match ไม่ตรงเพราะรูปแบบชื่อ = กรณีหยุดรายงาน ห้ามปรับกติกาในเซสชันเอง)
    /// </summary>
    internal static class Sp5T0NameMatch
    {
        /// <summary>cap จำนวนชื่อ missing ที่ลง log (list จริง 57 — cap กันไฟล์โตถ้า list โตผิดปกติ)</summary>
        internal const int MissingCap = 64;

        internal struct MatchResult
        {
            public int Total;              // จำนวน entry ใน list (null นับรวม)
            public int Present;            // entry ที่ match ได้ ≥1 hero
            public List<string> Missing;   // entry ที่ match ไม่ได้ (ตามลำดับ list, cap ที่ MissingCap — null คง null)
            public bool MissingTruncated;  // true = missing เกิน cap ถูกตัด
        }

        /// <summary>ตรวจทุกชื่อใน list เทียบสอง set ชื่อ hero — ไม่โยน (input null = ทุกชื่อ missing)</summary>
        /// <param name="listNames">ชื่อจาก loveableSpeHeroList (entry null/empty = missing)</param>
        /// <param name="givenNames">set ของ HeroData.heroName (probe เตรียม — entry ว่างไม่ใส่)</param>
        /// <param name="fullNames">set ของ HeroData.heroFamilyName + HeroData.heroName</param>
        internal static MatchResult Match(IList<string> listNames, HashSet<string> givenNames, HashSet<string> fullNames)
        {
            MatchResult r = new MatchResult();
            r.Missing = new List<string>();
            if (listNames == null) return r;
            r.Total = listNames.Count;
            HashSet<string> g = givenNames;
            HashSet<string> f = fullNames;
            for (int i = 0; i < listNames.Count; i++)
            {
                string nm = listNames[i];
                if (!string.IsNullOrEmpty(nm) && g != null && f != null && (g.Contains(nm) || f.Contains(nm)))
                    r.Present++;
                else
                {
                    if (r.Missing.Count < MissingCap) r.Missing.Add(nm);
                    else r.MissingTruncated = true;
                }
            }
            return r;
        }
    }
}
