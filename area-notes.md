# area-notes — Area / UI survey (read-only)

 session: 2026-10-22 · label `area-20261022-0615`
 วิธี: `il2cpp-frida-mcp` (frida-il2cpp-bridge, typed field access) + `dump.cs` · **0 memory write · 0 state-changing method · 0 ปุ่มที่ผมกดแทนคุณ**
 เกม: hash ตรง `VERSION.md` ทั้ง 2 ไฟล์ · Unity 2020.3.48f1c1 · MelonLoader 0.7.3 · mod build `cb35a93f…` (S2a-2)
 เซฟ: `Save.real-area-20261022-0615` (aside, ไม่ถูกเขียน) + `Save` = สำเนา · baseline `coop-test/baselines/20261022-061550` (rebaseline volatile-only: `PlayerprefData.dat` ไฟล์เดียว)
 หลักฐานดิบ: `evidence/area-20261022-0615/dump-extract.txt` (41 class จาก dump.cs), `coop-test/area-20261022-0615/` (MANIFEST, savehash, history)
 ป้าย: **RUNTIME** = อ่านจาก process ที่รันอยู่ · **DUMP** = ลายเซ็นจาก `dump.cs` · **GUESS** = ยังไม่วัด

สภาพโลกตอนเริ่มวัด: เวลา `10/9/4 h18.39` → หลังวัดจบ `10/9/19 h18.394` (+15 วัน จากการทำงาน 10 วัน + Wait 5 วัน **บนสำเนาเท่านั้น**) · `Heros=897` (883 live), `TempHeros=223`, `Areas=69`, `Forces=30` **[RUNTIME]
ตัวเอก: `heroID=0` "หวังเวิ่นเทียน" · `atAreaID=55` 仙霞派 · `belongForceID=25` · `heroForceLv=5` **[RUNTIME]**

---

## P1-1 Area — class, controller, ความเชื่อมกับ `HeroData.atAreaID`, ครอบครอง vs ไม่ครอบครอง

| ชั้น | อะไร | หลักฐาน |
|---|---|---|
| data | `AreaData` (`areaID`, `areaName`, `areaType`, `areaStartLv`, `belongForceID`, `bigMapPos`, `people/maxPeople`, `safe/support/defence`, `insideHeros`, `changeResource`, `resourceValueRateBase/Temp`, `areaTiles`, `areaTreasurePriceData`, `areaInteractionTimeData`, `branchLeaderID`, `areaBranchDefenceLv/UpgradeLeftTime`, `thisMonthManaged`, `missionNumCount`, `plotNumCount`, `autoBuild*`) | DUMP |
| data (โลก) | `WorldData.Areas` (69), `AreasDict`, `Forces` (30), `ForcesDict`, `Heros` (897), `HerosDict`, `ResourcePoints`, `worldTime`, `hour` | RUNTIME |
| data (static DB) | `GameDataController.areaDataBase` `Dictionary<int,AreaData>` + `forceDataBase` + `buildingDataBase` | DUMP |
| controller หน้าจอ | `AreaController` (`areaData`, `gridUnits`, `areaGrid`, `heroIconGrid`, `buildingIconPanel`, `areaUIBelow`, `treasurePriceGrid`, `resourcePointUIGrid`, `areaLog`) | DUMP |
| เข้าหน้า Area | `AreaController.GenerateAreaMap(AreaData)` / `ReturnBigMapButtonClicked()` / `PlayerLeaveArea()` | DUMP |

- ความเชื่อม: ตัวละครมี `HeroData.atAreaID` (-1 = wilderness) — ตอนวัดได้ 55 **[RUNTIME]**
- **ข้อพิสูจน์ว่า data class กับหน้าจอเป็นตัวเดียวกัน**: `AreaController.inited=true`, `areaData.areaID=55`, `nowScale=1.2` ขณะที่ `worldData.Areas` ให้ 69 รายการ และ `AreasDict` ครบ **[RUNTIME]**
- ครอบครอง vs ไม่ครอบครอง = `AreaData.belongForceID` เท่านั้น (`0` = ไม่มีเจ้าของ, `>0` = forceID) — ไม่มีธงแยกอื่น **[RUNTIME]**
  - ตัวอย่างของเรา: id55 仙霞派 `belongForceID=25` (type 2 = สำนัก) · `ForceData(25).ownAreasID=2`, `ownHeros=18`, `leader=0` (ตัวเอกเป็นเจ้าสำนัก) **[RUNTIME]**
  - ตัวอย่างไม่มีเจ้าของ: id1 杭州 `belongForceID=0`, type 0, people 2600/2600, `safe 99.34`, `support 96.63`, `defence 100`, `areaStartLv=3`, tiles 225 **[RUNTIME]**
  - ตัวอย่างของ force อื่น: id2 福州 `belongForceID=9`, people 2500/2500, `safe 96.8`, `support 100`, `defence 100`, `areaStartLv=2` **[RUNTIME]**
- **ประเภท Area เปลี่ยนความหมายของ field**:สำนักของตัวเอง (type 2) มี `people=maxPeople=0` และ `safe/support/defence=0` ทั้งหมด ⇒ ค่าเหล่านี้เป็นของ city/village ไม่ใช่ของสำนัก **[RUNTIME]**
- สรุปการกระจายเจ้าของ 69 Area: `belongForceID=0` 3 แห่ง · ของ force 1–29 · ของเรา (25) 2 แห่ง **[RUNTIME]**

## P1-2 ข้อมูลที่แสดงบนหน้า Area → field ต้นทาง

