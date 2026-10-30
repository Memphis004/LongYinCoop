#!/usr/bin/env python3
"""เทสต์ synthetic ของ time_v1_analyze (อ่านอย่างเดียว — ไฟล์จำลองใน temp เท่านั้น)

ครอบ: การจำแนก route-a/route-b ตามลำดับ event (static §B.3) · R3 latch จาก TimeV2a ·
กฎ gap (CONTAMINATED-CANDIDATE) · STATIC-CONTRADICTION-CANDIDATE · exit code 0/1/2 ·
--screen-values echo ตามจริง · --json · บรรทัดเสีย/ไฟล์ว่าง (ไม่เดา)
"""
import json
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import time_v1_analyze as tva  # noqa: E402


def write_lines(path, lines):
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")


class TestLoadNdjson(unittest.TestCase):
    def test_malformed_counted_not_guessed(self):
        fd, path = tempfile.mkstemp(suffix=".ndjson")
        os.close(fd)
        self.addCleanup(os.remove, path)
        write_lines(path, ['{"t":1,"ev":"session.begin"}', "not-json", "", '{"t":2,"ev":"x"}'])
        events, malformed = tva.load_ndjson(path)
        self.assertEqual(len(events), 2)
        self.assertEqual(malformed, 1)

    def test_missing_file_raises_valueerror(self):
        # load_ndjson โยน ValueError — main จัดการเป็น exit 2 (fail-closed)
        with self.assertRaises(ValueError):
            tva.load_ndjson(os.path.join(tempfile.gettempdir(), "no-such-v1-xyz.ndjson"))


class TestAnalyzeProbe(unittest.TestCase):
    def test_route_a_ui_cancel_tc1_like(self):
        events = [
            {"t": 1, "ev": "session.begin"},
            {"t": 10, "ev": "middlestopwork.enter", "hasState": True, "working": True},
            {"t": 11, "ev": "stopworking.enter", "finished": False, "fireWorkFinished": False,
             "finishFuc": "FinishWorkInProductionBuilding", "workName": "采药",
             "hasState": True, "working": True, "workFinished": False,
             "leftWorkingDay": 4, "totalWorkingDay": 3, "noCancel": False},
            {"t": 12, "ev": "stopworking.exit", "finished": False, "hasState": True,
             "working": False, "workFinished": False},
            {"t": 13, "ev": "middlestopwork.exit", "hasState": True, "working": False},
            {"t": 99, "ev": "session.end"},
        ]
        res = tva.analyze_probe(events)
        self.assertEqual(len(res["stop_calls"]), 1)
        call = res["stop_calls"][0]
        self.assertEqual(call["route"], "route-a")
        self.assertEqual(call["finished"], False)
        self.assertTrue(call["exit_seen"])
        self.assertEqual(call["exit_working"], False)
        r1 = {"cancel_via_plot_choice_observed": any(
            c["route"] == "route-a" and c["finished"] is False for c in res["stop_calls"])}
        self.assertTrue(r1["cancel_via_plot_choice_observed"])

    def test_route_b_direct_call_without_middlestopwork(self):
        # per-day handler เรียก StopWorking ตรง (route-b) — ต้องไม่ถูกนับเป็น route-a
        events = [
            {"t": 1, "ev": "session.begin"},
            {"t": 10, "ev": "stopworking.enter", "finished": False, "hasState": True,
             "finishFuc": None},
            {"t": 11, "ev": "stopworking.exit", "finished": False, "hasState": True},
        ]
        res = tva.analyze_probe(events)
        self.assertEqual(res["stop_calls"][0]["route"], "route-b")
        self.assertEqual(res["middlestopwork"], 0)

    def test_exit_between_breaks_route_even_if_msw_earlier(self):
        # msw.enter อยู่ไกล (มี stopworking.exit คั่น) — ต้องตัดเป็น route-b (ไม่คู่กัน)
        events = [
            {"t": 1, "ev": "middlestopwork.enter"},
            {"t": 2, "ev": "stopworking.enter", "finished": True},
            {"t": 3, "ev": "stopworking.exit", "finished": True},
            {"t": 4, "ev": "middlestopwork.exit"},
            {"t": 50, "ev": "stopworking.enter", "finished": False},
            {"t": 51, "ev": "stopworking.exit", "finished": False},
        ]
        res = tva.analyze_probe(events)
        self.assertEqual(res["stop_calls"][1]["route"], "route-b")

    def test_finish_true_and_exit_missing(self):
        events = [
            {"t": 10, "ev": "stopworking.enter", "finished": True, "fireWorkFinished": True,
             "workFinished": False, "hasState": True},
            # ไม่มี exit = INCOMPLETE (postfix ไม่เคยรัน/ยังไม่จบ)
        ]
        res = tva.analyze_probe(events)
        call = res["stop_calls"][0]
        self.assertEqual(call["finished"], True)
        self.assertFalse(call["exit_seen"])

    def test_finalizer_exception_recorded(self):
        events = [
            {"t": 10, "ev": "stopworking.finalizer", "ex": "SomeError"},
            {"t": 11, "ev": "middlestopwork.finalizer", "ex": None},
        ]
        res = tva.analyze_probe(events)
        self.assertEqual(len(res["finalizers_with_ex"]), 1)
        self.assertEqual(res["finalizers_with_ex"][0]["ex"], "SomeError")


