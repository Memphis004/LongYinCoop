# HANDOFF — session `AUTO-20261029-013511` (2026-10-29 01:35 เริ่ม — ปิด ~02:1x)

> ไฟล์ส่งมอบบนดิสก์ **ไม่ commit** · คำสั่งต้นทาง: ข้อความผู้ใช้ 2026-10-29 (งาน "เตรียม V2-A ให้พร้อมรัน" —
> offline เท่านั้น) พร้อมมติ [V2A-1]..[V2A-5] ครบ · label = `AUTO-20261029-013511` (อ่านจากเครื่องจริง 01:35:11)

## สรุปผูกมัด 6 บรรทัด (อ่านก่อน)

1. **V2-A พร้อมรัน** — artifact READY: DLL `983b2384…6893` (366,080 B) + PDB `951de8fc…e7d6` (99,568 B) ที่
   `evidence/V2A-20261029-020816/` (provenance READY, source commit `211ea21…`, build 0W/0E exit 0, dotnet 9.0.311)
   · **ยังไม่ deploy — `Mods\LongYinCoop.dll` ยังเป็น `6a94abb5…` ตามมติ G-3 (ห้ามแตะในรอบนี้)**
2. **รีวิวแผน (Gate 0 มติ V2A-1)**: errata `evidence/AUTO-20261029-013511/time-plan-errata.md` — **BLOCKER 0 ·
   STALE 4 · NOTE 4** · **D9 (บรรทัด 255–256) ไม่ขัดมติ [V2A-2]** — หลักการตรงกันทุกข้อ สิ่งที่ล้าสมัยมีเฉพาะข้อเท็จจริง
   ยุค S2a-3 (baseline ปัจจุบัน = `df8e5a11beed…` @ `20261028-071340` ตามสายโซ่ที่อนุมัติแล้ว) ⇒ ไม่เข้าเงื่อนไขหยุด
3. **sampler ใหม่ (มติ V2A-4)**: `src/LongYinCoop/TimeV2a/{TimeV2aRules,TimeV2aSampler}.cs` — อ่านอย่างเดียว
   100% · typed interop เท่านั้น · ไม่มี Harmony/hook · ไม่มี write path เข้า state เกม · ไม่ยุ่ง SP5 (diff ว่าง
   นับแต่ `636c209`) · pref ใหม่ **`TimeV2a`** default false (ประกาศใน errata N1 ก่อนใช้) · เขียนไฟล์ได้เฉพาะ
   NDJSON ตาม pointer file `UserData\LongYinCoop\time-v2a.out` (fail-closed)
4. **เทสต์ (exit code จริงทุกคำสั่ง)**: C# **633 PASS / 0 FAIL** (596 เดิม + 37 ใหม่) · python 53 OK + discover
   68 OK · `s2_static_check.py` CLEAN · **B4: analyzer จริงอ่าน NDJSON ของ detector จริงได้ ไม่แก้ analyzer** —
   fixture N=5 → FORM2, day_ticks=5==N
5. **RUNBOOK-V2A พร้อม** (`evidence/V2A-20261029-020816/RUNBOOK-V2A.md`): ขั้น 0–8 ครบ — deploy ต้องสำรอง
   `.prev-6a94abb5` (ตรวจชื่อว่างก่อน) · split ต่อเคสด้วย `--expected-baseline …\20261028-071340\savehash.real.before.txt`
   · ตารางเคส A1–A4 ×3 รอบ (สำเนาแยกต่อรอบ กฎ 2 §D.0) · tolerance ต้องประกาศก่อนวัด (เสนอ 0.1 s) · MISMATCH = หยุด
   · ปิด session: restore EXACT + ตรวจค่า key ของ pref (ไม่ใช้ hash)
6. **ห้ามทั้งหมดถูกปฏิบัติครบ**: ไม่เปิดเกม/IPC · ไม่ deploy/swap ใน Mods\ · ไม่แก้ net.cfg/MelonPreferences/เซฟ ·
   ไม่ split/restore/rebaseline · ไม่ tag/push/amend/force/add -A · ทุก commit ระบุ path · ไม่ log secret

## commits ราย Phase (mod repo)

| Phase | commit | เนื้อหา (path) |
|---|---|---|
| A | `21e99a0` | errata — `evidence/AUTO-20261029-013511/time-plan-errata.md` เดียว |
| B1 | `b0ca9f7` | sampler — `src/LongYinCoop/TimeV2a/TimeV2aRules.cs` (ใหม่), `TimeV2aSampler.cs` (ใหม่), `CoopPrefs.cs`, `CoopMod.cs` |
| B2 | `91bf85c` | เทสต์ — `tests/S2Protocol.Tests/TimeV2aTests.cs` (ใหม่ 37 checks), `Program.cs` (CLI `--emit-v2a-fixture` + suite hook), `S2Protocol.Tests.csproj` (ลิงก์ TimeV2aRules.cs — **รายงานการเพิ่มไฟล์เข้า csproj ตาม B6**) |
| B4 | `211ea21` | หลักฐาน analyzer cross-check — `evidence/AUTO-20261029-013511/b4-analyzer-crosscheck.md` + `b4-analyze-result.txt` (fixture `.ndjson` ไม่ commit ตาม .gitignore:60) |
| C | `bd7885e` | provenance + RUNBOOK — `evidence/V2A-20261029-020816/{provenance,RUNBOOK-V2A}.md` + `evidence/AUTO-20261029-013511/c3_strings_heap_check.py` + `c3-strings-result.txt` |

