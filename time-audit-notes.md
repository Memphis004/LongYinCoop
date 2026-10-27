# time-audit-notes — Design Feasibility Audit: Activity Timing / Global Pause / Forced Training Cancellation

> Audit รอบ 3 — 2026-10-27 · **static read-only 100%** (ไม่เปิดเกม · ไม่ hook · ไม่ invoke เกมที่รันอยู่ ·
> ไม่เขียนไฟล์/เซฟ · รายงานต้นฉบับอยู่ในแชท; ไฟล์นี้สร้างภายหลังตามคำสั่งผู้ใช้ · `time-notes.md` และ
> `time-followup-notes.md` **ไม่ถูกแก้** — ข้อที่ถูกปรับ/หักล้างอยู่ใน §7 รอมติก่อนไปแก้โน้ตเดิม)
> **วิธีใหม่รอบนี้:** static disassembly ของ `GameAssembly.dll` **บนดิสก์** (pefile + capstone —
> capstone ติดตั้งลง python user-site เป็นเครื่องมืออ่านไฟล์เท่านั้น ไม่ใช่ dependency ของโปรเจกต์ ·
> สคริปต์วิเคราะห์อยู่ใน /tmp และลบแล้ว) + module-wide byte-scan `E8 rel32` (เทคนิคเดียวกับ battle-notes §4.1)
> ป้าย: **RUNTIME** · **DUMP** · **DISASM** = ถอดจาก machine code ของ DLL บนดิสก์ (static) · **GUESS** ·
> **UNKNOWN** · **NOT-RUN**

---

## 1. Executive summary + feasibility conclusion

### 1.1 ข้อค้นพบใหม่ที่เปลี่ยนภาพทั้งหมด (ส่วนใหญ่เลื่อนจาก GUESS/UNKNOWN → DISASM)

1. **`ChangeHour(float)` (0x792620) คือนาฬิกาดวลเดียวของโลก** [DISASM]:
   - `WorldData.hour += delta` — **จุด write ของ `hour` ที่ตามหามา 3 รอบอยู่ในตัว ChangeHour** (0x79268B `movss [rax+0xB4], xmm3`)
   - เลื่อนไอคอนเดินทาง: `ManageAllHeroMove(delta)` (0x792728 — call 0x8DDD60)
   - เดิน weather timer: `ChangeWeatherLastTime(delta ÷ k)` (0x7927BE — call 0xC18A90) ⇒ **weather ผูกชั่วโมงโลก ไม่ใช่ wall-clock** (แก้ time-notes §weather)
   - ถ้า `hour ≥ 24` → wrap −24 → **เรียก `ChangeDay()` เอง** (0x792818); ไม่งั้นถ้าข้ามขอบชั่วโมงเต็ม → `ManageAllAI(true)` (0x7927E9 — กลไก `manageAiHour/considerAIHour`)
2. **caller scan ทั้งโมดูลชี้ขาด** [DISASM — E8 rel32 scan ทั้ง .text]:
   - `ChangeHour`: **3 จุด** = `BigMapController.FixedUpdate()` (0x8D9640) ×2 (0x8D9F66, 0x8DA308) + `WorkingUIController.Update` ×1 (0x9CBC30)
     - สายเดินทาง: `Time.get_fixedDeltaTime` (0xDA9E80) × `GlobalData[+0x124]` (=BaseTravelTimeScale 2.4 [RUNTIME]) × constant ตาม `fastMode @0xE9` ⇒ **fastMode คูณอัตรานาฬิกาโลกจริง** (แก้ worldmap-notes "fastMode เป็น UI ล้วน" — จริงเฉพาะ timeScale)
   - `ChangeDay()`: **2 จุด** = `ChangeDay(int)` (0x7925F0 — **ซึ่งไม่มีใครเรียก: 0 call site**, ยืนยันซ้ำ hook S2cd) + wrap ใน ChangeHour
   - `ChangeDayDirect/MonthDirect`: ถูกเรียกเฉพาะจาก `Console.Input(string)` (0x9A1340 — console นักพัฒนา) · `ChangeYearDirect`: 0 caller
   - `StartWorking`: งานทั้ง 42 call-site ลง **6p @0x9CABF0** หมด; 7p @0x9CB090 (รับ `_noCancel` ชัดเจน) มี caller เดียว = `PlotController.TryThinkAboutMissingSkill` (0x685030); overload 2p/5p/3p tail-delegate → 6p
   - `StopWorking`: 5 call sites ทั้งหมดเป็น **per-day callback ที่หยุดงานเองกลางทาง** (`ForceTeachNPCOneDay`, `HospitalCureInjury` ×2, `PlayerContinuePrisonOneDay`, `RestInHotel`)