| สิ่งที่เห็น | field | หลักฐาน |
|---|---|---|
| ชื่อ Area | `AreaData.areaName` | RUNTIME |
| ระดับสิ่งปลูกสร้าง | `AreaTileData.building` → `AreaBuildingData.lv` (ต่อ tile) | RUNTIME |
| population | `AreaData.people` / `maxPeople` (city/village เท่านั้น) | RUNTIME |
| safe/support/defence | `AreaData.safe` / `support` / `defence` | RUNTIME |
| ทรัพยากร +ต่อวันของ Area | `changeResource` (6 ค่า) × `resourceValueRateBase` (+`resourceValueRateTemp`) | RUNTIME (ค่า) / GUESS (สูตรคูณ) |
| ของ force (เงิน/เสบียง/ไม้/แร่/สมุนไพร/บารมี) | `ForceData.resourceStore` / `resourceChange` / `resourceStoreMax` | RUNTIME |
| เจ้าของ | `AreaData.belongForceID` → `ForceData.forceName` | RUNTIME |
| ราคา (treasure price) | `AreaData.areaTreasurePriceData` (`treasureType`, `expensive`) + `AreaController.FreshAreaTreasurePriceGrid()` | DUMP |
| โหนดทรัพยากร | `WorldData.ResourcePoints` (`resourcePointID/TypeID`, `belongForceID`, `connectAreaID`, `changeResource`, `thisMonthExplored`) + `AreaController.FreshResourcePointUIGrid()` | DUMP |
| ลิสต์ตัวละคร | `heroIconGrid` ← `HeroData.atAreaID` (ดู P1-3) | RUNTIME |
| ลิสต์สิ่งปลูกสร้างซ้าย | `buildingIconPanel` ← `AreaData.areaTiles[].building` | DUMP+RUNTIME |

ค่าที่วัดได้ (area55): `changeResource=[370,-120,210,175,25,55]`, `rateBase=[0.8,1.3,1,1,1.2,1.1]`, `rateTemp=[0,0,0,0,0,0]`, tiles 169, buildings จริง 22 หลัง (lv 0–10) **[RUNTIME]**

## P1-3 ลิสต์ตัวละครใน Area — คำนวณจากอะไร

- `AreaData.insideHeros` = `List<int>` heroID ของคนใน Area นั้น **[RUNTIME]**
- **ตรวจแล้ว**: area55 `insideHeros` มี 11 รายการ และ hero ที่ `atAreaID==55` มี 11 ตัว — **เป็น set เดียวกัน** (RUNTIME)
- ⇒ ลิสต์คำนวณจาก `HeroData.atAreaID` (not a separate ownership structure) — `insideHeros` เป็นดัชนีที่เกม sync ให้ ไม่ใช่แหล่งความจริงแยก **[RUNTIME]**
- รูป/ตำแหน่ง/สถานะ ที่ใช้แสดง: `HeroData.faceData` (`HeroFaceData`), `bigMapPos`, `inSafeArea`, `inPrison`, `dead`, `hp`, `heroID/heroName/heroNickName` **[DUMP]**; ตัวคุมไอคอนคือ `AreaUnitController`/`BigmapNpcController` (`heroData @0x18`) **[DUMP + worldmap-notes]**
- จำนวนคนใน Area ไม่ผูกกับเพดานปาร์ตี้ (insideHeros 11 > maxTeamMate 6) **[RUNTIME]**

## P1-4 interactive list → handler → method จริง (และอันไหนกินเวลาโลก)

**กติกาการจับคู่**: ปุ่มทุกปุ่มของอาคาร/หน้าจอ ถูกสร้างจาก `AreaBuildingChoice { text, describe, mainCondition[], subCondition[], justOneCondition, callFuc, callFucParam }` และ `SinglePlotChoiceData { choiceText, callFuc, callParam, requirements, relations, costResource, playerInteractionTimeNeed }` ⇒ **ชื่อ method อยู่ในข้อมูล ไม่ต้องเดาจาก UI** **[DUMP]**

ปุ่มจริงที่อ่านได้จากหน้าจอ (อาคาร `buildingID=0` 正厅 lv4, 7 ปุ่ม) **[RUNTIME]**

| ปุ่ม | callFuc |
|---|---|
| 门派弟子 | `ShowForceHero` |
| 门派职位 | `ManageForceSetting` |
| 弟子方针 | `ManageForceHeroSetting` |
| 门派科研 | `ShowResearch` |
| 门派委托 | `ShowBuildingMission` |
| 挥师出征 | `LeaderManageForceAttack` |
| 外交派遣 | `LeaderInteractOtherForce` |

ตาราง static 76 อาคาร (`AreaBuildingDataBase.areaBuildingChoices`) อ่านครบที่ runtime **[RUNTIME]** — ตัวอย่างที่เกี่ยวกับเวลาโลก:

| อาคาร | ปุ่ม → method | กินเวลาโลก? |
|---|---|---|
| 练武场 (2) | 练习 `StudyFightSelf`, 切磋 `StudyFightOther` | ใช่ (ฝึกยาวหลายวัน ผ่าน `WorkingUIController`) **[DUMP]** |
| 闭关室 (3) | 修炼 `StudyInternalSelf`, 突破 `BreakThroughSkill`, 天赋 `ManageTag` | ใช่ **[DUMP]** |
| 农田/森林/矿场/药园/钱庄 (11–14,10) | 耕作/砍伐/挖掘/采收/经营 `ProduceBuildingWork`, 育种/勘探 `RecoverBuildingResourceRate`, `ProduceBuildingSteal` | ใช่ **[DUMP]** |
| 客栈 (17) | 住宿 `HotelRest`, 打杂 `SimpleWork`, 义诊 `AreaBuildingWork` | ใช่/ไม่ใช่ ต้องวัดแยก **[GUESS]** |
| 祠堂 (4) | 祭典 `ProduceBuildingWork`, 鉴定 `IdentifyItem`, 宴会 `StartForceParty` | ใช่ **[DUMP]** |
| 藏经阁 (1) | 阅读 `ShowBookStore`/`ChooseReadBook`, 管理 `ManageBookStore`, 盗窃 `StealForceSkill` | อ่าน/เลือกไม่กินเวลา, อ่านหนังสือกิน **[GUESS]** |
| 仓库/宿舍/疗伤室 | `ManageForceMoney`, `OpenSelfStorage`, `HomeRest`, `HospitalCureInjuryForce`, `DoctorWork` | ปนกัน **[GUESS]** |

