# SP5-NPC Reduce — แผน implement ร่าง (DRAFT — N1 อนุมัติและ**ทำเสร็จแล้ว** · N2–N6 ยังรออนุมัติ)

- **label:** `SP5-NPC-plan-draft-20261030` · **สถานะ:** มติผู้ใช้ 2026-10-30 = GO ทาง (ก) ร่างแผน ·
  **มติถัดมา: อนุมัติเฉพาะ stage N1 (offline ล้วน)** — **มติถัดมาอีก (2026-10-30): อนุมัติเฉพาะ
  stage N2 (offline = hook ต่อ predicate + เทสต์) — N3–N6 ยังไม่อนุมัติ ห้าม deploy ห้ามเปิดเกม** ·
  **N1 = เสร็จแล้ว** (artifact `b6d24d34…` · `evidence/SP5T0-N1-20261030-025854/`) ·
  **N2 = เสร็จแล้ว** (hooks G1/G4/G5 + เทสต์ · artifact `2b59e46c…` ·
  `evidence/SP5NPC-N2-20261030-033852/provenance.md` — **ยังไม่ deploy ทั้งคู่** · พารามิเตอร์
  scale/rounding + มติระดับ/refill/key ที่ bracket ยังไม่ได้เลือก = คงรอ ดู decisions-pending §7.1) ·
  **N3 = เสร็จแล้ว 2026-10-30** (append — ตัวเลข artifact N1/N2 ข้างบนคงตามจริง ยังไม่ deploy ถาวร;
  N3 deploy artifact N2 `2b59e46c…` ชั่วคราวแล้ว rollback ครบ) — T1 ผ่านครบ ×3 รอบ (census
  h=883 ตรง T0 ทุกประเภททุกรอบ · gate install สำเร็จ ×3 · ไม่มี patch นอกชุด · SaveSlot* ไม่ถูกเขียน) ·
  rollback ครบ (DLL กลับ `6a94abb5`, cfg `976ccfb9` เป๊ะ, เซฟ EXACT 0/0/0, net.cfg ไม่แตะ) ·
  tag `stage-N3` · หลักฐาน `evidence/SP5NPC-N3-20261030-035500/session-record.md` ·
  ขั้นถัดไป = N4 (T2–T4, npcPolicy=light) — คำขอ `plans/sp5-npc-n4-approval-request.md` (ร่าง ยังไม่ดำเนินการ) ·
  ตัวเอกอ้างอิง: `evidence/SP5design-20261027/sp5-design-report.md` (design-only, §3–§8) + ผลรอบจริง
  `evidence/SP5T0-run-20261030-012502/round-report.md` (T0 ของ design §6.2 รันแล้ว)
- **ห้ามเสนอ/ตัดสินเลือกระดับ L1/L2/L3 จนกว่าผู้ใช้สั่ง** (มติ 2026-10-30) — **มติถัดมา (2026-10-30):
  เลือกแล้วสำหรับ N3/T4 = Light ×0.5** · **นโยบาย refill = ไม่ใช้ในรอบแรก** (ตรง design §4 แถว L1 —
  G4/G5 ไม่ activate) · **key `npcPolicy` = ยืนยัน default "off"; ตั้งระดับเฉพาะตอนรัน** (T1 ยัง off;
  ตั้ง light เฉพาะตอน N4/T2–T4) — ค่า scale/rounding อื่นยังเป็นพารามิเตอร์ที่ยังไม่ผ่านการวัดจริง ·
  เอกสารคำขออนุมัติ N3 = `plans/sp5-npc-n3-approval-request.md` (ร่างแล้ว ยังไม่ดำเนินการ)
- **กติกาครอบ:** กติกา 1 (ทำตามแผนที่อนุมัติเท่านั้น) · กติกา 5 (สำเนาเซฟ) · กติกา 6 (main thread) ·
  กติกา 8 (typed interop — ห้าม raw offset ถ้าล้มให้หยุดรายงาน) · ห้ามแตะ Steam/anti-cheat/เซฟจริง

## §0 ขอบเขต

