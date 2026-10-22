# S2a-3-plan — TcpTransport (loopback) + viewer-client + metrics + RESYNC

**สถานะ: ฉบับเสนอ 2026-10-22 · ยังไม่อนุมัติ · ยังไม่เขียนโค้ด · ยังไม่ build · ยังไม่ deploy · ยังไม่เปิดเกม**
อ้างอิงน้ำหนัก: `S2-plan.md` (rev2, §13 มีผลเหนือ §1–§12) > `S2a-2-plan.md` (rev4/rev5) > `evidence/**` > `STAGEPLAN.md` §1.1/§1.4/§1.6/§1.7/§5/§10
**ไม่ใช้** `STAGEPLAN.v2-draft.md` เป็นข้อสรุป (ยังไม่อยู่ใน git และไม่ได้รับอนุมัติ)

ป้ายหลักฐาน: **RUNTIME** = วัดจากของที่รันแล้ว · **CODE** = อ่านจาก source ที่ ship/มีใน repo · **SPEC** = ข้อกำหนดในแผน · **GUESS** = ยังไม่ยืนยัน

---

## 0. ขอบเขตของ stage นี้ (และสิ่งที่ไม่ทำ)

**ทำ (S2a-3 = ตาม `S2-plan.md` §13.3 แถว S2a-3 เท่านั้น)**
- `TcpTransport` ฝั่ง host (listen/accept บน loopback) + framing/reassembly บน stream
- handshake + auth จริงครั้งแรก (S2a-2 ยังไม่เคยมี handshake — loopback ไม่มี peer)
- `seq` / ตรวจจับ gap / `RESYNC` / disconnect-reconnect
- `tools/viewer-client.py` (Python, stdlib เท่านั้น) = guest ตัวจริงสำหรับวัด
- metrics ทั้งสองฝั่ง + เครื่องวัด frame time ต่อเฟรม (p99) ในมอด
- วัด exit criteria ของ S2a-3 (§6)

**ไม่ทำ (เด็ดขาด)**
- ไม่มี write path ไปยัง state เกม (เหมือน S2a-2) · ไม่มี type คำสั่งเกมในช่อง guest→host
- ไม่ทำ overlay IMGUI / `WriteGuard` hook / role=guest ในมอด → **S2a-4**
- ไม่ทำ TCP ข้ามเครื่อง / VPN / bind นอก loopback → **S2b** (ต้องมีคำตอบ Q3 ของ rev2 ก่อน)
- ไม่แตะ `IpcServer.cs`, `TurnGate.cs`, `tools/save-split.py`, `tools/s15d_sample.py`, mod อื่น (`LongYinThai`, `UnityExplorer`)
- ไม่แตะเซฟจริง/ไม่เปลี่ยน baseline (`4226730f66ff`) · ไม่เปิดเกมเอง (ผู้ใช้เปิดเท่านั้น)
- ไม่เพิ่ม dependency ใหม่ (`csproj` ต้องไม่มี `PackageReference`; ใช้ `System.Net.Sockets`)

---

## 1. ข้อเท็จจริงที่ตรวจโค้ดรอบนี้ (CODE / RUNTIME) — และสิ่งที่มันบังคับให้แผนเปลี่ยน

| # | ข้อเท็จจริง | ที่มา | ผลต่อแผน |
|---|---|---|---|
| A1 | `seq` ที่ส่งบน wire **= `frame.FrameId` ของ sampler** (`(uint)frame.FrameId` ตอน encode) ไม่ใช่ตัวนับของ session | CODE `S2/Host/S2Sender.cs:297-300` | ถ้า sender ทิ้งเฟรม (`TryPost` คิวเต็ม capacity 2) seq จะ "ขาด" ทั้งที่ไม่ได้หายบนเน็ต → guest จะยิง RESYNC ปลอม · **ต้องย้าย seq ไปนับตอน send จริง** |
| A2 | sampler ถูกเรียก **ทุกเฟรม Unity** และ post ทุกเฟรม (ไม่มี dedup/pacing) | CODE `S2/S2HostService.cs:89-96`, `CoopMod.cs:106` | ที่ ~128 fps [RUNTIME S1.5g] bandwidth = payload×fps · **ต้องมี change-triggered + pacing + heartbeat** |
| A3 | payload จริงที่ 9 ยูนิต = **304 B (p50, p95=305, max=305)** · คำนวณจาก codec: header ≈ 85 B (battleKey hex 64 ตัว) + ≈ 24.3 B/ยูนิต ⇒ 85 + 9×24.3 = **304 B ตรงกับที่วัด** | RUNTIME `UserData\LongYinCoop\S2\s2-snapshots.ndjson` (49,901 เฟรม, unitCount=9 ทุกเฟรม, `payloadBytes` p50) | ใช้เป็นฐานพยากรณ์ bandwidth ที่ 60 ยูนิต (§6.3) |
| A4 | 49,901 เฟรมมี frameId 3..49903 **ต่อเนื่อง 0 gap** ⇒ รอบ S2a-2 ไม่มี drop จริง | RUNTIME (ไฟล์เดียวกัน) | เป็นหลักฐานว่า "gap ที่จะวัดได้ใน S2a-3" ต้อง ** induce เอง** (client-side drop) ไม่มีของจริงให้รอ |
| A5 | **ยังไม่มี handshake ในโค้ดที่รัน**: `S2Auth` มีแต่ primitive, `S2HostService` ไม่เคยส่ง `Hello` และไม่เคยตรวจ `Auth` (gate แค่นับ `ControlFramesHandled`) | CODE `S2/Protocol/S2Auth.cs`, `S2/S2HostService.cs:293-305`, `S2/Protocol/S2Types.cs:36` | S2a-3 = **implementation แรกของ handshake** ต้องมี test vectors + timeout/rate-limit ครบ |
| A6 | **`RESYNC` ถูก allowlist ผ่านแต่ไม่มีใครทำอะไร**: `S2GuestGate.GateOnType` คืน `Control` แล้วทิ้ง message | CODE `S2/Host/S2Counters.cs:137-152` | ต้องมี `S2ControlHandler` + counter `ResyncServed` + snapshot ที่ติดธง `resync` |
| A7 | wire payload **ไม่มี** `replicaHash`, ไม่มี timestamp, ไม่มี `sessionId` | CODE `S2/Host/S2Sender.cs:115-147` | divergence และ latency วัดไม่ได้ถ้าไม่เพิ่มฟิลด์ → **ProtoVer 2** (Q1) |
| A8 | `net.cfg` มีแค่ `enabled/role/secret/allowNonLoopback` — **ไม่มี bind/port/rate** | CODE `S2/S2NetConfig.cs`, งาน A | ต้องเพิ่ม key + เทสต์ (Q2) |
| A9 | frame-time **p99 วัดจาก recorder 2 Hz ไม่ได้** (p99 จริง 33.5 ms; noise ระหว่างบล็อก 30 s เอง median 0.067 / p95 0.568 / max 1.101 ms) | RUNTIME `evidence/S2a2-.../frame-time-measurability-2345.txt` | exit 5 (p99) ต้องมี **เครื่องวัดต่อเฟรมในมอด** หรือยอมรับ NOT-RUN (Q3) |
| A10 | `sampleMs+serializeMs` p99 = **0.1047 ms** (48,565 เฟรม) ที่ 9 ยูนิต | RUNTIME `evidence/S2a2-.../metrics-summary.txt` | เกณฑ์ 4 ms ยังมี margin มาก แต่ **ห้ามยกผลไป 60 ยูนิต** |
| A11 | `battleKey` เป็น **candidate** (sha256 ของ teamIDs + frame แรกที่เห็น) — ยังไม่เคยวัดความเสถียร | CODE `S2/Host/S2Sampler.cs:148-178` + SPEC §13.2 ข้อ 2 | S2a-3 วัดเพิ่ม (ฟรี) ว่า battleKey เปลี่ยนกลางศึก/ข้ามเทิร์นไหม — ถ้าเปลี่ยน ใช้ `sessionId` เป็นกุญแจ resync |

