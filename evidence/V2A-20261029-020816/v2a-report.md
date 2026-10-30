# v2a-report — V2 ชุด A: T(N) runtime verification (session `AUTO-20261029-013511` · ปิด session 2026-10-29 04:33)

> **สถานะ: PROVISIONAL — ไม่ปิด stage ไม่ tag** (ตามมติผู้ใช้) · เกณฑ์ตัดสิน = [CRITERIA] ภาคผนวก A
> (RUNBOOK-V2A.md @ `e90de47`) · deviation จากแผน §E.1 ระบุในหัวข้อ 5 · **ห้ามอ้างเป็น runtime-confirmed โมเดลใด**

## 1) สรุปผูกมัด

- **9 runs** (นับจาก label dir จริง 9 dir: `evidence/V2A-a1r1…a3r3` — ไม่รวม `V2A-20261029-020816` ซึ่งเป็น
  dir ของ artifact/รายงาน) = **5 valid** (A3-r1, A3-r3, A2-r2, A1-r2, A1-r3) + **2 CONTAMINATED** (A3-r2,
  A2-r3 — กฎ gap > 2 s) + **1 INVALID** (A2-r1 — โครง work_start ×4) + **1 pilot** (A1-r1) ·
  บนสำเนาเซฟทั้งหมด · restore EXACT ทุกรัน ·
  เซฟจริงไม่ถูกแตะ (digest `df8e5a11beed…` คงเดิมทุกรอบ) · 0 crash · 0 soft-lock
- **ผล fit (5 จุด non-pilot non-contaminated):** slope **1.2675 s/วัน** (ผ่าน 1.25±0.1) · intercept **0.8056 s** ·
  CI_t (df=3, t=3.182) **[0.7665, 0.8446]** — ไม่คาบเกี่ยว window ฟอร์มใด ⇒ ป้าย **UNRESOLVED_OFFSET**
  (ยอมรับได้ · PROVISIONAL) · residual รายรัน ≤ 20 ms
- **2 runs CONTAMINATED** (กฎ gap > 2 s — A3-r2 16,118 ms popup กลางงานหลัง day_tick วันที่ 6 · A2-r3 9,043 ms
  popup "ได้ไม้" หลัง day_tick วันที่ 1) · 1 run INVALID (A2-r1 โครง — day_ticks 13≠5 · gap 43,428–77,289 ms
  เข้าข่ายกฎ gap ด้วย — excluded ตามเดิม ผลไม่เปลี่ยน) · 1 pilot (A1-r1) ไม่เข้า fit
- **deviation จากแผน §E.1:** ≥3 รอบ/ค่า → **≥2 valid/ค่า** (ทาง B ลดรูป มติ 2026-10-29) — เหตุผล: N=10 ซ้ำ
  ต่าง 36 ms · CI_t ห่าง window ฟอร์มชัด · popup กลางงานเสี่ยงซ้ำ — **ผลกระทบ: N=5 มี valid 1 รัน
  (ไม่มี pure error)** · รอบซ้ำเพิ่ม = งานอนาคต
- **ชนิดงาน:** ผู้ใช้ระบุ = **"ตัดไม้"** ตามที่จำได้ (รายรันไม่ได้แยก — จดหลัง A2-r3 เท่านั้น · NDJSON ไม่มีฟิลด์
  ชนิดงาน — ไม่มี workName) · คำถาม popup (ปิดเอง/หายเอง กี่วินาที) **ยังไม่ได้ตอบ**

## 2) ตารางรวมทุกรัน (OBSERVED)

