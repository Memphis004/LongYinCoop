# §4 — call chain การสร้างโลก/ตัวเอก (hook v3, รอบ 5, Free mode, 2026-10-25)

## Chain ที่วัดได้ (RUNTIME, log-only hooks, timestamps เป็น ms จาก Date.now())

```
GameController.StartNewGame()                      d1  t+0      self=0x27bacc14a80 (GameController เดียว)
└─ GameController.GenerateHero()                   d2  t+77
   ├─ GameController.GenerateHeroData#2(...)       d3  ×963 enters (args: name=null,
   │    heroID=1..N, belongForceID(0..29 / -1 / -2), heroForceLv float, ptr...)
   │    ⇒ NPC factory — สร้าง hero รายตัว ใน ~1.1 s
   └─ WorldData.GenerateHeroDict()                 d3  t+1023  self=worldData 0x27bb5953c80
        (สร้าง dict heroID→HeroData; ret=0x27bb6874120)
   ← GenerateHero return = 0x1                         t+1024
WorldData.AddTempHero(HeroData)                    ×81 enters  t+1025..1201  (นอก GenerateHero)
GameController.StartNewGameSetting#0               count=1 (log-tail หลุด cap — ลำดับสัมพัทธ์ UNKNOWN)
GameController.GameStartTeleportPlayer#0           count=1 (log-tail หลุด cap)
GameController.PlayerEnterArea#0                   count=1 (log-tail หลุด cap)
```

- counts ทั้งหมด: StartNewGame=1 · GenerateHero=1 · GenerateHeroData#2=963 · GenerateHeroDict=1 ·
  AddTempHero=81 (= TempHeros list) · StartNewGameSetting=1 · GameStartTeleportPlayer=1 · PlayerEnterArea=1
- การกระจาย belongForceID ของ NPC ที่ผ่าน factory: สำนัก 0–29 (≈17–35 คน/สำนัก; 仙霞派=0x19 มี 17) ·
  -1 (ไม่มีสำนัก) ×186 · -2 ×21

## ตัวเอก (heroID 0)

- **ไม่พบ**การสร้างตัวเอกใน chain ที่ hook ได้: ไม่มี call ที่ heroID=0, playerPtr (`0x27bb9dcba80`)
  ไม่ปรากฏเป็น arg หรือ ret ใน log 2,000 แถว ⇒ ตัวเอกถูกสร้าง**นอก** path ที่ hook
  (GUESS: สร้างไว้ตั้งแต่หน้าจอสร้างตัวละคร ก่อน StartNewGame — สอดคล้อง ctor-burst ก่อน StartNewGame
  1.4 s ในรอบ 3; หรือภายใน StartNewGameSetting ที่ log หลุด cap) — UNKNOWN ต้องรายงานตรง ๆ
- รอบ 3 (v1): `GenerateHeroData` = 0 ครั้ง แต่รอบ 5 = 963 ครั้ง — การ gen NPC มีมากกว่าหนึ่ง path
  (รอบ 3 ต้องใช้ pool/cache จาก process เดิม; ทั้งสองรอบผลลัพธ์ปลายทางเหมือนกัน: Heros≈883) —
  รายงานเป็นข้อสังเกต ไม่สรุปกลไก

## ข้อจำกัดเครื่องมือ (บันทึกตามจริง)

- log cap 2,000 แถว: GenerateHeroData#2 (1,880 ev) กินพื้นที่ ⇒ tail (AddTempHero หลัง 58/81,
  StartNewGameSetting, GameStartTeleportPlayer, PlayerEnterArea) หลุดจาก log แต่ counter พิสูจน์ว่าถูกเรียก
- AddTempHero แกว่งตามโลก: 88 (รอบ 1) / 88 (รอบ 3) / 81 (รอบ 5) = การสุ่มของโลก (INFER)

## field ขั้นต่ำที่สังเกตได้ของตัวเอกที่ใช้งานได้ (RUNTIME จาก snapshot รอบ 1/3/5)

`heroID=0` · `heroName`/`heroFamilyName` (string) · `isFemale` · `age=18` · `belongForceID`
(-1 = ไม่มีสำนัก) · `atAreaID` (-1 = BigMap) · `bigMapPos{x,y}` · `hp/maxhp` · `power/maxPower` ·
`mana/maxMana` · `itemListData{money, allItem, weight, maxWeight}` · `heroAIData` · `faceData` ·
`generation` · worldTime อยู่ฝั่ง WorldData (ปี/เดือน/วัน) — field อื่นของ HeroData มีค่า default
จาก ctor ทั้งหมด (187 fields, ดู hero-fields*.json)

## dependency ที่วัดได้

- `WorldData.Player()` = instance method (pure getter) บน worldData ที่อ้างจาก `GameController.worldData`
- การสร้างโลกทั้ง chain ทำงานภายใน GameController/WorldData — **ไม่มีความผูกกับ PlotController/UI ใน chain
  ที่ hook ได้** (PlotController.JoinForceParty ไม่ยิงระหว่างสร้าง; จะยิงหรือไม่ตอนเข้าสำนัก = งาน §5)
- playerPtr รอบ 5: `0x27bb9dcba80` · worldPtr: `0x27bb5953c80` · GameController: `0x27bacc14a80`
