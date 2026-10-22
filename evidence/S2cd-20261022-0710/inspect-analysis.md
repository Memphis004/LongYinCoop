# `__cd.inspect()` — ChangeDay ใน build จริง (2026-10-22 07:16)

- เป้าหมาย: เปิดเกม (ผู้ใช้เปิดเอง, pid 26188) → Frida attach → รัน **`inspect()` อย่างเดียว**
- ไม่ติด hook · ไม่กด `>>` · ไม่เรียก `ChangeDay` · ไม่แตะ pref/เซฟ
- ไฟล์ดิบ: `inspect-raw.json` (เขียนจากในเกมด้วย `File` API = **ไม่ผ่านการพิมพ์มือ**, 2,197 bytes) · `inspect-result.txt` = สำเนาที่มีหัวกำกับ (ตรวจแล้วว่า `json.loads` ของทั้งสองเท่ากันทุกฟิลด์)

## 1. Signature / ตำแหน่ง (RUNTIME)

| overload | param | type | RVA | VA | static |
|---|---|---|---|---|---|
| `ChangeDay` | 1 | `System.Int32` | `0x7925f0` | `0x7ffeba8825f0` | `false` |
| `ChangeDay` | 0 | — | `0x790e30` | `0x7ffeba880e30` | `false` |

- ตรงกับ DUMP (`dump.cs:376737` / `:376743`) — ไม่มี overload ที่สาม
- `thunk` = **"first insn looks like a body"** ⇒ `method_ptr` ชี้ตัว body จริง ไม่ใช่ trampoline (ดูก่อนหน้านี้ว่าเป็นความเสี่ยงข้อ 3.3 ใน `hook-review.md`)
- arg slot: x64 fastcall ⇒ `rcx`=this, `edx`=deltaDay ⇒ `args[1]` = deltaDay **ยืนยันได้จาก disassembly เอง** (ดู §2 บรรทัด 1–6): `test edx, edx` / `mov ebx, edx` และ `mov rdi, rcx` (this)

## 2. Disassembly (40 คำสั่ง, ตัดมาเฉพาะส่วนที่มีความหมาย)

```
0x…25f0  test edx, edx            ; deltaDay <= 0 ?
0x…25f2  jle  0x…261d             ;  --> ret (ไม่ทำอะไรเลย)
0x…25f4  mov  [rsp+8], rbx
0x…25f9  push rdi
0x…25fa  sub  rsp, 0x20
0x…25fe  mov  ebx, edx            ; ebx = deltaDay   (ตัวนับ)
0x…2600  mov  rdi, rcx            ; rdi = this
0x…2603  xor  edx, edx            ; <-- จุดเริ่ม loop
0x…2605  mov  rcx, rdi
0x…2608  call 0x7ffeba880e30      ; === ChangeDay()  (RVA 0x790e30 ตรงกับ overload 0 พารามิเตอร์เป๊ะ)
0x…260d  dec  ebx
0x…260f  test ebx, ebx
0x…2611  jg   0x…2603             ; loop กลับ
0x…2613  mov  rbx, [rsp+0x30]
0x…2618  add  rsp, 0x20
0x…261c  pop  rdi
0x…261d  ret                      ; <-- จบฟังก์ชัน
0x…261e  int3
0x…261f  int3
0x…2620  push rbx                 ; ฟังก์ชันถัดไปใน image (ไม่ใช่ ChangeDay) — ใช้ xmm1 (float)
```

## 3. Side-effect analysis

