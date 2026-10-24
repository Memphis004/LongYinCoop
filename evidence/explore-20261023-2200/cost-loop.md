# ต้นทุนอีเวนต์: getter vs ที่หักจริง — ปิดวง (2026-10-23 23:42)

สถานะ: **RUNTIME** (hook `tools/frida_cost_probe.py` + เรียก getter สด) · แผนที่ `潜入藏经阁` · `exploreDifficulty = 4` · `maxPower = 55`
หลักฐานดิบ: `cost-probe.ndjson`

## สูตร (RUNTIME static — disasm)
`ExploreController` อ่าน `exploreMapData` (+0x70 = 112) → `exploreDifficulty` (+0x28 = 40):

| method | สูตร | ค่าคงที่ที่อ่านได้ |
|---|---|---|
| `GetDigTreasureCost()` | `diff × 0.5 + 2` | 0.5, 2 |
| `GetOpenLockCost()` | `diff × 0.25 + 3` | 0.25, 3 |
| `GetCollectResourceCost()` | `diff × 0.25 + 2` | 0.25, 2 |

ท้ายฟังก์ชัน `jmp 0x…4ce0` → `cvtss2sd` → call → `cvttsd2si` ⇒ แปลง float→int ด้วยการปัด

## ค่าที่วัดได้บน diff 4
| method | ret |
|---|---|
| `GetDigTreasureCost` | **4** |
| `GetOpenLockCost` | **4** |
| `GetCollectResourceCost` | **3** |

**แก้ไขความเข้าใจ:** แถวที่เห็นสามตัวติดกัน (≈t 201.4–202.1 s) เป็น **การเรียกของผมเอง** ผ่าน `method().invoke()` (probe hook จับการเรียกนั้นด้วย) ไม่ใช่เกมเรียก — เกมเรียกจริงเฉพาะ `GetDigTreasureCost` ภายใน `DigTreasureChoosen` ตามวงจรด้านล่าง

## วงจรจริง: ขุดสมบัติ 1 ครั้ง
```
DigTreasureChoosen  in   lp = 8
  GetDigTreasureCost() ret = 4  (diff = 4)
  ChangeMoveStep     in   lp = 8
  ChangeMoveStep     out  lp = 4      <- หัก 4 ตรงกับ ret เป๊ะ
DigTreasureChoosen  out  lp = 4
```
- `ChooseDigTreasure` (ตอนกดเลือก) **ไม่หักพลัง** — หักตอน `DigTreasureChoosen` เท่านั้น
- ของที่ได้: `饕餮纹鼎` (weight 4) · กระเป๋า 71 → 72 ชิ้น
- จุดหักเงิน/พลังคือ `ChangeMoveStep` **ภายใน** `DigTreasureChoosen` (ไม่ได้เรียกฟังก์ชันหักเงินแยก)

## วงจรจริง: หีบล็อก (ev20) 1 ครั้ง — `(3,4)` diff 4
```
ManageTileEvent  in   tile=[3,4,20] lp = 49
  GetOpenLockCost() ret = 4          <- เรียกตอนเปิดข้อความ (ยังไม่หัก)
ManageTileEvent  out             lp = 49
... ผู้เล่นเลือกข้อ 2 ...
OpenLockChest    in              lp = 49
  GetOpenLockCost() ret = 4
OpenLockChest    out             lp = 45   <- หัก 4 ตรงกับ ret
```
- ของที่ได้: `鸟兽散` (weight 2) · กระเป๋า 71 → 72
- **ต้นทุน lock หักตอน `OpenLockChest`** (คนละจุดกับ dig ที่หักใน `DigTreasureChoosen`)

## เทียบกับเคสเก่า (ทั้งหมดตรงสูตร)
| เคส | diff | สูตร | หักจริง |
|---|---|---|---|
| ev19 (14,7) | 10 | 5+2 = 7 | **7** ✓ |
| ev19 (3,1) | 4 | 2+2 = 4 | **4** ✓ |
| ev20 (3,2) lock | 4 | 1+3 = 4 | **4** ✓ |
| ev19 (รอบนี้) | 4 | 2+2 = 4 | **4** ✓ (จับคู่ ret↔Δlp ในอีเวนต์เดียว) |

## อีเวนต์ที่ไม่ผ่าน getter：ขุดผ่าน obstacle
- `(4,10)` `ev12` + `exploreTileObstacleData` → `ManageTileEvent` หัก **2** (lp 20→18) ผ่าน `ChangeMoveStep` ข้างใน
- พื้นของช่อง = `Road` (`costStep = 1`) ⇒ ไม่ใช่ค่าพื้น · หลังขุด obstacle ถูกล้างเป็น null
- **ไม่มีการเรียก `Get*Cost` ใด ๆ** ⇒ ต้นทุน obstacle เป็นคนละเส้นทาง (น่าจะ `obstacle.needNum` — ยังไม่ยืนยัน)



## วงจรจริง: ev23 "เก็บเกี่ยว" — `(7,4)` diff 4 (ปิดเคสสุดท้าย)
```
ManageTileEvent        in   tile=[7,4,23] lp = 39
ManageTileEvent        out                lp = 39   <- เหยียบแล้วยังไม่หัก
... เลือกข้อ 2 "เก็บเกี่ยว" ...
ChooseCollectResource  in                 lp = 39
  GetCollectResourceCost() ret = 3        <- เกมเรียกเอง (diff 4)
  ChangeMoveStep  39 -> 36                <- หัก 3 = ret เป๊ะ
ChooseCollectResource  out                lp = 36
```
**ผลลัพธ์ที่เกมแจ้งบนจอ (ข้อความ UI ไม่ใช่ค่าที่อ่านจากหน่วยความจำ):**
- สำนักได้ **ไม้ซุง +234** · หวังเวิ่นเทียนได้ **ผลงาน +3** · **ทักษะตัดไม้ +1** · **พลังกาย −3**

**ตรวจในหน่วยความจำได้บางส่วน:**
- `leftPower` 39 → **36** = ตรงกับ `GetCollectResourceCost()` ✓
- กระเป๋าฮีโร่ / `forceStorage` / `governStorage`: **ไม่เปลี่ยน** (diff itemID = ว่าง) ⇒ รางวัลไม่ใช่ item
- `ForceData.resourceStore` = `List<float>` 6 ช่อง (ค่าปัจจุบัน `[7185.433, 5063.833, 3712.042, 2844.915, 4669.408, 2450]`, max ช่องแรก 14700) — **ยืนยัน +234 ไม่ได้** เพราะ snapshot ก่อนหน้าใช้ตัวอ่านลิสต์แบบ object ซึ่งคืน `[]` กับ `List<float>` (ข้อจำกัดเครื่องมือ ไม่ใช่ของเกม)
- `HeroData.forceContribution` = 998.82 (ไม่มีค่าก่อนหน้าให้เทียบ)

**บทเรียนเครื่องมือ:** `List<float>`/`List<int>` ต้องอ่านผ่าน pointer ของ `_items` + offset 0x20 + i*4 ไม่ใช่ `.get(i)` (คืน object ไม่ได้) — แก้ในสคริปต์แล้ว

## ยังไม่รู้
- กติกาปัดเศษที่จุด `.5`: diff 10 → collect = 4.5 · ev23 รอบก่อนหัก 5 (อาจเป็น 4.5→5 หรือ 4 + 1 ก้าว) — ยังแยกไม่ได้
- สูตร/ที่มาของต้นทุน obstacle (2)
- พฤติกรรมเมื่อ `leftPower` ไม่พอจ่าย (ระบบกันไม่ให้เลือก หรือติดลบ)
