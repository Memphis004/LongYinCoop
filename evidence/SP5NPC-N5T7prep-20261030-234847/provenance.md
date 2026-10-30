# provenance — SP5NPC-N5T7prep (T7 ทาง (ข) ตามมติผู้ใช้ 2026-10-30 · offline เท่านั้น ไม่ deploy)

- **ขอบเขต:** stage N5 ทางวัด T7 = (ข) เพิ่ม census field ใน probe — log-only อ่านอย่างเดียว
  ห้ามเปลี่ยนตรรกะ gate/hook อื่น ห้าม raw offset · **ไม่ deploy ไม่เปิดเกม ไม่ split ไม่แตะ
  Mods\ / net.cfg / เซฟจริง** · **ยังไม่ rebaseline ยังไม่เริ่ม N5 runtime** (ตามมติครบทุกข้อ)
- **session-start (23:2x):** เกมไม่รัน · hash เกมตรง VERSION.md ทั้งสองไฟล์ (`33c56def…7920c1`
  / `451311d4…18bfd0`) · เซฟ `Save` 57 ไฟล์ digest `d3f7cb2fcae2` **ตรง anchor ที่ผู้ใช้ให้มาเป๊ะ**
  (`save-split status` read-only · leftovers: none) · mod repo HEAD `0c3e5fe` (tree มี
  `M decisions-pending.md` + `?? evidence/AUTO-20261030-071500/` ของ session ขนาน — **ไม่แตะ
  ไม่ commit**) · game repo HEAD `944ceda`

## สิ่งที่เปลี่ยนใน repo (code)

| ไฟล์ | สถานะ | เนื้อหา |
|---|---|---|
| `src/LongYinCoop/SP5/Sp5T0T7Census.cs` | ใหม่ | pure (ไม่แตะ Il2Cpp/Unity) — dict passthrough · heroID dedup (DupIds ครั้งเดียวต่อ id, cap 16 + truncated, ตัวนับเต็มแยก) · leader-exists (leader==0 ไม่นับ · missing ตัวนับเต็ม) · ownHeros dangling + perForce (ช่องอ่านไม่ได้ = -1) — ไม่โยน ทุก input null = สถานะ "อ่านไม่ได้" |
| `src/LongYinCoop/SP5/Sp5T0Probe.cs` | แก้ 4 จุด | header doc + สถานะ `_lastT7*` 8 ตัว + บล็อก `t7` ใน `SnapshotWorld` (อ่าน typed: `WorldData.HerosDict`/`TempHerosDict` `.Count` · heroID loop แยก · ownHeros คัดลอกต่อสำนัก · leaderIds **ส่งต่อ**จากบล็อก T2 ไม่อ่านซ้ำ — JSON ประกอบใน StringBuilder แยกก่อนแนบ กัน JSON ครึ่งบรรทัด) + `summary.json` เพิ่ม `t7Last` |
| `tests/S2Protocol.Tests/Sp5T0T7CensusTests.cs` | ใหม่ | 24 เคส pure (โลกสะอาด/dedup/cap/leader/dangling/perForce/id 0/null ทุกตัว) |
| `tests/S2Protocol.Tests/Program.cs` | แก้ | register `Sp5T0T7CensusTests.Run()` |
| `tests/S2Protocol.Tests/S2Protocol.Tests.csproj` | แก้ | ลิงก์ `Sp5T0T7Census.cs` (Sp5T0Probe.cs ยังไม่ลิงก์ — Il2Cpp/Harmony ตาม convention) |

- **ที่มา field (DUMP — `Il2CppDumper/dump.cs`):** `WorldData` (TypeDefIndex 8181) `HerosDict`
  0x268 / `TempHerosDict` 0x278 · `ForceData` (8230) `leader` 0x58 / `ownHeros` 0x70 — **offset
  ใช้สำรวจเท่านั้น โค้ดอ่านผ่าน interop property ทั้งหมด** (typed, กติกา 8) · ยืนยันชื่อ property
  มีจริงใน interop `Assembly-CSharp.dll` (binary grep พบครบ 3 ชื่อ) ก่อนเขียนโค้ด
