# C1–C4 — เงื่อนไขก่อนแก้ using ตามมติ [G-1] รอบใหม่ (session `AUTO-20261028-072525`)

> อ่านอย่างเดียวทั้งหมด · วันเวลา 2026-10-28 07:25–07:3x · ผล: **ผ่านทั้ง 4 ข้อ** ⇒ เข้า Phase A ได้

## C1 — อ่าน Sp5T0Probe.cs ทั้งไฟล์ (1,477 บรรทัด) — เส้นทางเมื่อ `Sp5T0=false`

**Static field initializers (สิ่งเดียวที่รันตอน type init — ถูก trigger ครั้งแรกเมื่อ `Apply` ถูกเรียกที่โหลด):**

| บรรทัด | โค้ด | side effect |
|---|---|---|
| 57 | `private static readonly object FileSync = new object();` | จอง object ล้วน |
| 58 | `private static readonly Stopwatch Clock = Stopwatch.StartNew();` | สร้าง Stopwatch (`System.Diagnostics` — using ที่กำลังจะเพิ่ม) = ตัวจับเวลา in-memory เท่านั้น |
| 76–79 | `_ovCalls/_ovResults/_ovResultsNonNull/_ovFinalizers/_ovFailures = new long[3]` | จอง array |
| 89 | `_lastAuditFp = new string[3]` | จอง array |

- **ไม่มี static constructor** — grep `static Sp5T0Probe` = ไม่พบ · **ไม่มี** `[ModuleInitializer]`, `new Thread`, `new Timer`, `new Task`, `async` (grep ทั้งชุด = ไม่พบ, exit 1)

**เส้นทาง `Apply(false)` ที่โหลด (พิสูจน์ด้วยเลขบรรทัด):**

```
CoopMod.cs:53        Sp5T0Probe.Apply(CoopPrefs.Sp5T0.Value)     ← pref = false
Sp5T0Probe.cs:146-152  Apply(bool wanted) { if (!wanted) { Uninstall(); return false; } … }
Sp5T0Probe.cs:260-265  Uninstall() { if (!Installed) { CloseSession("uninstall-not-installed"); return; } … }
Sp5T0Probe.cs:1326-1330 CloseSession(reason) { lock (FileSync) { if (!SessionOpen) return; … } }
```

- `Installed` = false และ `SessionOpen` = false ณ โหลด ⇒ จบที่ guard ทันที — **โค้ดหลัง guard ไม่ถูกแตะ**
- จุดที่**สร้าง side effect ได้**อยู่หลัง guard ทั้งหมด (ในกิ่ง `wanted==true` เท่านั้น — ยืนยันด้วย grep):
  - `new HarmonyLib.Harmony` ตำแหน่งเดียว = **บรรทัด 201** (หลัง gate 148–152 และหลัง `OpenWriter()` 197)
  - `_harmony.Patch(...)` ×7 = **บรรทัด 203–230** (กิ่งเดียวกัน)
  - `OpenWriter()` (สร้างไดเรกทอรี + เปิดไฟล์, บรรทัด 1243–1261 ใช้ `Directory.CreateDirectory`/`FileStream`) ถูกเรียกตำแหน่งเดียว = **บรรทัด 197** ในกิ่ง `wanted==true`
  - `WriteSummary` (เขียน summary.json, บรรทัด 1445) ถูกเรียกเฉพาะจาก `CloseSession` **หลัง** guard `SessionOpen` (1388)
- คลาสภายนอกที่ถูกแตะจริงในเส้นทาง false: `System.Diagnostics.Stopwatch` + BCL พื้นฐาน (`object`, `lock`) เท่านั้น — `CoopLog`/`CoopPaths`/`CompatInventory`/`AccessTools` อยู่หลัง guard ทั้งหมด
- **สรุป C1:** เมื่อ `Sp5T0=false` ทุกเส้นทางที่รันตอนโหลด = allocation in-memory + lock + อ่าน static bool ⇒ **ไม่สร้าง Harmony instance, ไม่ติด patch, ไม่เขียน/เปิดไฟล์, ไม่เปิดเธรด/timer** ✓

## C2 — ไม่มี path อื่นติดตั้ง hook โดยไม่ผ่าน pref

grep ทั้ง `src/ tests/ tools/` (ไฟล์ .cs/.py/.csproj): caller ของ `Sp5T0Probe` มีเฉพาะใน `CoopMod.cs` 5 จุด:

| บรรทัด | โค้ด | gate |
|---|---|---|
| 53 | `Sp5T0Probe.Apply(CoopPrefs.Sp5T0.Value);` (ใน `OnInitializeMelon`) | ค่า pref เป็นตัวกำหนด (false = เข้าเส้นทาง no-op ของ C1) |
| 104 | `Sp5T0Probe.Uninstall();` (ใน `OnDeinitializeMelon`) | ถอน — ติดตั้งไม่ได้ |
| 119 | `Sp5T0Probe.Uninstall();` (ใน `Tick` กิ่ง `!Enabled`) | ถอน — ติดตั้งไม่ได้ |
| 158–160 | `if (CoopPrefs.Sp5T0.Value != Sp5T0Probe.Installed) Sp5T0Probe.Apply(CoopPrefs.Sp5T0.Value);` | ผ่าน pref โดยตรง |
| 162–164 | `if (Sp5T0Probe.Installed) Sp5T0Probe.Tick();` | ทำงานเมื่อติดอยู่แล้วเท่านั้น |

ไม่มี reference อื่นใน src (ส่วนที่เหลือในผล grep = self-reference ภายใน Sp5T0Probe.cs เอง) · **สรุป C2: ผ่าน** ✓

## C3 — pref default และไฟล์จริง

- `CoopPrefs.cs:56`: `Sp5T0 = Category.CreateEntry("Sp5T0", false);` — **default = false**
- `UserData\MelonPreferences.cfg` (sha256 `269b571c…98d1` — ตรง session-start ของ session นี้): `grep "Sp5T0"` = **ไม่พบ key เลย** (exit 1; ไฟล์ 69 บรรทัด — ไม่พิมพ์เนื้อหาอื่นตามข้อห้าม) ⇒ MelonLoader ใช้ค่า default `false` แน่นอน · **สรุป C3: ผ่าน** ✓

## C4 — hook ของ S2 ทั้งหมดไม่ถูกติดตั้งตอนโหลด (M5-3 / แผน S2a-4 rev4)

- grep `Harmony` ใน `src/LongYinCoop/S2/**`: พบเฉพาะ 2 บรรทัด **comment/doc** (`S2WriteGuard.cs:15` อธิบายย้อนหลัง, `S2WriteGuardHook.cs:35` เป็นข้อห้ามใน docstring) — **ไม่มี code reference**
- `S2WriteGuardHook.cs`: ไม่มี `HarmonyLib`/`Patch` ใน code (pure flag logic — call sites อยู่ใน tests เท่านั้น บังคับโดย scanner)
- `.Patch(` จริงทั้ง src (grep): `HarmonyProbe.cs:50`, `S1/BattleLogHook.cs:64`, `S1.5c/JitProbe*.cs` (ทั้งหมด S1-era และ pref-gated), `SP5/Sp5T0Probe.cs:203–230` — **0 จุดใน S2/**
- `python tools/s2_static_check.py` = **CLEAN (0 violations), exit 0** (write-path call sites = 0, Harmony code refs ใน S2/** = 0)
- CoopMod โหลด S2 ผ่าน `new S2HostService(...)` + `new S2OverlayService(...)` = service objects ไม่ใช่ patch; W4(a) ไม่ถูก implement ตามมติ M5-3 (GO-1 commits — AGENTS.md 2026-10-28: "ไม่มี W4(a) ไม่มี hook ใดถูกสร้าง/ติดตั้ง") · **สรุป C4: ผ่าน** ✓

**บันทึกเชิงสังเกต (นอกขอบเขต C4 — รายงานเพื่อความครบถ้วน ไม่ใช่ข้อหยุด):** `S1/BattleLogHook.cs` (S1-era) ติด Harmony postfix บน `BattleController.AddInfoText` แบบไม่มี pref รายตัว (มีแค่ gate `Enabled` ของ CoopMod) และถูกเรียกที่ `CoopMod.cs:55` ทุกครั้งที่โหลด — เป็นพฤติกรรมเดิมตั้งแต่ S1 ที่อยู่ใน build ที่ deploy อยู่แล้ว (`1b4c17aa`) และผ่านการวัดทุก stage ตั้งแต่ S1.5 · การเพิ่ม using 1 บรรทัด **ไม่เปลี่ยน** พฤติกรรมนี้ · นอกขอบเขตมติ M5-3 (ซึ่งตัดสินงาน hook ใหม่ของ S2a-4 = W4(a))

## มติบนข้อเท็จจริง

C1 ✓ C2 ✓ C3 ✓ C4 ✓ ⇒ ตามมติ [G-1] รอบใหม่: อนุมัติแก้ `using System.Diagnostics;` 1 บรรทัดใน `src/LongYinCoop/SP5/Sp5T0Probe.cs` — เข้า Phase A
