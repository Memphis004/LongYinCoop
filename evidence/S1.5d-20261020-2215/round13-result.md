# S1.5-d / รอบ 13 — คำสั่งเดินครั้งที่ 5 บน build `bbf8cd42` · 2026-10-21 00:0x [RUNTIME]

build `bbf8cd42be5d` · เกม pid 20232 · arm ครั้งเดียว, คำสั่งเดียว, ไม่ retry

## 1. ก่อนยิง (`logs/round13-pre.json`)
```
active=0:0 @ (3,0) · Fighting · WaitPlayerInput · playerControl=true
turnMoved=false · paused=false · playingAnim=false · turnIndex=7
```
เป้าที่เลือก: ช่องว่างติดกันช่องแรก (row+1) = **(4,0)**

## 2. ลำดับที่ยิง
```
set_pref allowRemoteInput=true      -> exit 0
spike arm 60                        -> exit 0
spike move id=0:0 row=4 col=0       -> exit 0
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
UnitRowAfter/ColAfter = (4,0)        PositionConfirmed = true
RollbackApplied     = false
CostMs              = 0
ElapsedMs           = 14
```

## 4. หลังเดินจบ (`logs/round13-post.json`)
```
0:0  @ (4,0) · hp=505.551 (เท่าเดิม) · alive=true
0:9  @ (5,0) · hp=221.326
0:4  @ (9,1) · hp=475.701
0:86 @ (2,0) · hp=765.306
active=0:86 @ (2,0) · WaitPlayerInput · turnMoved=false · turnIndex=10 · frame=182,758
paused=false · playingAnim=false
stateHash=665bde75fe5fa37869e35915d65e776bfa7fa2d4b18878bfd8cd017a26bf683c
```

## 5. ข้อสรุป
1. ยูนิต 0:0 ถึงช่อง (4,0) ตรงตามที่สั่ง · `PositionConfirmed=true` · hp ไม่เปลี่ยน · ไม่ค้างโหมด · ไม่มี exception
2. **รอบนี้เทิร์นสลับเอง**: หลังเดิน active เปลี่ยนเป็น `0:86` (`turnMoved=false`, `playerControl=true`) — พฤติกรรมเดียวกับรอบ 10
   สรุปที่วัดได้ตอนนี้: สลับเอง 2 ครั้ง (รอบ 10, 13), ไม่สลับ 2 ครั้ง (รอบ 11, 12) — **ยังไม่มีรูปแบบที่อธิบายได้**
3. `CostMs=0` · `ElapsedMs=14` ms

## 6. ความปลอดภัย
- `allowRemoteInput=false` + `disarm` แล้ว (exit 0 ทั้งคู่)
- เซฟจริง: `real 0/0/0`, drift 0, sentinel 5/5, **RESULT: PASS**

## 7. เกณฑ์ S1.5-d
| ข้อ | ผล |
|---|---|
| ยูนิตถึงช่องที่สั่ง | **PASS** (ครั้งที่ 5 บน build นี้) |
| hp ไม่เปลี่ยน / ไม่ค้าง / ไม่ exception | PASS |
| exit 1 (10/10) | **5/10** (build `bbf8cd42`) |
