# S1.5-a — ความคืบหน้า (2026-10-20 14:23) — **หยุดก่อนรอบ 3 ตามคำสั่ง**

สถานะ: restore + dry-run 2 รอบ **เสร็จและผ่าน** · รอบ 3 (sentinel) **ยังไม่เริ่ม** — รอผู้ใช้เปิดเกม
ไม่มีคำสั่ง IL2CPP ใดถูกเรียกในทั้ง session นี้

## 1. Restore สถานะค้างจาก S1 [RUNTIME]

คำสั่ง: `python tools/save-split.py restore --label S1-20261020-113926 --real-dir Save.real-S1`
- `Save` (สำเนา S1, pref เปลี่ยน 1 ไฟล์) → `coop-test/S1-20261020-113926/Save.used-20261020-142205/`
- `Save.real-S1` → `Save` (rename, ไม่ copy ทับ)
- rehash อิสระ (hashlib, ไม่ผ่านโค้ด split): **57/57 ตรง `savehash.real.before.txt`**
  changed 0 · missing 0 · added 0 · `SaveSlot*` changed 0 · `PlayerprefData.dat` ตรง
- ไม่มี `Save.real-*`/`Save.used-*` เหลือใน `LongYinLiZhiZhuan_Data/`
- log: `logs/restore-s1.log`

## 2. Dry-run รอบ 1 — label `S1.5-20261020-142220-dry1` [RUNTIME]

| ขั้น | ผล |
|---|---|
| `split` | exit 0 · baseline = S1 real 57/57 · copy 57 ไฟล์ digest ตรง |
| `verify` | **PASS** · mounted vs real 0/0/0 · vs copy 0/0/0 · real mtime/size drift 0 |
| `restore` | exit 0 · renamed back · rehash 0/0/0 |
| หลัง restore | `status`: `Save` digest `03d9559772da` (ของจริง) · leftovers none |

log: `logs/dry1*.log`

## 3. Dry-run รอบ 2 — label `S1.5-20261020-142237-dry2` [RUNTIME]

| ขั้น | ผล |
|---|---|
| `split` | exit 0 · baseline = dry1 real (57/57 ตรง) |
| `verify` | **PASS** · 0/0/0 ทั้งสองชุด · drift 0 |
| `restore` | exit 0 · 0/0/0 |
| หลัง restore | `Save` digest `03d9559772da` · 57 ไฟล์ · leftovers none |

log: `logs/dry2*.log`

## 4. เกณฑ์ §1.4 (สถานะณ ตอนนี้)

| # | เกณฑ์ | สถานะ |
|---|---|---|
| 1 | `split`→`verify`→`restore` 2 รอบติดกัน ไม่เรียกคำสั่งเกม | **PASS** (รอบ 1 + 2) |
| 2 | หลัง restore: 57/57 ตรง baseline, changed/missing/added 0, ไม่มี leftover | **PASS** (ทั้งสองรอบ) |
| 3 | รอบ 3: sentinel P-a + P-b | **NOT-RUN** — รอผู้ใช้เปิดเกม |
| 4 | `PlayerprefData.dat` ของจริงตรง baseline ตลอด | **PASS ณ จุดนี้** (ของจริงไม่ถูกแตะ; ยังต้องวัดซ้ำหลังรอบ 3) |
| 5 | ข้อใดไม่ผ่าน → S1.5 ไม่เริ่ม | ยังไม่มีข้อที่ FAIL |

## 5. ข้อเท็จจริงเรื่อง sentinel slot [RUNTIME — อ่านจากสำเนา dry2]

**`SaveSlot8` ไม่ว่าง** — `Info` (197 B) เป็น JSON จริง:

```
SaveVersion: 1.0.0 f8.2
SaveDetail : หวังเวิ่นเทียน / 10年9月3日 / 剧情 / 仙霞派掌门 / 小试牛刀
Hero 34,469,884 B · Save 11,033,680 B · TempHero 5,181,090 B
```

ทั้ง 11 สล็อต (`SaveSlot0`–`SaveSlot10`) มี `Info` + `Hero` + `Save` + `TempHero` ครบ → **ไม่มีสล็อตว่างจริง**
การซ่อน `SaveSlot8` จะทำให้เซฟนั้นหายจากรายการโหลดชั่วคราว (ซึ่งคือสิ่งที่ P-a ต้องการ) แต่ต้องได้รับการยืนยันว่าจะใช้สล็อตนี้
