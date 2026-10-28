#!/usr/bin/env python3
"""time_v2a_fit.py — fit รวมของ V2 set-A ตามเกณฑ์ฉบับที่ 2 [CRITERIA] (RUNBOOK-V2A.md ภาคผนวก A)

อ่าน NDJSON รายรัน (ไฟล์ละ session) → ตรวจโครงรายรัน → fit เชิงเส้นรวม `measured = slope×N + intercept`
(least squares) → ตัดสินตามเกณฑ์ที่ล็อก:
  - ความชัน slope ต้องอยู่ใน 1.25 ± 0.1 s/วัน (ผิด = MISMATCH จริง — หยุดรายงาน)
  - ฟอร์มจาก intercept: [−0.1, +0.1] = FORM1 · [+0.4, +0.6] = FORM2 · นอกนั้น/คาบเกี่ยว =
    INDISTINGUISHABLE (ผลที่ยอมรับได้)
  - residual รายรัน = measured − (slope×N + intercept) รายงานแยก ไม่ใช้ตัดสินฟอร์ม
โครงที่ตรวจรายรัน: work_start → pause_end → [day_tick → pause_end]×N → work_end (abort = work_abort)
โดย day_ticks ต้อง == N และ leftWorkingDay ไล่ลงถูกต้อง

การใช้: ส่งเฉพาะไฟล์รอบ **normal** (A1/A2/A3) — รอบ skip (A4) รายงานแยก ห้ามเข้า fit (k ต่าง)
pilot A1-r1 ไม่นับเป็นผลยืนยัน (มติ 2026-10-29) จึงไม่ส่งเข้า fit

exit code: 0 = fit สำเร็จ (slope อยู่ในเกณฑ์ — ฟอร์มตัดสินแยก) · 3 = slope MISMATCH (หยุดรายงาน) ·
1 = ไม่มี run ที่ใช้ได้ / จุดไม่พอ fit (ต้องมี ≥2 run ที่ N ต่างกัน) · 2 = args/ไฟล์ผิด
เครื่องมือนี้อ่านอย่างเดียว — ไม่แตะเกม/เซฟ/pref และห้ามใช้แทน time_v2a_analyze.py (ไม่แก้ไฟล์นั้น)
"""
import argparse
import json
import sys

SLOPE_TARGET = 1.25
SLOPE_TOL = 0.1
I1, I2, ITOL = 0.0, 0.5, 0.1


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
                raise SystemExit(f"bad ndjson at {path}:{i}: {e}")
    return events


def extract_sessions(events):
    """ตัด session ตาม event หลัก (work_start/work_end/work_abort) — ตรวจโครงทีหลัง"""
    sessions, cur = [], None
    for ev in events:
        e = ev.get("ev")
        if e == "work_start" and cur is None:
            cur = {"n": ev["leftWorkingDay"], "t_start": ev["t"], "events": [ev]}
        elif cur is not None:
            cur["events"].append(ev)
            if e in ("work_end", "work_abort"):
                cur["t_end"] = ev["t"]
                cur["aborted"] = (e == "work_abort")
                sessions.append(cur)
                cur = None
    if cur is not None:
        raise SystemExit("unterminated work session in stream (no work_end/work_abort)")
    return sessions


def validate_structure(s):
    """โครงบังคับ: work_start → pause_end → [day_tick → pause_end]×N → work_end|work_abort
    คืน (ok, reason, day_ticks)"""
    evs = s["events"]
    n = s["n"]
    expect = ["pause_end"] + ["day_tick", "pause_end"] * n
    mids = [e.get("ev") for e in evs[1:-1]]
    if mids != expect:
        return False, f"event order mismatch (expected {expect}, got {mids})", -1
    day_ticks = sum(1 for e in evs if e.get("ev") == "day_tick")
    if day_ticks != n:
        return False, f"day_ticks {day_ticks} != N {n}", day_ticks
    left = [e["leftWorkingDay"] for e in evs if e.get("ev") == "day_tick"]
    if left != list(range(n - 1, -1, -1)):
        return False, f"leftWorkingDay sequence wrong: {left}", day_ticks
    return True, "ok", day_ticks


