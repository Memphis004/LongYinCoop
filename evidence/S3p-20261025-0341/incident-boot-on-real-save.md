# Incident — เกมบูตบนเซฟจริงโดยไม่มี save split/preflight (2026-10-25 04:56)

Label: `S3p-20261025-0341` (ต่อเนื่อง) · สถานะเอกสาร: ร่างรายงานจากหลักฐานจริง — ไม่มีการแก้ save/baseline ใด

## 1. ลำดับเหตุการณ์ (OBSERVED)

| เวลา | เหตุการณ์ | หลักฐาน |
|---|---|---|
| 04:14:21 | (รอบก่อน) crash ระหว่าง w2 เคส 7 → hard stop → restore + rollback DLL เป็น `5d7b7cc5…` | `gate2-crash.md` |
| 04:56:32 | MelonLoader เริ่ม log session ใหม่ `26-10-25_4-56-32.log` | ชื่อไฟล์ log |
| 04:56:37 | mod boot: `stage=S2a-3 version=0.5.0` · pid **27872** · `IPC listening \\.\pipe\longyin-coop-27872` · `allowRemoteInput=False` | log บรรทัดที่ boot markers |
| (ก่อน 05:0x) | ผู้ใช้แจ้ง "เปิดเกมแล้ว" — **ขณะนั้นไม่มี split mounted (`leftovers: none`) และ DLL ที่โหลดคือ build เก่า** | save-split status + sha256 `Mods\LongYinCoop.dll` |
| ~05:0x | มติผู้ใช้เลือก (ค) hold runtime + save-watch read-only | แชต |

**OBSERVED:** เกมบูตโดยไม่มี save split/preflight (tool รายงาน `leftovers: none` ณ ตรวจแรก) และรันบน**เซฟจริง** · DLL ที่โหลด = `5d7b7cc5520ae784c5efc198dda8c1c3ba33ff3c55eb7aaa955ede5f63735953` (build เก่า S2a-3 — ไม่ใช่ build ทดสอบ `f2ad30cd…` ซึ่งพักอยู่ที่ `.s3prereq-f2ad30cd`)

## 2. ผลตรวจเซฟเต็ม 57 ไฟล์ สองทิศ สอง baseline (OBSERVED — ใช้ฟังก์ชันของ `tools/save-split.py` เอง)

สังเกต 2 จุดเวลา (T0, T0+12 s) ผล**นิ่งเหมือนกันทั้งคู่**:

| เทียบกับ | changed | missing | added | ไฟล์ที่เปลี่ยน |
|---|---|---|---|---|
| `S3p-20261025-0341/savehash.copy.before.txt` | 1 | 0 | 0 | `PlayerprefData.dat` |
| `baselines/20261024-232024/savehash.real.before.txt` | 1 | 0 | 0 | `PlayerprefData.dat` |

- digest ปัจจุบัน `693525697aab…` = สถานะ `7332fc93eb98…` + pref เปลี่ยน 1 ไฟล์ · **`SaveSlot*` ทุกไฟล์ตรง baseline** · key set 57 ไฟล์ตรง (ไม่มีเพิ่ม/หาย)
- hash ของไฟล์ที่เปลี่ยน (ตรวจซ้ำด้วย sha256sum ตรง ๆ ณ เวลาเขียนเอกสาร):
  - baseline: `0b7c753677ea87f74321dbdf946d29d81b9ec94dc1f4cc5301d83153182bd3bb`
  - ปัจจุบัน: **`dc4296f6ed27d8a83120b5de16781dbbfeb0a5f86c27a147148889989e16001a`**

## 3. ข้อผิดพลาดเดิมของสคริปต์ parse (บันทึกตามจริง — ไม่ปกปิด)

สคริปต์เปรียบเทียบที่เขียนเองครั้งแรก **parse baseline ผิด** เพราะไฟล์ baseline เป็น `sha256sum` output แบบ **CRLF** (`…*Save/SaveSlot0/Hero\r\n`) — การ split ด้วย `'  '` โดยไม่ตัด `\r` ทำให้ path ไม่ตรง → ผลแจ้ง "changed 56 ไฟล์" ที่ผิดทั้งชุด (ปรากฏในแชตรอบ Gate 2) · และการสรุป "เทียบครบแล้ว" ก่อนหน้านี้ใช้สคริปต์ที่ parse ผิดนั้น — **ไม่นับเป็นหลักฐาน**

