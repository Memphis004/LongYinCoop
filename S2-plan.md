# S2 — แผน Spectator (transport + viewer) · ฉบับเสนอ 2026-10-21 · **รออนุมัติ (ยังไม่ลงมือ)**

อ้างอิง: `STAGEPLAN.md §5 (S2)`, `§1.1` (สิทธิ์การเขียน), `§1.6` (เครือข่าย/IPC), `§1.4` (main thread), `§1.7` (denylist)
สถานะก่อนเริ่ม: S1.5 ปิดแล้ว (tag `stage-S1.5`) · เซฟจริง `57 ไฟล์ digest 2b0548fe464b` · build `dll 517758147f59…`

## 0. สรุปสั้น

S2 = **ทางเดียว: host → guest** · guest เป็น **spectator ล้วน** (ดู overlay) · **ไม่มีคำสั่งเกมจาก guest แม้แต่ไบต์เดียว** · transport ซ่อนหลัง `ITransport` 4 เมธอด เริ่มด้วย TCP loopback → ค่อยขยับไป VPN
ผลลัพธ์ที่วัดได้ = latency, bandwidth, divergence, resync, host ไม่กระตุก, guest อ่านอย่างเดียว

## 1. ขอบเขต / ไม่ทำ

**ทำ**
- `ITransport` + `LoopbackTransport` + `TcpTransport` (ไม่มี dependency ใหม่ — `System.Net.Sockets`, `System.Security.Cryptography` มีใน net6.0)
- protocol v1 (host→guest) + ช่องควบคุม guest→host แบบ allowlist (ดู §4.3)
- handshake ด้วย shared secret (HMAC-SHA256 + nonce) และ `PROTO_VER`
- snapshot/delta แบบ event-driven + `seq` + `RESYNC` + `replicaHash`
- guest overlay (IMGUI) + metrics + `viewer-sim.py` / `viewer-client.py`
- วัดผลตาม exit criteria §8

**ไม่ทำ (เด็ดขาดใน S2)**
- ไม่รับ/ไม่รันคำสั่งเกมจากเครือข่าย (นั่นคือ S3)
- ไม่เรียก method ที่เปลี่ยน state ของเกม · ไม่เขียน field เกม (§1.1)
- ไม่มี NAT traversal / relay / STUN / UPnP / telemetry · ไม่แตะ Steam/สถิติ/`cheat*`
- guest ไม่บันทึกเซฟ ไม่เรียก `Save/Load`
- ไม่ mirror world/ไม่เข้า battle scene ฝั่ง guest (นั่นคือ S4 opt-in)

## 2. ตารางสิทธิ์การเขียน §1.1 — คอลัมน์ S2 (ยึดตามนี้)

| การกระทำ | S2 | หมายเหตุ |
|---|---|---|
| อ่าน field ผ่าน Il2CppInterop แบบมีชนิด | ✅ | snapshot sampler อ่านอย่างเดียว |
| อ่านด้วย raw offset/pointer | ❌ (ขออนุมัติก่อน) | ต้อง re-resolve ผ่าน interop |
| เรียก getter บริสุทธิ์ | ✅ | |
| Harmony patch (อ่าน argument เท่านั้น) | ✅ | ใช้ hook เดิมของ S1 (`BattleLogHook`/`BattleSampler`) ถ้าจำเป็น |
| เรียก method ที่เปลี่ยน state ของเกม | ❌ | S2 ไม่มี write path เลย |
| เขียน field ของเกมเอง | ❌ | |
| รับ input จากเครือข่ายแล้วรัน | ❌ | **ข้อห้ามหลักของ S2** — มีเทสต์ปฏิเสธใน §8 ข้อ 7 |
| เรียก `GameDataController.Save/Load` | ❌ | |
| เขียนไฟล์ (log/pref/capture ของ mod) | ✅ | `UserData/LongYinCoop/...` เท่านั้น |
| Steam / online services | ❌ | denylist §1.7 ยังทำงาน |

## 3. สถาปัตยกรรม

### 3.1 `ITransport` (เล็กสุด 4 เมธอด + stats)

```csharp
public interface ITransport {
    bool Connect(string host, int port, int timeoutMs);   // guest เรียก; host ใช้ Listen แยกใน TcpTransport
    void Send(ReadOnlySpan<byte> frame);                   // frame เดียว ≤ maxFrameBytes
    event Action<byte[]> Received;                          // เรียกบน receive thread → ส่งต่อ MainThreadDispatcher
    void Close();
    TransportStats Stats { get; }                           // bytesIn/Out, frames, drops
}
```

