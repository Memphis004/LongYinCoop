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
FAKE_MIXED = "AbCdEf0123456789xyzXYZ+/="


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
        self.assertEqual(ss.classify_line("secret = %s" % FAKE_MIXED), "token")

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
        text = "secret = %s\nsecret = %s\n" % (FAKE_HEX, FAKE_MIXED)
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
            # key is split so this scanner does not flag its own test source
            return b"sec" + b"ret = " + FAKE_HEX.encode() + b"\x00\x01binary"

        with mock.patch.object(ss, "_git", side_effect=fake_git):
            self.assertEqual(ss.scan_range("origin/master..HEAD"), [])


class ClassifyV2Tests(unittest.TestCase):
    """v2 pattern set: new detections and their false-positive guards."""

    # ---- detections ------------------------------------------------------
    def test_quoted_short_secret_is_real(self):
        self.assertEqual(ss.classify_line_v2('secret = "s3cr3t-pw"'), "quoted")

    def test_quoted_api_key_is_real(self):
        self.assertEqual(ss.classify_line_v2('  "api_key": "abcd1234wxyz"'), "quoted")

    def test_base64_token_is_real(self):
        value = "QWxhZGRpbjpvcGVuIHNlc2FtZQ"  # 26 chars, base64 alphabet
        self.assertEqual(ss.classify_line_v2("token = %s" % value), "base64")

    def test_hex_secret_still_labelled_hex(self):
        self.assertEqual(ss.classify_line_v2("secret = %s" % FAKE_HEX), "hex")

    def test_generic_token_key_is_real(self):
        self.assertEqual(ss.classify_line_v2("token = AbCd-1234_efgh-99"), "token")

    def test_private_key_header_is_real(self):
        pem = "-----BEGIN " + "RSA PRIVATE KEY-----"
        self.assertEqual(ss.classify_line_v2(pem), "private-key")

    def test_openssh_private_key_header_is_real(self):
        pem = "-----BEGIN " + "OPENSSH PRIVATE KEY-----"
        self.assertEqual(ss.classify_line_v2(pem), "private-key")

    def test_github_pat_classic_is_real(self):
        fake = "gh" + "p_" + "A1b2C3d4E5f6G7h8I9j0K1l2M3n4O5p6Q7r8"
        self.assertEqual(ss.classify_line_v2("token: %s" % fake), "github-token")

    def test_github_pat_fine_grained_is_real(self):
        fake = "github_" + "pat_" + "11ABCDEFG0123456789_abcdef"
        self.assertEqual(ss.classify_line_v2("use %s now" % fake), "github-token")

    def test_aws_access_key_id_is_real(self):
        fake = "AK" + "IA" + "IOSFODNN7EXAMPLE"
        self.assertEqual(ss.classify_line_v2("aws_access_key_id = %s" % fake), "aws-key-id")

    def test_slack_token_is_real(self):
        fake = "xo" + "xb-" + "1234567890-abcdefghijkl"
        self.assertEqual(ss.classify_line_v2(fake), "slack-token")

    def test_v2_scantext_reports_lines(self):
        text = 'token = "abcdefgh1234"\nnote = hi\nkey: %s\n' % FAKE_HEX
        self.assertEqual(ss.scan_text_with(text, "v2"), [(1, "quoted"), (3, "hex")])

    def test_v2_default_scanner_is_v1(self):
        # behaviour of the legacy entry point must not change
        self.assertEqual(ss.scan_text_with('secret = "s3cr3t-pw"', "v1"), [])

    # ---- false positives that must stay clean ----------------------------
    def test_placeholder_angle_bracket_clean(self):
        for ph in ("<hex>", "<redacted>", "<secret>", "<token>", "<key>"):
            self.assertIsNone(ss.classify_line_v2("token = %s" % ph))

    def test_repeated_x_placeholder_clean(self):
        self.assertIsNone(ss.classify_line_v2("secret = xxxxxxxxxxxxxxxxxxxxxxxx"))

    def test_bare_redacted_clean(self):
        self.assertIsNone(ss.classify_line_v2('secret = "REDACTED"'))

    def test_env_var_reference_clean(self):
        self.assertIsNone(ss.classify_line_v2("secret = ${COOP_SECRET}"))
        self.assertIsNone(ss.classify_line_v2("token = $GITHUB_TOKEN"))

    def test_keys_plural_is_benign(self):
        self.assertIsNone(ss.classify_line_v2('keys = "abcdefgh1234"'))

    def test_keyboard_keyword_benign(self):
        self.assertIsNone(ss.classify_line_v2('keyboard = "abcdefgh1234"'))
        self.assertIsNone(ss.classify_line_v2('keyword = "abcdefgh1234"'))
        self.assertIsNone(ss.classify_line_v2('monkey = "abcdefgh1234"'))

    def test_short_quoted_value_clean(self):
        self.assertIsNone(ss.classify_line_v2('secret = "short"'))

    def test_prose_without_assignment_clean(self):
        self.assertIsNone(ss.classify_line_v2("the token is read from net.cfg at startup"))
        self.assertIsNone(ss.classify_line_v2("note: the token is expired"))

    def test_ordinary_numbers_and_flags_clean(self):
        self.assertIsNone(ss.classify_line_v2("heartbeatMs = 1000"))
        self.assertIsNone(ss.classify_line_v2("enabled = false"))
        self.assertIsNone(ss.classify_line_v2("secretEnabled = true"))

    def test_sha_hex_not_a_key(self):
        self.assertIsNone(ss.classify_line_v2("sha256 = %s" % FAKE_HEX))

    def test_provider_prefixes_in_prose_clean(self):
        self.assertIsNone(ss.classify_line_v2("rotate any ghp_ / AKIA / xoxb- tokens"))
        self.assertIsNone(ss.classify_line_v2("never commit an AKIA.... key id"))

    def test_masked_preview_with_ellipsis_clean(self):
        self.assertIsNone(ss.classify_line_v2('coop.secret = "abc123...x9y8"'))
        self.assertIsNone(ss.classify_line_v2("token = abcd\u2026wxyz"))

    def test_snake_case_identifier_with_pat_prefix_clean(self):
        self.assertIsNone(ss.classify_line_v2("def test_github_pat_fine_grained_is_real(self):"))
        self.assertIsNone(ss.classify_line_v2("github_pat_placeholder_documentation"))

    def test_scanner_sources_are_self_clean(self):
        # the scanner and its tests must not trip their own v2 patterns
        for name in ("scan_secrets.py", "test_scan_secrets.py"):
            path = os.path.join(HERE, name)
            with open(path, encoding="utf-8") as fh:
                found = ss.scan_text_with(fh.read(), "v2")
            self.assertEqual(found, [], "%s has %d finding(s)" % (name, len(found)))


