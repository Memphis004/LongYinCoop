# time-notes — ระบบเวลาโลกของ LongYinLiZhiZhuan (read-only survey)

> สำรวจ 2026-10-27 · **read-only 100%**: ไม่เปิดเกม · ไม่ติด hook · ไม่เขียนไฟล์/เซฟ
> วิธี: อ่าน `Il2CppDumper/dump.cs` (เลขบรรทัดจากไฟล์จริง) + หลักฐาน RUNTIME ที่วัดไว้แล้วใน repo นี้
> + เปิดไฟล์เซฟจริงเพื่อ**อ่าน**อย่างเดียว (ยืนยัน serialization)
> ป้าย: **RUNTIME** = วัดจาก process/ไฟล์จริง (session ก่อนหน้า) · **DUMP** = จาก `dump.cs` เท่านั้น
> · **GUESS** = อนุมาน · **UNKNOWN** = หลักฐานไม่พอ · **NOT-RUN** = ต้อง session อนุมัติแยก
> หลักฐานดิบ: `evidence/S2cd-20261022-0710/`, `evidence/S1-20261020-113926/worldtime-*.md`,
> `evidence/sp0sp7-20261024-225939/`, `worldmap-notes.md`, `battle-notes.md`, `area-notes.md`, `explorer-notes.md`

---

## 1. แหล่งความจริงของเวลา (authority)

```
WorldData (dump.cs:367587)
  0xA8  TimeData worldTime        :367615   → TimeData { int year 0x10, int month 0x14, int day 0x18 }  (dump.cs:367149)
  0xB0  float TimeDifficulty      :367616   live 9.6667 [RUNTIME worldmap] · บทบาทยังไม่พิสูจน์ [UNKNOWN]
  0xB4  float hour                :367617   live 21.6108 [RUNTIME] — ไม่ถูกรีเซ็ตเมื่อวันเปลี่ยน [RUNTIME]
GameController : MonoBehaviour (dump.cs:376432) — singleton
  0x20  WorldData worldData
  0x28  bool needAutoSave   0x2C  float checkUpdateTime  (ตัวใช้งาน UNKNOWN)
```

- HUD `HudController.timeLabel` อ่านจาก `worldTime` ตรง ๆ — เทียบตรงทุกฟิลด์ 5/5 sample [RUNTIME worldmap §2.1 + S2cd ui-crosscheck §2.1]
- จอแสดงทรัพยากรแบบ **ตัดทศนิยมทิ้ง** (4733.7 → `4733`) ไม่ใช่ปัดขึ้น [RUNTIME ui-crosscheck §2.2]
- เซฟ = JSON ทั้งโลก: `SaveSlot0/Save` มีจริง `"worldTime":{"year":10,"month":1,"day":15}`,
  `"hour":0.0355…`, `"TimeDifficulty":9.9`, `"lastRandomWorldEventDay":18` [RUNTIME — อ่านอย่างเดียว 2026-10-27]
- `TimeData` methods: `DeltaDay(TimeData)` 0xAA6F80 · `IsMeetingTime()` 0xAA70B0 · `NextMeetingTime()` 0xAA70D0 ·
  `GetExactYear()` 0xAA7070 [DUMP] — ความยาวเดือนยังไม่ได้พิสูจน์ทุกกรณี (พบเคสเดียว: เดือน 30 วัน) [RUNTIME n=1]

## 2. Write paths — ตัวเดินเวลาทั้งหมดที่รู้จัก

> **ปฏิทินไม่มี tick อัตโนมัติ** — idle นิ่ง (Δhour=0 ตลอด 2 วิ), ศึกนิ่ง (2,664 sample), ดันเจี้ยนนิ่ง,
> ไม่มี paused flag ในเกมเลย (grep ครบ) — เวลาหยุดเพราะ "ไม่มีใครเรียก ChangeDay" [RUNTIME worldmap §2.2/§2.4/§6]

