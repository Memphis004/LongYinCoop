# S1.5e — บันทึกการปิดเกม + restore เซฟจริง (2026-10-21)

## 1. ปิดเกมโดยไม่กด Save (RUNTIME)

| รายการ | ค่า |
|---|---|
| วิธีปิด | `Stop-Process -Id 40516 -Force` (ไม่มี in-game save, ไม่มีการกด quit ในเกม) |
| pid / start | 40516 / 2026-10-21 01:29:42 |
| เวลาสั่งปิด | 2026-10-21 02:36:24 |
| ตรวจ process หลังปิด | `process_count_after=0` (ไม่เหลือ `LongYinLiZhiZhuan*`) |
| เวลาตรวจ | 2026-10-21 02:36:27 |
| การเขียนเซฟตอนปิด | **ไม่มี** — ไฟล์ใหม่สุดใน `Save` (สำเนา) ยังเป็น `01:29:53` (`PlayerprefData.dat`) เท่าเดิมก่อนสั่งปิด |
| บันทึก | `logs/close-game.log` |

## 2. Restore (RUNTIME)

```
python tools/save-split.py sentinel unhide --label S1.5e-20261021-0101 --slot 8
  moved aside recreated SaveSlot8 (1 identical files) -> recreated-SaveSlot8-20261021-023720
  OK unhidden .hidden-SaveSlot8 -> SaveSlot8 (5 files) hash_match=True
python tools/save-split.py restore --label S1.5e-20261021-0101
  copy check: changed=1 (pref=1) missing=0 added=0     <- เฉพาะ PlayerprefData.dat
  rename  : Save -> Save.used-20261021-023720
  rename  : Save.real-S1.5e-20261021-0101 -> Save
  restored Save vs real baseline: changed=0 missing=0 added=0
  OK restore S1.5e-20261021-0101: 57 files
```

- เซฟจริงถูก **rename กลับ** ไม่ได้เขียนทับ (ไบต์เดิมทุกไฟล์)
- สำเนาที่ใช้ทดสอบเก็บไว้ที่ `coop-test/S1.5e-20261021-0101/Save.used-20261021-023720`
- บันทึก: `logs/restore.log`

## 3. Save verification หลัง restore (RUNTIME)

| ตัวชี้วัด | ผล |
|---|---|
| `save-split.py status` | `game: not running` · `Save files=57 digest=2b0548fe464b` · `leftovers: none` |
| เทียบ baseline ที่บันทึกไว้ตอน split | **EXACT** ทั้ง 6 baseline (`S1.5c-originhp`, `S1.5c-w2w4`, `S1.5e`) |
| rehash อิสระด้วย PowerShell (57 ไฟล์, sha256 ต่อไฟล์) | `normalized_compare_diff_lines=0` → **57/57 ตรง** |
| leftover ใน `Save` (`.hidden*`, `recreated-*`) | ไม่มี |
| โฟลเดอร์ใน `LongYinLiZhiZhuan_Data` | `il2cpp_data`, `Plugins`, `Resources`, `Save`, `StreamingAssets` (เหลือ `Save` เดียว) |
| บันทึก | `logs/verify-postclose.log`, `logs/save-real-hash-postrestore.txt` |

**สรุป: digest กลับมาตรง baseline `2b0548fe464b` = 57/57 ไฟล์ → PASS**
