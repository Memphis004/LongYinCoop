# S1.5-d / รอบ 5 — ผลยิงจริง (FAIL) · 2026-10-20 22:47 [RUNTIME]

build `4b3a2d719797` · เซฟก่อนยิง PASS (real 0/0/0, drift 0, sentinel 5/5)

## คำสั่งที่ยิง (1 ครั้ง ไม่ retry)
```
arm(60s) -> spike move id=0:0 row=2 col=1
```

## ผล [RUNTIME]
```
PRE : state=WaitPlayerInput pos=(2,0) hp=505.551
ตอบกลับ: {"ok": false, "error": "spike move: no path generated (state ShowMovingRage); cancelMove applied"}
POST: state=WaitPlayerInput pos=(2,0) hp=505.551 moved=false
```

## สิ่งที่ยืนยันได้
1. `BattleGridClicked(ช่องที่ยูนิตยืน)` **พาเข้าโหมดเดินได้จริง** (state ขึ้น 6 = `ShowMovingRage`)
   → ทางเข้าไม่ใช่ปัญหาเหมือนรอบ 4 (ปุ่มย้ายก็ขึ้น 6 ได้)
2. `GenerateMovePath(cell.gridData)` ที่ mod เรียกเอง **ไม่สร้าง path** และไม่ดัน state 6→7
   ทั้งๆ ที่ตอนผู้เล่น hover จริง (W1/W2 f43219+) call นี้เปลี่ยน state เป็น 7 ทุกครั้ง
3. ข้อสรุป: handler ตอน hover ทำอะไรเพิ่มก่อนเรียก `GenerateMovePath` (เช่น set ช่องเป้าหมาย / ช่วงเดิน) — ชิ้นนี้ยังขาด

## สถานะหลังรอบนี้
- panic switch ปิด · arm ปลดแล้ว · `CancelMove()` คืน `WaitPlayerInput` · hp ไม่เปลี่ยน
- probe **W6** เปิดอยู่ (read-only, `missing=0`) เพื่อจับ stack ของ `GenerateMovePath` รอบถัดไป
  ไฟล์: `UserData/LongYinCoop/CoopSpike/jitprobe-w6-20261020-224819.ndjson` (ยัง 0 event)

## เกณฑ์ S1.5-d
| เกณฑ์ | ผล |
|---|---|
| ยูนิตถึงช่องที่สั่ง | **FAIL** (0/5) |
| ไม่ค้างโหมด / hp ไม่เปลี่ยน / ไม่ exception | **PASS** |
| exit 1 (10/10) | **0/10** |
