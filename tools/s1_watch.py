"""S1 world-map watcher: poll `stats` + `snapshot world` and write NDJSON.

Usage: python tools/s1_watch.py --seconds 600 --out evidence/S1-.../world-watch.ndjson
Read-only: uses the existing IPC client in coopctl.py, no game state is touched.
"""
import argparse
import json
import time

import coopctl


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--seconds", type=int, default=600)
    ap.add_argument("--interval", type=float, default=15.0)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    pid = coopctl.find_pid()
    if not pid:
        raise SystemExit("game not running")

    end = time.time() + args.seconds
    n = 0
    with open(args.out, "w", encoding="utf-8") as f:
        while time.time() < end:
            t0 = time.time()
            row = {"wall": round(t0, 3)}
            try:
                st = coopctl.call(pid, "stats")
                row["stats"] = st.get("result")
                row["statsOk"] = st.get("ok")
            except Exception as e:
                row["statsErr"] = repr(e)
            try:
                w = coopctl.call(pid, "snapshot", extra={"what": "world", "n": 1})
                res = w.get("result") or {}
                s = res.get("sample") or {}
                row["worldOk"] = w.get("ok")
                row["ageMs"] = res.get("ageMs")
                row["sample"] = {
                    "t": s.get("t"),
                    "frame": s.get("frame"),
                    "scene": s.get("scene"),
                    "worldTime": s.get("worldTime"),
                    "partyPos": s.get("partyPos"),
                    "npcCount": len(s.get("npcs") or []),
                    "collectMs": s.get("collectMs"),
                    "money": (s.get("resources") or {}).get("money"),
                }
            except Exception as e:
                row["worldErr"] = repr(e)
            row["callMs"] = round((time.time() - t0) * 1000.0, 3)
            f.write(json.dumps(row, ensure_ascii=False) + "\n")
            f.flush()
            n += 1
            time.sleep(max(0.0, args.interval - (time.time() - t0)))
    print("done samples=%d out=%s" % (n, args.out))


if __name__ == "__main__":
    main()
