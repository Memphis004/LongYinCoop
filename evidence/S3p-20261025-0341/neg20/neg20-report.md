# NEG20 Report — negative 20 เคส บน build `f2ad30cd` (S3 prerequisite, Gate 2 ซ้ำ)

Label split: `NEG20-f2ad30cd-20261025-202331` · baseline บังคับ: `coop-test/baselines/20261025-201545` (digest `588839490e5b418ae6e20172ea7af21916c9afa3a935c99525984fc9e16a0485`) · runner: `tools/negative-tests.py` @ commit `a7b1bc1` · plans แม็พ: `plan-w1-mapped.json`, `plan-w2-mapped.json` (provenance `id_mapping` ในไฟล์ — เคส/กลุ่ม/expect ไม่แก้ เฉพาะ ID) · วันที่รัน: 2026-10-25 20:23–22:09

**สรุปบรรทัดเดียว:** ยิงจริง 12 เคส **ปฏิเสธ 12/12** ทุกเคส stateHash ก่อน/หลังเหมือนเดิม · **PASS 12 · NOT-RUN 8 · FAIL 0 · UNKNOWN 0** · จบศึกปกติ (state=Loot) · restore EXACT · **Gate 2 ยังไม่สรุป 20/20 — รอมติข้อยกเว้น NOT-RUN**

## 0. เงื่อนไขก่อนรัน (OBSERVED)

| รายการ | ค่า |
|---|---|
| DLL deployed | `f2ad30cd2090a63879a74605c32efc999163b9d1873da6a68bd875e872f112a8` (ตรวจ pre-split / หลัง split / ขณะรัน) |
| split | EXACT ตาม baseline ที่อนุมัติ (0/0/0) · real → aside · preflight-launch READY 11/11 |
| config | `DevCommands=true` · `AllowRemoteInput=false` (สภาพพัก — runner เปิด/ปิดเองช่วงรัน) |
| session | boot 21:41:29 PID 37556 · โหลด `.\Mods\LongYinCoop.dll` ชุดเดียว (ไม่มี shadow — `.dll` โหลดได้ใน Mods มี 3 ตัวเท่าเดิม) · heartbeat `err=0` ตลอด session |
| marker build | `hashText = battleframe/v2` (ยืนยันครั้งแรกตอนเข้าศึก 21:46:55 — surface คืน None นอกศึก) |
| หมายเหตุ | session 20:57–21:41 (ก่อน session ปัจจุบัน) อยู่บนสำเนาที่ mount ไม่เข้าศึก ปิดสะอาด — เซฟจริง aside ไม่ถูกแตะ |

## 1. ผลรายเคส

**w1 — `WaitPlayerInput`, active=`0:0`, เทิร์น 0 (รัน 21:50:13–21:50:23 · base_hash `979e03ffd265aec1`)**

| # | group | cmd | args (แม็พแล้ว) | ผล | mod ตอบ |
|---|---|---|---|---|---|
| 1 | DISARMED | move | `0:0`→(1,0) | **PASS** | `DISARMED: run spike arm <seconds> first` |
| 2 | DISARMED | attack | `0:0`→`1:10034` (arm 1s+wait 2.2s) | **PASS** | `DISARMED: run spike arm <seconds> first` |
| 3 | NOT_ACTIVE | move | `0:51`→(4,0) | **PASS** | `unit 0:51 is not the active unit (0:0)` |
| 4 | NOT_ACTIVE | move | `0:86`→(11,0) | **PASS** | `unit 0:86 is not the active unit (0:0)` |
| 17 | STATE | attack | `0:0`→`1:10034` | **PASS** | `NOT_ATTACK_MODE: nowActiveState=WaitPlayerInput` |
| 18 | STATE | attack | `0:0`→`1:10034` | **PASS** | `NOT_ATTACK_MODE: nowActiveState=WaitPlayerInput` |
| 20 | GRID | move | `0:0`→(99,99) | **PASS** | `no grid cell at (99,99)` (grid จริง 20×19) |

**w2 — `WaitAttacking`, active=`0:0`, target `1:10034` มีชีวิต (รัน 22:03:24–22:03:32 · base_hash `168963240f3466a7`)**

| # | group | cmd | args (แม็พแล้ว) | ผล | mod ตอบ |
|---|---|---|---|---|---|
| 5 | NOT_ACTIVE | attack | `0:51`→`1:10034` | **PASS** | `unit 0:51 is not the active unit (0:0)` |
| 6 | NOT_ACTIVE | attack | `0:86`→`1:10034` | **PASS** | `unit 0:86 is not the active unit (0:0)` |
| 15 | STATE | move | `0:0`→(1,0) | **PASS** | `nowActiveState=WaitAttacking (expected WaitPlayerInput)` |
| 16 | STATE | move | `0:0`→(0,1) | **PASS** | `nowActiveState=WaitAttacking (expected WaitPlayerInput)` |
| 19 | SELF | attack | `0:0`→`0:0` | **PASS** | `target 0:0 is the active unit` |
| 7 | OUT_OF_RANGE | attack | `0:0`→`1:10034` | **NOT-RUN** | — (ไม่ส่ง) |
| 8 | OUT_OF_RANGE | attack | `0:0`→`1:10034` | **NOT-RUN** | — (ไม่ส่ง) |
| 9 | OUT_OF_RANGE | attack | `0:0`→`1:10034` | **NOT-RUN** | — (ไม่ส่ง) |
| 10 | OUT_OF_RANGE | attack | `0:0`→`1:10034` | **NOT-RUN** | — (ไม่ส่ง) |

