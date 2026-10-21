# S2a-2 measurement protocol v2 (user ruling 2026-10-21, after the pre-launch questions)

Status: BINDING for this run. Supersedes the exit-3/exit-4 procedure in `S2a-2-plan.md` rev4 (rev4 stays valid for exits 1, 2, 5, 6, 7, 8).
Label: `S2a2-20261021-2254`.

## 1. Exit 3 (frame-time A/B) — mean only
- Criterion (unchanged, never moved): `delta = mean(frameMs | B) - mean(frameMs | A)`, margin **1.0 ms**, band **+/- 0.5 ms**.
- **Only the mean** is evaluated. **No p99.** p99 of frame time is reported as **NOT-RUN** (a 2 Hz sampler cannot measure it; true p99 ~= 33.5 ms on the reference series).
- Passing the mean criterion = **PASS-with-limitation** (p99 = NOT-RUN). It must not be upgraded to a full PASS.
- `delta` inside the band (+/- 0.5 ms) around the 1.0 ms margin = **INCONCLUSIVE** (not FAIL, not PASS).
- No redeploy / no extra instrumentation for this exit. Measured justification: on the S1.5gB 100 ms reference series, block means over 30 s are reproduced exactly by a 500 ms simulation (difference 0.0000 ms), while natural adjacent-block noise is median 0.067 / p95 0.568 / max 1.101 ms — comparable to the band. See `frame-time-measurability-2345.txt` (+ `.py`).

## 2. Exit 3 procedure (A-B-A-B)
Order: **A1 (false, 30 s) -> B1 (true, 30 s) -> A2 (false, 30 s) -> B2 (true, 30 s)** = 120 s.
- `A` = `coop.net.enabled=false`, `B` = `coop.net.enabled=true` (flip in `UserData\MelonPreferences.cfg`; the mod polls every 1000 ms).
- **Throughout all 120 s the battle must sit still at `nowActiveState = WaitPlayerInput`.** No action (`spike move` / `spike attack` / UI input) may be issued during any block.
- Before each block: confirm `coop-test\S2a2-20261021-2254\sampler.ndjson` is **still growing** (record size/line count). If the recorder died mid-block, **discard that block and report it** (do not silently reuse it).
- Log for every pref switch into `frame-time-ab.txt`: wall-clock time, sha256 of `MelonPreferences.cfg` before and after the switch.
- After each switch, if the expected markers (`phase.start` / `phase.idle`) do not appear in `s2-snapshots.ndjson` within **~3 s**, **stop and report**. Do not continue the blocks.
- Blocks are analyzed with `S2Exit3Rule` (margin 1.0, band 0.5); phases are matched by the mod's own `phase.start` / `phase.end` markers.

## 3. Exit 4 (per-unit compare) — moved after A-B-A-B
- Runs **only after** the 120 s A-B-A-B finishes.
- Leave phase **B enabled** (`coop.net.enabled=true`), then **play one turn** in the battle and compare the per-unit fields against the host's own values (same battle, same turn).
- No pref switching while playing. Exit 4 result is reported against the plan rev4 criterion.

## 4. Exit 6 (self-test) — must happen in a real battle
- The self-test runs once per process, on the **first `enabled=true`**, i.e. at B1. That is why the process must already be inside a real battle before the first switch.
- Validity: the `selftest` marker in `s2-snapshots.ndjson` must have **`haveBefore=true` AND `haveAfter=true`**. If either is false the marker is **VOID for exit 6** (outside Prepare/Ready/Fighting, `S2Sampler.Read()` returns null and `auditEqual` becomes vacuously true).
- If the marker is void, report it as NOT-RUN for exit 6 (do not re-launch the game just for this; that is a separate decision).

## 5. Operational preconditions
- `PREFLIGHT READY` must be re-verified **in the same turn** as telling the user to launch the game (sampler alive + preflight), never from an earlier turn.
- Only one IPC client may hold the pipe during the run (the recorder).
- `DevCommands=true` is required for `spike status`; it has no effect on the S2 guard (pure 6-pref guard, 64/64 tests).
- Test save only: `Save` = mounted copy, real save aside as `Save.real-S2a2-20261021-2254`; approved baseline digest `854cc890430f`.

## 6. Stop conditions (report, do not proceed)
- recorder death mid-block, missing `phase.*` markers within ~3 s, `nowActiveState != WaitPlayerInput` during the 120 s, any launched-but-unintended game process, save hash drift beyond the documented volatile-only rebaseline.