| run | N | measured (s) | max gap (ms) | สถานะ | fit |
|---|---|---|---|---|---|
| A1-r1 | 1 | 2.088 | 1,077 | pilot | ไม่เข้า fit |
| A1-r2 | 1 | 2.082 | 1,053 | OK | ✓ |
| A2-r1 | 5 | 189.037 (pooled) | **77,289** | INVALID (โครง: work_start ×4 · day_ticks 13≠5) + CONTAMINATED | ไม่เข้า |
| A2-r2 | 5 | 7.136 | 1,069 | OK | ✓ |
| A2-r3 | 5 | 16.018 | **9,043** | **CONTAMINATED** (popup "ได้ไม้" หลัง day_tick วันที่ 1) | ไม่เข้า |
| A3-r1 | 10 | 13.500 | 1,106 | OK | ✓ |
| A3-r2 | 10 | 29.440 | **16,118** | **CONTAMINATED** (popup กลางงานหลัง day_tick วันที่ 6) | ไม่เข้า |
| A3-r3 | 10 | 13.464 | 1,064 | OK | ✓ |

- กฎ gap > 2 s คัดถูกตัวทั้งสองตัว (scan ย้อนทุกไฟล์ — valid เดิม max gap 1053–1106 ไม่โดน) ·
  ลายเซ็นต่างกัน: A2-r1 จบด้วย **work_start ใหม่** · A3-r2/A2-r3 จบด้วย **pause_end ของงานเดิม** (งานวิ่งต่อ)

## 3) ผลตามเกณฑ์ [CRITERIA] (PROVISIONAL)

| ข้อ | ผล | เกณฑ์ |
|---|---|---|
| slope | **1.2675 s/วัน** (4 จุด: 1.2670 · 5 จุด: 1.2675) | 1.25 ± 0.1 ⇒ **ผ่าน** |
| intercept | **0.8056 s** (CI_t df=3: **[0.7665, 0.8446]**) | นอกทั้ง [−0.1,+0.1] และ [+0.4,+0.6] ไม่คาบเกี่ยว ⇒ **UNRESOLVED_OFFSET** (ยอมรับได้) |
| residual รายรัน | +20/−16/−7/+9/−5 ms (N=10/10/5/1/1) | รายงานแยก — ไม่ใช้ตัดสิน |
| day_ticks / ลำดับ event | ตรงโครงทุกรัน valid | day_ticks == N ⇒ ไม่ INVALID |

- **CI สองวิธีคู่ขนาน** (มติรอบ A3-r2 เตรียม): แบบ t (valid ตาม df) = [0.7665, 0.8446] · แบบ 1.96
  (**ไม่ valid สำหรับ df ต่ำ** — เทียบรอบก่อน) = [0.7815, 0.8296] · ทั้งคู่ไม่คาบเกี่ยว window ใด
- **วิธี CI:** ±tcrit(95%, df)×SE(intercept) จาก OLS residual · **ข้อจำกัด: n=5 จุด / 1–2 รันต่อค่า /
  N=5 มีจุดเดียว ⇒ CI ยังไม่ใช่ค่าที่น่าเชื่อถือเต็มที่** (ต้องมีรอบซ้ำเพิ่ม)
- **ความหมายของ UNRESOLVED_OFFSET:** measured เป็นเส้นตรงกับ N แม่นมาก (residual ≤ 20 ms) แต่มี
  ค่าคงที่ต่อ run ~0.81 s ที่**ไม่ตรงทั้ง FORM1 (0) และ FORM2 (0.5)** และยัง**ระบุสาเหตุไม่ได้จากไฟล์** —
  จึงแยกฟอร์มไม่ได้ด้วยข้อมูลชุดนี้ (ไม่ใช่ความล้มเหลวของการวัด)

## 4) ตารางรายช่วง + นิยามเหตุการณ์ที่กั้น (OBSERVED)

**นิยามเหตุการณ์ (จาก TimeV2aRules.cs — ตรวจจับจากค่าต่อเฟรม ไม่มี hook):**
- `work_start` = เฟรมแรกที่เห็น `working==1` (หลัง StartWorking เขียน field ครบ: left=N, nextDayTime=0,
  leftPauseTime=pauseTime) — `TimeV2aRules.cs:144`
- `pause_end` = เฟรมแรกที่เห็น `leftPauseTime` ข้ามจาก >0 ไป ≤0 (`:172–174` — ค่าติดลบเล็ก −0.007
  คือ overshoot 1 เฟรมของปั๊ม ยังจับได้)
