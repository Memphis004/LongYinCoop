# S2a-4 — แผน Guest Overlay (in-process, loopback) + WriteGuard hook + `set_pref` hardening

**สถานะ: ฉบับเสนอเพื่อ GO-1 — rev4 (รออนุมัติแผนและขอบเขต) — ยังไม่เริ่ม implementation ใด ๆ**
จัดทำ: 2026-10-26 · ฐาน: ร่าง S2a-4 rev2/rev2.1/rev2.2 + มติผู้ใช้ (§0) · repo HEAD ตอนเขียน: `f9020c2`
**แก้ไขเป็น rev3 (2026-10-26 ตามคำขอผู้ใช้ก่อนอนุมัติ GO-1):** (1) นิยามป้าย (a)(b)(c) ในตาราง W4 + กติกา "ก่อนมติ M5 ห้ามสร้างหรือติดตั้ง hook ทุกกรณี — อนุญาตเฉพาะ static check, audit และตรรกะ flag แบบ pure" (2) กำหนดป้ายผลของ E5 เมื่อ M5 ยังไม่อนุมัติ + ห้ามอ้างว่า hook ผ่าน (3) §8 เพิ่มตารางความเสี่ยงรายกลุ่ม hook 4 กลุ่ม + ตัวเลือก M5 "ไม่ hook Save/Load" และ "ไม่ hook เลย" (4) แก้หัวข้อ §4 และตารางงานให้ตรงกัน (5) ระบุว่า GO-1 ครอบเฉพาะ W0–W3, W5, W4(b)(c) เท่านั้น
**แก้ไขเป็น rev4 (2026-10-26 ตามคำขอผู้ใช้):** (1) E5 — PASS เต็มได้เฉพาะ M5-1 · M5-2/M5-3/ยังไม่อนุมัติ สูงสุด PASS-with-limitation พร้อมระบุกลุ่มที่ (a) = NOT-RUN (2) §8.6 ตรงกับ §8.2 — กลุ่ม Save/Load ห้าม throw ทุกกรณี (3) W4(b)(c) — ตรรกะ flag แบบ pure ต้องไม่อ้าง `HarmonyLib`/ชนิดเป้าหมายของ hook และไม่มี call site นอก tests + `s2_static_check.py` ตรวจ `HarmonyLib`/`Patch` ในไฟล์ใหม่ใต้ `S2/**`

อ้างอิงบนสุด: `S2-plan.md` §13 (มติรายข้อ §13.9 มีผล — การใช้ §13 แทน §1–§12 ทั้งฉบับยังรอยืนยันจากผู้ใช้) · `STAGEPLAN.md` §5/§1.1/§1.4/§1.2 · `AGENTS.md`

---

## 0. มติที่ใช้ในแผน (ล่าสุดทับก่อนหน้า)

| # | มติ (ผู้ใช้) | ผลในแผนนี้ |
|---|---|---|
| M1 | **Fixture 60 ยูนิต — แนวทาง (ข):** S2b-1 รับสถานะ **PASS-with-limitation** ได้เมื่อการเชื่อมต่อและเกณฑ์ที่วัดจริงผ่าน ณ ขนาดศึกที่ระบุ โดยระบุ limitation ชัดเจน **ห้ามอ้างว่าผ่านที่ 60 ยูนิต** | §12 |
| M2 | **Bandwidth ที่ 60 ยูนิตเป็นข้อค้างบังคับก่อนปิด S2 ทั้ง stage** — ห้ามลดเป้า 60 ยูนิต (ตามมติ Q4 §13.9) | §12.4 |
| M3 | **Toggle overlay ใช้ flat key ใน `net.cfg` ปิดเป็น default ไม่ใช้ hotkey** — พฤติกรรม reload อ้างจากหลักฐานจริงของ parser/poll | §5 |
| M4 | **Guest slot เดียวยอมรับสำหรับ S2a-4** (overlay กับ `viewer-client.py` รันพร้อมกันไม่ได้) · fan-out = scope ใหม่ ยังไม่อนุมัติ | §6 |
| M5 | **WriteGuard ยังไม่ตัดสิน** — ผู้ใช้เลือก: กลุ่มที่จะ hook (ครบ 4 กลุ่ม / **ไม่ hook Save/Load** / **ไม่ hook เลย** — §8.3) และมิติ build (§8.4) · **ก่อนมีมติเป็นลายลักษณ์: ห้ามสร้างหรือติดตั้ง hook ทุกกรณี — อนุญาตเฉพาะ (b) static check, (c) audit และตรรกะ flag แบบ pure** | §8 |
| M6 | **ไม่ต้อง rebuild `1b4c17aa` จาก `82e9093` เป็นงานแยก** · แต่ build ของ S2a-4 ต้องมาจาก **source tree ที่ commit แล้วและสะอาด** พร้อมบันทึก source commit / toolchain / build command / SHA-256 | §9 |
| เดิม | Q1/Q2/Q4/Q5/Q6 (`ba0d240`) · มติ X5 (Δmean margin 1.0 / band ±0.5) · baseline `20261025-201545` (digest `588839490e5b418ae6e20172ea7af21916c9afa3a935c99525984fc9e16a0485`) · Q3 ตอบแล้ว (ZeroTier + สอง topology) = **กำหนดขอบเขตเท่านั้น ไม่ใช่การอนุมัติ topology/implementation** | ทั่วแผน |

---

## 1. ขอบเขต

**ทำ:** guest overlay แบบ IMGUI **ในโปรเซสเดียวกับเกม host** กินข้อมูลผ่าน loopback TCP ที่ต่อกลับเข้า listener ของตัวเอง (เส้นทางเดียวกับแขกจริง) · C# guest decoder · WriteGuard ตามป้าย `S2-plan.md §13.2` ข้อ 5 — **(b) static check + (c) audit + ตรรกะ flag แบบ pure อยู่ในขอบเขต GO-1** · **(a) runtime hook ห้ามสร้าง/ติดตั้งทุกกรณีก่อนมีมติ M5 แยก (§8.3)** · ปิดช่อง `set_pref` เปิด write path ขณะ S2 active · suite เทสต์ใหม่
**ไม่ทำ:** S2b-1 (ZeroTier) · S2b-2 (สองอินสแตนซ์) · S3 · fan-out หลาย guest · เปลี่ยน protocol/wire format (`ProtoVer` คงเดิม) · ลดเป้า 60 ยูนิต · แตะ `TurnGate.cs`/`RangeCells.cs`/`.gitignore`

