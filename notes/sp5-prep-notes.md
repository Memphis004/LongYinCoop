# SP5-prep — บันทึกสำรวจ DUMP-only: จำนวน hero ตอนเริ่มโลกใหม่มาจากไหน

- **label:** `SP5prep-20261026-2222` · **วันที่:** 2026-10-26 22:22 (+0700)
- **ขอบเขต:** DUMP-only (อ่าน `Il2CppDumper\dump.cs` อย่างเดียว) — ห้ามเปิดเกม ห้ามเรียก method ใด
  ห้าม build/deploy ห้ามแตะเซฟ/config/Steam ห้ามแก้ source หรือไฟล์ track ใด ๆ (สร้างไฟล์นี้ไฟล์เดียว)
- **คำถามที่ตั้ง:** hero ~963 (`GenerateHeroData`) + 81–88 (`TempHero`) ตอนเริ่มโลกใหม่มาจากไหน
- **ข้อจำกัดของแหล่งข้อมูล (อ่านก่อนทุกหัวข้อ):** dump.cs ของ Il2CppDumper มีเฉพาะ **การประกาศ**
  (ลายเซ็น method, ฟิลด์, enum, attribute) — **ไม่มี method body เลย** ทุกข้อสรุปเรื่อง flow/caller/ค่าคงที่
  จึงเป็น GUESS หรือ UNKNOWN และ **ไม่มีข้อใดในเอกสารนี้ยืนยันพฤติกรรม runtime ได้**

ป้ายที่ใช้: **DUMP** = อ่านจากการประกาศใน dump.cs โดยตรง · **RUNTIME** = จากหลักฐาน hook เดิมของ
`sp0sp7-20261024-225939` · **GUESS/INFER** = อนุมานจากชื่อ/โครงสร้าง · **UNKNOWN** = ยังพิสูจน์ไม่ได้ ·
**SCREENSHOT-USER** = จากภาพที่ผู้ใช้ให้ (ไม่ใช่ runtime ไม่ใช่ DUMP)

---

## 0) Anchor แหล่งข้อมูล + วิธีตรวจซ้ำ

### 0.1 Anchor (วัด 2026-10-26 22:2x +0700)

| ไฟล์ | SHA-256 | ขนาด (bytes) | mtime |
|---|---|---|---|
| `Il2CppDumper\dump.cs` | `b983820b23a46d63eae2c4123b1264dc48f6317969139fb50626d140309b5503` | 14,953,011 | 2026-10-20 00:47:49 +0700 |
| `GameAssembly.dll` | `33c56def4102844a1190eec963a05c834cfcf992d42b423d485ce8584d7920c1` | 33,669,120 | — |
| `LongYinLiZhiZhuan_Data\il2cpp_data\Metadata\global-metadata.dat` | `451311d4dc8c1b8b5073905c8b39a9781bd7e1f44ed88fbf7b35f5345c18bfd0` | 7,960,432 | — |

- `GameAssembly.dll` + `global-metadata.dat` **ตรง** `VERSION.md` บรรทัด 8–9 (DUMP ทั้งคู่ เทียบแล้ว) ⇒
  dump.cs ที่สำรวจคือของเกมเวอร์ชันปัจจุบัน (V1.1.2f0)
- หลักฐาน RUNTIME ที่อ้าง (§2): `evidence/sp0sp7-20261024-225939/sp4-creation-chain.md`,
  `evidence/sp0sp7-20261024-225939/sp4-hook-v1-partial.md`, `sp0-sp7-notes.md` (root ของ mod repo)

### 0.2 วิธีตรวจซ้ำ (คำสั่งจริงที่ใช้ — ระวัง ugrep)

**คำเตือนเครื่องมือ:** `grep` ใน shell นี้คือ **ugrep** — pattern ที่ขึ้นต้นด้วย `//` ถูกตีเป็น option
(error `invalid option -/...`) และ **process substitution `<(...)` ใช้กับ ugrep ไม่ได้** (error คล้ายกัน)
⇒ สำหรับ pattern ที่มี `//` ให้ใช้ **awk `match()`** แทน; กรองบรรทัด RVA ด้วย `grep -v -E "RVA:|Offset:"`

