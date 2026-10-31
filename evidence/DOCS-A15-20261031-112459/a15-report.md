# A1.5 disasm report — งาน 3 รอบ 6 (มติ R6-6 · 2026-10-31 · offline · read-only · ไม่เปิดเกม)

- **วิธี:** ใช้ toolchain เดิมของ session SP5body — `evidence/SP5body-TempHerosDict-20261031-034658/time_static_common.py` (GameImage: เปิด `GameAssembly.dll` + `Il2CppDumper/script.json` **READ-ONLY** · capstone 5.0.7) + runner ของรอบนี้ `a15_disasm.py` (อยู่ในโฟลเดอร์นี้) → bodies/*.txt (23 ไฟล์) + `run-output.txt` (histogram resolved calls ต่อ body)
- **การ re-resolve:** ทุก method resolve จาก `script.json` ScriptMethod (ชื่อเต็ม) ไม่ใช่ค่า RVA จากเอกสาร — ขอบเขต body = start ถึง ScriptMethod ถัดไป (approx เฉพาะ method สุดท้ายของ class) · ป้าย: **[DISASM]** = อ่านจาก body จริงของรอบนี้ · [DUMP] = dump.cs · [INFERRED] = สรุปเชิงโครง · [UNKNOWN] = ยังไม่ตอบได้
- **สรุปหนึ่งบรรทัด:** ใน body ทั้ง 23 ไฟล์ **ไม่พบ call `ChangeHour`/`ChangeDay`/`ChangeMonth`/`ChangeYear` หรือ field วันใดเลย** (grep ตรงใน bodies — 0 match) — รายงานต่อข้อ:

## A1.5-1 — per-day handler ของงานอาคาร/ช่วยงาน (คอขวด ช-A)

| body | rva | insns | call สำคัญ |
|---|---|---|---|
| `PlotController$$WorkInProductionBuilding` | 0x68ADA0 | 868 | `WorkingUIController$$AddResourceNum` ×2 · `String$$Format` ×2 · `GameController$$GenerateMaterial` · `GenerateTreasure` · `WorldData$$Player` |
| `PlotController$$FinishWorkInProductionBuilding` | 0x4F2920 | 671 | `GetRandomHero` · Format ×2 · Concat · `WorldData$$Player` |
| `PlotController$$WorkInAreaBuilding` | 0x68A720 | 361 | `AddResourceNum` ×1 · `String$$Replace` · `WorldData$$Player` |
| `PlotController$$FinishWorkInAreaBuilding` | 0x4F1690 | 1054 | `GetRandomHero` · Format ×3 · Concat ×2 · Replace |
| `PlotController$$FinishHelpWork` | 0x4E5FA0 | 110 | (ไม่มี call ตรง INTEREST) |
| `PlotController$$HelpPatrol` | 0x5305B0 | 131 | `WorldData$$Player` |
| `PlotController$$FinishHelpPatrol` | 0x4E5A90 | 79 | `WorldData$$Player` |

- **(ก) day-state/ปฏิทิน:** **ไม่พบ** call/field เดินปฏิทินใดทั้ง 7 body [DISASM] — handler เป็นสาย "จ่าย + จัดข้อความ" เท่านั้น · `WorldData$$Player` = getter ตัวผู้เล่น (ไม่ใช่วัน) · ⇒ สมมติฐาน "mod เรียก per-day handler N ครั้งโดยไม่เรียก `ChangeHour(24)`/`ChangeDay()`" **สนับสนุนแรง** (ยกระดับจาก [INFERRED] ของ A1-4 → **สนับสนุนด้วย [DISASM]** — ยังต้องยืนยัน RUNTIME ก่อน A3 ตามแผน)
- **(ข) สูตรรางวัล:** handler รับ `resourceIDText` (callFucParam คงที่เท่าเดิมทุกวัน — ไม่มีพารามิเตอร์ day-index [DUMP: `AreaBuildingChoice.callFucParam`]) และไม่อ่านวันที่ ⇒ **ผลต่อวันคงที่ (เส้นตรง)** [INFERRED เชิงโครง — ทางเดียวที่จะไม่เส้นตรงคืออ่านวันจาก state ซึ่งไม่พบ] · จุดจ่าย = `AddResourceNum` · สายผลิตพบ `GenerateMaterial`/`GenerateTreasure` (drop วัสดุ/สมบัติเสริม — สูตรเชิงตัวเลข = [UNKNOWN])
- **(ค) จุด "16%/วัน":** ตัวเลข = **คำนวณตอนรันจาก data อาคาร แล้ว format ลงข้อความ** ที่ `AreaController$$FreshAreaBuildingButton` (String.Format + Concat + `LTLocalization$$SetText` ×3 [DISASM]) — template/ตัวเลขต้นทางอยู่ใน data asset = [UNKNOWN] (เช่นเดียวกับ A1.5-2)

## A1.5-2 — ค่าชุดตัวเลือกจริงรายอาคาร (`AreaBuildingDataBase.areaBuildingChoices`) — **UNKNOWN หยุดตามเงื่อนไข**

- ตรวจ `ScriptString` ทั้งหมดใน script.json (string literal ระดับ metadata): รูปแบบ `';'-delimited + callFuc` พบ **1 รายการเดียว** (`'完成巡查;FinishHelpPatrol;'` @ 0x1DAE2C8 — ไม่ใช่ชุดตัวเลือกอาคาร) ⇒ ค่าชุดตัวเลือก (1/5/10 vs 5/10/15 vs 1–5) **ไม่อยู่ในโค้ด/metadata** — อยู่ใน **serialized game data assets** [INFERRED]
- **ตามเงื่อนไขของ prompt:** ต้องใช้เครื่องมือสกัด asset ที่ repo ยังไม่มี ⇒ **หยุดข้อนี้ — UNKNOWN + เสนอเป็นคำขอแยก** (ห้ามเพิ่มเครื่องมือเอง) · ทางเลือกอื่นที่ไม่ต้องเพิ่มเครื่องมือ = วัดบนจอทีละปุ่ม [RUNTIME — session แยก] · **ไม่กระทบการ patch** (A1-1: patch จุดเดียวที่ StartWorking 6p ครอบทุกงาน)

## A1.5-3 — ต้นทุนถูกหักที่ไหนตอนเริ่มงาน

| body | insns | call สำคัญ | การหักทรัพยากร |
|---|---|---|---|
| `StudySkillController$$SureStartStudySkill` | 193 | `WorkingUIController$$StartWorking` · `ShowTextOnMouse` · Format | **ไม่มี** |
| `BuildingButtonController$$OnClick` (ปุ่มวันของงานอาคาร — มี field `areaBuildingChoice`) | 133 | `SendMessage` ×2 (ยิง `callFuc`/`callFucParam` ของ choice) · `ShowTextOnMouse` (เงื่อนไขไม่ผ่าน) | **ไม่มี** |
| `PlotController$$StartWorking(string)` | 141 | `WorkingUIController$$StartWorking` (6p) | **ไม่มี** (ตรง A1-5 เดิม) |

- ⇒ **สายเริ่มงานทั้งฝึกวิชาและงานอาคาร ไม่มีการหักทรัพยากรใด — ต้นทุน = เวลา (dayNum) เท่านั้น** [DISASM] · ทอง 100–1600 ของงานเลี้ยง = สาย `PartyController` (นอกขอบเขตตามมติ ซ — จุดหักยังไม่ได้ pin ในรอบนี้)
- ผลต่อมติ (ข)/D2: ไม่มี "ต้นทุนที่ต้องคืนตอนยกเลิก" ในสายงานอาคาร/ฝึกวิชา — กันจ่ายซ้ำ/กันไม่จ่าย ครอบเฉพาะรางวัลตามวันที่ครบ (A1-5 เดิมยังใช้ได้)

## A1.5-4 — `PartyController` เดินเวลา/ปฏิทินด้วยอะไร

| body | insns | สิ่งที่พบ |
|---|---|---|
| `PartyController.<HeroEnteringParty>d__54$$MoveNext` | 1327 | `WaitForSeconds` ×3 (เวลาจริง—แสดงผล) · `GenerateRandomItem` · ข้อความ |
| `PartyController.<PartyContinue>d__56$$MoveNext` | 575 | `WaitForSeconds` ×2 · ข้อความ/Replace |
| `PartyController.<FinishParty>d__58$$MoveNext` | 812 | Format ×6 · `WaitForSeconds` |
| `PartyController.<EndParty>d__59$$MoveNext` | 339 | `PartyEnd` · `WaitForSeconds` |
| `PartyController$$PartyStart` | 370 | `GenerateRandomItem` ×3 (ไวน์/อาหาร/ของลับ) |
| `PartyController$$PartyEnd` | 246 | UI cleanup/เทวีน (`DeleteAllChild`, `DOFade`, `RefreshExtraRateInfo`) |

- ⇒ **งานเลี้ยงไม่เดินปฏิทินทุกทาง** — ไม่พบ `ChangeHour`/`ChangeDay` ใน coroutine ทั้ง 4 + PartyStart/PartyEnd [DISASM] · เวลาที่ใช้ = WaitForSeconds (real-time, ประกอบฉาก) — **ปิด UNKNOWN ของ A1-3** · ผลต่อ double-driver: งานเลี้ยง **ไม่มีความเสี่ยงนับวันซ้ำ** (ไม่แตะปฏิทิน) — มติ (ซ) คงเดิมทั้งระบบจึงปลอดภัยต่อนาฬิกา server

## A1.5-5 — จุด format ข้อความ "คาดว่าแต่ละวัน…" และป้ายปุ่ม "N วัน"

- **โซ่ display [DISASM]:** `AreaController$$FreshAreaBuildingButton` (คำนวณตัวเลข + `String$$Format` + `SetText` ×3 — ป้าย/คำอธิบายบนปุ่มวัน) ← สร้างปุ่มโดย `BuildingUIController$$GenerateBuildingButton` → `CreateBuildingButton(AreaBuildingChoice)` (`LTLocalization$$SetText` 1 · `GetConditionDescribe` · `MeetCondition` ×2 · `set_interactable` — เปิด/ปิดปุ่มตามเงื่อนไข)
- ⇒ ข้อความบนจอ = **โค้ด format ตอนรัน บน template ที่เป็น data** (`describe`/`text` ของ `AreaBuildingChoice`) [DISASM+INFERRED] — ตรง A1-2 เดิม · ผลต่อมติ (ฌ): แก้ป้ายสองหน่วย = แทรกจุด format ที่ `FreshAreaBuildingButton`/`CreateBuildingButton` (โค้ด) ไม่ใช่แก้ data ทุกอาคาร — แต่ template เต็มของแต่ละอาคารยังอยู่ใน data asset = [UNKNOWN] (ผูกกับคำขอแยกของ A1.5-2)

## UNKNOWN คงเหลือ + คำขอแยก

1. **ค่าชุดตัวเลือกจริงรายอาคาร + template describe เต็ม** — ใน game data assets; เสนอ: เครื่องมือสกัด asset (คำขอแยก — ห้ามเพิ่มเอง) หรือวัดบนจอ [RUNTIME]
2. **สูตรเชิงตัวเลขของ `GenerateMaterial`/`GenerateTreasure` (drop เสริมสายผลิต)** — อยู่นอกคำถาม ช-A ของรอบนี้; ถ้าต้องใช้ = สืบต่อ (คำขอแยก)
3. **จุดหักทองของงานเลี้ยง** — สาย PartyController นอกขอบเขต (มติ ซ); ถ้าจะแปะใน co-op ภายหลัง = สืบต่อ
4. ยืนยัน RUNTIME ข้อสรุป "เรียก handler ซ้ำโดยไม่เดินปฏิทิน" ก่อน A3 (ตามแผน stage rev 2 §5 ข้อ 5)
