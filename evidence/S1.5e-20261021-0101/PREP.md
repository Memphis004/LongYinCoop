# S1.5e — PREP: W7 attack probe + split ใหม่ (2026-10-21 01:01)

สถานะ: **พร้อมเปิดเกมเพื่อวัด chain ของ attack** · ยังไม่มีคำสั่งที่เปลี่ยน state ของเกมในรอบนี้ (0 คำสั่ง)

## 1. สิ่งที่เพิ่ม (ได้รับอนุมัติ 2026-10-21 00:5x)

| ไฟล์ | สิ่งที่เพิ่ม |
|---|---|
| `src/LongYinCoop/S1.5c/JitProbeW7.cs` | ใหม่ — probe อ่านอย่างเดียว 13 method ของ chain attack |
| `src/LongYinCoop/CoopPrefs.cs` | `JitProbeW7` (default **false**) |
| `src/LongYinCoop/CoopMod.cs` | ติด/ถอด W7 ตาม pref (OnInitialize + Tick poll + OnDeinitialize + kill switch), StageLabel → `S1.5e-W7` |
| `src/LongYinCoop/IpcServer.cs` | `set_pref --key jitprobeW7` (เปิด/ปิดระหว่างรัน + คืน `installed/missing/hits`) |

hook ทั้งหมดเป็น **prefix/postfix อ่าน argument และอ่าน field** — ไม่เรียก method ของเกม, ไม่เขียน field, ไม่แตะ `__result`, ห่อ try/catch ทุก hook, เขียนเฉพาะ ndjson ของ mod:

```
BattleGridClicked(GameObject)        pre+post
GridUnitController.OnClick()         post
ChangeActiveSkill(skill, Vector3)    pre+post
ChangeNowActiveAttackSkill(...)      pre+post
RefreshAttackRange()                 pre+post
CancelAttack()                       post
PlayBattleUnitAttack()               post
BattleUnitAttackStart()              post
BattleUnitAttackHappen()             post
BattleUnitAttackHit(GridUnitData,f)  post   (+ target row/col + ยูนิตบนช่องนั้น)
BattleUnitAttackFinish()             post
BattleUnitAttackEnd(f)               post
ManageDamage(src, tgt, bool)         pre+post (hp/alive ของ src+tgt)
```

field ที่บันทึกต่อ event: `frame`, `turnIndex`, `battleState`, `nowActiveState`, `paused`, `playingAnim`, `attackConfirmed` (`attackComfirmed`), `turnAttacked`, `selectedGrid` (name/row/col), `attackRangeCount`/`damageRangeCount`/`moveRangeCount`/`movePathCount`, `attackRange` (สูงสุด 12 ช่อง), ยูนิตที่ถึงตา (stableId/pos/hp/`nowOnAttackSkill.skillID`), ยูนิตบนช่องที่คลิก, stack ย่อ 30 ครั้งแรกต่อ method

## 2. build / deploy

| รายการ | ค่า |
|---|---|
| build | `dotnet build -c Release` → **0 warning / 0 error** (2 ครั้ง: ก่อน/หลังเพิ่ม `set_pref jitprobeW7`) |
| DLL สุดท้าย | **`35dd058b692d47e267a4bcec339039e6675f13ecf69217bcfb77ed524086c5b9`** (180,224 B) |
| DLL ที่ deploy | `Mods\LongYinCoop.dll` = `35dd058b…` (ตรวจ sha256 หลัง copy) |
| backup รุ่นก่อน | `Mods\LongYinCoop.dll.prev-bbf8cd42be5d` = build ที่ปิด S1.5d |
| เกมตอน deploy | ปิด (ไม่มี `LongYinLiZhiZhuan.exe`) |

## 3. เซฟ — restore + split ใหม่ (2026-10-21 00:59–01:00)

| ขั้น | ผล |
|---|---|
| `sentinel unhide --label S1.5c-originhp-… --slot 8` | moved aside recreated `SaveSlot8` (1 ไฟล์เหมือนเดิม) → unhide 5 ไฟล์ `hash_match=True` |
| `restore --label S1.5c-originhp-…` | `Save.used-20261021-010002` เก็บไว้ · `Save` = ของจริง **changed 0/0/0**, 57 ไฟล์ |
| `split --label S1.5e-20261021-0101` | copy 57 ไฟล์ `changed=0 missing=0 added=0` · จริงเก็บเป็น `Save.real-S1.5e-20261021-0101` |
| `sentinel hide --slot 8` | `SaveSlot8` → `.hidden-SaveSlot8` (5 ไฟล์) |
| `verify --label S1.5e-20261021-0101` | ของจริง **changed 0 / missing 0 / added 0**, drift 0 · copy baseline `0/0/0` · sentinel 5/5 · **RESULT: PASS** |

## 4. ขั้นตอนวัด (ทำเมื่อผู้ใช้สั่ง — ยังไม่เริ่ม)

