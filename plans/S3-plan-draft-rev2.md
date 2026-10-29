# S3 — ร่างแผนฉบับแก้ (DRAFT rev2 · 2026-10-28 · เอกสารรีวิวเท่านั้น — ยังไม่ขอเริ่ม)

> **สถานะ:** ร่าง rev2 ที่ปรับตามสถานะปัจจุบันตามคำสั่งผู้ใช้ (Phase D ของ session `AUTO-20261028-023904`) —
> **ไม่แก้ `S3-plan-draft.md` ฉบับเดิม (2026-10-25)** · ไฟล์นี้ไม่ใช่การอนุมัติ implementation ใด ๆ ·
> กรอบบน: `STAGEPLAN.md` §6 (S3) + §1.2–1.5 · addendum §16/§17 · ทุก integration point อ้างหลักฐานวัดจริง
> · **ถ้าไฟล์นี้ขัดกับ STAGEPLAN/AGENTS ให้หยุดรายงาน ห้ามเลือกเอง**

## 0. อะไรเปลี่ยนจากฉบับเดิม (2026-10-25)

| เรื่อง | ฉบับเดิม | rev2 (สถานะ 2026-10-28) |
|---|---|---|
| Gate 2 (Runtime Prerequisite) | **BLOCKED** — negative 20 + จบศึก = NOT RUN | **PASS** (มติผู้ใช้ 2026-10-26): NEG20R **20/20** บน build `1b4c17aa` (rangecells patch commit `82e9093`) + จบศึกปกติ (`state=Loot`) + restore EXACT — รายงาน `evidence/S3p-20261025-0341/neg20r/neg20r-report.md` ⇒ **งาน W0 เดิมปิดเป็นหลักฐานแล้ว — แต่ Gate 2 = PASS ไม่ใช่การอนุมัติ S3** (§14 ของ S2a-4-plan) |
| WriteGuard | วางแผนบน hook 4 กลุ่ม | **มติ M5 = M5-3 (ไม่ hook เลยใน S2a-4)** — E5(a) = NOT-RUN ทั้ง 4 กลุ่ม ⇒ การตรวจ "guest อ่านอย่างเดียว" ของ S3 ต้องอาศัย **(b) static check + (c) audit + validator** ล้วน (ดู §6 ใหม่) · **ห้ามอ้างว่า "write path ถูก hook ตรวจแล้ว" ในเอกสาร/รายงานใดของ S3** |
| `set_pref` gate | ไม่มี | **S2a-4 เพิ่ม `S2SetPrefGate` แล้ว (W5)**: ขณะ `S2HostService.Active` ปฏิเสธการ**เปิด** write-path pref (`allowRemoteInput`, `jitprobe*`) + T4 fail-closed/TOCTOU — ผลคือช่องเวลา K10 (≤ ~1 s) ปิดแล้วในระดับเกต logic (ยืนยัน runtime ที่ E6 ของ GO-3 ซึ่งยังไม่รัน) |
| overlay | ไม่มี | S2a-4 เพิ่ม in-process overlay (`S2OverlayClient/Service`) ซึ่ง**กิน guest slot เดียว (M4)** — ศึก S3 ที่มี guest จริงต่ออยู่ ⇒ overlay กับ guest จริงใช้ slot พร้อมกันไม่ได้ (§5 ใหม่) |
| ระบบเวลา/กิจกรรม | ไม่มี | แผนเวลา (time-plan-draft) ยืนยันแล้วว่า **ปั๊มกิจกรรมเป็น singleton บนโฮสต์ + unscaled delta + state ไม่อยู่ในเซฟ** (B.9) และ protocol ไม่มี activity message ⇒ **S3 ห้ามให้ guest สั่งกิจกรรมทุกรูปแบบ** (§7 ใหม่) |
| baseline เซฟ | `4226730f66ff` (ยุค S2a-3) | **`58883949…` @ `coop-test/baselines/20261025-201545`** (จาก VERSION.md) — split ทุกครั้งต้อง `--expected-baseline` ไฟล์นี้ตรง ๆ |

## 1. เป้าหมาย / Non-goals (คงเดิมจากฉบับเดิม §1)

- **เป้าหมาย:** guest สั่งยูนิตบางตัวของฝั่งเรา; **host ตรวจ (validator) แล้วรันผ่าน method ของเกมเอง** —
  timeout → AI ทำแทนแล้วคืนสิทธิ์