- **ใน:** กลไกลดประชากร NPC เริ่มโลก (จุด A ของ design) + นโยบายเติมรายเดือน (จุด C) + นโยบายศัตรุสุ่ม
  (จุด B — แนว B1 "ใช้ปุ่มเกมเอง" ไม่มีโค้ด) + งานย่อย offline §2 + เทสต์ + rollback
- **นอก (ห้ามแตะ ตาม design §7):** SpeHero 170 + name-binding 3 ชั้น, ตัวเอก heroID=0, กลุ่ม forceID −2,
  TempHeros/temp plot hero/NPC ศึก plot/RecruitUI (นโยบายแยกเสมอ), auto-quit ของ
  `ManageHeroAutoQuitAndEnter` (ห้ามปิดทั้ง method), เซฟ/CSV (ไม่เขียนเลย — สิ่งที่เขียน = RAM ของ process),
  factory กลาง `GenerateHeroData_C` (ห้าม gate ครอบจักรวาล), normal mode (default Off, opt-in สองชั้น)

## §1 ข้อมูลตั้งต้นจากรอบ T0 จริง (RUNTIME วัดแล้ว 2026-10-30 — แทนค่า INFERRED ของ design)

- census เริ่มโลก: **Heros 883 = protag 1 + SpeHero 170 + สมาชิกสำนัก 562 + เร่ร่อน 150 + f2 0** ·
  TempHeros 87 (AddTempHero 87 = W3 calls 87) · factory = **overload C เท่านั้น 969 calls** (A/B = 0)
- **ประกาศตามมติ: E1/E2 = UNKNOWN (rev 13)** — โครงตัวเลขสอดคล้อง (883==882+1, 87==87==87) แต่
  `inventoryStability` ภายใน window ไม่มีกลไกพิสูจน์ ⇒ แผนนี้**ไม่อ้าง** pipeline-success; ข้อมูล
  heroNum/census/การจำแนก (E4/E5/E6/E7 ปิดแล้ว) ใช้เป็นฐานได้
- **หมายเหตุบังคับตามมติ: `heroNum` Σ=88 (4 forces × 6 levels) ≠ จำนวนสมาชิกสำนัก 562** —
  ขัดสมมติฐานเดิมของ design §3.1 ที่ว่าลูป #2 สร้างสมาชิกสำนักทั้งหมดจาก heroNum ⇒ **ขอบเขตการลดของ
  A1 (ย่อ heroNum) ยังระบุไม่ได้เต็ม** — ต้องปิดด้วยงานย่อย N1-3 (§2) ก่อนเลือกตัวคูณจริง;
  สมมติฐานที่ต้องตรวจ: สมาชิกสำนักส่วนใหญ่มาจากลูป #3 (`GetRandomBigForceID` → force 0..29 หรือ −1)
  ไม่ใช่ลูป #2 — ปิดได้**จาก NDJSON ที่มีอยู่โดยไม่ต้องเปิดเกม**
- census f2 = 0 รอบนี้ (design INFERRED ≈21) — องค์ประกอบโลกต่างตาม setting; ตัวเลขระดับใหม่ใน §4
  จึงคำนวณจาก composition รอบนี้ + ต้องวัดซ้ำต่อโลกจริง
- patch.audit รอบ T0: inventory เชิงความหมายตรง expected ทุกจุด (บั๊ก string-format ของ comparator
  ทำให้ allMatch=false — งานย่อย N1-1) · probe ติด 7/ถอน ownedLeft=0 · ML log 0 ERROR

## §2 งานย่อย offline (ไม่ต้อง deploy/เปิดเกม/split — ทำได้ทันทีเมื่ออนุมัติแผนนี้)

