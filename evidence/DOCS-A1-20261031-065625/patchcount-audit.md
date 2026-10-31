# patch-count audit — นิยาม `CountOwnedPatches` vs ค่าคาดของทุก probe (งาน 3 · 2026-10-31 · offline · อ่านอย่างเดียว)

## วินิจฉัยหลัก (ยืนยันจากโค้ด — ไม่มีการรัน/แก้ source)

**`CompatInventory.CountOwnedPatches(owner)` (src/LongYinCoop/CompatInventory.cs:78–95) นับ "จำนวน METHOD ที่มี patch ของ owner" — ไม่ใช่จำนวน patch objects:**
- วน `Harmony.GetAllPatchedMethods()` (yield method ละ **1 ครั้ง** ไม่ว่า method นั้นจะมีกี่ patch) → `GetPatchInfo(m).Owners.Contains(owner)` → นับ method
- ⇒ เป้าหมาย 1 method ที่มี prefix+postfix+finalizer = นับได้ **1** ไม่ใช่ 3

**TimeV1 — ตรงกับอาการ "2 (คาด 6)" เป๊ะ (src/LongYinCoop/TimeV1/TimeV1Probe.cs:117–137):**
- patch **2 เป้า**: `WorkingUIController.StopWorking(bool)` (dump.cs:392739, RVA 0x9CB220) + `PlotController.MiddleStopWork()` (dump.cs:384077)
- แต่ละเป้า 3 patch-methods (prefix/postfix/finalizer) = **`ExpectedPatchCount = 6` (นับ patch-methods)**
- หลังติด เรียก `CountOwnedPatches` ⇒ ได้ **2** (จำนวน method) → `2 != 6` → **ถอนทั้งชุด**
- ⇒ **วินิจฉัยใหม่ [INFERRED-from-code]: patch น่าจะติดครบทั้ง 2 เป้า (count=2 = ครบทุกเป้า) — probe ถอนเพราะตัวนับนับ method ขณะที่ค่าคาดนับ patch-methods** — สอดคล้อง log 100%: `patch count หลังติด = 2 (คาด 6)` ซ้ำ **283 ครั้ง** (05:41:49–05:46:35 ≈ 1 ครั้ง/วิ — ตรง throttle 1 s ของ `Apply`), `[ERROR]=0`, writer ถูกปิดทันทีรายรอบ (`TC1-r1.ndjson` = 0 byte)
- ตอบ UNKNOWN เดิมของ V1-close-report: **"2 patch ที่ติดคือตัวไหน" = `StopWorking(bool)` + `MiddleStopWork` (ทั้งคู่มี patch ของ owner)** · **"ทำไม retry 283 ครั้งไม่หยุด" = throttle 1 วิ/attempt วนจนสิ้นหน้าต่าง** (fail-closed ทำงานถูกต้อง — แค่เงื่อนไขผ่านไม่ได้)

## ตารางราย probe

