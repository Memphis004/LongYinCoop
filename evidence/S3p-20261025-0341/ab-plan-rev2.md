# A/B Plan rev2 (รอการยืนยันขั้นสุดท้าย · ห้ามเริ่ม runtime จนกว่าจะได้คำสั่ง)

rev2 = rev1 แก้ตามมติผู้ใช้ 2026-10-25 (A/B Plan Revision and Execution Authorization) 4 ข้อ:
§1 gate ยืนยันสถานะก่อนยิง · §2 บันทึกความต่าง arm timing · §3 เงื่อนไขการทดลอง · §4 ตรวจการบังคับ expected baseline
rev1 (`ab-plan-draft.md`) เก็บไว้ไม่แตะ เพื่อเทียบสิ่งที่เปลี่ยน

เป้าหมายไม่เปลี่ยน: ตรวจว่า crash หลังเข้า `WaitAttacking` **เกิดซ้ำกับ build เก่า `5d7b7cc5…` หรือไม่** · ยิง **1 ครั้ง** · ไม่ retry

## 0. สถานะที่ยึด (ตรวจแล้ว read-only ณ จัดทำ rev2 — OBSERVED)

- HEAD mod repo `ce3e06c` · `git diff ce3e06c -- src/ tests/` = ว่าง (ซอร์สที่จะอ้าง = ซอร์สที่ commit)
- `Mods\LongYinCoop.dll` = **`5d7b7cc5520ae784c5efc198dda8c1c3ba33ff3c55eb7aaa955ede5f63735953`** (A/B target — ห้าม swap) · build ใหม่ S3-prereq `f2ad30cd…` พักที่ `Mods\LongYinCoop.dll.s3prereq-f2ad30cd`
- เกมไม่รัน (tasklist ไม่พบ `LongYinLiZhiZhuan.exe`)
- `Save` mounted 57 ไฟล์ digest `b7f67154cff6…` ตรง baseline `coop-test/baselines/20261025-052020` (`REBASELINE.json` digest = `b7f67154cff613c618994fbf6cb0866a0a8b360bbcb557f5b977320f57ce61cf`, volatile_only=true, เหตุ = incident บูตบนเซฟจริง 2026-10-25 04:56) · `leftovers: none` (เซฟจริง mount อยู่ — ยังไม่ split)
- `UserData\MelonPreferences.cfg` `[LongYinCoop]`: `DevCommands = true` (spike status ทำงานได้ — ไม่ต้องแก้ cfg) · `AllowRemoteInput = false` (สภาพพัก — จะเปิดชั่วคราวด้วย IPC `set_pref` เท่านั้น ไม่แก้ไฟล์)
- หมายเหตุเครื่องมือ: `save-split.py status` แสดง `~S3p-20261025-0341/... changed=1` เพราะ glob ของ status หา baseline เจอเฉพาะระดับ label เดียว (ไม่คลุม `baselines/` ที่ลึก 2 ระดับ) — การเทียมที่ผูกพันจริงคือ `split --expected-baseline` ในขั้นถัดไป ไม่ใช่บรรทัดนี้

## 1. Gate ยืนยันสถานะก่อนยิง (ใหม่ตามมติ §1) — ทำก่อน `spike arm` และก่อน `spike attack`

อ่าน status สด (`spike status` — read-only) และบันทึกผลเต็มลงไฟล์หลักฐาน **ก่อน arm** ตรวจ 5 เงื่อนไข:

