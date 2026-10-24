# STAGEPLAN — LongYinLiZhiZhuan Co-op Mod (v2 merged)

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
1. **`GameDataController.Save/Load` signature จริง** — [DUMP, SP0 2026-10-25]: `Save` **1 overload** `(int)` · `Load` 1 overload `(int)` · ที่มี 2 overload คือ `GetSaveDataPath` (`(int,int)` และ `()`) → patch ให้ถูกตัว, ยืนยันว่า postfix ไม่รบกวนการเขียนไฟล์ · **ข้อมูลนี้เป็น DUMP เท่านั้น ยังไม่ใช่การยืนยัน runtime save path — ต้องวัดจริงใน S5**
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

### 12.2 Changelog
- **v1:** ฉบับแรก + S1.5 (S3-spike) + แก้ตารางสิทธิ์ Harmony ของ S0
- **v2:** S0 เปลี่ยนเป็น "ไม่แตะ state ของเกม", S2 เปลี่ยนเป็น event/delta + snapshot เต็มเป็นระยะ, S1.5 เพิ่มการพิสูจน์ว่าเกมหยุดรอ input และตรวจ `battleTime`/`battleMove`/`battlePaused`, S4 เพิ่มงานสำรวจ hour/encounter chain และต้องรัน S5 ซ้ำ, S0 เพิ่ม exit criteria 4 ข้อ, เพิ่มกฎ interop แบบมีชนิด, `VERSION.md`, repo แยก, ความเสี่ยง #5–7
- **v2 merged (หลังทดสอบ S0):** แยกเกณฑ์ IPC เป็น ping/`stats` กับคำสั่งที่แตะ IL2CPP, เพิ่ม dev command `bench_dispatch`, ระบุ loopback TCP เป็นข้อยกเว้นสำหรับทดสอบในเครื่อง, และนิยาม latency = host-apply → guest-store
- **S0 measured:** เกณฑ์ IPC เปลี่ยนเป็นอิงเฟรม (≤1× frame p95 / ≤ frame p99+10 ms พร้อมรายงาน frame time), เพิ่มการวัด frame time ใน `stats` (p50/p95/p99), และยกเว้น `PlayerprefData.dat` จากเกณฑ์ savehash หลัง control test `Enabled=false`
- **S0 closed (2026-10-20):** ผ่าน 12/12 + tag `stage-S0`; เพิ่ม §2 "carry-over จาก S0" (ข้อ 4 → S4 JIT 1, ข้อ 9 ครึ่งศึก → S1 JIT 7) และ §14 สถานะ
- **v2 merged:** ฉบับนี้ — รวม v2 เข้ากับ v1 โดยเก็บรายละเอียดของ v1 ไว้ทั้งหมด (field/ตัวเลข/ขั้นตอนย่อย) และตัดสิน 4 ข้อที่ขัดกัน: (1) S2 ใช้ **event-driven** ของ v2 (ตัด 2 Hz/20 Hz ออก, เกณฑ์ bandwidth วัดแบบ event-driven), (2) แถว raw offset บังคับเฉพาะ **โค้ดที่ ship** สำรวจยังทำได้, (3) S0 gate เรื่อง thread ผ่านได้ถ้ามี **ทางแก้ที่ทดสอบแล้ว**, (4) เรียงเลขหัวข้อตาม v2 (`§0` ขอบเขต, `§0A` สภาพแวดล้อม, `§1.3` โครงโปรเจค, `§1.7` Steam, `§12` ลำดับ+Changelog) และเพิ่ม `§13` ภาคผนวก anchor

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
