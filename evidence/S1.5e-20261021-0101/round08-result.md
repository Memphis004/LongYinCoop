# S1.5e — attack round 8 (PASS)

วันที่: 2026-10-21 ~02:20 · build DLL `51775814…` · pid 40516 · เซฟ = สำเนา

## ลำดับที่รัน (1 arm = 1 ยิง)

| # | คำสั่ง | ผล |
|---|---|---|
| 1 | `spike status` | `WaitAttacking` (10) · active `0:0` @ (12,15) · turn 25 · range=11/11 |
| 2 | ผู้ใช้ยืนยันว่าเกมให้ยิง `1:10107` @ (12,14) | — |
| 3 | `set_pref allowRemoteInput=true` + `spike arm 120` | armed=true |
| 4 | `spike attack --id 0:0 --target 1:10107` | **ok=true** |
| 5 | `spike status` (ยืนยันอิสระ) | ตรงกัน |
| 6 | `spike disarm` + `set_pref allowRemoteInput=false` | ปิดครบ |

## ผลวัด (RUNTIME)

| ตัวชี้วัด | ก่อน | หลัง |
|---|---|---|
| `attackComfirmed` | — | **true** |
| `1:10107` hp (เป้า) | 333.231 | **64.19005** → **ลด 269.04095** (hit แรง — น่าจะ crit) |
| `1:10107` `unitAlive` | true | true |
| `0:0` hp (ผู้โจมตี) | 366.77798 | 353.656 (−13.122) |
| `1:10102` hp | 471.545 | 471.545 (ไม่เปลี่ยน — AoE ไม่โดน) |
| turn | 25 | 26 |
| active ถัดไป | `0:0` @ (12,15) | `0:4` @ (13,15) |
| `stateHash` | — | `9e214a0669be683054769675b7c933105f47280c73b33d1b4b05f45a7db317c0` |
| `CostMs` / `ElapsedMs` | — | 1 ms / 209 ms |
