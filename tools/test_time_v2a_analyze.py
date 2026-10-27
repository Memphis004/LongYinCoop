"""Tests for time_v2a_analyze.py — synthetic NDJSON only (offline, no game)."""
import json
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import time_v2a_analyze as an


class TestV2AAnalyzer(unittest.TestCase):
    def stream(self, n, total_ms, start_ms=1_000_000, skipping=False, abort=False):
        """Synthesize one work session: work_start, N evenly spaced day_ticks, work_end at total."""
        ev = [{"ev": "work_start", "t": start_ms, "leftWorkingDay": n, "skipping": skipping,
               "skipK": 0.5 if skipping else 1.0, "dayTime": 1.0, "pauseTime": 0.25}]
        for i in range(1, n + 1):
            ev.append({"ev": "day_tick", "t": start_ms + int(i * total_ms / n),
                       "leftWorkingDay": n - i})
        kind = "work_abort" if abort else "work_end"
        ev.append({"ev": kind, "t": start_ms + total_ms})
        return ev

    def dump(self, events):
        fd, path = tempfile.mkstemp(suffix=".ndjson")
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            for e in events:
                f.write(json.dumps(e) + "\n")
        self.addCleanup(os.remove, path)
        return path

    def test_form1_synthetic(self):
        # stream built exactly as form1 = N*(D*k) + N*P (k=1, D=1, P=0.25, N=5 → 6.25 s)
        n = 5
        total_ms = (n * (1.0 * 1.0) + n * 0.25) * 1000
        s = an.extract_sessions(an.load_events(self.dump(self.stream(n, total_ms))))[0]
        r = an.analyze_session(s, tolerance_s=0.1)
        self.assertEqual(r["day_ticks"], n)
        self.assertEqual(r["verdict"], "FORM1", r)

    def test_form2_synthetic(self):
        # stream built exactly as form2 = N*D + (N+2)*P (N=5 → 6.75 s)
        n = 5
        total_ms = (n * 1.0 + (n + 2) * 0.25) * 1000
        s = an.extract_sessions(an.load_events(self.dump(self.stream(n, total_ms))))[0]
        r = an.analyze_session(s, tolerance_s=0.1)
        self.assertEqual(r["verdict"], "FORM2", r)

    def test_n10_indistinguishable(self):
        # N=10, D=1, P=0.25: form1=12.5, form2=13.0 — gap 0.5 >= tolerance → INDISTINGUISHABLE
        s = an.extract_sessions(an.load_events(self.dump(self.stream(10, 12_500))))[0]
        r = an.analyze_session(s)
        self.assertEqual(r["verdict"], "INDISTINGUISHABLE", r)
        self.assertAlmostEqual(r["form1_s"], 12.5, places=3)
        self.assertAlmostEqual(r["form2_s"], 13.0, places=3)
        self.assertAlmostEqual(r["form_gap_s"], 0.5, places=3)

    def test_invalid_day_ticks(self):
        s = an.extract_sessions(an.load_events(self.dump(self.stream(3, 4_000))))[0]
        s["day_ticks"] = 5
        r = an.analyze_session(s)
        self.assertEqual(r["verdict"], "INVALID")

    def test_aborted_excluded(self):
        s = an.extract_sessions(an.load_events(self.dump(self.stream(3, 4_000, abort=True))))[0]
        r = an.analyze_session(s)
        self.assertEqual(r["verdict"], "EXCLUDED")

    def test_skip_k_propagates(self):
        n, k = 4, 0.5
        total_ms = (n * (1.0 * k) + n * 0.25) * 1000
        s = an.extract_sessions(an.load_events(self.dump(self.stream(n, total_ms, skipping=True))))[0]
        r = an.analyze_session(s)
        self.assertEqual(r["k"], 0.5)
        self.assertAlmostEqual(r["form1_s"], n * (1.0 * k + 0.25), places=3)

    def test_unterminated_raises(self):
        events = [{"ev": "work_start", "t": 1, "leftWorkingDay": 2, "skipping": False,
                   "dayTime": 1.0, "pauseTime": 0.25}]
        with self.assertRaises(SystemExit):
            an.extract_sessions(an.load_events(self.dump(events)))

    def test_bad_json_raises(self):
        fd, path = tempfile.mkstemp(suffix=".ndjson")
        with os.fdopen(fd, "w") as f:
            f.write("{not json}\n")
        self.addCleanup(os.remove, path)
        with self.assertRaises(SystemExit):
            an.load_events(path)

    def test_two_sessions(self):
        evs = self.stream(3, 4_000) + self.stream(2, 3_000, start_ms=2_000_000)
        sessions = an.extract_sessions(an.load_events(self.dump(evs)))
        self.assertEqual(len(sessions), 2)
        self.assertEqual([s["n"] for s in sessions], [3, 2])


if __name__ == "__main__":
    unittest.main()
