# VERSION — LongYinLiZhiZhuan build anchors

ตรวจเมื่อ: 2026-10-20 (S0)
เครื่อง: Windows / `C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan`

| ไฟล์ | SHA-256 | ขนาด (bytes) |
|---|---|---|
| `GameAssembly.dll` | `33c56def4102844a1190eec963a05c834cfcf992d42b423d485ce8584d7920c1` | 33,669,120 |
| `LongYinLiZhiZhuan_Data/il2cpp_data/Metadata/global-metadata.dat` | `451311d4dc8c1b8b5073905c8b39a9781bd7e1f44ed88fbf7b35f5345c18bfd0` | 7,960,432 |

สภาพแวดล้อม: Unity **2020.3.48f1c1** · MelonLoader **0.7.3** · runtime **net6.0** · Il2CppInterop 1.5.1-ci.845

## Baseline เซฟที่อนุมัติ (มติผู้ใช้ 2026-10-21, session ก่อนเริ่ม S2)

| รายการ | ค่า |
|---|---|
| digest ที่อนุมัติ | `6dd645d9ef17fb626c3b680d8c12b8bf5297d5fbce532b9e7bc4286d343ef19e` (57 ไฟล์) |
| ไฟล์ baseline | `coop-test/baselines/20261021-200450/savehash.real.before.txt` |
| เหตุผล | ต่างจาก canonical `2b0548fe464b` **เฉพาะ `PlayerprefData.dat`** (volatile/pref drift จาก incident เปิดเกมบนเซฟจริง 16:07 และ 19:49) · `REBASELINE.json` = `volatile_only: true`, `changed: ["PlayerprefData.dat"]` |
| ผลบังคับ | ใช้เป็น expected baseline ของทุก `split` ตั้งแต่ S2 เป็นต้นไป — ต้องระบุ `--expected-baseline` ตรง ๆ ทุกครั้ง (ห้ามพึ่ง default ของ tool) |
| หลักฐาน | `evidence/S1.5gB-20261021-200449/save-verification.md`, `coop-test/baselines/20261021-200450/APPROVAL.md` |

`2b0548fe464b` ยังคงเป็นบันทึก canonical ของ S1.5 แต่ **ไม่ใช่เกณฑ์ผ่าน/ไม่ผ่านของ save อีกต่อไป** — เกณฑ์คือ `SaveSlot*` ต้องไม่เปลี่ยน และ digest ต้องตรง baseline ที่อนุมัติข้างต้น (ยกเว้น `PlayerprefData.dat`/`steam_autocloud.vdf`)

## Baseline ยุค S2a-2 (ประวัติ — rebaseline volatile-only · มติผู้ใช้ 2026-10-21 · superseded ตามสายโซ่ด้านล่าง)

| รายการ | ค่า |
|---|---|
| digest ที่อนุมัติ | `854cc890430f` = `6dd645d9ef17…` ที่เปลี่ยน `PlayerprefData.dat` ไฟล์เดียว |
| ไฟล์ baseline | `coop-test/baselines/20261021-225856/savehash.real.before.txt` |
| เหตุผล | incident 2026-10-21 22:54:18 — เกมถูกเปิดบนเซฟจริงระหว่างเตรียม S2a-2 (14 s หลัง session-start check ที่ยังพบเกมปิด) · **`SaveSlot*` ทั้ง 55 ไฟล์ไม่ถูกเขียน (ทรีรวม 57 ไฟล์ — นับจาก baseline `20261021-225856`)** · ผู้ใช้อนุมัติ **ทาง (ก) rebaseline volatile-only** |
| ผลบังคับ | ใช้ `--expected-baseline coop-test/baselines/20261021-225856/savehash.real.before.txt` สำหรับ `split` ของรอบ `S2a2-20261021-2254` (รันแล้ว) |
| หลักฐาน | `evidence/S2a2-20261021-2254/incident-game-open-2254.md`, `rebaseline.txt`, `coop-test/baselines/20261021-225856/REBASELINE.json` |

## Baseline ของรอบสำรวจ Area/UI (2026-10-22 · `area-20261022-0615`)

