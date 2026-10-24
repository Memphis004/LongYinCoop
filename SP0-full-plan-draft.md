# SP0-full — แผนย่อยร่าง (singleton census ชั้น A ครบ + รวมหลักฐานชั้น B/C จากรอบสำรวจ)

**สถานะ: DRAFT rev2.1 — แก้ตามมติ Q1–Q6 (2026-10-25) + ตรวจเก็บรอบ 2 (เนื้อหาเดิม ไม่รีไรท์ทั้งไฟล์) · รออนุมัติผู้ใช้ ห้ามเริ่มงานจนกว่าร่างนี้จะได้รับอนุมัติ**
วันที่ร่าง: 2026-10-25 (rev1) · rev2: แก้ตามมติ Q1–Q6 · rev2.1: ตรวจเก็บรอบ 2 (ชี้แจง Q5 เป็นข้อกำหนด · exit 7 ห้ามสรุป "บล็อก" · ขอบเขตเครื่องมือ) · บันทึกมติเต็มท้ายไฟล์ · ผู้ร่าง: ผู้ช่วย (จากงานที่ผู้ใช้สั่ง "เตรียมร่างแผน SP0 เต็ม")

แหล่งอ้างอิงของร่างนี้:
- กรอบ SP0 ต้นทาง: `STAGEPLAN-v3-addendum.md` §16 SP0 (rev3 — **ไม่ถูกแก้โดยร่างนี้**)
- ผลสำรวจรอบก่อน: `sp0-sp7-notes.md` + `evidence/sp0sp7-20261024-225939/` ทั้งโฟลเดอร์
- กติกาใช้ร่วมของ spike: addendum §16 กติกา 1–8 · ตารางสิทธิ์ §17 (SP0 = อ่านอย่างเดียวทั้ง spike)

---

## 1. จุดประสงค์และขอบเขต

**จุดประสงค์:** ทำ census singleton ให้ครบในสิ่งที่ทำได้โดย **ไม่เปิดเกม** — เติมชั้น A ให้ครบ 100% ตามกฎค้นหา (**candidate types + metadata references** — call-sites อยู่นอกขอบเขตตามมติ Q2) และรวมหลักฐานชั้น B/C ที่**วัดไว้แล้ว**จากรอบ `sp0sp7-20261024-225939` เข้าตารางเดียว — เพื่อให้ SP1–SP7 รู้ล่วงหน้าว่าต้องแวะที่ไหน และเป็นข้อมูลของมติ C2

**ขอบเขตเป็น DUMP-only โดยเจตนา:**
- อ่าน `dump.cs` (`<เกม>\Il2CppDumper\dump.cs`) เป็นข้อความตรง ๆ และ/หรืออ่าน **metadata ล้วน** ของ interop assemblies (`MelonLoader/Il2CppAssemblies/*.dll`) ด้วยตัวอ่านแบบ metadata-only (เช่น `System.Reflection.Metadata` / `MetadataLoadContext`)
- **ขอบเขตเครื่องมือบังคับ (ยืนยันชัด):** **ห้าม attach เข้ากระบวนการเกม · ห้ามเปิด/โหลดโปรเซสเกม · ห้ามเรียก method, getter หรือ instantiate type ของเกมทุกชนิด** — สิ่งที่อนุญาตคือการอ่านไฟล์บนดิสก์เท่านั้น
- **ข้อจำกัด (ไม่เดา):** ถ้าภายหลังพบว่าเครื่องมือที่เลือกใช้ไม่สามารถยืนยันได้ว่าเป็นการอ่าน metadata ล้วน (เช่น ต้องโหลด assembly แบบปกติจนอาจรัน initializer ก่อนอ่านได้) — ให้ตัดส่วน interop assemblies ออกและใช้ `dump.cs` อย่างเดียว พร้อมบันทึกเป็นข้อจำกัดของรอบในรายงาน ห้ามเดาว่าปลอดภัย
- **ไม่เปิดเกม** (ไม่มี MelonLoader log ใหม่) · **ไม่เรียก method ใดของเกม** (รวมถึง pure getter ทาง runtime) · **ไม่สร้างโค้ด mod / ไม่ build / ไม่ deploy DLL**
- สิ่งที่สร้างได้: สคริปต์ช่วยแยกไฟล์ `dump.cs` (read-only parser ใน `tools/` ของ mod repo — ไม่ใช่โค้ด mod) + ตาราง census + รายงาน ใน `evidence/SP0-<ts>/`
- ชั้น B/C ที่ยังไม่มีหลักฐาน = ติดสถานะ **`ยังไม่วัด (runtime required)`** ในตาราง และระบุว่าต้องรอบ runtime ถัดไป (ซึ่งต้อง save-split/preflight + อนุมัติแยก) — ห้ามเดา

