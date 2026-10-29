# SP0/SP7 Investigation — บันทึกผล (2026-10-24/25 · label `sp0sp7-20261024-225939`)

> รอบสำรวจก่อน SP0/SP7 เต็ม ตามโปรโตคอลที่ผู้ใช้อนุมัติ — อ่านอย่างเดียว + observational hooks
> เกม: hash ตรง VERSION.md · Unity 2020.3.48f1c1 · V1.1.2f0 · เซฟจริงไม่ถูกเขียนตลอดรอบ
> (baseline รอบ: `coop-test/baselines/20261024-232024` digest `7332fc93eb98…` — มติ (ก)A+(ข)1)

## 1. Singleton census (DUMP + RUNTIME)

รายละเอียดเต็ม: `evidence/sp0sp7-20261024-225939/sp6-census.md`

| Controller / UI | Singleton ต่อโปรเซส | ผูกกับ Player() | Data-only path | หลักฐาน | ความมั่นใจ |
|---|---|---|---|---|---|
| GameController | ✅ heap=1 | เก็บ worldData (Player() อยู่ในนี้) | สร้างโลก/hero = data-level | DUMP 241m (decl) + RUNTIME | สูง |
| BattleController | ✅ heap=1 · get_Instance **hot** (38k+ calls) | ฉากศึกที่มีผู้เล่น | ศึก NPC-vs-NPC **ไม่** ผ่าน (§7) | DUMP 166m (decl) + counter | สูง |
| PlotController | ✅ heap=1 | **god-class 1526 methods (decl)** | ยังไม่พิสูจน์ | DUMP + RUNTIME | ปานกลาง |
| ChooseController | ✅ heap=1 | 15 methods | ยังไม่พิสูจน์ | DUMP + RUNTIME | ปานกลาง |
| AuctionController | ✅ heap=1 | 28 methods | ยังไม่พิสูจน์ | DUMP + RUNTIME | ปานกลาง |
| CraftUIController | ✅ heap=1 | 39 methods (ผูก UI) | ยังไม่พิสูจน์ | DUMP + RUNTIME | ปานกลาง |
| CraftPoisonUIController | ✅ heap=1 | 21 methods (ผูก UI) | ยังไม่พิสูจน์ | DUMP + RUNTIME | ปานกลาง |
| WorldPlotEventController / BattleAiMenuController / ForceGroupFightMatchChooseController | ✅ heap=1 ทุกตัว | 9/8/11 methods | - | DUMP + RUNTIME | สูง |
| BattleUnit + 7 controller รอง | ❌ ไม่ใช่ singleton | - | - | DUMP | สูง |

**หมายเหตุการนับ method (แก้ 2026-10-25 ตามมติผู้ใช้):** ตัวเลขหลัก = จำนวน **method declarations** (รวม `.ctor`/accessor/explicit-interface · nested นับแยก · ไม่นับ GenericInstMethod block) จาก `tools/sp0_dump_parser.py` · ตัวเลขเดิม 244/186/1530 (วงเล็บ 16/33/41/9/56) = นับรวมบรรทัด attribute ที่มี `// RVA:` — GameController 241+3=244 ✓ · BattleController 166+20=186 ✓ · **PlotController 1526+5=1531 ≠ 1530 ต่าง 1 ยังอธิบายไม่ได้** (วิธีนับเดิมไม่ถูกบันทึก) — เก็บทั้งสองชุดเป็นประวัติ · เต็มที่ `evidence/SP0-20261025-0211/singleton-census.md` §1.4

**ผลต่อ flow พร้อมกันในโปรเซสเดียว**: controller สำคัญทุกตัวเป็น singleton 1 instance ⇒ Battle/Plot
แชร์ state กลางตัวเดียวกัน ทำขนานกันไม่ได้ตามโครงสร้างปัจจุบัน (SP0 กติกา: ห้ามเลือกวิธีแก้ล่วงหน้า)
**อาจแยกจาก UI ได้**: `HeroData.JoinForce` (เข้าสำนัก), `GenerateHeroData` (สร้าง hero) — data-level
**ต้องศึกษาต่อ**: PlotController (ขนาด), จุด save ของเกม, Choose/Craft/Auction runtime trace

## 2. Player spawn / HeroData field map (RUNTIME)

- chain สร้างโลก: `GameController.StartNewGame` → `GenerateHero` → `GenerateHeroData#2` ×963
  (args: name=null, heroID 1..N, forceID 0..29/-1/-2, forceLv float) → `WorldData.GenerateHeroDict`
  → `AddTempHero` ×81 (แกว่ง 81–88 ตามโลก) · รวม ~1.2 s (sp4-creation-chain.md)
- **ตัวเอก = heroID 0** — ไม่ได้ถูกสร้างใน chain ที่ hook (UNKNOWN; GUESS: หน้าจอสร้างตัวละครก่อน
  StartNewGame — สอดคล้อง ctor-burst ก่อน StartNewGame 1.4 s)
