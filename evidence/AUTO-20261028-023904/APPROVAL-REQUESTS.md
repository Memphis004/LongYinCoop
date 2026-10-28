# APPROVAL-REQUESTS — แพ็กเกจที่รอมติผู้ใช้ (สร้าง 2026-10-28 โดย session `AUTO-20261028-023904`)

> อ่านไฟล์นี้แล้วตัดสินใจได้เร็ว · **ทุกแพ็กเกจยังไม่มีสิทธิ์เริ่มจนกว่าผู้ใช้จะระบุมติครบตามที่แจกแจง**
> · **กติกาคาดโทษ: 4 สายงาน (S2a-4 GO-3 / V1 / V2 / SP5-T0 และ S3) ห้ามอยู่ในเซสชันเกมเดียวกัน**
> · ลำดับที่แนะนำ (เหตุผล: ลดตัวแปร — จบงาน deploy/runtime หนึ่งเรื่องให้จบ แล้วค่อยเปิดเรื่องใหม่):
> **GO-3 → V2 ชุด A → SP5-T0 → V1 → (S3 แยกไปอีกที)** · V1 กับ V2 ชุด A ใช้ dependency ต่างกันตาม
> time-plan-draft §F.0 (ชุด A ไม่ต้องรอ V1) — แต่การรันจริงยังแยกเซสชันเกม

---

## แพ็กเกจ 1 — S2a-4 GO-3 (build/deploy/runtime ของ Guest Overlay + set_pref gate)

**จุดประสงค์:** deploy build ที่มี overlay in-process + `set_pref` gate แล้ววัด G0/E6/E1–E8 ตามเกณฑ์
`S2a-4-plan.md` rev4 §11 (ตรงตัว ไม่ปรับเกณฑ์) บนสำเนาเซฟ · มติ M5 = M5-3 (ไม่ hook เลย) ใช้อยู่แล้ว

**สถานะหลักฐาน:** PASS-2 (offline) = PASS [A1] · C# 596/0 ×3 รอบ (วัดซ้ำ 2026-10-28) · python 53 OK ·
static CLEAN · **BLOCKED 2 ข้อ:** (1) Release build ล้ม — `SP5/Sp5T0Probe.cs` ขาด using
(ดู `evidence/S2a4-GO3-20261028-030025/provenance.md`) (2) digest เซฟ ≠ baseline (ต่างเฉพาะ
`PlayerprefData.dat` — ดู session-start.md DEVIATION-1) · RUNBOOK ครบแล้ว:
`evidence/S2a4-GO3-20261028-030025/RUNBOOK-G3.md`

**สิ่งที่ต้องมติผู้ใช้ (ลอกจากแผน/สถานะจริง — ค่าเริ่มต้นที่เสนอ + เหตุผลสั้น):**

| # | มติ | ตัวเลือก | ค่าเริ่มต้นที่เสนอ | เหตุผล |
|---|---|---|---|---|
| G-1 | แก้ build break | (ก) เพิ่ม `using System.Diagnostics;` ใน Sp5T0Probe.cs (1 บรรทัด) / (ข) exclude `SP5/**` จาก csproj / (ค) ยังไม่แก้ — เลื่อน GO-3 | **(ก)** | แก้น้อยสุด ไม่เปลี่ยนพฤติกรรม probe (ยังไม่ถูกติดตั้งอยู่แล้ว); (ข) กระทบ build ทุก config มากกว่า |
| G-2 | baseline เซฟ | (ก) rebaseline volatile-only (`PlayerprefData.dat` เดียว) / (ข) คง baseline `58883949…` + บันทึก divergence แล้ว restore เอง | **(ก)** | SaveSlot* 55 ไฟล์ EXACT อยู่แล้ว; drift = volatile ตามปกติทุกครั้งเปิดเกม; รูปแบบเดิมที่เคยอนุมัติซ้ำหลายครั้ง |
| G-3 | หลัง session: DLL ใหม่คงไว้หรือ rollback `.prev-1b4c17aa` | ตัดสินตอนจบ session | — | RUNBOOK ขั้น 8.3 ห้ามตัดสินแทน |
| G-4 | อนุมัติวลี "เริ่ม GO-3 session" = [N1]+[N2] ตาม RUNBOOK | ใช่/ยัง | — | วลีต้องมาหลัง G-1/G-2 มีมติแล้ว |

**สิ่งที่ agent จะทำเมื่ออนุมัติ:** ขั้น 0–9 ตาม RUNBOOK-G3.md (session-start ใหม่ → ตรวจ artifact →
สำรอง `.prev-*` + deploy → net.cfg (overlay=true) → split+preflight → **ผู้ใช้เปิดเกมเอง** → G0 → E6 →
E1/E2/E3/E4/E7/E8 → ปิด session restore+คืน net.cfg → รายงานราย E)

