# S0 — ผลตรวจเทียบ exit criteria (v2: เกณฑ์ IPC อิงเฟรม + PlayerprefData volatile)

Run: 2026-10-20 · repo commit `3648c5d` + frame-time instrumentation · mod `LongYinCoop 0.1.0` stage 0
เกม: Unity 2020.3.48f1c1 · MelonLoader 0.7.3 · net6.0
Build: `GameAssembly.dll` `33c56def…d7920c1` · metadata `451311d4…c18bfd0`

## ผลรายข้อ

| # | เกณฑ์ | ผล | หลักฐาน |
|---|---|---|---|
| 1 | `S0 loaded` + 0 exception 10 นาที | **PASS** — run6 ต่อเนื่อง **11 นาที 25 วิ** (05:24:04→05:35:29, pid 45208) `[ERROR]` = **0**, probeHits 70,931, queued/ran 7/7 | `coop-run-run6.log` |
| 2 | dispatcher: `bench_dispatch 1000`, 0 dropped, p99 ≤ 4 ms | **PASS** — 1000/1000, dropped 0, elapsed 4 ms, p99 0.0008 ms, max 0.12 ms | `bench_dispatch` |
| 3 | IPC อิงเฟรม (D9) | **PASS** — ดูตารางล่าง | `bench` + `stats` |
| 4 | `snapshot world` ตรงกับ Frida (3 ครั้ง) | **PASS** — 3/3 ครั้ง ตรงกันทุก field (x, y, heroID) ผ่าน object path เดียวกัน วัดที่ MainGame (ดูข้อจำกัดในไฟล์หลักฐาน) | `frida-crosscheck.md` |
| 5 | savehash ไม่เปลี่ยน (ยกเว้น `PlayerprefData.dat`, D10) | **PASS** — `SaveSlot0–10` ไม่เปลี่ยนเลย (ยืนยันซ้ำกับ run6 11 นาที: `diff` ต่างเฉพาะ `PlayerprefData.dat`); `PlayerprefData.dat` เปลี่ยนทั้ง 2 control runs แม้ `Enabled=false` | `savehash.real.before/after10min.txt`, `savehash.control2.*.txt` |
| 6 | probe hit > 0, thread ตรง, unpatch = 0 | **PASS** — hits 70,931 (run6) · thread 1 = 1 · `probe: unpatched (owned patches before=1 after=0)` ตอนปิดเกมปกติ 05:35:29 · `Probe=false` → ไม่มี patch ของเรา | `s0-result.json`, `coop-run-run6.log` |
| 7 | UIStrings ครอบทุกข้อความ | **PASS** — ย้ายข้อความ log/วินิจฉัย/compat-notes เข้า `UIStrings.cs` เป็นเทมเพลตหมดแล้ว; grep เหลือแค่ path/JSON key/format; build 0 warn 0 err; log + `compat-notes.md` หลัง refactor **เหมือนเดิมทุกบรรทัด** | `uistrings-grep.txt`, `boot-after-refactor.log` |
| 8 | ถอด mod (`.off`) แล้วเกมบูตปกติ | **PASS** — บูต 05:36:22 ด้วย `LongYinCoop.dll.off` · mod ที่โหลดมีแค่ `LongYinThai` + `UnityExplorer` (+`UniverseLib`) ไม่มีบรรทัดของ LongYinCoop · 12 บรรทัด `Error:` ใน log เป็น type-cache ของ MelonLoader/UIElements (มาจาก UnityExplorer ไม่ใช่ mod เรา — รอบนี้ mod ไม่ได้โหลดเลย) | `boot-modoff.log` |
| 9 | world map ปกติ + ศึกจบ 1 ศึก | **PASS** — world map: `get_player_pos` เปลี่ยน (-3807, 84) → (-4001.15, 1015.25) · ศึก: ผู้ใช้ยืนยันว่าสู้หลายศึกต่อเนื่องแล้ววิ่งวนต่อจนครบ 11 นาที โดยไม่มี error (log ไม่มี marker ของศึกเอง) | `coop-run-run6.log` |
| 10 | LongYinThai ยังแปลได้ | **PASS (ด้วยตา)** — ภาพหน้าจอผู้ใช้: คำแปลไทยขึ้นครบทั้งชื่อสิ่งก่อสร้าง/ห้องโถง/หอคัมภีร์ ขณะ mod เราโหลดอยู่ | ภาพหน้าจอผู้ใช้ 2026-10-20 |
| 11 | rename `.dll.off` แล้วเกมกลับปกติ | **PASS** — คืนชื่อ `LongYinCoop.dll`/`.pdb` แล้ว (ขนาด/เวลาเดิม 05:09) เกมบูตปกติทั้งสองกรณี | `boot-modoff.log` |
| 12 | thread ตรง หรือมีทางแก้ที่ทดสอบแล้ว | **PASS** — ตรง ไม่ต้องใช้ทางแก้ | `s0-result.json` |

