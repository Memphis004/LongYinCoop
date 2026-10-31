# TIME-PLAN (ร่าง) — ระบบเวลาและ Activity Lifecycle ของ LongYinCoop

- **label:** `TIME-plan-draft-20261028` · **สถานะ: อนุมัติเฉพาะขอบเขต V2 ชุด A — มติ V2A-1 (Gate 0) + V2A-2 (D9) + V2A-3 (Gate 5a) แนบวลีเริ่ม session 2026-10-29 (หลักฐาน `evidence/AUTO-20261029-013511/HANDOFF.md`) · V2-A รันปิดแล้ว PROVISIONAL (`evidence/V2A-20261029-020816/v2a-report.md`) · ส่วน V1 / Gate 2–4 / ชุด B–C ยังรอ gate ตาม §F** · **วันที่:** 2026-10-28
- **ขอบเขตการร่าง:** planning/documentation only — อ่านหลักฐานที่มีอยู่แล้วเท่านั้น · **ไม่เปิดเกม · ไม่ attach/invoke/hook ·
  ไม่ build/deploy · ไม่แตะเซฟ/pref/static field · ไม่เปลี่ยน `TestMode` · ไม่แก้ source/protocol/STAGEPLAN ·
  ไม่แก้รายงาน P-T0/T5 และโน้ตเวลาเดิม · ไม่ commit/tag/stash/reset** · ไฟล์ใหม่ของรอบนี้ = ไฟล์เดียว (แผนนี้)
- **ไฟล์นี้ไม่ใช่การอนุมัติ** runtime verification, implementation, protocol change หรือ NPC reduction — ทุก phase
  เริ่มได้เฉพาะเมื่อผู้ใช้อนุมัติ gate ที่เกี่ยวข้องตาม §F ใน session แยก
- **แยกจากแผน NPC-reduction ชัดเจน:** แผนนี้ไม่มีขอบเขตร่วมกับ `sp5-t0-plan.md` (SP5 — บัญชี NPC ตอนสร้างโลก)
  ไม่มี hook ฝั่ง NPC/creation round ใด ๆ — แม้ทั้งสองแผนใช้โปรโตคอล save-split เดียวกัน ก็เป็นแผน/stage/การอนุมัติ แยกกันโดยเด็ดขาด
- **anchor ตอนร่าง:** mod repo HEAD `df46de1` · game repo HEAD `c672dc2` · ป้ายหลักฐาน: **OBSERVED** (อ่านจาก
  disasm/body/source) · **RUNTIME** (วัดจากเกมจริง — session ก่อน) · **DUMP** (dump.cs/script.json) ·
  **INFERRED** (อนุมาน) · **UNKNOWN** (หลักฐานไม่พอ) · **NOT-RUN** (ต้อง runtime) · **SPEC** (กติกาของเรา)
- **เอกสารหลักฐานที่อ้าง (มีอยู่จริง ตรวจ path แล้ว 2026-10-28):**
  - `evidence/TIME-T5-P0-20261028-011213/p-t0-static-followup-audit.md` (P-T0 — ตัวหลักของ §B)
  - `evidence/TIME-T5-20261028-003723/activity-multiplayer-audit.md` (T5)
  - `evidence/TIMEstatic-20261027-051647/time-static-notes.md` (+ `xref-summary.txt`, `bodies/`)
  - `time-audit-notes.md` · `time-notes.md` · `time-followup-notes.md` · `area-notes.md` (P1-5/P1-6/P1-7)
  - `STAGEPLAN.v3-draft.md` §15–§21 (SP2/H6, กติการ่วม) · `sp5-t0-plan.md` (แผนแยก — อ้างเพื่อระบุว่าไม่เกี่ยว)
- **ความสัมพันธ์กับ STAGEPLAN.v3:** แผนนี้อยู่ใต้เส้นหลักฐานของ SP2 (world clock) / H6 และกติการ่วม §15–§19 ของ
  `STAGEPLAN.v3-draft.md` — **ไม่แก้ลำดับ stage หรือคำถาม §20 ใด ๆ** · ผล V1/V2 เป็นขาเข้าของขั้น P-T1+ ใน T5 §10
  · ถ้าเอกสารนี้ขัดกับ STAGEPLAN.v3-draft หรือ AGENTS.md ให้**หยุดและรายงาน** ไม่เลือกเอง

---

## A. Objective และ Non-goals

### A.1 Objective

ยืนยันพฤติกรรมจริง (runtime) ของระบบกิจกรรม/เวลาของเกมใน 4 หัวข้อ ก่อนออกแบบหรือแก้ระบบเวลาใน mod:

1. **Activity lifecycle** — เริ่มงาน/จบงาน/เริ่มทับงาน ทำอะไรกับ state ของ `WorkingUIController` จริงบนจอ
2. **Cancellation** — ยกเลิกผ่าน UI (`TryCancelWorking`) และ forced path (`MiddleStopWork`) ให้ผลอะไรกับ
   state/เงิน/ทรัพยากร/รางวัลจริง ๆ
3. **Resource/reward persistence** — ผลรายวัน (`workingFuc`) และ housekeeping ของวันที่ผ่าน ย้อนคืนได้หรือไม่
4. **T(N)** — เวลา elapsed จริงของงาน N วัน ที่ rate ปกติและตอนกด `>>` เทียบกับโครงสูตรจาก disasm

ผลลัพธ์ของแผนนี้ = decision register ที่ปิดครบ (§C) + ข้อมูลวัดจริงของ V1/V2 (§D/§E) ซึ่งเป็นขาเข้าบังคับก่อน
Gate 5/6 (ออกแบบ implementation ของระบบเวลาใน mod)

### A.2 Non-goals (ห้ามทำภายใต้แผนนี้ทุกกรณี)

- **ห้ามแก้ NPC logic ใด ๆ** — งานลด NPC/บัญชี creation เป็นของ SP5 (`sp5-t0-plan.md`) แยกเด็ดขาด
- **ห้ามแก้ protocol** — ไม่เพิ่ม/เปลี่ยน message ใน `S2/Protocol/S2Types.cs` (แบบร่าง `AREA_*`/`TIME_ACTION_*`
  ที่ท้าย `area-notes.md` ยังไม่ผ่านการอนุมัติ — ไม่ถือเป็นข้อตกลง)
- **ห้ามแก้ production code ของ mod (`src/`)** — ไม่มี build/deploy ใด ๆ ในแผนนี้ นอกจาก probe build ที่จะ
  ขออนุมัติแยกใน Gate 3 (V1) หรือ Gate 5a/5b/5c (V2 รายชุด) (log-only เท่านั้น)
- **ห้ามแก้ระบบ multiplayer** — คิว host/validator/pause-policy ยังไม่ออกแบบจนผ่าน Gate 5
- **ห้ามเขียน static field ใด ๆ** — การเขียน `dayTime`/`pauseTime` เป็นส่วนของ V2 ต้องได้อนุมัติ**แยก**ก่อนทำจริง (§E.4)
- **ห้ามเขียน/เปลี่ยน pref ใด ๆ รวมถึง `TestMode`** — อ่านได้อย่างเดียว (OBSERVED: ทุกจุดที่พบเป็นการอ่านผ่าน
  `PlayerPrefDictionary.GetInt`; กฎ repo ข้อ 5)

### A.3 สถานะของ Option A (ต้องอ่านก่อนอ้างแผนนี้)

Option A — "Native Pump เป็น executor และ Host serialize กิจกรรมผ่าน queue" (T5 §7.2) — **ยังไม่เป็นข้อยุติ**
ให้ถือเป็น **candidate เท่านั้น** ที่ต้องรอ (ก) ผล V1/V2, (ข) มติผู้ใช้ใน §C.3 (ข้อ D3) และ (ค) การรีวิว
implementation plan แยกใน Gate 6 · T5 เองระบุว่าข้อเสนอนั้นเป็น "คำแนะนำเท่านั้น — ไม่ใช่การอนุมัติ implement"
· ทางเลือก B (activity manager ต่อผู้เล่น) และ C (แยก elapsed ออกจากปฏิทิน) ยังเปิดอยู่

---

## B. Evidence baseline (จาก P-T0 + T5 — แยกสถานะทุกข้อ)

> ขอบเขตของ static scan เดิม: E8/E9 ทั้ง .text ที่ validate ทุกจุด + FF15 (พบ 0 จุด) + slot/string resolve —
> **ไม่ครอบ** register-indirect call, binding ใน asset/scene, reflection/SendMessage dispatch และ generic sharing
> ⇒ ทุกจำนวน "จุดที่พบ" เป็น **lower bound** และห้ามใช้คำว่า "ครบทุก caller" (P-T0 §2, T5 §2.2)
> RVA ทั้งหมดอ้างจาก `GameAssembly.dll` `33c56def…7920c1` ตรง `VERSION.md`

### B.1 `StartWorking` ไม่มี working guard — เขียนทับ activity state ได้ [OBSERVED เชิงโค้ด]

- ทุก path เริ่มงานที่ถอด (6p `0x9CABF0`, 7p `0x9CB090`, 3p, `List<string>`,
  `PlotController.StartWorking(string)` `0x6603F0`, `PlayerQuickTravel`, `WaitUIController.SureButtonClicked`)
  **ไม่มีการอ่าน `working` (0x48) ก่อนเขียน state เลย** — เช็คเดียวคือ gameObject (0x18) สำหรับ SetActive
  (P-T0 §3.1, §5.2)
- 6p/7p เขียนชุดเดิม **unconditional**: `working=1` (0x9CAC65/0x9CB105), `leftWorkingDay=dayNum`,
  `totalWorkingDay=0`, `nextDayTime=0`, `resourceNum=0`, `leftPauseTime=pauseTime` (initial, 0x9CACAA),
  `workName/callFuc/callFucParam/finishCallFuc/finishCallFucParam`, `noCancel`
- **ผลข้างเคียงของการทับ [OBSERVED เชิงโค้ด]**: (1) `finishCallFuc` เดิมถูกเขียนทับ ⇒ งานเดิม**ไม่มีทางยิง
  finishFuc** (งานเดิมสูญเงียบ — ค่าใช้จ่ายที่จ่ายไปแล้ว เช่นเงินค่าฝึก/เงิน QuickTravel ไม่ถูกคืนโดยกลไกที่ถอด);
  (2) `workFinished` เดิมค้าง; (3) `skipping` เดิมค้าง; (4) วันแรกของงานใหม่เริ่มหลังพัก initial pause เสมอ
- พฤติกรรมบนจอตอนทับ = **NOT-RUN** (T5 V4 — ไม่บังคับก่อน V1/V2 ตาม P-T0 §8 G6)

### B.2 `workFinished` และ `skipping` ไม่ถูก reset ใน start paths — latch ค้างข้ามงาน [OBSERVED]

- **`workFinished` (0x60)**: writers ที่ถอดได้มี 2 จุด — กิ่งจบงานของ Update (0x9CBCF6 → 1) และ `StopWorking`
  (0x9CB25F → arg) · StartWorking ทุก overload + `PlotController.StartWorking(string)` **ไม่เขียน 0x60**
  ⇒ เป็น **latch ผลงานล่าสุด**: หลังจบงานปกติครั้งแรก ค่าค้าง 1 ตลอดงานถัดไปจนกว่างานนั้นจบ/ถูก StopWorking
  (P-T0 C9)