**วิธีตรวจซ้ำที่ใช้ (และแนะนำให้ใช้ต่อไป):** เรียกใช้ฟังก์ชันของ `tools/save-split.py` เอง — `read_hashfile` (:97 ตัด `\r` ผ่าน `strip()`, `lstrip("*")`, ปรับ `\`→`/`, ตัด prefix `Save/`) + `hash_tree` + `diff_hashes` + `digest_of` — ซึ่งเป็นโค้ดเดียวกับที่ tool ใช้ตัดสินทุก stage · สคริปต์เขียนเองห้ามใช้ตัดสิน

## 4. การจำแนก

- **OBSERVED:** ทุกรายการใน §1–§2 (boot time, PID, log, DLL hash, ผลเทียบ 57 ไฟล์สองทิศสอง baseline, hash ของ pref)
- **INFER:** เกมถูกเปิดโดยผู้ใช้ (ผู้ใช้แจ้งเอง; agent ไม่มีคำสั่งเปิดเกม) · ยังไม่มีการเซฟในเกมเพราะ `SaveSlot*` ครบและผู้ใช้ยืนยันว่ายังไม่ได้เซฟ (INFER จากข้อมูลไฟล์ — ไม่ใช่ OBSERVED จนกว่าจะเฝ้าต่อเนื่อง)
- **UNKNOWN:** (ก) จะมีการเขียน `SaveSlot*` ตามมาหรือไม่ (ขึ้นกับการเล่นของผู้ใช้ต่อจากนี้) (ข) ทริกเกอร์การบูตที่แท้จริง (Steam UI/มือ — ตาม precedent forensic ครั้งก่อน ระบุ parent ไม่ได้) (ค) pref จะเปลี่ยนอีกก่อนปิดเกมหรือไม่

## 5. เสนอ rebaseline (volatile-only) — ~~ยังไม่รัน รออนุมัติ~~ → **ดำเนินการแล้ว ผลอยู่ใน §6**

- เหตุผล: drift เดียวคือ `PlayerprefData.dat` (volatile — เกมเขียนทุกครั้งที่เปิด/ปิด ตาม precedent S1.5g/S2a-2/S2a-3) · `SaveSlot*` ครบ · tool จะ**ยอมรับเฉพาะ volatile** และ**ปฏิเสธ (REFUSED) ถ้าไฟล์ non-volatile ต่าง** (`save-split.py:605`, `VOLATILE_TOP = {PlayerprefData.dat, steam_autocloud.vdf}` :37)
- คำสั่งที่เสนอ (หลังผู้ใช้ปิดเกม + อนุมัติ):
  ```
  python tools/save-split.py rebaseline \
    --against coop-test/baselines/20261024-232024/savehash.real.before.txt \
    --reason "S3p incident 2026-10-25 04:56: เกมบูตบนเซฟจริงโดยไม่มี split — pref drift volatile-only (SaveSlot* ครบตามผลเทียบ 57 ไฟล์ใน incident doc)"
  ```
- ผลที่คาด: baseline ใหม่ใน `coop-test/baselines/<stamp>/` · `digest` ใหม่ = `693525697aab…` (ณ เวลาวัดล่าสุด — ต้องวัดซ้ำตอนรันจริงเพราะ pref อาจเปลี่ยนอีกตอนปิดเกม) · `volatile_only: true` · **baseline เดิม `7332fc93eb98` ไม่ถูกลบ/แก้**
- ห้าม: เปลี่ยน baseline เอง, แก้/คืน save, รันขณะเกมเปิด

## 6. ผลจบ — rebaseline (volatile-only) สำเร็จ (มติผู้ใช้ "Authorization — Rebaseline PlayerprefData.dat Only" + ยืนยันจบจาก Post-Close Save Verification)

| รายการ | ค่า (OBSERVED — ตรวจจากไฟล์จริง) |
|---|---|
| รันเมื่อ | 2026-10-25 05:20:20 |
| baseline ใหม่ | `coop-test/baselines/20261025-052020/` (`savehash.real.before.txt` + `REBASELINE.json`) |
| --against | `coop-test/baselines/20261024-232024/savehash.real.before.txt` (ตามที่เสนอใน §5) |
| ผลเทียบ | changed=1 = `PlayerprefData.dat` · missing=0 · added=0 · **`SaveSlot*` ครบตาม §2** (UNKNOWN (ก) ปิดแล้ว: ไม่มีการเขียน SaveSlot เพิ่ม) |
| volatile_only | `true` · baseline เดิม `7332fc93eb98…` (232024) และ `4226730f66ff…` (075422) ไม่ถูกลบ/แก้ |
| hash ของ pref ใหม่ | **`5bc97eb2d9db7cf5b9e49141b89ae40162c407eab4f20ef3849810422ac62a19`** (จาก hashfile ของ baseline ใหม่ — ตรวจด้วย `grep PlayerprefData`) |
| digest ใหม่ | **`b7f67154cff613c618994fbf6cb0866a0a8b360bbcb557f5b977320f57ce61cf`** |

- **หมายเหตุความต่างจาก §5:** digest ตอนเสนอ (ยังไม่รัน) ประเมินจาก pref hash ณ ตอนนั้น = `693525697aab…` แต่ค่าจริงตอนรันคือ `b7f67154cff6…` — ตรงตามข้อความเตือนใน §5 เอง ("ต้องวัดซ้ำตอนรันจริงเพราะ pref อาจเปลี่ยนอีกตอนปิดเกม"): เกมเขียน pref อีกครั้งตอนผู้ใช้ปิดเกม · ไม่ใช่ความไม่สอดคล้อง — ทั้ง §2 (dc4296f6… ณ เวลาตรวจ) และค่าสุดท้าย (5bc97eb2…) เป็นสแนปชอตของ pref คนละเวลา
- UNKNOWN (ข) (ทริกเกอร์การบูต) ยังไม่ปิด — ตาม precedent forensic ระบุ parent ไม่ได้
- การใช้งานจริงของ baseline นี้ครั้งแรก: A/B test `AB-oldbuild-20261025-054139` (2026-10-25) — split บังคับ `--expected-baseline …/20261025-052020/savehash.real.before.txt` ผ่าน EXACT, หลัง restore ตรง EXACT + `leftovers: none` (`ab/ab-report.md`)
