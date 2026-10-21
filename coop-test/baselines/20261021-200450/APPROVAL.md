# APPROVAL — ใช้ baseline นี้เป็นเกณฑ์เซฟจริง (2026-10-21)

ผู้ใช้อนุมัติในการเริ่ม session ก่อน S2 (task: "เริ่ม session ใหม่ → ตรวจสถานะหลังปิด S1.5g → เสนอแผน S2")

## สิ่งที่อนุมัติ

| รายการ | ค่า |
|---|---|
| baseline | `coop-test/baselines/20261021-200450/savehash.real.before.txt` |
| digest | `6dd645d9ef17fb626c3b680d8c12b8bf5297d5fbce532b9e7bc4286d343ef19e` |
| จำนวนไฟล์ | 57 |
| canonical เดิม | `2b0548fe464b` — ต่างเฉพาะ `PlayerprefData.dat` |

## ขอบเขต

- ใช้เป็น expected baseline ของทุก `split` ตั้งแต่ S2 เป็นต้นไป
- ทุก `split` ต้องระบุ `--expected-baseline` เป็น path นี้ตรง ๆ (ไม่พึ่ง `newest_baseline()` default ซึ่งไม่สแกน `baselines/`)
- `SaveSlot*` ยังต้องไม่เปลี่ยนเลยแม้แต่ไฟล์เดียว (§1.2 ข้อ 6) · `PlayerprefData.dat`/`steam_autocloud.vdf` = volatile
- การอนุมัตินี้ **ไม่ได้** เปลี่ยนสิทธิ์การเขียนใด ๆ ใน §1.1

## หลักฐานประกอบ

- `REBASELINE.json` (ในโฟลเดอร์นี้): `volatile_only: true`, `changed: ["PlayerprefData.dat"]`
- `evidence/S1.5gB-20261021-200449/save-verification.md`
- `evidence/S1.5gB-20261021-200449/incident-real-save-mounted-1949.md`
- `save-split.py status` (2026-10-21 20:50): `Save files=57 digest=6dd645d9ef17`, `leftovers: none`
