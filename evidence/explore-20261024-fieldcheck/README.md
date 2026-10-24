# field-verify — แก้ชื่อฟิลด์ snapshot (2026-10-24)

สถานะ: **RUNTIME** (อ่านอย่างเดียว · attach เกมที่รันอยู่ pid 37828 · ไม่ได้สั่ง action ใด ๆ)
ข้อมูลดิบ: `field-verify.json`

## 1. `governStorage` ไม่ได้อยู่ที่ `ForceData`
- **[DUMP]** `ForceData` (dump.cs:369387) มี `resourceStore` 0x88 / `resourceStoreMax` 0x90 / `resourceChange` 0x98 /
  `forceStorage` 0xA0 / `bookStorage` 0xB8 — **ไม่มี** `governStorage`
- **[DUMP]** `governStorage` เป็นฟิลด์ของ **`WorldData`** @ **0x1D0** (`ItemListData`)
- **[RUNTIME]** `worldData.governStorage.money` = 0 · `allItem` = 15 ชิ้น ⇒ path ใหม่ใช้งานได้
- probe ตัวก่อนล้มด้วย `Il2CppError: couldn't find non-static field governStorage in hierarchy of class ForceData`

## 2. `List<float>` อ่านได้แบบ typed — ไม่ต้อง raw offset
- **[RUNTIME]** `list.field("_items").value.get(i)` บน `List<float>` คืน **JS number** ตรง ๆ (`resourceStore[0]` = 5996.73046875)
- ตัวกรองแบบ object (`x.handle.isNull()`) ใช้กับ list ของ value type **ไม่ได้** — จะได้ `[]` เงียบ ๆ
  (นี่คือสาเหตุที่ snapshot ก่อนหน้าอ่าน `resourceStore` ได้ลิสต์ว่าง)

## 3. ค่าที่อ่านได้ ณ ตอนวัด (หน้า Area · ยังไม่อยู่ในดันเจี้ยน)
| รายการ | ค่า |
|---|---|
| `player.heroID` / `badFame` | 0 / **0** |
| `player.forceContribution` | 995.79 |
| `worldData.totalBadFame` | **67.745** |
| `force` | id 25 `仙霞派` · `forceStorage` 171 ชิ้น · `bookStorage` 32 เล่ม |
| `resourceStore` | 5996.73 / 4900 / 4900 / 2447.87 / 4600 / 2333.16 |
| `resourceStoreMax` | 12250 / 4900 / 4900 / 4900 / 4900 / 2450 |
| กระเป๋าฮีโร่ | 70 ชิ้น |

**ยังไม่รู้:** `HeroData.badFame` ของผู้เล่น = 0 แต่ `WorldData.totalBadFame` = 67.745 ⇒ ยังไม่รู้ว่าค่าอาญากรรม
ที่ผู้เล่นเห็นบนจอมาจากตัวไหน และบวกตอนเข้า/ตอนจบดันเจี้ยนหรือไม่ (ยังไม่วัดรอบเข้า–ออก)