**w3 — ไม่รัน (มติผู้ใช้ทางเลือก ก)**

| # | group | cmd | ผล |
|---|---|---|---|
| 11–14 | TARGET_DEAD | attack ×4 | **NOT-RUN** — ไม่มีคำสั่งถูกส่ง |

**รวม: PASS 12 · NOT-RUN 8 · FAIL 0 · UNKNOWN 0** (planned 20 · ยิงจริง 12 · ไม่มี retry ทุกเคสส่งครั้งเดียว)

## 2. เหตุผล NOT-RUN (แยกชัด — ห้ามนับเป็น PASS)

- **เคส 7–10 (OUT_OF_RANGE):** `premise-unverifiable-range` — spike status surface คืนเฉพาะ `attackRangeCount` **ไม่ expose พิกัดช่องระยะโจมตี** ⇒ ข้อเท็จจริง "target อยู่นอกระยะ" ยืนยันไม่ได้จากข้อมูลสด ผู้ตรวจ (runner) จึง fail-closed ไม่ยิง · ตรวจยืนยันจาก NDJSON: `prechecks` ผ่าน 4 ข้อแรก (attacker active/alive, target exists/alive) แล้วบล็อกที่ `range-membership-verifiable=false` · **ทางแก้เชิงโครงสร้าง = ให้ mod expose range cells (แก้ source — ต้องอนุมัติแยก)**
- **เคส 11–14 (TARGET_DEAD):** เชิงโครงสร้าง — ศึกที่ใช้ทดสอบมีศัตรูมีชีวิต**ตัวเดียว** (`1:10034`) การฆ่ามัน = จบศึกทันที ⇒ สถานะที่ premise ต้องการ ("มีศัตรูตาย ≥1 และยังมีศัตรูมีชีวิต ≥1 พร้อมกัน ขณะ WaitAttacking active=0:0") **เกิดขึ้นไม่ได้** ในศึกนี้ · มติผู้ใช้เลือกไม่รัน (ห้ามฝืนสร้างสถานการณ์) · จะรันได้เฉพาะเมื่อเข้าศึกที่มีศัตรู ≥2 ตัวและฆ่าได้ 1 ตัวโดย `0:0` ยังเดินเข้าจังหวะเล็งได้

## 3. PASS criteria ที่ใช้ (ทุกเคสที่ยิง)

mod ปฏิเสธ (reply ไม่ ok) **AND** ไม่มี applied flag **AND** stateHash ก่อน/หลังเหมือนเดิม **AND** error code ตรง expect — ตรวจซ้ำรายเคสใน NDJSON (`reply_outcome`, `hash_before/after`, `code_ok`) · stateHash รวมทั้งหน้าต่าง: `979e03ffd265aec1` (w1) และ `168963240f3466a7` (w2) เท่าค่าก่อนรัน — **สถานะเกมไม่ถูกแตะจากคำสั่งทั้ง 12**

## 4. Cleanup หลังแต่ละหน้าต่าง (OBSERVED จาก ML log)

- w1: `pref set allowRemoteInput=True` 21:50:13 (startup) → `False` 21:50:23 (cleanup) + `spike disarm` ทั้งสองจังหวะ · w2: `True` 22:03:24 → `False` 22:03:32 + `spike disarm` ✓
- `cleanup_ok=true` ทั้งสองหน้าต่าง (จาก runner summary JSON)
- พฤติกรรม log ที่บันทึกไว้: บรรทัด `spike attack/move` ปรากฏเฉพาะคำสั่งที่**ผ่าน guard** (เส้นทางยิงจริง) — คำสั่งที่ถูกปฏิเสธไม่ถูก log ฝั่ง mod; หลักฐานการปฏิเสธ = NDJSON ของ runner (reply พร้อม error code ทุกเคส)
- heartbeat `err=0` ต่อเนื่องตลอด session (ตรวจล่าสุด `ticks=196800 err=0`)

## 5. จบศึก + ปิดเกม + restore (OBSERVED)

