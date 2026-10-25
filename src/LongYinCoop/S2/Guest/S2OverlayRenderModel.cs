using System;
using System.Globalization;

namespace LongYinCoop.S2.Guest
{
    /// <summary>ผลการเลือกเฟรมสำหรับเฟรมนี้ของ OnGUI</summary>
    public struct S2OverlaySelection
    {
        public bool HasSnapshot;
        public GuestTimedSnapshot Snapshot;
        public long AgeMs;
        public bool Stale;
    }

    /// <summary>
    /// W3 (แผน S2a-4 §4): โมเดลการเลือกเฟรมของ overlay — **pure** (เทสต์ได้ใน suite)
    ///
    /// smoothing 100 ms (STAGEPLAN §5): เลือก "snapshot ใหม่สุดที่อายุ ≥ smoothingMs"
    /// เพื่อกลืน jitter ของอัตราส่ง · **ห้าม extrapolate** — ไม่เคยสร้าง/เดาสถานะที่ยังไม่มาถึง
    /// (ไม่มีเฟรมพอ → ใช้เฟรมเก่าสุดที่มีอยู่จริง) · เก่าเกิน `StaleMs` = ติดป้าย stale
    /// </summary>
    public static class S2OverlayRenderModel
    {
        /// <summary>ความหน่วงแสดงผลกลืน jitter (STAGEPLAN §5: smoothing 100 ms)</summary>
        public const int SmoothingMs = 100;
        /// <summary>เก่ากว่านี้ = ข้อมูลค้าง ให้ติดป้าย stale บน overlay</summary>
        public const int StaleMs = 3000;

        /// <summary>เงื่อนไขว่าจะวาดหรือไม่: ต้องเปิด overlay และมีข้อมูลอย่างน้อย 1 เฟรม (overlay=false ไม่มีผลใด)</summary>
        public static bool ShouldRender(bool overlayEnabled, int ringLength)
        {
            return overlayEnabled && ringLength > 0;
        }

        /// <summary>
        /// เลือกเฟรมจาก ring (เรียงเก่า→ใหม่ ตามสัญญาของ `S2OverlayClient.CopyRing`)
        /// extrapolate=false: ผลลัพธ์เป็นสมาชิกของ ring เสมอ หรือ HasSnapshot=false
        /// </summary>
        public static S2OverlaySelection Select(GuestTimedSnapshot[] ring, long nowMs, int smoothingMs = SmoothingMs)
        {
            S2OverlaySelection selection = default(S2OverlaySelection);
            if (ring == null || ring.Length == 0) return selection;

            if (smoothingMs < 0) smoothingMs = 0;

            // ใหม่สุดที่ "พ้นหน้าต่าง smoothing" — ไล่จากใหม่ไปเก่า
            for (int i = ring.Length - 1; i >= 0; i--)
            {
                long age = nowMs - ring[i].ReceivedAtMs;
                if (age >= smoothingMs)
                {
                    return Finish(ring[i], age);
                }
            }

            // ทุกเฟรมยังใหม่กว่าหน้าต่าง → ใช้เฟรมเก่าสุดที่มีอยู่จริง (ไม่เดาเฟรมถัดไป)
            long oldestAge = nowMs - ring[0].ReceivedAtMs;
            return Finish(ring[0], oldestAge);
        }

        private static S2OverlaySelection Finish(GuestTimedSnapshot snapshot, long ageMs)
        {
            return new S2OverlaySelection
            {
                HasSnapshot = true,
                Snapshot = snapshot,
                AgeMs = ageMs < 0 ? 0 : ageMs,
                Stale = ageMs > StaleMs,
            };
        }
    }

    /// <summary>ประกอบข้อความของ overlay จาก template ใน `UIStrings` (pure — ไม่แตะ GUI)</summary>
    public static class S2OverlayText
    {
        public static string SnapshotLine(GuestSnapshotDisplay d)
        {
            return string.Format(CultureInfo.InvariantCulture, LongYinCoop.UIStrings.OverlaySnapshotArgs,
                d.BattleKey, d.TurnIndex, d.BattleTime.ToString("F1", CultureInfo.InvariantCulture),
                string.IsNullOrEmpty(d.ActiveUnitId) ? "-" : d.ActiveUnitId, d.Units.Length);
        }

        public static string UnitLine(GuestUnitDisplay u)
        {
            return string.Format(CultureInfo.InvariantCulture, LongYinCoop.UIStrings.OverlayUnitArgs,
                u.StableId, u.PlayerControl ? "*" : " ",
                u.Row, u.Column, u.HpText,
                u.Dead ? " DEAD" : string.Empty,
                u.Moved ? " M" : string.Empty,
                u.Attacked ? " A" : string.Empty);
        }
    }
}
