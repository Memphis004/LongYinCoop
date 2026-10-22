# S2a-3 · C4–C6 (Gate #2) — deploy prep + test-session preparation

**Scope this round:** archive old DLL/PDB, back up prefs, deploy S2a-3 build, create `net.cfg`, split the save, start the recorder, run preflight.
**Not done (forbidden):** no game launch, no C7 measurement, no save write, no baseline change, no `rebaseline`.

- timestamp: `2026-10-22 14:36–14:41`
- label / evidence tag: `S2a3-20261022-143608`
- game repo commit before this round: `70c1944` · mod repo HEAD before: `87aa9e9`
- approved baseline: `4226730f66ffdcf9adb110fd8e26e29ffa8ba40620ccc33b013ef11adfaa46d7` (`coop-test/baselines/20261022-075422`)

---

## 0. Pre-checks (before any write)

| check | result |
|---|---|
| baseline manifest present + 57 files + digest | ✅ `coop-test/baselines/20261022-075422/savehash.real.before.txt`, 57, digest `4226730f66ff…46d7` (recomputed with `digest_of(read_hashfile())`) |
| manifest committed to git (this round, 1 file only) | ✅ `af0288e` |
| game process | ✅ not running |
| MelonLoader process | ✅ not running |
| `Save` files / digest | ✅ 57 / `4226730f66ff` |
| leftovers | ✅ none (before split) |
| other sessions holding the save | ✅ none — all `python.exe` on the machine = Unity WebGL `emcc` build (ProjectSurvive) + 2 MCP servers; no `save-split.py`, no `s15d_sample.py`, no `viewer-client.py` |

---

## 1. C4 — archive old build

| item | sha256 | bytes | note |
|---|---|---|---|
| `Mods\LongYinCoop.dll` (before) | `cb35a93f617b16b3cdf069a9c7e76d166dde757b8692b943c8b42b92026ceee0` | 257,536 | S2a-2 build, lastWrite 2026-10-21 22:55:43 — matched the approved value before archiving |
| → `Mods\LongYinCoop.dll.prev-cb35a93f` | `cb35a93f…ceee0` | 257,536 | archive verified identical |
| `Mods\LongYinCoop.pdb` (before) | `4fc142bdaef7acf52c2889292024d9b0b86e065f9ac73fb324d52f751e47240e` | 66,936 | paired PDB of the S2a-2 build |
| → `Mods\LongYinCoop.pdb.prev-cb35a93f` | `4fc142bd…240e` | 66,936 | archive verified identical |
| `UserData\MelonPreferences.cfg` | `269b571c1b1572b7b525cda3e8a27e09b732b8eb31bdfe290269c8ed62a098d1` | 3,232 | backup → `evidence/S2a3-20261022-143608/prefs.before.cfg` (hash equal) |
| dotted `coop.*` keys in prefs | — | — | **NONE** (regex `^\s*(coop\.[A-Za-z0-9_.]+)`, 0 hits) — nothing to clean |
| `UserData\LongYinCoop\net.cfg` before | — | — | **ABSENT** (recorded before creating it in C5) |

**Warning carried forward:** `Mods\LongYinCoop.dll.off` = `517758147f59…2612` (202,752 B) is the **older S1.5e-W7 build**, *not* S2a-2. Rollback to S2a-2 must use `.prev-cb35a93f`, not `.off`.

---

## 2. C5 — deploy S2a-3 build

