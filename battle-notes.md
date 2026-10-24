# LongYinLiZhiZhuan — Battle System Notes (read-only survey)

> Session constraints: **read-only**. No memory writes, no method invocation. Everything below came from
> reading memory / metadata / disassembly only.
> Game was mid-battle (`battleState = Fighting`) while this was captured.

## How to read the evidence tags

| Tag | Meaning |
|---|---|
| **[RUNTIME]** | Verified live from the running process (instance fields, values, call-graph scan of machine code) |
| **[DUMP]** | From `Il2CppDumper/dump.cs` + `il2cpp.h` only — signatures & offsets are reliable, *behaviour* is inferred |
| **[GUESS]** | Inference. Not proven. Treat as hypothesis for co-op design |

**Module base (GameAssembly.dll): `0x7ffa2d2e0000`**
All RVAs below are module-relative; absolute = base + RVA. RVA column matches `dump.cs` "RVA:" values,
so dump signatures and runtime addresses join 1:1.

---

## 0. Live battle snapshot **[RUNTIME]**

> ⚠️ **หมายเหตุ 2026-10-20:** ค่า `battlePaused 1 (true)` ใน snapshot นี้ **ขัดกับ** การวัดของ S1.5-b (false 82,695/82,695 sample ในศึก HardFight/StudyFight รวมเทิร์นผู้เล่น) — ยังไม่รู้ว่าเกิดจากประเภทศึก, การกดหยุดศึก, หรือวิธีอ่าน; **อย่าใช้ฟิลด์นี้ตัดสินว่า "ถึงตาผู้เล่น"** จนกว่าจะปิดคำถามนี้

```
BattleController instance   0x2a1b826e540   (singleton, get_Instance RVA 0x8257C0)
battleType                  2  = DeathFight
battleState                 3  = Fighting
nowActiveState              2  = WaitPlayerInput
battlePaused                1  (true)
inited                      true
havePlayer                  true
nowActiveUnit = playerBattleUnit = 0x2a22e81b120   → "หวังเวิ่นเทียน" heroID 0
battleTime                  1.84 s     timeLimit 0  (no time limit this fight)
nowScale                    1
fightEndCallFuc             "HelpForceFightResult~1-DeathFight"
teams                       [0] player 11 units, [1] enemy 7 units
teamMemJoinBattleNum        [11, 7]
teamTotalFightScore         [112943.97, 30474.53]
AIBattlePlans               46 plans, bestPlanValue 52.48, AIWaitTime -0.0069 (counting down)
managedGrids                4
```

---

## 1. BattleController

`BattleController : MonoBehaviour` — TypeDefIndex **8070**, 166 methods **[DUMP]**
[`dump.cs:363003`](Il2CppDumper/dump.cs) · instance found via `gc_choose` **[RUNTIME]**

### 1.1 Field map (offsets verified against the live instance)

**Identity / lifecycle**
```
_instance            static 0x50      get_Instance RVA 0x8257C0
battleType                  0x20      BattleType  (StudyFight=0, HardFight=1, DeathFight=2)
battleState                 0x24      BattleState (None,Prepare,Ready,Fighting,End,Exception,Loot)
fightEndCallFuc             0x28      string callback, e.g. "HelpForceFightResult~1-DeathFight"
winTeamID                   0x30      int
maxHeroNum                  0x34      int (-1 = unlimited)
battlePaused                0x1D0     bool   ← ❗ หลักฐานขัดกัน: snapshot §0 อ่านได้ 1 (true) ตอน WaitPlayerInput แต่ S1.5-b วัดได้ false 82,695/82,695 — ยังไม่รู้สาเหตุ · ห้ามใช้เป็นตัวตัดสินเทิร์น (ดู §1.2)
noSkip                      0x1D1     bool
inited                      0x1A4     bool
extraFinishTime             0x1A8     float
```

**Teams / rosters**
```
teams                       0x70      List<BattleTeam>            ← the actual combatants
teamMemData                 0x40      List<List<HeroData>>        ← roster per team (11 / 7)
teamSupportData             0x48      List<List<HeroData>>        ← reinforcements / allies that join later
teamMemPrepareData          0x50      List<List<TeamMemPrepareData>>
teamSupportPrepareData      0x68      List<List<TeamMemPrepareData>>
teamMemJoinBattleNum        0x58      List<int>  (11 / 7)
teamTotalFightScore         0x60      List<float> (112943.97 / 30474.53)
baseSupportEnterTime        0x78      float — when supports enter
battleIncludeTeamMate       0x90      bool (true)
playerSpeControlTeamID      0x8C      int (-1 = not overridden)
```

**Turn / active unit**
```
nowActiveUnit               0x110     BattleUnit
nowActiveState              0x124     ActiveUnitBattleState
nowActiveUnitMoved          0x120     bool
nowActiveUnitAttacked       0x121     bool
playingAnim                 0x128     bool
activeUnitList (private)    0x2F8     List<BattleUnit> (size 1 in this snapshot)
managedUnits (private)      0x308     List<BattleUnit>
managedGrids (private)      0x310     List<GridUnitData> (4)
originUnitData              0x118     GridUnitData (pre-move snapshot for undo)
```

