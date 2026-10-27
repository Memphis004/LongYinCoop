# time-static-notes-2 — งาน static อ่านอย่างเดียวเพิ่มเติม (A5 ตามคำสั่งผู้ใช้ 2026-10-28)

> **label:** `TIMEstatic2-20261028-024734` · **อ่านอย่างเดียว 100%** — ไม่เปิดเกม · ไม่ hook · ไม่ build/deploy ·
> ไม่แตะเซฟ/pref/config · ไม่แก้ `time-static-notes.md` เดิม (ไฟล์นี้เป็นไฟล์ใหม่ตามคำสั่ง)
> **hash ยืนยัน session-start 2026-10-28 02:39:** GameAssembly.dll `33c56def…7920c1` · global-metadata.dat
> `451311d4…18bfd0` — ตรง VERSION.md ⇒ RVA anchor ทั้งชุดใช้ได้
> ป้าย: **OBSERVED** = อ่านเองจาก disasm/dump.cs รอบนี้ · **INFERRED** = อนุมาน · **UNKNOWN** = หลักฐานไม่พอ ·
> **DUMP** = จาก dump.cs · ขอบเขตการสแกนกำกับทุกข้อ — ตัวเลขทุกตัวเป็น **lower bound ของช่องทางที่สแกน**
> เครื่องมือ: Python 3.11.9 + pefile 2024.8.26 + capstone 5.0.7 (มีอยู่แล้ว ไม่ติดตั้งเพิ่ม) · ใช้ loader
> `evidence/TIMEstatic-20261027-051647/time_static_common.py` (import เท่านั้น — ไม่แก้) ·
> สคริปต์ของรอบนี้ (อยู่โฟลเดอร์นี้, รันซ้ำได้): `scan_hudpanel.py` · `scan_field_stores.py` ·
> `context_and_cross.py` (+ สคริปต์บริบท inline) · `leaf_order.py` · ผล: `*.tsv`, `needautosave-context.txt`
> **ขอบเขตร่วมของทุกข้อ:** linear disasm เฉพาะช่วง method ของ ScriptMethod (96,106 รายการ) — โค้ดนอกช่วง
> (thunk/padding/native) ไม่ครอบ · owner attribution = "method เริ่มก่อนหน้าใกล้สุด" (อาจผิดถ้ามี foreign code
> แทรก) · ไม่ครอบ register-indirect call, reflection/SendMessage, asset/scene binding ⇒ ทุกผลลัพธ์ = lower bound

---

## (ก) ผู้ตั้ง `needAutoSave` (GameController instance 0x28 — dump.cs:376441 `public bool needAutoSave; // 0x28`)

**ขอบเขต:** linear disasm ทุก method `GameController$$*` (สแกน `mov byte ptr [reg+0x28]` ทุกแบบ base ที่ไม่ใช่
RIP) + สแกนซ้ำทุก method ทั้งไบนารีเพื่อจัดหมวด false positive · ตัวกรอง: base = `[rsp+0x28]`/`[rbp+0x28]` =
**stack local ไม่ใช่ field** (ตัดออก) · origin ของ base register ตามรอย 1 instruction (heuristic — ตรวจบริบท
ด้วยตาทุกจุดสำคัญแล้ว)

**ผู้เขียนที่พบ (instance 0x28) — 6 จุด ใน 3 method [OBSERVED]:**

| # | site | owner | การเขียน | บริบท (OBSERVED) |
|---|---|---|---|---|
| 1 | 0x7E3D4E | `GameController$$Update` | `[this(rbx)+0x28] = 0` | กิ่ง autosave ตาม §2.12 เดิม: guard = `GameDataController$$CanSaveLoad` คืน true (`test al,al; je`) ⇒ **เคลียร์ธงก่อน Save(slot 0)** |
| 2 | 0x792216 | `GameController$$ChangeDay` (leaf 0x790E30) | `[singleton+0x28] = r13b` | base = `[statics slot 0x1D74CE8 + 0xB8]` = **GameController._instance** (singleton path) · ค่าจาก register r13b (ตั้งก่อนหน้านอกหน้าต่างบริบท — ค่าต้นทางยังไม่ pin) |
| 3 | 0x7923D7 | `GameController$$ChangeDay` (leaf) | `[singleton+0x28] = 0` | singleton path เดียวกัน — เคลียร์ธง |
| 4 | 0x792586 | `GameController$$ChangeDay` (leaf) | `[rbx+0x28] = 1` | guard = `test al,al` จาก `List<AISettingType>$$Contains` (0x79257D) ⇒ **ตั้งธงตามเงื่อนไข AI-setting** (semantics เชิงเกมยังไม่ถอด — UNKNOWN) |
| 5 | 0x793F47 | `GameController$$ChangeMonth` (0x793A20) | `[singleton+0x28] = 1` | singleton path — **ตั้งธงตอนขึ้นเดือน** |
| 6 | 0x79404F | `GameController$$ChangeMonth` | `[singleton+0x28] = 0` | singleton path — เคลียร์ธง |

