# prep — stage N5 runtime (label `SP5NPC-N5-20261031-002431`) · 2026-10-31 00:24

มติผู้ใช้ 2026-10-31 (วลีสั่ง "เริ่ม N5 session" + มติครบ 10 ข้อ) — ทำตามคำขอ N5 ขั้น 1–6
(ขั้น 6 = ผู้เปิดเกม = ผู้ใช้ · เซสชันเกม 2 ครั้งแรก เพิ่มได้ถ้า T6 ไม่ถึงเกณฑ์)

## session-start (read-only) — ตรงหมด

- เกมไม่รัน · hash เกมตรง VERSION.md (`33c56def…` / `451311d4…`) · Mods DLL `6a94abb5…` + PDB
  `856d3bed…` · เซฟ `d3f7cb2fcae2` (= anchor ใหม่ `20261031-001811`) · mod repo HEAD `fb831fe`
  · tree = ของ session ขนานเท่านั้น (`M context.txt` · `M decisions-pending.md` ·
  `?? evidence/AUTO-20261030-071500/`) — ไม่แตะ

## ขั้น 3 — deploy artifact (ตามมติ: ใช้ `c58434b1…` แทน `476f390e…`)

| ไฟล์ | ก่อน | หลัง |
|---|---|---|
| `Mods\LongYinCoop.dll` | `6a94abb5…` | **`c58434b1…`** (จาก evidence/SP5NPC-N5T7prep-20261030-234847/) |
| `Mods\LongYinCoop.pdb` | `856d3bed…` | **`7d7f291e…`** |
| rollback point | `LongYinCoop.dll.prev-6a94abb5-20261031-002431` = `6a94abb5…` · `LongYinCoop.pdb.prev-856d3bed-20261031-002431` = `856d3bed…` | |

## ขั้น 4 — config

- `net.cfg` → backup `net.cfg.bak-20261031-002431` = **`12541f34…` (เป้า restore ท้ายเซสชัน)** ·
  แก้ด้วย sed จุดเดียว + append 1 บรรทัด: `enabled = true` · `npcPolicy = light` · ที่เหลือคงเดิม
  (heartbeatMs=1000 · loopback · secret ไม่แตะ) · hash ใหม่ `d1242aa26b2e0112…`
- `MelonPreferences.cfg` → backup `.bak-20261031-002431` = `733d9a0c…` · `Sp5T0 = true` (sed บรรทัดเดียว)
  · hash ใหม่ **`5a9d4280f2dd8590e6f5498dd45a6562eb2bbbd43c634b41fccd5a2e6505a519`** = ตรงรูป cfg
  สมัย N4 รอบ 2–3 ที่ Sp5T0=true เป๊ะ (consistency ดี) · semantic ยืนยันหลังแก้:
  `Sp5T0=true · TimeV2a=false · AllowRemoteInput=false`

## ขั้น 5 — save-split (baseline ใหม่ตามมติ)

- `split --label SP5NPC-N5-20261031-002431 --expected-baseline coop-test/baselines/20261031-001811/savehash.real.before.txt`
- **`Save vs baseline: changed=0 missing=0 added=0`** (ยืนยัน rebaseline เมื่อค่ำนี้ถูกต้อง) ·
  real aside `Save.real-SP5NPC-N5-20261031-002431` (57 ไฟล์ `d3f7cb2fcae2` · ไม่ถูกแตะ) ·
  สำเนา mount เป็น `Save` (57 ไฟล์ EXACT)
- **`PREFLIGHT-LAUNCH READY`** (`preflight-launch --label …` — 7 OK: manifest/aside/copy-baseline/
  mounted-is-copy/real-untouched)

## กลไก probe ระหว่างเดินยาว (ตรวจจากโค้ดก่อน deploy — RUNTIME ยังต้องยืนยันตอนเปิดเกม)

- worldcount (มี T7 field ครบ) เกิดที่ boundary: session.begin / sng.begin / sng.end /
  genhero.begin/end / genenemy.begin/end (Sp5T0Probe.cs:288–466)
- settle: 10 นาทีหลัง sng.end → CloseSession("settle") + Uninstall (Sp5T0Probe.cs:338–342)
- **re-arm:** CoopMod.cs:180–183 poll `pref != Installed → Apply` — หลัง uninstall probe เปิด
  session ใหม่ทันที (Sp5T0 ยัง true) ⇒ `session.begin` worldcount กลางการเดิน (โลกโหลดอยู่ =
  census + T7 จริง) และ genenemy worldcount ต่อเนื่องตลอดการเล่น จนปิดเกม/cap 50000 events —
  ตรงกับที่คำขอ N5 เขียนไว้ "probe บันทึก worldcount ตลอด" · ไฟล์ ndjson = ต่อ session (หลายไฟล์
  เมื่อ re-arm) — นับ evidence รวมทุกไฟล์ตอนวิเคราะห์
- **ข้อจำกัดที่คาดไว้:** เซสชันเกมที่ 2 (โหลดโลก — ไม่มี sng) ไม่มี worldcount ณ จุดโหลด
  (load path ไม่ถูก hook) — census ของเซสชัน 2 มาจาก genenemy ระหว่างเล่น + session.begin ของ re-arm

## เงื่อนไขหยุด (จากมติผู้ใช้ — ตรวจตอนผู้ใช้ปิดเกมแต่ละครั้ง)

hash เกม/Mods/เซฟไม่ตรง snapshot · gate install ล้ม · patch นอกชุด · SaveSlot* บนเซฟจริงถูกเขียน
(เป็นไปไม่ได้เชิงกลไก — real ถูก rename aside) · crash/soft-lock · dangling-ID error ·
dictH≠list count / heroID ชน / leader หาย (T7 บนเกมจริงครั้งแรก) · typed read ล้ม ·
การแก้จาก session อื่นที่ไม่ได้ระบุ

## ท้าย stage (เมื่อ T5–T7 ครบตามมติ) — ยังไม่ทำ

restore เซฟ EXACT + rollback DLL กลับ `6a94abb5…` (+PDB `856d3bed…`) + net.cfg คืน `12541f34…` เป๊ะ
+ Sp5T0=false · **ไม่ปิด stage ไม่ tag ไม่ push ไม่ N6/Mid/Strong**