**สรุปผลกระทบ:** โครง S2a-2 ใช้ต่อได้ แต่ **3 จุดต้องแก้ก่อนต่อสายจริง** — seq semantics (A1), อัตราส่ง (A2), และ envelope ที่ขาด hash/timestamp/session (A7)

---

## 2. Architecture + data flow + ไฟล์/คลาส

### 2.1 data flow (S2a-3 = เครื่องเดียว, สองโปรเซส)

```
[Unity main thread]                     [sender thread]                [socket writer thread]
S2Sampler.Read()  ──POCO──► S2HostService ──TryPost──► S2SerializingSender ──enqueue──► TcpTransport
  (IL2CPP typed read,        (pacing/dedup/                 (encode v2 + chunk)      (framed bytes)
   tApplyUs at entry)         seq assignment)                                             │
                                                                                          ▼ 127.0.0.1:47800
                                                                        ┌──────── viewer-client.py (guest) ────────┐
                                                                        │ stream reader → frame → chunk → snapshot │
                                                                        │ → decode → replicaHash recompute → store │
                                                                        │ → viewer.ndjson (per message)            │
                                                                        └──── PING / RESYNC / BYE (allowlist) ─────┘
                                                                                          │
[Unity main thread] ◄── TryReceive ◄── TcpTransport(inbox) ◄───────────────────────────────┘
        └─► S2GuestGate(allowlist) → S2ControlHandler → counters/resync (ไม่แตะเกม)
```

**threading (บังคับตาม §1.4):** IL2CPP อ่านเฉพาะ main thread · serialize ที่ sender thread · socket I/O ที่ transport thread · main thread ห้าม block เกิน 4 ms/เฟรม · object ของ IL2CPP ห้ามข้ามเฟรม

### 2.2 ไฟล์/คลาสที่จะเพิ่ม

| ไฟล์ | คลาส/เนื้อหา | เหตุผล |
|---|---|---|
| `src/LongYinCoop/S2/Transport/TcpFrameStream.cs` | `TcpFrameStream` — buffer stream → frame (partial/coalesced, cap 1100, fail-closed) **pure, ไม่มี socket** | ทดสอบ reassembly ได้โดยไม่ต้องมีเน็ต |
| `src/LongYinCoop/S2/Transport/TcpTransport.cs` | `TcpTransport : ITransport` — listen/accept (host), connect (guest/ทดสอบ), bounded **message** queue, writer thread, drops ไม่ block | S2a-3 ตัวจริง |
| `src/LongYinCoop/S2/Protocol/S2Handshake.cs` | `S2Handshake` — state machine (Hello→Auth→Result→Deny), nonce ต่อ connection, timeout 5 s, pending ≤4, rate 1/5 s/IP, fieldHash echo check | A5 |
| `src/LongYinCoop/S2/Protocol/SeqTracker.cs` | `SeqTracker` — expectedSeq, gap detect (host+guest role), resync request/ack, counters **pure** | A1/A6 |
| `src/LongYinCoop/S2/Protocol/BindPolicy.cs` | `BindPolicy.Validate(bind, allowNonLoopback, secret)` — ปฏิเสธ `0.0.0.0`/`::`/`*`/hostname/ว่าง, loopback เท่านั้นถ้าไม่ตั้ง pref **pure** | §13.2 ข้อ 6 |
| `src/LongYinCoop/S2/Host/S2ControlHandler.cs` | จัดการ `Auth/Ping/Resync/Bye` → counter + resync (ไม่มี write path ใด ๆ) | A6 |
| `src/LongYinCoop/S2/Host/S2FrameMeter.cs` | `Add(double deltaMs)` + window p50/p95/p99 (ring 4096, pure) + adapter อ่าน `Time.unscaledDeltaTime` | A9 |
| `tools/viewer-client.py` | guest client: handshake, stream/chunk, decode v2, **reimplement replicaHash**, metrics ndjson, `--inject-*`, `--report` (stdlib) | exit 1/2/3/4 วัดจากตัวนี้ |
| `tests/S2Protocol.Tests/S2a3Tests.cs` | ชุดเทสต์ใหม่ (§5) | — |
| `tests/S2Protocol.Tests/FakeS2Host.cs` | host ปลอม (ไม่มีเกม) สำหรับ T1 + ให้ Python client ยิงใส่ได้ | ทดสอบโดยไม่เปิดเกม |
| `tests/S2Protocol.Tests/fixtures/replica-vectors.ndjson` | vector ที่ C# สร้าง → Python ใช้ `--selftest` | กัน Python hash เพี้ยน |

### 2.3 ไฟล์ที่จะแก้ (แก้เท่าที่จำเป็น)

