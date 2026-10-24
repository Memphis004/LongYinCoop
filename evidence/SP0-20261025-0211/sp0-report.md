# SP0 DUMP-only — รายงานผลตาม exit criteria (label `SP0-20261025-0211`)

- แผน: `SP0-full-plan-draft.md` **rev2.1** (อนุมัติ 2026-10-25 · commit `b0660e2`) · วัดวันที่ 2026-10-25
- ขอบเขต: DUMP-only — อ่าน `<เกม>\Il2CppDumper\dump.cs` (sha256 `b983820b23a4…09b5503` · 14,953,011 B · mtime 2026-10-20 00:47 · ยืนันรุ่นเดียวกับหลักฐานเดิมผ่าน `GameDataController` @ `dump.cs:377212` ตรง `battle-notes.md:545`) ด้วย `tools/sp0_dump_parser.py` (read-only) · **ไม่เปิดเกม ไม่เรียก method ไม่ build/deploy ไม่แตะเซฟ/config/Steam**
- Deliverable: `singleton-census.md` (ตาราง census) + `census-candidates.tsv` (330 แถว) + `census-references.tsv` + `parser-integrity.json`

## ผลตาม exit criteria

| # | เกณฑ์ (rev2.1 §7) | ผล | หลักฐาน |
|---|---|---|---|
| 1 | Candidate census ครบ 100% ตามกฎค้นหา + วิธีตรวจซ้ำ | **PASS** — N = **330** (ก=304 · ข=146 · ค=112) จาก game types 1,705 | ตรวจยืนยันอิสระ: ก awk=304 ตรงเป๊ะ · ค awk union=112 ตรงเป๊ะ · ข awk=137 + คลายเหตุต่าง 9 รายครบ (census §1.3) · วิธีตรวจซ้ำใน census §1.1 + คำสั่งด้านล่าง |
| 2 | Metadata references ราย candidate | **PASS** — รายตัวใน `census-references.tsv` (field/prop/param exact+generic, ret แยกเป็น informational, self static, top referencers) | แถวหลักสรุปใน census §2 · data types เสริม §5 |
| 3 | Call-sites แยกชัด | **PASS** — ทุกแถว = `UNAVAILABLE IN DUMP-ONLY` (ไม่มี method bodies ใน dump จึงไม่อ้าง) | census §หัวข้อ + §7 |
| 4 | Battle/Plot/Choose/Craft/Auction คลุมครบ | **PASS** — 8/3/3/2/1 candidates (รายชื่อครบใน census §4) | census §4 |
| 5 | ทุกแถวมีวิธีตรวจ + ป้าย + `ยังไม่วัด (runtime required)` | **PASS** — แถวที่ไม่มีหลักฐาน runtime (HudController, WorkingUIController, SureMenu, GameDataController, hidden singletons 11 ตัว, camera/UI) ติดสถานะครบ | census §2–§3 |
| 6 | verdict "ใช้ร่วมไม่ได้" เฉพาะแถวมีหลักฐาน RUNTIME ≥1 | **PASS** — ไม่มีแถวไหนถูกสรุป "ใช้ร่วมไม่ได้" จาก DUMP ล้วน · แถวที่อ้าง RUNTIME ใช้หลักฐานรอบ `sp0sp7-20261024-225939` ตามป้ายเดิม ไม่อ้างว่าวัดใหม่ | census §2 (คอลัมน์ป้ายระบุที่มา) |
| 7 | สรุปราย spike = candidate + refs + subsystem + ต้องตรวจต่อ · **ห้ามสรุป "บล็อก"** | **PASS** — สรุปด้านล่างไม่มีข้อสรุป "บล็อก" ใด ๆ · รายการ runtime-required ระบุครบ | §สรุปราย spike ด้านล่าง |
| 8 | ความบริสุทธิ์ของรอบ | **PASS** — ก่อน/หลังรอบ: hash `GameAssembly.dll` `33c56def…` + `global-metadata.dat` `451311d4…` ตรง `VERSION.md` · เกมไม่รัน · **ไม่มี MelonLoader log ใหม่** (ล่าสุดยังเป็น `26-10-25_0-23-2.log` ก่อนรอบ) · Save 57 ไฟล์ digest `7332fc93eb98` EXACT · `leftovers: none` · `net.cfg` `12541f34c633…` · `MelonPreferences.cfg` `269b571c…` คงเดิม · ไม่ build/deploy DLL | บันทึก pre/post ใน `session-purity.md` |