**Time**
```
battleTime                  0x1C8     float (real seconds, keeps ticking)
timeLimit                   0x1CC     float (0 = none)
nowScale                    0xE0      float (zoom, not time)
AIWaitTime                  0x220     float (countdown before AI acts)
BattleMaxTime (static)      0x0       List<float> per battle-type cap
```

**Grid / targeting**
```
gridUnits                   0x100     GameObject[,]
selectedGrid                0x108     GameObject
attackComfirmed             0x210     bool   (field ไม่ใช่ method — ห้ามใช้เป็นจุด hook) [DUMP dump.cs:363082]
moveRangeGridUnits          0x1D8     List<GridUnitData>
movePath                    0x1E0     List<GridUnitData>
attackRangeGridUnits        0x1F8     List<GridUnitData>
attackRangeGridGroups       0x200     List<List<GridUnitData>>
damageRangeGridUnits        0x208     List<GridUnitData>
originUnitData              0x118
```

**AI scratch**
```
AIBattlePlans               0x228     List<AIBattlePlan> (46 live)
AIBattleChoiceList          0x230     List<int>
bestPlanValue               0x238     float
AIBattleChoice              0x23C     int
AIBattleAttackChoice        0x240     int
```

**Battle-log / info feed**
```
infoText                    0x188     GameObject (UI Text)
infoCount                   0x1A0     int
cachedBattleTimeText        0x2E8
```

### 1.2 Turn-based or real-time?

**Turn-based, driven per frame.** Evidence:

- One unit is "active" at a time (`nowActiveUnit`, `activeUnitList.Count == 1`) and each unit has an
  explicit action state: `ActiveUnitBattleState { None, WaitAIPlan, WaitPlayerInput, Rest,
  ChooseUseItemTarget, UseItem, ShowMovingRage, WaitMoving, Moving, ShowAttackingRage, WaitAttacking,
  Attacking, Finish, Finishing }`. **[DUMP]**
- ⚠️ **หลักฐานขัดกัน (บันทึก 2026-10-20 หลัง S1.5-b):** ข้อความเดิม "`battlePaused = true` while the player is choosing an action **[RUNTIME]**" มาจาก snapshot §0 (อ่านได้ 1 ตอน `WaitPlayerInput`) แต่การวัด S1.5-b พบ `true` **0 จาก 82,695 sample** ที่ `inBattle=true` (รวม 41,777 sample `activePlayerControl=true` + `WaitPlayerInput`) **[RUNTIME S1.5-b · evidence/S1.5b-20261020-144353/S1.5b-report.md §3]**
  → **ยังไม่รู้สาเหตุที่ขัดกัน** (ผู้ต้องสงสัย: ประเภทศึก DeathFight vs HardFight/StudyFight, จังหวะที่ผู้เล่นกดหยุดศึก, หรือวิธีอ่านค่าต่างกัน) — ต้องวัดเพิ่มก่อนสรุป **[GUESS]**
  → ข้อปฏิบัติที่ปลอดภัย: **ห้ามใช้ `battlePaused` ตัดสินว่าถึงตาผู้เล่น**; ใช้ `nowActiveState == WaitPlayerInput` (+ `nowActiveUnit.playerControl`) [RUNTIME] และ `BattlePauseButtonClicked` คือผู้ต้องสงสัยเดียวใน dump สำหรับการ toggle **[GUESS]**
- `nowActiveUnitMoved` / `nowActiveUnitAttacked` = per-turn "already used move / already used attack" flags **[DUMP + RUNTIME]**
- Rounds exist: `FinishRound`, `ManageHeroBuffRound` **[RUNTIME call-graph]**
- …but the loop is **frame/real-time**: `Update()` → `RunBattle()` every frame, with
  `Time.deltaTime`-style ticking (`GetBattleDeltaTime`), real-time skill cooldowns
  (`ManageHeroSkillCdTime`), buff timers in seconds (`ManageHeroBuffTime`), a running `battleTime`,
  an optional `timeLimit`, and a `TimeScaleController` for slow-mo. **[RUNTIME]**

**Summary for co-op:** state machine is turn-based (locally simulatable), but timing/animation is
real-time and time-scaled, so a lockstep design cannot assume fixed tick counts unless animation time
is also synced or skipped.

---

## 2. Battle flow methods (name / signature / RVA)

RVA taken from the live method table (`il2cpp_list_methods`) — identical numbering to `dump.cs`. **[RUNTIME]**

### 2.1 Entry
| RVA | Signature | Note |
|---|---|---|
| `0x822030` | `void StartBattleButtonClicked()` | UI "start" |
| `0x822140` | `IEnumerator StartBattle()` | coroutine bootstrap |
| `0x7FC470` | `void BattleTeamPrepare()` | roster → prefabs |
| `0x7F9640` | `void AddNewTeamPreparePrefab()` | |
| `0x8150C0,0x8152E0,0x814750,0x814F90,0x814C50,0x814A70,0x814BF0,0x814DD0,0x8148A0,0x815500,0x815CC0` | `void PrepareBattleMap()` ×11 overloads | grid + map build |
| `0x8231B0` | `IEnumerator TeamEnterBattleField()` | |
| `0x80A650` | `IEnumerator HeroEnterBattleFieldCoroutine()` | |
| `0x80A730` | `GameObject HeroEnterBattleField()` | |
| `0x80AFA0` | `IEnumerator HeroEnterGridDelay()` | |
| `0x81F050` | `void SetNowActiveUnitUI()` | first turn highlight |

