README-a3r2-contaminated — บันทึกประกอบรัน A3-r2 (มติผู้ใช้ 2026-10-29 ทาง (ก): A3-r2 = CONTAMINATED
ไม่นับเข้า fit · fit คงใช้ 3 จุดสะอาด · ยังไม่ปิด session · รันใหม่เป็น A3-r3)

## 1) ข้อเท็จจริงจากไฟล์ a3-r2.ndjson (OBSERVED)

- โครง VALID: work_start ×1 → pause_end → [day_tick → pause_end]×10 → work_end · day_ticks = 10 == N ·
  statics dayTime=1 / pauseTime=0.25 อ่านได้ทุก event
- **measured = 29.440 s** (A3-r1 = 13.500) — ส่วนเกิน +15.94 s ทั้งหมดมาจาก**ช่องว่างเดียว**:
  **16,118 ms ระหว่าง day_tick (left=4, leftPause=0.25) → pause_end (left=4)** — คือ inter-day pause
  หลังวันที่ 6 ยืดออกจาก ~200 ms เป็น 16.1 s (working=1 ค้าง · ไม่มี event ใดเลยตลอดช่วง)
- ช่วงอื่นทุกช่วงปกติ: initial 573 · day 987–1084 · inter-day อื่น 197–245 · final 470 · tail 6 —
  รวมได้ 29,440 ms เป๊ะ
- ML log `26-10-29_3-47-45.log` สะอาด (0 SP5 · 0 ERROR · writer 23 บรรทัด) — ไม่ใช่ฝั่ง mod
- ตัวแปร freeze ภายในของปั๊ม (plotHappen / speShowUIPanel / HudPanelActive — time-static-notes §2.5)
  **ไม่อยู่ในสเปก NDJSON** ⇒ ยืนยันชนิด freeze จากไฟล์เดี่ยวไม่ได้ [UNKNOWN]

## 2) คำตอบของผู้ใช้เรื่อง popup

- **ยังไม่ได้ระบุ** — ข้อความมติมีช่องเติมค้าง (placeholder) เป็นครั้งที่สอง (ครั้งแรก A2-r1) ·
  บันทึกตามจริงว่ายังไม่มีคำตอบ · ห้ามเดาแทนผู้ใช้ · ถ้าเล่าภายหลัง ให้ append ในไฟล์นี้
- ผู้ใช้บรรยายสั้น: "เจอ popup จบงาน แล้วปิดเกม" — แต่จากไฟล์ การค้างอยู่**กลางงาน (หลังวันที่ 6)**
  ไม่ใช่ตอนจบงาน — ตำแหน่งต่างจากที่บรรยาย ⇒ บันทึกความไม่ตรงนี้ไว้ตามจริง (รอคำอธิบายเพิ่ม)

## 3) ข้อสังเกต RUNTIME สำหรับ V1 (OBSERVED เท่านั้น)

- **"ปั๊มวันค้างได้ระหว่างงานโดยไม่มี work_end"** — 16.1 s ที่ working=1 · leftWorkingDay=4 ค้าง ·
  ไม่มี pause_end/day_tick/work_end ใด ๆ (ต่างจาก A2-r1 ที่ช่วงเงียบจบด้วย work_start ใหม่ —
  รอบนี้จบด้วย event ถัดไปของงานเดิมเอง) ⇒ ปั๊มมีสถานะหยุดนิ่งภายในงานได้ โดยไม่มีร่องรอยใน
  field ที่สเปก NDJSON จับ (working/leftWorkingDay/leftPauseTime ค้างค่าเดิม)
- เมื่อปั๊มกลับมา: งานวิ่งต่อจาก left=4 จนจบครบ 10 วัน — measured โดยรวมจึงบวกเวลาค้างทั้งก้อน

## 4) ผลกระทบต่อ fit (หลักฐานตัวเลข)

- fit 4 จุด (รวม A3-r2) = **SLOPE MISMATCH exit 3 จริง**: slope 2.248 นอก 1.25±0.1 · residual
  −7.407/+8.533/−2.532/+1.406 s · CI กว้าง ±35 s — ไฟล์ `fit-4point-WITH-CONTAMINATED.txt`
- fit 3 จุดสะอาด (ตัด A3-r2) คงเดิมเป๊ะ: slope 1.2688 · intercept 0.8055 · CI_t [0.5881, 1.023]
  คาบเกี่ยว FORM2 · UNRESOLVED_OFFSET — `interim-report-3run-v2.md`
- **กฎคุณภาพข้อมูลใหม่** (มติ 2026-10-29): gap > 2.0 s ภายใน work_start→work_end = CONTAMINATED
  กันออกจาก fit อัตโนมัติ — ใช้ย้อนทุกรัน (scan: รัน valid เดิม max gap 1053–1106 ms ทุกไฟล์ —
  ผลไม่เปลี่ยน) — RUNBOOK-V2A.md ภาคผนวก A @ commit `e90de47` · เครื่องมือ @ `81580f0`
- สถานะขั้นตอนปกติ: restore EXACT 0/0/0 · Save 57 ไฟล์ `df8e5a11beed…` + leftovers none ·
  สำเนา `coop-test\V2A-a3r2-20261029-034639\Save.used-20261029-035140`
