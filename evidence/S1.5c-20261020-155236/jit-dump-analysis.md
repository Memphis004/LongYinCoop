# S1.5-c ก้าวแรก — วิเคราะห์จาก dump (ยังไม่ติด hook · ยังไม่เปิดเกม)

วันที่: 2026-10-20 · ขอบเขตที่อนุมัติ: **งานวิเคราะห์จาก dump + เก็บงาน เท่านั้น**
สถานะ: ไม่มีการเปิดเกม · ไม่มีการติด Harmony hook ใหม่ · ไม่มีการเรียก IL2CPP · ไม่เขียน field ใดของเกม

แหล่งข้อมูล: `Il2CppDumper/dump.cs` (game folder) — อ้างเป็น `dump.cs:<line>` ทั้งหมด
ป้าย: **DUMP** = มีใน dump จริง · **GUESS** = ยังไม่ยืนยันด้วยรันไทม์ · **SPEC** = กติกาของเรา

> dump ไม่มี method body → **ทุกข้อความเป็น DUMP เฉพาะ "การมีอยู่/signature"** ส่วน "ใครเรียกใคร" เป็น GUESS จนกว่าจะวัดด้วย postfix probe (S1.5-c ขั้นติด hook)

---

## 1. J1 — entry method ของ attack ที่รับ target + ยืนยัน `BattleGridClicked`

### 1.1 ผลค้น
| สิ่งที่ค้น | ผล | ป้าย |
|---|---|---|
| `BattleController.BattleGridClicked(GameObject clickedGrid)` | **มี** · public instance · `dump.cs:363169` · RVA `0x7FA090` | DUMP |
| method ที่รับ `BattleUnit`/`GameObject` เป็น **เป้าโจมตี** (`BattleUnitClicked`, `UnitClicked`, `ClickUnit`, `OnUnitClick`, `AttackButtonClicked`) | **0 hit ทั้งไฟล์** | DUMP |
| `BattleUnit` (MonoBehaviour) มี `OnClick`/`OnHover` ไหม | **ไม่มี** (0 hit ใน `dump.cs:363973–364250`) | DUMP |
| `GridUnitController.OnClick()` | **มี** · public instance · ไม่มีพารามิเตอร์ · `dump.cs:364349` · RVA `0x870200` | DUMP |
| coroutine สายโจมตี | `PlayBattleUnitAttack()` `dump.cs:363508` · `BattleUnitAttackStart()` 363512 · `BattleUnitAttackHappen()` 363532 · `BattleUnitAttackHit(GridUnitData targetGridUnit, float startDelay)` 363539 · `BattleUnitAttackFinish()` 363543 | DUMP |
| field เป้าโจมตี | `attackRangeGridUnits` 0x1F8 · `selectedGrid` 0x108 · `attackComfirmed` (private bool 0x210, **field ไม่ใช่ method**) `dump.cs:363082` | DUMP |

### 1.2 ข้อสรุป J1
- **ไม่มี entry method เฉพาะกิจ "attack(target)" ในเกมนี้** — ทั้ง target selection และการยืนยันเป้าไหลผ่าน **เส้นทางคลิกกริด** ไม่ใช่คลิกตัวละคร (ยูนิตไม่มี OnClick เลย) **[DUMP]**
- entry ที่เป็นไปได้มีตัวเดียว: `BattleController.BattleGridClicked(GameObject)` ซึ่งถูกเรียกจาก `GridUnitController.OnClick()` ของช่องที่คลิก **[GUESS — ต้องยืนยันด้วย probe postfix บน `OnClick` + `BattleGridClicked` พร้อมกัน: ถ้าคลิก 1 ครั้งแล้วเห็นทั้งคู่ตามลำดับ = ยืนยัน]**
- ลำดับการโจมตี (GUESS จากชื่อ method + field): `BattleGridClicked` → ตรวจ `attackRangeGridUnits` → ตั้ง `selectedGrid`/`attackComfirmed` → `PlayBattleUnitAttack()` → `BattleUnitAttackStart/Happen/Hit/Finish` → `BattleUnitAttackEnd(float delayTime)` (`dump.cs:363565`) → ตั้ง `nowActiveUnitAttacked`
- ⚠️ ถ้าลำดับจริงต้อง **เขียน** `selectedGrid`/`attackComfirmed` ก่อนเรียก entry → **หยุดถามทันที** ตาม S1.5-plan §0 ข้อ 4 (ยังไม่อนุมัติเขียน field)

