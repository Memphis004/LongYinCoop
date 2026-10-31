# decisions-pending — มติที่รอผู้ใช้ทั้งหมด (รวมศูนย์)

> สร้างโดย docs session offline 2026-10-30 00:2x (untracked ตาม precedent `plans/closed/X5-metric-proposal.md`)
> · แต่ละหัวข้อ = สถานะ · ทางเลือก · ผลของแต่ละทาง (ระบุชัด ต้อง deploy/เปิดเกม/split หรือไม่) · ข้อเสนอของ agent + เหตุผล
> · ไฟล์นี้**ไม่ใช่การอนุมัติอะไร** — มติเกิดจากข้อความผู้ใช้เท่านั้น · เมื่อมีมติแล้วให้ append ผลมติ + วันที่ท้ายหัวข้อ ห้ามลบทางเลือกเดิม

## 1) V2-A ขั้นถัดไป — สถานะ: **มติ [ค] แล้ว 2026-10-29 · เหลือ (ก)/(ข) รอมติ**

บริบท: ชุด A ปิดแล้ว **PROVISIONAL** (9 runs · slope 1.2675 ผ่าน 1.25±0.1 · intercept 0.8056 UNRESOLVED_OFFSET · deviation §E.1: N=5 มี valid 1/2 — ขาดรอบซ้ำ A2 อีก 1 รอบสะอาด) · มติ [ค] = รับผลสืบสวน offline (`evidence/UNKNOWN-invest-20261029/` — commit `ee993d1` + ปรับถ้อยคำ `4f3dea3`) **ไม่รันเกมซ้ำ · ปิดการวัดชุด A rate ปกติ**

