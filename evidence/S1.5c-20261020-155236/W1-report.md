# S1.5-c / W1 — click-chain probe (J4 + L3) · รายงานวัด (2026-10-20)

สถานะ: **ผ่านเกณฑ์ของ W1 ตามที่อนุมัติ** · 1 ข้อปิดได้ (J4) · 1 ข้อปิดได้บางส่วน (L3 = move) · 2 ข้อ **ยังไม่ปิด** (J6 pause, J3 field) · 0 FAIL

- build ที่รัน: `LongYinCoop.dll` sha256 `d339fcad…` (deploy 16:09, `logs/deploy.log`)
- pref ที่ใช้: `jitprobe` (เปิด 16:14:58 → ปิด 16:24, `installed=false`, hook ถูกถอดจริง — `compat_dump` สดแล้วไม่พบ owner `longyincoop.jitprobe.w1`)
- เกม: pid **32984** บน **สำเนาเซฟ** `S1.5c-20261020-160858`
- หลักฐานดิบ: `jitprobe-20261020-161458.ndjson` (204 บรรทัด, sha256 `70aacb8a…`) · `spike-status-w1.ndjson` (54,473 sample, sha256 `52f63e3d…`) · `analysis-w1.txt` (sha256 `31d653f6…`) · `SHA256SUMS-w1.txt`

## 0. ขอบเขตที่ทำจริง (อ่าน/วัดเท่านั้น)

patch แบบ **postfix อ่าน argument เท่านั้น** 2 method:
`GridUnitController.OnClick` (อ่าน `gridData.row/column` + `gameObject.name`) และ
`BattleController.BattleGridClicked(GameObject)` (อ่าน `clickedGrid.name`)
— ไม่มีการเรียก method ของเกม, ไม่เขียน field, ไม่แตะ `Save/`, ไม่มีคำสั่ง `spike move/attack/pause` ใน build นี้

## 1. ผลวัด

| # | ข้อ | ค่า | ป้าย |
|---|---|---|---|
| 1 | คลิกกริดที่ผู้เล่นกดจริง | **102 คลิก** (`onClickHits=102`, `battleGridClickedHits=102`, `dropped=0`) | RUNTIME |
| 2 | ทุกคลิกผ่าน `GridUnitController.OnClick` | **102/102** | RUNTIME |
| 3 | ลำดับ call | `BattleController.BattleGridClicked` **ก่อน** `GridUnitController.OnClick` เสมอ · **frame เดียวกัน 102/102** · `obj` ตรงกัน 102/102 | RUNTIME |
| 4 | argument ที่ UI ส่ง | `BattleGridClicked(GameObject)` ได้ GameObject ชื่อ `"<row>_<col>"` เช่น `"3_4"`; `OnClick` อ่าน `gridData.row/column` ได้ค่าเดียวกัน | RUNTIME |
| 5 | `nowActiveUnitMoved` (turnMoved) | false→true **61 ครั้ง**; 60 ครั้งมีคลิกก่อนหน้า ≤ 300 frame; lag **7–279 frame** (p50 = 26) | RUNTIME |
| 6 | คลิกที่เป็นการเดินจริง | **39 คลิก** ที่ยูนิตไปยืนช่องที่คลิกและ `turnMoved=true` ค้าง ≥ 5 frame — นับได้ **21–824 frame** ต่อคลิก (ทุกคลิก ≥ 21) | RUNTIME |
| 7 | `nowActiveUnitAttacked` (turnAttacked) | true ทั้งหมด **1,005 sample** และ **`isPlayerTurn=false` ทุกตัว** · false→true 65 ครั้ง · `turnAttacked=true` ขณะ `isPlayerTurn=true` = **0 / 10,177 sample** | RUNTIME |
| 8 | `battlePaused` | **0 / 54,473 sample** (รวมช่วงที่ผู้ใช้กดปุ่มหยุดศึก 1 ครั้ง) | RUNTIME |
| 9 | sample ที่อยู่ในศึก | 28,412 / 54,473 (นอกศึก 26,061) · `stateHash` เปลี่ยน 926 ครั้ง · errors 0 | RUNTIME |
| 10 | เซฟ | `verify` **PASS**: mounted copy vs copy baseline `changed=1` (เฉพาะ `PlayerprefData.dat`) `missing=0 added=0`; real `Save.real-S1.5c-…` vs baseline **0/0/0**, drift 0 | RUNTIME |

## 2. เทียบเกณฑ์ของ S1.5-c (S1.5-plan §2: "ได้ลำดับ call จริงของ move 1 ครั้ง + attack 1 ครั้ง พร้อม argument, ระบุ entry method ได้, รู้ว่าต้องเขียน field ไหม")