- **กติกาที่ถูกแทนที่ [OBSERVED → CORRECTED]**: เดิม repo ใช้ "งานกำลังเดิน = `working && !workFinished`"
  (area-notes S4c, T5 §4.3) — กติกานี้**ใช้ไม่ได้ตั้งแต่งานที่สองเป็นต้นไป** · ทดสอบที่ถูกคือ **`working == 1`**
  (ทั้งกิ่งจบงานและ StopWorking เคลียร์ `working`)
- **`skipping` (0x62)**: ไม่มี store ใน start/stop/finish paths ที่ถอด (6p/7p/StopWorking/TryCancelWorking/
  Update รวมกิ่งจบงาน/`PlotController.StartWorking(string)`) · writers ที่พบ = `SkipButtonClicked` (toggle ตรง
  0x9CAA9F) และ `SetSkippingState` (0x9CA928) ซึ่ง xref E8/E9 เดี่ยวพบ **0 direct caller** ⇒ งานใหม่หลังกด `>>`
  ค้างจะเดินที่ threshold ×0.2/×0.5 ทันทีจนกว่าจะมีใคร toggle ปิด (P-T0 C10)
- **ข้อจำกัด**: ครอบเฉพาะ writers ใน bodies ที่ถอด — writer อื่นนอก bodies ไม่ได้สแกนทั้งโมดูล; และ
  `SetSkippingState` 0 direct caller **ไม่พิสูจน์ว่าไม่มี indirect dispatch** (SendMessage/virtual/serialized) =
  UNKNOWN (ดู §H5)

### B.3 เงื่อนไข `TryCancelWorking`, forced path `MiddleStopWork` และการยิง `finishFuc` [OBSERVED]

- **`TryCancelWorking`** (0x9CB330): guard = `leftWorkingDay>0 && noCancel==0` (0x9CB3A9/0x9CB3B3) —
  **ไม่มี**เช็ค `working` และ**ไม่มี**เช็ค `workFinished` ใน guard ⇒ ยกเลิกได้เฉพาะช่วงนับวันที่เหลือวันอยู่ ·
  **ระหว่าง pause (รวม final 2×pause) `leftWorkingDay==0` ⇒ TryCancel กลับเงียบ ๆ** — แต่ forced path ยังใช้ได้
- **`MiddleStopWork`** (0x54EEA0) = forced path: เช็คเพียง Instance null แล้ว tail-jmp `StopWorking(false)`
  (0x54EF55) — **ไม่เช็ค noCancel ซ้ำ**
- **`StopWorking(finished)`** (0x9CB220): `workFinished=finished` → **ยิง `finishFuc` เสมอ** (SendMessage
  1-arg 0x9CB2C2 / 2-arg 0x9CB2FA — ไม่มี branch อ่าน `finished` นอกจากตอนเขียน 0x60) → `working=0` + SetActive(false)
  · กิ่งจบงานปกติของ Update (0x9CBD5D/0x9CBD99) ยิงเหมือนกัน ⇒ **ผลต่างระหว่างจบ/ยกเลิกเกิดที่ handler ปลายทาง**
  (ดู B.4)
- **เส้นทางเข้า `StopWorking(finished)` แยก 2 กลุ่ม — ห้ามยุบรวม** [OBSERVED]:
  - **(a) UI-cancel flow — ผ่าน plot-choice**: ปุ่มยกเลิกใน scene (ผูกนอก call graph — gaps) →
    `TryCancelWorking` (0x9CB330 — guard 2 เงื่อนไข) → **สร้าง plot ยืนยัน + ผูก choice** `"中止;MiddleStopWork"`
    (slot 0x1D74540; ข้อความ "确定要中止{0}吗？" slot 0x1D99870) → `PlotController.MiddleStopWork` (0x54EEA0,
    tail-jmp 0x54EF55) → `StopWorking(false)` — `TryCancelWorking` เอง**ไม่ได้**เรียก StopWorking โดยตรง
    · **ข้อจำกัดหลักฐาน**: static ยืนยันเฉพาะว่า `TryCancelWorking`**สร้าง** plot ยืนยันและผูก choice แบบ string
    dispatch ไว้ — **"ผู้ใช้ผ่าน dialog ยืนยันเสมอ" และ dispatch จาก choice ไป `MiddleStopWork` จริงบนจอ =
    UNKNOWN/NOT-RUN** (ปลายทาง plot-choice ผูกใน scene/asset — อยู่นอก call graph; time-static-notes §2.7,
    T5 gaps) ⇒ **ให้ V1 (TC-1) verify ด้วย hook ที่ `MiddleStopWork` + `StopWorking`** (§D.1 ข้อ 5) ว่า flow
    เดินครบบนจอจริงก่อนจะอ้างเส้นนี้เป็นข้อเท็จจริงเต็มรูป
  - **(b) เรียก `StopWorking` โดยตรง — ไม่ผ่าน TryCancelWorking/plot-choice**: `MiddleStopWork` ถูกเรียกตรงจาก
    ที่อื่น (เช่นการ invoke ของ TC-3a/TC-3b — ไม่มี guard ซ้ำ) และกลุ่ม per-day handler ที่ตัดงานเอง
    (`HospitalCureInjury` ×2, `ForceTeachNPCOneDay`, `PlayerContinuePrisonOneDay`, `RestInHotel`) — ค่า
    `finished` ที่กลุ่มนี้ส่งเข้า `StopWorking` ยังไม่ถอดรายจุด [UNKNOWN] (B.4 แถวสุดท้าย)

### B.4 handler ที่ตรวจแล้ว — cancellation gate ต่างกัน [OBSERVED; ครอบเฉพาะที่อ่าน]

| handler (RVA) | gate | เมื่อถูกยกเลิก (`workFinished=false`) | refund/persistence |
|---|---|---|---|
| `PlotController.RealStartStudySkill` 0x56FF80 → `StudySkillController.RealStartStudySkill` 0xFE5490 | **gate workFinished สองชั้น** (0x57002C-30 + 0xFE55CB) | return เงียบ — ไม่ได้วิชา | เงินค่าฝึกถูกหักก่อนเริ่ม (`StartStudySkill` 0xFE59D0 `ChangeMoney(−)`) — **ไม่พบ refund ในสายที่ถอด**; refund จริง = NOT-RUN |
| `PlotController.StudyMaxFightSkillFinish` 0x671140 | **gate workFinished** (0x67123E-42) | ข้ามทั้งหมด — ไม่ได้สกิล | — |
| `PlotController.StudyFightSelfFinish` 0x66CC40 | **ไม่มี gate** (ไม่มี read 0x60 ทั้ง body) | **เหมือนจบปกติทุกประการ** | exp ต่อวันถูกให้แล้วโดย workingFuc `"GetStudyFightExp;"` (slot 0x1DB50D8) และคงอยู่ [OBSERVED per-day credit + INFERRED no-reversal] |
| `PlotController.FinishWorkInProductionBuilding` 0x4F2920 | **gate `totalWorkingDay>0`** (0x4F2B46) — ไม่ใช่ workFinished | ผ่าน gate ตราบใดที่ทำไป ≥1 วัน ⇒ dialog สรุปผล/เหตุการณ์ hero สุ่ม**น่าจะเกิดหลังยกเลิกด้วย** [INFERRED — พฤติกรรมจอ NOT-RUN, G2] | ทรัพยากรถูกจ่ายรายวันโดย `WorkInProductionBuilding` (per-day, RUNTIME area-notes P1-5) — ไม่พบ reversal ในทั้งสอง body |
| Wait — ไม่มี handler (`callFuc=null`) | — | ผลมาจาก day-tick housekeeping ล้วน | — |
| กลุ่ม per-day handler ที่เรียก `StopWorking` **โดยตรง** (เส้นทาง (b) ของ B.3): `HospitalCureInjury` ×2 (0x53A96D, 0x53ABD8), `ForceTeachNPCOneDay` (0x4FC67E), `PlayerContinuePrisonOneDay` (0x557777), `RestInHotel` (0x57A05C) | ยังไม่ถอด | — | body ยังไม่ถอด ⇒ ผล resource/refund และค่า `finished` ที่ส่ง = **UNKNOWN** |

**กติกาการอ่านตาราง**: "ไม่พบ refund ใน handler ที่อ่าน (4 ตัว + สายเริ่งฝึก) **≠ ระบบไม่มี refund**" —
finish-handler อีก 40+ ตระกูลและเส้น refund ระดับ plot/scene ยังไม่ถอด [UNKNOWN] — ห้ามตีความเป็นข้อเท็จจริงเชิงลบ
(ดู §H4)

### B.5 Quick Travel ใช้ activity pump เดียวกัน และไม่มี working guard [OBSERVED]

- `GameController.PlayerQuickTravel` (0x7D9750): `PlayerLeaveArea` ก่อนเริ่ม → `dayNum =
  BigMapPos.QuickTravelTime(playerPos, areaPos)` → **จ่ายเงินก่อนเริ่ม** (`ChangeMoney` 0x7D99A7) →
  `StartWorking(6p)`: workName=`"乘车"`, `callFuc=""` (สตริงว่าง — ไม่มี per-day callback),
  `finishCallFuc="PlayerQuickTravelToArea"` (finishCallFucParam โหลดนอกหน้าต่างที่ถอด — UNKNOWN)
- **ไม่มี guard เช็ค `working` เลยทั้ง method** (0 read ของ `[reg+0x48]`; เช็คเดียวคือ Instance null 0x7D9A73)
  ⇒ การเดินทางไว**แย่ง singleton pump กับงานอื่น** และเริ่มทับงานที่กำลังทำได้ตาม B.1
- area membership เปลี่ยนระหว่าง "งานเดินทาง" (`PlayerLeaveArea` ก่อนเริ่ม ⇒ atAreaID −1 ระหว่างเดินทาง) [INFERRED
  จาก PlayerLeaveArea + finish-teleport]

### B.6 โครง T(N) จาก disasm [OBSERVED เชิงโครง — ตัวเลขยังไม่ runtime-confirmed]

ลำดับจริงต่อหนึ่งวันจาก `WorkingUIController.Update` (0x9CB4B0, 586 insns — P-T0 §6.1):

```
StartWorking: [initial pause = p]   (ตั้งที่ 0x9CACAA — จุดที่โมเดลเดิมของ T5 ไม่ได้นับ)
  day1(dayTime×k) → [p + anim] → day2(dt×k) → … → dayN(dt×k) → [2×p, ไม่มี anim] → (เฟรมถัดไป) FINISH
```

**โครงสูตร: `T(N) = N × dayTime × k + (N+2) × pauseTime`** โดย `k = 1.0` ปกติ / `0.5` ตอน skipping (TestMode≠1) /
`0.2` ตอน skipping (TestMode==1) — ตัวคูณ skip กระทบ**เฉพาะ day threshold** (0x9CBA92–0x9CBB12) · **pause ไม่ย่อ
ด้วย skip** [โครง — ยังไม่เคยวัดที่ rate ใหม่] · workingFuc รายวันยิง**ก่อน** day-tick (`SendMessage` 0x9CBB9C
มาก่อน `ChangeHour(24)` 0x9CBC30) และ callback อาจหยุดงานเอง (re-check `working` 0x9CBBE0)

