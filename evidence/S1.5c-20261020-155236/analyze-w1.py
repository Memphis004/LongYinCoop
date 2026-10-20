"""W1 analysis: correlate jitprobe click events (OnClick / BattleGridClicked) with samples.

Inputs
  events : jitprobe-*.ndjson (2 lines per click: BattleGridClicked then GridUnitController.OnClick)
  status : spike-status-w1.ndjson (spike status samples, ~120 Hz)

Output = plain text facts only (no interpretation).
"""
import json
import sys

EVENTS = sys.argv[1]
STATUS = sys.argv[2]
WINDOW = int(sys.argv[3]) if len(sys.argv) > 3 else 150


def load_clicks(path):
    rows = []
    with open(path, encoding="utf-8-sig") as f:
        for line in f:
            line = line.strip()
            if line:
                rows.append(json.loads(line))
    clicks = []
    for i, r in enumerate(rows):
        if r["method"] != "GridUnitController.OnClick":
            continue
        prev = rows[i - 1] if i else None
        clicks.append({
            "frame": r["frame"],
            "row": r.get("row"),
            "col": r.get("col"),
            "obj": r.get("obj"),
            "bc_frame": prev["frame"] if prev and prev["method"] == "BattleController.BattleGridClicked" else None,
            "bc_obj": prev.get("obj") if prev and prev["method"] == "BattleController.BattleGridClicked" else None,
        })
    return rows, clicks


def load_samples(path):
    out = []
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            r = json.loads(line)
            if r.get("ok"):
                out.append(r)
    return out