- `LoopbackTransport` — คิวในโปรเซส (ใช้ T0/T1) ไม่มี socket
- `TcpTransport` — `Socket` แบบ non-blocking, receive thread ของตัวเอง, reassembly ตาม length-prefix, ปิดเมื่อ frame ผิดรูป
- **ห้ามโค้ดส่วนอื่นแตะ `Socket` โดยตรง** — ทุกอย่างผ่าน `ITransport` (เปลี่ยนเป็น QUIC/pipe ทีหลังได้โดยไม่แตะ protocol)
- thread: I/O อยู่บน thread ของ transport; ทุกอย่างที่แตะ object เกม/UI ผ่าน `MainThreadDispatcher` (§1.4)

### 3.2 framing (ทุก frame ≤ **1100 B** ตาม §1.6)

```
[u16 BE len][u8 type][u8 ver][u32 BE seq][payload ...]        len = 1+1+4+payload
```
- message ใหญ่เกิน → แบ่งเป็น `CHUNK{msgId u16, idx u16, total u16, data}` แล้ว reassemble (มี cap 64 KB ต่อ message, timeout 2 s ทิ้งทั้งก้อน)
- writer เป็น custom compact writer (varint สำหรับ int, float32 ตรง ๆ) — **ไม่เพิ่ม dependency**

### 3.3 ชนิดข้อความ

**host → guest**
| type | ความหมาย |
|---|---|
| `HELLO` | `{protoVer, modVer, gameVer, unityVer, hostName, nonce[32]}` |
| `AUTH_RESULT` | `{ok u8, reason}` |
| `DENY` | `{reasonCode u8, text}` แล้วปิด |
| `WORLD` | world map delta (≤5 Hz) |
| `BATTLE_EVENT` | move/attack/damage/death/turn/pause (event-driven) |
| `SNAPSHOT` | snapshot เต็ม + `baseSeq` + `replicaHash` |
| `PONG` | ตอบ `PING` (echo nonce + host time) |
| `BYE` | ปิดอย่างสุภาพ |

**guest → host (allowlist เท่านั้น)**
| type | ความหมาย |
|---|---|
| `AUTH` | `{hmac[32]}` |
| `PING` | `{nonce u64}` |
| `RESYNC` | `{lastSeq u32}` |
| `BYE` | |

- **ไม่มี type ใดในช่องนี้ที่บรรจุการกระทำของเกม** (ไม่มี move/attack/pause/skill/field อะไรทั้งสิ้น)
- type นอกลิสต์ → host log `DROP forbidden type` + counter + ปิด connection ถ้าซ้ำเกิน 3 ครั้ง
- ขนาด `AUTH`/`PING`/`RESYNC` ถูก cap ไว้ (≤ 64 B) — frame ใหญ่ผิดปกติจาก guest ถูกทิ้ง

### 3.4 handshake + auth (ส่วนที่ STAGEPLAN ไม่ได้ระบุ — ต้องอนุมัติ)

1. TCP accept → host ส่ง `HELLO` พร้อม `nonce` 32 ไบต์ (สุ่มต่อ connection)
2. guest คำนวณ `hmac = HMAC_SHA256(secret, nonce || PROTO_VER)` → ส่ง `AUTH`
3. host เทียบด้วย `CryptographicOperations.FixedTimeEquals` → `AUTH_RESULT{ok=1}` หรือ `DENY` + ปิดภายใน 2 s
4. `PROTO_VER != 1` → `DENY{proto}` (แจ้งเวอร์ชัน ไม่ต่อรอง)
5. rate limit: 1 handshake / 5 s / IP, pending สูงสุด 4, timeout 5 s
6. **secret ไม่เคยถูกส่งบนสาย** · ถ้า `role≠off` และ `secret` ว่าง → ไม่ยอมเริ่ม listener (log ชัด ๆ)
7. ไม่มี TLS ใน S2 (ขอบเขต: VPN + shared secret + loopback) — บันทึกเป็นความเสี่ยง §11

### 3.5 prefs (ค่าเริ่มต้นปลอดภัย)

| pref | default | ความหมาย |
|---|---|---|
| `coop.net.enabled` | `false` | เปิดระบบเครือข่าย |
| `coop.net.role` | `off` | `off` / `host` / `guest` |
| `coop.net.bind` | `127.0.0.1` | **IP เจาะจงเท่านั้น** (ไม่รับ `0.0.0.0`/`*`) |
| `coop.net.port` | `47800` | |
| `coop.net.secret` | `""` | shared secret (ผู้ใช้ตั้งเอง) |
| `coop.net.maxFrameBytes` | `1100` | |
| `coop.net.showIpOnStart` | `true` | แสดง IP:port ให้เห็นก่อนเชื่อม (ตาม §1.6) |
| `coop.net.snapshotEveryActions` | `20` | snapshot เต็มเป็นระยะ |

