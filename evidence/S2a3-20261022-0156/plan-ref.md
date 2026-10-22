# plan-ref — S2a-3 (งาน C) · ช่วง **C1** = โค้ด + เทสต์ T0/T1 เท่านั้น

อ้างอิงแผน: `S2a-3-plan.md` (อนุมัติแล้ว, Q1–Q11)
ขอบเขตของเอกสารนี้: **C1 เท่านั้น** — ไม่ build มอด, ไม่ deploy, ไม่เปิดเกม
หลักฐานการวัดในเกมจริง (T2: E1–E7 / X1–X9) **ยังไม่มี** และจะไม่ถูกเขียนว่า PASS ในเอกสารนี้

---

## 1. §2 Architecture / data flow → ที่อยู่ในโค้ด

| แผน § | สิ่งที่ต้องมี | ที่อยู่ในโค้ด | สถานะ |
|---|---|---|---|
| §2.2 transport | `TcpTransport` (listener + reader + writer thread, bounded queue, ไม่บล็อก main thread) | `src/LongYinCoop/S2/Transport/TcpTransport.cs` (402 บรรทัด) | เขียนแล้ว · คอมไพล์ผ่านเฉพาะทางเทสต์ |
| §2.2 framing | `TcpFrameStream` (header 8 B, bodyLen, ver, chunk) | `src/LongYinCoop/S2/Transport/TcpFrameStream.cs` | เขียนแล้ว |
| §2.2 control handler | `Ping` / `Resync` / `Bye` — **ไม่มี** คำสั่งเขียน state เกม | `src/LongYinCoop/S2/Host/S2ControlHandler.cs` | เขียนแล้ว (no game write path) |
| §2.3 sender v2 | change-triggered + `sendHz` pacing + heartbeat 1 Hz + seq ตอนส่ง | `src/LongYinCoop/S2/Host/S2Sender.cs` (`S2SenderOptions`) | เขียนแล้ว |
| §2.3 sampler | `HostApplyUs` ที่ต้น `S2Sampler.Read()` | `src/LongYinCoop/S2/Host/S2Sampler.cs` | เขียนแล้ว |
| §2.3 meters | frame time ต่อเฟรม + marker `framemeter.window` | `src/LongYinCoop/S2/Host/S2FrameMeter.cs` | เขียนแล้ว |
| §2.3 host wiring | ประกอบทั้งหมด + accept/reject + resync + self-test | `src/LongYinCoop/S2/S2HostService.cs` (rewrite, +574/−…) | เขียนแล้ว · **ยังไม่ถูกคอมไพล์** (ดู §8) |

## 2. §3 Protocol

| แผน § | สิ่งที่ต้องมี | ที่อยู่ในโค้ด | เทสต์ |
|---|---|---|---|
| §3.1 framing | 1 B–3 เฟรม, 1100 B พอดี, 1101 B = ปิด | `TcpFrameStream.cs` | **U1**, U2 |
| §3.1 chunk | idx ซ้ำ/เกิน/total ไม่ตรง/timeout 2 s | `TcpFrameStream.cs`, `FrameCodec.cs` | **U3** |
| §3.2 seq | gap/duplicate/wraparound u32 | `src/LongYinCoop/S2/Protocol/SeqTracker.cs` | **U4** |
| §3.3 RESYNC | ขอ → ack → snapshot(resync) → ปิด window; 3 ครั้ง = giveUp | `SeqTracker.cs` (`ResyncState`), `S2ControlHandler.cs` | **U5**, **I7** |
| §3.4 disconnect/reconnect | แยก disconnect ปกติ (`LastDisconnect`) จาก error จริง (`LastError`); sessionId ใหม่, seq รีเซ็ต | `TcpTransport.cs`, `S2HostService.cs` | **I3**, **I4** |
| §3.5 envelope v2 | `sessionId`, `hostApplyUs`, `replicaHash`, resync flag; `ResyncAck=11`; ไม่มี field เก่าถูกถอด | `src/LongYinCoop/S2/Protocol/S2Types.cs`, `S2Snapshot.cs` | **U9**, I1 |
| handshake | `Hello/Auth/AuthResult/Deny` + HMAC + fieldHash + protoVer | `src/LongYinCoop/S2/Protocol/S2Handshake.cs` (430), `S2Auth.cs` | **U6** |

## 3. §4 Security

