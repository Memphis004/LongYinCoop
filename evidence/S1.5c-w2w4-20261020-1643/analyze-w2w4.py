"""S1.5c W2/W4 + J7 analysis.

Inputs
  --w2   jitprobe-w2-*.ndjson   (postfix events from move-side handlers)
  --w4   jitprobe-w4-*.ndjson   (postfix events from BattlePauseButtonClicked)
  --st   status-100ms.ndjson    (100 ms `spike status` samples)
Facts only, no interpretation beyond counting.
"""
import argparse
import json
from collections import Counter


def load(path):
    out = []
    with open(path, encoding="utf-8-sig") as f:
        for line in f:
            line = line.strip()
            if line:
                out.append(json.loads(line))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--w2", required=True)
    ap.add_argument("--w4", required=True)
    ap.add_argument("--st", required=True)
    a = ap.parse_args()

    w2 = load(a.w2)
    w4 = load(a.w4)
    st = [r for r in load(a.st) if r.get("ok")]

    print("== W2 events: %d" % len(w2))
    for m, n in Counter(r["method"] for r in w2).most_common():
        print("   %-45s %d" % (m, n))

    print("\n== W2 sequence (first 40, frame order)")
    for r in w2[:40]:
        extra = ""
        if "targetRow" in r:
            extra = " target=%s_%s" % (r["targetRow"], r["targetCol"])
        print("   f=%-6d %-42s state=%s paused=%s row=%s col=%s%s"
              % (r["frame"], r["method"], r.get("nowActiveState"), r.get("paused"),
                 r.get("row"), r.get("col"), extra))

    # gaps between GenerateMovePath calls (rate sanity)
    gm = [r for r in w2 if r["method"] == "BattleController.GenerateMovePath"]
    if len(gm) >= 2:
        d = [b["frame"] - a2["frame"] for a2, b in zip(gm, gm[1:])]
        print("   GenerateMovePath frame gaps: min=%d max=%d (n=%d)" % (min(d), max(d), len(d)))

    print("\n== W4 events: %d" % len(w4))
    for r in w4:
        print("   f=%-6d obj=%-6s paused=%-5s state=%-3s t=%-7s scale=%s aiWait=%s"
              % (r["frame"], r.get("obj"), r.get("paused"), r.get("nowActiveState"),
                 r.get("battleTime"), r.get("timeScale"), r.get("aiWaitTime")))

    # does the 100 ms stream ever see paused=true ?
    paused_true = [r for r in st if r.get("paused")]
    print("\n== status stream: %d samples, paused=true in %d samples" % (len(st), len(paused_true)))
    for r in paused_true[:10]:
        print("   t=%.3f turn=%s state=%s paused=%s" % (r["t"], r.get("turnIndex"),
                                                        r.get("nowActiveStateName"), r.get("paused")))

    # window around the first W4 event
    if w4:
        t0 = w4[0]["t"] / 1000.0
        t1 = w4[-1]["t"] / 1000.0
        win = [r for r in st if t0 - 1.0 <= r["t"] <= t1 + 1.0]
        print("\n== samples inside W4 press window (%.3f..%.3f): %d" % (t0, t1, len(win)))
        seq = []
        for r in win:
            key = (r.get("paused"), r.get("nowActiveState"), r.get("battleTime"))
            if not seq or seq[-1][0] != key:
                seq.append((key, r["t"]))
        for (k, t) in seq[:20]:
            print("   t=%.3f paused=%s state=%s battleTime=%s" % (t, k[0], k[1], k[2]))

    # ---- J7: player-turn still segments ----
    print("\n== J7 segments (inBattle & isPlayerTurn), split on turnIndex/state/scale change")
    seg_start = None
    prev_key = None
    segs = []
    for r in st:
        key = (r.get("turnIndex"), r.get("nowActiveState"), r.get("timeScale"))
        active = bool(r.get("inBattle")) and r.get("isPlayerTurn")
        if active and (prev_key is None or key == prev_key):
            if seg_start is None:
                seg_start = r
            prev_key = key
        else:
            if seg_start is not None:
                segs.append((seg_start, last))
            seg_start = r if active else None
            prev_key = key if active else None
        last = r

    if seg_start is not None:
        segs.append((seg_start, last))

    for s, e in segs:
        wall = e["t"] - s["t"]
        bt = (e.get("battleTime") or 0) - (s.get("battleTime") or 0)
        if wall < 2:
            continue
        print("   turn=%-4s state=%-16s wall=%6.2fs battleTime=%.3f -> %.3f (d=%+.3f) ratio=%.2f"
              % (s.get("turnIndex"), s.get("nowActiveStateName"), wall,
                 s.get("battleTime") or -1, e.get("battleTime") or -1, bt,
                 (bt / wall) if wall > 0 else 0))

    print("\n== timeScale values seen: %s" % sorted(set(r.get("timeScale") for r in st)))
    print("== turnIndex values seen: %s" % sorted(set(r.get("turnIndex") for r in st)))
    turns = []
    for r in st:
        if not turns or turns[-1] != r.get("turnIndex"):
            turns.append(r.get("turnIndex"))
    print("== turnIndex transitions: %s" % turns[:25])


main()
