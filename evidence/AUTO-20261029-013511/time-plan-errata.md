# time-plan-errata — รีวิว `time-plan-draft.md` (Gate 0 มติ [V2A-1]) · label `AUTO-20261029-013511`

> งานเอกสารอย่างเดียว — **ไม่แก้ `time-plan-draft.md`** ตามมติ [V2A-1] ข้อผิดพลาด/จุดที่ล้าสมัยที่พบอยู่ในไฟล์นี้
> · ป้าย: **STALE** = ข้อเท็จจริงประกอบล้าสมัย (หลักการของแผนยังยืน) · **NOTE** = ข้อจำกัด/ข้อสังเกตที่ต้องประกาศก่อนวัด
> · **BLOCKER** = กระทบความถูกต้องของการวัด (ต้องหยุด) · บรรทัดอ้างจาก `time-plan-draft.md` ณ mod repo HEAD `3ac0761`
> · session-start ผ่านครบ (hash เกม/Mods/pref/net.cfg/เซฟ EXACT baseline `20261028-071340` · เกมไม่รัน · HEAD+tags ตรง)

## สรุปผลรีวิว (อ่านก่อน)

- **BLOCKER = 0 ข้อ** — ไม่มีข้อใดกระทบความถูกต้องของการวัดชุด A ⇒ เดินหน้า Phase B ได้ตามมติ [V2A-4]
- **STALE = 4 ข้อ** (ทั้งหมดเป็นการอ้าง baseline ยุค S2a-3 ที่ผ่านการ rebaseline อนุมัติไปแล้ว — หลักการของแผนทุกข้อยังใช้ได้)
- **NOTE = 4 ข้อ** (ข้อจำกัดของ analyzer + การประกาศตามสเปกที่ต้องทำก่อนใช้)
- ไฟล์อ้างอิงทั้ง 11 รายการในหัวแผน (บรรทัด 14–19) **มีจริงครบ** (ตรวจ `[ -e ]` รายไฟล์ 2026-10-29)

## รายการ errata

| # | ป้าย | ที่อยู่ (บรรทัด) | ข้อความเดิม (ตัดย่อ) | ข้อเท็จจริงปัจจุบัน | ผลต่อชุด A |
|---|---|---|---|---|---|
| E1 | STALE | 255–256 (§C.3 D9) | "`Save` diverge จาก baseline ที่อนุมัติ `4226730f66ff` 5 ไฟล์ (SaveSlot8 2026-10-24 02:15 + `PlayerprefData.dat`) — ต้องเลือก (ก) rebaseline … หรือ (ข) คง baseline เดิม + บันทึก divergence … **ห้ามอ้างว่า fixture ที่ diverge ตรง baseline เดิมแบบ EXACT**" | สายโซ่ baseline เดินต่อแล้วด้วยมติอนุมัติ: `4226730f66ff` (S2a-3) → `7332fc93eb98` → `b7f67154cff6` → `58883949…` → **`df8e5a11beed…` @ `coop-test/baselines/20261028-071340` (ปัจจุบัน — มติ [G-2] 2026-10-28, rebaseline volatile-only)** · รายละเอียดครบใน `VERSION.md` หัวข้อ "Baseline ที่อนุมัติปัจจุบัน" | ใช้ตามมติ [V2A-2]: `split --expected-baseline coop-test/baselines/20261028-071340/savehash.real.before.txt` + **snapshot เซฟก่อนทดสอบทุกเคส** · หลักการ "ห้ามอ้าง fixture ตรง baseline เดิม EXACT" ของแผนคงใช้เหมือนเดิม |
| E2 | STALE | 309–315 (§D.1 ข้อ 2) | "ทาง (ข) … ตรวจ `Save` (fixture) ตรง snapshot ก่อนทดสอบ … (รายการ divergence 5 ไฟล์คงอยู่ตามเดิม) — ห้ามตรวจ fixture เทียบ original baseline `4226730f66ff` แบบ EXACT" | หลักการตรวจด้วย snapshot ก่อนทดสอบยังยืน — แต่ "รายการ divergence 5 ไฟล์" และการอ้าง `4226730f66ff` เป็นข้อเท็จจริงยุค S2a-3 แล้ว | เกณฑ์ Gate 1 ของชุด A = ตรวจ `Save` EXACT ตาม baseline ปัจจุบัน `df8e5a11beed…` (E1) + snapshot ต่อเคส (กฎ 2 §D.0 ครอบอยู่แล้ว) |
| E3 | STALE | 524–531 (§G.2 ข้อ 1) | "ทาง (ข) คง baseline เดิม ⇒ fixture ตรง snapshot ก่อนทดสอบของ session นั้น … **ห้าม**อ้างว่า fixture ตรง original baseline `4226730f66ff` แบบ EXACT" | หลักการเดียวกับ E2 — การ restore ปลาย session ตรวจคืนด้วยเครื่องมือ (`save-split status/restore`) ซึ่งตรวจกับ baseline ปัจจุบัน `df8e5a11beed…` อยู่แล้ว | ไม่เปลี่ยนขั้นตอน rollback — ใช้ `--expected-baseline` ไฟล์ `20261028-071340` ให้ตรงกับ split ทุกครั้ง |
| E4 | STALE | 11 (anchor ตอนร่าง) | "anchor ตอนร่าง: mod repo HEAD `df46de1` · game repo HEAD `c672dc2`" | ณ session เตรียมนี้: mod HEAD `3ac0761` (= tag `stage-S2a-4`) · game HEAD `39c5403` — เป็น anchor ประวัติของวันร่าง (2026-10-28) ไม่ใช่ข้อผิดพลาด | ไม่มี — ข้อมูลประกอบเท่านั้น |