**สถานะของรอบนี้ (มติ Q3):** SP0 DUMP-only = **งานเตรียมการก่อน "ช่วง 1"** (ไม่เปิดเกม ไม่เรียก method ของเกม ไม่ build/deploy ไม่เปลี่ยน state) — **ไม่ถือเป็นการอนุมัติ "ช่วง 1" runtime investigation หรือ implementation ใด ๆ** · prerequisite ของ S3 และ §21 ตามแผนคงไว้ทุกตัว

**เหตุผลที่รอบนี้ DUMP-only:** รอบ `sp0sp7-20261024-225939` เก็บข้อมูล B/C ส่วนใหญ่ไปแล้ว (census heap=1, sect-join chain, NPC battle, creation chain); งานที่เหลือซึ่งไม่ต้องเปิดเกมคือชั้น A + จำนวนจุดอ้างอิงรายตัว (ช่องว่างที่ระบุไว้ใน `sp0-sp7-notes.md` §6 "ยังต้องทำ: ชั้น A/B/C ครบ + จำนวนจุดอ้างอิงรายตัว" — ซึ่งตามมติ Q2 แปลเป็นขอบเขตที่ทำได้จริงคือ **candidate types + metadata references** เท่านั้น)

**ความไม่สอดคล้องกับ addendum — ได้มติแล้ว (Q1, 2026-10-25):** exit 3 ของ SP0 ใน addendum (verdict "ใช้ร่วมไม่ได้" ต้องมีหลักฐาน runtime ≥1) ยังคงบังคับสำหรับ**การปิด SP0 census ทุกชั้น** — รอบ DUMP-only ปิดต่างหากด้วย verdict **`GO-DUMP-ONLY`** โดยแถว B/C ที่ยังไม่มีหลักฐานคงสถานะ `ยังไม่วัด (runtime required)` และ**ห้ามใช้ผล DUMP-only ยืนยันข้อสรุปด้าน runtime ที่ไม่มีหลักฐาน** (บันทึกมติเต็ม: หัวข้อ "บันทึกมติ Q1–Q6" ท้ายไฟล์)

## 2. Singleton census แบบครบชั้น A/B/C

ตารางรวมฐานะปัจจุบัน (มาจากรอบก่อน) → รอบ SP0-full จะเติมคอลัมน์ "ชั้น A (candidate + metadata ref)" รายแถว, ระบุ call-sites เป็น `UNAVAILABLE IN DUMP-ONLY` ทุกแถว และเติมแถวที่ขาด

