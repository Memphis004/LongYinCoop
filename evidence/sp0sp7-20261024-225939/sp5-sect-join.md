# §5 — การเข้าสำนักผ่าน UI จริง 1 ครั้ง (โลก Free, รอบ 5, 2026-10-25)

## state ก่อน → หลัง (RUNTIME, อ่านจาก Player HeroData + WorldData)

| field | ก่อน | หลัง |
|---|---|---|
| `belongForceID` | -1 (ไม่มีสำนัก) | **3 = 丐帮 (พรรคกระยาจก)** |
| `atAreaID` | -1 (BigMap) | **33** (พื้นที่สำนัก) |
| `forceJobType` / `forceJobID` | -1 / -1 | -1 / -1 (ยังไม่มีตำแหน่ง) |
| `heroForceLv` / `skillForceID` | 0 / -1 | 0 / -1 |
| worldTime | ปี 1 เดือน 1 วัน 1 | ปี 1 เดือน 1 **วัน 2** (เวลาเดินระหว่างขั้นตอน) |

- ชื่อสำนักจาก `WorldData.Forces` (forceID 3) = **丐帮** — ตรงกับที่ผู้ใช้เลือกบนหน้าจอ
- หมายเหตุ: รอบ 2 (Plot) ผู้เล่นถูก fix เข้า 仙霞派 (forceID 25) ตั้งแต่เริ่ม — ส่วน Free ผู้เล่นเลือกเข้าเองผ่าน UI

## call chain ที่พิสูจน์ได้

- **`HeroData.JoinForce` ถูกเรียก 1 ครั้ง** (hook counter) — method ของ HeroData เอง
  ตาม dump: `JoinForce(int _forceID, int _forceLv = -1, int _generation = -1, bool showInfo = True, …)`
  ⇒ ผลลัพธ์เป็น **data-level operation บน HeroData** ไม่ผ่าน PlotController
- ที่ **ไม่** ยิง: `PlotController.JoinForceParty` = 0, `HeroData.JoinForceServant` = 0,
  `GameController.ManageNewHeroJoinForce` = 0 (method นั้นสำหรับ hero ใหม่ของระบบ ไม่ใช่การสมัครของผู้เล่น)
- **ข้อจำกัด**: log รายละเอียดของ join (args/depth/จังหวะ) หลุดเพราะ log v3 เต็ม cap 2,000
  จากพายุ `GenerateHeroData` ตอนสร้างโลก ⇒ chain ฝั่ง "UI → JoinForce" ยังไม่ถูกจับ
  (caller ตัวจริง = UNKNOWN; GUESS: UI panel เรียก `HeroData.JoinForce` ตรง) — hook v4 แบบ ring-buffer
  ติดแล้วเพื่อไม่ให้เกิดซ้ำ

## คำตอบคำถาม SP7 (วิเคราะห์จากหลักฐานที่มี — ยังไม่ทดลองเรียก)

- แยกจาก PlotController/UI ได้ไหม: **น่าจะได้** — `HeroData.JoinForce(forceID, …)` เป็น method ระดับข้อมูล
  ที่รับ forceID ตรง ๆ (RUNTIME: ถูกเรียกจริงเมื่อ UI เข้าสำนัก; DUMP: signature รับ forceID/forceLv/generation)
- พึ่ง Player()/singleton ไหม: ผู้รับคือ instance ของ HeroData ตัวผู้เล่น (ได้จาก `WorldData.Player()` —
  pure getter) — ยังไม่เห็น dependency อื่นจากหลักฐานที่จับได้ (GUESS ส่วนที่เหลือ)
- ข้อควรระวัง: การเข้าสำนักเปลี่ยน state หลายจุด (belongForceID, atAreaID, แถวโครงสร้างฝั่งสำนัก,
  worldTime) — ห้ามตั้ง `belongForceID` ตรง ๆ โดยข้าม JoinForce (validation ของเกมไม่ถูกเรียก)

## baseline ฝั่งศึก ณ จุดนี้ (สำหรับ §F)

`totalFightCount=0`, `totalWinFightCount=0`, `WorldNewsDatas`=0, `WorldEventDatas`=0, worldTime วันที่ 2
