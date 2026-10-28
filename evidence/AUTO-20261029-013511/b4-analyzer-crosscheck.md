# B4 — ตรวจ `tools/time_v2a_analyze.py` กับรูปแบบ NDJSON ของ sampler จริง (label `AUTO-20261029-013511`)

## วิธีตรวจ (offline ทั้งหมด — ไม่เปิดเกม ไม่ deploy)

1. สร้าง NDJSON ด้วย **detector จริง** (`TimeV2aDetector` ใน `src/LongYinCoop/TimeV2a/TimeV2aRules.cs`)
   ผ่านโมเดลจำลองปั๊มตาม time-static-notes §2.4 (N=5, dayTime=1.0, pauseTime=0.25, k=1.0, 60 fps):

   ```
   dotnet run --project tests/S2Protocol.Tests -c Release -- --emit-v2a-fixture evidence/AUTO-20261029-013511/v2a-fixture.ndjson
   → EXIT=0 · "V2A FIXTURE written …\v2a-fixture.ndjson" (13 event = 1 start + 6 pause_end + 5 tick + 1 end)
   ```

2. รัน **analyzer ตัวจริง** กับไฟล์นั้น (tolerance 0.1 s — ดู errata N4):

   ```
   python tools/time_v2a_analyze.py evidence/AUTO-20261029-013511/v2a-fixture.ndjson --tolerance 0.1
   → EXIT=0
   ```

   ผล (`b4-analyze-result.txt`):

   ```json
   {"n": 5, "k": 1.0, "day_time": 1, "pause_time": 0.25, "measured_s": 6.656, "day_ticks": 5,
    "form1_s": 6.25, "form2_s": 6.75, "form_gap_s": 0.5, "verdict": "FORM2",
    "reason": "measured closer to FORM2 (Δ1=0.406, Δ2=0.094, gap=0.500)"}
   summary: sessions=1 decided=1 indistinguishable=0 invalid=0 mismatch=0
   ```

## ข้อสรุป

- ฟิลด์/ชนิด **ตรงกันทุกตัว** — analyzer อ่าน `work_start` (`t`, `leftWorkingDay`, `skipping`, `dayTime`,
  `pauseTime` และ `skipK` เสริม) และ `day_tick`/`work_end` จาก NDJSON ของ sampler ได้โดยไม่แก้ analyzer เลย
- `day_ticks == N` ⇒ ไม่ INVALID · measured 6.656 s ใกล้ form2 (6.75) ภายใน tolerance 0.1 s ⇒ FORM2
  (ช่องว่าง 94 ms = ความหยาบ quantization 1 เฟรม @60 fps ของโมเดลจำลอง — ไม่ใช่ข้อบกพร่องของ analyzer)
- **ข้อจำกัดที่ยืนยันด้วยการตรวจนี้** (ตรง errata N4): analyzer อ่าน k จาก `work_start` เท่านั้น — fixture นี้
  ไม่ skip จึงยังไม่ครอบรอบ skip; รอบ skip จริงต้องอ่าน k จากฟิลด์ `skipK` ของ `day_tick` นอก analyzer

## ไฟล์

| ไฟล์ | สถานะ |
|---|---|
| `v2a-fixture.ndjson` (13 บรรทัด) | อยู่บนดิสก์ — **ไม่ถูก commit** (`.gitignore:60` = `evidence/**/*.ndjson` ตาม convention) |
| `b4-analyze-result.txt` | commit |
| `b4-analyzer-crosscheck.md` (ไฟล์นี้) | commit |
