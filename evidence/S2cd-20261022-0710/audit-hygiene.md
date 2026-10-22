# Phase A — Audit hygiene (2026-10-22)

ขอบเขต: ทำความสะอาด *ระเบียนที่ git track* โดยไม่แตะหลักฐานประวัติศาสตร์แม้แต่ไฟล์เดียว

## 1. สิ่งที่พบ

`REBASELINE.json` ทั้ง 9 ชุดมี absolute path + ชื่อผู้ใช้ในฟิลด์ `against`:

```
"against": "C:\\Users\\memph\\LongYinCoop\\coop-test\\baselines\\20261022-064205\\savehash.real.before.txt"
```

(REBASELINE = ไฟล์ที่ tool เขียนเองอัตโนมัติ; ไม่ใช่ไฟล์ที่ผมจด)

## 2. วิธีแก้ — sanitized export แทนการแก้ต้นฉบับ

| รายการ | ผล |
|---|---|
| ต้นฉบับ `REBASELINE.json` | **ไม่ถูกแก้** — sha256 ก่อน/หลัง export ตรงกันทั้ง 9 ไฟล์ (`sha256sum -c` = OK ×9) |
| ระเบียนใหม่ `AUDIT.json` | เขียนข้าง ๆ ต้นฉบับ, ฟิลด์ `against` เป็น **repo-relative** (`coop-test/baselines/.../savehash.real.before.txt`) |
| provenance | `source` + `source_sha256` (hash ของต้นฉบับ) + `generated_by` |
| determinism | ไม่มี timestamp ในไฟล์ ⇒ `export --check` = `stale: 0` (re-export ไม่ทำให้ git เปลี่ยน) |
| fail-closed | ถ้า sanitize ไม่หมด (เหลือ drive letter / `Users\<name>`) ⇒ error, ไม่เขียนไฟล์ |
| tool | `tools/coop-audit.py` (`export` / `export --dry-run` / `export --check` / `verify`) |

```
$ python tools/coop-audit.py export --check
records: 9  written: 0  stale: 0  up-to-date: 9
PASS: audit records match sanitized sources

$ python tools/coop-audit.py verify
checked 9 audit record(s)
PASS: no absolute path, drive letter or user name in audit records
```

## 3. สิ่งที่ git track (`.gitignore`)

```
coop-test/**
!coop-test/**/savehash.real.before.txt
!coop-test/**/savehash.copy.before.txt
!coop-test/**/savehash.*.txt.note
!coop-test/**/AUDIT.json          <- ใหม่ (แทน !REBASELINE.json)
!coop-test/**/APPROVAL.md
coop-test/**/save/
coop-test/**/Save.used-*/
coop-test/**/Save.real-*/
coop-test/**/__pycache__/
coop-test/**/REBASELINE.json      <- re-ignore: มี absolute path
coop-test/**/MANIFEST.json        <- re-ignore: absolute path + mtime 57 รายการ
```

ตรวจด้วย `git check-ignore` (2026-10-22 07:10):

| path | ต้องการ | ผล |
|---|---|---|
| `baselines/20261022-064714/AUDIT.json` | tracked | tracked ✅ |
| `baselines/20261022-064714/REBASELINE.json` | ignored | ignored ✅ |
| `baselines/20261022-064714/MANIFEST.json` | ignored | ignored ✅ |
| `baselines/20261022-064714/savehash.real.before.txt` | tracked | tracked ✅ |
| `area-20261022-0615/MANIFEST.json` | ignored | ignored ✅ |
| `area-20261022-0615/save/SaveSlot0/Save` (สำเนาเซฟ) | ignored | ignored ✅ |
| `area-20261022-0615/sampler.ndjson` | ignored | ignored ✅ |

`git ls-files coop-test` = **19 ไฟล์** (9 × `AUDIT.json` + 9 × `savehash.real.before.txt` + 1 × `APPROVAL.md`) ไม่มีไฟล์เซฟแม้ไบต์เดียว

## 4. git status / commit

| commit | เนื้อหา |
|---|---|
| `fc3c10d` | `.gitignore` + `tools/coop-audit.py` + 9 × `AUDIT.json` (12 ไฟล์, +302/−30) — git เห็นเป็น rename จาก `REBASELINE.json` (R050–R055) จึงตามประวัติต่อได้ |
| ไม่ staged | `src/LongYinCoop/**` (10 ไฟล์), `tests/**` (3 ไฟล์), `S2a-3-plan.md`, `evidence/S2a3-*`, ไฟล์ใหม่ของ S2a-3 (`S2ControlHandler.cs`, `S2FrameMetrics.cs`, …) — **งาน S2a-3 ทั้งหมด ไม่แตะ** |

## 5. หนี้ที่เหลือ (ยังไม่แก้ เพราะอยู่นอกขอบเขต Phase A)

`git ls-files | grep -F 'C:\Users'` = **36 ไฟล์** นอก `coop-test/` — ทั้งหมดเป็นหลักฐานประวัติศาสตร์ใต้ `evidence/**` + `README.md`:

```
9  evidence/S2a2-20261021-2254          7  evidence/S1.5-20261020-141903/logs
4  evidence/S2a2-20261021-2213-step0   4  evidence/S1.5b-20261020-144353/logs
2  evidence/S2a-1-20261021-preflight    2  evidence/S1.5f-20261021-0247/logs
1  evidence/rollbackB-20261022-0108     1  evidence/S2-gate-20261021-2050
1  evidence/S1.5gB-20261021-200449(+logs) 1  evidence/S1.5g-20261021-160331(+logs)
1  evidence/S1.5b-20261020-144353       1  README.md
```

ไม่ได้แก้เพราะคำสั่ง Phase A ข้อ 2 ห้ามแก้หลักฐานย้อนหลัง — ถ้าต้องการให้ปิดช่องนี้ต้องมีมติแยก (เช่น squash history หรือยอมรับว่าเป็นหลักฐานภายในเครื่อง)
