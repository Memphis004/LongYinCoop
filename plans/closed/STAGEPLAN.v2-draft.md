# STAGEPLAN — LongYinLiZhiZhuan Co-op Mod (v2)

สถานะ: อนุมัติแล้ว — กำลังเริ่ม S0
หลักการ S0: ไม่แตะ state ของเกม ไฟล์ที่เพิ่มในโฟลเดอร์เกมมีแค่ DLL ของเรา (Mods\LongYinCoop.dll) และไฟล์ config/log ของ mod เอง [v2]
ลำดับ: S0 → S1 → S1.5 → S2 → S3 → S5 → S4 → S6

## 0. เป้าหมายและขอบเขต
- เกม: LongYinLiZhiZhuan, Unity 2020.3.48f1c1, IL2CPP
- Loader: MelonLoader v0.7.3 (ติดตั้งอยู่แล้ว), HarmonyX ที่มากับ ML, runtime net6
- Mod อื่นที่มีอยู่: LongYinThai.dll, UnityExplorer (ห้ามแก้)
- โมเดล: host เป็นผู้ตัดสินเพียงคนเดียว (authoritative) guest ไม่จำลอง ไม่สุ่ม ไม่คำนวณผล
  เหตุผล: RNG ของศึกเป็น UnityEngine.Random ตัวกลางที่ไม่มี seed ของเกมเอง
- รูปแบบ co-op: guest คุมยูนิตที่อยู่ในเซฟของ host (ศิษย์/สหาย) รางวัลเข้าเซฟ host guest ห้าม save
- ป้ายหลักฐาน: RUNTIME / DUMP / GUESS / SPEC

## 1. กติการ่วม

### 1.1 ตารางสิทธิ์การเขียน
| การกระทำ | S0 | S1 | S1.5 | S2 | S3 | S5 | S4 | S6 |
|---|---|---|---|---|---|---|---|---|
| อ่าน field ผ่าน Il2CppInterop แบบมีชนิด [v2] | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| อ่านด้วย raw offset/pointer [v2] | ❌ | ขออนุมัติก่อน | ขออนุมัติก่อน | ขออนุมัติก่อน | ขออนุมัติก่อน | ขออนุมัติก่อน | ขออนุมัติก่อน | ❌ |
| เรียก getter บริสุทธิ์ (ไม่เปลี่ยน state) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Harmony patch (อ่าน argument เท่านั้น) | ✅ probe no-op | ✅ ถาวร | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| เรียก method ที่เปลี่ยน state ของเกม | ❌ | ❌ | ✅ host | ❌ | ✅ host | ✅ | ✅ | ❌ |
| เขียน field ของเกมเอง | ❌ | ❌ | ❌ | ❌ | ✅ host | ✅ | ✅ guest | ❌ |
| รับ input จากเครือข่ายแล้วรัน | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ |
| เรียก GameDataController.Save/Load | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| เขียนไฟล์ของ mod เอง (log/pref/capture) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Steam / online services | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

หมายเหตุ: offset/RVA ใน notes ใช้สำหรับสำรวจเท่านั้น ในโค้ด mod ต้อง re-resolve ผ่าน interop [v2]

### 1.2 โปรโตคอลสำเนาเซฟ
1. ปิดเกมให้สนิท
2. คัดลอกเซฟไปที่ coop-test/<timestamp>/save/
3. บันทึก sha256 ทั้งเซฟจริงและสำเนา ก่อนและหลังทดสอบ
4. ชี้ path เซฟไปที่สำเนา หรือสลับโฟลเดอร์เซฟ
5. ข้อห้าม: ห้ามทดสอบบนเซฟจริง, ห้ามสั่ง Save() ทับเซฟจริง, guest ห้ามเขียนเซฟ (บังคับด้วย pref + hook Save ให้ no-op ในโหมด guest)
6. ถ้า hash เซฟจริงเปลี่ยน ให้หยุดทุกอย่างแล้วรายงาน

### 1.3 โครงโปรเจค
- git repo แยกนอกโฟลเดอร์เกม build แล้ว copy DLL เข้า Mods\ [v2]
- .gitignore: DLL ของเกม, Il2CppAssemblies, Il2CppDumper, dump.cs ห้าม commit [v2]
- C# class library target net6.0 อ้างอิงแบบ Private=false:
  MelonLoader\net6\MelonLoader.dll, 0Harmony.dll, Il2CppInterop.Runtime.dll,
  MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll, UnityEngine.CoreModule.dll, Il2Cppmscorlib.dll
