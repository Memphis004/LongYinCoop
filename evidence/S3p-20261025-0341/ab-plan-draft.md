# A/B Plan (DRAFT — รออนุมัติ · ห้ามเริ่ม runtime จนกว่าจะได้คำสั่ง)

เป้าหมาย: ตรวจว่า crash หลังเข้า `WaitAttacking` **เกิดซ้ำกับ build เก่า `5d7b7cc5…` หรือไม่** ภายใต้เงื่อนไขเดียวกับ incident (attack ที่เกมรับ ระหว่างรันลำดับโจมตี) · ยิง **1 ครั้ง** · ไม่ retry

## 0. สถานะที่ยึด (ตรวจแล้ว read-only ณ จัดทำแผน)

- HEAD `ce3e06c` · baseline ล่าสุด `coop-test/baselines/20261025-052020` (digest `b7f67154cff6…` · volatile_only · 57 ไฟล์)
- `Mods\LongYinCoop.dll` = **`5d7b7cc5…`** (A/B target — ไม่ swap) · `f2ad30cd…` พักที่ `.s3prereq-f2ad30cd`
- `split` รองรับ `--expected-baseline` (CLI ยืนยันแล้ว) · preflight-launch READY-style ใช้ได้

## 1. ลำดับปฏิบัติ (หลังอนุมัติแผนแยก)

1. **Pre-flight (เกมปิด):** ตรวจ working tree ไม่มีการเปลี่ยน src · DLL hash = `5d7b7cc5…` · รัน split + preflight (คำสั่ง §3) — ทุกข้อต้อง PASS ก่อนขอให้ผู้ใช้เปิดเกม
2. **ผู้ใช้เปิดเกมเอง** → agent ยืนยัน: ML log session ใหม่ · DLL ที่โหลดยัง `5d7b7cc5…` · เก็บ Player.log สำเนา "ก่อนทดสอบ"
3. **ผู้ใช้เข้า battle + เข้าสู่ `WaitAttacking` ด้วยตัวเอง** (แจ้ง agent)
4. **อ่าน state สด** (`spike status` — read-only): ยืนยัน inBattle/Fighting/WaitAttacking · active id · units[] ทั้งหมด (id/team/row/col/hp/unitAlive) · attackRangeCount — **IDs จากศึกปัจจุบันเท่านั้น**
5. **ยืนยันเป้าหมายในระยะ (trusted source):** ผู้ใช้ยืนยันจากหน้าจอเกมว่าเป้าหมายที่เลือกถูกเกม**ไฮไลต์เป็นเป้าโจมตีที่โจมตีได้** (เกม mark เองในโหมดเล็ง) + status สดยืนยัน attacker/target มีจริง-มีชีวิต-พิกัด · **ถ้ายืนยันไม่ได้ = NOT-RUN ห้ามยิง** (surface ไม่ expose cells — การยืนยันด้วยสายตาจาก UI ของเกมคือแหล่งเดียวที่เชื่อถือได้บน build นี้)
6. **ยิง 1 ครั้ง:** `spike arm 120` → `spike attack --id <active> --target <เป้าที่ยืนยันแล้ว>` — timestamp บันทึก · **ไม่ retry ไม่มีคำสั่งสำรอง**
7. **สังเกตหลังยิง (หลักฐานความมีชีวิต ไม่ใช่แค่ไม่มี Event 1000):** ตรวจซ้ำ `spike status` ได้ + process ยังรัน + ML log มีบรรทัดใหม่ต่อเนื่อง + ผล attack (confirmed/hp drop/state เปลี่ยน) · เก็บ Player.log สำเนา "หลังทดสอบ"
8. **ผู้ใช้เล่นจนจบศึกตามปกติ** (หลักฐาน "จบศึกได้") → ปิดเกม → agent ตรวจ save/split digest + restore + รายงาน

## 2. มือ vs `spike attack` — ความต่างและทางที่เลือก

| | คลิกมือ | `spike attack` |
|---|---|---|
| entry | game UI input → `BattleGridClicked` | mod เรียก `BattleGridClicked` ผ่าน MainThreadDispatcher |
| timing | มนุษย์ | ทันทีหลัง arm (same-frame) |
| mod job | ไม่มี deferred hp-check | Tick poll `heroData` ทุกเฟรมจนครบ timeout |
| incident | — | **เกิดบน path นี้** |

**เลือก `spike attack` เป็นวิธีหลักเพียงวิธีเดียว** — เพราะต้องการ reproduce เงื่อนไข incident ให้ได้มากที่สุด (path + timing + mod job ตรงกัน) · ห้ามสลับไปมือระหว่างทดสอบ (ถ้า spike ไม่ crash แล้วอยากทดมือ = แผนใหม่อนุมัติแยก)

## 3. คำสั่งเต็มที่จะใช้ (ยังไม่รัน)