| ไฟล์ | แก้อะไร | ความเสี่ยง |
|---|---|---|
| `S2/Protocol/S2Types.cs` | `ProtoVer 1→2`, เพิ่ม `ResyncAck=11`, helper `IsDataMessage` | ต้องแก้พร้อม codec/client (atomic) |
| `S2/Host/S2Snapshot.cs` | envelope: `SessionId`, `HostApplyUs`, `ReplicaHash`, `Resync`, `ResyncFromSeq` + ndjson **additive** (`sessionId`,`hostApplyUs`,`replicaHash`,`resync`,`wireBytes`) | ฟิลด์เดิมไม่เปลี่ยนชื่อ คงเทียบกับ S2a-2 ได้ |
| `S2/Host/S2Sender.cs` | **seq ตอน send**, dedup (เทียบ `replicaHash`), pacing, heartbeat, นับ wire bytes จาก `transport.Stats` และหักลบ header ให้ตรง | จุดสำคัญที่สุดของ stage |
| `S2/Host/S2Sampler.cs` | เก็บ `HostApplyUs` ที่ **ต้น** `Read()` (ก่อนแตะ IL2CPP) + คงพฤติกรรมเดิมทุกอย่าง | 1-2 บรรทัด |
| `S2/Host/S2Counters.cs` | counter ใหม่: `SeqSent, GapReports, ResyncServed, ResyncRequests, Handshake*, Denied*, MalformedTotal, WireBytes` | — |
| `S2/S2HostService.cs` | เลือก transport ตาม cfg (`loopback|tcp`), accept/handshake loop, control handling, framemeter, marker ใหม่, stop ที่ปิด socket | ไฟล์ใหญ่สุดที่แตะ |
| `S2/S2NetConfig.cs` | key ใหม่ `bind`, `port`, `sendHz`, `heartbeatMs`, `transport` (+ validate) | ต้องเทสต์ fail-closed |
| `CoopMod.cs` | `StageLabel = "S2a-3"`, `Version 0.4.0 → 0.5.0` | cosmetic |
| `S2-plan.md` | §13.10 addendum: protocol v2 + key ใหม่ + seq semantics | เอกสาร |
| `AGENTS.md` (game repo) | สถานะ stage + ข้อเท็จจริงใหม่ | commit แยก |

**ไม่แตะ:** `IpcServer.cs` (metrics อ่านจาก ndjson ไม่ใช่ IPC), `TurnGate.cs`, `tools/save-split.py`, `tools/s15d_sample.py`

---

## 3. Protocol: framing, seq, gap, RESYNC, disconnect/reconnect

### 3.1 framing บน TCP (ใช้ header เดิมทั้งหมด)
```
[u16 BE bodyLen][u8 type][u8 ver=2][u32 BE seq][payload]     2+bodyLen ≤ 1100
```
- `TcpFrameStream` รับ byte ทีละก้อนได้ (1 B ถึงหลาย frame ติดกัน) · frame ผิด (`bodyLen<6`, `2+bodyLen>1100`, type ไม่รู้จัก, ver ไม่ตรง) → **ปิด connection ทันที** + log reason (fail-closed, ไม่พยายามเดา)
- message ใหญ่ → `Chunk{msgId, idx, total, type}` เดิม (cap 64 KB, timeout 2 s, pending ≤8)
- **queue เป็น "message" ไม่ใช่ "frame"** — chunk ทุกใบของ message ถูก enqueue/ทิ้งพร้อมกัน ⇒ ไม่มีทางประกอบ message จาก chunk ที่ขาดกลาง (capacity 4, ทิ้งใบเก่าสุด + นับ)

### 3.2 sequence
| กติกา | ค่า |
|---|---|
| `seq` นับอะไร | **เฉพาะ data message ที่ host ส่งจริง** (`Snapshot`) — เริ่ม 1 ต่อ connection |
| message ควบคุม (`Hello/AuthResult/Deny/Pong/ResyncAck/Bye`) | `seq = 0` และ **ไม่ถูกนับ** |
| ใครกำหนด | sender thread ตอน `Send` สำเร็จ (ไม่ใช่ sampler) ⇒ drop/pacing **ไม่ทำให้ seq ขาด** |
| guest คาดหวัง | `expectedSeq` ต่อ session; `newSeq == expectedSeq` = ปกติ, `newSeq > expectedSeq` = **gap**, `newSeq < expectedSeq` = duplicate/stale (ทิ้ง + นับ) |
| เปลี่ยน session | `sessionId` ใหม่ทุก connection → guest ล้าง store, `expectedSeq=1` |

### 3.3 ตรวจจับ gap และ RESYNC
guest ยิง `Resync{lastSeq}` เมื่ออย่างใดอย่างหนึ่ง:
1. **seq gap** (`newSeq > expectedSeq`) — บันทึก `gapSize`, เปิด "gap window"
2. **ไม่มีข้อมูลเกิน 2×`heartbeatMs`** (ดีฟอลต์ 2 s) — บันทึก `stallDetected`
3. **Div-A mismatch** (replicaHash ที่คำนวณเอง ≠ ที่ host ส่งมา) — บันทึก `hashMismatch`

host เมื่อได้ `Resync`:
- `ResyncRequests++` → ส่ง `ResyncAck{fromSeq, nextSeq, state=idle|battle}` **ทันที** (seq=0, ไม่กิน seq) แล้วส่ง snapshot ถัดไปพร้อมธง `resync=true, resyncFromSeq=lastSeq` **ข้าม pacing 1 ครั้ง**
- `ResyncServed++` + marker `resync.served{fromSeq,ms}` ลง `s2-metrics.ndjson`
- rate limit: ยอมรับ Resync ≤ 5 ครั้ง/วินาที ต่อ connection (เกิน = ทิ้ง + นับ `ResyncThrottled`)

guest ปิด gap window เมื่อได้ snapshot ที่ `resync=true` **และ** Div-A ตรง ⇒ บันทึก `gapOpenMs` (p50/max) ลง summary
ถ้า 500 ms ไม่ได้ resync → ยิงซ้ำ (สูงสุด 3) → ยังไม่ได้ = ปิด connection แล้ว reconnect (นับ `resyncGiveUp`)

### 3.4 disconnect / reconnect (guest)
- ตรวจด้วย: `recv` คืน 0 / exception / ไม่มี heartbeat
- backoff 0.5 → 1 → 2 → 4 s (เพดาน 4 s) · reconnect แล้ว handshake ใหม่ (nonce ใหม่, secret เดิม)
- ระหว่างหลุด: host **ไม่ปิด listener** และ **ไม่ปิด state อะไรของเกม** (ไม่มี state ให้ปิด) · เขียน `session.close{reason}` + `session.open` ใหม่
- host ปิด connection เมื่อ: protocol violation 3 ครั้งติด / write error / ไม่มี traffic ใด ๆ 60 s (`guestIdleMs`)

