# แผนเก็บ X6 ให้ได้ n ≥ 1000 (2026-10-24)

เป้าหมาย: ตัดสิน X6 (`p99(sampleMs+encodeMs) ≤ 4 ms`) ด้วย sample จริง ≥ 1000 (ตอนนี้ได้แค่ n=159)

## 1. ทำไม n ถึงน้อย
`S2Metrics.AddFrame` ถูกเรียกต่อ **snapshot ที่ส่งจริง** เท่านั้น (ไม่ใช่ต่อเฟรมเกม) · ตอนยืน `WaitPlayerInput`
ไม่มีการเปลี่ยนแปลง ⇒ ส่งเฉพาะ heartbeat 1 Hz ⇒ ต้องรอ ~17 นาทีเพื่อครบ 1000 ซึ่งยาวเกินไปและเสี่ยง recorder หลุด

## 2. ทางที่เลือก (ไม่แตะโค้ด/เกณฑ์)
- ตั้ง `heartbeatMs = 200` **ชั่วคราวใน `net.cfg`** (ไฟล์ config ที่แผนอนุมัติคีย์อยู่แล้ว) ⇒ ~5 send/s แม้ยืนนิ่ง
  ⇒ 1000 sample ภายใน ~200 s · ผู้เล่นเล่น 1–2 เทิร์นจะเร็วขึ้นอีก
- เหตุผลเชิงระเบียบวิธี: X6 วัด **ต้นทุนต่อการส่งหนึ่งครั้ง** ไม่ได้กำหนด send rate; การเพิ่ม rate ทำให้ภาระหนักกว่า
  ค่า default (1 Hz) ⇒ เป็นทิศที่**ไม่เข้าข้างมอด** (conservative) และยังเบากว่า S2a-2 ที่วัดที่ 60 Hz
- ต้องบันทึก sha ของ `net.cfg` ก่อน/หลัง และคืน `heartbeatMs = 1000`, `enabled = false` เมื่อจบ

## 3. ขั้นตอน
1. split สำเนาเซฟ label ใหม่ + recorder + preflight (เกมต้องปิด)
2. ผู้ใช้เปิดเกม → เข้าศึก → `WaitPlayerInput` → แจ้ง
3. เปิด S2 (`enabled=true`) → ตรวจ listener ≤2 s
4. `viewer-client.py --duration 600 --ping-hz 0.2` (เพดาน 10 นาที) — **หยุดเมื่อ `sent ≥ 1000`** โดยอ่าน
   `sent=` จาก `[S2] active …` ใน MelonLoader log ทุก ~10 s
5. รอ marker `s2.metrics` ตัวถัดไป (≤60 s) → อ่าน `window` (= min(sent,2048)) และ `combinedP99`
6. ตัดสิน: `window ≥ 1000` และ `combinedP99 ≤ 4 ms` → **PASS**; `> 4 ms` → **FAIL**; `window < 1000` → **NOT-RUN**
   (ไม่ขยับเกณฑ์) · รายงาน marker ทุกตัวของรอบนั้น
7. ปิด S2, คืน `heartbeatMs=1000`, ผู้ใช้ปิดเกม → C8 verify/restore

## 4. Acceptance / หลักฐาน
- `sent` counter ≥ 1000 (จาก log) **และ** marker `window ≥ 1000`
- `viewer` summary: Div-A = 0, errors = 0, sessions = 1
- ไฟล์: `x6-run-*.md`, `s2.metrics` lines, MelonLoader log excerpt, `c8c-verify/restore`
- ความเสี่ยง: heartbeat 200 ms ทำให้ wire bytes สูงขึ้น ~5× (แต่ X2 ไม่ได้วัดในรอบนี้ — ต้องระบุว่าไม่ใช้ผล X2 จากรอบนี้)