- สถานะโมเดล: M-DISASM เดิม `(N+1)×pauseTime` = **RETRACTED** (C8); M-PROMPT `(N+2)` = โครงได้รับการยืนยันจาก
  disasm แล้ว; M-RUNTIME `N×(dt+p)` โครงไม่ตรง disasm
- **ตัวเลข RUNTIME ที่รายงานไว้สำหรับเคสนี้มีค่าเดียว**: กรณี 10 วัน ที่ `dayTime=1.0, pauseTime=0.25` ≈
  **12.5 วิ** [RUNTIME — `area-notes.md` P1-5 บันทึกการวัดไว้ครั้งเดียว] เทียบโครงสูตรที่ N=10 ได้ 13.0 วิ —
  ต่าง 2p = 0.5 วิ ซึ่งอยู่ในความหยาบของการวัดเดิม (hook cadence 0.75–2.9 วิ) ⇒ **ตัดสินไม่ได้ — ยังเป็น
  UNKNOWN จนกว่า V2 จะวัด** (ห้ามเลือกโมเดลจากค่าวัดที่รายงานไว้ค่าเดียว) · หมายเหตุสัญกรณ์ "n=1" ในเอกสาร
  ต้นทาง (กำกวม — ไม่มีนิยาม) ดู §H1
- **ห้ามอ้างสูตรนี้เป็น "runtime-confirmed"** — ที่ยืนยันคือ**โครงสร้าง** (ลำดับ/จำนวน pause/ตำแหน่ง finish) จาก
  disasm เท่านั้น (ดู §H2)

### B.7 เวลา elapsed จริงและ inter-day animation ยังต้องวัด [UNKNOWN → V2]

1. ตัวเลข T(N) ที่ `dayTime` ใหม่ (8–12 วิ/วัน) — ไม่เคยวัด
2. เวลาแอนิเมชัน inter-day (`AddAnimation("TimeAnim")` + `PlaySound(0.4)` ที่ 0x9CBE62–0x9CBEB6) — ไม่รวมใน
   `dayTime`/`pauseTime` ขนาดจริง UNKNOWN
3. พฤติกรรม skip ที่ rate ใหม่ — โครงทำนายว่า pause เต็ม `(N+2)p` ไม่ย่อ (ที่ dt=1, k=0.5, N=10 ⇒ ขั้นต่ำ 8 วิ)
   ยังไม่เคยวัด
4. freeze 3 ทาง native (`plotHappen` / `speShowUIPanel.activeSelf` / `HudPanelActive()`) + การ์ตูนของแต่ละงาน
   รวมเป็น noise ที่ต้องวัดจริง

### B.8 `TestMode == 1` ทำให้จำนวนวันจาก Wait slider ถูกคูณ 100 [OBSERVED]

- `WaitUIController.SureButtonClicked` (0xC14E4E): `dayNum = (int)slider × (GetInt("TestMode")==1 ? 100 : 1)`
  (0xC14E15–0xC14E39) — Wait ใช้ overload `(int,string)`, `callFuc=null` (ไม่มี per-day callback) · slider 1–30
  [RUNTIME area-notes P1-7]
- ผลเดียวกันกับตัวคูณ skip: threshold ×0.2 (TestMode==1) / ×0.5 และ anim ×5.0/×2.0
- **`TestMode` เป็น pref ที่อ่านอย่างเดียว — ห้ามเขียนทุกกรณี** · ค่าปัจจุบันบนเครื่อง (0 หรือ 1) **ยังไม่มี
  หลักฐาน** ⇒ ก่อนทดสอบ Wait ใด ๆ ต้องยืนยันค่าก่อน (V2 §E.3 — ไม่งั้น "Wait 30" อาจกลายเป็น 3,000 วัน)
- **เวลาแอนิเมชันที่วัดจริง (V2 §E.1) ต้องรายงานแยกตามโหมด/ตาม TestMode** — ตัวคูณ anim ต่างกัน (ปกติ ×1.0 /
  skip ×2.0 เมื่อ TestMode≠1 / skip ×5.0 เมื่อ TestMode==1) — ห้ามรวมเป็นค่าเดียว และสูตรเชิงโครงของ B.6 คงอยู่
  แยกต่างหากจากโมเดล elapsed (ห้ามเรียกโมเดล elapsed ว่า runtime-confirmed ก่อนวัดจริง)

### B.9 บริบท MP ที่ผูกกับการตัดสิน (จาก T5)

- **singleton** — state งานทั้งชุดเป็น field ของ object เดียวต่อโปรเซส + `dayTime/pauseTime` เป็น static
  (ค่า live 1.0/0.25 [RUNTIME area-notes P1-5]) ⇒ native pump มีกิจกรรมได้ **1 งานเท่านั้น** และ static rate
  เป็นของทั้งโปรเซส [OBSERVED โครง + INFERRED ผลทับ]
- **ปั๊มใช้ unscaled delta** (`RealTime.get_deltaTime` → tail-jmp `Time.get_unscaledDeltaTime` 0xCFD4A2) ⇒
  `timeScale=0` ไม่หยุดปั๊ม [OBSERVED] — global pause ต้อง gate สายปั๊มฝั่ง mod เอง
- **protocol ปัจจุบันไม่มี activity lifecycle** — guest→host allowlist = `Auth/Ping/Resync/Bye` เท่านั้น
  (`S2/Protocol/S2Types.cs:58-70`); grep src ไม่พบ activity/pause/area message [OBSERVED]
- **state ปั๊มไม่อยู่ในเซฟ** (ไม่มี working field ใน WorldData; `GameSaveData = WorldData + HeroList +
  TempHeroList` [DUMP]) ⇒ งานค้างน่าจะหายหลังโหลด [GUESS — NOT-RUN] · ผู้ตั้ง `needAutoSave` ยังไม่พบ [UNKNOWN]
- **mod ปัจจุบันไม่มี write path ไปยัง activity state เลย** (grep `Working|workFinished|leftWorkingDay` ใน
  `src/` = 0 hit นอก comment) — และแผนนี้**ไม่**เพิ่ม write path (นอกจากข้อเสนอ V2 §E.4 ที่ต้องอนุมัติแยก)

---

## C. Decision register — คำถามที่ต้องปิดก่อน implementation

> กติกา: ห้ามตัดสินข้อใน §C.3 แทนผู้ใช้ · ทุกข้อมี "ปิดด้วยอะไร" กำกับ · ข้อที่ยังเปิดจะถูกยกไปรีวิวที่ gate ที่เกี่ยว

### C.1 สิ่งที่หลักฐานตัดสินได้แล้ว (ไม่ต้องตัดสินซ้ำ)

| ข้อ | ข้อยุติ | ป้าย |
|---|---|---|
| E1 | เงื่อนไข "งานกำลังเดิน" ที่ใช้ได้ = **`working == 1`** — `working && !workFinished` ใช้ไม่ได้ตั้งแต่งานที่สอง | OBSERVED → CORRECTED (B.2) |
| E2 | `finishFuc` ถูกยิง**เสมอ** ทั้งจบปกติและยกเลิก — ผลต่างเกิดที่ handler ปลายทาง | OBSERVED (B.3) |
| E3 | `noCancel` ระดับ overload: 6p hardcode 0 และ 5p/2p/List/3p delegate → 6p ⇒ false ทุกงานโดยโครง; 7p = arg จาก caller (จุดเดียวที่ pin ได้ `true` = `TryThinkAboutMissingSkill` งาน "参悟") | OBSERVED (P-T0 §3) |
| E4 | โครงสูตร T(N) = `N×dayTime×k + (N+2)×pauseTime`; skip ย่อเฉพาะ day threshold, pause ไม่ย่อ [โครง] | OBSERVED เชิงโครง (B.6) |
| E5 | `TestMode==1` คูณวัน Wait ×100; `TestMode` อ่านอย่างเดียว | OBSERVED (B.8) |
| E6 | QuickTravel = กิจกรรมปั๊มเต็มรูป ไม่มี working guard | OBSERVED (B.5) |
| E7 | ปั๊ม unscaled — `timeScale` ไม่พอ gate | OBSERVED (B.9) |
| E8 | singleton = กิจกรรมขนานระหว่างผู้เล่นทำไม่ได้โดยโครง | OBSERVED โครง (B.9) |
| E9 | mod ปัจจุบันไม่มี activity write path — ข้อความ "แก้เงื่อนไขใน mod ได้แล้ว" ใด ๆ = เท็จ | OBSERVED (B.9) |

### C.2 สิ่งที่ต้องทดสอบ runtime

| ข้อ | ช่องว่าง | ชุดทดสอบ |
|---|---|---|
| R1 | ผลเงิน/ทรัพยากร/buff จริงตอนยกเลิก (refund มี/ไม่มี รายกลุ่มงาน) | V1 — TC-1/TC-2 |
| R2 | พฤติกรรมจอของ `FinishWorkInProductionBuilding` เมื่อถูกยกเลิก (dialog โผล่หลัง cancel? hero สุ่มยังเกิด?) | V1 — TC-2 |
| R3 | `workFinished` stale บนจอ/ต่อ logic ที่ใช้อยู่จริงของงานที่สอง | V1 — TC-4 |
| R4 | ตัวเลข T(N) + initial pause/final 2×pause + เวลาแอนิเมชัน ที่ rate ปกติและ rate ใหม่ | V2 |
| R5 | skip ที่ rate ใหม่ (pause ไม่ย่อจริงไหม) | V2 |
| R6 | พฤติกรรมจอตอน "เริ่มทับงานที่กำลังทำ" | V4 ของ T5 — **ไม่บังคับก่อน V1/V2** |

### C.3 สิ่งที่ต้องให้ผู้ใช้ตัดสิน (ห้ามตัดสินแทน)