| ข้อ | ที่อยู่ในโค้ด | เทสต์/หลักฐาน |
|---|---|---|
| bind loopback เท่านั้น (8 เคส) | `src/LongYinCoop/S2/Protocol/BindPolicy.cs` | **U7** (8 เคส) |
| rate limit handshake | `src/LongYinCoop/S2/Protocol/S2RateLimiter.cs` | **U6** — *เบี่ยงจากแผน ดู §7 ข้อ (1)* |
| allowlist ชนิดข้อความ (client ส่งได้แค่ Ping/Resync/Bye) | `S2Types.IsDataMessage`, `S2ControlHandler.cs` | **I5** (forbidden = 3, accepted = 0) |
| malformed 12 แบบ | `TcpFrameStream.cs`, `S2Handshake.cs` | **I6** |
| ไม่มี call site เขียน state เกมในเส้นทาง TCP | `grep` (ยังไม่ได้ทำใน C1 — อยู่ใน C9) | ยังไม่มีหลักฐาน |

## 4. §5 test matrix — ผลจริง

| ID | ผล | หลักฐาน |
|---|---|---|
| U1–U10 | **PASS ครบ** | `tests.txt` (`S2a-3 RESULT PASS (pass 179 fail 0)`) |
| I1, I3, I4, I6, I7, I8 | **PASS ครบ** | `tests.txt` |
| I2 (client หยุดอ่าน 3 s) | **PASS** ตามที่ implement (host `Send` ไม่บล็อก, drop นับ, ไม่ crash) | `tests.txt` |
| I5 (forbidden) | **PASS** ทั้งทาง in-proc และทาง TCP จริง | `tests.txt`, `i9-forbidden-*.txt` |
| I9 (Python ↔ fake host) | **PASS** (ทำ 10 s แทน 60 s — ดู §7 ข้อ (3)) | `i9-report.txt`, `i9-viewer.ndjson`, `i9-host.out.txt` |
| U8 (C# ↔ Python hash) | **PASS 9/9 vector** | `python-selftest.txt` + `fixtures/replica-vectors.ndjson` |
| รวมทั้ง suite | **421 PASS / 0 FAIL** | `tests.txt` |

## 5. §6.2 เกณฑ์ X1–X9 — สถานะ ณ C1 (ยังไม่วัดในเกม)

| ID | สถานะ C1 | เหตุผล |
|---|---|---|
| X1 latency p95 ≤ 40 ms | **NOT-RUN** | ต้องวัดในเกมจริง (T2) · ตัวเลข loopback บน fake host (`i9-report.txt` p95 0.3 ms) **ไม่ใช่** หลักฐานของ X1 |
| X2 bandwidth ≤ 30 KB/s | **NOT-RUN (คาดว่า PASS-with-limitation)** | พยากรณ์ §6.3: 60 ยูนิต @30 Hz = 46.9 KB/s > 30 · U10 ให้ 9.1/15.9/20.5/47.9 KB/s ที่ 9/18/24/60 ยูนิต |
| X3 divergence = 0 (Div-A) | **NOT-RUN ในเกม** · แต่กลไกพิสูจน์แล้วที่ T1 | Div-A = 0 บน 213 snapshot ผ่าน TCP จริง (`i9-report.txt`) + 9 vector ข้ามภาษา (`python-selftest.txt`) — **ยังไม่ใช่** การวัดในเกม |
| X4 gap → resync → Div-A 0 | **NOT-RUN ในเกม** · กลไก PASS ที่ T1 | `tests.txt` I7: gap ตรวจเจอ 1, resync ปิด window 1, Div-A = 0, maxGapOpenMs = 47 |
| X5 host frame time p99 ≤ +1 ms | **NOT-RUN** | ต้องมี `phase A0/W/B1/B2/A1` ในเกม |
| X6 main-thread p99 ≤ 4 ms | **NOT-RUN** | ต้องมี `S2Metrics` จากเกม |
| X7 auth/bind | **PASS ที่ T0** (U6, U7) · ยืนยันในเกม = **NOT-RUN** |
| X8 guest อ่านอย่างเดียว | **PASS ที่ T1** (`forbiddenTypeDrops=3`, `commandsAccepted=0` ทั้ง in-proc และ TCP) · audit tuple ในเกม = **NOT-RUN** · `grep` call sites = ยังไม่ทำ |
| X9 ไม่แตะเซฟ | C1 **ไม่แตะเซฟเลย** — ไม่มี build/deploy/เปิดเกม · ไม่อ้างตัวเลข digest ของ stage |

## 6. §7 ตำแหน่งของงานตอนนี้

- **C0** (แผน + Q1–Q11) = ปิดแล้ว
- **C1** (โค้ด + เทสต์ T0/T1) = **เสร็จในรอบนี้** — หลักฐานอยู่ในโฟลเดอร์นี้
- **C2** (commit source) = commit เดียวกับ C1 ในรอบนี้ (ดู `commit.txt`)
- **C3** (build) = **ยังไม่ทำ — รอ gate #1**
- C4–C9 = ยังไม่เริ่ม

## 7. เบี่ยงจากแผน (ต้องรายงานตรง ๆ)

1. **rate limit handshake**: แผนเขียน "1 ครั้ง / 5 s ต่อ IP" — ที่ implement คือ **1 ครั้ง / 5 s หลัง auth ล้มเหลวเท่านั้น** (`AllowsNewConnection`) การ reconnect ปกติไม่ถูกจำกัด เพื่อไม่ให้ I4/reconnect จริงพัง
2. **`S2Fields.GameAnchor`** = SHA-256 คู่จาก `VERSION.md` แบบ hardcode · **ไม่ rehash ตอนรัน** (ช้ากว่า 100 MB ทุกครั้ง) — ระบุในโค้ดแล้ว
3. **I9 ทำ 10 s** ไม่ใช่ 60 s (fake host ทำ 60 Hz ไม่ได้จริง ~39 Hz) — ตัวเลข throughput จาก fake host **ห้ามใช้เป็น X2**
4. **X8 self-test**: ทำแบบ synchronous ในเฟรม ผ่าน `LoopbackTransport` ชั่วคราว; ส่วน TCP wire self-test แยกเป็น `selftest.tcp` และรายงาน `auditCrossCheck=NOT-CLAIMED(live-sim)`
5. **seq = ตอนส่ง** ไม่ใช่ `frameId` ของ sampler (เพิ่ม `frameId` ใน ndjson แยก)
6. **`S2Sender` legacy path** คงพฤติกรรม S2a-2 ผ่าน `S2SenderOptions { Dedup=false, SendHz=0, HeartbeatMs=0 }` เพื่อไม่ให้ regression test ของ S2a-2 เปลี่ยนความหมาย

## 8. สิ่งที่ยังไม่ถูกคอมไพล์ (ความเสี่ยงอันดับ 1 ของ C1)

ไฟล์เหล่านี้ **ไม่อยู่ใน `tests/S2Protocol.Tests.csproj`** จึงผ่านแค่ "อ่านด้วยตา" ไม่ผ่านคอมไพเลอร์:

- `src/LongYinCoop/S2/S2HostService.cs`
- `src/LongYinCoop/S2/Host/S2Sampler.cs`
- `src/LongYinCoop/CoopMod.cs`

ไฟล์อื่นทั้งหมดของ C1 (Protocol/, Transport/, Host/S2Sender, S2Snapshot, S2Counters, S2NetConfig, S2ControlHandler, S2FrameMeter, S2TcpSelfTest) ถูก link เข้าเทสต์และคอมไพล์ผ่านจริง

## 9. ดัชนีหลักฐานในโฟลเดอร์นี้

| ไฟล์ | เนื้อหา |
|---|---|
| `preflight.txt` | สภาพก่อนเริ่มวัด (repo, DLL ที่ deploy, hash เกม, สถานะเซฟ) |
| `tests.txt` | ผล `dotnet run --project tests\S2Protocol.Tests -c Release` เต็ม (421 PASS / 0 FAIL) |
| `python-selftest.txt` | U8 ข้ามภาษา 9/9 vector |
| `i9-viewer.ndjson` / `i9-viewer.txt` | client ผ่าน TCP จริง (213 snapshot) |
| `i9-report.txt` | สรุป latency / Div-A / gap / bytes |
| `i9-host.out.txt` / `i9-host.err.txt` | log host ฝั่ง fake |
| `i9-forbidden-*.txt` | ยิง forbidden 3 ชนิด → host drop 3, accepted 0 |
| `plan-ref.md` / `code-diff-summary.md` / `commit.txt` | เอกสารชุดนี้ |
