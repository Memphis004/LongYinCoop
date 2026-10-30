# Rollback DLL ตามมติผู้ใช้ "ยกเลิก V1" (2026-10-31 — docs session)

## มติ/ที่มา
- ผู้ใช้เลือก **"ยกเลิก — rollback DLL เป็น 6a94abb5"** ใน docs session 2026-10-31 (หลังรายงานสถานะ V1: TC1-r1 = INCOMPLETE-probe-down — TimeV1 ติด **2/6** patch แล้วถอนทั้งชุด = เข้าเงื่อนไขหยุดทันทีตาม RUNBOOK §G.1 "hook ติดไม่ครบ 6")
- ขั้นที่ทำ = RUNBOOK-V1 ขั้น 8 ข้อ 5 (rollback DLL — "ถามผู้ใช้") · restore เซฟ (ขั้น 8.1) ทำแล้วโดยเซสชัน V1 เอง 05:50:47 (`restore-20261031-055047.json` — digest `d3f7cb2fcae2…` ✓)

## การกระทำ + hash (วัดจริง)
| รายการ | ก่อน | หลัง |
|---|---|---|
| `Mods\LongYinCoop.dll` | `ddbb7fe87896f58fb56c86d4483ffdc60e069d4e0d728c5eafcbbf457f60d30a` (TimeV1) | **`6a94abb579e65d5e6b0af917dfed4ba6f5350ad76847190bd7263cda3077b334`** ✓ |
| `Mods\LongYinCoop.pdb` | `1213b165f917ce775df9d604e697cf2540e6c943d438ca15234540d9cf936fdc` | **`856d3beda727218823170f861f532509c5458eb91e51c79f07c0452d3b196035`** ✓ |

- วิธี: copy กลับจาก `LongYinCoop.dll.prev-6a94abb5-20261031-050028` / `LongYinCoop.pdb.prev-856d3bed-20261031-050028` (verify เนื้อ .prev ก่อน copy = `6a94abb5`/`856d3bed` ✓) — **ไม่ลบ .prev** (ยังอยู่ครบ 2 ไฟล์หลังทำ)
- เกมปิดตลอด (tasklist count = 0 ก่อน/หลัง)
- build TimeV1 (`ddbb7fe8`) ยังเก็บใน artifact โฟลเดอร์นี้ (`LongYinCoop.dll`/`.pdb` mtime 04:43) — ไม่สูญหาย

## สถานะค้างที่ตรวจหลัง rollback (read-only)
- pref: `TimeV1a=false` · `TimeV2a=false` · `Sp5T0=false` · `AllowRemoteInput=false` · `Enabled=true` — **ครบแล้ว** (เซสชัน V1 รีเซ็ตก่อนแล้ว ตามขั้น 8.2)
- pointer `time-v1.out`/`time-v2a.out`: ไม่พบ (ลบแล้วตามขั้น 8.4)
- `net.cfg` = `12541f34c633…` ✓ (ไม่ถูกแตะตลอด)
- เซฟจริง: restore แล้ว 05:50:47 (`d3f7cb2fcae2…`) — **ต่อมาเกมเปิดบนเซฟจริง 06:02:12 (ML log `26-10-31_6-2-12.log`) แล้วปิด ⇒ drift เฉพาะ `PlayerprefData.dat`** (digest ปัจจุบัน `7cd024019604`, SaveSlot* ไม่ถูกเขียน — ต้อง rebaseline volatile-only เมื่อมีมติ)

## สิ่งที่ยังค้างของสาย V1
- รายงานปิด session ของเซสชัน V1 (ขั้น 8.6) — ยังไม่เห็นไฟล์ (ไม่แตะ — เป็นของเซสชันนั้น)
- สาเหตุ probe ติด 2/6 (คาด 6) = UNKNOWN — ต้องสืบก่อนรัน V1 รอบใหม่ (build `ddbb7fe8` คงอยู่ใน artifact)
- ผล TC1-r1: R1 = ไม่มีข้อมูล (probe down, 0 events) · TimeV2a = 5 events (work_start → pause_end → day_tick 2→1 → pause_end → work_abort; gap 43 s = contaminated candidate) · ฟอร์มผู้ใช้ครบ (`analyze-TC1-r1.json`, `TC1-r1-screen.json`)
