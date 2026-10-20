# S1.5-d / รอบ 5 — เตรียมยิงด้วยทางเข้าที่ผู้เล่นใช้จริง (2026-10-20 22:44) [RUNTIME]

## สิ่งที่ deploy
| รายการ | ค่า |
|---|---|
| build จาก source | `4b3a2d719797` (source 22:41:59 → DLL 22:42:12) |
| DLL ใน `Mods\LongYinCoop.dll` | `4b3a2d719797` (22:44:39) |
| backup รุ่นก่อน | `LongYinCoop.dll.prev-3198df6df2e3` |
| commit | `4cb3574` |

## เซฟก่อนยิง (label `S1.5c-originhp-20261020-172346`)
```
real folder Save.real-S1.5c-originhp-20261020-172346: changed=0 missing=0 added=0 · drift 0
sentinel .hidden-SaveSlot8 -> counts as SaveSlot8/ (5 files)
RESULT: PASS
```

## ลำดับคำสั่งรอบนี้ (เปลี่ยนขั้นแรกจากรอบ 4)
1. `BattleGridClicked(ch่องที่ยูนิตยืน)` → คาด state 6
2. `GenerateMovePath(ช่องเป้าหมาย.gridData)` → คาด state 7
3. `BattleGridClicked(ช่องเป้าหมาย)` → คาดยูนิตเดิน

เหตุผล: รอบ 4 พิสูจน์ว่า `MoveButtonClicked()` ขึ้น state 6 ได้ แต่ `GenerateMovePath` จาก state 6 ไม่สร้าง path
และ W1/W2 22:26 (f43218) แสดงว่าผู้เล่นเข้าทางคลิกช่องตัวเองก่อน

## เงื่อนไขหยุด (ไม่ retry)
- ขั้น 1 ไม่เปลี่ยน state (ยัง `WaitPlayerInput`) → รายงาน FAIL ทันที ไม่มี rollback เพราะไม่มีอะไรค้าง
- ขั้น 2 ยังเป็น 2/6 → `CancelMove()` + รายงาน FAIL
- ขั้น 3 ยังเป็น 6 → `CancelMove()` + รายงาน FAIL
- exception / hp เปลี่ยน / state ค้าง → หยุดและรายงาน

## เกณฑ์ผ่าน
ยูนิตถึงช่องเป้าหมาย (`UnitRowAfter/UnitColAfter`), ไม่มี exception, hp ไม่เปลี่ยน, ไม่ค้างโหมด
ทำซ้ำ 10/10 จึงจะนับ exit 1 (รอบนี้เริ่ม 1 ครั้ง)
