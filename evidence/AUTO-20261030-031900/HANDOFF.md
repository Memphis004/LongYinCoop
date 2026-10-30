# HANDOFF — session offline stage N1 ของแผน SP5-NPC `2026-10-30 03:19` (บนดิสก์ ไม่ commit ตาม convention)

> คำสั่งต้นทาง: ข้อความผู้ใช้ 2026-10-30 (offline เท่านั้น) · มติ: อนุมัติเฉพาะ stage N1 (N2–N6
> ยังไม่อนุมัติ ห้ามเขียนโค้ดนอกขอบเขต N1) · popup `[ไม่มีคำตอบเพิ่ม]` · push `[ไม่ push รอบนี้ /
> อนุมัติ...]` = placeholder ไม่มีคำตอบ · **ห้ามเสนอเลือก L1/L2/L3 จนกว่าผู้ใช้สั่ง**

## สรุปผูกมัด 5 บรรทัด

1. **session-start ผ่านครบ** — เกมไม่รัน · hash เกม EXACT · `Mods\LongYinCoop.dll = 6a94abb5…` ·
   `Sp5T0=false` · เซฟ 57/57 `df8e5a11beed` + leftovers none · git ตรง snapshot (mod `f088736` +
   dirty/untracked ทุกตัวระบุตัวตนได้จาก session ก่อน; game `944ceda`) — ไม่มีการแก้จาก session อื่น
2. **N1 = เสร็จครบ 5 งาน** (offline ล้วน — ไม่ deploy/เปิดเกม/split) — commit **`44b7529`** (โค้ด 9 ไฟล์
   +327/−14) + **`3493bfb`** (evidence 5 ไฟล์ + สถานะแผน):
   - **N1-1** probe comparator: `NormalizePatchId` + เทียบ format-insensitive — ปิดบั๊ก allMatch=false
     ของรอบ T0 (harness +6 checks รวมคู่ string จริงจากรอบนั้น)
   - **N1-2** `tools/sp5t0_audit.py` schema จริง: `h` เล็ก + `perForce` list 30 ช่อง (+dict เดิม) +
     skip world-null + **key ด้วย label `at`** (เพราะ worldcount เขียน t=0 — พบระหว่างแก้) + dH/dT
     observed · tests 6→10 OK · รัน log จริงได้ (E5/E7 CLEAN, dH=883, dT=87)
   - **N1-3** attribute ลูปจาก NDJSON เดิม: generic 712 = **31 runs ต่อเนื่องราย force** (0..29 ละ
     10–24 + เร่ร่อน −1 ละ 150) · **quota รายสำนักราย band มาจากตาราง heroNum** (25/30 สำนัก fit
     แถวใดหนึ่งภายใน [+0,+1] ต่อ band ≥5/6) ⇒ **A1 ครอบ 562/883 (63.6%)** · **Light 0.5 คาดลด
     ~281 ตัว ≈ 31.8% — ตรง ~32% ของ design** (สมมติ linear — UNKNOWN จนวัดจริง T4)
   - **N1-4** consumers ของ `forceLevelData`: writer เดียว = LoadAllGameData/CSV · reader ยืนยัน =
     GenerateHero · read-candidates unattributed 3 จุด = UNKNOWN (ไม่บล็อก A1, disasm เพิ่ม = อนุมัติแยก)
   - **N1-5** `Sp5NpcPolicy` pure (ParseValue fail-closed + Decide) + key `npcPolicy` ใน
     S2NetSettings/Parse (default "off", ค่าแปลกไม่ทำไฟล์พัง) + tests grid 80 ชุด + parser
3. **เทสต์ ≥ เดิมทุกชุด:** C# **673 PASS / 0 FAIL** (เดิม 650) · python **53 OK + discover 110 OK**
   (เดิม 106) · static **CLEAN** · build **0W/0E determinism ×2** · artifact **DLL `b6d24d34…`
   (367,104 B) / PDB `4632b607…`** — provenance READY `evidence/SP5T0-N1-20261030-025854/` ·
   **ยังไม่ deploy — `Mods\` ยืนยันปลาย session ยังเป็น `6a94abb5`**
4. **popup = ครั้งที่ 6 ไม่มีคำตอบ** (`[ไม่มีคำตอบเพิ่ม]`) — append testimony แล้ว (uncommitted) ·
   สถานะคงตามครั้งที่ 5 (คำตอบ agent จากหลักฐาน + "ไม่แน่ใจ")
5. **push = ไม่ทำ** — มติมาเป็น placeholder `[ไม่ push รอบนี้ / อนุมัติ…]` = ยังไม่ได้เลือก ⇒ local นำ
   origin **2 commits**: `44b7529` (N1 code) + `3493bfb` (N1 evidence+plan) · origin/master = `f088736`,
   origin/main = `944ceda` (synced ทั้งคู่จากรอบก่อน) · decisions-pending append ครบ (§2 N1 ผล, §4 push,
   §6 popup ครั้งที่ 6, §7.1/7.2 สถานะใหม่) — ไม่ลบเดิม

## ข้อจำกัด/UNKNOWN ที่ต้องจำ

- ผล N1-3 "linear ⇒ ~31.8%" เป็น**ข้อสันนิษฐาน** — ต้องวัดจริง T4 (อนุมัติ stage แยก) · mapping
  สำนัก→แถว heroNum ของ 5 สำนัก + เงื่อนไข band-5 + แหล่งจำนวนเร่ร่อน 150 = คง UNKNOWN
- probe build ใหม่ (`b6d24d34`) ยังไม่เคยรันบนเกม — allMatch fix ตรวจผ่านเฉพาะ harness offline
  (runtime ตรวจซ้ำตอน stage ที่ deploy จริง)
- N1-5 แค่ parser+predicate — **ยังไม่มี hook ใดต่อ predicate** (hook = N2 ซึ่งยังไม่อนุมัติ)

## ขั้นถัดไปที่รอมติ

1. อนุมัติ push 2 commits (`44b7529`, `3493bfb`) เมื่อต้องการ
2. อนุมัติ N2 (implement hooks) — ก่อนตัดสินต้องเลือกระดับ+ตัวคูณ และมติ refill + ยืนยัน key `npcPolicy`
3. ค้างเดิมทั้งหมดคงเดิม (V2-A (ก)/(ข), D3–D10, S2-plan §13, VPN/guest/60 ยูนิต, E4 ซ้ำ)
