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
| **X5** guest หน่วงแล้ว host frame time ไม่เปลี่ยน — มติ 2026-10-24: ตัดสินด้วย **Δmean** (margin 1.0/band ±0.5) · p99/p95/max observational | **PASS** | re-measure `S2a3x5r-20261024-0248`: **Δmean = −0.023 ms** (A 8.173 / B 8.150), paired −0.059/−0.058 · รอบแรกตามเกณฑ์ p99 เดิม = FAIL (+4.624 ms) เก็บเป็นประวัติใน §X5 (p99 จำแนกไม่ได้: A2 ที่ S2 ปิด = 34.720 เท่ากับ B) |
| **X6** p99(sample+serialize) ≤ 4 ms | **PASS** | **default 1 Hz: 0.1337 ms ที่ n=1413** · 5 Hz: 0.1258 ms ที่ n=1080 ⇒ ไม่ใช่ผลของ rate |
| **X7** auth/bind | **PASS** | bind `127.0.0.1:47800` เท่านั้น (netstat + host log `loopback only`) · secret ผิด → DENY `hmac mismatch` ≤0.3 s (≤2 s) · fieldHash ผิด → DENY `fieldHash prefix mismatch` ≤0.3 s (≤1 s) |
| **X8** guest อ่านอย่างเดียว | **PASS** | `selftest` gained=3 commandsAccepted=0 auditEqual=true exit6=PASS (blockIndex 1–6) · `selftest.tcp` handshakeOk forbiddenGained=3 exit8=PASS · host log `commandsAccepted=0` ทุกบรรทัด · grep call site เขียน state = 0 |
| **X9** ไม่แตะเซฟ | **PASS** | `SaveSlot*` เขียน 0 (real folder `changed=0` ตอน verify) · restore แล้ว **57 ไฟล์ exact** ทุก baseline (10 ไฟล์เทียบ) · rehash อิสระ changed/missing/added = 0 · `leftovers: none` · `net.cfg` `12541f34c633` (เหมือนก่อนเริ่ม) · pref `269b571c…98d1` |

**สรุป (ปิด stage 2026-10-24): 7 PASS · 2 PASS-with-limitation (X1, X2) · 0 FAIL · 0 NOT-RUN** — X5 = PASS ตามมติเปลี่ยนตัวชี้วัด (Δmean, margin 1.0 เดิม; p99/p95/max เป็นข้อมูลประกอบ) · limitation ของ X1/X2 อยู่ในตาราง · ข้อจำกัดของ BP-1 ดู §X5

## X5 — รอบแรก (เกณฑ์ p99 เดิม) = FAIL จากตัวชี้วัด → มติเปลี่ยนตัวชี้วัด → re-measure = PASS

### รอบแรก (01:41–01:47 ตามเกณฑ์ p99 เดิม) — ค่าดิบคงไว้เป็นประวัติ

| บล็อก | p99 | mean | max |
|---|---|---|---|
| A0 (S2 off) | 27.783 | 8.088 | 111.107 |
| B1 (guest stall 60 s) | 34.719 | 8.083 | 83.334 |
| A1 (S2 off) | 27.783 | 8.104 | 118.046 |
| B2 (guest stall 60 s) | 34.720 | 8.054 | 90.278 |
| A2 (S2 off) | **34.720** | 8.119 | 104.168 |

- ตามตัวอักษรเกณฑ์ p99 เดิม: `delta = +4.624 ms` > 1.5 ⇒ **FAIL** — ไม่มีการขยับเกณฑ์
- แต่ **p99 จำแนกไม่ได้**: A2 (S2 **ปิด**) = 34.720 เท่ากับ B เป๊ะ · `p50 = 6.945` ทุกหน้าต่าง = 1/144 s ⇒ frame time ถูก quantize ทีละ 6.945 ms (7 เท่าของ margin) — percentile ทุกตัวตกถังและสลับได้เองแม้ S2 ปิด · การ stall เกิดจริง (guest `latencyP95 ≈ 57 s`) แต่ `TcpTransport.Send` = TryAdd ลง outbox 256 + writer thread ⇒ main thread ไม่แตะ socket

### มติ 2026-10-24 + re-measure (label `S2a3x5r-20261024-0248`, 02:50–03:05) — **PASS**

