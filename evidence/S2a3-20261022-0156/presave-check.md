# presave-check — read-only ก่อนคืนเซฟ (ก่อน Gate #2)

รอบ: 2026-10-22 ~14:2x–14:3x · **read-only ทั้งหมด**: ไม่ restore / ไม่ rename / ไม่ split / ไม่แก้เซฟ / ไม่แก้เอกสาร / ไม่ build / ไม่ deploy / ไม่เปิดเกม
ผู้รัน: agent · label ที่เกี่ยวข้อง: `explore-20261022-0755` (ของ session อื่น) · stage ที่กำลังทำ: S2a-3 (C4–C6 ยังไม่เริ่ม)

---

## 1. สถานะจริงตอนนี้ (`save-split.py status`, 14:2x)

```
game     : not running
  Save                          files=57  digest=2e593e635ad9   <- สำเนาที่ mount อยู่
  Save.real-explore-20261022-0755 files=57 digest=4226730f66ff  <- เซฟจริง (renamed aside)
leftovers: Save.real-explore-20261022-0755
mounted Save matches ~explore-20261022-0755/savehash.copy.before.txt: changed=1 missing=0 added=0
```

**rehash อิสระ** (สคริปต์ PowerShell แยกจาก `save-split.py`; สูตรเดียวกับ `digest_of` = `sha256` ต่อด้วย `"<rel>\0<sha256>\n"` เรียง **ordinal**) — ต้องเรียงแบบ `[StringComparer]::Ordinal` มิฉะนั้น `SaveSlot10` จะสลับที่กับ `SaveSlot2` และ digest จะไม่ตรง:

| โฟลเดอร์ | ไฟล์ | bytes | digest (อิสระ) | ตรงกับ |
|---|---|---|---|---|
| `Save` (สำเนา) | 57 | 485,795,554 | `2e593e635ad9b588956720538a7587ad26b32c80695254c7e8a44e7a3bfa5251` | tool: `2e593e635ad9` ✅ |
| `Save.real-explore-20261022-0755` | 57 | 485,795,554 | `4226730f66ffdcf9adb110fd8e26e29ffa8ba40620ccc33b013ef11adfaa46d7` | baseline `20261022-075422` ✅ (changed=0 missing=0 added=0) |

สำเนาเทียบ baseline `20261022-075422`: **changed=1 missing=0 added=0**
- `PlayerprefData.dat` — baseline `547f038f148c051c3c1bd05c287d40ed7551feb6a6ac24a57472beaf318673ec` → ปัจจุบัน `2488f86197d706f6ee507964f63661a4870c05b93946c7a351e6dc2e08acfcdf`
- `SaveSlot*` **0 ไฟล์เปลี่ยน** (55 slot files + steam_autocloud ครบ)

อื่น ๆ: ไม่มี `Save.used-*` · ไม่มีไฟล์ sentinel/ซ่อนใน `Save` · 11 โฟลเดอร์ slot ครบ · `net.cfg` **ยังไม่มี** (`UserData\LongYinCoop\net.cfg` = False) · `MelonPreferences.cfg` sha256 `269b571c1b1572b7b525cda3e8a27e09b732b8eb31bdfe290269c8ed62a098d1`, 69 บรรทัด, **ไม่มี dotted `coop.*`** (มีแต่ `[LongYinCoop]` + `[UnityExplorer]`)

## 2. หลักฐาน session `explore-20261022-0755`

| หลักฐาน | ค่า |
|---|---|
| `coop-test/explore-20261022-0755/history.ndjson` | `split 07:55:01` (57 ไฟล์) · `preflight 07:56:26 ok=true` (sampler pid 2204) · `verify 13:49:36 ok=true` (ของรอบนี้) |
| `MANIFEST.json` | real dir = `Save.real-explore-20261022-0755` · copy dir = `coop-test/explore-20261022-0755/save` · `expect_baseline` = `baselines/20261022-075422/savehash.real.before.txt` · digest 4226730f66ff ทั้ง real และ copy |
| `savehash.real.before.txt` / `savehash.copy.before.txt` | 57 ไฟล์ทั้งคู่, digest = `4226730f66ff…` (ตอน split สำเนาเหมือนจริงทุกไบต์) |
| `sampler.log` | ลงท้าย `[08:07:03] game exited` → **sampler ตายแล้ว** และไม่มี `python s15d_sample.py` ใน process list (เหลือแค่ MCP server 2 ตัว) |
| `sampler.ndjson` | 1,015,078 bytes (info only) |
| `evidence/explore-20261022-0755/phase1.md` | บันทึก session (เกมเปิด 07:57, ผู้ใช้ปิด 08:07) — **ไม่มีบันทึกว่า restore** |
| ไม่มี `restore-*.json` ใน label dir | ⇒ ยืนยันว่า **ยังไม่เคย restore** |