### 2.2 Turn loop (proven call-graph) **[RUNTIME]**
```
Update (0x823DB0) ──▶ RunBattle (0x81AAF0)                    [1 call, verified by scanning code bytes]
RunBattle ──▶ GetBattleDeltaTime   ×3
          ──▶ ManageHeroSkillCdTime (0x8127A0)
          ──▶ ManageHeroBuffRound  (0x8115E0)
          ──▶ GenerateAIPlanUseSkill (0x802CC0)
          ──▶ GetValueableAIBattlePlan (0x809240)  ×2
          ──▶ GetBestValueGrid (0x806D90)
          ──▶ GenerateMovePath (0x804DD0)  ×2
```
Other loop helpers: `ManageHeroBuffTime 0x812480`, `BattlePaused 0x7FAA80`,
`GetBattleDeltaTime 0x806320`, `FinishRound 0x802840`, `ResetAllAIPlans 0x81A7A0`.

### 2.3 Player action
| RVA | Signature |
|---|---|
| `0x7FDB50` | `bool BattleUnitControlable()` |
| `0x7FDBD0` | `bool CanPlayerControl()` |
| `0x80A490` | `bool HavePlayerControlUnit()` |
| `0x808220` | `int GetPlayerControlTeamID()` |
| `0x7FA090` | `void BattleGridClicked(GameObject clickedGrid)` — entry ที่ UI เรียกเมื่อคลิกช่องกริด **[DUMP `dump.cs:363169`]**; ยังไม่ได้ re-resolve/ยืนยันตอนรัน |
| `0x813FC0` | `void MoveButtonClicked()` |
| `0x7FDEE0` | `void CancelMove()` |
| `0x81A4D0` | `void RegretMove()` |
| `0x7FDFA0` | `void ChangeActiveSkill()` |
| `0x7FE0A0` | `void ChangeNowActiveAttackSkill()` |
| `0x7FDE90` | `void CancelAttack()` |
| `0x80B9A0` / `0x80B7A0` | `void ItemButtonClicked()` / `ItemButtonChoosen()` |
| `0x81A940` | `void RestButtonClicked()` |
| `0x800800` | `void CureButtonClicked()` |
| `0x8146C0` | `void PostureButtonClicked()` |
| `0x804DD0` | `void GenerateMovePath()` |
| `0x809AC0` | `void HandleActiveUnitFastKeys()` |

### 2.4 Action resolution (attack pipeline)
| RVA | Signature |
|---|---|
| `0x8143E0` | `IEnumerator PlayBattleUnitAttack()` |
| `0x7FDAE0` | `IEnumerator BattleUnitAttackStart()` |
| `0x7FD9D0` | `IEnumerator BattleUnitAttackHappen()` |
| `0x7FDA40` | `IEnumerator BattleUnitAttackHit()` |
| `0x7FD960` | `IEnumerator BattleUnitAttackFinish()` |
| `0x7FD8E0` | `IEnumerator BattleUnitAttackEnd()` |
| `0x814450` | `IEnumerator PlayBattleUnitMove()` |
| `0x80CA10` | `bool ManageDamage()` |
| `0x812930` | `bool ManageRecover()` |
| `0x8006E0` / `0x800770` / `0x8001C0` | `void CreateSpeEffect()` ×3 overloads |
| `0x813260` | `void ManageSkillSpeEffect()` |
| `0x813810` | `void ManageUnitStaySpeGrid()` |
| `0x7FF130` | `float CountBaseDamage()` |
| `0x7FFDF0` | `static float CountDamageRatio()` |
| `0x80BD70` | `static float LimitDamageRatio()` |
| `0x7FE8D0` | `void CheckAllDead()` |

### 2.5 End of battle
| RVA | Signature | Note |
|---|---|---|
| `0x7FE9E0` | `void CheckBattleEnd()` | win/lose evaluation |
| `0x7FA020` | `IEnumerator BattleEnd()` | called via StartCoroutine (method-pointer, not a direct call — so it does **not** show up in a byte scan) **[GUESS on the link, DUMP on the signature]** |
| `0x7FAAA0` | `void BattleRealEnd()` | teardown |
| `0x7FFF20` | `float CountPlayerBattleScore()` | |
| `0x7FFE20` | `float CountHeroBattleContribution()` | |
| `0x81A820` | `void ResetGridUnitsToNormal()` | |