### 3.5 envelope v2 (payload ของ `Snapshot`)
```
u8 ver=2 | u32 sessionId | varu64 hostApplyUs | 32B replicaHash | u8 flags(resync) | varu32 resyncFromSeq
| str battleKey | varu32 turnIndex | str activeUnitId | u8 paused | varu32 nowActiveState | f32 battleTime
| varu32 unitCount | unit×N { str stableId | varu team | varu heroId | varu row | varu col | f32 hp | f32 maxHp
                     | u8 isAlive | u8 unitAlive | u8 moved | u8 attacked | u8 playerControl }
```
- ต้นทุนเพิ่ม ≈ 40 B/snapshot (ยอมรับได้) · `unitAlive` **คงไว้** (ห้ามแก้ hash spec `s2|2` — มติ 2026-10-21 ข้อ 2(ii))
- `replicaHash` ที่ส่ง = ค่าที่ host คำนวณ (ตัวเดียวกับ ndjson) เพื่อให้ guest เทียบแบบ end-to-end

---

## 4. Security

| ข้อบังคับ | กลไก | วัด/ทดสอบ |
|---|---|---|
| bind เฉพาะ loopback | `BindPolicy`: รับเฉพาะ IP literal; **ปฏิเสธ** `0.0.0.0`,`::`,`*`,hostname,ว่าง; ถ้าไม่ใช่ loopback ต้อง `allowNonLoopback=true` **และ** มีอนุมัติเป็นลายลักษณ์ (S2b) | เทสต์ 8 เคส + ตรวจ `Socket.LocalEndPoint` หลัง bind ว่าเป็น loopback + log บอก IP:port |
| authentication | shared secret (hex ≥32) → `Hello{nonce}` → `Auth{HMAC(secret, nonce‖ver), ver, fieldHashPrefix8}` → `AuthResult`/`Deny` · เทียบด้วย `FixedTimeEquals` · secret **ไม่เคยส่งบนสาย/ไม่ log** | vector T0 + secret ผิด/สั้น/HMAC เพี้ยน/protoVer ผิด/fieldHash ผิด → DENY ≤2 s (fieldHash ≤1 s) |
| guest อ่านอย่างเดียว | allowlist 4 ชนิด (`Auth/Ping/Resync/Bye`) เท่านั้น · **ไม่มี type คำสั่งเกมในสารบบ** · ไม่มีโค้ดเขียน state เกมใน `S2/**` | (a) ยัด Hello/Snapshot/BattleEvent(chunk) → DROP 3 + `commandsAccepted=0` + audit tuple เท่าเดิม (ต่อจาก self-test เดิม) (b) grep call sites (`MoveCommand.StartMove|AttackCommand.StartAttack|BattlePauseButtonClicked|GameDataController.Save|Load`) ใน `src/LongYinCoop/S2/**` = 0 (c) ไม่มี `IpcServer` command ใดถูกเรียกจาก S2 |
| ขนาด/ abuse | guest frame: single frame เท่านั้น, payload ≤ 64 B, เกิน = malformed · violation 3 ครั้ง = ปิด · handshake pending ≤4, timeout 5 s, rate 1/5 s/IP | เทสต์ malformed 12 เคส |
| ไม่มี TLS | ยอมรับเป็นข้อจำกัด (loopback เท่านั้นใน stage นี้); S2b ต้องพึ่ง VPN + secret | บันทึกในรายงาน |
| denylist | ไม่แตะ Steam/`APIDispatcher`/`DnsChecker`/`CheatCheck`/`cheat*`/`Save/Load` · ไม่เพิ่ม dependency | ตรวจ diff + `csproj` review |

---

## 5. Test matrix

ชั้น: **T0** = pure (ไม่ socket) · **T1** = in-proc จริงบน `127.0.0.1` (ยังไม่ต้องมีเกม) · **T2** = เกมจริง + `viewer-client.py`

