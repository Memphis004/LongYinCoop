#!/usr/bin/env python3
"""time_v2a_fit.py — fit รวมของ V2 set-A ตามเกณฑ์ฉบับที่ 2 [CRITERIA] (RUNBOOK-V2A.md ภาคผนวก A,
ปรับตามมติ 2026-10-29 รอบ A: เพิ่ม UNRESOLVED_OFFSET + รายงาน observational รายช่วง + --exclude pilot)

อ่าน NDJSON รายรัน → ตรวจโครงรายรัน → fit เชิงเส้นรวม `measured = slope×N + intercept` (least squares)
→ ตัดสินตามเกณฑ์ที่ล็อก (ตัวเลขตัดสินฟอร์มคงเดิมทุกตัว):
  - ความชัน slope ต้องอยู่ใน 1.25 ± 0.1 s/วัน (ผิด = MISMATCH จริง — หยุดรายงาน)
  - ฟอร์มจาก intercept: จุดอยู่ใน [−0.1, +0.1] = FORM1 · ใน [+0.4, +0.6] = FORM2 · จุดอยู่นอกทั้งคู่
    **และไม่ใช่กรณีคาบเกี่ยว** = UNRESOLVED_OFFSET (รายงานค่า intercept + ช่วงความเชื่อมั่นถ้ามี) ·
    INDISTINGUISHABLE สงวนไว้เฉพาะกรณีคาบเกี่ยว/ข้อมูลไม่พอแยก (ช่วงความเชื่อมั่นครอบ/ทับทั้งสอง window)
    — ทั้ง UNRESOLVED_OFFSET และ INDISTINGUISHABLE เป็นผลที่ยอมรับได้ ไม่ใช่ความล้มเหลว · ห้าม refit
  - residual + segment observational รายรัน (initial / day mean-min-max / final / tail) — ไม่ใช้ตัดสินฟอร์ม
โครงที่ตรวจรายรัน: work_start → pause_end → [day_tick → pause_end]×N → work_end (abort = work_abort)
โดย day_ticks ต้อง == N และ leftWorkingDay ไล่ลงถูกต้อง

การใช้: ส่งเฉพาะไฟล์รอบ **normal** (A1/A2/A3) — รอบ skip (A4) รายงานแยก ห้ามเข้า fit (k ต่าง)
`--exclude <path>` = กันไฟล์ออกจาก fit (เช่น pilot A1-r1 ที่ไม่นับเป็นผลยืนยัน — มติ 2026-10-29)

exit code: 0 = fit สำเร็จ (slope อยู่ในเกณฑ์ — ฟอร์มตัดสินแยก รวม INDISTINGUISHABLE/UNRESOLVED_OFFSET) ·
3 = slope MISMATCH (หยุดรายงาน) · 1 = ไม่มี run ที่ใช้ได้ / จุดไม่พอ fit · 2 = args/ไฟล์ผิด
เครื่องมือนี้อ่านอย่างเดียว — ไม่แตะเกม/เซฟ/pref และห้ามใช้แทน time_v2a_analyze.py (ไม่แก้ไฟล์นั้น)
"""
import argparse
import json
import math
import sys

SLOPE_TARGET = 1.25
SLOPE_TOL = 0.1
I1, I2, ITOL = 0.0, 0.5, 0.1
# กฎคุณภาพข้อมูล (เพิ่มหลังเจอ A3-r2 — ใช้ย้อนกับทุกรัน: ผลของรัน valid เดิมไม่เปลี่ยน):
# ช่องว่างระหว่าง event ติดกัน > 2.0 s ภายใน work_start→work_end = CONTAMINATED (กันออกจาก fit อัตโนมัติ)
GAP_CONTAMINATION_MS = 2000.0

# t-critical สองด้านที่ 95% ตามองศาอิสระของ fit (n_points − 2) — ดีฟอลต์ >10 จุด = 1.96
T_CRIT_95 = {1: 12.706, 2: 4.303, 3: 3.182, 4: 2.776, 5: 2.571,
             6: 2.447, 7: 2.365, 8: 2.306, 9: 2.262, 10: 2.228}


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


def segments_of(s):
    """segment observational รายรัน (OBSERVED จากไฟล์ — ไม่มีการตีความ):
    initial = work_start→pause_end#1 · day_i = pause_end#i→day_tick#i (แต่ละวัน) ·
    final = day_tick สุดท้าย→pause_end สุดท้าย · tail = pause_end สุดท้าย→work_end"""
    evs = s["events"]
    pauses = [e for e in evs if e.get("ev") == "pause_end"]
    days = [e for e in evs if e.get("ev") == "day_tick"]
    t_ws = evs[0]["t"]
    initial = (pauses[0]["t"] - t_ws) / 1000.0
    day_list = [(days[i]["t"] - pauses[i]["t"]) / 1000.0 for i in range(len(days))]
    final = (pauses[-1]["t"] - days[-1]["t"]) / 1000.0
    tail = (s["t_end"] - pauses[-1]["t"]) / 1000.0
    return {
        "initial_ms": round(initial * 1000, 1),
        "day_mean_ms": round(sum(day_list) / len(day_list) * 1000, 1),
        "day_min_ms": round(min(day_list) * 1000, 1),
        "day_max_ms": round(max(day_list) * 1000, 1),
        "final_ms": round(final * 1000, 1),
        "tail_ms": round(tail * 1000, 1),
    }