---

## 2. สถานะความรู้ (OBSERVED / INFER / UNKNOWN — อ้าง path:line ที่เปิดตรวจจริง)

| # | ข้อเท็จจริง | ป้าย | อ้างอิง |
|---|---|---|---|
| K2 | ไม่มีโค้ด GUI/overlay เดิมใน mod เลย (grep `OnGUI/GUI./IMGUI` = 0) | OBSERVED | grep `src/**` |
| K3/K4 | `MelonEvents.OnGUI` มีใน MelonLoader 0.7.3 (metadata) + `UnityEngine.IMGUIModule.dll` interop ถูก generate — แต่**ยังไม่เคยวัดว่า event ยิงจริงบน build นี้** | OBSERVED / INFER | `MelonLoader/net6/MelonLoader.dll`, `Il2CppAssemblies/UnityEngine.IMGUIModule.dll`, `CoopMod.cs:39` |
| K6 | in-process TCP client + handshake พิสูจน์แล้ว (`S2TcpSelfTest` ต่อกลับ listener ตัวเอง — X8 PASS) | OBSERVED | `S2/S2HostService.cs:340-352,653-672` |
| K7 | listener รับ guest **1 ตัว** (`BusyFrame` DENY "another guest is connected") | OBSERVED | `S2/S2HostService.cs:317-318,407-437` |
| K8 | `S2WriteGuard` ปัจจุบันเป็น pure function ตรวจ 6 pref — ยังไม่มี hook ใด | OBSERVED | `S2/Host/S2WriteGuard.cs:19-47` |
| K9/K10 | `set_pref` **ไม่มี gate** (`IpcServer.cs:405-437` ไม่ตรวจ `DevCommands`/`Enabled`) + S2 guard ประเมินใหม่ทุก poll ~1 s ⇒ ช่องเวลา ≤ ~1 s ที่ `spike move/attack` ผ่านได้หลังเปิด `AllowRemoteInput` | OBSERVED (code) / INFER (ช่วงเวลา) | `IpcServer.cs:207,405-437,344,355,381`, `S2/S2NetConfig.cs:154-161`, `S2/S2HostService.cs:206-238` |
| K11 | เครื่องมือวัด frame มีพร้อม (`S2FrameMeter` + marker `s2.metrics` ทุก 60 s) | OBSERVED | `S2/S2HostService.cs:686-731` |
| K13 | ฝั่ง C# ยังไม่มี guest-side snapshot decoder (host encode มี; ตัว decode เต็มอยู่ที่ `viewer-client.py`) | OBSERVED (ระดับโครงไฟล์) | `S2/Protocol/CompactCodec.cs`, `tools/viewer-client.py` |
| K15 | แหล่งศึก 60 ยูนิตไม่มีหลักฐานว่ามี/พร้อม (วัดจริงล่าสุด 9 ยูนิต) | UNKNOWN | `S2-plan.md` F20, มติ Q4 |
| K16 | เกมอนุญาตให้ host เซฟระหว่าง S2 session หรือไม่ — ไม่เคยวัด | UNKNOWN | — |
| K19 | frame cap **1100 B บังคับที่ encode และ read** + chunk/reassembly มีเทสต์ | OBSERVED | `S2/Protocol/FrameCodec.cs:46-60`, `tests/…/S2a3Tests.cs:191,258,1258` |
| K22 | ชื่อ IPC pipe = `longyin-coop-<ProcessId>` (เอกลักษณ์ต่อ instance) | OBSERVED | `IpcServer.cs:28` |
| K23 | โค้ด mod มี**เฉพาะ host role**; `role=guest` = idle; `net.cfg` ไม่มี key ปลายทางเชื่อมต่อ | OBSERVED | `S2/S2HostService.cs:208,253`, `S2/S2NetConfig.cs:120-135` |
| K25 | `UserData\MelonPreferences.cfg` (ของ ML): เขียนใหม่ตอนออกเกม · `Save\PlayerprefData.dat` (ของเกม): เปลี่ยนทุกครั้งเปิด/ปิดที่เคยสังเกต (D10 control test 2/2 + incident ทุกครั้ง + rebaseline `volatile_only` เสมอ) · **พฤติกรรมสองอินสแตนซ์ = UNKNOWN** | OBSERVED (single-instance) / UNKNOWN (สองอินสแตนซ์) | `STAGEPLAN.md:565` (D10), `S2NetConfig.cs:97-102`, `coop-test/baselines/20261025-201545/AUDIT.json` |
| K26 | สองอินสแตนซ์บน install เดียวแชร์ `UserData\` ทั้งก้อน (net.cfg/MelonPreferences/log) — ไม่มีกลไกแยกในโค้ด | OBSERVED (โครงไฟล์) / INFER (ผลกระทบ) | `CoopMod.cs:64`, `CoopPaths.cs` |
| K27 | Steam กับสองโปรเซส/บัญชีเดียว — ไม่มีหลักฐาน · เป็นเขตห้ามแตะ (AGENTS ข้อ 5) | UNKNOWN | `STAGEPLAN.md` §10 ข้อ 6–7 |
| K28 | ค่า MTU จริงของ ZeroTier interface ของผู้ใช้ — ยังไม่เคยวัด | UNKNOWN | — |
| K30 | **parser/poll ของ net.cfg (ฐานของ §5):** `Poll` อ่านไฟล์ใหม่ทุกรอบ ≥1000 ms เสมอ (ไม่ใช่เฉพาะไฟล์เปลี่ยน) · key ที่ไม่รู้จักถูก**ข้าม** (build เก่าอ่านไฟล์ที่มี key ใหม่ได้ปลอดภัย) · `IsTrue` รับ `true/1/yes/on` | OBSERVED | `S2/S2NetConfig.cs:140,154-161,276,350-355`, `S2/S2HostService.cs:106-109` |

---

## 3. โครงสร้างเกต (แยกชัดเจน — อนุมัติแต่ละเกตเป็นคนละเรื่อง)

**GO-1 — อนุมัติแผน/ขอบเขต เพื่อเริ่ม implementation — ครอบเฉพาะ W0–W3, W5 และ W4(b)(c) เท่านั้น (ไม่รวม W4(a) runtime hook)**
ต้องมี: อนุมัติแผนฉบับนี้เป็นลายลักษณ์ · คำตอบ 2 คำถามที่กำหนดรูปโค้ด: (1) กลไก toggle = **มีมติแล้ว (M3)** (2) ยอมรับ guest slot เดียว = **มีมติแล้ว (M4)** ⇒ คงค้างเพียง **M5 (จะ hook กลุ่มใด + มิติ build — §8.3/§8.4)** ซึ่งเป็นเงื่อนไขเริ่ม **W4(a) เท่านั้น** — ไม่บล็อก W0–W3, W5, W4(b)(c) · ถ้าถึง GO-3 โดย M5 ยังไม่มีมติ = ทำ runtime แบบไม่มี hook และ E5(a) = NOT-RUN ตาม §11
**ห้าม:** ไม่ต้องมีผลเทสต์ W1–W5 ก่อนเริ่ม (เป็นผลที่ต้องได้จาก implementation เอง) · อนุมัติแผน **ไม่ใช่** อนุมัติ build/deploy

**PASS-2 — offline verification หลัง W1–W5**
หลักฐาน: T0–T4 ทั้งชุด (+T2h เมื่อ M5 อนุมัติและทำ W4(a)) + **regression 466+53** เขียวครบ + ผล static check (b) + diff รีวิว (ไม่มี call site เมธอดเปลี่ยน-state ใน `src/LongYinCoop/S2/**` และ — ถ้า M5 ยังไม่มีมติ — **ไม่มีไฟล์/call ติดตั้ง hook ใหม่แม้แต่อันเดียว**) · ข้อใดแดง = แก้แล้วรันซ้ำในวง offline · PASS-2 เป็น **prerequisite ของ GO-3** เท่านั้น

**GO-3 — build/deploy/runtime (อนุมัติแยก)**
ต้องมี: อนุมัติ build/deploy เป็นลายลักษณ์ (แยกจาก GO-1 เสมอ) · PASS-2 ผ่าน · **provenance ครบตาม §9** · preflight: hash เกมตรง `VERSION.md` + เกมไม่รัน + `split --expected-baseline coop-test/baselines/20261025-201545/savehash.real.before.txt` + preflight READY · runtime เริ่มที่เกตย่อย **G0** (ตรวจ `MelonEvents.OnGUI` ยิงจริง — ไม่ยิง = หยุดรายงาน ไม่เดาทางแก้ก่อนอนุมัติ) แล้ววัด E1–E8 (§11)

มติที่ใช้สืบต่อทุกเกตโดยไม่ต้องขอใหม่: Q1/Q2/Q4/Q5/Q6 · มติ X5 · baseline `20261025-201545` · Q3 (เฉพาะขอบเขต) · **Gate 2 = PASS ไม่ใช่การอนุมัติ S2a-4** (§14)

---

## 4. งาน W0–W5 + GO-3 (เรียงตาม dependency — W4 แยกเป็น W4(b)(c) และ W4(a) ตามป้าย `S2-plan.md §13.2` ข้อ 5)

| ขั้น | งาน | เปิดเกม? | ไฟล์ที่คาดว่าเปลี่ยน | เกณฑ์จบขั้น |
|---|---|---|---|---|
| W0 | track แผนนี้เข้า git (รวมกับ commit เอกสารถัดไปตามคำสั่ง) + โครง suite | ไม่ | `tests/S2Protocol.Tests/S2a4Tests.cs` (ใหม่) | suite รันได้ ผ่านทุกอันที่มี |
| W1 | `S2GuestDecoder` — decode payload `CompactCodec` → display struct (pure) | ไม่ | `src/LongYinCoop/S2/Guest/S2GuestDecoder.cs` (ใหม่) | T0 vectors ผ่าน 100% |
| W2 | `S2OverlayClient` — in-process TCP client (แพตเทิร์น `S2TcpSelfTest`) + handshake + PING ตามรอบ | ไม่ | `S2/Guest/S2OverlayClient.cs` (ใหม่) | T1 ผ่าน |
| W3 | `S2OverlayService` — subscribe `MelonEvents.OnGUI` + render จาก buffer ล่าสุด (smoothing 100 ms, `extrapolate=false` ตาม STAGEPLAN §5) · string ทั้งหมดจาก `UIStrings` · **ไม่มี interactive control** (วาด Label/Box เท่านั้น — ไม่กิน input) | ไม่ | `S2/Guest/S2OverlayService.cs` (ใหม่), `UIStrings.cs`, `CoopMod.cs` | T3 ผ่าน |
| **W4(b)(c)** | WriteGuard **ส่วนที่อนุญาตก่อนมีมติ M5** — นิยามป้าย (ตาม `S2-plan.md §13.2` ข้อ 5): **(a) runtime hook** = การสร้างโค้ด hook และ/หรือการติดตั้ง Harmony patch ที่จุด hook 4 กลุ่ม (§8.1) — **ก่อนมีมติ M5 ห้ามทั้งสร้างและติดตั้ง ทุกกรณี แม้จะตั้งใจให้เป็น no-op/ปิดอยู่** · **(b) static check** = `tools/s2_static_check.py` สแกน call sites ของเมธอดเปลี่ยน-state ใน `src/LongYinCoop/S2/**` **+ ตรวจว่าไฟล์ใหม่ทุกไฟล์ใต้ `src/LongYinCoop/S2/**` ไม่มี reference `HarmonyLib`/`Patch` ใด ๆ จนกว่า M5 จะอนุมัติ** · **(c) audit** = หลักฐาน "ไม่มี write" จากกลไก audit/`coopctl` ที่มีอยู่แล้ว (วัดจริงที่ E5 — ไม่มีงานโค้ดใหม่) · งานของขั้นนี้ = (b) + จัดเตรียม (c) + **ตรรกะ flag แบบ pure** (decision function ของ guard เป็น pure function + T2 — ไม่มีการ patch อะไรทั้งสิ้น · **ข้อกำหนด: ต้องไม่อ้าง `HarmonyLib` และไม่อ้างชนิดเป้าหมายของ hook (`MoveCommand`/`AttackCommand`/`BattleController`/`GameDataController`) และต้องไม่ถูกเรียกจากโค้ดทำงานจริง — มี call site ได้เฉพาะใน tests จนกว่า W4(a) จะเริ่ม**) | ไม่ | `tools/s2_static_check.py` (ใหม่), `S2/Host/S2WriteGuard.cs` (แตะน้อย), `tests/S2Protocol.Tests/S2a4Tests.cs` | T2 ผ่าน + static check จับ fake violation ได้ (ทั้ง call site ปลอมและ `HarmonyLib`/`Patch` ปลอมในไฟล์ใหม่ใต้ `S2/**`) + **diff ไม่มีไฟล์ hook ใหม่/call ติดตั้ง patch ใหม่ และ pure flag logic ไม่มี call site นอก tests** |
| **W4(a)** | WriteGuard **runtime hook** — สร้าง + ติดตั้ง Harmony patch **เฉพาะกลุ่มที่ M5 เลือก** (§8.3) ตามพฤติกรรม §8.5–§8.7 · **ห้ามเริ่มจนกว่า M5 จะมีมติเป็นลายลักษณ์** — ตัวเลือก "ไม่ hook เลย" (M5-3) = ขั้นนี้ NOT-RUN ถาวรของ stage | ไม่ | `S2/Host/S2WriteGuardHooks.cs` (ใหม่ — สร้างหลังอนุมัติเท่านั้น) | T2h ผ่าน (เมื่อทำ) |
| W5 | `set_pref` gate: ปฏิเสธการ**เปิด** write-path pref ขณะ `_s2.Active==true` (ปิดยังอนุญาต) + wire ref `_s2` เข้า `IpcServer` | ไม่ | `IpcServer.cs`, `CoopMod.cs` | T4 ผ่าน |
| GO-3 | build (provenance §9) → deploy (backup `.prev-1b4c17aa`) → G0 → วัด E1–E8 | **ใช่** | — | §11 + save protocol ครบ |

**Regression:** suite 466+53 เดิมต้องผ่านครบทุกครั้งที่รัน (ทุก W)

---

## 5. Toggle overlay — ดีไซน์ตาม M3 (อิงหลักฐาน K30)

- เพิ่ม **flat key `overlay`** ใน `UserData\LongYinCoop\net.cfg` · **default `false`** · ค่าที่รับ = `true/1/yes/on` (`IsTrue`) · **ไม่มี hotkey** (M3)
- **พฤติกรรม reload จากหลักฐานจริง:** ใช้กลไก poll เดิมของ `S2NetConfig` — `Poll` อ่านไฟล์ใหม่ทุก ~1 s เสมอ (K30) และ `ApplySettings` ถูกเรียกทุกรอบที่ poll กลับมา ⇒ การเปลี่ยนค่า `overlay` มีผลภายใน ≤ ~1 s **โดยไม่ต้องรีสตาร์ตเกม** (SPEC บนกลไกที่ OBSERVED แล้ว — ยืนยันจริงที่ G0/runtime)
- **ความเข้ากันได้ย้อนหลัง:** parser ปัจจุบันข้าม key ที่ไม่รู้จัก (K30: `S2NetConfig.cs:276`) ⇒ ไฟล์ที่มี `overlay` key ถูกอ่านโดย build เก่าได้ปลอดภัย / build ใหม่อ่านไฟล์เก่า (ไม่มี key) ได้ default=false — fail-closed ทั้งสองทิศ
- เงื่อนไขการทำงาน: `overlay=true` เปิดบริการ overlay client เท่านั้น — การเชื่อมต่อจะเกิดเมื่อ listener ของ S2 active (`enabled=true role=host transport=tcp`) และ**กิน guest slot เดียว** (§6)
- ห้าม log ค่า `secret` (กติกาเดิม `S2NetConfig.Describe`)

## 6. ข้อจำกัด guest slot เดียว (M4 — ยอมรับ)

overlay in-process และ `viewer-client.py` ใช้ slot guest เดียวกัน (`S2HostService.cs:317-318`) — **รันพร้อมกันไม่ได้**: ตัวที่สองได้ `DENY busy` · ระหว่างวัด E2–E4 ใช้ overlay; การวัดที่ต้องการ `viewer-client.py` (เช่น inject-gap/stall) ทำก่อน/หลัง overlay session ไม่ซ้อน · fan-out = scope ใหม่ ต้องขออนุมัติแยก (ไม่อยู่ในแผนนี้)

## 7. `set_pref` hardening (ปิดช่อง K9/K10)

- กติกา: เมื่อ `S2HostService.Active == true` → `set_pref` ปฏิเสธคำสั่งที่ตั้ง write-path pref เป็น **true** (`allowRemoteInput`, `jitprobe`, `jitprobew2/w4/w6/w7`) · การ**ปิด** (false) ยังอนุญาตทุกสถานะ
- เหตุผล: ปัจจุบันไม่มี gate เลย (K9) และ S2 ใช้เวลา ≤ ~1 s กว่าจะ deactivate เอง (K10) — ช่องนั้น `spike move/attack` ผ่านได้
- การปฏิเสธต้อง: ตอบ error พร้อมเหตุผล + **ไม่เปลี่ยนค่า pref** + log + นับเป็น evidence (T4)
- อนึ่ง: กฎนี้ไม่ได้กันการแก้ `MelonPreferences.cfg` ด้วยมือ (นอกสโคป — บันทึกเป็นข้อจำกัด)

## 8. WriteGuard — ป้ายงาน, ความเสี่ยงรายกลุ่ม และตัวเลือก M5 (ห้ามสร้าง/ติดตั้ง hook ก่อนมีมติ M5 · ห้ามตัดสินแทนผู้ใช้)

**กติกาพื้นฐานของ stage นี้:** ก่อนมีมติ M5 เป็นลายลักษณ์ **ห้ามสร้างหรือติดตั้ง hook ทุกกรณี** — รวมการเขียนโค้ด hook ไว้ล่วงหน้าแม้ยังไม่ติดตั้ง และแม้จะออกแบบให้เป็น no-op · สิ่งที่อนุญาตมีเท่านั้น: **(b) static check**, **(c) audit ด้วยกลไกที่มีอยู่** และ**ตรรกะ flag แบบ pure** (ทั้งหมดคืองาน W4(b)(c))

**8.1 จุด hook (ตาม `S2-plan.md §13.2` ข้อ 5a):** `MoveCommand.StartMove`, `AttackCommand.StartAttack` (เมธอดของ mod — เรียกเฉพาะผ่าน `spike` ที่ถูก `AllowRemoteInput` กันอยู่แล้ว) · `BattleController.BattlePauseButtonClicked` (UI ของเกม — คลิกเท่านั้น) · `GameDataController.Save/Load` (เส้นทางเซฟของเกม)

**8.2 ตารางความเสี่ยงรายกลุ่ม hook (ฐานผลักดันการเลือก M5):**

| กลุ่ม hook | เจ้าของโค้ด / path | ความเสี่ยงเฉพาะเมื่อ hook | แตะเส้นทางเซฟ? |
|---|---|---|---|
| `MoveCommand.StartMove` | โค้ด **mod** — ทางเดียวที่ถูกเรียกคือคำสั่ง `spike` (มี `AllowRemoteInput` กันอยู่แล้ว) | ต่ำสุดของ 4 กลุ่ม — exception ใน postfix กระทบเฉพาะ path คำสั่งของ mod ไม่แตะ UI/เซฟของเกม | ไม่ |
| `AttackCommand.StartAttack` | โค้ด **mod** — คู่ขนานของ StartMove | ต่ำ (เหมือน StartMove) | ไม่ |
| `BattleController.BattlePauseButtonClicked` | โค้ด **เกม** — UI คลิกเท่านั้น | กลาง — patch โค้ดเกมบน Unity main thread · exception ตอนผู้ใช้กดปุ่ม = กระทบ UI ของเกมโดยตรง (แต่ไม่อยู่ใน flow เซฟ) | ไม่โดยตรง |
| `GameDataController.Save` / `Load` | โค้ด **เกม** — **เส้นทางเซฟ/โหลดจริงของ host** | **สูงสุด** — postfix รันอยู่ใน flow การเซฟจริง · exception ตรงนี้คือความเสี่ยงต่อความสมบูรณ์ของเซฟ (กติกาข้อ 4–5, E7) · ไม่ว่าจะเลือกตัวเลือก M5 ใด กลุ่มนี้ต้องออกแบบเป็น log+count เท่านั้น ห้าม throw | **ใช่ — โดยตรง** |

**8.3 ตัวเลือก M5 (ผู้ใช้เลือกหนึ่งตัวเลือก):**

| ตัวเลือก | กลุ่มที่ถูก hook | ผลต่อ E5(a) | ความเสี่ยงที่ยอมรับ |
|---|---|---|---|
| **M5-1 hook ครบ 4 กลุ่ม** | ทั้ง 4 กลุ่ม | วัดได้ครบทุกกลุ่ม — E5 เต็ม (3 แนว) ได้ | สูงสุด — รวม patch บนเส้นทางเซฟของเกม (แถวที่ 4 ของ §8.2) |
| **M5-2 ไม่ hook Save/Load** | StartMove + StartAttack + BattlePauseButtonClicked | วัดได้ 3 กลุ่ม — Save/Load = NOT-RUN (ระบุในผล E5) ⇒ E5 สูงสุด = PASS-with-limitation | ตัดกลุ่มเสี่ยงสูงสุด (เส้นทางเซฟ) ออก — เหลือการ patch โค้ดเกมเฉพาะปุ่ม pause |
| **M5-3 ไม่ hook เลย** | ไม่มี | (a) = NOT-RUN ทั้งหมด ⇒ E5 ตัดสินด้วย (b)+(c) = PASS-with-limitation | ศูนย์ — ไม่มี patch ใหม่ · ยึด static check + audit เป็นหลักฐานตาม §13.2 |

หมายเหตุ: M5-1/M5-2 ต้องเลือก**มิติ build** ต่ออีกชั้น (§8.4) · M5-3 ไม่มี hook จึงไม่เกี่ยวกับ §8.4 · ทุกตัวเลือก — W4(a) เริ่มไม่ได้จนกว่า M5 จะมีมติเป็นลายลักษณ์

**8.4 มิติ build ของตัวเลือกที่มี hook (M5-1/M5-2 เท่านั้น):**

| | (ก) Debug-only hook | (ข) Release hook แบบ default-off |
|---|---|---|
| ความเสี่ยงต่อ build deploy | ศูนย์ (โค้ดไม่ถูก ship) | มีโค้ด hook ใน binary ที่ใช้จริง |
| การวัด E5(a) | ต้องมี session วัดแยกบน Debug build (= เปิดเกมเพิ่มอีกชุด) | วัดได้ใน session เดียวกับ E1–E8 |
| ช่องเปิดโดยไม่ตั้งใจ | ไม่มี | มีได้ถ้า pref ถูกเปิด (ผ่าน cfg ด้วยมือ) — ต้องกัน `set_pref` ไม่ให้เปิดได้ (hardcode ห้ามอยู่ใน allowlist) |

**8.5 พฤติกรรมเมื่อ hook ติดตั้งแต่ ambient flag ปิด:** จุด entry ที่ถูก hook (เฉพาะกลุ่มที่ M5 เลือก) ทำงานเฉพาะนับ counter ราย method (เขียน marker `writeguard` เป็นระยะ) — ไม่เปลี่ยนพฤติกรรมเกม · ความถี่การถูกเรียกต่ำมาก (Save/Load เฉพาะเปิด/ปิด/เซฟจริง, PauseButton เฉพาะคลิก, StartMove/StartAttack เฉพาะ path คำสั่ง mod) ⇒ ต้นทุน performance คาดว่าเชิงปริมาณต่ำ (INFER — ต้องวัดที่ E4 ร่วมกับ overlay) · M5-3 = ไม่มีส่วนนี้ทั้งหมด

**8.6 ความเสี่ยง stability/thread/exception:** hook ทั้งหมดอยู่บน Unity main thread (path เดียวกับ `BattleLogHook`/`JitProbe` ที่ใช้มาตั้งแต่ S1 — กลไก Harmony เดียวกัน) · ความเสี่ยงเฉพาะ: (i) method ถูก inline ⇒ patch ติดแต่ไม่โดนเรียก — ตรวจด้วย `PatchApplied` + hit counter (แพตเทิร์นเดิม) (ii) **throw ใน postfix ระหว่าง host เซฟ/โหลดจริง** — ตามข้อสรุปของ §8.2 แถวที่ 4: **กลุ่ม `GameDataController.Save/Load` ห้าม throw ทุกกรณี แม้ ambient flag (S2 code path กำลังทำงาน) จะตั้งอยู่** — พบ write ระหว่าง S2 active ในกลุ่มนี้ = log+count+marker `writeguard` เท่านั้น · กลุ่มที่เหลือ (StartMove/StartAttack/BattlePauseButtonClicked) จะ throw ได้หรือไม่ เป็นไปตามที่มติ M5 กำหนด (ข้อเสนอค่าเริ่มต้น = log+count ไม่ throw ทุกกลุ่ม) + ยึด (b) static check เป็นหลักฐานหลัก (ยังเป็นไปตาม §13.2: ได้ a+b ⇒ PASS-with-limitation ตามเกณฑ์เดิม)

**8.7 hook ติดตั้งไม่สำเร็จ:** `Apply()` ต่อ method มี log `PatchApplied` (แพตเทิร์นเดิม) — ตัวใดล้ม ⇒ log warn + marker `writeguard.partial` + **ไม่ throw** · E5 วัดตามจำนวนที่ติดจริง · ถอด: `Uninstall()` ตอน deinit (แบบเดียวกับ probe ที่มี)

**8.8 test evidence ที่ต้องมีก่อนพิจารณาอนุมัติ M5:** T2 (flag logic pure ครบชุด — ทำได้ก่อนมติ M5) · T2h (hook runtime — เมื่อ M5 อนุมัติและทำ W4(a)) · self-test marker ตอน activate (`writeguard.installed n=X`) · runtime session ที่ E5(a) มีหลักฐาน (ติด + ไม่ throw + counter เดิน) · ถ้าเกมอนุญาต (K16) ให้มีการกดเซฟของ host หนึ่งครั้งระหว่าง S2 active เป็นหลักฐานว่าไม่ขวาง (เกี่ยวข้องเฉพาะ M5-1 — กลุ่มเดียวที่ hook Save/Load) · ผล E4 ที่รวมต้นทุน hook

**การตัดสิน "ความเสี่ยงยอมรับได้หรือไม่" เป็นของผู้ใช้เท่านั้น** — แผนนี้นำเสนอตัวเลือกและหลักฐานที่ต้องมีเท่านั้น

## 9. Build provenance (M6)

- build ของ S2a-4 ต้องมาจาก **source tree ที่ commit แล้วและ `git status` สะอาด (ไม่มี uncommitted ใน `src/`)** ณ เวลา build
- บันทึกลง evidence ทุกครั้ง: source commit (full SHA) · toolchain (`dotnet --version`) · build command/configuration เต็ม (เช่น `dotnet build -c Release src/LongYinCoop/LongYinCoop.csproj`) · SHA-256 ของ DLL+PDB ผลลัพธ์ · ผล `cmp` byte-compare กับที่ deploy
- **ไม่ต้อง** rebuild `1b4c17aa` ย้อนจาก `82e9093` เป็นงานแยก (M6) — ความสัมพันธ์ build↔source ของ `1b4c17aa` บันทึกตามที่เป็นจริงใน `S2-plan.md §13.6`
- การอัปเดต `VERSION.md` ด้วย provenance ใหม่ = งานเอกสารแยก ต้องขออนุมัติ

## 10. Test matrix (offline — ไม่เปิดเกม)

| # | ชุด | ครอบคลุม |
|---|---|---|
| T0 | decoder vectors | snapshot ปกติ · ศพ (`hp=0`+`isAlive=false`) · synthetic 60-unit frame · edge ขอบช่วง |
| T1 | client↔fake-host | handshake ผ่าน/ผิด secret · DENY busy · resync · PING offset |
| T2 | WriteGuard ก่อน hook — **อนุญาตทำได้ก่อนมติ M5** (W4(b)(c)) | ตรรกะ flag แบบ pure: ambient flag set/clear เป็น pure function · pure logic ไม่อ้าง `HarmonyLib`/ชนิดเป้าหมายของ hook และไม่มี call site นอก tests · static check จับ fixture ที่มี call site ปลอมได้ และจับ `HarmonyLib`/`Patch` ปลอมในไฟล์ใหม่ใต้ `S2/**` ได้ และชุดสะอาด = 0 |
| T2h | WriteGuard hook runtime — **หลังมติ M5 เท่านั้น** (W4(a)) | installed-but-off ไม่เปลี่ยนพฤติกรรมเกม · throw-condition ตามที่มติ M5 กำหนด (ข้อเสนอค่าเริ่มต้น = log+count ไม่ throw ตาม §8.6) · partial install (§8.7) |
| T3 | render model | เลือกเฟรมตามอายุ (smoothing 100 ms) · ไม่ extrapolate · overlay=false ไม่มีผล |
| T4 | `set_pref` gate | matrix (active × key × value) · parser `overlay` key + reload semantics |
| RG | regression | **466+53 เดิมผ่านครบทุกครั้ง** |

## 11. Exit criteria ของ S2a-4 (runtime ภายใต้ GO-3)

| # | เกณฑ์ | วิธีวัด | ผ่าน |
|---|---|---|---|
| E1 | โหลด + ไม่พัง | 0 ERROR/0 Exception ตลอด session + ผู้ใช้จบ 1 ศึก | ตรงเกณฑ์ |
| E2 | overlay ตรงข้อมูลจริง | เทียบ grid/unit/hp บน overlay กับ `spike status` ณ key เดียวกัน | ≥ 20/20 |
| E3 | input isolation | ผู้ใช้เล่น 1 เทิร์นเต็มระหว่าง overlay เปิด: คลิกเกมทำงานปกติ · `commandsAccepted=0` · audit เปลี่ยนเฉพาะจาก action ผู้ใช้ | ตรงเกณฑ์ |
| E4 | ต้นทุนเฟรม | Δmean overlay ON vs OFF ด้วย `S2FrameMeter`/`s2.metrics` (ศึกนิ่ง `WaitPlayerInput`, protocol v2, wall-clock+sha256 net.cfg ทุกสลับ · รวมต้นทุน hook ถ้า M5 อนุมัติและติดตั้ง — M5-3 = ต้นทุน overlay ล้วน) | \|Δmean\| ≤ 1.0 ms (band ±0.5 = INCONCLUSIVE ตามมติ X5 — รายงานตามจริง) |
| E5 | guest อ่านอย่างเดียว (§13.4 #7) | (a) hook ติดตั้งเฉพาะกลุ่มที่ M5 เลือก (§8.3) + ไม่ throw · (b) static check call sites = 0 · (c) IPC audit ไม่มี write ตลอด session | **PASS เต็มได้เฉพาะ M5-1** เมื่อครบทั้ง 3 แนว (แนวใดไม่ผ่าน = รายงานตามจริง) · **M5-2** = สูงสุด PASS-with-limitation โดย (a) ต้องระบุ NOT-RUN กลุ่ม `GameDataController.Save/Load` · **M5-3 หรือ M5 ยังไม่อนุมัติ** = สูงสุด PASS-with-limitation โดย (a) = NOT-RUN **ทั้ง 4 กลุ่ม** (ระบุเหตุ: มติไม่ hook / รอมติ M5) · ทุกกรณีที่ (a) ไม่ครบทุกกลุ่ม — **ห้ามอ้างว่า "hook ผ่าน" หรือ "write path ถูก hook ตรวจแล้ว" ในรายงานทุกฉบับ** (ตาม §13.2: ได้แค่ a+b ⇒ PASS-with-limitation) |
| E6 | `set_pref` hardening | ขณะ active: `allowRemoteInput=true` → ปฏิเสธ + S2 ยัง active + `spike move` ยัง DISARMED · ขณะ idle: พฤติกรรมเดิมทุกประการ | ครบสองฝั่ง |
| E7 | เซฟปลอดภัย | `SaveSlot*` เปลี่ยน 0 · digest ตรง baseline `58883949…` (ยกเว้น volatile) · `leftovers: none` · restore EXACT | ตรงเกณฑ์ |
| E8 | สเกล (มติ Q4) | บันทึก unit count จริงทุกหน้าต่างวัด — ไม่มี fixture 60 = บันทึกเป็นข้อจำกัดของ S2a-4 (ไม่ใช่ของ S2 ทั้ง stage) | บันทึกตามจริง |

**หยุดทันทีและรายงาน (NO-GO ระหว่าง runtime):** G0 ไม่ยิง · input isolation พัง · E4 เกิน margin หลัง optimize 1 รอบ · เกม crash / เทิร์นค้าง / เซฟ hash เปลี่ยน

## 12. S2b-1 (ZeroTier สองเครื่อง) — เงื่อนไขตาม M1/M2 (แยกสถานะไม่ให้ปะปน)

**GO (เริ่มงาน S2b-1) ต้องมี:** S2a-4 ปิดสมบูรณ์ · **อนุมัติแยกเป็นลายลักษณ์**: `allowNonLoopback=true` + bind เฉพาะ IP ของ ZeroTier interface (เงื่อนไข `BindPolicy.cs:20`, §13.2-6) · เครือข่าย ZT พร้อม · ข้อตกลง firewall ขาเข้า (ตั้ง rule = เปลี่ยน system state ต้องอนุมัติแยก) · เครื่อง guest เกมเวอร์ชันเดียวกัน (fieldHash)

**สถานะผล (แยกกันชัดเจน):**
- **BLOCKED** = prerequisite ของการวัดยังขาด (เช่น ZT ไม่พร้อม, ไม่มีเครื่อง guest, **ไม่มีบันทึกค่า MTU 4a**) — ไม่ใช่ความล้มเหลวของระบบ
- **PASS-with-limitation (ตาม M1)** = การเชื่อมต่อ + เกณฑ์ที่วัดจริงผ่าน **ณ ขนาดศึกที่ระบุ** (บันทึก unit count จริงทุกผล) โดยรายงาน limitation ว่า "ยังไม่ได้วัดที่ 60 ยูนิต" — **ห้ามอ้างว่าผ่านที่ 60 ยูนิต**
- **FAIL/NO-GO** = วัดแล้วไม่ผ่านเกณฑ์ (รายงานตามจริง ไม่ปรับเกณฑ์เอง)

**เกณฑ์ที่ต้องวัด (ทุกข้อมีหลักฐานแยกไฟล์):**
1. **ขนาดศึกจริง** บันทึกในทุกผลทดสอบ (M1/D)
2. **p95 ≤ 150 ms ครบ 3 รอบ — ทุกรอบต้องผ่าน** (host-apply → guest-store, PING-offset ประมาณ skew — §13.4 #2) · รายงานรายรอบ
3. **bandwidth ณ จำนวนยูนิตจริง** เฉลี่ยทั้งศึก 5 นาที (§13.4 #3) — รายงาน bytes/s ที่ N ยูนิตจริง **ไม่อ้างแทน 60** · **ข้อค้างบังคับก่อนปิด S2 ทั้ง stage: ต้องได้วัดที่ 60 ยูนิตจริง (M2, มติ Q4 — ห้ามลดเป้า)**
4a. **MTU ของ ZT interface = หลักฐานเชิงบริบท** — วัดและบันทึกค่า + วิธีวัด · ไม่ใช้ตัดสินผ่าน/ไม่ผ่านพียงลำพัง · ค่า < 1100 B = บันทึกเป็น**ข้อสังเกตความเสี่ยง fragmentation** แล้วตีความจากผล p95/bandwidth จริงเท่านั้น (**ห้ามสรุปว่า protocol ใช้ไม่ได้โดยอัตโนมัติ** — transport เป็น TCP: OBSERVED `S2/Transport/TcpTransport.cs` — เฟรมแอปไม่ต้องพอดี IP packet เดียว; ผลกระทบจริงคือ overhead/latency: INFER) · **ไม่มีค่าวัดเลย = BLOCKED (หลักฐานไม่ครบ)**
4b. **ตรวจ application frame ≤ 1100 B** — หลักฐาน: สถิติ sender ของ session จริงบน ZT + unit tests cap (`S2a3Tests.cs:191`, `FrameCodec.cs:50-51`) · ผ่าน = **0 เฟรมเกิน 1100 B ตลอด session** + tests เขียว · พบเฟรมเกิน = FAIL (บั๊ก protocol) — แยกอิสระจาก 4a
5. **negative บนเครื่องจริง:** type ต้องห้ามจาก guest ผ่าน ZT → drop + `commandsAccepted=0` · secret ผิด → `DENY ≤ 2 s` (§13.4 #9)

## 13. S2b-2 (สองอินสแตนซ์เครื่องเดียว) — เงื่อนไข

GO ต้องมี: S2b-1 ปิด · อนุมัติแยกเรื่อง **การแยก `UserData`/`net.cfg`/log ต่อ instance** (ปัจจุบันไม่มีกลไก — K26) · คำตอบ/ทางเลือกเรื่อง Steam สองโปรเซสที่ไม่แตะ Steam (K27) · อนุมัติลายลักษณ์ตาม §13.3/§13.7 · พื้นฐานความเสี่ยง: `PlayerprefData.dat`/`MelonPreferences.cfg` บน install เดียว (K25/K26 — พฤติกรรมสองอินสแตนซ์ยัง UNKNOWN ต้องวัดก่อนใช้จริง)

## 14. หลักฐาน Gate 2 และ provenance (เปิดอ่านรายงานจริงแล้ว 2026-10-26)

- **รายงาน:** `evidence/S3p-20261025-0341/neg20r/neg20r-report.md` (เปิดตรวจจริง — 100 บรรทัด)
- **เกณฑ์ PASS (STAGEPLAN §6):** ยิงคำสั่งผิดกติกา 20 แบบ → ปฏิเสธ 20/20 + stateHash ก่อน/หลังไม่เปลี่ยน
- **ผล:** **PASS 20 · NOT-RUN 0 · FAIL 0 · UNKNOWN 0** บน binary `1b4c17aa…` · ทุก PASS ครบ 4 เงื่อนไข (mod ปฏิเสธ + ไม่ applied + stateHash เหมือนเดิม + error code ตรง) · **Gate 2 = PASS (มติผู้ใช้ 2026-10-26)** · restore ทั้งสอง split EXACT ที่ `58883949…` · ไม่พบ crash/Event 1000-1001
- **แยกข้อเท็จจริงชัดเจน:** ต้นเหตุ crash เดิม (incident 2026-10-25 04:14, `UnityPlayer.dll+0x1322c1e` บน build `f2ad30cd`) **ยัง UNKNOWN** — ผล negative ทั้งหมดเป็นเคส "ถูกปฏิเสธ/เกมไม่รับคลิก" ไม่ได้พิสูจน์ accepted-attack path · ข้อจำกัดเพิ่มเติมตามรายงาน §4 (ทดสอบครั้งเดียวต่อเคส, 2 layouts, ศพเดียว 4 เคส)
- **หมายเหตุความทันสมัยของรายงาน:** รายงาน §4 เขียน "patch rangecells ยังไม่ commit (HEAD `8fe33ce`)" — เป็นจริงเมื่อเขียนรายงาน (2026-10-26 00:06) แต่ commit `82e9093` (00:18) ได้ commit patch นั้นไปแล้ว (OBSERVED git: parent/files ของ `82e9093`) — อ่านต้องคู่กัน
- **ข้อย้ำ:** Gate 2 = PASS **ไม่ใช่การอนุมัติ S2a-4** และไม่ได้อธิบาย crash root cause

## 15. Rollback + evidence checklist

**Rollback:** net.cfg `overlay=false`/`enabled=false` (เอฟเฟกต์ ≤ ~1 s ตาม K30 — ยืนยันจริงที่ runtime) → DLL กลับ `.prev-1b4c17aa` → git tag `stage-S2a-4` ตอนปิด (checkpoint ระหว่างทำตามขั้น) → เซฟ `verify`/`restore` ตาม protocol · WriteGuard hook (เฉพาะเมื่อมีมติ M5 และติดตั้งแล้ว): pref/gate ปิด = no-op, ถอดที่ deinit, ลบทิ้งด้วย `.prev-*` · M5-3 = ไม่มี hook ให้ถอด
**Evidence (`evidence/S2a4-<label>/`):** `s2a4-report.md` (วัด E1–E8 เทียบเกณฑ์ทีละข้อ + สถานะ NOT-RUN ของ E5(a) ถ้า M5 ค้าง) · `s2-snapshots.ndjson` (marker `overlay.*`, `writeguard.*`) · sampler + `blocks.txt` (A/B) · `savehash.*` + verify/restore · static check output · **build provenance ครบตาม §9** · unit count จริงทุกหน้าต่างวัด · wall-clock + sha256 net.cfg ทุกสลับ (protocol v2) · **ห้ามมีค่า secret ใดใน evidence**

## 16. UNKNOWN รวม (ณ วันจัดทำ)

fixture 60 ยูนิต (บล็อกการปิด S2 ทั้ง stage ตาม M2) · OnGUI ยิงจริงไหมบน build นี้ (G0) · ค่า MTU จริงของ ZT + ผลกระทบ fragmentation ต่อ p95 (INFER) · พฤติกรรม `PlayerprefData.dat` สองอินสแตนซ์ / จุดเขียนใน lifecycle / เนื้อหา · Steam สองโปรเซส (K27) · กลไกแยก instance (K26) · guest-side decode ใช้ซ้ำได้แค่ไหน (ยืนยัน W1) · เกมอนุญาต host เซฟระหว่าง S2 session ไหม (K16) · ต้นเหตุ crash Gate-2 (§14) · overlay frame cost รวม hook (วัดที่ E4)

## 17. สิ่งที่รออนุมัติ/คำตอบ (ห้ามเริ่มจนมีคำตอบ)

1. **GO-1:** อนุมัติแผนฉบับนี้ + ขอบเขต — **ครอบเฉพาะ W0–W3, W5, W4(b)(c) เท่านั้น** (ไม่รวม W4(a) runtime hook)
2. **M5:** เลือกตัวเลือกตาม §8.3 — **M5-1** hook ครบ 4 กลุ่ม / **M5-2** ไม่ hook Save/Load / **M5-3** ไม่ hook เลย (+ มิติ build §8.4 ถ้าเลือก M5-1/M5-2 · ข้อเสนอค่าเริ่มต้น = log+count ไม่ throw ตาม §8.6) — ต้องมีมติเป็นลายลักษณ์**ก่อนเริ่ม W4(a)** · ไม่มีมติ = W4(a) และ E5(a) = NOT-RUN (E5 สูงสุด = PASS-with-limitation ตาม §11)
3. **GO-3:** อนุมัติ build/deploy/runtime (แยกจาก GO-1)
4. (ถัดไป นอกแผนนี้) S2b-1: อนุมัติ `allowNonLoopback` + bind IP ของ ZT · S2b-2: อนุมัติแนวทางแยก instance + Steam
