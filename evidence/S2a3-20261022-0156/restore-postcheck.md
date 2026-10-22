# restore-postcheck — คืนเซฟ `explore-20261022-0755` (อนุมัติโดยผู้ใช้ 2026-10-22)

คำสั่งที่รัน (ครั้งเดียว):
```
python tools\save-split.py restore --label explore-20261022-0755
```

เอาต์พุตจริง (เต็มใน `restore-explore.txt`):
```
copy check: changed=1 (pref=1) missing=0 added=0
rename  : Save -> Save.used-20261022-142312
rename  : Save.real-explore-20261022-0755 -> Save
  restored Save vs real baseline: changed=0 missing=0 added=0
OK restore explore-20261022-0755: 57 files, copy kept at ...\coop-test\explore-20261022-0755\Save.used-20261022-142312
real folder now at ...\LongYinLiZhiZhuan_Data\Save (renamed back, never rewritten)
exit code: 0
```

## post-check 6 ข้อ (ตาม `presave-check.md` §3)

| # | เงื่อนไข | ผล | หลักฐาน |
|---|---|---|---|
| 1 | `status` = `Save` 57 ไฟล์ digest `4226730f66ff`, `leftovers: none` | **PASS** | `Save files=57 digest=4226730f66ff` · `leftovers: none` · `EXACT` ทั้ง `savehash.copy.before.txt` และ `savehash.real.before.txt` |
| 2 | rehash **อิสระ** ของ `Save` = `4226730f66ff…`, changed=0 vs `baselines/20261022-075422` | **PASS** | เครื่องมืออิสระ (PowerShell ordinal digest): `4226730f66ffdcf9adb110fd8e26e29ffa8ba40620ccc33b013ef11adfaa46d7`, 57 ไฟล์, changed=0 missing=0 added=0 |
| 3 | `restore-<ts>.json`: `ok:true`, 57 ไฟล์, digest ถูก | **PASS** | `restore-20261022-142313.json` = `{"restored_files":57,"digest":"4226730f66ff…","used_dir":"Save.used-20261022-142312","ok":true}` |
| 4 | สำเนาถูกเก็บครบที่ `coop-test/explore-20261022-0755/Save.used-20261022-142312` | **PASS** | มีจริง 57 ไฟล์ · digest `2e593e635ad9b588956720538a7587ad26b32c80695254c7e8a44e7a3bfa5251` (ตรงกับสำเนาก่อน restore เป๊ะ, drift `PlayerprefData.dat` ไฟล์เดียวตามที่วัดไว้) |
| 5 | `history.ndjson` มี event `restore` | **PASS** | `{"ts":"2026-10-22T14:23:13","event":"restore","used_dir":"Save.used-20261022-142312","files":57}` |
| 6 | ไม่มี `Save.real-*` / `Save.used-*` ค้างใน `LongYinLiZhiZhuan_Data` | **PASS** | มีเฉพาะ `Save` |

**สรุป: post-check ผ่าน 6/6 · digest ตรง `4226730f66ff` · ไม่ต้อง rebaseline**

## สิ่งที่ไม่ได้ทำ (ตามข้อกำหนด)

- ไม่เปิดเกม · ไม่ build · ไม่ deploy (`Mods\LongYinCoop.dll` ยังเป็น `cb35a93f…ceee0` = build S2a-2, `lastWrite` 2026-10-21 22:55:43)
- ไม่แก้เอกสาร (`VERSION.md`, `AGENTS.md`, `S2a-3-plan.md`) และไม่แตะ baseline
- ไม่แตะ `MelonPreferences.cfg` (sha256 `269b571c…098d1` เท่าเดิม) และ `net.cfg` ยังไม่มี
