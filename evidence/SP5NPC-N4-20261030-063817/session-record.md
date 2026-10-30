# session record — SP5-NPC stage N4 รอบ 2–3 (T2–T4 ต่อจากรอบ 1, artifact `476f390e…`) — เริ่ม 2026-10-30 06:38 +0700

## มติ/อนุมัติที่รับ

- วลีสั่งผู้ใช้: **`เริ่ม N4 session`** (รอบที่สองของ stage N4 — ตามคำขอ N4 append ที่ 3) ·
  deploy artifact ใหม่ **`476f390e…` (PDB `e8c44e4f…`)** จาก `evidence/SP5NPC-N4prep2-20261030-062233/`
  แทน `ba1e1dd1…` · baseline เซฟใหม่ `coop-test/baselines/20261030-060854` (digest
  `7e1e65e52659…`) + cfg anchor `733d9a0c…` · **จำนวนรอบ: มติเขียน "[3 / 2 ตามคำขอ N4 ที่ append]"
  — ทั้งสองค่าชี้ผลเดียวกัน = รันรอบ 2–3 (ครบ 3 รอบรวมรอบ 1)** · band T4 [280,350] (รอบ 1 = 311) ·
  T2 = spe 170 / protag 1 / 掌门 30/30 / Teacher 1 อาจารย์/สำนัก / **loveable ใหม่ = listCount=57
  และ listPresent=57** (หายตัวไหน log รายชื่อแล้วหยุดรายงาน) / BirthSetting เทียบฐานรอบ 1
  (`count=6, vals=[-1,0,0,0,0,0]`) · **heroNum: window Σ=50 และ sng.end Σ=88 — ไม่ตรง = หยุดรายงาน** ·
  npcPolicy=light + enabled=true เฉพาะช่วงรัน, Sp5T0=true, คืน net.cfg `12541f34…` เป๊ะท้ายเซสชัน ·
  G1 typed write ล้ม = ห้าม raw offset หยุดรายงาน · N5–N6 ไม่อนุมัติ ไม่ปิด stage ไม่ tag ไม่ push ·
  จบเซสชัน: restore เซฟ EXACT ที่ baseline ใหม่ + rollback DLL `6a94abb5…` + net.cfg คืน + Sp5T0=false

## session-start checks (read-only, 06:38:02) — ผ่านครบ (snapshot ใหม่)

| รายการ | ค่าที่วัด | เทียบ |
|---|---|---|
| เกม | not running | ✅ |
| `GameAssembly.dll` / `global-metadata.dat` | `33c56def…` / `451311d4…` | ✅ VERSION.md |
| `Mods\LongYinCoop.dll` / `.pdb` (ก่อน deploy) | `6a94abb5…` / `856d3bed…` | ✅ มติ rollback |
| `MelonPreferences.cfg` (ก่อนแก้) | `733d9a0c…` (`Sp5T0=false` บรรทัด 15) | ✅ anchor ใหม่ |
| `net.cfg` (ก่อนแก้) | `12541f34…` — ไม่มี `npcPolicy` | ✅ |
| Save | 57 ไฟล์ digest `7e1e65e52659` · `leftovers: none` | ✅ baseline ใหม่ `20261030-060854` |
| mod repo | HEAD `fdc70ec` (772c8f2 + 2 commits N4-follow-up) · working tree 20 รายการ = ชุดเดิม | ✅ |
| game repo | HEAD `944ceda` | ✅ |

## ขั้น 2 — deploy (06:38)

- สำรอง: `Mods\LongYinCoop.dll.prev-6a94abb5-20261030-063817` = `6a94abb5…` ✅ ·
  `.pdb.prev-856d3bed-20261030-063817` = `856d3bed…` ✅
- คัดลอกจาก `evidence/SP5NPC-N4prep2-20261030-062233/` → rehash: DLL =
  **`476f390e0993adcb4be797cf588e2b4eacb3c00ad6f044c7ffa323e8407ee868`** (377,344 B) ✅ ·
  PDB = **`e8c44e4ff790efda635b7ca7591de8f42be0a1cd8fff5bd8c62c804fea65da74`** (104,744 B) ✅

