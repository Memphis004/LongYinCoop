# Phase C — Runtime preparation (ถึงขั้น READY, ยังไม่เปิดเกม)

## 1. Session-start checks (ทำก่อนทุกอย่าง)

| รายการ | คำสั่ง | ผล |
|---|---|---|
| hash เกมตรง `VERSION.md` | `sha256sum GameAssembly.dll …/global-metadata.dat` | `33c56def…20c1` / `451311d4…bfd0` = **ตรงทั้งคู่** ✅ |
| เกมปิด | `tasklist \| grep -ci LongYinLiZhiZhuan` | `0` ✅ |
| สถานะเซฟ | `save-split status` | `Save` 57 ไฟล์ `de7e2af49b23`, `leftovers: none` (= เซฟจริง mount อยู่), ตรง `area-20261022-0615/savehash.copy.before.txt` changed=1 (volatile) ✅ |

## 2. Split ด้วย baseline ที่อนุมัติ

```
$ python tools/save-split.py split --label S2cd-20261022-071043 \
    --expected-baseline coop-test/baselines/20261022-064714/savehash.real.before.txt
expect  : coop-test\baselines\20261022-064714\savehash.real.before.txt
  Save vs baseline: changed=0 missing=0 added=0
copy    : Save -> coop-test/S2cd-20261022-071043/save
rename  : Save -> Save.real-S2cd-20261022-071043
copy    : coop-test/S2cd-20261022-071043/save -> Save
  copy vs real: changed=0 missing=0 added=0
OK split S2cd-20261022-071043: 57 files, real kept as Save.real-S2cd-20261022-071043
```

## 3. preflight-launch → READY

`preflight-launch.txt` (คัดลอกมาเต็มไฟล์ข้าง ๆ):

```
  OK   game-not-running: not running
  OK   leftovers-unexpected: none
  OK   leftovers-state: 1 expected aside: Save.real-S2cd-20261022-071043
  OK   no-hidden-sentinel: none
  OK   label: S2cd-20261022-071043
  OK   manifest: MANIFEST.json label=S2cd-20261022-071043
  OK   aside-matches-manifest: ['Save.real-S2cd-20261022-071043'] vs manifest 'Save.real-S2cd-20261022-071043'
  OK   copy-baseline-file: savehash.copy.before.txt
  OK   real-save-mounted: no
  OK   mounted-is-copy: files=57 vs copy baseline changed=0 missing=0 added=0
  OK   real-aside-untouched: Save.real-S2cd-20261022-071043 files=57 digest=de7e2af49b23
PREFLIGHT-LAUNCH READY - you may launch the game
```

⇒ 11/11 OK, exit 0 ⇒ **อนุญาตให้เปิดเกม** (ขั้นถัดไปต้องให้ผู้ใช้เปิดเอง — ผมไม่เปิด)

## 4. สถานะหลังเตรียม (`save-status.txt`)

```
game     : not running
  Save                         files=57  digest=de7e2af49b23      (สำเนา)
  Save.real-S2cd-20261022-071043 files=57  digest=de7e2af49b23    (ของจริง aside)
leftovers: Save.real-S2cd-20261022-071043
mounted Save matches:
  EXACT  S2cd-20261022-071043/savehash.copy.before.txt
  EXACT  S2cd-20261022-071043/savehash.real.before.txt
```

## 5. หลังผู้ใช้เปิดเกม (07:13:50, pid 26188)

| ขั้น | ผล |
|---|---|
| `tasklist` เห็นโปรเซสเดียว | `LongYinLiZhiZhuan.exe` 26188 ✅ |
| Frida attach (`local`, `attach_pid 26188`) | `Ready` ✅ |
| โหลด `tools/changeday_hook.js` เข้าเกม | คืน `__cd ready` ✅ |
| `__cd.inspect()` | อ่านผลใน `inspect-analysis.md` + `inspect-raw.json` ✅ |

## 6. ยังไม่ทำ (รออนุมัติ)

- ยังไม่ติด hook, ยังไม่กด `>>`, ยังไม่เรียก `ChangeDay`
- เกมยังรันอยู่ที่ pid 26188 (ไม่ได้ปิด) และยัง**ไม่ restore** เซฟ — ต้อง `save-split restore --label S2cd-20261022-071043` ตอนจบรอบ
