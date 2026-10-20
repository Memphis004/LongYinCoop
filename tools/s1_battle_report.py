"""S1 capture analyzer — marker pairing, capture rate, seq integrity, snapshot coverage.

Usage: python tools/s1_battle_report.py "<capture dir>"
Read-only. Skips a trailing partial line (writer may still be appending).
"""
import json
import os
import sys
from collections import Counter, defaultdict


def lines(path):
    out = []
    bad = 0
    with open(path, "rb") as f:
        for i, raw in enumerate(f):
            s = raw.decode("utf-8", "replace").strip()
            if not s:
                continue
            try:
                out.append(json.loads(s))
            except Exception:
                bad += 1
    return out, bad


def main():
    d = sys.argv[1]
    mk, mk_bad = lines(os.path.join(d, "marker.ndjson"))
    lg, lg_bad = lines(os.path.join(d, "log.ndjson"))

    print("== files ==")
    for n in ("world", "battle", "log", "marker"):
        p = os.path.join(d, n + ".ndjson")
        print("  %-7s %10.1f MB" % (n, os.path.getsize(p) / 1e6))
    print("  unparsable: marker=%d log=%d (trailing partial incl.)" % (mk_bad, lg_bad))

    # ---- marker pairing
    battles = []
    pending = None
    unpaired_end = 0
    for m in mk:
        if m["kind"] == "battle:start":
            if pending is not None:
                battles.append({"start": pending, "end": None, "note": "superseded"})
            pending = m
        elif m["kind"] == "battle:end":
            if pending is None:
                unpaired_end += 1
                battles.append({"start": None, "end": m, "note": "end-without-start"})
            else:
                battles.append({"start": pending, "end": m})
                pending = None
    if pending is not None:
        battles.append({"start": pending, "end": None, "note": "still-open"})

    print("\n== markers ==")
    print("  marker events=%d  battles=%d  unpaired_end=%d  open_at_eof=%s"
          % (len(mk), len(battles), unpaired_end, pending is not None))
    print("  battleType: %s" % dict(Counter(
        (b["start"] or b["end"])["battleTypeName"] for b in battles)))

    # ---- log integrity
    print("\n== log ==")
    seqs = [e["seq"] for e in lg]
    dup = [s for s, c in Counter(seqs).items() if c > 1]
    gaps = 0
    for a, b in zip(seqs, seqs[1:]):
        if b != a + 1:
            gaps += 1
    print("  events=%d  distinct_seq=%d  dup=%d  non_monotonic_or_gap=%d"
          % (len(lg), len(set(seqs)), len(dup), gaps))
    print("  sources: %s" % dict(Counter(e.get("source") for e in lg)))

    # ---- per-battle log bucketing by frame window
    print("\n== per battle ==")
    print("  #  type        frames(start-end)   log_evts  sumΔinfoCount  rate      snapFrames  turns  stateEnd  dur(s)")
    total_evt = 0
    total_delta = 0
    rows = []
    for i, b in enumerate(battles, 1):
        s, e = b["start"], b["end"]
        if s is None or e is None:
            rows.append((i, b, None))
            continue
        lo, hi = s["frame"], e["frame"]
        evs = [x for x in lg if lo <= x["frame"] <= hi]
        delta = sum(x.get("infoDelta", 0) for x in evs)
        total_evt += len(evs)
        total_delta += delta
        rate = (100.0 * len(evs) / delta) if delta else float("nan")
        rows.append((i, b, (len(evs), delta, rate)))
        print("  %2d %-11s %7d-%7d  %8d  %8d  %s  %s  %5d  %-8s  %.1f"
              % (i, s["battleTypeName"], lo, hi, len(evs), delta,
                 ("%6.2f%%" % rate) if delta else "   n/a",
                 "?", e.get("turnIndex", -1), e.get("battleStateName"),
                 (e["t"] - s["t"]) / 1000.0))

    # events outside any battle window
    spans = [(b["start"]["frame"], b["end"]["frame"]) for b in battles
             if b["start"] and b["end"]]
    outside = [x for x in lg if not any(lo <= x["frame"] <= hi for lo, hi in spans)]
    print("\n  log events outside battle windows: %d" % len(outside))
    if outside:
        for x in outside[:5]:
            print("    frame=%d infoCount=%s text=%r" % (x["frame"], x.get("infoCount"), x["text"][:60]))

    print("\n  TOTAL: captured=%d  sumΔinfoCount=%d  literal_rate=%.2f%%"
          % (total_evt, total_delta, 100.0 * total_evt / total_delta if total_delta else 0))
    # counted-only rate: every infoCount++ must produce exactly one captured event
    d1 = sum(1 for x in lg if x.get("infoDelta") == 1)
    print("  counted-only: events_with_delta1=%d  sumΔinfoCount=%d  exact_match=%s"
          % (d1, total_delta, d1 == total_delta))
    print("  infoCount ceiling: max=%d  events_at_ceiling(delta==0)=%d"
          % (max((x.get("infoCount", 0) for x in lg), default=0),
             sum(1 for x in lg if x.get("infoDelta") == 0)))

    # per-battle: events where infoDelta==0 (popup/overlay not counted by game)
    zero_delta = [x for x in lg if x.get("infoDelta", 0) == 0]
    print("  events with infoDelta==0 (not counted by game counter): %d" % len(zero_delta))
    if zero_delta:
        print("    examples: %s" % [x["text"][:40] for x in zero_delta[:5]])

    # ---- battle snapshot coverage
    bp = os.path.join(d, "battle.ndjson")
    print("\n== battle snapshot ==")
    per_battle = defaultdict(lambda: {"frames": 0, "unitCount": Counter(), "types": Counter(),
                                      "stateNames": Counter(), "noActive": 0,
                                      "stableIds": set(), "noHeroId": 0, "noName": 0,
                                      "grid": Counter()})
    seqb = []
    cur = None
    bframes = sorted(spans)
    for fr in lines(bp)[0]:
        seqb.append(fr["seq"])
        f = fr["frame"]
        idx = None
        for j, (lo, hi) in enumerate(bframes):
            if lo <= f <= hi:
                idx = j
                break
        key = idx if idx is not None else "outside"
        p = per_battle[key]
        p["frames"] += 1
        p["types"][fr.get("battleTypeName")] += 1
        p["unitCount"][len(fr.get("units") or [])] += 1
        p["stateNames"][fr.get("battleStateName")] += 1
        if fr.get("nowActiveUnit") is None:
            p["noActive"] += 1
        p["grid"]["%sx%s" % (fr.get("gridW"), fr.get("gridH"))] += 1
        for u in fr.get("units") or []:
            # heroId < 0 = summon (heroData present, heroID unset) — NOT a missing hero
            if u.get("heroId", -1) < 0:
                p["noHeroId"] += 1
            if u.get("name") is None:
                p["noName"] += 1
            p["stableIds"].add(u.get("stableId"))
    dupb = [s for s, c in Counter(seqb).items() if c > 1]
    gapsb = sum(1 for a, b in zip(seqb, seqb[1:]) if b != a + 1)
    print("  frames=%d  dup_seq=%d  gap=%d" % (len(seqb), len(dupb), gapsb))
    for key in sorted([k for k in per_battle if k != "outside"], key=lambda x: str(x)):
        p = per_battle[key]
        print("  battle %s: frames=%d types=%s states=%s units=%s grid=%s noActive=%d summon(heroId<0)=%d noName=%d uniqStableId=%d"
              % (key + 1 if isinstance(key, int) else key, p["frames"], dict(p["types"]),
                 dict(p["stateNames"]), dict(p["unitCount"]), dict(p["grid"]),
                 p["noActive"], p["noHeroId"], p["noName"], len(p["stableIds"])))
    if "outside" in per_battle:
        p = per_battle["outside"]
        print("  outside battles: frames=%d units=%s" % (p["frames"], dict(p["unitCount"])))


if __name__ == "__main__":
    main()
