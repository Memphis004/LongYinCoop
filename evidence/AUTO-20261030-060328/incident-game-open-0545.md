# incident — เกมถูกเปิดระหว่างเซสชัน offline (05:45:05) — snapshot ไม่ตรง ⇒ หยุดเซสชันโค้ด N4-follow-up

> เซสชัน: offline แก้โค้ด Sp5T0Probe ตามมติ N4-follow-up (วลีสั่ง 2026-10-30 ~06:00) ·
> session-start 06:03:28 พบ snapshot ไม่ตรง 2 จุด ⇒ **หยุดทันทีตามเงื่อนไขผู้ใช้ — ไม่ได้แก้โค้ด
> ไม่ build ไม่ commit ในเซสชันนี้** · ไฟล์นี้ = บันทึกการวินิจฉัย (read-only ล้วน)

## สิ่งที่ session-start พบ (เทียบค่าที่มติกำหนด)

| รายการ | มติคาดหวัง | วัดได้ 06:03 | ผล |
|---|---|---|---|
| เกม | not running | not running | ✅ |
| GameAssembly.dll | `33c56def…` | `33c56def…` | ✅ |
| global-metadata.dat | `451311d4…` | `451311d4…` | ✅ |
| Mods DLL | `6a94abb5…` | `6a94abb5…` | ✅ |
| net.cfg | `12541f34…` | `12541f34…` | ✅ |
| pref (Sp5T0=false) | hash `976ccfb9…` | **`733d9a0c…`** (ค่า Sp5T0/TimeV2a/AllowRemoteInput = false ครบ) | ❌ hash |
| เซฟ | 57 ไฟล์ `df8e5a11beed` | 57 ไฟล์ **`7e1e65e52659`**, leftovers none | ❌ digest |
| mod repo | HEAD `772c8f2` | `772c8f2` | ✅ |
| game repo | HEAD `944ceda` | `944ceda` | ✅ |

## วินิจฉัย (หลักฐานครบ)

- **ML log ใหม่ `26-10-30_5-45-5.log`**: เปิดเกม 05:45:05 · `Preferences Loaded!` (05:45:06) ·
  `Preferences Saved!` (05:46:28 = ML เขียน cfg ตอนปิดเกม) · **ไม่มีบรรทัด Sp5T0/Sp5Npc/[S2] เลย**
  (ตรง config: Sp5T0=false + net.cfg enabled=false ⇒ probe/gate/S2 ไม่ทำงาน — build ที่รันคือ
  `6a94abb5…` ตาม Mods\) · [ERROR] = 0
- **เซฟรายไฟล์เทียบ baseline ที่อนุมัติ `20261028-071340` (`df8e5a11beed`):** **56/57 EXACT ·
  CHANGED = `Save/PlayerprefData.dat` ไฟล์เดียว** (`efd4ac5b…` → `fe89333b…`) · missing=0 ·
  ไฟล์ใหม่ใน Save = 0 ⇒ **`SaveSlot*` ไม่ถูกเขียนแม้แต่ไฟล์เดียว**
- **`MelonPreferences.cfg`:** hash เปลี่ยนจาก `976ccfb9…` → `733d9a0c…` จากการที่ ML เขียนตอนปิดเกม
  (รูปแบบเดียวกับ divergence ที่เคยบันทึกใน VERSION.md เช่น `66fb92d5…` หลัง GO-3) · ค่าที่ผูก
  ความปลอดภัย/การวัดทั้งหมดคงเดิม: `Sp5T0 = false` · `TimeV2a = false` · `AllowRemoteInput = false`
  (อ่านเนื้อหาเทียบค่าแล้ว — ไม่มีสำเนาไฟล์เก่าไว้ diff ทุก byte)
- **รูปแบบตรงกับ incident เดิมที่รู้จัก** (16:07/19:49 ของ 2026-10-21, 22:54:18, 07:48 ของ
  2026-10-22, [G-2] ของ 2026-10-28): ผู้ใช้เปิดเกมเอง ⇒ drift = `PlayerprefData.dat` + ML เขียน
  cfg — เป็น volatile-only ไม่กระทบ SaveSlot*
- ไม่พบการแก้จาก session อื่น: git สอง repo ตรง snapshot, ไม่มีไฟล์ใหม่/หายใน Save, ไม่มีการ
  deploy DLL ใหม่

## สถานะและทางเลือก (รอมติผู้ใช้)

- เซสชันโค้ด N4-follow-up **ยังไม่เริ่ม** (ไม่มีการแก้ src ไม่มี build ไม่มี commit ใหม่ —
  mod repo HEAD ยัง `772c8f2`)
- **(ก)** อนุมัติ `rebaseline volatile-only` (`save-split.py rebaseline --against 20261028-071340`)
  → baseline ใหม่บันทึก divergence ของ cfg (`976ccfb9…` → `733d9a0c…`, ค่า false ครบ) ลง
  VERSION.md → session-start ใหม่ด้วย baseline ที่ rebaseline แล้ว แล้วรันมติ N4-follow-up ต่อ
  (รูปแบบเดียวกับมติ [G-2] 2026-10-28)
- **(ข)** มติอื่นตามที่ผู้ใช้กำหนด — เซสชันนี้หยุดรอเท่านั้น