**ความเสี่ยง:** OnGUI อาจไม่ยิง (G0 = NO-GO หยุดก่อนอย่างอื่น) · overlay กิน guest slot เดียว (ห้ามรัน
viewer-client พร้อมกัน) · E4 อาจ INCONCLUSIVE (band ±0.5 — รายงานตามจริง ห้ามขยับ) · deploy DLL ใหม่
บนเกมจริง (rollback พร้อมทุกขั้น)

**Rollback:** ขั้น 8 ของ RUNBOOK + `.prev-*` + restore เซฟ EXACT + คืน net.cfg

**วลีสั่งงาน:** `"เริ่ม GO-3 session"` (แนบมติ G-1/G-2 มาในข้อความเดียวกัน — ถ้าไม่ครบ agent ปฏิเสธ)

---

## แพ็กเกจ 2 — V2 ชุด A (T(N) rate ปกติ — อ่านอย่างเดียว ไม่เขียน static field)

**จุดประสงค์:** วัด T(N) ที่ `dayTime=1.0, pauseTime=0.25` ระดับเฟรม (N ∈ {1,5,10} + N=10 กด `>>`)
เพื่อ**แยกสองรูปของสูตร T(N)** — `N*(D*k)+N*P` (12.5 s) vs `N*D+(N+2)*P` (13.0 s) ที่ N=10 — ซึ่งค่าวัด
เดิม (~12.5, n=1) แยกไม่ได้ (time-static-notes หมายเหตุ 2026-10-28 + time-plan-draft B.6/§E.2)

**สถานะหลักฐาน:** โครงสูตร OBSERVED เชิง disasm (Update 0x9CB4B0 — initial pause 0x9CACAA, threshold
×k, final 2×pause) · สเปก sampler + สคริปต์วิเคราะห์**พร้อมแล้ว**: `tools/time_v2a_analyze.py` + เทสต์
9/9 (commit `b1418a3`) — ตัดสิน FORM1/FORM2/INDISTINGUISHABLE/MISMATCH/INVALID (day_ticks ≠ N = INVALID)

**สิ่งที่ต้องมติผู้ใช้ (จาก time-plan-draft §F.0 — ชุด A = Gate 0 + Gate 1 + Gate 5a โดยตรง ไม่ต้องรอ Gate 2–4):**

| # | มติ | ตัวเลือก/ค่าเริ่มต้นที่เสนอ | เหตุผล |
|---|---|---|---|
| V2A-1 | **Gate 0** — อนุมัติ `time-plan-draft.md` เป็นแผนทำงาน | ใช่/แก้ก่อน | แผนยังสถานะ DRAFT รอรีวิว |
| V2A-2 | **มติ D9** — baseline/fixture เซฟ | (ก) rebaseline / (ข) คง divergence + snapshot ก่อนทดสอบ | Gate 1 ไม่ผ่านถ้าไม่มี D9 (ระบุในแผนตรง ๆ) — แนะแนวทาง (ข) ตามที่แผนเขียนไว้เองว่า "ห้ามอ้าง fixture diverge ตรง baseline เดิม EXACT" |
| V2A-3 | **Gate 5a** — อนุมัติรันชุด A เฉพาะตัว | ใช่/ยัง | การอนุมัติชุด A ไม่รวมชุด B/C โดยปริยาย (§F.0) |
| V2A-4 | **อนุมัติเขียนโค้ด sampler อ่านอย่างเดียวชุดใหม่ใน mod** (ยังไม่มีสิทธิ์เขียน src/ — [N5]) | ใช่/ยัง | จำเป็นเพราะต้องวัดระดับเฟรม; spec อยู่ด้านล่าง — log-only, typed interop, ไม่มี write path |
| V2A-5 | อนุมัติ build/deploy DLL ที่มี sampler (แยกจาก GO-3 build) | ใช่/ยัง | sampler = โค้ดใหม่ใน mod ต้อง deploy จึงวัดได้ |

**สเปก sampler (ร่าง — เขียนจริงหลัง V2A-4):** อ่านผ่าน **typed interop เท่านั้น** ห้าม raw offset —
`WorkingUIController.Instance` fields (`working`, `leftWorkingDay`, `totalWorkingDay`, `nextDayTime`,
`leftPauseTime`, `workFinished`, `skipping`, `noCancel`) + statics `dayTime`/`pauseTime` (อ่าน) +
`WorldData` `hour`/วันที่ · ความถี่: **per-frame ขณะ `working==1`** (นอกงานไม่เขียน) · รูปแบบ NDJSON ต่อ
event: `t` (unix ms), `frame`, `ev ∈ {work_start, day_tick, pause_end, work_end, work_abort}`,
`leftWorkingDay`, `totalWorkingDay`, `nextDayTime`, `leftPauseTime`, `workFinished`, `skipping`,
`dayTime`, `pauseTime`, `hour` · day_tick ตรวจจับจาก `leftWorkingDay` ลดลงระหว่างเฟรม (ห้ามใช้ hook
ChangeDay — ไม่มี hook ในเส้นนี้) · ทุกเคส = สำเนาแยก (กฎ §D.0 กฎ 2) · ทำซ้ำ ≥3 รอบ/ค่า