| รายการ | ค่า |
|---|---|
| baseline ก่อน split | `coop-test/baselines/20261022-061550/savehash.real.before.txt` · digest `b069e56c1228` |
| เหตุผล | เกมถูกเปิดบนเซฟจริง 06:00:22–06:1x **ก่อน** session (ผู้ใช้เป็นคนเปิด) ⇒ เขียน `PlayerprefData.dat` ไฟล์เดียว · **`SaveSlot*` ทั้ง 55 ไฟล์ไม่ถูกเขียน (ทรีรวม 57 ไฟล์)** |
| baseline ณ ตอนนั้น (หลัง incident 06:39) | `coop-test/baselines/20261022-064205/savehash.real.before.txt` · digest **`1901ad90a2c7`** |
| เหตุผล | incident 2026-10-22 06:39 — **เปิดเกมบนเซฟจริงหลัง restore** เพื่อทดสอบปุ่ม `>>` โดยไม่มี split/preflight · เปลี่ยนเฉพาะ `PlayerprefData.dat` (ตรวจ hash รายไฟล์) · ผู้ใช้ปิดเกมแล้ว · rebaseline volatile-only ตามกฎ |
| baseline ณ ตอนนั้น (ถูกแทนที่ 07:54 — ดูหัวข้อถัดไป) | `coop-test/baselines/20261022-064714/savehash.real.before.txt` · digest **`de7e2af49b23`** |
| เหตุผล | incident 2026-10-22 06:43 (ครั้งที่ 3) — เปิดเกมบนเซฟจริงหลัง restore อีกครั้งเพื่อวัด `>>` ซ้ำ · เปลี่ยนเฉพาะ `PlayerprefData.dat` · `SaveSlot*` 55 ไฟล์ไม่ถูกเขียน · rebaseline volatile-only |
| ผลบังคับ (ประวัติ) | ใช้ `--expected-baseline …/20261022-064714` เฉพาะช่วงก่อน 07:54 · หลังจากนั้นใช้ `4226730f66ff` (ยุค S2a-3 — ดูหัวข้อประวัติ) · ตั้งแต่ 2026-10-25 ใช้ `b7f67154cff6` (ยุค A/B) → **ปัจจุบันคือ `58883949…` @ `20261025-201545` (หัวข้อ Baseline ท้ายสุด)** |

## Baseline ของ S2a-3 (มติผู้ใช้ 2026-10-22 · หลังคืนเซฟ `explore-20261022-0755`) — **superseded สายโซ่ 2026-10-25: `20261025-052020` → `20261025-201545` (หัวข้อ Baseline ท้ายสุด)**

> สถานะ: เป็น expected baseline **เฉพาะยุค S2a-3** — ถูกแทนสายโซ่ `b7f67154cff6` (ยุค A/B) → `58883949…` (ปัจจุบัน) ตามหัวข้อ Baseline ด้านล่าง · ค่าด้านล่างคงไว้เป็นประวัติ ห้ามลบ

| รายการ | ค่า |
|---|---|
| digest ที่อนุมัติ | `4226730f66ffdcf9adb110fd8e26e29ffa8ba40620ccc33b013ef11adfaa46d7` (57 ไฟล์ · 485,795,554 bytes) |
| ไฟล์ baseline | `coop-test/baselines/20261022-075422/savehash.real.before.txt` |
| คำสั่งบังคับ | `python tools\save-split.py split --label <label> --expected-baseline coop-test\baselines\20261022-075422\savehash.real.before.txt` |
| เหตุผล | incident 2026-10-22 07:48 — เกมถูกเปิดบนเซฟจริงหลัง restore ของ `S2cd` · เทียบ `064714` แล้วเปลี่ยนเฉพาะ `PlayerprefData.dat` (`SaveSlot*` ไม่ถูกเขียน) · `REBASELINE.json` = `volatile_only: true`, `changed: ["PlayerprefData.dat"]` |
| การอนุมัติ | **มติผู้ใช้ 2026-10-22** หลัง restore `explore-20261022-0755` ผ่าน post-check 6/6 · `coop-test/baselines/20261022-075422/APPROVAL.md` |
| ยืนยันด้วยการวัด | `Save` 57 ไฟล์ digest `4226730f66ff` **EXACT** · `changed=0 missing=0 added=0` · `leftovers: none` · rehash อิสระ (ordinal sort) ตรงกัน · `evidence/S2a3-20261022-0156/restore-postcheck.md`, `presave-check.md` |
| ผลบังคับ | เป็น expected baseline ของ **S2a-3** (C6) และใช้ตัดสิน E7/X9/C8 · ถ้า digest ไม่ตรง **หยุดและรายงาน** ห้าม rebaseline กลบ |

