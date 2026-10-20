#!/usr/bin/env python3
"""S1.5-c speed comparison - x10 window vs x1 window, with PINNED boundaries.

Pinned so the numbers in RUN-speedup.md stay reproducible even though the capture
kept growing after the analysis. Read-only over the captured NDJSON.

Usage:
    python evidence/<run>/analyze-speed.py evidence/<run>/logs/spike-full.ndjson
"""
import collections
import datetime
import json
import statistics
import sys

# pinned windows (local time, Asia/Bangkok/SEAST = +07)
X10_START = datetime.datetime(2026, 10, 20, 21, 48, 43, 616000)
X10_END = datetime.datetime(2026, 10, 20, 21, 50, 14, 999000)
X1_START = X10_END
X1_END = datetime.datetime(2026, 10, 20, 21, 53, 4, 0)


def ts(t):
    return datetime.datetime.fromtimestamp(t).strftime("%H:%M:%S")


def load(path):
    out = []
    for line in open(path, encoding="utf-8"):
        line = line.strip()
        if not line:
            continue
        r = json.loads(line)
        if r.get("ok") and not r.get("error"):
            out.append(r)
    return out


def window(recs, lo, hi):
    return [r for r in recs if lo.timestamp() <= r["t"] < hi.timestamp()]


def summarise(recs, lo, hi):
    rows = window(recs, lo, hi)
    if not rows:
        return None
    turns = [r.get("turnIndex", -1) for r in rows if r.get("turnIndex", -1) >= 0]
    wall = rows[-1]["t"] - rows[0]["t"]
    dturn = turns[-1] - turns[0] if turns else 0
    segs = collections.defaultdict(list)
    cur = None
    for r in rows:
        st = r["nowActiveStateName"]
        if cur is None or st != cur["st"]:
            if cur:
                segs[cur["st"]].append(cur["t1"] - cur["t0"])
            cur = {"st": st, "t0": r["t"]}
        cur["t1"] = r["t"]
    if cur:
        segs[cur["st"]].append(cur["t1"] - cur["t0"])
    return {
        "rows": len(rows),
        "from": ts(rows[0]["t"]),
        "to": ts(rows[-1]["t"]),
        "wall": wall,
        "turns": dturn,
        "s_per_turn": wall / dturn if dturn else float("nan"),
        "segs": {k: [x for x in v if x >= 0.05] for k, v in segs.items()},
        "scales": collections.Counter(r.get("battleTimeScale") for r in rows),
        "timeScale": collections.Counter(r.get("timeScale") for r in rows),
    }


def med(s, name):
    v = s["segs"].get(name) or []
    return (len(v), statistics.median(v)) if v else (0, float("nan"))


def main():
    recs = load(sys.argv[1])
    a = summarise(recs, X10_START, X10_END)
    b = summarise(recs, X1_START, X1_END)

    print("window x10 = %s .. %s" % (X10_START.strftime("%H:%M:%S.%f")[:-3],
                                     X10_END.strftime("%H:%M:%S.%f")[:-3]))
    print("window x1  = %s .. %s" % (X1_START.strftime("%H:%M:%S.%f")[:-3],
                                     X1_END.strftime("%H:%M:%S.%f")[:-3]))
    for name, s in (("x10", a), ("x1", b)):
        if s is None:
            print("%s: no samples in window" % name)
            continue
        print("%s: rows=%d %s->%s wall=%.1fs turns=%d (%.2f s/turn) scales=%s timeScale=%s"
              % (name, s["rows"], s["from"], s["to"], s["wall"], s["turns"],
                 s["s_per_turn"], dict(s["scales"]), dict(s["timeScale"])))

    print("\n%-16s %10s %10s %8s" % ("state median", "x1", "x10", "ratio"))
    for st in ("Attacking", "Moving", "Rest", "WaitAttacking", "WaitMoving"):
        n1, m1 = med(b, st)
        n10, m10 = med(a, st)
        ratio = ("%.2fx" % (m1 / m10)) if (m10 and m10 == m10 and m1 == m1) else "n/a"
        print("%-16s %9.2fs %9.2fs %8s   (n=%d / %d)" % (st, m1, m10, ratio, n1, n10))

    if a and b and a["s_per_turn"] == a["s_per_turn"] and b["s_per_turn"] == b["s_per_turn"]:
        print("\noverall speedup = %.2fx  (%.2f s/turn -> %.2f s/turn)"
              % (b["s_per_turn"] / a["s_per_turn"], b["s_per_turn"], a["s_per_turn"]))

    # longest idle wait per window (battle clock must stay frozen)
    for name, lo, hi in (("x10", X10_START, X10_END), ("x1", X1_START, X1_END)):
        best, cur = None, None
        for r in window(recs, lo, hi):
            if r["nowActiveStateName"] == "WaitPlayerInput" and not r["paused"]:
                if cur is None or r["turnIndex"] != cur["turn"]:
                    if cur and (best is None or cur["t1"] - cur["t0"] > best["t1"] - best["t0"]):
                        best = cur
                    cur = {"turn": r["turnIndex"], "t0": r["t"], "bt0": r["battleTime"]}
                cur["t1"] = r["t"]
                cur["bt1"] = r["battleTime"]
            else:
                if cur and (best is None or cur["t1"] - cur["t0"] > best["t1"] - best["t0"]):
                    best = cur
                cur = None
        if cur and (best is None or cur["t1"] - cur["t0"] > best["t1"] - best["t0"]):
            best = cur
        if best:
            print("%s: longest idle wait %.1fs at turn %s, battleTime %.4f -> %.4f (%+.4f)"
                  % (name, best["t1"] - best["t0"], best["turn"], best["bt0"], best["bt1"],
                     best["bt1"] - best["bt0"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