- **`WorldData.playerHero` ไม่มีอยู่จริง (0 references ใน dump)** — ข้อค้างของ S6 §9 ข้อ 1 ปิด:
  ตัวเอกระบุด้วย `Player()` getter + heroID 0 ใน dict เดียวกับ NPC ⇒ ไม่มี field ผูกตัวเอกถาวร
- field ขั้นต่ำของ hero ใช้งานได้: heroID, heroName/heroFamilyName, isFemale, age(18),
  belongForceID(-1=ไม่มีสำนัก), atAreaID(-1=BigMap), bigMapPos{x,y}, hp/maxhp, power, mana,
  itemListData{money, allItem, weight, maxWeight}, heroAIData, faceData, generation —
  (187 fields เต็ม: hero-fields*.json) · HeroData ctor 886 ครั้ง/โลก vs Heros(883)+Temp(88) — ต่าง 85 (UNKNOWN)
- โลก default: Heros 883 + TempHeros 88 + Forces 30 · `gameMode` "Free"/"Plot" · gameDifficulty=0

## 3. Sandbox ไม่มีสำนัก + sect-join call chain (RUNTIME)

คู่ทดสอบ Free vs Plot (origin ทหาร คงที่ทั้งคู่):

| | Free (โหมดอิสระ) | Plot (โหมดเน้นเนื้อเรื่อง) |
|---|---|---|
| `WorldData.gameMode` | `"Free"` | `"Plot"` |
| เริ่มในสำนัก | ไม่ (belongForceID=-1) | **ใช่ — 仙霞派(25) ศิษย์นอก + Teacher=1 + เงิน 200** |
| `openLeaveForce` / `openForceBuilding` | true / true | **false / false** |
| `worldPlotEventStartData` | 0 | 5 |

- เข้าสำนักผ่าน UI จริง 1 ครั้ง: `belongForceID -1 → 3` (丐帮), `atAreaID -1 → 33`,
  `forceJobType` คง -1, เวลาโลกวัน 1→2
- **call chain**: UI → `HeroData.JoinForce(forceID, …)` (1 ครั้ง — data-level บน HeroData) —
  `PlotController.JoinForceParty`=0, `JoinForceServant`=0, `ManageNewHeroJoinForce`=0
  ⇒ **เข้าสำนักไม่ผ่าน PlotController** (ข้อจำกัด: chain เหนือ JoinForce ยังจับไม่ได้ — log cap)
- ห้ามตั้ง `belongForceID` ตรง ๆ — ให้ผ่าน `JoinForce` เท่านั้น (validation ของเกม)

## 4. NPC vs NPC battle (RUNTIME, วันเกม 1→25+, hook ครอบทุก overload)

- marker ดาบไขว้บนแผนที่ 2 ครั้ง (ผู้เล่นเจอจริง) — `PrepareBattleMap` ×10, `StartBattle`, `RunBattle`,
  `BattleEnd` ฯลฯ = **0 ครั้ง** · `totalFightCount` = 0 ⇒ **ศึก NPC-vs-NPC resolve ทาง world-sim
  (data path) ไม่ผ่าน BattleController ฉาก** (sp7-npc-battles-partial.md)
- การเข้าร่วมศึกของผู้เล่นถูก gate ด้วยชื่อเสียง (RUNTIME: dialog ต้องการ 234; ผู้เล่นมี 60)
- `BattleController.get_Instance` = hot getter (37,997→+18,436/15 นาที) — ห้าม hook แบบ log

## 5. ความเสี่ยงและคำถามเปิด (6 ข้อ)

1. จุดสร้างตัวเอก (heroID 0) ยังไม่ถูกจับ — SP7 เต็มต้อง hook ก่อนกดสร้างใน session ใหม่
2. method resolve ความขัดแย้ง NPC ฝั่ง data path ยังไม่ระบุ (งาน hook เพิ่มในกลุ่ม RandomEvent)
3. H8 ฝั่ง "เนื้อเรื่องหลักเดี่ยว/reentrant" ยังไม่ได้วัด — ต้อง trace บทเรื่องจริง
4. จุดเรียก save ของเกมยังไม่พบ (`GameController.Save` ไม่มีจริง) — สำคัญกับ mod
5. กลไก pool/cache hero gen (0 vs 963 calls ระหว่างรอบใน process เดียว) ยังไม่เข้าใจ — กระทบ SP5/H7
6. Choose/Craft/Auction/Plot ยังไม่ผ่าน runtime trace ตาม flow จริง (มีแค่ heap=1)

## 6. ร่าง diff ข้อเสนอ H8 / SP0 / SP7 (ร่างเท่านั้น — ยังไม่แก้ STAGEPLAN)

