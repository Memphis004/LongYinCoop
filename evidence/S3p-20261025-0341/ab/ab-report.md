# A/B Report — crash ซ้ำบน build เก่า `5d7b7cc5…` หรือไม่ (single-arm, 1 ครั้ง)

Label split: `AB-oldbuild-20261025-054139` · แผนที่กำกับ: `evidence/S3p-20261025-0341/ab-plan-rev2.md` (มติผู้ใช้ 4 ข้อ: gate ก่อนยิง / arm-timing disclosure / เงื่อนไข / การบังคับ expected baseline) · วันที่รัน: 2026-10-25 05:41–06:02

**ข้อสรุปบรรทัดเดียว:** ยิง `spike attack` **หนึ่งครั้ง**บน build เก่า `5d7b7cc5…` สำเร็จ — เกมรับคำสั่ง (`confirmed=true`), hp เป้าลด **111.5318** (≈ 111.53), **ไม่พบ crash ในการทดลองครั้งนี้** · ผลนี้**ไม่พิสูจน์ว่า build ปลอดภัย** และ**ไม่ยืนยันสาเหตุ crash เดิม** (ยัง UNKNOWN)

## 1. สภาพก่อนทดสอบ (OBSERVED)

| รายการ | ค่า | หลักฐาน |
|---|---|---|
| baseline บังคับ | `coop-test/baselines/20261025-052020` · digest `b7f67154cff613c618994fbf6cb0866a0a8b360bbcb557f5b977320f57ce61cf` (volatile_only, 57 ไฟล์) | `REBASELINE.json` ของ baseline |
| split | 05:41:39 · `--expected-baseline …/20261025-052020/savehash.real.before.txt` → `Save vs baseline changed=0 missing=0 added=0` (EXACT fail-closed) → real พักเป็น `Save.real-AB-oldbuild-20261025-054139` | ผลรัน split (แชต + coop-test/<label>/) |
| preflight-launch | **READY ทุกข้อ** (11 checks) · `real-aside-untouched digest=b7f67154cff6` | ผลรัน preflight-launch |
| DLL ที่ deploy | `5d7b7cc5520ae784c5efc198dda8c1c3ba33ff3c55eb7aaa955ede5f63735953` (ตรวจก่อน/หลังเปิดเกม) · build ทดสอบรอบ Gate-2 (`f2ad30cd…`) พักที่ `Mods\LongYinCoop.dll.s3prereq-f2ad30cd` — ไม่ถูกใช้ | sha256sum |
| เกมเปิดโดยผู้ใช้ | 05:43:31 boot · PID 44740 · `Command-Line:` ว่าง · mod load `stage=S2a-3 v0.5.0` · `dev=True allowRemoteInput=False` · `IPC listening \\.\pipe\longyin-coop-44740` (ยืนยัน pipe มีจริงด้วย Python `os.listdir`) | ML log `26-10-25_5-43-30.log` |
| TypeLoadException boot noise | 12 รายการ (UIElements type cache) — ชุดเดิมที่พบทุก session รวม boot ของ incident · บันทึกว่า "น่าจะไม่เกี่ยวแต่พิสูจน์ไม่ได้" | ML log |

## 2. Gate §1 ก่อนยิง (มติผู้ใช้ — ครบ 5 ข้อจึงยิง)

| # | เงื่อนไข | ผล | หลักฐาน |
|---|---|---|---|
| 1 | `nowActiveStateName=WaitAttacking` | PASS | status สด 05:52:58.863: `WaitAttacking` (nowActiveState=10), `paused=false`, `playingAnim=false` |
| 2 | active = `0:0` (ค่าตายตัว) | PASS | `active="0:0"` (จางเสี่ยวตู้) @(11,13) hp 1006.416 |
| 3 | attacker/target มีจริงในศึกปัจจุบัน | PASS | units[] 5 ตัว: ฝั่งเรา 4 มีชีวิตทั้งหมด · **ศัตรูมีชีวิตตัวเดียว `1:10034`** @(11,18) hp 494.4 team 1 |
| 4 | เป้าไฮไลต์ชัด + ไม่ขอบระยะ | PASS (human) | ผู้ใช้ยืนยัน "ยืนยัน — ยิงได้" จากหน้าจอเกม |
| 5 | ระยะจากข้อมูลเชื่อถือได้ | PASS (human+machine) | ผู้ใช้ยืนยัน + status: attacker @(11,13) vs target @(11,18) แถวเดียวกันห่าง 5 ช่อง · `attackRangeCount=83` |

