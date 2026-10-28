# Session-start checks — label `AUTO-20261028-023904`

- **วัน-เวลาเครื่องจริง:** 2026-10-28 02:39:04
- **ผู้รับคำสั่ง:** agent ต่อเนื่อง (งาน Phase A–D ตามคำสั่งผู้ใช้ 2026-10-28)
- **คำสั่งที่ใช้ทั้งหมด = อ่านอย่างเดียว** (tasklist, Get-FileHash, save-split status, python diff hash ของไฟล์เซฟ) — ไม่มี split/restore/rebaseline, ไม่เขียนไฟล์เซฟ/pref ใด

## ผลตรวจรายข้อ

| # | ข้อ | ผล | สรุป |
|---|---|---|---|
| 1 | เกมไม่รัน | `tasklist` = ไม่พบ `LongYinLiZhiZhuan.exe` · save-split status ขึ้น `game: not running` | **PASS** |
| 2 | hash เกมตรง VERSION.md | GameAssembly.dll = `33c56def4102844a1190eec963a05c834cfcf992d42b423d485ce8584d7920c1` · global-metadata.dat = `451311d4dc8c1b8b5073905c8b39a9781bd7e1f44ed88fbf7b35f5345c18bfd0` | **PASS** (ตรง VERSION.md ทั้งคู่) |
| 3 | git mod repo | branch `master` · HEAD `390e856` (commit "plans/notes/diffs ตามรายการผู้ใช้ 2026-10-28") · `git status --short` = 53 รายการ **untracked เท่านั้น** ไม่มี modified/deleted ของ tracked (สแนปช็อตเก็บไว้เทียบตอนจบ) | **PASS** |
| 4 | save-split status | `Save files=57 digest=df8e5a11beed` · `leftovers: none` · ตรวจ diff รายไฟล์กับ baseline ที่อนุมัติ `coop-test/baselines/20261025-201545/savehash.real.before.txt` (digest `58883949…`): **CHANGED = `Save/PlayerprefData.dat` ไฟล์เดียว** (baseline `50e1612b…` → ปัจจุบัน `efd4ac5b…`) · ADDED=0 · MISSING=0 · **`SaveSlot*` ทั้ง 55 ไฟล์ EXACT** | **ไม่ผ่าน (DEVIATION-1)** — รายละเอียดด้านล่าง |
| 5 | hash ไฟล์ mod | `Mods\LongYinCoop.dll` = `1b4c17aa6a934b25c884f62dbab59a795a8bbadf60fd2fc869ee0927fb3f0177` (316,416 B) **ตรง VERSION.md** · `UserData\MelonPreferences.cfg` = `269b571c1b1572b7b525cda3e8a27e09b732b8eb31bdfe290269c8ed62a098d1` **ตรง** · `UserData\LongYinCoop\net.cfg` = `12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8` **444 B** | **ไม่ผ่าน (DEVIATION-2)** — รายละเอียดด้านล่าง |
| 6 | ML log ใหม่ | log ใหม่สุด = `26-10-27_2-26-26.log` (บูต 02:26:26 ปิด 02:30:35 สะอาด — heartbeat err=0, `probe: unpatched`, Preferences Saved) — **เก่ากว่า session ก่อน (T5 audit 2026-10-28 00:37)** จึงไม่ใช่ log ใหม่หลังการตรวจครั้งก่อน · บูต 3 ครั้งบน 2026-10-27 (00:47, 02:16, 02:26) = session เกมของผู้ใช้ · ERROR/Exception 12 เส้น = `Can't cache type … UIElements TypeLoadException` ตอนบูตของ ML/interop (noise ที่รู้จัก ไม่ใช่จาก mod) | **PASS** (พร้อมหมายเหตุ) |

## DEVIATION-1 — digest เซฟไม่ตรง baseline ปัจจุบัน (volatile-only)

