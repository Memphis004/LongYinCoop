# S2a-4 GO-3 report — label `S2a4-GO3-20261028-074003` (รันจริง 2026-10-29 00:00–01:08)

วลีสั่งงาน `เริ่ม GO-3 session` (ผู้ใช้ 2026-10-28) · RUNBOOK-G3 ขั้น 0–9 · เกณฑ์ §11 ของแผน S2a-4 rev4 **ตรงตัว ไม่ปรับ** · มติ M5 = M5-3 (ไม่ hook เลย)

## deploy / environment

- DLL ที่ deploy: `6a94abb579e65d5e6b0af917dfed4ba6f5350ad76847190bd7263cda3077b334` (359,936 B · source `636c209`, provenance READY @ `1359217`) · PDB `856d3bed…`
- backup: `Mods\LongYinCoop.dll.prev-1b4c17aa` + `.pdb.prev-dcd78ebd` (สร้าง 07:40:28 ก่อน deploy — rollback พร้อม)
- net.cfg: แก้ `enabled=true` + เพิ่ม `overlay=true` (07:40:37 `12541f34…` → 07:41:12 `5ce166da…`) · **คืน EXACT `12541f34…` แล้วตอนปิด session (01:08)**
- save-split: `S2a4-GO3-20261028-074003` baseline `coop-test/baselines/20261028-071340` (มติ [G-2]) · split EXACT 0/0/0 · PREFLIGHT READY 5/5 (07:45:23) · restore EXACT (01:08)
- เกม: PID 39720 เปิด 00:00:24 ปิด 01:06:22 (ปกติ) · ศึก 1 ครั้ง type=HardFight (เริ่ม 00:26:38 จบ 01:06:09 turn=16)

## ผลรายข้อ (เกณฑ์ §11 ตรงตัว)

| ข้อ | ผล | หลักฐาน/ตัวเลข |
|---|---|---|
| **G0** OnGUI ยิงจริง | **PASS** | หน้า title จอว่าง = ถูกต้องตามดีไซน์ (`ShouldRender = overlay && ring>0`; host ส่งเฉพาะในศึก) · เข้าศึก: ภาพถ่ายผู้ใช้แสดงกล่อง overlay วาดจริง (status + stateHash + ตารางยูนิต 7 แถว) · `sent` 0→97→625 |
| **E1** โหลด+ไม่พัง+จบศึก | **PASS** | mod โหลด (DLL=artifact) · ERROR/Exception = 12 รายการคงที่ตลอด = noise MelonLoader type-cache (`UnityEngine.UIElements`) **นับเท่ากันเป๊ะใน log เปิดเกมครั้งก่อน (build เก่า)** ⇒ ไม่มี error ใหม่จาก build นี้ · heartbeat `err=0` ตลอด · **จบศึกจริง** `battle:end type=HardFight state=End player=3 enemy=8 turn=16` · ปิดเกมสะอาด (`probe: unpatched before=1 after=0`) |
| **E2** overlay ตรงจริง ≥20 คู่ | **PASS** | **42/42 คู่** (7 ยูนิต × id/row/col/hp/alive/team) ณ key เดียวกัน (battleTime=0.524/turn 0/WaitPlayerInput) + active unit ตรง + เลขที่สอง = `maxHp` ตรง payload 7/7 · divA=0 · gaps=0 · resync 0/0 · ดู `e2-overlay-vs-status.md` |
| **E3** input isolation | **PASS** | ผู้ใช้เล่น 1 เทิร์นเต็มขณะ overlay เปิด · `commandsAccepted=0` ทุก status line ตลอด session · audit ของ IPC มีเฉพาะร่องรอยคำสั่งของ agent (E6 set_pref roundtrip + spike status อ่านอย่างเดียว) |
| **E4** ต้นทุนเฟรม | **PASS** | A/B/A2 ที่ศึกนิ่ง WaitPlayerInput (protocol v2: wall-clock+sha256 ทุกสลับ): mean(A∪A2)=7.685 vs mean(B)=7.929 ⇒ **Δmean = +0.244 ms ≤ 1.0** · ฝั่งบวก = ต้นทุน overlay วัดไม่ออกใน noise · หน้าต่างสะอาด A=1/B=1/A2=2 (สเปรดระหว่างหน้าต่าง 0.248 ms ⇒ ความละเอียด ~0.25 ms) · p50/p95 ติด bucket 6.945/13.888 (observational) · ดู `e4-e7-e8-framecost-and-savesafety.md` |
| **E5** guest อ่านอย่างเดียว | **PASS-with-limitation** | (a) = **NOT-RUN ทั้ง 4 กลุ่ม ตาม M5-3 — ไม่มี hook ให้ทดสอบ ห้ามเขียนว่า "hook ผ่าน"** · (b) `s2_static_check.py` = CLEAN (0 call sites) · (c) IPC audit ทั้ง session: `commandsAccepted=0`, คำสั่งที่ยิงมีเฉพาะ set_pref (E6, mod-state) + spike status (อ่าน) · (d) **ML log ไม่มีบรรทัด SP5/Sp5T0 install/patch ใด** (ข้อตรวจขั้น 6 — grep exit 1) · DLL มีโค้ด SP5 compiled แต่ `Sp5T0=false` ⇒ `Apply(false)` no-op (C1–C4) |
| **E6** set_pref hardening | **PASS** | ขณะ S2 active: `set_pref allowRemoteInput=true` → **rejected พร้อมเหตุผล** + `spike move` = DISARMED · ขณะ idle: รับแบบเดิม → คืน false ทันที · pref file ค่าจริง `AllowRemoteInput=false` · ดู `e6-set-pref-hardening.md` |
| **E7** เซฟปลอดภัย | **PASS** | `SaveSlot*` 55 ไฟล์ EXACT ตรวจ 3 จุดเวลา (กลาง session / หลังจบศึก+ปิดเกม / หลัง restore) · drift = `PlayerprefData.dat` อย่างเดียว (volatile ยกเว้นตามเกณฑ์) · เซฟจริง aside ไม่ถูกเขียน (`df8e5a11beed`) · **restore EXACT 0/0/0 + `leftovers: none` + rehash อิสระ 57/57** |
| **E8** สเกล | **PASS (บันทึกตามจริง)** | unitCount = 7 (player 3 + enemy 4) · grid 18×19 · **ข้อจำกัด: ไม่มี fixture 60 ยูนิต** (ค้าง Q9/§11.1 — ห้ามขยายผล) |

