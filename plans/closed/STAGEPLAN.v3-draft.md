> [v3 rev1 — SUPERSEDED 2026-10-24] ไฟล์นี้ถูกแทนด้วย **STAGEPLAN-v3-addendum.md (rev2)** ตามมติรีวิว ข้อ 7 (ส่วนใหม่ของ v3 อยู่ในไฟล์เดียวนั้น ไม่แทรกต่อท้ายเล่มเก่า) — ไฟล์นี้คงไว้เป็นประวัติ rev1 เท่านั้น ห้ามอ้างอิง §15–§20 จากไฟล์นี้ · เนื้อหา rev1 ครบใน STAGEPLAN.v2-to-v3.diff

# STAGEPLAN — LongYinLiZhiZhuan Co-op Mod (v2 merged)

> **[v3] สถานะ: ร่างรออนุมัติ (2026-10-24) — ยังไม่เริ่มงานใด ๆ ของ ช่วง 1 (§15–§20) ทั้งโค้ด/probe/สร้างไฟล์ จนกว่า §20 จะได้มติครบ และต้องผ่าน gate ของ S3 ก่อน**
> [v3] เป้าหมายใหม่ที่เพิ่มในเล่มนี้: **C2 = "โลกเดียวกลาง server ตัดสินทุกอย่าง"** — world จำลองเดียว อยู่กับ server = โปรเซสเกมหนึ่งตัวที่ถือ state จริงทั้งหมด; ผู้เล่นทุกคน (รวม host) ขับโลกผ่าน verb ที่ผ่านการตรวจฝั่ง server เท่านั้น
> [v3] กติกาการเขียนฉบับนี้: v3 ทำเป็น **diff จาก v2** — ทุกส่วน §0–§14 คงข้อความของ v2 merged ไว้เป๊ะทุกตัวอักษร (บรรทัดสถานะถัดลงไปเป็นข้อความเดิมของ v2 ที่ล้าสมัยแล้ว — คงไว้ตามตัวอักษรเพื่อให้ diff สะอาด) · สิ่งที่เพิ่มมี 4 จุดเท่านั้น: (1) บล็อกหัวไฟล์นี้ (2) ลำดับ ช่วง 1 แทรกท้าย §12.1 (3) บรรทัด changelog v3 ท้าย §12.2 (4) §15–§20 แทรกหน้าภาคท้าย (v2 ดิบ + แผนย่อย S1.5/S2a-2/S2a-3) — **v3 ไม่แก้ขอบเขต/เกณฑ์/ข้อความของ S0–S6 ใด ๆ**
> [v3] สถานะจริง ณ วันเขียน: S0/S1/S1.5/S1.5g/S2 (S2a-2, S2a-3) ปิดแล้ว — tag `stage-S2a-3` รออนุมัติขั้นสุดท้าย (X5 = PASS ตามมติ 2026-10-24, Δmean −0.023 ms); ถัดไปตามลำดับ ช่วง 0 คือ S3 — สถานะปัจจุบันฉบับเต็มดู §14 + AGENTS.md


**สถานะ: รออนุมัติ — ยังไม่เริ่ม S0 (ไม่มีโค้ด, ไม่แตะไฟล์เกม)**
หลักการ S0: ไม่แตะ state ของเกม ไฟล์ที่เพิ่มในโฟลเดอร์เกมมีแค่ DLL ของเรา (`Mods\LongYinCoop.dll`) และไฟล์ config/log ของ mod เอง [v2]
ลำดับการทำงาน: **S0 → S1 → S1.5 → S2 → S3 → S5 → S4 → S6**
อ้างอิง: `battle-notes.md`, `worldmap-notes.md`, `Il2CppDumper/dump.cs`, และการตรวจโฟลเดอร์เกมจริง

Legend ของ tag:
- **[RUNTIME]** = วัด/เห็นจากเกมที่รันอยู่ หรือจากไฟล์ในโฟลเดอร์เกมตอนนี้
- **[DUMP]** = จาก `dump.cs` / `il2cpp.h` (ยังไม่ยืนยันตอนรัน)
- **[GUESS]** = ยังไม่ยืนยัน ต้องเก็บหลักฐานเพิ่ม
- **[SPEC]** = ข้อกำหนด/การออกแบบของแผนนี้ (ไม่ใช่ข้อเท็จจริงของเกม)

---

## 0. เป้าหมายและขอบเขต

### 0.1 ขอบเขต

- เกม: LongYinLiZhiZhuan, Unity 2020.3.48f1c1, IL2CPP
- Loader: MelonLoader v0.7.3 (ติดตั้งอยู่แล้ว), HarmonyX ที่มากับ ML, runtime net6
- Mod อื่นที่มีอยู่: `LongYinThai.dll`, UnityExplorer (ห้ามแก้)
- โมเดล: **host เป็นผู้ตัดสินเพียงคนเดียว (authoritative)** — guest ไม่จำลอง ไม่สุ่ม ไม่คำนวณผล
  เหตุผล: RNG ของศึกเป็น `UnityEngine.Random` ตัวกลางที่ไม่มี seed ของเกมเอง
- รูปแบบ co-op: guest คุมยูนิตที่อยู่ในเซฟของ host (ศิษย์/สหาย) รางวัลเข้าเซฟ host — **guest ห้าม save ตลอดไป**
- ป้ายหลักฐาน: `RUNTIME` / `DUMP` / `GUESS` / `SPEC` (ดู Legend ด้านบน)

### 0.2 หลักการเดียวที่ห้ามละเมิด

**host จำลองของจริงคนเดียว; guest ไม่จำลอง ไม่สุ่ม ไม่คำนวณผล** เพราะ RNG ของศึกคือ `UnityEngine.Random` ตัวกลาง (global) — ถ้าสองฝั่งสุ่มซ้ำจะ desync ทันที [DUMP/RUNTIME จาก battle-notes §RNG]

### 0.3 สถาปัตยกรรมโดยสรุป

```
HOST (real game, authoritative)              GUEST (NOT authoritative)
+------------------------------+               +-------------------------------+
| game state (BattleController)|               | (mirror: S4 writes only)      |
|   ^ method calls only        |               |   ^ state writes only in S4   |
|   |                          |               |   |                           |
| CoopMod.OnUpdate (main thr.) |               | CoopMod.OnUpdate (main thr.)  |
|   -> MainThreadDispatcher    |               |   -> MainThreadDispatcher     |
|        ^                     |               |        ^                      |
|   CommandValidator <-- CMD --+----TCP------->|   IntentBuilder (guest input) |
|        |                     |               |        |                      |
|   SnapshotReader -- SNAP ----+---SNAP------->|   ViewModel -> overlay/IMGUI  |
|   BattleLogHook (Harmony)    |               |                               |
+------------------------------+               +-------------------------------+
```

---

## 0A. ผลตรวจสภาพแวดล้อมก่อนติดตั้ง (ต้องทำก่อนทุกอย่าง)

### 0A.1 ตารางหลักฐาน — ตรวจแล้วเมื่อ 2026-10-20

