# HANDOFF — session `AUTO-20261028-072525` (2026-10-28 07:25 เริ่ม — ปิด ~07:4x)

> ไฟล์ส่งมอบบนดิสก์ **ไม่ commit** (ตามคำสั่ง D) · คำสั่งต้นทาง: ข้อความผู้ใช้ 2026-10-28 (ปิดมติ [G-1] รอบใหม่ + build Release artifact S2a-4 GO-3 — ห้ามเริ่ม GO-3/เปิดเกม/deploy/แตะ net.cfg/เซฟ/tag/ปิด stage)

## สรุปผูกมัด 5 บรรทัด (อ่านก่อน)

1. **[G-1] รอบใหม่ ปิดแล้ว** — มติผู้ใช้ = ทาง (ก) โดยตีความ tripwire ใหม่ว่า "กัน hook ที่ติดตั้งจริงตอนโหลด ไม่ใช่การเรียก no-op" · เงื่อนไขก่อนแก้ **C1–C4 ผ่านครบ** (อ่านอย่างเดียว, หลักฐาน `c1-c4-precondition-checks.md`): ณ `Sp5T0=false` (default ทั้งจาก `CoopPrefs.cs:56` และจากการไม่มี key ใน MelonPreferences.cfg) `Apply(false)` จาก load path = no-op ล้วน — ไม่สร้าง Harmony (บรรทัด 201 เดียว อยู่กิ่ง `wanted==true`), ไม่ patch (203–230), ไม่แตะไฟล์ (`OpenWriter` เรียกที่ 197 เท่านั้น), ไม่มี static ctor/Thread/Timer, S2/** ไม่มี hook (static check CLEAN)
2. **Phase A (commit `636c209`)** — แก้ `using System.Diagnostics;` 1 บรรทัด (git diff = +1 บรรทัด ไฟล์เดียว) · **C# 596 PASS/0 FAIL (exit 0)** · python **53 OK** + discover **68 OK (exit 0)** · **static CLEAN (exit 0)** — exit code จริงทุกคำสั่ง (redirect log + `$?` แยก ไม่มี pipe กลืนสถานะ)
3. **Phase B (commit `1359217`)** — Release build ณ `636c209`: **0 Warning / 0 Error, exit 0** (dotnet 9.0.311) · **artifact = DLL `6a94abb579e65d5e6b0af917dfed4ba6f5350ad76847190bd7263cda3077b334` (359,936 B) + PDB `856d3beda727218823170f861f532509c5458eb91e51c79f07c0452d3b196035` (97,128 B)** อยู่ `evidence/S2a4-GO3-20261028-030025/` (hash ตรงต้นทางหลัง copy; ไม่ commit — .dll ถูก gitignore, **ไม่อยู่ใน Mods\**) · **B3 #Strings heap ดิบ: 6 ชนิด FOUND ครบ** (สคริปต์ `b3_strings_heap_check.py` + ผล `b3-strings-result.txt` ใน evidence ของ session นี้ — parse PE→CLR→BSJB ไม่โหลดแอสเซมบลี) · provenance.md = **READY** (BLOCKED history คงไว้) · RUNBOOK-G3: prereq 1 ปิด, ขั้น 6 เพิ่มข้อตรวจ **ML log ต้องไม่มีบรรทัด SP5/Sp5T0 install/patch ใด (ถ้ามี = หยุดรายงาน)**, E5 เพิ่มหลักฐาน (d) — เกณฑ์ E1–E8/G0/E6 **ไม่แก้**
4. **Phase C (game repo commit `36f1150`, path AGENTS.md เดียว)** — baseline ปัจจุบัน = `df8e5a11beed…` @ `coop-test/baselines/20261028-071340` (เก่า `20261025-201545` = superseded เก็บเป็นประวัติ) · net.cfg = `12541f34…` 444 B (VERSION.md แก้แล้วตาม DEV-2 ของ session 070417) · deviation เซฟ = ปิดแล้ว (G-2) · สรุป G-1/G-2/DEV-2 ครบ · M/D เดิมของ game repo (STAGEPLAN.md, battle-notes.md, worldmap-notes.md, il2cpp-frida-mcp) ไม่ถูกแตะ
5. **GO-3 พร้อมเริ่ม** — prerequisite 2 ข้อปิดครบ (artifact READY + baseline ใหม่) · พิมพ์ `เริ่ม GO-3 session` ตาม RUNBOOK-G3 ขั้น 0–9 (ผู้ใช้เปิดเกมเอง · deploy/swap `Mods\` = สิทธิ์จากวลีนั้นเท่านั้น)

## Commits (เรียงตามเวลา)

| repo | commit | สาระ |
|---|---|---|
| mod | `636c209ecd61a572f25d40179b99de370e9892fb` | build-fix only: using 1 บรรทัด (path เดียว) |
| mod | `1359217` | provenance READY + RUNBOOK prereq 1 closed + ขั้น 6/E5 เพิ่มข้อตรวจ SP5 (2 path) |
| game | `36f1150` | AGENTS.md: baseline ใหม่/net.cfg/สรุปมติ (path เดียว) |

- สายโซ่ mod repo: `ec78a8b` → (070417) `d4fdf12`, `9f5c17a`, `8ea8a90` → (072525) `636c209`, `1359217` · game repo: `246792e` → `36f1150`
- ไม่มี tag/push/amend/force/add -A · ทุก commit ตรวจ `git diff --cached --name-only` ก่อน commit · ไม่แตะไฟล์ .dat สำรองใน evidence 070417

## ผลตรวจ session-start (อ่านอย่างเดียว — ผ่านครบ)

เกมไม่รัน (PS count=0 + tool) · hash เกม `33c56def…`/`451311d4…` ตรง VERSION.md · mod HEAD `8ea8a90` (ตรงที่กำหนด) + 0 tracked-modified, 69 untracked (เดิม) · game HEAD `246792e` + M/D เดิม · save 57 ไฟล์ digest `df8e5a11beed` ตรง baseline `20261028-071340` + `leftovers: none` · mod 4 ไฟล์ (`1b4c17aa`/`dcd78ebd`/`269b571c`/`12541f34` 444 B) ตรง · ไม่มี ML log ใหม่หลัง `26-10-27_2-26-26.log`
- หมายเหตุ: **ไม่มี** โฟลเดอร์ `evidence/S2a4-GO3-20261028-0655` (มีแค่ `030025`) — คำสั่งกันลบโฟลเดอร์อื่นปฏิบัติครบ (ไม่ลบ/ย้ายอะไร)

## ผลตรวจ C1–C4 (สรุป — เต็มใน `c1-c4-precondition-checks.md`)

| ข้อ | ผล | หลักฐานหลัก |
|---|---|---|
| C1 | **ผ่าน** | อ่าน Sp5T0Probe.cs ครบ 1,477 บรรทัด — static init = object/Stopwatch/arrays เท่านั้น (57–58, 76–79, 89); ไม่มี static ctor/ModuleInitializer/Thread/Timer/Task/async (grep exit 1); `new HarmonyLib.Harmony` ที่ 201, `.Patch` 203–230, `OpenWriter()` เรียกที่ 197 = เฉพาะกิ่ง `wanted==true`; เส้นทาง false: 148–152 → 262–265 → 1330 guard return ล้วน |
| C2 | **ผ่าน** | caller 5 จุดใน CoopMod.cs (53/104/119/158–160/162–164) — ติดตั้งได้เฉพาะผ่าน `CoopPrefs.Sp5T0.Value==true`; ไม่มี reference อื่นใน src/tests/tools |
| C3 | **ผ่าน** | `CoopPrefs.cs:56` default false · MelonPreferences.cfg (`269b571c…` ตรง session-start) **ไม่มี key `Sp5T0`** (grep exit 1, 69 บรรทัด ไม่พิมพ์เนื้อหาอื่น) |
| C4 | **ผ่าน** | S2/** ไม่มี Harmony code refs (2 บรรทัด doc เท่านั้น) · S2WriteGuardHook = pure, call sites ใน tests เท่านั้น · `.Patch(` จริงทั้ง src = S1-era + SP5 เท่านั้น · `s2_static_check.py` CLEAN (0 violations, exit 0) · W4(a) ไม่ถูก implement ตาม M5-3 |

## ผลวัด Phase A/B (เทียบเกณฑ์)

| เกณฑ์ | ค่าที่ต้องได้ | ค่าที่วัด | ผล |
|---|---|---|---|
| git diff หลังแก้ | +1 บรรทัด ไฟล์เดียว | 1 file, 1 insertion(+) | ✓ |
| C# suite | 596 PASS / 0 FAIL | 596/0 (นับ `[PASS]` รายบรรทัด; s2a3 179/0, s2a4 130/0), exit 0 | ✓ |
| python tests | 68 OK | Ran 53 = OK (exit 0) + Ran 68 = OK (exit 0) | ✓ |
| s2_static_check.py | CLEAN | CLEAN, 0 violations, exit 0 | ✓ |
| build Release | 0W/0E | 0 Warning(s) / 0 Error(s), exit 0 | ✓ |
| 6 ชนิดใน #Strings | ครบ | FOUND ทั้ง 6 (S2GuestDecoder/S2OverlayClient/S2OverlayRenderModel/S2OverlayService/S2SetPrefGate/S2WriteGuardHook), exit 0 | ✓ |
| artifact | ตรงต้นทางหลัง copy | DLL `6a94abb5…` 359,936 B · PDB `856d3bed…` 97,128 B ตรงเป๊ะ | ✓ |
| provenance | READY | READY + artifact table + HEAD `636c209` + ข้อความ SP5 ชัด | ✓ |

- บริบท build: source commit `636c209ecd61a572f25d40179b99de370e9892fb` · `dotnet --version` = 9.0.311 · คำสั่งเต็ม `dotnet build src/LongYinCoop -c Release --no-incremental` (จาก mod repo root) · log เก็บที่ `/tmp/rel-build.log` (0W/0E ปรากฏใน commit message + provenance)

## hash ก่อน/หลัง session (ต้องเหมือนกัน — ตรงทุกตัว)

| รายการ | ก่อน (07:25) | หลัง (07:4x) | เหมือนกัน |
|---|---|---|---|
| GameAssembly.dll | `33c56def…92c1` | เดียวกัน | ✓ |
| global-metadata.dat | `451311d4…bfd0` | เดียวกัน | ✓ |
| Mods\LongYinCoop.dll | `1b4c17aa…0177` (316,416 B) | เดียวกัน — **ไม่ deploy** | ✓ |
| Mods\LongYinCoop.pdb | `dcd78ebd…1e17` | เดียวกัน | ✓ |
| MelonPreferences.cfg | `269b571c…98d1` | เดียวกัน | ✓ |
| net.cfg | `12541f34…cc8` (444 B) | เดียวกัน | ✓ |
| Save (tool digest) | `df8e5a11beed` | `df8e5a11beed` + EXACT 57/57 ตาม baseline `20261028-071340` | ✓ |
| เกมรัน? | ไม่ | ไม่ | ✓ |
| mod repo tracked | 0 modified | 0 modified | ✓ |
| game repo M/D เดิม | 4 รายการ | 4 รายการ (ไม่แตะ) | ✓ |

## สิ่งที่ยังไม่รู้ / UNKNOWN

1. **พฤติกรรมรันจริงของ `Apply(false)` ยังไม่เคยพิสูจน์บนเกม** — C1 เป็นการอ่านซอร์ส (RUNTIME-พฤติกรรมจากซอร์ส); การตรวจซ้ำจริง = RUNBOOK ขั้น 6 (ML log ไม่มีบรรทัด SP5/Sp5T0 install/patch)
2. **DLL ใหม่ยังไม่เคยรันบนเกม** — G0/E1–E8 ทั้งหมด = NOT-RUN จนกว่า GO-3 (โครงสร้าง overlay/decoder เทสต์ offline ครบ 596/0 แล้ว)
3. deterministic rebuild ของ artifact ไม่ได้ทำซ้ำ (build ครั้งเดียว — ต่างจากยุค 1b4c17aa ที่ยืนยัน byte-identical) — ถ้าต้องการหลักฐาน reproducibility รอ build ซ้ำอีกครั้งใน GO-3 หรือสั่งแยก
4. ค้างเดิมที่ยังไม่แตะ: `S2-plan.md §13` rev2, VPN/เครื่อง guest/ศึก 60 ยูนิต, tag `stage-S2a-3` + `stage-S2a-4` (รอมติขั้นสุดท้าย), แพ็กเกจ V2-A/SP5-T0/V1, hardening เพิ่มเติม

## ข้อผิดพลาดกระบวนการ (รายงานตรง ๆ)

- ข้อความใน AGENTS.md ที่ append ครั้งแรกเขียนสายโซ่ commit คลุมเครือ ("`8ea8a90` → `d4fdf12`…") — ตรวจพบเองและแก้ใน working tree **ก่อน** commit (สายโซ่ถูกต้องใน commit `36f1150` เดียว) · ไม่มีการแก้ไฟล์ commit แล้ว
- shell chain ตรวจปลายทางครั้งแรกจบ exit 1 เพราะ `grep -v` ไม่เจอ tracked changes (exit 1 ของ grep เมื่อไม่มี match) — ตรวจซ้ำด้วย `grep -vc` = 0 บรรทัด ยืนยันสะอาด · ไม่มีผลต่อระบบ
- ไม่มีข้อผิดพลาดอื่น · ไม่มี incident (เกมไม่ถูกเปิด, ไม่ deploy, ไม่แตะ net.cfg/MelonPreferences/เซฟ, ไม่มี IPC)

## ขั้นถัดไป (ผู้ใช้)

1. พิมพ์ **`เริ่ม GO-3 session`** — agent ทำตาม RUNBOOK-G3 ขั้น 0–9 (session-start ใหม่ → ตรวจ artifact → สำรอง `.prev-1b4c17aa` + deploy → net.cfg overlay=true → split ด้วย `--expected-baseline coop-test\baselines\20261028-071340\savehash.real.before.txt` + preflight → **ผู้ใช้เปิดเกมเอง** → G0 → E6 → E1–E8 → ปิด session restore + คืน net.cfg → รายงานราย E — ไม่ tag ไม่ปิด stage)
2. แนบมาในข้อความเดียวกัน: มติ G-3 (rollback หรือคง DLL ใหม่) ตัดสินได้ท้าย session, G-4 (อนุมัติวลี) — ตาม APPROVAL-REQUESTS แพ็กเกจ 1
3. ค้างอื่นตามลำดับแนะนำ: V2-A → SP5-T0 → V1 (ห้ามรวมเซสชันเกมเดียวกับ GO-3)
