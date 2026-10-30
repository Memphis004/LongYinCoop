using System;
using System.Collections.Generic;

namespace LongYinCoop
{
    /// <summary>
    /// T7 census ของ stage N5 (มติผู้ใช้ 2026-10-30 ทาง (ข) "เพิ่ม census field ใน probe" —
    /// ตามร่าง plans/sp5-npc-n5-approval-request.md §T7) — pure ล้วน ไม่แตะ Il2Cpp/Unity ⇒
    /// เทสต์ offline ได้ (ลิงก์แยกใน tests/S2Protocol.Tests.csproj ตาม convention ของ Sp5T0NameMatch)
    ///
    /// ข้อมูลดิบทั้งหมดถูกอ่านแบบ typed อย่างเดียวโดย Sp5T0Probe.SnapshotWorld (ห้าม raw offset
    /// ตามกติกา 8) แล้วส่งมาเป็น plain C# data — คลาสนี้คำนวณและจำแนกเท่านั้น ไม่มีการเขียน
    /// state เกมใด ๆ:
    ///   - <c>dictH</c>/<c>dictT</c> = Count ของ WorldData.HerosDict / TempHerosDict (-1 = อ่านไม่ได้ ตาม convention)
    ///   - <c>heroIds</c> = heroID ทุกตัวที่อ่านได้จาก list Heros (entry null probe ข้ามไว้แล้ว)
    ///   - <c>leaderIds</c> = ForceData.leader 30 สำนัก (บล็อก T2 zhangmen คำนวณแล้ว — probe ส่งต่อ ไม่อ่านซ้ำ)
    ///   - <c>ownHerosPerForce</c> = ForceData.ownHeros คัดลอกต่อสำนัก (null = สำนัก/list อ่านไม่ได้)
    ///
    /// นิยามผลลัพธ์ (เกณฑ์ T7 ของคำขอ N5):
    ///   dict count == list count → เทียบ DictH กับ h และ DictT กับ t ในบรรทัด worldcount เดียวกัน
    ///   heroID ไม่ซ้ำใน Heros → DupCount = readable − distinct
    ///   leader มีอยู่จริงใน Heros → LeaderExists เทียบ LeaderNonZero (leader==0 = ไม่มี ไม่นับ)
    ///   ownHeros ครบ → OwnDangling = entry ที่ไม่มี heroID นั้นใน Heros
    /// </summary>
    internal static class Sp5T0T7Census
    {
        /// <summary>cap จำนวน id ที่ลง log ต่อกลุ่ม (dup / leader-missing / own-dangling) — กันไฟล์โตถ้าโลกพังหนัก</summary>
        internal const int ListCap = 16;

        internal struct Result
        {
            public int DictH;                  // HerosDict.Count (-1 = อ่านไม่ได้ / null)
            public int DictT;                  // TempHerosDict.Count (-1 = อ่านไม่ได้ / null)
            public int IdsReadable;            // heroID ที่อ่านได้ (entry non-null ใน Heros)
            public int IdsDistinct;            // heroID ไม่ซ้ำ
            public int DupCount;               // readable − distinct
            public List<int> DupIds;           // id ที่ปรากฏ >1 ครั้ง (cap ListCap — เรียงขึ้นตามลำดับพบ)
            public bool DupTruncated;          // true = DupIds เกิน cap ถูกตัด
            public int LeaderNonZero;          // สำนักที่ leader != 0
            public int LeaderExists;           // สำนักที่ leader != 0 และมี heroID นั้นใน Heros
            public int LeaderMissing;          // จำนวนเต็มที่ไม่มีใน Heros (list ด้านล่าง cap ที่ ListCap)
            public List<int> LeaderMissingIds; // leader != 0 ที่ไม่มีใน Heros (cap ListCap)
            public bool LeaderMissingTruncated;
            public int LeaderForcesRead;       // จำนวนช่อง leaderIds ที่ได้ค่า (null array = -1)
            public int[] OwnPerForce;          // จำนวน entry ownHeros ต่อสำนัก (ช่องอ่านไม่ได้ = -1)
            public int OwnForcesRead;          // จำนวนสำนักที่อ่าน ownHeros สำเร็จ
            public int OwnTotal;               // Σ entry ownHeros ของสำนักที่อ่านได้
            public int OwnDangling;            // entry ที่ heroID ไม่มีใน Heros
            public List<int> OwnDanglingIds;   // cap ListCap
            public bool OwnDanglingTruncated;
        }

