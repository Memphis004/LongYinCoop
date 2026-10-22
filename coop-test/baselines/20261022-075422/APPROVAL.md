# APPROVAL — ใช้ baseline นี้เป็นเกณฑ์เซฟจริงของ S2a-3 (2026-10-22)

ผู้ใช้อนุมัติในรอบ S2a-3 (ข้อความ: "อนุมัติให้ใช้ baseline `4226730f66ff` (`coop-test/baselines/20261022-075422`) เป็น baseline สำหรับ S2a-3 ตามหลักฐาน restore-postcheck ที่ผ่าน 6/6")

## สิ่งที่อนุมัติ

| รายการ | ค่า |
|---|---|
| baseline | `coop-test/baselines/20261022-075422/savehash.real.before.txt` |
| digest | `4226730f66ffdcf9adb110fd8e26e29ffa8ba40620ccc33b013ef11adfaa46d7` |
| จำนวนไฟล์ | 57 (485,795,554 bytes) |
| ก่อนหน้า | `de7e2af49b23` (`baselines/20261022-064714`) — ต่างเฉพาะ `PlayerprefData.dat` |
| canonical ของ S1.5 | `2b0548fe464b` — ไม่ใช่เกณฑ์ผ่าน/ไม่ผ่านอีกต่อไป |

## ขอบเขต

- ใช้เป็น **expected baseline ของ S2a-3** (ขั้น C6) และใช้ตัดสิน **E7 / X9 / C8**
- ทุก `split` ต้องระบุ `--expected-baseline coop-test\baselines\20261022-075422\savehash.real.before.txt` ตรง ๆ (ไม่พึ่ง default ของ tool)
- `SaveSlot*` ยังต้องไม่เปลี่ยนเลยแม้แต่ไฟล์เดียว · `PlayerprefData.dat` / `steam_autocloud.vdf` = volatile
- ถ้า digest ไม่ตรง **หยุดและรายงาน** — ห้าม rebaseline กลบปัญหา
- การอนุมัตินี้ **ไม่ได้** เปลี่ยนสิทธิ์การเขียนใด ๆ ใน §1.1 ของ STAGEPLAN

## หลักฐานประกอบ

- `REBASELINE.json` (ในโฟลเดอร์นี้): `volatile_only: true`, `changed: ["PlayerprefData.dat"]`, `against: …/20261022-064714`, เหตุผล = incident 2026-10-22 07:48 (เกมถูกเปิดบนเซฟจริงหลัง restore ของ `S2cd`)
- `evidence/S2a3-20261022-0156/restore-postcheck.md` — post-check 6/6 หลังคืนเซฟ `explore-20261022-0755`: `Save` 57 ไฟล์ `4226730f66ff` EXACT (`changed=0 missing=0 added=0`), `leftovers: none`, rehash อิสระ (ordinal sort) ตรง, `restore-20261022-142313.json` = `ok: true`
- `evidence/S2a3-20261022-0156/presave-check.md` — ตรวจก่อนคืนเซฟ (real folder = baseline เป๊ะ, สำเนา drift `PlayerprefData.dat` ไฟล์เดียว)
- `evidence/S2a3-20261022-0156/restore-explore.txt` — เอาต์พุตจริงของ `save-split restore`
- `python tools\save-split.py status` (2026-10-22 14:24): `Save files=57 digest=4226730f66ff`, `leftovers: none`
