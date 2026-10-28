# SP0/SP7 session-start checks — 2026-10-24 22:52–23:00 (read-only)

label ที่เตรียมใช้สำหรับ save-split: `sp0sp7-20261024-225939` (ยังไม่ split)

## 1. Game integrity (กฎ 9)

| ไฟล์ | SHA-256 ที่วัด | VERSION.md | ผล |
|---|---|---|---|
| `GameAssembly.dll` | `33c56def4102844a…8584d7920c1` | ตรง | PASS |
| `LongYinLiZhiZhuan_Data/il2cpp_data/Metadata/global-metadata.dat` | `451311d4dc8c1b8b…345c18bfd0` | ตรง | PASS |

## 2. Process check (22:52)

- `LongYinLiZhiZhuan.exe` — **not running** (tasklist; save-split status ยืนยัน `game: not running`)
- ไม่มี sampler/watcher ของโปรเจกต์ (spike_watch / s15d_sample / s1_watch / worldtime_watch ฯลฯ) ค้าง
- ที่พบ: `il2cpp-frida-mcp.exe` ×2 (MCP server ของ session นี้), python `mcp_server.py` (MCP อื่น),
  pwsh ×3 + VS Code terminal, python 35508 (ไม่มี script แนบ — ไม่ใช่ sampler ที่รู้จัก),
  powershell 34652 = codebuff watchdog (นอกโปรเจกต์, read-only temp)

## 3. Save state

- `save-split status`: `Save` = 57 ไฟล์ digest **`7332fc93eb98`** · `leftovers: none`
  (สถานะปัจจุบัน = **เซฟจริง mounted อยู่, ยังไม่ split** — ห้ามเปิดเกมจนกว่าจะ split สำเร็จ)
- mounted Save เทียบ copy-baseline ล่าสุด (`S2a3x5-20261024-0132/savehash.copy.before.txt`):
  `changed=5 missing=0 added=0`
- diff เทียบ baseline ที่อนุมัติ `coop-test/baselines/20261022-075422` (`4226730f66ff`):
  **changed=5, ตรงกับที่บันทึกไว้ใน AGENTS ทุกไฟล์**

| ไฟล์ | ประเภท | mtime |
|---|---|---|
| `PlayerprefData.dat` | volatile | 2026-10-24 22:20:15 (session เปิดเกม 22:08:57 ของผู้ใช้) |
| `SaveSlot8/Hero` | NON-volatile | 2026-10-24 02:15:47 (ผู้ใช้เซฟเอง) |
| `SaveSlot8/Info` | NON-volatile | 2026-10-24 02:15:47 |
| `SaveSlot8/Save` | NON-volatile | 2026-10-24 02:15:46 |
| `SaveSlot8/TempHero` | NON-volatile | 2026-10-24 02:15:47 |

## 4. Slot inventory (รายละเอียดเต็ม: `slot-inventory.txt`)

**ไม่มีสล็อตว่าง** — `SaveSlot0`–`SaveSlot10` ทั้ง 11 สล็อตมี 5 ไฟล์/สล็อต, 25–50 MB

| สล็อต | bytes | digest12 | newest mtime |
|---|---|---|---|
| SaveSlot0 | 47,433,866 | `84e59fedb9f9` | 2026-10-24 22:08:55 |
| SaveSlot1 | 25,206,466 | `96dd9ede7c06` | 2026-10-24 22:08:55 |
| SaveSlot10 | 48,742,249 | `bbf1afd08522` | 2026-10-24 22:08:55 |
| SaveSlot2 | 48,740,877 | `637dbe25bb99` | 2026-10-24 22:08:55 |
| SaveSlot3 | 45,996,120 | `3c6390b4fcd7` | 2026-10-24 22:08:55 |
| SaveSlot4 | 48,671,977 | `babc59481945` | 2026-10-24 22:08:55 |
| SaveSlot5 | 25,632,063 | `7821b77e5c15` | 2026-10-24 22:08:55 |
| SaveSlot6 | 47,345,741 | `7193fdadd858` | 2026-10-24 22:08:55 |
| SaveSlot7 | 50,054,944 | `eec058216369` | 2026-10-24 22:08:55 |
| SaveSlot8 | 50,252,727 | `a1edb76a21be` | 2026-10-24 22:08:55 |
| SaveSlot9 | 47,271,341 | `8cac8264b4ac` | 2026-10-24 22:08:55 |

