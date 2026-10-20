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
            # only in-battle samples may satisfy the UI comparison; an out-of-battle
            # sample would trivially "match" an empty probe and hide a real bug
            if rec.get("ok") and rec.get("inBattle") and rec.get("frame") is not None:
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

        # mod reports -1 when there is no active unit; the probe reports null
        def norm(value):
            return None if value in (None, -1) else value

        problems = []
        if norm(p.get("activeHeroID")) != norm(s.get("activeHeroId")):
            problems.append("activeHeroID %r != %r" % (p.get("activeHeroID"), s.get("activeHeroId")))
        cell = p.get("activeCell") or {}
        if norm(p.get("activeHeroID")) is not None:
            if (cell.get("row"), cell.get("col")) != (s.get("activeRow"), s.get("activeColumn")):
                problems.append("activeCell %r != %r" % (cell, (s.get("activeRow"), s.get("activeColumn"))))
        if "activePlayerControl" in p and bool(p["activePlayerControl"]) != bool(s.get("activePlayerControl")):
            problems.append("playerControl %r != %r" % (p.get("activePlayerControl"), s.get("activePlayerControl")))
        if "paused" in p and bool(p["paused"]) != bool(s.get("paused")):
            problems.append("paused %r != %r" % (p.get("paused"), s.get("paused")))
        if "nowActiveState" in p and p["nowActiveState"] != s.get("nowActiveState"):
            problems.append("nowActiveState %r != %r" % (p.get("nowActiveState"), s.get("nowActiveState")))
        if "activeMoved" in p and bool(p["activeMoved"]) != bool(s.get("moved")):
            problems.append("moved %r != %r" % (p.get("activeMoved"), s.get("moved")))
        if "activeAttacked" in p and bool(p["activeAttacked"]) != bool(s.get("attacked")):
            problems.append("attacked %r != %r" % (p.get("activeAttacked"), s.get("attacked")))
        # per-turn flags live on BattleController: the S1.5 TurnGate uses these, and (measured
        # 2026-10-20) BattleUnit.moved/attacked above never became true even after a real move
        if "turnMoved" in p and bool(p["turnMoved"]) != bool(s.get("turnMoved")):
            problems.append("turnMoved %r != %r" % (p.get("turnMoved"), s.get("turnMoved")))
        if "turnAttacked" in p and bool(p["turnAttacked"]) != bool(s.get("turnAttacked")):
            problems.append("turnAttacked %r != %r" % (p.get("turnAttacked"), s.get("turnAttacked")))

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