- ปุ่มที่เป็น **UI ล้วน ไม่แตะโลก**: `Show*`/`Manage*`/`Open*` (เช่น `ShowForceHero`, `OpenSelfStorage`) **[DUMP]**
- ปุ่มที่ **เปลี่ยนโลกทันที**: `*Steal*`, `*Rob*`, `LeaderManageForceAttack`, `LeaderInteractOtherForce`, `GovernmentClearBadFame`, `StartForceParty` **[DUMP]**
- ปุ่มที่ **กินเวลาเป็นวัน** = ตัวที่ไปลงที่ `WorkingUIController.StartWorking(dayNum, …)` หรือ `GameController.ChangeDay(n)` **[DUMP]**

## P1-5 ตัวเลือก 1/5/10 วัน — ที่เก็บ, ส่งเข้า method ไหน, เวลาเดินยังไง

**ยืนยันแล้ว [RUNTIME]**
- ตัวควบคุมงานหลายวันคือ `WorkingUIController` มี field: `working`, `workName`, `workingFuc`, `workingFucParam`, `finishFuc`, `finishFucParam`, `leftWorkingDay`, `totalWorkingDay`, `nextDayTime`, `leftPauseTime`, `resourceNum`, `workFinished`, `noCancel`, `skipping`, `skipButton`, static `dayTime`/`pauseTime`
- ตอนวัด: `working=false`, `totalWorkingDay=0`, `leftWorkingDay=0`, `skipping=false`, `noCancel=false` ⇒ **ไม่มีงานค้างในเซฟนี้**
- method รับงาน: `StartWorking(int dayNum, string callFuc)`, `StartWorking(int dayNum, string callFuc, string callFucParam, string finishCallFuc, string finishCallFucParam)`, `StartWorking(string _workName, int dayNum, …)`, `StartWorking(List<string> paramList)` **[DUMP]**
- ยกเลิกกลางคัน: `TryCancelWorking()` + `noCancel` flag (งานบางอย่างตั้ง `noCancel=true`) **[DUMP]**
- ปุ่มเร่ง: `skipButton` + `skipping` (ยังไม่กดวัด) **[DUMP, NOT-RUN]**
- ทางเข้าที่แพ็ค "ชื่องาน;จำนวนวัน;…" เป็นสตริงเดียว: `PlotController.StartWorking(string param)` โดย `param = workName;days;callFucParam;finishCallFuc;finishCallFucParam` — ตัวอย่างจริงที่อ่านได้ **[RUNTIME]**:
  - `巡查;3;;;HelpPatrol;` · `配置药品;2;;;FinishHelpCureDisease;` · `诊治病人;3;…` · `打杂;5;…` · `指点;3;;;FinishHelpStudy;` · `工作;3;;;FinishHelpWork;` · `捉贼;3;;;TryCatchThief;`
  - ⇒ **จำนวนวันฝังอยู่ใน callParam** ไม่ได้เก็บเป็น field แยกของ UI

### ✅ วัดงาน 10 วันจริงแล้ว (ผู้ใช้กดบนสำเนา, 2026-10-22)

| สิ่งที่วัด | ค่า | หลักฐาน |
|---|---|---|
| ชื่องาน / method | `workName="挖掘"`, `workingFuc="WorkInProductionBuilding"`, `workingFucParam="3_1"` (buildingID 3) | RUNTIME |
| **ที่เก็บจำนวนวัน** | **`WorkingUIController.totalWorkingDay = 10`** (ค่าที่เลือก) และ `leftWorkingDay` ไล่ 10→0 | RUNTIME |
| อัตราเวลา | static `WorkingUIController.dayTime = 1` (วินาที/วัน), `pauseTime = 0.25` ⇒ 10 วัน ≈ 12.5 วิ และ **จบเองโดยไม่ต้องกด >>** | RUNTIME |
| ผลต่อปฏิทิน | `10/9/4` → `10/9/14` (+10 พอดี) · `hour` คงที่ `18.394` ⇒ **วันเปลี่ยนโดยไม่รีเซ็ตชั่วโมง** | RUNTIME |
| วิธีเดินเวลา | ไล่ทีละวันใน `WorkingUIController.Update` (1 วิ/วัน) ⇒ `GameController.ChangeDay` ทีละ 1 (ไม่ใช่กระโดด 10 ทีเดียว) | RUNTIME (พฤติกรรม) / DUMP (`ChangeDay(int)`) |
| ทรัพยากรสำนัก | `store [6240,4700,4900,2844.9,4900,2450] → [6896,5214.2,4575,3841.3,4900,2450]` = Δ `+656, +514.2, −325, +996.4, 0, 0`; `change[1]` `+20.5 → −3.6` | RUNTIME |
| อาคารที่ทำงาน | `buildingID=3, lv=4, produceRate=1.0, resourceStoreRate=1.8, belongHeroID=-1` | RUNTIME |
| ยกเลิก | ยังไม่วัด (`TryCancelWorking()` + `noCancel` flag) | NOT-RUN |
| **`>>` (skip) — รอบ 2 (06:44, 2 samples)** | `t1` (กลาง skip): `day=4`, `total=9`, `left=1`, `skipping=true`, `workFinished=false`, `working=true` → `t2` (+5 วิ): `day=5`, `total=10`, `left=0`, `skipping=true`, `workFinished=true`, `working=false` · `store[0]` = `7085.4` เท่ากันทั้ง 2 sample (baseline day4 = `6240`) ⇒ **skip จ่ายผลผลิตครบตามจำนวนวัน แต่ปฏิทินเดินเพิ่มเพียง +1 วัน** (ตรงกับรอบแรกที่ +1) | RUNTIME (2 รอบผลตรงกัน) |
| **`>>` (skip) — รอบ 1 (06:39)** | วัดแล้ว 2026-10-22 06:39 (บน **เซฟจริงโดยไม่ตั้งใจ** — ดู incident ด้านล่าง): หลังกด `>>` ที่งาน 10 วัน → `leftWorkingDay=0`, `workFinished=true`, `working=false`, **`skipping=true` (ค้างไม่รีเซ็ต)**, `leftPauseTime=-0.007` **แต่ปฏิทินเดินเพิ่มเพียง +1 วัน (`10/9/4`→`10/9/5`)** ขณะที่ `totalWorkingDay=10` ⇒ **skip ไม่เดินวันให้ครบตามจำนวนงาน** · ทรัพยากรสำนักเปลี่ยน `[6240,4700,4900,2844.9,4900,2450] → [5420.4,4933.7,5378,4524.4,5099.4,2450]` (Δ `−819.6,+233.7,+478,+1679.5,+199.4,0`) | RUNTIME (ยังต้องวัดซ้ำแบบคุมตัวแปร) |

