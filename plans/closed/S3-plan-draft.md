# S3 — ร่างแผนฉบับเต็ม (DRAFT v1 · 2026-10-25 · ยังไม่ขอเริ่ม — Gate 3 = BLOCKED)

> สถานะ: **ร่างเพื่อรีวิวเท่านั้น** — Runtime Prerequisite ยังไม่แล้วเสร็จ (negative 20 เคส + จบศึกได้ตามปกติ = NOT RUN) ⇒ ตามกติกา Gate 3 ส่วน B **ยังห้ามเริ่ม implementation** · แผนนี้เตรียมไว้ล่วงหน้าเพื่อรีวิวพร้อมกัน
> กรอบบน: `STAGEPLAN.md` §6 (S3) + §1.2–1.5 · addendum §16/§17 · ทุก integration point อ้างหลักฐานวัดจริงเท่านั้น

## 1. เป้าหมาย / Non-goals

**เป้าหมาย (ตาม §6):** guest สั่งยูนิตบางตัวของฝั่งเรา; **host ตรวจ (validator) แล้วรันผ่าน method ของเกมเอง** — มี timeout → AI ทำแทนแล้วคืนสิทธิ์
**Non-goals (ห้ามทำใน S3):** guest เขียน state เกม (= 0 ตลอด) · guest คำนวณผล/RNG/damage · plot/world-map verbs · SP7/SP4 · แก้ B3/∞ นอกงานที่อนุมัติ · ห้ามเพิ่ม feature นอก §6

## 2. สถาปัตยกรรม Host-authoritative

- **Host = ผู้ตัดสินเดียว**: guest ส่ง "เจตนา" (CMD) เท่านั้น · host validator ตรวจบน Unity main thread ด้วย state จริงของเกม → executor เรียก method ของเกม → เกมคำนวณ/dmg/RNG ทั้งหมด → host อ่านผลจริง (cell/hp) ส่ง delta กลับ (ไม่ predict)
- **บทบาท**: Host = state reader + validator + executor + auditor · Guest = ผู้เสนอคำสั่ง + ผู้รับรายงานผล (ใช้แชแนล snapshot S2 เดิมดู state)
- **Threading (§1.4 invariant):** ทุก IL2CPP access บน main thread ผ่าน `MainThreadDispatcher` (มีแล้ว) · CMD ถูก parse บน network thread เป็น POCO ก่อนเข้าคิว
- **ฐานที่มีแล้ว (หลักฐาน):** `TcpTransport` loopback + Hello/Auth HMAC + Deny + seq/resync (S2a-3, X1–X9) · `S2WriteGuard` (guest เข็ม read-only พิสูจน์ด้วย exit6) · `TurnGate` stateHash **v2** + `AliveRule` predicate ร่วม (S3 prereq, unit tests 447/0 + Debug 0W/0E — **runtime ยัง NOT RUN**) · `MoveCommand`/`AttackCommand` + arm ครั้งเดียว (S1.5) · panic `coop.allowRemoteInput`

## 3. CommandValidator (host)

- **ข้อมูลที่เชื่อถือได้ (อ่านบน main thread เท่านั้น):** `TurnGate.Build()` (nowActiveState/battleState/paused/turnIndex/activeId/units) · `AliveRule.AliveForS3` (ธงตายเดียวที่วัดแล้ว [RUNTIME S1.5f §3]) · `attackRangeGridUnits`/`damageRangeGridUnits` = **advisory เท่านั้น** (ค้างข้ามเทิร์น [RUNTIME]) · `attackComfirmed` = transient อ่านทันทีในเฟรมคลิก [RUNTIME]
- **เงื่อนไขรับ (ทั้งหมดต้องผ่าน):** session ผ่าน Auth (S2) · `battleState=Fighting` + ไม่ paused + ไม่มี modal ค้าง · **กติกา D3**: ยูนิตถึงตาตัวเองแล้วเท่านั้น (`activeId` ตรง) · side=player (`playerControl`) · ยูนิต/target ผ่าน `AliveForS3` · phase รับคำสั่งตรง op (move=WaitPlayerInput, attack=WaitAttacking [RUNTIME]) · range/skill ใช้**ตัวตรวจของเกมเองก่อน** (`attackComfirmed` path) แล้วค่อยเสริม
- **ปฏิเสธ:** reason code ครบทุกเงื่อนไข (`NOT_YOUR_UNIT/STALE_TURN/UNIT_NOT_ALIVE/TARGET_NOT_ALIVE/WRONG_PHASE/OUT_OF_RANGE/SKILL_NOT_READY/MODAL_OPEN/…`) · ทุก REJECT ต้อง stateHash ไม่เปลี่ยน
- **Replay/duplicate:** `cmdId` unique ต่อ session (window ล่าสุด N=256) + `clientTick` monotonic ต่อ guest + `deadlineMs` → หมดอายุ = `REJECT(STALE_TURN)` · คำสั่งซ้ำ (cmdId เดิม) = `REJECT(DUPLICATE)` ไม่รันซ้ำ
- **หมายเหตุจาก Gate 1:** B3 (IsAlive read-status ไม่แยก exception) + ∞ hp — ต้องมีมติก่อนใช้ `TargetAliveAfterKnown` ตัดสินใน validator

