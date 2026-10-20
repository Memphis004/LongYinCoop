# S1.5-c run 2 — PREP (2026-10-20 21:02)

## 1. สถานะเซฟตอนเริ่มรอบนี้ (RUNTIME, 21:02)

| รายการ | ค่า |
|---|---|
| mounted `Save/` | สำเนา split `S1.5c-originhp-20261020-172346` — 58 path (57 ไฟล์ + `SaveSlot8/steam_autocloud.vdf` ที่ Steam เพิ่งสร้างตอนเปิดเกม 20:56) |
| เทียบ copy baseline | ตรงทุกไฟล์ **ยกเว้น `PlayerprefData.dat`** (volatile ตาม S1 §2) |
| sentinel | `.hidden-SaveSlot8/` ยังอยู่ครบ 5 ไฟล์ (Hero, Info, Save, TempHero, steam_autocloud.vdf) |
| `Save.real-S1.5c-originhp-20261020-172346` | **57/57 digest `2b0548fe464b` = baseline EXACT** |
| `Save.used-*` / leftover อื่น | none |

**หมายเหตุ**: `.hidden-SaveSlot8` มี Windows hidden attribute — `ls -a` และ `Path.glob(".hidden*")` มองไม่เห็น ต้องอ่านผ่าน `hash_tree()` (สรุป: อย่าใช้ glob ตรวจ sentinel)

## 2. ช่วงเวลาที่ผู้ใช้ทำ פעล แต่ **ไม่มี sampler อัด** (ต้องบันทึกว่า NOT-MEASURED)

| เวลา | เหตุการณ์ (จากผู้ใช้) | หลักฐานในเครื่อง |
|---|---|---|
| 20:56 | เข้าเกม | MelonLoader log `26-10-20_20-56-2.log` เริ่ม 20:56:02 |
| 20:57:16 | เข้า battle HardFight | log: `battle marker battle:start type=HardFight state=Prepare` — **ตรง** |
| 20:58 | โจมตีโดนศัตรู + กดหยุด 5 รอบ | ไม่มี sampler → ไม่มีข้อมูล |
| 20:59 | กด x5 แล้วกลับ x1 | ไม่มี sampler → ไม่มีข้อมูล |

สาเหตุ: sampler ยังไม่ถูกเปิด (รอบก่อนหน้าผมขอให้ผู้ใช้พิมพ์ "เปิดแล้ว" ก่อน แต่ผู้ใช้ส่ง timeline มาหลังทำครบแล้ว)

**ผลต่อเกณฑ์**: exit 5 ยังนับได้ 5/10 รอบ (จากรอบ S1.5c เดิม) · `spike attack` = NOT-RUN · J7 ฝั่งเร่ง = NOT-RUN

## 3. Snapshot สด 21:00 — ข้อค้นพบที่ล้มสมมติฐานเดิม (RUNTIME, `logs/live-status-2100.json`)

ศึกยังอยู่: `battleState=Fighting`, `nowActiveState=WaitPlayerInput`, `turnIndex=20`, 9 units, `timeScale=1`, `deltaTime=0.0138943`

| unit | `hp` | `originHp` | ตีความ |
|---|---|---|---|
| `0:0` | 452 | 505.551 | hp < originHp |
| `1:10102` | 187.45 | 589.26 | hp < originHp |
| `0:9` / `0:4` / `0:86` / `1:10105` / `1:10066` / `1:10107` / `1:10067` | เท่ากัน | เท่ากัน | ยังไม่โดนดาเมจ |

→ **`BattleUnit.originHp` = HP ตั้งต้น (max) ไม่ใช่ HP ปัจจุบัน** จึงใช้เป็นตัววัดดาเมจไม่ได้
→ ตัววัดดาเมจคือฟิลด์ `hp` ที่มีอยู่แล้วใน `spike status` (`units[].hp`)
→ ต้องแก้ถ้อยคำใน commit `14813fa` / S1.5-plan ที่เรียก `originHp` ว่า "candidate damage field" — ยังไม่แก้เอกสารในรอบนี้ (รออนุมัติ)

ยังไม่ได้เทียบกับตัวเลขบนจอ (กติกา AGENTS §5) — ต้องมี screenshot ยืนยัน

## 4. Sampler รอบนี้

- tool ใหม่: `tools/s15d_sample.py` — อัด `spike status` ทั้งก้อน (รวม `units[]`) ทุก 100 ms
- read-only: ส่งเฉพาะ `{"cmd":"spike","sub":"status"}` (grep แล้วไม่มีคำสั่งเปลี่ยน state)
- smoke 3 วิ = 30 samples, errors 0
- run เต็ม: `logs/spike-full-2101.ndjson` เริ่ม 21:01:45 ยาว 1200 s · `logs/sampler-2101.log` มี progress ทุก 10 s
- วัด rate ได้: 101 samples / 10 s ≈ 10.1 Hz, errors 0

## 5. สิ่งที่ต้องวัดต่อ (รอผู้ใช้ทำในศึกที่ยังเปิดอยู่)

1. โจมตี 1 ครั้งให้โดนศัตรู → ดู `hp` ลดลงจริง (เทียบ originHp ที่ต้องคงที่)
2. pause → resume 5 รอบ → ครบ 10/10 (นับรวม 5 รอบเดิมที่วัดได้ใน S1.5c)
3. กด x5 ~10 วิ แล้วกลับ x1 → ดู `timeScale` / `deltaTime` / `unscaledDeltaTime` / อัตราเดินของ `battleTime`
4. (ถ้าเปิดดูได้) screenshot ตัวเลข HP บนการ์ดยูนิต เพื่อ cross-check กับ `hp`
