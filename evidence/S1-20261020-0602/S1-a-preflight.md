# S1-a pre-flight — หลักฐานตั้งต้น

Run: 2026-10-20 06:02–06:05 · mod `LongYinCoop 0.2.0` stage 1 · pid 22296 (run แรก) / pid ใหม่หลัง rebuild
เกม: `LongYinLiZhiZhuan.exe` · Unity 2020.3.48f1c1 · MelonLoader 0.7.3

## 1. Version pin (ก่อนเริ่ม)

`sha256sum` ตรงกับ `VERSION.md` ทั้งสองไฟล์:

```
33c56def4102844a1190eec963a05c834cfcf992d42b423d485ce8584d7920c1 *GameAssembly.dll
451311d4dc8c1b8b5073905c8b39a9781bd7e1f44ed88fbf7b35f5345c18bfd0 *LongYinLiZhiZhuan_Data/il2cpp_data/Metadata/global-metadata.dat
```

## 2. Save copy (§1.2)

- สำเนา: `coop-test/20261020-055746/save/` (57 ไฟล์)
- hash: `coop-test/20261020-055746/savehash.copy.before.txt` + `savehash.real.before.txt`
- หลังรันถึง TitleScene แล้วปิดเกม: `SaveSlot*` **ไม่เปลี่ยน** (`savehash.real.after-title.txt`, diff ว่าง)
  → `PlayerprefData.dat` เป็น volatile ตาม D10 (ยังไม่ตรวจในรอบนี้)

## 3. Interop type/field ยืนยันตอนรัน [RUNTIME]

อ้าง `probe-types-title-v2.json` (reflection บน type ที่ Il2CppInterop สร้างจริง)

**ข้อค้นพบสำคัญ:** Il2CppInterop 1.5.1 รุ่นนี้ expose ฟิลด์เกมเป็น **property** (getter/setter) ไม่ใช่ field
นอกจาก static `NativeFieldInfoPtr_*` — โค้ด mod จึงต้องอ่านผ่าน property (typed) ตามปกติ

| type | จำนวน property | property ที่ S1 ใช้ | มีจริง |
|---|---|---|---|
| `Il2Cpp.BattleController` | 133 | `infoCount`, `battleType`, `battleState`, `nowActiveUnit`, `nowActiveState`, `battlePaused`, `battleTime`, `teams`, `infoText`, `battleMapData`, `inited` | ครบ |
| `Il2Cpp.BattleUnit` | 54 | `playerControl`, `heroData`, `mapGrid`, `battleTeam`, `autoFight`, `moved`, `attacked` | ครบ |
| `Il2Cpp.BattleTeam` | 6 | `ID`, `battleUnits` | ครบ |
| `Il2Cpp.GridUnitData` | 12 | `row`, `column`, `battleUnit` | ครบ |
| `Il2Cpp.HudController` | 33 | `timeLabel`, `nameLabel`, `fightScoreLabel`, `moneyLabel`, `fameLabel`, `badfameLabel`, `forceLabel`, `contributionLabel`, `heroNumLabel`, `areaNumLabel` | ครบ |

`infoCountProperty = infoCount:Int32`, `initedProperty = inited:Boolean`
(ยืนยันผ่าน `Type.GetProperty` — `GetField` คืน null ตามที่อธิบายข้างบน)

ยัง **ไม่** ยืนยันว่า HP/ตำแหน่งแหล่งจริงคือ `HeroData.hp` / `GridUnitData.row,column` หรือค่าที่ UI แสดง
— ต้องเทียบในศึก (S1-d)

## 4. Hook ที่ติดตั้ง [RUNTIME]

```
battlelog: PatchApplied Il2Cpp.BattleController.AddInfoText(String,Boolean)
battlelog: patched 1/1 overload(s)
```

มี overload เดียวใน dump และ runtime ยืนยัน 1 ตัว

## 5. Patch ของ mod อื่น [RUNTIME]

`patch-report-title.json` — ณ TitleScene:

| method | owners | postfix |
|---|---|---|
| `Il2Cpp.BattleController.AddInfoText` | `longyin.coop.s1.battlelog` | 1 (priority 400) |
| `UnityEngine.UI.Text.set_text` | `LongYinThai` | 1 |

**ไม่มี** mod อื่น patch `AddInfoText` → hook ของเราไม่ชนกันที่ method เดียวกัน
แต่ LongYinThai patch **setter ของ `Text`** ซึ่งเป็นชั้นแสดงผล:
คาดว่าข้อความที่ postfix เก็บได้จะเป็น **ก่อนแปลไทย** — S1-c ต้องพิสูจน์ด้วยการเทียบกับ UI

## 6. ต้นทุนตั้งต้น (TitleScene) [RUNTIME]

`s1-report-title.json`: tick p50 0.0006 ms / p99 0.0173 ms / max 2.56 ms (2,527 ticks), err 0
(ค่านี้ยังไม่ใช่ world map 10 นาที หรือในศึก)