| ข้อ | คำถาม | ปิดที่ gate |
|---|---|---|
| D1 | cancellation ทดสอบ/ใช้งานผ่าน**ปุ่ม UI เท่านั้น** หรืออนุญาต invoke `MiddleStopWork` ด้วย — ถ้าเฉพาะ UI จะไม่ครอบเคส forced-cancel (ต้องบันทึกข้อจำกัด) | Gate 2/3 |
| D2 | ยอมรับเป็นหลักการว่า **ผลรายวันที่ให้ไปแล้วอาจย้อนคืนไม่ได้** (per-day credit ถาวรทันทีที่ยิง — B.4) | Gate 2 |
| D3 | รูปแบบ activity serialization/ownership สำหรับ multiplayer — Option A = **candidate เท่านั้น** (§A.3); ทางเลือก B/C ยังเปิด; รวมถึงความหมาย (1)/(2)/(3) ของ "กิจกรรม N วันใช้เวลา X วินาที" (T5 §6.1) — **มติ 2026-10-31: เลือก Option B (activity manager ต่อผู้เล่น) — decisions-pending §13** | Gate 5/6 |
| D4 | policy เมื่อมีคำขอเริ่มงานใหม่ขณะมีงานเดิม (เข้าคิว / ปฏิเสธ / ยอมให้ทับแบบเกม — ทับแล้วงานเดิมสูญเงียบ ตาม B.1) — **มติ 2026-10-31: ทับแบบเกม — decisions-pending §13** | Gate 5/6 |
| D5 | policy ต่อ `noCancel` (งานที่ห้ามยกเลิกทาง UI), forced cancellation (`MiddleStopWork`) และผลของ finish handlers แต่ละตระกูล (gate/no-gate/totalWorkingDay — B.4) — **มติ 2026-10-31: คงพฤติกรรมเกมเดิม — decisions-pending §13** | Gate 5/6 |
| D6 | policy สำหรับค่า N นอกชุดที่ทดสอบ {1,5,10,15} (linear fallback / default rate / ปฏิเสธ) — ยังไม่กำหนดแทนผู้ใช้ — **มติ 2026-10-31: ล็อกสูตร (ฉ) `max(30, 12×N)` วิ ทุก N ในขอบเขต ACT-1; นอกขอบเขต (งานเลี้ยง) คงปั๊ม native — decisions-pending §13** | Gate 5/6 |
| D7 | วิธีจัดการ `workFinished`/`skipping` ที่ค้างข้ามงาน — mod ต้องใช้ `working==1` (E1) และอาจต้องเคลียร์/หลีกธงค้างเองถ้าจะใช้ skip ใน MP (T5 §11.11) — **มติ 2026-10-31: อนุญาต skip + mod เคลียร์/หลีกธงเอง — decisions-pending §13** | Gate 5/6 |
| D8 | ข้อเท็จจริง E6 (QuickTravel + กิจกรรมอื่นใช้ปั๊มเดียวกัน) มีผลต่อ MP อย่างไร — travel ของผู้เล่นหลายคนผ่านคิวเดียวหรือไม่ — **มติ 2026-10-31: แยกคิวต่อผู้เล่น — decisions-pending §13** | Gate 5/6 |
| D9 | สถานะ baseline/fixture เซฟก่อน V1 — ตอนนี้ `Save` diverge จาก baseline ที่อนุมัติขณะนั้น `4226730f66ff` 5 ไฟล์ (SaveSlot8 จากการเซฟของผู้ใช้เอง 2026-10-24 02:15 + `PlayerprefData.dat`) — **มติ D9 แล้ว 2026-10-28 ทาง (ก) rebaseline volatile-only: baseline ที่อนุมัติปัจจุบัน = `df8e5a11beed6e62cc18d40da9ae0f2d0d5fbfdc56640b4196ece95df1ed4920` @ `coop-test/baselines/20261028-071340` (มติ [G-2] + V2A-2 — ดู VERSION.md หัวข้อ "Baseline ที่อนุมัติปัจจุบัน")** — ต้องเลือก **(ก) rebaseline**: บันทึก manifest/digest ใหม่แล้วใช้ manifest นั้นเป็นเกณฑ์ preflight/rollback ทั้งหมด หรือ **(ข) คง baseline เดิม + บันทึก divergence**: แยก "original approved baseline" (`4226730f66ff`) ออกจาก "approved test fixture ปัจจุบัน" ระบุรายการ divergence ที่อนุมัติ และใช้เกณฑ์ตรวจคืน fixture ให้ตรงกับ **snapshot ก่อนทดสอบ** (§D.1/§G.2) · **ห้ามอ้างว่า fixture ที่ diverge ตรง baseline เดิมแบบ EXACT** · การบันทึก/ใช้ manifest ทั้งสองทางต้องเป็นไป
ตามนิยาม "ห้ามแตะเซฟจริง" (§D.0) · ถ้ายังไม่มีมติ D9 ⇒ Gate 1 ไม่ผ่าน ห้ามเปิดเกม (มติ D9 แล้ว 2026-10-28 ทาง (ก) — ดู VERSION.md) | Gate 1 |
| D10 | ทดสอบขอบ slider Wait หรือไม่ — **อนุญาตเฉพาะหลังยืนยันค่า `TestMode` แล้ว** (§E.3) — **มติ 2026-10-31: ทำ (เงื่อนไข TestMode คงเดิม) — decisions-pending §13** | Gate 5c |

### C.4 สิ่งที่อาจต้องกลับไปตรวจ static เพิ่ม (ทำได้ต่อ อ่านอย่างเดียว — ไม่บังคับก่อน V1/V2)

- prologue ของ StartWorking call sites ที่เหลือ (43 จุดจาก 47 external) — pin args อื่น (dayNum/callFuc/finishFuc) ราย site
- finish-handler เพิ่มจากกลุ่ม 40+ ตระกูลที่ยังไม่ถอด (ปิดช่อง refund ให้แคบลงก่อนตัดสิน D2/D5)
- writer ของ `needAutoSave` (สแกน store `byte [reg+0x28]` ในบริบท GameController)
- ลำดับเงื่อนไขเต็มของ `ChangeDay()` leaf (housekeeping) จาก `bodies/GameController_ChangeDay_leaf.txt`
- หา dispatch ทางอ้อมของ `SetSkippingState` (scene/asset/reflection) ถ้าจะใช้ skip ใน MP จริง (D7)

---

## D. V1 — Cancellation / persistence verification (ร่างขั้นทดสอบ — เริ่มได้เฉพาะหลัง Gate 3)

> **เงื่อนไขเริ่ม — dependency ของ V1 = Gate 0–3** (ตาม §F.0: Gate 0–2 เป็นข้อกำหนดก่อนเริ่ม, Gate 3 = ผู้ใช้
> ประกาศอนุมัติ V1 เป็นครั้งแยก; Gate 4 รีวิวผล**หลัง**รัน) — **ข้อความในแผนนี้ไม่ใช่การอนุมัติรัน**
> · ทุกอย่างบน**สำเนาเซฟ**เท่านั้น ตามโปรโตคอล §1.2 ของ STAGEPLAN · วัดที่ rate ปกติ (`dayTime=1.0`) — V1 **ไม่ต้อง**
> เขียน static field ใด ๆ

### D.0 นิยามและกฎการจัด session/สำเนา (บังคับทั้ง V1 และ V2)

- **test case (TC)** = สถานการณ์ทดสอบหนึ่งเรื่อง มี state เริ่มต้น/precondition นิยามชัด ·
  **test set (ชุด)** = กลุ่ม TC ที่แชร์เงื่อนไข (rate/โหมด/TestMode — ชุด A/B/C ของ §E.2) ขออนุมัติและรีวิว
  รายชุด · **session** = หนึ่งรอบเปิดเกมบนสำเนาที่เตรียมไว้หนึ่งชุด ปิดด้วย restore + verify ตาม §G.2
- **กฎ 1 — state ต่อเนื่อง ⇒ session เดียว**: เคสที่ผลของขั้นหลังขึ้นกับ state จากขั้นก่อน (ปัจจุบัน: TC-4/TC-4b —
  งานแรกจบปกติแล้วเริ่มงานที่สอง) ต้องทำ**ทุกขั้นใน session เดียวกัน** โดยไม่มี restore/ปิดเกม/เปลี่ยนสำเนาคั่น
- **กฎ 2 — เคสอิสระ ⇒ สำเนาแยกต่อเคส**: เคสที่ไม่พึ่ง state ของเคสอื่น (TC-1, TC-2, TC-3a, TC-3b; การวัด T(N)
  แต่ละค่า N/dt/โหมด **รวมถึงรอบซ้ำเพื่อวัด noise ทุกรอบ**) ใช้**สำเนาใหม่ (fresh split) ต่อเคส** และ restore +
  verify ปิดเคสนั้นก่อนเริ่มเคสถัดไป — เพื่อกัน confound จากธงค้าง (`workFinished`/`skipping`) ของเคสก่อนหน้า
- **กฎ 3 — ห้าม restore คั่นกลางลำดับต่อเนื่อง**: ห้าม restore/ปิดเกมคั่นกลางเคสภายใต้กฎ 1 ทุกกรณี — แม้ขั้นย่อย
  พลาด ให้จบเคสแบบบันทึกผลจริงแล้วจึง restore
- **นิยาม "ห้ามแตะเซฟจริง" (ใช้เหมือนกันทุกจุด: D9 / §D.1 / §G.1 / §G.2)**:
  (1) **ห้ามแก้ไข/เขียนทับเนื้อหาไฟล์ใด ๆ ของเซฟจริง และห้ามลบไฟล์ของเซฟจริง** ทุกกรณี — ไม่ว่าชุดข้อมูลนั้นจะ
  อยู่ที่ตำแหน่ง/ชื่อใดตาม protocol (ชื่อ `Save.real-*` ระหว่าง aside หรือกลับมาที่ `Save` หลัง rename กลับ);
  (2) การเปลี่ยนตำแหน่ง/ชื่อโฟลเดอร์ (aside → ทำงาน → rename กลับ) ทำได้**เฉพาะขั้นตอนที่ save-split protocol
  (§1.2) กำหนดและได้รับอนุมัติไว้แล้ว** เท่านั้น;
  (3) **digest/manifest ใช้ติดตามข้อมูลเซฟจริงชุดเดิม (dataset) ข้ามการ rename — ไม่ขึ้นกับชื่อ/path ของโฟลเดอร์**:
  ตรวจ digest/manifest **ก่อนและหลัง**ทุกการเปลี่ยนตำแหน่ง/ชื่อ และหลัง rename กลับ ณ ตำแหน่งปลายทางที่ protocol
  กำหนด เพื่อยืนยันว่า**เนื้อหาไม่เปลี่ยน** (ที่เปลี่ยนได้มีแต่ตำแหน่ง/ชื่อ — ไม่ใช่เนื้อหา);
  (4) ถ้า protocol ที่ใช้อยู่ไม่สามารถรับประกันข้อ (1)–(3) ได้ (เช่น ไม่มี digest ก่อน/หลัง, rename กลับไม่ได้ผล
  ตามคาด, preflight ตรวจคืนไม่ได้) **หรือไม่สามารถระบุ/ยืนยันตัวข้อมูลเซฟจริงหลัง rename กลับได้อย่างปลอดภัย**
  ⇒ **หยุดและรายงานผู้ใช้ทันที ห้ามสร้างขั้นตอนย้าย/restore ใหม่ขึ้นเอง**

### D.1 Prerequisites

1. `save-split` ตาม §1.2 — ทำ**ต่อเคส/session ตามกฎ §D.0** (สำเนาใหม่ต่อเคสอิสระ) พร้อม**ขั้นตรวจ digest/manifest
   ของข้อมูลเซฟจริงชุดเดิม 3 จุด** (ติดตาม dataset ข้าม rename ไม่ผูก path — นิยาม §D.0 ข้อ (3)):
   (ก) **ก่อน aside** — บันทึก digest/manifest ของเซฟจริง ณ ตำแหน่งเดิม (digest อ้างอิงของ session นี้);
   (ข) **หลัง aside** — ตรวจข้อมูลที่ย้ายตำแหน่งแล้ว (ณ ตำแหน่ง aside ตาม protocol) เทียบกับ digest ก่อน aside
   (ต้องตรง — เนื้อหาไม่เปลี่ยน); แล้วจึง `Save` = สำเนา → `save-split preflight` = **READY ก่อนเปิดเกมเสมอ**;
   (ค) **หลัง restore (ปิด session)** — ตรวจข้อมูลเซฟจริงที่กลับมาอยู่ภายใต้ชื่อ/path ปลายทางตาม protocol เทียบกับ
   digest ก่อน aside — ขั้นนี้ทำที่ §G.2 ข้อ 1