- Machine part ตรวจซ้ำอัตโนมัติโดย client (`tools/coopctl.py` `prefire_attack_block` :116–156 เรียกที่ :278 ก่อนส่ง attack ทุกครั้ง)
- ID ทุกตัวมาจาก status ของศึกปัจจุบันเท่านั้น (ไม่มี ID จากศึกเก่า/S1.5f mapping)
- ไฟล์หลักฐาน: `ab/status-before-20261025-055258.txt` (mtime 05:52:58.914)

## 3. ไทม์ไลน์การยิง (OBSERVED — ทุกค่าจากไฟล์หลักฐาน)

| เวลา | เหตุการณ์ |
|---|---|
| 05:52:58.863 | `spike status` สด (Gate §1 machine) — ML log: IPC connect/disconnect คู่เดียวของช่วง |
| 05:52:58 – 05:54:22.847 | **การยืนยันของผู้ใช้ (ข้อ 4–5) ไม่มีเวลาบันทึกในไฟล์หลักฐาน — UNKNOWN** ทราบเฉพาะกรอบช่วงนี้ (ไม่เดาค่า) |
| 05:54:22.847 | `set_pref allowRemoteInput=true` → ok (response `armed:false` = ยังไม่มี window — ลำดับถูกต้อง) |
| 05:54:23.133 | `spike arm --seconds 120` → ok, remainingMs=119999 — **arm ครั้งเดียว** |
| 05:54:23.398 | `spike attack --id 0:0 --target 1:10034` ส่ง — **arm→attack gap = 265 ms** |
| 05:54:25.419 | ผล: `Ok=true confirmed=True` · hp 494.39996 → 382.86816 (**drop 111.5318**) · `TargetAliveAfter=true` · `TargetInAttackRange=true` · `StateBefore/After=WaitAttacking` · `Call=BattleGridClicked(target)` · `cost=5ms total=1758ms err=-` · คลิกเดียว ไม่ retry |
| 05:54:45.608 | `spike status` หลังยิง: ศึกเดินต่อ `turnIndex` 3→5, ตาถัดไปฝั่งผู้เล่น (`0:51` WaitPlayerInput) — ไม่มี soft-lock |
| 05:55:21–22 | cleanup: `disarm` (armed=false) + `set_pref allowRemoteInput=false` คืนสภาพพัก |
| 05:46:33 / 05:55:22 | Player.log สำเนา "ก่อน/หลังทดสอบ" เก็บ (sha ใน §7) |
| 06:00:19–22 | ผู้ใช้ปิดเกม (กลางศึก): heartbeat `err=0` ต่อเนื่องถึง 06:00:19 → `battle:end` marker (ค่า teardown: `state=None player=0 enemy=0 turn=5` — ไม่ใช่สถิติการเล่น) → `probe: unpatched (owned patches before=1 after=0)` → `Preferences Saved!` — **ลำดับ shutdown ปกติ ไม่มีบรรทัด crash/exception** |

## 4. หลังปิดเกม (ขั้น 8 — OBSERVED)

