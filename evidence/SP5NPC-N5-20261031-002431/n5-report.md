# N5 รายงานรวม — SP5-NPC T5–T7 บนโลกที่ลด (Light ×0.5) · label `SP5NPC-N5-20261031-002431`

วัดจริง 2026-10-31 · 2 เซสชันเกม (00:36–00:46 · 02:09–02:43) · artifact `c58434b1…` (ตามมติ
ใช้ต่อทั้ง stage หลังพบบั๊ก emission) · baseline `20261031-001811` (`d3f7cb2fcae2…`) ·
**มติผู้ใช้ 2026-10-31 รอบสุดท้าย: T6 = บันทึกตามพยาน (ไม่ถึงเกณฑ์จำนวน) · T7 TempHeros = ทาง (ก)
รับเป็น vanilla + ข้อจำกัด** · **ไม่ปิด stage ไม่ tag ไม่ push ตามมติ**

## T5 — ไม่เด้งกลับ (≥25 วัน ข้าม ChangeMonth ≥2) = **PASS**

| รายการ | ผลวัด | เกณฑ์ | ตัดสิน |
|---|---|---|---|
| เวลาเดิน | task 24 วัน + พัก ~2 เดือน (S1) + ~33 นาทีเล่นต่อ (S2) [USER] · หลักฐาน refill เองยืนยันเวลาผ่านมาก | ≥25 วัน · ≥2 ChangeMonth | ผ่าน (พยาน + ข้อมูล) |
| Heros census ระหว่างเดิน | **45 จุดวัด**: h=632 (สร้างโลก) → 647 (หลังพัก) → 655 → 659 → **668** (ท้าย S2) — **ไม่มีจุดไหนวิ่งกลับสู่ 900** | ไม่วิ่งกลับ 900 / ไม่เกินเพดานเดิม | **ผ่าน** |
| refill | เกมเติมธรรมชาติช้า ๆ (+36 ตลอด S2 · sect 311→334) — ตรง L1 "refill ของเกมเองทำงานตามปกติ" | การเติมธรรมชาติเท่านั้น | ผ่าน |
| sect รายสำนัก | 311 นิ่งช่วงสร้าง (band N4 [280,350] ✓) → เติมธรรมชาติเป็น 334 | คงรูปแบบ N4 + เติมธรรมชาติ | ผ่าน |
| TempHeros | list คง 83 · dict แกว่ง 7–55 (ดู T7) | observational | — |
| error/crash/soft-lock | 0 ทั้งสองเซสชัน (ML `[ERROR]`=0 · probe errors=0) | 0 | ผ่าน |

## T6 — plot/mission ≥20 = **จำนวนไม่ถึงเกณฑ์ (NOT MET) — ไม่ผ่านตามเกณฑ์จำนวน** · คุณภาพ = ผ่าน

| รายการ | ผลวัด | ตัดสิน |
|---|---|---|
| plot เปิด ≥20 | **นับจาก ndjson ไม่ได้** (probe ไม่มี event ตรง · addtemp ไม่มีชื่อ) · พยานผู้ใช้: จำไม่ได้ [USER] | **ไม่ยืนยัน — ไม่ผ่านตามเกณฑ์จำนวน** |
| mission รับ ≥20 | พยานผู้ใช้: **รับเควส ~10 · สำเร็จ ~4-5 · ที่เหลือล้มเหลวเชิงเกมเพลย์** [USER] | **ไม่ถึง ≥20 — NOT MET** (มติ: ห้ามตีความว่าผ่าน) |
| 0 dangling-ID error | ML `[ERROR]`=0 ทั้ง 2 เซสชัน · probe errors=0 · ผู้ใช้รายงานไม่มี dialog พัง · เควสที่ล้มเหลว = ความล้มเหลวเชิงเกมเพลย์ ไม่ใช่ error | **ผ่าน** |
| temp plot hero spawn/ลบ | addtemp 115 ครั้ง · list คง 83 · วน spawn/despawn ปกติ | ผ่าน |
| ศึก plot เริ่ม/จบได้ | ศึก 19 สุ่ม + ศึกตัวละครมีชื่อ ★396/★485 เริ่ม/จบครบ | ผ่าน |

> ตามมติ "เพิ่มได้ถ้า T6 ไม่ถึงเกณฑ์ (agent รายงานแล้วรอสั่ง)" — ผู้ใช้รับผลและไม่สั่งเพิ่มเซสชัน ⇒
> บันทึก T6 จำนวน = NOT MET เป็นข้อจำกัดของ stage นี้ (โลก 33 นาที/2 เซสชัน สั้นกว่าที่เกณฑ์ ≥20
> ต้องการ — เกณฑ์นี้ออกแบบมาสำหรับการเล่นยาว)

