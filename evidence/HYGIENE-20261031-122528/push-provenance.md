# push provenance — ใคร push อะไรขึ้น origin เมื่อไร (งาน 4a · read-only)

- **วันที่วัด:** 2026-10-31 (session `HYGIENE-20261031-122528`)
- **ขอบเขต:** อ่านอย่างเดียว — `git reflog show origin/master`, `git log`, `git ls-remote`, `git rev-list` (ไม่ fetch ไม่ push ในขั้นนี้)
- **repo:** `C:\Users\memph\LongYinCoop` (mod repo) · origin = GitHub `Memphis004/LongYinCoop` (private)
- **ป้ายหลักฐาน:** ทุกข้อในไฟล์นี้เป็น **[RUNTIME-git]** = อ่านจาก git object/reflog ของ repo นี้เอง เว้นแต่ระบุอย่างอื่น
- **ไฟล์นี้ไม่มีค่า secret ใด ๆ** — ค่าที่อ้างถึงแสดงเป็น `<redacted>` หรืออธิบายด้วย hash เท่านั้น

---

## 1) ตาราง push ทั้งหมดบน `origin/master` (reflog 23 รายการ)

`git reflog show origin/master --date=iso` — ทุกบรรทัดมี `: update by push` (ไม่ใช่ `fetch`/`clone`)
คอลัมน์ `+commits` = `git rev-list --count <tip_เก่า>..<tip_ใหม่>` (จำนวน commit ที่ push รอบนั้นเพิ่มจริง)

| # | เวลา (push) | tip ใหม่ | +commits | บันทึกใน `decisions-pending.md` §4? |
|---|---|---|---|---|
| 1 | 2026-10-31 12:16:27 | `b70cfd7` | 3 | **มี** (รอบ 7 A4) |
| 2 | 2026-10-31 11:50:49 | `409ac9b` | **34** | **มี** (รอบ 6/7 — ระบุว่า "เกิดจริงหลังรายงาน ผู้ทำ = UNKNOWN") |
| 3 | 2026-10-31 00:19:04 | `a7dd4ec` | 5 | **ไม่มี** (มี testimony ใน commit `fb831fe` — ดู §3) |
| 4 | 2026-10-30 23:55:44 | `b814d61` | 1 | **ไม่มี** (มี testimony ใน HANDOFF ของ `a7dd4ec`) |
| 5 | 2026-10-30 07:13:17 | `08a5ceb` | 1 | **ไม่มี** (มี testimony ใน `b814d61` — "parallel session") |
| 6 | 2026-10-30 07:12:56 | `928a8eb` | 6 | **ไม่มี** (testimony เดียวกัน: "full line pushed by parallel session") |
| 7 | 2026-10-30 06:17:38 | `772c8f2` | 1 | **ไม่มีใน §4** (ถูกอ้างถึงใน §11 ว่า "fetch เจอ = superset") |
| 8 | 2026-10-30 05:09:42 | `b58d553` | 3 | **มี** (§11 — ระบุชัดว่าเป็นการ push ของ session นั้นเอง) |
| 9 | 2026-10-30 04:36:03 | `3131df8` | 2 | **มี** (บันทึกว่า "พบภายหลัง actor = UNKNOWN") |
| 10 | 2026-10-30 03:29:26 | `7396b94` | 3 | **มี** (บันทึกว่า "actor = UNKNOWN") |
| 11 | 2026-10-30 03:05:25 | `3493bfb` | 2 | **มี** |
| 12 | 2026-10-30 02:16:15 | `f088736` | 4 | **มี** |
| 13 | 2026-10-30 01:17:04 | `a998509` | 5 | **มี** |
| 14 | 2026-10-30 00:45:25 | `8816748` | 3 | **มี** |
| 15 | 2026-10-30 00:25:47 | `c826961` | 8 | **มี** (มติ "8 commits `c07ae7c..c826961`") |
| 16 | 2026-10-29 05:09:07 | `d81c86d` | 1 | ไม่พบใน §4 (ก่อนยุคที่มี ledger — **commit นี้คือ commit ที่พา secret เข้า history**) |
| 17 | 2026-10-29 05:08:24 | `7036224` | 35 | ไม่พบใน §4 (ยุคเดียวกัน) |
| 18 | 2026-10-28 02:32:23 | `84c6874` | 2 | ไม่พบใน §4 |
| 19 | 2026-10-27 22:49:08 | `7ff47ab` | 1 | ไม่พบใน §4 |
| 20 | 2026-10-27 01:35:38 | `dd13491` | 21 | ไม่พบใน §4 |
| 21 | 2026-10-24 20:52:11 | `1f84983` | 45 | ไม่พบใน §4 |
| 22 | 2026-10-21 01:26:23 | `ded325e` | 68 | ไม่พบใน §4 |
| 23 | 2026-10-20 05:04:58 | `3648c5d` | 68 (จาก root) | ไม่พบใน §4 |