| Controller / UI | ระบบ | instance ที่ยืนยันได้ | ความสัมพันธ์กับ `Player()` | Data-only path | UI dependency | หลักฐาน | ป้าย | ชั้น A (candidate + metadata ref — งานรอบนี้) |
|---|---|---|---|---|---|---|---|---|
| `GameController` | world | heap=1 (0x27bacc14a80 รอบ 5) · 244 methods | เก็บ `worldData` (`Player()` อยู่ในนี้) | สร้างโลก/hero gen = data-level (chain §4) | มี (หน้าจอเกม) แต่ chain สร้างโลกไม่ผูก UI ในที่วัดได้ | `sp6-census.md` + `sp4-creation-chain.md` | DUMP+RUNTIME | **ยังไม่นับ — งานรอบนี้** |
| `BattleController` | Battle | heap=1 · `get_Instance` hot (37,997 → +18,436/15 นาที, ptr เดิมต่อเนื่อง) · 186 methods | ฉากศึกที่มีผู้เล่น (`PrepareBattleMap` ×10 overloads) | ศึก NPC-vs-NPC **ไม่ผ่าน** (0 calls ตลอด 25+ วันเกม, 2 เหตุการณ์จริง) | ผูกฉาก UI | `sp6-census.md` + `sp7-npc-battles-partial.md` | DUMP+RUNTIME | **ยังไม่นับ** |
| `PlotController` | Plot | heap=1 (0x27bb77eb900) · **god-class 1530 methods** | จับ event/บทสนทนา | ยังไม่พิสูจน์ (`JoinForceParty`=0 ระหว่างเข้าสำนัก) | สูงตามชื่อ/ขนาด (ยังไม่ trace) | `sp6-census.md` + `sp5-sect-join.md` | DUMP+RUNTIME | **ยังไม่นับ** |
| `ChooseController` | Choose | heap=1 · 16 methods | ตัวเลือกในบท | ยังไม่พิสูจน์ | ผูก UI ตามชื่อ | `sp6-census.md` | DUMP+RUNTIME | **ยังไม่นับ** |
| `AuctionController` | Auction | heap=1 · 33 methods | งานประมูล | ยังไม่พิสูจน์ | ยังไม่พิสูจน์ | `sp6-census.md` | DUMP+RUNTIME | **ยังไม่นับ** |
| `CraftUIController` | Craft | heap=1 · 41 methods | งานคราฟต์ | ยังไม่พิสูจน์ | ผูก UI (ตามชื่อ) | `sp6-census.md` | DUMP+RUNTIME | **ยังไม่นับ** |
| `CraftPoisonUIController` | Craft | heap=1 · 21 methods | คราฟต์ยาพิษ | ยังไม่พิสูจน์ | ผูก UI (ตามชื่อ) | `sp6-census.md` | DUMP+RUNTIME | **ยังไม่นับ** |
| `WorldPlotEventController` | Plot/world | heap=1 · 9 methods | event เรื่องระดับโลก | ยังไม่พิสูจน์ | ยังไม่พิสูจน์ | `sp6-census.md` | DUMP+RUNTIME | **ยังไม่นับ** |
| `BattleAiMenuController` | Battle | heap=1 · 9 methods | เมนู AI ศึก | — | ผูก UI ตามชื่อ | `sp6-census.md` | DUMP+RUNTIME | **ยังไม่นับ** |
| `ForceGroupFightMatchChooseController` | Battle/Choose | heap=1 · 11 methods | จับคู่ยุทธ์กลุ่ม | — | ผูก UI ตามชื่อ | `sp6-census.md` | DUMP+RUNTIME | **ยังไม่นับ** |
| `BattleUnit` + 7 controller รอบ (`BattleBackground`, `BattleTeamHeroIconController`, `PlotInteractController`, `StartBattleButtonController`, `BattleAISettingController`, `BattlePrepareSpellButtonController`, `RecruitChooseToggleController`) | Battle/Plot | ❌ ไม่ใช่ singleton (DUMP) | - | - | - | `sp6-census.md` | DUMP | **ยังไม่นับ** |
| `WorldData` | world (data) | ไม่ใช่ controller singleton — container ใน `GameController.worldData`; `Player()` = instance method pure getter | จุดเข้าถึงตัวเอก (`Player()` + heroID 0 ใน dict เดียวกับ NPC) | ใช่ (เป็น data ทั้งก้อน) | ไม่มีที่พบ | `sp4-creation-chain.md` + `sp6-census.md` | DUMP+RUNTIME | **ยังไม่นับ** |
| `WorldData.playerHero` | world | **ไม่มีอยู่จริง — 0 references ใน dump** (ปิดข้อค้าง S6 §9 ข้อ 1) | ตัวเอกระบุด้วย `Player()` + heroID 0 เท่านั้น | - | - | `sp0-sp7-notes.md` §2 | DUMP | n/a (0) |
| `HudController` | UI | **ยังไม่วัด** (อยู่ในขอบเขต addendum §16: "ผูกปาร์ตี้เดียว") | สมมติผูกตัวเอก | ไม่น่าเป็น (HUD) | ผูก UI | ไม่มี — ต้องเติมชั้น A รอบนี้ | — | **ยังไม่นับ** |
| modal stack (`SureMenu.pause`, `WorkingUIController.*`, plot/dialogue controller อื่น), camera/UI binding, จุด input | UI | **ยังไม่วัด** (ระบุไว้ในขอบเขต addendum) | ตาม addendum ผูก H8 | - | ผูก UI | ไม่มี | — | **ยังไม่นับ** |

ข้อจำกัดการตีความ (ห้ามลืม): "heap=1" = ณ เวลาวัด (กลางเกม รอบ 5) — ไม่ใช่การพิสูจน์ตลอดอายุโปรเซส; ยกเว้น `BattleController.get_Instance` ที่คืน ptr เดิมต่อเนื่อง 38k+ calls = ยืนยันต่อเนื่องได้จริง

ข้อจำกัดของแหล่งข้อมูล (มติ Q2): `dump.cs` **ไม่มี method bodies ตามปกติ** — รอบนี้นับได้เฉพาะ **candidate types** (ตามกฎค้นหาใน §7.1) และ **metadata references** (field/property/parameter ที่มีชนิดเป็น type นั้นใน signature + static field/accessor ของ type นั้น) · **actual call-sites = `UNAVAILABLE IN DUMP-ONLY`** ทุกแถว — เลื่อนเป็นงานอื่น (รอบ runtime trace หรือเครื่องมืออื่นที่ต้องอนุมัติแยก) **ห้ามอ้างว่านับ actual call-sites ได้จาก `dump.cs` อย่างเดียว**

## 3. แยกข้อเท็จจริงออกจากสมมติฐาน