- **Non-goals (ห้ามทำใน S3):** guest เขียน state เกม (= 0 ตลอด) · guest คำนวณผล/RNG/damage ·
  **plot/world-map verbs + กิจกรรม/เวลาทุกชนิด** (SP5/แผนเวลาแยกเด็ดขาด) · SP7/SP4 · แก้ B3/∞ นอกงาน
  ที่อนุมัติ · เพิ่ม feature นอก §6 ของ STAGEPLAN · ลดจำนวน NPC

## 2. สถาปัตยกรรม (คงเดิม + อัปเดตฐาน)

Host = ผู้ตัดสินเดียว · guest ส่ง "เจตนา" (CMD) · validator บน main thread ด้วย state จริง → executor เรียก
method ของเกม → เกมคำนวณทั้งหมด → host อ่านผลจริงส่ง delta กลับ
**ฐานที่มีแล้ว (อัปเดต):** `TcpTransport` loopback + Hello/Auth HMAC + Deny + seq/resync (S2a-3 X1–X9) ·
`S2GuestDecoder` + overlay client/service + `S2SetPrefGate` (S2a-4 GO-1 — **ยังไม่ผ่าน runtime GO-3**) ·
`S2WriteGuard` pure (6 pref) · `TurnGate` stateHash **v2** + `AliveRule` (unit tests 447/0 — runtime
negative ผ่านแล้วผ่าน NEG20R) · `MoveCommand`/`AttackCommand` arm ครั้งเดียว (S1.5) · panic
`allowRemoteInput` [RUNTIME S1.5g]

## 3. CommandValidator (คงเดิมจากฉบับเดิม §3 — ย่อ)

เงื่อนไขรับครบ: session ผ่าน Auth · `battleState=Fighting` + ไม่ paused + ไม่มี modal · กติกา D3
(`activeId` ตรง) · side=player · ยูนิต/target ผ่าน `AliveForS3` · phase ตรง op (move=`WaitPlayerInput`,
attack=`WaitAttacking` [RUNTIME]) · range/skill ใช้ตัวตรวจเกมก่อน (`attackComfirmed` path) · REJECT มี
reason code + stateHash ไม่เปลี่ยน · cmdId window N=256 + clientTick monotonic + deadlineMs ·
`attackRangeGridUnits`/`damageRangeGridUnits` = advisory เท่านั้น

## 4. Network command channel (คงเดิมจากฉบับเดิม §4 — ย่อ)

ต่อยอด S2 protocol: เพิ่ม `CMD`/`CMD_RESULT` ใน envelope เดิม + **bump `PROTO_VER` 2→3** ผูก fieldHash —
**การแก้ protocol ต้องผ่านการอนุมัติแยก** (อยู่ในรายการมติท้ายไฟล์) · `cmdId` idempotent · ไม่ retry อัตโนมัติ ·
host-side timeout → `TIMEOUT` + AI fallback

## 5. การทำงานร่วมกับ S2a-4 (ใหม่ — ระบุชัดตามคำสั่งผู้ใช้)

- **guest slot เดียว (M4):** ในศึกที่ guest จริงต่ออยู่ **overlay ในเครื่อง host ต่อ listener ตัวเองไม่ได้**
  (ได้ `DENY busy`) — สลับได้แต่ไม่พร้อมกัน ⇒ รอบวัด S3 (W6) ต้องเลือกว่าจะเปิด overlay หรือไม่ และ
  บันทึกสถานะ slot ทุกหน้าต่างวัด · การวัดที่ต้องใช้ `viewer-client.py` ทำก่อน/หลัง session ของ guest จริง
- **`set_pref` gate กับ panic switch:** `allowRemoteInput=false` = panic ปิดทั้งแชแนล [RUNTIME S1.5g] ·
  เกตของ S2a-4 **ปฏิเสธการเปิด `allowRemoteInput=true` ขณะ S2 active** ⇒ ถ้า panic กลาง session,
  การเปิดกลับผ่าน IPC ทำไม่ได้ (fail-closed โดยดีไซน์) — ทางเดียวคือ restart session/mod state · S3 ต้อง
  **ยอมรับพฤติกรรมนี้** และไม่ออกแบบทางเปิดกลับระหว่าง S2 active (ห้าม bypass เกต)
- **คำสั่ง S3 กับ flag:** คำสั่ง CMD ของ guest ไม่ได้ผ่าน `allowRemoteInput` (เป็น message ของ protocol S2
  ที่ auth แล้ว) — แต่ panic flag ยังปิดทั้ง listener ⇒ ยังใช้เป็นสวิตช์ดับด้านบนสุดได้ · validator ต้อง
  **ตรวจซ้ำว่า S2 active + auth ยังไม่หมด** ทุกคำสั่ง
