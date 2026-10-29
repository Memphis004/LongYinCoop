# คำขออนุมัติ stage N4 — SP5-NPC (T2–T4 ที่ระดับ Light ×0.5, `npcPolicy=light`) — **ร่าง ยังไม่ดำเนินการ**

- **label:** `SP5-NPC-N4-request-20261030` · อ้างแผน `plans/sp5-npc-reduce-plan-draft.md` §7 แถว N4 +
  §5 T2–T4 + design `evidence/SP5design-20261027/sp5-design-report.md` §6.2 T2–T4 ·
  มติที่ปิดแล้ว: ระดับ = **Light ×0.5** (`Sp5NpcScales.LightHeroScale = 0.5` — ไม่ต้องแก้โค้ด) ·
  refill = **ไม่ใช้ในรอบแรก** (G4/G5 activate เฉพาะ Mid/Strong — ที่ light ไม่ activate ตามโค้ด
  provenance N2) · key `npcPolicy` = **ตั้ง "light" เฉพาะช่วงรันของ N4** (คง off นอกช่วง — มติ
  2026-10-30 "ตั้งระดับเฉพาะตอนรัน") · ฐานเทียบจาก N3: census Heros = deterministic 883
  (protag 1 / spe 170 / sect 562 / wanderer 150 / f2 0) ทุกจุดข้อมูล · TempHeros = RNG 79–87
- **สิ่งที่ขออนุมัติ:** รัน stage N4 เท่านั้น — **N5–N6 ยังไม่รวม** (วลีสั่งแยกทุก stage ตามแผน §7)

## ขอบเขตกลไกที่ activate ที่ light

- **G1 เท่านั้น** = prefix ย่อ `heroNum` ต่อ entry (round half-up) + **finalizer คืนค่าเดิมทุกทางออก
  รวม exception** (restore ภายใน window ของ creation round) — ครั้งแรกที่ gate **เขียนจริง** บนเกม
  (N3 = Decide Off ไม่มีการเขียนเลย, N2 = เทสต์ offline ล้วน)
- ผลคาดหวังเชิงตาราง: heroNum 4 แถว `[4,4,3,3,2,1]/[5,4,4,3,3,2]/[5,5,4,4,3,2]/[6,5,5,4,4,3]`
  → `[2,2,2,2,1,1]/[3,2,2,2,2,1]/[3,3,2,2,2,1]/[3,3,3,2,2,2]` (**Σ 88→50** ตาม provenance N2)
- G4/G5 ไม่ activate · B1 = ไม่มีโค้ด · กติกา 8 (typed interop) ครอบ — ถ้า typed write ล้มต้องใช้
  raw offset = **หยุดรายงาน ห้ามดำเนินการต่อ**

## ผลวัดที่ต้องการ + เกณฑ์ผ่าน/ไม่ผ่าน (รายเกณฑ์)

### T2 — PROTECTED ครบ (zero-tolerance — ตัวใดหาย/ผิดตัว = NO-GO หยุดทันที)

| รายการ | แหล่งวัด | สถานะเครื่องมือ |
|---|---|---|
| spe = 170 | worldcount hist + `speDbTableSize` | ✅ probe ครอบแล้ว |
| ตัวเอก heroID=0 | factory.call ตัวแรก (heroID) + hist protag=1 | ✅ probe ครอบแล้ว |
| 掌门 30/30 · LoveableSpeHero 57 ครบ · `Teacher=1` จุดเรียนได้ตัวเดิม · BirthSetting ถูกต้อง | — | ⚠️ **probe ปัจจุบันไม่มี field ย่อยเหล่านี้** (hist `spe`=170 รวมอยู่แล้ว ไม่แยก) ⇒ **ต้องเลือกทางวัดในวลีสั่ง** (ด้านล่าง) |

**ทางเลือกวัด T2 ส่วนที่ probe ไม่ครอบ (ตัดสินในวลีสั่ง):**
- **(ก) ตรวจจอ/มือ** — นับจาก UI เกม (จำกัด: Teacher/Loveable ตรวจจอยาก, ไม่แน่น)
- **(ข) ขั้นเตรียม offline ก่อน N4** — เพิ่ม census field ใน `Sp5T0Probe` (นับ 掌门/loveable/teacher
  ต่อรอบ) + build/provenance ใหม่ = **งานโค้ดใหม่ ต้องอนุมัติแยก** (กติกา 1/8) แล้ว N4 deploy
  build ใหม่นั้นแทน `2b59e46c…` — **ข้อเสนอ agent: (ข)** เพราะ T2 เป็น zero-tolerance และ N4
  ต้อง deploy อยู่แล้ว ต้นทุนเพิ่มคืองาน offline เท่านั้น
