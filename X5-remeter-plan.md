# แผนวัด X5 ซ้ำ — meter ครอบทั้งวินโดว์ + แยก periodic noise (2026-10-24)

สถานะ: **plan only** (ยังไม่แก้โค้ด/ยังไม่ build) · เกณฑ์ X5 ไม่เปลี่ยน: `p99(B) − p99(A) ≤ 1 ms`, exception = 0,
`|Δ−1ms| ≤ 0.5ms` = INCONCLUSIVE

## 1. ปัญหาที่วัดได้จริง (2026-10-24)
| ปัญหา | หลักฐาน |
|---|---|
| meter ring **4096** ทิ้งช่วงต้นวินโดว์ | `S2FrameMeter` ring 4096; ที่ ~126 fps (frames +7,547/60 s) ⇒ p99 ใช้แค่ **~32.5 s ท้าย**; ที่ 500 fps จะเหลือ ~8 s |
| p99 แกว่งเองแม้ S2 ปิด | phase A windows: 27.782 / 27.776 ms แต่ window อื่นในรอบเดียวกัน (S2 ปิด ไม่มี client) = **34.721**; phase B บาง window = **20.839** |
| ค่า B1/B2 ที่วัดได้ = ระดับ 34.7 ที่เกิดได้เอง | B1 34.719, B2 34.722, A0 27.782 ⇒ Δ +6.94 ms แต่ **เท่ากับระดับ noise ที่เห็นตอน S2 ปิด** |
| max spike ปนเข้ามา | B2 window max 479 ms (kill) แต่ p99 ไม่ขยับ |

## 2. การแก้ meter (ข้อเสนอ — ยังไม่ทำ)
1. **ขยาย ring เป็น 65536** (double 512 KB) ⇒ ครอบ 60 s ได้ถึง ~1,092 fps; reset ทุก window เหมือนเดิม
2. เพิ่มฟิลด์ยืนยันความครอบคลุม: `windowMs`, `samples`, `firstSampleOffsetMs`, `coverageMs`
   (ถ้า `coverageMs < 59,000` → window นั้นใช้ไม่ได้ ห้ามนำไปตัดสิน)
3. ทางเลือกถ้าอยากประหยัด memory: histogram 1 ms bucket ต่อ window (p99 ละเอียด 1 ms) — เพียงพอต่อเกณฑ์ 1 ms
   แต่ **ต้องคงค่า p99 เดิมให้เทียบกับ S2a-3 ได้** (รายงานทั้งสองแบบ)
4. บันทึก `GC.CollectionCount(0/1/2)` + `Time.unscaledDeltaTime` เกิน 16 ms ต่อ window เพื่อช่วยระบุต้นเหตุ noise

## 3. โปรโตคอลวัดใหม่ (แยก noise ด้วยการจับคู่)
ห้ามขยับเกณฑ์; เปลี่ยนแค่ **การออกแบบบล็อก** ให้ A กับ B อยู่ใกล้กันและสลับหลายรอบ

1. **Phase 0 — characterize noise (10 นาที, S2 ปิด, ไม่มี client)**
   เก็บ per-window p99 10 ค่า → ดูระดับ/คาบ (พบอย่างน้อย 20.84 / 27.78 / 34.72 ในข้อมูลวันนี้)
   ถ้าพบคาบชัด ให้จัดบล็อกให้ A และ B ตกลง **phase เดียวกันของคาบ**
2. **Phase 1 — paired blocks** (ทุกบล็อก 60 s เต็มวินโดว์, window-aligned):
   `A0 · B1 · A1 · B2 · A2 · B1' · A3 · B2'` = 8 นาที
   - B1 = client stall 60 s (เหมือนเดิม), B2 = client kill 60 s
   - A ทุกตัว = S2 ปิด 60 s, ไม่มี client, state `WaitPlayerInput`
3. **การตัดสิน**
   - คู่: `Δ1 = B1 − mean(A0,A1)`, `Δ2 = B2 − mean(A1,A2)`, `Δ3 = B1' − mean(A2,A3)`, `Δ4 = B2' − mean(A3,A4)`
   - รายงาน Δ ทุกคู่ + median; ใช้ median เป็นตัวตัดสิน (เกณฑ์เดิม ≤1 ms)
   - ถ้า median อยู่ใน ±0.5 ms รอบ 1 ms = INCONCLUSIVE ตามกติกาเดิม
   - ถ้า noise ระหว่าง A windows (max−min) ≥ 1 ms ⇒ ระบุ **NOT-MEASURABLE** ในรายงาน (ไม่เรียก PASS/FAIL)
4. ทุกบล็อก: ตรวจ `sampler.ndjson` โต, `framemeter.window` มี `coverageMs ≥ 59,000`, จด wall-clock, ห้าม Esc

## 4. Acceptance ของการแก้นี้
- meter ใหม่ให้ p99 ต่างจากของเดิม ≤ 0.05 ms เมื่อรันวินโดว์เดียวกัน (ทดสอบย้อนหลังด้วยข้อมูลเดิมไม่ได้ — ต้องวัดสด)
- window ที่ `coverageMs < 59,000` ถูกตัดออกอัตโนมัติ และรายงานว่าตัดไปกี่ window
- ไม่แตะโปรโตคอล/เน็ตเวิร์ก/เซฟ; rollback = คืน DLL `.prev-*`

## 5. ความเสี่ยง
| ความเสี่ยง | ลด |
|---|---|
| ring ใหญ่ขึ้นกระทบ frame time เอง | เขียนลง array แบบ O(1), ไม่ allocate ต่อเฟรม; วัด tickP99 ก่อน/หลัง |
| จัดบล็อกไม่ตรง boundary | ใช้ marker เป็นตัวตั้งต้นเหมือนรอบนี้; ถ้าเหลื่อม ให้ยืดบล็อกเป็น 125 s |
| เกมไม่มี client จริงใน B | ใช้ `viewer-client.py` เดิม (kill/stall) — ไม่ต้องแก้ client |
