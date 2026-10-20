# S1.5-d / รอบ 11 — คำสั่งเดินครั้งที่ 3 บน build `bbf8cd42` · 2026-10-20 23:39:43 [RUNTIME]

build `bbf8cd42be5d` · เกม pid 20232 · arm ครั้งเดียว, คำสั่งเดียว, ไม่ retry

## 1. ก่อนยิง (`logs/round11-pre.json`)
```
active=0:4 @ (7,0) · Fighting · WaitPlayerInput · playerControl=true
turnMoved=false · paused=false · playingAnim=false · turnIndex=2
```
เป้าที่เลือก: ช่องว่างติดกันช่องแรกตามลำดับ (row+1, col+1, row-1, col-1) = **(8,0)** (ไม่มีหน่วยยืนอยู่)

## 2. ลำดับที่ยิง
```
set_pref allowRemoteInput=true      -> exit 0
spike arm 60                        -> exit 0
spike move id=0:4 row=8 col=0       -> exit 0
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
UnitRowAfter/ColAfter = (8,0)        PositionConfirmed = true
RollbackApplied     = false
CostMs              = 0
ElapsedMs           = 10
```

## 4. หลังเดินจบ (`spike status` เป็นตัวนับ, `logs/round11-post.json`)
```
0:4  @ (8,0) · hp=475.701 (เท่าเดิม) · alive=true
0:86 @ (2,0) · hp=765.306
0:0  @ (3,0) · hp=505.551
active=0:4 @ (8,0) · WaitPlayerInput · turnMoved=true · turnIndex=2 · frame=121,759
paused=false · playingAnim=false
stateHash=e5a7ece152de6e7c2086a30f069c7b264e0bdda18cbc850369314fbdccf2ef37
```

## 5. ข้อสรุป
1. ยูนิต 0:4 ถึงช่อง (8,0) ตรงตามที่สั่ง · `PositionConfirmed=true` · hp ไม่เปลี่ยน · ไม่ค้างโหมด · ไม่มี exception
2. **รอบนี้เทิร์นไม่สลับเอง** — หลังเดิน active ยังเป็น 0:4 และ `turnMoved=true` (ต่างจากรอบ 10 ที่ 0:86 เดินแล้วเกมสลับไป 0:4)
   → ห้ามสรุปพฤติกรรม "จบเทิร์นอัตโนมัติ"; ต้องอ่าน `spike status` ทุกครั้งก่อนยิง
3. `CostMs=0` · `ElapsedMs=10` ms — ตัวอย่างเพิ่มของเกณฑ์ p99 ≤ 8 ms

## 6. ความปลอดภัย
- `allowRemoteInput=false` + `disarm` แล้ว (exit 0 ทั้งคู่)
- เซฟจริง: `real 0/0/0`, drift 0, sentinel 5/5, **RESULT: PASS** (หลักฐาน: `logs/save-verify/round11.json`)

## 7. เกณฑ์ S1.5-d
| ข้อ | ผล |
|---|---|
| ยูนิตถึงช่องที่สั่ง | **PASS** (ครั้งที่ 3 บน build นี้) |
| hp ไม่เปลี่ยน / ไม่ค้าง / ไม่ exception | PASS |
| exit 1 (10/10) | **3/10** (build `bbf8cd42`) |
