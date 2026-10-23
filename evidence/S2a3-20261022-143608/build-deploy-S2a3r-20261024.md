# Build / deploy — resync fix + X6 marker (2026-10-24 01:07–01:10)

Approved scope: **B** (resync fix + X6 combined-p99 marker).

## Source changes
| file | change |
|---|---|
| `src/LongYinCoop/S2/Host/S2Sender.cs` | set `frame.Resync`/`ResyncFromSeq` **before** `Encode` (§resync fix) + `_combined` ring + `CombinedPercentile` |
| `src/LongYinCoop/S2/S2HostService.cs` | write marker `s2.metrics` every 60 s (`combinedP99`, `sampleP99`, `serializeP99`, `frames`, `window`, phase) |
| `tools/viewer-client.py` | earlier: `--inject-gap` semantics (after ≥4 snapshots, decrement expected) |

Tests: `dotnet run --project tests/S2Protocol.Tests` → **421 PASS / 0 FAIL** (`tests-after-x6-marker.txt`)

## Build (gate #1)
`dotnet build src/LongYinCoop/LongYinCoop.csproj -c Release` → 0 warnings / 0 errors (`build-20261024.txt`)
- DLL `3c9584a4dd851b8567450a0802db0dae171fd9fb1529ecd35922e7eb7b74e625`
- PDB `9cd7cbd0aabde6c56ca04e62486b03180f6f94b10a339b615552808fbc119020`

## Deploy (gate #2)
- backup: `Mods\LongYinCoop.dll.prev-c51debef` = `c51debef8c08f8a29524b51f20785f9f71ab9ed29d2eee889977c718af94ae96` (+ `.pdb.prev-c51debef`)
- deployed: `Mods\LongYinCoop.dll` = `3c9584a4…e625`, `.pdb` = `9cd7cbd0…9020`
- `MelonPreferences.cfg` `269b571c…98d1` unchanged; `net.cfg enabled=false` (454 B)
- game not running; port 47800 no listener

## Save prep (new label `S2a3r-20261024-0115`)
- `split --expected-baseline …/20261022-075422/savehash.real.before.txt` → real aside + copy, **changed=0**
- real `Save.real-S2a3r-20261024-0115` 57 files `4226730f66ff`
- recorder `s15d_sample.py` **pid 24360** (wait until ~03:09), out `coop-test\S2a3r-20261024-0115\sampler.ndjson`
- `preflight` → **READY 5/5** (`preflight-S2a3r-20261024.txt`)

## Rerun plan (after the user launches)
1. user enters battle → `WaitPlayerInput` → tells me
2. I verify recorder connected + state, then flip `enabled=true` (sha recorded) → wait `[S2] listening` ≤ 2 s
3. W ~120 s with `viewer-client.py` (no `--inject-gap`) → first `s2.metrics` marker(s) for X6; user plays 1–2 turns to raise the sample count
4. B3: `--duration 120 --inject-gap 3` → X4 (expect `gaps=1`, `resync closed≥1`, `giveUp=0`, Div-A=0, guest sees `resync=true`)
5. flip `enabled=false`, stop client, then C8 (`verify` → `restore` → `4226730f66ff` + `leftovers: none`)

Rollback: copy `LongYinCoop.dll.prev-c51debef` back (and `.pdb.prev-c51debef`), `save-split restore --label S2a3r-20261024-0115`.
