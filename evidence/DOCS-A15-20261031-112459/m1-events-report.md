# M1 events report — งาน 4 รอบ 6 (มติ R6-7 · 2026-10-31 · static read-only · ไม่เปิดเกม)

- **ขอบเขต (R6-7):** อายุอีเวนท์ตามแผนที่เท่านั้น (BigMap/AreaMap random + scheduled world events) · **เศรษฐกิจเหมือง 16%/วัน ไม่อยู่ใน M1 นี้** · ไม่รวม Q6 (timer ที่ไม่ใช่หน่วยวัน) · ภารกิจ (`MissionData.leftTime`/`GiveUpMission`) ไม่อยู่ใน Q1–Q5 — อ้างอิงเท่าที่พบ
- **วิธี:** (1) evidence เดิม `evidence/TIMEstatic-20261027-051647/bodies/GameController_ChangeDay_leaf.txt` (decrement/สาย event-expiry) (2) `Il2CppDumper/dump.cs` (โครง class/enum/field) (3) disasm รอบนี้ `m1_disasm.py` → `m1-bodies/` 17 ไฟล์ (resolve จาก script.json ตรงที่อยู่ — read-only) · ป้าย: [DISASM] [DUMP] [INFERRED] [UNKNOWN]
- **โครงหลัก [DUMP]:** `EventData.leftTime` 0x68 (int, **หน่วยวันเกม**) · `EventData.eventOutTimeCallFuc` 0x90 (string) · `WorldEventDataBase{startTime:TimeData 0x20 (year/month/day), repeatType 0x30, repeatDay 0x34, repeatDayRandomRange 0x38, lastTime 0x3C}` · `WorldEventRepeatType {Repeat=0, AutoRandom=1, NoRepeat=2}`

## Q1 — ชนิดอีเวนท์ + ฟังก์ชันสร้าง

| ชนิด | ผู้สร้าง | ตัวตั้ง `leftTime` | แหล่งค่า |
|---|---|---|---|
| อีเวนท์สุ่ม (BigMap/AreaMap) | `GameController$$GenerateRandomEvent` (0x7B18A0) — **ถูกเรียกใน `ChangeDay` leaf** [DISASM + evidence เดิม] | `leftTime = GlobalData$$RandomRange(7,15)` และอีกเส้นทาง `RandomRange(5,11)` [DISASM — `lea edx,15; lea ecx,edx-8` / `lea edx,11; lea ecx,edx-6` แล้ว call] | **hard-code ในโค้ด (ช่วงสุ่ม)** — template จาก `RandomEventController$$GetRandomEventDataBase` (data) → `EventData$$Clone` + สุ่ม `difficultyRate` |
| อีเวนท์กำหนดเวลา/ซ้ำ | `WorldEventController$$ManageWorldEvent` (0x9D27B0 — เรียกใน ChangeDay leaf) → `CreateWorldEvent(WorldEventDataBase)` (0x9D17D0) และ overload ที่รับ `lastTime` เป็นพารามิเตอร์ (0x9D1EF0/0x9D2100) | `leftTime (0x68) = พารามิเตอร์ lastTime` [DISASM — `mov [rax+0x68], edi`] · ทาง `CreateWorldEvent(WorldEventDataBase)` อ่าน `lastTime` จาก field 0x3C [DISASM] | **data asset** (`WorldEventDataBase.lastTime`) = ค่ารายชนิด [UNKNOWN] |
| อีเวนท์ถูกลบ (จบ/หมดอายุ) | `RemoveBigMapRandomEvent` / `RemoveAreaMapRandomEvent` / `RemoveEvent` (เรียกใน ChangeDay leaf) → `RemoveWorldEvent` [DISASM] | — | outTime flag ผ่าน overload 2 พารามิเตอร์ |

## Q2 — `leftTime` เริ่มต้นต่อชนิด + หน่วย + อายุเป็นเวลาจริงที่ 720 วิ/วัน

