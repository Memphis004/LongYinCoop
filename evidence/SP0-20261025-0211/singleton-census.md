# SP0 DUMP-only Singleton Census — ชั้น A ครบ + รวมหลักฐานชั้น B/C รอบก่อน

- **Label:** `SP0-20261025-0211` · วันที่: 2026-10-25 · แผน: `SP0-full-plan-draft.md` rev2.1 (approved, commit `b0660e2`)
- **แหล่งข้อมูลชั้น A:** `<เกม>\Il2CppDumper\dump.cs` — sha256 `b983820b23a46d63eae2c4123b1264dc48f6317969139fb50626d140309b5503` · 14,953,011 bytes · mtime 2026-10-20 00:47 · **ยืนันว่าเป็นรุ่นเดียวกับหลักฐาน [DUMP] รอบก่อน**: `GameDataController` TypeDefIndex 8376 ที่บรรทัด 377212 ตรงอ้างอิง `battle-notes.md:545` เป๊ะ · อ่านด้วย `tools/sp0_dump_parser.py` (read-only, ไม่โหลด assembly, ไม่แตะกระบวนการเกม)
- **ขอบเขต game types:** TypeDefIndex ≥ 7172 = `Assembly-CSharp-firstpass.dll` (เริ่ม 7172) + `Assembly-CSharp.dll` (เริ่ม 7703)
- **ป้ายหลักฐาน:** ข้อมูลรอบนี้ = **DUMP** · ข้อมูลที่ยกมาจากรอบ `sp0sp7-20261024-225939` = **RUNTIME** (ตามหลักฐานเดิม) · ที่ไม่มีหลักฐาน = **`ยังไม่วัด (runtime required)`**
- **actual call-sites ของทุกแถว = `UNAVAILABLE IN DUMP-ONLY`** (มติ Q2 — `dump.cs` ไม่มี method bodies จึงนับ call-site จริงไม่ได้ไม่ว่าแถวใด)

---

## 1. ตัวเลขรวม (exit 1)

| รายการ | ค่า | วิธีตรวจซ้ำ |
|---|---|---|
| type declarations ทั้งไฟล์ | **8,877** | `grep -c "TypeDefIndex:" dump.cs` |
| game types (idx ≥ 7172) | **1,705** | parser (`parser-integrity.json`) |
| game **classes** (kind=class) | **1,341** | parser |
| **candidate types N = (ก)∪(ข)∪(ค)** | **330** | parser + ตรวจยืนยันอิสระด้านล่าง |
| ก = ชื่อมี Controller/UI/Manager/Singleton | **304** | awk อิสรับาย = **304 ตรงเป๊ะ** |
| ข = มี static field ชนิด game class | **146** | awk อิสระ = 137 + **คลายเหตุต่าง 9 รายครบ** (§1.3) |
| ค = มี static accessor `get_Instance`/`Instance` | **112** | awk อิสระ union = **112 ตรงเป๊ะ** |
| การซ้อนกันของกฎ | ก=184 · ก+ข=19 · ก+ข+ค=101 · ข=15 · ข+ค=11 | parser |
| simple names กำกวม (ชื่อเดียวหลาย class) | 38 ชื่อ | `parser-integrity.json` → `ambiguous_simple_names` |
| บรรทัด member ที่ parser อ่านไม่ได้ | **0** (จาก 8,877 types / ทุก body) | `parser-integrity.json` → `unparsed_count` |
| ลำดับ TypeDefIndex ในไฟล์ | monotonic = true | parser-integrity |

ไฟล์ข้อมูลดิบ: `census-candidates.tsv` (330 แถว), `census-references.tsv`, `parser-integrity.json`

### 1.1 คำอธิบายกฎ (ถอดแบบ deterministic เต็ม)

- **(ก)** substring case-sensitive บน simple name (segment หลัง `.` สุดท้าย) ของ `Controller` / `UI` / `Manager` / `Singleton`
- **(ข)** static field ที่ declared type หลัง strip (i) ตัวประกอบ `ref/out/in/params/readonly` (ii) generic arguments (iii) suffix `[]`/`*`/`?` (iv) namespace/nested qualifier (`A.B` → `B`) แล้วเป็น game **class** · กรณี generic เช่น `List<HeroData>` **ไม่นับ** (ตัวถังเป็นของระบบ) แต่ `BetterList<Color>` (generic ของ game class) นับ · static field แบบ generic-arg-only แยกไว้คอลัมน์ `static_fields_generic_only_not_counted` ไม่นับใน N
- **(ค)** static method ชื่อ `get_Instance` หรือ static property ชื่อ `Instance` · (พบว่าทุก type ที่มี method ก็มี property คู่กัน — 112/112 ทั้งคู่)