3. **ปั๊มงานเดินด้วยเวลา unscaled** [DISASM]: `WorkingUIController.Update` (0x9CB4B0) เรียก **`RealTime.get_deltaTime()` (0xCFD4A0)** สองจุด (0x9CBA56 ตัวนับวัน, 0x9CBDC0 ตัว pause-phase) และ**ไม่มี** `Time.get_deltaTime` (0xDA9E50) เลย ⇒ **`Time.timeScale=0` ไม่หยุดปั๊มงาน** — แต่หยุดสาย FixedUpdate (เดินทาง/เคลื่อนที่) — Global Pause **ต้อง gate สองสายแยก**
4. **ปั๊มเดินวันผ่าน `ChangeHour(≈24)` ต่อวัน** — ไม่ได้เรียก `ChangeDay()` ตรง [DISASM] ⇒ อธิบายครบ: "1 leaf hit = 1 วัน" (hook วัดผ่าน wrap) + "`hour` คงที่ตอนทำงาน" (+24 วนกลับชั่วโมงเดิม)
5. **`ChangeDay()` leaf (0x790E30) = day-tick ทั้งโลก** [DISASM — call chain ตามลำดับ]:
   `ChangeMonth` (0x793A20) → `ManageMailTime` (0x7CE800) → บาดเจ็บ ×3 (`GetHeroAutoInjuryChange`+`ChangeExternal/Internal/PoisonInjury`) → `ManageHeroAutoRecover` (0x7CE480) → `ManageTagTime` (0x89C680) → `TryIdentifyAllItem` (0x8A8210) → **`ManageAllAI`** (0x7C5A50) → `ResetAllAIMananged` → `ManageAreaStuff` (0x7C5C40) → ภารกิจหมดเวลา `GiveUpMission` (0x7BF7D0) → ประชุม `IsMeetingTime/SetMeetingEnd` → event expiry (`RemoveBigMapRandomEvent/RemoveAreaMapRandomEvent(outTime)` 0x7DCDF0/0x7DCCD0, `RemoveEvent` 0x7DCF00) → callback สตริง (`Split`+`SendMessage` — eventOutTimeCallFuc) → `ManageAiForceAttackAreaAndResourcePoint` (0x7C4660) → `GenerateRandomEnemy/GenerateRandomEvent` → `ManageWorldEvent` (0x9D27B0) → `ManageForceStuff` (0x7C81C0 — **ภายใต้ `Monitor.Enter` lock** 0x26B680) → `ManageWeaponResearch/ManageMeditation/ManageForceSpeResearch/ManageSpePoison/ManageSpeSummonResearch` → `CheckWorldPlotEventDataBase` (0x9D4590)
   ⇒ **จำนวน housekeeping = จำนวนครั้งที่ `ChangeDay()` ถูกเรียก** ไม่ขึ้นกับอัตราเวลาจริง
6. **ยกเลิกถอดครบ** [DISASM]: `TryCancelWorking` (0x9CB330) guard `leftWorkingDay>0 && noCancel==0` (มิฉะนั้น return เงียบ) → เปิด plot `'确定要中止{0}吗？'` choice `'中止;MiddleStopWork'` (stringliteral 0x1D99870/0x1D74540) → **`PlotController.MiddleStopWork()` (0x54EEA0, dump.cs:384077) tail-call `StopWorking(false)` โดยไม่เช็ค `noCancel` ซ้ำ** ⇒ เกมมี forced-cancellation primitive อยู่แล้ว · `StopWorking(finished)` (0x9CB220) **ยิง `finishFuc` เสมอ** (ไม่มี branch ตาม finished) + `working=false` + UI hide ⇒ "ยกเลิก" = `workFinished=false` ซึ่ง finish-handler ของแต่ละงานอ่านเอง — **ไม่มี refund กลาง**
7. อื่น ๆ: `StartWorking` ตั้ง `totalWorkingDay=0` (นับขึ้น), `nextDayTime=0`, `leftPauseTime=pauseTime(static)`, `resourceNum=0`, `working=true` [DISASM 0x9CB090/0x9CABF0] · skip มีตัวคูณจาก prefs (`PlayerPrefDictionary.GetInt` ×2 ใน Update 0x9CBAF6/0x9CBA07 และใน leaf 0x7924A6/0x792534) · `GameController.Update` (0x7E3BF0, ขนาด 0x590) **ไม่มี ChangeHour** แต่มี quick save/load hotkey + `CanSaveLoad()`→`Save(int)` (0x7E3D88 — autosave hook มีจริง, เงื่อนไข Unknown)

