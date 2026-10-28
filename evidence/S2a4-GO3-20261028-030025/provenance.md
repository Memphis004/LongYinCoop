# provenance — S2a-4 GO-3 artifact (สถานะ: **READY — build ผ่าน 0W/0E · artifact ครบ · 6 ชนิดครบ**)

> อัปเดต 2026-10-28 07:3x โดย session `AUTO-20261028-072525` (หลังมติ [G-1] รอบใหม่ของผู้ใช้) · ประวัติ BLOCKED ด้านล่างคงไว้ตามจริง

## ผล build ที่ผ่านแล้ว (2026-10-28 07:3x)

- **มติ [G-1] รอบใหม่ (ผู้ใช้ 2026-10-28):** ทาง (ก) เพิ่ม `using System.Diagnostics;` 1 บรรทัดใน `SP5/Sp5T0Probe.cs` — เงื่อนไขก่อนแก้ C1–C4 ตรวจครบ **ผ่านทั้ง 4** (อ่านอย่างเดียว — `evidence/AUTO-20261028-072525/c1-c4-precondition-checks.md`) · commit การแก้ = `636c209ecd61a572f25d40179b99de370e9892fb` (path เดียว, diff +1 บรรทัด, ข้อความระบุ "build-fix only")
- **คำสั่ง:** `dotnet build src/LongYinCoop -c Release --no-incremental` (จาก mod repo, ณ HEAD `636c209…` · `src/ tools/ tests/` ไม่มี uncommitted ก่อน build) · **ผล: Build succeeded — 0 Warning(s) / 0 Error(s), exit 0** (CS0246 ของ `91c276b` หมดไป)
- **dotnet SDK:** 9.0.311
- **เทสต์ก่อน build (exit code จริงทั้งหมด):** C# suite **596 PASS / 0 FAIL** (exit 0 — นับ `[PASS]` รายบรรทัด; กลุ่ม s2a3 179/0, s2a4 130/0) · python `test_negative_preconditions` **53 OK** (exit 0) · discover `test_*.py` **68 OK** (exit 0) · `s2_static_check.py` **CLEAN, 0 violations** (exit 0)

## ตาราง artifact (คัดลอกแล้ววัดซ้ำตรงต้นทาง — ไฟล์อยู่ในโฟลเดอร์นี้, ไม่ commit, **ไม่อยู่ใน Mods\**)

| ไฟล์ | sha256 | ขนาด (bytes) |
|---|---|---|
| `LongYinCoop.dll` | `6a94abb579e65d5e6b0af917dfed4ba6f5350ad76847190bd7263cda3077b334` | 359,936 |
| `LongYinCoop.pdb` | `856d3beda727218823170f861f532509c5458eb91e51c79f07c0452d3b196035` | 97,128 |

- ต้นทาง: `src/LongYinCoop/bin/Release/` — สำเนาในโฟลเดอร์นี้ sha256 ตรงต้นทางเป๊ะ (วัดซ้ำหลัง copy)
- `.dll` ถูก .gitignore กัน (`evidence/**/*.dll` — บรรทัด 63) · `.pdb` untracked และ**ไม่ถูก add** ใน commit ใด

## ตรวจ metadata ดิบ (B3 — อ่านไบต์ #Strings heap, **ไม่โหลดแอสเซมบลี**)

สคริปต์ `evidence/AUTO-20261028-072525/b3_strings_heap_check.py` (parse PE → CLR dir → BSJB → #Strings, หา exact null-terminated entry) · ผล (`b3-strings-result.txt`, exit 0): **ครบ 6 ชนิด = FOUND ทั้งหมด** — `S2GuestDecoder` · `S2OverlayClient` · `S2OverlayRenderModel` · `S2OverlayService` · `S2SetPrefGate` · `S2WriteGuardHook`

## ข้อความชัดเรื่อง SP5 ใน DLL (M5-3)

**DLL นี้มีโค้ด `SP5/Sp5T0Probe.cs` compiled อยู่จริง** (csproj หลัก SDK-style compile ทุก .cs) — แต่ตามเงื่อนไขมติ [G-1] รอบใหม่ + หลักฐาน C1–C4: ณ default (`Sp5T0=false` — `CoopPrefs.cs:56`, ไม่มี key ใน `MelonPreferences.cfg`) การเรียก `Apply(false)` ที่โหลดเป็น **no-op ล้วน: ไม่สร้าง Harmony instance, ไม่ติด patch, ไม่เขียน/เปิดไฟล์, ไม่เปิดเธรด/timer** (`new HarmonyLib.Harmony` บรรทัด 201 / `.Patch` 203–230 / `OpenWriter` 197 อยู่เฉพาะกิ่ง `wanted==true`) ⇒ **ไม่ขัด M5-3** (ไม่มี hook ถูกติดตั้ง) · การตรวจซ้ำตอนรันจริง = ขั้น 6 ของ RUNBOOK (ML log ต้องไม่มีบรรทัด SP5/Sp5T0 install/patch ใด — ถ้ามี = หยุด)

## ประวัติ (คงไว้ตามจริง)

- **B3/B4 ของ session `AUTO-20261028-023904` = NOT-RUN (build ล้ม)** — `dotnet build -c Release --no-incremental` ณ HEAD `91c276b9436fccdf498fc3b3eb08b62f10ec4406` (2026-10-28 02:5x): `SP5/Sp5T0Probe.cs(52,33) error CS0246 'Stopwatch'` — ต้นเหตุ = commit `7ff47ab` ถูก commit โดยยังไม่เคย compile (test csproj ไม่ลิงก์ SP5/** จึงเทสต์ยังผ่าน)
- **session `AUTO-20261028-070417`:** มติ [G-1] รอบแรก — เงื่อนไข tripwire ติด (probe ถูกเรียกจาก load path ตามตัวอักษร) ⇒ A3 หยุดรอมติ, build ไม่รัน · ผู้ใช้ตัดสินรอบใหม่ = ทาง (ก) พร้อมเงื่อนไข C1–C4 (ดูหัวข้อ build ที่ผ่านแล้ว)
- baseline เซฟที่เกี่ยวข้องกับ GO-3: ปัจจุบัน = `df8e5a11beed…` @ `coop-test/baselines/20261028-071340` (มติ [G-2] — ดู VERSION.md หัวข้อ "Baseline ที่อนุมัติปัจจุบัน")