- game repo **ไม่มี commit ใหม่** (HEAD คง `39c5403`) · ไม่มี tag ใหม่/ไม่ย้าย tag เดิม
- ก่อนเริ่ม session: mod HEAD `3ac0761` · หลังจบ: mod HEAD `bd7885e` (5 commits ข้างบน · src/ diff นับแต่
  `636c209` = เฉพาะ TimeV2a ใหม่ + wiring 2 ไฟล์)

## สิ่งที่ทำ ราย Phase

- **Phase A** — รีวิว `time-plan-draft.md` ทั้งฉบับ + เทียบข้อความ D9 กับมติ V2A-2 (อ้างบรรทัดตรง ๆ ใน errata) +
  ตรวจไฟล์อ้างอิง 11 รายการมีจริง + ตรวจ field ทุกตัวใน dump.cs (`WorldData.hour` = float 0xB4 · `TimeData`
  year/month/day = int) + ตรวจชื่อ member ใน #Strings heap ของ interop `Assembly-CSharp.dll` — เขียน errata (0 BLOCKER)
- **Phase B** — อ่าน idiom เดิม (CoopPrefs/CoopMod/S2Sampler/S2NdjsonWriter/S2NetConfig/WorldSampler/CaptureWriter)
  แล้วเขียน: (1) `TimeV2aRules.cs` pure — detector 5 event ตามสเปกแพ็กเกจ 2 (day_tick จาก leftWorkingDay ลดลง —
  ไม่มี hook), skipK ที่วัดได้ต่อ tick, gate fail-closed, writer buffered (flush ทุก 20 + Dispose flush, append)
  (2) `TimeV2aSampler.cs` — อ่าน `WorkingUIController` (8 fields + statics `dayTime/pauseTime`) + `WorldData.hour`
  ต่อเฟรมบน main thread, try/catch + counters (Events/Dropped/Errors) (3) เทสต์ 37 ข้อ + fixture CLI
  (4) รัน suite ครบ + B4 cross-check กับ analyzer จริง
- **Phase C** — Release build `--no-incremental` ณ HEAD → 0W/0E exit 0 → artifact + provenance + RUNBOOK +
  C3 metadata 12/12 + C4 no-op เชิงโค้ด (เลขบรรทัด)
- **Phase D** — hash ปลายทางตรงเริ่มทุกตัว (ตารางด้านล่าง) · HANDOFF นี้ (ไม่ commit)

## ผลวัดเทียบเกณฑ์

| ข้อ | เกณฑ์ | ผลจริง | สรุป |
|---|---|---|---|
| session-start | ตรงครบทุกข้อตามคำสั่ง | ตรงครบ (เกมไม่รัน · hash ตรง · HEAD/tags · เซฟ EXACT 57/57) | PASS |
| D9 vs มติ V2A-2 | ต้องอ้างข้อความจริง + ตัดสินว่าขัดหรือไม่ | อ้าง L255–256 ตรงตัว — **ไม่ขัด** (หลักการเดียวกัน, เฉพาะข้อเท็จจริงยุค S2a-3 ล้าสมัย) | PASS — ไม่หยุด |
| errata | รายการข้อ/บรรทัด/เดิม/ปัจจุบัน/ผล + BLOCKER ต้องหยุด | 4 STALE + 4 NOTE · **BLOCKER 0** | PASS |
| field ตามสเปก | dump.cs มีจริง + typed interop อ่านได้ | ครบทุกตัว (8 fields + statics 2 + hour float) · compile 0W/0E พิสูจน์ | PASS |
| เทสต์ C# | 596/0 + เทสต์ใหม่ | **633 PASS / 0 FAIL** (exit 0) | PASS |
| python | 68 OK | 53 OK + 68 OK (exit 0 ทั้งคู่) | PASS |
| static check | CLEAN | CLEAN 0 violations (exit 0) | PASS |
| B4 analyzer | ฟิลด์/ชนิดตรงกัน ไม่แก้ analyzer | FORM2, day_ticks=5==N, sessions=1 (exit 0) | PASS |
| C1 build | 0W/0E | **0 Warning / 0 Error, exit 0** (ณ `211ea21`) | PASS |
| C3 metadata | ชนิดใหม่ + S2a-4 ครบ | 12/12 FOUND (exit 0) | PASS |
| C4 no-op ที่ default | ไม่สร้าง Harmony/ไม่เปิดไฟล์/ไม่เปิดเธรด | หลักฐานเลขบรรทัดครบ (provenance §C4) — gate 2 ชั้น + fail-closed | PASS |
| hash ปลายทาง | ตรงตอนเริ่มทุกตัว | ตรงทุกตัว (ตารางด้านล่าง) | PASS |

