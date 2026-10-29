# ขั้น B (save-split) + deploy + ตั้ง pref — session `SP5T0-run-20261030-012502`

> ลำดับตามวลีสั่งผู้ใช้ 2026-10-30: preflight ขั้น A **PASS ครบ** (`preflight-A.md`) → ขั้น B → deploy →
> ตั้ง Sp5T0=true → **หยุดรอผู้ใช้เปิดเกมเอง** · ทั้งหมดทำกับเกมปิด

## ขั้น B — save-split (มติ §4 ข้อ 3)

- คำสั่ง: `python LongYinCoop/tools/save-split.py split --label SP5T0-20261030-012502
  --expected-baseline C:\Users\memph\LongYinCoop\coop-test\baselines\20261028-071340\savehash.real.before.txt`
- ผล: **OK** — `Save vs baseline: changed=0 missing=0 added=0` (ตรง baseline ที่อนุมัติเป๊ะ) ·
  `rename Save -> Save.real-SP5T0-20261030-012502` · copy mount กลับ · `copy vs real: 0/0/0` ·
  **57 ไฟล์** · sentinel_slot 8 (default ตามมติ ข้อ 7)
- hash ก่อน/หลัง: `savehash.real.before.txt` + `savehash.copy.before.txt` (tool เขียน, สำเนาเก็บใน
  evidence โฟลเดอร์นี้แล้ว) · MANIFEST.json + history.ndjson อยู่ที่ `coop-test/SP5T0-20261030-012502/`
- **ขั้นตอนคืนสภาพ:** `restore --label SP5T0-20261030-012502` + post-check (57 ไฟล์ EXACT ทุก
  baseline `20261028-071340`, `leftovers: none`, rehash อิสระ) — ประกาศล่วงหน้าแล้วใน `preflight-A.md`

## Deploy (มติ §4 ข้อ 2 — ทาง ก)

| รายการ | ค่า | ตรง provenance |
|---|---|---|
| สำรองก่อนทับ | `Mods\LongYinCoop.dll.prev-6a94abb5-20261030` = `6a94abb579e65d5e6b0af917dfed4ba6f5350ad76847190bd7263cda3077b334` · `Mods\LongYinCoop.pdb.prev-856d3bed-20261030` = `856d3beda727218823170f861f532509c5458eb91e51c79f07c0452d3b196035` | ✓ (hash ยืนยันหลัง copy) |
| deploy `Mods\LongYinCoop.dll` | **`865b4a87b6ee4aa5b5ac2b20e63fb4537ab14c49210948875b93eed8a441a84a`** (366,592 B) | ✓ EXACT |
| deploy `Mods\LongYinCoop.pdb` | **`60ad4546aa556ab2613525b2f193d9ed3b690e7163346541b72b14c958e51c0d`** (99,568 B บนดิสก์) | sha256 **EXACT** — หมายเหตุ: provenance.md/HANDOFF เขียน "99,572 B" แต่ไฟล์จริงใน evidence prep ก็ 99,568 B เท่ากันทุก byte กับที่ deploy (hash เป็นหลัก) — ตัวเลขขนาดในเอกสารคลาดเอง |
| ต้นทาง copy | `evidence/SP5T0-prep-20261030-010247/LongYinCoop.{dll,pdb}` (rehash ก่อน copy ตรง provenance READY) | ✓ |
| VERSION.md | เพิ่มหัวข้อ deploy 2026-10-30 (append-only 16 บรรทัด, `git diff` ยืนยัน 0 deletion) — ตามที่ prep provenance เว้นไว้ "อัปเดตเฉพาะตอน deploy จริง" | — |

## ตั้ง Sp5T0=true (มติ §4 ข้อ 2 — ก่อนเปิดเกม)

- `UserData\MelonPreferences.cfg`: `976ccfb9a6e6ff5be9718229b25af3e4b2bd765edeae419bc6fa0cd0ba39d325`
  → **`5a9d4280f2dd8590e6f5498dd45a6562eb2bbbd43c634b41fccd5a2e6505a519`** — แก้ 1 บรรทัด:
  บรรทัด 15 `Sp5T0 = false` → `Sp5T0 = true` (key อื่นทุกค่าเดิม · `AllowRemoteInput = false` คงเดิม)
- `net.cfg` `12541f34…` ไม่แตะ

## สถานะเมื่อหยุดรอ (01:2x 2026-10-30)

- เกมปิด · `Save` = สำเนาทดสอบ 57 ไฟล์ `df8e5a11beed` EXACT baseline (จริงเก็บ `Save.real-SP5T0-20261030-012502`)
- `Mods\` = build `865b4a87` + PDB `60ad4546` (probe SP5-T0 log-only, CORE H1–H5 + patch.audit 4 จุด)
- `Sp5T0 = true` · ปลายทางเขียนของ probe = `UserData\SP5T0\<ts>\sp5t0.ndjson` (จะถูกสร้างตอน `session.begin`
  ณ จุดที่ pref ถูกอ่าน — เกมเขียนเอง)
- OPTIONAL hooks H6–H11 = ไม่ติด (มติ ข้อ 5) · แผนสำรอง List.Add = ไม่อนุมัติ (ข้อ 6 — residual ≠ 0 จริง = หยุดถาม)
- ขั้นถัดไป: **ผู้ใช้เปิดเกมเอง → สร้างโลกใหม่ทันที → ไม่เปิด plot/mission/recruit ระหว่างรอบ (โปรโตคอลรอบสะอาด §2.1) →
  เล่นจบ creation round + settle → ปิดเกม → agent ตรวจ ML log + sp5t0.ndjson → เก็บ evidence → restore
  เซฟ + rollback DLL `6a94abb5` → รายงานเทียบ §5/เงื่อนไขความถูกต้องของรอบ**
- คาดหมายล่วงหน้า (rev 13): `#C.ok`/E1/E2 คง UNKNOWN (ไม่มีกลไกพิสูจน์ `inventoryStability` ภายใน window) —
  ข้อมูลอื่น (heroNum, E4/E5/E6, census, patch.audit ณ จุดตรวจ) ใช้ได้ตามปกติ
