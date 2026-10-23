# C8 — verify / restore / post-check (2026-10-24 01:03–01:05)

Game pid 42000 was closed by the user before this step (`status: game not running`).

## Sequence
1. `save-split verify --label S2a3-20261022-143608` → **PASS**
   - mounted copy: changed=1 (`PlayerprefData.dat` only, volatile) · missing=0 · added=0
   - real aside `Save.real-S2a3-20261022-143608`: **changed=0** · mtime/size drift 0
   raw: `c8-verify-20261024.txt`
2. `save-split restore --label S2a3-20261022-143608` → OK
   - `Save` → `Save.used-20261024-010342` (copy kept)
   - `Save.real-S2a3-20261022-143608` → `Save` (renamed back, **never rewritten**)
   - restored vs real baseline: changed=0 missing=0 added=0
   raw: `c8-restore-20261024.txt`

## Post-check
| item | value |
|---|---|
| `Save` | 57 files · digest **4226730f66ff** |
| matches baseline files | EXACT vs `explore-20261022-0755/{copy,real}.before.txt` and `S2a3-20261022-143608/{copy,real}.before.txt` |
| independent rehash (per-file sha256 vs `savehash.real.before.txt`) | 57/57 · changed **[]** · missing **[]** · added **[]** |
| leftovers | **none** (only `Save` in the data dir) |
| `SaveSlot*` writes during the whole run | **0** (only `PlayerprefData.dat` drifted on the copy) |
| `MelonPreferences.cfg` | `269b571c1b1572b7b525cda3e8a27e09b732b8eb31bdfe290269c8ed62a098d1` (unchanged) |
| `net.cfg` | `dc8e9098490d74c2dfbd6c8a1dffecfbd6befbfeccb2d235dbd0756f4d861b61` (enabled=false, 454 B — same as A0 start) |
| game / port 47800 | not running · no listener |
| test copy kept | `coop-test/S2a3-20261022-143608/Save.used-20261024-010342` |
| baseline changed? | **NO** |
