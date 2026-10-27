# SP5-Candidate Census — สำรวจ NPC ที่ลดได้อย่างปลอดภัย (read-only)

- **label:** `SP5census-20261026-224844` · **วันที่:** 2026-10-26 22:48 (+0700)
- **ขอบเขต:** read-only เท่านั้น — อ่าน `Il2CppDumper\dump.cs`, `stringliteral.json`, Unity assets ผ่าน UnityPy
  (in-memory, ไม่เขียนไฟล์เกม), evidence/โน้ตเดิม — ไม่เปิดเกม ไม่ hook ไม่ build/deploy ไม่แตะเซฟ/config
  ไม่ commit/tag ไม่แก้โหมดเดิม (ไฟล์ที่สร้างใหม่ = โฟลเดอร์ evidence นี้เท่านั้น)
- **ความต่อเนื่อง:** ต่อจาก `SP5prep-20261026-2222` (DUMP-only รอบก่อน) — รอบนี้เพิ่มชั้นข้อมูลที่รอบก่อน
  ทำไม่ได้: **ตารางข้อมูลจริงของเกม** (TextAsset ใน `resources.assets`) ถูกดึงและวิเคราะห์ครบกลุ่ม NPC

## 0) Purity ของรอบ

- hash เกมวัดซ้ำตอนท้ายรอบ (22:5x): GameAssembly `33c56def…7920c1`, global-metadata `451311d4…18bfd0`
  — **ตรง VERSION.md** ⇒ dump.cs/ตารางที่อ่านเป็นของเวอร์ชันปัจจุบัน (V1.1.2f0)
- เกมไม่รัน (tasklist ไม่พบ `LongYinLiZhiZhuan.exe`)
- game repo git status = ตรง snapshot เริ่ม conversation (ไม่มีไฟล์เกมถูกแตะ)
- เครื่องมือ: UnityPy 1.25.3 (มีอยู่แล้วในเครื่อง ไม่ได้ติดตั้งเพิ่ม) · objdump/capstone **ไม่มี** ⇒ ไม่ได้
  disassemble method body ในรอบนี้ (คงเป็น UNKNOWN ตามรายการ §6)

## 1) เส้นทางข้อมูลของตัวละคร (data path)

```
resources.assets (TextAsset 586 ชิ้น — เกมไม่ใช้ StreamingAssets เก็บตาราง)
└─ GameDataController.LoadAllGameData() + LTCSVLoader  [DUMP: ลายเซ็น; จุดเรียก per-table ไม่เห็น body]
   ├─ SpeHeroDataBase      ← SpeHeroData.csv        (170 แถว, id 1–170)
   ├─ forceDataBase        ← ForceData.csv          (30 แถว, id 0–29)
   ├─ areaDataBase         ← AreaData.csv           (79 แถว, id 0–78)
   ├─ buildingDataBase     ← BuildingData.csv
   ├─ kungfuSkillDataBase  ← KungFuData.csv
   ├─ familyNameDataBase/givenName/male/female ← NameData.csv (姓87/名80/男名76/女名62)
   ├─ speAddDataBase/forceSpeAddDataBase ← SpeAddDataBase.csv / ForceSpeAddDataBase.csv
   ├─ heroTagDataBase ← HeroTagData.csv · innDataBase ← InnData.csv · loveableSpeHeroList ← LoveableSpeHero.csv (57)
   └─ forceLevelData (List<ForceLevelData>) — **ไม่พบ CSV ใน asset ใด** ⇒ ค่าถูกคอมไพล์ในโค้ด
      (เช่นเดียวกับ GlobalData.* ทุกค่า) — ค่าจริง UNKNOWN จนกว่าจะ disassemble/อ่าน runtime
GameController.StartNewGame → GenerateHero → GenerateHeroData (factory) → WorldData.Heros/TempHeros
GameSaveData.HeroList/TempHeroList (list จริงมี [JsonIgnore] — เซฟผ่าน GameSaveData + ธง finished)  [DUMP]
```

