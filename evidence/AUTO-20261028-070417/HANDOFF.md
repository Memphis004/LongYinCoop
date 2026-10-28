# HANDOFF — session `AUTO-20261028-070417` (2026-10-28 07:04 เริ่ม — ปิด ~07:2x)

> ไฟล์ส่งมอบบนดิสก์ **ไม่ commit** (ตามคำสั่ง D2) · คำสั่งต้นทาง: ข้อความผู้ใช้ 2026-10-28 (ปิดมติ G-1/G-2/DEV-2 + เตรียม Release artifact S2a-4 GO-3 — offline เท่านั้น)

## สรุปผูกมัด 5 บรรทัด (อ่านก่อน)

1. **G-1 = หยุดตามเงื่อนไขที่ผู้ใช้กำหนดเอง — การแก้ using 1 บรรทัด ยังไม่ถูกทำ** — เงื่อนไขก่อนแก้ติด: `CoopMod.cs:53` เรียก `Sp5T0Probe.Apply(CoopPrefs.Sp5T0.Value)` จาก `OnInitializeMelon()` = **probe ถูกเรียกจากเส้นทางโหลดปลั๊กอิน** ตรงตัวอักษรของข้อความ *"ถ้า probe ถูกเรียกหรือผูก hook จากเส้นทางที่รันตอนโหลด = หยุด"* · ข้อเท็จจริงประกอบมติ: ณ pref default (`Sp5T0=false`, `Enabled` gate), `Apply(false)` → `Uninstall()` → `CloseSession` (return ทันที) = **ไม่สร้าง Harmony ไม่ติด patch ไม่เขียนไฟล์** — การตัดสินว่า "ถูกเรียก" พอหรือไม่ = สิทธิ์ผู้ใช้ · หลักฐาน `a1-a2-reachability.md` (ไฟล์เดียวกันนี้) + commit `d4fdf12`
2. **G-2 ปิดแล้ว** — rebaseline volatile-only ด้วย tool: baseline ใหม่ **`df8e5a11beed…` @ `coop-test/baselines/20261028-071340/savehash.real.before.txt`** (ต่างจาก `58883949…` @ `20261025-201545` เฉพาะ `PlayerprefData.dat` `50e1612b…`→`efd4ac5b…`; SaveSlot* 55 EXACT; ไฟล์เซฟไม่ถูกแตะ) · baseline เก่าไม่ถูกเขียน · commit `9f5c17a`
3. **DEV-2 ปิดแล้ว** — VERSION.md แก้บันทึก net.cfg เป็นค่าวัดจริง `12541f34…` 444 B โดยเก็บ `dc8e9098…` 454 B เป็น "เคยบันทึกเป็น" + divergence record (ตรงบันทึก AGENTS.md 2026-10-24) · ไฟล์ net.cfg จริงไม่ถูกแตะ · ไม่มี secret ในเอกสารใด
4. **Phase B = BLOCKED (ไม่รัน build)** — Release build จะล้ม CS0246 ตัวเดิมแน่นอน (`src/` ไม่เปลี่ยน 91c276b..ec78a8b ยืนยันว่าง; ไม่มีใครแก้ using) ⇒ ไม่มี artifact, ตรวจ 6 ชนิด = NOT-RUN · RUNBOOK-G3/provenance อัปเดตเป็นสถานะจริง (prereq 2 ปิด / prereq 1 ยัง BLOCKED) · commit `8ea8a90`
5. **GO-3 ยังเริ่มไม่ได้** — ต้องรอมติ G-1 รอบใหม่จากผู้ใช้ → แก้ using + build 0W/0E + artifact ครบก่อนพิมพ์ `เริ่ม GO-3 session` (split ขั้น 4 ของ RUNBOOK ชี้ baseline ใหม่ `20261028-071340` แล้ว)

## Commits (mod repo — เรียงตามเวลา)

| commit | สาระ |
|---|---|
| `d4fdf12` | Phase A evidence: G-1 tripwire TRIPPED (reachability) — ไม่มีการเปลี่ยน source |
| `9f5c17a` | Phase C: G-2 rebaseline (baseline ใหม่ + VERSION.md baseline section) + DEV-2 (net.cfg record) + evidence C2/C3/C4 |
| `8ea8a90` | Phase B docs: RUNBOOK-G3 + provenance = สถานะจริง (prereq 2 CLOSED, prereq 1 BLOCKED, เกณฑ์ E1–E8/G0/E6 ไม่ถูกแก้) |