| # | งาน | ผลลัพธ์ | ผลจริง N1 (2026-10-30 — `evidence/SP5T0-N1-20261030-025854/`) |
|---|---|---|---|
| N1-1 | **แก้บั๊ก comparator ใน `Sp5T0Probe.cs`** — normalize ตัวคั่น (`ns:Type::Method` ↔ `ns.Type.Method`) ก่อนเทียบ expected inventory | patch.audit allMatch รายงานถูกต้องในรอบถัดไป | ✅ `NormalizePatchId` + เทียบ format-insensitive + harness +6 checks |
| N1-2 | **แก้ `tools/sp5t0_audit.py`** ให้อ่าน `perForce` เป็น list 30 ช่อง (ตาม schema จริง) | tool ใช้ได้โดยไม่ต้องพึ่ง analyze_round.py เฉพาะกิจ | ✅ รองรับ list+dict · key ด้วย label `at` (worldcount เขียน t=0) · dH/dT observed · tests 6→10 OK · รัน log จริง: dH=883 dT=87 CLEAN |
| N1-3 | **attribute ลูป #2/#3 จาก NDJSON รอบ T0** (offline — เรียงลำดับ call, heroForceLv, db, postBelongForceID เทียบ heroNum ที่อ่านได้) | ตอบว่า A1 ย่อ heroNum กระทบกี่ call จาก 712 generic — ประมาณขอบเขตการลดจริงของ A1 | ✅ **A1 ครอบ 562/883 (63.6%)** — quota รายสำนักราย band มาจากตาราง heroNum (25/30 fit [+0,+1]) · Light 0.5 คาดลด ~281 ≈ **31.8%** (ตรง ~32% ของ design — linear เป็นข้อสันนิษฐาน คง UNKNOWN จนวัดจริง) |
| N1-4 | **enumerate consumer อื่นของ `forceLevelData`** (static — dump.cs + xref) | รายการ consumer + ผลถ้า heroNum ถูกย่อ | ✅ writer เดียว = LoadAllGameData/CSV · reader ยืนยัน = GenerateHero · unattributed candidates 3 จุด = UNKNOWN (ไม่บล็อก A1) |
| N1-5 | predicate `npcPolicy` + parser (สไตล์ `S2NetConfig` fail-closed) + truth-table tests | โค้ด pure + เทสต์ผ่าน | ✅ `Sp5NpcPolicy` + key `npcPolicy` + tests (grid 80 ชุด + parser) — รวมใน build `b6d24d34…` |

**เทสต์หลัง N1: C# 673 PASS / 0 FAIL (เดิม 650) · python 53 OK + discover 110 OK (เดิม 106) · static CLEAN ·
build 0W/0E determinism ×2 · artifact DLL `b6d24d34…` / PDB `4632b607…` — ยังไม่ deploy**

## §3 กลไก implement (ยึด design §3/§5 — สรุปสิ่งที่จะเขียน)

- **จุด A (เริ่มโลก):** A1 = latch ที่ prefix ของ `GenerateHero`: ถ้า policy เปิด → ย่อ `List<int>
  heroNum` ของ `forceLevelData` แต่ละ entry ด้วยตัวคูณ (เก็บค่าเดิมไว้คืนได้; ค่าโหลดจาก CSV ทุก
  `LoadAllGameData`) — ลูป #1/ตัวเอกไม่มีโค้ดใดแตะ · A2 (budget กิ่ง forceID==−1) เฉพาะเมื่อต้องการลด
  เร่ร่อน + hook ยืนยันจำแนกแล้ว (หลัง N1-3) — forceID −2 ไม่มีงบ ห้ามแตะ
- **จุด B (ศัตรุสุ่ม):** B1 = ไม่มีโค้ด — คู่มือให้ host ตั้ง slider ของเกมต่ำตอนสร้างโลก (ศูนย์ code risk)
- **จุด C (เติมรายเดือน):** L2 = C2 postfix บน `GetRecommendHeroNum`/`GetMaxHeroNum` (เพดานย่อ —
  auto-quit ทำงานต่อ) · L3 = C1 prefix บน `WorldAddNewHero` คืน null (caller 2 site มี null-check
  OBSERVED — **ต้องยืนยัน null-branch ก่อนเปิดจริง**) · กิ่ง 150 (`MinOutforceSimpleHeroNum` อ่าน field
  ตรง) ครอบด้วย C1 เท่านั้น — ห้ามแก้ static field (C5 ตัดทิ้ง)
