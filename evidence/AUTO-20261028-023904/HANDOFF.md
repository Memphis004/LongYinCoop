# HANDOFF — session `AUTO-20261028-023904` (2026-10-28 02:39 เริ่ม — ปิด ~03:1x)

> ไฟล์นี้พอให้ session ใหม่ที่ไม่มีบริบทอ่านแล้วทำต่อได้ · **ไม่ได้ commit** (เก็บบนดิสก์ตามคำสั่ง §8)
> · คำสั่งต้นทาง: ข้อความผู้ใช้ 2026-10-28 (Phase A–D + ทะเบียนอนุมัติ [A1]–[A6] / [N1]–[N11] + เงื่อนไขหยุด §9)

## สรุปผูกมัด 5 บรรทัด (อ่านก่อน)

1. ทำครบ Phase A/B/C/D · commits ใหม่ mod repo 7 (HEAD `ec78a8b`) + game repo 1 (HEAD `246792e`) — ไม่มี tag/push/amend
2. **DEVIATION-1 (ยังค้าง):** digest `Save` = `df8e5a11beed` ≠ baseline ที่อนุมัติ `58883949…` — ต่าง**เฉพาะ `PlayerprefData.dat`** (volatile, จากการเปิดเกมของผู้ใช้ 2026-10-27 ×3; `SaveSlot*` 55 ไฟล์ EXACT) → **ต้องมีมติ rebaseline volatile-only หรือ restore ก่อน GO-3/เซสชันเกมใด ๆ** (ห้าม agent ทำเอง)
3. **DEVIATION-2 (ยังค้าง):** `net.cfg` = `12541f34c633…` 444 B — ไม่ตรงบันทึก `dc8e9098…` 454 B ใน VERSION.md (ตรงบันทึก AGENTS 2026-10-24; เนื้อหา semantically ตรงสถานะอนุมัติ `enabled=false, heartbeatMs=1000`) → อัปเดต VERSION.md ต้องอนุมัติแยก
4. **B3/B4 = NOT-RUN: Release build ล้ม** — `SP5/Sp5T0Probe.cs(52) CS0246 'Stopwatch'` (ขาด `using System.Diagnostics;`; ไฟล์ commit `7ff47ab` ตั้งใจ "ยังไม่ build"; test suite ไม่ compile SP5/** จึงยังผ่าน 596/0) → ต้องมีมติแก้ (using 1 บรรทัด / exclude SP5) ก่อนทำ provenance ซ้ำ
5. GO-3 พร้อมรันเมื่อข้อ 2+4 ปิด: RUNBOOK `evidence/S2a4-GO3-20261028-030025/RUNBOOK-G3.md` + แพ็กเกจมติทั้งหมดใน `evidence/AUTO-20261028-023904/APPROVAL-REQUESTS.md` + วลีสั่งงานใน `TRIGGERS.md` (สรุปไว้ใน AGENTS.md หัวข้อ 2026-10-28 แล้ว)

## (ก) Phase ที่เสร็จ + commit hash

| Phase | งาน | commit (mod repo เว้นแต่ระบุ) |
|---|---|---|
| ตรวจตั้งต้น | session-start 6 ข้อ → `evidence/AUTO-20261028-023904/session-start.md` (ไม่ commit) | — |
| A1 | time-static-notes.md เพิ่มหมายเหตุ 2 รูป T(N) [(ข) TestMode ห้ามเขียน + (ค) bodies ไม่ commit = มีอยู่แล้วจาก session 2026-10-27 ตามมติผู้ใช้ — ไม่แก้ซ้ำ] | `061628f` |
| A2 | D9 ของ time-plan-draft เทียบ VERSION.md → **ล้าสมัย** (บันทึกใน (ง) ด้านล่าง ห้ามแก้ไฟล์แผน) | — |
| A3 | กลุ่ม 1–3 **ถูก commit ไปแล้วโดย session ก่อน**: กลุ่ม 1 = `df46de1` (โน้ต+สคริปต์ 6+TSV เล็ก 3; xref-summary.txt 11 KB **ไม่ถูก commit โดยตั้งใจ** ตามมติผู้ใช้ 2026-10-27 บันทึกในโน้ต §6) · กลุ่ม 2 = `84c6874` (T5/P-T0 .md) + `390e856` (time-notes ×3 + time-plan-draft) · กลุ่ม 3 = `390e856` (sp5-t0-plan root) · .gitignore:68 กัน `gameassembly.disasm.txt` ✓ และไฟล์นั้นไม่ถูก track ✓ · STAGEPLAN.v3-draft.md ไม่แตะ (clean vs HEAD) | (ตรวจแล้ว ไม่มี commit ใหม่) |
| A4 | AGENTS.md (game repo) append หัวข้อสถานะ S2a-4 (GO-1 commits 9 ตัว, ผล offline, [A1][A2], baseline, Gate 2 PASS, net.cfg จริง 444 B, deviation เซฟ, ตารางวลีสั่งงาน) | `246792e` (game repo, main) |
| A5 | TIMEstatic2-20261028-024734: 4 งาน static อ่านอย่างเดียว (สรุปใน (ข)) → `time-static-notes-2.md` | `91c276b` |
| B1 | tree สะอาด (src/tools/tests ไม่มี uncommitted) | — |
| B2 | C# suite **3 รอบเหมือนกัน**: 596 PASS/0 FAIL (66 T0/T1 + 221 s2a2 + 179 s2a3 + 130 s2a4) · python `test_negative_preconditions` **53 OK** · `s2_static_check.py` **CLEAN (0 violations)** · discover ทุก test_*.py = 68 OK (53+9+6) · `test_preflight_launch` ไม่มี unittest case (Ran 0 — harness prints ไม่ใช่เทสต์) | — |
| B3 | Release build **FAILED** (CS0246) → **NOT-RUN** (provenance บันทึกสถานะ + ทางแก้ + ขั้นทำซ้ำ) · ตรวจ 6 ชนิดใน metadata = NOT-RUN (ไม่มี artifact) | — |
| B4 | `evidence/S2a4-GO3-20261028-030025/` = provenance.md (สถานะ BLOCKED) — **ไม่มี DLL/PDB** | — |
| B5 | RUNBOOK-G3.md (9 ขั้น, G0→E6→E1–E8 เกณฑ์ §11 ตรงตัว, M5-3, prerequisite 2 ข้อ, rollback) | `a7ceccd` (รวม B6) |
| B6 | commit RUNBOOK + provenance (docs-only) | `a7ceccd` |
| C1 | สคริปต์ `tools/time_v2a_analyze.py` + เทสต์ 9/9 + สเปก sampler (typed interop, per-frame, NDJSON) ใน APPROVAL-REQUESTS แพ็กเกจ 2 | `b1418a3` |
| C2 | แพ็กเกจ V1 (D1+D2+Gate 3, TC-3a/3b ไม่ครอบเว้นอนุมัติแยก) ใน APPROVAL-REQUESTS | (ไฟล์ evidence — ไม่ commit) |
| C3 | สคริปต์ `tools/sp5t0_audit.py` + เทสต์ synthetic 6/6 + checklist ตรวจโค้ด probe + รายการอนุมัติ §4 ครบ 8 ข้อ ใน APPROVAL-REQUESTS แพ็กเกจ 3 | `16a38c3` + fix `ea3c7dd` |
| C ปิดท้าย | `APPROVAL-REQUESTS.md` (3 แพ็กเกจ + V1 + ลำดับแนะนำ GO-3→V2-A→SP5-T0→V1) + `TRIGGERS.md` | (ไฟล์ evidence — ไม่ commit) |
| D | `S3-plan-draft-rev2.md` (Gate 2 PASS, M5-3 ⇒ static+audit+validator, set_pref gate interop, guest slot, เวลา/กิจกรรมห้ามสั่งจาก guest, UNKNOWN 4 ข้อคงเดิม, มติที่ต้องขอ 6 ข้อ) — ไม่แก้ฉบับเดิม | `ec78a8b` |

## (ข) ผลตรวจ/ตัวเลขจริง (A5 static — รายละเอียดเต็มใน `evidence/TIMEstatic2-20261028-024734/time-static-notes-2.md`)

- **(ก) needAutoSave writers = 6 จุด 3 method [OBSERVED]:** `Update` 0x7E3D4E (=0, กิ่ง autosave หลัง CanSaveLoad ผ่าน) · leaf `ChangeDay` 0x792216 (=r13b, singleton `[statics 0x1D74CE8+0xB8]`), 0x7923D7 (=0), 0x792586 (=1, guard `List<AISettingType>$$Contains`) · `ChangeMonth` 0x793F47 (=1), 0x79404F (=0) · **false positive `mainThreadWaitForLock` (static 0x28 คลาสเดียวกัน, dump.cs:376444) ชัดแล้ว**: ไม่พบ statics-origin store ใน GameController$$ เลย (statics-origin +0x28 ทั้งไบนารีมีแค่ System.Net/System.Xml)
- **(ข) `HudPanelActive` (0xB09ED0) caller ชุดเต็มภายใต้ E8/E9/FF15 = 1 จุด:** `WorkingUIController$$Update` 0x9CB93C (FF15 = 0)
- **(ค) `plotHappen` writers ใน PlotController$$ = 2 จุด:** `ShowPlot` 0x5893F4 (=1) · `DisactiveInteractUI` 0x4BE572 (=0) · cross-module = **UNKNOWN** (4 method ที่ call PlotController แต่ยังพิสูจน์ไม่ได้ว่า store `[reg+0x18]` ของพวกมันเป็นของ PlotController; 1 hit suspect ใน SteamManager$$Awake — งบ 2 attempts หมด)
- **(ง) ChangeDay leaf ลำดับเงื่อนไข (บางส่วน [OBSERVED เชิงรายการ]):** ธง .data 0x790E4A → 36× thunk init → วัน>0x1E → `ChangeMonth` → guard 0x9C==1 + cmp 0x10 ∈ {2,3,4} → `ManageMailTime` → บาดเจ็บ ×3 → `ManageHeroAutoRecover`/`ManageTagTime`/`TryIdentifyAllItem`/`ManageAllAI`/`ResetAllAIMananged`/`ManageAreaStuff` → ลูป + `LoadImage` → `HaveArea`→`GenerateRandomEnemy(0,4)`+`GenerateRandomEvent` → `Monitor.Enter`→`ManageForceStuff`→`LogError`→`Monitor.Exit` → ManageWeaponResearch/Meditation/ForceSpeResearch/SpePoison/SpeSummonResearch → `CheckWorldPlotEventDataBase` → `GetInt`×2 → `Contains`→needAutoSave=1 → ret · **ยังไม่ถอด:** semantics field 0x9C/0x10/0x1E8/0x2E0/0x24, ต้นทางธง 0x790E4A, key ของ GetInt 2 จุด, CFG กิ่งย่อย
- **ผลที่ขัดความเข้าใจเดิม:** classifier origin ของ scan ติดป้าย `[rax+0x184]` ให้สองจุด แต่บริบทจริงคือ singleton path (`[statics+0xB8]` → instance) — แก้ก่อนเขียนโน้ตแล้ว (ระบุในโน้ต §(ก) ว่า heuristic ต้องตรวจบริบทตาม)

## (ค) สิ่งที่ข้าม/ทำไม่เสร็จ + เหตุผล

1. **B3/B4 Release provenance + 6-type metadata check = NOT-RUN** — build ล้ม (รายละเอียดบรรทัด 3 ของสรุป) — ห้ามแก้ source นอก [A1]–[A6]
2. **A2:** ไม่แก้ time-plan-draft.md (ห้ามแก้ไฟล์แผนที่ผู้ใช้ยังไม่อนุมัติ) — ผลเทียบอยู่ใน (ง)
3. A3: ไม่ commit เพิ่มในกลุ่ม 1–3 เพราะทั้งหมด track แล้ว; ไฟล์ untracked ที่เหลือ (evidence/S3p-*, S3prereq-*, sp0sp7-*, coop-test/* ดิบ, TIMEstatic bodies+TSV ใหญ่) = **นอกขอบเขตกลุ่ม 1–3** จึงไม่แตะ
4. **กระบวนการผิดพลาด 1 ครั้ง (รายงานตรง ๆ):** commit `16a38c3` (sp5t0_audit) ถูกสร้าง**ก่อน**เทสต์เขียว (pipe `| tail` กลืน exit code ของ unittest ทำให้ chain ไม่หยุด; เทสต์ FAIL 2 ตัวเป็นบั๊กของเทสต์เอง — index ผิด/คาดหวังจำนวน failure ผิด) → แก้แล้ว commit ตามหลัง `ea3c7dd` (6/6 PASS, discover 68 OK) — ขัดคำสั่ง "เทสต์ผ่านก่อน commit" 1 ครั้ง, แก้คืนแล้ว, ไม่ amend ([N9])
5. ไม่ได้อ่านเต็ม T5 audit + P-T0 (610+311 บรรทัด) — ใช้สรุปที่อ้างใน time-plan-draft §B (อ่านเต็ม) แทน; ไม่กระทบผลงาน Phase A–D
6. ไม่แตะ: STAGEPLAN.v3-draft.md, STAGEPLAN.md (game repo M เดิม), battle-notes/worldmap-notes (D เดิม), il2cpp-frida-mcp (m เดิม), net.cfg, MelonPreferences.cfg, เซฟทุกชั้น

## (ง) คำถามค้างสำหรับผู้ใช้ (ตัดสินเองไม่ได้ทั้งหมด)

1. **DEVIATION-1:** rebaseline volatile-only (`PlayerprefData.dat` เดียว, 57→57 ไฟล์ `SaveSlot*` EXACT) หรือคง baseline `58883949…` + restore? (แพ็กเกจ 1 ข้อ G-2)
2. **DEVIATION-2:** net.cfg จริง = `12541f34…` 444 B — อนุมัติอัปเดตบันทึก VERSION.md หรือคง + divergence record?
3. **G-1:** แก้ build break ทางไหน — (ก) `using System.Diagnostics;` 1 บรรทัดใน Sp5T0Probe.cs (เสนอ) / (ข) exclude SP5/** จาก csproj / (ค) เลื่อน
4. มติแพ็กเกจอนุมัติทั้งหมด: ดู `APPROVAL-REQUESTS.md` (GO-3: G-1..G-4 · V2-A: V2A-1..5 รวม D9/Gate 0/5a/สิทธิ์เขียน sampler · SP5-T0: §4 ข้อ 1–8 · V1: D1+D2+Gate 3)
5. **A2 ผลเทียบ (จาก VERSION.md จริง):** D9 ของ time-plan-draft อ้าง "diverge จาก `4226730f66ff` 5 ไฟล์ (SaveSlot8 ×4 + pref)" — **ล้าสมัย 2 จุด**: (1) baseline อนุมัติปัจจุบัน = `58883949…` @ `20261025-201545` (สายโซ่ 7332fc93→b7f67154→58883949 รวม SaveSlot8 เข้า baseline แล้ว) (2) divergence ปัจจุบันจาก baseline ปัจจุบัน = `PlayerprefData.dat` ไฟล์เดียว (DEVIATION-1) — ถ้าผู้ใช้อนุมัติ ให้ session ถัดไปอัปเดต D9 ใน time-plan-draft
6. ค้างเดิมที่ยังไม่แตะ: `S2-plan.md §13` rev2, VPN/เครื่อง guest/ศึก 60 ยูนิต, tag `stage-S2a-3` (รออนุมัติขั้นสุดท้าย), hardening เพิ่มเติมอื่น ๆ, tag `stage-S2a-4` (เมื่อปิด stage)

## (จ) ขั้นถัดไปที่ผู้ใช้ต้องทำ (เรียงตามลำดับที่เสนอ)

1. ตัดสิน 3 มติเปิดทาง: G-1 (build fix) + DEVIATION-1 (baseline) + DEVIATION-2 (VERSION.md record)
2. เมื่อ G-1 มีมติ → สั่ง build/provenance ซ้ำ (ขั้นใน provenance.md) → ตรวจ artifact ครบ
3. พิมพ์ `"เริ่ม GO-3 session"` (แนบมติ G-1/G-2) — agent ทำตาม RUNBOOK-G3 ขั้น 0–9 · **ผู้ใช้เป็นคนเปิดเกมเอง** และทำตามที่ RUNBOOK ระบุรายขั้น (เข้าศึก/ปล่อยค้าง/เล่น 1 เทิร์น/ปิดเกม)
4. หลัง GO-3 จบ: ตัดสิน G-3 (คง DLL ใหม่หรือ rollback) → แพ็กเกจถัดไป (V2-A / SP5-T0 / V1) ตามลำดับใน APPROVAL-REQUESTS — 4 สายงานห้ามรวมเซสชันเกมเดียวกัน
5. ตรวจสอบสถานะ: `save-split status`, hash ไฟล์ mod ทั้งสาม, `git log` ทั้งสอง repo — ค่าอ้างอิงอยู่ใน (ช)

## (ฉ) สถานะ git ก่อน/หลัง

| repo | ก่อน | หลัง |
|---|---|---|
| mod repo (`C:\Users\memph\LongYinCoop`, master) | HEAD `390e856` · 53 untracked, 0 modified | HEAD `ec78a8b` (+7 commits: `061628f`, `91c276b`, `a7ceccd`, `b1418a3`, `16a38c3`, `ea3c7dd`, `ec78a8b`) · untracked 60 (เพิ่มจากไฟล์ evidence ใหม่ที่ไม่ commit: AUTO-*, S2a4-GO3-*, TIMEstatic2-* ส่วน TSV/txt) · 0 modified tracked |
| game repo (main) | HEAD `c672dc2` · M STAGEPLAN.md, D battle-notes/worldmap-notes, m il2cpp-frida-mcp, untracked เดิม | HEAD `246792e` (+1 commit: AGENTS.md เดียว) · การแก้เดิมทั้งหมดคงสภาพ ไม่แตะ |

## (ช) hash ก่อน/หลัง (ต้องเหมือนกันทั้งหมด — ตรงแล้ว)

| รายการ | ก่อน (02:39) | หลัง (~03:1x) | เหมือนกัน |
|---|---|---|---|
| `Mods\LongYinCoop.dll` | `1b4c17aa6a934b25c884f62dbab59a795a8bbadf60fd2fc869ee0927fb3f0177` (316,416 B) | เดียวกัน | ✓ |
| `UserData\LongYinCoop\net.cfg` | `12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8` (444 B) | เดียวกัน | ✓ |
| `UserData\MelonPreferences.cfg` | `269b571c1b1572b7b525cda3e8a27e09b732b8eb31bdfe290269c8ed62a098d1` | เดียวกัน | ✓ |
| digest `Save` | `df8e5a11beed` (57 ไฟล์, leftovers none) | `df8e5a11beed` (57 ไฟล์, leftovers none) | ✓ (ค่าคงที่ — แต่ ≠ baseline อนุมัติ ตาม DEVIATION-1) |
| hash เกม 2 ไฟล์ | `33c56def…` / `451311d4…` ตรง VERSION.md | ไม่วัดซ้ำตอนจบ (เกมไม่เคยถูกเปิดระหว่าง session — เกม process ไม่พบทั้งต้น/ปลาย) | ✓ (โดยเหตุ) |
| เกมรัน? | ไม่ | ไม่ | ✓ |

- session นี้ **ไม่เปิดเกม · ไม่ส่ง IPC · ไม่ deploy · ไม่แตะเซฟ/pref/net.cfg/Steam · ไม่ tag/push/amend · ไม่ใช้ git add -A** — ทุก commit ระบุ path รายไฟล์ และตรวจ `git diff --cached --name-only` ก่อน commit ทุกครั้ง
- ไฟล์ disassembly/TSV ใหญ่/bodies ของเกม**ไม่ถูก commit** ทุกชุด (TIMEstatic2 commit เฉพาะ .md; SP5 gameassembly.disasm.txt กันด้วย .gitignore:68)
- ค่า `secret` ใน net.cfg **ไม่ถูกคัดลอกลงเอกสารใด** ของ session นี้