**H8** (ปัจจุบัน GUESS · หลักฐาน: census + sect-join):
```
- | H8 | plot/บทสนทนา เป็น flow เดี่ยวผูกกับตัวเอก … | GUESS | SP0 + SP7 | … |
+ | H8a | PlotController เป็น singleton ต่อโปรเซส (state เดียว) | RUNTIME | สำรวจ 2026-10-24 | heap=1 + god-class 1526m (decl) |
+ | H8b | เนื้อเรื่องหลัก non-reentrant ผูกตัวเอก | GUESS (ยังไม่วัด) | SP0 เต็ม + trace บทเรื่อง | — |
+ | H8c | การเข้าสำนักผูกกับ plot เดี่ยว | **ถูกหักล้าง (RUNTIME)** — JoinForce เป็น data-level บน HeroData, JoinForceParty=0 | สำรวจ 2026-10-25 | sp5-sect-join.md |
```

**SP0** (census): บันทึกผลรอบแรกเข้า deliverable รูปแบบเต็ม — เพิ่มข้อเท็จจริง:
`playerHero` ไม่มีจริง (ปิดข้อค้าง S6 §9 ข้อ 1) · `get_Instance` ของ BattleController เป็น hot getter
(ข้อจำกัดเครื่องมือ: ห้าม log ตรง) · ยังต้องทำ: ชั้น A/B/C ครบ + จำนวนจุดอ้างอิง (reference count) รายตัว

**SP7** (spawn+sect join): §15.4 ข้อ 1 ยืนยันฝั่ง Free เท่านั้น — Plot mode ขัดข้อ 1 (fix 仙霞派 +
`openLeaveForce=false`) ⇒ เสนอเพิ่มข้อกำหนด: โลกของ server ต้องเป็นโหมดอิสระ (หรือมีข้อยกเว้นเป็นลายลักษณ์)
· ผู้สมัคร `world.join_sect` (งบ v1.5): validator อ้าง `HeroData.JoinForce` (data-level, ผ่านวัดจริง 1 ครั้ง)
· ค้าง: จุดสร้าง hero ผ่าน method เกม (ข้อ 1 ของ SP7 — ต้อง hook รอบใหม่)

## 7. Save integrity / rollback (เงื่อนไข 4–5)

- ก่อนเริ่ม: split `sp0sp7-20261024-225939` (baseline ใหม่ `20261024-232024` = `7332fc93eb98…`,
  มติ ก-A) · preflight-launch READY 11/11 · aside ยืนยัน 57 ไฟล์/55+55 hashes
- ระหว่างทดสอบ: drift = `SaveSlot1/{Hero,Info,Save,TempHero}` + volatile เท่านั้น (PASS ทุกจุดตรวจ)
- restore 2026-10-25 01:13: **post-check ผ่าน** — `Save` 57 ไฟล์ `7332fc93eb98` EXACT,
  `leftovers: none` · สำเนาทดสอบเก็บ `coop-test/sp0sp7-20261024-225939/Save.used-20261025-011336`
- baseline ประวัติ `4226730f66ff` (S2a-3) ไม่ถูกแตะ

## 8. GO/NO-GO

**GO** — ข้อมูลครบตามเป้าหมายของรอบสำรวจนี้: census (§1) คลุม Battle/Plot/Choose/Craft/Auction,
spawn/field map (§2), sandbox+join chain (§3), NPC-vs-NPC (§4) · เซฟคืนต้นฉบับเป๊ะทุกข้อ
· ไม่มี stage ใด NO-GO เพราะ "ข้อมูลไม่พอ" ข้อเดียวที่ต้องเก็บต่อ = จุดสร้างตัวเอก (SP7 ข้อ 1)

**ขั้นถัดไปที่เสนอ (รออนุมัติ — ห้ามเริ่มเอง)**
1. มติผู้ใช้เรื่องร่าง diff H8a/H8b/H8c + การนำผล census ไปใส่ SP0 เต็ม
2. SP0 เต็ม: ชั้น A/B/C + นับจุดอ้างอิง (อ่าน dump อย่างเดียว ไม่ต้องเปิดเกม)
3. SP7 ข้อ 1: hook จุดสร้างตัวเอกใน session ใหม่ (หนึ่ง creation round)
4. ค้างเดิมที่ไม่เกี่ยวกับรอบนี้: tag `stage-S2a-3`, `S2-plan.md §13` rev2, VPN/guest/60 ยูนิต, hardening `set_pref`

## หลักฐานทั้งหมด: `evidence/sp0sp7-20261024-225939/`
session-start.md · slot-inventory.txt · real-aside-verification.txt · gate-completed.md ·
hero-fields{,-r2,-r3}.json · world-fields{,-r2,-r3}.json · dump-WorldData.cs · dump-HeroData.cs ·
sp4-hook-v1-partial.md · sp4-creation-chain.md · sp5-sect-join.md · sp6-census.md ·
sp7-npc-battles-partial.md · incident-game-process-death-0023.md
