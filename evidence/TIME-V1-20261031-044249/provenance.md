# provenance — TIME-V1 artifact (มติ [V1-deploy] 2026-10-31 · compile-only · ยังไม่ deploy)

> label `TIME-V1-20261031-044249` · เตรียมโดย session `AUTO-20261031-035916` · **สถานะ: READY**
> · artifact นี้ deploy ได้**เฉพาะใน session ที่ผู้ใช้พิมพ์วลี `เริ่ม V1 session`** ตาม `RUNBOOK-V1.md`
> (ไฟล์เดียวกันโฟลเดอร์นี้) · ห้ามเข้า `Mods\` ด้วยตัวเอง

## build

| รายการ | ค่า |
|---|---|
| source | mod repo HEAD `6e99c538e7a2178e9c99f8c2b5032bc137fc3ad4` (สายโซ่ Phase รอบนี้: `fbdbb7d` errata → `5bcb009` B1 pure → `545536e` B2 probe+harness → `90682d8` B3 tools → `6e99c53` C analyzer) |
| คำสั่ง build เต็ม | `dotnet build src/LongYinCoop -c Release --no-incremental` (cwd = mod repo root) |
| dotnet | 9.0.311 |
| ผล build | **0 Warning(s) / 0 Error(s) · exit 0** (วัด 2026-10-31 04:4x) |
| determinism | ×2 — build ซ้ำ `--no-incremental` ได้ sha256 DLL/PDB **ตรงเป๊ะ** ทั้งสองครั้ง |
| ข้อบังคับ (จ) ของ [Gate 3] | `git diff 69c7a8d..6e99c53 --stat -- src/LongYinCoop/S2 src/LongYinCoop/SP5 src/LongYinCoop/TimeV2a` = **ว่าง** (ไม่แก้ S2/** SP5/** TimeV2a/**) |

## artifact (คัดลอกจาก `src/LongYinCoop/bin/Release/` → โฟลเดอร์นี้ · hash วัดซ้ำหลังคัดลอกตรงทั้งคู่)

| ไฟล์ | sha256 | ขนาด (B) |
|---|---|---|
| `LongYinCoop.dll` | `ddbb7fe87896f58fb56c86d4483ffdc60e069d4e0d728c5eafcbbf457f60d30a` | 390,144 |
| `LongYinCoop.pdb` | `1213b165f917ce775df9d604e697cf2540e6c943d438ca15234540d9cf936fdc` | 109,536 |

## ตรวจ metadata (ไม่โหลดแอสเซมบลี)

- ไฟล์หลักฐาน: `strings-check.txt` (สแกนดิบ — #Strings ชื่อสั้น ASCII + #US literal UTF-16LE) — ผล **ALL-FOUND**
- ชนิด/เมธอด V1 ใหม่ครบ: `TimeV1Probe`, `TimeV1Event`, `TimeV1Gate`, `TimeV1NdjsonWriter` +
  hook ทั้ง 6 (`StopWorkingPrefix/Postfix/Finalizer`, `MiddleStopWorkPrefix/Postfix/Finalizer`)
  + literal `time-v1.out`, `longyincoop.timev1.probe`
- ชนิดเดิมยังอยู่ครบ (S2a-4/TimeV2a/SP5): `Sp5T0Probe`, `TimeV2aSampler`, `TimeV2aObs/Event/Detector/Gate/NdjsonWriter`,
  `S2SetPrefGate`, `S2OverlayClient`, `S2WriteGuardHook`, `S2FrameMeter` + literal `time-v2a.out`, `longyincoop.sp5t0.probe`

## เทสต์ ณ build นี้ (exit code จริงทุกคำสั่ง — ไม่มี pipe กลืน exit)

| ชุด | คำสั่ง | ผล |
|---|---|---|
| C# เต็ม | `dotnet run --project tests/S2Protocol.Tests` | **RESULT PASS — 783 `[PASS]` / 0 `[FAIL]` · exit 0** (รวม TimeV1 pure 31 + TimeV1 purity harness 23 บน artifact จริง) |
| python negative | `python -m unittest test_negative_preconditions` (ใน tools/) | 53 tests **OK** · exit 0 |
| python เต็ม | `python -m unittest discover -p "test_*.py"` (ใน tools/) | 129 tests **OK** · exit 0 (รวม time_v1_analyze 17 + pref_key_check 21) |
| static check | `python tools/s2_static_check.py` | **CLEAN (0 violations)** · exit 0 |

## ขอบเขตของ build นี้

- **เพิ่มใหม่**: `src/LongYinCoop/TimeV1/TimeV1Rules.cs` (pure) · `src/LongYinCoop/TimeV1/TimeV1Probe.cs`
  (Harmony log-only 2 เป้า = 6 patches) · pref `TimeV1a` (default **false**) · pointer `time-v1.out` ·
  wiring 3 จุดใน `CoopPrefs.cs`/`CoopMod.cs`
- **ไม่แตะ**: `S2/**`, `SP5/**`, `TimeV2a/**` (พิสูจน์ด้วย diff ว่าง), ไฟล์เกม, `Mods\`, net.cfg,
  MelonPreferences.cfg, เซฟ
- default ณ โหลด = `TimeV1a=false` ⇒ `Apply(false)` = no-op ล้วน (ไม่สร้าง Harmony/ไม่เปิดไฟล์/ไม่เปิดเธรด) —
  พิสูจน์ด้วย harness (TimeV1PurityHarness บน artifact จริง)

## rollback point ที่คาดหวังตอน deploy (ตรวจซ้ำใน RUNBOOK ขั้น 1)

`Mods\LongYinCoop.dll` = `6a94abb5…` (359,936 B) / `Mods\LongYinCoop.pdb` = `856d3bed…` (97,128 B) —
สำรองเป็น `.prev-6a94abb5-<ts>` / `.prev-856d3bed-<ts>` (ตรวจชื่อว่างก่อนทุกครั้ง — ชื่อซ้ำ = เลือก ts ใหม่)
