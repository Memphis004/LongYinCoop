# INDEX — สารบัญเอกสารของ repo นี้

> จัดโครงสร้างเมื่อ 2026-10-29 ตามคำขอผู้ใช้ — ย้ายด้วย `git mv` ทั้งหมด (เนื้อหาไฟล์ไม่ถูกแก้)
> **ไม่มีไฟล์ใดถูกลบ** · `evidence/` และ `coop-test/` **ไม่ถูกย้ายเลย** (หลักฐาน/บาสไลน์ — ห้ามจัดระเบียบทับ)
> ถ้าเอกสารอื่น (AGENTS.md, รายงานใน evidence/, คอมเมนต์ในโค้ด) อ้างชื่อไฟล์แบบ root เดิม ให้ใช้แผนที่ท้ายไฟล์นี้
> อัปเดต 2026-10-30: ย้าย `sp5-t0-plan.md` + `sp5-npc-n3-approval-request.md` → `plans/closed/` (git mv, เนื้อหาไม่แก้) และเพิ่มแถวไฟล์ SP5-NPC ที่เกิดใหม่หลัง 2026-10-29

## ไฟล์ระดับ root (ไฟล์ที่ใช้งานจริง)

| ไฟล์ | สถานะ | หมายเหตุ |
|---|---|---|
| `README.md` | ใช้งาน | วิธี build/deploy + prefs + IPC (เนื้อหายุค S0 — ส่วน prefs/IPC ล้าสมัยบางส่วน) |
| `STAGEPLAN.md` | **ใช้งาน — เล่มหลัก (v2 merged)** | แหล่งความจริงอันดับ 1 ของ ช่วง 0 (S0–S6) ตาม AGENTS.md |
| `STAGEPLAN-v3-addendum.md` | **ใช้งาน — เอกสารทำงานของ ช่วง 1** | rev3 (2026-10-24) §15–§21 · ยังห้ามเริ่มงาน ช่วง 1 จน gate ครบ |
| `VERSION.md` | ใช้งาน | hash เกม 2 ไฟล์ + baseline + deploy record — ตรวจทุก session-start |
| `battle-notes.md` | ใช้งาน | แหล่งความจริงอันดับ 2 ตาม AGENTS.md — คงไว้ root เพราะ AGENTS อ้างชื่อตรง ๆ |
| `worldmap-notes.md` | ใช้งาน | เช่นเดียวกับ battle-notes.md |
| `INDEX.md` | ไฟล์นี้ | |

## `plans/` — แผนที่ยังใช้งาน / รอ gate ถัดไป