| # | เคส | ชั้น | คาดหวัง | หลักฐาน |
|---|---|---|---|---|
| U1 | frame stream: ทีละ 1 B / รวม 3 เฟรม / frame 1100 B พอดี / 1101 B | T0 | ประกอบถูก, 1101 = ปิด | `tests` log |
| U2 | frame ผิดรูป: `bodyLen<6`, type=0, ver≠2, trailing bytes | T0 | ปิด connection + reason, ไม่ throw รั่ว | |
| U3 | chunk: ปกติ / idx ซ้ำ / idx เกิน total / total ไม่ตรง / timeout 2 s | T0 | complete เมื่อครบ, ที่เหลือ = drop | |
| U4 | seq: ปกติ, gap 1, gap 17, duplicate, wraparound u32 | T0 | gap/duplicate นับถูก, ไม่ throw | |
| U5 | resync state machine: ขอ→ack→snapshot(resync)→ปิด window; 3 ครั้งไม่ได้ → giveUp | T0 | ทรานซิชันครบ + เวลาไม่เกิน limit | |
| U6 | handshake: สำเร็จ / secret ผิด / สั้น / HMAC เพี้ยน / protoVer ผิด / fieldHash ผิด / timeout / pending เกิน / rate | T0 | ผล + เวลาตรงสเปก | |
| U7 | bind policy 8 เคส (`127.0.0.1`, `localhost`, `0.0.0.0`, `::`, `*`, `192.168.x.x` + ไม่ตั้ง pref, ว่าง, `::1`) | T0 | ผ่านเฉพาะ loopback literal / `::1`; ที่เหลือปฏิเสธพร้อมเหตุผล | |
| U8 | replicaHash: vector 5 ชุด (C#) ↔ Python recompute | T0/T1 | ตรงทุกไบต์ | `fixtures/replica-vectors.ndjson` |
| U9 | **wire v2 roundtrip**: encode→TCP→decode ผ่าน header/session/timestamp/hash | T1 | field ครบ, hash ตรง | |
| U10 | bandwidth/projection: 9/18/24/60 ยูนิต × sendHz | T0 | ตัวเลขตรงสูตร §6.3 (±1 B) | |
| I1 | listen/accept บน 127.0.0.1 พอร์ตว่าง → handshake สำเร็จ → 100 snapshot ต่อเนื่อง | T1 | ครบ 100, seq ต่อเนื่อง, ไม่มี drop | |
| I2 | client หยุดอ่าน 3 s (ไม่ recv) → host `Send` ยังไม่บล็อก (< 1 ms/call) + drop นับ + ไม่ crash | T1 | ผ่าน; หลัง client กลับมาอ่านได้ snapshot ใหม่ + resync | |
| I3 | client ตัด connection กลาง chunk | T1 | host ไม่ crash, ปิด session, accept ใหม่ได้ | |
| I4 | reconnect 3 รอบ (backoff) | T1 | sessionId ใหม่ทุกรอบ, seq รีเซ็ต, store ล้าง | |
| I5 | inject forbidden (`Hello/Snapshot/BattleEvent-chunk`) จาก client | T1 | `forbiddenTypeDrops=3`, `commandsAccepted=0` | |
| I6 | inject malformed 12 แบบ (สั้น/ยาว/type มั่ว/chunk ซ้ำ/payload 65 B) | T1 | ปิดตามกติกา / ไม่ crash / นับครบ | |
| I7 | inject gap (client ทิ้ง seq เอง) → resync → Div-A = 0 | T1 | gap ตรวจเจอ, resync สำเร็จ, gap window ปิด | |
| I8 | 2 client พร้อมกัน (กรณีไม่ควรเกิด) | T1 | ปฏิเสธตัวที่สองอย่างสุภาพ (`Deny{busy}`) หรือปิดตัวเก่า — **เลือก: ปฏิเสธตัวใหม่** | |
| I9 | Python client ↔ fake host (`--serve-fake`) 60 s | T1 | ndjson ครบ, `--report` ให้ p95/bytes/s, Div-A=0 | |
| E1 | **เกมจริง**: เข้าศึก → client ต่อ → handshake OK → ได้ snapshot จริง | T2 | log 2 ฝั่งตรงกัน, unitCount ตรงกับ `spike status` รอบเดียวกัน | |
| E2 | 5 นาที delivery ต่อเนื่อง (ผู้เล่นเล่น 2-3 เทิร์น) | T2 | latency/bandwidth/Div-A/Div-B (§6) | |
| E3 | ผู้เล่น Esc 1 ครั้ง (บอกผม) | T2 | บันทึกหน้าต่าง exclude, ไม่ตีความเป็น pause ของ S2 | |
| E4 | stall 60 s (client หยุดอ่านเอง) | T2 | host p99 (§6.5) + ไม่ crash + กลับมาอ่านได้ | |
| E5 | disconnect 60 s แล้ว reconnect | T2 | handshake ใหม่สำเร็จ, Div-A=0 หลังต่อ | |
| E6 | ยิง forbidden จาก client ระหว่างเกมจริง | T2 | audit tuple ไม่เปลี่ยน + counters | |
| E7 | ปิดเกม → `verify`/`restore` | T2 | เซฟ 57 ไฟล์ `4226730f66ff`, `leftovers: none` | |

**กติกาการนับ verdict:** FAIL ข้อใด = รายงานตรง ๆ ห้ามเลื่อนเกณฑ์ · เคสที่ "วัดไม่ได้" = **NOT-RUN** (ห้ามเขียน PASS) · เคสที่ได้ผลแต่เล็กกว่าเป้า (เช่น unit count < 60) = **PASS-with-limitation** + เหตุผล

---

## 6. เกณฑ์วัด + วิธีวัดที่ทำซ้ำได้

### 6.1 แหล่งเวลา (ต้องพิสูจน์ก่อนใช้ ไม่เดา)
- host stamp: `Stopwatch.GetTimestamp()` → µs (`HostApplyUs` = **ต้น** `S2Sampler.Read()` = "instant ที่มอดเห็น state ของเกมครั้งแรก")
- guest stamp: `time.perf_counter_ns()/1000`
- สมมติฐานที่ต้องวัด: บน Windows ทั้งสองฝั่งใช้ QPC → **offset ≈ 0** · วัดด้วย PING/PONG 4 timestamp (สูตร NTP) ทุก 5 s, ใช้ **median ของ 5 ค่าล่าสุด** เป็น offset
- **เกณฑ์ความน่าเชื่อถือ:** spread ของ offset ในหน้าต่างวัด ≤ **0.5 ms** ⇒ ใช้ได้; เกิน ⇒ verdict = PASS-with-limitation (รายงาน raw + corrected)
- latency = `recvUs − HostApplyUs − offset` · รายงาน p50/p95/p99/max + n + unit count + offset(median, spread)

### 6.2 ตัวชี้วัดและเกณฑ์

| ID | เกณฑ์ (SPEC) | metric | เครื่องมือ | ผ่านเมื่อ | ถ้าวัดไม่ได้ |
|---|---|---|---|---|---|
| **X1** | loopback latency **p95 ≤ 40 ms** | host-apply → guest-store ต่อ snapshot | `viewer.ndjson` (§6.1) | p95 ≤ 40 ms, n ≥ 1000 | n < 1000 หรือ offset spread > 0.5 ms ⇒ PASS-with-limitation |
| **X2** | bandwidth **≤ 30 KB/s เฉลี่ย 5 นาที** | wire bytes ที่ guest อ่านได้ (รวม framing, ไม่รวม TCP/IP header) / วินาที | `viewer.ndjson` bucketed 1 s | avg ≤ 30 KB/s ในหน้าต่าง 300 s | unit count < 60 ⇒ **PASS-with-limitation + พยากรณ์ §6.3** (ห้ามเรียกว่าผ่านที่ 60) |
| **X3** | divergence **= 0** | **Div-A**: `sha256(replicaHash ที่ guest คำนวณจาก field ที่รับ) ≠ replicaHash ในข้อความเดียวกัน` | client recompute + `replicaHash` บน wire | mismatches = 0 จากทุก snapshot ที่รับ (≥1000) **และ** unit count/duplicate id ตรง | ถ้า client ไม่ได้ recompute = NOT-RUN |
| **X4** | seq ขาด → resync → divergence 0 | `gapDetected ≥ 1`, `resyncServed ≥ 1`, `gapOpenMs` ปิดทุก window, Div-A หลัง resync = 0 | counters 2 ฝั่ง + marker | ครบทุกข้อ | ไม่มี gap จริง (A4) ⇒ ต้อง induce; ถ้า induce ไม่ได้ = NOT-RUN |
| **X5** | host frame time **p99 เปลี่ยน ≤ 1 ms** ขณะ guest หน่วง/หลุด | `Time.unscaledDeltaTime` ต่อเฟรม, window 60 s, p99 | `S2FrameMeter` ในมอด (marker `framemeter.window`) | `p99(B1|B2) − p99(A0) ≤ 1 ms` และ exception = 0 | ไม่มี meter (ถ้าไม่อนุมัติ Q3) = **NOT-RUN** · ถ้า |delta−1 ms| ≤ 0.5 ms = INCONCLUSIVE (กติกาเดียวกับ rev5) |
| **X6** | main-thread cost ต่อเฟรม **p99 ≤ 4 ms** | `sampleMs + encodeMs` (เดิมของ S2a-2) | `S2Metrics` (มีอยู่) | p99 ≤ 4 ms | — |
| **X7** | auth/bind | bind loopback ✓; secret ผิด → DENY ≤ 2 s; fieldHash ผิด → DENY ≤ 1 s; bind นอก loopback ถูกปฏิเสธ | marker + `handshake` log | ครบ | — |
| **X8** | guest อ่านอย่างเดียว | `commandsAccepted = 0`, `forbiddenTypeDrops = 3` (self-test), audit tuple เท่าเดิม, grep call sites = 0 | counters + `grep` | ครบ | grep ทำมือ = หลักฐานใน evidence |
| **X9** | ไม่แตะเซฟ | `SaveSlot*` เขียน 0 ไฟล์; digest = `4226730f66ff`; `leftovers: none` | `save-split.py verify/status` | ครบ | — |

### 6.3 พยากรณ์ bandwidth (จาก A3: header 85 B + 24.3 B/ยูนิต + envelope v2 40 B + frame header 8 B/frame; frame ละ ≤ 1092 B payload)

| ยูนิต | payload v2 | จำนวน frame | wire/snapshot | @10 Hz | @30 Hz | @60 Hz (พฤติกรรม S2a-2) |
|---|---|---|---|---|---|---|
| 9 | 344 B | 1 | 352 B | 3.4 KB/s | **10.3 KB/s** | 21.1 KB/s |
| 18 | 562 B | 1 | 570 B | 5.6 KB/s | **16.7 KB/s** | 33.4 KB/s ✗ |
| 24 | 708 B | 1 | 716 B | 7.0 KB/s | **21.0 KB/s** | 42.0 KB/s ✗ |
| 60 | 1583 B | 2 | 1599 B | 15.6 KB/s | **46.9 KB/s ✗** | 93.7 KB/s ✗ |

**ข้อสรุปที่ต้องพูดตรง ๆ:** ที่ **60 ยูนิต full snapshot 30 Hz = 46.9 KB/s > 30 KB/s** ⇒ ถ้าต้องผ่านทั้ง X1 (40 ms) และ X2 (30 KB/s) ที่ 60 ยูนิตพร้อมกัน **codec นี้ทำไม่ได้** ต้องมี delta/event encoding (แผนจัดไว้ S2b) · ที่ 18 ยูนิต 30 Hz = 16.7 KB/s ผ่าน X2 แต่ p95 อาจชน 40 ms ถ้า change ส่วนใหญ่ถูก pacing ทิ้ง — จะวัดจริงและรายงานตามจริง (ห้ามลดเกณฑ์) · S2a-3 วัดที่จำนวนยูนิตจริงของเซฟ (เคยได้ 9 และ 18) แล้วรายงาน **PASS-with-limitation + พยากรณ์** (Q9)

### 6.4 นิยาม divergence (ขอบเขตชัดเจน — ห้ามหมายความเกิน)
| ชั้น | นิยาม | วัดได้ใน S2a-3? | เกณฑ์ 0 ใช้กับชั้นนี้? |
|---|---|---|---|
| **Div-A** | guest แปลง message → คำนวณ `replicaHash` ใหม่ = ค่าที่ host ส่งมาในข้อความเดียวกัน (fidelity ของ codec + hash spec + framing) | ✅ วัดได้เป๊ะทุก message | **ใช่ — X3/X4** |
| **Div-B** | guest store "เก่า" กว่าข้อมูลล่าสุดของ host (seq gap / stall) | ✅ วัดเป็น gap window (ms) + resync | ไม่ใช่ divergence แต่ **ต้องปิดทุก window** (X4) |
| **Div-C** | guest store ≠ state จริงของเกม ณ เวลานั้น | ❌ ไม่มี peered game ฝั่ง guest (overlay = S2a-4) | **ไม่กล้าเคลม** — ใกล้สุดคือ exit 4 ของ S2a-2 (9 ยูนิต, ไม่ขยายผล) |

### 6.5 ลำดับวัดในเกมจริง (ตามลำดับ ห้ามสลับ)
1. `phase A0` S2 ปิด 60 s (baseline ของ X5) — ต้องอยู่ `WaitPlayerInput` และ **ห้ามสั่ง action**
2. `phase W` S2 เปิด + client ต่อ 300 s (ผู้เล่นเล่น 2–3 เทิร์น) → X1/X2/X3 + X6 + unit count
3. `phase B1` client stall 60 s (client หยุดอ่านเอง) → X5
4. `phase B2` client ถูก kill 60 s → X5 + host log 0 exception
5. `phase B3` client reconnect 60 s → X4 (resync) + Div-A = 0
6. `phase A1` S2 ปิด 60 s อีกครั้ง (ยืนยัน baseline ซ้ำ) → X5 ใช้ `A0`+`A1` เฉลี่ย
- ทุกบล็อก: ตรวจว่า `sampler.ndjson` ยังโต (recorder ตาย = ทิ้งบล็อกนั้น) + จด wall-clock
- **ห้าม pause ตอน `WaitPlayerInput`** (มติ S1.5) · ถ้ากด Esc → บอกผมเพื่อ exclude

---

## 7. แผน implementation เป็นช่วง + acceptance + rollback + หลักฐาน

| ช่วง | ทำอะไร | ต้องมีเกม? | ต้องอนุมัติ? | acceptance | rollback |
|---|---|---|---|---|---|
| **C0** | แผนนี้ | ❌ | **รออนุมัติ (ตอนนี้)** | ผู้ใช้อนุมัติ + ตอบ Q1–Q11 | ไม่มีอะไรเปลี่ยน |
| **C1** | เขียนโค้ด §2.2/§2.3 + เทสต์ §5 (T0/T1) · **ไม่ deploy** | ❌ | อนุมัติแล้วจาก C0 | `dotnet run --project tests\S2Protocol.Tests` = PASS ทุกเคส, 0 FAIL · รายงาน diff | `git checkout -- .` / revert commit |
| **C2** | commit source (ยังไม่ build) + ส่งสรุป diff | ❌ | **gate #1 (อนุมัติ build)** | tree สะอาด + commit hash | revert commit |
| **C3** | `dotnet build -c Release` → sha256 DLL + build log (ยังไม่ copy) | ❌ | gate #1 | 0 error/0 warning ใหม่ | ลบ `bin/` |
| **C4** | สำรอง DLL ปัจจุบัน (`LongYinCoop.dll` → `.off`/`.prev-<hash>`) · backup `MelonPreferences.cfg` + `net.cfg` · **ล้าง dotted key เก่าใน MelonPreferences.cfg ถ้ามี** (ต้นเหตุ incident 23:54) | ❌ | **gate #2 (อนุมัติ deploy)** | sha256 ครบทั้งก่อน/หลัง + ไฟล์ใหม่ตรง build | copy `.off` กลับ |
| **C5** | copy DLL+PDB เข้า `Mods\` · ตั้ง `net.cfg` (enabled=false, role=host, secret 32B hex, bind=127.0.0.1, port=47800, sendHz=30) + sha256 | ❌ | gate #2 | sha256 ใน `Mods\` = C3 | คืน `.off` + คืน cfg |
| **C6** | session-start checks (hash เกม, เกมไม่รัน, `save-split status`=57/`4226730f66ff`/leftovers none) → `split --expected-baseline coop-test\baselines\20261022-075422\savehash.real.before.txt` → recorder `s15d_sample.py` → `preflight` **READY** | ❌ | gate #2 | READY + split manifest | `restore` |
| **C7** | **ผู้ใช้เปิดเกม** → เข้าศึก → แจ้งผม → ผมรัน §6.5 → ผู้ใช้เล่น 2–3 เทิร์น → แจ้ง Esc/เหตุการณ์ → ปิดเกม | ✅ | **ผู้ใช้เปิดเองเท่านั้น** | ทุก phase ครบ + `viewer.ndjson` + markers | ปิดเกม + `.off` |
| **C8** | ปิดเกม → `verify` → `restore` → คืน pref + `net.cfg` (sha256 ตรงก่อนตั้ง) → `rebaseline` ถ้า pref drift → รวบรวม evidence | ❌ | — | 57 ไฟล์ `4226730f66ff`, `leftovers: none` | restore ซ้ำ |
| **C9** | รายงานเทียบ X1–X9 ทีละข้อ + rollback test | ❌ | **gate #3 (อนุมัติ tag `checkpoint-S2a`)** | verdict ครบทุกข้อ (รวม NOT-RUN) | — |

**มติ 2026-10-22 (อัปเดต baseline ของ stage):** baseline ที่ใช้ตัดสิน E7/X9/C6/C8 เปลี่ยนจาก `854cc890430f` → **`4226730f66ff`** (`coop-test/baselines/20261022-075422`) หลังคืนเซฟจริงของ session `explore-20261022-0755` แล้ววัดได้ 57 ไฟล์ `changed=0 missing=0 added=0` · หลักฐาน `evidence/S2a3-20261022-0156/restore-postcheck.md`, `presave-check.md`, `coop-test/baselines/20261022-075422/REBASELINE.json` + `APPROVAL.md` · `854cc890430f` ยังเป็นประวัติของ S2a-2 (ห้ามใช้เป็นเกณฑ์ของ stage นี้)

**หลักฐาน `evidence/S2a3-<ts>/`**
`plan-ref.md` · `code-diff-summary.md` · `commit.txt` · `tests.txt` (จำนวน PASS/FAIL) · `build.txt` (log + sha256) · `dll-before-after.txt` · `prefs-sha-before.txt` · `prefs-sha-after.txt` · `netcfg-values.txt` (ไม่ log secret) · `preflight.txt` · `split.json` · `sampler.ndjson` · `s2-snapshots.ndjson` · `s2-metrics.ndjson` (จาก `UserData\LongYinCoop\S2`) · `viewer.ndjson` · `viewer-summary.txt` · `latency.txt` · `bandwidth.txt` · `divergence.txt` · `resync.txt` · `framemeter.txt` · `unit-count.txt` · `audit-tuple.txt` · `nowrite-grep.txt` · `verify-restore.txt` · `report.md`

---

## 8. Build / Deploy / เปิดเกม — จุดที่ต้องขออนุมัติ (ชัดเจน)

| จุด | สิ่งที่ทำได้เมื่ออนุมัติ | ต้องขออนุมัติก่อน |
|---|---|---|
| **gate #1** หลัง C1/C2 | `dotnet build -c Release` (สร้าง DLL ใหม่) | ✅ ต้องอนุมัติ **ก่อน build** (ตามกติกา ห้าม build จนได้รับอนุมัติ) |
| **gate #2** หลัง C3 | copy DLL เข้า `Mods\`, แก้ `net.cfg`/`MelonPreferences.cfg`, split เซฟ, เปิด recorder, preflight | ✅ ต้องอนุมัติ **ก่อน deploy** |
| **เปิดเกม** | — | **ผมไม่เปิดเองเด็ดขาด** — ผู้ใช้เปิดหลัง preflight READY เท่านั้น |
| **gate #3** หลัง C9 | tag `checkpoint-S2a` (+ `stage-S2a-3` เมื่ออนุมัติปิด stage) | ✅ อนุมัติหลังอ่านรายงาน |

หมายเหตุ: `dotnet run --project tests\S2Protocol.Tests` (C1) **ไม่ใช่การ build/deploy มอด** — มัน link source แบบ read-only เข้า assembly ของเทสต์ (`S2Protocol.Tests.csproj:13-28`) และไม่แตะ `Mods\` ( precedent: S2a-2 step 0 ทำแบบนี้แล้ว )

---

## 9. ความเสี่ยง

| ความเสี่ยง | ผล | ทางลด |
|---|---|---|
| เปลี่ยน seq semantics กระทบ ndjson เดิม | เทียบกับ S2a-2 ตรง ๆ ไม่ได้ | เพิ่ม `frameId` แยกจาก `seq` ใน ndjson (additive) + จดใน §13.10 |
| pacing/dedup ทำให้ "เห็น state ใหม่ช้า" | ผู้เล่นรู้สึกหน่วง | change-triggered ส่งทันที + heartbeat 1 Hz + วัด staleness (Div-B) แยกจาก latency |
| dedup ซ่อนการเปลี่ยนแปลง | viewer ค้างค่าเก่า | hash ครอบ hp/pos/alive/flags; heartbeat บังคับ refresh; ทดสอบด้วยการเดิน/โจมตีจริง |
| 60 ยูนิตชนเพดาน bandwidth (A3/§6.3) | exit 2 ผ่านไม่ได้ที่เป้า | บันทึกเป็น limitation + พยากรณ์ + เสนอ delta encoding ใน S2b (Q9) |
| thread ใหม่ (socket×2) ทำให้เฟรมสะดุด | X5/X6 ตก | bounded queue, ไม่ lock ร่วมกับ main thread, วัดด้วย meter + เทียบ A/B |
| `MelonPreferences.cfg` มี dotted key ค้าง → TOML เสีย → มอดนิ่ง (เคยเกิดจริง) | วัดไม่ได้ทั้งรอบ | C4 ล้าง key เก่า + backup + sha256 + preflight ตรวจ |
| เข้าใจผิดว่า guest สั่งได้ | ขัดเจตนา co-op | allowlist + ไม่มี type คำสั่งในสารบบ + counter + grep + audit tuple |
| TCP listener ถูก firewall/AV ทัก | เปิดเกมสะดุด | bind `127.0.0.1` เท่านั้น (ไม่ควรมี prompt) · ถ้ามี prompt = หยุดและรายงาน ไม่กด allow เอง |
| client Python ช้า/GIL ทำให้ค่าผิด | verdict เพี้ยน | client ไม่ทำงานหนักต่อ message (ไม่วาด UI, ไม่ parse JSON หนัก), เขียน ndjson แบบ buffered |
| 2 client ต่อพร้อมกัน | สับสนค่า | ปฏิเสธตัวที่สอง (`Deny{busy}`) + เทสต์ I8 |

---

## 10. คำถามที่ต้องให้ผู้ใช้ตัดสิน (ห้ามเริ่มจนได้คำตอบ)

| # | คำถาม | ข้อเสนอของผม | ผลถ้าเลือกทางอื่น |
|---|---|---|---|
| **Q1** | อนุมัติ **ProtoVer 1→2** (เพิ่ม `sessionId`, `hostApplyUs`, `replicaHash`, `resync` flag, type `ResyncAck=11`; **ไม่ลบฟิลด์ใด, ไม่แก้ `ReplicaHash` spec `s2|2`**)? | อนุมัติ | ถ้าไม่ bump: latency/divergence วัดไม่ได้ตามนิยาม ต้องใช้การประมาณ (อ่อนกว่า) |
| **Q2** | อนุมัติ key ใหม่ใน `net.cfg`: `bind=127.0.0.1`, `port=47800`, `sendHz=30`, `heartbeatMs=1000`, `transport=tcp` (+validate fail-closed)? | อนุมัติ | ถ้าไม่เพิ่ม: ต้อง hardcode (แก้ทีต้อง build ใหม่) |
| **Q3** | อนุมัติ **`S2FrameMeter`** (อ่าน `Time.unscaledDeltaTime` แบบ getter, ring 4096, เขียน marker) เป็นเครื่องวัด p99 ของ X5? | อนุมัติ (p99 วัดด้วย recorder 2 Hz ไม่ได้ — A9) | ถ้าไม่อนุมัติ: X5 = **NOT-RUN** อย่างเป็นทางการ |
| **Q4** | อนุมัติ **เพิ่มไฟล์ใหม่** `tools/viewer-client.py` (ไม่แตะไฟล์ tools เดิม) | อนุมัติ | ถ้าไม่อนุมัติ: ไม่มี guest จริง → stage นี้วัดไม่ได้ |
| **Q5** | อนุมัติการเปลี่ยน **seq = นับตอน send** (แทน sample frameId) + `frameId` คงไว้ใน ndjson | อนุมัติ | ถ้าไม่เปลี่ยน: pacing/drop ทำให้เกิด RESYNC ปลอม |
| **Q6** | อนุมัติ **change-triggered + `sendHz=30` + heartbeat 1 Hz** (ทดแทน "ส่งทุกเฟรม") | อนุมัติ | ถ้าไม่เปลี่ยน: bandwidth 18 ยูนิต @60 Hz = 34 KB/s (ตกเกณฑ์) |
| **Q7** | **ห้าม bind นอก loopback ใน stage นี้** (S2a-3 = loopback เท่านั้น; S2b ค่อยขออนุมัติ IP VPN) | ตาม §13.2 ข้อ 6 | ถ้าต้องการนอก loopback = ต้องอนุมัติเป็นลายลักษณ์ + เปิด `allowNonLoopback` |
| **Q8** | อนุมัติให้ **แตะ `IpcServer.cs` ไหม** เพื่อเพิ่มคำสั่งอ่านอย่างเดียว `s2_status` (metrics สด) | **ไม่แตะ** — อ่านจาก `s2-metrics.ndjson` พอ | ถ้าต้องการสด: +1 ไฟล์ที่มีความเสี่ยง (ต้องเทสต์ IPC ใหม่) |
| **Q9** | **60 ยูนิตยังไม่มี fixture** — ยอมรับ X2 = PASS-with-limitation + พยากรณ์ §6.3 หรือไม่ (ห้ามลดเป้า 60 ตามมติ Q4 ของ rev2) | ยอมรับ limitation + บันทึก | ถ้าไม่ยอมรับ: ต้องหา/สร้างเซฟ 60 ยูนิตก่อน (บล็อก stage) |
| **Q10** | เวลาในเกมที่ต้องใช้ ≈ **10–12 นาที** ในศึกเดียว (A0 60s + W 300s + B1/B2/B3 180s + A1 60s) — รับได้ไหม | รับได้ | ถ้าไม่ได้: ตัด W เหลือ 180 s (ผลจะ weaker, บันทึกไว้) |
| **Q11** | ระหว่างวัด **ผู้ใช้เล่น 2–3 เทิร์น** (ไม่ใช่ปล่อยนิ่ง) เพื่อให้ divergence/bandwidth มีความหมาย — ตกลงไหม | ตกลง | ถ้าปล่อยนิ่ง: ได้เฉพาะ idle-stream ไม่ครอบคลุมการต่อสู้ |

---

## 11. สิ่งที่จะไม่เกิดขึ้นใน stage นี้ (ย้ำ)
- ไม่มีคำสั่งจากเครือข่ายที่แตะ state เกม · ไม่มี `Save/Load` · ไม่แตะ Steam/สถิติ/`cheat*`
- ไม่มี overlay/IMGUI · ไม่มี role=guest ในมอด · ไม่มี VPN/ข้ามเครื่อง
- ไม่แตะเซฟจริง · ไม่เปลี่ยน baseline `4226730f66ff` · ไม่เปิดเกมเอง · ไม่ build/deploy ก่อนอนุมัติ

---

## 12. อ้างอิงภายในเล่ม
`S2-plan.md` §13.2 (ข้อบังคับ protocol/audit/bind), §13.3 (S2a-3 scope), §13.4 (exit 1–11), §13.6 (rollback) ·
`S2a-2-plan.md` §5 (นิยาม exit เดิม), §8.2 (audit คำสั่ง IPC), §8.5 (วินัยการวัด rev5) ·
`evidence/S2a2-20261021-2254/` (`metrics-summary.txt`, `frame-time-measurability-2345.txt`, `S2a2-report.md`) ·
`UserData\LongYinCoop\S2\s2-snapshots.ndjson` (49,901 เฟรม, 9 ยูนิต, 304 B) ·
`VERSION.md` (anchors + baseline `4226730f66ff`)
