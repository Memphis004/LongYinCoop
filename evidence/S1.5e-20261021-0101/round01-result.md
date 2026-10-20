# S1.5e — attack round 1 (PASS)

วันที่: 2026-10-21 ~01:45 · build DLL `51775814…` · pid 40516 · เซฟ = สำเนา (`Save.real-S1.5e-20261021-0101` แยกไว้)

## ขั้นตอนที่รัน (ยิงจริง 1 ครั้ง ไม่ retry)

| # | คำสั่ง | ผล |
|---|---|---|
| 1 | `set_pref allowRemoteInput=true` | true |
| 2 | `spike arm --seconds 120` | armed=true |
| 3 | `spike attack --id 0:0 --target 1:10066` | ok=true (1 ครั้ง) |
| 4 | `spike status` (ยืนยันอิสระ) | ตรงกับผลของ mod |
| 5 | `spike disarm` + `set_pref allowRemoteInput=false` | ปิดครบ |

## ผลวัด (RUNTIME)

| ตัวชี้วัด | ก่อน | หลัง | หมายเหตุ |
|---|---|---|---|
| `nowActiveState` | `WaitAttacking` (10) | `WaitAttacking` (10) ตอนอ่านในเฟรมคลิก | attackComfirmed อ่านในเฟรมเดียวกับ `BattleGridClicked` |
| `attackComfirmed` | — | **true** | transient (ค่าถูกตรวจในเฟรมคลิกเท่านั้น) |
| `1:10066` hp (เป้า) | 551.870 | **429.068** | **ลด 122.802 — ยืนยันซ้ำด้วย `spike status` อิสระ** |
| `1:10066` `unitAlive` | true | true | ไม่ตาย |
| `1:10067` hp (ข้างเป้า) | 553.840 | 338.878 | ลด 214.962 — AoE โดนข้างเคียง (damageRangeCount=11) |
| `0:0` hp (ผู้โจมตี) | 495.316 | 466.825 | **ลด 28.491** → ยืนยันว่าห้ามใช้ hp ผู้โจมตีเป็นเงื่อนไขคงที่ |
| `turnIndex` | 12 | 13 | เทิร์นจบเองหลังโจมตี |
| active ถัดไป | `0:0` @ (6,15) | `0:9` @ (3,4) | |
| `stateHash` | (ก่อนยิง ไม่ได้เก็บในรอบนี้) | `9fb402618d0558b634f99a308eb8df95a8081490d472baeef3f0eb418aebe401` | |

- `CostMs` (main thread) = **2 ms** · `ElapsedMs` (รวมรอ hp) = **207 ms**
- `Call` = `BattleGridClicked(target)` ครั้งเดียว · ไม่มีการเรียก `ChangeActiveSkill`/`ChangeNowActiveAttackSkill` จาก mod
- `TargetInAttackRange=true` จาก `attackRangeGridUnits` (advisory) — ยังไม่ใช้ตัดสิน

## ข้อสังเกตที่ต้องถือ

1. mod เห็น hp ลดใน **207 ms** หลังคลิก (ไม่ต้องรอ 3000 ms) — polling ใน `Tick` จับได้เร็ว
2. AoE โดนยูนิตข้างเคียงและ **โดนผู้โจมตีเอง** — การยืนยันต้องอิง hp ของเป้าที่ระบุเท่านั้น
3. ก่อนยิง เกมอยู่ใน `WaitAttacking` เพราะ **ผู้ใช้กดปุ่มโจมตี + hover ใน UI** (ตามมติ: mod ไม่เรียก ChangeActiveSkill เอง) — การตั้งท่าเป็นงานของผู้ใช้ 1 ครั้ง/รอบ
