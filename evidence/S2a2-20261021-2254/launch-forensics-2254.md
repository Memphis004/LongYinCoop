# Item 3 - forensics of the un-commanded launch 2026-10-21 22:54:18 (read-only)

## What the logs say about the launch itself

| source | finding | label |
|---|---|---|
| `Steam\logs\gameprocess_log.txt` | `[22:54:18] AppID 3202030 adding PID 45256 as a tracked process "...\LongYinLiZhiZhuan.exe"` · `[22:58:34] no longer tracking PID 45256, exit code 0` | RUNTIME |
| `Steam\logs\console_log.txt` | `[22:54:16] GameAction [AppID 3202030, ActionID 6] : LaunchApp changed task to CheckShaderDepotManifest` → `SynchronizingCloud` → `ShowInterstitials` → `CreatingProcess` → `[22:54:18] Game process added ... ProcID 45256` | RUNTIME |
| `Steam\logs\console_log.txt` | **no** `ExecuteSteamURL` / `ExecCommandLine` line for this launch. Nearest before: `[2026-10-20 23:24:54] steam://run/3202030//`; nearest after: `[2026-10-21 23:20:04] "Steam.exe"` | RUNTIME |
| `Steam\logs\content_log.txt` | `[22:54:18] AppID 3202030 state changed : Fully Installed,App Running,` | RUNTIME |
| `Steam\logs\cloud_log.txt` | `[22:58:34]` auto-cloud persists `Save\PlayerprefData.dat` (matches the volatile-only drift) | RUNTIME |
| `MelonLoader\Logs\26-10-21_22-54-18.log` | `Command-Line:` **empty** → launched with no arguments (ordinary Steam launch); mod (old DLL `51775814…2612`, StageLabel `S1.5e-W7`) loaded and heartbeat normal | RUNTIME |
| `Steam\logs\cef_log.txt` | `22:54:15.043 Device Event: Displays updated, count: 1 … bounds=[0,0 1920x1080], workarea=[0,0 1920x1032]` — a display-config change ~1 s before the LaunchApp action started | RUNTIME (cause UNKNOWN) |

The Steam client session that performed the launch had been running since 15:54:35; its
game-action counter for this app runs 16:07=1, 18:17=2, 18:18=3, 19:49=4, 20:15=5, **22:54=6**.
`ActionID` is only a per-session counter, it does **not** identify the trigger.

## What could NOT be recovered (retroactively)

| source | result |
|---|---|
| Security 4688 (process creation) | not available: `auditpol /get /subcategory:"Process Creation"` → *"A required privilege is not held by the client"*; query for Id 4688 in the last 2 h → *no events* ⇒ no parent-process record |
| System log 22:50–23:00 | only one unrelated event (`DistributedCOM 10029` 22:58:19, BcastDVR/Game Bar) |
| TerminalServices LocalSessionManager / RemoteConnectionManager (22:40–23:00) | no events ⇒ no RDP logon around the launch |
| `C:\Windows\Prefetch` | enumeration returned 0 entries (no read access / disabled) ⇒ no `.pf` string list |
| scheduled tasks referencing steam/LongYin | none |
| scripts referencing `steam://` / `-applaunch` / the exe | none in `LongYinCoop/**`, `<game>\.opencode/**`, `%USERPROFILE%\.opencode` |

## Conclusion (honest)

**The trigger of the 22:54:18 launch cannot be identified from the surviving logs.**
What is established: it was driven by the *long-running Steam client session* (GameAction
pipeline), **not** by an external `steam://` URL or a `steam.exe -applaunch` command line, with no
game arguments. That signature is what a click inside the Steam client UI (library / tray /
"Play") produces — but a UI-automation click would leave the **same** signature, so the two cannot
be separated after the fact. No RDP session, no scheduled task, no launcher script.

## Next launch: watcher (read-only, already running)

`evidence\S2a2-20261021-2254\watch-parent.ps1` (PID 21580, heartbeat
`parent-watch-heartbeat.txt`). On the first sighting of `LongYinLiZhiZhuan.exe` it writes
`parent-watch.ndjson` with:

- `gamePid` / `gamePpid` / `gameCreated` / `gameCmd`
- `ancestorChain` (up to 6 levels) resolved from a **rolling PID→process map**, so a launcher that
  already exited is still identified
- full process snapshot (pid, ppid, name, creation, command line) → `parent-watch-procs-*.txt`
- `foregroundWindow` (pid + title + that process's command line) at the moment of launch
- tails of `console_log.txt` (25 lines) and `gameprocess_log.txt` (15 lines)

It is read-only, never starts/kills anything, records once and exits (or on
`parent-watch.stop`, or after 8 h). Backup for the same purpose without any background process:
while the game is still running, `Get-CimInstance Win32_Process -Filter "Name='LongYinLiZhiZhuan.exe'"`
gives `ParentProcessId`, and as long as the parent (normally `steam.exe`) is alive it can still be
resolved in name/cmdline.
