# Gate completed — SP0/SP7 (label `sp0sp7-20261024-225939`)

บันทึก 2026-10-24 ~23:2x · มติผู้ใช้: **(ก) A + (ข) 1 พร้อมเงื่อนไข 6 ข้อ** (ฉบับเต็ม:
`coop-test/baselines/20261024-232024/APPROVAL.md`)

## สิ่งที่ทำแล้ว (ตามเงื่อนไข)

| เงื่อนไข | สิ่งที่ทำ | ผล |
|---|---|---|
| 1. baseline ใหม่ + REBASELINE.json + APPROVAL.md | `coop-test/baselines/20261024-232024/` (57 ไฟล์, digest เต็ม `7332fc93eb98e4751b698ede19e6d2324d6cc57ab93a962c4d041a6e45d424c7`, read-back ตรง, `volatile_only: false`, changed = 5 ไฟล์ตามตารางใน APPROVAL) | ✓ |
| 2. ห้ามแตะ baseline `4226730f66ff` | `20261022-075422` ถูกใช้แค่อ่านเทียบ (assert ก่อนเขียนทุกครั้ง) | ✓ |
| 3. ยืนยัน aside ก่อนเริ่ม | `Save.real-sp0sp7-20261024-225939`: 57 ไฟล์, total digest ตรง, **per-file sha256 55/55 ตรง slot-inventory.txt ก่อน split**, per-slot digest 11/11 ตรง, mtime drift 0 — `real-aside-verification.txt` | ✓ |
| 4. drift expectation ของการทดสอบ | ดูหัวข้อ "กติกาตรวจ drift" ด้านล่าง | บันทึกแล้ว |
| 5. restore path + post-check | ดูหัวข้อ "ความพร้อมของ restore" ด้านล่าง | ตรวจแล้ว |
| 6. PREFLIGHT ก่อนเปิดเกม | `save-split preflight-launch --label sp0sp7-20261024-225939` = **READY ทั้ง 11 ข้อ** (เกมไม่รัน, aside 1 ตัวตาม manifest, mounted-is-copy EXACT changed=0, real-aside-untouched digest `7332fc93eb98`) | ✓ |

## สถานะปัจจุบันของโฟลเดอร์เซฟ

- `Save` = **สำเนาทดสอบ** (57 ไฟล์, digest `7332fc93eb98`, EXACT ทั้ง savehash.real.before และ copy.before ของ label)
- `Save.real-sp0sp7-20261024-225939` = **เซฟจริง** (renamed ไม่ถูกเขียน, 57 ไฟล์)
- `coop-test/sp0sp7-20261024-225939/save` = สำเนาสำรองอีกชั้นของ split
- leftovers = พอดี 1 (`Save.real-sp0sp7-20261024-225939`) — ตามที่ preflight ต้องการ
- sentinel: **ไม่ใช้** (ไม่มี hide/unhide; `sentinel_slot: 8` ใน MANIFEST เป็นค่า default ที่ไม่ถูกเรียกใช้)

## กติกาตรวจ drift ระหว่าง/หลังการทดสอบ (เงื่อนไข 4)

- **คาดหมาย (hash-level)**: สล็อตที่เปลี่ยนได้ = `SaveSlot1/*` (เป้าหมาย) + `PlayerprefData.dat` +
  `steam_autocloud.vdf` (volatile) เท่านั้น
- **mtime churn ที่ไม่ถือเป็นการเปลี่ยน**: ตอนเปิดเกม เกม/Steam เขียนไฟล์สล็อตซ้ำแบบ byte-identical
  (RUNTIME obs 22:08:55) — hash ไม่เปลี่ยน จึงไม่นับ
- สล็อตอื่น (0,2,3,4,5,6,7,8,9,10) hash เปลี่ยนแม้แต่ไฟล์เดียว = **หยุดและรายงานทันที ห้าม rebaseline กลบ**
- หมายเหตุเครื่องมือ: `save-split verify` ตามตัวจะ FAIL เมื่อ `SaveSlot1` เปลี่ยน (slot_drift check
  ไม่มี whitelist) — การตรวจปลายทางของรอบนี้จะใช้ diff แบบ whitelist `SaveSlot1` + volatile tops
  พร้อมบันทึกหลักฐานแยก

## ความพร้อมของ restore (เงื่อนไข 5)

- preconditions ของ `restore --label sp0sp7-20261024-225939` ครบ: real dir อยู่ + digest ตรง manifest ✓,
  ไม่มี hidden sentinel ✓, copy drift ปัจจุบัน = 0 ✓ (restore อนุญาต drift ระดับ SaveSlot + volatile ตามดีไซน์)
- restore จะ rename copy → `coop-test/sp0sp7-20261024-225939/Save.used-<ts>` (เก็บข้อมูลทดลองไว้)
  แล้ว rename real กลับ พร้อมตรวจ restored vs `savehash.real.before.txt` (= สถานะ `7332fc93eb98`) —
  ตรงกับ post-check ที่มติกำหนด; ปิดท้ายด้วย `status` ต้องได้ 57 ไฟล์ `7332fc93eb98` + `leftovers: none`
- RUNTIME precedent: restore ใช้สำเร็จหลายรอบใน S2a-2/S2a-3

## ขั้นตอนถัดไป (ให้ผู้ใช้ตรวจก่อนเปิดเกม)

1. ผู้ใช้ยืนยันรายงานนี้ (label / baseline / สล็อต / กติกา drift)
2. **ผู้ใช้เปิดเกมเอง** ผ่าน Steam — เกมจะเห็น `Save` = สำเนาทดสอบ
3. พอถึงหน้า Title แจ้งผู้ช่วย → attach frida (read-only) ก่อน แล้วค่อยไปหน้าสร้างโลก/ตัวละคร
4. §3A: แคปหน้าตั้งค่าสร้างโลก/ตัวละครทุกแท็บ + จดค่าที่เลือก
5. จุดที่เกมให้เลือกสล็อตของโลกใหม่ → **`SaveSlot1` เท่านั้น** ห้ามแตะสล็อตอื่น
6. รอบแรก (§3B): สร้างเสร็จแจ้งผู้ช่วยเก็บ snapshot (read-only) → §3C: ออกกลับ Title **ไม่เซฟ**

ห้าม: เปิด IPC client อื่น, แตะสล็อตอื่นในเกม, restore/ย้ายโฟลเดอร์ `Save*` เองกลางรอบ
