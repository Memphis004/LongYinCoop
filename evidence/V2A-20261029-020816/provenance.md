# provenance — V2-A artifact (สถานะ: **READY — build ผ่าน 0W/0E · artifact ครบ · metadata 12/12 · C4 ผ่าน**)

> session `AUTO-20261029-013511` (2026-10-29) · งาน "เตรียม V2-A ให้พร้อมรัน" — **compile-only** ตามมติ [V2A-5]
> · **การ deploy จริงทำเฉพาะใน "เริ่ม V2-A session"** (ต้องสำรอง `.prev-6a94abb5` ตอนนั้น — ดู RUNBOOK-V2A.md)
> · **ไฟล์นี้ไม่ใช่การอนุมัติเปิดเกม/deploy** — วลีสั่งงานต้องมีมติ V2A-1..3 + D9 ครบ (APPROVAL-REQUESTS แพ็กเกจ 2)

## ผล build (C1/C2 — ตรวจจาก committed tree, 0 untracked-modified ณ ตอน build)

- **คำสั่ง:** `dotnet build src/LongYinCoop -c Release --no-incremental` (จาก mod repo)
- **source commit:** `211ea21d27d3b56d95829052dcb23004d8196afe` (HEAD — `src/` สะอาด, `git status` tracked-modified = 0)
- **dotnet SDK:** 9.0.311
- **ผล: Build succeeded — 0 Warning(s) / 0 Error(s), exit 0**

## ตาราง artifact (คัดลอกแล้ววัดซ้ำตรงต้นทาง — อยู่ในโฟลเดอร์นี้, ไม่ commit, **ไม่อยู่ใน Mods\**)

| ไฟล์ | sha256 | ขนาด (bytes) |
|---|---|---|
| `LongYinCoop.dll` | `983b238432a466535e6f3bfad1408f4ee0a657f910f2169af84f0d2f9d16893b` | 366,080 |
| `LongYinCoop.pdb` | `951de8fc1385a285cb1a02174639e92b1a6a3719bf6028e2adcfdbe52afee7d6` | 99,568 |

- ต้นทาง: `src/LongYinCoop/bin/Release/` — สำเนาในโฟลเดอร์นี้ sha256 ตรงต้นทางเป๊ะ (วัดซ้ำหลัง copy)
- `.dll` ถูก .gitignore กัน · `.pdb` untracked และ**ไม่ถูก add** ใน commit ใด
- เทียบ build ก่อนหน้า (S2a-4 GO-3): DLL `6a94abb5…` 359,936 B → ตัวนี้โตขึ้น 6,144 B = โค้ด TimeV2a ใหม่ (สมเหตุสมผล)

## เทสต์ก่อน build (exit code จริงทั้งหมด — บทเรียน `16a38c3`)

| ชุด | คำสั่ง | ผล | exit |
|---|---|---|---|
| C# suite | `dotnet run --project tests/S2Protocol.Tests -c Release` | **633 PASS / 0 FAIL** (596 เดิม + 37 ใหม่ TimeV2a) | 0 |
| python negative | `python -m unittest test_negative_preconditions` (ใน tools/) | Ran 53 tests — OK | 0 |
| python ทั้งชุด | `python -m unittest discover -p "test_*.py"` (ใน tools/) | Ran 68 tests — OK (รวม test_time_v2a_analyze 9/9) | 0 |
| static check | `python tools/s2_static_check.py` | CLEAN — 0 violations | 0 |
| B4 analyzer cross-check | `dotnet run … -- --emit-v2a-fixture` + `python tools/time_v2a_analyze.py <fixture> --tolerance 0.1` | **FORM2**, day_ticks=5==N, sessions=1 decided=1 — analyzer อ่าน NDJSON ของ detector จริงได้ **โดยไม่แก้ analyzer** | 0 |

หลักฐาน B4: `evidence/AUTO-20261029-013511/b4-analyzer-crosscheck.md` (+ `b4-analyze-result.txt` · fixture
`v2a-fixture.ndjson` อยู่บนดิสก์เท่านั้นตาม `.gitignore:60`)

## C3 — ตรวจ metadata ดิบ (#Strings heap — อ่านไบต์ล้วน **ไม่โหลดแอสเซมบลี**)

สคริปต์ `evidence/AUTO-20261029-013511/c3_strings_heap_check.py` (วิธีเดียวกับ B3 ของ GO-3 — ไฟล์เดิมไม่แก้)
ผล (`c3-strings-result.txt`, exit 0): **ครบ 12 ชนิด = FOUND ทั้งหมด**

