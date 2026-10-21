# incident — เกมรันบนเซฟจริงก่อน split (2026-10-21 16:07–16:10)

## 1. ข้อเท็จจริง [RUNTIME]

| เวลา | เหตุการณ์ | หลักฐาน |
|---|---|---|
| 16:03:31 | pre-flight: เกม **ปิด**, `Save` 57 ไฟล์ digest `2b0548fe464b`, leftovers none | `logs/status-preflight-1603` (บันทึกในแผน §0) |
| 16:07:51 | เกมเริ่มทำงาน **pid 28084** (ผู้ใช้เปิดเองตามที่เลือก "ผมเปิดเกมเอง") | `Get-Process` |
| 16:09:5x | ผมสั่ง `save-split.py split --label S1.5g-20261021-160331` | `logs/split.log` |
| — | **REFUSED: LongYinLiZhiZhuan.exe is running** (guard ของ tool ทำงานถูก) | `logs/split.log` |
| 16:10 | `status`: `Save` 57 ไฟล์ digest `046ceb7a4810`, leftovers none | `logs/status-pre-split.log` |
| 16:10 | `verify --label S1.5f-…`: **changed=1 (PlayerprefData.dat)**, missing 0, added 0 | `logs/verify-game-running.log` |

## 2. การประเมินความเสียหาย

- **`SaveSlot0`–`SaveSlot10` ไม่ถูกเขียน** (changed = 0 เทียบ baseline `2b0548fe464b`)
- ไฟล์ที่ต่างมีไฟล์เดียว = `PlayerprefData.dat` ซึ่งเป็น volatile (D10) — เกมเขียนทับตอนเปิด/ปิดตามปกติ ไม่ใช่ข้อมูลการเล่น
- log mod ช่วง 16:07:55–16:09:46: `battle=0/0`, ไม่มี `Save`/`Load`, ไม่มี exception
- **ยังไม่มี gameplay ใด ๆ เกิดขึ้นบนเซฟจริง** (ไม่เข้าเกม ไม่เข้าศึก)

## 3. การตัดสินใจ

1. **หยุดทันที** ไม่เริ่ม Phase A/B บนเซฟจริง
2. ให้ปิดเกมแบบไม่ Save (ห้ามเข้าเกม/ห้ามกดบันทึก) → ผมยืนยัน process จบ
3. จากนั้น `split` สำเนา (baseline ของ S1.5g = สภาพจริง ณ ตอนปิดเกม; จะบันทึกว่า pref ต่างจาก canonical `2b0548fe464b` เพราะเกมถูกเปิดก่อน split)
4. ให้ผู้ใช้เปิดเกมใหม่ → รันบนสำเนาเท่านั้น → เริ่ม Phase A

## 4. ผลต่อ exit criteria

- ยังไม่มีข้อมูลใดถูกนับเป็น PASS/FAIL ของ S1.5g
- เกณฑ์ "Save จริงไม่เปลี่ยน" ต้องรายงานตามจริงว่า **`PlayerprefData.dat` เปลี่ยน 1 ไฟล์ก่อนเริ่มทดสอบ** (สาเหตุ: เปิดเกมบนเซฟจริงช่วง 16:07–16:10) และ `SaveSlot*` ไม่เปลี่ยน
