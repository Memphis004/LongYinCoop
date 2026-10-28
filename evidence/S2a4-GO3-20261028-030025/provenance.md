# provenance — S2a-4 GO-3 artifact (สถานะ: **BLOCKED — build ยังไม่ได้ · G-1 รอมติผู้ใช้**)

> อัปเดตสถานะ 2026-10-28 โดย session `AUTO-20261028-070417` (B5 ฉบับปรับตามผล — build ไม่ได้รันเพราะเงื่อนไข [G-1] ติด) · **ไม่มี DLL/PDB artifact ใดในโฟลเดอร์นี้** (ห้ามเขียนทับ/เติมทีหลังโดยไม่ผ่านขั้นตอนนี้ซ้ำทั้งหมด)

- **เหตุที่ยัง BLOCKED (อัปเดต 2026-10-28):** มติ [G-1] ทาง (ก) แก้ using 1 บรรทัด มี**เงื่อนไขก่อนแก้** = ตรวจ reachability ของ `Sp5T0Probe` — ผลตรวจ (อ่านอย่างเดียว): **เงื่อนไขติด** — `CoopMod.cs:53` เรียก `Sp5T0Probe.Apply(...)` จาก `OnInitializeMelon()` (load path) ตามตัวอักษรของเงื่อนไข *"ถ้า probe ถูกเรียก…จากเส้นทางที่รันตอนโหลด = หยุด"* ⇒ **การแก้ using ไม่ถูกทำ, build ไม่ถูกรันซ้ำ, ห้ามเปลี่ยนไปทาง (ข) เอง** — รอมติผู้ใช้ · ข้อเท็จจริงประกอบมติ (RUNTIME-พฤติกรรมจากซอร์ส): ณ pref default `Sp5T0=false`, `Apply(false)` → `Uninstall()` → `CloseSession` (return ทันที) = **ไม่สร้าง Harmony ไม่ติด patch ไม่เขียนไฟล์** · หลักฐานเต็ม `evidence/AUTO-20261028-070417/a1-a2-reachability.md`
- **สถานะ src/ ณ session `AUTO-20261028-070417`:** HEAD `ec78a8b` · `git diff --stat 91c276b ec78a8b -- src/` = **ว่าง** (ยืนยันแล้ว) ⇒ ผล build ล้มของ `91c276b` ยังคงอธิบายทรีปัจจุบันได้ (CS0246 ตัวเดิม deterministic) — จึงไม่รัน build ซ้ำโดยรู้ผลล่วงหน้า
- **(ปิดแล้ว แยกสาย) baseline เซฟ:** มติ [G-2] 2026-10-28 — rebaseline volatile-only แล้ว ⇒ baseline ปัจจุบัน `df8e5a11beed…` @ `coop-test/baselines/20261028-071340` (ดู RUNBOOK ขั้น 4 · VERSION.md หัวข้อ "Baseline ที่อนุมัติปัจจุบัน")
- **คำสั่งที่รันครั้งล่าสุด (ตาม [A5], session `AUTO-20261028-023904`):** `dotnet build src/LongYinCoop -c Release --no-incremental` ณ mod repo HEAD `91c276b9436fccdf498fc3b3eb08b62f10ec4406` (branch master, `src/ tools/ tests/` ไม่มี uncommitted) — **2026-10-28 02:5x**
- **ผล:** **FAILED — 0 Warning(s) / 1 Error(s)**:
  `src\LongYinCoop\SP5\Sp5T0Probe.cs(52,33): error CS0246: The type or namespace name 'Stopwatch' could not be found`
- **สาเหตุที่วิเคราะห์ (อ่านอย่างเดียว) [OBSERVED]:**
  - `SP5/Sp5T0Probe.cs` ใช้ `Stopwatch` (บรรทัด 52) แต่ using block ไม่มี `System.Diagnostics` · csproj หลัก `ImplicitUsings=disable`
  - ไฟล์นี้ commit ครั้งแรกใน `7ff47ab` ("SP5: … log-only probe source (**ยังไม่ build/deploy**)") — **ถูก commit โดยยังไม่เคย compile** ⇒ แผนตรวจ 0W/0E ที่รายงานในยุค S2a-4 GO-1 (ก่อน `7ff47ab`) ยังจริงสำหรับทรียุคนั้น แต่**ทรีปัจจุบัน (HEAD `91c276b`) Release-build ไม่ผ่าน**
  - เทสต์ C# 596/0 ยังผ่านเพราะ `tests/S2Protocol.Tests.csproj` include เฉพาะไฟล์ที่เลือกไว้ (S2/Protocol/**, S2/Transport/**, S2/Host subset ฯลฯ) **ไม่รวม SP5/**` — จึงไม่ได้ compile ไฟล์นี้ (แยกสองโลกกันชัดเจน)
- **ทางแก้ที่พบ (เสนอ ไม่ตัดสิน — ทั้งสองทางต้องอนุมัติแยก เพราะเป็นการแก้ tracked source):**
  - (ก) เพิ่ม `using System.Diagnostics;` ใน `SP5/Sp5T0Probe.cs` (1 บรรทัด — แก้ tracked source ของ SP5)
  - (ข) exclude `SP5/**` ออกจาก `LongYinCoop.csproj` (แก้ csproj — กระทบ build ทุก config)
  - ห้าม agent แก้เองในรอบนี้ (นอกขอบเขต [A1]–[A6] — คำสั่ง B3 กำหนด "ต้อง 0/0 ถ้าไม่ใช่หยุด")
- **สิ่งที่ต้องทำซ้ำเมื่อได้รับอนุมัติแก้แล้ว (ตาม [A5] + S2a-4-plan §9):**
  1. commit การแก้ (แยก commit, ระบุ path ไฟล์เดียว)
  2. `dotnet build src/LongYinCoop -c Release --no-incremental` ต้อง **0 warning / 0 error**
  3. บันทึก: full SHA ของ source commit · `dotnet --version` · คำสั่ง build เต็ม · sha256 + ขนาด (bytes) ของ `bin/Release/LongYinCoop.dll` และ `.pdb`
  4. ตรวจเมทาดาทาดิบ (อ่านไบต์ #Strings heap ของ DLL — **ไม่โหลดแอสเซมบลีเข้ากระบวนการใด**) ยืนยันชนิดครบ 6 ชนิด: `S2GuestDecoder`, `S2OverlayClient`, `S2OverlayRenderModel`, `S2OverlayService`, `S2SetPrefGate`, `S2WriteGuardHook`
  5. คัดลอก DLL+PDB มาไว้โฟลเดอร์นี้เป็น artifact (ไม่ commit — ห้ามคัดลอกเข้า `Mods\`)
  6. อัปเดตไฟล์นี้ด้วยค่าจริง + เติมตาราง artifact ด้านล่าง

## ตาราง artifact (ยังว่าง — จะเติมเมื่อ build ผ่าน)

| ไฟล์ | sha256 | ขนาด (bytes) |
|---|---|---|
| `LongYinCoop.dll` | — ยังไม่มี (build ล้ม) — | — |
| `LongYinCoop.pdb` | — ยังไม่มี (build ล้ม) — | — |