### 1.2 การกระจายคีย์เวิร์ดของกฎ (ก)

Controller ล้วน 173 · UI ล้วน 102 · Controller+UI 26 · Manager 3 · **Singleton 0** (ไม่มีชื่อ type ใดมีคำว่า Singleton)

### 1.3 ความต่าง 146 vs 137 ของกฎ (ข) — คลายเหตุครบ 9 ราย

awk อิสระใช้ type-token ตรง ๆ จึงพลาด 9 กรณีที่ parser แก้ชื่อได้ถูกต้องตามนิยาม §1.1:
array 2 ราย (`InvDatabase[]`, `UICamera.MouseOrTouch[]`) + ชื่อ qualified ด้วย namespace/nested 7 ราย (`Localization.LoadFunction`, `Localization.OnLocalizeNotification`, `NGUIText.GlyphInfo`, `EventDelegate.Callback`, `GridUnitController.GridSetComparer` — pattern singleton จริง (`static readonly ... Instance`), `RAIL_API_PINVOKE.SWIGExceptionHelper`, `RAIL_API_PINVOKE.SWIGStringHelper`) — รายละเอียด raw line อยู่ใน `census-candidates.tsv` คอลัมน์ `static_fields_of_game_class`

### 1.4 ความไม่ตรงกับตัวเลข method ของรอบก่อน — อธิบายได้แล้ว 2/3 · เก็บทั้งสองชุด (ห้ามเขียนทับหลักฐานเดิม)

**วิธีนับของรอบนี้:** นับบรรทัดประกาศ method ใน body ของ type นั้น (รวม `.ctor`, property accessor `get_X`/`set_X` ที่โผล่เป็น method, explicit interface implementation) · **nested types นับแยกต่างหาก** (ไม่รวมเข้า parent) · block `/* GenericInstMethod */` (instantiation listing) ไม่นับ · ตรวจสอบ 3 ทางให้ตรงกันเอง: regex parse = จำนวนบรรทัด `// RVA:` = จำนวน method-like lines

**ที่มาของตัวเลขเดิม (ค้นพบรอบนี้):** ตัวเลขรอบก่อน = **จำนวน method + จำนวนบรรทัด attribute ที่แบบ `\t[XxxAttribute] // RVA: …`** (attribute application points — ไม่ใช่ method) เช่นนับด้วย grep ที่จับทุกบรรทัดมี `// RVA:`:

| Type | รอบนี้ (methods) | + attr-`// RVA:` lines | = ผลรวม | ตัวเลขเดิม | ผล |
|---|---|---|---|---|---|
| GameController | 241 | 3 | **244** | 244 | ✓ ตรงเป๊ะ |
| BattleController | 166 | 20 | **186** | 186 | ✓ ตรงเป๊ะ |
| PlotController | 1526 | 5 | **1531** | 1530 | ✗ ต่าง 1 — อธิบายไม่ได้ (น่าจะ off-by-one ของการนับรอบก่อน — ไม่มีทางพิสูจน์ย้อนกลับ วิธีนับเดิมไม่ถูกบันทึกไว้) |

| Type | รอบนี้ | เดิม | | Type | รอบนี้ | เดิม |
|---|---|---|---|---|---|---|
| ChooseController | 15 | 16 | | CraftPoisonUIController | 21 | 21 ✓ |
| AuctionController | 28 | 33 | | WorldPlotEventController | 9 | 9 ✓ |
| CraftUIController | 39 | 41 | | BattleAiMenuController | 8 | 9 |
| ForceGroupFightMatchChooseController | 11 | 11 ✓ | | | | |

**มติ (ตามคำสั่งผู้ใช้ 2026-10-25):** เก็บทั้งสองชุดพร้อมที่มา · ตัวเลขของรอบนี้นับวิธีที่บันทึกไว้ชัดและตรวจซ้ำได้ (3 ทาง) — เสนอใช้เป็นค่าอ้างอิงต่อไป · **ยังไม่แก้ไฟล์หลักฐานเดิม** (`sp6-census.md`/`sp0-sp7-notes.md`) จนกว่าจะมีมติแยก

---