| # | เงื่อนไข | แหล่งตรวจ | ประเภท |
|---|---|---|---|
| 1 | `nowActiveStateName = WaitAttacking` | status สด (client auto: `prefire_attack_block` coopctl.py:116–156, เรียกอัตโนมัติก่อนส่ง attack ที่ :278) | machine |
| 2 | active unit = **`0:0`** (ค่าตายตัวตามมติผู้ใช้ — ถ้า active เป็นตัวอื่น = NOT-RUN ห้ามเปลี่ยน attacker เอง) | status สด field `active` | machine |
| 3 | attacker (`0:0`) และ target มีอยู่จริงในศึกปัจจุบัน: อยู่ใน `units[]`, `unitAlive=true`, target คนละฝั่ง | status สด (client auto ตรวจข้อ 3 ซ้ำตอนยิง) | machine |
| 4 | target ถูกเกมไฮไลต์ชัดเจนเป็นเป้าโจมตีได้ ในโหมดเล็ง **และไม่อยู่ตรงขอบระยะ** | ผู้ใช้ยืนยันจากหน้าจอ (build นี้ไม่ expose range cells — UI ของเกมคือแหล่งเดียวที่เชื่อถือได้; S1.5: `attackRangeGridUnits` ทำนายการรับคลิก 13/13) | human |
| 5 | ระยะโจมตียืนยันได้จากข้อมูลที่เชื่อถือได้ | ข้อ 4 + status สด (`attackRangeCount`, พิกัด attacker/target) บันทึกคู่กัน | human+machine |

- ข้อใดไม่ตรงหรือยืนยันไม่ได้ ⇒ **NOT-RUN และห้ามส่งคำสั่งโจมตี** (รวมถึงห้าม arm)
- **ห้ามใช้ unit ID จากศึกเก่า** (รวม ID จาก incident เช่น `1:10066`, mapping ของ S1.5f หรือ status ที่อ่านไปแล้ว) โดยไม่ผ่านการตรวจสถานะสด — ID ทุกตัวต้องมาจาก status ของศึกปัจจุบันที่อ่านในขั้นนี้
- คำยืนยันของผู้ใช้ (ข้อ 4–5) บันทึกเป็นข้อความ + timestamp ลงหลักฐาน

## 2. ความต่าง arm timing เทียบ incident (ตามมติ §2) — บันทึกเป็นข้อเท็จจริง ห้ามอ้างว่าเทียบเท่า

**OBSERVED จาก incident (`game-log-crash.log:332–496`, 2026-10-25 04:12–04:14):**

| หน้าต่าง | ลำดับ |
|---|---|
| w1 (04:12) | `set_pref allowRemoteInput=True` 04:12:08.404 → arm 1s (09.451) → **arm 120s ×5** (12.187–14.320) → disarm ×2 + `allowRemoteInput=False` (14.839–.847) |
| w2 (04:14) | `set_pref allowRemoteInput=True` 04:14:18.126 → **arm 120s ×6** (18.653 / 19.222 / 19.750 / 20.277 / 20.842 / 21.389 — arm ทุก ~0.52 s, หนึ่งครั้งต่อเคสของ runner) → `spike attack id=0:0 target=1:10066` **21.398 (9 ms หลัง arm สุดท้าย)** confirmed=true → crash 04:14:21 (Event 1000: `UnityPlayer.dll + 0x1322c1e`, 0xc0000005) |

**A/B นี้:** `set_pref True` → gate §1 → **arm 120s ×1** → attack ×1 (gap จริงจะเป็นหลักวินาทีจากการพิมพ์คำสั่ง — บันทึก timestamp จริงทุกคำสั่ง) → cleanup (`disarm` + `set_pref False` เมื่อเกมยังรัน)

**ความต่างที่ประกาศไว้ล่วงหน้า (ห้ามสรุปว่าการทดลองเทียบเท่า incident ทุกประการ):**
1. incident ยิงหลัง arm สะสม 6 ครั้ง (window refresh ต่อเนื่อง ~2.7 s); A/B arm ครั้งเดียว
2. incident gap arm→attack = 9 ms; A/B gap = หลักวินาที
3. attacker/target มาจากศึกใหม่ (ID ต่างจาก incident ต้องตรวจสดตาม §1)
4. build ที่ทดสอบต่างกัน — และนั่นคือประเด็นของการทดลอง (5d7b7cc5 vs f2ad30cd)