- มติผู้ใช้ (02:30): ตัดสินด้วย **mean frame time เดี่ยว** margin/band คงเดิม (≤1.0 PASS · 1.0–1.5 INCONCLUSIVE · >1.5 FAIL) · p99/p95/max = observational · เหตุผลเชิงวัดครบใน `X5-metric-proposal.md` (rev2 — noise floor จาก 8 หน้าต่าง S2-off จริง: p99 span 13.88 ms = 2 ถัง, sd 4.45 · mean sd 0.034 ใน session เดียว ⇒ percentile ใช้ตัดสินไม่ได้ตั้งแต่ต้น, mean แยกได้ถึง ~0.1–0.5 ms)
- **ผล: A {8.183, 8.234, 8.102} vs B(stall 1 Hz) {8.124, 8.176} ⇒ Δmean = −0.023 ms ⇒ PASS** · paired B1−A0 = −0.059, B2−A1 = −0.058 · **A1 (S2 ปิด) ได้ p99 = 34.720 สูงกว่า B ทั้งคู่** — ยืนยันซ้ำเชิงประจักษ์ว่า p99 ไม่จำแนก · p95 เท่ากัน 13.894–13.895 · max ฝั่ง B ต่ำกว่า A (90.3 vs 97.2–131.9) · n = 7,286–7,405/หน้าต่าง, coverage ≈ 60,000 ms · host log ERROR/Exception = 0, `commandsAccepted=0`
- **ข้อจำกัด BP-1 (ตามจริง):** พยายามสร้าง backpressure จริง 2 ครั้ง — (1) พบว่า mod อ่าน `heartbeatMs` **ตอน activate listener เท่านั้น** (`enabled` ถูก poll สด) ครั้งแรกจึงไม่ apply (คง 0.9/s) (2) ครั้งที่สอง (BP1b) ตั้ง hb=100 ก่อน activate ได้ **9.3 msg/s** จริง + guest `--rcvbuf 1024` stall 60 s แต่ **`queuedDropped = 0`** — kernel send buffer กลืน ~195 KB หมด, outbox 256 ไม่เต็ม ⇒ **drop path ยังไม่เคยถูกกระตุ้น** (จะกระตุ้นต้องแก้ mod เช่น `SO_SNDBUF` เล็ก หรือเพิ่มวอลุ่มจริง) — **ไม่กระทบ X5** เพราะ X5 ตัดสินที่ Δmean ซึ่งวัดครบ และกลไกที่กังวลไม่มีอยู่จริงทั้งเชิงโค้ดและเชิงประจักษ์ · guest BP1b: 668 snapshots, gaps 0, Div-A 0/668
- **Deviation (ผู้ใช้อนุมัติ):** วัดบนเซฟจริง (เกมเปิดก่อน split ตามลำดับ) — ชดเชยด้วย save-watch sha256 ต้นไม้ Save 57 ไฟล์ ก่อน/หลัง = **changed 0 ทั้งคู่** · หลักฐาน `evidence/S2a3x5r-20261024-0248/`

## เซฟหลังปิด stage — divergence จากผู้ใช้ (ไม่ใช่จากการทดสอบ)

- หลัง restore 02:09 (57 ไฟล์ `4226730f66ff` EXACT) ผู้ใช้เปิดเกมเอง 02:14:33 และ**เซฟเกมเองบน SaveSlot8 ตอน 02:15:47** ⇒ เซฟปัจจุบันต่าง baseline **5 ไฟล์**: `SaveSlot8/{Hero,Info,Save,TempHero}` + `PlayerprefData.dat` — **การทดสอบ S2a-3 ทั้งหมดไม่ได้เขียนเซฟแม้ 1 ไฟล์** (X9 วัดตอน 02:09 ก่อนเหตุการณ์นี้ verdict ยังคงใช้ได้)
- **เกณฑ์ตัดสิน X9 ยังอ้าง baseline `4226730f66ff` ตามเดิม — ไม่มีการเปลี่ยน baseline**
- เซฟปัจจุบันของผู้ใช้**รักษาไว้ ห้าม restore ทับ** · เสนอ: สร้าง **baseline ใหม่สำหรับ stage ถัดไป** (rebaseline volatile-only ตามโปรโตคอลเดิม + บันทึก divergence ของ SaveSlot8) — **ต้องขออนุมัติก่อน**

## Rollback ที่พร้อมใช้

- `Mods\LongYinCoop.dll.prev-3c9584a4` (build X6 marker) · `.prev-c51debef` (build C3) · `.off` (`517758147f59…`, S1.5e-W7)
- `net.cfg` = `enabled=false`, sha `12541f34c633` · `MelonPreferences.cfg` sha `269b571c…98d1`
- เซฟ: `Save` = โฟลเดอร์จริงที่ rename กลับ (ไม่ถูกเขียนทับ) · สำเนาทดสอบ `coop-test/S2a3x5-20261024-0132/Save.used-20261024-020908`
- git: tag `stage-S2a-2` @ `14757e3` (mod repo)

## หลักฐาน

- `measurements-20261024.md` (ตาราง X1–X8) · `x5-paired.log`, `x5-blocks.txt` · `x6-1hz.log`, `x6-1hz-metrics.txt` · `viewer-X6-1hz.out.log` · `x7-wrongsecret.*`, `x7-wronghash3.*`, `viewer-B1/B2.out.log` · `x8-nowrite-grep.txt` · `x9-verify-20261024.txt`, `x9-restore-20261024.txt`, `x9-rehash.txt`
- **Re-measure X5 + BP-1 (2026-10-24 03:0x):** `evidence/S2a3x5r-20261024-0248/` — `measurements-20261024.md` (ผล + มติ + deviation), `blocks.txt` (บรรทัดหน้าต่างที่ตัดสิน), `savehash-watch.txt`, `run.log`, `run-bp1b.log`
- รอบก่อนหน้าใน stage: `evidence/S2a3-20261022-143608/{x4-rerun-20261024.md,x6-run-20261024.md,c8c-verify-20261024.txt,c8c-restore-20261024.txt}`
- **หมายเหตุ `.gitignore`**: `*.log` และ `evidence/**/*.ndjson` ถูก ignore ⇒ ไฟล์ดิบ (`x5-paired.log`, `x6-1hz.log`, `viewer-*.out.log`, `x7-*.ndjson`, `viewer-B1/B2/X6-1hz.ndjson`, `S2a3x5r-20261024-0248/run*.log`) **มีอยู่บนเครื่องแต่ไม่อยู่ใน git** — ค่าที่ตัดสิน verdict ถูกคัดลอกไว้ใน `measurements-20261024.md`, `x5-blocks.txt` และ `x6-1hz-metrics.txt` ซึ่ง commit แล้ว
