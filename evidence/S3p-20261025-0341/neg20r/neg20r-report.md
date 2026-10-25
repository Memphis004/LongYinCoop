# NEG20R Report — negative 20/20 retest บน build `1b4c17aa` (Gate 2 closure)

Campaign: split `NEG20R-1b4c17aa-20261025-230950` (w1/w2/w3) + split `NEG20R-case7-20261025-234850` (เคส 7 retest) · runner: `tools/negative-tests.py` @ `a7b1bc1` + patch `range_verdict_from_state` · build: **`1b4c17aa6a934b25c884f62dbab59a795a8bbadf60fd2fc869ee0927fb3f0177`** (PDB `dcd78ebd…1e17`) · baseline บังคับ: `coop-test/baselines/20261025-201545` (digest `588839490e5b418ae6e20172ea7af21916c9afa3a935c99525984fc9e16a0485`) · วันที่รัน: 2026-10-25 23:09 – 2026-10-26 00:02

**สรุปบรรทัดเดียว:** negative 20 เคสครบบน binary เดียว — **PASS 20 · NOT-RUN 0 · FAIL 0 · UNKNOWN 0** · ทุกเคสยิงครั้งเดียว ทุก PASS ครบ 4 เงื่อนไข (mod ปฏิเสธ + ไม่ applied + stateHash ก่อน/หลังเหมือนเดิม + error code ตรง expect) · **Gate 2 = PASS ตามมติผู้ใช้ 2026-10-26** — โดยแยกข้อเท็จจริงว่า **ต้นเหตุ crash เดิม (Gate-2 incident 04:14) ยัง UNKNOWN**

## 0. พื้นหลังและเหตุผลของ retest

- NEG20 รอบแรก (build `f2ad30cd`, รายงาน `../neg20/neg20-report.md`): **PASS 12 · NOT-RUN 8** — NOT-RUN 2 กลุ่ม: (a) เคส 7–10 ตรวจระยะไม่ได้ (surface ไม่ expose range cells) (b) เคส 11–14 ศึกศัตรูเดียว (โครงสร้าง)
- แก้ที่ต้นเหตุด้วย patch **rangecells** (read-only instrumentation): expose `attackRangeCells` ใน status จาก list เดียวกับ attack path (`bc.attackRangeGridUnits` — AttackCommand.ListHas) + runner derive verdict จาก cells สด (consistency gate `len == attackRangeCount`, fail-closed None) — Phase 1–2 audit + deterministic build provenance ครบ
- w3 เปิดทางด้วยศึกใหม่ที่มีศัตรู 4 ตัว (โครงสร้างเดิมบังคับศัตรูเดียว)

## 1. ผลรายเคส (20/20 — binary เดียว)

**w1 — `WaitPlayerInput` (7/7 PASS · base_hash `3ee48391b750a15b`)**

| # | cmd | mod ตอบ |
|---|---|---|
| 1 | DISARMED move | `DISARMED: run spike arm <seconds> first` |
| 2 | DISARMED attack (`1:10181`) | `DISARMED: run spike arm <seconds> first` |
| 3 | NOT_ACTIVE move (`0:51`) | `unit 0:51 is not the active unit (0:0)` |
| 4 | NOT_ACTIVE move (`0:86`) | `unit 0:86 is not the active unit (0:0)` |
| 17 | STATE attack | `NOT_ATTACK_MODE: nowActiveState=WaitPlayerInput` |
| 18 | STATE attack | `NOT_ATTACK_MODE: nowActiveState=WaitPlayerInput` |
| 20 | GRID move (99,99) | `no grid cell at (99,99)` (grid จริง 20×20) |

**w2 — `WaitAttacking` (8 PASS + 1 NOT-RUN→ปิดด้วย retest · base_hash `790ae07dfed2581b`)**