## Baseline ของรอบ A/B (มติผู้ใช้ 2026-10-25 · Post-Close Save Verification · rebaseline volatile-only) — **superseded 2026-10-25 (ค่ำ) โดย `20261025-201545` (หัวข้อถัดไป)**

> สถานะ: ใช้จริงเฉพาะรอบ A/B (`AB-oldbuild` / `AB-newbuild`) — **ตั้งแต่รอบ NEG20 เปลี่ยนไปใช้ `58883949…` ตามหัวข้อ "Baseline ที่อนุมัติสำหรับรอบ NEG20"** · ค่าด้านล่างคงไว้เป็นประวัติ ห้ามลบ

| รายการ | ค่า |
|---|---|
| digest ที่อนุมัติ (ยุค A/B) | `b7f67154cff613c618994fbf6cb0866a0a8b360bbcb557f5b977320f57ce61cf` (57 ไฟล์ = SaveSlot* 55 + `PlayerprefData.dat` + `steam_autocloud.vdf`) |
| ไฟล์ baseline | `coop-test/baselines/20261025-052020/savehash.real.before.txt` |
| **คำสั่งบังคับ (ประวัติ — ยุค A/B)** | `python tools\save-split.py split --label <label> --expected-baseline coop-test\baselines\20261025-052020\savehash.real.before.txt` — **ทุก `split` ตั้งแต่ 2026-10-25 ต้องระบุ flag นี้ตรง ๆ ทุกครั้ง ห้ามพึ่ง default ของ tool** |
| เหตุผล | ต่อ incident บูตบนเซฟจริง 2026-10-25 04:56 (`evidence/S3p-20261025-0341/incident-boot-on-real-save.md` §6): drift = `PlayerprefData.dat` ไฟล์เดียว (hash ใหม่ `5bc97eb2d9db7cf5b9e49141b89ae40162c407eab4f20ef3849810422ac62a19`) · **`SaveSlot*` ทั้ง 55 ไฟล์ตรงครบ (ทรีรวม 57 ไฟล์)** · `REBASELINE.json` = `volatile_only: true` |
| สายโซ่ baseline | `4226730f66ff` (S2a-3 · `20261022-075422`) → `7332fc93eb98` (`20261024-232024` — SaveSlot8 ที่ผู้ใช้เซฟเอง + pref) → `b7f67154cff6` (ยุค A/B — pref เท่านั้น) → **`58883949…` (`20261025-201545` — ปัจจุบัน, หัวข้อถัดไป)** · ตัวเก่าคงไว้ไม่ลบ |
| ยืนยันด้วยการวัด | restore ของ `AB-oldbuild-20261025-054139` (A/B test 2026-10-25): `Save` 57 ไฟล์ `b7f67154cff6` **EXACT** (changed=0 missing=0 added=0) · `leftovers: none` · `evidence/S3p-20261025-0341/ab/ab-report.md` |
| ผลบังคับ (ประวัติ) | เป็น expected baseline **เฉพาะรอบ A/B** · **superseded — ตั้งแต่รอบ NEG20 ใช้ `20261025-201545` (หัวข้อถัดไป)** · ถ้า digest ไม่ตรง **หยุดและรายงาน ห้าม rebaseline กลบ** |

## Baseline ที่อนุมัติสำหรับรอบ NEG20 และ split ถัดไป (มติผู้ใช้ 2026-10-25 ค่ำ · rebaseline volatile-only) — **superseded 2026-10-28 โดย `20261028-071340` (หัวข้อถัดไป)**

