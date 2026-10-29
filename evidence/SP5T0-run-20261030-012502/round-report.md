# รายงานรอบรัน SP5-T0 — session `SP5T0-run-20261030-012502` (ผู้ใช้เปิดเกม 01:31:40 ปิด 01:35:03)

> แผน `plans/sp5-t0-plan.md` rev 13 · มติ §4 ข้อ 2–8 (วลี "รัน SP5-T0") · probe = build `865b4a87`
> (source `ab9d07b`) · log-only 100% · ปลายทาง log จริง: `UserData\LongYinCoop\SP5T0\20261030-013144\`
> (สำเนาเก็บใน evidence โฟลเดอร์นี้) · **ผู้ใช้ทำเอง:** เปิดเกม → สร้างโลกใหม่ทันที (ไม่เปิด
> plot/mission/recruit ระหว่างรอบ) → ปิดเกม

## 0) สรุปผูกมัด

- **ติดตั้ง/ถอนสะอาด:** `Sp5T0 installed (log-only): patches=7 (before=0)` (ML log 01:31:44) ·
  `Sp5T0 removed: events=3014 errors=0 unattributed=0 outside=0 phaseStuck=0 rebaseline=0 readfail=0
  ownedLeft=0` (01:35:03) · patch count คืน 0 ตาม contract
- **ML log ไม่มี ERROR เลย** (0 บรรทัด — รวม noise type-cache เดิมก็ไม่พบ)
- **เงื่อนไขความถูกต้องของรอบ = ครบทุกข้อ** (§5) ⇒ รอบนี้ใช้ตีความบัญชีได้ (ยกเว้นสมการที่ rev 13
  กำหนดให้ UNKNOWN ตามโครงสร้าง)

## 1) เงื่อนไขความถูกต้องของรอบ (§5 — ครบก่อนตีความสมการ)

| เงื่อนไข | ผล | สรุป |
|---|---|---|
| `phaseStuck == 0` (นิยาม rev 4) | 0 events · finalizer `phasePop=true` 969/969 · stack ว่างที่ session.end | PASS |
| Δ-count ใช้ instance เดียวกัน / rebaseline อธิบายครบ | `wid/hidH/hidT` = {2944718092640, 2944753401856, 2944753409936} **คงที่ทุก snapshot** · rebaseline=0 | PASS |
| `Σ hist == H` ทุก snapshot (E7) | 7 snapshots: ครบทุกจุด (session.begin = world null, h=0 — ยังไม่มีโลก ตามคาดหมาย) | PASS |
| `Σ perForce == hist[sect]` ทุก snapshot | PASS ทุกจุด | PASS |
| E5: factory call ใน creation อยู่ W2/W3 เท่านั้น | creation 969 calls = 882 `sng.genhero` + 87 `sng.genenemy` · **`sng.unattributed` = 0** · ไม่มี `plot`/`recruit` · pre/post = 0 events | PASS |
| E6 ตามลำดับตรวจ | (1) `sng.unattributed`=0 (2) `#C.calls(W2)`=882 ≠ 0 ⇒ เงื่อนไข "path ที่สอง" ไม่กระตุ้น — ไม่มีข้อสงสัย non-factory path (SP4 ยอด 882/81/883/963 = เทียบเคียงเท่านั้น) | PASS |
| patch.audit 4 จุดตรวจ | ครบทั้ง 4 จุด (session-begin / sng-prefix / genhero-prefix / genenemy-prefix) · `allStableBetweenCheckpoints=true` ทุกคู่ · **inventory ดิบ = เฉพาะ patch ของ probe** (pre=1, post=1, fin=1, trans=0, ilm=0 ต่อ overload A/B/C) · **แต่ `allMatch=false` ทุกจุด = บั๊กรูปแบบสายอักขระของ comparator ใน probe** (รายละเอียด §4) — เชิงความหมาย (owner+method+จำนวน) ตรง expected **ทุกจุด** | PASS-with-bug (บั๊กเครื่องมือ ไม่ใช่ข้อมูล) |
| เกณฑ์ GO §5 ข้อ 2 ส่วน runtime | ordering จริงของ Harmony รันแล้วครบ 969 triples (call→result→finalizer ผูก iid) | มีหลักฐานแล้ว |

## 2) ข้อมูลบัญชีของรอบ (ตัวเลขจริง)

- **เส้นเวลา:** session.begin 01:31:44 → `sng.begin` +172.6 s (ผู้ใช้อยู่หน้าสร้างตัวละคร) → `genhero`
  1.0 s → `genenemy` 0.24 s → `sng.end` (+1.63 s จาก sng.begin — เทียบค่าเคยวัด ~1 s: โอเคสำหรับรอบ
  log 715 KB, observational) → settle 24.5 s → session.end (ผู้ใช้ปิดเกมก่อนครบ 10 นาที — อนุญาตตาม §2.5 ก)
- **heroNum.read (ทางหลัก T0 ที่ `GenerateHero` prefix):** forces=4 · entries=24 ·
  `[[4,4,3,3,2,1],[5,4,4,3,3,2],[5,5,4,4,3,2],[6,5,5,4,4,3]]` · **Σ=88** · `speDb` ตาราง=170 ·
  identity ตรง snapshot (wid/hidH/hidT)
