# p-t0-static-followup-audit — P-T0: Static Follow-up Audit (ก่อน Runtime Verification)

> **label:** `TIME-T5-P0-20261028-011213` · ต่อเนื่องจาก `evidence/TIME-T5-20261028-003723/activity-multiplayer-audit.md` (T5) ·
> **งาน read-only 100%**: ไม่เปิดเกม · ไม่ attach/invoke/hook · ไม่ build/deploy · ไม่แตะเซฟ/pref/`TestMode` · ไม่แก้ protocol ·
> ไม่ commit/tag/reset · **ไฟล์ใหม่ของรอบนี้ = ไฟล์เดียวในโฟลเดอร์นี้เท่านั้น**
> **วิธี:** (1) อ่าน disassembly bodies ที่มีอยู่ใน `evidence/TIMEstatic-20261027-051647/bodies/` (แตะไฟล์เดิม ไม่แก้) ·
> (2) **targeted disasm เฉพาะจุด** ด้วย loader เดิม (`time_static_common.py` + pefile + capstone — เปิด `GameAssembly.dll`
> hash `33c56def…7920c1` ตรง VERSION.md แบบ read-only, รันผ่าน `python -` heredoc **ไม่สร้างไฟล์ใหม่นอก evidence นี้**,
> ไม่สแกนทั้งโมดูล — scan เดียวที่ทำคือ xref เดี่ยวของ `SetSkippingState` 1 target) · (3) grep `Il2CppDumper/dump.cs`
> อ่านอย่างเดียว (ลายเซ็น StartWorking)
> **anchor:** mod repo HEAD `df46de1` (ตรวจ 01:12 และปิดงานแล้วไม่เปลี่ยน) · game repo HEAD `c672dc2`
> ป้าย: **OBSERVED** = อ่านเองจาก disasm/body รอบนี้ (แนบ RVA ทุกจุด) · **RUNTIME** = วัดจาก process จริง (session ก่อน) ·
> **DUMP** = จาก `dump.cs` · **INFERRED** = อนุมานจาก OBSERVED · **UNKNOWN** = หลักฐานไม่พอ · **NOT-RUN** = ต้อง runtime แยก ·
> **RETRACTED / CORRECTED** = ข้อสรุปเดิมของ T5/โน้ตก่อนหน้าที่รอบนี้แก้

---

## 1. Executive summary

รอบนี้ปิดช่องว่าง 3 กลุ่มของ T5 ด้วย body เต็ม + targeted disasm โดยไม่สแกนใหม่ทั้งโมดูล:

1. **`noCancel` ปิดครบทุก overload และทุก call site ที่สแกนได้** — 6p (`0x9CABF0`) เขียน `noCancel=0` **ตายตัว**
   (0x9CAC6E `xor edi,edi` → 0x9CACF6 `mov byte [rbx+0x61], dil`); overload 5p / 2p `(int,string)` / `List<string>` /
   3p `(string,int,string)` **delegate → 6p ทั้งหมด** ⇒ `noCancel=0` ทางโครง; 7p (`0x9CB090`) รับจาก arg และ **caller
   เดียว `PlotController.TryThinkAboutMissingSkill` ส่ง `true`** (0x68543A `mov byte [rsp+0x38], 1`) — ปิดคำถามค้าง
   "ค่าที่ site ส่งยัง pin ไม่ได้" [OBSERVED]
2. **ข้อขัดแย้งระหว่างเอกสาร 10 จุดคลี่คลายครบ** — รวม 2 ข้อที่ต้อง**แก้ข้อสรุปเดิม**: (ก) `time-static-notes.md §2.9`
   **สลับ label overload** 0x9CAE00/0x9CAE90/0x9CB000 (ตาม `dump.cs:392721-392730` ที่อ่านตรงแล้ว) และ T5 §3.1
   รอยมาด้วย — ตัวเลข per-RVA ไม่เปลี่ยน; (ข) **โมเดล M-DISASM ของ T5 (N+1)×pauseTime ผิด** — โครงจริงจาก
   `Update` เต็มคือ **T(N) = N×dayTime×k + (N+2)×pauseTime** (มี initial pause + วันสุดท้าย 2×pause ที่ถูกนับจริง
   ก่อน finish เฟรมถัดไป) — **โครงของ M-PROMPT ได้รับการยืนยันจาก disasm** และ M-DISASM เดิมถูกถอน (RETRACTED)
3. **ข้อค้นพบใหม่ที่กระทบกติกา mod โดยตรง:** `StartWorking` (ทุก overload ที่ถอด) **ไม่ reset `workFinished` (0x60)**
   และไม่ reset `skipping` (0x62) ⇒ `workFinished` เป็น **latch ผลล่าสุดที่ค้างข้ามงาน** — กติกาเดิมของ repo
   "งานกำลังเดิน = `working && !workFinished`" (area-notes S4c, T5 §4.3) **ใช้ไม่ได้ตั้งแต่งานที่สองเป็นต้นไป** —
   ทดสอบที่ถูกคือ `working == 1` [OBSERVED → CORRECTED]; และ `PlayerQuickTravel` พิสูจน์แล้วว่าเป็นกิจกรรมปั๊มเต็มรูป
   (จ่ายเงินก่อน 0x7D99A7, `dayNum = BigMapPos.QuickTravelTime(...)`, **ไม่มี guard เช็ค `working` เลย**)

**สิ่งที่ยังปิดไม่ได้ด้วย static** (ส่งต่อ V1/V2): ผล resource/เงินจริงตอนยกเลิก (refund — ไม่พบใน handler ที่อ่านครบ 4
ตัว แต่**ห้ามตีความว่าไม่มีที่อื่น**), ตัวเลข T(N) ที่ rate ใหม่ (โครงยืนยันแล้ว ตัวเลขวัด n=1 ยังเพี้ยน 2×pause=0.5 วิ),
และ dispatch string ของ finish handler บางสายที่ผูกใน plot-data

---

## 2. ตารางข้อขัดแย้งของหลักฐานและวิธีคลี่คลาย

ทุกข้อคลี่คลายด้วยการกลับไปอ่าน **instruction จริง** ใน bodies/targeted disasm — ไม่เลือกฝั่งที่ถูกกล่าวซ้ำมากกว่า