| รายการ | ค่าที่ตรวจได้ | หลักฐาน |
|---|---|---|
| Unity version | **2020.3.48f1c1** (China build) | [RUNTIME] `LongYinLiZhiZhuan_Data/globalgamemanagers` + `MelonLoader/Latest.log` |
| Scripting backend | **IL2CPP** (`GameAssembly.dll`, `global-metadata.dat`) | [RUNTIME] |
| `MelonLoader\` มีอยู่แล้ว | **ใช่ — v0.7.3** (ไม่ต้องติดตั้งซ้ำ) | [RUNTIME] `Latest.log`: `MelonLoader v0.7.3` |
| `Mods\` มีอยู่แล้ว | **ใช่** — `LongYinThai.dll` (v1.9.0), `UnityExplorer.ML.IL2CPP.CoreCLR.dll`, โฟลเดอร์ `LongYinThai/`, `sinai-dev-UnityExplorer/` | [RUNTIME] `ls Mods` |
| ML runtime | CoreCLR **net6.0** (`tfm: net6.0`, rollForward LatestMinor) | [RUNTIME] `MelonLoader/net6/MelonLoader.runtimeconfig.json` |
| HarmonyX | **มีให้แล้ว** — `MelonLoader/net6/0Harmony.dll` (264,192 B) + `Il2CppInterop.HarmonySupport.dll` (26,624 B) | [RUNTIME] `ls -l` |
| Compile-time refs | `MelonLoader/Il2CppAssemblies/` = **90 assemblies** รวม `Assembly-CSharp.dll` | [RUNTIME] `ls` |
| Dummy DLL สำรอง | `Il2CppDumper/DummyDll/` = 59 assemblies | [RUNTIME] |
| Loader proxy | `version.dll` ที่ root = proxy ของ MelonLoader | [RUNTIME] |
| mod อื่นที่อาจชน | `LongYinThai` patch ผ่าน **HarmonyLib + TextMeshPro** (ตรวจ string ใน dll: พบ `HarmonyLib`, `MelonMod`, `TextMeshPro`; **ไม่พบ** `AddInfoText` / `BattleController` / `HudController`) | [RUNTIME] `grep -a` บน `Mods/LongYinThai.dll` |
| ML เห็นเกมเป็นอะไร | `Game Type: Il2cpp`, `Game Arch: x64`, host = HostFXR 10.0.12 | [RUNTIME] `Latest.log` |
| ML จับคู่ Unity ให้แล้ว | `Using Unity Dependencies Version = 2020.3.48` (ตรงกับ 2020.3.48f1c1 ของเกม) | [RUNTIME] `Latest.log` บรรทัด `Il2CppAssemblyGenerator` |
| Interop generator ที่ค้างอยู่ | `Il2CppInterop 1.5.1-ci.845+f03c8f4`, `Cpp2IL 2022.1.0-pre-release.21`; `Assembly is up to date. No Generation Needed.` | [RUNTIME] `Latest.log` |
| mod ที่โหลดสำเร็จจริง | `LongYinThai v1.9.0`, `UnityExplorer v4.13.6` → ยืนยันว่า ML รัน mod IL2CPP ได้บน build นี้ | [RUNTIME] `Latest.log` (`Melon Assembly loaded`) |
| RemoteAPI ของ ML | `Game Version: UNKNOWN` + `Game Not Found on RemoteAPI Host` → **ไม่มี mapping/dump สำเร็จรูปให้** ต้องใช้ offset จาก dump ของเราเอง | [RUNTIME] `Latest.log` |

### 0A.2 เลือก loader/เวอร์ชัน + เหตุผล

**ตัดสินใจ: ใช้ MelonLoader 0.7.3 ที่ติดตั้งอยู่แล้ว ไม่ติดตั้ง/อัปเกรด/ดาวน์เกรด** [SPEC]

เหตุผล:
1. Unity **2020.3.48f1c1** เป็น IL2CPP → ML 0.7.x คือสายที่รองรับ IL2CPP + CoreCLR(net6) และ **โหลดอยู่จริงในเครื่องนี้แล้ว** (UnityExplorer + LongYinThai ทำงาน) — ไม่มีอะไรต้องพิสูจน์ซ้ำ [RUNTIME]
2. ML 0.7.3 พา **HarmonyX (`0Harmony.dll`) + `Il2CppInterop.HarmonySupport.dll`** มาให้แล้ว → ไม่ต้อง vendor Harmony เอง และไม่ต้องกลัวเวอร์ชันชนกับ LongYinThai ที่โหลด `0Harmony` ตัวเดียวกัน
3. การเปลี่ยนเวอร์ชัน loader = ต้อง regenerate `Il2CppAssemblies/` (90 dll) + เสี่ยงพัง UniverseLib ที่ UnityExplorer พึ่งอยู่ และเสี่ยงพัง LongYinThai → ผลตอบแทนไม่คุ้มกับความเสี่ยงชัดเจน
4. ML ประกาศเองว่าใช้ **Unity Dependencies 2020.3.48** → interop ที่ generate ตรงกับ Unity ของเกม (f1c1) ไม่ต้องไปหาเวอร์ชันอื่นมาชน [RUNTIME]
5. ข้อจำกัดที่ยอมรับ: 0.7.x ยัง beta → pref API อาจเปลี่ยน; แผนนี้จึงห่อ pref ทั้งหมดไว้ใน `CoopPrefs` ชั้นเดียว (แก้ที่เดียวถ้า API เปลี่ยน)

**Contingency (ไม่ทำตอนนี้):** ถ้า Harmony patch บน IL2CPP ล้มเหลวเชิงระบบ (ไม่ใช่ราย method) → ทางเลือกคือ ML 0.7.4+ สายเดียวกัน (layout เดิม, ไม่ต้องย้าย net6) เท่านั้น; **ห้ามกระโดดไป 0.6.x** เพราะจะเปลี่ยนทั้ง `net6/`→`net35/` และ interop generator

**VERSION.md (บังคับทุก session) [v2]:** บันทึก SHA-256 ของ `GameAssembly.dll` และ `global-metadata.dat` แล้วเทียบทุกครั้งที่เริ่มงาน — ถ้าไม่ตรงให้หยุดและรายงาน (เกมอัปเดต = anchor ทั้งหมดเชื่อไม่ได้)
**แนะนำตั้ง Steam เป็น "Only update this game when I launch it" [v2]** เพื่อไม่ให้เกมแพตช์กลาง session

---

## 1. กติการ่วม

### 1.1 ตารางสิทธิ์การเขียน — stage ไหนเริ่มเขียนได้

คอลัมน์เรียงตาม **ลำดับที่รันจริง** (S4 ถูกเลื่อนไปท้ายสุดตาม D1/D2)

| การกระทำ | S0 | S1 | S1.5 | S2 | S3 | S5 | S4 (opt-in) | S6 |
|---|---|---|---|---|---|---|---|---|
| อ่าน field ผ่าน Il2CppInterop แบบมีชนิด [v2] | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| อ่านด้วย raw offset/pointer [v2] | ❌ | ขออนุมัติก่อน | ขออนุมัติก่อน | ขออนุมัติก่อน | ขออนุมัติก่อน | ขออนุมัติก่อน | ขออนุมัติก่อน | ❌ |
| เรียก getter บริสุทธิ์ (ไม่เปลี่ยน state) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Harmony patch (อ่าน argument เท่านั้น) | ✅ **probe** (ติดแล้วถอดทิ้ง) | ✅ ถาวร | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| เรียก method ที่เปลี่ยน state ของเกม | ❌ | ❌ | ✅ (host, เฉพาะ move/attack ของยูนิตที่ถึงตา) | ❌ | ✅ (host) | ✅ | ✅ | ❌ |
| เขียน field ของเกมเอง | ❌ | ❌ | ❌ | ❌ | ✅ (host, เฉพาะ battle) | ✅ | ✅ (guest mirror world) | ❌ |
| รับ input จากเครือข่ายแล้วรัน | ❌ | ❌ | ❌ (ไม่มีเครือข่าย) | ❌ | ✅ | ✅ | ✅ | ❌ |
| เรียก `GameDataController.Save/Load` | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (เฉพาะสำเนา) | ❌ | ❌ |
| เขียนไฟล์ (log/pref/capture ของ mod เอง) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Steam / online services | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

**ขอบเขตของแถว raw offset [v2]:** บังคับกับ **โค้ดของ mod ที่ ship** เท่านั้น — การสำรวจตอนทำ JIT (Frida / IL2CPP MCP, memory scan, Il2CppDumper, disasm) ทำได้ตามปกติโดยไม่ต้องขออนุมัติ เพราะเป็นการอ่านชั่วคราวเพื่อหา anchor; เมื่อได้ anchor แล้ว โค้ดจริงต้อง re-resolve ผ่าน Il2CppInterop แบบมีชนิด หรือขออนุมัติก่อนใช้ raw pointer
**หมายเหตุ [v2]:** offset/RVA ใน notes (`battle-notes.md`, `worldmap-notes.md`, §13) ใช้สำหรับสำรวจเท่านั้น ในโค้ด mod ต้อง re-resolve ผ่าน interop

**S0 กับ Harmony:** อนุญาตเฉพาะ **probe** — ติด patch ชั่วคราวเพื่อตอบคำถามเดียวว่า "patch บน IL2CPP build นี้ทำงานไหม/ถูกเรียกจริงไหม" แล้ว **unpatch ทิ้ง** ในโปรเซสเดียวกัน; ห้ามแก้ค่า argument และห้ามใช้ probe เป็น hook ถาวร (hook ถาวรเริ่มที่ S1)
**S1.5 (S3-spike)** คือ stage แรกที่เรียก method ที่เปลี่ยน state ของเกม — host เท่านั้น, ไม่มีเครือข่าย, บนเซฟสำเนา
**S3** คือ stage แรกที่รับคำสั่งจากเครือข่ายแล้วรัน (ผ่าน validator)
**S4** เป็น opt-in และทำหลัง S5 — เป็น stage แรกที่ guest เขียน state ของเกมตัวเอง (mirror world), ห้ามเขียนอะไรบน host

### 1.2 โปรโตคอลสำเนาเซฟ (ทำก่อนทุกครั้งที่ทดสอบ)

1. ปิดเกมให้สนิท (ตรวจ `LongYinLiZhiZhuan.exe` ไม่มีใน process list)
2. คัดลอกโฟลเดอร์เซฟทั้งก้อน → `coop-test/<timestamp>/save/` (ไม่คัดลอกทีละไฟล์)
3. บันทึก hash: `find save -type f -exec sha256sum {} + | sort > savehash.before.txt` — เก็บทั้งของเซฟจริงและสำเนา
4. ทดสอบโดยชี้ path เซฟไปที่สำเนา (S0 ค้นหา `GameDataController.saveDataPath` / `GetSaveDataPath` แบบอ่านอย่างเดียว แล้วเลือกวิธี redirect ที่ปลอดภัยที่สุด — ถ้า redirect ไม่ได้จริง ให้ใช้วิธี "สลับโฟลเดอร์เซฟเข้า-ออก" พร้อม hash ยืนยัน)
5. จบการทดสอบ: `sha256sum` เทียบ `savehash.before.txt` ของ **เซฟจริง** ต้องไม่เปลี่ยน
6. **ถ้า hash ของเซฟจริงเปลี่ยน ให้หยุดทุกอย่างแล้วรายงาน** [v2] — ยกเว้น `PlayerprefData.dat` (volatile, พิสูจน์ด้วย control test ตาม §2 ข้อ 5); ถ้าไฟล์ใน `SaveSlot*` เปลี่ยนแม้แต่ไฟล์เดียว ให้หยุดทันที

**ห้าม**: ทดสอบบนเซฟจริงแม้แต่ครั้งเดียว, ห้าม `Save()` ทับเซฟจริง, ห้ามให้ guest เขียนเซฟ (guest ต้องไม่มีสิทธิ์ save — บังคับด้วย pref + hook `Save` ให้ no-op ในโหมด guest หลังยืนยันว่าไม่ทำให้เกมพัง — ดู §7)

### 1.3 โครงโปรเจค [v2]

- **git repo แยกนอกโฟลเดอร์เกม** — build แล้ว copy `LongYinCoop.dll` เข้า `Mods\` (โฟลเดอร์เกมมีแค่ DLL + config/log ของ mod)
- `.gitignore`: DLL ของเกม, `Il2CppAssemblies`, `Il2CppDumper`, `dump.cs` — **ห้าม commit**
- C# class library target `net6.0` อ้างอิงแบบ `Private=false`:
  `MelonLoader\net6\MelonLoader.dll`, `0Harmony.dll`, `Il2CppInterop.Runtime.dll`, `Il2CppInterop.HarmonySupport.dll`,
  `MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll`, `UnityEngine.CoreModule.dll`, `Il2Cppmscorlib.dll`
- output: `Mods/LongYinCoop.dll` (+ `.pdb`); source อยู่ที่ `coop/LongYinCoop/`
- **ห้ามอ้างอิง Steamworks** (ดู §1.7)
- `VERSION.md` — SHA-256 ของ `GameAssembly.dll` + `global-metadata.dat` เทียบทุก session (ดู §0A.2)
- แนะนำตั้ง Steam เป็น "Only update this game when I launch it"

### 1.4 Threading model (invariant เดียวที่ห้ามพัง)

1. **ทุกการเรียก IL2CPP ต้องอยู่บน Unity main thread** — network thread ห้ามแตะ object ของเกมแม้แต่อ่าน field [SPEC; เหตุผล: IL2CPP domain/GC ไม่ปลอดภัยข้ามเธรด]
2. entry point เดียว: `MainThreadDispatcher.Enqueue(Action)` → drain ใน `MelonMod.OnUpdate` (S0 จะ **วัดจริง** ว่า OnUpdate อยู่ main thread ด้วย `Thread.ManagedThreadId` เทียบกับ **Harmony probe** บน `GameController.Update` — probe นี้ได้รับอนุญาตตาม §1.1 แล้ว, ไม่ขัดกับ S0; ถ้าไม่ใช่ main thread จะย้ายไป `MelonEvents.OnUpdate` / postfix บน `GameController.Update`)
3. drain มี **budget 4 ms/frame**; เกินแล้วเลื่อน remainder ไปเฟรมถัดไป (ห้ามทำให้เกมกระตุกเพราะ mod)
4. snapshot ถูกสร้างเป็น **plain struct/POCO บน main thread** แล้วค่อย serialize บน thread อื่น (ห้ามถือ `Il2CppObjectBase` ข้ามเฟรม)
5. ทุก command ถูกห่อ try/catch ต่อรายการ, นับ error, ไม่ให้รายการเดียวฆ่าเฟรม
6. dev build มี `MainThreadGuard`: ถ้าเรียก API เกมนอก main thread → throw + log stack (fail fast ตอนพัฒนา, ปิดใน release)
7. **S0 gate เรื่อง thread [v2]:** ต้องวัดว่า `OnUpdate` กับ Harmony probe อยู่ thread เดียวกัน — ถ้าไม่ตรง **ผ่านได้เฉพาะเมื่อมีทางแก้ที่ทดสอบแล้วในเซสชันเดียวกัน** (เช่น ย้ายไป `MelonEvents.OnUpdate` หรือ postfix บน `GameController.Update`) พร้อมหลักฐานใน log; ถ้ามีแต่ข้อสังเกตยังไม่มีทางแก้ = S0 ยังไม่ผ่าน

### 1.5 Rollback mechanism (ทุก stage มีเหมือนกัน)

| ระดับ | วิธี | ผล |
|---|---|---|
| ปิดทั้ง mod | เปลี่ยนชื่อ `Mods/LongYinCoop.dll` → `.dll.off` (ML ข้ามไฟล์ที่ไม่ใช่ `.dll`) | เกมกลับเป็น vanilla 100% |
| ปิดเฉพาะฟีเจอร์ | pref `coop.enabled=false` / `coop.allowRemoteInput=false` / `coop.capture=false` / `coop.follow=false` → early-return ทุก path (MelonPreferences = kill switch) | mod โหลดแต่ไม่ทำอะไร |
| ปิด hook | postfix เช็ค pref ก่อนทำงาน (ยังไม่ยืนยันว่า unpatch ตอนรันเสถียรบน IL2CPP — S0 probe จะทดสอบ; ถ้าเสถียรให้ใช้เป็นวิธีหลักแทน flag) | hook ยังติดแต่เป็น no-op |
| คืนเซฟ | ก๊อป `coop-test/<ts>/save/` กลับ + เทียบ hash | เซฟกลับสภาพเดิม |
| คืนโค้ด | ทุก stage จบด้วย `git tag stage-SN` | checkout กลับได้ทันที |

pref ทุกตัวอยู่ใน `UserData/MelonPreferences.cfg` ภายใต้หมวด `[LongYinCoop]`

### 1.6 ขอบเขตเครือข่าย / IPC

- IPC **ภายในเครื่อง** = named pipe เท่านั้น
- โหมดข้ามเครื่อง = **TCP บน IP ของ interface VPN (ZeroTier/Tailscale)** เท่านั้น ตาม D6 — bind เฉพาะ IP นั้น, ต้องเปิดด้วย pref + แสดง IP ให้เห็นก่อนเชื่อม
- **ข้อยกเว้น (ตัดสินแล้ว):** viewer transport ของ S2 รันในเครื่องเดียวกันเพื่อทดสอบ/วัด ได้บน `127.0.0.1` — ไม่ใช่โหมดเล่นจริง และไม่ใช่ช่อง IPC/ควบคุมของ mod (ช่องนั้นยังเป็น named pipe เท่านั้น)
- **ไม่มี** UPnP / relay / STUN / NAT traversal / telemetry; ไม่แตะ matchmaking ของเกม
- frame ใหญ่สุด **≤ 1100 B** (VPN บางเส้น MTU ต่ำกว่า 1500 — รายละเอียด §5)

### 1.7 Steam / online / cheat flag denylist

- mod ต้อง **ไม่ reference** `Steamworks` / `Steamworks.NET` / `Facepunch` ในโปรเจกต์เลย (ถ้า compile ผ่านโดยไม่มี assembly เหล่านั้น = หลักประกันชั้นแรก)
- runtime guard ใน `MainThreadDispatcher`: command ใดที่มีชื่อ type ขึ้นต้นด้วย `Steam`, `APIDispatcher`, `DnsChecker`, `PlayerStatsStored`, `Achievement*` → **ปฏิเสธทันที + log** (denylist อยู่ในโค้ด ไม่ใช่ config ที่แก้ตอนรันได้)
- ห้าม hook method ของ Steam/สถิติ: ถ้าจำเป็นต้องรู้ว่า unlock เกิดตอนไหน ให้ใช้ log file แทน hook
- ห้ามแตะ `WorldData.cheating/cheated`, `GameController.CheatCheck`, `PlayerPrefData` (เขียนแล้วอาจทำให้เซฟติดธง)

---

## 2. S0 — ฐานราก

**เป้าหมาย:** mod เปล่าที่โหลดได้ + logging + main-thread dispatcher + IPC อ่านอย่างเดียว **ยังไม่แตะ state เกม** (Harmony ใช้ได้เฉพาะ probe ตาม §1.1) [v2]

### Deliverable
- โปรเจกต์ `coop/LongYinCoop/` — `net6.0` classlib ตามโครงใน §1.3; output `Mods/LongYinCoop.dll` (+ `.pdb`)
- `CoopMod : MelonMod` — `OnInitializeMelon` log เวอร์ชัน/stage/role, ผูก pref, ลงทะเบียน dispatcher
- `MainThreadDispatcher` — queue + 4 ms budget + counters (`queued/ran/deferred/errors`), endpoint IPC `stats`
- `IpcServer` — **named pipe** `\\.\pipe\longyin-coop-<pid>` เป็นหลัก (ไม่ต้องเปิดพอร์ต ไม่มี firewall prompt) + `127.0.0.1` JSON-RPC (พอร์ตจาก pref, default 38999) เป็น debug ทางเลือก; framing = length-prefixed NDJSON, 1 connection ต่อ client, มี `hello/version`
- `SnapshotReader` (โครง) — อ่านอย่างเดียว: `WorldData.worldTime`/`hour`, `HeroData.bigMapPos`, scene name; ยังไม่ทำ battle
- `HarmonyProbe` — patch ชั่วคราวบน `GameController.Update` (prefix นับครั้ง + จำ `Thread.ManagedThreadId`) เพื่อตอบ 2 คำถาม: (ก) patch ติดจริงไหม (ข) OnUpdate อยู่ main thread ไหม → **ถอด patch ทิ้ง** หลังวัดเสร็จ (`UnpatchAll` หรือ `Unpatch(self)`; ถอดได้ด้วย pref ด้วย) และ log ผล
- `UIStrings` — string ที่ผู้ใช้เห็นทั้งหมดรวมที่เดียว (`coop/LongYinCoop/UIStrings.cs`, `static class` + key enum) ภาษา **อังกฤษก่อน**; ไฟล์เดียวเพื่อแปลไทยทีหลังโดยไม่แตะ logic
- CLI ทดสอบ `coop/tools/coopctl.py` — `stats`, `snapshot world`, `tail`
- `coop/README.md` — วิธี build/ติดตั้ง/ถอด
- Dev command `bench_dispatch` (pref `DevCommands`) — enqueue N no-op ผ่าน dispatcher แล้วรายงาน drain stats; ใช้ทดสอบเกณฑ์ข้อ 2 เท่านั้น ไม่แตะ state เกม
- `VERSION.md`, `.gitignore` ตาม §1.3 [v2]

### Just-in-time ที่ต้องสำรวจใน S0
1. **ยืนยันชื่อ managed type/field ใน interop assembly** ตรงกับ `dump.cs` (reflection enumerate `Assembly-CSharp.dll`: `WorldData.worldTime`, `TimeData.{year,month,day}`, `HeroData.bigMapPos`, `BigMapPos.{x,y}`) — กัน interop rename
2. **OnUpdate อยู่ main thread จริงไหม + Harmony ทำงานบน build นี้ไหม** — `HarmonyProbe` บน `GameController.Update`: เทียบ thread id 300 เฟรม, นับ probe hit, ยืนยันว่า patch ติด/ถอดได้สะอาด (ถ้า probe ล้มเหลว = หยุดแผนทันที เพราะ S1–S5 พึ่ง Harmony) [v2]
3. **save path จริง** — `GameDataController.saveDataPath` / `GetSaveDataPath(int,int)` (RVA อ้างใน notes §13) → ยืนยันกับโฟลเดอร์บนดิสก์ แล้วเขียน `coop-test/` protocol ให้ใช้ได้จริง
4. **ต้นทุน baseline** — dispatcher ว่าง + 1,000 no-op command วัด p50/p99 ต่อเฟรม ทั้งบน world map และในศึก
5. **pref/assembly resolution** — ตรวจว่า ML โหลด interop dll ของเราได้ (probe path) และ `MelonPreferences` 0.7.3 API ตรง
6. **รายการ method ที่ LongYinThai patch ไว้** — `Harmony.GetAllPatchedMethods` (อ่านอย่างเดียว) บันทึกลง `compat-notes.md` โดยเฉพาะ `AddInfoText` และ setter ของ `Text` [v2]

### Exit criteria (วัดได้)
1. `Latest.log` มี `[LongYinCoop] S0 loaded role=... stage=0` และ **0 exception ใน 10 นาที**
2. dispatcher: `bench_dispatch 1000` → 0 dropped, drain p99 ≤ 4 ms/frame, ไม่มีเฟรมที่ mod ใช้เกิน 4 ms ติดกัน > 3 เฟรม
3. IPC (**แยกสองชั้น + อิงเฟรม — ตัดสินหลังวัดจริง**): รายงาน frame time ของฉากนั้น (p50/p95/p99) กำกับทุกครั้ง แล้วตัดสินด้วยเกณฑ์อิงเฟรม
   - (ก) คำสั่งที่ไม่แตะ IL2CPP (`ping`, `stats`) 1,000 round-trip → **p99 ≤ 1 × frame p95** ของฉาก
   - (ข) คำสั่งที่แตะ IL2CPP ผ่าน dispatcher (`get_scene`, `get_player_pos`) 1,000 ครั้ง → **p99 ≤ frame p99 + 10 ms**
   - เหตุผล: ทุกคำสั่ง IL2CPP ต้องรอ `OnUpdate` รอบถัดไป จึงมีพื้นล่าง ≥ 1 frame และเกมรันคนละเฟรมเรตในแต่ละฉาก; ตัวเลขคงที่ (5/25 ms) สูงหรือต่ำกว่าข้อจำกัดจริงโดยไม่สัมพันธ์กับภาระฉาก
   - ค่าวัด 2026-10-20: TitleScene frame 6.906/11.185/24.429 ms → (ก) 4.25 ≤ 11.19 ✓, (ข) 17.66 ≤ 34.43 ✓; MainGame frame 6.906/14.680/27.679 ms → (ก) 8.62 ≤ 14.68 ✓, (ข) 33.63/35.62 ≤ 37.68 ✓
   - ต้องรายงานตัวเลขสองชุดแยกกัน, 0 malformed, 0 leak (pipe ปิดแล้วเปิดใหม่ 20 รอบไม่มี fd ค้าง)
4. `snapshot world` ตรงกับที่อ่านผ่าน Frida (tolerance 1e-3) อย่างน้อย 3 ครั้ง ห่างกัน ≥ 1 นาที
5. **ไม่มีการเขียน state:** `savehash.before == after` ของเซฟจริง และเซฟสำเนา — **ยกเว้น `LongYinLiZhiZhuan_Data/Save/PlayerprefData.dat`** ซึ่งเกมเขียนเองทุกครั้งที่เปิด/ปิด (พิสูจน์ด้วย control test `Enabled=false` 2 ครั้ง: `0c8aef57…`→`1b8ad58a…` และ `1b8ad58a…`→`0ebc1769…` โดย mod ไม่ทำงานเลย; ไฟล์ `SaveSlot*` ไม่เปลี่ยน); code review checklist: 0 การเรียก method ที่ไม่ใช่ getter
6. **Harmony probe ทำงาน:** probe hit > 0 ใน 60 วิ, หลัง unpatch ไม่มี probe hit เพิ่ม (0 ครั้ง) และเกมเดินปกติ (เรื่อง thread ดูข้อ 12)
7. `UIStrings.cs` มีอยู่จริงและทุกข้อความที่ mod log/แสดงดึงจากมัน (grep: ไม่มี user-facing literal นอก `UIStrings.cs`) และทุกคีย์มีคำแปลอังกฤษ
8. ถอด mod (`.off`) แล้วเกมบูตปกติ ไม่มี error ใหม่ใน log
9. เล่น world map ได้ปกติ และเล่นศึกจบได้ 1 ศึก [v2]
10. LongYinThai ยังแปลได้ตามเดิม [v2]
11. rename เป็น `.dll.off` แล้วเกมกลับเหมือนเดิม (`Latest.log` ไม่มีชื่อ mod เรา) [v2]
12. OnUpdate กับ probe อยู่ thread เดียวกัน — หรือถ้าไม่ตรง ต้องแนบ**ทางแก้ที่ทดสอบแล้ว + หลักฐาน log** (ดู §1.4 ข้อ 7) [v2]

### Carry-over จาก S0 (บันทึกหลัง tag `stage-S0`, 2026-10-20)

สองข้อนี้ **ผ่านเกณฑ์แล้ว** แต่หลักฐานยังไม่ครอบเคสจริง — ไม่บล็อก gate แต่ต้องเก็บให้ครบใน stage ที่เกี่ยวข้อง ห้ามถือว่าปิดแล้ว:

| จาก | สิ่งที่ยังไม่ครอบ | ไปเก็บที่ |
|---|---|---|
| S0 ข้อ 4 | วัด `snapshot world` เทียบ Frida 3 ครั้ง **ตอนปาร์ตี้เดินอยู่บน BigMap** — ที่ทำได้จริงคือตอนอยู่นิ่งใน MainGame ค่าเดียว (3/3 ตรงกันแต่เป็นชุดเดียว) | **S4** JIT ข้อ 1 |
| S0 ข้อ 9 (ครึ่งศึก) | log ไม่มี **marker ของศึก** — S0 ยืนยันได้แค่ว่าผู้ใช้สู้หลายศึกต่อเนื่อง 11 นาทีโดยไม่มี error ไม่ได้ยืนยันเชิงโครงสร้างว่าเข้าศึก/จบศึกจริง | **S1** JIT ข้อ 7 (ทำเป็น marker ตั้งแต่ต้น เพื่อไม่ให้ S3/S5 เจอปัญหาเดียวกัน) |

### ความเสี่ยง
- ML 0.7.3 beta: API `MelonPreferences`/`OnUpdate` อาจต่างจาก doc → แก้ที่ชั้น `CoopPrefs` เดียว
- Harmony probe อาจติดไม่ได้บน IL2CPP (patch ต้องการ `Il2CppInterop.HarmonySupport` ที่โหลดถูกต้อง) → เป็นความเสี่ยงที่ต้องเคลียร์ใน S0 ไม่ใช่ S1; ถ้า probe ติดไม่ได้เลย = หยุดแผน
- named pipe ACL / Windows path escaping → ใช้ชื่อ pipe ที่มี pid กันชน, ทดสอบ 2 instance พร้อมกัน
- net6 assembly probing: ถ้า `Il2CppInterop.Runtime` โหลดไม่ขึ้น ให้ใส่ใน `Mods/` แทน `UserLibs/` (ทดสอบทั้งสองแบบ)
- probe ชนกับ LongYinThai (patch order) → ตรวจใน JIT ข้อ 6 [v2]

### Rollback
ลบ/เปลี่ยนชื่อ dll — ไม่มี state ค้าง เพราะ S0 ไม่เขียนอะไร

---

## 3. S1 — สังเกตการณ์ฝั่ง host

**เป้าหมาย:** snapshot ต่อเฟรม (world + battle) และ **battle log ที่ครบถ้วน** ผ่าน Harmony postfix ที่ `AddInfoText`; ยังอ่านอย่างเดียว

### Deliverable
- `WorldSnapshot` (10 Hz, ปรับได้): `{t, frame, worldTime{year,month,day,hour,difficulty}, scene, partyPos{x,y,areaId}, npcs[]{id,x,y,distance,isFightIcon}, resources{money,fame,badFame,force,contribution,heroNum,areaNum}}`
  - **[2026-10-20]** snapshot เต็ม (มี `npcs`) ยังสร้างที่ 10 Hz ในหน่วยความจำ + ส่งผ่าน IPC `snapshot world`; **stream ลงดิสก์ตัด `npcs` ออก** และเขียนที่ `WorldStreamHz` (default 2 Hz) — เหตุผล: 11.4 KB/record = 6.6 MB/นาที
- `BattleSnapshot` (60 Hz): `{battleType, turnIndex, phase, battlePaused, nowActiveUnit{stableId,heroId,side,cell{x,y},hp,maxHp,autoFight}, units[]{...}, gridW,gridH}`
- `BattleLogHook` — Harmony **postfix** บน `BattleController.AddInfoText(string, bool)` ทุก overload: อ่าน argument, **ไม่แก้ค่า**, ประกอบ `{seq, frame, text, infoCount}`; `seq` = counter ของ mod, `infoCount` = ค่าจากเกมเพื่อคำนวณ **capture rate**
- Ring buffer: log 4,096 events, battle snapshot 900 เฟรม (capacity คงที่, ทับของเก่า)
- IPC: `snapshot world|battle|log`, `capture start|stop` → เขียน NDJSON ที่ `UserData/CoopCapture/<ts>/`
- `dropped/gap` counter: ถ้า `ΔinfoCount > captured` → log warning (คือตัววัดความครบ)

### Just-in-time ที่ต้องสำรวจใน S1
1. **offset ของ `infoCount`** — ถ้าไม่มีใน dump ให้หาแบบ runtime (gc scan หา `BattleController`) แล้วบันทึกเป็น anchor ใหม่ (การสำรวจทำได้ตาม §1.1; โค้ดต้อง re-resolve)
2. **มีกี่ overload ของ `AddInfoText`** — patch ให้ครบ, log `PatchApplied` ต่อ overload; ถ้าตัวใด patch ไม่ติด (inline/เล็กเกิน) → fallback polling `Text.m_Text` (offset 0xE0) + diff
3. **แหล่งความจริงของ HP/ตำแหน่ง** — `GridUnitData` vs `BattleUnit` vs UI: ยืนยันว่าค่าที่อ่านตรงกับตัวเลขบนจอใน 20 เฟรมสุ่ม
4. **ตัวตนถาวรของ unit** (จำเป็นต่อ S2 delta) — pointer ถูกใช้ซ้ำไหม, `heroId + side + index` พอไหม
5. **ต้นทุน hook** — วัด ns/call ของ postfix และจำนวน call/วินาทีในศึกใหญ่ (30 vs 60 units)
6. **ลำดับ postfix ของเราเทียบกับ LongYinThai** — ข้อความที่ได้เป็นก่อนหรือหลังแปลไทย [v2]
7. **marker ของศึกใน log (carry-over จาก S0 ข้อ 9)** — S0 ไม่มีทางยืนยันจาก log ว่าเข้าศึก/จบศึกจริง ต้องมี marker ตั้งแต่ S1: hook จุดเริ่ม/จบศึก (`PrepareBattleMap` เข้า, ผลศึกออก) แล้ว log `battle: start|end battleType=… units=… turn=…` เพื่อให้ทุก stage หลังจากนี้ตรวจย้อนหลังได้โดยไม่ต้องพึ่งคำบอกเล่า; ใช้ marker นี้เป็นตัวนับ "1 ศึกเต็ม" ของ exit criteria S1 ด้วย

### Exit criteria (วัดได้)
- 10 นาทีบน world map: 0 exception, main-thread cost ของ mod p99 ≤ **1.5 ms**
- 1 ศึกเต็ม: **capture rate ≥ 99%** (`captured / ΣΔinfoCount`), 0 duplicate `seq`, ลำดับ event ตรงกับลำดับในเกม (ยืนยันด้วยการเทียบ 30 บรรทัดกับ UI)
- battle snapshot ตรงกับ UI: สุ่ม 20 เฟรม ตรวจ `nowActiveUnit`, HP 3 units, cell — ต้องตรง 20/20
- memory: managed heap โต ≤ 20 MB ใน 10 นาที, ring buffer ไม่โตเกิน capacity
- ปิด `coop.capture=false` กลางศึก → ไม่มี event ใหม่, เกมเดินต่อปกติ, ไม่มี exception

**หมายเหตุจากการวัดจริง (2026-10-20 · 21 ศึก · `evidence/S1-20261020-113926/battle-coverage-21.md`):**
`BattleController.infoCount` **ไม่ใช่ตัวนับสะสม** — เพดานที่ **101** (max ทั้งไฟล์ 101 · 615 event มี `infoDelta=0`)
ดังนั้น `captured / ΣΔinfoCount` ให้ค่า >100% ทุกศึก (วัดได้ 173.73%) และใช้เป็นตัววัดความครบไม่ได้

นิยามที่ใช้จริง (ผู้ใช้ยืนยัน 2026-10-20): **counted-only** = จำนวน event ที่ `infoDelta == 1` ÷ ΣΔinfoCount
→ **100.00% (1245/1245 ตรงกันทุกศึก 21/21)**; กลุ่ม `infoDelta == 0` (popup `[毒素3]` ฯลฯ) **ยังไม่มีตัวหารอิสระ**
จึงยังไม่ยืนยันความครบของกลุ่มนี้ — ต้องเขียนเป็นข้อจำกัดในรายงานปิด S1 (ห้ามเขียนว่าครบ 100% ทั้งหมด)

### ความเสี่ยง
- hook บน method ที่ถูก inline → patch ติดแต่ไม่ถูกเรียก (ตรวจด้วย capture rate, ไม่ใช่แค่ `PatchApplied`)
- allocation ต่อ event (string ซ้ำทุกครั้ง) → ใช้ `StringBuilder`/intern + ปล่อยเป็น struct
- hook ชนกับ LongYinThai ที่ patch TMP: ข้อความใน log อาจเป็น**ก่อน**แปลไทย (ไม่ใช่บั๊ก แต่ต้องบันทึกไว้ใน README)

### Rollback
`coop.capture=false` (hook ยังติดแต่ no-op) → ถ้าต้องถอดจริงให้ `.dll.off`

---

## 4. S1.5 — S3-spike (ไม่มีเครือข่าย)

**เป้าหมาย:** พิสูจน์ว่า **ปลั๊กอินสามารถสั่งยูนิตที่ "ถึงตา" ให้เดิน/โจมตี ผ่าน method ของเกมได้จริง** ก่อนลงทุนกับเครือข่าย (S2) และช่องคำสั่ง (S3) — เป็น stage แรกที่เรียก method ที่เปลี่ยน state ตาม §1.1

**ทำไมต้องมี:** ถ้าสั่งยูนิตไม่ได้ หรือสั่งแล้ว state machine พัง ทั้ง S3 ก็ไร้ความหมาย; spike นี้แยกความเสี่ยง "เรียก method ผิด" ออกจากความเสี่ยง "เครือข่าย/validator" และราคาถูกกว่ามาก (ไม่มี transport, ไม่มี guest)

### Deliverable
- `UnitCommandSpike` — คำสั่งเดียวผ่าน IPC/คีย์ลัด: `spike move <unitStableId> <cellX> <cellY>` และ `spike attack <unitStableId> <targetStableId>`
- `TurnGate` — ตรวจจากเกมเองว่า "ตอนนี้ถึงตายูนิตนี้จริง" (อ่าน `nowActiveUnit` + phase) ก่อนอนุญาต; ไม่ถึงตา → ปฏิเสธ (นี่คือ proto ของ validator ใน S3)
- `Executor` เวอร์ชันแรก — ลำดับ call ที่ได้จาก JIT ข้อ 1 (ตั้ง active unit / selected grid → เรียก entry method ของเกม) ผ่าน `MainThreadDispatcher`
- `Audit` — log: unit, target, ก่อน/หลัง (cell, hp), frame ที่ apply, ระยะเวลา accept→applied
- `UserData/CoopSpike.md` — บันทึกลำดับ call ที่ใช้ได้จริง → กลายเป็นสเปกของ S3

### Just-in-time ที่ต้องสำรวจ (ทั้งหมดบนเซฟสำเนา)
1. **ลำดับ call ของ UI จริง** — disasm `BattleController.BattleGridClicked` / `MoveButtonClicked` / `ChangeActiveSkill` / `RegenerateMovePath` / `attackComfirmed` แล้วเขียนลำดับที่แอปเรียก; ทดสอบทีละขั้น (หยุดกลางทางได้)
2. **ต้องตั้ง `nowActiveState`/field ไหนก่อน** — enumerate ค่า enum ตอนเล่นจริง เทียบกับ phase ที่เห็น; ห้าม hardcode
3. **ต้องมีแอนิเมชัน/หน่วงเวลาไหม** — ถ้าเรียกทันทีหลังตั้ง state แล้วเกมข้าม/ค้าง ให้หา "จุดที่ปลอดภัย" (เช่น postfix ของ `Update` หลัง state เปลี่ยน)
4. **เรียกซ้ำ/ผิดลำดับแล้วเกิดอะไร** — วัดว่าเกม recover ได้หรือค้าง (ข้อมูลนี้คือพื้นฐานของ panic switch)
5. **auto/AI แทนชั่วคราว** — `SetAllAuto` / `autoFight` scope และวิธีคืนค่า (S3 ใช้ตอน timeout)
6. **เกมหยุดรอ input จริงไหมเมื่อยูนิตผู้เล่นถึงตา** — ทดสอบทั้งตอนเร่งเวลาและไม่เร่ง [v2]
7. **พฤติกรรมของ `battleTime` / `GetBattleDeltaTime` / `battleMove` / `ManageHeroSkillCdTime` ระหว่างรอ** — เพื่อตอบว่าศึกเป็นสลับเทิร์นล้วน หรือมีเวลาเดินแบบ ATB [v2]
8. **`battlePaused`** — หยุดศึกได้โดยไม่ทำให้ค้าง และปลดแล้วเล่นต่อได้ [v2]

### Exit criteria (วัดได้)
1. `spike move` สำเร็จ **10/10** บนยูนิตที่ถึงตา (ยูนิตไปถึง cell ที่สั่ง, hp ไม่เปลี่ยน, 0 exception)
2. `spike attack` สำเร็จ **10/10** (target hp ลดตามที่เกมคำนวณ, mod ไม่คำนวณ dmg เองเลย)
3. **ปฏิเสธถูกต้อง 20/20** เมื่อสั่งยูนิตที่ไม่ถึงตา / ยูนิตฝั่งตรงข้าม / target ตายแล้ว / cell นอก range — state hash ก่อน/หลังไม่เปลี่ยน
4. **ยืนยันว่าเกมหยุดรอ input ตอนถึงตายูนิตผู้เล่น** ทั้งเร่งและไม่เร่งเวลา — หรือรายงานว่าไม่หยุด พร้อมผลกระทบต่อ S3 [v2]
5. `battlePaused` เปิด/ปิดได้ **10/10** โดยไม่ค้าง [v2]
6. เกมไม่ค้างหลังคำสั่ง: 10 นาทีเล่นต่อได้ปกติ, 0 soft-lock, 0 exception
7. main-thread cost ต่อคำสั่ง p99 ≤ 8 ms (ไม่รวมเวลาแอนิเมชัน)
8. **เซฟจริงไม่ถูกแตะ:** hash ก่อน/หลังเท่ากัน; ไม่มี `Save/Load` ถูกเรียก (log + code review)
9. panic switch: `coop.allowRemoteInput=false` ระหว่าง spike → คำสั่งถัดไปถูกปฏิเสธทันที และ UI ปกติยังใช้ได้

### ความเสี่ยง
- **เรียก method ผิดลำดับ → soft-lock** (สูงสุดของทั้งแผน) → ทำทีละขั้น, มี panic switch, เซฟสำเนาเท่านั้น
- state machine อาจผูกกับ UI ที่ถูกสร้างจริง (ทดสอบจาก CLI โดยไม่มีหน้าจออาจพัง) → ต้องเล่นผ่าน UI จริง ไม่ใช่ headless
- IL2CPP marshalling ของ struct/enum ที่ method ต้องการ → ถ้า wrapper ไม่ตรง ให้ใช้ `IntPtr` + offset
- **ศึกไม่ใช่เทิร์นล้วน (ATB)** → ต้องออกแบบ S3 ใหม่; พิสูจน์ใน JIT ข้อ 6–8 ก่อนสร้างเครือข่าย [v2]

### Rollback
`coop.allowRemoteInput=false` (ปฏิเสธทุกคำสั่ง) → `.dll.off` → คืนเซฟจากสำเนา; spike ไม่แตะ world/clock เลย จึงย้อนง่ายกว่า S4 มาก

---

## 5. S2 — Spectator

**เป้าหมาย:** transport เครือข่าย + ตัวแสดงผลฝั่ง guest (viewer) เห็นศึก/แผนที่ของ host — **ยังไม่มีคำสั่งจาก guest**

**ขอบเขตตาม D1:** guest ใน S2–S3 เป็น **viewer overlay** เท่านั้น — ไม่เข้า battle scene, ไม่ mirror world (การเป็นเกมอินสแตนซ์ที่ตาม host = S4 opt-in ท้ายสุด)

### Deliverable
- Transport: TCP เท่านั้น (named pipe ใช้เฉพาะ IPC ภายในเครื่อง ไม่ข้ามเครื่องได้) — **รันบน VPN เสมือน LAN (ZeroTier/Tailscale) ตาม D6**: bind เฉพาะ IP ของ interface VPN, ค่าเริ่มต้น `127.0.0.1` **สำหรับทดสอบในเครื่องเท่านั้น** (§1.6); length-prefixed binary frame, custom compact writer (**ไม่เพิ่ม dependency**), มี `PROTO_VER`
- ข้อบังคับเพิ่มเติมเรื่อง VPN: ไม่ทำ NAT traversal/relay/stun เอง, ไม่พึ่ง service ของเกม, และตรวจ MTU/การแบ่ง frame (VPN บางเส้น MTU ต่ำกว่า 1500 → frame ใหญ่สุด ≤ 1100 B) (ดู §1.6); ต้องเปิดด้วย pref + แสดง IP ให้เห็นก่อนเชื่อม ตาม §1.6
- Protocol v1 (host → guest เท่านั้น): `HELLO{protoVer, modVer, gameVer, unityVer, fieldHash}`, `WORLD`, `BATTLE`, `LOG`, `PING/PONG`, `BYE`
  - framing: length-prefixed ต่อ message; message ที่ใหญ่กว่า 1100 B ต้องแบ่งเป็นหลาย frame และ reassembly ฝั่ง guest ได้ (ห้ามส่ง frame ใหญ่กว่า 1100 B ตาม §1.6)
- **รูปแบบการส่ง [v2]:**
  - **ศึก:** ส่ง event/delta ทุกครั้งที่ action จบ (ใครทำอะไร, ผล, HP/ตำแหน่งที่เปลี่ยน, log) — **ไม่ส่งตามรอบเวลา**
  - **world map:** ส่ง delta ของตำแหน่ง/เวลาเมื่อเปลี่ยน โดยจำกัดความถี่ไม่เกินที่จำเป็นต่อการแสดงผล (เพดาน 5 Hz)
  - **snapshot เต็ม + `fieldHash` เป็นระยะ** (ทุก N action หรือทุกไม่กี่วินาที — กำหนดจากผลวัดใน stage นี้) ไว้ตรวจ desync และให้ guest ที่เพิ่งเข้าตามทัน
  - ทุกข้อความมีเลข `seq` — ถ้าขาดให้ guest ขอ **snapshot เต็ม** ใหม่ (ไม่พยายามเดา)
- Guest side: `role=guest`, **ไม่แตะ state เกม**, แสดงผลด้วย IMGUI overlay (`OnGUI`) — grid, unit, HP bar, ชื่อ, log ล่าสุด N บรรทัด, สถานะการเชื่อมต่อ/ latency
- Display smoothing buffer 100 ms + `extrapolate=false` — เกลี่ยการแสดงผลระหว่าง event เท่านั้น แสดงค่าล่าสุดที่ยืนยันแล้ว ไม่เดาค่า
- Metrics: bytes/s, p50/p95 latency, จำนวน event ที่ขาด (นับจาก `seq`), จำนวนครั้งที่ขอ snapshot เต็ม, divergence hash ต่อ snapshot
- `coop/tools/viewer-sim.py` — replay capture file เข้า viewer โดยไม่ต้องมีเกม (ทดสอบ UI ได้เร็ว)

### Just-in-time ที่ต้องสำรวจใน S2
1. **stable identity ของ unit** (ต่อจาก S1 ข้อ 4) — ต้องได้ key ที่ไม่เปลี่ยนทั้งศึก ไม่งั้น delta พัง
2. **field ที่จำเป็นจริงสำหรับ viewer ที่ "อ่านรู้เรื่อง"** — ตัดให้เหลือน้อยสุด (ทดลองกับ 2 ศึก)
3. **bandwidth จริง** ในศึกเป้าหมาย **30 ยูนิตต่อฝั่ง (60 รวม)** ตาม D8 → วัดแบบ event-driven: bytes/action, bytes/นาที, burst สูงสุด; ตัดสินว่าจะทำ delta/quantize (float16) หรือไม่; วัดทั้ง loopback และผ่าน VPN
4. **overlay ไม่รบกวน input/เฟรมของ guest** — วัด `GUI.depth`, `Event.current` consumption, frame time delta
5. **guest ต้องอยู่ฉากไหน** — world map ก็ดู overlay ได้หรือต้องเข้า battle scene (ถ้าเข้า ต้องมี escape ที่ปลอดภัย)

### Exit criteria (วัดได้)
1. latency (**นิยาม:** host-apply → guest-store) **p95 ≤ 150 ms ผ่าน VPN (ZeroTier/Tailscale)**, ≤ 40 ms บน loopback — วัดที่ศึก **60 ยูนิต (30/ฝั่ง)** ตาม D8; ค่า host-apply → guest-display (หลัง smoothing buffer) **บันทึกแยกเป็นข้อมูล** และ smoothing ต้องไม่เพิ่มดีเลย์ของค่าล่าสุดเกิน 1 frame
2. bandwidth ≤ **30 KB/s** (เพดาน; คาดว่าเฉลี่ยต่ำกว่านี้มาก [v2]) — เพดานเดิมคงไว้ แต่ **วัดแบบ event-driven**: ค่าเฉลี่ยต่อเนื่องทั้งศึก 5 นาทีที่ 60 ยูนิต; burst ต่อ action บันทึกเป็นข้อมูล (ยังไม่ตั้งเพดานจนกว่าจะวัดจริง) [v2]; ถ้าเกิน → ต้อง quantize/delta ก่อนผ่าน stage
3. 5 นาทีในศึก: divergence ของ field ที่ replicate = **0** (hash guest store vs host store ทุก snapshot เต็ม)
4. `seq` ขาด → guest ขอ snapshot เต็มแล้วกลับมาตรงกันได้ (นับเป็น 0 divergence หลัง resync) [v2]
5. guest ปิด/แล็ก/หลุด → host ไม่กระตุก (frame time p99 ไม่เปลี่ยน > 1 ms), ไม่มี exception ฝั่ง host
6. guest **อ่านอย่างเดียว:** ไม่เรียก method ของเกม, ไม่มี write path (code review + dev build `WriteGuard` โยน exception ถ้ามี)

### ความเสี่ยง
- pointer reuse ทำให้ delta ผิด → ต้องมี `stableId` แล้วเท่านั้น
- float precision (map-space ระดับ 1e4) → ส่งเป็น float32 ตรง ๆ ก่อน, ค่อยพิจารณา quantize
- overlay ราคาแพงบน IMGUI → จำกัด redraw, ใช้ cached GUIStyle
- event หายระหว่างทาง → ต้องมี `seq` + snapshot resync (ไม่ใช่ retry เชิงเดา)

### Rollback
`role=off` / ปิด transport — ไม่มี state ให้คืน

---

## 6. S3 — ช่องคำสั่งในศึก (stage แรกที่รับคำสั่งจากเครือข่าย)

**เป้าหมาย:** guest สั่งยูนิตบางตัวของฝั่งเรา; host ตรวจแล้วรันผ่าน method ของเกมเอง, มี timeout → AI ทำแทน

**ฐานที่ได้จาก S1.5:** ลำดับ call ที่ใช้ได้จริง + `TurnGate` + panic switch มีแล้วใน S3-spike → S3 ต่อแค่ transport, validator ฉบับเต็ม, control map และ timeout

### Deliverable
- Protocol v2: `CMD{cmdId, guestId, unitStableId, op: MOVE|SKILL|TARGET|CANCEL|AUTO, args, clientTick, deadlineMs}` ↔ `CMD_RESULT{cmdId, ACCEPT|REJECT(reason)|TIMEOUT|APPLIED, appliedFrame}`
- **CommandValidator (host)** — ตรวจก่อนแตะเกม: เป็นตาของยูนิตนั้นจริง, side == player, ยูนิตมีชีวิต/ไม่ติดสถานะ, target อยู่ใน range, skill พร้อม/มี cost/ไม่ติด cooldown, phase == ช่วงรับคำสั่งของผู้เล่น, ไม่มี modal ค้าง — **ใช้ตัวตรวจของเกมเองก่อน** แล้วค่อยเสริมของเรา (กัน logic เพี้ยน)
- **กติกาตาม D3:** รับคำสั่งเฉพาะยูนิตที่ **ถึงตาตัวเองแล้ว** — ไม่รับคำสั่งล่วงหน้า (queue ข้ามเทิร์น), ไม่รับคำสั่งให้ยูนิตอื่นในเทิร์นเดียวกัน และไม่รับ "plan ล่วงหน้า" ทั้งหมด; คำสั่งที่มาช้าเกินกว่าตาของยูนิตนั้นจบ → `REJECT(STALE_TURN)`
- **CommandExecutor (host)** — ทำตามลำดับเดียวกับที่ UI ทำ: ตั้ง `nowActiveUnit`/`selectedGrid` → เรียก entry method ของเกม (move/attack) → เกมคำนวณ+dmg+RNG เองทั้งหมด
- **Timeout/AI fallback** — เกิน `deadlineMs` → โหมด auto เฉพาะยูนิตนั้นเฉพาะเทิร์นนั้น แล้วคืนค่ากลับ และคืนการควบคุม [v2]
- `UserData/CoopControl.json` — แมป `stableId → guestId` (hot reload ผ่าน IPC)
- Audit log ทุก command: `cmdId, op, validator verdict, applied frame, ผลหลังรัน (cell/hp)` + rejected reason code

### Just-in-time ที่ต้องสำรวจใน S3 (สำคัญสุดของแผน)
1. **ขยายผลจาก S1.5** — ลำดับ call ที่พิสูจน์แล้วใน `CoopSpike.md` ใช้ได้กับทุกกรณีหรือไม่: สกิล AoE, target หลายตัว, โจมตีไม่ถึง, ยูนิตที่ถูก stun → เก็บส่วนต่างเป็น validator rule (ไม่เดาลำดับใหม่)
2. **ต้องตั้ง `nowActiveState` เป็นค่าไหนก่อนเรียก** — enumerate enum value ตอนเล่นจริง (อ่าน field, เทียบกับ phase ที่เห็น)
3. **วิธี auto แทนผู้เล่นชั่วคราว** — `SetAllAuto` / `HeroAutoSettingType` / `BattleUnit.autoFight`: อันไหน scope แคบสุด, คืนค่าเดิมยังไง (ต้องคืนได้ 100%)
4. **ผลข้างเคียงต่อ animation/turn timer** — ถ้าเรียกเร็วเกิน เกมจะข้ามแอนิเมชัน/ค้างเติ่ง? วัดเวลา accept→applied
5. **ยืนยันว่า host ไม่ desync ตัวเอง** — หลัง apply อ่าน cell/hp ซ้ำแล้วส่ง delta ทันที (guest เห็นผลจริง ไม่ predict)

### Exit criteria (วัดได้)
- 50 คำสั่งจาก guest: ≥ 45 ACCEPT, ทุก REJECT มี reason code ที่ตรงกับเคสทดสอบ, host crash = 0
- **authority proof:** ยิงคำสั่งผิดกติกา 20 แบบ (นอกเทิร์น/ยูนิตคนอื่น/เกิน range/ยูนิตตาย/skill ไม่พร้อม) → ปฏิเสธ 20/20 และ state hash ก่อน/หลังไม่เปลี่ยน
- timeout: บังคับ 20 ครั้ง → AI ทำงาน 20/20, เทิร์นจบ, คืนสิทธิ์ผู้เล่นเทิร์นถัดไป 20/20
- main-thread cost ต่อคำสั่ง p99 ≤ 8 ms (ไม่รวมเวลาแอนิเมชันของเกม), ไม่มีเฟรมที่ mod ใช้เกิน budget
- guest ยังเขียน state เกม = 0
- 10 นาทีเล่นจริงด้วยกัน: 0 soft-lock, 0 turn ค้าง

### ความเสี่ยง
- **ใช้ state machine ผิด → soft-lock** (สูงสุดในแผน) → บังคับทดสอบบนเซฟสำเนา, มี "panic switch" `coop.allowRemoteInput=false` ที่ทำให้ host กลับมาใช้ UI ปกติทันที (ไม่ต้องรีสตาร์ท)
- แอนิเมชัน/timeScale ระหว่างรอผล → ถ้า guest เห็นช้า ให้ถือว่า acceptable ใน S3 (S5 ค่อยปรับ)
- หลายยูนิตต่อเทิร์น / turn order ที่ไม่ใช่ผู้เล่นนำ → validator ต้องอ่านจากเกม ไม่ hardcode

### Rollback
`coop.allowRemoteInput=false` + เคลียร์ control map → host เล่นเองได้เหมือนเดิมทันที; ถ้าแย่กว่านั้น `.dll.off`

---

## 7. S5 — ความทนทาน

**เป้าหมาย:** save/load, ศึกจบกลางคัน, หลุดการเชื่อมต่อ, เช็กเวอร์ชัน, ทดสอบอัตโนมัติ

**ลำดับ:** S5 รัน **ก่อน S4** (สลับจากแผนเดิมตาม D1/D2) เพราะ S4 ต้องใช้ fault harness, การกัน save ของ guest และ `FieldHash` ที่ S5 สร้าง — การทำ S4 ก่อนจะเป็นการทดสอบเรื่องเสี่ยงสุดด้วยเครื่องมือที่ยังไม่ครบ

### Deliverable
- `FieldHash` = SHA256 ของ (game build hash + Unity version + รายการ anchor offset/RVA ที่ใช้จริง + `PROTO_VER`) → handshake ปฏิเสธทันทีถ้าไม่ตรง พร้อมบอกว่าอะไรไม่ตรง; handshake เทียบ `VERSION.md` ด้วย (SHA-256 ของ `GameAssembly.dll` / `global-metadata.dat`) [v2]
- Reconnect: `sessionId` + keyframe resume; guest buffer flush; host เดินต่อไม่สะดุด
- หลุดกลางศึก: ยูนิตของ guest → AI (ใช้ path ของ S3), บันทึก audit, ไม่ค้างเทิร์น
- Save/Load detection: Harmony postfix `GameDataController.Save/Load` → `SESSION_RESET` ไป guest (guest ล้าง view) — **ห้ามบล็อกการ save ของ host**
- `coop/test/` — driver python: สตาร์ท host+guest บนสำเนา, ยิง scenario ผ่าน IPC, เก็บ log, ออกรายงาน pass/fail; + in-mod `--selftest` (serializer, validator, dispatcher, ring buffer; ไม่ต้องมี state เกม)
- Fault injection matrix: ตัด TCP, ระงับ process guest, frame เสีย, `cmdId` ซ้ำ, `clientTick` เก่า

### Just-in-time ที่ต้องสำรวจใน S5
1. **`GameDataController.Save/Load` signature จริง** (2 overload: `(int)` และอื่น) → patch ให้ถูกตัว, ยืนยันว่า postfix ไม่รบกวนการเขียนไฟล์
2. **guest จะไม่ save ได้อย่างไรโดยไม่ทำเกมพัง** — เรียงจากปลอดภัยสุด: (ก) ไม่เรียก save เองเลย (ข) redirect `saveDataPath` (ค) hook `Save` เป็น no-op — ทดสอบ (ค) บนสำเนาก่อนใช้
3. **ตรวจจับ handle leak ของ Il2CppInterop** — นับ handle ก่อน/หลัง 60 นาที
4. **พฤติกรรมตอน host โหลดเซฟระหว่างมี session** — อะไรพังก่อน (pointer ตายทั้งหมด) → กำหนดว่า `SESSION_RESET` ต้องล้างอะไรบ้าง

### Exit criteria (วัดได้)
- 3 scenario × 5 รอบ: **15/15 ผ่าน** (ไม่ crash, invariant ครบ: ไม่มี state ค้าง, ไม่มี turn ค้าง, ไม่มี desync ที่ replicate)
- fault matrix 5 แบบ: กู้คืน ≤ 5 s หรือ abort สะอาดพร้อมข้อความให้ผู้ใช้; **host crash = 0** ในทุกกรณี
- version mismatch: ปฏิเสธ ≤ 1 s, ข้อความบอกสาเหตุ, ไม่มี session ค้างครึ่ง ๆ (ตรวจทั้ง `fieldHash` และ `VERSION.md`)
- save integrity: hash ของเซฟจริงไม่เปลี่ยนตลอดทั้งชุดทดสอบ; restore จากสำเนา 2 ครั้งติดกันได้ผลเหมือนเดิม
- soak 60 นาที: exception 0, heap แกว่ง ±30 MB, handle leak 0

### ความเสี่ยง
- `UnityEngine.Random` ไม่มี seed → **ห้าม assert ค่าที่สุ่ม** ให้ assert invariant แทน
- automation บนเกมที่ไม่มี headless mode → ต้องยอมรับว่า test เป็น semi-automated (สคริปต์ + จอเดียว)
- ML unload path ยัง beta → ปิดเกมให้สุดทุกครั้งก่อนคัดลอกเซฟ

### Rollback
ถอย DLL เป็นเวอร์ชัน stage ก่อน (versioned build), pref คงเดิม, เซฟคืนจากสำเนา

---

## 8. S4 — World follow (opt-in, ทำหลัง S5)

**เป้าหมาย (opt-in เท่านั้น):** guest รันเกมอินสแตนซ์ของตัวเองแล้วตามปาร์ตี้ host — เวลา/ตำแหน่ง/ปะทะ/เข้าศึกพร้อมกัน; ปริยายยังเป็น overlay viewer ของ S2

**ขอบเขตตาม D1/D2/D4:**
- ทำ **หลัง S5 ผ่าน** เท่านั้น (ทำท้ายสุดของงานที่ลงมือ — ก่อน S6 ซึ่งเป็นวิจัย) เหตุผล: S4 เป็น stage ที่เสี่ยงต่อเซฟ/สองอินสแตนซ์มากสุด จึงต้องมี fault harness ของ S5 พร้อมก่อน
- ใช้ **โหมด B (path-follow) เท่านั้น** — **ตัดโหมด A (teleport `bigMapPos`) ออก** เพราะเสี่ยงต่อ encounter/นาฬิกาโลก และขัดกับหลัก "ไม่ใช่ guest จำลอง"
- การให้ guest **เข้า battle scene เอง** (`PrepareBattleMap` ของ guest) = **วิจัยเท่านั้น** ตาม D4; ใน S4 guest ดูศึกผ่าน overlay ของ S2

### Deliverable
- Protocol v3: `WORLD_AUTH{worldTime{day,hour}, partyPos, areaId, travelSpeedFactor, modalState, encounter}` 5 Hz + on-change; `ENCOUNTER{npcId, kind}`; `BATTLE_START/BATTLE_END`
- Guest follower — **path-follow เท่านั้น:** เรียก `SetPlayerMoveTargetArea` ไปยัง waypoint ของ host (เดินผ่านระบบของเกมเอง → เวลา/encounter ยังคำนวณโดยเกมของ guest ตามปกติ) ไม่เขียน `bigMapPos` ตรง ๆ
- ไม่มี "โหมด A (teleport)" — ตัดออกตาม D2 (ถ้าอนาคตอยากได้ ต้องเป็น proposal ใหม่พร้อมข้อมูล `CheatCheck`/encounter ที่พิสูจน์แล้ว)
- **Clock mirror:** guest แสดงนาฬิกา host บน overlay เสมอ; จะเขียน `WorldData.hour` ก็ต่อเมื่อ JIT ยืนยันว่าไม่ทำให้ `cheat*` เปลี่ยนและไม่ถูกเขียนทับทุกเฟรม
- **Modal state:** `enum {None, Shop, Dialogue, Plot, Inn, Working, Menu}` สร้างจาก `SureMenu.pause`, `WorkingUIController.*`, และ UI stack ที่ enumerate ได้ตอนรัน — ใช้ 2 ทาง: (ก) guest รู้ว่าตอนนี้ host "หยุดโลก" อยู่ → ห้ามส่งคำสั่ง, (ข) ใช้เป็น exit criteria ข้อ 4
- **Encounter sync:** Harmony postfix `BigMapController.MeetBigMapNpc` (อ่านอย่างเดียว) → host ส่ง `ENCOUNTER` → guest ระงับ encounter ของตัวเอง (ไม่ให้เกิดศึกซ้อน) แล้วรอ `BATTLE_START`
- Battle บน guest: **overlay ของ S2 เท่านั้น** — การให้ guest รัน `PrepareBattleMap` ของตัวเองถูกย้ายไปเป็นคำถามวิจัยใน S6 ตาม D4 ไม่ใช่เป้าหมายของ S4

### Just-in-time ที่ต้องสำรวจใน S4
1. **วัดนาฬิกาโลกซ้ำตอนปาร์ตี้เดิน** — sample `worldTime/hour` ที่ 10 Hz 60 วินาที ขณะ host เดิน/พัก/ทำงาน (ผลเดิม Δhour = 0 วัดตอนอยู่นิ่ง 2 วินาที จึงยังไม่พอ) [v2] → หา **จุดที่ `hour` ถูกเขียน** (ยังไม่เจอใน worldmap-notes §7 — ยังเป็น GUESS) เพื่อรู้ว่า (ก) mirror clock ควรเขียนตอนไหน (ข) guest จะ accrual เองหรือไม่
   - **carry-over จาก S0 ข้อ 4:** การเทียบ `snapshot world` กับ Frida 3 ครั้ง (ห่าง ≥ 1 นาที) ต้องทำซ้ำ **ขณะปาร์ตี้เดินบน BigMap** ด้วย เพราะ S0 วัดได้แค่นิ่ง/อยู่ MainGame — ใช้ช่วงเดียวกับข้อนี้ (เดิน 60 วิ) แล้วบันทึกค่า x/y ที่ขยับจริงเป็นหลักฐาน
2. **modal ที่หยุดโลกให้ครบ** — เปิดร้านค้า/บทสนทนา/พล็อต/นอนโรงแรม/ทำงาน แล้ว enumerate: field ไหนเปลี่ยน, `pause` ตัวไหนถูกตั้ง, เวลาเดินไหม → เติม enum ให้ครบ (เดิมมีแค่ `SureMenu.pause` + `WorkingUIController` ที่ยืนยัน — ที่เหลือยังเป็น GUESS) [v2]
3. **path-follow ทำงานได้จริงแค่ไหน** — ยิง `SetPlayerMoveTargetArea` ที่ 1/5 Hz ตาม waypoint ของ host: วัด lag, jitter, การรีสตาร์ทเส้นทางเมื่องุด และยืนยันว่า encounter ยังยิงปกติ
4. **`CheatCheck` ตอบสนองต่อการเขียน clock ไหม** (ถ้าเลือกเขียน `hour`) — อ่าน `WorldData.cheat*` ก่อน/หลัง; ห้ามเขียน flag เอง
5. **modal ของ guest ระหว่างตาม** — ถ้า guest เปิด modal ของตัวเอง path-follow ควรหยุดหรือไม่ และหยุดแล้วกลับมาต่อได้สะอาดไหม
6. **หา chain จากปะทะบนแผนที่ (`MeetBigMapNpc`) ไปถึง `PrepareBattleMap`** — ผลสแกน 338 hit เดิมเชื่อไม่ได้ (naive module scan ของ overload ทั้ง 11) ต้องพิสูจน์แบบ runtime [v2]

### Exit criteria (วัดได้)
1. 10 นาทีเดินตาม (โหมด B): ระยะห่าง guest→host p95 ≤ **2 map units** หรือ ≤ 0.5 s ของการเดิน; 0 เหตุการณ์ rubber-band > 1 s
2. เวลา: `day/hour` ตรงกันทุก sample, drift = 0 หลัง 10 นาที — โหมด **display-only ถือว่าผ่าน**; การเขียน `WorldData.hour` เป็นทางเลือกที่ต้องผ่าน JIT ข้อ 4 ก่อนเท่านั้น
3. 12 ครั้งที่ปะทะ → guest แสดงศึกเดียวกัน (overlay) 12/12 ภายใน ≤ 2 s, ศึกซ้อน = 0 (guest ไม่เข้า battle scene เองตาม D4)
4. modal state ถูก 5 ชนิด (ร้าน/บทสนทนา/พล็อต/โรงแรม/ทำงาน) ≥ 9/10 ครั้ง
5. ทรัพยากร (`money/fame`) ของ guest ไม่เพี้ยนหลังจบ session (hash เท่ากับก่อนเริ่ม); ไม่มีการเขียนเซฟโดย guest (`savehash` ไม่เปลี่ยนเลยตลอดการทดสอบ)
6. **รันชุดทดสอบ S5 ซ้ำทั้งชุดหลังจบ S4 และผ่านเท่าเดิม** [v2]

### ความเสี่ยง
- **สองอินสแตนซ์ใช้ Steam user/เซฟเดียวกัน → เซฟเสีย/Achievement ปน** (สูง) → ต้องใช้สำเนา + พิจารณาแยก userdata; ถ้าแยกไม่ได้ ให้ยอมรับว่า guest "ห้าม save" และปิดด้วย hook
- เกมอาจ save ตอนปิด → guest ต้องปฏิเสธการ save ในโหมด guest (ทดสอบว่าไม่ทำให้เกมพังก่อนเปิดใช้)
- ยิง waypoint ถี่/ห่างเกิน → guest เดินส่ายหรือตามไม่ทัน, encounter เพี้ยน
- เวลาของ guest เดินเองระหว่างตาม → drift; ถ้าหาจุด accrual ไม่เจอ ให้ใช้ display-only

### Rollback
`role=host` อย่างเดียว / `coop.follow=false` → guest หยุดเขียน world ทันที; host ไม่ได้รับผลกระทบเลยในทุกกรณี

---

## 9. S6 — วิจัยเท่านั้น (ยังไม่เขียนโค้ด)

**เป้าหมาย:** ตอบว่า guest เดินอิสระคนละที่ได้จริงไหม และราคาเท่าไร

**Deliverable:** `coop/research/RESEARCH-independent-party.md` + GO/NO-GO + ประมาณการงาน

**สิ่งที่ต้องสำรวจเพิ่ม (Just-in-time, อ่านอย่างเดียว):**
1. เกมรองรับปาร์ตี้ผู้เล่นหลายชุดพร้อมกันไหม — `WorldData.playerHero` เป็น singleton หรือเปล่า; มีระบบ "party" อื่นที่ independent ไหม
2. เวลาโลกเป็น global หรือ per-party (ผูกกับข้อที่ยังไม่รู้ใน S4)
3. ถ้าแยกกันเดิน การปะทะ/ศึกจะแชร์กันได้อย่างไร — `BattleController` มี state เดียวต่อโปรเซสหรือเปล่า (สองศึกพร้อมกัน = 2 instance?)
4. นับจุดที่ผูกกับ singleton (`playerHero` reference count, `HudController` ผูกกับปาร์ตี้เดียว) → ประเมินงาน

**Exit criteria (วัดได้):** ตอบครบ 4 ข้อพร้อมหลักฐานอ้างอิง (RVA/offset/ค่าที่วัด), มีตาราง "singleton ที่ต้องรื้อ" พร้อมจำนวนจุดอ้างอิง, และมีคำแนะนำ GO/NO-GO ที่มีเหตุผล

**ความเสี่ยง:** (ก) probe อาจต้องเข้า battle scene/shop ซึ่งเป็นการเลื่อนสถานะเกม → ใช้สำเนาเซฟ + protocol §1.2 เหมือน stage อื่น; (ข) ขอบเขตบานปลาย (นี่ไม่ใช่ stage ที่ส่งมอบโค้ด); (ค) สรุป GO ทั้งที่หลักฐานบาง — ต้องระบุระดับความมั่นใจต่อข้อ

**Rollback:** ไม่มี artifact ที่ลงในเกม; สคริปต์ probe อยู่ใน `coop/research/` ลบโฟลเดอร์ได้ทันที และไม่มีการแก้ไฟล์เกม/เซฟจริง

---

## 10. ความเสี่ยงรวม (cross-stage)

| # | ความเสี่ยง | ผลกระทบ | mitigation | ตรวจได้อย่างไร |
|---|---|---|---|---|
| 1 | Harmony patch บน method ที่ถูก **inline/เล็กเกิน** | patch "ติด" แต่ไม่ทำงาน → log ขาด | ตรวจ `PatchApplied` + วัด capture rate จริง; fallback polling | S1 capture rate ≥ 99%; ถ้าตกให้เห็นทันที |
| 2 | **Marshalling ของ Il2CppInterop** (string/struct by value, `Il2CppString*`) | crash หรือค่าเพี้ยนเงียบ ๆ | patch ผ่าน wrapper ที่ generate มา, ถ้าไม่ชัวร์ใช้ `IntPtr` signature | unit test + เทียบค่ากับ UI 20 เฟรม |
| 3 | **Thread safety ของ IL2CPP** | crash แบบสุ่ม, state พัง | คิวเดียว + main-thread assert ทุก call + `MainThreadGuard` ใน dev | guard throw = เจอทันที; soak 60 นาที |
| 4 | **GC handle leak / pointer ตาย** | ค่าขยะ, crash หลังโหลดเซฟ | ห้ามถือ wrapper ข้ามเฟรม, ส่งต่อแค่ `stableId`, re-resolve ทุก tick | นับ handle ก่อน/หลัง, 60-min soak |
| 5 | **Frame budget** (snapshot 60 Hz + overlay) | เกมกระตุก | budget 4 ms, delta encoding, ring buffer คงที่ | p99 frame cost ต่อ stage |
| 6 | **สองอินสแตนซ์/เซฟเดียว/Steam user เดียว** | เซฟเสีย, Achievement ปน | สำเนาเซฟ + guest ห้าม save + แยก userdata ถ้าทำได้ | hash เซฟจริงไม่เปลี่ยน |
| 7 | **Save ตอนปิดเกม** | guest เขียนทับ | guest: deny save (ทดสอบบนสำเนาก่อน) | savehash guest คงที่ |
| 8 | **Version drift** (เกมแพตช์/ML อัปเดต) [v2] | offset/RVA/interop เปลี่ยนทั้งหมด | `FieldHash` handshake + `VERSION.md` (SHA-256 `GameAssembly.dll` / `global-metadata.dat`) เทียบทุก session + anchor list เดียวในโค้ด; **ML ไม่มี mapping ให้เกมนี้ (`Game Not Found on RemoteAPI`)** → ทุก anchor มาจาก dump ของเราเอง ต้อง re-dump เมื่อเกมแพตช์ | hash ไม่ตรง → หยุด; mismatch ปฏิเสธ ≤ 1 s |
| 9 | **ชนกับ LongYinThai** (patch TMP/Text) [v2] | ลำดับ patch, ข้อความ log ไม่ตรงจอ, เกม error | hook ของเราเป็น postfix อ่านอย่างเดียว; ตรวจรายการ patch ใน S0 (`compat-notes.md`), ลำดับ postfix ใน S1; บันทึกใน README; ทดสอบเปิด/ปิดสลับกัน | 2 ศึกเทียบข้อความจอ vs log |
| 10 | **`CheatCheck` / flag ในเซฟ** | เซฟติดธง | ห้ามเขียน `cheat*`, อ่านก่อน/หลังทุกครั้งที่เขียน field ใหม่ | ค่า flag ไม่เปลี่ยน |
| 11 | **ความปลอดภัยเครือข่าย** | guest ยิงคำสั่งอันตราย | validator ฝั่ง host เท่านั้น, bind loopback/VPN เท่านั้น, ไม่มี relay | red-team test 20 เคส |
| 12 | **ML 0.7.3 beta API** | build พังหลังอัปเดต | ห่อ pref/hook ไว้ชั้นเดียว, pin เวอร์ชัน | build matrix (เฉพาะเวอร์ชันที่ pin) |
| 13 | **RNG เป็น global `UnityEngine.Random`** | desync ถ้า guest สุ่มซ้ำ | guest ไม่จำลองเลย; host-only authority | divergence hash = 0 |
| 14 | **Soft-lock จาก state machine** (S1.5/S3) | เล่นต่อไม่ได้ | panic switch ทันที, ทดสอบบนสำเนา, audit log | 10 นาทีเล่นจริง 0 ค้าง |
| 15 | **VPN (ZeroTier/Tailscale) jitter/MTU** | p95 เกิน 150 ms, frame แตก | frame ≤ 1100 B, ใช้ TCP (ทน retransmit กว่า UDP), วัดก่อนรับเป้า | S2 วัด p95 ที่ 60 ยูนิตผ่าน VPN จริง |
| 16 | **ศึก 60 ยูนิต (30/ฝั่ง) หนักกว่าเคสที่เคยเห็น (11v7)** | snapshot/hook หลุดเป้า, bandwidth เกิน | วัดที่ 60 ยูนิตตั้งแต่ S1; ถ้าเกินให้ลดความถี่ของ snapshot เต็ม + เพิ่ม delta/quantize ก่อนเพิ่ม optimization อื่น (S2 ส่งแบบ event-driven จึงไม่มีการ "ลด rate" ของ stream) | ตัวเลข S1/S2 จริง |
| 17 | **ศึกไม่ใช่เทิร์นล้วน (ATB)** [v2] | ต้องออกแบบ S3 ใหม่ (ลำดับ call และ validator ใช้ไม่ได้) | พิสูจน์ใน S1.5 (JIT ข้อ 6–8) ก่อนสร้างเครือข่าย | exit criteria 4/5 ของ S1.5 |

---

## 11. การตัดสินใจที่ยืนยันแล้ว (2026-10-20) และผลต่อแผน

| # | หัวข้อ | คำตอบ | ผลต่อแผน (แก้แล้วที่ไหน) |
|---|---|---|---|
| D1 | Topology | **(ก) viewer overlay ก่อน ผ่าน S3**; world-follow แบบเกมอินสแตนซ์ = opt-in ทีหลังสุด | S2/S3 เป็น viewer-only (§5, §6); S4 เลื่อนหลัง S5 (§8) |
| D2 | S4 MOVE | **โหมด B (path-follow)** และเลื่อนไปทำหลัง S3 ผ่าน | ตัดโหมด A ออกทั้งหมด; S4 อยู่ท้ายสุด (§8) |
| D3 | ขอบเขตคำสั่ง S3 | **เฉพาะยูนิตที่ถึงตาของมันแล้ว** | validator เพิ่ม `REJECT(NOT_YOUR_TURN / STALE_TURN)`, ไม่มี queue ข้ามเทิร์น (§6) |
| D4 | guest เข้า battle | **วิจัยก่อน ไม่ใช่เป้าหมายจริง** | ย้ายไป S6; S4 ใช้ overlay ของ S2 เท่านั้น (§8, §9) |
| D5 | นโยบายเซฟ | **guest ห้าม save ตลอดไป + ทดสอบบนสำเนาเสมอ** | §1.2 เป็นข้อบังคับ; S5 ต้องพิสูจน์ deny-save (§7) |
| D6 | IPC/Transport | **named pipe เป็นหลัก**; TCP ใช้ผ่าน **VPN เสมือน LAN (ZeroTier/Tailscale)** | S2 transport = TCP บน interface VPN, frame ≤ 1100 B (§5); pipe = IPC ภายในเครื่อง (§2, §1.6) |
| D7 | ภาษา UI | **อังกฤษก่อน** แยก string ไฟล์เดียวเพื่อแปลไทยทีหลัง | `UIStrings.cs` เป็น deliverable ของ S0 + test (§2) |
| D8 | สเกล/latency | **30 ยูนิต/ฝั่ง (60 รวม)**, p95 **150 ms** | S2 exit criteria + ความเสี่ยงข้อ 15/16 (§5, §10) |
| D9 | เกณฑ์ IPC (S0) | **อิงเฟรม:** ping/`stats` p99 ≤ 1× frame p95; คำสั่ง IL2CPP p99 ≤ frame p99 + 10 ms (รายงาน frame time กำกับ) | §2 ข้อ 3 |
| D10 | `PlayerprefData.dat` | **volatile** — เกมเขียนเองทุกครั้งที่เปิด/ปิด (control test `Enabled=false` ยังเปลี่ยน) ยกเว้นจากเกณฑ์ savehash; `SaveSlot*` ยังต้องเท่าเดิม 100% | §1.2, §2 ข้อ 5 |

### 11.1 คำถามเล็กที่ยังค้าง (ไม่บล็อก S0, ต้องการก่อน S2)
1. **VPN ค่ายไหน** — ZeroTier หรือ Tailscale? (โค้ดจะ bind ตาม interface ไม่ผูกยี่ห้อ แต่ test harness ต้องรู้ว่าจะตั้ง lab อย่างไร)
2. **เครื่อง guest** — เครื่องที่สองแยก หรือเครื่องเดียวกันรัน 2 อินสแตนซ์? (มีผลกับ pipe naming, ขนาดหน่วยความจำที่ S1/S2 ใช้ และวิธีทำ S5 fault injection)
3. **ศึก 30v30 หาได้จากไหน** — เซฟสำเนาใด/โหมดไหน ใช้เป็น benchmark ตอน S1–S2 (ถ้าหาไม่ได้ ให้ปรับเป้าเป็น 20/ฝั่งและบันทึกเหตุผล)

---

## 12. ลำดับการทำงาน และ Changelog

### 12.1 ลำดับการทำงาน

```
S0 --gate--> S1 --gate--> S1.5 (S3-spike) --gate--> S2 --gate--> S3 --gate--> S5 --gate--> S4 (opt-in) --> S6 (วิจัย)
```

เหตุผลของลำดับ:
- **S1.5 หลัง S1:** พิสูจน์การสั่งยูนิตได้ก่อนลงทุนกับเครือข่าย — ถ้าล้มเหลว หยุดที่ S1 ได้โดยไม่เสียงาน transport
- **S5 ก่อน S4:** S4 เสี่ยงสุด (สองอินสแตนซ์/เซฟ/เขียน world) ต้องมี fault harness + deny-save + `FieldHash` พร้อมก่อน
- **S4 ท้ายสุด (opt-in):** ตาม D1/D2; S2/S3 ที่ผ่านแล้วยังใช้ได้เต็มรูปแบบโดยไม่ต้องมี S4
- **S6 วิจัยคู่ขนานได้** แต่ไม่บล็อก gate ใด

ทุก gate ต้องมี: รายงานผลวัด, hash เซฟก่อน/หลัง, `git tag stage-SN`, และบันทึก anchor ใหม่ที่ค้นเจอ

**[v3] ลำดับ ช่วง 0 (S1.5→S3) คงเดิมทุกตัวอักษร — ห้ามแทรก spike หรืองานอื่นใดของ ช่วง 1 ก่อน/ระหว่าง stage ของ ช่วง 0 รวมถึงห้ามสำรวจ JIT ของ ช่วง 1 แทรกระหว่าง stage ของ ช่วง 0**

**[v3] ลำดับ ช่วง 1 (C2 spike ladder) — เริ่มได้เฉพาะหลัง gate ของ S3 ปิดแล้ว:**

```
S3 --gate--> SP1 puppet hero --gate--> SP2 world clock --gate--> SP3 headless server
     --gate--> SP4 puppet client --gate--> SP5 scale/ลด NPC --gate--> SP6 battle worker
     --gate--> มติ "C2 จริงหรือไม่" (tag checkpoint-C2)
