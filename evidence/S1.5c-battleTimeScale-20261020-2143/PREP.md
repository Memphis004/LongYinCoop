# S1.5-c battleTimeScale — PREP (2026-10-20 21:43)

## 1. การเปลี่ยนแปลง (อนุมัติโดยผู้ใช้ 21:41)

- `src/LongYinCoop/S1.5/TurnGate.cs`: `spike status` เพิ่ม `battleTimeScale` = `GameController.Instance.worldData.battleTimeScale` (`Math.Round(...,4)`, typed, อ่านอย่างเดียว, ไม่อยู่ใน `stateHash`)
- `NotInBattle()` คืน `battleTimeScale = -1f` (schema เดียวกัน)
- ที่มา: **[ASM]** `BattleController.SetTimeScaleTab()` อ่าน `[rax+0x1D8]` → **[DUMP]** `float` ตัวเดียวที่ offset นี้ = `WorldData.battleTimeScale`
- ตัวเข้า: `GameController.Instance.worldData` — โค้ดเดิมใช้ path นี้อยู่แล้ว (`src/LongYinCoop/S1/WorldSampler.cs:94`)

## 2. build / deploy

| ข้อ | ค่า |
|---|---|
| build | `dotnet build -c Release` → **0 warning / 0 error** |
| DLL ใหม่ | **`c45ac7b53ae2`** |
| DLL เดิม | `45fa4036ab8f` (backup `Mods/LongYinCoop.dll.prev-45fa4036ab8f`) |
| deployed hash | `c45ac7b53ae2` (ตรงกับ build) |

## 3. สถานะเซฟก่อนเปิดเกม (RUNTIME 21:43)

`verify --label S1.5c-originhp-20261020-172346` → **RESULT: PASS**
- mounted `Save` = สำเนา · ต่างจาก copy baseline แค่ `PlayerprefData.dat` (volatile)
- sentinel `.hidden-SaveSlot8` ครบ 5 ไฟล์
- `Save.real-S1.5c-originhp-20261020-172346` = **0/0/0** vs baseline · drift **0**

pref: `JitProbe=false`, `JitProbeW2=false`, `JitProbeW4=false` (ไม่มี hook เปิด)

## 4. ผลวัดรอบก่อน (`nowScale`) — สรุปสุดท้าย

`evidence/S1.5c-nowscale-20261020-2133/logs/spike-full.ndjson`: 3,975 sample ที่ ok (21:35:27–21:42:09) + **2 record error ตอนท้าย** = ตอนที่ผม kill sampler ระหว่างรอ (pipe ถูกปิดกลางคำขอ) ไม่ใช่ความผิดของ mod
`nowScale` = **1 ทุก sample** · `timeScale` = 1 ทุก sample · `paused` = false ตลอด

## 5. แผนวัดรอบถัดไป

1. เปิดเกม → `spike status` ต้องมี `battleTimeScale`
2. ค่าคาดหวังเริ่มต้น: `battleTimeScale` = 1 (ถ้าเป็นค่าที่บันทึกไว้ในเซฟ อาจไม่ใช่ 1 — บันทึกค่าจริงไว้)
3. กดปุ่มเร่ง (x2 และ x5 ถ้ามี) ค้างละ ~3 วิ แล้วกลับ x1
4. เกณฑ์: ค่าต้องเปลี่ยนตามปุ่ม และ `timeScale` ของ Unity ยังคงเป็น 1 (ถ้าเปลี่ยน = เกมใช้ Time.timeScale ด้วย)
5. ยังไม่ต้องกด pause ในรอบนี้
