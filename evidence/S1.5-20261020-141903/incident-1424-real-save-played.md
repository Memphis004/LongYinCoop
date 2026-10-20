# Incident 2026-10-20 14:24 — เกมถูกรันบนเซฟจริงระหว่างเตรียมรอบ 3

สถานะ: **หยุดก่อนรอบ 3** (`split` ถูกปฏิเสธเอง) · ไม่มีคำสั่ง IL2CPP จาก mod · ไม่มี `SaveSlot*` เปลี่ยน

## ไทม์ไลน์ [RUNTIME — Steam `logs/cloud_log.txt` + mtime/hash]

| เวลา | เหตุการณ์ |
|---|---|
| 14:21:43 | เกมปิด (`AC Exit`) — ตรงกับที่ผู้ใช้แจ้ง "ปิดเกมแล้ว" |
| 14:22:05 | restore สถานะ S1 สำเร็จ (57/57 ตรง baseline) |
| 14:22:20–14:22:48 | dry-run 1 + 2 (split→verify→restore) ผ่านทั้งคู่ |
| 14:23 | `status`: `Save` digest `03d9559772da` = ของจริง |
| **14:24:33** | **เกมถูกเปิด (`AC Launch`)** — ไม่ได้เกิดจากคำสั่งของ agent ใน session นี้ |
| **14:24:47** | `Save/PlayerprefData.dat` ถูกเขียน (mtime ใหม่, hash ใหม่) |
| 14:26:37 | เกมปิด (`AC Exit`) · AutoCloud on exit |
| 14:27:08 | `split --label S1.5-20261020-142708` **ถูกปฏิเสธ** เพราะ hash ไม่ตรง baseline |

## ผลกระทบ [RUNTIME]

| ไฟล์ | ก่อน (baseline S1) | หลัง |
|---|---|---|
| `SaveSlot*` 56 ไฟล์ | — | **เปลี่ยน 0** (hash ตรง baseline ทุกไฟล์) |
| `PlayerprefData.dat` | `8038352c…dc5abc` (14,953 B) | `e6e845c3…438f87` (14,953 B, mtime 14:24:47) |

- `Save` digest ปัจจุบัน `dc537a32ef96` · ไม่มี `Save.real-*`/`Save.used-*` · เกมปิด
- ไม่มี label `coop-test/S1.5-20261020-142708/` ถูกสร้าง (split ปฏิเสธก่อน mkdir)
- hash ใหม่ไม่ตรงกับไฟล์ใดใน `coop-test/` → เขียนโดยเกม ไม่ใช่สคริปต์ของเรา

## ข้อเท็จจริงเรื่อง Steam Cloud [RUNTIME]

- AppID `3202030` · `remotecache.vdf` มี rule sync ทั้ง `LongYinLiZhiZhuan_Data/Save/*` และทุก `SaveSlot*/`
- log ระบุ `Sync Disabled` และ `Need to upload file … but not attempting now` → **ยังไม่มีการอัปโหลด/ดาวน์โหลดจริง**
- AutoCloud ตรวจ SHA ของ `PlayerprefData.dat` ทุกครั้งที่เกมเปิด/ปิด (`ValidateCache`)

## ผลต่อเกณฑ์

- §1.2 ข้อยกเว้น `PlayerprefData.dat` (volatile) ยังใช้ได้ **แต่** split gate ในแผน §1.0 บังคับ 57/57
  → ต้องให้ผู้ใช้ตัดสินว่าจะรับ pref ใหม่เป็น baseline หรือหยุด
- §1.4 ข้อ 3 ยัง NOT-RUN
