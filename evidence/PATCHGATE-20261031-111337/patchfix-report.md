# patchfix-report — แก้ patch-count gate (งาน 2 รอบ 6 · มติ R6-6)

- **วันที่:** 2026-10-31 · **ประเภท:** offline · compile-only — **ไม่ deploy ไม่เปิดเกม ไม่แตะ Mods\/net.cfg/pref/เซฟ**
- **ต้นทาง:** `evidence/DOCS-A1-20261031-065625/patchcount-audit.md` (วินิจฉัย `CountOwnedPatches` นับ METHOD ไม่ใช่ patch-entries ⇒ TimeV1 false-fail 2/6 ของ TC1-r1 · Sp5T0 ผ่านโดยบังเอิญนิยาม · Sp5Npc delta จับ partial-install ไม่ได้)
- **ขอบเขตไฟล์ที่แก้ (ตาม prompt รอบ 6 เท่านั้น):** `src/LongYinCoop/CompatInventory.cs` · `TimeV1/TimeV1Probe.cs` · `SP5/Sp5T0Probe.cs` · `SP5/Sp5NpcGate.cs` · `tests/S2Protocol.Tests/{PatchGateHarness.cs(ใหม่), Program.cs, TimeV1PurityHarness.cs(คอมเมนต์)}` — **ไม่แตะไฟล์ src/ อื่น · ไม่เพิ่ม dependency ใหม่ · ไม่เปลี่ยนรายชื่อเป้าหมาย hook**

## 1) สิ่งที่แก้

| ไฟล์ | การแก้ |
|---|---|
| `CompatInventory.cs` | (ก) คอมเมนต์นิยามบน `CountOwnedPatches` เดิม (method-count — เหมาะเฉพาะเช็ค `== 0`, ห้ามใช้ตรวจติดครบชุด) คงฟังก์ชันไว้ไม่ลบ (ข) เพิ่ม `CountOwnedPatchEntries(owner)` — นับ patch **entries** (prefix/postfix/finalizer/transpiler ที่ `p.owner == owner` — 0Harmony 2.10.2 เปิดเจ้าของเป็น field `owner` ตัวเล็ก) (ค) เพิ่ม `CollectOwnedPatchSet` + pure trio `DescribeKey` / `BuildExpectedSet` / `ComparePatchSets` (signature BCL เท่านั้น — เทสต์ผ่าน reflection ได้โดยไม่ต้องโหลด 0Harmony) + `OwnedPatchTargetsMatch(owner, targets[], entries[], out report)` — เทียบชุด method จริงกับคาด + จำนวน entries ต่อ method, ไม่ตรง → report ราย "คาด/จริง + ขาด/เกิน/จำนวนไม่ตรง" (รายชื่อทั้งสองฝั่ง) แล้วผู้เรียก fail-closed ถอนทั้งชุดเหมือนเดิม |
| `TimeV1Probe.cs` | gate หลังติดเปลี่ยนจาก `ownedAfter != ExpectedPatchCount` (method-count vs 6) → `OwnedPatchTargetsMatch(HarmonyId, {stop, msw}, {3, 3})` · fail → log "patch set หลังติดไม่ตรง (คาด 6 patch-methods บน 2 เป้า)" + report เต็ม → `Uninstall()` → `return false` เหมือนเดิม · log ติดตั้งสำเร็จเปลี่ยนเป็น `patchMethods=<n> patchEntries=<6>` (เดิม `patches=<n>` — ตรวจแล้ว python analyzer ไม่ parse สตริงนี้) · เป้า/เงื่อนไขก่อนติด (`ownedBefore != 0`) และเช็คหลังถอน (`owned != 0`) **คงเดิม** (เช็ค zero เหมาะกับ method-count) |
| `Sp5T0Probe.cs` | gate เดิม `ownedAfter != 7` (ผ่านโดยบังเอิญนิยาม) → `OwnedPatchTargetsMatch(HarmonyId, {7 เป้า}, {3×7})` — คาดรวม **21 patch-methods บน 7 เป้า** · รวมความจริงที่ GenerateHeroData มี 4 overload (DescribeKey รวม signature จึงไม่ชนกัน) · log สำเร็จเป็น `patchMethods= patchEntries= (before=)` · เช็คหลังถอนคงเดิม |
| `Sp5NpcGate.cs` | gate เดิม delta `ownedAfter != ownedBefore + 4` (method-count) → `OwnedPatchTargetsMatch(HarmonyId, {GenerateHero, GetRecommend, GetMax, WorldAdd}, {2, 1, 1, 1})` — คาดรวม **5 patch-methods บน 4 เป้า** · ลบ local `ownedBefore` ที่ตาย (อ่าน counter ไม่มีผลข้างเคียง — พฤติกรรมไม่เปลี่ยน) · patch หลงเหลือจากรอบก่อนจะโดนนับ "เกิน" → fail-closed เช่นกัน |
| `tests/**` | `PatchGateHarness.cs` ใหม่ (ดู §2) · `Program.cs` ลงทะเบียนรันท้ายสุด · `TimeV1PurityHarness.cs` แก้คอมเมนต์เอกสารรูปแบบ log (สตริงใน Check ทุกตัวคงเดิม) |

