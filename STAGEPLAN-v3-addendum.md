# STAGEPLAN v3 — Addendum (ส่วนใหม่ทั้งหมดของ v3: §15–§21)

**สถานะ: rev3 (2026-10-24) — ปรับตามรีวิวผู้ใช้รอบสอง · ยังไม่เริ่มงานใด ๆ ของ ช่วง 1**

> [v3] เป้าหมาย: **C2 = "โลกเดียวกลาง server ตัดสินทุกอย่าง"** — world จำลองเดียว อยู่กับ server = โปรเซสเกมหนึ่งตัวที่ถือ state จริงทั้งหมด; ผู้เล่นทุกคน (รวม host) ขับโลกผ่าน verb ที่ผ่านการตรวจฝั่ง server เท่านั้น
> [v3] โครงสร้าง (มติรีวิว 2026-10-24 ข้อ 7): ส่วนใหม่ของ v3 อยู่ใน **ไฟล์เดียวคือไฟล์นี้** ไม่แทรกต่อท้ายเล่มเก่า — `StagePlan.txt` (v2, โฟลเดอร์เกม) ไม่ถูกแตะ · **สถานะเอกสาร (มติรีวิวรอบสอง ข้อ 10): STAGEPLAN v2/v3 ไฟล์ draft เดิม = historical drafts ไม่ใช้เป็นแหล่งสั่งงานหลัก** — ไฟล์นี้คือเอกสารทำงานของ ช่วง 1 (เล่มหลักยังใช้กับ ช่วง 0 จน S3 ปิด) · `STAGEPLAN.v3-draft.md` (rev1) เก็บเป็นประวัติเท่านั้น
> [v3] ประวัติ: rev1 = สถาปัตยกรรม H1–H7 + SP1–SP6 + งบ verb v1 + การตรวจฝั่ง server · rev2 = +H8, +SP0, +SP7, SP5 เกณฑ์สัมบูรณ์, +§15.4, +§18.3, +§21 · **rev3 (รีวิวรอบสอง, 10 ข้อ) = SP0 คลุม Battle/Plot/Choose/Craft/Auction + ห้ามตัดสินวิธีแก้ (คิว/worker) ล่วงหน้า, SP7 ห้ามตั้ง sect state เอง, SP5 แยก overhead/capacity test + แยกแกน clients×SessionActor, §18.1 เพิ่ม schema/validator ราย verb, §18.3 ห้าม broadcast hidden world state, §15.3/§15.4 ตามมติ Q2 (S5 prerequisite + requirement 3 ข้อห้ามลบ), §19 +สถานะเอกสาร + เช็คลิสต์ prerequisite, Q1–Q6 ปรับกรอบ + §21 บันทึกมติที่ได้รับ**
> [v3] diff: rev1→rev2 = `STAGEPLAN-v3-addendum.diff` (ประวัติ) · rev2→rev3 = `STAGEPLAN-v3-addendum-rev3.diff`
> [v3] **ห้ามเริ่มงาน ช่วง 1 จน (ก) S3 ปิด (ข) §21 ครบทุกข้อที่ยังเปิด (ค) ผู้ใช้อนุมัติ**

---

## 0A. [v3] จุดเชื่อมกับเล่มหลัก — apply ต่อเมื่อ v3 ได้อนุมัติ (ตอนนี้ยังไม่ apply)

เมื่ออนุมัติ ให้แก้ `StagePlan.txt` **เฉพาะ 3 จุดต่อไปนี้** (นอกนั้นห้ามแตะ):

1. **หัวไฟล์** — แทรกบล็อก `[v3]` หลังบรรทัด title: "เพิ่ม ช่วง 1 = เป้าหมาย C2 (โลกเดียวกลาง server ตัดสินทุกอย่าง) — รายละเอียดทั้งหมดอยู่ที่ `STAGEPLAN-v3-addendum.md` (§15–§21) ใน mod repo"
2. **§12.1 ลำดับ** — แทรกลำดับ ช่วง 1 หลังลำดับเดิม พร้อมกติกา:

```
S3 --gate--> SP0 singleton census (อ่านอย่างเดียว) --gate--> SP1 puppet hero --gate--> SP2 world clock
     --gate--> SP3 headless server --gate--> SP4 puppet client --gate--> SP5 scale/ลด NPC
     --gate--> SP6 battle worker --gate--> SP7 spawn+sect join --gate--> มติ "C2 จริงหรือไม่" (tag checkpoint-C2)
```

- ช่วง 0 (S1.5→S3) คงเดิมทุกตัวอักษร — ห้ามแทรก spike หรืองานใดของ ช่วง 1 ก่อน/ระหว่าง stage ของ ช่วง 0 (รวมถึงห้ามสำรวจ JIT ของ ช่วง 1 แทรกใน ช่วง 0)
- SP0 รันก่อน SP1 เสมอ · spike ไหนจบ NO-GO ห้ามวิ่งไปตัวถัดไปเอง — หยุดรายงานและรอมติผู้ใช้ก่อน · แต่ละ spike ต้องมีแผนย่อยราย stage อนุมัติก่อนเปิดเกมเสมอ
3. **§12.2 changelog** — เพิ่มบรรทัด: "- **v3 (2026-10-24, ร่าง rev3):** เพิ่ม ช่วง 1 = เป้าหมาย C2 (โลกเดียวกลาง server ตัดสินทุกอย่าง) — ส่วนใหม่ทั้งหมดอยู่ที่ `STAGEPLAN-v3-addendum.md` (mod repo)"

---

## 15. [v3] ช่วง 1 — เป้าหมาย C2 และสถาปัตยกรรมสมมติฐาน (ทุกข้อ GUESS จนกว่า spike จะยืนยัน)

**เป้าหมาย C2 [SPEC]:** มี **world จำลองเดียว** อยู่กับ **server = โปรเซสเกมหนึ่งตัว** ที่เป็นผู้ตัดสินทุกอย่าง (authoritative เดียว — คงหลัก §0.2 เดิม); ผู้เล่นทุกคนรวมถึง host ขับโลกด้วย **verb** (§18.1) ที่ server ตรวจแล้วรันเท่านั้น (§18.2); client ที่เหลือเป็น **puppet** — ปิดการจำลองในเครื่อง รับ state จาก server มาแสดงผล

### 15.1 หลักที่ไม่เปลี่ยนตามสถาปัตยกรรมใหม่ [SPEC]
- host-authoritative / guest ไม่จำลอง ไม่สุ่ม ไม่คำนวณผล (§0.2)
- RNG ของศึกเป็น `UnityEngine.Random` ตัวกลาง — จุดสุ่มมีที่เดียวเสมอ อยู่ฝั่งที่รัน sim
- guest ห้ามเขียนเซฟ (D5) — **ธงความขัดแย้ง (มติ Q2, §15.4 ข้อ 4 ค):** requirement "rewards กลับเซฟ guest" ขัดกับ D5 — โมเดลเซฟใหม่ตัดสินที่มติ C2; **D5 ยังบังคับเต็มรูปแบบทั้ง ช่วง 0 และ ช่วง 1** · ทดสอบบนสำเนาเสมอ (§1.2), ห้ามแตะ Steam/cheat flag (§1.7)
- ทุกการเรียก IL2CPP ผ่านคิว main thread (§1.4)

### 15.2 สถาปัตยกรรมสมมติฐาน — ห้ามใช้ตัดสินงานจริงจนกว่า spike ที่ผูกไว้จะยืนยัน

