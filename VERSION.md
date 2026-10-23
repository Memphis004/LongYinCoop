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

## Baseline ปัจจุบันของ S2a-2 (rebaseline volatile-only · มติผู้ใช้ 2026-10-21)

| รายการ | ค่า |
|---|---|
| digest ที่อนุมัติ | `854cc890430f` = `6dd645d9ef17…` ที่เปลี่ยน `PlayerprefData.dat` ไฟล์เดียว |
| ไฟล์ baseline | `coop-test/baselines/20261021-225856/savehash.real.before.txt` |
| เหตุผล | incident 2026-10-21 22:54:18 — เกมถูกเปิดบนเซฟจริงระหว่างเตรียม S2a-2 (14 s หลัง session-start check ที่ยังพบเกมปิด) · `SaveSlot*` ทั้ง 57 ไฟล์ไม่ถูกเขียน · ผู้ใช้อนุมัติ **ทาง (ก) rebaseline volatile-only** |
| ผลบังคับ | ใช้ `--expected-baseline coop-test/baselines/20261021-225856/savehash.real.before.txt` สำหรับ `split` ของรอบ `S2a2-20261021-2254` (รันแล้ว) |
| หลักฐาน | `evidence/S2a2-20261021-2254/incident-game-open-2254.md`, `rebaseline.txt`, `coop-test/baselines/20261021-225856/REBASELINE.json` |

## Baseline ของรอบสำรวจ Area/UI (2026-10-22 · `area-20261022-0615`)

| รายการ | ค่า |
|---|---|
| baseline ก่อน split | `coop-test/baselines/20261022-061550/savehash.real.before.txt` · digest `b069e56c1228` |
| เหตุผล | เกมถูกเปิดบนเซฟจริง 06:00:22–06:1x **ก่อน** session (ผู้ใช้เป็นคนเปิด) ⇒ เขียน `PlayerprefData.dat` ไฟล์เดียว · `SaveSlot*` 57 ไฟล์ไม่ถูกเขียน |
| baseline ณ ตอนนั้น (หลัง incident 06:39) | `coop-test/baselines/20261022-064205/savehash.real.before.txt` · digest **`1901ad90a2c7`** |
| เหตุผล | incident 2026-10-22 06:39 — **เปิดเกมบนเซฟจริงหลัง restore** เพื่อทดสอบปุ่ม `>>` โดยไม่มี split/preflight · เปลี่ยนเฉพาะ `PlayerprefData.dat` (ตรวจ hash รายไฟล์) · ผู้ใช้ปิดเกมแล้ว · rebaseline volatile-only ตามกฎ |
| baseline ณ ตอนนั้น (ถูกแทนที่ 07:54 — ดูหัวข้อถัดไป) | `coop-test/baselines/20261022-064714/savehash.real.before.txt` · digest **`de7e2af49b23`** |
| เหตุผล | incident 2026-10-22 06:43 (ครั้งที่ 3) — เปิดเกมบนเซฟจริงหลัง restore อีกครั้งเพื่อวัด `>>` ซ้ำ · เปลี่ยนเฉพาะ `PlayerprefData.dat` · `SaveSlot*` 55 ไฟล์ไม่ถูกเขียน · rebaseline volatile-only |
| ผลบังคับ (ประวัติ) | ใช้ `--expected-baseline …/20261022-064714` เฉพาะช่วงก่อน 07:54 · **หลังจากนั้นใช้ `4226730f66ff` (หัวข้อถัดไป)** |

## Baseline ปัจจุบันของ S2a-3 (มติผู้ใช้ 2026-10-22 · หลังคืนเซฟ `explore-20261022-0755`)