ข้อสังเกตเล็กของหลักฐานเขา: `phase1.md` บรรทัด 12 เขียนว่า drift "`PlayerprefData.dat` (+`steam_autocloud.vdf`)" แต่ `REBASELINE.json` ของ `20261022-075422` ระบุ `changed: ["PlayerprefData.dat"]` เท่านั้น — ถ้อยคำไม่ตรงหลักฐาน (ไม่กระทบ hash: `steam_autocloud.vdf` ไม่เปลี่ยนไบต์)

## 3. วิธี restore ตาม runbook (อ่านจาก `tools/save-split.py::cmd_restore` + help)

คำสั่ง (ยัง **ไม่รัน**):
```
python tools\save-split.py restore --label explore-20261022-0755
```
(ไม่ต้องใส่ `--real-dir` เพราะ `MANIFEST.json` มี `real.dir`)

สิ่งที่ tool ทำจริง (ลำดับ):
1. `require_game_closed` — ถ้าเกมรันอยู่ = ปฏิเสธ
2. ต้องมี `coop-test/explore-20261022-0755/savehash.real.before.txt` + `MANIFEST.json`
3. ต้องมีโฟลเดอร์ `Save.real-explore-20261022-0755` และ `Save`
4. **ปฏิเสธถ้ามีไฟล์ sentinel ที่ยังซ่อนใน `Save`** (ตอนนี้ไม่มี)
5. ตรวจ drift ของสำเนา: ปฏิเสธถ้ามีไฟล์เปลี่ยนที่ไม่ใช่ `SaveSlot*` และไม่ใช่ volatile (`PlayerprefData.dat`/`steam_autocloud.vdf`) — ตอนนี้ changed=1 = pref เท่านั้น ⇒ ผ่าน
6. `rename Save → Save.used-<ts>` แล้ว `rename Save.real-… → Save` (**ย้ายชื่อ ไม่เขียนทับ ไม่ copy**)
7. rehash `Save` เทียบ `savehash.real.before.txt` → ถ้าไม่ตรง = **rollback อัตโนมัติ** (สลับกลับ) และจบด้วย FAILED
8. ย้าย `Save.used-<ts>` ไปเก็บใน `coop-test/explore-20261022-0755/` · เขียน `restore-<ts>.json` · append `history.ndjson`

### เงื่อนไขตรวจสอบ **ก่อน** restore (ทั้งหมดวัดแล้ววันนี้)
| # | เงื่อนไข | สถานะ |
|---|---|---|
| 1 | เกมไม่รัน | ✅ `game process running: False` |
| 2 | sampler ของ session เดิมไม่รัน | ✅ pid 2204 ตาย, log `game exited` |
| 3 | `status` มี leftovers = `Save.real-explore-20261022-0755` | ✅ |
| 4 | real folder = baseline `4226730f66ff` เป๊ะ | ✅ (อิสระ, changed=0) |
| 5 | สำเนา drift เฉพาะ `PlayerprefData.dat` | ✅ (อิสระ, changed=1) |
| 6 | ไม่มี sentinel ที่ซ่อนอยู่ | ✅ |
| 7 | ไม่มี session อื่นกำลังยุ่งกับเซฟ | ⚠️ **ต้องยืนยันจากผู้ใช้** (ผมตรวจได้แค่ process) |
| 8 | พื้นที่ดิสก์ (การ rename ไม่ใช้พื้นที่เพิ่ม) | ✅ free 175 GiB |

### เงื่อนไขตรวจสอบ **หลัง** restore
| # | เงื่อนไข | วิธี |
|---|---|---|
| 1 | `status` = `Save` 57 ไฟล์ digest `4226730f66ff`, **ไม่มี** `leftovers` | `python tools\save-split.py status` |
| 2 | rehash อิสระของ `Save` = `4226730f66ff…` และ changed=0 vs `baselines/20261022-075422` | สคริปต์ ordinal digest (ซ้ำข้อ 1) |
| 3 | `restore-<ts>.json` มี `ok: true`, `restored_files: 57`, `digest 4226730f66ff…` | อ่านไฟล์ |
| 4 | สำเนาถูกเก็บไว้ครบ 57 ไฟล์ที่ `coop-test/explore-20261022-0755/Save.used-<ts>` | `Get-ChildItem` นับไฟล์ |
| 5 | `history.ndjson` มี event `restore` | อ่านท้ายไฟล์ |
| 6 | ไม่มีไฟล์ใน `LongYinLiZhiZhuan_Data` ที่ขึ้นต้นด้วย `Save.real-` / `Save.used-` | `Get-ChildItem` |

## 4. baseline `4226730f66ff` — ตรวจว่า "ตรง" อย่างไร