- ห้ามอ้างอิง Steamworks
- VERSION.md: SHA-256 ของ GameAssembly.dll และ global-metadata.dat เทียบทุก session ถ้าไม่ตรงให้หยุดและรายงาน [v2]
- แนะนำตั้ง Steam เป็น "Only update this game when I launch it" [v2]

### 1.4 Threading
1. ทุกการเรียก IL2CPP อยู่บน Unity main thread; network thread ห้ามแตะ object ของเกม
2. MainThreadDispatcher.Enqueue(Action) → drain ใน MelonMod.OnUpdate (budget 4 ms/frame)
3. snapshot สร้างเป็น POCO บน main thread ก่อน serialize; ห้ามถือ Il2CppObjectBase ข้ามเฟรม
4. MainThreadGuard ใน dev build: throw ถ้าเรียก API เกมนอก main thread
5. S0 ต้องวัดว่า OnUpdate กับ Harmony probe อยู่ thread เดียวกัน ถ้าไม่ ต้องแก้ข้อนี้ก่อนไป S1 [v2]

### 1.5 Rollback ทั่วไป
- ปิดทั้ง mod: เปลี่ยนชื่อ .dll → .dll.off
- ปิดฟีเจอร์: MelonPreferences (Enabled=false เป็น kill switch)
- คืนเซฟ: copy จาก coop-test/<ts>/save/
- คืนโค้ด: git tag stage-SN

## 2. S0 — ฐานราก
**เป้าหมาย:** โหลด mod เปล่าได้ มี logging, dispatcher, IPC อ่านอย่างเดียว โดยไม่แตะ state ของเกม [v2]

**Deliverables**
- LongYinCoop.dll: CoopMod : MelonMod, log แยกไฟล์, pref Enabled (ค่าเริ่ม false)
- MainThreadDispatcher: ConcurrentQueue<Action>, drain ≤ 4 ms/frame
- HarmonyProbe (ชั่วคราว): prefix no-op บน GameController.Update ใช้วัด ManagedThreadId เท่านั้น ถอดได้ด้วย pref
- IpcServer: named pipe \\.\pipe\longyin-coop-<pid> คำสั่ง ping, get_version, get_scene, get_player_pos (อ่านอย่างเดียว ผ่าน dispatcher)
- SnapshotReader (โครงเปล่า), UIStrings.cs, coopctl.py (client ทดสอบ IPC)
- VERSION.md, .gitignore [v2]

**สำรวจ just-in-time**
- thread ของ OnUpdate เทียบกับ thread ของ probe
- รายการ method ที่ LongYinThai patch ไว้ (Harmony.GetAllPatchedMethods) อ่านอย่างเดียว บันทึกลง compat-notes.md โดยเฉพาะ AddInfoText และ setter ของ Text [v2]

**Exit criteria**
1. exception จาก mod = 0 ใน 10 นาที
2. dispatcher p99 ≤ 4 ms/frame
3. hash เซฟจริงก่อน/หลังเท่ากัน
4. ถอด probe ด้วย pref แล้วไม่เหลือ patch ของเรา
5. UIStrings ใช้งานได้
6. เล่น world map ได้ปกติ และเล่นศึกจบได้ 1 ศึก [v2]
7. LongYinThai ยังแปลได้ตามเดิม [v2]
8. rename เป็น .dll.off แล้วเกมกลับเหมือนเดิม (Latest.log ไม่มีชื่อ mod เรา) [v2]
9. OnUpdate กับ probe อยู่ thread เดียวกัน (หรือรายงานถ้าไม่ตรง) [v2]

**ความเสี่ยง:** API ของ ML 0.7.3 ต่างจากเอกสาร, probe ชนกับ LongYinThai, ACL ของ named pipe
**Rollback:** .dll.off หรือลบ DLL

## 3. S1 — สังเกตการณ์ฝั่ง host
**เป้าหมาย:** อ่าน state โลกและศึก และจับ battle log แบบอ่านอย่างเดียว

**Deliverables**
- WorldSnapshot: worldTime, hour, bigMapPos ของปาร์ตี้และ NPC
- BattleSnapshot: battleState, nowActiveUnit, ตำแหน่ง/HP หน่วย
- BattleLogHook: Harmony postfix ที่ AddInfoText
- ring buffer, IPC snapshot/capture, ตัวนับ dropped/gap