- **factory calls:** **A=0 · B=0 · C=969** (creation ทั้งหมด) — 969/969 `factory.result` nonNull ·
  969/969 `factory.finalizer` รายงาน `exceptionAtProbeFinalizer=null` · `factory.failure` = 0 ·
  iid 1..1056 ไม่ซ้ำไม่ขามือ (969 factory + 87 addtemp คนละ kind ไม่ทับกัน)
- **บัญชี Heros (W2):** ΔH = 0→**883** ที่ `genhero.end` (H(sng.end) ตรง บน instance เดียวกัน) ·
  census ที่ genhero.end: **protag 1 · spe 170 · sect 562 · wanderer 150 · f2 0 · temp 0 ·
  tempPlot 0 · other 0** (รวม 883)
- **บัญชี TempHeros (W3):** `tempBefore`=0 → `tempAfter`=**87** · `addtemp`=87 · W3 calls=87
- **การจำแนกจาก post-state (typed จาก `__result`, rev 6):** arg flags (isTemp,isRandom) = (F,F)×882 +
  (T,T)×87 — **ตรงกับ post-state 100%** (ไม่มี arg ที่ต่างจากค่าหลังเรียก)

## 3) สมการ E1–E7 (ตาม rev 13 — ถ้อยคำต้องเป๊ะ)

| # | ผล | ถ้อยคำที่อ้างได้ |
|---|---|---|
| E1 | **UNKNOWN (ตามโครง rev 13)** — โครงตัวเลขสอดคล้องเป๊ะ: ΔH 883 == #C(W2, post !temp && !random) 882 + 1 | ทุกเงื่อนไขที่สังเกตได้ครบ (result nonNull + finalizer null + audit เชิงความหมายตรง + instance เดียว + phaseStuck 0) แต่ **`inventoryStability` ภายใน window ไม่มีกลไกพิสูจน์ใน build นี้** ⇒ ห้ามสรุป pipeline-success ⇒ E1 คง UNKNOWN — สิ่งที่อ้างได้: "ไม่มี exception ที่สังเกตได้ในขอบเขตของ patching pipeline ที่ตรวจสอบแล้ว (ณ จุดตรวจ 4 จุด)" |
| E2 | **UNKNOWN (ตามโครง rev 13)** — โครงตัวเลขสอดคล้องเป๊ะ: ΔT 87 == #addtemp(W3) 87 == #C(W3, post temp && random) 87 | เช่นเดียวกับ E1 |
| E3 | **ปิดเชิงสังเกต** — residual +1 ของ E1 = protag (census `protag=1`, สร้างนอก factory — ตรงสมมติฐาน ctor ใน `GenerateHero`) | เชิง observational ของรอบนี้ |
| E4 | **ปิดด้วยกฎที่ทำนาย world-state ตรงเป๊ะ (969/969):** `heroDataBase` nonNull ↔ `postSpeHero=true` (170/170 — ครบตาราง 170, ไม่ lazy ในโลกนี้) · db null ↔ generic (799 = 562 sect + 150 wanderer + 87 temp) · ใน generic W2: `belongForceID` 0..29 ↔ sect (562 ตรง census) / −1 ↔ wanderer (150 ตรง census) — กฎสรุปจาก**ชุดหลักฐานรวม** (db + post-state + census) ตามข้อกำหนด "ห้ามใช้ตัวเดียวตัดสิน" | ผ่านเกณฑ์ "กฎที่สรุปได้ทำนาย world-state classification ได้ตรง" — **ข้อจำกัด:** สรุปจากรอบเดียว 1 โลก (band เดียว); การอ้างข้ามโลก/ข้าม force-set ต้องรอบเพิ่ม |
| E5 | **PASS** — creation ครบ W2/W3, `sng.unattributed` = 0, `plot`/`recruit` = 0 | เส้นทาง creation ถูกอธิบายครบด้วย 2 window ที่รู้จัก (รอบนี้) |
| E6 | **PASS** — ตามลำดับตรวจ: unattributed=0 และ #C.calls(W2)=882 ≠ 0 ⇒ ไม่เข้าเงื่อนไข path ที่สอง | SP4-check ด้วยตัวเลขรอบตัวเอง — ยอด SP4 ไม่ถูกใช้เป็นเกณฑ์ |
| E7 | **PASS** — Σhist==H และ ΣperForce==hist[sect] ทุก snapshot (7/7) | snapshot semantics ตาม rev 6 |
| E8 | N/A | OPTIONAL H6/H7 ไม่ติด (มติ ข้อ 5) — ไม่ใช่ของ T0 |

