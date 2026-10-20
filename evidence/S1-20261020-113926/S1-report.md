# S1 — รายงานปิด stage (วัด 2026-10-20)

**build ที่ใช้วัด:** mod 0.2.0 · build `6f9e82c1` · pid 8492 · capture `UserData/CoopCapture/20261020-115237` (11:52:37 → 12:34:54)
**เกม:** SHA-256 `GameAssembly.dll` = `33c56def…920c1`, `global-metadata.dat` = `451311d4…8bfd0` → **ตรง `VERSION.md`**
**ยังไม่ใช้วัดบน build สุดท้าย:** ทุกตัวเลขในรายงานนี้มาจาก build `6f9e82c1` (`LongYinCoop.dll` sha256 `6f9e82c1…ba8a`)
build หลังแก้ world stream (`8ebf86f4…47de`) **สลับเข้า `Mods/` แล้วเมื่อ 12:44** (เก่าถูกเก็บเป็น `LongYinCoop.dll.prev-6f9e82c1`) แต่ **ยังไม่ได้รันแม้แต่ครั้งเดียว**

**ความสมบูรณ์ของไฟล์ capture หลังปิดเกม (12:44):** parse ได้ 100% — world 27,643 · battle 34,723 · log 2,163 · marker 42 · **bad = 0 ทุกไฟล์** (ก่อนหน้ามี 1 บรรทัดค้างเพราะอ่านตอน writer ยังเขียน)

สถานะที่ใช้: **PASS** / **PASS-with-limitation** / **FAIL** / **NOT-RUN**

## 1. ตารางเกณฑ์ 1–9

| # | เกณฑ์ (STAGEPLAN §3) | สถานะ | หลักฐาน / เหตุผล |
|---|---|---|---|
| 1 | world map 10 นาที: 0 exception | **PASS** | 6f9e82c1: 11:52:37–12:04:12 (11.6 นาที) `err=0`; **8ebf86f4: 12:48:08–13:01 (13 นาที) `err=0`** |
| 2 | main-thread cost p99 ≤ 1.5 ms | **PASS** | 6f9e82c1: สูงสุด 0.52 ms (กลาง 21 ศึก); **8ebf86f4: p99 0.3305 ms, max 0.4678 ms** (`s1_report`) |
| 3 | 1 ศึกเต็ม: capture rate ≥ 99% (`captured / ΣΔinfoCount`) | **PASS-with-limitation** | counted-only **100.00% (1245/1245 ตรง 21/21 ศึก)** บน `6f9e82c1`; ซ้ำบน `8ebf86f4`: 6/6 ตรง 1:1 · ตัวหารตามตัวอักษรให้ 173.73% เพราะ `infoCount` เพดาน 101 → ดู §3 |
| 4 | 0 duplicate `seq` + ลำดับ event ตรงกับลำดับในเกม | **PASS-with-limitation** | `seq` 2,163/2,163 ไม่ซ้ำ ไม่มี gap; **เทียบ log บนจอ 6/6 บรรทัดตรงกัน + 1 บรรทัดระบบ** (ศึก 13:09 มีเพียง 7 บรรทัด จึงไม่ครบ 30 ตามถ้อยคำ) · ลำดับบนจอ = ใหม่สุดอยู่บน |
| 5 | battle snapshot ตรง UI: สุ่ม 20 เฟรม (`nowActiveUnit`, HP 3 unit, cell) ตรง 20/20 | **PASS** | **20/20 เฟรม** (frame 301301–301415, Δframe ≤1) · ตรวจ 6 units × 20 = **120 checks, mismatch 0** · ในนั้น **60 checks เป็นยูนิตที่เลือดลดแล้ว** · เลขบนจอ `104/498` ตรงกับ `shownHp/shownMaxHp` 103.539/496.279 |
| 6 | memory: managed heap โต ≤ 20 MB / 10 นาที, ring buffer ไม่โตเกิน capacity | **PASS** | **8ebf86f4:** `growthMb=9.93` ใน 784.7 s (baseline 72.72 → 82.66 MB) · control run §8: ON 127.2→77.24 MB, OFF 85.5→83.93 MB (Δ ติดลบทั้งคู่) · rings log **4096/4096** (capacity), battle 0/900 → ไม่โตเกิน · หมายเหตุ: ช่วง**มีศึก**ในเซสชันก่อนโต +33 MB/28 นาที (ยังไม่แยกสาเหตุ) |
| 7 | ปิด `coop.capture=false` กลางศึก → ไม่มี event ใหม่ เกมเดินปกติ ไม่มี exception | **PASS** | 13:13:56–13:14:07 กลางศึก HardFight: log 6→6, marker 1→1, `battle.ndjson` ขาด frame 214872→216252 (1,380 เฟรม) แล้วกลับมาต่อ, `errors=0`; บน world map ก็ผ่าน (750→750) |
| 8 | marker เริ่ม/จบศึก (carry-over S0 ข้อ 9) | **PASS** | marker 42 events = 21 คู่ · unpaired 0 · open-at-EOF 0 · `battleType` ใน marker ตรงกับ snapshot ทุกเฟรม |
| 9 | ไม่เขียน state ของเกม: `SaveSlot*` hash ก่อน/หลังเท่ากัน (เล่นบนสำเนา) | **PASS** | ดู §2 — `SaveSlot*` **เปลี่ยน 0 ไฟล์** จาก 57 ไฟล์; เปลี่ยนเฉพาะ `PlayerprefData.dat` (ข้อยกเว้นที่ระบุใน §1.2 ข้อ 6) |