```bash
cd "<เกม>/Il2CppDumper"

# หาตำแหน่ง class เป้าหมาย (บรรทัดใน dump.cs)
grep -n -E "^[[:space:]]*(public |internal |private )?(sealed |abstract |partial |static )*class (GameController|WorldData|ForceData|HeroData)[ :{]" dump.cs
#   => WorldData 367587 · ForceData 369387 · HeroData 369988 · GameController 376432

# หา method เป้าหมายทั้งเกม
grep -n -E "GenerateHero|StartNewGame|GenerateHeroDict|AddTempHero" dump.cs | grep -v -E "RVA:|Offset:"
grep -n -E -i "CustomDifficulty|relaxMode|enemyNum|teammate" dump.cs | grep -v -E "RVA:|Offset:"

# หา section boundary ภายในช่วง class (awk เพราะ pattern มี //)
awk 'NR>=367587 && NR<=369390 && match($0, /^\t\/\/ (Properties|Methods)$/) {print NR": "$0}' dump.cs

# ฟิลด์ที่เก็บ heroID ทั้งเกม
grep -n -E "^\t(public|private|internal) [^=]*[Hh]eroID;" dump.cs

# ระบบอ้าง heroID ข้ามระบบ
grep -n -E "targetHeroID|sourceHeroID" dump.cs | grep -v -E "RVA:|Offset:"

# อ่านช่วงบรรทัดที่ต้องการ
sed -n '376432,377189p' dump.cs   # GameController เต็ม
```

---

## 1) ผลสำรวจ 4 หัวข้อ (ตามรายงานในแชต — ย่อให้ไฟล์นี้สตบองตัวเองได้)

### 1.1 ตัวเกมเอง: GameController / WorldData

**GameController** (TypeDefIndex 8374, dump.cs:376432–377185, DUMP):

| Method | ลายเซ็น | RVA | หมายเหตุ |
|---|---|---|---|
| StartNewGame | `void StartNewGame()` | 0x7E0FC0 | ไม่รับพารามิเตอร์; มี lambda `<StartNewGame>b__7_0` (CompilerGenerated) ⇒ น่าจะทำงานหลายขั้น/หน่วง GUESS; body UNKNOWN |
| StartNewGameSetting | `void StartNewGameSetting()` | 0x7DFB60 | น่าจะเป็นขั้นตั้งค่าก่อนสร้างโลก GUESS |
| GenerateHero | `void GenerateHero()` | 0x7AC8F0 | ไม่รับพารามิเตอร์ ⇒ จำนวนที่สร้างต้องมาจาก field/ตาราง ไม่ใช่ argument (DUMP) |
| GenerateHeroData | 3 overload (ด้านล่าง) | 0x7AC840 / 0x7AC8A0 / 0x7AB1F0 | |
| GenerateForceDataBase | `void GenerateForceDataBase()` | 0x7A95E0 | น่าจะ clone `forceDataBase` → `WorldData.Forces` GUESS |
| GenerateRelationShip | `void GenerateRelationShip()` | 0x7B4400 | น่าจะเป็นตัวเติม Teacher/Students/Lover/Friends GUESS |
| GenerateRandomEnemy | `void GenerateRandomEnemy(int maxNum = 999)` | 0x7B12E0 | ตัวเดียวที่ "จำนวน" เป็นพารามิเตอร์โดยตรง (DUMP) |
| WorldAddNewHero | `HeroData WorldAddNewHero(int forceID, int heroForceLv, bool outSideForce, bool _showInfo = True)` | 0x7E5B50 | สร้าง hero เพิ่มภายหลัง (DUMP) |
| GetMaxHeroNum / GetRecommendHeroNum | `int` | 0x7B9FB0 / 0x7BEED0 | น่าจะอ่าน `GlobalData.MaxHeroNum`/`RecommendHeroNum` GUESS |

**GenerateHeroData ทั้ง 3 overload** (DUMP — ค่า default อ่านได้จากลายเซ็น):

```csharp
// #A RVA 0x7AC840 (ลำดับที่ 1 ใน dump)
public HeroData GenerateHeroData(int heroID, int belongForceID, float heroForceLv,
    HeroData heroDataBase, bool isTempHero = True, bool isRandomEnemy = False)

// #B RVA 0x7AC8A0 (ลำดับที่ 2 ใน dump)
public HeroData GenerateHeroData(string heroName, int heroID, int belongForceID,
    float heroForceLv, bool isRandomEnemy = False)

// #C RVA 0x7AB1F0 (ลำดับที่ 3 ใน dump)
public HeroData GenerateHeroData(string heroName, int heroID, int belongForceID,
    float heroForceLv, HeroData heroDataBase, bool isTempHero, SexLimit sexLimit = 0,
    bool isRandomEnemy = False, bool outSideForce = False)
```