### 2.6 Pause / skip / timescale / support
| RVA | Signature |
|---|---|
| `0x7FAA10` | `void BattlePauseButtonClicked()` |
| `0x7FC3D0` | `void BattleSkipButtonClicked()` |
| `0x822780` | `void SureSkipBattle()` |
| `0x7FD7C0` | `void BattleTimeScaleButtonClicked()` |
| `0x807A20` / `0x809170` | `float GetHalfBattleTimeScale()` / `GetThirdBattleTimeScale()` |
| `0x7FF060` | `IEnumerator CheckSupport()` |
| `0x8085F0` / `0x8085D0` | `List<HeroData> GetSupportList()` / `float GetSupportEnterTime()` |
| `0x823250` | `void ToggleAllButtonClicked()` |
| `0x81ECA0` | `void SetAllAuto(bool auto)` — แก้ 2026-10-20: dump มีพารามิเตอร์ 1 ตัว (`dump.cs:363654`) **[DUMP]** |
| `0x809A20` / `0x822660` | `void GiveUpBattleButtonClicked()` / `SureGiveUpBattle()` |

---

## 3. Who fights in battle, and where they live

```
BattleController.teams            List<BattleTeam>                                    [RUNTIME, 2 entries]
  BattleTeam (plain class, TypeDefIndex 8072)
    0x10  int ID
    0x14  bool havePlayer
    0x18  List<BattleUnit> battleUnits          ← the actual combatants
    0x20  List<BattleUnit> needProtectUnits     ← escort / protect-objective units
    0x28  bool needProtectUnitDestroyed
    0x30  HeroSpeAddData teamSpeAddData         ← team-wide stat aura

BattleUnit : MonoBehaviour (TypeDefIndex 8081)   ← per-fighter runtime object
    0x38  bool playerControl                    ← this unit accepts player input
    0x40  HeroData heroData                     ← the persistent character
    0x48  BattleUnit summonSourceHero           ← owner, if summoned
    0x50  KungfuSkillLvData summonSourceSkill   ← the skill that summoned it
    0x58  BattleTeam battleTeam
    0x60  GridUnitData mapGrid
    0xA8  BattleInfoData battleInfo             ← per-fight stats
    0xB0  bool autoFight
    0xB4  float battleMove
    0xB8  bool moved / 0xB9 attacked / 0xBA reborn
    0xBC  int stepMoved
    0xC0..0xD0  originHp/originMp/origin*Injury  ← pre-action snapshot for undo
    0xD8  KungfuSkillLvData nowOnAttackSkill

GridUnitData (TypeDefIndex 8093)
    0x10  int mapID
    0x14  GridType gridType
    0x18  BattleUnit battleUnit
    0x20  int passes
    0x24  int row
    0x28  int column
    0x30  ObstacleData obstale
```

**Link to `HeroData`:** `BattleUnit.heroData` is a direct pointer to the save-file struct.
`BattleController.teamMemData[i]` is the same `HeroData` objects as a plain list, so
`teams[i].battleUnits[k].heroData` ≡ `teamMemData[i][k]` (same instances, order may differ).

**[RUNTIME] verified:** team 0 = player side (11 units, `havePlayer=1`), team 1 = 7 enemies.
The player avatar is `heroID 0` / `playerControl=1` / `nowActiveUnit`. All 18 units had
`moved=0, attacked=0` at snapshot time (fresh turn).

**Note on `BattleInfoData` @0xA8** (TypeDefIndex 8044): `enemyKilled, enemyKillScorePercent,
makeDamage, takeDamage` — this is the per-unit battle log, not a text log.

---

## 4. What writes the battle log

**There is no persistent battle-log class in the dump.** No `BattleLog`, `FightRecord`, `BattleRecord`
type exists. What exists:

| RVA | Symbol | Evidence |
|---|---|---|
| `0x7F9310` | `void BattleController.AddInfoText(string addInfo, bool newLine = true)` | **[DUMP]** signature; **[RUNTIME]** disassembly shows it reading `this+0x188` (infoText GameObject) and resolving a `Text` component before writing |
| — (`AddInfoText` callers) | 31 call sites — see §4.1 | **[RUNTIME]** whole-module scan |
| `0x8D4F60` | `void BattleUnit.ShowTextOnHead()` | **[DUMP]** floating combat text |
| `0x...` | `HeroData.recordLog` | **[DUMP]** per-hero log list |
| `0x7FFF20` / `0x7FFE20` | `CountPlayerBattleScore()` / `CountHeroBattleContribution()` | **[DUMP]** post-battle results |

**Conclusion:** the visible "battle log" is a UI text feed in `BattleController.infoText`, written via
`AddInfoText`. If co-op needs a canonical log, it must be produced by tapping `AddInfoText` (or by
scraping `BattleInfoData` per unit + `battleState` transitions) — there is no log file on disk.

### 4.1 Complete `AddInfoText` caller map (whole-module scan) **[RUNTIME]**

Method: enumerate every `r-x` range of `GameAssembly.dll` (base `0x7ffa2d2e0000`, 25,948,160 bytes of
code), byte-scan for `E8`/`E9 rel32` resolving to `0x7ffa2dad9310` (RVA `0x7F9310`), then map each
call site to its owning method by nearest method-start RVA in `dump.cs` (62,298 methods indexed).
Boundary sanity check: the only `int3` padding in `[0x80CA10, 0x8115E0)` is at `0x8115D6` (end of
`ManageDamage`), so the 19 calls from `0x80D47C` to `0x810D90` really are inside that one function.

**Result: 31 direct call sites, 0 tail-jumps (`E9`), 0 indirect call hits.**

