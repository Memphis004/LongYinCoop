# provenance — SP5-NPC N4-follow-up prep: นิยาม T2 loveable ใหม่ + จุดอ่าน heroNum ใหม่ (มติ 2026-10-30 อนุมัติตามร่าง) — สถานะ: **READY · ไม่ deploy**

- label `SP5NPC-N4prep2-20261030-062233` · ร่างต้นทาง `plans/sp5-npc-n4-followup-draft.md`
  (อนุมัติทั้ง 2 ร่าง) · ก่อนหน้า: incident เกมถูกเปิดเอง 05:45 → rebaseline volatile-only
  (`evidence/AUTO-20261030-060328/incident-game-open-0545.md`, baseline ใหม่ `20261030-060854`)
- ขอบเขตที่อนุมัติ: "แก้ Sp5T0Probe.cs (log-only อ่านอย่างเดียว) ห้ามเปลี่ยนตรรกะ gate/hook อื่น ·
  ห้าม raw offset · แก้ comment เก่าให้ตรงความจริง · เก็บ heroCount ไว้เป็นข้อมูลอ้างอิง
  ไม่ใช้ตัดสิน"

## งานที่ทำ (3 ไฟล์: probe + pure helper + tests)

### 1. `src/LongYinCoop/SP5/Sp5T0NameMatch.cs` (ไฟล์ใหม่ — pure ไม่มี Il2Cpp/Unity)

- กติกา match ของ T2 ข้อ loveable: ชื่อใน list = present ก็ต่อเมื่อมี HeroData ≥1 ตัวที่
  `heroName` ตรง **หรือ** `heroFamilyName+heroName` ตรง (Ordinal) · missing ตามลำดับ list,
  cap 64 + `MissingTruncated` · ไม่โยน (list/set null = ทุกชื่อ missing)
- **กติกา match ยังไม่ RUNTIME-confirmed — รอบแรกที่ใช้บนเกม = การยืนยันกติกา** (มติ: log
  ผล match รายชื่อ, ชื่อที่ match ไม่ได้ต้อง log ออกมา; ถ้าไม่ตรงเพราะรูปแบบชื่อ = หยุดรายงาน)

### 2. `src/LongYinCoop/SP5/Sp5T0Probe.cs` (แก้ — log-only ทุกจุด)

| จุด | การเปลี่ยนแปลง |
|---|---|
| doc comment หัวคลาส | บันทึกนิยามใหม่ + จุดอ่านใหม่ (ร่าง 1+2) |
| statics | +`_lastLoveablePresent/_lastLoveableMissing` +`_heroNumWindowEmitted/_heroNumWindowSum/_heroNumPostRestoreSum` |
| comment ใน loop hist | `// LoveableSpeHero (T2)` เดิม → บันทึกความจริง "flag ≠ สมาชิก list (744/883, 631/632) — heroCount = ข้อมูลอ้างอิงเท่านั้น, T2 ตัดสินที่ listMatch" |
| loop ใหม่ (แยกจาก loop hist เดิม) | เก็บ set ชื่อ: `heroName` + `heroFamilyName+heroName` (Ordinal, per-item try/catch + readfail) |
| บล็อกอ่าน list | เดิมอ่านแค่ Count → เพิ่มเก็บชื่อทุก entry เข้า `loveNames` (typed) |
| JSON `loveable` | เดิม `{heroCount, listCount}` → เพิ่ม `{listPresent, listMissing[], listMissingTruncated}` — ชื่อที่ match ไม่ได้ถูก log ครบตามมติ (-1 = อ่านไม่ได้) |
| JSON worldcount ที่ `at=="sng.end"` | +บล็อก `"heroNum":{force[],sum,note}` — **post-restore จุดเดียว** (gate ไม่ patch StartNewGame; ห้ามใช้ genhero.end — probe postfix รันก่อน gate finalizer) · คาด Σ=88 · ล้ม = readfail + force:null |
| method ใหม่ `EmitHeroNumWindowOnce()` | event `heroNum.window` one-shot ต่อ session จาก `GenDataCPrefix` (หลัง Commit ของ factory.call) — ใน window ⇒ คาด Σ=50 · deterministic จาก nesting · one-shot guard ตั้งก่อน emit · ไม่แตะ `_lastHeroNumArrays/_lastSpeDb` |
| summary.json | censusLast เพิ่ม `listPresent`/`listMissingCount` + เพิ่ม `heroNumWindowSum`/`heroNumPostRestoreSum` (-1 = ยังไม่อ่าน) |
| **ไม่แตะ** | ตัวนับเดิมทั้งหมด (hist 8 คลาส, perForce, Classify, tempMatch, ovCalls/Results, linkage iid, patch.audit), `ReadHeroNum` เดิม (schema byte-identical), gate/Sp5Npc* ทุกไฟล์, hook set เดิม 7 ชุด (patch count ตรวจ `คาด 7` คงเดิม) |