- จบศึกปกติ = **MET**: `battle:end type=DeathFight state=Loot player=4 enemy=1 turn=9` (22:06:33 — จบถึงหน้า loot, ไม่ใช่ teardown state=None) · ผู้ใช้เล่นจนจบเอง (คำสั่งโจมตีสุดท้ายมาจากผู้เล่น ไม่ใช่ runner)
- ปิดเกมสะอาด: heartbeat ต่อเนื่องถึง 22:06:40 → `probe: unpatched` → `Preferences Saved!` · **ไม่มี crash/exception** · Event 1000/1001 ตั้งแต่ 21:41 = ไม่พบ
- restore label `NEG20-f2ad30cd-20261025-202331`: drift สำเนา = `PlayerprefData.dat` ไฟล์เดียว (volatile — ผู้ใช้ไม่ได้เซฟ · **`SaveSlot*` ทั้ง 55 ไฟล์ตรงครบ ของทรีรวม 57 ไฟล์ = 55 SaveSlot + pref + steam_autocloud**) · สำเนาเก็บ `coop-test/NEG20-f2ad30cd-20261025-202331/Save.used-20261025-220912` (57 ไฟล์, digest `87b6b0d7134e7753582d21f79ea5bef1a33f96a5c1ff070c61f602afed06a0c6`)
- **หลัง restore: `Save` 57 ไฟล์ · changed=0 missing=0 added=0 · full digest `588839490e5b418ae6e20172ea7af21916c9afa3a935c99525984fc9e16a0485` = EXACT กับ baseline 20261025-201545 · `leftovers: none`**

## 6. การจำแนก

**OBSERVED:** ทุกค่าใน §0–§5 (จาก runner console/NDJSON/summary JSON, ML log, Event log, save-split outputs, sha256)

**INFER:** guard ของ mod ทำงานครบลำดับเดียวกันทั้งสอง build (error code/เงื่อนไขตรงกันทุกเคสที่เทียบได้กับรอบ build เก่า — แต่รอบเก่ารันบน build `5d7b7cc5` คนละ session จึงเป็นการเปรียบเทียบข้าม session ไม่ใช่การควบคุมตัวแปรเดียว) · การปฏิเสธไม่บริโภคสิทธิ์เทิร์น (`turnAttacked=false` หลัง w2)

**UNKNOWN:** ต้นเหตุ crash ของ Gate-2 (04:14) ยังไม่ถูกตัดสิน — ผลบวกของ NEG20 ไม่พิสูจน์เรื่องนั้น (NEG20 ไม่มีเคส accepted-attack นอกจากผู้ใช้เล่นเอง) · พฤติกรรม OUT_OF_RANGE จริงบน build นี้ (ไม่ยิง = ไม่วัด) · มุมมอง range cells ที่ยังไม่ expose

## 7. Gate 2 — สถานะ

เกณฑ์เดิม (STAGEPLAN §6): "ยิงคำสั่งผิดกติกา 20 แบบ → ปฏิเสธ 20/20 + stateHash ไม่เปลี่ยน" — ผลจริง: **12/12 ที่ยิงได้ถูกปฏิเสธครบ + hash ไม่เปลี่ยน · อีก 8 เคส NOT-RUN** (4 = ข้อจำกัด surface ด้าน range cells, 4 = โครงสร้างศึกศัตรูเดียว) ⇒ **ยังไม่สรุปว่าผ่าน 20/20 — รอมติผู้ใช้เรื่องข้อยกเว้น/ทางเลือกเพิ่มเติมแยกต่างหาก** · ไม่เริ่ม S3

## 8. ทะเบียนหลักฐาน (`evidence/S3p-20261025-0341/neg20/`)

| ไฟล์ | เนื้อหา |
|---|---|
| `session-start-20261025-2145.md` | ตรวจ startup 3 ข้อ (load/shadow, marker deferred, heartbeat) + บันทึก session 20:57 |
| `status-sessionstart/battleentry/beforew2/prew2/afterw1/afterw2-*.txt` | status สดทุกจุดตรวจ (6 ไฟล์ พร้อม timestamp ในชื่อไฟล์) |
| `plan-w1-mapped.json`, `plan-w2-mapped.json` | plans ที่ใช้รันจริง + `id_mapping` provenance |
| `negatives-w1.json/.ndjson`, `negatives-w2.json/.ndjson` | runner summaries + audit trails (pre-fire/post records, flush ราย record) |
| `run-w1-console-*.log`, `run-w2-console-*.log` | console output ของ runner (ถูก `.gitignore *.log` — เก็บบนดิสก์) |
| `ML-26-10-25_21-41-29.post-close-*.log` (sha `075ccc48…`), `Player.log.post-close-*.log` (sha `cb2b27a0…`) | log เต็ม post-close (ถูก `.gitignore *.log` — sha บันทึกไว้) |

## 9. ข้อจำกัดของรอบนี้

- ระยะโจมตี (เคส 7–10) และ TARGET_DEAD (เคส 11–14) ยังไม่ถูกวัดบน build นี้ — สาเหตุและทางเลือกตาม §2
- ผลทั้งหมดเป็นการทดสอบครั้งเดียวต่อเคสบนศึก/layout หนึ่ง — ไม่ขยายผลไปทุก layout/state
- ศึกที่ใช้มี grid 20×19, ศัตรู 1 ตัว — GRID case #20 ใช้ (99,99) นอก grid จริง; เคสอื่น premise ตรวจจากข้อมูลสดทั้งหมด
