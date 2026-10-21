# S1.5gB · Phase B (รอบใหม่) — 10-minute stability · ผลวัด

label `S1.5gB-20261021-200449` · build `5177581…` (DLL เดิม 202,752 B) · เกม anchor ตรง `VERSION.md`
ทดสอบบน **สำเนาเซฟ** (ของจริงเก็บเป็น `Save.real-S1.5gB-20261021-200449`) · ไม่มีคำสั่ง mod เปลี่ยน state · ไม่แก้ `src/`

---

## 1. สรุปตัดสิน

| เกณฑ์ (§7: "600 s ไม่มี pause (ถ้ามี pause ให้ต่อเวลาชดเชย) · 0 error ใน log") | ผล |
|---|---|
| เล่นต่อเนื่องจริง ≥ 600 s | **PASS — 694.8 s (11.6 นาที)** |
| ไม่มี pause ค้าง | **PASS** — `paused=true` = **0 sample** (ไม่มี pause เลยทั้งรอบ) |
| ไม่มีช่วงหยุดนิ่ง (เมนู Esc ฯลฯ) | **PASS** — idle stretch > 20 s = **0 ช่วง** |
| `turnIndex`/state เดินหน้า | **PASS** — `turnIndex` 0 → **139** (140 ค่าไม่ซ้ำ) · `battleTime` +18.28 |
| 0 crash / exception / soft-lock | **PASS** — log 170 บรรทัด error-like **0** · ไม่มี gap > 0.60 s ในหน้าต่างวัด · ปิดเกมปกติ |
| ไม่เร่งเวลา | **PASS (หัก 3.5 s แรกออก)** — ดู §3 |
| Save | **PASS** — ดู `save-verification.md` |

**Phase B = PASS** · **S1.5g ปิดครบทุกเกณฑ์** (Phase A 16/16 จากรอบแรก + Phase B PASS จากรอบนี้)

## 2. ข้อมูลดิบ [RUNTIME]

| รายการ | ค่า |
|---|---|
| sampler เริ่ม / เชื่อมเกม | 20:05:17 รอ → 20:15:36 connected `pid=30672` |
| ช่วงจับ sample | 20:15:38.223 → 20:28:08.707 (750.5 s) |
| sample | ok **7,438** · fail **7** (5 ครั้งแรกตอน IPC ต่อติด 20:15:36–39 · 2 ครั้งตอนเกมปิด รวม `[Errno 32] Broken pipe` 20:28:09.308) |
| gap > 0.6 s | **1 ครั้ง** 0.60 s (20:15:39.471 — ตอน connect) · **ในหน้าต่างวัด = 0** |
| sample ในศึก | 6,911 · `units` 9 ตัวทั้งต้น/ท้าย |
| `turnIndex` | 0 → 139 (140 ค่า) · `battleTime` 0.0000 → 18.8831 |
| ผลการต่อสู้ | หน่วยที่มี hp ลด 7/9 · **ตายจริง 4** (`1:10105`, `1:10107`, `1:10102`, `1:10067`) |
| `activeAutoFight` | false 100% (6,911/6,911) |
| `nowActiveStateName` | Attacking 1,964 · WaitPlayerInput 1,888 · WaitMoving 739 · WaitAttacking 705 · Rest 676 · Moving 675 · None 216 · Finishing 17 · Finish 12 · WaitAIPlan 12 · ShowAttackingRage 4 · ShowMovingRage 3 |
| `timeScale` | 1 เป็นหลัก (มี pulse 0.05/0.2 ของเอฟเฟกต์ตีปกติ) |
| log mod | 170 บรรทัด (20:05–20:28) · **error-like 0** |
| USER-OBSERVED | ผู้ใช้เล่นต่อเนื่องโดยไม่พัก ไม่เปิดเมนู Esc ค้าง ไม่กด Save · ปิดเกมแบบไม่ Save ตอน 20:28 |

## 3. การบัญชีเวลา [RUNTIME]

| หัวข้อ | ค่า | หมายเหตุ |
|---|---|---|
| หน้าต่างดิบ (first in-battle sample → สิ้นสุด) | 698.4 s (20:16:30.190 → 20:28:08.707) | `logs/analysis-final.txt` |
| หักช่วงที่ `battleTimeScale=10` | **−3.5 s** (20:16:30.190 → 20:16:33.699, 33 sample) | เกมเร่งความเร็วค้างจากรอบก่อน (ไม่ใช่คำสั่ง mod) — ตัดออกให้เข้มกว่าเกณฑ์ |
| หัก pause (`paused=true`) | −0.0 s | ไม่มี pause เลย |
| หัก idle/เมนู Esc | −0.0 s | idle stretch > 20 s = 0 |
| **เวลาเล่นจริงที่ใช้อ้างอิง** | **694.8 s (11.6 นาที)** | `logs/analysis-window-excl-speedup.txt`: `wall=694.8s paused=0.0s fast_scale=0` |

⇒ เกินเกณฑ์ 600 s อยู่ **94.8 s** และไม่มีองค์ประกอบต้องหักอื่นอีก

## 4. เกณฑ์รอง (ยืนยันว่า "เล่นจริง" ไม่ใช่ค้าง)

- `turnIndex` เพิ่มขึ้นเรื่อย ๆ: 0 → 139 ภายใน 695.8 s (≈ 1 เทิร์น/5 s) — ไม่มีช่วงนิ่งเกิน 20 s เลยทั้งรอบ
- `battleTime` เดินหน้า 18.28 (กลไกนาฬิกาศึกทำงาน ไม่ค้าง)
- มี **การตายจริง 4 ยูนิต** และ hp ลด 7 ตัว ⇒ ไฟต์เดินจริงถึงการต่อสู้ ไม่ใช่แค่ยืนรอ
- ไม่มี sample gap ในหน้าต่าง → main thread ไม่สะดุด
- เกมยังอยู่ `Fighting` ตอนผู้ใช้ออก (20:28:06) ⇒ ออกกลางศึกได้ตามปกติ ไม่ค้าง

## 5. ส่วนเบี่ยงจากแผนที่ต้องบันทึก

| เรื่อง | ข้อเท็จจริง | ผลต่อเกณฑ์ |
|---|---|---|
| เกมถูกเปิดบนเซฟจริง 19:49–20:05 ก่อน split | เกิดจริง (incident #2) · `SaveSlot*` ไม่ถูกเขียน | ไม่มีข้อมูลถูกนับเป็น PASS/FAIL จากช่วงนั้น · ต้อง rebaseline pref |
| `battleTimeScale=10` 3.6 s แรกของหน้าต่าง | ค่าค้างจากรอบก่อน (mod ไม่มีคำสั่งเร่งเวลา) | หักออกจากนาฬิกา → ยัง PASS |
| `split` ต้องส่ง `--expected-baseline` | quirk ของ `newest_baseline()` (มองไม่เห็น `coop-test/baselines/*/`) | ไม่กระทบผล |

## 6. หลักฐาน

`logs/status-100ms.ndjson` · `logs/analysis-final.txt` (หน้าต่างดิบ) · `logs/analysis-window-excl-speedup.txt` (หน้าต่างที่ใช้อ้างอิง) · `logs/analysis-engagement.txt` · `logs/coop-log-slice.txt` · `logs/sampler-console.log` · `logs/{split,rebaseline,restore,status-*}.*` · `RUN-START.txt`