## การตรวจ D9 กับมติ [V2A-2] (รายงานตามคำสั่ง — ก่อนเขียน errata)

**ข้อความ D9 ตรงตัว (บรรทัด 255–256):**

> "**D9** | สถานะ baseline/fixture เซฟก่อน V1 — ตอนนี้ `Save` diverge จาก baseline ที่อนุมัติ `4226730f66ff` 5 ไฟล์ (SaveSlot8 จากการเซฟของผู้ใช้เอง 2026-10-24 02:15 + `PlayerprefData.dat`) — ต้องเลือก **(ก) rebaseline**: บันทึก manifest/digest ใหม่แล้วใช้ manifest นั้นเป็นเกณฑ์ preflight/rollback ทั้งหมด หรือ **(ข) คง baseline เดิม + บันทึก divergence**: แยก "original approved baseline" (`4226730f66ff`) ออกจาก "approved test fixture ปัจจุบัน" ระบุรายการ divergence ที่อนุมัติ และใช้เกณฑ์ตรวจคืน fixture ให้ตรงกับ **snapshot ก่อนทดสอบ** (§D.1/§G.2) · **ห้ามอ้างว่า fixture ที่ diverge ตรง baseline เดิมแบบ EXACT** · การบันทึก/ใช้ manifest ทั้งสองทางต้องเป็นไป
> ตามนิยาม "ห้ามแตะเซฟจริง" (§D.0) · ถ้ายังไม่มีมติ D9 ⇒ Gate 1 ไม่ผ่าน ห้ามเปิดเกม (ค้างอนุมัติเดิมของ repo) | Gate 1 |"

**การตีความของ session นี้:** มติ [V2A-2] = เลือกแนว (ข) — "ห้ามอ้าง fixture ตรง baseline เดิม `4226730f66ff` · ใช้ baseline ปัจจุบัน `coop-test/baselines/20261028-071340/savehash.real.before.txt` (digest `df8e5a11beed…`) กับ `--expected-baseline` และต้อง snapshot เซฟก่อนทดสอบทุกเคส"

**ผลการเทียบ: ข้อความ D9 ในแผน ไม่ขัดกับมติ [V2A-2]** — เหตุผล 3 ข้อ:

1. ข้อห้ามกลางของ D9 ("ห้ามอ้างว่า fixture ที่ diverge ตรง baseline เดิมแบบ EXACT") **ตรงกับ**ข้อห้ามของมติ ("ห้ามอ้าง fixture ตรง baseline เดิม 4226730f66ff") — เป็นหลักการเดียวกัน
2. แนว (ข) ของ D9 นิยามไว้เองว่าต้อง "แยก original approved baseline ออกจาก approved test fixture ปัจจุบัน + ใช้เกณฑ์ตรวจคืน fixture ให้ตรงกับ snapshot ก่อนทดสอบ" — มติ [V2A-2] ทำตามรูปแบบนี้เป๊ะ: baseline ที่ "อนุมัติปัจจุบัน" = `df8e5a11beed…` @ `20261028-071340` (ซึ่งถูกสร้างด้วยมติอนุมัติแยก [G-2] 2026-10-28 — ไม่ใช่การ rebaseline กลบของ session นี้) + snapshot ต่อเคส
3. ส่วนที่ล้าสมัยของ D9 มีเฉพาะ**ข้อเท็จจริงประกอบ** (ตัวเลข "5 ไฟล์" และการอ้าง `4226730f66ff` เป็น baseline ตั้งต้น — เป็นสภาพ ณ วันร่าง 2026-10-28 ก่อนมติ [G-2]) — ไม่ใช่ข้อขัดแย้งเชิงหลักการ

จึง**ไม่เข้าเงื่อนไขหยุด** ("ถ้าข้อความ D9 ขัดกับมตินี้ = หยุดรายงาน") — บันทึกเป็น E1/E2/E3 STALE ข้างบน

## NOTE — ประกาศที่ต้องทำก่อนใช้ (มติ [V2A-4] ให้เสนอชื่อก่อนใช้)

