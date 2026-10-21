# S2a-2 — รอบรันขั้น 1–10 (label `S2a2-20261021-2254`) · 2026-10-21 23:0x

**สถานะ ณ สิ้นรอบ: `PREFLIGHT READY` — ยังไม่เปิดเกม (หยุดตามคำสั่ง)**

## ผลทีละขั้น

| ขั้น | สิ่งที่ทำ | ผล | หลักฐาน |
|---|---|---|---|
| 1a | game repo: `git add AGENTS.md` แล้ว commit เดี่ยว (ไม่รวม `il2cpp-frida-mcp` / `.opencode/` / `STAGEPLAN.v2-draft.md`) | ✅ `61e3e3e` (1 ไฟล์) | `git log` |
| 1b | mod repo: commit source + plan rev4 + evidence step 0 | ✅ `427f815` (30 ไฟล์, 4838 insertions) tree สะอาด | `git log` |
| 2 | `dotnet build -c Release` | ✅ 0 warning / 0 error · `LongYinCoop.dll` sha256 `cb35a93f617b16b3cdf069a9c7e76d166dde757b8692b943c8b42b92026ceee0` (257,536 B) | `build.txt` |
| 3 | สำรอง `Mods\LongYinCoop.dll`/`.pdb` → `.off` | ✅ `51775814…2612` / `029d8a74…2fc1` (ตรงกับ DLL/PDB ก่อน copy) | `dll-before-after.txt` |
| 4 | pre-split checks | ⚠️ พบเกมถูกเปิด 22:54:18 + `PlayerprefData.dat` drift (volatile-only) ⇒ **หยุดถาม → ผู้ใช้อนุมัติ (ก)** | `presplit.txt`, `incident-game-open-2254.md` |
| 4a | `rebaseline --against …/20261021-200450` | ✅ baseline ใหม่ `coop-test/baselines/20261021-225856` (57 ไฟล์ digest `854cc890430f`, `volatile_only: true`, changed=[PlayerprefData.dat]) | `rebaseline.txt`, `coop-test/baselines/20261021-225856/REBASELINE.json` |
| 4b | สำรอง prefs ก่อนแก้ | ✅ sha256 `269b571c…98d1` (3,232 B) | `prefs.before.cfg`, `prefs-sha-before.txt` |
| 5 | `save-split split --label S2a2-20261021-2254 --expected-baseline …/20261021-225856/…` | ✅ 57 ไฟล์ · real-aside = `Save.real-S2a2-20261021-2254` · mounted copy ตรง real (`changed=0 missing=0 added=0`) | `split.txt`, `split-manifest.json`, `split-history.ndjson` |
| 6 | copy DLL/PDB → `Mods\` | ✅ `Mods\LongYinCoop.dll` sha256 = `cb35a93f…ceee0` **เท่ากับ build (ขั้น 2)** | `dll-before-after.txt`, `LongYinCoop.incoming.dll` |
| 7 | ตั้ง pref | ✅ `coop.net.enabled=false` · `coop.role=host` · `coop.secret=<64 hex>` · `coop.allowNonLoopback=false` · **`DevCommands=true` (explicit)** · `AllowRemoteInput=false` · `JitProbe/W2/W4/W6/W7=false` · sha256 ใหม่ `037fc14a…8ff9` | `pref-values.txt`, `prefs.after.cfg` |
| 8 | ตรวจเครื่องมือวัด | ✅ ผ่าน (เงื่อนไขล่วงหน้าครบ: `spike status` ต้อง Enabled+Ipc+DevCommands = ครบ; recorder อยู่ใน allowlist; replica มี `isAlive`+`unitAlive`) | `tools-check.txt` |
| 9 | เปิด recorder | ✅ `tools\s15d_sample.py` pid **46824** `--seconds 7200 --interval 0.5 --append --wait 3600` · ไฟล์ผล pre-created | `recorder.txt`, `recorder-stdout.log` |
| 10 | `preflight` | ✅ **`PREFLIGHT READY - you may launch the game`** (5/5 check OK) | `preflight.txt` |

## ผล preflight (ขั้น 10)

```
preflight: label=S2a2-20261021-2254
  OK   real-aside: Save.real-S2a2-20261021-2254 files=57 digest=854cc890430f
  OK   mounted-is-copy: files=57 vs copy baseline changed=0 missing=0 added=0
  OK   leftovers: Save.real-S2a2-20261021-2254
  OK   sampler: pid 46824 (s15d_sample.py) out=coop-test\S2a2-20261021-2254\sampler.ndjson
  OK   prefs-disarmed: 6 keys=false
PREFLIGHT READY - you may launch the game
```

## สภาพแวดล้อม ณ สิ้นรอบ

| รายการ | ค่า |
|---|---|
| เกม | **ไม่รัน** (ปิดโดยผู้ใช้ก่อนขั้น 4a) |
| `Save` (ที่ mount) | สำเนา 57 ไฟล์ digest `854cc890430f` |
| `Save.real-S2a2-20261021-2254` | เซฟจริง 57 ไฟล์ digest `854cc890430f` (aside, ไม่ถูกแตะ) |
| `Mods\LongYinCoop.dll` | `cb35a93f…ceee0` (build rev4) · สำรองเดิมอยู่ที่ `.dll.off` |
| `UserData\MelonPreferences.cfg` | `037fc14a…8ff9` |
| recorder | pid 46824 รันอยู่ (รอเกม ≤3600 s) |
| game repo HEAD | `61e3e3e` |
| mod repo HEAD | `427f815` (ยังไม่ commit หลักฐานรอบนี้) |

## หมายเหตุ/ความเสี่ยงที่ต้องบอกก่อนผู้ใช้เปิดเกม

1. **เกมจะโหลดสำเนา** ไม่ใช่เซฟจริง — ถ้าเปิดเกมแล้วเห็น SaveSlot แปลกไป/หาย ให้หยุดทันที (สำเนาอาจมี sentinel slot ที่ซ่อน)
2. **ห้ามเปิด IPC client อื่น** ระหว่างวัด (recorder ถือ pipe เดียว)
3. `coop.net.enabled=false` (เฟส A) — S2 จะ idle จนกว่าจะสลับเป็น `true` (D2-i)
4. ถ้า MelonLoader เขียน `MelonPreferences.cfg` ใหม่ (เช่นตอนออกเกม) บรรทัด `coop.*` ที่เพิ่มเองอาจหาย — ตรวจ sha256/ค่าใหม่ทุกครั้งหลังเกมปิด
5. เตือน: ตาม §2 คำเตือนตอนเล่น — ห้ามใช้ `set_pref`/`spike arm` ระหว่างวัด

## ยังไม่ทำ (ตามแผน/ตามคำสั่ง)

- ไม่เปิดเกม · ไม่เริ่มวัด · ไม่ปิด stage · ไม่ tag `stage-S2a-2` · ไม่ commit หลักฐานรอบนี้
- ไม่รัน `restore` (เซฟจริงยัง aside; จะคืนเมื่อจบรอบวัด)