## 2. ตาราง census หลัก (แถวตามแผน §2 — เติมชั้น A ครบ)

คอลัมน์ "refs" = metadata references จากทุก game type: `field/param` (exact — ไม่รวม generic-arg, มีแยกใน TSV) · "self" = static field/accessor ของตัวเอง · **call-sites ทุกแถว = `UNAVAILABLE IN DUMP-ONLY`**

| Type | ชั้น A (rules) | methods | self (ข/ค) | refs field/param | ชั้น B (heap) | ชั้น C (flow) | ป้ายรวม |
|---|---|---|---|---|---|---|---|
| `GameController` | ก,ข,ค | 241 | `_instance` + `Instance`/`get_Instance` | 2/0 (แทบทั้งหมดในตัวเอง/nested) | heap=1 [RUNTIME รอบก่อน] | สร้างโลก/hero = data-level [RUNTIME] | DUMP+RUNTIME |
| `BattleController` | ก,ข,ค | 166 | ✓ | 19/0 (ส่วนใหญ่ nested coroutine) | heap=1 + `get_Instance` hot 38k+ calls [RUNTIME] | ศึก NPC-vs-NPC **ไม่ผ่าน** [RUNTIME] | DUMP+RUNTIME |
| `PlotController` | ก,ข,ค | 1526 | ✓ | 4/0 | heap=1 [RUNTIME] | ยังไม่พิสูจน์ (`JoinForceParty`=0) | DUMP+RUNTIME |
| `ChooseController` | ก,ข,ค | 15 | ✓ | 1/0 | heap=1 [RUNTIME] | ยังไม่พิสูจน์ | DUMP+RUNTIME |
| `AuctionController` | ก,ข,ค | 28 | ✓ | 4/0 | heap=1 [RUNTIME] | ยังไม่พิสูจน์ | DUMP+RUNTIME |
| `CraftUIController` | ก,ข,ค | 39 | ✓ | 1/0 | heap=1 [RUNTIME] | ยังไม่พิสูจน์ | DUMP+RUNTIME |
| `CraftPoisonUIController` | ก,ข,ค | 21 | ✓ | 1/0 | heap=1 [RUNTIME] | ยังไม่พิสูจน์ | DUMP+RUNTIME |
| `WorldPlotEventController` | ก,ข,ค | 9 | ✓ | 1/0 | heap=1 [RUNTIME] | ยังไม่พิสูจน์ | DUMP+RUNTIME |
| `BattleAiMenuController` | ก,ข,ค | 8 | ✓ | 1/0 | heap=1 [RUNTIME] | — | DUMP+RUNTIME |
| `ForceGroupFightMatchChooseController` | ก,ข,ค | 11 | ✓ | 1/0 | heap=1 [RUNTIME] | — | DUMP+RUNTIME |
| `HudController` | **ก,ข,ค** | 11 | ✓ | 1/0 | **`ยังไม่วัด (runtime required)`** | `ยังไม่วัด (runtime required)` | DUMP อย่างเดียว |
| `WorkingUIController` (modal stack) | **ก,ข,ค** | 16 | ✓ | 1/0 | `ยังไม่วัด (runtime required)` | `ยังไม่วัด (runtime required)` | DUMP อย่างเดียว |
| `SureMenu` (modal stack) | **ข,ค** (ไม่มีคีย์เวิร์ด — hidden singleton) | 18 | ✓ | 1/0 | `ยังไม่วัด (runtime required)` | `ยังไม่วัด (runtime required)` | DUMP อย่างเดียว |
| `GameDataController` | ก,ข,ค | 71 | ✓ | 2/0 | `ยังไม่วัด (runtime required)` | มี `Save(int)`/`Load(int)`/`GetSaveDataPath`×2 [DUMP — candidate เดิมที่เอกสารก่อนพูดถึงแล้ว ไม่ใช่จุด save ที่พิสูจน์แล้ว] | DUMP อย่างเดียว |
| `BattleUnit` | **ไม่ใช่ candidate** (ไม่มี ข/ค) | — | — | 11/26 (ถูกอ้างเยอะแต่เป็น instance ปกติ) | ไม่ใช่ singleton [DUMP รอบก่อน] | — | DUMP |
| controller รอบ 7 ตัว (`BattleTeamHeroIconController` ก, `PlotInteractController` ก, `StartBattleButtonController` ก, `BattleAISettingController` ก, `BattlePrepareSpellButtonController` ก, `RecruitChooseToggleController` ก, `BattleBackground` ไม่ใช่ candidate) | กล้วนหรือไม่มี | 2–7 | **ไม่มี ข/ค** | 0 | ไม่ใช่ singleton [DUMP รอบก่อน] | — | DUMP |
| `WorldData` | **ไม่ใช่ candidate** (data container) | — | — | 2/0 — ถูกเก็บเป็น field เฉพาะใน `GameController.worldData` และ `GameSaveData` | container ใน GameController [RUNTIME] | `Player()` = instance method คืน `HeroData` [DUMP] | DUMP+RUNTIME |
| `WorldData.playerHero` | **ไม่มีจริง — 0 fields ชื่อ playerHero ใน WorldData** (ตรวจเป็นระบบทั้ง parse) | n/a | n/a | n/a | n/a | n/a | DUMP |
| camera/UI binding, จุด input | `UICamera` = ก,ข (75m, 106 fields — NGUI ของ lib) · `CameraController` = ก | — | — | — | `ยังไม่วัด (runtime required)` | `ยังไม่วัด (runtime required)` | DUMP อย่างเดียว/บางส่วน |