**verdict รอบนี้ = `GO-DUMP-ONLY`** (ข้อ 1–8 ผ่านครบ) — เป็น verdict ของ deliverable DUMP-only เท่านั้น **แยกออกจากการปิด SP0 census ทุกชั้น** ตามมติ Q1 · ไม่ถือเป็นการอนุมัติ "ช่วง 1" runtime investigation หรือ implementation ใด ๆ (มติ Q3)

## สรุปราย spike SP1–SP7 (exit 7 — candidate + metadata refs + subsystem + ต้องตรวจต่อ)

- **SP1 (โครง/สถาปัตยกรรม):** 330 candidates, singleton pattern เต็ม (ก,ข,ค) 101 ตัว · controller หลักเข้าถึงผ่าน static accessor ไม่ผ่าน field ของ type อื่น ⇒ ต้องตรวจต่อ: call-site จริง (**runtime required**) — ชั้น A ไม่สามารถแยก flow ใดแตะตัวไหน
- **SP2 (world/save):** `WorldData` container เดี่ยวใน `GameController.worldData` (field refs 2) · **`GameDataController.Save(int)`/`Load(int)`/`GetSaveDataPath`×2 = ยืนยันชื่อ candidate ที่เอกสารเดิมพูดถึงแล้ว [DUMP — ไม่ใช่ของใหม่]** ต้อง hook ยืนยันเป็นจุดเขียนเซฟจริง (runtime required) · `GameController.Save` ไม่มีจริง (ยืนยันซ้ำ)
- **SP3–SP5 (spawn/hero/sect):** `HeroData` = type ที่ถูกอ้างหนักสุด (field 41+49generic, param 235+35) · ไม่มี type `Player` — ตัวเอก = `WorldData.Player()` คืน `HeroData` + heroID 0 · จุดสร้าง heroID 0 (**runtime required** — SP7 ข้อ 1)
- **SP6 (battle):** `BattleController` ก,ข,ค (166m) · `BattleUnit` ไม่ใช่ candidate (instance ปกติ, ถูกอ้าง field 11/param 26 ผ่าน BattleController 24) · ผู้เล่นเข้าฉากผ่านอะไร (**runtime required**)
- **SP7 (spawn trace):** ตาราง census ชี้เป้า hook รอบหน้า: caller เหนือ `HeroData.JoinForce` (ring-buffer v4) · จุดสร้าง heroID 0 · coverage จริงของ PlotController 1526m (**runtime required ทั้งหมด**)

**ข้อสรุป "บล็อก": ไม่มี** — ไม่มีแถวใดมีหลักฐาน runtime ว่าบล็อก spike ใด (การอ้าง singleton เพียงอย่างเดียวห้ามใช้สรุปตามกติกา)

## งบพยายาม (มติ Q6) — บัญชีจริง + ruling ผู้ใช้

- **Work sessions: 1** · DUMP attempts ต่อ exit criterion: การตอบแต่ละข้อมาจากรอบรันสุดท้ายของ parser 1 รอบ + การตรวจยืนยันอิสระ (awk) 1–2 ครั้งต่อข้อ = ไม่เกิน 2
- **Ruling ผู้ใช้ (2026-10-25):** parser-development iterations **8 รอบ**ได้รับการยอมรับ **เฉพาะในรอบนี้** ในฐานะ**การพัฒนาเครื่องมือ ไม่ใช่ census attempts ที่ใช้ตัดสิน exit criteria** · เงื่อนไข: (i) ไม่มีการรับรองผลแต่ละรอบเป็นผลตัดสินอย่างเป็นทางการ (ii) ผลสุดท้ายต้องผ่าน independent verification — ผ่านแล้ว (ก 304/304 · ค 112/112 · ข 146=137+9 คลายเหตุครบ) · (iii) รายงานจำนวนรอบตามจริง ห้ามซ่อนหรือเปลี่ยนประวัติ — บันทึกแล้วในหัวข้อนี้
- รายละเอียด 8 รอบ (แก้ regex จุด): image name prefix → ref-attribution keys → `\{{` escape ใน METHOD_RE → col-0 braces/attribute lines → `/* */` comment blocks → const fields → ชื่อ compiler-generated `<..>`/`$` → name-token เปิด + static-array-init suffix — ทุกรอบอ่าน `dump.cs` ไฟล์เดิมอย่างเดียว ไม่มี side effect อื่น · ตัวเลขที่ใช้ตัดสินมาจากรอบสุดท้าย (`unparsed_count=0`) เท่านั้น

