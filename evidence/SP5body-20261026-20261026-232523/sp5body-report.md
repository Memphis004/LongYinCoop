# SP5-Static Method Body Census — ปิด UNKNOWN ก่อนพิจารณาลด NPC (read-only)

- **label:** `SP5body-20261026` · **evidence folder:** `evidence/SP5body-20261026-20261026-232523/`
- **วิธี:** native disassembly ด้วย `dumpbin /DISASM` (Visual Studio 2022 Community ที่ติดตั้งอยู่แล้ว — ไม่ได้
  ติดตั้ง/ดาวน์โหลดอะไรเพิ่ม) บน `GameAssembly.dll` → ไฟล์ disasm เต็ม 471MB ในโฟลเดอร์นี้
  (`gameassembly.disasm.txt`) · แยก body 42 method เป้าหมาย + call edges สองทิศทาง + strings ด้วย
  `analyze_disasm.py` / `analyze2.py` (อยู่ในโฟลเดอร์นี้) · ชื่อ address resolve ด้วย `script.json`
  (ScriptMethod 96,106 รายการ / ScriptString 19,400 / ScriptMetadata 7,530)
- **purity:** hash GameAssembly `33c56def…7920c1` + global-metadata `451311d4…18bfd0` ตรง VERSION.md ·
  เกมไม่รันทั้งรอบ · dump.cs/script.json/il2cpp.h hash บันทึกใน `method-address-table.tsv` header
  (dump.cs `b983820b…` ตรง anchor รอบ SP5prep) · สิ่งที่เขียนใหม่ = โฟลเดอร์นี้เท่านั้น · ไม่แก้โค้ด/เซฟ/config

## 0) บัญชีเครื่องมือ (task 1)

| เครื่องมือ | สถานะ | ใช้ทำอะไร |
|---|---|---|
| dumpbin.exe (VS2022 MSVC 14.44, Hostx64/x64) | **มีอยู่จริง → ใช้งาน** | native disassembly ทั้ง DLL |
| ilspycmd (.NET tool) | มี — ไม่ได้ใช้ | decompile managed เท่านั้น; DummyDll body เป็น stub ห้ามอ้างเป็น logic (ตามกติกา) |
| pefile (Python) | มี — ใช้ | map VA→file offset เพื่ออ่าน float constant ใน .rdata |
| Ghidra / IDA / radare / objdump (PATH, WSL) | **ไม่มี** (WSL มีแค่ docker-desktop) | — |
| dump.cs / script.json / il2cpp.h | **ครบ** ของเวอร์ชันนี้ (hash ตรง VERSION.md ผ่าน dump.cs anchor) | ตาราง address + ชื่อ + string |

ข้อจำกัดวิธี: วิเคราะห์ **direct call เท่านั้น** (call/jmp ที่ dumpbin resolve เป็น address ตรง) — indirect/
virtual call ไม่ถูกนับ ⇒ จำนวน call site ทุกตัวเป็น lower bound · generic sharing ของ IL2CPP ทำให้
`List<T>::Add` ใช้ address เดียวกันทุก T (0x18181F800) ⇒ การระบุว่า Add ลงลิสต์ไหนอ้างบริบทรอบ call site
(ผมอ่านบริบทเอง — ป้าย INFER ตรงจุดที่อาศัยบริบท) · ctor stub ร่วม 0x180210B70 ถูกหลายคลาสใช้
(label collision — แก้ด้วยการอ่านบริบทรอบ call)

## 1) ตาราง method/signature/RVA/file-offset (task 2)

เต็มอยู่ใน `method-address-table.tsv` (87 เป้าหมาย + 35 candidate) — ตัวสำคัญ (OBSERVED จาก dump.cs
+ ยืนยันใน disasm แล้ว):

