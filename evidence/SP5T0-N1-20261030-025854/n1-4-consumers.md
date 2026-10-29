# N1-4 — enumerate consumer ของ `forceLevelData` (static — dump.cs + หลักฐาน disasm เดิม + bounded scan ใหม่)

> แผน plans/sp5-npc-reduce-plan-draft.md §2 N1-4 · แหล่ง: `Il2CppDumper/dump.cs` (ประกาศล้วน) ·
> `evidence/SP5body-20261026-20261026-232523/` (target-bodies.disasm.txt) ·
> `evidence/SP5refs-20261026-235218/sp5refs-report.md` (สแกน writes ทั้งไบนารี) · ผลวัด T0 (N1-3)

## ตาราง consumer (confidence ตามหลักฐาน)

| บทบาท | method | หลักฐาน | ความเชื่อมั่น |
|---|---|---|---|
| **WRITER (สร้างค่า)** | `GameDataController.LoadAllGameData` — สร้างตารางจาก TextAsset CSV ผ่าน `LTCSVLoader` (body @0x180C1F9C0, call site จริง @0x180C20599) — เขียน field +0x40 | SP5body report §"forceLevelData" + SP5refs สแกน `mov [reg+40h],reg` ทั้งไบนารี = จริง 4 site (ที่เหลือ 195 เท็จ) | **OBSERVED** |
| **READER (ยืนยันแล้ว)** | `GameController.GenerateHero` — ลูป quota อ่าน `forceLevelData[n].heroNum[edi]` (r14 = Instance.forceLevelData @0x1807ACE26–ACEB5 แล้ว `get_Item`) | SP5refs disasm + **T0 จริง: quota รายสำนัก ราย band fit ตาราง heroNum 25/30 สำนัก ภายใน [+0,+1] (N1-3)** | **OBSERVED (static + runtime ตรงกัน)** |
| READER (unattributed candidates) | read `[reg+40h]` ใน bodies ที่ถอดแล้ว ณ 0x1807AC6DF (ก่อน GenerateHeroData A) · 0x1807AE1E0 (ท้าย GenerateHero) · 0x1807CD1EE (ใน `ManageHeroAutoQuitAndEnter`) | bounded scan `target-bodies.disasm.txt` วันนี้ — **offset 0x40 ใช้ร่วมหลายคลาส อ่าน `[rax/rbx+40h]` เปล่า ๆ ระบุเจ้าของไม่ได้ ต้องถอด register provenance ต่อ** | **UNKNOWN** — ไม่บล็อก A1 แต่ต้องเคลียร์ก่อนอ้างว่า "GenerateHero เป็น consumer เดียว" |
| READER (นอก bodies ที่ถอด) | method อื่นทั้งหมดของ `GameDataController` (RVA surface ยาว) และคลาสอื่น — shared-generic ของ `List<ForceLevelData>::get_Item/Add` ไม่มี MethodInfo slot ครบ (SP5refs) ⇒ สแกนด้วย address เดี่ยวไม่ได้ | SP5refs ข้อจำกัด catalog | **UNKNOWN** (design §8.1 ข้อ 1 คงเดิมบางส่วน) |

## ผลต่อ A1 (ย่อ heroNum ใน RAM)

1. ค่าที่ A1 ย่อ = **RAM ของ process เท่านั้น** — `LoadAllGameData` โหลดจาก CSV ใหม่ทุกครั้งที่สร้างโลก
   ⇒ ค่าตารางกลับคืนเองทุกรอบ (แผน §6 rollback ไม่ต้องแตะไฟล์จริง — ตรง design §4 หมายเหตุร่วม)
2. consumer ที่ยืนยันมี consumer เดียว = ลูป quota ของ `GenerateHero` — A1 จึงมีผลเฉพาะจำนวนสมาชิกสำนัก
   เริ่มโลก (562) ตาม N1-3
3. unattributed candidates 3 จุด + reader นอก bodies = ความเสี่ยงคงเหลือของ A1 — การรับมือในแผน:
   T1 (normal mode) / T4 (ลดตรง band) / T5 (ไม่เด้งกลับ) / T8 (save-load) จะจับพฤติกรรมผิดปกติ
   ถ้ามี consumer อื่นจริง; ถ้าต้องการปิดก่อน runtime = ถอด provenance 3 จุดเพิ่ม (**งาน disasm
   อนุมัติแยก — ไม่อยู่ใน N1**)

## สิ่งที่ N1-4 ปิด / ไม่ปิด

- **ปิด:** writer เดียว (LoadAllGameData/CSV) · reader ยืนยัน = GenerateHero (static+runtime ตรงกัน) ·
  ขอบเขตผลของ A1 ต่อตารางที่ย่อ (RAM-only, คืนอัตโนมัติเมื่อโหลดใหม่)
- **ไม่ปิด (คง UNKNOWN):** attribution ของ 3 read-candidates + method นอก bodies · ไฟล์ CSV ต้นทาง
  (SP5refs หาไม่เจอใน 586 TextAsset — ค่าจริงวัดได้จาก T0 แล้วจึงไม่บล็อก A1)
