# time-static-notes — สรุป static disassembly ระบบเวลาของ GameAssembly.dll (รอบตรวจซ้ำ + สังเคราะห์ 3 รอบล่าสุด)

> **label:** `TIMEstatic-20261027-051647` · **งานเอกสาร read-only 100%**: ไม่เปิดเกม · ไม่เรียก method · ไม่ hook ·
> ไม่ build/deploy · ไม่แตะเซฟ/config/Steam · ไม่แก้ source · ไม่แก้ `time-notes.md` / `time-followup-notes.md` (คงไว้เป็นประวัติ) · ไม่ commit
> ไฟล์ใหม่ในโฟลเดอร์นี้เท่านั้น (โน้ต + สคริปต์ + ผล TSV/ดิสแอส)
> **hash ยืนยันก่อนงาน (2026-10-27 05:1x):** `GameAssembly.dll` = `33c56def…7920c1` · `global-metadata.dat` = `451311d4…18bfd0` — **ตรง VERSION.md ทั้งคู่** ⇒ anchor ที่อยู่ทุกตัวในโน้ตนี้ใช้ได้
> ป้าย: **OBSERVED** = อ่านเองจาก disasm/ไฟล์เกมรอบนี้ · **INFERRED** = อนุมานจากหลักฐาน OBSERVED · **UNKNOWN** = หลักฐานไม่พอ ·
> **UNVERIFIED-ON-RERUN** = claim จากรอบก่อนที่รันตรวจซ้ำด้วยวิธีเดิมแล้ว**ไม่ได้ตัวเลขเดิม** — ห้ามใช้เป็นข้อเท็จจริง
> · RUNTIME = วัดจาก process จริงใน session ก่อน ๆ · DUMP = จาก `Il2CppDumper/dump.cs`/`script.json`

---

## 1) วิธีและขอบเขต

### เครื่องมือ (ไม่ติดตั้งอะไรเพิ่ม)
| เครื่องมือ | เวอร์ชัน | ใช้ทำ |
|---|---|---|
| Python | 3.11.9 | รันสคริปต์วิเคราะห์ |
| pefile | 2024.8.26 | แผนที่ RVA↔file offset, อ่าน section, อ่าน constant pool |
| capstone | 5.0.7 | ดีสแอส x86-64 (linear disasm + validate บิต E8/E9/FF15) |
| `Il2CppDumper/script.json` | ของเวอร์ชันนี้ (hash ตรง VERSION.md) | resolve ชื่อ: ScriptMethod 96,106 รายการ (Address = **RVA**, base 0x180000000), ScriptMetadataMethod (slot FF15), ScriptMetadata (slot คลาส), ScriptString (slot literal) |
| `Il2CppDumper/dump.cs` | เดียวกัน | field offset + ลายเซ็น (อ้างเลขบรรทัดจากไฟล์จริง) |

### สคริปต์ที่ใช้จริง (อยู่โฟลเดอร์นี้ — รันซ้ำได้บนเครื่องเดียวกัน)
`time_static_common.py` (loader) → `scan_time_xrefs.py` (สแกน E8/E9/FF15 ทั้ง section execute → `xref-scan.tsv`, `xref-ff15.tsv`)
→ `summarize_xref_counts.py` (นับ per-target + owner → `xref-summary.txt`) → `disasm_time_bodies.py` (30 bodies → `bodies/*.txt`, `floats-observed.tsv`)
→ `resolve_slots.py` (slot .data → คลาส/สตริง/method → `slots-resolved.tsv`) → `scan_hour_writes.py` (สแกนเชิงลบ write `[+0xB4]` → `hour-writes.tsv`)

### รูปแบบที่สแกนครอบ
- **E8** (call rel32) + **E9** (jmp rel32/tail-call) ทั้ง section execute — ทุก candidate ถูก validate ด้วยการ decode 5 ไบต์เป็น call/jmp จริง (ตัด false hit จาก operand byte)
- **FF15** (call qword ptr [rip+slot]) — slot resolve ผ่าน ScriptMetadataMethod — **ผล: พบ 0 จุด** ที่ชี้เป้าหมาย
  ระบบเวลาใด ๆ (slot ที่ match ชื่อ "StudySkill*" มี 1 แต่ไม่มี site เรียก) ⇒ ทุก caller ที่รายงานในโน้ตนี้เป็น
  direct E8/E9 ล้วน (`xref-ff15.tsv` = header เปล่า)
- **slot-loads**: การอ้าง static field (`mov rax,[rip+X]; … [rax+0xB8]` = class statics) และ constant pool (`movss xmm,[rip+X]` → decode float จาก .rdata ด้วย pefile)
- **string literal dispatch**: `SendMessage(target, <literal>, …)` + literal ที่เทียบกับ callFuc/finishFuc และ plot-choice `"ชื่อ;handler"`
- **scene grep level3** (จากรอบก่อน): TextAsset/serialized binding ใน scene — รอบนี้ไม่ทำซ้ำ (อ้างผลเดิมเท่านั้น)

### สิ่งที่วิธีนี้ **ไม่ครอบ** (อ่านทุกตัวเลข "จำนวนจุด" ในโน้ตนี้เป็น lower bound)
- **register-indirect call** (`call rax`, `call [rax+0x2A8]` เช่น delegate/virtual ของ ActionBarUnit) — นอกหน้าต่างสแกน
- **binding ใน asset อื่น** (plot-choice ที่ผูกชื่อ handler ไว้ใน TextAsset/serialized scene — เช่น ปลายทางถัดจาก `ChangePlot`)
- **reflection** (`GameObject.SendMessage` รับชื่อ method เป็น string — สาย dispatch ทั้งเส้นอยู่นอก call graph)
- generic sharing: address เดียวใช้ร่วมหลาย T (ยกเว้นไว้: `GetComponent<StudySkillStarController>` 0x967EF0 = NOISE 2740-hit หลายสิบจุด, `.ctor` stub 0x3A1810 ร่วมหลายคลาส)
- owner attribution = "method เริ่มก่อนหน้าใกล้สุดจาก script.json" — site ใน padding/ข้อมูลระหว่าง method อาจถูก attribute ผิด (ตรวจบริบทเองตรงจุดสำคัญแล้ว)

**กติกาการเขียน:** ข้อความ "caller = X" ในโน้ตนี้หมายความว่า **"พบ X จุดภายใต้ช่องทางที่สแกน (E8/E9/FF15/slot/sentinel ข้างบน)"** เสมอ — ไม่ใช่การอ้างว่าครบทุก call graph

---

## 2) ข้อค้นพบ (พร้อม RVA และป้าย)

