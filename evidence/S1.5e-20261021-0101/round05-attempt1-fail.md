# S1.5e — attack round 5, ความพยายามที่ 1: **FAIL (เกมไม่รับคลิก)**

วันที่: 2026-10-21 ~02:22 · build DLL `51775814…` · pid 40516 · เซฟ = สำเนา

## ลำดับที่รัน (ยิง 1 ครั้ง ไม่ retry)

| # | คำสั่ง | ผล |
|---|---|---|
| 1 | `spike status` | `WaitAttacking` (10) · active `0:4` @ (13,15) · turn 20 · range=**4**/5 |
| 2 | `set_pref allowRemoteInput=true` + `spike arm 120` | armed=true |
| 3 | `spike attack --id 0:4 --target 1:10102` (ช่อง (13,14)) | **ok=false** `OUT_OF_RANGE: attackComfirmed=false` |
| 4 | `spike status` | **ไม่มีอะไรเปลี่ยน** (turn 20, hp ทุกตัวเท่าเดิม, state ยัง `WaitAttacking`) |
| 5 | `spike disarm` + `set_pref allowRemoteInput=false` | ปิดครบ |

## ผลวัด (RUNTIME)

| ตัวชี้วัด | ค่า |
|---|---|
| active → เป้า | `0:4` @ (13,15) → `1:10102` @ (13,14) = **ติดกัน 1 ช่อง (orthogonal)** |
| `attackComfirmed` | **false** |
| `TargetInAttackRange` (อ่านจาก `attackRangeGridUnits`) | **false** |
| `attackRangeCount` / `damageRangeCount` | **4** / 5 |
| hp เป้า / ยูนิตอื่น | ไม่เปลี่ยนเลย (589.26 / 537.66 / 99.204 / 475.701 / 738.988 / 377.736 / 221.326) |
| `CostMs` / `ElapsedMs` | 0 ms / 0 ms |
| `TargetHpAfter=0` `TargetAliveAfter=false` | ค่า default ของ struct (deferred poll ไม่รัน) — ห้ามอ่านเป็นผล |

## หลักฐานสะสม 6 ครั้ง: `TargetInAttackRange` ทำนายผลการคลิกได้ 6/6

| # | ผู้ออกคลิก | ช่องเป้า | `TargetInAttackRange` | `attackComfirmed` |
|---|---|---|---|---|
| 1 | r1 `0:0` | (6,16) | true | **true** |
| 2 | r2a1 `0:86` | (12,13) | false | **false** |
| 3 | r2a2 `0:86` | (9,14) | true | **true** |
| 4 | r3 `0:0` | (6,16) | true | **true** |
| 5 | r4 `0:0` | (6,16) | true | **true** |
| 6 | r5a1 `0:4` | (13,14) | false | **false** |

→ **H1 (ระยะ/รูปทรงของ `attackRangeGridUnits`) ได้รับการสนับสนุนเต็มที่, H2 (ผูก hover) ไม่จำเป็นต้องอธิบายข้อมูลชุดนี้**
→ และเคส (13,15)→(13,14) ที่ "ติดกัน 1 ช่อง" แต่ยิงไม่ได้ ⇒ **การโจมตีไม่ใช่ระยะ 1 ช่องเสมอ** (น่าจะมี min/max grid distance ของสกิล เช่น `damageGridUnitsMinGridDistance`/`MaxGridDistance` ใน dump)

## ข้อเสนอ (ยังไม่ทำ — ต้องอนุมัติ + build/รีสตาร์ท)

เพิ่ม **prefire guard**: ถ้า `attackRangeGridUnits` ไม่มีช่องเป้า → คืน `OUT_OF_RANGE` **โดยไม่คลิกเลย** (เหมือน `coopctl prefire_*`) และ log รายชื่อ 4-36 ช่องในลิสต์ เพื่อเลือกเป้าได้แม่นโดยไม่ต้องลองผิดลองถูก

## สถานะนับรอบ

ผ่านจริง **4/10** · attempt ถูกปฏิเสธ **2** ครั้ง (r2a1, r5a1 — รายงานครบ ไม่ retry)