## 4. Network command channel

- **ต่อยอด S2 protocol (ไม่ใช่แชแนลใหม่แยก):** เพิ่ม message type `CMD` (guest→host) และ `CMD_RESULT` (host→guest) ใน envelope เดิม (length-prefix + type + payload + seq) — **bump `PROTO_VER` 2→3** พร้อมลงใน fieldHash/handshake (ผูกกับ game build hash ตาม §1.1 ของ S5 frame)
- **CMD schema (ตาม §6):** `cmdId, guestId, unitStableId, op: MOVE|SKILL|TARGET|CANCEL|AUTO, args, clientTick, deadlineMs` · **CMD_RESULT:** `cmdId, ACCEPT | REJECT(reason) | TIMEOUT | APPLIED, appliedFrame, delta(cell/hp)`
- **Ordering:** ประมวลตามลำดับรับบน host (เดี่ยว-guest ใน S3) · seq ของแชแนลเดิมใช้ตรวจ gap/resync ต่อ
- **Timeout/retry:** ไม่ retry อัตโนมัติ (คำสั่งเก่าเสี่ยง STALE_TURN) — guest ทำใหม่ด้วย cmdId ใหม่ · host-side `deadlineMs` จับเอง → `TIMEOUT` + AI fallback
- **Duplicate delivery:** idempotent ที่ cmdId (ตรวจก่อน validator) · error handling: payload invalid = `REJECT(BAD_REQUEST)` + นับ counter (ต่อเนื่องจาก forbidden-frame DROP ของ S2)

## 5. Integration points กับ game API (อ้างหลักฐานวัดแล้วเท่านั้น — ไม่เดา)

| จุด | ใช้อะไร | หลักฐาน |
|---|---|---|
| โจมตี | `bc.BattleGridClicked(gridGO ของเป้า)` 1 คลิก · อ่าน `attackComfirmed` ทันทีเฟรมเดียวกัน | S1.5e chain (evidence/S1.5e-20261021-0101) |
| เดิน | ลำดับ `MoveCommand` เดิม (arm + 1 ครั้ง) | S1.5d |
| ตาย/เป้า | `AliveRule.AliveForS3` + `CompletionAlive` | S3 prereq (59276a4) + [RUNTIME S1.5f §3] |
| stateHash/สถานะ | `TurnGate.Build()` v2 | S1.5→S3-prereq |
| **ห้าม hook log** | `BattleController.get_Instance` = hot getter | [RUNTIME counter 38k+] |
| **ยังห้ามใช้** | เขียน `nowActiveState` เอง / `ChangeActiveSkill` เอง / auto-fallback (`SetAllAuto` ฯลฯ) | **JIT §6 ข้อ 2–3 — ยังไม่วัด** |

## 6. Security/authority boundary

- Guest **ไม่มี**ทางส่งผลลัพธ์: ไม่มี field ผลลัพธ์ใน CMD (มีแต่เจตนา) · RNG/damage = ของเกมล้วน · snapshot ยังเป็น read-only path เดิม (S2WriteGuard คุม)
- Deny-by-default: ทุก op ใหม่ต้องผ่าน validator + allowlist · `allowRemoteInput=false` (panic) ปิดทั้งแชแนลทันที [RUNTIME พิสูจน์ S1.5g]
- Guest ไม่ save (D5) · control map `stableId→guestId` เป็นข้อมูล host-side (`UserData/CoopControl.json` hot-reload ผ่าน IPC ตาม §6)

## 7. Observability + save safety

- Audit log ทุกคำสั่ง: `cmdId, op, verdict, appliedFrame, delta(cell/hp), rejected reason` + counters (รับ/ปฏิเสธ/หมดอายุ) — pattern เดียวกับ marker `s2.metrics`/selftest ของ S2
- ตรวจความถูกต้องโดยไม่เสี่ยงเซฟ: stateHash v2 ก่อน/หลังทุกคำสั่ง · save-watch 57 ไฟล์ก่อน/หลังทุกหน้าต่างวัด · **ทุก runtime บนสำเนาเซฟเท่านั้น** (§1.2)

## 8. Tests / rollback