2. baseline/fixture digest **ตามมติ D9** — ตรวจก่อนเปิดเกมทุกเคส: ทาง (ก) rebaseline ⇒ ตรวจ `Save` ตรง
   manifest/digest ใหม่ที่บันทึกไว้ **EXACT**; ทาง (ข) คง baseline เดิม ⇒ ตรวจ `Save` (fixture) ตรง
   **snapshot ก่อนทดสอบที่บันทึกไว้ตอน split** (ซึ่งต้องตรง fixture ที่อนุมัติในมติ D9(ข) — รายการ divergence
   5 ไฟล์คงอยู่ตามเดิม) — **ห้าม**ตรวจ fixture เทียบ original baseline `4226730f66ff` แบบ EXACT ·
   **ข้อมูลเซฟจริง (ขณะ aside อยู่ที่ `Save.real-*`) = ตามนิยาม "ห้ามแตะเซฟจริง" §D.0** (ห้ามแก้/เขียนทับ/ลบเนื้อหา;
   เปลี่ยนตำแหน่ง/ชื่อได้เฉพาะขั้นที่ protocol กำหนด พร้อม digest ก่อน–หลัง) ·
   **ถ้ายังไม่มีมติ D9 ⇒ Gate 1 ไม่ผ่าน ห้ามเปิดเกม**
3. game hash ตรง `VERSION.md` ทั้งสองไฟล์ (GameAssembly.dll + global-metadata.dat) — ไม่ตรง ⇒ หยุดทันที (กฎ repo ข้อ 9)
4. ไม่เขียน pref ใด · จด sha256 ของ `MelonPreferences.cfg` + `UserData\LongYinCoop\net.cfg` ก่อน/หลัง
5. probe: hook **log-only** ที่ `StopWorking` 0x9CB220 (entry: เห็น `finished` arg), จุดยิง finishFuc
   0x9CB2C2/0x9CB2FA (log ชื่อ handler + ค่า workFinished ขณะยิง) และ `MiddleStopWork` 0x54EEA0 (entry —
   แยกเส้นทาง (a)/(b) ของ §B.3 และ verify dispatch จาก plot-choice ที่ยังเป็น UNKNOWN) — ถ้าต้อง build probe
   DLL ใหม่ = ขออนุมัติ build/deploy ภายใน Gate 3 (log-only เท่านั้น, rollback ตาม §G.2)
6. ผู้ใช้กด UI เองทุกเคสที่เป็น UI; การ invoke (`TC-3`) ทำได้เฉพาะเมื่อมติ D1 อนุญาต
7. อ่าน state ทั้งหมดผ่าน typed interop แบบมีชนิด (กฎ repo ข้อ 8): `WorkingUIController.Instance` fields
   0x48–0x62, `ItemListData.money`, `ForceData.resourceStore`, `WorldData` worldTime/hour — อ่านอย่างเดียว

### D.2 Test cases และลำดับ (ห้ามสลับ)

| TC | การจัด session/สำเนา (§D.0) + เงื่อนไขก่อน | ขั้นตอน | อ้างอิง |
|---|---|---|---|
| **TC-1** | **เคสอิสระ — สำเนาใหม่ 1 session** (กฎ 2) · เงินพอสำหรับค่าฝึก | เริ่มฝึก 2 วัน (สาย `StartStudySkill` — ผู้ใช้กด) → ระหว่างนับวัน**ยกเลิกผ่านปุ่ม UI** (เส้นทาง (a): TryCancelWorking → plot-choice) | R1 สายฝึก |
| **TC-2** | **เคสอิสระ — สำเนาใหม่แยกจาก TC-1** (กฎ 2 — ไม่ผูกกับผล TC-1) | เริ่มงานผลิต 10 วัน → ให้จบไป 3 วัน (สังเกตทรัพยากรเข้ารายวัน) → ยกเลิกผ่านปุ่ม UI | R1/R2 สายผลิต (gate `totalWorkingDay>0`) |
| **TC-3a** (เฉพาะมติ D1 อนุญาต invoke) | **เคสอิสระ — สำเนาใหม่** (กฎ 2) · งาน noCancel=0 กำลังนับวัน | invoke `PlotController.MiddleStopWork()` **ระหว่างนับวัน** (เส้นทาง (b) — เรียก StopWorking โดยตรง ไม่ผ่าน plot-choice) | ยืนยัน forced path |
| **TC-3b** (เฉพาะมติ D1 อนุญาต invoke) | **เคสอิสระ — สำเนาใหม่แยกจาก TC-3a** (กฎ 2) · งาน noCancel=0 | ให้งานเดินถึง**หน้าต่าง final 2×pause** (`leftWorkingDay==0`) แล้ว invoke `MiddleStopWork()` — พิสูจน์ว่า TryCancelWorking เงียบในช่วงนี้ (B.3) แต่ forced path ยังตัดได้ | ยืนยัน forced path ใน pause window |
| **TC-4 (+TC-4b)** | **state ต่อเนื่อง — session เดียว สำเนาเดียว ห้าม restore คั่น** (กฎ 1 + กฎ 3) | งานแรก**จบปกติ** (ไม่ยกเลิก) → **เริ่มงานที่สองใน session เดียวกัน** → อ่าน `workFinished` ระหว่างงานที่สองกำลังเดิน (คาดค้าง = 1 จากงานแรก) → สังเกตว่างานที่สองเดิน/จบปกติไหม, UI/สถานะผิดปกติไหม, logic ใดที่จอแสดงอาการ — (TC-4b ต่อเนื่องใน session เดียวกัน ถ้าผู้ใช้อนุญาต: ยกเลิกงานที่สองผ่าน UI แล้วยืนยัน handler ของงานที่สองอ่าน workFinished ที่ถูก set สด = false) | R3 — ยืนยัน latch และว่ามีผลต่อ logic ที่ใช้อยู่จริงหรือไม่ |

ลำดับที่เสนอ: TC-1 → TC-2 → TC-3a → TC-3b (ตามมติ D1) → TC-4/TC-4b — ปิดแต่ละเคสด้วย restore + verify
ก่อนเคสถัดไป (กฎ 2) เว้นแต่ TC-4/TC-4b ซึ่งต่อเนื่องภายในเคสเดียว (กฎ 1/3)

### D.3 สิ่งที่ต้องสังเกต (ทุก TC — ก่อน/ระหว่าง/หลัง)

- **state**: `working/leftWorkingDay/totalWorkingDay/nextDayTime/leftPauseTime/workFinished/skipping` + `noCancel`
  ของงานนั้น · ค่าต้องเทียบตัวเลขบนจอ (นับวัน/UI label) ทุกครั้ง
- **เงิน**: `ItemListData.money` ก่อนเริ่ม/หลังยกเลิก (ค่าฝึก/ค่า QuickTravel ถูกหักก่อนเริ่ม — ตรวจว่าคืนไหม)
- **ทรัพยากร**: `ForceData.resourceStore` (+ resourceChange) รายวัน และหลังยกเลิก — ตรวจว่าวันที่ผ่านคงอยู่ไหม
- **log**: การยิง finishFuc (จาก hook) — ยิงจริงไหมตอนยกเลิก, ชื่อ handler, ค่า workFinished ณ จุดยิง
- **dialog/UI**: dialog ที่โผล่หลังยกเลิกโดยเฉพาะ TC-2 (ผลจาก `FinishWorkInProductionBuilding` — R2) · ปุ่ม/แผง
  งานที่ค้าง · soft-lock
- **ปฏิทิน**: วันที่เดินจริงกี่วัน (ต้องเท่าวันที่ผ่านจริง — ไม่นับรวม pause)
- **เซฟ**: digest สำเนาก่อน/หลัง TC แต่ละตัว (สำเนาเปลี่ยนได้ตามปกติ) + digest/manifest ของ**ข้อมูลเซฟจริงชุดเดิม**
  (ติดตาม dataset ข้าม rename ไม่ผูก path — นิยาม §D.0 ข้อ (3)) ครบ 3 จุดตรวจของ §D.1 ข้อ 1 และ §G.2 ข้อ 1
  (เนื้อหาจริงต้องไม่เปลี่ยน)

### D.4 Expected — แยก "จาก static" ออกจาก "hypothesis"

| สิ่งที่คาด | สถานะของความคาด |
|---|---|
| finishFuc ยิงเสมอทั้งจบและยกเลิก | จาก static [OBSERVED] — runtime เป็นการยืนยันซ้ำ |
| TC-1: ไม่ได้วิชา, เงินค่าฝึกยังถูกหักค้าง, **ไม่พบ refund** | "ไม่พบ refund" จาก static = ไม่เห็น call คืนเงินในสายที่ถอด — **refund ไม่มีจริงหรือไม่ยังเป็น hypothesis จนกว่าเงินวัดจริง** |
| TC-2: ทรัพยากรวันที่ 1–3 คงอยู่หลังยกเลิก (per-day credit ถาวร) | จาก static + RUNTIME เดิม (P1-5) — คงอยู่จริง = ยืนยัน R1 ฝั่ง persistence |
| TC-2: dialog สรุปผลโผล่หลังยกเลิก (เพราะ gate `totalWorkingDay>0` ผ่าน) | **hypothesis** จาก static — พฤติกรรมจอยังไม่เคยวัด (R2) |
| TC-4: `workFinished` ค้าง = 1 ตลอดงานที่สอง | จาก static [OBSERVED] — runtime ยืนยัน latch semantics (R3) |
| TC-4: งานที่สองเดิน/จบปกติ ไม่มีอาการผิดปกติบนจอ | **hypothesis** — static ไม่พบ consumer ที่อ่าน 0x60 ระหว่างงานกำลังเดิน (ในขอบเขตที่ถอด) แต่ไม่ได้พิสูจน์เชิงลบทุกเส้น |
| TC-1: flow ยกเลิกทาง UI ผ่าน dialog ยืนยันครบ (TryCancelWorking → plot-choice → MiddleStopWork → StopWorking(false)) ทำงานจริงบนจอ | **UNKNOWN/NOT-RUN** — static เห็นเฉพาะการ**สร้าง** plot ยืนยัน + การผูก choice แบบ string dispatch (§B.3 ข้อจำกัดหลักฐาน) — ต้อง verify ใน V1 ด้วย hook ที่ `MiddleStopWork` + `StopWorking` (§D.1 ข้อ 5) |
| ปฏิทินเดินเท่าวันที่ผ่านจริงทุก TC | จาก static (day-tick ต่อวันนับ) |

### D.5 ข้อจำกัดของผล V1 (บันทึกไปกับรายงานเสมอ)

- ห้ามสรุป "ไม่มี refund" ไปกว่า handler/สายที่วัด — handler อีก 40+ ตระกูลยังไม่ถอด (§H4)
- ห้ามสรุปพฤติกรรมของงานตระกูลอื่นที่ไม่ได้ทดสอบ (แต่ละ finish handler gate ต่างกัน — B.4)
- ผล TC-3a/TC-3b ใช้ได้เฉพาะเมื่อมติ D1 อนุญาต invoke; ถ้าเฉพาะ UI ⇒ เคส forced-cancel และหน้าต่าง final 2×pause =
  NOT-RUN พร้อมบันทึกข้อจำกัด

