# S1.5-d / รอบ 14 — คำสั่งเดินครั้งที่ 6 บน build `bbf8cd42` · 2026-10-21 00:1x [RUNTIME]

build `bbf8cd42be5d` · เกม pid 20232 · arm ครั้งเดียว, คำสั่งเดียว, ไม่ retry

## 1. ก่อนยิง (`logs/round14-pre.json`)
```
active=0:86 @ (2,0) · Fighting · WaitPlayerInput · playerControl=true
turnMoved=false · paused=false · playingAnim=false · turnIndex=10
```
เป้าที่เลือก: ช่องว่างติดกันช่องแรก (row+1) = **(3,0)**

## 2. ลำดับที่ยิง
```
set_pref allowRemoteInput=true       -> exit 0
spike arm 60                         -> exit 0
spike move id=0:86 row=3 col=0       -> exit 0
```

## 3. ผลคำสั่ง [RUNTIME]
```
StableId            = 0:86
ActiveIdBefore      = 0:86
StateBefore         = 2 WaitPlayerInput
MovedFlagBefore     = false
StateAfterSelfClick = 6 ShowMovingRage
StateAfterPreview   = 7 WaitMoving
StateAfterPath      = 7 WaitMoving
UnitRowAfter/ColAfter = (3,0)        PositionConfirmed = true
RollbackApplied     = false
CostMs              = 0
ElapsedMs           = 11
```

## 4. หลังเดินจบ (`logs/round14-post.json`)
```
0:86 @ (3,0) · hp=765.306 (เท่าเดิม) · alive=true
0:0  @ (4,0) · hp=505.551
0:9  @ (5,0) · hp=221.326
0:4  @ (9,1) · hp=475.701
active=0:86 @ (3,0) · WaitPlayerInput · turnMoved=true · turnIndex=10 · frame=200,792
paused=false · playingAnim=false
stateHash=76b6f7ce0b03bbfcb90ddb892a9cb41d34462c997e3bce57fcd3a30684056a95
```

## 5. ข้อสรุป
1. ยูนิต 0:86 ถึงช่อง (3,0) ตรงตามที่สั่ง · `PositionConfirmed=true` · hp ไม่เปลี่ยน · ไม่ค้างโหมด · ไม่มี exception
2. **รอบนี้เทิร์นไม่สลับเอง** (เหมือนรอบ 11, 12): active ยังเป็น 0:86 และ `turnMoved=true`
3. `CostMs=0` · `ElapsedMs=11` ms

## 6. ความปลอดภัย
- `allowRemoteInput=false` + `disarm` แล้ว (exit 0 ทั้งคู่)
- เซฟจริง: `real 0/0/0`, drift 0, sentinel 5/5, **RESULT: PASS**

## 7. เกณฑ์ S1.5-d
| ข้อ | ผล |
|---|---|
| ยูนิตถึงช่องที่สั่ง | **PASS** (ครั้งที่ 6 บน build นี้) |
| hp ไม่เปลี่ยน / ไม่ค้าง / ไม่ exception | PASS |
| exit 1 (10/10) | **6/10** (build `bbf8cd42`) |
