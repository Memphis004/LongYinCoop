"""Sample worldTime from the mod over IPC at a fixed interval (read-only).

Usage:
  python tools/worldtime_watch.py --samples 5 --interval 30 --out evidence/.../worldtime-ipc.ndjson

Each line is one IPC `snapshot world` result:
  {wall, frame, year, month, day, hour, hudTime, scene, partyPos, ageMs}
The Frida side is sampled separately by the caller (il2cpp exec_js) so a
mismatch between the two can be attributed to the sampler, not to the game.
"""
import argparse
import datetime
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from coopctl import call, find_pid  # noqa: E402


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--samples", type=int, default=5)
    ap.add_argument("--interval", type=float, default=30.0)
    ap.add_argument("--out", default=None, help="NDJSON path (appended)")
    ap.add_argument("--pid", type=int, default=None)
    args = ap.parse_args()

    pid = args.pid or find_pid()
    out = open(args.out, "a", encoding="utf-8") if args.out else None
    try:
        for i in range(args.samples):
            if i:
                time.sleep(args.interval)
            wall = datetime.datetime.now().strftime("%H:%M:%S")
            try:
                res = call(pid, "snapshot", extra={"what": "world"})
                s = (res.get("result") or {}).get("sample") or {}
                row = {
                    "wall": wall,
                    "frame": s.get("frame"),
                    "scene": s.get("scene"),
                    "year": s.get("year"),
                    "month": s.get("month"),
                    "day": s.get("day"),
                    "hour": s.get("hour"),
                    "hudTime": (s.get("hud") or {}).get("time"),
                    "partyPos": s.get("partyPos"),
                    "ageMs": (res.get("result") or {}).get("ageMs"),
                    "ok": bool(res.get("ok")),
                }
            except Exception as e:  # sampling must not die on one bad read
                row = {"wall": wall, "error": str(e)}
            line = json.dumps(row, ensure_ascii=False)
            print(line, flush=True)
            if out:
                out.write(line + "\n")
                out.flush()
    finally:
        if out:
            out.close()


if __name__ == "__main__":
    main()
