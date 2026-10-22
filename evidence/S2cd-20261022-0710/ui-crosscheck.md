# UI cross-check — เทียบค่าจาก memory กับตัวเลขบนจอ (2026-10-22 07:27)

กติกาหลักฐาน: ค่าที่อ่านจาก memory ต้องเทียบกับตัวเลขบนจอ **ก่อน** ใช้เป็นข้อเท็จจริง
วิธี: อ่าน component ที่ HUD ใช้แสดงผลจริง (`HudController` → `UnityEngine.UI.Text.m_Text`) แล้วเทียบกับค่าที่ `__cd.snapshot()` อ่านจาก `WorldData`/`TimeData`/`ForceData`

## 1. แหล่งบนจอที่ใช้

| สิ่งที่แสดง | field บน component |
|---|---|
| วันที่/เวลา | `HudController.timeLabel` (`Text`) |
| ชื่อตัวละคร | `HudController.nameLabel` |
| เงิน | `HudController.moneyLabel` |
| ชื่อสำนัก/ตำแหน่ง | `HudController.forceLabel` |
| ชื่อเสียง/ชื่อเสีย/คุณูปการ | `fameLabel` / `badfameLabel` / `contributionLabel` |
| จำนวนฮีโร่/พื้นที่ | `heroNumLabel` / `areaNumLabel` |
| ทรัพยากรในแถบทรัพยากร | `UnityEngine.UI.Text` ที่มีรูปแบบ `<ค่า>/<สูงสุด>` |

## 2. ผลเทียบ

### 2.1 วันที่ — ตรง

| | ค่า |
|---|---|
| memory (`worldTime`) | year **10** · month **9** · day **10** |
| บนจอ (`timeLabel`) | `10 ปี 9 เดือน 10 วัน` |
| สรุป | **ตรงทุกฟิลด์** ✅ |

### 2.2 ทรัพยากร — ตรง (แต่จอปัดเศษทิ้ง ไม่ปัดขึ้น)

| # | memory `store` | บนจอ | ตรง? |
|---|---|---|---|
| 0 | 4764.4 | `4764/14700` | ✅ (floor) |
| 1 | 4733.7 | `4733/6125` | ✅ (floor; **ไม่ใช่** 4734) |
| 2 | 5570.3 | `5570/6125` | ✅ (floor) |
| 3 | 5475.5 | `5475/6125` | ✅ (floor; **ไม่ใช่** 5476) |
| 4 | 3683.8 | `3683/6125` | ✅ (floor; **ไม่ใช่** 3684) |
| 5 | 2450.0 | `2450/2450` | ✅ |

⇒ ค่าทั้ง 6 ตรงกับ memory แบบ **ตัดทศนิยมทิ้ง** (truncate) ไม่ใช่ปัดครึ่งขึ้น — ผมเคยคาดว่า 4733.7 จะแสดงเป็น 4734 ซึ่ง **ผิด**; ที่ถูกคือ 4733

### 2.3 HUD อื่น ๆ (บันทึกไว้ ไม่ได้ใช้ตัดสินอะไร)

```
nameLabel        = หวังเวิ่นเทียน
moneyLabel       = 61058
forceLabel       = สำนักเซียนเสีย<color=#FD1430>ประมุขสำนัก</color>
fameLabel        = 9999      badfameLabel = 0
contributionLabel= 1065/3200
heroNumLabel     = 148/145   areaNumLabel = 2/10
fightScoreLabel  = 26171
```

### 2.4 งาน 5 วัน "เสร็จแล้ว" — เห็นข้อความสรุปบน UI แต่ยังไม่ยืนยันว่ากำลังแสดงอยู่

พบ `Text` หนึ่งรายการในชุด UI:

```
花费5天เวลา，收获了348แร่。
ผลงานเช่นนี้ก็ถือว่าพอไปวัดไปวาได้กระมัง
```

= "ใช้เวลา 5 วัน ได้รับ 348 แร่" ⇒ เป็นข้อความสรุปจบงานของรอบที่ 2 (5 วัน)

**ข้อจำกัด**: การอ่าน `m_Text` ไม่ได้บอกว่าออบเจกต์นั้น *กำลังแสดงอยู่* หรือถูกซ่อน/อยู่ใน pool (การเช็กต้องเรียก `activeInHierarchy` ซึ่งเป็นการ invoke → ต้องอนุมัติ)
ฝั่งข้อมูล: `workFinished=true`, `leftWorkingDay=0`, `working=false` ⇒ งานจบจริงตาม state

## 3. ข้อสรุป

- **วันที่บนจอ = ค่าจาก memory** (year 10 / month 9 / day 10) ⇒ ใช้ `worldTime` เป็นแหล่งความจริงเรื่องเวลาได้
- **ทรัพยากรทั้ง 6 ตรง** กับ `ForceData.resourceStore` (แสดงแบบ floor)
- **`snapshot()` ผ่านการตรวจกับจอ** ⇒ ชุด field ที่ใช้ (`WorldData.worldTime`, `WorldData.hour`, `WorkingUIController.*`, `ForceData.resourceStore`) ใช้เป็นหลักฐานได้ (ระดับ: RUNTIME + cross-check กับจอ)
- ยังไม่ยืนยันด้วยตา: สถานะ "จบงาน" บนแผงงาน (มีข้อความสรุปอยู่ใน UI แต่ไม่รู้ว่า active)