**ข้อจำกัดการสรุป:** crash ซ้ำ ⇒ สรุปได้เฉพาะ "crash เกิดซ้ำบน build เก่าภายใต้เงื่อนไข single-arm" — ไม่พิสูจน์บทบาทของ multi-arm pattern และไม่พิสูจน์สาเหตุ · ไม่ crash ⇒ ไม่พิสูจน์ความปลอดภัย และไม่พิสูจน์ว่า incident ต้องมี multi-arm จึงเกิด
**ห้ามเพิ่มการ arm ซ้ำเพื่อจำลอง incident** เว้นแต่ได้รับอนุมัติแยกต่างหาก — ถ้า window 120 s หมดก่อนยิง (mod ปฏิเสธ attack) = ยุติเป็น NOT-RUN รายงาน ห้าม re-arm เอง

## 3. เงื่อนไขการทดลอง (ตามมติ §3)

- บังคับ baseline `coop-test/baselines/20261025-052020` ผ่าน `split --expected-baseline <path ของ baseline นั้น>` (กลไก = §4)
- ใช้ DLL `5d7b7cc5…5953` เท่านั้น — ตรวจ hash ก่อนเปิดเกม · ห้าม deploy/swap
- `spike arm` แล้ว `spike attack` **หนึ่งครั้ง ไม่ retry** — ไม่มีคำสั่งสำรอง
- ผู้ใช้เป็นผู้เปิดเกมและเข้าศึกเอง (agent ห้ามเปิดเกม)
- เก็บ: MelonLoader log เต็มของ session · Unity `Player.log` ก่อน/หลัง (`%USERPROFILE%\AppData\LocalLow\<company>\<product>\Player.log`) · DLL hash ก่อน/หลัง · ถ้า crash = Event 1000/1001 (faulting module + offset)
- **ถ้า crash, hang หรือหลักฐานไม่ครบ ⇒ หยุดทันที ห้ามเปิดเกมใหม่ ห้ามทดลองซ้ำเอง**

## 4. การบังคับ expected baseline — ตรวจจากโค้ดแล้ว (DUMP ระดับซอร์ส: `tools/save-split.py` @ HEAD `ce3e06c`)

**`split` บังคับโดยตรง (fail-closed EXACT):**
- `--expected-baseline` รับค่าเป็น **path ของไฟล์ hash** (`save-split.py:291–292`: `Path(args.expected_baseline)`) — ไม่ใช่ digest string; ค่า `b7f67154cff6` เป็นตัวระบุว่าต้องชี้ไฟล์ของ baseline ไหน
- `:296–299` อ่าน baseline → `report_diff("Save vs baseline", expected, real_hashes)` → **เทียบ 3 ทิศ (changed/missing/added) รวมไฟล์ volatile ด้วย (ไม่มีตัวกรอง VOLATILE_TOP ใน split)** — ต่างแม้ไฟล์เดียว = `SystemExit("REFUSED: Save does not match the real baseline")` → abort **ก่อน rename อะไรทั้งสิ้น**
- `:271` ปฏิเสธถ้าเกมรัน · `:309–322` หลังผ่าน: real → aside (rename ไม่เขียน), copy mount แล้วเทียม copy EXACT กับ real
- ⇒ ถ้า `PlayerprefData.dat` โดนแตะหลัง 05:20 จน digest เลื่อน split จะ REFUSE — ทางเดียวคือหยุดรายงาน (rebaseline ต้องอนุมัติแยก ตามกติกาเดิม)

