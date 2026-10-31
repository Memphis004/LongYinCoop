# A1 static survey — งาน 4 (2026-10-31 · docs-only · ไม่เปิดเกม)

> วิธี: อ่าน `Il2CppDumper/dump.cs` + asm bodies ที่มีอยู่ (`evidence/SP5body-TempHerosDict-20261031-034658/bodies/`)
> · RVA ทุกตัว **re-resolve จาก dump.cs โดยตรง** และ cross-check กับ header ของ body (แสดงรายจุด) · ป้าย: [DUMP] [asm] [INFERRED] [UNKNOWN]

## A1-1 — method ที่สร้างชุดปุ่มวัน + เลือกชุดตามบริบทอาคาร/เจ้าของ

- **เจ้าของข้อมูลตัวเลือก = data รายอาคาร [DUMP]:** `AreaBuildingDataBase.areaBuildingChoices` (`List<AreaBuildingChoice>`, dump.cs:368167 class / :368178 field 0x40) — โครงตัวเลือก = `AreaBuildingChoice { text 0x10, describe 0x18, justNeedOneCondition 0x20, mainCondition 0x28, subCondition 0x30, callFuc 0x38, callFucParam 0x40 }` (dump.cs:367869)
- **สายโซ่ยืนยันด้วย asm [asm]:** choice (`callFuc="StartWorking"`, `callFucParam="ชื่องาน;วัน;callFucParam;finishCallFuc;finishCallFucParam"` — รูปจริงจาก [RUNTIME P1-5] เช่น `巡查;3;;;HelpPatrol;`, `工作;3;;;FinishHelpWork;`) → `PlotController.StartWorking(string param)` (dump.cs:384297–384299, **RVA 0x6603F0** — ตรง body header เป๊ะ) → **`String::Split` (0x0660485) → `Int32::Parse` (0x066052b) → `WorkingUIController::StartWorking` 6p (call 0x0660590 → RVA 0x9CABF0 — ตรง dump.cs:392725)** → ปั๊ม
- **สมมติฐาน "ชุดตัวเลือกขึ้นกับเจ้าของ/บริบทอาคาร" = สอดคล้องเชิงโครง [INFERRED]:** ชุดปุ่ม = data รายอาคาร (`areaBuildingChoices` ต่ออาคาร + conditions รายตัวเลือก) — อาคารต่างบริบท (ตระกูลอื่น vs สำนักตัวเอง) มีชุด choice คนละชุดใน data ไม่ใช่โค้ดเลือก
- **UNKNOWN:** ค่าชุดจริง (1/5/10 · 5/10/15 · 1–5) อยู่ใน **game data assets** (ค่า data ไม่ปรากฏใน dump.cs) — ต้องอ่าน data table (ชื่อ asset ยังไม่รู้) หรือวัดบนจอทีละปุ่ม [RUNTIME]
- **ผลต่อ (ฉ):** patch จุดเดียวที่ `StartWorking` 6p **ครอบทุกงานผ่านปั๊มได้จริง** (ทุก callParam ไหลมาที่นี่เสมอ) — แต่ป้ายปุ่มเป็น data (ดู A1-2)

## A1-2 — จุดสร้างข้อความ "คาดว่าแต่ละวัน…" และป้ายปุ่ม "N วัน"

- ป้ายปุ่ม = `AreaBuildingChoice.text` · ข้อความอธิบาย (รวม "คาดว่าแต่ละวันจะได้รับ 293 เสบียง") = `AreaBuildingChoice.describe` — **เป็น data ไม่ใช่โค้ด hard-code** [DUMP โครง]
- ตัวเลข 293/16% = **คำนวณตอนรัน** [INFERRED — อาคาร/ระดับต่างกันให้เลขต่างกันบนจอ] — **จุด format/replace = UNKNOWN** (ต้อง disasm ผู้สร้าง dialog จาก `areaBuildingChoices`)
- หลักฐานเสริม: โค้ดมี `String::Format` ใช้กับ dialog แล้วบางจุด [asm — `TryCancelWorking` 0x01809cb402 = dialog ยืนยันยกเลิก] — จึงน่าจะมีจุด format รางวัลเช่นเดียวกัน (ยังไม่ pin)
- ผลต่อ (ฌ): แก้ข้อความ = แก้ data/template รายอาคาร (ไม่ใช่ patch จุดเดียว) — รายละเอียด UNKNOWN จนกว่าจะเจอจุด format

## A1-3 — PartyController เดินเวลาอย่างไร (มติ (ซ): นอกขอบเขต AFK)

- **โครง = coroutine ของตัวเอง [DUMP]** (dump.cs:382078–38242x): `StartButtonClicked()` → IEnumerator `HeroEnteringParty()` → `PartyContinue()` → `FinishParty()` → `EndParty()` · มี `skipping` 0xCC + `skipButton` 0xD0 + `SetSkippingState(bool)` ของตัวเอง · คะแนน: `baseScore` 0x94 + `scoreRate` 0x98 + `ChangeBaseScore(float)` / `GetTotalScore()` / `RefreshExtraRateInfo()`
- **ไม่มี `leftWorkingDay`/field ปั๊ม `WorkingUIController`** [DUMP]
- **"เดินปฏิทิน (`ChangeHour`/`ChangeDay`) ระหว่างงานเลี้ยงหรือไม่" = UNKNOWN** — dump.cs ไม่มี call graph ต้อง disasm coroutines (คำขอแยก) — ถ้าเรียก = ความเสี่ยง double-drive กับนาฬิกา server (CLOCK-1 §10) ต้องรู้ก่อน A3 runtime
- ตามมติ (ซ): task manager ไม่แตะงานเลี้ยง — ข้อ UNKNOWN นี้จึงไม่บล็อก A1–A2 ของแผน stage

