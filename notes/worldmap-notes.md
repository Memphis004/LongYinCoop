# LongYinLiZhiZhuan — World Map Notes

> Read-only survey while standing on the **world map** (not in battle). No memory writes, no state-changing
> method calls. Every number below was read out of the live process unless tagged otherwise.
>
> Module base `GameAssembly.dll` = `0x7ffa2d2e0000`; `RVA + base = address`.
> Tags: **[RUNTIME]** observed live · **[DUMP]** from `Il2CppDumper/dump.cs` only · **[GUESS]** inference.

---

## 0. Live snapshot **[RUNTIME]**

```
BigMapController        0x2a1b822bbb0   nowScale 0.95, scrollSpeed 1
playerArmy (GameObject) 0x2a1b7e4b260   targetPos null, nextPos (private), fastMode false, playerResting false
player hero             heroID 0, "หวังเวิ่นเทียน"
  bigMapPos             (-1939.31, 311.86)      atAreaID -1  inSafeArea 0
  itemListData.money    61058
  fame 9999   badFame 0   heroForceLv 5
WorldData               0x2a19dc21000
  worldTime             10 ปี / 9 เดือน / 9 วัน   (TimeData 0x2a19f0e0000)
  hour                  21.6108   TimeDifficulty 9.6667
  Heros                 897 entries
BigMap icon lists       normal NPC 33 · temp NPC 70 · random-event icons 124 · needRemove 0
World clock over 2.0 s of observation: hour 21.6108 → 21.6108  (Δ = 0)
HUD labels              "10 ปี 9 เดือน 9 วัน" · "26171" · "9999" · "61058" · "148/145" · "2/10" · "995/3200"
```

---

## 1. Where the player party lives, and how it walks

### 1.1 Position (logical, the one that matters for saves/sync)

```
HeroData.bigMapPos      @ 0xC8   → BigMapPos { float x @0x10, float y @0x14 }   TypeDefIndex 8232
HeroData.atAreaID       @ 0xC0   current area id (-1 = wilderness)
HeroData.inSafeArea     @ 0xD0
HeroData.inPrison       @ 0xD1
```
**[DUMP]** offsets · **[RUNTIME]** live value `(-1939.31, 311.86)`, `atAreaID = -1`

`BigMapPos` is **not** a Unity world position; it is map-space scaled by
`GlobalData.BigMapVectorScale = 8` **[RUNTIME]**, and the map half-extents are
`BigMapWidthHalf = 7680`, `BigMapHeightHalf = 4320` **[RUNTIME]** (so the full map is 15360 × 8640 map units,
matching `BigMapController.bigMapWidth/Height = 153.6 / 86.4` at 1/100 scale **[RUNTIME]**).

The player has a map avatar too: `BigMapController.playerArmy` is a **GameObject** (`@0x58`), live pointer
`0x2a1b7e4b260` **[RUNTIME]**. Every map actor is rendered by a `BigmapNpcController` whose `heroData @0x18`
is the single source of truth for that actor (layout **[DUMP]**). That the player's own avatar is driven by the
*same* controller class is an inference from `FindBigMapNPC` / `MeetBigMapNpc` taking the same icon objects —
**not** verified this session, so it is tagged **[GUESS]** in §7.

### 1.2 Movement state on the controller

```
BigMapController (8101)
  0x58  GameObject playerArmy
  0xF0  BigMapPos targetPos          (private)  ← where the player was told to go
  0xF8  Vector2  nextPos             (private)  ← next steering waypoint
  0xE8  bool playerResting
  0xE9  bool fastMode
  0x100 bool bigMapControlAniming
```
**[DUMP]** · live: `targetPos` and `nextPos` empty, `fastMode/playerResting = false` **[RUNTIME]**

### 1.3 How you command walking

