# C2 — rehash อิสระ Save เทียบ baseline `20261025-201545` (G-2 precondition)

session: `AUTO-20261028-070417` · วิธี: Python hashlib นอกเหนือจาก `save-split status` (อ่านอย่างเดียว) · วันเวลา 2026-10-28 07:0x

```
baseline=57 now=57 same=56
ADDED: []
MISSING: []
CHANGED: ['Save/PlayerprefData.dat']
  Save/PlayerprefData.dat old 50e1612b968d -> new efd4ac5b5dbb
SaveSlot* files: 55 EXACT vs old baseline: True
```

- digest จาก `save-split status` = `df8e5a11beed` (57 ไฟล์) · `leftovers: none` · `game: not running`
- hash ของ `PlayerprefData.dat` เดิมใน baseline = `50e1612b968d…` → ปัจจุบัน `efd4ac5b5dbb…` (drift จากการเปิดเกมของผู้ใช้ 2026-10-27 ×3 — ML logs `00:47 / 02:16 / 02:26`, เหตุการณ์เดียวกับ DEVIATION-1 ของ session `AUTO-20261028-023904`)
- **SaveSlot\* ทั้ง 55 ไฟล์ EXACT baseline เก่า, ADDED=0, MISSING=0, CHANGED = `PlayerprefData.dat` ไฟล์เดียว** — ตรงเงื่อนไข [G-2] ครบ ⇒ ผ่านเงื่อนไขเดินหน้า C3

## C1 — สำรอง pref ก่อน rebaseline

- สำเนา: `evidence/AUTO-20261028-070417/PlayerprefData.dat.pre-rebaseline-20261028-0704`
- sha256 ต้นทาง = สำเนา = `efd4ac5b5dbb557bf053ad5600f379484e6d9a73c979e0a02055eeb8f7580037` (444-byte-เทียบไม่ได้ที่นี่ — เป็นไฟล์ pref ของเกม ไม่ใช่ net.cfg)