| รายการ | ค่า |
|---|---|
| digest ที่อนุมัติ | **`588839490e5b418ae6e20172ea7af21916c9afa3a935c99525984fc9e16a0485`** (57 ไฟล์ = SaveSlot* 55 + `PlayerprefData.dat` + `steam_autocloud.vdf`) |
| ไฟล์ baseline | `coop-test/baselines/20261025-201545/savehash.real.before.txt` |
| **คำสั่งบังคับ** | `python tools\save-split.py split --label <label> --expected-baseline coop-test\baselines\20261025-201545\savehash.real.before.txt` — **ทุก `split` ตั้งแต่รอบ NEG20 ต้องระบุ flag นี้ตรง ๆ ทุกครั้ง ห้ามพึ่ง default ของ tool** |
| เหตุผล | ต่อเหตุการณ์เปิดเกมโดยไม่มี split 2 session (12:47:32, 15:05:27 — ไม่เข้าศึก, ปิดสะอาด): drift = `PlayerprefData.dat` ไฟล์เดียว (hash ใหม่ `50e1612b…`) · **`SaveSlot*` ทั้ง 55 ไฟล์ตรง baseline `20261025-052020` ครบ (ทรีรวม 57 ไฟล์)** · `REBASELINE.json` = `volatile_only: true` · อนุมัติ rebaseline volatile-only เฉพาะจุด (มติผู้ใช้ 2026-10-25 — ห้าม rebaseline แบบเหมารวม) |
| สถานะ | baseline **ที่อนุมัติภายหลังสำหรับรอบ NEG20 และ split ถัดไปที่ใช้ baseline นี้** · baseline เก่าทุกตัว (ล่าสุดก่อนหน้า = `b7f67154cff6` @ `20261025-052020`) คงอยู่เป็นประวัติ ห้ามลบ/เขียนทับ |
| ยืนยันด้วยการวัด | split ของ `NEG20-f2ad30cd-20261025-202331` EXACT (0/0/0) + restore หลังจบ NEG20: `Save` 57 ไฟล์ **EXACT** (changed=0 missing=0 added=0) · `leftovers: none` · `evidence/S3p-20261025-0341/neg20/neg20-report.md` |
| ผลบังคับ | เป็น expected baseline ของ `split` ถัดไป · ถ้า digest ไม่ตรง **หยุดและรายงาน ห้าม rebaseline กลบ** (rebaseline เพิ่มต้องอนุมัติแยก) |

## Mod build ที่ deploy ล่าสุด (2026-10-25 ค่ำ · build `1b4c17aa…` — rangecells patch · ก่อนหน้า: `f2ad30cd…`)