- **สรุปเชิงโครง [OBSERVED]**: ธงถูก**ตั้ง**ใน `ChangeDay` (เงื่อนไข AI-setting, 0x792586) และ `ChangeMonth`
  (0x793F47) และถูก**เคลียร์**ใน leaf (0x7923D7, 0x79404F) และใน `Update` กิ่ง autosave (0x7E3D4E) —
  เข้ากันได้กับ "autosave เกิดระหว่างเล่นเมื่อธงถูกตั้ง" เดิม · ค่าจาก singleton (`[statics+0xB8]`) เป็น
  instance เดียวกับ `this` ของ Update [INFERRED — singleton ของ class เดียว]
- **false positive `mainThreadWaitForLock` — ระบุชัด [DUMP+OBSERVED]:** `public static bool
  mainThreadWaitForLock; // 0x28` (dump.cs:376444) อยู่**คลาสเดียวกัน** แต่เป็น **static** — เขียนผ่าน
  `[statics-slot + 0x28]` ไม่ใช่ `[instance+0x28]` · ในการสแกนรอบนี้ **ไม่พบ** store แบบ statics-origin
  ใน method `GameController$$` เลย (0 จุด) — writer ของ `mainThreadWaitForLock` = **UNKNOWN** (อาจเขียนด้วย
  รูปแบบคำสั่งอื่น/นอกช่วง ScriptMethod/ไม่ถูกเขียนในโค้ด il2cpp) · store แบบ statics-origin ที่ +0x28 ทั้งไบนารี
  มี 2 owner เท่านั้น (`System.Net.HeaderInfoTable$$.cctor`, `System.Xml.Schema.XmlAnyConverter$$ChangeType`
  — statics ของคลาสอื่น ไม่เกี่ยว) ⇒ **ข้อสรุปเก่าที่ชี้ store `[reg+0x28]` ให้ `mainThreadWaitForLock`
  (จากบริบท hook) เป็น false positive ของ offset collision — ยืนยันซ้ำ**: ห้าม attribute store ของ
  instance-0x28 ไปให้ static field และกลับกัน
- **ข้อจำกัด:** (i) writer ที่อยู่ใน method ของคลาส**อื่น**ซึ่งถือ reference ของ GameController instance
  (เขียนผ่าน alias) ไม่สามารถ type-verify แบบ static ได้ด้วยวิธีนี้ ⇒ รายการ 6 จุด = writer ภายใน
  `GameController$$` + singleton path เท่านั้น (lower bound) · (ii) store รูปแบบอื่น (เช่น ผ่าน memset/
  16-byte) ไม่ครอบ · (iii) ค่า r13b ที่ 0x792216 ยังไม่ pin ต้นทาง

## (ข) ครบชุด caller ของ `HudController.HudPanelActive` (0xB09ED0 — dump.cs:380000)

**ขอบเขต:** E8/E9 ทั้งสอง section execute (validate decode ทุกจุด — เครื่องมือ `scan_direct_xrefs`) + FF15
slot scan (slot ที่ชื่อ HudPanelActive = 0 รายการใน ScriptMetadataMethod)

**ผล: พบ caller ตรงเพียง 1 จุด [OBSERVED]:**

| site | kind | owner | ตรวจช่วง |
|---|---|---|---|
| 0x9CB93C | E8 | `WorkingUIController$$Update` | in-range |

