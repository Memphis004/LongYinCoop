README-a2r3-contaminated — บันทึกประกอบรัน A2-r3 (มติผู้ใช้ 2026-10-29: A2-r3 = CONTAMINATED ตามกฎ gap
ไม่เข้า fit · ลำดับถัดไป: A1-r3 แล้วปิด session · A2-r4 และ A4 ยังไม่ทำ)

## 1) ข้อเท็จจริงจากไฟล์ a2-r3.ndjson (OBSERVED)

- โครง VALID: work_start ×1 (leftWorkingDay=5 — **งาน 5 วัน ตรงแผน**) → pause_end →
  [day_tick → pause_end]×5 → work_end (total=5) · day_ticks = 5 == N
- **measured = 16.018 s** — ส่วนเกิน +9.27 s มาจาก**ช่องว่างเดียว**: **9,043 ms ระหว่าง
  day_tick (left=4, leftPause=0.25) → pause_end (left=4)** — คือ inter-day pause หลังวันที่ 1
  ยืดจาก ~200 ms เป็น 9 s (working=1 ค้าง · ไม่มี event ใดตลอดช่วง) · ช่วงอื่นปกติทุกช่วง
  (initial 541 · day 1038–1059 · inter-day อื่น 207–282 · final 486 · tail 7)
- กฎคุณภาพข้อมูล (GAP_CONTAMINATION_MS = 2000) จับอัตโนมัติครั้งแรกในสนามจริง:
  `status: CONTAMINATED · max_gap_ms: 9043 · after day_tick(left=4) -> pause_end(left=4)`
- ML log `26-10-29_4-15-44.log` สะอาด (0 SP5 · 0 ERROR · writer 13 บรรทัด) — ไม่ใช่ฝั่ง mod
- สำเนาเก็บ: `coop-test\V2A-a2r3-20261029-041029\Save.used-20261029-041820` (restore EXACT 0/0/0)

## 2) คำตอบของผู้ใช้เรื่อง popup / ชนิดงาน

- **คำตอบ 2026-10-29 (ตอนปิด session V2-A):** ผู้ใช้ระบุชนิดงาน = **"ตัดไม้"** ตามที่จำได้
  (A2-r3 ชัดจากข้อความ "ตัดไม้ไป" — งานอื่น A1-r1…A3-r2 ผู้ใช้ไม่ได้แยกรายรัน จดรวมว่าตัดไม้
  ตามที่จำได้) · **คำถาม popup (ต้องกดปิดเองหรือหายเอง กี่วินาที) ยังไม่ได้ตอบ** ·
  **ความหมายที่ตั้งใจของ "1 วัน" ยังไม่ได้ชี้แจง** — บันทึกตามจริง · เติมภายหลังได้ จะ append ไฟล์นี้
- ความไม่ตรงที่บันทึกไว้แล้ว: ผู้ใช้พูด "ตัดไม้ไป 1 วัน" แต่ไฟล์ชี้งาน 5 วันครบ (ซ้ายขวาตรง B.1)

## 3) ข้อสังเกต RUNTIME สำหรับ V1 (OBSERVED เท่านั้น)

- **"popup 'ได้ไม้' ค้างปั๊มหลัง day_tick วันที่ 1 นาน 9,043 ms"** (OBSERVED ตำแหน่ง/ระยะเวลา) ·
  **[สมมติฐาน: ผูกกับชนิดงานหรือไม่ = UNKNOWN]** — ไฟล์ NDJSON **ไม่มีฟิลด์ชนิดงาน** (ตรวจ keys ทั้งหมด:
  t/frame/ev/leftWorkingDay/totalWorkingDay/nextDayTime/leftPauseTime/workFinished/skipping/dayTime/
  pauseTime/hour/noCancel(work_start)/skipK(day_tick) — **ไม่มี workName**) จึงแยกชนิดงานจากไฟล์ไม่ได้ ·
  noCancel=false ทุก run · hour คงที่ 5.0781 ทุก run (จุดเซฟเดียวกัน) — ไม่ช่วยจำแนกชนิดงาน
- เทียบข้ามรัน (OBSERVED): การค้างเกิดหลัง day_tick ทั้งคู่ (A2-r3 หลังวันที่ 1 · A3-r2 หลังวันที่ 6) —
  ตำแหน่งไม่ซ้ำ · A2-r1 ช่วงเงียบจบด้วย work_start ใหม่ (รูปแบบต่างจาก popup)

## 4) สถานะขั้นตอนปกติ

- restore EXACT 0/0/0 · Save 57 ไฟล์ `df8e5a11beed…` + leftovers none
- fit คงใช้ 4 จุดสะอาด (A3-r1 13.500 · A3-r3 13.464 · A2-r2 7.136 · A1-r2 2.082):
  slope 1.2670 · intercept 0.810 · CI_t [0.7264, 0.8936] → UNRESOLVED_OFFSET (PROVISIONAL) — ไม่เปลี่ยน
- ลำดับถัดไปตามมติ: เตรียม A1-r3 (N=1) → รายงาน fit 5 จุด → หยุดรอ → ปิด session รอสั่งแยก