- **gate 5 จุด (design §5.2):** G1 (ย่อ heroNum) · G2 (budget กิ่ง −1, ถ้ามี) · G3 (B1 = นโยบายไม่ใช่โค้ด) ·
  G4 (C2) · G5 (C1) — ทุก hook fail-closed ใต้ predicate (cfg เสีย/ค่าแปลก/ไม่ใช่ host ⇒ Off = เกมเดิม
  100%) · idiom Apply/Uninstall เดิม (JitProbeW*) · ทุก hook try/catch กลืน + นับ error · กติกา 8
  (typed interop เท่านั้น)

## §4 ระดับการลด — ตัวเลขคาดการณ์ใหม่จาก composition รอบ T0 (INFERRED — ต้องวัดซ้ำจริง)

| ระดับ | กลไก | คาด Heros เริ่มโลก (จาก 883) | เติมรายเดือน |
|---|---|---|---|
| L1 Light | A1 heroScale ≈ 0.5 (เฉพาะส่วนที่ N1-3 ยืนยันว่าอ่านจาก heroNum) | ขึ้นกับผล N1-3 — ถ้า heroNum ครอบ ~88 ตัว: ≈839 (ลด ~5%); ถ้าครอบทั้ง 562: ≈601 (ลด ~32%) | คงเดิม (เพดาน 900 คุมอยู่) |
| L2 Mid | A1 + C2 clamp เพดาน ≈ครึ่ง | เช่นเดียวกับ L1 ฝั่งเริ่มโลก | เติมถึงเพดานย่อแล้วหยุด |
| L3 Strong | A1 (สเกลต่ำ) + C1 deny | ≈35–70 สมาชิกสำนัก + 150 เร่ร่อน (ถ้าไม่เปิด A2) + 171 PROTECTED | ไม่เติมใหม่ (auto-quit คงอยู่ — เพดานล่าง UNKNOWN) |

> **การเลือกระดับ + ตัวคูณจริง = มติผู้ใช้แยก** (design §8.2 ข้อ 1 — ยังไม่มีมติ) · แผนนี้ทำให้ครบทั้ง 3
> ระดับในโค้ดเดียว (ระดับ = ค่า policy ใน net.cfg) เพื่อไม่ต้อง build ใหม่ต่อระดับ

## §5 เกณฑ์ผ่าน/ไม่ผ่าน

**Static (ทำก่อน deploy ทุก stage):** S1 truth-table fail-closed ครบ · S2 parser (ต่อชุด 242 เคส) ·
S3 hook inventory ตรงรายการ §3 (ไม่มี hook นอกรายการ, ไม่มี hook ใน save/load path) · S4 phase-guard
A2 (ถ้ามี) · S5 re-resolve target ด้วย interop ล้วน (RVA SP5body ใช้เทียบเท่านั้น) · S6 ไม่แตะเขตต้องห้าม ·
S7 ชุดเทสต์เดิม PASS ทั้งหมด (S2 ไม่ถูกกระทบ) · build Release 0W/0E · provenance READY

**Runtime (ทุกข้อต้องอนุมัติแยก — บนสำเนาเซฟ):** T0 = **ปิดแล้ว** (รอบ 20261030 — รับ E4/E5/E6/E7;
E1/E2 คง UNKNOWN ตามประกาศ §1) · T1 normal mode off: ไม่มี patch ติด + census ใน band RNG (≥3 รอบ) ·
T2 PROTECTED ครบ (spe 170, 掌门 30, ตัวเอก heroID=0, LoveableSpeHero 57) · T3 f2 คงพฤติกรรม ·
T4 generic ลดตามระดับ ±band, กระจายรายสำนักคงสัดส่วน (ค่าเบี่ยงเกณฑ์กำหนดตอนอนุมัติระดับ), ไม่มีการลบหลังสร้าง ·
T5 ≥25 วัน ข้าม ChangeMonth ≥2: ไม่เด้งกลับเหนือเพดานที่ตั้ง · T6 plot ≥20 + mission ≥20: 0 dangling-ID ·
T7 dict/list/ID สอดคล้อง · T8 save/load/save digest นิ่ง · T9 guest ตรง host (Div-A=0 — ผูก VPN/guest
ที่ยังค้าง) · T10 rollback จริง: ปิด gate กลาง session โลกเดินต่อ ไม่ crash

