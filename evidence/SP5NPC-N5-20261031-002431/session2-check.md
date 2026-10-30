# session-2 check — N5 (label `SP5NPC-N5-20261031-002431`) · 2026-10-31 02:4x

เซสชันเกมครั้งที่ 2/2 (โหลด save01 — โลกหลังพัก ~2 เดือน) · เปิด ~02:09:47 ปิด ~02:43:10
(ML log `26-10-31_2-9-47.log` · ~33 นาที) · ผู้ใช้เล่นตามแนวทาง T6 (ศึกกับตัวละครมีชื่อ ★396/★485
ตามภาพหน้าจอ) · **ตรวจแล้วหยุดรายงาน 2 เรื่อง (T7 TempHeros + T6 นับไม่ได้) — รอมติ**

## hash / เซฟ — ตรงทุกจุด

- GameAssembly `33c56def…` · Mods DLL `c58434b1…` (ไม่เปลี่ยน)
- **เซฟจริง `Save.real-…` = 57 ไฟล์ `d3f7cb2fcae2` ไม่ถูกเขียน** ✓
- สำเนาเปลี่ยน 5 ไฟล์ = `SaveSlot1/*` ×4 + pref (เซฟทับ save01 ตามมติ) ✓ · missing=0 added=0

## ML log — สะอาด

`[ERROR]`=0 · Exception 12 = noise เดิม · probe installed patches=7 · gate installed ·
removed clean: events=851 errors=0 ownedLeft=0

## probe ndjson (`20261031-020952` · 851 events · closeReason=uninstall · ~33 นาที · 1 session — โหลดโลกไม่มี sng ⇒ ไม่ settle ตรงคาด)

- factory.call 212 · **addtemp 115** (กระจาย frame 27280–230464 · ไม่มีชื่อใน event) ·
  genenemy ×19 · worldcount 39 (repair `}` 38 บรรทัด — บั๊ก emission เดิม, lossless)
- errors=0 · readfail=0 · phaseStuck=0 · rebaseline=0

## T5 — census โลกหลังพัก (จุดวัด = genenemy 19 จุด + session.begin)

| frame | h | dictH | sect | dictT(listT) |
|---|---|---|---|---|
| 27280 (แรก) | **647** | 647 | 319 | 8 (83) |
| 70488 | 655 | 655 | 324 | 16 (83) |
| 157789 | 659 | 659 | 330 | 21 (83) |
| 207337–230464 (ท้าย) | **668** | 668 | 334 | 44–49 (83) |

- **ไม่เด้งกลับ 900** — โลกหลังพัก 2 เดือนเริ่มที่ h=647 (จาก 632 ตอนเซฟ) และเติมธรรมชาติช้า ๆ
  (+21 ระหว่างเซสชัน · sect 311→334) = refill ของเกมเองทำงานตามปกติ ตรง L1 ของเกณฑ์ T5
- TempHeros list คง 83 ตลอด (ระบบ temp spawn/despawn วนตามศึก) · ศึกเริ่ม/จบได้ (19 ศึกสุ่ม +
  ศึกตัวละครมีชื่อ ★396/★485 ตามภาพ) · error/crash/soft-lock = 0
- ⇒ **T5 ฝั่งข้อมูลครบ: ประชากรไม่วิ่งกลับ · refill ธรรมชาติ · 0 error** (เวลา ≥25 วัน/≥2 ChangeMonth
  = พยานผู้ใช้ [USER] + หลักฐาน refill เอง)

## T7 — Heros ฝั่ง = CLEAN ทุก 64 จุดทั้งสองเซสชัน · TempHeros ฝั่ง = **พบ finding**

- **HerosDict: dictH==h ทุกจุด** (632/647/655/659/668) · heroID **dup=0** ทุกจุด · leader
  **30/30 exists, missing=0** ทุกจุด · ownHeros **dangling=0** (total 390→413 ตาม refill) —
  **ไม่เข้าเงื่อนไขหยุดของมติ (dictH≠list / ID ชน / leader หาย)**
- **TempHerosDict: S1 (สร้างโลก) dictT==listT==83 ทุกจุด ✓ · S2 (หลังโหลด) dictT=7–55 ≠ listT=83
  ทุก 38 จุด** — และ **dictT == tempMatch (จำนวน temp ที่ isTempHero&&isRandomEnemy) เป๊ะทุกจุด
  ทั้งสองเซสชัน** · listT=83 คงที่ (tempMatch 8→55, tempOther 75→34)
- **หลักฐานกลไก (DUMP):** `TempHerosDict` และ `HerosDict` มี `[JsonIgnoreAttribute]`
  (dump.cs:367696–367701) ⇒ ไม่ถูกเซฟ — โหลดแล้วเกม rebuild เอง · HerosDict rebuild ครบ == list;
  TempHerosDict rebuild ได้เฉพาะ subset = random-enemy temp (INFER จาก invariant — กลไกยังไม่พิสูจน์)
- **การตีความที่เป็นไปได้ (INFER — ยังไม่ยืนยัน):** นี่คือพฤติกรรมเดิมของเกม (dict เก็บเฉพาะ
  random-enemy temp; tempOther = temp จาก plot/mission อยู่ list เท่านั้น) — **ไม่น่าใช่ความเสียหาย
  จากโลกที่ลด** (gate แตะเฉพาะ heroNum ตอนสร้างโลก ไม่แตะ temp/dict path) · ผู้เล่นเล่น 33 นาที
  ศึก/dialog ปกติ 0 error — ไม่เห็นผลกระทบเชิงประจักษ์ · **แต่ตามเกณฑ์ T7 ที่เขียนไว้ ("dict count ==
  list count (Heros / TempHeros)" · NO-GO "dict คลาด") = ข้อ TempHeros ไม่ผ่านตามตัวอักษร** ⇒
  ห้ามตีความเอง — รอมติ

## T6 — คุณภาพ = ผ่านทั้งสองเซสชัน · จำนวน = **นับจาก ndjson ไม่ได้ — รอพยานผู้ใช้**

- 0 dangling-ID error (ML `[ERROR]`=0 ทั้ง 2 เซสชัน · probe errors=0 · ผู้ใช้รายงานไม่มีอะไรพัง)
  · temp hero spawn/ลบวนได้ (addtemp 115 · listT คง 83 · ศึกเริ่ม/จบได้)
- **จำนวน plot/mission ที่เปิด: probe ไม่มี event ตรง** (addtemp ไม่มีชื่อ — แยก plot กับศึกสุ่ม
  ไม่ได้) ⇒ ตามมติ "นับย้อนจาก evidence/ndjson; ถ้า < 20 ให้รายงานและรอสั่ง ห้ามตีความว่าผ่าน" —
  **รายงาน: พิสูจน์ ≥20 จาก evidence เดี่ยว ๆ ไม่ได้ — รอพยานจำนวนจริงจากผู้ใช้ หรือมติอื่น**

## สถานะเซ็ตอัป (คงไว้จนกว่ามติ — ยังไม่ restore/rollback)

split `SP5NPC-N5-20261031-002431` ยัง mount · net.cfg `d1242aa2` (enabled+npcPolicy=light) ·
`Sp5T0=true` · DLL `c58434b1` + rollback point `.prev-6a94abb5-20261031-002431` พร้อม ·
เกมปิด · เซฟจริง aside ไม่ถูกแตะ