| # | ข้อ (แหล่ง A vs แหล่ง B) | หลักฐานต้นฉบับที่ใช้ตัดสิน | ผล | สถานะ |
|---|---|---|---|---|
| C1 | ค่า `noCancel` ของ 6p — `time-static-notes.md §2.6` บอก "= 0 ตายตัว (`xor edi,edi`)" vs `time-audit-notes.md §5` บอก "6p เขียน 0x61 **จาก trailing arg**; ค่าที่ site ส่งยัง pin ไม่ได้" | body `bodies/WorkingUIController_StartWorking_6p.txt`: 0x9CAC6E `xor edi,edi` → 0x9CAC70 `mov [rbx+0x4c], esi` … → 0x9CACF6 `mov byte [rbx+0x61], dil` — `dil=0` ตลอด, **ไม่มีการอ่าน stack arg ใด** เข้าช่อง 0x61 | **static-notes ถูก**; ประโยคใน audit-notes §5 ผิด (audit-notes §1.1 ข้อ 2 ของตัวเองก็สอดคล้องกับ static-notes) · 6p มี 6 params (this + 6) ตาม `dump.cs:392733` — ไม่มี bool ให้รับ | RESOLVED |
| C2 | ค่า `noCancel` ที่ caller ส่งเข้า 7p — T5 §3.1/§11.9 และ audit-notes §5 บอก "pin ไม่ได้" | targeted disasm `PlotController.TryThinkAboutMissingSkill` (0x685030, call site 0x68545C): 0x68543A `mov byte [rsp+0x38], 1` — [rsp+0x38] ที่ caller = arg ที่ 8 = `_noCancel` ของ callee (`movzx eax,[rsp+0x68]` ที่ 0x9CB196 ยืนยันช่องเดียวกัน) | **noCancel = true** ที่ call site เดียวของ 7p [OBSERVED] — งาน "参悟" (dayNum=1, finish="FinishThinkAboutMissingSkill") เป็นงานแรกที่พิสูจน์ว่าห้ามยกเลิกทาง UI | RESOLVED (ปิด gap เดิม) |
| C3 | ชื่อ overload — `time-static-notes §2.9` (และ T5 §3.1 ที่รอยมา) ระบุ 0x9CAE00=List\<string\>, 0x9CAE90="3p", 0x9CB000="(int,string)" vs ลายเซ็นจริง | `Il2CppDumper/dump.cs:392721-392730` (grep ตรง อ่านอย่างเดียว): **0x9CAE00 = `StartWorking(int dayNum, string callFuc)`**, **0x9CAE90 = `StartWorking(List<string> paramList)`**, **0x9CB000 = `StartWorking(string _workName, int dayNum, string callFuc)`** | **สลับ label กันระหว่าง 0x9CAE00 ↔ 0x9CAE90 + 0x9CB000 ติด label ผิด** — ตัวเลข call site per-RVA **ไม่เปลี่ยน** (42/51/47 คงเดิม) · T5 §3.1 ตารางแถว label ต้องแก้ตาม dump.cs แต่ "ผู้เรียก 0x9CB000 = WaitUIController.SureButtonClicked (+2)" ฯลฯ ยังถูก | RESOLVED (ข้อผิดในโน้ต — แก้เอกสารรอบหน้า ห้ามแก้ไฟล์เดิมรอบนี้) |
| C4 | จำนวน caller ของ `StopWorking` — audit-notes §1.1 "5 call sites" vs static-notes §2.8 "6 จุด (รวม MiddleStopWork E9)" | วิธีสแกนต่างกัน: audit-notes = E8 rel32 อย่างเดียว (ระบุไว้ในหัวไฟล์); static-notes = E8+E9 validate ทุกจุด · `xref-summary.txt:124-129` (E8+E9) = 6 จุด | ไม่ใช่ขัดแย้งเชิงข้อเท็จจริง — เป็นขอบเขตเครื่องมือ; **ตัวเลขมาตรฐาน = 6 (E8=5, E9=1)** — บทเรียน E9 เดิม (static-notes §4 ข้อ 2) | RESOLVED |
| C5 | `finishFuc` เมื่อ `StopWorking(false)` — ทุกเอกสารบอก "ยิงเสมอ" | body `bodies/WorkingUIController_StopWorking.txt`: 0x9CB25F `mov byte [rbx+0x60], dil` (workFinished=arg) → เช็ค finishCallFuc(0x38) ไม่เป็น null → SendMessage 1-arg (0x9CB2C2) หรือ 2-arg (0x9CB2FA) — **ไม่มี branch ใดอ่าน `finished` นอกจากตอนเขียน 0x60** | ยืนยัน **ยิง finishFuc เสมอทั้งจบและยกเลิก** [OBSERVED] — ผลที่ต่างกันเกิดที่ **handler ปลายทาง** (ดู Section 4) | CONFIRMED |
| C6 | handler gate — T5 §4.3/§4.2 มี 3 handler (gate ×2, ไม่ gate ×1) | body เต็ม `PlotController_StudyFightSelfFinish.txt` (ไม่มี read 0x60 เลย) + `PlotController_StudyMaxFightSkillFinish.txt` (gate 0x67123E) + `PlotController_RealStartStudySkill.txt` (gate 0x570030) + **targeted disasm `FinishWorkInProductionBuilding` (0x4F2920): gate ที่ 0x4F2B46 `cmp dword [rcx+0x50], 0; jle` = เช็ค `totalWorkingDay` — ไม่มี read ของ 0x60 เลย** | บัญชีขยายเป็น **4 handler**: gate-workFinished ×2, **ไม่ gate ×1**, **gate-totalWorkingDay ×1 (ใหม่ — ตัวแปรลำดับงาน ไม่ใช่ผลล่าสุด)** | EXPANDED |
| C7 | "รางวัลบางส่วน" จาก `StudyFightSelfFinish` — T5 §4.2(ค) สรุป "exp ค้างใช้ต่อ" | body เต็ม 0x66CC40: เข้า handler ทันที (ไม่อ่าน 0x60) → `GetAreaAvailableHelpHero` → ถ้า list ว่าง (0x66CD0C) **หรือ** roll ไม่ผ่าน (0x66CD47, โอกาส `min(0.05+n×0.025, 0.25)`) → `GetStudyFightExp(param)` (0x66D08A→0x517B70); roll ผ่าน → สุ่ม hero ช่วย + คิด exp จาก `KungfuSkillLvData` + `AddPlot` (0x66D059) | พฤติกรรม**เดียวกันทั้งจบปกติและถูกยกเลิก** (ไม่มี input จาก workFinished); "รางวัลบางส่วน" ที่แท้จริงมาจาก **workingFuc รายวัน** `"GetStudyFightExp;"` ที่ยิงไปแล้วต่อวัน (slot 0x1DB50D8) — ส่วน finish handler เป็น "โบนัส/บทสรุป" | REFINED |
| C8 | โมเดล T(N) — T5 §4.4 M-DISASM `(N+1)×pauseTime` vs โครงจริง | `Update` body เต็ม (586 insns) — ลำดับครบ (รายละเอียด Section 6): **StartWorking ตั้ง initial pause** (6p 0x9CACAA `mov [rbx+0x58], ecx` จาก static pauseTime) ที่ T5 ไม่ได้นับ; pause วันสุดท้าย 2×pause **ถูกนับจริงก่อนจบ** (pause-complete path 0x9CBDD8 `jle 0x9CBCA4` = return เปล่า ๆ → **เฟรมถัดไป** counting-entry เจอ `leftWorkingDay<=0` → finish 0x9CBCC7) | **M-DISASM เดิมผิด → RETRACTED**; โครงที่ถูก = `(N+2)×pauseTime` = **โครงของ M-PROMPT** [OBSERVED]; คงเหลือแค่ความขัดแย้งตัวเลขกับ RUNTIME n=1 (12.5 vs 13.0 วิ) — UNKNOWN, ตัดสินที่ V2 | CORRECTED |
| C9 | กติกา "งานกำลังเดิน = `working && !workFinished`" — area-notes S4c + T5 §4.3 | writers ของ 0x60 ทั้งหมดที่ถอด = Update-finish branch (0x9CBCF6 →1) และ StopWorking (0x9CB25F →arg); **StartWorking 6p/7p ไม่เขียน 0x60**; `PlotController.StartWorking(string)` (bodies, 0x6603F0) ก็ไม่เขียน | `workFinished` = **latch ผลงานล่าสุด** — หลังจบงานปกติครั้งแรก มันค้าง 1 ตลอดงานถัดไปจนกว่างานนั้นจบ/ถูก StopWorking ⇒ กติกาเดิม **ใช้ไม่ได้ตั้งแต่งานที่สอง** — ทดสอบที่ถูกคือ `working == 1` (ทั้ง finish และ StopWorking เคลียร์ working) · caveat: ครอบ writers ที่ถอดแล้ว; writer อื่นนอก bodies = ไม่ได้สแกนทั้งโมดูล | CORRECTED |
| C10 | `skipping` ถูก reset เมื่อเริ่มงานใหม่? — T5 §11.11 (hypothesis) | ไม่มี store ไป 0x62 ใน 6p/7p/StopWorking/TryCancelWorking/Update(รวมกิ่ง finish)/PlotController.StartWorking(string); writers ที่พบ = `SkipButtonClicked` (toggle ตรง 0x9CAA9F `mov [rbx+0x62], al` โดย al=!เดิม) และ `SetSkippingState` (0x9CA928) ซึ่ง **xref E8/E9 = 0 caller** (scan เดี่ยว 1 target รอบนี้) ⇒ ถูกเรียกทาง indirect (SendMessage/virtual/serialized) เท่านั้น | **ยืนยัน hypothesis**: เริ่มงานใหม่ไม่เคลียร์ skipping — งานใหม่หลังกด `>>` ค้างจะเดินที่ threshold ×0.2/×0.5 ทันที จนกว่าจะมีใคร toggle ปิด [OBSERVED ภายใต้ direct-call ขอบเขต; indirect dispatch = UNKNOWN] | CONFIRMED |

