# S1.5g · Phase A — Pause/Resume (ปุ่ม "หยุด" ของเกม) · ผลวัด

label `S1.5g-20261021-160331` · build mod `517758147f59170e584075a4b42f61e00d293803febe36f8a040bcdad4592612` (202,752 B) · เกม Unity 2020.3.48f1c1 (anchor ตรง `VERSION.md`)
วันที่วัด: 2026-10-21 · เซฟที่ mount = **สำเนา** (ของจริงถูก rename เก็บ) · ไม่มีการแก้ `src/` และไม่มีคำสั่ง mod ที่เปลี่ยน state ใน stage นี้

ป้าย: **[RUNTIME]** วัดจาก stream/hook · **[USER-OBSERVED]** ผู้ใช้รายงาน · **[SPEC]** กติกาของเรา

---

## 1. สิ่งที่วัด (เครื่องมือ + นิยาม) [SPEC]

| รายการ | ที่มา |
|---|---|
| `paused` | ฟิลด์ `paused` ใน `spike status` = `BattleController.battlePaused` (`S1Probe.cs:186`, `BattleSampler.cs:164`) — sample ทุก 100 ms ด้วย `tools/s15d_sample.py` |
| การกดปุ่มจริง | `coop.jitprobeW4` = Harmony **postfix อ่านอย่างเดียว** บน `BattleController.BattlePauseButtonClicked` → 1 บรรทัดต่อการกด 1 ครั้ง (`paused`, `nowActiveState`, `battleTime`, `timeScale`, `frame`) |
| รอบที่ "นับ" | window ที่ `paused=true` ต่อเนื่อง ≥ 0.3 s **และ** มี event W4 ของการกดปุ่มตรงกับเวลาเริ่ม window |
| รอบที่ "กดไม่ลงทะเบียน" | ไม่มี window `paused=true` หลังการกด → **ไม่นับ** |
| "clock หยุด" | `battleTime` delta = 0.000000 ตลอด window |
| "resume ได้" | ภายใน 2 s หลัง `paused=false` ต้องมี `battleTime` เดินหน้า **หรือ** `nowActiveState` เปลี่ยน **หรือ** `turnIndex` เปลี่ยน |

หมายเหตุ: build นี้ **ไม่มีคำสั่ง `spike pause`** (IpcServer ลงทะเบียนเฉพาะ `status/arm/disarm/move/attack`) ⇒ pause ที่ทดสอบ = ปุ่ม **"หยุด"** ของเกมเอง ผ่าน UI ปกติ ไม่ใช่โค้ด mod

---

## 2. ตาราง 16 รอบ [RUNTIME]

คลิกทั้งหมด 32 ครั้ง (16 pause + 16 resume) · `consecutive_flips = 31/31` · **คลิกตอน `WaitPlayerInput` (state 2) = 0/32** · `timeScale` = 1 ทั้ง 32 ครั้ง (pause ไม่ได้ทำผ่าน `Time.timeScale`)

| # | pause (W4 click) | resume (W4 click) | window | dur | state ตอนกด | turn | battleTime ก่อน→ระหว่าง | clock หยุด | หลัง resume +2 s |
|---|---|---|---|---|---|---|---|---|---|
| R01 | 18:24:58.441 | 18:25:36.806 | 18:24:58.522→18:25:36.849 | 38.23 s | Rest (3) | 4 | 1.044 → 1.044 | ✅ | bt +0.0625 · None→WaitAIPlan · turn 4→5 |
| R02 | 18:25:37.646 | 18:25:40.847 | 18:25:37.654→18:25:40.870 | 3.12 s | Moving (8) | 5 | 1.141 → 1.141 | ✅ | bt +0.0417 · Rest→None · turn 5→5 |
| R03 | 18:25:41.916 | 18:25:45.013 | 18:25:41.977→18:25:45.094 | 3.02 s | None (0) | 5 | 1.183 → 1.183 | ✅ | bt +0.0000 · WaitMoving→WaitAIPlan · turn 6→6 |
| R04 | 18:25:45.563 | 18:25:47.743 | 18:25:45.597→18:25:47.809 | 2.11 s | Moving (8) | 6 | 1.183 → 1.183 | ✅ | bt +0.0139 · Rest→WaitMoving · turn 6→7 |
| R05 | 18:25:48.979 | 18:25:51.687 | 18:25:49.017→18:25:51.733 | 2.62 s | WaitMoving (7) | 7 | 1.197 → 1.197 | ✅ | bt +0.0000 · WaitMoving→Rest · turn 7→7 |
| R06 | 18:25:52.749 | 18:25:54.812 | 18:25:52.841→18:25:54.851 | 1.91 s | Rest (3) | 7 | 1.197 → 1.197 | ✅ | bt +0.0000 · WaitPlayerInput→WaitPlayerInput · turn 7→8 |
| R07 | 18:26:03.131 | 18:26:05.193 | 18:26:03.198→18:26:05.208 | 1.91 s | WaitAttacking (10) | 9 | 1.259 → 1.259 | ✅ | bt +0.0000 · WaitAttacking→Attacking · turn 9→9 |
| R08 | 18:26:06.295 | 18:26:08.971 | 18:26:06.316→18:26:09.031 | 2.61 s | Attacking (11) | 9 | 1.259 → 1.259 | ✅ | bt +0.0555 · None→WaitMoving · turn 9→10 |
| R09 | 18:26:25.857 | 18:26:27.419 | 18:26:25.930→18:26:27.439 | 1.41 s | WaitMoving (7) | 15 | 2.079 → 2.079 | ✅ | bt +0.0000 · WaitMoving→WaitAttacking · turn 15→15 |
| R10 | 18:26:27.948 | 18:26:29.303 | 18:26:28.043→18:26:29.352 | 1.21 s | Moving (8) | 15 | 2.079 → 2.079 | ✅ | bt +0.0000 · WaitAttacking→Attacking · turn 15→15 |
| R11 | 18:26:30.164 | 18:26:32.296 | 18:26:30.156→18:26:32.370 | 2.11 s | Attacking (11) | 15 | 2.079 → 2.079 | ✅ | bt +0.2014 · Attacking→WaitMoving · turn 15→16 |
| R12 | 18:26:33.311 | 18:26:34.456 | 18:26:33.377→18:26:34.483 | 1.01 s | WaitMoving (7) | 16 | 2.280 → 2.280 | ✅ | bt +0.0000 · WaitMoving→WaitAttacking · turn 16→16 |
| R13 | 18:26:35.844 | 18:26:36.955 | 18:26:35.893→18:26:36.999 | 1.01 s | WaitAttacking (10) | 16 | 2.280 → 2.280 | ✅ | bt +0.0000 · Attacking→Attacking · turn 16→16 |
| R14 | 18:26:37.687 | 18:28:19.624 | 18:26:37.704→18:28:19.705 | **101.90 s** | Attacking (11) | 16 | 2.280 → 2.280 | ✅ | bt +0.0208 · None→WaitAttacking · turn 16→17 |
| R15 | 18:28:20.937 | 18:28:24.166 | 18:28:21.012→18:28:24.233 | 3.12 s | WaitAttacking (10) | 17 | 2.356 → 2.356 | ✅ | bt +0.0000 · Attacking→Attacking · turn 17→17 |
| R16 | 18:30:50.221 | 18:30:55.644 | 18:30:50.217→18:30:55.650 | 5.33 s | Attacking (11) | 21 | 2.509 → 2.509 | ✅ | bt +0.2153 · Finishing→WaitPlayerInput · turn 21→22 |