---

## 2. J4 — `GridUnit`/`MapGrid` มี `OnClick`/`PointerClick`/`SendMessage` ไหม

| คำถาม | ผล | ป้าย |
|---|---|---|
| มีคลาส `GridUnit` / `MapGrid` ไหม | **ไม่มี** — ของจริงคือ `GridUnitController : MonoBehaviour` (`dump.cs:364250`, TypeDefIndex 8085) และ data class `GridUnitData` (`dump.cs:364522`, 8093) | DUMP |
| `GridUnitController` implement `IPointerClickHandler`? | **ไม่** — interface list ของคลาสมีแค่ MonoBehaviour; ไม่มี `OnPointerClick` | DUMP |
| `GridUnitController` มี callback อะไร | `OnClick()` 364349 · `OnHover()` 364352 · `OnDrag(Vector2 delta)` 364355 · `OnScroll(float delta)` 364358 · `OnHit(BattleUnit nowActiveUnit)` 364316 · `OnEnter()`/`OnLeave()` 364319/364322 — ทั้งหมด public instance | DUMP |
| เกมเรียก `SendMessage("OnClick")` เองไหม | ไม่พบ `SendMessage(string)` ที่เป็นของเกม — ที่เจอทั้งหมดเป็น UnityEngine internals | DUMP |
| `GridUnitData` มีพฤติกรรมคลิกไหม | ไม่มี — เป็น data ล้วน (field `row`/`column`/`gridType` ฯลฯ) | DUMP |

**สรุป J4:** การคลิกกริดไม่ได้ผ่าน Unity EventSystem/`IPointerClickHandler` แต่ผ่าน method เปล่า `OnClick()` ที่ถูกเรียกจากตัวจัดการ raycast/คอนโทรลเลอร์ของเกม **[GUESS ว่าใครเรียก — ยืนยันได้ด้วย probe postfix บน `GridUnitController.OnClick` ตอนผู้เล่นคลิกช่องจริง]**
→ ทางเลือกที่แตะ state น้อยที่สุดสำหรับ S1.5-d คือ "เรียก `OnClick()` ผ่าน `GameObject`-level" **แต่ยังไม่ทดลอง** และถ้าต้องตั้ง state ก่อน (เช่น `nowActiveState`) = นอกสิทธิ์ ต้องขออนุมัติ

---

## 3. J6 — `battlePaused` ถูก toggle ด้วย method อะไร

| สิ่งที่ค้น | ผล | ป้าย |
|---|---|---|
| method ที่ชื่อมี Pause ใน `BattleController` | `BattlePauseButtonClicked(GameObject buttonClicked)` `dump.cs:363624` · getter `BattlePaused()` 363403 · `HaveBattlePauseUI()` 363406 · `SetPauseButtonInteractable(bool)` 363627 | DUMP |
| method อื่นในทั้งไฟล์ที่ตั้ง `battlePaused` | **ตรวจจาก dump ไม่ได้** (ไม่มี body) — ชื่อ method ไม่บอก; ต้องวัด | — |
| ฟิลด์ `battlePaused` | public bool 0x1D0 `dump.cs:363073` | DUMP |
| ค่าที่วัดได้ในศึกจริง (S1.5-b) | `true` **0 จาก 82,695 sample** รวม 41,777 sample ที่ `activePlayerControl=true` + `WaitPlayerInput` | RUNTIME |