| ทางเข้า | กลไก | หลักฐาน |
|---|---|---|
| **ปั๊มงาน** | `WorkingUIController.Update` (0x9CB4B0) นับ `nextDayTime` ถอยหลังด้วยเวลาจริง → เรียก `ChangeDay()` วันละครั้ง | RUNTIME (hook + วัดอัตรา) |
| **เดินทางบน BigMap** | `hour += f(ระยะทาง)` ~0.0157 ชม./หน่วย (median 86.1 หน่วย → +1.296 ชม., Pearson r=0.839) · ครบ ~24 ชม. → วันเดิน (+215.8 ชม. → +9 วัน) | RUNTIME (correlation) · **จุด write = UNKNOWN** (worldmap §7 ข้อ 1) |
| **สภาพอากาศ** | `WeatherController.ChangeWeatherLastTime(float deltaTime)` 0xC18A90 → `WorldData.weatherLastTime` 0x170 | DUMP (signature) · จังหวะ NOT-RUN |
| **hard jump** | `ChangeDayDirect(int)` 0x790CA0 · `ChangeMonthDirect(int)` 0x793920 · `ChangeYearDirect(int)` 0x795380 · `ChangeHour(float)` 0x792620 — **ไม่พบ caller** | DUMP |

### 2.1 `GameController.ChangeDay` — สอง overload (dump.cs:376737/376743)

```
ChangeDay(int deltaDay)  RVA 0x7925F0  — disasm ยืนยัน: if (delta<=0) return; แล้ว for-loop เรียก ChangeDay()
                                          วน delta รอบ · ไม่มี write ฟิลด์เลย [RUNTIME inspect-analysis.md §2–3]
ChangeDay()              RVA 0x790E30  — leaf ตัวเดินวันจริง · **body ยังไม่ถูกอ่าน** [UNKNOWN]
```

**วัดจริง 2 รอบด้วย hook 2 จุด (S2cd measurement-skip1/2.md):**

| รอบ | งาน | `ChangeDay(int)` | `ChangeDay()` | ปฏิทิน | `hour` |
|---|---|---|---|---|---|
| 1 | 10 วัน | **0 ครั้ง** | **10 ครั้ง** (burst 10.5 วิ, ทุก ~0.75–2.9 วิ) | 10/8/25 → 10/9/5 (+10) | 10.138 คงที่ |
| 2 | 5 วัน | **0 ครั้ง** | **5 ครั้ง** | 10/9/5 → 10/9/10 (+5) | 10.138 คงที่ |

- ปุ่ม `>>` (SkipButtonClicked) = ลูป `skipping` เรียก leaf เอง ไม่ผ่าน wrapper — `>>` เป็นแค่การเร่ง
  animation วันยังเดินทีละวันเหมือนเดิม [RUNTIME]
- ⚠️ **ข้อสรุปเก่า "`>>` เดินปฏิทิน +1 วัน" ถูกหักล้างแล้ว** (sample เดิมถ่ายกลางการ์ตูน skip ที่ `leftWorkingDay=1`)
  — ห้ามอ้างกลับ [RUNTIME — ดู measurement-skip1 §6]
- `skipping` **ค้าง true** หลัง `workFinished=true` ทั้ง 2 รอบ ⇒ ห้ามใช้ `skipping` เป็นตัวบอกสถานะ
  ให้ใช้ `working && !workFinished` [RUNTIME]
- side effect ของ leaf ที่เห็น: +1 วัน, `hour` ไม่แตะ, ทรัพยากรสำนักขยับ **แม้ตอน Wait ที่ไม่มี `workingFuc`**
  (Wait 5 วัน: store +455/+235.6/0/0/−400) ⇒ น่าจะมี housekeeping รายวัน (AI/ทรัพยากร/expiry) อยู่ใน leaf หรือ
  callee ของมัน [GUESS — รอ disasm]