| # | เรื่อง | ค่าที่ประกาศ |
|---|---|---|
| N1 | **ชื่อ pref ใหม่** (default `false`) | **`TimeV2a`** (หมวด `[LongYinCoop]` — `CoopPrefs`) · `false` = sampler ไม่อ่าน/ไม่เขียนอะไรเลย (no-op — ตรวจด้วยโค้ด ระบุบรรทัดใน provenance ขั้น C4) |
| N2 | **กลไกชี้ไฟล์ NDJSON** | pointer file `UserData\LongYinCoop\time-v2a.out` — บรรทัดแรกที่ไม่ว่าง = path เต็มของไฟล์ NDJSON ปลายทาง (เซสชันจริงจะชี้เข้า `LongYinCoop\evidence\<label>\time-v2a.ndjson` ผ่าน junction) · fail-closed: ไม่มีไฟล์/ว่าง/นามสกุล ≠ `.ndjson` = sampler ไม่เขียนเลย · ไฟล์นี้ **ไม่ได้สร้างในรอบเตรียม** (สร้างเฉพาะใน "เริ่ม V2-A session" ตาม RUNBOOK) |
| N3 | **ฟิลด์เสริมเกินสเปก NDJSON** (analyzer รับได้ — ใช้ `.get`/ไม่อ่าน key แปลก) | (1) `noCancel` บน event `work_start` (หลักฐานว่าอ่าน field ครบตามสเปกอ่าน) (2) `skipK` บน `work_start` (= 1.0 เมื่อ `skipping==false` — ค่า threshold multiplier ที่ปั๊มจะใช้ ตาม time-static-notes §2.4) และบน `day_tick` (= ค่าที่วัดได้จริงของวันนั้น: max(`nextDayTime`) ที่สะสมไว้ก่อน tick ÷ `dayTime` — ตอบโจทย์แผน §E.2 "บันทึก k ที่เกิดจริง 0.5/0.2") |
| N4 | **ข้อจำกัดของ analyzer (ไม่แก้ analyzer — รายงานตามคำสั่ง B4)** | (1) analyzer อ่านค่า k จาก event `work_start` เท่านั้น (`extract_sessions`) — รอบ skip ของชุด A กด `>>` **หลัง**เริ่มงาน ⇒ k ณ work_start = 1.0 ไม่สะท้อน skip ⇒ **verdict ของ session แบบ skip เป็นข้อมูลประกอบเท่านั้น ไม่ใช่คำตัดสิน FORM1/FORM2** (k จริงอ่านจากฟิลด์ `skipK` ของ `day_tick` นอก analyzer) — RUNBOOK ต้องเขียนกันความเข้าใจผิดตรงนี้ (2) tolerance ดีฟอลต์ 0.5 s ทำให้ N=10, D=1, P=0.25 (gap = 0.5 = tolerance) ออก **INDISTINGUISHABLE ตั้งแต่โครง** — การวัดระดับเฟรมรองรับ tolerance แคบลง (เสนอ 0.1 s) — **ค่า tolerance เป็นส่วนของเกณฑ์ที่ต้องประกาศก่อนวัดตามแผน §E.1** ⇒ ระบุใน RUNBOOK-V2A และให้ผู้ใช้ยืนยันตอน "เริ่ม V2-A session" ไม่ตัดสินเอง |

## สิ่งที่ตรวจแล้วและ**ไม่**พบข้อผิดพลาด (บันทึกเพื่อให้รู้ขอบเขตการรีวิว)

- ความสอดคล้อง §E.1/§E.2 ชุด A กับสเปก sampler ในแพ็กเกจ 2 (`APPROVAL-REQUESTS.md` 45–84): field ตัวอ่าน/event/ความถี่/กฎ 2 §D.0 — ตรงกัน
- ข้อความมติ [V2A-3] (ชุด A เฉพาะตัว N ∈ {1,5,10} + N=10 กด `>>`, ไม่รวม B/C) ตรงกับ §E.2 ชุด A และ §F.0
- field ทั้งหมดในสเปกมีจริงใน `dump.cs` (DUMP, ตรวจ 2026-10-29): `WorkingUIController` — `working` 0x48 · `leftWorkingDay` 0x4C · `totalWorkingDay` 0x50 · `nextDayTime` 0x54 · `leftPauseTime` 0x58 · `workFinished` 0x60 · `noCancel` 0x61 · `skipping` 0x62 · statics `dayTime`/`pauseTime` + `Instance` · `WorldData.hour` = **float** (0xB4 — เขียน NDJSON เป็น float) · `TimeData` year/month/day = int (dump.cs 367149–367153) · ชื่อ member ครบใน #Strings heap ของ interop `Assembly-CSharp.dll` (grep ดิบ ไม่โหลดแอสเซมบลี)
- control flow ปั๊มที่ใช้ออกแบบ event (`work_start`/`day_tick`/`pause_end`/`work_end`/`work_abort`) อ้าง time-static-notes §2.4–2.8 (OBSERVED) — จุดตัดสิน `work_end` vs `work_abort` = ค่า `workFinished` ที่ถูกเขียนในเฟรมเดียวกับ working 1→0 (finish branch เขียน 1 · StopWorking เขียน arg)
