# S2a-1 — preflight guard (`tools/save-split.py`)

สถานะ: **commit แล้ว** · ไม่เปิดเกม · ไม่แตะเซฟ · ไม่ build/copy DLL
ขอบเขต: subcommand ใหม่ `preflight` ในไฟล์เดียว (addition ล้วน ไม่แก้ subcommand เดิม)

## สิ่งที่ตรวจ (fail-closed)

| # | check | ผ่านเมื่อ |
|---|---|---|
| 1 | `real-aside` | โฟลเดอร์ `Save.real-<label>` ยังอยู่ และ digest ตรง `MANIFEST.real.digest` |
| 2 | `mounted-is-copy` | `Save` ตรง `savehash.copy.before.txt` (อนุญาตเฉพาะ volatile: `PlayerprefData.dat`, `steam_autocloud.vdf`) + ไม่มี hidden sentinel ค้าง |
| 3 | `leftovers` | มี leftover แค่ `Save.real-<label>` ตัวเดียว |
| 4 | `sampler` | pid มีจริง, command line มีชื่อ sampler script ที่รู้จัก, และ `--sampler-out` มีไฟล์แล้ว |
| 5 | `prefs-disarmed` | `[LongYinCoop]` มีอยู่ และ 6 key ไม่ถูกarm |

## prefs semantics (มติ ผู้อนุมัติ 2026-10-21)

- key **ไม่มี** ใน section → **WARN** (ถือว่า default = false ปลอดภัย) ไม่ FAIL
- key มีค่าเป็น `true/1/yes/on` (หรือค่าที่ไม่ใช่ falsey) → **FAIL**
- section `[LongYinCoop]` ไม่มีเลย → **FAIL** (ตรวจไม่ได้ = ไม่ผ่าน)
- `--sampler-out` เป็น **required** (ไม่ใส่ = argparse exit 2)

## ผลทดสอบ

- `preflight-sandbox-test.py` → `preflight-sandbox-output.txt`: **16/16 PASS**
  (T14 missing key → WARN + READY, T15 section missing → REFUSED, T16 ไม่ใส่ `--sampler-out` → exit 2)
- สภาพจริง (เซฟจริง mount, ไม่มี sampler) → `preflight-real-condition-output.txt`: **REFUSED** ถูกต้อง
  (`real-aside` missing + `leftovers: none` + sampler not running + `prefs-disarmed: 6 keys=false`)
- ค่า prefs จริง → `real-prefs-keys.txt`: section อยู่บรรทัด 1, 6/6 key = false

sandbox อยู่ที่ `%TEMP%\preflight-sbx-*` (ไม่ใช้เซฟจริง) — สคริปต์ทดสอบเก็บไว้ในโฟลเดอร์นี้เพื่อรันซ้ำได้