| ข้อ | ผล | ระดับ |
|---|---|---|
| `ChangeDay(int)` เขียนฟิลด์เอง | **ไม่เขียน** — ทั้ง body มีแค่ `mov` ของ register/stack, `test/dec/jg`, `call`, `ret` ไม่มี `mov [rcx+..]` / ไม่มี static store | RUNTIME (disasm) |
| จำนวนรอบ | `max(deltaDay, 0)` รอบพอดี (`test/jle` + `dec/jg`) | RUNTIME (disasm) |
| ทำอะไรต่อรอบ | เรียก **`ChangeDay()` (0 พารามิเตอร์) เท่านั้น** | RUNTIME (call target = VA ของ overload ที่ 2) |
| side effects ที่แท้จริง | = side effects ของ `ChangeDay()` × deltaDay — **ยังไม่รู้** เพราะยังไม่ได้อ่าน body ของตัวนั้น | UNKNOWN (ต้อง inspect ตัวที่ 2) |
| `deltaDay <= 0` | ไม่ทำอะไรเลย (guard มากัน infinite loop / ค่าติดลบ) | RUNTIME |
| ฟังก์ชันถัดไปที่ `0x…2620` | เป็นคนละเมธอด (มี `push rbx`/`sub rsp,0x30` ของตัวเอง + ใช้ `xmm1` float) — น่าจะ `ChangeHour(float)` แต่ **ยังไม่ยืนยัน** | GUESS |

## 4. ผลต่อแผนวัด (สำคัญ — เปลี่ยนสมมติฐาน)

1. **`ChangeDay(int)` เป็น wrapper บาง ๆ ไม่ใช่ตัวที่แก้โลก** ⇒ ถ้า hook เฉพาะตัวนี้ เราจะรู้แค่ว่า "มีใครเรียกด้วย delta เท่าไร และกี่ครั้ง" ซึ่งพอตอบคำถามหลัก (`>>` เรียก ChangeDay ไหม) และให้ผลรวมวัน = `sum(delta)` แบบตรงไปตรงมา
2. แต่จำนวน **วันจริงที่เดิน** = `sum(delta)` ของ wrapper = จำนวนครั้งที่ `ChangeDay()` ถูกเรียก **ถ้า** ไม่มีใครเรียก `ChangeDay()` ตรง ๆ (ข้าม wrapper) — และนี่คือกรณีที่ต้องแยกให้ได้ เพราะผลวัดเดิม (ผลผลิตจ่ายครบ N วัน แต่ปฏิทิน +1) ชี้ว่าอาจมีคนเรียก `ChangeDay()` ตรง ๆ ในลูป `skipping`
3. ทางที่ดีที่สุดคือ **attach 2 จุดพร้อมกัน** (`ChangeDay(int)` + `ChangeDay()`) แล้วเทียบ: ถ้า `sum(delta)=1` แต่ `ChangeDay()` ถูกเรียก N ครั้ง ⇒ ลูป skip เรียกตัว per-day เอง; ถ้า `sum(delta)=N` และ `ChangeDay()` ถูกเรียก N ครั้ง ⇒ ทุกอย่างผ่าน wrapper
4. ยังไม่ยืนยันว่า `>>` เรียกอะไรเลย — ต้อง hook ก่อนถึงจะรู้ (ห้ามเดา)

## 5. ข้อสังเกตเรื่องเครื่องมือ (ไม่ใช่ของเรา)

- ไลบรารี `il2cpp-frida-mcp` ตอนอ่าน `virtualAddress` ครั้งแรกจะเรียก `System.Reflection.Module..cctor()` เพื่อหา offset ของ `method_ptr` (อ่านได้จาก `_agent.js` ของ repo นั้น) ⇒ **การ invoke นี้มาจากไลบรารี ไม่ใช่จากโมดูลของเรา** และเป็น static ctor ของ corlib ไม่ใช่ของเกม — บันทึกไว้เพื่อความโปร่งใส (ระดับ: อ่านโค้ดไลบรารี, ไม่ได้วัด)
- log ของ MelonLoader รอบนี้ (`26-10-22_7-13-50.log`): `[S2]` = **0 บรรทัด** ⇒ mod ไม่ได้ sampling อยู่ ไม่มีอะไรแย่ง pipe/เขียน state; `ERROR|Exception` = 12 บรรทัด เป็น `TypeLoadException` ของ `UnityEngine.UIElements*` จาก Interop type cache ทั้งหมด (ไม่เกี่ยวกับ `GameController`)
