# S2a-3 measurement session 2026-10-24 (label `S2a3x5-20261024-0132`)

Build ใน `Mods\`: `LongYinCoop.dll` sha256 `5d7b7cc5…5953` (X5 meter: `S2FrameMeter` ring 65536 + `coverageMs`) · PDB `…`
เกม pid 28612 (ผู้ใช้เปิด 01:29:19) · สถานะตลอดการวัด: `Fighting / WaitPlayerInput`, turn 0, 9 units
Recorder `s15d_sample.py` pid 44188 (`--interval 0.1`) · `net.cfg` เริ่มที่ `enabled=false` (sha `12541f34c633`)

## X5 — paired A/B (host frame p99)

แผน: A0 · B1(stall) · A1 · B2(stall) · A2 · หน้าต่างละ 60 s (`framemeter.window`, coverage ≈ 60.000 s, n ≈ 7,400/หน้าต่าง)
B = guest เปิด socket ค้างแต่ **หยุดอ่าน** 60 s (`viewer-client.py --stall 60`) ⇒ host เขียนค้างใน outbox

| บล็อก | p99 (ms) | mean (ms) | max (ms) | n | coverageMs |
|---|---|---|---|---|---|
| A0 (S2 off) | 27.783 | 8.088 | 111.107 | 7,419 | 60,004 |
| B1 (stall) | 34.719 | 8.083 | 83.334 | 7,423 | 59,997 |
| A1 (S2 off) | 27.783 | 8.104 | 118.046 | 7,404 | 60,004 |
| B2 (stall) | 34.720 | 8.054 | 90.278 | 7,450 | 60,004 |
| A2 (S2 off) | **34.720** | 8.119 | 104.168 | 7,390 | 59,997 |

- `delta = mean(p99 B) − mean(p99 A) = 34.7195 − 30.0953 = **+4.624 ms**` → ตามเกณฑ์ (≤1 ms, margin 1.0, band ±0.5) = **FAIL**
- **หลักฐานว่า p99 ไม่จำแนก S2:** A2 (S2 ปิด) ให้ **34.720** เท่ากับ B ทั้งสอง — ค่า p99 กระโดดเป็นขั้นตาม bucket ของ frame time
  (`p50 = 6.945 ms` ทุกหน้าต่าง = 1/144 s ⇒ p99 ตกที่ 27.78 หรือ 34.72 สลับกัน)
- mean frame time: A 8.104 ms vs B 8.069 ms ⇒ **−0.035 ms** (ไม่มีผล) · p95 เท่ากัน 13.894/13.895 ms
- guest ฝั่ง stall: `snapshots=75`, `gaps=0`, `latencyP95 ≈ 57 s` (ข้อมูลค้างใน buffer แล้วมาทีเดียวหลัง stall จบ) ⇒ การ stall เกิดจริง
  แต่ **host ไม่บล็อก** เพราะ `TcpTransport.Send` เป็น TryAdd ลง outbox (capacity 256) + writer thread (source: `Transport/TcpTransport.cs:15,410`)
  และที่ 1 Hz/60 s มีเพียง ~60 frame < 256 ⇒ ไม่มี drop และไม่มี backpressure
- host log: exception 0 (`[S2]` ไม่มี error ระหว่างบล็อก)

## X6 — main-thread cost ต่อเฟรม (`sampleMs + serializeMs`)

| รอบ | heartbeatMs | n (cumulative) | combinedP99 | sampleP99 | serializeP99 | verdict |
|---|---|---|---|---|---|---|
| 2026-10-24 01:11 (5 Hz) | 200 | 1,080 | **0.1258 ms** | 0.1187 | 0.0095 | PASS (มี limitation: rate สูงกว่า default) |
| 2026-10-24 01:48–02:05 (**default**) | 1000 | 1,413 | **0.1337 ms** | 0.1184 | 0.0070 | **PASS** |

- รอบ 1 Hz: marker `s2.metrics` ระหว่างทาง n = 1174 (0.1379) → 1234 (0.1379) → 1294 (0.1379) → 1354 (0.1337) → 1413 (0.1337)
  ⇒ ไม่ใช่ผลของ rate: 0.126–0.138 ms ทั้งสอง rate, ห่างเพดาน 4 ms ~29 เท่า
- guest ในรอบนี้: `snapshots=1015`, `seq 1..1015`, `gaps=0`, `duplicates=0`, `malformedRecv=0`, `errors=0`

## X1 / X2 / X3 จากรอบ 1 Hz (n = 1015 ≥ 1000)

- **X1** raw latency: p50 **0.193 ms**, p95 **0.277 ms**, p99 **0.957 ms**, max 6.449 ms → ≤ 40 ms
  limitation: NTP offset ใช้ไม่ได้ (spread **21.4465 ms**, corrected negative 771/1015) ⇒ รายงาน raw เท่านั้น (เหมือน W)
- **X2** `wireBytes = 362,024 B / 1,020 s = **0.355 KB/s**` (9 units) → ≤ 30 KB/s · limitation: 9 < 60 units
- **X3** Div-A mismatch **0 / 1015** · duplicates 0 · unitCount 9 คงที่

## X7 — auth/bind (host log `26-10-24_1-29-19.log`)

| ข้อ | วิธีวัด | ผล |
|---|---|---|
| bind loopback | `netstat` + host log | `listening on tcp-listen(127.0.0.1:47800) (loopback only…)` · `Get-NetTCPConnection` = `127.0.0.1:47800` เท่านั้น |
| secret ผิด | client `--secret 000…` | host DENY `code=1 hmac mismatch` (`[02:06:40.739] guest auth failed: Denied BadAuth`) ⇒ ≤ 0.3 s (เกณฑ์ ≤ 2 s) |
| fieldHash ผิด | `--field-hash deadbeef… --ignore-host-hash` (ส่ง AUTH จริง) | host DENY `code=3 fieldHash prefix mismatch` (`[02:07:50.261] guest auth failed: Denied FieldMismatch`) ⇒ ≤ 0.3 s (เกณฑ์ ≤ 1 s) |

- tooling change: `tools/viewer-client.py` เพิ่ม `--ignore-host-hash` (Python only) เพื่อไม่ให้ client ตัดการเชื่อมต่อเองก่อนถึง host deny path
- หลังทดสอบ `net.cfg` กลับเป็น `enabled=false` sha `12541f34c633` (454 B)

## X8 — guest อ่านอย่างเดียว (markers ใน `s2-snapshots.ndjson`)

- `selftest` (ทุกครั้งที่ activate; รอบนี้ blockIndex 1–6):
  `injectedFrames=4 chunks=2 expectedForbiddenDrops=3 gained=3 commandsAccepted=0 auditEqual=true exit6=PASS`
- `selftest.tcp`: `handshakeOk=true sentForbidden=true forbiddenGained=3 commandsAccepted=0 exit8=PASS`
- host log ระหว่าง phase: `commandsAccepted=0` ทุกบรรทัด (`forbidden=` เพิ่มขึ้นตาม self-test)
- `grep -rn "CommandsAccepted++|Spike|AllowRemoteInput" src/LongYinCoop/S2` → ไม่มี call site ที่เขียน state เกม (มีแต่ `S2WriteGuard` ที่อ่าน pref)