| Method | RVA | file Offset | VA |
|---|---|---|---|
| GameController::StartNewGame | 0x7E0FC0 | 0x7E03C0 | 0x1807E0FC0 |
| GameController::GenerateHero | 0x7AC8F0 | 0x7ABCF0 | 0x1807AC8F0 |
| GameController::GenerateHeroData_A (int heroID, int belongForceID, float heroForceLv, HeroData heroDataBase, bool isTempHero=True, bool isRandomEnemy=False) | 0x7AC840 | 0x7ABC40 | 0x1807AC840 |
| GameController::GenerateHeroData_B (string heroName, int heroID, int belongForceID, float heroForceLv, bool isRandomEnemy=False) | 0x7AC8A0 | 0x7ABCA0 | 0x1807AC8A0 |
| GameController::GenerateHeroData_C (string heroName, int heroID, int belongForceID, float heroForceLv, HeroData heroDataBase, bool isTempHero, SexLimit sexLimit=0, bool isRandomEnemy=False, bool outSideForce=False) | 0x7AB1F0 | 0x7AA5F0 | 0x1807AB1F0 |
| GameController::GenerateRelationShip | 0x7B4400 | 0x7B3800 | 0x1807B4400 |
| GameController::GenerateRandomEnemy (int maxNum=999) | 0x7B12E0 | 0x7B06E0 | 0x1807B12E0 |
| WorldData::GetRandomEnemyCount | 0x9CEA70 | 0x9CDE70 | 0x1809CEA70 |
| GameDataController::LoadAllGameData | 0xC1F9C0 | 0xC1EDC0 | 0x180C1F9C0 |
| GameController::GetMaxHeroNum | 0x7B9FB0 | 0x7B93B0 | 0x1807B9FB0 |
| GameController::WorldAddNewHero | 0x7E5B50 | 0x7E4F50 | 0x1807E5B50 |
| GameController::ManageHeroAutoQuitAndEnter | 0x7CC020 | 0x7CB420 | 0x1807CC020 |
| StartGameSettingController::BirthSettingPlayerData | 0xC6E3D0 | 0xC6D7D0 | 0x180C6E3D0 |
| GlobalData::.cctor | 0xA8A670 | 0xA89A70 | 0x180A8A670 |
| HeroData::.ctor | 0x8AA1D0 | — | 0x1808AA1D0 |

## 2) Creation call graph (OBSERVED — direct calls ทั้งไบนารี; ดู `calls-xref.tsv`, `xref-summary.tsv`)

