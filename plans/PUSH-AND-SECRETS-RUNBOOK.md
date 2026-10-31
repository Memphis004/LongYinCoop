# PUSH-AND-SECRETS-RUNBOOK

> **สถานะ:** เอกสารขั้นตอน (docs-only) · 2026-10-31 · รอบ 8
> ใช้กับ **mod repo** (`C:\Users\memph\LongYinCoop`) เท่านั้น · game repo = ไม่ push (มติ `[PUSH-GAME-REPO] = ไม่อนุมัติ`)
> เครื่องมือที่เกี่ยวข้อง: `tools/scan_secrets.py` (สแกน · อ่านอย่างเดียว · ไม่พิมพ์ค่า), `tools/githooks/pre-push` (hook ที่ track ใน repo), `tools/test_pre_push_hook.py`, `tools/save-split.py`

---

## 1. ขั้นก่อน push ทุกครั้ง (ลำดับบังคับ · ห้ามข้าม)

1. **`git fetch`** แล้วยืนยันว่า push ได้แบบ fast-forward
   ```
   git -C <mod repo> fetch
   git -C <mod repo> merge-base --is-ancestor origin/master HEAD   # ต้องได้ exit 0
   git -C <mod repo> rev-list --left-right --count origin/master...HEAD
   ```
   origin/master ต้องเป็น **ancestor ของ HEAD** และ **ไม่ขยับ** ระหว่างตรวจกับตอนสั่ง push (ถ้าขยับ = หยุด ทบทวนใหม่)
2. **ตรวจเนื้อที่จะขึ้น:** `git log origin/master..HEAD --stat` ต้อง **ไม่มี**
   `*.dll` / `*.pdb` / `dump.cs` / `Il2CppAssemblies/` / `GlobalMetadata` / ไฟล์เซฟ (`Save/…`, `SaveSlot*`, `PlayerprefData.dat`) /
   `net.cfg` ที่ยังมีค่าจริง · ไฟล์ binary ใด ๆ ที่ไม่ตั้งใจ
3. **สแกน secret (pattern v2)** — ต้อง **0 finding**:
   ```
   python tools/scan_secrets.py --range origin/master..HEAD --patterns v2     # exit 0 = สะอาด
   python tools/scan_secrets.py --tree --patterns v2                          # tree ปัจจุบัน
   ```
   (option) สแกนประวัติทั้งสายเมื่อ suspect: `python tools/scan_secrets.py --all-history --patterns v2` (~11 s บน repo นี้)
4. **รัน hook จริง** — push ต้องผ่าน `pre-push` ของ repo (ติดตั้งตาม §3) · **ผลลัพธ์ที่ต้องเห็น:** `pre-push: <ref> clean` และ `pre-push: secret scan passed`
   * **ห้ามใช้ `--no-verify` ทุกกรณี** (รวมงานด่วน)
   * ถ้า hook บล็อก = **หยุด** ทำตาม §4 ห้ามเลี่ยง
5. **ไม่มี force push** — `--force` / `--force-with-lease` ต้องมีมติผู้ใช้แยกทุกครั้ง (สวิตช์ `[FORCE-PUSH]` ค่าเริ่มต้น = ไม่อนุมัติ)
6. **บันทึกผล:** hash origin/master **ก่อน/หลัง** (และ commit range ที่ push) ลง `decisions-pending.md` §4
   (บันทึกเป็น commit local แยก และ **ไม่ push commit บันทึกนั้น** เว้นแต่ผู้ใช้อนุมัติรอบถัดไป)

**สรุปเช็กลิสต์ (copy ใช้ได้):**

```
[ ] fetch + origin/master เป็น ancestor + ไม่ขยับ
[ ] log origin/master..HEAD --stat ไม่มี binary/dll/pdb/dump.cs/Il2CppAssemblies/เซฟ/net.cfg มี secret
[ ] scan_secrets --range ... --patterns v2 = 0 finding
[ ] scan_secrets --tree --patterns v2 = 0 finding (หรือมีแต่ known-FP ที่ระบุไว้)
[ ] push ผ่าน hook จริง (ไม่ใช้ --no-verify, ไม่ force)
[ ] บันทึก hash ก่อน/หลัง + commit range ลง decisions-pending.md §4
```

---

## 2. กฎเรื่อง secret

| กฎ | รายละเอียด |
|---|---|
| **ห้าม commit ค่าจริง** | ค่า secret ของ `net.cfg` (`UserData\LongYinCoop\net.cfg`) ห้ามอยู่ในไฟล์ที่ track ทุกชนิด — โค้ด เอกสาร evidence log รายงาน |
| **สุ่มใหม่ทุกครั้งก่อนใช้เครือข่าย** | ก่อนเปิด `net.cfg` (`enabled=true`) ทุกครั้ง และ **ก่อนเริ่ม S3** ต้องสุ่มค่าใหม่ · ตัวอย่างวิธีสุ่ม **ในเครื่องผู้ใช้เอง** (repo นี้ไม่รันให้และไม่บันทึกผล): `python -c "import secrets; print(secrets.token_hex(32))"` |
| **burned = ห้ามนำกลับมาใช้** | ค่าที่เคยหลุดเข้า git history (เช่นค่าใน `evidence/S2a4-GO3-…/net.cfg.during-GO3` ที่ redact แล้ว) ถือว่า **compromised ถาวร** — ห้าม reuse แม้จะดู "เป็นส่วนตัว" |
| **redact ก่อน commit** | evidence/ไฟล์ใดที่จำเป็นต้องเก็บเนื้อ `net.cfg` ให้แทนค่าด้วย `<redacted>` (หรือ `<hex>`) **ก่อน** commit — ดู `tools/scan_secrets.py` ว่า placeholder อะไรถือว่าสะอาด |
| **ห้ามพิมพ์ค่าลงแชต/รายงาน** | รายงานให้อ้าง `path:line` + จำนวนเท่านั้น · ห้ามคัดลอกค่าลง `decisions-pending.md`/AGENTS/evidence |
| **ค่า secret = per-machine** | `net.cfg` เป็นของเครื่อง ไม่ใช่ของ repo · เก็บนอก git เสมอ (`UserData\LongYinCoop\net.cfg` ถูก ignore) |
| **ควอรันต์ก่อน commit ที่แตะ `evidence/**`** | `python tools/scan_secrets.py --tree --patterns v2` — exit 0 ก่อน `git add` |