- **write evidence ภายใต้ M5-3:** ไม่มี hook ⇒ หลักฐาน "host เขียนเท่าที่ควร" = (b) `s2_static_check.py`
  (call sites เมธอดเปลี่ยน-state ใน `S2/**` = 0 — รวมไฟล์ S3 ใหม่ด้วย) + (c) audit (stateHash v2 ก่อน/หลัง
  **ทุกคำสั่ง**, save-watch 57 ไฟล์, counters รับ/ปฏิเสธ/timeout, `commandsAccepted`) + validator เป็น pure
  rules · **ห้ามอ้าง hook** · ถ้าอนาคตจะเพิ่ม hook ต้องเปิดมติ M5 ใหม่ (อยู่นอกขอบเขต S3 rev2)

## 6. Integration points (คงเดิม — ย่อ, อ้างหลักฐานเดิม)

โจมตี = `BattleGridClicked` 1 คลิก + อ่าน `attackComfirmed` เฟรมเดียวกัน (S1.5e) · เดิน = `MoveCommand`
ลำดับเดิม (S1.5d) · ตาย/เป้า = `AliveRule.AliveForS3` + `CompletionAlive` · stateHash = `TurnGate.Build()`
v2 · **ห้าม hook log** `BattleController.get_Instance` (hot getter) · **ยังห้ามใช้:** เขียน `nowActiveState`
เอง / `ChangeActiveSkill` เอง / auto-fallback ก่อนวัด JIT

## 7. ความสัมพันธ์กับแผนเวลา (ใหม่ — ตามคำสั่งผู้ใช้)

- **กิจกรรม (WorkingUIController) ต้องไม่ถูกสั่งจาก guest ใน S3** — ทุกรูปแบบ: ไม่มี message เริ่ม/ยกเลิก/
  skip งานใน protocol (allowlist guest→host = `Auth/Ping/Resync/Bye` + CMD ที่เสนอ เฉพาะ op
  `MOVE|SKILL|TARGET|CANCEL|AUTO` ของศึกเท่านั้น — `CANCEL` หมายถึงยกเลิกคำสั่งยูนิตในศึก ไม่ใช่
  ยกเลิกกิจกรรม)
- เหตุผลจากหลักฐาน: ปั๊มกิจกรรมเป็น singleton + unscaled delta + state ไม่อยู่ในเซฟ (time-plan B.9) —
  การ serialize กิจกรรมเป็นงาน Gate 5/6 ของแผนเวลา (Option A/B/C ยังเปิด) **ไม่ใช่ของ S3** · ถ้า S3 ทำ
  ศึกขณะงานกำลังเดิน งานเดินต่อตามเกมปกติ (host-local) — guest เห็นผ่าน snapshot เท่านั้น
- `TryCancelWorking`/`MiddleStopWork` ไม่ถูกเรียกจากเส้นทาง S3 ทุกกรณี

## 8. Observability + save safety (คงเดิม — ย่อ)

Audit log ทุกคำสั่ง (pattern `s2.metrics`) · stateHash v2 ก่อน/หลังทุกคำสั่ง · save-watch 57 ไฟล์ · ทุก runtime
บนสำเนาเซฟ (§1.2) + split `--expected-baseline 20261025-201545` ตรง ๆ

## 9. Tests / rollback (คงเดิม — อัปเดตสถานะ W0)

- **Unit (ไม่เปิดเกม):** validator rule table (pure) · envelope CMD/CMD_RESULT codec + PROTO_VER 3 ·
  cmdId window/duplicate
- **Integration (in-proc):** fake-guest ต่อ TcpTransport จริง + forbidden CMD frames → DROP/REJECT
- **Runtime (บนสำเนา + ผู้ใช้เปิดเกม):** ~~negative 20~~ **เสร็จแล้ว (NEG20R 20/20 — Gate 2 PASS)** ·
  authority proof 20 แบบ · 50 คำสั่งจริง ≥45 ACCEPT · timeout/AI 20/20 · 10 นาทีร่วม 0 soft-lock
- **Rollback:** `allowRemoteInput=false` (panic) → เคลียร์ control map → `.prev-*`/`.dll.off` → git tag
  ตอนปิด (อนุมัติแยก)

## 10. ลำดับงานย่อย (ปรับจาก W0 ที่ปิดแล้ว)

