# S2a-2-plan **rev4** — host sampler/sender (loopback) · มติ 2026-10-21 ข้อ 1–4 (รันขั้น 1–10 ได้)

**สถานะ: step 0 + fix round 2 เขียนโค้ด+เทสต์เสร็จ (232 PASS / 0 FAIL) · rev4 · พร้อมรันขั้น 1–10 · ยังไม่ commit · ยังไม่ build Release · ยังไม่ copy DLL · ยังไม่แตะเซฟ · ยังไม่เปิดเกม**
rev1 → rev2: เพิ่มขั้น 0 (เขียนโค้ด+เทสต์ in-proc ก่อน), สลับลำดับเป็น split → copy DLL → sampler → preflight → ผู้ใช้เปิดเกม, เพิ่มการตรวจความพอของเครื่องมือวัดก่อนเปิดเกม, commit source ก่อน build, นิยามใหม่ exit 3/5/6/2/8, เพิ่มการตั้ง/คืน pref + sha256 และไฟล์ `s2-snapshots.ndjson`, เพิ่มคำเตือนตอนเล่น

rev2 → rev3 (มติผู้ใช้ 2026-10-21 หลังตรวจ diff step 0): **ยกเลิก D1** — `TurnGate.cs` กลับเป็น `57e68dc` ทุกไบต์ (guard เทสต์ = byte-exact กับ fixture) · replica ใช้ `IsAlive` ตัวเดียวแล้วเทียบกับ `unitAlive` ของ `spike status` (ค่าจาก `BattleUnit.IsAlive` ตัวเดียวกัน) · ป้ายเฟส: **A = `coop.net.enabled=false` (baseline, S2 idle) · B = `true` (S2 ทำงาน)** · exit 3 = **`(B − A) ≤ 1 ms` โดยดูเครื่องหมาย** + band ±0.5 ms = INCONCLUSIVE · exit 6 = **`forbiddenTypeDrops == 3` พอดี + `commandsAccepted == 0` + audit tuple ก่อน/หลังเท่ากัน**
rev3 → rev4 (มติผู้ใช้ 2026-10-21 ข้อ 1–4): **`DevCommands` = (ก)** — ยอมให้ `DevCommands=true` ตอนวัด แล้ว **ย้ายตรรกะ guard เป็นฟังก์ชัน pure** `S2WriteGuard.WritePathsClosed(allowRemoteInput, jitProbe, jitProbeW2, jitProbeW4, jitProbeW6, jitProbeW7)` ที่ **`DevCommands` ไม่มีผลกับผลลัพธ์** (เทสต์ครบ 64 ชุดค่าผสม + reflection + source check) · **ตาราง audit คำสั่ง IPC** ทุกคำสั่งที่เปิดได้ตอน `DevCommands=true` (§8.2) · ขั้น 7 ตั้ง `DevCommands=true` **อย่างชัดเจน** + บันทึกใน evidence · `ReplicaHash.UnitAlive` = **(ii)** คงฟิลด์ **ห้ามแก้ hash spec** และ `S2Sampler` ใส่ค่า `= u.IsAlive` (สำเนา, ล้าหลัง `hp` ≤1 เฟรม; พิจารณาตัดตอนเปลี่ยนเวอร์ชันโปรโตคอล) · recorder ขั้น 9 = `tools\s15d_sample.py` (เหตุผลในตารางขั้น 9) · `AGENTS.md` โฟลเดอร์เกม = commit แยก 1 commit (`git add AGENTS.md` เท่านั้น) · **ไม่แตะ `IpcServer.cs` / `tools/` ในรอบนี้**
มติ ①–④ (2026-10-21): **ใช่ทั้งหมด** · `.dll.off` — **ตรวจแล้วไม่มีไฟล์เดิมใน `Mods\`** (ไม่มี `*.off` เลย) ใช้ชื่อนี้ได้
อ้างอิง: `S2-plan.md §13.3/§13.4/§13.6`, `STAGEPLAN.md §1.1/§1.4/§1.6`
DLL เดิม: `517758147f59170e584075a4b42f61e00d293803febe36f8a040bcdad4592612` (= `src\LongYinCoop\bin\Release\LongYinCoop.dll` byte เดียวกัน, ยืนยัน sha256 แล้ว)

## 0. กติกาที่บังคับในขั้น 0 (เขียนโค้ด)

- อ่าน IL2CPP **บน main thread เท่านั้น** ผ่าน `MainThreadDispatcher` (budget 4 ms/เฟรม ตาม §1.4)
- thread อื่นแตะได้เฉพาะ **POCO/serialized byte buffer** — ห้ามถือ `Il2CppObjectBase` ข้ามเฟรม
- interop **แบบมีชนิด** (`u.IsAlive`, `hero.hp`, `grid.row`, `team.battleUnits`) — ห้าม raw offset/pointer
- S2 ไม่แตะ `GameDataController.Save/Load`, ไม่มี type `pause/move/attack/skill/item` ในช่อง guest→host, ไม่แตะ Steam/`APIDispatcher`/`DnsChecker`/`CheatCheck`/`cheat*`

## 1. ขอบเขต

**ทำ**

| ไฟล์ | เนื้อหา |
|---|---|
| `src/LongYinCoop/S2/Transport/ITransport.cs` | interface + `TransportStats` + `TransportKind` |
| `src/LongYinCoop/S2/Transport/LoopbackTransport.cs` | in-proc pair (send → sink queue), ไม่มี socket, อ่าน byte buffer เท่านั้น |
| `src/LongYinCoop/S2/Host/S2Snapshot.cs` | POCO ต่อยูนิต + header (ไม่มี Il2Cpp type) |
| `src/LongYinCoop/S2/Host/S2Sampler.cs` | อ่าน game state บน main thread → POCO + audit tuple |
| `src/LongYinCoop/S2/Host/S2Sender.cs` | serialize (Protocol codec) + ส่งผ่าน `ITransport` + metrics |
| `src/LongYinCoop/S2/Host/S2Counters.cs` | `forbiddenTypeDrops`, `commandsAccepted`, `framesSent`, `bytesSent`, allowlist enforcement |
| `src/LongYinCoop/S2/S2NetConfig.cs` | อ่าน `coop.net.*` จาก `MelonPreferences.cfg` (poll ≤1 Hz, ไม่ใช้ raw offset) |
| `tests/S2Protocol.Tests/Program.cs` | เพิ่มชุดเทสต์ in-proc (loopback, ยัดเฟรมต้องห้าม, audit) |
| `src/LongYinCoop/CoopMod.cs` | hook เปิด/ปิด S2 (แก้เล็กสุด, เฉพาะ init/poll) |

**ไม่ทำ:** `TcpTransport`, `viewer-client.py`, resync ผ่านเน็ต (→ S2a-3) · overlay + `WriteGuard` hook (→ S2a-4) · VPN (→ S2b) · แก้ `tools/` · `TurnGate.cs` (**ไม่แตะเลย** — D1 ยกเลิกแล้ว, guard = byte-exact กับ `57e68dc`) · **`IpcServer.cs` (rev4: ไม่แตะเลย — ตาราง audit §8.2 เป็นเอกสารเท่านั้น; ถ้าต้องกัน `set_pref`/`disarm` เพิ่ม จะทำเป็น stage ถัดไปหลังคุณอนุมัติ)**

## 2. ลำดับ (ห้ามสลับ)

| # | ใคร | ขั้น | ผ่านเมื่อ |
|---|---|---|---|
| **0** | ผม | เขียนโค้ดตาม §1 + เทสต์ in-proc → `dotnet run --project tests\S2Protocol.Tests` → **ส่งสรุป diff ให้คุณตรวจ แล้วหยุดรอ** | in-proc PASS ทุกเคส รวม "ยัดเฟรมต้องห้าม" |
| 1 | ผม | **commit source S2a-2** (ยังไม่ build) → จด commit hash ลง evidence | tree สะอาด |
| 2 | ผม | `dotnet build src\LongYinCoop\LongYinCoop.csproj -c Release` → จด sha256 DLL ใหม่ + build log (**ยังไม่ copy**) | build 0 error/warning ใหม่ |
| 3 | ผม | สำรอง DLL เดิม: `Mods\LongYinCoop.dll` → `Mods\LongYinCoop.dll.off` (+ `.pdb.off`) → จด sha256/ขนาดของ `.off` | ยืนยันแล้วว่า `.off` ยังไม่มีไฟล์เดิม |
| 4 | ผม | ตรวจก่อน split: เกมไม่รัน · `save-split status` = 57 ไฟล์ `6dd645d9ef17` · `leftovers: none` · sha256 เกม 2 ไฟล์ตรง `VERSION.md` · **backup `MelonPreferences.cfg` → `prefs.before.cfg` + sha256** | ทุกข้อผ่าน (ไม่ผ่าน = หยุด) |
| 5 | ผม | `save-split split --label S2a2-<ts> --expected-baseline coop-test\baselines\20261021-200450\savehash.real.before.txt` | `Save.real-S2a2-<ts>` อยู่, `Save` = สำเนา |
| 6 | ผม | copy DLL+PDB ใหม่เข้า `Mods\` → sha256 ใน `Mods\` ต้องตรงข้อ 2 | ตรง |
| 7 | ผม | ตั้ง pref: `coop.net.enabled=false` (เฟส A = baseline ปิด), `coop.role=host`, `coop.secret=<hex 32B>`, **`coop.DevCommands=true` (มติ rev4 ข้อ 1 = (ก) — จำเป็นให้ `spike status` ทำงาน; ต้องจดลง evidence)**, `coop.AllowRemoteInput=false`; บันทึก sha256 ของ cfg หลังตั้ง + ค่าทุกคีย์ใน section `[LongYinCoop]` | `AllowRemoteInput=false` · `JitProbe/JitProbeW2/W4/W6/W7=false` · `DevCommands=true` บันทึกแล้ว |
| 8 | ผม | ตรวจความพอของเครื่องมือวัด (**ผลตรวจแล้ว 2026-10-21**): `spike status` ต่อยูนิตมี `hp,row,col,unitAlive,playerControl,originHp,alive` — `unitAlive` = `BattleUnit.IsAlive` ⇒ **พอสำหรับ exit 4** (เทียบ `isAlive` ของ replica ซึ่งมี `unitAlive` บน wire/ndjson ด้วย) · ต้อง `Enabled+Ipc+DevCommands` ทั้งสาม (IpcServer.cs:333-335) ⇒ rev4 ตั้ง `DevCommands=true` แล้ว ⇒ **ข้อนี้ผ่าน** | ผ่าน (ไม่ต้องรอคำตัดสินแล้ว) |
| 9 | ผม | เปิด recorder: **`tools\s15d_sample.py --out coop-test\S2a2-<ts>\sampler.ndjson --seconds 7200 --interval 0.5 --append --wait 3600`** (background) + จด PID · สร้างไฟล์ `sampler.ndjson` เปล่าไว้ **ก่อน** เปิด process (เพื่อให้ preflight เห็นไฟล์; `--append` ไม่ทำลาย) · **หมายเหตุ:** แผนเดิมใช้ `spike_watch.py` แต่สคริปต์นั้น **exit 0 ทันทีที่ `battle_ready`** ⇒ ให้ชุดข้อมูล A/B 120 s ไม่ได้; `s15d_sample.py` อยู่ใน `SAMPLER_SCRIPTS` allowlist ของ `save-split.py` เหมือนกัน, read-only (`spike status` เท่านั้น), บันทึก **full payload ต่อยูนิต** (ใช้ได้ทั้ง exit 3 และ exit 4 จาก series เดียว) — ไม่แก้ `tools/` | ไฟล์ถูกสร้าง + process ยังรัน |
| 10 | ผม | `save-split preflight --label S2a2-<ts> --sampler-pid <pid> --sampler-out …\sampler.ndjson` → **ต้อง READY** | READY (REFUSED = หยุด ไม่เปิดเกม) |
| 11 | **คุณ** | **เปิดเกม** → เข้า 1 ศึก (คาด 11v7) → เล่น 1 เทิร์น · **ระหว่างเล่นดู §6** → ปิดเกม แล้วบอกผม | — |
| 12 | ผม | เก็บผล: `s2-snapshots.ndjson`, metrics, เทียบ per-unit `isAlive` ↔ `unitAlive` (§5 exit 4), audit tuple, unit count, A/B frame time (§5 exit 3) | — |
| 13 | ผม | ปิดเกมแล้ว: `verify` → `restore` → **คืน pref** (copy `prefs.before.cfg` กลับ + sha256 ต้องตรงก่อนตั้ง) → `rebaseline` ถ้า pref drift | เซฟ 57 ไฟล์ `6dd645d9ef17`, `leftovers: none` |
| 14 | ผม | รายงานเทียบ exit criteria ทีละข้อ + rollback test → **รออนุมัติ** ก่อน tag `checkpoint-S2a` | — |

## 3. คำเตือนผู้ใช้ตอนเล่น (ข้อ 11)

1. **ห้าม Save** เกม (F5/เมนู save) ตลอดช่วงวัด — ใช้สำเนาเท่านั้น
2. **ห้าม pause ตอน `WaitPlayerInput`** (มติ S1.5: เกมไม่รับ) — ถ้าจะ pause ให้ทำตอน state อื่น
3. **ถ้ากด Esc** (เมนู Esc หยุด sim แต่ `battlePaused` ยัง false, `timeScale` ยัง 1) → **แจ้งผม** เพื่อ exclude หน้าต่างนั้นจากการวัด
4. อย่าออกจากศึกกลางคันถ้าเลี่ยงได้ (เกมไม่มี autosave ตอนออก)
5. ถ้าเกมค้าง/เด้ง ให้แจ้งทันที ห้ามกดอะไรซ้ำ

## 4. Pref: ตั้ง/คืน + sha256

| จุด | สิ่งที่ทำ | หลักฐาน |
|---|---|---|
| ก่อนตั้ง (ข้อ 4) | copy `UserData\MelonPreferences.cfg` → `coop-test\S2a2-<ts>\prefs.before.cfg` + sha256 | `prefs-sha-before.txt` |
| ระหว่าง (ข้อ 7) | แก้เฉพาะ section `[LongYinCoop]`: `coop.net.enabled=false` (เริ่มที่ปิด — เฟส A) · `coop.role=host` · `coop.secret=<hex 32B>` · **`coop.DevCommands=true` (rev4 ข้อ 1 = (ก))** · คง `coop.AllowRemoteInput=false` + `coop.JitProbe/W2/W4/W6/W7=false` · `coop.Enabled=true`, `coop.Ipc=true` (โมดต้องรัน/IPC ต้องมี) | sha256 หลังตั้ง + ค่าทุกคีย์ (จด `DevCommands=true` ชัด ๆ) |
| ระหว่างวัด (ข้อ 11–12) | สลับ `coop.net.enabled` ผ่านไฟล์ cfg (S2 อ่านทุก ≤1 Hz): **A = `false` (baseline, S2 idle) → B = `true` (S2 ทำงาน)** เป็นบล็อก **A-B-A-B บล็อกละ 30 s** แล้วจบที่ A | บันทึกเวลา + sha256 ทุกครั้งที่สลับ |
| หลังจบ (ข้อ 13) | copy `prefs.before.cfg` กลับทับ + sha256 **ต้องตรงก่อนตั้งเป๊ะ** | `prefs-sha-after.txt` + diff |

## 5. Exit criteria (นิยาม rev2)

| # | เกณฑ์ | วิธีวัด/ผ่าน |
|---|---|---|
| 1 | โหลดได้/ไม่ crash | log mod ปรากฏ, 1 เทิร์นจบ, exception = 0 |
| 2 | ต้นทุนต่อเฟรม | `sampleMs+serializeMs` p99 ≤ **4 ms** (§13.2 ข้อ 4) บันทึก mean/p99/max · **ถ้ารอบนี้ 18 ยูนิต → verdict = PASS-with-limitation (ห้ามขยายผลถึง 60)** |
| 3 | **ตัววัด frame time + baseline (นิยาม rev3)** | ตัววัด: `deltaTime`/`frameCount` delta จาก recorder ตัวเดียวกัน (**rev4: `tools\s15d_sample.py`** — read-only, เก็บ `frame` ต่อตัวอย่าง; ไม่ผูกกับ S2) · **A = `coop.net.enabled=false` (baseline, S2 idle) · B = `true` (S2 ทำงาน)** สลับเป็นบล็อก **A-B-A-B บล็อกละ 30 s (รวม 120 s) ในศึกเดียว** · **เกณฑ์: `mean(B) − mean(A) ≤ 1 ms` โดยดูเครื่องหมาย** (ติดลบ = S2 ถูกกว่า = ผ่าน) · `|delta − 1 ms| ≤ 0.5 ms` = **INCONCLUSIVE — ห้ามฟันธงผ่านจากรอบเดียว** (โค้ด: `S2Exit3Rule`) · เวลาที่สลับบันทึกด้วยมือ (`frame-time-ab.txt`) เพราะ `s2-snapshots.ndjson` ไม่มี wall timestamp ของเฟส · ถ้าสลับ pref กลางรันไม่ได้ →ใช้ **Option D2-ii** (2 รัน: `.dll.off` vs DLL ใหม่) แล้วระบุชัดในรายงาน |
| 4 | snapshot ตรงกับเกม | per-unit เทียบ `spike status` (จาก `sampler.ndjson`) vs `s2-snapshots.ndjson`: `unitCount, id, row, col, hp(3), isAlive` mismatch 0 · **`isAlive` ของ replica (`S2Unit.IsAlive`) เทียบกับ `unitAlive` ของ `spike status`** — ทั้งสองคือ `BattleUnit.IsAlive` ตัวเดียวกัน (TurnGate `SafeUnitAlive(u) → u.IsAlive`), ไม่มี D1, ไม่มี raw read ⇒ **เทียบได้เต็ม ไม่ใช่ NOT-RUN** · **rev4: replica ยังมีฟิลด์ `UnitAlive` (สำเนา `IsAlive`) บน wire + ndjson ⇒ เทียบ `unitAlive` สองฝั่งได้ตรง ๆ** · `moved/attacked` ต่อยูนิต: S1.5 วัดแล้วว่า `BattleUnit.moved/attacked` ไม่พลิกจริง ⇒ ใช้ `isPlayerTurn/turnMoved/turnAttacked(turnFlagsAvailable)` ระดับ controller แทน และรายงาน per-unit flag เป็นข้อมูลประกอบเท่านั้น · **ข้อนี้พึ่ง `spike status` ⇒ ต้อง `DevCommands=true` (rev4 ตั้งแล้ว)** |
| 5 | ความตายใน replica | **ไม่มีศพในรอบนี้ → NOT-RUN** (ไม่ใช่ FAIL) บันทึกไว้ทำต่อ · ถ้ามีศพ: `hp=0 && isAlive=false` และต้องยืนยันซ้ำด้วย `unitAlive=false` ของ `spike status` (= ค่าเดียวกัน) |
| 6 | **ยัดเฟรมต้องห้าม (นิยาม rev3)** | **in-proc:** ยัดชุดเดียวกับที่รันจริง `S2SelfTest.BuildFrames()` (Hello + Snapshot + BattleEvent ที่ถูก chunk) → **`forbiddenTypeDrops == 3` พอดี**, **`commandsAccepted == 0`**, **audit tuple ก่อน/หลังเท่ากัน** · **ในเกม:** self-test รันเองตอน activate เขียน marker `selftest` ลง `s2-snapshots.ndjson` ต้องได้ `gained=3 · commandsAccepted=0 · auditEqual=true · exit6=PASS` (ไม่ผ่าน → marker `selftest.fail` + log WARN) |
| 7 | ไม่แตะเซฟ | `SaveSlot*` เปลี่ยน 0 ไฟล์ · หลัง restore digest = `6dd645d9ef17` (ยกเว้น volatile) · `leftovers: none` |
| 8 | **จำนวนยูนิตจริง** | บันทึก unit count จริงทุกรอบ · **18 ยูนิต (11v7) = PASS-with-limitation; ห้ามลดเป้า 60 และห้ามสรุปว่าผ่านที่ 60** |
| 9 | rollback ใช้ได้จริง | คืน `.dll.off` แล้วเกมรันได้ (log mod เดิม) + `restore` คืนเซฟได้ + pref คืน hash ตรง |

FAIL ข้อใด = รายงานตรง ๆ คงสถานะ rollback ไม่เลื่อนเกณฑ์

## 6. Rollback

1. **DLL**: `Copy-Item Mods\LongYinCoop.dll.off Mods\LongYinCoop.dll -Force` (+ `.pdb.off`)
2. **pref**: `Copy-Item prefs.before.cfg → UserData\MelonPreferences.cfg` + ตรวจ sha256
3. **เซฟ**: `save-split.py restore --label S2a2-<ts>` + `verify` ⇒ 57 ไฟล์ `6dd645d9ef17`
4. **git**: `git checkout stage-S1.5g` (`e3e798a`) หรือ revert commit ของข้อ 1
5. **tag**: `checkpoint-S2a` เฉพาะหลังอนุมัติรายงาน

## 7. หลักฐาน (`evidence/S2a-2-<ts>/`)

`code-diff-summary.md` · `commit.txt` (hash ข้อ 1) · `build.txt` (log + sha256 DLL ใหม่) · `dll-before-after.txt` (`.off` + ใหม่) · `prefs-sha-before.txt` · `prefs-sha-after.txt` · **`pref-values.txt` (ค่าทุกคีย์ใน `[LongYinCoop]` รวม `DevCommands=true`)** · **`recorder.txt` (เหตุผลที่ใช้ `s15d_sample.py`)** · `preflight.txt` · `split.json` · `sampler.ndjson` (recorder กลาง) · **`s2-snapshots.ndjson` (S2 ส่งออก)** · `metrics-summary.txt` · `frame-time-ab.txt` (A/B + เวลาที่สลับ pref) · `snapshot-compare.txt` · `audit-tuple.txt` · `unit-count.txt` · `verify-restore.txt` · `notes.md`

## 8. สถานะมติ + ข้อที่ต้องได้คำตอบก่อนเปิดเกม (rev4)

### 8.1 D1 — per-unit `isAlive` สำหรับ exit 4: **ยกเลิก (มติผู้ใช้ 2026-10-21)**
- `TurnGate.cs` กลับเป็น `57e68dc` **ทุกไบต์** (guard เทสต์ = byte-exact กับ fixture, sha256 `737d48941c3707cef4aea1ef4ccae26163f5fe05f28cd065ae246b84854523ca`)
- `unitAlive` ที่ `spike status` รายงาน = `SafeUnitAlive(u) → u.IsAlive` = `BattleUnit.IsAlive` ตัวเดียวกันกับที่ replica เก็บ ⇒ **ไม่ต้องอ่านค่าที่สองจากเกม, ไม่มี raw read** (กติกา 8)
- exit 4 จึงเทียบ `S2Unit.IsAlive` ↔ `unitAlive` ของ `spike status` แทน

### 8.2 `DevCommands` = **(ก) อนุมัติแล้ว (มติ 2026-10-21 ข้อ 1)** + ตาราง audit คำสั่ง IPC
- rev3 เจอข้อขัดแย้ง: `spike status` บังคับ `Enabled+Ipc+DevCommands` (IpcServer.cs:333-335) แต่ guard เดิมบังคับ `DevCommands=false`
- **มติ (ก):** อนุญาต `DevCommands=true` ตอนวัด แล้ว **ผ่อน guard** — ย้ายตรรกะเป็นฟังก์ชัน pure `S2WriteGuard.WritePathsClosed(6 pref)`
  โดย **`DevCommands` ไม่ถูกตรวจ** และเพิ่ม `JitProbe/JitProbeW2/W4/W6/W7` เป็นเงื่อนไขปฏิเสธ (เดิมตรวจแค่ `AllowRemoteInput`+`DevCommands`)
  เทสต์: truth table ครบ **64 ชุด** + `Describe` + reflection (ไม่มีพารามิเตอร์ `dev*`) + source check (`S2HostService` ไม่มี `DevCommands` เลย)
- ขั้น 7 ต้องตั้ง `coop.DevCommands=true` **ชัดเจน** และจดใน evidence (`pref-values.txt`)

**ตาราง audit คำสั่ง IPC ที่เปิดได้ตอน `DevCommands=true`** (ตารางเต็ม + เลขบรรทัด: `evidence/S2a2-20261021-2213-step0/ipc-audit-20261021.md`)

| คำสั่ง | gate | ประเภท | ผ่าน `AllowRemoteInput`? |
|---|---|---|---|
| `spike status` | `Enabled+Ipc+DevCommands` | อ่านอย่างเดียว | — (ไม่ต้อง) |
| `spike arm` | +`AllowRemoteInput` (:344) | เปลี่ยน mod-state (เปิดทาง move) | ✓ |
| `spike move` | +`AllowRemoteInput` (:355) | **เขียน state เกม** | ✓ |
| `spike attack` | +`AllowRemoteInput` (:381) | **เขียน state เกม** | ✓ |
| `spike disarm` | `Enabled+Ipc+DevCommands` เท่านั้น (:349-352) | เปลี่ยน mod-state (ล้าง arm flag) — **เขียนเกมไม่ได้** | ✗ (เจตนา = เบรกฉุกเฉิน) |
| `bench_dispatch` | `DevCommands` (:558) | เปลี่ยน mod-state (enqueue no-op) | ✗ |
| `set_pref` | **ไม่มี gate** (:405-553) | เปลี่ยน pref/mod-state + ติด-ถอด Harmony patch (**เปิด `allowRemoteInput` ได้เอง**) | ✗ |
| อื่น ๆ (`ping/get_version/get_scene/get_player_pos/stats/snapshot*/capture/compat_dump/probe_types/patch_report/probe_units/s1_report`) | ไม่มี gate | อ่านอย่างเดียว หรือเขียนเฉพาะ **ไฟล์หลักฐาน** | — (ไม่มี game write) |

**สรุป:** ไม่มีคำสั่งที่ **เขียน state เกม** ได้โดยไม่ผ่าน `AllowRemoteInput` · แต่มี 3 รายการที่เขียน **mod-state** ได้โดยไม่ผ่าน
(`spike disarm`, `bench_dispatch`, `set_pref`) ⇒ **รายงานให้ผู้ใช้ตัดสิน (ยังไม่เลือกเอง, ไม่แก้ `IpcServer.cs`)** ·
ข้อเสนอ (stage ถัดไป, รออนุมัติ): กัน `set_pref` ไม่ให้เปิด write path ขณะ S2 active (หรือรวมใน `WriteGuard` hook ของ S2a-4)

### 8.3 `ReplicaHash.UnitAlive` = **(ii) อนุมัติแล้ว (มติ 2026-10-21 ข้อ 2)**
- **คงฟิลด์ไว้ ห้ามแก้ hash spec** — `ReplicaHash.Compute` ไม่ถูกแตะ (เทสต์ T0 ผ่านเท่าเดิม)
- `S2Sampler` ใส่ค่า `UnitAlive = S2UnitRule.MirrorUnitAlive(u.IsAlive)` = **สำเนาของ `IsAlive`**
  (ไม่มีแหล่งค่าที่สอง: interop ไม่มี `BattleUnit.unitAlive`; `unitAlive` ของ `spike status` = `IsAlive`)
- คอมเมนต์ใน `ReplicaHash.cs` แก้ให้ตรงความจริงแล้ว (สำเนา, ล้าหลัง `hp` ≤1 เฟรม) + จดใน `S2-plan.md §13.2` ว่าจะพิจารณาตัดตอนเปลี่ยนเวอร์ชันโปรโตคอล
- ฟิลด์เดินทางบน wire (`S2FrameCodec`) + อยู่ใน ndjson (`"unitAlive"`) — audit tuple ไม่ใส่ (ซ้ำกับ `isAlive`)
- **ProtoVer ยัง = 1** เพราะยังไม่มี peer จริง (S2b) — การเพิ่ม 1 ไบต์/ยูนิตบันทึกไว้ที่นี่และใน `S2-plan.md`

### 8.4 D2 — วิธี baseline ของ exit 3: **ใช้ D2-i** (A/B ในศึกเดียว สลับ `coop.net.enabled` ผ่าน cfg ≤1 Hz) เป็นบล็อก A-B-A-B 30 s · D2-ii เป็น fallback
- ป้ายเฟส: **A = `false` (baseline, S2 idle) · B = `true` (S2 ทำงาน)** · เกณฑ์ `mean(B) − mean(A) ≤ 1 ms` แบบดูเครื่องหมาย
- **recorder (ขั้น 9) = `tools\s15d_sample.py`** แทน `spike_watch.py` (ตัวหลัง `exit 0` ทันทีที่ `battle_ready` ⇒ ให้ series 120 s ไม่ได้)
  ทั้งคู่อยู่ใน allowlist `SAMPLER_SCRIPTS` ของ `save-split.py`, อ่านอย่างเดียว, ไม่แก้ `tools/` · ไฟล์ผล `sampler.ndjson` ถูกสร้างก่อนเปิด process (preflight เห็นไฟล์)

**preflight:** READY ได้ต่อเมื่อมี sampler PID + ไฟล์ผล (ข้อ 9–10) + เกมปิด + `Save` เป็นสำเนา (ไม่ตรวจ `DevCommands`)

ขั้น 0 + fix round 2 เสร็จแล้ว (**232 checks PASS / 0 FAIL** · `evidence/S2a2-20261021-2213-step0/`) · **rev4 นี้รันขั้น 1–10 ได้ทันที**

### 8.5 rev5 — มติผู้ใช้ 2026-10-21 (หลังคำถามก่อนเปิดเกม) **แทนที่วิธีทำ exit 3 และ exit 4 ข้างบน**
เอกสารบังคับ: `evidence/S2a2-20261021-2254/ab-protocol-v2.md` (สรุปใน `AGENTS.md` มติ rev5)
- **exit 3 = วัด mean อย่างเดียว** ไม่เพิ่ม p99 ไม่ redeploy · ผ่านเกณฑ์ mean = **PASS-with-limitation** (p99 = **NOT-RUN**) · `delta` ใน ±0.5 ms รอบ margin 1.0 ms = **INCONCLUSIVE** · เกณฑ์ margin 1.0 / band 0.5 ไม่ขยับ
- **A-B-A-B 120 s:** ศึกต้องนิ่งที่ `nowActiveState = WaitPlayerInput` ตลอด และห้ามสั่ง action ใด ๆ · ก่อนทุกบล็อกตรวจ `sampler.ndjson` ยังโต (ตายกลางบล็อก = ทิ้งบล็อกนั้น) · จด wall-clock ทุกการสลับ pref + sha256 cfg ก่อน/หลัง ลง `frame-time-ab.txt` · `phase.start/phase.idle` ไม่โผล่ใน ~3 s = หยุดและรายงาน
- **exit 4 ย้ายไปหลังจบ A-B-A-B** โดยเปิดเฟส B ค้างไว้แล้วเล่น 1 เทิร์น (ไม่สลับ pref ระหว่างเล่น)
- **exit 6:** self-test ต้องเกิดในศึกจริง และ marker ต้องมี `haveBefore=true` **และ** `haveAfter=true`; ถ้า false = โมฆะ/NOT-RUN
- ลำดับบล็อกคงเดิม: A1(false 30s) → B1(true 30s, self-test รันในศึก) → A2(false 30s) → B2(true 30s) → exit 4