## ขั้น 3 — net.cfg + pref (06:38)

- `net.cfg`: สำรอง `bak-n4r2-20261030-063817` = `12541f34…` ✅ → `enabled=true` + append
  `npcPolicy = light` → hash **`992c71b8…`** (ตรงสถานะช่วงรันของรอบ 1 เป๊ะ — แก้ deterministic)
- `MelonPreferences.cfg` บรรทัด 15: `Sp5T0 = true` → **`5a9d4280…`** (ตรงสถานะช่วงทดสอบเดิม)

## ขั้น 4 — save-split (06:39)

```
split --label SP5NPC-N4-20261030-063817 --expected-baseline coop-test/baselines/20261030-060854/savehash.real.before.txt
  Save vs baseline: changed=0 missing=0 added=0
  copy vs real:     changed=0 missing=0 added=0
OK split SP5NPC-N4-20261030-063817: 57 files, real kept as Save.real-SP5NPC-N4-20261030-063817
```

## คาดหมายก่อนเปิดเกม (ต่อรอบ ×2 — รอบ 2 และ 3)

- ML log: `Sp5T0 installed … patches=7` + `Sp5Npc gate installed` · ตอนปิด `Sp5T0 removed: …
  outside=0 ownedLeft=0 errors=0` + `probe: unpatched` · `[ERROR]`=0 · บรรทัด `[S2]` = ตามที่
  enabled=true อนุมัติ (self-test ในตัว)
- **T2 (zero-tolerance):** spe=170 · protag=1 · 掌门 30/30 · Teacher (membersWithTeacher ==
  eqLeader, distinct = ชุด leaderIds) · **`loveable`: listCount=57 และ listPresent=57 — ถ้า
  `listMissing` ไม่ว่าง = หยุดรายงานรายชื่อ** (รอบ 2 = การยืนยันกติกา match ครั้งแรก) ·
  BirthSetting = `count=6, vals=[-1,0,0,0,0,0]` (ผู้ใช้สร้างโลกด้วย setting เดิมรอบ 1)
- **heroNum (มติ: ไม่ตรง = หยุด):** `heroNum.window` (event ใหม่, one-shot ณ GenDataC call แรก)
  **Σ=50** + ตารางย่อ `[2,2,2,2,1,1]/[3,2,2,2,2,1]/[3,3,2,2,2,1]/[3,3,3,2,2,2]` · worldcount ที่
  `sng.end` บล็อก `heroNum` **Σ=88** (post-restore — มติ: ไม่ restore = หยุด)
- **T3:** f2=0 + ไม่มี error · **T4:** sect_after ∈ [280,350] · perForce รายสำนัก · h นิ่ง ·
  factory C ≈ Heros+TempHeros−1 · A/B=0 — หลุด band = รายงานรายสำนักแล้วหยุด
- เซฟ: `SaveSlot*` ห้ามถูกเขียน · drift คาดหมาย = `PlayerprefData.dat` เท่านั้น
- **หยุดทันที** ถ้า: hash ไม่ตรง snapshot ใหม่ · gate install ล้ม · patch นอกชุด · `SaveSlot*`
  ถูกเขียน · PROTECTED ขาด · heroNum ไม่ restore · การแก้จาก session อื่น

## รอบเปิดเกม (ผู้ใช้เปิดเอง ×2 — โลกใหม่ทุกรอบ ห้ามเซฟ slot · BirthSetting เดิมรอบ 1)

| รอบ | เปิด/ปิด (ML log) | ตรวจหลังปิด | ผล |
|---|---|---|---|
| 2 | `26-10-30_6-41-45.log` (เปิด 06:41:45 ปิด ~06:42:56 · ~71 s) | ✅ ผ่านทุกข้อ (ด้านล่าง) | **PASS ทุกเกณฑ์** |
| 3 | `26-10-30_6-45-42.log` (เปิด 06:45:42 ปิด ~06:46:31 · ~49 s) | ✅ ผ่านทุกข้อ (เท่ารอบ 2) | **PASS ทุกเกณฑ์** |

