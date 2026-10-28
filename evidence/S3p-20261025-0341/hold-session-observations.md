# Hold Session — read-only observations (มติผู้ใช้ (ค) · 2026-10-25 ~05:0x)

โหมด: เฝ้าเซสชันที่กำลังรัน แบบ **read-only เท่านั้น** — ไม่เปิด/ปิดเกม ไม่ส่ง IPC ใด ๆ (รวม `spike status`) ไม่ deploy/swap DLL ไม่แตะ save/baseline/VERSION.md ไม่ commit/tag

## 1. Process / DLL (ตรวจซ้ำ 2 ครั้ง — ค่าคงเดิมทั้งสองครั้ง)

| รายการ | ค่า | หลักฐาน |
|---|---|---|
| process | `LongYinLiZhiZhuan.exe` **pid 27872** (คงเดิมตลอดการสังเกต) | tasklist |
| boot | 04:56:37 · log `MelonLoader/Logs/26-10-25_4-56-32.log` · `stage=S2a-3 version=0.5.0` · `IPC listening \\.\pipe\longyin-coop-27872` · `allowRemoteInput=False` | boot log |
| DLL ที่โหลด | **`5d7b7cc5520ae784…` = build เก่า S2a-3** (rollback หลัง crash — build ทดสอบ `f2ad30cd…` ยังพักอยู่ที่ `Mods\LongYinCoop.dll.s3prereq-f2ad30cd`) | sha256 ไฟล์ใน `Mods\` (ไม่ได้สั่ง swap ใด) |
| stateHash | `battleframe/v1` (build เก่า — อ่านครั้งเดียวก่อนมติ hold · หลังจากนั้นไม่มี IPC อีก) | spike status ครั้งสุดท้ายก่อนมติ |

## 2. Save observations (read-only · ใช้ฟังก์ชันของ `tools/save-split.py` เอง: `hash_tree`/`read_hashfile`/`diff_hashes`/`digest_of`)

สังเกต 2 จุดเวลา (T0, T0+12 s) — **ผลเหมือนกันทั้งสองจุด (นิ่ง)**:

| เทียบกับ | changed | missing | added | รายการที่เปลี่ยน |
|---|---|---|---|---|
| `S3p-20261025-0341/savehash.copy.before.txt` | 1 | 0 | 0 | `PlayerprefData.dat` |
| `baselines/20261024-232024/savehash.real.before.txt` | 1 | 0 | 0 | `PlayerprefData.dat` |

- **files=57 · key set ตรง baseline ทุกไฟล์ (ไม่มีไฟล์เพิ่ม/หาย)** · digest ปัจจุบัน `693525697aab…` = สถานะ `7332fc93eb98…` + pref เปลี่ยน 1 ไฟล์
- **`SaveSlot*` ทุกไฟล์ตรง baseline** (การเทียบครบทั้ง 57 ไฟล์ด้วย parser ของ tool — แก้บั๊กสคริปต์เก่าที่ parse CRLF ผิด; รายงานก่อนหน้าที่เขียนว่า "เทียบครบแล้ว" ครั้งแรกมาจากสคริปต์ที่ parse ผิด ครั้งนี้เทียบใหม่ด้วยโค้ด tool เอง)
- การเปลี่ยนแปลงเดียว = **volatile pref** — ตรง precedent (เกมเขียน pref ตอนเปิด/ปิด · rebaseline volatile-only เคยอนุมัติเป็นปกติ)
- **ข้อจำกัดที่ยังเปิด:** เซฟจริงยังไม่มีการเขียน `SaveSlot*` ใด ๆ **ณ จุดสังเกต** — ถ้าผู้ใช้ save ในเกมหลังจากนี้ ไฟล์จะเปลี่ยน (เป็นการเซฟของผู้ใช้เองบนเซฟจริง ไม่ใช่ผลจากการทดสอบ — ไม่มีการยิงใดทั้งสิ้น) · ต้องสังเกตซ้ำถ้าต้องการยืนยันต่อเนื่อง

## 3. A5 — control flow หลัง audit write failure (offline review · ไม่แก้ implementation)

**ผังจริง (`tools/negative-tests.py`):** ทุกจุดที่ `emit()` คืน False ⇒ `aborted = "audit-trail-write-failed"` + `break` **ทันที** — ไม่มีการยิงเพิ่ม:

| จุด emit | line | หลัง fail |
|---|---|---|
| NOT-RUN (hash-unstable) | :413–415 | abort + break |
| NOT-RUN (premise) | :433–434 | abort + break |
| pre-fire (ก่อนยิง) | :450–456 | abort + break — **ยังไม่ยิง** |
| terminal UNKNOWN (ยิงแล้วอ่านหลังไม่ได้) | :497 | best-effort + break (aborted ตั้งจากสาเหตุเดิม) |
| terminal PASS/FAIL | :529 | **downgrade เป็น UNKNOWN/INCOMPLETE + `failed=True`** + break (:547–551) |

หลัง loop (:554–562): cleanup `spike disarm` + `set_pref(allowRemoteInput,false)` ใน try/except (`cleanup_ok`), `nd.close()` กัน exception, สรุปผล, **exit 3 เมื่อ aborted** (:617–618) · summary JSON write มี try/except (`summary_write_error` :610)

**ข้อค้นพบ (รายงาน ไม่แก้):** หลัง audit write failure, cleanup **ยังส่ง IPC อีก 2 คำสั่ง** (disarm + set_pref) โดยไม่มี audit record ปิดท้าย — วิเคราะห์: เป็น** intentional safety-restore** (คืน `allowRemoteInput=False` ปิด remote input ทันที — ข้ามไป = เสี่ยงกว่า) และเป็น mod-state command ไม่ใช่ game-state · ทางเลือกถ้าจะเข้ม: emit ความพยายาม cleanup 1 record (ถ้าเขียนได้) ก่อนส่ง — **เสนอเป็น diff แยก รออนุมัติ** ไม่แก้เอง

**tests ที่ครอบ A5 แล้ว (37/37 OK · exit 0):** `test_ndjson_write_fails_on_first_record_zero_commands` (fail ก่อนยิง = 0 คำสั่ง) · `test_ndjson_write_fails_after_pre_fire_incomplete_not_pass` (fail หลังยิง = INCOMPLETE ไม่นับ PASS) · `test_state_unreadable_mid_run_unknown_and_exit3` (UNKNOWN record + exit 3) · `test_ndjson_open_failure_exit2_zero_commands` / `test_hash_unstable_at_start_exit2_zero_commands` (exit 2)

**ช่องว่างทดสอบที่ยังเปิด:** ไม่มี test assert พฤติกรรม cleanup หลัง audit-fail (ว่า disarm/set_pref ถูกพยายามหรือไม่ · นับ sub ใน FakeIpc ได้) — เสนอเพิ่ม 1 assertion เมื่อได้อนุมัติ

## 4. สรุปความเสี่ยง

- เกมรันบนเซฟจริง: ความเสี่ยงเดียวที่มองเห็น = ผู้ใช้ save เกม (เป็นสิทธิ์ของผู้ใช้ — จะทำให้ baseline `7332fc93eb98` ต้อง rebaseline ก่อนรอบทดสอบถัดไป ตาม precedent)
- ไม่มีการยิง/IPC/control ใดจากฝั่ง agent หลังมติ hold
