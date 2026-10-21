# S1.5g · Phase B — 10-minute stability · ผลวัด

label `S1.5g-20261021-160331` · build `5177581…` · วัดด้วย `spike status` ทุก 100 ms (อ่านอย่างเดียว)
window ตามแผน: เริ่มที่ resume รอบสุดท้ายของ Phase A = **18:28:24.030**

---

## 1. สรุปตัดสิน

| เกณฑ์ (§7: "600 s ไม่มี pause (ถ้ามี pause ให้ต่อเวลาชดเชย) · 0 error ใน log") | ผล |
|---|---|
| เวลาเล่นต่อเนื่องจริง ≥ 600 s | **NOT-RUN — วัดได้ 411.1 s (6.9 นาที)** |
| 0 crash / exception | **PASS** (0 error-like ใน 690 บรรทัด; ไม่มี crash; process อยู่ครบ 51 นาที) |
| ไม่มี soft-lock | **PASS เท่าที่วัดได้** (sample ไหลทุก 100 ms ตลอด, `frame` เดิน, ปิดเกมได้ปกติ) |
| `turnIndex`/state เดินหน้า | **PASS ในช่วงที่เล่น** (turn 17→44, ศึกจบเอง) — **ไม่มีข้อมูลในช่วงท้าย** (ผู้ใช้หยุดเล่น) |
| Save | ดู `save-verification.md` (PASS) |

**สรุป: Phase B = NOT-RUN** — ไม่ใช่เพราะเกมพัง แต่เพราะ **เวลาที่เล่นจริงไม่ถึง 600 s** (รายละเอียด §3) ตามกติกา "ห้ามตีความหลักฐานที่ไม่ครบเป็น PASS" จึงไม่รายงานเป็น PASS

---

## 2. ข้อมูลดิบทั้ง run [RUNTIME]

| รายการ | ค่า |
|---|---|
| ช่วงจับ sample | 18:18:11.507 → 19:09:24.802 (3,073.3 s ≈ 51 นาที) |
| sample | ok **30,510** · fail **5** (ดู §5) |
| gap > 0.6 s | 3 ครั้ง สูงสุด **0.60 s** (18:18:11.909 / 18:19:16.938 / 18:35:11.401) |
| sample ในศึก | 29,031 |
| `turnIndex` ที่พบ | 45 ค่า (0–44) |
| `nowActiveStateName` | WaitPlayerInput 24,439 · Finish 2,088 · Attacking 671 · WaitMoving 596 · WaitAttacking 330 · Moving 300 · Rest 231 · WaitAIPlan 51 · UseItem 15 · Finishing 3 · ShowAttackingRage 1 · None 306 |
| `activeAutoFight` | false 100% (29,031/29,031) — ไม่มีการเปิด auto |
| `battleTimeScale` | = 10 แค่ **18:19:34–18:22:05** (1,501 sample, ก่อนผู้ใช้ตั้ง x1 = ช่วง pre-test) · หลัง 18:22:10 = 1 ทั้งหมด · Phase B = 0 sample ที่ไม่ใช่ 1 |
| log mod | 690 บรรทัดในช่วงทดสอบ · **error-like = 0** (`ERROR`/`Exception`/`Traceback`) |

---

## 3. การบัญชีเวลา Phase B (เหตุผลที่ยังไม่ครบ 600 s) [RUNTIME]

| ช่วง | เวลา | หลักฐาน |
|---|---|---|
| Phase B wall | **2,460.8 s** (18:28:24.030 → 19:09:24.802) | sample แรก/สุดท้าย |
| − pause ปุ่ม "หยุด" (R16) | −5.4 s (18:30:50.2–18:30:55.7) | `paused=true` + W4 event 31/32 |
| − เมนู Esc (sim หยุด) | −67.8 s (18:33:20.330–18:34:28.132) | window นิ่งที่ state `Finish`/`Attacking` (ดู §4) |
| − ช่วงจบศึก→เข้าศึกใหม่ | −65.2 s (18:35:03.450–18:36:08.634, `inBattle=false`) | `battleStateName` เปลี่ยน, turn = −1 |
| − ช่วงท้ายที่ผู้ใช้หยุดเล่น | −1,976.5 s (18:36:28.320–19:09:22.267, `turnIndex=0` ค้างที่ `WaitPlayerInput`) | idle stretch 1,973.9 s (§4) |
| **= เวลาเล่นจริง** | **411.1 s (6.9 นาที)** | `analyze-engagement.py` |

**ทำไมช่วงท้ายจึงไม่นับเป็น "เล่นต่อเนื่อง":** `WaitPlayerInput` = เกมรอคลิกของผู้เล่น (clock หยุดโดยดีไซน์) — ไม่มี `turnIndex`/state/battleTime ขยับเลย 1,973.9 s ⇒ เป็น **idle** ไม่ใช่การเล่น ช่วงที่เล่นจริงคือ 18:28:24 → 18:36:28 (ซึ่งรวมการเล่นในศึกแรกจนจบ + เมนู + เปลี่ยนศึก) ⇒ 411 s

