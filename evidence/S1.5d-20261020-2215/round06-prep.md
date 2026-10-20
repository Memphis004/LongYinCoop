# S1.5-d / รอบ 6 — คำสั่งเดินข้ามเฟรม (2026-10-20 22:57) [RUNTIME]

## deploy
| รายการ | ค่า |
|---|---|
| build จาก source | `f5196b30e3cf` |
| DLL ใน `Mods\LongYinCoop.dll` | `f5196b30e3cf` |
| backup | `LongYinCoop.dll.prev-4b3a2d719797` |
| commit | `5cd89fe` (ข้ามเฟรม) + `35195be` (guard timeout) |

## เซฟก่อนยิง (label `S1.5c-originhp-20261020-172346`)
`real 0/0/0 · drift 0 · sentinel .hidden-SaveSlot8 = 5 ไฟล์ · RESULT: PASS`

## prefs
`AllowRemoteInput=false`, `JitProbe/W2/W4/W6=false` → ไม่มี hook ค้าง, panic ปิด

## ลำดับคำสั่งรอบนี้ (ต่างจากรอบ 5 ตรง "เวลา")
```
frame N     : BattleGridClicked(ช่องที่ยูนิตยืน)     -> state 6 (ShowMovingRage)
frame N+1.. : MoveCommand.Tick() รอ state 7 (WaitMoving) — timeout 1500 ms
frame N+k   : GenerateMovePath(target)  +  BattleGridClicked(target)   (เฟรมเดียวกัน)
```
เหตุผล: W6 (22:49) แสดงว่า state 6→7 เกิดระหว่างเฟรม f28538→f28539 และตอน hover จริง state เป็น 7 อยู่แล้วก่อนเรียก `GenerateMovePath`
รอบ 5 ล้มเพราะยิงทั้งสาม call ในสแต็กเดียวตั้งแต่ยัง state 6

## เงื่อนไขหยุด
- ไม่ถึง state 7 ภายใน 1500 ms → `CancelMove()` เฉพาะเมื่ออยู่ในโหมดเดิน + รายงาน FAIL
- preview ไม่เกิด / ยังเป็น 6 หลัง commit → `CancelMove()` + FAIL
- exception / hp เปลี่ยน → หยุดและรายงาน

## เกณฑ์ผ่าน
ยูนิตถึงช่องเป้าหมาย · hp ไม่เปลี่ยน · ไม่ค้างโหมด · ไม่มี exception
ต้องทำซ้ำ 10/10 จึงนับ exit 1 (รอบนี้เริ่ม 1 ครั้ง)
