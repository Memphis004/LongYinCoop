# Explorer / ดันเจี้ยน — บันทึกสำรวจ (read-only)

สถานะ: **สำรวจรอบแรกจบ** · 2026-10-23 22:00–23:35 · ไม่มีโค้ด mod / ไม่ build / ไม่ deploy / ไม่แตะ STAGEPLAN
โปรโตคอลเซฟ: ใช้ split ที่ค้างจาก `S2a3-20261022-143608` (Save = สำเนา, Save.real = ของจริง aside) · `save-split verify` PASS · hash เกมตรง `VERSION.md`
หลักฐานดิบรอบนี้: `evidence/explore-20261023-2200/` (`event-probe*.ndjson`, `exit-probe.ndjson`, `before-ev20.json`, `after-ev20.json`, `ev20-measurement.md`, `exit-flow.md`, `success-run.md`)

ป้ายหลักฐาน: **RUNTIME** = อ่านจากโปรเซสที่รันอยู่ · **DUMP** = `Il2CppDumper/dump.cs` · **GUESS** = ยังไม่ยืนยัน · **SPEC** = ข้อเสนอ

---

## 1. ภาพรวมดันเจี้ยน "ขโมยวิทยายุทธ์" (StealForceSkill)

| รายการ | ค่า (RUNTIME) |
|---|---|
| `exploreName` | `潜入藏经阁` (รอบแรก) |
| `exploreType` | `Maze` (1) · `exploreMapType = -1` |
| ขนาด | 15 × 15 = 225 tiles · 1 ชั้น |
| `exploreDifficulty` | 10 |
| `maxPower` | **76** รอบแรก · **55** รอบหลังรีเกม ⇒ ไม่คงที่ต่อ entry |
| `finishFuc` | `FinishStealSkill` |
| `successCallPlot` | `StealSkillSuccess~5` · `failCallPlot` = `""` |
| `gridUnits` | `GameObject[25,25]` (จอง 625, ใช้จริง 225) |

**เข้า:** `PlotController.StartExploreFuc(exploreParamString, eventName, difficulty)` — RUNTIME เห็นตอนเข้าดันเจี้ยนใหม่ (lp ตั้งเป็น 55 ทันทีในฟังก์ชันนี้) · `leftPower` ถูกตั้งใหม่ทุกครั้งที่เข้า (ไม่ต่อจากรอบก่อน)

**ไม่มี explore state ในเซฟ:** `GameSaveData` มีแค่ `WorldData` + `HeroList` + `TempHeroList` (DUMP) — ปิดเกม/โหลดเซฟแล้วสถานะในดันเจี้ยนหายทั้งหมด ต้องเข้าใหม่ (RUNTIME: รีเกม 2 ครั้งแล้วเข้าใหม่ทุกครั้ง)

---

## 2. ชนิดช่อง (`exploreTileEventType`) — ค่าที่เห็นจริง

enum มี 0..22 (RUNTIME อ่าน static enum) แต่แผนที่จริงมีค่าที่**นอก enum** ด้วย:

| ค่า | ชื่อใน enum | ที่พบ | หมายเหตุ |
|---|---|---|---|
| 0 | Empty | ส่วนใหญ่ | |
| 1 | Step | 3 | |
| 4 | Resource | 1 | |
| 5 | Money | 3 | |
| 6 | LivingSkillExp | 1–2 | |
| 7 | State | 1–2 | เดินเหยียบแล้วได้ของ (items +1) ในรอบสำเร็จ |
| 12 | Event | 2–3 | บ่อพิษ/อีเวนต์เรื่องราว — ไม่เสียของ |
| 14 | Teleport | 1 | จุดเกิดศึกในรอบแรก |
| 15 | ExternalInjury | 2 | |
| 16 | Key | 2 | |
| 19 | LockTreasureChest | 1–2 | UI เป็น "ขุดสมบัติ" ไม่ใช่หีบกุญแจ |
| 20 | CollectResource | 2 | UI เป็น **หีบล็อก 3 ตัวเลือก** (เหมือน ev19) |
| 22 | SpeStoneEvent | 1 | |
| **23** | **ไม่อยู่ใน enum** | 1 (รอบแรก) | "เก็บเกี่ยว" (harvest) |
| **-1** | ไม่อยู่ใน enum | 1 (ทางออก) | ช่องจบ/exit |

