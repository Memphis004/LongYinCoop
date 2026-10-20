# S1.5e — attack round 10 (PASS — เป้าตาย) · **ครบ 10/10**

วันที่: 2026-10-21 ~02:27 · build DLL `51775814…` · pid 40516 · เซฟ = สำเนา

## ลำดับที่รัน (1 arm = 1 ยิง)

| # | คำสั่ง | ผล |
|---|---|---|
| 1 | `spike status` | `WaitAttacking` (10) · active `0:86` @ (13,13) · turn 27 · `turnMoved=true` · range=12/12 |
| 2 | `set_pref allowRemoteInput=true` + `spike arm 120` | armed=true |
| 3 | `spike attack --id 0:86 --target 1:10107` | **ok=true** |
| 4 | `spike status` (ยืนยันอิสระ) | ตรงกัน |
| 5 | `spike disarm` + `set_pref allowRemoteInput=false` | ปิดครบ |

## ผลวัด (RUNTIME)

| ตัวชี้วัด | ก่อน | หลัง |
|---|---|---|
| `attackComfirmed` | — | **true** |
| `1:10107` hp (เป้า) | 17.376463 | **0** → **ลด 17.376463 (ตาย)** |
| `unitAlive` ที่ mod อ่าน | true | true (ยังไม่อัปเดต) · `spike status` หลังจบ: **false** |
| `0:86` hp | 727.9665 | 645.073 (ลดจากเทิร์นศัตรู/AI หลังจบรอบ) |
| turn | 27 | 29 |
| active ถัดไป | `0:86` @ (13,13) | `0:0` @ (12,15) |
| `stateHash` | — | `34d1c0326b66747b3424d9f26f4ea5ef88f5a9de9bdd74c34d1a9c6ec12793dd` |
| `CostMs` / `ElapsedMs` | — | 1 ms / 284 ms |

ศัตรูที่เหลือหลังรอบนี้: `1:10102` 382.778 @ (14,15) — ศึกยังไม่จบ (ไม่อยู่ในเงื่อนไข "ข้ามศึก" ข้อ 5)