| # | cmd | ผล | mod ตอบ |
|---|---|---|---|
| 5 | NOT_ACTIVE attack (`0:51`) | **PASS** | `unit 0:51 is not the active unit (0:0)` |
| 6 | NOT_ACTIVE attack (`0:86`) | **PASS** | `unit 0:86 is not the active unit (0:0)` |
| 15 | STATE move | **PASS** | `nowActiveState=WaitAttacking (expected WaitPlayerInput)` |
| 16 | STATE move | **PASS** | `nowActiveState=WaitAttacking (expected WaitPlayerInput)` |
| 19 | SELF attack (`0:0`) | **PASS** | `target 0:0 is the active unit` |
| 7 | OUT_OF_RANGE attack (`1:10181` @(13,16)) | **NOT-RUN** (ครั้งแรก) | cells สดชี้ target **อยู่ในระยะ** → `premise-mismatch` → **runner ปฏิเสธยิง** — กลไกกันเหตุ Gate-2 ทำงานจริง |
| 8 | OUT_OF_RANGE attack (`1:10183` @(18,16)) | **PASS** | `OUT_OF_RANGE: the game did not accept the click (attackComfirmed=false)` — ยิงจริงครั้งแรกที่ยืนยันนอกระยะจาก cells สด (32 ช่อง, len==count) |
| 9 | OUT_OF_RANGE attack (`1:10184` @(6,18)) | **PASS** | เดิม |
| 10 | OUT_OF_RANGE attack (`1:10117` @(16,16)) | **PASS** | เดิม |

**เคส 7 retest — split ใหม่ `NEG20R-case7-20261025-234850` (1 PASS · base_hash `5934a70833c5d934`)**

- Gate สด: `attackRangeCells` 79 ช่อง (len==count) · เป้าพิสูจน์นอกระยะได้ 2 ตัว (`1:10183` @(0,18), `1:10117` @(14,18)) · เป้าในระยะ 2 ตัว (`1:10181`, `1:10184`)**ไม่ถูกยิง**
- เลือก `1:10183` @(0,18) (ตัวแรกตามลำดับ units ที่พิสูจน์นอกระยะได้ — provenance ใน `plan-case7-mapped.json`)
| # | cmd | ผล | mod ตอบ |
|---|---|---|---|
| 7 | OUT_OF_RANGE attack (`1:10183`) | **PASS** | `OUT_OF_RANGE: the game did not accept the click (attackComfirmed=false)` — stateHash เหมือนเดิม |

**w3 — `WaitAttacking` + ศพจริง + ศัตรูมีชีวิตเหลือ (4/4 PASS · base_hash `34cc1fa954dba3d0`)**

- Gate: ศพ **2 ตัว** ยืนยัน signature ครบ (`1:10181`, `1:10117` — `unitAlive=false` + `hp=0` + `(-1,-1)` + `alive=false`) · ศัตรูมีชีวิตเหลือ 2 (`1:10183`, `1:10184`) · active=`0:0` (รอบแรก active=`0:51` → NOT-RUN ตามกติกา → ผู้ใช้เล่นแก้ state → Gate รอบสองผ่าน)
- เลือกศพ `1:10181` (ศพแรกที่ยืนยัน — rationale ใน `plan-w3-mapped.json`)
| # | cmd | ผล | mod ตอบ |
|---|---|---|---|
| 11–14 | TARGET_DEAD attack ×4 (`1:10181`) | **PASS ทั้ง 4** | `TARGET_DEAD: spike attack: target 1:10181 is not alive` — stateHash เหมือนเดิมทุกเคส |

## 2. รวม 20 เคส: **PASS 20 · NOT-RUN 0 · FAIL 0 · UNKNOWN 0**

- ทุกเคสยิงครั้งเดียว ไม่มี retry · ไม่มี FAIL/UNKNOWN/crash/soft-lock ตลอด campaign
- stateHash ไม่เปลี่ยนทุกเคสที่ยิง (แต่ละเคสเทียบก่อน/หลังของตัวเอง)
- NOT-RUN ระหว่างทาง (เคส 7 ครั้งแรก, Gate w3 รอบแรก active=0:51) ถูกแก้ด้วยการเล่นปกติของผู้ใช้ + Gate ใหม่ — **ไม่มีการฝืนสร้างสถานการณ์ ไม่มีการนับ NOT-RUN เป็น PASS**

## 3. Gate 2 — สถานะ (มติผู้ใช้ 2026-10-26)

