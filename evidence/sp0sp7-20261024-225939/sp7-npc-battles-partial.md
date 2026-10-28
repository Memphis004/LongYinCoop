# §7 (F) — NPC vs NPC battle observational trace (รอบ 5, วันเกม 1→25+, 2026-10-25)

## หน้าต่างการทดสอบและ coverage

- hook ครอบคลุมพื้นผิว battle scene ทั้งหมดที่รู้จัก: `BattleController.PrepareBattleMap` ×10 overloads,
  `StartBattle`, `RunBattle`, `BattleEnd`, `BattleRealEnd`, `CheckBattleEnd`, `SureSkipBattle`,
  `SureGiveUpBattle`, `BattleTimeScaleButtonClicked` (v5, log+counter) — ตั้งแต่กลางวันที่ 2 ถึงหลังวันที่ 25
- เหตุการณ์ที่ผู้ใช้พบเห็นจริงบนแผนที่: marker ดาบไขว้ (NPC vs NPC) **2 ครั้ง**
  (ครั้งแรกเข้าไม่ทัน, ครั้งที่สองถูกปฏิเสธการเข้าร่วมเพราะชื่อเสียง 60 < 234 — dialog ของเกมระบุเงื่อนไข)

## ผล (RUNTIME)

| จุดวัด | ค่า |
|---|---|
| `BattleController.PrepareBattleMap` (×10) | **0 ครั้ง** |
| `StartBattle` / `RunBattle` / `BattleEnd` / `BattleRealEnd` | **0 ครั้ง** |
| `WorldData.totalFightCount` / `totalWinFightCount` | **0 / 0** (วันที่ 25) |
| `WorldNewsDatas` | 0 |
| `WorldEventDatas` | 0 → 3 (ประลองยุทธ์ / ประลองสำนัก / ประมูล — ไม่ใช่ศึก) |
| `BigMapRandomEventDatas` | 62 items (สมบัติ/พ่อค่า/เหตุการณ์แผนที่) |
| `AreaMapRandomEventDatas` | 60 items (รวม 门派恩怨 / 武林争斗 / 地痞闹事 = ความขัดแย้ง) |
| `BattleController.get_Instance` | hot getter, 37,997 → +18,436 ต่อ ~15 นาที (นับต่อเนื่อง, ptr เดิม) |

## ข้อสรุปที่กล้าพูดได้

- **ศึก NPC-vs-NPC บนแผนที่ (marker ดาบไขว้) ไม่ผ่าน BattleController ฉากจริง** ในหน้าต่างการทดลองนี้ —
  coverage ครอบจุด entry ของ battle scene ครบทุก overload ที่ dump มี ตลอด 25+ วันเกมกับ 2 เหตุการณ์จริง
  ⇒ ข้อสรุปที่แรงกว่า "ไม่พบในการทดลองนี้": การลงท้ายของความขัดแย้ง NPC เป็น **data-level/world-sim path**
  (ยังไม่รู้ method ภายใน — อยู่นอกชุด hook นี้)
- `totalFightCount` ยังไม่ขยับ ⇒ คงเป็น counter เฉพาะศึกที่ "จบแบบนับได้" (น่าจะรวมผู้เล่นหรือเมื่อ resolve) — UNKNOWN
- การเข้าร่วมศึกของผู้เล่นถูก gate ด้วยชื่อเสียง (RUNTIME: dialog ต้องการ 234, ผู้เล่นมี 60) —
  ถ้าชื่อเสียงพอ ผู้เล่นเข้าฉากศึกได้ (น่าจะผ่าน `PrepareBattleMap` — ยังไม่ได้วัด)

## สิ่งที่ยังไม่รู้

- method ที่ resolve ความขัดแย้ง NPC จริง ๆ (data path) — ต้อง hook เพิ่มในกลุ่ม WorldEvent/RandomEvent
- ศึกฉากเมื่อผู้เล่นเข้าร่วม (ชื่อเสียงพอ) ผ่าน BattleController หรือไม่ — รอผู้เล่นมีชื่อเสียงพอจึงวัดได้
- ข้อควรระวัง: ห้ามเรียก PrepareBattleMap เองเพื่อบังคับศึก (ผิดข้อตกลง §7)