**สิ่งที่ agent จะทำเมื่ออนุมัติ:** "เตรียม V2-A ให้พร้อมรัน" = เขียน sampler + เทสต์ + compile-only build +
provenance (ยังไม่ deploy) → "เริ่ม V2-A session" = preflight/split → ผู้ใช้เปิดเกม → ผู้ใช้เริ่มงาน N วันตาม
เคส → เก็บ ndjson → analyze → รายงาน → restore

**ความเสี่ยง:** ต่ำสุดของสามแพ็กเกจ (ไม่มี write ใด ทั้งเกม/เซฟ/pref) · ความเสี่ยงหลัก = คุณภาพการวัด
(แอนิเมชัน/freeze 3 ทางเป็น noise — แผน §B.7 กำหนดวัดแยก) · **MISMATCH = หยุดรายงาน ห้าม refit โมเดลเอง**

**Rollback:** restore สำเนา + คืน net.cfg/pref + sampler ปิดด้วย pref default-off (idiom เดิม)

**วลีสั่งงาน:** `"เตรียม V2-A ให้พร้อมรัน"` (ต้องแนบ V2A-4/V2A-5 ถ้าจะให้เขียนโค้ดไปด้วย) →
`"เริ่ม V2-A session"` (ต้องแนบ V2A-1..3 + มติ D9 ครบ — ไม่ครบ agent ปฏิเสธและแสดงรายการที่ขาด)

---

## แพ็กเกจ 3 — SP5-T0 (บัญชี NPC 1 creation round — probe log-only)

**จุดประสงค์:** ตรวจบัญชีการสร้าง NPC หนึ่งรอบ (`GenerateHero`/`GenerateRandomEnemy`/`AddTempHero`
ฯลฯ) ด้วย probe log-only เพื่อปิดเงื่อนไขก่อนคิดเรื่องลด NPC (ห้ามลด NPC ในรอบนี้ — [N8])

**สถานะหลักฐาน:** แผน `sp5-t0-plan.md` rev 13 ครบ · **โค้ด probe source มีแล้ว** (`SP5/Sp5T0Probe.cs`,
commit `7ff47ab` — **ยังไม่เคย compile** และเป็นต้นเหตุ build พังปัจจุบัน) · สคริปต์ตรวจสมการพร้อมแล้ว:
`tools/sp5t0_audit.py` + เทสต์ synthetic 6/6 (commits `16a38c3` + fix `ea3c7dd`) — ตรวจ bucket
pre/creation/post, E5 unattributed, E7 Σhist==H + ΣperForce==hist[sect], phaseStuck==0,
E1/E2 = observational UNKNOWN ตาม rev 13

**รายการอนุมัติ §4 ของ sp5-t0-plan.md ที่ยังค้างทั้ง 8 (ห้าม agent ตัดสินแทน):**

| # | รายการ | ค่าเริ่มต้นที่เสนอ | เหตุผลสั้น |
|---|---|---|---|
| 1 | อนุมัติแผน T0 (rev 13 — หมายเหตุ: §4 ข้อ 1 ในไฟล์ยังเขียน "rev 7" เป็นข้อความค้างของ revision เก่า) แล้วจึงแก้/ตรวจโค้ด probe | ใช่ (หลังรีวิว) | กติกา 1 — แผนก่อนลงมือ |
| 2 | build/deploy DLL ที่มี probe + ตั้ง `Sp5T0=true` ก่อนเปิดเกม | ใช่ (ตามลำดับ preflight → split → probe → เปิดเกม) | probe ต้องอยู่ใน build จริง |
| 3 | preflight §2.6 ขั้น A (read-only) PASS ครบ + **อนุมัติ save-split ขั้น B แยก** | ใช่ — แยกมติขั้น A / ขั้น B ชัด | ขั้น B เปลี่ยน filesystem (rev 5) |
| 4 | ทดสอบบนสำเนาเซฟตาม §1.2 | ใช่ | กติกา 4 ของ repo |
| 5 | OPTIONAL hooks H6–H11 | **ไม่ติดทั้งหมด** (CORE H1–H5 พอสำหรับ E1–E7) | ลดตัวแปรรอบแรก |
| 6 | แผนสำรอง hook `List<T>.Add` | ไม่อนุมัติตอนนี้ — เสนอใหม่เมื่อ residual ≠ 0 จริง | hook กว้างเสี่ยง frame-time |
| 7 | settle window / event cap / ตำแหน่ง evidence | ตามแผน: 10 นาที / 50,000 / `evidence/SP5t0-<ts>/` | ค่าที่แผนเสนอไว้ |
| 8 | ถ้า typed read ล้มเหลวต้องใช้ raw offset | **ปฏิเสธ** (อ่านไม่ได้ = รายงาน readfail + ข้าม) | กติกา 8 |

