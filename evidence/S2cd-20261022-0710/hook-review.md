# Phase B — ChangeDay hook safety review (2026-10-22)

ไฟล์ที่ตรวจ: `tools/changeday_hook.js` (commit `c4732f3` + การแก้จากการตรวจรอบนี้)
โมดูลนี้เป็น **observer อย่างเดียว** — ตั้งใจใช้ตอบคำถาม "`>>` เรียก `ChangeDay` กี่ครั้ง ด้วย delta เท่าไร"

## 1. ChangeDay — signature (DUMP)

ที่มา: `Il2CppDumper/dump.cs` (build ที่ปักหมุดใน `VERSION.md`)

```
dump.cs:376432   public class GameController : MonoBehaviour // TypeDefIndex: 8374
dump.cs:376737   public void ChangeDay(int deltaDay) { }
dump.cs:376743   public void ChangeDay() { }
```

- **DUMP**: มี 2 overload — `ChangeDay(int)` และ `ChangeDay()`; ทั้งคู่ instance method (ไม่มี `static`)
- **DUMP**: มีเมธอดข้างเคียงที่ห้ามสับสน — `ChangeDayDirect(int)` (376735), `ChangeMonthDirect(int)`, `ChangeYearDirect(int)`, `ChangeHour(float)`
- **RUNTIME (ยังไม่วัดในรอบนี้)**: RVA/VA จริง + ตัว body — ต้องรอ `__cd.inspect()` (Phase C)

## 2. Checklist ตามที่สั่ง

| ข้อ | ผล | หลักฐาน |
|---|---|---|
| `Interceptor.attach` เท่านั้น | **PASS** | `grep -nE "Interceptor\.(attach\|replace\|revert)"` ⇒ เจอ `attach` บรรทัดเดียว (197) ไม่มี `replace`/`revert` |
| ไม่มี `Method.invoke` | **PASS** | ไม่มี `.invoke(` และไม่มี `NativeFunction` ในไฟล์ (grep = 0 hit) |
| ไม่มี `Interceptor.replace` | **PASS** | ตามข้อแรก |
| ไม่เขียน memory | **PASS** | `grep -nE "\.value\s*=[^=]\|Memory\.(write\|writePointer\|writeUtf8\|protect)\|\.write\(\|ptr\("` ⇒ **0 hit**; ทุกการอ่านเป็น getter `.value` / `.content` |
| ไม่เปลี่ยน game state | **PASS (โดยโครงสร้าง)** | ไม่มี path ที่เรียกเมธอดเกม ไม่มีการเขียนฟิลด์; `inspect/dump` อ่านอย่างเดียว; `snapshot()` อ่านฟิลด์เท่านั้น |
| argument capture ถูกต้อง | **PASS หลังแก้** — เดิม **ไม่พอ** (ดู §3.1) | บังคับ `isStatic === false` + param type `System.Int32`; arg index = 1 |
| snapshot ปลอดภัย | **PASS-with-limitation** (ดู §3.3) | ทั้งก้อนอยู่ใน `try/catch` คืน `{error}`; `hook()` ปฏิเสธถ้า snapshot error; แต่ `Il2Cpp.gc.choose()` หนักและยังไม่ยืนยันการอ่าน `List<T>` |
| unhook ถูกต้อง | **PASS หลังแก้** — เดิมมีช่องนับซ้ำ (ดู §3.2) | detach ทุก listener, `hooked=false`; `reset()` ปฏิเสธขณะ hooked; `hook()` ปฏิเสธถ้ายังมี count ค้าง |

## 3. ช่องโหว่ที่เจอและแก้แล้ว (สำคัญ)

### 3.1 `args[1]` ไม่ถูกตรวจ calling convention
เดิมเลือก overload ด้วย `parameterCount === 1` อย่างเดียว ⇒ ถ้า overload นั้นเป็น `static` (หรือ type ไม่ใช่ int) ค่าที่ log จะเป็น garbage เงียบ ๆ ซึ่งทำลายคุณค่าหลักฐานทั้งรอบ
แก้: `pickTarget()` ตรวจ **ชื่อ + จำนวนพารามิเตอร์ + type = `System.Int32`** และ `hook()` ปฏิเสธถ้า `isStatic !== false`
หลักฐานประกอบ (อ่านโค้ดไลบรารี): `il2cpp-frida-mcp/_agent.js` → `Method.fridaSignature` ใส่ `"pointer"` เป็นอาร์กิวเมนต์แรก **เฉพาะ instance method** ⇒ สำหรับ `ChangeDay(int)` แบบ instance, `args[1]` = `deltaDay` จริง