- ตัวเลือกจำนวนวัน (1/5/10) เป็น **UI ของเกมที่สร้างตอนรัน** — ไม่มีในข้อมูล static (`AreaBuildingChoices`/`PlotData` ให้ค่าคงที่ 2/3/5) ⇒ เลือก 10 แล้วได้ `totalWorkingDay=10` **[RUNTIME]**; รายการตัวเลือกที่มีให้เลือกจริง (1/5/10) เป็นสิ่งที่ **ผู้ใช้เห็นเอง** = GUESS จนกว่าจะวัดทีละปุ่ม
- ระหว่างรอ: โลกเดิน AI รายวันผ่าน `AIController.ManageAIOneDay(HeroData)` + `BuildingUIController.AreaBuildingWork` per-area **[DUMP + worldmap-notes §]***/

## P1-6 ฝึกวิชา (ChoosePanel)

- UI เลือก: `ChooseController.ShowChoosePanel(ChooseType, List<HeroData>|List<object>, GameObject target, string sendResultFuc, string sendResultParam, ChooseFilterType, string cancelFuc)` + `ChooseType = { HeroSkill=0, HeroItem=1, Hero=2 }` ⇒ **เลือกวิชา = `ChooseType.HeroSkill`** **[DUMP]**
- ไอคอนในลิสต์: `SkillIconController { skillListID, skillLvData, skillIconType, OnClick() }`, `SkillIconType = { Equip, UnEquip, Show, Use, Choose }` **[DUMP]**
- ฝั่ง logic: `PlotController.RealStartStudySkill()`, `GetSkillMartialClubStudyTime(KungfuSkillLvData)` → int (จำนวนวัน), `GetSkillMartialClubStudyMoney(...)`, `StudyMartialClubSkillStart/Choose/Sure/Finish`, `StudyFightSelfChoose(skillType)` / `StudyFightSelfFinish`, `StudyLivingSkillStart/Finish` **[DUMP]**
- ใช้ยูนิตไหน: `PlotController.sourceInteractHero` / `targetInteractHero` / `plotInteractHeroList` (ผู้เรียน = `sourceInteractHero` ของ interaction นั้น) **[DUMP]**
- กลไกเวลา: ใช้ `WorkingUIController` ชุดเดียวกับข้อ 5 (`GetSkillMartialClubStudyTime` = dayNum ที่ส่งเข้า `StartWorking`) **[DUMP]**

### ✅ วัดฝึกวิชาจริงแล้ว (ผู้ใช้กดที่ลานฝึก → เลือกวิชาหมัด, 2026-10-22)

| สิ่งที่วัด | ค่า | หลักฐาน |
|---|---|---|
| ชนิดการเลือก | `ChooseController.chooseType = **HeroSkill**` (ChooseType 0), `inited=true` | RUNTIME |
| callback ที่ถูกตั้ง | `sendResultFuc = "StudyFightSelfChoosen"`, `sendResultParam = "false"` | RUNTIME |
| ผู้เรียน | `ChooseController.targetHero = heroID 0` (**ตัวเอก**) — ไม่ใช่สมาชิกทีม | RUNTIME |
| ชื่อวิชา | `WorkingUIController.workName = "练习<color=#9A7CFF>劈金碎玉掌</color>"` | RUNTIME |
| **จำนวนวัน** | `totalWorkingDay = 2` (วิชานี้ใช้ 2 วัน) · `leftWorkingDay` 0 หลังจบ · `working=false` | RUNTIME |
| ผลต่อปฏิทิน | `10/9/19` → `10/9/21` (**+2 พอดี**), `hour` คงที่ `18.394` | RUNTIME |
| `workingFuc` | **null** สำหรับการฝึก (ต่างจากงานผลิตที่ `"WorkInProductionBuilding"`) ⇒ ฝึกวิชาไม่ได้ใช้ field นี้ **[GUESS: ใช้ coroutine/callback ของ PlotController]** | RUNTIME (ค่า null) / GUESS |
| `ChoosePanel` | ยัง `activeSelf=true` หลังจบการฝึก (ไม่ปิดเอง) | RUNTIME |
- สรุป: **ฝึกวิชาใช้กลไกเวลาเดียวกับงานหลายวัน/WaitTime** (`WorkingUIController.totalWorkingDay` + `dayTime=1 s/วัน`) ⇒ S4c ครอบเดียวได้
- ตัวควบคุมอาคารที่เรียก: `BuildingUIController.StudyFightSelf/StudyFightOther/StudyInternalSelf/StudyLivingSkill(param)/StudyMaxFightSkill(param)/StudyMaxState(param)` **[DUMP]**

## P1-7 ปุ่ม WaitTime