def fit(points):
    """least squares ผ่าน (n, measured) — คืน (slope, intercept, se_intercept หรือ None, df)
    df = จุด − 2 (องศาอิสระของ residual) · se = sqrt(MSE×(1/n + mean_n²/Sxx)) เมื่อ MSE > 0"""
    ns = [p[0] for p in points]
    ys = [p[1] for p in points]
    mean_n = sum(ns) / len(ns)
    mean_y = sum(ys) / len(ys)
    sxx = sum((n - mean_n) ** 2 for n in ns)
    cov = sum((n - mean_n) * (y - mean_y) for n, y in points)
    slope = cov / sxx
    intercept = mean_y - slope * mean_n
    df = len(points) - 2
    se = None
    if len(points) >= 3 and sxx > 0:
        resid = [y - (slope * n + intercept) for n, y in points]
        ssr = sum(r * r for r in resid)
        mse = ssr / df
        if mse > 0:
            se = math.sqrt(mse * (1.0 / len(points) + mean_n * mean_n / sxx))
    return round(slope, 6), round(intercept, 6), (None if se is None else round(se, 6)), df


def overlaps(intercept, ci_half, target, itol):
    """ช่วง [intercept±ci_half] คาบเกี่ยว window [target±itol] หรือไม่ (จุดอยู่ใน window = คาบเกี่ยว)"""
    if target - itol <= intercept <= target + itol:
        return True
    if ci_half is None:
        return False
    lo, hi = intercept - ci_half, intercept + ci_half
    return lo <= target + itol and hi >= target - itol


def form_verdict(intercept, ci_half):
    """ตัดสินฟอร์มตาม [CRITERIA]: จุดใน window = ฟอร์มนั้น · จุดนอกทั้งคู่:
    ช่วงความเชื่อมั่นครอบ/ทับทั้งสอง window = INDISTINGUISHABLE (คาบเกี่ยว/ข้อมูลไม่พอแยก) ·
    ไม่งั้น = UNRESOLVED_OFFSET — ทั้งสองป้ายหลังเป็นผลที่ยอมรับได้"""
    if I1 - ITOL <= intercept <= I1 + ITOL:
        return "FORM1", None
    if I2 - ITOL <= intercept <= I2 + ITOL:
        return "FORM2", None
    if ci_half is not None:
        lo, hi = intercept - ci_half, intercept + ci_half
        f1_ov = lo <= I1 + ITOL and hi >= I1 - ITOL
        f2_ov = lo <= I2 + ITOL and hi >= I2 - ITOL
        if f1_ov and f2_ov:
            return "INDISTINGUISHABLE", [round(lo, 4), round(hi, 4)]
    return "UNRESOLVED_OFFSET", ([round(intercept - ci_half, 4), round(intercept + ci_half, 4)]
                                 if ci_half is not None else None)


