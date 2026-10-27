# activity-multiplayer-audit — Phase T5: Activity Callback & Multiplayer Ownership Audit

> **label:** `TIME-T5-20261028-003723` · **งานเอกสาร read-only 100%** — ไม่เปิดเกม · ไม่ attach/invoke · ไม่ build/deploy ·
> ไม่แตะเซฟ/pref · ไม่แก้ source/protocol · ไม่ commit/tag/reset · ไฟล์ใหม่ของรอบนี้ = ไฟล์เดียวในโฟลเดอร์นี้เท่านั้น
> **anchor:** mod repo HEAD `df46de1` (ตรวจ 2026-10-28 00:37, ไม่มีการเปลี่ยนแปลง git ทั้งรอบ) · game repo HEAD `c672dc2`
> · `GameAssembly.dll` = `33c56def…7920c1`, `global-metadata.dat` = `451311d4…18bfd0` ตรง `VERSION.md`
> (จาก session `TIMEstatic-20261027-051647` เมื่อ 2026-10-27 05:1x — รอบนี้ไม่ได้ rehash ใหม่เพราะไม่มีเหตุการณ์เปลี่ยนไฟล์เกม
> ระหว่างสอง session และห้ามรันเกม/เขียนไฟล์)
> **ป้าย:** **OBSERVED** = พบจาก source/disasm/ไฟล์ที่ระบุได้ (รวม disasm ที่เก็บไว้ใน `bodies/`) · **RUNTIME** = วัดจาก process จริง (session ก่อน ๆ, อ้างตามไฟล์หลักฐาน) ·
> **DUMP** = จาก `Il2CppDumper/dump.cs`/`script.json` · **INFERRED** = มีหลักฐานสนับสนุนแต่ยืนยัน behavior ไม่ครบ ·
> **UNKNOWN** = หลักฐานไม่พอ/มีคำอธิบายแข่งขัน · **NOT-RUN** = ต้องทดสอบ runtime ใน session แยก ·
> **UNVERIFIED-ON-RERUN** = claim รอบก่อนที่ตรวจซ้ำแล้วไม่ยืนยันได้
> เส้นทางไฟล์ทั้งหมดอ้างแบบ repo-relative จาก root ของ mod repo (`C:\Users\memph\LongYinCoop`) เว้นแต่ระบุเป็น absolute

---

## Section 1 — Executive summary

งานนี้ไม่วัดใหม่ ไม่เปิดเกม — ใช้หลักฐานเดิมของ repo ให้เต็มที่ (`time-notes.md`, `time-followup-notes.md`,
`time-audit-notes.md`, `evidence/TIMEstatic-20261027-051647/` รวม disasm 30 bodies, `area-notes.md`,
`evidence/S2cd-20261022-0710/`, `evidence/sp0sp7-20261024-225939/`, notes worldmap/battle/explorer/sp0-sp7)
แล้วตรวจโค้ด mod `src/LongYinCoop/` ตรงจุดเพื่อตอบช่องว่าง 4 กลุ่มของโจทย์

**สิ่งที่หลักฐานตอบได้แล้ว (สรุป — รายละเอียดใน Section ที่อ้าง):**

1. **บัญชี StartWorking call sites ครบตามขอบเขตสแกน** — 6p (`0x9CABF0`) = 42 จุด (38 external + 4 cross-overload),
   ทุก overload รวม **51 จุด (external 47, distinct owner 47)** จาก E8/E9 scan ทั้ง .text ที่ validate ทุกจุด
   (`evidence/TIMEstatic-20261027-051647/time-static-notes.md §2.9` + `xref-summary.txt:64-122`) — REPRODUCED
   แล้วแก้ความหมายเลข "42" ให้ชัด (Section 3)
2. **กลไกปั๊มถอดครบฝั่ง static แล้ว**: `WorkingUIController.Update` (0x9CB4B0) นับวันด้วย **unscaled delta**
   (`RealTime.get_deltaTime` → `Time.get_unscaledDeltaTime` — `timeScale=0` ไม่หยุดปั๊ม), เดินวันผ่าน
   **`ChangeHour(24.0)` → wrap → `ChangeDay()` leaf** (ไม่ได้เรียก leaf ตรง), freeze 3 ทาง (plot/speShow/HUD),
   `skipping` ลด threshold วันเป็น 0.2×/0.5× ตาม pref `"TestMode"` (อ่านอย่างเดียว — ห้ามเขียน) — ทั้งหมด
   [OBSERVED disasm, Section 4/6]
3. **เส้นยกเลิกถอดครบ**: `TryCancelWorking` guard 2 เงื่อนไข (`leftWorkingDay>0 && noCancel==0`) → plot-choice
   `"中止;MiddleStopWork"` (string literal dispatch) → `MiddleStopWork` **ไม่เช็ค noCancel ซ้ำ** → tail-jmp
   `StopWorking(false)` → `workFinished=false` + **ยิง `finishFuc` เสมอ** [OBSERVED] — handler รายงานอ่าน
   `workFinished` เอง: `RealStartStudySkill` และ `StudyMaxFightSkillFinish` **มี gate** (ยกเลิก ⇒ ไม่ได้รางวัล),
   `StudyFightSelfFinish` **ไม่มี gate** (สถานะค้างถูกใช้ต่อ) [OBSERVED, Section 4.3]
4. **singleton = คอขวดของกิจกรรมหลายผู้เล่นแบบขนาน** — state งานทั้งชุดเป็น field ของ object เดียวต่อโปรเซส +
   `dayTime/pauseTime` เป็น static ⇒ native pump มีกิจกรรมได้ **1 งานเท่านั้น** และ static rate เป็นของทั้งโปรเซส
   [INFERRED จาก field layout + RUNTIME หนุน (เริ่มงานใหม่ reset ทับ), Section 5.2]
5. **โปรโตคอล mod ปัจจุบันไม่มี activity lifecycle เลย** — guest→host allowlist = `Auth/Ping/Resync/Bye` เท่านั้น
   (`S2Types.cs:58-70`), `commandsAccepted` ออกแบบให้เป็น 0, snapshot ครอบเฉพาะศึก (`S2Sampler.Read` คืน null
   นอกศึก), ไม่มีข้อความ activity/pause/area ใน `src/` (grep ไม่พบ — มีแต่แบบร่างใน `area-notes.md` ท้ายไฟล์)
   [OBSERVED, Section 5.3]
6. **ตอบคำถาม "กิจกรรม 5 วัน 60 วินาที"**: ภายใต้ native pump ความหมาย (1) "real-time 60 วิ + ปฏิทินเดิน 5 วัน"
   และ (3) "เดินตามปฏิทินแต่ปรับอัตรา dayTime" **เป็นของเดียวกัน** — ปั๊มคือตัวเดินปฏิทิน การตั้ง `dayTime` ต่อ
   StartWorking จึงได้ทั้งสองพร้อมกัน; ความหมาย (2) "real-time แยกจากปฏิทิน" ต้องเขียน timer ใหม่และเรียก
   `ChangeHour(24)`/`ChangeDay()` เอง — primitive มีจริงแต่ต้องรักษาลำดับ callback เอง [INFERRED, Section 6.1]
7. **primitive ของศึกมีจริงทั้งฝั่งยกเลิกและเริ่มศึก** — `MiddleStopWork` (forced path, ไม่เช็ค noCancel) และ
   `PrepareBattleMap` ×11 overload + `fightEndCall` string callback — แต่ **ไม่มี wiring ใดเชื่อม "เริ่มศึก ⇒ ยกเลิก
   กิจกรรม" ทั้งในเกมและ mod** และ NPC-vs-NPC resolve ด้วย data path ไม่เปิดฉาก (`PrepareBattleMap` 0 call, day 25+
   [RUNTIME sp7]) [OBSERVED/UNKNOWN, Section 5/7]

**สิ่งที่ยังขาดก่อนเขียนโค้ดได้ (5 ข้อใหญ่):** (α) พฤติกรรม `TryCancelWorking`/`StopWorking(false)` ต่อ
resource/เงิน/รางวัลราย handler — NOT-RUN; (β) ตัวเลข T(N) จริงที่ `dayTime` ใหม่ (8–12 วิ/วัน) + ลำดับ
`2×pauseTime` วันสุดท้าย — โมเดล 3 แบบแข่งกันไม่ตัดสินได้ (Section 6.4); (γ) ผู้ตั้ง `needAutoSave`/พฤติกรรม
autosave ระหว่างงานค้าง; (δ) เกมไม่มี "เริ่มศึกทับ modal งานโดยไม่ยกเลิก" วัดเลย; (ε) design decision ของผู้ใช้:
โมเดล serialize กิจกรรม, pause-policy ของคำสั่ง, ระยะเวลาของ N นอก {1,5,10,15} (Section 11)

**ข้อความของโจทย์ที่ถูกปรับ/หักล้างโดยหลักฐานจริง** (รายงานตามข้อ §3 ของโจทย์ — รายละเอียด Section 2.3):
"ปั๊มงานเรียก ChangeHour(24)" ยืนยันแต่เส้นทางคือ `Update → ChangeHour(24) → wrap → ChangeDay()` ไม่ใช่เรียก
`ChangeDay()` ตรง; "`>>` ลด threshold ของ activity timer" ถูกต้องแต่คูณลงบน **day threshold** (0.2×/0.5×)
ไม่ใช่แค่ animation; "Skip มีผลต่อ animation" จริงควบคู่ (×5/×2) — ทั้งหมด [OBSERVED]

---

## Section 2 — Evidence and coverage

### 2.1 ไฟล์หลักฐานที่อ่าน (มีอยู่จริง ตรวจ path แล้ว 2026-10-28)

| ไฟล์ | สถานะ | สรุปบทบาท |
|---|---|---|
| `time-notes.md` | tracked | ระบบเวลาภาพใหญ่ — authority, write paths, consumers, chains (ส่วนที่ถูกหักล้างภายหลังระบุใน `time-static-notes.md §3` แล้ว) |
| `time-followup-notes.md` | tracked | pause 6 ชั้น (P1–P6), activity timing inventory, mapping table, การยกเลิกฝึก |
| `time-audit-notes.md` | tracked (สร้าง 2026-10-27 23:15) | feasibility audit — concurrency matrix, 3 ทางเลือก timing, protocol gap inventory |
| `evidence/TIMEstatic-20261027-051647/time-static-notes.md` | tracked | **หลักฐานหลักของ Section 3–4** — static disasm ครบชุด (hash เกมตรง VERSION.md ก่อนงาน) |
| `evidence/TIMEstatic-20261027-051647/xref-summary.txt` | untracked (บนดิสก์) | รายชื่อ caller รายจุดของเป้าหมายเวลาทั้งหมด (StartWorking/StopWorking/ChangeHour/ChangeDay/RealTime) |
| `evidence/TIMEstatic-20261027-051647/bodies/*.txt` (30 ไฟล์) | untracked (บนดิสก์) | disassembly ตัวเต็ม — Update, StartWorking 6p/7p, StopWorking, TryCancelWorking, ChangeDay leaf/int, ChangeHour, MiddleStopWork, RealStartStudySkill ฯลฯ |
| `evidence/TIMEstatic-20261027-051647/hour-writes.tsv`, `floats-observed.tsv`, `xref-scan.tsv`, `slots-resolved.tsv`, `scan-run.log` | untracked (บนดิสก์) | ผลสแกนดิบ (negative scan ของ writer `hour`, float pool, ทุก candidate E8/E9/FF15) |
| `area-notes.md` | tracked | ระบบ Area/สำนัก + วัดงาน 10 วัน/ฝึก/Wait จริง (P1-4..P1-7) + แบบร่าง protocol AREA_*/TIME_ACTION_* (ท้ายไฟล์) |
| `worldmap-notes.md`, `battle-notes.md`, `explorer-notes.md`, `sp0-sp7-notes.md`, `sp5-prep-notes.md` | tracked (บางไฟล์ถูกลบใน game repo แต่ mod repo มี) | BigMap/ศึก/ดันเจี้ยน/census/เซฟ |
| `evidence/S2cd-20261022-0710/` | tracked | hook วัด ChangeDay 2 รอบ + disasm `ChangeDay(int)` + ui-crosscheck |
| `evidence/sp0sp7-20261024-225939/` | untracked ส่วนใหญ่ (บนดิสก์) | census singletons, NPC battle partial, field snapshots HeroData/WorldData |
| `src/LongYinCoop/**` | tracked | โค้ด mod — อ่านตรง: `S2/Protocol/S2Types.cs`, `S2/Host/S2ControlHandler.cs`, `S2/Host/S2Sampler.cs`, `S1/WorldSampler.cs`, `S1.5/AttackCommand.cs`, `S1.5/MoveCommand.cs` (grep), `S1.5/TurnGate.cs`, `IpcServer.cs` (grep), `S2/S2HostService.cs` (grep), `CoopMod.cs` (grep), `CoopPrefs.cs` (grep), `S2/S2NetConfig.cs` (grep) |
| `Il2CppDumper/dump.cs`, `script.json` | (เกม) อ่านอย่างเดียว | ลายเซ็น/field offset — อ้างผ่านโน้ตที่จดเลขบรรทัดไว้แล้ว |