> หมายเหตุ: ถ้านับแบบหลวมที่สุด (wall − pause = 2,455 s) จะ "ผ่าน" 600 s ทันที แต่การนับแบบนั้นจะทำให้ช่วง idle 33 นาทีกลายเป็นหลักฐานว่าเกมเสถียรระหว่างเล่น ซึ่งไม่ตรงกับความจริง ⇒ **ไม่ใช้**

## 4. idle / freeze ที่ตรวจพบ [RUNTIME]

| # | ช่วง | ยาว | state | ตีความ |
|---|---|---|---|---|
| 1 | 18:28:29.566–18:30:37.641 | 128.1 s | WaitPlayerInput | ผู้เล่นคิด/เลือก action (clock หยุดโดยดีไซน์) — นับเป็นการเล่น |
| 2 | 18:31:19.198–18:31:41.031 | 21.8 s | WaitPlayerInput | เช่นเดียวกัน |
| 3 | **18:33:20.330–18:34:28.132** | **67.8 s** | `Attacking`→`Finish` | **เมนู Esc** — sim หยุด (`battleTime` 4.641→4.641, turn 37→37) แต่ `frame` 116,322→125,020 (≈128 fps) ⇒ ไม่ใช่ main-thread freeze |
| 4 | 18:35:03.450–18:36:08.634 | 65.2 s | `None` (`inBattle=false`) | จบศึก → เข้าศึกใหม่ (ปกติ) |
| 5 | **18:36:28.320–19:09:22.267** | **1,973.9 s** | WaitPlayerInput (`turnIndex=0`) | ผู้ใช้หยุดเล่น (คุยกับผม) — ไม่ใช่ soft-lock: sample ไหลปกติ, `frame` เดิน, ปิดเกมได้ |

### ข้อเท็จจริงใหม่จากเมนู Esc [RUNTIME]

ในหน้าต่าง 18:33:20.330–18:34:28.132 (676 sample):
- `timeScale` = **1** ตลอด · `battleTimeScale` = **1** ตลอด
- `battlePaused` = **false** ตลอด ⇒ **`battlePaused` ไม่ใช่ตัวชี้วัดว่า "เกมหยุด"** (เมนู Esc หยุด sim ด้วยกลไกอื่น)
- `battleTime` และ `turnIndex` นิ่งสนิท แต่ `frame` เดิน ~128 fps · `playingAnim` ยังรายงาน `True` เป็นช่วง ๆ
- **[USER-OBSERVED]** ผู้ใช้ยืนยัน: กด Esc แล้วเกม pause ในทุกฉาก (battle/BigMap/เมือง) แอนิเมชันหยุดนิ่ง

⇒ ผลต่อ stage ถัดไป: การตัดสิน "เกมเดินอยู่ไหม" ต้องดู `battleTime`/`turnIndex`/state ไม่ใช่ `battlePaused` หรือ `Time.timeScale` อย่างเดียว

## 5. sample ที่ fail 5 ครั้ง [RUNTIME]

| เวลา | บริบท |
|---|---|
| 18:18:12.010 | sample แรกก่อนเข้าเกม (IPC เพิ่งต่อ) |
| 18:19:17.038 | กำลังโหลดเข้าฉากศึก |
| 18:35:11.501 | กำลังออกจากศึก (จบศึก 18:35:03) |
| 19:09:24.903 / 19:09:25.403 | เกมกำลังปิด (`[Errno 22] Invalid argument` = pipe ขาด) |

ทั้งหมดเป็น sample เดี่ยว ๆ ที่จุดเปลี่ยนฉาก/ปิดเกม และมี sample `ok` ต่อทันที — ไม่ใช่ exception ของ mod (log 0 error-like)

## 6. สิ่งที่พิสูจน์ได้แล้วจากรอบนี้ (ใช้เป็นหลักฐานของ stage ถัดไปได้)

1. **จบศึกได้ตามปกติ**: 18:35:03.350 `inBattle=false`, `turnIndex` 44 → −1, ไม่ค้าง (ผู้ใช้ยืนยันตรงกัน)
2. **เข้าศึกใหม่ได้**: 18:36:08.634 `Prepare` → 18:36:27 `Fighting` (`battleTime` รีเซ็ต)
3. เกมรันต่อเนื่อง 51 นาทีโดยไม่ crash/exception และปิดเกมได้ตามปกติ (pid หาย)
4. ไม่มี main-thread freeze: sample ไม่ขาดเกิน 0.60 s ตลอด run แม้ตอนเปิดเมนู Esc

## 7. หลักฐาน

`logs/status-100ms.ndjson` · `logs/analysis-final.txt` · `logs/analysis-engagement.txt` · `logs/coop-log-slice.txt` · `logs/sampler-console.log` · `logs/timeline-raw.txt` · สคริปต์ `analyze-s15g.py`, `analyze-engagement.py`