- FF15 = 0 จุด · ตรงกับจุดเดียวที่โน้ตเดิม (§2.5) เคยเห็น — ตอนนี้**สแกนเต็มช่องทางแล้ว** ⇒ ภายใต้
  E8/E9/FF15, `HudPanelActive` ถูกเรียกจาก `WorkingUIController$$Update` เท่านั้น (freeze gate ที่ 3 ของปั๊ม)
- **ข้อจำกัด:** caller แบบ indirect/virtual/delegate และ binding ใน asset/scene อยู่นอกช่องทาง ⇒
  "ครบชุด" ใช้ได้เฉพาะความหมาย "ครบภายใต้ช่องทางที่สแกน" (lower bound — กติกาเดียวกับโน้ตเดิม §1)

## (ค) writer ของ `PlotController.plotHappen` (instance 0x18 — dump.cs:382608 `public bool plotHappen; // 0x18`)

**ขอบเขต:** (1) linear disasm ทุก method `PlotController$$*` หา `mov byte ptr [reg+0x18]` (base ไม่ใช่ RIP,
ตัด rsp/rbp stack local) · (2) สแกนซ้ำทุก method ทั้งไบนารี รวบ owner ที่ไม่ใช่ PlotController แล้วกรอง
(attempt 2) เฉพาะ method ที่**มี call ตรงไปยัง method `PlotController$$` ใด ๆ** (E8 validate)

**ผล owner-scoped — พบ 2 จุด ใน 2 method [OBSERVED]:**

| site | owner | การเขียน | อ่านเชิงเกม |
|---|---|---|---|
| 0x5893F4 | `PlotController$$ShowPlot` | `[this(r14)+0x18] = 1` | **ตั้งธงเมื่อเริ่มเล่นพล็อต** |
| 0x4BE572 | `PlotController$$DisactiveInteractUI` | `[this(rbx)+0x18] = 0` | **เคลียร์ธงเมื่อปิด interaction** |

- **cross-module candidates (attempt 2):** จาก 355 จุด store `[reg+0x18]` ใน 301 owner ที่ไม่ใช่
  PlotController — มีเพียง **4 method** ที่ call เข้า `PlotController$$` โดยตรง:
  `GameController$$Start` (→ `QuickScreenBlack`) · `BattleController$$BattleRealEnd` (→
  `FightEndLoadInteractHero`) · `SteamManager$$Awake` (→ hit ที่ resolve เป็น
  `Mono.Runtime$$SetGCAllowSynchronousMajor` — **suspect false hit ของการสแกน** รายงานตามจริง) ·
  `MissionData$$GetMissionExtraDescribe` (→ `CheckMeetRequire`)
  — **แต่**การ call method ของ PlotController ≠ การเขียน `plotHappen` และ store `[reg+0x18]` ของแต่ละ
  method เหล่านี้**ยังไม่ได้พิสูจน์**ว่า base = PlotController instance (อาจเป็น field 0x18 ของคลาสตัวเอง)
  ⇒ cross-module writer ของ `plotHappen` = **UNKNOWN** (ไม่พบหลักฐาน ไม่ได้หักล้าง) · งบ 2 ความพยายาม
  ของข้อนี้หมดที่นี่ (ห้ามขยาย)
- **สรุป [OBSERVED + lower bound]:** ภายใน `PlotController$$` มี writer 2 จุดเท่านั้น — set=1 ที่
  `ShowPlot`, clear=0 ที่ `DisactiveInteractUI` · เพียงพออธิบาย freeze gate ข้อ 1 ของปั๊มงาน
  (โน้ตเดิม §2.5): ธงขึ้นตอนพล็อตเล่น ลงตอนปิด · ข้อสันนิษฐานเดิมที่ว่า "มี writer อื่น" ยังไม่มีหลักฐาน
  รอบนี้มาสนับสนุน

## (ง) ลำดับเงื่อนไขของ `ChangeDay()` leaf (0x790E30) จาก bodies ที่เก็บไว้