| ทาง | ต้องทำอะไร | ผลถ้าทำ | ผลถ้าไม่ทำ |
|---|---|---|---|
| (ก) รอบวัดซ้ำ N=5 บน build เดิม `983b2384` | **deploy ใหม่**: copy DLL/PDB `983b2384…`/`951de8fc…` จาก `evidence/V2A-20261029-020816/` → `Mods\` (สำรอง `6a94abb5` เป็น `.prev` ก่อน) · ตั้ง `TimeV2a=true` · **save-split** (`--expected-baseline coop-test/baselines/20261028-071340/…`) · **ผู้ใช้เปิดเกม** 1 run สะอาด (ไม่มี popup กลางงาน >2 s) | N=5 ครบ ≥2 valid ตามเกณฑ์ลดรูป §E.1 ⇒ ผลชุด A ยกจาก PROVISIONAL เป็นเต็ม | N=5 คง PROVISIONAL (มติรับทราบแล้ว 2026-10-29) — ไม่บล็อกงานอื่น |
| (ข) A4-r1 build ใหม่ (TestMode สัญญาณ 1) | **build + provenance ใหม่** + deploy + `TimeV2a=true` + split + เปิดเกม | เปิดเส้นตรวจ TestMode (ทดสอบขอบ slider Wait — ผูก D10/§E.3) · ต้นทุน/ความเสี่ยงสูงสุดของสามทาง | เส้น TestMode ค้างรอ |
| (ค) รับผลสืบสวน offline | ไม่ต้องทำอะไร | — | **✅ มติแล้ว 2026-10-29** |

**ข้อเสนอ agent:** (ก) เข้าคิวเป็น**เซสชันเกมแยกของตัวเอง**ภายหลัง — ไม่เร่ง เพราะ slope (ตัวหลักของโมเดล T(N)) ผ่านแล้ว สิ่งค้างคือ valid-count ของ N=5 เท่านั้น · **ห้ามรวมเซสชันเกมเดียวกับ SP5-T0/V1/S3** (กติกาคาดโทษ APPROVAL-REQUESTS) · (ข) ทำเฉพาะเมื่อจะแตะ TestMode จริง

## 2) SP5-T0 — สถานะ: **§4 ข้อ 1 = อนุมัติแผนแล้ว 2026-10-29 (แผน rev 13 — แก้ "rev 7"→"rev 13" แล้ว commit `8816748`) · ข้อ 2–8 ยังรอ (จะขออนุมัติรายข้อตอนสั่งรันด้วยวลีแยก)**

ข้อความ §4 เต็ม (จาก `plans/sp5-t0-plan.md` หลังแก้ rev 13 — verbatim):

1. **อนุมัติแผน T0 (rev 13 — ฉบับสุดท้ายของเอกสารนี้) แล้วจึงเขียนโค้ด probe** (ตามกติกา 1 — แผน stage ก่อนลงมือ) — **✅ มติ 2026-10-29: อนุมัติแผนเท่านั้น ห้ามเริ่มรัน/ติดตั้ง/split จนกว่าผู้ใช้สั่งวลีแยก**
2. build/deploy DLL ใหม่ที่มี probe (probe = โค้ดใหม่ใน mod) + ตั้ง `Sp5T0=true` ใน cfg **ก่อนเปิดเกม** (ลำดับ: **preflight §2.6 ขั้น A → [อนุมัติ split] → ขั้น B → เตรียม probe → ผู้ใช้เปิดเกม**) — **ต้อง build+deploy+split+เปิดเกม**
3. preflight §2.6 **ขั้น A (read-only)** ต้อง PASS ครบทุกข้อ (hash เกม / save status / UserData baseline / ตาราง volatile) และ **ขั้น B (save-split) ต้องได้รับอนุมัติแยกก่อนดำเนินการ** พร้อม hash ก่อน/หลังและขั้นตอนคืนสภาพที่บันทึกไว้ — ก่อนสั่งเปิดเกม — **ต้อง split**
4. ทดสอบบนสำเนาเซฟตามโปรโตคอล §1.2 (โลกใหม่ไม่แตะเซฟจริง — แต่เกมยังต้องเปิดบนสำเนา) — **ต้องเปิดเกม**
5. OPTIONAL hooks: เลือกเปิด H6/H7 (วัดรายวัน–รายเดือนภายหลัง), H8 (cross-check heroNum), H9/H10, H11 ตัวไหน — **ดีฟอลต์ของรอบแรก: ไม่ติดทั้งหมด** (CORE H1–H5 พอสำหรับสมการ E1–E7)
6. แผนสำรอง List.Add (§2.2) — อนุมัติเฉพาะเมื่อ residual ≠ 0 จริง
7. ค่า settle window / event cap / ตำแหน่งไฟล์ evidence ของรอบรัน
8. ถ้า typed read ล้มเหลวและต้องการใช้ raw offset แทน — **ต้องเสนอใหม่และรออนุมัติก่อน** (ตามขอบเขตบังคับของรอบนี้ + กติกา 8)

**ข้อเสนอ agent:** เมื่อสั่งรัน (วลีแยก) ใช้ดีฟอลต์ข้อ 5 = ไม่ติด OPTIONAL ทั้งหมด และข้อ 6–7 ตามค่า default ของแผน — ลดจำนวนมติที่ต้องตัดสินก่อนเริ่ม · หมายเหตุ: probe build อาจมีอยู่แล้วจาก S2a-4 (`Sp5T0` gate ใน build `6a94abb5` — ตรวจครบตาม §5 GO ก่อนเสมอ)

> **ผลมติ/งานเตรียม 2026-10-30 (session offline):** ตรวจ §5 ข้อ 2 ครบแล้ว — CORE H1–H5 + patch.audit 4 จุด + finalizer contract ครบใน build เดิม `6a94abb5`; gap เดียว = **harness test ของ linkage `iid`** → เขียน `Sp5T0LinkageHarness` (reflection บน artifact จริง, 17 checks) **ผ่าน 17/0** + ป้าย probe "UNPROVEN→harness-proven" (string-only, commit `ab9d07b`) · artifact ใหม่ READY: DLL `865b4a87…` / PDB `60ad4546…` @ `ab9d07b` (determinism ×2 · provenance `evidence/SP5T0-prep-20261030-010247/` @ `a998509` · tests 650 PASS/53 OK/106 OK/CLEAN) · **ยังไม่ deploy — `Mods\` ยังเป็น `6a94abb5`** · ข้อ 2–8 ยังรอมติรายข้อ (ตอนสั่งรัน) · หมายเหตุ: ตอน deploy ต้องเลือกด้วยว่า deploy build นี้ (มี TimeV2a + ป้ายใหม่) หรือ `6a94abb5` (probe ตรงกันแต่ป้ายเก่า) — จะระบุในคำขออนุมัติข้อ 2
>
> **ผลมติ/งานจริง 2026-10-30 (session เกม `SP5T0-run-20261030-012502` — มติ §4 ข้อ 2–8 แนบมากับวลี "รัน SP5-T0" ทั้งชุด):** ข้อ 2 ✅ ใช้แล้ว — เลือกทาง (ก) deploy `865b4a87`/`60ad4546` (สำรอง `6a94abb5` เป็น `.prev-6a94abb5-20261030` ก่อน, hash ยืนยัน) + ตั้ง `Sp5T0=true` ก่อนเปิดเกม · ข้อ 3 ✅ ใช้แล้ว — ขั้น A PASS ครบ 6 ข้อ + ขั้น B split `SP5T0-20261030-012502` ด้วย `--expected-baseline 20261028-071340` ตรงเป๊ะ 0/0/0 + hash ก่อน/หลัง + คืนสภาพบันทึกล่วงหน้า · ข้อ 4 ✅ ใช้แล้ว — เกมเปิดบนสำเนา โลกใหม่ **`SaveSlot*` ไม่ถูกเขียนเลย** restore EXACT ท้ายรอบ · **ข้อ 5 สถานะจริง: ไม่ติด OPTIONAL H6–H11 ตามมติ (CORE 7 hooks เท่านั้น — patch count ยืนยัน 7)** · **ข้อ 6 สถานะจริง: แผนสำรอง List.Add ไม่ถูกใช้ — residual ของสมการ E1/E2 = 0 ตามโครง (883==882+1, 87==87==87) จึงไม่มีเหตุถาม** · **ข้อ 7 สถานะจริง: ใช้ default ของแผนครบ** — settle จบด้วยการผู้ใช้ปิดเกม 24.5 s หลัง `sng.end` (อนุญาตตาม §2.5 ก) · event cap ใช้ 3,014/50,000 · evidence = `evidence/SP5T0-run-20261030-012502/` · **ข้อ 8 สถานะจริง: typed read สำเร็จทุกเส้น (readfail=0) ไม่มีการใช้ raw offset เลย** · ผลรอบ: ติด 7/ถอน ownedLeft=0 · events=3014 errors=0 · E4/E5/E6/E7 ปิด · E1/E2 คง UNKNOWN ตาม rev 13 · **บั๊กที่พบ 2 ตัว** (comparator string-format ของ probe → allMatch=false เป็น artifact; `tools/sp5t0_audit.py` คาด perForce เป็น dict แต่ log จริงเป็น list จึงล้ม) — **ยังไม่อนุมัติแก้ บันทึกค้างด้านล่าง (หัวข้อ 7)** · รายงาน `evidence/SP5T0-run-20261030-012502/round-report.md` (commit `bbea491`) · ค้างต่อจากรอบนี้ = ตัดสิน GO/NO-GO ลด NPC (หัวข้อ 7 ใหม่) + การแก้บั๊ก (อนุมัติแยก)

## 3) V1 (time-plan) — สถานะ: **D1/D2 มติแล้ว 2026-10-29 · D3–D10 ยังรอ**

- **D1 ✅ = ปุ่ม UI เท่านั้น** — ข้อจำกัดที่ต้องบันทึก (ตามข้อความในตาราง C.3 เอง): **ไม่ครอบเคส forced-cancel ผ่าน `MiddleStopWork`** — ผลต่อแผน: TC-3 (invoke) และ §D.1 ข้อ 6 ("การ invoke ทำได้เฉพาะเมื่อมติ D1 อนุญาต") จึง**ทดสอบไม่ได้ในรอบนี้** — ต้องบันทึกเป็น NOT-RUN/limitation ของ V1
- **D2 ✅ = ยอมรับหลักการ per-day credit ถาวร** (ผลรายวันที่ให้ไปแล้วอาจย้อนคืนไม่ได้ — B.4)
- ยังรอ: **D3–D8 (Gate 5/6)** — activity serialization/ownership, policy งานทับ, policy `noCancel`/forced-cancel, ค่า N นอก {1,5,10,15}, `workFinished`/`skipping` ค้าง, QuickTravel · **D10 (Gate 5c)** — ทดสอบขอบ slider Wait (อนุญาตเฉพาะหลังยืนยันค่า `TestMode`)
- **ข้อเสนอ agent:** ไม่ต้องตัดสิน D3–D8 ตอนนี้ — ทั้งหมดผูก Gate 5/6 ซึ่งมา**หลัง** V1 วัดเสร็จ ตัดสินเมื่อเตรียมแพ็กเกจ V1 จริง

## 4) push — สถานะ: **ทำแล้ว 2026-10-30 ตามมติ (scope เฉพาะ 8 commits)**

> **ผลมติ 2026-10-30 (รอบถัดมา — session ปัจจุบัน):** **mod push รอบที่ 3 = สำเร็จ** — มติแก้เงื่อนไขหยุด
> เป็น "origin/master ≠ `a998509` = หยุด" · fetch ตรวจ origin/master = `a998509` ตรงเงื่อนไข → push
> `a998509..f088736` fast-forward = 4 commits (`c816815`, `bbea491`, `8557f91`, `f088736`) · ห้าม force
> ยืนยัน · **game push = สำเร็จ** — มติ `[อนุมัติ] 18 commits` (FF, docs-only) · fetch ตรวจ origin/main =
> `531fe42` ตรงเงื่อนไข → push FF 18 commits (docs AGENTS.md+STAGEPLAN.md) → ผลจริงบันทึกใน HANDOFF
> `evidence/AUTO-20261030-<ts>/HANDOFF.md` · ค้างหลัง push: **ไม่มี commit ค้างในสายหลัก** (untracked
> คงเดิม: decisions-pending.md, HANDOFF ×4, coop-test label)

- มติ: push เฉพาะ `c07ae7c..c826961` · ตรวจ `git fetch` ก่อน (origin/master = `d81c86d` ตรงก่อน push) · ห้าม force — **ผลจริง: `d81c86d..c826961` fast-forward สำเร็จ → GitHub Memphis004/LongYinCoop (private)**
- commits ใหม่ของ session นี้ — mod repo `afb70e2`/`7d8f98f`/`8816748` + game repo `944ceda` — **ยัง local** · push รอบหน้าเมื่ออนุมัติ (ตรวจ fetch ก่อนเช่นเดิม)

> **ผลมติ 2026-10-30:** mod push รอบใหม่ **สำเร็จ** — `c826961..8816748` fast-forward (3 commits ตามมติ; fetch ก่อน: origin/master = `c826961` ตรง snapshot เป๊ะ) → **origin/master ปัจจุบัน = `8816748`** · **game push = ค้างรอมติ**: fetch แล้วพบ `origin/main = 531fe42` ("docs: record the W2/W4 restore") ขณะ local main นำอยู่ **18 commits** (docs AGENTS.md+STAGEPLAN.md ทั้งหมด 10-21→10-30 · ไม่มี divergence — FF ได้) ซึ่ง**ขัดมติที่เขียนว่า "game 1 commit (944ceda)"** ⇒ หยุดรายงานตามกฎ "origin ไม่ตรง snapshot" — ทางเลือก: (ก) push 18 commits ทั้งชุด (ข) push เฉพาะช่วงล่าสุดจนถึง `944ceda` ไม่ได้เพราะ FF ครอบทั้งช่วง (ค) ไม่ push และเก็บ local ต่อ — รอผู้ใช้เลือก
>
> **ผลมติ 2026-10-30 (รอบถัดมา — ข้อความมติ push):** mod push รอบที่ 2 **สำเร็จ** — fetch ตรวจ `origin/master = 8816748` ตรงเงื่อนไขมติ → push `8816748..a998509` fast-forward = 5 commits (`0ef34e6`, `f595c02`, `2f280ae`, `ab9d07b`, `a998509`) → **origin/master ปัจจุบัน = `a998509`** (synced) · **game push ยังค้าง** — ข้อความมติมาในรูป **`[อนุมัติ / ไม่อนุมัติ]` = ช่องเลือกยังไม่ได้เลือก** (ไม่ใช่คำตอบ) ⇒ ไม่ push ตามวินัย "ห้ามเดาแทนผู้ใช้" · fetch ตรวจแล้ว: `origin/main = 531fe42` ตรง snapshot, ห่าง 18 (FF) คงเดิม · commit ใหม่จากรอบนี้ `c816815` (popup record) **ยัง local**
>
> **ผลมติ 2026-10-30 (รอบถัดมาอีก — session offline หลังปิดรอบ SP5-T0):** **mod push = ไม่ทำ — เงื่อนไขหยุดกระตุ้นตามตัวอักษร:** มติรอบนี้เขียน "ถ้า origin/master ≠ `8816748` = หยุดรายงาน" แต่ fetch แล้ว `origin/master = a998509` (ตามที่ push รอบที่ 2 ข้างบนทำไว้ — ไม่มี divergence, FF ได้, `8816748` ยังเป็น ancestor ในสายโซ่) ⇒ หยุดตามเงื่อนไข ไม่ตีความแทนผู้ใช้ — **ต้องการทราบว่าเงื่อนไขที่ตั้งใจคือ `a998509` (ค่าจริงหลัง push รอบ 2) หรือยังยึด `8816748` เพื่อให้ push รอบ 3 เดินได้** · local ปัจจุบันนำ origin **3 commits**: `c816815` (popup record — เก่า) + `bbea491` (evidence SP5-T0 run) + `8557f91` (VERSION.md deploy record) · **game push = ค้างเช่นเดิม** — มติรอบนี้มาในรูป **`[อนุมัติ / ไม่อนุมัติ] 18 commits`** = ช่องเลือกยังไม่ได้เลือก (ไม่ใช่คำตอบ) ⇒ ไม่ push · fetch ตรวจ: `origin/main = 531fe42`, local main นำ 18 (FF, docs-only) คงเดิม
>
> **ผลมติ 2026-10-30 (รอบถัดมา — session ก่อนรอบ N1):** mod push รอบที่ 3 **สำเร็จ** — มติแก้เงื่อนไขเป็น "origin/master ≠ `a998509` = หยุด" · fetch ตรวจ origin/master = `a998509` ตรง → push `a998509..f088736` FF = 4 commits (`c816815`, `bbea491`, `8557f91`, `f088736`) → origin/master = `f088736` · game push **สำเร็จ** (มติ [อนุมัติ] 18 commits) — `531fe42..944ceda` FF → origin/main = `944ceda` = HEAD · ไม่ force ทั้งคู่
>
> **ผลมติ 2026-10-30 (รอบ N1 — ปัจจุบัน):** push มาในรูป **`[ไม่ push รอบนี้ / อนุมัติ push เฉพาะ commit ของ session นี้…]`** = ช่องเลือกยังไม่ได้เลือก (ไม่ใช่คำตอบ ตาม fallback rule) ⇒ **ไม่ push รอบนี้** — commits ใหม่ของ session N1 (code + evidence + แผน) คง local รอมติ push รอบถัดไป (fetch ก่อน ห้าม force เช่นเดิม)
>
> **ผลมติ 2026-10-30 (รอบถัดมา — session N2 ปัจจุบัน):** **mod push รอบที่ 4 สำเร็จ** — มติ "[อนุมัติ]
> fetch ก่อน ถ้า origin/master ≠ `f088736` = หยุด" · fetch ตรวจ origin/master = `f088736` ตรงเงื่อนไข →
> push `f088736..3493bfb` FF = 2 commits (`44b7529` N1 code, `3493bfb` N1 evidence+plan) → origin/master
> = `3493bfb` · **game push = ไม่มีอะไรค้าง** (มติ "[ไม่มีอะไรค้าง / ตามสถานะจริง]" — ตรวจจริง:
> origin/main = `944ceda` = HEAD ทุก byte ตั้งแต่ push รอบก่อน) · commits ใหม่ของ session N2
> (โค้ด + evidence/แผน) คง local รอมติ push รอบถัดไป
>
> **ผลมติ 2026-10-30 (รอบถัดมา — session push ปัจจุบัน `AUTO-20261030-034239`):** **mod push รอบที่ 5 =
> ไม่ทำ — เงื่อนไขหยุดกระตุ้น 2 ทางพร้อมกัน:** (1) มติกำหนด "fetch ก่อน ถ้า origin/master ≠ `3493bfb` =
> หยุดรายงาน" — fetch แล้ว **origin/master = `7396b94`** (2) พบ push ที่**ไม่มีบันทึกใน §4**:
> reflog `refs/remotes/origin/master` = `7396b94 … update by push @ 2026-10-30 03:29:26 +0700`
> ขณะ commit `7396b94` สร้าง 03:29:01 — push จากเครื่องนี้ หลัง push รอบที่ 4 (`3493bfb` @ 03:05:25)
> และก่อน session ปัจจุบันเริ่ม (~03:42) · **ข้อเท็จจริง:** commits ที่มติสั่ง push (`9d0ff02`, `d8036f7`,
> `7396b94`) **อยู่บน origin ครบแล้ว** — origin/master = local master = `7396b94`
> (`origin/master..master` = ว่าง) ⇒ ถ้า push ก็เป็น no-op · **ไม่ push ไม่ force ไม่ตีความแทนผู้ใช้ —
> รอผู้ใช้ยืนยันที่มาของ push @ 03:29:26** (session คู่ขนาน หรือผู้ใช้เอง) · **game push = ไม่มีอะไรค้าง
> ยืนยันแล้ว** (fetch แล้ว origin/main = `944ceda` = HEAD ทุก byte) · session-start checks ผ่านครบ:
> เกม not running · hash เกม 2 ไฟล์ตรง VERSION.md (`33c56def…` / `451311d4…`) · Mods DLL `6a94abb5…`
> + PDB `856d3bed…` · cfg `976ccfb9…` (`Sp5T0 = false` บรรทัด 15) · net.cfg `12541f34…` (ไม่แตะ) ·
> Save 57 ไฟล์ `df8e5a11beed` + `leftovers: none` · หลักฐาน `evidence/AUTO-20261030-034239/HANDOFF.md`
>
> **ผลมติ 2026-10-30 (รอบถัดมา — session ปิด stage N3 ปัจจุบัน):** **mod push = ไม่ทำ — เงื่อนไขหยุด
> กระตุ้นตามตัวอักษรอีกครั้ง:** มติเขียน "fetch ก่อน ถ้า origin/master ≠ `3493bfb` = หยุดรายงาน" —
> fetch แล้ว **origin/master = `7396b94`** (ค่าเดียวกับที่ HANDOFF `034239` พบและรายงานไว้: push @
> 03:29:26 ที่ยังรอผู้ใช้ยืนยันที่มา — `3493bfb` เป็น ancestor ของ `7396b94` ไม่มี divergence,
> FF ได้) ⇒ **ไม่ push ไม่ force ไม่ตีความแทนผู้ใช้** — commits ใหม่ของ session นี้ (`bda5200`
> evidence N3, `3131df8` แผน+คำขอ N4) และ tag `stage-N3` **คง local รอมติ push รอบถัดไป**
> (ระวังเงื่อนไข hash เก่ากระตุ้น no-op — ครั้งที่ 3 แล้วตามบันทึก §4) · **push tag = bracket
> `[อนุมัติ / ไม่อนุมัติ]` ยังไม่ได้เลือก = ไม่ใช่คำตอบ ⇒ ไม่ push tag** (และ moot เมื่อ push หยุด) ·
> **game push = ไม่มีอะไรค้าง** (origin/main = `944ceda` = HEAD)
>
> **สถานะ 2026-10-30 04:38 (แก้ไขข้อเท็จจริงข้างบน — append-only):** **พบ push ที่ไม่ได้มาจาก
> session ปิด stage N3 ระหว่างที่ session กำลังเขียนบันทึก** — reflog `refs/remotes/origin/master` =
> `3131df8 … update by push @ 2026-10-30 04:36:03 +0700` (session นี้ fetch และ**หยุด push** ตาม
> เงื่อนไขมติ @ 04:32 แล้ว ไม่มีคำสั่ง push ใดจาก session) · ผลตรวจ `git ls-remote`: **origin/master =
> `3131df8` = local master** ⇒ commits `bda5200` + `3131df8` **ขึ้น origin แล้วโดย actor อื่น** (push
> จากเครื่องนี้ — ตัวตนพิสูจน์ไม่ได้จาก reflog เช่นเดียวกับ push @ 03:29:26) · ไม่มี divergence
> (`7396b94` เป็น ancestor) · **tag `stage-N3` ไม่อยู่บน origin** (ls-remote ไม่มี refs/tags) ⇒ push
> tag คงตามมติที่ยังไม่ได้เลือก · session ไม่ push/force/แตะอะไรเพิ่ม — บันทึกและรายงานเท่านั้น ·
> **push ที่ไม่มีบันทึกใน §4 = ครั้งที่ 2 ของวันนี้** (03:29:26, 04:36:03) — รอผู้ใช้ยืนยันที่มาทั้งคู่ ·
> รายละเอียด: `evidence/AUTO-20261030-043638/HANDOFF.md` §ADDENDUM
>
> **ผลมติ 2026-10-30 (รอบถัดมา — session N4-prep ปัจจุบัน 04:43–):** (1) **push mod = ไม่ทำ** — bracket
> `[อนุมัติ/ไม่อนุมัติ]` **ยังไม่ได้เลือก** (ไม่ใช่คำตอบ) — เงื่อนไขที่แนบ ("fetch ก่อน ถ้า origin/master ≠
> `3131df8` = หยุด") จึงไม่ถูกใช้ · (2) **push tag `stage-N3` = ไม่ทำ** — bracket `[อนุมัติ/ไม่อนุมัติ]`
> (เลือกอย่างใดอย่างหนึ่ง) **ยังไม่ได้เลือก** ⇒ tag คง local (@ `dbca3b0` แล้วหลัง commit งาน T2) ·
> (3) **ที่มา push 03:29:26/04:36:03 = bracket ยังไม่ได้เลือก** — session ทำเฉพาะการสืบ read-only ที่ระบุ
> ฝังใน bracket (ผล: ไม่มี hook/alias/config ที่ push ได้เอง · ไม่มี scheduled task git · ไม่มี git.exe
> ตอน snapshot · มี process MCP/agent infrastructure รันค้าง 5× il2cpp-frida-mcp + mcp_server.py +
> freebuff node + powershell watcher = session คู่ขนานเป็นไปได้สูง · ทั้งสอง push มาจาก clone นี้แน่นอน
> ตาม reflog — ตัวตนพิสูจน์ไม่ได้) · **(4) พบ commit จาก session คู่ขนานอีกต่อเนื่อง: `5edb41a`
> @ 04:46:22** (ย้าย sp5-t0-plan.md + sp5-npc-n3-approval-request.md → plans/closed/ + INDEX.md —
> ไม่ push, ไม่แตะไฟล์ของ session นี้) ⇒ **กระตุ้นเงื่อนไขหยุด "เจอการแก้จาก session อื่น"** — session
> commit งาน T2 ที่เสร็จ+ตรวจแล้วก่อนพบเป็น `dbca3b0` (กันโดนดูด) แล้ว**หยุด git state change ที่เหลือ**:
> N4-request update คง uncommitted (M), ไม่ push อะไรทั้งสิ้น — รอผู้ใช้ตัดสิน
>
> **ผลมติ 2026-10-30 (รอบถัดมา — session ปัจจุบัน 05:04–):** (1) **push mod = ไม่ทำ** — bracket
> `[อนุมัติ/ไม่อนุมัติ]` **ยังไม่ได้เลือก** (ครั้งที่ 4 ของหัวข้อ push) — เงื่อนไขที่แนบ ("fetch ก่อน
> origin/master ≠ `3131df8` = หยุด · push ได้เฉพาะ `5edb41a`, `dbca3b0` [ตัดออกถ้าไม่ยอมรับ `5edb41a`]")
> จึงไม่ถูกใช้ · (2) **push tag `stage-N3` = ไม่ทำ** — bracket ยังไม่ได้เลือก (ครั้งที่ 3) · (3) **ข้อเท็จจริง
> 2 ข้อ (ที่มา `5edb41a` / ที่มา push 03:29:26+04:36:03) = bracket ยังว่างทั้งคู่** ⇒ สถานะคง "รอผู้ใช้
> ยืนยัน" — ผลการสืบ read-only รอบก่อน (hooks/config/alias/process) คงอยู่ใน HANDOFF
> `AUTO-20261030-050126` · (4) **commit คำขอ N4 = [อนุมัติ] ทำแล้ว** — `git add plans/sp5-npc-n4-approval-request.md`
> (add ราย path เท่านั้น ตามมติ) → commit **`b58d553`** (1 ไฟล์ 19 insertions, append-only) ·
> **หมายเหตุสำคัญต่อ push รอบหน้า:** local ปัจจุบันนำ origin/master (`3131df8`) **3 commits** —
> `5edb41a`, `dbca3b0`, `b58d553` — โดย `b58d553` เกิด**หลัง**รายการ push ที่มติระบุ ("เฉพาะ
> `5edb41a`, `dbca3b0`") ⇒ มติ push รอบหน้าต้องระบุชุด commit ให้ครบ/ชัดว่ารวม `b58d553` หรือไม่ ·
> (5) popup ครั้งที่ 12 (§6 + testimony) — ยังไม่มีคำตอบจริงจากผู้ใช้
>
> **ผลมติ 2026-10-30 05:09 (รอบถัดมา — session ปัจจุบัน · bracket กรอกครบทุกข้อ):**
> (1) **`5edb41a` = ยอมรับ** ("เป็นงานจัดเอกสารที่ฉันรู้เรื่อง") — **ปิดคำถามที่มาของ commit นี้** ·
> (2) **push เก่า 03:29:26/04:36:03 = ไม่ทราบ** — ผู้ใช้สั่งสืบ process/watcher read-only; **ผล:**
> ไม่มี hook/alias/config/scheduled task ที่ push ได้เอง (ยืนยันซ้ำ) · ZCode app รันอยู่พร้อม
> plugin-host infrastructure (presentations/pdf/documents/spreadsheets/node-repl) ⇒ **chat session
> คู่ขนานในแอปเดียวกันเป็นกลไกที่เป็นไปได้ที่สุด** (ทุก session รัน Bash → git ได้) · watcher เก่า
> (`watch-parent.ps1`/`parent-watch.ndjson`) ไม่มีอยู่แล้ว · bash history มี `git push -u origin main`
> เก่า ๆ ไม่มี timestamp เทียบไม่ได้ · **สรุป: ระบุตัวตนย้อนหลังไม่ได้จากหลักฐานที่มี — แต่ไม่พบ
> กลไกอัตโนมัติใด ๆ; ทางเลือกถ้าจะปิดชั้นเดียว = สืบ transcript ของ session คู่ขนาน (ต้องมติ —
> อยู่นอกขอบเขต "process/watcher")** · (3) **push mod = อนุมัติ ทำแล้วสำเร็จ** — fetch ตรวจ origin/master
> = `3131df8` ตรงเงื่อนไข → push `3131df8..b58d553` FF **3 commits (`5edb41a`, `dbca3b0`, `b58d553`)
> ตรงชุดที่มติระบุเป๊ะ** (rev-list count = 3) · ไม่ force · **origin/master ปัจจุบัน = `b58d553` = local
> HEAD** · (4) **push tag `stage-N3` = อนุมัติ ทำแล้วสำเร็จ** — `[new tag]` บน origin · ls-remote:
> `refs/tags/stage-N3` = `e66630a` (tag object) → `^{}` = `3131df8` ตรงจุดปิด stage · (5) **popup =
> USER TESTIMONY จริงครั้งแรก: "ไม่แน่ใจ ทั้ง 3 ข้อ"** — บันทึก testimony ครั้งที่ 13 · freeze gate
> ยังสรุปไม่ได้จากคำตอบนี้ (คงทางสืบ runtime อนุมัติแยก)

> **ผลมติ 2026-10-31 (รอบ 7 — session offline · ส่วน A: secret + push):** **mod push สำเร็จ** —
> แต่ว่า**สถานะก่อนรอบนี้ไม่ตรงกับที่รายงานรอบ 6 ไว้**: รายงานรอบ 6 เขียนว่า `d81c86d`
> "ยังไม่เคยถูก push" + "34 commits ค้าง push" แต่ **A1 วัดจริง: `d81c86d` อยู่บน `origin/master`
> แล้ว** (`git branch -r --contains d81c86d` = origin/master · `merge-base --is-ancestor` exit 0 ·
> `origin/master = 409ac9b` = HEAD · `rev-list --left-right --count origin/master...HEAD` = `0 0` ·
> reflog origin/master 5 รายการบนสุด = `update by push` ไล่ถึง `409ac9b` = commit ปิดรอบ 6) ⇒
> **การ push รอบ 6 เกิดขึ้นจริงหลังรายงาน** (ผู้สั่ง/ผู้ทำ = UNKNOWN ไม่มีบันทึกใน AGENTS.md) ·
> **ผลต่อ secret: ถือว่า burned — ไม่ rewrite history ไม่ force ตามมติ `[FORCE-PUSH] = ไม่อนุมัติ`** ·
> **การ redact:** `evidence/S2a4-GO3-20261028-074003/net.cfg.during-GO3` (เพิ่มโดย `d81c86d`)
> ค่า secret → `<redacted>` commit `805902a` (467 B → 413 B, 1 บรรทัด) · สำเนาต้นฉบับเก็บ
> `coop-test/quarantine-secrets/SECRET-20261031-121438/` (untracked) sha256
> `5ce166da…` ตรงกันก่อน/หลังย้าย · **push `409ac9b..b70cfd7` FF 3 commits** (`1165966` tools,
> `805902a` redact, `b70cfd7` incident note + INDEX) — fetch ตรวจ origin/master ไม่ขยับระหว่าง
> ตรวจกับ push (`409ac9b6d9…` ทั้งสองครั้ง) · range ไม่มี binary/DLL/PDB/dump.cs/เซฟ ·
> `tools/scan_secrets.py --range` = 0 findings · ไม่ force · **origin/master หลัง push = `b70cfd7`
> = HEAD (0/0)** · ยืนยัน blob บน origin = `secret = <redacted>` ·
> **ข้อกำหนดต่อไป: ห้ามใช้ค่า secret เดิมซ้ำ — สุ่มใหม่ทุกครั้งก่อนเปิดเครือข่าย/ก่อน S3**
> (incident note `evidence/SECRET-20261031-121438/incident-note.md` · เครื่องมือ
> `tools/scan_secrets.py` + 27 tests) · **game repo = ไม่ push** ตาม `[PUSH-GAME-REPO] = ไม่อนุมัติ`
> · commit บันทึกนี้ (`decisions-pending.md`) = **local ไม่ push** ตามคำสั่ง

## 5) ของค้างที่พบใหม่รอบนี้ (จาก session ก่อน — ยังไม่มีมติ agent ไม่แตะ)

| รายการ | สถานะ | ทางเลือก · ข้อเสนอ |
|---|---|---|
| `context.txt` (mod repo) — worktree = **ไฟล์ว่าง** (unstaged) | เนื้อหาเดิม 888 บรรทัด (master prompt ยุคแรก) ยังอยู่ใน git HEAD ครบ | (ก) กู้คืนด้วย `git checkout -- context.txt` (ข) รับไฟล์ว่าง → commit (ค) ลบไฟล์ — **ข้อเสนอ: (ก) กู้คืน เพราะถูกตัดโดยไม่มีมติ** |
| untracked `notes/battle-notes.md`, `notes/worldmap-notes.md` | ไฟล์ที่ reorg session เตรียม (INDEX.md delta unstaged อ้างถึง — เนื้อหาคู่กับ battle-notes.md/worldmap-notes.md ที่ถูกลบจาก game repo) | (ก) commit พร้อม INDEX.md delta (ข) ทิ้ง — **ข้อเสนอ: (ก) ถ้าจะคง reorg ให้ครบ** |
| untracked `scan_pdb_secrets.py`, `scan_prefbinary_secrets.py` | สคริปต์สแกนจากการตรวจ binary ของ `d81c86d` (**ผล**บันทึกแล้วใน `c826961` แต่ตัวสคริปต์ไม่ถูก commit) | (ก) commit เป็นหลักฐานเสริม (ข) ทิ้ง — **ข้อเสนอ: (ก)** |
| untracked `evidence/AUTO-20261029-054954/` | HANDOFF ของ session ก่อน — untracked **ตาม convention** | คงเดิม |
| blobs binary 3 ไฟล์ของ `d81c86d` ยังอยู่ใน pushed history | มติ ทาง ค บางส่วนแล้ว (`c826961`: untrack + gitignore + ไม่ rewrite · repo private · สแกนแล้วไม่พบ secret) | (ก) ปล่อยไว้ (ข) rewrite history + force-push — **ข้อเสนอ: (ก) — force-push ห้ามทำโดยไม่มีมติชัดเจน** |

> **ผลมติ 2026-10-30 (ทำแล้วตามมติของผู้ใช้ในข้อความสั่งงาน):** `context.txt` = **กู้คืนแล้ว (ทาง ก)** — worktree ตรง HEAD ทุก byte (887 บรรทัด + บรรทัดสุดท้ายไม่มี newline) · `notes/battle-notes.md` + `notes/worldmap-notes.md` + INDEX.md delta = **commit แล้ว (ทาง ก) = `0ef34e6`** (notes byte-identical กับ game repo HEAD — ปิด reorg) · `scan_pdb_secrets.py` + `scan_prefbinary_secrets.py` = **commit แล้ว (ทาง ก) = `f595c02`** (หลักฐานเสริม) · blobs `d81c86d` = **ปล่อยไว้ (ทาง ก)** ไม่ rewrite/ไม่ force-push · commits ใหม่รอบนี้ (`0ef34e6`, `f595c02`, `2f280ae`, `ab9d07b`, `a998509`) **ยัง local — ไม่ได้อนุมัติ push**

## 6) ค้างเดิม (ยังไม่มีมติ)

- `S2-plan.md §13` rev2 (ตอบคำถาม §13 ของแผน S2 — ตอนนี้อยู่ `plans/S2-plan.md`)
- VPN (ZeroTier/Tailscale) · เครื่อง guest · ศึก 60 ยูนิต (Q9 / STAGEPLAN §11.1)
- E4 วัดซ้ำความละเอียดสูง (<0.25 ms — อนุมัติแยก)
- **คำตอบ popup 3 ข้อของ V2-A** (`evidence/V2A-20261029-020816/user-popup-answers-20261029.md` — USER TESTIMONY, append ได้ท้ายไฟล์): ชนิดงานรายรัน · popup ปิดเอง/หายเองกี่วินาที · รัน valid 5 รอบมี popup หรือไม่ — **ตอบแล้วช่วยปิดเงื่อนไข freeze gate ได้ทันทีโดยไม่ต้องเปิดเกม**

> **สถานะ 2026-10-30:** ข้อความสั่งงานรอบใหม่ส่งคำถาม 3 ข้อนี้มา**ช่องคำตอบยังว่าง `[...]` อีกครั้ง** — บันทึก append-only แล้วท้ายไฟล์ testimony (ป้าย USER TESTIMONY, ไม่เติมแทน) ⇒ ทั้ง 3 ข้อ**ยังไม่มีคำตอบจากผู้ใช้** · freeze gate ยังเปิดได้ด้วยคำตอบข้อความเท่านั้น (หรือสืบสวน runtime อนุมัติแยก)
>
> **สถานะ 2026-10-30 (รอบถัดมา):** ข้อความมติ push ส่ง popup มาในรูป **`[ยังไม่ตอบ / ตอบ: ...]`** = ไม่มีเนื้อคำตอบ (ครั้งที่ 3 ที่มาแบบไม่กรอก) — บันทึก append-only ท้ายไฟล์ testimony แล้ว (commit `c816815`) ⇒ คำตอบทั้ง 3 ข้อ**ยังคงสถานะยังไม่ตอบ**
>
> **สถานะ 2026-10-30 (ครั้งที่ 4):** ข้อความมติรอบ SP5-T0 offline ส่ง popup มาในรูป **`[ยังไม่ตอบ / ตอบ: ...]`** อีกครั้ง = ไม่มีเนื้อคำตอบ — บันทึก append-only ท้ายไฟล์ testimony แล้ว (ไม่ commit — ไม่อยู่ในมติ commit รอบนี้) ⇒ คำตอบทั้ง 3 ข้อ**ยังคงสถานะยังไม่ตอบ** (freeze gate ยังปิดด้วยคำตอบข้อความได้ทันทีเมื่อมา)
>
> **สถานะ 2026-10-30 (ครั้งที่ 5 — เติมคำตอบแล้วตามคำสั่ง):** มติรอบปัจจุบันมาในรูป `1) [...] 2) [...] 3) [...]` + คำสั่งตรงตัว **"ถ้าไม่แน่ใจ ให้เขียนคำว่า 'ไม่แน่ใจ' ตรง ๆ"** ⇒ agent เติมคำตอบท้ายไฟล์ testimony แล้ว: ตอบจากหลักฐาน runtime เท่านั้น (Q3: 5 valid runs **ไม่มี gap>2s เลย** — ยืนยันจากข้อมูล; Q1: เห็น popup "ได้ไม้" + ไม่ระบุชื่อ ใน 2 รอบที่ถูก exclude, บัญชีเต็ม = ไม่แน่ใจ; Q2: ไม่แน่ใจ — ไม่เคยวัดอายุ popup) · **ป้ายชัด: เป็นคำตอบ agent จากหลักฐาน ไม่ใช่ USER TESTIMONY** — ส่วนที่ log ครอบไม่ได้ (ชนิดงานเต็ม/อายุ popup) ยังเปิดให้ผู้ใช้ append คำตอบจริงได้ทุกเมื่อ · (ไฟล์ testimony ยัง uncommitted — ไม่อยู่ในมติ commit)
>
> **สถานะ 2026-10-30 (ครั้งที่ 6):** มติรอบ N1 ส่ง popup มาในรูป **`[ไม่มีคำตอบเพิ่ม / ตอบ: ...]`** = ไม่มีเนื้อคำตอบ — บันทึก append-only ตาม protocol ห้ามเติมแทน ⇒ สถานะคำตอบ 3 ข้อ = คงตามครั้งที่ 5 (agent-evidence + ไม่แน่ใจ) โดย**ยังไม่มี USER TESTIMONY จริงจากผู้ใช้**
>
> **สถานะ 2026-10-30 (ครั้งที่ 7):** มติรอบ N2 ส่ง popup มาในรูป **`[ไม่มีคำตอบเพิ่ม / ตอบ: ...]`** อีกครั้ง = ไม่มีเนื้อคำตอบ — บันทึก append-only (ไฟล์ testimony) ⇒ สถานะคงตามครั้งที่ 5 · USER TESTIMONY ยังไม่มี
>
> **สถานะ 2026-10-30 (ครั้งที่ 9 — ต่อจากครั้งที่ 8 ที่บันทึกเฉพาะในไฟล์ testimony):** มติรอบ push
> ปัจจุบัน (`AUTO-20261030-034239`) ส่ง popup มาในรูป **`1) [...] 2) [...] 3) [...]`** พร้อมคำสั่งเดิม
> "ไม่แน่ใจให้เขียนว่า 'ไม่แน่ใจ'" — คำตอบคงตาม append ครั้งที่ 5 ของไฟล์ testimony ทุกข้อ
> (agent-evidence + "ไม่แน่ใจ" ทุกจุดที่ไม่มีหลักฐาน; Q3 = 5 valid runs ไม่มี gap>2s เลย ยืนยันจากข้อมูล)
> — ไม่มีข้อมูลใหม่ จึงไม่เขียนซ้ำ (บันทึกครั้งที่ 9 ในไฟล์ testimony แล้ว) · USER TESTIMONY ยังไม่มี
>
> **สถานะ 2026-10-30 (ครั้งที่ 10 — session ปิด stage N3 ปัจจุบัน):** มติรอบนี้ส่ง popup มาในรูป
> **`1) [...] 2) [...] 3) [...]`** + คำสั่งเดิม "ไม่แน่ใจให้เขียนว่า 'ไม่แน่ใจ'" — คำตอบคงตาม append
> ครั้งที่ 5 ของไฟล์ testimony ทุกข้อ (agent-evidence + "ไม่แน่ใจ" ทุกจุดที่ไม่มีหลักฐาน; Q3 = 5 valid
> runs ไม่มี gap>2s เลย ยืนยันจากข้อมูล) — บันทึกครั้งที่ 10 ในไฟล์ testimony แล้ว · USER TESTIMONY
> ยังไม่มี
>
> **สถานะ 2026-10-30 (ครั้งที่ 11 — session N4-prep):** มาแบบ `1) [...] 2) [...] 3) [...]` + คำสั่งเดิม —
> บันทึกในไฟล์ testimony แล้ว · คำตอบคงตามครั้งที่ 5 · USER TESTIMONY ยังไม่มี
>
> **สถานะ 2026-10-30 (ครั้งที่ 12 — session ปัจจุบัน):** มาแบบ `1) [...] 2) [...] 3) [...]` + คำสั่งเดิม
> "ไม่แน่ใจให้เขียนว่า 'ไม่แน่ใจ'" — บันทึกในไฟล์ testimony แล้ว · คำตอบคงตามครั้งที่ 5 (agent-evidence
> + "ไม่แน่ใจ" ทุกจุดที่ไม่มีหลักฐาน) · USER TESTIMONY ยังไม่มี

## 7) ใหม่ 2026-10-30 — GO/NO-GO ลด NPC (SP5design §6) + บั๊กของรอบ SP5-T0 — สถานะ: **รอมติทั้งหมด**

บริบท: รอบรันจริงแรกของ SP5-T0 ปิดแล้ว (`evidence/SP5T0-run-20261030-012502/round-report.md`, commit `bbea491`) — ข้อมูลบัญชีครบ: 969 factory calls (C เท่านั้น; A/B=0) = 882 W2 (`genhero`) + 87 W3 (`genenemy`) · census 883 Heros = 1 protag + 170 spe + 562 sect + 150 wanderer · TempHeros 87 · E4/E5/E6/E7 ปิด · E1/E2 คง UNKNOWN ตาม rev 13 (โครงตัวเลขสอดคล้องเป๊ะ แต่ `inventoryStability` ภายใน window ไม่มีกลไกพิสูจน์)

### 7.1 GO/NO-GO ลด NPC (implement ตาม `SP5design-20261027` §6)

| ทาง | ต้องทำอะไร | ผลถ้าทำ | ผลถ้าไม่ทำ |
|---|---|---|---|
| (ก) GO — อนุมัติเข้าแผน implement ลด NPC | ร่างแผน implement ใหม่ (ราย stage + จุด hook + เกณฑ์วัด) จากฐาน SP5design §6 + ข้อมูลจริงรอบนี้ → **รออนุมัติแผนก่อนเขียนโค้ด** (กติกา 1) — ยังไม่ deploy/เปิดเกมในขั้นนี้ | ได้แผนชัดรออนุมัติ — ต้นทางของ T5/T6/T8/T9 ตาม design | สถานะคงเป็น "ข้อมูลพร้อม รอตัดสิน" |
| (ข) NO-GO — คงสำรวจต่อ | ไม่ทำอะไร | — | ข้อมูล T0 คงอยู่ ใช้ตัดสินได้ทุกเมื่อ |
| (ค) รอบ T0 เพิ่มก่อนตัดสิน (เช่น re-check `inventoryStability` ใน handler = ปิด E1/E2) | เขียนกลไก re-check + harness เพิ่ม → อนุมัติแยก → รอบเกมแยก | E1/E2 อาจปิดจาก UNKNOWN เป็นผ่าน/ไม่ผ่านจริง — ฐานตัดสินแน่นกว่า | E1/E2 คง UNKNOWN (โครงตัวเลขยังสนับสนุนอยู่) |

**ข้อเสนอ agent:** (ก) เริ่มร่างแผน implement (offline, รออนุมัติก่อนเขียนโค้ด) โดยระบุในแผนว่า E1/E2 ยัง UNKNOWN — และพิจารณา (ค) ควบคู่เป็นรอบเกมแยกถ้าอยากปิด E1/E2 ก่อน implement · **ห้ามรวมเซสชันเกมกับ V1/S3** (กติกาคาดโทษ)

> **ผลมติ 2026-10-30 (รอบถัดมา):** **GO ทาง (ก) = ร่างแผน implement แบบ offline แล้ว** — แผน
> `plans/sp5-npc-reduce-plan-draft.md` (label `SP5-NPC-plan-draft-20261030`, commit `f088736`)
> ครบตามเงื่อนไขมติ: ขอบเขต §0 · **ประกาศ E1/E2 = UNKNOWN (rev 13)** §1 · **หมายเหตุ heroNum
> Σ=88 ≠ สมาชิกสำนัก 562** §1 (A1 อาจลดได้เฉพาะส่วน — ปิดด้วย N1-3 attribute จาก NDJSON เดิม
> ไม่ต้องเปิดเกม) · บั๊ก comparator + sp5t0_audit.py = **งานย่อย N1-1/N1-2 (ยังไม่แก้โค้ดจนกว่า
> อนุมัติแผน)** · เกณฑ์ผ่าน/ไม่ผ่าน §5 (S1–S7 + T1–T10, T0 ปิดแล้ว) · แผน rollback §6 ·
> ตาราง deploy/split/เปิดเกมราย stage §7 (N1–N6 — N1/N2 offline ล้วน) · **สถานะ: DRAFT รอผู้ใช้
> อนุมัติแผนก่อนเขียนโค้ดใด ๆ** · ค้างในแผน: เลือกระดับ+ตัวคูณ (หลัง N1-3), นโยบาย refill, key
> `npcPolicy`, ตำแหน่ง stage ใน STAGEPLAN, T9 ผูก VPN/guest
>
> **ผลมติ 2026-10-30 (รอบถัดมา — อนุมัติเฉพาะ N1):** **N1 = ทำเสร็จแล้ว** (offline ล้วน — ไม่ deploy/
> เปิดเกม/split) · N1-1/N1-2/N1-3/N1-4/N1-5 ครบตามแผน §2 · ผล N1-3 ตามที่มติสั่งรายงาน: **A1 ครอบ
> 562/883 Heros (63.6%)** — quota รายสำนักราย band **มาจากตาราง heroNum** (25/30 สำนัก fit แถวใด
> หนึ่งภายใน [+0,+1] ต่อ band ≥5/6) · **Light (heroScale 0.5) คาดลด ~281 ตัว ≈ 31.8% — ตรงประมาณการ
> ~32% ของ design §4** (ข้อสันนิษฐาน linear — คง UNKNOWN จนวัดจริง T4) · เทสต์: C# **673 PASS/0 FAIL**
> (≥650) · python **53+110 OK** (≥53+106) · static CLEAN · build 0W/0E determinism ×2 · artifact
> **`b6d24d34…`/`4632b607…` provenance READY ยังไม่ deploy** · **N2–N6 ยังไม่อนุมัติ ห้ามเริ่ม** ·
> **ไม่เสนอเลือก L1/L2/L3 จนกว่าผู้ใช้สั่ง** (ตามมติ)
>
> **ผลมติ 2026-10-30 (รอบถัดมา — อนุมัติเฉพาะ N2 offline):** **N2 = ทำเสร็จแล้ว** (hooks G1/G4/G5
> ต่อ predicate + เทสต์ — G2/A2 **ไม่ implement** ตามแผน §7 "+G2 ถ้าอนุมัติ" ยังไม่มีมติ) ·
> artifact **`2b59e46c…`/`857cd1b8…` provenance READY ยังไม่ deploy** · C# **693 PASS/0 FAIL**
> (N1=673 → +20) · python 53+110 OK · static CLEAN · determinism ×2 · พารามิเตอร์ที่ implement
> เลือก (rounding half-up, restore ท้ายรอบ, G4=Mid/G5=Strong, install ตอนโหลด + Decide ต่อ invocation)
> บันทึกครบใน provenance §"พารามิเตอร์" — แก้ได้ก่อน runtime · **N3–N6 ยังไม่อนุมัติ** ·
> **ขั้นถัดไปที่เสนอ: ขออนุมัติ N3 (deploy/split/เปิดเกม) เป็นเซสชันแยก** — ก่อนขอ ต้องมีมติ 3 ข้อด้านล่าง
>
> **มติ 3 ข้อสำหรับ runtime (จากข้อความมติรอบ N2 — bracket ยังไม่ได้เลือก บันทึกตามจริง):**
> (1) ระดับ+ตัวคูณ: bracket เขียน "[เช่น Light ×0.5]" = **ตัวอย่าง ไม่ใช่คำตอบ** — ยังรอเลือกจริง
> (โค้ดรองรับครบทั้ง 3 ระดับแล้ว เลือกที่ net.cfg ได้ทุกเมื่อโดยไม่ build ใหม่) · (2) นโยบาย refill:
> bracket "[ตามตัวเลือกในแผน]" = ยังรอ (โค้ดมีทั้ง C2 clamp เฉพาะ Mid และ C1 deny เฉพาะ Strong —
> ระดับเป็นตัวเลือก) · (3) key `npcPolicy`: bracket "[ยืนยัน default "off" ตามที่ implement แล้ว / แก้เป็น …]"
> = **สถานะปัจจุบันยังคง default "off" ตามที่ implement ใน N1-5** (ไม่มีการแก้ — รอยืนยันชัด) ·
> ทั้ง 3 ข้อ**ไม่บล็อกงาน offline** (โค้ด level-parameterized) แต่**บล็อก N3/N4** ที่ต้องตั้งค่าจริงบน net.cfg
>
> **ผลมติ 2026-10-30 (รอบถัดมา — คำตอบจริงครบ 3 ข้อ):** **(1) ระดับ+ตัวคูณสำหรับ N3/T4 = Light
> ×0.5** (ค่าที่ implement อยู่แล้วใน `Sp5NpcScales.LightHeroScale = 0.5` — ไม่ต้องแก้โค้ด) ·
> **(2) นโยบาย refill = ไม่ใช้ในรอบแรก** — ตรง design §4 แถว L1 (C off: คงเกมเดิม เพดาน 900
> คุมอยู่); G4/G5 จึงไม่ activate ในรอบ N3/T4 (โค้ดคงอยู่ ปิดด้วยระดับ) · **(3) key `npcPolicy` =
> ยืนยัน default "off"; ตั้งระดับเฉพาะตอนรัน** — net.cfg คง `npcPolicy` ไม่มี/off นอกช่วงรัน;
> ตอน N3/T1 **ยังคง off** (T1 = วัด normal mode ไม่เปลี่ยน); การตั้ง `npcPolicy = light` จะเกิดเฉพาะ
> ตอน stage ถัดไป (N4/T2–T4) ตามรันบุ๊ค · มติ push: [อนุมัติ] เงื่อนไข origin/master = `3493bfb` ·
> มติ game push: ไม่มีอะไรค้าง · มติ popup: ไม่แน่ใจให้เขียน "ไม่แน่ใจ" (คงตามครั้งที่ 5) ·
> **ขั้นถัดไปตามมติ: ร่างเอกสารคำขออนุมัติ N3 (ยังไม่ดำเนินการ) = `plans/sp5-npc-n3-approval-request.md`**
>
> **ผลมติ 2026-10-30 (รอบถัดมา — session N3 runtime + ปิด stage):** **N3 = ทำเสร็จแล้วและปิด stage
> แล้ว** (วลี `เริ่ม N3 session พร้อม Sp5T0` = อนุมัติขั้น 1–7 ของคำขอ + ตัวเลือก census เชิงตัวเลข) ·
> รันจริง label `SP5NPC-N3-20261030-035500`: deploy `2b59e46c…` (สำรอง `.prev-6a94abb5-20261030-035500`)
> · split EXACT 0/0/0 · เปิดเกม ×3 โดยผู้ใช้ · **T1 = PASS ทุกเกณฑ์ ×3 รอบ** — gate install สำเร็จ ×3,
> outside=0/ownedLeft=0/errors=0, census h=883 ตรง T0 ทุกประเภททุกรอบ (heroNum Σ=88 ไม่ถูกย่อ),
> SaveSlot* ไม่ถูกเขียน · คืนสภาพครบ (DLL กลับ `6a94abb5`, cfg `976ccfb9` เป๊ะ, เซฟ EXACT 0/0/0,
> net.cfg ไม่แตะ) · **มติรับรายงาน: ปิด stage N3** — commit evidence `bda5200` (เฉพาะ md/json —
> log/ndjson โดน gitignore ตามมติ ห้าม force-add) + commit แผน/คำขอ N4 `3131df8` (append-only) ·
> **tag `stage-N3`** (annotated) @ `3131df8` — ชื่อจาก convention `stage-<stage-id>` ตามคำสั่ง
> "ตรวจ convention จาก tag เดิมก่อน tag" (tag เดิม: stage-S0…stage-S2a-4 · checkpoint-* แยกชุด) ·
> **คำขอ N4 ร่างแล้ว** = `plans/sp5-npc-n4-approval-request.md` (T2–T4 ที่ Light ×0.5, npcPolicy=light
> เฉพาะช่วงรัน, band T4 เสนอ [280,350], ทางวัด T2 3 ทาง — **ยังไม่ดำเนินการ รอวลีสั่งแยก**) ·
> **N5–N6 ยังไม่อนุมัติ** · push = หยุดตามเงื่อนไข (ดู §4)
>
> **ผลมติ 2026-10-30 (รอบถัดมา — session N4-prep):** **T2 = ทาง (ข) อนุมัติและทำเสร็จแล้ว (offline)** —
> เพิ่ม census field ใน `Sp5T0Probe` ครบ 4 ชุด (zhangmen จาก `ForceData.leader` field ตรง · loveable
> จาก `HeroData.loveAble` + `GDC.loveableSpeHeroList.Count` · teacher จาก `HeroData.Teacher` ของ
> สมาชิกสำนักเทียบ leader สำนักตัวเอง · birth จาก `StartGameSettingController.Instance.BirthSetting`)
> + `censusLast` ใน summary.json — **probe ยัง log-only ครบ, ตรรกะ/ตัวนับเดิมไม่ถูกแตะ** (field ใหม่
> append ท้าย line) · build Release 0W/0E ×2 determinism · artifact **`ba1e1dd1…`/`a96a4a38…` ไม่ deploy**
> (`Mods\` คง `6a94abb5…`) · เทสต์ชุดเต็ม **693 PASS/0 FAIL + 53 OK + 110 OK + static CLEAN** (= เดิม
> ทุกชุด ตรงเงื่อนไข "ไม่น้อยกว่าเดิม") · audit tool sanity PASS บน ndjson จริง N3 · provenance
> `evidence/SP5NPC-N4prep-20261030-045558/` · commit งาน = `dbca3b0` (probe + provenance) · คำขอ N4
> อัปเดตสถานะ T2 แล้ว (append) แต่**คง uncommitted** ตามการหยุดหลังพบ commit คู่ขนาน `5edb41a` (§4) ·
> **N4 ยังรอวลีสั่ง** (band T4 [280,350] + จำนวนรอบยังรออนุมัติในวลี)

### 7.2 บั๊กที่พบจากรอบ SP5-T0 — **ยังไม่อนุมัติแก้ (บันทึกค้างเท่านั้น ตามมติรอบนี้)**

1. **comparator string-format ใน `Sp5T0Probe.cs`:** expected string รูป `ns.Type.Method` vs ที่ `HarmonyX GetPatchInfo` คืน `ns:Type::Method` ⇒ `patch.audit.allMatch=false` ทั้ง 4 จุดตรวจ **แม้ inventory จริงตรง expected เชิงความหมายทุก category** — ไม่มี patch แปลกปลอม · ผลกระทบ: ไม่เปลี่ยน verdict ใด (E1/E2 UNKNOWN อยู่แล้ว) · การแก้ = แก้ probe (normalize ตัวคั่น) + harness — **ต้องอนุมัติแยก + รอบ deploy แยกเมื่อจะวัดจริงอีก**
2. **`tools/sp5t0_audit.py` (repo):** คาด `perForce` เป็น dict แต่ probe เขียน list 30 ช่อง ⇒ tool ล้ม `AttributeError` — ใช้ `evidence/SP5T0-run-20261030-012502/analyze_round.py` (ปรับ schema จริง + semantic audit) แทนไปก่อน · การแก้ tool ใน repo = อนุมัติแยก (commit tools/)
3. **หมายเหตุเล็ก:** PDB size ใน provenance prep เขียน 99,572 B แต่ไฟล์จริง 99,568 B — sha256 ตรงเป๊ะทุกไฟล์ (ตัวเลขขนาดคลาดเอง บันทึกใน VERSION.md + round-report แล้ว)

> **ผลมติ 2026-10-30 (รอบถัดมา — อนุมัติ stage N1):** ข้อ 1 และ 2 = **แก้แล้วเสร็จใน N1** ตามมติ
> "อนุมัติเฉพาะ stage N1" — N1-1: `NormalizePatchId` + เทียบ format-insensitive (harness +6 checks) ·
> N1-2: tool รองรับ schema จริง (perForce list + worldcount t=0 → key ด้วย label `at`) + dH/dT observed ·
> tests ของ tool 6→10 OK · ใช้กับ log จริงได้ (dH=883 dT=87 E5/E7 CLEAN) · รายละเอียด/ข้อจำกัด
> `evidence/SP5T0-N1-20261030-025854/provenance.md` §"งานที่ทำ" — ข้อความเดิมด้านบนคงไว้ตามวินัย
> append-only (สถานะปัจจุบันคือบรรทัดนี้)

### 7.3 ไฟล์ log/ndjson ของรอบ (รายงานขนาดตามมติ)

- `sp5t0.ndjson` = **715,055 B (715 KB)** — โดน `.gitignore` บรรทัด 60 (`evidence/**/*.ndjson` — นโยบาย "keep evidence small: big runtime logs stay local") ⇒ **ไม่ถูก commit** (ไฟล์คงอยู่บนดิสก์ 2 ที่: evidence + `UserData/LongYinCoop/SP5T0/20261030-013144/`) · ถ้าต้องการลง git = force-add หรือแก้ gitignore (**ต้องมติแยก — ผมไม่ทำเอง**)
- ML log สำเนา = 22,382 B — โดน `.gitignore` บรรทัด 47 (`*.log` — "logs stay local") เช่นกัน ⇒ ไม่ถูก commit · ต้นฉบับคงอยู่ `MelonLoader/Logs/26-10-30_1-31-40.log`
- ตัวที่ commit แล้ว (commit `bbea491`): เอกสาร/วิเคราะห์/hash ทั้งหมด 9 ไฟล์ 562 บรรทัด + `summary.json` (สรุปตัวเลขสำคัญครบ)

## 8. ผลมติ/สถานะ N4 รอบ 1 + ร่าง follow-up (append 2026-10-30 ~05:45 — offline session)

- **ผลมติ (offline session, วลีสั่ง "โหมด: offline เท่านั้น…"):** สืบ loveable เสร็จ — **การอ่าน
  ถูกต้อง (offset `loveAble @0x5F` ตรง dump.cs, typed interop)** · ต้นเรื่อง = ข้อสมมติของ N4prep
  ที่ว่า "flag = สมาชิก list 57" ซึ่ง**ขัดโน้ต RUNTIME เดิมใน repo** (`notes/area-notes.md:170` —
  true 744/883 ในโลกผู้ใช้, ตัวเอก false) และ RUNTIME N4 (631/632) · list 57 = โครงแยกโหลดจาก asset
  `GameData/LoveableSpeHero` (TextAsset 629 B หัวข้อ 剧情可解锁恋爱角色 + 57 ชื่อ — นับได้ 57 พอดี
  ตรง listCount runtime) · ผู้เขียน flag true ตอนสร้าง = **UNKNOWN offline** (dump body ว่าง, ไม่มี
  decompiler ในเซสชัน) · รายละเอียด `evidence/SP5NPC-N4-20261030-052251/loveable-investigation.md`
- **ค้างตัดสิน (1):** อนุมัติ/แก้ ร่างนิยาม T2 ข้อ loveable ใหม่ — listCount=57 + ทุกชื่อใน list
  ต้องมี HeroData ใน Heros (match ชื่อ ยังไม่ RUNTIME-confirmed) + เลิกใช้ heroCount ตัดสิน —
  `plans/sp5-npc-n4-followup-draft.md` ร่าง 1
- **ค้างตัดสิน (2):** อนุมัติ/แก้ ร่างจุดอ่าน heroNum ใหม่ — in-window Σ=50 ที่ GenDataC call แรก
  (deterministic จาก nesting) + post-restore Σ=88 ที่ sng.end (gate ไม่ patch StartNewGame) —
  พร้อมข้อควรรู้: genhero.end ใช้เป็น post-restore **ไม่ได้** (postfix รันก่อน gate finalizer) และ
  genhero.begin เดิมเป็นจุด order-ไม่-deterministic — ร่าง 2 ของไฟล์เดียวกัน · ยังไม่เขียนโค้ด
- **ค้างตัดสิน (3):** ทางปิด UNKNOWN "ใครเขียน flag true ตอนสร้าง" — disassembly
  `SetHeroLoveAble`/`GenerateHeroData` = งาน RE อนุมัติแยก (ถ้ายังต้องการ)
- **ค้างตัดสิน (4):** N4 รอบ 2–3 (รันต่อ) — ต้องรอนิยาม T2 ใหม่ผ่านมติก่อน (รอบ 1 หยุดที่ T2 ·
  T3 f2=0 ผ่าน · T4 band ผ่านที่ sect=311 แต่ per-force/rank ยังไม่วิเคราะห์เชิงลึก · heroNum
  restore รอบถัดไปไม่ได้วัด)
- **commit หลักฐาน N4 รอบ 1** = อนุมัติในเซสชันนี้ (path-scoped: evidence N4 session-record +
  loveable-investigation + แผน 3 ไฟล์) · push = ไม่ push ตามมติ · decisions-pending.md คง untracked
  (สถานะเดิม — จะ commit เมื่อมีมติ)
- **บทเรียนกระบวนการ:** ตั้งค่าคาดหวัง census ใหม่ต้อง grep โน้ตทั้ง repo (notes/) ก่อน — ข้อสมมติ
  "57" ข้ามหลักฐาน RUNTIME ที่มีอยู่แล้ว ทำให้ N4 รอบ 1 หยุดกลางทาง (รอบเกม 1 ครั้งเสียไปโดยไม่จำเป็น)

> **ผลมติ 2026-10-30 (รอบถัดมา — N4-follow-up offline ชุดที่ 2):** ยืนยันผู้ใช้เปิดเกมเอง 05:45
> (ใช่ — ML log ยืนยัน) · **อนุมัติทาง (ก) rebaseline volatile-only แล้ว** — baseline ใหม่
> **`7e1e65e52659…` @ `coop-test/baselines/20261030-060854`** (56/57 EXACT + PlayerprefData.dat
> เดียว, `REBASELINE.json` volatile_only=true) · cfg divergence `976ccfb9→733d9a0c` บันทึก
> VERSION.md แล้ว (ค่า key false ครบ) · anchor เซสชันนี้ต่อไป = ค่าใหม่ · จากนั้น **ร่าง follow-up
> อนุมัติทั้ง 2 ร่างและทำเสร็จแล้ว**: artifact `476f390e…`/PDB `e8c44e4f…` (ไม่ deploy,
> `evidence/SP5NPC-N4prep2-20261030-062233/`) · C# **705/0** (693+12 name-match) · python 53+110 ·
> static CLEAN · audit tool รูปเดิม+synthetic ใหม่ PASS ทั้งคู่ · อัปเดตคำขอ N4 (append ที่ 3):
> deploy `476f390e…` แทน `ba1e1dd1…`, เกณฑ์ T2 loveable ใหม่ (listCount=57 + listPresent=57,
> heroCount = observational, รอบแรก = ยืนยันกติกา match), BirthSetting เทียบฐานรอบ 1
> (count=6 vals [-1,0,0,0,0,0]), รอบ 2–3 ต้องรันครบ · commit 2 อัน (ไม่ push) · disassembly
> loveAble = ไม่อนุมัติ · decisions-pending.md คง untracked
> **ค้างตัดสินถัดไป:** วลีสั่ง N4 รอบ 2 (deploy `476f390e…` + split baseline `20261030-060854` +
> net.cfg สองชั้น + เปิดเกม ×2 รอบ + คืนสภาพครบ — แยกเซสชันเกมตามกติกาคาดโทษ)

## 9. มติ design จากผู้ใช้ 2026-10-30 — pause-policy + battle turn timer + ขอบเขต co-op (สถานะ: **บันทึกแล้ว — ยังไม่ implement ห้ามเริ่มจนกว่าจะมีแผน stage แยกผ่านการรีวิว**)

> ที่มา: ข้อความผู้ใช้ 2026-10-30 (session offline) — ตอบช่อง "pause-policy ยังไม่ออกแบบจนผ่าน Gate 5"
> ของ time-plan-draft §A.2 · **ข้อจำกัด: ทุกข้อในหัวข้อนี้เป็น DESIGN mandate ไม่ใช่ผลวัด — ห้ามอ้าง
> เป็น RUNTIME/DUMP ยกเว้นบรรทัดที่ปักป้ายไว้ชัด** · implement ของข้อเหล่านี้**อยู่นอกขอบเขต
> time-plan-draft** (§A.2: ห้ามแก้ production code/protocol/multiplayer ภายใต้แผนนั้น) — ต้องเป็น
> แผน stage ใหม่ รีวิวตาม §F (Gate 6) และผ่านกติกา 5–7 (write-path: Harmony prefix/button gating)

### 9.1 ข้อกำหนดที่ผู้ใช้ตัดสินแล้ว (MANDATE)

| # | มติ | หมายเหตุ/ข้อเปิดที่ยังต้องตัดสิน |
|---|---|---|
| T-PAUSE-1 | **ยกเลิกระบบ pause เมื่อ co-op ทุกโหมด** — Battle + BigMap/worldmap + Area (เมือง/สำนัก) + Explorer (ดันเจี้ยน) · เวลาปฏิทินต้องไหลอยู่ตลอด | โหมดเดี่ยว (non-co-op) คง pause เดิมหรือใช้กติกาเดียวกัน = **ยังไม่มีมติ** |
| T-TURN-1 | **Battle turn timer 30 วิ/เทิร์น** (สไตล์ TS Online): host เป็นเจ้าของตัวจับเวลา — เห็นยูนิตผู้เล่นเข้า `WaitPlayerInput` → เริ่มนับ 30 วิ → ส่งเวลาเริ่มให้ client **แสดงผลอย่างเดียว ไม่ตัดสินเอง** → ครบกำหนด host **บังคับจบเทิร์น** | ต้องใช้ path จบเทิร์นของเกมเอง ห้ามเซ็ต state เอง (ดู 9.2) · การส่งเวลาให้ client = **protocol change** (ปัจจุบันไม่มี message timer — ห้ามแก้ใน time-plan) |
| T-TURN-2 | ตัวจับเวลาใช้ **real/unscaled time ฝั่ง host** — ไม่ใช้ `Time.timeScale` (process-wide + `TimeScaleController` ปรับตอน slow-mo/hit-stop) | ตรงข้อเท็จจริง DISASM เดิม: ปั๊มงานใช้ `RealTime.get_deltaTime()` (`time-audit-notes` §30) · battle มี `GetBattleDeltaTime()` [DUMP] ต้องรอบสำรวจยืนยันพฤติกรรม |
| T-SCOPE-1 | **co-op multiplayer เฉพาะโหมด sandbox** — ไม่สร้างโหมดใหม่ อิงโหมด sandbox ที่มีอยู่ · ผู้เล่นเข้า world = ไม่มีสำนัก ต้องหา/เลือกเข้าสำนักเอง (แรงบันดาลใจ GTA V FiveM RP / Mount & Blade RP server) | กระทบโครงสร้างเริ่มเกมของแผน co-op ระยะยาว (หลัง V1/V2) — ต้องสำรวจ "โหมด sandbox" ของเกมจริงว่าเริ่ม world ต่างจากเกมปกติอย่างไร (ยังไม่เคยสำรวจ) |

### 9.2 ข้อเปิดที่แผนถัดไปต้องร่างให้ชัด (ระบุโดยผู้ใช้)

- **T-UI-1 ปุ่ม speed/skip ของ battle:** speed ทำให้ animation สองเครื่องไม่ตรงกัน · skip ทั้งศึก
  (`BattleSkipButtonClicked()`/`SureSkipBattle()` [DUMP]) ต้องกำหนดว่าใครมีสิทธิ์กด — **เปิดเป็นข้อตัดสิน**
- **T-TURN-3 กลไก "จบเทิร์นเฉย ๆ":** ยังไม่รู้ path เดิมของเกม — **ไม่มี method ชื่อ EndTurn ใน dump.cs**
  · candidate [DUMP รอ RUNTIME ยืนยัน]: `RestButtonClicked()`/`PlayRest()` (จบเทิร์นแบบพัก —
  nowActiveState เปลี่ยนเป็น Rest หรือไม่ต้องวัด), `FinishRound()`, `AutoButtonClicked()`
  · ปุ่ม pause ฝั่ง battle [DUMP]: `BattlePauseButtonClicked(GameObject)` +
  `SetPauseButtonInteractable(bool)` + `battlePaused` (0x1D0) + `HaveBattlePauseUI()`/`BattlePaused()`
  — จุด block ที่เสนอ = Harmony prefix ที่ `BattlePauseButtonClicked` + ปิด interactable (write-path)
- **T-PAUSE-2 pause นอก battle (Area/Explorer):** BigMap **ไม่มี paused flag เลย** (เวลาหยุดเพราะ
  ไม่มีใครเรียก `ChangeDay` — RUNTIME `time-notes` §35) · Area/Explorer มี modal stack
  `SureMenu.pause` ถูกอ้างใน addendum §16 — ยัง**ไม่เคยสำรวจ**ว่า pause ของสองโหมดนี้ทำงานผ่านอะไร
  ทั้งหมด (survey อนุมัติแยกก่อนร่างแผน)
- **T-DAY-1 อัตราเวลา "1 วัน = 10 วินาที":** ผู้ใช้ระบุเป้า (กิจกรรม active/interactive — work/ฝึกวิชา/
  ขุดแร่/ตัดไม้: ผู้เล่น AFK รอเวลา) — **ขัดตัวเลขที่วัดไว้: slope V2-A = 1.2675 วิ/วัน** (default
  `dayTime=1.0`, PROVISIONAL) · 10 วิ/วัน ตรงกับ `dayTime≈8` (เคสแรกของ V2 ชุด B: T(1) = 8 + 3×0.25
  = 8.75 วิ) มากกว่า — **ต้องให้ผู้ใช้ยืนยัน**: (a) เป้าหมาย design ที่ต้องการ (⇒ ผูกกับ Gate 5b เขียน
  `dayTime`) หรือ (b) เข้าใจว่าเป็นค่าปกติของเกม (⇒ ไม่ตรงการวัด — แก้ความเข้าใจ)
- ที่มาของเรื่อง active/interactive = **time-plan-draft** (D2 per-day credit, ชุด C/Wait, V2) +
  `time-followup-notes` — **ไม่ใช่ sp5-t0-plan** (ปิดแล้ว = probe บัญชี NPC ตอน creation round)

### 9.3 ลำดับงานที่เหลือเพื่อ "ปิดแผนเวลา" ก่อนถึงงานนี้ (ตาม §F — ทุก gate ต้องวลีสั่งจากผู้ใช้)

1. **V2-A ปิดโดยสมบูรณ์:** เลือก (ก) รอบวัดซ้ำ N=5 (ต้อง deploy + split + เปิดเกม — อนุมัติแยก) หรือ
   (ข) ยอมรับ PROVISIONAL เป็นคำตอบของชุด A (ต้องมีมติแยกด้วย)
2. **V1** (วัด cancel/refund/finish — ตรงเงื่อนไข D1/D2 ที่มติแล้ว): Gate 2 ผ่านแล้ว · ขาด **Gate 3**
   (อนุมัติ probe build V1) → รัน TC-1–TC-4 → Gate 4 รีวิว
3. **V2 ชุด B** (เขียน `dayTime`/`pauseTime`): **Gate 5b** — ถ้า T-DAY-1 ต้องการ 10 วิ/วัน ข้อนี้คือขั้นพิสูจน์
4. **ชุด C/Wait** (slider วัน — Gate 5c/D10): เฉพาะหลังยืนยันค่า `TestMode`
5. จบ V1/V2 ⇒ **Gate 5/6: ร่างแผนใหม่แยก** — คิว host/validator + pause-policy (T-PAUSE-1/2) +
   turn timer (T-TURN-1/2/3) + กติกา speed/skip (T-UI-1) + ขอบเขต sandbox (T-SCOPE-1) — แผนนั้น
   ต้องรีวิวผู้ใช้ก่อน implement ทุกครั้ง · ห้ามรวมเซสชันเกมกับสายงานอื่น (กติกาคาดโทษ)

## 10. CLOCK-1 — ปฏิทินกลาง server + ไม่มี pause (append 2026-10-30 — มติผู้ใช้ + ร่างจาก session อื่น + รีวิว repo แล้ว)

- **ไฟล์มติ:** `plans/CLOCK-1-decision-draft.md` (tracked) — ร่างต้นฉบับผู้ใช้นำมาจาก session อื่นที่ไม่มี
  repo access (ไฟล์ใน Downloads) · session นี้ตรวจซ้ำทุกข้ออ้างกับ repo, ปักหมุด DUMP เพิ่ม, แก้ 2 จุด
  (คำว่า "โหมดใหม่" + ป้าย RUNTIME ของ "ศึกไม่แตะเวลาโลก" → INFERRED) — รายละเอียด §6 ของไฟล์
- **มติที่ได้ (ผู้ใช้ 2026-10-30):** **P1** server เจ้าของปฏิทิน เดินตามเวลาจริงตลอด — อัตรา = config
  `secondsPerDay` **ยังไม่ล็อก 10 วิ** · **P2** ไม่มี pause ทุกฉากใน co-op (เมนูเปิดได้ โลกไม่หยุด) ·
  **P3** กิจกรรมหลายวัน = (วันเริ่ม, N) บนปฏิทินกลาง — AFK ได้ ขนานกันได้ · **P4** battle 30 วิ/เทิร์น
  นับเฉพาะ `WaitPlayerInput` ด้วยเวลาจริง host หมดเวลา = ข้ามเทิร์น · **a** เดินทาง/ดันเจี้ยนใช้เวลาจริง
  นาฬิกา server · **b** กิจกรรมขนาน = adapter ต่อผู้เล่น · **d** โหมดเดี่ยว pause ไม่ได้เช่นกัน ·
  **e** D2 ใช้ต่อ + D3–D8 เดิมเขียนใหม่
- **ยังเปิด — ข้อ c:** อัตรา `secondsPerDay` เทียบเวลาเควส/อีเวนท์ (หน่วยวัน [DUMP]) — รอ **M1** (static
  survey, ต้องอนุมัติ) → **M2** (runtime, เซสชันแยก) — เสนอเริ่มทดสอบ 60–120 วิ/วัน · ทางเสริม C1 (เลือก
  อัตรา)/C2 (ชดเชยเวลาในศึก-ดันเจี้ยน)/C3 (คูณ leftTime ตอนสร้าง) เลือกหลังวัด
- **ผลต่อเอกสารเดิม:** T-DAY-1 ของ §9 = ถูกแทนด้วย P1 · คำถาม "โหมดเดี่ยว" ของ T-PAUSE-2 = ปิดด้วย d ·
  T-PAUSE-1 → P2, T-TURN-1/2 → P4 · **T-UI-1 (speed/skip) และ T-TURN-3 (path จบเทิร์น) ยังเปิด** ·
  D3–D8 ของ time-plan §C.3 = เขียนใหม่ตาม e · §9.3 (ลำดับปิด time-plan) ยังใช้ได้เหมือนเดิม
- **ข้อบังคับที่ยกจาก audit §6.2 เข้าแผน implement ในอนาคต:** ทาง (2) (นาฬิกา server แยก) ต้องกำหนดกฎ
  "ใครเป็นผู้เดินวัน" ให้เดียว — แนะนำเชิงโครง host เท่านั้น [OBSERVED audit :381–382]
- **ค้างอนุมัติถัดไป:** (1) M1 static survey (2) M2 runtime session (3) วลีสั่งปิด V2-A ตาม §9.3 ข้อ 1 —
  งาน implement ของ CLOCK-1 ทั้งหมดอยู่หลัง Gate 5/6 ห้ามเริ่มโค้ดก่อน
- **อัปเดต 2026-10-30 (รอบ 2 — รายละเอียด §7 ของไฟล์ CLOCK-1):** มติเพิ่ม = **ค่าตั้งต้นทดสอบ
  `secondsPerDay` = 120 วิ/วัน** (1 ชม.จริง ≈ 1 เดือนเกม — ยังเป็น config ปรับได้ ล็อกสุดท้ายรอ M1/M2) ·
  **กฎ "กิจกรรมกินเวลาจริง ≤ ~25% ของเควสสั้นสุด (~5 นาที)" = ข้อเสนอผู้ใช้ ยังไม่ใช่มติ** — anchor
  "เควสสั้นสุด" ยังไม่เคยวัด ต้องมาจาก M1 · M1 เพิ่มหน้าที่: `leftTime` ของอีเวนท์แผนที่ (โครง DUMP
  พร้อม — time-notes §99, ค่าอยู่ในตารางข้อมูล อ่าน static ได้) · M2 เพิ่มหน้าที่: เวลาศึกจริง + จำนวน
  เทิร์น/ศึก (เพดาน P4 = 30 วิ × เทิร์น) + เวลาบทสนทนา/ร้านค้า · N=15 = ~30 นาที AFK → ตัดสินแยกใน
  หัวข้อ "ค่า N" ของ D3–D8 · ข้อ c = มีค่าตั้งต้นแล้ว **ยังไม่ปิด** จน M1/M2 ยืนยันเกณฑ์
- **อัปเดตรอบ 3 (2026-10-30 ต่อเนื่อง — รายละเอียด §8 ของไฟล์ CLOCK-1):** `secondsPerDay`
  ค่าตั้งต้นทดสอบ = **720 วิ/วัน** (1 เดือน 30 วัน = 6 ชม.จริง · 12 นาทีจริง/วัน — **แทน 120 รอบ 2**) ·
  งาน AFK ถูกปรับโดย ACT-1 รอบ 3 (ชั่วยาม = 60 วิจริง — ไฟล์ NPC-1-ACT-1 §4) ⇒ หมายเหตุ "N=15 =
  30 นาที" รอบ 2 หมดผล (15 วัน = 3 นาที) · กฎ 25% anchor ใหม่ ≈ 30 นาที (ถ้าเควสสั้นสุด 10 วัน) รอ M1 ·
  ความเสี่ยงใหม่: 1 วันจริง = 120 วันเกม ⇒ เควสหมดเวลาตอนออฟไลน์ — ต้องตัดสินนับเฉพาะออนไลน์/ชดเชย
  (แนว C2) — ผูกกับ O4 (อายุ) ด้วย

## 11. ปิด stage N4 + ค้าง push (append 2026-10-30 ~06:5x — offline session)

- **มติที่ทำแล้ว:** รับรายงาน N4 → **ปิด stage N4** → tag `stage-N4` (annotated) ชี้ `994d9c7`
  (commit ปิด — มี evidence + สถานะแผน + ร่าง N5 ทั้งหมดใน ancestry) · commit ของเซสชันนี้:
  `0fdb778` (evidence N4 3 โฟลเดอร์ — summary JSON + session-record + HANDOFF เก่า) และ
  `994d9c7` (แผน + VERSION.md divergence + ร่าง N5 + INDEX.md) · อัปเดตแผน 2 ไฟล์ + INDEX.md
  (append-only) · ร่างคำขอ N5 = `plans/sp5-npc-n5-approval-request.md` (T5–T7 ที่ Light —
  **ยังไม่ดำเนินการ**) · N6/ระดับ Mid/Strong = ยังไม่อนุมัติ · decisions-pending.md คง untracked
- **ค้างตัดสิน (1) — push mod ถูกบล็อกตามเงื่อนไขของมติเอง:** มติกำหนด "origin/master = b58d553"
  แต่ **fetch จริง 06:5x = `772c8f2`** (superset — มี commit evidence N4 รอบ 1 ซึ่งถูก push
  จากเซสชันก่อนหน้าแล้ว) ⇒ ตามเงื่อนไข "origin ≠ b58d553 = หยุดรายงาน" ⇒ **ไม่ push** ·
  ช่อง "[อนุมัติ/ไม่อนุมัติ]" ของ push mod/push tag ยังไม่ได้ระบุ (ถามแล้วไม่ได้คำตอบ) ·
  สถานะ local นำ origin: `159f868`, `fdc70ec`, `d3329fa`, `0fdb778`, `994d9c7` (5 commits) + tag
  `stage-N4` · ทางเลือกรอมติ: (ก) push จน HEAD รวม d3329fa (CLOCK-1 draft จากเซสชันขนาน —
  plans-only) (ข) push เฉพาะถึง fdc70ec ไม่รวม d3329fa แล้วตัดสิน d3329fa แยก (ค) ไม่ push
  ต่อ — ทั้งหมด fast-forward ไม่ force · ถ้าจะ push: ยืนยัน origin/master ณ ตอนนั้นก่อนทุกครั้ง
- **ค้างตัดสิน (2) — HEAD ต่างจากที่มติระบุ:** มติ session-start ระบุ HEAD = `fdc70ec` แต่
  HEAD จริง = **`d3329fa`** (CLOCK-1 decision draft, commit 06:47:28 จากเซสชันขนาน —
  plans-only: INDEX.md +1, plans/CLOCK-1-decision-draft.md +108, ไม่แตะโค้ด/เกม/เซฟ) ·
  เซสชันนี้ดำเนินการงาน local ที่ไม่อันตรายต่อไปบน d3329fa (commits/tag คง local ทั้งหมด —
  ย้อนได้) และรายงานตามจริง · ถ้าผู้ใช้ต้องการให้ tag/commit แยกออกจาก d3329fa = ต้องมีมติแยก
  (ห้าม rewrite history ตามกติกา)
- **ค้างตัดสิน (3) — N5:** คำขอร่างแล้ว — วลีสั่งต้องระบุ: ทางวัด T7 (ก) จอ/มือ (ข) เพิ่ม census
  field ใน probe = งานโค้ดอนุมัติแยก (ค) ndjson บางส่วน · นโยบายโลกทดสอบ: เซฟ slot บนสำเนา
  (SaveSlot* เขียนบนสำเนา — ต่างจาก N3/N4) หรือเดิน 25 วันต่อเนื่องเซสชันเดียว · แบ่งกี่เซสชันเกม
- **ค้างเดิมทั้งหมดคงอยู่:** N6/ระดับ Mid/Strong · S3 rev2 · VPN/guest (Q9) · E4 วัดซ้ำความละเอียด
  สูง · perForce vs template/rank วิเคราะห์เชิงลึก (ข้อมูลครบใน ndjson N4)

## 12. NPC-1 + ACT-1 — สหายร่วมตี้ + งาน AFK หน่วยชั่วยาม (append 2026-10-30 — มติผู้ใช้ + ร่างจาก session อื่น + รีวิว repo แล้ว)

- **ไฟล์มติ:** `plans/NPC-1-ACT-1-decision-draft.md` (tracked) — ร่างต้นฉบับจาก session อื่นที่ไม่มี
  repo access (Downloads) · session นี้ตรวจซ้ำข้ออ้างครบ (§2b ของไฟล์ — **ผ่านทั้งหมด**: `GetMaxTeamMate`
  = heroForceLv+1 [asm area-notes:183–185], `SetAllAuto(bool)` [dump.cs:363654], `ChangeYear()`
  [dump.cs:376788], ชุด N∈{1,5,10,15} [audit §6.5], `autoLeaveTeamDay` 0x300, `loveAble` @0x5F)
- **มติ NPC-1 (N1–N7):** ตัวจริง = ข้อมูล server (ไม่รับผลจากการรับเข้าตี้ — สงครามสำนักใช้ตัวจริง) ·
  สำเนาต่อผู้เล่น copy-on-recruit · ID สังเคราะห์ · **เทมเพลต ณ ตอนสร้างโลก** (สำเนาเก่งตามผู้เล่นปั้น —
  กันของแรงฟรีจากการก๊อปสถานะปัจจุบัน) · ความสัมพันธ์เป็นของสำเนา · **ตี้สูงสุด 4 รวมผู้เล่น** (ต้อง
  override `GetMaxTeamMate` — ค่า heroForceLv ของผู้ไม่มีสำนักยังไม่รู้) · สำเนาตายไม่ได้ (บังคับโดย
  mod — ไม่พึ่งข้อสังเกต "NPC sandbox ไม่ตาย" ซึ่งคงป้าย [USER] ยังไม่พบในบันทึก)
- **มติ ACT-1 รอบ 3 (แทนชุด {1,6,12} ชั่วยามของ v2 เพราะ 6/12 นาที "นานไป กลัวคนเล่นเบื่อ"):**
  แสดงผล+จังหวะรายตัวเลือก — **1 วัน→ครึ่งชั่วยาม 30 วิ · 5 วัน→1 ชั่วยาม 60 วิ · 15 วัน→3 ชั่วยาม
  180 วิ · 20 วัน→5 ชั่วยาม 300 วิ** (1 ชั่วยาม = 2 ชม.ในเกม = 60 วิจริง — ตรงสูตร วินาที=ชั่วยาม×60
  ทุกแถว · ภายในโค้ดยังเป็น "วันงาน" เดิม เปลี่ยนแค่ display + จังหวะ)
- **ช่องว่างที่ต้องตัดสินต่อ (จากภาพเหมือง USER-SCREENSHOT 2026-10-30):** (1) **10 วัน หายจาก mapping
  แต่มีจริงในภาพ** (เหมืองมี 5/10/15 + ยกเลิก) — เสนอ **2 ชั่วยาม = 120 วิ** (แนวเส้นตรง 5→1, 15→3) รอมติ
  (2) 20 วัน ยังไม่ยืนยันว่ากิจกรรมใดมี (ภาพเหมืองสูงสุด 15 · audit N∈{1,5,10,15}) (3) **รางวัลต่อชั่วยาม
  = ข้อเปิดหลัก** — แนว "เต็มตามวันเดิมของตัวเลือก" = เร็วกว่าอัตราปฏิทิน ~60× (v2 = ~12×) ต้องยืนยันจาก
  ผู้ใช้ + ปรับ config จากการเล่นจริง (4) ปุ่ม **"เร่งเวลา"** บนจอเหมืองยืนยันจากภาพ — ต้องปิดใน co-op
- **O1–O7 ยังเปิด (รายละเอียดในไฟล์):** เทมเพลต snapshot ณ สร้างโลก (ผูกผล SP5-NPC ลดจำนวน) · 30 วิ/เทิร์น
  นับต่อยูนิตหรือเจ้าของ (ตัดสินหลังวัดจำนวนเทิร์น/ศึก) · autoLeaveDay ของสำเนา (เสนอปิด) · อายุ/ความชรา
  สำเนา · favor เริ่มต้น · UI ป้ายสำเนา vs ตัวจริง · "ตัวจริงตายได้ไหม" (static ตรวจได้)
- **ค้างอนุมัติรวมสาย CLOCK-1/NPC-1:** M1 static survey (ซ้าย: leftTime เควส/อีเวนท์แผนที่ + ชุดตัวเลือก
  กิจกรรมจากตารางข้อมูล + O7) → M2 runtime (เซสชันแยก) — implement ทั้งหมดอยู่หลัง Gate 5/6 ·
  ห้ามเริ่มโค้ดก่อนแผน stage ผ่านการรีวิว

- **อัปเดตรอบ 6 (2026-10-31 — แก้ถ้อยคำ §12 ให้ตรง NPC-1-ACT-1 §12 (รอบ 5 ฉบับสั่งงาน) + §13 (รอบ 6) — งาน 1.3 ตามมติผู้ใช้ · ไม่ลบข้อความเดิม ประกาศ supersession รายข้อ):**
  - ตาราง "มติ ACT-1 รอบ 3" ด้านบน (1→ครึ่งชั่วยาม 30 วิ · 5→1 · 15→3 · 20→5 ชั่วยาม + "ภายในโค้ดยังเป็นวันงานเดิม เปลี่ยนแค่ display + จังหวะ") = **ถูกแทนด้วย (ฉ) ของ NPC-1-ACT-1 §12**: สูตร `max(30, 12×N)` วินาทีจริง ทุก N · ป้ายชั่วยามปัดครึ่งชั่วยาม display เท่านั้น · ป้ายรางวัลสองหน่วย (ฌ) — ตารางเก่าเก็บเป็น "เคยเสนอ/มติรอบ 3"
  - "เสนอ 2 ชั่วยาม = 120 วิ … รอมติ" = **ปิดแล้ว** โดย (ฉ) (10→120 วิ ตรงสูตร ไม่ต้องเสนอแยก)
  - "รางวัลต่อชั่วยาม = ข้อเปิดหลัก — แนว 'เต็มตามวันเดิมของตัวเลือก' … ต้องยืนยันจากผู้ใช้ + ปรับ config" = **ปิดแล้ว** โดย (ช) = ช-A (คง dayNum/รางวัลต่อวันงานเดิม ไม่เขียนตัวจ่ายใหม่) — ส่วน "ปรับ config จากการเล่นจริง" เหลือเป็น **"การปรับความแรงรางวัล (ช-A) ยังไม่ออกแบบ"** (ยังเปิดตามรอบ 6 — ไม่มี `rewardDaysPerShichen` แล้ว)
  - "M1 static survey … ค้างอนุมัติ" = **อนุมัติแล้วเฉพาะส่วนอีเวนท์ตามแผนที่** (R6-7 — static อ่านอย่างเดียว · เศรษฐกิจเหมือง 16%/วัน **ไม่อยู่ใน M1** · ไม่รวม Q6) · M2 runtime + O7 ยังเปิดเหมือนเดิม
  - ผล A1/A1.5 และมติ R6-1..R6-8 ครบที่ `plans/NPC-1-ACT-1-decision-draft.md` §13 (แผน ACT-1 rev 2 อยู่ในไฟล์แผน)
