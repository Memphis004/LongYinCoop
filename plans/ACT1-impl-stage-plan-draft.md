## ACT-1 implement — แผน stage (DRAFT รออนุมัติ — **ยังไม่ได้รับอนุมัติ ห้ามเริ่มโค้ด**)

- **วันที่ร่าง:** 2026-10-31 · **อ้างมติ:** รอบ 4 (CLOCK-1 §9 / NPC-1-ACT-1 §6) + **รอบ 5** (NPC-1-ACT-1 §10 · CLOCK-1 §10) · **ที่มา:** คำสั่งผู้ใช้ "อัปเดตแผน implement §9 ตามมติ แล้วเสนอแผน stage มาขออนุมัติ"
- **สถานะ:** เอกสารเท่านั้น — ไม่แตะโค้ด/เกม/เซฟ · ต้องผ่าน Gate (§5) ก่อนเขียนโค้ด · ห้ามรวมเซสชันเกมกับสายงานอื่น (กติกา 4 สายงาน)

### 1) ขอบเขต

**ใน:** งาน AFK 4 ตระกูล (ฝึกวิชา · ขุดแร่ · ตัดไม้ · ผลิตในอาคาร) — ตัวเลือกเป็น**วินาทีจริง 5 ค่า** (30/60/120/180/300 วิ แสดง ครึ่ง/1/2/3/5 ชั่วยาม) · host นับ progress (authoritative) · รางวัล = config ตัวคูณ (เริ่ม **×2**) · ปฏิทินเดินเฉพาะ host ออนไลน์ (**α**) · guest แสดงผลอย่างเดียว (S2 snapshot) · ปิดปุ่ม skip/เร่งเวลาใน co-op
**นอก:** กิจกรรมอื่นทั้งหมด (คราวต์/อ่านหนังสือ/วิจัย/ตีแต่ง/Wait/QuickTravel — **คงปั๊ม native**) · ระบบศึก (P4 แยก) · NPC-1 ทั้งหมด · เควส/อีเวนท์หน้าแผนที่ (M1 แยก)

### 2) กลไกที่เสนอ (ยังไม่ pin RVA — ให้ A1 ยืนยันก่อนเขียนโค้ด)

1. **Task manager ฝั่ง mod (host):** state ต่อผู้เล่น `{owner, family, secondsTotal, elapsedHostSec, rewardSpec, worldDayAtStart}` นับด้วยเวลาจริงของ host (**ห้าม**ใช้ `battlePaused`/`Time.timeScale` — บทเรียน S1.5) · **persist ฝั่ง mod เอง** (เกมไม่เซฟ state ปั๊ม [audit §6.6])
2. **ตัดปั๊ม native ออกจากงาน AFK:** intercept/gate `WorkingUIController.StartWorking` ทุก overload (6p = 0x9CABF0) เมื่อเป็นงานใน 4 ตระกูล — ปั๊มเดิมเดินปฏิทิน N วัน (ขัดมติรอบ 4 ข้อ 3)
3. **ตัวเลือก UI:** แทน/ซ่อนปุ่มวัน (สร้างตอนรัน [RUNTIME P1-5] — ฝั่งผลิตผ่าน plot-choice/`AreaBuildingChoice`, ฝึกวิชาผ่าน `StudySkillController.SureStartStudySkill`) แสดงป้ายชั่วยาม เก็บค่าวินาที
4. **รางวัล:** จบงาน → จ่าย = วันงานเทียบเท่า (วินาที ÷ 30 × config) ผ่านเส้นทางของเกม (`AddResourceNum` / `FinishStudySkill`) · config เสนอ **`UserData\LongYinCoop\act.cfg`** flat fail-closed (แพตเทิร์นเดียวกับ `net.cfg` — เลี่ยง ML exit-rewrite) key เสนอ `rewardDaysPerShichen = 2`
5. **ปฏิทิน (α):** host เดิน `ChangeHour`/`ChangeDay` ด้วยเวลาจริงขณะออนไลน์ · **กฎกันนับซ้ำกับปั๊ม native ของกิจกรรมอื่น = ต้องตัดสิน** — ให้ A1 เสนอ 2 ทาง: **(i)** host-clock suspend/เลื่อน anchor ระหว่างกิจกรรมปั๊ม (ปฏิทินเดินด้วยปั๊มช่วงนั้นตามเดิม) หรือ **(ii)** host-clock เป็น driver เดียวแล้วค่อยแปลงปั๊มที่เหลือภายหลัง — **ขอมติหลัง A1**
6. **ยกเลิก:** ตาม D1 (ปุ่ม UI เท่านั้น) · **ชั่วคราวจนกว่าข้อ (ข) จะมีมติ: ยกเลิก = ไม่ได้รางวัล** (ตรงพฤติกรรม gate ของเกม — ข้อเสนอ รอผู้ใช้ยืนยันใน Gate)
7. **Guest:** อ่าน progress จาก S2 snapshot → overlay · คำสั่งใด ๆ ผ่าน validator ของ S3 เท่านั้น (หลัง S3 เปิด)
8. **ปิด skip/เร่งเวลา:** `SkipButtonClicked`/`SetSkippingState` + ปุ่ม "เร่งเวลา" ของเหมือง (มติเดิม (d) §4)

