# Incident — เกมถูกเปิดบนเซฟจริงอีกครั้ง (2026-10-20 ~15:56) ก่อน deploy W1

ตรวจพบตอนเตรียม deploy (15:59:21) · **ไม่มีคำสั่งของ mod ถูกเรียกในเหตุการณ์นี้** (DLL ที่โหลดอยู่ = build เดิม `e9063ed9…` ซึ่งไม่มี W1)

## 1. สิ่งที่วัดได้ [RUNTIME]

| ข้อ | ค่า |
|---|---|
| process | `LongYinLiZhiZhuan.exe` pid **5804** รันอยู่ (คนละตัวกับ 27916 ที่ปิดไปตอน 15:45) |
| `save-split status` | `game: RUNNING` · `Save` **57 ไฟล์ digest `db9512bd5b62`** (ไม่ใช่ `80e8d7364047`) · `leftovers: none` |
| ไฟล์ที่เปลี่ยน | **`PlayerprefData.dat` เท่านั้น** — sha256 `584a59f0…`, mtime **15:56** (เดิม `a047d9ec…`/baseline ของ stage) |
| `SaveSlot*` | **55 ไฟล์ ไม่เปลี่ยนเลย** (rehash อิสระเทียบ `coop-test/S1.5-20261020-144353/savehash.real.before.txt`: changed มีเฉพาะ pref) |
| โฟลเดอร์ค้าง | ไม่มี `Save.real-*` / `Save.used-*` — เพราะ `Save` คือของจริงที่ restore ไว้ |

## 2. การวินิจฉัย

- หลัง `restore` (15:46) `Save` = ของจริง; เกมถูกเปิดใหม่โดยไม่มี `split` ก่อน → เกมเขียน `PlayerprefData.dat` ของจริงเอง (พฤติกรรมเดียวกับ incident 14:24/14:28 ที่บันทึกไว้ใน `evidence/S1.5-20261020-141903/`)
- ความเสียหาย: **จำกัดที่ pref** — ข้อมูลเซฟจริง (`SaveSlot*`) ไม่ถูกแตะ; ไม่มี mod code path ที่เขียน `Save/`
- ผลต่อขั้นตอน S1.5-c: `deploy` และ `split` ทำไม่ได้จนกว่าเกมจะปิด (สคริปต์ปฏิเสธเองเมื่อเกมรัน)

## 2.1 การจัดการเมื่อเกมปิด (ทำแล้ว 16:08–16:09)

| ขั้น | ผล | หลักฐาน |
|---|---|---|
| ตรวจ `SaveSlot*` ซ้ำก่อนทำอะไร | **55/55 ไม่เปลี่ยน** (`changed=['PlayerprefData.dat']`, missing 0, added 0) | rehash อิสระ 16:08:28 |
| `rebaseline` (volatile-only) | **OK** → `coop-test/baselines/20261020-160840/` · `volatile_only=true` · `changed=[PlayerprefData.dat]` | `coop-test/baselines/20261020-160840/REBASELINE.json` |
| pref hash ก่อน / หลัง | `a047d9ec…be68a5` (baseline ของ stage) → **`584a59f0…1efdbb`** (mtime 15:56) | `grep PlayerprefData` ใน baseline ทั้งสองชุด |
| digest รวมของเซฟจริงหลัง rebaseline | `db9512bd5b62c8577caba93cc190800f016b0c35714947586ba565a34199e4ff` | `REBASELINE.json` |
| deploy | backup `Mods/LongYinCoop.dll.prev-e9063ed9980d` (`e9063ed9…`) → deploy `d339fcad…` (ตรง build) | `logs/deploy.log` |
| `split` | label **`S1.5c-20261020-160858`** · real → `Save.real-S1.5c-20261020-160858` · copy 57 ไฟล์ (0/0/0 vs real) | `logs/split.log` |
| sentinel | `.hidden-SaveSlot8` (5 ไฟล์) | `logs/sentinel-hide.log` |
| `verify` ก่อนเล่น | **PASS** — copy 0/0/0 (นับ sentinel) · real 0/0/0 · `mtime/size drift 0` | `logs/verify-pre.log` |

> หมายเหตุเครื่องมือ: `split` เลือก baseline ด้วย `newest_baseline()` ซึ่ง**ไม่เห็น** ไฟล์ใต้ `coop-test/baselines/` → รอบแรกถูก REFUSED; แก้ด้วย `--expected-baseline coop-test/baselines/20261020-160840/savehash.real.before.txt` (บันทึกเป็นข้อจำกัดของ tool: ควรอ่าน baselines/ ด้วย — ยังไม่แก้)

## 3. สิ่งที่ยังไม่ทำ (รอผู้ใช้ปิดเกม)

1. ปิด `LongYinLiZhiZhuan.exe` pid 5804 (ผู้ใช้ต้องปิดเอง — ห้ามฆ่า process ที่ agent ไม่ได้เปิด)
2. ตัดสินใจเรื่อง pref drift: `rebaseline` (volatile-only, แบบ S1.5-a) หรือคืนจาก `coop-test/…/save/`
3. สำรอง DLL เดิม → `Mods/LongYinCoop.dll.prev-e9063ed9980d` แล้ว deploy build ใหม่ `82e25257ce1b…` (สร้างเสร็จแล้ว ยังไม่ deploy)
4. `split --label S1.5c-<ts>` + `sentinel hide --slot 8` + `verify` ตามโปรโตคอล
