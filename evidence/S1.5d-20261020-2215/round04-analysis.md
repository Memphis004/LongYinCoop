# S1.5-d / รอบ 4 — ลำดับ call จริงของการเดิน 1 ครั้ง (วัดจากผู้เล่นกดเอง) [RUNTIME]

วันที่ 2026-10-20 22:26:02–22:29:16 · build ที่รัน `b278955e33c8` (probe W1/W2 เดิม) · panic switch ปิด (`AllowRemoteInput=false`)

## ไฟล์ดิบ
| ไฟล์ | sha256 (16) | events |
|---|---|---|
| `raw/jitprobe-20261020-222447.ndjson` (W1: OnClick + BattleGridClicked) | `3f2db513474ceadc` | 16 |
| `raw/jitprobe-w2-20261020-222447.ndjson` (W2: move chain + state หลัง call) | `382b3af2e90805da` | 30 |
| `raw/compat-notes-2229.md` (Harmony inventory) | — | 50 patched |

`stat` ยืนยัน: ไฟล์ถูกสร้าง 22:26:02 (Birth) = เวลา event แรกจริง ไม่ใช่ไฟล์ตกค้าง

## ลำดับที่วัดได้ (เฉพาะช่วงที่ commit สำเร็จ 1 ครั้ง)
```
f43218  BattleGridClicked(obj=6_0)          <- คลิกช่องที่ยูนิตยืนอยู่
f43218  OnClick(6,0)
f43219  GenerateMovePath(target=6,0)   state=7
f43231  GenerateMovePath(target=6,1)   state=7      \
f43238  GenerateMovePath(target=6,2)   state=7       |  preview ตามเมาส์ที่เลื่อนผ่านช่อง
  ...                                                |  (ทุก call: state หลัง call = 7)
f43286  GenerateMovePath(target=6,9)   state=7      /
f43357  BattleGridClicked(obj=6_9)                   <- คลิกปลายทาง = commit
f43357  GenerateMovePath(target=6,9)   state=7       <- เกมสร้าง path ซ้ำเองในเฟรมเดียวกัน
f43357  OnClick(6,9)
```
หลัง f43357 ยูนิตอยู่ที่ `(6,9)` และ `nowActiveState = 2 (WaitPlayerInput)`, `moved=false`

## ข้อสรุปที่วัดได้ (และข้อที่ต้องแก้ความเชื่อเดิม)
1. **`MoveButtonClicked` ไม่ถูกเรียกเลยทั้ง session** (0 event) และ `CancelMove` ก็ 0
   → UI จริงไม่ได้เข้าทางปุ่มย้าย แต่เข้าโหมดด้วยการคลิกช่องที่ยูนิตยืน (f43218)
2. **`GenerateMovePath` คือตัวที่ทำให้เกิด path และดัน state 6→7** — เรียกกี่ครั้งก็ได้ (29 ครั้งใน 3 นาที) ระหว่าง hover
3. **ลำดับ commit จริงคือ `BattleGridClicked(target)` แล้ว `GenerateMovePath(target)` แล้ว `OnClick`** ในเฟรมเดียว
   → รอบ 3 ที่ยิง `MoveButtonClicked` + `BattleGridClicked` อย่างเดียวจึงไม่เกิดอะไร: ไม่มี path ให้ commit
4. `BattleGridClicked` ถูกเรียกในเฟรมเดียวกับ `OnClick` เสมอ (8/8 ครั้ง) — ตรงกับ W1 เดิม
5. **`patch_report` เชื่อถือไม่ได้**: ตอน probes ติดอยู่ มันรายงาน `count=5` (ไม่มี owner ของเรา)
   ขณะที่ `compat_dump` รายงาน 50 patched methods และมี `longyincoop.jitprobe.w1/w2` ครบ 6 แถว
   → ใช้ `compat_dump` เป็นหลัก ห้ามสรุป "hook ไม่ติด" จาก `patch_report`
6. Hit counters (`jitprobe hits=6`, `jitprobeW2 hits=21` ณ 22:28) เป็นตัวเลขจริงและตรงกับจำนวน event ที่เขียนลงไฟล์

## ข้อผิดพลาดของผมในรอบนี้ (บันทึกไว้กันซ้ำ)
ตอน 22:26:0x ผมสรุปว่า "probe ไม่ยิงเลย" เพราะ `ls -l | tail -6` ตัดสองไฟล์ใหม่ออกจากผลลัพธ์ (เรียงตามตัวอักษร) และ `cat` เรียกก่อน event แรกเกิดขึ้นจริง
→ ควรเช็คด้วย `stat` (Birth time) หรือ `grep -c` ไม่ใช่ `tail`

## ผลต่อ `spike move` (สมมติฐานที่แก้แล้ว)
ลำดับที่ต้องยิง = `MoveButtonClicked()` → `GenerateMovePath(cell.gridData)` → `BattleGridClicked(cell.gameObject)`
ความต่างจากรอบ 3 อยู่ที่ call กลาง (preview) ซึ่งเป็นตัวสร้าง path จริง

## สถานะหลังวัด
- probe ทั้งสองถูกปิด (`jitprobe=false`, `jitprobeW2=false`) → ไม่มี hook ค้าง
- panic switch ปิด, ไม่มีคำสั่งเปลี่ยน state ถูกเรียกตลอดรอบนี้
- เซฟจริงไม่ได้ถูกแตะ (mod ไม่ได้เรียก Save)
