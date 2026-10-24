# ออกจากดันเจี้ยน → กลับหน้า Area (2026-10-23 ~23:16)

สถานะ: RUNTIME (hook `tools/frida_exit_probe.py` + อ่านหลังจบ) · แผนที่ `FinishStealSkill` (ดันเจี้ยนสำนักอู่ตัง) · `maxPower` 55

## ลำดับการเรียก (ms นับจากกดปุ่มออก)

| t (ms) | method | arg |
|---|---|---|
| 0 | `ExploreController.QuitExploreButtonClicked()` | – |
| 423 | (ออกจาก method) | – |
| 2745 | **`ExploreController.ExploreFail()`** | – |
| 3379 | **`ExploreController.FinishExploreMap(int)`** | **arg0 = 0** |
| 4014 | `ExploreController.HideExploreMap()` | – |
| 4439 → 4864 | `ExploreController.ResetExploreMap()` | – |
| 5288 | (HideExploreMap ออก) | – |
| 6359 | (FinishExploreMap ออก) | – |
| 7473 | (ExploreFail ออก) | – |

- **ไม่มีการเรียก** `PlotController.FinishStealSkill` / `PlotController.FinishExploreMap` / `AreaController.PlayerLeaveArea` / `ReturnBigMapButtonClicked` เลย
- ⇒ เส้นทาง "กดออก" = `QuitExploreButtonClicked` → **`ExploreFail`** → `FinishExploreMap(0)` (0 = ล้มเหลว) → ซ่อน/รีเซ็ตแมพ
- ใช้เวลา ~7.5 s ตั้งแต่กดจนจบ flow

## สถานะก่อน/หลัง (ตลอด flow ไม่เปลี่ยน)
- `leftPower` **42 → 42** ⇒ **กดออกไม่หักพลังงาน**
- `ExploreController.inited` true ตลอด (ไม่ได้เคลียร์)
- `AreaController.inited` true ตลอด, `areaData` = **41 `武当派`**
- `plotTextShowing` / `plotChoiceShowing` = false ตลอด (ไม่มี dialog กลางทาง)

## หลังจบ flow (อ่านสด 23:18)
| object | activeSelf |
|---|---|
| `ExploreController.exploreObj` | **false** |
| `ExploreController.exploreUIPanel` | **false** |
| `AreaController.areaUIBelow` | **true** |
| `BigMapController.bigmapRoot` | true (root พื้นหลัง) |
| `BigMapController.bigmapUIPanel` | **false** |
| `BigMapController.playerArmy` | **false** |

⇒ กลับไป **หน้า Area ของสำนัก (area 41 武当派)** ไม่ใช่หน้าแผนที่ใหญ่ และไม่มีหน้าสรุปผล/ダイアログ

## ข้อสรุป
1. ปุ่มออกจากดันเจี้ยน = `ExploreController.QuitExploreButtonClicked()` ไม่ใช่ `FinishExploreMap` ตรง ๆ
2. เกมถือว่า **"ออกก่อนจบ" = ล้มเหลว** (`ExploreFail()` ถูกเรียกจริง) และ `FinishExploreMap(0)` รับค่า 0
3. ปลายทางคือหน้า Area เดิม (AreaController ไม่ถูกปิด/reload, แค่ explore UI ถูกซ่อน+รีเซ็ต)
4. การออกไม่เสีย `leftPower`

## ยังไม่รู้
- `FinishExploreMap(1)` = เส้นทางสำเร็จหรือไม่ (ยังไม่ได้ทำให้จบแบบสำเร็จในรอบนี้)
- `FinishExploreMap(arg)` ใช้ arg ทำอะไร (branch ในโค้ดยังไม่ได้อ่าน)
- ผลข้างเคียงต่อภารกิจ (`finishFuc = FinishStealSkill`) — รอบนี้ไม่ถูกเรียกเลย