### 1.2 Feasibility conclusion

- **กิจกรรมหลายอย่างพร้อมกัน: ไม่ feasible กับโครงปั๊มเดิมแบบขนาน** — singleton ต่อโปรเซส + state ชุดเดียว ⇒ มีกิจกรรมได้ 1; การเริ่มทับ = งานเก่าหาย (RUNTIME: `totalWorkingDay` 10→5 ใน S2cd) ⇒ ระบบหลายผู้เล่นต้อง **serialize กิจกรรมผ่านคิว host** หรือ manager ใหม่ (§3)
- **ตาราง 1/5/10/15 → 10/60/90/120 วิ: feasible** ผ่าน static `dayTime`/`pauseTime` ต่อการเริ่มกิจกรรม (10/12/9/8 วิ/วัน) — เงื่อนไข: serialize + ตั้งให้ทันก่อน day แรก (`nextDayTime` เริ่ม 0) + รู้ว่า skip-multiplier จาก prefs คูณทับ
- **Global Pause: feasible แบบสองสาย** — เดินทาง/ศึก = `timeScale=0` + `TimeScaleController.paused=true`; กิจกรรม = gate ฝั่ง mod (ไม่มี flag เนทีฟ — Update เช็กแค่ `leftWorkingDay/skipping/working` [DISASM])
- **ยกเลิกฝึกเมื่อถูกโจมตี: feasible ด้วย `MiddleStopWork`** (forced path ของเกมเอง) — ขาดแค่ wiring protocol/validator + พฤติกรรม finish-handler ของงานฝึกเมื่อถูกยกเลิก (NOT-RUN)

---

## 2. Concurrency matrix (Audit A)

**โครงสร้าง:** `WorkingUIController` singleton (`_instance @0x8`) · state งานทั้งหมดเป็น field เดียวของ object เดียว · `dayTime/pauseTime` static · **ไม่มีโครงสร้างกิจกรรมต่อผู้เล่นใน WorldData/HeroData** (Observed absence ใน dump-WorldData.cs/dump-HeroData.cs)

| # | เคส | ผล | ป้าย | หลักฐาน |
|---|---|---|---|---|
| 1 | ผู้เล่นเดียวฝึก | ปกติ | **Observed** | area-notes P1-6 (ฝึก 2 วัน) |
| 2 | สองคนฝึกพร้อมกัน | **ทำไม่ได้ในโครงเดียว** — StartWorking ครั้งหลังทับ state ⇒ serialize/queue | **Inferred** | singleton + field layout; RUNTIME หนุน (เริ่มงานใหม่ reset `totalWorkingDay`) · "เริ่มทับขณะ working=true" เองยังไม่เคยวัด = Unknown จุดเดียว |
| 3 | ฝึก + อีกคนทำงาน | เดียวกับ 2 | **Inferred** | เดียวกัน |
| 4 | ฝึก + อีกคนเดินทาง | **ขนานได้เชิงกลไก** — สอง driver คนละสาย (Update vs FixedUpdate) แต่ mutate `worldTime/hour` บน main thread เดียว ⇒ ไม่มี data race; **ลำดับ day-tick จากสองสายสลับกันได้** (วันจากการเดินทางแทรกระหว่างวันงาน) | **Inferred** | caller scan (3 จุด) + ChangeHour disasm + sp0-sp7 census |
| 5 | ฝึก + ถูกโจมตี | primitive ครบสองฝั่ง (`MiddleStopWork` / `PrepareBattleMap` ×11) แต่ไม่มีการเชื่อมในเกมและ mod — wiring ใหม่ทั้งหมด (§5) | **Inferred** (primitive) / Unknown (เริ่มศึกทับ modal งานโดยไม่ยกเลิก — ไม่เคยวัด) | dump.cs:384077 + worldmap-notes §4.2 |

