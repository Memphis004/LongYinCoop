# S1.5gB · Save verification (สำเนาทดสอบ + restore 57/57)

label `S1.5gB-20261021-200449` · เกมปิดก่อน/หลังทุกขั้น · **ไม่มีการกด Save ในเกม** · mod ไม่เรียก Save/Load

---

## 1. ก่อนทดสอบ [RUNTIME]

| รายการ | ค่า |
|---|---|
| สภาพจริงตอนเริ่ม | 57 ไฟล์ digest `6dd645d9ef17` (หลัง rebaseline 20:04:50 จาก pref drift ของเหตุการณ์ 19:49) |
| baseline ที่ใช้ | `coop-test/baselines/20261021-200450/savehash.real.before.txt` · `REBASELINE.json` ระบุ `volatile_only: true`, `changed: ["PlayerprefData.dat"]` |
| เกม | ปิด (`require_game_closed` ผ่าน) · anchor ตรง `VERSION.md` · DLL `5177581…` |

## 2. Split (G-0) [RUNTIME]

`logs/split.log`:
```
expect  : coop-test\baselines\20261021-200450\savehash.real.before.txt
  Save vs baseline: changed=0 missing=0 added=0
rename  : Save -> Save.real-S1.5gB-20261021-200449
copy    : coop-test/S1.5gB-20261021-200449/save -> Save
  copy vs real: changed=0 missing=0 added=0
OK split S1.5gB-20261021-200449: 57 files
```
`logs/status-post-split.log`: `Save` 57 ไฟล์ digest `6dd645d9ef17` · `Save.real-…` 57 ไฟล์ digest เดียวกัน · `EXACT` ทั้ง `savehash.copy.before` และ `savehash.real.before`

## 3. หลังทดสอบ — ใครเขียนอะไร [RUNTIME]

`logs/verify-postclose.log` (เกมปิดแล้ว):
```
mounted Save:  vs real baseline: changed=1  (changed[playerpref]=1: PlayerprefData.dat)
               vs copy baseline: changed=1
real folder Save.real-S1.5gB-20261021-200449:
               vs real baseline: changed=0 missing=0 added=0
               mtime/size drift: 0
RESULT: PASS
```

| ข้อ | ผล |
|---|---|
| เกมเขียนลงสำเนา | **ใช่** — digest `6dd645d9ef17` → `854cc890430f` |
| เขียนอะไร | `PlayerprefData.dat` **ไฟล์เดียว** · `slot_drift = 0` |
| `SaveSlot*` | **ไม่ถูกเขียน** ตลอด 11.6 นาที (ผู้ใช้ไม่กด Save) |
| โฟลเดอร์ของจริง | `changed=0`, `mtime/size drift=0` — ไม่ถูกแตะ |

## 4. Restore + ตรวจซ้ำ [RUNTIME]

`logs/restore.log`:
```
copy check: changed=1 (pref=1)
rename  : Save -> Save.used-20261021-202904
rename  : Save.real-S1.5gB-20261021-200449 -> Save
  restored Save vs real baseline: changed=0 missing=0 added=0
OK restore: 57 files
```
`logs/status-post-restore.log`: `Save` 57 ไฟล์ digest **`6dd645d9ef17`** · `leftovers: none` · `EXACT` ทั้งสอง hashfile

### rehash อิสระ (`verify-final.py`)

| เปรียบเทียบ | ผล |
|---|---|
| current `Save` vs `S1.5gB/savehash.real.before.txt` | **changed=0 missing=0 added=0 → 57/57 EXACT** (digest `6dd645d9ef17…`) |
| `S1.5gB/real.before` vs `S1.5gB/copy.before` | changed=0 missing=0 added=0 |
| current vs baseline ก่อนหน้า (S1.5g `046ceb7a4810`) | changed=**1** = `PlayerprefData.dat` (pref drift จากเหตุการณ์ 19:49 ที่ rebaseline ไว้แล้ว) |

## 5. ไฟล์เก็บย้อนหลัง

| ที่อยู่ | เนื้อหา |
|---|---|
| `coop-test/S1.5gB-20261021-200449/Save.used-20261021-202904/` | สำเนาที่เกมเขียนระหว่างทดสอบ (pref 1 ไฟล์) |
| `coop-test/S1.5gB-20261021-200449/save/` | สำเนาต้นฉบับตอน split |
| `coop-test/S1.5gB-20261021-200449/{savehash.*,MANIFEST.json,verify-*.log/…}` | hash + manifest + ผลตรวจ |
| `coop-test/baselines/20261021-200450/` | baseline จาก rebaseline (volatile-only) |

## 6. สรุป

**Save = PASS**: 57/57 digest `6dd645d9ef17` เท่าตอนเริ่มเป๊ะ · ของจริงไม่ถูกเขียน · ไม่มี `SaveSlot*` เปลี่ยนทั้งตอนรันบนสำเนา · `leftovers: none`