**`preflight-launch` ไม่มี flag `--expected-baseline` — บังคับผ่านห่วงโซ่ (พิสูจน์แล้วว่าห่วงโซ่ปิด):**
- ตรวจ (ล้วน read-only, exit 1 = ห้ามเปิดเกม): เกมไม่รัน (:799) · `Save.used-*` = ไม่มี (:810–812) · `Save.real-*` เหลือ **ตัวเดียว** (:813–819) · ไม่มี sentinel ซ่อน (:821–823) · MANIFEST มีจริง + aside ตรง manifest (:834–844) · **mounted Save ตรง `savehash.copy.before.txt` ที่ split สร้างจากเซฟจริง ณ ตอนนั้น** (volatile-tolerated, :861–868) · **real-aside ยังไม่ถูกแตะ** (digest เทียม MANIFEST, :870–874)
- ห่วงโซ่: `savehash.copy.before.txt` เกิดหลัง split พิสูจน์ `Save == baseline 20261025-052020` แล้ว ⇒ copy ที่ preflight-launch รับรอง = สำเนาของ baseline นั้น · ระบุตรง ๆ ว่า preflight-launch **ไม่ได้เทียม baseline โดยตรง** — การบังคับ baseline อยู่ที่ split จุดเดียว
- เลือก `preflight-launch` ไม่ใช้ `preflight` (sampler): `preflight` บังคับ prefs disarmed (`:42–46` + `:747–758` — `AllowRemoteInput=false` ขัดกับการทดลองที่ต้องเปิดผ่าน `set_pref` ขณะรัน) และ A/B ไม่มี sampler process

## 5. ลำดับปฏิบัติฉบับเต็ม (หลังการยืนยันขั้นสุดท้าย)

1. **Pre-flight (เกมปิด):** ตรวจ DLL hash + src clean → `split --expected-baseline` → `preflight-launch` → `status` (เกณฑ์ = §7) — ครบก่อนขอให้ผู้ใช้เปิดเกม
2. **ผู้ใช้เปิดเกมเอง** → agent ยืนยัน: ML log session ใหม่ + บรรทัด load `dev=True allowRemoteInput=False` · DLL ที่โหลดยัง `5d7b7cc5…` · เก็บ Player.log สำเนา "ก่อนทดสอบ"
3. **ผู้ใช้เข้าศึก + เข้าสู่ `WaitAttacking` ด้วยตัวเอง** (แจ้ง agent)
4. **Gate §1:** อ่าน status สด บันทึกไฟล์ → ตรวจ 5 เงื่อนไข → ผู้ใช้ยืนยันไฮไลต์+ไม่ขอบระยะ — ข้อใดขาด = NOT-RUN
5. **ยิงครั้งเดียว:** `set_pref allowRemoteInput=true` → `spike arm --seconds 120` → `spike attack --id 0:0 --target <จาก status สด>` — timestamp ทุกคำสั่ง · ไม่ retry · window หมด = ยุติ NOT-RUN ห้าม re-arm
6. **สังเกตหลังยิง:** status ซ้ำ + process รันต่อ + ML log มีบรรทัดใหม่ต่อเนื่อง + ผล attack (confirmed/hp/state) · เก็บ Player.log สำเนา "หลังทดสอบ"
7. **Cleanup (เกมยังรันเท่านั้น):** `spike disarm` + `set_pref allowRemoteInput=false`
8. **ผู้ใช้เล่นจนจบศึกตามปกติ** (หลักฐาน "จบศึกได้") → ปิดเกม → agent ตรวจ digest + restore + รายงาน OBSERVED/INFER/UNKNOWN

## 6. คำสั่งเต็มที่จะใช้ (ยังไม่รัน — รอการยืนยันขั้นสุดท้าย)

