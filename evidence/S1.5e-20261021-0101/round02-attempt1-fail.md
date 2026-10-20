# S1.5e — attack round 2, ความพยายามที่ 1: **FAIL (เกมไม่รับคลิก)**

วันที่: 2026-10-21 ~01:52 · build DLL `51775814…` · pid 40516 · เซฟ = สำเนา

## ขั้นตอนที่รัน (ยิง 1 ครั้ง ไม่ retry ตามกฎ)

| # | คำสั่ง | ผล |
|---|---|---|
| 1 | `set_pref allowRemoteInput=true` | true |
| 2 | `spike arm --seconds 120` | armed=true |
| 3 | `spike attack --id 0:86 --target 1:10107` | **ok=false** — `OUT_OF_RANGE: the game did not accept the click (attackComfirmed=false)` |
| 4 | `spike status` | ยืนยันว่า**ไม่มีอะไรเปลี่ยน** |
| 5 | `spike disarm` + `set_pref allowRemoteInput=false` | ปิดครบ |

## ผลวัด (RUNTIME)

| ตัวชี้วัด | ค่า |
|---|---|
| state ก่อนยิง | `WaitAttacking` (10) |
| active | `0:86` @ (9,11) · turn 14 · `turnMoved=true` `turnAttacked=false` |
| เป้าที่ mod เลือก | `1:10107` @ (12,13) hp 537.66 (ห่างจาก active 3 แถว 2 คอลัมน์) |
| `attackComfirmed` | **false** (อ่านในเฟรมเดียวกับ `BattleGridClicked`) |
| `TargetInAttackRange` (advisory) | false |
| `attackRangeCount` / `damageRangeCount` | 36 / 5 |
| hp เป้าหลังยิง | 537.66 (**ไม่เปลี่ยน**) · `unitAlive=true` |
| hp ยูนิตอื่นทั้งหมด | ไม่เปลี่ยน (0:9 221.326 · 0:4 475.701 · 0:86 765.306 · 0:0 466.825) |
| state หลังยิง | `WaitAttacking` (10) · turn 14 เดิม · `turnAttacked=false` |
| `CostMs` / `ElapsedMs` | 1 ms / 0 ms (ไม่มีการรอ hp เพราะยิงไม่ติด) |
| `TargetHpAfter=0` `TargetAliveAfter=false` | **ค่า default ของ struct เพราะ deferred poll ไม่รัน** — ห้ามอ่านเป็นผลวัด |

## ข้อสรุปที่มีหลักฐาน

1. `attackComfirmed` เป็นประตูจริง: เกม **ปฏิเสธ** คลิกที่ไม่อยู่ในเงื่อนไข และไม่เกิดดาเมจเลย → G6 ได้หลักฐานยืนยัน ("ให้เกมเป็นผู้ตัดสินระยะ")
2. การคลิกที่ไม่ผ่าน **ไม่เปลี่ยน state ของเกม** (turn ไม่ถูกใช้, ยูนิตไม่ขยับ, ไม่มีดาเมจ) → safe-fail
3. `attackRangeCount=36` เป็นเพียงจำนวนช่องในลิสต์ ไม่ได้แปลว่าศัตรูทุกตัวใน 36 ช่องจะโจมตีได้ — ต้องหาว่าเงื่อนไขจริงคืออะไร

## สมมติฐานที่ยังไม่ได้แยก (GUESS ทั้งคู่ ต้องวัดต่อ)

- **H1 (รูปทรงระยะ)**: (12,13) อยู่นอกขอบเขตโจมตีจริงของ `0:86` (ลิสต์ 36 ช่องไม่ใช่พื้นที่ต่อเนื่อง)
- **H2 (ผูกกับการ hover)**: เกมยอมรับเฉพาะคลิกที่ช่อง **ตรงกับช่องที่เมาส์ชี้อยู่** (`selectedGrid` ที่ตั้งโดย `OnPointerEnter`) — หลักฐาน W7 คือคลิกช่อง `4_15` ขณะ `selectedGrid=4_15` พอดี จึงยังแยกไม่ได้

## ผลต่อแผน

รอบ 2 **ยังไม่ผ่าน** (ไม่นับเป็น 1 ใน 10) — ต้องแยก H1/H2 ก่อนยิงรอบถัดไป โดยให้ผู้ใช้ระบุตัวที่ชี้อยู่จริง แล้ว mod ยิงไปที่ช่องนั้นเป๊ะ
