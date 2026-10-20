# S1.5-d / รอบ 16 — คำสั่งเดินครั้งที่ 8 บน build `bbf8cd42` · 2026-10-21 00:3x [RUNTIME]

build `bbf8cd42be5d` · เกม pid 20232 · arm ครั้งเดียว, คำสั่งเดียว, ไม่ retry

## 1. ก่อนยิง (`logs/round16-pre.json`)
```
active=0:0 @ (4,0) · Fighting · WaitPlayerInput · playerControl=true
turnMoved=false · paused=false · playingAnim=false · turnIndex=12
```
หมายเหตุ: ผู้ใช้จบเทิร์นก่อนหน้าด้วย **hotkey สกิล 2** (สกิล 1 ติด cooldown)
เป้าที่เลือก: (row+1) = (5,0) มี 0:9 ยืนอยู่ → ใช้ (col+1) = **(4,1)**

## 2. ลำดับที่ยิง
```
set_pref allowRemoteInput=true       -> exit 0
spike arm 60                         -> exit 0
spike move id=0:0 row=4 col=1        -> exit 0
```

## 3. ผลคำสั่ง [RUNTIME]
```
StableId            = 0:0
ActiveIdBefore      = 0:0
StateBefore         = 2 WaitPlayerInput
MovedFlagBefore     = false
StateAfterSelfClick = 6 ShowMovingRage
StateAfterPreview   = 7 WaitMoving
StateAfterPath      = 7 WaitMoving
UnitRowAfter/ColAfter = (4,1)        PositionConfirmed = true
RollbackApplied     = false
CostMs              = 0
ElapsedMs           = 14
```

## 4. หลังเดินจบ (`logs/round16-post.json`)
```
0:0  @ (4,1)  · hp=505.551 (เท่าเดิม) · alive=true
0:86 @ (3,0)  · hp=765.306
0:9  @ (5,0)  · hp=221.326
0:4  @ (10,1) · hp=475.701
active=0:0 @ (4,1) · WaitPlayerInput · turnMoved=true · turnIndex=12 · frame=236,377
paused=false · playingAnim=false
stateHash=fe3125af2688663cc48510b67cb0ee80e3780b2761773fa7357935a5b27e8cc8
```

## 5. ข้อสรุป
1. ยูนิต 0:0 ถึงช่อง (4,1) ตรงตามที่สั่ง · `PositionConfirmed=true` · hp ไม่เปลี่ยน · ไม่ค้างโหมด · ไม่มี exception
2. **รอบนี้เทิร์นไม่สลับเอง**: active ยังเป็น 0:0 และ `turnMoved=true`
3. `CostMs=0` · `ElapsedMs=14` ms

## 6. ความปลอดภัย
- `allowRemoteInput=false` + `disarm` แล้ว (exit 0 ทั้งคู่)
- เซฟจริง: `real 0/0/0`, drift 0, sentinel 5/5, **RESULT: PASS**

## 7. เกณฑ์ S1.5-d
| ข้อ | ผล |
|---|---|
| ยูนิตถึงช่องที่สั่ง | **PASS** (ครั้งที่ 8 บน build นี้) |
| hp ไม่เปลี่ยน / ไม่ค้าง / ไม่ exception | PASS |
| exit 1 (10/10) | **8/10** (build `bbf8cd42`) |
