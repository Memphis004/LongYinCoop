#!/usr/bin/env python3
"""S1.5g Phase B engagement timeline: where the player was actually playing.

Reads only logs/status-100ms.ndjson (read-only samples). No game interaction.
Definitions (conservative, quotable in the report):
  * "progress tick" = a sample where turnIndex, nowActiveState or battleTime moved.
  * "idle stretch"  = consecutive samples with no progress tick.
  * "menu freeze"   = idle stretch > 60 s whose state is NOT WaitPlayerInput
                      (the Esc menu froze the sim: state stuck at Finish/Attacking).
  * "dead tail"     = idle stretch > 300 s at WaitPlayerInput: the player stopped
                      playing; nothing in the battle can advance without a click.
"""
import json
import datetime

PATH = r"evidence/S1.5gB-20261021-200449/logs/status-100ms.ndjson"
PHASE_B_START = 1792588590.191  # 18:28:24 local, last Phase-A resume


def fmt(t):
    return datetime.datetime.fromtimestamp(t).strftime("%H:%M:%S.%f")[:-3]


rows = [json.loads(l) for l in open(PATH, encoding="utf-8-sig") if l.strip()]
ok = [r for r in rows if r.get("ok")]
win = [s for s in ok if s["t"] >= PHASE_B_START]

segs = []
cur = [win[0]]
for a, b in zip(win, win[1:]):
    new_battle = (b.get("inBattle") and not a.get("inBattle")) or \
                 ((b.get("battleTime") or 0) + 1e-6 < (a.get("battleTime") or 0))
    if new_battle:
        segs.append(cur)
        cur = [b]
    else:
        cur.append(b)
segs.append(cur)

print("== Phase B battle segments (from samples) ==")
for i, seg in enumerate(segs, 1):
    turns = [s.get("turnIndex") for s in seg]
    print("seg%d %s -> %s  dur=%.1fs inBattle=%s turnMax=%s turnLast=%s"
          % (i, fmt(seg[0]["t"]), fmt(seg[-1]["t"]), seg[-1]["t"] - seg[0]["t"],
             seg[0].get("inBattle"), max(turns), turns[-1]))

print()
print("== idle stretches > 20 s in Phase B (no turn/state/battleTime movement) ==")
idle = []
start, key = win[0], (win[0].get("turnIndex"), win[0].get("nowActiveStateName"), win[0].get("battleTime"))
for s in win[1:]:
    k = (s.get("turnIndex"), s.get("nowActiveStateName"), s.get("battleTime"))
    if k != key:
        d = s["t"] - start["t"]
        if d > 20.0:
            idle.append((start, s, d, key))
        start, key = s, k
for a, b, d, key in idle:
    print("  %s -> %s  %6.1fs  turn=%s state=%s  (paused=%s)"
          % (fmt(a["t"]), fmt(b["t"]), d, key[0], key[1], a.get("paused")))

paused = sum(b["t"] - a["t"] for a, b in zip(win, win[1:]) if a.get("paused") and b.get("paused"))
# menu freeze: sim frozen mid-battle (state stuck, not waiting for input, not the
# battle-end transition where inBattle flips to false => state name None)
menu = sum(d for a, b, d, key in idle if d > 60 and key[1] not in ("WaitPlayerInput", None))
tail = [x for x in idle if x[2] > 300 and x[3][1] == "WaitPlayerInput"]
play_end = tail[0][0]["t"] if tail else win[-1]["t"]

print()
print("== accounting ==")
print("Phase B wall                     = %8.1fs (%s -> %s)"
      % (win[-1]["t"] - win[0]["t"], fmt(win[0]["t"]), fmt(win[-1]["t"])))
print("battle-pause windows (paused)    = %8.1fs" % paused)
print("Esc-menu freeze (sim frozen)     = %8.1fs" % menu)
print("dead tail (stopped playing)      = %8.1fs (%s -> %s)"
      % (win[-1]["t"] - play_end, fmt(play_end), fmt(win[-1]["t"])))
play = (play_end - PHASE_B_START) - paused - menu
print("PLAY TIME (start -> stopped)     = %8.1fs  (= %.1f min)" % (play, play / 60.0))
print()
print("VERDICT(600 s of continuous play): %s"
      % ("PASS" if play >= 600 else "NOT-RUN (only %.0f s < 600 s)" % play))
