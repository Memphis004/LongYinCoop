# S2a-3 ปิด stage — รายงานเทียบ X1–X9 (C9)

วันที่: 2026-10-24 · build ใน `Mods\`: `LongYinCoop.dll` sha256 `5d7b7cc5…5953` (X5 meter: ring 65536 + `coverageMs`; X4 fix: `frame.Resync/ResyncFromSeq` ตั้งก่อน `Encode`; X6: `_combined` + marker `s2.metrics`)
รุ่น mod: `0.5.0 stage=S2a-3` protoVer=2 · fieldHash prefix `5424e4cd7dfad630`
เกม: pid 28612 (`[01:29:19]` ผู้ใช้เปิดเอง) · ปิดโดยผู้ใช้ก่อน restore · hash เกมตรง `VERSION.md` ทั้งสองไฟล์ (session-start check 01:11)
เซฟ: baseline `4226730f66ff` (`coop-test/baselines/20261022-075422`) · label `S2a3x5-20261024-0132`

## สรุป verdict

| ข้อ | verdict | ตัวเลข/หลักฐาน |
|---|---|---|
| **X1** latency p95 ≤ 40 ms | **PASS-with-limitation** | raw p95 **0.277 ms**, p50 0.193, p99 0.957, max 6.449, n=1015 · limitation: offset NTP ใช้ไม่ได้ (spread 21.45 ms, corrected-negative 771/1015) ⇒ raw เท่านั้น |
| **X2** ≤ 30 KB/s | **PASS-with-limitation** | **0.355 KB/s** (362,024 B / 1,020 s) · 9 units · limitation: 9 < 60 ตาม Q9 |
| **X3** Div-A = 0 (n ≥ 1000) | **PASS** | **0 / 1015** (รอบ 1 Hz) · สะสม session: 0/1584 (W 299 + W2 120 + B1 75 + B2 75 + 1015), duplicates 0, unitCount 9 คงที่ |
| **X4** gap → resync → ปิด + Div-A 0 | **PASS** | gap 1 (ขนาด 3) · resync sent/ack/**closed = 1/1/1** · giveUp **0** · gapOpenMs 13 · guest ถอดได้ `resync=true, resyncFromSeq=2` (ก่อนแก้เป็น false/0) · Div-A 0/61 |
| **X5** p99 เปลี่ยน ≤ 1 ms ขณะ guest หน่วง | **FAIL** | p99 A = 27.783 / 27.783 / **34.720**, B = 34.719 / 34.720 ⇒ `delta = +4.624 ms` · mean frame time Δ **−0.035 ms** · **A2 (S2 ปิด) = 34.720 เท่ากับ B** ⇒ p99 จำแนกไม่ได้ (ดู §X5) |
| **X6** p99(sample+serialize) ≤ 4 ms | **PASS** | **default 1 Hz: 0.1337 ms ที่ n=1413** · 5 Hz: 0.1258 ms ที่ n=1080 ⇒ ไม่ใช่ผลของ rate |
| **X7** auth/bind | **PASS** | bind `127.0.0.1:47800` เท่านั้น (netstat + host log `loopback only`) · secret ผิด → DENY `hmac mismatch` ≤0.3 s (≤2 s) · fieldHash ผิด → DENY `fieldHash prefix mismatch` ≤0.3 s (≤1 s) |
| **X8** guest อ่านอย่างเดียว | **PASS** | `selftest` gained=3 commandsAccepted=0 auditEqual=true exit6=PASS (blockIndex 1–6) · `selftest.tcp` handshakeOk forbiddenGained=3 exit8=PASS · host log `commandsAccepted=0` ทุกบรรทัด · grep call site เขียน state = 0 |
| **X9** ไม่แตะเซฟ | **PASS** | `SaveSlot*` เขียน 0 (real folder `changed=0` ตอน verify) · restore แล้ว **57 ไฟล์ exact** ทุก baseline (10 ไฟล์เทียบ) · rehash อิสระ changed/missing/added = 0 · `leftovers: none` · `net.cfg` `12541f34c633` (เหมือนก่อนเริ่ม) · pref `269b571c…98d1` |

**สรุป: 7 PASS (ในนั้น 2 มี limitation: X1, X2) · 1 FAIL (X5) · 0 NOT-RUN**

## X5 — ทำไม FAIL และทำไมตัวเลขยังไม่น่าเชื่อ

ตารางเต็ม (window 60 s, coverage ~60.000 s, n ≈ 7,400):

| บล็อก | p99 | mean | max |
|---|---|---|---|
| A0 (S2 off) | 27.783 | 8.088 | 111.107 |
| B1 (guest stall 60 s) | 34.719 | 8.083 | 83.334 |
| A1 (S2 off) | 27.783 | 8.104 | 118.046 |
| B2 (guest stall 60 s) | 34.720 | 8.054 | 90.278 |
| A2 (S2 off) | **34.720** | 8.119 | 104.168 |

- ตามตัวอักษรของเกณฑ์: `delta = 34.7195 − 30.0953 = +4.624 ms` > 1.5 ⇒ **FAIL** — ไม่มีการขยับเกณฑ์
- หลักฐานว่า **p99 ไม่ใช่ตัวชี้วัดที่จำแนกได้**: หน้าต่างที่ S2 ปิด (A2) ให้ค่าเท่ากับ B เป๊ะ (34.720) · `p50 = 6.945 ms` ทุกหน้าต่าง = 1/144 s ⇒ p99 ตกลงบน bucket 27.78 หรือ 34.72 สลับกัน (หนึ่ง bucket = 6.945 ms = 7 เท่าของ margin)
- mean frame time แทบไม่ต่าง (A 8.104 vs B 8.069) · p95 เท่ากัน (13.894/13.895)
- การ stall เกิดจริง (guest `latencyP95 ≈ 57 s`, `snapshots=75`, gaps 0) แต่ **host ไม่บล็อกโดยดีไซน์**: `TcpTransport.Send` = TryAdd ลง outbox 256 + writer thread ⇒ ที่ 1 Hz/60 s (~60 frame) ไม่มี backpressure และไม่มี drop
- ⇒ ข้อสรุปที่ตรง: ตัวเลข FAIL เกิดจาก meter/ตัวชี้วัด ไม่ใช่จากภาระของ guest · **การแก้ต้องขออนุมัติ** (เช่น เลือกใช้ mean/p99.9/ความชันของ tail หรือสร้าง backpressure จริงด้วย `SO_RCVBUF` เล็ก + rate สูง) — ห้ามแก้เกณฑ์เอง

## Rollback ที่พร้อมใช้

- `Mods\LongYinCoop.dll.prev-3c9584a4` (build X6 marker) · `.prev-c51debef` (build C3) · `.off` (`517758147f59…`, S1.5e-W7)
- `net.cfg` = `enabled=false`, sha `12541f34c633` · `MelonPreferences.cfg` sha `269b571c…98d1`
- เซฟ: `Save` = โฟลเดอร์จริงที่ rename กลับ (ไม่ถูกเขียนทับ) · สำเนาทดสอบ `coop-test/S2a3x5-20261024-0132/Save.used-20261024-020908`
- git: tag `stage-S2a-2` @ `14757e3` (mod repo)

## หลักฐาน

- `measurements-20261024.md` (ตาราง X1–X8) · `x5-paired.log`, `x5-blocks.txt` · `x6-1hz.log`, `x6-1hz-metrics.txt` · `viewer-X6-1hz.out.log` · `x7-wrongsecret.*`, `x7-wronghash3.*`, `viewer-B1/B2.out.log` · `x8-nowrite-grep.txt` · `x9-verify-20261024.txt`, `x9-restore-20261024.txt`, `x9-rehash.txt`
- รอบก่อนหน้าใน stage: `evidence/S2a3-20261022-143608/{x4-rerun-20261024.md,x6-run-20261024.md,c8c-verify-20261024.txt,c8c-restore-20261024.txt}`
- **หมายเหตุ `.gitignore`**: `*.log` และ `evidence/**/*.ndjson` ถูก ignore ⇒ ไฟล์ดิบ (`x5-paired.log`, `x6-1hz.log`, `viewer-*.out.log`, `x7-*.ndjson`, `viewer-B1/B2/X6-1hz.ndjson`) **มีอยู่บนเครื่องแต่ไม่อยู่ใน git** — ค่าที่ตัดสิน verdict ถูกคัดลอกไว้ใน `measurements-20261024.md`, `x5-blocks.txt` และ `x6-1hz-metrics.txt` ซึ่ง commit แล้ว
