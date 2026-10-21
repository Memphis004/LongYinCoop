# evidence/area-20261022-0615 — read-only Area/UI survey

รอบ: 2026-10-22 06:10–06:37 · label `area-20261022-0615` · ผลลัพธ์: `LongYinCoop/area-notes.md`

## ขอบเขต
อ่านอย่างเดียว (ข้อยกเว้นสโคปที่ผู้ใช้อนุมัติ): ไม่มีโค้ด mod, ไม่ build/deploy, ไม่แก้ STAGEPLAN,
ไม่เขียน memory, ไม่เรียก method ที่เปลี่ยน state, ไม่กดปุ่มแทนผู้ใช้

## ลำดับเหตุการณ์
| เวลา | เหตุการณ์ | หลักฐาน |
|---|---|---|
| 06:03 | session-start: hash เกมตรง `VERSION.md` 2/2; พบเกมรันอยู่ (pid 2620, 06:00:22) บน **เซฟจริง** | `MelonLoader/Logs/26-10-22_6-0-22.log` |
| 06:03 | เทียบ hash รายไฟล์: ต่างเฉพาะ `PlayerprefData.dat` (volatile) · `SaveSlot*` 57 ไฟล์ไม่ถูกเขียน | สคริปต์เทียบ hash ในบทสนทนา |
| ~06:10 | ผู้ใช้ปิดเกม | `tasklist` = 0 |
| 06:15 | `rebaseline` volatile-only → `coop-test/baselines/20261022-061550/` | `REBASELINE.json` |
| 06:15 | `split --label area-20261022-0615 --expected-baseline …061550` | `MANIFEST.json`, `savehash.*.txt` |
| 06:16 | recorder `s15d_sample.py` pid 5204 | `sampler.log` |
| 06:17 | `preflight` **READY 5/5** | `history.ndjson` |
| 06:1x–06:35 | runtime survey ผ่าน il2cpp-frida-mcp (local attach) | `dump-extract.txt` + ข้อความใน notes |
| 06:35 | `verify` PASS: real folder changed=0, mtime drift 0 | เอาต์พุตใน notes §ท้าย |
| 06:37 | `restore` → `Save` จริงกลับมา (ไม่ถูกเขียนทับ) | `Save.used-20261022-063732` |
| 06:38 | `status` = 57 files digest `b069e56c1228`, `leftovers: none`; rehash อิสระ 0/0/0 | เอาต์พุตใน notes |
| 06:39 | incident: เปิดเกมบนเซฟจริงหลัง restore เพื่อทดสอบ `>>` (ไม่มี split/preflight) | pid 6076 |
| 06:39 | วัด `>>`: `totalWorkingDay=10` แต่ปฏิทิน +1 วันเท่านั้น, `skipping=true` ค้าง | ข้อความใน notes §P1-5 |
| 06:42 | ปิดเกม; rebaseline volatile-only → `coop-test/baselines/20261022-064205` (digest `1901ad90a2c7`) | `REBASELINE.json` |
| 06:43 | rehash อิสระ vs baseline ใหม่: changed=0 missing=0 added=0 | เอาต์พุตในบทสนทนา |
| 06:44 | incident #3: เปิดเกมบนเซฟจริงอีกครั้ง (pid 2784) เพื่อวัด `>>` ซ้ำ | `tasklist` |
| 06:44 | วัด `>>` รอบ 2 (2 samples): `day 4→5` (+1 เท่านั้น), `total 9→10`, `skipping=true` ค้าง, store[0] 7085.4 คงที่ | ข้อความใน notes |
| 06:47 | ปิดเกม; rebaseline #3 → `coop-test/baselines/20261022-064714` (digest `de7e2af49b23`); rehash 0/0/0 | `REBASELINE.json` |

## ผลวัดที่ได้ (RUNTIME)
- ตัวเอก heroID 0, atAreaID 55 仙霞派, force 25, heroForceLv 5
- เพดานปาร์ตี้ 3/6 = `len(teamMates)` / `heroForceLv+1` (asm `GetMaxTeamMate` = `[rcx+0xB8]+1`)
- insideHeros(11) == hero ที่ `atAreaID==55` (11) — set เดียวกัน
- งาน 10 วัน: `totalWorkingDay=10`, `dayTime=1 s/วัน`, ปฏิทิน +10, hour ไม่รีเซ็ต
- Wait 5 วัน: ผ่าน `WorkingUIController` ชุดเดียวกัน (`totalWorkingDay=5`), ปฏิทิน +5
- ฝึกวิชา 2 วัน: `chooseType=HeroSkill`, `targetHero=heroID 0`, `sendResultFuc=StudyFightSelfChoosen`
- หน้ารายชื่อทุกสำนัก = `ForceDetailController` (30 แท็บ)
- 48 field/class ต้นทางถูกบันทึกพร้อมป้ายใน `area-notes.md`

## กติกาของ `preflight-launch` (มติ 2026-10-22)

- PASS ต้องมี `leftovers = exactly [Save.real-<label>]` (unexpected = 0)
- `leftovers: none` ตาม `status` = เซฟจริงถูก mount = **REFUSED** (สภาพ incident)
- `Save.used-*` หรือ `Save.real-*` เกิน 1 ตัว = ambiguous = REFUSED
- `--dry-run` ไม่ give green light
- หลักฐาน: `guard-preflight-launch.txt` (real state REFUSED + sandbox 14/14)

## ข้อจำกัดของรอบนี้
- ปุ่ม `>>` วัดได้แต่ **ไม่ได้คุมตัวแปร** และทำบนเซฟจริงโดยไม่ตั้งใจ (incident 06:39) — ต้องวัดซ้ำแบบ split ก่อน
- `areaInteractionTimeData` ความหมายของค่า 1/1 ยังไม่ยืนยัน (GUESS)
- `GetMaxStudent()` = 6 แต่ `Students` มี 7 — ยังไม่ตรวจซ้ำ
- ตัวเลือก 1/5/10 เป็น UI ที่ผู้ใช้เห็น; ผมยืนยันได้แค่ว่าเลือก 10 แล้ว `totalWorkingDay=10`