- controller: `WaitUIController { waitUIPanel, waitTimeText, waitTimeSlider }` + `ShowWaitUI()`, `SliderValueChanged()`, `RefreshWaitUI()`, `SureButtonClicked()`, `UnshowWaitUI()` **[DUMP]**
- **ช่วงค่า [RUNTIME]**: `waitTimeSlider` min **1**, max **30**, `wholeNumbers=true` ⇒ สไลเดอร์ 1–30 วัน ไม่ใช่ 1/5/10

### ✅ วัด Wait 5 วันจริงแล้ว (ผู้ใช้กดบนสำเนา)

| สิ่งที่วัด | ค่า | หลักฐาน |
|---|---|---|
| ค่าที่ส่งผล | `10/9/14` → `10/9/19` (**+5 พอดี**), `hour` คงที่ `18.394` | RUNTIME |
| **กลไกเดียวกันกับงาน** | หลังกด: `WorkingUIController.totalWorkingDay = 5`, `leftWorkingDay = 0`, `working = false` ⇒ **WaitUI ลงเอาที่ `WorkingUIController` ชุดเดียวกับงานหลายวัน** (ไม่ใช่ `ChangeDay` ตรง ๆ จาก UI) | RUNTIME |
| ทรัพยากรสำนัก | `store [6896,5214.2,4575,3841.3,4900,2450] → [7351,5449.8,4575,3841.3,4500,2450]` = Δ `+455, +235.6, 0, 0, −400, 0` | RUNTIME |
| `AreaData.changeResource` / `rateTemp` | **ไม่เปลี่ยน** (`[370,-120,210,175,25,55]` / `all 0` ทั้งก่อนและหลัง) | RUNTIME |
| `areaInteractionTimeData` | **ไม่เปลี่ยน** (`explore=1`, `patrol=1`) ทั้งก่อน/หลังงาน 10 วัน และหลัง Wait 5 วัน ⇒ ไม่ใช่ตัวนับวันแบบ decrement ต่อวัน **[GUESS: เป็นวันสุดท้ายที่ทำ หรือค่าคงที่ของ area]** | RUNTIME (ค่า) / GUESS (ความหมาย) |
| สิ่งที่เปลี่ยนใน 1 วัน | ปฏิทิน +1, `ForceData.resourceStore` ขยับตาม `resourceChange`, `change[]` ของ force คำนวณใหม่ | RUNTIME |
| `hour` รีเซ็ตไหม | **ไม่รีเซ็ต** (คง 18.394 ตลอด 15 วัน) | RUNTIME |

## P1-8 ความสัมพันธ์ NPC + เพดานปาร์ตี้

**ที่เก็บ (RUNTIME, อ่านจากตัวเอก)**
- `HeroData.favor` (float) = ค่าความสัมพันธ์กับฮีโร่รายคน — ตัวเอกยังเป็น **`-999999`** = ค่าตั้งต้น/ยังไม่ผูก **[RUNTIME]**
- `HeroData.loveAble` (bool) — ตัวเอก `false`, ทั้งโลก true 744/883 **[RUNTIME]**
- กลุ่มความสัมพันธ์ใน `HeroData`: `Students` (`List<int>`, 7) · `Lover` (`int`, -1) · `PreLovers` (`List<int>`, 0) · `Friends` (`List<int>`, 5) **[RUNTIME]** — ตรงกับแท็บ "ศิษย์ / คู่ / สหาย"
- `FavorData { int heroID; float favorDregee }` (โครงแยกสำหรับระดับความสัมพันธ์รายคู่) **[DUMP]**
- ระดับ favor ฝั่งสำนัก: `ForceData.forceFavor` (`List<float>`), `forceFavorDict`, `forceFavorSetting` (`ForceFavorSettingData`) **[DUMP]**
- UI ที่แสดง: `ForceDetailController.favorText` — ข้อความจริงที่อ่านได้ตอนเปิดหน้า: `"ป้ายอมตะ สนิทแนบแน่น(100) 11111"` **[RUNTIME]**

**NPC เนื้อเรื่อง vs NPC สุ่ม [RUNTIME]**
- `WorldData.TempHeros` = 223 รายการ และ **ทั้ง 223** มี `isTempHero=true` + `isRandomEnemy=true`; `tempPlotHero` ในลิสต์นี้ = 0
- `WorldData.Heros` 883 live: `isTempHero=0`, `isRandomEnemy=0` ⇒ **NPC สุ่ม/ชั่วคราวอยู่ใน `TempHeros`, NPC เนื้อเรื่องอยู่ใน `Heros`** (field `HeroData.isTempHero` 0x3CC, `isRandomEnemy` 0x3CD, `tempPlotHero` 0x5D)
- ช่วง `heroID` ใน `Heros` = 0–896 (ไม่มีค่าลบ), ตัวเอก = 0
- บทสนทนา/พล็อตอ้างค่า: `PlotData.plotRandomHero` (`PlotRandomHeroData`), `SinglePlotData.plotSource/plotTarget/sourceName/targetName`, `PlotData.targetHeroID`, `SinglePlotChoiceData.relations` (`RelationRequirementType`) **[DUMP]**

**เพดานปาร์ตี้ "เพื่อนร่วมทีม 3/6" — ตอบครบ**
- `HeroData.GetMaxTeamMate()` = `mov eax,[rcx+0xB8]; inc eax; ret` (**asm ยืนยัน ไม่มี write**) → `ebp = heroForceLv + 1`
- `heroForceLv=5` → `GetMaxTeamMate()=6`, `GetMaxStudent()=6` (RVA เดียวกัน `0x88FC70`) **[RUNTIME]**
- `teamMates=[86,4,9]` = 3 ⇒ **3/6 = len(teamMates) / (heroForceLv+1)** **[RUNTIME]**
- `inTeam=false`, `teamLeader=-1` ตอนวัด (ตัวเอกเป็น leader เอง)
- ยูนิตที่ guest จะคุม: ต้องอยู่ในปาร์ตี้ (`teamMates` + ตัวเอก) เพราะศึกดึงปาร์ตี้ด้วย `PrepareBattleMap(..., includeTeamMate, ...)` **[DUMP]**; `TeamMemPrepareData.PrepareControlable()` = ตัวตัดสินว่ายูนิตไหน "คุมได้" **[DUMP]**

