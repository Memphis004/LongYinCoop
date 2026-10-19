# S0 — ผลตรวจเทียบ exit criteria

Run: 2026-10-20 · repo commit `60a9051` · mod `LongYinCoop 0.1.0` stage 0
เกม: Unity 2020.3.48f1c1 · MelonLoader 0.7.3 · net6.0
Build hash: `GameAssembly.dll` `33c56def…d7920c1` · metadata `451311d4…c18bfd0`

## ผลรายข้อ

| # | เกณฑ์ | ผล | หลักฐาน |
|---|---|---|---|
| 1 | `S0 loaded` + 0 exception 10 นาที | **ผ่านบางส่วน** — 0 exception/ERROR ตลอด 4 runs (~15 นาทีสะสม) แต่ยังไม่มี run เดียวที่ต่อเนื่อง 10 นาที | `coop-run.log` (4 × `S0 loaded`, 0 ERROR) |
| 2 | dispatcher: 1,000 no-op, 0 dropped, p99 ≤ 4 ms | **PASS** | `bench_dispatch 1000` → queued 1000 / ran 1000 / dropped 0 / elapsed 4 ms / p99 0.0008 ms / max 0.12 ms |
| 3 | IPC แยกสองชั้น | **FAIL บน world map** | ดูตารางล่าง |
| 4 | savehash ก่อน/หลังเท่ากัน | **FAIL** — `PlayerprefData.dat` เปลี่ยน (ไฟล์ pref ของเกม) ; `SaveSlot*` ไม่เปลี่ยนเลย | `savehash.real.before.txt` vs `savehash.real.after.txt` |
| 5 | ไม่เขียน state เกม | **PASS** (code review: มีแต่ getter/อ่าน; ไม่มี Save/Load, ไม่มี setter) | `src/LongYinCoop/*.cs` |
| 6 | probe hit > 0, thread ตรง, unpatch = 0 | **PASS** | hits 10,668 · `onUpdateThread=1 probeThread=1` · `s0-result.json` `threadMatch=true` · `probe: unpatched (owned patches before=1 after=0)` |
| 7 | UIStrings ครอบทุกข้อความ | **ผ่านบางส่วน** — ข้อความหลัก/Log หลักอยู่ใน `UIStrings.cs` แต่ข้อความวินิจฉัยยังมี literal | `UIStrings.cs`, grep |
| 8 | ถอด mod (`.off`) แล้วเกมบูตปกติ | **ยังไม่ทดสอบ** | — |
| 9 | world map ปกติ + ศึกจบ 1 ศึก | world map **ผ่าน** (ผู้ใช้โหลดเซฟ/เดินได้) · ศึก **ยังไม่ทดสอบ** | scene `MainGame`, `get_player_pos` = (-3807, 84) |
| 10 | LongYinThai ยังแปลได้ | **ยังไม่ทดสอบด้วยตา** | — |
| 11 | rename `.dll.off` แล้วเกมปกติ | **ยังไม่ทดสอบ** | — |
| 12 | thread ตรง หรือมีทางแก้ที่ทดสอบแล้ว | **PASS** — ตรงกัน ไม่ต้องใช้ทางแก้ | `s0-result.json` |

## ข้อ 3 — ตัวเลขจริง (n = 1,000 ต่อช่อง)

| ฉาก | คำสั่ง | reuse p50 / p95 / p99 | per_call p50 / p95 / p99 |
|---|---|---|---|
| TitleScene | `ping` | 0.033 / 0.041 / 0.058 ms | 0.768 / 3.169 / **4.247** ms |
| TitleScene | `get_scene` | 6.92 / 10.51 / 16.04 ms | 6.94 / 11.85 / **17.66** ms |
| MainGame (world map) | `ping` | 0.034 / 0.045 / 0.126 ms | 0.647 / 3.407 / **8.620** ms |
| MainGame | `get_scene` | 6.91 / 19.16 / **30.74** ms | 6.90 / 20.62 / **33.63** ms |
| MainGame | `get_player_pos` | 6.92 / 19.78 / **32.04** ms | 6.91 / 21.01 / **35.62** ms |

- เกณฑ์ (ก) ping/stats p99 ≤ 5 ms: ผ่านที่ TitleScene (4.25), **ไม่ผ่านที่ MainGame (8.62)**
- เกณฑ์ (ข) IL2CPP p99 ≤ 25 ms: ผ่านที่ TitleScene (17.66), **ไม่ผ่านที่ MainGame (33.63)**
- dispatcher drain ระหว่างวัด: p99 0.024 ms, max 0.0655 ms, errors 0, pending 0
- p50 ≈ 6.9 ms ทุกคำสั่ง = ครึ่งเฟรม → p99 ~1–4 เฟรม ขึ้นกับภาระฉาก; เกณฑ์แบบ ms คงที่จึงไม่สะท้อนข้อจำกัดจริง (ต้องรอ main-thread frame ≥ 1 เฟรม)

## Save hash (ข้อ 4)

```
< a2e45f8c… *LongYinLiZhiZhuan_Data/Save/PlayerprefData.dat
> 54ee5ee5… *LongYinLiZhiZhuan_Data/Save/PlayerprefData.dat
```
เปลี่ยนไฟล์เดียว และเปลี่ยนทุกครั้งที่เปิด/ปิดเกม แม้ไม่โหลดเซฟ; ไฟล์ `SaveSlot0..10` ไม่เปลี่ยนแม้แต่ไฟล์เดียว

## สิ่งที่พบระหว่างทำ (แก้แล้ว)

1. Il2CppInterop 1.5.1 สร้าง type เกมใน namespace **`Il2Cpp`** (`Il2Cpp.GameController`) ไม่ใช่ global
2. `MelonLoader.dll` มี namespace ชื่อ `Harmony` → ต้องเรียก `HarmonyLib.Harmony.GetAllPatchedMethods()`
3. named pipe แบบ instance เดียวทำให้ client เจอ BUSY ระหว่างสลับ client → เปลี่ยนเป็น double-buffer (สร้าง instance ถัดไปก่อน handle)
4. probe ถอดได้จริง: `before=1 after=0`; run ที่ `Probe=false` ไม่มี patch ของเราใน inventory (0 ครั้ง)

## ยังค้าง / ต้องตัดสิน

1. **เกณฑ์ 3** — จะคง 5/25 ms (แล้วถือว่าไม่ผ่าน) หรือเปลี่ยนเป็น "≤ 2 frame times ของฉากนั้น" (พร้อมรายงาน frame time)
2. **`PlayerprefData.dat`** — ถือเป็น volatile ที่ยกเว้นจากเกณฑ์ 4 หรือต้องห้ามเขียนแม้แต่ไฟล์ pref
3. เกณฑ์ 8/11 (`.off`) และ 10 (LongYinThai) ต้องทดสอบเพิ่ม; 9 ต้องเล่นศึก 1 ครั้ง
