# X5 re-measure — label `S2a3x5r-20261024-0248` (วัด 02:50–03:05)

มติที่ใช้ตัดสิน (2026-10-24): **mean frame time เดี่ยว** margin 1.0 / band ±0.5 (`S2FrameRule` เดิม เปลี่ยนแค่สถิติ) + BP-1 ควบคู่ — รายละเอียด `LongYinCoop/X5-metric-proposal.md` (rev2)
Build: `Mods\LongYinCoop.dll` sha `5d7b7cc5…5953` (เกม PID 38772 เปิด 02:36 โดยผู้ใช้ · ผู้ใช้อยู่ state `WaitPlayerInput` ตลอด) · fieldHash prefix `5424e4cd7dfad630` · protoVer=2

## Deviation ที่ผู้ใช้อนุมัติ (บันทึกตามจริง)

- **วัดบนเซฟจริง** (ไม่มี save-split — เกมเปิดก่อน split ตามลำดับปกติ) — ชดเชยด้วย save-watch (sha256 ต้นไม้ Save 57 ไฟล์ ก่อน/หลัง) + ผู้ใช้งดเซฟ/งดออกระหว่างวัด
- **เซฟเปลี่ยนจาก baseline `4226730f66ff` อยู่แล้ว 5 ไฟล์ก่อนการวัด** (`SaveSlot8/{Hero,Info,Save,TempHero}` + `PlayerprefData.dat`) — เขียน 02:15:47 โดย session เปิดเกม 02:14:33 = **การเล่น/เซฟของผู้ใช้เอง** · การวัดทั้งหมด (02:50–03:05) **ไม่เขียนเซฟเพิ่มแม้ 1 ไฟล์** (save-watch start `changed=0` → end `changed=0`; diff ปลายทางเทียบ baseline = 5 ไฟล์เดิมพอดี)
- สถานะปิด: `net.cfg` = `12541f34c633` (enabled=false, heartbeatMs=1000) · recorder หยุดแล้ว · ไม่มี orphan process

## X5 — paired A/B (ตัดสินด้วย Δmean)

A-B-A-B-A, หน้าต่าง 60 s (`framemeter.window`, coverage 59,990–60,011 ms), B = guest ต่อ socket ค้างแต่หยุดอ่าน 60 s (`--stall 60`) ที่ 1 Hz:

| บล็อก | mean (ms) | p99 | p95 | max | n |
|---|---|---|---|---|---|
| A0 (S2 off) | 8.183 | 27.783 | 13.895 | 131.939 | 7,332 |
| B1 (stall) | 8.124 | 27.783 | 13.895 | 90.271 | 7,386 |
| A1 (S2 off) | 8.234 | **34.720** | 13.895 | 97.223 | 7,286 |
| B2 (stall) | 8.176 | 27.784 | 13.895 | 90.283 | 7,340 |
| A2 (S2 off) | 8.102 | 27.781 | 13.894 | 118.050 | 7,405 |

- **Δmean = mean(B) − mean(A) = 8.150 − 8.173 = −0.023 ms ⇒ PASS** (เกณฑ์ ≤ 1.0; ไม่เข้า band 1.0–1.5)
- Paired: B1−A0 = **−0.059 ms**, B2−A1 = **−0.058 ms** (ทั้งคู่ติดลบ — B เร็วกว่าเพื่อนบ้าน) · A-spread 0.132 ms, B-spread 0.052 ms ⇒ delta อยู่ใน noise ของ A เอง
- **p99 (observational เท่านั้น)**: A1 (S2 **ปิด**) ให้ 34.720 สูงกว่า B ทั้งคู่ — ซ้ำเชิงยืนยันว่า p99 ตกถังตาม quantization 1/144 s ไม่จำแนก S2 (ตรงกับการวิเคราะห์ใน rev2 §1)
- p95 เท่ากันทั้งสอง phase (13.894–13.895) · max ฝั่ง B ต่ำกว่า A (90.3 vs 97.2–131.9)
- host log ระหว่างวัด: ERROR/Exception จาก mod = 0 · `commandsAccepted=0` ทุกบรรทัด
- หลักฐานดิบ: `blocks.txt`, `run.log` (flip shas: on=`c7d1b1d1c6a8`, off=`12541f34c633` — ตรงรอบ 0132)

