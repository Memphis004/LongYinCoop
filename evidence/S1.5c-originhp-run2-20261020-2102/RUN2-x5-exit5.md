# S1.5-c run 2 — exit 5 + ความพยายามวัด x5 (2026-10-20 21:19–21:25) [RUNTIME]

หลักฐาน: `logs/spike-full-2119.ndjson` (3,782 sample @100 ms, error 0) · `jitprobe-w4-20261020-210855.ndjson` (32 event)

## 1. exit 5 — pause→resume ที่ยืนยันได้

| ข้อ | ค่า |
|---|---|
| hook event ทั้งหมด | **32** (21:16:38 → 21:24:46) |
| `paused` หลัง call | true **16** / false **16** → **16 รอบสมบูรณ์** |
| สลับทุกครั้ง ไม่มีซ้ำ | ใช่ (T,F,T,F,…) |
| `nowActiveState` ตอนกด | 1, 7, 8, 10, 11, 12 — **ไม่มี 2 (WaitPlayerInput)** |
| `timeScale` ตอนกด | 1 ทุกครั้ง |
| pause ล่าสุด | 21:24:21.703 (state 8) → resume 21:24:46.845 (state 1) |

ฝั่ง status stream:
- window pause 21:24:21.754 → 21:24:46.874 = **25.02 s** `paused=true` · state `WaitAIPlan` · `battleTime` **5.2005 นิ่ง** · frame เดิน
- ก่อนหน้า: window 21:19:27–21:22:02 (pause ค้าง 154.27 s ที่ state `Finish`) แล้ว resume สำเร็จที่ 21:22:02.120
- **หลัง resume เกมเดินต่อ:** `battleTime` 3.5999 → 5.2328 · `turnIndex` 31 → 47 · มี damage event ต่อเนื่อง

## 2. x5 — ยังวัดไม่ได้ (ผลเป็นลบกับทุกฟิลด์ที่อ่านอยู่)

สิ่งที่วัด (ช่วงที่ผู้ใช้บอกว่าเปิด x5 แล้วตี 1 ครั้ง ≈ 21:24:00–21:24:48):

| ตัววัด | ผล |
|---|---|
| `Time.timeScale` | **1** ตลอด 3,782 sample — ที่ไม่ใช่ 1 มีแค่ hit-stop: `0.05` ×6 และ `0.2` ×8 (ทั้งหมดอยู่ช่วง `Attacking` ก่อน 21:24) |
| ระยะ `Attacking` (วินาทีผนัง) | ก่อน 21:24: median **0.70** (n=11, 0.50–2.31) · หลัง: median **0.80** (n=3, 0.60–1.81) → **ไม่เร็วขึ้น** |
| ระยะ `WaitAttacking` | ก่อน median 0.85 · หลัง median 1.41 → ไม่เร็วขึ้น |
| ระยะ `Moving` | 0.10–0.20 s ทั้งก่อนและหลัง |
| จำนวนเทิร์นต่อวินาที | 21:23:01–21:23:42 = 5 เทิร์น (39→43) · 21:24:11–21:24:48 = 4 เทิร์น (44→47) → ไม่ต่าง |

**สรุปที่กล้าพูดได้:** ปุ่ม x5 **ไม่ใช้ `Time.timeScale`** (ยืนยันซ้ำ) และ **ผลต่อระยะแอนิเมชันยังไม่ปรากฏ** ในข้อมูลชุดนี้ — แต่ยัง **สรุปไม่ได้ว่าปุ่มไม่ทำงาน** เพราะฟิลด์ที่ปุ่มน่าจะเขียนยังไม่ได้อ่าน

**[DUMP] ตัวชี้ที่ควรอ่านต่อ (ยังไม่อ่าน — รออนุมัติ):**
- `BattleController.nowScale` (`0xE0`, `public float`) — ชื่อตรงกับ "scale" ของศึก
- `BattleController.timeScaleTab` (`0x198`, GameObject) — แท็บปรับความเร็วบนจอ
- เมธอด: `SetTimeScaleTab()`, `GetHalfBattleTimeScale()`, `GetThirdBattleTimeScale()`
- เสนอ: เพิ่ม `nowScale` (typed, อ่านอย่างเดียว) ลง `spike status` แล้ววัดซ้ำ — ต้องปิดเกม + rebuild + split ใหม่

## 3. ได้เพิ่ม: `unitAlive` = ตาย/รอดในศึก (ใช้งานได้)

| unit | `hp` | `alive` (`HeroData.dead`) | `unitAlive` (`BattleUnit.IsAlive`) |
|---|---|---|---|
| `1:10102` | ลดลงจน **0.000** (21:22:30.090) | **true ตลอด** | **true → false** (1,983 sample) |
| `1:10107` | ลดลงจน **0.000** (21:22:30.995) | **true ตลอด** | **true → false** (1,974 sample) |

→ `hp` กับ `unitAlive` สอดคล้องกันเป๊ะ (`hp<=0` ⇔ `unitAlive=false`), ส่วน `alive` ไม่สะท้อนการตายในศึก
→ ใช้ `unitAlive` เป็นตัวยืนยันผลของ `spike attack` ได้ และควรใส่ใน `stateHash` (ปัจจุบัน hash ใช้ `HeroData.hp`+`dead` ซึ่งพลาดการตายในศึก)

## 4. เทียบเกณฑ์

| เกณฑ์ | ผล |
|---|---|
| exit 5 — 10 รอบ pause→resume | **16 รอบ** (ตัวเลขผ่าน แต่ **รอผู้ใช้ยืนยันว่าจะรับตามตัวอักษรเกณฑ์ A หรือไม่**) |
| exit 4 (J7 เร่ง) | **ยัง NOT-RUN** — ต้องอ่าน `nowScale` ก่อน |
| ตัววัดดาเมจ | **PASS** — 32 damage event, `hp` ตรงกับ `unitAlive`, `originHp` คงที่ |
| ไม่เขียน field / ไม่เรียก method เปลี่ยน state | **PASS** |