| ไฟล์ | สถานะ ณ 2026-10-30 |
|---|---|
| `S2-plan.md` | แผนแม่ S2 — S2a-0…S2a-3 ปิดแล้ว (tags) · ใช้ §13 (rev2) แทน §1–§12 ตามมติรายข้อ §13.9 · Q3 ยังไม่ตัดสิน |
| `time-plan-draft.md` | TIME-PLAN (V1/V2 + Gate 0–6) — **V2-A รันปิดแล้ว 2026-10-29** (`evidence/V2A-20261029-020816/v2a-report.md`) · phase ถัดไปรออนุมัติ gate ตาม §F |
| `GATE56-OptionB-stage-plan-draft.md` | แผน stage Gate 5/6 ตามมติ D3 = Option B — **ร่าง rev 1 (รอรีวิว — ห้ามเริ่มโค้ด)** · ขั้น G1–G4 · 5 ประเด็นรอมติก่อน G1 (§5.1) |
| `S3-plan-draft-rev2.md` | ร่างแผน S3 rev2 — **เอกสารรีวิวเท่านั้น ยังไม่ขอเริ่ม** (Gate 2 = PASS แล้ว แต่ไม่ใช่การอนุมัติ S3) |
| `sp5-npc-reduce-plan-draft.md` | แผน SP5-NPC (ลด NPC) — **N1–N4 อนุมัติ+ทำเสร็จ+ปิดแล้ว** (2026-10-30 · N4 วัดครบ 3 รอบ: sect 311 ×3, heroNum 50→88 วัดตรง, loveable listPresent 57 · tag `stage-N4` · evidence `SP5NPC-N4-20261030-052251` + `-063817`) · **N5 ร่างคำขอแล้ว ยังไม่ดำเนินการ · N6 และระดับ Mid/Strong ยังไม่อนุมัติ** · มติระดับ/refill คงรอใน `decisions-pending.md` §7.1 |
| `sp5-npc-n4-approval-request.md` | คำขออนุมัติ stage N4 (T2–T4 ที่ Light ×0.5) — **อนุมัติ+รัน+ปิดแล้ว 2026-10-30** (append ที่ 1–4 ตามลำดับมติ · รอบ 1 หยุดที่นิยาม loveable เก่า → นิยามใหม่ผ่านร่าง follow-up · 3/3 รอบ PASS · tag `stage-N4`) |
| `sp5-npc-n5-approval-request.md` | คำขออนุมัติ stage N5 (T5–T7 บนโลกที่ลด ที่ Light — เดิน ≥25 วัน ข้าม ChangeMonth ≥2 + plot/mission ≥20+≥20 + dict/ID ตรวจ) — **ร่าง ยังไม่ได้รับอนุมัติ ยังไม่ดำเนินการ** (ต้องระบุทางวัด T7 + นโยบายเซฟ slot บนสำเนาในวลีสั่ง) |
| `CLOCK-1-decision-draft.md` | ร่างมติปฏิทินกลาง server + ไม่มี pause (P1–P4, a/b/d/e = มติผู้ใช้ 2026-10-30 · ข้อ c ค่าตั้งต้นล่าสุด **720 วิ/วัน** = 1 เดือน 6 ชม.จริง — แทน 120 · ล็อกสุดท้ายรอ M1/M2 · กฎ 25% = ข้อเสนอ ยังไม่ใช่มติ) — **มติรอบ 5 (§10) + รอบ 6 (bullet ท้าย §10): (ค) = α ยืนยันกลับ (แทนสถานะ "ยังเปิด" โดยไม่ลบ) · double-driver = ทาง (ii) นาฬิกา host driver เดียว ไม่ใช้ทาง (i)** · M1 events (static) = อนุมัติแล้วเฉพาะส่วนอีเวนท์ (R6-7) — ผล `evidence/DOCS-A15-20261031-112459/m1-events-report.md` |
| `NPC-1-ACT-1-decision-draft.md` | มติสหายร่วมตี้ NPC (N1–N7) + ACT-1 งาน AFK — **มติรอบ 5 ฉบับสั่งงาน = §12** ((ฉ) สูตร `max(30,12×N)` วิ ทุก N · (ช) = ช-A คง dayNum/รางวัลต่อวัน · (ซ) งานเลี้ยงนอกขอบเขต · (ฌ) ป้ายสองหน่วย · (ข) ปิด = เครดิตวันงานที่ครบ D2) · **มติรอบ 6 = §13 (R6-1..R6-8)** (double-driver ทาง (ii) · (ค)=α · ขอบเขต = ทุกกิจกรรมผ่านปั๊มกลางยกเว้นงานเลี้ยง (R6-5) · hook อนุมัติในหลักการ + เงื่อนไข patch-count fix (R6-4) · V2-A superseded (R6-8)) · ผล A1 = `evidence/DOCS-A1-20261031-065625/` · ผล A1.5 = `evidence/DOCS-A15-20261031-112459/a15-report.md` |
| `ACT1-impl-stage-plan-draft.md` | แผน stage ACT-1 — **rev 2 (รอบ 6 · DRAFT รออนุมัติ ห้ามเริ่มโค้ด)** — ตรง §12 + ผล A1 + R6-1..R6-8 (diff rev 1→2 ในไฟล์ คงสมมติฐานเก่าในรูป "ถูกแทนด้วย") · ลำดับ: A1 (เสร็จ) → A1.5 (เสร็จ — disasm read-only) → A2 (pure+config+เทสต์ compile-only) → A3 (runtime — ผู้ใช้เปิดเกม) → A4 (guest overlay หลัง S3) |
| `PUSH-AND-SECRETS-RUNBOOK.md` | runbook ความปลอดภัยของ repo (รอบ 8) — เช็กลิสต์ก่อน push · กฎ secret (สุ่มใหม่ทุกครั้งก่อนเปิดเครือข่าย/ก่อน S3 · burned ห้าม reuse · redact ก่อน commit) · ติดตั้ง/ถอน `core.hooksPath tools/githooks` (config ไม่ track) · ขั้นตอนเมื่อสแกนพบ secret (ห้าม rewrite บน origin) |

## `plans/closed/` — แผนที่ปิดแล้ว / superseded / ประวัติ diff (อ่านอย่างเดียว)