**ตอบคำถาม Audit A:** singleton ต่อโปรเซส = ใช้ · state แชร์ทั้งหมด = ใช่ · กิจกรรมอิสระหลายตัว = ไม่ได้ · remote player แทนด้วย host UI state = **ไม่ได้** (ต้องเป็น mod-state ใหม่) · กิจกรรมหนึ่งเปลี่ยน timing อีกกิจกรรม = ได้โดยโครงสร้าง (static อ่านทุกเฟรม — 0x9CBA9D) แต่เพราะมีกิจกรรมเดียวจึงเป็น "งานถัดไปได้ rate ใหม่" · callback ที่ต้องพึ่งปั๊ม = `workingFuc`/`finishFuc` (SendMessage จาก Update/StopWorking เท่านั้น — DISASM) · ขนานในโลก authoritative เดียว = กิจกรรม↔กิจกรรม ไม่ได้; กิจกรรม↔เดินทาง ได้; กิจกรรม↔ศึก ต้องยกเลิกก่อน

---

## 3. สามแนวทาง activity-timing (Audit B)

**ลำดับต่อหนึ่งวันของปั๊ม** [DISASM Update 0x9CB4B0]: `nextDayTime += RealTime.deltaTime` → ถ้า ≥ `dayTime`(× skip-multiplier จาก pref เมื่อ skipping) → **(1) `SendMessage(workingFuc[, workingFucParam])`** (0x9CBB9C/0x9CBDBD) → เช็ก `working` ยัง true (callback อาจหยุดงานเอง) → **(2) `ChangeHour(delta≈24)`** (0x9CBC30) → wrap → `ChangeDay()` housekeeping → **(3)** ลด `leftWorkingDay` (จุด dec อยู่ช่วง 0x9CBBE0–0x9CBCA4 — ตำแหน่งเป๊ะไม่ได้ pin) → ถ้าหมด → `SendMessage(finishFuc[,param])` + `SetActive(false)` · ระหว่างวันมี pause phase (`leftPauseTime`, RealTime จุดที่สอง)

| เกณฑ์ | (1) เลือก total duration ตอนเริ่ม + จัดวันกระจาย | (2) ปรับ per-day pump ตาม N | (3) server manager ต่อกิจกรรม |
|---|---|---|---|
| กลไก | dayRate = T(N)/N ⇒ set `dayTime` ตอน StartWorking (10/12/9/8 วิ/วัน) | เหมือน (1) ทุกประการเชิงกลไก | mod จับเวลาเอง → สั่ง day-tick เอง |
| จุดเกิด world-time increment | ไม่เปลี่ยน: wrap ใน ChangeHour เท่านั้น (2 caller — DISASM) | เดียวกัน | เรียก `ChangeHour(24)` เอง (จุดเดียวกับเกม) หรือ `ChangeDay()` ตรง (เกมไม่ใช้) |
| ลำดับ ChangeDay/workingFuc/resource/finishFuc | คงลำดับเดิมอัตโนมัติ (workingFuc **ก่อน** day-tick — DISASM) | เดียวกัน | **ต้องเลียนเอง** — ผิดจังหวะ = ผลต่าง (resource รายวันอยู่ใน leaf) |
| callbacks ครบ 1 ครั้ง/วัน | ใช่ (1 leaf/วัน โดยโครง) | ใช่ | ไม่มีประกันจากเกม |
| ผลต่อผู้เล่นอื่น/sim/event/resource | จำนวน day-tick คงเดิม; จังหวะเวลาจริงกระจายกว่าขึ้น (interleave มากขึ้น) | เดียวกัน | คุมได้ละเอียดสุด แลกความเสี่ยงเลียนแบบผิด |
| Pause/Resume | ปั๊มไม่หยุดด้วย timeScale — ต้อง gate ฝั่ง mod | เดียวกัน | ง่ายสุด |
| Save/Load | state ปั๊มไม่อยู่ในเซฟ (ไม่มี working field ใน WorldData) ⇒ กิจกรรมหายหลังโหลด [GUESS]; autosave hook มีจริง (เงื่อนไข UNKNOWN) | เดียวกัน | ต้อง persist mod-state เอง |
| ความเสี่ยงรวม | **ต่ำ** | = (1) | **สูง** — เลียน callback 42+ งาน (list §1.1 ข้อ 2 และ scan8) |