## T7 — โครงสร้าง world data = **PASS (ฝั่ง Heros CLEAN 64 จุด) · TempHeros = ข้อจำกัดตามมติ (ก)**

| รายการ | ผลวัด | ตัดสิน |
|---|---|---|
| dict == list (Heros) | **dictH==h ทุก 64 จุด** (632/647/655/659/668) ทั้งสองเซสชัน | **ผ่าน** |
| ไม่มี ID ชน | heroID **dup=0 ทุกจุด** (รวมหลัง refill) | **ผ่าน** |
| leader ต่อสำนัก | leader 30/30 **exists, missing=0 ทุกจุด** (leaderIds คงเดิม) | **ผ่าน** |
| ownHeros ครบ | total 390→413 ตาม refill · **dangling=0 ทุกจุด** | **ผ่าน** |
| dict == list (TempHeros) | S1 (ก่อนเซฟ): 83==83 ทุกจุด ✓ · S2 (หลังโหลด): dictT=7–55 ≠ listT=83 · **dictT==tempMatch เป๊ะทุกจุด** · DUMP: dict มี `[JsonIgnore]` ⇒ เกม rebuild ตอนโหลด (INFER: rebuild เฉพาะ random-enemy temp = vanilla behavior) | **ข้อจำกัดตามมติ (ก)** — รับเป็นพฤติกรรม vanilla พร้อมบันทึก: กลไกยังเป็น INFER ไม่พิสูจน์ · ไม่เห็นผลกระทบเชิงประจักษ์ (0 error · เล่นปกติ) |

## ข้อจำกัด/บั๊กที่บันทึกตามจริง

1. **บั๊ก emission JSON ของบล็อก t7 (artifact `c58434b1`)** — worldcount 92 บรรทัด (54+38) ขาด `}`
   1 ตัว ⇒ ไม่ parse ตรงได้ · repair เติม `}` แบบ deterministic lossless 92/92 (ข้อมูลครบทุก field)
   · source fix 1 ตัวอักษร commit `634c418` — **ยังไม่ build/ไม่ deploy** (deploy ครั้งหน้าต้อง
   build determinism ×2 + provenance ก่อน)
2. กลไก TempHerosDict rebuild = INFER (ไม่มี control บนโลก vanilla — มติ (ก) เลือกไม่วัด)
3. T6 จำนวน = พยาน [USER] ล้วน (จำไม่ได้แม่น) — evidence นับตรงไม่ได้เพราะ probe ไม่มี event
   plot/mission (งานเสนออนาคต: ถ้าต้องการเกณฑ์ ≥20 จริง ต้องเพิ่ม event นับใน probe — อนุมัติแยก)
4. เวลาเดิน ≥25 วัน = พยาน [USER] (ndjson ไม่มี field วันที่)

## คืนสภาพ (ขั้น 8 — ทำแล้ว 2026-10-31 02:55 ยืนยันทุก hash)

| ขั้น | ผล |
|---|---|
| restore เซฟ | **`Save` = ของจริง renamed กลับ ไม่เคยถูกเขียน** · 57 ไฟล์ `d3f7cb2fcae2` **EXACT ทั้งสอง baseline** · `leftovers: none` · โลกทดสอบ N5 เก็บ `coop-test/SP5NPC-N5-20261031-002431/Save.used-20261031-025507` |
| rollback DLL | `Mods\LongYinCoop.dll` = **`6a94abb5…`** · PDB = **`856d3bed…`** (จาก `.prev-*-20261031-002431`) |
| net.cfg | **`12541f34…` EXACT** (ไม่มี npcPolicy · enabled=false) |
| pref | `Sp5T0 = false` · `TimeV2a = false` · `AllowRemoteInput = false` (ตรวจค่า key — cfg hash ใหม่จาก ML cosmetic rewrite ตามปกติ) |
| rollback points | `.prev-6a94abb5-20261031-002431` / `.prev-856d3bed-20261031-002431` คงไว้ตาม convention |

## สถานะ

- **การวัด N5 ปิดแล้ว · stage ยังไม่ปิดเป็นทางการ ไม่มี tag ไม่มี push ตามมติ** — ตัดสินปิด/tag =
  มติผู้ใช้แยก (พร้อมข้อจำกัด: T6 จำนวน NOT MET · TempHeros INFER · emission bug fix ยังไม่ deploy)
- ค้างถัดไป (จาก decisions-pending เดิม): N6/ระดับ Mid/Strong · S3 rev2 · VPN/guest (Q9) · CLOCK-1
  M1/M2 · สาย push (local commits ของ sessions นี้)
