# คำขออนุมัติ stage N5 — SP5-NPC (T5–T7 บนโลกที่ลดแล้ว ที่ Light ×0.5) — **ร่าง ยังไม่ดำเนินการ**

- **label:** `SP5-NPC-N5-request-20261030` · อ้างแผน `plans/sp5-npc-reduce-plan-draft.md` §7 แถว N5
  + §5 T5–T7 + design `evidence/SP5design-20261027/sp5-design-report.md` §6.2 (T5/T6/T7) ·
  ฐานตั้งต้นจาก N4: **โลกทดสอบต้องสร้างใหม่ที่ Light ×0.5** (census ตั้งต้นที่เคยวัด: h=632 =
  protag 1 + spe 170 + sect 311 + wanderer 150 + f2 0 · TempHeros RNG 79–89) · ระดับ = **Light**
  (มติเดิม — N6/Mid/Strong ยังไม่อนุมัติ) · **สิ่งที่ขออนุมัติ: รัน stage N5 เท่านั้น** (วลีสั่งแยก)
- **ลักษณะ stage:** runtime ยาว ("≥25 วัน ข้าม ChangeMonth ≥2" — อาจแบ่งหลายเซสชันเกมตาม §7)

## ขอบเขตกลไกที่ activate

- **ไม่มี hook ใหม่** — gate G1 ทำงานเฉพาะตอนสร้างโลก (ย่อ heroNum ที่ prefix ของ GenerateHero
  + finalizer คืน) — พฤติกรรม N4 ยืนยันแล้ว · ระหว่าง T5–T7 gate เปิดคงเดิม (npcPolicy=light)
  แต่ไม่มีจุดตัดสินใหม่นอก creation window · G4/G5 ยังไม่ activate (Light) · **งานโค้ดใหม่ที่อาจ
  จำเป็น = ตัววัด T7 (ด้านล่าง) — ต้องอนุมัติแยกตามกติกา 1/8 ถ้าเลือกทาง (ข)**

## ผลวัดที่ต้องการ + เกณฑ์ผ่าน/ไม่ผ่าน (รายเกณฑ์ — band ยังรออนุมัติในวลีสั่ง)

### T5 — ไม่เด้งกลับ (≥25 วัน ข้าม ChangeMonth ≥2 ครั้ง)

| รายการวัด | เกณฑ์ PASS (เสนอ) | NO-GO |
|---|---|---|
| Heros census ระหว่างเดิน 25 วัน (worldcount ต่อจุดเปลี่ยนสำคัญ) | **ไม่วิ่งกลับสู่ 900 / ไม่เกินเพดานเกมเดิม** (L1: refill ของเกมเองทำงานตามปกติ — gate ไม่มีส่วนหลัง creation) | ประชากรวิ่งกลับ 900 / เกินเพดาน |
| sect รายสำนัก (perForce) | คงรูปแบบ N4 (311 แบ่งตาม N4) + การเติมธรรมชาติของเกมเท่านั้น | เบ้ผิดธรรมชาติ |
| TempHeros / addtemps | band RNG ตามเกม (79–89 จากข้อมูลเดิม — คง observational) | — |
| error/crash/soft-lock ระหว่างเดินยาว | 0 | เกิด |
- วิธีเดิน 25 วัน: ปุ่ม `>>` ของเกม (ChangeDay() วันละครั้ง — RUNTIME ยืนยันแล้วใน AGENTS.md
  ChangeDay section) + งาน/พักตามผู้ใช้เลือก · probe บันทึก worldcount ตลอด

### T6 — plot/mission บนโลกที่ลด (เปิด plot ≥20 + mission ≥20)

| รายการวัด | เกณฑ์ PASS (เสนอ) | NO-GO |
|---|---|---|
| plot ที่เปิด ≥20 | 0 dangling-ID error (probe errors=0 + ML log [ERROR]=0 + ไม่มี dialog พังบนจอ) | dangling เกิด |
| mission ที่เปิด ≥20 | 0 dangling-ID error | เกิด |
| temp plot hero | spawn/ลบได้ตามปกติ (probe tempMatch/tempOther + census) | พัง |
| ศึก plot (ถ้าเกิดขึ้นระหว่างเดิน) | เริ่ม/จบได้ | ศึกค้าง |
- หมายเหตุ: การเปิด plot/mission = การเล่นจริงของผู้ใช้ (agent นับจาก ndjson/log ไม่ได้คนเดียว —
  ผู้ใช้บันทึกจำนวนที่เปิดเองหรือใช้เกณฑ์ "เล่นตามปกติครบ ≥25 วัน" แล้วนับย้อนจาก evidence)

### T7 — โครงสร้าง world data (dict/list/ID)

| รายการวัด | เกณฑ์ PASS (เสนอ) | NO-GO |
|---|---|---|
| `HerosDict`/`TempHerosDict` = list ครบ | dict count == list count (Heros / TempHeros) | dict คลาด |
| ไม่มี ID ชน | heroID ไม่ซ้ำใน Heros | ชน |
| ownHeros/leader ต่อสำนัก | สอดคล้อง (leader ของแต่ละ force มีอยู่จริงใน Heros · ownHeros ครบ) | หาย |
- **ช่องว่างเครื่องมือ (ต้องเลือกทางในวลีสั่ง — เหมือนแนว T2 ของ N4):**
  - **(ก) ตรวจจอ/มือ** — จำกัด ไม่แน่น
  - **(ข) เพิ่ม census field ใน probe** (`HerosDict.Count`, `TempHerosDict.Count`, heroID dedup,
    leader-exists — typed read log-only) + build/provenance ใหม่ = **งานโค้ดใหม่ ต้องอนุมัติแยก**
    (ข้อเสนอ agent เพราะ N4 พิสูจน์แนวนี้แล้ว — ต้นทุน offline เท่านั้น)
  - **(ค) ใช้ ndjson เดิมบางส่วน** — ไม่ครบทุกข้อ T7 (ไม่มี dict fields)