**ข้อเท็จจริง (มีหลักฐาน):**
- [DUMP] จำนวน method ราย controller ตามตาราง §2 · `WorldData.playerHero` ไม่มีใน dump (0 references)
- [RUNTIME] heap=1 ทุกตัวในตาราง (ณ จุดวัด) · `BattleController.get_Instance` คืน ptr เดิมต่อเนื่อง 38k+ calls
- [RUNTIME] การสร้างโลก: `GameController.StartNewGame` → `GenerateHero` → `GenerateHeroData#2` ×963 → `WorldData.GenerateHeroDict` → `AddTempHero` ×81 (แกว่ง 81–88 ตามโลก) รวม ~1.2 s — เป็น data-level ทั้ง chain ไม่พบ PlotController/UI ใน chain ที่ hook ได้ (`sp4-creation-chain.md`)
- [RUNTIME] เข้าสำนักผ่าน UI จริง 1 ครั้ง: `belongForceID −1→3`, `atAreaID −1→33`, worldTime วัน 1→2 — ผ่าน `HeroData.JoinForce` ×1 เท่านั้น; `PlotController.JoinForceParty`=0, `JoinForceServant`=0, `ManageNewHeroJoinForce`=0 (`sp5-sect-join.md`)
- [RUNTIME] ศึก NPC-vs-NPC (marker ดาบไขว้ 2 ครั้ง, วันเกม 1→25+): `PrepareBattleMap` ×10 overloads / `StartBattle` / `RunBattle` / `BattleEnd` รวม = **0 ครั้ง**, `totalFightCount`=0 ⇒ resolve ทาง world-sim data path ที่ยังระบุ method ไม่ได้ (`sp7-npc-battles-partial.md`)
- [RUNTIME] โลก Plot mode: ผู้เล่นถูก fix เข้า 仙霞派(25) ตั้งแต่เริ่ม + `openLeaveForce=false`/`openForceBuilding=false` + `worldPlotEventStartData`=5 (Free = 0) — ขัดข้อกำหนด §15.4 ข้อ 1 ของ addendum ถ้าใช้ Plot mode เป็นโลก server
- [RUNTIME] จุด save ของเกมยังหา method ไม่เจอ (`GameController.Save` ไม่มีจริง) — hash-watch ใช้แทนได้ (`sp6-census.md`)

**สมมติฐาน/การอนุมาน (ห้ามใช้ตัดสินงาน):**
- [INFER] "singleton ทุกตัว ⇒ flow ทำขนานกันไม่ได้ ต้องต่อคิว" — **ยังเป็นอนุมานจากโครงสร้างเท่านั้น** ไม่ใช่ข้อเท็จจริงที่วัด: การจะบอกว่า flow ใด "ต้องเข้าคิว" ต้องรู้ว่า flow นั้นแตะ singleton ตัวไหนจริง (call-site จริง) — ซึ่ง**เกินขอบเขต DUMP-only** (call-sites = `UNAVAILABLE IN DUMP-ONLY` ตามมติ Q2 — ต้องรอบ runtime); รอบนี้เติมได้เฉพาะ candidate + metadata references เพื่อชี้เป้าว่าต้องไปวัดอะไรต่อ
- [GUESS] Choose/Craft/Auction: มีแค่ heap=1 ยังไม่ผ่าน runtime trace ตาม flow จริง — ความมั่นใจ "ปานกลาง"
- [GUESS] จุดสร้างตัวเอก heroID 0: สร้างนอก chain ที่ hook ได้ — GUESS ว่าหน้าจอสร้างตัวละครก่อน `StartNewGame` (สอดคล้อง ctor-burst ก่อน 1.4 s) — UNKNOWN ต้องรอบ runtime ใหม่
- [UNKNOWN] กลไก pool/cache ของ hero gen (รอบ 3 = 0 calls vs รอบ 5 = 963 calls ใน process เดียวกัน, ปลายทางเหมือนกัน Heros≈883) — กระทบ SP5/H7
- [UNKNOWN] HeroData ctor 886 ครั้ง/โลก vs Heros(883)+Temp(88) ต่างกัน 85 — ยังอธิบายไม่ได้
- [UNKNOWN] caller เหนือ `HeroData.JoinForce` (log หลุด cap 2,000 จากพายุ `GenerateHeroData`) — hook v4 ring-buffer ติดแล้วรอรอบหน้า

**กติกาตายตัว (จาก addendum §16 SP0 + มติ Q1 2026-10-25):** ห้ามใช้การมี singleton เพียงอย่างเดียวสรุปว่าทุก flow ต้องต่อคิว — SP0 วัด/จำแนก/นับจุดอ้างอิงเท่านั้น · และ**ห้ามใช้ผล DUMP-only ยืนยันข้อสรุปด้าน runtime ที่ไม่มีหลักฐาน** (verdict เกี่ยวกับพฤติกรรม runtime ต้องมีหลักฐาน RUNTIME จริงเท่านั้น)

## 4. ข้อเสนอถ้อยคำ H8a/H8b/H8c (แทน H8 เดิม)

อ้างร่าง diff ใน `sp0-sp7-notes.md` §6 — ถ้อยคำที่เสนอสำหรับตาราง §15.2 ของ addendum (ยังไม่แก้ไฟล์จริง):