| owner | call-site RVAs | # |
|---|---|---|
| `BattleController.ManageDamage(BattleUnit, BattleUnit, bool)` | `0x80D47C 0x80EA09 0x80F04F 0x80F214 0x80F307 0x80F3F1 0x80F519 0x80F6CA 0x80F830 0x80FA6B 0x80FBBB 0x80FD0B 0x80FED7 0x80FFB7 0x810061 0x810123 0x8101F6 0x8102B2 0x810D90` | 19 |
| `BattleController.AutoChangeInjury(BattleUnit, BattleUnit, float, bool)` | `0x7F9CCC` | 1 |
| `BattleController.ManageRecover(BattleUnit, BattleUnit)` | `0x812CA6` | 1 |
| `BattleController.ManageUnitStaySpeGrid(BattleUnit)` | `0x813F28` | 1 |
| `BattleUnit.AddBuff(int, float)` | `0x8CE08B` | 1 |
| `BattleUnit.CheckDead()` | `0x8D046D` | 1 |
| `BattleUnit.UseMedFood(ItemData, HeroData)` | `0x8D5BE0` | 1 |
| `BattleController.<BattleUnitAttackHit>d__259` (coroutine `MoveNext`) | `0x94ABF0 0x94BABF` | 2 |
| `BattleController.<PlayBattleUnitAttack>d__246` | `0x94FAE2` | 1 |
| `BattleController.<PlayCure>d__238` | `0x950548` | 1 |
| `BattleController.<PlayPosture>d__239` | `0x950B57` | 1 |
| `BattleController.<PlayRest>d__237` | `0x950F53` | 1 |

**What the call sites pass as the string** (recovered by decoding the rip-relative `mov rdx` that feeds
the call):

| call site | literal found |
|---|---|
| `0x80D47C` | `"NoEvadeBalance"` — a **localization key**, not display text |
| `0x80F04F`, `0x80F307`, `0x80F3F1`, `0x80F519`, `0x80F6CA`, `0x80F830`, `0x80FA6B`, `0x80FBBB`, `0x80FD0B`, `0x80FED7`, `0x80FFB7`, `0x810061`, `0x810123`, `0x8101F6`, `0x8102B2`, `0x94BABF` | `"f0"` — the float format specifier handed to a number→string helper |
| `0x813F28` | `"\n"` |
| `0x8D5BE0` | `"。"` (CJK full stop) |
| `0x80EA09`, `0x80F214`, `0x810D90`, `0x812CA6`, `0x8CE08B`, `0x8D046D`, `0x94ABF0`, `0x94FAE2`, `0x950548`, `0x950B57`, `0x950F53` | no literal — text built at runtime (`string.Format` / interpolated) |

So the log body is **composed per hit** (format string + localized key + numbers), which is why a
byte-scan of literals yields mostly `"f0"` and `"\n"` rather than readable log lines.

**How to capture the complete log without writing to the game:**

1. `BattleController.infoCount` (`BC+0x1A0`) increments once per `AddInfoText` call — it was `82`
after ~2.6 s of fighting **[RUNTIME]**. Poll it as a cheap "new log line" cursor.
2. The rendered feed lives on the `Text` component of `infoText` (`BC+0x188`); the raw string is
`UnityEngine.UI.Text.m_Text` at **offset `0xE0`** **[DUMP]**. Reading it needs a
GameObject→component walk (pure reads), or an `Interceptor` hook on `AddInfoText` (a code patch —
out of scope for the read-only prototype).
3. Per-unit numeric log (`makeDamage` / `takeDamage` / `enemyKilled`) is available with zero extra
cost from `BattleInfoData` — already sampled by `coop-sync/coop-snapshot.js`.

---

## 5. RNG: which one, and the seed

**Two RNGs are in play, and the battle code uses the Unity one.** Proven by scanning the actual
machine code of each method for call targets **[RUNTIME]**:

| Method | RNG used | Hits |
|---|---|---|
| `BattleController.ManageDamage` (0x80CA10) | `UnityEngine.Random.value` | ×1 |
| `BattleController.ManageRecover` (0x812930) | `UnityEngine.Random.value` | ×3 |
| `BattleController.AutoButtonClicked` (0x7F9720) | `Random.value` ×3, `Random.Range(float)` ×4 | ×7 |
| `BattleController.GetValueableAIBattlePlan` (0x809240) | `UnityEngine.Random.Range(int,int)` | ×1 |
| `BattleController.GetBestValueGrid` (0x806D90) | `UnityEngine.Random.Range(int,int)` | ×2 |
| `CountBaseDamage`, `CountDamageRatio`, `GetNowActiveUnitComboRate`, `GetDamageRangeAntiCombo`, `CheckBattleEnd`, `GenerateAIPlanUseSkill`, `FinishRound`, `BattleUnitAttack*` | no direct RNG call in first 0x900 bytes | — |

So: **`UnityEngine.Random` (global Unity static) decides damage variance, recovery, auto-fight picks and
AI tie-breaks.**

**The game also has its own RNG wrapper** (used by world/plot/event systems, not observed in battle):

