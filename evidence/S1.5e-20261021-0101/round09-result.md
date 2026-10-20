# S1.5e — attack round 9 (PASS; attempt 1 ถูกปฏิเสธ)

วันที่: 2026-10-21 ~03:20 · build DLL `51775814…` · pid 40516 · เซฟ = สำเนา

## ลำดับที่รัน

| # | คำสั่ง | ผล |
|---|---|---|
| 1 | `spike status` | `WaitAttacking` (10) · active `0:4` @ (13,15) · turn 26 · range=**8**/9 |
| 2 | `spike attack --id 0:4 --target 1:10102` (attempt 1, ช่อง (13,14)) | **ok=false** `attackComfirmed=false` · `TargetInAttackRange=false` |
| 3 | `spike status` | ไม่มีอะไรเปลี่ยน (turn 26 · hp เท่าเดิม) |
| 4 | `set_pref allowRemoteInput=true` + `spike arm 120` | armed=true |
| 5 | `spike attack --id 0:4 --target 1:10107` (ช่อง (12,14)) | **ok=true** |
| 6 | `spike status` (ยืนยันอิสระ) | ตรงกัน |
| 7 | `spike disarm` + `set_pref allowRemoteInput=false` | ปิดครบ |

## ผลวัด (RUNTIME)

| ตัวชี้วัด | ก่อน | หลัง |
|---|---|---|
| `attackComfirmed` | — | **true** |
| `1:10107` hp (เป้า) | 64.19005 | **17.376463** → **ลด 46.813583** (ยังไม่ตาย) |
| `1:10107` `unitAlive` | true | true (`spike status`: 17.376 @ (12,14)) |
| `0:4` hp (ผู้โจมตี) | 468.6766 | 463.319 (−5.358) |
| `1:10102` hp | 471.545 | 421.123 (ลดจากเทิร์นศัตรู/AI) |
| turn | 26 | 27 |
| active ถัดไป | `0:4` @ (13,15) | `0:86` @ (9,11) |
| `stateHash` | — | `62cc0821237ac50b7549d8f370d9de0f5d5a65c5773cf007bff465da7571703f` |
| `CostMs` / `ElapsedMs` | — | 1 ms / 179 ms |

## หลักฐานเรื่องรูปทรงระยะของ `0:4` (RUNTIME, ซ้ำ 3 ครั้ง)

| จาก | ไป | ผล |
|---|---|---|
| (13,15) | (13,14) — ตั้ง 1 ช่อง | **ปฏิเสธ** (`TargetInAttackRange=false`) — เกิด 2 ครั้ง (r5a1, r9a1) แม้ `attackRangeCount` เปลี่ยน 4→8 |
| (13,15) | (12,14) — ทแยง | **ผ่าน** 2/2 (r5, r9) |

→ `0:4` โจมตีได้เฉพาะช่อง **ทแยง** (และ `attackRangeCount` ไม่ได้บอกว่าเป็นช่องไหน) · `TargetInAttackRange` ยังทำนายผลถูก **11/11 ครั้ง**

## สถานะนับรอบ

ผ่านจริง **9/10** · attempt ถูกปฏิเสธ **3** ครั้ง (r2a1, r5a1, r9a1)
ศัตรูเหลือ 2: `1:10107` 17.376 (12,14) · `1:10102` 421.123 (13,14)
