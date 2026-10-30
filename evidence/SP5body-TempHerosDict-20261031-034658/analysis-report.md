# SP5body-TempHerosDict — disasm `WorldData$$OnDeserializedMethod` (N5 supplement · 2026-10-31)

- **session:** `AUTO-20261031-034658` · **mandate:** ข้อ 3 ของคำสั่ง 2026-10-31 (disasm ตามที่อนุมัติ — เสริมรายงาน N5 เท่านั้น)
- **ขอบเขต:** วิเคราะห์ body เดียว — โครงสร้าง loop ที่เติม HerosDict/TempHerosDict, เงื่อนไขกรอง, callees AddTempHero/Dictionary.Add · **ห้าม/ไม่ได้เปลี่ยนมติ (ก) หรือเกณฑ์ใด · ไม่ได้ย้าย tag `stage-N5` · ไม่ได้แก้ `n5-report.md`**
- **pipeline:** สำเนาของ `evidence/TIMEstatic-20261027-051647/{time_static_common.py,disasm_time_bodies.py}` (ไฟล์ต้น mtime Oct 27 ไม่ถูกแตะ) + **เพิ่ม 1 แถว** `("WorldData_OnDeserializedMethod", "WorldData$$OnDeserializedMethod")` ใน BODIES เท่านั้น
- **การรัน (read-only):** script อ่าน `GameAssembly.dll` เป็น bytes (`Path.read_bytes` + pefile parse) — **ไม่มีการโหลดเป็นโมดูล/execute โค้ดเกม · ไม่เขียนไฟล์เกม** · เขียนเฉพาะโฟลเดอร์นี้ (`bodies/*.txt` + `floats-observed.tsv`) · exit 0
- **run summary (stdout):** 30 bodies ถูกสร้าง (regenerate ครบตาม BODIES เดิม + แถวใหม่) · `!! not found: WorkingUIController$$Start` = พฤติกรรมเดิมของ pipeline (แถวเดิม, resolve ไม่เจอใน script.json — ไม่เกิดจากแถวใหม่) · **`WorldData_OnDeserializedMethod: 0x9CF0A0..0x9CF2A0 (129 insns, 11 pool reads)`**

## 1) ตำแหน่งและขอบเขต

| รายการ | ค่า | ป้าย |
|---|---|---|
| method | `WorldData$$OnDeserializedMethod(StreamingContext)` | OBSERVED (script.json) |
| toolchain RVA | **0x9CF0A0** | OBSERVED (script.json = ตรงที่สั่ง) |
| VA | **0x1809CF0A0** | OBSERVED (format dump.cs: `VA = RVA + 0x180000000` ทุก method — ยืนยันว่า 0x1809CF0A0 เป็น VA) |
| end(before) | **0x9CF2A0** = start ของ `WorldData$$OnSerializingMethod` (method ถัดไปใน script.json) | OBSERVED |
| ขอบเขต end เป็น approximation | **กรณีนี้ "แน่น"** — method นี้ไม่ใช่ method สุดท้ายของ class จึงไม่เข้าเคส approximation ของ pipeline · ทั้ง 0x200 bytes = code จริง: `ret` สุดท้ายที่ 0x9CF299, throw-tail 0x9CF29A–F (`call 0xD6610` + `int3`) — ไม่มี padding ที่ถอดเพี้ยน | OBSERVED (โครง) + INFER (ความหมาย throw-tail) |

## 2) โครงสร้าง body (รายช่วง — ป้ายรายประโยค)

### 2.1 prologue + class-init guard (0x9CF0A0–0x9CF0F6)

- push rbx/rbp/rdi, `sub rsp, 0x20`; `rbx = rcx` (this) — OBSERVED
- `cmp byte ptr [0x1E9F84D], 0` → ถ้ายังไม่ init: `call 0xD64A0` **5 ครั้ง** ด้วย class-metadata pointer ต่างกัน ([0x1D87D18]/[0x1D91B90]/[0x1DAF100]/[0x1D91C10]/[0x1DAF180]) แล้วตั้ง flag byte = 1 — OBSERVED (โครง)
- ความหมายของ `0xD64A0` = helper init class/metadata ของ il2cpp (guard idiom เดียวกับทุก method ที่ถอดใน repo) — **INFER** (helper ไม่มีชื่อใน script.json)
- pool reads ทั้ง 11 จุดของ body = metadata/class pointer + float-garbage decode — **ไม่มี float constant จริงที่ใช้ตัดสินอะไร** — OBSERVED (จาก floats-observed.tsv แถวของ body นี้)

### 2.2 Loop A — rebuild `WorldEventDatas` จาก `WorldEventDatasSaveRecord` (0x9CF0F7–0x9CF1AF)