**ยอดรวม: 8 PASS · 1 PASS-with-limitation (E5) · 0 FAIL · 0 INCONCLUSIVE · 0 NOT-RUN(ยกเว้น E5(a) ที่ M5-3 กำหนด)**

## hash ปลายทาง (หลัง restore + คืนค่า)

| รายการ | ค่า | สถานะ |
|---|---|---|
| `Save` | 57 ไฟล์ digest `df8e5a11beed` + `leftovers: none` + EXACT ทั้ง copy/real baseline | ✓ ตรง baseline `20261028-071340` |
| `UserData\LongYinCoop\net.cfg` | **`12541f34…` 444 B (EXACT ค่าก่อนแก้)** | ✓ |
| `UserData\MelonPreferences.cfg` | `66fb92d5…` (ML เขียนใหม่ตอนปิดเกมหลัง pref dirty จาก E6 — **ค่า `AllowRemoteInput=false` ถูกต้อง**; hash เปลี่ยนจาก `269b571c…` เพราะ ML normalize — divergence บันทึกแล้ว) | ✓ ค่า / ⚠ hash เปลี่ยนจาก ML exit-save |
| `Mods\LongYinCoop.dll` / `.pdb` | `6a94abb5…` / `856d3bed…` (build ใหม่ค้างอยู่ — **รอมติ G-3**: rollback `.prev-1b4c17aa` หรือคงไว้) | รอผู้ใช้ |
| เกม 2 ไฟล์ | ไม่วัดซ้ำ (เกมไม่อัปเดตเอง — VERSION.md anchor ยังผูก) | — |

## กระบวนการ/เหตุการณ์ระหว่างทาง (ตามจริง)

1. recorder ครั้งที่ 1 ตาย 08:44 (`--wait 3600` หมด — ผู้ใช้เปิดเกมช้ากว่ากำหนด 3 ชม. 16 น.) → รีสตาร์ทหลังเกมเปิด ต่อสำเร็จ 00:02:25 · ครั้งที่ 2 จบสะอาด 01:06:22 ตามเกมปิด (`game exited`) · ไม่มีข้อมูลหาย (24,583+ samples, errors=0)
2. G0 รอบแรกตรวจผิดฝั่ง agent (บอกผู้ใช้หา "กล่องรอ" ซึ่งเป็นโค้ดตายตามดีไซน์) — แก้การตีความก่อนตัดสิน G0 แล้วพิสูจน์ด้วยการเข้าศึกจริง
3. `forbidden` counter 6→9: อธิบายได้ด้วยการวัด (self-test 1 ครั้ง/การ activate, +3 เสมอ) — by-design
4. preflight รอบแรก REFUSED ข้อเดียว (ไฟล์ out ของ recorder ยังไม่เกิด) → placeholder เปล่า (recorder เปิดแบบ truncate) → READY
5. ไม่มี NO-GO · ไม่มี crash · ไม่มี soft-lock · ไม่มีเซฟ hash เปลี่ยน · ไม่มี secret ลงเอกสาร

## สิ่งที่ยังไม่รู้ / ข้อจำกัด

- E4 ความละเอียด ~0.25 ms (4 หน้าต่างสะอาด) — ต่ำกว่านั้นต้องวัดซ้ำบล็อกยาว (อนุมัติแยก)
- E8 ไม่มี fixture 60 ยูนิต — ค้าง Q9
- การจับคู่ auth ต่อ activation ของ self-test (session id ไหนคือ selftest/overlay) ยังไม่แยกละเอียด — ไม่กระทบเกณฑ์
- MelonPreferences.cfg hash เปลี่ยนจาก ML exit-save (ค่าถูกต้อง) — ถ้าต้องการ hash เดิมเป๊ะต้องรอดูว่า ML เขียนซ้ำไหมในเปิดเกมครั้งถัดไป (ไม่บังคับ)

## ไม่ปิด stage / ไม่ tag

การปิด S2a-4 + tag `stage-S2a-4` = มติผู้ใช้แยก · G-3 (rollback/คง DLL) = รอคำตอบผู้ใช้