### D.6 Stop conditions ของ V1 — ดู §G.1 (ใช้ร่วมทุก phase)

### D.7 Rollback ของ V1 — ดู §G.2 แบบมาตรฐาน (restore + verify digest + คืน pref)

---

## E. V2 — T(N) runtime verification (ร่างขั้นทดสอบ — เริ่มได้เฉพาะหลัง Gate 4+5)

> **เงื่อนไขเริ่ม — dependency ต่อเส้นทางตาม §F.0 (การอนุมัติชุดหนึ่งไม่รวมชุดอื่นโดยปริยาย)**: ชุด A = ผ่าน
> Gate 0 + Gate 1 (มติ D9 + `save-split preflight` — **บังคับเพราะเป็นการทดสอบ runtime แม้ไม่เขียน static field**)
> + อนุมัติชุด A โดยตรง (Gate 5a) — ไม่ต้องรอ Gate 2–4 · ชุด B = ผ่าน Gate 0–4 **และ** อนุมัติเขียน static
> `dayTime`/`pauseTime` **ชัดเจนแยกต่างหาก** (Gate 5b — หลังอ่านค่าเริ่มต้นได้และตรงเงื่อนไข §E.4) · ชุด C/Wait =
> ผ่าน Gate 0 + Gate 1 + Gate 5c (**ยังปิด** จนกว่ายืนยัน `TestMode` §E.3 + มติ D10 + อนุมัติเคสเฉพาะ) ·
> ทุกชุด: บนสำเนาเซฟเท่านั้น ตาม §1.2 และผ่าน preflight ก่อนเปิดเกมทุกเคส

### E.1 สิ่งที่วัด — แยกตัวแปรครบทุกชุด

| ตัวแปร | วิธีวัด |
|---|---|
| จำนวนวัน N (ตั้งใจ) | จาก args ของ StartWorking (`dayNum`) ที่ hook entry จับ |
| จำนวน `ChangeDay` leaf hits | hook log-only ที่ leaf 0x790E30 — **นับเฉพาะช่วงงาน** (ห้ามมีการเดินทาง/กิจกรรมอื่นแทรก — ถ้า leaf เกิน N ⇒ หยุดตาม §G.1) |
| `dayTime` / `pauseTime` | อ่าน static ก่อนเริ่มทุกชุด (ค่าปกติ 1.0/0.25) + ค่าที่ตั้ง (ถ้าชุด B) |
| โหมด normal vs skipping | บันทึกสถานะ `skipping` ตลอดชุด + คูณ k ที่สังเกตได้ |
| initial pause / inter-day pause / final 2×pause | timestamp เหตุการณ์ log-only: StartWorking entry (E1), จุด day-tick ก่อน ChangeHour (E2 0x9CBC30), จุด pause-complete/AddAnimation (E3 0x9CBE62), กิ่งจบงาน (E4 0x9CBCC7), StopWorking entry (E5) — ช่วงเวลา derive จาก gap ระหว่างเหตุการณ์ |
| เวลา wall-clock รวม | E1 → E4 (หรือ E5 ถ้ายกเลิก — ไม่นับเป็นผล T(N)) |
| measurement noise | ทำซ้ำอย่างน้อย 3 รอบต่อการวัด — **รอบซ้ำแต่ละรอบนับเป็นเคสอิสระ = สำเนาแยกต่อรอบ** (§D.0 กฎ 2), บันทึก fps/กรอบ freeze ที่เกิด, ห้ามเล่น action อื่นระหว่างวัด |

โมเดลเทียบ (แยกจากผลวัดเสมอ — §H2): คาด wall-clock ≈ `N×dt×k + (N+2)×p + (N−1)×t_anim` — นิพจน์นี้เป็น**โมเดล
เพื่อการวัด (hypothesis อนุมานจากโครง disasm)** **ไม่ใช่** runtime-confirmed และจะยืนยันได้ต่อเมื่อ V2 วัดจริงเท่านั้น ·
**`t_anim` ต้องวัดและรายงานแยกตามโหมด/ตาม TestMode** (ตัวคูณ anim ต่างกัน: ปกติ ×1.0, skip ×2.0 เมื่อ TestMode≠1,
skip ×5.0 เมื่อ TestMode==1 — B.6/B.8) จึงได้ `t_anim,normal` และ `t_anim,skip(TestMode=…)` ห้ามรวมเป็นค่าเดียว ·
สูตรเชิงโครงจาก disasm (B.6) คงอยู่**แยกต่างหาก**จากโมเดล elapsed นี้ตลอด · เกณฑ์ผ่าน/ไม่ผ่านกำหนด**ก่อนวัด**ที่
Gate รายชุด (5a/5b/5c — ตามชุดที่วัด) (ตามร่าง: โครงผ่านเมื่อ leaf hits == N และช่วง pause ตรงโครง; ตัวเลข elapsed
รายงาน Δ จากสูตรโดยแยก t_anim รายโหมดออก — **ถ้าไม่ตรง ห้ามปรับโมเดลเอามั่ว ให้บันทึกผลจริงและหยุดรายงาน**)

### E.2 ชุดทดสอบที่เสนอ (เรียงความเสี่ยงต่ำ → สูง, หยุดรีวิวระหว่างชุดได้)

| ชุด | เงื่อนไข | เนื้อหา | ความเสี่ยง |
|---|---|---|---|
| **A — rate ปกติ (ไม่เขียน field)** | Gate 0 + Gate 1 (preflight บังคับ — runtime test) + Gate 5a อนุมัติชุด A โดยตรง (ไม่ต้องรอ Gate 2–4 — ตาม §F.0) | N ∈ {1, 5, 10} โหมด normal ที่ `dayTime=1.0, pauseTime=0.25` (โครงทำนาย 1.75 / 6.75 / 13.0 วิ) + งานเดียวกัน N=10 หนึ่งรอบโดยกด `>>` ค้าง (กติกา skip บังคับ §E.3 ข้อ 5; บันทึก k ที่เกิดจริง 0.5/0.2 พร้อมยืนยัน TestMode ตาม §E.3 ข้อ 1–2) | ต่ำสุด — ไม่มี write ใด |
| **B — rate ใหม่ (ต้องอนุมัติ §E.4)** | Gate 0–4 (ตาม §F.0) + **Gate 5b — อนุมัติเขียน `dayTime`/`pauseTime` ชัดเจนแยกต่างหาก** (หลังเงื่อนไข §E.4 ผ่าน) | N ∈ {1, 5, 10} ที่ `dayTime=8` (เริ่มค่าเดียว) → ถ้าตรงโครง ขยาย {N=10 × dt ∈ {9,10,12}} และ {N=15 × dt=8} — pauseTime คง 0.25 ตลอด · ทำซ้ำหนึ่งเคสแบบกด `>>` เพื่อวัด skip ที่ rate ใหม่ (R5 — กติกา skip บังคับ §E.3 ข้อ 5) | ต้อง restore ค่าหลังเสร็จทุกครั้ง (§E.4) |
| **C — Wait (ปิดจนกว่า TestMode ยืนยัน)** | Gate 0 + Gate 1 + Gate 5c — ยืนยัน `TestMode` (§E.3) + มติ D10 + อนุมัติเฉพาะเคส (ไม่ต้องรอ Gate 2–4 — ตาม §F.0) | เริ่มจาก slider=1 พร้อมเช็ค `leftWorkingDay` ทันทีหลังเริ่ม (§E.3 ข้อ 3) — **ห้ามสั่ง Wait 30 ตรง ๆ** | ดู §E.3 |

- N=15/dt สูงมีเวลารวมนาน (เช่น 15×12+17×0.25 ≈ 184 วิ) — จัดท้ายชุด B และทำเฉพาะเมื่อ N=10 ตรงโครงก่อน
- ทุกชุด: จบแล้ว restore สำเนา + verify digest ตาม §G.2 (นิยาม "ห้ามแตะเซฟจริง" §D.0) ก่อนชุดถัดไป (กัน state
  ค้างข้ามชุด) · การจัด session/สำเนาภายในชุดทำตามกฎ §D.0 (เคสอิสระ = สำเนาแยกต่อเคส รวมถึงรอบซ้ำ)
- **การอนุมัติชุดใดชุดหนึ่ง (5a/5b/5c) ไม่รวมสิทธิ์ชุดอื่นโดยปริยาย** — แต่ละชุดขออนุมัติและรีวิวแยก

### E.3 ความเสี่ยง `TestMode` — ต้องยืนยันก่อนทดสอบ Wait ใด ๆ (และห้ามเปลี่ยนค่าเสมอ)

`TestMode==1` ทำให้ Wait slider ×100 (B.8) ⇒ ถ้าสั่ง "Wait 30" ขณะ TestMode==1 จะได้ **3,000 วัน** — การลดความเสี่ยง
ที่เสนอ (ทำตามลำดับ, หยุดทันทีที่สัญญาณขัดกัน):

1. **อ่านค่าแบบ read-only** ผ่านเส้นทางเดียวกับเกม (`PlayerPrefDictionary.GetInt("TestMode")` — log-only, ห้ามเขียน)
2. **ยืนยันทางพฤติกรรมอีกสัญญาณ**: หนึ่งรอบกด `>>` ระหว่างงานสั้น — ตัวคูณที่เกิด (0.5 vs 0.2) และความเร็ว anim
   (×2 vs ×5) ระบุ TestMode ได้อิสระจากข้อ 1 (สองสัญญาณต้องตรงกัน ไม่ตรง = หยุดรายงาน)
3. **ถ้าจะทดสอบ Wait**: เริ่ม slider=**1** เสมอ → อ่าน `leftWorkingDay` **ทันทีหลัง StartWorking** — ถ้าค่า ≠ 1
   (เช่น = 100) ⇒ TestMode==1 active ⇒ ยกเลิกงานทันทีด้วยเส้นทางที่มติ D1 อนุญาต (ตรวจ TryCancel ผ่านได้เพราะ
   `leftWorkingDay>0` และ `noCancel==0`) แล้ว**หยุด Wait testing ทั้งหมด** รายงานผู้ใช้
4. **ห้ามเขียน/เปลี่ยน `TestMode` ทุกกรณี** ทั้งในรอบวัดนี้และภายหลัง (กฎ repo ข้อ 5 + B.8)
5. **กติกา skip — บังคับทุกเคสที่กด `>>`/toggle `skipping`** (ชุด A รอบ skip และชุด B เคส skip):
   (ก) **snapshot สถานะ `skipping` (0x62) ก่อนเริ่มเคสและก่อนเริ่มทุกรอบ** — `StartWorking` **ไม่** reset
   skipping (B.2) — **ห้ามสมมติว่าเริ่มงานใหม่เคลียร์ให้**
   (ข) ตรวจสถานะ**ก่อน/หลังการกด toggle ทุกครั้ง** (กด → อ่านกลับยืนยันว่าธงเปลี่ยนตรงคาด)
   (ค) ปลายเคส: คืน `skipping` กลับเป็นค่าจาก snapshot (กด toggle กลับ) แล้วอ่านกลับยืนยันตรง snapshot
   (ง) ถ้า snapshot ค่าเริ่มต้นไม่ได้ หรือ**คืนสถานะอย่างปลอดภัยไม่ได้** (อ่านกลับไม่ตรง snapshot) ⇒ **ยกเลิกเคส
   skip ของชุดนั้นทั้งหมด** และรายงานข้อจำกัด (skip ที่ rate นั้น = NOT-RUN) — เคสที่ไม่เกี่ยวกับ skip ทำต่อได้ปกติ