| ไฟล์ | สถานะ + รายงานปิด |
|---|---|
| `S1.5-plan.md` | tag `stage-S1.5`/`stage-S1.5g` · `evidence/S1.5-close-20261021-031700/S1.5-report.md` |
| `S2a-2-plan.md` | tag `stage-S2a-2` · `evidence/S2a2-20261021-2254/S2a2-report.md` |
| `S2a-3-plan.md` | tag `stage-S2a-3` · `evidence/S2a3x5-20261024-0132/S2a3-report.md` |
| `S2a-4-plan.md` | tag `stage-S2a-4` · `evidence/S2a4-GO3-20261028-074003/s2a4-report.md` |
| `SP0-full-plan-draft.md` | rev2.1 อนุมัติ+รัน 2026-10-25 · `evidence/SP0-20261025-0211/sp0-report.md` (8/8 PASS) |
| `X5-metric-proposal.md`, `X5-remeter-plan.md` | งาน X5 ของ S2a-3 จบแล้ว — ตัดสินด้วย Δmean ตามมติ 2026-10-24 |
| `X6-metrics-20261024.md`, `X6-sample-plan.md` | งาน X6 ของ S2a-3 จบแล้ว (PASS) |
| `S3-plan-draft.md` | ฉบับ 2026-10-25 — superseded โดย `plans/S3-plan-draft-rev2.md` (คงไว้ตามหัวไฟล์ rev2) |
| `STAGEPLAN.v2-draft.md` | superseded โดย `STAGEPLAN.md` (v2 merged) — เดิมถูกระบุว่าห้ามใช้เป็นแหล่งความจริง |
| `STAGEPLAN.v3-draft.md` | rev1 — SUPERSEDED โดย `STAGEPLAN-v3-addendum.md` (ป้ายในหัวไฟล์) |
| `sp5-t0-plan.md` | rev 13 อนุมัติ + **รัน T0 จริง 2026-10-30** · `evidence/SP5T0-run-20261030-012502/round-report.md` (เงื่อนไขความถูกต้อง PASS · E4–E7 ปิด · E1/E2 UNKNOWN ตามโครง rev 13 · restore EXACT `df8e5a11beed` + rollback `6a94abb5` + cfg `976ccfb9` ครบ) · มติ GO ลด NPC ตัดสินแล้ว → `plans/sp5-npc-reduce-plan-draft.md` |
| `sp5-npc-n3-approval-request.md` | stage N3 อนุมัติ+รัน+ปิดแล้ว 2026-10-30 — T1 ผ่าน 3/3 (census h=883 ตรง T0 ทุกประเภททุกรอบ, SaveSlot* ไม่ถูกเขียน) · `evidence/SP5NPC-N3-20261030-035500/` · commits `7396b94`→`bda5200`/`3131df8` (หัวไฟล์ยังเป็นถ้อยคำ "ร่าง" ก่อนอนุมัติ — เนื้อหาไม่แก้ตามธรรมเนียมการย้าย) |
| `S2a-4-plan-rev3.diff`, `S2a-4-plan-rev4.diff`, `S2a4-*.diff` (6 ไฟล์) | ประวัติ diff ของแผน S2a-4 |
| `STAGEPLAN.v2-to-v3.diff`, `STAGEPLAN-v3-addendum.diff`, `STAGEPLAN-v3-addendum-rev3.diff` | ประวัติ diff ของ STAGEPLAN v2→v3 / addendum rev1→rev2→rev3 (ถูกอ้างเป็น "ประวัติ" ในหัว addendum) |

## `notes/` — โน้ตสำรวจความรู้เกม (read-only ทั้งหมด อ้างอิงย้อนหลัง)

| ไฟล์ | เรื่อง · วันที่สำรวจ |
|---|---|
| `area-notes.md` | Area / UI survey (frida typed access, 0 write) · 2026-10-22 |
| `explorer-notes.md` | Explorer / ดันเจี้ยน รอบแรก · 2026-10-23 |
| `sp0-sp7-notes.md` | SP0/SP7 investigation (observational hooks) · 2026-10-24/25 |
| `sp5-prep-notes.md` | จำนวน hero ตอนสร้างโลกใหม่ (DUMP-only) · 2026-10-26 |
| `time-notes.md` | ระบบเวลาโลก (read-only survey) · 2026-10-27 |
| `time-audit-notes.md` | Activity timing / global pause / ยกเลิกฝึก (audit รอบ 3) · 2026-10-27 |
| `time-followup-notes.md` | Pause / ระยะเวลา interactive (สืบค้นรอบ 2) · 2026-10-27 |
| `battle-notes.md` | Battle / ศึกต่อสู้ · 2026-10-27 |
| `worldmap-notes.md` | BigMap / WorldMap แผนที่โลก· 2026-10-27 |

