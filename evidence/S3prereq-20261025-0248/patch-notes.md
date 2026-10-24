# S3 prerequisites — patch + re-run prep (ยังไม่ build/deploy · รออนุมัติ)

- มติผู้ใช้ 2026-10-25 (ส่วน D): อนุมัติให้ "เตรียม" source/tests — **ยังห้าม build/deploy/เปิดเกม** · การอนุมัตินี้ไม่ใช่การรับรองว่า S3 ผ่าน
- ข้อเท็จจริงที่ patch อ้าง (RUNTIME, S1.5f §3 + AGENTS): `HeroData.dead` **ไม่ใช่**ธงตายในศึก — ศพมี `dead=false`, `hp=0`, `BattleUnit.IsAlive=false`, grid `(-1,-1)` · `IsAlive` ยัง true ในเฟรมที่ hp→0

## แก้ source 2 ไฟล์ (uncommitted — ส่ง diff รอรีวิว)

### 1. `src/LongYinCoop/S1.5/TurnGate.cs` (อัปเดตตามรีวิวรอบ 1)
- **stateHash v2**: `alive` ต่อยูนิตเปลี่ยนเป็น **`AliveForS3(u, hp)`** = `SafeUnitAlive(u) && hp > 0f` (= `BattleUnit.IsAlive` + `hp`) — ใช้ทั้งใน canonical hash text และ `units[].alive` ของ `spike status`
- **`AliveForS3` = นิยามร่วม** (internal helper ใน TurnGate) — AttackCommand เรียกตัวเดียวกัน ⇒ เคสขอบ `IsAlive=true` แต่ `hp<=0` ว่าไม่ alive **ทั้งสองที่** (ข้อ 1 ของรีวิว)
- `HashVersion` `"battleframe/v1"` → **`"battleframe/v2"`** + หมายเหตุในโค้ด: v2 ใน evidence เก่า (S1.5c jit-dump-analysis §8) เป็นข้อเสนอคนละนิยามที่**ไม่เคยถูกใช้จริง** (ข้อ 4 ของรีวิว — คง v2 ตามมติ)
- `SafeUnitAlive` เปลี่ยน `private` → `internal` · `units[].unitAlive` **คงเดิม** = `SafeUnitAlive(u)`

### 2. `src/LongYinCoop/S1.5/AttackCommand.cs` (อัปเดตตามรีวิวรอบ 1+4)
- **guard เป้าหมาย**: `TargetAliveBefore` = **`TurnGate.AliveForS3(target, r.TargetHpBefore)`** (นิยามร่วมกับ stateHash — ข้อ 1) ⇒ ศพ/เป้า hp<=0 โดน `TARGET_DEAD: ... is not alive` จริง **ก่อน**ถึงเงื่อนไข grid
- **เฟส 2 ยืนยันผล (Tick)**: เลิกอ่าน `h.dead` — สำเร็จ = hp ลด หรือ `targetAliveKnown && !targetAlive`
- **`TargetAliveAfterKnown` (รีวิวรอบ 1+4)**: completion ใช้ **`AliveRule.CompletionAlive`** (pure) — `TargetAliveAfter` = `AliveForS3(isAlive, hp)` predicate เดียวกับ stateHash/guard · **Known=true เฉพาะเมื่ออ่าน isAlive + hp ครบ** · Target null / IsAlive หรือ hp อ่านไม่ได้ ⇒ **unknown (Known=false)** ห้ามตีความ false ว่า dead · ไม่เพิ่ม TARGET_UNREADABLE ในรอบนี้ (ค้างเป็นงานเสนอของ validator ใน S3)
- **propagation ยืนยัน (รีวิวรอบ 3)**: `TargetAliveAfterKnown` ออก IPC ทุก path (`Ok(cmd, ar)` / `Fail(cmd, err, ar)` ใส่ `result` เต็ม) + log `aliveKnown={…}` ในบรรทัด `spike attack` ของ Finish()

## ผลตรวจรอบ 4 (B: tests + compile-only Debug build)

- `dotnet run --project tests/S2Protocol.Tests` = **447 [PASS] / 0 FAIL** (S2a-3 179/0 + T0/T1) · เพิ่ม `CompletionAliveTests` 7 เคส (รวมอ่านไม่ได้ ⇒ unknown) — ผลจริงใน `tests-output-r4.txt`
- `dotnet build src/LongYinCoop -c Debug --no-incremental` = **Build succeeded · 0 Warning · 0 Error** (compile-only · ผลใน `debug-build-output.txt`) — ยืนยัน TurnGate.cs/AttackCommand.cs compile กับชนิดเกมจริงผ่าน interop · DLL เกิดที่ `bin/Debug/` เท่านั้น · **`Mods\LongYinCoop.dll` ยังเป็น `5d7b7cc5…` ไม่ถูกแตะ** (ยังไม่ deploy)

## negative tests 20 เคส (เตรียมพร้อม re-run บน build ใหม่)

- `plan-w1.json` (7) + `plan-w2.json` (9) + `plan-w3.json` (4) = **20 เคส** — คัดลอกจาก `evidence/S1.5f-20261021-0247/` เปลี่ยนเฉพาะ `label` เป็น `S3prereq-20261025-0248` (ตัวเคสไม่แก้)
- **ข้อเปลี่ยนพฤติกรรมที่คาดหลัง patch (เฉพาะเคส 11–14):** ใน S1.5f ศพผ่าน guard แล้วโดน `no grid cell at (-1,-1)` (ต้องเตรียม w3b แก้ expect) — **หลัง patch นี้ เคส 11–14 ต้องได้ `TARGET_DEAD` ตาม plan-w3 เดิม** ⇒ `plan-w3b` (grid-error variant) **ล้าสมัยสำหรับ build นี้** — ไม่คัดลอกมา
- stateHash ก่อน/หลังทุกเคสต้องเท่ากันเหมือนเดิม (runner เช็คอยู่แล้ว) — version v2 ไม่กระทบเพราะเทียบใน window เดียวกัน