- `day_tick` = เฟรมแรกที่เห็น `leftWorkingDay` ลดลง (`:161–163` — **หลังปั๊มทำครบ**: SendMessage(callFuc)
  → leftWorkingDay−− → ChangeHour(24) → ตั้ง leftPauseTime — time-static-notes §2.4) ⇒ day segment
  **รวมเวลา callFuc + housekeeping**
- `work_end`/`work_abort` = working 1→0 โดย workFinished true/false (`:130`)

**ตารางรายช่วง (ms — เฉพาะรัน valid):**

| run | N | initial = work_start→pause_end#1 | day_i = pause_end#i→day_tick#i | inter-day_i = day_tick#i→pause_end#(i+1) | final = day_tick#N→pause_end#(N+1) | tail = →work_end |
|---|---|---|---|---|---|---|
| A3-r1 | 10 | 544 | 1060.9 (1043–1106) | 208.0 (186–226) ×9 | 469 | 6 |
| A3-r3 | 10 | 577 | 1047.2 (1037–1064) | ≤235 ×9 | 465 | 13 |
| A2-r2 | 5 | 557 | 1055.6 (1044–1069) | 199–218 ×4 | 445 | 6 |
| A1-r2 | 1 | 564 | 1053 | — | 458 | 7 |
| A1-r3 | 1 | 2.068 s รวม → initial **549** | 1061 | — | 451 | 7 |
| A1-r1 (pilot) | 1 | **559** | 1077 | — | 445 | 7 |
| โครงจาก statics | | **250** (pauseTime) | **1000** (dayTime) | **250** (pauseTime) | **500** (2×pauseTime) | — |

## 5) หมายเหตุที่มติกำหนด: initial segment ≈ 0.56 s **ขัดกับ pauseTime จาก disasm**

