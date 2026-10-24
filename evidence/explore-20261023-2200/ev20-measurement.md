# ev20 CollectResource @ (3,2) — before/after ครบ (2026-10-23 ~23:12)

สถานะ: RUNTIME (อ่านผ่าน interop + hook `ManageTileEvent`) · map `FinishStealSkill` · maxPower 55

## Before (23:10, `before-ev20.json`)
- `leftPower` 47 · `keyNum` 0
- กระเป๋าฮีโร่: money 61,058 · 72 ชิ้น · weight 303
- `forceStorage` (#25): 0 เงิน · 170 ชิ้น
- `governStorage`: 0 เงิน · 11 ชิ้น
- `resourceStore`: []

## เหตุการณ์ (hook `ExploreController.ManageTileEvent`)
```
tileEvent in  tile=[3,2,20] lp 46 n 72 w 303 pt False
tileEvent out              lp 46 n 72 w 303 pt True   <- plot เปิด (ยังไม่จ่าย)
```
- เดิน 1 ก้าวจาก `(2,2)` → `(3,2)`: lp 47 → 46
- UI ที่เด้ง: "หีบล็อก" 3 ตัวเลือก (ใช้กุญแจ / ใช้แรง / ไม่เอา) — ผู้เล่นเลือกข้อ 2
- `ExploreController.GetOpenLockCost` มีอยู่จริงในคลาส (ยังไม่ได้เรียกวัดค่าตรง ๆ)

## After (23:14, `after-ev20.json`)
- `leftPower` **42** (−4 จากตอน plot เปิด ⇒ ต้นทุนเปิด = 4 พลังงาน, ไม่ใช่ 5 แบบ ev23)
- กระเป๋า **73 ชิ้น** · weight **304** ⇒ ได้ของ **1 ชิ้น น้ำหนัก 1**
- ของที่ได้: `宰鸡牛刀` (`itemID = 0`, weight 1) — เป็นของประเภทอาวุธ ไม่ใช่วัตถุดิบ
- money 61,058 (เท่าเดิม) · `forceStorage` 170 (เท่าเดิม) · `governStorage` 11 (เท่าเดิม) · `resourceStore` [] (เท่าเดิม)

## Diff แบบ multiset (itemID)
```
bag     added {0: 1}  removed {}
force   added {}      removed {}
govern  added {}      removed {}
```

## ข้อสรุป
- ev20 บนแผนที่นี้ **ให้ของเข้ากระเป๋าฮีโร่ทันที** 1 ชิ้น และ **ไม่แตะ** money/force/govern/resourceStore
- UI ของ ev20 เหมือน "หีบล็อก" (ev19) ทุกประการ ⇒ แยกชนิดด้วยตาไม่ได้ ต้องดู `exploreTileEventType`
- **แผนที่นี้ไม่มี ev23 เลย** (นับ 0 ช่อง) ⇒ ยังวัด ev23 ซ้ำไม่ได้

## ยังไม่รู้
- `GetOpenLockCost` คืน 4 ตรงกับที่หักจริงหรือไม่ (ยังไม่ได้เรียก)
- ตัวเลือกข้อ 1 (ใช้กุญแจ) ต่างกันแค่ต้นทุนหรือรางวัลด้วย
- รางวัลของ ev23 (harvest) ไปลงที่ไหน — รอบก่อนวัดได้ lp −5 แต่กระเป๋า/force/govern ไม่เปลี่ยน
