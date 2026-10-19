# ข้อ 4 — `snapshot world` (get_player_pos) เทียบกับ Frida · 3 ครั้ง

Run: 2026-10-20 05:42–05:44 · เกม pid 30904 · mod `LongYinCoop 0.1.0` stage 0 (probe=True, ipc=True)
Frida: attach local, `frida-il2cpp-bridge` (il2cpp-frida-mcp)

## เส้นทางที่อ่าน (เหมือนกันทั้งสองฝั่ง)

```
BigMapController._instance (static field)
  → .playerArmy (UnityEngine.GameObject)
  → GetComponent<BigmapNpcController>()
  → .heroData (HeroData)
  → .bigMapPos (BigMapPos) → x, y ; .heroID
```

- ฝั่ง mod: `SnapshotReader.GetPlayerPos()` ผ่าน Il2CppInterop (typed) → IPC `get_player_pos`
- ฝั่ง Frida: อ่าน IL2CPP field ตรง ๆ ด้วย bridge (ไม่พึ่ง interop ของ mod) → ยืนยันว่า interop ไม่ได้คืนค่าผิด

## ผล

| # | เวลา | IPC (`get_player_pos`) | Frida (field ตรง) | ตรงกัน |
|---|---|---|---|---|
| 1 | 05:42:1x | x=-3807, y=84, heroID=0 | x=-3807, y=84, heroID=0 | ✅ |
| 2 | 05:43:5x | x=-3807, y=84, heroID=0 | x=-3807, y=84, heroID=0 | ✅ |
| 3 | 05:44:2x | x=-3807, y=84, heroID=0 | x=-3807, y=84, heroID=0 | ✅ |

dispatcher ระหว่างวัด: queued 30 / ran 30 / errors 0 / pending 0 · p99 0.0007 ms
frame ตอนวัด (MainGame): p50 6.928 / p95 19.215 / p99 38.741 ms

## ข้อจำกัด

- ทั้ง 3 ครั้งวัดขณะอยู่ **MainGame (โซนสำนัก)** ไม่ได้อยู่ BigMap — ตำแหน่งจึงเป็นค่าเดียวกันตลอด
  (ค่า BigMap จริงเคยยืนยันทาง IPC แล้วใน run6: -3807,84 → **-4001.15, 1015.25**)
- `heroID=0` ทั้งสองฝั่ง — เป็นค่าที่เกมเก็บจริง ไม่ใช่ความคลาดของ interop

**สรุป: PASS** — ค่าตรงกัน 3/3 ครั้ง, error 0, และใช้ object path เดียวกับที่ S1 จะอ่านต่อ
