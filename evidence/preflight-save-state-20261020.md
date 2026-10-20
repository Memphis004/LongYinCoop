# Pre-flight 2.5 — สถานะโฟลเดอร์เซฟก่อนเริ่ม S1.5 (วัด 2026-10-20 14:04)

คำสั่งตรวจ (อ่านอย่างเดียว, เกมปิดอยู่ — `tasklist` ไม่พบ `LongYinLiZhiZhuan.exe`):

```bash
ls -d LongYinLiZhiZhuan_Data/Save*
find Save        -type f -print0 | xargs -0 sha256sum
find Save.real-S1 -type f -print0 | xargs -0 sha256sum
# เทียบกับ baseline ของ S1 (normalize path: ตัด prefix Save/ · save/ · path เต็ม)
coop-test/S1-20261020-113926/savehash.real.before.txt
coop-test/S1-20261020-113926/savehash.copy.before.txt
```

## ผล [RUNTIME]

| โฟลเดอร์ | ไฟล์ | เทียบ `savehash.real.before.txt` (ของจริง S1) | เทียบ `savehash.copy.before.txt` (สำเนา S1) | mtime `SaveSlot0/Info` |
|---|---|---|---|---|
| `LongYinLiZhiZhuan_Data/Save` | 57 | **ไม่ตรง** — 1 ไฟล์ต่าง (`PlayerprefData.dat` `8038352c…` → `f64597c6…`), `SaveSlot*` 56/56 ตรง | **ตรง 56/57** (ต่างเฉพาะ `PlayerprefData.dat`) | 2026-10-20 11:39:34 (ตอนสลับเข้า) |
| `LongYinLiZhiZhuan_Data/Save.real-S1` | 57 | **ตรง 57/57** (changed 0 · missing 0 · added 0) | — | 2026-04-04 22:18:55 (เดิมของเกม) |

## ข้อสรุป

1. **`Save` ที่ mount อยู่ = สำเนา S1 ไม่ใช่เซฟจริง** — ยืนยันด้วย hash ที่ตรงกับ `savehash.copy.before.txt` 56/57 (ไฟล์ที่ 57 คือ `PlayerprefData.dat` ที่เกมเขียนเองระหว่างเล่นบนสำเนา ตาม D10)
2. **`Save.real-S1` ยังค้างอยู่** และเป็นของจริงที่ **ไม่ถูกแตะเลย** (57/57 ตรง baseline, mtime ยังเป็น เม.ย.)
3. S1 ปิด stage โดยยังไม่คืนโฟลเดอร์ — เป็นสภาพที่รับรู้ได้จาก S1 report §2 แต่ไม่เคย restore

## ผลต่อ S1.5

ตามคำสั่ง pre-flight 2.5: **มี `Save.real-*` ค้าง → ห้ามรัน `save-split split`** จนกว่า `Save` จะเป็นของจริงและ hash ตรง `savehash.real.before.txt` ของ S1 ครบ 57/57

ทางที่จะทำให้ผ่าน (ยังไม่ทำ — รออนุมัติ):
- `restore`: rename `Save` → `coop-test/S1-20261020-113926/save.used-pre-S1.5/` แล้ว rename `Save.real-S1` → `Save` → rehash ต้องได้ 57/57 ตรง baseline
- ตรวจซ้ำด้วยชุดคำสั่งเดิมในไฟล์นี้ แล้วแนบผลลง `S1.5-report.md`

**ห้าม:** `copy` ทับของจริง, `rm -rf` โฟลเดอร์ใดใน `LongYinLiZhiZhuan_Data/`, หรือรัน `split` ก่อนข้อ 3 ผ่าน
