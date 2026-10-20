# S1.5-d / รอบ 12 — คำสั่งเดินครั้งที่ 4 บน build `bbf8cd42` · 2026-10-20 23:44:00 [RUNTIME]

build `bbf8cd42be5d` · เกม pid 20232 · arm ครั้งเดียว, คำสั่งเดียว, ไม่ retry

## 1. ก่อนยิง (`logs/round12-pre.json`)
```
active=0:9 @ (4,0) · Fighting · WaitPlayerInput · playerControl=true
turnMoved=false · paused=false · playingAnim=false · turnIndex=3
```
เป้าที่เลือก: ช่องว่างติดกันช่องแรก (row+1) = **(5,0)**

## 2. ลำดับที่ยิง
```
set_pref allowRemoteInput=true      -> exit 0
spike arm 60                        -> exit 0
spike move id=0:9 row=5 col=0       -> exit 0
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
UnitRowAfter/ColAfter = (5,0)        PositionConfirmed = true
RollbackApplied     = false
CostMs              = 0
ElapsedMs           = 14
```

## 4. หลังเดินจบ (`logs/round12-post.json`)
```
0:9  @ (5,0) · hp=221.326 (เท่าเดิม) · alive=true
0:4  @ (9,1) · hp=475.701   <-- ตำแหน่งเปลี่ยนจากรอบ 11 (8,0) โดยไม่ใช่คำสั่งของ mod
0:86 @ (2,0) · hp=765.306
0:0  @ (3,0) · hp=505.551
active=0:9 @ (5,0) · WaitPlayerInput · turnMoved=true · turnIndex=3 · frame=156,395
paused=false · playingAnim=false
stateHash=af11adcc3f74f9b5bd1e775411f3a5ad74331b5f197fb9adbe5728a0feef90e3
```

## 5. ข้อสรุป
1. ยูนิต 0:9 ถึงช่อง (5,0) ตรงตามที่สั่ง · `PositionConfirmed=true` · hp ไม่เปลี่ยน · ไม่ค้างโหมด · ไม่มี exception
2. **รอบนี้เทิร์นไม่สลับเอง** (เหมือนรอบ 11): active ยังเป็น 0:9 และ `turnMoved=true` → ต้องจบเทิร์นเองทุกครั้ง
3. สังเกต: `0:4` ย้ายจาก (8,0) → (9,1) ระหว่างรอบ — ไม่ได้มาจากคำสั่ง mod (mod ยิงเฉพาะ 0:9 รอบนี้); บันทึกไว้เพื่อไม่ให้ปนกับผลทดสอบ
4. `CostMs=0` · `ElapsedMs=14` ms

## 6. ความปลอดภัย
- `allowRemoteInput=false` + `disarm` แล้ว (exit 0 ทั้งคู่)
- เซฟจริง: `real 0/0/0`, drift 0, sentinel 5/5, **RESULT: PASS** (หลักฐาน: `logs/save-verify/round12.json`)

## 7. เกณฑ์ S1.5-d
| ข้อ | ผล |
|---|---|
| ยูนิตถึงช่องที่สั่ง | **PASS** (ครั้งที่ 4 บน build นี้) |
| hp ไม่เปลี่ยน / ไม่ค้าง / ไม่ exception | PASS |
| exit 1 (10/10) | **4/10** (build `bbf8cd42`) |