## ผลตรวจ source/tests (2026-10-25 · ยังไม่ build Release/ยังไม่ deploy)

- `dotnet run --project tests/S2Protocol.Tests` = **RESULT PASS ทั้งหมด** — S2a-3 suite **179 PASS / 0 FAIL** + T0/T1 รวม **จำนวนใหม่หลังรีวิวรอบ 1 — ดูผลจริงล่าสุดในรายงาน**
- guard test เดิม "TurnGate.cs matches 57e68dc byte-exact" (freeze ของ S2a-2 ที่ D1 ถูกยกเลิก) **ถูกแทนด้วย pin ใหม่** ตามมติ S3 prerequisite นี้:
  - fixture ใหม่ `tests/S2Protocol.Tests/fixtures/TurnGate.s3prereq-20261025.cs.txt` sha256 `2600731e9043ea4f788493241c3490da5f4ca667a6cf7417d8c67d54bbc70ecc` (อัปเดตหลังรีวิวรอบ 1) (byte-exact กับ TurnGate.cs ที่แก้แล้ว)
  - fixture เก่า `TurnGate.before.cs.txt` (57e68dc, sha `737d4894…`) คงเดิม ตรวจ pin ต่อ (หลักฐานประวัติ)
  - เพิ่ม/ปรับ semantic checks (ข้อ 3 ของรีวิว — **static เท่านั้น ไม่แทน negative runtime tests ซึ่งยัง NOT RUN**): TurnGate = `battleframe/v2` + หมายเหตุ v2-เก่า-ไม่เคย-ใช้ + `AliveForS3(u, hp)` + นิยาม `return SafeUnitAlive(u) && hp > 0f;` + ห้าม `!hero.dead` · AttackCommand = guard ใช้ `TurnGate.AliveForS3(target, r.TargetHpBefore)` + มี `TargetAliveAfterKnown` + `(targetAliveKnown && !targetAlive)` + ห้าม `!targetHero.dead`/`dead = h.dead`
- ระหว่างพัฒนา: เจอ fail 1 ครั้งจาก comment ของผมเองมี literal `!hero.dead` ชน semantic check — แก้โดยเรียบเรียงคำใน comment (ไม่แตะเกณฑ์) แล้วผ่าน

## ไฟล์ที่เปลี่ยน (ทั้งหมด uncommitted — รอรีวิว)

| ไฟล์ | สถานะ | เนื้อหา |
|---|---|---|
| `src/LongYinCoop/S1.5/TurnGate.cs` | M | stateHash v2 (alive = IsAlive+hp) + SafeUnitAlive internal |
| `src/LongYinCoop/S1.5/AttackCommand.cs` | M | guard `target.IsAlive` + เฟส 2 ยืนยันด้วย IsAlive |
| `tests/S2Protocol.Tests/S2a2Tests.cs` | M | guard test ย้าย pin + semantic checks ใหม่ |
| `tests/S2Protocol.Tests/fixtures/TurnGate.s3prereq-20261025.cs.txt` | ใหม่ | fixture pin ใหม่ (sha `2600731e…` หลังรีวิวรอบ 1) |
| `evidence/S3prereq-20261025-0248/plan-w{1,2,3}.json` | ใหม่ | 20 เคสพร้อม re-run (label ใหม่) |
| `evidence/S3prereq-20261025-0248/patch-notes.md` | ใหม่ | ไฟล์นี้ |

## สิ่งที่ยังต้องทำหลังอนุมัติ (ไม่ทำในรอบนี้)

1. Commit patch นี้ + `dotnet build -c Release` + deploy DLL — ต้องอนุมัติก่อน
2. Re-run negative 20 เคสบน build ใหม่ (ต้อง save-split/preflight + ผู้ใช้เปิดเกม)
3. "จบศึกได้ตามปกติ" เป็นการบังคับในศึกรอบแรกของ S3 (ตาม prerequisite เดิม)

## พบเพิ่มระหว่างตรวจ (ยังไม่แก้ — นอกขอบเขต รอมติ)

- `src/LongYinCoop/S1/BattleSampler.cs:248` (`st.alive = !hero.dead`) และ `src/LongYinCoop/S1/S1Probe.cs:171` (`alive = hero != null && !hero.dead`) — S1-era samplers ใช้ธงเดียวกันที่ไม่ใช่ธงตาย · กระทบข้อมูล S1 capture เก่า ไม่กระทบ stateHash/attack path — เสนอให้ตัดสินแยก (แก้พร้อม round ถัดไป หรือทำเครื่องหมายไว้)
- S2 (`S2Sampler`/`S2Snapshot`/`ReplicaHash`) ใช้ `IsAlive` ถูกอยู่แล้ว — ไม่ต้องแก้ · ข้อสังเกต: audit ที่เทียบ `units[].alive` ของ spike status กับ `IsAlive` ของ S2 อาจต่างกันได้ ≤1 เฟรมในเฟรมที่ hp→0 (โดยการออกแบบของ v2)
