# docs-baseline-update — รับ baseline `4226730f66ff` เป็นเกณฑ์ของ S2a-3 (เอกสารเท่านั้น)

อนุมัติ: ข้อความผู้ใช้ 2026-10-22 ("อนุมัติให้ใช้ baseline `4226730f66ff` (`coop-test/baselines/20261022-075422`) เป็น baseline สำหรับ S2a-3 ตามหลักฐาน restore-postcheck ที่ผ่าน 6/6")
รอบนี้ **ไม่แตะเซฟ · ไม่สร้าง baseline ใหม่ · ไม่ build/deploy · ไม่เปิดเกม**

## 1. commits

| repo | commit | เนื้อหา |
|---|---|---|
| mod `C:\Users\memph\LongYinCoop` | `5a1eb8a` | `VERSION.md`, `S2a-3-plan.md`, `coop-test/baselines/20261022-075422/APPROVAL.md` (3 ไฟล์ +53/−10) |
| game `<เกม>` | `70c1944` | `AGENTS.md` เท่านั้น (+11/−2) — ไฟล์ค้างของ session อื่นไม่ถูก commit |

## 2. สิ่งที่แก้

### `VERSION.md`
- §Area/UI: เปลี่ยนป้าย "baseline ปัจจุบัน (สุดท้าย)" → "baseline ณ ตอนนั้น (ถูกแทนที่ 07:54)" และ "ผลบังคับ (ประวัติ)" (ค่า `de7e2af49b23` ยังอยู่เป็นประวัติ)
- ป้าย "baseline ปัจจุบัน (หลัง incident 06:39)" → "baseline ณ ตอนนั้น (หลัง incident 06:39)" (กันอ่านผิดว่ายังใช้อยู่)
- **เพิ่ม §"Baseline ปัจจุบันของ S2a-3"**: digest เต็ม `4226730f66ff…46d7` · ไฟล์ `coop-test/baselines/20261022-075422/savehash.real.before.txt` · **คำสั่งบังคับ** `split --label <label> --expected-baseline coop-test\baselines\20261022-075422\savehash.real.before.txt` · เหตุผล (incident 07:48) · การอนุมัติ · หลักฐาน · ผลบังคับ (ห้าม rebaseline กลบ)

### `S2a-3-plan.md`
- เปลี่ยน `854cc890430f` → `4226730f66ff` 7 จุด (§0 "ไม่เปลี่ยน baseline", E7, X9, C6, C8, §11, §12)
- C6: `--expected-baseline coop-test\baselines\20261021-225856\...` → `…\20261022-075422\...`
- เพิ่มหมายเหตุ §7 (ก่อนรายการหลักฐาน) ระบุการเปลี่ยน baseline + ลิงก์หลักฐาน และย้ำ `854cc890430f` เป็นประวัติของ S2a-2

### `AGENTS.md` (game repo)
- L78 (Baseline เซฟที่อนุมัติ): ต่อท้ายด้วย baseline ปัจจุบันของ S2a-3 = `4226730f66ff` โดยคง `6dd645d9ef17` / `854cc890430f` ไว้
- §S2 ChangeDay: L103 ระบุว่า `de7e2af49b23` เป็นของ restore 07:31 และถูกแทนที่ 07:54
- **เพิ่ม §"สถานะปัจจุบัน (2026-10-22)"**: S2a-3 C1 PASS (421/0), C3 build 0/0 + sha256 `c51debef…ae96`, ยังไม่ deploy + เตือน `.off` ไม่ใช่ S2a-2, สถานะเซฟ = `4226730f66ff` EXACT + สำเนา `Save.used-20261022-142312`, baseline ที่อนุมัติ, `net.cfg` ยังไม่มี, ถัดไป = C4–C6 รออนุมัติ

### `coop-test/baselines/20261022-075422/APPROVAL.md` (ใหม่)
รูปแบบเดียวกับ `20261021-200450/APPROVAL.md`: สิ่งที่อนุมัติ · ขอบเขต · หลักฐานประกอบ (restore-postcheck, presave-check, restore-explore.txt, status)

## 3. ตรวจความสอดคล้อง (วัดจริงหลังแก้)

| ข้อ | ผล |
|---|---|
| digest ที่เขียนในเอกสาร = digest ของไฟล์ baseline | ✅ `4226730f66ffdcf9adb110fd8e26e29ffa8ba40620ccc33b013ef11adfaa46d7` (คำนวณด้วย `digest_of(read_hashfile(...))`, 57 ไฟล์) |
| path ที่อ้างมีจริง | ✅ `coop-test/baselines/20261022-075422/savehash.real.before.txt` |
| path ใน C6 มีจริง | ✅ เดียวกัน |
| ไม่มีคำว่า "baseline ปัจจุบัน" ที่ยังชี้ค่าเก่า | ✅ ค่าเก่าทั้งหมดอยู่ในบริบทประวัติ (ตรวจด้วย grep: `854cc890430f` เหลือ 1 จุดใน plan = หมายเหตุการเปลี่ยน; `de7e2af49b23` เหลือ 1 จุดใน VERSION = หัวข้อประวัติ) |
| เซฟไม่เปลี่ยนจากการแก้เอกสาร | ✅ `Save files=57 digest=4226730f66ff`, `leftovers: none` |
| DLL/pref ไม่ถูกแตะ | ✅ dll `cb35a93f…ceee0` · prefs `269b571c…098d1` · game not running |
| ไม่ commit ไฟล์ของ session อื่น | ✅ mod repo: tracked tree สะอาด; game repo: แก้เฉพาะ `AGENTS.md` (ของค้าง `D STAGEPLAN.md`, `D battle-notes.md`, `D worldmap-notes.md`, `?? Plans/`, `?? .opencode/`, `m il2cpp-frida-mcp` ยังอยู่เหมือนเดิม) |

## 4. ค้าง / ต้องตัดสินใจ

1. `coop-test/baselines/20261022-075422/savehash.real.before.txt` **ไม่ถูก ignore** แต่รอบนี้ commit เฉพาะ `APPROVAL.md` ตามคำสั่ง — ถ้าต้องการให้ baseline file อยู่ใน git ด้วย (เหมือนชุด `20261020-*`/`20261021-*` บางตัวที่ tracked) ต้องสั่งเพิ่ม
2. `REBASELINE.json` ของโฟลเดอร์นี้ถูก `.gitignore` (`coop-test/**` + ไม่มี negation) ⇒ อยู่บนดิสก์เท่านั้น
3. `evidence/explore-20261022-0755/phase1.md` L12 ยังเขียน "+`steam_autocloud.vdf`" ไม่ตรง `REBASELINE.json` (ของ session นั้น — ไม่แก้เอง)
