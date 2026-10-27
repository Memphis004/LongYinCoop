# SP5-Serialized-Ref Census — หา ForceLevelData.heroNum (read-only)

- **label:** `SP5refs-20261026` · **evidence folder:** `evidence/SP5refs-20261026-235218/`
- **ต่อจาก:** `SP5census-20261026-224844` (ตารางข้อมูล) และ `SP5body-20261026` (disasm 471MB — อ้าง
  `gameassembly.disasm.txt` ของรอบนั้นซ้ำ ไม่ได้สร้างใหม่)
- **เครื่องมือ:** UnityPy 1.25.3 + Python stdlib + ไฟล์ disasm เดิม — ไม่ติดตั้ง/ดาวน์โหลดเพิ่ม
- **purity:** hash GameAssembly `33c56def…7920c1` + global-metadata `451311d4…18bfd0` ตรง VERSION.md
  (วัดก่อนเริ่ม) · เกมไม่รัน · ไฟล์ใหม่ = โฟลเดอร์นี้เท่านั้น · ไม่ commit/tag/hook/แก้โค้ด

## 1) ตาราง serialized references ของ GameDataController

**MonoScript** (OBSERVED — UnityPy):
- `GameDataController` = `globalgamemanagers.assets` pathID **438** (Assembly-CSharp.dll)
- `LTCSVLoader` = `globalgamemanagers.assets` pathID **170**

**Instance ที่ serialize ไว้ใน scene** (OBSERVED — parse raw MonoBehaviour layout จาก 12,642 MB ทั้งเกม):

| ไฟล์ | pathID | ขนาด | เนื้อหา |
|---|---|---|---|
| level0 | 63 | 11,264 B | เกือบว่าง: string 2 ชุด (EventData ทัพโจมตี: `据点遭袭` + ข้อความฟอร์แมต), PPtr dummies (fileID 0, pathID 4/2) ~70 คู่, float 0.0/1.0 |
| level1 | 4939 | 11,264 B | เดียวกัน |
| level3 | 22877 | 11,264 B | เดียวกัน |

- **ไม่มี reference ไปยัง TextAsset ใด ๆ บน instance เหล่านี้** — ตารางทั้งหมดถูกโหลดด้วย
  **`Resources.Load("GameData/<ชื่อ>", typeof(TextAsset))` ตอน runtime** (OBSERVED จาก disasm)
- ตัวเลข heroNum/ตารางข้อมูล **ไม่ปรากฏใน serialized blob** (ตรวจ int-stream ทั้ง 2,816 cells ต่อ instance)

**Manifest การโหลดตาราง (OBSERVED — `Resources.Load` 31 sites ใน `LoadAllGameData` @0xC1F9C0,
เรียงตามลำดับใน body):** `GameData/NameData, SpeAddDataBase, ForceSpeAddDataBase, TechDataBase, AreaData,
ResourcePointTypeData, ResourcePointData, HeroTagData, InnData, SkinDataBase, ForceData, BuildingData,
WeaponData, ArmorData, MedData, FoodData, HorseData, HeroNatureTalkText, HeroSpeTalkText, PlotData,
SummonData, SummonKungFuData, KungFuData, SpeHeroData, SpeHeroFaceData, BookTypeIconData,
CheckReplaceSkillIconList, AchievementData, TipsData, LoveableSpeHero, MartialClubData`
— พ่วง: ปิดคำถามค้างจากรอบก่อน **`PlotData.csv` (บทสนทนา) = แหล่งของ `PlotDataBase`** และ
`Start` (0xC2D2D0) → `LoadAllGameData` (0xC1F9C0) ยืนยันทาง chain

**ไม่มี** path รูปแบบ ForceLevel ใน string literal ทั้งไบนารี (19,400 literals)

## 2) TextAsset ของ ForceLevelData + ค่า heroNum: **ยังไม่พบ (UNKNOWN)** — บันทึกสิ่งที่ตรวจครบ