- **การ binding CSV→loader:** ไม่พบชื่อตารางเป็น string literal เลย (19,400 literals ตรวจแล้ว) ⇒
  น่าจะผูกผ่าน Unity serialization (Inspector reference) — ชื่อ TextAsset ตรงกับชื่อฟิลด์ตารางแบบ
  1:1 เป็นหลักฐานสนับสนุน — ป้าย **INFER** (ทางการยัง UNKNOWN)
- ไฟล์รายสำนัก 40 ไฟล์ (丐帮 ฯลฯ) / 13 เมือง / `VillageDefault0-6` / `ForceDefault` / `CityDefault` =
  **grid ผังพื้นที่** (ตัวเลข -3..2) ไม่ใช่ตารางจำนวนสมาชิก
- ชื่อตัวละครพิเศษบางตัวมี string literal ของตัวเองในโค้ด (เช่น `杨思迟` ×6, `魏胥华` + บททั้งย่อหน้า)
  — DUMP

## 2) แผนผังอ้างอิง heroID ข้ามระบบ (DUMP ทั้งหมด — เลข = offset ใน class)

**HeroData** (369988, 175 ฟิลด์ใน dump รอบนี้; เต็ม 187 ตาม hero-fields*.json):
- ตัวตน/ธง: `heroID` 0x58 · `speHero` 0x5C · `tempPlotHero` 0x5D · `recruitAble` 0x5E · `loveAble` 0x5F ·
  `hide` 0x60 · `dead` 0x61 · `isTempHero` 0x3CC · `isRandomEnemy` 0x3CD · `belongForceID` 0x84 ·
  `outsideForce` 0x8C · `isLeader` 0xB4 · `atAreaID` 0xC0 · `bigMapPos` 0xC8
- ความสัมพันธ์ (ID อิสระ): `Teacher` 0x31C(int) · `Lover` 0x328(int) · `Students` 0x320 · `PreLovers` 0x330 ·
  `Relatives` 0x338 · `Brothers` 0x340 · `Friends` 0x348 · `Haters` 0x350 (List<int>) ·
  `teamLeader` 0x2F4 · `teamMates` 0x2F8
- ตัวถือข้อมูลลูก: `itemListData` 0x220 / `selfStorage` (ItemListData.heroID) · `kungfuSkills` 0x260 +
  internal/dodge/unique/attack (KungfuSkillLvData.belongHeroID) · `heroAIData` 0x40
- ระบบอื่นที่อ้าง hero:
  - `ForceData.leader` 0x58(int) · `ownHeros` 0x70(List<int>) · `heroLvNum` 0x78 · `totalPopulation` 0x84 ·
    `autoAddMember` 0x25 · `bookWriterList` 0xB0(List<BookWriterData>)
  - `PlotData.targetHeroID` 0x2C · `plotRandomHero` 0x20(List<PlotRandomHeroData>:
    areaID/favorRange/forceID/forceLv) · `plotDatas` 0x40(List<SinglePlotData> — ผูกด้วย**ชื่อ**)
  - `MissionData.sourceHeroID` 0x54 · `missionHeroID` 0x5C · `missionJoinTeamHeroID` 0xA0 ·
    `MissionRelateToHero(int)` (missions ฝังใน `AreaBuildingData.missionDatas` 0x30)
  - `AreaBuildingData.belongHeroID` 0x44 · `BookWriterData.bookWriterHeroID` 0x18 ·
    `ItemListData.heroID` 0x10 · `KungfuSkillLvData.belongHeroID` 0x24
  - `FavorData.heroID` — **ไม่พบ class ใดอ้างกลับ** (metadata-only) = orphan-risk legacy