```
GlobalData (TypeDefIndex 8397, 110 methods / 236 fields, class 0x2a10960b600)
  static +0x0  6C0   int  randomSeed                    [RUNTIME value: 25,654,550]
  static +0x6C8       ThreadLocal<System.Random> threadRandom   [RUNTIME non-null]
  0xA85210  static Random GetThreadRandom()
  0xA872C0  static float  RandomRange(float, float, Random)
  0xA87220  static int    RandomRange(int, int, Random)
  0xA871A0  static double RandomRangeDouble(Random)
```
`RandomRange` disassembly: if the `Random` arg is null it calls `GetThreadRandom()`, then invokes the
`Random` virtual slot at `+0x1A8` (`NextDouble`) and lerps **[RUNTIME disasm]**.
`GetThreadRandom` just returns `threadRandom.Value` **[RUNTIME disasm]**.

Related per-system seeds **[DUMP]**: `EventData.randomSeed` @0x9C, `MissionData.missionRandomSeed` @0x70,
`PlotController.GetNowEventRandomSeed()` → returns `System.Random` (RVA 0x..., TypeDefIndex 8521 area),
`UnityEngine.Random` static class TypeDefIndex 4053, `System.Random` TypeDefIndex 296.

**Seed source:** `GlobalData.randomSeed` is a *private static* field and is not exposed by any setter
(nothing named `SetRandomSeed`/`InitState` exists in the dump). It is populated in `GlobalData..cctor`
(RVA `0xA8A670`); that function is a huge metadata initializer and the assignment site was **not**
traced. Live value was `25,654,550` **[RUNTIME]**. Whether it is `new Random().Next()` (i.e. from
`Environment.TickCount`) or derived from save/world time is **[GUESS]**.

### Co-op implication (important)
- Battle rolls come from **`UnityEngine.Random`, a process-global generator with no per-battle seeding
  and no observable seed field**. Two clients running the same fight **will not** produce the same rolls.
  Lockstep/deterministic-replay co-op is therefore not viable without replacing or seeding that RNG.
- The game's own `GlobalData.RandomRange(...)` is *not* used by the battle damage path, so hooking it
  will not capture combat rolls.

---

## 6. TimeScaleController

`TimeScaleController : MonoBehaviour` — TypeDefIndex 8628 **[DUMP]**
[`dump.cs:391804`](Il2CppDumper/dump.cs) · instance `0x2a1b80d22d0` **[RUNTIME]**

```
0x18  bool paused                  [live: false]
0x1C  float nowSlowTimeScale       [live: 0]
0x20  List<SlowTimeData> slowTimeDatas  [live: list present]
  SlowTimeData: 0x10 float slowTime, 0x14 float slowTimeScale

0xAA7450  static TimeScaleController get_Instance()
0xAA70E0  void Awake()
0xAA71D0  void Update()
0xAA7130  void SetSlowTime(float time, float scale = 0.2)
```

**How it works [RUNTIME disasm of `Update` @0x7ffa2dd871d0]:**
1. `if (paused) return;`
2. If `slowTimeDatas.Count > 0` → slow-motion branch: sets `nowSlowTimeScale = 1.0f`, iterates the
   active `SlowTimeData` entries and blends their `slowTimeScale` into `Time.timeScale` (uses
   `Time.get_timeScale` 0x2E089F70 / `Time.set_timeScale` 0x2E08A030 and a real-time delta helper
   0x2DFDD4A0).
3. Else → restores `Time.timeScale = 1.0f`.

So it is a small **slow-mo stack**: `SetSlowTime(seconds, scale)` pushes an entry, `Update` picks the
effective scale and writes `Time.timeScale`, then removes expired entries.
`BattleController` consumes it through `GetBattleDeltaTime()` (RVA 0x806320) and the
`BattleTimeScaleButtonClicked` / `GetHalfBattleTimeScale` / `GetThirdBattleTimeScale` (battle speed UI).

**Co-op implication:** `Time.timeScale` is global to the process — any slow-mo (hit-stop, skill
cinematic) affects every timer in both clients; drive shared timers from `battleTime`, not
`Time.timeScale`, or freeze `Time.timeScale` while syncing.

---

## 7. NPC decision making

**Battle AI lives in `BattleController`, not `AIController`.**

`AIController` (TypeDefIndex 8010, `AIController : MonoBehaviour`) is the **world-map / daily-life AI**:
`ManageAIOneDay(HeroData)`, `AICheckEquipment(HeroData)`, `AICheckSkill(HeroData)`,
`AICheckSpeMed(HeroData)`, `ManageAIUsePoison(HeroData)`, `CheckInteractTarget(HeroData)`,
`GetRandomMoveTargetArea(HeroData)`, `CheckHeroNeedMove(HeroData)`, `CanLeaveArea()`,
`GetSkillScore(HeroData, KungfuSkillLvData)` **[DUMP]**. It does not touch `BattleUnit`.

