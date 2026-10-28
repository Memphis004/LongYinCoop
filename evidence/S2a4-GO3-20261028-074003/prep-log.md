# prep-log — S2a4-GO3-20261028-074003 (RUNBOOK-G3 ขั้น 0–4)

วลีสั่งงาน: `เริ่ม GO-3 session` (ผู้ใช้ 2026-10-28 — อนุมัติ [N1] deploy/swap ใน Mods\ + [N2] split/restore เฉพาะตาม RUNBOOK) · มติแนบ: [G-1] ปิด (636c209/1359217) · [G-2] baseline `20261028-071340` · [DEV-2] ปิด · [G-3] เก็บถามท้ายขั้น 8 · [G-4] = อนุมัติวลี

## ขั้น 0 — session-start (ผ่านครบ)

| ข้อ | ผล |
|---|---|
| 1 เกมไม่รัน | OK (PowerShell count=0) |
| 2 hash เกม | `33c56def…92c1` / `451311d4…bfd0` ตรง VERSION.md |
| 3 mod HEAD | `13592173aa8885ef817a7a895cdc818d1ab1fd88` = `1359217` ตรงที่กำหนด · tracked-modified = 0 · `git diff --stat 636c209 HEAD -- src/` ว่าง |
| 4 save-split status | 57 ไฟล์ digest `df8e5a11beed` ตรง baseline `20261028-071340` · `leftovers: none` |
| 5 mod ไฟล์ | DLL `1b4c17aa…` / PDB `dcd78ebd…` / MelonPreferences `269b571c…` / net.cfg `12541f34…` 444 B — ตรงหมด |
| 6 ML log | ใหม่ล่าสุด = `26-10-27_2-26-26.log` (ไม่มี log ใหม่) |
| หมายเหตุ | โฟลเดอร์ `evidence/S2a4-GO3-20261028-0655` ไม่มีอยู่จริง (มีแค่ `030025`) — ไม่ลบ/ย้าย evidence อื่น |

## ขั้น 1 — artifact + provenance (ผ่าน)

- `evidence/S2a4-GO3-20261028-030025/LongYinCoop.dll` = `6a94abb579e65d5e6b0af917dfed4ba6f5350ad76847190bd7263cda3077b334` (359,936 B)
- `evidence/S2a4-GO3-20261028-030025/LongYinCoop.pdb` = `856d3beda727218823170f861f532509c5458eb91e51c79f07c0452d3b196035` (97,128 B)
- ตรงตาราง provenance.md (READY) เป๊ะ · HEAD = provenance commit

## ขั้น 2 — สำรอง + deploy (ผ่าน)

| เวลา (wall-clock) | เหตุการณ์ | hash |
|---|---|---|
| 07:40:28 ก่อนสำรอง | ชื่อ `.prev` ว่างทั้งคู่ (ตรวจแล้ว) | DLL `1b4c17aa…` / PDB `dcd78ebd…` |
| 07:40:28 หลังสำรอง | `Mods\LongYinCoop.dll.prev-1b4c17aa` + `LongYinCoop.pdb.prev-dcd78ebd` | prev hash ตรงของเดิมเป๊ะ |
| 07:40:28 หลัง deploy | คัดลอก artifact → `Mods\` | **DLL `6a94abb5…` / PDB `856d3bed…` ตรง artifact เป๊ะ** ✓ |

## ขั้น 3 — net.cfg (protocol v2: wall-clock + sha256 ก่อน/หลังทุกแก้)

| เวลา | sha256 | การแก้ |
|---|---|---|
| 07:40:37 (ก่อน) | `12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8` (444 B) — ตรงสถานะอนุมัติ "พัก" | — |
| 07:41:12 (หลัง) | `5ce166da52fbbcd031495f6f8e974660b5d9cc7e21205947aad6c1733d50a5a5` | `enabled` false→true · **เพิ่ม `overlay = true`** (M3) · role=host / transport=tcp / bind=127.0.0.1 / port=47800 / sendHz=30 / heartbeatMs=1000 อยู่ที่ค่าถูกต้องอยู่แล้ว (ไม่แก้) · `secret` + `allowNonLoopback=false` คงเดิม (ค่า secret ไม่ถูกพิมพ์/log ที่ใด) |

- แก้ด้วยสคริปต์ Python รายบรรทัด (regex เฉพาะ key `enabled`) + แทรกบรรทัด overlay หลัง heartbeatMs — รักษา format เดิมทั้งไฟล์
- ปิด session ต้องคืนให้ได้ `12541f34…` กลับ (ขั้น 8)

## ขั้น 4 — split + preflight (ผ่าน)

```
python tools/save-split.py split --label S2a4-GO3-20261028-074003 \
  --expected-baseline coop-test/baselines/20261028-071340/savehash.real.before.txt
  → Save vs baseline: changed=0 missing=0 added=0 (EXACT) · exit 0
  → real kept as Save.real-S2a4-GO3-20261028-074003 · copy mounted
```

- preflight รอบแรก (07:44:58) = REFUSED ข้อเดียว: `sampler out` ยังไม่มีไฟล์ (recorder สร้างไฟล์เมื่อ connect กับ pipe — ซึ่งต้องมีเกมก่อน) → สร้างไฟล์ placeholder เปล่า (recorder เปิดแบบ "w" truncate เมื่อ connect — ปลอดภัย) → preflight รอบสอง:
- **07:45:23 — PREFLIGHT READY - you may launch the game (exit 0, 5/5):** real-aside OK (`df8e5a11beed`) · mounted-is-copy OK (0/0/0) · leftovers = `Save.real-S2a4-GO3-20261028-074003` ตัวเดียว · sampler OK (pid 26408, s15d_sample.py) · prefs-disarmed OK (6 keys=false)
- recorder: `python tools/s15d_sample.py --out evidence/S2a4-GO3-20261028-074003/sampler.ndjson --wait 3600 --seconds 7200 --interval 0.1` (pid 26408, background — รอ pipe เกม)
- savehash คัดลอกแล้ว: `savehash.real.before.txt`, `savehash.copy.before.txt` อยู่โฟลเดอร์นี้

## ขั้น 5 — รอผู้ใช้เปิดเกม (07:45)

แจ้งผู้ใช้แล้ว: "เปิดเกมได้" — agent ไม่แตะเกม/ไม่เปิด IPC client อื่น (recorder ถือ pipe · overlay ใช้ TCP guest slot เดียวตาม M4)