def fit(points):
    """least squares ผ่าน (n, measured) — คืน (slope, intercept)"""
    ns = [p[0] for p in points]
    ys = [p[1] for p in points]
    mean_n = sum(ns) / len(ns)
    mean_y = sum(ys) / len(ys)
    cov = sum((n - mean_n) * (y - mean_y) for n, y in points)
    var = sum((n - mean_n) ** 2 for n in ns)
    slope = cov / var
    intercept = mean_y - slope * mean_n
    # round 6 ตำแหน่งก่อนตัดสินขอบเขต — กัน float drift ที่เส้นแบ่ง 0.1/0.4/0.6 (ตามเกณฑ์ที่ล็อก)
    return round(slope, 6), round(intercept, 6)


def form_verdict(intercept):
    if I1 - ITOL <= intercept <= I1 + ITOL:
        return "FORM1"
    if I2 - ITOL <= intercept <= I2 + ITOL:
        return "FORM2"
    return "INDISTINGUISHABLE"


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("ndjson", nargs="+", help="NDJSON รายรัน (รอบ normal เท่านั้น — ดูภาคผนวก A)")
    args = ap.parse_args(argv)

    rows, points = [], []
    for path in args.ndjson:
        try:
            sessions = extract_sessions(load_events(path))
        except OSError as e:
            print(f"ERROR cannot read {path}: {e}")
            return 2
        for s in sessions:
            measured = (s["t_end"] - s["t_start"]) / 1000.0
            if s["aborted"]:
                rows.append({"file": path, "n": s["n"], "measured_s": round(measured, 3),
                             "status": "EXCLUDED", "reason": "aborted session is not a T(N) result"})
                continue
            ok, reason, day_ticks = validate_structure(s)
            if not ok:
                rows.append({"file": path, "n": s["n"], "measured_s": round(measured, 3),
                             "day_ticks": day_ticks, "status": "INVALID", "reason": reason})
                continue
            rows.append({"file": path, "n": s["n"], "measured_s": round(measured, 3),
                         "day_ticks": day_ticks, "status": "OK"})
            points.append((s["n"], measured))

    for r in rows:
        print(json.dumps(r, ensure_ascii=False))

    valid = [r for r in rows if r["status"] == "OK"]
    if len(points) < 2 or len({p[0] for p in points}) < 2:
        print(f"summary: valid_runs={len(valid)} points={len(points)} "
              f"fit=NOT_POSSIBLE (need >=2 runs with distinct N) "
              f"invalid={sum(1 for r in rows if r['status'] == 'INVALID')} "
              f"excluded={sum(1 for r in rows if r['status'] == 'EXCLUDED')}")
        return 1

    slope, intercept = fit(points)
    slope_ok = abs(slope - SLOPE_TARGET) <= SLOPE_TOL
    fv = form_verdict(intercept)
    print(json.dumps({
        "points": len(points), "slope": round(slope, 4), "intercept": round(intercept, 4),
        "slope_ok": slope_ok, "form": fv,
        "criteria": f"slope in {SLOPE_TARGET}±{SLOPE_TOL}; intercept [{I1 - ITOL},{I1 + ITOL}]=FORM1, "
                    f"[{I2 - ITOL},{I2 + ITOL}]=FORM2, else INDISTINGUISHABLE",
    }, ensure_ascii=False))
    for r in rows:
        if r["status"] == "OK":
            resid = r["measured_s"] - (slope * r["n"] + intercept)
            print(json.dumps({"file": r["file"], "n": r["n"], "residual_s": round(resid, 3),
                              "note": "unexplained per-run residual — not used for form verdict"},
                             ensure_ascii=False))
    if not slope_ok:
        print(f"summary: SLOPE MISMATCH ({slope:.4f} outside {SLOPE_TARGET}±{SLOPE_TOL}) — STOP and report, "
              f"do not refit")
        return 3
    print(f"summary: slope OK ({slope:.4f} in {SLOPE_TARGET}±{SLOPE_TOL}) · form={fv} · "
          f"valid_runs={len(valid)} invalid={sum(1 for r in rows if r['status'] == 'INVALID')} "
          f"excluded={sum(1 for r in rows if r['status'] == 'EXCLUDED')}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
