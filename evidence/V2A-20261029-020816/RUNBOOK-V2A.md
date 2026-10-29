# RUNBOOK-V2A — ฉบับสั่งงาน `เริ่ม V2-A session` (V2 ชุด A — T(N) rate ปกติ อ่านอย่างเดียว)

> สำหรับ session ที่ผู้ใช้พิมพ์วลี **`เริ่ม V2-A session`** — วลีต้องแนบมติ **V2A-1 (Gate 0) + V2A-2 (D9) +
> V2A-3 (Gate 5a)** ครบ ไม่ครบ agent ต้องปฏิเสธและแสดงรายการมติที่ขาด (APPROVAL-REQUESTS แพ็กเกจ 2)
> · artifact ที่จะ deploy = `evidence/V2A-20261029-020816/` (provenance READY @ commit `211ea21…`)
> · **ห้ามรวมเซสชันเกมเดียวกันกับ** GO-3/SP5-T0/V1/S3 (กติกาคาดโทษ APPROVAL-REQUESTS)

## ขั้น 0 — session-start (เกมยังไม่เปิด)

1. เกมไม่รัน · hash เกม 2 ไฟล์ตรง `VERSION.md` (`33c56def…` / `451311d4…`)
2. `Mods\LongYinCoop.dll` = `6a94abb5…` · `.pdb` = `856d3bed…` · `.prev` ทั้งคู่ (`1b4c17aa…`/`dcd78ebd…`)
3. เซฟ EXACT 57/57 baseline `df8e5a11beed…` @ `coop-test/baselines/20261028-071340` + `leftovers: none`
   (`python tools\save-split.py status`)
4. `MelonPreferences.cfg` ตรวจ**ค่า key** ไม่ใช่ hash (บทเรียน GO-3): `Enabled = true` (โมด), `AllowRemoteInput = false`,
   ไม่มี key `TimeV2a` หรือมีแล้วเป็น false · `net.cfg` = `12541f34…` (444 B, `enabled=false` — ไม่แตะตลอด session)
   — ตรวจด้วยเครื่องมือ (เพิ่ม 2026-10-29 ตามอนุมัติผู้ใช้ — commit `606f64f`): `python tools\pref_key_check.py`
     **exit 0 = ผ่าน** (fail = หยุดรายงาน)
5. ข้อใดไม่ตรง = **หยุดรายงาน** ไม่ลงมือ

## ขั้น 1 — สำรอง + deploy artifact

1. ตรวจ**ชื่อว่างก่อน**: `Mods\LongYinCoop.dll.prev-6a94abb5` และ `Mods\LongYinCoop.pdb.prev-856d3bed` **ต้องไม่มีอยู่**
   (ถ้ามี = หยุดรายงาน)
2. copy `Mods\LongYinCoop.dll` → `.prev-6a94abb5` + verify sha256 = `6a94abb5…`; ทำเช่นกันกับ `.pdb` → `.prev-856d3bed`
   (verify = `856d3bed…`)
