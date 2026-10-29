# Preflight ขั้น A (read-only) — SP5-T0 run session `SP5T0-run-20261030-012502`

> แผน `plans/sp5-t0-plan.md` rev 13 §2.6 ขั้น A · วลีสั่งผู้ใช้ 2026-10-30 (มติ §4 ข้อ 2–8) ·
> ทำกับเกมปิด · ไม่เขียน/ไม่ย้าย/ไม่แก้ไฟล์ใด ๆ ในขั้นนี้ (ไฟล์เดียวที่สร้าง = เอกสารนี้ใน evidence)

## ผลรายข้อ

| ข้อ §2.6.A | ผลที่วัด | สรุป |
|---|---|---|
| A1 hash เกม | `GameAssembly.dll` = `33c56def4102844a1190eec963a05c834cfcf992d42b423d485ce8584d7920c1` (33,669,120 B) · `global-metadata.dat` = `451311d4dc8c1b8b5073905c8b39a9781bd7e1f44ed88fbf7b35f5345c18bfd0` (7,960,432 B) — **ตรง VERSION.md เป๊ะ** | PASS |
| A1 เกมไม่รัน | `tasklist //FI "IMAGENAME eq LongYinLiZhiZhuan.exe"` = no tasks + PowerShell path-check ไม่พบ process จากโฟลเดอร์เกม | PASS |
| A2 save status (read-only) | `save-split status` = `Save` **57 ไฟล์ digest `df8e5a11beed`** · `leftovers: none` · ไม่มี `Save.real-*`/`Save.used-*` ค้าง · mounted EXACT ทุก label ที่เคย split บน baseline `20261028-071340` (GO-3 + V2A ทั้ง 9 รอบ) | PASS |
| A3 UserData baseline | `MelonPreferences.cfg` sha256 `976ccfb9a6e6ff5be9718229b25af3e4b2bd765edeae419bc6fa0cd0ba39d325` (3,260 B · mtime 2026-10-29 04:33:19) — key `Sp5T0 = false` อยู่ใน `[LongYinCoop]` แล้ว (default ตาม CoopPrefs) · `net.cfg` sha256 `12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8` (444 B · mtime 2026-10-29 01:09:06 · `enabled=false`) — **T0 ไม่ใช้ net.cfg, enabled=false ตรงคาดหมาย** | PASS |
| A4 ตาราง volatile (เกณฑ์ตัดสินท้ายรอบ) | เขียนได้โดยเกม/ML: `PlayerprefData.dat` (volatile ทุกเปิด/ปิด ⇒ drift ปกติ rebaseline volatile-only ห้าม restore), `MelonLoader\Logs\*`, `UserData\MelonPreferences.cfg` (ML เขียนตอนออกเมื่อ pref dirty), `UserData\SP5T0\<ts>\` (ปลายทางของ probe เอง — log-only) · **`SaveSlot*` = ไม่ถูกเขียนตอนเปิด/ปิด** (ยืนยันจาก incident ×3 เดิม) — ผิดปกติ = หยุดรายงาน | บันทึกแล้ว |
| A5 ปลายทางเขียนโมดอื่นปิด | `UserData\LongYinCoop\S2\s2-snapshots.ndjson` mtime **2026-10-29 04:16:50** (ไม่โต · เกมไม่รันจึงไม่มีผู้เขียน) · ไม่มี recorder/IPC client ค้าง — python.exe ×3 ที่พบ = **MCP servers ของ agent tooling** (`il2cpp-frida-mcp --stdio` ×2, `mcp_server.py --stdio`) ไม่ใช่ตัวจับข้อมูลเกม ไม่มี `s15d_sample.py`/`viewer-client.py` · `UserData\SP5T0\` = **absent** (probe ไม่เคยรัน — รอบแรกสะอาด) | PASS |
| A6 สรุป | **PASS ครบทุกข้อ** — มีสิทธิ์เดินขั้น B ตามมติ §4 ข้อ 3 | **PASS** |

## ข้อสังเกตเพิ่ม (ไม่บล็อก)

- mod repo HEAD = `c816815` — ใหม่กว่า snapshot ของ prep session (`a998509`) หนึ่ง commit:
  **append-only เอกสารล้วน** 9 บรรทัดใน `evidence/V2A-20261029-020816/user-popup-answers-20261029.md`
  (บันทึกว่า push mandate ที่มากับ popup ไม่มีเนื้อหาคำตอบ) — **ไม่แตะ `src/`/artifact/deploy/net.cfg/เซฟ**
  จึงไม่เข้าเงื่อนไขหยุด "การแก้จาก session อื่น"; origin/master = `a998509` (commit นี้ยัง local)
- artifact ที่จะ deploy (มติ ข้อ 2): DLL `865b4a87b6ee4aa5b5ac2b20e63fb4537ab14c49210948875b93eed8a441a84a`
  (366,592 B) + PDB `60ad4546aa556ab2613525b2f193d9ed3b690e7163346541b72b14c958e51c0d` (99,572 B) —
  **rehash จากไฟล์ใน `evidence/SP5T0-prep-20261030-010247/` ตรง provenance READY เป๊ะ** (วัด 2026-10-30 01:2x)
- `Mods\LongYinCoop.dll` ปัจจุบัน = `6a94abb579e65d5e6b0af917dfed4ba6f5350ad76847190bd7263cda3077b334`
  (359,936 B · mtime 2026-10-29 04:39) — rollback source ครบ มี `.prev-6a94abb5` (359,936 B, 2026-10-29 02:25)
  + `.pdb.prev-856d3bed` (97,128 B) อยู่แล้วจากรอบ V2-A

## ขั้นตอนคืนสภาพที่จะใช้ (ประกาศล่วงหน้าตาม §2.6.B)

1. `python LongYinCoop/tools/save-split.py restore --label SP5T0-20261030-<ts>` + post-check
   (57 ไฟล์ EXACT ทุก baseline `20261028-071340`, `leftovers: none`, rehash อิสระ) — ตามโปรโตคอล §1.2
2. rollback `Mods\LongYinCoop.dll`/`.pdb` กลับ `6a94abb5…`/`856d3bed…` (มติรอบนี้: rollback DLL เป็น 6a94abb5)
3. `MelonPreferences.cfg` เทียบท้ายรอบ — ถ้า ML เขียนทับ (pref dirty ตอนออกเกม) ตรวจว่า `Sp5T0`
   ควรถูกตั้งกลับ `false` หลังรอบจบ (ผู้ใช้สั่ง rollback DLL — pref ตามสภาพหลัง ML เขียน แล้วรายงาน)
4. `net.cfg` ห้ามแตะ (ไม่เกี่ยวกับ T0)
