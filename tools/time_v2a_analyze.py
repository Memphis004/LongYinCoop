#!/usr/bin/env python3
"""time_v2a_analyze.py — offline analyzer for the V2 set-A sampler NDJSON (time-plan-draft §E.2 set A).

Reads an NDJSON event stream from the (future, not-yet-approved) read-only activity sampler and:
  1. extracts work sessions: work_start -> work_end with per-day day_tick events between,
  2. computes wall-clock T(N) from frame timestamps,
  3. evaluates BOTH candidate T(N) formula forms (still unverified at runtime — time-static-notes
     2026-10-28 note):
        form1  N*(D*k) + N*P        (the form written in time-static-notes §2.4)
        form2  N*D + (N+2)*P        (form including the initial start pause)
     and reports whether the measurement separates them (|T1-T2| margin vs measured tolerance).

Pure offline tool — opens only the file given; no game, no IPC, no writes except stdout/files asked.
"""
import argparse
import json
import sys

FORM1 = lambda n, d, k, p: n * (d * k) + n * p
FORM2 = lambda n, d, k, p: n * d + (n + 2) * p


def load_events(path):
    events = []
    with open(path, "r", encoding="utf-8") as f:
        for i, line in enumerate(f, 1):
            line = line.strip()
            if not line:
                continue
            try:
                events.append(json.loads(line))
            except json.JSONDecodeError as e:
                raise SystemExit(f"bad ndjson at line {i}: {e}")
    return events


def extract_sessions(events):
    """Return list of dicts: n, t_start_ms, t_end_ms, day_ticks, k (from skipping flag at start),
    day_time, pause_time (from the start event)."""
    sessions = []
    cur = None
    for ev in events:
        e = ev.get("ev")
        if e == "work_start" and cur is None:
            cur = {
                "n": ev["leftWorkingDay"],
                "t_start": ev["t"],
                "k": 1.0 if not ev.get("skipping") else float(ev.get("skipK", 0.5)),
                "day_time": ev["dayTime"],
                "pause_time": ev["pauseTime"],
                "day_ticks": 0,
                "day_tick_ts": [],
            }
        elif e == "day_tick" and cur is not None:
            cur["day_ticks"] += 1
            cur["day_tick_ts"].append(ev["t"])
        elif e == "work_end" and cur is not None:
            cur["t_end"] = ev["t"]
            sessions.append(cur)
            cur = None
        elif e in ("work_abort",):
            if cur is not None:
                cur["aborted"] = True
                cur["t_end"] = ev["t"]
                sessions.append(cur)
                cur = None
    if cur is not None:
        raise SystemExit("unterminated work session in stream (no work_end/work_abort)")
    return sessions


def analyze_session(s, tolerance_s=0.5):
    n, d, p, k = s["n"], s["day_time"], s["pause_time"], s["k"]
    t = (s["t_end"] - s["t_start"]) / 1000.0
    f1, f2 = FORM1(n, d, k, p), FORM2(n, d, k, p)
    gap = abs(f1 - f2)
    res = {
        "n": n, "k": k, "day_time": d, "pause_time": p,
        "measured_s": round(t, 3),
        "day_ticks": s["day_ticks"],
        "form1_s": round(f1, 3),
        "form2_s": round(f2, 3),
        "form_gap_s": round(gap, 3),
    }
    if s["day_ticks"] != n:
        res["verdict"] = "INVALID"
        res["reason"] = f"day_ticks ({s['day_ticks']}) != N ({n}) — per plan E1: leaf hits must equal N"
        return res
    if s.get("aborted"):
        res["verdict"] = "EXCLUDED"
        res["reason"] = "aborted session is not a T(N) result (plan §E.1)"
        return res
    if gap <= tolerance_s:
        res["verdict"] = "INDISTINGUISHABLE"
        res["reason"] = (f"form gap {gap:.3f}s <= tolerance {tolerance_s}s: this measurement "
                         f"cannot separate form1/form2 (expected for N=10,D=1,P=0.25)")
    else:
        d1, d2 = abs(t - f1), abs(t - f2)
        if min(d1, d2) > tolerance_s:
            res["verdict"] = "MISMATCH"
            res["reason"] = (f"measured {t:.3f}s differs from BOTH forms beyond tolerance "
                             f"(form1 Δ={d1:.3f}, form2 Δ={d2:.3f}) — STOP and report, do not refit")
        else:
            res["verdict"] = "FORM2" if d2 < d1 else "FORM1"
            res["reason"] = f"measured closer to {res['verdict']} (Δ1={d1:.3f}, Δ2={d2:.3f}, gap={gap:.3f})"
    return res


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("ndjson")
    ap.add_argument("--tolerance", type=float, default=0.5,
                    help="measurement tolerance in seconds (default 0.5 — hook cadence noise of old measurement)")
    args = ap.parse_args(argv)
    sessions = extract_sessions(load_events(args.ndjson))
    if not sessions:
        print("no complete work sessions found")
        return 1
    out = []
    for s in sessions:
        r = analyze_session(s, args.tolerance)
        out.append(r)
        print(json.dumps(r, ensure_ascii=False))
    decided = [r for r in out if r["verdict"] in ("FORM1", "FORM2")]
    print(f"summary: sessions={len(out)} decided={len(decided)} "
          f"indistinguishable={sum(1 for r in out if r['verdict']=='INDISTINGUISHABLE')} "
          f"invalid={sum(1 for r in out if r['verdict']=='INVALID')} "
          f"mismatch={sum(1 for r in out if r['verdict']=='MISMATCH')}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