**NO-GO (หยุดรายงานทันที):** path ที่สองปรากฏ (factory=0 แต่ Heros เต็ม — กรณี SP4 รอบ 3) · การลด
ต้องใช้การลบ NPC หลังสร้าง · null-branch ของ `WorldAddNewHero` ไม่ปลอดภัย · typed read ล้มต้องใช้
raw offset · census PROTECTED ขาด/ผิดตัว · soft-lock/crash · เซฟ hash เปลี่ยนนอกแผน

## §6 แผน rollback

1. **ระดับ config:** `npcPolicy=off` (หรือลบ key) = gate Off — hook หลุดหมด (Apply/Uninstall idiom
   + ตรวจ ownedPatches คืน 0) · parser fail-closed: ไฟล์เสีย = Off
2. **ระดับ build:** rollback DLL/PDB เป็น `.prev-<hash>` ที่สำรองทุก deploy (idiom เดิม)
3. **ระดับโลก:** โลกที่สร้างบน policy เปิด เมื่อ gate ปิด = ประชากรคงอยู่ตามที่สร้าง, refill กลับสู่เกมเดิม
   — **มติ §8.2 ข้อ 2 (ยอมรับ refill กลับหรือไม่) ยังรอ** ก่อนใช้งานจริงกับโลกที่คุมสถิติ
4. **ระดับเซฟ:** ทุกรอบทดสอบบนสำเนา (split + restore EXACT + `leftovers: none`) — เกมไม่เขียนเซฟเพิ่มจาก
   การเล่นปกติของผู้ใช้บนสำเนาเท่านั้น

## §7 ขั้นที่ต้อง deploy / เปิดเกม / split (แยกชัดตามมติ)

| Stage | งาน | deploy | split | เปิดเกม |
|---|---|---|---|---|
| N1 (offline) | งานย่อย §2 ครบ + predicate/parser + เทสต์ offline | ไม่ | ไม่ | ไม่ |
| N2 (offline) | **เสร็จแล้ว 2026-10-30** — implement hooks G1/G4/G5 (+negative tests; **G2 ไม่ implement — ยังไม่อนุมัติ**) · artifact `2b59e46c…` · C# 693/0 · พารามิเตอร์/ข้อจำกัด = `evidence/SP5NPC-N2-20261030-033852/provenance.md` | ไม่ (ยังไม่ deploy) | ไม่ | ไม่ |
| N3 (runtime) | **เสร็จแล้ว 2026-10-30 (append)** — T1 normal mode (gate off) ผ่านครบ ×3 รอบ · ผลรายเกณฑ์+คืนสภาพครบ = `evidence/SP5NPC-N3-20261030-035500/session-record.md` · tag `stage-N3` | ใช่ (+สำรอง .prev) | ใช่ | ใช่ (ผู้ใช้เปิดเอง ×3) |
| N4 (runtime) | T2–T4 ที่ระดับที่อนุมัติ | ใช่ | ใช่ | ใช่ |
| N5 (runtime) | T5–T7 (ยาว — อาจแบ่งหลาย session) | ใช่ | ใช่ | ใช่ |
| N6 (runtime) | T8 + T10 (T9 เมื่อมี guest/VPN) | ใช่ | ใช่ | ใช่ |

- แต่ละ stage = แผนย่อย + รายงานผลเทียบ exit criteria + tag ตามกติกา 2 · ห้ามรวมหลาย stage ใน
  เซสชันเกมเดียวโดยไม่มีมติ · ลำดับ STAGEPLAN ให้ผู้ใช้วาง (design §8.2 ข้อ 5 — งานนี้ = write path
  แรกของโมด)

## §8 ยังรอมติ (ห้ามเริ่มเอง)

1. เลือกระดับ L1/L2/L3 + ตัวคูณจริง (หลัง N1-3 รายงานขอบเขตจริงของ A1) — ก่อน N4
2. นโยบาย refill ของโลกที่ลดแล้วเมื่อ gate ปิด (มติ §8.2 ข้อ 2 ของ design)
3. ชื่อ/ค่า key `npcPolicy` ใน net.cfg ยืนยันก่อน N2
4. ตำแหน่ง stage ใน STAGEPLAN + วลีสั่งแยกต่อ stage
5. T9 ผูกกับ VPN/เครื่อง guest ที่ยังค้างเดิม

