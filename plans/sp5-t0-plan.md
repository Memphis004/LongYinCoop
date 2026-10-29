# SP5-T0 Probe Plan — เตรียมตรวจบัญชี NPC จริง 1 creation round (ยังไม่ติดตั้ง hook)

- **label:** `SP5-T0-plan-20261027` · **วันที่:** 2026-10-27
- **ขอบเขตรอบนี้:** อ่าน source / interop metadata (`Il2CppDumper\dump.cs`, `MelonLoader\Il2CppAssemblies`)
  / evidence เดิมเท่านั้น · **ไม่ติดตั้งหรือเขียน runtime hook, ไม่ build/deploy, ไม่เปิดเกม, ไม่แตะเซฟ,
  ไม่แก้ source/config, ไม่ commit/tag** · ไฟล์ใหม่ = โฟลเดอร์นี้ไฟล์เดียว
- **อ้างหลักฐาน:** `SP5census-20261026-224844` · `SP5body-20261026` · `SP5refs-20261026` ·
  `SP5design-20261027` (§8.1 อนุมัติข้อ 3 = อ้างแผนนี้) · RUNTIME เดิม `sp0sp7-20261024-225939`
- **rev 2 (2026-10-27):** แก้ตามรีวิว — (1) CORE hooks ลดเหลือ H1–H5, ย้าย H6–H8 ไป OPTIONAL พร้อมเหตุผล
  (2) แยก `SpeHeroDataBase.Count` (ตาราง) ออกจากจำนวน `HeroData` ที่สร้างจริง และเลิกใช้ "SpeHero ครบ
  170" เป็น GO criterion (3) เพิ่ม instance identity ของ `WorldData`/`Heros`/`TempHeros` ใน boundary
  logs + เงื่อนไขก่อนใช้ Δ-count + กฎ `rebaseline` + เลิกบังคับยอด 882/81/883/963 (4) phase cleanup
  เมื่อ exception ด้วย finalizer + เกณฑ์ `phaseStuck == 0` (5) เพิ่ม preflight §2.6 (ไม่เปิดเกมในขั้นนี้)
- **rev 3 (2026-10-27):** เข้มการอ้าง phase ของ factory ใน creation round — E5 ยอมรับเฉพาะ factory call
  ใน window ของ `GenerateHero` (W2) และ `GenerateRandomEnemy` ที่ถูกเรียกใน round (W3) ไม่ใช่ทุก
  `sng.*` · เพิ่ม phase `sng.unattributed` (factory call ขณะ `StartNewGame` ยังทำงานแต่นอก W2/W3) +
  bucket `pre|creation|post` — **คำนวณจาก stack/จุดเวลาที่มีอยู่ ไม่เพิ่ม hook** · `sng.unattributed
  != 0` ⇒ บัญชียังปิดไม่ได้: ต้องระบุ caller หรือคง UNKNOWN **ห้ามสรุปทันทีว่าเป็น path ที่สอง** ·
  เหตุการณ์ factory หลัง `sng.end` รายงานใน bucket `post` แยก ไม่ผสม E5/สมการ creation และไม่ติดป้าย
  ผิดปกติโดยอัตโนมัติ
- **rev 4 (2026-10-27):** แยกกติกา phase stack **ต่อ bucket** — `creation`: `sng → genhero` หรือ
  `sng → genenemy` เท่านั้น · `post`: `GenerateRandomEnemy` เปิด/ปิด scope `genenemy` ได้**โดยไม่มี
  `sng` parent = การทำงานปกติ** (context `ChangeDay` บันทึกได้เมื่อ hook ที่ให้ context นั้นติดตั้ง
  อยู่แล้วเท่านั้น — **ห้ามบังคับให้ต้องติด H7**) · `phaseStuck` ตรวจเฉพาะ nesting ที่ผิดรูปจริง (pop
  ไม่ตรง scope / stack ไม่ว่างเมื่อ session จบ / creation ไม่ legal) — ไม่ใช่การพบ `genenemy` ใน
  bucket `post` ไร้ parent · `post` ไม่เข้า E1–E7 และเหตุการณ์ `post` ที่ถูกต้อง**ไม่ทำให้ creation
  round ตกเงื่อนไขความถูกต้องย้อนหลัง**
