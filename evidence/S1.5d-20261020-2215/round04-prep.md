# S1.5-d / รอบ 4 — วัด "คลิกจริงของผู้เล่น" ด้วย probe ที่ติดตั้งอยู่แล้ว (read-only)

สถานะ 2026-10-20 22:24 [RUNTIME]

## ทำไมรอบนี้
รอบ 3 (`MoveButtonClicked` → `BattleGridClicked(cell.gameObject)`) ไม่เกิด path และไม่เปลี่ยน state
→ แปลว่า mod เรียกผิด "ประตูเข้า" ไม่ใช่แค่อาร์กิวเมนต์ผิด จึงต้องวัดลำดับ call จริงของผู้เล่น 1 ครั้งก่อนเดารอบใหม่

## สิ่งที่ติดตั้งแล้ว (ของเดิมใน build `b278955e33c8` — ไม่ต้อง deploy ใหม่)
| probe | hook | ไฟล์ |
|---|---|---|
| W1 (`jitprobe`) | `GridUnitController.OnClick`, `BattleController.BattleGridClicked(GameObject)` | `UserData/LongYinCoop/CoopSpike/jitprobe-20261020-222447.ndjson` |
| W2 (`jitprobeW2`) | `MoveButtonClicked`, `GenerateMovePath(GridUnitData)`, `CancelMove`, `RegretMove` + state หลัง call | `UserData/LongYinCoop/CoopSpike/jitprobe-w2-20261020-222447.ndjson` |

ทั้งสองเป็น postfix อ่านอย่างเดียว: ไม่เรียก method เกม, ไม่เขียน field, ไม่แตะ `__result`
panic switch `AllowRemoteInput = false` (mod ยิงคำสั่งเองไม่ได้)

## สถานะก่อนวัด [RUNTIME]
`nowActiveState = WaitPlayerInput`, active unit `0:0` ที่ `(6,0)`, `moved = false`

## ขั้นตอนที่ขอให้ผู้เล่นทำ (ห้ามให้ mod ยิงคำสั่ง)
1. กดปุ่ม **ย้าย** ในเกมด้วยมือ
2. คลิกช่องปลายทาง 1 ช่องด้วยมือ (เช่น ข้าง ๆ ยูนิต)
3. ปล่อยให้ยูนิตเดินจนจบ แล้วกด Cancel/จบตามปกติของเกม

## สิ่งที่จะวัดได้
- ลำดับจริง + frame ของ 5 method (`OnClick` / `BattleGridClicked` / `MoveButtonClicked` / `GenerateMovePath` / `CancelMove`)
- `nowActiveState` หลังแต่ละ call → รู้ว่า state 6→7 เกิดที่ call ไหน
- `GenerateMovePath` ถูกเรียกตอนคลิกจริงหรือไม่ และได้ `targetRow/targetCol` อะไร
- ชื่อ GameObject ที่ UI จริงส่งเข้าสู่ `BattleGridClicked` เทียบกับ `GridUnitController.gameObject`

## ข้อจำกัดที่รู้ล่วงหน้า
- build นี้ **ไม่มี** stack trace และไม่มี pointer identity (เพิ่มไว้ใน W6 ซึ่งยังไม่ deploy เพราะเกมเปิดอยู่)
- ถ้าผลออกมาไม่ชี้ชัด (เช่น ต้องรู้ว่าใครเรียกอะไร) จะ deploy W6 (`81a8f8971255`) แล้ววัดซ้ำอีกรอบ
