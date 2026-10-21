# งาน B — exit 9 rollback test (`.dll.off`) · label `rollbackB-20261022-0108`

ทำ 2026-10-22 · ตอบ exit 9 ของ S2a-2 ที่ค้าง NOT-RUN · ลำดับงานที่อนุมัติ A → **B** → C

## 0. ขั้นตอน (ทำตามลำดับจริง)

1. session-start checks: hash เกมตรง `VERSION.md` ทั้งสองไฟล์ · เกมไม่รัน · mod repo `1372bc3` tree clean
2. `save-split split --label rollbackB-20261022-0108 --expected-baseline coop-test\baselines\20261021-225856\savehash.real.before.txt`
   → `changed=0 missing=0 added=0` · `Save` = สำเนา · จริงเก็บเป็น `Save.real-rollbackB-20261022-0108`
3. sampler `tools\s15d_sample.py --out coop-test\rollbackB-20261022-0108\sampler.ndjson --seconds 1800 --interval 0.5 --append --wait 900` (detached, pid 11284)
4. `save-split preflight --label … --sampler-pid 11284 --sampler-out …` → **PREFLIGHT READY 5/5**
5. สลับ DLL: `LongYinCoop.dll` (S2a-2 build `cb35a93f…ceee0`) → `LongYinCoop.dll.keep-S2a2`;
   `LongYinCoop.dll.off` (`517758147f59…2612`, build ก่อนหน้า) → `LongYinCoop.dll` (pdb สลับตาม)
6. **ผู้ใช้เปิดเกมเอง** 01:11:13 → ปิด 01:12:18 (65 s, ไม่เข้าศึก) · ผมไม่แตะเกม
7. `verify` → `restore --label` → คืน DLL → ตรวจ hash/prefs

## 1. ผลวัดทีละข้อ

| # | เกณฑ์ exit 9 | ผล | หลักฐาน |
|---|---|---|---|
| 1 | mod เก่าโหลดได้ | **PASS** | `MelonLoader\Logs\26-10-22_1-11-13.log`: `Melon Assembly loaded: '.\Mods\LongYinCoop.dll'` · `LongYinCoop v0.4.0` · mod log `=== LongYinCoop v0.4.0 stage=S1.5e-W7 pid=46908 ===` + `S1 loaded role=host … enabled=True probe=True ipc=True capture=True` + `IPC listening \\.\pipe\longyin-coop-46908` |
| 2 | เกมรันได้ปกติ | **PASS** | 65 s: heartbeat `ticks=7800 err=0 battle=0/0 tickP99=0.012ms` · ออกอย่างสะอาด `probe: unpatched (owned patches before=1 after=0)` · `IPC client disconnected` |
| 3 | build เก่าไม่มี S2 (คาดไว้) | **PASS** | session นี้ 23 log lines · `[S2]` = **0** · `UserData\LongYinCoop\S2\s2-snapshots.ndjson` mtime ยังเป็น 00:47:02 (ของ S2a-2) ไม่โต |
| 4 | ไม่แตะเซฟ | **PASS** | `verify`: mounted copy `changed=1 (playerpref=1)` = `PlayerprefData.dat` เท่านั้น · real folder `changed=0 mtime/size drift 0` · หลัง `restore`: `Save` 57 ไฟล์ `854cc890430f` **EXACT** ทุก baseline (copy/real ของ `rollbackB` + `S2a2`), `leftovers: none` · `SaveSlot*` mtime ยังเป็น 2026-10-21 22:54 (ก่อนหน้า) |
| 5 | คืน DLL ได้ | **PASS** | `LongYinCoop.dll` = `cb35a93f617b…ceee0` · `.off` = `517758147f59…2612` · ไม่มี `*.keep-S2a2` ค้าง |
| 6 | pref | **PASS** | sha256 `269b571c…098d1` เท่าเดิม (ML เขียนซ้ำตอนออกเกม 01:12:17 แต่เนื้อหาเดิม) |

ข้อสังเกต: `Can't cache type named … UnityEngine.UIElementsModule … TypeLoadException` = **12 บรรทัดทั้งรอบนี้และรอบ S2a-2 (00:31)** ⇒ เป็นเสียงรบกวนเดิมของ MelonLoader/IL2CPP ไม่เกี่ยวกับ mod หรือการ rollback

## 2. ข้อมูลประกอบ

- sampler ได้ **123 samples / 111,708 bytes** (≈ 62 s ที่ 0.5 Hz) — ยืนยันว่า IPC pipe ของ build เก่าทำงานจริง ไม่ใช่แค่ log
- `Save.used-20261022-011259` = สำเนาทดสอบของรอบนี้ (เก็บไว้)

## 3. สถานะหลังงาน B

- active DLL = `cb35a93f…ceee0` (S2a-2 build — build ที่ stage `stage-S2a-2` วัดจริง) · `.off` = build ก่อนหน้า
- **ยังไม่ได้ build/deploy** build ที่มี `net.cfg` (งาน A) — ยังไม่ได้ขออนุมัติ
- ไม่มีไฟล์ `net.cfg` ในเครื่องเกม ⇒ build ถัดไปจะยัง idle จนกว่าจะสร้างไฟล์ (fail-closed)
- เซฟ: `Save` = ของจริง 57 ไฟล์ `854cc890430f` · `leftovers: none`
- prefs: `269b571c…098d1` = สถานะก่อนทดลอง

## 4. สรุป

**exit 9 = PASS** ⇒ S2a-2 กลายเป็น **8 PASS/PASS-with-limitation · 1 NOT-RUN (exit 5 ไม่มีศพ) · 0 FAIL**
(การอัปเดตนี้ไม่ย้าย tag `stage-S2a-2` — tag ครอบ commit `14757e3` ตามที่ปิด stage ไปแล้ว;
หลักฐานชุดนี้อยู่ commit ถัดมา)