**ไม่แตะ:** JitProbeW6/W7/Waves + HarmonyProbe (log-only ไม่ใช่ gate — ตามตาราง audit) · TimeV2aSampler (ไม่มี Harmony) · รายชื่อเป้า hook ทุกตัว · พฤติกรรมอื่นของ probe

## 2) ผลทดสอบ (เลขจริง · exit code จริง)

| การทดสอบ | ผล | เทียบเดิม |
|---|---|---|
| `dotnet build src/LongYinCoop -c Release --no-incremental` | **0 Warning / 0 Error** | 0W/0E ✓ |
| `dotnet run --project tests/S2Protocol.Tests -c Release` (exit 0) | **808 PASS / 0 FAIL** (นับบรรทัด `[PASS]`/`[FAIL]` จาก stdout เต็ม) | เดิม 596 PASS / 0 FAIL (2026-10-28 GO-1) — **ผ่านมากกว่าเดิม** ✓ |
| └ `TimeV1PurityHarness` (เดิม) | 23 PASS / 0 FAIL (ค่าคาด `ExpectedPatchCount = 6` คงเดิม) | เท่าเดิม ✓ |
| └ `PatchGateHarness` (ใหม่) | **25 PASS / 0 FAIL** — static check ซอร์ส 9 ข้อ + semantic 0Harmony จริง 15 ข้อ + cleanup 1 | เพิ่มใหม่ ✓ |
| `cd tools; python -m unittest test_negative_preconditions` | **Ran 53 tests … OK** (exit 0) | เท่าเดิม 53 OK ✓ |
| `python -I tools/s2_static_check.py` | **STATIC CHECK CLEAN (0 violations)** (exit 0) | CLEAN ✓ |

หมายเหตุการวัด: `python -I -m unittest test_negative_preconditions` (จาก tools/) = `ModuleNotFoundError` — เพราะ `-I` ตัด CWD ออกจาก sys.path ไม่ใช่ปัญหาโค้ด; รันตาม convention เดิม (ไม่มี `-I`, จาก tools/) ผ่าน 53/53

### 2.1) semantic test จับ "นิยามการนับ" จริง (PatchGateHarness — 0Harmony 2.10.2 จาก `MelonLoader\net6` ผ่าน AssemblyResolve)