## ผลตรวจรอบ 2 (06:43–06:44) — ndjson `20261030-064149`

- **ML log:** `Sp5T0 installed … patches=7 (before=0)` + `Sp5Npc gate installed` (06:41:49) ·
  ปิด: `Sp5T0 removed: events=2254 errors=0 unattributed=0 outside=0 phaseStuck=0 rebaseline=0
  readfail=0 ownedLeft=0` + `probe: unpatched (before=1 after=0)` · **`[ERROR]`=0**
- **heroNum สองจุดใหม่ (มติ: ไม่ตรง = หยุด) — ตรงทั้งคู่:**
  - `heroNum.window` (one-shot ณ GenDataC call แรก): **Σ=50** · force =
    **`[[2,2,2,2,1,1],[3,2,2,2,2,1],[3,3,2,2,2,1],[3,3,3,2,2,2]]`** ตรงตารางย่อที่คาดเป๊ะ ✅
  - worldcount `sng.end` บล็อก `heroNum` (post-restore): **Σ=88** · force = ตารางต้นฉบับ
    `[[4,4,3,3,2,1],[5,4,4,3,3,2],[5,5,4,4,3,2],[6,5,5,4,4,3]]` ✅ ⇒ **restore ภายในรอบวัดตรงครั้งแรก**
- **T2 รายค่า:**
  - spe=170 (hist + speDb=170) ✅ · protag=1 ✅ (first factory.call = heroID 1/force 25 เท่าฐาน —
    protag นอก factory)
  - 掌门 **30/30** ✅ · Teacher สถานะสุดท้าย **311/311, distinct 30 = ชุด leaderIds** ✅ (timing
    เดิม: genhero.end ยัง 311/0 — สมบูรณ์ตอน genenemy.begin เหมือนรอบ 1)
  - **loveable ใหม่: `listCount=57` และ `listPresent=57` · `listMissing=[]`** ✅✅ ⇒ **กติกา match
    ชื่อถูกยืนยันบน runtime ครั้งแรก (57/57 ชื่อเจอใน Heros)** · `heroCount=631` (อ้างอิงเท่านั้น —
    ตรงรอบ 1)
  - BirthSetting **`count=6, vals=[-1,0,0,0,0,0]`** ตรงฐานรอบ 1 ทุกค่า ✅
- **T3: f2=0** ✅ · errors=0 · readfail=0 · phase ปกติ
- **T4:** sect_after = **311 — ใน band [280,350]** ✅ (เท่ารอบ 1 เป๊ะ) · perForce
  `[10,11,12,12,12,12,11,11,11,13,13,12,11,11,11,11,12,10,12,11,11,9,9,9,4,3,9,11,8,9]` **เท่ารอบ 1
  ทุกสำนัก** (Σ=311 — census โควตา deterministic ตรงโมเดล) · h=632 นิ่ง genhero.end→sng.end ✅ ·
  factory C=716 (A/B=0) ≈ 632+85−1 ✅ · TempHeros=85 (RNG — ช่วงเดิม 79–88)
- **เซฟ verify PASS:** drift เฉพาะ `PlayerprefData.dat` · **`SaveSlot*` ไม่ถูกเขียน** · real 0/0/0
  + mtime drift 0