⇒ **ห้ามเดาชนิดช่องจาก UI** — ev19 กับ ev20 ให้หน้าตา "หีบ" เหมือนกัน ต้องอ่าน `exploreTileEventType` เท่านั้น

---

## 3. การเดิน

- 1 ก้าว = **−1 `leftPower`** บนพื้น `Road` (วัดซ้ำได้ทุกก้าว: 76→72, 55→22)
- นาฬิกาโลก **ไม่เดิน** ระหว่างเดินในดันเจี้ยน (`hour` นิ่งทุกครั้งที่วัด)
- call ต่อก้าว: `ExploreTileClicked(GameObject)` + `(ExploreTileData)` → `PlayerEnterGrid(int,int,bool)` → `ChangeMoveStep` / `ManageMoveStepLimit` → `ManagePlayerAroundGrid` (5–6 ครั้ง) → `PlayerFinishMove()`
- ช่องที่เดินได้ถูกตั้งผ่าน `ManagePlayerAroundGrid(column,row,moveAble)` ⇒ `moveAble=true` = ช่องที่เดินได้**ในก้าวถัดไป** (ไม่ใช่แค่ "เพิ่งเปิด")
- ไม่มีเทิร์นศัตรู/การเดินของ NPC ระหว่างอยู่ในดันเจี้ยน

---

## 4. อีเวนต์ต่อช่อง — ต้นทุนและรางวัล (RUNTIME ครบ before/after)

| อีเวนต์ | ที่ | ตัวเลือก | ต้นทุน | รางวัล | ปลายทางรางวัล |
|---|---|---|---|---|---|
| ev19 "ขุดสมบัติ" | (14,7) | 3 ข้อ (ขุด) | **−7** | `加彩博山炉` (rare 3, value 655) | `HeroData.itemListData` |
| ev19 "ขุดสมบัติ" | (3,1) | 3 ข้อ (ขุด) | **−4** | +1 item | `HeroData.itemListData` |
| ev20 "หีบล็อก" | (3,2) | กุญแจ / **แรง** / ไม่เอา | **−4** (เลือกข้อ 2) | `宰鸡牛刀` (weight 1) | `HeroData.itemListData` |
| ev23 "เก็บเกี่ยว" | (14,6) diff 10 | ไม้ / **เก็บเกี่ยว** / ไม่สนใจ | **−5** (ข้อความบอก 5) | ไม่พบในกระเป๋า/force | UNKNOWN (รอบนั้นไม่ได้อ่าน `resourceStore`) |
| ev23 "เก็บเกี่ยว" | (7,4) diff 4 | ไม้ 40 / **เก็บเกี่ยว** / ไม่สนใจ | **−3** = `GetCollectResourceCost()` | สำนัก **ไม้ซุง +234** · ผลงาน +3 · ทักษะตัดไม้ +1 (จากข้อความ UI) | `ForceData.resourceStore` (float) — ยังไม่ยืนยันตัวเลข |

