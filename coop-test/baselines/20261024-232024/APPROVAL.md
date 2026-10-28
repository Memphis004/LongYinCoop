# APPROVAL — baseline `20261024-232024` (รอบ SP0/SP7)

- **วันเวลามติ**: 2026-10-24 ~23:05–23:20 (session SP0/SP7 gate)
- **ผู้อนุมัติ**: ผู้ใช้ (เจ้าของเซฟ) — ตอบใน session: "อนุมัติมติ (ก) A + (ข) 1 โดยมีเงื่อนไขดังนี้ …"
- **สิ่งที่อนุมัติ**:
  - (ก) ทาง A — สร้าง **baseline ใหม่** จาก `Save` ปัจจุบัน digest `7332fc93eb98`
    (เต็ม: `7332fc93eb98e4751b698ede19e6d2324d6cc57ab93a962c4d041a6e45d424c7`, 57 ไฟล์)
    เพื่อใช้กับ `save-split split --label sp0sp7-20261024-225939` ของการสำรวจ SP0/SP7
  - (ข) ทาง 1 — ใช้ **`SaveSlot1` เป็นสล็อตเป้าหมายเฉพาะบนสำเนาทดสอบ** (เซฟจริงถูกแยกเป็น
    `Save.real-sp0sp7-20261024-225939` พร้อม hash รายไฟล์/รายสล็อต)
- **เหตุผล**: baseline เดิม `4226730f66ff` (`coop-test/baselines/20261022-075422`) ล้าสมัยจาก
  การเซฟของผู้ใช้เอง (SaveSlot8 เวลา 02:15:47) + pref drift จาก session เปิดเกม 22:08 ของวันเดียวกัน
  — drift ทั้ง 5 ไฟล์ตรงกับที่บันทึกไว้ใน AGENTS/`evidence/sp0sp7-20261024-225939/session-start.md`
  ทุกไฟล์ ไม่มีไฟล์อื่นเปลี่ยน
- **ไฟล์ที่ต่างจาก baseline เดิม (5)**:
  | ไฟล์ | ประเภท | mtime |
  |---|---|---|
  | `PlayerprefData.dat` | volatile | 2026-10-24 22:20:15 |
  | `SaveSlot8/Hero` | NON-volatile (ผู้ใช้เซฟเอง) | 2026-10-24 02:15:47 |
  | `SaveSlot8/Info` | NON-volatile | 2026-10-24 02:15:47 |
  | `SaveSlot8/Save` | NON-volatile | 2026-10-24 02:15:46 |
  | `SaveSlot8/TempHero` | NON-volatile | 2026-10-24 02:15:47 |

## เงื่อนไขจากมติ (บันทึกครบทั้ง 6 ข้อ)

1. baseline ใหม่ต้องอยู่โฟลเดอร์ที่ไม่ซ้ำ พร้อม `REBASELINE.json` (`volatile_only: false`) และ
   `APPROVAL.md` ไฟล์นี้ — ✓ ทำแล้ว (read-back ตรง, changed = 5 ไฟล์ตามตาราง)
2. **ห้ามเขียนทับ/ลบ** baseline ประวัติ `4226730f66ff` ของ S2a-3 — ไฟล์เดิมถูกใช้แค่ "อ่านเทียบ"
3. `SaveSlot1` ใช้ได้ **เฉพาะบนสำเนาทดสอบ**; ต้องยืนยันก่อนเริ่มว่า `Save.real-<label>` ครบ 57 ไฟล์
   และมี hash รายสล็อตครบ — เป็นขั้นบังคับหลัง split
4. หลังสร้างเซฟทดลอง ตรวจ hash ทุกสล็อต — **คาดหมาย: เปลี่ยนเฉพาะ `SaveSlot1` + volatile tops**
   (`PlayerprefData.dat`, `steam_autocloud.vdf`) — สล็อตอื่นเปลี่ยน = หยุดรายงาน, **ห้าม rebaseline กลบ**
5. ทำตาม save-split/preflight runbook; การคืนเซฟถือว่าผ่านเมื่อ post-check = digest `7332fc93eb98`
   ครบ 57 ไฟล์ และ `leftovers: none`
6. ห้ามเปิดเกมเอง / กด UI แทนผู้ใช้ / เริ่มสำรวจ SP0/SP7 ก่อนรายงาน `PREFLIGHT READY`
   (label, baseline, สล็อต, ขั้นตอนถัดไป) ให้ผู้ใช้ตรวจอีกครั้ง

## ขอบเขต

baseline นี้ใช้ **เฉพาะรอบ `sp0sp7-20261024-225939`** — ไม่ใช่การเปลี่ยน canonical baseline ของ
S2a-3 และไม่มีผลย้อนหลังกับ stage ก่อนหน้า