> **จุดที่ต้องยืนยัน (ขัดกับ §1.6 — ดู §12 ข้อ 1):** ผู้ใช้สั่ง "ต้องเปิดฟังบน interface อื่นด้วย pref ที่ตั้งเอง" — แผนนี้ตีความว่า pref รับ **IP ของ interface นั้นแบบเจาะจง** (เช่น IP ของ ZeroTier/Tailscale) ไม่ใช่ `0.0.0.0`

## 4. โมเดลข้อมูลฝั่ง guest (replica)

### 4.1 field ที่ replicate (S2)

**battle header:** `battleKey`, `turnIndex`, `nowActiveState`, `nowActiveStateName`, `activeUnitId`, `battlePaused`, `battleTime`, `activeAutoFight`
**unit (ทุกตัว):** `stableId`, `team`, `heroId`, `row`, `col`, `hp`, `originHp`, **`unitAlive`**, `alive`, `moved`, `attacked`, `playerControl`
**event:** `seq`, `kind`, `actorId`, `targetId`, `hpBefore`, `hpAfter`, `cells`, `t`

- **`hp` และ `unitAlive` ส่งค่าดิบทุกครั้ง** (ไม่ derive จาก `stateHash`, ไม่อิง `alive` ที่พิสูจน์แล้วว่าไม่สะท้อนการตาย) — ตามมติ 2026-10-21
- `SNAPSHOT` ส่งชุดนี้ครบทุกยูนิต + `replicaHash`

### 4.2 `replicaHash` (ของ S2 — **ไม่ใช่ `stateHash` ของ S1.5**)

```
sha256( "s2|1" + battleKey + turnIndex + activeUnitId + paused
        + ∀unit( stableId|team|row|col|hp(3)|unitAlive|moved|attacked ) เรียงตาม stableId )
```
- ตั้งใจให้ **เปลี่ยนเมื่อ hp เปลี่ยน/ยูนิตตาย** (ปิดช่องโหว่ที่วัดได้ใน S1.5)
- ใช้เทียบ host store ↔ guest store เท่านั้น ไม่ใช่ตัวตัดสิน gameplay

### 4.3 `seq` + resync

- `seq` เพิ่มทีละ 1 ต่อ message (ต่อ connection) · guest เห็นช่องว่าง → ส่ง `RESYNC{lastSeq}`
- host ตอบ `SNAPSHOT` เต็มพร้อม `baseSeq` → guest แทน store ทั้งก้อน (ไม่เดา ไม่ interpolate ข้ามหาย)
- นับ `missingSeq`, `resyncCount`, `divergenceCount` ลง metrics

## 5. ฝั่ง guest (viewer)

- IMGUI `OnGUI` เท่านั้น: grid, ชื่อ/HP bar, ชื่อยูนิตที่ถึงตา, log ล่าสุด N บรรทัด, สถานะเชื่อมต่อ/latency/role
- smoothing buffer 100 ms, `extrapolate=false` (แสดงค่าล่าสุดที่ยืนยัน) — buffer ต้องไม่เพิ่มดีเลย์ค่าล่าสุดเกิน 1 frame
- `GUI.depth` ต่ำ, cached `GUIStyle`, ไม่ allocate ต่อเฟรม
- guest ไม่แตะ object เกมใด ๆ — แสดงจาก store ของตัวเองเท่านั้น (พิสูจน์ด้วย code review + `WriteGuard`)

## 6. วิธีทดสอบบนเครื่องเดียวก่อน (ตามลำดับ)

| ขั้น | ทำอะไร | ต้องมีเกม? | วัดอะไร |
|---|---|---|---|
| **T0** | unit tests ในโปรเซสเดียว: framing/chunking, seq gap, HMAC vectors, canonical hash, allowlist | ❌ | ความถูกต้องของ codec/protocol |
| **T1** | `LoopbackTransport` ในเกมจริง (host+client โปรเซสเดียว) | ✅ (เซฟสำเนา) | event/snapshot ตรงกับสภาพจริง, ไม่มี write path |
| **T2** | เกม (host, `bind=127.0.0.1`) + `tools/viewer-client.py` (Python) | ✅ | end-to-end TCP, latency loopback, bytes/s, resync |
| **T3** | เกม 2 อินสแตนซ์บนเครื่องเดียว (host + guest overlay) — **ต้องอนุมัติก่อน** (Steam/2 อินสแตนซ์/เซฟ) | ✅✅ | overlay จริง, input isolation |
| **T4** | เครื่องที่สองผ่าน VPN (ZeroTier/Tailscale) bind IP ของ VPN | ✅✅ | exit criteria ข้อ 1–2 (p95/bandwidth) |

