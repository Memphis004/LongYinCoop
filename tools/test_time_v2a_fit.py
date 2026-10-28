"""Tests for time_v2a_fit.py — synthetic NDJSON only (offline, no game) · เกณฑ์ [CRITERIA] ภาคผนวก A
(ปรับตามมติ 2026-10-29 รอบ A: UNRESOLVED_OFFSET · INDISTINGUISHABLE เฉพาะคาบเกี่ยว/ข้อมูลไม่พอ ·
segment observational · --exclude pilot)"""
import contextlib
import io
import json
import os
import subprocess
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import time_v2a_fit as ft


def session(n, measured_ms, start_ms=1_000_000.0, abort=False, drop_pause_index=None,
            stretch_pause_index=None, stretch_ms=0):
    """สร้าง session ตรงโครง: work_start → pause_end → [day_tick → pause_end]×N → work_end
    กระจาย measured_ms: initial 250 + n×(day+250) + finish 7 (float — ให้ fit ตรงเป้า)
    drop_pause_index = ลบ pause_end ตัวที่ index นั้น (0-based จาก pause_end ทั้งหมด) เพื่อทดสอบโครงพัง
    stretch_pause_index/stretch_ms = ยืดช่องว่างก่อน pause_end ตัวนั้น (จำลอง stall กลางงาน)
    คืน (events, expected_segments)"""
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
    if stretch_pause_index is not None:
        pauses = [e for e in ev if e["ev"] == "pause_end"]
        target_t = pauses[stretch_pause_index]["t"]
        for e in ev:
            if e["t"] >= target_t:
                e["t"] += stretch_ms
    expected_segments = {
        "initial_ms": 250.0,
        "day_mean_ms": round(day_ms, 1), "day_min_ms": round(day_ms, 1),
        "day_max_ms": round(day_ms, 1),
        "final_ms": 250.0, "tail_ms": 7.0,
    }
    return ev, expected_segments


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

    def run_main(self, argv):
        """เรียก ft.main พร้อมจับ stdout — คืน (exit_code, stdout)"""
        buf = io.StringIO()
        with contextlib.redirect_stdout(buf):
            code = ft.main(argv)
        return code, buf.getvalue()

    def test_form1_fit(self):
        # measured = 1.25×N + 0 → slope 1.25, intercept 0 → FORM1 · exit 0
        files = [dump(self.tmp, f"f1-n{n}.ndjson", [session(n, 1250 * n)[0]])
                 for n in (1, 5, 10)]
        code, out = self.run_main(files)
        self.assertEqual(code, 0)
        self.assertIn('"form": "FORM1"', out)

    def test_form2_fit(self):
        # measured = 1.25×N + 0.5 → intercept 0.5 → FORM2 · exit 0
        files = [dump(self.tmp, f"f2-n{n}.ndjson", [session(n, 1250 * n + 500)[0]])
                 for n in (1, 5, 10)]
        code, out = self.run_main(files)
        self.assertEqual(code, 0)
        self.assertIn('"form": "FORM2"', out)

    def test_slope_mismatch_exit3(self):
        # measured = 1.6×N → slope นอก 1.25±0.1 → exit 3 (หยุดรายงาน)
        files = [dump(self.tmp, f"bad-n{n}.ndjson", [session(n, 1600 * n)[0]])
                 for n in (1, 5, 10)]
        code, out = self.run_main(files)
        self.assertEqual(code, 3)
        self.assertIn("SLOPE MISMATCH", out)

    def test_unresolved_offset_intercept(self):
        # intercept 0.25 (นอกทั้งสอง window และไม่มี CI ให้คาบเกี่ยว) → UNRESOLVED_OFFSET · exit 0
        # (จุด 3 จุดอยู่บนเส้นตรงเดียว = residual 0 = ไม่มี CI)
        files = [dump(self.tmp, f"unr-n{n}.ndjson", [session(n, 1250 * n + 250)[0]])
                 for n in (1, 5, 10)]
        code, out = self.run_main(files)
        self.assertEqual(code, 0)
        self.assertIn('"form": "UNRESOLVED_OFFSET"', out)

    def test_ci_overlap_indistinguishable(self):
        # ตรรกะคาบเกี่ยว: intercept 0.25 + CI ±0.4 → ช่วง [−0.15, 0.65] ครอบ/ทับทั้ง FORM1 และ FORM2
        # → INDISTINGUISHABLE (สงวนไว้เฉพาะกรณีนี้ — มติ A1)
        fv, ci = ft.form_verdict(0.25, 0.4)
        self.assertEqual(fv, "INDISTINGUISHABLE")
        self.assertEqual(ci, [-0.15, 0.65])
        # CI ทับเฉพาะฝั่งเดียว = ยังไม่ INDISTINGUISHABLE (ตก UNRESOLVED_OFFSET)
        fv2, _ = ft.form_verdict(0.25, 0.1)
        self.assertEqual(fv2, "UNRESOLVED_OFFSET")
        # ไม่มี CI (จุดน้อย/fit แน่น) = UNRESOLVED_OFFSET
        fv3, ci3 = ft.form_verdict(0.7, None)
        self.assertEqual(fv3, "UNRESOLVED_OFFSET")
        self.assertIsNone(ci3)

    def test_boundary_intercepts(self):
        # จุดใน window ยังตัดสินฟอร์มได้ตรง ๆ: 0.1 → FORM1 · 0.4 → FORM2
        files = [dump(self.tmp, f"b1-n{n}.ndjson", [session(n, 1250 * n + 100)[0]])
                 for n in (1, 5, 10)]
        code, out = self.run_main(files)
        self.assertEqual(code, 0)
        self.assertIn('"form": "FORM1"', out)
        files = [dump(self.tmp, f"b2-n{n}.ndjson", [session(n, 1250 * n + 400)[0]])
                 for n in (1, 5, 10)]
        code, out = self.run_main(files)
        self.assertEqual(code, 0)
        self.assertIn('"form": "FORM2"', out)

    def test_segments_report(self):
        # A2: segment observational รายรันตรงโครง generator (initial 250 / day / final 250 / tail 7)
        s, expected = session(1, 2000)
        p = dump(self.tmp, "seg.ndjson", [s])
        code, out = self.run_main([p])
        self.assertEqual(code, 1)   # จุดเดียว fit ไม่ได้ — แต่ row ยังต้องมี segments
        row = [json.loads(l) for l in out.splitlines() if l.startswith("{") and "segments_observed" in l]
        self.assertEqual(len(row), 1)
        got = row[0]["segments_observed"]
        for k, v in expected.items():
            self.assertAlmostEqual(got[k], v, delta=0.2, msg=f"{k}: {got} vs {v}")

    def test_pilot_excluded_from_fit(self):
        # --exclude กันไฟล์ pilot ออกจาก fit (A1-r1 ไม่นับเป็นผลยืนยัน — มติ 2026-10-29)
        # รูปแบบใช้งาน: ส่งไฟล์ครบเป็น positional แล้วระบุไฟล์ที่ต้องกันด้วย --exclude (ตรง path เป๊ะ)
        keep = [dump(self.tmp, f"keep-n{n}.ndjson", [session(n, 1250 * n + 500)[0]])
                for n in (1, 10)]
        pilot = dump(self.tmp, "pilot.ndjson", [session(5, 9999)[0]])   # ค่าเพี้ยน — ต้องไม่มีผลต่อ fit
        code, out = self.run_main(keep + [pilot, "--exclude", pilot])
        self.assertEqual(code, 0)
        self.assertIn("EXCLUDED_PILOT", out)
        self.assertIn('"form": "FORM2"', out)   # fit ใช้เฉพาะ keep → intercept 0.5 ยังตรง
        self.assertNotIn("9999", out)           # ค่าของ pilot ไม่ถูกวัดเข้า fit
        self.assertIn("excluded=1", out)

    def test_invalid_day_ticks_excluded(self):
        # day_ticks != N → INVALID (ตัดออก) — ถ้าไม่เหลือ run ใช้ได้ = exit 1
        s, _ = session(3, 5000)
        bad = [e for e in s if e["ev"] != "day_tick"]
        p = dump(self.tmp, "invalid.ndjson", [bad])
        code, out = self.run_main([p])
        self.assertEqual(code, 1)
        self.assertIn("INVALID", out)

    def test_aborted_excluded(self):
        s, _ = session(3, 5000, abort=True)
        p = dump(self.tmp, "ab.ndjson", [s])
        code, out = self.run_main([p])
        self.assertEqual(code, 1)
        self.assertIn("EXCLUDED", out)

    def test_single_point_not_fittable(self):
        # run เดียว (N เดียว) → fit ทำไม่ได้ → exit 1 (ต้องมี ≥2 run ที่ N ต่างกัน)
        s, _ = session(1, 1250)
        p = dump(self.tmp, "one.ndjson", [s])
        code, _ = self.run_main([p])
        self.assertEqual(code, 1)

    def test_missing_file_exit2(self):
        code, _ = self.run_main([os.path.join(self.tmp, "nope.ndjson")])
        self.assertEqual(code, 2)

    def test_cli_real_exit_code(self):
        # ตรวจ exit code จริงผ่าน subprocess (บทเรียน 16a38c3 — ห้ามกลืน exit status)
        files = [dump(self.tmp, f"cli-n{n}.ndjson", [session(n, 1250 * n + 500)[0]])
                 for n in (1, 5, 10)]
        script = os.path.join(os.path.dirname(os.path.abspath(__file__)), "time_v2a_fit.py")
        r = subprocess.run([sys.executable, script] + files, capture_output=True, text=True)
        self.assertEqual(r.returncode, 0, r.stdout + r.stderr)
        self.assertIn('"form": "FORM2"', r.stdout)
        files = [dump(self.tmp, f"cli2-n{n}.ndjson", [session(n, 1600 * n)[0]])
                 for n in (1, 5, 10)]
        r = subprocess.run([sys.executable, script] + files, capture_output=True, text=True)
        self.assertEqual(r.returncode, 3, r.stdout + r.stderr)
        self.assertIn("SLOPE MISMATCH", r.stdout)


    def test_ci_t_df1_wider_than_196(self):
        # 3 จุด → df=1 → tcrit 12.706 · รายงาน CI แบบ t (valid) คู่กับ 1.96 (ติดป้ายไม่ valid สำหรับ df ต่ำ)
        # · intercept 0.8175 (นอกทั้งคู่) + residual ±15–26 ms ⇒ CI_t คาบเกี่ยวเฉพาะ window ของ FORM2
        files = [dump(self.tmp, f"t1-n{n}.ndjson",
                      [session(n, 1250 * n + 800 + d)[0]])
                 for n, d in ((1, 0), (5, 30), (10, -20))]
        code, out = self.run_main(files)
        self.assertEqual(code, 0)
        self.assertIn('"df": 1', out)
        self.assertIn('"tcrit_95": 12.706', out)
        row = json.loads([l for l in out.splitlines() if '"tcrit_95"' in l][0])
        self.assertGreater(row["intercept_ci_half_t"], row["intercept_ci_half_196"])
        self.assertIn("1.96 invalid for low df", row["ci196_note"])
        self.assertEqual(row["overlaps_t"]["FORM1"], False)
        self.assertEqual(row["overlaps_t"]["FORM2"], True)
        self.assertEqual(row["form"], "UNRESOLVED_OFFSET")

    def test_ci_t_df2(self):
        # 4 จุด → df=2 → tcrit 4.303 · intercept ~0.5026 ใน window ของ FORM2 (จุดตัดสินก่อน CI)
        files = [dump(self.tmp, f"t2-n{n}.ndjson",
                      [session(n, 1250 * n + 500 + d)[0]])
                 for n, d in ((1, 0), (2, 20), (3, -15), (5, 10))]
        code, out = self.run_main(files)
        self.assertEqual(code, 0)
        self.assertIn('"df": 2', out)
        self.assertIn('"tcrit_95": 4.303', out)
        self.assertIn('"form": "FORM2"', out)

    def test_ci_df0_two_points_no_ci(self):
        # 2 จุด → df=0 → ไม่รายงาน CI (null ทั้งคู่) แต่ fit ยังออก slope/intercept/ป้ายจากจุด
        files = [dump(self.tmp, "d0-n1.ndjson", [session(1, 1250 + 500)[0]]),
                 dump(self.tmp, "d0-n5.ndjson", [session(5, 1250 * 5 + 500)[0]])]
        code, out = self.run_main(files)
        self.assertEqual(code, 0)
        self.assertIn('"df": 0', out)
        self.assertIn('"intercept_ci_t": null', out)
        self.assertIn('"intercept_ci_196": null', out)
        self.assertIn('"form": "FORM2"', out)


    def test_gap_rule_contaminated_auto_excluded(self):
        # กฎคุณภาพข้อมูล: ช่องว่างระหว่าง event ติดกัน > 2000 ms = CONTAMINATED (กันออกจาก fit อัตโนมัติ)
        # · จำลอง stall 2.5 s กลางงาน (ยืด inter-day pause ก่อน pause_end ตัวที่ 2)
        s, _ = session(3, 5000, stretch_pause_index=2, stretch_ms=2500)
        p = dump(self.tmp, "stall.ndjson", [s])
        code, out = self.run_main([p])
        self.assertEqual(code, 1)   # เหลือ 0 จุด valid
        self.assertIn("CONTAMINATED", out)
        row = [json.loads(l) for l in out.splitlines() if '"CONTAMINATED"' in l][0]
        self.assertGreater(row["max_gap_ms"], 2000)
        self.assertIn("day_tick", row["max_gap_after"])   # gap เกิดหลัง day_tick

    def test_gap_rule_boundary_clean(self):
        # ยืด pause 1.5 s (gap ~1750 < 2000) = ยัง OK — fit ทำงานปกติ (สร้าง offset ใน stream
        # เพราะ stretch เลื่อน t ทุก event ถัดไป — จุดตรวจคือสถานะ gap rule ไม่ใช่ฟอร์ม)
        files = [dump(self.tmp, f"gs-n{n}.ndjson",
                      [session(n, 1250 * n + 500, stretch_pause_index=1, stretch_ms=1500)[0]])
                 for n in (1, 5, 10)]
        code, out = self.run_main(files)
        self.assertEqual(code, 0)
        self.assertNotIn("CONTAMINATED", out)
        self.assertEqual(out.count('"status": "OK"'), 3)
        self.assertIn('"slope_ok": true', out)

    def test_gap_rule_retroactive_real_files(self):
        # ใช้ย้อนกับไฟล์จริงของ session นี้: A3-r2 = CONTAMINATED (gap 16118 ms หลัง day_tick left=4) ·
        # A3-r1/A2-r2/A1-r2 = OK เหมือนเดิม (clean — ไม่มี gap > 2 s) ⇒ ผลรัน valid เดิมไม่เปลี่ยน
        base = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "evidence")
        f = lambda lbl, fn: os.path.join(base, lbl, fn)
        files = [f("V2A-a3r1-20261029-030222", "a3-r1.ndjson"),
                 f("V2A-a3r2-20261029-034639", "a3-r2.ndjson"),
                 f("V2A-a2r2-20261029-032912", "a2-r2.ndjson"),
                 f("V2A-a1r2-20261029-033639", "a1-r2.ndjson")]
        code, out = self.run_main(files)
        self.assertEqual(code, 0)
        self.assertEqual(out.count('"status": "CONTAMINATED"'), 1)   # เฉพาะ A3-r2
        self.assertIn('"max_gap_ms": 16118', out)
        self.assertIn('after day_tick(left=4) -> pause_end(left=4)', out)
        self.assertEqual(out.count('"status": "OK"'), 3)             # A3-r1/A2-r2/A1-r2 ไม่เปลี่ยน


if __name__ == "__main__":
    unittest.main()