- **(ค) สืบจากข้อมูลเดิม** — ตรวจว่า `factory.call` (heroID/db/belongForceID) map หา 掌门/loveable
  ได้ครบหรือไม่ (offline ไม่มีค่าใช้จ่าย แต่ไม่รับประกันครบ — ทำคู่กับ (ก) หรือ (ข) ได้)

### T3 — f2 (forceID −2) คงพฤติกรรม

- เกณฑ์ PASS: `f2 = 0` (ตรงจุดข้อมูลเดิมทั้ง 4: T0 + N3×3) + ไม่มี error/phase ผิดปกติ ·
  NO-GO: f2 เปลี่ยน/เกิด error ใน bucket ใด

### T4 — generic ลดจริงจากต้นทาง (census ต่อสำนัก/ระดับ)

| รายการวัด | เกณฑ์ PASS (เสนอ — **band ต้องอนุมัติในวลีสั่ง** ตามแผน §5 "ค่าเบี่ยงเกณฑ์กำหนดตอนอนุมัติระดับ") |
|---|---|
| sect รวมหลังย่อ | **sect_after ∈ [280, 350]** — ครอบสองโมเดล: quota-model ≈ 300–316 (per-force quota มาจาก heroNum rows ย่อแล้ว Σ50) · linear-model ≈ 281 (N1-3) · + noise rounding ±1/force · Heros_after = 321 + sect_after ⇒ ≈ 601–671 (คาดกลาง ~637) · รอบแรก calibrate แล้วปรับ band แคบลงในรอบถัดไป |
| กระจายรายสำนักคงสัดส่วน | per-force after ≈ template ย่อแล้ว ±2 ต่อ band · อันดับขนาดสำนัก (rank) คงสัดส่วน ไม่เบ้ |
| ไม่มีการลบหลังสร้าง | `h` นิ่งหลัง `genenemy.end` · factory C-calls ลดสอดคล้อง (≈ sect_after + 170 + 1 + 150) · A/B = 0 คงเดิม |
| heroNum window | `heroNum.read` ในรอบ = ค่าย่อแล้ว Σ=50 · restore ท้ายรอบกลับ Σ=88 (ตรวจจาก ndjson/heroNum.read รอบถัดไป) |
| NO-GO | เบ้เกิน band / ลดไม่ตรง / ต้องลบ NPC หลังสร้าง / PROTECTED ขาด / census ไม่ปิดบัญชี |

## ขั้นดำเนินการ (เมื่อได้รับวลีสั่ง — ลำดับเดิม N3)

1. session-start read-only (hash เกม/Mods/เซฟ/git ตรง snapshot)
2. **deploy** artifact (ตัดสินในวลีสั่ง: (i) `2b59e46c…` เดิม — ไม่มีงานเตรียม หรือ (ii) build ใหม่
   จากทาง (ข) T2 — ต้องผ่าน N4-prep offline + provenance ก่อน) — **สำรอง `.prev-6a94abb5-<ts>`
   ก่อนทับทุกครั้ง** (rehash หลัง deploy ตรง artifact)
3. **ตั้ง `npcPolicy = light` ใน net.cfg เฉพาะช่วงรัน** — สำรอง `net.cfg.bak-<ts>` (คืน `12541f34…`
   เป๊ะท้ายเซสชัน) + hash ก่อน/หลัง · `Sp5T0=true` ต่อ (ตามตัวเลือก census ในวลีสั่ง)
4. **save-split** `--expected-baseline coop-test/baselines/20261028-071340/savehash.real.before.txt`
   (label `SP5NPC-N4-<ts>`)
5. **ผู้ใช้เปิดเกมเอง ×3 รอบ** (โลกใหม่ ห้ามเซฟ slot) — agent ตรวจ ML log + hash หลังปิดทุกรอบ
   (**ครั้งแรกที่ Decide=Light บนเกมจริง** — ถ้า install ล้ม / patch นอกชุด / พฤติกรรมเบ้ = หยุดทันที)
6. เก็บ evidence + วิเคราะห์ T2–T4 รายเกณฑ์
7. **คืนสภาพ:** restore เซฟ EXACT + rollback DLL + **`npcPolicy` ออกจาก net.cfg (คืน `12541f34…`)** +
   `Sp5T0=false`
8. รายงานรายเกณฑ์ — **ไม่ปิด stage เอง ไม่ tag เอง**

## rollback (ทุกจุด — ตามแผน §6)

1. **config:** ลบ/เปลี่ยน `npcPolicy` กลับ off ระหว่างรอบ = gate กลับ Off ทันที (poll ≤1 s, fail-closed) ·
   โลกที่สร้างแล้วคงตามที่สร้าง (มติ refill ของโลกที่ลดแล้วยังรอ — แต่โลกทดสอบ N4 ทิ้งได้ ไม่เกี่ยว)