top-level: `PlayerprefData.dat` (14,955 B) · `steam_autocloud.vdf` (52 B)

**RUNTIME observation (mtime-level, 22:08:55 = ตอนเปิดเกม):** ทุก `SaveSlot*` **ยกเว้น SaveSlot8**
มี mtime ใหม่ที่ 22:08:55 แต่ hash ตรง baseline เดิม (content ไม่เปลี่ยน) — เกม/Steam เขียนไฟล์สล็อต
ซ้ำแบบ byte-identical ตอนเปิด · SaveSlot8 ไม่ถูกแตะ (mtime คง 02:15:47) · กลไกที่แท้จริงยัง GUESS
(Steam cloud sync หรือ game rewrite) — เพิ่มในบันทึกข้อเท็จจริง ยังไม่มีผลสรุป

MelonLoader sessions วันนี้: 01:29, 02:14, 02:36, 04:49, 05:19, 22:08 (ล่าสุด; ปิด ~22:20)

## 5. Instrumentation readiness

- frida MCP: `frida_list_devices` ตอบสนอง (Local device พร้อม) — attach ได้เมื่อเกมเปิด
- `dump.cs` อยู่ที่ `<เกม>\Il2CppDumper\dump.cs` (DUMP source สำหรับ census)
- `tools/frida_event_probe.py` พร้อมใช้ (read-only, hook Explorer/Plot/Battle entry points,
  ไม่มี write, เรียกเฉพาะ pure getter `WorldData.Player()`) — เหมาะกับ §4/§5/§7 ของ SP0/SP7
- ไม่แตะ `net.cfg`/`MelonPreferences.cfg`/mod DLL (คง build `5d7b7cc5…5953`, IPC disabled)

## 6. Gate verdict

| รายการ gate §0 | ผล |
|---|---|
| เกม hash ตรง VERSION.md | PASS |
| เกมไม่รันก่อนเปิดทดสอบ | PASS |
| leftovers / สถานะ split ชัดเจน | PASS (clean, ยังไม่ split) |
| instrumentation พร้อม | PASS |
| **baseline สำหรับ split** | **BLOCKED — รอมติผู้ใช้** (drift 5 ไฟล์, 4 ไฟล์ non-volatile ⇒ split และ rebaseline volatile-only จะ REFUSED ทั้งคู่ตามดีไซน์) |
| **สล็อตว่าง + หลักฐานไม่มีข้อมูลต้องเก็บ** | **BLOCKED — ไม่มีสล็อตว่าง (11/11 มีข้อมูล)** ⇒ ต้องมีมติผู้ใช้เลือกทาง |
| `PREFLIGHT` | NOT-RUN (ต้องรอ split ก่อน) |

## 7. มติที่รอผู้ใช้

(ก) Baseline — ทาง A: อนุมัติสร้าง baseline ใหม่จากสถานะปัจจุบัน (drift 5 ไฟล์ของผู้ใช้เอง;
เขียน `coop-test/baselines/<ใหม่>/` + REBASELINE.json `volatile_only:false` + APPROVAL.md;
canonical `4226730f66ff` คงเดิมเป็นหลักอ้างอิง S2a-3) แล้ว `split --expected-baseline <ไฟล์ใหม่>` ·
ทาง B: หยุด ไม่ rebaseline (SP0/SP7 ยังไม่เริ่ม)

(ข) สล็อตทดสอบ — ทาง 1: กำหนด SaveSlot1 (เล็กสุด) เป็นสล็อตเป้าหมายบน "สำเนา" (ข้อมูลเดิมมีครบใน
`Save.real-<label>` + digest) · ทาง 2: ผู้ใช้เคลียร์สล็อตในเกมบนสำเนาผ่าน UI ก่อนเริ่ม A/B ·
ทาง 3: ระบุสล็อตที่แสดง "ว่าง" ในหน้าเกม (ถ้ามี)
