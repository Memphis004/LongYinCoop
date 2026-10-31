# V1 (time-plan) — บันทึกสถานะ: **หยุดชั่วคราว / ยังไม่ปิด** (แทนถ้อยคำ CLOSED-CANCELLED)

- **วันที่บันทึก:** 2026-10-31 · session `AUTO-20261031-174215` (docs-only) · **มติผู้ใช้ 2026-10-31**
- **สถานะ V1 = หยุดชั่วคราว / ยังไม่ปิด (ไม่ใช่ CLOSED-CANCELLED)** — ไฟล์นี้ประกาศสถานะเหนือปก `V1-close-report.md` บรรทัด 1 ("ยกเลิก (CLOSED-CANCELLED)") — **ไฟล์เดิมไม่ถูกแก้** (เป็นหลักฐานของ session ปิดสาย ณ ตอนนั้น) · ต้นเหตุการหยุด = ชนกับสาย CLOCK-1/ACT-1 (มติผู้ใช้)
- ยืนยันจากไฟล์ ณ วันบันทึก: rollback DLL `6a94abb5…`/PDB `856d3bed…` + `.prev-*` คงอยู่ [VERSION.md :193–204 · `rollback-user-cancel-20261031.md`] · baseline ใหม่ `7cd024019604…` @ `coop-test/baselines/20261031-063808` [VERSION.md :223–226]

## เงื่อนไขที่ต้องมีก่อนกลับมา (มติผู้ใช้ 2026-10-31 — ถ้อยคำตรงตามคำสั่ง)

1. **แก้ ExpectedPatchCount** (นับจำนวน method ไม่ใช่ patch รวม)
2. **เทสต์ที่ติด Harmony จริงแล้วนับ**
3. **build/provenance ใหม่**
4. **มติ deploy แยก**

## หมายเหตุข้อเท็จจริง ณ วันบันทึก (ไม่แก้เงื่อนไขข้างบน — รายงานความคืบหน้าเท่านั้น)

- **ข้อ 1:** ปิดแล้วโดย fix patch-count gate `f4947b2` — TimeV1 gate เปลี่ยนจาก `ExpectedPatchCount` เป็น set+entries matcher `{stop,msw}×{3,3}` (ตรวจครบชุดเป้าหมาย + จับ partial-install ได้) · วินิจฉัย false-fail เดิม = `CountOwnedPatches` นับ method ไม่ใช่ patch-entries [INFERRED-from-code — `patchcount-audit.md:21, :36`] · suite 808 PASS/0 FAIL
- **ข้อ 2:** มี harness เชิงหลักแล้ว (`PatchGateHarness` 25 checks semantic บน 0Harmony 2.10.2 จริง — `d27e281`) — การทดสอบ V1 บน build ใหม่**ยังต้องทำตอนกลับมา**
- **ข้อ 3–4:** ยังไม่ทำ — การ build/deploy ใด ๆ ต้องมีมติแยก (ห้ามแตะ `Mods\` จนกว่านั้น)

## หลักฐานประกอบ

- `V1-close-report.md` — verdict `INCOMPLETE-probe-down` · TC-1…TC-4 ไม่ได้ผลวัด · R1/R2 = NOT-MEASURED
- `rollback-user-cancel-20261031.md` · `analyze-TC1-r1.json` · `patchcount-audit.md` (ใน `evidence/DOCS-A1-20261031-065625/`)
- ผลต่อสายงานอื่น: ACT-1 rev 4 = `plans/ACT1-plan-rev4.md` §R4-2 แถว 5 (ยกเลิก/ตัวนับวัน/refund = รอผล V1)
