# S2a-2 stage report — host sampler/sender (loopback), label `S2a2-20261021-2254`

Date: 2026-10-21 22:58 → 2026-10-22 00:48 · game pid 6156 (session 00:31:10 → 00:47:46) · mod `0.4.0` / `S1.5e-W7`
DLL measured: `Mods\LongYinCoop.dll` sha256 `cb35a93f…ceee0` (unchanged during the stage)

## 1. Verdict table (plan §5)

| # | criterion | result | evidence |
|---|---|---|---|
| 1 | loads / no crash | **PASS** | mod log `enabled=True` + `IPC listening` 00:31:14; 1 turn completed (turn 0→1 at 00:45:47, marker `battle:end` at 00:47:44); `[ERROR]`=0, Exception=0, WARN=3 (all 3 = the self-test's intentional forbidden frames) |
| 2 | per-frame cost `sampleMs+serializeMs` p99 ≤ 4 ms | **PASS** | 48,565 frames: mean 0.0654 · p95 0.0801 · **p99 0.1047** · max 10.7323 ms (`sampleMs` p99 0.0969, `serializeMs` p99 0.0070) |
| 3 | frame-time A/B, mean only | **PASS-with-limitation** | A=7.6927 ms, B=7.9213 ms, **delta=+0.2285 ms** (margin 1.0 / band 0.5; estimator Δframe/Δt) · secondary estimator (mean `deltaTime`) delta=+0.4571 ms → PASS · **p99 = NOT-RUN** by ruling |
| 4 | snapshot == game per unit | **PASS** | frozen key (battleTime 0.689, turn 1), frame seq 44677: 45/45 field comparisons mismatch 0 (`hp,row,col,unitAlive,playerControl` × 9 units) + replica `isAlive`/`unitAlive` vs host `unitAlive` 9/9 match; `unitCount` 9 = 9 |
| 5 | death in replica | **NOT-RUN** | 0 frames with `hp==0` / `isAlive=false` / `unitAlive=false` — no corpse in this battle (per plan: NOT-RUN, not FAIL) |
| 6 | self-test | **PASS** | marker `selftest`: `expectedForbiddenDrops=3 gained=3 commandsAccepted=0 auditEqual=true exit6=PASS malformed=0 reassembled=1 haveBefore=true haveAfter=true`, auditBefore == auditAfter |
| 7 | save untouched | **PASS** | `SaveSlot*` written 0 files during the whole stage (only `PlayerprefData.dat`, volatile) · after `restore`: 57 files digest `854cc890430f`, **EXACT** vs both `savehash.real.before.txt` and `savehash.copy.before.txt`, `leftovers: none` |
| 8 | real unit count | **PASS-with-limitation** | `unitCount = 9` in every one of 48,565 frames (plan's reference case is 18 = PASS-with-limitation; 9 is smaller ⇒ no generalisation, 60-unit goal untouched) |
| 9 | rollback works | **NOT-RUN** | requires relaunching with `LongYinCoop.dll.off` — not done in this run (needs approval + a launch) |

No criterion FAILED.

## 2. A-B-A-B detail (protocol v2)

| block | window | duration | samples | state |
|---|---|---|---|---|
| A1 | 00:40:38–00:41:08 | 30.10 s | 58 | `WaitPlayerInput` 58/58 |
| B1 | 00:41:08–00:41:38 | 30.10 s | 60 | 60/60 |
| A2 | 00:41:39–00:42:09 | 30.02 s | 58 | 58/58 |
| B2 | 00:42:09–00:42:39 | 30.00 s | 58 | 58/58 |

- `battleTime` 0.6065 and `turnIndex` 0 frozen for the whole 120 s; no action was issued (`commandsAccepted=0`).
- markers: `phase.start` blockIndex=2 → `phase.end` (elapsedMs 31047, frames 3975, framesSent 12531, bytes 3809424, forbidden 3) → `phase.start` blockIndex=3.
- pref switches recorded with wall clock + cfg sha256 in `frame-time-ab.txt`:
  `037fc14a…8ff9` (A) ⇄ `cb8bcd39…9fcf` (B).
- exit 4 then ran with B left enabled: 1 turn played by the user (WaitMoving → WaitAttacking → Attacking → turn 1, battleTime 0.689).

## 3. Incidents in this stage

### 3.1 Launch 23:54 — mod inert (invalid TOML in MelonPreferences.cfg)
`coop.role = host` / `coop.secret = f16d97…` were written as **bare strings** (invalid TOML). MelonLoader's
Tomlet threw `TomlInvalidValueException … line 16, found 'h'`, defaulted **all** prefs, `Enabled` fell back to
its default `false`, the mod logged `Disabled by pref (Enabled=false)` and did nothing — no IPC pipe at all.
Measured, root-caused, fixed under user ruling A (booleans kept in the cfg, `role`/`secret` injected only at
runtime). Details: `incident-mod-inert-20261022-0015.md`, `toml-parse-test.txt`, `fix-addendum-20261022-0027.md`.

### 3.2 Harness defect — false stop at 00:38:07
`run_abab.py` parsed markers with `json.loads`, but the mod writes them as
`{"kind":"phase.start","marker":true,<raw body>}` — not valid JSON. The driver aborted at B1 although the mod
had activated correctly (`phase.start blockIndex=1` + a valid `selftest` were present). Fixed in
`run_abab_v2.py` (regex `"kind":"…"`); attempt 1 discarded, attempt 2 is the measured run. The `selftest`
marker used for exit 6 came from attempt 1 (once per process) and is valid (`haveBefore/haveAfter=true`).

### 3.3 Design finding — MelonLoader normalises the dotted keys on its own save
On game exit MelonLoader re-serialised its prefs and turned

```
coop.net.enabled = false
coop.allowNonLoopback = false
```

into

```
[LongYinCoop.coop]
net = { enabled = false }
allowNonLoopback = false
```

and **dropped** `coop.role` / `coop.secret` (unknown entries, never loaded as entries). `S2NetConfig`'s naive
reader only accepts keys inside the literal `[LongYinCoop]` section, so after any MelonLoader save the
`D2-i` mechanism (flip `coop.net.enabled` inside `MelonPreferences.cfg`) **silently stops working** while the
game is running with a freshly written file. This did not affect this run (no save happened during the 120 s),
but it must be resolved before S3 relies on the same mechanism. Options: (a) read `coop.net.*` from a separate
file (`UserData\LongYinCoop\net.cfg`) that MelonLoader never touches, or (b) accept two spellings in the reader
and re-quote the values. Recommendation: (a) — smaller failure surface, no TOML coupling.

## 4. State at close

- game: not running (graceful exit 00:47:46, `probe: unpatched (owned patches before=1 after=0)`)
- save: `Save` = the real folder, renamed back and **never rewritten**
  (`Save.real-S2a2-20261021-2254` → `Save`), 57 files `854cc890430f`, `leftovers: none`;
  test copy kept at `coop-test\S2a2-20261021-2254\Save.used-20261022-004827`
- prefs: restored from `prefs.before.cfg`, sha256 `269b571c…098d1` = `prefs-sha-before.txt` (3232 bytes)
  · the ML-written runtime file kept as `UserData\MelonPreferences.cfg.during-S2a2` for the record
- recorder: stopped (pid 11472) · 1,776 samples in `sampler.ndjson` (5.6 MB)

## 5. Evidence index (`evidence/S2a2-20261021-2254/`)

`S2a2-report.md` (this file) · `metrics-summary.txt` · `snapshot-compare.txt` · `audit-tuple.txt` ·
`unit-count.txt` · `frame-time-ab.txt` · `frame-time-measurability-2345.txt(.py)` · `ab-protocol-v2.md` ·
`run_abab_v2.py` · `analyze_exit3.py` · `compare_exit4.py` · `exit_checks.py` · `sampler.ndjson` (in
`coop-test/…`) · `S2\s2-snapshots.ndjson` (48,565 frames, in the game folder) · incident/fix files ·
`split-*`, `preflight*`, `prefs*`, `rebaseline.txt`, `recorder*.txt`

## 6. Open items / proposed next steps

1. **Approval to close the stage** → tag `stage-S2a-2` in the mod repo (+ record in AGENTS status).
2. **exit 9 rollback test** (not run): restore `Mods\LongYinCoop.dll.off`, launch once, confirm the old mod
   logs load and the game runs, then restore the new DLL. Needs a launch decision from the user.
3. **§3.3 design finding** must be fixed (or explicitly accepted) before S3 uses `coop.net.*` at runtime.
4. exit 5 (corpse in replica) and the 18/60-unit scaling remain NOT-RUN.

## 7. Close-out (user rulings, 2026-10-22 ~00:50)

- Stage closed with **commit + tag `stage-S2a-2`** (approved). exit 9 (rollback `.dll.off`) stays **NOT-RUN**
  and is carried as the next task (it needs one game launch).
- Decision for §3.3: move `coop.net.*` out of `MelonPreferences.cfg` into `UserData\LongYinCoop\net.cfg`
  (a file MelonLoader never reads or rewrites). **Not implemented in this stage** — it is the first task of the
  next stage, before anything relies on a runtime enable switch.
- `prefs.after.cfg` and `prefs.before-fix.cfg` in this folder are **redacted** (`<redacted-64hex-coop-secret>`)
  before committing, because the repo has a public remote. The pre-redaction sha256 values are recorded in
  `prefs-sha-before.txt` / this report; the unredacted copies remain only in the local game `UserData`.
- Committed evidence excludes `parent-watch.ndjson` (89 MB), `*.ndjson`, and `*.dll` (`.gitignore`).