- helpers กัน dangling ฝั่งเกม (DUMP decl, พฤติกรรม UNKNOWN จนถอด body): `WorldData.GetHero/GetHeroForce/
  AddNewHero/RemoveHero/AddTempHero/RemoveTempHero/FindAvailableHeroID/FindAvailableTempHeroID` ·
  `HeroData.HaveFriend/HaveTeacher/HaveLover/HaveRelationBetterThanFriend` ·
  `GameController.HeroRealAutoRemove/ManageHeroAutoQuitAndEnter/GetHeroName` · `ForceData.FindAllHero(...)`

## 3) Census — NPC แยกตามต้นทางการสร้าง

โลก default เริ่มใหม่ (RUNTIME, `sp0sp7-20261024-225939` รอบ 5 + `area-notes`): **Heros 883 + TempHeros 88
→ 223 หลัง ~25 วันเกม + Forces 30 + Areas 69 (ตารางมี 79 — ต่าง 10, UNKNOWN) + ตัวเอก heroID 0**

| สาย | ต้นทาง | จำนวน | ป้าย |
|---|---|---|---|
| A ตัวละครพิเศษ | `SpeHeroDataBase` ← SpeHeroData.csv (id 1–170) | **170** (ในสำนัก 79 / เร่ร่อน 无 91 / ซ่อน 18 / 掌门 30 = ครบ 1 ต่อสำนัก / 出师角色 38) | DUMP(ตาราง) |
| A ยืนยันมีในโลก | จุดเรียน仙霞派ได้ `Teacher=1` (= 姜.映泉, แถวแรกของตาราง) | 1 เคส | RUNTIME |
| B สมาชิกสำนักทั่วไป | factory `GenerateHeroData` (overload เดียวที่เคยเห็นยิง, ชื่อ hook "#2") — ในสำนัก 756 ครั้ง (17–35/สำนัก) ลบ special ในสำนัก 79 | **≈677** | RUNTIME(นับ)+INFER(ลบเลข) |
| C เร่ร่อนทั่วไป | factory forceID −1 ×186 ลบ special 无 91 | **≈95** | RUNTIME+INFER |
| D กลุ่ม forceID −2 | factory ×21 | **21** — ความหมาย −2 UNKNOWN | RUNTIME(นับ) |
| E TempHero เริ่มโลก | `AddTempHero` ×81–88 (แกว่งตามโลก) → โตเป็น 223 ใน 25 วันเกม (`isTempHero=isRandomEnemy=true` ทั้งหมด, `tempPlotHero=0` ตอน snapshot) | 88 → +135 | RUNTIME |
| F temp plot hero | สูตร `临时:<ชื่อ>&<เพศ>;<อายุ>;<forceID>;<forceLv>` ในบทสนทนา (PlotData.csv: 536 cell / 308 spec) spawn ตอนเล่น plot | ต่อ plot | DUMP(ตาราง) |
| ตัวเอก | heroID 0 — **ไม่พบจุดสร้าง**ใน chain ที่ hook | 1 | RUNTIME(มีจริง)+UNKNOWN(จุดสร้าง) |

**บัญชีตัวเลข (คงสถานะเดิม — ยังไม่ปิด):** factory ×963 vs Heros 883 = gap 80 · generic จาก factory 793
≈ 713 ใน Heros + ~80 ใน TempHeros (INFER ปิดบัญชีได้ประมาณ ยังไม่พิสูจน์) · ctor 886 vs 971 (UNKNOWN) ·
รอบ 3 factory = 0 ครั้งแต่ Heros≈883 ⇒ **มี path ที่สอง (pool/cache ใน process เดียว)** (RUNTIME) —
การนับ call ของ factory **ไม่ใช่**ตัวแทนจำนวน NPC

**สัดส่วนเริ่มโลก (ประชากร ≈972):** PROTECTED 171 ≈ 17.6% · CANDIDATE (generic B+C ≈ 772 + TempHero
เริ่มโลก 88) ≈ 88% ของที่เหลือ (เลขล้นเพดานเพราะ gap 80 — รายงานเป็นช่วง) · กลุ่ม D 21 ≈ 2%
หลัง 25 วันเกม TempHeros 223 ⇒ สัดส่วน random/temp เพิ่มเป็น ~19% ของประชากร (~1,107)

