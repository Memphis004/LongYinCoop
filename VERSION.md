# VERSION — LongYinLiZhiZhuan build anchors

ตรวจเมื่อ: 2026-10-20 (S0)
เครื่อง: Windows / `C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan`

| ไฟล์ | SHA-256 | ขนาด (bytes) |
|---|---|---|
| `GameAssembly.dll` | `33c56def4102844a1190eec963a05c834cfcf992d42b423d485ce8584d7920c1` | 33,669,120 |
| `LongYinLiZhiZhuan_Data/il2cpp_data/Metadata/global-metadata.dat` | `451311d4dc8c1b8b5073905c8b39a9781bd7e1f44ed88fbf7b35f5345c18bfd0` | 7,960,432 |

สภาพแวดล้อม: Unity **2020.3.48f1c1** · MelonLoader **0.7.3** · runtime **net6.0** · Il2CppInterop 1.5.1-ci.845

## กฎ

1. เทียบ SHA-256 ทั้งสองไฟล์ทุกครั้งที่เริ่ม session — **ไม่ตรง = หยุดและรายงาน** (เกมอัปเดต = anchor ใน notes ทั้งหมดเชื่อไม่ได้)
2. ห้าม commit `GameAssembly.dll`, `global-metadata.dat`, `MelonLoader/Il2CppAssemblies/`, `dump.cs` (ดู `.gitignore`)
3. ค่าใน `STAGEPLAN.md §13` (RVA/offset) ใช้สำรวจเท่านั้น โค้ดต้อง re-resolve ผ่าน interop
