# unknowns-investigation — สืบสวน UNKNOWN ของ v2a-report (§5/§6) ด้วย log/โค้ด/disasm เท่านั้น · 2026-10-29

> ขอบเขต: **ไม่เปิดเกม ไม่แตะเซฟ/Mods/UserData** — ใช้ (1) ไฟล์ NDJSON ที่วัดไว้แล้ว 9 รัน (2) โค้ด sampler
> ใน `src/LongYinCoop/TimeV2a/` (3) static disassembly เดิม `evidence/TIMEstatic-20261027-051647/bodies/`
> (GameAssembly.dll อ่านอย่างเดียวผ่าน capstone + Il2CppDumper script.json) (4) ML logs ของ session รัน
> (5) สแกนใหม่ 1 ชุด (`scan_058_dt_writers.py` + `scan_058_dt_writers.tsv` ในโฟลเดอร์นี้ — อ่าน DLL อย่างเดียว)
> · ป้าย: **DUMP** = อ่านจาก binary/dump.cs เอง · **RUNTIME** = จากไฟล์ NDJSON/log ที่วัดแล้ว ·
> **SPEC** = ความหมายมาตรฐานของ Unity (เอกสาร engine) · **INFER** = สรุปจากการประกอบหลักฐาน (ไม่มีการวัดตรง)

## 0) บทสรุป (3 ข้อ — ทั้งหมดเป็น **สมมติฐานที่สอดคล้องเชิงปริมาณ (hypothesis) — ไม่มีการวัด dt ต่อเฟรมใน NDJSON จึงยังไม่พิสูจน์ตรง**)

1. **initial segment ~0.56 s (เขียน 0.25)** = ปั๊มถูก gate หยุด ~**34–42 เฟรม (~0.27–0.34 s)** ช่วงเริ่มงาน
   โดย fps ปกติตลอด — **สอดคล้อง**กับ **freeze gate 3 เงื่อนไขของปั๊ม (time-static-notes §2.5)** หรือกรณี panel ยังไม่
   active · **เงื่อนไขไหนใน 3 ตัว = ยังระบุไม่ได้จากไฟล์** (แก้ที่ "UNKNOWN เชิงกลไก" → "UNKNOWN เชิงตัวตนเงื่อนไข")
2. **inter-day/final pause สั้นกว่าค่า written ~30–55 ms ทุกช่วง** = **สอดคล้องเชิงปริมาณ (hypothesis)**: **day-tick housekeeping
   (SendMessage(callFuc) + ChangeHour(24) + SetText ×3) ทำ frame นั้นช้าลง ~40–120 ms แล้วค่า `unscaledDeltaTime`
   ของเฟรมถัดไป (วัดแบบ start-to-start — SPEC) กลืนต้นทุนนั้นเป็น subtract ใหญ่ครั้งเดียวของ pause** —
   เฟรมถัดไปนั้นอยู่**หลัง** timestamp ที่ sampler บันทึก day_tick จึงไม่ปรากฏในหน้าต่าง t ของ pause
   (ตรวจครบ **22 inter-day + 2 final + 20 day segments ของ 5 รัน valid** — ทุกช่วง**เข้ากันได้**กับโมเดลนี้ ·
   NDJSON ไม่บันทึก dt ต่อเฟรม จึง**ยังไม่มีการพิสูจน์ตรง** — ดู §3a/§3b)
3. **popup กลางงาน (9.0 s / 16.1 s)** = **สอดคล้องกับ** freeze gate เดียวกันข้อ 1 ค้างนาน: ขณะ stall โมดยังเดินทุกเฟรม
   (heartbeat `ticks +600 ต่อ 5.00 s = 119.9 Hz` · err=0) และ ML log **ไม่มี** บรรทัด Unity/exception/popup เลย
   ⇒ ปั๊มถูก gate (working=1 ค้าง ไม่มี event) — **แผงไหน (plot/SpeShow/HUD) = ยังระบุไม่ได้จากไฟล์**

## 1) ฝั่ง static — ตรวจซ้ำระดับคำสั่ง (DUMP)

