#!/usr/bin/env python3
"""S1.5g analysis: pause/resume rounds + 10-minute stability, from read-only samples.

Inputs (both read-only artifacts, no game interaction):
  logs/status-100ms.ndjson   - full `spike status` payload every ~100 ms (s15d_sample.py)
  logs/jitprobe-w4.ndjson    - BattlePauseButtonClicked postfix (read-only hook), optional
  logs/coop-log-slice.txt    - slice of the mod log for the run (optional)

Outputs: printed report (also written by the caller with Tee-Object).
This script never sends a command to the game and never touches the save folder.
"""
import argparse
import collections
import json
import sys


def load_ndjson(path):
    rows = []
    # W4 ndjson is written with a UTF-8 BOM; "utf-8-sig" parses both cases.
    with open(path, "r", encoding="utf-8-sig") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                rows.append(json.loads(line))
            except json.JSONDecodeError:
                pass
    return rows


def fmt(t):
    import datetime
    return datetime.datetime.fromtimestamp(t).strftime("%H:%M:%S.%f")[:-3]


def pause_windows(samples):
    """Return list of dicts for every paused==True run of samples."""
    windows = []
    cur = None
    for s in samples:
        p = bool(s.get("paused"))
        if p and cur is None:
            cur = {"start_t": s["t"], "start": s, "samples": [s]}
        elif p and cur is not None:
            cur["samples"].append(s)
        elif not p and cur is not None:
            cur["end_t"] = s["t"]
            cur["end"] = s
            windows.append(cur)
            cur = None
        last = s
    if cur is not None:
        cur["end_t"] = None
        cur["end"] = None
        windows.append(cur)
    return windows


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--status", required=True)
    ap.add_argument("--w4", default=None)
    ap.add_argument("--log", default=None)
    ap.add_argument("--phase-b-start", type=float, default=None,
                    help="unix ts of the last Phase-A resume = start of the 600 s window")
    ap.add_argument("--phase-b-end", type=float, default=None,
                    help="unix ts of the end of the stability window (default: last sample)")
    ap.add_argument("--stable-seconds", type=float, default=600.0)
    ap.add_argument("--min-pause", type=float, default=0.3)
    args = ap.parse_args()

    rows = load_ndjson(args.status)
    ok = [r for r in rows if r.get("ok")]
    err = [r for r in rows if not r.get("ok")]
    if not ok:
        print("NO SAMPLES (ok=false or empty) - nothing to analyse")
        return 2

    print("== capture ==")
    print("samples_ok=%d errors=%d span=%.1fs (%s -> %s)"
          % (len(ok), len(err), ok[-1]["t"] - ok[0]["t"], fmt(ok[0]["t"]), fmt(ok[-1]["t"])))
    gaps = []
    for a, b in zip(ok, ok[1:]):
        d = b["t"] - a["t"]
        if d > 0.6:
            gaps.append((a["t"], d))
    print("sample_gaps>0.6s=%d max_gap=%.2fs"
          % (len(gaps), max([g[1] for g in gaps], default=0.0)))
    for t, d in gaps[:10]:
        print("   gap %.2fs at %s" % (d, fmt(t)))

    inb = [s for s in ok if s.get("inBattle")]
    print("in_battle_samples=%d" % len(inb))
    if inb:
        bt = [s.get("battleTime", -1) for s in inb]
        print("battleTime %.4f -> %.4f (delta %+.4f)"
              % (min(bt), max(bt), bt[-1] - bt[0]))
        turns = [s.get("turnIndex") for s in inb]
        print("turnIndex %s -> %s (distinct=%d)"
              % (turns[0], turns[-1], len(set(turns))))
        st = collections.Counter(s.get("nowActiveStateName") for s in inb)
        print("states:", dict(st))
        af = collections.Counter(bool(s.get("activeAutoFight")) for s in inb)
        print("activeAutoFight:", dict(af))
        fast = sum(1 for s in inb if (s.get("battleTimeScale") or 1) not in (0, 1))
        print("samples_with_battleTimeScale!=1: %d" % fast)

    print()
    print("== pause windows (paused=true) ==")
    wins = [w for w in pause_windows(ok) if (w["samples"][-1]["t"] - w["start_t"]) >= args.min_pause]
    print("windows>=%.2fs: %d" % (args.min_pause, len(wins)))
    rounds = 0
    for i, w in enumerate(wins, 1):
        s0 = w["start"]
        se = w["samples"][-1]
        dur = se["t"] - s0["t"]
        bt0, bt1 = s0.get("battleTime"), se.get("battleTime")
        frozen = bt0 is not None and bt1 is not None and abs(bt1 - bt0) < 1e-4
        # resume check: first sample after the window with paused=false
        after = [s for s in ok if w["end_t"] and w["end_t"] <= s["t"] <= w["end_t"] + 3.0]
        resumed = None
        if after:
            # "game kept going" = clock advanced OR state changed OR turn changed OR the
            # unit was flagged as having moved this turn (all are read-only signals)
            moved = any((s.get("battleTime") or 0) > (bt1 or 0) + 1e-4 for s in after)
            changed_state = any(s.get("nowActiveState") != s0.get("nowActiveState") for s in after)
            changed_turn = any(s.get("turnIndex") != s0.get("turnIndex") for s in after)
            moved_unit = any(s.get("turnMoved") or s.get("moved") for s in after)
            resumed = bool(moved or changed_state or changed_turn or moved_unit)
        if w["end_t"]:
            rounds += 1
        print("W%02d %s -> %s dur=%5.2fs state=%s(%s) turn=%s paused=%s bt=%.4f->%.4f frozen=%s resumed=%s"
              % (i, fmt(w["start_t"]), fmt(w["end_t"]) if w["end_t"] else "OPEN",
                 dur, s0.get("nowActiveState"), s0.get("nowActiveStateName"),
                 s0.get("turnIndex"), s0.get("paused"), bt0 or -1, bt1 or -1,
                 frozen, resumed))
    print("complete_pause_resume_rounds=%d" % rounds)

    print()
    print("== after-resume progression (2 s window per round) ==")
    for i, w in enumerate(wins, 1):
        if not w["end_t"]:
            continue
        end = w["end_t"]
        win = [s for s in ok if end <= s["t"] <= end + 2.0]
        if len(win) < 2:
            print("R%02d: insufficient samples" % i)
            continue
        b0 = win[0].get("battleTime") or 0
        b1 = win[-1].get("battleTime") or 0
        print("R%02d +2s: battleTime %+.4f state %s->%s turn %s->%s"
              % (i, b1 - b0, win[0].get("nowActiveStateName"),
                 win[-1].get("nowActiveStateName"), win[0].get("turnIndex"),
                 win[-1].get("turnIndex")))

    if args.phase_b_start:
        start = args.phase_b_start
        end = args.phase_b_end if args.phase_b_end else ok[-1]["t"]
        win = [s for s in ok if start <= s["t"] <= end]
        print()
        print("== phase B window (%.3f -> %.3f) ==" % (start, end))
        if len(win) < 2:
            print("NOT-RUN: not enough samples inside the window")
        else:
            paused_sec = 0.0
            for a, b in zip(win, win[1:]):
                if a.get("paused") and b.get("paused"):
                    paused_sec += b["t"] - a["t"]
            span = win[-1]["t"] - win[0]["t"]
            print("wall=%.1fs samples=%d paused_seconds=%.1fs active_seconds=%.1fs"
                  % (span, len(win), paused_sec, span - paused_sec))
            gaps = [(a["t"], b["t"] - a["t"]) for a, b in zip(win, win[1:]) if b["t"] - a["t"] > 0.6]
            print("gaps>0.6s=%d max_gap=%.2fs" % (len(gaps), max([g[1] for g in gaps], default=0.0)))
            inb = [s for s in win if s.get("inBattle")]
            print("in_battle_samples=%d/%d" % (len(inb), len(win)))
            if inb:
                print("turnIndex %s -> %s (distinct=%d)"
                      % (inb[0].get("turnIndex"), inb[-1].get("turnIndex"),
                         len(set(s.get("turnIndex") for s in inb))))
                print("battleTime %.4f -> %.4f (delta %+.4f)"
                      % (inb[0].get("battleTime", -1), inb[-1].get("battleTime", -1),
                         (inb[-1].get("battleTime", 0) or 0) - (inb[0].get("battleTime", 0) or 0)))
                st = collections.Counter(s.get("nowActiveStateName") for s in inb)
                print("states:", dict(st))
                print("paused_samples=%d fast_scale_samples=%d"
                      % (sum(1 for s in inb if s.get("paused")),
                         sum(1 for s in inb if (s.get("battleTimeScale") or 1) not in (0, 1))))
                # damage / death progression from the per-unit array
                first = inb[0].get("units") or []
                last = inb[-1].get("units") or []
                hp_first = {u.get("id"): u.get("hp") for u in first}
                drops = 0
                for u in last:
                    hp0 = hp_first.get(u.get("id"))
                    if hp0 is not None and (u.get("hp") is not None) and u.get("hp") < hp0 - 1e-4:
                        drops += 1
                print("units_first=%d units_last=%d units_with_hp_drop=%d"
                      % (len(first), len(last), drops))
                dead = [u.get("id") for u in last if u.get("unitAlive") is False]
                print("units_dead(last)=%s" % (dead,))
            fights = [s for s in win if s.get("battleStateName") == "Fighting"]
            print("fighting_samples=%d non_fighting_samples=%d"
                  % (len(fights), len(win) - len(fights)))
            if fights and len(fights) < len(win):
                lastfight = max(s["t"] for s in fights)
                print("battle ended at %s (last Fighting sample); samples after=%d"
                      % (fmt(lastfight), sum(1 for s in win if s["t"] > lastfight)))

    if args.w4:
        try:
            ev = load_ndjson(args.w4)
        except OSError:
            ev = []
        print()
        print("== jitprobe W4 (read-only hook on BattlePauseButtonClicked) ==")
        print("events=%d" % len(ev))
        states = collections.Counter()
        toggles = []
        for e in ev:
            st = e.get("nowActiveState")
            states[st] = states.get(st, 0) + 1
            toggles.append(e.get("paused"))
        print("states_at_click:", dict(states))
        print("paused_after_click true=%d false=%d"
              % (sum(1 for x in toggles if x is True), sum(1 for x in toggles if x is False)))
        flips = sum(1 for a, b in zip(toggles, toggles[1:]) if a != b)
        print("consecutive_flips=%d/%d" % (flips, max(0, len(toggles) - 1)))
        print("clicks_in_WaitPlayerInput=%d" % states.get(2, 0))

    if args.log:
        try:
            text = open(args.log, "r", encoding="utf-8", errors="replace").read()
        except OSError:
            text = ""
        bad = [ln for ln in text.splitlines()
               if ("ERROR" in ln or "Exception" in ln or "Traceback" in ln)]
        print()
        print("== mod log ==")
        print("log_lines=%d error_like=%d" % (len(text.splitlines()), len(bad)))
        for ln in bad[:10]:
            print("   " + ln[:200])
    return 0


if __name__ == "__main__":
    sys.exit(main())