- game repo: **ไม่มี commit ใหม่** (HEAD คง `246792e`) · ไม่มี tag/push/amend ทั้งสอง repo · ไม่มี add -A (ทุก commit ระบุ path รายไฟล์ + ตรวจ `git diff --cached --name-only` ก่อน commit)

## ผลตรวจรายขั้น

| ขั้น | ผล |
|---|---|
| session-start 6 ข้อ | **ผ่านครบ** — เกมไม่รัน (tasklist+PS+tool) · hash เกมตรง VERSION.md (`33c56def…`/`451311d4…`) · mod repo HEAD `ec78a8b` (M 2 ไฟล์ = หมายเหตุ uncommitted จาก session ก่อน — รวมเข้า commit `8ea8a90` แล้ว) + game repo HEAD `246792e` ตรง HANDOFF เดิม · เซฟ 57 ไฟล์ CHANGED=PlayerprefData.dat ไฟล์เดียว + SaveSlot* 55 EXACT + digest `df8e5a11beed` · mod 4 ไฟล์ (`1b4c17aa…`/`dcd78ebd…`/`269b571c…`/`12541f34…` 444 B) ตรง session ก่อน · ไม่มี ML log ใหม่หลัง `26-10-27_2-26-26.log` |
| A1 | commit "ยังไม่ build" ที่มี .cs ถูก csproj หลัก compile แต่ test ไม่รวม = **รายการเดียว** `7ff47ab` (SP5/Sp5T0Probe.cs) · ตัวอื่น docs-only หรือปิดไปแล้ว (S3 prereq → `f2ad30cd`) |
| A2 | **tripwire ติด** — caller 5 จุดใน CoopMod.cs (53 load path, 104 unload, 119/158–164 runtime poll) · รายละเอียด + พฤติกรรม Apply(false) ใน `a1-a2-reachability.md` |
| A3 | **ไม่ทำ** (หยุดรอมติ) — ไม่มีการแก้ source แม้ 1 บรรทัด |
| B1–B4 | **BLOCKED / NOT-RUN** — ไม่รัน build ที่รู้ผลล้ม, ไม่มี artifact, ตรวจ 6 ชนิด NOT-RUN |
| B5 | ทำเฉพาะส่วน "ตามผล": RUNBOOK prereq 2 ปิด + ขั้น 4 ชี้ baseline ใหม่; provenance คง BLOCKED + บันทึกเหตุ G-1; หมายเหตุ stray 4 บรรทัดถูกแทนที่ด้วยเนื้อหาจริง; ยืนยัน `git diff --stat 91c276b ec78a8b -- src/` ว่างก่อนแก้ |
| C1 | สำรอง `PlayerprefData.dat` → `evidence/AUTO-20261028-070417/PlayerprefData.dat.pre-rebaseline-20261028-0704` sha256 `efd4ac5b…0037` ตรงต้นทาง (**ไม่ commit** — ไฟล์เซฟ) |
| C2 | rehash อิสระ: ADDED=0 MISSING=0 CHANGED=[PlayerprefData.dat] · SaveSlot* 55 EXACT baseline เก่า ⇒ ผ่านเงื่อนไข G-2 |
| C3 | `save-split.py rebaseline --against 20261025-201545 --reason "G-2 …"` exit 0 → `baselines/20261028-071340/` (tool สร้างโฟลเดอร์ใหม่เสมอ — ตรวจโค้ด save-split.py:596–628 ก่อนรัน; `REBASELINE.json` volatile_only=true) |
| C4 | status: 57 ไฟล์ digest `df8e5a11beed` · leftovers none · rehash อิสระ **EXACT 57/57** กับ baseline ใหม่ (ตรวจซ้ำตอนปิด session ด้วย) |
| C5 | VERSION.md: หัวข้อ "Baseline ที่อนุมัติปัจจุบัน" + ประกาศ superseded ของ `20261025-201545` + หัวข้อ "Divergence record — net.cfg (DEV-2)" |
| D1 | hash ปลายทาง = ต้นทาง**ทุกตัว**: เกม 2 ไฟล์, Mods DLL/PDB, MelonPreferences.cfg, net.cfg (`12541f34…`), Save digest `df8e5a11beed` + EXACT 57/57 baseline ใหม่ · เกมไม่รันตลอด session |
| D2 | HANDOFF นี้ (ไม่ commit) |

## สิ่งที่ยังไม่รู้ / UNKNOWN