### 3.2 นับซ้ำได้ / หลักฐานเจือกัน
เดิม `unhook()` ไม่ล้าง `count`/`log`/`before` ⇒ hook รอบสองจะรวมยอดกับรอบแรกและ `before` ของรอบเก่าค้าง
แก้: เพิ่ม `reset()` (คืนจำนวนที่ทิ้ง) และ `hook()` คืน `REFUSED: N call(s) already logged … call __cd.reset() first`

### 3.3 snapshot ยังเป็น GUESS
- ชื่อฟิลด์ (`worldData`, `worldTime`, `hour`, `Forces`, `Heros`, `resourceStore`, `WorkingUIController.*`) มาจาก **DUMP**
- วิธีเข้าถึง `List<T>` ผ่าน `field('_items')`/`field('_size')` เป็น **GUESS** — ยังไม่เคยรันจริงใน build นี้ (ค่าที่เคยวัดใน `area-20261022-0615` อ่านด้วย MCP `list_fields` ไม่ใช่ไฟล์นี้)
- `heroID === 0` เป็นสมมติฐาน "ฮีโร่ผู้เล่น" (**GUESS**) — ถ้าผิด `forceID`/`store` จะไม่มีความหมาย
- `Il2Cpp.gc.choose()` เดิน heap ทั้งก้อน ⇒ ต้องเรียก **นอก** hook เท่านั้น (ใน hook จะช้าและกวน body)
- แก้แล้ว: `hook()` **ปฏิเสธการติดตั้ง** ถ้า snapshot คืน `{error}` ⇒ จะไม่มีการเก็บ log ที่ไม่มี before/after คู่กัน

### 3.4 ข้อจำกัดที่ยังเหลือ (ยอมรับ ไม่แก้)
1. `onLeave` ผูก `doneAt` กับ entry ล่าสุด ⇒ ถ้าเรียกซ้อนกัน (re-entrant) เวลาจะสลับกัน — ตัวเลขนับ/`delta` ยังถูก, เวลาอาจผิด
2. `asserted` flags เป็น **tripwire ไม่ใช่ sandbox** — ค่าเป็น `false` ตายตัวและจะกลายเป็น `true` ก็ต่อเมื่อมีโค้ดในอนาคตตั้งค่าเอง ⇒ คำอธิบาย "refuse by construction" ในหัวไฟล์หมายถึง *ไม่มีโค้ด path ที่ผิดสัญญา* ไม่ใช่ *runtime enforcement*
3. `Interceptor.attach` ที่ `method_ptr` ของ IL2CPP อาจได้ **thunk** ไม่ใช่ body (ขึ้นกับ build/inlining) ⇒ `inspect()` เพิ่ม `thunkHint` (ดูคำสั่งแรก: ถ้าเป็น `b*` = กระโดดทันที) ต้องอ่านผลก่อนตีความ disassembly
4. ถ้ามี instrumentation อื่นติดอยู่ที่เมธอดเดียวกัน (เช่น `spike` ของ mod) จะ hook ซ้อน — build ปัจจุบัน mod ไม่ได้ patch `ChangeDay` แต่ **ยังไม่ได้ตรวจ** ว่าไม่มีใครอื่น patch
5. `dayAtInstall` (เดิมชื่อ `dayBefore`) เป็นค่าคงที่ที่อ่านครั้งเดียวตอนติดตั้ง — เปลี่ยนชื่อให้ตรงความจริงแล้ว (เดิมชื่อชวนให้เข้าใจว่าเป็น "วันก่อนเรียกแต่ละครั้ง")

## 4. Static audit ที่รันจริง

```
$ node --check tools/changeday_hook.js
syntax OK
$ grep -nE "Interceptor\.(attach|replace|revert)|\.invoke\(|Method\.invoke|NativeFunction" tools/changeday_hook.js
11:  4. __cd.hook()        -> Interceptor.attach, LOG ONLY (refuses static/ambiguous)
25:  - never calls Method.invoke / any game method
27:  - never uses Interceptor.replace
197:    const listener = Interceptor.attach(m.virtualAddress, {
$ grep -nE "\.value\s*=[^=]|Memory\.(write|writePointer|writeUtf8|writeByteArray|protect)|\.write\(|ptr\(" ...
(0 hit)
```

⇒ `attach` 1 จุด, `replace`/`invoke`/`NativeFunction` 0, memory write 0

## 5. สรุปคำตัดสิน

**ผ่านการตรวจในฐานะ read-only observer** โดยมีเงื่อนไข:
- ต้องรัน `inspect()` และรายงาน overload/param type/RVA/`thunkHint` ให้ผู้ใช้อนุมัติ **ก่อน** `hook()`
- ต้องรัน `snapshot()` หนึ่งครั้งนอก hook เพื่อยืนยันว่าไม่ error (ถ้า error ⇒ ห้าม hook)
- ระหว่างวัดห้ามกด `>>` เกิน 1 ครั้งต่อ hook 1 รอบ และต้อง `reset()` ก่อนรอบถัดไป