| สิ่งที่ตรวจ | วิธี | ผล |
|---|---|---|
| resources.assets 586 TextAsset: grid ตัวเลข ≤5 col | UnityPy สแกน 2 รอบ (เข้ม/หลวม) | 0 ไฟล์ตรงรูป |
| path literal "ForceLevel*" | stringliteral.json ทั้งหมด | ไม่มี |
| การ alloc `ForceLevelData` ใน `LoadAllGameData` | grep ctor stub 0x180210B70 ใน body 9,689 lines | 0 ครั้ง |
| การเขียน field +0x40 (forceLevelData) | scan `mov [reg+40h],reg` ใกล้ inlined-getter (holder 0x181D74D70) ทั้งไบนารี | จริง 4 + เท็จ 195 (ทุกตัวเป็น field คลาสอื่น/stack) |
| `List<ForceLevelData>::Add` | ScriptMetadataMethod 17,578 รายการ | **ไม่มี slot** (ข้อควรระวัง: catalog ไม่ครบทุก generic instantiation — ไม่ถือเป็นพิสูจน์ว่าไม่มี Add) |
| scene serialization | blob 3 instance | ไม่มีข้อมูลตาราง |
| `GameController.GenerateForceDataBase` (0x7A95E0, 1,303 lines) / `GameDataController..ctor` / `WriteGameDataCsv` / `Awake` / `Start` / `InitWithEtag` / `InitWithLocalFile` | ถอดจาก disasm เต็ม | ไม่พบการสร้าง forceLevelData |

**ข้อสรุปที่อนุญาต:** ยังระบุแหล่งข้อมูล/ค่าไม่ได้ด้วยวิธี static ในรอบนี้ — **ห้ามสรุปว่า hard-code**
เพราะยังไม่ได้ตรวจ (ก) serialized ของ prefab อื่นนอก 4 ไฟล์หลัก (ข) block ใน LoadAllGameData ที่ store
+0x40 ผ่าน register ที่ window 0x300–0x400 ไม่ครอบ (ค) MethodInfo slot ที่ catalog ไม่ครบ
**ทางปิดที่ตรงที่สุด:** log-only hook 1 ครั้ง — อ่าน `GameDataController.Instance.forceLevelData`
หลัง `LoadAllGameData` จบ (dump ค่า heroNum ทุก entry) บนสำเนาเซฟ — หรือถอดต่อด้วย decompiler-grade

## 3) ความสัมพันธ์ heroNum ↔ ลูปสร้างสมาชิกสำนัก (OBSERVED — ถอดเต็มจาก disasm เดิม)

site อ่าน: `GenerateHero` @0x1807ACE26–ACEB5 (ภายในลูปประชากร, ก่อน factory loop sites 0x1807ADAE6):

```
r14 = GameDataController.Instance.forceLevelData        ; [instance+0x40]
      ; ยืนยันชนิด: get_Item ใช้ MethodInfo slot 0x181D8A910
      ;   = "Method$System.Collections.Generic.List<ForceLevelData>.get_Item()" ตรงเป๊ะ
rax = this.worldData                                   ; GameController+0x20
rcx = worldData.Forces                                 ; WorldData+0x48 (List<ForceData>)
F   = Forces[ebx]                                      ; ebx = index สำนัก (30 สำนัก)
n   = (int)((float)F.forceLv × xmm9_rate)              ; ForceData+0x34 (forceLv, เช่น 3/4/5)
      ; กิ่งเทียบ F.forceMaleRate (+0x30) คุมสัดส่วนเพศของสมาชิกที่ปั่น
L   = forceLevelData[n]                                ; get_Item(n)
k   = L.heroNum[edi]                                   ; ForceLevelData+0x10, edi วน 0..5
      ; (ขอบลูป edi: cmp edi,6 / jl @0x1807AD27C)
→ k ป้อน RandomRange/Clamp → GenerateHeroData_C → List.Add(Heros, …)
```

- **บทบาทของ heroNum = สเปกจำนวนสมาชิกต่อระดับสำนัก (สูงสุด 6 ค่าต่อ entry) ที่ลูปสร้างสมาชิกใช้จริง**
  — ป้าย: กลไก **OBSERVED** (native) · ความหมายเชิงเกมของ edi (ระดับย่อย 0..5) = **INFERRED** ·
  ตัวแปร rate (xmm9) ยังไม่ผูกกลับถึงแหล่งกำหนด (อาจผ่าน difficulty — INFERRED)
