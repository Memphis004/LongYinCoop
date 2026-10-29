# ร่างเสนอต่อจาก N4 รอบ 1 — นิยาม T2 ข้อ loveable ใหม่ + จุดอ่าน heroNum ใน probe — **DRAFT รออนุมัติ ยังไม่เขียนโค้ด**

- **label:** `SP5-NPC-N4-followup-draft-20261030` · ต้นทาง: มติผู้ใช้ (offline session 2026-10-30
  ~05:45) · ผลสืบ `evidence/SP5NPC-N4-20261030-052251/loveable-investigation.md` · สถานะ: **ทั้งสอง
  ร่างยังไม่มีสิทธิ์ดำเนินการ** — เขียนโค้ดได้เฉพาะเมื่อผู้ใช้อนุมัติร่าง (งานโค้ดต้องอนุมัติแยก
  ตามกติกา 1/8) · อ้างแผน `plans/sp5-npc-n4-approval-request.md` (N4 รอบ 1 หยุดที่ T2)

## ร่าง 1 — นิยาม T2 ข้อ loveable ใหม่ (แทน `loveable.heroCount`)

**ที่มา:** flag `HeroData.loveAble` ไม่ใช่การเป็นสมาชิก list 57 (หลักฐาน: RUNTIME 2 จุด —
744/883 ในโน้ตเดิม, 631/632 ใน N4 รอบ 1) · design เดิม (sp5-design-report.md:290) กำหนดให้ตรวจ
"LoveableSpeHero 57 ครบ" ตั้งแต่ต้น ซึ่งตรวจได้จาก list โดยไม่ต้องใช้ flag

**นิยามที่เสนอ (ตรวจได้ ตรง design เดิม, zero-tolerance คงเดิม):**

| ข้อ | เกณฑ์ | แหล่งวัด |
|---|---|---|
| (a) | `listCount == 57` (GDC.loveableSpeHeroList โหลดครบจาก asset) | probe worldcount field เดิม |
| (b) | **ทุกชื่อใน list 57 ต้องมี HeroData อยู่ใน WorldData.Heros อย่างน้อย 1 ตัว** (match ด้วย `heroName` ตรงตัวอักษร; ถ้าชื่อใน list เป็นแบบ family+given ให้เทียบ `heroName` ก่อน แล้ว fallback `heroFamilyName + heroName`) — รายงานชื่อที่หาไม่เจอทุกตัว (ถ้ามี) แล้วหยุด | probe ใหม่: iterate `List<string>` + loop `Heros` (typed ล้วน ตามกติกา 8) |
| (c) | `loveable.heroCount` (flag) **เลิกใช้ตัดสิน** — ถ้าเก็บต่อให้ติดป้าย observational "generic flag ไม่ใช่ list membership" พร้อมค่าอ้างอิงจากหลักฐาน (744/883, 631/632) | probe เดิม (field คงอยู่ แก้ comment) |

**ข้อจำกัดที่ต้องยอมรับก่อนอนุมัติ:**

1. กติกา match ชื่อ (ข้อ b) **ยังไม่ RUNTIME-confirmed** — รูปแบบชื่อใน list (เช่น 杨思迟) เทียบ
   `heroName`/`heroFamilyName` อย่างไร ต้องยืนยันครั้งแรกตอนรันจริง; ถ้า match ไม่ตรงเพราะรูปแบบชื่อ
   (ไม่ใช่ hero หาย) เป็นกรณี "หยุดรายงาน" เหมือนกัน — ห้ามปรับกติกา match ในเซสชันเอง
2. ชื่อซ้ำ (homonym) ใน Heros: เกณฑ์ (b) ใช้ "มีอยู่ ≥1 ตัว" ไม่ต้อง unique — ถ้าต้องการ unique
  ต้องมี key อื่น (heroID ไม่อยู่ใน list เพราะ list เก็บชื่อ) = ข้อจำกัดโครงข้อมูลเกม ระบุตามจริง
