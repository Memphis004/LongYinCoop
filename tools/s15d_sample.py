#!/usr/bin/env python3
"""S1.5-c (run 2): sample the FULL `spike status` payload every N ms.

Read-only: only sends {"cmd":"spike","sub":"status"}. One ndjson line per sample,
including the per-unit array (hp / originHp / alive / unitAlive), so damage,
pause and speed effects can all be read from the same series.

Usage:
    python tools/s15d_sample.py --out evidence/<run>/spike-full.ndjson --seconds 600 --interval 0.1
"""
import argparse
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import coopctl  # noqa: E402


def stamp():
    return time.strftime("%H:%M:%S")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--seconds", type=float, default=600.0)
    ap.add_argument("--interval", type=float, default=0.1)
    ap.add_argument("--append", action="store_true",
                    help="append to --out instead of truncating (for supervised restart loops)")
    args = ap.parse_args()

    out_path = os.path.abspath(args.out)
    os.makedirs(os.path.dirname(out_path), exist_ok=True)

    pid = coopctl.find_pid()
    if not pid:
        print("[%s] game not running" % stamp(), flush=True)
        return 2
    f = coopctl.connect(pid)
    print("[%s] connected pid=%d interval=%.0fms seconds=%.0f out=%s"
          % (stamp(), pid, args.interval * 1000, args.seconds, out_path), flush=True)

    n = 0
    errors = 0
    end = time.time() + args.seconds
    next_report = time.time() + 10.0

    with open(out_path, "a" if args.append else "w", encoding="utf-8") as out:
        while time.time() < end:
            t = time.time()
            try:
                res = coopctl.request(f, "spike", {"sub": "status"})
            except Exception as exc:
                errors += 1
                out.write(json.dumps({"t": round(t, 4), "error": str(exc)}) + "\n")
                out.flush()
                try:
                    f.close()
                except Exception:
                    pass
                time.sleep(1.0)
                try:
                    pid = coopctl.find_pid()
                    if not pid:
                        print("[%s] game exited" % stamp(), flush=True)
                        return 3
                    f = coopctl.connect(pid)
                except Exception:
                    pass
                continue

            r = res.get("result") or {}
            rec = {"t": round(t, 4), "ok": bool(res.get("ok"))}
            rec.update(r)
            out.write(json.dumps(rec, ensure_ascii=False) + "\n")
            n += 1
            if n % 20 == 0:
                out.flush()

            if time.time() >= next_report:
                print("[%s] samples=%d errors=%d state=%s turn=%s timeScale=%s"
                      % (stamp(), n, errors, r.get("battleStateName"),
                         r.get("turnIndex"), r.get("timeScale")), flush=True)
                next_report = time.time() + 10.0

            sleep = args.interval - (time.time() - t)
            if sleep > 0:
                time.sleep(sleep)

    print("[%s] done samples=%d errors=%d" % (stamp(), n, errors), flush=True)
    return 0 if errors == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