| RVA | Signature | Purpose |
|---|---|---|
| `0x8E2F10` | `void SetPlayerMoveTargetArea(int areaID)` | walk to an area by id |
| `0x8E2F90` | `void SetPlayerMoveTargetArea(string areaID)` | walk to an area by string id |
| `0x8E30B0` | `void SetPlayerMoveTargetArea(GameObject target)` | click on an area icon |
| `0x8E3170` / `0x8E30F0` | `void SetPlayerMoveTargetInn(...)` | walk to an inn |
| `0x8E3290` / `0x8E33B0` | `void SetPlayerMoveTargetResourcePoint(...)` | walk to a resource point |
| `0x8E0F50` | `void MovePlayerIconToArea(int areaID)` | teleport/relocate the icon (fast travel) |
| `0x8E1A20` | `void PlayerStopMove()` | stop |
| `0x8E1980` | `void PlayerRestButton(bool onPress)` | rest in place |

**[DUMP]** signatures · **[RUNTIME]** these symbols exist in the live method table (same RVAs).

Per-frame movement:
```
BigMapController.Update (0x8E3A70)  →  ManageAllHeroMove(float deltaTime) (0x8DDD60)
                                    →  ManageHeroMove(GameObject heroIcon, float deltaTime) (0x8DDEA0)
```
**[DUMP]** signatures; **[RUNTIME]** verified by scanning `ManageHeroMove`'s machine code — it calls
`BigmapNpcController.GetBigMapTravelSpeed()` (`0xC807D0` / `0xC807F0`), `HeroData.ManageHeroHorseMove`
(`0x89BCB0`), `SetHorseButton` (`0x8E2D60`) and the `BigMapPos` helpers (`0xC7D6E0/0xC7D740/0xC7D7B0`).

### 1.4 Speed model (this is where "189 % / 220 %" comes from)

```
HeroData.GetFinalTravelSpeed()          RVA 0x88B4D0
   ├─ no horse  → GetTravelSpeed(false,false)   RVA 0x895220
   └─ horse     → GetHorseTravelSpeed(...)      RVA 0x88E430  (and 0x88E540 = no-arg overload)
GetTravelSpeed()                        0x895340
GetWeighChangeTravelSpeed()             0x896ED0   ← equipment weight
GetWeatherChangeTravelSpeed()           0x896CD0   ← weather
GetTerrainChangeTravelSpeed()           0x894B30   ← terrain
GetHorseScore()                         0x88E350
BigmapNpcController.GetBigMapTravelSpeed()       0xC807D0 / 0xC807F0(HeroData)
GlobalData.BaseTravelSpeed      = 0.4    (static 0x120)   [RUNTIME]
GlobalData.BaseTravelTimeScale  = 2.4    (static 0x124)   [RUNTIME]
```
**[DUMP]** for the chain · **[RUNTIME]** for the two global constants.

**The sprint button is a speed multiplier, not a time-scale.** Disassembly of
`HeroData.GetHorseTravelSpeed(bool havePower, bool isSprint)` **[RUNTIME]**:

```
xmm6 = horseSpeed(horse)                      ; base speed from the horse item
xmm7 = (isSprint && havePower) ? 2*sprintStat(horse) + 20 : 0
xmm0 = havePower ? 1.0 : powerStat(horse) / 200
return (xmm6 + xmm7) * xmm0 / 100             ; constants 20 / 1.0 / 200 / 100 read from the float pool
```
So travel speed is stored and combined **in percent units** and divided by 100 at the end — exactly the
shape you would render as `189 %` (walk/horse) and `220 %` (sprint, i.e. the `2sprint + 20` bonus).
The two numbers as literal constants do **not** exist in the binary (searched `189`, `220`, `1.89`, `2.2`
across the whole dump — only `HeroSpeAddDataType livingSkill1ExpRate = 189`), so they are computed live
from the character's horse/power stats. **[GUESS]** that the HUD label is this percentage.