---

## 3. `noCancel` call-site inventory

### 3.1 ระดับ overload (ผลสุทธิ์ต่อ `WorkingUIController.noCancel @0x61`)

| overload (RVA) | ลายเซ็นจริง (dump.cs) | ทางไป 6p | `noCancel` สุดท้าย | ป้าย |
|---|---|---|---|---|
| 6p `0x9CABF0` | `StartWorking(string _workName, int dayNum, string callFuc, string callFucParam, string finishCallFuc, string finishCallFucParam)` (dump.cs:392733) | (ตัวจริง) | **0 ตายตัว** — 0x9CAC6E `xor edi,edi` → 0x9CACF6 `mov [rbx+0x61], dil` | OBSERVED |
| 5p `0x9CAD70` | `(int dayNum, string callFuc, string callFucParam, string finishCallFuc, string finishCallFucParam)` (392724) | call ที่ 0x9CADDD | **0** (6p hardcode) | OBSERVED |
| 2p `(int,string)` `0x9CAE00` | `(int dayNum, string callFuc)` (392721) | call ที่ 0x9CAE6E | **0** | OBSERVED |
| `List<string>` `0x9CAE90` | `(List<string> paramList)` (392730) | call ที่ 0x9CAFD4 | **0** | OBSERVED |
| 3p `(string,int,string)` `0x9CB000` | `(string _workName, int dayNum, string callFuc)` (392727) | call ที่ 0x9CB067 | **0** | OBSERVED |
| 7p `0x9CB090` | `(..., bool _noCancel)` (392736) | (ตัวจริง) 0x9CB196 `movzx eax,[rsp+0x68]` → 0x9CB19B `mov [rbx+0x61], al` | **= arg ของ caller** | OBSERVED |

body ของทั้ง 6p/7p **ไม่เช็ค `working` (0x48) ก่อนเขียน state ใด ๆ เลย** (เช็คเดียวคือ gameObject 0x18 สำหรับ SetActive) —
และเขียนชุดเดิม unconditional: `working=1` (0x9CAC65/0x9CB105), `leftWorkingDay=dayNum`, `totalWorkingDay=0`,
`nextDayTime=0`, `resourceNum=0`, `leftPauseTime=pauseTime (initial)`, `workName/callFuc/callFucParam/finishCallFuc/finishCallFucParam`,
`noCancel` — **ไม่แตะ `workFinished` และ `skipping`** [OBSERVED]

### 3.2 call sites สำคัญที่ pin ค่าได้ (งานหลักตามโจทย์ §2)

| call site (RVA) | caller | overload | args ที่ pin ได้ (สตริง resolve จาก ScriptString slot) | `noCancel` สุดท้าย | ป้าย |
|---|---|---|---|---|---|
| 0xFE6535 | `StudySkillController.SureStartStudySkill` | 6p | workName/dialog ค่าเงิน (`StudyMoneyCost`→`ChangeMoney(−)` ตาม static-notes §2.10), `dayNum=StudyDayCost()`, `callFuc=null`, `finishCallFuc="RealStartStudySkill"` | **false** | OBSERVED (chain เดิม + body รอบนี้) |
| 0xC14E4E | `WaitUIController.SureButtonClicked` | 3p `(string,int,string)` | workName=`"等待"`, `dayNum = (int)slider × (GetInt("TestMode")==1 ? **100** : 1)` (0xC14E15–0xC14E39), `callFuc=null` (r9=0) ⇒ **Wait ไม่มี per-day callback**; ช่อง finish ถูกเติมด้วย slot เดียวกัน 0x1D96AE8 (ค่าใน namespace ScriptString = `"Value of flags is invalid."` — ประเภท usage ของ slot ยังไม่ยืนยัน) | **false** | OBSERVED |
| 0x7D9AAB | `GameController.PlayerQuickTravel` | 6p | workName=`"乘车"`, `dayNum=BigMapPos.QuickTravelTime(playerPos, areaPos)`, `callFuc=""` (slot 0x1DC6AE8 = สตริงว่าง ⇒ ไม่มี per-day callback), `finishCallFuc="PlayerQuickTravelToArea"`, `finishCallFucParam`=r14 (โหลดนอกหน้าต่างที่ถอด — UNKNOWN) | **false** | OBSERVED |
| 0x68545C | `PlotController.TryThinkAboutMissingSkill` | **7p** | workName=`"参悟"`, `dayNum=1`, `callFuc=""`, `finishCallFuc="FinishThinkAboutMissingSkill"`, `noCancel` arg = **1** (0x68543A) | **true** | OBSERVED |
| 0x6758FB | `PlotController.SureBuildingWork` | `List<string>` (tail-jmp) | สร้าง List จาก param ของ plot-choice แล้ว tail-call; **List format = `[workName, dayNum, callFuc, callFucParam, finishCallFuc, finishCallFucParam]`** (ถอดจาก body 0x9CAE90: items[0]→workName, items[1]=`Int32.Parse`→dayNum, items[2..5]→สตริงที่เหลือ; pad ให้ครบ 6 ช่อง 0x9CAF00–0x9CAF1E) | **false** | OBSERVED |
| 0x660590 | `PlotController.StartWorking(string)` | 6p | `param.Split(';')` 6 ช่อง: `[workName, days, callFuc, callFucParam, finishCallFuc, finishCallFucParam]` (0x660485 Split, 0x660525 `Int32.Parse` items[1], 0x660590 call) — **แก้รูปแบบเดิมของ area-notes P1-5 ที่เขียน 5 ช่อง**: ตัวอย่างจริง `巡查;3;;;HelpPatrol;` = callFuc=`""`, callFucParam=`""`, finishCallFuc=`"HelpPatrol"` | **false** | OBSERVED |

