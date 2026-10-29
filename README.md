# LongYinCoop — co-op mod for LongYinLiZhiZhuan (IL2CPP)

Stage **S0 (base)**: mod โหลดได้, log แยกไฟล์, main-thread dispatcher, Harmony probe วัด thread,
inventory ของ Harmony patch ที่มีอยู่, named-pipe IPC อ่านอย่างเดียว — **ไม่แตะ state เกม**

> repo นี้อยู่นอกโฟลเดอร์เกม (`C:\Users\memph\LongYinCoop`) — build แล้ว copy DLL เข้า `Mods\`
> ห้าม commit ไฟล์เกม (`.gitignore` กันไว้แล้ว)
> สารบัญเอกสารทั้งหมด (แผน/โน้ต/สถานะแต่ละไฟล์): **`INDEX.md`** · แผนที่ใช้งานอยู่ `plans/` · แผนปิดแล้ว `plans/closed/` · โน้ตสำรวจ `notes/`

## โครงสร้าง

```
src/LongYinCoop/
  CoopMod.cs            MelonMod entry, pref gate, tick
  CoopPrefs.cs          MelonPreferences (Enabled = kill switch)
  MainThreadDispatcher.cs  ConcurrentQueue + 4 ms/frame + counters
  HarmonyProbe.cs       probe no-op บน GameController.Update (วัด thread)
  CompatInventory.cs    Harmony.GetAllPatchedMethods() -> compat-notes.md
  SnapshotReader.cs     อ่าน scene / player pos ผ่าน typed interop (namespace Il2Cpp)
  IpcServer.cs          named pipe อ่านอย่างเดียว
  S0Report.cs           UserData/LongYinCoop/s0-result.json
  UIStrings.cs          ข้อความผู้ใช้ทั้งหมด (อังกฤษก่อน)
tools/coopctl.py        CLI ทดสอบ IPC
VERSION.md              SHA-256 ของ GameAssembly.dll + global-metadata.dat
```

## Build / deploy

```bash
dotnet build src/LongYinCoop/LongYinCoop.csproj -c Debug
cp src/LongYinCoop/bin/Debug/LongYinCoop.dll  "/c/Program Files (x86)/Steam/steamapps/common/LongYinLiZhiZhuan/Mods/"
cp src/LongYinCoop/bin/Debug/LongYinCoop.pdb  "/c/Program Files (x86)/Steam/steamapps/common/LongYinLiZhiZhuan/Mods/"
```

เกมติดตั้งที่อื่น: `dotnet build ... -p:GameDir="D:\path\to\game"`

## Prefs (`UserData/MelonPreferences.cfg`, หมวด `[LongYinCoop]`)

| key | default | ความหมาย |
|---|---|---|
| `Enabled` | `false` | kill switch — false = mod ไม่ทำอะไรเลย |
| `Probe` | `true` | Harmony probe บน `GameController.Update` (วัด thread เท่านั้น) |
| `Ipc` | `true` | เปิด named pipe `\\.\pipe\longyin-coop-<pid>` |
| `JitProbe` | `false` | S1.5-c W1 — postfix อ่าน argument บน `GridUnitController.OnClick` + `BattleGridClicked` |
| `JitProbeW2` | `false` | S1.5-c W2 — `MoveButtonClicked` / `GenerateMovePath` / `CancelMove` / `RegretMove` |
| `JitProbeW4` | `false` | S1.5-c W4 — `BattlePauseButtonClicked` + `timeScale`/`battleTime` ตอนกด pause |

ทุก pref ของ wave เริ่มที่ `false` และแก้ระหว่างรันได้ผ่าน `coopctl.py set_pref --key <name> --value true|false`
(เปิด/ปิดแล้ว `UnpatchSelf()` ทันที — ไม่เหลือ patch ของ mod หลังปิด)

## IPC (อ่านอย่างเดียว)

```bash
python tools/coopctl.py ping
python tools/coopctl.py get_version
python tools/coopctl.py get_scene
python tools/coopctl.py get_player_pos
python tools/coopctl.py stats
python tools/coopctl.py bench_dispatch --n 1000   # dev: dispatcher 1,000 no-op
python tools/coopctl.py bench --cmd ping --n 1000 # วัด p99 สองแบบ
```

`bench` วัดสองโหมดแล้วรายงานคู่กัน:
- `reuse` = เปิด pipe ครั้งเดียว ยิงต่อเนื่อง N ครั้ง (วัด transport ล้วน)
- `per_call` = connect ใหม่ทุกคำสั่ง (วัดแบบ CLI จริง รวม overhead ของ pipe)

คำสั่งที่แตะ IL2CPP (`get_scene`, `get_player_pos`) จะมี latency ~1 frame เพราะรอ `MainThreadDispatcher`

framing = 4-byte little-endian length + UTF-8 JSON; 1 client ต่อ connection
ทุกคำสั่งที่แตะ IL2CPP วิ่งผ่าน `MainThreadDispatcher` (timeout 500 ms)

## S0 verification (ย่อ)

1. `sha256sum` ของ `GameAssembly.dll` + `global-metadata.dat` ตรงกับ `VERSION.md`
2. คัดลอกเซฟก่อนทดสอบตาม `STAGEPLAN.md §1.2` (เกมต้องปิดสนิท)
3. เปิดเกม → `UserData/LongYinCoop/logs/coop-YYYYMMDD.log` ต้องมี `S0 loaded`
4. `s0-result.json` ต้องมี `threadMatch: true` และ `probeHits > 0`
5. `compat-notes.md` มีรายการ patch ของ LongYinThai
6. ปิดเกม → hash เซฟจริงต้องเท่าเดิม

## ถอด mod

เปลี่ยนชื่อ `Mods/LongYinCoop.dll` → `LongYinCoop.dll.off` (MelonLoader ข้ามไฟล์ที่ไม่ใช่ `.dll`)
