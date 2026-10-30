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

## Mod build ที่ deploy ล่าสุด (2026-10-25 ค่ำ · build `1b4c17aa…` — rangecells patch · ก่อนหน้า: `f2ad30cd…`) — **superseded 2026-10-29 โดย build `6a94abb5…` (หัวข้อถัดไป)**

| รายการ | ค่า |
|---|---|
| `Mods\LongYinCoop.dll` | **`1b4c17aa6a934b25c884f62dbab59a795a8bbadf60fd2fc869ee0927fb3f0177`** (316,416 B · deploy 2026-10-25 ค่ำ เพื่อ Gate-2 closure retest — expose `attackRangeCells` ใน status (read-only) · byte-identical (`cmp`) กับ artifact `src\LongYinCoop\bin\Release\` · deterministic rebuild ยืนยัน hash ตรง source tree ปัจจุบัน) |
| `Mods\LongYinCoop.pdb` | **`dcd78ebd5241f25196c1c71296ab05dae3d14a034959f9d0d934155e1d4c1e17`** (83,180 B · byte-identical กับ artifact) |
| backup ก่อน deploy | `Mods\LongYinCoop.dll.prev-f2ad30cd` = `f2ad30cd2090a63879a74605c32efc999163b9d1873da6a68bd875e872f112a8` · `Mods\LongYinCoop.pdb.prev-fab0e051` = `fab0e051a6d279e2dae583073af88257670cc16c4d9853e3d2af5663dee1f891` (สร้าง 2026-10-25 ค่ำ — ยืนยันชื่อว่างก่อนสำรอง) |
| build ก่อนหน้า (ประวัติ) | `f2ad30cd2090a63879a74605c32efc999163b9d1873da6a68bd875e872f112a8` (314,880 B · source `59276a4` — S3 prerequisite patch · ใช้ A/B สองรอบ + NEG20 ครบ 20 เคส · สำรองที่ `.prev-f2ad30cd` และ `.s3prereq-f2ad30cd`) · `5d7b7cc5520ae784c5efc198dda8c1c3ba33ff3c55eb7aaa955ede5f63735953` (ยุค S2a-3 · source `ffc2c88` · A/B รอบแรก · สำรองที่ `.prev-5d7b7cc5`) |
| source ที่ตรงกับ build ใหม่ | HEAD `8fe33ce` **+ patch ยังไม่ commit** (rangecells: `TurnGate.cs` + `S3/RangeCells.cs` + runner/tests — diff ตรวจแล้วใน Phase 2 · deterministic rebuild = `1b4c17aa…` ตรงทุก byte) — **อัปเดต 2026-10-29: patch commit แล้ว @ `82e9093`** (Gate 2 closure) |
| `UserData\MelonPreferences.cfg` | `269b571c1b1572b7b525cda3e8a27e09b732b8eb31bdfe290269c8ed62a098d1` (ไม่เปลี่ยน) |
| `UserData\LongYinCoop\net.cfg` (A0/พัก) | **`12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8`** (444 B · `enabled=false` · `heartbeatMs=1000` — วัดจริง 2026-10-28, มติ [DEV-2] · **เคยบันทึกเป็น** `dc8e9098…61b61` 454 B — ค่านั้นล้าสมัย ดู divergence record หัวข้อ "Divergence record — net.cfg (DEV-2)" ท้ายไฟล์) |
| baseline ที่ใช้กับ build นี้ | `coop-test/baselines/20261025-201545/savehash.real.before.txt` · digest `588839490e5b418ae6e20172ea7af21916c9afa3a935c99525984fc9e16a0485` — split ทั้งสอง label บังคับ `--expected-baseline` และ restore แล้ว EXACT ทั้งคู่ |
| สถานะ 2026-10-26 | **rangecells patch (attackRangeCells — read-only instrumentation) deploy แล้ว และผ่าน Phase 3 retest ครบ 20/20** · **Gate 2 = PASS (มติผู้ใช้ 2026-10-26)** · **ต้นเหตุ crash เดิม (Gate-2 incident 04:14) ยัง UNKNOWN — แยกจากผล Gate 2 ชัดเจน** · rollback พร้อม (`.prev-f2ad30cd`) · patch rangecells **ยังไม่ commit** (source ณ HEAD `8fe33ce` + patch — **อัปเดต 2026-10-29: commit แล้ว @ `82e9093`**) |
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

## Mod build ที่ deploy ล่าสุด (2026-10-29 · build `6a94abb5…` — S2a-4 GO-3: overlay + set_pref gate · ก่อนหน้า: `1b4c17aa…`)

> deploy 2026-10-28 07:40:28 ใน GO-3 (วลี `เริ่ม GO-3 session` = สิทธิ์ [N1]) · วัดจริงครบเกณฑ์ GO-3 → **คงไว้ใน Mods\ ตามมติ [G-3] ทาง (ก) (ผู้ใช้ 2026-10-29 — ห้าม rollback, `.prev` คงไว้)** · รายงาน `evidence/S2a4-GO3-20261028-074003/s2a4-report.md` (8 PASS · 1 PASS-with-limitation · 0 FAIL)

| รายการ | ค่า |
|---|---|
| `Mods\LongYinCoop.dll` (ปัจจุบัน) | **`6a94abb579e65d5e6b0af917dfed4ba6f5350ad76847190bd7263cda3077b334`** (359,936 B · deploy 2026-10-28 07:40:28 · hash หลัง deploy ตรง artifact provenance READY @ mod repo `1359217`) |
| `Mods\LongYinCoop.pdb` (ปัจจุบัน) | **`856d3beda727218823170f861f532509c5458eb91e51c79f07c0452d3b196035`** (97,128 B) |
| **เคยบันทึกเป็น** (build ก่อน — rollback point) | DLL `1b4c17aa6a934b25c884f62dbab59a795a8bbadf60fd2fc869ee0927fb3f0177` (316,416 B) · PDB `dcd78ebd5241f25196c1c71296ab05dae3d14a034959f9d0d934155e1d4c1e17` (83,180 B) — สำรองไว้ **`Mods\LongYinCoop.dll.prev-1b4c17aa`** + **`Mods\LongYinCoop.pdb.prev-dcd78ebd`** (hash ตรง ยืนยัน 2026-10-29 01:14) |
| source ที่ตรงกับ build ใหม่ | `636c209ecd61a572f25d40179b99de370e9892fb` (using fix — บล็อคเดียวที่ต่างจาก provenance build) · docs `1359217` · tag `stage-S2a-4` @ `3ac0761` (2026-10-29 01:27) · เคยบันทึกเป็น "ยังไม่ tag (รอมติผู้ใช้)" |
| `UserData\MelonPreferences.cfg` | **`66fb92d51e5d9d24ca2578740b561a3bc6fb4876ebfeddce944535e9a02e74cf`** · **เคยบันทึกเป็น `269b571c1b1572b7b525cda3e8a27e09b732b8eb31bdfe290269c8ed62a098d1`** — divergence record: ML เขียนไฟล์ตอนปิดเกม (01:06:22, "Preferences Saved!") หลัง pref dirty จาก E6 set_pref roundtrip (true→false คืนทันที) · **ค่า `AllowRemoteInput = false` ถูกต้อง** (บรรทัด 9 — ตรวจแล้ว 2026-10-29 01:14) · agent ไม่ได้แก้ไฟล์นี้ |
| `UserData\LongYinCoop\net.cfg` | `12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8` (444 B · `enabled=false`, `heartbeatMs=1000` — คืน EXACT หลัง GO-3 ตาม RUNBOOK ขั้น 8 · ระหว่าง session ใช้ `5ce166da…` = enabled=true+overlay=true) |
| baseline เซฟที่ใช้ | `coop-test/baselines/20261028-071340/savehash.real.before.txt` · digest `df8e5a11beed…` (คงเดิม — หลัง GO-3 restore EXACT + `leftovers: none`) |
| ผล GO-3 สรุป | G0/E1/E2/E3/E4/E6/E7/E8 = PASS · E5 = PASS-with-limitation (M5-3: E5(a) NOT-RUN — ไม่มี hook) · E4 Δmean +0.244 ms ≤ 1.0 · ตรวจหลัง deploy ไม่มีบรรทัด SP5/Sp5T0 ใน ML log · `commandsAccepted=0` ตลอด |

## V2-A session (2026-10-29 · PROVISIONAL — ไม่ปิด stage ไม่ tag · รายงาน `evidence/V2A-20261029-020816/v2a-report.md` @ `ee993d1` — แก้ไขเพิ่มหลัง close: `44b255b` (8→9 runs ตามมติ) · `c07ae7c` (fix งานตรวจค้าง) · `ee993d1` (ผลสืบสวน UNKNOWN — log/โค้ด/disasm เท่านั้น) · `9663a21` (สถานะปิดงาน §9 — มติ (ข) 2026-10-31))

| รายการ | ค่า |
|---|---|
| `Mods\LongYinCoop.dll` (ปัจจุบัน) | `6a94abb579e65d5e6b0af917dfed4ba6f5350ad76847190bd7263cda3077b334` (359,936 B) — **rollback กลับตามมติผู้ใช้ 2026-10-29 (ทาง ข)** หลังปิด session V2-A · verify hash ก่อน/หลัง copy ตรง · build V2-A `983b238432a466535e6f3bfad1408f4ee0a657f910f2169af84f0d2f9d16893b` (366,080 B) เก็บที่ `evidence/V2A-20261029-020816/` · `.prev` ทั้งหมดคงอยู่ไม่ลบ |
| `Mods\LongYinCoop.pdb` (ปัจจุบัน) | `856d3beda727218823170f861f532509c5458eb91e51c79f07c0452d3b196035` (97,128 B) — rollback พร้อมกัน · build V2-A `951de8fc1385a285cb1a02174639e92b1a6a3719bf6028e2adcfdbe52afee7d6` เก็บใน evidence เดียวกัน |
| runs ของ V2-A — นับจาก label dir จริง = **9** | ไม่รวม `V2A-20261029-020816` (dir ของ artifact/รายงาน) และ `AUTO-20261029-013511` (dir ของ session) |
| runs — **5 valid** (เข้า fit) | A3-r1 · A3-r3 (N=10 ×2) · A2-r2 (N=5) · A1-r2 · A1-r3 (N=1 ×2) — measured 13.500 / 13.464 / 7.136 / 2.082 / 2.068 s |
| runs — **2 เข้าข่ายกฎ gap** (CONTAMINATED · auto-exclude) | A3-r2 (16,118 ms · popup กลางงานหลัง day_tick วันที่ 6) · A2-r3 (9,043 ms · popup "ได้ไม้" หลัง day_tick วันที่ 1) |
| runs — **1 INVALID** (โครง) | A2-r1 (work_start ×4 · day_ticks 13≠5 · gap 43,428–77,289 ms จบด้วย work_start ใหม่ — เข้าข่ายกฎ gap ด้วย) |
| runs — **1 pilot** (ไม่เข้า fit) | A1-r1 (2.088 s) |
| ผล fit 5 จุด | slope **1.2675 s/วัน** (ผ่าน 1.25±0.1) · intercept **0.8056 s** · residual +20/−16/−7/+9/−5 ms |
| ป้ายตามเกณฑ์ | **UNRESOLVED_OFFSET** · CI_t (df=3, t=3.182) [0.7665, 0.8446] — ไม่คาบเกี่ยวทั้ง FORM1 [−0.1,+0.1] และ FORM2 [+0.4,+0.6] · CI 1.96 (ไม่ valid df ต่ำ — เทียบรอบก่อน) [0.7815, 0.8296] |
| deviation §E.1 | ≥3 รอบ/ค่า → ≥2 valid/ค่า (ทาง B ลดรูป มติ 2026-10-29) — **N=5 ไม่ครบเกณฑ์ ≥2 valid (มี 1 รัน: A2-r2)** · N=10/N=1 ครบ 2 รัน · PROVISIONAL จนมีรอบซ้ำเพิ่ม |
| `UserData\MelonPreferences.cfg` | ปัจจุบัน `976ccfb9a6e6ff5be9718229b25af3e4b2bd765edeae419bc6fa0cd0ba39d325` · **เคยบันทึกเป็น** `66fb92d51e5d9d24ca2578740b561a3bc6fb4876ebfeddce944535e9a02e74cf` (ก่อน session) → `dd33ef531c2adae2fcd75d0cb2d4833c554705bc36e774c19de6a35619d2436e` (agent ตั้ง `TimeV2a=true` ก่อนวัด) → `b07cab85241bb0700a2d7e3ebc868b18aaf309be837bd3f6f56582adb82404ee` (ML exit-save ตอนปิดเกม) → `976ccfb9…` (agent คืน `TimeV2a=false` ตอนปิด session) — **ค่า key semantic ตรงตลอด** (ตรวจด้วยค่า key ไม่ใช่ hash — บทเรียน RUNBOOK-G3 บรรทัด 120: `evidence/S2a4-GO3-20261028-030025/RUNBOOK-G3.md` + commit `3ac0761`) |
| `UserData\LongYinCoop\net.cfg` | `12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8` (444 B · `enabled=false`) — **ไม่ถูกแตะตลอด session** |
| pointer file `time-v2a.out` | ลบแล้วตอนปิด session (fail-closed — ไม่มีผลเมื่อ `TimeV2a=false`) |
| baseline เซฟ | `df8e5a11beed6e62cc18d40da9ae0f2d0d5fbfdc56640b4196ece95df1ed4920` @ `20261028-071340` คงเดิม — restore EXACT ทุก run |
| tag ที่เกี่ยว | `stage-S2a-4` → `3ac0761` (2026-10-29 01:27) คงเดิม · ไม่มี tag ใหม่ · ไม่ push |
| สถานะปิดงาน (docs-only · มติ (ข) 2026-10-31 · session `AUTO-20261031-031942`) | **รับ PROVISIONAL เป็นคำตอบของชุด A** — ป้ายผลคง **UNRESOLVED_OFFSET (PROVISIONAL)** ตาม [CRITERIA] · ข้อจำกัด: N=10 valid 2 รัน (A3-r1/A3-r3 ต่าง 36 ms) · N=5 valid 1 รัน (A2-r2) · N=1 valid 2 รัน (A1-r2/A1-r3 — A1-r1 pilot ไม่นับ) ⇒ ไม่ครบ ≥3 รอบ/ค่าตามแผน §E.1 เดิม · A4 (skip `>>`) ไม่ได้วัด เลื่อน · popup/ปั๊มค้าง = UNKNOWN 2/9 รัน (A3-r2 gap 16,118 ms หลัง day_tick วันที่ 6 · A2-r3 gap 9,043 ms "ได้ไม้" หลัง day_tick วันที่ 1 · USER TESTIMONY 2026-10-30 = "ไม่แน่ใจ" ทั้ง 3 ข้อ) · คำอธิบาย offset ยังเป็น hypothesis (freeze gate ~34–42 เฟรม — ยังไม่พิสูจน์ตรง) · รายละเอียด = `v2a-report.md` §9 (commit `9663a21`) · การปิดงานนี้ docs-only — ไม่เปิดเกม ไม่แตะเซฟ/pref/Mods\ · สิ่งที่ตรวจซ้ำณ ปิดงาน: DLL `6a94abb5…`/PDB `856d3bed…` ✓ · `TimeV2a=false` ✓ · `time-v2a.out` ไม่พบ (ลบแล้ว) ✓ · `.prev-6a94abb5`/`.prev-856d3bed` คงอยู่ ✓ |

## Mod build ที่ deploy ล่าสุด (2026-10-30 · build `865b4a87…` — SP5-T0 probe log-only · ก่อนหน้า: `6a94abb5…`)

> deploy 2026-10-30 01:2x ใน session `SP5T0-run-20261030-012502` (วลีผู้ใช้ "รัน SP5-T0" = มติ §4 ข้อ 2–8
> แผน rev 13) · **กำหนดจากวลีสั่ง: จบรอบ rollback DLL กลับ `6a94abb5…`** · provenance READY
> `evidence/SP5T0-prep-20261030-010247/` @ mod repo `ab9d07b` (a998509) · รอบรัน `evidence/SP5T0-run-20261030-012502/`

| รายการ | ค่า |
|---|---|
| `Mods\LongYinCoop.dll` (ปัจจุบัน) | **`865b4a87b6ee4aa5b5ac2b20e63fb4537ab14c49210948875b93eed8a441a84a`** (366,592 B · deploy 2026-10-30 01:2x · hash หลัง deploy ตรง artifact provenance READY เป๊ะ) |
| `Mods\LongYinCoop.pdb` (ปัจจุบัน) | **`60ad4546aa556ab2613525b2f193d9ed3b690e7163346541b72b14c958e51c0d`** (99,568 B บนดิสก์ — provenance.md เขียน 99,572 B: **sha256 ตรงตามตารางเป๊ะ**, ส่วนตัวเลขขนาดในเอกสารคลาด 4 B บันทึกตามจริง) |
| **เคยบันทึกเป็น** (build ก่อน — rollback point) | DLL `6a94abb579e65d5e6b0af917dfed4ba6f5350ad76847190bd7263cda3077b334` (359,936 B) · PDB `856d3beda727218823170f861f532509c5458eb91e51c79f07c0452d3b196035` (97,128 B) — สำรองไว้ **`Mods\LongYinCoop.dll.prev-6a94abb5-20261030`** + **`Mods\LongYinCoop.pdb.prev-856d3bed-20261030`** (hash ตรง ยืนยัน 2026-10-30 01:2x) |
| source ที่ตรงกับ build ใหม่ | `ab9d07b` (Sp5T0LinkageHarness + ป้าย linkage — ตรรกะ probe เดียวกับ `6a94abb5` @ `636c209` สายโซ่) · determinism ×2 ยืนยันใน provenance |
| `UserData\MelonPreferences.cfg` | ก่อน `976ccfb9a6e6ff5be9718229b25af3e4b2bd765edeae419bc6fa0cd0ba39d325` → หลังตั้ง **`5a9d4280f2dd8590e6f5498dd45a6562eb2bbbd43c634b41fccd5a2e6505a519`** (`Sp5T0 = true` บรรทัด 15 — agent แก้ 1 บรรทัดตามมติ ข้อ 2 "ตั้ง Sp5T0=true ก่อนเปิดเกม"; key อื่นทุกค่าเดิม) |
| `UserData\LongYinCoop\net.cfg` | `12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8` (444 B · `enabled=false`) — **ไม่แตะ** (T0 ไม่ใช้) |
| baseline เซฟที่ใช้ | `coop-test/baselines/20261028-071340/savehash.real.before.txt` · digest `df8e5a11beed…` — split label `SP5T0-20261030-012502` (Save vs baseline changed=0 missing=0 added=0) · จบรอบ restore + rollback DLL |
| **สถานะหลังจบรอบ (2026-10-30 01:4x)** | **rollback แล้วตามวลีสั่ง:** `Mods\LongYinCoop.dll` = `6a94abb5…` + PDB `856d3bed…` (hash ตรง rollback point ยืนยัน 01:44) · `MelonPreferences.cfg` = `976ccfb9…` **EXACT byte-level ก่อน session** (`Sp5T0=false` — ML ที่เขียนตอนออกเกมเป็น cosmetic rewrite, ค่า key ทุกตัวเหมือนเดิม) · เซฟ restore EXACT + `leftovers: none` · `SaveSlot*` ไม่ถูกเขียนตลอดรอบ (โลกใหม่ไม่ได้เซฟลง slot) · รอบรันผลครบ: ติด 7 patch/ถอน ownedLeft=0 · events=3014 errors=0 · E4/E5/E6/E7 ปิด · E1/E2 คง UNKNOWN ตาม rev 13 · พบบั๊ก comparator string-format ของ probe (allMatch=false เป็น artifact — inventory เชิงความหมายตรงทุกจุด) · รายงาน `evidence/SP5T0-run-20261030-012502/round-report.md` |

## Baseline ที่อนุมัติปัจจุบัน (มติผู้ใช้ 2026-10-30 N4-follow-up · rebaseline volatile-only — แทนหัวข้อ [G-2] ด้านบนเป็น anchor ปัจจุบัน) — **superseded 2026-10-31 โดย `20261031-001811` (หัวข้อถัดไป)**

| รายการ | ค่า |
|---|---|
| เหตุการณ์ | ผู้ใช้เปิดเกมเอง 05:45:05–05:46:28 (ML log `26-10-30_5-45-5.log` — ไม่มีบรรทัด probe/gate/S2, [ERROR]=0) ระหว่างเซสชัน offline ⇒ snapshot ไม่ตรง ⇒ เซสชันโค้ด N4-follow-up หยุดตามเงื่อนไข แล้วมติผู้ใช้อนุมัติทาง (ก) rebaseline volatile-only (รูปแบบ [G-2]) · incident `evidence/AUTO-20261030-060328/incident-game-open-0545.md` |
| ยืนยันก่อน rebaseline | เซฟรายไฟล์เทียบ `20261028-071340`: **56/57 EXACT · CHANGED = `Save/PlayerprefData.dat` ไฟล์เดียว** · missing=0 · extra=0 ⇒ `SaveSlot*` ทั้ง 55 ไม่ถูกเขียน (วัดซ้ำ 06:07 ครั้งที่ 2 ตรง) |
| digest ที่อนุมัติ (anchor ใหม่) | **`7e1e65e52659e878650b206e43322692879aad9750d047091f34450e94830fa6`** (57 ไฟล์) @ `coop-test/baselines/20261030-060854/savehash.real.before.txt` · `REBASELINE.json` = `volatile_only: true, changed=[PlayerprefData.dat]` |
| สายโซ่ baseline | `4226730f66ff` → `7332fc93eb98` → `b7f67154cff6` → `58883949…` (`20261025-201545`) → `df8e5a11beed…` (`20261028-071340`) → **`7e1e65e52659…` (`20261030-060854` — ปัจจุบัน)** · ตัวเก่าคงไว้ไม่ลบ |
| `UserData\MelonPreferences.cfg` | `976ccfb9a6e6ff5be9718229b25af3e4b2bd765edeae419bc6fa0cd0ba39d325` (หลังปิด N3) → `733d9a0c8fd801cace2c5640421facb2852c646a011754a8660e30149884130c` (ML เขียนตอนปิดเกม 05:46:28) → `5a9d4280f2dd8590e6f5498dd45a6562eb2bbbd43c634b41fccd5a2e6505a519` (agent ตั้ง `Sp5T0=true` ช่วงรัน N4 รอบ 2–3 · ML exit-save ×2 ที่ 06:42:56/06:46:31) → **`976ccfb9…` ปัจจุบัน** (agent คืน `Sp5T0=false` ปิดเซสชัน 06:47) — **ค่า key semantic ตรงตลอดทุกจุด**: `Sp5T0 = false` · `TimeV2a = false` · `AllowRemoteInput = false` (ตรวจด้วยค่า key ไม่ใช่ hash — บทเรียน RUNBOOK-G3; **ML เขียน cfg ทุกครั้งที่ปิดเกม = cosmetic rewrite ตามปกติ คาดหวังได้**) |
| `net.cfg` / `Mods\LongYinCoop.dll` | `12541f34…` / `6a94abb5…` — ไม่ถูกแตะ (วัด 06:03 + 06:07 · หลังจบ N4 รอบ 2–3 ยืนยันซ้ำ 06:47 ตรงเป๊ะ) |
| **drift รอบที่ 3 (ผู้ใช้เปิดเกม 06:54–06:58 — ML log `26-10-30_6-54-6.log`)** | เซฟ digest `7e1e65e52659…` → **`d3f7cb2fcae2…`** (ไฟล์เดียว = `PlayerprefData.dat`, SaveSlot* ครบ · วัด 07:12) · cfg `976ccfb9…` → **`733d9a0c…`** (ML exit-save — ค่า key ตรงตลอด) · **มติ 2026-10-30: ดำเนินงาน push (git-side) ต่อโดยไม่ rebaseline ตอนนี้ — สถานะนี้ = anchor เริ่มต้นเซสชันถัดไป** · **คำเตือน: เซสชัน runtime ถัดไปต้อง `rebaseline volatile-only --against 20261030-060854` ก่อน split** (มิฉะนั้น expected-baseline check จะ fail ที่ pref) · ปัจจุบัน push แล้วโดยเซสชันขนาน: origin/master = `08a5ceb` (สายโซ่ครบจน 08a5ceb) + tag `stage-N4` = `eb3db5b5…` (push โดยเซสชันนี้ 07:1x) |

## Mod build ที่ deploy 2026-10-31 (build `ddbb7fe8…` — TimeV1 probe log-only · **ยกเลิกโดยผู้ใช้ + rollback แล้ว**)

> deploy 2026-10-31 ~05:00:28 ใน session `เริ่ม V1 session` (RUNBOOK-V1 · `evidence/TIME-V1-20261031-044249/`) ·
> **มติผู้ใช้ 2026-10-31: ยกเลิก V1 → rollback DLL ทันที** หลังรอบรันจบด้วย `INCOMPLETE-probe-down`
> (TimeV1 ติด 2/6 patch ⇒ RUNBOOK §G.1) · รายงานปิดสาย `evidence/TIME-V1-20261031-044249/V1-close-report.md`

| รายการ | ค่า |
|---|---|
| build ที่ deploy (ตอนนี้ถอนแล้ว) | DLL **`ddbb7fe87896f58fb56c86d4483ffdc60e069d4e0d728c5eafcbbf457f60d30a`** (390,144 B) · PDB **`1213b165f917ce775df9d604e697cf2540e6c943d438ca15234540d9cf936fdc`** (109,536 B) |
| source ที่ตรงกับ build | mod repo `6e99c538e7a2178e9c99f8c2b5032bc137fc3ad4` (provenance READY · determinism ×2 · 0W/0E) |
| rollback point ที่ใช้ | `.prev-6a94abb5-20261031-050028` (DLL) + `.prev-856d3bed-20261031-050028` (PDB) — **ทั้งคู่ยังอยู่ ไม่ถูกลบ** · หมายเหตุ: mtime ของ `.prev` = 02:55:29 ขัด suffix `050028` = anomaly คงไว้ (UNKNOWN) |
| `UserData\MelonPreferences.cfg` | `TimeV1a` เพิ่มใน build นี้ (default false) · หลังปิดเซสชัน: `TimeV1a=false` · `TimeV2a=false` · `Sp5T0=false` · `AllowRemoteInput=false` (ตรวจด้วยค่า key ไม่ใช่ hash) |
| `UserData\LongYinCoop\net.cfg` | `12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8` (444 B · `enabled=false`) — **ไม่ถูกแตะตลอดรอบ** |
| baseline เซฟที่ใช้ตอน split | `coop-test/baselines/20261031-001811/savehash.real.before.txt` · digest `d3f7cb2fcae2…` — split label `TIMEV1-TC1-r1` (preflight READY 11/11 · mounted-is-copy changed=0) · restore 05:50:47 EXACT `d3f7cb2f` 57 ไฟล์ `ok:true` |
| ผลรอบ | **INCOMPLETE-probe-down** — `TimeV1: patch count หลังติด = 2 (คาด 6) — ถอนทั้งชุด` ซ้ำ **283 ครั้ง** (05:41:49→05:46:35) · `TC1-r1.ndjson` = 0 byte (0 events) ⇒ TC-1…TC-4 = NOT-RUN · `[ERROR]`=0 · `Exception`=0 · TimeV2a เก็บได้ 5 events (observational, gap 43,063 ms = contaminated) |
| **สถานะหลังปิด (2026-10-31 06:1x–06:3x)** | **rollback แล้ว:** DLL = `6a94abb5…` (359,936 B) + PDB = `856d3bed…` (97,128 B) — hash วัดซ้ำตรง rollback point · build TimeV1 เก็บในโฟลเดอร์ evidence (mtime 04:43) · เกมไม่รัน · **ไม่มี tag** (ยกเลิก ไม่นับเป็น stage) · drift pref จากเกมเปิดบนเซฟจริง 06:02–06:07 ⇒ ปิดด้วย rebaseline volatile-only 06:38 (หัวข้อถัดไป) |

## Baseline ที่อนุมัติปัจจุบัน (มติผู้ใช้ 2026-10-31 · rebaseline volatile-only — แทนหัวข้อ 2026-10-30 ด้านบนเป็น anchor ปัจจุบัน) — **superseded 2026-10-31 06:38 โดย `20261031-063808` (หัวข้อถัดไป)**

| รายการ | ค่า |
|---|---|
| เหตุการณ์ | rebaseline volatile-only ตามมติผู้ใช้ 2026-10-31 (offline session — evidence `evidence/AUTO-20261031-001917/`) — **ปิดคำเตือนของ drift รอบที่ 3** ในหัวข้อก่อน ("เซสชัน runtime ถัดไปต้อง `rebaseline volatile-only --against 20261030-060854` ก่อน split") · ไม่มี incident เกมเปิดใหม่ — เซฟคงสถานะเดิมจาก anchor `d3f7cb2fcae2…` ที่ `b814d61` บันทึกไว้ |
| ยืนยันก่อน rebaseline | เซฟรายไฟล์เทียบ `20261030-060854` (read-only ผ่านฟังก์ชันของ `save-split.py` เอง): **56/57 EXACT · CHANGED = `Save/PlayerprefData.dat` ไฟล์เดียว** · missing=0 · added=0 · digest ปัจจุบัน `d3f7cb2fcae2…` ตรง anchor ที่ผู้ใช้ระบุในมติเป๊ะ |
| digest ที่อนุมัติ (anchor ใหม่) | **`d3f7cb2fcae297f0e4b1cfb61630c4d19adb2e848dd6dd2c2ebf2346afe91b0f`** (57 ไฟล์) @ `coop-test/baselines/20261031-001811/savehash.real.before.txt` · `REBASELINE.json` = `volatile_only: true, changed=[PlayerprefData.dat], missing=[], added=[]` · ต่อเนื่องจาก split ทุกครั้งใช้ `--expected-baseline` ไฟล์นี้ตรง ๆ |
| สายโซ่ baseline | `4226730f66ff` → `7332fc93eb98` → `b7f67154cff6` → `58883949…` (`20261025-201545`) → `df8e5a11beed…` (`20261028-071340`) → `7e1e65e52659…` (`20261030-060854`) → **`d3f7cb2fcae2…` (`20261031-001811` — ปัจจุบัน)** · ตัวเก่าคงไว้ไม่ลบ |
| `UserData\MelonPreferences.cfg` | `733d9a0c8fd801cace2c5640421facb2852c646a011754a8660e30149884130c` (วัด 00:19 — คงจาก drift รอบที่ 3 · ML cosmetic rewrite ตามปกติ · **ค่า key semantic ตรงตลอด**: `Sp5T0 = false` · `TimeV2a = false` · `AllowRemoteInput = false`) |
| `net.cfg` / `Mods\LongYinCoop.dll` | `12541f34…` / `6a94abb5…` — ไม่ถูกแตะ (วัด 00:19) |

## Baseline ที่อนุมัติปัจจุบัน (มติผู้ใช้ 2026-10-31 รอบ 2 · rebaseline volatile-only — **anchor ปัจจุบัน**)

| รายการ | ค่า |
|---|---|
| เหตุการณ์ | หลังปิดสาย V1 — ผู้ใช้เปิดเกมบน**เซฟจริง** 06:02:12–~06:07 (ML log `26-10-31_6-2-12.log`) หลัง restore 05:50:47 ⇒ pref drift ⇒ มติผู้ใช้ 2026-10-31: rebaseline volatile-only (รูปแบบเดิม [G-2]/N4-follow-up) · หลักฐาน `evidence/TIME-V1-20261031-044249/V1-close-report.md` §1 |
| ยืนยันก่อน rebaseline | เซฟรายไฟล์เทียบ `20261031-001811` (ด้วย `save-split.py rebaseline` + rehash อิสระของ session): **56/57 EXACT · CHANGED = `Save/PlayerprefData.dat` ไฟล์เดียว** · missing=0 · added=0 |
| digest ที่อนุมัติ (anchor ใหม่) | **`7cd02401960495034fa5e2e284645a8d3f504e5d609214a5786ef64b59452a7d`** (57 ไฟล์) @ `coop-test/baselines/20261031-063808/savehash.real.before.txt` · `REBASELINE.json` = `volatile_only: true, changed=[PlayerprefData.dat], missing=[], added=[]` · split รอบถัดไปใช้ `--expected-baseline` ไฟล์นี้ตรง ๆ |
| สายโซ่ baseline | `4226730f66ff` → `7332fc93eb98` → `b7f67154cff6` → `58883949…` (`20261025-201545`) → `df8e5a11beed…` (`20261028-071340`) → `7e1e65e52659…` (`20261030-060854`) → `d3f7cb2fcae2…` (`20261031-001811`) → **`7cd024019604…` (`20261031-063808` — ปัจจุบัน)** · ตัวเก่าคงไว้ไม่ลบ |
| `UserData\MelonPreferences.cfg` | `TimeV1a=false` · `TimeV2a=false` · `Sp5T0=false` · `AllowRemoteInput=false` (ตรวจด้วยค่า key · ML cosmetic rewrite ทุกครั้งที่ปิดเกมเป็นปกติ) |
| `net.cfg` / `Mods\LongYinCoop.dll` | `12541f34…` (444 B) / `6a94abb5…` — ไม่ถูกแตะ (วัด 06:38–06:4x) |

## กฎ

1. เทียบ SHA-256 ทั้งสองไฟล์ทุกครั้งที่เริ่ม session — **ไม่ตรง = หยุดและรายงาน** (เกมอัปเดต = anchor ใน notes ทั้งหมดเชื่อไม่ได้)
2. ห้าม commit `GameAssembly.dll`, `global-metadata.dat`, `MelonLoader/Il2CppAssemblies/`, `dump.cs` (ดู `.gitignore`)
3. ค่าใน `STAGEPLAN.md §13` (RVA/offset) ใช้สำรวจเท่านั้น โค้ดต้อง re-resolve ผ่าน interop