**ข้อจำกัดที่ต้องรายงาน:** (1) ป้าย `noCancel=false` ของ overload ที่ delegate ไป 6p ผลสุทธิ์เหมือนกันทุกงาน
**แต่ไม่ได้แปลว่า cancellation semantics เหมือนกัน** — ผลยกเลิกขึ้นกับ finish-handler ปลายทาง (Section 4); (2) call site
อื่น ๆ อีก 43 จุด (จาก 47 external) ยังไม่ได้ถอด prologue รายจุด — แต่เพราะทุกจุดชี้ 6p/overload-delegate ที่ hardcode 0,
**ค่า noCancel สุดท้าย = false ทุกจุดโดยโครง** [OBSERVED เชิงโครง] — สิ่งที่ยัง pin ไม่ได้ต่อ site คือ **args อื่น**
(dayNum/callFuc/finishFuc) เท่านั้น; (3) เส้นทางที่เรียก 7p จาก scene/dispatch อื่น = ไม่พบใน call graph [UNKNOWN —
ครอบด้วยข้อจำกัด E8/E9 เดิม]

---

## 4. finish-handler / cancellation inventory

### 4.1 บัญชี handler (ฝั่งที่มีหลักฐานระดับ body)

| handler (RVA) | dispatch ที่มีหลักฐาน | gate | การแก้ state ที่ยืนยันจาก static | เมื่อ `workFinished=false` (ยกเลิก) | refund/partial | ป้าย |
|---|---|---|---|---|---|---|
| `PlotController.RealStartStudySkill` 0x56FF80 → tail-jmp `StudySkillController.RealStartStudySkill` 0xFE5490 | finishFuc `"RealStartStudySkill"` ของสาย `StartStudySkill` (ตั้งโดย `SureStartStudySkill` 0xFE6280 — slot 0x1DA37A0 ตาม static-notes §2.10) | **gate workFinished สองชั้น**: PlotController 0x57002C-0x570030 (`cmp byte [rcx+0x60],0; je ret`) + StudySkillController 0xFE55CB (ตาม static-notes) | ผ่าน gate → `PlayerStudySkill`/บันทึกสถิติ (ตาม static-notes) | **return เงียบ — ไม่ได้วิชา** | เงินค่าฝึกถูกหักก่อนเริ่ม (`ChangeMoney(−)` ใน `StartStudySkill` 0xFE59D0 — static-notes §2.10); **ไม่พบ refund ในสายที่ถอด** — refund จริง ๆ มีหรือไม่ = NOT-RUN | OBSERVED (gate) / NOT-RUN (refund) |
| `PlotController.StudyMaxFightSkillFinish` 0x671140 | finishFuc ของสาย `StudyMaxFightSkillStart` (0x671627) [INFERRED-name — binding string ใน plot-data] | **gate workFinished** 0x67123E-42 (`cmp byte [rcx+0x60],0; je 0x6713CD` — ข้ามทั้งหมด, ไม่เขียน 0x60) | ผ่าน gate → `Int32.Parse(param)` → `WorldData.Player()` → `HeroData.ChangeMaxFightSkill(player, skillId, 1, true)` (0x671304) → dialog `ChangePlot` | **ไม่ได้สกิล — ข้ามทั้งหมด** | — | OBSERVED |
| `PlotController.StudyFightSelfFinish` 0x66CC40 | finishFuc ของสาย `StudyFightSelf` [INFERRED-name] | **ไม่มี gate** — ไม่มี read ของ 0x60 ทั้ง body | no-helper หรือ roll ไม่ผ่าน → `GetStudyFightExp(param)` (0x517B70); roll ผ่าน (`min(0.05+n×0.025, 0.25)` — float pool 0x18C12EC/0x193C1BC/0x193C1C8) → สุ่ม hero ช่วย → exp จาก `KungfuSkillLvData` ×0.01×(1+param) ×100 → `AddPlot` dialog (0x66D059) | **เหมือนจบปกติทุกประการ** (input ไม่มี workFinished) | exp ต่อวันถูกให้ไปแล้วโดย workingFuc `"GetStudyFightExp;"` (0x1DB50D8) — คงอยู่หลังยกเลิก [OBSERVED per-day credit + INFERRED no-reversal] | OBSERVED |
| `PlotController.FinishWorkInProductionBuilding` 0x4F2920 (targeted disasm — ไม่มี body ไฟล์เดิม) | finishFuc ของสายงานผลิต [INFERRED-name — **binding string อยู่ใน plot-data** ( SureBuildingWork สร้าง List จาก param runtime; slot 0x1DA5E50 ไม่ใช่ string literal — pin ไม่ได้)] | **gate `totalWorkingDay > 0`** ที่ 0x4F2B46 (`cmp dword [rcx+0x50], 0; jle 0x4F341E`) — **ไม่ใช่ workFinished** (ไม่มี read 0x60) | อ่าน `resourceNum` ([rcx+0x5C]) ×2 (0x4F2DD9, 0x4F3074) → `GetPlotResultText` + `SinglePlotData` dialog (0x4F2F6C, 0x4F33C0) + `Random`/`AreaData.GetForce`/`GetRandomHero` + `PlotRandomHeroData` (0x4F31DC) — เหตุการณ์ hero สุ่มปลายงาน | **เหมือนจบปกติ** ตราบใดที่ `totalWorkingDay > 0` (ยกเลิกหลังทำไป ≥1 วัน ⇒ เข้าเงื่อนไข) | ทรัพยากรถูกจ่าย**รายวัน**โดย `WorkInProductionBuilding` แล้ว (`ChangeResource` 0x68B45B/0x68BB8E + `AddResourceNum` 0x9CA580 ×2) — **ไม่พบ reversal/refund ในทั้งสอง body** | OBSERVED (calls) / INFERRED (ความหมาย behavior) |
| `PlotController.WorkInProductionBuilding` 0x68ADA0 (per-day `workingFuc` — ยืนยันโดย RUNTIME area-notes P1-5 `workingFuc="WorkInProductionBuilding"`) | SendMessage รายวันจาก Update (0x9CBB9C/0x9CBBD8) | n/a (per-day callback) | `GetResourceProduceNum` (0x50CEA0) → `AddResourceNum` ×2 (0x68B376, 0x68BB61) + `HeroData.ChangeResource` ×2 (0x68B45B, 0x68BB8E) + `ChangeLivingSkillExp` (0x68B188) + `HaveForceFunction` bonus + `GenerateMaterial`/`GenerateTreasure` (0x68B86D/0x68B8A6 — ของหายากสุ่มได้) + `ShowPlotItem`/`AddPlot` | n/a | เครดิตรายวัน**ถาวรทันทีที่ยิง** — ยกเลิกหลังวันใดวันหนึ่ง = เก็บผลของวันนั้นไว้ | OBSERVED |
| Wait — ไม่มี handler | `callFuc=null` (0xC14E3C `xor r9d,r9d`) | — | ผล Wait มาจาก **day-tick housekeeping ล้วน** (ทรัพยากรสำนักขยับโดยไม่มี workingFuc — RUNTIME area-notes P1-7) | n/a | — | OBSERVED |
| กลุ่ม per-day handler ที่เรียก `StopWorking` เอง: `HospitalCureInjury` (0x53A96D, 0x53ABD8), `ForceTeachNPCOneDay` (0x4FC67E), `PlayerContinuePrisonOneDay` (0x557777), `RestInHotel` (0x57A05C) | workingFuc รายวัน (INFERRED จากชื่อ/บริบท) — caller เหล่านี้ยืนยันจาก xref (`xref-summary.txt:124-129`) | ยังไม่ถอด | เรียก `StopWorking` (จบงานก่อนกำหนดด้วยเงื่อนไขของ handler เอง) | — | body ยังไม่ถอด — ผล resource/refund = UNKNOWN | OBSERVED (xref) / UNKNOWN (body) |

