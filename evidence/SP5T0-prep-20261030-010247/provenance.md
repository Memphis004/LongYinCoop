# provenance — SP5-T0 prep artifact (สถานะ: **READY — build ผ่าน 0W/0E · artifact ครบ · ยังไม่ deploy**)

> สร้าง 2026-10-30 โดย session เตรียม SP5-T0 (โหมด offline) — มติ SP5-T0 §4 **ข้อ 1 เท่านั้น** (อนุมัติแผน rev 13) ·
> ข้อ 2–8 ยังไม่อนุมัติ ⇒ **deploy / save-split / เปิดเกม ห้ามจนกว่าวลีสั่งแยกในเซสชันเกมแยก**

## งานที่ทำ (อ้างแผน `plans/sp5-t0-plan.md` rev 13)

- **gap ที่พบจากการตรวจ §5 ข้อ 2:** CORE H1–H5 + patch.audit 4 จุด + finalizer contract + pref default false =
  **ครบใน build เดิม `6a94abb5…`** — ขาดอย่างเดียวคือ **harness test ของ invocation linkage (`iid`)** ตาม §2.5
  rev 8–9 + §5 ข้อ 2 (ไม่มี test ของ SP5 ใน `tests/` เลย; probe เขียนเองว่า "iid UNPROVEN รอ harness test ตอน build")
- **เขียน `tests/S2Protocol.Tests/Sp5T0LinkageHarness.cs`** — reflection บน artifact จริง (โหลด `LongYinCoop.dll`
  ที่ build แล้ว — ไม่ re-implement กลไก, **ไม่แตะตรรกะ probe**): 17 checks = LIFO nesting (2 + 4 ระดับ, identity
  ตรงตัวทุกชั้น) / exception-path (finalizer pop โดยไม่มี postfix) / cross-thread isolation ([ThreadStatic] จริง) /
  iid โมโนโทนิก · **ขอบเขตประกาศในหัวไฟล์:** mismatch-branch ของ `PopInv` (เรียก PhaseStuckMark → UnityEngine)
  **ไม่ถูกกระตุ้น** — ครอบด้วย code audit (GO-1) + `s2_static_check` · ordering จริงของ Harmony บนเป้าเกมยังไม่รัน
- **ป้ายใน probe 4 จุด** เปลี่ยน "iid UNPROVEN / รอ harness test" → "mechanism harness-proven offline
  (Sp5T0LinkageHarness)" (header + InvRec comment + `patch.audit` linkage field + `summary.json`) — แก้เฉพาะ
  string/คอมเมนต์ **ไม่แตะตรรกะใด** — บังคับโดยวินัยหลักฐานเมื่อ harness ผ่าน (แผน rev 8–9: พิสูจน์แล้วจึงระบุเป็น
  ข้อเท็จจริงได้) · **`#C.ok`/E1/E2 ยังคง UNKNOWN ตาม rev 13** (inventoryStability ภายใน window ยังไม่มีกลไก —
  ป้าย `cOk_E1_E2` ใน summary ไม่แตะ)
- commit โค้ด = **`ab9d07b`** (3 ไฟล์: `src/LongYinCoop/SP5/Sp5T0Probe.cs` +19/−8, `tests/.../Sp5T0LinkageHarness.cs`
  +199 ใหม่, `tests/.../Program.cs` +1)

## ผล build

- **คำสั่ง:** `dotnet build src/LongYinCoop -c Release --no-incremental` (ณ HEAD `ab9d07b`) ·
  **ผล: Build succeeded — 0 Warning(s) / 0 Error(s), exit 0** (dotnet SDK 9.0.311)
- **determinism:** rebuild ซ้ำ 2 ครั้งที่ HEAD เดียวกัน = hash ตรงกันทุก byte (`865b4a87…`)
- **หมายเหตุ UNKNOWN:** build ก่อน commit (`33c48c8c…`) มีซอร์สเนื้อหาเดียวกันแต่ HEAD ยังเป็น `2f280ae`
  (ไฟล์แก้ยัง uncommitted) แล้วให้ hash **ต่าง**จาก build post-commit — สาเหตุยังไม่ถอด (UNKNOWN; น่าจะมี
  metadata ฝังที่ขึ้นกับ git state) — **artifact ที่ใช้จริง = ตัว post-commit `865b4a87…` ที่ determinism ยืนยันแล้ว ×2**