`BigMapController.fastMode` (`@0xE9`) is **only a UI state**: `FastModeButtonClicked` (`0x8D9310`) flips the
bool and repaints the button with `GlobalData.ButtonDarkYellowColor` (`0x398`); `SetFastModeState` (`0x8E2BE0`)
does the same when set programmatically. **[RUNTIME]** disassembly — it never touches `Time.timeScale`.

---

## 2. World clock

### 2.1 Where the date lives

```
WorldData (8181)
  0xA8  TimeData worldTime      → year @0x10, month @0x14, day @0x18   (8162)
  0xB0  float TimeDifficulty    [live 9.6667]
  0xB4  float hour              [live 21.6108]
GameController (8374)
  0x20  WorldData worldData
  0x28  bool needAutoSave
  0x2C  float checkUpdateTime
```
**[DUMP]** layout · **[RUNTIME]** values.

The HUD string `"10 ปี 9 เดือน 9 วัน"` read from `HudController.timeLabel` matches `worldTime = 10/9/9`
exactly — two independent paths, same numbers **[RUNTIME]**.

### 2.2 Who advances it

| RVA | Signature | Note |
|---|---|---|
| `0x7E3BF0` | `GameController.Update()` | per-frame driver; calls into `ManageHeroHorseTime`-class helpers per hero **[GUESS on the call edge]** |
| `0x790CA0` | `GameController.ChangeDayDirect(int deltaDay)` | hard jump |
| `0x793920` | `GameController.ChangeMonthDirect(int deltaMonth)` | hard jump |
| `0x795380` | `GameController.ChangeYearDirect(int deltaYear)` | hard jump |
| — | `GameController.ChangeDay(int deltaDay)` / `ChangeDay()` | normal (eventful) day change **[DUMP, no RVA captured]** |
| `0x89BD60` | `HeroData.ManageHeroHorseTime(float deltaTime)` | ticks the **horse** power/CD, *not* the calendar **[RUNTIME]** disasm: it only decrements `horseData[+0x44]` |

**Observation that matters [RUNTIME]:** with the game idle on the world map, `WorldData.hour` did **not**
change over 2.0 s (21.6108 → 21.6108). So the clock is **not** a real-time accumulator — it is advanced by
**actions** (travel, rest, work, events) and by the `ChangeDay*` calls. **[RUNTIME for the non-advance,
GUESS for the exact accrual sites]**

**Added 2026-10-20 — measured while the party was actually moving [RUNTIME]:** 2 minutes of continuous
movement on BigMap, sampled every 30 s from two independent paths (mod IPC `snapshot world` and Frida reading
`WorldData.hour` / `TimeData` directly):