ข้อสังเกต (DUMP): overload #A มี `isTempHero = True` เป็น **ค่า default** ⇒ เปิดทางให้ hero ที่ clone จาก
`heroDataBase` (น่าจะจาก `SpeHeroDataBase`) ถูกผลักเข้า `WorldData.TempHeros` ได้เป็นจำนวนมาก GUESS —
overload ไหนถูกเรียกกี่ครั้งใน StartNewGame เดิมเป็น UNKNOWN (**อัปเดตแล้วใน §2**)

**WorldData** (TypeDefIndex 8181, dump.cs:367587–367865, DUMP):
- ที่เก็บ: `Heros` (0x50) + `HerosDict` (0x268), `TempHeros` (0x58) + `TempHerosDict` (0x278)
  — ฟิลด์ list ทั้งสองมี `[JsonIgnore]` ⇒ เซฟจริงแยกผ่าน `GameSaveData.HeroList`/`TempHeroList`
  พร้อมธง `heroListFinished`/`tempHeroListFinished` (DUMP, dump.cs:366877–366904)
- Methods (DUMP): `GenerateHeroDict()` 0x9CD210 · `FindAvailableHeroID()` 0x9CCCF0 ·
  `FindAvailableTempHeroID()` 0x9CCE70 · `AddTempHero(HeroData)` 0x9CC280 · `RemoveTempHero` ·
  `GetTempHeroCount()` · `AddNewHero(HeroData)` · `RemoveHero` · `GetHeroCount()` ·
  `GetHero(int/string)` · `GetHeroForce(int)` · `GetRandomEnemyCount()` 0x9CEA70
- `GenerateHeroDict` น่าจะแค่สร้าง dict จาก list (ไม่ใช่ตัวสร้าง hero) GUESS (**อัปเดตแล้วใน §2**)

### 1.2 ตาราง/ค่าที่น่าจะกำหนดจำนวน

- **`ForceLevelData.heroNum` (List<int>)** — class นี้มีฟิลด์เดียว (DUMP, dump.cs:369294–369306)
  เป็นตัวสมเป้าสำหรับ "จำนวน hero ต่อระดับสำนัก" GUESS เรื่อง indexing (ต่อ forceLv หรือช่วง) = UNKNOWN
- `GameDataController` (dump.cs:377212–377506): `forceDataBase` (Dictionary<int, ForceData>),
  `SpeHeroDataBase` (Dictionary<int, HeroData>), `forceLevelData` (List<ForceLevelData>),
  `bigForceIDList`, ตารางชื่อ `familyNameDataBase`/`givenNameDataBase`/`maleGivenNameDataBase`/
  `femaleGivenNameDataBase` + `GenerateRandomHeroFamilyName/GivenName/Name` (DUMP ตัวตาราง;
  วิธีใช้ GUESS) · โหลดผ่าน `LoadAllGameData()` (private) + `LTCSVLoader` ⇒ ตารางฐานเป็น CSV
  (DUMP โดยนัยจากลายเซ็น; ตำแหน่งไฟล์ UNKNOWN)
- `GlobalData` (dump.cs:378111+): `MaxHeroNum`, `RecommendHeroNum`, `MinOutforceSimpleHeroNum`,
  `MaxFriendNum`, `MaxLoverNum`, `MaxBrotherNum`, `TempHeroIDStart`, `PlayerForceID`,
  `NoFemaleForce` (List<int>), `SpeNoRandomName`/`SpeMasterName`/`SpeWeakHeroName`/
  `SpeTeammateName` (List<string> — hero พิเศษถูกระบุ **ด้วยชื่อ**) — ค่าตัวเลขจริงอยู่ใน .cctor
  ที่ dump ไม่เห็น ⇒ UNKNOWN
- `GameController` static: `difficultyExtraPoint` (readonly List<int>), `MiddleSelfForceEvent`/
  `HardSelfForceEvent`, `SelfForceEventMinHeroNum` (Dictionary<int,int> — นับ hero ขั้นต่ำของ
  สำนักผู้เล่นเพื่อ trigger event, GUESS) — DUMP ประกาศ, ค่า UNKNOWN
- ความยาก: `WorldData.gameDifficulty` (int, ไม่มี enum ใน dump), `gameMode` (enum Plot=0/Free=1),
  `relaxMode` (bool — มีแหล่งเดียวใน dump ทั้งไฟล์), `TimeDifficulty`, `customDifficultyData`

### 1.3 ForceData และข้อมูลตั้งต้นโลก (DUMP ทั้งหมด)

