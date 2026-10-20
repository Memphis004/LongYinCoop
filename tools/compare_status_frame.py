#!/usr/bin/env python3
"""S1.5-b: join `spike status` samples against game-side UI probe samples.

Inputs:
    --status  NDJSON written by tools/spike_capture.py
    --probe   JSONL, one `il2cpp_exec_js` ui-probe.js result per line
              ({"frame":..,"activeHeroID":..,"activeCell":{..},"units":[..],
                "nowActiveState":..,"paused":..,"activeMoved":..,"activeAttacked":..})

For each probe frame the nearest status sample within --tol frames is used.
Compares: active hero id, active cell, moved/attacked of the active unit,
paused, nowActiveState. Also reports stateHash behaviour between consecutive
status samples (same hash on idle frames, different hash after a change).
"""
import argparse
import json
import sys


def load_status(path):
    rows = []
    with open(path, encoding="utf-8") as fh:
        for line in fh:
            line = line.strip()
            if not line:
                continue
            rec = json.loads(line)
            if rec.get("ok") and rec.get("frame") is not None:
                rows.append(rec)
    return rows


def load_probe(path):
    rows = []
    with open(path, encoding="utf-8") as fh:
        for line in fh:
            line = line.strip()
            if not line:
                continue
            rec = json.loads(line)
            if rec.get("error"):
                continue
            rows.append(rec)
    return rows


def nearest(rows, frame):
    return min(rows, key=lambda r: abs(int(r["frame"]) - int(frame)))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--status", required=True)
    ap.add_argument("--probe", required=True)
    ap.add_argument("--tol", type=int, default=2)
    args = ap.parse_args()

    status = load_status(args.status)
    probe = load_probe(args.probe)
    if not status or not probe:
        print("no samples (status=%d probe=%d)" % (len(status), len(probe)), file=sys.stderr)
        return 2

    matched = 0
    mismatches = []
    for p in probe:
        s = nearest(status, p["frame"])
        delta = abs(int(s["frame"]) - int(p["frame"]))
        if delta > args.tol:
            mismatches.append({"frame": p["frame"], "reason": "no status within %d frames (nearest delta %d)"
                               % (args.tol, delta)})
            continue

        problems = []
        if p.get("activeHeroID") != s.get("activeHeroId"):
            problems.append("activeHeroID %r != %r" % (p.get("activeHeroID"), s.get("activeHeroId")))
        cell = p.get("activeCell") or {}
        if (cell.get("row"), cell.get("col")) != (s.get("activeRow"), s.get("activeColumn")):
            problems.append("activeCell %r != %r" % (cell, (s.get("activeRow"), s.get("activeColumn"))))
        if "paused" in p and bool(p["paused"]) != bool(s.get("paused")):
            problems.append("paused %r != %r" % (p.get("paused"), s.get("paused")))
        if "nowActiveState" in p and p["nowActiveState"] != s.get("nowActiveState"):
            problems.append("nowActiveState %r != %r" % (p.get("nowActiveState"), s.get("nowActiveState")))
        if "activeMoved" in p and bool(p["activeMoved"]) != bool(s.get("moved")):
            problems.append("moved %r != %r" % (p.get("activeMoved"), s.get("moved")))
        if "activeAttacked" in p and bool(p["activeAttacked"]) != bool(s.get("attacked")):
            problems.append("attacked %r != %r" % (p.get("activeAttacked"), s.get("attacked")))

        if problems:
            mismatches.append({"probe_frame": p["frame"], "status_frame": s["frame"],
                               "delta": delta, "problems": problems})
        else:
            matched += 1

    transitions = sum(1 for a, b in zip(status, status[1:]) if a["stateHash"] != b["stateHash"])
    same = len(status) - 1 - transitions
    print("frames matched: %d/%d (tol +/-%d frames)" % (matched, len(probe), args.tol))
    print("status samples: %d  consecutive hash changes: %d  consecutive identical: %d"
          % (len(status), transitions, same))
    cost = status[-1].get("cost") or {}
    print("main-thread cost: p99=%.4fms max=%.4fms errors=%s samples=%s"
          % (cost.get("p99", 0.0), cost.get("max", 0.0), cost.get("errors"), cost.get("samples")))
    for m in mismatches:
        print("MISMATCH " + json.dumps(m, ensure_ascii=False))
    return 0 if matched == len(probe) else 1


if __name__ == "__main__":
    sys.exit(main())
