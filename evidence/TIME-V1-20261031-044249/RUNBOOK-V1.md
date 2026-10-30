# RUNBOOK-V1 — ฉบับสั่งงาน `เริ่ม V1 session` (V1 Cancellation/persistence — probe log-only 2 เป้า + TimeV2a ร่วม)

> สำหรับ session ที่ผู้ใช้พิมพ์วลี **`เริ่ม V1 session`** — มติที่ผ่านแล้ว (แนบวลี "เตรียม V1 ให้พร้อมรัน"
> 2026-10-31): **[D1]** ปุ่ม UI เท่านั้น (TC-3a/3b = NOT-RUN/limitation ทุกรายงาน) · **[D2]** ยอมรับ
> per-day credit ถาวร (รันบนสำเนาเสมอ) · **[Gate 0 ฝั่ง V1]** อนุมัติ time-plan-draft §D เป็นแผนทำงาน
> (ที่ล้าสมัยใช้ errata) · **[Gate 3]** อนุมัติ probe log-only + ข้อ (ก)–(จ) · **[V1-deploy]** deploy เฉพาะ
> ในวลีนี้ · **[POPUP]** กฎ popup ด้านล่าง · errata = `evidence/AUTO-20261031-035916/time-plan-v1-errata.md`
> · artifact = โฟลเดอร์นี้ (provenance **READY** @ mod repo `6e99c538`) · **ห้ามรวมเซสชันเกมเดียวกันกับ**
> V2/SP5-NPC/S3 (กติกาคาดโทษ TRIGGERS.md)
> · โครงอ้าง: time-plan-draft §D (D.0 กฎ session/สำเนา · D.2 ตาราง TC · D.4 expected · G.1 stop · G.2 rollback)
> · เอกสารบทเรียน: RUNBOOK-V2A (heredoc/od -c/preflight-launch/key-check)

## ขั้น 0 — session-start (เกมยังไม่เปิด — ข้อใดไม่ตรง = หยุดรายงาน)

1. เกมไม่รัน · hash เกม 2 ไฟล์ตรง `VERSION.md` (`33c56def…` / `451311d4…`)
2. `Mods\LongYinCoop.dll` = `6a94abb5…` · `.pdb` = `856d3bed…` (สถานะก่อน deploy)
3. เซฟ EXACT 57/57 baseline **`d3f7cb2fcae2…` @ `coop-test/baselines/20261031-001811`** + `leftovers: none`
   (`python tools\save-split.py status`) — **ห้ามใช้ baseline ยุคเก่าใด** (errata E1)
4. `python tools\pref_key_check.py` — **exit 0 = ผ่าน** (ค่าคาด: `Enabled=true`, `AllowRemoteInput=false`,
   `Sp5T0=false`, `TimeV2a=false`, `TimeV1a=false` — key ใดยังไม่มี = NOTE ตามเครื่องมือ)
5. artifact ตรง provenance: `sha256sum` ของไฟล์ DLL/PDB ในโฟลเดอร์นี้ = `ddbb7fe8…` / `1213b165…`
   · HEAD mod repo ยังมี commit ที่ build นี้อ้าง (`6e99c538` ในสายโซ่) และ `src/` ไม่เปลี่ยนหลัง build

## ขั้น 1 — สำรอง + deploy artifact (เฉพาะวลีนี้ — มติ [V1-deploy])

1. เลือก suffix `<ts>` = เวลาเครื่องจริงของการ deploy · ตรวจ**ชื่อว่างก่อน**: `Mods\LongYinCoop.dll.prev-6a94abb5-<ts>`
   และ `Mods\LongYinCoop.pdb.prev-856d3bed-<ts>` **ต้องไม่มีอยู่** (มี = เปลี่ยน ts ใหม่ — ห้ามทับ backup เดิม
   เช่น `.prev-6a94abb5-20261031-002431` ที่มีอยู่แล้ว)
2. copy `Mods\LongYinCoop.dll` → `.prev-6a94abb5-<ts>` + verify sha256 = `6a94abb5…`; ทำเช่นกัน
   `.pdb` → `.prev-856d3bed-<ts>` + verify = `856d3bed…`
