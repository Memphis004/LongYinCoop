#!/usr/bin/env python3
"""Offline unit tests for tools/scan_secrets.py.

No network, no game, no git history mutation: `_git` is patched with fixture
output and the repo root is redirected to a temp directory.  Every test also
asserts that the secret value never appears in scanner output.

Run:  python tools/test_scan_secrets.py -v
"""
import importlib.util
import io
import os
import sys
import tempfile
import unittest
from contextlib import redirect_stdout, redirect_stderr
from unittest import mock

HERE = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location("scan_secrets", os.path.join(HERE, "scan_secrets.py"))
ss = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(ss)

# Fixture secrets are synthetic (not the burned value from evidence/).
FAKE_HEX = "0123456789abcdef0123456789abcdef"
FAKE_TOKEN = "AbCdEf0123456789xyzXYZ+/="


class ClassifyTests(unittest.TestCase):
    def test_plain_hex_is_real(self):
        self.assertEqual(ss.classify_line("secret = %s" % FAKE_HEX), "hex")

    def test_dotted_key_is_real(self):
        self.assertEqual(ss.classify_line("coop.secret = %s" % FAKE_HEX), "hex")

    def test_quoted_value_is_real(self):
        self.assertEqual(ss.classify_line('secret = "%s"' % FAKE_HEX), "hex")

    def test_colon_separator_is_real(self):
        self.assertEqual(ss.classify_line("secret: %s" % FAKE_HEX), "hex")

    def test_trailing_comment_stripped(self):
        self.assertEqual(ss.classify_line("secret = %s   # generated" % FAKE_HEX), "hex")

    def test_uppercase_hex_is_real(self):
        self.assertEqual(ss.classify_line("SECRET = %s" % FAKE_HEX.upper()), "hex")

    def test_non_hex_long_token_is_real(self):
        self.assertEqual(ss.classify_line("secret = %s" % FAKE_TOKEN), "token")

    def test_placeholder_angle_hex_is_clean(self):
        self.assertIsNone(ss.classify_line("coop.secret = <hex>"))

    def test_placeholder_redacted_is_clean(self):
        self.assertIsNone(ss.classify_line("secret = <redacted>"))

    def test_placeholder_bare_redacted_is_clean(self):
        self.assertIsNone(ss.classify_line("secret = REDACTED"))

    def test_empty_value_is_clean(self):
        self.assertIsNone(ss.classify_line("secret = "))

    def test_short_value_is_clean(self):
        self.assertIsNone(ss.classify_line("secret = abc123"))

    def test_non_secret_key_is_clean(self):
        self.assertIsNone(ss.classify_line("heartbeatMs = 1000"))

    def test_unrelated_long_hex_is_clean(self):
        self.assertIsNone(ss.classify_line("sha256 = %s" % FAKE_HEX))

    def test_secret_mentioned_in_prose_is_clean(self):
        self.assertIsNone(ss.classify_line("never reuse the secret from net.cfg"))


class ScanTextTests(unittest.TestCase):
    def test_line_numbers(self):
        text = "enabled = false\nsecret = %s\nrole = host\n" % FAKE_HEX
        self.assertEqual(ss.scan_text(text), [(2, "hex")])

    def test_multiple_hits(self):
        text = "secret = %s\nsecret = %s\n" % (FAKE_HEX, FAKE_TOKEN)
        self.assertEqual(ss.scan_text(text), [(1, "hex"), (2, "token")])

    def test_clean_text(self):
        self.assertEqual(ss.scan_text("enabled = false\nsecret = <hex>\n"), [])


class OutputTests(unittest.TestCase):
    def test_format_never_includes_value(self):
        rendered = "\n".join(
            ss.format_findings(
                [{"rev": "HEAD", "commit": None, "path": "a/net.cfg", "line": 5, "kind": "hex"}]
            )
        )
        self.assertIn("a/net.cfg:5", rendered)
        self.assertNotIn(FAKE_HEX, rendered)

    def test_range_output_includes_commit(self):
        rendered = "\n".join(
            ss.format_findings(
                [
                    {
                        "rev": None,
                        "commit": "d81c86d0000ffff",
                        "path": "e/net.cfg",
                        "line": 5,
                        "kind": "hex",
                    }
                ]
            )
        )
        self.assertIn("e/net.cfg:5", rendered)
        self.assertIn("d81c86d0000f", rendered)
        self.assertNotIn(FAKE_HEX, rendered)


class TreeScanTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.repo = self.tmp.name
        os.makedirs(os.path.join(self.repo, "evidence", "X"), exist_ok=True)
        with open(os.path.join(self.repo, "evidence", "X", "net.cfg"), "w", encoding="utf-8") as fh:
            fh.write("enabled = false\nsecret = %s\n" % FAKE_HEX)
        with open(os.path.join(self.repo, "evidence", "X", "clean.md"), "w", encoding="utf-8") as fh:
            fh.write("secret = <hex>\n")
        self.patches = [
            mock.patch.object(ss, "REPO", self.repo),
            mock.patch.object(
                ss,
                "_git",
                side_effect=self._fake_git,
            ),
        ]
        for p in self.patches:
            p.start()
            self.addCleanup(p.stop)

    @staticmethod
    def _fake_git(*args):
        if args[:2] == ("ls-tree", "-r"):
            return b"evidence/X/net.cfg\nevidence/X/clean.md\n"
        raise RuntimeError("unexpected git call: %s" % (args,))

    def test_scan_tree_finds_only_real(self):
        findings = ss.scan_tree("HEAD")
        self.assertEqual(len(findings), 1)
        self.assertEqual(findings[0]["path"], "evidence/X/net.cfg")
        self.assertEqual(findings[0]["line"], 2)
        self.assertNotIn(FAKE_HEX, repr(findings))

    def test_main_exit_code_is_one_when_found(self):
        out = io.StringIO()
        err = io.StringIO()
        with redirect_stdout(out), redirect_stderr(err):
            code = ss.main(["--tree"])
        self.assertEqual(code, 1)
        self.assertNotIn(FAKE_HEX, out.getvalue())
        self.assertIn("1 finding(s)", out.getvalue())

    def test_main_exit_code_zero_when_clean(self):
        for name in ("net.cfg",):
            path = os.path.join(self.repo, "evidence", "X", name)
            with open(path, "w", encoding="utf-8") as fh:
                fh.write("enabled = false\nsecret = <redacted>\n")
        out = io.StringIO()
        with redirect_stdout(out):
            code = ss.main(["--tree"])
        self.assertEqual(code, 0)
        self.assertIn("0 finding(s)", out.getvalue())

    def test_main_exit_code_two_on_git_error(self):
        with mock.patch.object(ss, "_git", side_effect=RuntimeError("boom")):
            err = io.StringIO()
            with redirect_stdout(io.StringIO()), redirect_stderr(err):
                code = ss.main(["--tree"])
        self.assertEqual(code, 2)

    def test_main_default_mode_is_tree(self):
        out = io.StringIO()
        with redirect_stdout(out):
            code = ss.main([])
        self.assertEqual(code, 1)
        self.assertIn("evidence/X/net.cfg:2", out.getvalue())


class RangeScanTests(unittest.TestCase):
    def test_scan_range_uses_blobs(self):
        blobs = {
            ("rev-list", "origin/master..HEAD"): b"aaa111\nbbb222\n",
            ("diff-tree", "--no-commit-id", "--name-only", "-r", "--root", "aaa111"): b"evidence/X/net.cfg\n",
            ("diff-tree", "--no-commit-id", "--name-only", "-r", "--root", "bbb222"): b"evidence/X/gone.cfg\n",
            ("show", "aaa111:evidence/X/net.cfg"): ("secret = %s\n" % FAKE_HEX).encode(),
        }

        def fake_git(*args):
            if args in blobs:
                return blobs[args]
            raise RuntimeError("unexpected: %s" % (args,))

        with mock.patch.object(ss, "_git", side_effect=fake_git):
            findings = ss.scan_range("origin/master..HEAD")

        self.assertEqual(len(findings), 1)
        self.assertEqual(findings[0]["commit"], "aaa111")
        self.assertEqual(findings[0]["path"], "evidence/X/net.cfg")
        self.assertNotIn(FAKE_HEX, repr(findings))

    def test_binary_blobs_are_skipped(self):
        def fake_git(*args):
            if args[:1] == ("rev-list",):
                return b"ccc333\n"
            if args[:1] == ("diff-tree",):
                return b"dump.cs\n"
            return b"secret = " + FAKE_HEX.encode() + b"\x00\x01binary"

        with mock.patch.object(ss, "_git", side_effect=fake_git):
            self.assertEqual(ss.scan_range("origin/master..HEAD"), [])


if __name__ == "__main__":
    unittest.main()
