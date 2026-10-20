"""Join Frida UI probes to captured battle frames by exact frame number.

Usage: python tools/compare_ui_frame.py --probe evidence/.../ui-probe.ndjson --capture <capturedir>
Each probe line is the JSON string returned by tools/ui-probe.js (one per line).
"""
import argparse
import json


def load_frames(path):
    by_frame = {}
    with open(path, "r", encoding="utf-8", errors="replace") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                b = json.loads(line)
            except Exception:
                continue  # in-flight tail line
            by_frame[b.get("frame")] = b
    return by_frame


def nearest(frames_sorted, target, tol):
    """BattleSampler writes ~every 3rd frame, so an exact frame is usually absent."""
    lo, hi = 0, len(frames_sorted) - 1
    while lo < hi:
        mid = (lo + hi) // 2
        if frames_sorted[mid] < target:
            lo = mid + 1
        else:
            hi = mid
    best = None
    for i in (lo - 1, lo):
        if 0 <= i < len(frames_sorted):
            f = frames_sorted[i]
            d = abs(f - target)
            if best is None or d < best[1]:
                best = (f, d)
    if best and best[1] <= tol:
        return best
    return None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--probe", required=True)
    ap.add_argument("--capture", required=True)
    ap.add_argument("--tol", type=int, default=2, help="max frame distance when joining")
    args = ap.parse_args()

    frames = load_frames(args.capture + "/battle.ndjson")
    frame_keys = sorted(frames.keys())
    rows = []
    with open(args.probe, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            rows.append(json.loads(line))

    ok = 0
    print("| # | frame | matched | active | hp 3 units (frida vs capture) | cell |")
    print("|---|---|---|---|---|---|")
    for i, p in enumerate(rows, 1):
        fr = p.get("frame")
        hit = nearest(frame_keys, fr, args.tol)
        if not hit:
            print("| %d | %s | no capture frame within %d | - | - | - |" % (i, fr, args.tol))
            continue
        fr, dframe = hit
        cap = frames[fr]
        p = dict(p)
        p["__dframe"] = dframe
        cup = {u["stableId"]: u for u in cap.get("units") or []}
        good = True
        cells = []
        for u in (p.get("units") or [])[:3]:
            sid_parts = None
            match = None
            for sid, cu in cup.items():
                if cu.get("heroId") == u["heroID"] and cu.get("teamIndex") == u["teamIndex"]:
                    match = (sid, cu)
                    break
            if match is None:
                good = False
                cells.append("heroID %s missing" % u["heroID"])
                continue
            sid, cu = match
            if round(cu.get("hp") or 0, 3) != round(u["hp"], 3):
                good = False
            cells.append("%s:%s/%s" % (u["heroID"], round(u["hp"], 1), round(cu.get("hp") or 0, 1)))
        act_ok = (p.get("activeHeroID") is None and cap.get("activeId") is None) or True
        if p.get("activeHeroID") is not None:
            act_ok = any(cu.get("heroId") == p["activeHeroID"] for cu in cup.values())
        cell_ok = True
        for u in (p.get("units") or []):
            for cu in cup.values():
                if cu.get("heroId") == u["heroID"] and cu.get("teamIndex") == u["teamIndex"]:
                    if cu.get("cell", {}).get("x") != u["col"] or cu.get("cell", {}).get("y") != u["row"]:
                        cell_ok = False
        if good and act_ok and cell_ok:
            ok += 1
        print("| %d | %s (Δ%d) | %s | active=%s | %s | cellOk=%s |" % (
            i, fr, dframe, "yes" if good else "NO", p.get("activeHeroID"), ", ".join(cells), cell_ok))
    print("\nmatched_frames=%d/%d" % (ok, len(rows)))


if __name__ == "__main__":
    main()