### 3. เทสต์ (ไฟล์ใหม่ + ลงทะเบียน)

- `tests/S2Protocol.Tests/Sp5T0NameMatchTests.cs` (12 checks — pure: given/full hit, fallback,
  missing ตามลำดับ, null/empty, set ว่าง/null, list null, cap 64+truncated, 57 ชื่อจริง,
  ชื่อซ้ำราย entry, Ordinal, โครงสร้างผลลัพธ์) + `Program.cs` ลงทะเบียน +
  `S2Protocol.Tests.csproj` ลิงก์ `Sp5T0NameMatch.cs` (Sp5T0Probe.cs ไม่ลิงก์ — Il2Cpp ตาม convention)

## build + เทสต์ (exit code จริง)

| รายการ | ค่า |
|---|---|
| build | `dotnet build src/LongYinCoop -c Release --no-incremental` ×2 — **0 Warning / 0 Error ทั้งครั้ง** · **determinism ×2** (hash เท่ากันทุก byte) |
| **artifact** | DLL **`476f390e0993adcb4be797cf588e2b4eacb3c00ad6f044c7ffa323e8407ee868`** (377,344 B) · PDB **`e8c44e4ff790efda635b7ca7591de8f42be0a1cd8fff5bd8c62c804fea65da74`** (104,744 B) — สำเนาใน evidence โฟลเดอร์นี้ (**gitignored** — คง local · hashes.txt บันทึกครบ) |
| เทียบ build ก่อนหน้า | N4prep `ba1e1dd1…` (373,760 B) → ใหญ่ขึ้น 3,584 B จาก field ใหม่ · `Mods\` ยังเป็น `6a94abb5…` (**ไม่ deploy ตามมติ**) |
| C# suite | **705 PASS / 0 FAIL** (= เดิม 693 + name-match 12 · ทุกชุด RESULT PASS · exit 0) |
| python | **53 OK + discover 110 OK** (= เดิม · บรรทัด "FAILED pass=0…" = stdout ของเทสต์ negative-path เอง ตาม N2 provenance) |
| static | `s2_static_check.py` = **CLEAN (0 violations, exit 0)** |
| audit tool | `sp5t0_audit.py` บน ndjson จริง N4 รอบ 1 (รูปเดิม) = **VERDICT PASS (structural)** เหมือนเดิม · บน **synthetic รูปใหม่** (มี listMatch + heroNum block + heroNum.window — `evidence/AUTO-20261030-060328/synthetic-new-format.ndjson`) = **VERDICT PASS (structural)** ⇒ tool อ่านได้ทั้งรูปเดิม/รูปใหม่ |

## ข้อจำกัด / คง UNKNOWN

- กติกา match ชื่อ (heroName ตรง หรือ family+given) = **ยังไม่ RUNTIME-confirmed** — รอบแรกที่
  ใช้บนเกม = การยืนยันกติกา; ชื่อที่ match ไม่ได้จะถูก log เต็ม ๆ ใน `listMissing`
- `heroNum.window`/`heroNum` (sng.end) เป็น point-in-time read — ค่า = ตาราง ณ จุดตรวจ
- artifact นี้ **ไม่ deploy** — `Mods\LongYinCoop.dll` คง `6a94abb5…` (ยืนยัน hash ปลาย session)
- รอบแรกที่ใช้ artifact นี้ = N4 รอบ 2 (ตามมติ: รอบ 2–3 ต้องรันครบ, คำขอ N4 อัปเดตแล้ว)