## P2-9 จัดการสิ่งปลูกสร้าง + สิทธิ์ผู้อาวุโส (บันทึกจุดเข้าเท่านั้น)

- controller: `AreaBuildController` — `BuildModeButtonClicked()`, `ChangeBuildMode(bool)`, `BuildNewButtonClicked(GameObject)`, `BuildChoiceButtonClicked(GameObject)`, `PlayerUpgradeBuilding(AreaBuildingData)`, `MoveBuildTarget(GameObject)`, `EndBuildMode()`, `GetMaxSpeBuildingNum()` **[DUMP]**
- **เกณฑ์สิทธิ์เป็น static `int` บน `AreaBuildController` — ค่าจริง [RUNTIME]**: `UpgradeBuildNeedForceLv=3`, `NewBuildNeedForceLv=4`, `DestroyBuildNeedForceLv=4`, `MoveBuildNeedForceLv=5`, `UpgradeRoadNeedForceLv=5`, `MaxSpeBuildingNum=5`
- ตัวเทียบคือระดับสำนักของผู้เล่น (`HeroData.heroForceLv` / `ForceData.forceLv`) — **ตัวดำเนินการเทียบ (`>=` หรือ `>`) ยังไม่ยืนยัน [GUESS]**
- ผลต่อทรัพยากร: `AreaBuildingDataBase.upgradeResource` / `changeResource` + `AreaBuildingData.buildTimeLeft/upgradeTimeLeft/destroyTimeLeft` (ตอนวัด = 0 ทั้งหมด) และ `noCancel` **[DUMP + RUNTIME]**
- ย้อนกลับได้ไหม: `destroyTimeLeft` + `noCancel` บอกว่าการรื้อเป็นงานที่กินวันและอาจยกเลิกไม่ได้ **[GUESS]**
- UI ฝั่งผู้เล่น: `BuildingUIController.UpgradeButtonClicked()` / `SureUpgradeBuliding()` / `EnterBuilding()` / `ShowBuildingUI()` **[DUMP]**
- **ห้ามกดยืนยันการสร้าง/อัปเกรด/ย้าย/รื้อ** จนคุณสั่ง — รอบนี้ไม่มีการกดเลย

## หน้ารายชื่อทุกสำนัก / ทุกตัวละคร (ปุ่ม O) — controller + วิธีเปิด + ที่มาข้อมูล

- **RUNTIME**: ตอนคุณกด O พบว่าหน้าที่ active คือ `ForceDetailPanel` (`ForceDetailController.forceDetailPanel.activeSelf=true`, child `ForceDetail:false`) — controller = **`ForceDetailController`**
- เปิดด้วย: `ForceDetailController.OpenForceDetail()` / `ShowForceDetail(int targetForceID)` / `RefreshForceDetailTab()` **[DUMP]**
- ในหน้านี้มี **30 แท็บ** (`ForceDetailTabController` 30 instance, แต่ละตัวมี `forceID` 29→20…) ⇒ แท็บ = รายชื่อทุกสำนัก; ข้อความมาจาก field ตรง: `baseDetailText`, `areaText`, `detailText`, `favorText` **[RUNTIME]**
- ข้อความที่อ่านได้ (ตัวอย่างจริง): `"แนวทางบริหาร ปานกลาง / ศิษย์ 19/0 / เงิน 365/1000 (+1050) / เสบียง 342/1000 (+120) / ไม้ 319/1000 (+280) / แร่ 312/1000 (+320) / สมุนไพร 396/1000 (+160) / บารมี 350/1000 (0)"` และ `favorText="ป้ายอมตะ สนิทแนบแน่น(100) 11111"` **[RUNTIME]**
  - ⚠️ `nowShowForceID=-1` ขณะอ่าน ⇒ ข้อความชุดนี้เป็น **ข้อความค้างจาก force ที่เปิดล่าสุด** ไม่ใช่ของ force 25 — อย่าใช้เป็นค่าของสำนักใด ๆ โดยไม่ระบุ forceID
- ที่มาข้อมูล: `WorldData.Forces` (30) + `ForceData` (resourceStore/Change, ownHeros, ownAreasID, forceLv, leader, forceFavor) ⇒ **data class ระดับโลก ไม่ได้สร้างตอน UI เปิด** (รายชื่อ 30 สำนัก + lv อ่านได้ก่อนเปิดหน้า) **[RUNTIME]**
- รายชื่อที่อ่านได้ทั้ง 30: 0 长乐帮/lv4, 1 唐门/lv7, 2 药王谷/lv4, 3 丐帮/lv5, 4 飞龙门/lv5, 5 茅山派/lv5, 6 铸剑山庄/lv4, 7 五毒教/lv5, 8 阎罗殿/lv6, 9 大隐阁/lv6, 10 少林寺/lv7, 11 武当派/lv9, 12 霸刀门/lv3, 13 蓬莱派/lv5, 14 峨眉派/lv4, 15 崆峒派/lv4, 16 神机门/lv5, 17 霹雳堂/lv3, 18 金刚密宗/lv6, 19 天山派/lv6, 20 聚义门/lv5, 21 黄河帮/lv2, 22 八卦门/lv2, 23 海沙帮/lv5, 24 铁掌帮/lv2, 25 仙霞派/lv4, 26 巨鲸帮/lv4, 27 金龙帮/lv4, 28 青城派/lv2, 29 伏牛派/lv3 **[RUNTIME]**

## ข้อมูล Area/สำนัก ที่ตัวเอกไม่ได้อยู่ — อ่านครบไหมโดยไม่เปิดหน้าจอ

