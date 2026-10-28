# E6 — set_pref hardening (ผลวัดจริง 2026-10-29 00:22–00:24, label `S2a4-GO3-20261028-074003`)

เกมรัน: PID 39720 (เปิด 00:00:24, ML log `26-10-29_0-0-24.log`) · ทุกคำสั่งผ่าน `tools/coopctl.py` (IPC named pipe) · recorder หยุดชั่วคราวช่วง E6 (pipe เดียว) แล้วรีสตาร์ทหลังจบ

## ฝั่งที่ 1 — ขณะ S2 **active** (net.cfg enabled=true, overlay=true)

| คำสั่ง (00:22:48) | ผล | ตัดสิน |
|---|---|---|
| `set_pref --key allowRemoteInput --value true` | **`ok:false` · exit 1** — `"rejected: cannot set allowRemoteInput=true while S2 is active (write-path pref stays unchanged; setting false is always allowed)"` | **ถูกปฏิเสธพร้อมเหตุผล** ✓ |
| `spike --sub move --id 0 --row 0 --col 0` (ทันทีหลังปฏิเสธ) | **`ok:false`** — `"DISARMED: coop.allowRemoteInput=false (panic switch)"` | **spike move ยัง DISARMED** ✓ |
| `spike --sub status` | `ok:true` (อ่านอย่างเดียว ทำงานปกติ) | S2/IPC ยัง active ✓ |

## ฝั่งที่ 2 — ขณะ **idle** (ปิด S2 ชั่วคราวผ่าน net.cfg)

| เวลา (wall-clock) | เหตุการณ์ | sha256 |
|---|---|---|
| 00:23:24 (ก่อน) | net.cfg สถานะ S2-on | `5ce166da52fbbcd031495f6f8e974660b5d9cc7e21205947aad6c1733d50a5a5` |
| 00:23:24 (หลังแก้ `enabled=false`) | S2 deactivation path (poll ≤1 s) | `6bfa68ec7699c74538d0149863b58de59db851cc5b9aae5583adc4854851737c` |
| 00:23:2x | `set_pref allowRemoteInput true` → **`ok:true` (exit 0, "รับแบบเดิม")** ✓ → **คืนทันที** `set_pref allowRemoteInput false` → `ok:true` ✓ | — |
| 00:23:45 (เปิด S2 กลับ `enabled=true`) | net.cfg กลับค่าเดิม | **`5ce166da52fbbcd031495f6f8e974660b5d9cc7e21205947aad6c1733d50a5a5` (ตรงก่อนแก้)** · log 00:24:03 `[S2] active transport=tcp phase=B` = active กลับมา ✓ |

- **MelonPreferences.cfg ไม่ถูกแตะค้าง:** hash หลังจบ = `269b571c…98d1` (เท่าเดิม) · `AllowRemoteInput = false` (บรรทัด 9 — ตรวจด้วย grep เดี่ยว ไม่พิมพ์ค่าอื่น)

## สรุป E6

**PASS ครบสองฝั่ง** — ปฏิเสธเมื่อ S2 active (พร้อม error+เหตุผล, write path ยังปิด, spike move DISARMED) · รับแบบเดิมเมื่อ idle · คืนสถานะครบทั้ง pref และ net.cfg · `commandsAccepted=0` ตลอด

## ข้อเท็จจริงเพิ่มเติมที่วัดได้จากรอบนี้ (แก้ UNKNOWN เดิมของ ขั้น 6)

- log S2 status: `auth=2→3, forbidden=6→9` หลังปิด/เปิด S2 1 รอบ ⇒ **self-test TCP รัน 1 ครั้งต่อการ activate** (inject 3 frame ต้องห้าม = Hello/Snapshot/chunked BattleEvent ตาม `S2SelfTest.BuildFrames`, `ExpectedForbiddenDrops=3`) ⇒ `forbidden=6` ตอนเปิดเกม = self-test รัน 2 รอบตอน startup — เป็นพฤติกรรม by-design ของ host (การปฏิเสธ frame ผิดประเภท) ไม่ใช่ความผิดปกติ · เหลือ UNKNOWN เชิงละเอียด: การจับคู่ auth ต่อ activation (selftest/overlay ตัวไหน auth กี่ session)
