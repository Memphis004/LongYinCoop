# Launch event 23:54:22 (game running before the "I'm launching now" message) + mod disabled by pref

Time of analysis: 2026-10-22 00:05–00:12. Read-only work, no game/save/pref/DLL writes.
Label: `S2a2-20261021-2254`.

## 1. Timeline

| time | event | evidence |
|---|---|---|
| 23:19:46 | new `steam.exe` session (PID 12752, parent `explorer.exe` 9676) | `console_log.txt` `[2026-10-21 23:20:04] ExecCommandLine: "C:\Program Files (x86)\Steam\Steam.exe"`, process list |
| 23:40:38 | recorder restarted → PID 29824 (window ends ~00:40:38); preflight READY (5/5) at 23:41 | `step9-10-restart-2340.md`, `preflight-restart-2341.txt` |
| 23:53:14 | parent watcher heartbeat `waiting` | `parent-watch-heartbeat.txt` |
| **23:54:22** | **game launched: PID 23020**, `ActionID 1`, no arguments | `parent-watch.ndjson`, `console_log.txt` |
| 23:54:23 | watcher recorded the launch and exited (its job) | `parent-watch.ndjson`, heartbeat `recorded 2026-10-21 23:54:23.337` |
| 23:54:27 | mod logs **`enabled=False`** → "Disabled by pref (Enabled=false) - mod does nothing" → **no IPC pipe** | `UserData\LongYinCoop\logs\coop-20261021.log` 23:54:27.243–.252 |
| 00:05:20 | user says "จะเปิดเกมแล้ว"; sampler alive, `sampler.ndjson` = 0 bytes | this session |
| 00:06 | `save-split preflight` → **REFUSED** (game running) | `preflight-0006.txt`, `status-0006.txt` |

Launcher identity: `GameAction [AppID 3202030, ActionID 1] : LaunchApp changed task to CreatingProcess` →
`waiting for user response to CreatingProcess` → `continues with user response "CreatingProcess"` →
`CreatingProcess` → `WaitingGameWindow` → `Completed`; parent chain `LongYinLiZhiZhuan.exe(23020) ← steam.exe(12752) ← explorer.exe(9676)`.
This is the plain in-UI launch path (contrast: the 22:54:18 event was `ActionID 6` and had no user-response step).
Foreground window at 23:54:23 was `MelonLoader v0.7.3` (owner = game PID). **Who pressed it is not established** — the
signature is consistent with a click/UI automation in the Steam window (explorer → steam).

**The game was therefore already running ~11 minutes before the user's message**, and the recorder never connected.

## 2. Why the recorder has no data (mod was off)

Mod log for PID 23020 (23:54:27):

```
=== LongYinCoop v0.4.0 stage=S1.5e-W7 | pid=23020 | start 2026-10-21 23:54:27 ===
S1 loaded role=host stage=S1.5e-W7 version=0.4.0 enabled=False probe=True ipc=True capture=True dev=True allowRemoteInput=False
Disabled by pref (Enabled=false) - mod does nothing
```

Every earlier launch today (01:06, 01:29, 02:48, 16:07, 18:17, 18:18, 19:49, 20:15, 22:54) logs
`enabled=True ... ` + `IPC listening \\.\pipe\longyin-coop-<pid>`. This one has **no IPC line** ⇒ no pipe ⇒ the sampler
(PID 29824, alive) has nothing to connect to; `sampler.ndjson` = 0 bytes (created 23:00:36, never written).

Other files are also empty for this instance (noted, unexplained): `MelonLoader\Logs\26-10-21_23-54-23.log` = 0 bytes,
`MelonLoader\Latest.log` = 0 bytes, `Player.log` (TppStudio\LongYinLiZhiZhuan) = 0 bytes. Game process is responsive
(`Responding=True`, window `LongYinLiZhiZhuan`), `version.dll` / `MelonLoader.NativeHost.dll` / `MelonLoader.dll` /
`Mods\LongYinCoop.dll` are all loaded.

## 3. Root-cause candidate — the pref file written by step 7 is not read by MelonLoader

Facts:

1. The values the mod logged are **exactly the registered defaults** (`src/LongYinCoop/CoopPrefs.cs`):
   `Enabled=false`, `Probe=true`, `Ipc=true`, `DevCommands=true`, `AllowRemoteInput=false`, `JitProbe*=false`, `Capture=true`.
   i.e. **every entry fell back to its default**; `Enabled` default (`false`) ≠ file value (`true`).
2. The file on disk says `Enabled = true` (`UserData\MelonPreferences.cfg`, mtime 23:00:03, len 3452,
   sha256 `037fc14a1b097b3a798a1b35b5da68c58d8cb1f0a06ca0bd51fbe8db6d6e8ff9` = exactly the step-7 "after" hash in
   `pref-values.txt`; unchanged since — the running game did not rewrite it).
3. That file is **CRLF-only** (72 CRLF / 0 bare LF), while MelonLoader's own writer emits **LF**
   (`UserData\Loader.cfg`, written 23:54:22 by this same launch = 68 LF / 0 CRLF), and the working pre-step-7 file
   (`prefs.before.cfg`, sha `269b571c…`) was LF (67 LF + 2 CRLF on lines that were already there).
4. Logical diff `prefs.before.cfg` → `prefs.after.cfg` (after stripping `\r`) is only the 4 added keys
   `coop.net.enabled / coop.role / coop.secret / coop.allowNonLoopback`.
5. At 22:54:22 the mod read the pre-step-7 file successfully (`enabled=True` + IPC up) — so the regression appeared
   exactly at the 23:00:03 rewrite.

⇒ The regression was introduced by the step-7 write (done inline, not by a repo tool — no `tools/*` writes prefs;
`save-split.py:741` only reads them). **Two candidate causes, not yet discriminated:** (A) line endings LF→CRLF,
(B) the 4 dotted `coop.*` keys. `preflight`'s own reader is Python-based and strips `\r`, so it sees the section and
keys fine — it cannot detect what MelonLoader actually reads.

**Gap found:** `preflight` does not verify `Enabled=true` / `Ipc=true`, so READY could not have caught this. The 6
"must be false" prefs it checks are unaffected (they are `false` both in file and as defaults).

## 4. Save safety (verified)

- real save aside `LongYinLiZhiZhuan_Data\Save.real-S2a2-20261021-2254` = 57 files, digest `854cc890430f`
  = the approved baseline (`coop-test/baselines/20261021-225856`) ✅
- mounted `LongYinLiZhiZhuan_Data\Save` = the copy, digest `bf17f3ceb117`, only the volatile set differs vs
  `savehash.copy.before.txt` (1 changed / 0 missing / 0 added) — the running instance writes into the copy only.
- `status-0006.txt` (save-split status) + `preflight-0006.txt` (REFUSED) recorded.

## 5. Consequence for the protocol

`PREFLIGHT READY` (23:41) is not evidence that the *next* launch is usable: the launch must be preceded by a
same-turn sampler+preflight check **and** by a post-run check that the mod came up (`enabled=True` + `IPC listening`).
Until the pref problem is fixed, no measurement (exit 1–8) is possible in this instance.