Battle-side planning:
```
BattleController.GenerateAIPlanUseSkill (0x802CC0)   ← builds plans      [RUNTIME: called from RunBattle]
BattleController.GetValueableAIBattlePlan (0x809240) ← picks best plan   [RUNTIME: ×2, RNG tie-break]
BattleController.GetBestValueGrid (0x806D90)         ← positional choice [RUNTIME, 2× Random.Range(int)]
BattleController.ResetAllAIPlans (0x81A7A0)
BattleController.GetBestEmenyDistanceGrid (0x8066B0) / GetBestPlayerDistanceGrid (0x806AE0)
BattleUnit.AISettingControlable (0x8CD760) / ChangeAutoType (0x8CE260, 0x8CE2D0)
```
Plan data:
```
AIBattlePlan (TypeDefIndex 8278)
   0x10 AIBattlePlanType battlePlanType
   0x14 float bestPlanValue
   0x18 GridUnitData moveTarget
   0x20 ItemData useItem
   0x28 List<AIBattleAttackPlan> battleAttackPlan
AIBattleAttackPlan (8279): 0x10 float planValue, 0x14 int activeSkill,
                           0x18 List<GridUnitData> attackTargetGridUnits
```
Live: `AIBattlePlans.Count = 46`, `bestPlanValue = 52.48`, `AIBattleChoiceList.Count = 1`,
`AIWaitTime` counting down from ~0 **[RUNTIME]** — the plan set is rebuilt every AI evaluation, not per round.

`BattleAiMenuController` / `SetAllAuto` / `AutoButtonClicked` form the auto-fight UI, whose RNG picks are
part of the same `UnityEngine.Random` stream.

---

## 8. Summons and "Party"

### 8.1 Summon
```
HeroData:                             BattleUnit:
 0x10 bool isSummon                    0x48 BattleUnit summonSourceHero
 0x14 int  summonID                    0x50 KungfuSkillLvData summonSourceSkill
 0x18 float summonLv                   0x8D26A0 BattleUnit GetSummonSource()
 0x1C int  summonMoveRange             0x8D25F0 KungfuSkillLvData GetSummonSourceSkill()
 0x20 bool summonControlable
 0x28 string summonSourceHero
```
**[RUNTIME]** In the current fight **all 18 units have `isSummon = 0`, `summonControlable = 0`,
`summonSourceHero = null`, `BattleUnit.summonSourceHero = null`** — no summons are deployed, so no
behaviour could be observed directly.

**[DUMP]** summons are ordinary `BattleUnit`s that carry an owner + originating skill. Control is decided
by the same pair of flags used for every unit: `BattleUnit.playerControl` (0x38) plus
`BattleController.BattleUnitControlable()` / `CanPlayerControl()` / `HavePlayerControlUnit()`.
`summonControlable` on the `HeroData` is the per-summon opt-in for that path; `summonMoveRange` overrides
movement range for the summoned actor.

**[GUESS]** `isSummon` is set when a `KungfuSkillData` of summon type spawns the unit; the summon dies /
leaves with its owner (`QuitBattleTeam` 0x8D3340, `LeaveBattleField` 0x8D2D60). Summon skill data is
loaded in `GameDataController.summonSkillDataBase` (0x130).

### 8.2 PartyController — **not a battle system**
```
PartyController : MonoBehaviour  TypeDefIndex 8491   [dump.cs:382078]
  0x18 PartyType partyType    0x1C PartyState partyState
  0x38..0x90 wineIcon/foodIcon/treasureIcon + wineData/foodData/treasureData + *Score
  0xA8 HeroData sourceHero    0xB0 HeroData sourceAssistHero
  0xB8 List<HeroData> targetHero
  0xB9... PartyStart(PartyType, HeroData, AreaData, float baseScore, float scoreRate, HeroData assist) RVA 0xB9A810
          PartyEnd() RVA 0xB9A3F0
```
It is the **social gathering** (wine / food / treasure → favor & score, `WeddingMeetTalkText`).
It never touches `BattleUnit` or `BattleController.teams`.

**Battle "party" concept is:** `BattleController.teams` (`BattleTeam.battleUnits`),
`teamSupportData` (reinforcements), `battleIncludeTeamMate`, `playerSpeControlTeamID`.

---

## 9. Save / Load of `GameSaveData`

`GameDataController : MonoBehaviour` — TypeDefIndex 8376 **[DUMP]** [`dump.cs:377212`](Il2CppDumper/dump.cs)

```
0x30  GameSaveData gameSaveData          [RUNTIME: live, key + HeroList(897) + TempHeroList]
0x20  string saveDataPath
0x28  string backupDataPath
static 0x8  RePlayerPrefData playerPrefData
static 0x0  List<string> saveDataFileName
const string playerprefDataFileName    = "PlayerprefData.dat"
const string ExternalStorageDataFileName = "ExternalStorateData.dat"
0x18  List<Task> Tasks                   ← save/load is async (Task-based)
```

