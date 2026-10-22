# build-report — S2a-3 · C3 (Gate #1 อนุมัติแล้ว)

## 1. สภาพก่อน build

| ข้อ | ค่า |
|---|---|
| repo | `C:\Users\memph\LongYinCoop` |
| HEAD | `a2eae06` (evidence commit ต่อจาก `a0ba387`) |
| tracked tree dirty | **0** (`git status --porcelain` ไม่มี M/A) |
| project | `src\LongYinCoop\LongYinCoop.csproj` (net6.0, ไฟล์เดียวใน `src/`) |
| dotnet | 9.0.311 |

## 2. คำสั่ง + ผล

```
dotnet build src\LongYinCoop\LongYinCoop.csproj -c Release --no-incremental
```

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.19
```

- **Error = 0 · Warning = 0** (ทั้ง incremental build แรก และ `--no-incremental` — log เต็มใน `build.txt`)
- **ไม่มี compile error ⇒ ไม่มีการแก้โค้ดใด ๆ ในรอบนี้** (ไม่มี diff เพิ่มจาก `a0ba387`)
- ไม่มี `warning`/`error` token อื่นใน log นอกจากสองบรรทัดสรุปข้างบน

## 3. DLL ที่ได้

| ข้อ | ค่า |
|---|---|
| path | `src\LongYinCoop\bin\Release\LongYinCoop.dll` |
| bytes | 313,344 |
| **SHA-256** | `c51debef8c08f8a29524b51f20785f9f71ab9ed29d2eee889977c718af94ae96` |
| PDB SHA-256 | `6ec6482c9649b61e8fb21e0e682710622a9b3a364b5261363244163d916ec110` |
| lastWrite | 2026-10-22T14:11:27+07:00 |

ยืนยันว่า build ใช้ซอร์ส C1 จริง (ไม่ใช่ cache เก่า):
- type names ใน `#Strings` heap: `TcpTransport`, `TcpFrameStream`, `S2Handshake`, `S2ControlHandler`, `S2FrameMeter`, `SeqTracker`, `BindPolicy` = **พบทั้งหมด**
- string literals (UTF-16 `#US`): `S2a-3` ✅ · `0.5.0` ✅ · `selftest.tcp` ✅ · `framemeter.window` ✅ · `forbidden` ✅ · `S2a-2` ❌ (ถูกแทนที่ตามคาด)

## 4. Unit tests หลัง build

`dotnet run --project tests\S2Protocol.Tests -c Release` → **421 PASS / 0 FAIL** (S2a-3 suite 179 PASS) · log: `tests-postbuild.txt`

เหตุผลที่รันซ้ำ: ยืนยันว่า tree ที่ build ยังผ่าน suite เดิม (ซอร์สไม่เปลี่ยนจากรอบ C1 — git ยืนยัน 0 ไฟล์ถูกแก้)

## 5. ยืนยันว่า **ไม่มีการ deploy**

| ไฟล์ | SHA-256 | lastWrite |
|---|---|---|
| `Mods\LongYinCoop.dll` | `cb35a93f617b16b3cdf069a9c7e76d166dde757b8692b943c8b42b92026ceee0` | 2026-10-21T22:55:43 (**เท่าเดิม = build S2a-2**) |
| `Mods\LongYinCoop.dll.off` | `517758147f59170e584075a4b42f61e00d293803febe36f8a040bcdad4592612` | ไม่เปลี่ยน |

- ไม่มี `Copy-Item`/`xcopy`/`robocopy` ไปที่ `Mods\` ในรอบนี้
- ไม่มี `.pdb` ใหม่ใน `Mods\`
- ไม่ได้เปิดเกม (ตรวจ process = ไม่รัน)
- build output อยู่แค่ `src\LongYinCoop\bin\Release\` (gitignored: `!! src/LongYinCoop/bin/`, `!! src/LongYinCoop/obj/`)

## 6. สรุป verdict C3

**PASS** — build สะอาด 0 error / 0 warning, ได้ DLL sha256 `c51debef…ae96`, ไม่มีการ deploy, ไม่มีการแตะเซฟ/เกม
ขั้นถัดไป = **Gate #2** (C4–C6): สำรอง DLL ปัจจุบัน, เตรียม `net.cfg`, split เซฟ, recorder, preflight — ยังไม่ทำจนกว่าจะอนุมัติ

--- Mods\ listing check (deploy must not have happened) ---
Mods\LongYinCoop.pdb sha256: 4fc142bdaef7acf52c2889292024d9b0b86e065f9ac73fb324d52f751e47240e
Mods\LongYinCoop.pdb lastWrite: 2026-10-21T22:55:43.2152358+07:00
Mods\LongYinCoop.dll lastWrite: 2026-10-21T22:55:43.2202817+07:00
game process running at report time: False
