"""Tests for sp5t0_audit.py — synthetic NDJSON only (offline, no game, no probe)."""
import json
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sp5t0_audit as aud


class TestSp5T0Audit(unittest.TestCase):
    def audit(self, events):
        fd, path = tempfile.mkstemp(suffix=".ndjson")
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            for e in events:
                f.write(json.dumps(e) + "\n")
        self.addCleanup(os.remove, path)
        return aud.audit(aud.load(path))

    def base_round(self):
        return [
            {"ev": "session.begin", "t": 0},
            {"ev": "patch.audit", "t": 1, "at": "session-begin"},
            {"ev": "sng.begin", "t": 100},
            # W2: genhero window — 3 factory calls (overload A) + heroNum read
            {"ev": "genhero.begin", "t": 110, "bucket": "creation"},
            {"ev": "heroNum.read", "t": 111},
            {"ev": "factory.call", "t": 112, "ph": "sng.genhero", "ov": "A"},
            {"ev": "factory.call", "t": 113, "ph": "sng.genhero", "ov": "A"},
            {"ev": "factory.call", "t": 114, "ph": "sng.genhero", "ov": "A"},
            {"ev": "genhero.end", "t": 120},
            # W3: genenemy window — 2 factory calls (overload C) + addtemp
            {"ev": "genenemy.begin", "t": 130},
            {"ev": "factory.call", "t": 131, "ph": "sng.genenemy", "ov": "C"},
            {"ev": "factory.call", "t": 132, "ph": "sng.genenemy", "ov": "C"},
            {"ev": "addtemp", "t": 133},
            {"ev": "addtemp", "t": 134},
            {"ev": "genenemy.end", "t": 140},
            {"ev": "worldcount", "t": 141, "H": 5, "hist": {"protagonist": 1, "spehero": 2, "sect": 2},
             "perForce": {0: 1, 1: 1}},
            {"ev": "sng.end", "t": 200},
            # post bucket: ChangeDay-driven genenemy without sng parent = normal (rev 4)
            {"ev": "factory.call", "t": 300, "ph": "genenemy", "ov": "C"},
            {"ev": "session.end", "t": 400},
        ]

    def test_clean_round(self):
        f = self.audit(self.base_round())
        self.assertEqual(f["e5_unattributed"], [])
        self.assertEqual(f["e7_failures"], [])
        self.assertEqual(f["phase_stuck"], [])
        self.assertEqual(f["factory_counts"]["creation"], 5)
        self.assertEqual(f["factory_counts"]["post"], 1)
        self.assertEqual(f["eq2"]["addtemp_W3"], 2)
        v = aud.verdict(f)
        self.assertIn("E5/E7 structural checks CLEAN", v)
        self.assertIn("PASS (structural)", v)
        self.assertIn("UNKNOWN (rev 13", v)  # E1/E2 stay observational

    def test_unattributed_fails_e5(self):
        ev = self.base_round()
        ev.insert(6, {"ev": "factory.call", "t": 115, "ph": "sng.unattributed", "ov": "B"})
        f = self.audit(ev)
        self.assertEqual(len(f["e5_unattributed"]), 1)
        v = aud.verdict(f)
        self.assertIn("E5 NOT CLOSED", v)
        self.assertIn("do NOT conclude 'second path'", v)
        self.assertIn("FAIL (structural)", v)

    def test_hist_mismatch_fails_e7(self):
        ev = self.base_round()
        idx = next(i for i, e in enumerate(ev) if e.get("ev") == "worldcount")
        ev[idx] = {**ev[idx], "H": 6}  # corrupt snapshot H only
        f = self.audit(ev)
        self.assertEqual(len(f["e7_failures"]), 1, f["e7_failures"])
        self.assertIn("FAIL (structural)", aud.verdict(f))

    def test_perforce_mismatch_fails_e7(self):
        ev = self.base_round()
        idx = next(i for i, e in enumerate(ev) if e.get("ev") == "worldcount")
        # break ONLY perForce (sum 3 != hist[sect] 2); hist sum stays == H
        ev[idx] = {**ev[idx], "perForce": {0: 1, 1: 2}}
        f = self.audit(ev)
        self.assertEqual(len(f["e7_failures"]), 1, f["e7_failures"])
        self.assertIn("perForce", f["e7_failures"][0]["why"])

    def test_phase_stuck_fails_round(self):
        ev = self.base_round()
        ev.append({"ev": "phaseStuck", "t": 350, "why": "pop-mismatch"})
        f = self.audit(ev)
        self.assertEqual(len(f["phase_stuck"]), 1)
        self.assertIn("phaseStuck != 0", aud.verdict(f))

    def test_pre_bucket_counted_separately(self):
        ev = self.base_round()
        ev.insert(2, {"ev": "factory.call", "t": 50, "ph": "outside", "ov": "B"})
        f = self.audit(ev)
        self.assertEqual(f["factory_counts"]["pre"], 1)
        self.assertEqual(f["factory_counts"]["creation"], 5)

    # ---- schema จริงของ probe (รอบ T0 20261030): `h` เล็ก · perForce = list 30 ช่อง ·
    #      session.begin = world null (ไม่มี hist/perForce) — แก้บั๊ก N1-2 ----

    def real_schema_worldcounts(self):
        """worldcount รูปจริง: world-null / genhero.begin (h=0) / genhero.end (h=5) / sng.end"""
        return [
            {"ev": "worldcount", "t": 1, "at": "session.begin", "h": 0},
            {"ev": "worldcount", "t": 111, "at": "genhero.begin", "h": 0,
             "hist": {"protag": 0, "spe": 0, "sect": 0, "wanderer": 0, "f2": 0, "temp": 0, "tempPlot": 0, "other": 0},
             "perForce": [0] * 30},
            {"ev": "worldcount", "t": 121, "at": "genhero.end", "h": 5,
             "hist": {"protag": 1, "spe": 2, "sect": 2, "wanderer": 0, "f2": 0, "temp": 0, "tempPlot": 0, "other": 0},
             "perForce": [1, 1] + [0] * 28},
            {"ev": "worldcount", "t": 201, "at": "sng.end", "h": 5,
             "hist": {"protag": 1, "spe": 2, "sect": 2, "wanderer": 0, "f2": 0, "temp": 0, "tempPlot": 0, "other": 0},
             "perForce": [1, 1] + [0] * 28},
        ]

    def test_real_schema_clean(self):
        ev = [e for e in self.base_round() if e.get("ev") != "worldcount"]
        ev.extend(self.real_schema_worldcounts())
        ev.sort(key=lambda e: e["t"])
        f = self.audit(ev)
        self.assertEqual(f["e7_failures"], [], f["e7_failures"])
        self.assertEqual(f["eq1"]["dH_observed"], 5)   # genhero.end 5 − genhero.begin 0
        v = aud.verdict(f)
        self.assertIn("PASS (structural)", v)

    def test_real_schema_perforce_list_mismatch(self):
        ev = [e for e in self.base_round() if e.get("ev") != "worldcount"]
        wc = self.real_schema_worldcounts()
        wc[2] = {**wc[2], "perForce": [2, 1] + [0] * 28}  # sum 3 != hist[sect] 2 (hist sum ยัง == h)
        ev.extend(wc)
        ev.sort(key=lambda e: e["t"])
        f = self.audit(ev)
        self.assertEqual(len(f["e7_failures"]), 1, f["e7_failures"])
        self.assertIn("perForce", f["e7_failures"][0]["why"])

    def test_world_null_snapshot_skipped(self):
        ev = [e for e in self.base_round() if e.get("ev") != "worldcount"]
        ev.extend(self.real_schema_worldcounts())
        ev.insert(1, {"ev": "worldcount", "t": 0, "at": "session.begin", "h": 0})  # world null — ไม่มี hist/perForce
        ev.sort(key=lambda e: e["t"])
        f = self.audit(ev)
        self.assertEqual(f["e7_failures"], [], f["e7_failures"])

    def test_dt_observed_from_temp_events(self):
        ev = self.base_round()
        idx_b = next(i for i, e in enumerate(ev) if e.get("ev") == "genenemy.begin")
        idx_e = next(i for i, e in enumerate(ev) if e.get("ev") == "genenemy.end")
        ev[idx_b] = {**ev[idx_b], "tempBefore": 0}
        ev[idx_e] = {**ev[idx_e], "tempAfter": 2}
        f = self.audit(ev)
        self.assertEqual(f["eq2"]["dT_observed"], 2)
        self.assertEqual(f["eq2"]["addtemp_W3"], 2)


if __name__ == "__main__":
    unittest.main()
