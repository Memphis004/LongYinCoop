# S1.5-d / รอบ 4 — ผลยิงจริง (FAIL) · 2026-10-20 22:40 [RUNTIME]

build `3198df6df2e3` · เซฟก่อนยิง PASS (real 0/0/0, drift 0, sentinel 5/5) · label `S1.5c-originhp-20261020-172346`

## คำสั่งที่ยิง (2 ครั้ง — ครั้งที่ 2 ยิงเพื่ออ่าน error text เท่านั้น)
```
arm(60s) -> spike move id=0:0 row=15 col=0   [22:40:16]
arm(40s) -> spike move id=0:0 row=15 col=0   [22:40:5x]
```

## ผล [RUNTIME]
```
PRE : state=WaitPlayerInput pos=(14,0) moved=false hp=505.551
ตอบกลับ: {"ok": false, "cmd": "spike", "error": "spike move: no path generated (state ShowMovingRage); cancelMove applied"}
POST: state=WaitPlayerInput pos=(14,0) moved=false hp=505.551
```
- `MoveButtonClicked()` → state 6 (`ShowMovingRage`) สำเร็จ (ยืนยันจาก error ที่รายงาน state 6 หลัง preview)
- `GenerateMovePath(cell.gridData)` **ไม่เกิด path** และ state ยังเป็น 6
- mod ถอยออกด้วย `CancelMove()` → `WaitPlayerInput`
- ไม่มี exception · hp ไม่เปลี่ยน · ไม่ค้างโหมด · ปลด arm + ปิด panic switch แล้ว

## ข้อสรุปใหม่ (แก้สมมติฐานรอบ 4)
1. **`MoveButtonClicked` ไม่ใช่ทางเข้าโหมดเดินที่ใช้ได้จริง** — ขึ้น state 6 ได้ แต่ `GenerateMovePath` จาก state 6 ไม่สร้าง path
2. ทางเข้าจริงตามหลักฐาน f43218 คือ **คลิกช่องที่ยูนิตยืน** (`BattleGridClicked(self)`) ซึ่งเกมตอบด้วย `OnClick(self)` และหลังจากนั้น hover จึงสร้าง path ได้ (state 7)
3. ลำดับที่ควรลองต่อไป: `BattleGridClicked(self)` → `GenerateMovePath(target)` → `BattleGridClicked(target)`

## สถานะหลังรอบนี้
- panic switch `AllowRemoteInput=false` · arm ปลดแล้ว · เซฟจริงไม่ถูกแตะ
- DLL ในเกม = `3198df6df2e3` (รุ่นที่ล้มเหลว) — build ใหม่ `4b3a2d719797` รอ deploy เมื่อปิดเกม

## เกณฑ์ S1.5-d
| เกณฑ์ | ผล |
|---|---|
| ยูนิตถึงช่องที่สั่ง | **FAIL** (0/4) |
| ไม่ค้างโหมด / hp ไม่เปลี่ยน / ไม่ exception | **PASS** |
| exit 1 (10/10) | **0/10** |