```
- | H8 | plot/บทสนทนา เป็น flow เดี่ยวผูกกับตัวเอก — เนื้อเรื่องหลักมีสถานะเดียวต่อเซฟ ไม่แยกต่อผู้เล่น | GUESS | SP0 + SP7 | modal "Plot" ใน enum ของ §8 ยืนยันว่า plot เป็น modal ที่หยุดโลก |
+ | H8a | PlotController เป็น singleton ต่อโปรเซส (state เดียว) | RUNTIME | สำรวจ 2026-10-24 | heap=1 (gc_choose, รอบ 5) + DUMP 1530 methods — ข้อจำกัด: วัด ณ จุดเดียว ไม่ใช่ตลอดอายุโปรเซส |
+ | H8b | เนื้อเรื่องหลักเป็น flow เดี่ยว / non-reentrant ผูกกับตัวเอก | GUESS (ยังไม่วัด) | SP0 DUMP-only (candidate + metadata ref ของ PlotController) + รอบ trace บทเรื่องจริง | ยังไม่มีหลักฐานตรง — ห้ามอ้าง |
+ | H8c | การเข้าสำนักเรียก `HeroData.JoinForce` ได้ใน data path โดยไม่พบ PlotController ในเส้นทางที่สังเกต | RUNTIME (จำกัดขอบเขต) | สำรวจ 2026-10-25 | JoinForce ×1 จริง, JoinForceParty/JoinForceServant/ManageNewHeroJoinForce = 0 · ข้อจำกัด: caller เหนือ JoinForce ยังจับไม่ได้ (log cap) — ห้ามขยายผลเกินหลักฐาน |
```

หมายเหตุความระวังต่อ H8c: วัดจากการเข้าสำนัก **1 ครั้ง ในโลก Free 1 ใบ** — ใช้เป็น "ตัวผู้สมัครของ data path" ได้ แต่ยังห้ามสรุปว่าทุกกรณีเข้าสำนัก (สำนักอื่น/เงื่อนไขอื่น/โหมด Plot) ไม่แตะ PlotController · **มติ Q4 (2026-10-25):** ถ้อยคำทั้งสามข้อนี้เป็น**ข้อเสนอใน draft นี้เท่านั้น** — H8a จำกัดข้อสรุปตามจุดวัด · H8b คงเป็น GUESS · H8c จำกัดเฉพาะกรณีเข้าสำนักที่สำรวจ — **ห้ามแก้ STAGEPLAN/addendum ตัวจริง** จนกว่าจะมีมติแยก

## 5. ผลต่อสถาปัตยกรรม Puppet Client (รายงาน ไม่ตัดสิน)

ประเด็นที่ต่างกันต้องไล่แยก — **ห้ามเลือก queue หรือ worker process ล่วงหน้าก่อนมีหลักฐานพอ** (กติกาตายตัวของ SP0 + มติรีวิวรอบสอง ข้อ 1):

| ประเด็น | สิ่งที่รู้ (ป้าย) | สิ่งที่ยังไม่รู้ |
|---|---|---|
| การเริ่มตัวละครใหม่ (spawn) | chain สร้างโลก/hero NPC = data-level ที่ GameController [RUNTIME] · ตัวเอก heroID 0 สร้างนอก chain ที่ hook ได้ [UNKNOWN] | จุดสร้าง heroID 0 จริง · กลไก pool/cache hero gen [UNKNOWN] — เป็นงาน SP7 runtime trace |
| world simulation | `WorldData` = data container ใน `GameController.worldData` [RUNTIME] · ศึก NPC-vs-NPC resolve ทาง data path ไม่ผ่านฉาก [RUNTIME] · นาฬิกา `ChangeDay()` 1 call = 1 วัน [RUNTIME 2 กรณี — ห้ามเหมารวม] | method resolve ความขัดแย้ง NPC · writer ของ `hour` · จุด save — งาน SP2/รอบ hook ใหม่ |
| plot interaction | PlotController heap=1, 1530 methods [RUNTIME+DUMP] · เข้าสำนักไม่ผ่าน PlotController ในกรณีที่วัด [RUNTIME จำกัด] | coverage จริงของ 1530 methods · reentrancy ของเนื้อเรื่องหลัก [GUESS] — ต้องรอบ runtime trace (call-sites = `UNAVAILABLE IN DUMP-ONLY` ตามมติ Q2) |
| battle (ฉาก) | BattleController heap=1 + `get_Instance` hot [RUNTIME] · ศึกผู้เล่นเข้าฉากน่าจะผ่าน `PrepareBattleMap` (ยังไม่วัด) · ถูก gate ด้วยชื่อเสียง [RUNTIME: ต้อง 234 มี 60] | รูปแบบ worker ของ SP6 — รอ Q5/มติ |