## ตาราง artifact (สำเนาในโฟลเดอร์นี้ hash ตรงต้นทาง `src/LongYinCoop/bin/Release/` — **ไม่ deploy, ไม่อยู่ใน Mods\**)

| ไฟล์ | sha256 | ขนาด (bytes) |
|---|---|---|
| `LongYinCoop.dll` | `865b4a87b6ee4aa5b5ac2b20e63fb4537ab14c49210948875b93eed8a441a84a` | 366,592 |
| `LongYinCoop.pdb` | `60ad4546aa556ab2613525b2f193d9ed3b690e7163346541b72b14c958e51c0d` | 99,572 |

- เทียบ build ก่อนหน้า: SP5/G0-3 era **`6a94abb5…`** (359,936 B — **ยัง deploy ใน `Mods\` ตามมติ [G-3]/rollback ทาง ข — ไม่แตะ**) ·
  V2-A **`983b2384…`** (366,080 B — เก็บ `evidence/V2A-20261029-020816/`) — build นี้ต่างจากทั้งคู่ด้วยสองกลุ่ม:
  TimeV2a (จาก `d81c86d` — มากับ V2-A) + ป้าย linkage ใหม่ของ SP5 (`ab9d07b`)
- `.dll`/`.pdb` ถูก .gitignore (`evidence/**/*.dll`, `evidence/**/*.pdb`) — อยู่บนดิสก์ ไม่ commit

## ผลเทสต์ (exit code จริงทั้งหมด — ไฟล์เต็มในโฟลเดอร์นี้)

| ชุด | ผล | exit |
|---|---|---|
| C# suite (`dotnet run --project tests/S2Protocol.Tests`) | **650 PASS / 0 FAIL** — รวม Sp5T0LinkageHarness **17 PASS / 0 FAIL** (log พิมพ์ sha256 ของ artifact ที่โหลด = `865b4a87…` ตรงตารางบน) | 0 |
| python `test_negative_preconditions` | Ran 53, OK | 0 |
| python `unittest discover` | Ran 106, OK | 0 |
| `s2_static_check.py` | CLEAN — 0 violations | 0 |

## ตรวจ metadata ดิบ (B3 — อ่านไบต์ #Strings heap, **ไม่โหลดแอสเซมบลี**)

`b3_strings_heap_check.py` (ต่อยอดสคริปต์ GO-3 — เพิ่มรายการ SP5/TimeV2a) → `b3-strings-result.txt` (exit 0):
`Sp5T0Probe` · `TimeV2aSampler` · `TimeV2aDetector` · `TimeV2aGate` · `TimeV2aNdjsonWriter` · `TimeV2aObs` ·
`TimeV2aEvent` + S2a-4 ทั้ง 6 (`S2GuestDecoder`/`S2OverlayClient`/`S2OverlayRenderModel`/`S2OverlayService`/
`S2SetPrefGate`/`S2WriteGuardHook`) = **FOUND ครบทุกชนิด**

## ข้อจำกัด / สิ่งที่ยังรอ

- artifact นี้**ยังไม่ได้ deploy** — `Mods\LongYinCoop.dll` ยังเป็น `6a94abb5…` (ห้ามแตะใน session นี้)
- deploy + split + เปิดเกม = §4 ข้อ 2–3 (**ยังไม่อนุมัติ**) — ต้องมีวลีสั่งแยกในเซสชันเกมแยก
- GO ที่เหลือตาม §5: ข้อ 2 ส่วน runtime (patch.audit จริง ณ 4 จุดตรวจ + ordering ของ Harmony — ทำตอนรันจริง) ·
  ข้อ 3 (preflight ขั้น A PASS + split อนุมัติแยก) · ข้อ 4 (ผู้ใช้อนุมัติ §4 ข้อ 1–8 ครบ — ปัจจุบันอนุมัติเฉพาะข้อ 1)
- mismatch-branch ของ `PopInv` ไม่ถูก harness กระตุ้น (Unity-bound) — ครอบด้วย code audit GO-1 + static check;
  ถ้าต้องการปิด = ออกแบบ harness เพิ่ม (อนุมัติแยก)
- VERSION.md ยังไม่แตะ — จะอัปเดต "Mod build ที่ deploy ล่าสุด" เฉพาะตอน deploy จริง (§4 ข้อ 2)