def main():
    rows, clicks = load_clicks(EVENTS)
    samples = load_samples(STATUS)
    print("EVENTS lines=%d clicks=%d" % (len(rows), len(clicks)))
    bc = sum(1 for r in rows if r["method"] == "BattleController.BattleGridClicked")
    print("BattleGridClicked lines=%d  OnClick lines=%d" % (bc, len(clicks)))
    paired = [c for c in clicks if c["bc_frame"] is not None and c["bc_obj"] == c["obj"]]
    same_frame = [c for c in paired if c["bc_frame"] == c["frame"]]
    before = [c for c in paired if c["bc_frame"] is not None and c["bc_frame"] <= c["frame"]]
    print("paired(obj equal)=%d  bc_frame<=onclick_frame=%d  same_frame=%d"
          % (len(paired), len(before), len(same_frame)))

    print("\nSAMPLES ok=%d frame=[%d..%d]" % (len(samples), samples[0]["frame"], samples[-1]["frame"]))
    turns = {}
    for s in samples:
        t = s.get("turnIndex")
        d = turns.setdefault(t, {"n": 0, "act": set(), "paused": 0, "moved": 0, "attacked": 0,
                                 "cells": {}, "atk_frames": set(), "moved_frames": set()})
        d["n"] += 1
        if s.get("active") is not None:
            d["act"].add(s["active"])
        if s.get("paused"):
            d["paused"] += 1
        if s.get("turnMoved"):
            d["moved"] += 1
            d["moved_frames"].add(s["frame"])
        if s.get("turnAttacked"):
            d["attacked"] += 1
        d["atk_frames"].add(s.get("turnAttacked"))
        cell = (s.get("activeRow"), s.get("activeColumn"))
        d["cells"][cell] = d["cells"].get(cell, 0) + 1

    print("\n%-4s %6s %-10s %6s %6s %6s %8s %s" %
          ("turn", "samp", "active", "moved", "atk", "paused", "turnAttacked", "top cells"))
    for t in sorted(turns, key=lambda x: (x is None, x)):
        d = turns[t]
        top = sorted(d["cells"].items(), key=lambda kv: -kv[1])[:3]
        af = [a for a in d["atk_frames"] if a is not None]
        print("%-4s %6d %-10s %6d %6d %6d %8s %s" %
              (t, d["n"], ",".join(sorted(d["act"])), d["moved"], d["attacked"], d["paused"],
               ("T" if any(af) else "-"),
               " ".join("%s:%d" % (c, k) for c, k in top)))

    print("\ntotal paused samples=%d / %d  (%.3f%%)"
          % (sum(d["paused"] for d in turns.values()), len(samples),
             100.0 * sum(d["paused"] for d in turns.values()) / max(1, len(samples))))

    # --- per-click effect ---
    print("\n--- per click: state change within %d frames ---" % WINDOW)
    print("%-6s %-8s %-9s %-9s %-9s %-8s %s" %
          ("frame", "cell", "turn", "base", "movedHits", "atkChg", "verdict"))
    verdicts = {"move": 0, "attack": 0, "select": 0, "none": 0}
    by_frame = {s["frame"]: s for s in samples}
    frames = [s["frame"] for s in samples]

    def base_at(fr):
        lo, hi = 0, len(frames) - 1
        if fr < frames[0]:
            return None
        while lo < hi:
            mid = (lo + hi + 1) // 2
            if frames[mid] <= fr:
                lo = mid
            else:
                hi = mid - 1
        return samples[lo]

    for c in clicks:
        b = base_at(c["frame"])
        if b is None:
            continue
        win = [s for s in samples if c["frame"] < s["frame"] <= c["frame"] + WINDOW]
        turned_moved = any(s.get("turnMoved") and not b.get("turnMoved") for s in win)
        atk_chg = any(s.get("attacked") != b.get("atkFrame") for s in win)
        on_cell = [s for s in win if (s.get("activeRow"), s.get("activeColumn")) == (c["row"], c["col"])
                   and s.get("turnMoved")]
        if turned_moved and on_cell:
            verdict = "move"
        elif atk_chg:
            verdict = "attack"
        elif any((s.get("activeRow"), s.get("activeColumn")) == (c["row"], c["col"]) for s in win):
            verdict = "select"
        else:
            verdict = "none"
        verdicts[verdict] += 1
        print("%-6d %-8s %-9s %-9s %-9d %-8s %s" %
              (c["frame"], "%s_%s" % (c["row"], c["col"]), b.get("turn"),
               "moved=%s atk=%s" % (b.get("turnMoved"), b.get("turnAttacked")),
               len(on_cell), atk_chg, verdict))

    print("\nverdict counts=%s" % verdicts)
    md = [c for c in clicks]
    print("clicks with >=5 frames unit-stays-on-clicked-cell AND turnMoved: %d"
          % sum(1 for c in md
                if len([s for s in samples
                        if c["frame"] < s["frame"] <= c["frame"] + WINDOW
                        and (s.get("activeRow"), s.get("activeColumn")) == (c["row"], c["col"]) and s.get("turnMoved")]) >= 5))
    # --- flag transitions vs preceding click ---
    cl_frames = [c["frame"] for c in clicks]

    def transitions(field):
        out = []
        for a, b in zip(samples, samples[1:]):
            if b.get(field) and not a.get(field):
                prev = [f for f in cl_frames if f <= b["frame"]]
                out.append((b["frame"], b.get("turnIndex"), b.get("isPlayerTurn"),
                            prev[-1] if prev else None))
        return out

    mv = transitions("turnMoved")
    print("\nturnMoved false->true transitions=%d" % len(mv))
    near = [t for t in mv if t[3] is not None and t[0] - t[3] <= 300]
    lags = sorted(t[0] - t[3] for t in near)
    print("  with a click within 300 frames before=%d" % len(near))
    if lags:
        print("  lag frames min=%d p50=%d max=%d" % (lags[0], lags[len(lags) // 2], lags[-1]))
    for f, t, pt, c in mv:
        print("  frame=%d turn=%s playerTurn=%s click_before=%s lag=%s"
              % (f, t, pt, c, (f - c) if c else None))

    at = transitions("turnAttacked")
    print("\nturnAttacked false->true transitions=%d (isPlayerTurn at transition: %s)"
          % (len(at), sorted(set(str(t[2]) for t in at))))
    atn = [t for t in at if t[3] is not None and t[0] - t[3] <= 300]
    print("  with a click within 300 frames before=%d (includes AI turns; see report)" % len(atn))

    print("\nmax consecutive matching frames per click:")
    for c in clicks:
        win = [s for s in samples if c["frame"] < s["frame"] <= c["frame"] + 2000
               and (s.get("activeRow"), s.get("activeColumn")) == (c["row"], c["col"]) and s.get("turnMoved")]
        if win:
            print("  frame=%d cell=%s_%s matched=%d span=%d..%d turn=%s"
                  % (c["frame"], c["row"], c["col"], len(win), win[0]["frame"], win[-1]["frame"],
                     win[0].get("turnIndex")))


main()
