# S1.5-d / รอบ 8 — คำสั่งเดินแรกบน build `bbf8cd42` · 2026-10-20 23:25:47 [RUNTIME]

build `bbf8cd42be5d` (guard `ALREADY_USED` + ยืนยันตำแหน่งหลังเดิน + prefire check ฝั่ง client)

## ก่อนยิง (PRE)
```
active=0:0 @ (2,0) · Fighting · WaitPlayerInput · playerControl=true
turnMoved=false · turnAttacked=false · paused=false · playingAnim=false · frame=6881
```
ศึกใหม่ (`battle marker battle:start type=HardFight state=Prepare` 23:25:05) — ยูนิต 0:0 ถึงตาและยังไม่เดิน

## ลำดับที่ยิง (arm ครั้งเดียว, คำสั่งเดียว, ไม่ retry)
```
set_pref allowRemoteInput=true
spike arm 60                        -> armed, remainingMs=59999
spike move id=0:0 row=3 col=0       -> ok
```

## ผลคำสั่ง [RUNTIME]
```
MovedFlagBefore     = false           (G3 ผ่าน — ไม่ใช่ ALREADY_USED)
StateAfterSelfClick = 6 ShowMovingRage
StateAfterPreview   = 7 WaitMoving
StateAfterPath      = 7 WaitMoving
UnitRowAfter/ColAfter = (3,0)        PositionConfirmed = true
RollbackApplied     = false
CostMs              = 2              (main thread, เฟส 1)
ElapsedMs           = 11             (arm→ยืนยันตำแหน่ง รวมแอนิเมชัน/เฟรม)
```

## หลังเดินจบ (POST, `spike status` เป็นตัวนับ)
```
active=0:0 @ (3,0) · WaitPlayerInput · turnMoved=true · turnAttacked=false
paused=false · playingAnim=false · frame=10717
0:0 hp=505.551 (เท่าเดิม) · alive=true
stateHash=dbcd84df2f9d86c6e2e2ab887be90566e1ec9bef212a637d0a75fe5e7e093c1d
```

## ข้อสรุป
1. `spike move` ทำงานสำเร็จบน build ใหม่ — ยูนิตถึงช่อง (3,0) ตรงตามที่สั่ง
2. `PositionConfirmed=true` และ `UnitRowAfter/ColAfter` ตรงเป้าแล้ว (ต่างจากรอบ 6 ที่รายงาน (4,0) ค่าเก่า)
3. `ElapsedMs=11` ms → `mapGrid` อัปเดตภายใน ~1 เฟรมหลัง commit; การรอ `PositionTimeoutMs=4000` ไม่ถูกใช้จริงในรอบนี้ (ยังไม่รู้ว่าอัปเดตตอน "เริ่ม" หรือ "จบ" แอนิเมชัน เพราะนาฬิกาคำสั่งสั้นกว่าแอนิเมชันที่เห็นบนจอ)
4. `CostMs=2` ms — main-thread cost ต่ำกว่าเกณฑ์ p99 ≤ 8 ms (ตัวอย่างที่ 1 ของ build นี้)
5. prefire check ฝั่ง client ทำงานจริง (อ่าน status ก่อนยิง) แต่ **กิ่งปฏิเสธยังไม่ถูกทดสอบสด** เพราะรอบนี้ `turnMoved=false`
6. panic switch ปิด (`allowRemoteInput=false`) + disarm แล้ว · hp ไม่เปลี่ยน · ไม่มี exception · ไม่ค้างโหมด — **รอบนี้ไม่ได้บันทึก JSON ของ `set_pref`/`disarm` แยก** (ใน `logs/` มีแค่ `round08-arm.json`) หลักฐานของลำดับ enable → arm → move → disarm → disable จึงอยู่ที่ mod runtime log `logs/modlog-rounds08-18.txt` (23:25:47 → 23:26:04) และเซฟจริง: `logs/save-verify/round08.json` (`real.unchanged=true`, `ok=true`)

## เกณฑ์ S1.5-d
| ข้อ | ผล |
|---|---|
| ยูนิตถึงช่องที่สั่ง | **PASS 1/1 (build นี้)** — รวมกับรอบ 6 = 2 ครั้ง แต่คนละ build |
| hp ไม่เปลี่ยน / ไม่ค้าง / ไม่ exception | PASS |
| exit 1 (10/10) | **ยังไม่ผ่าน** — นับตาม build `bbf8cd42` ได้ 1/10 |