- **Unit (ไม่เปิดเกม):** validator rule table (pure — ต่อยอด AliveRule/CompletionAlive pattern) · envelope CMD/CMD_RESULT codec + PROTO_VER 3 · cmdId window/duplicate
- **Integration (in-proc):** fake-guest ต่อ TcpTransport จริงแบบ S2a-3 I1–I8 + forbidden CMD frames → DROP/REJECT
- **Runtime (บนสำเนา + ผู้ใช้เปิดเกม):** (ก) **re-run negative 20 เคสบน build S3-prereq ก่อน** (ค้างอยู่ — เงื่อนไขขั้น 0) (ข) authority proof 20 แบบของ §6 (ซ้ำ base เดียวกับ negative) (ค) 50 คำสั่งจริง ≥45 ACCEPT (ง) timeout/AI 20/20 (จ) 10 นาทีร่วม 0 soft-lock
- **Rollback:** `allowRemoteInput=false` + เคลียร์ control map (คืน UI ทันที) → `.dll.off` → git tag · deploy/rollback DLL ตามขั้นตอนที่ใช้ไปใน Gate 2 (พิสูจน์แล้ว)

## 9. ลำดับงานย่อย + gates

| งาน | เนื้อหา | dependency | acceptance | gate |
|---|---|---|---|---|
| **W0** | ปิด Runtime Prerequisite: ผู้ใช้เปิดเกม → negative 20 (แม็พ ID รออนุมัติ) → จบศึกได้ตามปกติ → restore + rollback DLL หรือคงไว้ตามมติ | Gate 2 ค้าง 3.3 | 20/20 + จบศึก + save EXACT | **GO/NO-GO ของทั้ง S3** |
| W0.5 | มติ B3 + ∞hp (แก้ SafeUnitAlive แยก read-status / เพิ่ม IsInfinity + tests) — commit + Debug build | W0 (หรือก่อน W0 ถ้ามติให้แก้ก่อน) | tests + compile | อนุมัติแยก |
| W1 | protocol v3: CMD/CMD_RESULT envelope + codec + unit tests | W0+W0.5 | unit ผ่านหมด | build/deploy gate |
| W2 | CommandValidator (pure rules + เชื่อม TurnGate/AliveRule) + unit table | W1 | rule table ครบทุก reason code | — |
| W3 | Executor + timeout/AI fallback (ต้องวัด JIT §6 ข้อ 3 ก่อนใช้ auto) + audit log | W2 | JIT มีหลักฐาน + unit | — |
| W4 | control map + IPC hot reload | W2 | อ่าน/เคลียร์ได้ ไม่แตะเกมตอน parse | — |
| W5 | integration in-proc (fake guest จริงผ่าน TCP) | W1–W4 | ทุกเคสผ่าน | — |
| W6 | runtime รอบ S3 (authority 20 + 50 คำสั่ง + timeout 20 + 10 นาที) บนสำเนา | W5 + deploy อนุมัติ | exit criteria §6 ครบ | deploy/runtime อนุมัติแยกทุกครั้ง |
| W7 | รายงานปิด S3 + tag | W6 | ตาม §2 กติกา 2 | อนุมัติปิด |

## 10. ที่ยังเป็น GUESS/INFER/UNKNOWN + วิธีปิด

| ข้อ | สถานะ | วิธีเก็บหลักฐาน |
|---|---|---|
| เขียน `nowActiveState` ก่อนเรียกได้/ควรไหม | **UNKNOWN** (JIT §6-2) | enumerate enum ระหว่างเล่นจริง (อ่านอย่างเดียว) ใน W0 รอบถัดไป |
| auto-fallback ตัวไหน scope แคบ + คืนค่าได้ 100% | **UNKNOWN** (JIT §6-3) | hook อ่าน + ทดสอบบนสำเนาใน W3 |
| ลำดับ call ใช้ได้กับ AoE/หลายเป้า/stun | **UNKNOWN** (JIT §6-1) | runtime trace ใน W6 (เก็บต่างเป็น validator rule) |
| cost p99 ≤ 8 ms ต่อคำสั่ง | **INFER** จาก S1.5 p99 0.1047 ms + S2 X1 p95 0.277 ms | วัดจริงใน W6 |
| B3 read-status / ∞ hp | **UNKNOWN** (Gate 1) | W0.5 + runtime สังเกต |
| stableId ของศึกจริงตรงกับ plan ไหม | **UNKNOWN** (Gate 2 ค้าง) | อ่าน `spike status` หลังเปิดเกม (W0) |
| multi-unit turn / turn order | **UNKNOWN** | validator อ่านจากเกมเท่านั้น (§6 ความเสี่ยง) |