- **ต้นทุนไม่คงที่ — เป็นฟังก์ชันของ `exploreDifficulty` ของแผนที่นั้น** (ดู §4.1)
- ev19/ev20 ให้ของเข้ากระเป๋าฮีโร่ทันที — money / `forceStorage` / `governStorage` / `resourceStore` **ไม่เปลี่ยน** (diff แบบ itemID multiset)
- ev23 **(7,4) วัดครบแล้ว**: cost `GetCollectResourceCost` = 3 (39→36) · รางวัล **ไม่ใช่ item** (กระเป๋า/forceStorage/govern ไม่เปลี่ยน) แต่ไปที่ **ทรัพยากรของสำนัก** (`ForceData.resourceStore`, `List<float>` 6 ช่อง) + `HeroData.forceContribution` + ทักษะชีวิต — ตัวเลขจากข้อความ UI: ไม้ซุง +234 / ผลงาน +3 / ตัดไม้ +1 (ยังไม่ยืนยันตัวเลขใน memory)
- ลำดับ method ของ ev19: `ManageTileEvent` → `PlotTextShowFinished`/`PlotChoiceShowFinished` → `ChooseDigTreasure(string param)` → `DigTreasureChoosen()`
- ลำดับของ ev20: `ManageTileEvent` → plot 3 ตัวเลือก (ไม่มี method `Choose*` เฉพาะจับได้ — ยังไม่รู้ว่าตัวเลือกเรียกอะไร)

**ช่องทางออก/จบ:** ค่า `-1` ที่มุมแผนที่ = ทางออก (รอบแรก (14,14)); รอบสำเร็จจุดจบคือช่องที่ทำให้เรียก `FinishStealSkill`

### 4.1 สูตรต้นทุน (RUNTIME static disasm + ยืนยันด้วยการเรียกจริง)

`ExploreController` อ่าน `exploreMapData` (offset 112) → `exploreDifficulty` (offset 40) แล้วคำนวณเป็น float ก่อนแปลงเป็น int:

| method | สูตร | ค่าที่เรียกได้จริง (diff 4) | ที่หักจริงจาก `leftPower` |
|---|---|---|---|
| `GetDigTreasureCost()` | `diff × 0.5 + 2` | **4** | 4 (diff 4, ev19 ที่ (3,1)) · 7 (diff 10, ev19 ที่ (14,7)) |
| `GetOpenLockCost()` | `diff × 0.25 + 3` | **4** | 4 (diff 4, ev20 ที่ (3,2)) |
| `GetCollectResourceCost()` | `diff × 0.25 + 2` | **3** | **3** (ev23 ที่ (7,4) diff 4, `ChooseCollectResource` 39→36) ✓ |

**ปิดวงแล้วทั้ง dig และ lock (RUNTIME 23:42–23:52):**
- dig: `DigTreasureChoosen` in `lp=8` → `GetDigTreasureCost()` ret **4** → `ChangeMoveStep` `8 → 4` → out `lp=4` ⇒ หักเท่ากับ ret เป๊ะ (`ChooseDigTreasure` ฟรี) · ของ `饕餮纹鼎`
- lock: `OpenLockChest` in `lp=49` → `GetOpenLockCost()` ret **4** → out `lp=45` ⇒ หักเท่ากับ ret เป๊ะ · ของ `鸟兽散`
- จุดหักพลัง = `ChangeMoveStep` ที่เรียกภายในเมธอดยืนยัน (`DigTreasureChoosen` / `OpenLockChest`)

- ค่าคงที่อ่านจาก memory: `0.5`, `2` (dig) · `0.25`, `3` (lock) · `0.25`, `2` (collect)
- **ข้อควรระวัง:** แถว `cost` ใน probe ที่ muncul ตอนผมเรียกเองด้วย `method().invoke()` ก็ถูก hook จับด้วย — เกมเรียกจริงเฉพาะ `GetDigTreasureCost` ใน `DigTreasureChoosen` (ดู §4.1 วงจร)
- ปลายทางท้ายฟังก์ชันคือ thunk `0x…4ce0`: `cvtss2sd` → call → `cvttsd2si` ⇒ **ปัดเศษเป็น int ก่อนใช้** (กติกาการปัดที่จุด `.5` ยังไม่ยืนยัน)
- **ข้อควรระวัง:** ค่าที่โชว์ในข้อความ plot กับค่าที่หักจริงมาจาก getter เดียวกัน แต่บางอีเวนต์ **ไม่เรียก getter เลย**

