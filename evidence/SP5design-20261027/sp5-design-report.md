# SP5-Multiplayer NPC Reduction — Design-only Proposal

- **label:** `SP5design-20261027` · **วันที่:** 2026-10-27
- **ขอบเขต:** design-only — อ่าน source/config/tests/evidence อย่างเดียว · ไฟล์ใหม่ = โฟลเดอร์นี้ไฟล์เดียว
  · ไม่ implement ไม่ build/deploy ไม่เปิดเกม ไม่ hook ไม่แตะเซฟ ไม่ commit/tag ไม่เปลี่ยนโหมดเดิม
  · ไม่มี runtime probe (รออนุมัติแยก)
- **หลักฐานที่อ้าง:** `SP5census-20261026-224844` · `SP5body-20261026` (disasm 471MB, 42 bodies,
  calls-xref 6,350 edges) · `SP5refs-20261026` · `SP5prep-20261026-2222` + RUNTIME เดิม
  `sp0sp7-20261024-225939` · source ปัจจุบันของ `LongYinCoop` (mod repo, อ่านจาก junction
  `<เกม>\LongYinCoop\src\LongYinCoop\`)
- **purity รอบนี้:** hash เกมวัดซ้ำ 2026-10-27 — GameAssembly `33c56def…7920c1`, global-metadata
  `451311d4…18bfd0` **ตรง VERSION.md** ⇒ dump.cs/disasm ที่อ้างเป็นของเวอร์ชันปัจจุบัน (V1.1.2f0) ·
  เกมไม่รัน (`LongYinLiZhiZhuan.exe` ไม่พบ) · game repo ไม่ถูกเขียน
- **ป้ายที่ใช้:** **OBSERVED** = อ่านจาก disasm/dump.cs/source mod/ตารางข้อมูล/RUNTIME ที่เคยวัด ·
  **INFERRED** = อนุมานจากหลักฐานที่มีอยู่ (ระบุเหตุผลทุกจุด) · **UNKNOWN** = ยังพิสูจน์ไม่ได้

---

## 1) ข้อเสนอหลักและเหตุผล

**ลด NPC ทั่วไป 3 สาย (สมาชิกสำนัก / เร่ร่อน / การเติมกลับรายเดือน) ที่ต้นทางของแต่ละสายแยกกัน —
ไม่แตะ factory กลางแบบครอบจักรวาล — โดย gate เฉพาะโหมด co-op ฝั่ง host แบบ opt-in และ fail-closed**

เหตุผลโครงสร้าง (ทั้งหมด OBSERVED ยกเว้นที่ป้ายกำกับ):

1. **ประชากรโลกเริ่มใหม่ ≈972 ตัว (Heros 883 + TempHeros 88) มี PROTECTED เพียง ~171 ตัว
   (SpeHero 170 + ตัวเอก heroID 0) ≈ 17.6%** — ที่เหลือเกือบทั้งหมดคือ generic ที่ปั่นชื่อ/ID ตอนรัน
   (INFERRED จาก RUNTIME รอบเดียว + บัญชีแบบช่วง เพราะ factory calls ≠ ประชากร — ข้อห้ามของโจทย์
   ถือครบ: ตัวเลข 963/883/883+88 ที่ใช้ในเอกสารนี้เป็น "ช่วงประมาณจาก hook รอบเดียว + static" ไม่ใช่
   census รายตัว)
2. **จำนวน generic มาจากต้นทางที่แยกกัน 3 สาย** และแต่ละสายมีจุดควบคุมของตัวเอง (§3):
   - สมาชิกสำนัก (≈677 INFERRED): ลูป #2 ของ `GenerateHero` อ่านจำนวนจาก
     `GameDataController.forceLevelData[n].heroNum[edi]` (OBSERVED native @0x1807ACE26–ACEB5,
     ก่อน factory site 0x1807ADAE6)
   - เร่ร่อน (≈95 INFERRED): ลูป #3 site 0x1807ADD13 — **แหล่งจำนวน UNKNOWN**
   - การเติมกลับ: `ChangeMonth` → `ManageHeroAutoQuitAndEnter` (caller เดียว, OBSERVED xref) ซึ่งมี
     ≥2 กิ่งเติมที่เทียบกับ `GetRecommendHeroNum` (=900) / `GetMaxHeroNum` (=1200+25×ปี) /
     `GlobalData.MinOutforceSimpleHeroNum` (=150) แล้วเติมผ่าน `WorldAddNewHero` (2 call sites)
     ⇒ **ถ้าไม่ gate สายนี้ โลกที่ลดแล้วจะถูกเติมกลับจนถึงเพดาน 900 ภายในไม่กี่เดือนเกม** —
     นี่คือจุดที่กำหนด *steady-state* ประชากร ไม่ใช่ตัวเลขเริ่มโลก
3. **แตะต้นทางดีกว่าลบของที่สร้างแล้ว** — HeroData ถูกอ้างด้วย int-ID ข้ามระบบ 10 สาย + ด้วย**ชื่อ**
   อีก 3 ชั้น (บทสนทนา `sourceName/targetName` / `GlobalData.Spe*Name` / `LoveableSpeHero`)
   (OBSERVED metadata); ตัว NPC generic ไม่มีรายชื่อรายตัวให้เลือกลบอย่างปลอดภัย (census §7)
4. **`GenerateHeroData_C` ใช้ร่วมกัน 11 caller methods** — เปิดโลก ×3 sites, ศัตรูสุ่ม, plot ศึก
   (ผ่าน wrapper B ×419 sites / A ×5 sites), temp plot hero, RecruitUI, `WorldAddNewHero`
   (OBSERVED xref) ⇒ gate ที่ factory กลางจะโดนระบบ TEMP/EVENT และการรับสมาชิกพร้อมกันทั้งหมด —
   ขัดนโยบายแยกกลุ่มของโจทย์ (เหตุผลเต็มใน §4.4)
5. **โหมด co-op เป็นฝั่ง host เท่านั้น** — host เป็น authoritative สร้างโลกเอง; guest อ่าน snapshot
   (S2) หรือส่งคำสั่งผ่าน validator (S3) ไม่เคยสร้างโลก ⇒ การลดติดตั้งบน host เท่านั้น

---

## 2) สถานะ "Multiplayer mode" ใน architecture ปัจจุบัน (ตรวจตามโจทย์ — ไม่สมมติ)

**สิ่งที่มีอยู่จริงใน source วันนี้ (OBSERVED — อ่านจาก `src/LongYinCoop/`):**

| สัญญาณ | ที่อยู่ | ความหมาย | ความน่าเชื่อถือ |
|---|---|---|---|
| `net.cfg` flat key=value | `S2NetConfig.cs` — อ่านตรงไฟล์, poll 1 Hz, fail-closed | `enabled` + `role` (off\|host\|guest) + `secret` + transport | สูง — MelonLoader ไม่แตะไฟล์นี้ (มติ 2026-10-22); พัง/ขาด = `IsValid=false` |
| `S2HostService.Active` | `ApplySettings()`: `want = Enabled && IsValid && Role=="host" && GuardWritePathsClosed` | host เปิดใช้ S2 จริง (listener ทำงาน) | สูง — volatile, ตัดสินบน main thread |
| `S2HostService.Authenticated` | `_authenticated` — handshake ผ่าน (HMAC secret) | **มี guest เชื่อมต่อและยืนยันตัวแล้ว** = session จริง | สูง |
| `TcpListening` / `TcpHost.Connected` | `S2HostService.cs:100-103` | มี listener / มี connection | สูง |

**สิ่งที่ *ไม่มี* (UNKNOWN แบบ "ยืนยันว่าไม่พบใน source ปัจจุบัน"):**

1. **ไม่มี concept "โลก co-op"** — ไม่มี flag ผูกโลก/เซฟกับ multiplayer, ไม่มี state ใดในโมดที่บอกว่า
   "โลกที่กำลังเล่นสร้างมาจาก session co-op" ทุกสัญญาณเป็นระดับ **config/process** ไม่ใช่ระดับโลก
2. ไม่มี hook ใดที่จุดสร้างโลก (`StartNewGame`/`GenerateHero`) ใน build ปัจจุบัน
3. `net.cfg` แก้ได้กลางคัน (poll สด) — ถ้าใช้เป็น gate โดยตรง นโยบายจะเปลี่ยนท่ามกลางโลกได้

**ข้อเสนอ gate (design):**

```
GameNpcPolicy = { Off, Light, Mid, Strong }        // pure enum ของโมด
Key ใหม่ใน net.cfg:  npcPolicy = off|light|mid|strong   (default off)

predicate (pure, เทสต์แบบ S2WriteGuard.Decide):
  gateActive = netCfg.Enabled && netCfg.IsValid && netCfg.Role == "host"
               && ParseNpcPolicy(netCfg.NpcPolicy) != Off
  // ค่าแปลก/ไฟล์ขาด/อ่านไม่ได้ ⇒ Off (fail-closed ตามรูปแบบเดิมของ S2NetConfig)

latch: อ่านครั้งเดียวตอนสร้างโลก (prefix ของ GameController.StartNewGame หรือ GenerateHero)
       เก็บใน mod-session state ใช้ตลอดโลกนั้น — config กลางคันไม่พลิกนโยบายโลกที่สร้างไปแล้ว
```

- จุด latch เลือก `GenerateHero` (caller เดียว = `StartNewGame`, OBSERVED xref) หรือ `StartNewGame`
  (caller = `Start` ×2 จุด — เงื่อนไข 2 กิ่งยังไม่ถอด, UNKNOWN) — **แนะนำ `GenerateHero`** เพราะ
  scope แคบกว่า (เฉพาะสร้างโลก ไม่โหลดเซฟ — `StartLoadGame` ไม่ผ่าน path นี้, OBSERVED body-report §2)
- ข้อจำกัดที่ต้องประกาศกับผู้ใช้: โลกที่สร้างแบบลดแล้ว **ไม่มีข้อมูลในเซฟบอกว่าเคยลด** (โมดไม่เขียน
  อะไรลงเซฟ) — ถ้าโหลดโลกนั้นโดย gate ปิด การเติมรายเดือนจะกลับไปพฤติกรรมเกมปกติ (ประชากรเด้งกลับ
  สู่เพดานเดิมอย่างถูกกฎของเกม) — เป็นนโยบายที่ต้องขอมติ (§6 อนุมัติข้อ 2)
- ไม่ใช้ `AllowRemoteInput`/`DevCommands` มาเป็นส่วนของ gate — อิงมติ rev4 ของ S2a-2 ที่ให้
  `DevCommands` ไม่มีผลกับ gate (OBSERVED source + เทสต์ 64 ชุด)
- ความสัมพันธ์กับ S2 write-guard: ฟีเจอร์นี้เป็น **write path ใหม่** ของโมด (ครั้งแรกที่เขียน state เกม)
  ตามลำดับ stage ปัจจุบัน (S2 = spectator ไม่มี write path) จึงต้องเป็น stage ถัดไป (S5) ไม่ใช่ส่วนของ
  S2 — hook ต้องถูกติดตั้ง/ถอดด้วย Harmony id แยกและมี pref/policy คุมเป็นของตัวเอง

---

## 3) จุดควบคุม 3 จุด — ทางเลือกและข้อแนะนำ (เสี่ยงต่ำสุดก่อน)

### 3.1 จุด A — `GenerateHero` ลูป #2 (สมาชิกสำนัก) + ลูป #3 (เร่ร่อน)

กลไกเดิม (OBSERVED): ลูป #2 อ่าน `k = forceLevelData[n].heroNum[edi]` (edi วน 0..5) → k ผ่าน
`RandomRange/Clamp` → `GenerateHeroData_C` → `List.Add(Heros)` ต่อสำนัก 30 สำนัก (ผ่าน
`FindAvailableHeroID` จัด ID) · ลูป #3 site 0x1807ADD13 ใช้ `GetRandomBigForceID` +
`RandomRareLvByBossLv` → C — **แหล่งจำนวนของลูป #3 ยังไม่ถูกผูกกลับ (UNKNOWN)**

| ทางเลือก | กลไก | ผลต่อการกระจาย | ความเสี่ยง | ตัดสิน |
|---|---|---|---|---|
| A1 **ย่อ heroNum ในหน่วยความจำ** (แนะนำ) | ตอน latch (ก่อน `GenerateHero` เดิน) เขียน `List<int> heroNum` ของ `forceLevelData` แต่ละ entry ด้วยตัวคูณระดับ (เก็บค่าเดิมคืนได้เพราะโหลดจาก CSV ทุกครั้ง `LoadAllGameData`) | **คงกระจายรายสำนัก/ระดับแบบสัดส่วนโดยโครงสร้าง** — ตัวเกมอ่านค่าที่ย่อแล้วไปสุ่มตามสูตรเดิม | ต่ำสุดในบรรดาทางเขียน: แตะ 5×N ค่า int ของตาราง instance เดียว, ไม่ patch ลำดับควบคุมของลูป; consumer อื่นของ `forceLevelData` ยังไม่ถูก enumerate ครบ (UNKNOWN §6.1) | **ใช้เป็นหลัก (ลูป #2)** |
| A2 budget ที่ factory ระหว่างสร้างโลก | prefix บน `GenerateHeroData_C` + "creation-phase" flag (ตั้งโดย prefix/postfix ของ `GenerateHero`, ต้องมี finalizer เคลียร์ทุกกรณี) นับแล้วปฏิเสธเกินงบ | กระจายเบ้ตามลำดับลูป (edi 0..5 — ตัดหาง = เบ้ระดับ) ต้องทำงบต่อสำนักเพื่อลดการเบ้ | กลาง — ต้องจำแนกลูปด้วย argument (INFERRED: ลูป#1 มี name-binding ผ่าน dict; #2 forceID 0..29; #3 forceID −1; **forceID −2 ห้ามแตะ**) และยังไม่มี hook ลายเซ็นเต็มมายืนยันการจำแนก | สำรอง ถ้า A1 โดน consumer อื่น |
| A3 ลบ/ถอด NPC หลังสร้าง | — | — | **ห้าม** ตามโจทย์ (dangling int-ID 10 สาย + name-binding 3 ชั้น) | ตัดทิ้ง |

- ลูป #3 (เร่ร่อน ≈95): แหล่งจำนวน UNKNOWN ⇒ **ระดับ Light ไม่แตะ** · ระดับที่ต้องการลดเร่ร่อนใช้
  A2 เฉพาะกิ่ง `belongForceID == −1` (INFERRED จำแนกได้จาก arg) **หลัง** hook log-only ยืนยัน
  การจำแนกครบ (เงื่อนไขเบื้องต้น §6 อนุมัติข้อ 3)
- ลูป #1 (`SpeHeroDataBase` 170 ตัว) และตัวเอก: **ไม่มีจุดควบคุมใดถูกแตะ** — A1 แตะเฉพาะ `heroNum`
  ซึ่งลูป #1 ไม่อ่าน (ลูป #1 ผูกด้วย dict+ชื่อ, OBSERVED) · A2 ไม่นับลูป #1
- **ข้อ UNKNOWN ที่ช่วยได้ถ้าจริง:** ถ้าลูป #3 อ่าน `heroNum` ชุดเดียวกัน (ยังไม่ผูกคืนได้) การย่อ
  heroNum จะลดเร่ร่อนไปด้วยโดยไม่ต้อง A2 — ตรวจด้วย hook log-only รอบเดียว

### 3.2 จุด B — `GenerateRandomEnemy` (นโยบายแยก ต่างจากประชากรโลก)

กลไกเดิม (OBSERVED เชิงโครงสร้าง): caller ตรงมี 2 จุด — `StartNewGame` (รอบสร้างโลก) และ
`ChangeDay` (รายวัน!) · อ่าน `GetRandomEnemyCount()` (นับใน TempHeros: `isRandomEnemy` && ธง
0x2F0==0) แล้วเติมให้ถึงเป้าจาก `customDifficultyLv[randomEnemyNum]` + `GetTimeRandomDifficulty` →
ต่อตัว: `GetRandomBigForceID` → C → `SetRandomEnemyBadFame` → `AddTempHero`
→ `BigMapController.CreateBigMapNpc`

| ทางเลือก | กลไก | ตัดสิน |
|---|---|---|
| B1 **ใช้ปุ่มของเกมเอง** (แนะนำ) | `customDifficultyLv[randomEnemyNum]` มี slider ในหน้าตั้งค่าเกมอยู่แล้ว (OBSERVED dump + SCREENSHOT-USER) — นโยบาย = **ไม่ hook เลย** ให้ host ตั้งค่าต่ำเวลาสร้างโลก co-op; บันทึกในคู่มือ session | **ศูนย์ code risk** — กลไก native, ย้อนกลับได้ทุกเมื่อ, ไม่แตะ normal mode |
| B2 clamp argument `maxNum` | prefix บน `GenerateRandomEnemy` ลด `maxNum=999` → ค่าต่ำ | UNKNOWN: การแก้ `__args` ของ HarmonyX บน method IL2CPP ที่เป็น int arg ต้องพิสูจน์ก่อน; และเป้าจริงมาจากสูตร difficulty ไม่ใช่ maxNum โดยตรง (สูตรเต็ม INFERRED) |
| B3 budget ที่ factory กิ่ง `isRandomEnemy=true` | ปฏิเสธ C หลังงบ | **ตัดทิ้ง** — chain ต่อจาก C (`SetRandomEnemyBadFame`/`AddTempHero`) ไม่มีหลักฐานว่า null-tolerant (ต่างจาก `WorldAddNewHero` ที่เห็น null-check ทั้ง 2 site) ⇒ NRE เสี่ยง soft-lock |

- ระดับไหนก็ตาม: **B เป็นนโยบายของ "TempHeros" ไม่ใช่ "Heros"** — ห้ามนับ/คุมรวมกับประชากรโลก
  (ID space แยกอยู่แล้ว: TempHeroIDStart=10000, OBSERVED)
- temp plot hero (`临时:` spec) และ NPC ศึกของ PlotController (wrapper A/B): **ไม่มีจุดควบคุมใดถูกเสนอ**
  — ห้ามแตะ (จำเป็นต่อการเล่น plot; wrapper B ×419 sites เป็นของระบบเนื้อเรื่อง)

### 3.3 จุด C — `ManageHeroAutoQuitAndEnter` (เติมรายเดือนจาก `ChangeMonth`) — **ห้ามปิดทั้ง method**

โครง body จาก disasm (OBSERVED — `target-bodies.disasm.txt` @0x1807CC020, body ~8.8 KB):

```
ManageHeroAutoQuitAndEnter (caller เดียว: ChangeMonth — OBSERVED xref)
├─ ช่วงต้น: ลูปใหญ่อ่าน state รายสำนัก/ราย hero (~50 getter call sites)
├─ 0x1807CC43A: HeroRealAutoRemove(...)            ← ส่วน "auto-quit" (สมาชิกจากไป)
├─ บล็อกเติม #1 (CD302–CD37E): GetHeroCount → เทียบ GetRecommendHeroNum (=900)
│    └→ WorldAddNewHero  site 1 (CD37E)  → ผลลัพธ์มี null-check (je CC547, OBSERVED)
├─ บล็อกกลาง (CD6A6–CD7DB): GetHeroCount → เทียบ GetMaxHeroNum (=1200+25·year) +
│    GetRecommendHeroNum + เงื่อนไขรายสำนัก/ราย hero (loop, ไม่มี WorldAddNewHero)
└─ บล็อกเติม #2 ท้าย (CDE9A–CE0B1): เทียบ GetMaxHeroNum + GlobalData.MinOutforceSimpleHeroNum
     (=150, field 0xA4) → WorldAddNewHero  site 2 (CE0B1) → null-check (je CE1A6, OBSERVED)
```

⇒ ยืนยันตามข้อบังคับโจทย์: method นี้**ไม่ใช่**แค่เติมสมาชิก — มีส่วน auto-quit
(`HeroRealAutoRemove`) และการจัดการสมาชิก/เหตุการณ์ที่ต้องรอด ⇒ ห้าม skip ทั้ง method

| ทางเลือก | กลไก | ผล | ตัดสิน |
|---|---|---|---|
| C1 **deny ที่ `WorldAddNewHero`** (แนะนำสำหรับ Mid/Strong) | prefix คืน null เมื่อ gate ตาม policy | ปิด "การสร้าง hero ใหม่ทั้ง 2 site" แต่ auto-quit/ย้ายสำนัก/จัดการสมาชิกทำงานต่อครบ — caller ทั้ง 2 site มี null-check อยู่แล้ว (OBSERVED; **สิ่งที่ null-branch ทำยังต้องยืนยันตอน implement**) | ความเสี่ยงต่ำสุดของการปิดเติม — `WorldAddNewHero` มี direct caller แค่ method นี้ (OBSERVED xref; indirect = lower bound) |
| C2 **clamp เป้าด้วย postfix** | postfix บน `GetRecommendHeroNum`/`GetMaxHeroNum` ลดค่าที่คืนเมื่อ gate เปิด (caller ตรง = method นี้เท่านั้น ×2 sites, OBSERVED xref) | เกมยังเติมแต่ถึงเพดานที่ต่ำกว่า — logic เดิมครบ, ปรับระดับได้เป็นขั้น | **แนะนำสำหรับ Light/Mid** — สมดุลกว่า deny (โลกยังมีชีวิต: สมาชิกจากไป/เข้ามาตามกติกาเดิมแต่ที่เพดานต่ำ) |
| C3 แก้ `ForceData.autoAddMember=false` รายสำนัก | เขียน bool field 28/30 สำนัก | ใช้สวิตช์ native ของเกม | UNKNOWN ว่า field นี้เป็นเงื่อนไขของกิ่งเติมหรือไม่ (decl-only) — เสนอไว้เป็นทางเลือก ต้องยืนยันด้วย hook ก่อน |
| C4 skip ทั้ง `ManageHeroAutoQuitAndEnter` | prefix return false | ตัด auto-quit + การจัดการอื่นพร้อมกัน | **ตัดทิ้ง** ตามโจทย์ |
| C5 แก้ `GlobalData.MinOutforceSimpleHeroNum` (static field) | เขียน field | ลดเพดานกิ่งเติมเร่ร่อนนอกสำนัก | ไม่แนะนำ — static readonly ใช้ร่วมทั้ง process, แตะ global state ของเกมโดยไม่จำเป็น (deny ที่ C1 ครอบแทนได้) |

หมายเหตุ (INFERRED จาก disasm): บล็อกเติม #2 ท้าย body เทียบกับ `MinOutforceSimpleHeroNum=150`
ด้วยการ**อ่าน field ตรง** (`cmp r13d,[rax+A4h]` ตรง layout GlobalData) ไม่ผ่าน method call ⇒
การ clamp ด้วย C2 ไม่ครอบกิ่งนี้ — ถ้า policy ต้องการปิดกิ่งนี้ด้วย ต้องใช้ C1 (deny ที่
WorldAddNewHero) — จุดนี้ทำให้ "refill policy" มี 2 ระดับจริง: clamp (C2) กับ deny (C1)

---

## 4) ระดับการลด — เปรียบเทียบ 3 ระดับ

> ตัวเลขทั้งหมดเป็น **ค่าที่เสนอเพื่อเปรียบเทียบ (INFERRED)** — คำนวณจากช่วงประมาณของ census
> (factory ≠ ประชากร; บัญชี 963/883/81 ยังเป็น INFERRED-closure) — **ห้ามอ้างว่าปลอดภัยจนกว่ามี
> runtime test ตาม §6** · ไม่กำหนดเป้าประชากรรวมตายตัวตามโจทย์

ตัวคูณที่ใช้: `heroScale` = ตัวคูณของ `heroNum` (A1) · `refill` = นโยบายเติมรายเดือน (C2/C1/off) ·
`enemy` = นโยบายศัตรูสุ่ม (B1/B2)

| หัวข้อ | **L1 Light** | **L2 Mid** | **L3 Strong** |
|---|---|---|---|
| สมาชิกสำนัก (ลูป#2, ≈677) | `heroScale ≈ 0.5` → ≈340 | `heroScale ≈ 0.25` → ≈170 | `heroScale ≈ 0.05–0.1` → ≈35–70 |
| เร่ร่อน (ลูป#3, ≈95) | ไม่แตะ (≈95) | ลดครึ่งด้วย A2 กิ่ง −1 (≈48) *หลัง hook ยืนยันจำแนก* | งบใกล้ 0 (≈0–20) |
| กลุ่ม forceID −2 (21) | คงเดิม 21 | คงเดิม 21 | คงเดิม 21 |
| PROTECTED (170+ตัวเอก) | ครบทุกระดับ — ไม่มีจุดควบคุมใดแตะลูป#1/ตัวเอก | ครบ | ครบ |
| **ประชากรเริ่มโลกโดยประมาณ (Heros)** | ≈625–650 | ≈410–440 | ≈230–280 |
| **เติมรายเดือน (ChangeMonth)** | คงเกมเดิม (C off) — เพดานเดิม 900 ยังคุม long-run | **C2 clamp เพดานเป้า ≈ครึ่ง** (postfix Recommend/Max) + กิ่ง 150 คงเดิม | **C1 deny ที่ WorldAddNewHero** — ไม่เติมใหม่เลย, auto-quit ทำงานต่อ ⇒ ประชากรเอียงลงตามธรรมชาติ (UNKNOWN: ต่ำสุดจะไปถึงเท่าไร) |
| **ศัตรุสุ่ม / TempHeros** | B1 (ปุ่มเกมเอง) — ค่าเดิม | B1 | B1 + คู่มือให้ตั้ง slider ต่ำ (หรือ B2 ถ้าอนุมัติพิสูจน์ `__args`) |
| ผลต่อ plot/mission | ต่ำสุด — สระ hero ยังใหญ่ | กลาง — ต้องวัด PlotRandomHeroData/FindAllHero | สูงสุดความเสี่ยง — สระเล็กมาก อาจเพาะซ้ำ/หาตัวไม่ได้ |
| ข้อดี | เสี่ยงต่ำสุด, แก้ย้อนง่ายสุด, คงบรรยากาศโลก | สมดุล — ลดชัด แต่โลกยังเติบโตได้ที่เพดานใหม่ | ใกล้เป้า "รักษา ~200" ของงานเดิม, steady-state คุมได้ |
| ข้อเสีย | ลดไม่มาก (~30–35%) | กระจายระดับเบ้นิดจาก A2; ต้อง clamp 2 method เพิ่ม | เนื้อหา/เควสเสี่ยงสุด; auto-quit โดยไม่มีเติม = สำนักกร่อน (UNKNOWN ผลต่อ SelfForceEventMinHeroNum) |
| UNKNOWN ต้องทดสอบ | consumer อื่นของ forceLevelData; ผลกับลูป#3 | null-branch ของทั้ง 2 site; ความเร็วเติมหลัง clamp | สระเล็กกับ mission/plot generator; เพดานล่างของ auto-quit |

หมายเหตุร่วม: ทุกระดับ **ไม่เขียนเซฟและไม่แก้ตาราง CSV** — สิ่งที่เขียนมีแค่หน่วยความจำของ process
(ค่าที่เกมกำลังจะอ่าน) และ hook ทั้งหมดถอดได้ตอน runtime (รูปแบบ Apply/Uninstall เดิมของโมด) ·
ระดับเลือกได้ **ต่อโลก** ตอนสร้างโลก (latch) ไม่ใช่ต่อเดือน

---

## 5) Call flow ก่อน/หลัง และจุด gate (ระดับ design)

### 5.1 ก่อน (เดิม — OBSERVED จาก SP5body §2)

```
Start(×2 sites) → StartNewGame
  ├─ LoadAllGameData (ตาราง CSV รวม forceLevelData ← LTCSVLoader)
  ├─ GenerateHero                      [เฉพาะสร้างโลก; caller เดียว]
  │   ├─ ลูป#1: SpeHeroDataBase (170, name-binding) → C → Heros   [PROTECTED]
  │   ├─ ตัวเอก ctor arg=0 → Heros                                 [PROTECTED]
  │   ├─ ลูป#2: heroNum[n][edi] → RandomRange → C → Heros          [≈677 generic]
  │   ├─ ลูป#3: GetRandomBigForceID+RandomRareLvByBossLv → C → Heros[≈95 generic + 21 กลุ่ม −2]
  │   └─ GenerateHeroDict
  ├─ GenerateRelationShip (ผูกจากรายชื่อที่เพิ่งสร้าง — ไม่มี ID คงที่)
  ├─ GenerateRandomEnemy(maxNum=999) → C → AddTempHero (เริ่มโลก)
  └─ ...
ChangeDay      → GenerateRandomEnemy (รายวัน)
ChangeMonth     → ManageHeroAutoQuitAndEnter
  ├─ HeroRealAutoRemove (auto-quit)
  └─ [เป้า 900 / 1200+25·year / 150] → WorldAddNewHero → C → AddNewHero (เติมกลับ)
PlotController  → B/A wrapper → C (ศึก/temp hero)  ·  RecruitUI → C/AddTempHero
```

### 5.2 หลัง (ข้อเสนอ — จุด gate ห้าจุด ทั้งหมด fail-closed ภายใต้ `gateActive`)

```
[npcPolicy key ใหม่ใน net.cfg → predicate (pure) → latch ที่ GenerateHero prefix]

Start → StartNewGame → GenerateHero
  ↑(prefix) latch policy ของโลกนี้ (Off = ไม่แตะอะไรเลย — normal mode เดินทางเดิม 100%)
  ├─ [G1] (Off→หยุดตรงนี้ทุกกิ่ง) ก่อนลูป#2/#3: ถ้า policy เปิด → ย่อ heroNum ในหน่วยความจำ
  │        ตาม heroScale (A1) — ลูป#1/ตัวเอกไม่เกี่ยว ไม่มีโค้ดใดแตะ
  ├─ ลูป#1: SpeHero 170 → คงเดิมทุกพฤติกรรม            [PROTECTED — ไม่มี gate ผ่าน]
  ├─ ตัวเอก heroID 0 → คงเดิม                            [PROTECTED]
  ├─ ลูป#2: อ่าน heroNum ที่ย่อแล้ว → สุ่ม/Clamp สูตรเดิม   [ลดจากต้นทาง]
  ├─ ลูป#3: คงเดิม (L1) / [G2] budget กิ่ง forceID==−1 (L2/L3, หลัง hook ยืนยัน)
  │        — กิ่ง forceID==−2 ไม่มีงบ ไม่มี gate ห้ามแตะ   [UNKNOWN-group: คงเดิม]
  └─ GenerateHeroDict / GenerateRelationShip → คงเดิม (อ่านสระที่เหลือ — ไม่มี ID คงที่)
GenerateRandomEnemy → ไม่ hook (B1: ใช้ slider ของเกม) [G3 เป็นนโยบาย ไม่ใช่โค้ด]
  (L3 เสนอ [G3'] clamp maxNum — ต้องพิสูจน์ __args ก่อน — ยังไม่แนะนำ)
ChangeMonth → ManageHeroAutoQuitAndEnter
  ├─ auto-quit / จัดการสมาชิก → คงเดิม 100% (ไม่มี gate)
  └─ [G4] L2: postfix GetRecommendHeroNum/GetMaxHeroNum → เพดานย่อ (เกมเติมถึงเพดานใหม่)
      [G5] L3: prefix WorldAddNewHero → null (caller 2 site null-check แล้ว — ยืนยัน
           null-branch ตอน implement ก่อนเปิดจริง)
PlotController/RecruitUI/temp plot hero → ไม่มี gate ใด ๆ คงเดิม 100%  [TEMP/EVENT แยกชัด]
```

### 5.3 fail-closed / ป้องกัน normal mode / เหตุผลไม่แก้ factory กลาง

- **fail-closed:** predicate ตัดสินจากไฟล์ที่ parse แบบ fail-closed อยู่แล้ว (ไม่มีไฟล์/มี section/บรรทัดเสีย
  ⇒ `IsValid=false` ⇒ gate=Off) · ค่า policy ที่ไม่รู้จัก = Off · hook ทุกตัวตัดสินจาก predicate
  (pure, เทสต์ truth-table ได้แบบ `S2WriteGuard.Decide`) — ถ้าไม่ยืนยันสถานะ co-op ได้ = เกมเดิม
- **normal mode ไม่เปลี่ยน:** default `npcPolicy=off` · gate ต้องมี `role=host` และ enabled — ผู้เล่นเดี่ยว
  ที่เผลอเปิด net.cfg ก็ยังได้เกมเดิมจนกว่าจะตั้ง policy ชัดเจน (opt-in สองชั้น) · ติดตั้ง hook แบบเดิม
  (Apply เมื่อ wanted / Uninstall ได้) — รูปแบบเดียวกับ JitProbeW* · มี negative test บังคับ (§6 ข้อ T1)
- **ห้ามแก้ `GenerateHeroData_C` แบบ global (เหตุผล):** factory ตัวเดียวรองรับ 11 caller methods —
  เปิดโลก, ศัตรูรายวัน, ศึก plot (×419 sites ผ่าน B), รางวัล/ผู้ตาม/ศึก master ของ plot (A ×5),
  temp plot hero ตอนบทสนทนา, RecruitUI, เติมรายเดือน — gate ครอบจักรวาลที่ C จะเปลี่ยนพฤติกรรม
  ระบบ TEMP/EVENT และการรับสมาชิกทันที ขัด (ก) นโยบายแยกกลุ่ม (ข) ห้ามเปลี่ยนโหมดเดิม
  (คร) ทำให้ null-tolerance กลายเป็นข้อกังวลทุก caller แทนที่จะจำกัดที่ caller ที่มีหลักฐาน null-check
  แล้ว (`WorldAddNewHero` 2 sites)

---

## 6) แผนทดสอบก่อนอนุมัติ implementation

### 6.1 Static checks (ทำได้โดยไม่เปิดเกม — ไม่ต้องขออนุมัติ runtime)

| # | ตรวจอะไร | วิธี | เกณฑ์ผ่าน |
|---|---|---|---|
| S1 | predicate ของ gate | pure truth-table (enabled×role×policy×cfg เสีย — สไตล์ 64 ชุดของ S2WriteGuard) | fail-closed ครบทุกชุด: cfg ไม่มี/เสีย/ค่าแปลก ⇒ Off; เฉพาะ enabled+host+policy≠off ⇒ เปิด |
| S2 | parser net.cfg กับ key ใหม่ | ต่อเทสต์ `S2NetConfig.Parse` เดิม (242 เคส) | key ไม่รู้จักถูกข้าม; `npcPolicy=Light` (ตัวพิมพ์ผสม) แปลงเป็น enum ได้; ค่าแปลก ⇒ Off + error ไม่ทำไฟล์พัง |
| S3 | รายการ hook เป้าหมาย | audit โค้ด (สคริปต์ + ตา) | มีเฉพาะ: `GenerateHero`(prefix), `GenerateHeroData_C`(budget — เฉพาะเมื่อเลือก A2), `GetRecommendHeroNum`/`GetMaxHeroNum`(postfix, L2), `WorldAddNewHero`(prefix, L3) — **ไม่มี** hook บน C โดยไม่มี phase-guard, ไม่มี hook ใดใน save/load path |
| S4 | phase-guard ของ A2 | ตรวจว่ามี finalizer เคลียร์ flag ทุกทางออก (throw รวม) | flag ค้างไม่ได้เกิน 1 call ของ GenerateHero |
| S5 | re-resolve target | เทียบ interop assembly กับ RVA ของ SP5body (0x7AC8F0/0x7B9FB0/0x7BEED0/0x7E5B50) | ทุกเป้าหาเจอด้วย interop แบบมีชนิด (กติกา 8) — ถ้าต้องใช้ raw offset ต้องรายงานก่อน |
| S6 | ไม่แตะเขตต้องห้าม | grep โค้ดใหม่ | ไม่มีการอ้าง Steam/APIDispatcher/DnsChecker/CheatCheck/achievement; ไม่เขียนเซฟ; ไม่แก้ CSV |
| S7 | S2 ไม่ถูกกระทบ | รันชุดเทสต์เดิมทั้งหมด (`S2Protocol.Tests` 179+242) | PASS ทั้งหมด, `git diff` ไม่แตะไฟล์ S2/Protocol |

### 6.2 Runtime tests (ทุกข้อมีต้องขออนุมัติแยก — บนสำเนาเซฟตาม §1.2 · ป้าย [A] = ต้อง hook)

| # | ทดสอบ | วิธี (ย่อ) | GO criterion | NO-GO criterion |
|---|---|---|---|---|
| T0 [A] **prerequisite ทุกระดับ** | hook log-only 1 creation round: ลายเซ็นเต็มของ C ทั้ง 3 overload + ค่า `forceLevelData.heroNum` หลัง LoadAllGameData + จำแนกลูปด้วย arg + นับ `List.Add` ต่อลิสต์ | บัญชีปิดแบบพิสูจน์ (882/81, gap=0); การจำแนก −1/−2/สำนัก ชัดทุก call; ค่า heroNum ได้จริง | hook เห็น path ที่สอง (factory=0 แต่ Heros เต็ม — กรณี SP4 รอบ 3) หรือนับไม่ปิด |
| T1 normal mode ไม่เปลี่ยน | สร้างโลก gate=off บนสำเนา | ไม่มี patch ติด (log ยืนยัน); census/จำนวน/กระจายอยู่ใน band RNG ของ baseline (≥3 รอบ); hash ไฟล์เกม/ตารางไม่เปลี่ยน | ต่างจาก band หรือมี patch ติด |
| T2 PROTECTED ครบ | census หลังสร้างโลก gate เปิด (ทุกระดับ) | `speHero=170` ครบ, 掌门 30/30, ตัวเอก heroID=0 + BirthSetting ถูกต้อง, `Teacher=1` จุดเรียนยังได้ตัวเดิม, LoveableSpeHero 57 ครบ | ตัวใดหาย/ผิดตัว |
| T3 กลุ่ม forceID −2 | นับ/สังเกตพฤติกรรม | จำนวน + พฤติกรรมเดิม (band RNG) | ต่างจาก band |
| T4 generic ลดจริงจากต้นทาง | census ต่อสำนัก/ระดับ | จำนวนลดตามระดับ (±band); กระจายรายสำนักคงสัดส่วน (ค่าเบี่ยงต่อสำนัก ≤ band ที่กำหนดในแผน implement); factory call ลดตาม (ไม่มีการลบหลังสร้าง) | เบ้เกิน/ลดไม่ตรง/ต้องลบทีหลัง |
| T5 ไม่เด้งกลับ | เดินเกม ≥25 วัน (เจาะจงข้าม ChangeMonth ≥2 ครั้ง) | L1: เพดานเดิมคุมอยู่; L2: เติมถึงเพดานย่อแล้วหยุด; L3: Heros ไม่โต (TempHeros ตาม B policy) | ประชากรวิ่งกลับสู่ 900 / เกินเพดานที่ตั้ง |
| T6 plot/mission/temp hero | เปิด plot ≥20 + mission ≥20 บนโลกที่ลด | 0 dangling-ID error; temp plot hero spawn/ลบได้; ศึก plot เริ่ม/จบได้ | dangling เกิด/plot ไม่ยิง/ศึกค้าง |
| T7 โครงสร้าง world data | ตรวจ `Heros`/`HerosDict`/`TempHerosDict`, `FindAvailableHeroID`, ownHeros/leader ต่อสำนัก | dict = list ครบ; ไม่มี ID ชน; leader/ownHeros สอดคล้อง; `GetHeroForce` ไม่ miss ในบริบทปกติ | dict คลาด/ID ชน/leader หาย |
| T8 save/load/save | เซฟ → โหลด → เซฟซ้ำบนสำเนา | digest นิ่งรอบสอง; HeroList/TempHeroList + ธง finished ครบ; โหลดแล้วจำนวนคงเดิม | digest เปลี่ยน/โหลดแล้วประชากรเปลี่ยน |
| T9 guest มองเห็นถูกต้อง | guest overlay ต่อ host (โลกที่ลด) | snapshot ตรง host (Div-A = 0); จำนวน NPC ที่ overlay แสดง = host | mismatch |
| T10 rollback จริง | ปิด gate กลาง session (โลกเดิม) + ปิดโมด | config กลับ = hook หลุดหมด; โลกที่สร้างไว้เดินต่อได้; ไม่ crash; ประชากรคงอยู่ตามที่สร้าง (ระบุชัดว่า refill กลับสู่เกมเดิม — มติ §2) | crash/หา patch ไม่เจอ/soft-lock |

**ลำดับ:** S1–S7 → T0 (อนุมัติ hook log-only ก่อนเป็นอันดับแรก) → T1–T10 ต่อระดับที่เลือก
→ รายงานผลวัด → ขออนุมัติ implement เต็ม (แยกตามกติกา stage)

---

## 7) ขอบเขตที่ต้องคงไว้ (สรุปบังคับ)

1. `SpeHeroData` 170 records + name-binding 3 ชั้น (บทสนทนา / `Spe*Name` / `LoveableSpeHero`) —
   ลูป #1 ไม่มีโค้ดใดแตะ, ห้ามแก้เงื่อนไข name-binding
2. ตัวเอก `heroID=0` + `BirthSettingPlayerData`
3. กลุ่ม `forceID=−2` (21) — คงพฤติกรรมเดิมจนกว่าจะระบุหน้าที่ (UNKNOWN)
4. `GenerateRandomEnemy`/TempHeros/temp plot hero/NPC ศึกของ PlotController/RecruitUI — นโยบายแยก
   (ห้าม gate รวมในประชากรโลก, ห้าม gate factory ครอบจักรวาล)
5. `ManageHeroAutoQuitAndEnter` ส่วน auto-quit + การจัดการสมาชิก — ห้ามปิดทั้ง method
6. normal mode ทั้งหมด — default Off, opt-in สองชั้น, negative test บังคับ
7. เซฟ/CSV/ข้อมูลเกม — ไม่เขียนทั้งหมด (สิ่งที่เขียน = หน่วยความจำของ process เท่านั้น)
8. กติกา 6 (main thread) / กติกา 8 (typed interop, ห้าม raw offset ยกเว้นรายงานก่อน) — ใช้กับ hook ใหม่ทุกตัว

---

## 8) UNKNOWN และข้ออนุมัติที่ยังต้องรอ

### 8.1 UNKNOWN (ปิดด้วย T0 / hook log-only เป็นหลัก)

1. ค่า `heroNum` ราย entry และ consumer อื่นของ `forceLevelData` นอกจากลูป #2 (ผลถ้าย่อแล้วต่อระบบอื่น)
2. แหล่งจำนวนของลูป #3 (เร่ร่อน) — และว่าย่อ heroNum กระทบมันหรือไม่
3. ความหมาย/หน้าที่ของกลุ่ม `forceID=−2` (21)
4. สิ่งที่ null-branch ของ `WorldAddNewHero` ทั้ง 2 site ทำจริง (เห็นแค่ว่ามี null-check)
5. สูตรเต็มของ `GenerateRandomEnemy` (SSE) — อัตราเติมต่อวันจาก difficulty
6. ผลของสระ hero เล็กลงต่อ `PlotRandomHeroData`/`FindAllHero`/mission generator (T6 วัด)
7. `SelfForceEventMinHeroNum` และ `GameController.cctor` — ผลต่อ event ของสำนักผู้เล่นเมื่อสำนักเล็กลง
8. กรณี SP4 รอบ 3 (factory=0 แต่ Heros≈883) — ต้อง re-run hook พิสูจน์ก่อนเชื่อว่า gate ครอบครบ
   (ถ้ามี path ที่สองจริง ต้องขยาย gate — เงื่อนไข NO-GO ของ T0)
9. HarmonyX `__args` mutation บน IL2CPP int arg (เฉพาะถ้าจะใช้ B2)
10. การแบ่ง `GlobalData.Spe*Name` ราย field (PARTIAL จาก SP5body) — ไม่บล็อก design แต่ต้องครบก่อน
    implement เผื่อการจำแนกที่อิงชื่อ

### 8.2 ข้ออนุมัติที่ต้องรอ (ห้ามเริ่มเอง)

1. **อนุมัติรายงาน design นี้** + เลือกระดับ (L1/L2/L3) และตัวคูณจริงต่อระดับ
2. มตินโยบาย: โลกที่ลดแล้วถ้าโหลดโดย gate ปิด — ยอมรับ refill กลับสู่เพดานเกมเดิมหรือไม่ (§2)
3. **อนุมัติ T0** (hook log-only 1 creation round บนสำเนาเซฟ) — เบื้องต้นของทุกงาน implement
4. ชื่อ key ใน net.cfg (`npcPolicy`) และค่าที่ยอมรับ — แก้ตามมติได้ก่อน implement
5. การกำหนด stage ถัดไปในลำดับ STAGEPLAN (งานนี้ = write path แรกของโมด ตามธรรมเนียมต้องมี
   แผนราย stage + exit criteria ก่อน build/deploy — แยกจากแผนค้างทั้งหมดของ S2/S3 ที่ยังรอมติเดิม)

---

**หยุดหลังส่งรายงาน — ไม่ implement, ไม่ hook, ไม่ runtime probe จนกว่าจะมีอนุมัติแยกตาม §8.2**