### 2.1 `ChangeHour(float)` = ตัวเดินเวลา ม.ค.ของโลก — แต่ **ไม่ใช่** writer เดียวของ `hour`

**body 0x792620 (121 insns) — OBSERVED:**
```
ChangeHour(delta):   // WorldData.hour อยู่ [WorldData+0xB4] (dump.cs:367617)
  hour = [wd+0xB4]
  [wd+0xB4] = hour + delta                          ; 0x79268B — write อัตราบวก
  BigMapController.ManageAllHeroMove(..., delta)     ; 0x792728 — ส่งเดลต้าต่อให้ระบบเดิน
  WeatherController.ChangeWeatherLastTime(-delta/24) ; 0x7927BE — สภาพอากาศนับถอยหน่วย "วัน"
  if (hour_new >= 24.0):                             ; 0x7927D4 comiss [0x193C28C]=24.0
      [wd+0xB4] = hour_new - 24                      ; 0x79280B — write ลดทอน
      ChangeDay()                                    ; 0x792818 → leaf 0x790E30
  elif (trunc(hour_old*10) != trunc(hour_new*10)):   ; ขยับตั้งแต่ 0.1 ชม.
      ManageAllAI(considerAIHour=true)               ; 0x7927E9
```
- **writer ของ `hour` ทั้งหมดที่สแกนพบ (movss [reg+0xB4] ทั้ง .text, `hour-writes.tsv`): 3 จุดใน 2 method** —
  ChangeHour ×2 (0x79268B, 0x79280B) + **`GameController$$GameDataUpdateChecking` (0x7A2272)** ที่ทำ `hour = min(hour, 24.0)`
  (object ยืนยันเป็น WorldData จาก `this+0x20` → OBSERVED) · caller เดียวของ GameDataUpdateChecking = **StartLoadGame** (0x7DF899)
  ⇒ เป็น clamp หลังโหลดเซฟ ไม่ใช่ตัวเดินเวลา
- ⇒ **"ChangeHour เป็นนาฬิกาโลกจุดเดียว" ถูกต้องในความหมาย "ตัว advance"** (accumulator + วัน-โอเวอร์โฟลว์) —
  **แต่การเขียน field มี 2 method / 3 จุด** ตามข้างบน (แก้คำเดิม "จุด write = UNKNOWN" ของ time-notes.md:40)
- call chain ของ leaf `ChangeDay()` 0x790E30 — **พบ 2 จุดเท่านั้น** ภายใต้ช่องทางที่สแกน: wrapper `ChangeDay(int)` (0x792608)
  และ **ChangeHour (0x792818)** ⇒ ทุกวันที่เดินเพราะเวลา ผ่าน `ChangeHour → ChangeDay` เสมอ (รวมปั๊มงาน — ดู 2.4)

### 2.2 ChangeHour caller — พบ 9 จุดภายใต้ช่องทางที่สแกน (E8=3, E9=6) — REPRODUCED
| # | site | kind | owner |
|---|---|---|---|
| 1 | 0x8D9F66 | E8 | BigMapController$$FixedUpdate (เดินทาง, ×fastMode) |
| 2 | 0x8DA308 | E8 | BigMapController$$FixedUpdate (สายที่สอง, ×fastMode) |
| 3 | 0x9CBC30 | E8 | WorkingUIController$$Update (ปั๊มงาน — delta = 24.0) |
| 4 | 0x5382FA | E9 | PlotController$$HideInteractUIOneDay |
| 5 | 0x557B1D | E9 | PlotController$$PlayerFinishPrison |
| 6 | 0x65DA3E | E9 | PlotController$$StartStudyFightWithGreatHeroFinalResult |
| 7 | 0x65E7A6 | E9 | PlotController$$StartStudyFightWithGreatHeroMultiResult |
| 8 | 0x65F326 | E9 | PlotController$$StartStudyFightWithGreatHeroSingleResult |
| 9 | 0x939FB2 | E9 | **ExploreController$$FinishExploreMap** (จบสำรวจ → นาฬิกาเดิน) |

(ตรงกับ claim "9 จุดรวม FinishExploreMap" — REPRODUCED) · `*Direct` (ChangeDayDirect 0x790CA0 / ChangeMonthDirect 0x793920 /
ChangeYearDirect 0x795180) — พบ caller แค่ **Console$$Input** (3 จุด) ภายใต้ช่องทางที่สแกน ⇒ ยังเป็น dev-console-only [OBSERVED]

### 2.3 `RealTime.get_deltaTime` = unscaled — REPRODUCED
`0xCFD4A0`: `xor ecx,ecx; jmp UnityEngine.Time$$get_unscaledDeltaTime (0xDA9FD0)` — tail-jmp 2 คำสั่ง [OBSERVED]
caller ที่เกี่ยวกับเวลาโลก (พบ 63 จุดทั้งไบนารี, `xref-summary.txt`): **WorkingUIController.Update ×2 (0x9CBA56, 0x9CBDC0)** ·
BigMapController.FixedUpdate ×2 · WeatherController.Update ×3 (+Time.get_deltaTime ×1) · TimeScaleController.Update ×2 ·
PlotController.Update ×2 · GameController.LateUpdate ×1 · BattleController/ExploreController/HudController ฯลฯ
⇒ **ปั๊มงาน/สภาพอากาศ/เดินทางใช้ unscaled delta** — `timeScale=0` จะไม่หยุด (แก้ UNKNOWN ของ time-followup-notes.md:110 ฝั่ง static)

