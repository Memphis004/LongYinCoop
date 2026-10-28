# E4 / E7 / E8 — ต้นทุนเฟรม (A/B overlay) · เซฟปลอดภัย · สเกล (label `S2a4-GO3-20261028-074003`)

## E4 — A/B overlay ON→OFF→ON ที่ศึกนิ่ง WaitPlayerInput (M5-3 = ต้นทุน overlay ล้วน)

meter: `S2FrameMeter` (Time.unscaledDeltaTime 1 ค่า/เฟรม) → marker `framemeter.window` หน้าต่าง 60 s ใน `UserData\LongYinCoop\S2\s2-snapshots.ndjson` · protocol v2: wall-clock + sha256 net.cfg ทุกสลับ · ผู้ใช้นิ่งที่ WaitPlayerInput ตลอด (ยืนยัน 00:47) · **ห้ามคลิก action ระหว่างวัด (ผู้ใช้ไม่กด)**

### จุดสลับ (wall-clock + sha256 net.cfg)

| เวลา | เหตุการณ์ | sha256 net.cfg |
|---|---|---|
| 00:48:28 | **A-start** (overlay=true อยู่แล้ว) · anchor marker `at=28700109` | `5ce166da…a5a5` |
| 00:50:06 | **B-start** (สลับ overlay=false) | `c777cdbb…564c` |
| 00:52:05 | **A2-start** (สลับ overlay=true กลับ) | **`5ce166da…a5a5` (ตรงก่อนแก้เป๊ะ)** |
| ~00:54:30 | เก็บ marker สุดท้าย | — |

### หน้าต่าง framemeter 60 s (attribute ด้วย anchor at↔wall-clock; นับเฉพาะหน้าต่างที่อยู่ในบล็อกเต็ม — ตัด straddle)

| window จบ (wall) | at | n (เฟรม) | p50 | p95 | p99 | mean | บล็อก |
|---|---|---|---|---|---|---|---|
| 00:47:28 | 28640109 | 4629 | 13.888 | 27.784 | 48.615 | 12.969 | (ยกเว้น — ช่วงเปิดฉาก/แอนิเมชัน ก่อน anchor) |
| 00:48:28 | 28700109 | 5622 | 6.951 | 20.839 | 41.672 | 10.673 | (ยกเว้น — straddle ช่วงเปิดฉาก) |
| **00:49:28** | 28760109 | 7949 | 6.950 | 6.951 | 20.839 | **7.549** | **A** |
| 00:50:28 | 28820109 | 7840 | 6.950 | 6.951 | 20.839 | 7.653 | (straddle A/B — ยกเว้น) |
| **00:51:28** | 28880109 | 7563 | 6.950 | 13.895 | 27.776 | **7.929** | **B** |
| 00:52:28 | 28940125 | 7708 | 6.945 | 13.888 | 20.839 | 7.787 | (straddle B/A2 — ยกเว้น) |
| **00:53:28** | 29000125 | 7785 | 6.945 | 6.952 | 20.840 | **7.709** | **A2** |
| **00:54:28** | 29060125 | 7695 | 6.945 | 13.888 | 27.776 | **7.797** | **A2** |

### การตัดสิน (margin มติ 2026-10-24: Δmean ≤1.0 PASS · 1.0–1.5 INCONCLUSIVE · >1.5 FAIL — คงเดิมไม่ปรับ)

- mean(A∪A2) = (7.549 + 7.709 + 7.797)/3 = **7.685** · mean(B) = **7.929**
- **Δmean = mean(B) − mean(A∪A2) = +0.244 ms ⇒ |Δ| ≤ 1.0 = PASS**
- Δ เป็น**ฝั่งบวก** (B/OFF วัดหนักขึ้นกว่า A/ON) ⇒ ต้นทุน overlay วัดไม่ออกภายใน noise ของ session นี้
- p50/p95 ติด bucket 6.945/13.888 (1/144 s — quantization เดิมที่รู้จักจาก X5) — รายงานเชิง observational
- **ข้อจำกัด:** หน้าต่างสะอาด A=1, B=1, A2=2 (เหลือบล็อกละ <80 s) · spread mean ระหว่างหน้าต่างฝั่ง A = 0.248 ms ≈ ตัว Δ เอง ⇒ ความละเอียดจำแนกของการวัดรอบนี้ ~0.25 ms (สอดคล้อง X5-metric-proposal rev2 ที่ประเมิน mean แยกได้ ~0.1–0.5 ms) — ถ้าต้องการความละเอียดสูงกว่าต้องวัดซ้ำด้วยบล็อกยาวขึ้น (ต้องอนุมัติแยก)

## E7 — เซฟปลอดภัย (ตรวจ ณ 00:55 ระหว่าง session, เกมยังรัน)

- `SaveSlot*` ที่ mount (สำเนา): **55 ไฟล์ EXACT เทียบ `savehash.copy.before.txt` — CHANGED = NONE**
- drift ทั้ง session = **`PlayerprefData.dat` ไฟล์เดียว** (volatile — ยกเว้นตามเกณฑ์ E7)
- เซฟจริง aside `Save.real-S2a4-GO3-20261028-074003` = 57 ไฟล์ digest **`df8e5a11beed`** (ไม่ถูกแตะ)
- `leftovers` = `Save.real-S2a4-GO3-20261028-074003` ตัวเดียว (ถูกต้อง)

## E8 — สเกล

- unitCount จริงตลอดการวัด = **7** (ผู้เล่น 3 + ศัตรู 4 · ศึก type=HardFight จาก battle marker) · grid 18×19
- ข้อจำกัดเดิมของ S2a-4: ไม่มี fixture 60 ยูนิต — บันทึกตามจริง (ตรงกับ Q9/§11.1 ที่ยังค้างคำตอบ VPN/เครื่อง guest/60 ยูนิต)

## E1 (สถานะ ณ กลาง session — ปิดสรุปหลังผู้ใช้จบศึก+ปิดเกม)

- ERROR/Exception = 12 (คงที่ — noise MelonLoader type-cache เดิมที่นับเท่ากันใน log build เก่า) · heartbeat `err=0` ตลอด · battle marker `battle:start type=HardFight` (log-only)
