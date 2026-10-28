# TRIGGERS — วลีสั่งงานที่เก็บไว้ใช้ทีหลัง (รอบนี้ห้ามรัน)

> สร้าง 2026-10-28 โดย session `AUTO-20261028-023904` ตามคำสั่งผู้ใช้ (ข้อ 6) · อ้างอิงจาก AGENTS.md เท่านั้น
> · **ห้ามรันในรอบที่สร้างไฟล์นี้**

## วลี: `เริ่ม GO-3 session`

= อนุมัติ [N1] (deploy/swap ไฟล์ใน `Mods\`) และ [N2] (save-split split/restore) **เฉพาะ S2a-4 GO-3 ตาม
`evidence/S2a4-GO3-20261028-030025/RUNBOOK-G3.md`** — ลำดับ 9 ขั้น:
1. session-start ใหม่ทั้งหมด (ข้อ 2 ของคำสั่ง session-start รันครบ อ่านอย่างเดียว)
2. ตรวจ artifact ใน `evidence/S2a4-GO3-<ts>/` ยังตรง provenance (hash) และ HEAD ยังเป็น commit เดิม
   (`src/` ไม่เปลี่ยน) — ไม่ตรง = หยุด
3. สำรอง `Mods\LongYinCoop.dll` + `.pdb` เป็น `.prev-<8 หลักของ hash เดิม>` (ตรวจชื่อว่างก่อน — ห้าม
   เขียนทับ backup เดิม) แล้ว deploy + ตรวจ hash หลัง deploy ตรง artifact
4. ตั้ง net.cfg เฉพาะ key ที่แผน S2a-4 กำหนดสำหรับ loopback + overlay (`enabled/role/transport/bind/port/
   overlay=true`) จด sha256 ก่อน/หลัง (ห้าม log secret)
5. save-split split พร้อม `--expected-baseline` ชี้ไฟล์ baseline ปัจจุบันตาม VERSION.md ตรง ๆ (ณ วันเขียน
   RUNBOOK = `coop-test/baselines/20261025-201545/savehash.real.before.txt` — ถ้ามีมติ rebaseline ใหม่
   ให้ชี้ไฟล์ที่อนุมัติล่าสุด) + preflight — **ถ้า split REFUSE แม้เพราะไฟล์ pref = หยุดรายงาน ห้าม rebaseline เอง**
6. บอกผู้ใช้สั้น ๆ ว่าเปิดเกมได้ แล้วรอ — **ห้ามเปิดเกมเอง**
7. หลังผู้ใช้แจ้ง: ยืนยัน log ใหม่ + DLL ที่โหลดตรง artifact แล้ววัด **G0 → E6 → E1/E2/E3/E4/E7/E8** ตามเกณฑ์
   `S2a-4-plan.md` rev4 §11 ทีละขั้น พร้อมบอกผู้ใช้ว่าต้องทำอะไรต่อในแต่ละขั้น (เข้าศึก/ปล่อยค้าง WaitPlayerInput
   ช่วง E2/E4, เล่น 1 เทิร์นช่วง E3, จบศึก, ปิดเกม)
8. หลังผู้ใช้ปิดเกม: ตรวจ digest → restore เซฟ → ยืนยัน EXACT + `leftovers: none` → คืน net.cfg (ตรวจ hash)
   → **ถามผู้ใช้ว่าจะ rollback DLL กลับ `.prev` หรือคงไว้ — ห้ามตัดสินเอง**
9. เขียนรายงานผลทีละ E (PASS / PASS-with-limitation / FAIL / INCONCLUSIVE / NOT-RUN พร้อมเหตุผล) —
   ไม่ปรับเกณฑ์ ไม่ประกาศปิด stage เอง ไม่ tag

**prerequisite ก่อนวลีนี้ใช้ได้จริง:** (1) Release artifact ครบใน `provenance.md` (ต้องแก้ build break ตามมติ
G-1 ก่อน — ดู APPROVAL-REQUESTS แพ็กเกจ 1) (2) มติ baseline G-2 (ปัจจุบัน digest `df8e5a11beed` ≠
baseline — split จะ REFUSE)

## วลีอื่น ๆ (ความหมายที่บันทึกไว้)

- `เตรียม V2-A ให้พร้อมรัน` · `เตรียม SP5-T0 ให้พร้อมรัน` · `เตรียม V1 ให้พร้อมรัน` =
  **งาน offline เท่านั้น**: เขียนโค้ด probe/sampler + เทสต์ offline + compile-only build + provenance —
  **ยังไม่ deploy / ไม่เปิดเกม / ไม่ split** · ต้องมีข้อความผู้ใช้ที่ระบุ**มติที่จำเป็นครบ**ตามแผนต้นทาง
  (V2-A: Gate 0 + D9 + Gate 5a + อนุมัติเขียน sampler/deploy · SP5-T0: §4 ข้อ 1–7 แยกขั้น A/B ·
  V1: D1 + D2 + Gate 3) — **ถ้ามติไม่ครบ agent ต้องปฏิเสธและแสดงรายการมติที่ขาด**
- `เริ่ม V2-A session` / `เริ่ม SP5-T0 session` / `เริ่ม V1 session` = รันจริงบนสำเนาเซฟ (ต้องผ่าน preflight/
  split ตามแผนนั้น ๆ + ผู้ใช้เปิดเกมเอง) — ใช้ได้เฉพาะหลัง "เตรียม …" ของสายงานนั้นเสร็จและมติครบ
- `ร่าง S3 rev2` = เขียน/ปรับ `S3-plan-draft-rev2.md` เอกสารอย่างเดียว (ไม่ implement) — รอบนี้ทำไปแล้ว
  (Phase D) ถ้าสั่งซ้ำให้ทำเป็นการทบทวนจากสถานะใหม่

## กติกาคาดโทษ (บังคับทุกวลี)

- **4 สายงาน (S2a-4 GO-3, V1/V2, SP5-T0, S3) ห้ามอยู่ในเซสชันเกมเดียวกัน** — แต่ละเซสชันเกมต้องปิด
  ด้วย restore + verify ครบก่อนเปิดสายงานอื่น
- วลีใดมาพร้อมมติไม่ครบ = ปฏิเสธ + แสดงรายการมติที่ขาด (ห้ามเดา ห้ามเติมเอง)
- ทุกเซสชันเกม: ห้ามเปิดเกมเอง, split ต้อง `--expected-baseline` ชี้ไฟล์ที่อนุมัติตาม VERSION.md ตรง ๆ,
  restore ต้อง EXACT + `leftovers: none`, คืน net.cfg/pref ทุกครั้ง, ไม่ tag/ไม่ปิด stage เอง