### 2.4 `WorkingUIController.Update` 0x9CB4B0 — ปั๊มงาน สูตร T(N) และ freeze 3 ทาง — REPRODUCED
**control flow (586 insns) — OBSERVED:**
```
Update():
  if (!working(0x48)) return
  … อัปเดต label ×3 (leftWorkingDay/callFuc/resourceNum → LTLocalization.SetText)
  if (PlotController.Instance.plotHappen(0x18) != 0)  → freeze+return   ; 0x9CB860
  if (SpeShowController.Instance.gameObject.activeSelf) → freeze+return ; 0x9CB8F6 (speShowUIPanel)
  if (HudController.HudPanelActive())                 → freeze+return   ; 0x9CB93C
  animSpeed = skipping ? (pref TestMode==1 ? 5.0 : 2.0) : 1.0           ; 0x9CBA0C–0x9CBA23
  if (leftPauseTime(0x58) > 0):                       ; โหมดพัก: นับถอย
      leftPauseTime -= RealTime.get_deltaTime()
      if (ยัง > 0) return
      … AddAnimation("TimeAnim") + PlaySound(0.4)                       ; 0x9CBDE2–0x9CBEB6
  else:                                               ; โหมดนับวัน
      nextDayTime(0x54) += RealTime.get_deltaTime()
      threshold = WorkingUIController.dayTime(static+0x0) ×
                  (skipping ? (pref TestMode==1 ? 0.2 : 0.5) : 1.0)      ; 0x9CBAF3–0x9CBB12
      if (nextDayTime < threshold) return
      if (callFuc(0x28) != null): SendMessage(target, callFuc[, callFucParam])   ; งานรายวัน
      leftWorkingDay(0x4C) -= 1 ; totalWorkingDay(0x50) += 1 ; nextDayTime = 0
      ChangeHour(24.0)                                    ; 0x9CBC30 → วัน +1 (ผ่าน ChangeHour!)
      leftPauseTime = (leftWorkingDay > 0 ? pauseTime(static+0x4) : 2×pauseTime)  ; 0x9CBC68–0x9CBC9F
      return
  ; กิ่ง leftWorkingDay <= 0 ตอนเข้า (0x9CBCC7):
  workFinished(0x60) = 1
  if (finishCallFuc(0x38) != null): SendMessage(target, finishCallFuc[, finishCallFucParam])
  working(0x48) = 0 ; SetActive(gameObject, false)
```
- **float จาก constant pool** (ทั้งหมด OBSERVED, `floats-observed.tsv`): **24.0** (0x193C28C — ทั้ง arg ของ ChangeHour
  และเพดานใน ChangeHour/GameDataUpdateChecking) · **0.2** (0x193C1E4) / **0.5** (0x18C12FC) = ตัวคูณ threshold ตอน
  `skipping` (แยกตาม pref `"TestMode"` — PlayerPrefDictionary.GetInt) · **2.0** (0x18C1330) / **5.0** (0x18C1138) =
  ความเร็ว animation ตอน `skipping` · 1.0 (0x18C1148) = ปกติ
- **กติกา pref `"TestMode"`: อ่านได้อย่างเดียว ห้ามเขียน** — ทุกจุดที่พบใน method ที่ถอดรอบนี้เป็นการอ่านผ่าน
  `PlayerPrefDictionary.GetInt` เท่านั้น (threshold/animSpeed §2.4 + gate ปุ่ม P §2.12) [OBSERVED เฉพาะขอบเขต
  ที่ถอด] · โมด/สคริปต์ **ห้ามเขียนค่านี้ทุกกรณี** — การเขียนเปลี่ยนพฤติกรรมเกมเอง (ความเร็ว skip ×0.2/0.5
  และเปิดปุ่ม P ตัดข้อความ) และอยู่นอกขอบเขตงานอ่านอย่างเดียวของรอบนี้
- **สูตรเวลา T(N)** [โครง OBSERVED, ตัวเลข RUNTIME n=1 จาก area-notes]:
  `T(N) ≈ N×(dayTime×k) + N×pauseTime` โดย `k = 1.0` ปกติ / `0.2|0.5` ตอน skip — วันสุดท้ายมี pause `2×pauseTime`
  ก่อนจบ [RUNTIME วัด 10 วัน ≈ 12.5 s ที่ dayTime=1, pauseTime=0.25 = 10×(1+0.25) ตรงโมเดล; ขั้น 2×pauseTime
  ยังไม่แยกได้ใน n=1 — INFERRED]
  **หมายเหตุ (2026-10-27): สองสูตร T(N) — แบบปกติ (k=1.0) และแบบ skip (k=0.2/0.5) — ยังไม่ยืนยัน** ·
  ตรวจสอบด้วย RUNTIME จริงแค่ n=1 เฉพาะโหมดปกติ (12.5 s) ส่วนโหมด skip และขั้น `2×pauseTime` วันสุดท้าย
  ยังไม่เคยวัดเลย ⇒ ทั้งสองสูตรเป็นโมเดลจาก disasm เท่านั้น **ห้ามใช้ตัดสิน/อ้างเป็นข้อเท็จจริง**
  จนกว่าจะวัด RUNTIME ซ้ำ (gaps §5 แถว "ค่ารวม T(N)")
- **หมายเหตุ (2026-10-28, เพิ่มตามคำสั่งผู้ใช้): สูตร T(N) มี 2 รูปที่ยังไม่ยืนยันตอนรัน** —
  รูปในโน้ตนี้ = `N×(D×k) + N×P` และรูปที่รวมช่วงพักเริ่มงาน (initial pause ตาม 0x9CACAA) = `N×D + (N+2)×P`
  · ที่ N=10, D=1, P=0.25 สองรูปให้ **12.5 กับ 13.0 วินาที** · ตัวเลขที่วัดไว้ (~12.5, n=1)
  **แยกสองรูปไม่ได้** (ต่างกัน 2P = 0.5 วิ อยู่ในความหยาบของการวัดเดิม) — ข้อความเดิมข้างบนที่เขียนว่า
  "10×(1+0.25) ตรงโมเดล" จึงห้ามอ่านเป็นการยืนยันรูปใดรูปหนึ่ง · เมื่อ pauseTime=0 ทั้งสองรูปให้ `T=N×D`
  (จำแนกไม่ได้) ⇒ การจำแนกสองรูปรอวัด RUNTIME ระดับเฟรมของ V2 ชุด A (time-plan-draft §E.2 ชุด A — ยังไม่อนุมัติ)
- **pause phase**: ปั๊มหยุดเอง 3 ทาง (native freeze — ดู 2.5) + ช่วง `leftPauseTime` ระหว่างวัน — ตอนหยุดโหมดพัก
  `nextDayTime` ค้างที่ 0 (นับเฉพาะหลังพักจบ)
- **"ปุ่ม `>>` เร่งแค่ animation" (time-notes.md:59–60, time-followup-notes.md:112–114) ถูกหักล้างฝั่ง static**:
  `SkipButtonClicked` 0x9CAA60 / `SetSkippingState` 0x9CA8E0 เขียนแค่ `skipping(0x62)` — ไม่มี call ไป ChangeDay เลย —
  แต่ `skipping` **ลด threshold ของวันเป็น 0.2×/0.5× dayTime** ⇒ skip เร่ง day-tick จริง (สอดคล้อง RUNTIME
  hook เห็น leaf 10 ครั้งใน 10.5 s — leaf hits เดินผ่าน Update→ChangeHour(24) ทั้งหมด)

