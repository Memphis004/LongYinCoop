# S1.5-d / รอบ 9 — ทดสอบกิ่งปฏิเสธ `turnMoved=true` (1 ครั้ง, ไม่ retry) · 2026-10-20 23:29 [RUNTIME]

build `bbf8cd42be5d` · เกม pid 20232 · อนุมัติโดยผู้ใช้: 1 ครั้ง, arm ครั้งเดียว

## สถานะก่อนทดสอบ (PRE)
```
active=0:0 @ (3,0) · WaitPlayerInput · turnMoved=true · turnAttacked=false
stateHash=dbcd84df2f9d86c6e2e2ab887be90566e1ec9bef212a637d0a75fe5e7e093c1d
```

## ลำดับที่ทำ
```
set_pref allowRemoteInput=true      -> ok (exit 0)
spike arm 60                        -> armed=true (exit 0)   [arm ครั้งเดียว]
spike move id=0:0 row=4 col=0       -> MOVE_EXIT=5, ไม่มี stdout
set_pref allowRemoteInput=false     -> ok
spike disarm                        -> armed=false
```

## ผล [RUNTIME]
```
stderr: prefire: REFUSED: turnMoved=true (unit 0:0 at (3,0) already moved this turn; G3 ALREADY_USED)
stdout: (ว่าง — ไม่ได้ส่งคำสั่งไปยัง mod)
```

**หลักฐานว่าไม่มีอะไรถึง mod:** log ของ mod หลัง `spike arm: 60s` (23:29:13.371) **ไม่มี** `spike move start`
มีเพียง `spike disarm` (23:29:13.724 / 13.884 — ตัวที่สองมาจาก set_pref ปิด panic switch)

## สถานะหลังทดสอบ (POST)
```
active=0:0 @ (3,0) · WaitPlayerInput · turnMoved=true
stateHash=dbcd84df2f9d… (เหมือน PRE เป๊ะ)
```

## ข้อสรุป
1. prefire check ฝั่ง client ทำงานจริงกับเกมจริง: `turnMoved=true` → **exit 5** และไม่ส่ง `spike move` ใด ๆ
2. `stateHash` ก่อน/หลังเท่ากัน → ไม่มี state เปลี่ยน
3. **กิ่ง guard `ALREADY_USED` ใน mod ยัง NOT-RUN** — การทดสอบนี้ถูกบล็อกที่ client ก่อนถึง mod
   (ถ้าจะทดสอบฝั่ง mod ต้องยิง IPC ตรงโดยข้าม prefire = อีก 1 ครั้ง ต้องขออนุมัติแยก)
4. panic switch ปิด + disarm แล้ว · ไม่มี exception · hp/ตำแหน่งไม่เปลี่ยน

## เกณฑ์ S1.5-d
| ข้อ | ผล |
|---|---|
| prefire ปฏิเสธ (`turnMoved=true`) | **PASS 1/1** |
| `stateHash` ไม่เปลี่ยน | **PASS** |
| guard `ALREADY_USED` ฝั่ง mod | **NOT-RUN** |
