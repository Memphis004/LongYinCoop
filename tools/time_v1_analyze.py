#!/usr/bin/env python3
"""time_v1_analyze — ตัววิเคราะห์ V1 (มติ [Gate 3] 2026-10-31 · time-plan-draft §D.4)

**อ่านอย่างเดียว 100%** · ตัดสินแบบ **observational** ต่อ R1/R2/R3 — ห้ามประกาศ "ผลผ่าน"
เกินที่ข้อมูลรองรับ · คำที่ใช้ = OBSERVED / NOT-OBSERVED / INCOMPLETE / CONTAMINATED-CANDIDATE
(การทิ้งรัน/ตัดสินจบ = ตามนโยบาย POPUP และ §G.1 ซึ่งเป็นของผู้รายงาน/ผู้ใช้ ไม่ใช่ของเครื่องมือนี้)

แหล่งข้อมูล:
  --probe <ndjson>   NDJSON ของ probe (TimeV1 — stopworking/middlestopwork events) รายรัน (ใช้ซ้ำได้หลายไฟล์)
  --v2a <ndjson>     NDJSON ของ TimeV2a (timeline ต่อเฟรม — ใช้กับ R3 + กฎ gap) (ใช้ซ้ำได้หลายไฟล์)
  --screen-values <json>  แบบฟอร์มที่ผู้ใช้จดค่าบนจอ (เงิน/ทรัพยากร/dialog ฯลฯ) — **echo ตามจริง
                     ไม่ใช้ตัดสิน ไม่เดาแทน** (ข้อมูลบนจอเป็นของผู้ใช้เท่านั้น)
  --gap-ms <N>       เกณฑ์ gap ตรวจ popup ค้าง (default 2000 — แนวเดียวกับ GAP_CONTAMINATION_MS ของ V2-A)
  --json <out>       เขียนผลสรุปเป็น JSON ลงไฟล์ด้วย (optional)

การจำแนกเส้นทางของ stop call (จาก static §B.3 — ตัวจำแนกใช้ลำดับ event ล้วน):
  route-a (UI-cancel ผ่าน plot-choice) = middlestopwork.enter นำหน้า stopworking.enter ภายใน 3 event
        และไม่มี stopworking.exit คั่น (MiddleStopWork tail-call StopWorking(false))
  route-b (เรียก StopWorking ตรง)      = ไม่มี middlestopwork.enter นำหน้าตามเงื่อนไขข้างบน
        — **หมายเหตุ**: กลุ่ม per-day handler (HospitalCureInjury/RestInHotel ฯลฯ) เข้า route-b เช่นกัน
        การระบุ "TC ใด" ต้องใช้บันทึกการกระทำของผู้ใช้ประกอบ (RUNBOOK แบบฟอร์ม) — analyzer ไม่เดา
  finished=true                        = จบงานปกติ (กิ่งจบงานของ Update หรือ StopWorking(true))

R1 = มี stop call finished=false ที่ route-a หรือไม่ (observed/not-observed รายไฟล์)
R2 = รายงาน finishFuc (handler) + fireWorkFinished + totalWorkingDay ของทุก stop call + echo dialog จากผู้ใช้
     (gate totalWorkingDay>0 ของ FinishWorkInProductionBuilding — รายงานค่าให้ดู ไม่ตัดสินว่า dialog โผล่)
R3 = ต้องมี --v2a: หา work session ที่ 2 ขึ้นไป (มี work_end ก่อนหน้า) — day_tick/pause_end ระหว่าง session
     นั้นพก workFinished (latch) — รายงานราย session: latch=true กี่ event / =false กี่ event
     (**ถ้าเจอ =false ใน session ที่ 2+ = CANDIDATE ขัด static B.2 → ตาม §G.1 ข้อ 4 ต้องหยุดรายงาน —
     เครื่องมือพิมพ์ป้าย STATIC-CONTRADICTION-CANDIDATE ให้เห็น ไม่ตัดสินเอง**)

Exit code: 0 = ผลิตผลวิเคราะห์แล้ว (observational ทุกกรณี) · 1 = ไม่มี event ใช้ได้ ·
           2 = args/ไฟล์ผิดพลาด (fail-closed — ไม่เดา)
"""
import argparse
import json
import sys
from pathlib import Path

DEFAULT_GAP_MS = 2000
ROUTE_LOOKBACK = 3   # middlestopwork.enter อยู่ห่าง stopworking.enter ไม่เกิน N event (tail-call)