- **rev 5 (2026-10-27):** (1) E4 — ห้ามจำแนก factory calls ด้วย `belongForceID` เพียงอย่างเดียว
  (SpeHero เร่ร่อน 91 ตัวใช้ forceID −1 เหมือน generic ลูป#3); เสนอวิธีจำแนก SpeHero/generic จาก
  `heroName`/`heroDataBase` (identity กับ `SpeHeroDataBase`)/`heroID` และสรุปกฎจากข้อมูลจริง —
  หลักฐานไม่พอ ⇒ E4 คง UNKNOWN (2) แยก phase attribution (`sng.unattributed` = ปัญหาบัญชี) ออกจาก
  stack integrity (`phaseStuck`) ชัดเจนขึ้น + บันทึก marker แยกเมื่อเกิดหลายปัญหาพร้อมกัน (3) §2.6
  แยก **preflight read-only** ออกจาก **save-split ซึ่งเปลี่ยน filesystem state** — split ต้องอนุมัติ
  ก่อน + hash ก่อน/หลัง + แผนคืนสภาพ (4) finalizer ต้องคืน exception เดิมของเกมตาม Harmony contract —
  รับประกันไม่ได้ = NO-GO ก่อน runtime
- **rev 6 (2026-10-27):** (1) E4 — ตรวจจาก signature §1.2: `heroDataBase` มีเป็น parameter
  **เฉพาะ overload A และ C** (B ไม่มี) ⇒ การเทียบ identity กับ `SpeHeroDataBase` เป็น**เงื่อนไข
  ทางเลือกเท่านั้น** (ต้องพิสูจน์ว่า interop อ่าน reference ได้จริง) และ **ห้ามใช้ `heroName != null`
  หรือรูปแบบ `heroID` เป็นหลักฐานจำแนกเดี่ยว ๆ** — default ของ E4 = UNKNOWN เมื่อหลักฐานที่สังเกต
  ได้ไม่พอ (2) E7 คง **snapshot semantics**: `Σ perForce == hist[sect]` ณ snapshot เดียวกัน (delta
  ต้องนิยามคู่ snapshot + identity ชัด) (3) แยก `#C.calls` (จำนวน call ที่เกิด) ออกจาก `#C.ok`
  (call ที่สำเร็จ) และ `isTempHero`/`isRandomEnemy` ที่ใช้จำแนกต้องอิง**สถานะของ target `HeroData`
  หลัง factory** จาก typed interop — ห้ามถือว่า arg flags = ค่าหลังเรียกโดยไม่มีหลักฐาน; อ่าน
  post-state ไม่ได้ ⇒ สมการที่พึ่งค่านั้นคง UNKNOWN
- **rev 7 (2026-10-27):** บันทึก return type ที่แท้จริงจาก dump.cs (OBSERVED @dump.cs:376620/623/626):
  **A/B/C ทั้งสาม `public HeroData GenerateHeroData(...)`** และไม่มี `out/ref HeroData` ใน dump.cs
  ⇒ target ของ C คือ **return value** ไม่ใช่ argument ที่ factory เติม (`heroDataBase` = template
  ต้นแบบ input ตาม signature) · event contract แยกชัด: `factory.call` (prefix) / `factory.result`
  (**postfix — original จบตามปกติเท่านั้น**) / finalizer = phase cleanup + บันทึก failure แล้วคืน
  exception เดิม — **ห้ามสมมติว่า finalizer ใช้ `__result` ได้เหมือน postfix** · `#C.ok` นิยามผูกกับ
  `factory.result` + ชนิด `HeroData` (§2.4); §4 ข้อ 1 แก้เลข revision เป็นฉบับสุดท้าย
- **rev 8 (2026-10-27):** (1) finalizer **lifecycle = รันทุก invocation ทั้งสำเร็จและมี exception**
  (phase pop เสมอ); `factory.failure` เป็น **conditional event เมื่อ `__exception != null` เท่านั้น** —
  ห้ามผูกการทำงานของ finalizer ไว้เฉพาะตอน throw (2) ความหมายของ `factory.failure` = **exception
  observed in the patched invocation pipeline** — ห้ามอ้างว่า original throw เว้นแต่มีหลักฐานแยก
  แหล่ง exception ได้จริง; คืน exception เดิมไม่กลืน/ไม่แทนที่ + catch exception ของ probe ภายใน
  handler (3) `factory.call`/`factory.result`/สถานะ finalizer ของ invocation เดียวกันต้องผูกด้วย
  **invocation identifier** จากกลไก typed (ผู้สมัคร: Harmony `__state` / thread-local LIFO) —
  **ต้องพิสูจน์ทำงานได้จริงก่อนระบุเป็นข้อเท็จจริง**; timestamp เดี่ยวห้ามใช้ยืนยัน identity; แยก
  invocation ซ้อนไม่ได้ ⇒ accounting ที่พึ่ง linkage คง UNKNOWN (4) `#C.ok` ต้องมี `factory.result`
  (`HeroData` non-null) **+ สถานะ finalizer ของ invocation เดียวกันยืนยันไม่มี exception** —
  `factory.result` อย่างเดียวไม่พอ (5) อัปเดต §2.3–§2.5/§3/§5 + เพิ่มความเสี่ยง postfix-ตามด้วย-
  exception; ไม่ลดเกณฑ์ NO-GO
- **rev 9 (2026-10-27):** (1) canonical cleanup — รวมนิยาม/ข้อความซ้ำของ rev 6–7: นิยาม
  `#C.calls`/`#C.ok` อยู่ที่บรรทัดสัญลักษณ์ §2.3 **ฉบับเดียว** (E1/E2 อ้างอิงแทนการพูดซ้ำ) และ
  §2.5 รวม lifecycle ที่เคยพูดซ้ำใน bullet "phase cleanup (rev 2)" กับ "finalizer contract" เป็น
  contract ฉบับเดียว (2) เพิ่ม event **`factory.finalizer`** — บันทึก**ทุก invocation** ทั้ง
  `__exception == null` และ `!= null` พร้อม `iid` (หลักฐานเชิงบวกของ lifecycle) แยกจาก
  `factory.failure` (conditional เมื่อมี exception) (3) invocation linkage — `__state` ต้องยืนยัน
  ว่า prefix/postfix/finalizer แชร์ state ได้ใน patch arrangement ที่ใช้ (ประกาศชนิด state เดียวกัน
  ทั้งสาม handler ใน patch เดียว) พร้อม thread-local LIFO เป็นทางเลือก — ทั้งสองพิสูจน์ด้วย harness
  test ก่อนระบุเป็นข้อเท็จจริง (4) `exceptionAtProbeFinalizer` ≠ "pipeline ทั้งหมดสะอาด" — ใช้
  เป็นหลักฐาน `#C.ok` ได้เมื่อพิสูจน์ patch-audit (มีแค่ patch ของ probe บน method เป้า ตรวจด้วย
  Harmony `GetPatchInfo`) + ordering; พิสูจน์ไม่ได้ ⇒ ห้ามสรุป success ทั้ง pipeline, E1/E2 คง
  UNKNOWN (5) cross-reference: E1–E8 ครบ 8 แถว ฟิลด์ log ไม่ซ้ำ ไม่มีการอ้าง revision เก่าขัดแย้ง
- **rev 10 (2026-10-27):** กำหนดขอบเขต patch-audit — `Harmony.GetPatchInfo(targetMethod)`
  มองเห็น**เฉพาะ patch ที่ลงทะเบียนผ่าน Harmony API/patching runtime ที่ใช้อยู่** และ
  `GetAllPatchedMethods()` เป็นเพียงดัชนีเมธอดที่ถูก patch **ไม่ใช่หลักฐานโดยตัวมันเองว่าไม่มี
  detour/instrumentation จากระบบอื่น** · expected patch inventory ต่อ H3 overload: เทียบ
  Prefix/Postfix/Finalizer/Transpiler จริง (owner + patch method + จำนวน) กับรายการที่ probe
  คาดหมาย — **ผ่านได้เมื่อตรงเป๊ะ ไม่ใช่เพียง "ทุก owner เป็น probe"** · พบ patch อื่น/inventory
  ไม่ตรง/ยืนยันขอบเขต runtime ไม่ได้ ⇒ พิสูจน์ความสะอาดของ pipeline ทั้งหมดไม่ได้ — ห้ามใช้
  `exceptionAtProbeFinalizer == null` เดี่ยวสรุป pipeline success, E1/E2 คง UNKNOWN · ถ้อยคำ
  ที่อ้างได้ = **"ไม่มี exception ที่สังเกตได้ในขอบเขตของ patching pipeline ที่ตรวจสอบแล้ว"** (ไม่ใช่
  "ไม่มี exception ใน pipeline ทั้งหมด" เว้นแต่พิสูจน์ขอบเขตครบ) — ข้อมูล T0 ส่วนอื่นที่ไม่พึ่ง
  ข้อสรุปนี้ใช้ต่อได้ · เพิ่ม event `patch.audit` (read-only ตอน `session.begin`) · คงเงื่อนไข
  `__state`/LIFO + harness, finalizer contract และ NO-GO เดิมทั้งหมด — ขอบเขตยัง log-only
- **rev 11 (2026-10-27):** ตรวจ API ตามเวอร์ชันจริง (OBSERVED — reflection บน
  `MelonLoader\net6\0Harmony.dll` ที่ csproj อ้าง): **HarmonyX 2.10.2** (Assembly 2.10.2.0) —
  `HarmonyLib.Patches` เปิดเผย categories: `Owners` (property) + fields `Prefixes`, `Postfixes`,
  `Transpilers`, `Finalizers`, `ILManipulators` · **ไม่มี `InnerPrefixes`/`InnerPostfixes`** (มีใน
  HarmonyX ใหม่กว่า — จึงตรวจไม่ได้ใน runtime นี้ = ข้อจำกัดของ inventory) · `HarmonyLib.Patch`
  เปิดเผย `owner`/`PatchMethod`/`index`/`priority` · เพิ่มการตรวจซ้ำ `patch.audit` ใน prefix ของ
  `StartNewGame` **ก่อน original ทำงาน** (จุดใกล้ creation round ที่สุด) — inventory ต้องเสถียร
  ทั้งสองจังหวะ; เปลี่ยน/ตรวจไม่ครบ/ยืนยันเสถียรภาพไม่ได้ ⇒ E1/E2 คง UNKNOWN เว้นแต่มีหลักฐาน
  อื่นปิดข้อจำกัด · คงเงื่อนไข `iid`, finalizer contract, bounded audit scope และ NO-GO เดิม
- **rev 12 (2026-10-27):** ขยายจุดตรวจ inventory เป็น **4 จุด** — `session-begin` · `sng-prefix` ·
  **`genhero-prefix` (ก่อนเข้า W2)** · **`genenemy-prefix` (ก่อนเข้า W3)** — การตรวจที่ `genhero`/
  `genenemy` ทำใน prefix ของ H2/H4 ที่ติดอยู่แล้ว (**ไม่เพิ่ม hook**), read-only, ตรวจ H3 overload
  ทั้งสามด้วย expected inventory ของ HarmonyX 2.10.2 ครบ 5 categories (Prefixes/Postfixes/
  Finalizers/Transpilers/ILManipulators) · **`#C.ok` ใช้ใน window ใดได้เมื่อ inventory ณ จุดตรวจ
  ก่อนเข้า window นั้นตรง expected** — เปลี่ยน/อ่านไม่ครบ/ระบุสาเหตุไม่ได้ ⇒ E1/E2 คง UNKNOWN ·
  **ขอบเขต: audit ณ จุดตรวจ = หลักฐานสถานะ ณ ช่วงตรวจเท่านั้น ไม่ใช่การพิสูจน์ว่าไม่มี native
  detour หรือไม่มีการเปลี่ยน patch ระหว่างทุก invocation** — ห้ามอ้างเกินหลักฐาน · คง expected
  inventory, `iid`, finalizer contract, NO-GO และขอบเขต log-only เดิม
- **rev 13 (2026-10-27):** แยกความหมาย 2 ปริมาณ — **`inventoryAtWindowEntry`** = inventory
  ที่ตรวจได้ ณ จุดตรวจก่อนเข้า W2/W3 (point-in-time) · **`inventoryStability`** = หลักฐานว่า
  inventory ไม่เปลี่ยน**ตลอดช่วงที่ใช้คำนวณ `#C.ok`** (การเทียบจุดตรวจซ้ำเท่ากันครอบเฉพาะช่วง
  **ระหว่างจุดตรวจ** ไม่ใช่ภายใน window) · **ห้ามใช้ `inventoryAtWindowEntry` เดี่ยวพิสูจน์
  ความเสถียรภายใน window**; รอบนี้**ไม่เพิ่ม hook และไม่เพิ่มกลไกติดตามการเปลี่ยน patch
  ระหว่าง runtime** ⇒ ความเสถียรภายใน window = UNKNOWN ⇒ **`#C.ok` และ E1/E2 ที่พึ่ง
  pipeline-success คง UNKNOWN** — ผล audit ณ จุดตรวจยังเป็นข้อมูลที่ใช้ได้ตามปกติ; หากรอบถัด
  ไปต้องการปิดช่องนี้ (เช่น re-check inventory ใน handler ที่มีอยู่) ต้องขออนุมัติแยก · คง audit
  4 จุด, point-in-time limitation, expected inventory, `iid`, finalizer contract, NO-GO เดิม
- **purity รอบนี้:** hash เกมวัดซ้ำ 2026-10-27 — GameAssembly `33c56def…7920c1`, global-metadata
  `451311d4…18bfd0` **ตรง VERSION.md** · เกมไม่รัน · mod repo ไม่ถูกเขียน (ไฟล์ใหม่ = โฟลเดอร์นี้เท่านั้น)
- **ป้าย:** **OBSERVED** = ยืนยันจาก dump.cs/disasm/source วันนี้โดยตรง · **INFERRED** = อนุมาน ·
  **UNKNOWN** = พิสูจน์ไม่ได้ · ห้ามใช้จำนวน factory call เป็นจำนวนประชากรโดยตรง (เกณฑ์ของ T0 คือ
  ความสัมพันธ์เชิงบัญชีของรอบเดียว ไม่ใช่ยอด 882/81 ตายตัว)

---

## 1) ตาราง hook candidates และ typed-access feasibility

### 1.1 กลไกที่จะใช้ (รูปแบบเดียวกับที่โมดทำงานจริงทุก wave — OBSERVED)

- โมดอ้าง interop แบบมีชนิดอยู่แล้ว: csproj reference `MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll`
  + `using Il2Cpp;` (`SnapshotReader` อ่าน `BigMapController.Instance`, `HeroData.heroID`,
  `hero.bigMapPos` แบบ typed ทั้งหมด — ไม่มี raw offset)
- hook = HarmonyLib prefix/postfix ผ่าน `AccessTools.Method(typeof(InteropType), "Name", paramTypes)`
  — resolve ไม่ได้ = **ข้าม (Missing++) ห้ามเดา ห้ามใช้ offset** (idiom ของ `JitProbeW2`)
- ทุก hook body ครอบ try/catch กลืนหมด + นับ errors; `Uninstall()` = `UnpatchSelf()` +
  ตรวจจำนวน patch ก่อน/หลัง (idiom `JitProbeW2` + `CompatInventory.CountOwnedPatches`)

### 1.2 ตาราง hook candidates (CORE = ขั้นต่ำที่ T0 ต้องมี, OPTIONAL = ขออนุมัติรายตัว)

**CORE ของ T0 = H1–H5 เท่านั้น** — สมการบัญชีทั้งหมดใน §2.3 อยู่ได้ด้วยชุดนี้ (creation round ครอบทั้ง
`GenerateRandomEnemy` เริ่มโลกด้วย เพราะถูกเรียกจาก `StartNewGame` เอง — OBSERVED xref) · ตัวอื่นเป็น
OPTIONAL เพราะวัดช่วงเวลาที่**นอก** creation round หรือมีทางหลักที่ไม่ต้อง hook แทน

ทุกตัวยืนยันจาก dump.cs วันนี้ (public ใน interop ยกเว้นที่หมายไว้):

| # | เป้า (class::method) | ลายเซ็น (OBSERVED dump.cs) | ชั้น | ทำอะไร | feasibility |
|---|---|---|---|---|---|
| H1 | `GameController::StartNewGame` | `public void StartNewGame()` | CORE | ปักหมุด **window ของ creation round** (prefix = begin, postfix = end) + phase ราก | สูง — public, idiom เดิม |
| H2 | `GameController::GenerateHero` | `public void GenerateHero()` | CORE | ปักหมุด **window ลูปสร้างโลก** (nested ใน H1) + phase `genhero` + **อ่าน `forceLevelData.heroNum` ที่ prefix (ทางหลักของ T0)** + นับ Heros ก่อน/หลัง | สูง |
| H3 | `GameController::GenerateHeroData` ×3 overload | A `(int,int,float,HeroData,bool=True,bool=False)` · B `(string,int,int,float,bool=False)` · C `(string,int,int,float,HeroData,bool,SexLimit=0,bool=False,bool=False)` — **return type ทั้งสาม = `HeroData`** (OBSERVED dump.cs:376620/623/626; ไม่มี `out/ref HeroData` ใน dump.cs) | CORE | **log-only ทุก argument ของทุก call** (แก้ mapping "#2"→ลายเซ็นของ SP4) — ห้ามปฏิเสธการสร้าง, ห้ามแก้ args, ห้าม**แก้** `__result` (rev 6–7: **อ่านอย่างเดียวใน postfix** เพื่อบันทึกสถานะหลัง factory ของ target — target ของ C คือ return value ชนิด `HeroData` ไม่ใช่ argument; `heroDataBase` = template input) · หมายเหตุ: `heroDataBase` เป็น parameter เฉพาะ A และ C — B ไม่มี (B เป็น wrapper → C) | สูง — `AccessTools.Method` ระบุ paramTypes ได้ครบต่อ overload; อ่าน `__result` แบบ typed ได้เพราะ return type เป็น interop `HeroData` |
| H4 | `GameController::GenerateRandomEnemy` | `public void GenerateRandomEnemy(int maxNum = 999)` | CORE | log `maxNum` + นับ TempHeros ก่อน/หลัง + phase | สูง |
| H5 | `WorldData::AddTempHero` | `public void AddTempHero(HeroData target)` | CORE | log heroID + จำนวน TempHeros ก่อน/หลัง — ปลายทางจริงของกลุ่ม temp | สูง |
| H6 | `GameController::WorldAddNewHero` | `public HeroData WorldAddNewHero(int, int, bool, bool = True)` | OPTIONAL | log args + `__result` nonNull — **ไม่จำเป็นต่อ T0**: caller เดียว = `ManageHeroAutoQuitAndEnter` ← `ChangeMonth` (OBSERVED xref) ซึ่งไม่ยิงใน creation round — เก็บไว้วัดช่วงเติมรายเดือนภายหลัง (E8/T5 ของ design) | สูง |
| H7 | `GameController::ChangeDay` ×2 + `ChangeMonth` + `ManageHeroAutoQuitAndEnter` | `public void` | OPTIONAL | phase context `changeday`/`changemonth` — **ไม่จำเป็นต่อ T0**: ใน creation round `GenerateRandomEnemy` ยิงจาก `StartNewGame` จึงอยู่ภายใต้ phase รากอยู่แล้ว; ถ้าเกมข้ามวันเกมช่วง settle เหตุการณ์รายวันถูกจัด **bucket `post`** และรายงานแยก (§2.1 rev 3–4) — context `changeday` ถูกบันทึกเพิ่มได้**เฉพาะเมื่อ H7 ติดตั้งอยู่แล้ว ไม่บังคับ** — เก็บไว้วัดรายวัน/รายเดือนภายหลัง | สูง |
| H8 | `GameDataController::LoadAllGameData` | `private void LoadAllGameData()` | OPTIONAL (ดีฟอลต์ไม่ติด) | cross-check ค่า heroNum หลังโหลด — **ไม่บังคับ**: ทางหลักของ T0 = อ่านที่ `H2` prefix แบบ typed (ข้อมูลโหลดเสร็จก่อนลูปเดินเสมอ) จึงไม่ต้องแตะ private; ถ้าเปิด ใช้เทียบว่าค่าไม่เปลี่ยนระหว่างโหลดกับลูป | กลาง — private: ต้องพิสูจน์ `AccessTools` resolve ได้บน interop ตอน build; resolve ไม่ได้ = ข้าม (ไม่หยุด T0) |
| H9 | `StartGameSettingController::BirthSettingPlayerData` | `public void` | OPTIONAL | marker จุดเติม attr ตัวเอก (1 call ต่อโลกใหม่) | สูง |
| H10 | `WorldData::GetRandomEnemyCount` / `FindAvailableHeroID` | `public int` | OPTIONAL | log ค่าคืน — หลักฐานสูตรเติมศัตรู/การจัด ID (prefix ห้ามแก้) | สูง |
| H11 | phase-setter ฝั่ง outside: `PlotController::StartSpeFight, PlotStartFight, GetTempPlotHeroData, NewAskJoin, GetSpeReward, FindSpeFollower, FindSpeMasterFight, AcceptLittleMission, StartProtectBountyMission, StartProtectRichMission, GetGovernBodyGuardSure, RescureNPCPrisonSure, StartReplaceOtherForceFight, FinishExploreTomb, StartChallengeGhostGateFight` + `RecruitUIController::SureButtonClicked` | caller ตรงของ wrapper A/B/C (OBSERVED calls-xref: B×419 sites อยู่ใน 12 method นี้ + `BattleMapData::GenerateGuard`) | OPTIONAL | ติดป้าย phase `plot`/`recruit` — แยกเส้นทางออกจาก creation round | กลาง — จำนวน hook เยอะ (16) จึงเป็น optional; ถ้าไม่ติด เส้นเหล่านี้ถูกแท็ก phase `outside` หรือ `sng.unattributed` ตาม stack ขณะนั้น — ใน bucket `creation` สะอาดต้อง = 0 ทั้งสอง phase (E5) |

**ไม่ hook:** `HeroData..ctor` (โดน ~886 ครั้ง/รอบ ไม่จำเป็น — ตัวเอกพิสูจน์ด้วยสมการ E3),
`GenerateRelationShip`, `GenerateHeroDict`, ระบบศึก (S2/S1.5 hooks เดิมไม่แตะ)

### 1.3 การอ่าน `forceLevelData`/`heroNum` ด้วย typed interop (งานเตรียมข้อ 2 — ไม่แก้ค่า)

OBSERVED จาก dump.cs (public ทั้งสาย):

```csharp
GameDataController gdc = GameDataController.Instance;      // public static get_Instance()
List<ForceLevelData> lvls = gdc.forceLevelData;            // public List<ForceLevelData> 0x40
foreach (ForceLevelData e in lvls)
    int[] copy = e.heroNum.ToArray();                       // public List<int> heroNum 0x10 — copy อ่านอย่างเดียว
```

- เขียนกลับเข้า field ใด ๆ **ห้าม** — ตัว probe เป็น read-only ทุกเส้น
- **ทางหลักของ T0: อ่านที่ `H2` (`GenerateHero`) prefix** — จุดนั้น `LoadAllGameData` จบแล้วและลูป
  ยังไม่เดิน จึงได้ค่าที่ลูปจะอ่านจริงโดย**ไม่ต้อง hook private `LoadAllGameData` เลย** (H8 ยังเสนอไว้
  เป็น OPTIONAL cross-check — ถ้าเปิด ใช้เทียบว่าค่าไม่เปลี่ยนระหว่างโหลดกับลูป, ดีฟอลต์ไม่ติด)
- พ่วง: log `SpeHeroDataBase.Count` (public Dictionary) เป็น**ขนาดตารางฐานข้อมูล (table size)**
  เท่านั้น — **ห้ามใช้แทนจำนวน `HeroData` ที่ถูกสร้างจริงในโลก (instances)** เป็นสองปริมาณต่างกัน:
  ตาราง 170 แถวไม่ได้แปลว่าโลก instantiate ครบ 170 (census §7: hidden อาจถูกสร้างแบบ lazy —
  UNKNOWN); จำนวนจริงในโลกมาจาก census ของ `Heros` เท่านั้น (§2.2)
- **ข้อจำกัดถ้า typed ล้มเหลว** (เช่น Instance ยัง null ตอนเรียก): รายงาน `readfail` marker แล้วข้าม —
  **ห้ามแก้ด้วย raw memory offset โดยไม่เสนอและรออนุมัติก่อน** (ตามขอบเขตบังคับ + กติกา 8)

### 1.4 ฟิลด์จำแนกประเภท NPC (งานเตรียมข้อ 4) — OBSERVED dump.cs public ทั้งหมด

`HeroData`: `heroID` (0x58) · `speHero` (0x5C) · `tempPlotHero` (0x5D) · `heroName` (0x68) ·
`belongForceID` (0x84) · `outsideForce` (0x8C) · `isTempHero` (0x3CC) · `isRandomEnemy` (0x3CD)

| คลาสใน census | เงื่อนไขจำแนก (บนสถานะโลก) |
|---|---|
| ตัวเอก | `heroID == 0` |
| SpeHero (PROTECTED) | `speHero == true` (ตรวจซ้ำว่า heroID อยู่ 1..170 ทุกตัว) |
| สมาชิกสำนัก | `!speHero && belongForceID >= 0 && belongForceID <= 29 && !isTempHero` |
| เร่ร่อน | `!speHero && belongForceID == -1 && !isTempHero` |
| กลุ่ม forceID −2 | `!speHero && belongForceID == -2 && !isTempHero` (คงพฤติกรรม — นับอย่างเดียว) |
| TempHero (ศัตรุสุ่ม) | อยู่ใน `TempHeros` + `isTempHero && isRandomEnemy` |
| temp plot hero | `tempPlotHero == true` (คาดหมาย = 0 ในรอบ creation — ตรวจซ้ำ) |
| other | ที่เหลือ — ต้อง = 0 ในรอบสะอาด ไม่งั้นรายงาน UNKNOWN |

---

## 2) แผนแยกเส้นทาง + การนับ + log schema + บัญชี

### 2.1 การแยก call sites (งานเตรียมข้อ 3) — phase context แบบ stack แทนการ patch ลูป

- phase = ข้อความต่อกันของ hook ที่กำลัง nested อยู่ เช่น `sng.genhero` · `sng.genenemy` ·
  **`sng.unattributed`** · `changeday` · `changemonth` · `plot` · `recruit` · `outside` (default) —
  **`sng.unattributed` (rev 3) = factory call ขณะ `StartNewGame` ยังทำงาน (stack มีแค่ราก `sng`)
  แต่ไม่ได้อยู่ใน W2/W3** — คำนวณจาก stack ที่มีอยู่แล้ว **ไม่ต้องเพิ่ม hook**
- ทุกเหตุการณ์ factory/wrapper ถูกจัด **bucket** จากจุดเวลาเทียบ `sng.begin`/`sng.end` (rev 3):
  `pre` (ก่อน `sng.begin`) · `creation` (`[sng.begin, sng.end]`) · `post` (`sng.end` → `session.end`)
  — bucket เป็นการจัดกลุ่มเชิงบัญชีของตัว log/analyzer ไม่ใช่ hook ใหม่
- prefix ผลัก phase, pop ผ่าน finalizer (§2.5); ตัว factory (H3) **log-only** อ่าน phase/bucket
  ปัจจุบัน — ไม่มีการปฏิเสธการสร้างใด ๆ ทั้งสิ้น (แม้แต่บรรทัดเดียว)
- **โปรโตคอลรอบที่สะอาด:** เปิดเกม → สร้างโลกใหม่ทันที → **ไม่เปิด plot/mission/recruit ระหว่างรอบ** →
  หยุดหลัง `sng.end` + ช่วง settle ที่กำหนด ⇒ ใน bucket `creation` ทุก factory call ต้องอยู่ใน
  **window ของ `GenerateHero` (W2) หรือ `GenerateRandomEnemy` ที่ถูกเรียกใน round (W3) เท่านั้น** —
  call ที่ phase `sng.unattributed` ⇒ บัญชีเส้นทางสร้างยังปิดไม่ได้ (E5/E6), call ที่ phase
  `plot`/`recruit` ในรอบ = พยานว่าเส้นทางอื่นยิง (รายงาน ไม่ลบทิ้ง)
- **bucket `post` รายงานแยก (rev 3):** เหตุการณ์ factory หลัง `sng.end` (เช่น `GenerateRandomEnemy`/
  `AddTempHero` จาก `ChangeDay` รายวัน) เป็น **พฤติกรรมเกมปกติที่คาดหมายได้** — นับ/รายงานในหมวด
  `post` ของมันเอง **ไม่นำมาผสมกับ E5 หรือสมการ creation และไม่ติดป้ายว่าผิดปกติโดยอัตโนมัติ**
- **กติกา phase stack แยกต่อ bucket (rev 4):**
  · `creation`: factory call ที่ legal มีแค่ **`sng → genhero` หรือ `sng → genenemy`** (รากเดี่ยว
  `sng` = `sng.unattributed` — ปัญหาบัญชี ไม่ใช่ stack)
  · `post`: `GenerateRandomEnemy` (CORE H4) เปิด/ปิด scope `genenemy` ได้**โดยไม่มี `sng` parent**
  — ถือเป็น**การทำงานปกติ**; ถ้า hook ที่ให้ context `ChangeDay` (H7 OPTIONAL) ติดตั้งอยู่แล้ว
  จะบันทึก context เป็น `changeday.genenemy` เพิ่มด้วย — **ห้ามบังคับให้ต้องติด H7**
  · `pre`: ไม่ควรพบ factory call — พบ = รายงานตามจริง (observational)
- บัญชีฝั่งเติมรายเดือน (`changemonth`) แยกเป็น **ช่วง optional**: ถ้ารอบทดสอบเดินเกมข้ามเดือนเกมได้
  จะ log เพิ่ม (H6/H7 OPTIONAL) — ไม่บังคับใน creation round (T5 ของ design จัดการ long-run)

### 2.2 การนับ Heros/TempHeros — state-based ที่ boundary (ไม่นับ List.Add เป็นหลัก) + instance identity

- จุดวัด (ทุกจุด = snapshot `worldcount`): `session.begin` · `sng.begin` · `genhero.begin/end` ·
  `genenemy.begin/end` · `sng.end` · `session.end` — จุด `changeday/changemonth` มีเฉพาะเมื่อ
  เปิด OPTIONAL H7 · จุด `genenemy.begin/end` **อาจเกิดซ้ำใน bucket `post`** (ChangeDay รายวัน,
  rev 4) — snapshot ยังเขียนพร้อม `bucket=post` แต่ใช้เชิงบัญชีได้เฉพาะชุดที่อยู่ใน
  bucket `creation` เท่านั้น
- **identity ทุก snapshot:** บันทึก `wid` (pointer ของ `worldData`) + `hidH`/`hidT` (pointer ของ list
  instance `Heros`/`TempHeros`) — อ่านจาก property `Pointer` ของ interop wrapper เท่านั้น
  (การเทียบตัวตน object ไม่ใช่การอ่านหน่วยความจำด้วย offset)
- **เงื่อนไขก่อนใช้ Δ-count:** baseline กับปลายทางต้องเป็น **list instance เดียวกัน** (`wid`/`hidH`/`hidT`
  ตรงกัน) — `StartNewGame` สร้าง `WorldData` ใหม่ ⇒ instance เปลี่ยนข้าม `sng.begin` ได้ตามธรรมชาติ
  ⇒ **baseline ของสมการบัญชีตั้งที่จุดแรกหลัง world state พร้อม** (`genhero.begin` สำหรับ `Heros`,
  `genenemy.begin` สำหรับ `TempHeros`) ไม่ใช่ที่ `sng.begin` · ถ้าพบ instance เปลี่ยนระหว่างจุดวัดใด ๆ
  ที่สมการอ้าง ⇒ ติดป้าย `rebaseline` (เริ่มนับจาก 0 บน instance ใหม่) และสมการใดที่ยังข้าม
  instance อยู่ = **ไม่ปิด** (รายงาน UNKNOWN)
- แต่ละ snapshot: `Heros.Count`, `TempHeros.Count`, histogram 8 คลาสข้างบน (เดิน list ครั้งเดียว,
  typed), per-force histogram (30 ช่อง สำหรับสมาชิกสำนัก) — snapshot ~1,000 ตัว เดินได้ในหลัก
  millisecond ที่ boundary (ไม่ใช่ per-frame)
- **ความเสี่ยง generic sharing ถ้าต้องนับ `List<T>.Add` (งานเตรียมข้อ 5):**
  `List<HeroData>::Add` เป็น shared generic — native body เดียว (`0x18181F800`) ใช้ร่วมทุก `T`
  (OBSERVED SP5body §0) ⇒ การ detour ตัวนี้โดนการ Add ของ**ทุกลิสต์ในเกม** (overhead กว้าง +
  เสี่ยง frame-time ขัดวินัย S2) และ address เดียวไม่บอกปลายทาง
  ⇒ **แผนหลักไม่ hook Add เลย** (สมการ §2.3 ปิดบัญชีด้วย boundary + factory args ได้)
  ⇒ **แผนสำรอง (เฉพาะเมื่อ residual ≠ 0 และได้อนุมัติแยก):** hook Add แบบ arm สั้น (เฉพาะ
  window `genhero`, ปิดทันทีที่ postfix) โดยนับเฉพาะเมื่อ
  `ReferenceEquals(__instance, worldData.Heros)` หรือ `TempHeros` — **มีหลักฐานบริบทจากอินสแตนซ์
  จริงของลิสต์ที่อ่าน typed ไว้ จึงไม่ละเมิดข้อห้าม "นับ shared address โดยไม่มีบริบท"** ·
  ถ้าแม้แต่แผนสำรองไม่อนุมัติ ⇒ residual คง UNKNOWN ตามเกณฑ์ (ห้ามเริ่มลด NPC)

### 2.3 บัญชี (accounting equations) — เกณฑ์ผ่านของ T0

สัญลักษณ์: `H(x)/T(x)` = จำนวนใน snapshot จุด x (ใช้ได้เมื่อ identity ของ instance ตรงกันตาม §2.2) ·
**`inventoryAtWindowEntry`** = inventory ที่ตรวจได้ ณ จุดตรวจก่อนเข้า window (point-in-time —
ค่าจาก `patch.audit` ของจุดนั้น) · **`inventoryStability`** = หลักฐานว่า inventory ไม่เปลี่ยน
**ตลอดช่วงที่ใช้คำนวณ `#C.ok`** — การเทียบจุดตรวจซ้ำเท่ากันพิสูจน์เฉพาะช่วง**ระหว่างจุดตรวจ**
ไม่ครอบภายใน window (rev 13) ·
`#C.calls(cond)` = จำนวน call ที่**เกิดขึ้น**ของ overload C (จาก prefix) · `#C.ok(cond)` = จำนวน
invocation ที่**สำเร็จครบใน pipeline** — ต้องมีหลักฐานครบ 3 ชั้นของ**invocation เดียวกัน**
(ผูกด้วย identifier ตาม §2.4/§2.5): (i) event `factory.result` (postfix) พร้อม `__result` เป็น
`HeroData` non-null (return type ของ A/B/C = `HeroData`, OBSERVED dump.cs), (ii) จำแนกด้วย
**สถานะของ target ที่อ่านจาก `__result`** (`isTempHero`/`isRandomEnemy` อ่าน typed ใน postfix
อย่างเดียว — **ห้ามใช้ arg flags ก่อนเรียกแทนค่าหลังเรียกโดยไม่มีหลักฐาน**; **ห้ามอ่าน `__result`
ใน finalizer**), (iii) **`factory.finalizer` ของ invocation เดียวกันรายงาน
`exceptionAtProbeFinalizer == null`** (exception **ณ finalizer ของ probe** — ไม่เท่ากับ "pipeline
ทั้งหมดไม่มี exception") **และ** patch-audit ผ่านตามขอบเขต rev 10–12: inventory จริง**ครบทุก
category ที่ API 2.10.2 เปิดเผย** (Prefixes/Postfixes/Transpilers/Finalizers/ILManipulators —
ไม่มี InnerPrefix/InnerPostfix ให้ตรวจ = ข้อจำกัด) บน method เป้า (จาก `Harmony.GetPatchInfo` —
**มองเห็นเฉพาะ patch ที่ลงทะเบียนผ่าน Harmony runtime ที่ใช้**; detour/instrumentation นอก
Harmony มองไม่เห็น) **ตรง expected inventory เป๊ะ** (owner + patch method + จำนวน) **ณ จุด
ตรวจก่อนเข้า window ที่เกี่ยวข้อง** (`inventoryAtWindowEntry`) — 4 จุด: `session-begin`/
`sng-prefix`/`genhero-prefix` (W2)/`genenemy-prefix` (W3) · **และ** `inventoryStability` ของ
window นั้นพิสูจน์ได้ — การเทียบจุดตรวจซ้ำเท่ากันเป็นหลักฐานความเสถียร**เฉพาะช่วงระหว่างจุด
ตรวจ** ไม่ครอบภายใน window; รอบนี้ไม่มีกลไกตรวจภายใน window (rev 13) ⇒ ความเสถียรภายใน
window = UNKNOWN ⇒ **`#C.ok` และ E1/E2 ที่พึ่ง pipeline-success คง UNKNOWN** (ผล audit ณ
จุดตรวจยังใช้ได้เป็นข้อมูล — rev 13); ผ่านครบทุกเงื่อนไข (เมื่อ stability มีหลักฐานจากกลไกที่ผ่าน
อนุมัติในอนาคต) สรุปได้เฉพาะ **"ไม่มี exception ที่สังเกตได้ในขอบเขตของ patching pipeline
ที่ตรวจสอบแล้ว"** (audit ณ จุดตรวจ = หลักฐานช่วงตรวจ ไม่ใช่การพิสูจน์ต่อเนื่องทุก invocation) —
พบ patch อื่น / inventory ไม่ตรง / เปลี่ยนระหว่างจุด / ยืนยันขอบเขต runtime ไม่ได้ ⇒
**ห้ามสรุป success ทั้ง pipeline** (rev 6–13) ·
`W2` =
`[genhero.begin, genhero.end]` · `W3` = `[genenemy.begin, genenemy.end]` **ของ bucket `creation`
เท่านั้น** (window `genenemy` ใน bucket `post` — เช่นจาก `ChangeDay` รายวัน — ไม่เข้าสมการ, rev 4) ·
**สมการ E1–E7 นับเฉพาะ
เหตุการณ์ใน bucket `creation` — เหตุการณ์ bucket `pre`/`post` รายงานแยกไม่เข้าสมการ (§2.1, rev 3–4)** ·
ทุกสมการถือว่า
**ไม่มีการถอด hero ใน window** (`HeroRealAutoRemove` มี caller แค่ Plot + ChangeMonth path —
OBSERVED xref — ไม่ยิงใน creation round; ถ้า E1/E2 ไม่ปิด ให้สงสัยเส้นถอดก่อน)

| # | สมการ | พิสูจน์อะไร |
|---|---|---|
| E1 | `H(genhero.end) − H(genhero.begin) == #C.ok(W2, post !isTempHero && !isRandomEnemy) + 1` (baseline ที่ `genhero.begin` — instance ใหม่พร้อมแล้ว) + cross-check `H(sng.end) == H(genhero.end)` บน instance เดียวกัน — `#C.ok` ตามนิยามฉบับเดียวในบรรทัดสัญลักษณ์ (`factory.result` non-null + `factory.finalizer` รายงาน `exceptionAtProbeFinalizer == null` + patch-audit ผ่าน **+ `inventoryStability` ของ W2 พิสูจน์ได้ — rev 13**); **เงื่อนไขใดไม่ครบ ⇒ สมการนี้คง UNKNOWN — รอบนี้ `inventoryStability` ภายใน window ยังพิสูจน์ไม่ได้ จึงคาดหมายว่า E1 คง UNKNOWN จนกว่า stability จะมีหลักฐานจากกลไกที่ผ่านอนุมัติ** (rev 6–13) | ทุก Heros ใหม่มาจาก factory สำเร็จ + ตัวเอก (ctor path) เท่านั้น และไม่มี Heros เพิ่มนอก W2 ใน creation round |
| E2 | `T(genenemy.end) − T(genenemy.begin) == #AddTempHero(W3) == #C.ok(W3, post isTempHero && isRandomEnemy)` (baseline ที่ `genenemy.begin` ของ bucket `creation`) + cross-check `T(sng.end) == T(genenemy.end)` — window `genenemy` ใน bucket `post` ไม่เกี่ยวกับสมการนี้ (rev 4); `#C.ok` ตามนิยามฉบับเดียวในบรรทัดสัญลักษณ์ — **เงื่อนไขใดไม่ครบ ⇒ UNKNOWN — รวม `inventoryStability` ของ W3 (rev 13) ที่รอบนี้ยังพิสูจน์ไม่ได้ จึงคาดหมายว่า E2 คง UNKNOWN จนกว่า stability จะมีหลักฐาน (rev 6–13)** | TempHeros เริ่มโลก = เส้น GenerateRandomEnemy เท่านั้น, ปลายทาง AddTempHero |
| E3 | (รวมใน E1 แล้ว) residual `+1` ของ E1 = ตัวเอกที่สร้างด้วย ctor ใน `GenerateHero` ไม่ผ่าน factory | จุดสร้างตัวเอก — ปิด UNKNOWN "จุดสร้างตัวเอก" |
| E4 | การจำแนก factory calls รายลูป **ห้ามใช้ `belongForceID` เพียงอย่างเดียว** (SpeHero เร่ร่อน 91 ตัวมี forceID −1 เหมือน generic ลูป#3, SpeHero ในสำนักใช้ 0..29 เหมือนลูป#2 — INFERRED จากตาราง) และ **ห้ามใช้ `heroName != null` หรือรูปแบบ `heroID` เป็นหลักฐานจำแนกเดี่ยว ๆ** (C สุ่มชื่อเมื่อ name null — OBSERVED; SP4 วัดได้ name=null ทั้งรอบ — UNKNOWN ว่า call ใด null) · **`heroDataBase`: ตรวจจาก signature §1.2 มีเป็น parameter เฉพาะ overload A และ C (B ไม่มี)** ⇒ การเทียบ identity กับ `SpeHeroDataBase` เป็น**เงื่อนไขทางเลือกเท่านั้น** — ใช้ได้เมื่อพิสูจน์ได้ว่า interop อ่าน reference นี้ได้จริง (ตรวจตอน build/runtime) และ**ห้ามใช้ตัดสินเดี่ยว ๆ** เพราะ generic loops อาจส่ง template จาก `SpeHeroDataBase` ก็ได้ (UNKNOWN) ⇒ กฎจำแนกต้อง**สรุปจากชุดหลักฐานที่สังเกตได้จริงทั้งหมด** (`factory.call` args + `factory.result` post-state + world-state census `speHero == true` เทียบภายหลัง) — `Δhist[SpeHero]` **บันทึกตามที่วัด ไม่บังคับ == 170** (จำนวน instantiate ≠ ขนาดตาราง; hidden อาจ lazy — UNKNOWN); **default ของ E4 = UNKNOWN** เมื่อหลักฐานที่สังเกตได้ไม่พอ — ถือว่าผ่านเฉพาะเมื่อกฎที่สรุปได้ทำนาย world-state classification ได้ตรงภายใน band; E1/E2/E7 ไม่พึ่ง E4 จึงยังประเมินได้ | mapping ลูป#1/#2/#3 ↔ SpeHero/generic — พิสูจน์ได้เฉพาะเมื่อหลักฐานที่สังเกตได้พอ ไม่งั้น UNKNOWN ตามกติกา |
| E5 | ใน bucket `creation`: ทุก factory call ต้องอยู่ใน **W2 หรือ W3** เท่านั้น (`phase ∈ {sng.genhero, sng.genenemy}`) — **`#factory(phase == sng.unattributed) != 0` ⇒ บัญชีเส้นทางสร้างยังปิดไม่ได้**: ต้องระบุ caller (จากลำดับเวลา/บริบทใน log เช่นจังหวะก่อน-หลัง boundary, marker ที่เปิดไว้) หรือคง UNKNOWN ก่อน — **ห้ามสรุปทันทีว่าเป็น path ที่สอง**; call ที่ phase `plot`/`recruit` ในรอบ = เส้นทางอื่นยิงจริง (รายงาน ไม่ลบทิ้ง) — เหตุการณ์หลัง `sng.end` เข้า bucket `post` ไม่ถูกนับในข้อนี้ | เส้นทาง creation ถูกอธิบายครบด้วย 2 window ที่รู้จัก — เข้มกว่า rev 1–2 ที่ยอมรับทุก `sng.*` |
| E6 | SP4-check ด้วย**ตัวเลขของรอบตัวเอง** — เมื่อ E1 ไม่ปิด ตรวจ**ตามลำดับ**: (1) มี factory call ใน `sng.unattributed` หรือไม่ — ถ้ามี ⇒ ระบุ caller/อธิบายก่อน (**ไม่ใช่หลักฐาน path ที่สองในตัวมันเอง**) (2) ถ้า `sng.unattributed == 0` และ `#C.calls(W2) == 0` (ไม่มี call เกิดขึ้นเลย — call ที่มี exception observed ใน pipeline ยังนับเป็น calls แต่ไม่ใช่ ok และเห็นแยกผ่าน `factory.failure`, rev 7–8) แต่ `H(genhero.end) − H(genhero.begin)` มากผิดปกติ ⇒ **path ที่สอง (non-factory) ⇒ NO-GO** คง UNKNOWN — ยอด 882/81/883/963 ของ SP4 = **ค่าอ้างอิงเชิงเปรียบเทียบเท่านั้น ห้ามใช้เป็นเกณฑ์ผ่าน/ไม่ผ่าน** | ตรวจกรณี SP4 โดยไม่ผูกกับยอดโลกเดิมและไม่กระโดดสรุปจาก unattributed |
| E7 | **snapshot semantics (rev 6):** ทุก snapshot: `Σ hist == H` และ **`Σ perForce == hist[sect]` ณ snapshot เดียวกัน** (perForce เป็น histogram ต่อ snapshot ไม่ใช่ delta) — ถ้าต้องการเทียบแบบ delta ต้องนิยามชัด `ΔperForce = perForce(s2) − perForce(s1)` กับคู่ snapshot `s1,s2` ที่ instance identity ตรงกัน (`wid`/`hidH`/`hidT` ตรง, ไม่มี `rebaseline` ข้ามคู่) | การจำแนกไม่หลุด/ไม่นับซ้อน — หน่วยบัญชีต่อ snapshot ชัดเจน ไม่ผสม snapshot กับ delta |
| E8 | (OPTIONAL — เมื่อเปิด H6+H7 และมีช่วง `changemonth`) `ΔH == #WorldAddNewHero − #remove` | เส้นเติมรายเดือนผ่าน WorldAddNewHero 2 sites เท่านั้น — **ไม่ใช่ของ T0 creation round** |

เกณฑ์ผ่านรวม (ตามโจทย์): ให้หลักฐานได้ครบ — (1) ค่า `heroNum` + จำนวน entry ที่อ่านจริง,
(2) จำนวน call ราย overload + args จำแนกได้, (3) จำนวนเข้า `Heros`/`TempHeros` แยกกัน,
(4) แยกเส้นทาง creation ออกจาก plot/recruit/monthly, (5) ตรวจกรณี SP4 (E6) — **ใช้ความสัมพันธ์
เชิงบัญชีของรอบเดียวเป็นเกณฑ์ ไม่บังคับยอด 882/81/883/963** · **เงื่อนไขความถูกต้องของรอบ
(ต้องครบก่อนตีความสมการใด ๆ):** ทุก Δ-count ใช้ list instance เดียวกันหรือมี `rebaseline`
marker ที่อธิบายครบ · `phaseStuck == 0` (§2.5) · `Σ hist == H` ทุก snapshot ที่ใช้ (E7) ·
สมการใดไม่ปิด ⇒ สมการนั้นคง UNKNOWN และ **ห้ามเริ่มลด NPC** จนกว่าจะอธิบายได้

### 2.4 Log schema (งานเตรียมข้อ 6)

- รูปแบบ: NDJSON บรรทัดละเหตุการณ์ → `evidence/SP5t0-<ts>/sp5t0.ndjson` (โฟลเดอร์ evidence ใหม่
  ตอนรันจริง) + ไฟล์ summary ตอน `session.end`
- ฟิลด์ร่วม: `t` (unix ms), `frame`, `ev`, `ph` (phase — รวมค่าใหม่ `sng.unattributed`; ใน bucket
  `post` อาจเป็น `genenemy` **ไร้ `sng` parent = ปกติ** หรือ `changeday.genenemy` เมื่อ H7
  ติดตั้งอยู่, rev 4), `depth`
  (ความลึก phase stack ขณะ ev), **`bucket` (`pre|creation|post` — จากจุดเวลาเทียบ `sng.begin`/
  `sng.end`, rev 3)**, **`iid` (invocation id — เฉพาะ event `factory.*`: ผูก `factory.call`/
  `factory.result`/สถานะ finalizer ของ invocation เดียวกัน; มาจากกลไก typed ที่พิสูจน์แล้ว —
  ห้ามใช้ timestamp เดี่ยวแทน, rev 8)**, `err` (ต่อเมื่อ >0)
- เหตุการณ์: `session.begin/end`, **`patch.audit`** (**4 จุดตรวจ, rev 12** — `at=session-begin` |
  `at=sng-prefix` (ใน prefix ของ `StartNewGame` ก่อน original ทำงาน) | `at=genhero-prefix`
  (ก่อนเข้า W2) | `at=genenemy-prefix` (ก่อนเข้า W3) — จุด `genhero`/`genenemy` ทำใน H2/H4
  ที่ติดอยู่แล้ว ไม่เพิ่ม hook) —
  ต่อ method เป้าของ H3 overload: inventory จริง**ครบทุก category ที่ API เปิดเผย**
  (`Prefixes`/`Postfixes`/`Transpilers`/`Finalizers`/`ILManipulators` — HarmonyX 2.10.2; ไม่มี
  InnerPrefix/InnerPostfix ให้ตรวจ) รายการ owner + patch method + จำนวน จาก
  `Harmony.GetPatchInfo` เทียบ **expected inventory** + ผล match/mismatch **+ ผลเทียบระหว่าง
  จุดตรวจ (stable/changed — ครอบเฉพาะช่วงระหว่างจุดตรวจ ไม่ครอบภายใน window, rev 13)** —
  ผลของแต่ละจุดคือ `inventoryAtWindowEntry` ของ window ถัดไป — read-only, มองเห็นเฉพาะ
  Harmony-registered patch (rev 10–13)),
  `sng.begin/end`, `heroNum.read` (ที่ `genhero.begin` — `heroNum`
  = array of int arrays + `speDb` = ขนาดตาราง `SpeHeroDataBase` + identity — ทางหลักของ T0),
  `heroNum.crosscheck` (เฉพาะเมื่อเปิด H8), `genhero.begin/end`, `genenemy.begin/end` (`maxNum` +
  counts), **`factory.call`** (prefix — `ov`: A/B/C + args ครบทุกตัว + arg flags + `db` = nonNull/
  pointer ของ `heroDataBase` เฉพาะ A/C (B = "n/a (wrapper)") + `name` ตัดที่ 32 ตัวอักษรเมื่อไม่ null),
  **`factory.result`** (postfix — รันเมื่อ pipeline มาถึงจุด postfix โดยไม่มี exception ณ จุดนั้น;
  **ยังไม่พอตัดสิน "สำเร็จ" เอง** — ต้องยืนยันด้วยสถานะ finalizer ของ invocation เดียวกัน, rev 8) —
  `__result` nonNull? + **สถานะหลัง factory** ของ target:
  `isTempHero`/`isRandomEnemy`/`speHero`/`belongForceID`/`heroID` อ่าน typed จาก `__result`
  (ชนิด `HeroData`, OBSERVED dump.cs) อย่างเดียว — rev 6,
  **`factory.finalizer`** (**event เชิงบวก — บันทึกทุก invocation ทั้ง `__exception == null` และ
  `!= null`**, rev 9: หลักฐาน lifecycle ครบทุกเคส) — ฟิลด์ `iid` +
  `exceptionAtProbeFinalizer` (null | ชนิด exception ที่สังเกต ณ finalizer ของ probe) + ยืนยัน
  phase pop,
  **`factory.failure`** (**conditional — ยิงเมื่อ `__exception != null` เท่านั้น**, rev 8) —
  บันทึก**รายละเอียด** exception คู่กับ `factory.finalizer` ของ `iid` เดียวกัน (ชนิด exception;
  ความหมาย = **exception observed in the patched invocation pipeline** — ยังไม่ได้แปลว่า original
  method throw เว้นแต่มีหลักฐานแยกแหล่งได้จริง); คืน exception เดิมโดยไม่กลืน/ไม่แทนที่;
  **ไม่มีการอ่าน `__result` ใน finalizer** (ห้ามสมมติว่าใช้ได้เหมือน postfix, rev 7),
  `addtemp` (`heroID`, counts), `worldadd` (args + result nonNull — เฉพาะเมื่อเปิด H6),
  `changeday/changemonth/manage.begin/end` (เฉพาะเมื่อเปิด H7), `worldcount` (histogram + perForce +
  **`wid`/`hidH`/`hidT`** — identity ของ instance, §2.2), `outside` (factory/wrapper call ที่ stack
  ไม่มี scope ใด — พบได้ใน bucket `pre`/`post`; **ใน bucket `creation` stack ที่มีแค่ราก `sng`
  ถูกแท็ก `sng.unattributed` ไม่ใช่ `outside`**, §2.1), **`phaseStuck`** (phase stack ไม่สมดุล —
  พร้อมค่า reset, §2.5), **`rebaseline`**
  (instance เปลี่ยนระหว่างจุดวัด — พร้อม pointer เก่า/ใหม่, §2.2)
- **หลีกเลี่ยงข้อมูลส่วนเกิน:** ไม่ log ชื่อ hero ยาวเต็ม (ตัด 32), ไม่ log ทั้ง object หรือ field ที่
  ไม่เข้าสมการ, ไม่ log per-frame (มีแค่ที่ boundary), ไม่ log เนื้อหาบทสนทนา/ข้อมูลเซฟ
- ขนาดคาดการณ์: ~1,100 เหตุการณ์/รอบ ≈ 150–250 KB — ใช้ buffered writer (รูปแบบ `CaptureWriter`/
  `ProbeSink` เดิม: lock + flush ที่ boundary + `session.end`) — ลด I/O บน main thread

### 2.5 จุดหยุด log / exception recovery / การถอน (งานเตรียมข้อ 6 ต่อ)

- หยุดเขียนโดยอัตโนมัติเมื่อ: (ก) `session.end` หลัง `sng.end` + settle window (ค่าเสนอ 10 นาที
  หรือผู้ใช้สั่งหยุดก่อน) (ข) เกิน cap เหตุการณ์ 50,000 (ค) ปิดเกม (`OnDeinitializeMelon` → flush
  + Uninstall) — ทั้งสามทาง flush ก่อนเสมอ
- exception ใน hook ใด: กลืน + `err` counter + ทำ marker บรรทัดถัดไปที่เขียนสำเร็จ — **ห้ามโยนออก
  จาก hook** (เกมต้องเดินต่อได้ทุกกรณี); ถ้า hook ติดตั้งสำเร็จ < จำนวนที่กำหนดขั้นต่ำ (CORE H1–H5
  ต้องครบ) ⇒ ถอนทั้งชุด (`UnpatchSelf`) แล้วรายงาน ไม่รันรอบด้วย hook ที่ขาด
- **finalizer contract — ฉบับ canonical (rev 5 ฐาน, รวม rev 2/7/8/9 — บังคับ · เนื้อหาส่วนนี้แทนที่
  bullet "phase cleanup (rev 2)" และ "finalizer contract" เดิมที่เคยพูด lifecycle ซ้ำกัน):**
  · **lifecycle:** finalizer รัน**ทุก invocation** — ทั้งกรณีสำเร็จและกรณีมี exception — และทำ
  phase pop เสมอ (ห้ามผูกการทำงานของ finalizer ไว้เฉพาะตอน throw; ใช้ postfix เฉย ๆ ไม่พอ เพราะ
  original throw แล้ว postfix ไม่ถูกเรียก ⇒ phase ค้าง; static audit: 1 push ↔ 1 pop ครบทุก
  เส้นทางของทุก hook ที่ตั้ง phase)
  · **events:** `factory.finalizer` = event **เชิงบวก บันทึกทุก invocation** (`iid` +
  `exceptionAtProbeFinalizer` = null | ชนิด exception + ยืนยัน phase pop); `factory.failure` =
  **conditional** — บันทึกรายละเอียด exception เมื่อ `__exception != null` เท่านั้น (คู่ `iid`
  เดียวกัน)
  · **ความหมาย exception:** `exceptionAtProbeFinalizer` = exception **ที่สังเกต ณ finalizer ของ
  probe** = "exception observed in the patched invocation pipeline" — ห้ามอ้างว่า original method
  throw เว้นแต่มีหลักฐานแยกแหล่ง; **ไม่เท่ากับการสรุปว่า pipeline ทั้งหมดสะอาด** (ต้องพิสูจน์
  patch-audit/ordering ก่อน — ด้านล่าง)
  · **ต้องคืน exception เดิมโดยไม่กลืน/ไม่แทนที่** (ห้าม `__exception = null`, ห้ามโยน exception
  ใหม่ออกนอก hook) + **catch exception ของ probe เองภายใน handler** → marker `probeErr` แยก —
  เกมต้องไม่เห็น exception จาก probe
  · **บทบาท 3 handler:** `factory.call` = prefix (call/arguments) · `factory.result` = postfix
  (ผล + post-state) · finalizer = phase cleanup (ทุกกรณี) + event ตามด้านบน — **ห้ามสมมติว่า
  finalizer ใช้ `__result` ได้เหมือน postfix** (post-state อ่านใน postfix เท่านั้น)
  · **invocation linkage (rev 8–9):** `factory.call`/`factory.result`/`factory.finalizer` ของ
  invocation เดียวกันผูกด้วย `iid` จาก**กลไก typed** — ผู้สมัคร (1) Harmony `__state`: ต้องยืนยัน
  ว่า prefix/postfix/finalizer **แชร์ state ได้ใน patch arrangement ที่ใช้** (ประกาศชนิด state
  เดียวกันทั้งสาม handler ใน patch เดียว) และ state ต่อ invocation แยกจากกันเมื่อ invocation ซ้อน
  (2) thread-local LIFO ของโมด (ควบคุมเองครบ) — **ทั้งสองทางต้องพิสูจน์ด้วย harness test ตอน build
  ก่อนระบุเป็นข้อเท็จจริง**; **timestamp เดี่ยวห้ามใช้ยืนยัน identity**; พิสูจน์ linkage ไม่ได้ ⇒
  accounting ที่พึ่ง linkage (`#C.ok` → E1/E2) คง UNKNOWN
  · **การใช้ `exceptionAtProbeFinalizer` เป็นหลักฐานของ `#C.ok` (rev 9, ขอบเขต rev 10):** ต้อง
  พิสูจน์ให้ได้ว่า (ก) **patch-audit ผ่าน expected inventory** — สำหรับ H3 overload แต่ละตัว
  เทียบ patch ที่พบจริง (owner + patch method + จำนวน) จาก
  `Harmony.GetPatchInfo(targetMethod)` กับ**รายการที่ probe คาดหมายชัดเจน ครบทุก category ที่
  API เวอร์ชันจริงเปิดเผย (HarmonyX 2.10.2: Prefixes/Postfixes/Transpilers/Finalizers/
  ILManipulators)** — expected ต่อ overload: 1 prefix + 1 postfix + 1 finalizer ของโมด,
  Transpiler = 0, ILManipulator = 0 — **ผ่านได้เมื่อตรงเป๊ะทั้งรายการ ห้ามผ่านเพียง
  เพราะ "ทุก owner เป็น probe"** จึงรับประกันว่า finalizer ของ probe เป็นจุดสุดท้ายของ pipeline
  นั้น และ (ข) ไม่มี handler อื่นที่อาจ throw ต่อจากจุดนั้น — เมื่อ (ก)+(ข) ผ่าน ⇒
  `exceptionAtProbeFinalizer == null` สรุปได้เฉพาะ **"ไม่มี exception ที่สังเกตได้ในขอบเขตของ
  patching pipeline ที่ตรวจสอบแล้ว"**; **ถ้ามี patch อื่นบน method เป้า (เช่น mod อื่น) หรือพิสูจน์
  ordering ไม่ได้ ⇒ ห้ามสรุป success ทั้ง pipeline ⇒ E1/E2 คง UNKNOWN**
  · **patch-audit scope + ข้อจำกัด (rev 10–11):** `Harmony.GetPatchInfo(targetMethod)` มองเห็น
  **เฉพาะ patch ที่ลงทะเบียนผ่าน Harmony API/patching runtime ที่ใช้อยู่** และ
  `GetAllPatchedMethods()` เป็นเพียงดัชนีเมธอดที่ถูก patch — **ไม่ใช่หลักฐานโดยตัวมันเองว่าไม่มี
  detour/instrumentation จากระบบอื่น** (เช่น native detour นอก Harmony หรือ Harmony assembly
  อื่นของ mod อื่นซึ่งมี patch store แยก) · **ข้อจำกัดตามเวอร์ชัน (rev 11):** HarmonyX 2.10.2
  **ไม่เปิดเผย InnerPrefix/InnerPostfix** (มีใน HarmonyX ใหม่กว่า) — patch ประเภทนั้น (ถ้ามีจาก
  runtime อื่น) มองไม่เห็นใน inventory นี้ ⇒ **ห้ามถือว่า inventory ผ่านสมบูรณ์เกินขอบเขตที่ API
  ตรวจเห็น**; ผล audit อ้างได้แค่ขอบเขตที่ Harmony 2.10.2 มองเห็น; **ยืนยันขอบเขต runtime ไม่ได้
  = พิสูจน์ความสะอาดของ pipeline ทั้งหมดไม่ได้** — ห้ามใช้ `exceptionAtProbeFinalizer == null`
  เดี่ยวสรุป pipeline success · **ถ้อยคำที่อ้างได้: "ไม่มี exception ที่สังเกตได้ในขอบเขตของ
  patching pipeline ที่ตรวจสอบแล้ว"** — ห้ามใช้ "ไม่มี exception ใน pipeline ทั้งหมด" เว้นแต่
  พิสูจน์ขอบเขตครบ; ข้อมูล T0 ส่วนอื่นที่ไม่พึ่งข้อสรุปนี้ (heroNum, E4, E5/E6, census) **ยังใช้ได้
  ตามปกติ ไม่ต้องยกเลิก**
  · **audit timing + ความเสถียร (rev 11–12, แยกความหมาย rev 13):** `patch.audit` ทำ **4 จุดตรวจ**
  — (1) ที่ `session.begin` (2) ใน prefix ของ `StartNewGame` **ก่อน original ทำงาน** (3) ใน prefix
  ของ `GenerateHero` **ก่อนเข้า W2** (4) ใน prefix ของ `GenerateRandomEnemy` **ก่อนเข้า W3** —
  จุด (3)(4) ทำใน H2/H4 ที่ติดอยู่แล้ว (**ไม่เพิ่ม hook**, read-only) ตรวจ H3 overload ทั้งสามด้วย
  expected inventory ครบ 5 categories ของ HarmonyX 2.10.2 · **สองปริมาณแยกกัน (rev 13):**
  **`inventoryAtWindowEntry`** = ผลตรวจ ณ จุดตรวจก่อนเข้า window (point-in-time) ·
  **`inventoryStability`** = หลักฐานว่า inventory ไม่เปลี่ยน**ตลอดช่วงที่ใช้คำนวณ `#C.ok`** —
  การเทียบจุดตรวจซ้ำเท่ากันพิสูจน์เฉพาะช่วง**ระหว่างจุดตรวจ**; **ห้ามใช้ `inventoryAtWindowEntry`
  เดี่ยวพิสูจน์ความเสถียรภายใน window** · **กติกาต่อ window:** `#C.ok` ใน window ใดใช้ได้เมื่อ
  `inventoryAtWindowEntry` ตรง expected **และ** `inventoryStability` พิสูจน์ได้ —
  **inventory เปลี่ยน / อ่านไม่ครบ / ระบุสาเหตุของความไม่ตรงไม่ได้ ⇒ E1/E2 คง UNKNOWN** ·
  **สถานะความเสถียรภายใน window (rev 13):** รอบนี้**ไม่เพิ่ม hook และไม่เพิ่มกลไกติดตามการ
  เปลี่ยน patch ระหว่าง runtime** ⇒ stability ภายใน window = **UNKNOWN** ⇒ **`#C.ok` และ
  E1/E2 ที่พึ่ง pipeline-success คง UNKNOWN** — แต่**ผล audit ณ จุดตรวจยังเป็นข้อมูลที่ใช้ได้**
  (บันทึกตามจริง, ใช้ประกอบการวิเคราะห์อื่นและเป็นฐานของรอบถัดไป); หากต้องการปิดช่องนี้
  (เช่น re-check inventory ใน handler ที่มีอยู่ ณ สิ้น window) ต้อง**ขออนุมัติแยกในรอบถัดไป** —
  ห้ามเริ่มเอง · **ขอบเขต (rev 12):** audit ณ จุดตรวจแต่ละจุด = หลักฐานสถานะ**ณ ช่วงตรวจ
  เท่านั้น** — ไม่ใช่การพิสูจน์ว่าไม่มี native detour หรือไม่มีการเปลี่ยน patch ระหว่างทุก invocation
  — ห้ามอ้างเกินหลักฐาน
  · **ถ้าพิสูจน์/รับประกัน contract นี้ไม่ได้ (เช่นต้องกลืน exception ของเกมจึงจะเขียน log ได้) =
  NO-GO ก่อน runtime (§5)**
- **แยก phase attribution ออกจาก stack integrity (rev 5):** `sng.unattributed` เป็น**ปัญหาบัญชี/
  การระบุ caller** (E5/E6) — ไม่ใช่ `phaseStuck` โดยตัวมันเอง; `phaseStuck` วัดความสมบูรณ์ของ
  stack เท่านั้น — ปัญหาสองประเภทนี้บันทึก marker คนละชุด ไม่แทนกัน
- **พิสูจน์ phase stack ไม่ค้างข้ามการเรียก factory (rev 2, ปรับ rev 3–5):** `phaseStuck` ตรวจ 4
  คลาสของ nesting ที่ผิดรูปจริง — (1) pop ไม่ตรง scope ของ push (ต้องคู่กันแบบ LIFO) (1b) scope
  ที่เปิดแล้ว**ไม่ถูกปิด** (leak — เช่น push ที่ไม่มี pop คู่แล้วเจอเหตุการณ์อื่นมาขณะ scope ยังค้าง)
  (2) ใน bucket
  `creation`: stack ของ factory call ไม่อยู่ในชุด legal ของ bucket นั้น (`sng → genhero` /
  `sng → genenemy` / รากเดี่ยว `sng` = `sng.unattributed`) เช่น depth > 2 หรือ `genenemy` ไร้
  `sng` parent (3) `session.end` แล้ว stack ไม่ว่าง — พบข้อใดข้อหนึ่ง ⇒ เขียน `phaseStuck` +
  auto-reset สู่ phase ที่คาด (`sng` หรือ `outside`) + นับ error
  **ไม่ใช่ stuck (rev 4):** `genenemy` ใน bucket `post` ที่ไม่มี `sng` parent = การทำงานปกติของ
  `ChangeDay` (§2.1) — บันทึกตามจริง; ถ้า hook ที่ให้ context `ChangeDay` (H7 OPTIONAL) ติดตั้ง
  อยู่แล้ว บันทึก context เป็น `changeday.genenemy` เพิ่มได้ — **ห้ามบังคับให้ต้องติด H7**
  ⇒ **เกณฑ์ความถูกต้องของรอบ: `phaseStuck == 0` ตลอดรอบ** — ไม่ศูนย์ ⇒ รอบนั้นใช้ตัดสิน
  บัญชีไม่ได้ (รายงานและรันใหม่ ไม่ตีความตัวเลขจากรอบที่ติดธง)
- **หลายปัญหาพร้อมกัน (rev 5):** ถ้าเกิดหลายปัญหาในเหตุการณ์/รอบเดียว (เช่น `phaseStuck` + 
  `sng.unattributed` + `readfail`/`probeErr`) บันทึก**แต่ละ marker แยกบรรทัด/แยก field กัน** —
  ห้ามรวมเป็น error เดียวจน marker ใดถูกปิดบัง และห้ามใช้ marker หนึ่งปิดข้อสรุปของอีก marker
- การถอน: pref ของ probe **default false** (ชื่อเสนอ `Sp5T0`, หมวดเดิม `[LongYinCoop]`) — ปิด pref =
  Uninstall ทันที (idiom JitProbe ต่อ wave); ห้ามเกี่ยวกับ `AllowRemoteInput`/`DevCommands` ตาม
  มติ rev4; ตรวจหลังถอนด้วย `CompatInventory.CountOwnedPatches` ต้องคืนเป็น 0
- thread: เป้า hook ทุกตัวถูกเรียกบน Unity main thread (creation flow — ข้อเท็จจริง S0) ⇒
  ไม่มี cross-thread access; writer เท่านั้นที่อาจ flush ฝั่งอื่นผ่าน lock

### 2.6 ก่อนรอบรัน — **แยกสองขั้น**: ขั้น A preflight (read-only) กับ ขั้น B save-split (เปลี่ยน filesystem — rev 5)

(ทำกับเกมปิดทั้งสองขั้น, **ไม่เปิดเกมในขั้นนี้**; ลำดับ: **A → [อนุมัติ split] → B → ติดตั้ง/ตั้งค่า
probe → ผู้ใช้เปิดเกม**)

**ขั้น A — preflight read-only** (ไม่เขียน/ไม่ย้าย/ไม่แก้ไฟล์ใด ๆ ทั้งสิ้น — ต้อง PASS ครบก่อน
สั่งขั้น B):

1. hash เกม 2 ไฟล์ตรง `VERSION.md` + ไม่พบ `LongYinLiZhiZhuan.exe` ใน tasklist
2. **อ่านสถานะ save ปัจจุบันแบบ read-only** (`save-split status`): `Save` ตรง baseline? ·
   `leftovers`? · มี `Save.real-*`/`Save.used-*` ค้าง? — บันทึกผลไว้เทียบก่อน/หลังขั้น B
3. **UserData baseline:** บันทึก sha256 + mtime ของ `UserData\MelonPreferences.cfg`,
   `UserData\LongYinCoop\net.cfg` (T0 **ไม่ต้องใช้ net.cfg** — คาดหมาย absent หรือ `enabled=false`),
   และไฟล์ config อื่นของโมด — เทียบท้ายรอบ
4. **รายการไฟล์ที่เกม/ML เขียนเองได้** (จำแนกล่วงหน้าจากหลักฐาน RUNTIME เดิม):
   `PlayerprefData.dat` = volatile — เขียนทุกครั้งที่เปิด/ปิดเกม (drift เป็นเรื่องปกติ ⇒
   rebaseline volatile-only ห้าม restore), `MelonLoader\Logs\*` = log ของ ML, `SaveSlot*` =
   **ไม่**ถูกเขียนตอนเปิด/ปิด (ยืนยันจาก incident ×3) — ตารางนี้คือเกณฑ์ตัดสินท้ายรอบว่า
   drift ใด "คาดหมาย" ใด "ผิดปกติ" (ผิดปกติ = หยุดรายงาน)
5. **ปลายทางเขียนของโมดอื่นปิด:** `s2-snapshots.ndjson` และ evidence dir ของ S2 ต้องไม่โตระหว่างรอบ
   (S2 disabled), ไม่มี recorder/IPC client ค้างจากรอบก่อน
6. สรุปผล = PASS/FAIL รายข้อ พร้อม hash/mtime — **FAIL ข้อใด = ห้ามเดินขั้น B และห้ามสั่งเปิดเกม**

**ขั้น B — save-split (rev 5: ไม่ใช่ read-only — เปลี่ยน filesystem state ชัดเจน):**

- การ split **ย้ายโฟลเดอร์** (`Save` → `Save.real-<label>`, สำเนากลับมาเป็น `Save`) จึงเป็นการ
  ดำเนินการที่เปลี่ยน state — **ต้องได้รับอนุมัติแยกก่อนดำเนินการ** (§4) ห้ามเรียกขั้นนี้ว่า read-only
  ในเอกสาร/รายงานใด
- **hash ก่อนและหลัง:** ก่อน split — `savehash.real.before.txt` (ของ `Save` ขณะยังเป็นของจริง) ·
  หลัง split — `savehash.copy.before.txt` + ยืนยันจำนวนไฟล์/digest ตรงกันทั้งสองฝั่ง · ทั้งสองไฟล์
  เก็บใน evidence ของรอบ
- **ขั้นตอนคืนสภาพชัดเจนก่อนเริ่ม:** `save-split restore --label <label>` + post-check (จำนวนไฟล์
  EXACT ทุก baseline, `leftovers: none`, rehash อิสระ) — ตามโปรโตคอล §1.2 ที่ใช้มาแล้ว; ถ้า
  post-check ไม่ผ่าน = หยุดและรายงานก่อนเดินต่อ

---

## 3) ความเสี่ยง / ข้อจำกัด และสิ่งที่ T0 พิสูจน์ไม่ได้

| ความเสี่ยง/ข้อจำกัด | ป้าย | การรับมือในแผน |
|---|---|---|
| `StartNewGame` สร้าง `WorldData` ใหม่ ⇒ list instance เปลี่ยน — Δ-count ข้าม instance ไม่ได้ | OBSERVED (โครงสร้าง) | identity `wid`/`hidH`/`hidT` ทุก snapshot + baseline ตั้งหลัง world พร้อม + `rebaseline` + สมการข้าม instance = ไม่ปิด (§2.2) |
| phase stack ค้างเมื่อ original throw (postfix ไม่ถูกเรียก) | UNKNOWN ถ้าใช้ postfix เฉย ๆ | pop ผ่าน Harmony **finalizer** + depth guard + `phaseStuck == 0` เป็นเงื่อนไขความถูกต้องของรอบ (§2.5) |
| factory call ใน creation round แต่นอก W2/W3 (`sng.unattributed`) — caller ที่เป็นไปได้ เช่น lambda `<StartNewGame>b__7_0`/`StartNewGameSetting` ยังไม่ถอด | UNKNOWN ถ้าพบ | บัญชียังปิดไม่ได้ — ระบุ caller จากบริบท log (ลำดับเวลา/boundary/marker ที่เปิดไว้) หรือคง UNKNOWN; **ห้ามสรุป path ที่สองทันที** (E5/E6, §2.1) — ถ้าต้องเพิ่ม hook เพื่อระบุ caller = เสนออนุมัติรอบแยก |
| เหตุการณ์ `post` ที่ถูกต้อง (`genenemy` ไร้ `sng` parent จาก `ChangeDay`) ถูกนับเป็น stack ผิดรูป / ดึง creation round ให้ตกเงื่อนไขความถูกต้อง | — (กันไว้ rev 4) | กติกา nesting แยกต่อ bucket (§2.1/§2.5): `phaseStuck` จับเฉพาะ nesting ผิดรูปจริง; `post` ไม่เข้า E1–E7 และไม่กระทบความถูกต้องของ creation round ย้อนหลัง (§5) |
| จำแนก SpeHero vs generic ใน factory calls ด้วย `belongForceID` เดี่ยว — ผิดแน่นอน (SpeHero เร่ร่อนใช้ forceID −1 เหมือน generic) | OBSERVED (ตาราง) + INFERRED (โครงลูป) | E4 rev 6: `heroDataBase` มีเฉพาะ overload A/C = **เงื่อนไขทางเลือกเท่านั้น** (พิสูจน์ interop อ่านได้จริงก่อน ห้ามตัดสินเดี่ยว ๆ); `heroName`/`heroID` ห้ามใช้เดี่ยว ๆ; กฎสรุปจากหลักฐานที่สังเกตได้ทั้งหมด — ไม่พอ ⇒ E4 คง UNKNOWN (E1/E2/E7 ไม่พึ่ง E4) |
| arg flags (`isTempHero`/`isRandomEnemy` ก่อนเรียก) ≠ สถานะ field หลัง factory ของ target `HeroData` | UNKNOWN (ต้องวัดเทียบ) | H3 log ทั้ง `factory.call` (args) และ `factory.result` (post-state อ่าน typed อย่างเดียว) เทียบกันได้; **อ่าน post-state ไม่ได้ ⇒ E1/E2 คง UNKNOWN — ห้ามอนุมานจาก args** (rev 6) |
| finalizer ของ Harmony อาจถูกเขียนให้กลืน exception ของเกม (เปลี่ยนพฤติกรรม original) | — (กันไว้ rev 5) | ข้อกำหนดบังคับ: finalizer คืน exception เดิมตาม Harmony contract, exception ของ probe ถูกกลืนภายใน + log marker `probeErr` แยก; รับประกันไม่ได้ = NO-GO ก่อน runtime (§5) |
| สมมติว่า finalizer อ่าน `__result` ได้เหมือน postfix ⇒ post-state จาก finalizer ไม่น่าเชื่อถือ / success ถูกนับผิด | — (กันไว้ rev 7) | event contract แยกชัด: post-state อ่านเฉพาะใน `factory.result` (postfix); finalizer = phase cleanup (ทุกกรณี) + `factory.finalizer`/`factory.failure` ไม่แตะ `__result` (§2.4/§2.5); success = `factory.result` non-null **+ `factory.finalizer` รายงาน `exceptionAtProbeFinalizer == null` + patch-audit ผ่าน** (rev 9) |
| postfix คืนผลปกติแต่ exception เกิดต่อจาก handler อื่นใน pipeline เดียวกัน — `factory.result` เดี่ยวไม่พอตัดสิน "สำเร็จ" | — (กันไว้ rev 8) | `#C.ok` ต้องยืนยันด้วย `factory.finalizer` ของ invocation เดียวกัน (`exceptionAtProbeFinalizer == null`) **+ patch-audit** (`GetPatchInfo` — มีแค่ patch ของ probe); เชื่อมโยง/พิสูจน์ไม่ได้ ⇒ E1/E2 คง UNKNOWN (§2.3/§2.5) |
| `exceptionAtProbeFinalizer == null` ถูกอนุมานเป็น "pipeline ทั้งหมดสะอาด" โดยไม่พิสูจน์ patch-audit/ordering (finalizer ของ probe อาจไม่ใช่จุดสุดท้าย หรือมี mod อื่น patch method เดียวกัน) | — (กันไว้ rev 9–10) | ต้องพิสูจน์ (ก) patch-audit **เทียบ expected inventory เป๊ะ** (owner + patch method + จำนวน ต่อ overload รวม Transpiler) (ข) ไม่มี handler อื่นที่อาจ throw ต่อ; ผ่านแล้วสรุปได้เฉพาะ **"ไม่มี exception ที่สังเกตได้ในขอบเขตของ patching pipeline ที่ตรวจสอบแล้ว"**; ไม่ครบ ⇒ **ห้ามสรุป success ทั้ง pipeline** — E1/E2 คง UNKNOWN แต่ข้อมูล T0 ส่วนอื่นใช้ต่อได้ (§2.5) |
| detour/instrumentation จากระบบอื่น (นอก Harmony) บน method เป้า — `GetPatchInfo`/`GetAllPatchedMethods` มองไม่เห็น; Harmony assembly อื่นของ mod อื่นมี patch store แยก | UNKNOWN (ขอบเขตเครื่องมือ) | ประกาศขอบเขตชัด (§2.5 rev 10–12): ผล patch-audit ครอบเฉพาะ Harmony-registered patch ของ runtime ที่ใช้ (HarmonyX 2.10.2) และเป็นหลักฐาน**ณ จุด/ช่วงตรวจ** — ไม่ใช่การพิสูจน์ว่าไม่มี detour หรือไม่มีการเปลี่ยน patch ระหว่างทุก invocation; ถ้อยคำสรุปต้องมีคำว่า "ในขอบเขตที่ตรวจสอบแล้ว" — ห้ามอ้างเกินหลักฐาน |
| HarmonyX 2.10.2 ไม่เปิดเผย InnerPrefix/InnerPostfix (มีใน HarmonyX ใหม่กว่า) — patch ประเภทนั้นจาก runtime อื่นมองไม่เห็นใน inventory | OBSERVED (reflection บน 0Harmony.dll จริง) | ระบุเป็นข้อจำกัดของ inventory (§2.5): **inventory ครบเฉพาะ 5 categories ที่ API เปิดเผย** (Prefixes/Postfixes/Transpilers/Finalizers/ILManipulators) — ห้ามถือว่าผ่านสมบูรณ์เกินขอบเขตนี้ (rev 11) |
| inventory เปลี่ยนระหว่างจุดตรวจ (patch ถูกเพิ่ม/ถอดกลางทาง — ระหว่าง session-begin, sng-prefix, genhero-prefix, genenemy-prefix) | UNKNOWN ถ้าตรวจจุดเดียว | audit 4 จุดตรวจ (`session-begin` + `sng-prefix` + `genhero-prefix` ก่อน W2 + `genenemy-prefix` ก่อน W3) — `inventoryAtWindowEntry` ของ window ใดต้องตรง expected จึงผ่านเงื่อนไขแรก; เปลี่ยน/อ่านไม่ครบ/ระบุสาเหตุไม่ได้ ⇒ E1/E2 คง UNKNOWN เว้นแต่มีหลักฐานอื่นปิดข้อจำกัด (rev 12) |
| ความเสถียร**ภายใน window** (ระหว่างจุดตรวจกับ factory call จริง — patch เปลี่ยนกลาง window) ไม่มีกลไกตรวจในรอบนี้ | UNKNOWN โดยการออกแบบ (rev 13) | แยก `inventoryAtWindowEntry` (จุดตรวจ) ออกจาก `inventoryStability` (ตลอด window); `#C.ok`/E1/E2 ที่พึ่ง pipeline-success คง UNKNOWN จนกว่าจะมีกลไกพิสูจน์ stability ที่ผ่านอนุมัติ (เช่น re-check ใน handler ที่มีอยู่ — ขออนุมัติแยก); **ผล audit ณ จุดตรวจยังเป็นข้อมูลที่ใช้ได้** — ห้ามเปลี่ยน UNKNOWN เป็น PASS ด้วยการอนุมาน (§2.5) |
| invocation ซ้อนกันแยก identity ด้วย timestamp เดี่ยวไม่ได้ — call/result/finalizer จับคู่ผิด | — (กันไว้ rev 8) | identifier จากกลไก typed: Harmony `__state` (**ยืนยันว่า prefix/postfix/finalizer แชร์ state ได้ใน patch arrangement ที่ใช้** — ชนิด state เดียวกันทั้งสาม handler ใน patch เดียว) หรือ thread-local LIFO — **พิสูจน์ด้วย harness test กับ nesting ก่อนระบุเป็นข้อเท็จจริง**; พิสูจน์ไม่ได้ ⇒ accounting ที่พึ่ง linkage คง UNKNOWN |
| ทางหลักอ่าน `heroNum` ต้องพึ่ง private `LoadAllGameData` | — (ไม่ใช่แล้ว rev 2) | ทางหลัก = อ่านที่ `GenerateHero` prefix แบบ typed; `LoadAllGameData` เป็น OPTIONAL cross-check ที่ถอดออกได้โดยไม่กระทบ T0 (§1.2–1.3) |
| hook shared generic `List<T>.Add` โดนทุก T | OBSERVED (SP5body) | แผนหลักไม่ใช้; แผนสำรอง = arm สั้น + ReferenceEquals ต้องอนุมัติแยก |
| overload `#2` ของ SP4 อาจไม่ใช่ C ตามที่ static ชี้ | INFERRED | H3 log ลายเซ็นครบทุก arg ทุก overload ⇒ mapping ปิดด้วยข้อมูลจริง |
| interop instance บางตัว null ในช่วงต้น (Instance ยังไม่ถูกสร้าง) | UNKNOWN | readfail marker + ข้าม; ไม่ retry ซ้ำใน loop สั้น |
| ปริมาณ hook OPTIONAL (H6–H11, รวม 16 ของ H11) ใหญ่ | — | ทั้งชุด optional; รอบสะอาดออกแบบให้ไม่จำเป็น (ดีฟอลต์ไม่ติดตั้ง) |
| probe อาจเพิ่มเวลาสร้างโลก (log ทุก factory call ของรอบ) | INFERRED | buffered write; บันทึกเวลา `sng.begin→end` เทียบค่าที่เคยวัด (~1 s) — เกิน band ชัดเจน = รายงาน |
| ค่า heroNum เปลี่ยนระหว่างโหลดกับ `genhero.begin` | UNKNOWN | เปิด H8 cross-check (optional) + เทียบ — ถ้าไม่เปิด รายงานเป็น UNKNOWN ที่ไม่บล็อก E1 |
| กรณี SP4 path ที่สอง | UNKNOWN | E6 ตรวจด้วยตัวเลขของรอบเอง; พบ ⇒ NO-GO + คง UNKNOWN |
| drift ไฟล์ที่เกม/ML เขียนเองตอนเปิด/ปิด ปนกับผลของ probe | UNKNOWN ถ้าไม่จำแนก | preflight §2.6 แจกแจง volatile ล่วงหน้า + เทียบ hash/mtime ท้ายรอบ |

**T0 พิสูจน์ได้:** ค่า `heroNum` + จำนวน entry · mapping overload + จำแนก args · บัญชี Heros/TempHeros
รายคลาส · จุดสร้างตัวเอก (E3) · การแยกเส้นทาง (E5 — creation ครบ W2/W3, `sng.unattributed == 0`) ·
กรณี SP4 (E6) · ค่า `GetRandomEnemyCount`/
การจัด ID (ถ้าเปิด H10)

**T0 พิสูจน์ไม่ได้ (ต้อง runtime tests ของ design §6.2 ต่อ):** ผลของสระ hero เล็กลงต่อ
Plot/Mission ที่เปิดจริง (T6) — T0 ไม่เปิด plot · save/load/save digest (T8) · long-run refill
ไม่เด้งกลับ (T5) · guest มองเห็นตรง (T9) · พฤติกรรมหลังลดจริงทั้งหมด (ต้อง implement ก่อน)

---

## 4) รายการที่ต้องอนุมัติก่อนลงมือจริง

1. **อนุมัติแผน T0 (rev 7 — ฉบับสุดท้ายของเอกสารนี้) แล้วจึงเขียนโค้ด probe** (ตามกติกา 1 — แผน stage
   ก่อนลงมือ)
2. build/deploy DLL ใหม่ที่มี probe (probe = โค้ดใหม่ใน mod) + ตั้ง `Sp5T0=true` ใน cfg **ก่อนเปิดเกม**
   (ลำดับ: **preflight §2.6 ขั้น A → [อนุมัติ split] → ขั้น B → เตรียม probe → ผู้ใช้เปิดเกม**)
3. preflight §2.6 **ขั้น A (read-only)** ต้อง PASS ครบทุกข้อ (hash เกม / save status / UserData
   baseline / ตาราง volatile) และ **ขั้น B (save-split) ต้องได้รับอนุมัติแยกก่อนดำเนินการ** พร้อม
   hash ก่อน/หลังและขั้นตอนคืนสภาพที่บันทึกไว้ — ก่อนสั่งเปิดเกม
4. ทดสอบบนสำเนาเซฟตามโปรโตคอล §1.2 (โลกใหม่ไม่แตะเซฟจริง — แต่เกมยังต้องเปิดบนสำเนา)
5. OPTIONAL hooks: เลือกเปิด H6/H7 (วัดรายวัน–รายเดือนภายหลัง), H8 (cross-check heroNum), H9/H10,
   H11 ตัวไหน — **ดีฟอลต์ของรอบแรก: ไม่ติดทั้งหมด** (CORE H1–H5 พอสำหรับสมการ E1–E7)
6. แผนสำรอง List.Add (§2.2) — อนุมัติเฉพาะเมื่อ residual ≠ 0 จริง
7. ค่า settle window / event cap / ตำแหน่งไฟล์ evidence ของรอบรัน
8. ถ้า typed read ล้มเหลวและต้องการใช้ raw offset แทน — **ต้องเสนอใหม่และรออนุมัติก่อน**
   (ตามขอบเขตบังคับของรอบนี้ + กติกา 8)

---

## 5) GO / NO-GO สำหรับเริ่ม T0 runtime

**GO ต้องครบทุกข้อ:**

1. static check ครบ: เป้า **CORE H1–H5** resolve ได้ครบด้วย `AccessTools` บน interop แบบ typed
   (OPTIONAL ตัวไหน resolve ไม่ได้ = ถอดออกได้ ไม่หยุด T0) — ไม่มีข้อใดต้องใช้ raw offset
2. audit โค้ด probe: log-only 100% (ไม่มี write เข้า state เกม, ไม่แก้ args/`__result` — **อ่าน
   `__result` อย่างเดียวใน postfix**, ไม่มี skip
   original), try/catch ครบทุก hook, **finalizer contract ฉบับ canonical (§2.5 rev 9) ครบ: lifecycle
   ทุก invocation + phase pop ครบทุก push + คืน exception เดิมตาม Harmony contract + event
   `factory.finalizer` (เชิงบวกทุก invocation) / `factory.failure` (conditional) แยกกัน + post-state
   อ่านเฉพาะใน `factory.result`**, **invocation linkage: กลไก `iid` (`__state` หรือ thread-local
   LIFO) พิสูจน์แล้วด้วย harness test ว่าแชร์ state/จับคู่ได้จริงกับ invocation ซ้อน**, pref default
   false,
   **patch-audit พร้อม: expected inventory ต่อ H3 overload ประกาศชัด (owner + patch method +
   จำนวน ครบทุก category ที่ HarmonyX 2.10.2 เปิดเผย — Prefixes/Postfixes/Transpilers/Finalizers/
   ILManipulators; Transpiler/ILManipulator = 0 ยกเว้น probe ประกาศ) + event `patch.audit`
   บันทึก**ครบ 4 จุดตรวจ** (`session-begin` + `sng-prefix` + `genhero-prefix` ก่อน W2 +
   `genenemy-prefix` ก่อน W3 — ทำใน H1/H2/H4 ที่ติดอยู่ ไม่เพิ่ม hook) และตรวจความเสถียร
   **ระหว่างจุดตรวจ** + ขอบเขตรู้ว่ามองเห็นเฉพาะ Harmony-registered patch ของ runtime นี้
   และเป็นหลักฐานณ ช่วงตรวจ + **ประกาศชัดว่า probe build นี้ไม่มีกลไกพิสูจน์ `inventoryStability`
   ภายใน window — E1/E2 คาดหมายคง UNKNOWN จนกว่าจะมีกลไกที่ผ่านอนุมัติ (§2.5 rev 13)**,
   Uninstall พิสูจน์ได้ (patch count คืน 0)
3. **preflight §2.6 ขั้น A (read-only) PASS ครบ** + hash เกมตรง VERSION.md + **ขั้น B (save-split)
  ได้รับอนุมัติแยก มี hash ก่อน/หลังและแผนคืนสภาพบันทึกไว้** (ลำดับ A → [อนุมัติ split] → B →
  probe → เปิดเกม)
4. ผู้ใช้อนุมัติข้อ 1–8 ของ §4 ครบ

**เงื่อนไขความถูกต้องของรอบ (ตรวจหลังรัน — ก่อนตีความสมการบัญชี):**
`phaseStuck == 0` **ตามนิยาม rev 4 (nesting ผิดรูปจริงเท่านั้น — เหตุการณ์ `post` ที่ถูกต้องเช่น
`genenemy` ไร้ `sng` parent ไม่นับ)** และ stack ว่างที่ `session.end` · ทุก Δ-count ใช้ list instance
เดียวกัน (`wid`/`hidH`/`hidT` ตรง) หรือมี `rebaseline` marker ที่อธิบายครบ · `Σ hist == H` ทุก
snapshot ที่
ใช้ · **บัญชี creation ปิดได้เฉพาะเมื่อทุก factory call ใน bucket `creation` อยู่ใน W2/W3 —
`sng.unattributed != 0` ⇒ บัญชียังไม่ปิด: ต้องระบุ caller หรือคง UNKNOWN (ห้ามสรุป path ที่สอง
ทันที — ลำดับตรวจตาม E6)** · เหตุการณ์ bucket `post` รายงานแยก ไม่เข้า E1–E7 **และเหตุการณ์
`post` ที่ถูกต้องไม่ทำให้ creation round ตกเงื่อนไขความถูกต้องย้อนหลัง** ·
**ทุก `#C.ok` ที่ใช้ใน E1/E2 ต้องผูกกับ invocation identity ที่ยืนยันได้ (`iid` จากกลไก typed
ที่พิสูจน์แล้ว) และมี `factory.finalizer` ของ invocation เดียวกันรายงาน
`exceptionAtProbeFinalizer == null` + patch-audit ผ่านโดย `inventoryAtWindowEntry`**ครบทุก
category ที่ API เปิดเผย (HarmonyX 2.10.2) ตรง expected inventory เป๊ะ (owner + patch method +
จำนวน) ณ จุดตรวจก่อนเข้า window ที่เกี่ยวข้อง — 4 จุด: `session-begin`/`sng-prefix`/
`genhero-prefix` (W2)/`genenemy-prefix` (W3)** **และ** `inventoryStability` ของ window นั้น
พิสูจน์ได้ — **รอบนี้ไม่มีกลไกพิสูจน์ stability ภายใน window ⇒ E1/E2 ที่พึ่ง pipeline-success
คง UNKNOWN** — เชื่อมโยง/พิสูจน์ไม่ได้ หรือพบ patch
อื่น/inventory ไม่ตรง/เปลี่ยนระหว่างจุด/ยืนยันขอบเขต runtime ไม่ได้ ⇒ E1/E2 คง UNKNOWN เช่นกัน
ห้ามใช้ `factory.result` เดี่ยว ห้ามอนุมาน
"pipeline ทั้งหมดสะอาด" จาก `exceptionAtProbeFinalizer` เฉย ๆ หรือใช้ timestamp แทน (rev 8–13) ·
สมการที่พึ่งข้อสรุป pipeline-clean สรุปได้สูงสุดแค่ **"ไม่มี exception ที่สังเกตได้ในขอบเขตของ
patching pipeline ที่ตรวจสอบแล้ว"** · สมการใดไม่เข้าเงื่อนไขนี้ = รอบนั้นใช้ตัดสินไม่ได้ (รายงาน +
รันใหม่) — ข้อมูล T0 ส่วนอื่นที่ไม่พึ่งข้อสรุปนี้ใช้ต่อได้ตามปกติ

**NO-GO (อย่างน้อยหนึ่งข้อ):**

1. เป้า CORE ใด resolve ไม่ได้ด้วย typed interop และทางแก้เดียวคือ raw memory
2. probe ต้องแก้ state เกม / ปฏิเสธ factory call เพื่อให้ได้ข้อมูล
3. hash เกมไม่ตรง VERSION.md (เกมอัปเดต — หยุดรายงานก่อน) หรือ preflight §2.6 FAIL
4. ไม่มีเงื่อนไขเซฟที่ปลอดภัย (เช่นต้องเปิดบนเซฟจริง)
5. hook ติดตั้งไม่ครบขั้นต่ำ (CORE H1–H5) — ถอนทั้งชุดแล้วรายงาน ห้ามรันรอบเสี่ยงขาดชิ้น
6. **finalizer ของ probe รับประกันการคืน exception เดิมของเกมตาม Harmony contract ไม่ได้** (เช่น
   ต้องกลืน/แก้ exception ของ original จึงจะทำงานได้ — §2.5 rev 5) — ห้ามรัน runtime

**ผลลัพธ์ที่คาดหวังหลัง GO:** ไฟล์ `sp5t0.ndjson` + summary ของ 1 creation round ที่ตอบ
§2.3 ครบทุกสมการ (หรือคง UNKNOWN พร้อมเหตุผล) — แล้วจึงกลับไปตัดสิน GO/NO-GO ของการ
implement การลด NPC ตาม `SP5design-20261027` §6

---

**หยุดหลังส่งแผน — ห้ามติดตั้งหรือรัน hook จนกว่าจะได้รับอนุมัติแยกต่างหาก**
