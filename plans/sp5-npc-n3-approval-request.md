# คำขออนุมัติ stage N3 — SP5-NPC (T1 normal mode gate off) — **ร่าง ยังไม่ดำเนินการ**

- **label:** `SP5-NPC-N3-request-20261030` · อ้างแผน `plans/sp5-npc-reduce-plan-draft.md` §7 (แถว N3)
  + §5 T1 · มติที่ปิดแล้ว: ระดับ = **Light ×0.5** (สำหรับ stage ถัดไป) · refill = **ไม่ใช้ในรอบแรก** ·
  key `npcPolicy` = **default "off" ตั้งเฉพาะตอนรัน** (provenance: `evidence/SP5NPC-N2-20261030-033852/`)
- **สิ่งที่ขออนุมัติ:** รัน stage N3 เท่านั้น — **N4–N6 ยังไม่รวม** (ต้องวลีสั่งแยกทุก stage ตามแผน §7)

## ขอบเขต N3 (ตามแผน §5 T1 + §7)

**T1 = พิสูจน์ normal mode ไม่เปลี่ยน บน build ใหม่ที่มี gate:** `npcPolicy` **คง "off" ตลอด stage
นี้** (ไม่ตั้ง light — การตั้ง light เป็นของ N4/T2–T4) — hook ทั้ง 4 ติดอยู่แต่ Decide = Off ⇒ no-op
ทุก invocation

| เกณฑ์ T1 (จากแผน §5) | วิธีวัด |
|---|---|
| ไม่มี patch นอกที่ติดตั้ง (log ยืนยัน) | ML log: `Sp5Npc gate installed` + ไม่มีบรรทัด patch อื่น + owned count ตรง |
| census/จำนวน/กระจาย ใน band RNG ของ baseline (**≥3 รอบ**) | สร้างโลกใหม่ 3 รอบ (แต่ละรอบ = เปิดเกมครั้งใหม่) — ใช้ probe `Sp5T0` วัด? **ไม่ — T1 ไม่เปิด probe** (แยกขอบเขต): นับจาก census UI จอ + ตรวจ ML log gate ไม่ activate; **ถ้าต้องการ census เชิงตัวเลข = เปิด Sp5T0 pref เพิ่ม (ขออนุมัติในวลีสั่งเป็นตัวเลือก)** |
| hash ไฟล์เกม/ตารางไม่เปลี่ยน | hash เกม 2 ไฟล์ก่อน/หลัง · `SaveSlot*` ไม่ถูกเขียน (โลกใหม่ไม่เซฟ) |

## รายการดำเนินการ (เมื่อได้รับอนุมัติ — ลำดับ)

1. session-start read-only (hash เกม/Mods/เซฟ/git ตรง snapshot)
2. **deploy** `evidence/SP5NPC-N2-20261030-033852/LongYinCoop.dll` = `2b59e46c…` + PDB `857cd1b8…`
   → `Mods\` — **สำรอง `6a94abb5…` เป็น `.prev-6a94abb5-<ts>` ก่อน** (rehash หลัง deploy ตรง artifact)
3. **save-split** `--expected-baseline coop-test/baselines/20261028-071340/savehash.real.before.txt`
   (label `SP5NPC-N3-<ts>`) + เก็บ hash ก่อน/หลัง
4. **ผู้ใช้เปิดเกมเอง ×3 รอบ** — แต่ละรอบ: สร้างโลกใหม่ → (ไม่จำเป็นต้องเล่นต่อ) → ปิดเกม ·
   **ห้ามเซฟลง slot** (โลกทิ้งได้) · ระหว่างรอบ agent ตรวจ ML log + hash ก่อนเปิดรอบถัดไป
5. เก็บ evidence (ML log ×3, ตรวจ T1 รายเกณฑ์, hash ก่อน/หลัง)
6. **คืนสภาพ:** restore เซฟ EXACT + **rollback DLL กลับ `6a94abb5…`** (รูปแบบเดิมทุก session)
7. รายงานรายเกณฑ์ T1 — **ไม่ปิด stage เอง ไม่ tag เอง**

## วลีสั่งรันที่เสนอ

- `เริ่ม N3 session` = อนุมัติขั้น 1–7 ตามเอกสารนี้ (deploy `2b59e46c` + split + เปิดเกม ×3 + คืนสภาพ)
- (ตัวเลือกที่ต้องตัดสินในวลีสั่ง) `เริ่ม N3 session พร้อม Sp5T0` = เพิ่มการเปิด pref `Sp5T0=true`
  ระหว่าง T1 เพื่อได้ census เชิงตัวเลข 3 รอบ (band RNG) — ถ้าไม่พิมพ์ส่วนนี้ = T1 แบบ log-only
  (census จากจอ, เกณฑ์ "ไม่มี patch นอกที่ติด" ยังวัดครบ)

## rollback (ทุกจุด — ตามแผน §6)

1. **config:** `npcPolicy` ไม่มี/off = gate no-op (fail-closed) — ใช้ได้ทุกเมื่อระหว่างรอบ
2. **build:** rollback `Mods\LongYinCoop.dll`/`.pdb` จาก `.prev-6a94abb5-<ts>` (hash ตรง `6a94abb5`)
3. **เซฟ:** `save-split restore --label SP5NPC-N3-<ts>` + post-check EXACT + leftovers none
4. เกมเขียนเฉพาะ RAM ของ process (heroNum restore ท้ายรอบ) — ไม่มีไฟล์เกม/CSV/เซฟถูกแตะโดย gate

## ความเสี่ยง/UNKNOWN ที่ต้องรู้ก่อนอนุมัติ

- build `2b59e46c…` = ครั้งแรกที่ gate hooks อยู่บนเกมจริง (N2 เทสต์ offline เท่านั้น) — ความเสี่ยงหลัก
  คือ install path ตอนโหลดโมด (ผ่าน idiom เดิม, fail-closed, ถอนทั้งชุดถ้า resolve ไม่ครบ) · ถ้า ML log
  แสดง install ล้ม = หยุดรายงานก่อนเล่น
- T1 วัด "band RNG ≥3 รอบ" ได้เต็มเฉพาะเมื่อเปิด Sp5T0 (ตัวเลข) — ไม่งั้นเป็นเชิงคุณภาพ (จอ + log)
- โลกใหม่ 3 โลกบนสำเนาเซฟ — `SaveSlot*` ไม่ถูกเขียนตามข้อเท็จจริงเดิม (ไม่มี autosave) · drift
  คาดหมาย = `PlayerprefData.dat` เท่านั้น

**หยุดหลังส่งเอกสาร — ไม่ deploy ไม่ split ไม่เปิดเกม จนกว่าจะพิมพ์วลีสั่งในเซสชันเกมแยก**