```
GameController.Start (เรียก StartNewGame ×2 จุด — เงื่อนไขยังไม่ถอด)
└─ StartNewGame                     [OBSERVED ลำดับจาก body]
   ├─ LoadAreaDataBase → GenerateForceDataBase → GenerateArea
   ├─ GenerateHero                  ← ตัวเดียวที่เติม Heros
   │  ├─ ลูป #1 site 0x1807AD659: GenerateHeroData_C → List.Add(Heros)   [สาย SpeHero: มี
   │  │    Dictionary<string,int>::get_Item + String op_Equality ก่อน call]
   │  ├─ ลูป #2 site 0x1807ADAE6: FindAvailableHeroID → C → List.Add(Heros) [สมาชิกสำนัก]
   │  ├─ ลูป #3 site 0x1807ADD13: GetRandomBigForceID + RandomRareLvByBossLv → C → List.Add(Heros) [เร่ร่อน]
   │  ├─ HeroData..ctor(arg=0: xor edx,edx @0x1807AD2BD) → List.Add(Heros, ตัวเอก) @0x1807AD2DD
   │  │    └─ BirthSettingPlayerData (เติม attr ตัวเอกจากหน้าสร้างตัวละคร)
   │  └─ GenerateHeroDict (สร้าง dict หลังเติมครบ)
   ├─ GenerateRelationShip          ← หลังเติม hero ก่อนยิงศัตรุ
   │    วนสำนัก: GetForce→GetOwnHero→GetLeader→AddStudent (อาจารย์-ลูกศิษย์)
   │    วนพื้นที่: AreaData.GetInsideHero→AddFriend/Hater (guard HaveRelationBetterThanFriend/HaveHater)
   │    ⇒ ไม่มี heroID คงที่ใด ๆ — ผูกจากรายชื่อที่เพิ่งสร้าง [OBSERVED เชิงโครงสร้าง]
   ├─ GenerateRandomEnemy           ← จาก StartNewGame **และ ChangeDay (เติมรายวัน!)**
   │    GetRandomEnemyCount (นับของเดิมใน TempHeros: isRandomEnemy(0x3CD) และ ธง 0x2F0==0)
   │    → เป้าหมายวันนี้ = f(CustomDifficultyData.GetDifficultyRate + GetTimeRandomDifficulty
   │      + Mathf.Min/Max/RoundToInt + RandomRange)   [โครงสร้าง OBSERVED, สูตรเต็ม INFERRED]
   │    ต่อตัว: GetRandomBigForceID → GenerateHeroData_C → SetRandomEnemyBadFame
   │      → WorldData.AddTempHero → BigMapController.CreateBigMapNpc (+กิ่ง HeroJoinTeam หายาก)
   ├─ GenerateRandomEvent
   └─ StartNewGameSetting (เรียก WorldData.GetHero 1 ครั้ง — ตัวเอก)
เส้นอื่นที่ "สร้าง" NPC (ไม่ใช่ตอนเปิดโลก):
  ├─ WorldAddNewHero(forceID, lv, outSideForce): C → WorldData.AddNewHero → HeroData.JoinForce
  │    caller: ManageHeroAutoQuitAndEnter (เติมรายเดือน)
  ├─ GenerateHeroData_B ×419 sites (PlotController$$StartSpeFight 351, PlotStartFight 57, ฯลฯ)
  │    = โรงงาน NPC ศึก/เหตุการณ์ของ Plot — B เป็น wrapper → C
  ├─ GenerateHeroData_A ×5 sites (PlotController GetSpeReward/FindSpeFollower/FindSpeMasterFight)
  ├─ WorldData.AddTempHero ×8 callers: GenerateRandomEnemy(1) + PlotController(6) + RecruitUI(1)
  └─ WorldData.AddNewHero ×3 callers: WorldAddNewHero / ManageNewHeroJoinForce / ManagePlayerRecruitHero
การโหลดเซฟ: StartLoadGame → GenerateHeroDict เท่านั้น (ไม่สร้าง hero ใหม่) [OBSERVED]
```

**Overload mapping (ปิดข้อ INFER เดิม):** A และ B เป็น **thin wrapper ที่ call C ทั้งคู่** (body มี call เดียว
@0x1807AC889 / 0x1807AC8DB) · C คือ factory จริง (ctor/Clone จาก heroDataBase → SetHeroID → สุ่มชื่อ
(GenerateRandomHeroFamilyName/Name) → สุ่ม attr/skill/tag/item → SetForce/ResetLoyal) · hook รอบ SP4
ที่นับ "GenerateHeroData#2 ×963" ระหว่างสร้างโลกจึงต้องเป็น **C** (A/B มี caller เฉพาะ PlotController ซึ่ง
ไม่ยิงตอนสร้างโลก) — ปิดหมายเหตุ (ก) ของ SP5prep

**การเข้าลิสต์ (ตอบปริศนา SP4 "883 ตัวไม่ผ่าน AddNewHero"):** ทั้ง 3 ลูปของ GenerateHero + ตัวเอก ยัด
`List<HeroData>::Add(Heros, …)` เอง (shared Add = 0x18181F800; บริบท call site ทั้ง 4 จุดชี้ WorldData+0x50
= Heros) — AddNewHero ใช้เฉพาะเส้นเติมทีหลัง [OBSERVED call sites / INFERRED การชี้ลิสต์จากบริบท]

## 3) ค่าคงที่และจุดควบคุมจำนวนที่ยืนยันได้ (OBSERVED — ถอดจาก GlobalData::.cctor + bodies)

