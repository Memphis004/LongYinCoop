# S1.5-d / รอบ 7 — ไม่ได้ยิง (ALREADY_USED) + build ใหม่ · 2026-10-20 23:17

## 1. ผลวัดก่อนยิง [RUNTIME]

`spike status` (อ่านอย่างเดียว, ไม่มี arm) 2 ครั้ง:

| เวลา/ไฟล์ | active | pos | nowActiveState | turnMoved | turnAttacked | turnIndex | frame |
|---|---|---|---|---|---|---|---|
| `logs/round07-status.json` | `0:0` | (5,0) | `WaitPlayerInput` | **true** | false | 0 | 110,815 |
| ก่อนหน้า (ในบทสนทนา) | `0:0` | (5,0) | `WaitPlayerInput` | **true** | false | 0 | 106,110 |

- `turnMoved=true` = ยูนิต 0:0 ใช้สิทธิ์เดินของเทิร์นนี้ไปแล้ว (รอบ 6 ที่ย้าย (4,0)→(5,0))
- `turnIndex` ยังเป็น 0 และ `frame` เดินหน้าแต่แฟล็กไม่รีเซ็ต → เทิร์นยังไม่เปลี่ยน
- ตามแผน §3 G3 เงื่อนไขนี้ต้องตอบ `ALREADY_USED` → **ไม่ยิง** (ไม่ consume arm, ไม่แตะ GameObject ใด ๆ)
- ไม่มี retry, ไม่ได้ arm เลยในรอบนี้

## 2. โค้ดที่เพิ่ม/แก้ (build + deploy แล้ว)

### 2.1 G3 guard — `ALREADY_USED`
- `src/LongYinCoop/S1.5/MoveCommand.cs`: หลังเช็ค `WaitPlayerInput` และก่อนแตะ GameObject ใด ๆ
  ถ้า `r.MovedFlagBefore` (= `TurnGate.ReadActiveMovedFlag(bc)` อ่าน `nowActiveUnitMoved` แบบ typed) เป็น true → คืน `UIStrings.SpikeMoveAlreadyUsed`
- `UIStrings.cs`: `SpikeMoveAlreadyUsed = "ALREADY_USED: this unit has already moved this turn"`
- **ทำไม:** รอบ 6 จบในเทิร์นที่ `turnMoved=true` และ build เดิมไม่มี guard นี้ — ถ้าคนยิงซ้ำจะเป็นการเดินสองครั้งในเทิร์นเดียว (ผิด G3) · ใช้ turn-level flag เท่านั้น ไม่ใช้ `BattleUnit.moved` (วัดแล้ว true 0/82,695 sample)

### 2.2 อ่านตำแหน่งหลังเดินจบ — `UnitRowAfter/UnitColAfter`
- `MoveCommand.cs`: เพิ่มเฟสที่ 4 ใน `Tick()` — หลัง commit (`StateAfterPath != 0`) อ่าน `job.Unit.mapGrid` **ซ้ำทุกเฟรม** จน `row/col == เป้าหมาย` แล้วจึงเติม `UnitRowAfter/UnitColAfter` + `PositionConfirmed=true` และ `Finish(ok)`
- เก็บ `BattleUnit Unit` ไว้ใน `Job` (typed reference) เพื่ออ่านยูนิตตัวเดิม ไม่ใช่ `nowActiveUnit` ที่อาจเปลี่ยนเมื่อจบเทิร์น
- timeout ใหม่ `PositionTimeoutMs = 4000` ms; ถ้าไม่ถึงเป้า → `ok=false` + `SpikeMovePositionUnconfirmed` (ไม่ rollback เพราะ commit ไปแล้ว)
- `IpcServer.cs`: เปลี่ยน `WaitDone(PhaseTimeoutMs + 800)` → `WaitDone(MoveCommand.WaitTimeoutMs)` (= 1500+4000+800)
- **ทำไม:** รอบ 6 รายงาน `UnitRowAfter=(4,0)` ทั้งที่ยูนิตไป (5,0) เพราะอ่านทันทีหลังคลิก — ค่าที่เชื่อถือได้จึงเป็น `spike status` เท่านั้น