ฟิลด์จำนวนสมาชิกของ `ForceData` (dump.cs:369387–369675): `ownHeros` (List<int> 0x70) ·
`heroLvNum` (List<int> 0x78 — น่าจะนับ hero ต่อระดับ GUESS) · `totalPopulation` · `totalSalary` ·
`autoAddMember` (bool) · `bigForce` (bool) · `forceLv` · `leader` (int) · `forceMaleRate` (float) ·
`masterForce`/`servantForce` · `forceJobSettingData` (`emptyNum` + `ForceJobs`) ·
static `JoinBigForceNeedSkill`/`JoinBigForceNeedFame` (ค่า UNKNOWN)

Methods ที่ตรวจ/แก้จำนวน (DUMP): `GetMaxHeroNum()` · `PopulationNotFull()` · `AreaNotFull()` ·
`GetMaxAreaNum()` · `AddHero(HeroData)` / `AddHero(HeroData, int _forceLv, int _generation)` ·
`RemoveHero` · `GetOwnHero(int)` · `GetOwnHeros()` ·
`FindAllHero(minForceLv, maxForceLv, noLeader, noJob, noPlayer, noPrison, noSpe)` ·
`GetLeader()/SetLeader()` · `ForceSexLimit()/MeetForceSexLimit(HeroData)` (คู่ enum `SexLimit`
None/Male/Female และ `GlobalData.NoFemaleForce`)

ข้อมูลตั้งต้นโลกที่ป้อนเข้าระบบ: `WorldData.Forces`/`ForcesDict` · `GameSaveData`
(WorldData + HeroList + TempHeroList) · enums `GameMode`, `SpeHeroLimit`, `ForceLimit`,
`ForceLvLimit` (มีค่า `ByDifficulty`), `AreaLimit`, `LeaderLimit` และ `PlotRandomHeroData`
(spec สุ่ม hero ของ plot, dump.cs:369006–369027)

### 1.4 ตัวเลื่อนหน้าตั้งค่าเกมใหม่ (DUMP)

**enum `CustomDifficultyType`** (dump.cs:367515–367530) ครบ 11 ค่า: expRate=0, fameRate=1,
maxweightRate=2, selfforceExpRate=3, otherforceExpRate=4, **randomEnemyStrength=5**,
**randomEnemyNum=6**, badfameRate=7, **maxSkillNum=8**, **teammateLimit=9**,
**aiForceDevelopSpeed=10**

**`CustomDifficultyData`** (dump.cs:367534–367583): เก็บ `Dictionary<int, int> customDifficultyLv`
(key = enum) + API `Get/SetDifficultyLv`, `GetDifficultyRate(type)`, `GetTotalDifficultyLv`,
`CanUnlockAchievement`, `GetExtraMaxTagNum` + static label `customDifficultyName`/
`customDifficultyLvRate`/`teammateLimitName`/`teammateLimitDescribe`

**UI:** `StartGameSettingController` (8582, dump.cs:389981) ถือ `gameDifficulty`, `gameMode`,
`endingTag`, `BirthSetting`, `customDifficultyData` + `StartSettingPlayerData()`/
`BirthSettingPlayerData()` — น่าจะเป็นตัวถ่ายค่าเข้าโลกใหม่ GUESS · `StartMenuController` (8584,
dump.cs:390040) มี `difficultRoot`/`customDifficultyRoot` + handler
`CustomDifficultySliderChanged(GameObject)`, `RefreshDifficultyTotalLv()`,
`RefreshDifficultySliderText(GameObject)`, `EvilChaosSliderChanged(GameObject)` — slider จ่ายเข้า
handler เหล่านี้ GUESS (ผูกผ่าน Unity Inspector ซึ่งไม่อยู่ใน dump)

**ปลายทางที่แต่ละค่าน่าจะควบคุม (GUESS จากชื่อ, body UNKNOWN):**

| Slider | ปลายทางที่น่าจะอ่านค่า |
|---|---|
| randomEnemyNum | `WorldData.GetRandomEnemyCount()` → `GameController.GenerateRandomEnemy(maxNum)` |
| teammateLimit | `HeroData.GetMaxTeamMate()` (คุม `teamMates`) |
| aiForceDevelopSpeed | `WorldData.GetAIForceDevelopSpeed()` |
| maxSkillNum | `GlobalData.MaxSkillNum` (List<float>) |
| expRate/selfforceExpRate/otherforceExpRate/fameRate/badfameRate/maxweightRate | ผ่าน `GetDifficultyRate(type)` — จุดเรียก UNKNOWN |
| randomEnemyStrength | ความแรงศัตรูสุ่ม — จุดเรียก UNKNOWN (พราง `ForceLvLimit.ByDifficulty` + `GetTimeRandomDifficulty()` เป็นเครื่องมือร่วม GUESS) |

