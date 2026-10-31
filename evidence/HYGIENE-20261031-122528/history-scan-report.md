# history-scan-report — รอบ 8 (งาน 1)

> **read-only · ไม่มีค่า secret ในไฟล์นี้** · session `HYGIENE-20261031-122528` · 2026-10-31
> repo: `C:\Users\memph\LongYinCoop` (mod repo) · HEAD ตอนสแกน `41e0d8f` (+ working tree ที่แก้ `tools/` ในรอบนี้)
> scanner: `tools/scan_secrets.py` (v1 = พฤติกรรมเดิม, v2 = ชุดใหม่) · `tools/test_scan_secrets.py`

## 1. ขอบเขตและคำสั่งที่รันจริง

| # | คำสั่ง | ผล |
|---|---|---|
| 1 | `python tools/scan_secrets.py --all-history --patterns v1` | exit 1 · **1 finding** · 10.7 s |
| 2 | `python tools/scan_secrets.py --all-history --patterns v2` | exit 1 · **4 findings** · 11.7 s |
| 3 | `python tools/scan_secrets.py --tree --patterns v2` (หลังแก้ FP) | **exit 0 · 0 finding** |
| 4 | `python tools/scan_secrets.py --range origin/master..HEAD --patterns v2` | **exit 0 · 0 finding** |
| 5 | byte scan ค่า burned เทียบทุก blob ในประวัติ (สคริปต์ชั่วคราวนอก repo, ไม่พิมพ์ค่า) | **2704 blobs · 1 blob มีค่า** |

* `--all-history` = `git rev-list --all` (ทุก ref ที่อ้างถึงได้ + root) = **260 commits** · เนื้อหาแต่ละ commit อ่านผ่าน `git diff-tree --raw -z` + `git cat-file --batch` (chunked) แล้ว dedupe ตาม blob
* จำนวน blob ที่สแกนจริง (รายงานจากสคริปต์ byte-scan อิสระ): **2,704 blobs**
* ผลดิบ (stdout จริง แปะไว้ในไฟล์นี้แทนการแนบไฟล์แยก — โฟลเดอร์ evidence รอบนี้จำกัด 3 ไฟล์):

  ```
  $ python tools/scan_secrets.py --all-history --patterns v1     # exit 1
  evidence/S2a4-GO3-20261028-074003/net.cfg.during-GO3:5 (commit d81c86d8cb42, hex)
  scan_secrets: 1 finding(s)

  $ python tools/scan_secrets.py --all-history --patterns v2     # exit 1
  evidence/S2a4-GO3-20261028-074003/net.cfg.during-GO3:5 (commit d81c86d8cb42, hex)
  tools/s2_static_check.py:42 (commit 759a6f1505ce, token)
  tools/test_scan_secrets.py:26 (commit 116596663a7f, base64)
  tools/test_scan_secrets.py:216 (commit 116596663a7f, quoted)
  scan_secrets: 4 finding(s)
  ```

## 2. ชุด pattern v2 ที่ใช้ (สรุป)

เพิ่มจาก v1 (เฉพาะ key `*secret*` + ค่า hex/token ≥16) เป็น:

| กลุ่ม | เงื่อนไข | kind |
|---|---|---|
| key | `*secret*` / `*token*` / `*key*` + `=`/`:` (มี quote ครอบ key ได้) | — |
| ค่า quoted | ความยาว ≥ 8 (หลังถอด quote) | `quoted` |
| ค่า hex | ≥ 16 hex | `hex` |
| ค่า base64 | ≥ 24 จาก `[A-Za-z0-9+/]` (+ `=` ท้าย ≤2) | `base64` |
| ค่า token ทั่วไป | ≥ 16 จาก `[A-Za-z0-9+/=_-]` | `token` |
| private key | `-----BEGIN … PRIVATE KEY-----` | `private-key` |
| GitHub | `ghp_…` (≥20) / `github_pat_…` (≥20) **และ body มีตัวเลข + ตัวพิมพ์ใหญ่** | `github-token` |
| AWS | `AKIA…` (≥12) + มีตัวเลข | `aws-key-id` |
| Slack | `xox[baprs]-…` (≥10) + มีตัวเลข | `slack-token` |

**ยกเว้น placeholder:** `<hex>` `<redacted>` `<secret>` `<token>` `<key>` `<redacted-hex>` `REDACTED` `CHANGEME` `TODO` `none` `null` `...` · ค่าที่เป็น `x`/`.`/`*` ล้วน · **ค่าที่มี `...` หรือ `…` อยู่ข้างใน (masked preview เช่น `abc123...x9y8`)** — เหตุผล: ค่า hex/base64 จริงมีจุดไม่ได้
**กัน false positive:** key ที่มี substring `keys`/`keyboard`/`keybind`/`keycode`/`keydown`/`keyup`/`keyword`/`keymap`/`monkey`/`hockey` · `${ENV}`/`$VAR` reference · identifier ที่ฝัง `github_pat_` แต่ไม่มีตัวเลข/ตัวพิมพ์ใหญ่
เทสต์: **59 tests OK** (27 เดิมผ่านครบ + 32 ใหม่: จับได้ทุก pattern + false-positive ทุกกลุ่ม + `--all-history` + self-clean ของซอร์สสแกนเนอร์เอง)

## 3. ผลสแกน — แยก 3 กลุ่มตามคำสั่ง