**รายการตรวจโค้ด probe (draft — ตรวจหลัง G-1 แก้ build แล้ว compile ผ่าน):** log-only 100% (ไม่มี write
เข้า state เกม, ไม่แก้ args, `__result` อ่านเดี่ยวใน postfix) · try/catch กลืนครบทุก hook + err counter ·
finalizer contract ฉบับ canonical (lifecycle ทุก invocation, คืน exception เดิม, `factory.finalizer` เชิงบวก /
`factory.failure` conditional) · invocation linkage `iid` พิสูจน์ด้วย harness test · patch-audit 4 จุดตรวจ
(expected inventory HarmonyX 2.10.2 ครบ 5 categories) · pref `Sp5T0` default false · Uninstall ตรวจ patch
count คืน 0 · ประกาศชัด: ไม่มีกลไกพิสูจน์ `inventoryStability` ภายใน window ⇒ E1/E2 คาดหมายคง UNKNOWN

**สิ่งที่ agent จะทำเมื่ออนุมัติ:** "เตรียม SP5-T0 ให้พร้อมรัน" = รีวิวโค้ด probe ตาม checklist + compile
(หลัง build fix) + provenance (ยังไม่ deploy) → "เริ่ม SP5-T0 session" = preflight ขั้น A → [มติ split ขั้น B]
→ split → deploy + `Sp5T0=true` → ผู้ใช้เปิดเกมแล้ว**สร้างโลกใหม่ทันที ไม่เปิด plot/recruit ระหว่างรอบ** →
เก็บ ndjson → `sp5t0_audit.py` → รายงาน → restore

**ความเสี่ยง:** probe โดนเรียก ~886+ ครั้ง/รอบ (buffered write แล้ว) · `StartNewGame` เปลี่ยน WorldData
instance (rebaseline marker + สมการข้าม instance = ไม่ปิด) · E1/E2 คาดหมายคง UNKNOWN (rev 13 —
ไม่ใช่ความล้มเหลว) · ห้ามสรุป "path ที่สอง" จาก unattributed เฉย ๆ

**Rollback:** pref `Sp5T0=false` = Uninstall ทันที + `CompatInventory.CountOwnedPatches` ต้องคืน 0 ·
restore สำเนา · deploy ตามขั้น .prev-* เดิม

**วลีสั่งงาน:** `"เตรียม SP5-T0 ให้พร้อมรัน"` (แนบมติข้อ 1 อย่างน้อย) → `"เริ่ม SP5-T0 session"` (ต้องแนบ
มติข้อ 1–7 ครบ แยกขั้น A/B ตามที่แผนกำหนด — ไม่ครบ agent ปฏิเสธและแสดงรายการที่ขาด)

---

## แพ็กเกจ V1 (อยู่ท้ายลำดับ — dependency หนักสุด)

**จุดประสงค์:** ตรวจพฤติกรรม**ยกเลิกกิจกรรม** (TC-1/TC-2/TC-4 ผ่าน**ปุ่ม UI**) บนสำเนาเซฟ — refund/ทรัพยากร/
dialog/latch `workFinished` (time-plan-draft §D)

**สิ่งที่ต้องมติก่อนเสมอ:** **D1** (ยกเลิกผ่านปุ่ม UI เท่านั้น หรืออนุญาต invoke `MiddleStopWork` ด้วย) และ
**D2** (ยอมรับหลักการ per-day credit ถาวรย้อนคืนไม่ได้) — Gate 2 · แล้ว Gate 3 (อนุมัติ V1 แยก รวม probe
build log-only ที่ `StopWorking`/`MiddleStopWork`/จุดยิง finishFuc ตาม §D.1 ข้อ 5) · **เคส TC-3a/TC-3b
(invoke method เกม) ไม่ถูกครอบด้วยแพ็กเกจนี้เว้นแต่ผู้ใช้อนุมัติแยก** (เป็นการเรียก method เกม = [N4]
โดยตรง) · ตาม §F.0 ทาง V1 ต้องผ่าน Gate 0→1→2→3 — ทำหลังแพ็กเกจ 1–2 เพื่อไม่ให้มติ D1/D2 ไปผูกกับ
ผลวัดอื่น

**วลีสั่งงาน:** `"เตรียม V1 ให้พร้อมรัน"` / `"เริ่ม V1 session"` (ต้องแนบ D1 + D2 + ประกาศอนุมัติ Gate 3 —
ไม่ครบ agent ปฏิเสธ)