ข้อสังเกตชั้น A สำคัญ: controller หลัก **แทบไม่ถูกเก็บเป็น field ของ type อื่นเลย** (GameController 2, PlotController 4, BattleController 19 ส่วนใหญ่ใน nested) ⇒ การเข้าถึงเกิดผ่าน static accessor เป็นหลัก = **call-site ซึ่ง dump มองไม่เห็น** — สอดคล้องข้อจำกัด Q2 และยืนว่า "flow ใดแตะ singleton ตัวไหน" ต้องรอรอบ runtime เท่านั้น

---

## 3. Hidden singletons — กฎ (ค) โดยไม่มีคีย์เวิร์ดชื่อ (11 types)

`RailCallBackHelper`(8m) · `MapNavigator`(6m) · `QuickDetail`(31m) · `ShakeCam`(5m) · `SimpleDetail`(8m) · `SteamStatsAndAchievements`(10m) · `StudyAttackPlayer`(9m) · `StudyDodgePlayer`(12m) · `StudyUniquePlayer`(7m) · `SureMenu`(18m) · `WegameStatsAndAchievements`(16m)

ทั้งหมดมี pattern เต็ม (static game-class field + `Instance`/`get_Instance`) · heap/flow = `ยังไม่วัด (runtime required)` · `SteamStatsAndAchievements`/`WegameStatsAndAchievements` อยู่ในกลุ่ม achievements/stats — **ห้ามแตะ** ตามกติกา 5 (เพียงบันทึกว่ามีอยู่ใน census)

## 4. ครอบคลุมระบบตาม exit 4

| ระบบ | จำนวน candidate | รายชื่อ |
|---|---|---|
| Battle | 8 | BattleController, BattleAiMenuController, BattleController.ActiveHeroUIRefs (nested), BattleAISettingController, BattlePrepareSpellButtonController, **BattlePrepareSpellController** (พบเพิ่มในรอบนี้), BattleTeamHeroIconController, StartBattleButtonController |
| Plot | 3 | PlotController, PlotInteractController, WorldPlotEventController |
| Choose | 3 | ChooseController, ForceGroupFightMatchChooseController, RecruitChooseToggleController |
| Craft | 2 | CraftUIController, CraftPoisonUIController |
| Auction | 1 | AuctionController |

ทุกระบบมีแถว ≥1 (ผ่าน exit 4) · รายการทั้ง 330 แถวอยู่ใน `census-candidates.tsv`

## 5. Metadata references ของ data types ที่ไม่ใช่ candidate (เก็บเพิ่มเพื่อชี้เป้า SP1–SP7)

| Type | มีจริง? | field (exact/generic) | param (exact/generic) | ret | ผู้อ้างหลัก |
|---|---|---|---|---|---|
| `HeroData` | ✓ | 41/49 | 235/35 | 23 | GameController(61), PlotController(61), BattleController(33), AIController(31) |
| `BattleUnit` | ✓ | 11/5 | 26/0 | 2 | BattleController(24), BattleTeam(5) |
| `WorldData` | ✓ | 2/0 | 0/0 | 0 | GameController, GameSaveData |
| `TimeData` | ✓ | 8/2 | 6/0 | 0 | WorldData, InfoData, WorldPlotEventData, MailData |
| `ForceData` | ✓ | 14/3 | 40/0 | 10 | GameController(22), WorldData, PlotController |
| `AreaData` | ✓ | 11/3 | 39/0 | 12 | GameController(24), WorldData, AreaController, BigMapController |
| `ItemListData` | ✓ | 15/0 | 26/0 | 0 | TradeUIController(9), HeroData, GameController |
| `Player` (type) | **ไม่มี type ชื่อนี้ใน game images** — "Player" เป็นเพียงชื่อ method `WorldData.Player()` คืน `HeroData` [DUMP] | | | | |