**อีเวนต์ขุดผ่าน obstacle (RUNTIME 2026-10-23 23:39):** ที่ `(4,10)` `ev12` + `exploreTileObstacleData` → `ManageTileEvent` หัก **2 พลังงาน** (lp 20→18) โดยเรียก `ChangeMoveStep` **ภายใน** ตัว ManageTileEvent เอง (ground ของช่อง = `Road`, `costStep = 1` ⇒ ไม่ใช่ค่าพื้น) · หลังขุด `obstacle` ถูกเคลียร์เป็น null ⇒ ต้นทุน obstacle เป็นอีกเส้นทางหนึ่ง ไม่ผ่าน `Get*Cost`

---

## 5. ศึกในดันเจี้ยน

- เกิดที่ช่อง `event 14` (`Teleport`, `enemyNum = 1`) → plot choice "来战！" → `PlotController.PlotStartFight(param)`
- `param` = `HardFight-ExploreRandomFightResult--4.642354-1` (**GUESS** การแบ่งฟิลด์ `<BattleType>-<fightEndCall>-<float>-<enemyNum>`)
- `PrepareBattleMap` ถูกเรียก **2 ครั้งซ้อน**: overload **12 พารามิเตอร์** → ข้างในเรียก overload **9 พารามิเตอร์**
- `BattleType = 1 (HardFight)` · `fightEndCall = "ExploreRandomFightResult"` · `includeTeamMate = true` · `randomTrophy = true` · `maxHeroNum = -1`
- ฝั่งเรา 4 คน (force 25) · ศัตรู 1 คน · `winTeamID = 0` · จบด้วย `BattleRealEnd()` → `ExploreRandomFightResult("0")`
- กลับเข้า Explorer **ที่ช่องเดิม** (`pos 14_4`) · `leftPower` **ไม่เปลี่ยน** · ได้ของ 1 ชิ้น (`血府逐瘀散`)
- ช่องที่เกิดศึกถูกตั้ง `eventHappen = true` (ไม่เกิดซ้ำ)

### 5.1 ศึกของดันเจี้ยนขโมยตำรา = `DeathFight` (RUNTIME 2026-10-23)