**สำรวจ just-in-time**
- AddInfoText ถูก inline ไหม (เทียบจำนวน log ที่จับได้กับที่แสดงบนจอ)
- ลำดับ postfix ของเราเทียบกับ LongYinThai (ได้ข้อความก่อนหรือหลังแปล) [v2]

**Exit criteria:** capture ≥ 99%, mod cost p99 ≤ 1.5 ms/frame, snapshot ตรงกับ UI 20/20 ครั้ง, ไม่มี memory leak, hash เซฟจริงไม่เปลี่ยน
**ความเสี่ยง:** method ถูก inline, allocation ต่อเฟรม, ชนกับ LongYinThai
**Rollback:** coop.capture=false

## 4. S1.5 — S3-spike (ไม่มีเครือข่าย)
**เป้าหมาย:** พิสูจน์ว่าปลั๊กอินสั่งยูนิตที่ถึงตาให้ move/attack ผ่าน method ของเกมได้ บนเซฟสำเนา (host เท่านั้น) ถ้าไม่ผ่าน ให้กลับมาทบทวนแผนก่อนสร้างเครือข่าย

**Deliverables:** UnitCommandSpike (ปุ่มลัด), TurnGate, Executor v1, audit log, CoopSpike.md

**สำรวจ just-in-time** [v2]
- เมื่อยูนิต playerControl ถึงตา เกมหยุดรอ input จริงไหม ทดสอบทั้งตอนเร่งเวลาและไม่เร่ง
- พฤติกรรมของ battleTime / GetBattleDeltaTime / battleMove / ManageHeroSkillCdTime ระหว่างรอ: ศึกเป็นสลับเทิร์นล้วน หรือมีเวลาเดินแบบ ATB
- battlePaused หยุดศึกได้โดยไม่ทำให้ค้าง และปลดแล้วเล่นต่อได้
- ลำดับ method ที่ UI เรียกตอนผู้เล่นสั่ง move/attack

**Exit criteria**
1. move/attack สำเร็จ 10/10
2. ปฏิเสธถูกต้อง 20/20 (ไม่ใช่ตา, นอกระยะ)
3. ไม่มี soft-lock
4. ยืนยันว่าเกมหยุดรอ input ตอนถึงตายูนิตผู้เล่น ทั้งเร่งและไม่เร่งเวลา (หรือรายงานว่าไม่หยุด พร้อมผลกระทบต่อ S3) [v2]
5. battlePaused เปิด/ปิดได้ 10/10 โดยไม่ค้าง [v2]
6. hash เซฟจริงไม่เปลี่ยน
7. panic switch coop.allowRemoteInput=false ใช้ได้

**ความเสี่ยง:** soft-lock, marshalling ของ IL2CPP ผิด, ศึกเป็น ATB จนต้องออกแบบ S3 ใหม่ [v2]
**Rollback:** coop.allowRemoteInput=false, คืนเซฟ

## 5. S2 — Spectator
**เป้าหมาย:** ส่ง state ของ host ไปให้ guest ดูผ่านเครือข่าย guest แสดงผลแบบ overlay อย่างเดียว

**Deliverables**
- transport TCP (ใช้ผ่าน VPN เสมือน LAN: ZeroTier/Tailscale/Radmin) + named pipe สำหรับทดสอบในเครื่อง
- Protocol v1 (host → guest เท่านั้น): HELLO{protoVer, modVer, gameVer, unityVer, fieldHash}, WORLD, BATTLE, LOG, PING/PONG, BYE
- รูปแบบการส่ง [v2]:
  - ศึก: ส่ง event/delta ทุกครั้งที่ action จบ (ใครทำอะไร, ผล, HP/ตำแหน่งที่เปลี่ยน, log) ไม่ส่งตามรอบเวลา
  - world map: ส่ง delta ของตำแหน่ง/เวลาเมื่อเปลี่ยน โดยจำกัดความถี่ไม่เกินที่จำเป็นต่อการแสดงผล
  - snapshot เต็ม + fieldHash เป็นระยะ (เช่น ทุก N action หรือทุกไม่กี่วินาที) ไว้ตรวจ desync และให้ guest ที่เพิ่งเข้าตามทัน
  - ลำดับข้อความมีเลข seq ถ้าขาดให้ขอ snapshot เต็มใหม่
