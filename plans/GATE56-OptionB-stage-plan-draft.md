# GATE 5/6 STAGE PLAN (ร่าง rev 1) — Activity manager ต่อผู้เล่น ตามมติ D3 = Option B

- **label:** `TIME-G56B-draft-20261031` · **สถานะ: DRAFT — รอผู้ใช้รีวิว** · ห้ามเริ่มโค้ด/ห้าม build/deploy/ห้ามเปิดเกม จนกว่าแผนจะผ่านรีวิวและประกาศอนุมัติ
- **session ร่าง:** docs-only — ไม่เปิดเกม ไม่ attach/invoke/hook ไม่แตะเซฟ/pref/static field/`Mods\`
- **ที่มาที่บังคับ:** มติ D3 = Option B — `decisions-pending.md` §13 ระบุว่า "แผน stage ของ Gate 5/6 ต้องร่างใหม่ตาม B แล้วรีวิวผู้ใช้ก่อนโค้ด" ⇒ ไฟล์นี้คือแผนร่างดังกล่าว
- **ความสัมพันธ์กับแผนเดิม:** ทำงานใต้ `time-plan-draft.md` (ยืมหลักฐาน §B และหมายเลข Gate 5a/5b/5c/6 ตาม §F ต้นฉบับเป๊ะ) — ต่างจากต้นฉบับที่ Section 10 (P-T2..P-T7) ตั้งต้นด้วย candidate ทาง A; มติ D3 เลือกทาง B ⇒ ตารางทาง A ของเดิม = ประวัติ ไม่ใช้เป็นข้อตกลง
- **anchor ณ ร่าง (จาก git ไม่ใช่การวัดใหม่):** mod repo HEAD `0e4ec17` (หลัง commit §13) — ก่อนขั้น G1 ต้องรัน session-start check ซ้ำ (hash เกม 2 ไฟล์ + baseline) ตามกติกา repo ข้อ 9 · baseline เซฟที่อนุมัติ = `7cd024019604…` @ `coop-test/baselines/20261031-063808` [VERSION.md]
- **ป้ายหลักฐาน:** SPEC = ข้อเสนอ ยังไม่ใช่มติ · OBSERVED = อ่านจาก disasm/body/source · RUNTIME = วัดจากเกมจริง · INFERRED · UNKNOWN · NOT-RUN — ติดป้ายตามวินัย AGENTS.md

---

## 1. ขอบข่าย

### 1.1 สิ่งที่ครอบ (เมื่ออนุมัติ)

1. **host activity manager ต่อผู้เล่น** — host รวมศูนย์ตัดสิน request รายกิจกรรมรายคน; guest ไม่รัน validator (โมเดล host-authoritative เดิมของ repo [SPEC ประกาศซ้ำ])
2. **state ต่อผู้เล่น (record) + state sync** — ผู้เล่นแต่ละคนเห็นงานของตัวเองต่างกันได้; ปฏิทินโลกยังเป็น host-clock เดียว (CLOCK-1 P1)
3. **องค์ประกอบที่ Option B ต้องมี** (ร่างทั้งหมดใน §3): record per-player, คิว serialize, policy ทับแบบเกม, กติกา skip/pause, เคลียร์ `workFinished`/`skipping` เอง
4. **config ใหม่ `activity.cfg`** — flat `key = value`, fail-closed, ปฏิเสธ `[section]` (ยึดรูปแบบ `net.cfg` ที่ผ่านการใช้จริงแล้ว [RUNTIME ของ net.cfg / SPEC ของ activity.cfg])
5. **เทสต์ pure + static check + เกณฑ์ exit รายขั้น**

### 1.2 สิ่งที่ไม่ครอบ (ห้ามใส่แม้ดูเกี่ยว)

- **ACT-1 งาน AFK (ราง `rewardMultiplier`)** — สายแยกมีมติ/แผนของตัวเอง; จุดเดียวที่ร่วมกัน = **hook StartWorking 6p จุดเดียว** ที่ล็อกไว้แล้ว (`ACT1-plan-rev5.md` §R5-1.3) — ดู §3.9
- **NPC-1 / SP5-NPC (O1–O7, N-stages)** — มีมติแยกแล้ว; แผนนี้ห้ามเสนอทางที่ขัดมติ O1–O7 (เช่น ห้ามแตะ genesis snapshot ของ SP5-NPC — มติ O1 "รอผล SP5-NPC" ยังอยู่)
- **SP5 บัญชี NPC ตอนสร้างโลก** — แยกเด็ดขาด (ตาม T5/time-plan เดิม)
- **protocol จริง** — การเขียน/แก้ `S2/Protocol/S2Types.cs` ยังห้าม (ร่าง `TIME_ACTION_*` ท้าย `area-notes.md` ยังเป็นร่าง ไม่ใช่ข้อตกลง) — stage นี้เสนอ "รายการ message ที่จำเป็น" ไว้รีวิวเท่านั้น
- **งาน implement ของจริง** — ไม่เกิดในรอบนี้; ฮุกเพิ่มนอกจากจุด 6p เดิมห้ามมี (มติ ACT-1 rev 5; ถ้าจำเป็นภายหลัง = มติแยก)

---

## 2. มติตั้งต้นที่แผนนี้ผูกเป็นเกณฑ์ (ถ้อยคำเต็ม = `decisions-pending.md` §13)

| มติ | สาระต่อ stage นี้ | ตำแหน่งในแผน |
|---|---|---|
| **D3** | Option B — activity manager ต่อผู้เล่น | ตัวตั้งทั้งแผน (§3) |
| **D4** | ทับแบบเกม — งานเดิมหายแบบเงียบ [OBSERVED B.1]; งาน/ค่าใช้จ่ายตั้งต้นไม่คืนโดยกลไก; มติรอบ 5 (ข) เครดิตตามวันงานที่ครบยังยืน | §3.6 |
| **D5** | คงพฤติกรรมเกมเดิม — mod ไม่เพิ่มการดัก noCancel/forced-cancel; finish handlers ทุกกลุ่มเดินตามเกม | §3.7 |
| **D6** | สูตร (ฉ) `max(30, 12 × N)` วินาที ทุก N — ในขอบเขต ACT-1; งานเลี้ยง (มติ (ซ)) คงปั๊ม native (R6-5/R6-7) | §3.5 |
| **D7** | อนุญาต skip + mod เคลียร์/หลีกธงเอง; ตัดสินสถานะงานด้วย `working == 1` (B.2 CORRECTED) | §3.8 |
| **D8** | แยกคิวต่อผู้เล่น — รวม QuickTravel ที่ใช้ปั๊มเดียวกับงาน (T5 §11 ร่างเดิม) | §3.3–3.4 |
| **D10** | เคส slider Wait ทำได้เฉพาะหลังยืนยัน `TestMode` (Route Gate 5c) | §4 row G3 + §5.1(5) |
| **D1/D2** (มีแล้ว 2026-10-30) | ปุ่ม UI เท่านั้น / per-day credit ถาวร | §3.7 / §3.6 |

- **D9** — คง divergence แบบบันทึก + baseline ใหม่ `7cd024019604…` [มติ 2026-10-31] — ขั้น G3 จะใช้ `--expected-baseline` ไฟล์นี้ตรง ๆ
- **O1–O7 (สาย SP5-NPC)** — มีมติเป็นของตัวเอง; สาระที่ผูกต่อแผนนี้: ห้ามแตะ genesis snapshot (O1), fallback สหายสลับ AI (`SetAllAuto`) ไม่รวมใน stage นี้ (อยู่สาย NPC-1 + S3), ห้ามเสนอ action ที่ขัดมติรายข้อ

---

## 3. กรอบพฤติกรรม Option B [SPEC — ทุกหัวข้อรอมติ]

### 3.1 state ต่อผู้เล่น (record)

- โครงต่อผู้เล่นจาก T5 §11.7 [INFERRED]: `{reqId, ownerPlayer, kind, dayNum, worldDayAtStart, actorHeroID, areaID}` — ขั้น G1 เป็นผู้ pin เทียบจุดจริงลงรายงาน ก่อนแนะตารางสุดท้าย
- จับเวลาด้วย `worldTime` ไม่ใช่ `totalWorkingDay` [OBSERVED — T5 §11.7]
- mod ไม่แตะ singleton/ชุด state ของเกมโดยตรง — state mod จัดการเอง, mod เรียกจุดเริ่มงานหนึ่งงานจากคิวผ่าน hook เดียว ส่วนกลไกเดินหลังจุดเริ่ม = เกมเป็นผู้เดินเอง
- **save/load**: เกมไม่เซฟ state ปั๊ม [OBSERVED §6.6 เดิม] ⇒ งานหายเหมือนเกมเดี่ยว — Option B = mod ต้อง persist mod-state ตัวเอง; วิธี persist อยู่ในขั้น G2 (จุดเขียน state ใหม่ต้องผ่านอนุมัติ ห้ามอ้างว่าอนุมัติแล้ว)

### 3.2 host flow [SPEC]

```
request(kind, dayNum, actorHeroID, areaID, fromPlayer)
  → validator pure (สิทธิ์, dayNum กับสูตร (ฉ), pref gate เดิม, noCancel from G1 ตาราง)
  → record ใหม่เข้าคิวต่อ player (D8)
  → host ปั๊มงานหนึ่งงานต่อ single-player (จุดเดียว §3.3)
  → งานจบด้วยเงื่อนไขเกมจริง (จบจากปั๊ม native ตามเกม, ไม่ re-create ตามลำดับเอง)
  → broadcast ผล + WORKING_STATE ใหม่ [SPEC — protocol ยังไม่อนุมัติ]