### 2.3 pre-fire check ฝั่ง client — `tools/coopctl.py`
- ก่อนส่ง `spike move`: เรียก `spike status` (read-only) แล้วถ้า `turnMoved=true` → พิมพ์ `prefire: REFUSED: turnMoved=true (unit … at (r,c) already moved this turn; G3 ALREADY_USED)` ทาง stderr แล้ว **exit 5 โดยไม่ส่งคำสั่ง move** (arm ที่ค้างอยู่ไม่ถูกใช้)
- ปฏิเสธเช่นกันเมื่อ `spike status` ใช้ไม่ได้ หรือผลไม่มีฟิลด์ `turnMoved` (build เก่า)
- ตรวจด้วย stub: true → REFUSED · false → ผ่าน · ไม่มีฟิลด์ → REFUSED · status ไม่ ok → REFUSED · `py_compile` ผ่าน
- **ยังไม่ทดสอบสด:** เกมปิดอยู่ (ทดสอบได้ตอนเปิดเกมครั้งถัดไป)

## 3. Build / deploy

| รายการ | ค่า |
|---|---|
| build | `dotnet build -c Release` → Build succeeded, 0 warning, 0 error |
| SHA-256 DLL สุดท้าย | `bbf8cd42be5d3d73c7e6e426d1e8163bbdf8001e9a4df0135837a3209636fa44` |
| DLL ที่ deploy | `Mods\LongYinCoop.dll` = `bbf8cd42be5d…` (ตรวจ sha256 แล้ว) |
| backup รุ่นรอบ 6 | `Mods\LongYinCoop.dll.prev-f5196b30e3cf` = `f5196b30e3cf…` |
| วิธีปิดเกม | `taskkill /PID 26708` (ไม่ force) → ออกภายใน ~1 s |
### 3.1 แยกเวลาออกจากกัน (สำคัญกับเกณฑ์ 7)
- `Finish()` เดิมเขียนทับ `CostMs` ด้วยเวลารวม → พอเพิ่มการรอตำแหน่ง เวลารวมจะดูเหมือน main-thread cost
- ตอนนี้ `CostMs` = เวลาที่บล็อก main thread เฉพาะเฟส 1 (วัดใน `StartMove` แล้ว) · `ElapsedMs` = เวลารวมถึงยืนยันตำแหน่ง (รวมแอนิเมชัน) · log พิมพ์ทั้งคู่ (`cost=…ms total=…ms`)

## 4. เซฟหลังปิดเกม [RUNTIME]

`save-split.py verify --label S1.5c-originhp-20261020-172346` → `real 0/0/0`, `mtime/size drift 0`, sentinel 5/5, **RESULT: PASS**

## 5. สถานะเกณฑ์ S1.5-d (ตามจริง)

| ข้อ | สถานะ |
|---|---|
| ยูนิตถึงช่องที่สั่ง | **1/10** — นับเฉพาะ build `f5196b30e3cf` (รอบ 6) |
| guard `ALREADY_USED` | **NOT-RUN** — เพิ่ง build/deploy ยังไม่เปิดเกมทดสอบ |
| `PositionConfirmed` | **NOT-RUN** — ยังไม่เปิดเกมทดสอบ |
| exit 1 (10/10) | **ไม่ผ่าน (1/10)** |

> ข้อจำกัดที่ต้องตัดสินก่อนนับต่อ: รอบ 7 เป็นต้นไปจะอยู่บน build `17131b6731bd` ส่วน 1/10 เดิมอยู่บน `f5196b30e3cf` — ถ้าจะให้นับรวมกันต้องยอมรับว่า chain การเรียกเหมือนกันทุกบรรทัด (ต่างเฉพาะ guard + การอ่านตำแหน่ง) มิฉะนั้นต้องเริ่มนับ 10 ครั้งใหม่บน build ใหม่