**คำตอบคำถามหลัก:** ตาราง non-linear คุม **ทั้ง real elapsed time และจังหวะเดินวันโลก แบบแยกไม่ได้ในแนวทาง (1)/(2)** — day-tick ของกิจกรรมคือการเดินวันของโลกในเวลาเดียวกัน; "กิจกรรม 90 วิ แต่วันโลกเดินอัตราอื่น" เป็นไปได้เฉพาะ (3) โดย mod เรียก `ChangeHour(24)`/`ChangeDay()` เอง — primitive เดิม แต่ลำดับ callback ต้องรักษาเอง · **ห้ามสรุปว่าปั๊มเดิมแทนได้** — หลักฐาน (ลำดับ disasm + 42 finish-handler ที่ยังไม่ถอด) ไม่พอ
**ข้อจำกัดที่ต้องรายงาน:** `ChangeHour` wrap **ครั้งเดียวต่อ call** (if เดียว ไม่ใช่ while — DISASM 0x7927D4–0x792818) ⇒ `ChangeHour(72)` เหลือ hour ≥24 ค้าง แล้วไล่ wrap ทีละวันใน call ถัดไป — mod ที่ใช้สายนี้ต้องเรียก delta ≤ 24 หรือเรียกซ้ำเอง

---

## 4. Global Pause — state-transition table (Audit C)

**ตอบคำถาม static:** ปั๊มงานใช้ **unscaled** (RealTime) — ตอบได้จาก DISASM แล้ว ไม่ต้องรอ runtime · `TimeScaleController.paused` side effect: Update early-return ⇒ ไม่ restore timeScale, ชุด `slowTimeDatas` ค้างไม่หมดอายุ (จับเวลาด้วย RealTime — ปรับเป็น Inferred) · สายเดินทาง = FixedUpdate → หยุดเมื่อ timeScale=0 (Unity semantics — Inferred จาก engine, ไม่ได้วัดในเกมนี้) · ปั๊มงานไม่เช็ค flag ใดของเกม ⇒ P4 (Esc) ยังอธิบายได้เฉพาะฝั่งศึก · transport ของ mod ไม่ผูก timeScale (เดินต่อได้ — โครง Observed)

| Transition | กลไกที่ต้องเกิด | หยุด / เดินต่อ | ป้าย |
|---|---|---|---|
| Running → Paused | `TimeScaleController.paused=true` **ก่อน** `timeScale=0` (ไม่งั้นถูก restore ทุกเฟรม — DISASM battle-notes §6) **+** gate สายปั๊มงานฝั่ง mod | หยุด: เดินทาง/เคลื่อนที่/weather/day-tickจากเดินทาง/ศึก(น่าจะ) · เดินต่อ: transport mod, ปั๊มงาน (ถ้าไม่ gate), UI | Inferred + Observed (โครงปั๊ม) |
| Paused → Resumed | timeScale กลับ → `paused=false` → ปลด gate ปั๊ม | ไม่มี day-debt (ไม่มีสิ่งใดสะสมค้าง) | Inferred |
| Guest connect ขณะ paused | handshake ปกติ + **ต้องมี pause-state ใน full snapshot** (ปัจจุบันไม่มี) | transport เดินตลอด | Observed (ของเดิม) + ช่องว่างใหม่ |
| Guest disconnect ขณะ paused | host คง pause ต่อ (pause เป็นของโลก); reconnect ส่ง state ใหม่ | — | ข้อเสนอ (design decision) |
| Pause ขณะมีศึก | timeScale=0 หยุด FixedUpdate (anims/ฟิสิกส์); `battleTime` น่าจะนิ่ง (`GetBattleDeltaTime`→0) — **ไม่เคยวัด**; `battlePaused` ไม่ถูกแตะ | ศึกค้างเหมือน pause ในเกม | Inferred / Not verified at runtime |
| Attack request ขณะ paused | validator ปฏิเสธหรือเข้าคิว — **ให้ผู้ใช้ตัดสิน** (แนะนำ: เข้าคิว — guest อาจสั่งก่อนเห็น pause) | — | design decision |
| Completion ครบกำหนดขณะ paused | ถ้า gate ปั๊มถูกต้อง `nextDayTime` หยุดนับ ⇒ ไม่มี completion ระหว่าง pause; **ถ้า gate ไม่ครอบ ปั๊มจะจบงานต่อไป** (RealTime!) — จุดเสี่ยงสุดของ design | — | Inferred (DISASM) |