- **OBSERVED (ขัดกันทุกรัน):** initial segment (work_start → pause_end#1) วัดได้ **544–577 ms (~0.56 s)
  ในทุก 6 รัน** ขณะที่ static/disasm ระบุ `StartWorking` เขียน `leftPauseTime = pauseTime = 0.25 s`
  (time-static-notes §2.4/§2.6 · dump.cs 0x58) ⇒ **ส่วนเกิน +294–314 ms คงที่ข้ามทุก N ทุก run**
- **สาเหตุ: UNKNOWN — ไม่มีหลักฐานในไฟล์ NDJSON** (ไม่มี event แอนิเมชัน/freeze) และคำตอบบนจอของ
  ผู้ใช้ยังไม่ครบ ⇒ รายงานเป็น**ข้อขัดแย้งที่ต้องสืบสวนต่อ** (ตัวเลือกเชิงสืบสวนเสนอไว้ใน 7) —
  **ห้ามอธิบายด้วยคำว่า "แอนิเมชัน" จนกว่าจะมีหลักฐาน**
- **ผลสืบสวน (log/โค้ด/disasm — 2026-10-29, `evidence/UNKNOWN-invest-20261029/unknowns-investigation.md`):**
  ขอบเขตถูกจำกัดด้วย**สมมติฐานที่สอดคล้องเชิงปริมาณ (hypothesis — ไม่มี dt ต่อเฟรมใน NDJSON จึงยังไม่พิสูจน์ตรง)** =
  ปั๊มถูก gate หยุด **~34–42 เฟรม (~0.27–0.34 s)** ช่วงเริ่มงาน โดย fps ปกติตลอด — **เข้ากันได้กับ** freeze gate
  3 เงื่อนไขของปั๊ม (time-static-notes §2.5) หรือ panel ยังไม่ active · **เงื่อนไขไหนใน 3 ตัว = ยังระบุไม่ได้
  จากไฟล์** (ต้อง runtime/คำบรรยายจอ) · ไม่ใช่สาเหตุจากอัตราเวลาหรือแอนิเมชัน · เกณฑ์หักล้าง/สิ่งที่ต้องวัดเพิ่ม
  อยู่ใน investigation §3a/§3b
- สัมพันธ์กับผล fit: intercept 0.8056 − โครง form2 0.5 = **+0.3056 s ≈ ส่วนเกิน initial +0.31 s** —
  ส่วนเกินของวัน (+19 ms/วัน) ถูกดูดเข้า slope (1.2675 vs 1.25) — **ทั้งหมดเป็นการพรรณนาเชิงเลข
  จาก OBSERVED ไม่ใช่การอธิบายสาเหตุ**

## 6) UNKNOWN (รวม)

1. สาเหตุ offset ~0.81 s / initial segment ~0.56 s (ขัด pauseTime 0.25 จาก disasm) — UNKNOWN
   **(อัปเดต 2026-10-29: hypothesis เชิงปริมาณ — offset แตกเป็น 0.5 (2P final) + ~0.31 s initial freeze
   (~34–42 เฟรม, fps ปกติ) — **เลขคณิต ไม่ใช่หลักฐานกลไก**; เงื่อนไข freeze ตัวไหน = ยังเปิด — ดู
   `evidence/UNKNOWN-invest-20261029/`)**
2. สาเหตุ inter-day/final pause วัด**สั้น**กว่าโครง 30–55 ms ทุกช่วงทุกรัน — UNKNOWN
   **(อัปเดต 2026-10-29: สอดคล้องเชิงปริมาณ — hypothesis ยังไม่พิสูจน์ตรง: day-tick housekeeping
   (~40–120 ms รายวัน) กลายเป็น subtract ใหญ่ครั้งเดียวของเฟรมถัดไป (`unscaledDeltaTime` แบบ start-to-start —
   SPEC) อยู่นอกหน้าต่าง t ของ sampler; ตรวจครบ 22 inter-day + 2 final ทุกรัน — ทุกช่วงเข้ากันได้กับโมเดล
   — ดู `evidence/UNKNOWN-invest-20261029/` §3 + เกณฑ์หักล้าง §3a + สิ่งที่ต้องวัด §3b)**
3. popup กลางงาน (A3-r2 หลังวันที่ 6 · A2-r3 "ได้ไม้" หลังวันที่ 1): ชนิด/สาเหตุ — ผู้ใช้ระบุชนิดงาน
   = "ตัดไม้" ตามที่จำได้ แต่**คำถาม popup (ปิดเอง/หายเอง กี่วินาที) ยังไม่ได้ตอบ** · ตำแหน่งที่ไฟล์
   ชี้ (หลัง day_tick) ต่างจาก "จบงาน" ที่บรรยาย — ค้างรอคำอธิบายเพิ่ม
   **(อัปเดต 2026-10-29: ลายเซ็น stall สอดคล้องกับ freeze gate 3 เงื่อนไขของปั๊ม (hypothesis) — โมดเดิน
   119.9 Hz ตลอด stall, ML log เงียบ; แผงไหน = ยังเปิด — ดู `evidence/UNKNOWN-invest-20261029/` §4)**
4. เลข frame ไม่ monotonic ใน A2-r1 — UNKNOWN (README-a2r1-invalid.txt)
5. คำตอบชนิดงานรายรัน A1-r1…A3-r1 — ผู้ใช้จำรวมเป็น "ตัดไม้" (ไม่แยกรายรัน)

## 7) สิ่งที่ยังต้องวัด/ทำเพื่อปิดผลสุดท้าย (อนาคต — ต้องมติแยก)

1. รอบซ้ำต่อค่าให้ครบ ≥3 valid/ค่า (A2 ขาด: ต้อง ≥2 รอบสะอาดอีก) — เพื่อ CI แคบ + pure error ครบทุกค่า
2. **A4-r1 (N=10 กด `>>`)** — observational (k จาก `skipK` ราย day_tick) · **ต้องยืนยัน TestMode
   สองสัญญาณ** (§E.3 บรรทัด 426–429): สัญญาณ 2 (พฤติกรรม: กด `>>` งานสั้น ดู k 0.5/0.2 + anim ×2/×5)
   ทำได้ทันที · สัญญาณ 1 (อ่าน `PlayerPrefDictionary.GetInt("TestMode")`) **ต้อง build ใหม่** (โค้ดปัจจุบัน
   ไม่มี) — รอมติ
3. สืบสวน initial segment ~0.56 s (static เพิ่ม: ถอด body ช่วง StartWorking→pause branch ละเอียด /
   วัด runtime ที่มี event คั่น) — ก่อนตีความ offset ใด ๆ
   **(อัปเดต 2026-10-29: ส่วน static/log/โค้ดทำแล้ว — hypothesis = freeze gate ~34–42 เฟรม (ดู
   `evidence/UNKNOWN-invest-20261029/` §3a/§3b — เกณฑ์หักล้าง + สิ่งที่ต้องวัดเพิ่ม); สิ่งที่ยังต้องทำ =
   ยืนยัน runtime ว่าเงื่อนไข freeze ตัวไหน — ต้อง build ใหม่ + อนุมัติเปิดเกม)**
4. (ถ้า V1 อนุมัติ) popup กลางงานเข้าข่าย freeze 3 ทางของปั๊ม — ผูกกับ TC ของ V1

## 8) สถานะ session (ปิดแล้ว 04:33)

| รายการ | ค่า | ตรง |
|---|---|---|
| เกมรัน | ไม่ (ตรวจ 04:33) | ✓ |
| `TimeV2a` | **false** (ตรวจค่า key — `976ccfb9…` · ก่อนแก้ `b07cab85…` ที่ ML เขียนตอนปิดเกม) | ✓ |
| `AllowRemoteInput` / `Sp5T0` / `Enabled` | false / false / true | ✓ |
| pointer `time-v2a.out` | **ลบแล้ว** | ✓ |
| net.cfg | `12541f34…` (444 B) — ไม่ถูกแตะตลอด session | ✓ |
| เซฟ | EXACT 57/57 `df8e5a11beed…` + `leftovers: none` | ✓ |
| DLL | `983b2384…` (366,080 B) + `.prev-6a94abb5` — **มติหลังปิด session: rollback ทาง (ข) กลับ `6a94abb5…` ทำแล้ว** (verify hash ตรง 2026-10-29 · บันทึก VERSION.md หัวข้อ V2-A) | ✓ |
| commits ของ session | `21e99a0`(A) `b0ca9f7`/`91bf85c`/`211ea21`(B) `bd7885e`(C) `397b569` `789f670` `3490cff` `2952e1c` `0c9202d` `516197d` `646f0f5` `1a5797b` `81580f0` `e90de47` — src/ ไม่เปลี่ยนจาก `bd7885e` | ✓ |
| ไฟล์รายงาน/หลักฐาน | v2a-report.md (ไฟล์นี้) · interim-report-3run.md/-v2.md · README-a2r1/a2r3 · ผล analyze/fit ราย label — **commit แล้ว** (มติผู้ใช้ 2026-10-29 · `f395ff9` — เดิมเขียน "บนดิสก์ ยังไม่ commit รอสั่ง" ซึ่งล้าสมัย) + แก้ไขเพิ่ม `44b255b`/`c07ae7c`/`ee993d1` · NDJSON ดิบและ .log ยัง gitignored | — |

## 9) สถานะปิดงาน (docs-only · มติผู้ใช้ (ข) 2026-10-31 · session `AUTO-20261031-031942`)

- **มติ (ข) 2026-10-31: รับสถานะ PROVISIONAL เป็นคำตอบของชุด A** — ปิดงานเชิงเอกสารเท่านั้น ไม่มีรอบวัดซ้ำ
  · ไม่แตะเกณฑ์ [CRITERIA]/analyzer/fit/RUNBOOK-V2A · สถานะหัวไฟล์ "PROVISIONAL — ไม่ปิด stage ไม่ tag"
  และ "**ห้ามอ้างเป็น runtime-confirmed โมเดลใด**" คงเดิมทุกตัวอักษร
- **ป้ายผลชุด A = UNRESOLVED_OFFSET (PROVISIONAL)** ตามเกณฑ์ที่ล็อกไว้ [CRITERIA] ภาคผนวก A
  (RUNBOOK-V2A @ `e90de47`): slope 1.2675 s/วัน ผ่าน 1.25±0.1 · intercept 0.8056 (CI_t [0.7665, 0.8446])
  นอกทั้ง FORM1 [−0.1,+0.1] และ FORM2 [+0.4,+0.6]
- **ข้อจำกัดข้อมูล (จาก §1–§2):** N=10 → valid 2 รัน (A3-r1, A3-r3 — ทำซ้ำได้ ต่างกัน 36 ms) · N=5 → valid
  **1 รัน** (A2-r2 — ไม่มี pure error) · N=1 → valid **2 รัน** (A1-r2, A1-r3 — A1-r1 pilot ไม่นับ) ⇒ **ไม่ครบ
  ≥3 รอบ/ค่าตามแผน §E.1 เดิม** (deviation ทาง B ลดรูป ≥2 valid/ค่า — N=5 ยังไม่ครบแม้เกณฑ์ลดรูป) ·
  เหตุผลที่ไม่วัดซ้ำเพิ่มใน session: N=10 ทำซ้ำได้ต่างกันแค่ 36 ms + popup ค้างปั๊มทำรันเสีย 2 รอบที่
  **ไม่ใช่โครงเวลา** (CONTAMINATED A3-r2/A2-r3) — เสี่ยงซ้ำโดยไม่เพิ่มสารสนเทศ
- **คำอธิบาย offset ยังเป็น hypothesis — ไม่ปิดเชิงกลไก:** initial segment ~0.56 s ≈ freeze gate ~34–42 เฟรม
  (§5 · `evidence/UNKNOWN-invest-20261029/`) = การพรรณนาเชิงเลขจาก OBSERVED ไม่ใช่หลักฐานกลไก ·
  เงื่อนไข freeze ตัวไหนใน 3 ตัวยังเปิด — USER TESTIMONY 3 ข้อ = **"ไม่แน่ใจ ทั้ง 3 ข้อ"** (2026-10-30
  append ครั้งที่ 13 ของ `user-popup-answers-20261029.md`) ⇒ freeze gate ยังสรุปไม่ได้จากคำตอบ
- **A4 (skip `>>`) ไม่ได้วัด — เลื่อน** (§7 ข้อ 2: สัญญาณ 2 ทำได้ทันที · สัญญาณ 1 ต้อง build ใหม่ — รอมติ)
- **popup/ปั๊มค้าง = ข้อจำกัด UNKNOWN:** **2 จาก 9 รัน** — A3-r2 (gap **16,118 ms** · popup กลางงาน
  ชนิดไม่ระบุชื่อ หลัง day_tick วันที่ 6) · A2-r3 (gap **9,043 ms** · popup "ได้ไม้" หลัง day_tick วันที่ 1) ·
  ลายเซ็นเดียวกัน = จบด้วย pause_end ของงานเดิม (งานวิ่งต่อ — §2) · 5 valid runs ไม่มี gap >2 s
  (max 1,053–1,106 ms) · อายุ/กลไก popup บนจอ = UNKNOWN (§6 ข้อ 3)
- **หมายเหตุการถอดความคำสั่ง:** ข้อความสั่งปิดงาน 2026-10-31 เขียน "N=1 valid 1 รัน" — ขัดกับตาราง §2
  (A1-r2 + A1-r3 ทั้งคู่เข้า fit) และ VERSION.md หัวข้อ V2-A ⇒ หัวข้อนี้บันทึก**ตามหลักฐาน = 2 รัน**
  (การตัดสินรับ PROVISIONAL ไม่ขึ้นกับจำนวนนี้ — ป้าย PROVISIONAL มาจาก UNRESOLVED_OFFSET + N=5 ไม่ครบแม้เกณฑ์ลดรูป)
