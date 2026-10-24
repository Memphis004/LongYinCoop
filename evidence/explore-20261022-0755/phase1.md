# evidence/explore-20261022-0755 — phase 1 (read-only, Explorer/dungeon)

รอบ 2026-10-22 · label `explore-20261022-0755` · read-only ผ่าน il2cpp-frida-mcp
(0 memory write, 0 state-changing method, 0 ปุ่มที่ผมกดแทนผู้ใช้)

## session-start

| เวลา | เหตุการณ์ | หลักฐาน |
|---|---|---|
| 07:47 | hash `GameAssembly.dll` = `33c56def…20c1`, `global-metadata.dat` = `451311d4…bfd0` ตรง `VERSION.md` | sha256sum |
| 07:47 | เกมรันอยู่ pid 26972 (เปิด 07:48 log) — **บนเซฟจริง** หลัง restore ของ `S2cd` (07:31) | `tasklist`, `MelonLoader/Logs/26-10-22_7-48-4.log` |
| 07:48 | เทียบ hash: ต่างเฉพาะ `PlayerprefData.dat` (+`steam_autocloud.vdf`), `SaveSlot*` 57 ไฟล์ไม่ถูกเขียน | `save-split rebaseline` output |
| 07:54 | `rebaseline --against …/20261022-064714` → `coop-test/baselines/20261022-075422` (changed=1 volatile) | `REBASELINE.json` |
| 07:55 | `split --label explore-20261022-0755 --expected-baseline …075422` — real kept as `Save.real-explore-20261022-0755`, copy mounted, copy vs real changed=0 | `MANIFEST.json` |
| 07:55 | sampler `tools/s15d_sample.py` pid 2204 (`--interval 0.5 --seconds 5400 --wait 3600`) | `sampler.log`, `sampler.ndjson` |
| 07:55 | `preflight` **READY 5/5** | `history.ndjson` |

deviation: `sampler.ndjson` ถูกสร้างเป็นไฟล์ว่างก่อนเปิดเกม เพราะ `s15d_sample.py` สร้างไฟล์
หลังต่อ pipe สำเร็จ และ preflight ข้อ 4 บังคับให้ไฟล์มีอยู่ — หลังผู้ใช้เปิดเกม sampler ต่อได้จริง
(`samples=162 errors=0`) จึงไม่ใช่การปิดบังผล

## เกมที่วัด (RUNTIME)

- ผู้ใช้เปิดเกม 07:57 (pid 31312) บนสำเนา · เข้า Explorer: **潜入藏经阁** (`exploreName`)
- `ExploreController.inited=true`, `exploreType`=`Maze`(1), `mapType`=-1, `difficulty`=10.0
- `finishFuc`=`FinishStealSkill`, `finishParam`="" , `successCallPlot`=`StealSkillSuccess~5`, `failCallPlot`=""
- `maxPower`=76 = `leftPower`=76 ตอนเข้า · `keyNum`=0 · `showFinal`=false
- เวลาโลกตอนเข้า: 10/9/6 `hour`=2.65

## Q2 ข้อมูลแผนที่/ช่อง

| ข้อ | ค่า | ป้าย |
|---|---|---|
| ชั้น: `ExploreMapData.exploreMapData` (List<ExplorePanelData>) | **1 panel** | RUNTIME |
| ขนาดตารางจริง `mapWidth`×`mapHeight` | **15×15 = 225** tiles | RUNTIME |
| `ExploreController.gridUnits` | `GameObject[,]` **25×25 = 625** ช่อง, ชื่อ `{column}_{row}` (เช่น `0_0`,`1_2`) | RUNTIME |
| flat index ของ gridUnits | `flat = row*25 + column` (ยืนยันจาก tile ผู้เล่น: name `1_2` ↔ tile row=2,col=1 ↔ flat 51) | RUNTIME |
| ground ทั้งแผนที่ | Road 225/225 (ดันเจี้ยนในร่ม ไม่มีภูมิประเทศอื่น) | RUNTIME |
| กำแพง | `wallType` None 120 / Wall 100 / Door 5 (**doorOpen=false ทั้ง 5**) | RUNTIME |
| event ต่อช่อง (`exploreTileEventType`) | Empty 203, Watch 4, Money 3, SpeStoneEvent 3, Event 2, Key 2, State 2, **ev=23 (ไม่รู้จัก) 2**, CollectResource 1, Step 1, LivingSkillExp 1, **ev=-1 1** | RUNTIME |
| ช่อง ev=-1 | (row 14, col 14) และ `seen=true` ตั้งแต่เริ่ม (ช่องเดียวที่เห็นนอกบริเวณจุดเริ่ม) ⇒ **GUESS: ช่องเป้าหมาย/ทางออก** | RUNTIME (ค่า) / GUESS (ความหมาย) |
| obstacles | 3 ช่อง: (5,2) type1 need 115 · (7,8) type0 need 112 · (13,12) type1 need 118 | RUNTIME |
| fog/seen | `ExploreTileData.seen` (bool) ต่อช่อง + `ExploreTileController.Seen` property | RUNTIME / DUMP |
| ช่องที่เดินได้ | `ExploreTileData.moveAble` (bool) — ตอนเริ่มมีช่องเดียว (row0,col1) | RUNTIME |
| เก็บ `eventHappen` | bool ต่อช่อง เปลี่ยนเป็น true หลังTrigger (เห็นที่ (0,2) หลังเหยียบ Watch) | RUNTIME |