3. deploy: copy `evidence\V2A-20261029-020816\LongYinCoop.dll` + `.pdb` → `Mods\` แล้ว **verify sha256 หลัง copy**
   = `983b2384…` / `951de8fc…` (366,080 / 99,568 B)

## ขั้น 2 — pref ของ sampler + pointer file (ก่อน split/preflight)

1. แก้ `UserData\MelonPreferences.cfg` เฉพาะบรรทัด pref: เพิ่ม/ตั้ง `TimeV2a = true` ใน section `[LongYinCoop]`
   (ค่าอื่นทุกตัวคงเดิม — โดยเฉพาะ `AllowRemoteInput = false` · จด sha256 ก่อน/หลัง แต่**ตรวจปลาย session ด้วยค่า key**)
2. สร้าง pointer file `UserData\LongYinCoop\time-v2a.out` บรรทัดเดียว (path ของ NDJSON **ของ run นั้น** ใน evidence
   ของ label นี้ ผ่าน junction) — **ชื่อไฟล์แยกต่อเคส×รอบ** (มติ 2026-10-29 รอบเริ่ม session: `<เคส>-r<รอบ>.ndjson`
   เช่น `a1-r1.ndjson` — แทนชื่อตายตัว `time-v2a.ndjson` ที่เคยระบุไว้ก่อนหน้า):

   ```
   C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan\LongYinCoop\evidence\<LABEL>\<เคส>-r<รอบ>.ndjson
   ```

   และ `mkdir` โฟลเดอร์ `LongYinCoop\evidence\<LABEL>\` ให้พร้อม (sampler เปิดแบบ append — ไฟล์ว่างเริ่มต้นได้)
   · **touch ไฟล์ปลายทางให้มีอยู่จริง (ว่าง) ก่อน preflight ของ run นั้นทุกครั้ง** (บทเรียน RUNBOOK-G3 — กันไฟล์
     evidence ไม่มีจริงตอนเปิดเกม)
   · **สลับ pointer ต่อ run = แก้บรรทัดเดียวให้ชี้ไฟล์ของ run ใหม่ระหว่างเกมปิด + touch ไฟล์ใหม่** ก่อน split/preflight
     (fail-closed: ไม่มีไฟล์/ว่าง = ไม่เขียน) · pointer ต้องชี้ถูกไฟล์ก่อนผู้ใช้เปิดเกม**ทุกรอบ**

## ขั้น 3 — split + preflight-launch (ต่อรันทุกครั้ง — กฎ 2 ของ time-plan §D.0: รอบซ้ำแต่ละรอบ = เคสอิสระ)

ต่อ**รัน** (สำเนาใหม่ทุกรัน เกมยังปิด — มติ 2026-10-29 รอบเริ่ม session):

```
python tools\save-split.py split --label <LABEL> --expected-baseline coop-test\baselines\20261028-071340\savehash.real.before.txt
python tools\save-split.py preflight-launch --label <LABEL>   # ต้อง "READY - you may launch the game" ก่อนเปิดเกมเสมอ
```

- **preflight-launch** (tools/save-split.py 773–886) = green light แบบ read-only สำหรับกรณี**ไม่มี recorder ภายนอก**
  (sampler อยู่ใน mod DLL) — ตรวจ: เกมปิด · copy mounted · leftovers = 1 Save.real-* · manifest ·
  real-aside-untouched · mounted-is-copy (hash) · **ห้ามใส่ --dry-run** (dry-run ไม่ให้ green light — บรรทัด 882–883)
  · **ไม่เขียน history.ndjson โดยดีไซน์** (docstring 776–777 — `append_history` ไม่มีใน 773–886) ⇒
  **เก็บผล stdout ลง `evidence\<LABEL>\preflight-launch.txt` เป็นหลักฐานแทน**
- **hand-check ก่อนเปิดเกมทุกรัน** (ชดเชยข้อ 4–5 ของ `preflight` เดิมที่ผูกกับ recorder ภายนอก):
  (ก) pointer file `UserData\LongYinCoop\time-v2a.out` ชี้ไฟล์ของ run นี้ — **ตรวจไบต์ด้วย `od -c`** ห้ามแค่ cat
  (ข) ไฟล์ปลายทาง `evidence\<LABEL>\<เคส>-r<รอบ>.ndjson` มีอยู่จริง (touch แล้วจากขั้น 2)
  (ค) `TimeV2a = true` **และ** `Sp5T0 = false` ใน `MelonPreferences.cfg` (ตรวจค่า key)
- **บทเรียน (incident จริง 2026-10-29 ตอนเตรียม A1-r1): เขียน pointer file ด้วย heredoc (`cat > file <<'EOF'`)
  ห้าม printf** — printf ตีความ `\e`/`\a` เป็น escape character ทำ path เสีย — **ตรวจไบต์ด้วย `od -c` ทุกครั้งหลังเขียน**
- baseline อ้างตามมติ [V2A-2]: `df8e5a11beed…` @ `20261028-071340` — **ห้ามใช้ baseline เดิม `4226730f66ff`**
  (errata E1–E3) · ถ้า digest ไม่ตรง = **หยุดรายงาน ห้าม rebaseline กลบ**
- snapshot ก่อนทดสอบ = `savehash.real.before.txt` ของ split นั้นเอง (เกณฑ์ตรวจคืนของรัน)

## ขั้น 4 — ผู้ใช้เปิดเกมเอง + ตารางเคส (ชุด A — Gate 5a; ไม่รวมชุด B/C)

ML log ต้องมีบรรทัด `[TimeV2a] writer open -> …<LABEL>\<เคส>-r<รอบ>.ndjson` หลังงานเริ่มครั้งแรก (ไฟล์ของ run
นั้น — pointer ต้องชี้ถูกไฟล์ก่อนเปิดเกมทุกรอบ ตามขั้น 2)
· **ต้องไม่มี**บรรทัด Harmony/patch/install ใด (มี = หยุด) · และต้องไม่มี SP5/Sp5T0 install/patch (ถ้ามี = หยุด)

| เคส | รอบ | สิ่งที่ผู้ใช้ทำ (ต่อรอบ — สำเนาใหม่ทุกรอบ) | คาดหวังเชิงโครง |
|---|---|---|---|
| A1: N=1 ปกติ | ×3 | เริ่มงาน 1 วัน (job ใดก็ได้ที่กดผ่าน UI ปกติ) → **ไม่กดอะไรต่อ** จนงานจบเอง → ปิดเกม | work 1.75 s (form2) / 1.25 s (form1) |
| A2: N=5 ปกติ | ×3 | เริ่มงาน 5 วัน → รอจบ → ปิดเกม | 6.75 (form2) / 6.25 (form1) |
| A3: N=10 ปกติ | ×3 | เริ่มงาน 10 วัน → รอจบ → ปิดเกม | 13.0 (form2) / 12.5 (form1) — gap 0.5 = จุดตัดสิน |
| A4: N=10 กด `>>` ค้าง | ×3 | เริ่มงาน 10 วัน → กด `>>` ค้างจนจบ → ปิดเกม | **observational เท่านั้น** (errata N4 — ไม่ใช่คำตัดสินฟอร์ม) |

กติกาบังคับทุกเคส (time-plan §E.2/§E.3):

- **บันทึกชนิดงาน (เพิ่มหลัง A2-r3 — มติ 2026-10-29): ในรอบต่อไปให้ผู้ใช้จดชนิดงานของแต่ละรัน และหากเป็นไปได้
  ให้ใช้ชนิดเดียวกันทุกรัน** — NDJSON ไม่มีฟิลด์ชนิดงาน (ไม่มี workName — ตรวจ keys แล้ว 2026-10-29) จึงต้อง
  พึ่งการจดของผู้ใช้เท่านั้น · [สมมติฐาน popup ผูกกับชนิดงาน = UNKNOWN]
- **ถ้ามี popup ระหว่างงาน: ปิด popup แล้วปล่อยงานวิ่งจนจบ — ห้ามกดเริ่มใหม่** · ปิดเกมหลังงานจบแล้วแจ้ง
  agent ทันที — **รันนั้นจะถูกติดป้าย CONTAMINATED และทิ้ง** (ตรวจอัตโนมัติด้วยกฎ gap > 2 s ด้านล่าง ·
  บทเรียน A2-r1: เริ่มงานซ้ำ = work_start ×4 · A3-r2: popup ค้างปั๊ม 16.1 s กลางงาน · A2-r3: popup "ได้ไม้"
  ค้าง 9.0 s หลัง day_tick วันที่ 1)
- ระหว่างงานเดิน **ห้าม action อื่นแทรก** (ไม่เดินทาง ไม่เปิดพล็อต ไม่เปิดเมนู Esc ค้าง — native freeze 3 ทางเป็น
  noise ที่ทำให้ T(N) เพี้ยน) · จด fps/การกระตุกของหน้าจอรายรอบ
- เคส A4 (skip): ก่อนรอบแรก **ยืนยัน `TestMode` ด้วยสองสัญญาณตาม §E.3 ข้อ 1–2** (อ่าน `PlayerPrefDictionary` ผ่าน
  log-only ไม่ได้ใน build นี้ ⇒ ใช้สัญญาณพฤติกรรม: คูณ skip ที่เกิดจาก `skipK` บน `day_tick` (0.5 vs 0.2) +
  ความเร็ว anim ×2 vs ×5) — สองสัญญาณต้องตรงกัน ไม่ตรง = หยุดรายงาน · ห้ามเขียน `TestMode` เด็ดขาด
- จบทุกรอบ: **ปิดเกมเอง** (งานจบก่อนเสมอ — ถ้าจบไม่ได้/soft-lock = หยุดรายงาน) → restore + verify ขั้น 6 → ค่อยรอบถัดไป

## ขั้น 5 — เก็บข้อมูล

- NDJSON แยกต่อเคส×รอบ = `LongYinCoop\evidence\<LABEL>\<เคส>-r<รอบ>.ndjson` (ตามขั้น 2 — มติ 2026-10-29) — ตรวจว่า
  ไฟล์โตและมี `work_end`/`work_abort` ปิด session ครบทุกงานที่ทำ
- ML log คัดลอกบรรทัด `[TimeV2a]` ทั้งหมดไป evidence ของ session

## ขั้น 6 — restore + verify (ปิดเคสก่อนเคสถัดไป — กฎ 2)

```
python tools\save-split.py restore --label <LABEL>
python tools\save-split.py status   # ต้อง EXACT ตามมติ [V2A-2]: 57 ไฟล์ digest df8e5a11beed… + leftovers: none
```

- ไม่ตรง = **หยุดรายงาน** (§G.1) · **ห้ามอ้างว่า fixture ตรง baseline เดิม `4226730f66ff` แบบ EXACT**

## ขั้น 7 — วิเคราะห์ (offline, หลังปิดเกม)

```
python tools\time_v2a_analyze.py evidence\<LABEL>\<เคส>-r<รอบ>.ndjson --tolerance <T>
```

- **`--tolerance` ต้องประกาศก่อนวัด** — เสนอ **0.1 s** (การวัดระดับเฟรม; ดีฟอลต์ 0.5 ทำให้ N=10 ปกติ = INDISTINGUISHABLE
  ตั้งแต่โครง — errata N4) · ผู้ใช้ยืนยันค่านี้ตอนพิมพ์วลีสั่งงาน (ไม่ตัดสินเอง)
- ตัดสิน (ตาม analyzer เดิม ห้ามแก้): `day_ticks ≠ N` = INVALID · measured เข้าใกล้ฟอร์มเดียวภายใน tolerance =
  FORM1/FORM2 · เข้าทั้งคู่ = INDISTINGUISHABLE · **MISMATCH = หยุดรายงาน ห้าม refit โมเดลเอง**
- รอบ skip (A4): ไม่ใช้ verdict ของ analyzer เป็นคำตัดสิน — อ่าน k จริงจาก `skipK` บน `day_tick` (เฉลี่ยรายรอบ)
  + เวลา wall-clock รายงานเป็นข้อมูลประกอบ
- สรุปรวม ≥3 รอบ/ค่า: รายงาน measured รายรอบ + median/spread — ห้ามเลือกรอบที่ "สวย" มาแทนชุด

## ขั้น 8 — ปิด session

1. ปิดเกมแล้ว: restore ของ label สุดท้าย + `status` EXACT baseline + `leftovers: none`
2. **คืน pref ด้วยการตรวจค่า key** (บทเรียน RUNBOOK-G3 — ML เขียน cfg ตอนออกเกม แฮชจึงเปลี่ยนได้เอง):
   `TimeV2a = false` · `AllowRemoteInput = false` · `Enabled = true` คงเดิม · จด sha256 ก่อน/หลังเป็น divergence record
   (ไม่ใช้ hash เป็นเกณฑ์ผ่าน) — ตรวจด้วย `python tools\pref_key_check.py` **exit 0 = ผ่าน** (เพิ่ม 2026-10-29
   ตามอนุมัติผู้ใช้)
3. **`net.cfg` ห้ามแตะตลอด session** — ตรวจท้าย = `12541f34…` (444 B)
4. pointer file `time-v2a.out`: ลบหรือคงไว้ก็ได้ (fail-closed — ไม่มีผลเมื่อ `TimeV2a=false`) — แนะนำ**ลบ**
   เพื่อลดสิ่งแวดล้อมค้าง
5. rollback ถ้าต้องถอน build: คืน `.prev-6a94abb5`/`.prev-856d3bed` (verify hash) — ตัดสินใจเรื่องคง build ใหม่
   = **มติผู้ใช้แยก** (เหมือน G-3 ของ GO-3 — ห้ามตัดสินแทน)
6. รายงานปิด: ผลรายเคส (INVALID/FORM/INDISTINGUISHABLE/MISMATCH + measured รายรอบ + skipK) · hash เซฟก่อน/หลัง ·
   ข้อจำกัด (NOT-RUN รายเคส) · ข้อขัดแย้งกับหลักฐาน static ใด ๆ = หยุดและรายงานทันที (§G.1 ข้อ 4)

## เงื่อนไขหยุดทันที (ระหว่าง session — เจอข้อใดข้อหนึ่ง = หยุด รายงาน ห้ามแก้เอง)

- session-start ไม่ตรง · split/preflight ไม่ผ่าน (digest ≠ baseline `df8e5a11beed…`)
- ML log มี Harmony/patch/install ใด (TimeV2a/SP5) ที่ไม่ใช่ `[TimeV2a] writer open/close`
- day_ticks ≠ N (INVALID), T(N) ผิดปกติชัด (งานไม่จบตามโครง), เกม crash/soft-lock, เซฟ hash เปลี่ยน
- MISMATCH ของ analyzer บนเคสปกติ → **หยุด รายงาน ห้าม refit** · สองสัญญาณ TestMode ไม่ตรงกัน (เคส A4)

## ภาคผนวก A — เกณฑ์ตัดสินฉบับที่ 2 [CRITERIA] (มติผู้ใช้ 2026-10-29 หลัง pilot A1-r1 — ล็อกก่อนรัน A3-r1)

> **สถานะของ A1-r1:** MISMATCH ตาม [TOL]=0.1 เดิมคงอยู่ตามจริง (`analyze-result.txt` ไม่แก้) — จัดเป็น
> **pilot — ไม่นับเป็นผลยืนยัน** (`evidence\V2A-a1r1-20261029-022648\pilot-a1r1-analysis.md`) · ตั้งแต่รัน A3-r1
> เป็นต้นไปตัดสินด้วยเกณฑ์นี้เท่านั้น — ผล `time_v2a_analyze.py --tolerance 0.1` รายรอบคงพิมพ์ตามจริงเป็น
> ข้อมูลประกอบ แต่**ไม่ใช่ตัวตัดสินผ่าน/ไม่ผ่านอีกต่อไป**

- **โครง:** ทุกรัน `day_ticks == N` **และ** ลำดับ event ตรงโครง = `work_start → pause_end → [day_tick →
  pause_end]×N → work_end` (ตาม static §2.4; abort = work_abort แทน work_end)
- **ความชัน:** fit เชิงเส้น `measured = slope×N + intercept` จาก **N=1,5,10 ทุกรอบรวม** (least squares ราย run —
  เฉพาะรอบ normal A1/A2/A3; **รอบ skip A4 รายงานแยก ไม่เข้า fit** เพราะ k ต่าง) —
  slope ต้องอยู่ใน **1.25 ± 0.1 s/วัน** ผิด = **MISMATCH จริง → หยุดรายงาน**
- **ฟอร์ม (ตัวเลขคงเดิมทุกตัว — มติรอบ A):** จุด intercept อยู่ใน **[−0.1, +0.1]** = **FORM1** · ใน
  **[+0.4, +0.6]** = **FORM2** · จุด**นอกทั้งสอง window และไม่ใช่กรณีคาบเกี่ยว** = **UNRESOLVED_OFFSET**
  (รายงานค่า intercept + ช่วงความเชื่อมั่นถ้ามี — เชิงประมาณ ±1.96×SE ของ intercept จาก residual ของ fit เดิม)
  · **INDISTINGUISHABLE สงวนไว้เฉพาะกรณีคาบเกี่ยว/ข้อมูลไม่พอแยก** (ช่วงความเชื่อมั่นครอบ/ทับ**ทั้งสอง**
  window) — ทั้ง UNRESOLVED_OFFSET และ INDISTINGUISHABLE เป็น**ผลที่ยอมรับได้ ไม่ใช่ความล้มเหลว** ·
  **ห้าม refit โมเดล ห้ามเพิ่มพารามิเตอร์แอนิเมชันเข้าสูตรเอง**
- **ส่วนเกินที่อธิบายไม่ได้:** residual รายรัน = `measured − (slope×N + intercept)` รายงานเป็นค่าแยก — **ไม่ใช้
  ตัดสินฟอร์ม**
- **รายงาน observational รายช่วง (มติรอบ A ข้อ A2 — ไม่ใช้ตัดสินฟอร์ม):** ต่อรันและต่อ N รายงาน
  initial (work_start→pause_end#1) · day ต่อวัน (mean/min/max) · final (day_tick สุดท้าย→pause_end สุดท้าย) ·
  tail (→work_end) พร้อมบอกว่าแต่ละช่วงขึ้นกับ N หรือคงที่ — **OBSERVED เท่านั้น ห้ามใส่คำอธิบาย "แอนิเมชัน"
  เว้นแต่มีหลักฐานในไฟล์**
- **pilot:** A1-r1 (MISMATCH ตาม [TOL] เดิม — `pilot-a1r1-analysis.md`) **ไม่นับเป็นผลยืนยัน ⇒ กันออกจาก fit
  ด้วย `--exclude <path>` เสมอ** (ข้อความมติ 2026-10-29 ข้อ 2)
- **กฎคุณภาพข้อมูล (เพิ่มหลังเจอ A3-r2 · ใช้ย้อนกับทุกรัน — ผลของรัน valid เดิมไม่เปลี่ยน):** รันใดมีช่องว่าง
  ระหว่าง event ติดกัน **> 2.0 s ภายใน work_start→work_end** = **CONTAMINATED** — กันออกจาก fit
  อัตโนมัติและรายงานเป็นรายการแยก (พร้อมตำแหน่ง gap และ field ที่ค้าง) · **หลักฐานใช้ย้อน (scan ทุกไฟล์
  2026-10-29):** A3-r1 max 1106 · A2-r2 max 1069 · A1-r2 max 1053 · A1-r1 max 1077 — ทุกตัว < 2000 ⇒
  ผลเดิมไม่เปลี่ยน · A2-r1 43,428–77,289 (INVALID เดิม — ผล excluded ไม่เปลี่ยน) · A3-r2 16,118 ⇒
  **CONTAMINATED** (ตรวจอัตโนมัติ: `GAP_CONTAMINATION_MS = 2000` ใน time_v2a_fit.py ·
  เทสต์ retroactive ครอบไฟล์จริงทั้ง 4)
- **เครื่องมือ:** `python tools\time_v2a_fit.py <ndjson รอบ normal ทุกไฟล์> [--exclude <pilot>]` — ตรวจโครงรายรัน →
  fit รวม → พิมพ์ slope/intercept/CI/verdict/residual/segments รายรัน · exit code: **0** = fit สำเร็จ (รวม
  INDISTINGUISHABLE/UNRESOLVED_OFFSET — ยอมรับได้) · **3** = slope MISMATCH (หยุดรายงาน) · **1** = ไม่มี run
  ใช้ได้/จุดไม่พอ fit · **2** = args/ไฟล์ผิด · ห้ามแก้ `time_v2a_analyze.py`