## 4) หลักฐานว่ากลุ่ม A ห้ามแตะ (PROTECTED — หลักฐานซ้อนกัน 6 ชั้น)

1. `SpeHeroDataBase: Dictionary<int, HeroData>` ใน GameDataController (DUMP)
2. 掌门 30 คนครบทุกสำนัก — `ForceData.leader` ต้องมีค่า (ตาราง + DUMP field)
3. บทสนทนาผูกด้วย**ชื่อ**: 469 ชื่อใน PlotData.csv, **160 ตรงกับ SpeHeroData** (normalize จุด 姓.名 ออก) ·
   SinglePlotData ใช้ `sourceName/targetName` (DUMP) · ชื่อบางตัวมี string literal ในโค้ด
4. `LoveableSpeHero.csv` 57 ชื่อ — match SpeHeroData ทั้งหมด 0 หลุด
5. ค่าความสัมพันธ์ preset 33 แถว อ้าง heroID 34 ตัว **ปิดภายในกลุ่ม 1–170 ทั้งหมด** (0 หลุดนอกตาราง)
6. `GlobalData.SpeNoRandomName/SpeMasterName/SpeWeakHeroName/SpeTeammateName/
   SpeDisableFreeModeUnlockRecruitName` (DUMP decl — ค่า UNKNOWN) = ตัวกรองด้วยชื่ออีกชั้น

รวม PROTECTED = **170 + ตัวเอก = 171** (เป้า "รักษา ~200" ของงานเดิม: มีงบเหลือ ~29 สำหรับสมาชิกเติม)

## 5) จุดควบคุมจำนวนที่มีหลักฐาน

| จุดควบคุม | หลักฐาน | ใช้ลดกลุ่ม |
|---|---|---|
| `ForceLevelData.heroNum` (List<int>) — decl เดียวของ class | DUMP (369294); **ไม่มี CSV; ค่า+indexing UNKNOWN** | B (สมาชิกต่อระดับสำนัก — INFER) |
| `GameController.GenerateRandomEnemy(int maxNum = 999)` | DUMP — ตัวเดียวที่รับจำนวนเป็นพารามิเตอร์ | E |
| `customDifficultyLv[randomEnemyNum=6]` + slider ในหน้าตั้งค่า (11 slider ตรง enum ครบ; **ไม่มี** slider จำนวน NPC ฐาน) | DUMP + SCREENSHOT-USER (รอบก่อน) | E (เกมรองรับอยู่แล้ว) |
| `GlobalData.MaxHeroNum/RecommendHeroNum/MinOutforceSimpleHeroNum/TempHeroIDStart` | DUMP decl; **ค่า UNKNOWN** (คอมไพล์ในโค้ด) | cap รวม/เร่ร่อนนอกสำนัก |
| `ForceData.autoAddMember = 1 ใน 28/30 สำนัก` (bigForce=1 ใน 20/30) | DUMP ตาราง | **สมาชิกเติมกลับเองตามเวลา** — ลดตั้งต้นอย่างเดียวไม่พอ |
| `CustomDifficultyType.aiForceDevelopSpeed=10, teammateLimit=9, randomEnemyStrength=5` | DUMP + SCREENSHOT-USER | อัตราโตฝั่ง AI |
| `GameController.GenerateHero()` ไม่รับพารามิเตอร์ เรียก 1 ครั้ง/สร้างโลก (~1 s) | DUMP + RUNTIME | จุดแทรก gate รอบการสร้างโลก |
| path ที่สอง (pool/cache, factory=0 ครั้งแต่ได้ Heros 883) | RUNTIME รอบ 3 | **gate ต้องครอบทั้งสอง path** หรือสร้างโลกใน process สด |
| `GenerateRelationShip()` 0x7B4400 | DUMP decl; body UNKNOWN | ต้อง re-check หลังลด |