```bat
:: ขั้น 1 — pre-flight (เกมปิด)
cd C:\Users\memph\LongYinCoop
sha256sum "C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan\Mods\LongYinCoop.dll"
::   ต้อง = 5d7b7cc5520ae784c5efc198dda8c1c3ba33ff3c55eb7aaa955ede5f63735953
python tools/save-split.py split --label AB-oldbuild-<ts> ^
  --expected-baseline coop-test/baselines/20261025-052020/savehash.real.before.txt
python tools/save-split.py preflight-launch --label AB-oldbuild-<ts>
python tools/save-split.py status

:: ขั้น 4 — gate ก่อนยิง (หลังผู้ใช้เข้า WaitAttacking)
python tools/coopctl.py spike --sub status | tee evidence\S3p-20261025-0341\ab\status-before-<ts>.txt
::   ตรวจ 5 เงื่อนไข §1 — ข้อใดไม่ผ่าน = NOT-RUN (หยุดที่บรรทัดนี้)

:: ขั้น 5 — ยิงครั้งเดียว (arm ครั้งเดียว ไม่ retry)
python tools/coopctl.py set_pref --key allowRemoteInput --value true
python tools/coopctl.py spike --sub arm --seconds 120
python tools/coopctl.py spike --sub attack --id 0:0 --target <target จาก status สด>

:: ขั้น 6–7 — สังเกต + cleanup (เกมยังรันเท่านั้น)
python tools/coopctl.py spike --sub status | tee evidence\S3p-20261025-0341\ab\status-after-<ts>.txt
python tools/coopctl.py spike --sub disarm
python tools/coopctl.py set_pref --key allowRemoteInput --value false

:: ขั้น 8 — post (หลังผู้ใช้เล่นจบศึก + ปิดเกม)
python tools/save-split.py status
python tools/save-split.py restore --label AB-oldbuild-<ts>
python tools/save-split.py status
```

## 7. Preflight PASS/FAIL (ต้องครบทุกข้อก่อนขอให้ผู้ใช้เปิดเกม)

- split: `Save vs baseline changed=0 missing=0 added=0` (เทียม `20261025-052020/savehash.real.before.txt`) · real → aside · `copy vs real` EXACT · ถ้า REFUSE (แม้เพราะ pref) = หยุดรายงาน ห้าม rebaseline เอง
- preflight-launch: **READY ทุกข้อ** (game-not-running · leftovers-unexpected none · leftovers-state 1 aside · no-hidden-sentinel · manifest · aside-matches-manifest · real-save-mounted no · mounted-is-copy · real-aside-untouched)
- DLL hash = `5d7b7cc5…5953` ✓ ไม่มีการ swap · src/tests clean เทียม `ce3e06c`
- ข้อใด FAIL = หยุด รายงาน ไม่เปิดเกม

## 8. หลักฐานที่เก็บระหว่างรัน

ML log เต็มของ session · Player.log ก่อน/หลัง · status JSON ก่อน/หลัง (ไฟล์) · timestamp ทุกคำสั่ง รวม **gap arm→attack จริง** · ผล attack · DLL hash ก่อน/หลัง · save digest ก่อน/หลัง · ถ้า crash = Event 1000/1001 (module + offset + exception code) · รายงานติดป้าย **OBSERVED / INFER / UNKNOWN** — "ไม่มี Event 1000" ไม่มีสิทธิ์สรุปว่าไม่ crash ต้องยืนยัน process รันต่อ + log ต่อเนื่อง

## 9. หยุด/ฟื้นคืน + โปรโตคอลเทียบ crash ซ้ำ (ตามมติ §3)

- หยุดทันทีเมื่อ: crash / hang / สถานะไม่ชัด / DLL hash ไม่ตรง / save integrity fail / หลักฐานไม่ครบ — ห้าม retry ห้ามเปิดเกมเอง · เก็บ log + crash evidence ก่อนแตะอะไร
- **ถ้า crash ซ้ำ:** เทียบ faulting module + offset + exception code กับ incident `UnityPlayer.dll + 0x1322c1e (0xc0000005)` — รายงานทั้ง **ส่วนที่ตรง** และ **ส่วนที่ต่าง** · **ห้ามสรุปสาเหตุจาก offset เพียงอย่างเดียว** (offset ตรงกันไม่พิสูจน์ root cause เดียวกัน, ต่างกันไม่พิสูจน์ต่างสาเหตุ)
- restore: `save-split restore --label AB-oldbuild-<ts>` — ทำเมื่อเกมปิดและปลอดภัย ตามขั้น 8 (หรือรอมติถ้ามีความผิดปกติ)

