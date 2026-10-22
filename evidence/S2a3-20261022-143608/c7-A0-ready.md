# C7 / phase A0 - READY report (2026-10-22 18:15) - Option A approved by user

Label `S2a3-20261022-143608`. **Nothing launched by me. No baseline change. Real `SaveSlot*` untouched.**
User decision taken: **Option A** = flip `net.cfg` to `enabled=false` now, user launches, A0 60 s with S2 OFF,
then I flip `enabled=true` live (config poll 1 s, no restart needed) and §6.5 continues.

## 1. Gate list (all green)

| # | check | result |
|---|---|---|
| 0 | game not running before launch | OK (preflight refuses otherwise) |
| 1 | `net.cfg` enabled true->false + backup + hashes | done: `0cca04f8...` (453 B) -> `dc8e9098...` (454 B), backup `net.cfg.bak-before-A0-20261022-181146` (`0cca04f8...`), only line 3 changed, secret byte-identical, UTF-8 no BOM |
| 2 | recorder | old pid 37536 **dead** (log `[15:39:18] game not running`, window ended 15:39:16) -> restarted **pid 18512**, wait until **~20:12:12**, err log empty, `sampler.ndjson` still 0 B = placeholder only |
| 3 | DLL / PDB / prefs / game hashes | `c51debef...ae96` / `6ec6482c...c110` / `269b571c...98d1` / game `33c56def...20c1` + metadata `451311d4...bfd0` (all match `VERSION.md`) |
| 4 | baseline `4226730f66ff` | mounted `Save` 57 files EXACT, aside `Save.real-S2a3-20261022-143608` 57 files EXACT, `leftovers: none` besides that aside, `status` = EXACT vs 4 baseline files |
| 5 | preflight | **PREFLIGHT READY 5/5** (`preflight-c7-A0.txt`, exit 0) |
| 6 | port 47800 | free (no listener) |
| 7 | fieldHash | recomputed from source constants: `5424e4cd7dfad630…` (matches client value) |
| 8 | CPU contention (c7-prep risk 1) | no emcc/node/clang build workers running now |
| 9 | old S2 ndjson | `s2-snapshots.ndjson` = 49,934 lines / 97,291,362 B before this run (append mode) -> analyse only lines > 49,934 |

## 2. Measurement decision that needs your OK (no criteria change)

`S2FrameMeter` closes a 60 s window on its own clock (started at mod load) and labels it `phase=B` if S2 is
active **at close time**. A block that starts mid-window forces me to either (a) throw the window away or
(b) accept a window that mixes A and B frames - and the very first in-battle window can also still contain
loading-screen frames (that inflates `p99(A)` and would make X5 look better than reality).

Plan I will use (no threshold touched):
- **window-aligned blocks**: I watch new `framemeter.window` markers, start each block right after a boundary,
  and take **only the window that lies completely inside the block** (A0 -> one clean 60 s window, same for A1).
- if alignment fails (marker late / block slips), fallback = extend A0/A1 to **125 s** and use the clean window.
- every window is reported raw (`n`, `p99`, all windows of the block), straddling windows flagged and excluded.

Say nothing = I use the window-aligned 60 s version (closest to §6.5 text).

## 3. Live runbook once you launch (I stay read-only until W)

| # | who | action | accept signal |
|---|---|---|---|
| 1 | you | launch, enter a battle, stop at `WaitPlayerInput` (no Esc) | you tell me |
| 2 | me | verify recorder really connected (not placeholder) | log `connected pid=.` + `sampler.ndjson` growing + last sample `battleStateName=WaitPlayerInput` |
| 3 | me | A0 (S2 off, aligned 60 s) | wall-clock + `framemeter.window phase=A` |
| 4 | me | flip `enabled=true` (+sha256, backup of the A0 state) | MelonLoader log `[S2] listening on 127.0.0.1:47800` <=2 s, marker `phase.start`, port 47800 listening |
| 5 | me+you | W 300 s (`viewer-client.py --duration 300`), you play 2-3 turns | `viewer.ndjson` grows, `seq` climbing, Div-A mismatches = 0, unit count recorded |
| 6 | me | B1 stall 60 s, B2 kill 60 s, B3 reconnect 60 s | `framemeter.window phase=B` per block, host log exceptions = 0, `resyncServed >= 1` |
| 7 | me | flip `enabled=false` -> A1 aligned 60 s | `phase.idle` marker + window `phase=A` |
| 8 | you | close the game normally | - |
| 9 | me | C8: `save-split verify` -> `restore` -> `4226730f66ff` EXACT + `leftovers: none`; restore prefs + `net.cfg` (`enabled=false`, sha `42e5efea...`) | `verify-restore.txt` |

Rules kept: no pause while `WaitPlayerInput`; a block is **void** if the recorder is dead or the file is not
growing; no PASS claim without complete evidence; no change of baseline.