- Event 1000/1001 ตั้งแต่ 05:40 ที่ match 'LongYin' = **ไม่พบ** (Get-WinEvent; "ไม่มี Event" ไม่ใช่พยานเดี่ยว — ยืนยันคู่กับ process รันต่อ + heartbeat/log ต่อเนื่อง + shutdown ปกติ)
- drift บนสำเนาที่ mount ก่อน restore = **`PlayerprefData.dat` ไฟล์เดียว** (volatile; ยืนยันชื่อไฟล์ด้วยฟังก์ชัน save-split.py ก่อน restore) · **`SaveSlot*` ไม่มีการเปลี่ยน** (สอดคล้อง precedent "เกมไม่มี autosave ตอนออก")
- `restore --label AB-oldbuild-20261025-054139` → สำเนาเก็บเป็น `coop-test/AB-oldbuild-20261025-054139/Save.used-20261025-060226` (57 ไฟล์, digest `4894c5ba4241debc00d11615c0b099b12e09acd8ecccfd98a41940c295597f2f`) · เซฟจริง rename กลับ **ไม่เคยถูกเขียน**
- **หลัง restore: Save ตรง baseline `b7f67154cff613c618994fbf6cb0866a0a8b360bbcb557f5b977320f57ce61cf` แบบ EXACT** (ตรวจอิสระเทียม `baselines/20261025-052020/savehash.real.before.txt`: 57 ไฟล์ changed=0 missing=0 added=0) · **`leftovers: none`**
- สภาพปิด: `MelonPreferences.cfg` = `269b571c…` เท่าก่อนเริ่ม (`DevCommands=true`, `AllowRemoteInput=false`) · ไม่มี pipe · ไม่มี process

## 5. การตีความ (ตามตารางที่อนุมัติในแผน — เข้มงวดตามสิทธิ์สรุป)

- ผลอยู่แถว **"ไม่ crash + log/process ต่อเนื่อง"** = **"ยังไม่พบ crash ใน 1 ครั้ง"** — **ไม่พิสูจน์ความปลอดภัยของ build** และ**ไม่ยืนยัน/ไม่ตัดสินสาเหตุ crash เดิม**
- **arm timing ต่างจาก incident เดิม**: การทดลองนี้ **arm หนึ่งครั้ง** (gap 265 ms) เทียบ incident ที่ **arm ×6 ใน ~2.7 s** (gap สุดท้าย 9 ms — `game-log-crash.log:439–496`) ⇒ การทดลอง**ไม่เทียบเท่า incident ทุกประการ**; ไม่ตัดสินบทบาทของ multi-arm pattern; ไม่มีการเพิ่ม arm ซ้ำ (ตามข้อห้ามของมติ)
- ความต่างอื่นที่ประกาศไว้ล่วงหน้า: ศึก/layout/ID ใหม่ทั้งหมด (Gate §1 บังคับ), build ที่ทดสอบคือประเด็น (5d7b7cc5 vs f2ad30cd), status ของ build เก่าใช้นิยาม alive แบบ v1 (`battleframe/v1`) — เทียบกับ incident ได้เฉพาะเรื่อง crash ไม่ใช่ semantics
- **"จบศึกได้ตามปกติ" = NOT MET** — ผู้ใช้ปิดเกมกลางศึก (รายงานตามจริง; ไม่มีความเสียหายเพราะเซฟจริงไม่ถูกเขียน)
- เปรียบเทียบ faulting module/offset กับ `UnityPlayer.dll+0x1322c1e`: **ไม่ applicable** (ไม่มี crash)

### การจำแนก

**OBSERVED:** ทุกค่าใน §1–§4 (จากไฟล์หลักฐาน: ML log, status/fire/cleanup .txt, Event log query, sha256, save-split outputs)

**INFER:**
- เกมถูกเปิด/ปิดโดยผู้ใช้ (ผู้ใช้แจ้งเอง; agent ไม่มีคำสั่งเปิด/ปิด)
- คลิกของ mod ทำงานเหมือนคลิก UI ในจุดที่วัดได้ (confirmed/hp/state ผ่าน BattleGridClicked) — ไม่ได้พิสูจน์ equivalence ทุกชั้นภายใน
- ไม่เกิด crash "ในเงื่อนไขที่ทดสอบ" — ขยายไปถึงเงื่อนไขอื่นไม่ได้