### E.4 การเขียน static `dayTime`/`pauseTime` — ต้องอนุมัติแยกก่อนทำจริง

- ตามรายงาน (T5 §9, P-T0 §9) การเขียน static field ของเกมเป็น **write ครั้งแรกของ mod ต่อ field เกม** —
  จึงต้องได้รับ**อนุมัติเขียนแยกต่างหาก** ทั้งจากการอนุมัติชุด A (Gate 5a) และจากการรีวิวผล V1 (Gate 4) —
  เจาะข้อนี้ชัด ๆ ใน Gate 5b
- เขียนผ่าน typed interop เท่านั้น (กฎ repo ข้อ 8) ก่อน StartWorking ของงานเป้าหมาย (ให้ทันก่อน day แรก —
  `nextDayTime` เริ่ม 0)
- **บันทึกค่าจริงของ `dayTime`/`pauseTime` ก่อนเริ่ม V2 (typed interop read-only) — ค่า rollback = ค่าที่บันทึกไว้
  จริง ไม่ใช่ค่าสมมติ** · 1.0/0.25 เป็นเพียง**ค่าคาดหมาย**จากการวัดในอดีต [RUNTIME area-notes P1-5] ใช้
  cross-check เท่านั้น
- **เงื่อนไขก่อนเขียน static field**: ค่าก่อนเริ่มต้อง (ก) **อ่านได้สำเร็จ** และ (ข) **ตรงค่าคาดหมาย 1.0/0.25** —
  ถ้าอ่านไม่ได้ หรือค่าไม่ตรง (มีอะไรแก้ไปก่อนหน้าโดยไม่รู้) ⇒ **หยุดทันที ห้ามเขียน static field** รายงานผู้ใช้
  (§G.1 ข้อ 9)
- **แผนคืนค่า**: จบทุก session ต้อง restore `dayTime`/`pauseTime` กลับเป็น**ค่าที่บันทึกไว้ก่อนเริ่ม** → อ่านกลับ
  ยืนยันตรงเป๊ะ → ยืนยันด้วยพฤติกรรมที่ rate ที่บันทึกไว้ (ถ้าค่าบันทึก = 1.0/0.25: งาน 5 วัน ≈ 6.75 วิ) → จึงปิด
  session
- ถ้า restore ไม่ได้ หรืออ่านกลับไม่ตรงกับค่าที่บันทึก = หยุดตาม §G.1 ทันที

### E.5 Stop conditions และ rollback ของ V2 — §G.1 ร่วม + §E.4 (คืน dayTime/pauseTime) + §G.2

---

## F. Gates และ dependency ของแต่ละเส้นทาง (ห้ามข้าม dependency ที่ระบุ — ทุก gate อนุมัติโดยผู้ใช้ใน session ที่มีการประกาศชัดเจน)

### F.0 Dependency ต่อเส้นทาง (สรุปบังคับ — รายละเอียดราย gate ในตารางถัดลงไป)

| เส้นทาง | ต้องผ่านครบทุกข้อก่อนเริ่ม | ไม่ต้องรอ |
|---|---|---|
| **V1** | Gate 0 → Gate 1 (มติ D9 + `save-split preflight` + game hash) → Gate 2 (D1+D2) → Gate 3 (อนุมัติ V1) | — · Gate 4 เป็นการ**รีวิวหลังรัน** ไม่ใช่เงื่อนไขเริ่ม |
| **V2 ชุด A** | Gate 0 → Gate 1 (มติ D9 + preflight + hash — **บังคับเพราะเป็นการทดสอบ runtime แม้ไม่เขียน static field**) → Gate 5a (อนุมัติชุด A โดยตรง) | Gate 2–4 |
| **V2 ชุด B** | Gate 0 → Gate 1 → Gate 2 → Gate 3 (รัน V1 จริง) → Gate 4 (รีวิวผล V1) → Gate 5b (อนุมัติเขียน `dayTime`/`pauseTime` **แยกต่างหาก** + เงื่อนไข §E.4/§G.1 ข้อ 9 ผ่าน) | — |
| **V2 ชุด C/Wait** | Gate 0 → Gate 1 (เดียวกับชุด A) → Gate 5c (ยืนยัน `TestMode` §E.3 + มติ D10 + อนุมัติเคสเฉพาะ) | Gate 2–4 |

| Gate | เนื้อหา | ผ่านเมื่อ | ผลลัพธ์ |
|---|---|---|---|
| **Gate 0** | รีวิวและอนุมัติ Time Plan ฉบับนี้ | ผู้ใช้รีวิว diff + ประกาศอนุมัติ (หรือสั่งแก้แล้วรีวิวซ้ำ) | แผนมีสถานะ "อนุมัติ" — **ยังไม่มีสิทธิ์เปิดเกม** |
| **Gate 1** | ยืนยัน baseline + save-split preflight | มติ D9 (rebaseline หรือคง divergence แบบบันทึก) + `save-split preflight` = READY + game hash ตรง VERSION.md | สิทธิ์เตรียม session วัด — **ยังไม่มีสิทธิ์รัน V1/V2 ชุดใด** (ต้องมี Gate 3 หรือ Gate 5a/5b/5c ตาม dependency ของเส้นทางอีกชั้น) |
| **Gate 2** | มติผู้ใช้เรื่อง cancellation และผลถาวรของรายวัน | ตอบ D1 + D2 ครบ | เงื่อนไขของ TC-3 และกติการายงาน V1 ชัดเจน |
| **Gate 3** | อนุมัติ V1 **แยกต่างหาก** (รวม probe build ถ้าจำเป็น) | ผู้ใช้รีวิว §D ขั้นสุดท้าย + ประกาศอนุมัติรัน V1 | รัน V1 ได้ใน session ที่กำหนด |
| **Gate 4** | รีวิวผล V1 (หลังรัน V1 เสร็จ) | รายงาน V1 รีวิวแล้ว — ตัดสิน R1/R2/R3 + ปรับ §C ตามผล | เป็นขาเข้าของ **Gate 5b เท่านั้น** — **ไม่ใช่**เงื่อนไขของ Gate 5a/5c (ชุด A/C ใช้ dependency ของตัวเองตาม §F.0) |
| **Gate 5a** | อนุมัติ V2 **ชุด A** เฉพาะตัว (rate ปกติ ไม่เขียน static field) | ผ่าน Gate 0 + Gate 1 (preflight บังคับ — runtime test) + ผู้ใช้รีวิว §E.2 ชุด A + ประกาศอนุมัติชุด A **โดยตรง** (แยกจากผล V1) | รัน V2 ชุด A ได้เท่านั้น |
| **Gate 5b** | อนุมัติ V2 **ชุด B** + **อนุมัติเขียน static `dayTime`/`pauseTime` ชัดเจนแยกต่างหาก** | ผ่าน Gate 0–4 ครบ (ตาม §F.0) + อ่านค่าเริ่มต้นได้และตรงเงื่อนไข §E.4/§G.1 ข้อ 9 + ผู้ใช้ประกาศอนุมัติเขียนเป็นข้อแยก | รัน V2 ชุด B ได้เท่านั้น |
| **Gate 5c** | อนุมัติ V2 **ชุด C/Wait** | ผ่าน Gate 0 + Gate 1 + ยืนยัน `TestMode` แล้ว (§E.3) + มติ D10 + ผู้ใช้รีวิว/อนุมัติเคสเฉพาะ | รัน V2 ชุด C ได้เท่านั้น |
| **Gate 6** | รีวิวผล V2 → เสนอ implementation plan **แยก** | รายงาน V2 รีวิวแล้ว — ปิด T(N)/skip + ตัดสิน candidate สถาปัตยกรรม (A/B/C) | implementation plan ใหม่ต้องผ่านรีวิวอีกชั้น — **ยังไม่แก้โค้ดจนกว่าจะอนุมัติ** |

- **ห้ามข้าม dependency ที่ระบุของแต่ละเส้นทาง (§F.0)** — เส้นทางใดต้องผ่าน gate ที่ dependency ระบุให้ครบก่อนเริ่ม
  · gate ที่**ไม่**อยู่ใน dependency ของเส้นทางนั้นไม่ใช่ข้อบังคับ (ไม่เรียกว่า "ข้าม") — แต่**ห้ามอ้าง**การผ่าน gate
  ของเส้นทางหนึ่งไปแทนเงื่อนไขของอีกเส้นทาง · การอนุมัติแผนนี้ (Gate 0) **ไม่เท่ากับ**การอนุมัติรันเกม/รัน V1/V2
- **การอนุมัติ Gate ใด/ชุดใดหนึ่ง ไม่รวมสิทธิ์อื่นโดยปริยาย** — เช่น อนุมัติชุด A (5a) ไม่รวมชุด B (5b)/ชุด C
  (5c); อนุมัติเขียน `dayTime`/`pauseTime` (5b) ไม่รวมสิทธิ์เขียน field อื่นใด; ผ่าน Gate 1 ไม่รวมสิทธิ์รัน V1/V2
- **การตรวจความครบของ dependency (กันการแอบข้ามเงื่อนไข)**: เส้นทาง V1 (§D) และ V2 ชุด A (§E.2 ชุด A)
  ดำเนินได้ครบด้วย dependency ของตัวเอง — เงื่อนไข runtime ทุกข้อ (save-split §1.2 ต่อเคส, preflight READY,
  game hash ตรง VERSION.md, ห้ามเขียน pref, typed interop, hook log-only, กฎ session/สำเนา §D.0) ระบุไว้ใน
  ตัวเส้นทางเองแล้ว ไม่พึ่ง gate ใดนอก dependency ของเส้นทางนั้น
- **ห้ามให้ agent เริ่ม V1/V2 เองอัตโนมัติ** — ทุก phase เริ่มได้เฉพาะเมื่อผู้ใช้ประกาศอนุมัติ gate ที่เกี่ยวใน
  session นั้น ๆ; ถ้า session จบก่อน gate ถัดไปผ่าน ให้หยุดและรอรายงาน
- รายงานปิดแต่ละ phase ต้องมี: ผลวัดเทียบ expected (แยก static/hypothesis), hash เซฟก่อน/หลัง, rollback ทำแล้ว,
  ข้อจำกัด (เช่น NOT-RUN รายเคส) — ตามรูปแบบรายงานของ AGENTS.md

---

## G. Stop conditions และ rollback

### G.1 เงื่อนไขหยุดทันที (ทุก phase — เจอข้อใดข้อหนึ่ง = หยุด รายงาน ห้ามแก้สถานการณ์เอง)