1. เปิดเกม (โหลดจากสำเนา) → เข้าศึก HardFight ที่มีศัตรู**อยู่ในระยะโจมตีของยูนิตที่ถึงตา**
2. `python tools/coopctl.py set_pref --key jitprobeW7 --value true` → ต้องได้ `installed=true`, `missing=0`, คืน `file=…jitprobe-w7-<ts>.ndjson`
   - ถ้า `missing>0` → หยุด รายงาน (method signature ไม่ตรงตาม dump) ห้ามเดา
3. `python tools/coopctl.py spike` (status อ่านอย่างเดียว) 1 ครั้งก่อนกด — เก็บ `stateHash` + ตำแหน่ง/hp
4. **ผู้เล่นกดโจมตีเอง 1 ครั้ง** ผ่าน UI ปกติ (ไม่ต้องใช้ mod) — ปล่อยให้แอนิเมชันจบ
5. `spike` status อีกครั้ง — เก็บ `stateHash` + hp ของ target
6. `set_pref --key jitprobeW7 --value false` → ต้องได้ `installed=false` (ตรวจว่า unpatch จริง)
7. ส่ง path ของ ndjson + ผล status ทั้งสองครั้งกลับมาให้ผมวิเคราะห์

**หมายเหตุที่ต้องถือ:**
- ระหว่าง W7 เปิดอยู่ `spike status` จะรายงาน `actionPatches.count > 0` (W7 hook เอง) — ไม่ใช่การละเมิด; ค่า `actionPatches` ถูก cache ที่การเรียกครั้งแรกของโปรเซส
- ยัง**ห้าม**ยิง `spike arm`/`move`/`attack` ในรอบวัดนี้ — รอบนี้เป็น read-only
- ถ้าเกม crash / ค้าง / เซฟ hash เปลี่ยน → หยุดทันทีตาม §8

## 4.1 คำตัดสินที่ผู้ใช้อนุมัติ (2026-10-21 01:10)

| ข้อ | คำตัดสิน | ผลต่อการวัด |
|---|---|---|
| 3 | ใช้ **HardFight ใหม่บน Save สำเนา** | ไม่ใช้ศึกค้างจาก S1.5d; เซฟสำเนาอาจ drift ได้ (ไม่แตะเซฟจริง) |
| 4 | ทดสอบ **เฉพาะสกิลโจมตี Default** | ห้ามสั่งเปลี่ยนสกิล (ไม่เรียก `ChangeNowActiveAttackSkill`/`ChangeActiveSkill` ด้วย mod) — ถ้าเกมเรียกเองระหว่างกด ให้บันทึกเป็นหลักฐานแต่ไม่นับเป็นเกณฑ์ |
| 5 | **ไม่ข้ามศึก**ในการนับ 10/10 | ถ้าศึกจบก่อนครบ 10 → หยุดและรายงานก่อน ไม่เริ่มศึกใหม่เอง |

## 4.2 สถานะจริงตอนเปิด probe (2026-10-21 01:09:56)

- `set_pref --key jitprobeW7 --value true` → `installed=true`, **`missing=0`**, 13/13 method ติด, ไฟล์ `UserData\LongYinCoop\CoopSpike\jitprobe-w7-20261021-010956.ndjson`
- log ยืนยัน: `JitProbe W7 installed: BattleGridClicked,ChangeActiveSkill,ChangeNowActiveAttackSkill,RefreshAttackRange,CancelAttack,PlayBattleUnitAttack,BattleUnitAttackStart,BattleUnitAttackHappen,BattleUnitAttackHit,BattleUnitAttackFinish,BattleUnitAttackEnd,ManageDamage,OnClick missing=0`
- เกม (pid 31684, start 01:05:58) โหลด build `S1.5e-W7` — ตรงกับ DLL `35dd058b…`
- สถานะศึกตอนนั้น: `inBattle=true`, `battleState=3 Fighting`, `nowActiveState=2 WaitPlayerInput`, `turnIndex=12`, 9 ยูนิต, active `0:0` ที่ (6,12), ศัตรูอยู่ col 15–18
- `attackRangeCount=5` และ `attackRange=8:17,7:17,9:17,7:18,9:18` **ทั้งที่ active อยู่ (6,12)** → เป็น range ค้างจากหน่วยก่อน (ยังไม่ยืนยันว่าใครเขียน) — ต้องดูจาก W7 ว่า `RefreshAttackRange` ถูกเรียกเมื่อไร
- `actionPatches.count=4` owners=`longyincoop.jitprobe.w7` (BattleGridClicked / ChangeActiveSkill / ChangeNowActiveAttackSkill / PlayBattleUnitAttack) — **คาดไว้แล้ว** ไม่ใช่การละเมิด
- ไฟล์หลักฐาน: `logs/status-0112-probe-on.json`, `logs/jitprobe-w7-start.ndjson`

## 5. ยังไม่ทำ
- ยังไม่เขียน `AttackCommand` (รอผล W7)
- ยังไม่แตะ `attackRange` ใน `spike status` (จะทำใน build ของ S1.5e หลังวัด)
- ยังไม่ทำ S1.5f/g/h · ไม่ push · ไม่สร้าง tag