**สรุป: 7 PASS · 2 PASS-with-limitation · 0 FAIL · 0 NOT-RUN** → **ครบทั้ง 9 ข้อ** (2 ข้อมีข้อจำกัดที่ระบุไว้ในตารางและ §3/§4)

## 2. เซสชัน 21 ศึกเล่นบนเซฟอะไร + hash ก่อน/หลัง

- **เล่นบนสำเนา ไม่ใช่เซฟจริง** — `coop-test/S1-20261020-113926/save/` (57 ไฟล์) ถูกสลับเข้าเป็น `LongYinLiZhiZhuan_Data/Save` เมื่อ 11:39; เซฟจริงถูกเก็บเป็น `LongYinLiZhiZhuan_Data/Save.real-S1`
- hash ตอนสลับ (`savehash.real.before.txt` = `savehash.copy.before.txt` = `savehash.swapped.txt`): ทั้งสามชุดตรงกัน (57 ไฟล์) → สำเนาเหมือนจริง ณ จุดเริ่ม
- hash หลังจบเซสชัน (วัด 12:43, `Save` = สำเนา):

| กลุ่มไฟล์ | เปลี่ยน / ทั้งหมด | หมายเหตุ |
|---|---|---|
| `SaveSlot*/` (Info, Save, Hero, TempHero, steam_autocloud.vdf) | **0 / 56** | mtime ของ `SaveSlot*/Info` ยังเป็น 11:39 → ไม่มีการบันทึกทับระหว่างเล่น |
| `PlayerprefData.dat` | 1 / 1 | `8038352c…` → `9d226789…` (เกมเขียนเอง — ข้อยกเว้น §1.2 ข้อ 6) |

**เซฟจริง (`Save.real-S1`) ยืนยันซ้ำหลังปิดเกม (12:47):** 57/57 ไฟล์ hash ตรงกับ `savehash.real.before.txt` ทั้งหมด — changed 0, missing 0, added 0 (รวม `PlayerprefData.dat` ที่ไม่ถูกแตะเลย เพราะเกมไม่เคยชี้ path นี้)
→ เกณฑ์ 9 ครอบทั้ง "เล่นบนสำเนา" และ "เซฟจริงไม่ขยับ"

## 3. capture rate — PASS-with-limitation และกลุ่ม `Δ=0`

`BattleController.infoCount` **ไม่ใช่ตัวนับสะสม**: เพดานที่ 101 (max ทั้งไฟล์ 101) และตัวนับหยุดเดินเมื่อ panel เต็ม

| นิยาม | ค่า |
|---|---|
| `captured ÷ ΣΔinfoCount` (ตามตัวอักษรของแผน) | 173.73% (2,163 ÷ 1,245) → **ใช้เป็นตัววัดไม่ได้** |
| counted-only: event ที่ `Δ=1` ÷ ΣΔinfoCount | **100.00%** (1,245 ÷ 1,245) · ตรงกัน **21/21 ศึก** (ส่วนต่าง 0 ทุกศึก) |

918 event ที่ `Δ=0` แยกได้ 3 กลุ่ม (วัดจริง ไม่ใช่การเดา):