- **hash หลังรอบ:** เกม `33c56def…` · Mods DLL `476f390e…` ตรงเดิมทุก byte
- หลักฐานรอบ: `ml-log-round2.log`, `summary-round2.json`, `sp5t0-round2.ndjson` (โฟลเดอร์นี้ ·
  ต้นฉบับ `UserData\LongYinCoop\SP5T0\20261030-064149\`)

## ผลตรวจรอบ 3 (06:47) — ndjson `20261030-064545`

- **ML log:** `Sp5T0 installed … patches=7 (before=0)` + `Sp5Npc gate installed` (06:45:46) ·
  ปิด: `Sp5T0 removed: events=2270 errors=0 unattributed=0 outside=0 phaseStuck=0 rebaseline=0
  readfail=0 ownedLeft=0` + `probe: unpatched (before=1 after=0)` · **`[ERROR]`=0**
- **heroNum สองจุดใหม่ — ตรงทั้งคู่ (รอบที่ 2 ติดกัน):** `heroNum.window` **Σ=50** ตารางย่อ
  `[[2,2,2,2,1,1],[3,2,2,2,2,1],[3,3,2,2,2,1],[3,3,3,2,2,2]]` ✅ · `sng.end` **Σ=88** ✅
- **T2:** spe=170 ✅ · protag=1 ✅ · 掌门 **30/30** ✅ · Teacher **311/311, distinct 30** ✅ ·
  **loveable `listCount=57` + `listPresent=57` · `listMissing=[]`** ✅ (ยืนยันกติกา match ครั้งที่ 2) ·
  BirthSetting `[-1,0,0,0,0,0]` ตรงฐาน ✅ · `heroCount=631` (อ้างอิง — ตรงรอบ 1–2)
- **T3: f2=0** ✅ · errors=0 · readfail=0
- **T4:** sect=**311** ใน band ✅ · perForce **เท่ารอบ 1–2 ทุกสำนัก** (Σ=311) · h=632 นิ่ง ✅ ·
  factory C=720 (A/B=0) ≈ 632+89−1 ✅ · TempHeros=89 (RNG — ช่วงใหม่สุด 79–89)
- **เซฟ verify PASS:** drift เฉพาะ `PlayerprefData.dat` · `SaveSlot*` ไม่ถูกเขียน · real 0/0/0 ·
  hash เกม `33c56def…` + Mods `476f390e…` ตรงเดิม
- หลักฐานรอบ: `ml-log-round3.log`, `summary-round3.json`, `sp5t0-round3.ndjson` (ต้นฉบับ
  `UserData\LongYinCoop\SP5T0\20261030-064545\`)

## ขั้นคืนสภาพ (06:47 — ตามมติจบเซสชัน)

1. **restore เซฟ:** `save-split restore --label SP5NPC-N4-20261030-063817` — `Save` = โฟลเดอร์จริง
   renamed กลับ (ไม่เคยถูกเขียน), **restored vs real baseline = 0/0/0 EXACT** (baseline ใหม่
   `20261030-060854`), สำเนาทดสอบเก็บ `coop-test/SP5NPC-N4-20261030-063817/Save.used-20261030-064747` ·
   หลัง restore: 57 ไฟล์ digest **`7e1e65e52659`** + **`leftovers: none`** ✅
2. **rollback DLL:** จาก `.prev-6a94abb5-20261030-063817`/`.pdb.prev-856d3bed-20261030-063817` →
   `Mods\LongYinCoop.dll` = **`6a94abb5…`** + `.pdb` = **`856d3bed…`** (rehash ตรงเป๊ะ) ✅
3. **net.cfg:** คืนจาก `bak-n4r2-20261030-063817` → **`12541f34…` EXACT** (npcPolicy = 0 รายการ,
   enabled=false) ✅
4. **pref:** `Sp5T0 = false` (บรรทัด 15) ✅ · **หมายเหตุ divergence:** hash ปลายทาง = **`976ccfb9…`**
   ไม่ใช่ anchor ต้นเซสชัน `733d9a0c…` — เพราะ **ML เขียน cfg ตอนปิดเกมทุกครั้ง**
   ("Preferences Saved!" ทั้งสองรอบ 06:42:56/06:46:31) · ค่า key ทั้งหมดตรงตลอด (`Sp5T0=false` ·
   `TimeV2a=false` · `AllowRemoteInput=false`) — สาย divergence อัปเดตใน VERSION.md แล้ว ·
   anchor cfg ของเซสชันถัดไป = `976ccfb9…` (ค่า key ตรวจแล้ว)
5. เกม 2 ไฟล์ = `33c56def…`/`451311d4…` ตรง VERSION.md ตลอดเซสชัน ✅ · เกมไม่รัน · ไม่มี orphan

## สรุป N4 รวม 3 รอบ (รอบ 1 = label `SP5NPC-N4-20261030-052251` · รอบ 2–3 = label นี้)

| เกณฑ์ | รอบ 1 (artifact `ba1e1dd1…`) | รอบ 2 (`476f390e…`) | รอบ 3 (`476f390e…`) |
|---|---|---|---|
| T2 spe=170 / protag=1 / 掌门 30/30 | PASS | PASS | PASS |
| T2 Teacher 1 อาจารย์/สำนัก | PASS (311/311) | PASS (311/311) | PASS (311/311) |
| T2 loveable (นิยามใหม่: listCount=57 + listPresent=57) | listCount=57 ✅ (listPresent ยังไม่มี — หยุดรอบด้วยเกณฑ์เก่าที่ heroCount=631) | **PASS 57/57** | **PASS 57/57** |
| T2 BirthSetting (ฐาน [-1,0,0,0,0,0]) | ตรงฐาน (count=6) | PASS | PASS |
| T3 f2=0 + no errors | PASS | PASS | PASS |
| T4 sect_after ∈ [280,350] | **311** | **311** | **311** |
| T4 perForce คงรูป | Σ=311 | เท่ารอบ 1 ทุกสำนัก | เท่ารอบ 1–2 ทุกสำนัก |
| T4 h นิ่ง / factory C (A/B=0) | PASS (719) | PASS (716) | PASS (720) |
| heroNum window Σ=50 (วัดตรง) | ไม่มีจุดวัด (probe เดิม) | **PASS** | **PASS** |
| heroNum post-restore Σ=88 (วัดตรง ณ sng.end) | ไม่มีจุดวัด | **PASS** | **PASS** |
| gate install / patch นอกชุด / [ERROR] | สำเร็จ / ไม่มี / 0 | สำเร็จ / ไม่มี / 0 | สำเร็จ / ไม่มี / 0 |
| `SaveSlot*` / hash ทุกไฟล์ | ไม่ถูกเขียน / ตรง | ไม่ถูกเขียน / ตรง | ไม่ถูกเขียน / ตรง |

- **กติกา match ชื่อ loveable = RUNTIME-confirmed แล้ว** (รอบ 2 = ครั้งแรก, รอบ 3 = ซ้ำ — 57/57
  ชื่อเจอใน Heros ด้วย heroName/family+given)
- **census โควตา deterministic ตรงโมเดล:** sect 311 + perForce รายสำนักเท่ากันทุก byte ใน 3 รอบ
  (ฐาน gate-off = 562) · TempHeros = RNG (79–89 จาก 6 จุดข้อมูล)
- **heroNum window/restore = วัดตรงได้ครั้งแรก** (artifact ใหม่) — Σ 50→88 ตรงมติทั้งสองจุดทั้งสองรอบ

**สถานะจบเซสชัน:** คืนสภาพครบ (เซฟ EXACT `7e1e65e52659` · DLL `6a94abb5…` · net.cfg `12541f34…`
เป๊ะ · Sp5T0=false) · **ไม่ commit ไม่ push ไม่ tag ไม่ปิด stage** — ไฟล์ evidence นี้คง untracked
รอมติ · **stage N4 วัดครบ 3 รอบ ตามเกณฑ์ทุกข้อ (รอบ 1 หยุดที่เกณฑ์เก่าของ loveable ซึ่งถูกแทนด้วย
นิยามใหม่ที่อนุมัติแล้ว)** — การปิด stage/tag = มติผู้ใช้แยกตามกติกา 2 · N5–N6 ยังไม่อนุมัติ