```

หลักคงเดิม: **host ตัดสินเดียว, guest ไม่ validator, ทุกขั้นเริ่มเฉพาะผ่านมติ per-stage** (กติกา repo ข้อ 1)

### 3.3 เปิดงานข้ามผู้เล่น — serialize ปั๊มเดียว [SPEC — เปิดรอมติ §5.1(1)]

เกมมี set state ชุดเดียว + static rate แชร์ทั้งโปรเซส [OBSERVED — T5 §11.2] ⇒ มีงานเดินจริงได้ทีละงาน; มติ D3/D8 มีได้ 2 แนวในการเดินงาน — แผนนี้เสนอ (ก) พื้นฐาน:

- **(ก) คิว FIFO กลาง (ต่อข้อเสนอเดิมของ T5)** — งานทุกชั้นรวมคิวเดียว เรียงตามเบอร์`reqId`; งานแต่ละอันเดินเวลาจริงเมื่อถึงคิว; วันที่ผลจบง่ายตรวจสอบ — ข้อจำกัด: งานในคิวรอ ต้องมีธงที่เก็บ "วันที่จริงที่คาดจบ" ใน record ให้ UI แสดงเดาเท่านั้น
- **(ข) ยอมให้วันเดินไม่ตรงตัวผู้ขอ** — ตาม T5 §A ข้อเดิม — ยังไม่เพิ่มการประเมินเนื้อจริง [UNKNOWN] — เลือก (ข) ต้องตัด (ก) ทิ้ง (ห้ามรวมสองแนวเดียวกันเป็นข้อตกลงซ้อนกัน)

### 3.4 กติกากันชนระหว่างคิว [SPEC]

1. งานของผู้เล่นเดียวกันทับกันเองตามเกม (D4) — งานเดิมหายแบบเงียบ — มติ D4 ยอมเดิม → ปิดแล้ว; แผนไม่ทำทับข้ามผู้เล่น (แต่ละ record ถือหนึ่ง hero/ชุดงานต่อผู้เล่น — [SPEC รอมติ ณ §5.1(2)])
2. ทุก request ที่เข้า host = ผ่าน validator กลาง — ผู้เล่นสองคนแย่งปั๊มไม่ได้ระหว่างกัน เพราะคิวกลางกันชนและ serialize เดียว
3. คิวแต่ละ record มี `reqId` — dedup ตาม `reqId` (ตามโครงเดิมของ T5 P-T2) — ห้ามเปิดจุด dedup ชุดใหม่โดยไม่มีมติ
4. QuickTravel ยึดปั๊มเดียวกันเสมอ (D8) — ถ้าระหว่างที่ QuickTravel ขอมา ไม่มีงานจากคิวเดินตามกติกา default ของตัวเกมเอง [SPEC]

### 3.5 สูตร D6 + static field [SPEC — เขียน static ผ่าน Route 5b แยก]

- สูตร (ฉ) `max(30, 12×N)` วิ/งาน (ขอบเขต ACT-1); นอกขอบเขต (งานเลี้ยง ตาม (ซ)) = คงปั๊ม native (มีมติแล้ว)
- mod กำหนด static `dayTime` ของเกมให้ตรงสูตรก่อนเริ่มงาน (field เดียวกับเกม [B.6 — static dayTime แชร์อัตโนมัติ], nextDayCount เริ่ม 0 [INFERRED B.6]); ค่าเริ่มต้นคงเดิม `dayTime = 1.0` / `pauseTime = 0.25` [RUNTIME P1-5 ป้ายเดิม]
- **การเขียน static field ของเกม = ต้องผ่าน Route Gate 5b** (อ้าง `time-plan-draft.md` §E.4/§F) — แผนนี้ให้โครงเท่านั้น ไม่มีสิทธิ์เขียนเพราะช่องทางนำนี้ — ขั้น G3 ใช้เท่านั้นหลังอนุมัติ

### 3.6 ผลทับ (D4) [SPEC]

- เงื่อนไขจาก B.1 [OBSERVED โค้ด]: งานที่ถูกเขียนทับไม่มี finishFuc — งาน/ทุนที่จ่ายไปแล้ว (ค่าฝึก ค่าเดินทาง) **ไม่ถูกคืนโดยกลไก** (มติ D4 = ยอมเดิม)
- บันทึกข้อจำกัด co-op: งานที่ถูกทับจะไม่มีรางวัลต่อเนื่อง — ไม่มีกลไก refund [SPEC — รอมติ]
- มติ (ข) รอบ 5 (เครดิตตามวันงานที่ครบ) = รายงานตามจริง ไม่มีกลไก refund ใหม่ในแผนนี้ [V4 ป้าย = NOT-RUN — ห้ามอ้างว่าวัดแล้ว]

### 3.7 noCancel / forced-cancel / finish handlers (D5) [SPEC]

- จุดยกเลิก 2 path เดิม: `TryCancelWorking` (guard 2 เงื่อนไข) + `MiddleStopWork` (forced, ไม่เช็ค noCancel ซ้ำ) [OBSERVED สะท้อนใน B.3 ของ T5 เดิม]
- finish handlers ทุกกลุ่มที่ B.4 ครอบ = ผ่านจริง [OBSERVED] — จุดที่ B.4/รายงาน**ไม่ได้**รวม = ยังเปิดทุกจุดที่ไม่พบ (R2, R3 ของ T5 — ณ ปัจจุบัน NOT-RUN)
- mod เป็นผู้เรียกจุดเริ่ม (D1 — ผ่าน UI เท่านั้น) — ห้ามฝังเงื่อนไขเพิ่มในกลไกเกม

### 3.8 `workFinished` / `skipping` (D7) [SPEC]

- ข้อเท็จจริงเดิม (B.2 [OBSERVED]): path เริ่มงาน `StartWorking` (2 overload) ไม่เคลียร์ `workFinished`(0x60)/`skipping`(0x62) — flag ค้างข้ามงาน ⇒ มติ D7: mod เคลียร์/หลีกธงเอง ณ จุดเริ่มงานจากคิว [SPEC — จุดเขียน field ยังต้องอนุมัติ stage ต่อ]
- ตัดสิน "กำลังเดินจริง" ด้วย `working == 1` ตาม B.2 CORRECTED — ไม่ใช่ `working && !workFinished` [SPEC ประกาศป้ายเดิม]
- hook 6p = จาก ACT-1 rev 5 (มีมติ) — แผนนี้**ไม่เพิ่ม patch `PlotController.StartWorking(string)`** หรือจุดเดิมอื่นนอกจาก 6p; patch-count gate = `CountOwnedPatchEntries` (fix จาก `f4947b2`/`d27e281`) — set ใหม่ = มติแยก (§5.1(3))

### 3.9 hook 6p — คงเดิมจาก ACT-1 rev 5 [มีมติแล้ว]

- hook `WorkingUIController.StartWorking` (6p) **จุดเดียว** — ทั้งสาย Option B และ ACT-1 ใช้จุดเดียวกัน; ห้ามเพิ่ม patch อื่นหรือเปลี่ยนนิยามของ set เดิม; แผน G1 pin จุด patch ตามตารางที่มีมติเดิม — "จุด patch ที่เกิด" รายงาน ณ session จริงห้ามเดาก่อนรีวิว [SPEC]

### 3.10 pause gate (R1) [SPEC — V3 = NOT-RUN ณ เอกสารนี้]

- ปั๊มงานเดิมเป็น unscaled [RUNTIME §2.3 เดิม] ⇒ ตัวหยุดเวลาของเกม (timeScale/pauseTime) ไม่ครอบงานที่กำลังเดิน [SPEC จากหลักฐานเดิม]
- แผนตั้งต้นต่อ: gate ฝั่ง mod จับ pause ณ host service [SPEC] — ข้อจำกัดที่ต้องบันทึก: ยังไม่ได้วัด (V3 NOT-RUN) ว่าคำสั่งจากคิวสามารถกด pause ให้งานหยุดชั่วขณะได้จริงหรือไม่ [UNKNOWN]
- ผล V3 = NOT-RUN ณ เอกสารนี้ — ห้ามบันทึกว่า "ผ่านแล้ว" จนกว่าขั้น G3 วัดจริง [SPEC]

### 3.11 รายการ message ที่ต้องมี (รีวิว protocol — ไม่ใช่การอนุมัติโค้드)

- ที่ต้องไม่ตกหล่น: **WORKING_STATE (snapshot)** · **TIME_ACTION (start/progress/complete/cancel)** [SPEC — จาգเอกสารรีวิว/
- เป็นเอกสารรีวิวเท่านั้น — จนกว่ามีมติรีวิว protocol แยก = ยังไม่อนุมัติ protocol

---

## 4. ลำดับขั้นของ stage เมื่ออนุมัติ (ห้ามข้าม อนุมัติรายขั้นแยก)

| ขั้น | เนื้อหา | session | อนุมัติที่ต้องประกาศก่อนขั้นนี้ |
|---|---|---|---|
| **G1** | Static survey: pin จุดจริงของ record, ตาราง noCancel รายกิจกรรม (แผนสำรอง ตามเดิม T5 P-T0), หา writer `needAutoSave` (R6 [UNKNOWN]), verify skeleton ไม่พยายามคำนวณสถานะทับ | docs-only read-only | อนุมัติแผน (rev ≥2 จากผลรีวิว) + มติ 5 ประเด็น §5.1 ครบ |
| **G2** | Pure logic + config `activity.cfg` + เทสต์ pure (compile-only, ไม่ deploy) — เทียบตาม T5 P-T2 (คิว+reqId dedup); ไม่มี wiring ใน `CoopMod.cs` (จุดที่ต้องรับทราบ) | offline | อนุมัติเฉพาะขั้น; **ไม่เขียน static field ตามสูตรในขั้นนี้** |
| **G2.5** | Gate RUNTIME หลังงาน G2 — รวบรวมผล V1 ล่าสุดก่อน deploy เพื่อป้องเอกสาร; ถ้าไม่มีผล V1 ชุดที่นับได้ = **ห้าม deploy** | offline | มติเงื่อนไขขั้นนี้ |
| **G3** | Runtime — deploy DLL + save-split + preflight + **ผู้ใช้เปิดเกมเอง** — ลำดับเซฟเดิม: split → sampler → ผู้ใช้ค่อยเปิดเกม (กติกา repo) | เกมจริง **แยก session** | Gate 5b (เขียน static) + มติ deploy + มติ D10 |
| **G4** | Guest synchronization ขั้นสุดท้าย — full snapshot + WORKING_STATE ที่รีวิวแล้ว | เกมจริง (หลัง S3) | มติแยก |

ข้อบังคับทั่วไปทุกขั้น: การเรียก IL2CPP ผ่าน MainThreadDispatcher + typed interop (กติกา repo ข้อ 6/8 — ห้าม raw offset/pointer) — hook log-only จนกว่า G3 — typed read เท่านั้น (ห้ามเขียน static field นอกขอบเขต Route 5b) — patch set ผ่าน `CountOwnedPatchEntries` + อนุมัติ set ใหม่แยก — TestMode ไม่แตะ — config = integer/`key = value` flat

---

## 5. ประเด็นที่ต้องมีมติก่อนขั้น G1 [SPEC — ต้องประกาศมติรายข้อ]

1. **(1) คิวกลางข้ามผู้เล่น** — เลือก (ก) FIFO กลาง หรือ (ข) ยอมวันเดินไม่ตรง — ทาง (ก) เป็นตั้งต้นของร่างนี้
2. **(2) policy ทับระหว่างงาน** — งานในคิวที่ถูกทับ = ยอมจบเงียบตาม D4 จาก B.1 — ต้องประกาศมติยืนยันก่อน G1
3. **(3) patch set ใหม่ / patch-gate** — จุด 6p เดิมคงเดิม — เพิ่ม/เปลี่ยน expected set ของ `CountOwnedPatchEntries` ต้องมีมติ — ห้ามจับคู่ข้อ (2)
4. **(4) `activity.cfg`** — ชุด key เริ่มและค่า kill-switch (`enabled` false = ปิดเรียบ) ต้องตรวจ ณ ผู้ใช้ก่อน G1
5. **(5) สถานะ TestMode + D10** — เคส slider Wait ต้องรอ Route Gate 5c — ลง stage แยก (ห้ามรวม phase ในขั้น G3 เดียวกัน)

---

## 6. Stop / rollback (มาตราตามโครงเดิม)

- ถ้าหลักฐานใหม่ขัดมติเดิมของ stage [SPEC ที่ถือแล้ว] → **หยุดและรายงานทันที** (กติกา repo เดิม) — ไม่แก้มติลงในเอกสารนี้เอง — การปรับเปลี่ยนข้อนั้น = มติแยก
- ท้ายแต่ละขั้น (ยึดเจตนาเดิม): save-split restore + verify digest ตรง baseline ✓ D9 + rehash อิสระ 0 diff + preflight + ไม่มี orphan process + `net.cfg` ตรง `12541f34…` + kill-switch ปิดหลังใช้ — ตามเอกสารโครงเดิมแนบ
- ให้เกณฑ์ที่นับได้: ไม่ถือยกระดับ "ผ่าน" ถ้าเจอ "ทำงานได้แต่ยัง" จากจริง — รายงานมองระดับรายขั้นเสมอ

---

## 7. สิ่งที่ยังไม่รู้ / ค้าง — เสนอให้แนบป้ายตามจริง

### 7.1 ผล V1 (พักชั่วคราว — 4 เงื่อนไขกลับมา)

1. แก้ ExpectedPatchCount → **ปิดแล้ว** (fix `CountOwnedPatchEntries` + set+entries — commit `f4947b2`/`d27e281` — [INFERRED-from-code])
2. เทสต์ใหม่นับโดยเฉพาะตามโทรแมน → **มี `PatchGateHarness` (25 checks) แล้ว** (`d27e281`) — ยังต้องรันซ้ำบน build ใหม่
3. build/provenance ใหม่ → **ยังไม่ทำ**
4. มติ deploy แยก → **ยังไม่มีมติ** (ห้ามแตะ `Mods\`)

### 7.2 T(N) — ผลวัดที่มี ณ วันนี้ [RUNTIME]

- T(N) จริงที่ rate ใหม่ = **ยังไม่ได้วัด** [NOT-RUN ตามจริง] — ผล V2-A เดิม [RUNTIME ครั้งก่อน] = แบบ PROVISIONAL เท่านั้น — ใช้เป็น "อ้างอิงปั๊ม native" ตามมติ (ง) รอบ 5 — **ห้ามยอดเป็น PASS**
- V3 ปัจจุบัน = NOT-RUN — ผลทับ (V4) = NOT-RUN

### 7.3 ข้อจำกัดอื่นที่ต้องขึ้นถ้อยคำตามถูก

- Sesterce/รางวัลแบบ per-day: เครดิตแต่ละวัน = จาก record + จุดจาก native — ยังไม่วัด [UNKNOWN — รายงานตามจริง]
- Sync รายบุคคลหลังขั้น G4: ยังอยู่เฉพาะร่างรีวิว [SPEC] — ยังไม่มีผลต่อ stage นี้ตามมติเดิม

---

## 8. สถานะเอกสาร

- ไฟล์นี้ = เอกสารรีวิวเท่านั้น — ไม่มีการประกาศมติจากฝั่ง agent ทุกข้อ
- ทุกข้อ [SPEC] = ยังไม่ตัดสิน — รอผู้ใช้รีวิวรายข้อ — ประกาศมติก่อนขั้น G1 ทุกข้อ (§5.1) — ถึงตอนนั้น = **ห้ามเริ่มโค้ด**
- วลีเริ่มขั้นถัดไป (หลังรีวิวผ่าน): `เริ่ม G56-G1 session` — ต้องแยก session เกมจริง (G3) ออกชัดเช่นโครงเดิม (กติกา repo ข้อ 1 + กติกา ห้ามรวมเซสชันเกม)