| กลุ่ม | ค่าเริ่มต้น | อายุจริง (720 วิ/วัน) | สถานะ |
|---|---|---|---|
| อีเวนท์สุ่ม — เส้นทางที่ 1 | **สุ่ม 7–15 วัน** [DISASM] | **84–180 นาที** | ยืนยันจากโค้ด |
| อีเวนท์สุ่ม — เส้นทางที่ 2 | **สุ่ม 5–11 วัน** [DISASM] | **60–132 นาที** | ยืนยันจากโค้ด (เส้นทาง = สายอีเวนท์คนละประเภท — ระบุว่าแผนที่ไหนยังไม่ได้แยกใน body) |
| อีเวนท์กำหนดเวลา (`WorldEventDataBase.lastTime`) | จาก **data asset** | ต่อวัน = 12 นาทีจริง (720 ÷ 60) | **รายชนิด = [UNKNOWN]** (อยู่ใน data asset — คำขอแยกเดียวกับ A1.5-2) |

- **หน่วย = "วันเกม"** ยืนยัน 2 ทาง: decrement วันละ 1 ใน `ChangeDay` leaf (`dec dword ptr [rax+0x68]` ×2 จุด [DISASM]) + สูตรซ้ำ `idiv repeatDay` [DISASM]
- **เกณฑ์เสนอ "อายุ × 720 วิ < 10 นาที = ต้องตัดสินแยก"** (เกณฑ์เป็นข้อเสนอ ไม่ใช่มติ): ค่าที่พบในโค้ดทั้งหมด (ต่ำสุด 5 วัน = 60 นาที) **ไม่มีรายการใดต่ำกว่าเกณฑ์** — ส่วน `lastTime` รายชนิดจาก data ยังไม่รู้ค่า ⇒ ถ้ามีชนิดที่ `lastTime ≤ 1` (≤ 12 นาที) จะต้องตัดสินแยก — ตรวจได้เมื่อได้ data asset หรือวัดบนจอ [RUNTIME]

## Q3 — ผู้ลด `leftTime` + ผลเมื่อถึง 0

- **ผู้ลด = `ChangeDay` leaf** [DISASM — `dec [rax+0x68]` ที่ 0x0180791900 และ 0x0180791A8D พร้อม `cmp` ≤ 0 ก่อนหน้า] — ตรง audit เดิม ("ChangeDay เรียก ManageWorldEvent/GiveUpMission/event-expiry") และ leaf เรียก `GenerateRandomEvent` + `WorldEventController$$ManageWorldEvent` + `Remove*` ครบ [DISASM evidence เดิม]
- **เมื่อถึง 0:** สาย `Remove*RandomEvent(target, outTime=true)` → `RemoveWorldEvent` + สาย `GameController$$ManageWorldEventOutTime` (0x7D4E90 — 826 insns): **string-dispatch** `String$$Equals` ×11 (เทียบชื่อกับ `eventOutTimeCallFuc` ของอีเวนท์) + `String$$Format` ×8 + `GenerateRandomItem` ×4 + `RoundToInt` ×4 — คือทั้งตัว dispatch และตัวทำ "ผลเสีย/ผลอัตโนมัติ" ในตัว [DISASM] · **ไม่มี call เดินปฏิทินภายใน** (grep 0 match)
- ผลต่อรอบ 6 (R6-3 = α): อายุอีเวนท์เดินตาม `ChangeDay` เท่านั้น ⇒ นาฬิกา server (host เดินวัน) เป็นตัวกำหนดอายุอีเวนท์โดยพฤติกรรมเกมเดิม — ไม่ต้อง patch เพิ่มเพื่อ α ในส่วนนี้ [INFERRED]

## Q4 — timeout handler เฉพาะ (อายุ + ผลเสียเมื่อหมด)