## 3. ปั๊มกิจกรรมกินเวลา — `WorkingUIController` (dump.cs:392679)

```
fields: working 0x48 · leftWorkingDay 0x4C · totalWorkingDay 0x50 · nextDayTime 0x54 · leftPauseTime 0x58
        resourceNum 0x5C · workFinished 0x60 · noCancel 0x61 · skipping 0x62 · skipButton 0x68
        static float dayTime 0x0 · static float pauseTime 0x4   ← live: dayTime = 1 (วิ/วัน), pauseTime = 0.25 [RUNTIME]
methods: StartWorking(int dayNum, string callFuc) + อีก 5 overload · StopWorking(bool finished) 0x9CB220
         TryCancelWorking() 0x9CB330 [NOT-RUN] · SkipButtonClicked() 0x9CA60 · SetSkippingState(bool) 0x9CA8E0
         AddResourceNum(int resourceID, float num, float iconNum, WorkResultType) 0x9CA580 · Update() 0x9CB4B0
```

**ทุกกิจกรรม "กินวัน" ที่พบล้วงลงปั๊มนี้** [RUNTIME + DUMP]:

| กิจกรรม | ทางเข้า | วัดแล้ว |
|---|---|---|
| งานผลิต (挖掘/耕作/…) | ปุ่มอาคาร → `ProduceBuildingWork` → `workingFuc="WorkInProductionBuilding"` | 10 วัน ≈ 12.5 วิ จบเองโดยไม่ต้องกด `>>` · ปฏิทิน +10 พอดี · `hour` คงที่ [RUNTIME] |
| **Wait 1–30 วัน** | `WaitUIController` (dump.cs:392394) slider min 1 max 30 wholeNumbers → `SureButtonClicked()` → **ปั๊มเดียวกัน** | Wait 5 วัน = +5 พอดี, `totalWorkingDay=5` [RUNTIME] |
| ฝึกวิชา | `BuildingUIController.StudyFightSelf` → `ChooseController` (HeroSkill) → `StartWorking(GetSkillMartialClubStudyTime())` | ฝึก 2 วัน = +2 พอดี, `workingFuc=null` สำหรับการฝึก [RUNTIME] |
| งานสังคม/ช่วยงาน | `PlotController.StartWorking(string param)` — param = `"workName;days;callFucParam;finishFuc;finishFucParam"` | ตัวอย่างจริง: `巡查;3`, `配置药品;2`, `打杂;5`, `指点;3`, `捉贼;3` [RUNTIME] |
| คราฟต์/ยาพิษพิเศษ/วิจัย/สมาธิ/เขียนหนังสือ/สร้างอาคาร | `SpePoisonData.leftTime`, `ForceSpeResearchData.leftTime`, `MeditationData.monthMeditationDay/*LeftTime`, `BookWriterData.workPercent`, `AreaBuildingData.buildTimeLeft/upgradeTimeLeft/destroyTimeLeft` | หน่วย = วัน [DUMP] · วิธีเดิน NOT-RUN |

- จำนวนวันตัวเลือกบนจอ (1/5/10/15) เป็น UI ที่สร้างตอนรัน ไม่ได้มาจาก static DB [RUNTIME area-notes P1-5]
- **จำนวนวันที่ผ่านจริงต้องอ่านจาก `worldTime` เท่านั้น** — `totalWorkingDay` ใช้เป็นหลักฐานเวลาไม่ได้
  (ระหว่าง skip มันไต่ 9→10 และ `workFinished` มาก่อนวันจบ) [RUNTIME area-notes S4c notes]

## 4. Consumers ของเวลา (inventory)