- field ที่ใช้ (ชื่อจาก dump.cs:367605–367609 · WorldData TypeDefIndex 8181): `[rbx+0x78]` = **`WorldEventDatasSaveRecord` (List<int>)** · `[rbx+0x80]` = **`WorldEventDatas` (List<EventData>)** — OBSERVED (dump.cs)
- loop: index `ebp` จาก 0 ถึง `size` (`[list+0x18]`), เดิน int[] ด้วย `r14 += 4` — OBSERVED
- null-check list/items ทุกจุด → ถ้า null = กระโดด throw-tail 0x9CF29A — OBSERVED · OOB → `ThrowHelper$$ThrowArgumentOutOfRangeException` — OBSERVED (ชื่อจาก script.json)
- **เงื่อนไขกรองของ Loop A: ข้ามแถวที่ record < 0** (`cmp dword [r14+rax], 0; jl skip`) — OBSERVED (edi คงค่า 0 ตลอด loop)
- ต่อแถวที่ผ่าน: `id = List.get_Item(i)` (`call 0xD6750` — helper generic get_Item ตาม idiom il2cpp: rcx=list, edx=index, r8=class ptr — **INFER**) → `ev = WorldData$$GetEventSaveIDEvent(this, id, 0)` (0x9CD9D0) — **OBSERVED** (ชื่อจาก script.json) → `WorldEventDatas.Add(ev)` (call ที่ script.json ตั้งชื่อ `List<List<List<int>>>$$Add` — **generic sharing**: code เดียวกันถูกแชร์ระหว่าง instantiation, จุด call จริงคือ `List<EventData>$$Add` ที่ rcx=`[rbx+0x80]` — OBSERVED ชื่อ + INFER การตีความ)
- **ผล Loop A: `WorldEventDatas` (0x80) ถูก rebuild จาก id ที่ serialize ไว้ใน `WorldEventDatasSaveRecord` (0x78)** — สอดคล้อง dump.cs ที่ `WorldEventDatas` เป็น **[JsonIgnore]** (ไม่ถูกเซฟ ต้อง rebuild) ขณะที่ SaveRecord **ไม่มี [JsonIgnore]** (ถูกเซฟ) — OBSERVED (โครงจาก disasm) + OBSERVED (attribute จาก dump.cs)

### 2.3 Loop B — rebuild `targetEvent` รายแถวของ `worldPlotEventStartData` (0x9CF1B4–0x9CF27E)

- field: `[rbx+0xF0]` = **`worldPlotEventStartData` (List<WorldPlotEventStartData>)** — OBSERVED (dump.cs:367629) · ไม่มี [JsonIgnore] (ถูกเซฟ) — OBSERVED
- loop: index `edi`, เดิน item array ด้วย `r15 += 8` (element เป็น reference) — OBSERVED
- element = `items[i]` + null-check — OBSERVED
- **เงื่อนไขกรองของ Loop B: `element.targetEventSaveRecord` (field 0x34 ของ WorldPlotEventStartData) >= 0** (`cmp dword [rcx+0x34], 0; jl skip`) — OBSERVED (disasm) · ชื่อ field จาก dump.cs (class WorldPlotEventStartData: `targetEventSaveRecord // 0x34`, `targetEvent // 0x38`) — OBSERVED
- ต่อแถวที่ผ่าน: `ev = WorldData$$GetEventSaveIDEvent(this, element.targetEventSaveRecord, 0)` (0x9CD9D0 — get_Item ถูกเรียกซ้ำ 2 ครั้งผ่าน helper 0x2F80 ก่อนหน้า — **INFER** ชื่อ helper) → **เขียน `element.targetEvent (0x38) = ev`** (`lea rcx,[rsi+0x38]; mov [rcx],rax`) ตามด้วย `call 0xD6020` = write-barrier helper (idiom il2cpp หลัง store reference — **INFER**)
- **ผล Loop B: ไม่มีการเติม dict ใด ๆ — เป็นการผูก EventData reference กลับเข้า field ของแถวเอง** — OBSERVED

### 2.4 epilogue + throw tail (0x9CF283–0x9CF29F)

- restore registers + `ret` ที่ 0x9CF299 — OBSERVED · throw-tail 0x9CF29A (`call 0xD6610` = throw NullReference ตาม idiom — **INFER** + `int3`) — OBSERVED (โครง)

## 3) callees ทั้งหมดที่พบใน body (ครบ 129 insns — ไม่มี call อื่น)

| callee | จำนวน | ป้ายชื่อ | ป้ายความหมาย |
|---|---|---|---|
| `0xD64A0` | ×5 | ไม่มีใน script.json | INFER — class/metadata init helper |
| `WorldData$$GetEventSaveIDEvent` (0x9CD9D0) | ×2 | OBSERVED | — (`public EventData GetEventSaveIDEvent(int)` — dump.cs:367766) |
| `List<List<List<int>>>$$Add` (shared) | ×1 | OBSERVED (ชื่อจาก script.json) | INFER — generic sharing; จุดจริง = `List<EventData>$$Add` |
| `0xD6750` / `0x2F80` | ×1 / ×2 | ไม่มีใน script.json | INFER — generic `List<T>::get_Item` shared impls |
| `ThrowHelper$$ThrowArgumentOutOfRangeException` | ×2 | OBSERVED | — |
| `0xD6020` | ×1 | ไม่มีใน script.json | INFER — write barrier |
| `0xD6610` | ×1 | ไม่มีใน script.json | INFER — throw NullReference tail |