### 4.2 สรุปนิยามที่ถูกต้องหลังรอบนี้ (แทน T5 §4.3 ที่ใช้กติกาเก่า)

- **finishFuc ถูกยิงเสมอ** ทั้งจบปกติ (Update 0x9CBD5D/0x9CBD99) และยกเลิก (`StopWorking` 0x9CB2C2/0x9CB2FA) —
  ต่างกันที่ค่า `workFinished` ที่ handler อ่านเอง (2 ใน 4 gate; 1 ไม่ gate; 1 gate ด้วย `totalWorkingDay`)
- **"กำลังทำงาน" = `working == 1`** (0x48) — ทั้ง finish branch (0x9CBDA2) และ StopWorking (0x9CB303) เคลียร์ working;
  `workFinished` เป็น latch ผลล่าสุด (stale ข้ามงาน — C9) **ห้าม**ใช้ `working && !workFinished` เป็นเงื่อนไข
  "กำลังทำงาน" — ผลข้างเคียง: งานที่ 2 หลังจบงานที่ 1 แบบปกติ จะมี `workFinished=1` ค้างตลอดงาน
- **หน้าต่างยกเลิกจริง**: `TryCancelWorking` (0x9CB3A9/0x9CB3B3) guard = `leftWorkingDay>0 && noCancel==0`
  (**ไม่มี**เช็ค `working` และ**ไม่มี**เช็ค `workFinished` ใน guard) ⇒ ยกเลิกได้เฉพาะช่วงนับวันที่เหลือวันอยู่ —
  **ระหว่าง pause (รวม final 2×pause) `leftWorkingDay==0` ⇒ TryCancel เงียบ** (แต่ `MiddleStopWork` ยังบังคับได้ —
  0x54EEA0 ไม่เช็คอะไรเลยนอกจาก Instance null แล้ว tail-jmp `StopWorking(false)` ที่ 0x54EF55) [OBSERVED]
- **"ไม่พบ refund ใน handler ที่อ่าน" ≠ "ระบบไม่มี refund"** — ครอบครบแค่ 4 handler + สายเริ่มงานฝึก; handler อีก
  40+ ตระกูลและเส้น refund ระดับ plot/scene ยังไม่ถอด [UNKNOWN]

---

## 5. `PlayerQuickTravel` และเส้นทางเริ่มกิจกรรมซ้อน

### 5.1 QuickTravel = กิจกรรมปั๊มเต็มรูปแบบ [OBSERVED — targeted disasm 0x7D9750..0x7D9AE0]

```
GameController.PlayerQuickTravel (0x7D9750, ขนาด 0x390):
  Int32.Parse(...) → QuickTravelUIController.HideQuickTravelUI (0x7D980C)
  → BuildingUIController.HideBuildingUI (0x7D9893) → AreaController.PlayerLeaveArea (0x7D991A)
  → WorldData.Player / WorldData.GetArea → BigMapPos.QuickTravelTime(playerPos, areaPos) (0x7D998D) = dayNum
  → HeroData.ChangeMoney (0x7D99A7 — จ่ายเงินก่อนเริ่มงาน)
  → StartWorking(6p): workName="乘车", dayNum=QuickTravelTime(...), callFuc="" (สตริงว่าง — ไม่มี per-day callback),
     finishCallFuc="PlayerQuickTravelToArea" (teleport ไอคอนเมื่อจบ), finishCallFucParam=r14 (UNKNOWN)
```

- **การเดินทางไวกินวันจริงผ่านปั๊มเดียวกับงาน** — จำนวนวันมาจากระยะทาง (`QuickTravelTime`) ไม่ใช่ slider
- **ไม่มี guard เช็ค `working` เลยทั้ง method** (filtered disasm: 0 reads of `[reg+0x48]`; เช็คเดียวคือ
  WorkingUIController.Instance null ที่ 0x7D9A73)
- เชื่อมกับ area: `PlayerLeaveArea` ก่อนเริ่ม + `finishCallFuc="PlayerQuickTravelToArea"` ตอนจบ ⇒ **area membership
  เปลี่ยนระหว่าง "งานเดินทาง"** — จุดเช็ค `atAreaID` ของ validator MP ต้องคำนึงว่าระหว่าง QuickTravel ผู้เล่นอยู่
  นอก area (atAreaID −1) [INFERRED จาก PlayerLeaveArea + finish-teleport]

### 5.2 เส้นทางเริ่มงานซ้อน — สิ่งที่ static พิสูจน์ได้

คำถามโจทย์ §4 → คำตอบ:

| คำถาม | คำตอบ | หลักฐาน |
|---|---|---|
| Quick Travel ใช้ singleton pump เดียวกับกิจกรรม? | **ใช่** — เรียก `WorkingUIController.StartWorking(6p)` ตรง | 0x7D9AAB [OBSERVED] |
| มีเงื่อนไขกัน `StartWorking` เมื่อ `working=true`? | **ไม่มี** ในทุกเส้นทางที่ถอด: 6p/7p bodies (ไม่มี read 0x48), 3p, `List<string>`, `PlotController.StartWorking(string)` (0x6603F0), `PlayerQuickTravel`, `WaitUIController.SureButtonClicked` (เช็คแค่ Instance null) | OBSERVED |
| เส้นทางใด reset `totalWorkingDay/leftWorkingDay/nextDayTime/leftPauseTime`? | **เฉพาะ body ของ StartWorking เอง** (เขียนทับ unconditional ทุกครั้ง): leftWorkingDay=dayNum, totalWorkingDay=0, nextDayTime=0, leftPauseTime=**pauseTime (initial)**, resourceNum=0 | 0x9CAC70–0x9CACAA / 0x9CB110–0x9CB14A [OBSERVED] |
| เริ่มงานใหม่แล้ว `skipping` ถูกเคลียร์? | **ไม่** — ไม่มี store ไป 0x62 ใน start path ทั้งหมด; writers = SkipButtonClicked (toggle 0x9CAA9F) + SetSkippingState (0x9CA928, 0 direct caller) ⇒ ธงค้างข้ามงาน | OBSERVED (C10) |
| `workFinished` ถูก reset? | **ไม่** — นี่คือข้อค้นพบใหม่ (C9): latch ค้างข้ามงาน | OBSERVED |
| state ถูกทับจริง หรือแค่ inference? | **ทับจริงเชิงโค้ด** — body 6p เขียน field ชุดเดิม unconditional (ไม่มี branch เช็คสถานะเดิม) · สิ่งที่**ยังไม่เคยวัด** = พฤติกรรมบนจอ/ผลข้างเคียง UI ตอนทับ (คงเป็น NOT-RUN เดิมของ T5 R5/V4) | OBSERVED (โค้ด) / NOT-RUN (พฤติกรรมเกมจริง) |

**ผลข้างเคียงของการทับที่ static ชี้ชัด** (สำคัญกับ MP): เริ่มงานใหม่ทับงานเดิม ⇒ (1) `finishCallFuc` เดิม**หาย**
(เขียนทับด้วยชุดใหม่) ⇒ งานเดิมจะ**ไม่มีทางยิง finishFuc** — งานเดิมสูญเงียบ (ค่าใช้จ่ายที่จ่ายไปแล้ว เช่นเงินค่าฝึก/
เงิน QuickTravel ไม่ถูกคืนโดยกลไกใดที่ถอด); (2) `workFinished` เดิมค้างอยู่ (อาจเป็น true จากงานก่อนหน้า);
(3) `skipping` เดิมค้าง; (4) `leftPauseTime` กลับเป็น initial pause ⇒ วันแรกของงานใหม่เริ่มหลังพัก 1 pause เสมอ

---

## 6. T(N) — เปรียบเทียบสามโมเดล (reconcile จาก control flow เต็ม)

### 6.1 ลำดับจริงต่อหนึ่งวันจาก `Update` (ทุกข้อ OBSERVED — RVA อ้างจาก `bodies/WorkingUIController_Update.txt`)

```
เฟรมปกติ (working=1, ผ่าน freeze 3 ทาง 0x9CB864/0x9CB8FF/0x9CB943):
  A) ถ้า leftPauseTime > 0  (0x9CBA3F ja 0x9CBDBE) — PAUSE phase:
       leftPauseTime -= RealTime.deltaTime            (0x9CBDC0–0x9CBDC5; **ไม่มีตัวคูณ skipping**)
       ถ้ายัง > 0 → return                             (0x9CBD2 jb)
       [เก็บค่า <=0 ที่ 0x9CBD CD]
       ถ้า leftWorkingDay <= 0 → return เปล่า          (0x9CBDD8 jle 0x9CBCA4)   ← พักวันสุดท้ายจบ = ยังไม่จบงาน
       ไม่เช่นนั้น → AddAnimation + PlaySound(0.4)      (0x9CBE62–0x9CBEB6)        ← แอนิเมชันเฉพาะ "พักระหว่างวัน"
  B) ไม่เช่นนั้น (leftPauseTime <= 0) — COUNT phase:
       ถ้า leftWorkingDay <= 0 → FINISH                (0x9CBA49 jle 0x9CBCC7)
           workFinished=1 (0x9CBCF6) → finishFuc (SendMessage 0x9CBD5D/0x9CBD99) → working=0 (0x9CBDA2) → SetActive(false)
       nextDayTime += RealTime.deltaTime               (0x9CBA56–0x9CBA64; unscaled)
       threshold = dayTime × (skipping ? (TestMode==1 ? 0.2 : 0.5) : 1.0)   (0x9CBA92–0x9CBB12)
       ถ้า nextDayTime < threshold → return            (0x9CBB1A jb)
       ถ้า callFuc != null → SendMessage(workingFuc[,param])                 (0x9CBB9C/0x9CBBD8)  ← ก่อนเดินวัน
       ถ้า working ถูก callback ปิดไปแล้ว → return โดยไม่นับวัน             (0x9CBBE0 je)          ← re-check หลัง callback
       leftWorkingDay--; totalWorkingDay++; nextDayTime=0                   (0x9CBBEA–0x9CBBF2)
       ChangeHour(24.0)                                 (0x9CBC30)           ← เดินวันทางปฏิทิน
       leftPauseTime = (leftWorkingDay>0 ? pauseTime : 2×pauseTime)         (0x9CBC3C–0x9CBC9F)
```

### 6.2 โครงเวลาที่ตามมา

```
StartWorking: [pause p]  (initial — ตั้งที่ 0x9CACAA)
  day1(dt) → [p + anim] → day2(dt) → [p + anim] → … → dayN(dt) → [2p, ไม่มี anim] → (เฟรมถัดไป) FINISH
```

| โมเดล | สูตร | สมมติฐานที่ต่างกัน | สถานะหลังรอบนี้ |
|---|---|---|---|
| M-RUNTIME | `N×dt + N×p` | ไม่มี initial pause; pause วันสุดท้าย = p | ตรงกับ n=1 (10 วัน ≈ 12.5 วิ) แต่**โครงไม่ตรง disasm** |
| **M-DISASM(v2 — โครงจริง)** | **`N×dt×k + (N+2)×p`** | initial pause 1× + inter-day (N−1)× + final 2× **ถูกนับก่อน finish**; pause ไม่ย่อด้วย skip; anim ระหว่างวันเพิ่มเวลาไม่คำนวณ | **โครง OBSERVED ครบ** (C8) — ที่ N=10, dt=1, p=0.25 ให้ **13.0 วิ** |
| M-PROMPT | `N×dt + (N+2)×p` | เดียวกับ M-DISASM(v2) | **โครงได้รับการยืนยันจาก disasm แล้ว** — แก้คำของ T5 ที่ว่า "ไม่พบหลักฐานรองรับ" |
| (เดิม) M-DISASM T5 | `N×dt + (N+1)×p` | ไม่นับ initial pause | **RETRACTED** |