## 6. ข้อค้นพบใหม่จากชั้น A (DUMP — รอการยืนยัน runtime ทั้งหมด)

1. **`GameDataController.Save/Load` = การยืนยัน candidate ที่เอกสารเดิมพูดถึงแล้ว ไม่ใช่ข้อค้นพบใหม่** — เอกสารเดิม: `battle-notes.md:545` [DUMP] (TypeDefIndex 8376 @ `dump.cs:377212` — รอบนี้ parse ได้ตรงเป๊ะ) · `STAGEPLAN.md:440` (ระบุ signature ไว้ใช้ patch) · addendum §16 กติกา 6 (ห้ามเรียก `GameDataController.Save/Load`) · สิ่งที่รอบนี้เพิ่ม: รายชื่อ method ที่ parse ได้ = `Save(int)` ×1 · `Load(int)` ×1 · `GetSaveDataPath` ×**2 overloads** `(int,int)` และ `()` · `GetOldSaveDataPath(int,int)` — ข้อสังเกต: `STAGEPLAN.md:440` เขียนว่า Save มี "2 overload" แต่ dump มี `Save` 1 overload (ที่มี 2 คือ `GetSaveDataPath`) — ควรแก้ตอนมีมติ · **DUMP เพียงอย่างเดียวยังไม่พิสูจน์ว่าเป็นจุดเขียนเซฟที่ใช้จริง** — ต้อง hook รอบ runtime (`GameController.Save` ไม่มีจริง ยืนยันซ้ำ)
2. `WorldData.playerHero` = 0 fields — ปิดข้อค้าง S6 §9 ข้อ 1 ซ้ำด้วยวิธีเป็นระบบ (parse ทั้งไฟล์ ไม่ใช่ grep จุด)
3. `BattlePrepareSpellController` เป็น candidate ที่ไม่อยู่ในตารางรอบก่อน (รอบก่อนบันทึก 7 controller รอง — ตัวนี้หลุด)
4. Singleton pattern (ค) พบ 112 ตัว **ทุกตัวที่มี method `get_Instance` ก็มี property `Instance` คู่กันเสมอ** (112/112)

## 7. ข้อจำกัดของรอบนี้ (ห้ามลืมตอนอ่านตาราง)

- **call-sites = `UNAVAILABLE IN DUMP-ONLY` ทุกแถว** — จำนวน refs ที่รายงานเป็น "จุดอ้างอิงใน metadata" (field/property/parameter/signature) เท่านั้น ไม่ใช่จำนวนการเรียกจริง และ**ห้ามใช้สรุปเรื่อง flow/queue ของ runtime** (มติ Q1/Q2)
- "heap=1" ของรอบก่อน = ณ เวลาวัด ไม่ใช่ตลอดอายุโปรเซส (ยกเว้น `BattleController.get_Instance` ที่พิสูจน์ต่อเนื่องด้วย counter)
- ความกำกวมของ simple name (38 ชื่อ) ทำให้การ resolve แบบ last-segment อาจรวม type คนละตัวที่ชื่อซ้ำ — แถวหลักในตาราง §2 ตรวจแล้วไม่กำกวม แต่การใช้ TSV รายแถวอื่นควรเช็คคอลัมน์ `ambiguous_simple_name`
- กฎ (ก) เป็น substring ตามตัวบรรทัดของแผน — จึงจับ `UICamera`/NGUI (ของ library ที่ถูก link เข้า game images) ด้วย; การแยก "ของเกมเอง vs ของ lib ที่อยู่ใน Assembly-CSharp" ต้องดูเป็นราย type
- ไม่ได้อ่าน interop assemblies (`MelonLoader/Il2CppAssemblies`) — ใช้ `dump.cs` อย่างเดียวตามข้อจำกัดเครื่องมือของแผน §1 (ผลตรงเป้าหมายชั้น A ครบโดยไม่ต้องถึง metadata reader)
