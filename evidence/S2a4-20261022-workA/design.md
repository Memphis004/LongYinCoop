# งาน A — ย้าย `coop.net.*` ไป `UserData\LongYinCoop\net.cfg` (2026-10-22)

มติผู้ใช้ 2026-10-22 (หลังปิด S2a-2): ให้ `net.cfg` เป็นทางแก้ §3.3 ของ `S2a2-report.md`
· ลำดับงานที่อนุมัติ: **A → B → C** · งาน A = ไม่เปิดเกม, ไม่ build/deploy DLL, ไม่แตะเซฟจริง

## 1. เหตุผล (RUNTIME, วัดแล้ว 2026-10-22)

`MelonPreferences.cfg` ใช้เก็บ `coop.net.*` ต่อไม่ได้ 2 ทาง:

1. **ค่า string ดิบที่ไม่ครอบ quote** (`coop.role = host`, `coop.secret = <hex>`) ทำให้ TOML ทั้งไฟล์เสีย
   → MelonLoader ใช้ค่า default → `Enabled=false` → mod นิ่ง ไม่มี IPC pipe (เกิดจริงตอนเปิด 23:54)
2. **ตอนออกเกม MelonLoader เขียน cfg ใหม่** แล้วแปลง dotted key เป็นตารางซ้อน
   `[LongYinCoop.coop]` + `net = { enabled = false }` และทิ้ง `coop.role`/`coop.secret`
   → reader ที่อ่านเฉพาะ section `[LongYinCoop]` แบบ literal อ่านไม่เจอเงียบ ๆ

## 2. รูปแบบไฟล์ใหม่ (flat, ไม่ใช่ TOML)

`UserData\LongYinCoop\net.cfg`

```ini
# LongYinCoop S2 — อ่านโดย mod เอง, MelonLoader ไม่แตะไฟล์นี้
enabled          = false      # true = ให้ S2 ทำงาน (ต้อง role=host และ write path ปิด)
role             = host       # host | guest
secret           = <hex ≥32>  # ห้าม log
allowNonLoopback = false
```

- `#` / `;` = comment · key ไม่สนตัวพิมพ์เล็ก/ใหญ่ · ค่าที่ครอบ `"` หรือ `'` ถอด quote ให้
- key ที่ไม่รู้จักถูกข้าม (เผื่ออนาคต) · key ซ้ำ = ตัวหลังชนะ
- **Fail-closed:** ไม่มีไฟล์ / อ่านไม่ได้ / มี `[section]` / บรรทัดที่ไม่ใช่ `key = value`
  ⇒ `FormatOk=false` ⇒ `IsValid=false` ⇒ S2 ไม่ทำงาน (ไม่ใช่ "disabled แต่ valid" เงียบ ๆ)

เหตุที่ต้องปฏิเสธ `[section]`: กันการก๊อปค่าจาก `MelonPreferences.cfg` (หรือค่าที่ ML เขียนเอง
`[LongYinCoop.coop] net = { enabled = false }`) มาใส่แล้วถูกอ่านเป็น "ปิดอยู่" ทั้งที่ผู้ใช้ตั้งใจเปิด

## 3. ไฟล์ที่แก้

| ไฟล์ | เปลี่ยน |
|---|---|
| `src/LongYinCoop/S2/S2NetConfig.cs` | เขียนใหม่: parser flat key=value (ไม่มี section), `SectionFound` → `FormatOk`, fail-closed ตาม §2, key constants = `enabled`/`role`/`secret`/`allowNonLoopback`, `FileName = "net.cfg"` |
| `src/LongYinCoop/CoopPaths.cs` | `MelonPreferencesPath` → `NetCfgPath` = `UserData\LongYinCoop\net.cfg` |
| `src/LongYinCoop/CoopMod.cs` | ส่ง `CoopPaths.NetCfgPath` ให้ `S2HostService` + คอมเมนต์ |
| `src/LongYinCoop/S2/S2HostService.cs` | ข้อความ log/idle ชี้ `net.cfg` (`enabled`, `role`), `DescribeIdle` ใช้ `FormatOk` |
| `tests/S2Protocol.Tests/S2Protocol.Tests.csproj` | link `CoopPaths.cs` เข้า test assembly (pure: System + IO) |
| `tests/S2Protocol.Tests/S2a2Tests.cs` | เขียน `NetConfigTests()` ใหม่ 26 checks (flat format + regression ของ incident) |

**ไม่แตะ**: `tools/*` (preflight ยังตรวจ 6 guard pref ใน MelonPreferences ตามเดิม — pref พวกนั้นเป็น entry จริงของ ML),
`Mods\LongYinCoop.dll` (ไม่ build/deploy), เซฟ, `UserData\MelonPreferences.cfg` ของเกม

## 4. ผลทดสอบ (`netcfg-tests.txt`)

```
dotnet run --project tests\S2Protocol.Tests -c Debug
RESULT PASS (all T0/T1 checks)   exit=0
checks 242 (เดิม 232) · FAIL 0
```

- write guard 64 ชุดค่าผสม: **`all 64 pref combinations follow the truth table` PASS**
  + `exactly 1 combination passes (all six false)` + `no DevCommands parameter` (ครบ 14 checks ของ guard)
- `net.cfg` 26 checks ผ่านทั้งหมด รวม regression ที่สำคัญ:
  - `[section] is rejected (flat file only)`
  - `MelonLoader-shaped pref content is rejected, never silently valid`
  - `net.cfg lives under UserData\LongYinCoop (MelonLoader never touches it)`
  - poll/ForceRead/toggle กลับ-ไป-มา ได้ (กลไก A/B ยังใช้ได้)

## 5. สถานะหลังงาน A (ยังไม่ปิด stage)

- ยังไม่มีไฟล์ `net.cfg` ในเครื่องเกม ⇒ mod ที่ deploy อยู่ (build เก่า) และ build ใหม่จะ **idle** เหมือนเดิม
- `Save` = 57 ไฟล์ digest `854cc890430f` **EXACT** ทั้ง `savehash.copy.before.txt` และ `savehash.real.before.txt` · `leftovers: none`
- `UserData\MelonPreferences.cfg` sha256 `269b571c…098d1` ไม่เปลี่ยน (mtime 2026-10-21 22:58:31)
- ยังไม่ได้ build mod DLL (ตามข้อจำกัด) ⇒ `S2HostService`/`CoopMod` ยังไม่ผ่าน compiler ในรอบนี้;
  `S2NetConfig`/`CoopPaths` ผ่าน compiler ผ่าน test assembly แล้ว

## 6. งานถัดไปที่เสนอ

1. **งาน B** — exit 9 rollback test (เปิดเกม 1 ครั้ง, `.dll.off`) — ต้องอนุมัติ + ผู้ใช้เปิด/ปิดเกม
2. ตอนจะใช้ S2 จริง (S2a-3) ต้อง **build DLL ใหม่จาก source นี้** (ขออนุมัติแยก) แล้วเขียน `net.cfg`
   ผ่านสคริปต์/มือ — ไม่ต้องแตะ MelonPreferences อีก
3. (เสนอ, ยังไม่ทำ) `save-split.py preflight` ควรตรวจ `net.cfg` ว่า `enabled=false` ก่อนอนุญาตเปิดเกม
   — ยังไม่มีช่องโหว่ตอนนี้เพราะยังไม่มีไฟล์ แต่จะเป็นประโยชน์ก่อน S2a-3
