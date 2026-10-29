# provenance — SP5-NPC N4-prep: T2 census fields ใน Sp5T0Probe (มติ 2026-10-30 ทาง (ข)) — สถานะ: **READY · ไม่ deploy**

- label `SP5NPC-N4prep-20261030-045558` · มติผู้ใช้: "(ข) เพิ่ม census field ใน probe offline:
  掌门 30, LoveableSpeHero 57, Teacher=1, BirthSetting; probe ยังเป็น log-only ห้ามเปลี่ยนตรรกะอื่น
  build+provenance ไม่ deploy เทสต์ชุดเต็มต้องไม่น้อยกว่าเดิม" — **ปฏิบัติครบทุกเงื่อนไข**

## งานที่ทำ (ไฟล์เดียว: `src/LongYinCoop/SP5/Sp5T0Probe.cs`)

| field ใหม่ใน event `worldcount` | typed path (dump.cs → interop) | คาดหวัง T2 |
|---|---|---|
| `zhangmen.count` / `.inSpe` / `.leaderIds[30]` | `WorldData.Forces` (List\<ForceData\>) → `ForceData.leader` (int field @0x58 = heroID — **อ่าน field ตรง ไม่เรียก GetLeader()**) | count = **30/30**, ทุก leader ∈ 1..170 |
| `loveable.heroCount` / `.listCount` | `HeroData.loveAble` (bool @0x5F) + `GameDataController.Instance.loveableSpeHeroList` (List\<string\> @0x1D8) | heroCount = **57**, listCount = 57 |
| `teacher.membersWithTeacher` / `.eqLeader` / `.distinct[]` | `HeroData.Teacher` (int @0x31C — heroID ของอาจารย์; `JoinForce(setTeacherToLeader=True)` กำหนด = leader) — นับเฉพาะ cls==2 (สมาชิกสำนัก) เทียบ `leaderIds[belongForceID]` | membersWithTeacher == eqLeader, distinct = ชุด leaderIds (1 อาจารย์/สำนัก = "Teacher=1") |
| `birth.count` / `.vals` | `StartGameSettingController.Instance.BirthSetting` (List\<int\> @0x20) | ค่าเดิมของโลก (baseline จากรอบ N3/T1 จะเป็นตัวเทียบ) |

- `summary.json` เพิ่มบล็อก `censusLast` (snapshot ล่าสุดทั้ง 7 ค่า) — ครอบใช้อ้างในรายงานโดยไม่ต้องแกะ ndjson
- **การอ่านล้มเหลว** (Instance null/exception) → `Swallow()` + `_readfail++` (idiom เดิมของ probe ทุกประการ)
- `teacherVals` cap 64 ค่า (กันหน่วยความจำ — คาดจริง ~30)

## ขอบเขตที่ไม่แตะ (ตามมติ "ห้ามเปลี่ยนตรรกะอื่น")

- ตัวนับเดิมทั้งหมด (`hist` 8 คลาส, `perForce`, `Classify`, `tempMatch/tempOther`, `_ovCalls/_ovResults/...`)
  **ไม่ถูกแตะ** — field ใหม่ append **ท้าย** JSON line (schema เดิม prefix คงเดิมทุก byte)
- hook ทั้ง 7 ตัว + linkage (iid) + patch.audit ไม่เปลี่ยน · ยังเป็น **log-only** (ห้ามเขียน state/args/__result,
  ห้าม skip original, ห้าม raw offset — ครบตาม doc-comment ของคลาสที่เพิ่มบันทึกข้อยกเว้นนี้)
- `tools/sp5t0_audit.py` sanity-check ซ้ำบน ndjson รอบจริง N3 (`20261030-035919`): **VERDICT PASS (structural)**
  เหมือนเดิม — field ใหม่ไม่ทำ tool พัง

## build + เทสต์ (exit code จริง)

| รายการ | ค่า |
|---|---|
| build | `dotnet build src/LongYinCoop -c Release --no-incremental` ×2 — **0 Warning / 0 Error ทั้งครั้ง** · **determinism ×2** (hash เท่ากันทุก byte) |
| **artifact** | DLL **`ba1e1dd14e0cbeaede4bbd57af55477bf45639638a33ad40a2c927f99abeefa0`** (373,760 B) · PDB **`a96a4a38e99c07f768a7d538a23018cc7164be9263b703f9f08390242b8ce132`** (102,920 B) — สำเนาใน evidence โฟลเดอร์นี้ (**gitignored** ตาม .gitignore แถว 63/66 `evidence/**/*.dll\|*.pdb` — คง local · hashes.txt บันทึกครบ) |
| เทียบ build ก่อนหน้า | N2 `2b59e46c…` (371,712 B) → ใหญ่ขึ้น 2,048 B จาก census fields · `Mods\` ยังเป็น `6a94abb5…` (**ไม่ deploy ตามมติ**) |
| C# suite | **693 PASS / 0 FAIL** (= เดิมพอดี — `dotnet run --project tests/S2Protocol.Tests`: S2a-2/S2a-3/S2a-4/TimeV2a + linkage harness 23 + NPC policy 17 + NPC rules 20 · RESULT PASS ทุกชุด · harness พิมพ์ sha256 ของ artifact ที่โหลดจริง) |
| python | **53 OK** + **discover 110 OK** (= เดิม · บรรทัด "FAILED pass=0…" = stdout ของเทสต์ negative-path เอง ตาม N2 provenance) |
| static | `s2_static_check.py` = **CLEAN (0 violations)** |

## ข้อจำกัด / คง UNKNOWN

- census fields เป็น typed read บน Il2Cpp — **ไม่ผ่านเทสต์ offline ได้** (Unity-bound) ตาม idiom hook shells;
  ค่าจริงครั้งแรกจะวัดตอน N4/T2 (deploy ตามวลีสั่งแยก) — **ค่าคาดหวัง 30/57/1 เป็นข้อกำหนดจาก design,
  ยังไม่ RUNTIME-confirmed**
- `BirthSetting.vals` คือค่าที่ผู้เล่นเลือกตอนสร้างโลก — การเทียบ "ถูกต้อง" ทำได้เฉพาะเทียบข้ามรอบบนสำเนาเดียวกัน
- artifact นี้ **ไม่ deploy** — `Mods\LongYinCoop.dll` คงเป็น `6a94abb5…` (ยืนยัน hash ปลาย session)