| กลุ่ม | จำนวน | `infoCount` ตอนเกิด | ตีความ |
|---|---|---|---|
| popup ตัวเลขกลางสนาม `[真伤][内伤][外伤][被反伤][毒素][吸内][淬毒…][失衡]` | 422 | 74% เกิดที่ `infoCount < 101` (26% ที่ 101) | **คนละช่องแสดงผล** — เกมไม่นับเป็น log line ตั้งแต่ต้น จึงไม่มีทางมีตัวหารจาก `infoCount` |
| log line จริง `<color=…>A</color>对<color=…>B</color>造成了伤害…` | 405 | **101 ทั้งหมด (100%)** | เกิดหลัง panel เต็ม → ตัวนับหยุดเดิน **ไม่ใช่เพราะเป็นช่องอื่น** |
| annotation `(<color=…>…雷电3合)` | 91 | **101 ทั้งหมด (100%)** | เหมือนกัน — หลัง panel เต็ม |

แยก popup ตามชนิด: 真伤 137 · 内伤 88 · 被反伤 38 · 外伤 37 · 被足履淬毒 37 · 饰品淬毒 22 · 吸内 16 · 毒素 12 · 被反伤吸内 12 · 被护甲淬毒 10 · 被头盔淬毒 7 · 失衡 6

**ข้อจำกัดที่ต้องเขียนติดรายงาน:** counted-only 100% พิสูจน์ว่า **ทุกครั้งที่ตัวนับเดิน มี event ถูกเก็บ 1 ครั้งพอดี** แต่ **ไม่ได้** พิสูจน์ความครบของ 496 log line ที่เกิดหลัง panel เต็ม และ 422 popup — สองกลุ่มนี้ยังไม่มีตัวหารอิสระ ต้องใช้ fallback (อ่านค่า `Text` ของ panel / นับผ่าน `Text.text` setter) ถ้าจะปิดช่องนี้

## 4. Frida cross-check BigMap — 3/3 แต่มีข้อจำกัด

**เพิ่มบน build สุดท้าย `8ebf86f4` (13:06–13:20):** 3/3 pairs Δx = Δy = 0; pair ที่แน่นที่สุดจับคู่ด้วย frame (Δframe = 13 ≈ 0.2 s) → รายละเอียด `frida-crosscheck-final-build.md`
ข้อจำกัดที่ยังอยู่: ทั้ง 3 ครั้ง **หยุดนิ่ง** และ `worldTime` ไม่ขยับเลยตลอด 25 นาที (`5.2903785`, 10/9/3) → ยังยืนยันการ sync ของเวลาไม่ได้

| # | build | สถานะ | Frida (x, y) | IPC (x, y) | Δ |
|---|---|---|---|---|---|
| 1 | 841ae14 (2026-10-19) | หยุดนิ่ง | 1886.27429, 283.28052 | 1886.2743, 283.28052 | 0 |
| 2 | 6f9e82c1 · 12:05:58 / frame 117302 | หยุดนิ่ง (หลังเดิน) | -4339.34375, -42.574631 | -4339.3438, -42.57463 | 0 |
| 3 | 6f9e82c1 · 12:08:37 / frame 138102 | หยุดนิ่ง (หลังเดิน) | -2424.58374, 431.294586 | -2424.5837, 431.2946 | 0 |

- 3 samples ครบตามจำนวน · Δ = 0 ทุกครั้งภายในความละเอียด float32
- **ข้อจำกัด:** (ก) sample 1 อยู่บน build เก่า → บน build `6f9e82c1` มีแค่ 2/3 (ข) ทั้ง 3 ครั้งวัดตอน **หยุดนิ่ง** ไม่ใช่ตอนเดิน (ค) ทั้งสองฝั่งอ่าน `HeroData.bigMapPos` ตัวเดียวกัน → พิสูจน์ interop/bridge ไม่ได้พิสูจน์ว่าเป็นค่าที่เกมวาดบนจอ
- ยังไม่มี sample บน build สุดท้าย (world stream) — ต้องวัดใหม่หลังรีสตาร์ต

## 5. heap — ข้อมูลจาก build `6f9e82c1` (ก่อนมี control run; ผล control run อยู่ §7)

| เวลา | heap | หมายเหตุ |
|---|---|---|
| 11:56 | 79.65 MB | หลัง GC drop แรก |
| 12:06 | 78.91 MB | ก่อนศึกส่วนใหญ่ |
| 12:18 | 98.17 MB | ระหว่างชุดศึก |
| 12:34 | 112.16 MB | หลังศึกสุดท้าย 3 นาที (ไม่มีศึก) |