| ค่า | ที่อยู่/หลักฐาน |
|---|---|
| **MaxHeroNum = 1200** | GlobalData::.cctor `mov dword ptr [rcx+9Ch],4B0h` |
| **RecommendHeroNum = 900** | เดียวกัน `[rcx+A0h]=384h` |
| **MinOutforceSimpleHeroNum = 150** | `[rcx+A4h]=96h` |
| **TempHeroIDStart = 10000** | `[rcx+118h]=2710h` (ID space: โลก 0–9999, temp 10000+) |
| PlayerForceID = 25 (仙霞派) | `[rcx+ECh]=19h` |
| MaxFriendNum=16 · MaxLoverNum=4 · MaxBrotherNum=4 | `[rcx+90h/94h/98h]` |
| HeroBaseHp=50.0 · HeroBaseMp=100.0 (float) | `[rcx+F0h]`, `[rcx+F4h]` (decode จาก .rdata) |
| SteamAppID = 3202030 | `[rcx+30h]` (ตรง Steam จริง — ยืนยัน mapping ถูกตัว) |
| **GetMaxHeroNum(year) = 1200 + RoundToInt(Clamp(year−1.0, 0, 20.0) × 25.0)** | body @0x1807B9FB0 + float จาก .rdata (1.0/20.0/25.0) ⇒ เพดาน 1425 ตอนปี 10, สูงสุด 1700 ตอนปี ≥21 |
| `GetRandomEnemyCount()` = นับใน TempHeros ที่ isRandomEnemy(0x3CD)=1 && ธง 0x2F0==0 | body @0x1809CEA70 (เทียบ byte offset กับ field map ของ HeroData) |
| Spe*Name รายการชื่อ: เจอชื่อพิเศษแบบตัดจุด ใน cctor (เช่น 杨思迟, 姜婉, 斗酒僧, 独孤剑, 独臂刀, 都点检, 逍遥生, 炼气士, 钦冲, 傅砚之, 张弥勒, 陆沉舟 …) | 629 string refs ใน cctor → `strings/` วิเคราะห์แล้ว; การแบ่งว่าชุดไหนเป็น field ไหน (NoRandom/Master/Weak/Teammate/DisableRecruit) ยังแยกไม่ครบ (PARTIAL) |
| forceLevelData: สร้างใน LoadAllGameData จาก **TextAsset CSV ผ่าน LTCSVLoader** (`ReadMultiLine` → เช็ค header ด้วย GetValueAt(0,1) → วนแถว 1..5 `GetValueAt` → int → `heroNum.Add` — เจอ call sites จริง @0x180C20599 บล็อก LTCSVLoader) | body @0x180C1F9C0; **ค่าตัวเลข heroNum ยัง UNKNOWN** (ยังระบุไฟล์ CSV ไม่ได้ — สแกน 586 TextAsset สองรอบไม่เจอ grid ตรงรูป; TextAsset ผูกแบบ serialized ref ไม่มีชื่อในโค้ด) |
| GameController::.cctor (SelfForceEventMinHeroNum ฯลฯ) | ยังไม่ถอด (0 imm-store ตรง pattern) — UNKNOWN |

## 4) คำตอบรายข้อของโจทย์ (task 4)

1. **heroNum:** กลไก OBSERVED (ข้างบน) · **ค่า UNKNOWN** — วิธีปิด: parse serialized refs ของ
   GameDataController (MonoBehaviour ใน scene) ด้วย UnityPy เพื่อจับคู่ field↔TextAsset, หรือ hook
   LoadAllGameData log-only 1 ครั้ง
2. **MaxHeroNum=1200 (+สูตรปี) / RecommendHeroNum=900 / MinOutforceSimpleHeroNum=150 / TempHeroIDStart=10000** —
   OBSERVED ครบ · การใช้จริง: Recommend กับ Max ถูกใช้ใน `ManageHeroAutoQuitAndEnter` (เดียว — 2 sites)
   [OBSERVED จาก xref] · สูตร GetRecommendHeroNum ยังไม่ถอดละเอียด (น่าจะคู่กับ Max — INFERRED)