## A1-4 — per-day handler เรียกซ้ำโดยไม่เดินปฏิทินได้หรือไม่ (คอขวดของ ช-A + task manager)

**ลำดับจริงต่อวัน จาก asm `WorkingUIController.Update` (586 insns, 100 calls):**
1. `Component::SendMessage(workingFuc, workingFucParam)` — 0x01809cbb9c (4-arg) / 0x01809cbbdb (3-arg) — **handler ถูกยิงก่อน** [asm]
2. `dec leftWorkingDay (0x4c)` · `inc totalWorkingDay (0x50)` · `nextDayTime = 0` — 0x01809cbbea–bbf2 [asm] — **หมายเหตุ: `totalWorkingDay` นับขึ้นจาก 0 (ไม่ใช่เก็บ N ตั้งต้น)** — สำคัญตอนอ่าน snapshot (ตรง TimeV2a: หลัง day_tick แรก total=1)
3. `GameController::ChangeHour(24.0)` — 0x01809cbc30 (constant xmm1 = 24.0) [asm] — wrap→ChangeDay ตาม audit §2.4
4. ถ้า `leftWorkingDay <= 0` → `workFinished = 1 (0x60)` → `SendMessage(finishFuc[, finishFucParam])` — 0x01809cbcef–cbd99 [asm] — **finish ยิงใน Update เอง**

- **เชิงโครง: handler เป็น string-dispatch ด้วย param คงที่ แยกจาก ChangeDay** (ถูกเรียก "ก่อน" day-tick — ตรง audit §6.2) ⇒ **mod เรียก handler เอง N ครั้งโดยไม่เรียก ChangeHour/ChangeDay เป็นไปได้เชิงโครง** [INFERRED]
- **UNKNOWN (คอขวด):** (i) ภายใน `WorkInProductionBuilding`/`WorkInAreaBuilding`/`FinishHelpWork` อ่าน day-state/ปฏิทินหรือไม่ (body ไม่มีใน evidence) (ii) สูตรรายวันเส้นตรงหรือมีเทอมตามลำดับวัน/โบนัส (handler NOT-RUN — audit :57) ⇒ **ต้อง disasm handler ก่อนเขียน task manager** (คำขอแยก) — ถ้า disasm พบ handler ผูก day-state = ขัดสมมติฐาน ช-A อย่างมีนัยสำคัญ → รายงานรอมติตามเงื่อนไขหยุด

## A1-5 — StartWorking หักอะไรตอนเริ่ม · cancel คืน/จ่ายอะไร (ผูกมติ (ข))

- **`StartWorking` 6p ไม่หักอะไรเลย** [asm StartWorking_6p.txt — resolved calls มีเพียง `GameObject::SetActive` / `Transform::Find` / field sets (workName/workingFuc/…) / UI toggle] ⇒ ต้นทุน (ค่าฝึก/ทอง 100–1600) ถูกหักที่ **caller** (เช่น `SureStartStudySkill` / handler ของ plot-choice) — **จุดหักจริง = UNKNOWN** (ต้อง disasm caller รายงาน — คำขอแยก) — ตอบตรงช่องว่างของมติ (ข) "ต้นทุนที่หักตอนเริ่มงาน = ยังไม่รู้"
- **`StopWorking(bool finished)`: ตรวจ finishFuc/finishFucParam (String::op_Inequality ×2) แล้ว `SendMessage` ×2 — เหมือนกันทั้ง true และ false** [asm StopWorking.txt 0x01809cb26a–cb2fa] ⇒ **native ไม่มี refund path แยกตอนยกเลิก** — จ่าย/ไม่จ่าย = อยู่ที่ handler (มี gate: `RealStartStudySkill`/`StudyMaxFightSkillFinish` vs ไม่มี gate: `StudyFightSelfFinish` — audit :38/:245–249/:559)
- ⇒ มติ (ข) "เครดิตตามวันงานที่ครบแล้ว (D2)" ต้องทำที่ **mod นับเอง** — และต้องออกแบบกัน "handler จ่ายซ้ำ/ไม่จ่าย" ตอนยกเลิก (ผล STOPWORKING(false)→finishFuc ต้องถูก gate โดย mod หรือปล่อยตาม gate เดิม — ตัดสินในแผน stage)
- `TryCancelWorking` = `String::Format` สร้างข้อความ → เปิด plot-choice ยืนยัน [asm — ตรง USER-SCREENSHOT TC-1] · `MiddleStopWork` = `HideInteractUI` + สาย forced-cancel (นอกขอบเขต D1 — audit เดิม)

## UNKNOWN รวม + คำขอแยกที่เสนอ (ห้ามทำใน session นี้)

1. **disasm** `WorkInProductionBuilding` / `WorkInAreaBuilding` / `FinishHelpWork` (A1-4) — คอขวดของ ช-A + task manager
2. **disasm** ผู้สร้าง dialog จาก `areaBuildingChoices` (A1-2) — จุด format ตัวเลข 293/16%
3. **อ่าน game data assets** ของ `areaBuildingChoices` (A1-1) — ค่าชุดจริงรายอาคาร
4. **disasm** `PartyContinue`/`FinishParty`/`EndParty` (A1-3) — เดินปฏิทินหรือไม่
5. **disasm** `SureStartStudySkill` / plot-choice handler ของอาคาร (A1-5) — จุดหักต้นทุนตอนเริ่มงาน