หมายเหตุ: `HeroData.GetMaxStudent()` เป็นอีก cap หนึ่ง (ลูกศิษย์) ไม่มีใน enum — แหล่งกำหนด UNKNOWN

### 1.5 ระบบที่อ้าง heroID ของ NPC ข้ามระบบ (เฉพาะ metadata — เสี่ยง dangling reference)

**ฟิลด์ที่เก็บ heroID แบบอิสระ (DUMP):**

| Class | ฟิลด์ | ประเภท |
|---|---|---|
| HeroData | `Teacher` (int), `Students`, `Lover` (int), `PreLovers`, `Relatives`, `Brothers`, `Friends`, `Haters` (List<int>), `teamLeader` (int), `teamMates` (List<int>) | ความสัมพันธ์ |
| ForceData | `leader` (int), `ownHeros` (List<int>) | สังกัด |
| PlotData | `targetHeroID` (int) + `plotRandomHero` (List<PlotRandomHeroData>) | เนื้อเรื่อง |
| MissionData | `sourceHeroID`, `missionHeroID`, `missionJoinTeamHeroID` (List<int>) + `MissionRelateToHero(int)` | ภารกิจ |
| AreaBuildingData | `belongHeroID` | สิ่งปลูกสร้าง |
| BookWriterData | `bookWriterHeroID` | งานเขียนหนังสือ |
| ItemListData | `heroID` | เจ้าของกระเป๋า |
| KungfuSkillLvData | `belongHeroID` | เจ้าของวิชา |
| FavorData | `heroID` | **ไม่มี class ใดอ้างถึงเลยใน metadata** — เสี่ยง orphan/legacy |

**Methods ที่อ่าน/แก้ (metadata, ไม่ใช่ call-site):**
`WorldData.GetHero(int)/GetHeroForce(int)/AddNewHero/RemoveHero/AddTempHero/RemoveTempHero` ·
`HeroData.HaveFriend/AddFriend/RemoveFriend(int)`, `HaveTeacher/GetTeacher/RemoveTeacher`,
`HaveStudent/AddStudent/RemoveStudent/RemoveAllStudent(int)`, `HaveLover/SetLover/RemoveLover`,
`GetRelationShipText(int)`, `HaveRelationBetterThanFriend`, `HaveTeacherStudentRelation`,
`GetMaxStudent`, `GetMaxTeamMate` · `GameController.GetHeroName(int, int)`, `RemoveHero`,
`HeroRealAutoRemove`, `ManageHeroAutoQuitAndEnter`, `ManagePlayerRecruitHero`,
`ManageNewHeroJoinForce`, `GenerateRelationShip` · `ForceData.GetOwnHero/AddHero/RemoveHero` ·
`AIController.AICheckRemoveFriend` · `PartyController.AddFriendToList` ·
`PlotController.DeleteInteractTempHero`, `GetHeroRelationBetterThanFriend` + handler
`FriendVisit`, `FriendAskJoin`, `MakeFriendWithBothNPC`, `LoverAskHeroBreakup/Marry/JoinTeam/
JoinForce`, `StudyFightAllFriendsResult`, `HelpFriendFightHater` ฯลฯ ·
`BigMapController.RemoveBigMapNpc(GameObject, bool isTempHero)`

**ประเมินความเสี่ยงถ้าลดจำนวน NPC (metadata-only, ยังไม่พิสูจน์):**
1. ตารางอ้างด้วย ID จำนวนมาก — ถ้า hero หายโดยไม่ผ่าน `RemoveHero`/`RemoveTempHero`/
   `HeroRealAutoRemove` ของเกม ฝั่งอ้างอิงจะค้างเป็น ID ตาย GUESS (เกมมี helper ตรวจก่อนใช้
   เช่น `HaveFriend`/`MissionRelateToHero`/`GetHero` ซึ่งลดเสี่ยง crash แต่ไม่การันต์ทุก consumer
   ตรวจ UNKNOWN)
2. `PlotData.targetHeroID` อยู่ใน `PlotDataBase` (ตารางนิ่งของเกม) — plot ที่ชี้ heroID ที่ไม่ถูก
   สร้างจะไม่ trigger GUESS
3. บทสนทนา (`SinglePlotData`, dump.cs:382255) ระบุตัวละครด้วย **ชื่อ** (`sourceName`/`targetName`
   string + `PlotTargetHeroType`) ไม่ใช่ ID (DUMP) ⇒ เหนือ ID ยังมี binding ทางชื่ออีกชั้น — ลด NPC
   แบบแตะชื่อ/ชุดสุ่มชื่ออาจพังบทสนทนา GUESS