**จุดที่ static ยังตัดสินไม่ได้ (UNKNOWN → V2):** (1) ตัวเลข RUNTIME n=1 (≈12.5 วิ) กับโครง (13.0 วิ) ต่างกัน 2p=0.5 วิ —
อธิบายได้ด้วยความหยาบของการวัดเดิม (≈, hook cadence 0.75–2.9 วิ) แต่พิสูจน์ไม่ได้ ห้ามเลือกข้าง; (2) เวลาแอนิเมชัน
inter-day (AddAnimation "TimeAnim" + PlaySound) ไม่รวมใน dt/p — ขนาดจริง UNKNOWN; (3) พฤติกรรม skip ที่ rate ใหม่
(โครง: ย่อเฉพาะ dt, **pause เต็มจำนวน (N+2)p ไม่ย่อ** — ที่ dt=1, k=0.5, N=10 ⇒ 5+3 = 8 วิขั้นต่ำ); (4) freeze 3 ทาง +
แอนิเมชันการ์ตูนของแต่ละงาน — รวมเป็น noise ที่ต้องวัดจริง

**ข้อสรุป T(N) ที่รายงานได้:** โครงสร้าง (ลำดับ/จำนวน pause/ตำแหน่ง finish) = OBSERVED; **elapsed จริง = UNKNOWN**
จนกว่า V2 จะวัดที่ rate ใหม่ (ตรงตามคำสั่งโจทย์ §5 — เก็บ UNKNOWN และชี้ V2)

---

## 7. รายการที่ปิดได้จาก static analysis (completion gate ข้อ 1)

1. **noCancel ระดับ overload**: 6p=0 ตายตัว; 5p/2p/List/3p delegate→6p ⇒ 0; 7p=arg — ครอบ call site ทุกจุดที่
   E8/E9 scan เห็น (51 จุด) [OBSERVED]
2. **noCancel=true ที่พิสูจน์แล้ว**: จุดเดียว — `TryThinkAboutMissingSkill` → งาน "参悟" (dayNum=1) [OBSERVED]
3. **cancellation mechanics**: guard 2 เงื่อนไข (`leftWorkingDay>0 && noCancel==0`, ไม่เช็ค working/workFinished);
   `MiddleStopWork` = forced path ไม่เช็คซ้ำ; `StopWorking(finished)` ยิง finishFuc เสมอ [OBSERVED]
4. **finish-handler gates ราย handler**: gate-workFinished ×2 (`RealStartStudySkill` สองชั้น, `StudyMaxFightSkillFinish`),
   ไม่ gate ×1 (`StudyFightSelfFinish`), gate-totalWorkingDay ×1 (`FinishWorkInProductionBuilding` — ใหม่) [OBSERVED]
5. **`workFinished` = latch ผลล่าสุด** — StartWorking ไม่ reset; กติกา "งานกำลังเดิน = `working && !workFinished`"
   ถูกแทนด้วย "`working == 1`" [OBSERVED → CORRECTED — แก้เอกสารรอบหน้า]
6. **`skipping` ค้างข้ามงาน** — ไม่มี reset ใน start/stop/finish; writers = toggle ปุ่ม + `SetSkippingState` (0 direct
   caller) [OBSERVED ภายใต้ direct-call]
7. **เส้นเริ่มงานไม่มี working-guard** — ทับ state ได้เสมอ; งานเดิมสูญเงียบ (finishCallFuc ถูกเขียนทับ ไม่มีทางยิง)
   [OBSERVED เชิงโค้ด]
8. **QuickTravel = กิจกรรมปั๊ม** — dayNum จากระยะ (`QuickTravelTime`), จ่ายเงินก่อน (`ChangeMoney` 0x7D99A7),
   finish = `PlayerQuickTravelToArea`, `PlayerLeaveArea` ก่อนเริ่ม [OBSERVED]
9. **รูปแบบข้อมูลทางเข้า**: `List<string>` = `[workName, dayNum, callFuc, callFucParam, finishFuc, finishFucParam]`
   (pad 6); plot-string = split `;` 6 ช่อง — แก้รูปแบบ 5 ช่องเดิมของ area-notes P1-5 [OBSERVED]
10. **Wait**: workName=`"等待"`, callFuc=null (ไม่มี per-day callback), `dayNum = slider × (TestMode==1 ? 100 : 1)`
    — **TestMode ยังทำหน้าที่คูณจำนวนวัน Wait ×100** (ข้อเท็จจริงใหม่ — อีกเหตุผลห้ามเขียน pref นี้) [OBSERVED]
11. **T(N) โครง** = `N×dt×k + (N+2)×p` + ลำดับ pause/finish ครบ (Section 6) [OBSERVED โครง]
12. **ตำแหน่ง workingFuc เทียบ day-tick**: workingFuc ยิง**ก่อน** `ChangeHour(24)` และ re-check `working` หลัง
    callback ก่อนนับวัน (callback หยุดงานเองได้โดยวันนั้นไม่ถูกนับ) [OBSERVED — ยืนยันโครงเดิมของ T5 §4.1]

---

## 8. รายการที่ยังต้อง Runtime Verification V1/V2 (completion gate ข้อ 2–3)

**ที่ static พิสูจน์ไม่ได้และยังมีผลต่อการตัดสิน:**

| # | ช่องว่าง | ทำไม static ปิดไม่ได้ | ชุดทดสอบ |
|---|---|---|---|
| G1 | ผลเงิน/ทรัพยากร/buff จริงตอนยกเลิก (refund มี/ไม่มี รายกลุ่มงาน) | refund ถ้ามีอยู่ใน handler/plot ที่ยังไม่ถอด (40+ ตระกูล) หรือผูกกับ scene; "ไม่พบใน 4 handler" ไม่ใช่หลักฐานเชิงลบ | **V1** |
| G2 | พฤติกรรมจอของ `FinishWorkInProductionBuilding` เมื่อถูกยกเลิก (dialog สรุปผลโผล่หลัง cancel? เหตุการณ์ hero สุ่มยังเกิด?) | body อ่านได้แต่ผลบนจอ/ต่อ state จริงต้องวัด | **V1** |
| G3 | `workFinished` stale บนจองานที่ 2 (ยืนยัน latch semantics ด้วยการเล่นจริง) | โค้ดชี้ชัดแต่ขัดกับกติกาเดิมที่เคย "วัด" บนเซฟงานแรก — ควรยืนยันซ้ำบนงานที่สอง | **V1** (เพิ่ม observable) |
| G4 | ตัวเลข T(N) ที่ `dayTime` 8–12 วิ + ยืนยัน initial pause/final 2×pause บนจอ + เวลาแอนิเมชัน inter-day | โครง OBSERVED แต่ elapsed + animation + noise = ต้องวัด | **V2** |
| G5 | skip ที่ rate ใหม่ (โครงทำนาย pause ไม่ย่อ — รวม (N+2)p เสมอ) | n=1 เดิมวัดที่ dt=1 เท่านั้น | **V2** |
| G6 | พฤติกรรมบนจอของ "เริ่มทับงานที่กำลังทำ" (งานเดิมหายเงียบ — ผล UI/ผู้เล่น) | โค้ดชี้ชัดแต่เป็น NOT-RUN เดิม (T5 R5/V4) | V4 (ไม่บังคับก่อน V1/V2) |