- **ข้อสังเกตเชิงเปรียบเทียบ (ไม่ใช่เกณฑ์):** 882 W2-calls / 883 Heros ตรงยอด SP4 เดิมพอดี; SpeHero 170 =
  ขนาดตาราง 170 ⇒ โลกนี้ instantiate ครบทุกตัว (ข้อสันนิษฐาน "hidden lazy" **ไม่จริงสำหรับโลกแบบนี้** —
  observational รอบเดียว) · `heroNum` Σ=88 ≠ 562 sect ⇒ heroNum **ไม่ใช่** จำนวนสมาชิกสำนักของโลก
  (ความหมายจริงคง UNKNOWN นอกขอบเขต T0 — บันทึกไว้กันการเข้าใจผิด)

## 4) บั๊ก/ความคลาดเคลื่อนที่พบรอบนี้ (บันทึกตามจริง — งานถัดไป)

1. **บั๊ก comparator ใน probe:** expected string สร้างรูป `longyincoop.sp5t0.probe.Sp5T0Probe.GenData*`
   แต่ `HarmonyX GetPatchInfo` คืน `longyincoop.sp5t0.probe:Sp5T0Probe::GenData*` ⇒ `allMatch=false`
   **ทุกจุดตรวจ แม้ inventory จริงตรง expected เชิงความหมายทุก category** — ผลกระทบ: ไม่เปลี่ยน
   verdict ใด (E1/E2 UNKNOWN อยู่แล้วตาม rev 13; หลักฐานดิบ inv อยู่ใน log ครบ) — **แก้รอบถัดไป:**
   normalize ตัวคั่นก่อนเทียบ (แก้ probe + harness ต้องอนุมัติแยก)
2. **schema mismatch ของ `tools/sp5t0_audit.py` (repo):** tool เขียนคาด `perForce` เป็น dict แต่ probe
   เขียนเป็น list 30 ช่อง ⇒ tool ล้ม (`AttributeError`) — ใช้ `analyze_round.py` ใน evidence โฟลเดอร์นี้
   แทน (ปรับตาม schema จริง + เทียบเชิงความหมาย) — tool ใน repo ควรแก้ตอนแก้ข้อ 1
3. **PDB size ใน provenance prep:** เอกสารเขียน 99,572 B, ไฟล์จริง 99,568 B — **sha256 ตรงตามตาราง
   เป๊ะทุกไฟล์** (ตัวเลขขนาดคลาดเอง) — บันทึกใน VERSION.md แล้ว

## 5) สถานะหลังคืนสภาพ (ปิด session)

- **restore เซฟ:** `restore --label SP5T0-20261030-012502` OK — `Save` = ของจริง renamed กลับ
  **57 ไฟล์ `df8e5a11beed` EXACT** baseline `20261028-071340` · `leftovers: none` · สำเนาทดสอบเก็บ
  `coop-test/SP5T0-20261030-012502/Save.used-20261030-014410` (drift เฉพาะ `PlayerprefData.dat`)
- **ระหว่างรอบ:** `SaveSlot*` **ไม่ถูกเขียนเลย** (โลกใหม่ไม่ได้เซฟลง slot — ตรงข้อเท็จจริง "ไม่มี
  autosave ตอนออก") · drift สำเนา = `PlayerprefData.dat` อย่างเดียว (volatile ตามตาราง preflight A4)
- **rollback DLL:** `Mods\LongYinCoop.dll` = `6a94abb5…` · `.pdb` = `856d3bed…` (hash ตรง rollback
  point `.prev-6a94abb5-20261030` / `.pdb.prev-856d3bed-20261030`) — build `865b4a87` ยังอยู่บนดิสก์ที่
  `evidence/SP5T0-prep-20261030-010247/` (gitignored)
- **pref:** `Sp5T0` คืน `false` — `MelonPreferences.cfg` กลับ **`976ccfb9…` EXACT byte-level**
  (ML เขียนซ้ำตอนออกเกม 01:35:03 เป็น cosmetic — ค่า key ทุกตัวเหมือนเดิม; ตรวจด้วยค่า key ตามบทเรียน
  RUNBOOK-G3) · `AllowRemoteInput=false` ✓ · `net.cfg` `12541f34…` ไม่ถูกแตะ · hash เกม 2 ไฟล์ไม่เปลี่ยน
- **log/probe คงไว้:** `MelonLoader/Logs/26-10-30_1-31-40.log` + `UserData/LongYinCoop/SP5T0/20261030-013144/`
  (สำเนาใน evidence โฟลเดอร์นี้ครบ: ndjson/summary/ml-log/audit-output/round-analysis.json)

## 6) ค้าง / ขั้นถัดไปที่เสนอ

- สถานะ T0: **รอบนี้ใช้ได้** — ข้อมูลบัญชีครบ, E4/E5/E6/E7 ปิด, E1/E2 คง UNKNOWN ตามโครง rev 13
  (เปิดทางรอบถัดไป: กลไก re-check inventory ใน handler ที่มีอยู่เพื่อพิสูจน์ `inventoryStability`
  = **ต้องขออนุมัติแยกตามแผน**)
- แก้บั๊ก comparator + tool `sp5t0_audit.py` (อนุมัติแยก) · ตัดสิน GO/NO-GO ของ implement ลด NPC ตาม
  `SP5design-20261027` §6 (มติผู้ใช้) · commit เอกสาร/evidence รอบนี้ (ยังไม่ทำ — รอวลีสั่ง)
