#!/usr/bin/env python3
"""S1.5-b: start recording `spike status` the moment the game waits for player input.

Waits for `nowActiveState == WaitPlayerInput` (2), then records status at max
rate for --seconds. Read-only: only sends {"cmd":"spike","sub":"status"}.
Progress is printed to stdout (ASCII only) so a log tail is enough to know
when recording has started.
"""
import argparse
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import coopctl  # noqa: E402

FIELDS = (
    "frame", "inBattle", "battleState", "battleStateName", "nowActiveState",
    "nowActiveStateName", "active", "activeHeroId", "activeName",
    "activePlayerControl", "activeAutoFight", "activeRow", "activeColumn",
    "isPlayerTurn", "moved", "attacked", "turnMoved", "turnAttacked",
    "paused", "playingAnim", "infoCount", "turnIndex", "unitCount",
    "stateHash", "costMs",
)


def stamp():
    return time.strftime("%H:%M:%S")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--seconds", type=float, default=90.0)
    ap.add_argument("--timeout", type=float, default=900.0)
    ap.add_argument("--interval", type=float, default=0.5)
    args = ap.parse_args()

    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    deadline = time.time() + args.timeout
    f = None
    pid = None

    while time.time() < deadline:
        pid = coopctl.find_pid()
        if not pid:
            time.sleep(2)
            continue
        try:
            f = coopctl.connect(pid)
            print("[%s] connected pid=%d - waiting for WaitPlayerInput" % (stamp(), pid), flush=True)
            break
        except Exception as exc:
            print("[%s] connect: %s" % (stamp(), exc), flush=True)
            time.sleep(1)

    if f is None:
        print("[%s] TIMEOUT waiting for game/pipe" % stamp(), flush=True)
        return 2

    ready = None
    while time.time() < deadline:
        try:
            res = coopctl.request(f, "spike", {"sub": "status"})
        except Exception as exc:
            print("[%s] pipe error: %s (reconnecting)" % (stamp(), exc), flush=True)
            try:
                f.close()
            except Exception:
                pass
            time.sleep(1)
            try:
                pid = coopctl.find_pid()
                if not pid:
                    print("[%s] game exited" % stamp(), flush=True)
                    return 3
                f = coopctl.connect(pid)
            except Exception:
                continue
            continue

        s = res.get("result") if res.get("ok") else None
        if s and s.get("inBattle") and s.get("battleState") == 3 and s.get("nowActiveState") == 2:
            ready = s
            break
        time.sleep(args.interval)

    if ready is None:
        print("[%s] TIMEOUT waiting for player input turn" % stamp(), flush=True)
        return 2

    print("[%s] AUTOCAPTURE_START frame=%s active=%s playerControl=%s paused=%s infoCount=%s"
          % (stamp(), ready.get("frame"), ready.get("active"),
             ready.get("activePlayerControl"), ready.get("paused"), ready.get("infoCount")),
          flush=True)

    samples = 0
    errors = 0
    hashes = []
    end = time.time() + args.seconds
    with open(args.out, "w", encoding="utf-8") as out:
        while time.time() < end:
            try:
                res = coopctl.request(f, "spike", {"sub": "status"})
            except Exception as exc:
                errors += 1
                out.write(json.dumps({"t": round(time.time(), 3), "error": str(exc)}) + "\n")
                out.flush()
                time.sleep(0.05)
                continue
            rec = {"t": round(time.time(), 3), "ok": bool(res.get("ok"))}
            if res.get("ok"):
                s = res.get("result") or {}
                for key in FIELDS:
                    rec[key] = s.get(key)
                hashes.append(s.get("stateHash"))
                samples += 1
            else:
                errors += 1
            out.write(json.dumps(rec, ensure_ascii=False) + "\n")
            out.flush()
            if samples and samples % 2000 == 0 and hashes:
                print("[%s] samples=%d lastFrame=%s inBattle=%s active=%s"
                      % (stamp(), samples, rec.get("frame"), rec.get("inBattle"), rec.get("active")),
                      flush=True)

    f.close()
    transitions = sum(1 for a, b in zip(hashes, hashes[1:]) if a != b)
    print("[%s] AUTOCAPTURE_DONE samples=%d errors=%d hash_changes=%d same=%d"
          % (stamp(), samples, errors, transitions, len(hashes) - 1 - transitions), flush=True)
    return 0 if errors == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