**นโยบายคำสั่ง (แนะนำ — design decision):** gameplay commands เข้าคิว host FIFO พร้อม reqId; validator ตรวจ **ตอน dequeue**; Pause/Resume = priority ก่อนคิว gameplay ค้าง; ผลลัพธ์ Accept/Reject ส่งกลับเป็น event

---

## 5. Training cancellation call graph (Audit D)

```
[เริ่มฝึก] BuildingUIController.StudyFightSelf (0xB7B530)
    → SinglePlotData..ctor (0x983E60) + PlotController.ChangePlot (0x498870)   ; เปิด dialog เลือก (ไม่ใช่ StartWorking ตรง)
    → (choice callback สตริง) → PlotController.StudyMartialClubSkillSure (0x671100)
    → StartWorking(6p @0x9CABF0)                                                ; 42 call-sites ลงที่นี่หมด [scan8]
        → working=true, leftWorkingDay=N, totalWorkingDay=0, nextDayTime=0,
          leftPauseTime=pauseTime, noCancel←trailing-arg (0x9CACF6 mov [0x61], dil)
        (7p @0x9CB090 รับ _noCancel ชัดเจน — caller เดียว: TryThinkAboutMissingSkill 0x685030)
[ระหว่างฝึก] WorkingUIController.Update วนตาม §3
[ยกเลิกทาง UI] ปุ่ม → TryCancelWorking (0x9CB330)
    → guard: leftWorkingDay<=0 → return; noCancel!=0 → return                   ; [DISASM 0x9CB3A9/0x9CB3B3]
    → SinglePlotData("确定要中止{0}吗？", ["中止;MiddleStopWork", …]) → ChangePlot
        → choice → PlotController.MiddleStopWork (0x54EEA0, dump.cs:384077)
            → HideInteractUI()
            → tail-call StopWorking(false)                                       ; [DISASM — ไม่เช็ค noCancel ซ้ำ]
[StopWorking(finished) 0x9CB220]
    → workFinished = finished
    → if finishFuc != null: SendMessage(obj, finishFuc[, finishFucParam])        ; [ยิงทั้งจบและยกเลิก — ไม่มี branch]
    → working = false; workingUI.SetActive(false)
```

**ตอบ Audit D:** overload ที่ฝึกเรียก = 6p (Observed) · ฝึกตั้ง `_noCancel` หรือไม่ = **Unknown** (6p เขียน 0x61 จาก trailing arg; ค่าที่ site ส่งยัง pin ไม่ได้ — ถอด prologue ราย site ต่อได้ static) · `TryCancelWorking` ยกเลิกฝึกได้**เฉพาะ noCancel=false** (true → return เงียบ — DISASM) · **forced path ไม่ต้องเขียนใหม่: `MiddleStopWork()` ไม่เช็ค noCancel** (DISASM) — ตรง requirement "ห้ามตีความใหม่เป็นปฏิเสธการโจมตี" · สิ่งที่เกิดก่อนยกเลิก: `workingFuc` ของวันที่ผ่าน (ฝึก: null — RUNTIME) + day-tick housekeeping ของวันที่ผ่าน (คืนไม่ได้) · ตอนยกเลิก `finishFuc` **ถูกยิงด้วย** (`workFinished=false`) — handler ฝึก (`StudyFightSelfFinish` [GUESS จากชื่อ]) ทำอะไรเมื่อยกเลิก (คืนเงิน? ไม่ขึ้นเลเวล?) = **Unknown / NOT-RUN** — ช่องว่างหลักที่เหลือ · กัน double-outcome: main-thread เดียว + กติกา "ยกเลิกได้เฉพาะ `working==true && workFinished==false`; หลัง StopWorking แล้ว finishFuc ยิงครั้งเดียว (`working=false` ปิดทาง)" · publish ไป client + เริ่มศึกหลัง accept = ไม่มีใน protocol (§6) · **ไม่ขยายไปงานอื่น** — แต่ข้อสังเกต: เกมมี "งานจบก่อนกำหนด" อยู่แล้วบางประเภท (`HospitalCureInjury/RestInHotel/PlayerContinuePrisonOneDay/ForceTeachNPCOneDay` เรียก StopWorking เอง — scan8) และ `AreaBuildingData.noCancel @0x24` แยกชั้นของงานสร้าง