def max_gap_of(s):
    """ช่องว่างระหว่าง event ติดกันที่ยาวสุดภายใน session — คืน (ms, คำอธิบายตำแหน่ง)"""
    evs = s["events"]
    best, desc = 0.0, ""
    for i in range(len(evs) - 1):
        g = evs[i + 1]["t"] - evs[i]["t"]
        if g > best:
            best = g
            desc = (f"after {evs[i]['ev']}(left={evs[i].get('leftWorkingDay')}) "
                    f"-> {evs[i + 1]['ev']}(left={evs[i + 1].get('leftWorkingDay')})")
    return best, desc


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("ndjson", nargs="+", help="NDJSON รายรัน (รอบ normal เท่านั้น — ดูภาคผนวก A)")
    ap.add_argument("--exclude", action="append", default=[],
                    help="ไฟล์ที่ต้องกันออกจาก fit (เช่น pilot A1-r1) — รายงานเป็น EXCLUDED_PILOT")
    args = ap.parse_args(argv)

    exclude = set(args.exclude)
    rows, points = [], []
    for path in args.ndjson:
        if path in exclude:
            rows.append({"file": path, "status": "EXCLUDED_PILOT",
                         "reason": "excluded from fit by --exclude (pilot — ไม่นับเป็นผลยืนยัน)"})
            continue
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
            max_gap, gap_after = max_gap_of(s)
            if max_gap > GAP_CONTAMINATION_MS:
                ok, sreason, day_ticks = validate_structure(s)
                rows.append({"file": path, "n": s["n"], "measured_s": round(measured, 3),
                             "day_ticks": day_ticks, "status": "CONTAMINATED",
                             "max_gap_ms": round(max_gap, 1), "max_gap_after": gap_after,
                             "structure": sreason if not ok else "ok",
                             "reason": (f"inter-event gap {max_gap:.0f} ms > {GAP_CONTAMINATION_MS:.0f} ms "
                                        f"ภายใน work_start→work_end — กันออกจาก fit อัตโนมัติ (กฎคุณภาพข้อมูล)")})
                continue
            ok, reason, day_ticks = validate_structure(s)
            if not ok:
                rows.append({"file": path, "n": s["n"], "measured_s": round(measured, 3),
                             "day_ticks": day_ticks, "status": "INVALID", "reason": reason})
                continue
            row = {"file": path, "n": s["n"], "measured_s": round(measured, 3),
                   "day_ticks": day_ticks, "status": "OK"}
            row["segments_observed"] = segments_of(s)
            rows.append(row)
            points.append((s["n"], measured))

    for r in rows:
        print(json.dumps(r, ensure_ascii=False))

    valid = [r for r in rows if r["status"] == "OK"]
    if len(points) < 2 or len({p[0] for p in points}) < 2:
        print(f"summary: valid_runs={len(valid)} points={len(points)} "
              f"fit=NOT_POSSIBLE (need >=2 runs with distinct N) "
              f"invalid={sum(1 for r in rows if r['status'] == 'INVALID')} "
              f"excluded={sum(1 for r in rows if r['status'] != 'OK')}")
        return 1

    slope, intercept, se, df = fit(points)
    slope_ok = abs(slope - SLOPE_TARGET) <= SLOPE_TOL
    # CI สองวิธีรายงานคู่ขนาน (มติ 2026-10-29 รอบ A3-r2 เตรียม): แบบ t ตาม df จริง (valid) และแบบ 1.96
    # (ไม่ valid สำหรับ df ต่ำ — เก็บไว้เทียบกับรายงานรอบก่อน) · df=0 (2 จุด) = ไม่รายงาน CI
    if se is not None:
        tcrit = T_CRIT_95.get(df, 1.96)
        ci_half_t = round(tcrit * se, 6)
        ci_half_196 = round(1.96 * se, 6)
        fv_t, range_t = form_verdict(intercept, ci_half_t)
        fv_196, range_196 = form_verdict(intercept, ci_half_196)
        ov_t = {"FORM1": overlaps(intercept, ci_half_t, I1, ITOL),
                "FORM2": overlaps(intercept, ci_half_t, I2, ITOL)}
        ov_196 = {"FORM1": overlaps(intercept, ci_half_196, I1, ITOL),
                  "FORM2": overlaps(intercept, ci_half_196, I2, ITOL)}
        ci_block = {
            "df": df, "tcrit_95": tcrit,
            "intercept_ci_half_t": ci_half_t, "intercept_ci_t": range_t,
            "overlaps_t": ov_t, "form": fv_t, "form_ci": "t (valid ตาม df)",
            "intercept_ci_half_196": ci_half_196, "intercept_ci_196": range_196,
            "overlaps_196": ov_196, "form_under_ci196": fv_196,
            "ci196_note": "1.96 invalid for low df — kept for comparison with earlier report",
        }
    else:
        fv_t, range_t = form_verdict(intercept, None)
        ci_block = {
            "df": df, "tcrit_95": None,
            "intercept_ci_half_t": None, "intercept_ci_t": None,
            "overlaps_t": None, "form": fv_t, "form_ci": "no CI (residual 0 หรือจุดไม่พอ)",
            "intercept_ci_half_196": None, "intercept_ci_196": None,
            "overlaps_196": None, "form_under_ci196": fv_t,
            "ci196_note": "no CI — residual 0 หรือจุดไม่พอ (df=0 สำหรับ 2 จุด = ไม่รายงาน CI)",
        }
    fv, ci_range = fv_t, range_t
    print(json.dumps({
        "points": len(points), "slope": slope, "intercept": intercept,
        "slope_ok": slope_ok, "form": fv,
        "criteria": "slope in 1.25±0.1; point in [-0.1,+0.1]=FORM1, [+0.4,+0.6]=FORM2; "
                    "outside both + CI overlaps both windows = INDISTINGUISHABLE; "
                    "outside both otherwise = UNRESOLVED_OFFSET (acceptable, no refit)",
        **ci_block,
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
          f"excluded={sum(1 for r in rows if r['status'] != 'OK')}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