## วิธีตรวจซ้ำ (reproducible)

```
# ตัวเลขหลัก (ต้องใช้ grep ที่ pattern ไม่ขึ้นต้นด้วย '/'; ugrep ในเครื่องนี้ตี '//...' เป็น option)
grep -c "TypeDefIndex:" "<เกม>/Il2CppDumper/dump.cs"        # 8877

# census ทั้งชุด (parser read-only — ให้ผล + parser-integrity.json)
python tools/sp0_dump_parser.py --dump "<เกม>/Il2CppDumper/dump.cs" --outdir <dir>

# กฎ (ก) อิสระ (awk) — 304 · กฎ (ค) — 112 · คำสั่งเต็มบันทึกใน singleton-census.md §1
```

## Deviation / สิ่งที่ต้องรายงาน (ไม่เงียบ)

1. **ตัวเลข methods ต่างจากหลักฐานรอบก่อน — อธิบายที่มาได้แล้ว 2/3 (เก็บทั้งสองชุด ห้ามเขียนทับเดิม):** ตัวเลขรอบก่อน (244/186/1530…) = **จำนวน method + บรรทัด attribute แบบ `\t[Xxx] // RVA:`** — ยืนยันเป๊ะ GameController 241+3=244 · BattleController 166+20=186 · ส่วน PlotController วิธีเดียวกันได้ 1526+5=1531 ต่างจากเดิม 1 (อธิบายไม่ได้ — น่าจะ off-by-one ของรอบก่อน วิธีนับเดิมไม่ถูกบันทึก) · ตัวเลขรอบนี้นับ 3 ทางอิสระให้ตรงกันเอง · รายละเอียดเต็ม + ตาราง: census §1.4
2. `GameDataController.Save/Load` **ไม่ใช่ข้อค้นพบใหม่** — เอกสารเดิมพูดถึงแล้ว (`battle-notes.md:545` [DUMP], `STAGEPLAN.md:440`, addendum กติกา 6) — รอบนี้คือการยืนยันชื่อคลาส/เมธอด candidate ที่ควรตรวจต่อ · DUMP เพียงอย่างเดียวยังไม่พิสูจน์ว่าเป็นจุดเขียนเซฟที่ใช้จริง · พบว่า `STAGEPLAN.md:440` เขียน Save "2 overload" แต่ dump มี `Save` 1 overload (ที่ 2 overload คือ `GetSaveDataPath`) — ควรแก้เมื่อมีมติ
3. พบ `BattlePrepareSpellController` เพิ่มจาก list รอบก่อน (census §6)
4. grep ของระบบนี้เป็น **ugrep** — pattern ที่ขึ้นต้นด้วย `//` ถูกตีเป็น option; คำสั่งตรวจซ้ำทั้งหมดเลี่ยงรูปนั้นแล้ว

## สิ่งที่ยังไม่รู้ / ต้องรอบ runtime (ไม่ขยายเอง)

- call-site จริงของ singleton ทุกตัว (flow ใดแตะตัวไหน) · heap/lifetime ของ hidden singletons 11 ตัวและ HudController/WorkingUIController/SureMenu · `GameDataController.Save` เป็นจุด save จริงหรือไม่ · จุดสร้าง heroID 0 · coverage จริงของ PlotController — **ทั้งหมด runtime required ต้องแผนย่อย + save-split/preflight + อนุมัติแยก**

## ไฟล์ของรอบนี้ (ยังไม่ commit — รอ review)

`evidence/SP0-20261025-0211/`: `singleton-census.md` · `sp0-report.md` (ไฟล์นี้) · `session-purity.md` · `census-candidates.tsv` · `census-references.tsv` · `parser-integrity.json` · และ `tools/sp0_dump_parser.py` (ไฟล์ใหม่ใน mod repo)

**ห้ามถือว่าการอนุมัติ SP0 ครั้งนี้เป็นการอนุมัติ Stage ถัดไปโดยอัตโนมัติ**
