#!/usr/bin/env python3
"""S2a-2 exit 4: per-unit compare, wire (s2-snapshots.ndjson) vs host truth (spike status via sampler).

Protocol v2 section 3: phase B stays enabled, one turn is played, then the per-unit fields are
compared against the host's own values for the same battle and the same turn.

Alignment: both series carry battleTime (3 dp) + turnIndex. While the battle waits for input the
game freezes both, so the frozen tail of the sampler is the stable "same turn" window; the last
snapshot frame with the same (battleTime, turnIndex) is compared unit-by-unit.

Read-only. No IPC call is made (the recorder owns the pipe).
"""
import json
import sys
import time

GAME = r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan"
SNAP = GAME + r"\UserData\LongYinCoop\S2\s2-snapshots.ndjson"
SAMPLER = r"C:\Users\memph\LongYinCoop\coop-test\S2a2-20261021-2254\sampler.ndjson"

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

FIELDS = ("hp", "row", "col", "unitAlive", "playerControl")


def load_sampler(path):
    rows = []
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                o = json.loads(line)
            except Exception:
                continue
            if "nowActiveStateName" in o:
                rows.append(o)
    return rows


def load_frames(path):
    frames = []
    with open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            line = line.strip()
            if not line or '"marker":true' in line:
                continue
            try:
                o = json.loads(line)
            except Exception:
                continue
            if "units" in o:
                frames.append(o)
    return frames


def key_of(battle_time, turn_index):
    return (round(float(battle_time), 3), int(turn_index))


def main():
    rows = load_sampler(SAMPLER)
    frames = load_frames(SNAP)
    if not rows or not frames:
        print("missing data: sampler=%d frames=%d" % (len(rows), len(frames)))
        return 2

    last = rows[-1]
    k = key_of(last["battleTime"], last["turnIndex"])
    print("frozen tail key (battleTime, turnIndex) = %s ; last sample %s"
          % (k, time.strftime("%H:%M:%S", time.localtime(last["t"]))))

    same = [r for r in rows if key_of(r["battleTime"], r["turnIndex"]) == k]
    print("sampler samples at this key: %d (window %.1f s)"
          % (len(same), same[-1]["t"] - same[0]["t"]))
    if len(same) < 4:
        print("WARN few samples at this key - the battle may not be frozen yet")

    cand = [f for f in frames if key_of(f["battleTime"], f["turnIndex"]) == k]
    if not cand:
        print("no snapshot frame with this key -> INCONCLUSIVE (alignment)")
        return 3
    fr = cand[-1]

    host = {}
    for u in last.get("units") or []:
        host[str(u.get("id"))] = u
    wire = {}
    for u in fr.get("units") or []:
        wire[str(u.get("id"))] = u

    print("frame seq=%s frameId=%s units=%d | sampler units=%d"
          % (fr.get("seq"), fr.get("frameId"), len(wire), len(host)))
    print("audit: frame=%s" % json.dumps(fr.get("audit"), ensure_ascii=True))
    print("sampler stateHash=%s hashText=%s"
          % (json.dumps(last.get("stateHash"), ensure_ascii=True)[:200], str(last.get("hashText"))[:80]))

    only_wire = sorted(set(wire) - set(host))
    only_host = sorted(set(host) - set(wire))
    if only_wire or only_host:
        print("UNIT SET MISMATCH only_wire=%s only_host=%s" % (only_wire, only_host))

    checks = 0
    bad = []
    for uid in sorted(set(wire) & set(host), key=lambda s: (len(s), s)):
        w, h = wire[uid], host[uid]
        for fld in FIELDS:
            if fld not in w or fld not in h:
                continue
            a, b = w[fld], h[fld]
            if isinstance(a, float) or isinstance(b, float):
                eq = abs(float(a) - float(b)) <= 0.0005
            else:
                eq = a == b
            checks += 1
            if not eq:
                bad.append((uid, fld, a, b))
    print("field comparisons=%d mismatches=%d" % (checks, len(bad)))
    for uid, fld, a, b in bad[:20]:
        print("  MISMATCH unit=%s field=%s wire=%r host=%r" % (uid, fld, a, b))

    ok = checks > 0 and not bad and not only_wire and not only_host
    print("RESULT: %s" % ("PASS" if ok else "FAIL"))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