**UNKNOWN:**
- ต้นเหตุ crash ของ incident (ยังเปิดทั้ง (a) พฤติกรรมเกมเอง (b) โค้ด S3-prereq (c) ปัจจัยแวดล้อม — ผลรอบนี้ทำให้ "โค้ดใหม่เป็นสาเหตุจำเป็น" อ่อนลงแต่ไม่ตัดสิน)
- เวลาที่ผู้ใช้กดยืนยัน Gate §1 ข้อ 4–5 (ไม่มี stamp ในไฟล์ — กรอบ 05:52:58–05:54:22.847)
- พฤติกรรมภายใต้ multi-arm / gap ~9 ms / layout เดิมของ incident (ไม่อนุญาตให้ทดลองซ้ำโดยไม่มีแผนใหม่)

### บันทึกการตรวจ timestamp (ตามคำสั่งผู้ใช้ — ตรวจจากไฟล์จริง ไม่แก้ด้วยการเดา)

รายงานแชตรอบแรกเขียนเวลาของ Gate §1 เป็น "05:55:58" — **ผิด (ความผิดพลาดของผู้เขียนรายงาน: พิมพ์ 55:58 แทน 52:58)** ค่าถูกต้องจากไฟล์หลักฐานคือ **05:52:58.863** (ML log + mtime ของ `status-before-20261025-055258.txt`) · ลำดับเหตุการณ์ไม่เปลี่ยน (gate ก่อนยิงจริง) · ไฟล์หลักฐานทุกไฟล์ถูกต้องตั้งแต่ต้น · ทุกเวลาในรายงานฉบับนี้ตรวจกับไฟล์แล้ว

## 6. ขอบเขตที่ทำ/ไม่ทำ

- ทำ: split (บังคับ expected baseline) → preflight-launch → เปิดเกมโดยผู้ใช้ → Gate §1 → set_pref/arm/attack ครั้งเดียว → สังเกต → cleanup → ผู้ใช้ปิดเกม → ตรวจหลังปิด → restore → ยืนยัน EXACT
- ไม่ทำ: deploy/swap DLL · retry/re-arm · ทดสอบมือ (manual click) · negative 20 เคส · S3 implementation · rebaseline · commit ระหว่างรัน

## 7. ทะเบียนหลักฐาน (ไฟล์ใน `ab/`)

| ไฟล์ | เนื้อหา | sha256 |
|---|---|---|
| `status-before-20261025-055258.txt` | status สดก่อนยิง (Gate §1) | — (ข้อความสั้น, mtime 05:52:58.914) |
| `status-after-20261025-055445.txt` | status สดหลังยิง | — |
| `fire-20261025-055422.txt` | set_pref + arm + attack พร้อม `T=` timestamps + ผล JSON | — |
| `cleanup-20261025-055521.txt` | disarm + set_pref false | — |
| `Player.log.before-20261025-054633.log` | Unity log ก่อนทดสอบ (2,674 B) | `8f0e446963aa5c9295a00290a98ae287dc14975ee3d5d48cb4dd8fa638f3d626` |
| `Player.log.after-20261025-055521.log` | Unity log หลังทดสอบ/ก่อนปิด (19,956 B) | `b4a954dcda4f505fde8e7be5f79bf46daa2a7237fb7b44cf106a287461970183` |
| `Player.log.post-close-20261025-060145.log` | Unity log หลังปิดเกม (81,499 B) | `cd4d2dbb14af5486bc9eedb22457e333fe7023b2221f91650af47fcccc148b86` |
| `ML-26-10-25_5-43-30.post-close-20261025-060145.log` | ML log เต็มของ session (43,072 B) | `e257b0dd7445c9d3225aaabf2dd8019e7a99ff2d2097db724bf5b205e9f555e2` |

- ไฟล์ `.log` **ถูก .gitignore (`*.log`, บรรทัด 47) ตาม convention ของ repo — ไม่ force-add** เก็บบนดิสก์ + sha256 ในตารางนี้แทน
- ต้นฉบับอ้างอิงอื่น: ML log ต้นทาง `MelonLoader\Logs\26-10-25_5-43-30.log` · incident log รอบ Gate-2 `evidence/S3p-20261025-0341/game-log-crash.log` · split/restore records `coop-test/AB-oldbuild-20261025-054139/`