### 2.2 วิธี static + ขอบเขตที่สแกนครอบ (สืบทอดจาก `TIMEstatic-20261027-051647`)

- สแกน **E8 (call rel32) + E9 (jmp rel32/tail-call) ทั้ง section execute** ของ `GameAssembly.dll` โดย validate
  ทุก candidate ด้วยการ decode 5 ไบต์จริง; **FF15 พบ 0 จุด** ที่ชี้เป้าหมายระบบเวลา (`xref-ff15.tsv` = header เปล่า)
  ⇒ ทุก caller ที่รายงานเป็น direct call ล้วน [OBSERVED — `time-static-notes.md §1`]
- script.json (ScriptMethod 96,106 รายการ, Address=RVA) resolve ชื่อ owner; slot .data resolve ผ่าน
  ScriptMetadataMethod/ScriptString; string literal dispatch (`SendMessage`, plot-choice `"ชื่อ;handler"`) ตรวจที่ฝั่งสตริง
- **สิ่งที่วิธีนี้ไม่ครอบ** (ตัวเลขทุกตัวเป็น **lower bound**): register-indirect call (`call rax`/vtable/delegate),
  binding ใน asset/scene (UnityEvent/serialized), reflection (`SendMessage` ปลายทางอยู่นอก call graph),
  generic sharing (ตัด NOISE `GetComponent<>` 0x967EF0 และ `.ctor` stub 0x3A1810 แล้ว — มิฉะนั้นนับผิดเป็นร้อย),
  owner attribution อาจคลาดเคลื่อนใน padding [OBSERVED — `time-static-notes.md §1`]
- ⇒ **ห้ามใช้คำว่า "ครบทุก caller"** — ทุกจำนวนในรายงานนี้หมายถึง "จุดที่พบภายใต้ช่องทางที่สแกน"

### 2.3 ข้อค้นพบรอบก่อนที่ตรวจซ้ำแล้ว (ตามที่โจทย์ข้อ §3 ให้เช็ค)

| โจทย์บอกว่า "known finding" | ผลตรวจกับไฟล์จริง |
|---|---|
| WorkingUIController singleton + field state ชุดเดียว | ยืนยัน — `dump.cs:392679`, field layout 0x48–0x68, static 0x0/0x4 [OBSERVED] |
| dayTime/pauseTime static | ยืนยัน — live 1.0/0.25 [RUNTIME area-notes P1-5] |
| Update ใช้ RealTime.get_deltaTime → unscaled | ยืนยันและ**ปิดข้อ UNKNOWN เดิม** — tail-jmp `Time.get_unscaledDeltaTime` ที่ 0xCFD4A2; Update เรียก ×2 (0x9CBA56, 0x9CBDC0) [OBSERVED `time-static-notes.md §2.3`] |
| ปั๊มงานเรียก ChangeHour(24) เดินวัน, ChangeHour เรียก ChangeDay เมื่อ wrap | ยืนยัน — 0x9CBC30 arg 24.0; wrap ที่ 0x7927D4→0x792818 [OBSERVED] — แก้คำเดิม "ปั๊มเรียก ChangeDay() ตรง" ของ `time-notes.md:39,119` |
| ChangeDay ทำ housekeeping หลายระบบ | ยืนยันเชิงรายการ — leaf 0x790E30 เรียก `ManageWorldEvent`/`ChangeMonth`/injury×3/`ManageTagTime`/`ManageAllAI`/`ResetAllAIMananged`/`ManageAreaStuff`(จาก audit-notes)/`GiveUpMission`/meeting/event-expiry/`ManageForceStuff`(Monitor.Enter)/research/meditation/spe-poison ฯลฯ [OBSERVED `time-static-notes.md §2.14` + `time-audit-notes.md §1.1 ข้อ 5`] — ลำดับเงื่อนไขเต็มยังไม่ถอด |
| Skip ลด threshold ตาม TestMode + ผลต่อ animation | ยืนยันและละเอียดขึ้น — threshold ×0.2 (TestMode=1) / ×0.5, anim ×5/×2, `SkipButtonClicked`/`SetSkippingState` toggle ธง 0x62 เท่านั้น (ไม่เรียก ChangeDay) [OBSERVED §2.4] |
| StartWorking 42 call sites → 6p, 51 รวมทุก overload | REPRODUCED เป๊ะ — §2.9 + `xref-summary.txt`; "42" = จุดที่ชี้ 6p พอดี (38 ext + 4 internal) |
| TryCancelWorking guard leftWorkingDay/noCancel | ยืนยัน — 0x9CB3A9/0x9CB3B3 [OBSERVED] |
| MiddleStopWork → StopWorking(false); StopWorking เรียก finishFuc แม้ยกเลิก | ยืนยัน — E9 0x54EF55; `workFinished=finished` + finishFuc ยิงเสมอ ไม่มี branch [OBSERVED] |
| โปรโตคอล MP ยังไม่มี activity lifecycle / combat initiation | ยืนยัน — `S2Types.cs:10-35` ไม่มี type เกี่ยวกิจกรรม, grep src ไม่พบ AREA_SNAPSHOT/TIME_ACTION/WORKING_STATE [OBSERVED] |

**ข้อที่โจทย์กล่าวถึงแต่ต้องแก้คำ**: โจทย์ §2.2 ให้โมเดล `T(N) ≈ N×dayTime + (N+2)×pauseTime` — พบว่าเป็น
**หนึ่งในสามโมเดลที่แข่งกัน** (RUNTIME n=1 ตรงกับ `N×(dayTime+pauseTime)`; disasm pause-structure ชี้
`N×dayTime + (N+1)×pauseTime` ถ้า finish รอ pause 2× จบ; โมเดลของโจทย์ (N+2) ไม่พบหลักฐานรองรับจาก disasm
โดยตรง) — ทั้งสามยัง UNVERIFIED ที่ rate ใหม่ รายละเอียด Section 6.4 ตามหลัก "รายงานทั้งสองด้าน ไม่เลือกให้"

### 2.4 การหลีกเลี่ยง false positive ของรอบนี้