---

## 3. pre-push hook (ติดตั้ง / ถอน / ข้อจำกัด)

**ไฟล์ใน repo:** `tools/githooks/pre-push` (LF เท่านั้น — `.gitattributes` ปัก `tools/githooks/* text eol=lf` กัน CRLF พังบน Git for Windows)

**ติดตั้ง (ต้องทำทุก clone — config ไม่ถูก track):**
```
git -C <mod repo> config --local core.hooksPath tools/githooks
git -C <mod repo> config --local core.hooksPath        # ตรวจว่าตั้งติด
```
**ถอน:**
```
git -C <mod repo> config --local --unset core.hooksPath
```

**พฤติกรรม:** อ่าน stdin ตาม protocol ของ git (`<local ref> <local sha> <remote ref> <remote sha>`) ต่อ ref
* ref ปกติ → สแกนช่วง `remote_sha..local_sha`
* **ref ใหม่** (remote sha = `000…0`) → สแกนด้วย `git rev-list <local_sha> --not --remotes`
* **ลบ ref** (local sha = `000…0`) → ข้าม
* **fail-closed:** ไม่พบ python (`python` / `python3` / `py -3`) · ไม่พบ `tools/scan_secrets.py` · scanner error (exit 2) ⇒ **บล็อก push** พร้อมข้อความ
* พิมพ์เฉพาะ `commit:path:line` — ไม่พิมพ์ค่า

**ข้อจำกัดที่ต้องรู้:**
* `core.hooksPath` เป็น **local config** — clone ใหม่/เครื่องใหม่ **ไม่มี hook จนกว่าจะตั้งเอง** (เอกสารนี้คือที่จด)
* hook ตรวจเฉพาะ **สิ่งที่กำลังจะขึ้น** (delta) — ไม่ตรวจประวัติเก่าทั้งสาย ต้องรัน `--all-history` เองเมื่อสงสัย
* hook เป็น gate ชั้นสอง ไม่ใช่ชั้นเดียว — ขั้น §1.3 ยังต้องรันด้วยมือ
* `--no-verify` ข้าม hook ได้เชิงเทคนิค — **ห้ามใช้** (วินัย + code review)

**ทดสอบ hook:** `python tools/test_pre_push_hook.py` (E2E กับ bare repo ชั่วคราวนอก repo: clean push ผ่าน · secret ถูกบล็อก · ref ใหม่ · ลบ ref · ไม่มี python · scanner หาย/พัง = บล็อก)

---

## 4. เมื่อสแกนพบ secret

1. **หยุดทันที** — อย่า push ต่อ (ถ้าเป็นของที่ยังไม่ push = ยัง controllable)
2. **ห้าม rewrite history บน origin** (`rebase`/`filter-repo`/`push --force`) — ต้องมีมติผู้ใช้แยกทุกครั้ง และค่าเริ่มต้นคือ **ไม่อนุมัติ**
3. **เขียน incident note** ใน `evidence/SECRET-<yyyymmdd-hhmmss>/incident-note.md` — บันทึก **เฉพาะ** `commit:path:line` + จำนวน + กลุ่ม (บน origin / ยังไม่ push) + วิธีที่พบ · **ห้ามมีค่า**
4. **ประเมินว่าค่า "burned" หรือไม่:** ถ้าอยู่บน origin แล้ว ⇒ ถือว่า burned ⇒ **ห้ามนำกลับมาใช้** ต้องสุ่มใหม่ก่อนใช้เครือข่ายครั้งถัดไป
5. **redact ที่ HEAD** (ถ้าค่าปรากฏในไฟล์ที่ยัง track): แทนด้วย `<redacted>` เป็น commit แยก + บันทึก sha256 ของไฟล์ก่อน/หลังลง incident note · การ redact **ไม่ลบ** ค่าออกจากประวัติเก่า (แก้ได้เฉพาะมติ rewrite เท่านั้น)
6. **ผู้ใช้ตัดสิน** ว่าจะ (ก) redact ที่ HEAD แล้วอยู่ต่อ · (ข) rewrite history (ต้องอนุมัติแยก) · (ค) ยอมรับความเสี่ยงเป็นลายลักษณ์อักษร
7. บันทึกผลลง `decisions-pending.md` §4 (push log) และอัปเดต `AGENTS.md` ถ้าเป็นข้อเท็จจริงที่ต้องถือต่อ

---

## 5. อ้างอิง

* `tools/scan_secrets.py` — scanner v1/v2 (`--tree`, `--range`, `--all-history`, `--patterns v1|v2|all`, `--json`) · exit 0 สะอาด / 1 พบ / 2 error
* `evidence/HYGIENE-20261031-122528/history-scan-report.md` — ผลสแกนประวัติทั้งสายรอบ 8
* `evidence/SECRET-20261031-121438/incident-note.md` — incident secret รอบ 7 (redact แล้ว)
* `AGENTS.md` — กติกา MASTER PROMPT (ข้อ 5/9/10 ที่เกี่ยวข้องกับความปลอดภัยของเซฟ/เวอร์ชัน)