- **57 ไฟล์ · 485,795,554 bytes** (เท่ากันทั้ง real folder, `savehash.real.before.txt` และ `savehash.copy.before.txt`)
- digest = `sha256( concat( "<rel>\0<sha256(file)>\n" ) )` เรียง **ordinal** → `4226730f66ffdcf9adb110fd8e26e29ffa8ba40620ccc33b013ef11adfaa46d7`
- คำนวณซ้ำด้วยโค้ดคนละภาษา/คนละเครื่องมือ (PowerShell `Get-FileHash` + SHA256 ของสตริง) ได้ค่าเดียวกัน ⇒ **ไม่ใช่การเชื่อ tool ตัวเดียว**
- `REBASELINE.json` (`coop-test/baselines/20261022-075422`): `against = …/20261022-064714`, `files 57`, `changed ["PlayerprefData.dat"]`, `volatile_only: true`, `digest 4226730f66ff…`, เหตุผล = incident 07:48 (เกมถูกเปิดบนเซฟจริงหลัง restore ของ S2cd)

### ลำดับ baseline ทั้งหมด (digest คำนวณจาก hash file ด้วย `digest_of`)
| baseline dir | digest | หมายเหตุ |
|---|---|---|
| `20261020-165555` | `2b0548fe464b` | canonical ของ S1.5 |
| `20261021-200450` | `6dd645d9ef17` | baseline ที่อนุมัติก่อน S2 |
| `20261021-225856` | `854cc890430f` | S2a-2 |
| `20261022-061550` | `b069e56c1228` | ก่อน split ของรอบ Area |
| `20261022-064205` | `1901ad90a2c7` | incident 06:39 |
| `20261022-064714` | `de7e2af49b23` | incident 06:43 |
| **`20261022-075422`** | **`4226730f66ff`** | incident 07:48 — **ใช้โดย split ของ explore และเป็นตัวที่ตรงกับเซฟจริงตอนนี้** |

## 5. เอกสารที่ยังเขียนค่าเก่า — รายการที่ต้องแก้ (ยังไม่ได้แก้)

| ไฟล์ | บรรทัด | ตอนนี้เขียน | ควรเป็น / ควรเพิ่ม |
|---|---|---|---|
| `VERSION.md` | §"Baseline ของรอบสำรวจ Area/UI" L43–45 | "baseline ปัจจุบัน (สุดท้าย) = `de7e2af49b23`" + "ใช้ `--expected-baseline …/20261022-064714` สำหรับ split ครั้งถัดไป" | เพิ่ม baseline `20261022-075422` = `4226730f66ff` (incident 07:48, volatile-only, `changed ["PlayerprefData.dat"]`) และชี้ `--expected-baseline` ไปที่ 075422 |
| `VERSION.md` | ไม่มีหัวข้อ | — | เพิ่ม §"Baseline ของรอบ explore-20261022-0755" + สถานะ split ที่ค้าง และเหตุผลที่ real = 4226730f66ff |
| `AGENTS.md` (อยู่ใน **game repo**) | L78 | "baseline ปัจจุบันของ S2a-2 = `854cc890430f`" | ต่อท้ายว่าปัจจุบันคือ `4226730f66ff` (20261022-075422) — เก็บ `854cc890430f` ไว้เป็นประวัติ |
| `AGENTS.md` | L103 | "เซฟจริงหลัง restore ตรง baseline `de7e2af49b23`" | จริง ณ 07:31 แต่ถูกแทนที่ 07:54 ด้วย `4226730f66ff` ⇒ ต้องแก้ให้ตรง (หรือระบุว่า "ก่อน rebaseline 07:54") |
| `AGENTS.md` | ท้ายไฟล์ (สถานะปัจจุบัน) | ไม่มีสถานะเซฟล่าสุด | เพิ่มบรรทัด: split ค้าง `explore-20261022-0755`, `Save` = สำเนา (`2e593e635ad9`, drift pref), real = `4226730f66ff`, `leftovers: Save.real-explore-20261022-0755`, ยังไม่ restore |
| `S2a-3-plan.md` | L26, 203, 230, 272, 274, 333, 342 | X9 ใช้ digest `854cc890430f` และ C6 ใช้ `--expected-baseline coop-test\baselines\20261021-225856\...` | เปลี่ยนเป็น `4226730f66ff` + `coop-test\baselines\20261022-075422\savehash.real.before.txt` (หรือเขียนว่า "baseline ที่อนุมัติล่าสุด ณ วันรัน" พร้อมลิงก์) |
| `S2-plan.md` | L6, 247, 254, 351 | อ้าง `2b0548fe464b` / `6dd645d9ef17` / `517758147f59` ในบริบท S2a-0..2 | **ไม่ต้องแก้** (เป็นประวัติของ stage ที่ปิดแล้ว) — แต่ §13.3 ถ้าใช้เป็นเกณฑ์ runtime ต้องชี้ baseline ปัจจุบัน |
| `coop-test/baselines/20261022-075422/` | — | มี `REBASELINE.json` + `savehash.real.before.txt` | **ไม่มีไฟล์อนุมัติของผู้ใช้** (ต่างจาก `20261021-200450/APPROVAL.md`) ⇒ เสนอเพิ่ม `APPROVAL.md` สั้น ๆ ก่อนใช้เป็น expected baseline ของ C6 |
| `evidence/explore-20261022-0755/phase1.md` | L12 | "…(+`steam_autocloud.vdf`)" | แก้ถ้อยคำให้ตรง `REBASELINE.json` (`changed` = `PlayerprefData.dat` ไฟล์เดียว) — ของ session นั้น ผมไม่แก้เอง |