| เกณฑ์ย่อย | ผล | หลักฐาน |
|---|---|---|
| ลำดับ call ของ **move** + argument | **PASS** | 102 คลิกครบลำดับ `BattleGridClicked(GameObject "r_c")` → `OnClick` (frame เดียวกัน) · 39 คลิกเดินจริง จับคู่กับ `turnMoved` ได้ |
| ลำดับ call ของ **attack** + argument | **PASS-with-limitation** | คลิกโจมตีก็ผ่าน chain เดียวกัน (ทุกคลิกผ่าน `OnClick` 102/102) แต่ **ยืนยันผลโจมตีด้วย turn flag ไม่ได้** (ดู §3.1) — ต้องใช้ hp ของ target ยืนยัน ซึ่ง capture รอบนี้ไม่มี |
| ระบุ entry method ได้ | **PASS** | entry ระดับ UI = คลิก GameObject ช่องกริด → `BattleController.BattleGridClicked(GameObject)` เป็นตัวแรกที่ถูกเรียก |
| รู้ว่าต้องเขียน field ไหม | **ยังไม่ปิด (J3)** | probe รอบนี้อ่าน argument เท่านั้น ไม่ได้อ่านค่าก่อน/หลัง interior ของ call จึงยังพิสูจน์ไม่ได้ว่า path นี้เขียน field อะไร |

## 3. สิ่งที่วัดได้แล้ว "ขัดกับสมมติฐานในแผน" — ต้องแก้เอกสาร

### 3.1 `turnAttacked` ใช้เป็นตัวตัดสินของ attack **ไม่ได้** ตามที่แผนสมมติ
แผน §3 (G3) เขียนว่า "ตัวชี้วัดที่ใช้ได้คือ turn flags ของ controller" — วัดจริงแล้ว:
- `turnAttacked=true` เกิดเฉพาะตอน `isPlayerTurn=false` (1,005/1,005 sample) และไม่เคย true พร้อม `isPlayerTurn=true` (0/10,177)
- ต่างจาก `turnMoved` ที่ true พร้อม `isPlayerTurn=true` ได้ปกติ (เช่น frame 37936 turn 0 `playerTurn=True lag=14`)

→ [RUNTIME] ข้อสรุป: **`nowActiveUnitMoved` ใช้ได้, `nowActiveUnitAttacked` ยังใช้ไม่ได้** จนกว่าจะวัดด้วยตัวอื่น (เช่น `attackComfirmed` flag หรือ hp ของ target) · G3 ของ S1.5-d ต้องไม่พึ่ง `turnAttacked`

### 3.2 `battlePaused` ไม่ขยับเมื่อกดปุ่มหยุดศึก
`paused=0` ตลอด 54,473 sample แม้ผู้ใช้กดหยุดศึก 1 ครั้ง (ผู้ใช้รายงาน) → [RUNTIME] ค่าที่ mod อ่าน (`BattleController.battlePaused`) **ไม่ใช่** ตัวแทนของการกดหยุดใน UI อย่างน้อยในเส้นทางนี้ · J6 ยังไม่ปิด; exit criterion 5 ยังไม่มีวิธีวัดที่ prove ได้

ข้อจำกัดที่ต้องถือ: ยืนยันไม่ได้ว่า "การกด" ครั้งนั้นลงทะเบียนกับเกมจริงหรือไม่ (ไม่มี hook บนปุ่ม) — จึงสรุปได้แค่ว่า *ถ้าลงทะเบียน* ค่า `battlePaused` ก็ไม่เปลี่ยน

## 4. สิ่งที่ยังไม่ทำ / ต้องขออนุมัติก่อน (W ถัดไป)

| # | งาน | ต้องอนุมัติเพราะ |
|---|---|---|
| W2 | hook อ่าน argument ของ `MoveButtonClicked`, `GenerateMovePath`, `CancelMove`, `RegretMove`, `ChangeActiveSkill` | patch เพิ่ม (ยัง read-only) |
| W3 | อ่าน field ก่อน/หลัง call เพื่อปิด J3 (`selectedGrid`, `nowActiveState`, `movePath`) | แตะการอ่าน field ระดับ interior — ต้องยืนยันว่าไม่เขียน |
| W4 | hook `BattlePauseButtonClicked` เพื่อปิด J6 | patch เพิ่ม (read-only) |
| W5 | เพิ่ม hp ต่อยูนิตใน `spike status` เพื่อยืนยันผล attack | แก้ build (ไม่แตะ IL2CPP write) |

## 5. สภาพหลังปิดงาน [RUNTIME]

- `jitprobe` = **false** · patch ถูกถอด (`compat_dump` สดไม่มี owner นี้แล้ว) · ไฟล์ event หยุดโตที่ 204 บรรทัด
- เกมยังรันอยู่ (pid 32984) บน **สำเนา** — ยังไม่ restore; `Save.real-S1.5c-20261020-160858` ยังค้างและสมบูรณ์ 57 ไฟล์
- ไม่มี incident ในรอบนี้: ไม่ crash, ไม่ soft-lock, ไม่มี exception, ไม่มีการบันทึก/โหลดเซฟระหว่างวัด (พิสูจน์ด้วย `verify` ข้างบน)