**สรุป J6:**
- ผู้ต้องสงสัยหลักคือ `BattlePauseButtonClicked(GameObject)` (ปุ่มหยุดศึกของผู้เล่น) **[GUESS]**
- **ขัดกับ `battle-notes.md` เดิม** ที่เขียนว่า `battlePaused = true` ขณะผู้เล่นเลือก action → **แก้เอกสารแล้ว** (battle-notes.md §1.1/§1.2/§10 พร้อมป้าย RUNTIME S1.5-b) และรายงานใน §7 ของไฟล์นี้
- AI/ระบบไม่น่าจะ toggle ฟิลด์นี้ (เพราะไม่เคยเป็น true ตอน AI เดินเลยใน 82,695 sample) **[GUESS — ยังไม่ยืนยันด้วย probe]**
- วิธีปิด J6 ให้สมบูรณ์: postfix บน `BattlePauseButtonClicked` + sample `battlePaused` ก่อน/หลัง ในตอนผู้เล่นกดปุ่มจริง (S1.5-c hook step)

---

## 4. J8 — `SetAllAuto` signature และ scope

| ข้อมูล | ค่า | ป้าย |
|---|---|---|
| `BattleController.SetAllAuto(bool auto)` | public instance · `dump.cs:363654` · RVA `0x81ECA0` — **มีพารามิเตอร์ `bool`** (เอกสารเดิมเขียน `void SetAllAuto()` → แก้แล้ว) | DUMP |
| method ใกล้เคียง | `BattleController.AutoButtonClicked()` 363437 · `BattleUnit.ChangeAutoType()` 364045 / `ChangeAutoType(bool auto)` 364048 · `BattleAISettingController.AutoButtonClicked()` 364640 · `BattleAiMenuController.AllAutoButtonClicked()` 364682 | DUMP |
| scope การเขียน | เขียน `BattleUnit.autoFight` (public bool 0xB0) ของยูนิตในทีมผู้เล่น — **เดาจากชื่อ/ฟิลด์ ยังไม่ยืนยัน** | GUESS |
| การใช้งานใน S1.5 | **ยังไม่เรียก** และจะไม่เรียกจนกว่าจะอนุมัติแยก (ใช้เป็น fallback กู้ค้างเท่านั้น ตาม §6 J8 เดิม) | SPEC |

---

## 5. J2 — รายชื่อ method ที่จะ hook (postfix อ่าน argument เท่านั้น)

เงื่อนไขร่วมของทุกตัว: `__originalMethod` ตรงกับรายการนี้ · postfix **อ่าน argument/field อย่างเดียว** · ห่อ try/catch ทั้งก้อน · ไม่เรียก method เกม · ไม่แตะ `__result`/`__instance` (อ่าน static/instance field ผ่าน `__instance` ได้เฉพาะ field อ่าน) · เขียนเฉพาะไฟล์ log ของ mod · มี rate limit

