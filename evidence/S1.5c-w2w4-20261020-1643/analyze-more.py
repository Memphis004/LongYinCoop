"""S1.5c follow-up analysis: battle-clock by state, hp changes, pause windows."""
import json
import sys
from collections import defaultdict

st = [r for r in (json.loads(l) for l in open(sys.argv[1], encoding="utf-8") if l.strip()) if r.get("ok")]

print("samples=%d inBattle=%d" % (len(st), sum(1 for r in st if r.get("inBattle"))))
print("timeLimit values: %s" % sorted(set(r.get("timeLimit") for r in st)))
print("aiWaitTime values (first 6): %s" % [r.get("aiWaitTime") for r in st[:6]])

# --- battleTime advance rate grouped by nowActiveStateName ---
groups = defaultdict(lambda: [0.0, 0.0, 0])
prev = None
for r in st:
    if prev is not None and r.get("inBattle") and prev.get("inBattle"):
        dt = r["t"] - prev["t"]
        dbt = (r.get("battleTime") or 0) - (prev.get("battleTime") or 0)
        if dt > 0:
            g = groups[prev.get("nowActiveStateName")]
            g[0] += dt
            g[1] += dbt
            g[2] += 1
    prev = r

print("\nstate -> wall seconds / battleTime advanced / ratio")
for name, (wall, dbt, n) in sorted(groups.items(), key=lambda kv: -kv[1][0])[:12]:
    print("  %-18s wall=%7.2fs dBT=%7.3f ratio=%5.2f n=%d" % (name, wall, dbt, dbt / wall if wall else 0, n))

# --- pause windows ---
print("\npause windows:")
start = None
for r in st:
    if r.get("paused") and start is None:
        start = r["t"]
    elif not r.get("paused") and start is not None:
        print("  t=%.3f..%.3f  duration=%.2fs  state=%s turn=%s"
              % (start, r["t"], r["t"] - start, r.get("nowActiveStateName"), r.get("turnIndex")))
        start = None
if start is not None:
    print("  t=%.3f..(end of capture, still paused)" % start)

# --- hp changes (attack evidence) ---
print("\nhp changes (unit id, turn, before -> after, t):")
prev_units = None
count = 0
for r in st:
    cur = {u[0]: (u[1], u[4]) for u in (r.get("units") or [])}
    if prev_units is not None and cur:
        for uid, (hp, alive) in cur.items():
            if uid in prev_units:
                hp0, alive0 = prev_units[uid]
                if hp0 is not None and hp is not None and abs(hp - hp0) > 0.001:
                    count += 1
                    if count <= 40:
                        print("  %-14s turn=%-3s %.3f -> %.3f (d=%+.3f) alive=%s->%s t=%.3f"
                              % (uid, r.get("turnIndex"), hp0, hp, hp - hp0, alive0, alive, r["t"]))
    if cur:
        prev_units = cur
print("total hp-change samples=%d" % count)
