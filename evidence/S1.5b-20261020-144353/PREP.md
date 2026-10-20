# S1.5-b — สถานะการเตรียมทดสอบ (2026-10-20 14:44) · ยังไม่มีผลวัด runtime

สถานะ: **โค้ด build + deploy + split + sentinel เสร็จ · หยุดรอผู้ใช้เล่น** — ยังไม่มีคำสั่ง IL2CPP ใดถูกเรียกใน session นี้

## 1. Pre-flight (ทำก่อนแตะอะไร)

| ข้อ | ผล | หลักฐาน |
|---|---|---|
| `GameAssembly.dll` sha256 | `33c56def…7920c1` ตรง `VERSION.md` | [RUNTIME] `sha256sum` 2026-10-20 |
| `global-metadata.dat` sha256 | `451311d4…5c18bfd0` ตรง `VERSION.md` | [RUNTIME] |
| เกมปิดอยู่ | ใช่ (`tasklist` ไม่พบ `LongYinLiZhiZhuan.exe`) | [RUNTIME] |
| `Save` = ของจริง 57/57 | ใช่ digest `80e8d7364047` ตรง baseline | [RUNTIME] `save-split status` |

## 2. สิ่งที่ deploy

| ของ | ค่า |
|---|---|
| commit ที่ build | `d5bf4de` (`S1.5-b: expose battle turn state through a read-only status command`) |
| DLL ใหม่ sha256 | `e9063ed9980d285500c7e9c1e62f216a5baad58769f1e73eea62037e6a80bfb5` (rebuild 14:46 — lazy resolve ของ `PropertyInfo` บน main thread; build `4a28edf1…` ไม่เคยรัน) |
| DLL เดิม → backup | `Mods/LongYinCoop.dll.prev-8ebf86f4d2cd` (sha256 `8ebf86f4…54847de` = build S1) |
| `Mods/LongYinCoop.dll` หลัง deploy | hash ตรงกับ `bin/Release` เป๊ะ → `logs/deploy.log` |

## 3. สภาพเซฟตอนนี้ [RUNTIME]

- label: **`S1.5-20261020-144353`**
- `Save` ที่ mount = **สำเนา** (57 ไฟล์) · `Save.real-S1.5-20261020-144353` = ของจริง (rename, ไม่เคยถูกเขียน)
- `sentinel hide --slot 8` สำเร็จ → `Save/.hidden-SaveSlot8` (5 ไฟล์) · hash ตรง `sentinel.json`
- `verify` ก่อนเล่น: **PASS** — real 0/0/0 + mtime drift 0 · copy 0/0/0 (นับ sentinel กลับเป็น `SaveSlot8/`)
- log: `logs/split.log`, `logs/sentinel-hide.log`, `logs/verify-pre.log`

## 4. ขอบเขตที่ลงทะเบียนใน build นี้

- `spike` รู้จัก **เฉพาะ `status`**; `spike move/attack/pause/arm` **ไม่มีในโค้ด** → ตอบ `spike: subcommand not registered in this build: <sub>` โดยไม่แตะ IL2CPP
- gate ของ `status`: `coop.enabled` + `coop.ipc` + `coop.devCommands` (ทั้งสาม = true อยู่) — **ไม่ใช้** `coop.allowRemoteInput`
- `coop.allowRemoteInput` ถูกประกาศเป็น pref ใหม่ default **false** (จะถูกเขียนลง cfg ตอนเปิดเกมครั้งแรก)
- ไม่มี Harmony patch ใหม่: patch ที่มีคือ `AddInfoText` postfix (S1) + `GameController.Update` probe ของ S0 เท่านั้น
- ฟิลด์ที่อ่าน: `battleState`, `nowActiveState`, `nowActiveUnit`, `nowActiveUnitMoved/Attacked` (private, ผ่าน `PropertyInfo`), `battlePaused`, `BattleUnit.moved/attacked/playerControl/autoFight`, `infoCount`, `teams` → ทุกตัวเป็น typed interop ไม่มี raw offset

## 5. ขั้นตอนที่ต้องมีผู้ใช้ (ยังไม่เริ่ม)

1. เปิดเกม (ตอนนี้ `Save` เป็นสำเนาแล้ว — **ห้ามเปิดเกมก่อน split** ตามกติกาในรายงาน S1.5-a)
2. ที่หน้าโหลด: ถ่ายภาพหน้าจอ **P-a** — ต้องเห็นว่า **สล็อต 8 หายไป** (เหลือ 0–7, 9, 10)
3. **โหลดสล็อตอื่นที่ไม่ใช่ 8** เพื่อเข้าศึก (แนะนำเซฟที่มี HardFight และมียูนิตที่ผู้เล่นคุม)
   - ถ้าเซฟที่ต้องการอยู่ในสล็อต 8 เท่านั้น: **อย่าเพิ่งโหลด** — แจ้ง agent, ปิดเกม, agent จะ `sentinel unhide --slot 8` ให้แล้วเปิดเกมใหม่ (ยังเป็นสำเนาเดิม)
4. เข้าศึก HardFight แล้วแจ้ง agent — agent จะเก็บ `spike status` ต่อเนื่อง + probe ฝั่งเกม 20 เฟรม
5. **ห้ามกดบันทึกเกม** ระหว่างรอบนี้ (เขียนลงสำเนาและทำให้ภาพเซฟเปลี่ยน)

หลังปิดเกม: agent ทำ `verify` (P-b/P-c) → `sentinel unhide` → `restore` → rehash จนเหลือ `Save` ของจริงโฟลเดอร์เดียว

## 6. เกณฑ์ §2 ที่ยังต้องวัด

| # | เกณฑ์ | สถานะ |
|---|---|---|
| 1 | `spike status` ตรงกับ UI 20/20 เฟรม (active, moved/attacked, paused) + มีเฟรมที่ hash เปลี่ยนจริงและเฟรมนิ่ง hash คงเดิม | **NOT-RUN** |
| 2 | ไม่มีการเรียก method ที่เปลี่ยน state (`postfix.calls` กลุ่ม action = 0) | **NOT-RUN** (static: ไม่มี patch/call ใหม่; runtime audit จะอ่านจาก `actionPatches` ใน status) |
| 3 | main-thread cost p99 + err = 0 | **NOT-RUN** |

## 7. หมายเหตุเรื่อง `infoCount` ใน stateHash

`infoCount` เพดาน 101 (S1 [RUNTIME] — log line จริงทั้งหมดเกิดที่ 101) → ใน hash มันเป็น **ค่าคงที่** หลัง panel เต็ม
ผลต่อการตรวจ "ไม่เปลี่ยน": ไม่ทำให้ hash เปลี่ยนผิด ๆ ในช่วงนิ่ง (ซึ่งเป็นทิศที่เสี่ยงต่อ negative test) แต่ทำให้ **ช่วงต้นศึก** hash อาจเปลี่ยนเพราะ log line ใหม่ แม้ยูนิตไม่ขยับ → ต้องแยกด้วย `infoCount` ที่รายงานมาคู่กัน
ข้อจำกัด: หลัง 101 แล้ว event ที่เป็น log อย่างเดียวจะไม่ถูกสะท้อนใน hash — ยอมรับได้เพราะ hash มีไว้ตรวจ battle state ไม่ใช่ log (จะยืนยันด้วยข้อมูลจริงในขั้นที่ 5)