- T0–T2 ทำได้ทันทีโดยไม่แตะเครือข่ายจริง · **T3/T4 ต้องอนุมัติแยก**
- เซฟ: ทุกขั้นที่รันเกมใช้ **สำเนา** ตาม §1.2 (`save-split.py`), เซฟจริงห้ามแตะ
- ถ้า T3 ไม่ได้ (Steam/2 อินสแตนซ์ไม่ยอม) → ทางเลือก: guest overlay ในโปรเซสเดียว อ่านจาก `LoopbackTransport` (ทดสอบ UI ได้) แล้วข้ามไป T4

## 7. JIT ที่ต้องตอบใน S2 (จาก STAGEPLAN §5)

1. **stable identity ของยูนิต** — มีเบาะแสแล้ว: `0:9`, `1:10066` คงที่ตลอดศึก S1.5e/f [RUNTIME] ต้องยืนยันว่าคงข้ามการโหลดศึก/ข้ามเทิร์น
2. **field ขั้นต่ำที่ viewer "อ่านรู้เรื่อง"** — ทดลอง 2 ศึก ตัดให้เหลือน้อยสุด
3. **bandwidth จริงที่ 60 ยูนิต (30/ฝั่ง ตาม D8)** — bytes/action, bytes/นาที, burst; ตัดสิน quantize
4. **overlay ไม่รบกวน input/เฟรม** — `GUI.depth`, `Event.current`, frame time delta
5. **guest อยู่ฉากไหน** — S2 ไม่เข้า battle scene; ยืนยันว่า overlay บน world map อ่านรู้เรื่อง หรือต้องมี "โหมดดู"

## 8. Exit criteria (วัดได้) — 6 ข้อของ STAGEPLAN + 3 ข้อเพิ่มของ S2

| # | เกณฑ์ | เกณฑ์วัด |
|---|---|---|
| 1 | latency p95 (host-apply → guest-store) | ≤ **150 ms** ผ่าน VPN · ≤ **40 ms** loopback · ที่ศึก 60 ยูนิต |
| 2 | bandwidth | ≤ **30 KB/s** เฉลี่ยทั้งศึก 5 นาที @60 ยูนิต (burst บันทึกเป็นข้อมูล) |
| 3 | divergence 5 นาทีในศึก | **0** (เทียบ `replicaHash` ทุก snapshot) |
| 4 | `seq` ขาด → resync | กลับมาตรง 0 divergence หลัง resync |
| 5 | guest ปิด/แล็ก/หลุด | host frame time p99 เปลี่ยน **≤ 1 ms**, 0 exception |
| 6 | guest อ่านอย่างเดียว | code review + `WriteGuard` (dev build) โยน exception ถ้ามี write path |
| **7** | **ไม่มี input ถึง host** | ยิง frame ปลอม `{type=move/attack/pause}` จาก client → host ต้อง `DROP forbidden type`, **0 การเปลี่ยน state** (`stateHash`+audit เท่าเดิม) |
| **8** | bind + auth | default ฟังเฉพาะ `127.0.0.1`; IP อื่นต้องตั้ง pref เจาะจง · secret ผิด → `DENY` + ปิด (ทดสอบทั้งคู่) |
| **9** | replica ถูกต้องเรื่องความตาย | snapshot ต้องเปลี่ยน `hp`→0 และ `unitAlive`→false เมื่อมีศพ (ใช้เคสศพจริงจาก S1.5f เป็น fixture) |

## 9. Deliverable + หลักฐาน

| # | สิ่งของ | path |
|---|---|---|
| 1 | `ITransport` + `LoopbackTransport` + `TcpTransport` | `src/LongYinCoop/S2/Transport/` |
| 2 | protocol codec + auth | `src/LongYinCoop/S2/Protocol/` |
| 3 | host sampler/sender | `src/LongYinCoop/S2/Host/` |
| 4 | guest store + overlay | `src/LongYinCoop/S2/Guest/` |
| 5 | metrics + logs | `UserData/LongYinCoop/logs/`, `UserData/LongYinCoop/S2/` |
| 6 | `viewer-sim.py` (replay) + `viewer-client.py` (TCP client) | `tools/` |
| 7 | รายงาน + หลักฐาน | `evidence/S2-<ts>/S2-report.md` |

## 10. Rollback

