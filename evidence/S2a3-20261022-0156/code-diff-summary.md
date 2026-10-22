# code-diff-summary — S2a-3 (งาน C) · C1

ฐาน: mod repo `0cc5fe0` (`master`) · ทุกอย่างในเอกสารนี้ **ยังไม่ commit** ณ เวลาที่เขียน (commit hash อยู่ใน `commit.txt`)
สรุป: **13 ไฟล์เดิมถูกแก้ (+965 / −130)** · **11 ไฟล์ใหม่ใน `src/`** · **2 ไฟล์ใหม่ใน `tests/`** · **1 fixture** · **1 tool Python**

---

## 1. ไฟล์ที่แก้ (ของเดิม)

| ไฟล์ | +/− | สาระที่เปลี่ยน |
|---|---|---|
| `src/LongYinCoop/S2/S2HostService.cs` | +574 / −… | rewrite: ประกอบ transport/handshake/control/sender/selftest, accept+deny busy, resync serving, `framemeter.window` เขียนได้แม้ S2 ปิด (ให้มี baseline A0) |
| `src/LongYinCoop/S2/Host/S2Sender.cs` | +243 | envelope v2, `S2SenderOptions`, dedup+pacing (skip ไม่ทับ `_lastSentHash`), heartbeat 1 Hz, seq ตอนส่ง, `FlagResync` |
| `src/LongYinCoop/S2/S2NetConfig.cs` | +105 | อ่าน `transport`/`bind`/`port`/`sendHz`/`heartbeatMs` + validate (fail-closed) จาก `net.cfg` (flat) |
| `src/LongYinCoop/S2/Host/S2Counters.cs` | +51 | counter ใหม่: forbidden drops, resync, gap windows, disconnect reason |
| `src/LongYinCoop/S2/Protocol/S2Auth.cs` | +31 | รองรับ protoVer 2 + fieldHash/protoVer mismatch แยกเหตุผล |
| `src/LongYinCoop/S2/Protocol/S2Types.cs` | +16 | `ProtoVer = 2`, `ResyncAck = 11`, `IsDataMessage` (allowlist) |
| `src/LongYinCoop/S2/Host/S2Snapshot.cs` | +26 | field `sessionId`/`hostApplyUs`/`replicaHash`/resync flag ในmessage |
| `src/LongYinCoop/S2/Host/S2Sampler.cs` | +4 | `HostApplyUs` = timestamp ที่ต้น `Read()` |
| `src/LongYinCoop/S2/Transport/ITransport.cs` | +3 | สัญญาใหม่ (`TakeNewConnection`, `LastError`/`LastDisconnect`, `RequestClose`) |
| `src/LongYinCoop/CoopMod.cs` | +6 / −6 | `MelonInfo` → `0.5.0`, `StageLabel = "S2a-3"` |
| `tests/S2Protocol.Tests/Program.cs` | +22 / −… | `S2a3Tests.Run()` + CLI `--emit-vectors`, `--fake-host` |
| `tests/S2Protocol.Tests/S2Protocol.Tests.csproj` | +7 | link ไฟล์ใหม่ + copy `fixtures/**` ไป output |
| `tests/S2Protocol.Tests/S2a2Tests.cs` | +7 / −… | call site เปลี่ยนไปใช้ `S2SenderOptions` (พฤติกรรมเดิม) |

## 2. ไฟล์ใหม่ — `src/` (1,770 บรรทัด)

| ไฟล์ | บรรทัด | หน้าที่ |
|---|---|---|
| `S2/Transport/TcpTransport.cs` | 402 | listener + reader/writer thread, bounded queue, `BusyFrame`, `TakeNewConnection`, `RequestClose`, `LastError` vs `LastDisconnect` |
| `S2/Protocol/S2Handshake.cs` | 430 | `Hello/Auth/AuthResult/Deny`, HMAC, fieldHash, protoVer, timeout, pending limit |
| `S2/Host/S2ControlHandler.cs` | 220 | `Ping` / `Resync` / `Bye` — ไม่มีเส้นทางเขียน state เกม |
| `S2/Protocol/SeqTracker.cs` | 186 | ตรวจ seq, นับ gap/dup/wraparound, `ResyncState` (ขอ/ack/ปิด window/giveUp) |
| `S2/Host/S2TcpSelfTest.cs` | 172 | self-test forbidden 3 ชนิดผ่าน TCP จริง (`selftest.tcp`) |
| `S2/Host/S2FrameMeter.cs` | 120 | `Time.unscaledDeltaTime` ต่อเฟรม + marker `framemeter.window` |
| `S2/Transport/TcpFrameStream.cs` | 86 | header 8 B, bodyLen, ver, chunk assemble |
| `S2/Protocol/BindPolicy.cs` | 68 | อนุญาตเฉพาะ loopback literal + `::1` |
| `S2/Protocol/S2RateLimiter.cs` | 53 | 1 ครั้ง / 5 s หลัง auth ล้มเหลว |
| `S2/Protocol/S2Fields.cs` | 33 | field-set ที่ hash ครอบ + `GameAnchor` (hardcode จาก `VERSION.md`) |
| `S2/Protocol/ReplicaHash.cs` | — | **ไม่ได้แก้** (spec `s2|2` คงเดิม — ต้องตรงทั้งสองฝั่ง) |