| # | สมมติฐาน | ป้าย | spike ที่ยืนยัน | สิ่งที่รู้แล้วบางส่วน |
|---|---|---|---|---|
| H1 | โปรเซสเกม (Unity player เต็ม) ใช้เป็น world server ได้ — sim เดินต่อแม้ไม่มีหน้าจอ/โฟกัส | **GUESS** | SP3 | พฤติกรรม minimize/hidden/`-batchmode` ของเกมนี้**ไม่เคยถูกวัด** |
| H2 | ผู้เล่นทุกคน = `HeroData` ในเซฟของ server (hero ของ guest เป็น HeroData/สหาย-class ในเซฟ host) | **GUESS** | SP0, SP1 | `WorldData.playerHero` เป็น singleton หรือไม่ยังเป็นคำถามวิจัยค้างของ S6 (§9 ข้อ 1) — SP0 จะนับจุดอ้างอิงทั้งหมด |
| H3 | ขับผู้เล่นด้วย **เส้นทางเดียวกับที่ AI ขับ NPC** (ผ่าน `ManageHeroMove`/`SetPlayerMoveTargetArea`) แล้วปิด AI **เฉพาะตัวนั้น** — ไม่เขียน `bigMapPos` ตรง ๆ (คงโหมด B ของ D2) | **GUESS** | SP1 | chain `BigMapController.Update → ManageAllHeroMove → ManageHeroMove` + `SetPlayerMoveTargetArea` 3 overloads มีใน anchor [DUMP, §13 ของเล่มหลัก] — ยังไม่ยืนยันว่า NPC hero วิ่งผ่านสายนี้จริง และไม่รู้ flag ปิด AI ต่อตัว |
| H4 | puppet client ปิดการจำลองในเครื่องได้จริง (หยุด AI + นาฬิกาท้องถิ่น) และแสดง state จาก server | **GUESS** | SP4 | transport/snapshot/resync/HMAC ของ S2a-3 ใช้ได้แล้ว [RUNTIME, X1–X9] — แต่เป็น state ศึก ยังไม่มี state โลก |
| H5 | battle worker = **โปรเซสแยกต่อหนึ่งศึก** | **GUESS** | SP6 | ข้อจำกัดเชิงระบบ: logic ศึกเป็น IL2CPP native ใน `GameAssembly.dll` → โปรเซสแยกที่เป็นไปได้ต้องเป็นอีก Unity player instance ไม่ใช่ .NET process ธรรมดา; `PrepareBattleMap` เรียกตรงโดยไม่โหลด world ได้หรือไม่ยังไม่รู้ [DUMP 3 overloads, §13 ของเล่มหลัก] |
| H6 | นาฬิกาโลกขับด้วย **เวลาจริงของ server**; action ที่กินเวลา (งาน/พัก/คราฟต์/เดินทาง) กลายเป็น **timer ส่วนตัวของผู้เล่น** | **GUESS** | SP2 | [RUNTIME] `GameController.ChangeDay()` เดินวันละ 1 วัน/ครั้ง, `ChangeDay(int)` ไม่ถูกเรียก, `hour` ไม่เปลี่ยนระหว่าง skip (evidence `LongYinCoop/evidence/S2cd-20261022-0710/`) · จุดเขียน `hour` ตอนเล่นปกติยังไม่เจอ (GUESS ค้างจาก S4 JIT §8 ข้อ 1 ของเล่มหลัก) |
| H7 | ลด NPC ตอนสร้างโลกได้ (gate จำนวน hero ที่โลกสร้าง) | **GUESS** | SP5 | ยังไม่มีข้อมูลใด ๆ เกี่ยวกับจุดสร้าง hero ของโลกใหม่ |
| H8a | PlotController มี **1 instance ณ จุดวัด** (heap snapshot) — **ห้ามสรุป singleton/state เดียวตลอดอายุโปรเซสจาก heap snapshot เพียงอย่างเดียว** | **RUNTIME (เฉพาะ ณ จุดวัด)** | สำรวจ 2026-10-24 (`sp6-census.md`) | heap=1 (gc_choose รอบ 5) + DUMP 1526 methods (decl) — ต้องยืนยันต่อเนื่องด้วยวิธีอื่น (แบบ counter ของ `BattleController.get_Instance`) จึงจะอ้างเป็น singleton ได้ |
| H8b | เนื้อเรื่องหลัก (story plot & dialogue choices) เป็น **flow เดียว / non-reentrant ผูกกับตัวเอก** — เนื้อเรื่องหลักมีสถานะเดียวต่อเซฟ ไม่แยกต่อผู้เล่น | **GUESS (ยังไม่วัด)** | SP0 DUMP-only (candidate + metadata ref ของ PlotController) + รอบ trace บทเรื่องจริง | modal "Plot" ใน enum ของ §8 (S4) ยืนยันว่า plot เป็น modal ที่หยุดโลก [RUNTIME บางส่วน] — แต่ "เดียว/ผูกตัวเอก" ยังไม่เคยวัด · SP0 census: call-sites = UNAVAILABLE IN DUMP-ONLY ⇒ ยังห้ามอ้าง |
| H8c | การเข้าสำนัก (เฉพาะกรณีที่ทดสอบ) เรียก `HeroData.JoinForce` ได้ใน data path · **เมธอดกลุ่ม plot ที่ติดตาม = 0 hit** — ยัง**สรุปไม่ได้**ว่า "ไม่มี PlotController ในเส้นทางทั้งหมด" เพราะ caller chain ยังไม่สมบูรณ์ | **RUNTIME (จำกัดเฉพาะกรณีที่ทดสอบ)** | สำรวจ 2026-10-25 (`sp5-sect-join.md`) | `JoinForce` ×1 จริง · `JoinForceParty`/`JoinForceServant`/`ManageNewHeroJoinForce` = **0 hit ของเมธอดที่ติดตาม** (0 hit ≠ พิสูจน์ว่าไม่มีในเส้นทาง) · caller เหนือ JoinForce ยังจับไม่ได้ (log cap) — วัด 1 ครั้งในโลก Free 1 ใบ ห้ามขยายผลเกินหลักฐาน |

**ผลของ H8 ต่อ C2 [INFER — จาก H8b ที่ยัง GUESS]:** ถ้าจริง → (1) plot หลักต้องเป็นของ server จุดเดียว — guest ได้แค่ดูผ่าน overlay หรือรอ (modal "Plot" เป็น exclusive ต่อโลก ไม่มีการเดิน/ศึกคู่ขนานระหว่างเล่น plot) (2) การเข้าสำนักของผู้เล่นที่สองถ้าต้องผ่าน plot เดียวกัน = ข้อจำกัดที่ต้องออกแบบรองรับ — **วิธีแก้ (คิว / flow แยก / วิธีอื่น) ห้ามเลือกล่วงหน้า** ตัดสินหลัง SP0 census และ SP7 วัดจริงเท่านั้น (3) มติ C2 ขั้นสุดท้ายต้องตัดสิน "เนื้อเรื่องหลักเป็นของใคร" ก่อนเปิดผู้เล่นหลายคน — SP0 ระบุ controller/state ที่เกี่ยว, SP7 วัดจริง

> ทุกแถวในตารางนี้ = **ห้ามเขียนโค้ด ship โดยอ้างว่าจริง** — ต้องรอ spike ตามคอลัมน์สุดท้ายยืนยันแล้วอัปเดตป้ายเป็น RUNTIME พร้อมหลักฐาน

### 15.3 ความสัมพันธ์กับ S4/S5/S6 เดิม [มติ 2026-10-24 Q2]
- v3 **ไม่แก้/ไม่ยกเลิก** ข้อความ §7 (S5), §8 (S4), §9 (S6) ของเล่มหลัก — เอกสารคงไว้ตามตัวอักษร
- **S5 คงเป็น prerequisite ของ networked gameplay** — ไม่ยุบ ไม่ข้าม · ชื่อ stage S4/S6 เดิม**ยกเลิกได้เฉพาะหลัง map deliverables ของมันเข้า ช่วง 1 ครบ** และ**ห้ามลบ requirement 3 ข้อ** (§15.4 ข้อ 4)
- งานวัดนาฬิกาที่เหลือ (เดินในเมือง / พักโรงแรม / งานคราฟต์ — carry-over ที่เดิมชี้ "JIT ของ S4") **ย้ายเป้าหมายเป็น JIT ของ SP2 แล้ว** (มติ Q2)
- SP0 (census) ขยายงานสำรวจ singleton ของ S6 (§9 ข้อ 1/4) แต่รันก่อน SP1, อ่านอย่างเดียว และมีรูปแบบตาราง/วิธีตรวจตายตัว

### 15.4 ข้อกำหนดเชิงออกแบบของ C2 [SPEC]
1. **ผู้เล่นใหม่เริ่มต้นแบบไม่มีสำนัก** — hero ของผู้เล่นทุกคนถือกำเนิดในเซฟของ server โดยไม่มีสำนัก; การเข้าสำนักเป็นการตัดสินของผู้เล่นผ่าน verb `world.join_sect` (งบ **v1.5** — SP7) ไม่ใช่ผลของการสร้าง hero
2. **PvP = ขอบเขตหลัง (ออกแบบเผื่อ ยังไม่ทำ)** — C2 ขั้นแรกเป็น co-op PvE เท่านั้น; ข้อกำหนดกันโกงเผื่อ PvP อยู่ที่ §18.3
3. การเข้าสำนักผ่าน plot flow เดียวของ H8b ได้หรือไม่ — ตัดสินจากผลของ SP7 (H8c ชี้ว่ากรณีที่สังเกตเป็น data path)
4. **Requirement ที่ห้ามลบ (มติ 2026-10-24 Q2 — มรดกของ S4/S6 เดิม):** (ก) **guest-owned character** — hero ของ guest เป็นของ guest คุมผ่าน verb ของ session ตัวเอง (ข) **ฝึก/ทำงานใน world ของ host ได้** (ค) **rewards ที่ได้ต้องกลับไปเซฟของ guest** — **ข้อสังเกตบังคับ [INFER]: (ค) ขัดกับ D5 ของเล่มหลัก ("guest ห้าม save ตลอดไป") โดยตรง** — การถือ requirement นี้ = โมเดลเซฟของ C2 ต้องปรับในอนาคต (guest มีเซฟของตัวเอง ขณะที่ world อยู่กับ server) รายละเอียด (จุดตัด rewards, การ sync สองเซฟ) ตัดสินที่มติ C2 หลัง SP ครบ — **ไม่ตัดสินใน ช่วง 1 และ D5 ยังบังคับ ช่วง 0 เต็มรูปแบบ**

## 16. [v3] Spikes ช่วง 1 — SP0–SP7

**กติกาใช้ร่วมของทุก spike [SPEC]:**
1. แผนย่อยราย spike (เป้าหมาย/ลำดับ/exit/วิธีวัด) ต้องอนุมัติก่อนเปิดเกมเสมอ — §16 นี้เป็นกรอบเท่านั้น
2. บังคับใช้ §1 (กติการ่วมของเล่มหลัก) เต็มรูปแบบ: สำเนาเซฟ §1.2, main thread §1.4, rollback §1.5, denylist §1.7
3. **งบพยายาม: ≤2 รอบวัดต่อ exit ข้อ** — หมดงบแล้วหยุดและรายงานตามจริง ห้ามขยับเกณฑ์เอง (ธรรมเนียมเดียวกับมติ X5 2026-10-24)
4. ปิดทุก spike ด้วย: รายงานผลวัดเทียบ exit ทีละข้อ + hash เซฟก่อน/หลัง + `git tag stage-SPn` (mod repo) + หลักฐาน `evidence/SPn-<timestamp>/`
5. **NO-GO ไม่ใช่ความล้มเหลวของโปรเจกต์** — เป็นผลวัดที่ต้องรายงานตรงแล้วหยุดรอมติผู้ใช้
6. สิ่งที่ทุก spike **ไม่ทำ**: ไม่แตะเซฟจริง, ไม่แตะ Steam/anti-cheat/flag `cheat*`, ไม่เรียก `GameDataController.Save/Load`, ไม่เขียน field ของเกมตรง (ดู §17), ห้ามเปิดเกมเอง (ผู้ใช้เปิดเอง), ห้ามเปิด IPC client อื่นระหว่างวัด
7. **ลำดับรันตามหมายเลข SP0→SP7** — **SP0 รันก่อน SP1 เสมอ** (census อ่านอย่างเดียว); ลำดับเต็มอยู่ที่ §0A ข้อ 2; การสลับลำดับต้องมีมติก่อน
8. dependency แบบหลวม: SP4 อ้างผลของ SP1 · SP5 อ้าง SP1+SP3 · SP6 อ้าง SP3 · SP7 อ้าง SP0+SP1 และตอบ H8b/H8c