3. **overload ที่ถูกเรียกต่อเส้นทาง:** เปิดโลก = **C** (ผ่าน 3 ลูปของ GenerateHero) + C ใน GenerateRandomEnemy ·
   plot ศึก = B (×419) / A (×5) · เติมภายหลัง = C ผ่าน WorldAddNewHero · arguments: เปิดโลกยืนยันด้วย RUNTIME
   เดิม (name=null, heroID=1..N, forceID 0..29/−1/−2, forceLv float, ptr heroDataBase) + บทบาท A/B/C จาก
   disasm — **ค่า argument ทุก call site ยังไม่ได้ถอดครบทีละตัว (เหลือ INFERRED)** — ถอดได้เพิ่มจาก body
   ที่เก็บไว้ถ้าต้องการ
4. **GenerateHero เติมอย่างไร / path ที่สอง:** ตอบใน §2 — Heros จาก 3 ลูป + ตัวเอก, TempHeros เฉพาะ
   AddTempHero · **ไม่พบ path สร้างโลกที่สองใน direct calls** (StartLoadGame แค่ rebuild dict) ⇒ กรณี
   SP4 รอบ 3 "factory=0 แต่ Heros≈883" **หาเหตุไม่ได้จาก static** — สมส่อน hook artifact (INFERRED) ต้อง
   re-run พิสูจน์
5. **GenerateRandomEnemy ↔ randomEnemyNum:** OBSERVED เชิงโครงสร้าง — อ่าน current count จาก
   GetRandomEnemyCount แล้วคูณ/จำกัดด้วย GetDifficultyRate (ค่า slider customDifficulty รวม
   randomEnemyNum=6) + GetTimeRandomDifficulty แล้วปั่นเท่าที่ขาด รายวัน (ChangeDay) · สูตรเต็ม INFERRED
6. **GenerateRelationShip:** ทำงาน**หลัง**เติมรายชื่อ (พ่อพันธุ์แม่พันธุ์โลก) และ**ก่อน** GenerateRandomEnemy —
   อ้าง ID แบบ dynamic จาก ownHeros/GetInsideHero เท่านั้น ไม่มี ID คงที่ ⇒ ถ้าสระเล็กลงมันผูกน้อยลง
   ไม่ dangle (แต่ต้องวัดผลเชิงคุณภาพ runtime)

## 5) ข้อจำกัดการปิดบัญชี 963/883/88 (ตามกติกาโจทย์ — ห้ามบวกเป็นประชากรจริง)

- ตัวเลข 963/883/81 มาจาก hook RUNTIME รอบเดียว (รอบ 5) ที่ hook ครอบ 3 overload พร้อมกันด้วยชื่อ "#2"
  ซึ่งพิสูจน์ per-overload ไม่ได้ — ตอนนี้ static ชี้ว่าต้องเป็น C (A/B ไม่ถูกเรียกตอนเปิดโลก) ⇒ **ถ้าเป็น
  C จริง บัญชีปิดสวย: 963 = 882 (เข้า Heros ผ่าน 3 ลูป) + 81 (TempHeros ผ่าน AddTempHero) และ Heros 883 =
  882 + ตัวเอก 1** — เป็น **INFERRED-closure** ที่สอดคล้องทุกตัวเลข แต่ยังไม่ใช่การพิสูจน์การนับซ้อน
  (ต้อง hook รอบใหม่ที่ log ลายเซ็นครบทุก arg + นับ List.Add ต่อลิสต์ จึงจะถือว่าปิด)
- การนับซ้อนที่ยังเป็นไปได้: ctor/Clone ภายใน C นับ HeroData เพิ่มจาก 963 (886 ctors ในรอบเดิม) —
  อย่าใช้จำนวน ctor เป็นประชากร

## 6) UNKNOWN ที่เหลือ + เหตุผล + วิธีปิด

