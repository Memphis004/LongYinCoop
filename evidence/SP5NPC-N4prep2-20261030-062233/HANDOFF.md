# HANDOFF — offline session 2026-10-30 06:03–06:2x (N4-follow-up: rebaseline + โค้ด + provenance)

## ลำดับที่ทำ (มติ 2 ข้อความ — รวม session หยุด + ต่อ)

1. session-start รอบแรก (06:03) พบ snapshot ไม่ตรง 2 จุด ⇒ **หยุดตามเงื่อนไข** + วินิจฉัย read-only
   → `evidence/AUTO-20261030-060328/incident-game-open-0545.md` (ผู้ใช้เปิดเกมเอง 05:45:05–05:46:28 ·
   56/57 EXACT เปลี่ยนเฉพาะ `PlayerprefData.dat` · ML เขียน cfg ตอนปิด)
2. มติผู้ใช้ (ข้อความถัดมา): ยืนยันเปิดเกมเอง = ใช่ · อนุมัติทาง (ก) **rebaseline volatile-only** —
   ทำแล้ว: baseline ใหม่ **`20261030-060854`** digest **`7e1e65e52659e878650b206e43322692879aad9750d047091f34450e94830fa6`**
   (`REBASELINE.json` volatile_only=true, changed=[PlayerprefData.dat]) · ยืนยันก่อนทำ 2 ครั้ง (05:xx/06:07) ตรงทั้งคู่ ·
   **cfg divergence `976ccfb9… → 733d9a0c…` บันทึก VERSION.md แล้ว** (ค่า key false ครบ — Sp5T0/TimeV2a/AllowRemoteInput) ·
   anchor เซสชันต่อไป: เซฟ `7e1e65e52659…` + cfg `733d9a0c…`
3. **งานโค้ดตามมติ N4-follow-up** (ร่างอนุมัติทั้ง 2 — รายละเอียดครบใน provenance โฟลเดอร์นี้):
   `Sp5T0NameMatch.cs` ใหม่ (pure) · `Sp5T0Probe.cs` (listMatch + heroNum.window one-shot ณ
   GenDataC call แรก + heroNum block ที่ sng.end + แก้ comment เดิม + heroCount คงเก็บเป็น
   อ้างอิง) · เทสต์ 12 checks + ลงทะเบียน + ลิงก์ csproj
4. **build + เทสต์:** 0W/0E ×2 determinism · artifact DLL **`476f390e…`** (377,344 B) / PDB
   `e8c44e4f…` (104,744 B) — **ไม่ deploy** (`Mods\` = `6a94abb5…` ตรงตลอด) · C# **705 PASS/0 FAIL**
   (693+12) · python 53+110 OK · static CLEAN · audit tool รูปเดิม PASS + synthetic รูปใหม่ PASS
5. **อัปเดตเอกสาร:** คำขอ N4 (append ที่ 3 — deploy `476f390e…` แทน `ba1e1dd1…`, เกณฑ์ T2 ใหม่,
   BirthSetting ฐานรอบ 1, รอบ 2–3 ต้องรันครบ) · decisions-pending.md (append, คง untracked)

## commits (อนุมัติ แยก 2 อัน — path-scoped, ไม่ push)

- commit 1 (โค้ด+provenance): `src/LongYinCoop/SP5/Sp5T0Probe.cs`, `src/LongYinCoop/SP5/Sp5T0NameMatch.cs`,
  `tests/S2Protocol.Tests/Sp5T0NameMatchTests.cs`, `tests/S2Protocol.Tests/Program.cs`,
  `tests/S2Protocol.Tests/S2Protocol.Tests.csproj`, `evidence/SP5NPC-N4prep2-20261030-062233/provenance.md`,
  `evidence/SP5NPC-N4prep2-20261030-062233/hashes.txt`
- commit 2 (เอกสาร): `plans/sp5-npc-n4-followup-draft.md` (สถานะอนุมัติ — append),
  `plans/sp5-npc-n4-approval-request.md` (append ที่ 3), `VERSION.md` (baseline/cfg ใหม่)
- **ไม่ push** · decisions-pending.md คง untracked · ndjson/dll/pdb/log โดน gitignore ห้าม force-add

## สถานะปลายเซสชัน (ตรวจด้วย hash ก่อนรายงาน)

- HEAD = 772c8f2 + 2 commits ใหม่ · ไม่มี tag · เซฟ `7e1e65e52659…` (ไม่แตะ) · cfg `733d9a0c…` (ไม่แตะ) ·
  net.cfg `12541f34…` · `Mods\` = `6a94abb5…` · เกม 2 ไฟล์ = `33c56def…`/`451311d4…` · เกมไม่รัน

## ค้างตัดสิน (ผู้ใช้)

- วลีสั่ง N4 รอบ 2: deploy `476f390e…` (สำรอง `.prev-6a94abb5` ก่อน) + split baseline
  `20261030-060854` + net.cfg สองชั้น (enabled=true + npcPolicy=light) + Sp5T0=true + เปิดเกม ×2
  + ตรวจ T2 ใหม่ (listPresent=57 — รอบแรก = ยืนยันกติกา match) / heroNum.window Σ=50 /
  sng.end Σ=88 / T3 f2=0 / T4 band [280,350] + BirthSetting ตรงฐานรอบ 1 → คืนสภาพครบท้ายเซสชัน
- อื่น ๆ ตาม decisions-pending.md หัวข้อ 8