| งาน | เนื้อหา | สถานะ |
|---|---|---|
| ~~W0~~ | Runtime Prerequisite (negative 20 + จบศึก) | **เสร็จ** — NEG20R 20/20 + `state=Loot` (Gate 2 PASS 2026-10-26) |
| W0.5 | มติ B3 + ∞hp (SafeUnitAlive read-status / IsInfinity + tests) | **ยังเปิด** — ต้องมติก่อน validator ใช้ `TargetAliveAfterKnown` |
| W1 | protocol v3 CMD/CMD_RESULT + codec + unit tests | รออนุมัติ protocol change |
| W2 | CommandValidator + unit table | — |
| W3 | Executor + timeout/AI fallback (ต้องวัด JIT §6-3 ก่อนใช้ auto) + audit log | — |
| W4 | control map + IPC hot reload (`UserData/CoopControl.json`) | — |
| W5 | integration in-proc | — |
| W6 | runtime รอบ S3 (authority 20 + 50 คำสั่ง + timeout 20 + 10 นาที) | deploy/runtime อนุมัติแยกทุกครั้ง |
| W7 | รายงานปิด S3 + tag | อนุมัติปิด |

## 11. UNKNOWN คงเดิม + วิธีปิด (ตามคำสั่งผู้ใช้ — ไม่ลบไม่เปลี่ยนสถานะเอง)

| ข้อ | สถานะ | วิธีเก็บหลักฐาน |
|---|---|---|
| เขียน `nowActiveState` ก่อนเรียกได้/ควรไหม | **UNKNOWN** (JIT §6-2) | enumerate enum ระหว่างเล่นจริง (อ่านอย่างเดียว) ในรอบ runtime ถัดไป |
| auto-fallback ตัวไหน scope แคบ + คืนค่าได้ 100% | **UNKNOWN** (JIT §6-3) | hook อ่าน + ทดสอบบนสำเนาใน W3 (hook นี้เป็นของ JIT — อนุมัติแยกจาก M5-3) |
| ลำดับ call ใช้ได้กับ AoE/หลายเป้า/stun | **UNKNOWN** (JIT §6-1) | runtime trace ใน W6 (เก็บต่างเป็น validator rule) |
| stableId ของศึกจริงตรงกับ plan ไหม | **UNKNOWN** | อ่าน `spike status` หลังเปิดเกมในรอบ S3 แรก (NEG20R ยังไม่ได้ยืนยันเป็นการเฉพาะ) |
| cost p99 ≤ 8 ms ต่อคำสั่ง | INFER จาก S1.5 p99 0.1047 ms + S2 X1 p95 0.277 ms | วัดจริงใน W6 |
| B3 read-status / ∞ hp | **UNKNOWN** | W0.5 + runtime สังเกต |

## 12. สิ่งที่ต้องขอมติผู้ใช้ก่อนเริ่ม S3 (รวมท้ายไฟล์ตามคำสั่ง)

1. **W0.5:** มติ B3 (SafeUnitAlive แยก read-status) + ∞ hp (IsInfinity) — แก้ `SafeUnitAlive` เป็นการแก้
   S2 source ที่ track → ต้องอนุมัติเป็นงานโค้ดแยก
2. **Protocol change:** bump `PROTO_VER` 2→3 + message `CMD`/`CMD_RESULT` + schema — แก้
   `S2/Protocol/*` ที่ track → อนุมัติแยก (อยู่นอกขอบเขต [N6] ของ session นี้)
3. **ลำดับเทียบ S2:** S2 ยังมีข้อค้างปิด stage (bandwidth ที่ 60 ยูนิต ตามมติ M2/Q4; GO-3 ของ S2a-4 ยัง
   ไม่รัน) — **S3 จะเริ่ม offline (W1–W5) ได้เมื่อไหร่ และ runtime (W6) ต้องรอ S2 ปิดก่อนหรือไม่** = มติ
   ผู้ใช้ (แผนนี้ไม่ตัดสิน)
4. **control map** `UserData/CoopControl.json` (hot reload ผ่าน IPC) — เขียนไฟล์ config ใหม่ของ mod =
   อนุมัติแยก
5. **deploy/runtime ทุกครั้ง** ของ W6 = อนุมัติแยกต่อครั้ง (เช่นเดิม) + baseline ที่ใช้ `--expected-baseline`
   ต้องเป็นตัวที่อนุมัติล่าสุด ณ วันรัน
6. **exit criteria ของ W6** (authority 20 / 50 คำสั่ง ≥45 / timeout 20/20 / 10 นาที 0 soft-lock) — ยืนยันว่า
   คงเดิมก่อนเริ่ม W6 (ห้ามปรับทีหลัง)