| UNKNOWN | เหตุผล | วิธีปิด |
|---|---|---|
| ค่า heroNum ราย level + ชื่อไฟล์ CSV | TextAsset ผูกแบบ serialized ref; สแกน 586 ไฟล์ไม่เจอ grid ตรงรูป (อาจมี header หลายคอลัมน์/เข้ารหัส) | parse MonoBehaviour GameDataController ใน scene (UnityPy + TypeTree) หรือ hook LoadAllGameData |
| สูตรเต็ม GenerateRandomEnemy (SSE arithmetic) | ตัดสินใจไม่ถอดต่อ — ไม่บล็อกการออกแบบ clamp | ถอดต่อจาก body ที่เก็บไว้ / hook |
| SelfForceEventMinHeroNum + GameController.cctor | pattern init ต่างรูป | ถอดต่อได้จาก disasm |
| ธง 0x2F0 ของ HeroData (เงื่อนไขที่ 2 ของ GetRandomEnemyCount) | ไม่อยู่ใน field map ที่เคย dump | เทียบ offset กับ il2cpp.h / runtime |
| การแบ่ง Spe*Name ราย field | shared List<string>::Add ทำ segment ไม่ตัด | ถอด rcx tracking / hook |
| กรณี SP4 รอบ 3 (factory=0 แต่ Heros≈883) | static ไม่พบ path ที่สอง — สมส่อน hook artifact | re-run hook เต็มลายเซ็น 1 รอบ |
| เงื่อนไข ×2 ของ StartNewGame call site ใน Start | ยังไม่ถอดกิ่ง | ถอดต่อจาก body |

## 7) ความพร้อมพิจารณา implementation (task deliverable 5)

**กลไก: พร้อมพอสำหรับการออกแบบ (design-ready) แต่ยังไม่พร้อมอนุมัติ implement** — จุดแทรก clamp ทั้งหมด
ถูกระบุด้วย address จริงและค่าคงที่หลักถูกถอดครบ:

- เปิดโลก: 3 ลูปใน `GenerateHero` (0x1807AD659/0x1807ADAE6/0x1807ADD13) — จำนวนมาจาก heroNum CSV
  (+SpeHeroDataBase) ⇒ clamp ที่นี่ = ลดสมาชิกสำนัก/เร่ร่อน
- ศัตรุสุ่ม: `GenerateRandomEnemy` (0x1807B12E0) — clamp ที่ call site หรือที่ GetDifficultyRate
- เติมรายเดือน: `ManageHeroAutoQuitAndEnter` (0x1807CC020) ใช้ RecommendHeroNum=900/GetMaxHeroNum(year)
  ⇒ **ถ้าไม่ gate ตรงนี้ โลกจะเติมกลับจนถึง 900 ทุกเดือนเกม** — จุดที่คนลืมง่ายที่สุด
- ID space: TempHeroIDStart=10000 แยกสระ temp ออกจาก hero โลกแล้ว

**เงื่อนไขที่ยังขาดก่อนอนุมัติ implementation:**
1. ค่า heroNum ราย level (เป้า 200 ต้องรู้ว่าจะ clamp จากฐานเท่าไร และเหลือ special 170 + ใครบ้าง)
2. runtime re-measure 1 creation round บนสำเนาเซฟ (hook ลายเซ็นครบ + นับ List.Add ต่อลิสต์) เพื่อ
   ปิดบัญชี 963/883/81/88 แบบพิสูจน์ (ตามกติกาห้ามใช้ factory calls อย่างเดียว)
3. ทดสอบผลของสระเล็กลงต่อ ManageHeroAutoQuitAndEnter (โลกจะพยายามเติมกลับ 900 — ต้องตัดสินใจว่า
   โหมด co-op จะห้ามเติม หรือปล่อยตามสูตร) และ GenerateRelationShip/Mission/Plot ที่เลือกจากสระ
4. negative test โหมดเดิมไม่เปลี่ยน

## ไฟล์หลักฐานในโฟลเดอร์นี้
`gameassembly.disasm.txt` (471MB — disasm เต็ม) · `method-address-table.tsv` · `target-index.tsv` ·
`target-bodies.disasm.txt` (42 bodies) · `calls-xref.tsv` (6,350 edges) · `xref-summary.tsv` ·
`call-sequence.tsv` · `constants-table.tsv` · `strings-in-bodies.tsv` · `heroNum-candidates.txt` ·
`analyze_disasm.py` / `analyze2.py` · `dumpbin.stderr.txt` (ว่าง = ไม่มี error)