4. hero พิเศษถูกระบุด้วยชื่อใน `GlobalData.Spe*Name` หลายรายการ — ลดแบบไม่ย่อยรายการพวกนี้จะทำให้
   เงื่อนไข special-hero หาตัวไม่เจอ GUESS
5. ทางที่โครงสร้างสนับสนุนคือลดที่ **ต้นน้ำ** (`ForceLevelData.heroNum`,
   `customDifficultyLv[randomEnemyNum]`) ไม่ใช่ลบของที่สร้างแล้ว GUESS — ต้องยืนยัน runtime

---

## 2) ความสอดคล้องกับหลักฐาน RUNTIME เดิม (`sp0sp7-20261024-225939`, 2026-10-25)

อ้าง: `evidence/sp0sp7-20261024-225939/sp4-creation-chain.md` (hook v3, รอบ 5, Free mode) ·
`evidence/sp0sp7-20261024-225939/sp4-hook-v1-partial.md` (hook v1, รอบ 3) · `sp0-sp7-notes.md` §2

| ข้อของ SP5-prep (ป้ายเดิมจากแชต) | หลักฐาน RUNTIME | ผล | ที่อ้าง (ไฟล์:บรรทัด) |
|---|---|---|---|
| "overload ไหนถูกเรียกกี่ครั้ง = UNKNOWN" | `GenerateHeroData#2` ×**963** (รอบ 5) แต่ ×**0** (รอบ 3) — args: `name=null, heroID=1..N, belongForceID(0..29/−1/−2), heroForceLv float, ptr…` | **ตอบได้บางส่วน** — มี overload เดียวเป็น NPC factory; แต่ "#2" → ลายเซ็นไหน (B หรือ C) ยัง INFER ดูหมายเหตุ (ก) ด้านล่าง | sp4-creation-chain.md:8–9, 31–33 · sp4-hook-v1-partial.md:17 |
| "`GenerateHeroDict` น่าจะแค่สร้าง dict จาก list (GUESS)" | ถูกเรียก 1 ครั้ง หลัง factory 963 จบ (t+1023) ภายใน `GenerateHero`; บันทึกว่า "สร้าง dict heroID→HeroData" | **รองรับ** (จุดเวลา + บทบาท) — ป้ายยังคง GUESS เพราะไม่มี body แต่ตอนนี้มี RUNTIME หนุน | sp4-creation-chain.md:11–12, 20 |
| "963 น่าจะมาจากลูปสมาชิกต่อสำนัก (GUESS)" | การกระจาย `belongForceID`: สำนัก 0–29 (≈17–35 คน/สำนัก; 仙霞派=25 มี 17) · −1 ×186 · −2 ×21 (รวม 963; ในสำนักรวม 756 = INFER ลบเลข) | **รองรับเชิงตัวเลข** — วัดต่อสำนักแล้ว; กลไกว่าใช้ `ForceLevelData.heroNum` จริงไหม = ยัง UNKNOWN | sp4-creation-chain.md:22–23 |
| "AddTempHero ผลักเข้า `TempHeros` (GUESS)" | `AddTempHero = 81 (= TempHeros list)` | **รองรับตรง ๆ** — ป้ายนี้ปรับเป็น RUNTIME-รองรับได้ | sp4-creation-chain.md:14, 21 |
| "TempHero แกว่ง 81–88 น่าจะสุ่มระหว่าง gen (GUESS)" | 88 (รอบ 1) / 88 (รอบ 3) / 81 (รอบ 5) — สรุปไว้ว่า "การสุ่มของโลก (INFER)" | **รองรับ + ยกระดับเป็น INFER จาก RUNTIME** | sp4-creation-chain.md:39 · sp4-hook-v1-partial.md:15 |
| "heroDataBase → clone จาก `SpeHeroDataBase` (GUESS)" | arg ที่ 5 ของ factory เห็นเป็น "ptr…" — อาจเป็น `heroDataBase` (overload C) หรือแค่ bool ของ overload B | **ยัง UNKNOWN** — ไม่มีข้อมูลหนุน/หักล้าง | sp4-creation-chain.md:9 |
| "`GenerateRelationShip` เติม Teacher/Students/Lover/Friends (GUESS)" | ไม่อยู่ใน chain ที่ hook | **ยัง GUESS/UNKNOWN** — ไม่มีข้อมูลหนุน/หักล้าง | — |
| "`GenerateRandomEnemy` รับจำนวนจาก `GetRandomEnemyCount()` (GUESS)" | `GenerateRandomEnemy` ไม่ได้อยู่ใน hook list ของรอบ SP4 | **ยัง GUESS/UNKNOWN** — ไม่มีข้อมูลหนุน/หักล้าง | sp4-hook-v1-partial.md:7–20 (รายการที่ hook) |
| — (ไม่ได้ถามในแชต) ตัวเอก heroID 0 | **ไม่พบ**ใน chain ที่ hook (ไม่มี call heroID=0) — GUESS เดิม: สร้างที่หน้าจอสร้างตัวละครก่อน StartNewGame (สอดคล้อง ctor-burst ก่อน StartNewGame ~1.4 s) | บันทึกเสริมสำคัญ: 963 ไม่รวมตัวเอก | sp4-creation-chain.md:27–30 · sp4-hook-v1-partial.md:28–29 |
| — (ไม่ได้ถามในแชต) ขนาดโลก default | Heros 883 + TempHeros 88 + Forces 30, gameDifficulty=0 | สอดคล้องกับ forceID 0..29 ของ factory | sp0-sp7-notes.md:43 · sp4-hook-v1-partial.md:41 |
| — (ไม่ได้ถามในแชต) บัญชีตัวเลขยังไม่ปิด | `HeroData..ctor` 886 vs Heros(883)+Temp(88)=971 ต่าง 85; 883 ตัวเข้า `Heros` **ไม่ผ่าน** `AddNewHero`/`WorldAddNewHero`/`GenerateHeroData` ⇒ list ถูก assign ทั้งชุดได้ | ยัง UNKNOWN (ข้อค้าง SP4) — กระทบการนับของ SP5 โดยตรง | sp0-sp7-notes.md:42 · sp4-hook-v1-partial.md:24–27 |
| — (ไม่ได้ถามในแชต) path ที่สองของการ gen | รอบ 3 factory = 0 ครั้งแต่ Heros≈883 เท่าเดิม ⇒ มี pool/cache ภายใน process เดียว | **คำเตือนต่อ SP5:** จำนวน call ของ `GenerateHeroData` ไม่ใช่ตัววัดจำนวน hero ที่น่าเชื่อถือเมื่อ gen ซ้ำใน process เดียว | sp0-sp7-notes.md:77 (§5 ข้อ 5) · sp4-creation-chain.md:31–33 |

