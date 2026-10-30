# session-1 check — N5 (label `SP5NPC-N5-20261031-002431`) · 2026-10-31 00:5x–01:0x

เซสชันเกมครั้งที่ 1/2 · ผู้ใช้เปิดเกม ~00:36:29 ปิด ~00:46:04 (ML log `26-10-31_0-36-29.log`) ·
ผู้ใช้รายงาน: สร้างโลกใหม่ → task 24 วัน → ยืนพักหน้าเมืองบน BigMap ~2 เดือน → เซฟ save01
· **ไม่ได้เปิด plot/mission** (ถามกลับว่าทำอะไรบ้าง — ตอบในรายงาน)

## hash / เซฟ — ตรงทุกจุด

- GameAssembly `33c56def…` · Mods DLL `c58434b1…` (artifact ที่ deploy ไม่ถูกแตะ)
- **เซฟจริง `Save.real-SP5NPC-N5-20261031-002431` = 57 ไฟล์ `d3f7cb2fcae2` ไม่ถูกเขียน** ✓
- สำเนาเปลี่ยน 5 ไฟล์ = `SaveSlot1/{Hero,Info,Save,TempHero}` + `PlayerprefData.dat` — **เซฟ slot
  บนสำเนาเท่านั้น ตามมติ** ✓ (missing=0 added=0)

## ML log — สะอาด

- `[ERROR]` = **0** · Exception 12 = type-cache noise เดิม (`UnityEngine.UIElements` TypeLoadException
  ตรงชุดที่รู้จักจาก GO-3) · `Sp5T0 installed (log-only) … patches=7 (before=0)` ·
  `Sp5Npc gate installed (fail-closed …)` · `Sp5T0 removed: events=2423 errors=0 … ownedLeft=0`

## probe ndjson (`SP5T0/20261031-003635/sp5t0.ndjson` · 2,423 events · closeReason=uninstall · 9m28s)

- summary: errors=0 · readfail=0 · phaseStuck=0 · rebaseline=0 · ovCalls C=733 · addtemps=83 ·
  heroNumWindowSum=**50** → heroNumPostRestoreSum=**88** (รูป gate N4 เป๊ะ) ·
  censusLast: zhangmen=30 · loveable list 57/57 present, missing=0 · teacher 311/311
- **เหตุการณ์:** sng.begin/end ×1 · genhero ×1 · **genenemy ×25 (มี worldcount คู่ 25 จุด)**
  · worldcount รวม 55
- **เส้นเวลา:** creation จบที่ frame 9809 · genenemy worldcount กระจาย frame 13202–19520
  (~90 วิแรกของการเล่นหลังสร้างโลก) · หลังจากนั้น (ช่วง task/พัก 2 เดือน — ไม่มีการต่อสู้)
  ไม่มี genenemy เลย ⇒ ไม่มี census ช่วงท้ายเซสชัน (ข้อจำกัดของ probe: census มาเฉพาะตอนเกม
  สุ่มศัตรู) — เซสชัน 2 (โหลด save01) จะวัดโลก"หลังพัก 2 เดือน"ผ่าน genenemy ระหว่างเล่น

### T5 — สถานะหลังเซสชัน 1

| รายการ | ผล |
|---|---|
| ≥25 วัน ข้าม ChangeMonth ≥2 | ตามพยานผู้ใช้ (task 24 วัน + พัก ~2 เดือน) [USER] — ndjson ไม่มี field วันที่ |
| ประชากรไม่วิ่งกลับ 900 | **h=632 นิ่งทั้ง 25 จุด** (creation + 25 จุดระหว่างเล่น) — ไม่มีจุดไหนเด้ง · sect=311 นิ่ง (band N4 [280,350] ✓) · แต่ไม่ครอบช่วงท้าย (ไม่มีศึก) ⇒ **PASS-with-limitation — รอ census หลังพักจากเซสชัน 2** |
| TempHeros band | t=83 นิ่งทุกจุด (addtemp 83 · tempMatch/tempOther ตาม log) |
| error/crash/soft-lock | 0 ✓ |

### T7 — ครั้งแรกบนเกมจริง = **CLEAN ทุกจุด (ไม่เข้าเงื่อนไขหยุด)**

- sng.end: h=632 · hist = protag 1 + spe 170 + sect 311 + wanderer 150 + f2 0 · t=83 ·
  **dictH=632==h · dictT=83==t · dup=0 · leader 30/30 exists, missing=0 · ownHeros total=390
  dangling=0**
- 25 จุด genenemy.end: ค่าเดิมทุก field ทุกจุด (dictH==h · dup=0 · leader ครบ · dangling=0)

## ⚠️ บั๊กที่พบ — จากโค้ด T7 ที่เตรียมไว้ (artifact `c58434b1`)

- **ทุก worldcount ที่มีบล็อก `t7` (54 บรรทัด) JSON ขาด `}` ปิดของ object t7** — บล็อกปิดแค่
  perForce+ownHeros (`]}")` Sp5T0Probe.cs:1430) ส่วน `Commit()` ปิดเฉพาะวงเล็บนอกสุด ⇒
  บรรทัดไม่สมบูรณ์ ไม่สามารถ parse ตรง ๆ ได้
- **ไม่กระทบข้อมูล/เกม** — field ครบทุกตัว เพียงขาดวงเล็บปิดตัวเดียว · repair = เติม `}` ท้ายบรรทัด
  (deterministic, lossless — พิสูจน์ 54/54 parse ผ่าน field ครบ) · บั๊ก emission-only ฝั่ง log
  ไม่แตะ state เกม/hook
- ทางแก้โค้ด = 1 ตัวอักษร (`]}}")`) — **ยังไม่แก้/ไม่ build จนกว่าผู้ใช้เลือกมติ** เพราะมติเดิม
  ปักหมุด artifact = `c58434b1…` (ทางเลือก: ใช้ต่อ+repair ในการวิเคราะห์ vs rebuild+redeploy
  ก่อนเซสชัน 2)

## อื่น ๆ

- `mods\`, `net.cfg` (`d1242aa2…` ระหว่างรัน), pref `Sp5T0=true` — คงสถานะระหว่าง stage ตามแผน
- ไม่มีการแก้ repo จาก session อื่นระหว่างรัน (tree เดิม: context.txt/decisions-pending/evidence ขนาน)
