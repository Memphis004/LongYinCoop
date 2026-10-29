# สืบเหตุ loveable.heroCount = 631 (N4 รอบ 1) — offline ล้วน · 2026-10-30

> มติผู้ใช้: "สืบ loveable: วิเคราะห์ HeroData.loveAble (dump.cs, บริบท field รอบข้าง, โค้ดเกมที่อ่าน
> flag นี้) เทียบ GDC.loveableSpeHeroList (57) แล้วรายงานว่า heroCount=631 เกิดจากอะไร
> (ความหมาย flag / offset / การอ่านผิด) ห้ามเดา ถ้าสรุปไม่ได้ให้บอกว่าไม่ได้" · session-start
> snapshot ตรงทุกข้อ (05:44) · ไม่แตะ Mods/net.cfg/pref/เซฟ · ไม่ deploy/เปิดเกม

## คำตอบโดยย่อ

**การอ่านถูกต้องทุกชั้น (offset ไม่ผิด) — ที่ผิดคือข้อสมมติของ N4prep ที่ว่า "flag `loveAble` = การเป็น
สมาชิก list 57"** — flag นี้เป็น true แก่ hero เกือบทั้งโลก (RUNTIME ยืนยัน 2 จุดอิสระ) ส่วน list 57
เป็นโครงข้อมูลแยกที่โหลดจาก asset `GameData/LoveableSpeHero` ข้อคาดหวัง "heroCount = 57" จึงเป็น
ข้อสมมติที่**ขัดกับโน้ต RUNTIME ที่มีอยู่แล้วใน repo** (`notes/area-notes.md:170` — true 744/883
ในโลกของผู้ใช้) ซึ่งไม่ได้ถูกปรึกษาตอนเขียน N4prep

## หลักฐานรายข้อ

### 1. offset/การอ่านถูกต้อง — ตัดสมมติฐาน "อ่านผิด offset" [DUMP]

- `Il2CppDumper/dump.cs` บรรทัด 370013: `public bool loveAble; // 0x5F` — บริบทรอบข้าง
  (HeroData @ dump.cs:369988): `speHero 0x5C · tempPlotHero 0x5D · recruitAble 0x5E ·
  **loveAble 0x5F** · hide 0x60 · dead 0x61` · `heroID 0x58`
- probe อ่านผ่าน typed interop (`hd.loveAble` — Sp5T0Probe.cs:1062) ซึ่ง generate จาก dump ตัวเดียวกัน
  ⇒ ไม่มี raw offset ไม่มีทางเพี้ยนชั้น interop
- field นี้มี**เพียงจุดเดียว**ใน dump.cs (ไม่มี field ชื่อคล้ายที่ชนกันได้)

### 2. flag ไม่ใช่การเป็นสมาชิก list 57 — RUNTIME ยืนยัน 2 จุดอิสระ [RUNTIME]

| จุดวัด | โลก | heroCount(flag=true) | ทั้งหมด | สัดส่วน |
|---|---|---|---|---|
| `notes/area-notes.md:170` (P1-8, โน้ตเดิมของ repo · commit `afb70e2`) | โลกจริงของผู้ใช้ (เล่นไปแล้ว, Heros 883) | **744** | 883 | 84.3% (ตัวเอก false) |
| N4 รอบ 1 (`sp5t0-round1.ndjson` 20261030-052706, worldcount genenemy.end) | โลกใหม่สำเนาทดสอบ | **631** | 632 | 99.8% |

- สองโลก สองช่วงเวลา ต่างกันมาก (744 vs 631) แต่**ทั้งคู่ ≫ 57** ⇒ ค่า flag ไม่ผูกกับ list 57 แน่นอน
- หมายเหตุตามจริง: probe รอบ N4 นับเฉพาะจำนวน ไม่ได้บันทึก heroID ใด false รายตัว — "631 = h−1
  และตัวที่ false น่าจะเป็นตัวเอก (ตรงโน้ตเดิม)" เป็นข้อสังเกต ยังไม่ใช่การยืนยันรายตัว

### 3. list 57 เป็นโครงข้อมูลแยก โหลดจาก asset ชื่อเดียวกัน [DUMP + RUNTIME]

- `GameDataController.loveableSpeHeroList` (`List<string>` @0x1D8 — dump.cs:377277) อยู่ในกลุ่ม
  base-data ชุดเดียวกับ `SpeHeroDataBase`/`PlotDataBase` ฯลฯ
- stringliteral.json: `'GameData/LoveableSpeHero'` + `'LoveableLock'` (2 literals เท่านั้น)
- เนื้อหา asset จริงใน `LongYinLiZhiZhuan_Data/resources.assets` (อ่านดิบ read-only, offset 6314108):
  TextAsset ขนาด 629 B, BOM UTF-8, หัวข้อ **`剧情可解锁恋爱角色`** (ตัวละครรักที่ปลดล็อกด้วยบท) +
  **รายชื่อ 57 ชื่อ** รายบรรทัด (杨思迟, 魏胥华, 顾游年, …, 张青蓬) — นับได้ 57 พอดี
  ⇒ ตรง `listCount = 57` ที่ runtime วัดได้ใน N4 รอบ 1