---

## 6. Protocol & authority gap inventory (Audit E)

**implement จริงวันนี้** [Observed — `S2/Protocol/S2Types.cs:10–35` + `S2ControlHandler.cs`]:
host→guest: `Hello, World, BattleEvent, BattleLog, Snapshot, Pong, Bye, AuthResult, Deny, Chunk, ResyncAck` · guest→host (allowlist): `Auth, Ping, Resync` (handler รับได้แค่ Ping/Resync/Bye — "ไม่มี write path ไปยังเกมเลย") · คำสั่งเปลี่ยน state เดิม = `spike move/attack` ทาง IPC ในศึก (gate `AllowRemoteInput`+`TurnGate`)

| concept ที่ต้องมี | สถานะ |
|---|---|
| Global pause state + PAUSE/RESUME(+ack) | **ไม่มี** — message ใหม่ + host flag fail-closed |
| Activity ต่อผู้เล่น (type/progress) | **ไม่มี** — ต้องเป็น mod-state ใหม่ + snapshot field (`WORKING_STATE`) |
| Target duration ฝั่ง server | **ไม่มี** — config table ฝั่ง host (ใหม่) |
| Complete/Cancelled outcome | **ไม่มี** — event ใหม่ `ACTIVITY_DONE{reqId, worldDayBefore/After, outcome}` (อ่านจาก `worldTime`/`workFinished`/`working` — ทุก field อ่านได้ typed interop) |
| Combat initiation นอกศึก | **ไม่มี** — `PrepareBattleMap` primitive พร้อม, คำสั่ง+validator ใหม่ |
| Join/reconnect restoration | มีแค่ battle resync (`Resync`/`ResyncAck`+hash) — world/activity snapshot ใหม่ |

**จัดลำดับคำสั่งชนกัน:** main thread เดียว ⇒ deterministic ถ้าทุกคำสังเข้าคิวเดียว — (1) validator ตรวจตอน dequeue, (2) กิจกรรม serialize (§2), (3) ยกเลิก→ตรวจ `working/workFinished`→เริ่มศึก **ในเฟรมเดียวกัน**, (4) Pause/Resume ไม่ถูก block ด้วยคิว gameplay

---

## 7. ความเสี่ยงสูงสุด + ข้อที่ปรับจากโน้ตเดิม

**ความเสี่ยง/ช่องว่างสูงสุด:**
1. ปั๊มงานไม่หยุดด้วย timeScale (RealTime — DISASM) ⇒ Global Pause ที่ gate ไม่ครอบสายปั๊ม = งานเดิน/จบระหว่าง pause ต่อไป — ต้องออกแบบ gate ใหม่ + ทดสอบจริง
2. finish-handler ของงานฝึกเมื่อถูกยกเลิก (`workFinished=false`) ทำอะไรกับ resource/reward — Unknown, NOT-RUN
3. ค่า `noCancel` ที่ call site ของการฝึก — Unknown (ถอด prologue ราย site ต่อได้ static)
4. `StartWorking` ทับขณะ `working=true` — Unknown (ไม่เคยวัด)
5. ศึกภายใต้ `timeScale=0` — Inferred ว่านิ่ง ไม่เคยวัด
6. จุดกระตุ้น autosave (`GameController.Update`→`Save(int)` — DISASM; เงื่อนไข `needAutoSave/checkUpdateTime` UNKNOWN)
7. skip-multiplier จาก prefs ที่ `dayTime` ใหม่ — Unknown

**ข้อที่รอบนี้ปรับ/หักล้างโน้ตเดิม (รอมติผู้ใช้ก่อนแก้ `time-notes.md`/`time-followup-notes.md`/`worldmap-notes.md`):**
- (ก) "จุด write ของ hour ไม่รู้ตำแหน่ง" → อยู่ใน `ChangeHour` (0x79268B)
- (ข) "weather timer real-time float" → **hour-units** ผ่าน `ChangeWeatherLastTime(delta/k)` — ผูกปฏิทิน
- (ค) "`>>`/ปั๊มเรียก `ChangeDay()` ตรง ๆ" → ผลยังจริง (1 hit=1 วัน) แต่เส้นทางคือ `Update → ChangeHour(≈24) → wrap → ChangeDay()`
- (ง) "fastMode เป็น UI ล้วน" → จริงเฉพาะ timeScale; **คูณอัตรานาฬิกาโลก**ใน FixedUpdate
- (จ) "`ChangeDay()` leaf housekeeping = GUESS" → Observed/DISASM ครบชุด §1.1 ข้อ 5