- **ไม่พบ**: `WorldData$$AddTempHero` (0x9CC280) · `WorldData$$RemoveTempHero` (0x9CFCC0) · `Dictionary`2$$Add` / Dictionary ctor ใด ๆ — **OBSERVED (ลบ — scan ครบทุก instruction)**

## 4) คำตอบตรงคำถามของ mandate

1. **โครงสร้าง loop ที่เติม HerosDict/TempHerosDict:** **ไม่มีอยู่ใน method นี้** — ไม่มี store ไปที่ `[this+0x50]` (Heros) / `[this+0x58]` (TempHeros) / `[this+0x268]` (HerosDict) / `[this+0x278]` (TempHerosDict) / `[this+0x280]` (tempHerosDictLock) เลยใน 129 instructions — OBSERVED (ลบ)
2. **เงื่อนไขกรอง (flag ที่ถูกเช็ก):** Loop A = `record < 0` แล้วข้าม; Loop B = `targetEventSaveRecord >= 0` ถึงจะผูก — OBSERVED · ไม่มีการอ่าน flag ชนิด bool/isRandomEnemy ใด ๆ ใน body นี้ — OBSERVED (ลบ)
3. **callees AddTempHero/Dictionary.Add:** **ไม่พบทั้งสอง** — OBSERVED (ลบ)

## 5) ผลที่ขัดกับบันทึกเดิม (รายงานตามจริง — ไม่เปลี่ยนมติ)

- **INFER ของ `audit-20261031-0314.md` §2 ที่ว่า "dict ถูก rebuild ตอนโหลดใน OnDeserializedMethod" ถูกหักล้าง** — method นี้ rebuild เฉพาะโครงสร้าง **event** (WorldEventDatas + targetEvent ของ worldPlotEventStartData) ไม่แตะ hero/dict เลย
- **ข้อเท็จจริงใหม่จาก dump.cs (OBSERVED, เสริม N5):** `Heros` (0x50) และ `TempHeros` (0x58) **เองก็เป็น [JsonIgnore]** — เซฟไม่เก็บทั้ง lists และ dicts ระดับ WorldData (สอดคล้องที่ GameSaveData.TempHeroList เป็นตัวเก็บจริง — audit §2) · พบ `tempHerosDictLock // 0x280` (lock object — บ่งชี้จุดเข้าถึง dict ที่กัน race)
- **ผลต่อ N5 (dictT(7–55) != listT(83) หลังโหลด):** กลไกที่เติม dictT บางส่วน **ยังเป็น UNKNOWN** — ตำแหน่ง rebuild ของ Heros/TempHeros/HerosDict/TempHerosDict ไม่ได้อยู่ที่นี่ (candidates ที่ยังไม่ตรวจ: load path ของ GameSaveData→WorldData, `AddTempHero` เอง, `.ctor`, method อื่นรอบคลาส — ตรวจเพิ่ม = สแกน static ต่อ อนุมัติแยก)
- **มติ (ก) และเกณฑ์ของ N5 ไม่ถูกเปลี่ยน** · tag `stage-N5` @ `cefdd86` ไม่ถูกย้าย · `n5-report.md` ไม่ถูกแก้ (รายงานนี้เป็น supplement แยก)

## 6) ข้อจำกัดของการวิเคราะห์รอบนี้

- ขอบเขต end ของ pipeline = next ScriptMethod start (0x9CF2A0) — ระบุตาม mandate; กรณีนี้ขอบเขตแน่น (ดู §1) แต่โดยทั่วไปยังเป็น approximation ของ pipeline
- ชื่อ helper (0xD64A0/0xD6750/0x2F80/0xD6020/0xD6610) ไม่มีใน script.json — ความหมาย = INFER จาก idiom il2cpp (รูปแบบ argument/ลำดับ call) ไม่ใช่สัญลักษณ์จริง
- ชื่อ method ของ call แบบ shared generic อ่านจาก script.json address เดียว — การตีความ instantiation จริงดูจาก register context (ระบุรายจุดแล้ว)
- วิเคราะห์เฉพาะ `OnDeserializedMethod` ตาม mandate — ไม่ได้สแกน caller/callee อื่น (GetEventSaveIDEvent body, AddTempHero body ยังไม่ถอด)
- ไฟล์: body = `bodies/WorldData_OnDeserializedMethod.txt` (129 insns) · pool reads = `floats-observed.tsv` (body นี้ 11 แถว) · scripts = สำเนา 2 ไฟล์ + 1 แถวที่เพิ่ม