## 3. ไฟล์ใหม่ — `tests/` + fixture + tool

| ไฟล์ | บรรทัด | หน้าที่ |
|---|---|---|
| `tests/S2Protocol.Tests/S2a3Tests.cs` | 1,378 | U1–U10, I1–I8, `EmitVectors` |
| `tests/S2Protocol.Tests/FakeS2Host.cs` | 295 | host แบบ headless (ใช้ใน I1/I3/I4/I6/I7/I8 + `--fake-host`) |
| `tests/S2Protocol.Tests/fixtures/replica-vectors.ndjson` | 9 vector | `V`/`U` line, hp เป็น `G17` ของ float32-as-double |
| `tools/viewer-client.py` | 909 | client วัดผล: `read_vectors`/`cmd_selftest`, `hp_text` (Decimal + ROUND_HALF_EVEN), `replica_hash`, latency (raw + NTP), `--append`, `--inject-forbidden` |

## 4. สัญญาณเชิงพฤติกรรมที่เปลี่ยน (ต้องรู้ก่อน deploy)

1. **wire v2** — เพิ่ม `sessionId`, `hostApplyUs`, `replicaHash`, resync flag, `ResyncAck=11` · **ไม่มี field เดิมถูกถอด** แต่ client/build เก่าที่อ่าน envelope แบบ v1 จะไม่รู้จัก (จึงต้อง deploy พร้อมกัน)
2. **seq ตอนส่ง** ไม่ใช่ `frameId` — ndjson ยังมี `frameId` แยก
3. **dedup + pacing**: `sendHz=30` ค่าเริ่มต้น, heartbeat 1,000 ms; change-triggered ส่งทันที · skip จาก pacing **ไม่ทับ** `_lastSentHash` (เปลี่ยนค้างไว้จะถูกส่งเองรอบถัดไป)
4. **`net.cfg` คีย์ใหม่**: `transport` (loopback), `bind` (127.0.0.1), `port` (47800), `sendHz` (30), `heartbeatMs` (1000) — default เท่ากับพฤติกรรม S2a-2
5. **`S2Sender` legacy path** สำหรับ S2a-2 regression: `Dedup=false, SendHz=0, HeartbeatMs=0` ⇒ พฤติกรรมเดิมทุกอย่าง
6. **`TcpTransport` แยก** `LastDisconnect` (เช่น `read: IOException`) จาก `LastError` — log ของ host จะไม่ตีความ disconnect ปกติเป็น error
7. **`S2HostService` เขียน `framemeter.window` แม้ S2 ปิด** — ตั้งใจ เพื่อให้มี baseline phase A0 ของ X5

## 5. ความเสี่ยงที่ยังเปิดอยู่

| ความเสี่ยง | ระดับ | หมายเหตุ |
|---|---|---|
| `S2HostService.cs` / `S2Sampler.cs` / `CoopMod.cs` **ไม่อยู่ใน test project** ⇒ ยังไม่ผ่านคอมไพเลอร์ | **สูง** | build แรก (C3, หลัง gate #1) อาจมี error — ต้องรายงานตามจริงก่อนไปต่อ |
| 60 ยูนิตชนเพดาน bandwidth (§6.3: 46.9 KB/s) | กลาง | ไม่แก้อะไรใน C1 · X2 จะเป็น PASS-with-limitation |
| `GameAnchor` hardcode | ต่ำ | ถ้าเกมอัปเดต ต้องแก้ค่า (ตรวจ hash ทุก session-start อยู่แล้ว) |
| client เก่า vs v2 | ต่ำ | loopback-only stage นี้, ไม่มี client ภายนอก |
| pacing ทำให้ state ใหม่ช้า | กลาง | วัดด้วย Div-B (gap window ms) ใน C7 |