2. **build:** rollback DLL/PDB จาก `.prev-<hash>` (hash ยืนยัน)
3. **เซฟ:** `save-split restore` + post-check EXACT + `leftovers: none`
4. **net.cfg:** คืนจาก `.bak` เป๊ะ `12541f34…`

## ความเสี่ยง/UNKNOWN

- **G1 เขียนจริงครั้งแรกบนเกม** — typed write บน interop `List<int>` ภายใน window + finalizer restore
  (provenance N2 ครอบ design; พฤติกรรมจริงยังไม่เคยวัด) — ถ้า restore ไม่ครบจะเห็นจาก `heroNum.read`
  (probe อ่านทุก `LoadAllGameData`) และ census รอบถัดไป
- **band T4 ยังกว้าง** (สองโมเดล 281–316) — รอบแรกของ N4 = calibration โดยตรง
- heroNum restore ผิดพลาด = กระทบโลกที่สร้างบนสำเนาเซฟเท่านั้น (ทิ้งได้) — เซฟจริงไม่เสี่ยงตาม split

## วลีสั่งที่เสนอ

- `เริ่ม N4 session` = T2 ทาง (ก)/(ค) + T3/T4 · deploy `2b59e46c…` เดิม · band T4 = [280, 350] ·
  3 รอบ (default)
- `เริ่ม N4 session พร้อม census เต็ม` = เพิ่มขั้นเตรียม offline ทาง (ข): เพิ่ม field 掌门/loveable/
  teacher ใน probe + build/provenance ใหม่ (แยกอนุมัติงานโค้ด) ก่อน deploy
- **ต้องระบุในวลีสั่ง:** ยอมรับ band T4 [280, 350] หรือแก้ · ทางวัด T2 · จำนวนรอบ (default 3)

**หยุดหลังส่งเอกสาร — ไม่ deploy ไม่ split ไม่เปิดเกม ไม่แก้โค้ด probe จนกว่าวลีสั่งแยก**

> **สถานะมติ 2026-10-30 (append — ข้อความเดิมคงไว้): T2 = ทาง (ข) อนุมัติและทำเสร็จแล้ว (offline).**
> มติผู้ใช้: "(ข) เพิ่ม census field ใน probe offline: 掌门 30, LoveableSpeHero 57, Teacher=1,
> BirthSetting; probe ยังเป็น log-only ห้ามเปลี่ยนตรรกะอื่น · build+provenance ไม่ deploy ·
> เทสต์ชุดเต็มต้องไม่น้อยกว่าเดิม" — **ปฏิบัติครบ:**
> - โค้ด: `Sp5T0Probe.cs` ไฟล์เดียว — event `worldcount` เพิ่ม 4 field อ่านอย่างเดียว
>   (`zhangmen` จาก `ForceData.leader` field ตรง · `loveable` จาก `HeroData.loveAble` +
>   `GDC.loveableSpeHeroList.Count` · `teacher` จาก `HeroData.Teacher` ของสมาชิกสำนัก เทียบ leader
>   ของสำนักตัวเอง · `birth` จาก `StartGameSettingController.Instance.BirthSetting`) + `censusLast`
>   ใน summary.json — **ตรรกะ/ตัวนับ/การจำแนกเดิมไม่ถูกแตะ** (field ใหม่ append ท้าย JSON line) ·
>   ยัง log-only ครบตามขอบเขตเดิม
> - ผลวัด: build Release **0W/0E ×2 determinism** · artifact DLL **`ba1e1dd1…`** (373,760 B) / PDB
>   **`a96a4a38…`** — **ไม่ deploy** (`Mods\` คง `6a94abb5…`) · C# **693 PASS/0 FAIL** (= เดิม) ·
>   python **53 OK + 110 OK** (= เดิม) · static **CLEAN** · `sp5t0_audit.py` sanity-check บน ndjson
>   รอบจริง N3 = PASS เหมือนเดิม
> - provenance: `evidence/SP5NPC-N4prep-20261030-045558/provenance.md`
> - **ผลกระทบต่อคำขอ N3 ด้านบน:** ขั้น 2 (deploy) ของ N4 เมื่อสั่งรัน จะใช้ **artifact ใหม่
>   `ba1e1dd1…` แทน `2b59e46c…`** (ครอบ census field ของ T2 ครบ) · ทางเลือก (ก)/(ค) ของ T2 ด้านบน
>   = ปิดแล้วด้วยมติ (ข) · band T4 [280, 350] + จำนวนรอบ **ยังรออนุมัติในวลีสั่ง N4**