| ลำดับติดตั้ง | คลาส | method (signature จาก dump) | public/static | dump.cs | ทำไมต้อง hook | ความเสี่ยงต่อการทำงานของเกม |
|---|---|---|---|---|---|---|
| 1 | `GridUnitController` | `OnClick()` | public instance | 364349 | พิสูจน์ว่า "คลิกช่อง" มาทางนี้จริง (J4) | ต่ำมาก — ถูกเรียกเฉพาะตอนคลิก/แตะช่อง; postfix อ่าน `gridData.row/column` |
| 2 | `BattleController` | `BattleGridClicked(GameObject clickedGrid)` | public instance | 363169 | entry หลักของ move/attack (J1) + ต้องการ argument เป็นชื่อ GameObject | ต่ำ — 1 ครั้งต่อคลิก; อ่านชื่อ/ตำแหน่ง GameObject |
| 3 | `BattleController` | `MoveButtonClicked()` | public instance | 363452 | เริ่มโหมดเดิน | ต่ำ — เฉพาะตอนกดปุ่ม |
| 4 | `BattleController` | `GenerateMovePath(GridUnitData targetGrid)` | public instance | 363412 | ได้ path ที่เกมคำนวณ (ไม่คำนวณเอง) | ต่ำ-กลาง — เรียกบ่อยตอนลาก/ชี้ช่อง; ต้องมี rate limit |
| 5 | `BattleController` | `CancelMove()` / `RegretMove()` | public instance | 363455 / 363458 | รู้จักการยกเลิก/ย้อนการเดิน | ต่ำ |
| 6 | `BattleController` | `ChangeActiveSkill(KungfuSkillLvData, Vector3)` | public instance | 363440 | รู้ว่าผู้เล่นเลือกสกิลอะไร (ส่งผลต่อ range) | ต่ำ — เฉพาะตอนเปลี่ยนสกิล |
| 7 | `BattleController` | `ChangeNowActiveAttackSkill(KungfuSkillLvData, Vector3)` | public instance | 363443 | สกิลโจมตีที่เลือก = ตัวกำหนด `attackRangeGridUnits` | ต่ำ |
| 8 | `BattleController` | `CancelAttack()` | public instance | 363446 | ขอบเขตของการยกเลิกโจมตี | ต่ำ |
| 9 | `BattleController` | `PlayBattleUnitMove()` | public instance (returns IEnumerator) | 363581 | จุดเริ่มแอนิเมชันเดิน = จุดที่ state เปลี่ยนเป็น Moving | ต่ำ — postfix ยิงตอน coroutine เริ่ม (ไม่ใช่ทุกเฟรม) |
| 10 | `BattleController` | `PlayBattleUnitAttack()` | public instance (IEnumerator) | 363508 | จุดเริ่มแอนิเมชันโจมตี | ต่ำ (เหมือนข้อ 9) |
| 11 | `BattleController` | `BattleUnitAttackStart()` / `BattleUnitAttackHappen()` / `BattleUnitAttackFinish()` | public instance (IEnumerator) | 363512 / 363532 / 363543 | ลำดับย่อยของ attack เพื่อเทียบกับ J1 | ต่ำ-กลาง — 3 hook เพิ่ม ให้ติดพร้อมกันเฉพาะเมื่อข้อ 9/10 ผ่าน |
| 12 | `BattleController` | `BattleUnitAttackHit(GridUnitData targetGridUnit, float startDelay)` | public instance (IEnumerator) | 363539 | **จุดที่รู้เป้าโจมตีจริง** (targetGrid) + เวลา hit | ต่ำ-กลาง |
| 13 | `BattleController` | `BattlePauseButtonClicked(GameObject buttonClicked)` | public instance | 363624 | ปิด J6 (ใคร toggle `battlePaused`) | ต่ำ — เฉพาะตอนกดปุ่ม |
| 14 | `BattleController` | `AutoButtonClicked()` / `SetAllAuto(bool auto)` | public instance | 363437 / 363654 | รู้ว่า auto ถูกเปิด/ปิดด้วยอะไร + argument | ต่ำ — เฉพาะตอนกดปุ่ม |
| 15 (เลือกได้) | `BattleUnit` | `EnterGrid(GridUnitData grid, bool noTurnRotation, bool teleport)` | public instance | 364173 | เห็นการย้ายช่องทีละก้าว (ยืนยัน path) | **กลาง** — เรียกทุกก้าวของทุกยูนิต; ต้อง rate limit เข้มหรือตัดออก |

**ไม่ hook:** `BattleController.RunBattle()` (363409) — เรียกทุกเฟรม, overhead/เสียงรบกวนสูง ไม่จำเป็น
**ไม่ hook:** `GameController.Update` (มี probe เดิมของ S0 แล้ว) และ `AddInfoText` (patch เดิมของ S1)

---

## 6. แผน hook ที่เสนอ (ยังไม่ติด — ขออนุมัติเป็นรายขั้น)

### 6.1 หลักการ
1. postfix ทั้งหมด อ่าน argument/field เท่านั้น — **ไม่เรียก method เกม ไม่เขียน field ไม่แตะ `__result`/`__instance` (เขียน)**
2. ห่อ `try { } catch (Exception) { }` ทั้งก้อน — exception ต้องไม่หลุดเข้าเกม (ห้าม throw/log ผ่าน MelonLoader แบบที่ทำให้เกมช้า)
3. pref ใหม่ **`coop.jitProbe`** default **false** (หมวด `[LongYinCoop]`) — false = ไม่ติด patch เลย (`PatchAll` ไม่ถูกเรียก) และถ้าติดอยู่แล้วจะ `UnpatchSelf()` (S0 พิสูจน์แล้วว่า unpatch ใช้ได้: `src/LongYinCoop/HarmonyProbe.cs:69`)
4. เขียนผลลง `UserData/CoopSpike/jitprobe-<ts>.ndjson` เท่านั้น (ไฟล์ของ mod) + rate limit ต่อ method (เช่น 200 บรรทัด/วินาที) กัน log ท่วมตอน `GenerateMovePath`
5. Harmony id แยก: `longyincoop.jitprobe.<method>` เพื่อ unpatch เฉพาะของเรา ไม่แตะ patch ของ mod อื่น
6. ติด **ทีละ wave** ตามตาราง §5 และหยุดรายงานหลังแต่ละ wave

