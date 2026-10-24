# รอบสำเร็จ: FinishStealSkill (2026-10-23 ~23:20–23:30)

สถานะ: RUNTIME · hook `tools/frida_exit_probe.py` (19 method) + `tools/frida_event_probe.py` (tile/plot)
หลักฐานดิบ: `exit-probe.ndjson`, `event-probe3.ndjson` (โฟลเดอร์เดียวกัน)

## เส้นทางผู้เล่น (tile จาก hook)
| ก้าว | tile | ev | method | lp | ของ |
|---|---|---|---|---|---|
| เริ่ม | – | – | `PlotController.StartExploreFuc` | 55 (เต็ม) | 71 ชิ้น |
| 7 | `(3,1)` | 19 | `ChooseDigTreasure` → `DigTreasureChoosen` | 48 → **44** | +1 (72) |
| 16 | `(7,2)` | 7 | `ManageTileEvent` | 39 | +1 (73) |
| 24 | `(7,6)` | 12 | `ManageTileEvent` | 31 | – |
| 31 | `(9,9)` | 7 | `ManageTileEvent` | 24 | +1 (74) |
| 32 | `(10,9)` | 12 | `ManageTileEvent` | 23 | – |
| 33 (จุดจบ) | – | – | `ChooseStealSkill` | 22 | – |

- **ต้นทุนขุดสมบัติ = 4 พลังงาน** (`DigTreasureChoosen` 48 → 44) — ตรงกับ ev20 ที่วัดไว้ (4) ไม่ใช่ 5 แบบ ev23
- ขั้นบันไดพลังงาน: 55 → 22 = 33 ใช้ไปทั้งหมด (เดิน 33 ก้าว + ค่าขุด 4 − พลังงานที่ได้จากอีเวนต์บางชนิด)

## จุดจบ + plot 2 ตัวเลือก
1. `PlotController.FinishStealSkill()` — ถูกเรียกทันทีที่เหยียบช่องจบ (`finishFuc` ของแผนที่นี้)
2. `PlotController.ChooseStealSkill()` — plot 3 ตัวเลือก (`避人耳目` ติดเงื่อนไข 轻功 80 / `速战速决` / `大张旗鼓`) ผู้เล่นเลือกข้อ 1
3. `PlotController.FinishExploreMap(...)` → `ExploreController.FinishExploreMap(...)` → `HideExploreMap()` → `ResetExploreMap()` — **ปิด explore UI**
4. `PlotController.StealSkillSuccess()` — ข้อความสรุป (เกิดหลัง explore UI ถูกซ่อนแล้ว ⇒ ผู้เล่นเห็นเป็น plot บนหน้า Area)
5. `PlotController.StealSkillSuccessChoose()` (t = 876,903) — plot **2 ตัวเลือก** (คัมภีร์ลับ / ตัวเลือกจีนอีกอัน) ผู้เล่นเลือกข้อ 1
6. `PlotController.StealSkillSuccessChoosen()` (t = 890,716) — **ของเข้ากระเป๋า 74 → 75 ชิ้น**

## สถานะหลังจบ (อ่านสด)
- `leftPower` 22 · `ExploreController.inited` true · `exploreUIPanel` **inactive**
- `AreaController.areaData` = **41 `武当派`** · `areaUIBelow` **active** ⇒ กลับหน้า Area ของสำนัก
- กระเป๋า 75 ชิ้น · น้ำหนัก 310
- ของที่ได้ในรอบนี้ (ท้ายลิสต์): `普通矿料` (2), `田七鲨胆丹` (2), **`飞针要诀` (1)** = คัมภีร์ที่เลือกจาก 2 ตัวเลือก

## ต่างจากการ "กดออกกลางคัน" (วัดก่อนหน้า)
| | กดออก | จบครบ |
|---|---|---|
| `ExploreFail` | **เรียก** | ไม่เรียก |
| `FinishExploreMap(arg)` | arg = **0** | arg ไม่ใช่ 0 (ค่าที่จับได้เป็น pointer/garbage — ยังไม่ยืนยันความหมาย) |
| `finishFuc` (`FinishStealSkill`) | ไม่เรียก | **เรียก** |
| ปลายทาง | Area 41 | Area 41 |

## ยังไม่รู้
- ความหมายจริงของ arg ใน `FinishExploreMap(int)` (ค่า 0 = ล้มเหลวชัด, ค่าอื่นยังไม่ยืนยัน)
- `StealSkillSuccessChoose`/`Choosen` มีตัวเลือกอะไรบ้างและรางวัลต่างกันอย่างไร (รอบนี้เห็นแค่ผลลัพธ์ = `飞针要诀`)
- เงื่อนไข `轻功 80` ถูกตรวจที่ไหน (plot ฝั่ง UI หรือ `ChooseStealSkill` เอง)