### SP0 — singleton census (สำมะโน singleton, อ่านอย่างเดียว — รันก่อน SP1)

**เป้าหมาย:** ก่อนลงทุน SP1–SP7 — สำรวจ (อ่านอย่างเดียว) รายชื่อ controller/UI/state ที่สมมติ "ผู้เล่นเดียวต่อโปรเซส" และจำแนกรายตัวว่า (ก) ใช้ร่วมหลายผู้เล่นพร้อมกัน**ไม่ได้** (ต้องแยก/รื้อ) (ข) ผูกกับ hero ปัจจุบันแต่**แยกต่อ hero ได้** (ค) เป็นกลาง — พร้อมวิธีตรวจรายตัว เพื่อให้ SP1–SP7 รู้ล่วงหน้าว่าต้องแวะที่ไหน และเป็นข้อมูลของมติ C2 รวม

**ขอบเขตสำรวจ (เป้าหมายที่ต้องคลุมให้เจอ — ไม่ใช่รายการปิด):** `WorldData.playerHero` (singleton ค้างของ S6 §9 ข้อ 1) · `HudController` (ผูกปาร์ตี้เดียว) · `GameController`/`BigMapController`/`BattleController` (state เดียวต่อโปรเซส) · modal stack (`SureMenu.pause`, `WorkingUIController.*`, plot/dialogue controller — ผูกกับ H8) · camera/UI binding · จุด input (ปุ่ม/เมาส์ → ยูนิตใด) · **ชุดแรกที่ต้องคลุม (มติรีวิวรอบสอง ข้อ 1): Battle / Plot / Choose / Craft / Auction** พร้อม controller/UI ที่เกี่ยวข้องทั้งหมดของแต่ละระบบ · อะไรที่ census เจอเพิ่มระหว่างทาง

**วิธีตรวจรายตัว (จำแนก 3 ชั้น — ระบุชั้นต่อแถวในตารางผล):**
- ชั้น A (static/singleton accessor) — ตรวจจาก dump + interop reflection (นับจุดอ้างอิง) **ไม่ต้องเปิดเกม**
- ชั้น B (UI/state ผูก hero ปัจจุบัน) — ตรวจด้วยการอ่านค่าขณะสลับ hero ผ่าน UI ของเกมเอง (อ่านอย่างเดียว, บนสำเนา)
- ชั้น C (พฤติกรรม global) — probe runtime แบบอ่านอย่างเดียว (ห้ามเรียก method เปลี่ยน state) และ/หรือเหตุการณ์จริงผ่าน UI เกม (เช่น เปิด modal 2 ชนิดซ้อน) เพื่อดูว่า controller ตัวไหนล้ม/ทับ/ปิดกัน

**Deliverable:** ตาราง census `evidence/SP0-<ts>/singleton-census.md` — คอลัมน์: ชื่อ type/field | ระบบ (Battle/Plot/Choose/Craft/Auction/world/other) | ชั้น A/B/C | จำนวนจุดอ้างอิง | วิธีตรวจ | ผลตรวจ | ป้ายหลักฐาน (RUNTIME/DUMP/GUESS) | ผลต่อ C2 (ใช้ร่วมได้ / แยกต่อ hero ได้ / ต้องแยกเชิงโครงสร้าง / บล็อกเกินแก้)

**กติกาตายตัวของ SP0:** **ห้ามตัดสินล่วงหน้า** ว่า singleton ที่พบจะแก้ด้วย **คิว (queue)** หรือ **worker process** (หรือวิธีอื่น) — SP0 วัด/จำแนก/นับจุดอ้างอิงเท่านั้น; การเลือกวิธีแก้เป็นมติของผู้ใช้หลัง SP0 ส่ง census แล้วเท่านั้น

**Exit criteria (วัดได้):**
1. ชั้น A ครบ 100% ของ singleton accessor ที่หาได้จาก dump (นิยาม "หาได้" ระบุชัดในแผนย่อย เช่น static field/accessor ของ type ที่ชื่อมี Controller/UI/Manager/Singleton + ทุก static field ชนิด class เกม)
2. ทุกแถวมีวิธีตรวจ + ป้ายหลักฐานครบ — แถวไหนยังไม่วัดติดแท็ก "ยังไม่วัด" ชัดเจน **ห้ามเดา**
3. ทุกตัวที่ตัดสินว่า "ใช้ร่วมไม่ได้" ชั้น B/C ต้องมีหลักฐาน runtime ≥1 ครั้ง
4. สรุปได้ทีละ spike ว่า "singleton ตัวไหนบล็อกอะไรของ SP1–SP7"
5. **ชุด Battle/Plot/Choose/Craft/Auction ถูกคลุมครบในตาราง** — แต่ละระบบมีแถว controller/UI อย่างน้อย 1 แถว (หรือบันทึก "ไม่พบ singleton" พร้อมวิธีค้นที่ใช้)
6. ไม่มี ERROR/Exception; savehash เซฟจริงไม่เปลี่ยน (census อ่านอย่างเดียว ทำบนสำเนา)

**GO/NO-GO:** GO = exit ครบ · NO-GO = dump/interop ให้ข้อมูลไม่พอทำ census ชั้น A ให้จบ → ความมั่นใจของ H2 ตก ต้องหยุดรายงานและรอมติผู้ใช้ก่อน SP1 (census อ่านอย่างเดียว ไม่มีทางทำเกม/เซฟพัง — NO-GO ที่นี่คือ "ข้อมูลไม่พอ" ไม่ใช่ความเสียหาย) · ถ้า census พบ singleton ที่ "บล็อกเกินแก้" (เช่น `playerHero` ถูกอ้างทั้งโปรเจกต์โดยไม่มี seam) — ไม่ใช่ NO-GO ของ SP0 แต่เป็นผลวัดที่ต้องหยุดรายงานและรอมติก่อนต่อ (อาจลดขอบเขต C2)

**ความเสี่ยง:** ขอบเขตบานปลาย (census ยาว) — จำกัดจำนวน session ตามงบ · การสลับ hero ผ่าน UI กระตุ้น state อื่น (ทำบนสำเนา) · สรุป GO จากหลักฐานบาง — ป้ายทุกแถว

**Rollback:** ไม่มี artifact ในโฟลเดอร์เกมนอกจาก log/pref ของ mod · ลบ evidence ได้ทันที · restore สำเนา

### SP1 — puppet hero (หุ่นเชิดฮีโร่)

**เป้าหมาย:** ยืนยัน H2+H3 — บน host (เกมมีหน้าจอปกติ, เซฟสำเนา) โค้ด mod สั่ง `HeroData` ตัวที่**ไม่ใช่** `playerHero` (สหาย/ศิษย์/hero ชั้นรอง) ให้เดินทางไป area เป้าหมายผ่าน path เดียวกับ AI และ**ปิด AI เฉพาะตัวนั้น**ได้ โดยไม่กระทบ hero ตัวอื่น

**Just-in-time (สำรวจก่อนเขียน อ่านอย่างเดียว):**
1. enumerate hero ทั้งหมดที่ `ManageAllHeroMove` วน — จำแนก `playerHero` / hero ประเภทอื่น (class/flag — ใช้ census ของ SP0 ช่วยระบุ)
2. หา flag หรือ method ที่เป็น "ก๊อก AI ต่อตัว" (ชื่อจริงยังไม่รู้ — GUESS): จุดตัดสินใจเลือกเป้าหมาย, เดินเอง, หยุดเอง
3. ทดสอบบนจอ: สั่ง target ทับของ AI ตอน AI เปิด/ปิด — พฤติกรรมต่างกันไหม
4. ทางเข้า encounter ระหว่าง puppet เดิน (เกิดศึก = บันทึกพฤติกรรม ไปตัดสินต่อใน SP6/มติ)

**ขอบเขตการเขียน:** อ่านทุกอย่างผ่าน interop แบบมีชนิด · Harmony **postfix log-only** บน `ManageHeroMove`/`SetPlayerMoveTargetArea` · เรียก method เปลี่ยน state = **เฉพาะ** `SetPlayerMoveTargetArea` กับ puppet hero + method ปิด AI ต่อตัวที่ JIT เจอ (ต้องระบุชัดและอนุมัติในแผนย่อยก่อนใช้) · **ห้ามเขียน `bigMapPos` ตรง ๆ** (คงโหมด B ของ D2) · gate ทุก write ด้วย pref ใหม่ `coop.c2.enabled` (default false)

**Exit criteria (วัดได้):**
1. สั่ง puppet hero เดินไป area เป้าหมาย 10 รอบ → ถึงเป้า ≥9/10 (ตำแหน่งเทียบ UI บนจอ)
2. ระหว่างเดิน: hook ที่จุดตัดสินใจ AI ของตัวนั้น **ยิง 0 ครั้ง** (หรือถูก gate ทุกครั้ง) ตลอดการเดิน — ถ้าปิด AI ต่อตัวทำไม่ได้ ให้รายงานกลไกจริงที่เจอ (ถือเป็นผลวัด ไม่ใช่ความล้มเหลวของการทดสอบ)
3. hero อื่น + ผู้เล่นหลักทำงานปกติ; จำนวน hero ก่อน/หลังเท่ากัน; soft-lock 0 ใน 10 min
4. Δmean frame time A-B-A เทียบไม่สั่ง ≤ margin ของ `S2FrameRule` (Δmean ≤ 1.0 ms — ตัวชี้วัดเดียวกับมติ X5 2026-10-24)
5. ERROR=0, Exception=0, `commandsAccepted` ตรงจำนวนครั้งที่สั่ง
6. savehash: เซฟจริง 0 ไฟล์เปลี่ยน; สำเนาบันทึกก่อน/หลัง

