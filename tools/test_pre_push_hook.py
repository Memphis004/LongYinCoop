#!/usr/bin/env python3
"""End-to-end tests for tools/githooks/pre-push (offline, throwaway remotes).

The hook is exercised twice:

  * through a real ``git push`` into a temporary **bare repo** (that repo is the
    only "remote" involved; the real origin is never touched), and
  * by invoking the hook script directly with crafted stdin, which is the only
    way to simulate "python is not installed" and "scanner is broken".

Synthetic secret values are generated per run with ``secrets.token_hex`` and
are never written into the repository under test's history outside the temp
directory, and never printed.

Run:  python tools/test_pre_push_hook.py -v
"""
import os
import random
import shutil
import subprocess
import sys
import tempfile
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
HOOK_SRC = os.path.join(HERE, "githooks", "pre-push")
SCANNER_SRC = os.path.join(HERE, "scan_secrets.py")
ZERO = "0" * 40

GIT = shutil.which("git") or "git"


def find_sh() -> str | None:
    cand = shutil.which("sh")
    if cand:
        return cand
    for path in (
        r"C:\Program Files\Git\bin\sh.exe",
        r"C:\Program Files\Git\usr\bin\sh.exe",
    ):
        if os.path.isfile(path):
            return path
    return None


SH = find_sh()


def synthetic_secret() -> str:
    """Random 32-hex value - a shape the scanner flags, not a real credential."""
    return "%032x" % random.getrandbits(128)


