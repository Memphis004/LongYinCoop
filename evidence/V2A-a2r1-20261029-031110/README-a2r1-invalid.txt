README-a2r1-invalid — บันทึกประกอบรัน A2-r1 (มติผู้ใช้ 2026-10-29: ทาง (ก) — A2-r1 = INVALID ไม่นับเข้า fit, รันใหม่เป็น A2-r2)

## 1) เรื่องไฟล์ fit-2point-PROVISIONAL.txt

- ไฟล์ชื่อบอก "2point" แต่**เนื้อหาจริง = INVALID**: A2-r1 ถูกตัดออกจาก fit อัตโนมัติ (โครงแตก) เหลือ
  valid points = 1 (A3-r1) ⇒ `fit=NOT_POSSIBLE` (exit 1) — **คงไว้ตามจริง ไม่เขียนทับ ไม่ลบ**
- ผลตัดสิน A2-r1 ตามเกณฑ์ = **INVALID** (`analyze-result.txt`: day_ticks 13 != N 5 — "ไม่เก็บเป็นผล" ตามกติกาเดิม)

## 2) ข้อสังเกต RUNTIME สำหรับ V1 (OBSERVED เท่านั้น — จากไฟล์ a2-r1.ndjson)

**"StartWorking ซ้ำขณะ working=1 ไม่มี work_end คั่น"** — ไฟล์มี work_start 4 ครั้ง (ทุกครั้ง
leftWorkingDay=5) โดยไม่มี work_end/work_abort คั่นเลย ⇒ working ค้าง 1 ตลอดช่วงเริ่มซ้ำ · ลายเซ็นนี้
สอดคล้องกับ time-plan-draft B.1 (StartWorking ไม่มี working guard — เขียนทับ activity state ขณะงานเดินได้,
รีเซ็ต leftWorkingDay/totalWorkingDay ทุกครั้ง) [OBSERVED ไฟล์ตรงกับ static — ห้ามขยายความ]

## 3) ข้อเท็จจริงประกอบอื่นจากไฟล์ (OBSERVED — สาเหตุ UNKNOWN ทั้งหมด)

- ช่องว่างไม่มี event 3 ช่วง: **43,428 / 77,289 / 45,038 ms** (ระหว่าง work_start ครั้งที่ 1→2 / 2→3 / 3→4)
  — ตลอดช่วง working=1 ค้าง, leftWorkingDay ค้างที่ 2/3/2 ตามลำดับ, ไม่มี pause_end/day_tick ใหม่เลย
- ในงานครั้งสุดท้าย (ครั้งที่ 4) มี inter-day pause ยาวผิดปกติ 1 ช่วง: **5,205 ms** (typical ช่วงอื่น 184–273 ms)
- เลข frame ไม่ monotonic ข้ามช่องว่าง: 3135→2854→2594→3774 — ผิดคาดจากตัวนับเฟรมรวมทั้งโปรเซส —
  สาเหตุ UNKNOWN (sampler อ่าน UnityEngine.Time.frameCount ตรง ๆ)
- `hour` คงที่ 5.0781 ตลอด (ตรงพฤติกรรมเดิม P1-5: วันเดิน ชั่วโมงคงที่)
- จังหวะภายในแต่ละ segment ปกติ (day ~1017–1065 ms · inter-day ~184–273 ms)
- **สาเหตุบนจอ: ผู้ใช้ยังไม่ได้ระบุ** (ข้อความมติมีช่องเติมค้าง) — บันทึกตามจริงว่ายังไม่มีคำตอบ ·
  ห้ามเดาแทนผู้ใช้ · ถ้าผู้ใช้เล่าภายหลัง ให้ append ในไฟล์นี้

## 4) ผลขั้นตอนปกติ

- ML log `26-10-29_3-20-20.log`: writer open/close ✓ · 0 SP5 · 0 ERROR · ไม่มี [TimeV2a] error
- restore EXACT 0/0/0 · Save 57 ไฟล์ `df8e5a11beed…` + leftovers none ·
  สำเนา `coop-test\V2A-a2r1-20261029-031110\Save.used-20261029-032348`
- มติผู้ใช้ 2026-10-29 (ข้อ ก): รันใหม่เป็น **A2-r2** (สำเนาใหม่ตามกฎ 2 §D.0) — A2-r1 คงสถานะ INVALID