**GO/NO-GO:** GO = ข้อ 1+2 ผ่าน และข้อ 3–6 ไม่มี FAIL · NO-GO = ปิด AI ต่อตัวไม่ได้เลย หรือการขับทำให้ระบบ hero พัง → หยุดรายงานและรอมติผู้ใช้ (ทางเลือกที่จะเสนอ: ขับผ่าน input จริงของ UI แทน AI path = เปลี่ยนสถาปัตยกรรม)

**ความเสี่ยง:** AI ถูกเรียกจากหลาย entry (ไม่ใช่แค่ `ManageAllHeroMove`) · ตำแหน่งกลางทางถูก persist ลงเซฟ · encounter ทับการเดิน · ปิด AI แล้ว hero ค้าง · การ persist อาจเขียน hero ตัวอื่นด้วย (ต้อง diff ทั้ง hero list)

**Rollback:** `coop.c2.enabled=false` → ทุก hook no-op · `.dll.off` · restore สำเนา · tag

### SP2 — world clock (แผนที่ writer ของ hour/day + caller การเดินวัน)

**เป้าหมาย:** สร้างตาราง writer ทั้งหมดของ `hour/day/worldTime` และ caller ของการเดินวันที่**วัดได้จริง** แล้วตอบว่า gate นาฬิกาได้จาก choke point เดียวหรือรายการสั้น — พื้นฐานของ H6 (นาฬิกาขับด้วยเวลาจริง + timer ส่วนตัว) และรับงาน carry-over นาฬิกา (เดินในเมือง/พักโรงแรม/คราฟต์ — ย้ายมาแล้วตามมติ Q2)

**Just-in-time:**
1. hook log-only ครอบ writer ที่รู้แล้ว: `ChangeDay()` ทั้ง 2 overload (ตัว 0-param ยิงจริง [RUNTIME]) + `TimeData.DeltaDay` ถ้าหาเจอ
2. สแกนหาจุดเขียน `hour` ตอนเล่นปกติ (ค้างจาก S4 JIT §8 ข้อ 1 ของเล่มหลัก — GUESS)
3. ทำ action ที่กินเวลาครบชุดบนจอ: เดินในเมือง, พักโรงแรม, งาน, คราฟต์, ปุ่ม `>>` และอย่างอื่นที่จอบอกว่าเวลาเดิน — จับ Δhour/Δday ต่อ action เทียบตัวเลขจอ

**ขอบเขตการเขียน:** **log-only ทั้ง spike — ไม่มี write path เลย** (postfix/prefix อ่าน argument + ค่าเท่านั้น) · probe แบบ transient ใช้กติกา probe ของ §1.1

**Exit criteria (วัดได้):**
1. ตาราง writer ครบต่อชุดทดสอบ: ทุก action ที่ทำให้เวลาเปลี่ยนบนจอ ถูกจับโดย hook ≥1 ตัว (catch-all 100% ของ action ที่เห็น Δ) พร้อม hit-count ต่อ action
2. "1 call `ChangeDay()` = 1 วัน" ยืนยันซ้ำบน ≥3 กรณีใหม่ และบันทึกทุกกรณีที่ไม่เป็น 1:1 (ห้ามเหมารวมจากงาน 5/10 วันเดิม)
3. เสนอ choke point ได้: รายการ writer ปลายทางที่ต้อง gate **≤3 จุด** (หรือรายงานตรงว่าไม่มี พร้อมจำนวนจริง)
4. ตอบ Δhour ต่อ action ครบ 3 งาน carry-over (เดินในเมือง/โรงแรม/คราฟต์) ตรงตัวเลขจอ
5. ERROR=0 · Δmean frame ≤ margin · savehash เซฟจริงไม่เปลี่ยน

**GO/NO-GO:** GO = ข้อ 1+3 · NO-GO = writer กระจัดกระจาย >3 จุดและไม่มีจุดร่วม → H6 แบบ "ขับเองที่เดียว" ทำไม่ได้ ต้องออกแบบนาฬิกาใหม่ — หยุดรายงานและรอมติผู้ใช้

**ความเสี่ยง:** writer เป็น field write ตรง (ไม่ผ่าน method) จับยาก · hook hot path ทำ frame หนัก (mitigate: log เฉพาะเมื่อค่าเปลี่ยน) · writer ตอน load เซฟต้องแยก context ออกจาก writer ตอนเล่น

**Rollback:** flag → no-op · `.dll.off` · restore สำเนา

### SP3 — headless server

**เป้าหมาย:** ยืนยัน H1 — world sim เดินต่อโดยไม่ต้องมีหน้าจอ/โฟกัส: ทดสอบ 3 แนวทาง (window minimize / hidden window / `-batchmode -nographics`) วัด sim-rate เทียบหน้าต่างปกติ เพื่อเลือกวิธีรัน world server จริงภายหลัง

**ขอบเขตการเขียน:** ไม่มีโค้ด mod ใหม่ที่จำเป็น (ใช้ build + sampler เดิม) · ไฟล์ใหม่เฉพาะ launch script ใน `tools/` (นอกโฟลเดอร์เกม) · **ไม่แตะ Steam ทุกทาง** — การ launch ตรง exe ทำให้ Steam ไม่นับเวลาเล่น = บันทึกเป็นข้อเท็จจริง ไม่ใช่การแตะ Steam (รอมติ Q4 ก่อนรันแนว launch ตรง exe) · **ทุกแนว launch = สมมติฐาน (H1)** ต้องพิสูจน์กับเกมจริงบนสำเนาเท่านั้น (มติ Q4); ทุกครั้งที่มี >1 instance ต้องแยก Save/UserData ต่อ instance (วิธีตาม SP4)

**Exit criteria (วัดได้):**
1. อย่างน้อย 1 แนวทาง: sim เดินครบ 10 min ต่อเนื่อง (ΔworldTime > 0, puppet ของ SP1 ถึงเป้าหมายได้, ไม่มี pause แอบแฝง — ตรวจด้วย `battleTime`/`turnIndex`/state ไม่ใช่ `battlePaused`/`timeScale` ตามข้อเท็จจริงของ S1.5)
2. sim-rate เทียบหน้าต่างปกติ: Δmean frame ≤ margin เดิม **และ** sim-rate ≥ 90% (นิยาม SPEC ก่อนวัด — ต่ำกว่า = FAIL ตรง)
3. เกมออกสะอาด (exit code 0), ERROR=0, ไม่มี artifact ค้างในโฟลเดอร์เกม
4. ถ้า `-batchmode` พัง/ค้าง — เก็บ crash log แล้วตัดแนวทางนั้นทิ้ง (ไม่ใช่ NO-GO ของ spike ตราบใดที่แนวอื่นผ่าน)

**GO/NO-GO:** GO = ข้อ 1+2 ด้วยแนวใดแนวหนึ่ง · NO-GO = ทุกแนว sim ค้างหรือช้า >10% แก้ไม่ได้ → C2 ต้องรัน server แบบมีหน้าต่าง (ยอมรับได้ไหม = §20 Q4)

**ความเสี่ยง:** logic ขับด้วย frame แทนเวลา (vsync ตก → sim ช้าลง) — GUESS ต้องวัด · เกมใช้ UI/render หนัก อาจพังใน batchmode · hidden window อาจโดน OS throttle timer

**Rollback:** ลบ launch script · ปิดเกมปกติ · restore สำเนา

### SP4 — puppet client

**เป้าหมาย:** ยืนยัน H4 — instance เกมที่สอง (บนเครื่องเดียวก่อน, loopback): (ก) ปิดการจำลองในเครื่องได้จริง (AI + นาฬิกาท้องถิ่นนิ่ง) (ข) รับ state โลกจาก server มาแสดงผลทาง overlay ให้ตรง server — **ยังไม่เขียน game object ฝั่ง guest** (puppet แบบเต็มเป็นงานหลังจบ ช่วง 1)

**Just-in-time:**
1. sandbox สอง instance: เกมอ่าน path เซฟคงที่ [GUESS — `GameDataController.GetSaveDataPath`, DUMP §13 ของเล่มหลัก] → ทดสอบวิธีแยกโฟลเดอร์เซฟ + `UserData` ของ instance ที่สอง (junction / สลับโฟลเดอร์ / อย่างอื่น) — พิสูจน์ก่อนรัน 2 instance พร้อมกันเสมอ (บนสำเนาเท่านั้น — มติ Q4)
2. นิยาม snapshot โลกขั้นต่ำ: hero list (id, bigMapPos, area), calendar, modal state (ต่อยอด enum ของ §8)
3. ปิด sim ฝั่ง guest: หาจุด gate AI+clock ท้องถิ่น (ใช้ผล SP1/SP2)
4. ตารางเทียบ **Tailscale vs ZeroTier** ตาม 3 เกณฑ์ (มติ Q3): access control · การตั้งค่าสำหรับเพื่อน · การจำกัดพอร์ต — ส่งเป็นหลักฐาน**ก่อนเลือกใช้จริง** (ทำหลัง SP4 loopback ผ่าน ก่อนขยายออกนอกเครื่อง)

**ขอบเขตการเขียน:** server: snapshot โลกใหม่แบบ**อ่านอย่างเดียว** · guest: ห้ามเขียน field เกมทุกจุด — แสดงผลทาง overlay/IMGUI เหมือน S2 · transport ต่อยอด TcpTransport + HMAC + resync ของ S2a-3 [RUNTIME] · คำสั่งขาขึ้นยังไม่มี (SP4 เป็น one-way state)

