# session record — SP5-NPC stage N3 (T1 normal mode gate off + Sp5T0 census) — เริ่ม 2026-10-30 03:55 +0700

## มติ/อนุมัติที่รับ

- วลีสั่งผู้ใช้: **`เริ่ม N3 session พร้อม Sp5T0`** = อนุมัติขั้น 1–7 ตาม `plans/sp5-npc-n3-approval-request.md`
  **+ ตัวเลือกเปิด pref `Sp5T0=true`** ระหว่าง T1 เพื่อ census เชิงตัวเลข (band RNG ≥3 รอบ)
- เงื่อนไขเพิ่มเติมจากผู้ใช้ (ยึดตามตัวอักษร):
  - `npcPolicy` คง **"off"** ตลอด stage นี้ — ห้ามตั้ง light (เป็นของ N4)
  - ขอบเขต N3 เท่านั้น — N4–N6 ยังไม่อนุมัติ
  - ไม่ปิด stage เอง ไม่ tag เอง · ไม่ push ในเซสชันนี้
  - หยุดทันทีถ้า: hash เกม/Mods/เซฟไม่ตรง snapshot · ML log แสดง gate install ล้ม · patch นอกที่ติดตั้งโผล่ ·
    SaveSlot* ถูกเขียน · เจอการแก้จาก session อื่น
  - จบเซสชัน: restore เซฟ EXACT + rollback DLL กลับ `6a94abb5` + `Sp5T0=false` + รายงานรายเกณฑ์ T1

## session-start checks (read-only, 03:53) — ผ่านครบ

| รายการ | ค่าที่วัด | เทียบ |
|---|---|---|
| เกม | not running (tasklist) | ✅ |
| `GameAssembly.dll` | `33c56def4102844a1190eec963a05c834cfcf992d42b423d485ce8584d7920c1` | ✅ VERSION.md |
| `global-metadata.dat` | `451311d4dc8c1b8b5073905c8b39a9781bd7e1f44ed88fbf7b35f5345c18bfd0` | ✅ VERSION.md |
| `Mods\LongYinCoop.dll` (ก่อน deploy) | `6a94abb579e65d5e6b0af917dfed4ba6f5350ad76847190bd7263cda3077b334` | ✅ มติ rollback |
| `Mods\LongYinCoop.pdb` (ก่อน deploy) | `856d3beda727218823170f861f532509c5458eb91e51c79f07c0452d3b196035` | ✅ |
| `MelonPreferences.cfg` (ก่อนแก้) | `976ccfb9a6e6ff5be9718229b25af3e4b2bd765edeae419bc6fa0cd0ba39d325` · `Sp5T0 = false` บรรทัด 15 | ✅ HANDOFF 034239 |
| `net.cfg` | `12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8` — **ไม่มี key `npcPolicy`** = default off | ✅ ไม่แตะ |
| Save | 57 ไฟล์ digest `df8e5a11beed` · `leftovers: none` · EXACT 22 รายการ (รวม `coop-test/baselines/20261028-071340` ตรวจผ่าน split) | ✅ |
| mod repo | HEAD `7396b941e0f5365d803ba4ee87faca86c1f8dfc5` · working tree = M testimony + untracked เดิม (ชุดเดียวกับ HANDOFF 034239 ไม่เพิ่ม/ไม่หาย) | ✅ |
| game repo | HEAD `944ceda2bd1d1d0d751eed8e24281c64335ff3b3` = origin/main snapshot | ✅ |

## ขั้น 2 — deploy (03:55)

- สำรองก่อนทับ: `Mods\LongYinCoop.dll.prev-6a94abb5-20261030-035500` = `6a94abb5…` ✅ ·
  `Mods\LongYinCoop.pdb.prev-856d3bed-20261030-035500` = `856d3bed…` ✅ (hash ยืนยันหลัง cp)
- คัดลอกจาก `evidence/SP5NPC-N2-20261030-033852/` → `Mods\` แล้ว rehash:
  - `Mods\LongYinCoop.dll` = **`2b59e46c53bb2f415d2e6f6f526eafe2409612315c08158ac733642c8028396f`** (371,712 B) ✅ ตรง artifact
  - `Mods\LongYinCoop.pdb` = **`857cd1b86aa3bb679ceac6825f66d614dc4520a193029d42e2456d5ade6a74fc`** (102,068 B) ✅ ตรง artifact

## ขั้น 3 — save-split (03:55)

```
split --label SP5NPC-N3-20261030-035500 --expected-baseline coop-test/baselines/20261028-071340/savehash.real.before.txt
  Save vs baseline: changed=0 missing=0 added=0
  copy vs real:     changed=0 missing=0 added=0