## 6) UNKNOWN ที่คงสถานะ (ไม่เดา)

ค่า `heroNum`/`MaxHeroNum`/`TempHeroIDStart`/`Spe*Name` (ต้อง disassemble ด้วย ILSpy+DummyDll หรือ
อ่าน runtime — เครื่องมือ disassemble ไม่มีในเครื่องรอบนี้) · mapping "#2"→ลายเซ็น overload (INFER รอ
hook ลายเซ็นเต็ม) · กลไก path ที่สอง · จุดสร้างตัวเอก heroID 0 · ความหมาย forceID −2 · gap 80/85 ·
AreaData 79 vs runtime 69 · ผลของสระ hero ที่เล็กลงต่อ mission/plot generator · FavorData orphan ·
`PlotData` runtime (targetHeroID) ถูกเติมจากไหน (ไม่พบ CSV ตรงชื่อ)

## 7) การจัดกลุ่ม + ข้อจำกัดของการจัด (บังคับตามโจทย์ข้อ 4)

- **PROTECTED (171):** กลุ่ม A ทั้งหมด + ตัวเอก — ข้อจำกัด: อ้างหลักฐาน static + RUNTIME 1 เคส (Teacher=1);
  ยังไม่ได้พิสูจน์ว่าทั้ง 170 ถูก instantiate ครบทุกโลกทุกโหมด (hidden 18 อาจรอ trigger)
- **CANDIDATE (≈772 เริ่มโลก: B ≈677 + C ≈95 + อีก ~80 แถว gap ในบัญชี):** เป็นการจัดแบบ**ประเภท/สาย
  การสร้าง** เท่านั้น — **ไม่มีรายชื่อรายตัว** เพราะ generic NPC ถูกปั่นชื่อ/ID ตอนรันจาก NameData ⇒
  **ห้าม**ตีความว่า "ตัวใดตัวหนึ่งปลอดภัยต่อการลบ"; ความปลอดภัยอยู่ที่การลดที่**ต้นทาง**แบบ gate ต่อโหมด
- **UNKNOWN (ไม่จัด CANDIDATE):** กลุ่ม D (forceID −2, 21 คน) · TempHero ที่โผล่หลังเริ่มโลก (event/
  random enemy — ต้องแยกชนิดก่อน) · temp plot hero (จำเป็นต่อการเล่น plot ถึงจะ spawn ชั่วคราว)

## 8) ความเสี่ยงถ้าลด (จากผัง §2)

1. dangling int-ID 10 สาย — เกมมี helper ตรวจก่อนใช้ (ลดโอกาส crash) แต่ยืนยันว่า **consumer ทุกตัว**
   ตรวจไม่ได้จาก metadata (UNKNOWN) ⇒ ต้องวัด runtime
2. ชื่อ-binding 3 ชั้น (บทสนทนา / GlobalData.Spe*Name / LoveableSpeHero) ⇒ ห้ามแตะกลุ่ม A และคลังชื่อ
3. autoAddMember 28/30 ⇒ โลกที่ปล่อยเวลาเดินจะเติมสมาชิกกลับจนถึง steady-state ตามตาราง — การลดต้อง
   ตัดสินนโยบายของ *โลก co-op* ว่าปิด auto-refill หรือไม่ (เปลี่ยนพฤติกรรมเฉพาะโหมดใหม่ ไม่แตะโหมดเดิม)
4. mission/plot generator เลือกจากสระ hero (FindAllHero/PlotRandomHeroData กรอง forceID/favor/area) —
   สระเล็กลงอาจสร้าง mission/plot ไม่ได้ตัวหรือเพาะซ้ำ (UNKNOWN → ทดสอบ)
5. `GenerateRelationShip` หลังสร้างโลกอาจอ้าง ID ที่ไม่ถูกสร้าง ⇒ ต้อง re-check หลัง clamp
6. เซฟ: HeroList/TempHeroList + ธง finished — โลกที่ลดแล้วต้องเซฟ/โหลด/เซฟซ้ำได้ครบ