- ความเสี่ยงที่ยืนยันเพิ่ม: ถ้า `forceLevelData` ว่างตอน `GenerateHero` ⇒ `get_Item` ยิง
  ArgumentOutOfRangeException ทันที (bounds check + throw helper ใน shared get_Item) — เกมไม่ crash
  ตอนสร้างโลก ⇒ **ต้องมีการเติมก่อนหน้าจริง** (writer ยังหาไม่เจอ = UNKNOWN)

## 4) UNKNOWN ที่เหลือ + แนวทางปิด

1. ค่า heroNum ราย entry + จำนวน entry ของ forceLevelData + คนเขียน (build site) — ปิดด้วย hook
   log-only 1 รอบ (อ่าน list หลัง StartNewGame) หรือถอด data-flow ต่อ
2. ตัวแปร rate (xmm9) ในสูตร — ถอดต่อจาก body ที่เก็บไว้ได้
3. ความหมายเชิงเกมของ index edi 0..5 — ต้องเทียบ runtime (จำนวนสมาชิกต่อสำนัก 17–35 ที่วัดเดิม)

## 5) ประเมินความพร้อมเขียน design-only proposal

**พร้อมพอสำหรับ design-only proposal (GO สำหรับการออกแบบ; ยังไม่ GO สำหรับ implementation)** เพราะ:
- จุดแทรกครบทุกชั้นด้วย address จริง: ลูปประชากร (GenerateHero), ศัตรูรายวัน (GenerateRandomEnemy ←
  StartNewGame + ChangeDay), เติมรายเดือน (ManageHeroAutoQuitAndEnter ← ChangeMonth, เป้า
  RecommendHeroNum=900 / เพดาน GetMaxHeroNum(year)), ศึก plot (wrapper B/A ของ PlotController)
- PROTECTED ยืนยันอีกชั้น: SpeHeroData CSV โหลดเข้า SpeHeroDataBase ผ่าน `GameData/SpeHeroData`
  (OBSERVED) + บทบาทพิเศษของลูป#1 (มีการเทียบชื่อ/Dictionary<string,int> ก่อน factory)
- **การออกแบบ clamp ไม่จำเป็นต้องรู้ค่า heroNum ล่วงหน้า** (ออกแบบเป็น "คูณ/จำกัดที่จุดปั่น" ได้) —
  แต่การอนุมัติ implementation ควรมีค่าจริงเป็นหลักฐานประกอบ + ตรวจผลกระทบที่ ManageHeroAutoQuitAndEnter
- เงื่อนไขที่ควรทำก่อน/ระหว่าง implementation: (1) hook log-only 1 รอบเพื่อ dump ค่า heroNum + นับ
  ประชากรจริงต่อลิสต์ (ปิดบัญชี 963/883/81/88 แบบพิสูจน์), (2) ตัดสินนโยบายโหมด co-op กับการเติม
  รายวัน/รายเดือน, (3) negative test โหมดเดิม

## การจัดกลุ่ม NPC ตามกติกาใหม่ (สำหรับ design)

- **PROTECTED:** 170 SpeHeroData (ลูป#1 ของ GenerateHero — มี name-binding) + ตัวเอก (ctor arg=0)
- **GENERIC CANDIDATE:** สมาชิกสำนัก + เร่ร่อนที่ปั่นจาก heroNum/ลูป#2–#3 (≈677+95 INFERRED จากรอบ census)
- **UNKNOWN:** กลุ่ม forceID −2 (21) — ห้ามลดจนยืนยัน · writer/ค่า forceLevelData
- **TEMP/EVENT:** ศัตรูสุ่ม (GenerateRandomEnemy — นโยบายแยกจากประชากรโลก) · NPC ศึก/เหตุการณ์ของ
  PlotController (wrapper B ×419 / A ×5 sites) · temp plot hero (`临时:` specs) — ห้ามปิดครอบคลุมทุกระบบ

## ไฟล์หลักฐานในโฟลเดอร์นี้
(รายงานนี้ไฟล์เดียว — การวิเคราะห์ทั้งหมดรันผ่าน stdin/ไฟล์ disasm เดิม; คำสั่งทุกตัวบันทึกใน
conversation และทำซ้ำได้จากเกมไฟล์เดิม hash ตรง)