## Frame time ที่วัด (D9)

| ฉาก | p50 | p95 | p99 | วิธีวัด |
|---|---|---|---|---|
| TitleScene | 6.906 | 11.185 | 24.429 ms | ring 1024 เฟรม |
| MainGame | 6.906 | 14.680 | 27.679 ms | median ของ 20 หน้าต่าง (40 วิ) |

p50 = 6.906 ms ทั้งสองฉาก → เกมค้างที่ ~145 fps (VSync 144 Hz) ความต่างอยู่ที่ p95/p99 (hitch)

## IPC (n = 1,000 ต่อช่อง) เทียบเกณฑ์อิงเฟรม

| ฉาก | คำสั่ง | reuse p99 | per_call p99 | เกณฑ์ | ผล |
|---|---|---|---|---|---|
| TitleScene | `ping` | 0.058 | 4.247 | ≤ 1× p95 = 11.185 | PASS |
| TitleScene | `get_scene` | 16.042 | 17.658 | ≤ p99+10 = 34.429 | PASS |
| MainGame | `ping` | 0.126 | 8.620 | ≤ 1× p95 = 14.680 | PASS |
| MainGame | `get_scene` | 30.741 | 33.625 | ≤ p99+10 = 37.679 | PASS |
| MainGame | `get_player_pos` | 32.038 | 35.621 | ≤ 37.679 | PASS |

dispatcher ระหว่างวัด: errors 0, pending 0, drain p99 0.024 ms

## Control test — `PlayerprefData.dat` (D10)

```
run 1  before: 0c8aef57a09e65d7598ef48d11c7c58e010f5fc6cf3e4780443a9d307ef5258e
run 1  after : 1b8ad58a8df5576884acf83ed8ff324a0f6d8823647e1e9515f16ab2199411f2
run 2  before: 1b8ad58a8df5576884acf83ed8ff324a0f6d8823647e1e9515f16ab2199411f2
run 2  after : 0ebc1769f3bd4c3745320087506d14f4f58e42ede7190f4f7228661454a4bb13
```
รันด้วย `Enabled=false` → log `Disabled by pref … mod does nothing`, probe ไม่ติด, IPC ไม่เปิด
**ยังเปลี่ยนไฟล์เดิมไฟล์เดียว** และ `SaveSlot*` ไม่เปลี่ยน → ยืนยันว่าเกมเขียนเอง ไม่ใช่ mod
→ ยกเว้นจากเกณฑ์ 5 (บันทึกใน STAGEPLAN D10 + §1.2)

## สิ่งที่พบ/แก้ระหว่างทำ

> หมายเหตุ: หลังปิดข้อ 7 มี rebuild 1 ครั้ง (`UIStrings` → เทมเพลต) แล้ว boot ซ้ำ 05:49:03–05:49:57
> → log ทุกบรรทัดเหมือนเดิม, IPC ทั้ง 6 คำสั่งทำงาน, `bench_dispatch` 1000/1000 dropped 0,
> `compat-notes.md` ต่างกันเฉพาะบรรทัด timestamp, ปิดเกมแล้ว `unpatched before=1 after=0`, `[ERROR]` = 0

1. Il2CppInterop 1.5.1 วาง type เกมใน namespace **`Il2Cpp`** (`Il2Cpp.GameController`)
2. `MelonLoader.dll` มี namespace `Harmony` → ต้องเรียก `HarmonyLib.Harmony.GetAllPatchedMethods()`
3. named pipe instance เดียว → client เจอ BUSY ระหว่างสลับ client → เปลี่ยนเป็น double-buffer
4. เพิ่มการวัด frame time (p50/p95/p99) ใน `stats` เพื่อใช้เกณฑ์อิงเฟรมได้ทุกฉาก
5. `PlayerprefData.dat` เป็น volatile (control test ข้างบน)

## ค้างก่อน tag `stage-S0`

- ~~รอบ 10 นาทีต่อเนื่อง (ข้อ 1)~~ → ปิดแล้วด้วย run6
- ~~`.off` + LongYinThai (ข้อ 8/10/11)~~ → ปิดแล้ว
- ~~ศึก 1 ครั้ง (ข้อ 9)~~ → ปิดแล้วด้วยคำยืนยันของผู้ใช้
- ~~`snapshot world` เทียบ Frida (ข้อ 4)~~ → ปิดแล้ว (`frida-crosscheck.md`)
- ~~UIStrings เก็บ literal ที่เหลือ (ข้อ 7)~~ → ปิดแล้ว (`uistrings-grep.txt`)

**ไม่มีอะไรค้าง — พร้อม tag `stage-S0`**