class TestAnalyzeV2a(unittest.TestCase):
    @staticmethod
    def day(i, t, wf, left):
        return {"t": t, "ev": "day_tick", "workFinished": wf, "leftWorkingDay": left}

    def test_r3_latch_on_second_session(self):
        # TC-4-like: งานแรกจบปกติ (work_end) → งานที่สอง — day_tick ต้องพก workFinished=true (latch)
        events = [
            {"t": 0, "ev": "work_start", "leftWorkingDay": 2},
            self.day(1, 100, False, 1),
            {"t": 200, "ev": "pause_end"},
            self.day(2, 300, False, 0),
            {"t": 400, "ev": "work_end"},
            {"t": 500, "ev": "work_start", "leftWorkingDay": 1},
            self.day(3, 600, True, 0),   # latch = true ระหว่างงานที่สอง
            {"t": 700, "ev": "pause_end", "workFinished": True},
            {"t": 800, "ev": "work_end"},
        ]
        res = tva.analyze_v2a(events, 2000)
        self.assertEqual(len(res["sessions"]), 2)
        s2 = res["sessions"][1]
        self.assertEqual(s2["latch_true_events"], 2)
        self.assertEqual(s2["latch_false_events"], 0)
        self.assertNotIn("static_contradiction_candidate", s2)

    def test_static_contradiction_candidate_flagged(self):
        events = [
            {"t": 0, "ev": "work_start"},
            self.day(1, 100, False, 0),
            {"t": 200, "ev": "work_end"},
            {"t": 300, "ev": "work_start"},
            self.day(2, 400, False, 0),  # session#2 latch=false → CANDIDATE
            {"t": 500, "ev": "work_end"},
        ]
        res = tva.analyze_v2a(events, 2000)
        s2 = res["sessions"][1]
        self.assertEqual(s2["latch_false_events"], 1)
        self.assertTrue(s2.get("static_contradiction_candidate"))

    def test_gap_contaminated_candidate(self):
        events = [
            {"t": 0, "ev": "work_start"},
            self.day(1, 100, False, 1),
            self.day(2, 5000, False, 0),  # gap 4900 ms > 2000
            {"t": 5100, "ev": "work_end"},
        ]
        res = tva.analyze_v2a(events, 2000)
        s1 = res["sessions"][0]
        self.assertTrue(s1.get("contaminated_candidate"))
        self.assertEqual(res["gap_hits"][0]["max_gap_ms"], 4900)

    def test_open_without_close(self):
        events = [
            {"t": 0, "ev": "work_start"},
            self.day(1, 100, False, 1),
            # จบไฟล์โดยไม่มี work_end
        ]
        res = tva.analyze_v2a(events, 2000)
        self.assertEqual(len(res["sessions"]), 1)
        self.assertEqual(res["sessions"][0]["end"]["ev"], "no-close-seen (run จบก่อนงานปิด)")
        self.assertEqual(res["open_without_close"], 0)  # session มี event → นับเป็น session จบแบบ no-close

    def test_no_sessions(self):
        res = tva.analyze_v2a([{"t": 1, "ev": "pause_end"}], 2000)
        self.assertEqual(len(res["sessions"]), 0)


class TestMain(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.mkdtemp(prefix="timev1-an-")
        self.probe = os.path.join(self.dir, "probe.ndjson")
        self.v2a = os.path.join(self.dir, "v2a.ndjson")
        self.screen = os.path.join(self.dir, "screen.json")

    def tearDown(self):
        for name in os.listdir(self.dir):
            os.remove(os.path.join(self.dir, name))
        os.rmdir(self.dir)

    def test_full_flow_exit0_and_json(self):
        write_lines(self.probe, [
            json.dumps({"t": 10, "ev": "middlestopwork.enter"}),
            json.dumps({"t": 11, "ev": "stopworking.enter", "finished": False,
                        "fireWorkFinished": False, "finishFuc": "FinishWorkInProductionBuilding",
                        "totalWorkingDay": 3, "hasState": True}),
            json.dumps({"t": 12, "ev": "stopworking.exit", "finished": False}),
        ])
        write_lines(self.v2a, [
            json.dumps({"t": 0, "ev": "work_start", "leftWorkingDay": 1}),
            json.dumps({"t": 100, "ev": "day_tick", "workFinished": False, "leftWorkingDay": 0}),
            json.dumps({"t": 200, "ev": "work_end"}),
        ])
        with open(self.screen, "w", encoding="utf-8") as f:
            json.dump({"money_before": 4733, "money_after": 4733, "dialog_shown": "yes"}, f)
        out_json = os.path.join(self.dir, "out.json")
        rc = tva.main(["--probe", self.probe, "--v2a", self.v2a,
                       "--screen-values", self.screen, "--json", out_json, "--label", "TC1-r1"])
        self.assertEqual(rc, 0)
        with open(out_json, "r", encoding="utf-8") as f:
            rep = json.load(f)
        self.assertEqual(rep["probe_files"][0]["r1"]["route_a_cancel_count"], 1)
        self.assertEqual(rep["screen_values_echo"]["money_before"], 4733)
        self.assertEqual(rep["v2a_files"][0]["sessions"][0]["latch_true_events"], 0)
        self.assertEqual(rep["v2a_files"][0]["sessions"][0]["latch_false_events"], 1)

    def test_no_usable_events_exit1(self):
        write_lines(self.probe, [])
        self.assertEqual(tva.main(["--probe", self.probe]), 1)

    def test_no_args_exit2(self):
        self.assertEqual(tva.main([]), 2)

    def test_bad_screen_values_exit2(self):
        write_lines(self.probe, [json.dumps({"t": 1, "ev": "session.begin"})])
        with open(self.screen, "w", encoding="utf-8") as f:
            f.write("{not json")
        self.assertEqual(tva.main(["--probe", self.probe, "--screen-values", self.screen]), 2)

    def test_missing_probe_file_exit2(self):
        rc = tva.main(["--probe", os.path.join(self.dir, "absent.ndjson")])
        self.assertEqual(rc, 2)


if __name__ == "__main__":
    unittest.main()