**ได้ครบ — [RUNTIME]**: ขณะตัวเอกอยู่ area55 ผมอ่าน area1 (杭州) และ area2 (福州) ได้ทุกฟิลด์ที่ UI ใช้ (`areaName/areaType/belongForceID/people/maxPeople/safe/support/defence/areaStartLv/areaTiles/insideHeros`) โดย**ไม่เคยเปิดหน้าจอของสอง Area นั้น**
- ที่มา: `WorldData.Areas` (69 ตัว live) + `GameDataController.areaDataBase` (69) ⇒ โหลดมาจากเซฟ/DB ไม่ใช่สร้างตอน UI เปิด
- ข้อยกเว้นที่ยังไม่วัด: ชื่อ/prefab/สไปรต์ของ **อาคาร** (`AreaBuildingDataBase.name/spriteName`) และค่าที่คำนวณเฉพาะหน้าจอ (เช่น `AreaController.GetAreaSpePriceRate()`) — ต้องเปิดหน้าจอหรือเรียก method คำนวณ **[GUESS]**

## หน้าจัดการศิษย์ตามลำดับขั้น (backlog — บันทึกชื่อเท่านั้น)

- จุดเข้า: อาคาร 正厅 → ปุ่ม "门派弟子" → `callFuc = ShowForceHero` (ยืนยัน [RUNTIME]) และปุ่ม "弟子方针" → `ManageForceHeroSetting` [RUNTIME]
- controller: `ForceHeroUIController.ShowForceHeroUI(ForceHeroUIType, ForceData)` โดย `ForceHeroUIType = { Show=0, SelfForce=1, Leader=2 }`; `RefreshHeroList()`, `ClearHeroList()`, `UpgradeButtonClicked()`, `BetrayButtonClicked()`, `LeaveButtonClicked()`, `ServantButtonClicked()` **[DUMP]**
- controller เสริม: `ForceHeroSettingController.ShowForceHeroSettingUI(ForceData)` (AISetting ต่อ hero), `BranchUIController.ShowBranchUI(AreaData)` + `DefenceUpgradeButtonClicked(GameObject)` **[DUMP]**
- **ไม่ลงลึกตามสั่ง** (เป็น backlog) — ไม่ได้เปิด/วัดหน้านี้

## incident ระหว่างรอบสำรวจ (ต้องบันทึก)

- **2026-10-22 06:39 — เกมถูกเปิดบนเซฟจริงหลัง restore** (pid 6076) เพื่อทดสอบ `>>` โดยไม่มี split/preflight ⇒ ผิดโปรโตคอล §1.2
- ผลกระทบที่วัดได้: เปลี่ยนเฉพาะ `PlayerprefData.dat` (57 ไฟล์, `SaveSlot*` ทั้ง 55 ไฟล์ไม่ถูกเขียน) — เกมไม่ autosave
- ต้อง rebaseline volatile-only หลังปิดเกม (บันทึกใน `coop-test/baselines/`) แล้วรายงาน
- บทเรียน: **ห้ามทดสอบ runtime ต่อหลัง restore** — ต้อง split ใหม่ทุกครั้ง

## พบโดยบังเอิญ (ไม่เกิน 10 บรรทัด)

1. `TimeScaleController { paused, nowSlowTime, slowTimeDatas }` + `SetSlowTime(time, scale)` — ตัวคุม slow-motion แยกจาก `Time.timeScale` ของ Unity **[DUMP]**
2. `WorldData` มี `cheating` / `cheated` (**ห้ามแตะ** ตามกฎข้อ 5) และ `battleTimeScale=10.0f` **[RUNTIME]**
3. `ExploreController` : `GenerateExploreMapData(eventName, exploreParamString, difficulty)`, `FinishExploreMap(bool success)`, `SeeAllTile()` **[DUMP]**
4. `CraftUIController` / `AuctionController` มีอยู่ในลิสต์ controller ของ `GameController` (P2 ข้าม) **[DUMP]**
5. `AreaController.GetAreaSpePriceRate()` + `AreaData.areaTreasurePriceData` = กลไกราคาพิเศษต่อ Area **[DUMP]**
6. `AreaData.autoBuild` / `autoBuildResourceRateLimit` / `autoBuildPriority` = ระบบสร้างอัตโนมัติ (ยังไม่สำรวจ) **[DUMP]**

---

## ข้อความโปรโตคอลที่ต้องเพิ่ม (ข้อเสนอ — ยังไม่แก้ STAGEPLAN)

### 1) `AREA_SNAPSHOT` (host → guest, ต่อเมื่อเข้า/ออก Area หรือทุก N วินาที)

```
AREA_SNAPSHOT {
  v:1, tag:"area", t:hostTicks,
  world: { year, month, day, hour },            // WorldData.worldTime + hour
  area: {                                        // AreaData ของ area ที่ host อยู่
    areaID, areaName, areaType, belongForceID, areaStartLv,
    people, maxPeople, safe, support, defence,
    changeResource[6], resourceValueRateBase[6], resourceValueRateTemp[6],
    insideHeros:[heroID...],
    branchLeaderID, missionNumCount, plotNumCount,
    interTime: { exploreTime, patrolTime }
  },
  buildings:[ { tileRow, tileCol, buildingID, lv, buildTimeLeft, upgradeTimeLeft,
                destroyTimeLeft, produceRate, belongHeroID, noCancel } ],
  force: { forceID, forceName, forceLv, leader, ownAreasID[], ownHeros[],
           resourceStore[6], resourceChange[6] },
  heroes:[ { heroID, atAreaID, inTeam, teamLeader, teamMates[] } ],   // เฉพาะคนใน area
  ui: { plotHappen, plotChoiceShowing, working, totalWorkingDay, leftWorkingDay, skipping }
}
```
เหตุผล: guest ต้อง "เห็น" Area ได้โดยไม่ต้องมี AreaData ของตัวเอง (S4 mirror) — ทุกฟิลด์ข้างบนยืนยันแล้วว่าอ่านได้จาก data class **[RUNTIME]**

