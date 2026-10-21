# S2 — แผน Spectator (transport + viewer) · ฉบับเสนอ 2026-10-21 · **rev2 (2026-10-21 20:50) — อ่าน §13 ก่อนใช้ §1–§12 · รออนุมัติ (ยังไม่ลงมือ)**

> **rev2:** §13 = ผลตรวจแผนเทียบข้อเท็จจริง RUNTIME ล่าสุด + ข้อบังคับใหม่ (field ความตาย, audit ที่ไม่ใช้ `stateHash`, bind loopback, preflight, fixture 60 ยูนิต) — **§13 มีผลเหนือ §1–§12 เมื่อขัดกัน**

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

---

## 13. rev2 — ผลตรวจแผนเทียบข้อเท็จจริง RUNTIME (2026-10-21 20:50) · **มีผลเหนือ §1–§12**

ตรวจโดย: อ่าน `S2-plan.md` + `STAGEPLAN.md §1.1/§1.4/§1.6/§1.7/§5/§10/§11.1` + โค้ด S1/S1.5 ที่ ship (`Mods\LongYinCoop.dll` = `517758147f59…`) + หลักฐาน S1.5f/S1.5g/S1.5gB
ไม่ได้รันเกม ไม่ได้แตะเซฟ ไม่ได้แก้ `src/` — ข้อที่เป็น RUNTIME คือค่าที่วัดไว้แล้วในหลักฐาน/อ่านจากโค้ดที่รันจริง

### 13.1 ตารางเทียบ แผนเดิม ↔ ข้อเท็จจริงล่าสุด

