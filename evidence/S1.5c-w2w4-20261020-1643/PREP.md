# S1.5c W2/W4 — เตรียมก่อนเปิดเกม (2026-10-20 16:55–16:57)

## ผลแต่ละขั้น [RUNTIME]

| ขั้น | คำสั่ง | ผล |
|---|---|---|
| เกมปิด | `tasklist` | ปิด (ไม่มี `LongYinLiZhiZhuan.exe`) |
| rebaseline (volatile only) | `save-split.py rebaseline --against coop-test/S1.5c-20261020-160858/savehash.real.before.txt` | OK → `coop-test/baselines/20261020-165555/` · changed=1 (`PlayerprefData.dat`) missing=0 added=0 |
| deploy | copy build → `Mods/` | old `d339fcad535a` (W1) → **new `f0cf25994008`** · backup `LongYinCoop.dll.prev-d339fcad535a` |
| split | `split --label S1.5c-w2w4-20261020-165604 --expected-baseline coop-test/baselines/20261020-165555/...` | OK 57 ไฟล์ · real → `Save.real-S1.5c-w2w4-20261020-165604` · copy vs real 0/0/0 |
| sentinel | `sentinel hide --slot 8` | OK `.hidden-SaveSlot8` (5 ไฟล์) |
| verify | `verify --label S1.5c-w2w4-20261020-165604` | **PASS** · copy vs baseline 0/0/0 (นับ sentinel) · real 0/0/0 · drift 0 |

## สถานะพร้อมเปิด

- `Save` ที่ mount = **สำเนา** (57 ไฟล์) · ของจริงแยกที่ `Save.real-S1.5c-w2w4-20261020-165604`
- DLL: `f0cf25994008` (W2 + W4 + W5/J7) · pref `coop.jitProbeW2` / `coop.jitProbeW4` = **false** (ยังไม่ติด patch)
- หลังเกมเปิด ลำดับบังคับ: `compat_dump` → ตรวจ 5 method (`MoveButtonClicked`, `GenerateMovePath`, `CancelMove`, `RegretMove`, `BattlePauseButtonClicked`) ว่ามี patch ของ LongYinThai → ชนตัวไหนข้ามตัวนั้น + รายงาน → แล้วค่อยเปิด pref W2/W4

## Script สำหรับรอบนี้

- `tools/coopctl.py set_pref --key jitprobeW2 --value true`
- `tools/coopctl.py set_pref --key jitprobeW4 --value true`
- `tools/spike_autocapture.py` (status อย่างเดียว) — ยังไม่ปรับให้ sample 100 ms สำหรับ J7; ต้องมีสคริปต์แยกหรือใช้ `--interval` (ปัจจุบันไม่ sleep บน success = ยิงถี่สุด)