### 2) `AREA_DELTA` (host → guest, หลังจบ 1 delta)

```
AREA_DELTA { v:1, tag:"areaDelta", fromTick, toTick,
  world:{ day, hour }, areaID,
  changed:{ path:"area.people", old, new }, ...      // เทียบ stateHash ของ AREA_SNAPSHOT
  hash:{ area:"<sha1 of area substate>", force:"...", buildings:"..." } }
```
ใช้กฎเดิมของ S2 (`auditEqual`) แต่ขยายขอบเขตจาก battle → area ⇒ **ต้องมี `areaHash` แยกจาก `battleHash`** ไม่งั้น guest จะ resync ผิดก้อน

### 3) คำขอ action ที่กินเวลา (guest → host → validator → host รัน)

```
TIME_ACTION_REQUEST {
  v:1, reqId, kind:"work"|"study"|"wait"|"build",
  args:{ work:"ProduceBuildingWork", param:"耕作;10;;;", buildingID, tile:{row,col},
         dayNum:10, targetHeroID, skillID },
  expect:{ worldDay, areaID, actorHeroID },
  clientSeq                                                   // ใช้ตรวจ replay
}
TIME_ACTION_ACCEPT { reqId, hostTick } | TIME_ACTION_REJECT { reqId, code:"..." }
TIME_ACTION_DONE   { reqId, worldDayBefore, worldDayAfter,
                     areaDelta:{...}, forceDelta:{...}, workingUI:{...} }
```
กติกา validator ที่เสนอ (จากที่วัดได้): `dayNum ∈ 1..30` (สไลเดอร์ WaitUI), `dayNum ≥ 1` สำหรับงาน, `actorHeroID` ต้อง `atAreaID == areaID`, `belongForceID` ของ area ต้องเป็น force ของ actor, และ `*NeedForceLv` ต้องผ่านก่อนสร้าง/ย้าย/รื้อ

## Diff ที่เสนอต่อ STAGEPLAN (S4 แยกเป็น 3 ก้อน)

**ปัจจุบัน** §8 S4 = "World follow (opt-in, ทำหลัง S5)" ก้อนเดียว

**เสนอ** แยกเป็น

| ก้อนใหม่ | ขอบเขต | เกณฑ์ผ่าน (exit) |
|---|---|---|
| **S4a — World follow** | host เดิน/เวลาโลก/area เปลี่ยน → guest ตาม (viewer) | walk 10/10, area enter/exit 10/10, `worldTime` ตรง 100%, ไม่มี write ฝั่ง guest |
| **S4b — Area view** | `AREA_SNAPSHOT`/`AREA_DELTA` + areaHash; UI ของ guest แสดง Area/อาคาร/ตัวละครใน Area | snapshot ตรง 57 field + 9 building field, delta ตรง 100% จาก 20 delta, hash mismatch → resync สำเร็จ |
| **S4c — Time-consuming actions** | `TIME_ACTION_*` + validator + WorkingUI/ChangeDay บน host เท่านั้น | งาน 1/5/10 วัน ผ่าน validator 10/10, ปฏิเสธ 20/20 (dayNum นอกช่วง/ผิด area/ไม่มีสิทธิ์), ปุ่ม >> + ยกเลิกกลางคันถูกต้อง, เซฟ hash ไม่เปลี่ยน |

**ยืนยันจาก runtime แล้ว (2026-10-22)**: งานหลายวันและ WaitTime ใช้ `WorkingUIController` ชุดเดียวกัน — `dayTime=1 s/วัน`, `totalWorkingDay` = จำนวนวันที่เลือก, ปฏิทินเดินทีละวัน, `hour` ไม่รีเซ็ต ⇒ S4c ต้อง mirror **WorkingUI state + day ticks** ไม่ใช่แค่ `ChangeDay`

**ยังต้องยืนยัน**: ตัวเลือก 1/5/10 สร้างจาก UI ตัวไหนแน่ (`ProduceBuildingWork` overload ไม่มี param) และพฤติกรรมปุ่ม `>>` **[NOT-RUN]**

## คำถามที่ต้องให้คุณตัดสินใจ

1. ~~1/5/10 วัน~~ **วัดแล้ว** (ดู P1-5) — เหลือเฉพาะ: จะวัดว่าตัวเลือกที่มีให้เลือกจริงคือ 1/5/10 ตามที่คุณเห็น หรือต่างออกไป (ต้องกดดูทีละปุ่ม)
2. ~~WaitTime~~ **วัดแล้ว** (ดู P1-7) — เหลือ: จะวัดซ้ำที่ค่าอื่น (เช่น 30 วัน) เพื่อดูว่า delta ของทรัพยากรเป็นเชิงเส้นไหม
3. **ปุ่ม >> (skip)**: ยังไม่被测 (งานจบเองก่อน ~12 วิ) — ต้องทำงาน 10 วันอีกครั้งแล้วกด `>>` ระหว่างนั้น เกี่ยวตรงกับ S4c
4. **`areaHash`**: ต้องการให้ hash ครอบ `insideHeros` (ซึ่งเปลี่ยนทุกครั้งที่ NPC เดินเข้า/ออก) หรือแยกเป็น field ที่ tolerate ได้? ถ้ารวม อาจ resync ถี่เกินจำเป็น
5. **เพดานปาร์ตี้**: ยืนยันหรือไม่ว่า guest คุมได้เฉพาะ `teamMates` + ตัวเอก (ตาม `PrepareBattleMap(includeTeamMate)`), ไม่ใช่ทุกคนใน Area (11 คน)
6. **`GetMaxStudent()` = `heroForceLv+1` = 6 แต่ `Students` มี 7** — เป็นบั๊กเกม/ค่าที่เกินเพดาน หรือผมอ่านผิด? ต้องตรวจซ้ำก่อนใช้เป็นเงื่อนไข validator