---

## 8. Recommended next step (รออนุมัติแยก — ห้ามลงมือ)

**session runtime บนสำเนาเซฟ (split + preflight §1.2) ปิด 4 ช่อง:**
1. เริ่มฝึก 2 วัน → invoke `PlotController.MiddleStopWork()` (0x54EEA0) → สังเกต `working/workFinished`, `finishFuc` ถูกยิงหรือไม่ (hook log-only ที่ StopWorking), เงิน/resource ก่อน-หลัง, วิชาไม่ขึ้น, ปฏิทินเดินกี่วัน
2. `TimeScaleController.paused=true` + `timeScale=0` → ยืนยันปั๊มงาน**เดินต่อ** (ตาม DISASM) และเดินทาง/ศึกหยุด
3. งาน 5 วันที่ `dayTime=12` (invoke เขียน static) → ยืนยันรวม 60±2 วิ, day-tick ครบ 5, skip-multiplier ยังถูกคูณ
- **ผ่าน:** ผลครบ 3 ข้อ + `SaveSlot*` เซฟจริง digest ไม่เปลี่ยน + hook detach สะอาด · **ไม่ผ่าน:** พฤติกรรมต่างจาก DISASM → หยุดรายงานก่อนขยาย
- **ก่อน implement จริง** ต้องมีมติ: (ก) pause-policy ของคำสั่ง attack ขณะ paused, (ข) โมเดล serialize กิจกรรมหลายผู้เล่น, (ค) ระยะเวลาของ N นอก {1,5,10,15}

---

## 9. ดัชนีหลักฐาน (disasm addresses หลัก)

| ข้อเท็จจริง | ตำแหน่ง |
|---|---|
| `ChangeHour`: hour write 0x79268B · ManageAllHeroMove 0x792728 · ChangeWeatherLastTime 0x7927BE · wrap→ChangeDay 0x792818 · ManageAllAI(true) 0x7927E9 | GameAssembly.dll RVA 0x792620–0x79281F |
| caller ทั้งโมดูล: ChangeHour ×3 (FixedUpdate 0x8D9F66/0x8DA308, Update 0x9CBC30) · ChangeDay ×2 · ChangeDay(int) ×0 · *Direct → `Console.Input` 0x9A1340 | byte-scan E8 rel32 ทั้ง .text |
| `BigMapController.FixedUpdate` (0x8D9640): `fixedDeltaTime × GlobalData[+0x124] × fastMode-const` | DISASM 0x8D9EC0–0x8DA312 |
| `WorkingUIController.Update`: RealTime.deltaTime 0x9CBA56/0x9CBDC0 · SendMessage workingFuc 0x9CBB9C/0x9CBDBD · ChangeHour 0x9CBC30 · finish sequence 0x9CBD01–0x9CBDB4 | DISASM 0x9CB4B0–0x9CBF19 |
| `TryCancelWorking` guard 0x9CB3A9/0x9CB3B3 · plot 0x983F70/0x498870 · strings `'确定要中止{0}吗？'`(0x1D99870)/`'中止;MiddleStopWork'`(0x1D74540) | DISASM + stringliteral.json |
| `MiddleStopWork` 0x54EEA0 → tail `StopWorking(false)` | DISASM 0x54EF55 → 0x9CB220 |
| `StopWorking`: `workFinished=finished` · finishFuc ยิงเสมอ 0x9CB2C2/0x9CB2FA · working=false 0x9CB303 | DISASM 0x9CB220–0x9CB320 |
| `StartWorking` 7p/6p field init + noCancel write (0x9CB196/0x9CACF6) · 42 call-sites → 6p · 7p caller เดียว | DISASM + scan8 |
| `ChangeDay()` leaf call chain ครบ | DISASM 0x790E30–0x7925F0 (รายการใน §1.1 ข้อ 5) |
| S2 MessageType | `S2/Protocol/S2Types.cs:10–35` |
| พื้นฐานรอบก่อน | `time-notes.md`, `time-followup-notes.md`, `worldmap-notes.md`, `battle-notes.md`, `area-notes.md`, `evidence/S2cd-20261022-0710/`, `evidence/sp0sp7-20261024-225939/` |
