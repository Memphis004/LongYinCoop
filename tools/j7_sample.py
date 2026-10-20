#!/usr/bin/env python3
"""S1.5-c J7: sample `spike status` every 100 ms to tell ATB from turn-based.

Read-only: only sends {"cmd":"spike","sub":"status"}.
While the player stands still on their own turn, if `battleTime` keeps rising and
`turnIndex` advances by itself, the battle is ATB (S1.5-plan §7.2 NO-GO).

Output: ndjson, one record per sample, plus a plain summary at the end.
"""
import argparse
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import coopctl  # noqa: E402

FIELDS = ("frame", "inBattle", "battleState", "nowActiveState", "nowActiveStateName",
          "active", "isPlayerTurn", "turnIndex", "paused", "playingAnim",
          "battleTime", "timeLimit", "aiWaitTime", "timeScale", "unitCount")


def stamp():
    return time.strftime("%H:%M:%S")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--seconds", type=float, default=70.0)
    ap.add_argument("--interval", type=float, default=0.1)
    args = ap.parse_args()

    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)

    pid = coopctl.find_pid()
    if not pid:
        print("[%s] game not running" % stamp(), flush=True)
        return 2
    f = coopctl.connect(pid)
    print("[%s] connected pid=%d interval=%.0fms seconds=%.0f"
          % (stamp(), pid, args.interval * 1000, args.seconds), flush=True)

    recs = []
    errors = 0
    end = time.time() + args.seconds
    t0 = time.time()
    next_report = t0 + 5.0

    with open(args.out, "w", encoding="utf-8") as out:
        while time.time() < end:
            t = time.time()
            try:
                res = coopctl.request(f, "spike", {"sub": "status"})
            except Exception as exc:
                errors += 1
                out.write(json.dumps({"t": round(t, 3), "error": str(exc)}) + "\n")
                out.flush()
                time.sleep(args.interval)
                continue

            rec = {"t": round(t, 3), "ok": bool(res.get("ok"))}
            if res.get("ok"):
                s = res.get("result") or {}
                for k in FIELDS:
                    rec[k] = s.get(k)
                units = s.get("units") or []
                rec["units"] = [[u.get("id"), u.get("hp"), u.get("row"), u.get("col"),
                                 u.get("alive")] for u in units]
                recs.append(rec)
            else:
                errors += 1
            out.write(json.dumps(rec, ensure_ascii=False) + "\n")
            out.flush()

            if t >= next_report:
                next_report = t + 5.0
                print("[%s] n=%d turn=%s state=%s t=%.3f scale=%s hp0=%s"
                      % (stamp(), len(recs), rec.get("turnIndex"), rec.get("nowActiveStateName"),
                         rec.get("battleTime") or -1, rec.get("timeScale"),
                         (rec.get("units") or [[None, None]])[0][1]), flush=True)

            sleep = args.interval - (time.time() - t)
            if sleep > 0:
                time.sleep(sleep)

    f.close()

    # ---- summary ----
    bt = [r["battleTime"] for r in recs if isinstance(r.get("battleTime"), (int, float))]
    turns = [r.get("turnIndex") for r in recs]
    states = [r.get("nowActiveState") for r in recs]
    hp_changes = 0
    prev = None
    for r in recs:
        cur = r.get("units")
        if prev is not None and cur != prev:
            hp_changes += 1
        prev = cur
    d_bt = (bt[-1] - bt[0]) if len(bt) >= 2 else 0.0
    wall = recs[-1]["t"] - recs[0]["t"] if len(recs) >= 2 else 0.0
    turn_seq = []
    for t in turns:
        if not turn_seq or turn_seq[-1] != t:
            turn_seq.append(t)
    state_seq = []
    for s in states:
        if not state_seq or state_seq[-1] != s:
            state_seq.append(s)

    print("[%s] J7_SUMMARY samples=%d errors=%d wall=%.2fs battleTime_delta=%.3f ratio=%.2f"
          % (stamp(), len(recs), errors, wall, d_bt, (d_bt / wall) if wall > 0 else 0.0), flush=True)
    print("  turnIndex sequence: %s" % turn_seq[:12], flush=True)
    print("  nowActiveState sequence: %s" % state_seq[:12], flush=True)
    print("  unit-hp changes: %d" % hp_changes, flush=True)
    print("  aiWaitTime range: %s..%s" % (
        min([r.get("aiWaitTime") for r in recs if isinstance(r.get("aiWaitTime"), (int, float))] or [-1]),
        max([r.get("aiWaitTime") for r in recs if isinstance(r.get("aiWaitTime"), (int, float))] or [-1])),
        flush=True)
    return 0 if errors == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