## ปลายทาง (วัด 02:1x — ตรงเริ่มทุกตัว)

| รายการ | ค่า | ตรง |
|---|---|---|
| เกมรัน | ไม่ | ✓ |
| GameAssembly.dll / global-metadata.dat | `33c56def…` / `451311d4…` | ✓ |
| Mods DLL / PDB | `6a94abb5…` / `856d3bed…` (ไม่ deploy — ตามมติ V2A-5) | ✓ |
| `.prev` ทั้งคู่ | `1b4c17aa…` / `dcd78ebd…` | ✓ |
| MelonPreferences.cfg / net.cfg | `66fb92d5…` / `12541f34…` (444 B) — ไม่ถูกแตะ | ✓ |
| pointer file `time-v2a.out` | **ไม่มี** (ถูกต้อง — ยังไม่ควรมีจนกว่า "เริ่ม V2-A session") | ✓ |
| Save | EXACT 57/57 digest `df8e5a11beed…` + `leftovers: none` | ✓ |
| mod repo | HEAD `bd7885e` · tags `stage-S2a-2/3/4` ครบเดิม | ✓ |
| game repo | HEAD `39c5403` (ไม่เปลี่ยน) | ✓ |
| artifact | `evidence/V2A-20261029-020816/` DLL `983b2384…` + PDB `951de8fc…` (sha วัดซ้ำตรงต้นทาง) | ✓ |

## ข้อผิดพลาดกระบวนการ (รายงานตรง ๆ)

- **เทสต์รอบแรก FAIL 2 ข้อ (แก้แล้วก่อน commit)** — (1) เทสต์ "detector ใช้ซ้ำ" เขียนผิดเอง (ทับตัวแปร ev2
  ด้วย assignment ทำให้นับเฉพาะ session ที่สอง) (2) เทสต์ "statics ขาด" เผยพฤติกรรมที่ไม่ตั้งใจของ detector:
  ปล่อย `day_tick` ทั้งที่ไม่มี session เปิด (statics ขาด) — **แก้ที่ detector ไม่ใช่เทสต์**: เพิ่ม `_sessionOpen`
  ให้ event ทั้งหมดอยู่ใต้ work_start ที่พก statics ครบเท่านั้น (สมเหตุผล: ไม่ปล่อย event ที่วิเคราะห์ไม่ได้) ·
  รันซ้ำ = 633/0 exit 0 · ไม่มีสถานะใดค้างจากเหตุการณ์นี้
- ไม่มีข้อผิดพลาดอื่น · ไม่มี orphan process · ไม่มีไฟล์ค้างในโฟลเดอร์เกม (pointer file ไม่ถูกสร้าง)

## UNKNOWN / ค้าง

- **พฤติกรรมจริงของ statics `dayTime/pauseTime` ผ่าน interop ณ runtime ยังไม่เคยวัด** (compile ผ่าน + ชื่ออยู่ใน
  #Strings heap แล้ว แต่การอ่านค่าจริงในเกม = ของ "เริ่ม V2-A session") — ถ้าอ่านไม่ได้ sampler จะไม่ปล่อย
  work_start (fail-closed) และวัดชุด A ไม่ได้ = หยุดรายงานตามเงื่อนไข
- ลำดับเฟรมระหว่าง `MelonEvents.OnUpdate` ของ mod กับ `WorkingUIController.Update` ของเกมไม่รับประกัน —
  ผลคือความหยาบ 1 เฟรมของ `skipK` (เทสต์ครอบด้วย tolerance ±0.05) ไม่กระทบการตรวจจับ event
- tolerance ของ analyzer (เสนอ 0.1 s) **ยังต้องให้ผู้ใช้ยืนยันตอนสั่ง "เริ่ม V2-A session"** — ไม่ตัดสินเอง (errata N4)
- ค้างเดิมทั้งหมดคงเดิม: `S2-plan.md §13` rev2 · VPN/เครื่อง guest/ศึก 60 ยูนิต (Q9) · E4 วัดซ้ำความละเอียดสูง ·
  hardening เพิ่มเติม

## ขั้นถัดไปที่เสนอ (ผู้ใช้)

1. พิมพ์ **`เริ่ม V2-A session`** พร้อมแนบ: มติ V2A-1..3 + D9 (มีอยู่แล้วในคำสั่งเตรียม — อ้างซ้ำได้) + **ค่า
   tolerance ที่ยืนยัน** (เสนอ 0.1 s) · agent จะทำตาม `evidence/V2A-20261029-020816/RUNBOOK-V2A.md` ขั้น 0–8
   · ต้องแนบค่าจริง: ชื่อ artifact = `evidence/V2A-20261029-020816/` (DLL `983b2384…`), provenance @ commit
   `bd7885e`, ชื่อ pref = `TimeV2a`, baseline = `coop-test/baselines/20261028-071340/savehash.real.before.txt`
2. หลัง V2-A จบ: SP5-T0 → V1 (แยกเซสชันเกม ตามลำดับ APPROVAL-REQUESTS)
3. push ยังไม่ทำ (ห้ามในคำสั่ง) — ถ้าต้องการ push = สั่งแยก