โต +33 MB ใน 28 นาที แต่ **ไม่โตตอนนิ่ง** → ยังแยกไม่ได้ว่าเป็นการจัดสรรของเกม (สร้าง object ต่อศึก) หรือของ mod
**control run ที่ต้องทำ (ยัง NOT-RUN):** 2 ช่วงเวลาเท่ากัน (เช่น 6 นาที/ช่วง) ในสภาพเดียวกัน — ช่วง A `Capture=false`, ช่วง B `Capture=true` — วัด heap ต้น/ปลายด้วย `S1Metrics.HeapMb` จาก heartbeat แล้วเทียบ Δ; ต้องทำหลังรีสตาร์ตด้วย build สุดท้าย

## 6. งานที่เหลือก่อนปิด S1 (ต้องมีเกม + ศึก)

1. สลับ `Mods/LongYinCoop.dll.new` → `.dll` (ต้องปิดเกมก่อน) แล้วรันซ้ำข้อ 1–3 บน build สุดท้าย + ยืนยัน world record ไม่มี `npcs` และขนาด/นาทีลด
2. เกณฑ์ 5: UI probe 20 เฟรม (ต้องอยู่ในศึก)
3. เกณฑ์ 7: kill switch กลางศึก 10 วินาที
4. เกณฑ์ 4 ส่วน UI: เทียบ 30 บรรทัดกับ log บนจอ
5. เกณฑ์ 6: control run ปิด/เปิด capture ช่วงเวลาเท่ากัน
6. Frida cross-check เพิ่มบน build สุดท้าย (ให้ได้ 3/3 บน build เดียว)
7. ปิดท้าย: สลับเซฟคืน (`Save.real-S1` → `Save`) + hash ยืนยัน + `git tag`

## 7. ผลวัดบน build สุดท้าย `8ebf86f4` (12:47–13:01, pid 2984, capture `20261020-124808`)

**world stream ใหม่ (ตัด `npcs`, `WorldStreamHz=2`):**

| ตัวชี้วัด | ก่อน (`6f9e82c1`) | หลัง (`8ebf86f4`) |
|---|---|---|
| field ใน record | 14 (มี `npcs`) | **13 — ไม่มี `npcs`** (`t, frame, scene, year, month, day, hour, difficulty, worldTime, partyPos, resources, hud, collectMs`) |
| ขนาดต่อ record | ~11,400 B | **1,108 B** |
| อัตราเขียนจริง | 10 Hz | **1.92 rec/s** (median interval 521 ms → ตรง 2 Hz) |
| ขนาดต่อนาที | ~6.6 MB | **~0.128 MB** (ลด ~51 เท่า) |
| meta.json | — | มี `worldHz=10`, `worldStreamHz=2`, `worldStreamNote` |

**heap control run (world map, ไม่มีศึก, เท่ากัน 2 ช่วง):**

| ช่วง | capture | เวลา | heap ต้น → ปลาย | Δ |
|---|---|---|---|---|
| A | ON | 12:49:35 → 12:54:42 (5.1 นาที) | 127.2 → 77.24 MB | **-49.96 MB** |
| B | OFF | 12:54:46 → 12:59:49 (5.05 นาที) | 85.5 → 83.93 MB | **-1.57 MB** |
| รวม | — | 12:49:35 → 12:59:49 (10.2 นาที) | 127.2 → 83.93 MB | **-43.27 MB** |

- `s1_report` ตอน 13:01: `heap.nowMb=82.66`, `baselineMb=72.72`, **`growthMb=9.93`** ใน 784.7 s
- `s1_report` ตอน 13:35 (22 นาทีหลังโหลด): `nowMb=77.36`, **`growthMb=4.63`** → ไม่โตขึ้นตวมเวลา (ค่าลดลงหลัง GC)
- rings: `log.count=4096 / capacity 4096` (เต็มพอดี ไม่เกิน), `battle 0/900`
- `written=813`, `dropped=0`, `err=0`, `postfix.calls=0` (ยังไม่มีศึก)
- **kill switch บน world map:** ตั้ง `capture=false` 5 นาที → world record 750 → **750** (ไม่มี record ใหม่), เกมเดินปกติ, `err=0`