**หมายเหตุ (ก) — การจับคู่ "#2" กับลายเซ็น:** hook รอบ SP4 ครอบ GenerateHeroData ทั้ง 3 overload
(sp4-hook-v1-partial.md:17) และแท็ก "#2" — สคริปต์ hook รอบนั้นไม่ถูกเก็บไว้ใน tools/ หรือ evidence/
(ค้นแล้ว 2026-10-26) จึงพิสูจน์ลำดับการนับไม่ได้ ถ้า "#2" อ้างลำดับ declaration ใน dump (สมมติฐานที่
เป็นไปได้ที่สุด) ⇒ เป็น overload **#B `(string heroName, int heroID, int belongForceID, float
heroForceLv, bool isRandomEnemy = False)` RVA 0x7AC8A0** — แต่ arg "ptr…" หลัง heroForceLv ใน log
ยังเข้ากันได้ทั้ง overload B (bool) และ C (`HeroData heroDataBase`) ⇒ **การ mapping ที่แน่นอน = INFER
รอยืนยัน** (hook ที่ log ลายเซ็นครบทุก arg หรือ static ถอด body)

**สรุปการขัดแย้งกับรายงานแชต:** **ไม่พบข้อขัดแย้ง** — ข้อที่ป้ายต้องแก้/ยกระดับมี 3 ข้อ และแก้แล้ว
ตามตาราง: (1) "AddTempHero ผลักเข้า TempHeros" GUESS → RUNTIME-รองรับ, (2) "GenerateHeroDict สร้าง
dict จาก list" GUESS → GUESS ที่มี RUNTIME หนุน (จุดเวลา t+1023 หลัง factory), (3) "overload ไหนถูก
เรียก" UNKNOWN → ตอบได้ว่ามี overload เดียว ×963 (ชื่อ hook `#2`) เหลือแค่ mapping "#2"→ลายเซ็นเป็น INFER

---

## 3) ข้อสังเกตจากภาพหน้าตั้งค่าเกม

- **SCREENSHOT-USER (จากภาพที่ผู้ใช้ให้ — ไม่ใช่ runtime และไม่ใช่ DUMP):** หน้าตั้งค่าเกมใหม่มี
  **ตัวเลื่อน 11 ตัว** ตรงกับ **`CustomDifficultyType` ครบ 11 ค่า** (expRate, fameRate, maxweightRate,
  selfforceExpRate, otherforceExpRate, randomEnemyStrength, randomEnemyNum, badfameRate, maxSkillNum,
  teammateLimit, aiForceDevelopSpeed) — การจับคู่รายตัวระหว่าง slider กับ enum ยังเป็น GUESS (ผูกผ่าน
  Unity Inspector ไม่อยู่ใน dump)