### 6.2 ลำดับ wave ที่เสนอ (อนุมัติทีละ wave)
| wave | method | เหตุผลการจัดลำดับ |
|---|---|---|
| **W1** | `GridUnitController.OnClick` + `BattleController.BattleGridClicked` | เสี่ยงน้อยสุด, ตอบ J1/J4 ทันที, เฉพาะตอนคลิก |
| **W2** | `MoveButtonClicked` · `GenerateMovePath` · `CancelMove` · `RegretMove` | ได้ลำดับ move ครบ 1 รอบ (พร้อม rate limit) |
| **W3** | `ChangeActiveSkill` · `ChangeNowActiveAttackSkill` · `CancelAttack` · `PlayBattleUnitMove` · `PlayBattleUnitAttack` | ได้ลำดับ attack + จุดเริ่มแอนิเมชัน |
| **W4** | `BattlePauseButtonClicked` · `AutoButtonClicked` · `SetAllAuto` | ปิด J6/J8 (เฉพาะตอนกดปุ่ม) |
| (เลือก) **W5** | `BattleUnitAttackStart/Happen/Hit/Finish` · `EnterGrid` | ละเอียด/เสียงดัง — เอาเฉพาะเมื่อ W1–W4 ไม่พอ |

### 6.3 เกณฑ์ผ่านของแต่ละ wave (SPEC)
- เกมไม่ crash, ไม่มี exception จาก patch (นับใน log ของ mod), `stateHash` ไม่เปลี่ยนเพราะ probe
- ได้ event ครบตามที่ wave นั้นต้องการ (เช่น W1: คลิก 1 ครั้ง → เห็น `OnClick` แล้ว `BattleGridClicked` ตามลำดับ, argument เป็นช่องที่คลิกจริง)
- ถอด hook ได้ (set `coop.jitProbe=false` → event หยุดทันที, `Harmony.GetAllPatchedMethods()` ไม่มี method ของเราเหลือ)

### 6.4 Compat กับ LongYinThai — **NOT-RUN (ยังตรวจไม่ได้)**
- `compat-notes.md` **ไม่มีในเครื่อง** (ไม่มีไฟล์ใน `UserData/`) → ยังยืนยันไม่ได้ว่า LongYinThai patch method ใดในรายการ §5
- วิธีตรวจก่อนติด W1 (ไม่ต้องเปิดเกมเพิ่มถ้าเปิดอยู่แล้ว): `python tools/coopctl.py compat_dump` → `UserData/compat-notes.md` แล้ว diff กับรายการ §5
- ถ้าพบว่า LongYinThai patch method เดียวกัน: Harmony รองรับ postfix หลายตัวอยู่แล้ว (ของเราอ่านอย่างเดียว ไม่แก้ `__result`) — แต่ **ต้องรายงานก่อน** ไม่ตัดสินเอง

---

## 7. เอกสารที่แก้ในก้าวนี้ (เจอข้อขัดกับความจริง → แก้ ไม่เงียบ)

