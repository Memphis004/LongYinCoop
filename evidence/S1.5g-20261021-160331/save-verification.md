# S1.5g · Save verification (สำเนาทดสอบ + restore 57/57)

label `S1.5g-20261021-160331` · เกมปิดก่อน/หลังทุกขั้น · **ไม่มีการกด Save ในเกม และ mod ไม่เรียก Save/Load**

---

## 1. สถานะก่อนทดสอบ [RUNTIME]

| รายการ | ค่า |
|---|---|
| `Save` ที่ mount ตอน pre-flight | ของจริง · 57 ไฟล์ · digest `2b0548fe464b` |
| anchor เกม | `GameAssembly.dll` / `global-metadata.dat` ตรง `VERSION.md` (เกมไม่อัปเดต) |
| เหตุการณ์ 16:07 | เกมถูกเปิดบน **เซฟจริง** โดยไม่ตั้งใจ (16:07–16:09) → เขียนเฉพาะ `PlayerprefData.dat`; `SaveSlot*` ไม่ขยับ → บันทึกใน `incident-real-save-mounted-1607.md` |
| rebaseline 16:11:20 | ยอมรับเฉพาะ volatile-only drift (`PlayerprefData.dat`) → baseline ใหม่ `coop-test/baselines/20261021-161120/` · digest **`046ceb7a4810`** |

## 2. การ split (G-0) [RUNTIME]

`logs/split.log`:
```
expect  : coop-test\baselines\20261021-161120\savehash.real.before.txt
  Save vs baseline: changed=0 missing=0 added=0
rename  : Save -> Save.real-S1.5g-20261021-160331
copy    : coop-test/S1.5g-20261021-160331/save -> Save
  copy vs real: changed=0 missing=0 added=0
OK split S1.5g-20261021-160331: 57 files
```
- ของจริงถูก **rename** เก็บเป็น `Save.real-S1.5g-20261021-160331` (57 ไฟล์ digest `046ceb7a4810`) — ไม่ถูกเขียนทับ
- สำเนาที่ mount = 57 ไฟล์ digest เดียวกันทุกไบต์ (`savehash.real.before.txt` ≡ `savehash.copy.before.txt`)

## 3. ระหว่าง/หลังทดสอบ — ใครเขียนอะไร [RUNTIME]

`verify-20261021-190937.json` (เกมปิดแล้ว):
```json
"mounted_digest": "f43521d486ca…", "mounted_is_real_baseline": false,
"copy_drift": { "changed": 1, "missing": 0, "added": 0, "slot_drift": 0, "other_drift": [] },
"real": { "present": true, "unchanged": true, "mtime_drift": [] }
```

| ข้อ | ผล |
|---|---|
| สำเนา (ที่เกมเห็น) ถูกเขียน | **ใช่** — digest เปลี่ยน `046ceb7a4810` → `f43521d486ca` ⇒ **เกมเขียนลงสำเนาจริง** |
| เขียนอะไร | **`PlayerprefData.dat` เท่านั้น** (`changed=1`, `slot_drift=0`) |
| `SaveSlot*` ถูกเขียนไหม | **ไม่** (0 ไฟล์) ⇒ ไม่มีการบันทึกเกมระหว่างทดสอบ |
| โฟลเดอร์ของจริง | **ไม่ขยับเลย**: `changed=0`, `missing=0`, `added=0`, `mtime/size drift = 0` |

## 4. Restore + ตรวจซ้ำ [RUNTIME]

`logs/restore.log`:
```
copy check: changed=1 (pref=1) missing=0 added=0
rename  : Save -> Save.used-20261021-190951
rename  : Save.real-S1.5g-20261021-160331 -> Save
  restored Save vs real baseline: changed=0 missing=0 added=0
OK restore: 57 files
```
```
Save           files=57  digest=046ceb7a4810
leftovers: none
mounted Save matches: EXACT savehash.copy.before.txt / EXACT savehash.real.before.txt
```

### ตรวจซ้ำแบบอิสระ (`verify-final.py`, rehash ทุกไฟล์ใหม่ทั้งหมด)

| เปรียบเทียบ | ผล |
|---|---|
| current `Save` vs `S1.5g/savehash.real.before.txt` | **changed=0 missing=0 added=0 → 57/57 EXACT** · digest `046ceb7a4810…` |
| `S1.5g/real.before` vs `S1.5g/copy.before` (ความสมบูรณ์ของ split) | changed=0 missing=0 added=0 |
| current vs canonical `2b0548fe464b` (baseline 8 ชุดเดิม) | changed=**1** = `PlayerprefData.dat` (drift ที่ยอมรับจากเหตุการณ์ 16:07 — ไม่ใช่ของใหม่จาก S1.5g) |

> บันทึก: การตรวจรอบแรกของผมรายงาน 57/57 ต่างกันเพราะ parse path ของไฟล์ hash ไม่ตรง prefix (`*Save/`) — แก้การ normalize แล้ว (ตัด `*Save/`, ใช้ `/`) ผลจริงคือ 0/0/0 ตามตารางข้างบน · สคริปต์ที่แก้แล้วเก็บใน `verify-final.py`

## 5. ไฟล์ที่เก็บไว้ตรวจย้อนหลัง

| ที่อยู่ | เนื้อหา |
|---|---|
| `coop-test/S1.5g-20261021-160331/Save.used-20261021-190951/` | สำเนาที่เกมเขียนระหว่างทดสอบ (pref เปลี่ยน 1 ไฟล์) |
| `coop-test/S1.5g-20261021-160331/save/` | สำเนาต้นฉบับตอน split |
| `coop-test/S1.5g-20261021-160331/{savehash.real.before,savehash.copy.before}.txt` | hash ทุกไฟล์ก่อนทดสอบ |
| `coop-test/S1.5g-20261021-160331/{MANIFEST,verify-20261021-190937,restore-20261021-190952}.json` | manifest + ผลตรวจ + ผล restore |
| `coop-test/baselines/20261021-161120/` | baseline ที่ rebaseline จากเหตุการณ์ 16:07 |

## 6. Cleanup อื่น (ไม่เกี่ยวกับเซฟ)

`coop.jitprobeW4` (hook อ่านอย่างเดียว) ถูกเปิดระหว่างวัดและ **ปิดคืนแล้ว** ใน `UserData\MelonPreferences.cfg`:
`JitProbeW4 = true` → `false` · sha256 ของ cfg: `9BB4CB95…B44D` → `269B571C…98D1` (แตะเฉพาะบรรทัดนี้; ไม่แตะส่วน `[UnityExplorer]`)

## 7. สรุป

**Save = PASS**: 57/57 ไฟล์ digest `046ceb7a4810` เท่าตอนเริ่มทดสอบเป๊ะ · ของจริงไม่ถูกเขียนเลยตลอด stage · ไม่มี `SaveSlot*` เปลี่ยนทั้งตอนเกมรันบนสำเนา · `leftovers: none` · drift เดียวที่เหลือเป็น pref ที่สืบจากเหตุการณ์ 16:07 ซึ่งบันทึกและ rebaseline ไว้แล้ว
