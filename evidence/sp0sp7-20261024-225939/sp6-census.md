# §6 (E) — Singleton census (DUMP + RUNTIME, รอบ 5 world, 2026-10-25)

## ตาราง census

| Controller / UI | Singleton ต่อโปรเซส | ผูกกับ Player() | Data-only path | หลักฐาน | ความมั่นใจ |
|---|---|---|---|---|---|
| `GameController` | ✅ heap=1 (0x27bacc14a80 รอบ5) | เก็บ `worldData` (Player() อยู่ในนั้น) | สร้างโลก/hero gen เป็น data-level (§4) | DUMP 241 methods (decl) + RUNTIME gc_choose=1 | สูง |
| `BattleController` | ✅ heap=1 · `get_Instance` **hot getter 38k+ ครั้ง** | ฉากศึกที่มีผู้เล่น = `PrepareBattleMap` ×10 overloads | ศึก NPC-vs-NPC บนแผนที่ **ไม่** ผ่าน (0 calls 25 วันเกม) — ดู §7 | DUMP 166 methods (decl) + RUNTIME gc_choose=1 + counter | สูง |
| `PlotController` | ✅ heap=1 (0x27bb77eb900) | จับ event/บทสนทนา; §5: เข้าสำนักไม่ผ่าน (`JoinForceParty`=0) | ยังไม่พิสูจน์ | DUMP **1526 methods (decl)** + RUNTIME gc_choose=1 | ปานกลาง |
| `ChooseController` | ✅ heap=1 | ตัวเลือกในบท (15 methods) | ยังไม่พิสูจน์ | DUMP + RUNTIME gc_choose=1 | ปานกลาง |
| `AuctionController` | ✅ heap=1 | งานประมูล (28 methods) | ยังไม่พิสูจน์ | DUMP + RUNTIME gc_choose=1 | ปานกลาง |
| `CraftUIController` | ✅ heap=1 | UI คราฟต์ (39) — ผูก UI | ยังไม่พิสูจน์ | DUMP + RUNTIME gc_choose=1 | ปานกลาง |
| `CraftPoisonUIController` | ✅ heap=1 | UI คราฟต์ยาพิษ (21) — ผูก UI | ยังไม่พิสูจน์ | DUMP + RUNTIME gc_choose=1 | ปานกลาง |
| `WorldPlotEventController` | ✅ heap=1 | event เรื่องระดับโลก (9) | ยังไม่พิสูจน์ | DUMP + RUNTIME gc_choose=1 | ปานกลาง |
| `BattleAiMenuController` | ✅ heap=1 | เมนู AI ศึก (8) | - | DUMP + RUNTIME gc_choose=1 | สูง |
| `ForceGroupFightMatchChooseController` | ✅ heap=1 | จับคู่ยุทธ์กลุ่ม (11) | - | DUMP + RUNTIME gc_choose=1 | สูง |
| `BattleUnit` | ❌ ไม่ใช่ singleton (54 methods) | unit ต่อตัวในศึก | - | DUMP | สูง |
| non-singleton อื่น: `BattleBackground`, `BattleTeamHeroIconController`, `PlotInteractController`, `StartBattleButtonController`, `BattleAISettingController`, `BattlePrepareSpellButtonController`, `RecruitChooseToggleController` | ❌ | - | - | DUMP | สูง |

หมายเหตุ: "heap=1" = ณ เวลาวัด (กลางเกม รอบ 5) — ไม่ใช่การพิสูจน์ตลอดอายุโปรเซส (แต่ get_Instance
ของ BattleController คืน ptr เดิมตลอด 38k+ calls = ยืนยันต่อเนื่อง)

**หมายเหตุการนับ method (แก้ 2026-10-25 ตามมติผู้ใช้):** ตัวเลขหลักข้างต้น = จำนวน **method declarations**
จาก `tools/sp0_dump_parser.py` (รวม `.ctor`/property accessor/explicit-interface · nested type นับแยก ·
ไม่นับ `/* GenericInstMethod */` block) · **ตัวเลขรอบแรกเดิม** 244/186/1530 (และวงเล็บ 16/33/41/9/56)
นับรวมบรรทัด attribute ที่มี `// RVA:` — สอดคล้องเป๊ะ: GameController 241+3=244 · BattleController
166+20=186 · **PlotController 1526+5=1531 ≠ 1530 ต่าง 1 ยังอธิบายไม่ได้** (วิธีนับเดิมไม่ถูกบันทึกไว้)
— เก็บทั้งสองชุดเป็นประวัติ · หลักฐานเต็ม `evidence/SP0-20261025-0211/singleton-census.md` §1.4

## สรุปผลต่อการออกแบบคิว/worker (Puppet Client)

- **flow ที่ทำพร้อมกันใน 1 โปรเซสไม่ได้ (จริง ๆ คือ state กลางร่วมกัน)**: Battle (BattleController
  มี state ฉากเดียว — `get_Instance` เดียว), Plot (PlotController god-class 1526 methods), 
  เพราะ singleton เดียวต่อโปรเซสทั้งหมด — ทุก flow แชร์ instance เดียวกัน
- **flow ที่อาจแยกผลจาก UI ได้**: การเข้าสำนัก (`HeroData.JoinForce` — data-level, §5),
  การสร้าง hero (`GenerateHeroData` — อยู่ที่ GameController, §4) — ยังต้องยืนยัน caller ฝั่ง UI
- **ต้องศึกษาต่อก่อนเลือกคิว/worker**: PlotController (1526 methods — ครอบคลุมอะไรบ้าง),
  จุด save/load เกม (ยังหา method ไม่เจอ), Choose/Craft/Auction ยังไม่ผ่านการ trace จริง

## ข้อเท็จจริงเพิ่มเติมที่เกี่ยวข้อง

- `WorldData` เป็น data container (ไม่ใช่ MonoBehaviour singleton แบบ controller) — อยู่ใน
  `GameController.worldData`; รายการ event ระดับโลก: `WorldEventDatas`(3 ณ วันที่ 25: ประลองยุทธ์/
  ประลองสำนัก/ประมูล), `BigMapRandomEventDatas`(62), `AreaMapRandomEventDatas`(60 — รวม 
  门派恩怨/武林争斗 = ความขัดแย้งสำนัก)
- method save ของเกมยังไม่พบตำแหน่ง (`GameController.Save` ไม่มีจริง — hook หาไม่เจอ; 
  hash-watch จับการเขียนไฟล์แทนได้)
