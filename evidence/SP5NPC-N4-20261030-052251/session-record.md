# session record — SP5-NPC stage N4 (T2–T4 ที่ Light ×0.5, `npcPolicy=light`) — เริ่ม 2026-10-30 05:22 +0700

## มติ/อนุมัติที่รับ

- วลีสั่งผู้ใช้: **`เริ่ม N4 session`** = อนุมัติขั้น 1–8 ตาม `plans/sp5-npc-n4-approval-request.md`
  **+ ระบุครบตามที่แผนกำหนด:**
  - deploy **artifact ใหม่ `ba1e1dd1…` (PDB `a96a4a38…`)** จาก `evidence/SP5NPC-N4prep-20261030-045558/`
    แทน `2b59e46c…` (T2 ทาง (ข) ทำเสร็จแล้ว — ไม่ทำขั้นเตรียมซ้ำ)
  - **band T4: sect_after ∈ [280, 350]** — ถ้าหลุด band รายงานตัวเลขจริงรายสำนัก แล้ว**หยุด**
    ห้ามตีความ/ห้ามปรับ band เอง
  - **จำนวนรอบ = 3** (default)
  - **T2 = census field ใหม่** (zhangmen 30 / loveable 57 / teacher / birth) — ถ้าค่าไม่ตรงที่คาด
    **หยุดรายงาน** (field เหล่านี้ยังไม่เคยอ่านบน runtime — คาดหวัง 30/57/1 เป็นข้อกำหนดจาก
    design ยังไม่ RUNTIME-confirmed ตาม provenance N4prep)
  - **npcPolicy = light เฉพาะช่วงรัน** · `Sp5T0=true` ต่อ · สำรอง `net.cfg.bak` แล้วคืน
    `12541f34…` เป๊ะท้ายเซสชัน
  - **typed write ของ G1 ล้ม = ห้ามใช้ raw offset — หยุดรายงาน**
  - **N5–N6 ไม่อนุมัติ · ไม่ปิด stage เอง · ไม่ tag เอง · ไม่ push**
- **มติเพิ่มเติมในเซสชัน (ถาม-ตอบ 05:2x):** net.cfg ช่วงรัน = **`enabled = true` + `npcPolicy = light`**
  — เหตุผล: `Sp5NpcPolicy.Decide` + เทสต์ 80 ชุด lock ไว้ว่า gate เปิดได้เฉพาะ enabled=true
  (opt-in สองชั้น — มติเดิมพูดแค่ npcPolicy ซึ่งจะได้ Off ตลอดเซสชัน) · side effect ที่อนุมัติ:
  **S2 TCP listener loopback 127.0.0.1:47800 activate ช่วงรัน** (spectator เท่านั้น ไม่มี write
  path, `AllowRemoteInput=false` คงเดิม) · ท้ายเซสชันคืน `net.cfg = 12541f34…` เป๊ะ (enabled=false)

## session-start checks (read-only, 05:17–05:19) — ผ่านครบ

| รายการ | ค่าที่วัด | เทียบ |
|---|---|---|
| เกม | not running (tasklist) | ✅ |
| `GameAssembly.dll` | `33c56def4102844a1190eec963a05c834cfcf992d42b423d485ce8584d7920c1` | ✅ VERSION.md |
| `global-metadata.dat` | `451311d4dc8c1b8b5073905c8b39a9781bd7e1f44ed88fbf7b35f5345c18bfd0` | ✅ VERSION.md |
| `Mods\LongYinCoop.dll` (ก่อน deploy) | `6a94abb579e65d5e6b0af917dfed4ba6f5350ad76847190bd7263cda3077b334` (359,936 B) | ✅ มติ rollback |
| `Mods\LongYinCoop.pdb` (ก่อน deploy) | `856d3beda727218823170f861f532509c5458eb91e51c79f07c0452d3b196035` (97,128 B) | ✅ |
| `MelonPreferences.cfg` (ก่อนแก้) | `976ccfb9a6e6ff5be9718229b25af3e4b2bd765edeae419bc6fa0cd0ba39d325` · `Sp5T0 = false` บรรทัด 15 (เพียงบรรทัดเดียวทั้งไฟล์) | ✅ สถานะหลังปิด N3 |
| `net.cfg` (ก่อนแก้) | `12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8` (444 B) — **ไม่มี key `npcPolicy`** | ✅ |
| Save | 57 ไฟล์ digest `df8e5a11beed` · `leftovers: none` | ✅ |
| mod repo | HEAD `b58d553` (N4-prep commits dbca3b0/b58d553 ครบ) · working tree = M testimony + untracked ชุดเดิม (ไม่เพิ่ม/ไม่หาย) | ✅ |
| game repo | HEAD `944ceda` = สถานะ AGENTS.md | ✅ |