### 2.5 native freeze ของปั๊ม — 3 เงื่อนไข — REPRODUCED
ทั้งสามทำให้ Update คืนก่อนนับเวลา (และกิ่ง freeze ตั้ง SkeletonGraphic timeScale=0 — 0x9CBEC0):
1. `PlotController.Instance.plotHappen != 0` (field 0x18, dump.cs:382609) — พล็อตกำลังเล่น
2. `SpeShowController.Instance.gameObject.activeSelf` — แผง speShow กำลังโชว์
3. `HudController.HudPanelActive()` (0xB09ED0) — HUD ปิดอยู่
[OBSERVED — ชื่อคลาสจาก slot 0x1D93C00/0x1DA64C8/0x1D78EC8 ผ่าน ScriptMetadata] · ครบชุด caller ของ
`HudPanelActive` ยังไม่ได้สแกนทั้งหมด (อยู่ใน gaps)

### 2.6 `noCancel` — StartWorking(6p) 0x9CABF0 vs 7p 0x9CB090 — REPRODUCED
- **6p** (dump.cs:392733): โครงเหมือนกันทุกอย่าง แต่ `noCancel(0x61) = 0` **ตายตัว** (`xor edi,edi` ก่อน store, 0x9CACF6)
- **7p** (dump.cs:392736): `noCancel(0x61) = arg8 bool` จาก stack (movzx [rsp+0x68], 0x9CB196)
- ทั้งคู่ตั้ง `leftPauseTime = statics+0x4 (pauseTime)`, `nextDayTime=0`, `resourceNum=0`, `totalWorkingDay=0` [OBSERVED]
- ⇒ เส้นที่เรียก 6p ทั้งหมด (เช่น SureStartStudySkill ของการฝึก — 0xFE6535) = **ยกเลิกได้** (noCancel=false) —
  แก้คำถามค้าง "noCancel ของการฝึกไม่เคยอ่านค่า" (time-followup-notes.md:160–161) ฝั่ง static

### 2.7 เส้นยกเลิก — TryCancelWorking → plot-choice → MiddleStopWork → StopWorking(false) — REPRODUCED
- `TryCancelWorking` 0x9CB330: guard **2 เงื่อนไข** — `leftWorkingDay(0x4C) > 0` และ `noCancel(0x61) == 0`
  (0x9CB3A9, 0x9CB3B3) แล้วสร้าง `SinglePlotData` (หัวข้อ `"确定要中止{0}吗？"` slot 0x1D99870) → `PlotController.ChangePlot`
- choice ผูกผ่าน **string literal dispatch**: `"中止;MiddleStopWork"` (slot 0x1D74540) และ `"取消;HideInteractUI"` (0x1D8DC50)
  [OBSERVED ที่ฝั่ง string — ปลายทาง dispatch ผ่าน Unity SendMessage/reflection อยู่นอก call graph]
- `MiddleStopWork` 0x54EEA0: `HideInteractUI()` แล้ว **E9 tail-jmp → StopWorking(false)** ที่ 0x54EF55 [OBSERVED]
- `StopWorking(bool finished)` 0x9CB220: `workFinished(0x60) = finished` → **ยิง finishFuc เสมอ** (SendMessage เหมือน
  กิ่งจบงานของ Update) → `working=0` + SetActive(false) [OBSERVED]
- ผู้เรียก TryCancelWorking จริง (ปุ่ม UI) ผูกใน scene/asset — ยังไม่พบใน call graph (gaps)

### 2.8 `StopWorking` — พบ 6 จุดภายใต้ช่องทางที่สแกน (E8=5, E9=1) — REPRODUCED
`PlotController$$HospitalCureInjury` ×2 (0x53A96D, 0x53ABD8) · `ForceTeachNPCOneDay` (0x4FC67E) ·
`MiddleStopWork` (0x54EF55, E9) · `PlayerContinuePrisonOneDay` (0x557777) · `RestInHotel` (0x57A05C)
⇒ ปั๊ม (Update) **ไม่เรียก StopWorking เลย** — จบงานปกติเดินผ่านกิ่ง workFinished ใน Update; StopWorking ใช้เฉพาะ
handler รายวันที่ตัดงานก่อนกำหนด [OBSERVED]

### 2.9 StartWorking call sites — REPRODUCED (คำว่า "42" มีความหมายชัดแล้ว)
| overload (RVA) | รูปแบบ | จุดที่พบ (E8/E9) |
|---|---|---|
| **0x9CABF0** | **6p** (finishCallFuc มี, noCancel=false) | **42 จุด — E8 ทั้งหมด** (38 external + 4 cross-overload internal: 0x9CADDD, 0x9CAE6E, 0x9CAFD4, 0x9CB067) |
| 0x9CAD70 | 5p | 3 (E8) |
| 0x9CAE00 | List\<string\> | 1 (E9) |
| 0x9CAE90 | 3p | 1 (E9) |
| 0x9CB000 | (int,string) | 3 (E8 — รวม WaitUIController.SureButtonClicked) |
| 0x9CB090 | 7p (noCancel จาก caller) | 1 (E8 — TryThinkAboutMissingSkill) |
| **รวม** | | **51 จุด (49 E8 + 2 E9) — external 47 จุด, distinct owner 47 method** |

claim เดิม "call-site 42 จุดของ StartWorking" = **REPRODUCED** — ตรงเป๊ะกับจำนวน call site ที่ชี้ **overload 6p
(0x9CABF0) เท่านั้น** (E8 scan; 38 จุดเป็น external caller + 4 จุดเป็น overload อื่นเรียกซ้อน) — ใช้ตารางนี้เป็น
นิยามมาตรฐานของเลข 42 ต่อไป

### 2.10 เส้นฝึกสำนัก (finishFuc) — SureStartStudySkill → RealStartStudySkill — REPRODUCED
```
StudyFightSelfChoosen (0x66CC1A) ─┐
CoachWorkChoosen     (0x4AF002) ──┴→ StudySkillController.StartStudySkill (0xFE59D0)
    → (ยืนยันเงิน: WorldData.Player → KungfuSkillLvData.StudyMoneyCost → ChangeMoney(−เงิน); slot "SureStartStudySkill")
    → SureStartStudySkill (0xFE6280): dayNum = KungfuSkillLvData.StudyDayCost()
      → WorkingUIController.StartWorking(6p, workName, callFuc=null, finishCallFuc="RealStartStudySkill")   ; slot 0x1DA37A0
    → (จบงาน dispatch ผ่าน SendMessage string) → PlotController.RealStartStudySkill (0x56FF80)
      → E9 → StudySkillController.RealStartStudySkill (0xFE5490)      ; 0x5700B7
RealStartStudySkill: if (WorkingUIController.Instance.workFinished(0x60) == 0) → return (0xFE55CB→0xFE58EF)
                     else … PlayerStudySkill/บันทึกสถิติ [OBSERVED]
```
- **gate `workFinished` ยืนยันที่ 0xFE55CB** — ยกเลิก (StopWorking(false) → workFinished=false) ⇒ RealStartStudySkill
  ทำอะไรไม่เลย [OBSERVED] · slot "AutoStudySkill" (0x1DC1B38) + dialog ค่าใช้จ่าย (0x1DBABD0) อยู่ใน body เดียวกัน