- `WorkingUIController$$Update` (0x9CB4B0 · `bodies/WorkingUIController_Update.txt`, VA−0x180000000 = RVA):
  - เข้า pause branch เมื่อ `leftPauseTime > 0` (`0x9CBA36` read `xmm6,[rdi+0x58]` → `0x9CBA3B comiss` →
    `0x9CBA3F ja 0x9CBDBE`)
  - **pause branch ลดค่าครั้งเดียวต่อ Update**: `0x9CBDC0 call RealTime$$get_deltaTime` → `0x9CBDC5 subss xmm6,xmm0`
    → `0x9CBDC9 comiss xmm8,xmm6` (xmm8 = 0 จาก `0x9CB77A xorps`) → `0x9CBCDC movss [rdi+0x58],xmm6` →
    ถ้ายัง > 0 = epilogue คืน (`0x9CBDD2 jb 0x9CBCA4`)
  - write `[rdi+0x58]` ใน Update มี 2 จุดเท่านั้น: `0x9CBC9F` (day-tick: `pauseTime` หรือ `2×pauseTime` จาก
    statics+0x4 — ตรง NDJSON 0.25/0.5) และ `0x9CBCDCD` (subtract ข้างบน) — **ไม่มี subtract ซ้ำในเฟรมเดียว**
  - day branch (`nextDayTime += dt` ที่ `0x9CBA56`, threshold เทียบ `0x9CBB16`) และ finish branch `0x9CBCC7` ตามโน้ต §2.4 เดิม
- **สแกนหา writer ที่สองของ leftPauseTime ทั้งไบนารี** (`scan_058_dt_writers.py` — เมธอดที่มีทั้ง float-store
  `[reg+0x58]` และ call `RealTime$$get_deltaTime` ในเมธอดเดียว): ได้ **2 เมธอด** = `WorkingUIController$$Update`
  (ข้างบน) และ `WeatherController$$Update` (field 0x58 ของ WeatherController เอง — คนละวัตถุ) ⇒ **ไม่มี
  writer อื่นที่ขับเคลื่อนด้วย dt**
- `WorkingUIController` ไม่มี Update รอง (dump.cs:392679: มี Awake/Update/TryCancelWorking/StartWorking×6/
  StopWorking/AddResourceNum/SkipButtonClicked/SetSkippingState/.ctor/.cctor เท่านั้น) — ไม่มี FixedUpdate/LateUpdate
- `RealTime$$get_deltaTime` = tail-jmp → `UnityEngine.Time$$get_unscaledDeltaTime` (0xCFD4A0, โน้ต §2.3 REPRODUCED)
- freeze gate 3 เงื่อนไข (โน้ต §2.5) อยู่**ก่อน**ทั้งสอง branch ⇒ เฟรมที่ gate จริง = **ไม่ลด pause ไม่สะสม day ไม่ปล่อย event ใด**

## 2) ฝั่ง sampler (โค้ด + RUNTIME)

- `t` = `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()` ณ mod Tick; `frame` = `Time.frameCount`
  (`TimeV2aSampler.ReadObservation`) · Tick ผูก `MelonEvents.OnUpdate` (CoopMod.cs:41) = ทุกเฟรม player loop ·
  detector = transition ระหว่างเฟรมที่อ่านติดกัน (TimeV2aRules.Feed) — ไม่มี buffer/throttle
- **พิสูจน์ว่าอ่านทุกเฟรมจริง (RUNTIME):** `skipK` บน day_tick = max(`nextDayTime`) ที่เคยเห็น ÷ dayTime =
  **0.9791–1.0000 ทุก tick ทุกรัน** (เกมเทียบ threshold ก่อน reset — ค่า tail ที่เห็นได้ = 1−dt ของเฟรม crossing
  ⇒ 0.9930 = 1−0.0069 คือการอ่านครบทุกเฟรม ไม่ใช่การข้าม) · ถ้า Tick ข้ามเฟรม max จะต่ำกว่านี้มาก
- **ขณะ stall 16.1 s ของ A3-r2 (03:48:26.853→03:48:42.971):** ML log มีเฉพาะ heartbeat `[LongYinCoop]` ทุก ~5 s
  (ticks 4200→6000 = **600 ticks/5.00 s = 119.9 Hz**, err=0, heap ปกติ) — ไม่มีบรรทัด Unity/exception/plot/popup เลย
  ทั้ง log 03-47-45 และ 04-15-44 ⇒ player loop เดินเต็มจังหวะ ปั๊มถูก gate ภายใน (RUNTIME)

## 3) ผลเชิงปริมาณรายช่วง (RUNTIME — จาก NDJSON ทุก event ของ 5 รัน valid)

**สมมติฐาน (hypothesis — สอดคล้องเชิงปริมาณกับทุกช่วง · ไม่มีการวัด dt ต่อเฟรมใน NDJSON จึงยังไม่พิสูจน์ตรง ·
INFER — ประกอบจาก DUMP §1 + SPEC + ตัวเลข §2):**