- **(A) method เดียวหลาย patch:** ติด prefix+postfix+finalizer บนเป้าเดียว → `CountOwnedPatches = 1` แต่ `CountOwnedPatchEntries = 3` — พิสูจน์ต่างนิยามจริง (กรณีที่ทำ TC1-r1 false-fail: ค่าคาด 6 patch-methods เทียบกับตัวนับ method 2 ไม่มีทางเท่ากัน)
- **(A) ติดครึ่งชุด (ขาด method):** คาด 2 เป้า ติดจริง 1 → `false` + report มี `ขาด:` + key เป้าที่ขาด
- **(B) ติดครึ่งชุด (entries ไม่ครบ):** `Unpatch(Postfix)` จริง → entries 2/3 → `false` + report `จำนวนไม่ตรง: … คาด 3 จริง 2` (method-count เดิมมองไม่เห็นกรณีนี้เลย — ยังนับ 1)
- **(C) เกินเป้า:** patch เป้านอกชุดคาด → `false` + report `เกิน:` + key · ครบชุด 2 เป้า → `true`
- **(D) `DescribeKey` แยก overload ชื่อเดียว** (กรณีจริง: `GenerateHeroData` ×4 ของ Sp5T0) + `ComparePatchSets` (pure) ชุด 2 key ตรง → `true`
- **(cleanup):** `UnpatchAll` → method-count = 0 + entries = 0 (ไม่ทิ้ง patch ค้างใน test process)

### 2.2) artifact/ไฟล์ (sha256)

- `src/LongYinCoop/bin/Release/LongYinCoop.dll` = `de0622abba33a96b78bf611ee490964bd9af8030251177aa377d77dd43f1b246` (build ที่ใช้ทดสอบ — **compile-only ไม่ deploy**)
- ซอร์สที่แก้: CompatInventory `1869772e…` · TimeV1Probe `ee99db1e…` · Sp5T0Probe `4c22fe6f…` · Sp5NpcGate `e59f49c6…` · PatchGateHarness `0c940551…`

## 3) ข้อจำกัด (ประกาศตามจริง)

1. เป้าของ semantic test = method ธรรมดาใน test assembly (Harmony runtime จริงจากโฟลเดอร์เกม ผ่าน AssemblyResolve — **ไม่เพิ่ม compile-time reference 0Harmony ให้ test project ตามข้อบังคับ "ห้ามเพิ่ม dependency"**) — **ไม่ใช่ interop เกม** ⇒ พฤติกรรม gate บนเกมจริงยังต้องยืนยันใน runtime session ของแต่ละสาย (ML log `patchMethods=/patchEntries=`)
2. `DescribeKey` ใช้ `ParamType.Name` (ไม่ใช่ FullName) — สำหรับเกมนี้ overload ใน class เดียวไม่มีชนิดพารามิเตอร์ชื่อซ้ำกัน; ถ้าอนาคตมี overload ที่ param เป็นคนละ type แต่ชื่อซ้ำ (คนละ namespace) จะต้องขยับไป FullName — บันทึกไว้ที่คอมเมนต์ method
3. **ห้ามถือว่า SP5-T0 พร้อมรัน** — gate ที่แก้ทำให้ตรวจ "ติดครบชุด" ได้ถูกเชิงความหมายแล้วเท่านั้น · §4 ข้อ 2–8 ของ sp5-t0-plan ยังไม่อนุมัติ
4. สาย V1 ยังปิดอยู่ (CLOSED-CANCELLED) — ถ้าจะกลับมารัน TC-1..TC-4 ด้วย gate ใหม่ = ต้องอนุมัติ session ใหม่แยก

## 4) ผลต่อ UNKNOWN ของ V1-close-report (ตอบที่ตอบได้)

- "2 patch ที่ติดคือตัวไหน" = `StopWorking(bool)` + `MiddleStopWork` (มี patch ของ owner ครบทั้งคู่) · "ทำไม retry 283 ครั้ง" = throttle 1 วิ/attempt วนจนหมดหน้าต่าง (fail-closed ทำงานถูกต้อง — เงื่อนไขผ่านไม่ได้เพราะนิยามตัวนับ) — ปิดด้วยหลักฐานโค้ด [INFERRED-from-code] เดิมของ patchcount-audit + ตอนนี้มี semantic test ยืนยันนิยามแล้ว · คง UNKNOWN: เหตุที่ probe ถอนเอง + anomaly mtime `.prev-…-050028` (นอกขอบเขตรอบนี้)