| ระบบ | ตัวแปร/เมธอด | หน่วยเวลา | หลักฐาน |
|---|---|---|---|
| AI รายวันทั้งโลก | `GameController.ManageAllAI(bool considerAIHour)` 0x7C5A50 · `ManageOneAI` 0x7D2DD0 · `AIController.ManageAIOneDay(HeroData)` 0x13E80A0 · `HeroData.manageAiHour` 0x34 + `dailyAIManaged` 0x38 | วัน + hour-scheduler | DUMP + field map RUNTIME · เชื่อมกับวันเปลี่ยน [GUESS] |
| เหตุการณ์โลก | `EventData.leftTime` 0x68 + `eventOutTimeCallFuc` 0x90 (dump.cs:368871+) · `WorldEventDataBase{startTime TimeData, repeatType, repeatDay, lastTime}` (367242) · `WorldData.lastRandomWorldEventDay` · timeout handlers ของ GameController (`ManageWorldEventOutTime` 0x7D4E90, `ForceAttackPlayerAreaTimeOut` 0x7A1350, `AuctionOutTime` 0x78D380, `FightMatchOutTime` 0x7A06E0, `HorseMatchOutTime` 0x7C15D0) | วัน | DUMP (โครงสร้าง) |
| plot/เนื้อเรื่อง | `WorldData.plotHappened` + `worldPlotEventStartTime` (Dictionary<int,TimeData>) · `WorldPlotEventData{startTime,endTime,startContinueTime}` (369137) · `WorldPlotEventStartData.startLeftDay` (369172) · `TimeData.IsMeetingTime/NextMeetingTime` (ประชุมสำนัก) | วัน | DUMP |
| ภารกิจ/bounty | `MissionData.leftTime` 0x24, `stageMinLeftTime`, `noAutoFinish` (dump.cs:373135+) | วัน | DUMP |
| ไปรษณีย์/ข่าว | `MailData.mailTime (TimeData) + autoDestroyTime` (369963) · `WorldNewsData.leftTime` (367271) · `ManageMailTime(int changeTime)` 0x7CE800 | วัน | DUMP |
| เศรษฐกิจรายเดือน | `WorldData.month*Time` ×17 (0x110–0x158, dump.cs:367637–367655) · `ChangeMonth()` 0x793A20 → `ManageMonthTask` 0x7CF030 · `MonthRefreshAllBuildings(bool)` 0x7D6EF0 · `RefreshInnsAndGovern` 0x7DC1F0 · `RefreshGovernStorage` 0x7DC030 · `ManageSalary` 0x7D3BC0 · `CheckForceMonthMoneyShortage` | เดือน | DUMP (โครงสร้าง) |
| ทรัพยากร | `ForceData.resourceStore/resourceStoreMax/resourceChange` (dump.cs:369410–369414) · `AreaData.changeResource` + `resourceValueRateBase/Temp` · `ResourcePointData.changeResource` | รายวัน (พบขยับตอน Wait ที่ไม่มีงาน) | RUNTIME (ผล) · ตำแหน่งโค้ด [GUESS] |
| สำนัก/การโจมตี | `ForceData.thisMonth*` · `randomAttackAreaDay` · `ForceStopWarTime` (Dict) · `ForceInteractionTimeData{addFavorTime,reduceFavorTime,stealResourceTime,giveGiftTime}` + `ResetTime()` (dump.cs:369899) | เดือน/วัน | DUMP |
| ฟื้นฟู/สถานะตัวละคร | `ManageHeroAutoRecover(hero, rest, needRefresh, extraRate)` 0x7CE480 + `ManageHeroAutoRecoverAndInjury` 0x7CE340 · `HeroData.rest` 0x1E8 · `selfCureTime` 0x238 · `MaxSelfCureTime/GetFullRecoverTime/GetSelfCure*InjuryTime` · `AddTag(id, float time=-1, …)` · `SetBuffTime(int,float)/AddBuff(int,float)` · `ManageTagTime()` | วัน + float duration | DUMP + field map RUNTIME |
| ทีม/สังกัด | `HeroJoinTeam(…, int autoLeaveDay)` 0x7C0920 · `HeroData.autoLeaveTeamDay` 0x300 · `ManageHeroAutoQuitAndEnter` 0x7CC020 | วัน | DUMP |
| อายุ | `HeroData.age` 0xD4 · `ChangeYear()` 0x7951B0 · `GetHeroSoundAgePitch` | ปี [GUESS] | field RUNTIME · กลไก UNKNOWN |
| **ศึก (แยกข้างนอก)** | `BattleController.battleTime` 0x1C8 (วินาทีจริงในฉาก) · `timeLimit` · `AIWaitTime` · skill CD `ManageHeroSkillCdTime` · buff `ManageHeroBuffTime/Round` · `TimeScaleController` เขียน `Time.timeScale` | วินาทีจริง — **ไม่แตะ worldTime/hour** | RUNTIME (2,664 sample Δ=0, battle-notes §1–6) |
| สภาพอากาศ | `WorldData.nowWeather 0x16C + weatherLastTime 0x170` · `WeatherController` (dump.cs:392525) · `WeatherData{baseLastTime, maxRateMonth, weatherTravelSpeedRate}` (392493) — น้ำหนักตามเดือน | float real-time + เดือน | DUMP · จังหวะ NOT-RUN |
| HUD | `timeLabel/timeCircle (dial กลางวัน-กลางคืน)/weatherIcon/seasonIcon` (dump.cs:379933–379937) | **cosmetic** — อ่านจาก worldTime/hour | RUNTIME |
| ดันเจี้ยน (explore) | `leftPower` ต่อก้าว — **นาฬิกาโลกนิ่งทุกครั้งที่วัด** | ไม่ผูกเวลา | RUNTIME explorer-notes §3 |
| ศึก NPC-vs-NPC | resolve ทาง world-sim data path **ไม่เปิดฉาก** (`PrepareBattleMap` 0 ครั้ง, `totalFightCount=0`) | — | RUNTIME sp0-sp7 §4 |