```
# pre-flight (เกมปิด)
python tools/save-split.py split --label AB-oldbuild-<ts> \
  --expected-baseline coop-test/baselines/20261025-052020/savehash.real.before.txt
python tools/save-split.py preflight-launch --label AB-oldbuild-<ts>
sha256sum Mods/LongYinCoop.dll          # ต้อง = 5d7b7cc5…
# runtime (หลังผู้ใช้เปิดเกม + เข้า WaitAttacking)
python tools/coopctl.py spike --sub status --quiet            # state สด + IDs
python tools/coopctl.py spike --sub arm --seconds 120
python tools/coopctl.py spike --sub attack --id <active-live> --target <target-live>
python tools/coopctl.py spike --sub status --quiet            # หลังยิง + ตรวจต่อเนื่อง
# post
python tools/save-split.py status                             # digest หลังจบ
```

## 4. Preflight PASS/FAIL (ต้องครบก่อนเปิดเกม)

- split: `Save vs baseline changed=0 missing=0 added=0` (vs `b7f67154cff6…`) · real → aside · copy EXACT
- preflight-launch: **READY ทุกข้อ** (เกมไม่รัน · aside ตรง manifest · real-aside-untouched · ไม่มี sentinel)
- DLL `5d7b7cc5…` ✓ และไม่มีการ swap · working tree src ไม่เปลี่ยน
- ข้อใด FAIL = หยุด รายงาน ไม่เปิดเกม

## 5. หลักฐานที่เก็บ (ระหว่างรัน)

ML log เต็มของ session · Player.log ก่อน/หลัง (หา path จาก `%USERPROFILE%\AppData\LocalLow\<company>\<product>\`) · timestamp ของ WaitAttacking/คำสั่ง/ผล · Event 1000/1001 ถ้า crash (module+offset) · DLL hash ก่อน/หลัง · save digest ก่อน/หลัง · รายงานแยก **OBSERVED / INFER / UNKNOWN** — "ไม่มี Event 1000" ไม่มีสิทธิ์สรุปว่าไม่ crash ต้องยืนยัน process รัน + log ต่อเนื่อง

## 6. หยุด/ฟื้นคืน

crash/hang/สถานะไม่ชัด/DLL hash ไม่ตรง/save integrity fail ⇒ หยุดทันที ห้าม retry ห้ามเปิดเกมเอง · เก็บ log + crash evidence ก่อนแตะอะไร · ตรวจ save/split/digest · restore เสนอเฉพาะเมื่อจำเป็นและปลอดภัย (ผ่านมติ)

## 7. ตารางตีความ (ตามกรอบผู้ใช้)

| ผล | ข้อสรุปที่อนุญาต |
|---|---|
| crash ซ้ำบน build เก่า | ยืนยัน crash เกิดซ้ำได้ภายใต้เงื่อนไขนี้ — ยังไม่พิสูจน์สาเหตุ |
| ไม่ crash + log/process ต่อเนื่อง | ยังไม่พบ crash ใน 1 ครั้ง — ไม่พิสูจน์ความปลอดภัย |
| hang/หลักฐานไม่ครบ | Inconclusive — ห้ามนับ PASS |
| ยืนยันระยะ/ส่งไม่ได้ | NOT-RUN — ห้ามยิงทดลอง |

## 8. งานเอกสาร/commit (แผนล่วงหน้า — ยังไม่ทำ ต้องอนุมัติแยก)

1. incident doc อัปเดต: rebaseline สำเร็จ · pref `5bc97eb2…` · digest ใหม่ `b7f67154cff6…` · baseline `20261025-052020`
2. `VERSION.md`: baseline ใหม่ + กติกา `--expected-baseline` สำหรับ split ถัดไป
3. รวบรวม/แก้รายการ baseline เก่าที่อ้าง `4226730f66ff` ซ้ำซ้อนให้เหลืออ้างอิงเดียวที่ชี้สถานะ "superseded โดย 232024 → 20261025-052020"
4. commit 2 ชุด (ไม่ใช้ `git add -A`): (i) `tools/negative-tests.py` + `tools/test_negative_preconditions.py` (ii) evidence/ + coop-test/baselines/20261025-052020/

## 9. ความเสี่ยงที่ยังไม่ปิด

- ต้นเหตุ crash UNKNOWN — ผล 1 ครั้งไม่พิสูจน์ทั้งสองทิศ
- การยืนยันระยะเป็น human-in-the-loop (UI highlight) — surface build เก่าไม่ expose cells
- alive ที่อ่านจาก build เก่าเป็นนิยาม v1 (`!hero.dead`) — เทียบกับ incident (v2) ได้เฉพาะเรื่อง crash ไม่ใช่ semantics
- การยิง 1 ครั้งเปลี่ยน state ศึกจริง (damage/ศพอาจเกิด) — ศึกต้องเล่นจบตามปกติหลังทดสอบ
- ถ้า crash ซ้ำบน build เก่า: ต้องมีแผนถัดไปแยก (เช่น ทดสอบ manual click เทียบ) — ไม่รวมในแผนนี้
