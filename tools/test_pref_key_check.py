"""Tests for pref_key_check.py — synthetic cfg text only (offline, no game, no real file touched)."""
import os
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pref_key_check as pkc


def write_cfg(text):
    fd, path = tempfile.mkstemp(suffix=".cfg")
    with os.fdopen(fd, "w", encoding="utf-8") as f:
        f.write(text)
    return path


class TestParse(unittest.TestCase):
    def test_sections_entries_quotes_comments(self):
        text = (
            "# comment\n"
            "[LongYinCoop]\n"
            "Enabled = true\n"
            "\"Quoted Key\" = false\n"
            "Str = 'C:\\path with spaces\\out'\n"
            "\n"
            "[UnityExplorer]\n"
            "\"UnityExplorer Toggle\" = \"F7\"\n"
        )
        sections, errors = pkc.parse_cfg(text)
        self.assertEqual(errors, [])
        self.assertEqual(sections["LongYinCoop"]["Enabled"], "true")
        self.assertEqual(sections["LongYinCoop"]["Quoted Key"], "false")
        self.assertEqual(sections["LongYinCoop"]["Str"], "'C:\\path with spaces\\out'")
        self.assertEqual(pkc.canon(sections["LongYinCoop"]["Str"]), "C:\\path with spaces\\out")
        self.assertEqual(sections["UnityExplorer"]["UnityExplorer Toggle"], "\"F7\"")

    def test_unparsed_line_reported(self):
        sections, errors = pkc.parse_cfg("[LongYinCoop]\nEnabled = true\nnot a line\n")
        self.assertEqual(len(errors), 1)
        self.assertEqual(errors[0][0], 3)

    def test_canon(self):
        self.assertEqual(pkc.canon(" True "), "true")
        self.assertEqual(pkc.canon("FALSE"), "false")
        self.assertEqual(pkc.canon("\"F7\""), "F7")
        self.assertEqual(pkc.canon("10"), "10")


class TestCheckFile(unittest.TestCase):
    BASE = (
        "[LongYinCoop]\n"
        "Enabled = true\n"
        "Probe = true\n"
        "AllowRemoteInput = false\n"
        "JitProbe = false\n"
        "Sp5T0 = false\n"
        "TimeV2a = false\n"
        "\n"
        "[UnityExplorer]\n"
        "\"Log To Disk\" = true\n"
    )

    def test_defaults_pass(self):
        path = write_cfg(self.BASE)
        self.addCleanup(os.remove, path)
        results, status = pkc.check_file(path, pkc.DEFAULT_CHECKS)
        self.assertEqual(status, "PASS")
        tags = {r[2]: r[0] for r in results}
        self.assertEqual(tags["Enabled"], "OK")
        self.assertEqual(tags["AllowRemoteInput"], "OK")
        self.assertEqual(tags["Sp5T0"], "OK")
        self.assertEqual(tags["TimeV2a"], "OK")

    def test_mismatch_is_fail(self):
        path = write_cfg(self.BASE.replace("AllowRemoteInput = false", "AllowRemoteInput = true"))
        self.addCleanup(os.remove, path)
        results, status = pkc.check_file(path, pkc.DEFAULT_CHECKS)
        self.assertEqual(status, "FAIL")
        self.assertTrue(any(r[0] == "FAIL" and r[2] == "AllowRemoteInput" for r in results))

    def test_required_key_missing_is_fail(self):
        path = write_cfg(self.BASE.replace("Sp5T0 = false\n", ""))
        self.addCleanup(os.remove, path)
        results, status = pkc.check_file(path, pkc.DEFAULT_CHECKS)
        self.assertEqual(status, "FAIL")
        self.assertTrue(any(r[0] == "FAIL" and r[2] == "Sp5T0" and "missing" in r[3] for r in results))

    def test_optional_key_absent_is_note_pass(self):
        path = write_cfg(self.BASE.replace("TimeV2a = false\n", ""))
        self.addCleanup(os.remove, path)
        results, status = pkc.check_file(path, pkc.DEFAULT_CHECKS)
        self.assertEqual(status, "PASS")
        self.assertTrue(any(r[0] == "NOTE" and r[2] == "TimeV2a" for r in results))

    def test_section_missing_is_fail(self):
        path = write_cfg("[Other]\nX = 1\n")
        self.addCleanup(os.remove, path)
        results, status = pkc.check_file(path, pkc.DEFAULT_CHECKS)
        self.assertEqual(status, "FAIL")
        self.assertTrue(any(r[0] == "FAIL" and "section 'LongYinCoop' missing" in r[3] for r in results))

    def test_nested_section_rename_reported_explicitly(self):
        # incident 2026-10-22: ML แปลงเป็นตารางซ้อน — ต้องรายงานชัดว่า section เปลี่ยน
        path = write_cfg("[LongYinCoop.coop]\nEnabled = true\n")
        self.addCleanup(os.remove, path)
        results, status = pkc.check_file(path, pkc.DEFAULT_CHECKS)
        self.assertEqual(status, "FAIL")
        hit = [r for r in results if r[0] == "FAIL" and "nested" in r[3] and "ML rewrite" in r[3]]
        self.assertEqual(len(hit), 1)

    def test_file_absent_is_error(self):
        results, status = pkc.check_file(os.path.join(tempfile.gettempdir(), "no-such-cfg-xyz.cfg"),
                                         pkc.DEFAULT_CHECKS)
        self.assertEqual(status, "ERROR")
        self.assertEqual(results[0][0], "ERROR")

    def test_unparsed_line_in_target_section_is_error_fail_closed(self):
        path = write_cfg("[LongYinCoop]\nEnabled = true\n??? garbage ???\n")
        self.addCleanup(os.remove, path)
        results, status = pkc.check_file(path, pkc.DEFAULT_CHECKS)
        self.assertEqual(status, "ERROR")
        self.assertTrue(any(r[0] == "ERROR" and "unparsed line" in r[3] for r in results))

    def test_quoted_value_and_case_insensitive_bool(self):
        path = write_cfg("[LongYinCoop]\nEnabled = True\nAllowRemoteInput = \"false\"\nSp5T0 = false\nTimeV2a = false\n")
        self.addCleanup(os.remove, path)
        results, status = pkc.check_file(path, pkc.DEFAULT_CHECKS)
        self.assertEqual(status, "PASS")