- **R01–R15 = Phase A (15 รอบ)** · **R16 = การ pause ที่เกิดใน Phase B** (นับเป็นรอบที่ลงทะเบียน แต่ช่วงเวลา 5.33 s ถูกหักออกจากนาฬิกา Phase B)
- **รอบที่ถูกตัดออก (excluded) = 0** — ทุก window มี event W4 ตรงเวลาแบบ 1:1 ทั้ง 16 รอบ
- R14 ยาว 101.9 s เพราะผู้ใช้กด pause แล้วเว้นช่วง (resume 18:28:19) — ไม่ใช่การค้าง: ระหว่างนั้น `frame` เดินปกติ และ resume ออกได้ด้วยคลิกเดียว

## 3. ข้อสรุป Phase A

| เกณฑ์ (§7 ของแผน) | ผล |
|---|---|
| pause ≥ 10 รอบ ใน state ที่ปุ่มกดได้ | **PASS 16/16** (เกิน 10) |
| resume ทุกรอบ: `paused` false + เกมเดินต่อ | **PASS 16/16** (`resumed=true` ทุกรอบ) |
| battle clock: delta 0.000 ตอน pause, > 0 หรือ state/turn เปลี่ยนหลัง resume | **PASS 16/16** (window นิ่งสนิท; หลัง resume มี progression ทุกรอบ) |
| ไม่มีคลิกที่ `WaitPlayerInput` | **PASS 0/32** ตรงตามมติ 2026-10-21 |
| UI/Input ตอบสนอง | **[USER-OBSERVED] PASS** — ผู้ใช้กด pause/resume ด้วยเมาส์ได้ทุกรอบ และเล่นต่อได้ตามปกติหลัง resume |

**Phase A = PASS** (16 รอบลงทะเบียน, 0 excluded, 0 ค้าง)

ข้อจำกัดของการวัด (บันทึกไว้ ไม่ได้ตีเป็น PASS เพิ่ม):
- ความละเอียด 100 ms ⇒ ระยะเวลา window คลาดได้ ±0.2 s
- สถานะ `nowActiveState` ที่รายงานคือค่าที่ sample จับได้ "ก่อน/ระหว่าง" window ไม่ใช่ค่าที่อ่านในจังหวะคลิกเป๊ะ ๆ (W4 บันทึกค่าที่จังหวะคลิกจริง — ใช้ตัดสินไม่ให้กดตอน `WaitPlayerInput` แล้ว)
- ไม่มีการวัด `activeAutoFight` ระหว่าง pause แบบแยกกรอบ: ทั้ง run เป็น `false` 100% (29,031/29,031 sample ในศึก)

## 4. หลักฐานอ้างอิง

| ไฟล์ | เนื้อหา |
|---|---|
| `logs/status-100ms.ndjson` | 30,515 บรรทัด (ok 30,510 / fail 5 ที่จุดเปลี่ยนฉาก-ออกเกม) |
| `logs/jitprobe-w4.ndjson` | 32 event (16 pause + 16 resume) |
| `logs/w4-table.txt` | ตาราง event W4 + state id→name ที่ derive จาก stream |
| `logs/analysis-final.txt` | ผล `analyze-s15g.py` (window, frozen, resumed, progression) |
| `logs/timeline-raw.txt` | timeline การเปลี่ยน state/paused/timeScale ทั้ง run |