3. list เก็บเป็น `List<string>` ชื่อ — ตรวจ heroID ตรง ๆ ไม่ได้ (ต่างจากตัวเลือก "heroID ทั้ง 57
  ใน list ต้องมีตัวใน Heros" ที่มติยกเป็นตัวอย่าง — ทำไม่ได้เพราะ list ไม่มี heroID)

**ขอบเขตโค้ดถ้าอนุมัติ:** `Sp5T0Probe.cs` ไฟล์เดียว — เพิ่ม 2 ค่าใน worldcount
(`loveable.listPresent` / `loveable.listMissing[cap]` หรือเทียบเท่า) + แก้ comment ข้อ (c) ·
ยัง log-only ทุก byte · เทสต์ schema + unit ของกติกา match (pure ส่วนเทียบชื่อ) · build/provenance
ใหม่ · ไม่ deploy จนกว่าวลีสั่ง N4 รอบ 2

## ร่าง 2 — จุดอ่าน heroNum ใหม่ใน probe (in-window Σ=50 + post-restore Σ=88)

**ที่มา:** N4 รอบ 1 พิสูจน์ shrink ทางอ้อม (sect 311) แต่ไม่มีการวัด Σ ใน window ตรง ๆ —
probe อ่าน heroNum จุดเดียวที่ `genhero.begin` (H2 prefix) ซึ่งเป็น **pre-window** (Σ=88)

**โครง ordering ที่ต้องยึด (Harmony 2.x — จากโค้ด ไม่ใช่การเดา):**

- ทั้ง probe และ gate hook `GameController.GenerateHero` ด้วยกัน (probe H2: prefix/postfix/finalizer ·
  gate: prefix=shrink/finalizer=restore) — order ระหว่าง patch สองชุดบน method เดียว**ไม่ใช่สัญญา
  ที่ deterministic** (observed รอบ N4: probe prefix รันก่อน gate prefix เพราะอ่านได้ Σ=88)
- pipeline ต่อ 1 invocation: prefixes → original → **postfixes → finalizers** (finalizer รันท้ายสุด
  เสมอ) ⇒ probe `genhero.end` worldcount (จาก H2 **postfix** — Sp5T0Probe.cs:395) รัน**ก่อน**
  gate restore ⇒ อ่านแล้วจะเห็น Σ=50 ไม่ใช่ 88 ⇒ **ห้ามใช้ genhero.end เป็นจุด post-restore**

**จุดอ่านที่เสนอ (ไม่มี same-method ordering ambiguity ทั้งคู่):**

| จุด | hook | เหตุผล deterministic | คาดหวัง |
|---|---|---|---|
| (A) in-window | H3 `GenerateHeroData` overload C **call แรกของรอบ** (prefix หรือ postfix จุดเดียว) | method ลูกที่ถูกเรียก**ภายใน** original ของ GenerateHero ⇒ gate prefix (shrink) รันไปแล้ว และ gate finalizer (restore) ยังไม่ถึง — nesting บังคับลำดับ | Σ=**50** + ตารางย่อแล้ว `[2,2,2,2,1,1]/[3,2,2,2,2,1]/[3,3,2,2,2,1]/[3,3,3,2,2,2]` |
| (B) post-restore | H1 `StartNewGame` **postfix** = จุด worldcount `sng.end` เดิม | gate ไม่ patch StartNewGame เลย ⇒ ณ จุดนี้ GenerateHero (พร้อม finalizer restore) คืนไปแล้วทั้งหมด | Σ=**88** + ตารางต้นฉบับ = restore ภายในรอบวัดตรง ๆ |

- อ่านแบบ **one-shot ต่อรอบ** (จุด A = call แรกเท่านั้น, จุด B = ครั้งเดียวท้ายรอบ) — กัน ndjson โต
  (factory C 719 calls ต่อรอบ)
- `heroNum.read` เดิมที่ `genhero.begin` **คงไว้** (pre-window baseline Σ=88) — พร้อม comment
  ระบุว่าเป็นจุดที่ order กับ gate prefix **ไม่ deterministic** (observed Σ=88 รอบ N4 — ถ้าเห็น Σ=50
  ที่จุดนี้รอบหน้า = ordering กลับ ไม่ใช่พฤติกรรม gate เปลี่ยน)
- ความหมายค่าที่ได้เป็น **point-in-time** — ถ้ามีโค้ดเกมอื่นแก้ตาราหลัง restore แล้วก่อน sng.end
  จุด B จะสะท้อนค่า ณ จุดตรวจ (ระบุตามจริงในรายงาน)

**ขอบเขตโค้ดถ้าอนุมัติ:** `Sp5T0Probe.cs` ไฟล์เดียว — emit event ใหม่ 2 รูปแบบ (เสนอ:
`heroNum.window` one-shot จาก GenDataC call แรก + เพิ่มบล็อก `heroNum` ใน worldcount ที่
`sng.end`) — schema เดิม prefix คงเดิมทุก byte, ยัง log-only, typed read ตามกติกา 8, ไม่แตะ
gate/Sp5Npc* ใด · เทสต์: schema round-trip + harness จำนวน events + ตัวนับ one-shot · build
Release 0W/0E determinism ×2 · provenance ใหม่ · เทสต์ชุดเต็มต้องไม่น้อยกว่าเดิม (693/53+110) ·
**ไม่ deploy จนกว่าวลีสั่ง N4 รอบ 2**

## สิ่งที่รอมติผู้ใช้

1. อนุมัติ/แก้ไข ร่าง 1 (นิยาม T2 loveable ใหม่ — รวมกติกา match ชื่อ)
2. อนุมัติ/แก้ไข ร่าง 2 (จุดอ่าน heroNum ใหม่)
3. ถ้าอนุมัติทั้งคู่: ทำ**รวมครั้งเดียว**ใน `Sp5T0Probe.cs` (build/provenance เดียว ประหยัดรอบ
   N4prep) หรือแยกก็ได้ตามสั่ง
4. ทางปิด UNKNOWN "ใครเขียน flag true" (ถ้ายังต้องการ): disassembly ของ
   `SetHeroLoveAble`/`GenerateHeroData` = งาน RE อนุมัติแยก

> **สถานะมติ 2026-10-30 (append — ข้อความเดิมคงไว้): อนุมัติทั้ง 2 ร่างตามร่าง และทำเสร็จแล้ว
> (offline — ไม่ deploy).** มติผู้ใช้: ร่าง 1 = listCount=57 + ทุกชื่อใน list 57 ต้องมี HeroData
> ใน Heros match ด้วย heroName (fallback family+given) · log ผล match รายชื่อ (ชื่อที่ match
> ไม่ได้ต้อง log ออกมา) · กติกา match ยังไม่ RUNTIME-confirmed ⇒ **รอบแรกที่ใช้ = การยืนยัน
> กติกา** · เลิกใช้ heroCount ตัดสิน (เก็บเป็นข้อมูลอ้างอิง) · ร่าง 2 = in-window Σ=50 ที่
> GenDataC call แรก / post-restore Σ=88 ที่ sng.end · ห้ามใช้ genhero.end เป็น post-restore ·
> งานโค้ด Sp5T0Probe.cs log-only ห้ามเปลี่ยนตรรกะ gate/hook อื่น ห้าม raw offset · แก้ comment
> เก่าให้ตรงความจริง · build Release determinism ×2 + provenance ไม่ deploy · เทสต์ ≥ เดิม ·
> sp5t0_audit.py ต้องยังอ่าน ndjson รูปเดิมได้ · commit แยก 2 อัน ไม่ push · disassembly หา
> writer ของ loveAble = **ไม่อนุมัติ** (ไม่จำเป็นต่อ N4)
> **ผลจริง:** artifact DLL `476f390e…` (377,344 B) / PDB `e8c44e4f…` (104,744 B) · C# 705/0 ·
> python 53+110 · static CLEAN · audit tool รูปเดิม+รูปใหม่ PASS · provenance
> `evidence/SP5NPC-N4prep2-20261030-062233/` · รายละเอียดการแก้โค้ดตรงตาม §"งานที่ทำ" ของ
> provenance นั้น (ขอบเขตเดิมในเอกสารนี้คงไว้ทุก byte)
