# S1-b Frida ↔ IPC cross-check (build 6f9e82c1, pid 8492, 2026-10-20)

ทุกครั้งอ่านคนละ **ทิศทาง traverse** แต่ต้องยอมรับว่า field ต้นทางเดียวกันคือ `HeroData.bigMapPos`
(บันทึกไว้เพื่อไม่ให้เข้าใจผิดว่าเป็นการยืนยันอิสระสองแหล่ง):
- Frida #2: `GameController.get_Instance() → worldData → Player() → bigMapPos`
- Frida #3: `GameController.get_Instance() → worldData → Heros[get_Item(i)]` แล้วเลือก element ที่ `heroID` ตรงกับ `Player().heroID`
- IPC: `snapshot world` → `partyPos` (mod: `GameController.Instance.worldData.Player().bigMapPos`)

| # | เวลา / frame | สถานะ | Frida x | Frida y | IPC x | IPC y | worldTime Frida | worldTime IPC |
|---|---|---|---|---|---|---|---|---|
| 1 | 2026-10-19 (build 841ae14) | หยุดนิ่ง | 1886.2742919921875 | 283.280517578125 | 1886.2743 | 283.28052 | — | — |
| 2 | 12:05:58 / 117302 | หยุดนิ่ง | -4339.34375 | -42.57463073730469 | -4339.3438 | -42.57463 | 10/10/28 07.840036869 | 10/10/28 07.840037 |
| 3 | 12:08:37 / 138102 | หยุดนิ่ง | -2424.583740234375 | 431.2945861816406 | -2424.5837 | 431.2946 | 10/10/30 12.736275672912598 | 10/10/30 12.736276 |

- Δ ที่เห็น = 0 ทุกครั้ง ภายในความละเอียด float32 ที่ JSON แสดง (7 หลัก)
- sample 3: `Heros` มี 897 element, พบ player ที่ index 0, `sameObject=true` (element เดียวกับ `Player()`)
- `heroID=0`, `atAreaID=-1` ตรงกันทั้งสองฝั่ง

## ข้อจำกัดของหลักฐานชุดนี้
1. ทั้งสองฝั่งลงเอยที่ object เดียวกัน (`HeroData` ตัวเดียวกัน) → พิสูจน์ว่า interop อ่านค่าตรงกับ bridge แต่ **ไม่ได้พิสูจน์ว่า field นี้คือค่าที่เกมใช้วาดบนจอ** (ข้อนั้นวัดแยกด้วย UI/HUD ใน S1-c)
2. sample 1 เป็นของ build ก่อนแก้ schema — ใช้เป็นข้อมูลประกอบเท่านั้น