- guest overlay ด้วย OnGUI, ตัววัดของ viewer

**Exit criteria**
1. latency ต่อ event p95 ≤ 150 ms ผ่าน VPN [v2]
2. bandwidth ≤ 30 KB/s (เพดาน; คาดว่าเฉลี่ยต่ำกว่านี้มาก) [v2]
3. fieldHash ของ host กับ guest ตรงกันทุกครั้งที่มี snapshot เต็ม (divergence = 0)
4. ข้อความขาด → guest ขอ snapshot เต็มแล้วกลับมาตรงกันได้ [v2]
5. ยืนยันว่า guest อ่านอย่างเดียว ไม่เรียก method ของเกม

**ความเสี่ยง:** stable ID ซ้ำ, ความแม่นยำ float, ประสิทธิภาพ IMGUI, event หายระหว่างทาง
**Rollback:** role=off หรือปิด transport

## 6. S3 — ช่องคำสั่งในศึก
**เป้าหมาย:** guest ส่งคำสั่งยูนิตที่ตัวเองคุม host ตรวจแล้วรันผ่าน method ของเกม

**Deliverables**
- Protocol v2: CMD, CMD_RESULT
- CommandValidator (host) ตรวจก่อนแตะเกม: เป็นตาของยูนิตนั้นจริง, side == player, ยูนิตมีชีวิต/ไม่ติดสถานะ, target อยู่ใน range, skill พร้อม/มี cost/ไม่ติด cooldown, phase == ช่วงรับคำสั่งของผู้เล่น, ไม่มี modal ค้าง — ใช้ตัวตรวจของเกมก่อน แล้วค่อยเสริมของเรา
- CommandExecutor (host) ต่อยอดจาก Executor ของ S1.5
- timeout: เกิน deadlineMs → auto เฉพาะยูนิตนั้น เฉพาะเทิร์นนั้น แล้วคืนการควบคุม
- control map (ยูนิตไหนใครคุม), audit log

**Exit criteria:** คำสั่งสำเร็จ ≥ 45/50, ปฏิเสธคำสั่งผิด 20/20, timeout → AI 20/20, p99 ≤ 8 ms, ไม่มี soft-lock
**ความเสี่ยง:** soft-lock, animation/timeScale ไม่ตรงกัน
**Rollback:** coop.allowRemoteInput=false, ล้าง control map

## 7. S5 — ความทนทาน
**เป้าหมาย:** ทนต่อ save/load, หลุดการเชื่อมต่อ, เวอร์ชันไม่ตรง และมีชุดทดสอบอัตโนมัติ

**Deliverables**
- handshake ด้วย fieldHash + VERSION.md
- session resume/reset
- fault injection matrix: ตัด TCP, ระงับ process guest, frame เสีย, cmdId ซ้ำ, clientTick เก่า
- coop/test/: driver python สตาร์ท host+guest บนสำเนา ยิง scenario ผ่าน IPC เก็บ log ออกรายงาน pass/fail; และ --selftest ใน mod

**Exit criteria:** scenario ผ่าน 15/15, host crash = 0, version mismatch ถูกจับได้ทุกครั้ง, soak 60 นาที, เซฟสมบูรณ์ (hash)
**ความเสี่ยง:** UnityEngine.Random ไม่มี seed, handle รั่ว, ML unload ไม่สะอาด
**Rollback:** ย้อน DLL ตามเวอร์ชัน, คืนเซฟ

## 8. S4 — World follow (opt-in)
**เป้าหมาย:** guest ตามปาร์ตี้ host บนแผนที่ ใช้โหมด path-follow เท่านั้น (ตัดโหมด teleport bigMapPos ออก)

**Deliverables**
- Protocol v3: WORLD_AUTH, ENCOUNTER
- guest path-follower: เรียก SetPlayerMoveTargetArea ไปยัง waypoint ของ host
- clock mirror (แสดงผล), ติดตาม modal

**สำรวจ just-in-time** [v2]
- วัด WorldData.hour ซ้ำตอนปาร์ตี้กำลังเดิน (ผลเดิม Δhour = 0 วัดตอนอยู่นิ่ง 2 วินาที)
- หาจุดที่เขียน WorldData.hour (ตอนนี้ยังเป็น GUESS)
- หา chain จากปะทะบนแผนที่ (MeetBigMapNpc) ไปถึง PrepareBattleMap (ผลสแกน 338 hit เดิมเชื่อไม่ได้)
- modal ที่หยุดโลก: ร้านค้า บทสนทนา พล็อต งาน (ตอนนี้ยังเป็น GUESS)