## ขั้น 2 — deploy (05:22)

- สำรองก่อนทับ: `Mods\LongYinCoop.dll.prev-6a94abb5-20261030-052251` = `6a94abb5…` ✅ ·
  `Mods\LongYinCoop.pdb.prev-856d3bed-20261030-052251` = `856d3bed…` ✅ (hash ยืนยันหลัง cp)
- คัดลอกจาก `evidence/SP5NPC-N4prep-20261030-045558/` → `Mods\` แล้ว rehash:
  - `Mods\LongYinCoop.dll` = **`ba1e1dd14e0cbeaede4bbd57af55477bf45639638a33ad40a2c927f99abeefa0`** (373,760 B) ✅ ตรง artifact
  - `Mods\LongYinCoop.pdb` = **`a96a4a38e99c07f768a7d538a23018cc7164be9263b703f9f08390242b8ce132`** (102,920 B) ✅ ตรง artifact

## ขั้น 3 — net.cfg + pref (05:23)

- `net.cfg`: สำรอง `net.cfg.bak-n4-20261030-052251` = `12541f34…` ✅ (ยืนยัน hash ก่อนแก้) →
  แก้ 2 จุด: `enabled = false` → **`true`** (spacing เดิม) + append **`npcPolicy       = light`**
  (บรรทัดท้าย · key อื่น/secret/comment ไม่แตะ) → hash หลังแก้ **`992c71b8fde13e3a2570d8e02715295862040a7930f7f1ecb4edf736b78a2dbc`**
- side effect ที่อนุมัติแล้ว: S2 service (enabled=true) จะ log `[S2] S2a-3 ACTIVE …` +
  `[S2] listening on … (loopback only …)` — spectator เท่านั้น · ถ้าขึ้น `[S2] SNAPSHOT SERVICE IDLE (reason)` = บันทึก
  reason ไว้ (ไม่บล็อก SP5 gate ซึ่งอ่าน net.cfg เอง)
- `MelonPreferences.cfg` บรรทัด 15: `Sp5T0 = false` → **`Sp5T0 = true`** (แก้บรรทัดเดียว) —
  ก่อนแก้ `976ccfb9…` → หลังแก้ **`5a9d4280f2dd8590e6f5498dd45a6562eb2bbbd43c634b41fccd5a2e6505a519`**
  (ตรงสถานะช่วงทดสอบของ N3 เป๊ะ) · ค่าอื่นไม่แตะ (`TimeV2a=false`, `AllowRemoteInput=false`)

## ขั้น 4 — save-split (05:24)

```
split --label SP5NPC-N4-20261030-052251 --expected-baseline coop-test/baselines/20261028-071340/savehash.real.before.txt
  Save vs baseline: changed=0 missing=0 added=0
  copy vs real:     changed=0 missing=0 added=0
