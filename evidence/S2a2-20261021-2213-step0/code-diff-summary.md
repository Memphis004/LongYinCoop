# S2a-2 (step 0) — code diff summary
label: `S2a2-20261021-2213-step0`
date: 2026-10-21 22:13 (local)
repo: `C:\Users\memph\LongYinCoop` · mod HEAD `57e68dc` · tree **dirty เฉพาะงาน step 0** (ยังไม่ commit)

## 0) ขอบเขตที่ทำ (step 0 เท่านั้น)

เขียนโค้ด + เทสต์ in-proc ของ S2a-2 และแก้ D1 ตามที่อนุมัติ **ยังไม่ commit / ไม่ build Release / ไม่ copy DLL /
ไม่เปิดเกม / ไม่แตะเซฟหรือ pref**

คำสั่งที่รันในขั้นนี้:

| คำสั่ง | ผล |
| --- | --- |
| `dotnet run --project tests\S2Protocol.Tests` | **RESULT PASS** · 175/175 checks (T0 66 + S2a2 109) · 0 FAIL · 0 NOT-RUN |
| `dotnet build src\LongYinCoop\LongYinCoop.csproj -c Debug` | **Build succeeded · 0 Error** (ใช้ตรวจชื่อ typed interop เท่านั้น; output ลง `bin\Debug` ที่ถูก gitignore; **ไม่ใช่** Release และไม่มีการ copy ไป `Mods\`) |

หลักฐานดิบในโฟลเดอร์นี้: `test-run.txt` (ผลเทสต์เต็ม), `git-diff-tracked.txt`, `git-status.txt`,
**`TurnGate.diff.txt` (diff ของ D1 แยกไฟล์ตามที่สั่ง)**

## 1) ไฟล์ที่เพิ่ม/แก้

### 1.1 ไฟล์ใหม่ — pure (ไม่พึ่ง IL2CPP, ถูก link เข้าเทสต์)

| ไฟล์ | บรรทัด | หน้าที่ |
| --- | --- | --- |
| `src/LongYinCoop/S2/Transport/ITransport.cs` | 56 | สัญญา transport (`Send`/`TryReceive`/`Stats`) — byte buffer ล้วน ไม่ถือ IL2CPP object; `Send` ห้ามบล็อก |
| `src/LongYinCoop/S2/Transport/LoopbackTransport.cs` | 117 | คู่ loopback in-proc มีขอบเขตคิว; ปิด/คิวเต็ม = คืน false + นับ (`SendDrops`, `FullDrops`) ไม่โยน exception บน main thread |
| `src/LongYinCoop/S2/Host/S2Snapshot.cs` | 193 | POCO ของ replica (`S2Unit`/`S2Header`/`S2Frame`), **audit tuple** ตาม §13.2 ข้อ 3 + `S2Ndjson.ToLine` |
| `src/LongYinCoop/S2/Host/S2Counters.cs` | 162 | ตัวนับ + `S2GuestGate` (allowlist guest→host, chunk reassembly) — `CommandsAccepted` เพิ่มได้ทางเดียวคือมีคนเพิ่ม write path (ซึ่ง S2 ไม่มี) |
| `src/LongYinCoop/S2/Host/S2Sender.cs` | 388 | `S2FrameCodec` (wire encode/decode), `S2Metrics` (p50/p99 ring buffer), `S2SerializingSender` (**serialize บน thread อื่น**, คิว 2, เต็ม = ทิ้ง+นับ), `S2NdjsonWriter` (append ได้, `WriteRawLine` สำหรับ marker) |
| `src/LongYinCoop/S2/Host/S2PhaseStats.cs` | 85 | ตัวสะสมต่อเฟส A/B ของ exit 3 (mean/min/max) |
| `src/LongYinCoop/S2/S2NetConfig.cs` | 227 | อ่าน `coop.net.*` จาก `UserData\MelonPreferences.cfg` **ตรง ๆ** + poll ≤1 Hz (`Poll`/`ForceRead`) → กลไก D2-i |

### 1.2 ไฟล์ใหม่ — พึ่ง IL2CPP (ไม่ถูก link เข้าเทสต์, ตรวจด้วย Debug compile เท่านั้น)

| ไฟล์ | บรรทัด | หน้าที่ |
| --- | --- | --- |
| `src/LongYinCoop/S2/Host/S2Sampler.cs` | 184 | อ่าน state ศึกบน main thread → POCO; typed interop ทั้งหมด; ไม่เรียก method ที่เปลี่ยน state |
| `src/LongYinCoop/S2/S2HostService.cs` | 353 | poll config → sample → post ให้ sender thread → drain ช่อง guest→host ผ่าน gate; เขียน marker เฟส/audit ลง `s2-snapshots.ndjson`; self-test ยิง frame ต้องห้ามในรัน |

### 1.3 ไฟล์เดิมที่แก้ (tracked)

| ไฟล์ | diff | เหตุผล |
| --- | --- | --- |
| `src/LongYinCoop/S1.5/TurnGate.cs` | **+3 บรรทัด** | D1 (ดูข้อ 2) |
| `src/LongYinCoop/CoopMod.cs` | +10/−1 | สร้าง `S2HostService` หลัง `_ipc.Start()`, เรียก `Tick()` หลัง `AttackCommand.Tick()`, `Stop()` ใน `OnDeinitializeMelon` (+`using LongYinCoop.S2;`) |
| `src/LongYinCoop/CoopPaths.cs` | +11 | เพิ่ม `MelonPreferencesPath` และ `S2EvidenceDir` |
| `tests/S2Protocol.Tests/S2Protocol.Tests.csproj` | +16/−3 | link ไฟล์ pure ของ S2a-2 (ไม่ link `S2Sampler.cs`/`S2HostService.cs`/`CoopMod.cs`) + `fixtures/**` เป็น `None` (ไม่ให้ glob compile) |
| `tests/S2Protocol.Tests/Program.cs` | +3/−1 | เรียก `S2a2Tests.Run()` |

### 1.4 เทสต์/หลักฐานที่เพิ่ม

- `tests/S2Protocol.Tests/S2a2Tests.cs` (599 บรรทัด, **109 checks**)
- `tests/S2Protocol.Tests/fixtures/TurnGate.before.cs.txt` = `git show 57e68dc:src/LongYinCoop/S1.5/TurnGate.cs` (byte-exact, ใช้เป็น fixture กัน D1 guard)

## 2) D1 — diff ของ `TurnGate.cs` (แยกให้ดูตามที่สั่ง)

ไฟล์ diff: `TurnGate.diff.txt` · **+3 บรรทัด, ลบ 0**

```diff
@@ -298,6 +298,9 @@ namespace LongYinCoop
                         playerControl = u.playerControl,
                         // S1.5-c ตัดสินใจ: ตัววัดดาเมจที่เป็นไปได้ (อ่านอย่างเดียว)
                         originHp = SafeOriginHp(u),
+                        // S2a-2 D1: unit death flag (BattleUnit.IsAlive) - added for S2 only,
+                        // NOT part of HashText/CanonicalUnit/stateHash
+                        isAlive = SafeUnitAlive(u),
                         unitAlive = SafeUnitAlive(u)
                     });
```

- เพิ่มเฉพาะ `isAlive` ในออบเจ็กต์ต่อยูนิตของ `Build()` — **ไม่แตะ** `HashText`, `CanonicalUnit`, `stateHash`, และไม่เพิ่ม `moved`/`attacked` ต่อยูนิต
- ใช้ `SafeUnitAlive(u)` ตัวช่วยเดิม (คืน `u.IsAlive` แบบมี try/catch) ไม่เพิ่ม exception surface ใหม่
- `spike status` ตอนนี้มีทั้ง `unitAlive` (ของเดิม, ค่าจาก `u.IsAlive`) และ `isAlive` (ของใหม่, ค่าเดียวกัน) — เอกสาร AGENTS อธิบาย `unitAlive` ว่าเป็นฟิลด์เกม ซึ่ง**ไม่ตรง**กับโค้ด (ดูข้อ 5.1)

**เทสต์พิสูจน์ `stateHash` ของ state เดิมไม่เปลี่ยน (mechanical guard, ใน `S2a2Tests.TurnGateGuardTests`)**

1. `D1 diff removes nothing` — multiset ของบรรทัดทั้งไฟล์: ฝั่งที่หายไป = 0 บรรทัด
2. `D1 diff adds exactly 3 lines` + `every added line is exactly the approved D1 block` — เพิ่มตรง 3 บรรทัดตามที่อนุมัติ
3. `hash preimage lines exist and are identical` + `hash preimage SHA-256 unchanged` — ทุกบรรทัดที่ต่อ `HashText.Append` เทียบกับ fixture ก่อนแก้ **เรียงต่อกันเหมือนเดิม และ sha256 ของข้อความ preimage เท่ากัน**
4. `no hash preimage line gained an isAlive field`, `CanonicalUnit signature and call site unchanged`,
   `stateHash input lines unchanged`, `isAlive appears exactly once outside the hash path`
5. `unitRows object still exposes both unitAlive and isAlive` — ลำดับ `isAlive` มาก่อน `unitAlive` ในออบเจ็กต์เดียว

ข้อจำกัดที่ต้องพูดตรง ๆ: เทสต์นี้พิสูจน์ว่า **ข้อมูลขาเข้าของ `stateHash` ไม่เปลี่ยน** (mechanical) ไม่ได้รัน hash ของเกมจริง
การยืนยันระดับ runtime ว่าค่า `stateHash` เท่าเดิมจะทำได้ตอน step 12 (เทียบ `spike status` ก่อน/หลัง build ใหม่ในศึกเดียวกัน)

## 3) เทสต์ in-proc 109 checks ครอบอะไร

| กลุ่ม | ตัวอย่าง check | เกี่ยวกับ exit ใด |
| --- | --- | --- |
| Loopback transport | ปิด = refuse+นับ, คิวเต็ม = ทิ้งไม่บล็อก, ทิศทาง guest→host | exit 6, 2 |
| Serializing sender | 5 frames ถึงปลายทางครบ, counters/metrics/writer ตรง, คิวเต็ม = `TryPost` คืน false ภายใน <200 ms (ไม่บล็อก main thread) | exit 2 |
| `S2FrameCodec` | roundtrip ครบ header/unit (รวม row/col = −1), audit recompute ตรง, payload ถูกตัด = fail-closed, unit > 64 = ปฏิเสธ, message > 64 KB = โยน | exit 4 |
| audit tuple | ค่าเดิม→ค่าเดิม, ไม่ขึ้นกับลำดับ, เปลี่ยนเมื่อ hp/isAlive/row/battleTime/turnIndex เปลี่ยน, **ไม่เปลี่ยน**เมื่อ moved/attacked เปลี่ยน | exit 4 |
| guest gate (exit 6) | Hello/Snapshot/BattleEvent/World(แบบ chunk) = Forbidden + นับ; Auth/Ping/Resync/Bye = Control; empty/garbage/trailing/idx≥total = Malformed; chunk ไม่ครบ = Await ไม่ทิ้ง; **`commandsAccepted == 0` ตลอด**; gate มี entry point เดียวคือ `Accept` และรับได้แค่ `byte[], long, out` (ไม่มีทางรับ game object) | exit 6 |
| `coop.net.*` | default ปลอดภัยเมื่อไม่มีคีย์, role/secret ผิด = invalid, `Describe()` ไม่มี secret, missing file ไม่โยน, **เปลี่ยนไฟล์กลางรันแล้ว poll รอบถัดไปเห็นค่าใหม่ (สลับ A/B ได้จริง)** | D2-i, exit 3 |
| `S2PhaseStats` | mean/min/max/reset ต่อเฟส + กติกา "|B−A| < 1 ms = สรุปไม่ได้" | exit 3 |
| ndjson writer | 1 บรรทัด/ระเบียน, append ต่อจากเดิม, ไม่มี secret | หลักฐาน |

## 4) ดีไซน์ที่ต้องรับรู้ (เพื่อตรวจให้ตรงเจตนา)

1. **ไม่มี write path** — โมดไม่มีเมธอดที่เขียน state เกมใน S2 เลย; `S2Counters.CommandsAccepted` ไม่มีเมธอดเพิ่มค่า
2. **main thread เท่านั้นที่แตะ IL2CPP** — `CoopMod.Tick()` → `S2HostService.Tick()` → `S2Sampler.Read()`; serialize/encode อยู่บน `S2Serialize` thread; คิวขนาด 2, เต็ม = ทิ้ง + นับ (`FramesQueuedDropped`)
3. **เฟส A/B** — A = `coop.net.enabled=true` (S2 ทำงาน), B = `false` (idle สนิท); ทุกการสลับเขียน marker
   `{"kind":"phase.start"|"phase.idle"|"phase.end", ...}` ลง `s2-snapshots.ndjson` เพื่อ align กับ recorder ภายนอก
4. **self-test ในรัน** — ตอน activate ครั้งแรกยิง frame ต้องห้าม 3 ตัว (Hello, Snapshot, BattleEvent แบบ chunk) จากฝั่ง guest เข้า host แล้ว drain; เขียน `{"kind":"selftest",...}` → ทำให้ exit 6 วัดได้จริงตอนรัน พร้อมรู้ baseline ว่า `forbiddenTypeDrops` มาจาก self-test 3 ตัว
5. **guard เพิ่ม (ใหม่, ไม่อยู่ในแผนเป๊ะ ๆ)** — S2 จะไม่ activate ถ้า `AllowRemoteInput=true` หรือ `DevCommands=true` (กันไม่ให้มี write path อื่นเปิดอยู่ระหว่างวัด) — ถ้าไม่เห็นด้วยแก้ได้บรรทัดเดียวที่ `S2HostService.GuardWritePathsClosed`
6. **ที่เก็บหลักฐาน** — โมดเขียน `UserData\LongYinCoop\S2\s2-snapshots.ndjson` (โฟลเดอร์เกม, ไม่ใช่ repo) แล้วค่อย copy เข้า `evidence\` ตอนปิด stage
7. `S2HostService`/`S2Sampler` ไม่ถูก execute ในขั้นนี้ — ตรวจแค่ **compile ผ่าน (Debug)**

## 5) สิ่งที่พบว่า**ขัดกับเอกสารเดิม** และส่วนที่ยังไม่ยืนยัน

### 5.1 `BattleUnit.unitAlive` ไม่มีใน interop assembly (RUNTIME-evidence: string heap)

ตรวจ `MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll` (ไฟล์ที่ใช้เป็น reference ตอน compile):
เจอ `get_IsAlive`, `DeadToAlive`, `CheckIfAlive` แต่ **ไม่เจอ `unitAlive` เลย**

⇒ เขียน `u.unitAlive` แบบ typed ไม่ได้ (และห้าม raw offset โดยไม่อนุญาต)
⇒ replica ของ S2 จึงเก็บ **`isAlive` (`u.IsAlive`) ตัวเดียว** และ **ตัดฟิลด์ `unitAlive` ออกจาก replica/codec/ndjson**
⇒ หมายเหตุ AGENTS ที่ว่า "`unitAlive` ยัง true ในเฟรมที่ hp→0" อาจหมายถึงฟิลด์ดิบใน dump ที่ probe อ่านแบบ raw
— **ต้องให้ฉันสรุปว่าจะจัดการอย่างไร** (เลือก: ใช้ `IsAlive` อย่างเดียว / อนุมัติ raw read เฉพาะฟิลด์นี้ / ตรวจ dump.cs ยืนยันชื่อจริงก่อน)

### 5.2 ยังไม่ยืนยัน (GUESS/SPEC)

- `battleKey` = sha256(teamIDs เรียง + frame แรกที่เห็นในศึก) — เป็น **candidate** ตาม §13.2 ข้อ 2 ยังต้องวัดความเสถียรในรัน; ถ้าไม่คงที่ให้เปลี่ยนเป็น sessionId
- `hero.maxhp` (typed) ผ่าน compile แต่ยังไม่ยืนยันค่าระดับ runtime ว่าเป็น max hp ของศึก
- ตัวเลข budget 4 ms/frame และค่า p99 ต้องวัดจริงที่ step 12 (ยังไม่มีข้อมูลรัน)
- `coop.net.*` เป็นคีย์ที่**ไม่ได้ register** กับ MelonPreferences (เราอ่านไฟล์เอง) — MelonLoader อาจลบคีย์ที่มันไม่รู้จักเมื่อเขียนไฟล์ใหม่ (ตอนเกมปิด) ⇒ ต้องเพิ่มคีย์ใหม่ทุกครั้งก่อนรัน และต้อง recording sha256 ของ `MelonPreferences.cfg` ก่อน/หลัง (จะทำใน step 5–12)

### 5.3 หมายเหตุด้านหลักฐาน

`.gitignore` มี `evidence/**/*.ndjson` และ `coop-test/` ⇒ ไฟล์รันจริง (`s2-snapshots.ndjson`) จะไม่เข้า git
ต้องสรุปเป็นตาราง/summary ใน `evidence/` ตอนปิด stage

## 6) สถานะความปลอดภัย ณ ตอนเขียน (วัดแล้ว)

| รายการ | ค่า |
| --- | --- |
| เกมรันอยู่ | ไม่ (process = 0) |
| `Mods\LongYinCoop.dll` sha256 | `517758147f59170e584075a4b42f61e00d293803febe36f8a040bcdad4592612` (เดิม ไม่ถูกเขียนทับ) |
| `Mods\*.off` | 0 ไฟล์ |
| `UserData\MelonPreferences.cfg` sha256 | `269b571c1b1572b7b525cda3e8a27e09b732b8eb31bdfe290269c8ed62a098d1` (เดิม) |
| `Save` | 57 ไฟล์ digest `6dd645d9ef17` · `leftovers: none` (ตรง baseline 20261021-200450) |
| git | ยังไม่ commit (stage นี้ต้องรออนุมัติ diff) |

## 7) ยังไม่ได้ทำ (รออนุมัติ)

step 1 commit → step 2 Release build + `.dll.off` → step 3 split สำเนาเซฟ → step 4 เปิด sampler → step 5 preflight →
step 6 ผู้ใช้เปิดเกม → step 7–12 เก็บผล A/B-A-B + เขียนรายงานปิด stage