## 5. Chains สำคัญ

```
[งาน] UI เลือกวัน → PlotController.StartWorking(string) → WorkingUIController.StartWorking(dayNum, callFuc, …)
      → Update(): nextDayTime ถอย (dayTime=1 วิ/วัน, พัก pauseTime=0.25) → ครบวัน → GameController.ChangeDay()
        ├─ worldTime.day +1 (hour คงที่)
        └─ [GUESS] housekeeping รายวัน (AI/ทรัพยากร/expiry)
      → workingFuc(param) รายวัน (เช่น WorkInProductionBuilding → AddResourceNum)
      → leftWorkingDay==0 → StopWorking(true) → workFinished → finishFuc (เช่น FinishHelpCureDisease)

[เดินทาง] SetPlayerMoveTargetArea → BigMapController.Update → ManageAllHeroMove → ManageHeroMove
      → hour += ~0.0157 ชม./หน่วย → ครบ ~24 ชม. → วัน +1 [GUESS จาก +215.8 ชม. → +9 วัน] · จุด write UNKNOWN

[event] CreateRandomWorldEvent(limitStartTime) → EventData{leftTime, eventOutTimeCallFuc}
      → [GUESS: วันเปลี่ยน] leftTime-- → หมด → ManageWorldEventOutTime/RemoveWorldEvent(target, outTime=true)

[เดือน] ChangeMonth() → ManageMonthTask + MonthRefreshAllBuildings + RefreshInnsAndGovern + ManageSalary
      + รีเซ็ต month*Time ×17 + ForceInteractionTimeData.ResetTime + สร้าง bounty/ประมูลใหม่

[เซฟ] GameSaveData{WorldData(worldTime/hour รวมอยู่), HeroList(897), TempHeroList} = JSON
      · ไม่มี autosave ตอนออก (วัด 3 ครั้ง SaveSlot* ไม่ถูกเขียน)
      · WorkingUI state เป็น field ของ singleton UI ไม่อยู่ใน WorldData ⇒ งานค้างน่าจะไม่ถูกเซฟ [GUESS — NOT-RUN]
```

