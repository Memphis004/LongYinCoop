# S1.5-c W2/W4 + J7 + W5 · รายงานวัด (2026-10-20)

รอบนี้: **W2 PASS · W4/J6 PASS (ปิด J6) · J7 ได้คำตอบฝั่งความเร็วปกติ · W5 ผิดตัวชี้วัด** — 1 correction สำคัญ (pause) และ 1 ตัวชี้วัดที่ใช้ไม่ได้ (hp)

- build: `f0cf25994008` (S1.5-c-W2W4, mod 0.4.0) · เกม pid 26720 · เซฟ = **สำเนา** `S1.5c-w2w4-20261020-165604`
- compat ก่อน hook: LongYinThai patch เฉพาะ `set_text` (UILabel / UI.Text / TextMesh / TMP_Text) → **ไม่ชน 5 method** (`compat-notes-prehook.md`)
- hook: `jitprobeW2` `installed=true missing=0` · `jitprobeW4` `installed=true missing=0` · ปิดแล้วทั้งคู่ (`installed=false`, hits 92 / 10, dropped 0)
- หลักฐาน: `jitprobe-w2-…ndjson` (92) · `jitprobe-w4-…ndjson` (10) · `status-100ms.ndjson` (8,945 sample @100 ms) · `analysis-w2w4.txt` · `analysis-more.txt` · `SHA256SUMS.txt`

## 1. W2 — ลำดับ call ฝั่ง move [RUNTIME]

| # | ข้อ | ค่า |
|---|---|---|
| 1 | event ทั้งหมด | 92 (dropped 0) |
| 2 | `MoveButtonClicked()` | **19** · post-state = **6** ทุกครั้ง |
| 3 | `GenerateMovePath(GridUnitData)` | **62** · post-state = **7** · `targetRow/targetCol` = ช่องที่ชี้ · gap ระหว่าง call 1–437 frame |
| 4 | `RegretMove()` | **10** · post-state = **2** |
| 5 | `CancelMove()` | **1** · post-state = **2** |
| 6 | ลำดับที่วัดได้ | `MoveButtonClicked` (→6) → `GenerateMovePath` หลายครั้ง (→7) → **`RegretMove` แล้ว `MoveButtonClicked` ในเฟรมเดียวกัน** (10/10) |
| 7 | active unit position ใน event | 18_0 → 17_0 → 8_0 → 9_0 (ยูนิตเปลี่ยนช่องจริงระหว่างรอบ) |

ข้อสรุปที่ใช้ต่อได้: ปุ่มเดินคือ `MoveButtonClicked()` (ไม่มี arg), path ที่เกมคำนวณได้จาก `GenerateMovePath(targetGrid)`, และ **การ regret จะพา UI กลับเข้าโหมดเดินเอง** (เรียก `MoveButtonClicked` ต่อทันที) — สำคัญกับ S1.5-d เพราะ replay ต้องไม่เรียกซ้ำ

## 2. W4 / J6 — pause ปิดแล้ว [RUNTIME]

| # | ข้อ | ค่า |
|---|---|---|
| 1 | `BattlePauseButtonClicked(GameObject)` | 10 ครั้ง · `obj` = **`Stop`** ทุกครั้ง |
| 2 | `battlePaused` หลัง call | สลับ **true/false สลับกันครบ 10 ครั้ง** (pause 5 / resume 5) |
| 3 | `Time.timeScale` ตอน pause | **1 ทุกครั้ง** → pause ไม่ได้ทำผ่าน timeScale |
| 4 | `nowActiveState` ตอนกด | 3, 12, 7, 7, 3, 12, 13, 0, 8, 1 (ค่าไม่ซ้ำ → กดได้ทุกสถานะ) |
| 5 | หน้าต่าง pause ใน stream | 5 ช่วง: 1.91 / 6.33 / 2.01 / 1.01 / 2.52 วินาที |
| 6 | `battleTime` ระหว่าง pause | **ไม่เดิน** (เช่น pause 6.33 s ที่ state=Moving: 2.283 → 2.283) → pause หยุดนาฬิกาศึกจริง |
| 7 | `timeLimit` / `AIWaitTime` | `timeLimit` = 0 · `AIWaitTime` = -0.007 คงที่ (ไม่มีนัย) |

