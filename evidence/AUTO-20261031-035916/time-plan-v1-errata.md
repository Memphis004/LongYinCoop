# time-plan-v1-errata — รีวิว `time-plan-draft.md` ส่วน V1 (Phase A · Gate 0 ฝั่ง V1) · label `AUTO-20261031-035916`

> งานเอกสารอย่างเดียว — **ไม่แก้ `time-plan-draft.md`** (ตามมติ [Gate 0 ฝั่ง V1] 2026-10-31: "อนุมัติ time-plan-draft.md
> ส่วน V1 เป็นแผนทำงาน โดยไม่แก้ตัวแผน — ที่ล้าสมัยให้เขียนเป็นไฟล์ errata แยก") · ทุกจุดที่ล้าสมัย/ตีความอยู่ในไฟล์นี้
> · ป้าย: **STALE** = ข้อเท็จจริงประกอบล้าสมัย (หลักการของแผนยังยืน) · **NOTE** = การตีความ/ข้อจำกัดที่ต้องประกาศ
> ก่อนใช้ · **BLOCKER** = กระทบความถูกต้องของการวัด (ต้องหยุด) · บรรทัดอ้างจาก `plans/time-plan-draft.md` ณ
> mod repo HEAD `69c7a8d` · มติที่แนบวลี "เตรียม V1 ให้พร้อมรัน": [D1] [D2] [Gate 0 ฝั่ง V1] [Gate 3] [V1-deploy] [POPUP]

## session-start ณ รีวิว (ผ่านครบ — วัดจริง 2026-10-31 03:59)

เกมไม่รัน (tasklist + save-split) · hash เกม 2 ไฟล์ตรง `VERSION.md` (`33c56def…` / `451311d4…`) ·
`Mods\LongYinCoop.dll` = `6a94abb5…` / `.pdb` = `856d3bed…` · `net.cfg` = `12541f34…` (444 B) ·
`MelonPreferences.cfg` = `976ccfb9…` (ตรง record; ค่า key ตรง: `Enabled=true`, `AllowRemoteInput=false`,
`Sp5T0=false`, `TimeV2a=false` — `pref_key_check.py` **PASS 4/4**) · เซฟ `Save` 57 ไฟล์ digest `d3f7cb2fcae2`
ตรง baseline `20261031-001811` + `leftovers: none` + mounted EXACT 2 ไฟล์ · HEAD: mod `69c7a8d` /
game `944ceda` · ไม่มี session อื่นเขียน repo (มี pwsh interactive ×3 + `python mcp_server.py --stdio` —
MCP server ไม่เขียน repo) · worktree มี M ค้างจาก session ก่อน: mod `context.txt`, `decisions-pending.md`
(ไม่แตะ) · game repo: `D battle-notes.md`/`worldmap-notes.md`/`.agents/skills/caricature-apose-prompt/**`,
`M il2cpp-frida-mcp` (ไม่แตะ)

## สรุปผลรีวิว (อ่านก่อน)

- **BLOCKER = 0 ข้อ** — ไม่มีข้อใดกระทบความถูกต้องของการวัด V1 ⇒ เดินหน้า Phase B ได้ตามมติ [Gate 3]
- **STALE = 3 ข้อ** (baseline chain ×2 จุด + anchor ตอนร่าง — หลักการทุกข้อยังใช้ได้)
- **NOTE = 7 ข้อ** (การตีความ "จุดยิง finishFuc" · ชื่อ field จริง · D1 ⇒ TC-3a/3b NOT-RUN ·
  preflight-launch · กฎ POPUP · TimeV2a ร่วม · ขอบเขตการอ่านเอกสาร)
- RVAs ของเป้า hook ตรง dump.cs ทุกตัว (ตารางด้านล่าง) · field ที่ probe ต้องอ่าน**มีจริงใน interop
  assembly ครบ** (สแกน #Strings ดิบของ `MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll` 2026-10-31,
  sha256 `812d8953899d3902…` — ไม่โหลดแอสเซมบลี): `WorkingUIController`, `finishFuc`, `workingFuc`,
  `workName`, `StopWorking`, `MiddleStopWork`, `TryCancelWorking`, `ItemListData`, `resourceStore`,
  `FinishWorkInProductionBuilding`, `RealStartStudySkill` — ทุก token พบ ≥2 ⇒ **ไม่มี field ที่ต้องอ่าน
  ด้วย raw offset** (เงื่อนไขหยุดข้อ "field จำเป็นอ่านด้วย typed interop ไม่ได้" ไม่เกิด)

## ตารางตรวจ RVA เป้า hook เทียบ dump.cs (DUMP — `Il2CppDumper/dump.cs` วัด 2026-10-31)

| เป้า (แผน §D.1 ข้อ 5) | RVA ในแผน | dump.cs | ผล |
|---|---|---|---|
| `WorkingUIController.StopWorking(bool finished)` | 0x9CB220 | บรรทัด 392738–392739 — `RVA: 0x9CB220` `public void StopWorking(bool finished)` | ตรง |
| จุดยิง finishFuc (ใน body StopWorking) | 0x9CB2C2 / 0x9CB2FA | (คำสั่งกลาง body — จาก disasm §B.3 ไม่ปรากฏใน dump.cs) | ดู E3 |
| `PlotController.MiddleStopWork()` | 0x54EEA0 | บรรทัด 384076–384077 — `RVA: 0x54EEA0` `public void MiddleStopWork()` (instance method ของ `PlotController`) | ตรง |
| `WorkingUIController.TryCancelWorking()` (อ้าง §B.3) | 0x9CB330 | บรรทัด 392716–392717 — `RVA: 0x9CB330` | ตรง |

field ของ `WorkingUIController` (dump.cs TypeDefIndex 8652): `working` 0x48 · `leftWorkingDay` 0x4C ·
`totalWorkingDay` 0x50 · `nextDayTime` 0x54 · `leftPauseTime` 0x58 · `resourceNum` 0x5C · `workFinished` 0x60 ·
`noCancel` 0x61 · `skipping` 0x62 — ตรงช่วง "0x48–0x62" ของ §D.1 ข้อ 7 · field ชื่องาน: `workName` 0x20 ·
`workingFuc` 0x28 · `workingFucParam` 0x30 · `finishFuc` 0x38 · `finishFucParam` 0x40 (ดู E2)

## รายการ errata

| # | ป้าย | ที่อยู่ (บรรทัด) | ข้อความเดิม (ตัดย่อ) | ข้อเท็จจริงปัจจุบัน | ผลต่อ V1 |
|---|---|---|---|---|---|
| E1 | STALE | 255–256 (§C.3 D9), 309–315 (§D.1 ข้อ 2), 524–531 (§G.2 ข้อ 1) | อ้าง baseline `4226730f66ff` / "divergence 5 ไฟล์" / มติ D9 2026-10-28 ทาง (ก) = `df8e5a11beed…` @ `20261028-071340` | สายโซ่ baseline เดินต่อด้วยมติอนุมัติ: … → `7e1e65e52659…` (`20261030-060854`) → **`d3f7cb2fcae297f0e4b1cfb61630c4d19adb2e848dd6dd2c2ebf2346afe91b0f` @ `coop-test/baselines/20261031-001811` (ปัจจุบัน — มติผู้ใช้ 2026-10-31, rebaseline volatile-only, `REBASELINE.json` `volatile_only:true changed=[PlayerprefData.dat]`)** | ทุก split ของ V1 ใช้ `--expected-baseline coop-test/baselines/20261031-001811/savehash.real.before.txt` ตรง ๆ · หลักการของแผนคงเดิมทุกข้อ: ตรวจ EXACT + snapshot ก่อนทดสอบต่อเคส + **ห้ามอ้าง fixture ตรง baseline เดิมยุคใดแบบย้อนหลัง** + `leftovers: none` |
| E2 | STALE | 77, 107–110 (§B.1/§B.3), 318–320 (§D.1 ข้อ 5) | ใช้คำ `callFuc`/`callFucParam`/`finishCallFuc`/`finishCallFucParam` เป็นชื่อ field | ชื่อ **field** จริงตาม dump.cs/interop = `workingFuc` (0x28) / `workingFucParam` (0x30) / `finishFuc` (0x38) / `finishFucParam` (0x40) — ชื่อ `callFuc`/`finishCallFuc` เป็นชื่อ**พารามิเตอร์**ของ `StartWorking` overload เท่านั้น (dump.cs บรรทัด 392725/392729) | probe อ่านด้วย typed interop ชื่อ field จริง: `w.workName`, `w.workingFuc`, `w.finishFuc` (ค่าใน NDJSON ใช้ key `workName`/`workingFuc`/`finishFuc` ตาม dump) — ไม่มีผลต่อตรรกะของแผน |
| E3 | NOTE | 318–320 (§D.1 ข้อ 5) | "จุดยิง finishFuc 0x9CB2C2/0x9CB2FA (log ชื่อ handler + ค่า workFinished ขณะยิง)" | **จุดยิงเป็นคำสั่งกลาง body ของ `StopWorking` (SendMessage call) — Harmony แพตช์ได้เฉพาะขอบเขต method ไม่ได้แพตช์กลาง body** ⇒ การทำจริงตามข้อกำหนด [Gate 3] "Harmony prefix/postfix log-only": hook `StopWorking` ด้วย prefix + postfix + finalizer (void — คืน exception เดิมตาม contract) · event `stopworking.enter` บันทึก `finished` (arg) + **pre-state ครบ** (รวม `finishFuc` = ชื่อ handler ที่กำลังจะถูกยิง, `workFinished` ค่าก่อนเขียน) · ค่า **"workFinished ขณะยิง"** = ค่า `finished` arg — จากลำดับ static §B.3 (`workFinished=finished` ที่ 0x9CB25F **ก่อน** fire 0x9CB2C2/0x9CB2FA) = ป้าย **SPEC/[OBSERVED เชิงโค้ด] ไม่ใช่การอ่าน ณ คำสั่งยิง** · event `stopworking.exit` ยืนยันหลังยิง (post-state `workFinished == finished`, `working == 0`) | ประกาศก่อนใช้ตามข้อ (ก) ของ [Gate 3] — analyzer ตีความ `stopworking.enter.fireWorkFinished` ในฐานะนี้เสมอ ห้ามอ้างเกิน |
| E4 | NOTE | 332–333 (§D.2 TC-3a/3b), 322 (§D.1 ข้อ 6), 370–371 (§D.5) | TC-3a/3b = invoke `MiddleStopWork` เมื่อมติ D1 อนุญาต | **มติ [D1] 2026-10-31 = "ปุ่ม UI เท่านั้น"** (decisions-pending.md บรรทัด 40) ⇒ **TC-3a/TC-3b = NOT-RUN/limitation ของ V1 ทุกรายงาน** (§D.5 ครอบไว้แล้ว) · hook `MiddleStopWork` **ยังติด** (อนุมัติใน [Gate 3]) — ผล `middlestopwork.enter` ใน TC-1/TC-2 ที่เกิดหลัง dialog ยืนยันของผู้ใช้ = หลักฐาน verify dispatch จาก plot-choice (UNKNOWN ของ §B.3 (ก)) ด้วยปุ่ม UI ล้วน · TC-4b (ยกเลิกงานที่สองผ่าน UI) ยังทำได้ตามแผน | RUNBOOK-V1 ใส่ TC-3a/3b เป็น NOT-RUN พร้อมเหตุผลมติ D1 ทุกที่ที่อ้าง |
| E5 | NOTE | 306 (§D.1 ข้อ 1) | "`save-split preflight` = READY ก่อนเปิดเกมเสมอ" | ในบริบท V1 (writer อยู่ใน mod DLL — ไม่มี recorder ภายนอก) เครื่องมือที่ใช้คือ **`preflight-launch`** (ตาม RUNBOOK-V2A ขั้น 3 + คำสั่งเตรียมรอบนี้: "preflight-launch READY (ไม่ใช้ preflight เดิม; ห้าม --dry-run)") — เก็บ stdout ลง `evidence\<LABEL>\preflight-launch.txt` เป็นหลักฐาน | RUNBOOK-V1 ใช้ `preflight-launch` + hand-check pointer/touch ก่อนเปิดเกมทุกรัน |
| E6 | NOTE | (แผน §D ไม่มีกฎ popup) | — | **[POPUP นโยบาย] แนบมติ 2026-10-31**: popup/ปั๊มค้างก่อนจุดผู้ใช้กดยกเลิก/ก่อนงานจบ = รันนั้น **CONTAMINATED** (restore แล้วเริ่มใหม่บน split ใหม่) · TC-4 เจอ = เริ่ม TC-4 ใหม่ทั้งชุด · รันเสียติดกัน 3 ครั้งของ TC เดียวกัน = หยุดรายงาน · ผู้ใช้จดชนิดงานทุกรัน · popup หลังจุดวัด (งานจบแล้ว) = บันทึกแต่ไม่ทิ้งรัน | RUNBOOK-V1 ใส่กฎครบ + analyzer มีกฎ gap > 2 s (เชิงเครื่องมือ เทียบแนว GAP_CONTAMINATION_MS ของ V2-A) เป็นสัญญาณตรวจ ส่วนการทิ้งรัน = ตัดสินจากพฤติกรรมจอตามนโยบาย |
| E7 | NOTE | 323–324 (§D.1 ข้อ 7) | "อ่าน state ทั้งหมดผ่าน typed interop: WorkingUIController.Instance fields 0x48–0x62, ItemListData.money, ForceData.resourceStore, WorldData worldTime/hour" | ยืนยัน interop มีครบ (สแกน #Strings — หัวข้อ "สรุปผลรีวิว") · **ขอบเขตการอ่านของ probe รอบนี้** = pump fields 0x48–0x62 + `workName`/`workingFuc`/`finishFuc` **ณ hook events เท่านั้น** + TimeV2a (ถ้าเปิด) อ่านต่อเฟรมแบบเดิม — **เงิน/ทรัพยากร = แบบฟอร์มจดของผู้ใช้** ตามคำสั่งเตรียม ("ข้อมูลที่ต้องอาศัยตัวเลขบนจอ (เงิน/ทรัพยากร) = ผู้ใช้จดตามแบบฟอร์มใน RUNBOOK ไม่ให้ analyzer เดา") — probe build นี้**ไม่อ่าน** `ItemListData.money`/`ForceData.resourceStore` (ลดพื้นผิว; เก็บไว้เป็นทางเลือกอนาคต) | ไม่มีผลต่อ R1/R3 (ใช้ hook + TimeV2a) · R2 ใช้ handler name + `totalWorkingDay` จาก event + แบบฟอร์ม dialog ของผู้ใช้ |
| E8 | STALE | 11 (anchor ตอนร่าง) | "anchor ตอนร่าง: mod repo HEAD `df46de1` · game repo HEAD `c672dc2`" | ณ session เตรียมนี้: mod HEAD `69c7a8d` · game HEAD `944ceda` — เป็น anchor ประวัติของวันร่าง (2026-10-28) ไม่ใช่ข้อผิดพลาด | ไม่มี — ข้อมูลประกอบเท่านั้น |
| E9 | NOTE | 3 (สถานะหัวแผน), 271–272 (§D เงื่อนไขเริ่ม) | "ส่วน V1 / Gate 2–4 ยังรอ gate ตาม §F" / "เริ่มได้เฉพาะหลัง Gate 3" | มติแนบวลีเตรียมครบแล้วตาม TRIGGERS.md (V1 ต้องมี D1 + D2 + Gate 3): **[D1]** ปุ่ม UI เท่านั้น · **[D2]** ยอมรับ per-day credit ถาวร (รันบนสำเนาเสมอ) · **[Gate 0 ฝั่ง V1]** อนุมัติส่วน V1 เป็นแผนทำงาน (ไม่แก้ตัวแผน) · **[Gate 3]** อนุมัติเขียน probe log-only ตาม §D.1 ข้อ 5 พร้อมข้อกำหนด (ก)–(จ) · **[V1-deploy]** build DLL เป็น artifact (compile-only) — **การ deploy จริง/เปิดเกม = วลี `เริ่ม V1 session` เท่านั้น** | สิทธิ์รอบนี้ = เตรียมอย่างเดียว ไม่มีสิทธิ์รัน — RUNBOOK-V1 ยังต้องรอวลี |
| E10 | NOTE | (นอกแผน — เอกสารอ่านก่อน) | คำสั่งเตรียมระบุ "รายงานทบทวน V1 ของรอบ AUTO-20261031-031942" | **ไม่พบไฟล์รายงานทบทวน V1 แยกของรอบ `AUTO-20261031-031942`** — รอบนั้น (docs-only) ผลิต: `v2a-report.md` §9 (สถานะปิดงาน V2-A PROVISIONAL, commit `9663a21`) + `VERSION.md` หัวข้อ V2-A (commit `22f0d3e`) เท่านั้น (ตรวจ git show ทั้งสอง commit + ค้นทั้ง repo 2026-10-31) — **อ่านครบแล้วทั้งสอง** และนำข้อจำกัด V2-A (popup 2/9 รัน, UNRESOLVED_OFFSET, A4 ไม่ได้วัด) มาประกอบ RUNBOOK-V1 แล้ว | ไม่กระทบความถูกต้องการวัด — บันทึกไว้เพื่อความโปร่งของการอ่านเอกสาร |

## ประเมินการเปิด TimeV2a ร่วมกับ probe (ตามคำสั่งเตรียม — ห้ามแก้ TimeV2a ห้ามเปิดใช้โดยไม่รายงาน)

**ข้อดี (ที่กระทบ R1/R2/R3 ตรง):**
1. **R3 วัดได้จริงระหว่างงานที่สอง** — hook เห็น `workFinished` เฉพาะตอน StopWorking/MiddleStopWork;
   latch "ค้าง = 1 ตลอดงานที่สอง" (§D.4) ต้องอ่านระหว่างงานเดิน = ต้องมี TimeV2a (event `day_tick`/`pause_end`
   พก field `workFinished` ทุกบรรทัด)
2. **gap/CONTAMINATED อัตโนมัติ** — timeline ต่อเฟรมของ TimeV2a ใช้กฎ gap > 2 s ตรวจ popup ค้าง
   (บทเรียน V2-A: A3-r2/A2-r3) แทนการพึ่งสายตา
3. **cross-check เหตุการณ์ 2 แหล่ง** — `work_end`/`work_abort` ของ TimeV2a เทียบลำดับกับ
   `stopworking.enter/exit` ของ probe (ผลต่างระหว่างจบ/ยกเลิกเกิดที่ handler ปลายทาง — §B.3) และ
   `work_start` ของ TC-4 ระบุงานที่สองชัด
4. ไม่ต้อง build กลไก timeline ใหม่ใน probe — probe คงขอบเขต 3 เป้า hook ตาม [Gate 3] เป๊ะ

**ความเสี่ยง/ต้นทุน:**
1. **สอง pointer file ต่อรัน** (`time-v1.out` + `time-v2a.out`) — โอกาสลืมสลับ/ตั้ง → RUNBOOK ใส่ hand-check
   ทั้งคู่ด้วย `od -c` + touch ไฟล์ปลายทางทั้งคู่ก่อน preflight ทุกรัน (fail-closed ทั้งสองฝั่ง: ไม่มี pointer =
   ไม่เขียน)
2. สอง writer เปิดพร้อมกัน — I/O ต่ำมาก (TimeV2a เขียนเฉพาะ event จังหวะเปลี่ยน, probe เขียนเฉพาะตอน
   hook ยิง — งานหนึ่ง ~10–30 บรรทัด) เคยรันร่วมกับโหลดจริงแล้วใน V2-A (9 รัน) ไม่มีผลวัดผิดปกติ
3. TimeV2a detector อ่าน pump ทุกเฟรม — เป็นโค้ดเดิมที่ไม่แก้ (ตรวจ `git diff` ณ commit) — ปฏิสัมพันธ์กับ
   hook: ทั้งคู่อ่าน field เดียวกันคนละจังหวะ ไม่มีฝั่งใดเขียน state เกม (อ่านอย่างเดียวทั้งคู่)

**ข้อเสนอ (รายงานตามคำสั่ง — การเปิดจริงเกิดใน `เริ่ม V1 session` ตาม RUNBOOK):** เปิด `TimeV2a=true` ร่วมกับ
`TimeV1a=true` **ทุกรัน V1** · NDJSON แยก 2 ไฟล์ต่อรัน (`<เคส>-r<รอบ>.ndjson` ทั้งคู่) · analyzer รับทั้งสองไฟล์

## ผลต่อการเดินหน้า

BLOCKER = 0 ⇒ ดำเนิน Phase B (probe + เทสต์ offline) ต่อตามมติ [Gate 3] — โดยประกาศใช้: ชื่อ pref ใหม่
**`TimeV1a`** (default false — ข้อ (ก) ของ [Gate 3]) · namespace `LongYinCoop.TimeV1` · pointer file
`time-v1.out` · โครง event NDJSON ตาม RUNBOOK-V1.md (Phase D) · **ห้ามแก้ `S2/**`, `SP5/**`, `TimeV2a/**`**
(ข้อ (จ) ของ [Gate 3]) — ตรวจด้วย `git diff --stat` ราย commit