| RVA | Signature |
|---|---|
| `0xC2CBB0` | `void Save(int saveID)` |
| `0xC2C3D0` | `void Load(int saveID)` |
| `0xC2CAB0` / `0xC2A790` | `void SavePlayerprefData()` / `LoadPlayerprefData()` |
| `0xC2C9B0` / `0xC29AF0` | `void SaveExternalStorageData()` / `LoadExternalStorageData()` |
| `0xC1D070` / `0xC1CE40` | `void GameIntoGameData()` / `GameDataIntoGame()` ← model ↔ runtime bridge |
| `0xC2C750` / `0xC2C610` | `void MoveSaveToBackUp(int)` / `MoveBackUpToSafe(int)` |
| `0xC1CA30` / `0xC1F4C0` | `void DeleteSave(int)` / `bool HaveSave(int)` |
| `0xC1D8B0` / `0xC1ECD0` | `SaveInfo GenerateSaveInfo()` / `SaveInfo GetSaveInfo(int)` |
| `0xC1EB70` / `0xC1E1C0` | `string GetSaveDataPath(int,int)` / `GetBackupDataPath(int,int)` |
| `0xC1C7C0` | `void CopyTestSave(string, string)` |
| `0xC2CBB0`+ | `SaveButtonClicked()` / `LoadButtonClicked()` (UI wrappers) |

`GameSaveData` (TypeDefIndex 8149) **[DUMP]**
```
0x10 string key              0x20 WorldData WorldData
0x18 bool worldDataFinished  0x28 List<HeroData> HeroList        [RUNTIME: 897 entries]
0x19 bool heroListFinished   0x30 List<HeroData> TempHeroList
0x1A bool tempHeroListFinished 0x38 float saveTimeCount
0x3C bool saveFailed         0x3D bool loading                   [RUNTIME: loading = true]
```
Format is JSON-ish: `HeroData` is decorated with `[JsonIgnore]` on many fields (runtime-only caches),
so save = serialized `GameSaveData` → file; `loading`/`*Finished` flags are the incremental-IO state
machine. **[DUMP]**

**Co-op implication:** the save is a single-player, single-writer, whole-world snapshot
(`HeroList` 897 + `WorldData`). Two co-op peers cannot share one save file; you need per-peer deltas or
one authoritative host that owns `GameSaveData`.

---

## 10. Notes for a co-op design (derived)

> Prototype that implements item 1–3 as read-only snapshots: `coop-sync/` (`coop-snapshot.js` +
> `host-runner.py`, NDJSON out — see `coop-sync/README.md`).

1. **Authoritative host, not lockstep.** Battle RNG is Unity's global generator with no seed hook → the
   host must roll damage/AI and replicate results.
2. **Sync points that already exist:** `battleState` (0x24), `nowActiveState` (0x124),
   `nowActiveUnit` (0x110), per-unit `moved`/`attacked` (0xB8/0xB9), `BattleInfoData` (0xA8),
   `teamMemJoinBattleNum`, `battleTime` (0x1C8).
3. **Board state** is `GridUnitData.row/column` (0x24/0x28) per `BattleUnit.mapGrid` (0x60) — small,
   cheap to replicate.
4. **Reinforcements** already exist as `teamSupportData` + `CheckSupport()` + `baseSupportEnterTime`;
   reusing that path is the least invasive way to bring a co-op partner's squad into a fight.
5. **Player input gating** already exists (`playerControl`, `BattleUnitControlable()`,
   `CanPlayerControl()`, `playerSpeControlTeamID`) — a second human needs either a second
   `BattleUnit.playerControl = true` or a team-id override.
6. **Time is hostile to sync:** `Time.timeScale` is process-global (TimeScaleController)
   and animations drive the loop. **`battlePaused` ไม่ได้ toggle ตามเทิร์นผู้เล่น** (แก้ 2026-10-20 หลังวัด 82,695 sample: true 0 ครั้ง) — อย่าออกแบบ sync บนฟิลด์นี้ [RUNTIME S1.5-b]
7. **No battle log file**: replicate from `AddInfoText` taps or from `BattleInfoData` deltas.
8. **PartyController is not the battle party** — do not confuse it with team building.

---

## 11. Verified vs guessed — quick index

| Claim | Status |
|---|---|
| BattleController offsets & live values (§0, §1.1) | **RUNTIME** |
| Battle is per-unit turn-based with round/buff-round structure | **RUNTIME** (state + flags) + **DUMP** (enums/methods) |
| `Update → RunBattle` and the loop's callees | **RUNTIME** (code-byte call scan) |
| Damage/recover/auto-fight/AI-tiebreak use `UnityEngine.Random` | **RUNTIME** (code-byte call scan) |
| `GlobalData.RandomRange` wraps `System.Random.NextDouble` via `ThreadLocal` | **RUNTIME** (disasm) |
| `GlobalData.randomSeed` value 25,654,550 | **RUNTIME** |
| Where `randomSeed` is seeded | **GUESS** (`.cctor` not traced) |
| `AIController` is world AI, battle AI is in `BattleController` | **DUMP** (method sets) + **RUNTIME** (46 live plans) |
| Summon field semantics (`isSummon`, `summonControlable`, owner linkage) | **DUMP**, behaviour **GUESS** (no summon present live) |
| `PartyController` is social, not battle | **DUMP** |
| `AddInfoText` writes the on-screen info feed | **DUMP** signature + **RUNTIME** partial disasm; caller list **RUNTIME** (31 sites, §4.1) |
| `CheckBattleEnd → BattleEnd` link | **GUESS** (coroutine via method pointer; not visible in byte scan) |
| Save/Load RVAs, `GameSaveData` layout | **DUMP**, `HeroList` size/`loading` **RUNTIME** |
| TimeScaleController writes `Time.timeScale` | **RUNTIME** (disasm) |