class TestMain(unittest.TestCase):
    def run_main(self, argv):
        out = []
        orig = sys.stdout
        class Cap:
            def write(self, s): out.append(s)
            def flush(self): pass
        sys.stdout = Cap()
        try:
            code = pkc.main(argv)
        finally:
            sys.stdout = orig
        return code, "".join(out)

    def test_exit_0_on_pass(self):
        path = write_cfg(TestCheckFile.BASE)
        self.addCleanup(os.remove, path)
        code, out = self.run_main(["--file", path])
        self.assertEqual(code, 0)
        self.assertIn("PREF-KEY-CHECK PASS", out)

    def test_exit_1_on_fail(self):
        path = write_cfg(TestCheckFile.BASE.replace("Enabled = true", "Enabled = false"))
        self.addCleanup(os.remove, path)
        code, out = self.run_main(["--file", path])
        self.assertEqual(code, 1)
        self.assertIn("PREF-KEY-CHECK FAIL", out)

    def test_exit_2_on_missing_file(self):
        code, out = self.run_main(["--file", "Z:/definitely/not/here.cfg"])
        self.assertEqual(code, 2)

    def test_check_override_sets_session_preflight(self):
        path = write_cfg(TestCheckFile.BASE.replace("TimeV2a = false", "TimeV2a = true"))
        self.addCleanup(os.remove, path)
        code, out = self.run_main(["--file", path, "--check", "LongYinCoop:TimeV2a=true"])
        self.assertEqual(code, 0)
        self.assertIn("TimeV2a", out)

    def test_check_override_detects_wrong_value(self):
        path = write_cfg(TestCheckFile.BASE)
        self.addCleanup(os.remove, path)
        code, out = self.run_main(["--file", path, "--check", "LongYinCoop:TimeV2a=true"])
        self.assertEqual(code, 1)

    def test_bad_check_arg_is_exit_2(self):
        path = write_cfg(TestCheckFile.BASE)
        self.addCleanup(os.remove, path)
        code, out = self.run_main(["--file", path, "--check", "no-colon-here"])
        self.assertEqual(code, 2)
        self.assertIn("ARGS ERROR", out)

    def test_parse_check_arg(self):
        self.assertEqual(pkc.parse_check_arg("LongYinCoop:TimeV2a=true"),
                         ("LongYinCoop", "TimeV2a", "true", False))
        with self.assertRaises(ValueError):
            pkc.parse_check_arg("KeyOnly=true")
        with self.assertRaises(ValueError):
            pkc.parse_check_arg("Section:Key=")


if __name__ == "__main__":
    unittest.main()
