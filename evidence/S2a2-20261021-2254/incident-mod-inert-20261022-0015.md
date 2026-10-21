# Incident — S2a-2 measurement could not start: mod is inert in pid 23020

- Date: 2026-10-22 ~00:14 – 00:20 (local)
- Label: `S2a2-20261021-2254`
- Status: **STOPPED before block A1. No block was measured. No exit has a result.**
- Save safety: intact (details below)

## 1. What the user did
User launched the game themselves at **23:54:22** and reported being in a real battle, idle at
`WaitPlayerInput`, then asked to start A-B-A-B. The launch was **not** covered by a
same-turn `PREFLIGHT READY` (protocol v2 §5), and there was no post-launch gate yet.

## 2. What was measured (facts)

| Check | Result |
|---|---|
| game process | `LongYinLiZhiZhuan.exe` pid **23020**, start **23:54:22**, Responding=True, MelonLoader + LongYinCoop.dll loaded |
| deployed DLL | `Mods\LongYinCoop.dll` sha256 `cb35a93f…ceee0` (matches the S2a-2 build) |
| mod log `UserData\LongYinCoop\logs\coop-20261021.log` | last write **23:54:27**, ends with:<br>`[INFO] S1 loaded … enabled=False probe=True ipc=True capture=True dev=True allowRemoteInput=False`<br>`[INFO] Disabled by pref (Enabled=false) - mod does nothing` |
| named pipe | `\\.\pipe\longyin-coop-23020` — **absent** (full pipe list checked) |
| IPC | none. `spike status` cannot be issued. S2 host service never constructed. |
| recorder pid 29824 | alive since 23:40:38, **never connected** (`sampler.ndjson` = 0 bytes, mtime 23:00:36, i.e. file untouched since before launch) |
| game-side S2 output | `UserData\LongYinCoop\S2\` — no snapshots / no `phase.*` / no `selftest` |

Consequence: exits 1–8 all depend on IPC/snapshots ⇒ **all NOT-RUN**. A-B-A-B never began.

## 3. Root cause (evidence, not guess)

`UserData\MelonPreferences.cfg` is **not valid TOML**. MelonLoader's parser (Tomlet) throws:

```
Expected the start of a number, string literal, boolean, array, or table on line 16, found 'h'
```

Line 16 is `coop.role = host` — the four `coop.*` keys were written with **unquoted string values**
(`host`, 64-char hex secret). Tomlet rejects bare strings, so the **whole file** fails to load and
MelonLoader silently falls back to **defaults for every mod**. `Enabled` has default `false`
(`src\LongYinCoop\CoopPrefs.cs:34`) ⇒ the mod's kill switch triggers.

Control tests (`toml-parse-test.txt`, same Tomlet assembly):
- current file → FAIL at line 16;
- same file without the 4 `coop.*` lines → **PARSE OK**;
- same file with `coop.role = "host"` / `coop.secret = "…"` → **PARSE OK**.

Why it was never seen before: all earlier launches (01:06, 16:07, 22:54, …) used the cfg **without**
the `coop.*` lines (see diff vs `coop-test\S2a2-20261021-2254\prefs.before.cfg`: only these 4 lines are
new, written 23:00:03). `S2NetConfig`'s 64 unit tests exercise the mod's own naive line reader, which
accepts bare values — so the conflict with MelonLoader's TOML was never exercised until a real launch.

Additional constraint (measured by replicating `S2NetConfig.Parse`): quoting the values fixes Tomlet but
the mod's reader then yields `role = "host"` (quotes kept) → `Error = "role must be host|guest when enabled"`.
So **neither** current-unquoted **nor** quoted cfg works end-to-end as shipped.

Second-order effect worth recording: while this cfg is on disk, **every** MelonLoader launch loads defaults
for **all** mods (UnityExplorer prefs included). This must be repaired before the next launch regardless of
how S2a-2 is resolved.

## 4. Save state at stop (read-only verification)

`python tools\save-split.py status`:
```
game     : RUNNING
  Save                         files=57  digest=bf17f3ceb117
  Save.real-S2a2-20261021-2254 files=57  digest=854cc890430f
leftovers: Save.real-S2a2-20261021-2254
mounted Save matches: changed=1 missing=0 added=0
```
Per-file diff vs `savehash.copy.before.txt`: exactly one file changed —
`Save/PlayerprefData.dat`, mtime **23:54:35** (written at game start). **All 57 `SaveSlot*` files unchanged.**
Real save aside digest `854cc890430f` = approved baseline ⇒ real save untouched.

No autosave happened on launch (consistent with the known fact: the game does not autosave on exit/enter),
so the user's current battle exists only in RAM on the copy save.

## 5. Stop reason (protocol v2 §6)
- `sampler.ndjson` not growing (never written) and the pipe is unreachable ⇒ "recorder not alive /
  cannot confirm sampler per block";
- §5 precondition (same-turn PREFLIGHT READY) not satisfied for this launch.

Reported instead of proceeding. Nothing was switched, no `coop.*` value was changed, no IPC command was sent.

## 6. Resolution options (need user decision)

**A. Runtime injection, no code change (fastest).**
1. Close the game.
2. Write a TOML-valid cfg: keep `Enabled = true`, `coop.net.enabled = false`, `coop.allowNonLoopback = false`,
   **omit** `coop.role` / `coop.secret` (verified: parses OK).
3. Launched build loads prefs correctly ⇒ mod active ⇒ pipe exists.
4. Inject `coop.role`/`coop.secret` (bare, unquoted) only when flipping to B1 at runtime; MelonLoader does not
   re-parse the file during a run, so the temporarily invalid file is inert.
   Risk: the run's cfg is invalid again for the whole session; if MelonLoader ever re-saved prefs the keys
   would be dropped (would show up as missing `phase.*` markers).

**B. Small parser fix + valid cfg (clean).**
Patch `S2NetConfig.Parse` to strip surrounding quotes for `Role`/`Secret` (and keep `IsTrue` as is), add unit
tests for quoted/unquoted, rebuild, redeploy (`No redeploy` ruling was about adding instrumentation for exit 3,
not about a defect fix — but it is still a build change and must be recorded). Then the cfg is valid TOML at all
times and protocol v2 runs exactly as written.

**C. Abort S2a-2 now**, restore the save, report stage as blocked.

**Longer term (separate work, not now):** move `coop.net.*` into its own file
(`UserData\LongYinCoop\net.cfg`) so mod settings never have to live inside MelonPreferences' TOML.

## 7. Proposed gate to add (prevents a repeat)
Pre-launch preflight already checks sampler + disarmed prefs, but nothing proved the **mod is actually alive**.
Add a post-launch gate, mandatory before block A1:
`pipe \\.\pipe\longyin-coop-<pid> present` **and** `spike status` returns `battleStateName = WaitPlayerInput`
**and** the mod log line shows `enabled=True`. Any failure ⇒ stop, do not measure.
