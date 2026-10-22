# C7 preparation (no game launch) — status + runbook

**State prepared at 2026-10-22 14:48 · label `S2a3-20261022-143608` · PREREQ. NOTHING LAUNCHED.**

Approved for: preparing C7 only. Still forbidden: launching the game myself, changing the baseline, touching real `SaveSlot*`.

---

## 1. Recorder — verified alive (placeholder explicitly NOT counted as evidence)

| check | result |
|---|---|
| pid 37536 alive | ✅ python, start 14:39:16, CPU 0.4 s |
| cmdline identity | ✅ `tools\s15d_sample.py --out …\coop-test\S2a3-20261022-143608\sampler.ndjson --seconds 1800 --interval 0.1 --wait 3600 --wait-interval 3` |
| log tail (14:46:27) | `waiting for the game (3168s left)` — still waiting, no error |
| `recorder.err.log` | empty |
| `sampler.ndjson` | **0 bytes**, mtime 14:40:20 = creation time of the placeholder |
| what that means | **no sample has ever been written**; there is no connection yet and no data. The file is *not* proof of recording. |
| accept-signal for C7 | log line `connected pid=…` + `sampler.ndjson` size/line-count growing |

Recorder wait window ends **≈ 15:39:16**. If the launch happens after that: restart it (`s15d_sample.py` with the same args) **and** re-run preflight (step 4 below) before launch. Do not run two recorders at once (they would both hold the pipe).

## 2. `net.cfg` — enabled for C7

| item | value |
|---|---|
| path | `<game>\UserData\LongYinCoop\net.cfg` |
| sha256 before (`enabled=false`) | `42e5efea68467829b6be4206c3b2a4baff571751f8a44ead731ba01be5635137` (455 B) |
| sha256 after (`enabled=true`) | `0cca04f8629f473c221de8a7c4b5b76932baaa1fcee7df5acc9449136660df0a` (453 B) |
| diff vs backup | **only** the `enabled` line (`false` → `true`) |
| secret | identical to before, 64 lower-case hex, value never printed (only the tag `sha256[0:16]=9512c5ebba57692c`) |
| backup | `<game>\UserData\LongYinCoop\net.cfg.bak-before-c7-20261022-144640` — **kept outside git** (it contains the secret) |
| redacted snapshot | `evidence/S2a3-20261022-143608/net-cfg-enabled-true.redacted.txt` |
| encoding | UTF-8 **no BOM** (`23 20 4c` first bytes) · flat `key = value`, no `[section]` |
| external accept closed | `bind=127.0.0.1` · `allowNonLoopback=false` · `transport=tcp` · `port=47800` · validator: `BindPolicy.Validate("127.0.0.1", false)=Ok` + `IsLoopbackLiteral=true` |
| port 47800 before launch | **no listener** (free) |

## 3. Build / prefs / save (re-measured now)

| item | sha256 | expected |
|---|---|---|
| `Mods\LongYinCoop.dll` | `c51debef8c08f8a29524b51f20785f9f71ab9ed29d2eee889977c718af94ae96` | == C3 build ✅ |
| `Mods\LongYinCoop.pdb` | `6ec6482c9649b61e8fb21e0e682710622a9b3a364b5261363244163d916ec110` | == C3 build ✅ |
| `UserData\MelonPreferences.cfg` | `269b571c1b1572b7b525cda3e8a27e09b732b8eb31bdfe290269c8ed62a098d1` | unchanged ✅ |
| game process | not running ✅ | — |
| `Save` (mounted copy) | 57 files · `4226730f66ff` | — |
| `Save.real-S2a3-20261022-143608` (real, aside) | 57 files · `4226730f66ff` | == approved baseline ✅ |
| leftovers | exactly `Save.real-S2a3-20261022-143608` | ✅ |

## 4. Preflight (re-run at 14:48:23 after enabling)

```
OK   real-aside: Save.real-S2a3-20261022-143608 files=57 digest=4226730f66ff
OK   mounted-is-copy: files=57 vs copy baseline changed=0 missing=0 added=0
OK   leftovers: Save.real-S2a3-20261022-143608
OK   sampler: pid 37536 (s15d_sample.py) out=…\sampler.ndjson
OK   prefs-disarmed: 6 keys=false
PREFLIGHT READY - you may launch the game
```