## `evidence/` — โฟลเดอร์ที่เพิ่ม/ปิดล่าสุด (รอบ 6 · 2026-10-31 — **รายการใหม่เท่านั้น ไม่ใช่สารบัญเต็มของ evidence/**)

| โฟลเดอร์ | เนื้อหา |
|---|---|
| `DOCS-A1-20261031-065625/` | ผล A1 static survey (a1-report.md — A1-1..A1-5) + patchcount-audit.md (วินิจฉัย CountOwnedPatches นับ method ไม่ใช่ patch-entries) |
| `DOCS-A15-20261031-112459/` | A1.5 disasm 5 รายการ (a15-report.md + bodies/ 23 ไฟล์ + m1-bodies/ 17 ไฟล์ + runner scripts) + M1 events (m1-events-report.md Q1–Q5) — งาน 3/4 รอบ 6 |
| `PATCHGATE-20261031-111337/` | patchfix-report.md — แก้ patch-count gate เป็น set+entries matcher (งาน 2 รอบ 6 · suite 808/0 · build 0W/0E) |
| `TIME-V1-20261031-044249/` | สาย V1 ปิด (CLOSED-CANCELLED — V1-close-report + rollback-user-cancel) · TC1-r1 = INCOMPLETE-probe-down |
| `SP5body-TempHerosDict-20261031-034658/` | asm bodies ระบบเวลา (29 ไฟล์) + toolchain (`time_static_common.py`/`disasm_time_bodies.py` — read-only) + analysis-report + floats-observed.tsv — toolchain ที่ A1/A1.5 อ้างอิง |
| `AUTO-20261030-071500/` · `AUTO-20261031-035916/` · `AUTO-20261031-GITAUDIT/` | HANDOFF ของ session ก่อน + time-plan-v1-errata + git audit snapshot |
| `SECRET-20261031-121438/` | incident note: ค่า secret จริงใน `evidence/S2a4-GO3-20261028-074003/net.cfg.during-GO3` (เพิ่มโดย `d81c86d`) → redact `<redacted>` commit `805902a` · d81c86d อยู่บน origin แล้ว = burned ไม่ rewrite ไม่ force · เครื่องมือ `tools/scan_secrets.py` (รอบ 7) |

## แผนที่ path เก่า → ใหม่ (สำหรับอ้างอิงที่ค้างอยู่ในเอกสาร/คอมเมนต์)

- `S2-plan.md` → `plans/S2-plan.md` · `sp5-t0-plan.md` → `plans/closed/sp5-t0-plan.md` (ปิดแล้ว 2026-10-30) · `time-plan-draft.md` → `plans/time-plan-draft.md` · `S3-plan-draft-rev2.md` → `plans/S3-plan-draft-rev2.md`
- แผนปิดทั้งหมด → `plans/closed/<ชื่อเดิม>` (S1.5-plan, S2a-2-plan, S2a-3-plan, S2a-4-plan, SP0-full-plan-draft, X5-*, X6-*, S3-plan-draft, STAGEPLAN.v2-draft, STAGEPLAN.v3-draft, sp5-t0-plan, sp5-npc-n3-approval-request — สองไฟล์ท้ายย้าย 2026-10-30)
- โน้ตทั้งหมด → `notes/<ชื่อเดิม>` (area-notes, explorer-notes, sp0-sp7-notes, sp5-prep-notes, time-notes, time-audit-notes, time-followup-notes)
- `STAGEPLAN.md`, `STAGEPLAN-v3-addendum.md`, `VERSION.md`, `README.md` — **ไม่ย้าย**
- ไฟล์ที่ไม่ใช่ .md และไม่ได้แตะ: `.probe.js`, `context.txt` · โฟลเดอร์ `src/ tests/ tools/ evidence/ coop-test/` คงเดิมทั้งหมด

> คอมเมนต์ในโค้ดที่อ้างเช่น `S2-plan.md §13.2` หรือ `S1.5-plan §4` ยังอ่านเข้าใจได้ตรงกัน (อ้างเลข section ไม่ใช่ path) — แก้เฉพาะเมื่อแตะไฟล์นั้นอยู่แล้ว
