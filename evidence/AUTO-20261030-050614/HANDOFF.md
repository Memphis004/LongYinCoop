# HANDOFF — session `AUTO-20261030-050614` (2026-10-30 05:04–05:07 +0700)

## โหมดและมติที่รับ

- โหมด: **offline เท่านั้น** — ห้าม deploy/เปิดเกม/split/แตะ `Mods\`, net.cfg, pref, เซฟ (ปฏิบัติครบ)
- มติที่ระบุชัด: **commit คำขอ N4 ที่ค้าง = `[อนุมัติ]`** (git add ราย path เท่านั้น)
- มติที่ bracket ยังไม่ได้เลือก: push mod `[อนุมัติ/ไม่อนุมัติ]` (เงื่อนไขแนบ: fetch ก่อน origin/master ≠
  `3131df8` = หยุด · push เฉพาะ `5edb41a`, `dbca3b0` [ตัดออกถ้าไม่ยอมรับ `5edb41a`]) · push tag
  `stage-N3` `[อนุมัติ/ไม่อนุมัติ]` · ข้อเท็จจริง: ที่มา `5edb41a` `[ของฉันเอง / ของ session อื่นที่ฉันปิดแล้ว /
  ไม่ทราบ]` · ที่มา push 03:29:26/04:36:03 `[ฉันเองหรือเครื่องมือของฉัน (ระบุ: …) / ไม่ทราบ]` —
  **ทั้งหมดยังว่าง** ⇒ ตาม fallback rule ที่บันทึกไว้ใน §4 (ใช้มาแล้วทั้ง session ก่อน ๆ): **ไม่ push,
  สถานะรอยืนยันคงเดิม**
- popup 3 ข้อ `1) [...] 2) [...] 3) [...]` = ครั้งที่ 12 (append testimony + §6)
- หยุดทันทีถ้า snapshot ไม่ตรง หรือเจอการแก้จาก session อื่น**ที่ยังไม่ได้ระบุ**ในข้อความมติ

## session-start checks (read-only, 05:04) — ผ่านครบ

| รายการ | ค่าที่วัด | เทียบ |
|---|---|---|
| เกมรันหรือไม่ | not running | ✅ |
| `GameAssembly.dll` / `global-metadata.dat` | `33c56def…` / `451311d4…` | ✅ VERSION.md |
| `Mods\LongYinCoop.dll` / `.pdb` | `6a94abb5…` / `856d3bed…` | ✅ ตรงมติ (ไม่ deploy ตามโหมด) |
| `MelonPreferences.cfg` | `976ccfb9…` (`Sp5T0 = false` บรรทัด 15) | ✅ |
| `net.cfg` | `12541f34…` (ไม่มี key `npcPolicy`) | ✅ ไม่แตะ |
| Save | 57 ไฟล์ `df8e5a11beed` + `leftovers: none` | ✅ EXACT baseline |
| mod repo | HEAD `dbca3b0` · origin/master `3131df8` (local นำ 2: `5edb41a`, `dbca3b0` — **ตรงรายการที่ผู้ใช้ระบุในข้อความมติเป๊ะ**) · tag `stage-N3` @ `3131df8` | ✅ |
| working tree | M testimony + M `plans/sp5-npc-n4-approval-request.md` (ค้างจากการหยุดรอบก่อน) + untracked เดิม | ✅ ไม่มีการแก้ใหม่ |
| game repo | HEAD `944ceda` · porcelain 24 = ชุดเดิม | ✅ |

**ไม่พบการแก้จาก session อื่นที่ยังไม่ได้ระบุ** — ทุกสิ่งที่ต่างจาก snapshot ก่อนหน้าอยู่ในรายการที่
มติระบุไว้แล้ว (`5edb41a`, push 2 ครั้ง)

## ทำมติ

1. **commit คำขอ N4 ([อนุมัติ]):** `git add plans/sp5-npc-n4-approval-request.md` (add ราย path เท่านั้น
   ตามมติ — ก่อน commit ผ่าน git status --porcelain + git log -1 → add → git diff --cached --name-only
   = 1 ไฟล์) → commit **`b58d553`** (1 ไฟล์ 19 insertions — append-only ตามหลักเดิม)
2. **push mod / push tag = ไม่ทำ** (bracket ยังว่างทั้งคู่ — ไม่ตีความแทนผู้ใช้; เงื่อนไข "เฉพาะ
   `5edb41a`, `dbca3b0`" จึงยังไม่ถูกใช้)
3. **บันทึก decisions-pending §4 + §6** (append) — รวม **หมายเหตุสำคัญ: `b58d553` เกิดหลังรายการ push
   ที่มติระบุ** ("เฉพาะ `5edb41a`, `dbca3b0`") ⇒ มติ push รอบหน้าต้องระบุชุด commit ให้ครบ (รวม
   `b58d553` หรือไม่)
4. popup ครั้งที่ 12 — testimony + §6 append · คำตอบคงตามครั้งที่ 5 · USER TESTIMONY ยังไม่มี

## สถานะปลายเซสชัน (05:07)

- mod repo: HEAD **`b58d553`** · local นำ origin/master (`3131df8`) **3 commits**: `5edb41a`,
  `dbca3b0`, `b58d553` · tag `stage-N3` @ `3131df8` local เท่านั้น · working tree = M testimony +
  untracked เดิม (คำขอ N4 ถูก commit แล้ว)
- เกม/Mods/cfg/net.cfg/เซฟ **ไม่ถูกแตะทั้งเซสชัน** (hash ตรง snapshot เริ่มเซสชันทุกตัว)

## สิ่งที่รอผู้ใช้

1. **ยืนยันข้อเท็จจริง 2 ข้อ** (bracket ยังว่าง): ที่มา `5edb41a` · ที่มา push 03:29:26/04:36:03
2. **มติ push mod + push tag** — ระบุ `[อนุมัติ]` ชัด + ชุด commit ให้ครบ (ตอนนี้ local นำ 3: `5edb41a`,
   `dbca3b0`, `b58d553`) + เงื่อนไข origin อ้างค่าจริง (`3131df8`) + push tag แยกหัวข้อ
3. **N4 วลีสั่ง** — band T4 [280, 350] ยอมรับ/แก้ · จำนวนรอบ (default 3) · deploy จะใช้ artifact
   `ba1e1dd1…` (census field T2 ครบ) — N5–N6 ยังไม่อนุมัติ
4. ค้างเดิม: V2-A ทาง (ก) · D3–D10 · popup USER TESTIMONY · `S2-plan.md §13` rev2 · VPN/guest/Q9 ·
   E4 วัดซ้ำความละเอียดสูง
