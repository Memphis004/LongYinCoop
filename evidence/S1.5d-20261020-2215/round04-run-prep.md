# S1.5-d / รอบ 4 — เตรียมยิงคำสั่งเดินด้วยลำดับที่วัดได้ (2026-10-20 22:36) [RUNTIME]

## สิ่งที่ deploy
| รายการ | ค่า |
|---|---|
| DLL ที่ build จาก source | `3198df6df2e3` (source 22:30:55 → DLL 22:31:05) |
| DLL ที่ deploy ลง `Mods\LongYinCoop.dll` | `3198df6df2e3` (22:36:38) |
| backup ของเดิม | `LongYinCoop.dll.prev-b278955e33c8` |
| commit | `f1af443` (โค้ด) + `fb4b1c0` (หลักฐาน) |

## เซฟก่อนยิง (label `S1.5c-originhp-20261020-172346`)
```
mounted Save vs real baseline: changed=1 (PlayerprefData.dat) missing=4 added=5 (sentinel .hidden-SaveSlot8)
mounted Save vs copy baseline: changed=1 missing=0 added=0
real folder Save.real-S1.5c-originhp-20261020-172346: changed=0 missing=0 added=0 · drift 0
RESULT: PASS
```
→ ของจริงไม่ถูกแตะ (0/0/0, drift 0) · `SaveSlot8` ยังถูกซ่อนเป็น sentinel ครบ 5 ไฟล์

## ลำดับที่จะยิง (ต่างจากรอบ 3 ตรง call กลาง)
1. `BattleController.MoveButtonClicked()` → คาด state 6 (`ShowMovingRage`)
2. `BattleController.GenerateMovePath(cell.gridData)` → คาด state 7 (path เกิดจริง)
3. `BattleController.BattleGridClicked(cell.gameObject)` → คาดยูนิตเดิน

หลักฐานที่มาของลำดับ: `round04-analysis.md` (f43219–f43286 `GenerateMovePath` state=7; f43357 click commit)

## เงื่อนไขหยุด (ไม่ retry)
- ถ้า state หลังขั้น 2 ยังเป็น 2 หรือ 6 → ถือว่า preview ไม่เกิด → `CancelMove()` แล้วรายงาน FAIL
- ถ้า state หลังขั้น 3 ยังเป็น 6 → `CancelMove()` แล้วรายงาน FAIL
- ถ้ามี exception / hp เปลี่ยน / state ค้าง → หยุดทันทีและรายงาน
- arm ใช้ได้ครั้งเดียว (คำสั่งล้มเหลวก็หลุด)

## เกณฑ์ผ่านของรอบนี้ (ไม่เลื่อน)
- ยูนิตถึงช่องเป้าหมายจริง (`UnitRowAfter/UnitColAfter` = เป้าหมาย)
- ไม่มี exception, hp ของยูนิตไม่เปลี่ยน, ไม่ค้างโหมด
- ทำซ้ำได้ 10/10 จึงจะนับ exit 1 (รอบนี้เริ่มจาก 1 ครั้ง)