- ชื่อเหล่านี้ปรากฏใน resources.assets หลายร้อยครั้งต่อชื่อ (เช่น 杨思迟 ×538) = ชื่อ hero จริงใน
  ข้อมูลเกม (observational)

### 4. ความหมาย flag ที่หลักฐานสนับสนุน + สิ่งที่ยัง UNKNOWN

- flag จับคู่กับ `recruitAble @0x5E` และมี setter ตระกูลเดียวกันใน dump.cs:
  `PlotController.SetHeroLoveAble(string param)` / `SetHeroLoveAbleNoSound(string param)`
  (dump.cs:386696/386702) — อยู่ในกลุ่ม plot-command ของ PlotController
  (`SetHeroDeath` / `AddWorldNews` / `SetHeroRecruitable` / `HeroLeaveForce`) [DUMP]
- `"loveAble"` **ไม่ปรากฏใน asset ข้อมูลเกมเลย** (grep binary: level0-3 / resources.assets /
  globalgamemanagers.assets = 0 hit) ⇒ flag ไม่ได้โหลดจากไฟล์ข้อมูล — ถูกเขียนด้วยโค้ด [RUNTIME-static]
- รายชื่อ method ที่เกี่ยวกับ love ใน dump.cs ทั้งหมด = ระบบคู่รัก (`Lover`/`PreLovers`/`SetLover`/
  `LoverInteractWithNPC`/`AskHeroToLover` ฯลฯ) — flag อยู่ในระบบความสัมพันธ์ ไม่ใช่ระบบจำแนก SpeHero [DUMP]
- **UNKNOWN (สรุปไม่ได้จากหลักฐาน offline):** ผู้เขียน `true` ตอนสร้าง hero — body ของ
  `GenerateHeroData`/`SetHeroLoveAble` ว่างใน dump.cs (Il2CppDumper ไม่มี body) และ session นี้
  ไม่มี decompiler/เกมรัน ตามขอบเขต offline ⇒ **ไม่ระบุกลไกเขียนได้** (ห้ามเดาตามมติ)
  ทางปิดที่เสนอได้: อ่าน body ผ่าน disassembly (งาน RE แยก ต้องอนุมัติ) หรือยอมรับนิยามใหม่ตาม
  ร่างข้อ 2 ซึ่งไม่พึ่ง flag

### 5. ที่มาของข้อสมมติ "57"

- โค้ด probe (Sp5T0Probe.cs:1062): `if (hd.loveAble) loveableCount++;  // LoveableSpeHero (T2)`
  — comment ระบุตรงว่าสมมติ flag = สมาชิก list
- provenance N4prep (`evidence/SP5NPC-N4prep-20261030-045558/provenance.md`) ระบุคาดหวัง
  "heroCount = **57**, listCount = 57" และแจ้งไว้แล้วว่า "ค่าคาดหวัง 30/57/1 เป็นข้อกำหนดจาก
  design ยังไม่ RUNTIME-confirmed"
- design report เดิม (`evidence/SP5design-20261027/sp5-design-report.md:290`) กำหนด T2 เป็น
  "**LoveableSpeHero 57 ครบ**" = ตรวจ**list** — ไม่เคยกำหนดให้ตรวจ flag
- โน้ต `notes/area-notes.md:170` (RUNTIME 744/883) มีอยู่ใน repo ก่อนเขียน N4prep แล้ว —
  ข้อสมมติของ N4prep ขัดกับหลักฐานที่มีอยู่ (จุดที่ต้องปรับกระบวนการ: ปรึกษา notes/ ก่อนตั้ง
  ค่าคาดหวัง census)

## ข้อสรุปรายทางเลือกที่มติถาม

| สมมติฐาน | ผลสืบ |
|---|---|
| offset อ่านผิด | **ไม่ใช่** — dump.cs ยืนยัน 0x5F, typed interop ตรง, field เดียวใน dump |
| อ่านผิดวิธี (typed read พัง) | **ไม่ใช่** — ค่ามีเหตุผลเชิงโครงสร้าง (631 = h−1, สอดคล้องโน้ตเดิม 744/883) |
| ความหมาย flag ≠ สมาชิก list 57 | **ใช่ — ตัวต้นเรื่อง** — flag true เกือบทั้งโลก; list 57 โหลดจาก asset แยก; สองโครงไม่เกี่ยวกันตามหลักฐาน |
| ใครเขียน flag true ตอนสร้าง | **UNKNOWN offline** (body ว่าง ไม่มี decompiler ในเซสชันนี้) |

## ผลต่อเกณฑ์ T2

- ตัวชี้วัด `loveable.heroCount` **ใช้ตัดสิน T2 ไม่ได้** (คาดหวัง 57 ผิดจากต้น) — เสนอนิยามใหม่ที่
  ตรวจได้ใน `plans/sp5-npc-n4-followup-draft.md` (DRAFT รออนุมัติ — ยังไม่แก้โค้ด)
- ค่าที่เก็บมาแล้วไม่สูญ: `listCount = 57` ✅ = หลักฐาน list ครบ ตาม design เดิม (design:290)
  ที่ runtime แล้ว