- นับ caller แยกตาม (target RVA, owner method) จาก `xref-summary.txt` ตรง ๆ — ไม่นับซ้ำจากชื่อ method
  (บทเรียน self-correction #9 ของ `time-static-notes.md §4`: ร่างแรกจับกลุ่มตามชื่อทำให้อ่าน 51→42 ผิด)
- การจัดประเภทกิจกรรม (Section 3.2) ใช้ชื่อ method + หลักฐาน chain ที่มีจริง — ทุกแถวติดป้าย ห้ามยกชื่อเป็นข้อเท็จจริง
- ข้อสรุปที่ค้างจากรอบก่อนซึ่งตรวจซ้ำไม่ได้ (ไม่มีเกม/ไม่มี disasm ใหม่ในขอบเขตรอบนี้) ติดป้าย UNVERIFIED-ON-RERUN
  หรือคงป้ายเดิมของแหล่งอ้าง ไม่อัปเกรดให้เอง

---

## Section 3 — StartWorking call-site inventory

### 3.1 ตาราง overload (target = `WorkingUIController.StartWorking`, `dump.cs:392733+`)

| overload | RVA | จุดที่พบ (E8/E9) | external | หมายเหตุ |
|---|---|---|---|---|
| **6p** `(int dayNum, string _workName, string _callFuc, string _callFucParam, string _finishCallFuc, string _finishCallFucParam)` | **0x9CABF0** | **42 (E8 ล้วน)** | **38** | `noCancel` เขียน 0 ตายตัว (0x9CACF6 `xor edi,edi`) ⇒ งานทุกงานที่ผ่าน 6p **ยกเลิกได้** [OBSERVED §2.6] · 4 จุด internal = overload อื่น tail-delegate เข้า 6p (0x9CADDD, 0x9CAE6E, 0x9CAFD4, 0x9CB067) |
| 5p | 0x9CAD70 | 3 (E8) | 3 | delegate → 6p |
| `List<string>` | 0x9CAE00 | 1 (E9) | 1 | delegate → 6p |
| 3p | 0x9CAE90 | 1 (E9) | 1 | delegate → 6p |
| (int,string) | 0x9CB000 | 3 (E8) | 3 | รวม `WaitUIController.SureButtonClicked` (0xC14E4E) |
| **7p** (เพิ่ม `bool _noCancel`) | 0x9CB090 | 1 (E8) | 1 | caller เดียว = `PlotController.TryThinkAboutMissingSkill` (0x68545C) — งานแรกที่ยืนยันว่าใช้ noCancel=true ได้ [OBSERVED] |
| **รวม** | | **51 (49 E8 + 2 E9)** | **47** | distinct owner = 47 method (1 owner อาจเรียกหลายจุด — ที่นี่ 1 จุด/owner ทุกตัว) |

หลักฐาน: `evidence/TIMEstatic-20261027-051647/time-static-notes.md §2.9` + `xref-summary.txt:64-122` (REPRODUCED
สองครั้งใน session เดียว — self-correction §4 ข้อ 9)

### 3.2 แผนที่ caller → ประเภทกิจกรรม (47 external)

กติกาการอ่านตาราง: "OBSERVED-name" = จัดกลุ่มจากชื่อ/ตำแหน่งใน call graph เท่านั้น; แถวที่มี chain จริงรองรับ
ติดป้าย OBSERVED-chain (มี disasm หรือ RUNTIME) — **ห้ามนับจำนวน call sites เป็นจำนวน activity types**
(call site หนึ่งแยกงานย่อยได้ หลาย site อาจเป็นกิจกรรมกลุ่มเดียว)

| # | Owner method (site) | Overload | กลุ่มกิจกรรม | แหล่งจำนวนวันที่รู้จัก | callback ที่พบจากหลักฐาน | noCancel | มั่นใจ |
|---|---|---|---|---|---|---|---|
| 1 | `PlotController.StudyMartialClubSkillSure` (0x671100) | 6p | ฝึกวิชาในสำนัก | `KungfuSkillLvData.StudyDayCost()` [OBSERVED disasm §2.10] | `finishCallFuc="RealStartStudySkill"` (slot 0x1DA37A0) [OBSERVED] | false (6p) | OBSERVED-chain |
| 2 | `StudySkillController.SureStartStudySkill` (0xFE6535) | 6p | ฝึกวิชา (เส้นเดียวกับ #1 — ตัวเรียกจริงจาก StudySkillController) | เดียวกัน | เดียวกัน | false | OBSERVED-chain |
| 3–7 | `StudyLivingSkillStart` (0x66F064), `StudyMaxFightSkillStart` (0x671627), `StudyMaxLivingSkillStart` (0x671B77), `StudyMaxStateStart` (0x67308A), `StartBreakThroughButtonClicked`→`BreakThroughController` (0xC95C52) | 6p | ฝึกทักษะ/บุกเบิก | UNKNOWN (ยกเว้น max-fight: handler จบ `StudyMaxFightSkillFinish` 0x671140 gate `workFinished` [OBSERVED §2.11]) | finish handler ชื่อตรงกัน (`*Finish`) [INFERRED-name] | false | OBSERVED-name + บาง chain |
| 8 | `ManageTagController.SureUnderstandTag` (0xA52482) | 6p | ฝึก/ปลดล็อก tag (天赋) | UNKNOWN | UNKNOWN | false | OBSERVED-name |
| 9 | `ReadBookController.SureStartReadBook` (0xCF90BE) | 6p | อ่านหนังสือ | UNKNOWN | UNKNOWN | false | OBSERVED-name |
| 10 | `PlotController.SureBuildingWork` (0x6758FB) | 3p | ผลิตในอาคาร (ผ่าน plot-choice ของ `AreaBuildingChoice`) | UNKNOWN (ตัวเลือก UI 1/5/10 สร้างตอนรัน [RUNTIME area-notes P1-5]) | `workingFuc="WorkInProductionBuilding"` เห็นจากงานจริง [RUNTIME area-notes P1-5] | false | OBSERVED-name + RUNTIME-adjacent |
| 11 | `BuildingUIController.ProduceBuildingWork` — **ไม่พบเป็น caller ตรง** | — | ผลิต (ทางเข้า UI) | เดียวกัน | เดียวกัน | — | INFERRED: ไปถึงปั๊มผ่าน plot-choice/`SureBuildingWork` — เส้นเชื่อมยัง pin ไม่ได้ |
| 12 | `CraftUIController.CraftButtonClicked` (0xA5CC7C) | 6p | คราฟต์ | UNKNOWN | UNKNOWN | false | OBSERVED-name |
| 13 | `CraftPoisonUIController.PoisonButtonClicked` (0xA5A860) | 6p | คราฟต์ยาพิษ | UNKNOWN | UNKNOWN | false | OBSERVED-name |
| 14 | `EnhanceUIController.EnhanceButtonClicked` (0x930C51) | 6p | ตีแต่งอาวุธ | UNKNOWN | UNKNOWN | false | OBSERVED-name |
| 15 | `SpeEnhanceEquipController.EnhanceButtonClicked` (0x98FB6E) | 6p | ตีแต่งพิเศษ | UNKNOWN | UNKNOWN | false | OBSERVED-name |
| 16 | `ForceSpeResearchUIController.StartResearchButtonClicked` (0x782C4C) | 6p | วิจัยสำนัก | UNKNOWN (`ForceSpeResearchData.leftTime` อีกเส้นเป็นวันผ่าน housekeeping) | UNKNOWN | false | OBSERVED-name |
| 17 | `WaitUIController.SureButtonClicked` (0xC14E4E) | (int,string) | **Wait** | slider 1–30 วัน [RUNTIME area-notes P1-7] | ไม่มี (Wait ไม่ตั้ง workingFuc — วัด `store` ขยับจาก day-tick [RUNTIME]) | false | OBSERVED-chain |
| 18 | `GameController.PlayerQuickTravel` (0x7D9AAB) | 6p | เดินทางไว (กินวันผ่านปั๊ม!) | UNKNOWN | UNKNOWN | false | OBSERVED-name — **สำคัญกับ MP: travel เป็นกิจกรรมปั๊มด้วย** |
| 19–20 | `PlotController.SureHomeRest` (0x67A335), `SureHomeRestRecover` (0x67A196) | 6p | พักบ้าน | UNKNOWN | UNKNOWN | false | OBSERVED-name |
| 21–22 | `PlotController.SureHotelRest` (0x67AA35), `SureHotelRestRecover` (0x67A896) | 6p | พักโรงแรม | UNKNOWN | UNKNOWN — `RestInHotel` (0x57A05C) เรียก `StopWorking` เองกลางงาน [OBSERVED §2.8] | false | OBSERVED-name + StopWorking-chain |
| 23 | `PlotController.SureHospitalCureInjury` (0x67A4F8) | 5p | รักษาบาดเจ็บ | UNKNOWN | `HospitalCureInjury` เรียก `StopWorking` ×2 (0x53A96D, 0x53ABD8) [OBSERVED §2.8] | false | OBSERVED-name + chain |
| 24 | `PlotController.SpeCureStart` (0x58C602) | 6p | รักษาพิเศษ | UNKNOWN | UNKNOWN | false | OBSERVED-name |
| 25 | `PlotController.PlayerContinuePrison` (0x5578DC) | (int,string) | คุก (ต่อคุก) | UNKNOWN | `PlayerContinuePrisonOneDay` เรียก `StopWorking` (0x557777) [OBSERVED §2.8] | false | OBSERVED-name + chain |
| 26 | `PlotController.SurePrepareBreakPrison` (0x67D976) | 6p | แหกคุก | UNKNOWN | UNKNOWN | false | OBSERVED-name |
| 27 | `PlotController.SureSimpleWork` (0x67FA11) | (int,string) | งานหนักหัส (打杂;5 เห็นจริง) | ฝังในสตริง plot [RUNTIME area-notes P1-5] | `finishFuc` ในสตริง | false | OBSERVED-chain |
| 28 | `PlotController.StartWorking(string)` (0x660590 → 6p) | 6p | **งานสังคม/plot ทั้งตระกูล** `param = "ชื่อ;วัน;callFucParam;finishFuc;finishFucParam"` | ฝังในสตริง [RUNTIME: `巡查;3`, `配置药品;2`, `打杂;5`, `指点;3`, `捉贼;3`] | ในสตริง (เช่น `HelpPatrol`, `FinishHelpCureDisease`) [RUNTIME] | false | OBSERVED-chain |
| 29–40 | plot เริ่มงานเฉพาะ: `PatrolAreaStart` (0x556382), `GuardRichStartWork` (0x524F34), `ForceLeaderAskClean` (0x4F957F), `EnlightForNpcSpeStart` (0x4C232C), `SpeGetTalentPointStart` (0x58CC32), `SpeReduceBadFameStart` (0x59D5E2), `SpeRemoveSkillStart` (0x59DEC6), `SpeRemoveTagStart` (0x59EB1E), `SpeStartPartySure` (0x59ED63), `StartPrepareParty` (0x5B1815), `StartAskNearRandomEvent` (0x5A1BCD), `StartDigTreasureMap` (0x5A5674) | 6p | งานสังคม/plot เฉพาะเรื่อง | UNKNOWN (บางส่วนคงมาจาก plot-data) | UNKNOWN | false | OBSERVED-name |
| 41–42 | `StealForceResourceStart` (0x662048), `StealForceSkillStart` (0x662938) | 6p | ขโมยทรัพยากร/วิชา | UNKNOWN | `StealSkillFightResult` พบเป็น `fightEndCall` ของศึก [OBSERVED explorer-notes §5.1] — อาจยกเลิกงานเมื่อแพ้/ถูกจับ (ไม่มีหลักฐาน) | false | OBSERVED-name |
| 43 | `PlotController.StartMeditationWork` (0x5ADD41) | 6p | สมาธิ | UNKNOWN (`MeditationData.monthMeditationDay` อีกเส้น) | UNKNOWN | false | OBSERVED-name |
| 44 | `PlotController.ExploreAreaStart` (0x4C72A2) | 6p | สำรวจพื้นที่ | UNKNOWN | UNKNOWN | false | OBSERVED-name |
| 45 | `PlotController.StartRecruitHero` (0x5B3A23) | 5p | รับสมัคร (recruit) | UNKNOWN | UNKNOWN | false | OBSERVED-name |
| 46 | `PlotController.ForceTeachNPCContinue` (0x4FC345) | 5p | สอน NPC (ต่อ) | UNKNOWN | `ForceTeachNPCOneDay` เรียก `StopWorking` (0x4FC67E) [OBSERVED §2.8] | false | OBSERVED-name + chain |
| 47 | `PlotController.RecoverBuildingResourceRate` (0x570531) | `List<string>` | ฟื้นอัตราผลิต (育种/勘探) | UNKNOWN | UNKNOWN | false | OBSERVED-name |
| — | `PlotController.TryThinkAboutMissingSkill` (0x68545C) | **7p** | เช็คขาดทักษะ (งานชนิดที่ตั้ง noCancel ได้) | UNKNOWN | UNKNOWN | **จาก arg — ค่าจริงที่ส่งยัง pin ไม่ได้** [UNKNOWN, ถอด prologue ต่อได้ static] | OBSERVED-site |

**ข้อจำกัดที่ต้องประกาศ:** (1) กลุ่ม "ฝึกวิชา" ที่วัด RUNTIME (`StudyFightSelf` → `ChooseController` →
`StudyFightSelfChoosen` → `StartStudySkill`) ปลายทางคือ #2 — ตรวจโดย `area-notes.md P1-6` [RUNTIME]; (2) ปลายทาง
ของ `workingFuc`/`finishFuc` เป็น **string dispatch ผ่าน `SendMessage`** ไปยัง target เดียว (thunk 0x46C460 —
[INFERRED] = `PlotController.Instance` จากบริบท) ที่อยู่นอก call graph — รายชื่อ handler ที่เห็นมาจากสตริงใน
disasm + plot-data + การวัด RUNTIME เท่านั้น ⇒ รายการ callback ยังไม่ครบทุกงาน [UNKNOWN]; (3) ปุ่ม UI ที่ผูก
`TryCancelWorking` และปลายทางหลัง plot-choice ยกเลิก ผูกใน scene/asset — ไม่พบใน call graph [UNKNOWN].

---

## Section 4 — Activity lifecycle and callback inventory

### 4.1 ลำดับเฟรมของปั๊ม (โครงที่ถอดแล้ว — ทุกข้อ OBSERVED ยกเว้นที่ติดป้าย)

```
WorkingUIController.Update() 0x9CB4B0  (เฟรมละครั้ง, เฉพาะ when working(0x48)=true)
  [freeze 3 ทาง — คืนก่อนนับเวลา: PlotController.plotHappen(0x18) / SpeShowController.gameObject.activeSelf / HudController.HudPanelActive()]
  โหมดพัก (leftPauseTime(0x58) > 0): leftPauseTime -= RealTime.deltaTime → จบพัก → AddAnimation("TimeAnim")+PlaySound
  โหมดนับวัน (else):
    ถ้า leftWorkingDay(0x4C) <= 0 → workFinished(0x60)=1; SendMessage(target, finishCallFuc[, finishCallFucParam]);
                                    working=0; SetActive(gameObject,false)   [จบงานปกติ]
    nextDayTime(0x54) += RealTime.deltaTime
    threshold = static dayTime(0x0) × (skipping ? (pref TestMode==1 ? 0.2 : 0.5) : 1.0)
    ถ้า nextDayTime < threshold → return
    ถ้า callFuc(0x28) != null → SendMessage(target, callFuc[, callFucParam])   ← workingFuc รายวัน (ก่อนเดินวัน)
    leftWorkingDay -= 1; totalWorkingDay(0x50) += 1; nextDayTime = 0
    ChangeHour(24.0)  0x9CBC30 → wrap → ChangeDay() leaf (housekeeping ทั้งโลก)
    leftPauseTime = (leftWorkingDay > 0 ? static pauseTime(0x4) : 2×pauseTime)
```

หลักฐาน: `time-static-notes.md §2.4` (control flow 586 insns) + `bodies/WorkingUIController_Update.txt` (ตรวจซ้ำ
2026-10-28 ที่จุด 0x9CBBAF–0x9CBC9F: `inc [0x50]`, `mov [0x54], ebx` (nextDayTime=0), call ChangeHour 0x9CBC55,
`movss [0x58], xmm0` ที่ 0x9CBC9F) + float pool (24.0/0.2/0.5/2.0/5.0/1.0) `floats-observed.tsv`
**ลำดับสำคัญเชิงซีแมนติกส์:** `workingFuc` ถูกยิง **ก่อน** day-tick ของวันนั้น (`SendMessage` ที่ 0x9CBB9C มาก่อน
ChangeHour 0x9CBC30) [OBSERVED] — งานผลิตจ่ายของวันก่อน housekeeping; และ callback รายวันอาจหยุดงานเอง
(เช็ค `working` คงอยู่หลัง callback — โครง disasm, `time-audit-notes.md §3`) [INFERRED]

### 4.2 execution chain รายกิจกรรม

**(ก) ฝึกวิชาในสำนัก (OBSERVED-chain เต็มสาย — รวมทั้ง finish และยกเลิก)**

```
UI: BuildingUIController.StudyFightSelf (0xB7B530) → SinglePlotData + PlotController.ChangePlot (เลือกวิชา, ChooseType.HeroSkill)
  → [RUNTIME: sendResultFuc="StudyFightSelfChoosen", targetHero=heroID 0 — area-notes P1-6]
  → StudySkillController.StartStudySkill (0xFE59D0) — ยืนยันเงิน: KungfuSkillLvData.StudyMoneyCost → ChangeMoney(−เงิน) [OBSERVED §2.10]
  → SureStartStudySkill (0xFE6280): dayNum = StudyDayCost() → StartWorking(6p, callFuc=null, finishCallFuc="RealStartStudySkill")
  → (ปั๊มเดิน N วัน — ฝึกไม่มี workingFuc [RUNTIME area-notes P1-6: workingFuc=null])
จบงาน: Update กิ่ง workFinished → SendMessage("RealStartStudySkill") → PlotController.RealStartStudySkill (0x56FF80)
  → E9 → StudySkillController.RealStartStudySkill (0xFE5490):
      **gate workFinished ที่ 0xFE55CB — ถ้า workFinished==0 → return (ไม่ทำอะไรเลย)**
      else → PlayerStudySkill/บันทึกสถิติ [OBSERVED §2.10]
ยกเลิกกลางทาง: StopWorking(false) → workFinished=false → finishFuc ยิงเสมอ → RealStartStudySkill gate แล้ว return
  ⇒ ฝึกสำนักเส้นนี้: ยกเลิก = ไม่ได้วิชา, เงินค่าฝึกถูกหักไปแล้วตอน SureStartStudySkill — **ไม่พบ refund** ใน body ที่ถอด
  [OBSERVED เชิง "ไม่เห็น call คืนเงินในสายที่ถอด" / refund อื่น = UNKNOWN]
```

**(ข) max-skill (StudyMaxFightSkillStart)** — handler จบ `StudyMaxFightSkillFinish` (0x671140): ถ้า
`workFinished==0` → ข้ามทั้งหมด (0x671242 je, ไม่เขียน workFinished) else → `ChangeMaxFightSkill(player, skillId, 1, true)`
[OBSERVED §2.11] ⇒ สายนี้ **กันรางวัลตอนถูกยกเลิก**

**(ค) ฝึกสำนักอีกสาย (StudyFightSelfFinish 0x66CC40)** — **ไม่มี gate workFinished**: เข้าเลย — สุ่ม hero ช่วย
(โอกาส `min(0.05+n×0.025, 0.25)`), exp ผ่าน `GetStudyFightExp`/KungfuSkillLvData; workingFuc รายวันของสายนี้ =
`"GetStudyFightExp;"` (slot 0x1DB50D8) [OBSERVED §2.11] ⇒ **ยกเลิก = exp ที่ไปแล้วคงอยู่ตามสถานะที่ค้าง** (รางวัล
"บางส่วน" เกิดจาก workingFuc รายวัน + handler ไม่ gate) — เป็นแบบอ้างอิงที่ดีสำหรับ "คำนวณผลตอบแทนตามผลงานที่ทำไปแล้ว"

**(ง) งานผลิต (挖掘/耕作/...)** — ทางเข้า `BuildingUIController.ProduceBuildingWork` (0xB6D580) → (เส้นเชื่อมยัง
pin ไม่ได้ — INFERRED) → `StartWorking`; ค่าจริงจากการวัด: `workName="挖掘"`, `workingFuc="WorkInProductionBuilding"`,
`workingFucParam="3_1"` (buildingID 3), 10 วัน ≈ 12.5 วิ จบเอง, ทรัพยากรสำนักขยับรายวัน, ปฏิทิน +10 พอดี,
`hour` คงที่ [RUNTIME area-notes P1-5] — `finishFuc` ของสายนี้ยังไม่ถูกถอด [UNKNOWN]

**(จ) Wait** — `WaitUIController.SureButtonClicked` (0xC14E4E) → `StartWorking(int,string)` → ปั๊มเดียวกัน; Wait 5 วัน =
+5 วัน, ทรัพยากรสำนักขยับ (+455/+235.6/0/0/−400/0) **แม้ไม่มี workingFuc** ⇒ housekeeping รายวันอยู่ใน ChangeDay
leaf [RUNTIME area-notes P1-7 + OBSERVED leaf §2.14]

**(ฉ) งานสังคม/plot สตริง** — `PlotController.StartWorking(string)` แยก `"ชื่อ;วัน;callFucParam;finishFuc;finishFucParam"`
แล้วเรียก 6p; ตัวอย่างจริง 7 ชุด (area-notes P1-5) — จำนวนวันฝังในสตริง ⇒ validator ของ MP ต้อง parse สตริงนี้เป็นอีก
แหล่งหนึ่ง [OBSERVED-chain]

**(ด) งานที่ตัดงานกลางคันเอง (StopWorking callers — งานจบก่อนกำหนดมีอยู่ในเกม)** —
`HospitalCureInjury` ×2, `ForceTeachNPCOneDay`, `PlayerContinuePrisonOneDay`, `RestInHotel` [OBSERVED §2.8] —
ปั๊มเองไม่เรียก StopWorking ทางเดียว (จบปกติผ่านกิ่ง workFinished) ⇒ กลุ่มงานเหล่านี้เป็นพฤติกรรม
"complete-early" ที่ handler รายวันตัดสินเอง — รายละเอียด per-day logic ยังไม่ถอด [UNKNOWN]

**(ด+) Quick travel** — `GameController.PlayerQuickTravel` เรียก StartWorking 6p [OBSERVED-name] ⇒ การเดินทางแบบไว
**เป็นกิจกรรมปั๊ม** (จำนวนวัน UNKNOWN) — กระทบ MP: travel กับงานอื่นแย่งปั๊มเดียวกัน [INFERRED]

### 4.3 Cancellation semantics — ตารางตามที่โจทย์ข้อ 5.2 กำหนด

> หมายเหตุก่อนตาราง: เส้นยกเลิกเดียวที่มีอยู่ในเกม = `TryCancelWorking` → plot → `MiddleStopWork` → `StopWorking(false)`
> [OBSERVED]; **`MiddleStopWork` ไม่เช็ค noCancel ซ้ำ** ⇒ เป็น forced path ที่ใช้แทน "ถูกโจมตีระหว่างงาน" ได้ในทาง
> primitive [OBSERVED] — แต่ผลข้างเคียง resource/เงิน/บัฟ **ยังไม่เคยวัด** ทั้งหมด (NOT-RUN) เว้นแต่ระบุ

| Case | สถานะก่อน | เส้นทางที่เรียก | สถานะหลัง | ผลตอบแทน/resource | ความมั่นใจ |
|---|---|---|---|---|---|
| **Complete normally** | working=1, leftWorkingDay=N→0, noCancel ใด ๆ | ปั๊มกิ่ง `leftWorkingDay<=0` | workFinished=1, working=0, UI ซ่อน, skipping ค้าง true [RUNTIME] | finishFuc ยิงหลังวันสุดท้าย + housekeeping ครบ N วัน; handler gate/ไม่ gate ต่างกัน (4.2 ก/ข/ค) | OBSERVED (โครง) + RUNTIME (งานจริง 3 ชนิด) |
| **Cancel before first day** | working=1, leftWorkingDay=N (ยังไม่ครบวันแรก), nextDayTime < threshold | ปุ่ม UI → TryCancelWorking (guard ผ่าน) → plot → MiddleStopWork → StopWorking(false) | workFinished=0, working=0, **เป็นก่อน day-tick แรก ⇒ ปฏิทินยังไม่เดินเลย** | workingFuc ยังไม่ถูกยิง; finishFuc ยิงด้วย workFinished=false → handler ที่ gate ทำอะไรไม่เลย (RealStartStudySkill/StudyMaxFightSkillFinish) | OBSERVED (โครง) · คืนเงิน/ทรัพยากร = NOT-RUN |
| **Cancel after partial progress** | working=1, leftWorkingDay=k (0<k<N), ผ่านไป N−k วัน + housekeeping ครบเท่านั้น | เดียวกัน | เดียวกัน | housekeeping/workingFuc ของวันที่ผ่าน **คืนไม่ได้** (ใบไม้เสร็จแล้ว); รางวัลปลายทาง: gate-handler = ไม่ได้, no-gate (StudyFightSelfFinish) = exp ค้างใช้ต่อ | OBSERVED (โครง + handler 2 สาย) · ผล resource = NOT-RUN |
| **noCancel=true** | working=1, noCancel=1 (ยืนยันได้จาก 7p caller เดียว `TryThinkAboutMissingSkill`; ค่าที่ส่งยัง pin ไม่ได้) | TryCancelWorking → **guard return เงียบ** (0x9CB3B3) | งานเดินต่อ | ยกเลิกทาง UI ไม่ได้; **MiddleStopWork ยังยกเลิกได้** (ไม่เช็ค noCancel ซ้ำ) — เส้นบังคับมีอยู่จริง | OBSERVED (guard) + INFERRED (ผลข้างเคียงเส้นบังคับ = UNKNOWN/NOT-RUN) |
| **Finish และ attack มาถึงหน้าต่างตัดสินเดียวกัน** | working=1 ใกล้จบ, คำสั่งโจมตีเข้าคิว host | main thread เดียว (เอนจิน + IPC dispatcher `MainThreadDispatcher`) | ยกเลิกได้เฉพาะ `working==1 && workFinished==0`; หลัง StopWorking, working=0 ปิดทางเรียกซ้ำ | ไม่มี double-outcome ได้ **ถ้า** validator ตรวจตอน dequeue ในคิวเดียวกับ day-tick [INFERRED — โครงคิวมีจริง, กติกายังไม่ implement] | INFERRED · ห้ามอ้างว่า "ปลอดภัยแล้ว" จน implement + วัด |

กติกาที่ห้ามอนุมานเกิน (ตามโจทย์ §5.1): `StopWorking(false)` เรียก finishFuc **ไม่ได้แปลว่า** handler จ่ายรางวัล/
คืนเงิน — ยืนยันแล้วเฉพาะ 3 handler (gate ×2, ไม่ gate ×1); callback ถูกเรียกซ้ำจากเส้นอื่น: ปัจจุบันเห็น finishFuc
ยิงจาก 2 จุดเท่านั้น (กิ่ง Update + StopWorking — exclusive กันเพราะ working ปิดหลังทั้งคู่) [INFERRED จากโครง —
เส้นอื่นที่ยิงสตริงเดียวกันทาง scene/dispatch = UNKNOWN]; กิจกรรมที่ห้ามยกเลิกจริง ๆ (noCancel=true) ยังระบุ list
ไม่ได้เพราะค่า arg ของ 7p-site ยังไม่ pin [UNKNOWN]

### 4.4 สูตรเวลา T(N) — สามโมเดลแข่งกัน (ตัดสินไม่ได้ในรอบนี้)

| โมเดล | สูตร | ที่มา | สถานะ |
|---|---|---|---|
| M-RUNTIME | `N×(dayTime+pauseTime)` | วัดงาน 10 วัน = 12.5 วิ ที่ 1/0.25 ตรงเป๊ะ (n=1) | [RUNTIME n=1] |
| M-DISASM | `N×dayTime + (N+1)×pauseTime` (pause หลังทุกวัน + วันสุดท้าย 2×pause ก่อนจบ) | โครง disasm §2.4 (ถ้า finish ต้องรอ pause 2× หมด) | [INFERRED — ลำดับ pause/finish ยังไม่ pin เต็ม] |
| M-PROMPT | `N×dayTime + (N+2)×pauseTime` | โจทย์ T5 §2.2 | [UNVERIFIED — ไม่พบหลักฐานรองรับใน disasm โดยตรง] |

ที่ dayTime=1, pauseTime=0.25, N=10: 12.5 / 12.75 / 13.0 วิ — RUNTIME วัดได้ ~12.5 ⇒ M-RUNTIME เข้าใกล้ที่สุด ณ
n=1 แต่ **ต่างกัน < 1 pause เดียว (0.25 วิ) ซึ่งเล็กกว่า noise ของ cadence hook ที่วัดเคย (0.75–2.9 วิ)** ⇒
ไม่มีสิทธิ์เลือกโมเดลจนกว่าจะวัดซ้ำที่ `dayTime` ใหม่ (Section 9) [UNKNOWN — `time-static-notes.md §2.4` หมายเหตุ
2026-10-27 ก็สั่งห้ามใช้ทั้งสองสูตรตัดสินแล้ว]

---

## Section 5 — Multiplayer ownership and concurrency

### 5.1 แผนที่ data ownership (ทุกแถวระบุไฟล์/method จริงของ mod — ไม่อนุมาน host-authoritative ครอบทุกระบบ)

| ข้อมูล | เจ้าของเชิงโครงสร้าง | mod เห็น/ทำอะไรวันนี้ | ชนิด ownership (ตามโจทย์ §6.1) |
|---|---|---|---|
| Player identity | เกม: `heroID 0` + `WorldData.Player()` (`sp0-sp7-notes.md §2`; `WorldData.playerHero` ไม่มีจริง 0 ref) | อ่าน: `SnapshotReader.GetPlayerHero()`, `hero.itemListData/fame/badFame/forceContribution` (`S1/WorldSampler.cs:123-133`) | **global game state** — mod ยังไม่มีมโนทัศน์ "ผู้เล่นหลายคน" ใน protocol (handshake 1 session/1 secret — `S2/Protocol/S2Handshake.cs`) |
| ตำแหน่ง/area membership | `HeroData.atAreaID` (−1 = BigMap), `bigMapPos`, `inSafeArea/inPrison` (`worldmap-notes.md §1.1`) | อ่าน: `SnapshotReader.GetPlayerPos()` → `partyPos.areaId` (`WorldSampler.cs:117-121`) + NPC icons/bigMapPos/fight icon (`WorldSampler.cs:162-208`) | **global game state** (เกม sync `AreaData.insideHeros` จาก `atAreaID` — `area-notes.md P1-3` [RUNTIME]) |
| Activity state | `WorkingUIController` singleton (fields 0x48–0x68, statics 0x0/0x4) | **ไม่แตะเลย** — grep `Working|workFinished|leftWorkingDay` ใน `src/` พบ 0 (ยกเว้น comment `SP5/Sp5T0Probe.cs:429`) | **host-only state** ที่ยังไม่ถูกแสดงออก — มีความหมายเฉพาะในโปรเซส host |
| Global calendar/hour | `WorldData.worldTime` + `hour`; writer = `ChangeHour` ×2 จุด + clamp `GameDataUpdateChecking` (`time-static-notes.md §2.1`) | อ่าน: `WorldSampler.Collect()` — `wt.year/month/day`, `world.hour`, `gameDifficulty` (`WorldSampler.cs:91-114`) 10 Hz (stream 2 Hz, `CoopPrefs.cs:60-62`) | **global game state** — ผู้เขียนเดียวในทางเกมคือ host process เอง |
| Resource/reward mutation | `ForceData.resourceStore/resourceChange`, `ItemListData.money`, `AreaData.changeResource` | อ่านเท่านั้น (`WorldSampler.cs:126-152`, `area-notes.md P1-2`) | **global game state** — mod ไม่มี write path ใด (ยืนยัน §5.3) |
| Battle participants/identity | `BattleController.teams/nowActiveUnit/nowActiveState/battleTime/battlePaused` | มิเรอร์อ่าน: `S2Sampler.Read()` (`S2/Host/S2Sampler.cs:31-144`) — battleKey candidate = sha256(`"s2battle|"+teamIDs+firstFrame`) (`S2Sampler.cs:156-182`); คำสั่งเขียน 2 ตัวในศึก: `spike move/attack` → `BattleGridClicked` เท่านั้น (gates: arm ครั้งเดียว + `battleState==Fighting` + `!battlePaused` + `nowActiveState` ที่กำหนด + `playerControl` + เป้า alive (`IsAlive && hp>0` — `S1.5/AttackCommand.cs:168-243`, `S1.5/MoveCommand.cs:203-246`)) | **host-authoritative เฉพาะศึก** — นี่คือระบบเดียวที่ host คุมคำสั่ง ผ่าน `AllowRemoteInput` + `TurnGate` |
| BigMap icons/fight representation | `BigmapNpcController.heroData @0x18` + `BigMapFightIcon @0xD0`; ตำแหน่งจาก `heroData.bigMapPos` | อ่าน: `npc.BigMapFightIcon.activeInHierarchy` (`WorldSampler.cs:205-207`) — S1 viewer | **global game state** (cosmetic layer อ่านจาก data) |
| Snapshots/deltas/resync | mod: `Snapshot` (+seq/hash) + `Resync/ResyncAck` (`S2Types.cs:10-35`; envelope resync ตั้งก่อน Encode) | ครอบเฉพาะ **ศึก** — `S2Sampler.Read` คืน null เมื่อไม่มีศึก/นอก Prepare/Ready/Fighting (`S2Sampler.cs:39-55`) | **host-only, battle-scoped** — world/activity snapshot ไม่มี |
| state ที่ guest ขอเปลี่ยนได้ | — | **ไม่มีอะไรเลย**: guest→host allowlist = `Auth/Ping/Resync/Bye` (`S2Types.cs:58-70`); `S2ControlHandler` ทำได้แค่ Pong/ResyncAck/ปิด (ไม่มี write ไปเกม — comment เอกสาร `S2ControlHandler.cs:18-28`); `commandsAccepted` ออกแบบให้ = 0 (self-test ตรวจ `S2HostService.cs:605-673`) | — (ข้อจำกัดชัดเจนของ S2 spectator) |
| pref ของ mod เอง | `MelonPreferences.cfg` + `UserData\LongYinCoop\net.cfg` (ML ไม่แตะ — `S2NetConfig.cs` flat key/value fail-closed, คีย์ `enabled/role/secret/allownonloopback/bind/port/sendhz/heartbeatms` — `S2NetConfig.cs:124-132`) | `set_pref` IPC = mod-state writer, มี gate ป้องกัน pref ที่ห้าม (`CoopPrefs.cs:37-62`: `Enabled/Probe/Ipc/DevCommands/AllowRemoteInput/JitProbe/W2/W4/W6/W7/Sp5T0/Capture/WorldHz/WorldStreamHz`) | **mod-state** แยกจากเกม |

### 5.2 Concurrency matrix (10 เคสตามโจทย์ §6.2 — แยก "ยืนยันจากโค้ด" กับ "อนุมาน")

| # | เคส | สิ่งที่ยืนยันจากโค้ด/หลักฐาน | สิ่งที่เป็นอนุมาน/ยังไม่รู้ |
|---|---|---|---|
| 1 | A ฝึกคนเดียว | ทำงานปกติ — วัดแล้ว 3 ชนิดงาน [RUNTIME] | — |
| 2 | A ฝึก ขณะ B เริ่มผลิต | singleton + field เดียว ⇒ ทาง native **ทำพร้อมกันไม่ได้** — StartWorking ครั้งหลังเขียนทับ state (`leftPauseTime=pauseTime, nextDayTime=0, resourceNum=0, totalWorkingDay=0` — disasm §2.6) | เงื่อนไขที่เกมเรียก StartWorking ขณะ working=1 ยังไม่ถูกบล็อกหรือปล่อยผ่าน — "เริ่มทับขณะ working=true" ไม่เคยวัด [UNKNOWN] |
| 3 | A ฝึก ขณะ B เดินทาง BigMap | สอง driver คนละสาย (`Update` vs `BigMapController.FixedUpdate` — caller ของ ChangeHour ×3 [OBSERVED §2.2/2.13]) | วันจากสองสาย**แทรกกันได้** (interleave) บน main thread เดียว — ลำดับจริงไม่ deterministic ต่อผู้เล่น [INFERRED] |
| 4 | A และ B ทำกิจกรรมกินวันพร้อมกัน | = เคส 2 (native serialize ไม่ได้) | MP ต้อง serialize ผ่านคิว host หรือ manager ใหม่ — โมเดลยังไม่เลือก [design decision] |
| 5 | A ทำกิจกรรม แล้วถูกโจมตี | primitive ครบสองฝั่ง: `MiddleStopWork` (ไม่เช็ค noCancel) + `PrepareBattleMap` ×11 + `fightEndCall` | **ไม่มี wiring** "เริ่มศึก ⇒ ยกเลิกงาน" ทั้งในเกมและ mod [OBSERVED absence]; ผลยกเลิกต่อ resource = NOT-RUN |
| 6 | ศึกเริ่มตรงจังหวะงานใกล้เสร็จ | กติกาตัดสินที่หลักฐานรองรับ: ยกเลิกได้เฉพาะ `working && !workFinished`; หลัง StopWorking `working=0` ปิดทาง | หน้าต่าง "ใกล้เสร็จ" ที่ workFinished=1 ไปแล้ว = รางวัลจ่ายแล้ว (finishFuc ยิงใน Update ก่อน) — ยกเลิกไม่ได้ [INFERRED จากโครง] |
| 7 | C เชื่อมต่อระหว่าง A ทำกิจกรรม | มีแค่ battle resync (`Resync/ResyncAck` + hash) — `S2ControlHandler.cs:122-156` | world/activity snapshot สำหรับ late join **ไม่มี** — ต้องสร้าง message ใหม่ [OBSERVED absence] |
| 8 | ผู้กำลังทำกิจกรรม disconnect/reconnect | host เป็นคนถือ state เกมอยู่แล้ว (guest ไม่มี write) ⇒ disconnect ไม่กระทบงานของ guest **ในฐานะ viewer** | ถ้าในอนาคต guest "เป็นเจ้าของงาน" ต้อง persist mod-state เอง (งานเกมไม่เซฟ state ปั๊ม — §6.6) |
| 9 | host เปลี่ยน session/โหลดโลก | ไม่มีเส้นทางรองรับใน mod ปัจจุบัน (listener per-process, `S2HostService` lifecycle `CoopMod.cs:79,93`) | [NOT-RUN] งานค้างข้าม save/load ของเกมเองก็ยังไม่วัด (`time-notes.md §8 ข้อ 4`) |
| 10 | Pause/Resume ขณะหลายกิจกรรม/มีศึก | ปั๊มใช้ unscaled delta ⇒ `timeScale=0` ไม่หยุดปั๊ม [OBSERVED §2.3]; pause ชั้นเกม (P1–P6) ไม่มีชั้นใดหยุดปฏิทิน | Global pause ต้อง gate สายปั๊มฝั่ง mod เอง (ไม่มี flag เนทีฟให้ตั้ง) + policy คำสั่งขณะ pause = design decision [INFERRED/decision] |

### 5.3 Protocol & write-path audit (สรุปจากโค้ดจริง)

- message types ทั้งหมด: host→guest `Hello, World, BattleEvent, BattleLog, Snapshot, Pong, Bye, AuthResult, Deny,
  Chunk, ResyncAck`; guest→host `Auth, Ping, Resync, Bye` (`src/LongYinCoop/S2/Protocol/S2Types.cs:10-70`) —
  **ไม่มี** type ของ activity/pause/area/combat-initiation (grep `AREA_SNAPSHOT|TIME_ACTION|WORKING_STATE|ACTIVITY|PAUSE|RESUME`
  ใน src = 0 hit)
- IPC (`IpcServer.cs`): `ping, get_version, get_scene, get_player_pos, stats, snapshot, capture, compat_dump,
  probe_types, patch_report, probe_units, s1_report, spike, set_pref, bench_dispatch` — ตัวที่เขียน**เกม**: เฉพาะ
  `spike move/attack` (ในศึก, gate `AllowRemoteInput` + arm-once + state gates ตาม §5.1); ตัวที่เขียน **mod-state**:
  `set_pref` (มี gate), `spike arm/disarm`, `capture`; ที่เหลืออ่านอย่างเดียว
- lifecycle: `CoopMod` เริ่ม IPC + `S2HostService` ตอน MelonLoader start, S2 ไม่ทำอะไรจน `net.cfg` `enabled=true`
  (poll ≤1 Hz — `CoopMod.cs:75-80`), overlay ฝั่ง guest วาดอย่างเดียวไม่กิน input (`CoopMod.cs:80`)

### 5.4 สรุป state ที่ "ยังไม่มี" สำหรับ activity lifecycle (จาก Section 5.1–5.3)

1. per-player activity state (kind/progress/owner) — ไม่มีทั้งในเกม (singleton) และ mod
2. `WORKING_STATE`/`ACTIVITY_*` message + `PAUSE/RESUME(ack)` — ไม่มี (มีแต่แบบร่าง `area-notes.md` ท้ายไฟล์
   `AREA_SNAPSHOT/AREA_DELTA/TIME_ACTION_REQUEST/ACCEPT/REJECT/DONE` — ยังไม่ได้ผ่านการอนุมัติ/implement)
3. validator นอกศึก (area membership/สิทธิ์/จำนวนวัน) — ไม่มี
4. world snapshot สำหรับ late join/reconnect — ไม่มี (มีเฉพาะ battle)
5. กลไกกันซ้ำ (reqId + ตรวจตอน dequeue) — ไม่มี (โครง `SeqTracker` มีเฉพาะฝั่ง data frame)

---

## Section 6 — Calendar/timer semantics

### 6.1 "กิจกรรม 5 วันใช้เวลา 60 วินาที" หมายถึงอะไรในระดับ world state (ตอบตามหลักฐาน — ไม่เลือกแทน)

| ความหมาย | กลไกที่ต้องมี | หลักฐานรองรับ | ผลเมื่อหลายกิจกรรมพร้อมกัน |
|---|---|---|---|
| (1) real-time 60 วิ **และ** ปฏิทินเดิน 5 วัน | native pump + ตั้ง static `dayTime=12` ต่อ StartWorking (pauseTime คง 0.25) | ปั๊ม**คือ**ตัวเดินปฏิทิน — `Update → ChangeHour(24) → wrap → ChangeDay()` [OBSERVED §2.4]; จำนวน `ChangeDay()` = N โดยโครง ⇒ housekeeping/AI/resource/expiry ครบ N ชุดเหมือนเดิม | กิจกรรม 2 งานขนานไม่ได้ (singleton) — แต่ถ้า serialize ตามลำดับ ปฏิทินเดินรวม N1+N2 วันตามงาน |
| (2) real-time 60 วิ **แต่** ปฏิทินเดินตามนาฬิกาส่วนกลางแยก | server timer ใหม่ + mod เรียก `ChangeHour(24)`/`ChangeDay()` เอง — **ต้องเลียนลำดับ callback เอง** (`workingFuc` ก่อน day-tick, finish หลังวันสุดท้าย) และ `ChangeHour` wrap **ครั้งเดียวต่อ call** (if เดียวไม่ใช่ while — `time-audit-notes.md §3` ข้อจำกัด) ⇒ delta ≤ 24 หรือเรียกซ้ำ | primitive เดียวกับเกมมีจริง (`ChangeHour` public, `Console.Input` ใช้ `*Direct` ได้) | คุมจังหวะได้ละเอียดสุด (per-activity rate) แต่เสี่ยงเลียนผิด — ลำดับ disasm มีหลักฐาน ส่วน control-flow เต็มของ leaf ยังไม่ถอด |
| (3) เดินตามปฏิทินเหมือนเดิมแต่ปรับอัตรา dayTime | ทางเดียวกับ (1) | เดียวกัน | เดียวกัน |

**สรุปเชิงหลักฐาน (ไม่ตัดสินให้):** ในเกมนี้ (1) และ (3) **เป็นความหมายเดียวกันโดยโครงสร้าง** — ไม่มี "นาฬิกากิจกรรม
แยกจากปฏิทิน" อยู่แล้ว; ทางแยกจริงมีแค่ (2) ซึ่ง mod ต้องรับผิดชอบลำดับ callback เอง ถ้าผู้ใช้กำหนดไม่ได้ว่า
"กิจกรรมคนละงานใช้ rate ต่างกันพร้อมกัน" เป็นข้อบังคับหรือไม่ งานจะยังเปิดอยู่ — รายงานเป็น design decision ที่
ยังไม่ปิด (Section 11 ข้อ 3–4) [UNKNOWN/decision]

### 6.2 housekeeping ไม่ถูกนับซ้ำ — เงื่อนไขที่ต้องรักษา

- จำนวน housekeeping = จำนวน `ChangeDay()` leaf ถูกเรียก [OBSERVED §2.14] — ทุกสายเดินวัน (ปั๊ม/เดินทาง/
  `FinishExploreMap`/plot `*Result`) รวมมาที่ leaf จุดเดียว (caller ของ leaf = 2 จุด: `ChangeDay(int)` และ
  `ChangeHour` [OBSERVED §2.1]) ⇒ ตราบใดที่ mod **ไม่เรียก ChangeHour/ChangeDay เองเพิ่ม** จะไม่มี day-tick ซ้ำ
  จากกิจกรรมคนละคน — แต่ถ้าเลือกทาง (2) ต้องกำหนดกฎ "ใครเป็นผู้เดินวัน" ให้เดียว (แนะนำเชิงโครง: host เท่านั้น —
  guest ไม่มี write path อยู่แล้ว) [INFERRED]
- `ChangeHour` เพิ่ม `hour` **โดยไม่รีเซ็ตเมื่อวันเปลี่ยน** (ปั๊มส่ง 24.0 → wrap คืนค่าเดิม) — เห็นจาก RUNTIME
  `hour` คงที่ตลอด 15 วันงาน [RUNTIME area-notes P1-5/P1-7] + disasm (write 2 จุด: บวกก่อน ลด 24 หลัง) [OBSERVED]
  ⇒ "วันของกิจกรรม" ไม่ขยับชั่วโมงของวัน — สำคัญกับ `WeatherController.ChangeWeatherLastTime(−delta/24)`
  (สภาพอากาศนับถอยเป็นหน่วยวัน แต่ −24/24 = −1 วันต่อวันงานเสมอ [OBSERVED §2.1])
- สายเดินทาง (`BigMapController.FixedUpdate`) สะสม `hour` ตามระยะ (×2.0 เมื่อ fastMode) และครบ 24 ชั่วโมง
  เรียก day-tick ตามธรรมชาติ ⇒ **วันจากการเดินทางแทรกกลางวันของกิจกรรมได้** (เคส 5.2-3/4) — ไม่มี mutex ใดกัน
  [OBSERVED (call sites) + INFERRED (interleave)]

### 6.3 การเพิ่มความเร็ว `dayTime` — ผลต่อ world state

- ค่าที่เปลี่ยน = เวลาจริงต่อวันเท่านั้น; **เนื้อหาต่อวันไม่เปลี่ยน** (จำนวน callback รายวัน = N, housekeeping = N,
  ผลผลิตรวม = ผลรวม workingFuc N ครั้ง) [INFERRED จากโครง + RUNTIME n=1] — จังหวะเวลาจริง "กระจายกว่าขึ้น"
  เท่าเดิม
- ผลที่**ยังไม่รู้**: (α) cadence จริงรวมช่วง freeze 3 ทาง + animation ที่ rate ใหม่ (gaps hook เคย 0.75–2.9 วิ);
  (β) `>>` ที่ threshold ใหม่ — โครงชี้ว่า skip คูณ threshold (×0.2/×0.5) จึงยังเร่งได้สัดส่วน แต่ไม่เคยวัดที่
  dayTime อื่น [INFERRED, n=1 เดิม]; (γ) `pref "TestMode"` ห้ามเขียน — ทุกจุดพบเป็นการอ่านผ่าน
  `PlayerPrefDictionary.GetInt` [OBSERVED §2.4] ⇒ mod ห้ามแตะและต้องคำนวณตารางโดยไม่พึ่ง TestMode
- ระบบที่**ไม่**อิง dayTime: ศึก (`battleTime` วินาทีจริง, 2,664 sample Δ=0 [RUNTIME battle-notes]), HUD (cosmetic),
  ดันเจี้ยน (นาฬิกานิ่ง [RUNTIME explorer-notes §3]), นาฬิกาเดินทาง (ต่อระยะ ไม่ต่อวินาที) — การปรับ dayTime จึง
  **ไม่กระทบ** ระบบเหล่านี้ [RUNTIME/DUMP]

### 6.4 ระบบที่ผูกวัน (จะเสียหายถ้าเดินวันผิดจังหวะ/ผิดจำนวน)

จาก consumers inventory (`time-notes.md §4` + leaf call list [OBSERVED §2.14]): AI รายวันทั้งโลก
(`ManageAllAI` + `ResetAllAIMananged` + `ManageAiForceAttackAreaAndResourcePoint`), ทรัพยากรรายวัน
(`ManageAreaStuff`/ForceStore — วัดขยับแม้ Wait [RUNTIME]), บาดเจ็บ/ฟื้น (`GetHeroAutoInjuryChange` ×3,
`ManageHeroAutoRecover`), tag/buff (`ManageTagTime`), ไปรษณีย์ (`ManageMailTime`), ภารกิจหมดเวลา
(`GiveUpMission`), ประชุมสำนัก (`IsMeetingTime/SetMeetingEnd`), event expiry (`RemoveBigMap/AreaMapRandomEvent`,
`RemoveEvent` + callback สตริง `eventOutTimeCallFuc`), สุ่มศัตรู/เหตุการณ์ (`GenerateRandomEnemy/Event`,
`ManageWorldEvent`), รายเดือน (`ChangeMonth → ManageMonthTask/MonthRefreshAllBuildings/ManageSalary/...`),
งานยาวอีกชั้น (วิจัย/สมาธิ/ยาพิษ/หนังสือ/สร้างอาคาร — เดินด้วย `leftTime` วันผ่าน housekeeping [GUESS,
caller ของ Manage* ยังไม่พบ — แต่ housekeeping อยู่ใน leaf แล้วจึงไหลผ่าน]) [OBSERVED รายการ/GUESS กลไกภายใน]
⇒ กฎแข็ง: **จำนวนวันที่เดินต้องมาจาก `worldTime` เสมอ ห้ามนับจาก `totalWorkingDay`** (มติเดิม area-notes S4c —
คงอยู่ไม่เปลี่ยน)

### 6.5 Skip / TestMode และ N นอกชุด {1,5,10,15}

- Skip เขียนแค่ธง `skipping` (0x62) [OBSERVED §2.4] ⇒ MP สั่ง skip ได้โดย set ธงเดียว (ผ่าน typed interop) —
  ไม่ต้องแตะ pref; **pref `"TestMode"` ห้ามเขียน** [OBSERVED]; snapshot ควรส่ง**หลังจบ animation skip** (วันไต่
  ทีละ 1 กลางการ์ตูน — มติเดิม area-notes S4c ยืนยันซ้ำด้วยโครง disasm: skip คูณ threshold ไม่ได้กระโดดวัน)
  [RUNTIME + OBSERVED]
- N อื่น: Wait slider 1–30 อยู่ในช่วง; งานที่วันมาจาก `StudyDayCost()`/สตริง plot/skill cost = ค่าจาก game data ⇒
  ตาราง mapping ต้องมีกฎ fallback สำหรับ N ทุกค่า (เช่น linear หรือ default rate) — **ยังไม่กำหนดแทนผู้ใช้**
  [design decision — เดิม `time-followup-notes.md §2.2` ก็ค้างเช่นเดียวกัน]

### 6.6 save/load / late join / reconnect

- state ปั๊ม (working/nextDayTime/leftPauseTime/...) **ไม่อยู่ใน `WorldData`** — ตรวจจาก full field snapshot
  `world-fields-r3.json` ไม่มีฟิลด์ working ใด ๆ + `GameSaveData = WorldData + HeroList + TempHeroList`
  [DUMP+RUNTIME] ⇒ งานค้างน่าจะหายหลังโหลด [GUESS — NOT-RUN เดิมคงอยู่] ⇒ MP ต้อง persist/restore activity state
  ฝั่ง mod เองและรวมใน full snapshot สำหรับ late join
- autosave ระหว่างเล่นมีจริง (`needAutoSave → GameDataController.Save(0)`, gate `DisableAutoSave`) แต่**ผู้ตั้ง
  `needAutoSave` ยังไม่พบ** [OBSERVED §2.12 + UNKNOWN] — ความเสี่ยง: งานค้าง + autosave กลางคัน = state ตัดสิน
  ไม่ได้หลัง crash; ไม่มี autosave ตอนออก [RUNTIME เดิม]
- reconnect: มี battle resync เท่านั้น — activity ต้องออกแบบแยก (Section 9/10)

---

## Section 7 — Architecture options and recommendation

### 7.1 เปรียบเทียบ Option A/B/C (ตามโจทย์ §6.3 — ขยายจาก `time-audit-notes.md §3/§4`)

| เกณฑ์ | **A — native pump แล้ว serialize** | **B — server-authoritative activity manager ต่อผู้เล่น** | **C — แยก elapsed กิจกรรมออกจากปฏิทิน** |
|---|---|---|---|
| correctness (หลักฐานที่รองรับ) | สูงสุด — ใช้ปั๊ม/ลำดับ callback/housekeeping ของเกมตรง ๆ [OBSERVED] | กลาง — callback และ state บางส่วนผูก singleton ⇒ ต้องมี adapter แยก state เกม (ตำแหน่งที่ต้องแก้: per-player wrapper รอบ `StartWorking` + ส่ง finish กลับผู้ขอ; static `dayTime` แชร์ทั้งโปรเซสเหมือนเดิม) | ต่ำสุด ณ วันนี้ — ต้องกำหนดกฎใครเดินวัน + เลียนลำดับ (`workingFuc` ก่อน day-tick) เอง; callback เดิมสมมติ "งานเดินวันเดียวกับปฏิทิน" |
| callback preservation | ครบโดยโครง (SendMessage จาก Update/StopWorking เท่านั้น — `time-audit-notes.md §3`) | ครบถ้ายังใช้ปั๊มเดิมเป็น executor (คิวรอง); ถ้าเรียก `StartWorking` ทีละงานจากคิว = ใช่ | ต้องเรียก ChangeHour/ChangeDay เอง — ไม่มีประกันลำดับจากเกม |
| simultaneous activities | **1 งานเท่านั้น** — คิว host เรียงงาน; UX: ผู้เล่นคนที่สองรอ; "ทับสถานะ UI ของกัน" ทางเกมเป็นไปได้เชิงโครง (StartWorking ครั้งหลังทับ — INFERRED ไม่เคยวัด) | ออกแบบให้ขนานได้เชิงตรรกะ (state ต่อ player) **แต่** executor เดินวันทางเดียว ต้องกลายเป็น "คิวแฝง" อยู่ดี หรือยอมให้วันเดินไม่ตรงตัวผู้ขอ | ขนานได้เต็มทางตรรกะ (แต่ละงานมี rate เอง) — แลกกับความเสี่ยงสูงสุด |
| save/load | เกมไม่เซฟ state ปั๊ม (§6.6) ⇒ งานหายเหมือนเกมเดี่ยว; mod เพิ่ม mod-state เฉพาะถ้าต้องการ | ต้อง persist mod-state เอง | สูงสุด (state ของ mod ทั้งหมด) |
| multiplayer authority | host เดินทุกอย่างอยู่แล้ว — เพิ่ม mirror/ack | host เป็นผู้ตัดสินชัดเจนต่อ request | host เป็นผู้ตัดสิน + ต้อง gate "ผู้เดินวัน" ให้เดียว |
| verification cost | ต่ำ — วัดซ้ำ T(N) + ยกเลิก + pause 3 ชุด (Section 9) | กลาง–สูง (adapter + ทดสอบแยกต่อชนิดงาน) | สูงสุด (ลำดับ callback ต้องพิสูจน์เทียบ native ทุกชนิดงาน) |

### 7.2 ข้อเสนอแนะ (คำแนะนำเท่านั้น — ไม่ใช่การอนุมัติ implement)

หลักฐานปัจจุบันหนุน **A (หรือ A+config เทียบเท่าทาง (3) เดิมของ followup)** เป็นจุดเริ่ม: ใช้ native pump เป็น
executor เดียว, host serialize กิจกรรมผ่านคิว, ตั้ง `dayTime` ต่อ StartWorking ตามตาราง server-config, ยกเลิกด้วย
`TryCancelWorking` (ปกติ) / `MiddleStopWork` (บังคับ — ไม่เช็ค noCancel), และเพิ่ม protocol `WORKING_STATE` +
`ACTIVITY_*` + `PAUSE/RESUME` ตามแบบร่าง `TIME_ACTION_*` ที่มีอยู่ ข้อจำกัดที่ต้องยอมรับตรง ๆ: (1) กิจกรรมขนาน
ระหว่างผู้เล่นไม่ได้ในขอบเขต A; (2) 5 ช่องว่างใน Section 1 (α–ε) ต้องปิด/มีมติก่อนเขียนโค้ดจริง; (3) Global pause
ต้อง gate สายปั๊มฝั่ง mod เองเพราะปั๊มเป็น unscaled [INFERRED→OBSERVED §2.3] — ทั้งหมดเป็นข้อจำกัดของทางเลือก
ไม่ใช่ข้อสรุปที่ผู้ใช้ยังไม่เลือก (Section 11)

---

## Section 8 — Risk register

| # | Risk | Evidence | Impact | Likelihood / confidence | Verification required | Stop condition |
|---|---|---|---|---|---|---|
| R1 | ปั๊มไม่หยุดด้วย `timeScale` — Global pause ที่ gate ไม่ครอบสายปั๊ม = งานเดิน/จบระหว่าง pause | unscaled disasm §2.3 [OBSERVED] | สูง — ผู้เล่นเห็นสถานะต่างจาก host, งานจบเอง | เกิดแน่ถ้า gate เฉพาะ timeScale (confidence: สูง) | RUNTIME ชุด V3 (Section 9) | ถ้า gate แล้วปั๊มยังเดิน → หยุดออกแบบ รายงานก่อน |
| R2 | ยกเลิกงานทำให้ resource/เงิน/บัฟเสียหายแบบคืนไม่ได้ (เงินค่าฝึกถูกหักก่อน — ไม่พบ refund) | SureStartStudySkill หักเงิน [OBSERVED §2.10]; ผลยกเลิก NOT-RUN | สูง — fairness ระหว่างผู้เล่น | UNKNOWN | V1 (TryCancel/MiddleStop บนสำเนา) | ถ้ามีผลถาวรที่ผู้ใช้ไม่ยอมรับ → งดทำ forced cancel ต่อ |
| R3 | Handler ไม่ gate (`StudyFightSelfFinish` type) ให้รางวัลบางส่วนตอนยกเลิก — อาจไม่สม่ำเสมอระหว่างงาน | §2.11 [OBSERVED] | กลาง — กติกา reward ต่องานไม่สม่ำเสมอ | พบแล้ว 1 จาก 3 handler (คาดกระจายกว้าง — INFERRED) | ถอด prologue/finish-handler เพิ่ม (static, ต่อได้) + V1 | — |
| R4 | T(N) ผิดเป้า (โมเดล 3 แบบต่างกัน ≤ 0.5 วิ ที่ N=10 แต่ไม่เคยวัดที่ rate ใหม่) | §4.4 [UNKNOWN] | กลาง — ประสบการณ์เวลาไม่ตรงตาราง | UNKNOWN | V2 (วัด 8–12 วิ/วัน + วันสุดท้าย 2×pause) | ถ้าคลาด > 2 วิ → ปรับตารางใหม่ ห้ามขยับเกณฑ์เอง |
| R5 | `StartWorking` ทับงานที่กำลังทำ (state หายโดยไม่มี finish) | disasm field init §2.6 + RUNTIME (เริ่มงานใหม่ reset ทับ) [INFERRED] | สูงใน MP — ผู้เล่นสองคนแย่งปั๊ม | พฤติกรรมทับ "ยังไม่เคยวัด" [UNKNOWN] | สแกน guard ใน `StartWorking` 6p prologue (static ต่อได้) + V4 | ถ้าทับแบบเสียหาย → คิว host ต้องบังคับเสมอ (A) หรือต้อง manager (B) |
| R6 | autosave กลางงานค้าง — state ตัดสินไม่ได้หลัง load | §2.12 + ผู้ตั้ง `needAutoSave` UNKNOWN | กลาง | UNKNOWN | หา writer `[+0x28]` (static ต่อได้) + V5 | — |
| R7 | `skipping`/pref `TestMode` ทำตารางเพี้ยนถ้า mod แตะ | ห้ามเขียน pref [OBSERVED §2.4] | กลาง — ขัดกฎข้อ 5 ของ repo | ต่ำ ถ้าไม่แตะ (ทางเลือกไม่ต้องแตะอยู่แล้ว) | — | ห้ามแตะทุกกรณี |
| R8 | ศึกเริ่มทับ modal งานโดยไม่ยกเลิก (double-state) | ไม่เคยวัด [UNKNOWN — `time-followup-notes.md §3.1`] | สูง | UNKNOWN | V6 (สั่งเริ่มศึกขณะงานเปิด บนสำเนา) | ถ้าเกิด → บังคับลำดับ "ยกเลิก→ยืนยัน→เริ่มศึก" ใน validator เสมอ |
| R9 | `ChangeHour` wrap ครั้งเดียวต่อ call — delta > 24 ทำ `hour` ค้าง ≥24 | `time-audit-notes.md §3` [OBSERVED] | กลาง (เฉพาะทาง B/C) | ตรงตาม disasm | ตรวจซ้ำใน disasm เดิม + กฎ delta ≤ 24 | — |
| R10 | เกม/เซฟ hash เปลี่ยนระหว่างอนุมัติ → anchor ทั้งหมดพ้นอายุ | VERSION.md + `time-static-notes` header | สูง (ทั้งแผน) | ต่ำ | session-start check ทุกครั้ง (กฎข้อ 9) | หยุดทันทีเมื่อ hash ไม่ตรง |

---

## Section 9 — Runtime verification plan (เอกสารเท่านั้น — ห้ามรันใน session นี้)

> ทุกชุดรันบน**สำเนาเซฟ** ตามโปรโตคอล §1.2 (save-split → preflight → เปิดเกม → วัด → restore → verify digest)
> และผ่าน `MainThreadDispatcher` + typed interop เท่านั้น; hook เป็น log-only; ตัวเลขยืนยันซ้ำกับจอทุกครั้ง

**V1 — ผลของการยกเลิก (ปิด R2/R3)** · setup: split save; เริ่มฝึก 2 วัน (ผู้ใช้กด) · inputs:
(ก) `TryCancelWorking()` ผ่าน UI ปุ่มยกเลิก, (ข) invoke `PlotController.MiddleStopWork()` (บังคับ) · observables:
`working/workFinished/leftWorkingDay/totalWorkingDay`, เงินก่อน/หลัง (`ItemListData.money`), resource สำนัก
ก่อน/หลัง, finishFuc ถูกยิงไหม (hook log-only ที่ `StopWorking` 0x9CB220), ปฏิทินเดินกี่วัน · expected: ตรงตาราง
§4.3 (gate/ไม่ gate); rollback: restore save + คืน pref จาก `prefs.before.cfg` · ผ่าน = คำตอบครบ 4 ชุด + เซฟจริง
digest ไม่เปลี่ยน

**V2 — T(N) ที่ rate ใหม่ (ปิด R4)** · setup: split; set static `dayTime` = 8/9/10/12 วิ (typed interop เขียน static
— ต้องอนุมัติ write ครั้งแรก) ต่องาน N∈{1,5,10,15} · observables: เวลารวมจริง (จาก hook `ChangeDay` leaf hits),
จำนวน leaf = N, ช่วงพักวันสุดท้าย, `hour` คงที่, `>>` ที่ threshold ใหม่ · expected: ตรงตารางเป้าหมาย ±2 วิ, ตัดสิน
โมเดล M-RUNTIME vs M-DISASM ได้ · ผ่าน/rollback เหมือน V1

**V3 — Global pause vs ปั๊ม (ปิด R1)** · setup: split; เริ่มงาน 10 วัน → `TimeScaleController.paused=true` +
`timeScale=0` (invoke — อนุมัติ) · observables: `nextDayTime/leftWorkingDay` นิ่งหรือเดิน (ตาม disasm คาด
**เดินต่อ** — ต้องมี mod-gate), เดินทาง/ศึกหยุด, transport mod เดินต่อ · ผลลบ = ปั๊มหยุดเอง (ดีกว่าคาด) —
บันทึกจริง

**V4 — StartWorking ทับงาน (ปิด R5)** · setup: split; เริ่มงาน 10 วัน → เริ่ม Wait 5 วันทับ · observables:
state ปั๊มทั้งชุด + ว่างานแรกให้ finishFuc หรือหายเงียบ + ปฏิทินรวม · expected: ตรง INFERRED (ทับ) หรือตรงกันข้าม
(บล็อก) — จริงอย่างไรรายงานจริง

**V5 — ผู้ตั้ง needAutoSave (ปิด R6)** · watch read-only ระหว่างเล่นยาว → เหตุการณ์ที่ autosave เกิด (เทียบ
`MelonLoader/Logs` เวลา) · static เสริม: สแกน store `byte [reg+0x28]` ในบริบท GameController

**V6 — เริ่มศึกขณะ modal งานเปิด (ปิด R8)** · setup: split; เริ่มงาน → NPC โจมตี/สั่งเข้าศึกผ่าน plot ปกติ ·
observables: งานถูกยกเลิก? `battleState` เปลี่ยนไหม? UI ค้าง? · ห้ามสั่งผ่าน mod ก่อนมี validator

ลำดับที่เสนอ: V1 → V2 → V3 (ต่อเนื่องได้ใน session เดียว) แล้ว V4/V5/V6 · ทุกชุดต้องมี preflight READY ก่อนเปิดเกม
และปิด session ด้วย restore + verify digest + rebaseline volatile-only หาก pref ขยับ

---

## Section 10 — Implementation plan draft (ร่าง — แยกอนุมัติได้รายขั้น, ห้ามแก้ไฟล์เหล่านี้ในงานนี้)

| ขั้น | เนื้อหา | dependencies | files likely to change | tests | acceptance criteria | rollback |
|---|---|---|---|---|---|---|
| **P-T0** อ่าน static เสริม (ยังไม่ implement) | pin ค่า noCancel ที่ site ของแต่ละ caller (ถอด prologue รายจุด), หา writer `needAutoSave`, ถอด finish-handler สายงานหลักเพิ่ม | — | ไฟล์โน้ตใหม่เท่านั้น | ตรวจซ้ำด้วย capstone เดิม | ตาราง noCancel-per-activity ครบ 47 owner ที่ pin ได้ | ไม่มี (อ่านอย่างเดียว) |
| **P-T1** activity timing model | ตาราง mapping N→rate (server config แบบ `net.cfg`-style) + โมเดล T(N) ที่ V2 ตัดสิน | V2 + มติ N นอกชุด | `S2NetConfig`-เสริม / ไฟล์ config ใหม่ของ mod, host service | unit test ตาราง (pure) | งาน N วันจบในเป้า ±2 วิ บนสำเนา; ปฏิทิน +N เป๊ะ | ลบ config/คืน dayTime 1/0.25 |
| **P-T2** per-player activity state (host) | mod-state: {ownerPlayer, kind, reqId, dayStartWorldDay, expectDayNum} + คิว serialize | P-T1; มติโมเดล serialize | ไฟล์ใหม่ใน `src/LongYinCoop/` (เช่น `ActivityManager.cs` — ชื่อร่าง) | unit test คิว/reqId dedup | งาน 2 คำสั่งซ้อนถูกเรียง; ไม่มีงานหาย | ธงปิด manager → fallback A |
| **P-T3** native callback integration | hook-lite: mirror `working/workFinished` + worldDay before/after (typed interop, read-only) ทุกเฟรม; ยิง `ACTIVITY_DONE` เมื่อ edge | P-T2 | `S2Sampler`-แยก / `S2Sender` message ใหม่ | snapshot↔จอ cross-check (แบบ ui-crosscheck เดิม) | `worldDayAfter − worldDayBefore == N` ทุกงาน 100/100 | ปิด mirror |
| **P-T4** cancellation/reward policy | คำสั่งยกเลิก host-side: guard `working && !workFinished` + noCancel; ปกติ = `TryCancelWorking`, บังคับ = `MiddleStopWork`; กติกา reward ตามผล V1 | V1; P-T2 | คำสั่งใหม่ใน host + validator | negative 20 เคส (ยกเลิกช่วงต้องห้าม/ได้) | 10/10 accept, 20/20 reject ตรงกติกา; ไม่มี double-finish | `.dll.off` |
| **P-T5** activity snapshot/reconnect | `WORKING_STATE` ใน full snapshot + resync path ขยางจาก battle | P-T3 | protocol ใหม่ (เวอร์ชัน +1), `S2HostService` | resync test รูปแบบ X4 เดิม | late join เห็นงานค้างถูกต้อง 3/3 | คง ProtoVer เดิมถ้าไม่ deploy |
| **P-T6** combat initiation + BigMap | คำสั่งโจมตีนอกศึก (validator: atAreaID/สิทธิ์) → ยกเลิกงาน (P-T4) → `PrepareBattleMap` + `fightEndCall`; ประกาศ fight บน BigMap ผ่าน state ที่ mirror มีอยู่ (`BigMapFightIcon`/event) | V6; P-T4 | คำสั่ง+message ใหม่ | integration บนสำเนา: A งาน→B โจมตี→งานยกเลิก→ศึกเริ่ม→C เห็น | flow ครบ 10 ขั้นตามโจทย์ §8 โดยไม่มี duplicate (reqId) | ปิดคำสั่ง (fail-closed) |
| **P-T7** runtime verification + regression | ชุด V1–V6 รวม + NEG20 เดิมบน build ใหม่ | ทุกขั้น | — | ตาม Section 9 | เกณฑ์ทุกขั้น + เซฟ digest คืนเป๊ะ | มาตรฐาน repo |

ข้อจำกัด: ตารางนี้เป็น**ร่างเพื่อรีวิว** — ตำแหน่งไฟล์/ชื่อคลาสใหม่เป็นข้อเสนอ ไม่ใช่ข้อตกลง; ทุกขั้นต้องรายงานแผน
ราย stage และรออนุมัติก่อนลงมือ ตามกติกาตายตัวข้อ 1

---

## Section 11 — Final decision questions

(ทุกข้อตอบพร้อมป้ายความมั่นใจ; ข้อที่ยังตัดสินไม่ได้ระบุชัดว่า "ต้องการอะไรเพิ่ม")

1. **ใช้อัตรา `dayTime` ต่อกิจกรรมโดยคง global calendar ได้มากน้อยเพียงใด?**
   ได้เต็มที่**เชิงจังหวะ** — ตั้ง static `dayTime` ก่อน StartWorking แต่ละงาน (ปิดเงื่อนไข: serialize ทีละงานเพราะ
   static แชร์โปรเซส + ปิด 4 คำถาม pause/skip/cadence) [OBSERVED กลไก + INFERRED ขอบเขต] — การตั้งต้อง "ทันก่อน
   day แรก" (nextDayTime เริ่ม 0) [INFERRED]
2. **Native pump รองรับกิจกรรมหลายผู้เล่นพร้อมกันหรือไม่?** **ไม่** — singleton + state ชุดเดียว + static rate
   [OBSERVED โครง / INFERRED ผลทับ] · ต้องแยก state ที่: per-activity record ฝั่ง mod + คิว serialize เข้าปั๊มเดียว;
   callback ที่ทำให้ยาก = `workingFuc`/`finishFuc` เป็นสตริง dispatch ผูก singleton (`SendMessage` จาก Update/
   StopWorking เท่านั้น) [OBSERVED]
3. **การเดินวันของกิจกรรมควรส่งผลต่อ global calendar อย่างไร?** ภายใต้ปั๊มเดิม: **เดินตามงาน** (กิจกรรมคือตัวเดิน
   ปฏิทิน) — ตัวเลือก "ปฏิทินเดินอัตราคงที่ แยกจากงาน" เท่ากับทาง B/C ที่ต้องเลียนลำดับเอง; **ข้อมูลที่ยังไม่พอเลือก:**
   ว่าผู้ใช้ยอมรับ "งานคนละงานผลักปฏิทินตามลำดับคิว" หรือต้องการ rate คงที่เสมอ [design decision — เปิด]
4. **งานใดปรับเวลาได้ง่าย / ยังไม่พร้อม?** ง่าย: งานที่ผ่านปั๊มแบบไม่มี workingFuc (Wait, ฝึกสาย StartStudySkill —
   วัดแล้ว) [RUNTIME]; ยังไม่พร้อม: งานที่วันมาจากสตริง plot/skill-cost และกลุ่ม research/meditation/build ที่
   เดินด้วย `leftTime` ทาง housekeeping (กลไกภายในยังไม่ถอด) [GUESS/UNKNOWN]
5. **handler ใดเสี่ยงให้รางวัล/ผลข้างเคียงผิดตอนยกเลิก?** ยืนยันแล้ว: `StudyFightSelfFinish` (ไม่ gate — exp ค้าง
   ใช้ต่อ) [OBSERVED]; ปลอดภัยที่พบ: `RealStartStudySkill`, `StudyMaxFightSkillFinish` (gate) [OBSERVED]; ที่เหลือ
   44 ตระกูลงาน = UNKNOWN จนถอด/วัดราย handler (P-T0) [UNKNOWN]
6. **MP ส่วนใดพร้อมใช้ / ต้องสร้างใหม่?** พร้อม: transport+handshake+auth (loopback/TCP, fail-closed), battle
   mirror + resync + hash audit, spike move/attack (ในศึก), world viewer (S1), IPC, pref guard — [OBSERVED โค้ด];
   ต้องสร้างใหม่: activity state/ownership, activity messages, validator นอกศึก, pause gate, world snapshot
   late-join, combat initiation นอกศึก, BigMap fight announcement [OBSERVED absence]
7. **ข้อมูลขั้นต่ำที่ต้อง sync สำหรับ activity start/progress/complete/cancel?**
   start: {reqId, ownerPlayer, kind, dayNum, worldDayAtStart, actorHeroID, areaID} · progress: {leftWorkingDay,
   worldDayNow} (จาก `worldTime` เสมอ — ห้าม `totalWorkingDay`) · complete/cancel: {reqId, outcome:
   complete|cancelled, worldDayBefore/After, workFinished, resourceDelta(ถ้าวัดได้)} — สอดคล้องแบบร่าง
   `TIME_ACTION_*` เดิม [INFERRED จาก field ที่อ่านได้ typed ทั้งหมด — area-notes P1-5 + `area-notes.md` ท้ายไฟล์]
8. **runtime verification ชุดเล็กที่สุดที่ลดความเสี่ยงการเริ่ม implement คือ?** V1 (ยกเลิก) + V2 (T(N) ที่ rate ใหม่)
   สองชุด — ปิด R2/R3/R4 ซึ่งตัดสินทั้งกติกา reward และตารางเวลา (Section 9)
9. **เงื่อนไขที่ต้องปิดครบก่อนอนุมัติ implementation?** (ก) V1/V2 มีผลและผู้ใช้รับกติกา; (ข) มติ 3 ข้อ: โมเดล
   serialize (A/B/C), pause-policy ของคำสั่ง, กฎ N นอกชุด; (ค) baseline เซฟใหม่หลัง divergence 5 ไฟล์ (ค้างอนุมัติ
   เดิมของ repo); (ง) P-T0 คืนตาราง noCancel/reward ที่รู้จริงอย่างน้อยกลุ่มงานหลัก
10. **(เสริมจากหลักฐาน) travel/quick-travel เป็นกิจกรรมปั๊ม** — `PlayerQuickTravel` ใช้ StartWorking 6p
    [OBSERVED-name] ⇒ การเดินทางแบบไวแย่งปั๊มกับงานอื่น — ต้องตัดสินว่าใน MP travel ของผู้เล่นหลายคนผ่านคิวเดียว
    หรือไม่ [design decision — เปิด]
11. **(เสริม) ธง `skipping` ค้าง true หลังจบงาน** [RUNTIME] — ตราบใดที่ไม่ reset ตอน StartWorking (disasm §2.6
    **ไม่พบ**การเขียน 0x62 ตอนเริ่มงาน) state เศษจากงานก่อนหน้าอาจทำให้งานใหม่เริ่มที่ threshold ×0.2/×0.5
    ทันที [INFERRED — สมมติฐานที่ต้องตรวจ: ธงค้าง + StartWorking ไม่เคลียร์ = งานใหม่เดินเร็วเกิน] — ต้อง pin ใน
    P-T0 ก่อนใช้ skip ใน MP

---

## สรุปปิดท้าย — unresolved decisions และ exact evidence gaps

**Design decisions ที่ยังเปิด (ของผู้ใช้):** โมเดล serialize (A/B/C §7.2) · ความหมาย (1)/(2)/(3) ของ Section 6.1 ·
pause-policy ของคำสั่งขณะ pause · กฎ N นอก {1,5,10,15} · ระดับ "ความยุติธรรม reward บางส่วน" ที่ยอมรับ ·
travel ผ่านคิวเดียวหรือไม่

**Exact evidence gaps (เรียงตามการใช้ตัดสิน):**

1. ผล `TryCancelWorking`/`StopWorking(false)` ต่อ เงิน/ทรัพยากร/buff รายกลุ่มงาน — NOT-RUN (V1)
2. T(N) จริงที่ `dayTime` 8–12 วิ + ลำดับ `2×pauseTime` วันสุดท้าย — NOT-RUN (V2); โมเดล M-PROMPT (N+2) ไม่มี
   หลักฐานรองรับจาก disasm ปัจจุบัน
3. ค่า noCancel ที่ call site จริงของแต่ละ caller (ถอด prologue รายจุดได้ static — P-T0) + รายการ finish-handler
   ครบชุด (string dispatch อยู่ใน data/scene — นอก call graph)
4. ผู้ตั้ง `needAutoSave` + พฤติกรรมงานค้างข้าม save/load — NOT-RUN (V5) + งานเดิม `time-notes.md §8 ข้อ 4`
5. "เริ่มศึกขณะ modal งานเปิด" ไม่เคยวัด — NOT-RUN (V6); wiring "เริ่มศึก ⇒ ยกเลิกงาน" ไม่มีในเกมและ mod
   [OBSERVED absence]
6. ผลของการเรียก `StartWorking` ทับขณะ working=true — UNKNOWN (V4)
7. ลำดับเงื่อนไขเต็มของ `ChangeDay()` leaf (housekeeping) — มีรายการ call ครบ แต่ control flow ยังถอดไม่จบ
   (`bodies/GameController_ChangeDay_leaf.txt` คงอยู่ — ถอดต่อได้ static)
8. สัมประสิทธิ์ hour-ต่อ-ระยะของการเดินทาง + กลไก P4 (เมนู Esc) — ค้างเดิม ไม่เกี่ยวตรงกับ activity แต่กระทบ
   interleave วัน

**purity ของรอบนี้:** อ่านอย่างเดียวทั้งหมด — ไม่มีการรันเกม, ไม่มี invoke/hook, ไม่มีการเขียนไฟล์ใดนอกโฟลเดอร์
`evidence/TIME-T5-20261028-003723/` (ไฟล์เดียว: รายงานนี้), ไม่มีการเปลี่ยน git ทั้งสอง repo (ตรวจ `git status`
หลังงานแนบในข้อความส่งมอบ)
