# Gate 2 — CRASH REPORT (hard stop) · 2026-10-25 04:14–04:17

## เหตุการณ์

| เวลา | เหตุการณ์ | หลักฐาน |
|---|---|---|
| 04:14:21.376–.389 | runner เริ่ม w2 (set_pref allowRemoteInput=true · wait_stable · เคส 5: arm 120 s) | game-log-crash.log |
| 04:14:21.398 | **`spike attack id=0:0 target=1:10066 confirmed=true (deferred hp check)`** — เกม**รับ**คลิก (attackComfirmed=true) | game-log-crash.log |
| 04:14:21 | **เกม crash** — Windows Event 1000: `Faulting module UnityPlayer.dll · Exception 0xc0000005 (access violation) · offset 0x1322c1e · pid 0x8470` | crash-events.json |
| 04:14:51–52 | WER 1001 + IPC disconnect สุดท้าย | ข้างต้น |

## ลำดับเคสที่เกิดจริง (จาก log + โค้ด runner)

- เคส 5, 6 (NOT_ACTIVE id=0:9/0:86): ถูก guard ปฏิเสธที่ตัวกรอก (rejection ไม่เข้า log — ตาม design) — ไม่ใช่ต้นเหตุ
- เคส 15, 16 (STATE move ใน WaitAttacking), 19 (SELF): ถูกปฏิเสธ — ไม่เข้า log
- **เคส 7 (OUT_OF_RANGE id=0:0 → 1:10066): เกมรับจริง (confirmed=true)** — ขัด expectation `OUT_OF_RANGE` ของแผน: 1:10066 @(7,17) อยู่**ในระยะ**ของ 0:0 @(6,0) ตาม `attackRangeGridUnits` (12 ช่อง ณ เวลานั้น) — สมมติฐาน "ทุกศัตรูอยู่นอกระยะ" ของ w2 ผิดสำหรับ layout ศึกนี้ (ใน S1.5f ID เดิมอยู่นอกระยะเพราะคนละ layout)
- เกมเริ่มรันลำดับโจมตีของตัวเอง (BattleUnitAttackStart → …) แล้ว **crash ระหว่างนั้น** — runner ตายพร้อมกัน (pipe หาย) ก่อนเขียน `negatives-w2.json` (ไม่มีผลเคสใดของ w2 ที่บันทึกได้)

## การวิเคราะห์ (แยกข้อเท็จจริงจากการอนุมาน — ยังตัดสินไม่ได้)

- **ข้อเท็จจริง:** crash เป็น native access violation ใน UnityPlayer.dll (ไม่ใช่ managed exception) · เกิดหลัง BattleGridClicked ถูกยอมรับ ระหว่างเฟรมต่อ ๆ มาของการรันลำดับโจมตี · มี TypeLoadException noise ของ MelonLoader (UIElements type cache) ตั้งแต่ boot — บันทึกไว้ว่าน่าจะไม่เกี่ยวแต่ยังพิสูจน์ไม่ได้
- **UNKNOWN — ต้นเหตุ:** จำแนกไม่ได้จากหลักฐานรอบนี้ว่า crash เกิดจาก (a) พฤติกรรมเกมเองกับ BattleGridClicked ใน state ที่บอบบาง (ความเสี่ยงที่ STAGEPLAN §6 ระบุไว้แล้ว) (b) โค้ดใหม่ของ S3-prereq (Tick poll `SafeUnitAlive`+`hp` ทุกเฟรม — C# ล้วน try/catch ไม่น่าทำ native AV) (c) ปัจจัยแวดล้อม · ที่ยังไม่เคยทดสอบ: A/B กับ build เก่า `5d7b7cc5`
- **ข้อต่างจาก S1.5e (โจมตี 10/10 ผ่าน):** build ต่างกัน (51775814 vs f2ad30cd) · S1.5e ยิงในศึก layout ของตัวเอง 10 ครั้ง — ไม่เคย crash · **เป็นได้ทั้งสองข้าง ยังพิสูจน์ไม่ได้ = UNKNOWN**

## การยุติที่ทำแล้ว (ตามขั้น 4 ของ Gate 2)

| ขั้น | ผล |
|---|---|
| เกมปิด | crash เอง — ผู้ใช้ไม่ต้องปิด |
| save restore | `save-split restore --label S3p-20261025-0341` = **Save = real folder renamed กลับ (ไม่เคยถูกเขียน)** · 57 ไฟล์ `7332fc93eb98` EXACT · copy เก็บที่ `coop-test/S3p-20261025-0341/Save.used-20261025-041656` (drift = pref เท่านั้น) |
| DLL rollback | `Mods\LongYinCoop.dll` = **`5d7b7cc5520ae784c5efc198dda8c1c3ba33ff3c55eb7aaa955ede5f63735953`** EXACT · PDB `3a713953…` EXACT · DLL ใหม่เก็บไว้ที่ `Mods\LongYinCoop.dll.s3prereq-f2ad30cd` (+pdb) ยังไม่ถูกลบ |
| leftovers | `none` (ยืนยันด้วย save-split status) |
| baseline | ไม่แตะทุกตัว |

## ผลรวม Runtime Prerequisite (สถานะตอนนี้)

| หน้าต่าง | เคส | ผล |
|---|---|---|
| w1 | 7 | **7/7 PASS** (`negatives-w1.json` — stateHash same ทุกเคส) |
| w2 | 9 | **ABORTED ที่เคส 7** (crash — ไม่มีผลบันทึกได้) |
| w3 | 4 | **NOT RUN** |
| จบศึกได้ตามปกติ | — | **NOT MET** (ศึกจบด้วย crash — ไม่นับ) |

**สถานะรวม = FAIL/BLOCKED (crash ระหว่างทดสอบ) — ต้องรอมติผู้ใช้ก่อนทดลองซ้ำทางใด**