| # | จุดในแผน | แผนเดิมว่า | ข้อเท็จจริง | ป้าย | ผลต่อแผน |
|---|---|---|---|---|---|
| F1 | หัวแผน บรรทัด 4 | เซฟจริง digest `2b0548fe464b` | เซฟจริง = `6dd645d9ef17` (57 ไฟล์) และ **ผู้ใช้อนุมัติเป็น baseline ใหม่ 2026-10-21** | RUNTIME | แก้เป็น `6dd645d9ef17` + ชี้ `coop-test/baselines/20261021-200450` |
| F2 | §10 Rollback | tag ฐาน = `stage-S1.5` | ฐานจริงก่อน S2 = `stage-S1.5g` → `e3e798a` (annotated) | RUNTIME (git) | tag ฐาน rollback = `stage-S1.5g` |
| F3 | §4.1 unit field | replicate `alive` | `alive` มาจาก `!hero.dead` (`BattleSampler.cs:248`) — ศพมี `dead=false` ⇒ **false-positive**; `BattleUnit.IsAlive` คือธงจริง (`TurnGate.SafeUnitAlive`) | RUNTIME (S1.5f) | **ตัด `alive` ออก** เพิ่ม `isAlive` |
| F4 | §4.1/§8 #9 | `unitAlive`→false พร้อม `hp`→0 | ในเฟรมที่ `hp`→0 `unitAlive` **ยัง true** (lag ≥1 เฟรม) | RUNTIME (S1.5f) | เกณฑ์ความตาย = `hp==0 && isAlive==false`; `unitAlive` อนุญาต lag ≤ 3 snapshot แล้วต้องเปลี่ยน ไม่งั้น FAIL |
| F5 | §8 #7 | ใช้ `stateHash` เป็นหลักฐาน "0 state change" | `stateHash` มี `alive` ที่มาจาก `hero.dead` + cap หน่วย 64 ⇒ ไม่ใช่ audit ที่เชื่อได้ | RUNTIME + SPEC | เปลี่ยนเป็น audit tuple (§13.4) |
| F6 | §4.1 header | `battleKey` | **ไม่มี field นี้** ใน sampler/`BattleFrame` (มี `battleType`, `battleState`) | RUNTIME (code) | ต้องนิยาม derived key (§13.2) + วัดว่าเสถียร |
| F7 | §4.1 | `originHp` | มีจริง (`BattleUnit.originHp`, อ่านใน `TurnGate.SafeOriginHp`) แต่คอมเมนต์ในโค้ดระบุ **ยังไม่ยืนยันความหมาย** | RUNTIME + GUESS(ความหมาย) | ใช้เพื่อแสดงเท่านั้น ห้ามใช้ตัดสิน gameplay |
| F8 | §4.1 | `activeAutoFight` | ไม่ใช่ field ของ `BattleFrame`; ประกอบจาก `nowActiveUnit.autoFight` ใน `TurnGate` | RUNTIME (code) | ระบุ source ให้ชัด |
| F9 | §3.3 `HELLO` | `{protoVer, modVer, gameVer, unityVer, hostName, nonce}` | `STAGEPLAN §5` บังคับ `fieldHash`; §10 risk #8 บังคับ mismatch → ปฏิเสธ ≤1 s | SPEC (แผนเก่า) | เพิ่ม `fieldHash` + `DENY{fieldHash}` ≤1 s + เทสต์ |
| F10 | §3.3 type list | ชุด type ของแผนเอง | `STAGEPLAN §5` ระบุ `HELLO/WORLD/BATTLE/LOG/PING-PONG/BYE` — แผนเพิ่ม `SNAPSHOT/AUTH/AUTH_RESULT/DENY/RESYNC` และ **ไม่มี `LOG`** | SPEC ขัดกัน | ต้องอนุมัติถ้อยคำ + เพิ่ม `BATTLE_LOG` (map กับ `LOG`) |
| F11 | §3.2/§4.3 | `seq` ต่อ message + chunk | ไม่ระบุว่า chunk นับ `seq` ด้วยหรือไม่ ⇒ reassembly กำกวม | SPEC กำกวม | `seq` = ต่อ **logical message** เท่านั้น; chunk มี `msgId` |
| F12 | §8 #1 latency | host-apply → guest-store | ไม่ระบุวิธีเทียบเวลาข้ามเครื่อง (clock skew) ⇒ วัด p95 ผ่าน VPN ไม่น่าเชื่อ | SPEC กำกวม | event พก `hostApplyMs` (monotonic ของ session) + ประเมิน offset ด้วย PING/PONG |
| F13 | §3.1 thread | "ทุกอย่างที่แตะ object เกมผ่าน MainThreadDispatcher" | ยังไม่ระบุจุด sampling/serialization ให้ครบ §1.4 | SPEC | เพิ่มข้อบังคับใน §13.2 |
| F14 | §8 #6 | `WriteGuard` โยน exception ถ้ามี write path | **ยังไม่มี `WriteGuard`** ใน build ปัจจุบัน (ไม่มีใน `src/`) — เป็นงานใหม่ | RUNTIME (code) | นิยามกลไกให้วัดได้จริง (§13.2) ไม่กล่าวอ้างเกิน |
| F15 | §3.5/§12.1 | bind = IP เจาะจง, รอเลือก A/B | ค่าเริ่มต้นต้องเป็น loopback; การ bind นอก loopback = ความเสี่ยงใหม่ ต้องอนุมัติแยก | SPEC (คำสั่ง session นี้) | default `127.0.0.1` + gate `allowNonLoopback=false` (§13.2) |
| F16 | ทั่วแผน | ใช้ `battlePaused` ใน header + วัด pause | **build นี้ไม่มี `spike pause`**; เมนู Esc หยุด sim ขณะ `battlePaused=false`, `timeScale=1` | RUNTIME (S1.5g) | ห้ามใช้ `battlePaused`/`timeScale` ตัดสิน "เกมเดิน"; S2 ไม่มี type pause อยู่แล้ว — คงไว้และเขียนเป็นข้อห้าม |
| F17 | §6/§8 | สมมติ sim เดินตลอดหน้าต่างวัด | Esc ทำให้ `battleTime`/`turnIndex` นิ่ง 67.8 s โดย `frame` ยังเดิน | RUNTIME (S1.5g) | ต้องตรวจ idle ด้วย `battleTime`/`turnIndex`/state และบันทึกหน้าต่างที่ exclude |
| F18 | §6/§10 | ทุก run ใช้สำเนา + verify | เกมไม่มี autosave ตอนออก; `PlayerprefData.dat` เขียนทุกครั้งที่เปิด/ปิด ⇒ drift เป็นปกติ | RUNTIME (S1.5gB) | ทุก run จบด้วย `verify` + `rebaseline` (volatile-only) + บันทึก |
| F19 | §6 | "ใช้สำเนาตาม §1.2" | เกมถูกเปิดบนเซฟจริงก่อน split มาแล้ว 2 ครั้ง (16:07, 19:49) — ไม่มี gate ก่อนเปิดเกม | RUNTIME (incident) | บังคับ **split → sampler → ผู้ใช้เปิดเกม** + preflight PASS (§13.5) |
| F20 | §8 #1–#3 | วัดที่ 60 ยูนิต | ศึกที่วัดได้จริงคือ 11v7; ศึก 30v30 ยังไม่มีแหล่ง (`STAGEPLAN §11.1` ข้อ 3 ค้าง) | RUNTIME | ต้องมี fixture หรืออนุมัติลดเป้า — **บล็อก exit 1/2/3** |
| F21 | §3.4/§6 T4 | ผ่าน VPN ZeroTier/Tailscale | ยังไม่เลือกค่าย VPN และยังไม่เลือกเครื่อง guest (`§11.1` ข้อ 1–2) | SPEC ค้าง | ต้องตอบก่อน T4; S2a (loopback) ไม่ต้องรอ |
| F22 | §6 | ใช้ `save-split.py` | `split` default `newest_baseline()` ไม่สแกน `coop-test/baselines/` ⇒ ต้องระบุ `--expected-baseline` เอง | RUNTIME (incident §4) | บังคับ flag ตรง ๆ ทุกครั้ง |
| F23 | §2 สิทธิ์ | "S2 ไม่มี write path" | build ปัจจุบันยังมี IPC `spike move/attack` + pref `AllowRemoteInput`; S2 ต้องไม่เรียก | RUNTIME (code) | preflight ต้องยืนยัน `AllowRemoteInput=false`, `JitProbe*=false` |
| F24 | §1.7 | denylist ทำงาน | ยังไม่เขียนเป็นข้อห้ามชัด ๆ ในแผน | SPEC | เพิ่มข้อความใน §13.2 |
| F25 | §2 | Harmony hook เดิมอ่านอย่างเดียว | `BattleSampler`/`BattleLogHook` อ่าน field/argument เท่านั้น (review โค้ด) แต่ sample ทุกเฟรมลง ring 900 | RUNTIME (code) | ใช้ได้ แต่ต้องวัดต้นทุนต่อเฟรม (§13.3 S2a-2) |