1. **มติ G-1 รอบใหม่** — ผู้ใช้ต้องตัดสิน: (α) ยืนยันใช้ทาง (ก) ต่อ (ยอมรับว่า probe "ถูกเรียก" แบบ no-op ณ default ไม่ขัด M5-3 เพราะไม่มี hook ติดจริง) / (β) เปลี่ยนไปทาง (ข) exclude SP5/** / (γ) เลื่อน GO-3 — agent ห้ามตัดสินแทน
2. พฤติกรรม `Apply(false)` ที่รายงานเป็นการอ่านจากซอร์ส (RUNTIME-พฤติกรรม) — **ยังไม่เคยรันจริงบนเกม** (ไฟล์นี้ไม่เคย compile สำเร็จ) — ป้ายความเชื่อมั่น: อ่านตรง ไม่มีทางแยก
3. Release artifact/ตรวจ 6 ชนิด = NOT-RUN จนกว่า build ผ่าน — ตัวเลขจริงยังไม่มี
4. AGENTS.md (game repo) ยังอ้าง baseline `58883949…` ในหัวข้อ 2026-10-28 = **ล้าสมัยแล้ว** (ปัจจุบัน `df8e5a11beed…` @ `20261028-071340`) — ไม่แก้เพราะนอกขอบเขตคำสั่ง session นี้; แนะให้แก้ใน session ถัดไปที่มีสิทธิ์แก้ AGENTS.md

## ข้อผิดพลาดกระบวนการ (รายงานตรง ๆ)

- สคริปต์เทียบเซฟด้วย shell `join` ครั้งแรกให้ผล false-positive (sort order locale) — ตรวจพบทันทีจากข้อความ "is not sorted" และคำนวณใหม่ด้วย Python ทุกครั้งที่รายงานผล · ผลที่อ้างอิงทั้งหมดมาจาก Python
- digest ad-hoc ตอนตรวจปลายทาง (`d116f2d391cd`) ต่างจาก digest ของ tool (`df8e5a11beed`) เพราะสูตรต่างกัน — ไม่ใช่การเปลี่ยนแปลงของเซฟ; ยืนยันด้วยการเทียบรายไฟล์ EXACT 57/57 และ digest ของ tool ก่อน/หลังเท่ากัน
- ไม่มีข้อผิดพลาดอื่น · ไม่มี incident (เกมไม่ถูกเปิด, ไม่มี process orphan, ไม่มีไฟล์ค้างใหม่นอก evidence ของ session นี้)

## hash ก่อน/หลัง (ต้องเหมือนกัน — ตรงหมด)

| รายการ | ก่อน (07:04) | หลัง (~07:2x) | เหมือนกัน |
|---|---|---|---|
| GameAssembly.dll | `33c56def…92c1` | เดียวกัน | ✓ |
| global-metadata.dat | `451311d4…bfd0` | เดียวกัน | ✓ |
| Mods\LongYinCoop.dll | `1b4c17aa…0177` | เดียวกัน | ✓ |
| Mods\LongYinCoop.pdb | `dcd78ebd…1e17` | เดียวกัน | ✓ |
| MelonPreferences.cfg | `269b571c…98d1` | เดียวกัน | ✓ |
| net.cfg | `12541f34…cc8` (444 B) | เดียวกัน | ✓ |
| Save digest (tool) | `df8e5a11beed` | `df8e5a11beed` | ✓ (+ EXACT 57/57 baseline ใหม่) |
| เกมรัน? | ไม่ | ไม่ | ✓ |
| mod repo HEAD | `ec78a8b` | `8ea8a90` (+3 commits) | — (ตามงาน) |
| game repo HEAD | `246792e` | `246792e` | ✓ |

## ขั้นถัดไปที่เสนอ (ผู้ใช้ตัดสิน)

1. **ตัดสิน G-1 รอบใหม่** (α/β/γ ด้านบน) — แนบมาในข้อความถัดไป
2. ถ้า (α): สั่ง "แก้ using + build + provenance ต่อ" → ทำ A3/B1–B4 ตามคำสั่งเดิม (เทสต์ 596/0 + 68 OK + CLEAN, ตรวจ exit code จริง, 6 ชนิด, artifact ลง S2a4-GO3-20261028-030025) แล้ว GO-3 พร้อม
3. ถ้า (β) หรือ (γ): รอคำสั่งเพิ่ม — ห้ามดำเนินการเอง
4. แนะเพิ่ม: อัปเดต AGENTS.md (game repo) บันทึก baseline ใหม่ + สถานะ session นี้ (ต้องสั่งแยก)