---

## 9. ข้อเสนอ: V1/V2 พร้อมเสนออนุมัติหรือยัง (completion gate ข้อ 4)

**คำตอบ: พร้อมเสนออนุมัติเป็นงานแยกแล้ว** — preconditions/observables/stop conditions/rollback ครบตามเกณฑ์ โดยรอบนี้
ทำให้ observables แม่นขึ้นมาก (รู้ว่าต้องดูอะไรจาก static แล้ว) รายละเอียด:

**V1 — cancellation semantics (บนสำเนาเซฟ, hook log-only ที่ `StopWorking` 0x9CB220 + `ChangeMoney`/`ChangeResource`
read-only)**
- *preconditions:* save-split ตาม §1.2 + `save-split preflight` = READY ก่อนเปิดเกม · เกม hash ตรง VERSION.md ·
  ไม่เขียน pref ใด · ผู้ใช้กดเองหรืออนุมัติ invoke
- *inputs:* (ก) ฝึก 2 วัน → ยกเลิกผ่าน**ปุ่ม UI** (TryCancelWorking path) (ข) งานผลิต 10 วัน → ยกเลิกหลังวันที่ 3
  (ค) ถ้าอนุมัติ invoke: `PlotController.MiddleStopWork()` (0x54EEA0) ระหว่างงาน noCancel=0 (ง) ทำงานจบปกติ 1 ครั้ง
  แล้ว**เริ่มงานที่สอง** แล้วอ่าน `workFinished` (ทดสอบ latch)
- *observables:* `working/leftWorkingDay/totalWorkingDay/workFinished/skipping` ก่อน-หลัง; เงิน
  (`ItemListData.money`) และ resource สำนัก (`ForceData.resourceStore`) ก่อน-หลัง; finishFuc ยิงจริงไหม
  (hook log ที่ 0x9CB2C2/0x9CB2FA); dialog ที่โผล่ (production finish หลัง cancel — G2); ปฏิทินเดินกี่วัน
- *expected (จาก static รอบนี้):* finishFuc ยิงเสมอ; ทรัพยากรรายวันคงอยู่; ฝึกสาย StartStudySkill = ไม่ได้วิชา;
  งานที่สองเห็น `workFinished` stale; ปฏิทินเดินเท่าวันที่ผ่านจริง
- *stop condition:* ผลขัดกับตาราง Section 4 → **หยุด รายงานก่อนขยาย** (ห้ามอนุมานต่อ); มี write ถาวรที่ผู้ใช้ไม่รับ →
  หยุดและตั้งประชุมกติกา reward
- *rollback:* restore save + verify digest 57 ไฟล์ + คืน pref จาก `prefs.before.cfg` (มาตรฐาน §1.2)

**V2 — T(N) ที่ rate ใหม่ (บนสำเนาเซฟ)**
- *preconditions:* เหมือน V1 + **อนุมัติเขียน static `dayTime`** (typed interop — write ครั้งแรกของ mod ต่อ field
  เกม; rollback = คืนค่า 1.0/0.25) + hook log-only ที่ `ChangeDay` leaf 0x790E30 นับจำนวนวัน
- *inputs:* งาน N∈{1,5,10,15} ที่ dt∈{8,9,10,12} + รอบกด `>>` หนึ่งรอบ + Wait 30 (ทดสอบขอบ slider)
- *observables:* เวลารวมจริง (wall-clock), จำนวน leaf hits (=N), ช่วงพักวันสุดท้าย (คาด 2p), ช่วงพักแรก (คาด p),
  `hour` คงที่, skip: วัดว่า pause ไม่ย่อจริงไหม
- *expected:* ตรง `N×dt×k + (N+2)×p ± เวลาแอนิเมชัน` — ถ้าตรงโครง ⇒ ตัดสิน M-RUNTIME-vs-M-PROMPT ปิด; ถ้าไม่ตรง
  ⇒ บันทึกผลจริงและหยุด (ห้ามปรับโมเดลเอามั่ว)
- *stop/rollback:* เหมือน V1 + คืน `dayTime/pauseTime` เป็น 1.0/0.25 ทุกครั้งก่อนปิด session

**เงื่อนไขที่ยังค้างก่อน "อนุมัติรัน":** (ก) มติผู้ใช้ว่าอนุญาต invoke `MiddleStopWork` หรือใช้เฉพาะปุ่ม UI (ถ้าเฉพาะ
ปุ่ม UI จะไม่ครอบเคส forced-cancel — ต้องบันทึกข้อจำกัด); (ข) baseline เซฟ (ค้างอนุมัติเดิมของ repo — divergence
5 ไฟล์ SaveSlot8); (ค) ผู้ใช้รับ "ผลถาวรของวันที่ผ่านคืนไม่ได้" เป็นหลักการก่อนวัด

---

## 10. purity ของรอบนี้

- **สิ่งที่อ่าน:** bodies 30 ไฟล์เดิม (read-only), `Il2CppDumper/dump.cs` (grep + sed อ่านอย่างเดียว), `GameAssembly.dll`
  (read bytes ผ่าน pefile — hash ไม่เปลี่ยน), script.json, xref-summary.txt, slots ผ่าน script.json
- **สิ่งที่รัน:** python inline (heredoc) เรียก `time_static_common.py` — capstone disasm เฉพาะ 7 หน้าต่างเล็ก
  (TryThinkAboutMissingSkill, PlayerQuickTravel, WorkInProductionBuilding, FinishWorkInProductionBuilding,
  SureButtonClicked, overload 0x9CB000 + 0x9CAE90, SureBuildingWork) + xref เดี่ยว 1 target (SetSkippingState) +
  slot resolve — **ไม่สแกนทั้งโมดูล, ไม่เขียนไฟล์, ไม่แตะ process เกม**
- **สิ่งที่เขียนใหม่:** ไฟล์เดียวในโฟลเดอร์นี้ · **ไม่แก้** `time-static-notes.md`/`time-audit-notes.md`/T5 report/
  area-notes (ข้อที่ควรแก้ระบุไว้ในตาราง Section 2 — คอลัมน์ "ผล" — รอมติผู้ใช้แล้วจึงแก้ไฟล์ต้นทางใน session หน้า)
- **git:** ไม่มี commit/tag/stash/reset; HEAD ทั้งสอง repo ตรวจปิดงานแล้วเหมือนเริ่ม (แนบในข้อความส่งมอบ)
