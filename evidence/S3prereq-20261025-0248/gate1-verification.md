# Gate 1 — Read-only Code Verification ก่อน Runtime Prerequisite (2026-10-25)

- ตรวจจาก source จริง @ `59276a4` (S3 source commit) + `ce3e06c` (VERSION.md docs) — `git diff 59276a4 -- src/ tests/` = ว่าง (working tree ตรง commit)
- **ไม่เปิดเกม · ไม่ deploy DLL (`Mods\` = `5d7b7cc5…` ตลอด) · ไม่แก้ source/config/save/baseline · ไม่ commit/tag/reset**
- สิ่งที่รัน: `dotnet run --project tests/S2Protocol.Tests` (exit 0) · `dotnet build src/LongYinCoop -c Debug --no-incremental` (exit 0, 0W/0E)

## A. AliveRule

| ข้อ | ผล | หลักฐาน |
|---|---|---|
| A1 implementation | **PASS** | `src/LongYinCoop/S3/AliveRule.cs:18-21` — `return isAlive && hp > 0f;` (pure ไม่อ้างชนิดเกม) |
| A2 fail-closed ตามข้อกำหนด | **PASS พร้อมข้อจำกัด** | เอกสารกำหนด = "NaN → false" (`AliveRule.cs:17`) · พฤติกรรมจริงตาม IEEE-754/C#: NaN→false ✓ · -∞→false ✓ · ติดลบ→false ✓ · 0f→false ✓ · -0.0f→false ✓ · **+∞→true (ไม่ fail-closed)** · Epsilon→true ✓ · **±∞ ยังไม่มีใน tests** (ช่องว่าง coverage — ดูความเสี่ยง) |
| A3 unit tests ครอบสำคัญ + ผลรันจริง | **PASS** | `S2a2Tests.cs` `AliveRuleTableTests` 9 เคส (NaN/0/eps/ติดลบ/true-false) + `CompletionAliveTests` 7 เคส — รันจริง exit 0 · **447 [PASS] / 0 FAIL** (S2a-3 179/0) |
| A4 แยก pure จากค่าจริงของเกม | **PASS (โครงสร้าง) / UNKNOWN (ค่าจริง)** | pure fn รับ bool+float (`AliveRule.cs:18`) · การอ่านค่าจริงอยู่ที่ `TurnGate.SafeUnitAlive` (TurnGate.cs:486-489 → `u.IsAlive`) และ `heroData.hp` (AttackCommand.cs:357-359) — **คุณภาพค่าจากเกมยัง UNKNOWN จนกว่า runtime** |

## B. hpRead / aliveKnown

| ข้อ | ผล | หลักฐาน |
|---|---|---|
| B1 hpRead = ผลการอ่าน แยกจากค่า | **PASS** | `AttackCommand.cs:359` — `try { hp = h.hp; hpRead = true; } catch { hp = -1f; }` — ตั้งเฉพาะเมื่อ assignment สำเร็จ |
| B2 sentinel ไม่ใช่หลักฐานสำเร็จ | **PASS** | เงื่อนไขสำเร็จ `AttackCommand.cs:369` บังคับ `hp >= 0f` (−1 เข้าไม่ได้) · `TargetHpDrop = hp >= 0f ? … : 0f` (:381) กัน sentinel · ข้อสังเกตเล็ก: timeout path รายงาน `TargetHpAfter=-1` ได้เมื่ออ่านไม่ได้ (Known=false บอกอยู่แล้ว — เป็นการรายงาน ไม่ใช่การตัดสิน) |
| B3 Known=true ต่อเมื่ออ่านสำเร็จ**จริง**ทั้งคู่ | **FAIL (ช่องโหว่ 1 จุด)** | `AliveRule.cs:30-36` บังคับ `isAliveRead && hpRead` ✓ · แต่ `AttackCommand.cs:353-354`: `isAlive = TurnGate.SafeUnitAlive(t); isAliveRead = true;` — **`SafeUnitAlive` กลืน exception เป็น false ภายใน** (TurnGate.cs:486-489: `try { return u != null && u.IsAlive; } catch { return false; }`) ⇒ IsAlive getter โยน → `isAliveRead=true` เหมือนอ่านสำเร็จ ⇒ ถ้า hp อ่านได้ → `known=true, aliveAfter=false` = **"ตายแบบ known" โดยไม่มีการอ่านสำเร็จจริง** · ลดความเสี่ยง: known=true ต้องผ่าน hpRead=true ด้วย และวัตถุที่ IsAlive โยนมัก heroData/hp โยนตาม → known=false — แต่คู่กรณี (IsAlive โยน + hp อ่านได้) ยังเป็นไปได้ |
| B4 ทุก branch | **PASS ยกเว้นข้อ B3** | t==null → known=false ✓ · heroData null (catch ที่ :356-358) → hpRead=false → known=false ✓ · hp โยน → hpRead=false ✓ · ปกติ → known=true ✓ — exception-in-IsAlive ตาม B3 |
| B5 ห้ามอนุมาน | **UNKNOWN ที่เกี่ยวกับ runtime** | เอกสารนี้ไม่อ้างผล log ตัวอย่าง — ทุกข้ออ้างบรรทัดโค้ด · ความน่าจะเป็นจริงของ B3 = UNKNOWN (ต้องวัด runtime) |

## C. stateHash v2 / `.dead`

| ข้อ | ผล | หลักฐาน |
|---|---|---|
| C1 predicate เดียวกัน | **PASS** | hash: `TurnGate.cs:292` `bool alive = AliveForS3(u, hp);` → wrapper :486-488 → `AliveRule.AliveForS3` · guard: `AttackCommand.cs:238` `TurnGate.AliveForS3(target, r.TargetHpBefore)` · completion: `AttackCommand.cs:366` → `AliveRule.CompletionAlive` → `AliveForS3` (:35) · `HashVersion="battleframe/v2"` (TurnGate.cs:31) |
| C2 `.dead` จุดที่เหลือ + callsites | **PASS (รายงานครบ)** | (1) `BattleSampler.cs:248` `st.alive = !hero.dead` → ผู้บริโภคเดียว `BattleSampler.cs:343` `uv.alive = st.alive` → capture view ของ S1 (ไฟล์ evidence ยุค S1) · (2) `S1Probe.cs:171` → รายงาน probe ผ่าน IPC (`IpcServer.cs:193/196/311` — คำสั่ง S1-era ยังลงทะเบียนอยู่) · **ไม่มีจุดอื่นใน src/** |
| C3 telemetry หรือตัดสินผล? | **PASS — telemetry/legacy ล้วน** | ทางตัดสินผลของ S2 (auditEqual) ใช้ `S2Audit.UnitsSum` ที่สร้างจาก **`S2Unit.IsAlive`** (`S2Snapshot.cs:143` `u.IsAlive ? '1' : '0'`) — **ไม่ผ่าน `.dead` และไม่ผ่าน `alive` ของ spike status เลย** · `st.alive`/probe `alive` ไม่ถูกนำไปเทียบ stateHash v2 หรือตัดสินผลใดในโค้ดปัจจุบัน |
| C4 ไม่กวาดแก้ | **PASS** | `git log` หลัง `59276a4` ไม่มีการแตะ `BattleSampler.cs`/`S1Probe.cs` — คงมติ "แก้เฉพาะจุดมีหลักฐานในขอบเขต" |

## D. Build/tests

| ข้อ | ผล | หลักฐาน |
|---|---|---|
| คำสั่ง/config ที่ใช้จริง | **PASS** | tests: `dotnet run --project tests/S2Protocol.Tests` (**exit 0**, 447 PASS/0 FAIL) · Debug: `dotnet build src/LongYinCoop -c Debug --no-incremental` โปรเจกต์ `src/LongYinCoop/LongYinCoop.csproj` (**exit 0**, 0 Warning/0 Error) — compile กับชนิดเกมจริงผ่าน interop |
| Debug แยกจาก Release | **PASS** | `bin/Release/LongYinCoop.dll` mtime **Oct 24 01:28** (ยุค S2a-3 — ไม่ถูก rebuild ในรอบนี้) · DLL ใหม่เกิดที่ `bin/Debug/` เท่านั้น |
| ไม่ deploy / ไม่แก้ VERSION | **PASS** | `Mods\LongYinCoop.dll` = `5d7b7cc5520ae784…` ตลอด (วัดซ้ำตอนปิด gate) · VERSION.md ไม่ถูกแตะหลัง `ce3e06c` |
| Purity | **PASS** | เกมไม่รัน · Save 57 ไฟล์ `7332fc93eb98` EXACT · `leftovers: none` · working tree ของ src/tests ตรง `59276a4` เป๊ะ |

## 1. Blocker ที่เหลือ

1. **B3 — `isAliveRead` ไม่แยก "อ่านสำเร็จ=false" จาก "อ่านไม่ได้"** (SafeUnitAlive กลืน exception) — เสนอแก้ในรอบถัดไปโดยให้ `SafeUnitAlive` แยกสถานะการอ่าน (เช่น `TryReadAlive(out bool ok)`) หรือยอมรับความเสี่ยงพร้อมบันทึก — **ต้องมีมติก่อน runtime ที่ยึดผล `TargetAliveAfterKnown` ตัดสิน**
2. **A2 (รอง) — `+∞` ⇒ alive=true ไม่ fail-closed และ ±∞ ไม่มีใน tests** — ความน่าจะเป็นที่เกมให้ hp=∞ = UNKNOWN; เสนอเพิ่ม `!float.IsInfinity(hp)` หรือเพิ่มเคส tests ในรอบแก้ B3 (รวมกัน one commit)
3. ไม่มี blocker อื่นฝั่ง `.dead` (C2–C4 PASS — telemetry ล้วน)

## 2. สถานะความพร้อม Runtime Prerequisite

**พร้อมแบบมีเงื่อนไข (CONDITIONALLY READY)** — โครงสร้าง predicate เดียว + tests + Debug compile ผ่านครบ · เงื่อนไข: (ก) มติ B3 (แก้ก่อน หรือยอมรับความเสี่ยงเป็นลายลักษณ์) (ข) งาน runtime ยัง NOT RUN ทั้งหมด: negative 20 เคสบน build ใหม่ + "จบศึกได้ตามปกติ" ในศึกรอบแรกของ S3 · ทั้งคู่ต้อง build Release + deploy + ผู้ใช้เปิดเกม (ประตูอนุมัติแยก)

## 3. แผน Runtime Prerequisite แบบแยกขั้น (เสนอ — ยังไม่เริ่ม)

- **ขั้น 0 (เตรียม)**: มติ B3 (+A2 ถ้ารวมแก้) → build Release → บันทึก sha ใหม่ใน VERSION.md (docs-only commit)
- **ขั้น 1 (deploy)**: สำรอง `Mods\LongYinCoop.dll` (5d7b7cc5) เป็น `.prev-…` → วาง DLL ใหม่ → ยืนยัน sha · rollback = คืนไฟล์สำรอง (ทดสอบแล้วใน S2a-3 งาน B)
- **ขั้น 2 (save safety)**: `save-split` + `preflight` (READY) + baseline `7332fc93eb98` → **ผู้ใช้เปิดเกมเอง** บนสำเนา · save-watch 57 ไฟล์ก่อน/หลังทุกหน้าต่างวัด
- **ขั้น 3 (negative 20 เคส)**: `tools/negative-tests.py --plan evidence/S3prereq-20261025-0248/plan-w{1,2,3}.json` — เคส 11–14 ต้องได้ `TARGET_DEAD` จาก guard จริง · stateHash ก่อน/หลังเท่ากันทุกเคส
- **ขั้น 4 (จบศึกได้ตามปกติ)**: ศึกรอบแรกของ S3 ต้องจบได้ (ไม่ค้าง/ไม่ crash) — เกณฑ์ตามแผน S3
- **ขั้น 5 (ปิด)**: restore เซฟ + คืน pref + รายงาน + tag ตามมติ

## 4. คำขออนุมัติ

ขออนุมัติ **เฉพาะ** ประเด็นตัดสินก่อนรัน: (1) B3 — แก้ SafeUnitAlive ให้แยกสถานะอ่าน หรือยอมรับความเสี่ยง (2) A2 — เพิ่ม IsInfinity/เคส ∞ หรือไม่ (3) เมื่อ (1)–(2) มีมติ จะขออนุมัติแยกสำหรับ build Release + deploy + เปิดเกมตามแผนขั้น 1–5 — **ยังไม่เริ่มอะไรจนกว่าจะได้รับคำสั่ง**