| ไฟล์ | แก้อะไร | เหตุ |
|---|---|---|
| `S1.5-plan.md` §3 G3 | ระบุห้ามใช้ `BattleUnit.moved/attacked` (0/82,695) ให้ใช้ `nowActiveUnitMoved/Attacked` และติดป้าย GUESS จนผ่านด่านเทียบ UI ตอนผู้เล่นเดินเอง (L3) | ผลวัด S1.5-b ขัดกับสมมติฐานเดิม |
| `S1.5-plan.md` §4 `stateHash` | เปลี่ยนองค์ประกอบเป็น turn flags + เอาต่อ-unit `moved/attacked` ออก พร้อมหมายเหตุว่า build `d5bf4de` ยังไม่ตรง | ผลวัด S1.5-b |
| `battle-notes.md` §1.1 / §1.2 / §10 | `battlePaused` ไม่ได้ true ตอนรอ input (0/82,695) — แก้ข้อความเดิมที่อ้าง [RUNTIME] | ผลวัด S1.5-b ขัดกับเอกสาร |
| `battle-notes.md` §2.6 (ตาราง RVA) | `SetAllAuto(bool auto)` มีพารามิเตอร์ 1 ตัว | dump.cs:363654 |
| `evidence/S1.5-20261020-141903/S1.5a-report.md` §7 (ใหม่) | อธิบายรวมว่า `PlayerprefData.dat` ของจริงต่างจาก baseline S1 เพราะเกมถูกเปิดบนเซฟจริง 14:24/14:28 **ไม่ใช่การเขียนของ mod** | คำสั่งผู้ใช้ |

---

## 8. Diff ที่เสนอสำหรับ build ถัดไป (ยังไม่แก้ — ขออนุมัติ)

```diff
--- a/src/LongYinCoop/S1.5/TurnGate.cs
+++ b/src/LongYinCoop/S1.5/TurnGate.cs
@@
-        public const string HashVersion = "battleframe/v1";
+        public const string HashVersion = "battleframe/v2";   // v2: turn flags แทน unit moved/attacked
@@ hash ต่อยูนิต
-            unitLines.Add(string.Join("|", stableId, side, teamIndex, row, col, hp3,
-                                      alive ? "1" : "0", u.moved ? "1" : "0", u.attacked ? "1" : "0"));
+            unitLines.Add(string.Join("|", stableId, side, teamIndex, row, col, hp3,
+                                      alive ? "1" : "0"));
@@ ระดับเทิร์น
+        HashText.Append("turnMoved=").Append(turnMoved ? 1 : 0).Append('\n');
+        HashText.Append("turnAttacked=").Append(turnAttacked ? 1 : 0).Append('\n');
```

```diff
--- status JSON (ชื่อฟิลด์)
-  "moved": false, "attacked": false,
+  "unitMoved": false, "unitAttacked": false,   // คงไว้เพื่อ audit แต่ไม่ใช่ตัวตัดสิน (ตาย: 0/82,695)
+  "turnMoved": ..., "turnAttacked": ...        // ตัวตัดสิน (มีอยู่แล้วใน build ปัจจุบัน)
```
ผลกระทบ: `tools/compare_status_frame.py` ต้องอ่าน `unitMoved/unitAttacked` แทน `moved/attacked`; ต้อง rebuild + deploy DLL ใหม่ (ต้องอนุมัติแยก) — และจะทำให้ `stateHash` เปลี่ยนตาม `HashVersion` (ต้อง rebaseline negative-test ก่อน S1.5-f)

---

## 9. สิ่งที่ยังไม่รู้ / ต้องวัดด้วย hook (NOT-RUN ทั้งหมด)

1. ใครเรียก `GridUnitController.OnClick()` (J4) — ต้องวัด
2. `BattleGridClicked` ถูกเรียกจริงตอนคลิกในศึกไหม + argument เป็นช่องที่คลิกจริงไหม (J1) — ต้องวัด
3. ลำดับ call จริงของ move 1 ครั้ง และ attack 1 ครั้ง (J2) — ต้องวัด
4. ต้องเขียน field (`selectedGrid`, `attackComfirmed`, `nowActiveState`) ก่อนเรียก entry ไหม (J3) — ต้องวัด; ถ้าใช่ = หยุดถาม
5. `battlePaused` ถูกตั้งที่ไหนนอกจาก `BattlePauseButtonClicked` (J6) — ต้องวัด
6. LongYinThai patch method ใดในรายการ §5 — ต้อง `compat_dump`
