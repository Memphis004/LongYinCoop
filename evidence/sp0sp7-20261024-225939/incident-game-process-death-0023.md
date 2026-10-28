# incident: โปรเซสเกมตายกลางการทดสอบ §4 (รอบ 4) — 2026-10-25 ~00:23

## ข้อเท็จจริง (RUNTIME)

- รอบ 4 (สร้างโลก Free+ทหาร หลังติด hook v2): ผู้ใช้รายงาน "เข้าฉากโลกเดินได้แล้ว" แต่
  frida agent script คืน `script has been destroyed`
- `tasklist`: PID เปลี่ยนจาก **21176 → 8380** ⇒ โปรเซสเกมเดิมตายและถูกเปิดใหม่
- MelonLoader logs คืนนี้: `26-10-24_23-28-47` → `26-10-24_23-29-22` (เปิดห่างกัน 35 s —
  session แรกจบทันที) → `26-10-25_0-23-2` (session ปัจจุบัน, heartbeat ปกติ err=0)
- **ไม่พบ crash dump** (`*_Data/Crashes`, `Crashes`, `%TEMP%\LongYinLiZhiZhuan`, error.log ใหม่ = ไม่มี)
- สาเหตุการตายของโปรเซส = **ผู้ใช้ปิดเกมเองโดยไม่ตั้งใจ** (คำตอบผู้ใช้ 2026-10-25: "ผมเผลอปิดเกมครับ เลยเข้าใหม่")
  — ไม่ใช่ crash ของเกม; ไม่มี soft-lock, ไม่มีผลต่อเซฟ
- hook log รอบ 4 (v2) อยู่ในหน่วยความจำของ script ที่ตาย ⇒ **หายทั้งหมด** — chain เต็มของ §4 ยังไม่ได้

## ผลกระทบต่อเซฟ

- hash diff หลัง restart ทั้งหมด: changed=1 = `PlayerprefData.dat` (volatile) — `SaveSlot*` ไม่ถูกเขียน
- สถานะ split คงเดิม (`Save.real-sp0sp7-20261024-225939` aside, mounted = copy) — PASS

## สถานะโลกปัจจุบัน (อ่าน 00:26 ผ่าน frida attach ใหม่, PID 8380)

- gameMode=Free, worldTime ปี 1 เดือน 1 วัน 1, heroID=0 "สมสมศรี",
  belongForceID=-1 (ไม่มีสำนัก), atAreaID=-1 (BigMap), money=1000 — ตรงสเปครอบ 4 ที่สั่ง
- โลกนี้ (สร้างหลัง relaunch) **ไม่มี hook ครอบคลุมการสร้าง** — ใช้ต่อสำหรับ §5 ได้ แต่ §4 ต้องรอบใหม่

## ข้อสังเกตเพิ่ม

- คู่ session 23:28:47/23:29:22 อาจเป็น double-launch (Steam/ผู้ใช้) — ยังไม่ได้ถามผู้ใช้
- hook v1 รอบ 3 รอดจนดึง log ได้บางส่วน (sp4-hook-v1-partial.md) — หลักฐาน §4 ปัจจุบัน = partial