        /// <summary>คำนวณทั้งหมดจากข้อมูลดิบ — ไม่โยน (input null/ค่า -1 = ผลลัพธ์สถานะ "อ่านไม่ได้" ตาม convention)</summary>
        internal static Result Compute(int dictH, int dictT, int[] heroIds, int[] leaderIds, int[][] ownHerosPerForce)
        {
            Result r = new Result();
            r.DictH = dictH;
            r.DictT = dictT;
            r.DupIds = new List<int>();
            r.LeaderMissingIds = new List<int>();
            r.OwnDanglingIds = new List<int>();

            // ---- heroID set + dedup (Heros เท่านั้น ตามเกณฑ์ T7) ----
            // DupIds = id ที่ซ้ำ **เฉพาะตัวแรกที่พบว่าซ้ำ** (id เดียวซ้ำกี่ครั้งก็บันทึก 1 ครั้ง)
            HashSet<int> idSet = new HashSet<int>();
            HashSet<int> dupSeen = new HashSet<int>();
            if (heroIds != null)
            {
                r.IdsReadable = heroIds.Length;
                for (int i = 0; i < heroIds.Length; i++)
                {
                    if (idSet.Add(heroIds[i])) continue;
                    if (!dupSeen.Add(heroIds[i])) continue;
                    if (r.DupIds.Count < ListCap) r.DupIds.Add(heroIds[i]);
                    else r.DupTruncated = true;
                }
                r.IdsDistinct = idSet.Count;
                r.DupCount = r.IdsReadable - r.IdsDistinct;
            }

            // ---- leader-exists ต่อสำนัก (leader==0 = ไม่มีผู้นำ ไม่นับทั้งสองฝั่ง) ----
            if (leaderIds == null)
            {
                r.LeaderNonZero = -1;
                r.LeaderExists = -1;
                r.LeaderForcesRead = -1;
            }
            else
            {
                r.LeaderForcesRead = leaderIds.Length;
                for (int fi = 0; fi < leaderIds.Length; fi++)
                {
                    int lid = leaderIds[fi];
                    if (lid == 0) continue;
                    r.LeaderNonZero++;
                    if (idSet.Contains(lid)) r.LeaderExists++;
                    else
                    {
                        r.LeaderMissing++;
                        if (r.LeaderMissingIds.Count < ListCap) r.LeaderMissingIds.Add(lid);
                        else r.LeaderMissingTruncated = true;
                    }
                }
            }

            // ---- ownHeros ต่อสำนัก (ครบ = ทุก entry มี heroID ใน Heros) ----
            if (ownHerosPerForce == null)
            {
                r.OwnPerForce = null;
                r.OwnForcesRead = -1;
                r.OwnTotal = -1;
                r.OwnDangling = -1;
            }
            else
            {
                r.OwnPerForce = new int[ownHerosPerForce.Length];
                for (int fi = 0; fi < ownHerosPerForce.Length; fi++)
                {
                    int[] oh = ownHerosPerForce[fi];
                    if (oh == null) { r.OwnPerForce[fi] = -1; continue; }
                    r.OwnForcesRead++;
                    r.OwnPerForce[fi] = oh.Length;
                    r.OwnTotal += oh.Length;
                    for (int j = 0; j < oh.Length; j++)
                    {
                        if (idSet.Contains(oh[j])) continue;
                        r.OwnDangling++;
                        if (r.OwnDanglingIds.Count < ListCap) r.OwnDanglingIds.Add(oh[j]);
                        else r.OwnDanglingTruncated = true;
                    }
                }
            }
            return r;
        }
    }
}