**สรุป:** ไม่มีข้อใดทำให้ "S2 = spectator อ่านอย่างเดียว" เปลี่ยน — แต่แผนเดิม **ยังไม่พร้อมวัด** ใน 4 จุด: field ความตาย (F3/F4), audit ปฏิเสธคำสั่ง (F5), handshake/`fieldHash` (F9/F10), และ fixture 60 ยูนิต + VPN (F20/F21)

### 13.2 สิ่งที่ rev2 เปลี่ยน (ข้อบังคับ)

1. **ความตาย/ดาเมจ (แทน §4.1/§4.2/§8 #9)**
   - replicate ต่อยูนิต: `stableId, team, heroId, row, col, hp, maxHp, realMaxHp, unitAlive, isAlive, moved, attacked, playerControl`
   - **ห้ามส่ง/ห้ามใช้ `alive` (hero.dead)** และ **ห้าม derive จาก `stateHash` เดิม**
   - `replicaHash` = `sha256("s2|2" + battleKey + turnIndex + activeUnitId + paused + Σ unit(stableId|team|row|col|hp(3)|isAlive|unitAlive|moved|attacked) เรียง stableId)` — เปลี่ยนเมื่อ hp เปลี่ยน/ยูนิตตาย
   - **มติ 2026-10-21 (rev4 ของ S2a-2):** `unitAlive` = **สำเนาของ `isAlive`** เท่านั้น — interop assembly ไม่มี `BattleUnit.unitAlive` (มีแต่ `get_IsAlive`) และ `unitAlive` ที่ `spike status` รายงาน = `SafeUnitAlive(u) → u.IsAlive` ตัวเดียวกัน; sampler เติมด้วย `S2UnitRule.MirrorUnitAlive(u.IsAlive)` (ล้าหลัง `hp` ≤1 เฟรม ตาม F4) ⇒ **ฟิลด์นี้ไม่ให้ข้อมูลเพิ่ม** แต่ **คงไว้เพราะห้ามแก้ hash spec** (`s2|2` ไม่เปลี่ยน; เทสต์ T0 ผ่านเท่าเดิม) · ฟิลด์เดินทางบน wire + ndjson (`ProtoVer` ยัง = 1 เพราะยังไม่มี peer จริง) · **พิจารณาตัดฟิลด์นี้เมื่อเปลี่ยนเวอร์ชันโปรโตคอล (S2b) พร้อมอัปเดต tag ของ `replicaHash`**
   - `originHp` = optional display field, ติดป้าย "semantics unconfirmed"
2. **`battleKey` (แทน F6):** derived = `sha256(battleType | team IDs เรียง | frame ที่เห็นครั้งแรก)`, คงที่ตลอดศึก, ใช้แยก snapshot ข้ามศึก — ต้องวัดว่าคงที่จริงก่อนใช้เป็นกุญแจ resync (ถ้าไม่คงที่ → ใช้ `sessionId` ของ connection แทน)
3. **Audit "ไม่มีคำสั่งถึง host" (แทน §8 #7):** ใช้ tuple `(battleTime, turnIndex, nowActiveState, unitCount, Σ(stableId|hp(3)|isAlive|row|col))` + counters `forbiddenTypeDrops=0`, `commandsAccepted=0` — **ไม่ใช้ `stateHash`**
4. **Threading (§1.4):** sampler สร้าง POCO บน main thread ผ่าน `MainThreadDispatcher` (budget 4 ms/เฟรม) → serialize บน thread อื่น; ห้ามถือ `Il2CppObjectBase` ข้ามเฟรม; network thread อ่านได้เฉพาะ byte buffer
5. **`WriteGuard` (F14) นิยามจริง:** (a) dev build hook บน entry ที่เปลี่ยน state (`MoveCommand.StartMove`, `AttackCommand.StartAttack`, `BattleController.BattlePauseButtonClicked`, `GameDataController.Save/Load`) → ถ้าถูกเรียกจาก code path ของ S2 ให้ `throw` + log; (b) static check ใน build script: `grep` call sites ของ method เหล่านั้นใน `src/LongYinCoop/S2/**` ต้องเป็น 0; (c) `coopctl` ไม่มีคำสั่ง write ถูกเรียกในเทสต์ S2 — ถ้าทำได้ทั้ง 3 จึงนับ exit #6 (ถ้าได้แค่ a+b → PASS-with-limitation)
6. **bind (F15):** default `127.0.0.1`; รับเฉพาะ IP literal; **ปฏิเสธ** `0.0.0.0`, `::`, `*`, hostname; bind นอก loopback ต้อง `coop.net.allowNonLoopback=true` (default false) **และ** ต้องมีอนุมัติเป็นลายลักษณ์ใน stage นี้ → ถ้าไม่ได้รับอนุมัติ S2a วัดได้เฉพาะ loopback
7. **ข้อห้ามที่ต้องเขียนในโค้ด/รีวิว (F16/F23/F24):** ไม่มี type `pause/move/attack/skill/item` ในช่อง guest→host; ไม่เรียก pause/timeScale; ไม่ตัดสิน liveness ด้วย `battlePaused`/`Time.timeScale`; ไม่แตะ Steam/`APIDispatcher`/`DnsChecker`/`CheatCheck`/`cheat*`; ไม่เรียก `GameDataController.Save/Load`; guest ไม่บันทึกเซฟ; ไม่เพิ่ม dependency (`csproj` ต้องไม่มี `PackageReference`)
8. **fieldHash (F9):** `fieldHash = sha256(รายชื่อ field ที่ protocol ใช้ + anchor hash ของเกม)`; mismatch → `DENY{fieldHash}` + ปิดภายใน 1 s; ทดสอบด้วยการแก้ field list 1 รายการใน test client

### 13.3 ขั้นย่อย (S2a = วัดได้ตอนนี้ · S2b = ต้องมีคำตอบ VPN/60 ยูนิต)

| ขั้น | ทำอะไร | ต้องมีเกม? | ต้องมีผู้ใช้ทำอะไร | เครื่องมือ | วัดอะไร |
|---|---|---|---|---|---|
| **S2a-0** | protocol codec + framing/chunk/reassembly + HMAC vectors + canonical hash + allowlist (ไม่มี socket, ไม่มีเกม) | ❌ | — | `tests/` (console, ไม่เพิ่ม dependency) | test vectors ผ่าน 100%, ไม่มี allocation ต่อเฟรมใน hot path |
| **S2a-1** | preflight guard ใน `tools/save-split.py` (subcommand `preflight`) | ❌ | อนุมัติแก้ `tools/` | `save-split.py preflight --label --sampler-pid` | REFUSED/READY ถูกต้อง 6 เคส (§13.5) |
| **S2a-2** | `ITransport` + `LoopbackTransport` + host sampler/sender (อ่านอย่างเดียว) | ✅ | split → เปิด sampler → **เปิดเกม** → เข้าศึก 1 รอบ | mod + `tools/s1_watch.py`/`spike_watch.py` | frame cost p99, snapshot ตรงกับ `spike status`, audit tuple ไม่เปลี่ยน |
| **S2a-3** | `TcpTransport` (loopback) + `viewer-client.py` + metrics + resync | ✅ | เปิดเกมค้างไว้ระหว่างวัด | `tools/viewer-client.py` | p95 loopback ≤40 ms, bytes/s, `seq` gap→resync→divergence 0 |
| **S2a-4** | guest overlay (IMGUI) ในโปรเซสเดียวผ่าน loopback + `WriteGuard` + static check | ✅ | เปิด overlay ดู 1 ศึก | mod | frame time delta, input isolation, 0 exception |
| **S2b-1** | TCP ข้ามเครื่องผ่าน VPN (bind IP ของ VPN ตามอนุมัติ) | ✅✅ | ตั้ง VPN + เลือกเครื่อง guest | mod + client | p95 ≤150 ms, bandwidth ≤30 KB/s @60 ยูนิต |
| **S2b-2** | (ทางเลือก, ต้องอนุมัติแยก) 2 อินสแตนซ์บนเครื่องเดียว | ✅✅ | เปิด 2 อินสแตนซ์ | mod | overlay จริง, input isolation, Steam/เซฟไม่กระทบ |

- ทุกขั้นที่เปิดเกม: **split → preflight PASS → sampler ทำงาน → ผู้ใช้เปิดเกม** (ห้ามสลับ) · จบขั้น: ปิดเกม → `verify` → `rebaseline` ถ้า pref drift → บันทึก
- S2a ปิดได้โดยไม่ต้องรอคำตอบ VPN; S2b คือส่วนที่ต้องใช้ exit 1–2 ผ่าน VPN

### 13.4 Exit criteria ฉบับ rev2 (วัดได้ทีละข้อ)

| # | เกณฑ์ | วิธีวัด/เงื่อนไขผ่าน | fixture |
|---|---|---|---|
| 1 | loopback latency p95 | host-apply → guest-store (timestamp monotonic ใน event) ≤ **40 ms** | ศึกที่มีจริง (11v7) + 60 ยูนิตเมื่อมี fixture |
| 2 | VPN latency p95 | ≤ **150 ms** (ต้องมีคำตอบ Q3) — วัดซ้ำ 3 รอบ | S2b เท่านั้น |
| 3 | bandwidth | เฉลี่ยทั้งศึก 5 นาที ≤ **30 KB/s**; burst/action บันทึกเป็นข้อมูล | 60 ยูนิต (ถ้าไม่มี → บันทึกเป้าที่อนุมัติ) |
| 4 | divergence | `replicaHash` ตรงทุก snapshot เต็ม 5 นาที = **0** | ศึกจริง |
| 5 | resync | ตัด frame เทียม 1 ครั้ง → `RESYNC` → กลับตรง **0 divergence** | ศึกจริง |
| 6 | host ไม่กระตุก | guest ปิด/แล็ก/หลุด → host frame p99 เปลี่ยน ≤ **1 ms**, exception = 0 | ศึกจริง |
| 7 | guest อ่านอย่างเดียว | (a) dev `WriteGuard` ไม่ throw, (b) static check call sites = 0, (c) `coopctl` audit ไม่มี write | ทั้งหมด |
| 8 | ไม่มีคำสั่งถึง host | ยิง `move/attack/pause` ปลอม → `DROP forbidden type` + **audit tuple เท่าเดิมทุกค่า** + `commandsAccepted=0` | ทุกขั้น TCP |
| 9 | bind + auth | default loopback ✓; bind นอก loopback ถูกปฏิเสธจนตั้ง pref + อนุมัติ; secret ผิด → `DENY` ≤2 s; `fieldHash` ผิด → `DENY` ≤1 s | T0 + S2a-3 |
| 10 | ความตายใน replica | เคสศพจริง: `hp=0` และ `isAlive=false`; `unitAlive` เปลี่ยนภายใน ≤3 snapshot | S1.5f fixture |
| 11 | ไม่แตะเซฟ | `SaveSlot*` เปลี่ยน 0 ไฟล์; digest ตรง baseline อนุมัติ (ยกเว้น volatile) | ทุก run |

### 13.5 Pre-flight guard (ยังไม่แก้ไฟล์ — **รออนุมัติ**)

ลำดับบังคับ: **`split` → เปิด sampler → `preflight` PASS → ผู้ใช้เปิดเกม**

`save-split.py preflight --label <label> --sampler-pid <pid>` ตรวจ:
1. เกมไม่รัน (`game_running()` — มีอยู่แล้ว)
2. `Save.real-<label>` มีอยู่จริง 1 ตัว และ digest ตรง `MANIFEST.real.digest`
3. `Save` (ที่ mount) digest ตรง `savehash.copy.before.txt` ของ label ⇒ เป็นสำเนา ไม่ใช่ของจริง
4. `leftovers` = เฉพาะ `Save.real-<label>` (ไม่มี `Save.used-*` ค้าง)
5. sampler: PID มีชีวิต + cmdline เป็นหนึ่งใน `spike_watch.py|s15d_sample.py|s1_watch.py|spike_capture.py` + ไฟล์ `--out` ถูกสร้างแล้ว
6. prefs: `AllowRemoteInput=false`, `JitProbe*`=false (กัน write path หลุดเข้ามา)
7. เขียนผลลง `coop-test/<label>/history.ndjson` (`event=preflight`) + พิมพ์ `READY: เปิดเกมได้` / `REFUSED: <เหตุ>`

เหตุผล: เกมถูกเปิดบนเซฟจริงก่อน split 2 ครั้ง (16:07, 19:49) — tool เดิมไม่มี gate ก่อนเปิดเกม

### 13.6 Rollback (rev2)

| ชั้น | ทำอะไร | ตรวจว่าใช้ได้ |
|---|---|---|
| pref | `coop.net.enabled=false` / `role=off` → ปิด listener + ตัด connection | `coopctl get_version` ยังตอบ, ไม่มี port ฟัง |
| DLL | `Mods\LongYinCoop.dll` → `.dll.off` (เกมกลับ vanilla) | เปิดเกมได้, ไม่มี log ของ mod |
| git | tag ฐาน `stage-S1.5g` (`e3e798a`) | `git checkout stage-S1.5g` build ได้ |
| เซฟ | `save-split.py verify --label …` + `restore` | `Save` 57 ไฟล์ `6dd645d9ef17`, `leftovers: none` |
| tag | `checkpoint-S2a` / `checkpoint-S2b` ตอนปิดงาน · `stage-S2` เมื่ออนุมัติรายงาน | tag annotated |

### 13.7 ความเสี่ยงต่อเซฟ/Steam (rev2 — เฉพาะที่เกี่ยวกับงานนี้)

| ความเสี่ยง | ผล | ทางลด |
|---|---|---|
| เปิดเกมบนเซฟจริงก่อน split (เกิดแล้ว 2 ครั้ง) | pref drift / เสี่ยงเซฟจริง | preflight (§13.5) + ลำดับ split→sampler→launch |
| `PlayerprefData.dat` เปลี่ยนทุกครั้งที่เปิด/ปิด | hash ไม่ตรง baseline | `rebaseline` volatile-only ทุกครั้ง + บันทึก |
| Esc ค้าง → เข้าใจผิดว่า sim เดิน | วัด latency/divergence ผิด | ตรวจ `battleTime`/`turnIndex`/state, บันทึกหน้าต่าง exclude |
| 2 อินสแตนซ์บนเครื่องเดียว (T3) | Steam user เดียว/เซฟชน | ไม่อยู่ใน S2a; S2b-2 ต้องอนุมัติแยก |
| bind นอก loopback | เครื่องอื่นเข้าถึงได้ | default loopback + `allowNonLoopback=false` + อนุมัติเป็นลายลักษณ์ |
| secret ใน `MelonPreferences.cfg` (plaintext) | เครื่องที่เข้าถึงไฟล์อ่านได้ | secret ≥32 ไบต์, จำกัดสิทธิ์ไฟล์, บันทึกเป็นข้อจำกัด (ไม่มี TLS ใน S2) |
| เข้าใจว่า guest สั่งได้ | ขัดเจตนา co-op | allowlist + audit tuple + `WriteGuard` + static check |

### 13.8 คำถามที่ต้องให้ผู้ใช้ตัดสิน (ห้ามเริ่มจนได้คำตอบ)

1. **อนุมัติ rev2 นี้หรือไม่** (โดยเฉพาะ F3/F4/F5: ตัด `alive`, เพิ่ม `isAlive`, เปลี่ยน audit)
2. **อนุมัติให้แก้ `tools/save-split.py` เพิ่ม subcommand `preflight`** ตาม §13.5 หรือไม่
3. **VPN ค่ายไหน + เครื่อง guest** (เครื่องที่สอง หรือ 2 อินสแตนซ์) — กระทบ S2b ทั้งบล็อก
4. **ศึก 60 ยูนิต (30/ฝั่ง) เอามาจากไหน** — ถ้าไม่มี ให้อนุมัติลดเป้า (เช่น 20/ฝั่ง) พร้อมเหตุผลบันทึกในรายงาน
5. **แยก S2 เป็น S2a (loopback, ปิดได้ก่อน) + S2b (VPN acceptance)** หรือไม่
6. **อนุมัติถ้อยคำ protocol** (เพิ่ม `fieldHash`, `SNAPSHOT`, `AUTH/AUTH_RESULT/DENY/RESYNC`, `BATTLE_LOG`) ให้บันทึกใน `STAGEPLAN §5` ก่อนเขียนโค้ด

### 13.9 มติผู้อนุมัติ (2026-10-21)

| # | คำถาม | คำตอบ | ผลบังคับ |
|---|---|---|---|
| Q1 | อนุมัติ rev2 (F3/F4/F5) | **อนุมัติ** | replicate `isAlive`, ตัด `alive`, audit tuple แทน `stateHash` |
| Q2 | preflight ใน `tools/` | **อนุมัติ — เฉพาะ `tools/save-split.py` ไฟล์เดียว, ห้ามแก้ subcommand เดิม** | ส่ง diff ก่อน commit + ทดสอบโดยไม่เปิดเกม |
| Q3 | VPN / เครื่อง guest | **ยังไม่ตัดสิน — ไม่บล็อก S2a** | ต้องได้คำตอบก่อนเริ่ม S2b |
| Q4 | เป้า 60 ยูนิต | **คงเป้า 60 ยูนิต · S2a ต้องบันทึกจำนวนยูนิตทุกรอบ · ห้ามลดเป้าเอง** | รายงานทุก run ระบุ unit count จริง |
| Q5 | แยก S2a/S2b | **อนุมัติ** | S2a ปิดได้ด้วยผล loopback |
| Q6 | ถ้อยคำ protocol | **อนุมัติตาม §13** | type set + `fieldHash` ตาม §13.2/§13.4 · บันทึกถ้อยคำลง `STAGEPLAN §5` เมื่อปิด stage |