class HookTestBase(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = self.tmp.name
        self.remote = os.path.join(self.root, "remote.git")
        self.work = os.path.join(self.root, "work")
        self._git(None, "init", "--bare", "-q", self.remote)
        self._git(self.remote, "config", "receive.denyDeleteCurrent", "ignore")
        self._git(None, "init", "-q", self.work)
        self._git(self.work, "config", "user.email", "t@example.invalid")
        self._git(self.work, "config", "user.name", "Test")
        self._git(self.work, "config", "commit.gpgsign", "false")
        self._git(self.work, "checkout", "-q", "-b", "master")
        self._git(self.work, "remote", "add", "origin", self.remote)
        # ship the hook + scanner inside the repo, exactly like the real layout
        os.makedirs(os.path.join(self.work, "tools", "githooks"), exist_ok=True)
        shutil.copy(HOOK_SRC, os.path.join(self.work, "tools", "githooks", "pre-push"))
        shutil.copy(SCANNER_SRC, os.path.join(self.work, "tools", "scan_secrets.py"))
        self._git(self.work, "config", "core.hooksPath", "tools/githooks")
        self.write("README.md", "clean\n")
        self.commit("init")

    # -- helpers -----------------------------------------------------------
    def _git(self, cwd, *args, check=False):
        proc = subprocess.run([GIT, *args], cwd=cwd, capture_output=True, text=True)
        if check and proc.returncode != 0:
            raise AssertionError("git %s failed: %s" % (args, proc.stderr))
        return proc

    def write(self, rel, text):
        path = os.path.join(self.work, rel)
        os.makedirs(os.path.dirname(path) or self.work, exist_ok=True)
        with open(path, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text)

    def commit(self, message):
        self._git(self.work, "add", "-A", check=True)
        self._git(self.work, "commit", "-q", "-m", message, check=True)

    def push(self, *args, env=None):
        full_env = os.environ.copy()
        if env:
            full_env.update(env)
        return subprocess.run(
            [GIT, "push", *args], cwd=self.work, capture_output=True, text=True, env=full_env
        )

    def remote_refs(self):
        out = self._git(self.remote, "for-each-ref", "--format=%(refname)").stdout
        return [line for line in out.splitlines() if line.strip()]

    def run_hook_direct(self, stdin_text, cwd=None, env_extra=None, path_override=None):
        assert SH, "no sh available"
        env = os.environ.copy()
        env.pop("PYTHON_CMD", None)
        if path_override is not None:
            env["PATH"] = path_override
            env.pop("PYTHON_CMD", None)
        if env_extra:
            env.update(env_extra)
        hook = os.path.join(self.work, "tools", "githooks", "pre-push")
        return subprocess.run(
            [SH, hook],
            cwd=cwd or self.work,
            input=stdin_text,
            capture_output=True,
            text=True,
            env=env,
        )


class CleanPushTests(HookTestBase):
    def test_clean_push_passes(self):
        self.write("docs/notes.md", "secret = <redacted>\ntoken = <hex>\n")
        self.commit("clean docs")
        proc = self.push("origin", "master")
        self.assertEqual(proc.returncode, 0, proc.stderr)
        combined = proc.stdout + proc.stderr
        self.assertIn("secret scan passed", combined)
        self.assertIn("refs/heads/master", self.remote_refs())

    def test_clean_push_with_new_branch_passes(self):
        self._git(self.work, "checkout", "-q", "-b", "feature", check=True)
        self.write("feature.txt", "hello\n")
        self.commit("feature")
        proc = self.push("origin", "feature")
        self.assertEqual(proc.returncode, 0, proc.stderr)
        self.assertIn("new ref", proc.stdout + proc.stderr)
        self.assertIn("refs/heads/feature", self.remote_refs())


class BlockedPushTests(HookTestBase):
    def test_secret_push_is_blocked(self):
        value = synthetic_secret()
        self.write("evidence/net.cfg", "enabled = false\nsecret = %s\n" % value)
        self.commit("add config with secret")
        proc = self.push("origin", "master")
        self.assertNotEqual(proc.returncode, 0, "push should have been blocked")
        combined = proc.stdout + proc.stderr
        self.assertIn("BLOCKED", combined)
        self.assertIn("evidence/net.cfg", combined)
        self.assertNotIn(value, combined)  # never leak the value
        self.assertEqual(self.remote_refs(), [])
        # the local commit still exists; the remote did not move
        head = self._git(self.work, "rev-parse", "HEAD").stdout.strip()
        self.assertTrue(head)

    def test_new_branch_with_secret_is_blocked(self):
        self._git(self.work, "checkout", "-q", "-b", "leaky", check=True)
        value = synthetic_secret()
        self.write("leak.txt", "api_key = %s\n" % value)
        self.commit("leak")
        proc = self.push("origin", "leaky")
        self.assertNotEqual(proc.returncode, 0)
        combined = proc.stdout + proc.stderr
        self.assertIn("new ref", combined)
        self.assertIn("BLOCKED", combined)
        self.assertNotIn(value, combined)
        self.assertNotIn("refs/heads/leaky", self.remote_refs())

    def test_old_commit_already_on_remote_is_not_rechecked(self):
        # push a clean commit, then a secret, then push: only the delta is scanned
        value = synthetic_secret()
        self.write("a.txt", "clean\n")
        self.commit("clean")
        self.assertEqual(self.push("origin", "master").returncode, 0)
        self.write("b.txt", "secret = %s\n" % value)
        self.commit("secret")
        proc = self.push("origin", "master")
        self.assertNotEqual(proc.returncode, 0)
        self.assertIn("b.txt", proc.stdout + proc.stderr)


class RefLifecycleTests(HookTestBase):
    def test_delete_ref_is_skipped(self):
        self.assertEqual(self.push("origin", "master").returncode, 0)
        proc = self.push("origin", "--delete", "master")
        self.assertEqual(proc.returncode, 0, proc.stderr)
        self.assertNotIn("refs/heads/master", self.remote_refs())

    def test_delete_ref_message(self):
        self.assertEqual(self.push("origin", "master").returncode, 0)
        remote_sha = self._git(self.work, "rev-parse", "HEAD").stdout.strip()
        proc = self.run_hook_direct(
            "(delete) %s refs/heads/master %s\n" % (ZERO, remote_sha)
        )
        self.assertEqual(proc.returncode, 0, proc.stderr)
        self.assertIn("deleted - nothing to scan", proc.stdout)


class FailClosedTests(HookTestBase):
    def test_no_python_blocks(self):
        if not SH:
            self.skipTest("sh not available")
        # PATH containing git's own directory only: git works, python does not
        git_bin = os.path.dirname(GIT)
        proc = self.run_hook_direct(
            "refs/heads/master %s refs/heads/master %s\n"
            % (self._git(self.work, "rev-parse", "HEAD").stdout.strip(), ZERO),
            path_override=git_bin,
        )
        self.assertEqual(proc.returncode, 1)
        self.assertIn("no python interpreter found", proc.stderr)

    def test_missing_scanner_blocks(self):
        if not SH:
            self.skipTest("sh not available")
        os.remove(os.path.join(self.work, "tools", "scan_secrets.py"))
        proc = self.run_hook_direct("refs/heads/master %s refs/heads/master %s\n" % ("a" * 40, ZERO))
        self.assertEqual(proc.returncode, 1)
        self.assertIn("scanner missing", proc.stderr)

    def test_broken_scanner_blocks(self):
        if not SH:
            self.skipTest("sh not available")
        with open(os.path.join(self.work, "tools", "scan_secrets.py"), "w", encoding="utf-8") as fh:
            fh.write("import sys; sys.exit(2)\n")
        proc = self.run_hook_direct("refs/heads/master %s refs/heads/master %s\n" % ("b" * 40, ZERO))
        self.assertEqual(proc.returncode, 1)
        self.assertIn("BLOCKED", proc.stderr)

    def test_no_stdin_input_passes(self):
        if not SH:
            self.skipTest("sh not available")
        proc = self.run_hook_direct("")
        self.assertEqual(proc.returncode, 0, proc.stderr)


class HookFileTests(unittest.TestCase):
    def test_hook_uses_lf_endings(self):
        with open(HOOK_SRC, "rb") as fh:
            data = fh.read()
        self.assertNotIn(b"\r\n", data, "hook must be LF-only or sh will fail")
        self.assertTrue(data.startswith(b"#!/bin/sh"))

    def test_gitattributes_pins_hook_eol(self):
        path = os.path.join(REPO, ".gitattributes")
        self.assertTrue(os.path.isfile(path), ".gitattributes must exist")
        with open(path, encoding="utf-8") as fh:
            text = fh.read()
        self.assertIn("tools/githooks/* text eol=lf", text)

    def test_hook_never_uses_no_verify(self):
        with open(HOOK_SRC, encoding="utf-8") as fh:
            text = fh.read()
        # the hook must never re-enter a push, and never bypass a hook itself
        self.assertNotIn("git push", text)
        self.assertNotIn("push --no-verify", text)


if __name__ == "__main__":
    unittest.main()
