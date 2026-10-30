# V1 (time-plan) — รายงานปิดสายงาน: **ยกเลิก (CLOSED-CANCELLED)**

> label `TIME-V1-20261031-044249` · ปิด 2026-10-31 (docs session) · **ไม่มี tag** (ยกเลิก — ไม่นับเป็น stage ที่ปิด)
> · วลีที่ใช้เริ่ม: `เริ่ม V1 session` (RUNBOOK-V1 ในโฟลเดอร์เดียวกัน)
> · **verdict: `INCOMPLETE-probe-down`** — ไม่ได้ผลวัด TC-1…TC-4 · หยุดตาม RUNBOOK §G.1 (hook ติดไม่ครบ 6)
> · ป้ายหลักฐาน: RUNTIME = วัด/อ่านจากไฟล์จริงในรอบนี้ · OBSERVED = เห็นจากหน้าจอ/ฟอร์มผู้ใช้ · UNKNOWN = ยังไม่รู้

## 1) Timeline (RUNTIME — จากไฟล์ในโฟลเดอร์นี้ + ระบบไฟล์)

| เวลา (2026-10-31) | เหตุการณ์ | หลักฐาน |
|---|---|---|
| ~05:00:28 | deploy build TimeV1 `ddbb7fe8…` + PDB `1213b165…` เข้า `Mods\` (สำรอง `.prev-6a94abb5-20261031-050028` / `.prev-856d3bed-20261031-050028`) | mtime ของ `.prev-*` = 02:55:29 **ขัดกับ suffix `050028`** — anomaly บันทึกไว้ ไม่ได้อธิบาย (UNKNOWN) |
| 05:01:48 | `split` label `TIMEV1-TC1-r1` (57 ไฟล์) | `coop-test/TIMEV1-TC1-r1/history.ndjson` |
| 05:02:05 | `preflight-launch` = **READY** (11/11 OK · mounted-is-copy changed=0 · real-aside `d3f7cb2f` 57 ไฟล์) | `preflight-launch-TIMEV1-TC1-r1.txt` |
| 05:41:48.966 | เกมเริ่ม (ผู้ใช้เปิดเอง) · `[TimeV1] writer open` | `mlog-TimeV1-TC1r1.txt:1` |
| 05:41:49.029 | **`TimeV1: patch count หลังติด = 2 (คาด 6) — ถอนทั้งชุด`** | `mlog:2` |
| 05:41:49 – 05:46:35 | **install attempt ซ้ำ 283 ครั้ง** (ทุก ~1.0 s) — ทุกรอบ `writer open` → patch 2/6 → ถอน → `writer closed lines=0` | นับจาก mlog (283 บรรทัด "patch count … = 2") |
| 05:46:35.787 | `[TimeV2a] writer closed lines=5` (ปิดพร้อมเกม) | `mlog` ท้ายไฟล์ |
| 05:50:47 | `restore` สำเนา → เซฟจริง `d3f7cb2fcae2…` 57 ไฟล์ `ok:true` | `coop-test/TIMEV1-TC1-r1/restore-20261031-055047.json` |
| 06:02:12 – ~06:07 | **ผู้ใช้เปิดเกมบนเซฟจริง** (ML log `26-10-31_6-2-12.log`) ⇒ drift เฉพาะ `PlayerprefData.dat` | `rollback-user-cancel-20261031.md:21` |
| 06:13:24 | **rollback DLL** `ddbb7fe8` → **`6a94abb5`** · PDB `1213b165` → **`856d3bed`** (copy จาก `.prev` · ไม่ลบ `.prev`) | `rollback-user-cancel-20261031.md` + hash วัดซ้ำในรอบปิด |
| 06:38:08 | **rebaseline volatile-only** → baseline ใหม่ `7cd024019604…` @ `coop-test/baselines/20261031-063808` (มติผู้ใช้ 2026-10-31) | `REBASELINE.json` |

## 2) ผลวัดรายข้อ (เทียบเกณฑ์ของ V1)

| ข้อ | ผล | หลักฐาน/เหตุผล |
|---|---|---|
| hook ติดครบ 6/6 (เงื่อนไขตั้งต้นของทุก TC) | **FAIL** | ติด 2/6 ทุกรอบ (283/283) — RUNBOOK §G.1 ⇒ หยุดทันที |
| **TC-1** (ยกเลิกกลางคัน — refund/credit) | **NOT-RUN** | probe TimeV1 = 0 events (`TC1-r1.ndjson` 0 byte · `analyze-TC1-r1.json`: `events:0`, `r1.*` = 0 ทั้งหมด) |
| **TC-2 / TC-3 / TC-4** | **NOT-RUN** | ไม่ได้เริ่ม — หยุดที่เงื่อนไข hook |
| R1 / R2 / R3 (observational จาก TimeV2a) | **ไม่มีข้อมูล** | probe TimeV1 ตาย · TimeV2a ให้ได้แค่ 5 events (คนละตัววัด) |
| ฟอร์มหน้าจอ TC-1 (ผู้ใช้กรอก) | **ครบ** | `TC1-r1-screen.json` (OBSERVED) |
| เซฟจริงไม่ถูกเขียนระหว่างรอบ | **PASS** | `SaveSlot*` ไม่ถูกเขียน · restore EXACT `d3f7cb2f` 57 ไฟล์ · `leftovers: none` |
| pref/net.cfg ไม่ค้าง | **PASS** | `TimeV1a=false` · `TimeV2a=false` · `Sp5T0=false` · `AllowRemoteInput=false` · `net.cfg` = `12541f34…` (444 B, `enabled=false`) |
| เกมไม่ crash / ไม่มี exception จาก mod | **PASS** | `[ERROR]` = 0 · `Exception` = 0 ใน mlog รอบนี้ |

## 3) ข้อมูล observational ที่ได้จริง (ใช้ตัดสินอะไรไม่ได้ตามเกณฑ์)

- **TimeV2a (5 events, 1 session)** — `analyze-TC1-r1.json`: `pause_end` (leftWorkingDay 2) → `day_tick` (**2→1**, `totalWorkingDay` 0→1, `skipK=1`) → `pause_end` → `work_abort` (`nextDayTime=0.694`) · `dayTime=1`, `pauseTime=0.25`, `hour≈5.078` คงที่ทุก event
- **gap 43,063 ms** ระหว่าง `pause_end` (t=`1793400258116` ≈ 05:44:18.116) กับ `work_abort` (t=`1793400301179` ≈ 05:45:01.179) ⇒ analyzer ติดป้าย `contaminated_candidate: true` — **ห้ามใช้เป็นข้อมูลสะอาด**
- **หน้าจอ (OBSERVED, ผู้ใช้):** dialog ยกเลิกโผล่จริง (ยืนยัน 2 ตัวเลือก) · เงิน 7,119 เท่าเดิมก่อน/หลัง · ทรัพยากร 6 ค่าเท่าเดิม · ชั่วยาม 21→22 (ไม่ข้ามวัน) · ไม่มี UI ค้าง
- **ผลต่อข้อ (ข) "ยกเลิกกลางคันได้รางวัลหรือไม่" = ยังตัดสินไม่ได้** — ไม่มีภาพช่วงหลังเริ่มงาน จึงพิสูจน์ทั้ง "ไม่ได้รางวัล" และ "ถูกหักค้าง" ไม่ได้

## 4) Root cause — สิ่งที่รู้ vs ไม่รู้

**รู้ (RUNTIME):** ไม่มี exception ใด ๆ ตอนติดตั้ง (ERROR/Exception = 0) · กลไกถอนทำงานถูก (patch count ≠ expected → ถอนทั้งชุด) · ไม่มี event หลุดออกไปเลย (0 บรรทัด)

**UNKNOWN:**
1. เหตุใดติดได้แค่ 2 จาก 6 (3 patch บน `StopWorking` + 3 บน `MiddleStopWork`) — ไม่มี diagnostic ราย patch ใน log
2. 2 patch ที่ติดคือตัวใด
3. ทำไม retry ไม่หยุดหลังล้มเหลวซ้ำ — วิ่ง 283 ครั้ง/4m47s ตลอดจนปิดเกม (ออกแบบไว้หรือบั๊ก)
4. anomaly mtime ของ `.prev-*-050028` = 02:55:29

## 5) สิ่งที่ต้องแก้ก่อนรัน V1 ใหม่ (ข้อเสนอ — ยังไม่ทำ)

1. log รายละเอียด **ต่อ patch** (target/kind/สำเร็จ/exception) ก่อนถอนทั้งชุด
2. **หยุด retry** หลังล้มเหลว N ครั้ง (เช่น 3) แทนที่จะวนทุกวินาทีตลอด session
3. ไม่เปิด writer ซ้ำเมื่อ patch ไม่ครบ (ตอนนี้เปิด-ปิด 283 รอบ)
4. ยืนยันรายการ 6 patch ว่าตรงกับ metadata จริงของ build (harness ตรวจแบบไม่ต้องเปิดเกม)

## 6) สถานะมติที่เกี่ยวข้อง (ไม่ถูกปิดโดยรอบนี้)

- **D1** (ยกเลิกผ่าน UI เท่านั้น) / **D2** (per-day credit ถาวร) = มติแล้ว 2026-10-29 — คงเดิม
- **D3–D8 / D10** = ยังเปิด · **TC-3** ทดสอบไม่ได้ตามข้อจำกัด D1 (`MiddleStopWork`)
- **ข้อ (ข) ของ NPC-1-ACT-1** = ยังเปิด — รอบนี้ไม่มีข้อมูล R1
- **V2-A** = PROVISIONAL (คนละสาย ไม่กระทบ)
- ข้อเท็จจริงเสริม: **ปั๊มงานเดินปฏิทินจริงระหว่าง task** (TimeV2a: `day_tick` 1 ครั้งจาก 2 วัน) — สอดคล้องมติรอบ 4/5 (ยังไม่ใช่หลักฐานของตัวคูณรางวัล)

## 7) สถานะหลังปิด (ตรวจซ้ำตอนเขียนรายงานนี้)

| รายการ | ค่า |
|---|---|
| `Mods\LongYinCoop.dll` | `6a94abb579e65d5e6b0af917dfed4ba6f5350ad76847190bd7263cda3077b334` (359,936 B) ✓ |
| `Mods\LongYinCoop.pdb` | `856d3beda727218823170f861f532509c5458eb91e51c79f07c0452d3b196035` (97,128 B) ✓ |
| build TimeV1 | `ddbb7fe8…` / `1213b165…` — เก็บในโฟลเดอร์นี้ (`LongYinCoop.dll`/`.pdb` mtime 04:43) |
| `.prev` | `.prev-6a94abb5-20261031-050028` + `.prev-856d3bed-20261031-050028` ครบ ไม่ถูกลบ |
| เซฟจริง | `Save` 57 ไฟล์ digest **`7cd024019604…`** · `leftovers: none` · baseline ใหม่ `20261031-063808` (volatile-only: `PlayerprefData.dat` ไฟล์เดียว) |
| เกม | ไม่รัน (เหลือ `UnityCrashHandler64` ค้าง 3 process จากรอบก่อน — ไม่ใช่ตัวเกม) |
| tag | **ไม่มี** (ยกเลิก) |
