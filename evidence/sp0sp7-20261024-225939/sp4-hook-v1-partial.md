# §4 hook v1 — ผลบางส่วน (รอบ 3, Free mode, 2026-10-25)

## สิ่งที่ยืนยันได้ (RUNTIME, hook log-only)

- `GameController.StartNewGame()` เข้าก่อน (depth 1) → `GameController.GenerateHero()` เข้าถัดมา
  (depth 2, nested ใน StartNewGame) — self เดียวกัน `0x2bb3c818b40` (GameController instance เดียว)
- ตัวนับ (counts) ทั้งหมดจากรอบสร้างโลก Free หนึ่งรอบ:
  | method | จำนวนครั้ง |
  |---|---|
  | `HeroData..ctor` | 886 |
  | `GameController.StartNewGame` | 1 |
  | `GameController.StartNewGameSetting` | 1 |
  | `GameController.GenerateHero` | 1 |
  | `WorldData.GenerateHeroDict` | 1 |
  | `WorldData.AddTempHero` | 88 (= TempHeros 88) |
  | `GameController.GameStartTeleportPlayer` | 1 |
  | `GameController.GenerateHeroData` ×3 overloads | **0** |
  | `GameController.WorldAddNewHero` | **0** |
  | `WorldData.AddNewHero` | **0** |
  | `GameController.ManageNewHeroJoinForce` | **0** (โหมด Free) |

## ข้อสังเกต (ยังไม่สรุป)

- HeroData 886 ตัวถูก `new` ผ่าน ctor แต่ `Heros`(883)+`TempHeros`(88)=971 — ต่าง 85;
  บาง hero อาจถูกสร้างทางอื่น (clone/กำหนด list ตรง) — UNKNOWN
- การเพิ่ม hero 883 ตัวเข้า `Heros` ไม่ผ่าน `AddNewHero`/`WorldAddNewHero`/`GenerateHeroData` เลย
  ⇒ มี path อื่น (list ถูก assign ทั้งชุดระหว่าง world gen) — ต้อง trace ต่อ (hook v2)
- ctor เริ่มยิง **ก่อน** `StartNewGame` ~1.4 s ที่ depth 1 (caller ไม่ได้ hook) — ช่วงหน้าจอยืนยันสร้างตัวละคร
  (GUESS: เกม gen hero pool ล่วงหน้าระหว่างโหลด)
- รอบ 3 (Free): เงินเริ่มต้นตอน snapshot หลังออก BigMap = 1000 (รอบ 1 ตอน snapshot หลังสร้าง = 0) —
  สาเหตุ UNKNOWN (จังหวะที่เกมแจกเงิน/สิ่งที่ผู้ใช้ทำหลังเข้าโลก) — ไม่ถือเป็นผลของโหมด

## ข้อจำกัดเครื่องมือ (บันทึกตามจริง)

- log v1 cap 600 แถว ถูก `heroCtor` (886) กินจน event ชื่ออื่นหลุดออกหลัง 2 แถวแรก
  ⇒ chain เต็มต้องรอ hook v2 (log เฉพาะ named event, ctor นับอย่างเดียว) จากรอบสร้างถัดไป
- ptr ที่บันทึก: player = `0x2bb370a2a80`, worldData = `0x2ba9c831960` (รอบ 3)

## สถานะรอบ 3 (Free mode ครั้งที่ 2)

- world: gameMode=Free, gameDifficulty=0, Heros=883, TempHeros=88, Forces=30 (ตรงรอบ 1)
- hero: heroID=0, สม/สมศรี, age 18, belongForceID=-1, atAreaID=-1 (BigMap),
  bigMapPos=(2058,799) เหมือนรอบ 1, money=1000
- snapshot: `hero-fields-r3.json`, `world-fields-r3.json`
