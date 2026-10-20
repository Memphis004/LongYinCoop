# S1.5-d / รอบ 15 — คำสั่งเดินครั้งที่ 7 บน build `bbf8cd42` · 2026-10-20 23:51:29 [RUNTIME]

build `bbf8cd42be5d` · เกม pid 20232 · arm ครั้งเดียว, คำสั่งเดียว, ไม่ retry

## 1. ก่อนยิง (`logs/round15-pre.json`)
```
active=0:4 @ (9,1) · Fighting · WaitPlayerInput · playerControl=true
turnMoved=false · paused=false · playingAnim=false · turnIndex=11
```
เป้าที่เลือก: ช่องว่างติดกันช่องแรก (row+1) = **(10,1)**

## 2. ลำดับที่ยิง
```
set_pref allowRemoteInput=true        -> exit 0
spike arm 60                          -> exit 0
spike move id=0:4 row=10 col=1        -> exit 0
```

## 3. ผลคำสั่ง [RUNTIME]
```
StableId            = 0:4
ActiveIdBefore      = 0:4
StateBefore         = 2 WaitPlayerInput
MovedFlagBefore     = false
StateAfterSelfClick = 6 ShowMovingRage
StateAfterPreview   = 7 WaitMoving
StateAfterPath      = 7 WaitMoving
UnitRowAfter/ColAfter = (10,1)       PositionConfirmed = true
RollbackApplied     = false
CostMs              = 1
ElapsedMs           = 14
```

## 4. หลังเดินจบ (`logs/round15-post.json`)
```
0:4  @ (10,1) · hp=475.701 (เท่าเดิม) · alive=true
0:86 @ (3,0)  · hp=765.306
0:0  @ (4,0)  · hp=505.551
0:9  @ (5,0)  · hp=221.326
active=0:4 @ (10,1) · WaitPlayerInput · turnMoved=true · turnIndex=11 · frame=216,199
paused=false · playingAnim=false
stateHash=ba1f1b782a8565e5a2f18ec7a8591cce1ec5515fc1c6a110efa3f9511ebc9fd7
```

## 5. ข้อสรุป
1. ยูนิต 0:4 ถึงช่อง (10,1) ตรงตามที่สั่ง · `PositionConfirmed=true` · hp ไม่เปลี่ยน · ไม่ค้างโหมด · ไม่มี exception
2. **รอบนี้เทิร์นไม่สลับเอง**: active ยังเป็น 0:4 และ `turnMoved=true`
3. `CostMs=1` · `ElapsedMs=14` ms

## 6. ความปลอดภัย
- `allowRemoteInput=false` + `disarm` แล้ว (exit 0 ทั้งคู่)
- เซฟจริง: `real 0/0/0`, drift 0, sentinel 5/5, **RESULT: PASS** (หลักฐาน: `logs/save-verify/round15.json`)

## 7. เกณฑ์ S1.5-d
| ข้อ | ผล |
|---|---|
| ยูนิตถึงช่องที่สั่ง | **PASS** (ครั้งที่ 7 บน build นี้) |
| hp ไม่เปลี่ยน / ไม่ค้าง / ไม่ exception | PASS |
| exit 1 (10/10) | **7/10** (build `bbf8cd42`) |
