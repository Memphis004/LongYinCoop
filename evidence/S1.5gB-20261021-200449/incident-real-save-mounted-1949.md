# incident #2 — เกมรันบนเซฟจริงก่อน split ของรอบ S1.5gB (2026-10-21 19:49–20:05)

## 1. ข้อเท็จจริง [RUNTIME]

| เวลา | เหตุการณ์ | หลักฐาน |
|---|---|---|
| 19:09:52 | จบ S1.5g รอบแรก · `restore` → `Save` = ของจริง 57 ไฟล์ digest `046ceb7a4810` | `evidence/S1.5g-20261021-160331/logs/restore.log` |
| **19:49:17** | เกมเริ่มทำงาน **pid 37904** (ผู้ใช้เปิดเพื่อเริ่ม Phase B ใหม่ — ยังไม่ได้ split) | `Get-Process` |
| 19:49–20:05 | เกมรันบน **เซฟจริง** ~15 นาที (ผู้ใช้เล่นจริง) · sampler ไม่ได้เปิด ⇒ **ไม่มีข้อมูลวัด** | — |
| 19:52:0x | ตรวจกลางคัน: digest `f8afbae3e981` = ต่างจาก baseline ที่ `PlayerprefData.dat` ไฟล์เดียว | `save-split.py verify --label S1.5g-…` |
| 20:04:5x | ผมตรวจอีกครั้ง: digest `6dd645d9ef17`, `changed[playerpref]=1`, **`slot_drift = 0`** | `save-split.py verify` |
| 20:05 | ผู้ใช้ปิดเกมแบบไม่ Save → process หาย | `save-split.py status` |
| 20:05 | ตรวจหลังปิด: 57 ไฟล์ digest `6dd645d9ef17` · เทียบ baseline: changed = `PlayerprefData.dat` **ไฟล์เดียว** · `SaveSlot*` ไม่เปลี่ยน | `verify-final.py` (ผล: `changed: PlayerprefData.dat`) |

## 2. การประเมินความเสียหาย

- **`SaveSlot0`–`SaveSlot10` ไม่ถูกเขียนเลย** ตลอด 15 นาทีที่เล่น (diff เทียบ baseline: `changed=1` = pref เท่านั้น)
- ไฟล์ที่ต่างคือ `PlayerprefData.dat` (volatile/prefs) — เกมเขียนตอนเปิด/ตั้งค่า ไม่ใช่ข้อมูลการเล่น; ตัวเลข `turnIndex`/ความคืบหน้าในศึก **ไม่ได้ถูกบันทึกลงสล็อต** เพราะไม่ได้กด Save และเกมไม่มี autosave ตอนออก (ยืนยันซ้ำจากรอบ 19:09 ซึ่ง `slot_drift=0` เช่นกัน)
- ⇒ **ข้อมูลเซฟจริงไม่เสียหาย** แต่ **การเล่นช่วงนั้นใช้อ้างอิงเป็นหลักฐานไม่ได้** (ไม่มี sampler + อยู่บนเซฟจริง) ⇒ ไม่นับเป็น PASS/FAIL

## 3. การตัดสินใจ (ทำแล้ว)

1. หยุด: ไม่เล่นต่อบนเซฟจริง ไม่ split ขณะเกมรัน (tool บล็อกอยู่แล้ว: `require_game_closed`)
2. ให้ปิดเกมแบบไม่ Save → ยืนยัน process จบ
3. `rebaseline` เฉพาะ volatile drift: `coop-test/baselines/20261021-200450/` (reason บันทึกใน `REBASELINE.json`) → baseline ใหม่ digest `6dd645d9ef17` · ผล: `changed=1 (volatile only)`
4. `split --label S1.5gB-20261021-200449 --expected-baseline coop-test\baselines\20261021-200450\savehash.real.before.txt` → สำเนา mount, ของจริงเก็บเป็น `Save.real-S1.5gB-20261021-200449` (57/57 digest `6dd645d9ef17`)
5. ให้ผู้ใช้เปิดเกมใหม่ → ครั้งนี้ **เปิดเกมหลัง split** → ทุกอย่างรันบนสำเนา (sampler เริ่มก่อนผู้ใช้เปิดเกม)

## 4. บทเรียน/ข้อควรระวังสำหรับ stage ถัดไป

- หลัง `restore` ของจริง แล้ว **ผู้ใช้ต้องไม่เปิดเกมเองก่อนที่ผมจะ split** — ลำดับที่ปลอดภัยคือ: split → เปิด sampler → *แล้ว* ผู้ใช้เปิดเกม
- เกมเขียน `PlayerprefData.dat` ทุกครั้งที่เปิด/ปิด ⇒ pref ที่ต่างจาก baseline เป็นเรื่องปกติ ให้ใช้ `rebaseline` (volatile-only) และบันทึกทุกครั้ง (มี 2 ครั้ง: 16:11, 20:04)
- `split` ต้องส่ง `--expected-baseline` เมื่อ baseline ใหม่ถูกเก็บใต้ `coop-test/baselines/<ts>/` เพราะ `newest_baseline()` ของ tool ค้นแค่ระดับ `coop-test/*/` (quirk ไม่ใช่บั๊กใหม่)