ข้อสรุปเชิงรายงาน: singleton ของ Battle/Plot ทำให้ state กลางถูกแชร์ใน 1 โปรเซส [DUMP+RUNTIME] — แต่ "ต้องต่อคิว/แยกโปรเซสหรือไม่" ยังตัดสินไม่ได้จนกว่าจะมีข้อมูล call-site จริง (**เกิน DUMP-only — ต้องรอบ runtime** ตามมติ Q2) และ H8b ได้รับการวัด · รอบ DUMP-only ให้ได้ candidate + metadata references เพื่อชี้เป้าการวัดถัดไปเท่านั้น

## 6. SP7 ที่แยกออกไป (ไม่ทำใรอบนี้)

- **จุดสร้างตัวเอก `heroID = 0` ยังไม่ยืนยัน** — ไม่ถูกจับใน chain ที่ hook (963 calls เป็น NPC ทั้งหมด heroID 1..N) · GUESS: หน้าจอสร้างตัวละครก่อน `StartNewGame` · กำหนดเป็น **งาน runtime trace รอบใหม่** (one creation round) ที่ต้องมี save-split/preflight + แผนย่อยอนุมัติก่อน — ห้ามเริ่มโดยถือว่าได้รับอนุมัติตามมาจาก SP0
- ของที่รอบ SP7 ถัดไปควรเก็บด้วย (จากค้างของรอบก่อน): caller เหนือ `HeroData.JoinForce` (hook v4 ring-buffer ติดแล้ว) · runtime trace ของ Choose/Craft/Auction ตาม flow จริง · ความยาวจุดยืนยัน "1 leaf hit = 1 วัน" ของ `>>` (ถ้าเกี่ยว)
- **มติ Q5 (2026-10-25): `Free` เป็นข้อกำหนดของ world mode ของ authoritative Host สำหรับ MVP** — ตรงกับข้อกำหนด sandbox ที่ผู้เล่นเริ่มไม่มีสำนัก (§15.4 ข้อ 1) · **Plot mode = ข้อจำกัดที่ทราบแล้ว ยังไม่รองรับ** จนกว่าจะมีหลักฐานและมติใหม่ (fix ผู้เล่นเข้า 仙霞派 + `openLeaveForce=false` ขัด §15.4 ข้อ 1)

## 7. Exit criteria ที่วัดได้ (รอบ DUMP-only — rev2 ตามมติ Q1/Q2/Q6 · แก้เพิ่ม rev2.1)

1. **Candidate census ครบ 100% ตามกฎค้นหา (Q2):** type ที่ (ก) ชื่อมี `Controller`/`UI`/`Manager`/`Singleton` หรือ (ข) มี static field ชนิด class ของเกม หรือ (ค) มี static accessor รูป `get_Instance`/`Instance` — ทั้งหมดจาก `dump.cs` · รายงานตัวเลข N candidate types ที่คลุม พร้อมวิธี grep ที่ใช้ให้ตรวจซ้ำได้
2. **Metadata references ราย candidate (Q2):** จำนวนจุดอ้างอิงที่มองเห็นได้จาก metadata ของ `dump.cs` — field/property/parameter ที่มีชนิดเป็น type นั้น + static field/accessor ของ type นั้น — ระบุรายตัวในตาราง census
3. **Actual call-sites แยกออกชัดเจน (Q2):** `dump.cs` ไม่มี method bodies ตามปกติ ⇒ คอลัมน์ call-sites ของ**ทุกแถว** ระบุ **`UNAVAILABLE IN DUMP-ONLY`** และเลื่อนเป็นงานอื่น (รอบ runtime trace หรือเครื่องมืออื่นที่ต้องอนุมัติแยก) — **ห้ามอ้างว่านับ actual call-sites ได้จาก `dump.cs` อย่างเดียว**
4. **ชุด Battle/Plot/Choose/Craft/Auction คลุมครบในตาราง** — แต่ละระบบมีแถว controller/UI ≥1 แถว หรือบันทึก "ไม่พบ singleton" พร้อมวิธีค้นที่ใช้
5. **ทุกแถวมีวิธีตรวจ + ป้ายหลักฐานครบ** — แถว B/C ที่ยังไม่มีหลักฐาน runtime ติดสถานะ **`ยังไม่วัด (runtime required)`** ชัดเจน (Q1) ห้ามเดา
6. **verdict "ใช้ร่วมไม่ได้"** อนุญาตเฉพาะแถวที่มีหลักฐาน RUNTIME ≥1 จากรอบ `sp0sp7-20261024-225939` (Q1) — และ**ห้ามใช้ผล DUMP-only ยืนยันข้อสรุปด้าน runtime ที่ไม่มีหลักฐาน**
7. **สรุปราย spike SP1–SP7 ได้เพียง:** candidate types + metadata references + ระบบ (subsystem) ที่น่าจะเกี่ยวข้อง + **รายการที่ต้องตรวจต่อ** (ติด `runtime required`) — **ห้ามสรุปว่า singleton ตัวใด "บล็อก" spike ใด** จนกว่าจะมีหลักฐานรองรับ (call-site จริง/พฤติกรรม runtime ที่วัดได้) · ข้อสรุปที่มีหลักฐาน RUNTIME จากรอบก่อนอ้างได้ตามป้ายจริง
8. **ความบริสุทธิ์ของรอบ DUMP-only:** ไม่มี MelonLoader log ใหม่ (เกมไม่ถูกเปิด) · sha256 `GameAssembly.dll`/`global-metadata.dat` ตรง `VERSION.md` · savehash 57 ไฟล์ (`7332fc93eb98`) ไม่เปลี่ยน · `net.cfg`/`MelonPreferences.cfg` hash คงเดิม · ไม่ build/deploy DLL

