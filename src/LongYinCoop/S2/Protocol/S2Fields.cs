using System;

namespace LongYinCoop.S2.Protocol
{
    /// <summary>
    /// รายการ field ที่ replicate (ใช้คำนวณ `fieldHash` ของ handshake)
    ///
    /// Game anchor = SHA-256 ของ `GameAssembly.dll` + `global-metadata.dat` ที่ปักหมุดใน
    /// `VERSION.md` (2026-10-20) — **S2a-3 ไม่ hash ไฟล์จริงตอนรัน** (การเทียบ hash ของเกมเป็น
    /// session-start check ตาม AGENTS กติกา 9) ⇒ anchor นี้แปลว่า "schema ของ replica บน build ที่ปักหมุด"
    /// ไม่ใช่ "ตรวจว่าเกมยังเป็น build เดิม" · ถ้าเกมอัปเดต ต้องแก้ anchor นี้พร้อม VERSION.md
    /// </summary>
    public static class S2Fields
    {
        public const string GameAnchor =
            "game|33c56def4102844a1190eec963a05c834cfcf992d42b423d485ce8584d7920c1"
            + "|451311d4dc8c1b8b5073905c8b39a9781bd7e1f44ed88fbf7b35f5345c18bfd0";

        public static readonly string[] Replicated =
        {
            "envelope.sessionId", "envelope.hostApplyUs", "envelope.replicaHash",
            "envelope.resync", "envelope.resyncFromSeq",
            "header.battleKey", "header.turnIndex", "header.activeUnitId", "header.paused",
            "header.nowActiveState", "header.battleTime",
            "unit.stableId", "unit.team", "unit.heroId", "unit.row", "unit.column",
            "unit.hp", "unit.maxHp", "unit.isAlive", "unit.unitAlive", "unit.moved",
            "unit.attacked", "unit.playerControl",
        };

        private static readonly string _hash = S2Auth.FieldHash(Replicated, GameAnchor);

        public static string Hash { get { return _hash; } }

        public static string Prefix { get { return S2Auth.FieldHashPrefix(_hash); } }

        /// <summary>รายชื่อ field แบบบรรทัดเดียว (สำหรับ marker/log — ไม่มี secret)</summary>
        public static string Inventory { get { return string.Join(",", Replicated); } }
    }
}