## Q5 การเดิน 5 ก้าว (วัด 2026-10-22 ~08:02)

คลิก 6 ครั้ง → เดินจริง 5 ก้าว (`enter3` 5 ครั้ง) · `leftPower` 76 → **71** (1 ก้าว = 1 พลัง)

ลำดับการเรียกต่อ 1 ก้าว (จาก hook, RUNTIME):
`ExploreController.ExploreTileClicked(GameObject)` → `ChangeMoveStep(-1, false)` →
`ManageMoveStepLimit()` → `PlayerEnterGrid(a, b, false)` → [`PlayerCanPassObstacle(tile, false)`] →
`PlayerFinishMove()` → `ManagePlayerAroundGrid(col, row, moveAble)` หลายครั้ง (เฉพาะช่องรอบตัวที่เปลี่ยน)

- **arg order ของ `PlayerEnterGrid`**: dump ประกาศ `(int column, int row, bool quickEnter)` แต่ที่วัดได้
  คลิกช่อง name `0_1` (= tile row1,col0) ส่ง `(1, 0, false)` ⇒ **arg จริงคือ (row, column)** — RUNTIME, ขัดกับชื่อพารามิเตอร์ใน dump
- คลิกช่องที่ไม่ติดกับตัว (name `0_4`, flat 100) → ไม่มี `enter3` (ไม่เดิน)
- **เวลาโลกไม่เดินเลยระหว่างเดิน 5 ก้าว**: `worldTime` = 10/9/6, `hour` = 2.65 ก่อนและหลัง (เหมือนกันทุก hook entry)
- ไม่มีเทิร์นศัตรูต่อก้าวในดันเจี้ยนนี้ (ไม่มีมอนสเตอร์ในแผนที่)

## Q6 ตัวอย่าง interaction แรก (Watch)

- เหยียบช่อง (row0,col2) `ev=2 Watch` → `ManageTileEvent(tile)` 1 ครั้ง → `WatchRandomTile(12)`
- ผล: `seen` 5 → **24** ช่อง; ช่องที่เพิ่มแบบสุ่ม 12 ช่อง = (1,6),(2,13),(3,6),(4,10),(4,13),(5,4),
  (5,5),(5,10),(10,10),(12,3),(13,7),(13,11) — ตรงกับ arg 12 พอดี ⇒ `Watch` = เปิด 12 ช่องสุ่ม
- **ไม่มีป๊อปอัป/PlotPanel** สำหรับ Watch (ผลเกิดทันที) · `eventHappen` ของช่องนั้น = true

## ข้อจำกัดของ phase 1

- ยังไม่ได้เห็น: หีบ (DigTreasure/LockTreasureChest), กับดัก (Injury), ประตู (Door), Event/PlotPanel,
  ช่อง ev=23, ช่องเป้าหมาย, การจบดันเจี้ยน, ศึกในดันเจี้ยน (แผนที่นี้ไม่มีช่อง Fight)
- `nowSeedBase`=`nowSeedSpe`=0 ⇒ ยังไม่รู้ seed ของการสร้างแผนที่ (ค่าที่ใช้จริงอาจถูกใช้แล้วทิ้ง)
