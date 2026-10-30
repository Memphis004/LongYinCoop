# HANDOFF — session เตรียม SP5-T0 (offline) `2026-10-30 01:02–`

> ไฟล์ส่งมอบบนดิสก์ **ไม่ commit** (convention เดิม) · คำสั่งต้นทาง: ข้อความผู้ใช้ 2026-10-30 "เตรียม SP5-T0 ให้พร้อมรัน"
> (โหมด offline เท่านั้น) · มติแนบมา: SP5-T0 §4 ข้อ 1 = อนุมัติแผน rev 13 เท่านั้น (ข้อ 2–8 ยังไม่อนุมัติ) +
> มติ context.txt / notes / scan scripts / blobs / push / popup 3 ข้อ

## สรุปผูกมัด 6 บรรทัด (อ่านก่อน)

1. **session-start ผ่านครบ** — เกมไม่รัน (ตรวจ tasklist + path-based) · hash เกม 2 ไฟล์ตรง VERSION.md เป๊ะ ·
   `save-split status` = 57 ไฟล์ `df8e5a11beed` + `leftovers: none` · git สอง repo ตรง snapshot (game `944ceda`,
   mod `8816748`) — รายการเดียวที่ไม่อยู่ใน snapshot = `evidence/AUTO-20261030-002912/HANDOFF.md` = **HANDOFF ของ
   session ก่อนเอง** (convention บนดิสก์) ตรวจแล้วไม่ใช่การแก้จาก session อื่น
2. **push:** mod = **สำเร็จ** `c826961..8816748` (3 commits ตามมติ; fetch ก่อน ตรง snapshot) → origin/master =
   `8816748` · **game = ค้างรอมติ** — fetch แล้ว `origin/main = 531fe42` (local นำ 18 commits, docs-only,
   FF ได้) ซึ่งขัดมติ "game 1 commit (944ceda)" ⇒ หยุดตามกฎ ไม่ push
3. **งานเตรียม SP5-T0:** gap ของ §5 ข้อ 2 มีข้อเดียว = harness test ของ linkage `iid` → เขียน
   `Sp5T0LinkageHarness` (reflection บน artifact จริง) **ผ่าน 17/0** · ป้าย probe "UNPROVEN→harness-proven"
   (string-only) · **artifact ใหม่ READY แต่ยังไม่ deploy**: DLL `865b4a87…` / PDB `60ad4546…` @ commit `ab9d07b`
   · `Mods\LongYinCoop.dll` **ยังเป็น `6a94abb5` ไม่แตะ**
4. **commits ใหม่ (local ทั้งหมด, ยังไม่อนุมัติ push):** `0ef34e6` (notes+INDEX ปิด reorg) → `f595c02`
   (scan scripts) → `2f280ae` (popup testimony append) → `ab9d07b` (harness + ป้าย probe) → `a998509`
   (evidence/provenance) · `context.txt` กู้คืนแล้ว (ไม่ต้อง commit — ตรง HEAD)
5. **popup 3 ข้อ:** ข้อความรอบนี้มาช่องว่าง `[...]` เหมือนเดิม → บันทึก append-only ท้าย
   `user-popup-answers-20261029.md` (USER TESTIMONY — ยังไม่มีคำตอบ, ไม่เติมแทน)
6. **decisions-pending.md อัปเดตแล้ว (append-only):** §2 (ผลงานเตรียม SP5-T0 + ทางเลือก build ตอน deploy) ·
   §4 (ผล push + game push ค้าง) · §5 (ผลมติ context.txt/notes/scans/blobs) · §6 (สถานะ popup) — ทางเลือกเดิมไม่ลบ

## session-start checks (read-only — ผลจริง)