**ขอบเขต:** อ่าน `evidence/TIMEstatic-20261027-051647/bodies/GameController_ChangeDay_leaf.txt`
(1,370 insns — linear disasm ที่เก็บไว้ตอน 2026-10-27) · แยกเฉพาะ cmp/test/jcc/jmp/call พร้อม resolve
ชื่อ call + ตำแหน่ง BB (ไฟล์ `changeday-leaf-order.tsv` ในโฟลเดอร์นี้) · **นี่คือลำดับเชิงรายการที่ถอดได้ —
ไม่ใช่ CFG ยืนยันทุกกิ่ง** (กิ่งที่ไม่มี call/memcmp เด่น ๆ และการไหลข้ามบล็อกยังอ่านไม่ครบ = UNKNOWN)

**ลำดับที่ถอดได้ (เรียงตาม address — [OBSERVED เชิงรายการ]):**

1. **0x790E4A** `cmp byte [rip+0x170BF12], 0` + `jne 0x790FEA` — เงื่อนไขก่อนอย่างอื่น (byte ใน .data —
   ธง/ค่า config; ที่มายังไม่ resolve = UNKNOWN) ถ้า ≠0 ข้ามบล็อก init ไป 0x790FEA
2. **0x790E5E–0x790FDE** — บล็อกเรียก thunk `0xD64A0` 36 ครั้งติดกัน (thunk ไม่มีชื่อใน script.json —
   รูปแบบเดียวกับ prologue ของ method ที่มี static-use จำนวนมาก [INFERRED: กลุ่ม initializer/cctor-gate];
   ไม่ใช่ call ระบบเกม)
3. **0x791009–0x791048** — สาย null-check (`test rcx/rcx` ×3, `test rdx`) ทุกกิ่งไม่ผ่าน → `int3`-trap
   helper 0x79259D (null-guard ของ generated code) + **`cmp eax, 0x1E; jle`** = **วันเกิน 30 → เรียก
   `ChangeMonth()` (0x791055 → 0x793A20)** [OBSERVED — ปี/เดือนโอเวอร์โฟลว์จากวัน]
4. **0x791067** `cmp dword [rcx+0x9C], 1; jne 0x7910E1` — เงื่อนไข field 0x9C (int) ต้อง ==1 จึงเดินชุดถัดไป
   · ภายในกิ่ง: `cmp dword [rax+0x10], 2 / 3 / 4` — โซ่เทียบค่า int ของ object (รูปแบบ switch/ตาราง —
   ตัว object และ semantics ยังไม่ resolve = UNKNOWN)
5. **0x7910F0** `ManageMailTime()` — เดินจดหมายรายวัน
6. **0x791104–0x7911D2** — `WorldData$$Player()` null-check แล้ว `cmp byte [rax+0x60]` (field ตัวละคร) ⇒
   ลูปบาดเจ็บอัตโนมัติ: `GetHeroAutoInjuryChange()` ×3 + `ChangeExternalInjury` (0x791155) /
   `ChangeInternalInjury` (0x791191) / `ChangePoisonInjury` (0x7911CD) — แต่ละชนิดมี guard
   (`cmp eax, r13d`, `cmp eax, 2`) คั่น
7. **0x7911DC** `cmp byte [rdi+0x1E8]` — เงื่อนไข field ของ object หลัก (rdi) แล้ว jmp ข้าม/เข้าบล็อกถัดไป
8. **0x791211** `ManageHeroAutoRecover()` → **0x791225** `WorldData$$Player()` → **0x791238**
   `HeroData$$ManageTagTime()` → **0x791264** `HeroData$$TryIdentifyAllItem()` → **0x791271**
   `ManageAllAI()` → **0x79127B** `ResetAllAIMananged()` → **0x791285** `ManageAreaStuff()`
