# S1.5-d / รอบ 6 — ผลยิงจริง: **ยูนิตเดินสำเร็จ 1/1** · 2026-10-20 23:03 [RUNTIME]

build `f5196b30e3cf` (คำสั่งข้ามเฟรม) · เซฟก่อนยิง PASS (real 0/0/0, drift 0, sentinel 5/5)

## คำสั่ง
```
arm(60s) -> spike move id=0:0 row=5 col=0        (ใช้เวลา 16 ms ทั้งคำสั่ง)
```

## ผล [RUNTIME]
```
PRE : state=WaitPlayerInput pos=(4,0) hp=505.551
ตอบกลับ ok=true:
  StateAfterSelfClick = 6  ShowMovingRage
  StateAfterPreview   = 7  WaitMoving
  StateAfterPath      = 7  WaitMoving
  Call                = BattleGridClicked(self) + GenerateMovePath + BattleGridClicked(target)
  CostMs              = 6 · RollbackApplied = false
POST: state=WaitPlayerInput pos=(5,0) hp=505.551 moved=false
```

**ยูนิตไปถึงช่อง (5,0) จริง** — จาก (4,0) → (5,0) · hp ไม่เปลี่ยน · ไม่ค้างโหมด · ไม่มี exception

## ข้อสังเกตที่ต้องบันทึกตามจริง
- `UnitRowAfter/UnitColAfter` ใน response อ่านทันทีหลังคลิก จึงยังเป็น **(4,0)** (ค่าเก่า) — ค่าที่เชื่อถือได้คือ `spike status` หลังคำสั่ง = (5,0)
  → ควรแก้ในรอบถัดไปให้อ่านตำแหน่งหลังการเดินจบ (หรือรายงานเป็น "posAtClick" แทน)
- `elapsed` ฝั่ง client 16 ms = เวลารวม arm→move (ข้ามเฟรมแค่ 1-2 เฟรมตามที่ W6 วัด)
- **แก้แล้วใน build `bbf8cd42be5d` (2026-10-20 23:2x):** `UnitRowAfter/UnitColAfter` รอจนตำแหน่งตรงเป้า + `PositionConfirmed` · รายละเอียด `round07-blocked.md` §2.2 (ยัง NOT-RUN เพราะยังไม่เปิดเกม)

## เทียบเกณฑ์ S1.5-d
| เกณฑ์ | ผล |
|---|---|
| ยูนิตถึงช่องที่สั่ง | **PASS 1/1** (เหลือ 9 ครั้งเพื่อ 10/10) |
| hp ไม่เปลี่ยน | **PASS** — 505.551 เท่าเดิม |
| ไม่ค้างโหมด | **PASS** — กลับ `WaitPlayerInput` |
| ไม่มี exception | **PASS** |
| panic ปิด / arm ปลด | **PASS** |
| exit 1 (10/10) | **1/10 — ยังไม่ผ่าน** |