**Exit criteria**
1. drift ของ path-follow ≤ 2 map units
2. เวลาที่แสดงตรงกันระหว่าง host/guest
3. guest ไม่ทำให้เกิดศึกเอง (0 ครั้ง)
4. ตรวจจับ modal ได้ 9/10
5. เซฟของ guest ไม่เปลี่ยน
6. รันชุดทดสอบ S5 ซ้ำทั้งชุดหลังจบ S4 และผ่านเท่าเดิม [v2]

**ความเสี่ยง:** ชนกับเซฟ/บัญชี Steam ของ guest, rubber-banding, path กระตุก
**Rollback:** coop.follow=false

## 9. S6 — วิจัย (ไม่มีโค้ด)
**เป้าหมาย:** ศึกษาว่าปาร์ตี้ guest เดินอิสระคนละที่ได้ไหม แล้วสรุป GO/NO-GO
**Deliverable:** RESEARCH-independent-party.md
**ความเสี่ยง:** scope creep, สรุป GO จากข้อมูลไม่พอ
**Rollback:** ลบโฟลเดอร์วิจัย

## 10. ความเสี่ยงรวม
| # | ความเสี่ยง | ผลกระทบ | วิธีลด | ตรวจได้โดย |
|---|---|---|---|---|
| 1 | Harmony กับ method ที่ถูก inline | hook ไม่ทำงานบางจุด | เทียบจำนวนที่จับได้กับบนจอ, hook ชั้นที่ไม่ถูก inline | capture rate ของ S1 |
| 2 | Thread safety ของ IL2CPP | crash แบบสุ่ม, state พัง | คิวเดียว + assert main thread + MainThreadGuard | guard throw ทันที, soak 60 นาที |
| 3 | Frame budget | เกมกระตุก | budget 4 ms, วัด p99 ทุก stage | ตัววัดใน mod |
| 4 | เซฟพัง / ชนกับ Steam | เสียเซฟจริง | โปรโตคอล §1.2, guest ห้าม save | hash ก่อน/หลัง |
| 5 | เกมอัปเดต [v2] | offset/RVA/interop เปลี่ยนทั้งหมด | VERSION.md + เทียบ hash ทุก session | hash ไม่ตรง → หยุด |
| 6 | ชนกับ LongYinThai [v2] | log/ข้อความผิด, เกม error | ตรวจรายการ patch ใน S0, ลำดับ postfix ใน S1 | compat-notes.md |
| 7 | ศึกไม่ใช่เทิร์นล้วน (ATB) [v2] | ต้องออกแบบ S3 ใหม่ | พิสูจน์ใน S1.5 ก่อนสร้างเครือข่าย | exit criteria 4 ของ S1.5 |

## 11. การตัดสินใจ
- D1: viewer overlay ก่อน, S4 เป็น opt-in ทำท้ายสุด
- D2: S4 ใช้ path-follow เท่านั้น ไม่ teleport
- D3: S3 สั่งได้เฉพาะตอนถึงตายูนิตนั้น
- D4: ศึกแยกจอฝั่ง guest ย้ายไป S6 (วิจัย)
- D5: guest ห้าม save ตลอดไป
- D6: named pipe ในเครื่อง + TCP ผ่าน VPN เท่านั้น
- D7: UI ภาษาอังกฤษก่อน แยก string ไว้ใน UIStrings.cs
- D8: รองรับ 30 ยูนิตต่อฝั่ง (รวม 60), latency p95 150 ms

## 12. Changelog
- v1: ฉบับแรก + S1.5 (S3-spike) + แก้ตารางสิทธิ์ Harmony ของ S0
- v2: S0 เปลี่ยนเป็น "ไม่แตะ state ของเกม", S2 เปลี่ยนเป็น event/delta + snapshot เต็มเป็นระยะ, S1.5 เพิ่มการพิสูจน์ว่าเกมหยุดรอ input และตรวจ battleTime/battleMove/battlePaused, S4 เพิ่มงานสำรวจ hour/encounter chain และต้องรัน S5 ซ้ำ, S0 เพิ่ม exit criteria 4 ข้อ, เพิ่มกฎ interop แบบมีชนิด, VERSION.md, repo แยก, ความเสี่ยง #5–7