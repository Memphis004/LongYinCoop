# S1.5-c nowScale — PREP (2026-10-20 21:33)

## 1. การเปลี่ยนแปลง (ได้รับอนุมัติจากผู้ใช้ 21:31)

- `src/LongYinCoop/S1.5/TurnGate.cs`: `spike status` เพิ่มฟิลด์ `nowScale` = `BattleController.nowScale` (typed, `Math.Round(...,4)`, อ่านอย่างเดียว, ห่อ try/catch, ไม่ใส่ใน `stateHash`)
- `NotInBattle()` คืน `nowScale = -1f` (schema เดียวกัน)
- เหตุผล: ปุ่ม x5 ไม่ขยับ `Time.timeScale`, `deltaTime`, `battleTime` หรือระยะแอนิเมชัน → ฟิลด์ความเร็วเดียวที่มีใน `BattleController` คือ `nowScale` (`0xE0`) [DUMP]

## 2. build / deploy

| ข้อ | ค่า |
|---|---|
| build | `dotnet build -c Release` → **0 warning / 0 error** |
| DLL ใหม่ | `45fa4036ab8f` |
| DLL เดิม (ที่รันอยู่ในรอบก่อน) | `742b1e7b011b` |
| deploy | คัดลอกแล้ว · backup `Mods/LongYinCoop.dll.prev-742b1e7b011b` |
| หลักฐาน | `logs/deploy.log` |

## 3. สถานะเซฟก่อนเปิดเกม (RUNTIME 21:33)

- เกมปิดเองเมื่อ 21:32:24 (log: `probe: unpatched (owned patches before=1 after=0)` · `Preferences Saved!` · `IPC client disconnected`) — ปิดสะอาด ไม่มี crash
- `verify --label S1.5c-originhp-20261020-172346` → **RESULT: PASS**
  - mounted `Save` = สำเนา (ต่างจาก copy baseline แค่ `PlayerprefData.dat` ซึ่งเป็น volatile)
  - sentinel `.hidden-SaveSlot8` ครบ 5 ไฟล์
  - `Save.real-S1.5c-originhp-20261020-172346` = **0/0/0** vs baseline · mtime/size drift **0**
- pref: แก้ `MelonPreferences.cfg` ให้ `JitProbeW4 = false` ด้วยมือ (ไฟล์ยังเป็น true ตอนปิดเกม) — ไม่แตะ pref อื่น (`Enabled=true`, `DevCommands=true`, `AllowRemoteInput=false`)

## 4. แผนวัด (ทำตอนเปิดเกมรอบถัดไป)

1. เปิดเกม → `spike status` ต้องมีฟิลด์ `nowScale` (ยืนยันว่า DLL ใหม่โหลด)
2. เริ่ม sampler 100 ms
3. เข้า battle → กดปุ่มเร่ง **x5** → รอ ~3 วิ → กลับ **x1**
4. เกณฑ์: `nowScale` ต้องเปลี่ยนตามปุ่ม (ค่าที่คาด: 1 → ค่าที่มากกว่า 1 แล้วกลับ 1) — ถ้าไม่เปลี่ยน = ปุ่มไม่ได้เขียนฟิลด์นี้ → ต้องหาตัวชี้ตัวอื่น (ห้ามเดา)
5. ระหว่างวัด ให้หยุด = ไม่ต้องกด pause (ไม่เกี่ยวกับ exit 5)
