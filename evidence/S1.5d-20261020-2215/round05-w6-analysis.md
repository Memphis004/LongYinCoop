# S1.5-d / วิเคราะห์ W6 — ทำไม `GenerateMovePath` ของ mod ไม่ทำงาน (2026-10-20 22:49) [RUNTIME]

probe W6 (`jitprobe-w6-20261020-224819.ndjson`, sha256_16 `f5acecfc682cd548`, 19 events, 10 stacks)

## สิ่งที่ผู้เล่นทำ
คลิกช่องที่ยูนิตยืน 1 ครั้ง (เข้าโหมดเดิน) แล้วเลื่อนเมาส์ผ่านช่องข้าง ๆ 8 ช่อง (ไม่คลิก)

## ไทม์ไลน์ที่วัดได้
```
f28538  BattleGridClicked.pre   obj=2_0 state=2   <- ก่อนเรียก
f28538  BattleGridClicked.post  obj=2_0 state=6   <- หลังเรียก = ShowMovingRage
f28538  OnClick.post            obj=2_0 state=6
f28539  GenerateMovePath.pre    target=(2,0) state=7   <- state เป็น 7 อยู่แล้วก่อนเรียก
f28539  GenerateMovePath.post   target=(2,0) state=7
f28613  GenerateMovePath.pre    target=(2,1) state=7
... (อีก 6 ครั้ง 2,3 / 2,4 / 2,5 / 2,6 / 2,7 / 3,8 — state=7 ทุกครั้ง)
```

## Stack ที่จับได้
```
[BattleGridClicked.pre]
  DMD<Il2Cpp.BattleController::BattleGridClicked>(BattleController this, GameObject clickedGrid)
  Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_invoke(...)
  DMD<Il2Cpp.GridUnitController::OnClick>(GridUnitController this)      <-- UI เรียก OnClick ก่อน
```
**แก้ความเชื่อเดิม:** UI ไม่ได้เรียก `BattleGridClicked` แล้วตามด้วย `OnClick` — `OnClick` เป็นตัวเรียก `BattleGridClicked` (อยู่ในสแตกเดียวกัน)

```
[GenerateMovePath.pre]
  DMD<Il2Cpp.BattleController::GenerateMovePath>(BattleController this, GridUnitData targetGrid)
  Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_invoke(...)   <-- ผู้เรียกอยู่นอก managed (Update ของเกม)
```
ผู้เรียก `GenerateMovePath` เป็น native (ไม่เห็นชื่อ method) แต่หลักฐาน state ชี้ชัดว่าไม่ใช่ตัวสร้าง state 7

## ข้อสรุป (แก้สมมติฐานรอบ 5)
1. `GenerateMovePath` **ไม่ได้**ทำให้ state 6→7 — ตอน hover จริง state เป็น 7 อยู่แล้วก่อนเรียก (prefix ทุกครั้ง = 7)
2. state 6→7 เกิดระหว่าง f28538 → f28539 คือ **เกมเองเปลี่ยนในเฟรมถัดไป** (ShowMovingRage = ช่วงแสดงผล범위 แล้วเข้าสู่ WaitMoving)
3. รอบ 5 ล้มเพราะ mod เรียก `GenerateMovePath` **ในเฟรมเดียวกับที่เพิ่งเข้าสู่ state 6** → เกมยังไม่พร้อมรับ target
4. ทางแก้คือ **ทำคำสั่งข้ามเฟรม**: คลิกช่องตัวเอง → รอ state 7 (timeout 1500 ms) → preview → commit

## สิ่งที่แก้ในโค้ด (build `844fb52ad4d2`)
- `MoveCommand.StartMove()` ทำขั้นที่ 1 แล้วฝากงานไว้; `MoveCommand.Tick()` (called จาก `CoopMod.Tick`) รอ state 7 แล้วทำขั้นที่ 2-3 ในเฟรมเดียวกัน
- `MoveCommand.WaitDone()` ให้ thread ของ IPC รอผล (ไม่บล็อก main thread)
- `IpcServer` เปลี่ยนไปใช้ StartMove + WaitDone; เพิ่ม `UIStrings.SpikeMoveBusy` / `SpikeMoveTimeout`