## 10. ตารางตีความ (กรอบผู้ใช้ คงเดิมจาก rev1)

| ผล | ข้อสรุปที่อนุญาต |
|---|---|
| crash ซ้ำบน build เก่า | ยืนยัน crash เกิดซ้ำได้ภายใต้เงื่อนไข single-arm นี้ — ยังไม่พิสูจน์สาเหตุ, ยังไม่ตัดสินบทบาท multi-arm |
| ไม่ crash + log/process ต่อเนื่อง | ยังไม่พบ crash ใน 1 ครั้ง — ไม่พิสูจน์ความปลอดภัย และไม่พิสูจน์ว่า incident ต้องมีเงื่อนไขอื่น |
| hang / หลักฐานไม่ครบ | Inconclusive — ห้ามนับ PASS |
| ยืนยันระยะ/ส่งไม่ได้ | NOT-RUN — ห้ามยิงทดลอง |

## 11. งานเอกสาร/commit (สายงานแยก — ยังไม่ทำ ต้องอนุมัติแยก ห้ามปะปนกับการทดลอง)

1. incident doc อัปเดต: rebaseline สำเร็จ · pref `5bc97eb2…` · digest `b7f67154cff6…` · baseline `20261025-052020`
2. `VERSION.md`: baseline ใหม่ + กติกา `--expected-baseline` สำหรับ split ถัดไป
3. dedupe รายการ baseline เก่าที่อ้าง `4226730f66ff` ซ้ำซ้อน → ชี้สถานะ superseded
4. commit 2 ชุด (ไม่ใช้ `git add -A`): (i) `tools/negative-tests.py` + `tools/test_negative_preconditions.py` (ii) evidence/ + `coop-test/baselines/20261025-052020/`
- ขอบเขตไฟล์ของสายงานนี้จำกัดเฉพาะรายการข้างบน — ห้ามแตะไฟล์อื่น

## 12. ความเสี่ยงที่ยังไม่ปิด

- ต้นเหตุ crash UNKNOWN — ผล 1 ครั้งไม่พิสูจน์ทั้งสองทิศ
- single-arm ≠ multi-arm ของ incident (§2) — ทิศทางการตีความถูกจำกัดทั้งผลบวกและผลลบ
- arm window 120 s อาจหมดก่อนยิง (gate §1 + ยืนยันมนุษย์ใช้เวลา) → NOT-RUN ห้าม re-arm เอง
- gate ข้อ 4–5 เป็น human-in-the-loop — ผิดพลาดได้; ลดความเสี่ยงด้วยบันทึกคำยืนยันเป็นข้อความ + timestamp + status JSON คู่กัน
- alive ที่อ่านจาก build เก่าเป็นนิยามเดิม (`!hero.dead` ใน status v1) — เทียบกับ incident ได้เฉพาะเรื่อง crash ไม่ใช่ semantics
- การยิง 1 ครั้งเปลี่ยน state ศึกจริง (damage/ศพอาจเกิด) — ศึกต้องเล่นจบตามปกติหลังทดสอบ
- ถ้า crash ซ้ำ: แผนถัดไป (เช่น ทดสอบ manual click เทียบ) ต้องทำแผนใหม่อนุมัติแยก — ไม่รวมใน rev2 นี้

## 13. ขอบเขตเด็ดขาด (จนกว่าการยืนยันขั้นสุดท้าย)

ห้าม split · ห้าม preflight จริง · ห้ามเปิดเกม · ห้าม deploy/swap DLL · ห้าม IPC จริง (รวม `set_pref`/`spike` ทุกชนิด) · ห้ามส่ง attack · ห้าม restore/แก้ baseline/แก้ `VERSION.md` · ห้าม commit/tag · ห้ามเริ่ม negative 20 เคสหรือ S3 implementation — ทุกอย่างเริ่มได้เฉพาะเมื่อผู้ใช้ยืนยันขั้นสุดท้ายหลังรีวิว rev2 นี้