### 2.11 `StudyMaxFightSkillFinish` 0x671140 — สาย workFinished=false ไม่ได้รางวัล — REPRODUCED
```
if (WorkingUIController.Instance == null) → null-check fail
if (workFinished(0x60) == 0) → ข้ามทั้งหมด (0x671242 je → 0x6713CD)   ; ไม่เขียน workFinished
skillId = Int32.Parse(param) → WorldData.Player() → HeroData.ChangeMaxFightSkill(player, skillId, 1, true)
→ ChangePlot(SinglePlotData("{0}新的练习最高纪录…"))
```
[OBSERVED] — ต่างจาก `StudyFightSelfFinish` (0x66CC40) ที่**ไม่มี gate workFinished**: เข้าไปเลย
(สุ่ม hero ช่วยด้วยโอกาส `min(0.05 + n×0.025, 0.25)` จาก pool 0x193C1BC/0x18C12EC, exp ผ่าน
KungfuSkillLvData, ไม่มีผู้ช่วย → `GetStudyFightExp` 0x517B70; workingFuc รายวันของเส้นนี้ = `"GetStudyFightExp;"` slot 0x1DB50D8)
⇒ สองเส้นจบงานฝึกต่างกัน: **max-skill กันรางวัลตอนถูกยกเลิก / ฝึกสำนักปล่อย exp ตามสถานะที่ค้าง** [OBSERVED]

### 2.12 save path — GameController.Update (0x7E3BF0) — REPRODUCED
- **autosave**: `if (!GlobalData.DisableAutoSave (statics+0x10, dump.cs:378118))` แล้ว `if (needAutoSave (0x28, dump.cs:376441))`
  → `CanSaveLoad(this, true)` + `GameDataController.CanSaveLoad` → `needAutoSave = 0` → `GameDataController.Save(slot=0)` (0x7E3D88)
- **F5** = `GlobalData.GetKeyDown(0x11E=286)` → `Save(slot=10)` (0x7E3EF8) — เฉพาะผ่าน CanSaveLoad สองชั้น
- **F9** = `GetKeyDown(0x122=290)` → `SaveLoadMenuController.LoadGame(slot=10)` (0x7E3E69)
- ท้าย method: บล็อก trial-end (`"试玩时间已结束…"`, `BackToMainTitleScene`) + `GetKeyDown(0x70='P')` ที่ gate ด้วย
  pref `"TestMode"==1` (ค่า return ถูกทิ้ง — ผลข้างเคียง UNKNOWN)
- **ผู้ตั้ง `needAutoSave` ยังไม่พบในช่องทางที่สแกน** (gaps) · "ไม่มี autosave ตอนออก" (RUNTIME เดิม) ยังคงยืน —
  Update นี้คือ trigger autosave ระหว่างเล่นเท่านั้น

### 2.13 FixedUpdate กับ fastMode — BigMapController — REPRODUCED
`BigMapController.FixedUpdate` (0x8D9640, 1,271 insns): `fastMode (0xE9, dump.cs:364833)` ถูกอ่าน 2 จุด
(0x8D9F35, 0x8DA2E1) — ถ้าเปิด **คูณ 2.0** ให้เดลต้าชั่วโมงก่อนเรียก `ChangeHour` ทั้งสอง site ⇒ **การเดินทาง
สะสม `hour` ผ่าน ChangeHour ด้วยอัตรา 2 เท่าเมื่อ fastMode** [OBSERVED] — ปิด "จุด write ของ hour จากการเดินทาง"
(time-notes.md:40) ด้วย call site ชัดเจน; อัตราต่อหน่วยระยะ (~0.0157 ชม./หน่วย) ยังเป็นค่า RUNTIME เดิม
(สูตรเต็มจากระยะ/เวลายังถอดไม่ครบ — UNKNOWN เชิงสัมประสิทธิ์)

### 2.14 เสริม — ChangeDay leaf (0x790E30) และ controllers
- **leaf housekeeping** (1,370 insns; รายการ call ที่พบ): `ManageWorldEvent` · `CheckWorldPlotEventDataBase` ·
  `GenerateRandomEnemy` ×1 · `ChangeMonth` (0x791055) · injury ×3 (`ChangeExternal/Internal/PoisonInjury`) ·
  `ManageTagTime` · `TryIdentifyAllItem` · `GetHeroAutoInjuryChange` ×3 · `ResetAllAIMananged` · `RemoveEvent` ·
  `TimeData.IsMeetingTime` + `MeetingController.SetMeetingEnd` · `SendMessage` ×2 · `Monitor.Enter/Exit` ·
  `PlayerPrefDictionary.GetInt` ×2 · `Debug.LogError` — ยืนยันว่า housekeeping รายวันอยู่ใน leaf จริง
  (ตอบข้อ GUESS เดิม time-notes.md:66) [OBSERVED เชิงรายการ — ลำดับเงื่อนไขยังไม่ถอดครบ]
- `WeatherController.Update` (0xC19DE0): ใช้ `RealTime.get_deltaTime` ×3 (คูณค่าก่อนใช้) + `Time.get_deltaTime` ×1
  [OBSERVED เชิงโครง] · `TimeScaleController.Update` (0xAA71D0): `get_timeScale`/`set_timeScale` + RealTime ×2 —
  รูปร่างตรงกับ battle-notes §6 เดิม [OBSERVED]
- singleton getters จริงอยู่ที่ GameController 0x7E6550 / PlotController 0x68EBD0 / WorkingUIController 0x9CBF70 /
  HudController 0xB0E370 / SpeShowController 0xC691E0 / TimeScaleController 0xAA7450; thunk 0x46C100/0x46C460/0x46C160/0x46C680
  (init-guard + อ่าน statics) ที่ bodies เรียกใช้**ไม่อยู่ใน ScriptMethod** — สรุปว่าเป็น accessor เทียบเท่า get_Instance
  จากบริบทการใช้ [INFERRED]

---

## 3) ตาราง "ข้อสรุปเดิม → ข้อสรุปใหม่" (เลขบรรทัดอ้างจาก grep ไฟล์จริง 2026-10-27)

