# A1/A2 — reachability ของ Sp5T0Probe (session `AUTO-20261028-070417`, 2026-10-28 07:0x)

> งานนี้เป็น **read-only** ทั้งหมด (ก่อนแก้ using ตามมติ [G-1] ทาง (ก)) · สรุป: **เงื่อนไขหยุดของ [G-1] ติดตรงตัว** — probe ถูกเรียกจากเส้นทางที่รันตอนโหลดปลั๊กอิน ⇒ ห้ามแก้ source, ห้ามเปลี่ยนไปทาง (ข) เอง, รายงานรอมติ

## A2 — ผลตรวจ reachability (คำสั่ง + ผลจริง)

คำสั่ง: `grep -rn "Sp5T0Probe" src/ tests/ tools/ --include="*.cs" --include="*.py" --include="*.csproj"`

ผล: caller อยู่ใน `src/LongYinCoop/CoopMod.cs` (entry point ของ mod — `MelonMod` เดียวของปลั๊กอิน) **5 จุด**:

| จุด | เส้นทาง | คำสั่ง |
|---|---|---|
| `CoopMod.cs:53` | **`OnInitializeMelon()` = เส้นทางโหลดปลั๊กอิน** (หลัง gate `CoopPrefs.Enabled`) | `Sp5T0Probe.Apply(CoopPrefs.Sp5T0.Value);` |
| `CoopMod.cs:104` | `OnDeinitializeMelon()` (unload) | `Sp5T0Probe.Uninstall();` |
| `CoopMod.cs:119` | `Tick()` กิ่ง `!Enabled` | `Sp5T0Probe.Uninstall();` |
| `CoopMod.cs:158–160` | `Tick()` (runtime poll) | `if (CoopPrefs.Sp5T0.Value != Sp5T0Probe.Installed) Sp5T0Probe.Apply(...)` |
| `CoopMod.cs:162–164` | `Tick()` | `if (Sp5T0Probe.Installed) Sp5T0Probe.Tick();` |

อ้างอิงตัวเองภายในไฟล์ probe (`typeof(Sp5T0Probe)`, ชื่อ patch audit) ไม่นับเป็น caller ภายนอก · **ไม่มี** reference ใน `tests/` และ `tools/` (test csproj ไม่ลิงก์ SP5/** — ดู A1)

### พฤติกรรมจริงตอนโหลด (RUNTIME-พฤติกรรม อ่านจากซอร์ส ยังไม่เคยรันจริง)

- `CoopPrefs.Sp5T0` default = **false** (`CoopPrefs.cs:56` `Category.CreateEntry("Sp5T0", false)`)
- `Apply(false)` (`Sp5T0Probe.cs:146–151`) → `Uninstall()` → เนื่องจาก `Installed == false` → `CloseSession("uninstall-not-installed")` → `SessionOpen == false` ⇒ **return ทันที** (`Sp5T0Probe.cs:1330`) ⇒ คืน false
- ⇒ ณ โหลดปลั๊กอินด้วย pref default: **ไม่สร้าง `HarmonyLib.Harmony` ไม่ติด patch ใด ไม่เขียนไฟล์ใด** (side effect = ศูนย์)
- เฉพาะเมื่อผู้ใช้/pref ตั้ง `Sp5T0=true` — `Apply(true)` จึง resolve target 7 method (H1–H5) และติด Harmony patch

### การตัดสินตามเงื่อนไขที่ผู้ใช้กำหนด

เงื่อนไข [G-1]: *"ถ้า probe ถูกเรียกหรือผูก hook จากเส้นทางที่รันตอนโหลด = หยุด ห้ามแก้ ห้ามเปลี่ยนไป (ข) เอง รายงานแล้วรอมติ"*

- ข้อความเป็นเงื่อนไข **คู่สลับ**: (1) probe **ถูกเรียก** จาก load path — **ตรง** (`CoopMod.cs:53` รันจริงตอน `OnInitializeMelon` เมื่อ `Enabled=true`; และถ้า `Enabled=false` บรรทัดนี้ถูกข้าม — แต่ตัวเรียกยังอยู่ในเส้นทางโหลด) (2) ผูก hook ตอนโหลด — **ไม่ตรง** ด้วย pref default
- ⇒ ตีความตามตัวอักษร: **หยุด** — ไม่แก้ `using System.Diagnostics;`, ไม่เปลี่ยนไปทาง (ข) (exclude SP5/**) เอง · รอมติผู้ใช้
- หมายเหตุเพื่อประกอบมติ: การเพิ่ม `using System.Diagnostics;` **ไม่เปลี่ยนพฤติกรรมรันไทม์ใด ๆ** (เป็นการ compile-time resolution ของ `Stopwatch` ที่ static field initializer `Clock` ใช้อยู่แล้ว; คลาสถูก JIT ต่อเมื่อถูกอ้างอิงครั้งแรก) — แต่การตัดสินว่า "ถูกเรียกจาก load path" พอหรือไม่พอ = สิทธิ์ของผู้ใช้ตามข้อความเดิม

## A1 — สแกน commit ที่ระบุ "ยังไม่ build" / "not built"

คำสั่ง: `git log --all -i --grep="ยังไม่" --oneline` และ `git log --all -i --grep="not.build" --oneline` (+ ตรวจ `git diff --stat 91c276b ec78a8b -- src/` = ว่าง)

| commit | สาระ | มี .cs ที่ csproj หลัก compile แต่ test ไม่รวม? |
|---|---|---|
| `7ff47ab` | **SP5 probe source (ยังไม่ build/deploy)** = `SP5/Sp5T0Probe.cs` | **ใช่ — รายการเดียว** (csproj หลัก SDK-style glob ทุก .cs; `tests/S2Protocol.Tests.csproj` = include list ชัดเจน ไม่มี SP5/**) |
| `2aaa98f` | docs(VERSION) กล่าวถึง S3 prerequisite patch "ยังไม่ถูก build" | ไม่ — docs-only; patch นั้นภายหลัง commit `59276a4` และ build `f2ad30cd` deploy แล้ว (ปิดแล้ว) |
| `df46de1` | TIMEstatic notes + สคริปต์ | ไม่ — สคริปต์/TSV ไม่ใช่ .cs ของ csproj หลัก |
| `cebcf59`, `32dfd96`, `c60f405` | docs/evidence | ไม่ |

**สรุป A1:** ไฟล์ .cs ที่ "ยังไม่ build" และถูก compile โดย csproj หลักแต่ test csproj ไม่รวม มี **รายการเดียว** = `src/LongYinCoop/SP5/Sp5T0Probe.cs` (commit `7ff47ab`) — ตรงกับที่ provenance.md วิเคราะห์ไว้ ไม่มีตัวอื่นแอบแฝง

## ผลต่อ Phase นี้

- **A3 = ไม่ทำ** (แก้ using 1 บรรทัด) — รอมติ [G-1] รอบใหม่จากผู้ใช้
- **Phase B = BLOCKED** — build Release จะล้มซ้ำด้วย CS0246 เดิมแน่นอน (src/ ไม่เปลี่ยน 91c276b..ec78a8b ยืนยันแล้ว) ⇒ ไม่รัน build ที่ตัดสินผลได้ล่วงหน้า, ไม่มี artifact, ตรวจ 6 ชนิด = NOT-RUN
- **Phase C (G-2 rebaseline) ไม่เกี่ยวกับข้อหยุดนี้** — เป็นมติที่อนุมัติแยกและไม่แตะ source/build/Mods\ ⇒ ทำต่อตามคำสั่ง
