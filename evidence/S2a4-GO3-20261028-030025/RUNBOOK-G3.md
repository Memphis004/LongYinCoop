# RUNBOOK-G3 — ลำดับ session เมื่อผู้ใช้พิมพ์ "เริ่ม GO-3 session" (S2a-4)

> **ฉบับ:** 2026-10-28 (session `AUTO-20261028-023904`, B5 · อัปเดต 2026-10-28 โดย `AUTO-20261028-070417` และ `AUTO-20261028-072525`) · **แผนอ้างอิง:** `S2a-4-plan.md` rev4 — §3 (เกต GO-3),
> §9 (provenance), §11 (เกณฑ์ E1–E8 ตรงตัว **ไม่แก้เกณฑ์**), §15 (rollback) · **มติที่ใช้:** M3 (overlay = flat key
> net.cfg, default off, ไม่มี hotkey) · M4 (guest slot เดียว) · **M5 = M5-3 ไม่ hook เลย** ⇒ W4(a) = NOT-RUN,
> E5(a) = NOT-RUN ทั้ง 4 กลุ่ม, E5 สูงสุด = PASS-with-limitation, **ห้ามติดตั้ง hook ใด ๆ**
> **เงื่อนไขเบื้องต้น (สถานะล่าสุด 2026-10-28 07:3x — ปิดครบทั้ง 2 ข้อ):**
> 1. **Release artifact — ปิดแล้ว (2026-10-28, session `AUTO-20261028-072525`)** — มติ [G-1] รอบใหม่ = ทาง (ก) พร้อมเงื่อนไข
>    C1–C4 ผ่านครบ ⇒ แก้ using 1 บรรทัด (commit `636c209…`) · build ผ่าน **0W/0E (exit 0)** ณ HEAD `636c209…` ·
>    artifact DLL+PDB ในโฟลเดอร์นี้ครบ (sha256 ใน `provenance.md` — สถานะ **READY**) · ตรวจ 6 ชนิดใน #Strings heap = FOUND ครบ ·
>    DLL มีโค้ด SP5 แต่ `Sp5T0=false` default ⇒ `Apply(false)` = no-op ไม่ติด hook (ตรวจซ้ำตอนรันจริงที่ขั้น 6)
> 2. **baseline เซฟ — ปิดแล้ว (2026-10-28, มติ [G-2])** — rebaseline volatile-only แล้ว: baseline ปัจจุบัน
>    **`df8e5a11beed…` @ `coop-test/baselines/20261028-071340/savehash.real.before.txt`** (57 ไฟล์ · ต่างจาก `58883949…` เฉพาะ `PlayerprefData.dat`) ·
>    หลักฐาน `evidence/AUTO-20261028-070417/c3-c4-rebaseline-record.md` · ขั้นที่ 4 ข้างล่างใช้ baseline นี้แล้ว
> **การอนุมัติที่วลี "เริ่ม GO-3 session" ให้:** [N1] deploy/swap ใน `Mods\` + [N2] save-split split/restore —
> **เฉพาะตาม RUNBOOK นี้** · สิ่งอื่นที่ไม่อยู่ใน RUNBOOK ยังห้าม (เช่น tag, rebaseline นอกมติ, แก้ source)

## ขั้น 0 — session-start ใหม่ทั้งหมด (อ่านอย่างเดียว)

1. เทียบ sha256 เกม 2 ไฟล์กับ VERSION.md (GameAssembly `33c56def…`, metadata `451311d4…`) — ไม่ตรง = **หยุด**
2. เกมไม่รัน (tasklist) — รันอยู่ = **หยุด**
3. `python tools/save-split.py status` — `leftovers: none` + digest ตรง baseline ปัจจุบันตาม VERSION.md
   **หลังมติแก้ DEVIATION-1 แล้ว** — ไม่ตรง = **หยุด** (รายงาน ห้าม rebalance เอง)
4. `git status` — `src/ tools/ tests/` สะอาด; HEAD บันทึกไว้เทียบตอนจบ
5. hash `Mods\LongYinCoop.dll`/`.pdb` ตรง VERSION.md (`1b4c17aa…`/`dcd78ebd…`) — ไม่ตรง = **หยุด**
6. ไม่มี ML log ใหม่ที่ไม่มีคำอธิบาย

## ขั้น 1 — ตรวจ artifact + HEAD ยังตรง provenance

- `git rev-parse HEAD` ต้องเป็น commit เดียวกับที่บันทึกใน `provenance.md` (หรือ descendant ที่ `src/`
  ไม่เปลี่ยน — ถ้า `src/` เปลี่ยน = **หยุด** ต้อง build provenance ใหม่)
- sha256 ของ artifact ในโฟลเดอร์นี้ (DLL+PDB) ตรงตารางใน `provenance.md` — ไม่ตรง = **หยุด**

## ขั้น 2 — สำรอง + deploy (อนุมัติโดยวลีสั่งงาน)

1. ตรวจชื่อ `Mods\LongYinCoop.dll.prev-1b4c17aa` **ยังไม่มีอยู่** (ห้ามเขียนทับ backup เดิม) — มีอยู่แล้ว =
   หยุดถามผู้ใช้
2. คัดลอก `Mods\LongYinCoop.dll` → `LongYinCoop.dll.prev-1b4c17aa` และ `.pdb` → `.pdb.prev-dcd78ebd`
   (ตั้งชื่อจาก 8 หลักแรกของ hash เดิม จริง ๆ ต้องคำนวณจาก hash ที่วัดในขั้น 0 ถ้าต่างจากบันทึก)
3. คัดลอก artifact (DLL+PDB) เข้า `Mods\` → วัด sha256 **หลัง deploy** ต้องตรง artifact เป๊ะ — ไม่ตรง =
   **หยุด** + rollback ทันที (ขั้น 8)
4. บันทึก wall-clock + hash ก่อน/หลัง ลง evidence ของ session

## ขั้น 3 — net.cfg (loopback + overlay)

- แก้เฉพาะ key ที่แผน S2a-4 กำหนด: `enabled = true` · `role = host` · `transport = tcp` · `bind = 127.0.0.1`
  · `port = 47800` · **`overlay = true`** (key ใหม่ตาม M3 — parser อ่าน flat key, default false) ·
  `heartbeatMs`/`sendHz` คงค่าเดิมของไฟล์ จด **sha256 ก่อน/หลัง** + wall-clock ทุกการแก้ (protocol v2)
- **ห้าม log ค่า `secret` ใน evidence ใด ๆ** (คงค่าเดิมในไฟล์)
- จำไว้: ปิด session ต้องคืนไฟล์ให้ hash ตรงค่าก่อนแก้ (ขั้น 8)

## ขั้น 4 — save-split + preflight

- `python tools/save-split.py split --label S2a4-GO3-<ts> --expected-baseline
  coop-test\baselines\20261028-071340\savehash.real.before.txt` — **ต้องระบุ flag นี้ตรง ๆ ห้ามพึ่ง default**
  (baseline ปัจจุบันตาม VERSION.md ตั้งแต่ 2026-10-28 = `20261028-071340` · ถ้า VERSION.md ระบุตัวใหม่กว่านั้นภายหลัง ให้ใช้ตาม VERSION.md แทน)
- แล้ว `save-split preflight` ต้อง **READY** — ถ้า split **REFUSE แม้แต่ไฟล์เดียว (รวม pref)** = **หยุดรายงาน
  ห้าม rebaseline เอง**
- เก็บ `savehash.real.before.txt` + `savehash.copy.before.txt` ลง evidence ของ session

## ขั้น 5 — บอกผู้ใช้เปิดเกม (ห้ามเปิดเอง)

- แจ้งสั้น ๆ: "เปิดเกมได้" แล้ว**รอ** — agent ไม่แตะเกม/IPC client อื่นในช่วงนี้ (overlay กิน guest slot เดียว
  M4; ห้ามรัน `viewer-client.py` พร้อมกัน)

## ขั้น 6 — ยืนยันโหลด + G0 (เกตย่อยแรก — ไม่ผ่าน = NO-GO)

1. ตรวจ ML log ใหม่: mod โหลด, DLL ที่โหลด = artifact (ตรวจเวอร์ชัน/log เปิด IPC + overlay), ERROR/Exception
   เริ่มต้น
   **เพิ่มเติม (สืบเนื่องมติ [G-1] รอบใหม่ — DLL มีโค้ด SP5 compiled แต่ต้องไม่ติด hook):** ML log ต้อง**ไม่มี**
   บรรทัด SP5/Sp5T0 install/patch ใด (เช่น `Sp5T0 installed`, `Sp5T0:` patch count/session) — **ถ้ามี = หยุดรายงาน**
   (สถานะที่ถูกต้องคือเงียบ เพราะ `Sp5T0=false` default ⇒ `Apply(false)` = no-op; `CloseSession("uninstall-not-installed")`
   ที่ guard return ก่อน ไม่ emit อะไร)
2. **G0 = ตรวจว่า `MelonEvents.OnGUI` ยิงจริง** (overlay render ขึ้นจอ/counter เดิน) — **ไม่ยิง = หยุดรายงาน
   ทันที ไม่เดาทางแก้ ไม่ปรับโค้ดก่อนอนุมัติ** (แผน §3/§11)

## ขั้น 7 — วัดตามเกณฑ์ §11 (ตรงตัว — รายงานรายข้อ ไม่ปรับเกณฑ์)

ลำดับ: **E6 → E1/E2 → E3 → E4 → E7 → E8** (E5 ตัดสินจากหลักฐานค้าง + static/audit — ไม่มี hook ตาม M5-3)

| ข้อ | สิ่งที่ agent ทำ | สิ่งที่ผู้ใช้ต้องทำ | เกณฑ์ผ่าน (§11 ตรงตัว) |
|---|---|---|---|
| **E6** set_pref hardening | ส่ง `set_pref allowRemoteInput=true` ขณะ S2 active → ต้องได้ **error+เหตุผล**, S2 ยัง active, `spike move` ยัง DISARMED; ทำซ้ำขณะ idle → ต้องรับแบบเดิม | รอ (ไม่ต้องทำอะไร) | ครบสองฝั่ง |
| **E1** โหลด+ไม่พัง | เฝ้า log ตลอด session | **เล่นจบ 1 ศึก** | 0 ERROR / 0 Exception + จบศึก |
| **E2** overlay ตรงจริง | เทียบ grid/unit/hp บน overlay กับ `spike status` ณ key เดียวกัน ≥ 20 คู่ | เข้าศึก ปล่อยค้างที่ `WaitPlayerInput` | **≥ 20/20** |
| **E3** input isolation | เฝ้า `commandsAccepted` + audit | **เล่น 1 เทิร์นเต็มระหว่าง overlay เปิด** | คลิกเกมปกติ · `commandsAccepted=0` · audit เปลี่ยนเฉพาะจาก action ผู้ใช้ |
| **E4** ต้นทุนเฟรม | A/B overlay ON vs OFF ด้วย `S2FrameMeter`/`s2.metrics` — ศึกนิ่ง `WaitPlayerInput`, protocol v2 (wall-clock + sha256 net.cfg ทุกสลับ) — M5-3 = ต้นทุน overlay ล้วน | คงศึกค้างนิ่งช่วงวัด (ห้ามคลิก action) | \|Δmean\| ≤ 1.0 ms (band ±0.5 = INCONCLUSIVE — รายงานตามจริง) |
| **E5** guest อ่านอย่างเดียว | (b) รัน `s2_static_check.py` = 0 call sites · (c) รวบ IPC audit ทั้ง session · (d) แนบหลักฐานขั้น 6: ML log **ไม่มี** บรรทัด SP5/Sp5T0 install/patch ใด (ประกอบ M5-3) | — | **M5-3 ⇒ (a) = NOT-RUN ทั้ง 4 กลุ่ม, สูงสุด PASS-with-limitation; ห้ามเขียนว่า "hook ผ่าน"** |
| **E7** เซฟปลอดภัย | ตรวจ `SaveSlot*` + digest สำเนาตลอด session | — | `SaveSlot*` เปลี่ยน 0 · digest ตรง baseline (ยกเว้น volatile) · `leftovers: none` |
| **E8** สเกล | บันทึก unit count จริงทุกหน้าต่างวัด | — | บันทึกตามจริง (ไม่มี fixture 60 = ข้อจำกัดของ S2a-4) |

**หยุดทันทีระหว่าง runtime (NO-GO ตาม §11):** G0 ไม่ยิง · input isolation พัง · E4 เกิน margin หลัง optimize
1 รอบ · เกม crash / เทิร์นค้าง / เซฟ hash เปลี่ยน

## ขั้น 8 — ปิด session (หลังผู้ใช้ปิดเกม)

1. ตรวจ digest → `save-split restore --label S2a4-GO3-<ts>` → ยืนยัน **EXACT** ตาม baseline ที่ใช้ + `leftovers:
   none` + rehash อิสระ — ไม่ EXACT = **หยุดรายงาน**
2. คืน `net.cfg` เดิม → วัด sha256 ต้องตรงค่าก่อนแก้ (ขั้น 3)
3. ถามผู้ใช้: **rollback DLL กลับ `.prev-1b4c17aa` หรือคง build ใหม่ไว้** — **ห้ามตัดสินเอง** (ถ้า rollback = คืน
   DLL+PDB จาก `.prev-*` แล้ววัด hash ตรงค่าก่อน deploy)
4. หยุด recorder/ตรวจ orphan process/ไม่มีไฟล์ค้าง

## ขั้น 9 — รายงาน

- เขียน `evidence/S2a4-<label>/s2a4-report.md`: ผลราย E (PASS / PASS-with-limitation / FAIL /
  INCONCLUSIVE / NOT-RUN พร้อมเหตุผล) + hash เซฟก่อน/หลัง + provenance ครบ §9 + unit count + wall-clock
  ทุกสลับ + **ไม่มีค่า secret ใน evidence**
- **ห้ามปิด stage เอง ไม่ tag** — การปิด S2a-4/tag `stage-S2a-4` = มติผู้ใช้แยก

## Rollback สรุป (§15)

net.cfg `overlay=false`/`enabled=false` (ผล ≤ ~1 s ตาม K30 — ยืนยันจริงตอนรัน) → DLL กลับ `.prev-*` →
restore เซฟตามขั้น 8 → M5-3 = ไม่มี hook ให้ถอด
