# Incident — เกมเปิดบนเซฟจริงก่อน split (2026-10-20 ~16:41) · รอบ W2/W4

ตรวจพบตอน deploy (16:43:16) · **รอบนี้ไม่มี hook W2/W4 ในโปรเซส** (DLL ที่โหลด = build W1 `d339fcad`)

## 1. สิ่งที่วัดได้ [RUNTIME]

| ข้อ | ค่า |
|---|---|
| process | `LongYinLiZhiZhuan.exe` pid **16664** (เปิดใหม่หลัง restore 16:36) |
| `save-split status` | `game: RUNNING` · `Save` = **ของจริง 57 ไฟล์** ไม่มี `Save.real-*` (ไม่มี split) |
| hash เทียบ baseline `coop-test/S1.5c-20261020-160858/savehash.real.before.txt` | 57/57 เทียบ: **changed = `Save/PlayerprefData.dat` เท่านั้น** · missing 0 · added 0 |
| `SaveSlot*` (55 ไฟล์) | **ไม่เปลี่ยนเลย** → ไม่มีการบันทึกเกม |
| digest รวม | baseline `db9512bd5b62` → ตอนวัด `2b0548fe464b` (ต่างเพราะ pref ไฟล์เดียว) |
| DLL ใน `Mods/` | `d339fcad…` (build W1) — เขียนทับไม่ได้ `Device or resource busy` เพราะเกมโหลดอยู่ |
| pref W1 `jitprobe` | ปิดอยู่ (ไม่มี patch W1 ในโปรเซสนี้) |

## 2. การวินิจฉัย

- ขั้นตอนที่ตกลง: `split` **ก่อน** เปิดเกมทุกครั้ง — รอบนี้เกมถูกเปิดก่อน split (ผู้ใช้แจ้งว่ากดปุ่มหยุดศึก 3-4 ครั้งเพื่อให้ W4 วัด)
- ผลกระทบ: **W4 วัดไม่ได้ในรอบนี้** เพราะ build ที่โหลดอยู่ไม่มี hook `BattlePauseButtonClicked` — ไม่มีไฟล์ event ถูกสร้าง การกดหยุดศึก 3-4 ครั้งจึงไม่ถูกบันทึก (ไม่ใช่ hook พลาด แต่คือไม่มี hook)
- ความเสียหายต่อเซฟจริง: จำกัดที่ `PlayerprefData.dat` (เกมเขียนเอง) — ข้อมูลเซฟ (`SaveSlot*`) ไม่ถูกแตะ

## 3. สิ่งที่ต้องทำ (ตามลำดับ ห้ามข้าม)

1. ปิดเกม (pid 16664) — agent ห้ามฆ่า process
2. `rebaseline` แบบ volatile-only (รับ pref ใหม่เป็น baseline) หรือคืน pref จาก `coop-test/`
3. deploy build ใหม่ `f0cf25994008…` (มี W2 + W4 + W5/J7 fields) พร้อม backup DLL เดิม
4. `split --label S1.5c-w2w4-<ts>` + `sentinel hide --slot 8` + `verify` → **แจ้ง "พร้อมเปิด"**
5. เมื่อเปิดเกม: `compat_dump` ตรวจ patch ของ LongYinThai บน 5 method ก่อน → ถ้าชนตัวไหน **ข้ามตัวนั้น + รายงาน** → ค่อยเปิด pref W2/W4
