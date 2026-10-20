#!/usr/bin/env python3
"""S1.5-b: sample `spike status` (read-only) and write NDJSON for evidence.

Usage:
    python tools/spike_capture.py --seconds 30 --out evidence/S1.5-<ts>/spike-status.ndjson

The script only sends {"cmd":"spike","sub":"status"} - it never sends a
state-changing command and never touches the save folder.
"""
import argparse
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import coopctl  # noqa: E402  (reuse the pipe client in the same repo)

FIELDS = (
    "frame", "inBattle", "battleState", "battleStateName", "nowActiveState",
    "nowActiveStateName", "active", "activeHeroId", "activeName",
    "activePlayerControl", "activeAutoFight", "activeRow", "activeColumn",
    "isPlayerTurn", "moved", "attacked", "turnMoved", "turnAttacked",
    "turnFlagsAvailable", "paused", "playingAnim", "infoCount", "turnIndex",
    "unitCount", "stateHash", "costMs",
)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--seconds", type=float, default=30.0)
    ap.add_argument("--interval", type=float, default=0.0, help="sleep between requests (s)")
    ap.add_argument("--out", required=True)
    ap.add_argument("--pid", type=int, default=None)
    args = ap.parse_args()

    pid = args.pid or coopctl.find_pid()
    if not pid:
        print("game not running (or pass --pid)", file=sys.stderr)
        return 2

    out_path = os.path.abspath(args.out)
    os.makedirs(os.path.dirname(out_path), exist_ok=True)

    f = coopctl.connect(pid)
    samples = 0
    errors = 0
    hashes = []
    cost_p99 = 0.0
    cost_errors = 0
    try:
        with open(out_path, "w", encoding="utf-8") as out:
            t_end = time.time() + args.seconds
            while time.time() < t_end:
                t0 = time.perf_counter()
                try:
                    res = coopctl.request(f, "spike", {"sub": "status"})
                except Exception as exc:  # pipe closed / game exited
                    errors += 1
                    out.write(json.dumps({"t": round(time.time(), 3), "error": str(exc)}) + "\n")
                    out.flush()
                    time.sleep(0.05)
                    continue

                rtt = (time.perf_counter() - t0) * 1000.0
                rec = {"t": round(time.time(), 3), "rttMs": round(rtt, 3), "ok": bool(res.get("ok"))}
                if res.get("ok"):
                    status = res.get("result") or {}
                    for key in FIELDS:
                        rec[key] = status.get(key)
                    rec["cost"] = status.get("cost")
                    rec["actionPatches"] = status.get("actionPatches")
                    hashes.append(status.get("stateHash"))
                    cost = status.get("cost") or {}
                    if isinstance(cost, dict):
                        cost_p99 = max(cost_p99, cost.get("p99") or 0.0)
                        cost_errors = max(cost_errors, cost.get("errors") or 0)
                    samples += 1
                else:
                    errors += 1
                out.write(json.dumps(rec, ensure_ascii=False) + "\n")
                out.flush()

                if args.interval > 0:
                    time.sleep(args.interval)
    finally:
        f.close()

    transitions = sum(1 for a, b in zip(hashes, hashes[1:]) if a != b)
    idle = len(hashes) - 1 - transitions
    print("samples=%d errors=%d distinct_hashes=%d consecutive_hash_changes=%d consecutive_same=%d"
          % (samples, errors, len(set(hashes)), transitions, idle))
    print("main_thread_cost_p99=%.4fms cost_errors=%d" % (cost_p99, cost_errors))
    print("wrote %s" % out_path)
    return 0 if errors == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