**เริ่มเกมใหม่:** `StartNewGame()` 0x7E0FC0 → `GenerateHero` → โลก default (Heros 883 + TempHeros 88 + Forces 30)
— ค่า `worldTime`/`hour` เริ่มต้นยังไม่เคยอ่านตอนสร้าง (world-fields-r3 อ่านได้แค่ชนิด) [UNKNOWN]
**โหลดเกม:** `StartLoadGame()` 0x7DF7B0 / `GameDataController.Load(int)` 0xC2C3D0 → `GameIntoGameData()` [DUMP]

## 6. ประเมิน Freeze / Decouple / Replace (สรุป — ไม่ได้เลือก)

| กลไก | Freeze | Decouple | Replace |
|---|---|---|---|
| ปั๊มงาน (`dayTime`) | ไม่แนะนำ (งานไม่จบ) | **แก้ static `dayTime` จุดเดียว** (เช่น 12 วิ/วัน ⇒ 5 วัน = 60 วิ) — ความเสี่ยงต่ำสุด, ไม่แตะ housekeeping | เขียนปั๊มใหม่ = ต้องเลียน callback สตริงทั้งหมด — เสี่ยง |
| ปฏิทิน `ChangeDay()` | ปิด leaf = AI/ทรัพยากร/expiry หยุดทั้งระบบ [รอ disasm] | คง leaf เป็น day-tick ภายใน ผูกจังหวะกับ server | ไม่ทำ — ทุก consumer + เซฟอ้าง `TimeData` |
| `hour` จากการเดินทาง | ต้องหาจุด write ก่อน (UNKNOWN) | travel-time ฝั่ง server | — |
| ศึก/HUD | ห้ามแตะ / cosmetic ไม่ต้องทำอะไร | — | — |

**แนวโน้มจากหลักฐาน:** เกมนี้ "กิจกรรม = ตัวเดินปฏิทิน" อยู่แล้วและ housekeeping ไหลผ่าน `ChangeDay()` จุดเดียว
⇒ **decouple จังหวะ/authority น่าจะเสี่ยงต่ำกว่า freeze ปฏิทินตรง ๆ** — แต่ยังตัดสินไม่ได้จนกว่าจะอ่าน body ของ leaf

## 7. ผลต่อ Multiplayer

- **มีอยู่จริง:** host-authoritative (guest ไม่มี write path, S2 spectator), S1 `WorldSampler` อ่าน
  `worldTime`/`hour`/HUD แบบ typed interop (`src/LongYinCoop/S1/WorldSampler.cs:97–113`), ระบบ audit hash+resync ของ S2 (ศึก)
- **ต้องเพิ่ม:** activity state ใน snapshot (`working/totalWorkingDay/leftWorkingDay/workFinished` + worldTime before/after —
  แบบ `AREA_SNAPSHOT`/`TIME_ACTION_*` ที่ร่างไว้ใน area-notes ท้ายไฟล์), ห้าม client เริ่ม/ยกเลิกกิจกรรมเอง,
  จัดการลำดับคำสั่ง "จบงาน vs เริ่มศึก" ในคิวเดียว (engine เป็น main-thread เดียว ⇒ race อยู่ที่ชั้นเครือข่าย)
- **กับดักที่วัดแล้ว:** `totalWorkingDay` ไม่ใช่หลักฐานเวลา · `skipping` ค้าง true · `>>` ส่ง snapshot กลาง animation ไม่ได้
  (วันไต่ทีละ 1, ทรัพยากรยังไม่นิ่ง — เสนอส่งหลังจบ animation)