| รายการ | ค่า |
|---|---|
| digest ที่อนุมัติ | `4226730f66ffdcf9adb110fd8e26e29ffa8ba40620ccc33b013ef11adfaa46d7` (57 ไฟล์ · 485,795,554 bytes) |
| ไฟล์ baseline | `coop-test/baselines/20261022-075422/savehash.real.before.txt` |
| คำสั่งบังคับ | `python tools\save-split.py split --label <label> --expected-baseline coop-test\baselines\20261022-075422\savehash.real.before.txt` |
| เหตุผล | incident 2026-10-22 07:48 — เกมถูกเปิดบนเซฟจริงหลัง restore ของ `S2cd` · เทียบ `064714` แล้วเปลี่ยนเฉพาะ `PlayerprefData.dat` (`SaveSlot*` ไม่ถูกเขียน) · `REBASELINE.json` = `volatile_only: true`, `changed: ["PlayerprefData.dat"]` |
| การอนุมัติ | **มติผู้ใช้ 2026-10-22** หลัง restore `explore-20261022-0755` ผ่าน post-check 6/6 · `coop-test/baselines/20261022-075422/APPROVAL.md` |
| ยืนยันด้วยการวัด | `Save` 57 ไฟล์ digest `4226730f66ff` **EXACT** · `changed=0 missing=0 added=0` · `leftovers: none` · rehash อิสระ (ordinal sort) ตรงกัน · `evidence/S2a3-20261022-0156/restore-postcheck.md`, `presave-check.md` |
| ผลบังคับ | เป็น expected baseline ของ **S2a-3** (C6) และใช้ตัดสิน E7/X9/C8 · ถ้า digest ไม่ตรง **หยุดและรายงาน** ห้าม rebaseline กลบ |

## Mod build ที่ deploy ล่าสุด (2026-10-24 · `stage-S2a-3`)

| รายการ | ค่า |
|---|---|
| `Mods\LongYinCoop.dll` | `3c9584a4dd851b8567450a0802db0dae171fd9fb1529ecd35922e7eb7b74e625` (build 2026-10-24 01:07, resync fix + `s2.metrics` marker) |
| `Mods\LongYinCoop.pdb` | `9cd7cbd0aabde6c56ca04e62486b03180f6f94b10a339b615552808fbc119020` |
| backup ก่อน deploy | `Mods\LongYinCoop.dll.prev-c51debef` = `c51debef8c08f8a29524b51f20785f9f71ab9ed29d2eee889977c718af94ae96` |
| `UserData\MelonPreferences.cfg` | `269b571c1b1572b7b525cda3e8a27e09b732b8eb31bdfe290269c8ed62a098d1` (ไม่เปลี่ยน) |
| `UserData\LongYinCoop\net.cfg` (A0/พัก) | `dc8e9098490d74c2dfbd6c8a1dffecfbd6befbfeccb2d235dbd0756f4d861b61` (454 B · `enabled=false` · `heartbeatMs=1000`) |
| source ที่ **ยังไม่ build** | `S2FrameMeter` ring 65536 + `coverageMs` (X5 remeter, 2026-10-24) — build/deploy ต้องขออนุมัติก่อน |
| label ที่ใช้ล่าสุด | `S2a3r-20261024-0115` (X4 rerun · restore แล้ว `4226730f66ff` EXACT) · `S2a3x6-20261024-0120` (X6 n=1080 · restore แล้ว `4226730f66ff` EXACT · `leftovers: none`) |
| หลักฐาน | `evidence/S2a3-20261022-143608/{build-deploy-S2a3r-20261024.md,x4-rerun-20261024.md}` + `X6-sample-plan.md` |

## กฎ

1. เทียบ SHA-256 ทั้งสองไฟล์ทุกครั้งที่เริ่ม session — **ไม่ตรง = หยุดและรายงาน** (เกมอัปเดต = anchor ใน notes ทั้งหมดเชื่อไม่ได้)
2. ห้าม commit `GameAssembly.dll`, `global-metadata.dat`, `MelonLoader/Il2CppAssemblies/`, `dump.cs` (ดู `.gitignore`)
3. ค่าใน `STAGEPLAN.md §13` (RVA/offset) ใช้สำรวจเท่านั้น โค้ดต้อง re-resolve ผ่าน interop