| handler | rva | ผลเมื่อหมดเวลา [DISASM] |
|---|---|---|
| `GameController$$ForceAttackPlayerAreaTimeOut` | 0x7A1350 | → `AutoManageForceAttackArea` — AI จัดการการบุกแทน (ผู้เล่นไม่ได้ร่วม) |
| `GameController$$FightMatchOutTime` | 0x7A06E0 | → `AutoManageFightMatch` + `GetAroundAreaHero` — จับคู่/ตัดสิน AI แทน |
| `GameController$$ForceFightMatchOutTime` | 0x7A1470 | → `AutoManageFightMatch` + `ForceData$$GetOwnHero` — บังคับจับคู่แทน |
| `GameController$$HorseMatchOutTime` | 0x7C15D0 | Sort/จัดอันดับ + `RoundToInt`/`Max` + ข้อความ — ประกาศผลแข่งโดยไม่มีผู้เล่น |
| `GameController$$AuctionOutTime` | 0x78D380 | **`HeroData$$ChangeMoney` ×2** + `RoundToInt` + `ItemData$$Name` — ประมูลปิดอัตโนมัติ **มีผลเงินเปลี่ยนมือแทนผู้เล่น** (ผลเสียตรงที่สุด) |

- อายุของอีเวนท์แต่ละชนิดที่ใช้ handler เหล่านี้ = `leftTime`/`lastTime` เดียวกันกับ Q2 (ค่ารายชนิดอยู่ใน data asset = [UNKNOWN]) · ผลเสียรวม: ถ้าหมดอายุตอนผู้เล่นออฟไลน์ อีเวนท์จะถูกตัดสินโดย AI/ระบบแทน (เงิน/ผลแข่ง/การบุก) — **ใช้เป็นข้อมูลตัดสิน "นับเฉพาะออนไลน์ (α)" ของ R6-3**

## Q5 — อีเวนท์ตามกำหนดซ้ำ (`WorldEventDataBase`)

- `WorldEventController$$ManageWorldEvent` [DISASM]: อ่าน `startTime` (0x20 → TimeData) ใช้ **`TimeData$$DeltaDay`** (วันที่เหลือถึงกำหนด) · แยกตาม `repeatType`: `cmp 0` = **Repeat** (ทุก `repeatDay` วัน — `idiv dword ptr [rcx+0x34]`), `cmp 1` = **AutoRandom** (สุ่มด้วย `GlobalData$$RandomRange`/`RandomRangeDouble` ในช่วง `repeatDayRandomRange` 0x38), อื่น = NoRepeat · เรียก `CreateWorldEvent` ×2 · `Mathf.Clamp` + `GetGameMaxDifficulty` สำหรับ difficulty
- ⇒ **ความถี่เป็น "วันเกม"** ทั้ง Repeat (ตายตัวตาม data) และ AutoRandom (สุ่มช่วง) — ที่ 720 วิ/วัน = repeatDay N → N×12 นาทีจริง [คำนวณ] · ค่ารายชนิดจาก data asset = [UNKNOWN]

## UNKNOWN + คำขอแยก (รวมกับ A1.5-2)

1. **ค่า `lastTime`/`repeatDay`/`repeatDayRandomRange` รายชนิด + ชื่อ `eventOutTimeCallFuc` ต่ออีเวนท์** — อยู่ใน game data assets (serialized) — repo ไม่มีเครื่องมือสกัด asset ⇒ เสนอคำขอแยก (เครื่องมือสกัด) หรือวัดบนจอ [RUNTIME] — กระทบเฉพาะ "อายุรายชนิด" ไม่กระทบกลไก
2. เส้นทาง RandomRange(7,15) vs (5,11) — เป็นอีเวนท์ BigMap vs AreaMap หรือชนิดอื่น = ยังไม่ได้แยก branch ใน body (สืบต่อได้จากไฟล์ body ถ้าต้องการ)
3. พฤติกรรมจริงบนเกม (เกิด/หมดอายุจริง) = RUNTIME M2 (เซสชันแยก ต้องอนุมัติ) — รายงานนี้ครอบ static เท่านั้น
