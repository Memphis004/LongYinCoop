# S1.5-d / รอบ 17 — คำสั่งเดินครั้งที่ 9 บน build `bbf8cd42` · 2026-10-21 00:4x [RUNTIME]

build `bbf8cd42be5d` · เกม pid 20232 · arm ครั้งเดียว, คำสั่งเดียว, ไม่ retry

## 1. ก่อนยิง (`logs/round17-pre.json`)
```
active=0:9 @ (5,0) · Fighting · WaitPlayerInput · playerControl=true
turnMoved=false · paused=false · playingAnim=false · turnIndex=13
```
เป้าที่เลือก: ช่องว่างติดกันช่องแรก (row+1) = **(6,0)**

## 2. ลำดับที่ยิง
```
set_pref allowRemoteInput=true       -> exit 0
spike arm 60                         -> exit 0
spike move id=0:9 row=6 col=0        -> exit 0
```

## 3. ผลคำสั่ง [RUNTIME]
```
StableId            = 0:9
ActiveIdBefore      = 0:9
StateBefore         = 2 WaitPlayerInput
MovedFlagBefore     = false
StateAfterSelfClick = 6 ShowMovingRage
StateAfterPreview   = 7 WaitMoving
StateAfterPath      = 7 WaitMoving
UnitRowAfter/ColAfter = (6,0)        PositionConfirmed = true
RollbackApplied     = false
CostMs              = 0
ElapsedMs           = 13
```

## 4. หลังเดินจบ (`logs/round17-post.json`)
```
0:9  @ (6,0)  · hp=221.326 (เท่าเดิม) · alive=true
0:86 @ (3,0)  · hp=765.306
0:0  @ (4,1)  · hp=505.551
0:4  @ (10,1) · hp=475.701
active=0:9 @ (6,0) · WaitPlayerInput · turnMoved=true · turnIndex=13 · frame=258,304
paused=false · playingAnim=false
stateHash=e3f8f14ece1a7b3349d523ccd8af9c485c5bdbd68434934701fd8ba92d87baeb
```

## 5. ข้อสรุป
1. ยูนิต 0:9 ถึงช่อง (6,0) ตรงตามที่สั่ง · `PositionConfirmed=true` · hp ไม่เปลี่ยน · ไม่ค้างโหมด · ไม่มี exception
2. **รอบนี้เทิร์นไม่สลับเอง**: active ยังเป็น 0:9 และ `turnMoved=true`
3. `CostMs=0` · `ElapsedMs=13` ms

## 6. ความปลอดภัย
- `allowRemoteInput=false` + `disarm` แล้ว (exit 0 ทั้งคู่)
- เซฟจริง: `real 0/0/0`, drift 0, sentinel 5/5, **RESULT: PASS**

## 7. เกณฑ์ S1.5-d
| ข้อ | ผล |
|---|---|
| ยูนิตถึงช่องที่สั่ง | **PASS** (ครั้งที่ 9 บน build นี้) |
| hp ไม่เปลี่ยน / ไม่ค้าง / ไม่ exception | PASS |
| exit 1 (10/10) | **9/10** (build `bbf8cd42`) |
