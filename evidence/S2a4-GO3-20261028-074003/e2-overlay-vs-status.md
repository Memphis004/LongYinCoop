# E2 — overlay ตรงกับ spike status (จับคู่ ≥ 20) · G0 พิสูจน์พร้อมกัน

session `S2a4-GO3-20261028-074003` · 2026-10-29 ~00:31 · ศึกแรกของสำเนาเซฟ (Prepare → Fighting → WaitPlayerInput)

## G0 — OnGUI ยิงจริง

- หน้า title (00:0x): จอว่าง = **ถูกต้องตามดีไซน์** (`S2OverlayRenderModel.ShouldRender = overlay && ring>0`; กล่อง "waiting" เป็นโค้ดตาย — ring ว่างไม่วาด; host ส่ง snapshot เฉพาะในศึก → `sent=0` ก่อนเข้าศึก)
- เข้าศึก (00:2x): host `sent=97` (จาก 0) · ผู้ใช้ถ่ายภาพหน้าจอได้ **กล่อง overlay วาดจริง** (status + stateHash + ตารางยูนิต) — seq=1 age≈1804 ms ตอนเปิดฉาก แล้ว seq=211 age=938 ms ตอน WaitPlayerInput
- **G0 = PASS** (ภาพผู้ใช้ 2 ใบ + `sent>0` ใน log)

## การจับคู่ (key เดียวกัน: battleTime=0.524 / turn 0 / WaitPlayerInput — นาฬิกาค้างตามข้อเท็จจริง S1.5)

แหล่งเทียบ 3 ฝั่ง: (1) ภาพหน้าจอ overlay ของผู้ใช้ (2) payload snapshot `seq=211` (`UserData\LongYinCoop\S2\s2-snapshots.ndjson`, battleTime=0.524) (3) `spike status` จาก recorder (`sampler.ndjson`, battleTime=0.5238396, unitCount=7)

| ยูนิต | overlay (จอ) | snapshot seq=211 (hp/maxHp) | spike status (hp/originHp, ตำแหน่ง) | ตรง |
|---|---|---|---|---|
| 0:51 | (7,0) 284/523 | 284.344 / 523.119 (7,0) | 284.344 / 284.344 (7,0) | ✓✓ |
| 0:86 | (14,0) 789/789 | 789.552 / 789.552 (14,0) | 789.552 / 789.552 (14,0) | ✓✓ |
| 0:0* (active) | (17,0) 852/934 | 852.473 / 934.784 (17,0) · playerControl=true | 852.473 / 852.473 (17,0) · active=0:0 | ✓✓ |
| 1:10163 | (6,17) 217/217 | 217.56 / 217.56 (6,17) | 217.56 (6,17) | ✓✓ |
| 1:10152 | (7,17) 228/228 | 228.01 / 228.01 (7,17) | 228.01 (7,17) | ✓✓ |
| 1:10151 | (11,17) 337/337 | 337.68 / 337.68 (11,17) | 337.68 (11,17) | ✓✓ |
| 1:10164 | (13,17) 236/236 | 236.67 / 236.67 (13,17) | 236.67 (13,17) | ✓✓ |

- **จำนวนคู่ที่เทียบและตรง:** ต่อยูนิต 6 ฟิลด์ (id, row, col, hp, alive, team) × 7 ยูนิต = **42 คู่ ตรง 42/42** + active unit ตรงทั้งสองฝั่ง + status line (gaps=0, resync=0/0, divA=0) ⇒ **≥20 คู่ = PASS**
- hp บน overlay = int ตัดส่วน (284.344→284, 852.473→852, 217.56→217 …) ตรงทุกตัว
- **เลขที่สองของ overlay = `maxHp` ของ payload** (ไม่ใช่ originHp ของ spike status — คนละฟิลด์โดยดีไซน์: spike ใช้ hp+originHp, snapshot ใช้ hp+maxHp) — ตรวจตรง payload 7/7
- **UI ของเกมแสดง maxHp เป็น 935 (ปัดขึ้นจาก 934.784) ขณะ overlay แสดง 934 (ปัดลง)** — ต่างกันแค่ convention การปัดจากค่า float เดียวกัน ไม่ใช่ mismatch ข้อมูล
- divA=0 (guest คำนวณซ้ำผ่าน) · gaps=0 · resync 0/0 · age 938 ms < StaleMs 3000

## สถานะประกอบ

- `commandsAccepted=0` ตลอด · recorder 10 Hz errors=0 · battle=1 (ศึกกำลังเดิน) · หน่วยวัด: unitCount=7 (ศึกจริงเล็กกว่าเคส 18 ของ S2a-3 — บันทึกตามจริงสำหรับ E8)
