# S1.5f — บันทึกการปิดเกม + restore เซฟจริง (2026-10-21)

## 1. ปิดเกม (RUNTIME)

| รายการ | ค่า |
|---|---|
| วิธีปิด | **ผู้ใช้ปิดเองในเกม** — ผมไม่สั่ง `Stop-Process`/`taskkill` ตามคำสั่ง |
| pid ที่เฝ้า | 42216 (`LongYinLiZhiZhuan.exe`, start 2026-10-21 02:48:29) |
| ตรวจครั้งแรก (03:12:19) | ยังรันอยู่ (`Responding=True`, heartbeat 03:12:17.958) → **หยุดรอ ไม่แตะเซฟ** |
| ตรวจครั้งที่สอง (03:13:01) | **pid 42216: GONE** · `tasklist /FI "IMAGENAME eq LongYinLiZhiZhuan.exe"` = *No tasks are running* |
| หลักฐานปิดใน log mod | `03:12:48.301 [INFO] probe: unpatched (owned patches before=1 after=0)` (MelonLoader ถอด patch ตอนปิด) · `lastWrite=03:12:48` |
| การเขียนเซฟตอนปิด | ไม่มีใน `SaveSlot*` — เทียบ copy baseline ได้ `changed=1` = `PlayerprefData.dat` เท่านั้น (`missing=0 added=0`) |

## 2. Sentinel (RUNTIME)

```
python tools\save-split.py sentinel unhide --label S1.5f-20261021-0247 --slot 8
  REFUSED: no sentinel record at ...\coop-test\S1.5f-20261021-0247\sentinel.json
```

- รอบนี้ **ไม่เคย hide sentinel** (ไม่มี `sentinel.json`, ไม่มีโฟลเดอร์ `.hidden-SaveSlot*` ใน `Save`) ⇒ ไม่มีอะไรต้อง unhide
- guard ของ `restore` (ถ้ามี `.hidden-SaveSlot8` จะ REFUSED) ไม่ทำงาน — ตรวจแล้วว่าไม่มี

## 3. Restore (RUNTIME)

```
python tools\save-split.py status          # ก่อน restore
  game     : not running
  Save                          files=57  digest=37933643ff0b     <- สำเนาที่ใช้ทดสอบ
  Save.real-S1.5f-20261021-0247 files=57  digest=2b0548fe464b     <- ของจริง (ไม่ถูกแตะ)
  leftovers: Save.real-S1.5f-20261021-0247
  copy drift vs savehash.copy.before.txt: changed=1 (PlayerprefData.dat) missing=0 added=0

python tools\save-split.py restore --label S1.5f-20261021-0247
  copy check: changed=1 (pref=1) missing=0 added=0
  rename  : Save -> Save.used-20261021-031418
  rename  : Save.real-S1.5f-20261021-0247 -> Save
  restored Save vs real baseline: changed=0 missing=0 added=0
  OK restore S1.5f-20261021-0247: 57 files
```

- เซฟจริงถูก **rename กลับ** ไม่ได้เขียนทับ (ไบต์เดิม 57/57)
- สำเนาที่ใช้ทดสอบเก็บไว้ที่ `coop-test/S1.5f-20261021-0247/Save.used-20261021-031418` (ไม่ได้อยู่ในโฟลเดอร์เกม)
- log: `logs/restore-20261021-031417.log`

## 4. ตรวจหลัง restore (RUNTIME)

| ตัวชี้วัด | ผล |
|---|---|
| `save-split.py status` | `game: not running` · `Save files=57 digest=2b0548fe464b` · `leftovers: none` |
| เทียบ baseline ที่บันทึกตอน split | **EXACT ทั้ง 8 baseline** (`S1.5c-originhp` ×2, `S1.5c-w2w4` ×2, `S1.5e` ×2, `S1.5f` ×2) |
| rehash อิสระ (PowerShell `Get-FileHash -Algorithm SHA256`, 57 ไฟล์) | `mismatch=0 missing=0 extra=0` · bytes `485,795,552` = baseline → **57/57 ตรง** |
| leftover ใน `Save` (`.hidden*`, `recreated-*`) | ไม่มี (`entries=13`, `hidden=0`) |
| โฟลเดอร์ `Save*` ใน `LongYinLiZhiZhuan_Data` | เหลือ **`Save` เดียว** — ไม่มี `Save.real-*` / `Save.used-*` ค้าง |
| panic switch ที่ค้างอยู่ | `MelonPreferences.cfg → [LongYinCoop] AllowRemoteInput = false` |

**สรุป: digest กลับมาตรง baseline `2b0548fe464b` = 57/57 ไฟล์ → PASS**

> หมายเหตุความถูกต้อง: ผมรัน `save-split.py verify` หลัง restore ด้วย ผลคือ `RESULT: FAIL` — **ไม่ใช่เซฟเสีย** แต่เป็น semantics ของ tool: `cmd_verify` ออกแบบไว้ *ก่อน* restore และบังคับเงื่อนไข "ต้องยังเหลือโฟลเดอร์ `Save.real-*`" (`src: tools/save-split.py` → `if is_real and not real_dir.is_dir(): FAIL`). หลัง restore โฟลเดอร์จริงถูก rename กลับเป็น `Save` แล้ว จึง FAIL ตามการออกแบบ · ตัวเลขเนื้อหาของมันเองยังเป็น `vs real baseline: changed=0 missing=0 added=0` และ `vs copy baseline: 0/0/0` · การยืนยันที่ใช้จริงคือ `status` (EXACT ×8) + rehash อิสระ (0 diff)
> ⇒ ควรแก้ถ้อยคำ/โหมดของ `verify` (เพิ่ม `--post-restore`) ในรอบถัดไป — บันทึกเป็นข้อบกพร่องของ tool

## 5. ไฟล์หลักฐาน

`logs/status-postclose-20261021-031310.log` · `logs/status-postclose-20261021-031327.log` · `logs/sentinel-unhide-20261021-031327.log` · `logs/restore-20261021-031417.log` · `logs/copy-drift-20261021-031410.log` · `logs/verify-postclose-20261021-031427.log` · `logs/rehash-independent-20261021-031510.log`