**Exit criteria (วัดได้):**
1. sandbox: instance ที่สองไม่เขียนโฟลเดอร์เซฟของ server (watch hash นิ่ง 10 min) และไม่เขียนเซฟตัวเองเลย
2. guest ปิด sim ท้องถิ่น: hook AI+clock ฝั่ง guest ยิง 0 ครั้ง (หรือ gate ทุกครั้ง) ตลอด 10 min
3. overlay แสดงตำแหน่ง hero + calendar ตรง server ทุก snapshot; latency p95 ≤ 500 ms (loopback — ค่าเริ่ม SPEC ตั้งก่อนวัด); gap/resync เดินตามกลไกเดิม (ทดสอบที่ 1 Hz และ 5 Hz)
4. server: Δmean frame ≤ margin ขณะมี client ต่อ (วิธีวัดเดียวกับ X5/X6)
5. ตารางเทียบ VPN ครบ 3 เกณฑ์พร้อมข้อเสนอ (ยังไม่เลือก — รอมติ)
6. ERROR=0 ทั้งสองฝั่ง; disconnect/reconnect กลางทาง 1 ครั้ง — ไม่ค้างทั้งสองฝั่ง

**GO/NO-GO:** GO = ข้อ 1–3 · NO-GO = ปิด sim ท้องถิ่นไม่ได้ (guest ยังขับโลกตัวเอง) → puppet แบบเต็มตายที่นี่ หยุดรายงานและรอมติผู้ใช้ (ทางเลือก: viewer-only ตาม S2/S4 เดิม)