- V2-A ใหม่ 6: `TimeV2aObs` · `TimeV2aEvent` · `TimeV2aDetector` · `TimeV2aGate` · `TimeV2aNdjsonWriter` · `TimeV2aSampler`
- S2a-4 เดิม 6 (ยังอยู่ครบ): `S2GuestDecoder` · `S2OverlayClient` · `S2OverlayRenderModel` · `S2OverlayService` · `S2SetPrefGate` · `S2WriteGuardHook`

## C4 — ณ pref default (`TimeV2a=false`) sampler ทำงานเป็น no-op สมบูรณ์ (หลักฐานเชิงโค้ด ระบุบรรทัด)

| ข้อพิสูจน์ | หลักฐาน |
|---|---|
| pref default = false | `src/LongYinCoop/CoopPrefs.cs:61` — `TimeV2a = Category.CreateEntry("TimeV2a", false);` |
| ประตูที่ 1 (mod) | `src/LongYinCoop/CoopMod.cs:142-148` — `Tick()` เรียก `TimeV2aSampler.Tick()` เฉพาะเมื่อ pref true; ถ้า pref false และ writer ยังเปิด → Stop() (ปิดไฟล์) |
| ประตูที่ 2 (sampler) | `src/LongYinCoop/TimeV2a/TimeV2aSampler.cs:45` — `if (!CoopPrefs.TimeV2a.Value) return;` (กันเรียกผิดฝั่ง) |
| ไม่มี Harmony instance/patch/hook | grep `Harmony|HarmonyLib|Patch|PatchAll` ใน `TimeV2a/**` = **1 hit เดียว = comment** (`TimeV2aSampler.cs:15` ประโยค "ไม่มี Harmony instance/patch/hook ใด") — 0 ในโค้ด · **ไม่มี W4(a) ไม่มี hook ใหม่ถูกสร้าง** (คงมติ M5-3) |
| ไม่เปิดเธรด/timer/process | grep `new Thread|new Timer|Process.Start|Dns` ใน `TimeV2a/**` = **0 hits** (exit 1) |
| ไม่มี write path เข้า state เกม | grep กำหนดค่า field เกม (`\.working\s*=` ฯลฯ 11 field ตามสเปก) ใน `TimeV2a/**` = **0 hits** · โค้ดอ่านอย่างเดียวทั้งหมด (typed property getters) |
| ไฟล์ I/O ทั้งหมดของ TimeV2a | (1) อ่าน pointer file `UserData\LongYinCoop\time-v2a.out` — `TimeV2aSampler.cs:118,123` (File.Exists/ReadAllLines, โพล ≤1 Hz เมื่อ pref on) (2) เขียน NDJSON ตาม path ที่ pointer ชี้ — `TimeV2aRules.cs:261` (StreamWriter append, flush ทุก 20 บรรทัด) · **fail-closed**: ไม่มี pointer/ว่าง/นามสกุล ≠ .ndjson = ไม่เขียนเลย (`TimeV2aGate` — เทสต์ 6 ข้อ) |
| SP5 ไม่ถูกแตะ | `git diff 636c209 HEAD -- src/LongYinCoop/SP5` = ว่าง · `Sp5T0` default คง false (`CoopPrefs.cs:56`) |
| สิ่งที่เปลี่ยนนับแต่ build source เดิม `636c209` | เฉพาะ: `TimeV2a/{TimeV2aRules,TimeV2aSampler}.cs` (ใหม่) · `CoopPrefs.cs`/`CoopMod.cs` (wiring 3 จุด) · tests (3 ไฟล์) · docs/evidence — **S2/**, SP5/, tools/, protocol ไม่ถูกแตะ** |

## ข้อจำกัดที่ประกาศไว้ล่วงหน้า (errata N4 — อ่านก่อนวัด)

1. analyzer อ่าน k จาก `work_start` เท่านั้น ⇒ **รอบ skip (กด `>>`) เป็นข้อมูลประกอบ ไม่ใช่คำตัดสิน FORM1/FORM2** — k จริงอ่านจากฟิลด์ `skipK` บน `day_tick` (ที่ sampler วัดเอง)
2. tolerance ดีฟอลต์ 0.5 s ทำให้ N=10 ปกติ (gap 0.5) = **INDISTINGUISHABLE ตั้งแต่โครง** — ค่า tolerance ต้องประกาศก่อนวัด (RUNBOOK-V2A เสนอ 0.1 s — ผู้ใช้ยืนยันตอนสั่ง "เริ่ม V2-A session")
3. `hour` = float (dump.cs 0xB4) — NDJSON พก float; ถ้าอ่านไม่ได้เฟรมใด = `null` (analyzer ไม่ใช้)
4. session ที่เกมปิดกลางงาน = unterminated ใน stream ⇒ analyzer SystemExit ทั้งไฟล์ (ออกแบบไว้ปลอดภัย — วิเคราะห์เฉพาะ stream ที่ปิดครบ)
