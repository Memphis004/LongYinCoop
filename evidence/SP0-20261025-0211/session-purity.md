# SP0 DUMP-only — session purity log (exit 8) · label `SP0-20261025-0211`

## Pre-round (2026-10-25 ~02:11 ก่อนเริ่ม census)

| ตรวจ | ค่า | ผล |
|---|---|---|
| กระบวนการเกม | `tasklist` ไม่พบ `LongYinLiZhiZhuan.exe` | ✓ |
| `GameAssembly.dll` sha256 | `33c56def4102844a1190eec963a05c834cfcf992d42b423d485ce8584d7920c1` | ✓ ตรง `VERSION.md` |
| `global-metadata.dat` sha256 | `451311d4dc8c1b8b5073905c8b39a9781bd7e1f44ed88fbf7b35f5345c18bfd0` | ✓ ตรง `VERSION.md` |
| Save (`save-split status`) | 57 ไฟล์ digest `7332fc93eb98` · EXACT ทั้ง copy/real baseline ของ `sp0sp7-20261024-225939` · `leftovers: none` | ✓ |
| `UserData/LongYinCoop/net.cfg` | `12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8` | ✓ ตรงสถานะอนุมัติ |
| `UserData/MelonPreferences.cfg` | `269b571c1b1572b7b525cda3e8a27e09b732b8eb31bdfe290269c8ed62a098d1` | ✓ ตรงสถานะอนุมัติ |
| MelonLoader log ล่าสุด (ก่อนรอบ) | `26-10-25_0-23-2.log` (เกิดก่อนรอบ — incident 00:23 ของรอบสำรวจก่อน) | ✓ บันทึกเป็นจุดอ้างอิง |

## Post-round (2026-10-25 ~02:3x หลังเสร็จ census + รายงาน)

| ตรวจ | ค่า | ผล |
|---|---|---|
| กระบวนการเกม | ไม่พบ (game: not running) | ✓ ตลอดรอบ |
| `GameAssembly.dll` / `global-metadata.dat` | `33c56def4102844a…` / `451311d4dc8c1b8b…` | ✓ ไม่เปลี่ยน |
| **MelonLoader log ใหม่** | **ไม่มี** — ล่าสุดยังเป็น `26-10-25_0-23-2.log` | ✓ เกมไม่ถูกเปิดตลอดรอบ |
| Save | 57 ไฟล์ `7332fc93eb98` EXACT · `leftovers: none` | ✓ ไม่ถูกเขียน |
| `net.cfg` / `MelonPreferences.cfg` | `12541f34c63389ce…` / `269b571c1b1572b7…` | ✓ ไม่เปลี่ยน |
| build/deploy DLL | ไม่มีการเรียก `dotnet build` · `Mods\` ไม่ถูกแตะ | ✓ |

หมายเหตุ: งานทั้งหมดเป็นการอ่านไฟล์บนดิสก์ (`dump.cs`) และเขียนเฉพาะใน mod repo (`tools/sp0_dump_parser.py`, `evidence/SP0-20261025-0211/*`) — ไม่มี artifact ใดในโฟลเดอร์เกม
