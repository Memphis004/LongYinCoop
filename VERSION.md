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

## กฎ

1. เทียบ SHA-256 ทั้งสองไฟล์ทุกครั้งที่เริ่ม session — **ไม่ตรง = หยุดและรายงาน** (เกมอัปเดต = anchor ใน notes ทั้งหมดเชื่อไม่ได้)
2. ห้าม commit `GameAssembly.dll`, `global-metadata.dat`, `MelonLoader/Il2CppAssemblies/`, `dump.cs` (ดู `.gitignore`)
3. ค่าใน `STAGEPLAN.md §13` (RVA/offset) ใช้สำรวจเท่านั้น โค้ดต้อง re-resolve ผ่าน interop
