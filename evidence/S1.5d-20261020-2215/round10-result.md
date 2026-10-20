# S1.5-d / รอบ 10 — คำสั่งเดินครั้งที่ 2 บน build `bbf8cd42` · 2026-10-20 23:3x [RUNTIME]

build `bbf8cd42be5d` · เกม pid 20232 · arm ครั้งเดียว, คำสั่งเดียว, ไม่ retry

## 1. ก่อนยิง — ตรวจสองครั้ง

| เวลา/ไฟล์ | active | pos | nowActiveState | turnMoved | turnIndex | frame | ผล |
|---|---|---|---|---|---|---|---|
| `logs/round10-pre.json` | `0:0` | (3,0) | `WaitPlayerInput` | **true** | 0 | 82,298 | **ไม่ยิง** (prefire จะปฏิเสธ) |
| `logs/round10-pre2.json` | `0:86` | (1,0) | `WaitPlayerInput` | **false** | 1 | 99,672 | ผ่าน → ยิง |

> ผู้ใช้กด hotkey สกิล 1 เพื่อจบเทิร์นของ 0:0 → active เปลี่ยนเป็น 0:86 (`turnIndex` 0→1)

## 2. ลำดับที่ยิง
```
set_pref allowRemoteInput=true                 -> exit 0
spike arm 60                                   -> exit 0 (armed=true)
spike move id=0:86 row=2 col=0                 -> exit 0
```

## 3. ผลคำสั่ง [RUNTIME]
```
StableId            = 0:86
ActiveIdBefore      = 0:86
StateBefore         = 2 WaitPlayerInput
MovedFlagBefore     = false          (G3 ผ่าน)
StateAfterSelfClick = 6 ShowMovingRage
StateAfterPreview   = 7 WaitMoving
StateAfterPath      = 7 WaitMoving
UnitRowAfter/ColAfter = (2,0)        PositionConfirmed = true
RollbackApplied     = false
CostMs              = 0              (main thread, เฟส 1)
ElapsedMs           = 13             (arm→ยืนยันตำแหน่ง)
```

## 4. หลังเดินจบ (`spike status` เป็นตัวนับ, `logs/round10-post.json`)
```
0:86 @ (2,0) · hp=765.306 (เท่าเดิม) · alive=true
0:0  @ (3,0) · hp=505.551
active=0:4 @ (7,0) · WaitPlayerInput · turnMoved=false · turnIndex=2 · frame=103,967
paused=false · playingAnim=false
stateHash=f2725e09c5e2bd0e61d2148c09e7078c66bdc971f451883225696c2090c332ee
```

## 5. ข้อสรุป
1. ยูนิต 0:86 ถึงช่อง (2,0) ตรงตามที่สั่ง · `PositionConfirmed=true` · hp ไม่เปลี่ยน · ไม่ค้างโหมด · ไม่มี exception
2. **หลังคำสั่งเดินของ 0:86 เกมสลับเทิร์นเองทันที**: active เปลี่ยนเป็น 0:4 (`turnIndex` 1→2, `turnMoved=false`, `playerControl=true`)
   — **แก้ภายหลัง (รอบ 11): ไม่เกิดซ้ำ** หลังเดินของ 0:4 เอง active ยังเป็น 0:4 และ `turnMoved=true` → ห้ามสรุปว่าเกมจบเทิร์นให้ทุกครั้ง; ต้องอ่าน status ทุกครั้งก่อนยิง
3. `CostMs=0` ms (ต่ำกว่า 1 ms) · `ElapsedMs=13` ms → ตัวอย่างเพิ่มของเกณฑ์ p99 ≤ 8 ms

## 6. ความปลอดภัย
- `allowRemoteInput=false` (exit 0) + `disarm` (exit 0) หลังยิง
- เซฟจริง: `save-split.py verify` → `real 0/0/0`, drift 0, sentinel 5/5, **RESULT: PASS**

## 7. เกณฑ์ S1.5-d
| ข้อ | ผล |
|---|---|
| ยูนิตถึงช่องที่สั่ง | **PASS** (ครั้งที่ 2 บน build นี้) |
| hp ไม่เปลี่ยน / ไม่ค้าง / ไม่ exception | PASS |
| exit 1 (10/10) | **2/10** (build `bbf8cd42`) |
