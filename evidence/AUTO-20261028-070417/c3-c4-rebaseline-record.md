# C3/C4 — rebaseline volatile-only (มติ [G-2] ผู้ใช้ 2026-10-28) · divergence record

session: `AUTO-20261028-070417` · ทำเมื่อ 2026-10-28 07:13

## คำสั่งที่รัน (กลไกของ tool เท่านั้น — ไม่เขียนไฟล์ baseline มือ)

```
python tools/save-split.py rebaseline \
  --against "coop-test/baselines/20261025-201545/savehash.real.before.txt" \
  --reason "G-2 (มติผู้ใช้ 2026-10-28, session AUTO-20261028-070417): ..."
```

ผล: `OK rebaseline: coop-test/baselines/20261028-071340/savehash.real.before.txt · changed=1 (volatile only) missing=0 added=0` · exit code 0

- baseline เก่า `coop-test/baselines/20261025-201545/` **ไม่ถูกเขียน/แก้** (tool สร้างโฟลเดอร์ใหม่เสมอ — ตรวจโค้ด `cmd_rebaseline` save-split.py:596–628 ก่อนรัน)
- `REBASELINE.json`: `volatile_only: true`, `changed: ["PlayerprefData.dat"]`, `files: 57`, digest `df8e5a11beed6e62cc18d40da9ae0f2d0d5fbfdc56640b4196ece95df1ed4920`

## C4 — การยืนยัน

1. `save-split status` หลัง rebaseline: `Save` 57 ไฟล์ digest `df8e5a11beed` · `leftovers: none` · game not running
2. rehash อิสระ (Python) รายไฟล์: **Save ปัจจุบัน EXACT baseline ใหม่ 57/57 (diffs: NONE)**
3. baseline ใหม่ต่างจาก baseline เก่า (`58883949…` @ `20261025-201545`) **เฉพาะ `Save/PlayerprefData.dat`** — hash เก่า `50e1612b968d…` → ใหม่ `efd4ac5b5dbb…`

## บันทึก divergence (ตามข้อ C4 ของคำสั่ง)

| รายการ | ค่า |
|---|---|
| baseline ใหม่ (ปัจจุบัน) | digest `df8e5a11beed6e62cc18d40da9ae0f2d0d5fbfdc56640b4196ece95df1ed4920` · `coop-test/baselines/20261028-071340/savehash.real.before.txt` |
| baseline เก่า (คงไว้เป็นประวัติ ห้ามลบ) | digest `588839490e5b418ae6e20172ea7af21916c9afa3a935c99525984fc9e16a0485` · `coop-test/baselines/20261025-201545/savehash.real.before.txt` |
| ความต่าง | `Save/PlayerprefData.dat` ไฟล์เดียว (`50e1612b…` → `efd4ac5b…`) — volatile/pref drift จากการเปิดเกมของผู้ใช้ 2026-10-27 ×3 (ML logs `00:47`, `02:16`, `02:26`) · `SaveSlot*` 55 ไฟล์ EXACT ทั้งสอง baseline |
| เหตุผล | มติ [G-2] ผู้ใช้ 2026-10-28 — rebaseline volatile-only ตามรูปแบบเดิมที่อนุมัติซ้ำหลายครั้ง · ปิด DEVIATION-1 ของ session `AUTO-20261028-023904` |
| ไฟล์เซฟทั้งชุด | **ไม่ถูกแตะ** — rebaseline เป็นการบันทึกสถานะเท่านั้น (ยืนยัน digest ก่อน/หลัง = `df8e5a11beed` ทั้งคู่) |
| ผลบังคับ | `split` ถัดไป (รวม GO-3 ขั้น 4) ใช้ `--expected-baseline coop-test/baselines/20261028-071340/savehash.real.before.txt` — ถ้า digest ไม่ตรง หยุดและรายงาน ห้าม rebaseline กลบ (rebaseline เพิ่มต้องอนุมัติแยก) |