## ขั้นดำเนินการ (เมื่อได้รับวลีสั่ง — ลำดับเดิม N4)

1. session-start read-only (anchor ปัจจุบัน: เซฟ `7e1e65e52659…` @ `20261030-060854` · cfg
   `976ccfb9…` · net.cfg `12541f34…` · Mods `6a94abb5…` · HEAD ณ ตอนนั้น)
2. (ถ้าเลือก T7 ทาง (ข)) ขั้นเตรียม offline: แก้ probe + build/provenance — **ไม่ deploy จนวลีสั่ง**
3. deploy artifact (ถ้ามี build ใหม่ = build นั้น · ถ้าไม่มี = `476f390e…` จาก N4prep2) —
   สำรอง `.prev-6a94abb5-<ts>` ก่อนทับ
4. net.cfg: `enabled=true` + `npcPolicy=light` (สองชั้น ตามที่ผ่านมา) · สำรอง `bak-<ts>` ·
   pref `Sp5T0=true`
5. save-split `--expected-baseline coop-test/baselines/20261030-060854/savehash.real.before.txt`
   (label `SP5NPC-N5-<ts>`)
6. ผู้เปิดเกม = ผู้ใช้ — สร้างโลกใหม่ (BirthSetting เดิม) → **เดิน ≥25 วัน ข้าม ChangeMonth ≥2** →
   เปิด plot ≥20 + mission ≥20 ระหว่างทาง → agent ตรวจหลังปิดแต่ละเซสชันเกม
   **[รอตัดสินในวลีสั่ง: โลก N5 จะเซฟ slot บนสำเนาได้หรือไม่ — ถ้าแบ่งหลายเซสชันเกมต้องเซฟ/โหลด
   slot บนสำเนา (SaveSlot* จะถูกเขียนบนสำเนาเท่านั้น — ต่างจาก N3/N4 ที่ไม่เคยเซฟ — ต้องอนุมัติชัด)
   หรือเดิน 25 วันต่อเนื่องในเซสชันเดียว]**
7. เก็บ evidence + วิเคราะห์ T5–T7 รายเกณฑ์
8. คืนสภาพ: restore เซฟ EXACT + rollback DLL + net.cfg คืน `12541f34…` เป๊ะ + Sp5T0=false
   (ถ้ามีการเซฟ slot บนสำเนา = restore กลับ baseline ปัจจุบันเช่นกัน — โลกทดสอบทิ้งได้)
9. รายงานรายเกณฑ์ — ไม่ปิด stage เอง ไม่ tag เอง

## rollback (ตามแผน §6 — ทุกจุด)

1. config: `npcPolicy` ออกจาก net.cfg = gate กลับ Off ทันที (poll ≤1 s fail-closed)
2. build: rollback DLL/PDB จาก `.prev-<hash>`
3. เซฟ: `save-split restore` + post-check EXACT + `leftovers: none`
4. net.cfg: คืนจาก `.bak` เป๊ะ `12541f34…`

## ความเสี่ยง/UNKNOWN

- **T5 ที่ Light = gate ไม่มีบทบาทหลัง creation** — การเดิน 25 วันวัดพฤติกรรมเกมเดิมบนโลกที่ลด
  (refill ธรรมชาติ, event สำนักเล็กลง — design UNKNOWN ข้อ 7 `SelfForceEventMinHeroNum`) ·
  ผลที่ได้ = หลักฐานความเสถียร ไม่ใช่การพิสูจน์ refill policy (ระดับ Mid/Strong ยังไม่อนุมัติ)
- **T6 การเปิด plot/mission ขึ้นกับการเล่นจริง** — จำนวน ≥20 อาจต้องหลายเซสชันเกม
- **T7 ยังไม่มีเครื่องมือครบ** — รอเลือกทาง (ก)/(ข)/(ค) ในวลีสั่ง
- โลกทดสอบที่เซฟ slot บนสำเนา = SaveSlot* เปลี่ยนบนสำเนาเท่านั้น — restore ท้าย stage กลับ
  baseline ปัจจุบันเสมอ

## วลีสั่งที่เสนอ

- `เริ่ม N5 session` = T5–T7 ที่ Light · ต้องระบุในวลีสั่ง: ทางวัด T7 (ก)/(ข)/(ค) ·
  โลกเซฟ slot บนสำเนาได้หรือไม่ + แบ่งกี่เซสชันเกม · band/เกณฑ์เพิ่มเติมถ้าต้องการแก้
- **ต้องแนบมติที่จำเป็นครบก่อนเริ่ม — ไม่ครบ = agent ปฏิเสธและแสดงรายการที่ขาด**

**หยุดหลังส่งเอกสาร — ไม่ deploy ไม่ split ไม่เปิดเกม ไม่แก้โค้ดจนกว่าวลีสั่งแยก**
