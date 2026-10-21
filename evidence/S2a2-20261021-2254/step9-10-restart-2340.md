# Step 9/10 deviation — recorder died, restarted at 23:40 (no game launch)

## What happened

| time | event | evidence |
|---|---|---|
| 23:00:56 | recorder started: `python tools\s15d_sample.py --out coop-test\S2a2-20261021-2254\sampler.ndjson --seconds 7200 --interval 0.5 --append --wait 3600` → PID **46824** | `recorder.txt` |
| 23:01:13 | `PREFLIGHT READY` (5/5 OK, sampler = PID 46824) | `preflight.txt` |
| **23:15:20** | recorder **last** stdout line `[23:15:20] waiting for the game (2717s left)`; process no longer present (checked 23:38) | `recorder-stdout.log` + `Get-Process -Id 46824` = none |

The sampler had **never connected** (the game never ran), so `sampler.ndjson` is still 0 bytes and
**nothing was measured / no evidence was lost**. No game, save, pref or DLL was touched.

Cause: **UNKNOWN** — most plausible is that the background process was killed together with the
shell/session that had spawned it (it was started as a background `shell` process). It is *not*
explained by the game (the game never started) or by the sampler code (it exits with a message).

## Deviation taken (read-only, no game launch)

1. restarted the same sampler with identical arguments → PID **29824**, stdout
   `recorder-stdout-2.log` (started 23:40:38, `waiting for the game (3600s left)`), liveness verified.
2. `python tools\save-split.py preflight --label S2a2-20261021-2254 --sampler-pid 29824
   --sampler-out coop-test\S2a2-20261021-2254\sampler.ndjson` → **PREFLIGHT READY** (5/5 OK) →
   `preflight-restart-2341.txt`. `save-split` history appended by the tool itself.
3. started the read-only parent-process watcher (item 3) → PID **21580**,
   `watch-parent.ps1`, heartbeat `parent-watch-heartbeat.txt`.

**Consequence for the protocol:** the previous `PREFLIGHT READY` (23:01) was stale, so READY is
only meaningful together with a *live* sampler PID. Therefore the launch protocol re-verifies the
sampler + re-runs `preflight` in the same turn as the launch (sampler restarted if it died).

## Standing status after the restart

- sampler PID 29824 alive, window ends ~00:40:38 (or `--seconds` 7200 from connect)
- watcher PID 21580 alive (exits after recording the first launch)
- `Save` = mounted copy, real aside = `Save.real-S2a2-20261021-2254`, digest `854cc890430f`
- 6 write-path prefs = false, `coop.net.enabled=false`, game not running