| ข้อ | ผล | สรุป |
|---|---|---|
| เกมไม่รัน | tasklist ไม่พบ `LongYinLiZhiZhuan.exe` + PowerShell path-check ไม่พบ process จากโฟลเดอร์เกม | PASS |
| hash เกม | `GameAssembly.dll` `33c56def…` EXACT · `global-metadata.dat` `451311d4…` EXACT (ตรง VERSION.md, ขนาดตรง) | PASS |
| เซฟ | `Save` 57 ไฟล์ digest `df8e5a11beed` + `leftovers: none` (mounted EXACT ทุก baseline รอบ V2-A/GO-3) | PASS |
| git mod | HEAD `8816748` ตรง · status ตรง snapshot + `evidence/AUTO-20261030-002912/` (HANDOFF session ก่อน — ระบุตัวตนได้) | PASS |
| git game | HEAD `944ceda` ตรง · status ตรง snapshot เป๊ะ | PASS |

## มติที่ทำแล้ว (ครบทุกข้อที่แนบมา ยกเว้น game push — ขัดเงื่อนไข)

| มติ | ผลจริง |
|---|---|
| push mod 3 commits | fetch ก่อน: origin/master `c826961` ตรง snapshot · push `c826961..8816748` FF สำเร็จ exit 0 · ยืนยัน origin = `8816748` |
| push game 1 commit | **ไม่ทำ** — origin/main `531fe42` (18 ระยะห่าง) ขัดมติ "1 commit" → หยุดรายงาน (FF ได้ ไม่มี divergence; 18 commits = docs AGENTS.md+STAGEPLAN.md 10-21→10-30) |
| context.txt | `git checkout -- context.txt` → 887 บรรทัด, `git diff HEAD` ว่าง (ตรง HEAD ทุก byte) |
| notes + INDEX | commit `0ef34e6` — notes 2 ไฟล์ byte-identical กับ game repo HEAD (ยืนยันด้วย diff ตรง), INDEX delta = 2 แถว + แก้บรรทัด "ไม่ย้าย" |
| scan scripts | commit `f595c02` — read-only scanners, ไม่มีค่าลบในไฟล์ (ตรวจก่อน commit) |
| blobs d81c86d | ไม่แตะ (ตามมติ — ไม่ rewrite ไม่ force-push) |
| popup 3 ข้อ | append `2f280ae` — สามคำถามช่องว่างตามข้อความจริง, ป้าย USER TESTIMONY, อ้างกฎ fallback ของผู้ใช้เอง |

## งานเตรียม SP5-T0 (ตามแผน rev 13 — §4 ข้อ 1 เท่านั้น)

**ตาราง gap (§5 ข้อ 2 เทียบ build `6a94abb5` — ตรวจจากซอร์ส `src/LongYinCoop/SP5/Sp5T0Probe.cs` 1,477 บรรทัด,
ซึ่ง `src/` ตรง `636c209` ที่เป็น build source เป๊ะ):**

| รายการ §5/แผน | สถานะ |
|---|---|
| H1 `StartNewGame` (prefix audit+push+sng.begin / postfix sng.end+snapshot / finalizer pop) | **มีครบ** |
| H2 `GenerateHero` (audit + heroNum.read ทางหลัก + baseline/ปลายทาง E1) | **มีครบ** |
| H3 `GenerateHeroData` A/B/C (factory.call ครบ args / factory.result อ่าน `__result` typed อย่างเดียว / factory.finalizer เชิงบวกทุก invocation / factory.failure conditional / ห้ามอ่าน `__result` ใน finalizer) | **มีครบ** |
| H4 `GenerateRandomEnemy` (audit + maxNum + tempBefore/After) | **มีครบ** |
| H5 `AddTempHero` (addtemp + hidT + countBefore/After) | **มีครบ** |
| patch.audit 4 จุดตรวจ + expected inventory เป๊ะ (5 categories ของ HarmonyX 2.10.2) + cross-stable + ประกาศ in-window UNKNOWN | **มีครบ** |
| log-only 100% / try-catch ทุก hook / void finalizer คืน exception เดิม / all-or-nothing install + ถอนถ้าไม่ครบ 7 / Uninstall ตรวจ patch คืน 0 / pref `Sp5T0` default **false** (`CoopPrefs.cs:59`) | **มีครบ** |
| **harness test ของ linkage `iid` (§2.5 rev 8–9 + §5 ข้อ 2)** | **ขาด → เขียนแล้วใน session นี้** |