| item | sha256 | bytes | verified against |
|---|---|---|---|
| `src\LongYinCoop\bin\Release\LongYinCoop.dll` | `c51debef8c08f8a29524b51f20785f9f71ab9ed29d2eee889977c718af94ae96` | 313,344 | C3 build output (Gate #1) |
| `Mods\LongYinCoop.dll` (after copy) | `c51debef…ae96` | 313,344 | **EQUAL to build output** |
| `src\LongYinCoop\bin\Release\LongYinCoop.pdb` | `6ec6482c9649b61e8fb21e0e682710622a9b3a364b5261363244163d916ec110` | 82,240 | C3 build output |
| `Mods\LongYinCoop.pdb` (after copy) | `6ec6482c…c110` | 82,240 | **EQUAL to build output** |

### `UserData\LongYinCoop\net.cfg` (new, 455 B, sha256 `42e5efea68467829b6be4206c3b2a4baff571751f8a44ead731ba01be5635137`)

Flat `key = value`, **no sections**, UTF-8 **without BOM** (first bytes `23 20 4c` = `# L`), so `S2NetConfig.Parse` sees a clean comment first line.

```
enabled          = false
role             = host
secret           = <redacted: 64 hex chars>
allowNonLoopback = false
transport        = tcp
bind             = 127.0.0.1
port             = 47800
sendHz           = 30
heartbeatMs      = 1000
```

- secret: 32 random bytes from `RandomNumberGenerator`, lower-case hex, length 64, `^[0-9a-f]{64}$` ✅ · **value never printed** to console or files; only a non-reversible tag is recorded (`sha256(secret)[0:16] = 9512c5ebba57692c`) so a later edit can be detected
- `transport = tcp` per plan Q2 (approved); `bind` stays loopback literal; `enabled = false` per instruction ⇒ **S2 will not start until this is flipped to `true`** (needs explicit approval before C7)
- this file is **not** a MelonPreferences file; MelonLoader never touches it (งาน A decision, `1372bc3`)

---

## 3. C6 — split + recorder + preflight

### split

```
python tools\save-split.py split --label S2a3-20261022-143608 ^
  --expected-baseline coop-test\baselines\20261022-075422\savehash.real.before.txt
expect  : coop-test\baselines\20261022-075422\savehash.real.before.txt
  Save vs baseline: changed=0 missing=0 added=0
copy    : Save -> coop-test/S2a3-20261022-143608/save
rename  : Save -> Save.real-S2a3-20261022-143608
copy    : coop-test/S2a3-20261022-143608/save -> Save
  copy vs real: changed=0 missing=0 added=0
OK split S2a3-20261022-143608: 57 files, real kept as Save.real-S2a3-20261022-143608
```

manifest (`coop-test/S2a3-20261022-143608/MANIFEST.json`, copied to `evidence/S2a3-20261022-143608/split-manifest.json`):

| field | value |
|---|---|
| `label` | `S2a3-20261022-143608` |
| `created` | `2026-10-22T14:39:10` |
| `expect_baseline` | `coop-test\baselines\20261022-075422\savehash.real.before.txt` |
| `real.dir` | `Save.real-S2a3-20261022-143608` |
| `real.files` / `real.bytes` | 57 / 485,795,554 |
| `real.digest` | `4226730f66ff…46d7` (== approved baseline) |

### recorder (detached so it does not die with the shell — the 23:15 failure mode of S2a-2)

```
pid 37536  python tools\s15d_sample.py ^
  --out coop-test\S2a3-20261022-143608\sampler.ndjson ^
  --seconds 1800 --interval 0.1 --wait 3600 --wait-interval 3
```

- started 14:39:16, still alive at 14:40:36, log shows `waiting for the game (…s left)`
- log files: `coop-test/S2a3-20261022-143608/recorder.out.log`, `recorder.err.log`

### preflight

```
preflight: label=S2a3-20261022-143608
  OK   real-aside: Save.real-S2a3-20261022-143608 files=57 digest=4226730f66ff
  OK   mounted-is-copy: files=57 vs copy baseline changed=0 missing=0 added=0
  OK   leftovers: Save.real-S2a3-20261022-143608
  OK   sampler: pid 37536 (s15d_sample.py) out=…\coop-test\S2a3-20261022-143608\sampler.ndjson
  OK   prefs-disarmed: 6 keys=false
PREFLIGHT READY - you may launch the game
```

raw: `evidence/S2a3-20261022-143608/preflight.txt` (exit 0), `save-status.txt`, `hashes.txt`

### DEVIATION (declared, not hidden)

First preflight run (14:40:0x) was **FAILED at check 4**: `output file not created yet`. Reason: `s15d_sample.py` creates its `--out` file only **after** it connects to the game pipe — and the game is not allowed to run in C6. That is a structural chicken-and-egg in "recorder before game".

Resolution used: created an **empty placeholder** file at the recorder's `--out` path (`0 B`, mtime 14:40:20), then re-ran preflight → READY.

- what this does **not** prove: that the recorder has ever written a sample. The empty file is not evidence of recording.
- what it does prove (independently checked): pid 37536 is alive, its cmdline is `s15d_sample.py`, and it is waiting for the game.
- on connect the recorder opens the path with mode `"w"` (no `--append`) and truncates ⇒ the placeholder cannot contaminate real data.
- the honest acceptance signal for C7 remains: opname grows / log prints `connected pid=…`.

---

## 4. Rollback plan (only if needed — no step executed)

1. **Restore the save:** `python tools\save-split.py verify --label S2a3-20261022-143608` then `python tools\save-split.py restore --label S2a3-20261022-143608` (must end `4226730f66ff` EXACT, `leftovers: none`).
2. **Restore the mod build (S2a-2):** copy `Mods\LongYinCoop.dll.prev-cb35a93f` → `Mods\LongYinCoop.dll`, `Mods\LongYinCoop.pdb.prev-cb35a93f` → `Mods\LongYinCoop.pdb`; expect `cb35a93f…ceee0` / `4fc142bd…240e`. **Do not use `.off`** (S1.5e-W7, older).
3. **Restore prefs:** copy `evidence/S2a3-20261022-143608/prefs.before.cfg` → `UserData\MelonPreferences.cfg`; expect `269b571c…098d1`.
4. **Disarm net:** set `enabled = false` in `UserData\LongYinCoop\net.cfg` (or delete the file — absence is fail-closed: `FileRead=false` ⇒ S2 inert).
5. **Stop the recorder:** `Stop-Process -Id 37536` (if still waiting).
6. **Re-check:** `save-split status` → 57 / `4226730f66ff` / `leftovers: none`; game not running.

---

## 5. Risks / open items for C7

1. **CPU contention:** a Unity WebGL build (`emcc`, ProjectSurvive) is running on this machine with many worker processes. That will add noise to `Time.unscaledDeltaTime` and can move X5/X6. Recommendation: stop/idle that build before the measurement session (not my call to kill it).
2. `enabled = false` now — S2 does not start on launch. Flip to `true` **only with explicit approval** as the first step of C7.
3. Recorder was started at 14:39:16 with `--wait 3600` ⇒ it gives up around **15:39:16** if the game never appears, and records at most 1800 s after connecting.
4. Not yet measured (unchanged): X1, X2, X5, X6, and E1–E7 — all need the real game.
