# HANDOFF — session offline stage N2 ของแผน SP5-NPC `2026-10-30 03:46` (บนดิสก์ ไม่ commit ตาม convention)

> คำสั่งต้นทาง: ข้อความผู้ใช้ 2026-10-30 (offline เท่านั้น — ห้าม deploy/เปิดเกม/split/แตะ Mods\,
> net.cfg, pref, เซฟ) · มติ: push mod [อนุมัติ เงื่อนไข origin = `f088736`] · push game
> "[ไม่มีอะไรค้าง / ตามสถานะจริง]" · มติ N2 3 ข้อ (bracket ยังไม่ได้เลือก — บันทึกตามจริง) ·
> แผน N2 [อนุมัติเฉพาะ N2 offline — N3–N6 ยังไม่อนุมัติ] · popup `[ไม่มีคำตอบเพิ่ม]`

## สรุปผูกมัด 5 บรรทัด

1. **session-start ผ่านครบ** — เกมไม่รัน · hash เกม EXACT · `Mods\LongYinCoop.dll = 6a94abb5…` ·
   `Sp5T0=false` · เซฟ 57/57 `df8e5a11beed` + leftovers none · git ตรง snapshot (mod `3493bfb`,
   game `944ceda`; dirty/untracked ทุกตัวระบุตัวตนได้) — ไม่มีการแก้จาก session อื่น
2. **push mod รอบที่ 4 = สำเร็จ** — fetch ตรวจ origin/master = `f088736` ตรงเงื่อนไข → push
   `f088736..3493bfb` FF = 2 commits (`44b7529` N1 code, `3493bfb` N1 evidence+plan) → origin/master
   = `3493bfb` · **game repo = ไม่มีอะไรค้าง** (origin/main = `944ceda` = HEAD ตรงมติ "ตามสถานะจริง")
3. **N2 = เสร็จครบ** (offline ล้วน) — commit **`9d0ff02`** (โค้ด 7 ไฟล์ +532) + **`d8036f7`**
   (evidence + สถานะแผน):
   - **G1** `GenerateHero` prefix ย่อ heroNum (typed write) + **finalizer restore ทุกทางออกรวม
     exception** (เขียนถูกจำกัดใน creation window) · **G4** postfix clamp `GetRecommendHeroNum`/
     `GetMaxHeroNum` (เฉพาะ Mid) · **G5** `WorldAddNewHero` deny ด้วย pattern bool-prefix +
     ref-result null-skip (**เฉพาะ Strong**) — **G2/A2 ไม่ implement** (แผนกำหนด "+G2 ถ้าอนุมัติ")
   - ตรรกะ pure ทั้งหมดใน `Sp5NpcRules` (เทสต์ได้) · gate fail-closed: Decide ต่อ invocation จาก
     net.cfg (poll ≤1 s) · idiom เดิมครบ (Missing++ all-or-nothing, owned-patch check, try/catch)
   - **บั๊กที่จับได้ก่อน build ผ่าน:** ร่างแรก G5 คืน null จาก prefix — contract ของ Harmony =
     null = ผ่านต่อ ไม่ใช่ skip ⇒ แก้เป็น bool+ref pattern (บันทึกใน provenance)
   - **พารามิเตอร์ที่ implement เลือก (บันทึกตามจริง แก้ได้ก่อน runtime):** rounding half-up
     (ผลตาราง T0: 88→50 เอ่อเหนือ linear 44), restore ท้ายรอบ, scale Light 0.5/Mid 0.25/Strong
     0.05/MidCeiling 0.5, install ตอนโหลด + Decide ต่อ invocation
4. **เทสต์ ≥ เดิมทุกชุด:** C# **693 PASS / 0 FAIL** (673 → +20 rules tests) · python **53 + 110 OK** ·
   static **CLEAN** · build **0W/0E determinism ×2** · artifact **DLL `2b59e46c…` (371,712 B) / PDB
   `857cd1b8…` (102,068 B)** — provenance READY `evidence/SP5NPC-N2-20261030-033852/` ·
   **ยังไม่ deploy — `Mods\` ยืนยันปลาย session ยังเป็น `6a94abb5`** · hook shells เป็น Il2Cpp
   ไม่ลิงก์เทสต์ (compile-only + static + audit ตาม idiom) — พฤติกรรมจริงรอ N3/N4
5. **มติ 3 ข้อของ N2 = bracket ยังไม่ได้เลือก (บันทึกตามจริง ไม่เดา):** ระดับ+ตัวคูณ "[เช่น Light
   ×0.5]" = ตัวอย่าง · refill "[ตามตัวเลือกในแผน]" = ยังรอ · key npcPolicy = **คง default "off"
   ตามที่ implement** (ไม่แก้) · ทั้ง 3 ข้อ**ไม่บล็อกงาน offline** (โค้ด level-parameterized แล้ว)
   แต่**บล็อก N3/N4** · popup ครั้งที่ 7 ไม่มีคำตอบ (append แล้ว uncommitted) · decisions-pending
   append ครบ (§2/§7.1 N2 + มติ 3 ข้อ, §4 push, §6 popup) — ไม่ลบเดิม

## สถานะปลาย session

- mod repo: HEAD `d8036f7` นำ origin **2 commits** (`9d0ff02`, `d8036f7`) — รอมติ push ·
  dirty = `M user-popup-answers` (ครั้งที่ 7, uncommitted ตามขอบเขต) · untracked คงเดิมทั้งหมด
- game repo: HEAD `944ceda` = origin/main (synced) — ไม่มีค้าง
- เกม/Mods\/net.cfg/pref/เซฟ **ไม่ถูกแตะเลยทั้ง session**

## ขั้นถัดไปที่รอมติ

1. **ขออนุมัติ N3** (deploy build `2b59e46c…` + split + ผู้ใช้เปิดเกม — T1 normal mode gate off)
   **เป็นเซสชันแยก** — ก่อนขอต้องมีมติ 3 ข้อ: ระดับ+ตัวคูณจริง · นโยบาย refill · ยืนยัน key
   `npcPolicy` (default "off") — ตามที่ระบุใน decisions-pending §7.1
2. push 2 commits (`9d0ff02`, `d8036f7`) เมื่อต้องการ
3. ค้างเดิมทั้งหมดคงเดิม (V2-A (ก)/(ข), D3–D10, S2-plan §13, VPN/guest/60 ยูนิต, E4 ซ้ำ, popup 3 ข้อ)