> **สถานะมติ 2026-10-30 (append — ข้อความเดิมคงไว้):** ข้อ 1 ✅ = **Light ×0.5** สำหรับ N3/T4 ·
> ข้อ 2 (เฉพาะรอบแรก) ✅ = **refill ไม่ใช้ในรอบแรก** (design L1: C off — นโยบาย refill ของโลกที่ลดแล้ว
> เมื่อ gate ปิด = ยังต้องตัดสินก่อนใช้งานจริงนอกรอบทดสอบ) · ข้อ 3 ✅ = **ยืนยัน default "off";
> ตั้งระดับเฉพาะตอนรัน** · ข้อ 4 (บางส่วน) ✅ = วลีสั่ง N3 เสนอในเอกสารคำขอ — ตำแหน่ง STAGEPLAN
> ยังรอ · ข้อ 5 ยังรอ (T9 ไม่อยู่ใน N3)

**หยุดหลังส่งแผน — ห้ามเขียนโค้ด implement/ห้าม deploy/ห้ามเปิดเกมจนกว่าอนุมัติแผนนี้ + วลีสั่งแยกต่อ stage**

> **สถานะ stage N3 = ปิดแล้ว 2026-10-30 (append — ตามมติผู้ใช้รับรายงาน T1 ผ่านครบ ×3 รอบ):**
> รันจริง label `SP5NPC-N3-20261030-035500` (วลี `เริ่ม N3 session พร้อม Sp5T0` — เปิด census เชิงตัวเลข
> ตามตัวเลือกในคำขอ) · deploy artifact N2 `2b59e46c…`/`857cd1b8…` (สำรอง `.prev-6a94abb5-20261030-035500`)
> · split `--expected-baseline 20261028-071340` ตรง 0/0/0 · เกมเปิด ×3 โดยผู้ใช้ (pid 30584/35688/43836)
> · **T1 = PASS ทุกเกณฑ์ทุกรอบ:** gate install สำเร็จ ×3 (ไม่มี install ล้ม) · probe 7 patches +
> `outside=0 unattributed=0 ownedLeft=0 errors=0` ×3 · census `h=883` ตรง baseline T0 **ทุกประเภท
> ทุกรอบ** (protag 1 / spe 170 / sect 562 / wanderer 150 / f2 0) · heroNum Σ=88 ไม่ถูกย่อ · `h` นิ่ง
> ตลอด (ไม่มีการลบหลังสร้าง) · TempHeros 81/79/84 เทียบ T0=87 = RNG worldgen · factory เฉพาะ overload
> C (963/961/966 · A/B=0) · `SaveSlot*` ไม่ถูกเขียนเลย · hash เกม/Mods ตรงทุกจุดวัด · คืนสภาพครบ:
> restore EXACT 0/0/0 (57 ไฟล์ `df8e5a11beed` + leftovers none) · DLL กลับ `6a94abb5`/`856d3bed` ·
> `Sp5T0=false` (cfg กลับ `976ccfb9` เป๊ะ) · `net.cfg` ไม่ถูกแตะ (ไม่มี key `npcPolicy` = off ตลอด
> stage ตามมติ) · **ฐานเทียบ N4 ที่ได้เพิ่ม:** Heros census = deterministic ตามโควตา (883 เท่ากัน 4 จุด
> ข้อมูล T0+N3×3) · TempHeros = ตัวแปร RNG (79–87) ไม่ใช่ตัวชี้วัด gate · tag `stage-N3` (annotated,
> mod repo) สร้างตาม convention `stage-<stage-id>` · ขั้นถัดไป = **N4 (T2–T4, npcPolicy=light)** —
> คำขอ `plans/sp5-npc-n4-approval-request.md` (ร่างแล้ว **ยังไม่ดำเนินการ** รอวลีสั่งแยก) · N5–N6
> ยังไม่อนุมัติ
