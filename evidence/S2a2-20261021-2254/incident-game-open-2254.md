# Incident — เกมถูกเปิดระหว่างเตรียม S2a-2 (2026-10-21 22:54:18) · STOP

## ลำดับเวลา (RUNTIME)

| เวลา | เหตุการณ์ | หลักฐาน |
|---|---|---|
| 22:54:04 | session-start checks: `tasklist` = **เกมไม่รัน** · `save-split status` = `Save` 57 ไฟล์ `digest=6dd645d9ef17` · `leftovers: none` · `mounted Save matches: EXACT` ทั้ง `S1.5gB-20261021-200449/savehash.copy.before.txt` และ `savehash.real.before.txt` | `save-split status` (ผลในบทสนทนา 22:54) |
| 22:54:18 | **เกมถูกเปิด** (PID 45256, `StartTime 22:54:18`) — หลังการตรวจ 14 วินาที | `Get-Process` |
| 22:54:31 | `Save\PlayerprefData.dat` ถูกเขียนใหม่ (mtime) | `Get-Item` |
| ~22:54:5x | mod (DLL เก่า `51775814…2612`, StageLabel `S1.5e-W7`) โหลดและ heartbeat ปกติ `tyicks` เพิ่ม, `err=0`, `battle=0/0` | `MelonLoader\Logs\26-10-21_22-54-18.log` |
| 23:0x | ผมตรวจซ้ำก่อนขั้น 5: `save-split status` = `game RUNNING` · `Save` 57 ไฟล์ **`digest=854cc890430f`** · `~S1.5gB-20261021-200449/savehash.copy.before.txt: changed=1 missing=0 added=0` | `presplit.txt` |

## สิ่งที่เปลี่ยน (คัดกรองครบ 57 ไฟล์)

เทียบ `Save` ปัจจุบัน กับ baseline `coop-test/baselines/20261021-200450/savehash.real.before.txt`:

- `changed` = **1 ไฟล์: `Save/PlayerprefData.dat` เท่านั้น**
- `missing` = 0 · `added` = 0
- **`SaveSlot*` ไม่ถูกเขียนเลย** (ทั้ง 57 ไฟล์ครบ, digest ของทุกไฟล์ใน `SaveSlot*` ตรง baseline)

⇒ เป็น **volatile-only pref drift** แบบเดียวกับ incident 16:07 และ 19:49 (AGENTS.md)
(เกมเขียน `PlayerprefData.dat` ทุกครั้งที่เปิด/ปิด — build นี้ไม่มี autosave ตอนออก)

## สถานะอื่นที่ตรวจแล้ว (ไม่เปลี่ยน)

| รายการ | ค่า | เทียบ anchor |
|---|---|---|
| `GameAssembly.dll` | `33c56def…20c1` | ตรง `VERSION.md` |
| `global-metadata.dat` | `451311d4…bfd0` | ตรง `VERSION.md` |
| `UserData\MelonPreferences.cfg` | `269b571c…98d1` (mtime 20:28:09) | **ไม่ถูกเขียน** — ตรงกับที่จดไว้ |
| `Mods\LongYinCoop.dll` | `51775814…2612` (mtime 01:25:43) | DLL เก่า ยังไม่ได้ copy ใหม่ (ขั้น 6 ยังไม่ทำ) |
| `Mods\LongYinCoop.dll.off` / `.pdb.off` | `51775814…2612` / `029d8a74…2fc1` | สำรองจากขั้น 3 ตรงกับ DLL/PDB ปัจจุบัน |

## สถานะงาน S2a-2 ตอนหยุด

- ✅ ขั้น 1: commit — game repo `61e3e3e` (AGENTS.md, แยก commit) · mod repo `427f815` (30 ไฟล์) tree สะอาด
- ✅ ขั้น 2: Release build สำเร็จ 0 warning/0 error · `LongYinCoop.dll` sha256 `cb35a93f617b16b3cdf069a9c7e76d166dde757b8692b943c8b42b92026ceee0` (257,536 B) → `build.txt`
- ✅ ขั้น 3: สำรอง `.off` แล้ว → `dll-before-after.txt`
- ⛔ **ขั้น 4/5 หยุด**: `cmd_split` บังคับ `require_game_closed` และ `--expected-baseline` จะ REFUSED (Save ไม่ตรง baseline)
- ⛔ ขั้น 6–10 ยังไม่ทำ (ไม่ copy DLL ใหม่, ไม่ตั้ง pref, ไม่เปิด recorder, ไม่ preflight)

## ยังไม่ได้แตะอะไรในเซฟจริง

อ่านอย่างเดียวทั้งหมด · ไม่ได้รัน `restore` / `rebaseline` / `split` · ไม่ได้เขียน `Save` หรือ `Save.real-*`

## ต้องได้คำตัดสินจากผู้ใช้

1. **ปิดเกม** ก่อน (บังคับสำหรับ `split`, `restore`, `rebaseline` ทุกกรณี)
2. เลือกทาง:
   - **(ก) rebaseline volatile-only** — สร้าง baseline ใหม่ (เช่น `coop-test/baselines/<ts>`) โดยบันทึกเฉพาะ `PlayerprefData.dat` ที่เปลี่ยน (`REBASELINE.json: volatile_only: true`) แล้ว `split --expected-baseline <ใหม่>` · ตรงกับแนวปฏิบัติของ incident 16:07/19:49 · **ต้องอนุมัติ baseline ใหม่**
   - **(ข) `restore`** — เขียนสำเนาจาก `coop-test/baselines/20261021-200450` (หรือ `S1.5gB-20261021-200449/Save.used-*`) กลับทับ เพื่อให้ digest กลับเป็น `6dd645d9ef17` แล้ว split ด้วย baseline เดิม (เขียนไฟล์จริงใน `Save` — ต้องอนุมัติ)
   - **(ค) หยุด stage ไว้** — ผมเขียนรายงานและไม่ทำอะไรต่อ