## BP-1 — backpressure จริง (observational, ไม่ใช่ตัวตัดสิน)

**ผลลัพธ์ใหญ่: ที่ ≤10 Hz ยังไม่มีทางให้ outbox เต็ม — `queuedDropped = 0` ทุก attempt** ⇒ main thread ไม่เคยถูกหน่วงจาก guest ยืนยันเชิงประจักษ์ (ตรงกับการอ่านโค้ด: `TcpTransport.Send` = TryAdd + writer thread)

| attempt | ลำดับ | heartbeats (วัดจริง) | queuedDropped | guest |
|---|---|---|---|---|
| BP1 (blockIndex=3) | hb 100 **หลัง** activate | 124/141 s ≈ **0.9/s — hb ไม่ถูก apply** | **0** | 73 snapshots, gaps 0, Div-A 0/73 |
| BP1b (blockIndex=4) | hb 100 **ก่อน** activate | 1,193/129 s ≈ **9.3/s — engage ✓** | **0** | 668 snapshots, gaps 0, Div-A 0/668 (drain หลัง stall ครบ ทุกข้อความ) |

- **ข้อเท็จจริงใหม่ (RUNTIME):** mod อ่าน `heartbeatMs` **ตอนเปิด listener เท่านั้น** (`enabled` ถูก poll แบบสด) — การแก้ hb จึงต้องทำตอน S2 ปิดก่อน
- ที่ 9.3 msg/s (~3.2 KB/s) stall 60 s สะสม ~195 KB ⇒ **kernel send buffer กลืนหมด** writer ไม่ block, outbox 256 ไม่เต็ม — จะพิสูจน์ drop path ต่อต้องแก้ mod (host `SO_SNDBUF` เล็ก) หรือเพิ่มวอลุ่มจริง (ศึกเดิน + `sendHz=30`) — **ยังไม่จำเป็นต่อ X5** เพราะ mechanism ที่ X5 กังวลไม่มีอยู่จริงใน build นี้
- หน้าต่างระหว่าง BP-1b (observational): mean 7.59–7.62, p95 6.951, p99 20.840, max 83–97 — เกมเร็วขึ้นเองช่วงนั้น (โหลดเครื่องเปลี่ยน) ไม่มี hitch ผิดปกติ (BP1 window แรกมี max 472 หนึ่งครั้ง — transient ตอนตัดการเชื่อมต่อ)
- หมายเหตุ: marker `rcvbuf` ของ client ไม่ถูกเขียนลงไฟล์ (emit ก่อน out file พร้อม) — หลักฐานเหลือ args `--rcvbuf 1024` ใน run log + พฤติกรรม traffic · ข้อผิดพลาด wrapper เล็กน้อย: BP-1b ABORT ตอนท้ายเพราะรอ client exit แค่ 30 s (client ยังไม่ครบ 140 s) — `safe_off` ทำงานถูกต้อง ข้อมูล client ครบ (summary 668 snapshots)

## สรุป

- **X5 = PASS** ตามมติตัวชี้วัด (Δmean −0.023 ms ≤ 1.0) — S2 ไม่ทำให้ frame time ของ host เปลี่ยนแม้ guest หน่วง
- p99/p95/max รายงาน observational เท่านั้น (quantization 1/144 s — rev2 §1)
- BP-1: stressor engage ได้ (10 Hz) แต่ drop path ยังไม่ถูกกระตุ้น — ปิดคำถาม X5 ด้วย "กลไกไม่มีอยู่จริง" ทั้งเชิงโค้ดและเชิงประจักษ์
- ค้างตัดสิน (ผู้ใช้): baseline `4226730f66ff` ล้าสมัยจากการเล่นของผู้ใช้เอง (SaveSlot8 02:15) — rebaseline หรือคง canonical + บันทึก divergence · tag `stage-S2a-3` พร้อมทำเมื่อ X5 มีมติแล้ว
