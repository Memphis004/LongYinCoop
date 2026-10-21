# Session-start checks — ก่อนเริ่ม S2 (2026-10-21 20:50)

โหมดงาน: read-only + เขียนเอกสารแผนเท่านั้น · **ไม่เปิดเกม · ไม่ build/copy DLL · ไม่แตะเซฟ · ไม่แก้ `src/`**

## 1. Anchor เวอร์ชันเกม (กติกาข้อ 9)

| ไฟล์ | SHA-256 ที่วัดได้ | ค่าที่ควรเป็น (`VERSION.md`) | ผล |
|---|---|---|---|
| `GameAssembly.dll` | `33c56def4102844a1190eec963a05c834cfcf992d42b423d485ce8584d7920c1` | เดียวกัน | **PASS** |
| `LongYinLiZhiZhuan_Data/il2cpp_data/Metadata/global-metadata.dat` | `451311d4dc8c1b8b5073905c8b39a9781bd7e1f44ed88fbf7b35f5345c18bfd0` | เดียวกัน | **PASS** |

หมายเหตุ: `VERSION.md` อยู่ใน repo mod (`C:\Users\memph\LongYinCoop\VERSION.md`) ไม่ได้อยู่ที่ root ของโฟลเดอร์เกม

## 2. เกมไม่รัน + สถานะเซฟ

`python tools\save-split.py status` (รันจาก repo mod):

```
data dir : C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan\LongYinLiZhiZhuan_Data
game     : not running
  Save                         files=57  digest=6dd645d9ef17
leftovers: none
mounted Save matches:
  EXACT  S1.5gB-20261021-200449/savehash.copy.before.txt
  EXACT  S1.5gB-20261021-200449/savehash.real.before.txt
```

- ไม่มี process `LongYinLiZhiZhuan.exe` [RUNTIME]
- `LongYinLiZhiZhuan_Data` มีโฟลเดอร์ `Save` เดียว — ไม่มี `Save.real-*`/`Save.used-*` [RUNTIME]
- process `Unity*` ที่เห็นในเครื่องเป็น **Unity Editor 6000.3.9f1** (`C:\Program Files\Unity\Hub\Editor\...`) ไม่ใช่เกม [RUNTIME]

## 3. Git

| repo | ค่า |
|---|---|
| mod repo (`C:\Users\memph\LongYinCoop`) | HEAD `e3e798a` (S1.5gB PASS) · working tree **สะอาด** (`status --porcelain` = 0 บรรทัด) |
| tag `stage-S1.5g` | annotated → `e3e798a` (peeled) · commit มี `evidence/S1.5gB-20261021-200449/` ครบ 14 ไฟล์ |
| game repo (โฟลเดอร์เกม) | HEAD `65dbd6a` = "AGENTS: S1.5g closed …" · tag มีแค่ `stage-S0`, `stage-S1` |
| game repo working tree | `?? STAGEPLAN.v2-draft.md`, `?? .opencode/`, `m il2cpp-frida-mcp` (submodule) — ไม่ได้แตะ |

**ข้อสังเกตเรื่อง tag:** tag `stage-S1.5g` อยู่ใน **mod repo** จึงครอบ commit `65dbd6a` ของ **game repo** ไม่ได้ (คนละ repo) — หลักฐาน S1.5gB ถูก tag ครบ; commit AGENTS อยู่ที่ HEAD ของ game repo โดยไม่มี tag · **ไม่ได้ย้าย/สร้าง tag ใหม่**

## 4. Prefs

`UserData\MelonPreferences.cfg` → `[LongYinCoop]`: `Enabled=true`, `Probe=true`, `Ipc=true`, `DevCommands=true`, `Capture=true`, `AllowRemoteInput=false`, `JitProbe*/W2/W4/W6/W7 = false`
→ **`JitProbeW4=false` PASS** · `AllowRemoteInput=false` PASS (ไม่มี write path จาก IPC)

## 5. Build ที่ติดตั้ง

`Mods\LongYinCoop.dll` = `517758147f59170e584075a4b42f61e00d293803febe36f8a040bcdad4592612` (202,752 B) — ตรงกับ build ที่ใช้ปิด S1.5g/`S1.5gB` [RUNTIME]

## 6. เอกสาร

- อ่าน `AGENTS.md`, `STAGEPLAN.md`, `S2-plan.md`, `VERSION.md`, `S1.5g*` evidence, `incident-real-save-mounted-1949.md`
- **ไม่ได้อ่าน `STAGEPLAN.v2-draft.md`** ตามคำสั่ง (ไม่ใช้เป็นแหล่งความจริง)

## 7. Baseline ที่อนุมัติ (มติผู้ใช้ session นี้)

- digest `6dd645d9ef17fb626c3b680d8c12b8bf5297d5fbce532b9e7bc4286d343ef19e` (57 ไฟล์)
- ไฟล์: `coop-test/baselines/20261021-200450/savehash.real.before.txt`
- บันทึก: `VERSION.md` §Baseline, `coop-test/baselines/20261021-200450/APPROVAL.md`, `AGENTS.md` (สถานะปัจจุบัน)

## 8. ผลรวม

| ข้อ | ผล |
|---|---|
| hash เกม 2 ไฟล์ | PASS |
| เกมไม่รัน | PASS |
| Save = ของจริง 57 ไฟล์ `6dd645d9ef17`, `leftovers: none` | PASS |
| git HEAD/tree/tag | PASS (ดูข้อสังเกต cross-repo) |
| `JitProbeW4=false` | PASS |
| ไม่อ่าน/ไม่ใช้ `STAGEPLAN.v2-draft.md` | PASS |
| เซฟไม่ถูกเขียนระหว่าง session นี้ | ต้องยืนยันซ้ำตอนจบ (hash เดิม) |