1. save-split หรือ baseline/fixture verification ไม่ผ่าน (รวม: สถานะเซฟไม่ตรงตามมติ D9 — ทาง (ก) manifest ใหม่
   หรือทาง (ข) snapshot fixture ที่อนุมัติ; และ: protocol ไม่สามารถรับประกันนิยาม "ห้ามแตะเซฟจริง" §D.0 ได้)
2. digest/manifest แตกต่างโดยไม่มีคำอธิบาย (ทั้งสำเนาและ**ข้อมูลเซฟจริงชุดเดิม** — เนื้อหาข้อมูลเซฟจริง ณ ตำแหน่ง
   ใดก็ตามตาม protocol (ขณะ aside หรือหลัง rename กลับ) ต่างจาก digest ก่อน aside แม้ 1 ไฟล์ = หยุดทันที ตาม
   นิยาม §D.0 ข้อ (3))
3. มี persistent write ที่ผู้ใช้ไม่ได้อนุมัติ (เช่น การเขียนเซฟ/pref/static field นอกขอบเขตที่อนุมัติ)
4. runtime behavior ขัดกับหลักฐาน static ที่ตาราง §B ยึดอยู่ (เช่น finishFuc ไม่ยิงตอนยกเลิก, ยกเลิกได้ใน
   pause window ผ่าน TryCancel, ทรัพยากรรายวันหายหลังยกเลิก) — **หยุดและรายงานก่อนขยาย ห้ามอนุมานต่อ**
5. จำนวนวันหรือเวลาไม่ตรงกับที่ตั้งใจทดสอบ (เช่น leaf hits ≠ N, `leftWorkingDay` หลังเริ่ม Wait ≠ slider,
   งานจบเร็ว/ชาวผิดปกติ)
6. rollback/verification ทำไม่ได้ (restore ไม่สำเร็จ, digest ตรวจซ้ำไม่ได้, คืน `dayTime/pauseTime` ไม่ได้)
7. เกม crash, soft-lock, หรือเซฟ hash เปลี่ยน (กฎ repo "เมื่อไรต้องหยุดถาม")
8. game hash ไม่ตรง `VERSION.md` ตอนเริ่ม session (กฎ repo ข้อ 9)
9. ค่า `dayTime`/`pauseTime` ก่อนเขียน (V2 ชุด B) **อ่านไม่ได้หรือไม่ตรงค่าคาดหมาย 1.0/0.25** — ห้ามเขียน static
   field และหยุด (§E.4)

### G.2 Rollback (ตามลำดับ — ตรวจยืนยันทุกขั้น)

1. **สำเนา**: `save-split restore --label <label>` (ขั้น rename กลับ = ขั้นตอนที่ protocol §1.2 กำหนด — นิยาม
   §D.0) → ข้อมูลเซฟจริงกลับมาอยู่ภายใต้ชื่อ/path ปลายทางตาม protocol (**ต้องไม่แก้ไข เขียนทับ หรือลบเนื้อหา**) → ตรวจคืน
   **ตามมติ D9**: ทาง (ก) rebaseline ⇒ 57 ไฟล์ตรง manifest ใหม่ **EXACT** + `leftovers: none` → rehash อิสระตรง;
   ทาง (ข) คง baseline เดิม ⇒ fixture ตรง **snapshot ก่อนทดสอบของ session นั้น** (รายการ divergence ที่อนุมัติ
   คงอยู่ตามเดิม — ไม่ถือเป็นความคลาด) + `leftovers: none` — **ห้าม**อ้างว่า fixture ตรง original baseline
   `4226730f66ff` แบบ EXACT · **ข้อมูลเซฟจริงชุดเดิม: เนื้อหาไม่เปลี่ยนทุกขั้น** — หลัง restore ให้ตรวจ dataset
   **ณ ตำแหน่งปลายทางที่ protocol กำหนด** (ไม่ใช่ตรวจที่ชื่อ `Save.real-*` ซึ่งหลัง restore อาจไม่มีอยู่แล้ว)
   เทียบกับ digest **ก่อน** aside (นิยาม §D.0 ข้อ (3) — ครบ 3 จุดตรวจตาม §D.1 ข้อ 1)
2. **pref**: คืนจาก `prefs.before.cfg` (ถ้า `MelonPreferences.cfg` ขยับ) · ตรวจ `net.cfg` ตรงค่าที่จดก่อนเริ่ม
3. **V2 เฉพาะ**: คืน `dayTime`/`pauseTime` เป็น**ค่าที่บันทึกไว้จริงก่อนเริ่ม V2** (ไม่ใช่ค่าสมมติ 1.0/0.25 — §E.4;
   ถ้าค่าก่อนเริ่มอ่านไม่ได้/ไม่ตรงค่าคาดหมาย งานหยุดตั้งแต่ก่อนเขียน จึงไม่มีการเขียนให้คืน) (typed interop) →
   อ่านกลับยืนยันตรงค่าที่บันทึก → ยืนยันพฤติกรรมจอ (§E.4)
4. **probe build** (ถ้า Gate 3 (V1) หรือ Gate 5a/5b/5c (V2) อนุมัติ build): สำรอง DLL เดิมเป็น `.prev-<hash>` ก่อนทับ → rollback = คืน DLL
   เดิม (หรือ `.off`) → verify ว่า mod โหลด/ไม่โหลดตามสถานะก่อน phase · ห้ามแตะ DLL ของ mod อื่น
5. **process**: recorder/hook หยุดสะอาด, ไม่มี orphan process, ไม่มีไฟล์ค้างในโฟลเดอร์เกม
6. บันทึกทั้งหมดใน `evidence/TIME-V1-<ts>/` หรือ `TIME-V2-<ts>/` ตาม convention ของ repo

---

## H. หมายเหตุต่อเอกสารต้นทาง (บันทึกเป็นข้อสังเกต — **ห้ามแก้ไฟล์ต้นทางในรอบนี้**)

1. **สัญกรณ์ "n=1" ในเอกสารต้นทาง (P-T0 §6.2, T5 §4.4) เป็นสัญกรณ์กำกวม** — ตรวจ `area-notes.md` และโน้ตเวลา
   ทั้งชุดแล้ว (2026-10-28): **ไม่พบข้อความใดนิยามว่า "n=1" หมายถึงจำนวนตัวอย่างการวัด** ("n=1" ปรากฏใน
   `time-followup-notes.md:114` และ `time-notes.md:30` ในบริบทอื่นโดยไม่มีนิยามเช่นกัน) ⇒ ทั้งการตีความว่า
   "n = จำนวนตัวอย่าง" และการอ่านเป็น `N=1` ล้วนเป็น**การอนุมานที่ไม่มีข้อความรองรับ — แผนนี้จึงไม่ตีความ** ·
   ข้อเท็จจริงที่ยืนยันได้มีเพียงข้อเดียว: ค่าประมาณ **12.5 วินาทีมาจากกรณี 10 วัน** ที่ `dayTime=1.0,
   pauseTime=0.25` (`area-notes.md` P1-5 [RUNTIME] — ระบุ "10 วัน ≈ 12.5 วิ" ตรงตัว) · แผนนี้จึงอ้างเฉพาะ
   "กรณี 10 วัน" (B.6/B.7) — รอมติผู้ใช้แล้วจึงแก้/ชี้แจงในเอกสารต้นทาง (ห้ามแก้ไฟล์ต้นทางในรอบนี้)
2. **สูตรเชิงโครง ≠ elapsed ที่วัดจริง** — `N × dayTime × k + (N+2) × pauseTime` เป็น**โครงจาก disasm**
   [OBSERVED เชิงโครง] ส่วนตัวเลข elapsed ยังไม่มีการวัดที่ยืนยันได้ (มีตัวอย่างเดียวของกรณี 10 วันที่ rate ปกติ)
   — แผนนี้ห้ามนำสูตรไปกล่าวว่า runtime-confirmed ทุกจุด (B.6, E.1)
3. **แยก `workFinished` (latch ค้างข้ามงาน) ออกจาก `working == 1`** — สอง field สองความหมาย: `working` =
   งานกำลังเดินจริง (เคลียร์ทั้ง finish และ StopWorking), `workFinished` = ผลงานล่าสุด (ไม่ถูก reset ตอนเริ่มงาน)
   — แผนนี้ไม่แอบอนุมานว่า "แก้เงื่อนไขใน mod ได้แล้ว" (mod ยังไม่มีโค้ดแตะ field เหล่านี้เลย — B.9 ข้อสุดท้าย)
4. **"ไม่พบ refund ใน handler ที่อ่าน" ≠ "ทุกเส้นทางไม่มี refund"** — ครอบเฉพาะ 4 handler + สายเริ่มงานฝึก;
   อีก 40+ ตระกูลและเส้น refund ระดับ plot/scene ยังไม่ถอด (B.4, C.4)
5. **`SetSkippingState` ไม่มี direct caller ในขอบเขตที่สแกน ≠ ไม่มี indirect/serialized dispatch** —
   SendMessage/virtual/serialized binding อยู่นอก call graph ที่สแกน (B.2) — ถ้าจะใช้ skip ใน MP ต้องหา
   dispatch ทางอ้อมก่อน (C.4)

---

## I. สถานะ git และความบริสุทธิ์ของ session ร่างแผนนี้

- session ร่างแผนนี้: อ่านอย่างเดียวทั้งหมด — ไม่เปิดเกม, ไม่ attach/invoke/hook, ไม่ build/deploy, ไม่แตะ
  เซฟ/pref/static field, ไม่เปลี่ยน `TestMode`, ไม่แก้ source/protocol/STAGEPLAN/เอกสารต้นทาง, ไม่ commit/tag
- ไฟล์ใหม่ที่สร้าง = ไฟล์เดียว: `time-plan-draft.md` (ที่ root ของ mod repo — ตาม convention แผนที่มีอยู่
  เช่น `sp5-t0-plan.md`, `S3-plan-draft.md`)
- การเปลี่ยนแปลงที่มีอยู่ก่อน session (pre-existing — **ไม่แตะและไม่เกี่ยวกับงานนี้**) รายงานแนบในข้อความ
  ส่งมอบ พร้อม `git status`/HEAD ก่อนและหลังของทั้งสอง repo

---

## ภาคท้าย — สิ่งที่แผนนี้**ไม่ได้อนุมัติ** (อ่านซ้ำก่อนอ้างอิง)

- ❌ การเปิดเกม/attach/invoke/hook ใด ๆ ในวันนี้ (V1 เริ่มได้หลัง Gate 0–3 · V2 ชุด A หลัง Gate 0+1+5a · ชุด B หลัง Gate 0–4+5b · ชุด C หลัง Gate 0+1+5c — ตาม §F.0)
- ❌ การเขียน static field (`dayTime`/`pauseTime`) — อนุมัติแยกใน Gate 5 เท่านั้น
- ❌ การเปลี่ยน `TestMode` หรือ pref ใด ๆ
- ❌ การแก้ source code, protocol, production build ของ mod
- ❌ การแก้ NPC logic / แผน NPC-reduction (SP5 แยกเด็ดขาด)
- ❌ การถือ Option A เป็นข้อยุติสถาปัตยกรรม (§A.3)
- ❌ การตัดสิน D1–D10 แทนผู้ใช้