## 9) แนวทางลดจากต้นทางโดยรักษาโหมดเดิม (ข้อเสนอ — ยังไม่ทำ)

หลักการ: เขียนทับพารามิเตอร์**ตอนสร้างโลกของโหมด multiplayer เท่านั้น** (gate ที่ StartNewGame/GenerateHero)
ไม่แก้ตาราง CSV, ไม่ลบ HeroData ที่สร้างแล้ว, ไม่แก้ข้อมูลเกม
1. clamp จำนวน generic ต่อสำนักในลูปสร้างโลก (จุดอ้าง: factory + heroNum) — เป้า 200 = 170 special +
   ตัวเอก + งบ ~29 หรือ 0 แล้วปล่อย autoAddMember ของโลก co-op เติมเองตามเวลา
2. clamp กลุ่ม E ผ่านกลไกเดิม (`GetRandomEnemyCount`/`GenerateRandomEnemy` + randomEnemyNum)
3. กัน path ที่สอง: สร้างโลก co-op ใน process สด หรือยืนยัน pool/cache ให้ชัดก่อน (งาน hook ต่อ)
4. ตัวเอก + กลุ่ม D ยังไม่แตะจนรู้ความหมาย −2

## 10) ผลตรวจที่จำเป็นก่อนอนุมัติ implementation (ข้อ 6 ของโจทย์)

1. ถอด body (ILSpy/dnSpy บน DummyDll หรือ Ghidra+il2cpp.h หรือ hook): `StartNewGame → GenerateHero →
   GenerateHeroData×3`, `GenerateRelationShip`, `GenerateRandomEnemy`, `GetRandomEnemyCount`,
   `LoadAllGameData` (ค่า heroNum/MaxHeroNum) — ปิด UNKNOWN กลไก/ค่าคงที่
2. hook log-only 1 creation round บนสำเนาเซฟ: ลายเซ็นเต็มทุก arg ทั้ง 3 overload + AddTempHero +
   path ที่สอง + จุดสร้างตัวเอก — ปิด mapping และบัญชี 886/971, gap 80
3. runtime probe หลัง clamp (สำเนาเซฟ): census หลังสร้างโลก (จำนวน/กระจาย forceID/speHero=170/掌门ครบ/
   TempHeroID ไม่ชน) → เดิน 1 เดือนเกม (autoAddMember, TempHero growth) → เซฟ/โหลด/เซฟซ้ำ digest คงที่
   → เปิด plot/mission สุ่ม ≥20 ชิ้นเช็ค dangling-ID error=0
4. negative: โหมดเดิม (ไม่เปิด co-op) ต้องได้พฤติกรรม/จำนวนเดิมทุกดัชนี — และ hash ไฟล์เกม/ตารางไม่เปลี่ยน

**GO:** ข้อ 1–2 ปิดกลไก+ค่าคงที่ · probe ข้อ 3 ครบทุกดัชนี (0 dangling, plot/mission ครบ, เซฟ digest คงที่) ·
โหมดเดิมเหมือนเดิมทุกดัชนี
**NO-GO:** เจอ consumer อ่าน heroID โดยไม่ตรวจ (crash/corrupt) · plot/mission พัง · path ที่สองหลุด gate ·
โหมดเดิมเปลี่ยนพฤติกรรม · ต้องแตะตาราง CSV/ข้อมูลเกมเพื่อให้ได้ผล

## หลักฐานประกอบในโฟลเดอร์นี้
`textasset-inventory.tsv` (586 รายการ) · `tables/` 66 ไฟล์ (SpeHeroData, ForceData, AreaData, NameData,
LoveableSpeHero, PlotData ฯลฯ — ดึงด้วย UnityPy in-memory แล้วเขียนที่นี่เท่านั้น)
