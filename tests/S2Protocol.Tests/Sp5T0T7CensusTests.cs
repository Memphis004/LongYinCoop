using System;
using System.Collections.Generic;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// Stage N5 (มติผู้ใช้ 2026-10-30 ทาง (ข)) — เทสต์ pure ของ T7 census (Sp5T0T7Census) —
    /// ไม่ใช้ Il2Cpp/Unity: dict count passthrough · heroID dedup (DupIds บันทึกครั้งเดียวต่อ id,
    /// cap 16 + truncated) · leader-exists (leader==0 ไม่นับ) · ownHeros dangling + perForce.
    /// ข้อมูลดิบ = สิ่งที่ Sp5T0Probe.SnapshotWorld อ่าน typed แล้วส่งมา — คลาสนี้ทดสอบส่วนคำนวณเท่านั้น
    /// </summary>
    internal static class Sp5T0T7CensusTests
    {
        private static int _pass;
        private static int _fail;

        private static void Check(string name, bool ok)
        {
            Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] (t7) {name}");
            if (ok) _pass++; else _fail++;
        }

        public static int Run()
        {
            _pass = 0;
            _fail = 0;
            Console.WriteLine();
            Console.WriteLine("== SP5-NPC T7 census (N5 — pure) ==");

            // ---- 1) โลกสะอาด: 631 hero distinct, dict ตรง, leader ครบ, ownHeros ครบ ----
            int[] ids = new int[631];
            for (int i = 0; i < 631; i++) ids[i] = i + 1;
            int[] leaders = new int[30];
            int[][] own = new int[30][];
            for (int fi = 0; fi < 30; fi++)
            {
                leaders[fi] = fi + 1;            // ทุกสำนักมี leader อยู่ใน Heros
                own[fi] = new int[] { fi + 1 };  // 1 คนต่อสำนัก มีจริง
            }
            Sp5T0T7Census.Result r = Sp5T0T7Census.Compute(631, 88, ids, leaders, own);
            Check("โลกสะอาด: dup=0 distinct=631 readable=631",
                  r.DupCount == 0 && r.IdsDistinct == 631 && r.IdsReadable == 631);
            Check("โลกสะอาด: dict passthrough dictH=631 dictT=88", r.DictH == 631 && r.DictT == 88);
            Check("โลกสะอาด: leaderNonZero=30 exists=30 missing=0",
                  r.LeaderNonZero == 30 && r.LeaderExists == 30 && r.LeaderMissing == 0 &&
                  r.LeaderMissingIds.Count == 0 && !r.LeaderMissingTruncated);
            Check("โลกสะอาด: ownHeros total=30 dangling=0 forcesRead=30",
                  r.OwnTotal == 30 && r.OwnDangling == 0 && r.OwnForcesRead == 30);
            Check("โลกสะอาด: ownPerForce ครบ 30 ช่อง ช่องละ 1",
                  r.OwnPerForce != null && r.OwnPerForce.Length == 30 && r.OwnPerForce[0] == 1 && r.OwnPerForce[29] == 1);

            // ---- 2) dedup: id เดียวซ้ำหลายครั้ง = บันทึกครั้งเดียว ----
            r = Sp5T0T7Census.Compute(-1, -1, new int[] { 1, 2, 2, 3, 2 }, new int[0], new int[0][]);
            Check("dedup: [1,2,2,3,2] → readable=5 distinct=3 dup=2", r.IdsReadable == 5 && r.IdsDistinct == 3 && r.DupCount == 2);
            Check("dedup: DupIds=[2] ครั้งเดียว ไม่ truncated", r.DupIds.Count == 1 && r.DupIds[0] == 2 && !r.DupTruncated);
            Check("dedup: leaderIds ว่าง → nonZero=0 exists=0 forcesRead=0",
                  r.LeaderNonZero == 0 && r.LeaderExists == 0 && r.LeaderForcesRead == 0);
            Check("dedup: ownHeros ว่าง → total=0 dangling=0 forcesRead=0 perForce ยาว 0",
                  r.OwnTotal == 0 && r.OwnDangling == 0 && r.OwnForcesRead == 0 &&
                  r.OwnPerForce != null && r.OwnPerForce.Length == 0);

            // ---- 3) cap: 17 id ที่ต่างกัน ซ้ำคนละครั้ง → DupIds=16 + truncated ----
            int[] manyDups = new int[34];
            for (int i = 0; i < 17; i++) { manyDups[2 * i] = 100 + i; manyDups[2 * i + 1] = 100 + i; }
            r = Sp5T0T7Census.Compute(5, 5, manyDups, null, null);
            Check("cap: 17 id ซ้ำ → DupIds 16 + truncated=true", r.DupIds.Count == 16 && r.DupTruncated);
            Check("cap: dupCount = 17 (ตัวนับเต็ม ไม่โดน cap)", r.DupCount == 17);
            r = Sp5T0T7Census.Compute(5, 5, null, null, null);
            Check("cap: heroIds null → readable/distinct/dup เป็น 0 แต่ dict passthrough",
                  r.IdsReadable == 0 && r.IdsDistinct == 0 && r.DupCount == 0 && r.DictH == 5 && r.DictT == 5);

            // ---- 4) leader: leader==0 ไม่นับ · leader ไม่มีใน Heros = missing ----
            leaders = new int[] { 0, 5, 999, 0, 7 };
            r = Sp5T0T7Census.Compute(10, 0, new int[] { 1, 5, 7 }, leaders, null);
            Check("leader: nonZero=3 (0 ข้าม) exists=2 missingCount=1",
                  r.LeaderNonZero == 3 && r.LeaderExists == 2 && r.LeaderMissing == 1);
            Check("leader: missingIds=[999] ไม่ truncated", r.LeaderMissingIds.Count == 1 && r.LeaderMissingIds[0] == 999 && !r.LeaderMissingTruncated);
            Check("leader: forcesRead=5", r.LeaderForcesRead == 5);

            // ---- 5) leaderIds null = อ่านไม่ได้ (-1 ตาม convention) ----
            r = Sp5T0T7Census.Compute(3, 3, new int[] { 1 }, null, new int[][] { new int[] { 1 } });
            Check("leader null → nonZero=-1 exists=-1 forcesRead=-1",
                  r.LeaderNonZero == -1 && r.LeaderExists == -1 && r.LeaderForcesRead == -1);
            Check("leader null: ownHeros ยังคำนวณได้ปกติ", r.OwnTotal == 1 && r.OwnDangling == 0);

            // ---- 6) ownHeros: dangling + สำนักอ่านไม่ได้ (null slot = -1 ใน perForce) ----
            own = new int[30][];
            own[0] = new int[] { 1, 2, 99 };     // 99 = dangling
            own[1] = null;                        // สำนักอ่านไม่ได้
            own[2] = new int[] { 5, 5 };          // มีจริง (ซ้ำใน list ของตัวเอง — ไม่ใช่ dangling)
            for (int fi = 3; fi < 30; fi++) own[fi] = new int[0];
            r = Sp5T0T7Census.Compute(7, 0, new int[] { 1, 2, 5 }, new int[30], own);
            Check("ownHeros: total=5 dangling=1 forcesRead=29",
                  r.OwnTotal == 5 && r.OwnDangling == 1 && r.OwnForcesRead == 29);
            Check("ownHeros: danglingIds=[99]", r.OwnDanglingIds.Count == 1 && r.OwnDanglingIds[0] == 99 && !r.OwnDanglingTruncated);
            Check("ownHeros: perForce[0]=3 perForce[1]=-1 perForce[2]=2 perForce[3]=0",
                  r.OwnPerForce[0] == 3 && r.OwnPerForce[1] == -1 && r.OwnPerForce[2] == 2 && r.OwnPerForce[3] == 0);

            // ---- 7) ownHeros cap: 17 dangling ต่างค่า → 16 + truncated (ตัวนับเต็ม 17) ----
            own = new int[1][];
            int[] dang = new int[17];
            for (int i = 0; i < 17; i++) dang[i] = 500 + i;
            own[0] = dang;
            r = Sp5T0T7Census.Compute(1, 0, new int[] { 1 }, new int[1], own);
            Check("ownHeros cap: dangling=17 (เต็ม) danglingIds=16 + truncated",
                  r.OwnDangling == 17 && r.OwnDanglingIds.Count == 16 && r.OwnDanglingTruncated);

            // ---- 8) ownHerosPerForce null ทั้งอาร์เรย์ = อ่านไม่ได้ทั้งกลุ่ม ----
            r = Sp5T0T7Census.Compute(2, 2, new int[] { 1, 2 }, new int[] { 1 }, null);
            Check("ownHeros null → forcesRead=-1 total=-1 dangling=-1 perForce=null",
                  r.OwnForcesRead == -1 && r.OwnTotal == -1 && r.OwnDangling == -1 && r.OwnPerForce == null);

            // ---- 9) heroID 0 = ปกติ (ตัดสินจาก membership จริง ไม่มี special case) ----
            r = Sp5T0T7Census.Compute(2, 0, new int[] { 0, 1 }, new int[] { 0 }, new int[][] { new int[] { 0 } });
            Check("id 0: มีใน Heros = ไม่ missing ไม่ dangling", r.LeaderExists == 0 && r.LeaderNonZero == 0 &&
                  r.OwnDangling == 0 && r.OwnTotal == 1);

            // ---- 10) input null ทุกตัว = สถานะอ่านไม่ได้ ไม่โยน ----
            r = Sp5T0T7Census.Compute(-1, -1, null, null, null);
            Check("null ทุกตัว: dict passthrough -1, ids 0, leader -1, own -1 — ไม่โยน",
                  r.DictH == -1 && r.DictT == -1 && r.IdsReadable == 0 && r.LeaderForcesRead == -1 && r.OwnForcesRead == -1);

            Console.WriteLine($"(t7) pass={_pass} fail={_fail}");
            return _fail;
        }
    }
}
