#!/usr/bin/env python3
"""S1.5g: print the 32 W4 click events (read-only hook) with state names + times.

The state-name map is taken from the status stream itself (nowActiveState -> name),
so nothing is assumed from dump.cs. W4 `t` is in milliseconds.
"""
import json
import datetime

DIR = r"evidence/S1.5g-20261021-160331/logs"


def fmt(t):
    if t > 1e11:  # milliseconds
        t = t / 1000.0
    return datetime.datetime.fromtimestamp(t).strftime("%H:%M:%S.%f")[:-3]


rows = [json.loads(l) for l in open(DIR + "/status-100ms.ndjson", encoding="utf-8-sig") if l.strip()]
ok = [r for r in rows if r.get("ok")]
names = {}
for s in ok:
    if s.get("nowActiveState") is not None and s.get("nowActiveStateName"):
        names[int(s["nowActiveState"])] = s["nowActiveStateName"]
print("state id -> name (observed in the status stream of this run):")
for k in sorted(names):
    print("  %2d = %s" % (k, names[k]))

ev = [json.loads(l) for l in open(DIR + "/jitprobe-w4.ndjson", encoding="utf-8-sig") if l.strip()]
print()
print("W4 events = %d" % len(ev))
print("  #  time          state(id)       paused  battleTime  timeScale  frame")
for i, e in enumerate(ev, 1):
    st = e.get("nowActiveState")
    print("%3d  %s  %-15s %-7s %-11s %-10s %s"
          % (i, fmt(e["t"]),
             "%s(%s)" % (names.get(int(st), "?"), st) if st is not None else "?",
             e.get("paused"), e.get("battleTime"), e.get("timeScale"), e.get("frame")))