**Correction ต่อข้อสรุป W1:** W1 สรุปว่า "`battlePaused` ไม่ขยับเมื่อกดปุ่ม" — รอบนี้พิสูจน์ว่า **`battlePaused` ขยับถูกต้องเมื่อปุ่มลงทะเบียน** (10/10) ข้อสรุป W1 จึงต้องแก้เป็น "การกดในรอบ W1 ไม่ได้ลงทะเบียน (ยืนยันไม่ได้เพราะรอบนั้นไม่มี hook บนปุ่ม)" ไม่ใช่ "ฟิลด์ไม่สะท้อน pause"

## 3. J7 — เทิร์นรอ input จริง (ไม่ใช่ ATB) ที่ความเร็วปกติ [RUNTIME]

| # | ข้อ | ค่า |
|---|---|---|
| 1 | `WaitPlayerInput` ทั้งหมด | **787.24 วินาที** · `battleTime` เพิ่ม **0.000** (ratio 0.00, 7,823 sample) |
| 2 | ช่วงรอนิ่งยาวสุด | **722.29 วินาที** ที่ turn 19 · `turnIndex` ไม่ขยับเลย |
| 3 | เทิร์นถูกตัดเองไหม | **ไม่** — `turnIndex` วิ่ง -1→19 ตามลำดับ และหยุดที่ 19 ตลอดการรอ |
| 4 | `battleTime` เดินตอนไหน | เฉพาะ `Attacking` (ratio 0.11) / `Rest` (0.05) / `Finish` (0.04) / `None` (0.27) — **แอนิเมชัน ไม่ใช่ตัวจับเวลาเทิร์น** |
| 5 | `timeScale` ที่เห็นทั้งรอบ | **1 เท่านั้น** → ยังไม่มีหลักฐานว่าโหมดเร่งความเร็วใช้ `Time.timeScale` |

**ยังไม่ปิดครบ:** เกณฑ์ J7 ต้องการทั้ง "ความเร็วปกติ" และ "เร่งความเร็ว" — รอบนี้ได้เฉพาะฝั่งปกติ (787 s ไม่มีการตัดเทิร์น) ฝั่งเร่ง **NOT-RUN** เพราะไม่เห็น `timeScale` เปลี่ยน จึงยังไม่กล้าสรุปว่าเกมเดินเร็วขึ้นด้วยกลไกใด

## 4. W5 — hp ที่เพิ่มเข้าไป **ไม่ใช่** hp ในศึก [RUNTIME]

- `units[].hp` อ่านจาก `HeroData.hp` (typed): ตลอด 8,945 sample มี **0 การเปลี่ยนแปลง** และทั้ง 9 ยูนิตมีค่าเดียวคงที่ (เช่น 221.326 / 475.701)
- ระหว่างรอบมี `Attacking` 6.13 s และ `WaitAttacking` 8.65 s → มีการโจมตีเกิดขึ้นจริง แต่ค่า hp ที่เราเลือกอ่านไม่ขยับ
- [DUMP] `BattleUnit` มี `originHp` (`0xC0`) และเมธอด `ChangeHp(float,bool,bool,bool)`, `ChangeTrueHp(float)`, `IsAlive` — ยังไม่เคยอ่าน `originHp`
- ผลกระทบ: **`stateHash` ปัจจุบันก็ไม่ไวต่อดาเมจ** (เพราะใส่ `HeroData.hp` อยู่) และยังใช้ยืนยันผล `spike attack` ไม่ได้

## 5. เทียบเกณฑ์ของรอบนี้

| เกณฑ์ | ผล |
|---|---|
| W2 ได้ลำดับ call ฝั่ง move + argument | **PASS** (92 event, 4 method, ลำดับคงที่ รวม regret→move) |
| W4 ปิด J6 (ใคร toggle pause) | **PASS** — `BattlePauseButtonClicked` + `battlePaused` สลับ 10/10 |
| exit 5: `battlePaused` เปิด/ปิด 10/10 ไม่ค้าง | **PARTIAL 5/10** — ได้ 5 รอบ (pause→resume) ไม่ค้าง แต่ยังไม่ถึง 10 รอบตามเกณฑ์ |
| J7 (ปกติ) | **PASS ฝั่งปกติ** — รอ 787 s ไม่มีการตัดเทิร์น |
| J7 (เร่งความเร็ว) | **NOT-RUN** — `timeScale` = 1 ตลอด |
| W5 hp ต่อยูนิต | **ส่งมอบแล้ว แต่ตัวชี้วัดผิด** — 0 การเปลี่ยนแปลง |
| ไม่เรียก method เปลี่ยน state / ไม่เขียน field | **PASS** (postfix อ่านอย่างเดียว, `AllowRemoteInput=false`) |
