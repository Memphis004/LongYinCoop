# provenance — SP5-NPC stage N1 artifact (สถานะ: **READY — build ผ่าน 0W/0E · เทสต์ ≥ เดิมทุกชุด · ยังไม่ deploy**)

> stage N1 (มติผู้ใช้ 2026-10-30: อนุมัติเฉพาะ N1 offline ล้วน — N2–N6 ยังไม่อนุมัติ) ·
> label `SP5T0-N1-20261030-025854` · **`Mods\LongYinCoop.dll` ยังเป็น `6a94abb5…` ไม่ถูกแตะ**
> (ยืนยัน hash ปลาย session)

## งานที่ทำ (ตามแผน §2 — ทุกตัว offline)

| # | งาน | ผล |
|---|---|---|
| N1-1 | แก้บั๊ก comparator ใน `Sp5T0Probe.cs`: เพิ่ม `NormalizePatchId` (normalize `:`/`::` → `.`) + เทียบ expected แบบ format-insensitive | บั๊ก allMatch=false ทั้ง 4 จุดตรวจของรอบ T0 ปิด (actual `owner:Type::Method` เทียบ expected `owner:Type.Method` ตรงกันหลัง normalize) |
| N1-2 | แก้ `tools/sp5t0_audit.py` ให้ตรง schema จริง: `h` เล็ก · `perForce` = list 30 ช่อง (รองรับ dict เดิมด้วย) · skip world-null snapshot · **key ด้วย label `at` ไม่พึ่ง bucket จาก t** (worldcount เขียน t=0) · เพิ่ม `dH_observed`/`dT_observed` | tool รันกับ NDJSON จริงได้: E5/E7 CLEAN · dH=883 · dT=87 · tests 6→10 OK |
| N1-5 | เพิ่ม `src/LongYinCoop/SP5/Sp5NpcPolicy.cs` (pure: `Sp5NpcLevel` + `ParseValue` + `Decide`) + ต่อ key `npcPolicy` ใน `S2NetSettings`/`S2NetConfig.Parse` (default "off" · ค่าแปลกไม่ทำไฟล์พัง) + เทสต์ `Sp5NpcPolicyTests` (truth-table 80 ชุด + parser 17 checks) | fail-closed ครบ: เปิดเฉพาะ enabled+host+formatOk+policy รู้จัก |
| N1-3 | attribute ลูปจาก NDJSON เดิม (`n1-3-analyze.py` + `n1-3-loop-attribution.md` + `n1-3-output.txt`) | **A1 ครอบ 562/883 (63.6%) — quota รายสำนัก ราย band มาจากตาราง heroNum (25/30 fit [+0,+1] ≥5/6 band) · Light 0.5 คาดลด ~281 ตัว ≈ 31.8% (สมมติ linear — คง UNKNOWN จนวัดจริง)** |
| N1-4 | enumerate consumers ของ `forceLevelData` (`n1-4-consumers.md`) | writer เดียว = LoadAllGameData/CSV · reader ยืนยัน = GenerateHero · read-candidates unattributed 3 จุด = คง UNKNOWN (ไม่บล็อก A1) |

## build + เทสต์ (exit code จริง)

| รายการ | ค่า |
|---|---|
| คำสั่ง | `dotnet build src/LongYinCoop -c Release --no-incremental` · **0 Warning / 0 Error** |
| determinism | rebuild ×2 ที่ HEAD เดียวกัน = hash ตรงกันทุก byte |
| **artifact** | DLL **`b6d24d34a28de488aed569ba3b11d46f73ff21f099df5928c3fa348b245ca6ef`** (367,104 B) · PDB **`4632b607ff759d99fdda250e89969405ad4f4ac539e6b002e3758c26bf9174e7`** (99,844 B) — สำเนาใน evidence โฟลเดอร์นี้ (gitignored ตามนโยบาย) |
| เทียบ build ก่อนหน้า | prep `865b4a87` (366,592 B — ไม่ deploy แล้ว) · `Mods\` = `6a94abb5` (ไม่แตะ) |
| C# suite | **673 PASS / 0 FAIL** (เดิม 650 → +6 harness NormalizePatchId +17 npc policy) — harness log พิมพ์ sha256 ของ artifact ที่โหลด |
| python | `test_negative_preconditions` **53 OK** · `discover` **110 OK** (เดิม 106 → +4 test_sp5t0_audit) — บรรทัด "ABORTED/FAILED pass=0" ที่พบ = stdout ของเทสต์ negative-path เอง ไม่ใช่ failure |
| static | `s2_static_check.py` **CLEAN (0 violations)** |

## ข้อจำกัด / สิ่งที่ยังรอ

- **ยังไม่ deploy** — N2–N6 ยังไม่อนุมัติ (มติ 2026-10-30) · artifact นี้ deploy ได้เฉพาะเมื่อมีวลีสั่ง stage ถัดไป
- N1-3 สรุป linear เป็น**ข้อสันนิษฐาน** — ผลจริงระดับใดต้องวัด T4 (อนุมัติแยก)
- ผลรอบนี้ไม่เปลี่ยน E1/E2 = UNKNOWN (rev 13) และไม่แก้ตรรกะ probe ใดนอกจาก normalize สายอักขระ