**ข้อจำกัด:** ช่วงที่ **มีศึก** ยังเห็น heap โต (+33 MB/28 นาทีในเซสชัน 6f9e82c1) — ยังไม่ได้แยกว่าเป็นของเกมหรือของ mod; ตัวเลขในตารางข้างบนเป็นช่วง world map ล้วน

## 8. เครื่องมือวัดที่แก้และทดสอบแล้ว (13:30–13:45)

- `tools/ui-probe.js` — เรียกใช้ได้จริงนอกศึก (`unitCount=0, skipped=0`) หลังแก้ 2 บั๊ก:
  (ก) frida-il2cpp-bridge คืน wrapper ที่ truthy สำหรับ null ref → ใช้ `handle !== "0x0"` กัน
  (ข) ยูนิตค้าง/null ref ทำ probe ตายทิ้งทั้งตัว → ครอบ try/catch ต่อยูนิตและนับ `skipped`
- `tools/ui-log-probe.js` — อ่าน log บนจอผ่าน `Il2Cpp.gc.choose(Text)` แล้วเลือก Text ที่ **ชื่อ transform = `Text` · parent = `Content` · grandparent = `Viewport`**
  (generic `GetComponentsInChildren<T>` เรียกผ่าน bridge ไม่ได้: `expected an integer` — ยืนยันแล้ว; การเทียบ `transform.handle` กับ `infoText` **ไม่ตรง** แม้เป็น object เดียวกัน → ต้องใช้ชื่อ)
- `tools/compare_ui_frame.py` — เพิ่ม `--tol` (default 2) เพราะ `BattleSampler` เขียนทุก ~3 เฟรม; join แบบ exact frame ให้ผลลวง "no capture frame" 4/7
- **ผลรันในศึกจริงแล้ว** (13:09 HardFight): log 6/6 บรรทัดตรง, probe 7/7 ตรง → เกณฑ์ 5 ยังขาดอีก 13 เฟรมและยังไม่ครอบช่วงเลือดเปลี่ยน

## 9. ร่างแผนย่อย S1.5 (S3-spike) — **รออนุมัติ ยังไม่เริ่ม**

เป้าหมาย: พิสูจน์ว่าเรียก method ของเกมเพื่อสั่งยูนิตที่ "ถึงตา" ได้จริง บน **สำเนาเซฟ** และไม่มีเครือข่าย

**งานที่ 1 (ต้องทำก่อนทุกอย่าง): โปรโตคอลแยกเซฟทดสอบ §1.2 ให้รันซ้ำได้แบบอัตโนมัติ**
- สคริปต์เดียว `tools/save-split.sh` (หรือ `coopctl save swap`) ทำ: ปิดเกมให้สนิท → ตรวจ process ว่าง → hash `Save` จริง → คัดลอกทั้งก้อนเป็น `Save.real-S1.5-<ts>` → สลับสำเนาเข้าเป็น `Save` → hash สำเนาทั้งชุด → เขียน `savehash.before/after` ลง `coop-test/S1.5-<ts>/`
- ต้องมีคำสั่งคืนสภาพที่ทดสอบแล้ว (`restore`) + hash ยืนยันก่อน/หลัง
- เกณฑ์ผ่านของงานที่ 1: สลับ+คืน 2 รอบติดกัน แล้ว hash ของเซฟจริงตรงกันทุกไฟล์ (ยกเว้น `PlayerprefData.dat` ที่ต้องไม่ถูกแตะเลย เพราะปิดเกมแล้ว) และไม่มีไฟล์ตกค้าง

**งานถัดไป (ยังไม่เริ่ม จนงานที่ 1 ผ่าน)**
2. `TurnGate` อ่านอย่างเดียว: `BattleController.nowActiveUnit/phase/turnIndex` → ตอบ "ถึงตายูนิตนี้ไหม" (ยังไม่เรียก method ใดที่เปลี่ยน state)
3. JIT: หา entry method สำหรับ move/attack ผ่าน interop + ยืนยันว่าต้องตั้ง active unit/selected cell อย่างไร — **ต้องขออนุมัติก่อนเรียก method ที่เปลี่ยน state ครั้งแรก**
4. `spike move` / `spike attack` ผ่าน IPC + `Audit` log ก่อน/หลัง (cell, hp, frame)
5. Rollback: `.dll.off` + คืนเซฟจากสำเนา