3. deploy: copy `LongYinCoop.dll` + `LongYinCoop.pdb` จากโฟลเดอร์นี้ → `Mods\` แล้ว **verify sha256 หลัง copy**
   = `ddbb7fe8…` (390,144 B) / `1213b165…` (109,536 B)

## ขั้น 2 — pref + pointer file ของ run (ก่อน split/preflight ของ run นั้น)

1. แก้ `UserData\MelonPreferences.cfg` **เฉพาะบรรทัด pref**: `TimeV1a = true` **และ** `TimeV2a = true`
   ใน section `[LongYinCoop]` (ค่าอื่นคงเดิม — โดยเฉพาะ `AllowRemoteInput = false`, `Sp5T0 = false`) ·
   จด sha256 ก่อน/หลัง แต่**ตรวจปลาย session ด้วยค่า key** (`pref_key_check.py --check ...` หรือ default)
2. สร้าง/สลับ **pointer 2 ไฟล์** (บรรทัดเดียว — path ของ NDJSON **ของ run นั้น**):
   - `UserData\LongYinCoop\time-v1.out` → `...\LongYinCoop\evidence\TIME-V1-20261031-044249\<เคส>-r<รอบ>.ndjson`
   - `UserData\LongYinCoop\time-v2a.out` → `...\LongYinCoop\evidence\TIME-V1-20261031-044249\<เคส>-r<รอบ>-v2a.ndjson`
   · **เขียนด้วย heredoc (`cat > file <<'EOF'`) ห้าม printf** — ตรวจไบต์ด้วย `od -c` ทุกครั้งหลังเขียน
   · `mkdir` โฟลเดอร์ evidence + **touch ไฟล์ปลายทางทั้งสองให้มีอยู่จริง (ว่าง) ก่อน preflight ทุกครั้ง**
   · สลับ pointer ต่อ run = แก้บรรทัดให้ชี้ไฟล์ของ run ใหม่ ระหว่างเกมปิด (fail-closed: ไม่มีไฟล์ = ไม่เขียน)
3. เหตุผลที่เปิด TimeV2a ร่วม (errata E7 — ประเมินแล้ว): R3 latch ระหว่างงานที่สอง + กฎ gap อัตโนมัติ +
   cross-check `work_end/abort` เทียบ hook events — ความเสี่ยง = ต้องสลับ pointer 2 ไฟล์ต่อรัน (hand-check
   ทั้งคู่ในขั้น 3)

## ขั้น 3 — split + preflight-launch (ต่อเคส/รันทุกครั้ง — กฎ 2 ของ §D.0: เคสอิสระ = สำเนาใหม่)

```
python tools\save-split.py split --label <LABEL> --expected-baseline coop-test\baselines\20261031-001811\savehash.real.before.txt
python tools\save-split.py preflight-launch --label <LABEL>   # ต้อง "READY - you may launch the game" ก่อนเปิดเกมเสมอ
```

- `<LABEL>` = `TIMEV1-<เคส>-r<รอบ>` (เช่น `TIMEV1-TC1-r1`) — แยกต่อเคส ต่อรอบ · **ห้ามใส่ --dry-run** ·
  เก็บ stdout ของ preflight-launch ลง `evidence\TIME-V1-20261031-044249\preflight-launch-<LABEL>.txt`
- **hand-check ก่อนเปิดเกมทุกรัน**: (ก) pointer ทั้งสองชี้ไฟล์ของ run นี้ — `od -c` ทั้งคู่ (ข) ไฟล์ปลายทาง
  ทั้งสองมีอยู่จริง (ค) `TimeV1a=true` **และ** `TimeV2a=true` **และ** `Sp5T0=false` (ตรวจค่า key)
- snapshot ก่อนทดสอบ = `savehash.real.before.txt` ของ split นั้นเอง (เกณฑ์ตรวจคืนของรัน)

## ขั้น 4 — ผู้ใช้เปิดเกมเอง + ตาราง TC (ลำดับห้ามสลับ — §D.2)

ML log ต้องมี: `TimeV1 installed (log-only): patches=6` (ติดครบ 6) · `[TimeV1] writer open -> …<เคส>-r<รอบ>.ndjson`
· `[TimeV2a] writer open -> …-v2a.ndjson` · **ต้องไม่มี**บรรทัด Harmony/patch/install อื่นใด และไม่มี SP5/Sp5T0
install (มี = หยุด) · ปิด session ต้องเห็น `TimeV1 removed: events=… ownedLeft=0`

| TC | session/สำเนา (§D.0) | สิ่งที่ผู้ใช้ทำ | หมายเหตุ |
|---|---|---|---|
| **TC-1** | สำเนาใหม่ 1 session (กฎ 2) · เงินพอค่าฝึก | เริ่มฝึก 2 วัน (ผู้ใช้กด) → ระหว่างนับวัน**ยกเลิกผ่านปุ่ม UI** → ยืนยัน dialog → สังเกตผล | R1 — คาด: `middlestopwork.enter` → `stopworking.enter finished=false` (route-a) |
| **TC-2** | สำเนาใหม่แยกจาก TC-1 (กฎ 2) | เริ่มงานผลิต 10 วัน → ให้จบไป 3 วัน (จดทรัพยากรเข้ารายวันบนจอ) → ยกเลิกผ่านปุ่ม UI | R1/R2 — คาด: handler `FinishWorkInProductionBuilding` + gate `totalWorkingDay>0` (dialog = hypothesis §D.4) |
| **TC-4 (+4b)** | **state ต่อเนื่อง — session เดียว ห้าม restore คั่น** (กฎ 1+3) | งานแรก**จบปกติ** → เริ่มงานที่สอง (session เดียวกัน) → สังเกตอาการผิดปกติบนจอ → (4b ถ้าอนุญาต: ยกเลิกงานที่สองผ่านปุ่ม UI) → ปิดเกม | R3 — latch: `workFinished` ตอนงานที่สองเดิน (TimeV2a) + pre-state ของ stop ถัดไป |
| TC-3a/3b | **NOT-RUN** | — | มติ [D1] ปุ่ม UI เท่านั้น — บันทึกเป็น NOT-RUN/limitation ทุกรายงาน |

**แบบฟอร์มจดค่าบนจอ (ผู้ใช้จดราย TC — เก็บเป็น JSON/ข้อความ → ให้ analyzer ด้วย `--screen-values`):**
`work_type` (ชนิดงาน — จด**ทุก TC ทุกรัน**), `money_before`, `money_after_cancel` (ตรวจว่าค่าฝึกคืนไหม —
คาด "ไม่พบ refund" ยังเป็น hypothesis), `resource_dailies` (รายวันที่เห็น, TC-2), `resource_after_cancel`
(ตรวจว่าวันที่ผ่านคงอยู่ไหม), `dialog_after_cancel` (มี/ไม่มี + ข้อความโดยย่อ), `calendar_days_passed`,
`ui_anomaly` (ปุ่ม/แผงค้าง/soft-lock หรือไม่)

**กฎ [POPUP] (บังคับ):** popup/ปั๊มค้าง**ก่อน**จุดที่ผู้ใช้กดยกเลิก/ก่อนงานจบ = รันนั้น **CONTAMINATED** —
ปิดเกม → restore → เริ่มใหม่บน split ใหม่ (เครื่องมือตรวจ gap > 2000 ms เป็นสัญญาณ แต่การทิ้งรัน = จาก
พฤติกรรมจอ) · TC-4 เจอ popup = เริ่ม TC-4 ใหม่**ทั้งชุด** · รันเสียติดกัน 3 ครั้งของ TC เดียวกัน = **หยุดรายงาน**
· ปล่อยงานจบแล้วมี popup หลังจุดวัด = บันทึกแต่**ไม่**ทิ้งรัน · ระหว่างงานเดินห้าม action อื่นแทรก (ไม่เดินทาง
ไม่เปิดพล็อตไม่เปิดเมนูค้าง) · จด fps/การกระตุกรายรัน

## ขั้น 5 — เก็บข้อมูล

- NDJSON คู่รายรัน = `evidence\TIME-V1-20261031-044249\<เคส>-r<รอบ>.ndjson` + `…-v2a.ndjson` — ตรวจว่าโต
  และมี `session.end` ปิด (หรือ `TimeV1 removed` ใน ML log)
- คัดลอกบรรทัด `[TimeV1]` และ `[TimeV2a]` ทั้งหมดจาก ML log → โฟลเดอร์นี้
- แบบฟอร์มจดค่าบนจอราย TC → `<เคส>-r<รอบ>-screen.json`

## ขั้น 6 — restore + verify (ปิดเคสก่อนเคสถัดไป — กฎ 2)

```
python tools\save-split.py restore --label <LABEL>
python tools\save-split.py status   # ต้อง EXACT 57 ไฟล์ d3f7cb2fcae2… + leftovers: none
```

- ไม่ตรง = **หยุดรายงาน** (§G.1) · ข้อมูลเซฟจริง (aside) ห้ามแตะเนื้อหา — ตรวจ digest ก่อน/หลังตาม §D.0 (3)

## ขั้น 7 — วิเคราะห์ (offline — หลังปิดเกมรายเคส)

```
python tools\time_v1_analyze.py --probe <probe.ndjson> --v2a <v2a.ndjson> --screen-values <screen.json> --label <LABEL>
```

- **observational เท่านั้น** — R1 (route-a observed?) · R2 (handler + fireWorkFinished + totalWorkingDay;
  dialog = แบบฟอร์มผู้ใช้) · R3 (latch ราย session) · ป้าย CONTAMINATED-CANDIDATE (gap) และ
  STATIC-CONTRADICTION-CANDIDATE (latch=false ใน session 2+) ⇒ **ทั้งสอง = หยุดรายงานตาม §G.1/นโยบาย POPUP
  ก่อนขยายความ** · ห้ามสรุป "ไม่มี refund" เกินสายที่วัด (§D.5) · TC-3a/3b รายงาน NOT-RUN ทุกครั้ง

## ขั้น 8 — ปิด session

1. ปิดเกมแล้ว (ผู้ใช้ปิดเอง): restore ของ label สุดท้าย + `status` EXACT baseline `d3f7cb2fcae2…` + `leftovers: none`
2. **คืน pref ด้วยการตรวจค่า key**: `TimeV1a = false` · `TimeV2a = false` · `AllowRemoteInput = false` ·
   `Sp5T0 = false` · `Enabled = true` คงเดิม — `python tools\pref_key_check.py` **exit 0 = ผ่าน**
   (จด sha256 cfg ก่อน/หลังเป็น divergence record — ML เขียน cfg ตอนออกเกมเอง = ปกติ)
3. `net.cfg` **ห้ามแตะตลอด session** — ตรวจท้าย = `12541f34…` (444 B)
4. pointer files `time-v1.out`/`time-v2a.out`: แนะนำ**ลบ** (fail-closed — ไม่มีผลเมื่อ pref false)
5. **rollback DLL: ถามผู้ใช้** — คง build `ddbb7fe8…` ไว้ หรือคืน `.prev-6a94abb5-<ts>`/`.prev-856d3bed-<ts>`
   (verify hash ทั้งคู่) — **ห้ามตัดสินแทนผู้ใช้** (แบบ G-3)
6. รายงานปิด: ผลราย TC (R1/R2/R3 + measured + screen form) · hash เซฟก่อน/หลัง · NOT-RUN TC-3a/3b ·
   ข้อขัด static ใด ๆ = หยุดรายงานทันที (§G.1 ข้อ 4) · ไม่ปรับเกณฑ์ ไม่ปิด stage เอง ไม่ tag

## เงื่อนไขหยุดทันที (ระหว่าง session — เจอข้อใดข้อหนึ่ง = หยุด รายงาน ห้ามแก้เอง)

- session-start ไม่ตรง · split/preflight ไม่ผ่าน (digest ≠ `d3f7cb2fcae2…`) · ML log มี Harmony/patch/install
  นอกจาก `patches=6` ของ TimeV1 + เดิมของ build · hook ติดไม่ครบ 6 / `ownedLeft≠0` ตอนถอน
- เกม crash/soft-lock · เซฟ hash เปลี่ยน · day/ปฏิทินเดินไม่ตรงที่ตั้งใจทดสอบ (§G.1 ข้อ 5)
- พฤติกรรม runtime ขัด static §B (เช่น finishFuc ไม่ยิงตอนยกเลิก, ยกเลิกได้ใน pause window ผ่าน TryCancel,
  ทรัพยากรรายวันหายหลังยกเลิก) = หยุดรายงานก่อนขยาย
- STATIC-CONTRADICTION-CANDIDATE จาก analyzer · popup ตามนโยบาย (CONTAMINATED ×3 ติดกัน)

## ภาคผนวก A — NDJSON schema ของ probe (TimeV1)

ทุกบรรทัด: `t` (unix ms) · `frame` · `ev` · `errN` (exception กลืนสะสม) · `hasState` (bool) ·
[ถ้า hasState] `working` · `workFinished` · `leftWorkingDay` · `totalWorkingDay` · `nextDayTime` ·
`leftPauseTime` · `resourceNum` · `noCancel` · `skipping` · `hasNames` (bool) · [ถ้า hasNames]
`workName` · `workingFuc` · `finishFuc` · [เฉพาะ stopworking.enter/exit] `finished` ·
[เฉพาะ stopworking.enter] `fireWorkFinished` (= arg `finished` — SPEC จากลำดับ static, errata E3) ·
[เฉพาะ *finalizer] `ex` (ชื่อ exception หรือ null) · event ชุด: `session.begin/end` (field น้อยที่สุด)

event และลำดับคาด (static §B.3): TC ยกเลิก UI = `middlestopwork.enter` → `stopworking.enter(finished=false)`
→ `stopworking.exit` → `middlestopwork.exit` · จบปกติ = `stopworking.enter(finished=true)` → `exit`
(ไม่มี middlestopwork) · ถ้า `stopworking.enter(finished=false)` ไม่มี msw นำหน้า = route-b (per-day handler
เรียกตรง — ใช้บันทึกผู้ใช้แยก TC)

TimeV2a NDJSON = schema เดิมของ V2-A (work_start/day_tick/pause_end/work_end/work_abort — แต่ละ event
พก `workFinished` ใช้ตัด R3)
