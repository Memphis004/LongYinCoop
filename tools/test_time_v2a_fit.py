"""Tests for time_v2a_fit.py — synthetic NDJSON only (offline, no game) · เกณฑ์ [CRITERIA] ภาคผนวก A"""
import json
import os
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import time_v2a_fit as ft


def session(n, measured_ms, start_ms=1_000_000.0, abort=False, drop_pause_index=None):
    """สร้าง session ตรงโครง: work_start → pause_end → [day_tick → pause_end]×N → work_end
    กระจาย measured_ms: initial 250 + n×(day+250) + finish 7 (float — ให้ fit ตรงเป้า)
    drop_pause_index = ลบ pause_end ตัวที่ index นั้น (0-based จาก pause_end ทั้งหมด) เพื่อทดสอบโครงพัง"""
    ev = [{"ev": "work_start", "t": start_ms, "leftWorkingDay": n, "skipping": False,
           "dayTime": 1.0, "pauseTime": 0.25}]
    overhead = 250 + n * 250 + 7
    day_ms = max(0.001, (measured_ms - overhead) / n)
    t = start_ms + 250.0
    ev.append({"ev": "pause_end", "t": t, "leftWorkingDay": n})
    for i in range(n):
        t += day_ms
        ev.append({"ev": "day_tick", "t": t, "leftWorkingDay": n - 1 - i})
        t += 250.0
        ev.append({"ev": "pause_end", "t": t, "leftWorkingDay": n - 1 - i})
    t += 7.0
    ev.append({"ev": "work_abort" if abort else "work_end", "t": t, "leftWorkingDay": 0,
               "workFinished": not abort})
    if drop_pause_index is not None:
        pauses = [e for e in ev if e["ev"] == "pause_end"]
        ev = [e for e in ev if e is not pauses[drop_pause_index]]
    return ev


def dump(tmp, name, sessions):
    path = os.path.join(tmp, name)
    with open(path, "w", encoding="utf-8") as f:
        for s in sessions:
            for e in s:
                f.write(json.dumps(e) + "\n")
    return path


class TestV2AFit(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.mkdtemp(prefix="v2afit-")

    def run_fit(self, paths):
        out = []
        for p in paths if isinstance(paths, list) else [paths]:
            with open(p, "r", encoding="utf-8") as f:
                out += f.read().splitlines()
        return "\n".join(out)

    def test_form1_fit(self):
        # measured = 1.25×N + 0 → slope 1.25, intercept 0 → FORM1 · exit 0
        files = [dump(self.tmp, f"f1-n{n}.ndjson", [session(n, int(1250 * n))])
                 for n in (1, 5, 10)]
        code = ft.main(files)
        self.assertEqual(code, 0)

    def test_form2_fit(self):
        # measured = 1.25×N + 0.5 → intercept 0.5 → FORM2 · exit 0
        files = [dump(self.tmp, f"f2-n{n}.ndjson", [session(n, int(1250 * n + 500))])
                 for n in (1, 5, 10)]
        code = ft.main(files)
        self.assertEqual(code, 0)

    def test_slope_mismatch_exit3(self):
        # measured = 1.6×N → slope นอก 1.25±0.1 → exit 3 (หยุดรายงาน)
        files = [dump(self.tmp, f"bad-n{n}.ndjson", [session(n, int(1600 * n))])
                 for n in (1, 5, 10)]
        code = ft.main(files)
        self.assertEqual(code, 3)

    def test_indistinguishable_intercept(self):
        # intercept 0.25 (นอกทั้งสองช่วง) → INDISTINGUISHABLE · exit 0 (ยอมรับได้)
        files = [dump(self.tmp, f"ind-n{n}.ndjson", [session(n, int(1250 * n + 250))])
                 for n in (1, 5, 10)]
        code = ft.main(files)
        self.assertEqual(code, 0)

    def test_boundary_intercepts(self):
        # 0.1 (ขอบบนของ FORM1) → FORM1 · 0.4 (ขอบล่างของ FORM2) → FORM2
        files = [dump(self.tmp, f"b1-n{n}.ndjson", [session(n, int(1250 * n + 100))])
                 for n in (1, 5, 10)]
        self.assertEqual(ft.main(files), 0)
        files = [dump(self.tmp, f"b2-n{n}.ndjson", [session(n, int(1250 * n + 400))])
                 for n in (1, 5, 10)]
        self.assertEqual(ft.main(files), 0)

    def test_invalid_day_ticks_excluded(self):
        # day_ticks != N → INVALID (ตัดออก) — ถ้าไม่เหลือ run ใช้ได้ = exit 1
        s = session(3, 5000)
        # ลบ day_tick 1 ตัวออก → โครงพัง (order mismatch + day_ticks 2 != 3)
        bad = [e for e in s if e["ev"] != "day_tick"]
        p = dump(self.tmp, "invalid.ndjson", [bad])
        code = ft.main([p])
        self.assertEqual(code, 1)

    def test_aborted_excluded(self):
        files = [dump(self.tmp, "ab.ndjson", [session(3, 5000, abort=True)])]
        code = ft.main(files)
        self.assertEqual(code, 1)

    def test_single_point_not_fittable(self):
        # run เดียว (N เดียว) → fit ทำไม่ได้ → exit 1 (ต้องมี ≥2 run ที่ N ต่างกัน)
        files = [dump(self.tmp, "one.ndjson", [session(1, 1250)])]
        code = ft.main(files)
        self.assertEqual(code, 1)

    def test_missing_file_exit2(self):
        code = ft.main([os.path.join(self.tmp, "nope.ndjson")])
        self.assertEqual(code, 2)

    def test_cli_real_exit_code(self):
        # ตรวจ exit code จริงผ่าน subprocess (บทเรียน 16a38c3 — ห้ามกลืน exit status)
        files = [dump(self.tmp, f"cli-n{n}.ndjson", [session(n, int(1250 * n + 500))])
                 for n in (1, 5, 10)]
        script = os.path.join(os.path.dirname(os.path.abspath(__file__)), "time_v2a_fit.py")
        r = subprocess.run([sys.executable, script] + files, capture_output=True, text=True)
        self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
        self.assertIn('"form": "FORM2"', r.stdout)
        files = [dump(self.tmp, f"cli2-n{n}.ndjson", [session(n, int(1600 * n))])
                 for n in (1, 5, 10)]
        r = subprocess.run([sys.executable, script] + files, capture_output=True, text=True)
        self.assertEqual(r.returncode, 3, r.stdout + r.stderr)
        self.assertIn("SLOPE MISMATCH", r.stdout)


if __name__ == "__main__":
    unittest.main()