- **big map:** ไอคอนดาบไขว้ `BigmapNpcController.BigMapFightIcon` อ่าน `activeInHierarchy` ได้แล้ว (S1 sampler) —
  ศึก NPC-vs-NPC เป็น data path ไม่มีฉาก ⇒ "ดูศึกผ่าน big map" สอดคล้องโครงเกม
- **ไม่มีหลักฐาน**ว่าเกมเชื่อม "เริ่มศึก → ยกเลิกกิจกรรมของอีกฝ่าย" อยู่แล้ว [UNKNOWN]

## 8. Open questions / evidence gaps (เรียงตามความสำคัญ)

1. **body ของ `ChangeDay()` (0x790E30)** — บอกว่า housekeeping รายวันใดอยู่ใน leaf (ตัดสิน freeze ได้) — NOT-RUN
2. **จุด write ของ `WorldData.hour`** — worldmap §7 ข้อ 1 ค้างมาตั้งแต่ S1 — NOT-RUN
3. **`TryCancelWorking()` + `noCancel`** — ยกเลิกกลางคันยังไม่เคยวัด — NOT-RUN
4. **งานค้างข้าม save/load** — เริ่มงาน → เซฟ → โหลด แล้ว `working` เป็นอย่างไร — NOT-RUN
5. `>>` ที่ `dayTime` อื่น / เพดานวันของ skip (ทดสอบแค่ 5/10) — NOT-RUN
6. ความยาวเดือนทุกกรณี (`TimeData.DeltaDay` — ต้อง invoke → ต้องอนุมัติ)
7. caller ของ `ChangeHour(float)` และ `*Direct` — ยังไม่พบ
8. บทบาท `TimeDifficulty` (live 9.6667 vs 0 ตอน new game) — UNKNOWN
9. ค่า `worldTime`/`hour` เริ่มต้นของ new game — UNKNOWN
10. รายชื่อ `workingFuc`/`finishFuc` ครบทุกงาน (callback เป็นสตริง กระจายใน data)

## 9. ดัชนีหลักฐาน

| ข้อเท็จจริง | ไฟล์ |
|---|---|
| ChangeDay hook 2 รอบ (10/5 ครั้ง, wrapper=0, hour คงที่) | `evidence/S2cd-20261022-0710/measurement-skip1.md`, `measurement-skip2.md` |
| disasm `ChangeDay(int)` + signature จริง | `evidence/S2cd-20261022-0710/inspect-analysis.md` |
| เทียบ memory ↔ จอ (วันที่ + 6 ทรัพยากร, floor) | `evidence/S2cd-20261022-0710/ui-crosscheck.md` |
| hour สะสมตามระยะทาง (r=0.839), ศึก/idle Δ=0 | `evidence/S1-20261020-113926/worldtime-accrual.md`, `worldtime-walk-test.md` + `worldmap-notes.md §2.2` |
| dayTime=1/pauseTime=0.25, Wait/ฝึก/งานวัดจริง, P1-4..P1-7 | `area-notes.md` |
| ดันเจี้ยนนาฬิกานิ่ง | `explorer-notes.md §3` |
| field map HeroData (manageAiHour/selfCureTime/…) + WorldData | `evidence/sp0sp7-20261024-225939/dump-HeroData.cs`, `dump-WorldData.cs`, `hero-fields*.json`, `world-fields*.json` |
| singleton census, NPC-vs-NPC data path | `evidence/sp0sp7-20261024-225939/sp6-census.md`, `sp7-npc-battles-partial.md` + `sp0-sp7-notes.md` |
| นาฬิกาศึกแยกจากโลก, RNG, TimeScaleController | `battle-notes.md §1–6` |
| เซฟ JSON มี worldTime/hour/TimeDifficulty/lastRandomWorldEventDay | `Save/SaveSlot0/Save` (อ่านอย่างเดียว 2026-10-27) |
| แบบร่าง protocol ฝั่ง mod | `area-notes.md` (AREA_SNAPSHOT/AREA_DELTA/TIME_ACTION_*) |