| # | ข้อสรุปเดิม (แหล่ง) | ข้อสรุปใหม่ (รอบนี้) | สถานะ |
|---|---|---|---|
| 1 | "จุด write ของ `hour` = UNKNOWN" — time-notes.md:40 | writer = 3 จุด 2 method: ChangeHour (0x79268B/0x79280B) + GameDataUpdateChecking clamp `min(hour,24)` (0x7A2272, caller StartLoadGame เท่านั้น) | ปิดฝั่ง static [OBSERVED] |
| 2 | "`ChangeHour` … **ไม่พบ caller**" — time-notes.md:42, :176 | พบ **9 จุด** (E8=3/E9=6) รวม FinishExploreMap, BigMapController.FixedUpdate ×2, WorkingUIController.Update | หักล้าง [OBSERVED] |
| 3 | ปั๊มงาน "→ เรียก `ChangeDay()` วันละครั้ง" — time-notes.md:39, :119 | Update เรียก **`ChangeHour(24.0)`** (0x9CBC30) → ChangeDay อยู่ใน ChangeHour; Update ไม่ได้เรียก leaf ตรง | แก้กลไก [OBSERVED] |
| 4 | "`>>` เป็นแค่การเร่ง animation วันยังเดินทีละวันเหมือนเดิม" — time-notes.md:59–60; "skip ตัดแค่ช่วง pauseTime" — time-followup-notes.md:112–114 | `skipping` **ลด threshold วันเป็น 0.2×/0.5× dayTime** (ตาม pref `"TestMode"`) + เร่ง animation 5×/2×; SkipButtonClicked/SetSkippingState แค่ toggle ธง 0x62 | หักล้าง [OBSERVED] |
| 5 | "`Update()` อ่าน scaled หรือ unscaled delta — UNKNOWN" — time-followup-notes.md:34, :110 | **unscaled** (`RealTime.get_deltaTime` tail-jmp → `Time.get_unscaledDeltaTime`; Update เรียก ×2) — `timeScale=0` ไม่หยุดปั๊ม | ปิดฝั่ง static [OBSERVED] |
| 6 | "จุด write ของ `hour` จากการเดินทาง = UNKNOWN" — time-notes.md:40 (worldmap §7) | BigMapController.**FixedUpdate** ×2 site, `fastMode` คูณ 2.0 ก่อน ChangeHour | ปิดฝั่ง static [OBSERVED] |
| 7 | "ผลของการยกเลิก … ยังไม่เคยวัดเลย = NOT-RUN; noCancel ของการฝึกไม่เคยอ่านค่า" — time-followup-notes.md:148–149, :160–161 | ฝั่ง static: ฝึกใช้ 6p ⇒ **noCancel=false** ยกเลิกได้; เส้นยกเลิก = plot `"中止;MiddleStopWork"` → `StopWorking(false)` → finishFuc ยิงเสมอ แต่ handler ฝั่ง max-skill gate ด้วย workFinished ⇒ ไม่ได้รางวัล; ผล resource/เงินยังต้องวัด RUNTIME | ปิดบางส่วน [OBSERVED + NOT-RUN คงเหลือ] |
| 8 | "finishFuc ผ่าน PlotController [GUESS]" — time-followup-notes.md:88 | dispatch จริง = **SendMessage string** ไปยัง target เดียว (thunk 0x46C460, [INFERRED] = PlotController.Instance); handler ฝั่ง PlotController + wrapper ไป StudySkillController | ยกระดับ GUESS→OBSERVED |
| 9 | "ไม่มี paused flag ในเกมเลย (grep ครบ)" — time-notes.md:35 | ยังถูกในขอบเขต GameController/WorldData แต่เติม: ปั๊มมี **native freeze 3 ทาง** (plotHappen / speShowUIPanel.activeSelf / HudPanelActive) — แยกจาก P1–P6 ของ followup | เพิ่มเติม [OBSERVED] |
| 10 | "ไม่มี autosave ตอนออก" (RUNTIME) — time-notes.md:135 | ยืนยันเดิม + เติม: autosave ระหว่างเล่นมีจริง (`needAutoSave`→Save(slot 0), gate `DisableAutoSave`), F5→Save(slot 10), F9→Load(slot 10) | เพิ่มเติม [OBSERVED] |
| 11 | "weather … จังหวะ NOT-RUN / delta แบบไหน UNKNOWN" — time-notes.md:41, time-followup-notes.md:38 | ChangeHour ส่ง `−delta/24` ให้ `ChangeWeatherLastTime`; WeatherController.Update ใช้ RealTime (unscaled) ×3 | ปิดฝั่ง static [OBSERVED] |
| 12 | "leaf body ยังไม่ถูกอ่าน [UNKNOWN]" — time-notes.md:49 | housekeeping รายวันอยู่ใน leaf จริง (รายการ call §2.14) — ยังไม่ถอดลำดับเงื่อนไขทั้งหมด | ปิดบางส่วน [OBSERVED] |

---

## 4) บันทึกการแก้ตัวเองระหว่างงานสำรวจ (false positive ที่จับได้ — รวมจากรอบก่อน + รอบนี้)

1. **mainThreadWaitForLock offset 0x28 ซ้ำกับ needAutoSave** (จากรอบก่อน): field 0x28 ของ GameController ใน
   dump.cs:376441 คือ `needAutoSave` — การชี้ 0x28 ไปที่ชื่ออื่น (mainThreadWaitForLock จากบริบท hook) เป็น
   false positive จาก offset collision — รอบนี้ยืนยัน 0x28 = needAutoSave ทั้ง dump.cs และ disasm
2. **สแกนรอบแรกขาด E9** (จากรอบก่อน): นับ caller เฉพาะ E8 ทำให้เห็น caller ไม่ครบ — รอบนี้สแกน E8+E9 พร้อม
   validate ทุกจุด; ผล: ChangeHour 9 จุดมี E9 ถึง 6 จุด (ถ้าสแกนแต่ E8 จะเห็นแค่ 3)
3. **StudyFightSelfFinish vs RealStartStudySkill** (จากรอบก่อน): เคยสรุปสลับกัน — รอบนี้ชี้ชัด:
   `RealStartStudySkill` = finishFuc ของเส้น StartStudySkill (gate workFinished ที่ 0xFE55CB) ส่วน
   `StudyFightSelfFinish` (0x66CC40) = handler จบฝึกสำนักที่คิด exp/hero ช่วย **และไม่มี gate**
4. **ChangeDay เรียกจาก Update vs ผ่าน ChangeHour** (จากรอบก่อน, ยืนยันซ้ำรอบนี้): Update เรียก ChangeHour(24)
   — leaf hits ที่ hook เคยนับ ตอนนี้อธิบายได้ครบผ่าน Update→ChangeHour→leaf
