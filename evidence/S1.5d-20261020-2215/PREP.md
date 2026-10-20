# S1.5-d — PREP: `spike move` (2026-10-20 22:15)

## 1. สิ่งที่เพิ่ม (ได้รับอนุมัติ 22:12)

| ไฟล์ | สิ่งที่เพิ่ม |
|---|---|
| `src/LongYinCoop/S1.5/MoveCommand.cs` | ใหม่ — arm state + `Move(stableId,row,col)` |
| `src/LongYinCoop/S1.5/TurnGate.cs` | `ActiveStableId(bc)` (อัลกอริทึมเดียวกับ `Build`), `ReadActiveMovedFlag(bc)` |
| `src/LongYinCoop/IpcServer.cs` | `spike arm/disarm/move` + `set_pref allowRemoteInput` |
| `src/LongYinCoop/UIStrings.cs` | ข้อความ error ของ arm/move |
| `tools/coopctl.py` | `spike --sub arm|disarm|move --id --row --col --seconds` |

## 2. กติกาที่บังคับในโค้ด

1. **arm ทุกครั้ง**: `spike arm <seconds>` (1–300, default 120) — เก็บใน memory เท่านั้น, ปิดเกม = หาย
2. **1 คำสั่งต่อ 1 arm**: `ConsumeArm()` ถูกเรียกทันทีที่เริ่มประมวลผล → สำเร็จหรือล้มเหลวก็ต้อง arm ใหม่ (ไม่มี retry/loop)
3. **panic switch**: `coop.allowRemoteInput=false` → `arm` และ `move` ตอบ `DISARMED` ทันที; การปิด pref จะ `Disarm()` ให้ด้วย
4. **main thread**: ทุก call ผ่าน `MainThreadDispatcher.Invoke(..., 500)`
5. **เรียก method เดียว**: `BattleController.BattleGridClicked(GameObject)` — entry เดียวกับ UI จริง (W1: 39/39 คลิกเดินไปช่องที่คลิก)
6. **ไม่เขียน field ของเกม**, ไม่เรียก Save/Load, ไม่แตะ `nowActiveState`/`selectedGrid` เอง
7. **precondition ก่อนยิง** (typed ทั้งหมด): `battleState==Fighting` · `!battlePaused` · `nowActiveState==WaitPlayerInput` · active unit `playerControl==true` · `stableId` ตรงกับยูนิตที่ถึงตา · มีช่อง (row,col) จริงจาก `GridUnitController.gridData`

## 3. build

| ข้อ | ค่า |
|---|---|
| DLL ใหม่ | **`fd1df6a8409c`** (เดิมรอบแรก `bc5ead96754c` — แก้ `MoveResult` ให้เป็น property เพราะ System.Text.Json ไม่ serialize field: ถ้าไม่แก้ client จะได้ `{}`) |
| build | `dotnet build -c Release` → **0 warning / 0 error** (ทั้งสองครั้ง) |
| DLL ที่เกมกำลังรัน | `c45ac7b53ae2` (ยังไม่ deploy — ต้องปิดเกมก่อน) |

## 4. ขั้นตอนทดสอบ (10 รอบ, exit 1)

ต่อ 1 รอบ:
1. `spike status` → เก็บ `active`/`activeRow`/`activeColumn`/`units[].hp`/`stateHash`
2. `set_pref allowRemoteInput true` (ครั้งแรกครั้งเดียว) → `spike arm 120`
3. `spike move --id <active> --row R --col C` (ช่องว่างในระยะเดิน)
4. รอ ≤ 2 s → `spike status` อีกครั้ง → ตรวจ
   - ยูนิต `active` ไปอยู่ (`R`,`C`) จริง
   - `hp` ของทุกยูนิตไม่เปลี่ยน
   - ไม่มี exception ใน log
5. ถ้าล้มเหลว → **ไม่ retry**; บันทึกแล้วหยุดถ้าเป็น soft-lock/crash

**เกณฑ์ผ่าน (exit 1):** 10/10 รอบเดินสำเร็จ · `hp` ไม่เปลี่ยนทุกครั้ง · exception 0 · ไม่มี soft-lock