| ช่วง | written | วัด (t-window) | อธิบาย |
|---|---|---|---|
| initial (work_start→pause_end#1) | 0.25 | 544–577 ms (65–75 เฟรม) | subtract จริง ~29–33 เฟรม + **ปั๊มถูก gate ~34–42 เฟรม** (fps ปกติ 7.7–8.4 ms/เฟรม ตลอด — ไม่ใช่ hitch ยาวเฟรมเดียว) |
| day_i (pause_end#i→day_tick#i) | 1.0 | 1038–1106 ms | = Σdt(1000) + **duration(เฟรม housekeeping day-tick) − duration(เฟรม crossing)** (เฟรม housekeeping อยู่ในหน้าต่าง t) |
| inter-day (day_tick#i→pause_end) | 0.25 | 186–235 ms (**14–28 เฟรม**) | subtract เฟรมแรกหลัง day_tick = **duration(เฟรม housekeeping) ทั้ยอด (~40–120 ms — SPEC start-to-start)** อยู่นอกหน้าต่าง t ⇒ เหลือ ~0.19 ที่วัดเป็น 14–28 เฟรม |
| final (day_tick#N→pause_end) | 0.5 | 445–469 ms (44–52 เฟรม) | กลไกเดียวกัน — เครดิต ~50–60 ms นอกหน้าต่าง t |
| tail (→work_end) | — | 1 เฟรม 6–13 ms | กิ่งจบงานตาม DUMP |

- ตัวอย่างจับคู่ชัด (a3-r1): day#1 วัด 1060 ms (+60) → inter-day ถัดมา 23 เฟรม/195 ms (เครดิต ~60) ·
  inter-day ที่ 16 เฟรม/186 ms (เครดิต ~120) อยู่หลัง day ที่ยาวสุด 1076 ms (+76) — housekeeping วันนั้นหนัก
  ⇒ ความแปรปรวนของ "เครดิต" ตามน้ำหนัก SendMessage/ChangeHour รายวัน (INFER)
- ผลต่อ fit: ต่อวัน (day+pause) = 1000+0.25 + (dD−dC) + (−dD_prev+dC) — เทเลสโคป = ผลต่าง housekeeping
  ของสองวันติดกัน ≈ 0 เฉลี่ย ⇒ slope เกิน +17.5 ms/วันที่วัด = เศษ overshoot crossing (~1–2 dt/ช่วง) +
  ความแปรปรวน housekeeping — สอดคล้อง residual รายรัน ±20 ms (ไม่ใช่อัตราเวลาคลาดเคลื่อน)
- **UNRESOLVED_OFFSET 0.8056 แตกได้เป็น: 0.5 (final pause = 2×pauseTime ตามโครง FORM2 — เวลาเกมจริง) +
  ~0.31 s (initial freeze — เวลาจริงทั้งยอด)** ⇒ "ค่าคงที่ต่อ run" ไม่ได้มาจากอัตราหรือสูตร — มาจาก
  สองเหตุการณ์คงที่หัว-ท้าย (INFER จากตัวเลขครบทุกรัน) · **ย้ำ: การแยกส่วนนี้เป็นเลขคณิตบนค่าที่วัด
  ไม่ใช่หลักฐานกลไก** — ป้ายตามเกณฑ์คง **UNRESOLVED_OFFSET (PROVISIONAL)** ไม่เปลี่ยน

## 3a) อะไรจะหักล้างสมมติฐานนี้ (refutation criteria — เขียนล่วงหน้าตามคำสั่งผู้ใช้)

สมมติฐานหลักของรายงานนี้มี 2 ชั้น และแต่ละชั้นหักล้างได้ดังนี้:
- **ชั้น pause สั้น (start-to-start dt กลืนต้นทุน housekeeping)** — จะถูกหักล้างถ้า:
  1. มีบันทึก `unscaledDeltaTime` **ต่อเฟรม** ช่วง inter-day แล้วเห็นว่า dt ของเฟรมแรกหลัง day_tick
     เท่ากับช่วงเฟรมปกติ (~8–9 ms) ขณะที่ leftPauseTime ยังลดเร็วกว่านั้น — เช่นเห็นค่าแรกหลัง write =
     0.25 − dtเล็ก (ไม่ใช่ 0.25 − dtใหญ่)
  2. วัด duration ของเฟรม day-tick แล้วพบว่า**ปกติ** (~8–9 ms) แต่ pause ยังสั้นอยู่ ⇒ ต้นทุนไม่ได้มาจาก
     housekeeping
  3. fps ต่ำผิดปกติคงเครื่องระหว่างช่วง pause ที่วัดสั้น (dt แย่งกันไม่ได้ตามโมเดล)
- **ชั้น initial freeze (~34–42 เฟรม)** — จะถูกหักล้างถ้า: ผู้ใช้ยืนยันบนจอว่าช่วงเริ่มงาน**ไม่มี** plot/popup
  /แผงใดขึ้นและ HUD ปกติตลอด แต่ initial ยังยาว ~0.56 s ทุกรัน ⇒ gate 3 ทางไม่ใช่ต้นเหตุ (ต้องหาสาเหตุใหม่)
- ถ้าสมมติฐานถูกหักล้าง ตัวเลข OBSERVED ทุกตัวใน v2a-report **ไม่เปลี่ยน** — มีแต่การตีความ UNKNOWN ที่ต้องเขียนใหม่

## 3b) ต้องวัดอะไรเพิ่มเพื่อยืนยัน (ทุกข้อ = **ต้อง build ใหม่ + มติแยก** — แตะโค้ด sampler)

1. เพิ่ม field ต่อเฟรมใน NDJSON: `unscaledDeltaTime` (หรือ `Time.unscaledDeltaTime` ที่ sampler อ่านเอง) +
   `frameDuration` — พิสูจน์ตรงชั้น pause สั้นได้ทันที (เทียบ dt เฟรมแรกหลัง day_tick กับ leftPauseTime)
2. บันทึก snapshot ต่อเฟรมช่วง initial (หรือ event คั่นเมื่อ freeze gate จริง: อ่าน `plotHappen`/SpeShow active
   /`HudPanelActive` ต่อเฟรม — อ่านอย่างเดียวเช่นเดิม) — ระบุเงื่อนไข freeze ตัวไหน
3. (ถ้าอนุมัติเปิดเกม) การทดลองเชิงพฤติกรรม: เริ่มงานโดยจงใจมี/ไม่มี popup แล้วเทียบ initial — คู่ TC ของ V1

## 4) popup กลางงาน (UNKNOWN #3) — สิ่งที่เพิ่มได้

- ลายเซ็นรอบ stall (working=1 ค้าง · leftWorkingDay ค้าง · ไม่มี event · โมด 119.9 Hz · log เงียบ) **สอดคล้องกับ
  freeze gate 3 เงื่อนไขเดิมของปั๊ม (hypothesis — DUMP §1/โน้ต §2.5)** — popup ใด ๆ ที่ทำให้ `plotHappen`/`SpeShow` active
  หรือ HUD ปิด จะให้ลายเซ็นนี้ทั้งหมด
- **ชื่อแผง/ข้อความ "ได้ไม้" ไม่ปรากฏใน ML log และ dump.cs (ข้อความอยู่ในตาราง localization ไม่ใช่ binary)**
  ⇒ ระบุแผงจากไฟล์ไม่ได้ — คงสถานะ UNKNOWN เดิม แต่**ขอบเขตลดลง**: เป็นผลผ่าน gate 3 ทาง ไม่ใช่ hang/freeze
  กระบวนการ (heartbeat พิสูจน์แล้ว) — ถ้าผู้ใช้เล่าว่าตอน stall หน้าจอมีอะไร (popup ไม้? แผง speShow? HUD หาย?) จะระบุเงื่อนไขได้ทันที

## 5) สิ่งที่ยังเปิด (ไม่เปลี่ยนสถานะเกณฑ์ใด)

1. เงื่อนไข freeze ตอนเริ่มงาน (3 ทาง/panel ไม่ active — ทางไหน) = ต้องวัด runtime หรือคำบรรยายจอของผู้ใช้
2. ตัวตนแผง popup "ได้ไม้"/popup กลางงาน = เดิม
3. ไม่ได้ตรวจ: พฤติกรรมที่ N ใหญ่/เครื่องช้า (housekeeping อาจกินมากกว่า 1 เฟรม) — นอกขอบเขตชุด A
4. ข้อสรุปนี้**ไม่เปลี่ยน** เกณฑ์/ป้าย/ตัวเลขของ v2a-report ใด — ป้าย UNRESOLVED_OFFSET คงเดิม (PROVISIONAL)

## 6) หลักฐานประกอบในโฟลเดอร์นี้

- `scan_058_dt_writers.py` + `scan_058_dt_writers.tsv` — สแกน writer ที่สอง (อ่าน GameAssembly.dll อย่างเดียว)
- ตัวเลขรายช่วงใน §3 คำนวณจาก NDJSON ที่ commit แล้วใน `evidence/V2A-a1r2…a3r3/` (คำนวณซ้ำได้ด้วยสคริปต์ใน report นี้ — inline python)