| probe | ไฟล์:บรรทัด | ค่าคาด | นิยามที่ใช้ตรวจ | โครงจริงของ patch | สอดคล้อง? |
|---|---|---|---|---|---|
| **TimeV1** | TimeV1Probe.cs:131 | **6** (patch-methods = 2 เป้า × 3) | `CountOwnedPatches` = **method count** | 2 เป้า × 3 = 6 patch-methods | **ไม่สอดคล้อง — false-fail** (ผลจริง TC1-r1: ได้ 2) |
| **Sp5T0** | Sp5T0Probe.cs:279 | **7** | `ownedAfter` (method count, absolute) | **7 เป้า** (7 `Patch()` calls, ยืนยัน `grep -c "_harmony.Patch(" = 7`) × 3 = 21 patch-methods | **ผ่านโดยบังเอิญนิยาม** — คาด 7 = จำนวนเป้า ตรงกับ counter (ผลจริงรอบ 2026-10-30: "ติด 7") ⇒ **ความเสี่ยงเดียวกัน: counter จับ "ติดครึ่ง" (patch บางส่วน) ไม่ได้ — ข้อเปิดตาม prompt ห้ามถือว่า SP5-T0 พร้อมรันในความหมายที่ counter ถูกเชิงความหมาย** |
| **Sp5Npc (gate)** | Sp5NpcGate.cs:114 | **4** (delta) | `(ownedAfter − ownedBefore)` = method ใหม่ | 4 เป้า (GenerateHero = prefix+finalizer, GetRecommend/GetMax = postfix, WorldAdd = prefix) = 5 patch-methods บน 4 เป้า | ผ่านด้วยนิยาม method-count — ความเสี่ยงเดียวกัน |
| **TimeV2a** | TimeV2aSampler.cs | — | **ไม่มี Harmony patch เลย** (sampler อ่าน state ผ่าน typed interop) | — | ไม่มีความเสี่ยงนี้ (อธิบายได้ว่าทำไม v2a เก็บ event ได้ทั้งที่ TimeV1 ถอน) |
| **JitProbeW6/W7/Waves** | S1.5c/*.cs:136/:124/:187 | — | `CountOwnedPatches` ใช้**ลง log** ก่อน/หลัง `UnpatchSelf` — **ไม่เป็น gate** | — | log-only ⇒ ไม่มี false-fail |
| **HarmonyProbe** | HarmonyProbe.cs:68–71 | — | before/after (log) | — | log-only |

## harness จับความหมายหรือไม่

- `tests/S2Protocol.Tests/TimeV1PurityHarness.cs:137–139`: `Check("ExpectedPatchCount = 6 (prefix/postfix/finalizer × 2 เป้า)", expected == 6)` — **เทียบ const กับค่าคาดเดียวกันทั้งคู่ (6 vs 6)** — **ไม่ได้ทดสอบความหมายของ `CountOwnedPatches`** (Harmony runtime API — mock ตรง ๆ ใน unit test ไม่ได้) ⇒ harness มองข้ามบั๊กนิยามนี้ทั้งชุด
- ไม่พบ test ใดที่ assert ว่าตัวนับนับ patch-entries (grep `CountOwnedPatches|PatchCount` ใน tests/ = แค่ harness ข้างบน)

## ข้อเสนอแก้ + harness (เสนอเท่านั้น — ต้องอนุมัติแยก · ห้ามแก้ source ใน session นี้)

1. เพิ่ม `CountOwnedPatchEntries(owner)` ใน CompatInventory: วน method → `GetPatchInfo(m)` → **รวมจำนวน patches ที่ owner เป็นเจ้าของ** (Prefixes+Postfixes+Finalizers ที่ `p.Owner == owner`) ⇒ ตรงนิยาม "จำนวน patch methods"
2. TimeV1 เปลี่ยนมาเทียบตัวนับใหม่กับ 6 (คง `ExpectedPatchCount` เดิม) — ทางเลือก (ไม่แนะนำ): คงตัวนับเดิมแต่เปลี่ยนค่าคาดเป็น 2 + เปลี่ยนชื่อความหมายเป็น "จำนวนเป้า" — ตัวนับเดิมจับ "ติดครึ่ง" (patch บางตัวล้มเงียบ) ไม่ได้
3. ปรับ gate ของ **Sp5T0 (7→21 patch-entries) / Sp5Npc (4→5)** ให้ใช้ตัวนับ patch-entries — **ต้องแก้ก่อนรัน SP5-T0/SP5-NPC รอบถัดไป** (เงื่อนไขหยุด G.1 จะ false-fail/หรือจับ partial-install ไม่ได้)
4. harness: เพิ่ม static check (แบบ `s2_static_check.py`) — assert ทุกจุด gate ใช้ตัวนับ patch-entries และค่าคาดตรงจำนวน patch-methods จากโค้ดจริง (ป้องกัน drift ครั้งถัดไป)
5. ผลต่อสาย V1: ถ้าอนุมัติแก้แล้ว build ใหม่ + session `เริ่ม V1 session` รอบใหม่ = TC-1..TC-4 กลับมาวัดได้ (ข้อมูล TC1-r1 รอบนี้ = NOT-RUN ตาม V1-close-report)