9. **0x79129D–0x791313** — สี่กิ่ง `test eax,eax; jle` (ค่า int ≤ 0 ข้าม — รูปแบบ "นับจำนวน X ถ้ามากกว่า 0
   ค่อยทำ") คั่น null-guard
10. **0x791315–0x79138F** — `WorldData$$Player()` ซ้ำ + `cmp qword [rax+0x2E0]` (field 8-byte เทียบ
    pointer/ค่า) + `js 0x791557` (เครื่องหมายลบ → ข้ามไปอีกสาย)
11. **0x7913CD–0x79154A** — ลูปรายการ (index `cmp edi, [rsi+0x18]` + ThrowArgumentOutOfRange) ภายในมี
    **`ImageConversion$$LoadImage` (0x791414)** — โหลดรูป (แนว ข่าว/ภาพรายวัน [INFERRED]) + สายเทียบ
    `cmp [rax+0x24], r12d` หลายกิ่ง (ขนาดรายการ/เงื่อนไข id)
12. **0x792016–0x792038** — `HeroData$$HaveArea` → ถ้า true `GenerateRandomEnemy(0,4)` (0x792029) →
    **`GenerateRandomEvent`** (0x792033) เสมอ
13. **0x792038–0x7921FE** — null-guard ต่อ + `List<int[]>$$RemoveAt` (0x7921D3) + thunk 0x5A6E0
    (null-check helper ที่เห็นซ้ำทั้งไบนารี [INFERRED])
14. **0x792236–0x7923D7** — **`Monitor$$Enter` (0x792260)** → `ManageForceStuff` (0x79226B) → สาย
    `String$$Concat` + **`Debug$$LogError`** (0x792326) ในกิ่ง error → **`Monitor$$Exit` (0x792372)**
    → จุดเขียน **`needAutoSave = 0`** (0x7923D7 — ข้อ (ก))
15. **0x7923E9–0x792426** — `ManageWeaponResearch` (0x7923E9) → `ManageMeditation` (0x7923F3) →
    `ManageForceSpeResearch` (0x7923FD) → `ManageSpePoison` (0x792407) → `ManageSpeSummonResearch`
    (0x792411)
16. **0x79244E** `WorldPlotEventController$$CheckWorldPlotEventDataBase` → 0x7924A6/0x792534
    **`PlayerPrefDictionary$$GetInt` ×2** (อ่าน pref — รวมรูปแบบ `"TestMode"` ที่โน้ตเดิมพบ [OBSERVED
    ตำแหน่ง — ค่า key จาก literal slot ยังไม่ resolve รายจุด])
17. **0x79254E–0x792586** — `List<AISettingType>$$Contains` (0x79257D) → ถ้า true **`needAutoSave = 1`**
    (0x792586 — ข้อ (ก)) → `ret` (0x79259C)
18. **0x79259D** — tail: `int3`-trap helper ทั้งหมดของ null-guard ชี้มาที่นี่ (ยกเว้นสายปกติ)

- **สิ่งที่ยังไม่ถอด [UNKNOWN]:** semantics ของ field 0x9C/0x10(2/3/4)/0x1E8/0x2E0/0x24, ต้นทาง byte ที่
  0x790E4A, ค่า key ของ `GetInt` ทั้งสองจุด (ต้อง resolve ScriptString slot ต่อ), การไหลของกิ่งที่ไม่มี
  call เด่น, และลำดับสัมพัทธ์ของบล็อก 0x791160–0x7915xx ที่ต่อกันด้วย jmp จำนวนมาก — ห้ามอ้าง "ลำดับ
  เต็มยืนยันแล้ว" เกินรายการข้างบน
- **ประโยชน์ที่ชัดจากผลนี้:** (1) `ChangeMonth` ยิงจาก leaf เมื่อ `eax > 0x1E` (วันที่ 31) — สอดคล้อง
  RUNTIME เดิม (>> เดินวัน) (2) `GenerateRandomEnemy`/`GenerateRandomEvent` ยิงใน leaf — ตรง xref เดิม
  (3) leaf เขียน `needAutoSave` ทั้งตั้ง (เงื่อนไข AI-setting) และเคลียร์ (ข้อ (ก)) (4) มีการอ่าน pref ใน leaf
  2 จุด

## purity ของรอบนี้

- เขียนไฟล์ใหม่**เฉพาะโฟลเดอร์นี้**: สคริปต์ 3 ไฟล์ + TSV/txt ผลลัพธ์ 6 ไฟล์ + โน้ตนี้ · ไม่เขียนอย่างอื่น
- ไฟล์เกมที่อ่าน: GameAssembly.dll (read-only, hash ตรง), Il2CppDumper/dump.cs (read-only),
  bodies/ ของ TIMEstatic เดิม (read-only) · เกมไม่รัน · ไม่มีการติดต่อเซฟ/pref/Steam
- ไฟล์ disassembly/TSV ใหญ่ของเกม**ไม่ถูก commit** (ตามมติเดิม — commit เฉพาะ .md ตามคำสั่ง A5)