**งบพยายาม (Q6):** **≤2 DUMP attempts ต่อ exit criterion และ ≤2 work sessions รวม** — หมดงบแล้วยังไม่ครบ = หยุดและรายงาน **`INCOMPLETE`** พร้อมรายการคำถามที่ยังตอบไม่ได้ · **ห้ามขยายงบ ห้ามขยับเกณฑ์เอง**

**GO/NO-GO (ตามมติ Q1):**
- **`GO-DUMP-ONLY`** = ข้อ 1–8 ครบ → ส่งมอบ `evidence/SP0-<ts>/singleton-census.md` — **verdict นี้เป็นของ deliverable DUMP-only เท่านั้น แยกออกจากการปิด SP0 census ทุกชั้น** (การปิดทุกชั้นต้องรอรอบ runtime ที่อนุมัติแยกตาม exit 3 ของ addendum)
- **`INCOMPLETE`** = งบหมดตาม Q6 แต่ข้อมูลไม่ครบ — รายงานรายการที่ยังตอบไม่ได้ แล้วหยุดรอมติผู้ใช้
- **NO-GO** = `dump.cs`/interop ให้ข้อมูลไม่พอทำชั้น A ให้จบ → ความมั่นใจของ H2 ตก หยุดรายงานรอมติ (NO-GO = "ข้อมูลไม่พอ" ไม่ใช่ความเสียหาย — census อ่านอย่างเดียว)
- ถ้าพบ singleton ที่ "บล็อกเกินแก้" (ซึ่งตาม exit 7 ต้องมีหลักฐานรองรับ — ไม่ใช่การอนุมานจาก DUMP อย่างเดียว) — ไม่ใช่ NO-GO ของ SP0: รายงานเป็นผลวัดแล้วหยุดรอมติ (อาจลดขอบเขต C2)

## 8. Rollback และขอบเขตความปลอดภัย

- รอบ DUMP-only **ไม่มีการเปลี่ยน state ของเกม**: ไม่เปิดเกม, ไม่เรียก method, ไม่เขียนเซฟ, ไม่แตะ `net.cfg`/`MelonPreferences.cfg`, ไม่แตะ Steam
- artifact ทั้งหมดอยู่ใน mod repo (`evidence/SP0-<ts>/` + สคริปต์ `tools/`) — ลบได้ทันที ไม่มีอะไรในโฟลเดอร์เกม
- **มติ Q3 (2026-10-25):** รอบนี้เป็น**งานเตรียมการก่อน "ช่วง 1"** — ไม่ใช่การอนุมัติ "ช่วง 1" runtime investigation หรือ implementation · prerequisite ของ S3 และ §21 คงไว้ตามแผนทุกตัว
- ไม่ commit ไฟล์ของเกมใด ๆ (ตามกติกา 10) · tag ปิด spike ตาม §16 กติกา 4 (`stage-SP0` เมื่อได้มติปิด)
- **ห้ามเริ่ม SP7 runtime หรือ implementation ใด โดยถือว่าได้รับอนุมัติตามมาจาก SP0** — SP7 ข้อ 1 (hook จุดสร้างตัวเอก) ต้องมี save-split/preflight + แผนย่อยอนุมัติแยกตามข้อ 6
- ข้อจำกัดเครื่องมือที่ทราบแล้ว (ส่งต่อ): `BattleController.get_Instance` เป็น hot getter — ห้าม hook แบบ log ตรง (ใช้ counter/sampling) · log cap 2,000 แถวของ frida — รอบ runtime ถัดไปใช้ hook v4 ring-buffer · ผู้ใช้อาจปิดเกมกลางรอบโดยไม่ตั้งใจ (incident 00:23) — hook ต้อง drain log ทันทีที่เหตุการณ์สำคัญจบ

---

## บันทึกมติ Q1–Q6 (2026-10-25 — รับจากผู้ใช้ สำหรับร่างนี้)

