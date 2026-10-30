# HANDOFF — session offline: มติ runtime N3 + เอกสารคำขอ `2026-10-30 03:54` (บนดิสก์ ไม่ commit ตาม convention)

> คำสั่งต้นทาง: ข้อความผู้ใช้ 2026-10-30 (offline เท่านั้น) · มติคำตอบจริงครบ: **Light ×0.5** สำหรับ
> N3/T4 · **refill ไม่ใช้ในรอบแรก** · **key npcPolicy ยืนยัน default "off" ตั้งเฉพาะตอนรัน** ·
> push mod [อนุมัติ เงื่อนไข origin = `3493bfb`] · push game ไม่มีอะไรค้าง · popup `[...]` ×3
> ("ไม่แน่ใจให้เขียน 'ไม่แน่ใจ'") · ขั้นตอน: push → append มติ → **ร่างคำขอ N3 (ยังไม่ดำเนินการ)**

## สรุปผูกมัด 4 บรรทัด

1. **session-start ผ่านครบ** — เกมไม่รัน · hash เกม EXACT · `Mods\ = 6a94abb5…` · `Sp5T0=false` ·
   เซฟ 57/57 `df8e5a11beed` + leftovers none · git ตรง snapshot (mod `d8036f7`, game `944ceda`) —
   ไม่มีการแก้จาก session อื่น
2. **push mod รอบที่ 5 = สำเร็จ** — fetch ตรวจ origin/master = `3493bfb` ตรงเงื่อนไข → push
   `3493bfb..7396b94` FF = 3 commits (`9d0ff02` N2 code, `d8036f7` N2 evidence+plan, `7396b94`
   คำขอ N3 + สถานะแผน) → origin/master = `7396b94` · **game = ไม่มีอะไรค้าง** (origin/main =
   `944ceda` = HEAD ตรงมติ) · ไม่ force
3. **มติ runtime 3 ข้อ append ครบแล้ว** (decisions-pending §7.1 + แผน header/§8 — ข้อความเดิมคงไว้):
   Light ×0.5 (ตรง `Sp5NpcScales.LightHeroScale` ที่ implement อยู่แล้ว ไม่ต้องแก้โค้ด) · refill
   ไม่ใช้รอบแรก (= design L1 C-off — G4/G5 dormant ด้วยระดับ) · key ยืนยัน "off" ตั้งเฉพาะตอนรัน
   (T1 ยัง off; ตั้ง light เฉพาะ N4/T2–T4) · **popup ครั้งที่ 8**: slots `[...]` + คำสั่งเดิม —
   คำตอบคงตามครั้งที่ 5 (agent-evidence + ไม่แน่ใจ) append แล้ว uncommitted
4. **เอกสารคำขออนุมัติ N3 = ร่างแล้ว ยังไม่ดำเนินการ** — `plans/sp5-npc-n3-approval-request.md`
   (commit `7396b94`): ขอบเขต T1 normal mode gate off (npcPolicy คง off ตลอด stage; ตั้ง light
   เป็นของ N4) · 3 รอบสร้างโลกใหม่ · deploy `2b59e46c…` พร้อม .prev ของ `6a94abb5` · split
   `--expected-baseline 20261028-071340` · restore + rollback DLL ท้ายรอบ · **วลีสั่งที่เสนอ
   `เริ่ม N3 session`** (+ ตัวเลือก `พร้อม Sp5T0` สำหรับ census เชิงตัวเลข — ไม่พิมพ์ = T1 แบบ
   log-only/จอ) · ความเสี่ยงประกาศชัด (ครั้งแรกที่ gate hooks อยู่บนเกมจริง — ตรวจ ML log install
   ก่อนเล่น)

## สถานะปลาย session

- mod repo: HEAD `7396b94` = origin/master (synced) · dirty = `M user-popup-answers` (ครั้งที่ 8,
  uncommitted ตามขอบเขต) · untracked คงเดิม (decisions-pending.md, HANDOFF ×6, coop-test label,
  prep HANDOFF)
- game repo: HEAD `944ceda` = origin/main (synced)
- เกม/Mods\/net.cfg/pref/เซฟ **ไม่ถูกแตะเลยทั้ง session** (ตรวจซ้ำปลาย session)

## ขั้นถัดไป

1. **ผู้ใช้พิมพ์ `เริ่ม N3 session`** (หรือ `เริ่ม N3 session พร้อม Sp5T0`) ในเซสชันเกมแยก —
   agent ทำตามเอกสารคำขอขั้น 1–7 (deploy `2b59e46c` + .prev → split → ผู้ใช้เปิดเกม ×3 → ตรวจ
   T1 → คืนสภาพ restore+rollback `6a94abb5` → รายงานรายเกณฑ์ T1 ไม่ปิด stage เอง)
2. หลัง N3: N4 (T2–T4 ที่ Light ×0.5 — ตั้ง `npcPolicy = light` เฉพาะตอนรัน) = วลีสั่งแยกอีก
3. ค้างเดิม: นโยบาย refill ของโลกที่ลดแล้วเมื่อ gate ปิด (นอกรอบทดสอบ — §8 ข้อ 2), ตำแหน่ง
   STAGEPLAN, T9 ผูก VPN/guest, V2-A (ก)/(ข), D3–D10, S2-plan §13, E4 ซ้ำ, popup 3 ข้อ
