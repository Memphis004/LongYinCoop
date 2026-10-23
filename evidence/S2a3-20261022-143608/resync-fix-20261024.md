# Resync flag ordering fix (2026-10-24) — source only, NOT built / NOT deployed

## Root cause (measured in B3b, confirmed by code)
`S2SerializingSender.SendOne()` encoded the snapshot payload **before** assigning
`frame.Resync` / `frame.ResyncFromSeq`, so the wire envelope always carried `0/false` while the ndjson log
(written later) showed `true`. Evidence: host log contains `seq 6/7/8` with `"resync":true,resyncFromSeq=2`,
guest decoded the same seqs as `resync:false,resyncFromSeq:0`; gap never closed (`closed=0`, `giveUp=1`).

## Change (`src/LongYinCoop/S2/Host/S2Sender.cs`)
```diff
+            // 0) BUGFIX 2026-10-24: the envelope carries the resync flag/fromSeq, so they must be
+            //    set BEFORE Encode. ...
+            frame.Resync = resync;
+            frame.ResyncFromSeq = resync ? resyncFrom : 0;
+
             long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
             byte[] payload = S2FrameCodec.Encode(frame, hash);
...
             frame.Heartbeat = heartbeat;
-            frame.Resync = resync;
-            frame.ResyncFromSeq = resync ? resyncFrom : 0;
```
No protocol change, no field change, no threshold change. Resync is not part of `replicaHash` (hash covers
header/units only), so Div-A is unaffected.

## Verification (no build)
- `dotnet run --project tests/S2Protocol.Tests` (read-only link; does **not** build/deploy the mod):
  **421 PASS / 0 FAIL**, incl. `I7 seq gap -> RESYNC -> resync-flagged snapshot closes the window`.
  Raw: `tests-after-resync-fix.txt`.
- Runtime proof still missing: the I7 test passed even before this fix, so the live failure is not covered by
  the suite. X4 must be re-measured in the game with the new DLL.

## Build / deploy (needs approval — gate #1/#2)
Not done. When approved:
1. `dotnet build -c Release` → record DLL/PDB sha256 (gate #1)
2. backup `Mods\LongYinCoop.dll` (`c51debef…ae96`) → `.prev-c51debef`, copy new DLL+PDB (gate #2)
3. `net.cfg enabled=false` + preflight; user launches; A0 → enable → B3 (`--inject-gap 3`) → A1
4. success criterion: `gaps=1`, `resync_sent/acked/closed ≥1`, `giveUp=0`, Div-A=0, plus `resync=true`
   decoded by the guest (`viewer.ndjson`)
Rollback: copy `.prev-c51debef` back, restore `net.cfg`, `save-split restore`.