**สรุปเชิงตัวเลข:** push ทั้งหมด 23 รอบ · ledger §4 ครอบ **10 รอบ** · **ไม่ครอบ 13 รอบ** (5 รอบมี testimony ใน commit/HANDOFF ของ repo: #3–#7 · 8 รอบอยู่ในยุคก่อนมี ledger: #16–#23)

> หมายเหตุ: ตัวเลข +68 ของ #23 คำนวณจาก `git rev-list --count 3648c5d` (root) ไม่ใช่ส่วนต่างจาก tip ก่อนหน้า เพราะเป็น push แรกของ repo

---

## 2) ใครเป็นคน push — git ตอบไม่ได้

git **ไม่เก็บผู้ทำ push** เก็บแต่ผู้เขียน/ผู้ commit และชื่อ remote-tracking ที่ถูกอัปเดต (`reflog` = local record ของ "tip ถูกเปลี่ยนเพราะ push จากเครื่องนี้") ข้อเท็จจริงที่วัดได้:

| ข้อ | ค่าที่วัดได้ | ที่มา |
|---|---|---|
| author ทุก commit ใน `7036224..HEAD` (83 commits) | `Westernholudur <westernholdur@gmail.com>` ทั้งหมด | `git log --pretty='%an <%ae>'` + Sort-Unique |
| committer ทุก commit ในช่วงเดียวกัน | `Westernholudur <westernholudur@gmail.com>` ทั้งหมด (ค่าเดียวกับ author) | `git log --pretty='%cn <%ce>'` |
| reflog entry ทุกบรรทัด | `: update by push` (ไม่มี `fetch`/`clone`/`rebase`) | §1 |
| tag บน origin | `stage-N3` (`3131df8`), `stage-N4` (`994d9c7`) | `git ls-remote --tags origin` |

⇒ **ตัวตนผู้ทำ push = UNKNOWN** ตามข้อเท็จจริงนี้ git ไม่มีข้อมูลให้ชี้ตัว (ทุก commit เป็น identity เดียวกันหมด — เป็นเครื่อง/บัญชีเดียว ไม่ใช่หลักฐานว่าเป็นคนเดียวกันในแต่ละรอบ)

### 2.1 testimony ที่มีอยู่ใน repo (ไม่ใช่การวัด — label [TESTIMONY])

- **push #4 (`b814d61`, 1 commit):** HANDOFF ฝังใน commit `a7dd4ec` ระบุ *"push b814d61 สำเร็จตามมติ (fetch ก่อน · origin `08a5ceb`..`b814d61` fast-forward · ls-remote ตรงเป๊ะ · ไม่ force)"*
- **push #5–#6 (`928a8eb`, `08a5ceb`):** commit `b814d61` ระบุ *"current origin state noted: master = `08a5ceb` (full line pushed by parallel session incl. N4 evidence + CLOCK-1 + HANDOFF docs)"* ⇒ ผู้ทำ = "session คู่ขนาน" (ไม่ระบุตัว)
- **push #3 (`a7dd4ec`, 5 commits):** commit `fb831fe` ระบุ *"push 5 commits (`9cf1a19..a7dd4ec`) ตามมติ 2026-10-31"* — แต่ `decisions-pending.md` §4 ไม่มีบรรทัดนี้
- **push #2 (`409ac9b`, 34 commits):** §4 บันทึกตรง ๆ ว่า *"push รอบ 6 เกิดจริงหลังรายงาน (ผู้ทำ = UNKNOWN ไม่ได้บันทึกใน AGENTS.md)"*
- **push #9–#10 (`3131df8`, `7396b94`):** §4 บันทึกว่าเป็น push ที่ session นั้น **ไม่ได้ทำ** และหาสาเหตุไม่ได้ (ตรวจ hook/alias/scheduled task แล้วไม่พบ) ⇒ actor = UNKNOWN

**ข้อสรุปที่ถือได้:** การ push ในรอบ #3–#7 **มี testimony ในประวัติ commit** ว่าเป็นการกระทำที่ตั้งใจและผ่าน FF (ไม่ force) — แต่ **ไม่มีหลักฐานระดับระบบ (process/audit log) ชี้ตัวผู้สั่ง** แต่อย่างใด

### 2.2 ทำไมยังชี้ตัวไม่ได้ (ข้อจำกัดของการสืบ)

- ไม่มี `GIT_TRACE_PUSH`/server-side audit log (repo private บน GitHub — ไม่ได้เปิด audit log ของ org)
- reflog เป็น **local** — ถ้า push เกิดจาก clone อื่น/เครื่องอื่น reflog ที่นี่จะไม่บันทึก (แต่กรณีนี้ reflog มี entry ⇒ push มาจาก **clone นี้** หรือ clone ที่ใช้ reflog ร่วม)
- Windows process audit (4688) ปิดอยู่ — บทเรียนจาก incident 2026-10-21 (`evidence/S2a2-20261021-2254/launch-forensics-2254.md`)

---

## 3) secret ใน `net.cfg` — มาจากไหน (กลไก)

**คำถาม:** ค่า 64-hex ใน `UserData\LongYinCoop\net.cfg` ถูกสร้างโดยโค้ดใน repo หรือพิมพ์มือ?

| ข้อ | ผล | ที่มา |
|---|---|---|
| โค้ดใน `src/` ที่สร้าง secret สำหรับ net.cfg | **ไม่พบ** | สแกน `*.cs` ด้วย `NewNonce|RandomNumberGenerator|secrets.|token_hex|urandom|uuid4` ⇒ เจอเฉพาะ `S2Auth.NewNonce()` = `RandomNumberGenerator.GetBytes(NonceBytes)` (nonce ของ handshake, `src\LongYinCoop\S2\Protocol\S2Auth.cs:15` เรียกที่ `S2Handshake.cs:262`) — **ไม่ใช่** ค่าใน net.cfg |
| โค้ดที่**เขียน** net.cfg | **ไม่พบ** | ทุกจุดที่อ้าง `net.cfg` เป็นการ **อ่าน** (`S2NetConfig`, `S2HostService` poll 1 Hz, `Sp5NpcGate`) — ไม่มี writer/generator |
| `net.cfg` ถูกอ่านฝั่งไหน | host + guest (`S2OverlayService` แปลง hex → `byte[]` ด้วย `S2Auth.BytesFromHex`) | `src\LongYinCoop\S2\Guest\S2OverlayService.cs:56` |
| hash ของ `net.cfg` ปัจจุบัน | `12541f34c63389ce3192a290b6802ff933fcce843f5eae17200a8e78187aecc8` · 444 B | `Get-FileHash` 2026-10-31 (session นี้) — ตรงกับ `VERSION.md` |
| hash นี้เปลี่ยนระหว่าง session ก่อน ๆ ไหม | **ไม่เปลี่ยน** (คงที่ตลอด S2a-3 → S2a-4 → V1 → รอบนี้) | เทียบกับบันทึก `VERSION.md` ทุกหัวข้อ |

**ข้อสรุป [INFERRED-from-code]:** ค่า secret ถูก**วางด้วยมือ** (หรือโดยเครื่องมือภายนอก repo) ครั้งเดียวแล้วคงที่ — **ไม่มีกลไก rotate ในโค้ด** และไม่มีบันทึกว่าใคร/เมื่อไรสร้าง ⇒ *วิธีสร้าง = UNKNOWN* (จะ "สุ่มรายรอบ" หรือ "สุ่มครั้งเดียวแล้วใช้ตลอด" ก็อธิบาย hash ที่คงที่ได้ทั้งคู่ เพราะไม่เคยมีรอบที่สั่ง rotate)

---

## 4) ⚠ ค่า secret ที่ burned **ยังอยู่ใน `net.cfg` จริงบนเครื่อง**

| ข้อ | ผล |
|---|---|
| `UserData\LongYinCoop\net.cfg` มี key `secret` (64 hex) | **มี** |
| ค่านั้น **เท่ากับ** ค่าใน quarantine ที่ burned แล้วหรือไม่ | **เท่ากัน** (เทียบ sha256 ของ *ค่า* ทั้งสองฝั่ง — `same value: True` · ไม่พิมพ์ค่า) |
| ไฟล์ quarantine ที่ใช้อ้างอิง | `coop-test\quarantine-secrets\SECRET-20261031-121438\net.cfg.during-GO3.original` (untracked · ห้าม commit) |
| ผลต่อความปลอดภัย | **ค่าเดียวกันที่อยู่ใน public-ish history (repo private แต่ history มีค่า) ยังถูกใช้เป็น shared secret ของ listener** ⇒ ต้อง **rotate ก่อนเปิดเครือข่ายครั้งถัดไป** (ก่อน S3 หรือก่อนวัดใดที่ตั้ง `enabled=true`) |

**ขั้นตอน rotate ที่ runbook กำหนด (ยังไม่ได้ทำในรอบนี้ — ต้องมีมติ/ช่วงเวลา):**
1. สุ่มค่าใหม่ 32 byte (64 hex) ด้วยเครื่องมือที่ไม่เขียนลง evidence
2. เขียนทับ `secret = <ค่าใหม่>` ใน `UserData\LongYinCoop\net.cfg` (คง key อื่น)
3. **ห้าม** บันทึกค่าใหม่ลง `evidence/**`, `VERSION.md`, commit message หรือ log ใด ๆ — ใช้ `<redacted>` + sha256 ของไฟล์เท่านั้น
4. บันทึก hash ของ `net.cfg` ใหม่ใน `VERSION.md` (divergence record) แล้วอัปเดต baseline ของ net.cfg ในเอกสาร

---

## 5) วิธีที่ใช้ + ข้อจำกัด

**ทำ:** `git reflog show origin/master --date=iso` (23 entries) · `git rev-list --count` ต่อช่วง · `git log --pretty` ช่วง `7036224..HEAD` (83 commits) · `git ls-remote --tags origin` · อ่าน `S2Auth.cs`/`S2NetConfig.cs`/`S2HostService.cs`/`S2OverlayService.cs` (เฉพาะบรรทัด secret) · `Get-FileHash` ของ `net.cfg` · เทียบ sha256 ของค่าที่อ่านจากสองไฟล์โดยไม่พิมพ์ค่า

**ไม่ได้ทำ (และเหตุผล):**
- ไม่ fetch/ไม่ดึง audit log จาก GitHub server — ต้องใช้ credential ของ org และไม่ใช่ขอบเขตงานนี้
- ไม่เปิด process audit / ไม่ได้ติดตาม process ในอดีต — ทำไม่ได้ย้อนหลัง (บทเรียน 2026-10-21)
- ไม่ได้ตรวจว่า `net.cfg` ถูกแก้ด้วย editor ใด — ไม่มีร่องรอย (ไม่มี NTFS USN journal ที่อ่านได้ในขอบเขตนี้)
- **ไม่ rotate secret** — เป็นการเปลี่ยน state ของเครื่อง นอกขอบเขตงาน 4a (read-only)

**สิ่งที่ยังตอบไม่ได้:** ใครเป็นผู้ push #3–#7 และ #9–#10 · ค่า secret ถูกสร้างด้วยวิธีใด · ค่าเดียวกันถูกใช้ตั้งแต่เมื่อไร (ไม่มีบันทึกก่อน S2a-2)