- digest ของ `Save` ที่ mount อยู่ = `df8e5a11beed…` ≠ baseline ที่อนุมัติ `588839490e5b418ae6e20172ea7af21916c9afa3a935c99525984fc9e16a0485` (`coop-test/baselines/20261025-201545`)
- ต่างกัน **เฉพาะ `Save/PlayerprefData.dat`** (volatile) — `SaveSlot*` ครบ 55 ไฟล์ตรง baseline **EXACT**, ไม่มีไฟล์เพิ่ม/หาย
- สาเหตุที่ตรวจพบ: เกมถูกเปิด/ปิดโดยผู้ใช้ 3 ครั้งบน 2026-10-27 (ML logs 00:47 / 02:16 / 02:26 — ปิดครั้งสุดท้าย 02:30:35 สะอาด) ⇒ pref drift เป็นรูปแบบที่พบซ้ำทุกครั้งที่เปิดเกม (RUNTIME เดิม: `PlayerprefData.dat` เขียนทุกครั้งเปิด/ปิด)
- **การจัดการ:** เป็นเงื่อนไขหยุดตามคำสั่งผู้ใช้ → รายงานในแชต + HANDOFF · **ห้าม rebaseline เอง** ([N2] — rebaseline volatile-only ต้องมีมติผู้ใช้) · งานที่เหลือของ session นี้ (Phase A–D = เอกสาร/เทสต์ offline/Release build ไม่ deploy) **ไม่ขึ้นกับสถานะเซฟ** จึงดำเนินต่อได้ตามกติกา "ข้ามไป Phase ที่ไม่ขึ้นกับสิ่งนั้น"
- **ผลบังคับ:** GO-3 ของ S2a-4 (RUNBOOK-G3) จะ REFUSE ที่ขั้น split จนกว่าจะมีมติ rebaseline volatile-only หรือผู้ใช้ restore ให้ digest ตรง baseline — ใส่ไว้ใน RUNBOOK-G3 เป็นเงื่อนไขหยุดชัดเจนแล้ว

## DEVIATION-2 — net.cfg hash/ขนาดไม่ตรงบันทึกใน VERSION.md

- วัดจริง 2026-10-28: `UserData\LongYinCoop\net.cfg` = sha256 `12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8` · **444 B** · เนื้อหา flat key = value (`enabled=false`, `heartbeatMs=1000`, `transport=tcp`, `bind=127.0.0.1`, `port=47800`, `sendHz=30`) — **ค่า secret ที่อยู่ในไฟล์ไม่ถูกคัดลอกลงเอกสารใด** (กติกา anti-leak)
- บันทึกใน `VERSION.md` (ตาราง "Mod build ที่ deploy ล่าสุด") = `dc8e9098…61b61` · 454 B — **ไม่ตรงกับไฟล์จริงวันนี้**
- hash `12541f34c633…` **ตรงกับบันทึกใน AGENTS.md หัวข้อสถานะ 2026-10-24 03:2x** (ซึ่งเขียนขนาด 454 B — สรุปว่าเลขขนาดในรายงานเก่าขัดกัน 444/454 B ตามที่ผู้ใช้ระบุ; **ขนาดจริงที่วัด = 444 B**)
- **การจัดการ:** ห้ามแก้ net.cfg ([N3]/ข้อ 10) · ห้ามแก้ VERSION.md ในรอบนี้ (การอัปเดต VERSION.md = งานเอกสารที่ต้องอนุมัติแยก ตาม S2a-4-plan §9) — บันทึกความจริงที่วัดไว้ใน HANDOFF + AGENTS status (A4) และขึ้นทะเบียนเป็นคำถามค้างให้ผู้ใช้ตัดสิน (คง baseline บันทึกเดิม + divergence record หรืออนุมัติอัปเดต VERSION.md)

## สถานะก่อนเริ่มงาน (สำหรับเทียบตอนจบ)

- mod repo: HEAD `390e856` (master) — untracked 53 รายการ (รายการเต็มอยู่ใน `git status --short` ตอนนั้น; กลุ่มหลัก = coop-test/* ดิบ, evidence/S3p-*, evidence/TIMEstatic-*/bodies+TSV ใหญ่, evidence/sp0sp7-*, evidence/SP5body-*)
- game repo: HEAD `c672dc2` (main) — M STAGEPLAN.md, D battle-notes.md, D worldmap-notes.md, m il2cpp-frida-mcp, untracked เดิม (ไม่แตะ)
- `Mods\LongYinCoop.dll` `1b4c17aa…` · `.pdb` ไม่ได้วัดแยกในรอบนี้ (ไม่มีคำสั่งให้ตรวจ pdb ตอนเริ่ม — วัดตอน Phase B เทียบบันทึก VERSION.md `dcd78ebd…`)
- เกมไม่รันตลอด session · ไม่เปิด IPC · ไม่ deploy อะไร
