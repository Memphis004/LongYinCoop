# SECRET incident note — ค่า secret จริงในไฟล์ evidence

- **วันที่:** 2026-10-31 ~12:14 (รอบ 7 · session offline · ไม่เปิดเกม)
- **สถานะ:** ปิดด้วยการ redact + push (ไม่ rewrite history · ไม่ force)
- **ไฟล์:** `evidence/S2a4-GO3-20261028-074003/net.cfg.during-GO3` (บรรทัด 5)
- **commit ที่เพิ่มไฟล์/ค่า:** `d81c86d8cb4257d6ab22f72a3562c7728eefc463` (2026-10-29 05:09:00 +0700 · "Add world generation and hero creation tracking scripts")
- **ค่า secret:** **ไม่บันทึกในเอกสารนี้และที่อื่นใด** (แทนด้วย `<redacted>` ใน repo)

## A1 — ยืนยันตำแหน่ง (คำสั่งจริง + ผลจริง)

```
$ git fetch origin
(ไม่มี output)

$ git branch -r --contains d81c86d
  origin/HEAD -> origin/master
  origin/master

$ git merge-base --is-ancestor d81c86d origin/master ; echo exit=$?
exit=0

$ git log --oneline origin/master -1
409ac9b index: อัปเดต INDEX.md (งานบ้าน ข้อ 3 รอบ 6) ...

$ git log --oneline -1            # HEAD
409ac9b index: อัปเดต INDEX.md (งานบ้าน ข้อ 3 รอบ 6) ...

$ git rev-list --left-right --count origin/master...HEAD
0	0

$ git reflog show origin/master -5
409ac9b refs/remotes/origin/master@{0}: update by push
a7dd4ec ...@{1}: update by push
b814d61 ...@{2}: update by push
08a5ceb ...@{3}: update by push
928a8eb ...@{4}: update by push
```

**ผล:** `d81c86d` **อยู่บน `origin/master` แล้ว** และ `origin/master` = `HEAD` (0 ahead / 0 behind)
⇒ ตามตาราง A1 = ทางที่คาด ⇒ **ไป A2 · ถือว่า secret นั้นหมดอายุ (burned) · ไม่ rewrite · ไม่ force**

**ข้อเท็จจริงที่ต้องแก้รายงานรอบ 6:** รายงานรอบ 6 ระบุว่า `d81c86d` "ยังไม่เคยถูก push"
และ "34 commits ค้าง push" — **ไม่จริงแล้ว ณ 2026-10-31 12:14** · `decisions-pending.md` §4
(บันทึกว่า `origin/master = d81c86d` ก่อน push `d81c86d..c826961`) ตรงกับที่วัดได้ ·
การ push เกิดหลังรอบ 6 (reflog origin/master 5 รายการบนสุด = update by push ไล่ถึง `409ac9b`
= commit ปิดรอบ 6) — **ผู้ที่ push ไม่ปรากฏในบันทึกของ session ใดใน AGENTS.md = UNKNOWN**

## A2 — สแกน (เครื่องมือใหม่ `tools/scan_secrets.py`)

```
$ python tools/scan_secrets.py --tree
evidence/S2a4-GO3-20261028-074003/net.cfg.during-GO3:5 (hex)
scan_secrets: 1 finding(s)
exit=1

$ python tools/scan_secrets.py --range origin/master..HEAD
scan_secrets: 0 finding(s)
exit=0            # ช่วงว่าง (origin/master == HEAD) => ไม่มี commit ค้าง push ตอนสแกน

$ git ls-files | grep net.cfg
evidence/S2a4-GO3-20261028-074003/net.cfg.during-GO3      # ไฟล์เดียว
```

**ผล:** พบ **ไฟล์เดียว** ตรงตามที่คาด (ไม่มีไฟล์อื่น ไม่มีในโค้ด/เอกสาร) ⇒ ไปต่อได้

## A3 — การดำเนินการ

| ขั้น | รายละเอียด |
|---|---|
| กักต้นฉบับ | `coop-test/quarantine-secrets/SECRET-20261031-121438/net.cfg.during-GO3.original` (untracked · `.gitignore` มี `coop-test/**` อยู่แล้ว) — **ไม่ลบทิ้ง** |
| sha256 ต้นฉบับ | `5ce166da52fbbcd031495f6f8e974660b5d9cc7e21205947aad6c1733d50a5a5` (467 B) — ตรงกันทั้งไฟล์ใน repo ก่อนแก้และสำเนาใน quarantine |
| redact | แทนค่าในบรรทัด 5 ด้วย `<redacted>` เท่านั้น · บรรทัดอื่นไม่แก้ · 1 insertion / 1 deletion |
| sha256 หลัง redact | `9d28fcda78321fc44aad96f457d5dc3dde0179339beef2100891079122769afb` (413 B) |
| commit redact | `805902a` (commit ปกติบน HEAD · ไม่ amend · ไม่ rebase) |
| ยืนยันซ้ำ | `python tools/scan_secrets.py --tree` = 0 findings |

## ข้อกำหนดต่อไป (ผลผูกพัน)

1. **ห้ามนำค่า secret นี้กลับมาใช้** ไม่ว่ากรณีใด (มันอยู่ในประวัติของ remote แล้ว = burned)
2. **สุ่มค่าใหม่ทุกครั้ง** ก่อนเปิดใช้เครือข่าย (ก่อน S3 / ก่อนวัดใด ๆ ที่เปิด `net.cfg`)
   — ค่าใหม่ต้องไม่ถูกคัดลอกลง evidence; ถ้าต้องเก็บหลักฐานให้ใช้ `<redacted>` เท่านั้น
3. ก่อน commit ทุกครั้งที่แตะ `evidence/**`: รัน `python tools/scan_secrets.py --tree`
   และก่อน push: เพิ่ม `--range origin/master..HEAD`
4. `coop-test/quarantine-secrets/**` **ห้าม commit** (untracked ตาม `.gitignore`) และห้าม push

## UNKNOWN (ยืนยันไม่ได้จากหลักฐานที่มี)

- **ไม่ได้ยืนยันว่า secret เดิม "สุ่มรายรอบ" หรือ "คงที่"** — ไม่มีบันทึกว่าใครเป็นผู้สร้างค่า
  นี้หรือสร้างเมื่อไร (สมมติฐานที่ใช้ได้: ค่าที่ใช้ใน GO-3 2026-10-28 ถูกคัดลอกลง evidence
  โดย session GO-3 ระหว่างวัด) ⇒ ถือว่า **ค่าที่เคยใช้ = compromised** ไม่ว่าที่มาจะเป็นแบบใด
- ไม่ทราบว่า `net.cfg` จริงบนเครื่อง (`12541f34…` · `enabled=false`) เคยใช้ค่าเดียวกันหรือไม่
  — **ไม่ตรวจค่า** (ห้ามพิมพ์เนื้อหา) · ข้อบังคับข้อ 2 ครอบเรื่องนี้อยู่แล้ว
- ไม่ทราบผู้ที่ push commits รอบ 6 ขึ้น origin (ดูหัวข้อ A1)

## อ้างอิง

- `tools/scan_secrets.py` · `tools/test_scan_secrets.py` (27 tests OK)
- commit: `1165966` (tools) · `805902a` (redact) · `d81c86d` (ต้นเหตุ)