5. **(รอบนี้) เดาผิด "ชั้น pause ของปั๊ม = TimeScaleController.paused"**: ตอนแรกอ่าน `cmp byte [rcx+0x18]` ใน
   Update ว่าเป็น TimeScaleController.paused — resolve slot 0x1D93C00 แล้วเป็น **PlotController**, 0x18 =
   **plotHappen** (dump.cs:382609) — แก้ก่อนเขียนลงโน้ต
6. **(รอบนี้) "ChangeHour เป็น writer เดียว" แทบกลายเป็น over-claim**: สแกนเชิงลบ `scan_hour_writes.py` เจอ
   `GameDataUpdateChecking` เขียน `[WorldData+0xB4]` เพิ่ม (clamp หลังโหลด) — ถ้าไม่ทำ negative scan จะเขียน
   "จุดเดียว" ผิดลงโน้ต
7. **(รอบนี้) bug ขอบเขต body ในสคริปต์ร่างแรก**: ใช้ "start ของ owner range" แทน "end" ทำให้ body ถูกตัดที่
   0x800 ไบต์ — เห็นตอน ChangeDay(int) (ควรยาว 0x30) ยาว 0x800 — แก้ (`body_end`) แล้ว disasm ใหม่ทั้งชุด;
   ไฟล์ bodies/*.txt ทั้งหมดในโฟลเดอร์นี้คือชุดหลังแก้
8. **(รอบนี้) label collision จาก generic sharing**: `GetComponent<StudySkillStarController>` (0x967EF0) กับ
   `.ctor` stub (0x3A1810) มี hits เป็นพันจากคลาสอื่น — ตัดออกเป็น NOISE ใน xref-summary.txt แล้ว (หากไม่ตัด
   "ผู้เรียก StudySkillController" จะผิดเป็นร้อย)
9. **(รอบนี้, จับตอน purity check) นับ StartWorking รวมทุก overload แล้วเขียน "60 จุด → 42 = UNVERIFIED"**:
   สคริปต์ summarize ร่างแรกจับกลุ่มตาม**ชื่อ** (overload ชื่อซ้ำมารวมกัน) ทำให้อ่าน 51 ไปเป็นของ 0x9CABF0 —
   ตรวจซ้ำด้วย Counter ตรงจาก TSV พบ 0x9CABF0 = **42 พอดี** (E8 ล้วน) ⇒ claim "42" REPRODUCED; แก้
   summarize เป็นกลุ่มตาม (RVA, ชื่อ) และแก้โน้ตก่อนส่งมอบ — ตัวเลขจริง: 0x9CABF0=42 (38 ext + 4 int),
   ทุก overload รวม 51 (external 47)

---

## 5) evidence gaps — ปิดแล้ว / ยังเปิด

### ปิดแล้วด้วยรอบนี้ (ฝั่ง static)
- writer ของ `WorldData.hour` ทั้งหมดที่สแกนพบ (3 จุด 2 method) — รวมจุดเดินทาง (FixedUpdate ×fastMode)
- caller ของ `ChangeHour` (9 จุด) และ `ChangeDay` leaf (2 จุด)
- scaled-vs-unscaled ของปั๊มงาน + weather + fastMode (unscaled ทั้งชุด)
- สูตร T(N) เชิงโครง + ค่า float ทั้งหมดใน Update (24.0/0.2/0.5/2.0/5.0/1.0/0.4)
- `noCancel`: 6p = false ตายตัว, 7p = จาก caller, guard ของ TryCancelWorking ครบ 2 เงื่อนไข
- เส้นยกเลิกจนถึง `StopWorking(false)` + กลไก plot-choice `"中止;MiddleStopWork"`
- gate `workFinished` ของ RealStartStudySkill และ StudyMaxFightSkillFinish (และ "ไม่มี gate" ของ StudyFightSelfFinish)
- save path ใน Update: autosave slot 0 / F5 slot 10 / F9 slot 10 / gate `GlobalData.DisableAutoSave`
- กลไก skip: toggle `skipping` + threshold ×0.2/0.5 + anim ×5/2 (pref `"TestMode"`)

### ยังเปิด (เรียงตามที่โจทย์ขอ + ที่พบเพิ่ม)
| gap | สถานะ | ทางปิด |
|---|---|---|
| ผู้ตั้ง `needAutoSave` (GameController.0x28) | ไม่พบในช่องทางที่สแกน (writer scan ยังไม่ได้ทำแบบ [+0x28]) | สแกน store `byte [reg+0x28]` เฉพาะบริบท GameController / hook |
| writer ของ `plotHappen` และ `speShowUIPanel` | รู้ว่าใครอ่าน (ปั๊ม) — ใครเขียนยังไม่ได้ไล่ | สแกน store ที่ offset 0x18 ของ PlotController / 0x?? ของ SpeShowController + xref `set_activeSelf` |
| ครบชุด caller ของ `HudPanelActive` | นับได้เฉพาะจุดที่ตรงกับ target list รอบนี้ (Update จุดเดียวที่เกี่ยว) | สแกน target 0xB09ED0 เดี่ยว ๆ |
| ผู้เรียก `StudyFightSelfFinish` (ผ่าน string dispatch) | เห็นแค่ชื่อ handler — สายส่งเป็น string ใน data/scene | ตามหา literal `"StudyFightSelfFinish"` ใน TextAsset/scene (level3 grep) |
| caller ของ `TryCancelWorking` ที่ผูกใน scene (ปุ่ม UI) | ไม่พบใน call graph — ผูกผ่าน UnityEvent/serialized | scene grep / hook |
| ปลายทางหลัง plot-choice ยกเลิก (คืน resource?) | static ไม่ครอบ (binding ใน asset) + ไม่เคยวัด RUNTIME | RUNTIME บนสำเนาเซฟ (ขั้นที่ 1 เดิมของ followup) |
| ค่ารวม T(N) บนเครื่องจริงที่ rate อื่น (8–12 s/วัน) + ขั้น `2×pauseTime` วันสุดท้าย | โครง OBSERVED แต่ตัวเลข RUNTIME มี n=1 เท่านั้น | วัด RUNTIME ตาม followup §5 ขั้นที่ 2 |
| ลำดับเงื่อนไขเต็มของ ChangeDay leaf (housekeeping) | รายการ call ครบ แต่ control flow ละเอียดยังไม่ถอด | ถอดต่อจาก `bodies/GameController_ChangeDay_leaf.txt` ที่เก็บไว้ |
| กลไก P4 (เมนู Esc หยุด sim) | รอบนี้ไม่พบธง pause กลางใน GameController.Update (บล็อกท้ายเป็น trial-dialog + P-key TestMode) — ยัง UNKNOWN เหมือนเดิม | วัด RUNTIME + trace SureMenu.pause ผู้เรียก |
| อัตรา hour ต่อระยะทางจริง (สัมประสิทธิ์ในสูตรเดินทาง) | เห็นโครง (ChangeHour×fastMode) ไม่เห็นสูตรระยะ | ถอดต่อ FixedUpdate / วัด RUNTIME |
| ผลของ `skipping` ต่อ cadence จริงที่ dayTime ใหม่ | โครงใหม่ ยังไม่เคยวัด | RUNTIME ตาม followup §5 |

### งานที่ต้องวัด RUNTIME (เสนอ — ต้องอนุมัติแยก ตามเดิม)
(1) TryCancelWorking บนสำเนาเซฟ (ผล resource/reward/callback) — (2) ตาราง mapping dayTime ที่ 8–12 s/วัน
A/B — (3) Global Pause กับปั๊มที่ตอนนี้รู้แล้วว่าใช้ unscaled ⇒ **timeScale ไม่พอ ต้อง gate ที่ปั๊มเอง** —
(4) ผู้ตั้ง needAutoSave (watch เมื่อ autosave เกิด)

---

## 6) purity

- **git status ก่อนงาน (game repo, 05:16):** `M STAGEPLAN.md`, `D battle-notes.md`, `D worldmap-notes.md`,
  `m il2cpp-frida-mcp`, untracked `.opencode/ Plans/ Prompt.md README.txt Roadmap StagePlan.txt context.txt`
  (สแนปช็อตเดิมของ repo ก่อนเริ่ม — ไม่มีการเปลี่ยนแปลงใด ๆ จากรอบนี้)
- **git status ก่อนงาน (mod repo LongYinCoop, 05:1x):** HEAD `dd13491` … + untracked ชุดเดิม (diff ไฟล์
  S2a-4/stageplan, coop-test/, evidence/ ก้อนเก่า) — ไม่แตะ
- **สิ่งที่เขียนใหม่ทั้งหมดของรอบนี้ = โฟลเดอร์นี้เท่านั้น**
  (`LongYinCoop/evidence/TIMEstatic-20261027-051647/`): โน้ตนี้ + สคริปต์ 6 ไฟล์ +
  `xref-scan.tsv` `xref-ff15.tsv` (header เปล่า — 0 hit) `xref-summary.txt` `floats-observed.tsv`
  `slots-resolved.tsv` `hour-writes.tsv` `scan-run.log` (log การรันสแกนรอบสุดท้าย) + `bodies/` 30 ไฟล์ —
  ตรวจ git status หลังงานอีกครั้งในรายงานท้าย session (แนบอยู่ในข้อความส่งมอบ)
- **ขอบเขต commit ของโฟลเดอร์นี้ (มติผู้ใช้ 2026-10-27): ต้องมีเฉพาะ** โน้ต (`time-static-notes.md`) +
  สคริปต์ 6 ไฟล์ `.py` + **TSV ขนาดเล็ก 3 ไฟล์** (`hour-writes.tsv` 2.5 KB, `xref-ff15.tsv` 43 B,
  `slots-resolved.tsv` 58 KB) · **ห้าม commit**: `bodies/` 30 ไฟล์ (ผล disassembly ของเกม),
  `xref-scan.tsv` (349 KB) + `floats-observed.tsv` (481 KB) (TSV ขนาดใหญ่), `xref-summary.txt` (11 KB)
  และ `scan-run.log` (356 KB — โดน `*.log` ใน .gitignore อยู่แล้ว) — ทั้งหมดคงอยู่บนดิสก์เป็นหลักฐานประกอบ
  · ไฟล์ disassembly ของเกมทั้งหมดไม่เข้า git (เช่นเดียวกับ `gameassembly.disasm.txt` 471 MB ที่
  .gitignore:68 กันไว้แล้ว)
- **ไฟล์เกมที่ถูกอ่าน:** `GameAssembly.dll` (hash ตรง VERSION.md), `Il2CppDumper/{script.json, dump.cs}` —
  เปิดแบบ read-only เท่านั้น · เกมไม่ถูกเปิดรัน (ไม่มี process เกมในรอบนี้) · ไม่มีการเขียนไฟล์ใดนอกโฟลเดอร์ evidence
- **เครื่องมือ:** Python 3.11.9 + pefile 2024.8.26 + capstone 5.0.7 (มีอยู่แล้ว — ไม่ติดตั้งเพิ่ม) · ไม่ใช้
  dumpbin/ilspycmd ในรอบนี้ (ต่างจาก SP5body ที่ใช้ dumpbin — รอบนี้ capstone ตามข้อกำหนด)

---

## ภาคแนบ: สถานะการตรวจซ้ำ 8 ข้อ (สรุปเดี่ยว)

| ข้อ | ผลการตรวจซ้ำ |
|---|---|
| (1) ChangeHour caller E8+E9 + owner | **REPRODUCED** — 9 จุด (E8=3/E9=6), owner ครบทุกจุด §2.2 |
| (2) RealTime.get_deltaTime → unscaledDeltaTime | **REPRODUCED** — tail-jmp 0xCFD4A2 → 0xDA9FD0 |
| (3) WorkingUIController.Update + floats + T(N) | **REPRODUCED** — §2.4 ครบทุกค่า 24.0/0.2/0.5/2.0/5.0 |
| (4) noCancel 6p vs 7p | **REPRODUCED** — 6p=false ตายตัว / 7p=arg |
| (5) MiddleStopWork → StopWorking(false) + TryCancel guard | **REPRODUCED** — E9 0x54EF55; guard 2 เงื่อนไข |
| (6) SureStartStudySkill → RealStartStudySkill + gate | **REPRODUCED** — finishCallFuc string + gate 0xFE55CB |
| (7) StudyMaxFightSkillFinish workFinished=false | **REPRODUCED** — gate read-only ที่ 0x671242 (ไม่เขียน workFinished) |
| (8) Update autosave/F5/F9 + DisableAutoSave | **REPRODUCED** — slot 0/10/10 + statics+0x10 |
| (เสริม) "StartWorking 42 call-site" | **REPRODUCED** — 42 = E8 sites ที่ชี้ overload 6p (0x9CABF0) พอดี; ทุก overload รวม 51 (external 47) ตาม §2.9 |
| (เสริม) "StopWorking 6 caller" | **REPRODUCED** — 6 จุดตรงเป๊ะ §2.8 |