OK split: 57 files, real kept as Save.real-SP5NPC-N3-20261030-035500
```

## ขั้น 4 เตรียม — pref (03:56)

- `MelonPreferences.cfg` บรรทัด 15: `Sp5T0 = false` → **`Sp5T0 = true`** (แก้บรรทัดเดียว)
  - ก่อนแก้ `976ccfb9…` → หลังแก้ **`5a9d4280f2dd8590e6f5498dd45a6562eb2bbbd43c634b41fccd5a2e6505a519`**
  - ค่าอื่นใน cfg ไม่แตะ (`TimeV2a=false`, `AllowRemoteInput=false` คงเดิม)
- `net.cfg` ไม่แตะ — ไม่มี `npcPolicy` key ⇒ gate Decide = **Off** (fail-closed) ทุก invocation

## คาดหมายก่อนเปิดเกม (เกณฑ์ตรวจหลังปิดแต่ละรอบ)

- ML log ใหม่ต้องมี: probe ติดครบ (แบบรอบ SP5T0-run: 7 hooks, ownedLeft=0 ตอนถอน) **+ บรรทัด `Sp5Npc gate installed`**
  (patch ของ gate: G1 prefix+finalizer, G4 postfix ×2, G5 prefix — รวมกับ probe 7 ต้องไม่มี patch นอกชุดนี้)
- `SaveSlot*` ห้ามถูกเขียน (โลกใหม่ไม่เซฟ) · drift คาดหมาย = `PlayerprefData.dat` เท่านั้น
- census เชิงตัวเลขจาก `UserData/LongYinCoop/SP5T0/<ts>/sp5t0.ndjson` ต่อรอบ — โครงคาดจากรอบ T0:
  protag 1 + spe 170 + sect (quota heroNum) + wanderer ~150 (RNG) + f2 (RNG เล็ก) · TempHeros = W3 calls
- **หยุดทันที** ถ้า ML log แสดง gate install ล้ม หรือ patch นอกชุดโผล่ (เงื่อนไขผู้ใช้)

## รอบเปิดเกม (ผู้ใช้เปิดเอง ×3) — สถานะ

| รอบ | เปิด/ปิด (ML log) | ตรวจหลังปิด | ผล |
|---|---|---|---|
| 1 | `26-10-30_3-59-14.log` (เปิด 03:59:19 ปิด ~04:00:18 · 57.4 s · pid 30584) | ✅ ผ่าน (รายละเอียดด้านล่าง) | **PASS ทุกข้อตรวจ** |
| 2 | `26-10-30_4-4-25.log` (เปิด 04:04:29 ปิด ~04:05:17 · 46.7 s · pid 35688) | ✅ ผ่าน (เท่ารอบ 1 ทุกข้อ) | **PASS ทุกข้อตรวจ** |
| 3 | `26-10-30_4-15-56.log` (เปิด 04:16:02 ปิด ~04:16:54 · 51.8 s · pid 43836) | ✅ ผ่าน (เท่ารอบ 1–2 ทุกข้อ) | **PASS ทุกข้อตรวจ** |

## ผลตรวจรอบ 1 (04:01–04:03)

- **ML log:** `Sp5T0 installed (log-only) … patches=7 (before=0)` + **`Sp5Npc gate installed (fail-closed; …)`**
  = gate install **สำเร็จ** (ไม่มีบรรทัด install ล้ม) · ตอนปิด: `Sp5T0 removed: events=2990 errors=0
  unattributed=0 outside=0 phaseStuck=0 rebaseline=0 readfail=0 ownedLeft=0` + `probe: unpatched (owned
  patches before=1 after=0)` ⇒ **ไม่มี patch นอกที่ติดตั้ง** (outside=0) · ERROR = 0 (WARN เฉพาะ noise
  UniverseLib/UnityExplorer ชุดเดิม เท่ากับ log รอบก่อน ๆ)
- **census เชิงตัวเลข (ndjson `20261030-035919`):** worldcount `h=883, t=81` · hist = **protag 1 + spe 170 +
  sect 562 + wanderer 150 + f2 0** = ตรง baseline รอบ T0 (883) **ทุกประเภท** · `h` นิ่ง 883 ตลอด (t 0→81,
  ไม่มีการลบหลังสร้าง) · TempHeros **81** (T0 = 87 — ตัวแปร RNG ของ worldgen, ไม่ใช่ Heros census) ·
  factory C=963 (A/B=0) · speDbTableSize=170 · heroNumLastRead Σ=88 (ตาราง CSV ไม่ถูกย่อ — gate Off)
- **audit tool (`sp5t0_audit.py`):** E5/E7 structural **CLEAN** · dH_observed=883 (T0: 883) · dT_observed=81
  (T0: 87) · VERDICT PASS (structural)
- **เซฟ (`save-split verify`): PASS** — mounted copy vs baseline changed=1 = **`PlayerprefData.dat` เท่านั้น**
  (volatile ตามคาด) · **`SaveSlot*` ไม่ถูกเขียนเลย** · real folder 0/0/0 + mtime drift 0
- **hash หลังรอบ:** เกม 2 ไฟล์ + Mods DLL ตรงเดิมทุก byte (`33c56def…`/`451311d4…`/`2b59e46c…`)
- หลักฐานรอบ: `ml-log-round1.log`, `summary-round1.json`, `sp5t0-round1.ndjson` (โฟลเดอร์นี้ · ndjson ต้นฉบับ
  `UserData\LongYinCoop\SP5T0\20261030-035919\`)

## ผลตรวจรอบ 2 (04:06)

- **ML log:** `Sp5T0 installed … patches=7 (before=0)` + `Sp5Npc gate installed` (04:04:29) · ตอนปิด
  `Sp5T0 removed: events=2982 errors=0 unattributed=0 outside=0 phaseStuck=0 rebaseline=0 readfail=0
  ownedLeft=0` + `probe: unpatched (before=1 after=0)` ⇒ เท่ารอบ 1 ทุกข้อ — **gate install สำเร็จ,
  ไม่มี patch นอกที่ติดตั้ง, ERROR=0** (WARN = noise UnityExplorer ชุดเดิม)
- **census (ndjson `20261030-040429`):** `h=883, t=79` · hist = **protag 1 + spe 170 + sect 562 +
  wanderer 150 + f2 0** — ตรง baseline T0 **ทุกประเภท เป็นรอบที่ 2 ติดกัน** · TempHeros **79**
  (แถวข้อมูล: T0=87, รอบ 1=81, รอบ 2=79) · factory C=961 (A/B=0) · heroNum Σ=88 ไม่ถูกย่อ
- **เซฟ verify: PASS** — changed=1 = `PlayerprefData.dat` เท่านั้น · `SaveSlot*` ไม่ถูกเขียน · real 0/0/0
- **hash:** เกม 2 ไฟล์ + Mods DLL ตรงเดิมทุก byte
- หลักฐานรอบ: `ml-log-round2.log`, `summary-round2.json`, `sp5t0-round2.ndjson`

## ผลตรวจรอบ 3 (04:17)

- **ML log:** `Sp5T0 installed … patches=7 (before=0)` + `Sp5Npc gate installed` (04:16:02) · ตอนปิด
  `Sp5T0 removed: events=3002 errors=0 unattributed=0 outside=0 phaseStuck=0 rebaseline=0 readfail=0
  ownedLeft=0` + `probe: unpatched (before=1 after=0)` — **[ERROR] = 0 บรรทัดทั้ง log**
- **census (ndjson `20261030-041602`):** `h=883, t=84` · hist = **protag 1 + spe 170 + sect 562 +
  wanderer 150 + f2 0** — ตรง baseline T0 ทุกประเภท **เป็นรอบที่ 3 ติดกัน** · TempHeros **84**
  (สะสม 3 รอบ: 81/79/84 · รวม T0=87) · factory C=966 (A/B=0) · heroNum Σ=88 ไม่ถูกย่อ
- **เซฟ verify: PASS** — changed=1 = `PlayerprefData.dat` เท่านั้น · `SaveSlot*` ไม่ถูกเขียน · real 0/0/0
- **hash:** เกม 2 ไฟล์ + Mods DLL ตรงเดิมทุก byte
- หลักฐานรอบ: `ml-log-round3.log`, `summary-round3.json`, `sp5t0-round3.ndjson`

## ขั้น 6 — คืนสภาพ (04:17–04:18)

1. **restore เซฟ:** `save-split restore --label SP5NPC-N3-20261030-035500` — `Save` = โฟลเดอร์จริง renamed
   กลับ (ไม่เคยถูกเขียน), **restored vs real baseline = 0/0/0 EXACT**, สำเนาทดสอบเก็บ
   `coop-test/SP5NPC-N3-20261030-035500/Save.used-20261030-041755` · หลัง restore: 57 ไฟล์ digest
   `df8e5a11beed` + **`leftovers: none`** ✅
2. **rollback DLL:** จาก `.prev-6a94abb5-20261030-035500`/`.pdb.prev-856d3bed-20261030-035500` →
   `Mods\LongYinCoop.dll` = **`6a94abb5…`** + `.pdb` = **`856d3bed…`** (rehash ตรงเป๊ะ) ✅
3. **pref:** `Sp5T0 = true` → **`false`** (บรรทัด 15) — `MelonPreferences.cfg` กลับ **`976ccfb9…`** =
   hash เดิมก่อน session **ทุก byte** ✅ (`TimeV2a=false`, `AllowRemoteInput=false` คงเดิม)
4. `net.cfg` = `12541f34…` ไม่ถูกแตะตลอดเซสชัน (ไม่มี key `npcPolicy` = off ตลอด stage ตามมติ) ✅
5. เกม 2 ไฟล์ = `33c56def…`/`451311d4…` ตรง VERSION.md ตลอด ✅

## สรุป T1 รายเกณฑ์ (ตารางจากเอกสารคำขอ `plans/sp5-npc-n3-approval-request.md`)

| เกณฑ์ T1 | วิธีวัด | ผล (×3 รอบ) |
|---|---|---|
| ไม่มี patch นอกที่ติดตั้ง (log ยืนยัน) | ML log: `Sp5Npc gate installed` + ไม่มีบรรทัด patch อื่น + owned count ตรง | **PASS ×3** — gate install สำเร็จทุกรอบ (ไม่มี install ล้ม) · probe 7 patches · ตอนปิด `outside=0 unattributed=0 ownedLeft=0 errors=0` ทุกรอบ · `[ERROR]`=0 บรรทัด |
| census/จำนวน/กระจาย ใน band RNG ของ baseline (≥3 รอบ) | probe `Sp5T0` (ตัวเลือกที่ผู้ใช้เลือก) — ndjson worldcount | **PASS ×3** — `h=883` ทุกรอบ **และตรง baseline T0 (883) ทุกประเภท**: protag 1 / spe 170 / sect 562 / wanderer 150 / f2 0 (เหมือนเป๊ะ 3 รอบติดกัน) · `h` นิ่งตลอด = ไม่มีการลบหลังสร้าง · TempHeros 81/79/84 (T0=87 — RNG worldgen, ไม่ใช่ Heros census) · factory เฉพาะ overload C (963/961/966, A/B=0) |
| hash ไฟล์เกม/ตารางไม่เปลี่ยน | hash เกม 2 ไฟล์ก่อน/หลัง · `SaveSlot*` ไม่ถูกเขียน | **PASS ×3** — เกม 2 ไฟล์ตรงทุก byte ทุกจุดวัด · heroNum Σ=88 ไม่ถูกย่อ (gate Off ยืนยันจาก census sect=562) · `SaveSlot*` ไม่ถูกเขียนเลย (verify PASS ทุกรอบ · drift = `PlayerprefData.dat` อย่างเดียวตามคาด) |

**เงื่อนไขหยุดของผู้ใช้:** ไม่มีข้อใดกระตุ้น — hash ตรง snapshot ทุกจุด · gate install ไม่ล้ม ·
ไม่มี patch นอกที่ติดตั้ง · `SaveSlot*` ไม่ถูกเขียน · ไม่พบการแก้จาก session อื่น (session-start และระหว่างทาง)

**หมายเหตุหลังจบ 3 รอบ (สำหรับ N4):** Heros census เชิงโควตา = deterministic (883 เท่ากันทุกรอบ/ตรง T0)
— ฐานเทียบ T4 ของ N4 จึงแน่น: ถ้า gate Light ×0.5 ทำงาน คาด sect ลดจาก 562 (~281 ตัว ≈ 31.8% ตาม
N1-3 — ข้อสันนิษฐาน linear ยังต้องวัดจริง) ขณะ spe 170/protag 1 ต้องคงเดิม · TempHeros = ตัวแปร RNG
(79–87 จาก 4 จุดข้อมูล) ไม่ใช่ตัวชี้วัดของ gate

**สถานะจบเซสชัน:** คืนสภาพครบทุกข้อตามเงื่อนไขผู้ใช้ · **ไม่ commit ไม่ push ไม่ tag ไม่ปิด stage** —
ไฟล์ evidence นี้คง untracked ใน mod repo working tree รอมติ commit/push รอบถัดไป · stage N3 = วัดครบ
3/3 รอบ PASS รอมติผู้ใช้ปิด stage/tag แยก (ตามกติกา 2) · N4–N6 ยังไม่อนุมัติ