### 3) ลำดับ stage (เสนอ)

| Stage | งาน | เปิดเกม? | exit criteria หลัก |
|---|---|---|---|
| **A1** | static survey อ่านอย่างเดียว: pin จุด patch (UI ตัวเลือก / call sites ของ `StartWorking` ใน 4 ตระกูล) + ที่มาชุดตัวเลือก 1/5/10 vs 5/10/15 และ 20 วัน (**เพื่อหาจุด patch เท่านั้น**) + กลไก double-driver เสนอ 2 ทาง | ไม่ | รายงาน pin ครบ + ข้อเสนอกฎปฏิทิน — รอมติ |
| **A2** | pure logic + config loader + เทสต์ offline (mapping วินาที→รางวัล, task manager, กฎปฏิทิน α) + compile-only build | ไม่ | เทสต์เขียวครบ · build 0W/0E · artifact provenance READY |
| **A3** | deploy + split + ผู้ใช้เปิดเกม: งาน AFK จบตามวินาที, ปฏิทินเดินจริงระหว่าง task (0 day-skip), กิจกรรมนอกขอบเขตปั๊มเดิมเหมือนเดิม, ยกเลิก = ไม่ได้รางวัล (ชั่วคราว) | ใช้ (ผู้ใช้เปิดเอง) | ผลตาม expected รายข้อ · เซฟ restore EXACT + `leftovers: none` · ERROR/Exception = 0 (นอก noise เดิม) |
| **A4** | guest overlay ขยาย (ผูกหลัง S3 พร้อม) | ใช้ | แยกแผนย่อยภายหลัง |

### 4) Rollback (ทุก stage)
- pref/switch ปิดพฤติกรรมใหม่ (mod นิ่งเมื่อ pref false — fail-closed) · DLL `.prev` + artifact เก็บใน evidence · save-split protocol ครบ · `net.cfg` ไม่แตะ

### 5) Gate ก่อนเขียนโค้ด
1. **อนุมัติแผนนี้** (ผู้ใช้)
2. มติกฎกันนับซ้ำปฏิทิน (หลัง A1)
3. มติยกเลิกชั่วคราว "ไม่ได้รางวัล" (จนข้อ (ข) มีข้อมูล)
4. มติ hook ของ stage (M5-3 ครอบเฉพาะ S2a-4 — stage นี้ต้องขอใหม่)
5. ข้อ (ข) ยังเปิดไม่บล็อก A1–A2 — บล็อกเฉพาะการตัดสินรางวัลตอนยกเลิก
6. มติ (ฉ)/(ช)/(ซ)/(ฌ) จาก NPC-1-ACT-1 §11 (mapping วัน→วินาที รายหน้าจอ vs ชุด 5 ค่า · รางวัล ช-A/ช-B · งานเลี้ยงนอกขอบเขตหรือไม่ · ข้อความรางวัลบนจอ) — ข้อขัด (ฉ) กับมติรอบ 4 ข้อ 3 ต้องปิดก่อน A2

### 6) UNKNOWN
- สาเหตุ probe TimeV1 ติด 2/6 (สาย V1 — ไม่บล็อก stage นี้ แต่เป็นบทเรียน Harmony patch ได้)
- กลไก double-driver · class เจ้าของปุ่มตัวเลือก · ค่า `StudyDayCost()` จริงรายวิชา · เศรษฐกิจ 16%/วัน ของเหมือง (M1)