raw: `evidence/S2a3-20261022-143608/preflight-c7-prep.txt` (exit 0)

---

## 5. OPEN DECISION (blocks phase A0 validity) — read before launching

Plan §6.5 step 1 = **`phase A0`: S2 OFF for 60 s** as the X5 baseline, *then* S2 on for W. The config is now `enabled=true`, so launching now gives **no A0 baseline**.

`S2NetConfig` re-reads `net.cfg` every ~1 s, so the flip can be done **live, without restarting the game**:

- **Option A (recommended, matches §6.5):** I flip `enabled=false` (~1 s, hashes recorded) → you launch → A0 60 s → I flip `enabled=true` → W starts. X5 measurable against A0 **and** A1.
- **Option B:** launch with `enabled=true` as it is now → A0 impossible → **X5 = NOT-RUN** (only A1 exists; no A/B difference can be computed). Everything else (X1/X2/X3/X4/X6/X7/X8) still measurable.

Nothing is flipped until you pick one.

## 6. Runbook (only after you say "พร้อม" and launch the game yourself)

Common setup in a shell (the secret is read from `net.cfg`, never echoed):

```powershell
cd C:\Users\memph\LongYinCoop
$cfg    = "C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan\UserData\LongYinCoop\net.cfg"
$secret = ((Get-Content $cfg | Where-Object { $_ -match '^\s*secret\s*=' }) -split '=',2)[1].Trim()
$out    = "coop-test\S2a3-20261022-143608"
```

| # | who | action | what I record |
|---|---|---|---|
| 1 | you | enter a battle and stop at `WaitPlayerInput` (no Esc, no action during blocks) | wall-clock |
| 2 | me | `phase A0` 60 s with S2 off (Option A) | recorder frame delta, `sampler.ndjson` growth |
| 3 | me | flip `enabled=true` → wait for `[S2]` listener line in `MelonLoader\Logs` | marker + pref/cfg hash |
| 4 | me | `python tools\viewer-client.py --connect 127.0.0.1:47800 --secret $secret --field-hash 5424e4cd7dfad630 --out "$out\viewer-W.ndjson" --duration 300 --ping-hz 0.2` | X1/X2/X3/X6, unit count, Div-A/Div-B |
| 5 | you | play 2–3 turns during block 4 (Q11) | turn/time markers |
| 6 | me | `phase B1` stall: same command `--duration 120 --stall 60 --out "$out\viewer-B1.ndjson"` | X5 |
| 7 | me | `phase B2` kill: `--duration 120 --kill-after 60 --out "$out\viewer-B2.ndjson"` (no `--reconnect`) | X5 + host exceptions |
| 8 | me | `phase B3` reconnect: `--duration 120 --kill-after 60 --reconnect --out "$out\viewer-B3.ndjson"` | X4 resync + Div-A=0 |
| 9 | me | flip `enabled=false` → `phase A1` 60 s | X5 baseline repeat |
| 10 | you | close the game normally, then tell me | — |
| 11 | me | `save-split verify --label S2a3-20261022-143608` → `restore` → confirm `4226730f66ff` EXACT + `leftovers: none`; restore prefs + `net.cfg` (`enabled=false`) | X9/E7 |

Rules that stay in force: no pause while `WaitPlayerInput`; if you press Esc, tell me so I can exclude that window; every block starts by checking `sampler.ndjson` is still growing (a dead recorder voids that block).

## 7. Risks carried into C7

1. **CPU contention:** a Unity WebGL build (`emcc`, `C:\Workspace\ProjectSurvive`) is running with many worker processes. `Time.unscaledDeltaTime` will be noisy and X5/X6 can shift for reasons unrelated to S2. Recommend idling/stopping it for the session (your call — I do not kill it).
2. Recorder expires ≈ **15:39:16** (restart + preflight if later).
3. `enabled=true` means the listener starts with the game; if `MelonPreferences.cfg` is rewritten by MelonLoader on exit it does **not** affect `net.cfg` (separate file, งาน A).