OK split SP5NPC-N4-20261030-052251: 57 files, real kept as Save.real-SP5NPC-N4-20261030-052251
```

## คาดหมายก่อนเปิดเกม (เกณฑ์ตรวจหลังปิดแต่ละรอบ)

- ML log ใหม่ต้องมี: `Sp5T0 installed (log-only) … patches=7 (before=0)` + **`Sp5Npc gate installed
  (fail-closed; ย่อ/คลำ/deny ตาม net.cfg npcPolicy)`** (gate = 4 patches: G1 prefix+finalizer,
  G4 postfix ×2, G5 prefix) — รวมกันต้องไม่มี patch นอกชุดนี้ · ตอนปิด: `Sp5T0 removed: … errors=0
  unattributed=0 outside=0 phaseStuck=0 rebaseline=0 readfail=0 ownedLeft=0` + `probe: unpatched
  (before=1 after=0)`
- บรรทัด `[S2] …` (ACTIVE/listening/IDLE) = ตามที่อนุมัติ enabled=true — ไม่ใช่ patch นอกชุด
- **T2 (zero-tolerance):** census ต้องครบ **spe=170 · protag=1 (factory.call ตัวแรก heroID=0) ·
  `zhangmen.count=30` ทุก leader ∈ 1..170 · `loveable.heroCount=57` = listCount · `teacher`:
  membersWithTeacher == eqLeader (Teacher=1 จุดเรียนได้) · `birth` = ค่าเดิมของโลก** — ค่าใดไม่ตรง
  ที่คาด = **หยุดรายงาน**
- **T3:** `f2 = 0` + ไม่มี error/phase ผิดปกติ
- **T4:** heroNum ในรอบ = **Σ=50** (ย่อแล้ว [2,2,2,2,1,1]/[3,2,2,2,2,1]/[3,3,2,2,2,1]/[3,3,3,2,2,2]) ·
  restore ท้ายรอบกลับ Σ=88 (ตรวจ heroNum.read รอบถัดไป) · **sect_after ∈ [280, 350]** (band อนุมัติ) ·
  per-force ≈ template ย่อแล้ว ±2/band · rank คงสัดส่วน · `h` นิ่งหลัง genenemy.end · factory C ≈
  sect_after + 170 + 1 + 150 · A/B = 0 — **หลุด band = รายงานตัวเลขจริงรายสำนักแล้วหยุด**
- เซฟ: `SaveSlot*` ห้ามถูกเขียน (โลกใหม่ไม่เซฟ) · drift คาดหมาย = `PlayerprefData.dat` เท่านั้น
- **หยุดทันที** ถ้า: hash เกม/Mods/เซฟไม่ตรง snapshot · gate install ล้ม · patch นอกชุด ·
  `SaveSlot*` ถูกเขียน · PROTECTED ขาด · heroNum ไม่ restore · การแก้จาก session อื่นที่ไม่ได้ระบุ

## รอบเปิดเกม (ผู้ใช้เปิดเอง ×3 — โลกใหม่ทุกรอบ ห้ามเซฟ slot)

| รอบ | เปิด/ปิด (ML log) | ตรวจหลังปิด | ผล |
|---|---|---|---|
| 1 | `26-10-30_5-27-2.log` (เปิด 05:27:02 ปิด ~05:27:53 · ~51 s) | ✅ ผ่าน (รายละเอียดด้านล่าง) | **T2 STOP ที่ loveable.heroCount=631 ≠ 57** — ไม่รันรอบ 2–3 |
| 2 | ไม่รัน (หยุดตามมติ T2) | — | — |
| 3 | ไม่รัน (หยุดตามมติ T2) | — | — |

## ผลตรวจรอบ 1 (05:28–05:34)

- **ML log:** `Sp5T0 installed (log-only) … patches=7 (before=0)` + `Sp5Npc gate installed
  (fail-closed; ย่อ/คลำ/deny ตาม net.cfg npcPolicy)` (05:27:06) · ตอนปิด `Sp5T0 removed: events=2265
  errors=0 unattributed=0 outside=0 phaseStuck=0 rebaseline=0 readfail=0 ownedLeft=0` +
  `probe: unpatched (owned patches before=1 after=0)` ⇒ gate install **สำเร็จ** ไม่มี patch นอกชุด ·
  **`[ERROR]` = 0 บรรทัด** · WARN = noise UnityExplorer/UniverseLib ชุดเดิม
- **บรรทัด `[S2]` ตามที่อนุมัติ enabled=true:** `listening on tcp-listen(127.0.0.1:47800) (loopback
  only)` + `S2a-3 ACTIVE transport=tcp` + `guest authenticated sessionId=1` + `guest frame dropped:
  forbidden type Hello/Snapshot/BattleEvent` (self-test ในตัว — ยืนยันจากโค้ด `RunLoopbackSelfTest`
  ที่ Activate, ไม่ใช่ client ภายนอก) · metrics ticker `commandsAccepted=0` ตลอด · ปิดท้าย
  `SNAPSHOT SERVICE IDLE (idle: unsupported settings) -> block A` (บันทึกตามจริง — ไม่บล็อก SP5 gate)
- **census (ndjson `20261030-052706` · worldcount 7 จุด นิ่งตั้งแต่ genhero.end ถึง sng.end):**
  `h=632` = **protag 1 + spe 170 + sect 311 + wanderer 150 + f2 0** · h นิ่ง (ไม่มีการลบหลังสร้าง) ·
  TempHeros/addtemps = **88** (tempMatch 88 · T0=87, N3=81/79/84 — RNG worldgen, ใหม่สุดของช่วงที่เคยเห็น) ·
  factory **C=719 (A/B=0)** ทั้งหมด nonNull · first factory.call = `heroID=1, belongForceID=25,
  heroForceLv=5` — **เท่า T0/N3 ทุกค่า** (protag ไม่ได้เกิดผ่าน factory — ตัวเอกยืนยันด้วย hist protag=1)
- **T2 รายค่า (censusLast + worldcount genenemy.end):**
  - `zhangmen`: **count=30, inSpe=30** ✅ · leaderIds 30 ค่า (10,13,16,…,1,76,77,78,79) ทุกค่า ∈ 1..170
  - `loveable`: **listCount=57** ✅ (GDC.loveableSpeHeroList) — แต่ **heroCount=631 ❌ ≠ 57 ที่คาด**
    (631 = h−1 — flag อ่านได้ true แทบทุก hero · **เงื่อนไขหยุดของผู้ใช้กระตุ้น**)
  - `teacher`: สุดท้าย **membersWithTeacher=311, eqLeader=311, distinct = 30 ค่า = ชุด leaderIds
    เป๊ะ** (1 อาจารย์/สำนัก ตรง design) — หมายเหตุ: ที่ genhero.end ยังเป็น eqLeader=0 ก่อนเทียบได้
    ตอน genenemy.begin · 311 = จำนวน sect พอดี (ทุกสมาชิกสำนักมี Teacher = ผู้นำสำนักตัวเอง)
  - `birth`: count=6, vals=`[-1,0,0,0,0,0]` — **ยังไม่มี baseline เทียบ** (N3 ไม่มี field นี้;
    provenance กำหนดให้เทียบข้ามรอบ — รอบเดียวจึงรายงานค่าดิบอย่างเดียว)
- **T3:** **f2 = 0** ✅ + errors=0 + phase ปกติ (audit: no unattributed, no phase outside W2/W3)
- **T4 รายค่า:**
  - **sect_after = 311 — ใน band [280, 350]** ✅ (ฐาน 562 ลดลง 251 · quota-model คาด ~300–316,
    linear ~281 — รายงานตามจริงไม่ตีความ)
  - perForce (Σ=311): `[10,11,12,12,12,12,11,11,11,13,13,12,11,11,11,11,12,10,12,11,11,9,9,9,4,3,9,11,8,9]`
    · สำนักเล็กสุด = force#25 (3), #24 (4) · ใหญ่สุด = #9/#10 (13) — **rank เทียบ N3 ยังไม่ทำ
    (หยุดที่ T2 ก่อนวิเคราะห์เชิงลึก ตามมติ "หยุดและรายงาน")**
  - `h` นิ่งหลัง genenemy.end ✅ · factory C = 719 ≈ 632 Heros + 88 TempHeros − 1 (protag นอก factory) ✅ · A/B = 0 ✅
  - heroNum: read เดียวที่ genhero.begin (ก่อน factory แรก = pre-window) = ตาราง CSV ต้นฉบับ Σ=88
    ✅ · **ค่าระหว่าง window Σ=50 ไม่มีการวัดตรง** (probe อ่าน heroNum ครั้งเดียว pre-window ตามดีไซน์
    เดิม) — หลักฐานทางอ้อม: sect 311 (single-shrink ของ quota 883→632 ไม่ compound) + finalizer 719/719 ·
    **restore Σ=88 "รอบถัดไป" ไม่ได้วัด (ไม่มีรอบ 2 — หยุดตาม T2)**
- **เซฟ (`save-split verify`): PASS** — mounted copy vs baseline changed=1 = **`PlayerprefData.dat`
  เท่านั้น** · **`SaveSlot*` ไม่ถูกเขียน** · real 0/0/0 + mtime drift 0
- **hash หลังรอบ:** เกม 2 ไฟล์ + Mods DLL/PDB ตรงเดิมทุก byte (`33c56def…` / `451311d4…` /
  `ba1e1dd1…` / `a96a4a38…`)
- **audit tool (`sp5t0_audit.py`):** E5/E7 structural **CLEAN** · creation=719 pre=0 post=0 ·
  dH_observed=632 (T0/N3: 883) · dT=88 · VERDICT **PASS (structural)**
- หลักฐานรอบ: `ml-log-round1.log`, `summary-round1.json`, `sp5t0-round1.ndjson` (โฟลเดอร์นี้ ·
  ต้นฉบับ `UserData\LongYinCoop\SP5T0\20261030-052706\`)

## มติ/การหยุดในเซสชัน

- **เงื่อนไขหยุดของผู้ใช้กระตุ้นที่ T2:** `loveable.heroCount = 631 ≠ 57` (มติวลีสั่ง: "ถ้าค่าไม่ตรง
  ที่คาด ให้หยุดและรายงาน — field เหล่านี้ยังไม่เคยอ่านบน runtime"; รายการหยุด: "PROTECTED ขาด …
  loveable≠57") ⇒ **หยุดทันที — ไม่รันรอบ 2–3 · ไม่ตีความค่า · ไม่แก้ band/tool/โค้ด**
- ข้อสังเกตที่รายงานคู่กันโดยไม่ตัดสิน: `loveableList (GDC.loveableSpeHeroList.Count) = 57` ครบตามคาด
  ขณะที่ `heroCount (นับ flag HeroData.loveAble) = 631` — สองการอ่านนี้ต่างกันตามจริง · ค่าคาด "57"
  ของ heroCount เป็นข้อกำหนดจาก design/provenance N4prep ที่**ยังไม่เคย RUNTIME-confirmed**

## ขั้นคืนสภาพ (ทำแล้ว 05:35 — ตามมติจบเซสชัน)

1. **restore เซฟ:** `save-split restore --label SP5NPC-N4-20261030-052251` — `Save` = โฟลเดอร์จริง
   renamed กลับ (ไม่เคยถูกเขียน), **restored vs real baseline = 0/0/0 EXACT**, สำเนาทดสอบเก็บ
   `coop-test/SP5NPC-N4-20261030-052251/Save.used-20261030-053522` · หลัง restore: 57 ไฟล์ digest
   `df8e5a11beed` + **`leftovers: none`** ✅
2. **rollback DLL:** จาก `.prev-6a94abb5-20261030-052251`/`.pdb.prev-856d3bed-20261030-052251` →
   `Mods\LongYinCoop.dll` = **`6a94abb5…`** + `.pdb` = **`856d3bed…`** (rehash ตรงเป๊ะ) ✅
   (ไฟล์ `.prev` ของรอบนี้คงไว้เป็น rollback point ตาม convention)
3. **net.cfg:** คืนจาก `bak-n4-20261030-052251` → **`12541f34…` EXACT** (ทั้ง `npcPolicy` ถูกถอด
   และ `enabled=false` — grep หา npcPolicy/enabled=true = 0 รายการ) ✅
4. **pref:** `Sp5T0 = true` → **`false`** (บรรทัด 15) — `MelonPreferences.cfg` กลับ **`976ccfb9…`** =
   hash เดิมก่อน session **ทุก byte** ✅ (`TimeV2a=false`, `AllowRemoteInput=false` คงเดิม)
5. เกม 2 ไฟล์ = `33c56def…`/`451311d4…` ตรง VERSION.md ตลอดเซสชัน (วัดซ้ำหลังปิดรอบ 1) ✅
6. game: not running · ไม่มี orphan process

## สรุป T2/T3/T4 รายเกณฑ์ (รอบเดียว — หยุดตามมติ T2)

| เกณฑ์ | วิธีวัด | ผลรอบ 1 |
|---|---|---|
| **T2 spe = 170** | census hist + `speDbTableSize` | **PASS** — spe=170 ทุก worldcount, speDb=170 |
| **T2 ตัวเอก** | hist protag=1 (first factory.call = heroID 1/force 25 **เท่า T0/N3 ทุกค่า** — protag ไม่ได้เกิดผ่าน factory) | **PASS** — protag=1, heroID 0 ไม่ปรากฏใน factory เช่นเดิมทุกรอบฐาน |
| **T2 掌门 30/30** | census field `zhangmen` | **PASS** — count=30, inSpe=30, leaderIds ครบ 30 ค่า ∈ 1..170 |
| **T2 loveable = 57** | census field `loveable` | **FAIL → STOP** — listCount (GDC list) = 57 ✅ แต่ **heroCount (flag `HeroData.loveAble`) = 631 ≠ 57** · 631 = h−1 (flag ขึ้นแทบทุก hero) — การอ่าน field นี้ไม่เคยมีบน runtime มาก่อน คาดหวัง 57 จาก design/provenance · **ไม่ตีความ ไม่แก้อะไร — รายงานและหยุดตามมติ** |
| **T2 Teacher = 1** | census field `teacher` | **PASS ที่สถานะสุดท้าย** — membersWithTeacher=311 = eqLeader=311, distinct = 30 ค่า = ชุด leaderIds เป๊ะ (1 อาจารย์/สำนัก) · timing note: eqLeader=0 ตอน genhero.end, สมบูรณ์ตอน genenemy.begin |
| **T2 BirthSetting** | census field `birth` | **ไม่มีฐานเทียบ** — count=6, vals=`[-1,0,0,0,0,0]` (N3 ไม่มี field นี้ · เทียบข้ามรอบต้องมีรอบถัดไป — ไม่ได้รัน) |
| **T3 f2 = 0** | census hist + errors | **PASS** — f2=0, errors=0, phase ปกติ (audit CLEAN) |
| **T4 sect_after ∈ [280,350]** | census hist | **PASS ตาม band** — sect=311 (ฐาน 562 → −251; quota-model ~300–316, linear ~281 — รายงานตามจริง) |
| **T4 กระจายรายสำนัก** | perForce | **Σ=311** รายสำนัก `[10,11,12,12,12,12,11,11,11,13,13,12,11,11,11,11,12,10,12,11,11,9,9,9,4,3,9,11,8,9]` — เทียบ template/rank ยังไม่วิเคราะห์เชิงลึก (หยุดที่ T2 ก่อน) |
| **T4 ไม่มีการลบหลังสร้าง** | h นิ่ง + factory | **PASS** — h=632 นิ่ง genhero.end→sng.end · factory C=719 (A/B=0) ≈ 632+88−1 (protag นอก factory) |
| **T4 heroNum window** | heroNum.read + census | **วัดตรงไม่ได้ตามดีไซน์ probe** (read เดียว pre-window Σ=88) · หลักฐานทางอ้อม: sect 311 = single-shrink (ไม่ compound) + finalizer 719/719 · restore Σ=88 รอบถัดไปไม่ได้วัด (ไม่มีรอบ 2) |

**เงื่อนไขหยุดของผู้ใช้ที่กระตุ้น:** 1 รายการ — `loveable.heroCount = 631 ≠ 57` (T2) · ไม่มีรายการอื่น:
hash ตรง snapshot ทุกจุด · gate install สำเร็จ · ไม่มี patch นอกชุด · `SaveSlot*` ไม่ถูกเขียน ·
spe/protag/掌门 ครบ · typed write ทางอ้อมยังไม่พบข้อล้ม (sect 311 = shrink ทำงานโดยไม่ compound) ·
ไม่พบการแก้จาก session อื่น

**สถานะจบเซสชัน:** คืนสภาพครบทุกข้อตามมติ (เซฟ EXACT `df8e5a11beed` · DLL `6a94abb5…` · net.cfg
`12541f34…` เป๊ะ · Sp5T0=false `976ccfb9…` · เกม 2 ไฟล์ตรง VERSION.md) · **ไม่ commit ไม่ push ไม่
tag ไม่ปิด stage** — ไฟล์ evidence นี้คง untracked ใน mod repo working tree รอมติ · รอบ 2–3 ไม่ได้รัน
(หยุดตาม T2) · **N4 ยังไม่มี verdict รวม** — ค้างตัดสินที่ loveable field: แก้การอ่าน field (design
ใหม่/ตรวจ offset `loveAble` กับ dump.cs) หรือเปลี่ยนเกณฑ์ T2 ไปที่ listCount — **ทั้งหมดเป็นมติผู้ใช้**