## 6. ลำดับที่ปลอดภัย (ยังไม่ดำเนินการ)

1. ผู้ใช้ยืนยันว่าไม่มี session อื่นถือเซฟ/กำลังเปิดเกม (ข้อ 7 ของ pre-check)
2. `python tools\save-split.py restore --label explore-20261022-0755`
3. ตรวจ post-check 6 ข้อ (§3) — ถ้าข้อ 1–2 ไม่ตรง **หยุดและรายงาน** ห้าม rebaseline กลบ
4. เอกสาร (หลังอนุมัติเท่านั้น): แก้ `VERSION.md` → `AGENTS.md` → `S2a-3-plan.md` ตาม §5 + เพิ่ม `APPROVAL.md`
5. **C4** (Gate #2): สำรอง DLL ปัจจุบันเป็น `Mods\LongYinCoop.dll.prev-cb35a93f` + backup `MelonPreferences.cfg` (sha ก่อน/หลัง) — ⚠️ ดูข้อ 7
6. **C5**: copy DLL/PDB ใหม่ (`c51debef…ae96`), สร้าง `net.cfg` (`enabled=false`, `role=host`, secret 64 hex, `bind=127.0.0.1`, `port=47800`, `sendHz=30`, `heartbeatMs=1000`), sha256 ครบ
7. **C6**: session-start checks → `split --expected-baseline coop-test\baselines\20261022-075422\savehash.real.before.txt --label S2a3-<ts>` → recorder → `preflight` READY
8. ผู้ใช้เปิดเกมเอง → C7

## 7. สิ่งที่พบและต้องตัดสินใจ (ไม่ใช่คำสั่ง)

1. **`Mods\LongYinCoop.dll.off` ไม่ใช่ build S2a-2** — sha256 = `517758147f59170e584075a4b42f61e00d293803febe36f8a040bcdad4592612` = build เก่ากว่า (ตัวที่ใช้ทดสอบ rollback B / S1.5e-W7) · build S2a-2 คือ `cb35a93f…ceee0` ที่อยู่ใน `LongYinCoop.dll` ตอนนี้ ⇒ ถ้า rollback ด้วย `.off` จะถอยไปไกลกว่าหนึ่ง stage · C4 ต้องสำรอง `cb35a93f` เป็น `.prev-cb35a93f` **ก่อน** ทับ
2. `UserData\LongYinCoop\net.cfg` **ยังไม่มี** — C5 ต้องสร้างใหม่ (secret ใหม่ 32 ไบต์ hex) · `MelonPreferences.cfg` สะอาด (ไม่มี dotted key) sha `269b571c…098d1`
3. game repo (โฟลเดอร์เกม) มีความเปลี่ยนแปลงค้างของ session อื่น: `D STAGEPLAN.md`, `D battle-notes.md`, `D worldmap-notes.md`, `?? .opencode/`, `?? Plans/`, `?? Prompt.md`, `?? context.txt`, `m il2cpp-frida-mcp` — **ผมไม่แตะ** แต่ต้องรู้ก่อนจะ commit AGENTS.md (จะปนกัน)
4. `verify` ที่ผมรัน (13:49 และรอบนี้) เขียนไฟล์ลง label dir: `verify-20261022-134936.json` + history event — เป็น artifact ของ tool ไม่ใช่การเขียนเซฟ (ไม่มีไฟล์ใต้ `Save`/`Save.real-*` ถูกเขียน)
5. digest ต้องเรียงแบบ **ordinal** — ถ้าเรียงแบบ culture (เช่น `Sort-Object` เปล่า ๆ) จะได้ `2ad93482a14b` ซึ่ง**ผิด** (ผมเจอเองระหว่างตรวจ) ⇒ ห้ามใช้สคริปต์ที่ sort แบบ culture เป็นหลักฐาน