- เกณฑ์เดิม (STAGEPLAN §6): "ยิงคำสั่งผิดกติกา 20 แบบ → ปฏิเสธ 20/20 + stateHash ก่อน/หลังไม่เปลี่ยน" — **ครบตามเกณฑ์บน binary `1b4c17aa`**
- **Gate 2 = PASS** (มติผู้ใช้ 2026-10-26 ตามรายงานนี้)
- **แยกข้อเท็จจริง:** ต้นเหตุ crash ของ incident เดิม (2026-10-25 04:14, `UnityPlayer.dll+0x1322c1e`, เกิดขณะรัน negative บน build `f2ad30cd`) **ยัง UNKNOWN** — ผล negative ที่ผ่านทั้งหมดเป็นการทดสอบคำสั่งที่**ถูกปฏิเสธ** + คลิกที่เกมไม่รับ ไม่ได้ทดสอบเส้นทาง accepted-attack ที่ incident เกิด (accepted-attack ถูกทดสอบแยกใน A/B สองรอบ: build เก่าและใหม่ ไม่พบ crash 1 ครั้ง/รอบ — ยังไม่พิสูจน์ความปลอดภัย)

## 4. ข้อจำกัด

- ผลทั้งหมดเป็นการทดสอบครั้งเดียวต่อเคส บน layout ศึก 2 แบบ (ศัตรู 1 ตัว + ศัตรู 4 ตัว) — ไม่ขยายผลทุก layout
- OUT_OF_RANGE พิสูจน์ด้วยเป้าจริง 3 ตัว (เคส 8–10) + retest (เคส 7) — ครบ 4 เคสตามแผน
- w3 ใช้ศพ `1:10181` เดียวทั้ง 4 เคส (ศพ `1:10117` มีอยู่ไม่ได้ใช้)
- patch rangecells ยังไม่ commit (source ณ HEAD `8fe33ce` + uncommitted patch) — build `1b4c17aa` deploy อยู่ rollback `.prev-f2ad30cd`

## 5. Post-close ทั้งสอง split (OBSERVED)

| label | จบศึก | drift สำเนา | restore | EXACT |
|---|---|---|---|---|
| `NEG20R-1b4c17aa-20261025-230950` | `battle:end state=End player=4 enemy=4 turn=19` (23:43:26) | `PlayerprefData.dat` เท่านั้น (SaveSlot 0) | ✓ | 57 ไฟล์ `58883949…0485` EXACT · leftovers none |
| `NEG20R-case7-20261025-234850` | `battle:end state=Loot player=4 enemy=4 turn=15` (00:01:09) | `PlayerprefData.dat` เท่านั้น (SaveSlot 0) | ✓ | 57 ไฟล์ `58883949…0485` EXACT · leftovers none |

- ไม่มี crash/exception · Event 1000/1001 ไม่พบทั้งสอง session · สำเนา log post-close hash ตรงต้นทาง (`12ee3e6d…`, `fe617ade…` / `120c6ef3…`, `15e110fd…`)
- สภาพพักปัจจุบัน: cfg `269b571c…` (`AllowRemoteInput=false`) · ไม่มี pipe · ไม่มี process · DLL deploy = `1b4c17aa…` (rollback `.prev-f2ad30cd`)

## 6. ทะเบียนหลักฐาน (`neg20r/` — 30 ไฟล์)

- plans แม็พ ×4 (`plan-w1/w2/w3/case7-mapped.json` — `id_mapping` provenance ทุกไฟล์ รวมเหตุผลการเลือกศพ/เป้า)
- runner outputs: `negatives-w1/w2/w3/case7.json` + `.ndjson` (pre-fire/post records, flush ราย record)
- console logs ×4 (`.log` — gitignore ตาม convention)
- status snapshots ×10 (session-start / battleentry / prew2 / afterw1 / afterw2 / prew3 ×2 / afterw3 / gate-case7 / aftercase7)
- post-close log archives ×2 ชุด (ML + Player, sha ตรงต้นทาง)
- สำเนาเซฟทดสอบ: `coop-test/NEG20R-*/Save.used-*` (นอก Git)

## 7. การจำแนก

**OBSERVED:** ทุกค่าใน §0–§5 จากไฟล์หลักฐานจริง
**INFER:** guard ของ mod ทำงานครบชุดเดียวกันทั้ง build `f2ad30cd` และ `1b4c17aa` (error codes/เงื่อนไขตรงกันทุกเคสที่เทียบได้) · คลิกนอกระยะไม่เปลี่ยน state (วัด 3 ครั้ง)
**UNKNOWN:** ต้นเหตุ crash Gate-2 เดิม · พฤติกรรม layout ศึกอื่น ๆ ที่ไม่ได้ทดสอบ