- มาจาก **`StealSkillSuccessChoose` ตัวเลือกที่ 2** (ตัวเลือกที่ 1 = ได้คัมภีร์ตรง ๆ `飞针要诀` แบบไม่ต้องสู้)
- `battleType = DeathFight` (2) · `fightEndCall = "StealSkillFightResult"` · `timeLimit = 0` · `baseSupportEnterTime = 5`
- ฝั่งเรา 4 คน (หวังเวิ่นเทียน#0 · 慕容星辰#86 · 余采薇#4 · 林云裳#9) · ศัตรู 2 (`耿持凉#764`, `秋娘#408`) + ตัวหนุน 4
- **ชนะ** → `BattleRealEnd()` → `PlotController.FinishExploreMap("true")` → `ExploreController.FinishExploreMap(true)` ⇒ กลับ Area 41 · **ไม่ผ่าน `StealSkillSuccessChoose` อีก** (ข้ามการเลือกคัมภีร์ฟรี)
- `DeathFight` = ศึกที่ตัวละครตายจริง — ผลของการแพ้ต่อตัวฮีโร่ยัง **ไม่ยืนยัน** (GUESS: อาจเสียฮีโร่ ไม่ใช่แค่ hp หมด)

---

## 6. ออกจากดันเจี้ยน — สองเส้นทางต่างกันชัดเจน

### 6.1 กดออกกลางคัน (= ล้มเหลว)
```
QuitExploreButtonClicked()        t=0
ExploreFail()                     t≈2.7 s
FinishExploreMap(false)           t≈3.4 s   (arg0 = 0)
HideExploreMap()                  t≈4.0 s
ResetExploreMap()                 t≈4.4–4.9 s
```
- **ไม่เรียก** `finishFuc` (`FinishStealSkill`), ไม่เรียก `PlotController.*` เลย
- `leftPower` **ไม่ถูกหัก** (42 → 42) · ไม่มี dialog สรุป
- ใช้เวลา ~7.5 s ตั้งแต่กดจนจบ

### 6.2 จบครบ (สำเร็จ)
```
FinishStealSkill()                ← เรียกทันทีที่เหยียบช่องจบ (finishFuc)
ChooseStealSkill()                ← plot 3 ตัวเลือก (避人耳目 [ต้อง 轻功 80] / 速战速决 / 大张旗鼓)
FinishExploreMap(...)             ← ปิด explore
HideExploreMap() → ResetExploreMap()
StealSkillSuccess()               ← ข้อความสรุป (แสดงบนหน้า Area หลัง explore UI ปิดแล้ว)
StealSkillSuccessChoose()         ← plot 2 ตัวเลือก (คัมภีร์ลับ / ตัวเลือกจีน)
StealSkillSuccessChoosen()        ← ของเข้ากระเป๋า (+1: 飞针要诀)
```
- ของเข้า **หลัง** explore UI ปิดแล้ว ⇒ ผู้เล่นเห็นเป็น plot บนหน้า Area

### 6.3 กลับไปหน้าไหน (ทั้งสองเส้นทาง)
`exploreUIPanel`/`exploreObj` = **inactive** · `AreaController.areaUIBelow` = **active** · `bigmapUIPanel`/`playerArmy` = inactive ⇒ **กลับหน้า Area ของสำนักเดิม (area 41 `武当派`)** ไม่ใช่ big map และไม่มีหน้าสรุปผล
`AreaController` ไม่ถูก reload (`inited = true` ตลอด, `areaData` ยังเป็น 41) — เกมแค่ซ่อน/รีเซ็ต explore overlay

**สรุปความต่าง:** `FinishExploreMap(bool success)` — ค่า `false` = กดออกกลางคัน (ผ่าน `ExploreFail`) · ค่าสำเร็จอ่านค่า arg ไม่สะอาด (bool 1 ไบต์ อ่านเป็น int32 เป็น garbage) ⇒ ยังไม่ยืนยันค่าตัวเลขของ success

### 6.4 แพ้ในศึกของดันเจี้ยน — ผู้เล่นรายงาน ยังไม่ได้วัด
- **ผู้เล่นรายงาน (NOT MEASURED):** แพ้ในดันเจี้ยนขโมยตำรา **ไม่ได้อะไรเลย** — แค่ออกจากดันเจี้ยนกลับหน้า Area
- **ผู้เล่นรายงาน (NOT MEASURED):** ค่าอาญากรรมเพิ่ม **ทุกครั้งที่เข้าดันเจี้ยนนี้** (ไม่ว่าชนะหรือแพ้) · ตัวแปรที่น่าจะใช่ = `HeroData.badFame` **[DUMP]** (+ `WorldData.totalBadFame` 0x198) · อ่านค่าได้แล้ว 2026-10-24 **[RUNTIME]**: `HeroData.badFame` = **0** · `WorldData.totalBadFame` = **67.745** แต่ยังไม่รู้ว่าบวกตัวไหน/ตอนไหน/เท่าไร (ยังไม่วัดรอบเข้า–ออกดันเจี้ยน)
- **ยังไม่รู้ path การแพ้:** แพ้แล้วเกมอาจไปทาง `ExploreFail()` → `FinishExploreMap(false)` (เหมือนกดออก §6.1) หรือเรียก `FinishExploreMap(false)` ตรง ๆ หรือมี plot แพ้ของตัวเอง ⇒ **ห้ามสรุปว่าเหมือน §6.1 จนกว่าจะวัด**
- **ไม่ต้องแยกก็ได้สำหรับ co-op:** ทั้งกดออกและแพ้จบที่ Area เดียวกัน ⇒ guest ซิงก์จาก `FinishExploreMap(bool)` (true/false) ได้ทั้งสองกรณี
- **แผนเก็บฟรี:** ถ้าเข้าไปในดันเจี้ยนนี้อีก ให้ติด `tools/frida_exit_probe.py` ทิ้งไว้รอบเดียว — ไม่ต้องจงใจแพ้

---

## 7. สถานะที่ต้อง sync สำหรับ spectate (SPEC/GUESS ตามเดิม)

- `mapId/name` + `difficulty` + `panelIndex` — คงที่ต่อ entry
- ตำแหน่งผู้เล่น (row,col) — เปลี่ยนทุกก้าว
- `seen` bitmap 225 บิต (~29 B) ส่งเฉพาะ delta
- `eventHappen` / `doorOpen` / `finalTile` — เปลี่ยนครั้งคราว
- `leftPower` / `keyNum` / `maxPower`
- modal ที่เปิดอยู่: `plotTextShowing` / `plotChoiceShowing` + ดัชนีตัวเลือก (guest โหลดข้อความเอง)
- ศึก: ต้องใช้ pipeline ของ S3

ขนาดหยาบ: snapshot เต็ม ≈ **< 400 B**, delta ต่อก้าว ≈ **5–40 B**

---

## 8. จุดชนกับแผน co-op

1. **ทุก modal ต้องเป็นสิทธิ์ host**: `ManageTileEvent`, `ChooseDigTreasure`, `DigTreasureChoosen`, `ChooseStealSkill`, `StealSkillSuccessChoose/Choosen`
2. **action ย้อนกลับไม่ได้**: เดิน (หัก stamina ทันที), ขุด/เปิดหีบ (หัก 4–7), จบ/ออกดันเจี้ยน
3. **ออกจากดันเจี้ยน = สำคัญต่อ protocol**: มีสองเส้นทางที่ state ต่างกัน (fail vs success) และมี plot หลังออก — guest ต้องรอ host ส่ง event สองชุดนี้ ไม่ใช่เดาจาก "ปิดหน้า explore"
4. **guest หลุด**: ไม่มี explore state ในเซฟ ⇒ resync จาก snapshot ของ host เท่านั้น
5. **S3 ใช้ต่อได้**: ศึกในดันเจี้ยนคือ `BattleController` เดียวกัน แต่มี `fightEndCall` เฉพาะ (`ExploreRandomFightResult`) ที่ต้องเรียกกลับให้ถูก

---

## 9. บันทึกการวัด

| เวลา | การกระทำ | ผล |
|---|---|---|
| 22:0x | อ่าน state เริ่ม | ตาม §1 |
| 22:2x | เดิน 4 ก้าว | lp 76→72 · clock Δ0 · seen 5→11 |
| 22:2x | เหยียบ (1,1) ev19 | plot text+choice · ข้อความขุด 7 พลังงาน |
| 22:28 | รีเกม | สถานะในดันเจี้ยนหาย · `SaveSlot*` ไม่ถูกเขียน |
| 22:30 | เข้าใหม่ + 5 ก้าว | lp 71 · pos `4_1` |
| 22:38 | 27 ก้าว → (14,6) ev23 | lp 49→**44** · กระเป๋าไม่เปลี่ยน (รางวัล UNKNOWN) |
| 22:44 | (14,7) ev19 ขุด | lp 41→**34 (−7)** · +`加彩博山炉` |
| 22:52 | (14,4) ศึก HardFight ชนะ | 12→9 params · team0 4 / team1 1 · lp คง 31 · +`血府逐瘀散` |
| 23:16 | **กดออก** | `QuitExploreButtonClicked`→`ExploreFail`→`FinishExploreMap(0)`→Hide/Reset · lp 42 คงเดิม · กลับ Area 41 |
| 23:10 | ev20 (3,2) หีบล็อก เลือก "แรง" | lp 47→**42** · +`宰鸡牛刀` (weight 1) · money/force/govern เท่าเดิม |
| 23:20–23:30 | **จบครบ**: 33 ก้าว → จุดจบ → 2 plot | lp 55→22 · `FinishStealSkill`→`ChooseStealSkill`→`FinishExploreMap`→`StealSkillSuccess`→`StealSkillSuccessChoose`→`Choosen` · +`飞针要诀` · กลับ Area 41 |

---

## 10. ยังไม่รู้ / ต้องวัดต่อ

- **ev23**: ตัวเลือก 1 (ต้องใช้ไม้ 40) ให้อะไร และตัวเลข +234 ยังไม่ยืนยันใน memory (ต้อง snapshot `resourceStore` ก่อน/หลังด้วยตัวอ่าน float)
- **ต้นทุนอีเวนต์**: สูตรของ `GetDigTreasureCost()` / `GetOpenLockCost()` (เจอ 7 กับ 4 ในชนิดเดียวกัน)
- **`FinishExploreMap(bool)`**: ค่าตัวเลขของ success (อ่าน bool ผ่าน interceptor ไม่สะอาด)
- **เงื่อนไข 轻功 80**: ตรวจที่ UI หรือใน `ChooseStealSkill`
- **`StealSkillSuccessChoose` ตัวเลือกที่ 2**: เปิดศึก `DeathFight` (§5.1) — ชนะแล้วกลับ Area **พร้อมแผงให้เลือกตำรา 1 เล่ม** (ผู้เล่นรายงาน · ยังไม่ได้ diff รายการของ) · ต่างจากตัวเลือกที่ 1 ที่ได้ `飞针要诀` ตรง ๆ
- **กดออกระหว่างมี plot ค้าง** (ปุ่มออกกดได้ไหม) และออกตอนสู้
- **แผนที่หลายชั้น**: `exploreMapData` มีได้หลาย element แต่ยังไม่เคยเจอ >1 ชั้น
- **แพ้ในศึกดันเจี้ยน**: path การออก (`ExploreFail` / `FinishExploreMap(false)` / plot แพ้) — ยังไม่วัด (ผู้เล่นรายงานว่าไม่ได้รางวัลและกลับ Area เฉย ๆ · ดู §6.4)
- **ค่าอาญากรรม**: อ่านได้แล้ว **[RUNTIME 2026-10-24]** — `HeroData.badFame` (ผู้เล่น) = **0**, `WorldData.totalBadFame` = **67.745** (ดู `evidence/explore-20261024-fieldcheck/`) · ยังไม่รู้ว่าค่าที่ผู้เล่นเห็นบนจอมาจากตัวไหน และบวกตอนเข้า/ตอนจบ/เท่าไร ⇒ ต้อง snapshot ก่อนเข้า–หลังออกดันเจี้ยน 1 รอบ
- ~~ตัวอ่าน snapshot ต้องแก้ก่อนใช้ diff ทรัพยากร~~ → **แก้แล้ว + วัดยืนยัน 2026-10-24** (`evidence/explore-20261024-fieldcheck/`): (ก) `ForceData` **ไม่มี** ฟิลด์ `governStorage` — `governStorage` เป็นของ **`WorldData` @ 0x1D0** **[DUMP+RUNTIME]** · ฝั่ง `ForceData` = `resourceStore` (0x88) / `resourceStoreMax` (0x90) / `forceStorage` (0xA0) / `bookStorage` (0xB8) · (ข) `List<float>` อ่าน `_items.get(i)` ได้ **JS number ตรง ๆ** — **ห้าม**กรองด้วย `x.handle.isNull()` (คืน `[]` เงียบ ๆ = สาเหตุที่ snapshot เก่าอ่าน `resourceStore` ว่าง)