| ชั้น | ทำอะไร |
|---|---|
| pref | `coop.net.enabled=false` / `role=off` → ปิด listener + ตัด connection (ไม่แตะเกม) |
| DLL | `Mods\LongYinCoop.dll` → `.dll.off` (mod หายทั้งตัว) |
| git | tag `stage-S1.5` (ฐานก่อน S2) |
| เซฟ | S2 ไม่มี write path — `save-split.py verify` + hash ยืนยันก่อน/หลัง |
| tag | `checkpoint-S2` เมื่อปิดงาน · `stage-S2` เมื่อผู้ใช้อนุมัติรายงานปิด stage |

## 11. ความเสี่ยง

| ความเสี่ยง | ผล | ทางลด |
|---|---|---|
| ไม่มี TLS (secret อาจรั่วผ่าน VPN ที่ไม่น่าเชื่อถือ) | คนกลางอ่าน/ปลอมได้ | จำกัดบน VPN ส่วนตัว + secret ยาว ≥32 ไบต์ · บันทึกเป็นข้อจำกัด |
| frame ใหญ่เกิน MTU VPN | แพ็กเก็ตหลุด/ค้าง | hard cap 1100 B + chunking + ทดสอบที่ MTU ต่ำ |
| overlay ทำเฟรมตก | guest เล่นไม่ได้ | cached style, redraw ตาม event, วัด frame delta |
| 2 อินสแตนซ์บนเครื่องเดียวชน Steam/เซฟ | เสี่ยงเซฟจริง | ใช้ T2 (Python client) ก่อน; T3 ต้องอนุมัติ |
| pointer/identity เปลี่ยน | delta ผิด | ใช้ `stableId` เท่านั้น + snapshot เป็นระยะ |
| guest ถูกเข้าใจผิดว่าเป็นช่องสั่งการ | ขัดเจตนา co-op | allowlist §3.3 + เทสต์ข้อ 7 + `WriteGuard` |

## 12. ข้อขัด/ข้อที่ต้องเติม (รายงานตามกติกา — **รอผู้ใช้ตัดสินก่อนเริ่ม S2**)

1. **ข้อขัดจริง — ขอบเขต bind:** `STAGEPLAN §1.6` ระบุ *"โหมดข้ามเครื่อง = TCP บน IP ของ interface VPN เท่านั้น — bind เฉพาะ IP นั้น"* แต่คำสั่งล่าสุดบอก *"ต้องเปิดฟังบน interface อื่นด้วย pref ที่ตั้งเอง"*
   - **A (แผนนี้เลือก):** pref = **IP เจาะจง** (ผู้ใช้พิมพ์ IP ของ interface ที่ต้องการ เช่น VPN IP) → ไม่ขัด §1.6, ไม่มี `0.0.0.0`
   - **B:** อนุญาต bind กว้าง (`0.0.0.0`/ทุก interface) → **ขัด §1.6/D6** ต้องแก้ STAGEPLAN ก่อน
   - ต้องยืนยัน A หรือ B ก่อนเริ่ม
2. **ต้องเติมใน STAGEPLAN (ไม่ขัดแต่ขัดแย้งภายใน):** §5 เขียนว่า protocol "host → guest เท่านั้น" แต่ก็ต้องมี `PING/PONG` และ "guest ขอ snapshot เต็ม" → แผนนี้เติม **ช่องควบคุม guest→host แบบ allowlist 4 ชนิด** (`AUTH/PING/RESYNC/BYE`) และห้ามมีคำสั่งเกม — ขออนุมัติถ้อยคำนี้
3. **ต้องเติม (ส่วนขยาย protocol):** shared secret handshake (`HELLO`+nonce → `AUTH` HMAC) ไม่มีใน STAGEPLAN §5 — เป็นส่วนที่ผู้ใช้สั่งให้มี; ต้องอนุมัติให้ protocol v1 มีขั้นนี้
4. **ต้องยืนยัน:** T3 (เกม 2 อินสแตนซ์บนเครื่องเดียว) ไม่ได้อยู่ในข้อยกเว้น §1.6 (ข้อยกเว้นพูดถึงเฉพาะ viewer transport บน loopback) → แผนนี้เลี่ยงไป T2 ก่อน และขออนุมัติแยกถ้าจะทำ T3
5. **หมายเหตุขอบเขต:** STAGEPLAN §5 ต้องการวัดที่ศึก **60 ยูนิต (30/ฝั่ง)** — ต้องมีเซฟที่มีศึกขนาดนั้น; ถ้าเซฟปัจจุบันไม่มี ต้องบันทึกเป็น JIT และหาทางทดสอบ (ยังไม่ต้องทำตอนนี้)
