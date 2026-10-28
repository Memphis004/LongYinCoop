# Gate 2 — ขั้น 1–3.2: Release build + rollback + save split + deploy (2026-10-25 03:41)

## ขั้น 1 — Release artifact (ยังไม่ deploy ตอน build)

| รายการ | ค่า |
|---|---|
| source commit | `59276a4` (`git diff 59276a4 -- src/ tests/` = ว่าง — working tree ตรง commit) · HEAD `ce3e06c` (docs-only ทับ) |
| build command | `dotnet build src/LongYinCoop -c Release --no-incremental` (โปรเจกต์ `src/LongYinCoop/LongYinCoop.csproj`) |
| ผล | **exit 0 · Build succeeded · 0 Warning · 0 Error** (log: `gate2-release-build.txt`… คัดลอกไว้ `release-build.txt`) |
| artifact DLL | `src/LongYinCoop/bin/Release/LongYinCoop.dll` sha256 **`f2ad30cd2090a63879a74605c32efc999163b9d1873da6a68bd875e872f112a8`** (314,880 B) |
| artifact PDB | `LongYinCoop.pdb` sha256 `fab0e051a6d279e2dae583073af88257670cc16c4d9853e3d2af5663dee1f891` (82,668 B) |
| เทียบตัว deploy เดิม | เดิม `5d7b7cc5…` (314,368 B, Oct 24 01:28, source `ffc2c88`) — **ต่างกัน** (เพิ่ม S3 prerequisite: AliveRule/CompletionAlive/guard/hash v2, +512 B) |
| provenance | source `59276a4` → build คำสั่งข้างบน → sha `f2ad30cd…` → deploy และตรวจ sha หลัง deploy ตรงกัน (ด้านล่าง) — ห่วงโซ่ปิดครบ ไม่มีขั้นตอนใดไม่มีหลักฐาน |

## ขั้น 2 — Rollback + save safety

| รายการ | ค่า |
|---|---|
| DLL deploy เดิม (ก่อนแตะ) | `Mods\LongYinCoop.dll` = `5d7b7cc5520ae784c5efc198dda8c1c3ba33ff3c55eb7aaa955ede5f63735953` · PDB = `3a713953b2246e664c588a8572e5cc32565da985554d52209de19590591ec069` |
| rollback สร้างใหม่ | `Mods\LongYinCoop.dll.prev-5d7b7cc5` (ตรวจก่อน: ไม่มีอยู่ — ไม่เขียนทับอะไร) · `Mods\LongYinCoop.pdb.prev-3a713953` — สองไฟล์ hash ตรงต้นทาง (`5d7b7cc5520ae784…`/`3a713953b2246e66…`) |
| rollback เดิมอื่น | คงทุกไฟล์ (`.prev-c51debef`, `.prev-3c9584a4`, `.off` ฯลฯ) ไม่ถูกแตะ |
| save split | `save-split split --label S3p-20261025-0341 --expected-baseline coop-test/baselines/20261024-232024/savehash.real.before.txt` — Save vs baseline **changed=0/missing=0/added=0** · real → `Save.real-S3p-20261025-0341` (aside) · `Save` = สำเนา 57 ไฟล์ (`7332fc93eb98`) |
| baseline `4226730f66ff` | **ไม่แตะ** (และ baseline ปัจจุบัน `7332fc93eb98` ก็ไม่แตะ — แค่อ้างอิง) |
| preflight | `save-split preflight-launch --label S3p-20261025-0341` = **READY ทั้ง 11 ข้อ** (เกมไม่รัน · aside ตรง manifest · mounted-is-copy EXACT changed=0 · real-aside-untouched `7332fc93eb98` · ไม่มี hidden sentinel) |

## ขั้น 3.1–3.2 — Deploy + ตรวจหลัง deploy

| รายการ | ค่า |
|---|---|
| deploy | copy artifact DLL+PDB → `Mods\` |
| hash หลัง deploy | `Mods\LongYinCoop.dll` = **`f2ad30cd2090a63879a74605c32efc999163b9d1873da6a68bd875e872f112a8`** ✓ ตรง artifact · PDB `fab0e051…` ✓ |
| rollback พร้อม | `.prev-5d7b7cc5` (314,368 B) + `.prev-3a713953` อยู่ในที่ |

## สถานะปัจจุบัน = หยุดรอผู้ใช้เปิดเกม (ขั้น 3.3)

- **เกมยังไม่เปิด** — ตามกติกา agent เปิดเกมเองไม่ได้
- ขั้น 3.4–3.8 (negative 20 เคส + ตรวจจบศึก) = **รอหลังผู้ใช้เปิดเกมและเข้าเงื่อนไขศึก**
- เงื่อนไขศึกที่ต้องมี (ตาม plan w1–w3): ศึกที่ผู้เล่นมี unit `0:0` เป็น active · ศัตรูนอกระยะ ≥1 ตัว (เคส OUT_OF_RANGE) · ศพ 1 ตัว (เคส TARGET_DEAD 11–14 — ผู้เล่นสังหารศัตรู 1 ตัวระหว่างทางแบบเดียวกับ S1.5f) · state WaitPlayerInput (w1/w3) และ WaitAttacking (w2)
- **ข้อจำกัดที่ต้องตัดสินร่วมหลังเปิดเกม:** plan ใช้ stableId จากศึกของ S1.5f (`1:10066` ฯลฯ) — ศึกในเซฟปัจจุบันอาจมี heroID ต่างออกไป ⇒ เมื่อเกมเปิดและเข้าศึกแล้ว agent จะอ่าน `spike status` เพื่อจับคู่ stableId จริง แล้ว**เสนอการแม็พ ID ของ 20 เคสเดิม (เคส/เงื่อนไข/expected ไม่แก้ — เฉพาะ target id)** ขออนุมัติก่อนยิงจริงตามเงื่อนไข "ห้ามแต่ง expected หรือสร้างเคสใหม่"
