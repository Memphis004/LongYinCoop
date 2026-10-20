#!/usr/bin/env python3
"""S1.5-c run 2 — analyse spike-full NDJSON: damage via units[].hp, pause, speed.

Read-only over already-captured evidence. Usage:
    python evidence/<run>/analyze-run2.py evidence/<run>/logs/spike-full-<x>.ndjson
"""
import datetime
import json
import sys
from collections import Counter


def ts(t):
    return datetime.datetime.fromtimestamp(t).strftime("%H:%M:%S.%f")[:-3]


def load(path):
    out = []
    for line in open(path, encoding="utf-8"):
        line = line.strip()
        if not line:
            continue
        r = json.loads(line)
        if r.get("ok") and not r.get("error"):
            out.append(r)
    return out


def main():
    recs = load(sys.argv[1])
    print("file      :", sys.argv[1])
    print("records   :", len(recs))
    print("window    :", ts(recs[0]["t"]), "->", ts(recs[-1]["t"]),
          "(%.1fs)" % (recs[-1]["t"] - recs[0]["t"]))
    print("states    :", dict(Counter(r.get("battleStateName") for r in recs)))
    print("active    :", dict(Counter(r.get("nowActiveStateName") for r in recs)))
    print("paused    :", dict(Counter(r.get("paused") for r in recs)))
    print("timeScale :", dict(Counter(r.get("timeScale") for r in recs)))
    print("turns     :", dict(Counter(r.get("turnIndex") for r in recs)))

    # records captured outside a battle carry units = null; unit sections use only
    # the samples that have a unit list
    with_units = [r for r in recs if isinstance(r.get("units"), list) and r["units"]]
    if not with_units:
        print("\n(no samples with units - nothing to compare per unit)")
        return 0

    print("\n== per-unit first -> last")
    first = {u["id"]: u for u in with_units[0]["units"]}
    last = {u["id"]: u for u in with_units[-1]["units"]}
    for uid in first:
        a, b = first[uid], last.get(uid, {})
        print("  %-9s hp %9.3f -> %9.3f  originHp %9.3f -> %9.3f  alive %s->%s"
              % (uid, a["hp"], b.get("hp", float("nan")), a["originHp"],
                 b.get("originHp", float("nan")), a["alive"], b.get("alive")))

    print("\n== hp/alive transitions")
    prev = dict(first)
    n = 0
    for r in with_units[1:]:
        for u in r["units"]:
            p = prev.get(u["id"])
            if p and (abs(u["hp"] - p["hp"]) > 1e-6 or u["alive"] != p["alive"]):
                n += 1
                print("  %s %-9s hp %9.3f -> %9.3f (d=%+8.3f)  originHp %9.3f -> %9.3f"
                      "  alive %s->%s  state=%s"
                      % (ts(r["t"]), u["id"], p["hp"], u["hp"], u["hp"] - p["hp"],
                         p["originHp"], u["originHp"], p["alive"], u["alive"],
                         r.get("nowActiveStateName")))
            prev[u["id"]] = u
    print("  total transitions:", n)

    print("\n== battleTime segments (wall >= 0.3 s)")
    segs, cur = [], None
    for r in recs:
        st = r.get("nowActiveStateName")
        if cur is None or st != cur["state"]:
            if cur:
                segs.append(cur)
            cur = {"state": st, "t0": r["t"], "bt0": r["battleTime"]}
        cur["t1"] = r["t"]
        cur["bt1"] = r["battleTime"]
    if cur:
        segs.append(cur)
    for s in segs:
        w = s["t1"] - s["t0"]
        if w < 0.3:
            continue
        print("  %s -> %s %-16s wall=%6.2fs battleTime %+8.4f"
              % (ts(s["t0"]), ts(s["t1"]), s["state"], w, s["bt1"] - s["bt0"]))

    print("\n== timeScale != 1 frames")
    for r in recs:
        if r.get("timeScale") != 1:
            print("  %s scale=%s deltaTime=%.6f unscaled=%.6f state=%s"
                  % (ts(r["t"]), r["timeScale"], r.get("deltaTime", 0),
                     r.get("unscaledDeltaTime", 0), r.get("nowActiveStateName")))

    print("\n== battleTimeScale values")
    print(" ", dict(Counter(r.get("battleTimeScale") for r in recs)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