def load_ndjson(path):
    """อ่าน NDJSON — บรรทัดเสียเก็บเป็น count (ไม่ throw, ไม่เดา) — คืน (events, malformed)"""
    events, malformed = [], 0
    try:
        with open(path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line:
                    continue
                try:
                    obj = json.loads(line)
                except ValueError:
                    malformed += 1
                    continue
                if isinstance(obj, dict):
                    events.append(obj)
                else:
                    malformed += 1
    except OSError as e:
        raise ValueError("อ่าน %s ไม่ได้: %s" % (path, e))
    return events, malformed


def is_v2a_work_open(ev):
    return ev.get("ev") == "work_start"


def is_v2a_work_close(ev):
    return ev.get("ev") in ("work_end", "work_abort")


def analyze_probe(events):
    """จำแนก stop calls + route — observational ล้วน"""
    out = {
        "stop_calls": [],
        "middlestopwork": 0,
        "session_markers": {"begin": 0, "end": 0},
        "last_err_n": None,
        "finalizers_with_ex": [],
    }
    for i, ev in enumerate(events):
        name = ev.get("ev")
        if name == "session.begin":
            out["session_markers"]["begin"] += 1
        elif name == "session.end":
            out["session_markers"]["end"] += 1
        if name == "middlestopwork.enter":
            out["middlestopwork"] += 1
        if name in ("stopworking.finalizer", "middlestopwork.finalizer"):
            ex = ev.get("ex")
            if ex:
                out["finalizers_with_ex"].append({"index": i, "ev": name, "ex": ex})
        if name != "stopworking.enter":
            continue
        # หา route: มองย้อน ≤ ROUTE_LOOKBACK event — ห้ามมี stopworking.exit คั่น
        route = "route-b"
        for back in range(1, ROUTE_LOOKBACK + 1):
            j = i - back
            if j < 0:
                break
            prev = events[j].get("ev")
            if prev == "stopworking.exit":
                break
            if prev == "middlestopwork.enter":
                route = "route-a"
                break
        # คู่ exit ถัดไป (ก่อน enter ถัดไป)
        post = None
        for k in range(i + 1, len(events)):
            nxt = events[k].get("ev")
            if nxt == "stopworking.enter":
                break
            if nxt == "stopworking.exit":
                post = events[k]
                break
        out["stop_calls"].append({
            "index": i,
            "t": ev.get("t"),
            "frame": ev.get("frame"),
            "route": route,
            "finished": ev.get("finished"),
            "fire_work_finished": ev.get("fireWorkFinished"),
            "finish_fuc": ev.get("finishFuc"),
            "work_name": ev.get("workName"),
            "has_state": ev.get("hasState"),
            "working_at_enter": ev.get("working"),
            "work_finished_pre": ev.get("workFinished"),
            "left_working_day": ev.get("leftWorkingDay"),
            "total_working_day": ev.get("totalWorkingDay"),
            "no_cancel": ev.get("noCancel"),
            "exit_seen": post is not None,
            "exit_working": (post or {}).get("working"),
            "exit_work_finished": (post or {}).get("workFinished"),
        })
    if events:
        last = events[-1]
        if isinstance(last.get("errN"), int):
            out["last_err_n"] = last["errN"]
    return out


def analyze_v2a(events, gap_ms):
    """R3 latch + gap/CONTAMINATED-CANDIDATE — observational ล้วน"""
    out = {
        "sessions": [],
        "open_without_close": 0,
        "max_gap_ms": 0,
        "gap_hits": [],
    }
    session = None

    def close_session(sess, end_ev, idx):
        if sess["events"]:
            sess["end"] = {"ev": end_ev.get("ev"), "t": end_ev.get("t"), "index": idx}
            gaps = []
            ts = sess["times"]
            for a, b in zip(ts, ts[1:]):
                if isinstance(a, int) and isinstance(b, int):
                    gaps.append(b - a)
            sess["max_gap_ms"] = max(gaps) if gaps else 0
            if sess["max_gap_ms"] > gap_ms:
                sess["contaminated_candidate"] = True
                out["gap_hits"].append({"index": sess["start_index"], "max_gap_ms": sess["max_gap_ms"]})
            latch_true = sum(1 for e in sess["events"] if e.get("ev") in ("day_tick", "pause_end")
                             and e.get("workFinished") is True)
            latch_false = sum(1 for e in sess["events"] if e.get("ev") in ("day_tick", "pause_end")
                              and e.get("workFinished") is False)
            sess["latch_true_events"] = latch_true
            sess["latch_false_events"] = latch_false
            if sess["ordinal"] >= 2 and latch_false > 0:
                sess["static_contradiction_candidate"] = True
        else:
            out["open_without_close"] += 1
        out["sessions"].append(sess)

    for idx, ev in enumerate(events):
        if is_v2a_work_open(ev):
            if session is not None:
                close_session(session, {"ev": "no-close-seen", "t": ev.get("t")}, idx)
            session = {
                "ordinal": len(out["sessions"]) + 1,
                "start_index": idx,
                "start": {"t": ev.get("t"), "leftWorkingDay": ev.get("leftWorkingDay"),
                          "noCancel": ev.get("noCancel")},
                "events": [],
                "times": [],
                "max_gap_ms": 0,
            }
            continue
        if session is not None:
            session["events"].append(ev)
            session["times"].append(ev.get("t") if isinstance(ev.get("t"), int) else None)
            if is_v2a_work_close(ev):
                close_session(session, ev, idx)
                session = None
    if session is not None:
        close_session(session, {"ev": "no-close-seen (run จบก่อนงานปิด)"}, len(events) - 1)
        out["open_without_close"] += 0  # นับที่ close_session แล้ว
    # work_end/work_abort นอก session (ปั๊มจบโดยไม่เห็น work_start เพราะเริ่มก่อนเปิดเครื่องมือ) — ไม่จัด session
    return out


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--probe", action="append", default=[], metavar="NDJSON",
                    help="probe NDJSON รายรัน (ใช้ซ้ำได้)")
    ap.add_argument("--v2a", action="append", default=[], metavar="NDJSON",
                    help="TimeV2a NDJSON รายรัน (ใช้ซ้ำได้ — ใช้กับ R3/กฎ gap)")
    ap.add_argument("--screen-values", metavar="JSON",
                    help="แบบฟอร์มค่าบนจอของผู้ใช้ — echo ตามจริง ไม่ใช้ตัดสิน")
    ap.add_argument("--gap-ms", type=int, default=DEFAULT_GAP_MS,
                    help="เกณฑ์ gap (default %d ms)" % DEFAULT_GAP_MS)
    ap.add_argument("--json", metavar="OUT", help="เขียนสรุป JSON ลงไฟล์")
    ap.add_argument("--label", default="", help="label ของ session (ประกอบรายงาน)")
    args = ap.parse_args(argv)

    if not args.probe and not args.v2a:
        print("ARGS-ERROR: ต้องระบุ --probe หรือ --v2a อย่างน้อย 1 ไฟล์")
        return 2

    screen = None
    if args.screen_values:
        try:
            with open(args.screen_values, "r", encoding="utf-8") as f:
                screen = json.load(f)
        except (OSError, ValueError) as e:
            print("ARGS-ERROR: อ่าน --screen-values %s ไม่ได้: %s" % (args.screen_values, e))
            return 2

    report = {"label": args.label, "gap_ms": args.gap_ms, "probe_files": [], "v2a_files": [],
              "screen_values_echo": screen}

    any_usable = False
    for path in args.probe:
        try:
            events, malformed = load_ndjson(path)
        except ValueError as e:
            print("ARGS-ERROR: %s" % e, file=sys.stderr)
            return 2   # fail-closed — ไฟล์อ่านไม่ได้ = ไม่วิเคราะห์ (docstring: exit 2)
        res = analyze_probe(events)
        res["file"] = path
        res["events"] = len(events)
        res["malformed"] = malformed
        # R1 observational
        route_a_false = [c for c in res["stop_calls"]
                         if c["route"] == "route-a" and c["finished"] is False]
        route_b_false = [c for c in res["stop_calls"]
                         if c["route"] == "route-b" and c["finished"] is False]
        res["r1"] = {
            "cancel_via_plot_choice_observed": len(route_a_false) > 0,
            "route_a_cancel_count": len(route_a_false),
            "route_b_cancel_count": len(route_b_false),
            "finish_true_count": sum(1 for c in res["stop_calls"] if c["finished"] is True),
            "note": "route-a = ผ่าน middlestopwork (plot-choice); route-b = เรียกตรง "
                    "(รวม per-day handler — ระบุ TC ด้วยบันทึกผู้ใช้เสมอ, errata E4/E10)",
        }
        # R2 observational — รายงานค่า ไม่ตัดสิน dialog
        res["r2"] = {
            "stop_calls_with_handler": [
                {k: c[k] for k in ("index", "finish_fuc", "fire_work_finished", "finished",
                                   "total_working_day", "work_name")}
                for c in res["stop_calls"] if c["finish_fuc"]
            ],
            "note": "gate totalWorkingDay>0 ของ FinishWorkInProductionBuilding (§B.4) — "
                    "analyzer รายงานค่า; dialog จริงบนจอ = บันทึกของผู้ใช้เท่านั้น (hypothesis §D.4)",
        }
        report["probe_files"].append(res)
        if events:
            any_usable = True

    for path in args.v2a:
        try:
            events, malformed = load_ndjson(path)
        except ValueError as e:
            print("ARGS-ERROR: %s" % e, file=sys.stderr)
            return 2   # fail-closed — ไฟล์อ่านไม่ได้ = ไม่วิเคราะห์ (docstring: exit 2)
        res = analyze_v2a(events, args.gap_ms)
        res["file"] = path
        res["events"] = len(events)
        res["malformed"] = malformed
        report["v2a_files"].append(res)
        if events:
            any_usable = True

    # พิมพ์รายงานข้อความ
    print("=" * 72)
    print("time_v1_analyze — OBSERVATIONAL ONLY (ห้ามประกาศผ่านเกินข้อมูล)")
    if args.label:
        print("label: %s" % args.label)
    print("gap_ms: %d" % args.gap_ms)
    for res in report["probe_files"]:
        print("-" * 72)
        print("PROBE %s — events=%d malformed=%d session.begin=%d session.end=%d"
              % (res["file"], res["events"], res["malformed"],
                 res["session_markers"]["begin"], res["session_markers"]["end"]))
        print("  counters: last_errN=%s middlestopwork.enter=%d finalizer_with_ex=%d"
              % (res["last_err_n"], res["middlestopwork"], len(res["finalizers_with_ex"])))
        for f in res["finalizers_with_ex"]:
            print("    finalizer ex @%d %s: %s" % (f["index"], f["ev"], f["ex"]))
        print("  R1 (cancel path): route-a(observed)=%s (count=%d) · route-b(direct) count=%d · finish=true count=%d"
              % (res["r1"]["cancel_via_plot_choice_observed"], res["r1"]["route_a_cancel_count"],
                 res["r1"]["route_b_cancel_count"], res["r1"]["finish_true_count"]))
        for c in res["stop_calls"]:
            print("    stop@%d route=%s finished=%s fireWorkFinished=%s handler=%s workName=%s"
                  % (c["index"], c["route"], c["finished"], c["fire_work_finished"],
                     c["finish_fuc"], c["work_name"]))
            print("             pre: working=%s workFinished=%s leftDay=%s totalDay=%s noCancel=%s"
                  % (c["working_at_enter"], c["work_finished_pre"], c["left_working_day"],
                     c["total_working_day"], c["no_cancel"]))
            if c["exit_seen"]:
                print("             post: working=%s workFinished=%s (exit พบ)"
                      % (c["exit_working"], c["exit_work_finished"]))
            else:
                print("             post: stopworking.exit ไม่พบ = INCOMPLETE (exit ถูกกลืน/ยังไม่จบ?)")
    for res in report["v2a_files"]:
        print("-" * 72)
        print("V2A %s — events=%d malformed=%d sessions=%d open_without_close=%d"
              % (res["file"], res["events"], res["malformed"], len(res["sessions"]),
                 res["open_without_close"]))
        print("  R3 (workFinished latch ราย session):")
        for s in res["sessions"]:
            print("    session#%d start t=%s leftDay=%s → end=%s events=%d maxGap=%d ms"
                  % (s["ordinal"], s["start"].get("t"), s["start"].get("leftWorkingDay"),
                     s["end"].get("ev") if s.get("end") else None, len(s["events"]),
                     s["max_gap_ms"]))
            print("      latch(day_tick/pause_end ที่ workFinished): true=%d false=%d%s%s"
                  % (s["latch_true_events"], s["latch_false_events"],
                     "  ** CONTAMINATED-CANDIDATE (gap>%d ms — ตามนโยบาย POPUP)**" % args.gap_ms
                     if s.get("contaminated_candidate") else "",
                     "  ** STATIC-CONTRADICTION-CANDIDATE (latch=false ใน session ที่ 2+ — §G.1 ข้อ 4: หยุดรายงาน)**"
                     if s.get("static_contradiction_candidate") else ""))
        if not res["sessions"]:
            print("    (ไม่มี work session — R3 NOT-OBSERVED จากไฟล์นี้)")
    if report["screen_values_echo"] is not None:
        print("-" * 72)
        print("SCREEN-VALUES (USER-RECORDED — echo ตามจริง ไม่ใช้ตัดสิน):")
        print(json.dumps(report["screen_values_echo"], ensure_ascii=False, indent=2, sort_keys=True))
    print("=" * 72)
    print("LIMITS: observational เท่านั้น · TC-3a/3b NOT-RUN (มติ D1 — ปุ่ม UI เท่านั้น) ·")
    print("        ห้ามสรุป 'ไม่มี refund' เกิน handler/สายที่วัด (§D.5) · เงิน/ทรัพยากร = แบบฟอร์มผู้ใช้ ·")
    print("        ป้าย CONTAMINATED/STATIC-CONTRADICTION = candidate — การทิ้งรัน/หยุดงาน = ผู้รายงานตามนโยบาย/§G.1")

    if args.json:
        try:
            Path(args.json).write_text(
                json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
            print("JSON written: %s" % args.json)
        except OSError as e:
            print("ARGS-ERROR: เขียน --json %s ไม่ได้: %s" % (args.json, e))
            return 2

    return 0 if any_usable else 1


if __name__ == "__main__":
    sys.exit(main())