class AllHistoryTests(unittest.TestCase):
    @staticmethod
    def _raw(blob_sha: str, path: str, status: str = "A") -> bytes:
        return (":000000 100644 0000000 %s %s\x00%s\x00" % (blob_sha, status, path)).encode()

    def test_scan_all_history_uses_rev_list_all(self):
        calls = []
        secret_text = ("secret = %s\n" % FAKE_HEX).encode()

        def fake_git(*args):
            calls.append(args)
            if args == ("rev-list", "--all"):
                return b"root1\nhead2\n"
            if args[:1] == ("diff-tree",) and args[-1] == "root1":
                return self._raw("1111111", "a/net.cfg")
            if args[:1] == ("diff-tree",) and args[-1] == "head2":
                return self._raw("2222222", "b/gone.cfg", status="D")
            raise RuntimeError("unexpected: %s" % (args,))

        with mock.patch.object(ss, "_git", side_effect=fake_git), mock.patch.object(
            ss, "_read_blobs", side_effect=lambda shas: {"1111111": secret_text}
        ):
            findings = ss.scan_all_history("v1")

        self.assertIn(("rev-list", "--all"), calls)
        self.assertEqual(len(findings), 1)
        self.assertEqual(findings[0]["commit"], "root1")
        self.assertEqual(findings[0]["path"], "a/net.cfg")
        self.assertNotIn(FAKE_HEX, repr(findings))

    def test_scan_all_history_is_clean_when_only_placeholders(self):
        def fake_git(*args):
            if args == ("rev-list", "--all"):
                return b"root1\n"
            return self._raw("3333333", "docs/net.cfg")

        with mock.patch.object(ss, "_git", side_effect=fake_git), mock.patch.object(
            ss, "_read_blobs", side_effect=lambda shas: {"3333333": b"secret = <redacted>\n"}
        ):
            self.assertEqual(ss.scan_all_history("v2"), [])

    def test_changed_blobs_parses_raw_z_output(self):
        raw = (
            b":000000 100644 0000000 aaa111 A\x00a/net.cfg\x00"
            b":100644 100644 aaa111 bbb222 M\x00b/two.cfg\x00"
            b":100644 100644 bbb222 0000000 D\x00c/deleted.cfg\x00"
        )
        with mock.patch.object(ss, "_git", return_value=raw):
            got = ss._changed_blobs("deadbeef")
        self.assertEqual(got, [("a/net.cfg", "aaa111"), ("b/two.cfg", "bbb222")])

    def test_main_all_history_flag(self):
        def fake_git(*args):
            if args == ("rev-list", "--all"):
                return b"root1\n"
            return self._raw("4444444", "x/net.cfg")

        out = io.StringIO()
        with mock.patch.object(ss, "_git", side_effect=fake_git), mock.patch.object(
            ss, "_read_blobs", side_effect=lambda shas: {"4444444": ("token = %s\n" % FAKE_HEX).encode()}
        ), redirect_stdout(out):
            code = ss.main(["--all-history", "--patterns", "v2"])
        self.assertEqual(code, 1)
        self.assertIn("x/net.cfg:1", out.getvalue())
        self.assertNotIn(FAKE_HEX, out.getvalue())

    def test_main_patterns_choice_rejects_unknown(self):
        with self.assertRaises(SystemExit), redirect_stderr(io.StringIO()):
            ss.main(["--patterns", "v3"])


if __name__ == "__main__":
    unittest.main()