```

- ลำดับ SP1→SP6 เรียงตามโจทย์ของผู้ใช้ (ความเสี่ยง/ความไม่รู้ที่ต้องตัดตอนก่อน) และเป็น dependency แบบหลวมด้วย: SP4 อ้างผลของ SP1, SP5 อ้าง SP1+SP3, SP6 อ้าง SP3 — การสลับลำดับต้องมีมติก่อน
- spike ไหนจบ NO-GO ห้ามวิ่งไปตัวถัดไปเอง — กลับไปตัดสินที่ §20 ก่อน (ต่อ/หยุด/ออกแบบใหม่)
- แต่ละ spike ยังต้องมี **แผนย่อยราย stage** (เหมือน S2a-2/S2a-3 ที่ทำมา) อนุมัติก่อนเปิดเกมเสมอ — §16 นี้เป็นกรอบ ไม่ใช่ใบอนุญาต

### 12.2 Changelog
- **v1:** ฉบับแรก + S1.5 (S3-spike) + แก้ตารางสิทธิ์ Harmony ของ S0
- **v2:** S0 เปลี่ยนเป็น "ไม่แตะ state ของเกม", S2 เปลี่ยนเป็น event/delta + snapshot เต็มเป็นระยะ, S1.5 เพิ่มการพิสูจน์ว่าเกมหยุดรอ input และตรวจ `battleTime`/`battleMove`/`battlePaused`, S4 เพิ่มงานสำรวจ hour/encounter chain และต้องรัน S5 ซ้ำ, S0 เพิ่ม exit criteria 4 ข้อ, เพิ่มกฎ interop แบบมีชนิด, `VERSION.md`, repo แยก, ความเสี่ยง #5–7
- **v2 merged (หลังทดสอบ S0):** แยกเกณฑ์ IPC เป็น ping/`stats` กับคำสั่งที่แตะ IL2CPP, เพิ่ม dev command `bench_dispatch`, ระบุ loopback TCP เป็นข้อยกเว้นสำหรับทดสอบในเครื่อง, และนิยาม latency = host-apply → guest-store
- **S0 measured:** เกณฑ์ IPC เปลี่ยนเป็นอิงเฟรม (≤1× frame p95 / ≤ frame p99+10 ms พร้อมรายงาน frame time), เพิ่มการวัด frame time ใน `stats` (p50/p95/p99), และยกเว้น `PlayerprefData.dat` จากเกณฑ์ savehash หลัง control test `Enabled=false`
- **S0 closed (2026-10-20):** ผ่าน 12/12 + tag `stage-S0`; เพิ่ม §2 "carry-over จาก S0" (ข้อ 4 → S4 JIT 1, ข้อ 9 ครึ่งศึก → S1 JIT 7) และ §14 สถานะ
- **v2 merged:** ฉบับนี้ — รวม v2 เข้ากับ v1 โดยเก็บรายละเอียดของ v1 ไว้ทั้งหมด (field/ตัวเลข/ขั้นตอนย่อย) และตัดสิน 4 ข้อที่ขัดกัน: (1) S2 ใช้ **event-driven** ของ v2 (ตัด 2 Hz/20 Hz ออก, เกณฑ์ bandwidth วัดแบบ event-driven), (2) แถว raw offset บังคับเฉพาะ **โค้ดที่ ship** สำรวจยังทำได้, (3) S0 gate เรื่อง thread ผ่านได้ถ้ามี **ทางแก้ที่ทดสอบแล้ว**, (4) เรียงเลขหัวข้อตาม v2 (`§0` ขอบเขต, `§0A` สภาพแวดล้อม, `§1.3` โครงโปรเจค, `§1.7` Steam, `§12` ลำดับ+Changelog) และเพิ่ม `§13` ภาคผนวก anchor
- **v3 (2026-10-24, ร่าง):** เพิ่ม ช่วง 1 = เป้าหมาย C2 "โลกเดียวกลาง server ตัดสินทุกอย่าง": สถาปัตยกรรมสมมติฐาน (§15, ทุกข้อ GUESS), spike ใหม่ 6 ตัว SP1–SP6 (§16), ตารางสิทธิ์ ช่วง 1 (§17), **งบ verb v1** (§18.1), **การตรวจฝั่ง server** (§18.2), กติกาที่ไม่เปลี่ยน (§19), คำถามรอมติ 6 ข้อ (§20) — §0–§14 คงข้อความ v2 merged เดิมทุกตัวอักษร

---

## 13. ภาคผนวก — anchor ที่จะใช้ (ต้อง re-verify ใน S0 ทุกตัว)

| ของ | ค่า | ที่มา |
|---|---|---|
| `WorldData.worldTime` / `hour` / `TimeDifficulty` | 0xA8 / 0xB4 / 0xB0 | worldmap-notes (§2) |
| `TimeData.year/month/day` | 0x10 / 0x14 / 0x18 | worldmap-notes |
| `HeroData.bigMapPos` → `BigMapPos.x/y` | 0xC8 → 0x10 / 0x14 | worldmap-notes (§1) |
| `HeroData.itemListData` / `fame` / `badFame` | 0x220 / 0x1C4 / 0x1C8 | worldmap-notes (§5) |
| `HudController` labels (`moneyLabel`, `fameLabel`, …) | 0x70, 0x50, … | worldmap-notes (§5) |
| `UnityEngine.UI.Text.m_Text` | 0xE0 | worldmap-notes |
| `BigMapController.MeetBigMapNpc` | 0x8DFFB0 | worldmap-notes (§4) |
| `BigMapController.Update` → `ManageAllHeroMove` → `ManageHeroMove` | 0x8E3A70 → 0x8DDD60 → 0x8DDEA0 | worldmap-notes (§1) |
| `SetPlayerMoveTargetArea` (int/string/GameObject) | 0x8E2F10 / 0x8E2F90 / 0x8E30B0 | worldmap-notes (§1) |
| `BattleController.AddInfoText` | 0x7F9310 | battle-notes (log) |
| `BattleController.PrepareBattleMap` (ตัวอย่าง 3 overload) | 0x8150C0 / 0x814A70 / 0x815CC0 | battle-notes (§battle start) |
| `BattleController.BattleGridClicked` / `MoveButtonClicked` | 0x7FA090 / 0x813FC0 | battle-notes |
| `BattleController.SetAllAuto` | 0x81ECA0 | battle-notes |
| `GameDataController.Save` / `Load` / `GetSaveDataPath` | 0xC2CBB0 / 0xC2C3D0 / 0xC1EB70 | battle-notes (save) |

> ทุก RVA ในตารางนี้มาจาก notes ที่เก็บไว้ **ยังไม่ถือว่ายืนยันสำหรับใช้เขียน** — S0 ต้อง re-resolve ผ่าน interop แล้วใส่ผลลง `FieldHash` ก่อน stage ใดจะเรียก method

---

## 14. สถานะ

- **S0 ปิดแล้ว (2026-10-20)** — ผ่าน 12/12 ข้อ, tag `stage-S0` (commit `0d33444`), หลักฐาน `evidence/S0-20261020-044245/`
- carry-over 2 ข้อบันทึกไว้ที่ §2 (ข้อ 4 → S4, ข้อ 9 ครึ่งศึก → S1)
- **กำลังรอ: อนุมัติแผนย่อย S1 (§3)** — ยังไม่เริ่มเขียนโค้ด S1


---

## 15. [v3] ช่วง 1 — เป้าหมาย C2 และสถาปัตยกรรมสมมติฐาน (ทุกข้อ GUESS จนกว่า spike จะยืนยัน)

**เป้าหมาย C2 [SPEC]:** มี **world จำลองเดียว** อยู่กับ **server = โปรเซสเกมหนึ่งตัว** ที่เป็นผู้ตัดสินทุกอย่าง (authoritative เดียว — คงหลัก §0.2 เดิม); ผู้เล่นทุกคนรวมถึง host ขับโลกด้วย **verb** (§18.1) ที่ server ตรวจแล้วรันเท่านั้น (§18.2); client ที่เหลือเป็น **puppet** — ปิดการจำลองในเครื่อง รับ state จาก server มาแสดงผล

### 15.1 หลักที่ไม่เปลี่ยนตามสถาปัตยกรรมใหม่ [SPEC]
- host-authoritative / guest ไม่จำลอง ไม่สุ่ม ไม่คำนวณผล (§0.2)
- RNG ของศึกเป็น `UnityEngine.Random` ตัวกลาง — จุดสุ่มมีที่เดียวเสมอ อยู่ฝั่งที่รัน sim
- guest ห้าม save ตลอดไป (D5), ทดสอบบนสำเนาเสมอ (§1.2), ห้ามแตะ Steam/cheat flag (§1.7)
- ทุกการเรียก IL2CPP ผ่านคิว main thread (§1.4)

### 15.2 สถาปัตยกรรมสมมติฐาน — ห้ามใช้ตัดสินงานจริงจนกว่า spike ที่ผูกไว้จะยืนยัน

| # | สมมติฐาน | ป้าย | spike ที่ยืนยัน | สิ่งที่รู้แล้วบางส่วน |
|---|---|---|---|---|
| H1 | โปรเซสเกม (Unity player เต็ม) ใช้เป็น world server ได้ — sim เดินต่อแม้ไม่มีหน้าจอ/โฟกัส | **GUESS** | SP3 | พฤติกรรม minimize/hidden/`-batchmode` ของเกมนี้**ไม่เคยถูกวัด** |
| H2 | ผู้เล่นทุกคน = `HeroData` ในเซฟของ server (hero ของ guest เป็น HeroData/สหาย-class ในเซฟ host) | **GUESS** | SP1 | `WorldData.playerHero` เป็น singleton หรือไม่ยังเป็นคำถามวิจัยค้างของ S6 (§9 ข้อ 1) |
| H3 | ขับผู้เล่นด้วย **เส้นทางเดียวกับที่ AI ขับ NPC** (ผ่าน `ManageHeroMove`/`SetPlayerMoveTargetArea`) แล้วปิด AI **เฉพาะตัวนั้น** — ไม่เขียน `bigMapPos` ตรง ๆ (คงโหมด B ของ D2) | **GUESS** | SP1 | chain `BigMapController.Update → ManageAllHeroMove → ManageHeroMove` + `SetPlayerMoveTargetArea` 3 overloads มีใน anchor [DUMP, §13] — ยังไม่ยืนยันว่า NPC hero วิ่งผ่านสายนี้จริง และไม่รู้ flag ปิด AI ต่อตัว |
| H4 | puppet client ปิดการจำลองในเครื่องได้จริง (หยุด AI + นาฬิกาท้องถิ่น) และแสดง state จาก server | **GUESS** | SP4 | transport/snapshot/resync/HMAC ของ S2a-3 ใช้ได้แล้ว [RUNTIME, X1–X9] — แต่เป็น state ศึก ยังไม่มี state โลก |
| H5 | battle worker = **โปรเซสแยกต่อหนึ่งศึก** | **GUESS** | SP6 | ข้อจำกัดเชิงระบบ: logic ศึกเป็น IL2CPP native ใน `GameAssembly.dll` → โปรเซสแยกที่เป็นไปได้ต้องเป็นอีก Unity player instance ไม่ใช่ .NET process ธรรมดา; `PrepareBattleMap` เรียกตรงโดยไม่โหลด world ได้หรือไม่ยังไม่รู้ [DUMP 3 overloads, §13] |
| H6 | นาฬิกาโลกขับด้วย **เวลาจริงของ server**; action ที่กินเวลา (งาน/พัก/คราฟต์/เดินทาง) กลายเป็น **timer ส่วนตัวของผู้เล่น** | **GUESS** | SP2 | [RUNTIME] `GameController.ChangeDay()` เดินวันละ 1 วัน/ครั้ง, `ChangeDay(int)` ไม่ถูกเรียก, `hour` ไม่เปลี่ยนระหว่าง skip (evidence `LongYinCoop/evidence/S2cd-20261022-0710/`) · จุดเขียน `hour` ตอนเล่นปกติยังไม่เจอ (GUESS ค้างจาก S4 JIT §8 ข้อ 1) |
| H7 | ลด NPC ตอนสร้างโลกได้ (gate จำนวน hero ที่โลกสร้าง) | **GUESS** | SP5 | ยังไม่มีข้อมูลใด ๆ เกี่ยวกับจุดสร้าง hero ของโลกใหม่ |

> ทุกแถวในตารางนี้ = **ห้ามเขียนโค้ด ship โดยอ้างว่าจริง** — ต้องรอ spike ตามคอลัมน์สุดท้ายยืนยันแล้วอัปเดตป้ายเป็น RUNTIME พร้อมหลักฐาน

### 15.3 ความสัมพันธ์กับ S4/S5/S6 เดิม [SPEC — ตัดสินที่ §20 Q2]
- v3 **ไม่แก้/ไม่ยกเลิก** ข้อความ §7 (S5), §8 (S4), §9 (S6) — เอกสารคงไว้ตามตัวอักษร
- ข้อขัดกันเชิงบทบาท: ถ้า C2 GO (SP1–SP6 ผ่าน) งาน "world follow" ของ S4 กับ "independent party" ของ S6 ถูก C2 กลืนบทบาท — จะยกเลิก/คงไว้ = มติที่ Q2
- งานวัดนาฬิกาที่เหลือ (เดินในเมือง / พักโรงแรม / งานคราฟต์ — carry-over ที่เดิมชี้ "JIT ของ S4") เสนอย้ายเป้าหมายไปเป็น **JIT ของ SP2** — ตัดสินพร้อมกันที่ Q2

## 16. [v3] Spikes ช่วง 1 — SP1–SP6

**กติกาใช้ร่วมของทุก spike [SPEC]:**
1. แผนย่อยราย spike (เป้าหมาย/ลำดับ/exit/วิธีวัด) ต้องอนุมัติก่อนเปิดเกมเสมอ — §16 นี้เป็นกรอบเท่านั้น
2. บังคับใช้ §1 (กติการ่วม) เต็มรูปแบบ: สำเนาเซฟ §1.2, main thread §1.4, rollback §1.5, denylist §1.7
3. **งบพยายาม: ≤2 รอบวัดต่อ exit ข้อ** — หมดงบแล้วหยุดและรายงานตามจริง ห้ามขยับเกณฑ์เอง (ธรรมเนียมเดียวกับมติ X5 2026-10-24)
4. ปิดทุก spike ด้วย: รายงานผลวัดเทียบ exit ทีละข้อ + hash เซฟก่อน/หลัง + `git tag stage-SPn` (mod repo) + หลักฐาน `LongYinCoop/evidence/SPn-<timestamp>/`
5. **NO-GO ไม่ใช่ความล้มเหลวของโปรเจกต์** — เป็นผลวัดที่ต้องรายงานตรงแล้วกลับไปตัดสินที่ §20
6. สิ่งที่ทุก spike **ไม่ทำ**: ไม่แตะเซฟจริง, ไม่แตะ Steam/anti-cheat/flag `cheat*`, ไม่เรียก `GameDataController.Save/Load`, ไม่เขียน field ของเกมตรง (ดู §17), ห้ามเปิดเกมเอง (ผู้ใช้เปิดเอง), ห้ามเปิด IPC client อื่นระหว่างวัด

### SP1 — puppet hero (หุ่นเชิดฮีโร่)

**เป้าหมาย:** ยืนยัน H2+H3 — บน host (เกมมีหน้าจอปกติ, เซฟสำเนา) โค้ด mod สั่ง `HeroData` ตัวที่**ไม่ใช่** `playerHero` (สหาย/ศิษย์/hero ชั้นรอง) ให้เดินทางไป area เป้าหมายผ่าน path เดียวกับ AI และ**ปิด AI เฉพาะตัวนั้น**ได้ โดยไม่กระทบ hero ตัวอื่น

**Just-in-time (สำรวจก่อนเขียน อ่านอย่างเดียว):**
1. enumerate hero ทั้งหมดที่ `ManageAllHeroMove` วน — จำแนก `playerHero` / hero ประเภทอื่น (class/flag)
2. หา flag หรือ method ที่เป็น "ก๊อก AI ต่อตัว" (ชื่อจริงยังไม่รู้ — GUESS): จุดตัดสินใจเลือกเป้าหมาย, เดินเอง, หยุดเอง
3. ทดสอบบนจอ: สั่ง target ทับของ AI ตอน AI เปิด/ปิด — พฤติกรรมต่างกันไหม
4. ทางเข้า encounter ระหว่าง puppet เดิน (เกิดศึก = บันทึกพฤติกรรม ไปตัดสินต่อใน SP6/มติ)

**ขอบเขตการเขียน:** อ่านทุกอย่างผ่าน interop แบบมีชนิด · Harmony **postfix log-only** บน `ManageHeroMove`/`SetPlayerMoveTargetArea` · เรียก method เปลี่ยน state = **เฉพาะ** `SetPlayerMoveTargetArea` กับ puppet hero + method ปิด AI ต่อตัวที่ JIT เจอ (ต้องระบุชัดและอนุมัติในแผนย่อยก่อนใช้) · **ห้ามเขียน `bigMapPos` ตรง ๆ** (คงโหมด B ของ D2) · gate ทุก write ด้วย pref ใหม่ `coop.c2.enabled` (default false)

**Exit criteria (วัดได้):**
1. สั่ง puppet hero เดินไป area เป้าหมาย 10 รอบ → ถึงเป้า ≥9/10 (ตำแหน่งเทียบ UI บนจอ)
2. ระหว่างเดิน: hook ที่จุดตัดสินใจ AI ของตัวนั้น **ยิง 0 ครั้ง** (หรือถูก gate ทุกครั้ง) ตลอดการเดิน — ถ้าปิด AI ต่อตัวทำไม่ได้ ให้รายงานกลไกจริงที่เจอ (ถือเป็นผลวัด ไม่ใช่ความล้มเหลวของการทดสอบ)
3. hero อื่น + ผู้เล่นหลักทำงานปกติ; จำนวน hero ก่อน/หลังเท่ากัน; soft-lock 0 ใน 10 min
4. Δmean frame time A-B-A เทียบไม่สั่ง ≤ margin ของ `S2FrameRule` (Δmean ≤ 1.0 ms — ตัวชี้วัดเดียวกับมติ X5 2026-10-24)
5. ERROR=0, Exception=0, `commandsAccepted` ตรงจำนวนครั้งที่สั่ง
6. savehash: เซฟจริง 0 ไฟล์เปลี่ยน; สำเนาบันทึกก่อน/หลัง

**GO/NO-GO:** GO = ข้อ 1+2 ผ่าน และข้อ 3–6 ไม่มี FAIL · NO-GO = ปิด AI ต่อตัวไม่ได้เลย หรือการขับทำให้ระบบ hero พัง → กลับ §20 (ทางเลือกที่จะเสนอ: ขับผ่าน input จริงของ UI แทน AI path = เปลี่ยนสถาปัตยกรรม)

**ความเสี่ยง:** AI ถูกเรียกจากหลาย entry (ไม่ใช่แค่ `ManageAllHeroMove`) · ตำแหน่งกลางทางถูก persist ลงเซฟ · encounter ทับการเดิน · ปิด AI แล้ว hero ค้าง · การ persist อาจเขียน hero ตัวอื่นด้วย (ต้อง diff ทั้ง hero list)

**Rollback:** `coop.c2.enabled=false` → ทุก hook no-op · `.dll.off` · restore สำเนา · tag

### SP2 — world clock (แผนที่ writer ของ hour/day + caller การเดินวัน)

**เป้าหมาย:** สร้างตาราง writer ทั้งหมดของ `hour/day/worldTime` และ caller ของการเดินวันที่**วัดได้จริง** แล้วตอบว่า gate นาฬิกาได้จาก choke point เดียวหรือรายการสั้น — พื้นฐานของ H6 (นาฬิกาขับด้วยเวลาจริง + timer ส่วนตัว) และรับงาน carry-over นาฬิกา (เดินในเมือง/พักโรงแรม/คราฟต์ — ถ้า Q2 อนุมัติให้ย้ายมาจาก S4)

**Just-in-time:**
1. hook log-only ครอบ writer ที่รู้แล้ว: `ChangeDay()` ทั้ง 2 overload (ตัว 0-param ยิงจริง [RUNTIME]) + `TimeData.DeltaDay` ถ้าหาเจอ
2. สแกนหาจุดเขียน `hour` ตอนเล่นปกติ (ค้างจาก S4 JIT §8 ข้อ 1 — GUESS)
3. ทำ action ที่กินเวลาครบชุดบนจอ: เดินในเมือง, พักโรงแรม, งาน, คราฟต์, ปุ่ม `>>` และอย่างอื่นที่จอบอกว่าเวลาเดิน — จับ Δhour/Δday ต่อ action เทียบตัวเลขจอ

**ขอบเขตการเขียน:** **log-only ทั้ง spike — ไม่มี write path เลย** (postfix/prefix อ่าน argument + ค่าเท่านั้น) · probe แบบ transient ใช้กติกา probe ของ §1.1

**Exit criteria (วัดได้):**
1. ตาราง writer ครบต่อชุดทดสอบ: ทุก action ที่ทำให้เวลาเปลี่ยนบนจอ ถูกจับโดย hook ≥1 ตัว (catch-all 100% ของ action ที่เห็น Δ) พร้อม hit-count ต่อ action
2. "1 call `ChangeDay()` = 1 วัน" ยืนยันซ้ำบน ≥3 กรณีใหม่ และบันทึกทุกกรณีที่ไม่เป็น 1:1 (ห้ามเหมารวมจากงาน 5/10 วันเดิม)
3. เสนอ choke point ได้: รายการ writer ปลายทางที่ต้อง gate **≤3 จุด** (หรือรายงานตรงว่าไม่มี พร้อมจำนวนจริง)
4. ตอบ Δhour ต่อ action ครบ 3 งาน carry-over (เดินในเมือง/โรงแรม/คราฟต์) ตรงตัวเลขจอ
5. ERROR=0 · Δmean frame ≤ margin · savehash เซฟจริงไม่เปลี่ยน

**GO/NO-GO:** GO = ข้อ 1+3 · NO-GO = writer กระจัดกระจาย >3 จุดและไม่มีจุดร่วม → H6 แบบ "ขับเองที่เดียว" ทำไม่ได้ ต้องออกแบบนาฬิกาใหม่ที่ §20

**ความเสี่ยง:** writer เป็น field write ตรง (ไม่ผ่าน method) จับยาก · hook hot path ทำ frame หนัก (mitigate: log เฉพาะเมื่อค่าเปลี่ยน) · writer ตอน load เซฟต้องแยก context ออกจาก writer ตอนเล่น

**Rollback:** flag → no-op · `.dll.off` · restore สำเนา

### SP3 — headless server

**เป้าหมาย:** ยืนยัน H1 — world sim เดินต่อโดยไม่ต้องมีหน้าจอ/โฟกัส: ทดสอบ 3 แนวทาง (window minimize / hidden window / `-batchmode -nographics`) วัด sim-rate เทียบหน้าต่างปกติ เพื่อเลือกวิธีรัน world server จริงภายหลัง

**ขอบเขตการเขียน:** ไม่มีโค้ด mod ใหม่ที่จำเป็น (ใช้ build + sampler เดิม) · ไฟล์ใหม่เฉพาะ launch script ใน `tools/` (นอกโฟลเดอร์เกม) · **ไม่แตะ Steam ทุกทาง** — การ launch ตรง exe ทำให้ Steam ไม่นับเวลาเล่น = บันทึกเป็นข้อเท็จจริง ไม่ใช่การแตะ Steam (รอมติ Q4 ก่อนรันแนว launch ตรง exe)

**Exit criteria (วัดได้):**
1. อย่างน้อย 1 แนวทาง: sim เดินครบ 10 min ต่อเนื่อง (ΔworldTime > 0, puppet ของ SP1 ถึงเป้าหมายได้, ไม่มี pause แอบแฝง — ตรวจด้วย `battleTime`/`turnIndex`/state ไม่ใช่ `battlePaused`/`timeScale` ตามข้อเท็จจริงของ S1.5)
2. sim-rate เทียบหน้าต่างปกติ: Δmean frame ≤ margin เดิม **และ** sim-rate ≥ 90% (นิยาม SPEC ก่อนวัด — ต่ำกว่า = FAIL ตรง)
3. เกมออกสะอาด (exit code 0), ERROR=0, ไม่มี artifact ค้างในโฟลเดอร์เกม
4. ถ้า `-batchmode` พัง/ค้าง — เก็บ crash log แล้วตัดแนวทางนั้นทิ้ง (ไม่ใช่ NO-GO ของ spike ตราบใดที่แนวอื่นผ่าน)

**GO/NO-GO:** GO = ข้อ 1+2 ด้วยแนวใดแนวหนึ่ง · NO-GO = ทุกแนว sim ค้างหรือช้า >10% แก้ไม่ได้ → C2 ต้องรัน server แบบมีหน้าต่าง (ยอมรับได้ไหม = §20 Q4)

**ความเสี่ยง:** logic ขับด้วย frame แทนเวลา (vsync ตก → sim ช้าลง) — GUESS ต้องวัด · เกมใช้ UI/render หนัก อาจพังใน batchmode · hidden window อาจโดน OS throttle timer

**Rollback:** ลบ launch script · ปิดเกมปกติ · restore สำเนา

### SP4 — puppet client

**เป้าหมาย:** ยืนยัน H4 — instance เกมที่สอง (บนเครื่องเดียวก่อน, loopback): (ก) ปิดการจำลองในเครื่องได้จริง (AI + นาฬิกาท้องถิ่นนิ่ง) (ข) รับ state โลกจาก server มาแสดงผลทาง overlay ให้ตรง server — **ยังไม่เขียน game object ฝั่ง guest** (puppet แบบเต็มเป็นงานหลังจบ ช่วง 1)

**Just-in-time:**
1. sandbox สอง instance: เกมอ่าน path เซฟคงที่ [GUESS — `GameDataController.GetSaveDataPath`, DUMP §13] → ทดสอบวิธีแยกโฟลเดอร์เซฟ + `UserData` ของ instance ที่สอง (junction / สลับโฟลเดอร์ / อย่างอื่น) — พิสูจน์ก่อนรัน 2 instance พร้อมกันเสมอ
2. นิยาม snapshot โลกขั้นต่ำ: hero list (id, bigMapPos, area), calendar, modal state (ต่อยอด enum ของ §8)
3. ปิด sim ฝั่ง guest: หาจุด gate AI+clock ท้องถิ่น (ใช้ผล SP1/SP2)

**ขอบเขตการเขียน:** server: snapshot โลกใหม่แบบ**อ่านอย่างเดียว** · guest: ห้ามเขียน field เกมทุกจุด — แสดงผลทาง overlay/IMGUI เหมือน S2 · transport ต่อยอด TcpTransport + HMAC + resync ของ S2a-3 [RUNTIME] · คำสั่งขาขึ้นยังไม่มี (SP4 เป็น one-way state)

**Exit criteria (วัดได้):**
1. sandbox: instance ที่สองไม่เขียนโฟลเดอร์เซฟของ server (watch hash นิ่ง 10 min) และไม่เขียนเซฟตัวเองเลย
2. guest ปิด sim ท้องถิ่น: hook AI+clock ฝั่ง guest ยิง 0 ครั้ง (หรือ gate ทุกครั้ง) ตลอด 10 min
3. overlay แสดงตำแหน่ง hero + calendar ตรง server ทุก snapshot; latency p95 ≤ 500 ms (loopback — ค่าเริ่ม SPEC ตั้งก่อนวัด); gap/resync เดินตามกลไกเดิม (ทดสอบที่ 1 Hz และ 5 Hz)
4. server: Δmean frame ≤ margin ขณะมี client ต่อ (วิธีวัดเดียวกับ X5/X6)
5. ERROR=0 ทั้งสองฝั่ง; disconnect/reconnect กลางทาง 1 ครั้ง — ไม่ค้างทั้งสองฝั่ง

**GO/NO-GO:** GO = ข้อ 1–3 · NO-GO = ปิด sim ท้องถิ่นไม่ได้ (guest ยังขับโลกตัวเอง) → puppet แบบเต็มตายที่นี่ กลับ §20 (ทางเลือก: viewer-only ตาม S2/S4 เดิม)

**ความเสี่ยง:** สอง instance ชนเซฟ/Steam user (ความเสี่ยง #6 ของ v2 รุนแรงขึ้น) · ML `UserData` ชนกัน · state โลกใหญ่ (หลาย hero) ทำ frame/bandwidth ตก · sandbox พังเงียบ ๆ แล้วเขียนเซฟจริง (mitigate: watch hash ทุก run)

**Rollback:** ปิด instance ที่สอง · flag · restore สำเนา · ลบ sandbox

### SP5 — scale + ลด NPC ตอนสร้างโลก (10/30/100 หุ่น)

**เป้าหมาย:** วัดต้นทุน scale ของโลกเดียวที่ 10/30/100 หุ่น (NPC + puppet) และยืนยัน H7 ว่าลด NPC ตอนสร้างโลกได้จริง — ตอบคำถาม scale ค้าง (§11.1 ข้อ 3; ข้อจำกัด X2: 9 units < 60)

**Just-in-time:**
1. หาจุดสร้าง hero/NPC ช่วง new game (GUESS — ไม่มีข้อมูลเดิม)
2. sandbox โลกทดลอง: เซฟใหม่แยกจาก canonical ทั้งก้อน (รอมติ Q6)
3. วิธีนับ hero ที่ตรวจสอบได้ (interop + เทียบ UI ตัวอย่าง)

**ขอบเขตการเขียน:** hook จุดสร้าง world (หาจุด = log-only; gate จำนวน = เขียนได้**เฉพาะบนเซฟโลกทดลองใหม่**และต้องผ่าน `coop.c2.enabled`) · ห้ามแตะ canonical baseline ทุกทาง (watch hash ทุก run)

**Exit criteria (วัดได้):**
1. โลกทดลอง 10/30/100 หุ่นสร้างได้: จำนวน hero ตรงเป้า ±0 ทุกขนาด
2. ที่ 100 หุ่น: Δmean frame เทียบ 10 หุ่น ≤ margin เดิม (เกิน = FAIL ตรง ห้ามปรับเกณฑ์) + snapshot encode p99 ≤ 4 ms (เกณฑ์เดิมของ S2a-2 exit 2)
3. bandwidth ต่อจำนวนหุ่น ที่ 1 Hz และ 5 Hz — รายงาน observational (ตัดสินความพอภายหลัง)
4. sim 100 หุ่น เดิน 10 min: soft-lock 0, ERROR=0
5. canonical baseline ไม่ถูกแตะ (hash ตรงทุก baseline หลังจบ)

**GO/NO-GO:** GO = ข้อ 1+2 · GO-with-limitation = 30 ผ่าน แต่ 100 FAIL (C2 ทำได้ที่ 30) — ตัดสินต่อที่ Q6 · NO-GO = หาจุด hook สร้างโลกไม่เจอเลย

**ความเสี่ยง:** จุดสร้าง hero กระจายหลายที่ · 100 หุ่น crash/พังเชิงระบบ · sandbox พังแล้วเขียน canonical (mitigate: watch hash + save-split ก่อนทุก run)

**Rollback:** ลบเซฟโลกทดลอง · flag · `.dll.off`

### SP6 — battle worker (โปรเซสแยกต่อหนึ่งศึก)

**เป้าหมาย:** พิสูจน์/หักล้าง H5 — worker (instance Unity player แยก หรือแนวทางที่ Q5 อนุมัติ) รัน**ศึกเดียว**เต็มวงจรแบบอัตโนมัติ (ทุกเทิร์นสั่งผ่าน IPC ผ่าน validator) ได้ และวัด lifecycle/determinism/latency เพื่อตัดสินว่า C2 แยกศึกออกจาก world server ได้จริงไหม

**Just-in-time:**
1. worker เข้าศึกโดยไม่โหลด world เต็มได้ไหม — เรียก `PrepareBattleMap` ตรง [DUMP 3 overloads, §13] บน instance ที่โหลดแค่หน้าเปล่า
2. seed ของ `UnityEngine.Random` ตั้งได้ไหม → ผล 2 รอบ seed เดียวเหมือนกันไหม (determinism)
3. worker อยู่ใน sandbox เซฟตัวเองได้ไหม (ต่อยอด sandbox ของ SP4)

**ขอบเขตการเขียน:** worker = instance แยก + sandbox เซฟเฉพาะตัว · คำสั่งผ่าน validator เดิมของ S3 ขยายเป็น auto-play (`battle.move`/`battle.attack`/`battle.pass` ตาม §18.1) · ห้ามแตะ Steam · ห้ามเขียนเซฟฝั่ง world server

**Exit criteria (วัดได้):**
1. lifecycle ครบ 3 รอบ: สร้าง worker → รันศึกจบ → ทำลาย worker — ไม่มี orphan process, บันทึก memory/exit code ได้
2. ศึกจบตามปกติ 3/3 (จบด้วยเงื่อนไขของเกมเอง ไม่ force)
3. latency คำสั่งเทิร์น p95 ≤ 200 ms (loopback — ค่าเริ่ม SPEC ตั้งก่อนวัด)
4. determinism: ถ้า seed ตั้งได้ — ผล 2 รอบ seed เดียวตรงกัน (hash battle log); ถ้าตั้งไม่ได้ = รายงาน NOT-RUN + ประเมินผลต่อสถาปัตยกรรม (worker ต้องเป็นจุดสุ่มเดียว — ตรงโมเดล C2 อยู่แล้ว อาจไม่บล็อก)
5. world server ระหว่าง worker ทำงาน: Δmean frame ≤ margin, ERROR=0

**GO/NO-GO:** GO = ข้อ 1–3 · NO-GO = worker รันศึกไม่ได้ในทุกแนวที่ Q5 อนุญาต → fallback ตาม Q5 (เสนอ: ศึกอยู่ใน world server = โมเดล S3 ขยาย)

**ความเสี่ยง:** สูงสุดของชุด — IL2CPP native + Unity runtime + world context อาจทำให้ "โปรเซสแยก" เป็นไปไม่ได้เลย · ต้นทุนเครื่อง (เกมเต็ม 2 ตัวพร้อมกัน) · NO-GO ที่นี่**ไม่**ทำ C2 พังทั้งแผน — เปลี่ยนเป็น "ศึกใน server" เท่านั้น (Q5)

**Rollback:** ปิด worker · ลบ sandbox · flag · `.dll.off`

## 17. [v3] ตารางสิทธิ์การเขียน ช่วง 1 (เพิ่มใหม่ — ไม่แก้ตาราง §1.1 ของ S0–S6)

เรียงคอลัมน์ตามลำดับรันจริง SP1→SP6 · แถวคงแถวเดิมของ §1.1 เพื่อเทียบได้ตรง

| การกระทำ | SP1 | SP2 | SP3 | SP4 | SP5 | SP6 |
|---|---|---|---|---|---|---|
| อ่าน field ผ่าน Il2CppInterop แบบมีชนิด | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| อ่านด้วย raw offset/pointer | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| เรียก getter บริสุทธิ์ (ไม่เปลี่ยน state) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Harmony patch (อ่าน argument/ค่า เท่านั้น) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| เรียก method ที่เปลี่ยน state ของเกม | ✅ เฉพาะ `SetPlayerMoveTargetArea` + method ปิด AI ต่อตัว (อนุมัติในแผนย่อยก่อนใช้) | ❌ (log-only ทั้ง spike) | ❌ | ❌ | ✅ เฉพาะตอนสร้างโลกทดลอง (gate จำนวน hero) | ✅ เฉพาะคำสั่งศึกผ่าน validator |
| เขียน field ของเกมเอง | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| รับ input จากเครือข่ายแล้วรัน | ❌ (IPC ในเครื่องเท่านั้น) | ❌ | ❌ | ✅ snapshot โลก (guest อ่านอย่างเดียว) | ✅ ต่อจาก SP4 | ✅ คำสั่งศึก (verb ตาม §18.1) |
| เรียก `GameDataController.Save/Load` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| เขียนไฟล์ของ mod เอง (log/pref/capture) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Steam / online services | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

> แถว ❌ ทุกคอลัมน์ = ค่าเดียวบังคับทั้ง ช่วง 1 · การสำรวจ JIT (อ่านชั่วคราวนอกโค้ด ship) ยังทำได้ตามขอบเขตแถว raw offset ของ §1.1 · ห้ามเขียน `bigMapPos` ตรง ๆ ทุก spike (คงโหมด B ของ D2) · ทุก write ต้องผ่าน gate `coop.c2.enabled` (default false) อีกชั้นหนึ่ง

## 18. [v3] งบ verb v1 และการตรวจฝั่ง server

### 18.1 งบ verb v1 (รายการ action ที่รองรับ/ปิด) [SPEC — รออนุมัติตาม §20 Q1]

**นิยาม "งบ":** verb ที่ server ยอมรับได้ใน C2 ขั้นแรก — **deny-by-default**: verb ที่ไม่อยู่ในงบ = ปฏิเสธทันที + log เหตุผล · เพิ่ม/ตัด verb ต้องมีมติ + validator เฉพาะ verb ครบก่อน และรายงาน diff ของงบในรายงาน stage ทุกครั้ง

**เปิด (ข้อเสนอ v1 — 6 verbs):**

| verb | พารามิเตอร์ | ใครใช้ได้ | อ้างอิง |
|---|---|---|---|
| `world.move.to` | areaId | เจ้าของ hero นั้น (host หรือ puppet ใดก็ได้) | ผลของ SP1 |
| `world.wait.minutes` | n | ทุก session | timer ส่วนตัวของผู้เล่น (H6) |
| `time.skipDay` | — | **host เท่านั้น (เสนอ)** — รอ Q1 | [RUNTIME 2 กรณี] 1 call `ChangeDay()` = 1 วัน — ห้ามเหมารวม (SP2 จะยืนยันเพิ่ม) |
| `battle.move` | unitId, cell | เจ้าของ unit ที่ถึงตา (D3 ยังบังคับ) | ต่อยอด validator ของ S3 (§6) |
| `battle.attack` | unitId, targetId | เจ้าของ unit ที่ถึงตา | เดียวกัน |
| `battle.pass` | unitId | เจ้าของ unit ที่ถึงตา | เดียวกัน |

**ปิดตลอด v1 (deny ทุกคำสั่งในกลุ่มนี้ ไม่มีข้อยกเว้น):** `save.*` · `trade.*` · `item.*` (ใช้/ทิ้ง/ซื้อขาย) · `recruit.*` · `skill.*` · `equipment.*` · `steam.*` / สถิติ / ความสำเร็จ · `pref.*` (ยกเว้น `set_pref` ที่ต้อง hardening ตามงาน S2a-4 ค้างก่อน) · ทุก verb ที่แตะ `APIDispatcher` / `DnsChecker` / `CheatCheck` / flag `cheat*` — ตาม denylist §1.7 ซึ่งยังบังคับ

### 18.2 การตรวจฝั่ง server (ไม่ไว้ใจ client) [SPEC]

1. **deny-by-default** — whitelist ต่อ session จากงบ §18.1; verb นอกงบปฏิเสธทันที
2. **ownership** — server จับ map session→heroId; verb ที่อ้าง unit/hero ต้องเป็น puppet ของ session นั้นเท่านั้น (ห้ามสั่ง hero ของคนอื่น รวมถึงของ host)
3. **precondition ตรวจที่ server เท่านั้นก่อนรัน** — ศึก: `nowActiveState`/เทิร์นของ unit ตาม D3 (REJECT ประเภทเดิม) · โลก: modal ที่หยุดโลก (enum ของ §8), hero ยังมีชีวิต, ไม่อยู่ระหว่างศึก · เงื่อนไขทรัพยากรที่ verb ต้องการ
4. **rate + replay** — rate limit ต่อ verb ต่อ session (ค่าตั้งก่อนวัดในแผนย่อยของ spike ที่เกี่ยว) · ต่อยอด seq + resync ของ S2a-3 เป็นตัวต้าน replay [RUNTIME]
5. **identity** — secret ต่อ client + HMAC ทุก envelope + `fieldHash` ต่อ field ที่ verb แตะ (ต่อยอด X7 — RUNTIME: DENY hmac/fieldHash ≤ 0.3 s)
6. **choke point เดียว** — ทุก write ผ่านคิว main thread (§1.4) และผ่านลำดับ guard ก่อนสั่งแบบเดียวกับ S1.5 plan §3
7. **audit** — log accepted/denied ทุกฉบับพร้อมเหตุผล + ตัวนับ `commandsAccepted` (ต่อยอดของ S2a-3)
8. **เซฟ** — guest/worker ไม่มีสิทธิ์เขียนเซฟทุกเส้นทาง (D5); server **ไม่เคย**ใช้ข้อความจาก client เป็นความจริงของ state — client ส่งได้แค่ intent

## 19. [v3] กติกาที่ไม่เปลี่ยน (ยืนยันซ้ำใน v3)

1. **โปรโตคอลสำเนาเซฟ §1.2 บังคับเต็มรูปแบบทุก spike** — ทดสอบบนสำเนาเท่านั้น, `SaveSlot*` 0 ไฟล์เปลี่ยน, `PlayerprefData.dat` volatile ตาม D10
2. **กติกาทุกข้อใน AGENTS.md ยังเหนือกว่าเล่มนี้** — ทำทีละ stage ตามแผนและอนุมัติก่อนเริ่ม, ป้ายหลักฐาน RUNTIME/DUMP/GUESS/SPEC, IL2CPP main thread, rollback ทุก stage, ห้ามแตะ Steam/anti-cheat, เทียบ hash เกมทุก session — ถ้าเล่มนี้ขัดกับ AGENTS.md ให้หยุดแล้วรายงาน
3. **ตารางสิทธิ์ §1.1 ของ S0–S6 ไม่เปลี่ยน** — §17 เป็นตารางใหม่สำหรับ ช่วง 1 เท่านั้น
4. **ห้ามเริ่มงานใด ๆ ของ ช่วง 1** (โค้ด/probe/สร้างไฟล์ในโฟลเดอร์เกม) จนกว่า (ก) S3 ปิดตาม exit criteria ของ v2 (ข) §20 ได้มติครบ (ค) แผนย่อยราย spike อนุมัติ
5. ข้อความ §0–§14 คงเป็น v2 merged เดิม — v3 ไม่แก้; ความขัดกันระหว่าง ช่วง 1 กับ S4/S5/S6 ตัดสินที่ Q2 เท่านั้น

## 20. [v3] คำถามที่ต้องให้ตัดสิน (6 ข้อ — ห้ามเริ่ม ช่วง 1 จนได้คำตอบครบ)

| # | คำถาม | ทางเลือก/ค่าเริ่มที่เสนอ |
|---|---|---|
| Q1 | งบ verb v1 (§18.1): อนุมัติรายการเปิด 6 verbs หรือปรับ? และ `time.skipDay` ให้ guest ใช้ได้หรือไม่? | เปิด 6 verbs ตามข้อเสนอ · `time.skipDay` = host เท่านั้น |
| Q2 | ตำแหน่ง S4/S5/S6 เดิมเมื่อมี ช่วง 1 และการย้ายงานวัดนาฬิกา carry-over | (a) คง S4/S5/S6 ตาม v2 (S5 ทำหลัง S3 ก่อนเปิด ช่วง 1) (b) S5 ยุบเป็น fault-injection ใน exit ของแต่ละ spike (c) S4/S6 ยกเลิกเมื่อ C2 GO — งานนาฬิกาย้ายเป้าจาก "JIT ของ S4" → "JIT ของ SP2" |
| Q3 | topology ของ SP4/SP6 | เริ่ม 2 instance บนเครื่องเดียว (loopback) เสมอ แล้วค่อยขยายเครื่อง guest จริง/VPN หลัง SP4 ผ่าน — พร้อมตอบค้าง §11.1 (VPN ค่ายไหน, เครื่อง guest คืออะไร) |
| Q4 | แนวทาง headless ที่อนุญาตให้ทดสอบใน SP3 | minimize / hidden window / `-batchmode -nographics` / launch ตรง exe (Steam ไม่นับเวลาเล่น) — เลือกได้หลายแนว |
| Q5 | fallback ของ SP6 ถ้า "โปรเซสแยก" พิสูจน์ไม่ได้ | (a) ศึกอยู่ใน world server (โมเดล S3 ขยาย) (b) หยุด C2 battle ไว้ก่อน รอแผนใหม่ (c) อนุญาตให้ SP6 ทดสอบแนวอื่นเพิ่ม (ระบุ) |
| Q6 | เซฟโลกทดลองของ SP5 และเกณฑ์ที่ 100 หุ่น | อนุญาตสร้างเซฟโลกทดลองใหม่นอก canonical (สร้างผ่าน UI เกมบนสำเนา + hook ลด NPC) — เกณฑ์ 100 หุ่นใช้ margin เดิม (Δmean ≤ 1.0 ms) หรือกำหนดใหม่ |

---

> [v3] จบส่วนเพิ่มของ v3 — เนื้อหาถัดลงไปเป็นภาคท้ายเดิมของ StagePlan.txt (v2 ดิบ + แผนย่อย S1.5/S2a-2/S2a-3) คงเดิมทุกตัวอักษร

# STAGEPLAN — LongYinLiZhiZhuan Co-op Mod (v2)

สถานะ: อนุมัติแล้ว — กำลังเริ่ม S0
หลักการ S0: ไม่แตะ state ของเกม ไฟล์ที่เพิ่มในโฟลเดอร์เกมมีแค่ DLL ของเรา (Mods\LongYinCoop.dll) และไฟล์ config/log ของ mod เอง [v2]
ลำดับ: S0 → S1 → S1.5 → S2 → S3 → S5 → S4 → S6

## 0. เป้าหมายและขอบเขต
- เกม: LongYinLiZhiZhuan, Unity 2020.3.48f1c1, IL2CPP
- Loader: MelonLoader v0.7.3 (ติดตั้งอยู่แล้ว), HarmonyX ที่มากับ ML, runtime net6
- Mod อื่นที่มีอยู่: LongYinThai.dll, UnityExplorer (ห้ามแก้)
- โมเดล: host เป็นผู้ตัดสินเพียงคนเดียว (authoritative) guest ไม่จำลอง ไม่สุ่ม ไม่คำนวณผล
  เหตุผล: RNG ของศึกเป็น UnityEngine.Random ตัวกลางที่ไม่มี seed ของเกมเอง
- รูปแบบ co-op: guest คุมยูนิตที่อยู่ในเซฟของ host (ศิษย์/สหาย) รางวัลเข้าเซฟ host guest ห้าม save
- ป้ายหลักฐาน: RUNTIME / DUMP / GUESS / SPEC

## 1. กติการ่วม

### 1.1 ตารางสิทธิ์การเขียน
| การกระทำ | S0 | S1 | S1.5 | S2 | S3 | S5 | S4 | S6 |
|---|---|---|---|---|---|---|---|---|
| อ่าน field ผ่าน Il2CppInterop แบบมีชนิด [v2] | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| อ่านด้วย raw offset/pointer [v2] | ❌ | ขออนุมัติก่อน | ขออนุมัติก่อน | ขออนุมัติก่อน | ขออนุมัติก่อน | ขออนุมัติก่อน | ขออนุมัติก่อน | ❌ |
| เรียก getter บริสุทธิ์ (ไม่เปลี่ยน state) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Harmony patch (อ่าน argument เท่านั้น) | ✅ probe no-op | ✅ ถาวร | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| เรียก method ที่เปลี่ยน state ของเกม | ❌ | ❌ | ✅ host | ❌ | ✅ host | ✅ | ✅ | ❌ |
| เขียน field ของเกมเอง | ❌ | ❌ | ❌ | ❌ | ✅ host | ✅ | ✅ guest | ❌ |
| รับ input จากเครือข่ายแล้วรัน | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ |
| เรียก GameDataController.Save/Load | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| เขียนไฟล์ของ mod เอง (log/pref/capture) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Steam / online services | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

หมายเหตุ: offset/RVA ใน notes ใช้สำหรับสำรวจเท่านั้น ในโค้ด mod ต้อง re-resolve ผ่าน interop [v2]

### 1.2 โปรโตคอลสำเนาเซฟ
1. ปิดเกมให้สนิท
2. คัดลอกเซฟไปที่ coop-test/<timestamp>/save/
3. บันทึก sha256 ทั้งเซฟจริงและสำเนา ก่อนและหลังทดสอบ
4. ชี้ path เซฟไปที่สำเนา หรือสลับโฟลเดอร์เซฟ
5. ข้อห้าม: ห้ามทดสอบบนเซฟจริง, ห้ามสั่ง Save() ทับเซฟจริง, guest ห้ามเขียนเซฟ (บังคับด้วย pref + hook Save ให้ no-op ในโหมด guest)
6. ถ้า hash เซฟจริงเปลี่ยน ให้หยุดทุกอย่างแล้วรายงาน

### 1.3 โครงโปรเจค
- git repo แยกนอกโฟลเดอร์เกม build แล้ว copy DLL เข้า Mods\ [v2]
- .gitignore: DLL ของเกม, Il2CppAssemblies, Il2CppDumper, dump.cs ห้าม commit [v2]
- C# class library target net6.0 อ้างอิงแบบ Private=false:
  MelonLoader\net6\MelonLoader.dll, 0Harmony.dll, Il2CppInterop.Runtime.dll,
  MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll, UnityEngine.CoreModule.dll, Il2Cppmscorlib.dll
- ห้ามอ้างอิง Steamworks
- VERSION.md: SHA-256 ของ GameAssembly.dll และ global-metadata.dat เทียบทุก session ถ้าไม่ตรงให้หยุดและรายงาน [v2]
- แนะนำตั้ง Steam เป็น "Only update this game when I launch it" [v2]

### 1.4 Threading
1. ทุกการเรียก IL2CPP อยู่บน Unity main thread; network thread ห้ามแตะ object ของเกม
2. MainThreadDispatcher.Enqueue(Action) → drain ใน MelonMod.OnUpdate (budget 4 ms/frame)
3. snapshot สร้างเป็น POCO บน main thread ก่อน serialize; ห้ามถือ Il2CppObjectBase ข้ามเฟรม
4. MainThreadGuard ใน dev build: throw ถ้าเรียก API เกมนอก main thread
5. S0 ต้องวัดว่า OnUpdate กับ Harmony probe อยู่ thread เดียวกัน ถ้าไม่ ต้องแก้ข้อนี้ก่อนไป S1 [v2]

### 1.5 Rollback ทั่วไป
- ปิดทั้ง mod: เปลี่ยนชื่อ .dll → .dll.off
- ปิดฟีเจอร์: MelonPreferences (Enabled=false เป็น kill switch)
- คืนเซฟ: copy จาก coop-test/<ts>/save/
- คืนโค้ด: git tag stage-SN

## 2. S0 — ฐานราก
**เป้าหมาย:** โหลด mod เปล่าได้ มี logging, dispatcher, IPC อ่านอย่างเดียว โดยไม่แตะ state ของเกม [v2]

**Deliverables**
- LongYinCoop.dll: CoopMod : MelonMod, log แยกไฟล์, pref Enabled (ค่าเริ่ม false)
- MainThreadDispatcher: ConcurrentQueue<Action>, drain ≤ 4 ms/frame
- HarmonyProbe (ชั่วคราว): prefix no-op บน GameController.Update ใช้วัด ManagedThreadId เท่านั้น ถอดได้ด้วย pref
- IpcServer: named pipe \\.\pipe\longyin-coop-<pid> คำสั่ง ping, get_version, get_scene, get_player_pos (อ่านอย่างเดียว ผ่าน dispatcher)
- SnapshotReader (โครงเปล่า), UIStrings.cs, coopctl.py (client ทดสอบ IPC)
- VERSION.md, .gitignore [v2]

**สำรวจ just-in-time**
- thread ของ OnUpdate เทียบกับ thread ของ probe
- รายการ method ที่ LongYinThai patch ไว้ (Harmony.GetAllPatchedMethods) อ่านอย่างเดียว บันทึกลง compat-notes.md โดยเฉพาะ AddInfoText และ setter ของ Text [v2]

**Exit criteria**
1. exception จาก mod = 0 ใน 10 นาที
2. dispatcher p99 ≤ 4 ms/frame
3. hash เซฟจริงก่อน/หลังเท่ากัน
4. ถอด probe ด้วย pref แล้วไม่เหลือ patch ของเรา
5. UIStrings ใช้งานได้
6. เล่น world map ได้ปกติ และเล่นศึกจบได้ 1 ศึก [v2]
7. LongYinThai ยังแปลได้ตามเดิม [v2]
8. rename เป็น .dll.off แล้วเกมกลับเหมือนเดิม (Latest.log ไม่มีชื่อ mod เรา) [v2]
9. OnUpdate กับ probe อยู่ thread เดียวกัน (หรือรายงานถ้าไม่ตรง) [v2]

**ความเสี่ยง:** API ของ ML 0.7.3 ต่างจากเอกสาร, probe ชนกับ LongYinThai, ACL ของ named pipe
**Rollback:** .dll.off หรือลบ DLL

## 3. S1 — สังเกตการณ์ฝั่ง host
**เป้าหมาย:** อ่าน state โลกและศึก และจับ battle log แบบอ่านอย่างเดียว

**Deliverables**
- WorldSnapshot: worldTime, hour, bigMapPos ของปาร์ตี้และ NPC
- BattleSnapshot: battleState, nowActiveUnit, ตำแหน่ง/HP หน่วย
- BattleLogHook: Harmony postfix ที่ AddInfoText
- ring buffer, IPC snapshot/capture, ตัวนับ dropped/gap

**สำรวจ just-in-time**
- AddInfoText ถูก inline ไหม (เทียบจำนวน log ที่จับได้กับที่แสดงบนจอ)
- ลำดับ postfix ของเราเทียบกับ LongYinThai (ได้ข้อความก่อนหรือหลังแปล) [v2]

**Exit criteria:** capture ≥ 99%, mod cost p99 ≤ 1.5 ms/frame, snapshot ตรงกับ UI 20/20 ครั้ง, ไม่มี memory leak, hash เซฟจริงไม่เปลี่ยน
**ความเสี่ยง:** method ถูก inline, allocation ต่อเฟรม, ชนกับ LongYinThai
**Rollback:** coop.capture=false

## 4. S1.5 — S3-spike (ไม่มีเครือข่าย)
**เป้าหมาย:** พิสูจน์ว่าปลั๊กอินสั่งยูนิตที่ถึงตาให้ move/attack ผ่าน method ของเกมได้ บนเซฟสำเนา (host เท่านั้น) ถ้าไม่ผ่าน ให้กลับมาทบทวนแผนก่อนสร้างเครือข่าย

**Deliverables:** UnitCommandSpike (ปุ่มลัด), TurnGate, Executor v1, audit log, CoopSpike.md

**สำรวจ just-in-time** [v2]
- เมื่อยูนิต playerControl ถึงตา เกมหยุดรอ input จริงไหม ทดสอบทั้งตอนเร่งเวลาและไม่เร่ง
- พฤติกรรมของ battleTime / GetBattleDeltaTime / battleMove / ManageHeroSkillCdTime ระหว่างรอ: ศึกเป็นสลับเทิร์นล้วน หรือมีเวลาเดินแบบ ATB
- battlePaused หยุดศึกได้โดยไม่ทำให้ค้าง และปลดแล้วเล่นต่อได้
- ลำดับ method ที่ UI เรียกตอนผู้เล่นสั่ง move/attack

**Exit criteria**
1. move/attack สำเร็จ 10/10
2. ปฏิเสธถูกต้อง 20/20 (ไม่ใช่ตา, นอกระยะ)
3. ไม่มี soft-lock
4. ยืนยันว่าเกมหยุดรอ input ตอนถึงตายูนิตผู้เล่น ทั้งเร่งและไม่เร่งเวลา (หรือรายงานว่าไม่หยุด พร้อมผลกระทบต่อ S3) [v2]
5. battlePaused เปิด/ปิดได้ 10/10 โดยไม่ค้าง [v2]
6. hash เซฟจริงไม่เปลี่ยน
7. panic switch coop.allowRemoteInput=false ใช้ได้

**ความเสี่ยง:** soft-lock, marshalling ของ IL2CPP ผิด, ศึกเป็น ATB จนต้องออกแบบ S3 ใหม่ [v2]
**Rollback:** coop.allowRemoteInput=false, คืนเซฟ

## 5. S2 — Spectator
**เป้าหมาย:** ส่ง state ของ host ไปให้ guest ดูผ่านเครือข่าย guest แสดงผลแบบ overlay อย่างเดียว

**Deliverables**
- transport TCP (ใช้ผ่าน VPN เสมือน LAN: ZeroTier/Tailscale/Radmin) + named pipe สำหรับทดสอบในเครื่อง
- Protocol v1 (host → guest เท่านั้น): HELLO{protoVer, modVer, gameVer, unityVer, fieldHash}, WORLD, BATTLE, LOG, PING/PONG, BYE
- รูปแบบการส่ง [v2]:
  - ศึก: ส่ง event/delta ทุกครั้งที่ action จบ (ใครทำอะไร, ผล, HP/ตำแหน่งที่เปลี่ยน, log) ไม่ส่งตามรอบเวลา
  - world map: ส่ง delta ของตำแหน่ง/เวลาเมื่อเปลี่ยน โดยจำกัดความถี่ไม่เกินที่จำเป็นต่อการแสดงผล
  - snapshot เต็ม + fieldHash เป็นระยะ (เช่น ทุก N action หรือทุกไม่กี่วินาที) ไว้ตรวจ desync และให้ guest ที่เพิ่งเข้าตามทัน
  - ลำดับข้อความมีเลข seq ถ้าขาดให้ขอ snapshot เต็มใหม่
- guest overlay ด้วย OnGUI, ตัววัดของ viewer

**Exit criteria**
1. latency ต่อ event p95 ≤ 150 ms ผ่าน VPN [v2]
2. bandwidth ≤ 30 KB/s (เพดาน; คาดว่าเฉลี่ยต่ำกว่านี้มาก) [v2]
3. fieldHash ของ host กับ guest ตรงกันทุกครั้งที่มี snapshot เต็ม (divergence = 0)
4. ข้อความขาด → guest ขอ snapshot เต็มแล้วกลับมาตรงกันได้ [v2]
5. ยืนยันว่า guest อ่านอย่างเดียว ไม่เรียก method ของเกม

**ความเสี่ยง:** stable ID ซ้ำ, ความแม่นยำ float, ประสิทธิภาพ IMGUI, event หายระหว่างทาง
**Rollback:** role=off หรือปิด transport

## 6. S3 — ช่องคำสั่งในศึก
**เป้าหมาย:** guest ส่งคำสั่งยูนิตที่ตัวเองคุม host ตรวจแล้วรันผ่าน method ของเกม

**Deliverables**
- Protocol v2: CMD, CMD_RESULT
- CommandValidator (host) ตรวจก่อนแตะเกม: เป็นตาของยูนิตนั้นจริง, side == player, ยูนิตมีชีวิต/ไม่ติดสถานะ, target อยู่ใน range, skill พร้อม/มี cost/ไม่ติด cooldown, phase == ช่วงรับคำสั่งของผู้เล่น, ไม่มี modal ค้าง — ใช้ตัวตรวจของเกมก่อน แล้วค่อยเสริมของเรา
- CommandExecutor (host) ต่อยอดจาก Executor ของ S1.5
- timeout: เกิน deadlineMs → auto เฉพาะยูนิตนั้น เฉพาะเทิร์นนั้น แล้วคืนการควบคุม
- control map (ยูนิตไหนใครคุม), audit log

**Exit criteria:** คำสั่งสำเร็จ ≥ 45/50, ปฏิเสธคำสั่งผิด 20/20, timeout → AI 20/20, p99 ≤ 8 ms, ไม่มี soft-lock
**ความเสี่ยง:** soft-lock, animation/timeScale ไม่ตรงกัน
**Rollback:** coop.allowRemoteInput=false, ล้าง control map

## 7. S5 — ความทนทาน
**เป้าหมาย:** ทนต่อ save/load, หลุดการเชื่อมต่อ, เวอร์ชันไม่ตรง และมีชุดทดสอบอัตโนมัติ

**Deliverables**
- handshake ด้วย fieldHash + VERSION.md
- session resume/reset
- fault injection matrix: ตัด TCP, ระงับ process guest, frame เสีย, cmdId ซ้ำ, clientTick เก่า
- coop/test/: driver python สตาร์ท host+guest บนสำเนา ยิง scenario ผ่าน IPC เก็บ log ออกรายงาน pass/fail; และ --selftest ใน mod

**Exit criteria:** scenario ผ่าน 15/15, host crash = 0, version mismatch ถูกจับได้ทุกครั้ง, soak 60 นาที, เซฟสมบูรณ์ (hash)
**ความเสี่ยง:** UnityEngine.Random ไม่มี seed, handle รั่ว, ML unload ไม่สะอาด
**Rollback:** ย้อน DLL ตามเวอร์ชัน, คืนเซฟ

## 8. S4 — World follow (opt-in)
**เป้าหมาย:** guest ตามปาร์ตี้ host บนแผนที่ ใช้โหมด path-follow เท่านั้น (ตัดโหมด teleport bigMapPos ออก)

**Deliverables**
- Protocol v3: WORLD_AUTH, ENCOUNTER
- guest path-follower: เรียก SetPlayerMoveTargetArea ไปยัง waypoint ของ host
- clock mirror (แสดงผล), ติดตาม modal

**สำรวจ just-in-time** [v2]
- วัด WorldData.hour ซ้ำตอนปาร์ตี้กำลังเดิน (ผลเดิม Δhour = 0 วัดตอนอยู่นิ่ง 2 วินาที)
- หาจุดที่เขียน WorldData.hour (ตอนนี้ยังเป็น GUESS)
- หา chain จากปะทะบนแผนที่ (MeetBigMapNpc) ไปถึง PrepareBattleMap (ผลสแกน 338 hit เดิมเชื่อไม่ได้)
- modal ที่หยุดโลก: ร้านค้า บทสนทนา พล็อต งาน (ตอนนี้ยังเป็น GUESS)

**Exit criteria**
1. drift ของ path-follow ≤ 2 map units
2. เวลาที่แสดงตรงกันระหว่าง host/guest
3. guest ไม่ทำให้เกิดศึกเอง (0 ครั้ง)
4. ตรวจจับ modal ได้ 9/10
5. เซฟของ guest ไม่เปลี่ยน
6. รันชุดทดสอบ S5 ซ้ำทั้งชุดหลังจบ S4 และผ่านเท่าเดิม [v2]

**ความเสี่ยง:** ชนกับเซฟ/บัญชี Steam ของ guest, rubber-banding, path กระตุก
**Rollback:** coop.follow=false

## 9. S6 — วิจัย (ไม่มีโค้ด)
**เป้าหมาย:** ศึกษาว่าปาร์ตี้ guest เดินอิสระคนละที่ได้ไหม แล้วสรุป GO/NO-GO
**Deliverable:** RESEARCH-independent-party.md
**ความเสี่ยง:** scope creep, สรุป GO จากข้อมูลไม่พอ
**Rollback:** ลบโฟลเดอร์วิจัย

## 10. ความเสี่ยงรวม
| # | ความเสี่ยง | ผลกระทบ | วิธีลด | ตรวจได้โดย |
|---|---|---|---|---|
| 1 | Harmony กับ method ที่ถูก inline | hook ไม่ทำงานบางจุด | เทียบจำนวนที่จับได้กับบนจอ, hook ชั้นที่ไม่ถูก inline | capture rate ของ S1 |
| 2 | Thread safety ของ IL2CPP | crash แบบสุ่ม, state พัง | คิวเดียว + assert main thread + MainThreadGuard | guard throw ทันที, soak 60 นาที |
| 3 | Frame budget | เกมกระตุก | budget 4 ms, วัด p99 ทุก stage | ตัววัดใน mod |
| 4 | เซฟพัง / ชนกับ Steam | เสียเซฟจริง | โปรโตคอล §1.2, guest ห้าม save | hash ก่อน/หลัง |
| 5 | เกมอัปเดต [v2] | offset/RVA/interop เปลี่ยนทั้งหมด | VERSION.md + เทียบ hash ทุก session | hash ไม่ตรง → หยุด |
| 6 | ชนกับ LongYinThai [v2] | log/ข้อความผิด, เกม error | ตรวจรายการ patch ใน S0, ลำดับ postfix ใน S1 | compat-notes.md |
| 7 | ศึกไม่ใช่เทิร์นล้วน (ATB) [v2] | ต้องออกแบบ S3 ใหม่ | พิสูจน์ใน S1.5 ก่อนสร้างเครือข่าย | exit criteria 4 ของ S1.5 |

## 11. การตัดสินใจ
- D1: viewer overlay ก่อน, S4 เป็น opt-in ทำท้ายสุด
- D2: S4 ใช้ path-follow เท่านั้น ไม่ teleport
- D3: S3 สั่งได้เฉพาะตอนถึงตายูนิตนั้น
- D4: ศึกแยกจอฝั่ง guest ย้ายไป S6 (วิจัย)
- D5: guest ห้าม save ตลอดไป
- D6: named pipe ในเครื่อง + TCP ผ่าน VPN เท่านั้น
- D7: UI ภาษาอังกฤษก่อน แยก string ไว้ใน UIStrings.cs
- D8: รองรับ 30 ยูนิตต่อฝั่ง (รวม 60), latency p95 150 ms

## 12. Changelog
- v1: ฉบับแรก + S1.5 (S3-spike) + แก้ตารางสิทธิ์ Harmony ของ S0
- v2: S0 เปลี่ยนเป็น "ไม่แตะ state ของเกม", S2 เปลี่ยนเป็น event/delta + snapshot เต็มเป็นระยะ, S1.5 เพิ่มการพิสูจน์ว่าเกมหยุดรอ input และตรวจ battleTime/battleMove/battlePaused, S4 เพิ่มงานสำรวจ hour/encounter chain และต้องรัน S5 ซ้ำ, S0 เพิ่ม exit criteria 4 ข้อ, เพิ่มกฎ interop แบบมีชนิด, VERSION.md, repo แยก, ความเสี่ยง #5–7

# แผนย่อย S1.5 — S3-spike (ไม่มีเครือข่าย) · **รออนุมัติ ยังไม่เริ่ม**

อ้างอิงบังคับ: `STAGEPLAN.md` §4 (deliverable + JIT 1–8 + exit criteria 1–9) · §1.1 ตารางสิทธิ์ · §1.2 สำเนาเซฟ · §1.4 threading · §1.5 rollback
สถานะก่อนเริ่ม: S1 ปิดแล้ว (tag `stage-S1`, 7 PASS · 2 PASS-with-limitation) · ยังไม่มีคำสั่งใดในแผนนี้ถูกเรียกในเกม

ป้ายกำกับ: **[RUNTIME]** วัดแล้ว · **[DUMP]** อ่านจาก dump · **[GUESS]** ยังไม่ยืนยัน · **[SPEC]** กติกาของเรา

---

## 0. ขอบเขตที่เขียนได้ / ไม่ได้ (ยึด §1.1)

| ทำได้ใน S1.5 | ทำไม่ได้ |
|---|---|
| อ่าน field ผ่าน interop แบบมีชนิด | เขียน field ของเกม (S1.5 ช่องนี้ ❌) |
| เรียก getter บริสุทธิ์ | เรียก `GameDataController.Save/Load` |
| Harmony patch **อ่าน argument เท่านั้น** (ถาวรได้แล้วใน S1) | รับคำสั่งจากเครือข่าย (ไม่มี transport ใน stage นี้) |
| เรียก method ที่เปลี่ยน state: **เฉพาะ `move`/`attack` ของยูนิตที่ถึงตา** + `battlePaused` toggle (exit 5) | แตะ world map / เวลาโลก / NPC / economy / Steam / achievement / cheat flag |
| เขียน log/pref/audit ของ mod เอง | เขียนไฟล์ใต้ `Save/` หรือแก้สำเนาเซฟระหว่างเล่น |

**กติกาเหล็กของ stage นี้**
1. คำสั่งเปลี่ยน state ทุกครั้งต้อง **armed** ก่อน (ดู §3 G0) — ไม่มี armed = ปฏิเสธ ไม่มีข้อยกเว้น
2. **1 คำสั่งต่อ 1 ครั้ง** ห้ามวนลูป ห้าม batch ห้าม retry อัตโนมัติ (ผู้สั่งต้อง arm ใหม่ทุกครั้งหลังล้มเหลว)
3. ทุกคำสั่งรันบน Unity main thread ผ่าน `MainThreadDispatcher.Enqueue` เท่านั้น (§1.4)
4. ถ้าลำดับ call ที่ JIT ได้ **ต้องเขียน field** → หยุดถามทันที (นอกสิทธิ์ S1.5) แล้วเสนอทางเลือก: เลื่อนไป S3 หรือขออนุมัติเป็นกรณีพิเศษ
5. ทดสอบบนสำเนาเซฟเท่านั้น — ไม่มี armed ถ้า P0 ยังไม่ผ่าน

---

## 1. P0 — save-split + ซ้อมพิสูจน์ว่าเกมโหลดสำเนา (gate แรก บล็อกทุกอย่าง)

### 1.0 Pre-flight 2.5 — ตรวจสถานะเซฟก่อนทำอะไร (บังคับ, ต้องผ่านก่อน `split` ทุกครั้ง)

รันชุดคำสั่งใน `evidence/preflight-save-state-20261020.md` (อ่านอย่างเดียว) แล้ว **หยุดและรายงานทันที** ถ้าเข้าเงื่อนไขข้อใดข้อหนึ่ง:

| เงื่อนไข | ผล |
|---|---|
| มี `LongYinLiZhiZhuan_Data/Save.real-*` หรือ `Save.used-*` ค้างอยู่ | **ห้าม `split`** — `Save` ยังไม่ใช่ของจริง |
| `Save` hash ไม่ตรง `coop-test/S1-20261020-113926/savehash.real.before.txt` ครบ 57/57 | **ห้าม `split`** — ต้อง restore และ rehash ให้ตรงก่อน |
| ยังมี `LongYinLiZhiZhuan.exe` ใน process list | **ห้าม `split`/`restore`** |

**ผลตรวจ 2026-10-20 14:04 [RUNTIME]:** ❌ ยังไม่ผ่าน — `Save.real-S1` ค้างอยู่ และ `Save` ที่ mount = สำเนา S1 (ตรง `savehash.copy.before.txt` 56/57, ต่างเฉพาะ `PlayerprefData.dat`) · `Save.real-S1` = ของจริง 57/57 ตรง baseline
→ ขั้นแรกของ S1.5 จึงเป็น **`restore`** (rename กลับ + rehash) ไม่ใช่ `split`; รายละเอียดใน `evidence/preflight-save-state-20261020.md`

### 1.1 ข้อเท็จจริงที่มีอยู่
- โฟลเดอร์เซฟ = `LongYinLiZhiZhuan_Data/Save/` มี `SaveSlot0`–`SaveSlot10` + `PlayerprefData.dat` (57 ไฟล์) **[RUNTIME S0/S1]**
- S0 ใช้วิธี "สลับโฟลเดอร์" ไม่ใช่ redirect (`GameDataController.saveDataPath` ยังไม่พบ) → วิธีเดิมใช้ต่อ แต่ต้อง **ทำให้เป็นสคริปต์ที่รันซ้ำได้ + มี proof ว่าเกมอ่านสำเนาจริง** **[SPEC]**
- `PlayerprefData.dat` volatile (D10) — แต่ถ้าเกมไม่เคยชี้ path จริง ไฟล์จริงจะไม่ถูกแตะเลย **[RUNTIME S1: 57/57 ตรงหลังปิดเกม]**

### 1.2 เครื่องมือ: `tools/save-split.py` (Python 3 stdlib ล้วน — ไม่เพิ่ม dependency)
คำสั่งย่อย:

| คำสั่ง | ทำอะไร |
|---|---|
| `status` | แสดงว่าตอนนี้ `Save/` เป็นของจริงหรือสำเนา, มี `Save.real-*` / `Save.copy-*` ค้างกี่ชุด, process เกมรันอยู่ไหม |
| `split --label S1.5-<ts>` | 1) ปฏิเสธถ้า `LongYinLiZhiZhuan.exe` ยังรัน 2) hash `Save` จริง → `coop-test/<label>/savehash.real.before.txt` 3) copy ทั้งก้อน → `coop-test/<label>/save/` 4) **rename** `Save` → `Save.real-S1.5-<ts>` (ไม่ copy ซ้ำ: atomic และไม่มีทางที่สำเนากับของจริงปนกัน) 5) copy `coop-test/<label>/save/` → `Save` 6) hash สำเนาที่ mount → `savehash.copy.before.txt` 7) เขียน `MANIFEST.json` (label, ts, จำนวนไฟล์, hash ของทั้งสามชุด) |
| `verify` | rehash `Save` ที่ mount + `Save.real-*` เทียบกับ baseline → รายงาน changed/missing/added แยกกลุ่ม `SaveSlot*/` กับ `PlayerprefData.dat` |
| `restore` | 1) ปฏิเสธถ้าเกมรัน 2) `verify` ก่อน (ต้องแน่ใจว่า `Save` คือสำเนา ไม่ใช่ของจริง) 3) rename `Save` → `Save.used-<ts>` (เก็บไว้ audit) 4) rename `Save.real-S1.5-<ts>` → `Save` 5) rehash เทียบ `savehash.real.before.txt` → ต้อง changed 0 / missing 0 / added 0 6) รายงาน path ที่เหลือ |
| `list` | ประวัติ split/restore ทุกครั้งพร้อมผล |

**หมายเหตุการออกแบบ:** `restore` ใช้ rename กลับ ไม่ copy ทับ → ของจริงไม่เคยถูกเขียนแม้แต่ไบต์เดียวตลอด stage

### 1.3 proof ว่า "เกมโหลดสำเนา" (ไม่ใช่ของจริง) — 2 หลักฐานอิสระ
- **P-a [RUNTIME, บนจอ] sentinel slot:** ก่อนเปิดเกม ซ่อน `SaveSlot<k>/` ในสำเนาโดย rename เป็น `.hidden-slotk` (เลือก k = สล็อตที่ว่าง/ไม่ใช้ — ตรวจจาก `SaveSlot<k>/Info` ตอน split; ถ้าทุกสล็อตมีข้อมูล ให้ถามก่อนว่าเลือกสล็อตไหน) → เปิดหน้าโหลดในเกม **สล็อต k ต้องหายไปจากรายการ** (ภาพหน้าจอ) → คืนชื่อโฟลเดอร์แล้วโหลดได้ตามปกติ ถ้าเห็นสล็อต k = เกมอ่านของจริง → **หยุดทันที**
- **P-b [RUNTIME, ไฟล์] ของจริงไม่ขยับ:** หลังเล่น/spike ทั้งเซสชัน `verify` ต้องได้ `SaveSlot*` changed 0 และ hash ของ `Save.real-*` ตรง baseline (mtime ก็ต้องไม่เปลี่ยน — ใช้เทียบซ้ำ)
- **P-c (ประกอบ):** `Save/PlayerprefData.dat` ของสำเนามี mtime/hash ใหม่หลังเล่น ขณะที่ของ `Save.real-*` ไม่มี → ชี้ว่า writes ลงสำเนา

### 1.4 เกณฑ์ผ่าน P0 (dry-run 2 รอบ ก่อน spike จริง)
1. `split` → `verify` → `restore` ทำ 2 รอบติดกันสำเร็จ โดย **ไม่มีคำสั่งเกมใดถูกเรียกเลย**
2. หลังแต่ละ `restore`: `Save` = ของจริง 57/57 hash ตรง baseline, changed/missing/added = 0, ไม่มี `Save.real-*`/`Save.used-*` ตกค้างในที่ที่ทำให้รอบถัดไปสับสน (เก็บใน `coop-test/<label>/` แทน)
3. รอบที่ 3: `split` → เปิดเกมจริง → เห็น sentinel (P-a) → ปิดเกม → `verify` (P-b) → `restore` → hash ตรง
4. `PlayerprefData.dat` ของจริง hash ตรง baseline ตลอด (ไม่ใช่แค่ `SaveSlot*`)
5. ถ้าข้อใดไม่ผ่าน → **S1.5 ไม่เริ่ม** และรายงาน

---

## 2. ลำดับขอบเขตการเขียน (เริ่มจาก move, ทีละคำสั่ง)

ทุกขั้นต้องผ่าน gate ของขั้นก่อนหน้า และมี `Audit` ก่อน/หลังของตัวเอง ไม่มีขั้นไหนรวบหลายคำสั่ง

| ขั้น | สิ่งที่ทำ | คำสั่งเปลี่ยน state? | ผ่านเมื่อ |
|---|---|---|---|
| **S1.5-a** | P0 (§1) — เริ่มด้วย **2.5 pre-flight check** → `restore` ให้ `Save` เป็นของจริง → `split` → dry-run → sentinel | ❌ | เกณฑ์ §1.4 ครบ |
| **S1.5-b** | `TurnGate` + `spike status` — อ่าน `battleState`, `nowActiveState`, `nowActiveUnit`, `moved/attacked`, `battlePaused` แล้วตอบ "ถึงตายูนิตนี้ไหม" + `stateHash` | ❌ | อ่านตรงกับ UI 20/20 เฟรม และไม่มีการเรียก method ใดที่เปลี่ยน state (`postfix.calls` ของ method กลุ่ม action = 0) |
| **S1.5-c** | **JIT** (§6 J1–J8) + `probe` hook บันทึกลำดับ call ของ UI จริง (Harmony อ่าน argument เท่านั้น) | ❌ (อ่าน/วัด) | ได้ลำดับ call จริงของ move 1 ครั้ง + attack 1 ครั้ง พร้อม argument, ระบุ entry method ได้, และรู้ว่าต้องเขียน field ไหม |
| **S1.5-d** | `spike move <stableId> <row> <col>` — **คำสั่งเปลี่ยน state คำสั่งแรก** | ✅ 1 ครั้ง/คำสั่ง | 10/10 (exit 1) + ไม่มีค้าง |
| **S1.5-e** | `spike attack <stableId> <targetStableId>` | ✅ | 10/10 (exit 2) |
| **S1.5-f** | negative tests 20 ครั้ง (§4) | ❌ ทั้งหมด (ต้องถูกปฏิเสธ) | 20/20 ปฏิเสธถูก code + `stateHash` ไม่เปลี่ยน |
| **S1.5-g** | `battlePaused` toggle + ATB/เวลาศึก (§6 J5–J7) + 10 นาทีเล่นต่อ | ✅ (เฉพาะ pause) | exit 4, 5, 6, 7 |
| **S1.5-h** | ปิด stage: รายงาน, `CoopSpike.md`, tag `stage-S1.5` | — | GO/NO-GO table §7 |

**การ mount/ถอด:** `spike` ทั้งหมดอยู่หลัง pref `coop.allowRemoteInput` (default **false**) + prefix คำสั่ง `spike` ต้องมี `coop.devCommands=true` — ถ้าปิดตัวใดตัวหนึ่ง คำสั่งตอบ `DISARMED` และ **ไม่แตะ IL2CPP เลย**

> **อนุมัติ 2026-10-20 ก่อน S1.5-b (ผู้ใช้สั่ง):** `spike status` เป็น read-only จึง **ยกเว้น** จากข้อบังคับข้างบน — ต้องการเพียง `coop.enabled` + `coop.ipc` + `coop.devCommands`; ไม่ต้องมี `coop.allowRemoteInput` และไม่ต้อง arm; pref ยังคง default **false** และคำสั่งเปลี่ยน state (S1.5-d ขึ้นไป) ต้องผ่าน pref + armed ตามเดิม · ใน build S1.5-b **ไม่มี** move/attack/pause/arm ลงทะเบียนเลย (`spike <other>` → error ทันที ไม่แตะ IL2CPP)

**ชื่อ/พารามิเตอร์ (SPEC):**
```
spike status                      -> { inBattle, battleState, nowActiveState, active, moved, attacked, paused, stateHash }
spike arm <seconds>               -> ตั้ง armed flag ในหน่วยความจำ (default 120 s, เพดาน 300 s); ปิด pref หรือครบเวลา = หลุดเอง
spike move <stableId> <row> <col>
spike attack <stableId> <targetStableId>
spike pause                       -> BattlePauseButtonClicked() เท่านั้น (ไม่มีอย่างอื่น)
spike disarm
```
`stableId` = ตัวเดียวกับที่ `BattleSampler` ใช้แล้ว (`teamId/heroID/index`) เพื่อให้ audit ของ S1 และ S1.5 join กันได้

---

## 3. Guard ก่อนสั่ง (เรียงตามลำดับที่เช็ค — ล้มข้อไหนตอบ code นั้น ไม่เรียก method ใด)

| # | Guard | ตอบเมื่อไม่ผ่าน |
|---|---|---|
| G0 | `coop.enabled && coop.ipc && coop.devCommands && coop.allowRemoteInput` **และ** armed ยังไม่หมดอายุ | `DISARMED` |
| G1 | เป็น host (`role=host`, ค่าเดียวใน stage นี้) และไม่มี guest | `WRONG_ROLE` |
| G2 | `BattleController.Instance != null` และ `battleState == Fighting` และ `inited` | `NO_BATTLE` |
| G3 | `nowActiveUnit != null` และ `nowActiveState == WaitPlayerInput` และ **`nowActiveUnitMoved` / `nowActiveUnitAttacked`** ยังไม่ถูกใช้เกินสิทธิ์ของคำสั่งนั้น · ⚠️ **ห้ามใช้ `BattleUnit.moved` / `BattleUnit.attacked` เป็นตัวตัดสิน** — วัดแล้วเป็น `true` **0 จาก 82,695 sample** แม้ยูนิตเดิน/โจมตีจริง (S1.5-b, `evidence/S1.5b-20261020-144353/S1.5b-report.md` §3) **[RUNTIME]**; ตัวชี้วัดที่ใช้ได้คือ turn flags ของ controller เท่านั้น แต่ยังคงป้าย **[GUESS]** จนกว่าจะเทียบกับ UI ตอนผู้เล่นเดินเองในเทิร์นของตัวเอง (ข้อจำกัด L3 ของ S1.5-b) | `NOT_YOUR_TURN` / `ALREADY_USED` |
| G4 | ยูนิตที่สั่ง = `nowActiveUnit` เป๊ะ (เทียบ `stableId`) และ `playerControl == true` และ team = `GetPlayerControlTeamID()` | `NOT_ACTIVE` / `NOT_PLAYER` |
| G5 | ไม่มีคำสั่ง in-flight, `playingAnim == false`, ผ่านมา ≥ 300 ms จากคำสั่งก่อน, budget ≤ 20/นาที และ ≤ 200/เซสชัน | `BUSY` / `BUDGET` |
| G6 | ปลายทาง: cell อยู่ใน `movePath`/`moveRangeGridUnits` (move) หรือ target อยู่ใน `attackRangeGridUnits` และ `alive` (attack) — อ่านจากเกม ไม่คำนวณเอง | `OUT_OF_RANGE` / `TARGET_DEAD` |
| G7 | denylist `Steam*`/`APIDispatcher`/`DnsChecker`/`Achievement*`/`PlayerStatsStored` + ห้าม `Save/Load` (มีอยู่แล้วใน `MainThreadDispatcher`) | `DENIED` |

> **ผลวัด 2026-10-20 (W1) — แก้สมมติฐานของ G3 [RUNTIME]:** `nowActiveUnitMoved` ใช้ได้ (ปิด L3 แล้ว: 39 คลิกที่ผู้เล่นกดเองทำให้ยูนิตไปยืนช่องนั้นพร้อม `turnMoved=true` ค้าง 21–824 frame, lag 7–279 frame) แต่ **`nowActiveUnitAttacked` ใช้ตัดสินไม่ได้**: `turnAttacked=true` เกิดเฉพาะตอน `isPlayerTurn=false` (**1,005/1,005 sample**) และ **ไม่เคย true ขณะ `isPlayerTurn=true` (0/10,177)** → G3 ของคำสั่ง attack ต้องหาตัววัดอื่น (hp ของ target / `attackComfirmed` flag) ก่อนเริ่ม S1.5-d · หลักฐาน `evidence/S1.5c-20261020-155236/W1-report.md` §3.1
>
> **ผลวัด 2026-10-20 (W4) — J6 ปิดแล้ว [RUNTIME]:** `BattlePauseButtonClicked(GameObject "Stop")` toggle `battlePaused` **สลับ true/false ครบ 10/10 ครั้ง** (pause 5 / resume 5) · `Time.timeScale` = 1 ตลอด → pause ไม่ได้ทำผ่าน timeScale · ระหว่าง pause `battleTime` ไม่เดิน (6.33 s ที่ state=Moving: 2.283 → 2.283) → **`battlePaused` ใช้เป็นตัววัด pause ได้** · **แก้ข้อสรุป W1:** ที่ W1 เห็น `paused=0/54,473` เพราะรอบนั้น**ไม่มี hook บนปุ่ม** จึงยืนยันไม่ได้ว่าการกดลงทะเบียน ไม่ใช่เพราะฟิลด์ไม่สะท้อน pause · หลักฐาน `evidence/S1.5c-w2w4-20261020-1643/W2-W4-report.md` §2
>
> **ผลวัด 2026-10-20 (W5) — hp ที่เลือกอ่านไม่ใช่ hp ในศึก [RUNTIME]:** `HeroData.hp` **ไม่เปลี่ยนเลย** ตลอด 8,945 sample แม้มี `Attacking` 6.13 s → ใช้ยืนยันผล `spike attack` ไม่ได้ และ `stateHash` ปัจจุบันก็ไม่ไวต่อดาเมจ [DUMP] ตัวเลือกที่ยังไม่ทดสอบ: `BattleUnit.originHp` (`0xC0`) + `ChangeHp/ChangeTrueHp/IsAlive` — ต้องขออนุมัติอ่านเพิ่มก่อนสรุป
>
> **ผลวัด 2026-10-20 (J7) — ไม่ใช่ ATB ที่ความเร็วปกติ [RUNTIME]:** `WaitPlayerInput` รวม 787.24 s (`battleTime` +0.000, ช่วงยาวสุด 722.29 s ที่ turn 19) และ `turnIndex` ไม่ขยับเอง → เกมหยุดรอ input · `battleTime` เดินเฉพาะช่วง `Attacking`/`Rest`/`Finish`/`None` (ratio 0.04–0.27) = นาฬิกาแอนิเมชัน · **ฝั่งเร่งความเร็วยัง NOT-RUN** เพราะ `Time.timeScale` = 1 ทั้งรอบ (ไม่เห็นกลไกเร่ง)

**GUESS ที่ต้องปิดใน S1.5-c:** การอ่าน `movePath`/`moveRangeGridUnits` เป็นตัวตัดสิน range อาจไม่ตรงกับที่ UI ใช้จริง (range ขึ้นกับสกิล/อุปกรณ์) — ค้นแล้วไม่มี helper ที่ใช้ได้: `CanMoveTo` **ไม่มีใน dump (0 hit)**; ที่ใกล้สุดคือ `HeroData.CanMove()` (`dump.cs:370503`) และ `ForceData.CanAttack(int)` (`dump.cs:369518`) ซึ่งไม่ใช่ range check ของยูนิตในศึก [DUMP] → G6 จึงใช้ "cell อยู่ใน `moveRangeGridUnits` / target อยู่ใน `attackRangeGridUnits`" ตามที่วัดได้ และบันทึกเป็นข้อจำกัด

---

## 4. Negative tests (exit criterion 3) — 20 ครั้ง, ไม่ retry

ทุกครั้ง: `stateHash` ก่อน → ยิงคำสั่ง → รอ 500 ms → `stateHash` หลัง → ต้อง **เท่ากันเป๊ะ** และไม่มี `Audit.applied` record

> **แก้ตาราง 2026-10-21 (อนุมัติแล้ว) — [RUNTIME + โค้ด]:** ตารางเดิมกำหนด `NOT_PLAYER` ×4 และ `BUSY` ×4 ซึ่ง build ปัจจุบันยิงไม่ถึง:
> - `NOT_PLAYER` — guard `active.playerControl` (`MoveCommand.StartMove` §"G4", `AttackCommand.StartAttack`) อยู่ **หลัง** guard `nowActiveState != WaitPlayerInput/WaitAttacking` เสมอ → ในสอง state นั้น active unit เป็นฝ่ายผู้เล่นเสมอ ⇒ guard นี้เป็น defensive-only
> - `BUSY` — guard `_job != null` แต่ IPC ทำงานทีละคำสั่ง (`WaitDone` บล็อกจน job จบ) → ยิงซ้อนต้องใช้ 2 client พร้อมกัน ซึ่งตัวเองเปลี่ยน state ทำให้เทียบ `stateHash` ไม่ได้
>
> ⇒ เปลี่ยน 8 เคสนั้นเป็นเคสที่ยิงถึง guard จริงและ hash นิ่ง (**ไม่เลื่อนเกณฑ์: ยัง 20 เคส, hash ต้องเท่าเดิมทุกเคส**) · runner: `tools/negative-tests.py`

| # | กลุ่ม | เคส | code ที่ต้องได้ |
|---|---|---|---|
| 1–2 | DISARMED | ไม่ arm (move) · `arm 1` แล้วรอ 2 วิ (attack) | `DISARMED` |
| 3–6 | NOT_ACTIVE | move id ผิด ×2 · attack id ผิด ×2 | `spike move/attack: unit X is not the active unit` |
| 7–10 | OUT_OF_RANGE | attack เป้าที่ไกลเกินระยะ ×4 (คนละคู่) | `OUT_OF_RANGE` |
| 11–14 | TARGET_DEAD | attack corpse ×4 (corpse ในศึกเดียวกัน) | `TARGET_DEAD` — **แก้ 2026-10-21: ดูกล่องล่าง** |

> **ผลวัด 2026-10-21 (เคส 11) — guard TARGET_DEAD ยิงไม่ถึงกับศพจริง [RUNTIME + โค้ด]:** `AttackCommand.StartAttack` ตัดสินด้วย `targetHero != null && !targetHero.dead` แต่ศพในศึกมี `HeroData.dead = false` (status `alive=true`) พร้อม `hp=0`, `BattleUnit.IsAlive=false` (status `unitAlive=false`), grid `(-1,-1)` ⇒ guard ที่ทำงานจริงคือ **ช่องกริด**: code ที่ได้คือ `spike attack: no grid cell at (-1,-1)` (ไม่มีดาเมจ, `stateHash` เท่าเดิม)
> - **อนุมัติแล้ว (2026-10-21):** นับเคส 11–14 เป็น **PASS-with-limitation** ด้วย code ที่วัดได้จริง (`no grid cell at (-1,-1)`) — ไม่ rebuild กลาง stage
> - **ข้อบกพร่องขึ้นทะเบียน:** `TARGET_DEAD` ตายด้าน (dead code) เพราะใช้ฟิลด์ผิด → แก้เป็น `&& target.IsAlive` ในรอบถัดไป (ยังไม่แก้ตามคำสั่ง)
> - ผลข้างเคียงของฟิลด์เดียวกัน: `alive` ใน `stateHash` อิง `!HeroData.dead` ⇒ hash **ไม่สะท้อนการตาย** ของยูนิต (สอดคล้องข้อสรุป W5 ว่า hash ไม่ไวต่อดาเมจ)
| 15–18 | NOT_YOUR_TURN / state | move ขณะ `WaitAttacking` ×2 · attack ขณะ `WaitPlayerInput` ×2 | `nowActiveState=... (expected WaitPlayerInput)` / `NOT_ATTACK_MODE` |
| 19–20 | guard อื่น | attack ตัวเอง ×1 · move ไปช่องที่ไม่มีกริด (99,99) ×1 | `is the active unit` / `no grid cell at (99,99)` |

> **เกณฑ์ 9 (panic switch) — ทดสอบเชิงลบจริง:** `allowRemoteInput=false` → `spike move` / `spike attack` / `spike arm` อย่างละ 1 ครั้ง ต้องได้ `DISARMED: coop.allowRemoteInput=false (panic switch)` ทั้ง 3 + `stateHash` เท่าเดิม + ไม่มี applied

`stateHash` (SPEC) = SHA-256 ของข้อความ canonical จาก `BattleFrame` ที่มีอยู่แล้วใน S1: `battleState`, `nowActiveState`, `battlePaused`, `turnIndex`, `activeId`, `infoCount` + ต่อยูนิต: `stableId|side|teamIndex|row|col|hp(round 3)|alive` + **ระดับเทิร์น: `turnMoved|turnAttacked`** — **ไม่รวม** `moved|attacked` ระดับยูนิต เพราะวัดแล้วเป็น `true` 0/82,695 sample และไม่สะท้อนการเดินจริง **[RUNTIME S1.5-b]** · **ไม่รวม** `battleTime`/`frame`/`seq` เพราะเดินตลอดเวลา

> ⚠️ **หมายเหตุความไม่ตรงกันที่ยังค้าง:** build ที่ deploy อยู่ (`d5bf4de`) ยังคำนวณ `stateHash` ด้วย `moved|attacked` ระดับยูนิตตามข้อความเดิม — ค่าที่รายงานใน `spike status` จึงยังไม่อัปเดตตามข้อนี้ ต้องแก้โค้ดก่อน (ยังไม่แก้ตามคำสั่ง: เสนอ diff ก่อน) และ `turnMoved|turnAttacked` ที่จะใช้ตัดสินยังติดป้าย **[GUESS]** จนกว่าจะผ่านด่าน L3

---

## 5. Snapshot ก่อน/หลัง + ห้าม retry + watchdog

- ก่อนยิงทุกคำสั่ง: เขียน `UserData/CoopSpike/audit-<ts>/pre-<n>.json` (ทั้ง `BattleFrame` + `stateHash` + guard snapshot)
- หลังยิง: รอจน state **นิ่ง** (นิยาม: `nowActiveState == WaitPlayerInput` หรือ turn เปลี่ยน หรือ `playingAnim=false` ต่อเนื่อง 2 เฟรม) **timeout 8 s** (ปรับได้จาก J5 แต่ห้ามเกิน 15 s) → เขียน `post-<n>.json` + audit line: `unit, target, cell before/after, hp before/after, frame ที่ apply, latency accept→applied (ms), code`
- **ห้าม retry:** timeout หรือ exception = `FAILED` + `disarm` ทันที, ต้องให้คน arm ใหม่; ไม่มีการยิงซ้ำเองแม้แต่ครั้งเดียว
- ถ้า timeout: ไม่เดา ไม่ยิงซ้ำ → escalate ตาม §8
- ทุก audit line ผูก `seq` เดียวกับ capture stream เพื่อ join กับ `battle.ndjson` ได้

---

## 6. ข้อมูลที่ต้องสำรวจเพิ่ม (JIT) เพื่อเลือก method + จุดเรียก

ทั้งหมด **อ่าน/วัดอย่างเดียว** บนสำเนาเซฟ, ทำใน S1.5-c ก่อน S1.5-d

| # | คำถาม | วิธี | ตอบแล้วเอาไปทำอะไร |
|---|---|---|---|
| J1 | entry method ของ move/attack คือตัวไหนกันแน่ | **move รู้แน่แล้ว [DUMP `dump.cs:363169`]:** `BattleController.BattleGridClicked(GameObject clickedGrid)` RVA `0x7FA090`, public → ที่เหลือคือ 1) ยืนยันว่าเป็น handler ที่ต่อกับคลิกจริง (ไม่ใช่ทางตาย) 2) ต้องส่ง GameObject อะไร (ช่องจาก `gridUnits[i,j]`?) 3) ต้องมี pre-state อะไร 4) หา entry ของ attack ที่รับ target (ค้นต่อใน `BattleController` methods ที่รับ `GameObject`/`BattleUnit`/`GridUnitData`) | เลือกจุดเรียกจริง 1 จุด (ไม่เดา) |
| J2 | UI จริงเรียกอะไร เรียงลำดับไหน | Harmony **postfix อ่าน argument เท่านั้น** บน `BattleGridClicked(GameObject)`, `MoveButtonClicked`, `GenerateMovePath`, `CancelMove`, `RegretMove`, `ChangeActiveSkill`, `ChangeNowActiveAttackSkill`, `PlayBattleUnitMove*`/`PlayBattleUnitAttack*` → ผู้เล่นกดเอง 1 เทิร์น (**ห้าม hook `attackComfirmed` — เป็น bool field `0x210` ไม่ใช่ method** [DUMP `dump.cs:363082`]; อ่านเป็น flag ก่อน/หลังแทน) | ได้ลำดับ call + จังหวะเวลา ที่จะเลียนแบบ |
| J3 | ต้องตั้ง state/field ไหนก่อนเรียก entry method | อ่านค่าก่อน/หลังทุก call ใน J2 (ไม่เขียนอะไร) | ถ้าต้องเขียน field → **หยุดถาม** ตาม §0 ข้อ 4 |
| J4 | ช่องกริดถูกคลิกผ่านอะไร | ตรวจ `GridUnit`/`MapGrid` component: มี `OnClick`/`PointerClick`/`SendMessage` ไหม; ถ้าใช่ ใช้ `GameObject`-level invoke แทนการเรียก method ตรง | ทางเลือกที่แตะ state น้อยที่สุด |
| J5 | หลังสั่งแล้ว state machine ใช้เวลานิ่งกี่ ms | วัด transition ของ `nowActiveState`/`playingAnim` จากการกดเอง 10 ครั้ง | ตั้ง settle timeout ของ §5 |
| J6 | `battlePaused` ถูก toggle ด้วยอะไร | hook/อ่านค่าตอนกดเอง + ตอน AI | ใช้ exit 5 อย่างถูกตัว |
| J7 | ศึกเป็น ATB หรือเทิร์นล้วน | sample `battleTime`, `AIWaitTime`, `nowActiveState` ทุก 100 ms เป็น 30 s ตอน "ถึงตาเราแต่ไม่กดอะไร" ทั้งเร่งเวลาและไม่เร่ง (exit 4) | ถ้า time เดินและเทิร์นถูกตัดเอง = ATB → **NO-GO** และรายงาน (§7) |
| J8 | `SetAllAuto` ใช้เป็น fallback ได้ไหม | อ่าน signature/scope จาก dump ก่อน; **ยังไม่เรียก** — ถ้าจำเป็นต้องใช้จริงจะขออนุมัติแยก | ทางกู้กรณีค้าง (สำรองสุดท้าย) |

---

## 7. Exit criteria + GO/NO-GO

### 7.1 เกณฑ์ (ลอกจาก STAGEPLAN §4 — ไม่เลื่อน ไม่ลด)
| # | เกณฑ์ | เกณฑ์วัด |
|---|---|---|
| 1 | `spike move` 10/10 บนยูนิตที่ถึงตา | ยูนิตถึง cell ที่สั่ง (`row/col` ตรง), hp ไม่เปลี่ยน, exception 0 |
| 2 | `spike attack` 10/10 | target hp ลด (ค่าที่เกมคำนวณ — mod ไม่คำนวณเอง), exception 0 |
| 3 | ปฏิเสธถูก 20/20 | §4 ครบ + `stateHash` ไม่เปลี่ยนทุกครั้ง |
| 4 | เกมหยุดรอ input ตอนถึงตาเรา | ทั้งเร่งและไม่เร่งเวลา (J7) — ถ้าไม่หยุด ให้รายงานพร้อมผลกระทบต่อ S3 |
| 5 | `battlePaused` เปิด/ปิด 10/10 ไม่ค้าง | 10 รอบ: pause→resume, หลัง resume เกมเดินต่อ — **แก้ถ้อยคำ 2026-10-21: รอบที่วัดได้ต้องเป็นรอบที่ปุ่มกดได้จริง คือ `nowActiveState ≠ 2` (WaitPlayerInput)** ตามที่วัดได้ 29/29 click; ห้ามนับรอบที่กดตอนตาผู้เล่น (เกมไม่รับ) |
| 6 | หลังคำสั่ง 10 นาทีเล่นต่อได้ | 0 soft-lock, 0 exception, จบศึกได้ตามปกติ |
| 7 | main-thread cost p99 ≤ 8 ms/คำสั่ง | วัดด้วย `bench`-style timing ในคำสั่งจริง (ไม่รวมแอนิเมชัน) |
| 8 | เซฟจริงไม่ถูกแตะ | `verify` 57/57 ตรง, ไม่มี `Save/Load` ใน code path (log + review) |
| 9 | panic switch | `coop.allowRemoteInput=false` ระหว่าง spike → คำสั่งถัดไป `DISARMED` ทันที, UI ปกติใช้ได้ |

> **สถานะเกณฑ์ ณ 2026-10-20 22:10 (ปิดการวัดของ S1.5-c) [RUNTIME]**
> - ข้อ 4 **PASS** — เกมหยุดรอ input ทั้งสองความเร็ว: x1 รอ 43.0 s (`battleTime` +0.0000) · x10 รอ ≥123.8 s (วัดซ้ำ 151.4 s, `battleTime` +0.0000, `turnIndex` นิ่ง, `activeAutoFight=false`) · หลักฐาน `evidence/S1.5c-battleTimeScale-20261020-2143/RUN-speedup.md`
> - ข้อ 5 **PASS-with-limitation ตามมติผู้ใช้ (2026-10-21)** — **16 รอบ** pause→resume (hook `BattlePauseButtonClicked`: `paused` true 16 / false 16 สลับทุกครั้ง) · หลัง resume เกมเดินต่อ (`battleTime` 3.5999→5.2328, `turnIndex` 31→47) · pause หยุดนาฬิกาศึก (window 19.00 s และ 25.02 s ที่ `battleTime` นิ่ง) · หลักฐาน `evidence/S1.5c-originhp-run2-20261020-2102/RUN2-pause-J6.md`
>   - **limitation:** click ทั้ง 29/29 ครั้งอยู่ใน state 1/7/8/10/11/12 — **0 ครั้งที่ `WaitPlayerInput` (state 2)** · USER-OBSERVED: ปุ่ม "หยุด" กดไม่ได้ตอนตาผู้เล่น ⇒ นิยาม "10/10" ของเกณฑ์นี้ถูกแก้ให้ตรงกับเกมแล้ว (แถวข้อ 5 ด้านบน)
>   - **ข้อผูกพันถึง stage ถัดไป:** S3/S5/S4 **ห้ามเรียก/สั่ง pause ตอน `nowActiveState = WaitPlayerInput`** (เกมไม่รับ และจะทำให้การนับรอบเพี้ยน)
> - **แก้ขอบเขต J6:** ปุ่มหยุดลงทะเบียนเฉพาะตอน `nowActiveState != 2` (WaitPlayerInput) — 29/29 click อยู่ใน state 1/7/8/10/11/12 · การนับรอบจึงต้องเทียบเฉพาะ sample ที่ไม่ใช่ `WaitPlayerInput` (ผู้ใช้ยืนยันด้วยการสังเกตเอง: ปุ่มกดไม่ได้ในตาผู้เล่น)
> - **กลไกความเร็ว (J7):** ปุ่มเร่งเขียน `WorldData.battleTimeScale` (ค่า {1,3,5,10}) ไม่ใช่ `Time.timeScale` (คงที่ 1) · ที่ x10 เกมเร็วขึ้น 3.32× โดยรวม และแอนิเมชันสั้นลง ≥8.65× · ช่วงรอ/AI ไม่ถูกเร่ง
> - ข้อ 8 **PASS ต่อเนื่อง** — `Save.real-*` digest `2b0548fe464b` ตรง baseline ทุกครั้งที่ตรวจ (drift 0) · mounted `Save` เป็นสำเนา
> - ข้อ 1, 2, 3, 6, 7, 9 **NOT-RUN** — ต้องมีคำสั่ง `spike move`/`spike attack` จริง (S1.5-d/e) ซึ่งยังไม่ได้เริ่ม
> - **ตัววัดดาเมจที่ใช้ได้:** `units[].hp` (= `HeroData.hp`) ลดลงตามดาเมจจริง · `originHp` เป็น HP ตั้งต้น (คงที่) · `unitAlive` (`BattleUnit.IsAlive`) เป็นตัวบอกการตายในศึก (ส่วน `alive` ไม่สะท้อน) — ใช้ `unitAlive` ใน `stateHash` ด้วย

> **สถานะเกณฑ์ ณ 2026-10-21 (ปิด S1.5) [RUNTIME]**
> - ข้อ 1 **PASS** 10/10 (`evidence/S1.5d-20261020-2215/S1.5d-exit1-summary.md`) · ข้อ 2 **PASS** 10/10 (`evidence/S1.5e-20261021-0101/S1.5e-exit2-report.md`)
> - ข้อ 3 **PASS** 20/20 (limitation: เคส 11–14 ได้ `no grid cell at (-1,-1)` แทน `TARGET_DEAD` — อนุมัติแล้ว) · ข้อ 9 **PASS** 3/3 (`evidence/S1.5f-20261021-0247/S1.5f-report.md`)
> - ข้อ 6 **PASS-with-limitation** — 0 soft-lock / `ERROR|Exception`=0 ตลอด S1.5 · "จบศึกได้ตามปกติ" **ยังไม่ยืนยัน** ⇒ ยกไปเป็น **การตรวจบังคับของศึกรอบแรกใน S3** (ดู §7.3)
> - ข้อ 7 **PASS-with-limitation** — `CostMs` max 2 ms จาก 20 คำสั่ง (move 10 + attack 10), ไม่มีคำสั่งใดเกิน 8 ms; n=20 ยังไม่พอเป็น p99 ทางสถิติ
> - ข้อ 8 **PASS** — เซฟจริง `2b0548fe464b` 57/57 restore แล้ว (`evidence/S1.5f-20261021-0247/restore-report.md`)
> - **มติผู้ใช้ 2026-10-21: GO ไป S2** (tag `stage-S1.5`)

### 7.3 มติที่ผูกพัน stage ถัดไป (2026-10-21)

1. **pause**: ห้ามเรียก pause ตอน `nowActiveState = WaitPlayerInput` (เกมไม่รับ — 29/29 click เป็น state อื่น) · ใช้เฉพาะ state ที่ปุ่มกดได้
2. **`TARGET_DEAD` / `stateHash` — prerequisite ของ S3 (แก้ก่อนเขียน validator):**
   - `AttackCommand.cs:232` → เพิ่มเงื่อนไข `&& target.IsAlive` (ปัจจุบัน guard ตายด้าน: ศพมี `HeroData.dead=false`, `hp=0`, `BattleUnit.IsAlive=false`, grid `(-1,-1)`)
   - `stateHash` → ให้ `alive` มาจาก `BattleUnit.IsAlive` + `hp` (ปัจจุบันอิง `!HeroData.dead` ⇒ hash ไม่สะท้อนการตาย/ดาเมจ)
   - **ต้อง re-run negative tests ครบ 20 เคสบน build ใหม่** หลังแก้ (รวม 4 เคส `TARGET_DEAD` ที่ต้องได้ code จริง)
   - หนี้ร่วมที่ยกไป S3: prefire guard `attackRangeGridUnits`, `attackRangeCells` ใน `spike status`, log เส้นทาง fail ฝั่ง mod, `ALREADY_USED` ยัง NOT-RUN
3. **ข้อ 6 "จบศึกได้ตามปกติ"**: เป็น **การตรวจบังคับของศึกรอบแรกใน S3** — เล่นศึกหนึ่งหลังใช้คำสั่ง mod จนจบ (ต้องเห็น battle end) แล้วบันทึกผล; ยังไม่ต้องทำตอนปิด S1.5

### 7.2 GO / NO-GO
- **GO (ไป S2/S3):** ข้อ 1, 2, 3, 5, 6, 7, 8, 9 ผ่าน **และ** ข้อ 4 ได้คำตอบชัด (หยุด หรือ ไม่หยุด-but-อธิบายได้) โดย P0 (§1.4) ผ่านทุกข้อ
- **NO-GO:** ข้อ 1 หรือ 2 ต่ำกว่า 10/10 หลังลองลำดับ call ทางเลือก **ไม่เกิน 2 ทาง** (แต่ละทางต้องมี audit) → หยุด, รายงาน, ไม่เริ่ม S2
- **NO-GO (ATB):** J7 พบว่าเวลา/เทิร์นเดินเองระหว่างรอ → ต้องออกแบบ S3 ใหม่ (validator ต้องมี timeout/ownership) → หยุดและขออนุมัติทิศทางก่อน
- **หยุดทันทีไม่ว่าผลเป็นอย่างไร:** soft-lock, crash, exception ต่อเนื่อง, `SaveSlot*` hash เปลี่ยน, หรือพบว่าต้องเขียน field ของเกม

---

## 8. แผนกู้เมื่อค้าง (recovery ladder)

| ระดับ | ทำอะไร | ต้องปิดเกม? | ใช้เมื่อ |
|---|---|---|---|
| R1 | `set_pref coop.allowRemoteInput=false` (ไม่แตะ IL2CPP) → ทุกคำสั่ง `DISARMED` | ❌ | พฤติกรรมเริ่มผิดปกติ/ไม่แน่ใจ |
| R2 | ปล่อยให้ผู้เล่นกด UI จบเทิร์นเอง (mod ไม่ถือ state ใด ๆ อยู่แล้ว) | ❌ | เทิร์นค้างแบบ "รอ input" |
| R3 | `spike pause` (ถ้า exit 5 ผ่านแล้ว) → กดเล่นต่อเอง / หรือ `SetAllAuto` **เฉพาะเมื่ออนุมัติแล้ว** | ❌ | ค้างกลางแอนิเมชัน/ต้องรีบจบศึก |
| R4 | ปิดเกมแบบไม่เซฟ (Alt+F4/จบ process) → `save-split verify` → `restore` | ✅ | ค้างจนเล่นต่อไม่ได้ |
| R5 | `LongYinCoop.dll` → `.dll.off` แล้วเปิดเกมใหม่ (vanilla) | ✅ | สงสัย mod เป็นต้นเหตุ |
| R6 | `git checkout stage-S1` ใน repo mod | — | ต้องย้อนโค้ด |
| R7 | คืนเซฟจาก `coop-test/S1.5-<ts>/save/` (ของจริงไม่เคยถูกแตะ — R4/R5 ไม่ควรจำเป็น) | ✅ | hash เพี้ยน |

**ทุกครั้งที่ใช้ R2–R7:** บันทึก `UserData/CoopSpike/incident-<ts>.md` (คำสั่งล่าสุด, audit n, statehash ก่อน/หลัง, อาการ) และรายงานฉันก่อนเล่นต่อ

**ห้าม:** กด "บันทึกเกม" ในเกมระหว่าง spike, กดโหลดเซฟอื่น, ใช้ `Save()` เพื่อ "รีเซ็ตสถานะ"

---

## 9. ทดสอบกับศึกประเภทไหน

`BattleType`: `StudyFight=0`, `HardFight=1`, `DeathFight=2` **[DUMP]** · S1 เจอทั้ง 3 แบบในเซสชันเดียว (21 ศึก) **[RUNTIME]**

| ลำดับ | ประเภท | เหตุผล |
|---|---|---|
| **หลัก** | **HardFight (1)** | มีผู้เล่น ≥2 ยูนิต, มีศัตรูหลายตัวพอสำหรับ move/attack/negative tests, ไม่ผูกกับสคริปต์สอน และผลลัพธ์ไม่ถาวรเท่า DeathFight [GUESS ว่าผลถาวรน้อยกว่า — ยังไม่มีหลักฐาน] |
| รอง | StudyFight (0) | ใช้เฉพาะเมื่อหา HardFight ไม่ได้ในเซฟสำเนา — ระวังว่าอาจมีบทเรียนบังคับ/UI พิเศษที่รบกวนการวัด |
| เลี่ยงในรอบแรก | DeathFight (2) | ผลกระทบสูงสุดถ้าพลาด (ตัวละคร/เซฟสำเนาเสียหาย) และ end-condition path กว้างกว่า; จะทำก็ต่อเมื่อ HardFight ผ่านครบและขออนุมัติแยก |

**เงื่อนไขศึกที่ใช้ทดสอบ (ต้องมีครบก่อนเริ่ม S1.5-d):**
1. `battleType == 1` และ `nowActiveUnit` เป็นยูนิตที่ `playerControl == true`
2. มีอย่างน้อย 5 ยูนิต และมีศัตรูที่ `alive` อย่างน้อย 2 ตัว
3. ยูนิตที่ถึงตามี cell ว่างในระยะ move อย่างน้อย 3 ช่อง (สำหรับ 10/10 + negative "นอก range")
4. เก็บศึกนี้ไว้เป็น **snapshot เซฟสำเนาเดียวกัน** ที่โหลดซ้ำได้ (ไม่ต้องเล่นใหม่ทุกครั้ง) — ใช้ `save-split` รอบใหม่ต่อการทดลองหนึ่งชุด
5. สำหรับ negative "target ตายแล้ว": ต้องมี corpse ในศึกเดียวกัน — ถ้าไม่มี ให้ยิง attack จนมีผู้ตาย 1 ตัวก่อน (นับเป็นคำสั่งในโควตา)

---

## 10. Deliverable ของ stage (ตรงตาม STAGEPLAN §4)

| # | ของที่ส่ง | path |
|---|---|---|
| 1 | `UnitCommandSpike` (move/attack/pause/arm/status) | `src/LongYinCoop/S1.5/` |
| 2 | `TurnGate` | `src/LongYinCoop/S1.5/TurnGate.cs` |
| 3 | `Executor` (ลำดับ call ที่พิสูจน์แล้ว) | `src/LongYinCoop/S1.5/SpikeExecutor.cs` |
| 4 | `Audit` + `stateHash` | `src/LongYinCoop/S1.5/SpikeAudit.cs` → `UserData/CoopSpike/` |
| 5 | `CoopSpike.md` (สเปก S3: ลำดับ call, guard, negative codes, จังหวะเวลา) | `UserData/CoopSpike.md` |
| 6 | `save-split` tool + MANIFEST/hash | `tools/save-split.py`, `coop-test/S1.5-<ts>/` |
| 7 | รายงานปิด stage + evidence | `evidence/S1.5-<ts>/S1.5-report.md` |
| 8 | tag | `stage-S1.5` (repo mod + repo docs) |

**Pref ใหม่ (declared):** `coop.allowRemoteInput` (false), `coop.spikeArmTtlSec` (120), `coop.spikeBudgetPerMin` (20), `coop.spikeMaxTotal` (200) — ทั้งหมดอยู่ในหมวด `[LongYinCoop]`, แก้ที่ `CoopPrefs` ที่เดียว

---

## 11. สิ่งที่ยังไม่รู้ / ความเสี่ยงที่รับไว้

- **entry method ของ move รู้แล้วแต่ยังไม่ re-resolve:** `BattleGridClicked(GameObject)` เป็น DUMP เท่านั้น ยังไม่เคยเรียก — ยังไม่รู้ว่าต้องส่ง GameObject ตัวไหนและต้องมี pre-state อะไร [DUMP]
- **อาจต้องเขียน field** (`selectedGrid`, `nowActiveState`) — ถ้าใช่ = นอกสิทธิ์ S1.5 ต้องหยุดถาม
- **ไม่มี range-check helper ให้ใช้** → G6 พึ่ง list `moveRangeGridUnits`/`attackRangeGridUnits` ซึ่งอาจไม่ตรงกับ UI จริง [GUESS]
- **soft-lock = ความเสี่ยงสูงสุดของทั้งแผน** → R1–R7 + audit ทุกคำสั่ง + 1 คำสั่งต่อการ arm
- **ATB** (J7) อาจทำให้ S3 ต้องออกแบบใหม่ — ยอมรับว่าเป็นผลลัพธ์ที่อาจต้องหยุด
- **DeathFight ยังไม่ทดสอบ** — ไม่ยืนยันว่า "ผลถาวรน้อยกว่า" จนกว่าจะวัด
- **`SaveSlot*/Info` mtime อย่างเดียวไม่พอ** พิสูจน์การโหลดสำเนา → ต้องมี P-a sentinel

---

## 12. ขออนุมัติ

1. อนุมัติแผนนี้ทั้งฉบับ (หรือขอแก้ข้อใด)
2. อนุมัติ `restore` ขั้นแรก (คืน `Save.real-S1` กลับเป็น `Save` + rehash 57/57) — **ตอนนี้เกมปิดแล้ว** และยังไม่เรียก method ใดของเกม
3. เลือกสล็อต k สำหรับ sentinel P-a (หรือให้ผมเลือกจากสล็อตว่างที่วัดได้ตอน split)
4. ยืนยันว่ายังห้ามผมเรียก method เปลี่ยน state จนกว่าจะอนุมัติเป็นรายขั้น (S1.5-d เป็นต้นไป)

# S2a-2-plan **rev4** — host sampler/sender (loopback) · มติ 2026-10-21 ข้อ 1–4 (รันขั้น 1–10 ได้)

**สถานะ: step 0 + fix round 2 เขียนโค้ด+เทสต์เสร็จ (232 PASS / 0 FAIL) · rev4 · พร้อมรันขั้น 1–10 · ยังไม่ commit · ยังไม่ build Release · ยังไม่ copy DLL · ยังไม่แตะเซฟ · ยังไม่เปิดเกม**
rev1 → rev2: เพิ่มขั้น 0 (เขียนโค้ด+เทสต์ in-proc ก่อน), สลับลำดับเป็น split → copy DLL → sampler → preflight → ผู้ใช้เปิดเกม, เพิ่มการตรวจความพอของเครื่องมือวัดก่อนเปิดเกม, commit source ก่อน build, นิยามใหม่ exit 3/5/6/2/8, เพิ่มการตั้ง/คืน pref + sha256 และไฟล์ `s2-snapshots.ndjson`, เพิ่มคำเตือนตอนเล่น

rev2 → rev3 (มติผู้ใช้ 2026-10-21 หลังตรวจ diff step 0): **ยกเลิก D1** — `TurnGate.cs` กลับเป็น `57e68dc` ทุกไบต์ (guard เทสต์ = byte-exact กับ fixture) · replica ใช้ `IsAlive` ตัวเดียวแล้วเทียบกับ `unitAlive` ของ `spike status` (ค่าจาก `BattleUnit.IsAlive` ตัวเดียวกัน) · ป้ายเฟส: **A = `coop.net.enabled=false` (baseline, S2 idle) · B = `true` (S2 ทำงาน)** · exit 3 = **`(B − A) ≤ 1 ms` โดยดูเครื่องหมาย** + band ±0.5 ms = INCONCLUSIVE · exit 6 = **`forbiddenTypeDrops == 3` พอดี + `commandsAccepted == 0` + audit tuple ก่อน/หลังเท่ากัน**
rev3 → rev4 (มติผู้ใช้ 2026-10-21 ข้อ 1–4): **`DevCommands` = (ก)** — ยอมให้ `DevCommands=true` ตอนวัด แล้ว **ย้ายตรรกะ guard เป็นฟังก์ชัน pure** `S2WriteGuard.WritePathsClosed(allowRemoteInput, jitProbe, jitProbeW2, jitProbeW4, jitProbeW6, jitProbeW7)` ที่ **`DevCommands` ไม่มีผลกับผลลัพธ์** (เทสต์ครบ 64 ชุดค่าผสม + reflection + source check) · **ตาราง audit คำสั่ง IPC** ทุกคำสั่งที่เปิดได้ตอน `DevCommands=true` (§8.2) · ขั้น 7 ตั้ง `DevCommands=true` **อย่างชัดเจน** + บันทึกใน evidence · `ReplicaHash.UnitAlive` = **(ii)** คงฟิลด์ **ห้ามแก้ hash spec** และ `S2Sampler` ใส่ค่า `= u.IsAlive` (สำเนา, ล้าหลัง `hp` ≤1 เฟรม; พิจารณาตัดตอนเปลี่ยนเวอร์ชันโปรโตคอล) · recorder ขั้น 9 = `tools\s15d_sample.py` (เหตุผลในตารางขั้น 9) · `AGENTS.md` โฟลเดอร์เกม = commit แยก 1 commit (`git add AGENTS.md` เท่านั้น) · **ไม่แตะ `IpcServer.cs` / `tools/` ในรอบนี้**
มติ ①–④ (2026-10-21): **ใช่ทั้งหมด** · `.dll.off` — **ตรวจแล้วไม่มีไฟล์เดิมใน `Mods\`** (ไม่มี `*.off` เลย) ใช้ชื่อนี้ได้
อ้างอิง: `S2-plan.md §13.3/§13.4/§13.6`, `STAGEPLAN.md §1.1/§1.4/§1.6`
DLL เดิม: `517758147f59170e584075a4b42f61e00d293803febe36f8a040bcdad4592612` (= `src\LongYinCoop\bin\Release\LongYinCoop.dll` byte เดียวกัน, ยืนยัน sha256 แล้ว)

## 0. กติกาที่บังคับในขั้น 0 (เขียนโค้ด)

- อ่าน IL2CPP **บน main thread เท่านั้น** ผ่าน `MainThreadDispatcher` (budget 4 ms/เฟรม ตาม §1.4)
- thread อื่นแตะได้เฉพาะ **POCO/serialized byte buffer** — ห้ามถือ `Il2CppObjectBase` ข้ามเฟรม
- interop **แบบมีชนิด** (`u.IsAlive`, `hero.hp`, `grid.row`, `team.battleUnits`) — ห้าม raw offset/pointer
- S2 ไม่แตะ `GameDataController.Save/Load`, ไม่มี type `pause/move/attack/skill/item` ในช่อง guest→host, ไม่แตะ Steam/`APIDispatcher`/`DnsChecker`/`CheatCheck`/`cheat*`

## 1. ขอบเขต

**ทำ**

| ไฟล์ | เนื้อหา |
|---|---|
| `src/LongYinCoop/S2/Transport/ITransport.cs` | interface + `TransportStats` + `TransportKind` |
| `src/LongYinCoop/S2/Transport/LoopbackTransport.cs` | in-proc pair (send → sink queue), ไม่มี socket, อ่าน byte buffer เท่านั้น |
| `src/LongYinCoop/S2/Host/S2Snapshot.cs` | POCO ต่อยูนิต + header (ไม่มี Il2Cpp type) |
| `src/LongYinCoop/S2/Host/S2Sampler.cs` | อ่าน game state บน main thread → POCO + audit tuple |
| `src/LongYinCoop/S2/Host/S2Sender.cs` | serialize (Protocol codec) + ส่งผ่าน `ITransport` + metrics |
| `src/LongYinCoop/S2/Host/S2Counters.cs` | `forbiddenTypeDrops`, `commandsAccepted`, `framesSent`, `bytesSent`, allowlist enforcement |
| `src/LongYinCoop/S2/S2NetConfig.cs` | อ่าน `coop.net.*` จาก `MelonPreferences.cfg` (poll ≤1 Hz, ไม่ใช้ raw offset) |
| `tests/S2Protocol.Tests/Program.cs` | เพิ่มชุดเทสต์ in-proc (loopback, ยัดเฟรมต้องห้าม, audit) |
| `src/LongYinCoop/CoopMod.cs` | hook เปิด/ปิด S2 (แก้เล็กสุด, เฉพาะ init/poll) |

**ไม่ทำ:** `TcpTransport`, `viewer-client.py`, resync ผ่านเน็ต (→ S2a-3) · overlay + `WriteGuard` hook (→ S2a-4) · VPN (→ S2b) · แก้ `tools/` · `TurnGate.cs` (**ไม่แตะเลย** — D1 ยกเลิกแล้ว, guard = byte-exact กับ `57e68dc`) · **`IpcServer.cs` (rev4: ไม่แตะเลย — ตาราง audit §8.2 เป็นเอกสารเท่านั้น; ถ้าต้องกัน `set_pref`/`disarm` เพิ่ม จะทำเป็น stage ถัดไปหลังคุณอนุมัติ)**

## 2. ลำดับ (ห้ามสลับ)

| # | ใคร | ขั้น | ผ่านเมื่อ |
|---|---|---|---|
| **0** | ผม | เขียนโค้ดตาม §1 + เทสต์ in-proc → `dotnet run --project tests\S2Protocol.Tests` → **ส่งสรุป diff ให้คุณตรวจ แล้วหยุดรอ** | in-proc PASS ทุกเคส รวม "ยัดเฟรมต้องห้าม" |
| 1 | ผม | **commit source S2a-2** (ยังไม่ build) → จด commit hash ลง evidence | tree สะอาด |
| 2 | ผม | `dotnet build src\LongYinCoop\LongYinCoop.csproj -c Release` → จด sha256 DLL ใหม่ + build log (**ยังไม่ copy**) | build 0 error/warning ใหม่ |
| 3 | ผม | สำรอง DLL เดิม: `Mods\LongYinCoop.dll` → `Mods\LongYinCoop.dll.off` (+ `.pdb.off`) → จด sha256/ขนาดของ `.off` | ยืนยันแล้วว่า `.off` ยังไม่มีไฟล์เดิม |
| 4 | ผม | ตรวจก่อน split: เกมไม่รัน · `save-split status` = 57 ไฟล์ `6dd645d9ef17` · `leftovers: none` · sha256 เกม 2 ไฟล์ตรง `VERSION.md` · **backup `MelonPreferences.cfg` → `prefs.before.cfg` + sha256** | ทุกข้อผ่าน (ไม่ผ่าน = หยุด) |
| 5 | ผม | `save-split split --label S2a2-<ts> --expected-baseline coop-test\baselines\20261021-200450\savehash.real.before.txt` | `Save.real-S2a2-<ts>` อยู่, `Save` = สำเนา |
| 6 | ผม | copy DLL+PDB ใหม่เข้า `Mods\` → sha256 ใน `Mods\` ต้องตรงข้อ 2 | ตรง |
| 7 | ผม | ตั้ง pref: `coop.net.enabled=false` (เฟส A = baseline ปิด), `coop.role=host`, `coop.secret=<hex 32B>`, **`coop.DevCommands=true` (มติ rev4 ข้อ 1 = (ก) — จำเป็นให้ `spike status` ทำงาน; ต้องจดลง evidence)**, `coop.AllowRemoteInput=false`; บันทึก sha256 ของ cfg หลังตั้ง + ค่าทุกคีย์ใน section `[LongYinCoop]` | `AllowRemoteInput=false` · `JitProbe/JitProbeW2/W4/W6/W7=false` · `DevCommands=true` บันทึกแล้ว |
| 8 | ผม | ตรวจความพอของเครื่องมือวัด (**ผลตรวจแล้ว 2026-10-21**): `spike status` ต่อยูนิตมี `hp,row,col,unitAlive,playerControl,originHp,alive` — `unitAlive` = `BattleUnit.IsAlive` ⇒ **พอสำหรับ exit 4** (เทียบ `isAlive` ของ replica ซึ่งมี `unitAlive` บน wire/ndjson ด้วย) · ต้อง `Enabled+Ipc+DevCommands` ทั้งสาม (IpcServer.cs:333-335) ⇒ rev4 ตั้ง `DevCommands=true` แล้ว ⇒ **ข้อนี้ผ่าน** | ผ่าน (ไม่ต้องรอคำตัดสินแล้ว) |
| 9 | ผม | เปิด recorder: **`tools\s15d_sample.py --out coop-test\S2a2-<ts>\sampler.ndjson --seconds 7200 --interval 0.5 --append --wait 3600`** (background) + จด PID · สร้างไฟล์ `sampler.ndjson` เปล่าไว้ **ก่อน** เปิด process (เพื่อให้ preflight เห็นไฟล์; `--append` ไม่ทำลาย) · **หมายเหตุ:** แผนเดิมใช้ `spike_watch.py` แต่สคริปต์นั้น **exit 0 ทันทีที่ `battle_ready`** ⇒ ให้ชุดข้อมูล A/B 120 s ไม่ได้; `s15d_sample.py` อยู่ใน `SAMPLER_SCRIPTS` allowlist ของ `save-split.py` เหมือนกัน, read-only (`spike status` เท่านั้น), บันทึก **full payload ต่อยูนิต** (ใช้ได้ทั้ง exit 3 และ exit 4 จาก series เดียว) — ไม่แก้ `tools/` | ไฟล์ถูกสร้าง + process ยังรัน |
| 10 | ผม | `save-split preflight --label S2a2-<ts> --sampler-pid <pid> --sampler-out …\sampler.ndjson` → **ต้อง READY** | READY (REFUSED = หยุด ไม่เปิดเกม) |
| 11 | **คุณ** | **เปิดเกม** → เข้า 1 ศึก (คาด 11v7) → เล่น 1 เทิร์น · **ระหว่างเล่นดู §6** → ปิดเกม แล้วบอกผม | — |
| 12 | ผม | เก็บผล: `s2-snapshots.ndjson`, metrics, เทียบ per-unit `isAlive` ↔ `unitAlive` (§5 exit 4), audit tuple, unit count, A/B frame time (§5 exit 3) | — |
| 13 | ผม | ปิดเกมแล้ว: `verify` → `restore` → **คืน pref** (copy `prefs.before.cfg` กลับ + sha256 ต้องตรงก่อนตั้ง) → `rebaseline` ถ้า pref drift | เซฟ 57 ไฟล์ `6dd645d9ef17`, `leftovers: none` |
| 14 | ผม | รายงานเทียบ exit criteria ทีละข้อ + rollback test → **รออนุมัติ** ก่อน tag `checkpoint-S2a` | — |

## 3. คำเตือนผู้ใช้ตอนเล่น (ข้อ 11)

1. **ห้าม Save** เกม (F5/เมนู save) ตลอดช่วงวัด — ใช้สำเนาเท่านั้น
2. **ห้าม pause ตอน `WaitPlayerInput`** (มติ S1.5: เกมไม่รับ) — ถ้าจะ pause ให้ทำตอน state อื่น
3. **ถ้ากด Esc** (เมนู Esc หยุด sim แต่ `battlePaused` ยัง false, `timeScale` ยัง 1) → **แจ้งผม** เพื่อ exclude หน้าต่างนั้นจากการวัด
4. อย่าออกจากศึกกลางคันถ้าเลี่ยงได้ (เกมไม่มี autosave ตอนออก)
5. ถ้าเกมค้าง/เด้ง ให้แจ้งทันที ห้ามกดอะไรซ้ำ

## 4. Pref: ตั้ง/คืน + sha256

| จุด | สิ่งที่ทำ | หลักฐาน |
|---|---|---|
| ก่อนตั้ง (ข้อ 4) | copy `UserData\MelonPreferences.cfg` → `coop-test\S2a2-<ts>\prefs.before.cfg` + sha256 | `prefs-sha-before.txt` |
| ระหว่าง (ข้อ 7) | แก้เฉพาะ section `[LongYinCoop]`: `coop.net.enabled=false` (เริ่มที่ปิด — เฟส A) · `coop.role=host` · `coop.secret=<hex 32B>` · **`coop.DevCommands=true` (rev4 ข้อ 1 = (ก))** · คง `coop.AllowRemoteInput=false` + `coop.JitProbe/W2/W4/W6/W7=false` · `coop.Enabled=true`, `coop.Ipc=true` (โมดต้องรัน/IPC ต้องมี) | sha256 หลังตั้ง + ค่าทุกคีย์ (จด `DevCommands=true` ชัด ๆ) |
| ระหว่างวัด (ข้อ 11–12) | สลับ `coop.net.enabled` ผ่านไฟล์ cfg (S2 อ่านทุก ≤1 Hz): **A = `false` (baseline, S2 idle) → B = `true` (S2 ทำงาน)** เป็นบล็อก **A-B-A-B บล็อกละ 30 s** แล้วจบที่ A | บันทึกเวลา + sha256 ทุกครั้งที่สลับ |
| หลังจบ (ข้อ 13) | copy `prefs.before.cfg` กลับทับ + sha256 **ต้องตรงก่อนตั้งเป๊ะ** | `prefs-sha-after.txt` + diff |

## 5. Exit criteria (นิยาม rev2)

| # | เกณฑ์ | วิธีวัด/ผ่าน |
|---|---|---|
| 1 | โหลดได้/ไม่ crash | log mod ปรากฏ, 1 เทิร์นจบ, exception = 0 |
| 2 | ต้นทุนต่อเฟรม | `sampleMs+serializeMs` p99 ≤ **4 ms** (§13.2 ข้อ 4) บันทึก mean/p99/max · **ถ้ารอบนี้ 18 ยูนิต → verdict = PASS-with-limitation (ห้ามขยายผลถึง 60)** |
| 3 | **ตัววัด frame time + baseline (นิยาม rev3)** | ตัววัด: `deltaTime`/`frameCount` delta จาก recorder ตัวเดียวกัน (**rev4: `tools\s15d_sample.py`** — read-only, เก็บ `frame` ต่อตัวอย่าง; ไม่ผูกกับ S2) · **A = `coop.net.enabled=false` (baseline, S2 idle) · B = `true` (S2 ทำงาน)** สลับเป็นบล็อก **A-B-A-B บล็อกละ 30 s (รวม 120 s) ในศึกเดียว** · **เกณฑ์: `mean(B) − mean(A) ≤ 1 ms` โดยดูเครื่องหมาย** (ติดลบ = S2 ถูกกว่า = ผ่าน) · `|delta − 1 ms| ≤ 0.5 ms` = **INCONCLUSIVE — ห้ามฟันธงผ่านจากรอบเดียว** (โค้ด: `S2Exit3Rule`) · เวลาที่สลับบันทึกด้วยมือ (`frame-time-ab.txt`) เพราะ `s2-snapshots.ndjson` ไม่มี wall timestamp ของเฟส · ถ้าสลับ pref กลางรันไม่ได้ →ใช้ **Option D2-ii** (2 รัน: `.dll.off` vs DLL ใหม่) แล้วระบุชัดในรายงาน |
| 4 | snapshot ตรงกับเกม | per-unit เทียบ `spike status` (จาก `sampler.ndjson`) vs `s2-snapshots.ndjson`: `unitCount, id, row, col, hp(3), isAlive` mismatch 0 · **`isAlive` ของ replica (`S2Unit.IsAlive`) เทียบกับ `unitAlive` ของ `spike status`** — ทั้งสองคือ `BattleUnit.IsAlive` ตัวเดียวกัน (TurnGate `SafeUnitAlive(u) → u.IsAlive`), ไม่มี D1, ไม่มี raw read ⇒ **เทียบได้เต็ม ไม่ใช่ NOT-RUN** · **rev4: replica ยังมีฟิลด์ `UnitAlive` (สำเนา `IsAlive`) บน wire + ndjson ⇒ เทียบ `unitAlive` สองฝั่งได้ตรง ๆ** · `moved/attacked` ต่อยูนิต: S1.5 วัดแล้วว่า `BattleUnit.moved/attacked` ไม่พลิกจริง ⇒ ใช้ `isPlayerTurn/turnMoved/turnAttacked(turnFlagsAvailable)` ระดับ controller แทน และรายงาน per-unit flag เป็นข้อมูลประกอบเท่านั้น · **ข้อนี้พึ่ง `spike status` ⇒ ต้อง `DevCommands=true` (rev4 ตั้งแล้ว)** |
| 5 | ความตายใน replica | **ไม่มีศพในรอบนี้ → NOT-RUN** (ไม่ใช่ FAIL) บันทึกไว้ทำต่อ · ถ้ามีศพ: `hp=0 && isAlive=false` และต้องยืนยันซ้ำด้วย `unitAlive=false` ของ `spike status` (= ค่าเดียวกัน) |
| 6 | **ยัดเฟรมต้องห้าม (นิยาม rev3)** | **in-proc:** ยัดชุดเดียวกับที่รันจริง `S2SelfTest.BuildFrames()` (Hello + Snapshot + BattleEvent ที่ถูก chunk) → **`forbiddenTypeDrops == 3` พอดี**, **`commandsAccepted == 0`**, **audit tuple ก่อน/หลังเท่ากัน** · **ในเกม:** self-test รันเองตอน activate เขียน marker `selftest` ลง `s2-snapshots.ndjson` ต้องได้ `gained=3 · commandsAccepted=0 · auditEqual=true · exit6=PASS` (ไม่ผ่าน → marker `selftest.fail` + log WARN) |
| 7 | ไม่แตะเซฟ | `SaveSlot*` เปลี่ยน 0 ไฟล์ · หลัง restore digest = `6dd645d9ef17` (ยกเว้น volatile) · `leftovers: none` |
| 8 | **จำนวนยูนิตจริง** | บันทึก unit count จริงทุกรอบ · **18 ยูนิต (11v7) = PASS-with-limitation; ห้ามลดเป้า 60 และห้ามสรุปว่าผ่านที่ 60** |
| 9 | rollback ใช้ได้จริง | คืน `.dll.off` แล้วเกมรันได้ (log mod เดิม) + `restore` คืนเซฟได้ + pref คืน hash ตรง |

FAIL ข้อใด = รายงานตรง ๆ คงสถานะ rollback ไม่เลื่อนเกณฑ์

## 6. Rollback

1. **DLL**: `Copy-Item Mods\LongYinCoop.dll.off Mods\LongYinCoop.dll -Force` (+ `.pdb.off`)
2. **pref**: `Copy-Item prefs.before.cfg → UserData\MelonPreferences.cfg` + ตรวจ sha256
3. **เซฟ**: `save-split.py restore --label S2a2-<ts>` + `verify` ⇒ 57 ไฟล์ `6dd645d9ef17`
4. **git**: `git checkout stage-S1.5g` (`e3e798a`) หรือ revert commit ของข้อ 1
5. **tag**: `checkpoint-S2a` เฉพาะหลังอนุมัติรายงาน

## 7. หลักฐาน (`evidence/S2a-2-<ts>/`)

`code-diff-summary.md` · `commit.txt` (hash ข้อ 1) · `build.txt` (log + sha256 DLL ใหม่) · `dll-before-after.txt` (`.off` + ใหม่) · `prefs-sha-before.txt` · `prefs-sha-after.txt` · **`pref-values.txt` (ค่าทุกคีย์ใน `[LongYinCoop]` รวม `DevCommands=true`)** · **`recorder.txt` (เหตุผลที่ใช้ `s15d_sample.py`)** · `preflight.txt` · `split.json` · `sampler.ndjson` (recorder กลาง) · **`s2-snapshots.ndjson` (S2 ส่งออก)** · `metrics-summary.txt` · `frame-time-ab.txt` (A/B + เวลาที่สลับ pref) · `snapshot-compare.txt` · `audit-tuple.txt` · `unit-count.txt` · `verify-restore.txt` · `notes.md`

## 8. สถานะมติ + ข้อที่ต้องได้คำตอบก่อนเปิดเกม (rev4)

### 8.1 D1 — per-unit `isAlive` สำหรับ exit 4: **ยกเลิก (มติผู้ใช้ 2026-10-21)**
- `TurnGate.cs` กลับเป็น `57e68dc` **ทุกไบต์** (guard เทสต์ = byte-exact กับ fixture, sha256 `737d48941c3707cef4aea1ef4ccae26163f5fe05f28cd065ae246b84854523ca`)
- `unitAlive` ที่ `spike status` รายงาน = `SafeUnitAlive(u) → u.IsAlive` = `BattleUnit.IsAlive` ตัวเดียวกันกับที่ replica เก็บ ⇒ **ไม่ต้องอ่านค่าที่สองจากเกม, ไม่มี raw read** (กติกา 8)
- exit 4 จึงเทียบ `S2Unit.IsAlive` ↔ `unitAlive` ของ `spike status` แทน

### 8.2 `DevCommands` = **(ก) อนุมัติแล้ว (มติ 2026-10-21 ข้อ 1)** + ตาราง audit คำสั่ง IPC
- rev3 เจอข้อขัดแย้ง: `spike status` บังคับ `Enabled+Ipc+DevCommands` (IpcServer.cs:333-335) แต่ guard เดิมบังคับ `DevCommands=false`
- **มติ (ก):** อนุญาต `DevCommands=true` ตอนวัด แล้ว **ผ่อน guard** — ย้ายตรรกะเป็นฟังก์ชัน pure `S2WriteGuard.WritePathsClosed(6 pref)`
  โดย **`DevCommands` ไม่ถูกตรวจ** และเพิ่ม `JitProbe/JitProbeW2/W4/W6/W7` เป็นเงื่อนไขปฏิเสธ (เดิมตรวจแค่ `AllowRemoteInput`+`DevCommands`)
  เทสต์: truth table ครบ **64 ชุด** + `Describe` + reflection (ไม่มีพารามิเตอร์ `dev*`) + source check (`S2HostService` ไม่มี `DevCommands` เลย)
- ขั้น 7 ต้องตั้ง `coop.DevCommands=true` **ชัดเจน** และจดใน evidence (`pref-values.txt`)

**ตาราง audit คำสั่ง IPC ที่เปิดได้ตอน `DevCommands=true`** (ตารางเต็ม + เลขบรรทัด: `evidence/S2a2-20261021-2213-step0/ipc-audit-20261021.md`)

| คำสั่ง | gate | ประเภท | ผ่าน `AllowRemoteInput`? |
|---|---|---|---|
| `spike status` | `Enabled+Ipc+DevCommands` | อ่านอย่างเดียว | — (ไม่ต้อง) |
| `spike arm` | +`AllowRemoteInput` (:344) | เปลี่ยน mod-state (เปิดทาง move) | ✓ |
| `spike move` | +`AllowRemoteInput` (:355) | **เขียน state เกม** | ✓ |
| `spike attack` | +`AllowRemoteInput` (:381) | **เขียน state เกม** | ✓ |
| `spike disarm` | `Enabled+Ipc+DevCommands` เท่านั้น (:349-352) | เปลี่ยน mod-state (ล้าง arm flag) — **เขียนเกมไม่ได้** | ✗ (เจตนา = เบรกฉุกเฉิน) |
| `bench_dispatch` | `DevCommands` (:558) | เปลี่ยน mod-state (enqueue no-op) | ✗ |
| `set_pref` | **ไม่มี gate** (:405-553) | เปลี่ยน pref/mod-state + ติด-ถอด Harmony patch (**เปิด `allowRemoteInput` ได้เอง**) | ✗ |
| อื่น ๆ (`ping/get_version/get_scene/get_player_pos/stats/snapshot*/capture/compat_dump/probe_types/patch_report/probe_units/s1_report`) | ไม่มี gate | อ่านอย่างเดียว หรือเขียนเฉพาะ **ไฟล์หลักฐาน** | — (ไม่มี game write) |

**สรุป:** ไม่มีคำสั่งที่ **เขียน state เกม** ได้โดยไม่ผ่าน `AllowRemoteInput` · แต่มี 3 รายการที่เขียน **mod-state** ได้โดยไม่ผ่าน
(`spike disarm`, `bench_dispatch`, `set_pref`) ⇒ **รายงานให้ผู้ใช้ตัดสิน (ยังไม่เลือกเอง, ไม่แก้ `IpcServer.cs`)** ·
ข้อเสนอ (stage ถัดไป, รออนุมัติ): กัน `set_pref` ไม่ให้เปิด write path ขณะ S2 active (หรือรวมใน `WriteGuard` hook ของ S2a-4)

### 8.3 `ReplicaHash.UnitAlive` = **(ii) อนุมัติแล้ว (มติ 2026-10-21 ข้อ 2)**
- **คงฟิลด์ไว้ ห้ามแก้ hash spec** — `ReplicaHash.Compute` ไม่ถูกแตะ (เทสต์ T0 ผ่านเท่าเดิม)
- `S2Sampler` ใส่ค่า `UnitAlive = S2UnitRule.MirrorUnitAlive(u.IsAlive)` = **สำเนาของ `IsAlive`**
  (ไม่มีแหล่งค่าที่สอง: interop ไม่มี `BattleUnit.unitAlive`; `unitAlive` ของ `spike status` = `IsAlive`)
- คอมเมนต์ใน `ReplicaHash.cs` แก้ให้ตรงความจริงแล้ว (สำเนา, ล้าหลัง `hp` ≤1 เฟรม) + จดใน `S2-plan.md §13.2` ว่าจะพิจารณาตัดตอนเปลี่ยนเวอร์ชันโปรโตคอล
- ฟิลด์เดินทางบน wire (`S2FrameCodec`) + อยู่ใน ndjson (`"unitAlive"`) — audit tuple ไม่ใส่ (ซ้ำกับ `isAlive`)
- **ProtoVer ยัง = 1** เพราะยังไม่มี peer จริง (S2b) — การเพิ่ม 1 ไบต์/ยูนิตบันทึกไว้ที่นี่และใน `S2-plan.md`

### 8.4 D2 — วิธี baseline ของ exit 3: **ใช้ D2-i** (A/B ในศึกเดียว สลับ `coop.net.enabled` ผ่าน cfg ≤1 Hz) เป็นบล็อก A-B-A-B 30 s · D2-ii เป็น fallback
- ป้ายเฟส: **A = `false` (baseline, S2 idle) · B = `true` (S2 ทำงาน)** · เกณฑ์ `mean(B) − mean(A) ≤ 1 ms` แบบดูเครื่องหมาย
- **recorder (ขั้น 9) = `tools\s15d_sample.py`** แทน `spike_watch.py` (ตัวหลัง `exit 0` ทันทีที่ `battle_ready` ⇒ ให้ series 120 s ไม่ได้)
  ทั้งคู่อยู่ใน allowlist `SAMPLER_SCRIPTS` ของ `save-split.py`, อ่านอย่างเดียว, ไม่แก้ `tools/` · ไฟล์ผล `sampler.ndjson` ถูกสร้างก่อนเปิด process (preflight เห็นไฟล์)

**preflight:** READY ได้ต่อเมื่อมี sampler PID + ไฟล์ผล (ข้อ 9–10) + เกมปิด + `Save` เป็นสำเนา (ไม่ตรวจ `DevCommands`)

ขั้น 0 + fix round 2 เสร็จแล้ว (**232 checks PASS / 0 FAIL** · `evidence/S2a2-20261021-2213-step0/`) · **rev4 นี้รันขั้น 1–10 ได้ทันที**

### 8.5 rev5 — มติผู้ใช้ 2026-10-21 (หลังคำถามก่อนเปิดเกม) **แทนที่วิธีทำ exit 3 และ exit 4 ข้างบน**
เอกสารบังคับ: `evidence/S2a2-20261021-2254/ab-protocol-v2.md` (สรุปใน `AGENTS.md` มติ rev5)
- **exit 3 = วัด mean อย่างเดียว** ไม่เพิ่ม p99 ไม่ redeploy · ผ่านเกณฑ์ mean = **PASS-with-limitation** (p99 = **NOT-RUN**) · `delta` ใน ±0.5 ms รอบ margin 1.0 ms = **INCONCLUSIVE** · เกณฑ์ margin 1.0 / band 0.5 ไม่ขยับ
- **A-B-A-B 120 s:** ศึกต้องนิ่งที่ `nowActiveState = WaitPlayerInput` ตลอด และห้ามสั่ง action ใด ๆ · ก่อนทุกบล็อกตรวจ `sampler.ndjson` ยังโต (ตายกลางบล็อก = ทิ้งบล็อกนั้น) · จด wall-clock ทุกการสลับ pref + sha256 cfg ก่อน/หลัง ลง `frame-time-ab.txt` · `phase.start/phase.idle` ไม่โผล่ใน ~3 s = หยุดและรายงาน
- **exit 4 ย้ายไปหลังจบ A-B-A-B** โดยเปิดเฟส B ค้างไว้แล้วเล่น 1 เทิร์น (ไม่สลับ pref ระหว่างเล่น)
- **exit 6:** self-test ต้องเกิดในศึกจริง และ marker ต้องมี `haveBefore=true` **และ** `haveAfter=true`; ถ้า false = โมฆะ/NOT-RUN
- ลำดับบล็อกคงเดิม: A1(false 30s) → B1(true 30s, self-test รันในศึก) → A2(false 30s) → B2(true 30s) → exit 4

# S2a-3-plan — TcpTransport (loopback) + viewer-client + metrics + RESYNC

**สถานะ: ฉบับเสนอ 2026-10-22 · ยังไม่อนุมัติ · ยังไม่เขียนโค้ด · ยังไม่ build · ยังไม่ deploy · ยังไม่เปิดเกม**
อ้างอิงน้ำหนัก: `S2-plan.md` (rev2, §13 มีผลเหนือ §1–§12) > `S2a-2-plan.md` (rev4/rev5) > `evidence/**` > `STAGEPLAN.md` §1.1/§1.4/§1.6/§1.7/§5/§10
**ไม่ใช้** `STAGEPLAN.v2-draft.md` เป็นข้อสรุป (ยังไม่อยู่ใน git และไม่ได้รับอนุมัติ)

ป้ายหลักฐาน: **RUNTIME** = วัดจากของที่รันแล้ว · **CODE** = อ่านจาก source ที่ ship/มีใน repo · **SPEC** = ข้อกำหนดในแผน · **GUESS** = ยังไม่ยืนยัน

---

## 0. ขอบเขตของ stage นี้ (และสิ่งที่ไม่ทำ)

**ทำ (S2a-3 = ตาม `S2-plan.md` §13.3 แถว S2a-3 เท่านั้น)**
- `TcpTransport` ฝั่ง host (listen/accept บน loopback) + framing/reassembly บน stream
- handshake + auth จริงครั้งแรก (S2a-2 ยังไม่เคยมี handshake — loopback ไม่มี peer)
- `seq` / ตรวจจับ gap / `RESYNC` / disconnect-reconnect
- `tools/viewer-client.py` (Python, stdlib เท่านั้น) = guest ตัวจริงสำหรับวัด
- metrics ทั้งสองฝั่ง + เครื่องวัด frame time ต่อเฟรม (p99) ในมอด
- วัด exit criteria ของ S2a-3 (§6)

**ไม่ทำ (เด็ดขาด)**
- ไม่มี write path ไปยัง state เกม (เหมือน S2a-2) · ไม่มี type คำสั่งเกมในช่อง guest→host
- ไม่ทำ overlay IMGUI / `WriteGuard` hook / role=guest ในมอด → **S2a-4**
- ไม่ทำ TCP ข้ามเครื่อง / VPN / bind นอก loopback → **S2b** (ต้องมีคำตอบ Q3 ของ rev2 ก่อน)
- ไม่แตะ `IpcServer.cs`, `TurnGate.cs`, `tools/save-split.py`, `tools/s15d_sample.py`, mod อื่น (`LongYinThai`, `UnityExplorer`)
- ไม่แตะเซฟจริง/ไม่เปลี่ยน baseline (`4226730f66ff`) · ไม่เปิดเกมเอง (ผู้ใช้เปิดเท่านั้น)
- ไม่เพิ่ม dependency ใหม่ (`csproj` ต้องไม่มี `PackageReference`; ใช้ `System.Net.Sockets`)

---

## 1. ข้อเท็จจริงที่ตรวจโค้ดรอบนี้ (CODE / RUNTIME) — และสิ่งที่มันบังคับให้แผนเปลี่ยน

| # | ข้อเท็จจริง | ที่มา | ผลต่อแผน |
|---|---|---|---|
| A1 | `seq` ที่ส่งบน wire **= `frame.FrameId` ของ sampler** (`(uint)frame.FrameId` ตอน encode) ไม่ใช่ตัวนับของ session | CODE `S2/Host/S2Sender.cs:297-300` | ถ้า sender ทิ้งเฟรม (`TryPost` คิวเต็ม capacity 2) seq จะ "ขาด" ทั้งที่ไม่ได้หายบนเน็ต → guest จะยิง RESYNC ปลอม · **ต้องย้าย seq ไปนับตอน send จริง** |
| A2 | sampler ถูกเรียก **ทุกเฟรม Unity** และ post ทุกเฟรม (ไม่มี dedup/pacing) | CODE `S2/S2HostService.cs:89-96`, `CoopMod.cs:106` | ที่ ~128 fps [RUNTIME S1.5g] bandwidth = payload×fps · **ต้องมี change-triggered + pacing + heartbeat** |
| A3 | payload จริงที่ 9 ยูนิต = **304 B (p50, p95=305, max=305)** · คำนวณจาก codec: header ≈ 85 B (battleKey hex 64 ตัว) + ≈ 24.3 B/ยูนิต ⇒ 85 + 9×24.3 = **304 B ตรงกับที่วัด** | RUNTIME `UserData\LongYinCoop\S2\s2-snapshots.ndjson` (49,901 เฟรม, unitCount=9 ทุกเฟรม, `payloadBytes` p50) | ใช้เป็นฐานพยากรณ์ bandwidth ที่ 60 ยูนิต (§6.3) |
| A4 | 49,901 เฟรมมี frameId 3..49903 **ต่อเนื่อง 0 gap** ⇒ รอบ S2a-2 ไม่มี drop จริง | RUNTIME (ไฟล์เดียวกัน) | เป็นหลักฐานว่า "gap ที่จะวัดได้ใน S2a-3" ต้อง ** induce เอง** (client-side drop) ไม่มีของจริงให้รอ |
| A5 | **ยังไม่มี handshake ในโค้ดที่รัน**: `S2Auth` มีแต่ primitive, `S2HostService` ไม่เคยส่ง `Hello` และไม่เคยตรวจ `Auth` (gate แค่นับ `ControlFramesHandled`) | CODE `S2/Protocol/S2Auth.cs`, `S2/S2HostService.cs:293-305`, `S2/Protocol/S2Types.cs:36` | S2a-3 = **implementation แรกของ handshake** ต้องมี test vectors + timeout/rate-limit ครบ |
| A6 | **`RESYNC` ถูก allowlist ผ่านแต่ไม่มีใครทำอะไร**: `S2GuestGate.GateOnType` คืน `Control` แล้วทิ้ง message | CODE `S2/Host/S2Counters.cs:137-152` | ต้องมี `S2ControlHandler` + counter `ResyncServed` + snapshot ที่ติดธง `resync` |
| A7 | wire payload **ไม่มี** `replicaHash`, ไม่มี timestamp, ไม่มี `sessionId` | CODE `S2/Host/S2Sender.cs:115-147` | divergence และ latency วัดไม่ได้ถ้าไม่เพิ่มฟิลด์ → **ProtoVer 2** (Q1) |
| A8 | `net.cfg` มีแค่ `enabled/role/secret/allowNonLoopback` — **ไม่มี bind/port/rate** | CODE `S2/S2NetConfig.cs`, งาน A | ต้องเพิ่ม key + เทสต์ (Q2) |
| A9 | frame-time **p99 วัดจาก recorder 2 Hz ไม่ได้** (p99 จริง 33.5 ms; noise ระหว่างบล็อก 30 s เอง median 0.067 / p95 0.568 / max 1.101 ms) | RUNTIME `evidence/S2a2-.../frame-time-measurability-2345.txt` | exit 5 (p99) ต้องมี **เครื่องวัดต่อเฟรมในมอด** หรือยอมรับ NOT-RUN (Q3) |
| A10 | `sampleMs+serializeMs` p99 = **0.1047 ms** (48,565 เฟรม) ที่ 9 ยูนิต | RUNTIME `evidence/S2a2-.../metrics-summary.txt` | เกณฑ์ 4 ms ยังมี margin มาก แต่ **ห้ามยกผลไป 60 ยูนิต** |
| A11 | `battleKey` เป็น **candidate** (sha256 ของ teamIDs + frame แรกที่เห็น) — ยังไม่เคยวัดความเสถียร | CODE `S2/Host/S2Sampler.cs:148-178` + SPEC §13.2 ข้อ 2 | S2a-3 วัดเพิ่ม (ฟรี) ว่า battleKey เปลี่ยนกลางศึก/ข้ามเทิร์นไหม — ถ้าเปลี่ยน ใช้ `sessionId` เป็นกุญแจ resync |

**สรุปผลกระทบ:** โครง S2a-2 ใช้ต่อได้ แต่ **3 จุดต้องแก้ก่อนต่อสายจริง** — seq semantics (A1), อัตราส่ง (A2), และ envelope ที่ขาด hash/timestamp/session (A7)

---

## 2. Architecture + data flow + ไฟล์/คลาส

### 2.1 data flow (S2a-3 = เครื่องเดียว, สองโปรเซส)

```
[Unity main thread]                     [sender thread]                [socket writer thread]
S2Sampler.Read()  ──POCO──► S2HostService ──TryPost──► S2SerializingSender ──enqueue──► TcpTransport
  (IL2CPP typed read,        (pacing/dedup/                 (encode v2 + chunk)      (framed bytes)
   tApplyUs at entry)         seq assignment)                                             │
                                                                                          ▼ 127.0.0.1:47800
                                                                        ┌──────── viewer-client.py (guest) ────────┐
                                                                        │ stream reader → frame → chunk → snapshot │
                                                                        │ → decode → replicaHash recompute → store │
                                                                        │ → viewer.ndjson (per message)            │
                                                                        └──── PING / RESYNC / BYE (allowlist) ─────┘
                                                                                          │
[Unity main thread] ◄── TryReceive ◄── TcpTransport(inbox) ◄───────────────────────────────┘
        └─► S2GuestGate(allowlist) → S2ControlHandler → counters/resync (ไม่แตะเกม)
```

**threading (บังคับตาม §1.4):** IL2CPP อ่านเฉพาะ main thread · serialize ที่ sender thread · socket I/O ที่ transport thread · main thread ห้าม block เกิน 4 ms/เฟรม · object ของ IL2CPP ห้ามข้ามเฟรม

### 2.2 ไฟล์/คลาสที่จะเพิ่ม

| ไฟล์ | คลาส/เนื้อหา | เหตุผล |
|---|---|---|
| `src/LongYinCoop/S2/Transport/TcpFrameStream.cs` | `TcpFrameStream` — buffer stream → frame (partial/coalesced, cap 1100, fail-closed) **pure, ไม่มี socket** | ทดสอบ reassembly ได้โดยไม่ต้องมีเน็ต |
| `src/LongYinCoop/S2/Transport/TcpTransport.cs` | `TcpTransport : ITransport` — listen/accept (host), connect (guest/ทดสอบ), bounded **message** queue, writer thread, drops ไม่ block | S2a-3 ตัวจริง |
| `src/LongYinCoop/S2/Protocol/S2Handshake.cs` | `S2Handshake` — state machine (Hello→Auth→Result→Deny), nonce ต่อ connection, timeout 5 s, pending ≤4, rate 1/5 s/IP, fieldHash echo check | A5 |
| `src/LongYinCoop/S2/Protocol/SeqTracker.cs` | `SeqTracker` — expectedSeq, gap detect (host+guest role), resync request/ack, counters **pure** | A1/A6 |
| `src/LongYinCoop/S2/Protocol/BindPolicy.cs` | `BindPolicy.Validate(bind, allowNonLoopback, secret)` — ปฏิเสธ `0.0.0.0`/`::`/`*`/hostname/ว่าง, loopback เท่านั้นถ้าไม่ตั้ง pref **pure** | §13.2 ข้อ 6 |
| `src/LongYinCoop/S2/Host/S2ControlHandler.cs` | จัดการ `Auth/Ping/Resync/Bye` → counter + resync (ไม่มี write path ใด ๆ) | A6 |
| `src/LongYinCoop/S2/Host/S2FrameMeter.cs` | `Add(double deltaMs)` + window p50/p95/p99 (ring 4096, pure) + adapter อ่าน `Time.unscaledDeltaTime` | A9 |
| `tools/viewer-client.py` | guest client: handshake, stream/chunk, decode v2, **reimplement replicaHash**, metrics ndjson, `--inject-*`, `--report` (stdlib) | exit 1/2/3/4 วัดจากตัวนี้ |
| `tests/S2Protocol.Tests/S2a3Tests.cs` | ชุดเทสต์ใหม่ (§5) | — |
| `tests/S2Protocol.Tests/FakeS2Host.cs` | host ปลอม (ไม่มีเกม) สำหรับ T1 + ให้ Python client ยิงใส่ได้ | ทดสอบโดยไม่เปิดเกม |
| `tests/S2Protocol.Tests/fixtures/replica-vectors.ndjson` | vector ที่ C# สร้าง → Python ใช้ `--selftest` | กัน Python hash เพี้ยน |

### 2.3 ไฟล์ที่จะแก้ (แก้เท่าที่จำเป็น)

| ไฟล์ | แก้อะไร | ความเสี่ยง |
|---|---|---|
| `S2/Protocol/S2Types.cs` | `ProtoVer 1→2`, เพิ่ม `ResyncAck=11`, helper `IsDataMessage` | ต้องแก้พร้อม codec/client (atomic) |
| `S2/Host/S2Snapshot.cs` | envelope: `SessionId`, `HostApplyUs`, `ReplicaHash`, `Resync`, `ResyncFromSeq` + ndjson **additive** (`sessionId`,`hostApplyUs`,`replicaHash`,`resync`,`wireBytes`) | ฟิลด์เดิมไม่เปลี่ยนชื่อ คงเทียบกับ S2a-2 ได้ |
| `S2/Host/S2Sender.cs` | **seq ตอน send**, dedup (เทียบ `replicaHash`), pacing, heartbeat, นับ wire bytes จาก `transport.Stats` และหักลบ header ให้ตรง | จุดสำคัญที่สุดของ stage |
| `S2/Host/S2Sampler.cs` | เก็บ `HostApplyUs` ที่ **ต้น** `Read()` (ก่อนแตะ IL2CPP) + คงพฤติกรรมเดิมทุกอย่าง | 1-2 บรรทัด |
| `S2/Host/S2Counters.cs` | counter ใหม่: `SeqSent, GapReports, ResyncServed, ResyncRequests, Handshake*, Denied*, MalformedTotal, WireBytes` | — |
| `S2/S2HostService.cs` | เลือก transport ตาม cfg (`loopback|tcp`), accept/handshake loop, control handling, framemeter, marker ใหม่, stop ที่ปิด socket | ไฟล์ใหญ่สุดที่แตะ |
| `S2/S2NetConfig.cs` | key ใหม่ `bind`, `port`, `sendHz`, `heartbeatMs`, `transport` (+ validate) | ต้องเทสต์ fail-closed |
| `CoopMod.cs` | `StageLabel = "S2a-3"`, `Version 0.4.0 → 0.5.0` | cosmetic |
| `S2-plan.md` | §13.10 addendum: protocol v2 + key ใหม่ + seq semantics | เอกสาร |
| `AGENTS.md` (game repo) | สถานะ stage + ข้อเท็จจริงใหม่ | commit แยก |

**ไม่แตะ:** `IpcServer.cs` (metrics อ่านจาก ndjson ไม่ใช่ IPC), `TurnGate.cs`, `tools/save-split.py`, `tools/s15d_sample.py`

---

## 3. Protocol: framing, seq, gap, RESYNC, disconnect/reconnect

### 3.1 framing บน TCP (ใช้ header เดิมทั้งหมด)
```
[u16 BE bodyLen][u8 type][u8 ver=2][u32 BE seq][payload]     2+bodyLen ≤ 1100
```
- `TcpFrameStream` รับ byte ทีละก้อนได้ (1 B ถึงหลาย frame ติดกัน) · frame ผิด (`bodyLen<6`, `2+bodyLen>1100`, type ไม่รู้จัก, ver ไม่ตรง) → **ปิด connection ทันที** + log reason (fail-closed, ไม่พยายามเดา)
- message ใหญ่ → `Chunk{msgId, idx, total, type}` เดิม (cap 64 KB, timeout 2 s, pending ≤8)
- **queue เป็น "message" ไม่ใช่ "frame"** — chunk ทุกใบของ message ถูก enqueue/ทิ้งพร้อมกัน ⇒ ไม่มีทางประกอบ message จาก chunk ที่ขาดกลาง (capacity 4, ทิ้งใบเก่าสุด + นับ)

### 3.2 sequence
| กติกา | ค่า |
|---|---|
| `seq` นับอะไร | **เฉพาะ data message ที่ host ส่งจริง** (`Snapshot`) — เริ่ม 1 ต่อ connection |
| message ควบคุม (`Hello/AuthResult/Deny/Pong/ResyncAck/Bye`) | `seq = 0` และ **ไม่ถูกนับ** |
| ใครกำหนด | sender thread ตอน `Send` สำเร็จ (ไม่ใช่ sampler) ⇒ drop/pacing **ไม่ทำให้ seq ขาด** |
| guest คาดหวัง | `expectedSeq` ต่อ session; `newSeq == expectedSeq` = ปกติ, `newSeq > expectedSeq` = **gap**, `newSeq < expectedSeq` = duplicate/stale (ทิ้ง + นับ) |
| เปลี่ยน session | `sessionId` ใหม่ทุก connection → guest ล้าง store, `expectedSeq=1` |

### 3.3 ตรวจจับ gap และ RESYNC
guest ยิง `Resync{lastSeq}` เมื่ออย่างใดอย่างหนึ่ง:
1. **seq gap** (`newSeq > expectedSeq`) — บันทึก `gapSize`, เปิด "gap window"
2. **ไม่มีข้อมูลเกิน 2×`heartbeatMs`** (ดีฟอลต์ 2 s) — บันทึก `stallDetected`
3. **Div-A mismatch** (replicaHash ที่คำนวณเอง ≠ ที่ host ส่งมา) — บันทึก `hashMismatch`

host เมื่อได้ `Resync`:
- `ResyncRequests++` → ส่ง `ResyncAck{fromSeq, nextSeq, state=idle|battle}` **ทันที** (seq=0, ไม่กิน seq) แล้วส่ง snapshot ถัดไปพร้อมธง `resync=true, resyncFromSeq=lastSeq` **ข้าม pacing 1 ครั้ง**
- `ResyncServed++` + marker `resync.served{fromSeq,ms}` ลง `s2-metrics.ndjson`
- rate limit: ยอมรับ Resync ≤ 5 ครั้ง/วินาที ต่อ connection (เกิน = ทิ้ง + นับ `ResyncThrottled`)

guest ปิด gap window เมื่อได้ snapshot ที่ `resync=true` **และ** Div-A ตรง ⇒ บันทึก `gapOpenMs` (p50/max) ลง summary
ถ้า 500 ms ไม่ได้ resync → ยิงซ้ำ (สูงสุด 3) → ยังไม่ได้ = ปิด connection แล้ว reconnect (นับ `resyncGiveUp`)

### 3.4 disconnect / reconnect (guest)
- ตรวจด้วย: `recv` คืน 0 / exception / ไม่มี heartbeat
- backoff 0.5 → 1 → 2 → 4 s (เพดาน 4 s) · reconnect แล้ว handshake ใหม่ (nonce ใหม่, secret เดิม)
- ระหว่างหลุด: host **ไม่ปิด listener** และ **ไม่ปิด state อะไรของเกม** (ไม่มี state ให้ปิด) · เขียน `session.close{reason}` + `session.open` ใหม่
- host ปิด connection เมื่อ: protocol violation 3 ครั้งติด / write error / ไม่มี traffic ใด ๆ 60 s (`guestIdleMs`)

### 3.5 envelope v2 (payload ของ `Snapshot`)
```
u8 ver=2 | u32 sessionId | varu64 hostApplyUs | 32B replicaHash | u8 flags(resync) | varu32 resyncFromSeq
| str battleKey | varu32 turnIndex | str activeUnitId | u8 paused | varu32 nowActiveState | f32 battleTime
| varu32 unitCount | unit×N { str stableId | varu team | varu heroId | varu row | varu col | f32 hp | f32 maxHp
                     | u8 isAlive | u8 unitAlive | u8 moved | u8 attacked | u8 playerControl }
```
- ต้นทุนเพิ่ม ≈ 40 B/snapshot (ยอมรับได้) · `unitAlive` **คงไว้** (ห้ามแก้ hash spec `s2|2` — มติ 2026-10-21 ข้อ 2(ii))
- `replicaHash` ที่ส่ง = ค่าที่ host คำนวณ (ตัวเดียวกับ ndjson) เพื่อให้ guest เทียบแบบ end-to-end

---

## 4. Security

| ข้อบังคับ | กลไก | วัด/ทดสอบ |
|---|---|---|
| bind เฉพาะ loopback | `BindPolicy`: รับเฉพาะ IP literal; **ปฏิเสธ** `0.0.0.0`,`::`,`*`,hostname,ว่าง; ถ้าไม่ใช่ loopback ต้อง `allowNonLoopback=true` **และ** มีอนุมัติเป็นลายลักษณ์ (S2b) | เทสต์ 8 เคส + ตรวจ `Socket.LocalEndPoint` หลัง bind ว่าเป็น loopback + log บอก IP:port |
| authentication | shared secret (hex ≥32) → `Hello{nonce}` → `Auth{HMAC(secret, nonce‖ver), ver, fieldHashPrefix8}` → `AuthResult`/`Deny` · เทียบด้วย `FixedTimeEquals` · secret **ไม่เคยส่งบนสาย/ไม่ log** | vector T0 + secret ผิด/สั้น/HMAC เพี้ยน/protoVer ผิด/fieldHash ผิด → DENY ≤2 s (fieldHash ≤1 s) |
| guest อ่านอย่างเดียว | allowlist 4 ชนิด (`Auth/Ping/Resync/Bye`) เท่านั้น · **ไม่มี type คำสั่งเกมในสารบบ** · ไม่มีโค้ดเขียน state เกมใน `S2/**` | (a) ยัด Hello/Snapshot/BattleEvent(chunk) → DROP 3 + `commandsAccepted=0` + audit tuple เท่าเดิม (ต่อจาก self-test เดิม) (b) grep call sites (`MoveCommand.StartMove|AttackCommand.StartAttack|BattlePauseButtonClicked|GameDataController.Save|Load`) ใน `src/LongYinCoop/S2/**` = 0 (c) ไม่มี `IpcServer` command ใดถูกเรียกจาก S2 |
| ขนาด/ abuse | guest frame: single frame เท่านั้น, payload ≤ 64 B, เกิน = malformed · violation 3 ครั้ง = ปิด · handshake pending ≤4, timeout 5 s, rate 1/5 s/IP | เทสต์ malformed 12 เคส |
| ไม่มี TLS | ยอมรับเป็นข้อจำกัด (loopback เท่านั้นใน stage นี้); S2b ต้องพึ่ง VPN + secret | บันทึกในรายงาน |
| denylist | ไม่แตะ Steam/`APIDispatcher`/`DnsChecker`/`CheatCheck`/`cheat*`/`Save/Load` · ไม่เพิ่ม dependency | ตรวจ diff + `csproj` review |

---

## 5. Test matrix

ชั้น: **T0** = pure (ไม่ socket) · **T1** = in-proc จริงบน `127.0.0.1` (ยังไม่ต้องมีเกม) · **T2** = เกมจริง + `viewer-client.py`

| # | เคส | ชั้น | คาดหวัง | หลักฐาน |
|---|---|---|---|---|
| U1 | frame stream: ทีละ 1 B / รวม 3 เฟรม / frame 1100 B พอดี / 1101 B | T0 | ประกอบถูก, 1101 = ปิด | `tests` log |
| U2 | frame ผิดรูป: `bodyLen<6`, type=0, ver≠2, trailing bytes | T0 | ปิด connection + reason, ไม่ throw รั่ว | |
| U3 | chunk: ปกติ / idx ซ้ำ / idx เกิน total / total ไม่ตรง / timeout 2 s | T0 | complete เมื่อครบ, ที่เหลือ = drop | |
| U4 | seq: ปกติ, gap 1, gap 17, duplicate, wraparound u32 | T0 | gap/duplicate นับถูก, ไม่ throw | |
| U5 | resync state machine: ขอ→ack→snapshot(resync)→ปิด window; 3 ครั้งไม่ได้ → giveUp | T0 | ทรานซิชันครบ + เวลาไม่เกิน limit | |
| U6 | handshake: สำเร็จ / secret ผิด / สั้น / HMAC เพี้ยน / protoVer ผิด / fieldHash ผิด / timeout / pending เกิน / rate | T0 | ผล + เวลาตรงสเปก | |
| U7 | bind policy 8 เคส (`127.0.0.1`, `localhost`, `0.0.0.0`, `::`, `*`, `192.168.x.x` + ไม่ตั้ง pref, ว่าง, `::1`) | T0 | ผ่านเฉพาะ loopback literal / `::1`; ที่เหลือปฏิเสธพร้อมเหตุผล | |
| U8 | replicaHash: vector 5 ชุด (C#) ↔ Python recompute | T0/T1 | ตรงทุกไบต์ | `fixtures/replica-vectors.ndjson` |
| U9 | **wire v2 roundtrip**: encode→TCP→decode ผ่าน header/session/timestamp/hash | T1 | field ครบ, hash ตรง | |
| U10 | bandwidth/projection: 9/18/24/60 ยูนิต × sendHz | T0 | ตัวเลขตรงสูตร §6.3 (±1 B) | |
| I1 | listen/accept บน 127.0.0.1 พอร์ตว่าง → handshake สำเร็จ → 100 snapshot ต่อเนื่อง | T1 | ครบ 100, seq ต่อเนื่อง, ไม่มี drop | |
| I2 | client หยุดอ่าน 3 s (ไม่ recv) → host `Send` ยังไม่บล็อก (< 1 ms/call) + drop นับ + ไม่ crash | T1 | ผ่าน; หลัง client กลับมาอ่านได้ snapshot ใหม่ + resync | |
| I3 | client ตัด connection กลาง chunk | T1 | host ไม่ crash, ปิด session, accept ใหม่ได้ | |
| I4 | reconnect 3 รอบ (backoff) | T1 | sessionId ใหม่ทุกรอบ, seq รีเซ็ต, store ล้าง | |
| I5 | inject forbidden (`Hello/Snapshot/BattleEvent-chunk`) จาก client | T1 | `forbiddenTypeDrops=3`, `commandsAccepted=0` | |
| I6 | inject malformed 12 แบบ (สั้น/ยาว/type มั่ว/chunk ซ้ำ/payload 65 B) | T1 | ปิดตามกติกา / ไม่ crash / นับครบ | |
| I7 | inject gap (client ทิ้ง seq เอง) → resync → Div-A = 0 | T1 | gap ตรวจเจอ, resync สำเร็จ, gap window ปิด | |
| I8 | 2 client พร้อมกัน (กรณีไม่ควรเกิด) | T1 | ปฏิเสธตัวที่สองอย่างสุภาพ (`Deny{busy}`) หรือปิดตัวเก่า — **เลือก: ปฏิเสธตัวใหม่** | |
| I9 | Python client ↔ fake host (`--serve-fake`) 60 s | T1 | ndjson ครบ, `--report` ให้ p95/bytes/s, Div-A=0 | |
| E1 | **เกมจริง**: เข้าศึก → client ต่อ → handshake OK → ได้ snapshot จริง | T2 | log 2 ฝั่งตรงกัน, unitCount ตรงกับ `spike status` รอบเดียวกัน | |
| E2 | 5 นาที delivery ต่อเนื่อง (ผู้เล่นเล่น 2-3 เทิร์น) | T2 | latency/bandwidth/Div-A/Div-B (§6) | |
| E3 | ผู้เล่น Esc 1 ครั้ง (บอกผม) | T2 | บันทึกหน้าต่าง exclude, ไม่ตีความเป็น pause ของ S2 | |
| E4 | stall 60 s (client หยุดอ่านเอง) | T2 | host p99 (§6.5) + ไม่ crash + กลับมาอ่านได้ | |
| E5 | disconnect 60 s แล้ว reconnect | T2 | handshake ใหม่สำเร็จ, Div-A=0 หลังต่อ | |
| E6 | ยิง forbidden จาก client ระหว่างเกมจริง | T2 | audit tuple ไม่เปลี่ยน + counters | |
| E7 | ปิดเกม → `verify`/`restore` | T2 | เซฟ 57 ไฟล์ `4226730f66ff`, `leftovers: none` | |

**กติกาการนับ verdict:** FAIL ข้อใด = รายงานตรง ๆ ห้ามเลื่อนเกณฑ์ · เคสที่ "วัดไม่ได้" = **NOT-RUN** (ห้ามเขียน PASS) · เคสที่ได้ผลแต่เล็กกว่าเป้า (เช่น unit count < 60) = **PASS-with-limitation** + เหตุผล

---

## 6. เกณฑ์วัด + วิธีวัดที่ทำซ้ำได้

### 6.1 แหล่งเวลา (ต้องพิสูจน์ก่อนใช้ ไม่เดา)
- host stamp: `Stopwatch.GetTimestamp()` → µs (`HostApplyUs` = **ต้น** `S2Sampler.Read()` = "instant ที่มอดเห็น state ของเกมครั้งแรก")
- guest stamp: `time.perf_counter_ns()/1000`
- สมมติฐานที่ต้องวัด: บน Windows ทั้งสองฝั่งใช้ QPC → **offset ≈ 0** · วัดด้วย PING/PONG 4 timestamp (สูตร NTP) ทุก 5 s, ใช้ **median ของ 5 ค่าล่าสุด** เป็น offset
- **เกณฑ์ความน่าเชื่อถือ:** spread ของ offset ในหน้าต่างวัด ≤ **0.5 ms** ⇒ ใช้ได้; เกิน ⇒ verdict = PASS-with-limitation (รายงาน raw + corrected)
- latency = `recvUs − HostApplyUs − offset` · รายงาน p50/p95/p99/max + n + unit count + offset(median, spread)

### 6.2 ตัวชี้วัดและเกณฑ์

| ID | เกณฑ์ (SPEC) | metric | เครื่องมือ | ผ่านเมื่อ | ถ้าวัดไม่ได้ |
|---|---|---|---|---|---|
| **X1** | loopback latency **p95 ≤ 40 ms** | host-apply → guest-store ต่อ snapshot | `viewer.ndjson` (§6.1) | p95 ≤ 40 ms, n ≥ 1000 | n < 1000 หรือ offset spread > 0.5 ms ⇒ PASS-with-limitation |
| **X2** | bandwidth **≤ 30 KB/s เฉลี่ย 5 นาที** | wire bytes ที่ guest อ่านได้ (รวม framing, ไม่รวม TCP/IP header) / วินาที | `viewer.ndjson` bucketed 1 s | avg ≤ 30 KB/s ในหน้าต่าง 300 s | unit count < 60 ⇒ **PASS-with-limitation + พยากรณ์ §6.3** (ห้ามเรียกว่าผ่านที่ 60) |
| **X3** | divergence **= 0** | **Div-A**: `sha256(replicaHash ที่ guest คำนวณจาก field ที่รับ) ≠ replicaHash ในข้อความเดียวกัน` | client recompute + `replicaHash` บน wire | mismatches = 0 จากทุก snapshot ที่รับ (≥1000) **และ** unit count/duplicate id ตรง | ถ้า client ไม่ได้ recompute = NOT-RUN |
| **X4** | seq ขาด → resync → divergence 0 | `gapDetected ≥ 1`, `resyncServed ≥ 1`, `gapOpenMs` ปิดทุก window, Div-A หลัง resync = 0 | counters 2 ฝั่ง + marker | ครบทุกข้อ | ไม่มี gap จริง (A4) ⇒ ต้อง induce; ถ้า induce ไม่ได้ = NOT-RUN |
| **X5** | host frame time **p99 เปลี่ยน ≤ 1 ms** ขณะ guest หน่วง/หลุด | `Time.unscaledDeltaTime` ต่อเฟรม, window 60 s, p99 | `S2FrameMeter` ในมอด (marker `framemeter.window`) | `p99(B1|B2) − p99(A0) ≤ 1 ms` และ exception = 0 | ไม่มี meter (ถ้าไม่อนุมัติ Q3) = **NOT-RUN** · ถ้า |delta−1 ms| ≤ 0.5 ms = INCONCLUSIVE (กติกาเดียวกับ rev5) |
| **X6** | main-thread cost ต่อเฟรม **p99 ≤ 4 ms** | `sampleMs + encodeMs` (เดิมของ S2a-2) | `S2Metrics` (มีอยู่) | p99 ≤ 4 ms | — |
| **X7** | auth/bind | bind loopback ✓; secret ผิด → DENY ≤ 2 s; fieldHash ผิด → DENY ≤ 1 s; bind นอก loopback ถูกปฏิเสธ | marker + `handshake` log | ครบ | — |
| **X8** | guest อ่านอย่างเดียว | `commandsAccepted = 0`, `forbiddenTypeDrops = 3` (self-test), audit tuple เท่าเดิม, grep call sites = 0 | counters + `grep` | ครบ | grep ทำมือ = หลักฐานใน evidence |
| **X9** | ไม่แตะเซฟ | `SaveSlot*` เขียน 0 ไฟล์; digest = `4226730f66ff`; `leftovers: none` | `save-split.py verify/status` | ครบ | — |

### 6.3 พยากรณ์ bandwidth (จาก A3: header 85 B + 24.3 B/ยูนิต + envelope v2 40 B + frame header 8 B/frame; frame ละ ≤ 1092 B payload)

| ยูนิต | payload v2 | จำนวน frame | wire/snapshot | @10 Hz | @30 Hz | @60 Hz (พฤติกรรม S2a-2) |
|---|---|---|---|---|---|---|
| 9 | 344 B | 1 | 352 B | 3.4 KB/s | **10.3 KB/s** | 21.1 KB/s |
| 18 | 562 B | 1 | 570 B | 5.6 KB/s | **16.7 KB/s** | 33.4 KB/s ✗ |
| 24 | 708 B | 1 | 716 B | 7.0 KB/s | **21.0 KB/s** | 42.0 KB/s ✗ |
| 60 | 1583 B | 2 | 1599 B | 15.6 KB/s | **46.9 KB/s ✗** | 93.7 KB/s ✗ |

**ข้อสรุปที่ต้องพูดตรง ๆ:** ที่ **60 ยูนิต full snapshot 30 Hz = 46.9 KB/s > 30 KB/s** ⇒ ถ้าต้องผ่านทั้ง X1 (40 ms) และ X2 (30 KB/s) ที่ 60 ยูนิตพร้อมกัน **codec นี้ทำไม่ได้** ต้องมี delta/event encoding (แผนจัดไว้ S2b) · ที่ 18 ยูนิต 30 Hz = 16.7 KB/s ผ่าน X2 แต่ p95 อาจชน 40 ms ถ้า change ส่วนใหญ่ถูก pacing ทิ้ง — จะวัดจริงและรายงานตามจริง (ห้ามลดเกณฑ์) · S2a-3 วัดที่จำนวนยูนิตจริงของเซฟ (เคยได้ 9 และ 18) แล้วรายงาน **PASS-with-limitation + พยากรณ์** (Q9)

### 6.4 นิยาม divergence (ขอบเขตชัดเจน — ห้ามหมายความเกิน)
| ชั้น | นิยาม | วัดได้ใน S2a-3? | เกณฑ์ 0 ใช้กับชั้นนี้? |
|---|---|---|---|
| **Div-A** | guest แปลง message → คำนวณ `replicaHash` ใหม่ = ค่าที่ host ส่งมาในข้อความเดียวกัน (fidelity ของ codec + hash spec + framing) | ✅ วัดได้เป๊ะทุก message | **ใช่ — X3/X4** |
| **Div-B** | guest store "เก่า" กว่าข้อมูลล่าสุดของ host (seq gap / stall) | ✅ วัดเป็น gap window (ms) + resync | ไม่ใช่ divergence แต่ **ต้องปิดทุก window** (X4) |
| **Div-C** | guest store ≠ state จริงของเกม ณ เวลานั้น | ❌ ไม่มี peered game ฝั่ง guest (overlay = S2a-4) | **ไม่กล้าเคลม** — ใกล้สุดคือ exit 4 ของ S2a-2 (9 ยูนิต, ไม่ขยายผล) |

### 6.5 ลำดับวัดในเกมจริง (ตามลำดับ ห้ามสลับ)
1. `phase A0` S2 ปิด 60 s (baseline ของ X5) — ต้องอยู่ `WaitPlayerInput` และ **ห้ามสั่ง action**
2. `phase W` S2 เปิด + client ต่อ 300 s (ผู้เล่นเล่น 2–3 เทิร์น) → X1/X2/X3 + X6 + unit count
3. `phase B1` client stall 60 s (client หยุดอ่านเอง) → X5
4. `phase B2` client ถูก kill 60 s → X5 + host log 0 exception
5. `phase B3` client reconnect 60 s → X4 (resync) + Div-A = 0
6. `phase A1` S2 ปิด 60 s อีกครั้ง (ยืนยัน baseline ซ้ำ) → X5 ใช้ `A0`+`A1` เฉลี่ย
- ทุกบล็อก: ตรวจว่า `sampler.ndjson` ยังโต (recorder ตาย = ทิ้งบล็อกนั้น) + จด wall-clock
- **ห้าม pause ตอน `WaitPlayerInput`** (มติ S1.5) · ถ้ากด Esc → บอกผมเพื่อ exclude

---

## 7. แผน implementation เป็นช่วง + acceptance + rollback + หลักฐาน

| ช่วง | ทำอะไร | ต้องมีเกม? | ต้องอนุมัติ? | acceptance | rollback |
|---|---|---|---|---|---|
| **C0** | แผนนี้ | ❌ | **รออนุมัติ (ตอนนี้)** | ผู้ใช้อนุมัติ + ตอบ Q1–Q11 | ไม่มีอะไรเปลี่ยน |
| **C1** | เขียนโค้ด §2.2/§2.3 + เทสต์ §5 (T0/T1) · **ไม่ deploy** | ❌ | อนุมัติแล้วจาก C0 | `dotnet run --project tests\S2Protocol.Tests` = PASS ทุกเคส, 0 FAIL · รายงาน diff | `git checkout -- .` / revert commit |
| **C2** | commit source (ยังไม่ build) + ส่งสรุป diff | ❌ | **gate #1 (อนุมัติ build)** | tree สะอาด + commit hash | revert commit |
| **C3** | `dotnet build -c Release` → sha256 DLL + build log (ยังไม่ copy) | ❌ | gate #1 | 0 error/0 warning ใหม่ | ลบ `bin/` |
| **C4** | สำรอง DLL ปัจจุบัน (`LongYinCoop.dll` → `.off`/`.prev-<hash>`) · backup `MelonPreferences.cfg` + `net.cfg` · **ล้าง dotted key เก่าใน MelonPreferences.cfg ถ้ามี** (ต้นเหตุ incident 23:54) | ❌ | **gate #2 (อนุมัติ deploy)** | sha256 ครบทั้งก่อน/หลัง + ไฟล์ใหม่ตรง build | copy `.off` กลับ |
| **C5** | copy DLL+PDB เข้า `Mods\` · ตั้ง `net.cfg` (enabled=false, role=host, secret 32B hex, bind=127.0.0.1, port=47800, sendHz=30) + sha256 | ❌ | gate #2 | sha256 ใน `Mods\` = C3 | คืน `.off` + คืน cfg |
| **C6** | session-start checks (hash เกม, เกมไม่รัน, `save-split status`=57/`4226730f66ff`/leftovers none) → `split --expected-baseline coop-test\baselines\20261022-075422\savehash.real.before.txt` → recorder `s15d_sample.py` → `preflight` **READY** | ❌ | gate #2 | READY + split manifest | `restore` |
| **C7** | **ผู้ใช้เปิดเกม** → เข้าศึก → แจ้งผม → ผมรัน §6.5 → ผู้ใช้เล่น 2–3 เทิร์น → แจ้ง Esc/เหตุการณ์ → ปิดเกม | ✅ | **ผู้ใช้เปิดเองเท่านั้น** | ทุก phase ครบ + `viewer.ndjson` + markers | ปิดเกม + `.off` |
| **C8** | ปิดเกม → `verify` → `restore` → คืน pref + `net.cfg` (sha256 ตรงก่อนตั้ง) → `rebaseline` ถ้า pref drift → รวบรวม evidence | ❌ | — | 57 ไฟล์ `4226730f66ff`, `leftovers: none` | restore ซ้ำ |
| **C9** | รายงานเทียบ X1–X9 ทีละข้อ + rollback test | ❌ | **gate #3 (อนุมัติ tag `checkpoint-S2a`)** | verdict ครบทุกข้อ (รวม NOT-RUN) | — |

**มติ 2026-10-22 (อัปเดต baseline ของ stage):** baseline ที่ใช้ตัดสิน E7/X9/C6/C8 เปลี่ยนจาก `854cc890430f` → **`4226730f66ff`** (`coop-test/baselines/20261022-075422`) หลังคืนเซฟจริงของ session `explore-20261022-0755` แล้ววัดได้ 57 ไฟล์ `changed=0 missing=0 added=0` · หลักฐาน `evidence/S2a3-20261022-0156/restore-postcheck.md`, `presave-check.md`, `coop-test/baselines/20261022-075422/REBASELINE.json` + `APPROVAL.md` · `854cc890430f` ยังเป็นประวัติของ S2a-2 (ห้ามใช้เป็นเกณฑ์ของ stage นี้)

**หลักฐาน `evidence/S2a3-<ts>/`**
`plan-ref.md` · `code-diff-summary.md` · `commit.txt` · `tests.txt` (จำนวน PASS/FAIL) · `build.txt` (log + sha256) · `dll-before-after.txt` · `prefs-sha-before.txt` · `prefs-sha-after.txt` · `netcfg-values.txt` (ไม่ log secret) · `preflight.txt` · `split.json` · `sampler.ndjson` · `s2-snapshots.ndjson` · `s2-metrics.ndjson` (จาก `UserData\LongYinCoop\S2`) · `viewer.ndjson` · `viewer-summary.txt` · `latency.txt` · `bandwidth.txt` · `divergence.txt` · `resync.txt` · `framemeter.txt` · `unit-count.txt` · `audit-tuple.txt` · `nowrite-grep.txt` · `verify-restore.txt` · `report.md`

---

## 8. Build / Deploy / เปิดเกม — จุดที่ต้องขออนุมัติ (ชัดเจน)

| จุด | สิ่งที่ทำได้เมื่ออนุมัติ | ต้องขออนุมัติก่อน |
|---|---|---|
| **gate #1** หลัง C1/C2 | `dotnet build -c Release` (สร้าง DLL ใหม่) | ✅ ต้องอนุมัติ **ก่อน build** (ตามกติกา ห้าม build จนได้รับอนุมัติ) |
| **gate #2** หลัง C3 | copy DLL เข้า `Mods\`, แก้ `net.cfg`/`MelonPreferences.cfg`, split เซฟ, เปิด recorder, preflight | ✅ ต้องอนุมัติ **ก่อน deploy** |
| **เปิดเกม** | — | **ผมไม่เปิดเองเด็ดขาด** — ผู้ใช้เปิดหลัง preflight READY เท่านั้น |
| **gate #3** หลัง C9 | tag `checkpoint-S2a` (+ `stage-S2a-3` เมื่ออนุมัติปิด stage) | ✅ อนุมัติหลังอ่านรายงาน |

หมายเหตุ: `dotnet run --project tests\S2Protocol.Tests` (C1) **ไม่ใช่การ build/deploy มอด** — มัน link source แบบ read-only เข้า assembly ของเทสต์ (`S2Protocol.Tests.csproj:13-28`) และไม่แตะ `Mods\` ( precedent: S2a-2 step 0 ทำแบบนี้แล้ว )

---

## 9. ความเสี่ยง

| ความเสี่ยง | ผล | ทางลด |
|---|---|---|
| เปลี่ยน seq semantics กระทบ ndjson เดิม | เทียบกับ S2a-2 ตรง ๆ ไม่ได้ | เพิ่ม `frameId` แยกจาก `seq` ใน ndjson (additive) + จดใน §13.10 |
| pacing/dedup ทำให้ "เห็น state ใหม่ช้า" | ผู้เล่นรู้สึกหน่วง | change-triggered ส่งทันที + heartbeat 1 Hz + วัด staleness (Div-B) แยกจาก latency |
| dedup ซ่อนการเปลี่ยนแปลง | viewer ค้างค่าเก่า | hash ครอบ hp/pos/alive/flags; heartbeat บังคับ refresh; ทดสอบด้วยการเดิน/โจมตีจริง |
| 60 ยูนิตชนเพดาน bandwidth (A3/§6.3) | exit 2 ผ่านไม่ได้ที่เป้า | บันทึกเป็น limitation + พยากรณ์ + เสนอ delta encoding ใน S2b (Q9) |
| thread ใหม่ (socket×2) ทำให้เฟรมสะดุด | X5/X6 ตก | bounded queue, ไม่ lock ร่วมกับ main thread, วัดด้วย meter + เทียบ A/B |
| `MelonPreferences.cfg` มี dotted key ค้าง → TOML เสีย → มอดนิ่ง (เคยเกิดจริง) | วัดไม่ได้ทั้งรอบ | C4 ล้าง key เก่า + backup + sha256 + preflight ตรวจ |
| เข้าใจผิดว่า guest สั่งได้ | ขัดเจตนา co-op | allowlist + ไม่มี type คำสั่งในสารบบ + counter + grep + audit tuple |
| TCP listener ถูก firewall/AV ทัก | เปิดเกมสะดุด | bind `127.0.0.1` เท่านั้น (ไม่ควรมี prompt) · ถ้ามี prompt = หยุดและรายงาน ไม่กด allow เอง |
| client Python ช้า/GIL ทำให้ค่าผิด | verdict เพี้ยน | client ไม่ทำงานหนักต่อ message (ไม่วาด UI, ไม่ parse JSON หนัก), เขียน ndjson แบบ buffered |
| 2 client ต่อพร้อมกัน | สับสนค่า | ปฏิเสธตัวที่สอง (`Deny{busy}`) + เทสต์ I8 |

---

## 10. คำถามที่ต้องให้ผู้ใช้ตัดสิน (ห้ามเริ่มจนได้คำตอบ)

| # | คำถาม | ข้อเสนอของผม | ผลถ้าเลือกทางอื่น |
|---|---|---|---|
| **Q1** | อนุมัติ **ProtoVer 1→2** (เพิ่ม `sessionId`, `hostApplyUs`, `replicaHash`, `resync` flag, type `ResyncAck=11`; **ไม่ลบฟิลด์ใด, ไม่แก้ `ReplicaHash` spec `s2|2`**)? | อนุมัติ | ถ้าไม่ bump: latency/divergence วัดไม่ได้ตามนิยาม ต้องใช้การประมาณ (อ่อนกว่า) |
| **Q2** | อนุมัติ key ใหม่ใน `net.cfg`: `bind=127.0.0.1`, `port=47800`, `sendHz=30`, `heartbeatMs=1000`, `transport=tcp` (+validate fail-closed)? | อนุมัติ | ถ้าไม่เพิ่ม: ต้อง hardcode (แก้ทีต้อง build ใหม่) |
| **Q3** | อนุมัติ **`S2FrameMeter`** (อ่าน `Time.unscaledDeltaTime` แบบ getter, ring 4096, เขียน marker) เป็นเครื่องวัด p99 ของ X5? | อนุมัติ (p99 วัดด้วย recorder 2 Hz ไม่ได้ — A9) | ถ้าไม่อนุมัติ: X5 = **NOT-RUN** อย่างเป็นทางการ |
| **Q4** | อนุมัติ **เพิ่มไฟล์ใหม่** `tools/viewer-client.py` (ไม่แตะไฟล์ tools เดิม) | อนุมัติ | ถ้าไม่อนุมัติ: ไม่มี guest จริง → stage นี้วัดไม่ได้ |
| **Q5** | อนุมัติการเปลี่ยน **seq = นับตอน send** (แทน sample frameId) + `frameId` คงไว้ใน ndjson | อนุมัติ | ถ้าไม่เปลี่ยน: pacing/drop ทำให้เกิด RESYNC ปลอม |
| **Q6** | อนุมัติ **change-triggered + `sendHz=30` + heartbeat 1 Hz** (ทดแทน "ส่งทุกเฟรม") | อนุมัติ | ถ้าไม่เปลี่ยน: bandwidth 18 ยูนิต @60 Hz = 34 KB/s (ตกเกณฑ์) |
| **Q7** | **ห้าม bind นอก loopback ใน stage นี้** (S2a-3 = loopback เท่านั้น; S2b ค่อยขออนุมัติ IP VPN) | ตาม §13.2 ข้อ 6 | ถ้าต้องการนอก loopback = ต้องอนุมัติเป็นลายลักษณ์ + เปิด `allowNonLoopback` |
| **Q8** | อนุมัติให้ **แตะ `IpcServer.cs` ไหม** เพื่อเพิ่มคำสั่งอ่านอย่างเดียว `s2_status` (metrics สด) | **ไม่แตะ** — อ่านจาก `s2-metrics.ndjson` พอ | ถ้าต้องการสด: +1 ไฟล์ที่มีความเสี่ยง (ต้องเทสต์ IPC ใหม่) |
| **Q9** | **60 ยูนิตยังไม่มี fixture** — ยอมรับ X2 = PASS-with-limitation + พยากรณ์ §6.3 หรือไม่ (ห้ามลดเป้า 60 ตามมติ Q4 ของ rev2) | ยอมรับ limitation + บันทึก | ถ้าไม่ยอมรับ: ต้องหา/สร้างเซฟ 60 ยูนิตก่อน (บล็อก stage) |
| **Q10** | เวลาในเกมที่ต้องใช้ ≈ **10–12 นาที** ในศึกเดียว (A0 60s + W 300s + B1/B2/B3 180s + A1 60s) — รับได้ไหม | รับได้ | ถ้าไม่ได้: ตัด W เหลือ 180 s (ผลจะ weaker, บันทึกไว้) |
| **Q11** | ระหว่างวัด **ผู้ใช้เล่น 2–3 เทิร์น** (ไม่ใช่ปล่อยนิ่ง) เพื่อให้ divergence/bandwidth มีความหมาย — ตกลงไหม | ตกลง | ถ้าปล่อยนิ่ง: ได้เฉพาะ idle-stream ไม่ครอบคลุมการต่อสู้ |

---

## 11. สิ่งที่จะไม่เกิดขึ้นใน stage นี้ (ย้ำ)
- ไม่มีคำสั่งจากเครือข่ายที่แตะ state เกม · ไม่มี `Save/Load` · ไม่แตะ Steam/สถิติ/`cheat*`
- ไม่มี overlay/IMGUI · ไม่มี role=guest ในมอด · ไม่มี VPN/ข้ามเครื่อง
- ไม่แตะเซฟจริง · ไม่เปลี่ยน baseline `4226730f66ff` · ไม่เปิดเกมเอง · ไม่ build/deploy ก่อนอนุมัติ

---

## 12. อ้างอิงภายในเล่ม
`S2-plan.md` §13.2 (ข้อบังคับ protocol/audit/bind), §13.3 (S2a-3 scope), §13.4 (exit 1–11), §13.6 (rollback) ·
`S2a-2-plan.md` §5 (นิยาม exit เดิม), §8.2 (audit คำสั่ง IPC), §8.5 (วินัยการวัด rev5) ·
`evidence/S2a2-20261021-2254/` (`metrics-summary.txt`, `frame-time-measurability-2345.txt`, `S2a2-report.md`) ·
`UserData\LongYinCoop\S2\s2-snapshots.ndjson` (49,901 เฟรม, 9 ยูนิต, 304 B) ·
`VERSION.md` (anchors + baseline `4226730f66ff`)