| ข้อ | มติ (สรุปตามที่รับ) | ผลต่อร่าง |
|---|---|---|
| Q1 | อนุญาตให้ปิด deliverable SP0 แบบ DUMP-only · B/C ที่ยังไม่มีหลักฐานคงสถานะ `ยังไม่วัด (runtime required)` · แยก verdict `GO-DUMP-ONLY` ออกจากการปิด SP0 census ทุกชั้น · ห้ามใช้ผล DUMP-only ยืนยันข้อสรุปด้าน runtime ที่ไม่มีหลักฐาน | §1, §3, §7 (exit 5–6, GO/NO-GO) |
| Q2 | ใช้นิยาม candidate census ตามกฎค้นหาในแผน แต่แยก candidate types / metadata references / actual call-sites ให้ชัด · `dump.cs` ไม่มี method bodies ตามปกติ ⇒ ห้ามอ้างว่านับ actual call-sites ได้จาก `dump.cs` อย่างเดียว · ไม่มีข้อมูลรองรับ = `UNAVAILABLE IN DUMP-ONLY` และเลื่อนเป็นงานอื่น | §2 (หมายเหตุใต้ตาราง), §7 (exit 1–3, 7) |
| Q3 | SP0 DUMP-only อนุญาตเป็นงานเตรียมการก่อน "ช่วง 1" (ไม่เปิดเกม ไม่เรียก method ไม่ build/deploy ไม่เปลี่ยน state) · ไม่ถือเป็นการอนุมัติ "ช่วง 1"/runtime investigation/implementation · prerequisite ของ S3 และ §21 คงตามแผน | §1 (สถานะของรอบ), §8 |
| Q4 | H8a/H8b/H8c เป็นข้อเสนอใน draft เท่านั้น (H8a จำกัดข้อสรุปตามจุดวัด · H8b คง GUESS · H8c จำกัดเฉพาะกรณีเข้าสำนักที่สำรวจ) · ห้ามแก้ STAGEPLAN/addendum ตัวจริง | §4 |
| Q5 | `Free` เป็น**ข้อกำหนด**ของ world mode ของ authoritative Host สำหรับ MVP (ตรงข้อกำหนด sandbox ที่เริ่มไม่มีสำนัก) · Plot mode = ข้อจำกัดที่ทราบแล้ว **ยังไม่รองรับ** จนกว่าจะมีหลักฐานและมติใหม่ — (ชี้แจงเป็นข้อกำหนดโดยผู้ใช้ รอบตรวจ rev2.1) | §6 |
| Q6 | จำกัด ≤2 DUMP attempts ต่อ exit criterion และ ≤2 work sessions รวม · ไม่ครบ = หยุดรายงาน `INCOMPLETE` พร้อมรายการที่ยังตอบไม่ได้ · ห้ามขยายงบ/ขยับเกณฑ์เอง | §7 (งบพยายาม + verdict `INCOMPLETE`) |

## คำถามที่ยังเปิดอยู่

1. **อนุมัติร่าง rev2.1 นี้** เพื่อเริ่ม SP0 DUMP-only — จนกว่าจะได้รับอนุมัติ ห้ามเริ่มงานจริง
2. **ตารางของรอบ runtime ถัดไป** (เศษ B/C ของ census + SP7 ข้อ 1 hook จุดสร้างตัวเอก heroID 0) — ต้องรอ prerequisite S3 + §21 ตามแผน และต้องมี save-split/preflight + แผนย่อยอนุมัติแยก
3. **จุดเวลา/รูปแบบการนำ H8a–H8c เข้าแก้ addendum จริง** — ค้างตามมติ Q4 จนกว่าจะมีมติแยก (ไม่แก้ในรอบนี้)

## ข้อเสนอ GO/NO-GO สำหรับ SP0 (รอบ DUMP-only)

**เสนอ `GO-DUMP-ONLY`** ตามมติ Q1: ชั้น A เป็นงานอ่าน `dump.cs` ล้วน ๆ เครื่องมือพร้อม ความเสี่ยงต่ำ และตอบช่องว่างที่ระบุไว้ (candidate + metadata references รายตัว) พร้อมผูกเงื่อนไข:
- actual call-sites = `UNAVAILABLE IN DUMP-ONLY` ทุกแถว (Q2) — ไม่นำผลไปสรุปเรื่อง flow จริง
- ยังไม่ถือว่า H8b ถูกพิสูจน์แม้ชั้น A จะครบ (คง GUESS จนมี trace บทเรื่องจริง)
- verdict `GO-DUMP-ONLY` ไม่ใช่การปิด SP0 census ทุกชั้น และไม่ใช่การอนุมัติ "ช่วง 1" (Q1/Q3)
- การเริ่มรอบ = ต้องได้รับอนุมัติร่าง rev2.1 นี้ก่อน