**สิ่งที่เพิ่ม (commit `ab9d07b`):** `tests/S2Protocol.Tests/Sp5T0LinkageHarness.cs` (+199) — โหลด
`LongYinCoop.dll` จริงด้วย reflection (ไม่ re-implement, ไม่แตะตรรกะ) · 17 checks: LIFO nesting 2+4 ระดับ /
exception-path pop / cross-thread isolation / iid โมโนโทนิก · ป้าย probe 4 จุด "UNPROVEN→harness-proven offline"
(string-only) · **ขอบเขตประกาศในไฟล์:** mismatch-branch ของ `PopInv` (Unity-bound) ไม่ถูกกระตุ้น — ครอบด้วย code
audit GO-1; ordering จริงของ Harmony บนเป้าเกมยังไม่รัน · **`#C.ok`/E1/E2 ยัง UNKNOWN ตาม rev 13** (ไม่เปลี่ยน)

**build + provenance (commit `a998509`, `evidence/SP5T0-prep-20261030-010247/`):**

| รายการ | ค่า |
|---|---|
| build | `dotnet build src/LongYinCoop -c Release --no-incremental` ณ `ab9d07b` — **0W/0E exit 0** (SDK 9.0.311) |
| artifact | DLL **`865b4a87…`** 366,592 B · PDB **`60ad4546…`** 99,572 B — สำเนาใน evidence hash ตรง · **determinism ×2 ที่ HEAD เดียวกัน** |
| หมายเหตุ UNKNOWN | build ก่อน commit (ซอร์สเนื้อหาเดียวกัน, HEAD ยัง `2f280ae`) ให้ `33c48c8c…` — ต่างจากต้นฉบับ post-commit; สาเหตุไม่ถอด (น่าจะ metadata ฝังขึ้นกับ git state) — บันทึกตามจริงใน provenance |
| เทสต์ | C# **650 PASS / 0 FAIL** (รวม harness 17/0 — log พิมพ์ sha256 ของ artifact ที่โหลด ตรง) · python **53 OK** + discover **106 OK** · static check **CLEAN** · B3 #Strings ดิบ: Sp5T0Probe + TimeV2a + S2a-4 ครบ **FOUND** (exit 0) |
| deploy | **ไม่ deploy** — `Mods\LongYinCoop.dll` ยังเป็น `6a94abb5` (มติ [G-3]/rollback ทาง ข) · VERSION.md ไม่แตะ |

## ทางเลือกที่ต้องมติตอน deploy (ระบุไว้ล่วงหน้าใน decisions-pending §2)

ตอนอนุมัติ §4 ข้อ 2 ต้องเลือก build ที่จะ deploy: (ก) build ใหม่ `865b4a87` (TimeV2a + ป้าย linkage ใหม่ —
probe ตรรกะเดียวกับ `6a94abb5`) หรือ (ข) `6a94abb5` (ป้ายเก่า "iid UNPROVEN") — agent ไม่เลือกเอง

## ค้างต่อ (ครบใน decisions-pending.md)

- **game push รอมติ** (18 commits — ดู §4 ของไฟล์) · ป้าย push commits ใหม่ 5 ตัวของ session นี้ยัง local
- **SP5-T0 §4 ข้อ 2–8 รออนุมัติรายข้อ** ด้วยวลีสั่งแยกในเซสชันเกมแยก (ห้ามรวมกับ V2-A (ก)/(ข) หรือ V1)
- popup 3 ข้อยังไม่มีคำตอบ · V2-A (ก)/(ข) · D3–D10 · ค้างเดิมทั้งหมด (S2-plan §13, VPN/guest/60 ยูนิต, E4 ซ้ำ)
