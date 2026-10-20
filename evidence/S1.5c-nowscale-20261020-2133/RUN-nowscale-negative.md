# S1.5-c nowScale — ผลวัด: **ลบ** และเจอตัวชี้ที่ถูก (2026-10-20 21:35–21:37) [RUNTIME]

## 1. ผลวัด `nowScale` (DLL `45fa4036ab8f`)

| ข้อ | ค่า |
|---|---|
| ตัวอย่าง | **625 sample @100 ms** (21:35:27.183 → 21:36:30) error 0 |
| `nowScale` | **= 1 ทุก sample** (ไม่มี sample ใด ≠ 1) |
| `timeScale` | = 1 ทุก sample |
| `deltaTime` / `unscaledDeltaTime` | 0.006938 / 0.006938 (เท่ากัน = ไม่มี time scaling) |
| ช่วงที่ผู้ใช้กด x5 | อยู่ในหน้าต่างนี้ (ผู้ใช้ยืนยัน "ครบ" 21:36) |

→ **`nowScale` ไม่ใช่ฟิลด์ที่ปุ่มเร่งเขียน** — และชื่อ/บริบทของมัน (`tweenFocusTargetScale`, `TweenFocusTarget(pos, scale)`, `FocusOnTarget`) ชี้ว่าเป็น **สเกลกล้อง/โฟกัส** ไม่ใช่ความเร็วศึก [DUMP + INFER]

## 2. ตามรอยจนเจอตัวชี้จริง (อ่านอย่างเดียว ไม่ได้แก้เกม)

**[ASM]** disassemble `BattleController.SetTimeScaleTab()` (frida MCP, attach pid 22884, ไม่ได้ hook):
- อ่าน `this.timeScaleTab` (`0x198`) → วนลูก (`GetChildCount`/`GetChild`)
- อ่านค่าจากออบเจกต์ที่ได้จาก singleton: `mov rax, [rax+0x20]` → `movss xmm6, [rax+0x1d8]`
- ดึง **ข้อความของลูก** แล้วแปลงเป็น int (`cvtdq2ps`) → เทียบกับค่านั้น → ตั้ง active tab

**[DUMP]** ฟิลด์ `float` ตัวเดียวที่ offset `0x1D8` ทั้ง dump:
```
public class WorldData : ICloneable   // TypeDefIndex: 8181
    public float battleTimeScale;      // 0x1D8
```

**การตีความ [INFER]:** `WorldData.battleTimeScale` = ค่าความเร็วศึกที่ผู้เล่นตั้ง และ **ค่าเร็วมาจากข้อความบนแท็บ** (ตัวเลขบนปุ่ม เช่น 1 / 2 / 5) — ยังไม่ยืนยันว่ารับค่าอะไรได้จริงนอกจากที่เกมมี

เข้าได้จากโค้ด mod ที่มีอยู่แล้ว: `GameController.Instance.worldData.battleTimeScale` (`src/LongYinCoop/S1/WorldSampler.cs:94` ใช้ `gc.worldData` อยู่แล้ว)

## 3. ข้อสรุป + สิ่งที่ต้องขออนุมัติ

- `nowScale` = ผลลบ → **ไม่ใช้ต่อ** (ยังเก็บไว้ใน status ได้ ไม่เสียหาย แต่ต้องไม่ใส่ใน hash)
- เสนอ: เพิ่ม `battleTimeScale` (typed, อ่านอย่างเดียว) ลง `spike status` แล้ววัดซ้ำ (ปิดเกม → rebuild → deploy → เปิดเกม → กด x5)
- ยังไม่พบว่า `battleTimeScale` ถูกนำไปคูณที่ใด (ต้อง disassemble จุดที่ใช้ฟิลด์นี้ ถ้าต้องการพิสูจน์กลไก) — ยังไม่ทำเพราะยังไม่อนุมัติ

## 4. หมายเหตุอื่น

- ระหว่างรอบนี้เกมเดินหน้าเร็วผิดปกติ (turn 0→8 ใน 20 วิ, หลังจากนั้นหยุดที่ turn 8) — **ยังไม่รู้สาเหตุ** และยังไม่สรุปว่าเกี่ยวกับปุ่มเร่ง
- ไม่มีการแตะเซฟ/Steam/achievement ในรอบนี้ · pref `JitProbeW4=false` · ไม่มี hook ใหม่