- **และไม่มีตัวเลื่อน "จำนวน NPC ของโลก"** (จำนวนสมาชิกสำนัก/hero ฐานของโลก) ในหน้านั้น — รวมกับ DUMP
  ที่ตัวเลขจำนวนมาจากตาราง (`ForceLevelData.heroNum` ฯลฯ) ⇒ จำนวน NPC ฐานของโลกไม่ได้ถูกควบคุมด้วย
  slider ปัจจุบัน **INFER** (สรุปจาก DUMP + SCREENSHOT-USER ร่วมกัน — ยังไม่พิสูจน์ runtime)

---

## 4) คำถามเปิด / ขั้นถัดไปที่ต้องอนุมัติแยก (ห้ามเริ่มเอง)

1. **อ่าน method body แบบ static** — ถอด assembly ด้วย ILSpy/dnSpy บน dummy DLL จาก dump.cs +
   Il2CppAssemblies (ไม่เปิดเกม) เพื่อปิด: flow ของ `StartNewGame`→`GenerateHero`, indexing ของ
   `ForceLevelData.heroNum`, mapping overload factory, จุดเรียก `GetRandomEnemyCount()`
2. **hook log-only บนสำเนาเซฟ** — log ลายเซ็นครบทุก arg ของ `GenerateHeroData` ทั้ง 3 overload +
   `AddTempHero` + `GenerateRelationShip` + `GenerateRandomEnemy` หนึ่ง creation round (ปิด mapping
   "#2" และบัญชี 886 vs 971)
3. **ตำแหน่งตาราง CSV** ที่ `LoadAllGameData()` อ่าน (ไฟล์/asset ไหน — น่าจะ StreamingAssets) เพื่อ
   อ่านค่า `heroNum` และค่าคงที่ `GlobalData` โดยไม่ต้อง hook
4. กลไก pool/cache ของ creation path ที่สอง (SP4 §5 ข้อ 5) — กระทบการออกแบบ SP5/H7

## 5) บันทึก purity ของรอบ (ตรวจก่อน/หลังเขียน)

**ก่อนเขียน (2026-10-26 22:22:24 +0700):**
- เกมไม่รัน — `tasklist` พบแค่ `il2cpp-frida-mcp.exe` (pid 25676, MCP server ของเครื่องมือ ไม่ใช่เกม);
  `LongYinLiZhiZhuan.exe` ไม่พบ
- hash เกม 2 ไฟล์ **ตรง** `VERSION.md` (GameAssembly `33c56def…7920c1`, metadata `451311d4…18bfd0`)
- MelonLoader log ไฟล์ล่าสุด = `MelonLoader\Logs\26-10-26_1-37-6.log` (01:37 ของวันนี้ — **เก่ากว่า**
  จุดเริ่มรอบเขียนนี้) ⇒ ไม่มี log ใหม่จากรอบนี้
- git status: game repo = ตรง snapshot ตอนเริ่ม conversation (M STAGEPLAN.md · D battle-notes.md ·
  m il2cpp-frida-mcp · D worldmap-notes.md · untracked 7 รายการ) — ไม่เปลี่ยน · mod repo HEAD
  `dd13491` (สกปรกอยู่แล้วก่อนรอบนี้ — untracked/modified 60 รายการ เก็บ snapshot ไว้เทียบ)
- ไฟล์ที่เขียนรอบนี้ = `evidence/SP5prep-20261026-2222/sp5-prep-notes.md` **ไฟล์เดียว** (สร้างใหม่)

**หลังเขียน (2026-10-26 22:26:05 +0700, ตรวจจริงแล้ว):**
- git status diff เทียบ snapshot ก่อนเขียน (เก็บที่ /tmp/sp5prep-{mod,game}-status-before.txt):
  mod repo ผลต่าง = `?? evidence/SP5prep-20261026-2222/` **รายการเดียว** · game repo = no change
- เกมยังไม่รัน (`LongYinLiZhiZhuan.exe` ไม่พบใน tasklist) · MelonLoader log ล่าสุดยังเป็น
  `26-10-26_1-37-6.log` (ไม่มี log ใหม่)

---

*สร้างโดยงานสำรวจ DUMP-only ตามคำสั่งผู้ใช้ · ห้าม commit เอง — รอคำสั่ง docs-only แยก (ไม่ใช้ `git add -A`)*