### (ก) ที่รู้แล้ว / burned — **1 รายการ**

| commit | path:line | kind | สถานะ |
|---|---|---|---|
| `d81c86d` (2026-10-29 05:09) | `evidence/S2a4-GO3-20261028-074003/net.cfg.during-GO3:5` | hex | **burned · redact ที่ HEAD แล้ว** (`805902a`, 467 B → 413 B) · ค่าต้นฉบับ quarantined ที่ `coop-test/quarantine-secrets/SECRET-20261031-121438/` (untracked) |

**ยืนยันซ้ำด้วยวิธีอิสระ (byte scan ทั้งประวัติ):** ค่า burned ปรากฏใน **blob เดียวเท่านั้น** จาก 2,704 blobs = `2456c9378ad5` ซึ่งเป็นของ `d81c86d:evidence/S2a4-GO3-20261028-074003/net.cfg.during-GO3` — **ไม่มีสำเนาที่อื่นในประวัติทั้งสาย**

### (ข) อื่นที่อยู่บน origin — **3 รายการ · ทั้งหมดเป็น FALSE POSITIVE (พิสูจน์ที่ระดับค่า)**

| commit | path:line | kind | ค่าคืออะไร (ไม่พิมพ์ค่า) | สถานะที่ HEAD |
|---|---|---|---|---|
| `759a6f1` | `tools/s2_static_check.py:42` | token | **ชื่อคลาสในโค้ด** `PURE_LOGIC_TOKEN = "…"` (ค่าคือ `S2WriteGuardHook`) — key บังเอิญลงท้ายด้วย `_TOKEN` | แก้แล้ว: เปลี่ยนชื่อเป็น `PURE_LOGIC_MARKER` |
| `1165966` | `tools/test_scan_secrets.py:26` | base64 | **fixture สังเคราะห์ของเทสต์** `FAKE_TOKEN = "…"` (สร้างเองในไฟล์เทสต์ ไม่ใช่ credential) | แก้แล้ว: เปลี่ยนชื่อเป็น `FAKE_MIXED` |
| `1165966` | `tools/test_scan_secrets.py:216` | quoted | **fixture ไบนารีของเทสต์** (`b"secret = " + …` สำหรับทดสอบว่า blob binary ถูกข้าม) | แก้แล้ว: ประกอบสตริงแบบแยกส่วน |

⇒ **ไม่พบ secret ใหม่ · ไม่มี (ค) (commit ที่ยังไม่ push) เลย** ⇒ ตามเกณฑ์งาน 1 ข้อ 3 **ไม่สร้าง `evidence/SECRET-<ts>/` รอบนี้** (ของเดิม `SECRET-20261031-121438` = รอบ 7 คงไว้) · การจัดกลุ่ม (ข) ที่เป็น FP ระดับค่า + แก้ที่ HEAD แล้ว = บันทึกไว้ที่นี่แทน

### (ค) commit ที่ยังไม่ push — **0 รายการ**

ตรวจ:`--range origin/master..HEAD --patterns v2` = 0 finding · และ finding ทั้ง 4 อยู่บน commit ที่เป็น ancestor ของ origin/master ทั้งหมด (`git merge-base --is-ancestor` exit 0 ทั้งสาม commit)

## 4. หมายเหตุที่ทำให้ v2 เปลี่ยนผลจาก v1

1. mock/FP จาก "ชื่อ identifier" — v2 ต้องมี **guard เชิงโครง** (มีตัวเลข/ตัวพิมพ์ใหญ่ + benign-key list + ellipsis) ไม่ใช่ regex เดียว
2. **`--all-history` ต้องอ่านผ่าน `cat-file --batch` แบบ chunked** — เขียน hash ทั้งหมดลง stdin ก่อนแล้วค่อยอ่าน = deadlock เมื่อ pipe buffer เต็ม (เกิดจริงรอบแรก: ค้าง >10 นาที) → แก้เป็น `subprocess.run(input=…)` ทีละ 200 hash ⇒ 260 commits ใช้ ~11 s
3. โฟลเดอร์ `evidence/` ที่ถูก track ไว้มี placeholder `<hex>`/`<redacted>` จำนวนมาก — ถ้าเพิ่ม rule โดยไม่มี exclusion ชุดนี้ จะได้ finding ปลอมหลายสิบรายการ

## 5. ข้อจำกัด (ต้องถือ)

- สแกน **ตาม pattern ที่กำหนด** เท่านั้น — ค่าที่ถูกวางในที่ที่ไม่ใช่ key (`secret`/`token`/`key`) เช่นในประโยคข้อความ หรือ key ที่ไม่สื่อความ (เช่น `s = …`) **จะไม่ถูกจับ** (นอกขอบเขต pattern)
- `--all-history` ครอบ **ทุก ref ที่มีอยู่ใน clone นี้** — ถ้า origin มี ref ที่ local ยังไม่ fetch จะไม่ถูกสแกน (รอบนี้ `git fetch` แล้ว)
- ค่าที่ถูกลบออกจาก object store แล้ว (prune) หรืออยู่ใน quarantine untracked **ไม่ถูกสแกน**
- สถานะ "สะอาด ณ HEAD" ของรอบนี้ = `41e0d8f` + working tree (สแกน tree หลังแก้ FP แล้ว = 0)