**ความเสี่ยง:** สอง instance ชนเซฟ/Steam user (ความเสี่ยง #6 ของเล่มหลัก รุนแรงขึ้น) · ML `UserData` ชนกัน · state โลกใหญ่ (หลาย hero) ทำ frame/bandwidth ตก · sandbox พังเงียบ ๆ แล้วเขียนเซฟจริง (mitigate: watch hash ทุก run)

**Rollback:** ปิด instance ที่สอง · flag · restore สำเนา · ลบ sandbox

### SP5 — capacity ของโลกเดียว + ลด NPC ตอนสร้างโลก (10/30/100 หุ่น)

**เป้าหมาย:** ยืนยัน H7 (ลด NPC ตอนสร้างโลกได้) และวัด capacity ของโลกเดียว — **แยก overhead test ออกจาก capacity test** และแยกสองแกนของ workload ให้ชัด: **จำนวน SessionActor** (หุ่นที่โลกจำลอง: 10/30/100) กับ **จำนวน connected clients** (1/2/…) (มติรีวิวรอบสอง ข้อ 3) — ตอบคำถาม scale ค้าง (§11.1 ข้อ 3; ข้อจำกัด X2: 9 units < 60)

**Just-in-time:**
1. หาจุดสร้าง hero/NPC ช่วง new game (GUESS — ไม่มีข้อมูลเดิม; ประสานกับผล SP0)
2. sandbox โลกทดลอง: เซฟใหม่แยกจาก canonical ทั้งก้อน — **อนุญาตแล้วในหลักการ (มติ Q6) โดยทำบนสำเนาเท่านั้น**
3. วิธีนับ hero ที่ตรวจสอบได้ (interop + เทียบ UI ตัวอย่าง)
4. นิยาม workload คงที่ของ Phase O (จำนวน SessionActor + clients + อัตรา snapshot ที่ตั้งตายตลอดรอบ)

**ขอบเขตการเขียน:** hook จุดสร้าง world (หาจุด = log-only; gate จำนวน = เขียนได้**เฉพาะบนเซฟโลกทดลองใหม่**และต้องผ่าน `coop.c2.enabled`) · ห้ามแตะ canonical baseline ทุกทาง (watch hash ทุก run)

**แบ่งเป็น 2 เฟส [มติรีวิวรอบสอง ข้อ 3]:**
- **Phase O — overhead test:** workload **คงที่** ตลอดรอบ (ค่าเริ่มที่เสนอ: 10 SessionActor + 1 client, snapshot 1 Hz — ตั้งตายในแผนย่อย) วัด overhead ของ mod ด้วย A-B-A (mod เปิด/ปิด) → Δmean frame ≤ margin ของ `S2FrameRule` (Δmean ≤ 1.0 ms) — ต้องทำ**ก่อน** Phase C เสมอ เป็น baseline
- **Phase C — capacity test:** เพิ่ม load **ทีละแกน** (รอบหนึ่งเปลี่ยนจำนวน SessionActor เท่านั้น หรือจำนวน connected clients เท่านั้น — ห้ามเปลี่ยนสองแกนพร้อมกัน) แล้ววัดเทียบ**เกณฑ์สัมบูรณ์**: (a) sim-rate เทียบเวลาจริง ≥ [ค่าที่อนุมัติ] (b) frame p95 ≤ [ค่าที่อนุมัติ] (c) snapshot encode p99 ≤ 4 ms (คงเกณฑ์เดิมของ S2a-2 exit 2) — **ห้ามรัน Phase C ก่อนค่า (a)/(b) ได้รับอนุมัติจากผู้ใช้** (ค่าเริ่มที่เสนอ: sim-rate ≥ 90% · frame p95 ≤ 33.3 ms — รอ Q6)

**Exit criteria (วัดได้):**
1. โลกทดลอง 10/30/100 หุ่นสร้างได้: จำนวน hero ตรงเป้า ±0 ทุกขนาด
2. Phase O: ที่ workload คงที่ Δmean frame (mod เปิด/ปิด, A-B-A) ≤ margin เดิม
3. Phase C: ทุกจุดที่วัด (SessionActor × clients — แยกแกน) ผ่านเกณฑ์สัมบูรณ์ที่อนุมัติแล้ว + encode p99 ≤ 4 ms — รายงานเป็นตารางแยกแกน
4. bandwidth ต่อแกน (จำนวนหุ่น / จำนวน clients) ที่ 1 Hz และ 5 Hz — รายงาน observational
5. sim ที่จุด load สูงสุดเดิน 10 min: soft-lock 0, ERROR=0
6. canonical baseline ไม่ถูกแตะ (hash ตรงทุก baseline หลังจบ)

**GO/NO-GO:** GO = ข้อ 1+2+3 (ด้วยค่าที่อนุมัติแล้ว) · GO-with-limitation = ผ่านถึง 30 หุ่นแต่ 100 FAIL (C2 ทำได้ที่ 30) — ตัดสินต่อที่ Q6 · NO-GO = หาจุด hook สร้างโลกไม่เจอเลย

**ความเสี่ยง:** จุดสร้าง hero กระจายหลายที่ · 100 หุ่น crash/พังเชิงระบบ · จำนวน clients × SessionActor โตเกินเวลาทดสอบ (mitigate: จำกัดจุดวัดตามงบ ≤2 รอบ/จุด) · sandbox พังแล้วเขียน canonical (mitigate: watch hash + save-split ก่อนทุก run)

**Rollback:** ลบเซฟโลกทดลอง · flag · `.dll.off`

### SP6 — battle worker (โปรเซสแยกต่อหนึ่งศึก)

**เป้าหมาย:** พิสูจน์/หักล้าง H5 — worker (instance Unity player แยก หรือแนวทางที่ Q5 อนุมัติ) รัน**ศึกเดียว**เต็มวงจรแบบอัตโนมัติ (ทุกเทิร์นสั่งผ่าน IPC ผ่าน validator) ได้ และวัด lifecycle/determinism/latency เพื่อตัดสินว่า C2 แยกศึกออกจาก world server ได้จริงไหม — **MVP ใช้ 1 active battle ต่อ authoritative world จนกว่า concurrent battle จะถูกพิสูจน์ (มติ Q5); SP6 คือการพิสูจน์ทาง ไม่ใช่การเปิด concurrent ใน ช่วง 1**

**Just-in-time:**
1. worker เข้าศึกโดยไม่โหลด world เต็มได้ไหม — เรียก `PrepareBattleMap` ตรง [DUMP 3 overloads, §13 ของเล่มหลัก] บน instance ที่โหลดแค่หน้าเปล่า
2. seed ของ `UnityEngine.Random` ตั้งได้ไหม → ผล 2 รอบ seed เดียวเหมือนกันไหม (determinism)
3. worker อยู่ใน sandbox เซฟตัวเองได้ไหม (ต่อยอด sandbox ของ SP4)

**ขอบเขตการเขียน:** worker = instance แยก + sandbox เซฟเฉพาะตัว · คำสั่งผ่าน validator เดิมของ S3 ขยายเป็น auto-play (`battle.move`/`battle.attack`/`battle.pass` ตาม §18.1) · ห้ามแตะ Steam · ห้ามเขียนเซฟฝั่ง world server · **ระหว่าง ช่วง 1 มีได้แค่ 1 active battle ต่อ world เสมอ (มติ Q5)**

**Exit criteria (วัดได้):**
1. lifecycle ครบ 3 รอบ: สร้าง worker → รันศึกจบ → ทำลาย worker — ไม่มี orphan process, บันทึก memory/exit code ได้
2. ศึกจบตามปกติ 3/3 (จบด้วยเงื่อนไขของเกมเอง ไม่ force)
3. latency คำสั่งเทิร์น p95 ≤ 200 ms (loopback — ค่าเริ่ม SPEC ตั้งก่อนวัด)
4. determinism: ถ้า seed ตั้งได้ — ผล 2 รอบ seed เดียวตรงกัน (hash battle log); ถ้าตั้งไม่ได้ = รายงาน NOT-RUN + ประเมินผลต่อสถาปัตยกรรม (worker ต้องเป็นจุดสุ่มเดียว — ตรงโมเดล C2 อยู่แล้ว อาจไม่บล็อก)
5. world server ระหว่าง worker ทำงาน: Δmean frame ≤ margin, ERROR=0

**GO/NO-GO:** GO = ข้อ 1–3 · NO-GO = worker รันศึกไม่ได้ในทุกแนวที่ Q5 อนุญาต → fallback ตาม Q5 (เสนอ: ศึกอยู่ใน world server = โมเดล S3 ขยาย)

**ความเสี่ยง:** สูงสุดของชุด — IL2CPP native + Unity runtime + world context อาจทำให้ "โปรเซสแยก" เป็นไปไม่ได้เลย · ต้นทุนเครื่อง (เกมเต็ม 2 ตัวพร้อมกัน) · NO-GO ที่นี่**ไม่**ทำ C2 พังทั้งแผน — เปลี่ยนเป็น "ศึกใน server" เท่านั้น (Q5)

**Rollback:** ปิด worker · ลบ sandbox · flag · `.dll.off`

### SP7 — player spawn + sect join (ผู้เล่นใหม่เริ่มไม่มีสำนัก)

**เป้าหมาย:** ยืนยัน H2 ในทิศ "ผู้เล่นใหม่" ตาม §15.4 ข้อ 1 — สร้าง `HeroData` ใหม่ในเซฟของ server ที่**เริ่มแบบไม่มีสำนัก** แล้วไล่ flow เข้าสำนัก (sect join) ของเกมจริงว่าผ่าน plot/บทสนทนาไหน (ตอบ H8) และร่าง verb `world.join_sect` เป็นผู้สมัครงบ **v1.5** — **เสนอเข้างบได้หลัง SP7 ผ่านเท่านั้น** ไม่ใส่งบ v1

**Just-in-time:**
1. จุดสร้าง `HeroData` ที่เกมใช้เอง (new game / recruit / อื่น ๆ — ผลจาก SP0/SP5 ช่วยระบุ) และค่าเริ่มของ hero "ยังไม่มีสำนัก" ที่เกมคาดหวัง (ทรัพยากร/ตำแหน่ง/ความสัมพันธ์ — ระบุให้ครบก่อนสร้าง)
2. ไล่ flow เข้าสำนัก: scene/dialogue/plot controller ที่รับผิดชอบ (อ้าง census ของ SP0 ชั้น plot — H8b/H8c) ลงถึง method ที่เปลี่ยน sect จริง
3. สิ่งที่ flow นี้ผูกกับ `playerHero` singleton หรือทำต่อ hero ใดก็ได้ (ใช้ผล SP0)
4. การ persist: hero ใหม่ + ผลของ sect join ลงเซฟถูกต้องไหม (save/load ครบ)

**ขอบเขตการเขียน:** ส่วน mapping flow = log-only · การสร้าง hero = อนุญาต**เฉพาะผ่าน method ของเกมที่สร้าง hero อยู่แล้ว** (ถ้ามี) บนสำเนา/โลกทดลอง — **ห้ามเขียน field ตรง** · **ห้ามตั้ง sect state เอง** — การเปลี่ยนสำนักเกิดได้เฉพาะผ่านกติกา/flow ของเกมที่ SP7 ยืนยันแล้วและอนุมัติในแผนย่อย (มติรีวิวรอบสอง ข้อ 2) — ถ้า flow ไม่ยืนยันได้ = ไม่มีทางเปลี่ยนสำนักใน ช่วง 1 · ขั้น flow ที่ mod สั่งเองได้ = เฉพาะที่แผนย่อยระบุชัด (ที่เหลือผู้ใช้กดผ่าน UI เกมเอง) · gate `coop.c2.enabled`

**Exit criteria (วัดได้):**
1. สร้าง hero ใหม่ "ไม่มีสำนัก" ≥3 ครั้งบนสำเนา/โลกทดลอง — ค่าเริ่มชุดเดียวกันทุกครั้ง (จำแนกค่า "ไม่มีสำนัก" ตามที่เกมใช้) และ persist ผ่าน save/load ครบ
2. แผนผัง flow เข้าสำนักครบ: plot/dialogue → method ที่เปลี่ยน sect — ทุกขั้นมีหลักฐาน RUNTIME (hook log-only) ≥1 รอบ
3. ตอบตรง ๆ ว่า flow นี้ใช้กับ hero ที่ไม่ใช่ตัวเอกได้จริงหรือถูกบล็อกโดย singleton ใด — อ้าง census ของ SP0 เป็นหลักฐานประกอบ
4. ร่าง validator + precondition ของ `world.join_sect` (ตรวจฝั่ง server: hero ยังไม่มีสำนัก, plot state ถูกต้อง, modal ปิด, ownership) — เป็นเอกสารเสนอ ยังไม่ใส่งบ
5. เซฟจริงไม่ถูกแตะ; ERROR=0

**GO/NO-GO:** GO = ข้อ 1+2 และข้อ 3 ตอบได้ (แม้คำตอบคือ "ถูกบล็อก") · NO-GO = สร้าง hero ผ่าน method เกมไม่ได้เลย (ต้องเขียน field ตรง = ต้องขออนุมัติใหม่) หรือ flow เข้าสำนักผูกกับ plot เดี่ยวของตัวเอกจนแยกไม่ได้ (H8 จริงในทางบล็อก) → หยุดรายงานและรอมติผู้ใช้ (ทางเลือกที่จะเสนอ: ผู้เล่นใหม่เริ่มจากเซฟต้นแบบที่เตรียมไว้แทนการสร้างระหว่างเล่น)

**ความเสี่ยง:** สร้าง hero ครึ่ง ๆ กลาง ๆ (ข้อมูลไม่ครบทำเซฟพัง) · plot flow ไม่ reentrant (เล่นซ้ำไม่ได้) · sect join เปลี่ยน world state กว้าง (สมาชิกสำนัก/NPC ผู้ดูแล) · อิง H8 ที่ยัง GUESS

**Rollback:** restore สำเนา/ลบโลกทดลอง · flag · `.dll.off`

## 17. [v3] ตารางสิทธิ์การเขียน ช่วง 1 (เพิ่มใหม่ — ไม่แก้ตาราง §1.1 ของ S0–S6)

เรียงคอลัมน์ตามลำดับรันจริง SP0→SP7 · แถวคงแถวเดิมของ §1.1 เพื่อเทียบได้ตรง

| การกระทำ | SP0 | SP1 | SP2 | SP3 | SP4 | SP5 | SP6 | SP7 |
|---|---|---|---|---|---|---|---|---|
| อ่าน field ผ่าน Il2CppInterop แบบมีชนิด | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| อ่านด้วย raw offset/pointer | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| เรียก getter บริสุทธิ์ (ไม่เปลี่ยน state) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Harmony patch (อ่าน argument/ค่า เท่านั้น) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| เรียก method ที่เปลี่ยน state ของเกม | ❌ (อ่านอย่างเดียวทั้ง spike) | ✅ เฉพาะ `SetPlayerMoveTargetArea` + method ปิด AI ต่อตัว (อนุมัติในแผนย่อยก่อนใช้) | ❌ (log-only ทั้ง spike) | ❌ | ❌ | ✅ เฉพาะตอนสร้างโลกทดลอง (gate จำนวน hero) | ✅ เฉพาะคำสั่งศึกผ่าน validator | ✅ เฉพาะ method สร้าง hero/ขั้น flow เข้าสำนักที่แผนย่อยระบุชัด (บนสำเนา/โลกทดลอง) |
| เขียน field ของเกมเอง | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| รับ input จากเครือข่ายแล้วรัน | ❌ | ❌ (IPC ในเครื่องเท่านั้น) | ❌ | ❌ | ✅ snapshot โลก (guest อ่านอย่างเดียว) | ✅ ต่อจาก SP4 | ✅ คำสั่งศึก (verb ตาม §18.1) | ❌ (IPC ในเครื่องเท่านั้น) |
| เรียก `GameDataController.Save/Load` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| เขียนไฟล์ของ mod เอง (log/pref/capture) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Steam / online services | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

> แถว ❌ ทุกคอลัมน์ = ค่าเดียวบังคับทั้ง ช่วง 1 · SP0 = อ่านอย่างเดียวทั้ง spike (แถว 5–8 ปฏิเสธทั้งหมด) · การสำรวจ JIT (อ่านชั่วคราวนอกโค้ด ship) ยังทำได้ตามขอบเขตแถว raw offset ของ §1.1 · ห้ามเขียน `bigMapPos` ตรง ๆ ทุก spike (คงโหมด B ของ D2) · ทุก write ต้องผ่าน gate `coop.c2.enabled` (default false) อีกชั้นหนึ่ง

## 18. [v3] งบ verb v1, การตรวจฝั่ง server และกันโกงเผื่อ PvP

### 18.1 งบ verb v1 (รายการ action ที่รองรับ/ปิด) [SPEC — รออนุมัติตาม §20 Q1]

**นิยาม "งบ":** verb ที่ server ยอมรับได้ใน C2 ขั้นแรก — **deny-by-default** (มติ Q1: คงไว้ตายตัว): verb ที่ไม่อยู่ในงบ = ปฏิเสธทันที + log เหตุผล · เพิ่ม/ตัด verb ต้องมีมติ + validator เฉพาะ verb ครบก่อน และรายงาน diff ของงบในรายงาน stage ทุกครั้ง

**เปิด (ข้อเสนอ v1 — 6 verbs):**

| verb | พารามิเตอร์ | ใครใช้ได้ | อ้างอิง |
|---|---|---|---|
| `world.move.to` | areaId | เจ้าของ hero นั้น (host หรือ puppet ใดก็ได้) | ผลของ SP1 |
| `world.wait.minutes` | n | ทุก session | timer ส่วนตัวของผู้เล่น (H6) |
| `time.skipDay` | — | **host เท่านั้น (มติ Q1 ตายตัว)** | [RUNTIME 2 กรณี] 1 call `ChangeDay()` = 1 วัน — ห้ามเหมารวม (SP2 จะยืนยันเพิ่ม) |
| `battle.move` | unitId, cell | เจ้าของ unit ที่ถึงตา (D3 ยังบังคับ) | ต่อยอด validator ของ S3 (§6 ของเล่มหลัก) |
| `battle.attack` | unitId, targetId | เจ้าของ unit ที่ถึงตา | เดียวกัน |
| `battle.pass` | unitId | เจ้าของ unit ที่ถึงตา | เดียวกัน |

**schema/validator ร่างต่อ verb (มติ Q1: ต้องระบุครบทั้ง 6 ก่อนอนุมัติ — ตรวจฝั่ง server ทุกคำสั่งก่อนรัน):**

| verb | schema (พารามิเตอร์) | validator ฝั่ง server (ร่าง) |
|---|---|---|
| `world.move.to` | `areaId: int ≥ 0` ที่มีอยู่จริงในแผนที่ | ownership hero ของ session · hero ไม่อยู่ศึก/modal · area ปลายทางช่วยได้ (reachable ตามเกม) · rate ต่อ session (ค่าตั้งในแผนย่อย) |
| `world.wait.minutes` | `n: int 1..N` (เพดาน N เสนอในแผนย่อย) | ownership · ไม่อยู่ศึก/modal · ผูก timer ส่วนตัว (H6) — ยกเลิกได้ด้วย action ใหม่ของ session เดียวกัน |
| `time.skipDay` | (ไม่มีพารามิเตอร์) | session = host เท่านั้น (มติ Q1) · ไม่อยู่ศึก/modal · [RUNTIME 2 กรณี] 1 call `ChangeDay()` = 1 วัน — SP2 ต้องยืนยันเพิ่มก่อนเปิดจริง |
| `battle.move` | `unitId: int`, `cell: {x,y}` ในกริด | ownership unit ของ session · `nowActiveState = WaitPlayerInput` · unit นั้นถึงตา (D3) · cell อยู่ในช่วงที่เกมรับ (ใช้ `attackRangeGridUnits` เป็นตัวทำนายการรับคลิก — [RUNTIME 13/13] จาก S1.5) |
| `battle.attack` | `unitId: int`, `targetId: int` | เดียวกับ `battle.move` + target อยู่ใน range ของ unit · target ยังมีชีวิต (`BattleUnit.IsAlive` — [RUNTIME] ตัวตัดสินเดียวที่ใช้ได้ ห้ามใช้ `dead`/hp) |
| `battle.pass` | `unitId: int` | ownership + unit ถึงตา (D3) |

**ไม่อยู่ในงบ v1 (ผู้สมัครงบ v1.5):** `world.join_sect` (SP7) — **รอ SP7 ผ่าน** (มติ Q1) แล้วจึงเสนอเข้างบพร้อม validator ของมันเอง

**ปิดตลอด v1 (deny ทุกคำสั่งในกลุ่มนี้ ไม่มีข้อยกเว้น):** `save.*` · `trade.*` · `item.*` (ใช้/ทิ้ง/ซื้อขาย) · `recruit.*` · `skill.*` · `equipment.*` · `steam.*` / สถิติ / ความสำเร็จ · `pref.*` (ยกเว้น `set_pref` ที่ต้อง hardening ตามงาน S2a-4 ค้างก่อน) · ทุก verb ที่แตะ `APIDispatcher` / `DnsChecker` / `CheatCheck` / flag `cheat*` — ตาม denylist §1.7 ซึ่งยังบังคับ

### 18.2 การตรวจฝั่ง server (ไม่ไว้ใจ client) [SPEC]

1. **deny-by-default** — whitelist ต่อ session จากงบ §18.1; verb นอกงบปฏิเสธทันที
2. **ownership** — server จับ map session→heroId; verb ที่อ้าง unit/hero ต้องเป็น puppet ของ session นั้นเท่านั้น (ห้ามสั่ง hero ของคนอื่น รวมถึงของ host) — ผูกกับตาราง account→heroId ของ §18.3 ข้อ 3
3. **precondition ตรวจที่ server เท่านั้นก่อนรัน** — ศึก: `nowActiveState`/เทิร์นของ unit ตาม D3 (REJECT ประเภทเดิม) · โลก: modal ที่หยุดโลก (enum ของ §8), hero ยังมีชีวิต, ไม่อยู่ระหว่างศึก · เงื่อนไขทรัพยากรที่ verb ต้องการ
4. **rate + replay** — rate limit ต่อ verb ต่อ session (ค่าตั้งก่อนวัดในแผนย่อยของ spike ที่เกี่ยว) · ต่อยอด seq + resync ของ S2a-3 เป็นตัวต้าน replay [RUNTIME]
5. **identity** — secret ต่อ client + HMAC ทุก envelope + `fieldHash` ต่อ field ที่ verb แตะ (ต่อยอด X7 — RUNTIME: DENY hmac/fieldHash ≤ 0.3 s)
6. **choke point เดียว** — ทุก write ผ่านคิว main thread (§1.4) และผ่านลำดับ guard ก่อนสั่งแบบเดียวกับ S1.5 plan §3
7. **audit** — log accepted/denied ทุกฉบับพร้อมเหตุผล + ตัวนับ `commandsAccepted` (ต่อยอดของ S2a-3)
8. **เซฟ** — guest/worker ไม่มีสิทธิ์เขียนเซฟทุกเส้นทาง (D5); server **ไม่เคย**ใช้ข้อความจาก client เป็นความจริงของ state — client ส่งได้แค่ intent

### 18.3 กันโกงเผื่อ PvP [SPEC — ออกแบบเผื่อ ยังไม่ทำ]

C2 ขั้นแรกเป็น co-op PvE (§15.4 ข้อ 2) — ข้อต่อไปนี้ไม่ได้ implement ใน ช่วง 1 ยกเว้นข้อที่ระบุ แต่โครงสร้าง/protocol ที่เขียนใน ช่วง 1 **ห้ามออกแบบในทางที่ปิดทาง** ข้อเหล่านี้:

1. **per-player interest management — ห้าม broadcast hidden world state**: server ส่งให้แต่ละ client เฉพาะ state ที่ผู้รับควรเห็น — รูปแบบ envelope ต้องมีจุด filter รายผู้รับ (ใน MVP โลก co-op ยังไม่มี hidden state จึงส่งเต็มได้ แต่ protocol ห้ามออกแบบแบบ "ทุก client ได้ทั้งโลกเสมอ" จนแก้อนาคตไม่ได้)
2. **ศึก** — **server-authoritative RNG: จุดสุ่มเดียวที่ server** คงหลัก §0.2 — server คำนวณผลทุกอย่าง client แค่แสดง · **turn timer**: เทิร์นมีเพดานเวลา (ค่าเสนอภายหลัง อนุมัติที่แผนย่อยก่อนใช้) พ้นเวลา = ระบบตัดสินแทน (pass/ยกเลิก) · **กติกา AFK/หลุดการเชื่อมต่อ**: หลุดตรงเทิร์นตัวเอง = grace period สั้นแล้ว auto-pass/ยกเลิกตามมติ · **กติกา battle-join (ใครเข้าศึกได้)**: ศึกมีเจ้าของ + รายชื่อผู้เข้าร่วมที่ server พิจารณาเอง — ไม่รับคำว่า "ฉันอยู่ศึกนี้" จาก client
3. **ตาราง account→heroId (server-side binding)** — server ถือ mapping player/session → heroId เดียวต่อ account เก็บฝั่ง server เท่านั้น (ห้ามให้ client ประกาศตัวเอง); สลับ/แอบอ้าง hero อื่น = DENY — **บังคับตั้งแต่ ช่วง 1** (เป็นรากของ ownership §18.2 ข้อ 2)
4. **ห้ามไว้ใจ client integrity** — ไม่มีการตรวจ "client ไม่ถูกแก้" เป็นมาตรการความปลอดภัย (client แก้ได้เสมอ) — ความน่าเชื่อถือทั้งหมดอยู่ที่ server validation; การตรวจเวอร์ชัน client ทำเพื่อ compat เท่านั้น — **บังคับตั้งแต่ ช่วง 1**
5. **audit ของ accepted/denied verbs** — log ผูก account ทุก accepted/denied พร้อมเหตุผล (ต่อยอด §18.2 ข้อ 7) + คำสั่ง server-side แบน/ปลด secret ที่ทำงานถาวรและเช็คก่อน handshake — ทำเมื่อเริ่มเปิดคนนอกวงเพื่อน
6. **payload limit + rate limit ก่อน parse** — ตรวจ cap **ก่อน decode เสมอ**: frame เกินเพดานขนาด (ค่าเดิม ≤ 1100 B ของ §1.6 — ปรับได้ตาม protocol) → ตัดการเชื่อมต่อทันทีไม่ parse; เกิน rate ต่อ connection → ตัด — กัน client พัง/โกงยิงขยะ — **บังคับตั้งแต่ ช่วง 1**
7. **พอร์ตผ่าน VPN เสมือน LAN เท่านั้น (ใช้จริงใน MVP)** — bind ตาม §1.6/D6: TCP บน interface VPN (Tailscale/ZeroTier — เลือกตาม Q3) เท่านั้น; **ห้ามเปิดพอร์ตตรงสู่อินเทอร์เน็ต / bind 0.0.0.0 / UPnP-port-forward ทุกชนิด** — **บังคับตั้งแต่ ช่วง 1** (denylist §1.7 คงอยู่)

> สรุปผลบังคับใน ช่วง 1: ข้อ 3, 4, 6, 7 (เป็นส่วนหนึ่งของ §18.2/§1.6 อยู่แล้วบางส่วน) · ข้อ 1 ออกแบบ protocol เผื่อไว้ · ข้อ 2, 5 ทำเมื่อเริ่ม PvP (ต้องมีแผนเฉพาะ + มติก่อน)

## 19. [v3] กติกาที่ไม่เปลี่ยน (ยืนยันซ้ำใน v3)

1. **โปรโตคอลสำเนาเซฟ §1.2 บังคับเต็มรูปแบบทุก spike** — ทดสอบบนสำเนาเท่านั้น, `SaveSlot*` 0 ไฟล์เปลี่ยน, `PlayerprefData.dat` volatile ตาม D10
2. **กติกาทุกข้อใน AGENTS.md ยังเหนือกว่าเล่มนี้** — ทำทีละ stage ตามแผนและอนุมัติก่อนเริ่ม, ป้ายหลักฐาน RUNTIME/DUMP/GUESS/SPEC, IL2CPP main thread, rollback ทุก stage, ห้ามแตะ Steam/anti-cheat, เทียบ hash เกมทุก session — ถ้าเล่มนี้ขัดกับ AGENTS.md ให้หยุดแล้วรายงาน
3. **ตารางสิทธิ์ §1.1 ของ S0–S6 ไม่เปลี่ยน** — §17 เป็นตารางใหม่สำหรับ ช่วง 1 เท่านั้น
4. **ห้ามเริ่มงานใด ๆ ของ ช่วง 1** (โค้ด/probe/สร้างไฟล์ในโฟลเดอร์เกม) จนกว่า (ก) S3 ปิดตาม exit criteria ของ v2 (ข) §21 ครบทุกข้อที่ยังเปิด**และผู้ใช้อนุมัติ** (ค) แผนย่อยราย spike อนุมัติ
5. ข้อความ §0–§14 ของเล่มหลักคงเป็น v2 เดิม — v3 ไม่แก้; ความขัดกันระหว่าง ช่วง 1 กับ S4/S5/S6 ตัดสินที่ Q2 เท่านั้น
6. **สถานะเอกสาร [มติ 2026-10-24 ข้อ 10]:** STAGEPLAN v2/v3 ไฟล์ draft เดิม = **historical drafts ไม่ใช้เป็นแหล่งสั่งงานหลัก**; ไฟล์นี้ (addendum) = แหล่งสั่งงานของ ช่วง 1 · **ก่อนเริ่ม ช่วง 1 ต้องยืนยันด้วยเช็คลิสต์ว่า S3/C2 prerequisite ปิดครบจริง:** (ก) S3 ปิดตาม exit criteria ของเล่มหลัก + tag (ข) S2a-3 ปิด (tag `stage-S2a-3`) และ baseline เซฟอยู่ในสถานะที่อนุมัติ (ค) pref/net.cfg สถานะอนุมัติ (ง) schema/validator ของ 6 verbs ร่างครบตาม §18.1 (จ) มติที่ยังเปิดของ §21 ครบ

## 20. [v3] คำถามที่ต้องให้ตัดสิน (6 ข้อ — ห้ามเริ่ม ช่วง 1 จนได้คำตอบครบ)

| # | คำถาม | สถานะมติ/ค่าเริ่มที่เสนอ |
|---|---|---|
| Q1 | งบ verb v1: อนุมัติ 6 verbs **พร้อม schema/validator ครบทั้ง 6** (§18.1) หรือไม่? | กรอบมติแล้ว (2026-10-24): deny-by-default คงอยู่ · `time.skipDay` = host-only · `world.join_sect` รอ SP7 — **คงเปิด: อนุมัติรายการ verb + schema/validator** |
| Q2 | ตำแหน่ง S4/S5/S6 เดิมเมื่อมี ช่วง 1 | **มติแล้ว (2026-10-24):** S5 = prerequisite ของ networked gameplay · วัด clock → SP2 · ยกเลิกชื่อ S4/S6 ได้หลัง map deliverables · ห้ามลบ requirement 3 ข้อ (§15.4 ข้อ 4) — คงเปิด: จุดตัด rewards/โมเดลสองเซฟ (ตัดสินที่มติ C2) |
| Q3 | topology SP4/SP6 + เลือก VPN | **กรอบมติแล้ว (2026-10-24):** loopback ก่อน → VPN ภายหลัง · ต้องมีตารางเทียบ Tailscale/ZeroTier (access control · ตั้งค่าสำหรับเพื่อน · จำกัดพอร์ต) ก่อนเลือกใช้จริง — คงเปิด: เลือกค่ายไหน (หลัง SP4 ส่งตาราง) + เครื่อง guest คืออะไร (§11.1) |
| Q4 | แนว launch ของ SP3/SP4 | **มติแล้ว (2026-10-24):** direct exe / multi-instance / `-batchmode -nographics` = **สมมติฐานของ SP3/SP4 เท่านั้น** — ต้องทดสอบกับเกมจริง แยก Save/UserData ต่อ instance บนสำเนาเท่านั้น |
| Q5 | รูปแบบศึกของ MVP | **มติแล้ว (2026-10-24):** MVP = **1 active battle ต่อ authoritative world** จนกว่า concurrent battle จะถูกพิสูจน์ — SP6 คือการพิสูจน์ทาง; fallback ถ้า worker ไม่ได้: (a) ศึกใน world server (โมเดล S3 ขยาย) (b) หยุด C2 battle ไว้ก่อน (c) แนวอื่น (ระบุ) |
| Q6 | เซฟโลกทดลอง + ค่าเกณฑ์ capacity | **กรอบมติแล้ว (2026-10-24):** อนุญาตเซฟโลกทดลอง**บนสำเนา** · capacity criteria แบบ absolute หลังเสนอค่าและรออนุมัติ — คงเปิด: อนุมัติค่าเมื่อ SP5 ยื่นแผนย่อย (ค่าเริ่มที่เสนอ: sim-rate ≥ 90% · frame p95 ≤ 33.3 ms) |

> คำตอบทุกข้อบันทึก**ตามถ้อยคำ**ที่ §21 — ห้ามเริ่มงาน ช่วง 1 จน §21 ครบทุกข้อที่ยังเปิดและได้อนุมัติ (ดู §21)

## 21. [v3] บันทึกมติ Q1–Q6 — **สถานะ: มติกรอบไว้แล้ว 3 ข้อ (Q2, Q4, Q5) · คงเปิด: Q1, Q3, Q6 (ส่วนย่อย)**

- **ขั้นตอนบังคับ [SPEC]:** มติที่ได้รับบันทึก**ตามถ้อยคำ**ของผู้ใช้ด้านล่าง (รับ 2026-10-24) · ข้อที่ยังเปิดรอคำตอบส่วนที่เหลือ แล้ว**หยุดรออนุมัติขั้นสุดท้าย** — ห้ามเริ่มงาน ช่วง 1 ใด ๆ (แม้ census/probe) จนผู้ใช้อนุมัติชัดเจน
- ถ้าคำตอบขัดกับข้อความอื่นในเล่ม (§15–§20, ตัวเล่มหลัก หรือ AGENTS.md) → หยุดรายงาน ไม่แก้เอง

| ข้อ | มติของผู้ใช้ (ตามถ้อยคำ — รับ 2026-10-24) | วันที่ | ผลต่อแผน | คงเปิด |
|---|---|---|---|---|
| Q1 | "คง deny-by-default และ `time.skipDay` เป็น host-only ระบุ verbs ทั้ง 6 ตัวพร้อม schema/validator ก่อนอนุมัติ `world.join_sect` รอ SP7" | 2026-10-24 | §18.1 เพิ่มตาราง schema/validator ราย verb (rev3) | อนุมัติ verb+schema/validator |
| Q2 | "คง S5 เป็น prerequisite ของ networked gameplay และย้ายการวัด clock ไป SP2 สามารถยกเลิกชื่อ stage S4/S6 แบบเก่าได้หลัง mapping deliverables แต่ห้ามลบ requirement เรื่อง Guest-owned character, การฝึก/ทำงานใน world ของ Host และการนำ rewards กลับไปเซฟ Guest" | 2026-10-24 | §15.3/§15.4 อัปเดต (rev3) — ธงขัดกับ D5 บันทึกไว้ | จุดตัด rewards/โมเดลสองเซฟ (ที่มติ C2) |
| Q3 | "loopback ก่อน แล้ว VPN ภายหลัง ให้เปรียบเทียบ Tailscale/ZeroTier จาก access control, การตั้งค่าสำหรับเพื่อน และการจำกัดพอร์ต ก่อนเลือกใช้จริง" | 2026-10-24 | SP4 JIT ข้อ 4 + exit ข้อ 5 (rev3) | เลือกค่าย VPN หลังเห็นตาราง |
| Q4 | "direct executable, multi-instance และ `-batchmode -nographics` เป็นสมมติฐานสำหรับ SP3/SP4 เท่านั้น ต้องทดสอบกับเกมจริงและแยก Save/UserData ของแต่ละ instance บนสำเนาเท่านั้น" | 2026-10-24 | SP3/SP4 ขอบเขตอัปเดต (rev3) | — (ปิดแล้ว) |
| Q5 | "MVP ใช้หนึ่ง active battle ต่อ authoritative world จนกว่าจะพิสูจน์ concurrent battle ได้" | 2026-10-24 | SP6 เป้าหมาย/ขอบเขตอัปเดต (rev3) — fallback (a)/(b)/(c) ยังรอถ้า NO-GO | fallback ของ SP6 (เฉพาะถ้า NO-GO) |
| Q6 | "อนุญาตเซฟโลกทดลองบนสำเนา และใช้ capacity criteria แบบ absolute หลังเสนอค่าและรออนุมัติ" | 2026-10-24 | SP5 แยก 2 เฟส (rev3) | อนุมัติค่า sim-rate/frame p95 เมื่อ SP5 ยื่นแผนย่อย |

> สรุปคงเปิดอยู่: **Q1** (อนุมัติ verb+schema/validator) · **Q3** (เลือกค่าย VPN หลังตารางเทียบ) · **Q6** (อนุมัติค่า capacity) — ทั้งหมดอนุมัติได้ทีละจุดตามแผนย่อย ไม่ต้องรอพร้อมกัน

---

> [v3] จบ addendum — ไฟล์นี้คือส่วนใหม่ทั้งหมดของ v3 (rev3) · เล่มหลัก `StagePlan.txt` (v2) ไม่ถูกแตะ · ยังไม่เริ่มงาน ช่วง 1