| t | year/month/**day** | hour | HUD `timeLabel` | party pos |
|---|---|---|---|---|
| 13:34:34 | 10/9/**4** | 22.042572 | `10 ปี 9 เดือน 4 วัน` | -3353.3, 292.5 |
| 13:35:04 | 10/9/**7** | 22.090881 | `10 ปี 9 เดือน 7 วัน` | -1455.4, 608.0 |
| 13:35:34 | 10/9/**8** | 3.5628974 | `10 ปี 9 เดือน 8 วัน` | -1117.9, 489.4 |
| 13:36:04 | 10/9/**10** | 22.475193 | `10 ปี 9 เดือน 10 วัน` | -1757.9, -534.4 |
| 13:36:34 | 10/9/**13** | 21.851498 | `10 ปี 9 เดือน 13 วัน` | -3553.5, -2068.9 |

- HUD day matched the sampled `TimeData.day` in **5/5** samples; a Frida read at frame 384795 and the IPC
  record at frame 385768 returned the **same** `hour` (3.5628974) → sampler and interop agree, no sampling bug
- Clock advanced **+215.8 in-game hours (9 days) in 120 s**, and it moved in **jumps of +5.5 to +72 h per 30 s**
  that do **not** scale with the distance travelled (358 units → +5.5 h; 1,924 units → +72 h)
- ~~Which action emits each chunk is still unknown~~ — **resolved the same day** (see below)

**Rule measured from the continuous 2 Hz stream (12:48–13:39, 5,283 records) [RUNTIME]:** every pair of
consecutive records was classified as battle / moving (>5 map units) / static, using the battle markers:

| class | samples (0.5 s each) | ΣΔhour | max Δ/sample |
|---|---|---|---|
| battle | 2,664 (3 battles: HardFight, DeathFight ×2) | **0.00** | 0.00 |
| moving | 313 | **+476.40** | 2.88 |
| static (incl. dialogue modal) | 2,326 | **+0.14** | 0.10 |

- Clock accrues **only while the party moves on the map**, in proportion to distance travelled:
  median 86.1 units → median 1.296 h, **Pearson r = 0.839**, ratio median **0.0157 h/unit** (~1 h per 64 units)
- **Battles do not advance the clock** (2,664 samples, Δ = 0.00), and neither do idle/dialogue-modal samples
  (2,326 samples, +0.14 h total = float rounding)
- So `WorldData.hour` is a **distance accumulator on the map**, not a real-time clock and not an action timer;
  the +5.5…+72 h "chunks" seen at 30 s intervals were sums of per-step increments
- Not yet measured: walking inside a city/sect (`areaId != -1`), destination-pick travel, inn rest /
  `WorkingUIController` day-advance, and whether accrual is per distance or per path step (r = 0.839 < 1)
- Evidence: `evidence/S1-20261020-113926/worldtime-accrual.md` (mod repo)
- Confounder found: during the earlier "frozen" readings the game window was **not focused**
  (`Application.isFocused = false` while chatting), so idle + unfocused states must be separated before
  attributing a freeze to the game logic
- Evidence: `evidence/S1-20261020-113926/worldtime-walk-test.md` ใน mod repo (`C:\Users\memph\LongYinCoop`) + `worldtime-ipc-walk.ndjson`

Corroborating structure: `WorkingUIController` (the crafting/work modal) carries
`dayTime` (static `0x0`), `pauseTime` (static `0x4`), `nextDayTime` (`0x54`), `leftPauseTime` (`0x58`),
`leftWorkingDay` (`0x4C`), `totalWorkingDay` (`0x50`) **[DUMP + RUNTIME class exists]** — i.e. work advances
**days** on a timer while the modal is open.

### 2.3 189 % / 220 % and Time.timeScale

- `TimeScaleController` (`8628`: `paused @0x18`, `nowSlowTimeScale @0x1C`, `slowTimeDatas @0x20`,
  `SetSlowTime 0xAA7130`, `Update 0xAA71D0`) is a **battle/cinematic slow-mo stack** that writes
  `Time.timeScale`; it has nothing to do with map travel speed. Its live values on the map are
  `paused=false`, `nowSlowTimeScale=0`.
- The travel-speed percentage comes from `HeroData.GetHorseTravelSpeed` / `GetFinalTravelSpeed` (§1.4) —
  a **game multiplier**, not engine time.

**Answer:** the 189 %/220 % labels are the character's **travel speed in percent** (walk vs sprint), driven by
`isSprint` inside `HeroData.GetHorseTravelSpeed`. They are *not* `Time.timeScale` and *not* `fastMode`.
**[RUNTIME for the formula and for `fastMode` doing nothing to time; GUESS for the exact label mapping]**

### 2.4 What stops the clock

| Mechanism | Evidence |
|---|---|
| Action-based clock (nothing accrues while idle) | **[RUNTIME]** Δhour = 0 over 2 s |
| `SureMenu.pause` (`@0x28`) — confirm/pause popup, holds a `fucName/fucParam` callback | **[DUMP]** [`dump.cs:391572`](Il2CppDumper/dump.cs) |
| Modal UIs generally (shop / dialogue / plot / work) block input; the clock simply does not tick | **[GUESS]** — no global "time paused" flag exists in the dump |
| `TimeScaleController.paused` | only affects `Time.timeScale` (battle slow-mo) **[RUNTIME]** |
| Battle | separate scene flow — `BattleController` becomes authoritative; the map controller is idle **[RUNTIME, from battle-notes.md]** |

There is **no** `WorldData.paused` / `GameController.paused` field in this build — checked by grepping every
`paused`/`pause` field in the game classes. Time stops because **nothing calls `ChangeDay`**, not because a
flag is set. **[RUNTIME/DUMP]**

---

## 3. NPC parties on the map

### 3.1 The actor

`BigmapNpcController : MonoBehaviour` — TypeDefIndex **8109**

```
0x18  HeroData heroData          ← the same persistent record as the save file
0x20  GameObject CircleObj
0x30  GameObject heroFollowTarget
0x38  HeroFollowType heroFollowType
0x3C  float heroChaseTime
0x40  float followRangeRate
0x44  float heroStopChaseTime
0x48  GameObject selfSkeleton
0x50  GameObject heroSimpleSprite
0x58  GameObject heroMissionTarget
0x60  List<BigMapFollower> followers          ← party members trailing the leader
0x70/0x78/0x80  hover / interact / see range colliders
0xD0  GameObject BigMapFightIcon              ← the crossed-swords icon
0xD8  GameObject BigMapFightIconPrefab
0xF8  BigMapSpeEffectType inBigMapSpeEffectType
0xFD/0xFE/0xFF  inWater / inMountain / inHill flags
```
**[DUMP]** layout.

**Position is not stored on the controller** — it is `heroData.bigMapPos` (§1.1), i.e. one shared source of
truth per hero. The controller only renders/steers toward it. **[DUMP + RUNTIME]**

### 3.2 Who updates them

```
BigMapController.Update (0x8E3A70)
   → ManageAllHeroMove(deltaTime)  (0x8DDD60)     ; loops all icons
      → ManageHeroMove(heroIcon, deltaTime) (0x8DDEA0)
           ├─ BigmapNpcController.GetBigMapTravelSpeed()   (0xC807F0)
           ├─ HeroData.ManageHeroHorseMove(deltaTime)      (0x89BCB0)
           └─ BigMapPos helpers 0xC7D6E0 / 0xC7D740 / 0xC7D7B0
```
**[DUMP]** for the signatures, **[RUNTIME]** for the call edges (byte-scanned out of `ManageHeroMove`).

NPC destination/AI:
```
BigmapNpcController.GetHeroTargetPos()            0xC7E610
BigmapNpcController.ConsiderHeroFollowType(other) 0xC7F210
BigmapNpcController.SetSkeletonRunAnim(...)       0xC83E60
AIController.ManageAIOneDay(HeroData)             0x13E80A0   ← world-level daily AI (equip/skill/poison/move)
AIController.GetRandomMoveTargetArea(HeroData)    0x13E6D50
```

### 3.3 How many

```
WorldData.Heros        897 records   [RUNTIME]
WorldData.TempHeros    (temp/plot heroes)
BigMapController.bigmapNormalNpcIcons  33    [RUNTIME]
BigMapController.bigmapTempNpcIcons    70    [RUNTIME]
BigMapController.bigmapRandomEventIcons 124  [RUNTIME]
BigMapController.areaIcons             Dictionary<int, GameObject>
BigMapController.innIcons              Dictionary<int, GameObject>
BigMapController.resourcePoints         Dictionary<int, GameObject>
BigMapController.needRemoveBigMapNpc   List<HeroData>  (0 live)
```
Icons are created/removed by `CreateBigMapNpc(HeroData)` `0x8D8440`, `RefreshBigMapNPC` `0x8E2440/0x8E24A0`,
`RemoveBigMapNpc` `0x8E2770/0x8E27F0/0x8E28D0`, bulk rebuild `RecreateAllBigMapHeroIcon` `0x8E1EA0`.
**[DUMP + RUNTIME]**

So: 897 persistent characters, ~33 visible normal NPC leaders + 70 temporary (plot/mission) ones + 124 event
icons at any time; each leader can drag a `followers` list behind it.

---

## 4. Encounter → battle

### 4.1 The crossed-swords icon

`BigmapNpcController.BigMapFightIcon` (`@0xD0`) / `BigMapFightIconPrefab` (`@0xD8`) **[DUMP]** — an icon child
on the NPC that represents a fightable party.

Click/hover path: `BigMapController.MeetBigMapNpc(GameObject target)` `0x8DFFB0` **[DUMP + RUNTIME symbol]`.
Its machine code calls `PlayerStopMove` (`0x8E1A20`), `RemoveBigMapNpc` (`0x8E2770`) and several
UI/plot helpers, but **does not call `PrepareBattleMap` directly** **[RUNTIME, byte scan of the function]** —
the fight itself is launched by the event/mission/confirm layer after the meet dialog resolves. **[GUESS]**

### 4.2 What is handed to the battle (the actual contract)

`BattleController.PrepareBattleMap` — 11 overloads, all `void`:

```
0x8150C0  (BattleType, HeroData hero0, HeroData hero1, string fightEndCall, bool includeTeamMate, bool randomTrophy)
0x8152E0  (... , BattleMapTypeData)
0x814750  (BattleType, HeroData hero0, SupportType s0, HeroData hero1, SupportType s1, string fightEndCall, bool includeTeamMate, bool randomTrophy)
0x814F90  (... , BattleMapTypeData)
0x814C50  (BattleType, List<HeroData> heroList0, List<HeroData> heroList1, string fightEndCall, bool includeTeamMate, bool randomTrophy)
0x814A70  (... , BattleMapTypeData)
0x814BF0  (BattleType, List<HeroData> h0, SupportType s0, List<HeroData> h1, SupportType s1, string, bool, bool)
0x814DD0  (... , BattleMapTypeData)
0x8148A0  (... , BattleMapTypeData, int maxHeroNum)
0x815500  (BattleType, List<HeroData> h0, List<HeroData> h0Support, List<HeroData> h1, List<HeroData> h1Support,
           float supportEnterTime, string fightEndCall, bool includeTeamMate, bool randomTrophy,
           BattleMapTypeData, int maxHeroNum, float timeLimit = 0)
0x815CC0  (BattleType, List<List<HeroData>> fightMemData, List<List<HeroData>> fightSupportData,
           float supportEnterTime, string fightEndCall, bool randomTrophy, BattleMapTypeData,
           int maxHeroNum, float timeLimit = 0)
```
**[DUMP]** exact signatures. Three of the eleven RVA↔signature pairs (`0x8150C0`, `0x814A70`, `0x815CC0`) were
confirmed against the live metadata (method pointer → parameter list), so the table is not a blind positional
guess.

`BattleMapData` (TypeDefIndex **8071**) — the arena delivered alongside:
```
0x10 int mapID
0x18 BattleMapTypeData battleMapTypeData
0x20 int mapWidth   0x24 int mapHeight   0x28 int wallColumn
0x30 GridUnitData[,] mapGrids
0x38 List<GridUnitData> mustEmptyGrids
0x40 List<GridUnitData> normalGrids
0x48 List<GridUnitData> obstacleGrids
```

Two of these arguments are directly observable in a running fight **[RUNTIME, from battle-notes.md]**:
`BattleController.fightEndCallFuc = "HelpForceFightResult~1-DeathFight"` (the `fightEndCall` string, telling
the result where to return) and `battleIncludeTeamMate = true`.

**Chain summary**
```
crossed-swords icon (BigmapNpcController.BigMapFightIcon @0xD0)
  → player clicks NPC  → BigMapController.MeetBigMapNpc(GameObject)   0x8DFFB0
  → [dialog / event / confirm layer]                                  GUESS
  → BattleController.PrepareBattleMap(BattleType, List<HeroData> side0, List<HeroData> side1,
                                      string fightEndCall, includeTeamMate, randomTrophy,
                                      BattleMapTypeData, maxHeroNum, supportEnterTime, timeLimit)
  → BattleController.StartBattleButtonClicked → StartBattle           0x822030 / 0x822140
  → BattleMapData built, BattleState = Fighting
```

---

## 5. Top-bar resources (numeric strip)

`HudController : MonoBehaviour` — TypeDefIndex **8436** (instance `0x2a1b7696bb0`) **[RUNTIME]**

```
0x20 Text timeLabel          ← "10 ปี 9 เดือน 9 วัน"
0x28 Image timeCircle        ← day/night dial
0x30 Text nameLabel          ← "หวังเวิ่นเทียน"
0x38 Text fightScoreLabel    ← "26171"
0x40 Image weatherIcon
0x48 Image seasonIcon
0x50 Text fameLabel          ← "9999"
0x58 Text badfameLabel       ← "0"
0x60 GameObject badfameIcon
0x68 RectTransform moneyLayout
0x70 Text moneyLabel         ← "61058"
0x78 Text forceLabel         ← "สำนักเซียนเสีย<color=#FD1430>ประมุขสำนัก</color>"
0x88 Text contributionLabel  ← "995/3200"
0x90 Text heroNumLabel       ← "148/145"
0x98 Text areaNumLabel       ← "2/10"
0xE8 List<HudResourceShowData> hudResourceShowDatas   (live count 0)
       HudResourceShowData { int id @0x10, float num @0x14 }   (8435)
0xB8/0xC0/0xC8 GameObject externalInjury / internalInjury / poisonInjury
0xD0 quickMap  0xD8 forceDetail  0xE0 heroSearch
```

**Where the numbers actually come from [RUNTIME verified]:**
```
money       = playerHero.itemListData.money        (HeroData 0x220 → ItemListData 0x18 int)   live 61058 ✔
fame        = playerHero.fame                      (HeroData 0x1C4 float)                    live 9999  ✔
badFame     = playerHero.badFame                   (HeroData 0x1C8 float)                    live 0     ✔
force       = ForceData of playerHero.belongForceID (HeroData 0x84) + force job title       GUESS
heroNum     = counts of owned heroes   (148/145)                                            GUESS on source
areaNum     = counts of owned areas    (2/10)                                               GUESS on source
contribution= force contribution       (HeroData 0x1C0 forceContribution)                   GUESS
```
`HudController.Update` RVA `0xB0AAC0` refreshes these; `AddHudResourceShowData(HudResourceShowData)` feeds the
optional resource-chip list. **[DUMP]**

**Useful trick:** every label was read without calling anything by reading
`UnityEngine.UI.Text.m_Text` at **offset 0xE0** from the `Text` pointer **[RUNTIME]** — the same technique works
for the battle info feed.

---

## 6. Modal states that freeze the world

There is **no single "game paused" flag** in this build (grepped every `pause`/`paused` field). The world
freezes because the clock is action-driven (§2.2) and because input is blocked while a modal owns the UI.

| Modal | Class (TypeDefIndex) | Evidence |
|---|---|---|
| Confirm / generic "sure?" popup | `SureMenu` (8621) — `pause @0x28`, `fucName @0x30`, `fucParam @0x38`, `cancelFucName @0x40`, `objToSendMessage @0x50` | **[DUMP]** — `pause` is the only explicit pause flag in the UI layer |
| Work / crafting / study (advances **days**) | `WorkingUIController` (8652) — `working @0x48`, `leftWorkingDay @0x4C`, `totalWorkingDay @0x50`, `nextDayTime @0x54`, `leftPauseTime @0x58`, `dayTime` static `0x0`, `pauseTime` static `0x4`, `skipping @0x62`, `skipButton @0x68` | **[DUMP]**; class present at runtime |
| Battle | `BattleController.battlePaused @0x1D0` (+ `TimeScaleController.paused @0x18` for slow-mo) | **[RUNTIME]** (battle-notes.md §1.1/§6) |
| Plot / dialogue | `PlotController` + `SureMenu`-style flow; no dedicated pause field | **[GUESS]** — not verified this session |
| Shop / trade / inn | `TradeUIController`, `BookStoreController`, `FreeTradeUIController` — modals, no pause field | **[DUMP]** classes exist; pause behaviour **[GUESS]** |

**What actually stops the world clock:** while `BigMapController.ManageAllHeroMove` is not advancing anyone
(no target), no `ChangeDay*` call happens, so the date stands still. Opening a modal simply removes the
player's ability to issue a move. **[RUNTIME for "idle ⇒ Δhour = 0"; GUESS for the modal-to-input link]**

---

## 7. Evidence index

| Claim | Status |
|---|---|
| `HeroData.bigMapPos@0xC8` = `BigMapPos{x@0x10,y@0x14}`, live `(-1939.31, 311.86)` | **RUNTIME** |
| Map scale 8, half-extents 7680×4320, `BaseTravelSpeed 0.4`, `BaseTravelTimeScale 2.4` | **RUNTIME** |
| Move API names + RVAs (`SetPlayerMoveTargetArea` … `PlayerStopMove`) | **DUMP** + **RUNTIME** symbols |
| `Update → ManageAllHeroMove → ManageHeroMove` and its callees | **RUNTIME** (byte scan) |
| Sprint = `isSprint` in `GetHorseTravelSpeed`, formula with 20/1.0/200/100 | **RUNTIME** (disasm + float pool) |
| 189 %/220 % are that percentage, not `Time.timeScale` | **GUESS** (no literal constants in the binary) |
| `fastMode` is UI-only, never touches `Time.timeScale` | **RUNTIME** (disasm of `FastModeButtonClicked` / `SetFastModeState`) |
| `worldTime = 10/9/9`, `hour 21.6108`, `TimeDifficulty 9.6667` | **RUNTIME** |
| Clock does not advance while idle (Δhour = 0 over 2 s) | **RUNTIME** |
| Who accrues `hour` during travel | **GUESS** (per-frame writer not located; `ManageHeroHorseTime` only ticks horse power) |
| 897 hero records, 33/70/124 icon buckets | **RUNTIME** |
| NPC position lives in `HeroData.bigMapPos`, not in the controller | **DUMP** + **RUNTIME** |
| Player's map avatar is driven by the same `BigmapNpcController` class | **GUESS** (avatar is a GameObject; controller binding not read) |
| Crossed-swords icon = `BigmapNpcController.BigMapFightIcon @0xD0` | **DUMP** |
| `MeetBigMapNpc` does not call `PrepareBattleMap` directly | **RUNTIME** (byte scan of `0x8DFFB0`) |
| `PrepareBattleMap` 11 overloads + `BattleMapData` layout | **DUMP** |
| HUD label → field mapping (money/fame/badFame) | **RUNTIME** (values match exactly) |
| heroNum / areaNum / contribution sources | **GUESS** |
| "No global paused flag" | **DUMP** (grepped all pause fields) |

### Things not established (and the cheapest next step)

1. **Exact accrual site of `WorldData.hour`.** Scan `GameController.Update` (`0x7E3BF0`) and
   `BigMapController.ManageHeroMove` (`0x8DDEA0`) for float stores to `+0xB4`, or set a read-only watch on the
   field while walking (needs a hook → out of read-only scope).
2. **Which UI prints 189 %/220 %.** Find the `Text` under `BigMapController.horseUI` (`@0xD0`) and read
   `Text.m_Text` (offset `0xE0`) — pure read.
3. **The exact encounter → `PrepareBattleMap` caller.** The 338 hits from a naive module scan of the 11 overload
   addresses are not trustworthy without instruction-alignment validation; start instead from
   `MeetBigMapNpc`'s string/callback parameters and the mission/plot entry points.