| รายการ | ค่า |
|---|---|
| `Mods\LongYinCoop.dll` | **`1b4c17aa6a934b25c884f62dbab59a795a8bbadf60fd2fc869ee0927fb3f0177`** (316,416 B · deploy 2026-10-25 ค่ำ เพื่อ Gate-2 closure retest — expose `attackRangeCells` ใน status (read-only) · byte-identical (`cmp`) กับ artifact `src\LongYinCoop\bin\Release\` · deterministic rebuild ยืนยัน hash ตรง source tree ปัจจุบัน) |
| `Mods\LongYinCoop.pdb` | **`dcd78ebd5241f25196c1c71296ab05dae3d14a034959f9d0d934155e1d4c1e17`** (83,180 B · byte-identical กับ artifact) |
| backup ก่อน deploy | `Mods\LongYinCoop.dll.prev-f2ad30cd` = `f2ad30cd2090a63879a74605c32efc999163b9d1873da6a68bd875e872f112a8` · `Mods\LongYinCoop.pdb.prev-fab0e051` = `fab0e051a6d279e2dae583073af88257670cc16c4d9853e3d2af5663dee1f891` (สร้าง 2026-10-25 ค่ำ — ยืนยันชื่อว่างก่อนสำรอง) |
| build ก่อนหน้า (ประวัติ) | `f2ad30cd2090a63879a74605c32efc999163b9d1873da6a68bd875e872f112a8` (314,880 B · source `59276a4` — S3 prerequisite patch · ใช้ A/B สองรอบ + NEG20 ครบ 20 เคส · สำรองที่ `.prev-f2ad30cd` และ `.s3prereq-f2ad30cd`) · `5d7b7cc5520ae784c5efc198dda8c1c3ba33ff3c55eb7aaa955ede5f63735953` (ยุค S2a-3 · source `ffc2c88` · A/B รอบแรก · สำรองที่ `.prev-5d7b7cc5`) |
| source ที่ตรงกับ build ใหม่ | HEAD `8fe33ce` **+ patch ยังไม่ commit** (rangecells: `TurnGate.cs` + `S3/RangeCells.cs` + runner/tests — diff ตรวจแล้วใน Phase 2 · deterministic rebuild = `1b4c17aa…` ตรงทุก byte) |
| `UserData\MelonPreferences.cfg` | `269b571c1b1572b7b525cda3e8a27e09b732b8eb31bdfe290269c8ed62a098d1` (ไม่เปลี่ยน) |
| `UserData\LongYinCoop\net.cfg` (A0/พัก) | **`12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8`** (444 B · `enabled=false` · `heartbeatMs=1000` — วัดจริง 2026-10-28, มติ [DEV-2] · **เคยบันทึกเป็น** `dc8e9098…61b61` 454 B — ค่านั้นล้าสมัย ดู divergence record หัวข้อ "Divergence record — net.cfg (DEV-2)" ท้ายไฟล์) |
| baseline ที่ใช้กับ build นี้ | `coop-test/baselines/20261025-201545/savehash.real.before.txt` · digest `588839490e5b418ae6e20172ea7af21916c9afa3a935c99525984fc9e16a0485` — split ทั้งสอง label บังคับ `--expected-baseline` และ restore แล้ว EXACT ทั้งคู่ |
| สถานะ 2026-10-26 | **rangecells patch (attackRangeCells — read-only instrumentation) deploy แล้ว และผ่าน Phase 3 retest ครบ 20/20** · **Gate 2 = PASS (มติผู้ใช้ 2026-10-26)** · **ต้นเหตุ crash เดิม (Gate-2 incident 04:14) ยัง UNKNOWN — แยกจากผล Gate 2 ชัดเจน** · rollback พร้อม (`.prev-f2ad30cd`) · patch rangecells **ยังไม่ commit** (source ณ HEAD `8fe33ce` + patch) |
| label ที่ใช้ล่าสุด | `NEG20R-case7-20261025-234850` (เคส 7 retest · restore EXACT · `leftovers: none`) · ก่อนหน้า: `NEG20R-1b4c17aa-20261025-230950` (w1/w2/w3 · restore EXACT · `leftovers: none`) · `NEG20-f2ad30cd-20261025-202331` + `AB-newbuild`/`AB-oldbuild` (restore EXACT ทั้งหมด) · ประวัติยุค S2a-3: `S2a3r-20261024-0115`, `S2a3x6-20261024-0120` |
| ผล Gate 2 closure retest (NEG20R · 2026-10-25/26 · build `1b4c17aa`) | negative 20 เคส same-binary: **PASS 20 · NOT-RUN 0 · FAIL 0 · UNKNOWN 0** · เคส 7–10 พิสูจน์ด้วย `attackRangeCells` สด (ยิงจริง 4 เคสผ่าน 4 เงื่อนไข + กันยิงเมื่อเป้าในระยะ 1 เคส) · เคส 11–14 ศพจริง (`unitAlive=false`, hp=0, (-1,-1)) + ศัตรูมีชีวิตเหลือ · restore EXACT ทั้งสอง split · รายงาน `evidence/S3p-20261025-0341/neg20r/neg20r-report.md` |
| ผล runtime รอบแรก (NEG20 · 2026-10-25 · build `f2ad30cd` — ประวัติ) | negative 20 เคส: **PASS 12 · NOT-RUN 8 (7–10 range-unverifiable + 11–14 structural) · FAIL 0 · UNKNOWN 0** · ทุกเคสที่ยิง stateHash ก่อน/หลังเหมือนเดิม · จบศึกปกติ (`state=Loot`) · restore EXACT · **NOT-RUN ทั้ง 8 ปิดครบด้วย NEG20R (หัวข้อข้างบน)** · รายงาน `evidence/S3p-20261025-0341/neg20/neg20-report.md` |
| หลักฐาน | `evidence/S3p-20261025-0341/{gate2-deploy.md,gate2-release-build.txt}` (provenance ยุคก่อน) · `ab/ab-report.md` · `neg20/neg20-report.md` · **`neg20r/neg20r-report.md` (Gate 2 closure)** · Phase 1–2 audit (diff + deterministic rebuild + tests 466+53) อยู่ในบันทึก session นี้ |

## Baseline ที่อนุมัติปัจจุบัน (มติผู้ใช้ 2026-10-28 [G-2] · rebaseline volatile-only · session `AUTO-20261028-070417`)

| รายการ | ค่า |
|---|---|
| digest ที่อนุมัติ | **`df8e5a11beed6e62cc18d40da9ae0f2d0d5fbfdc56640b4196ece95df1ed4920`** (57 ไฟล์ = SaveSlot* 55 + `PlayerprefData.dat` + `steam_autocloud.vdf`) |
| ไฟล์ baseline | `coop-test/baselines/20261028-071340/savehash.real.before.txt` |
| **คำสั่งบังคับ** | `python tools\save-split.py split --label <label> --expected-baseline coop-test\baselines\20261028-071340\savehash.real.before.txt` — **ทุก `split` ตั้งแต่ 2026-10-28 ต้องระบุ flag นี้ตรง ๆ ทุกครั้ง ห้ามพึ่ง default ของ tool** |
| เหตุผล | ปิด DEVIATION-1 ของ session `AUTO-20261028-023904`: drift = `PlayerprefData.dat` ไฟล์เดียว (hash `50e1612b…` → `efd4ac5b…`) จากการเปิดเกมของผู้ใช้ 2026-10-27 ×3 (ML logs 00:47/02:16/02:26) · **`SaveSlot*` ทั้ง 55 ไฟล์ EXACT baseline `20261025-201545`** · rehash อิสระ ADDED=0 MISSING=0 · `REBASELINE.json` = `volatile_only: true` |
| สายโซ่ baseline | `4226730f66ff` (S2a-3) → `7332fc93eb98` → `b7f67154cff6` (ยุค A/B) → `58883949…` (`20261025-201545` — NEG20) → **`df8e5a11beed…` (`20261028-071340` — ปัจจุบัน)** · ตัวเก่าคงไว้ไม่ลบ |
| ยืนยันด้วยการวัด | rebaseline ด้วย tool (`save-split.py rebaseline --against 20261025-201545`) exit 0 · Save 57 ไฟล์ EXACT baseline ใหม่ (rehash อิสระ diffs: NONE) · ไฟล์เซฟไม่ถูกแตะ (digest ก่อน/หลัง = `df8e5a11beed` เท่ากัน) · หลักฐาน `evidence/AUTO-20261028-070417/c2-independent-rehash.md`, `c3-c4-rebaseline-record.md` |
| ผลบังคับ | เป็น expected baseline ของ `split` ถัดไป (รวม GO-3 ขั้น 4 ของ RUNBOOK-G3) · ถ้า digest ไม่ตรง **หยุดและรายงาน ห้าม rebaseline กลบ** (rebaseline เพิ่มต้องอนุมัติแยก) |

## Divergence record — `net.cfg` (มติ [DEV-2] ผู้ใช้ 2026-10-28 · แก้บันทึก ไม่แตะไฟล์)

| รายการ | ค่า |
|---|---|
| ค่าที่วัดจริง (2026-10-28, session `AUTO-20261028-070417`) | sha256 **`12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8`** · ขนาด **444 B** · เนื้อหา semantic = `enabled=false`, `heartbeatMs=1000` (สถานะ "พัก" ตามอนุมัติ) |
| **เคยบันทึกเป็น** (2026-10-24, ยุค S2a-3/งาน A — ล้าสมัย) | `dc8e9098490d74c2dfbd6c8a1dffecfbd6befbfeccb2d235dbd0756f4d861b61` · 454 B |
| คำอธิบายหลักฐาน | ค่าที่วัดจริง `12541f34…` **ตรงกับบันทึก AGENTS.md หัวข้อ 2026-10-24** (net.cfg คืนสถานะอนุมัติหลังปิด stage S2a-3) · ขนาดที่รายงานเก่า (444 vs 454 B) ขัดกันเองในรายงานยุคนั้น — ค่าวัดจริงล่าสุด = 444 B · เนื้อหาไฟล์ไม่เปลี่ยน semantic (enabled=false, heartbeatMs=1000) ⇒ เป็นการแก้**บันทึก**เท่านั้น ไม่ใช่ไฟล์ |
| ข้อจำกัด | ห้ามคัดลอกค่า `secret` ลงเอกสารใด (ไม่ได้ log ใน record นี้) · ไฟล์ `UserData\LongYinCoop\net.cfg` ไม่ถูกแตะใน session นี้ (hash ก่อน/หลัง = `12541f34…` เท่ากัน) |

## กฎ

1. เทียบ SHA-256 ทั้งสองไฟล์ทุกครั้งที่เริ่ม session — **ไม่ตรง = หยุดและรายงาน** (เกมอัปเดต = anchor ใน notes ทั้งหมดเชื่อไม่ได้)
2. ห้าม commit `GameAssembly.dll`, `global-metadata.dat`, `MelonLoader/Il2CppAssemblies/`, `dump.cs` (ดู `.gitignore`)
3. ค่าใน `STAGEPLAN.md §13` (RVA/offset) ใช้สำรวจเท่านั้น โค้ดต้อง re-resolve ผ่าน interop
