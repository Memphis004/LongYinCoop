# APPROVAL — ใช้ baseline นี้เป็นเกณฑ์เซฟจริง (2026-10-31)

> **บันทึกตามจริง (เขียนใน session รอบ 6 — งานบ้าน ข้อ 1 ตาม prompt รอบ 6):** การอนุมัติ rebaseline นี้เกิดใน **รอบ 5 (2026-10-31)** และการรัน rebaseline ทำโดย **เซสชัน V1 (commit `6fbed5b`)** — ไม่ใช่ session ที่เขียนไฟล์นี้ · session นี้เป็นเพียงผู้บันทึก APPROVAL.md ที่ขาดจาก baseline ก่อนหน้า (รายงานค้างจาก session งาน 0–4 รอบ 5 ฉบับสั่งงาน)

## สิ่งที่อนุมัติ (รอบ 5 · ผู้ใช้ 2026-10-31)

| รายการ | ค่า |
|---|---|
| baseline | `coop-test/baselines/20261031-063808/savehash.real.before.txt` |
| digest | `7cd02401960495034fa5e2e284645a8d3f504e5d609214a5786ef64b59452a7d` |
| จำนวนไฟล์ | 57 |
| baseline ก่อนหน้าในสายโซ่ | `d3f7cb2fcae2…` @ `coop-test/baselines/20261031-001811` — ต่างเฉพาะ `PlayerprefData.dat` (volatile — เกมถูกเปิดบนเซฟจริง 06:02 หลัง restore ของ V1 TC1-r1) |

## ขอบเขต

- rebaseline แบบ **volatile-only** — ไฟล์ที่เปลี่ยน = `PlayerprefData.dat` **ไฟล์เดียว** · `SaveSlot*` 55 ไฟล์ **EXACT** · missing/added = 0
- ใช้เป็น expected baseline ของทุก `split` หลังจากนี้ — ทุก `split` ต้องระบุ `--expected-baseline` เป็น path นี้ตรง ๆ
- `PlayerprefData.dat`/`steam_autocloud.vdf` = volatile (drift เป็นเรื่องปกติ ต้อง rebaseline และบันทึกทุกครั้ง)
- การอนุมัตินี้ **ไม่ได้** เปลี่ยนสิทธิ์การเขียนใด ๆ ใน §1.1 ของ STAGEPLAN

## หลักฐานประกอบ

- `REBASELINE.json` (ในโฟลเดอร์นี้): `--against 20261031-001811` · `volatile_only: true` · `changed: ["PlayerprefData.dat"]`
- `evidence/TIME-V1-20261031-044249/V1-close-report.md` (สาย V1 ปิด — CLOSED-CANCELLED · drift เกิดจากเกมเปิดบนเซฟจริง 06:02 ตาม timeline ของรายงาน)
- การตรวจซ้ำโดย session รอบ 6 (งาน 0, 2026-10-31): rehash อิสระ 57/57 ไฟล์ = **EXACT match** เทียบ `savehash.real.before.txt` ทุกไฟล์ (diff 0) · `save-split status`: `Save files=57 digest=7cd024019604` + `leftovers: none` · VERSION.md บันทึก baseline นี้แล้ว (หัวข้อ baseline ปัจจุบัน)