- **ขอบเขต log-only คงเดิม:** ไม่มี hook ใหม่ ไม่แตะ Harmony/gate/ตัวนับ/การจำแนกเดิมใด ๆ —
  บล็อก T7 อ่านแล้ว append JSON ลงบรรทัด worldcount เดิมเท่านั้น · exception ทุกชั้นกลืนตาม
  convention (readfail นับเพิ่ม · `t7.readfail:"exception"` แยกบรรทัดเดิมไม่พัง)

## ผลวัด

| รายการ | ผล | เทียบเกณฑ์มติ |
|---|---|---|
| C# suite (`dotnet run --project tests/S2Protocol.Tests`) | **729 PASS / 0 FAIL** (RESULT PASS) | ≥705 ✓ (+24 จาก T7) |
| python `unittest test_negative_preconditions` | **Ran 53 · OK** | = เดิม ✓ (บรรทัด "ABORTED/FAILED pass=0" = stdout negative-path เอง ตาม N2/N4prep provenance) |
| python `unittest discover` (tools) | **Ran 110 · OK** | = เดิม ✓ |
| python `test_preflight_launch.py` | **14/14 PASS** | เสริม (fixture tempdir ล้วน — ไม่แตะ path จริง) |
| `python tools/s2_static_check.py` | **CLEAN (0 violations)** exit 0 | CLEAN ✓ |
| build Release ครั้งที่ 1 (`--no-incremental`) | Build succeeded · **0 Warning / 0 Error** | — |
| build Release ครั้งที่ 2 (`--no-incremental`) | Build succeeded · **0 Warning / 0 Error** | — |
| determinism ×2 | DLL+PDB hash **IDENTICAL สองรอบ** | ✓ |

## artifact (ไม่ deploy)

- `LongYinCoop.dll` sha256 **`c58434b130b405bf81d7f63590fc36a30ab489b45a0a00a2f96d37399fe9df31`**
- `LongYinCoop.pdb` sha256 **`7d7f291ebf474f8edfd7ffeb169046a2892a4c06e7a0231aff8a5e0a4c3ea046`**
- build จาก: dotnet SDK **9.0.311** · ต้นไม้ที่ commit code ของ session นี้ (src เดียวกันทั้งสองรอบ)
- ตำแหน่ง: `src/LongYinCoop/bin/Release/` + สำเนาใน folder นี้ (bin และ `*.dll` ถูก gitignore —
  ไฟล์คงอยู่ local ตาม convention "binary drops inside evidence stay local")
- **`Mods\LongYinCoop.dll` ยังเป็น `6a94abb5…` ไม่ถูกแตะ** — deploy เกิดเฉพาะในวลีสั่ง N5
  (ขั้น 3 ของคำขอ จะใช้ artifact นี้แทน `476f390e…` — ให้ระบุในวลีสั่ง)

## deviation / หมายเหตุ

- ไม่มี deviation จากมติ — ขอบเขต offline ครบ · ไม่แตะ `decisions-pending.md` (working-tree
  ของ session ขนาน) และไม่แตะ `evidence/AUTO-20261030-071500/` (HANDOFF ของ session ก่อน —
  ยัง untracked ตามที่ทิ้งไว้)
- VERSION.md ไม่แตะ (ไม่มี deploy ไม่มี drift ใหม่ — เซฟยัง anchor `d3f7cb2fcae2` ณ จบการตรวจ)
- เทสต์ C# รอบแรกพบ FAIL 1 ช่อง = **เคสทดสอบเขียน input ผิดจากชื่อเคส** (ส่ง array แทน null —
  helper ทำงานถูกต้อง) แก้เคสแล้ว rerun ผ่านทั้งหมด — บันทึกตามจริง ไม่ซ่อน
