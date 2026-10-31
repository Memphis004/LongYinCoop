#!/usr/bin/env python3
"""Read-only secret scanner for the LongYinCoop repo.

Purpose
-------
Find *real* secret values (e.g. a generated ``secret = <hex>`` line copied into
an evidence file) without ever printing the value itself.  Only locations are
reported: ``path:line``, commit hash and counts.

What counts as a real secret
----------------------------
A line that assigns a token to a ``secret`` key (case-insensitive, so
``secret`` / ``coop.secret`` / ``net.secret`` all match) where the token is

  * >= 16 hex characters, or
  * >= 16 characters from ``[A-Za-z0-9+/=_-]`` (non-hex long token)

and is NOT one of the accepted placeholders (``<hex>``, ``<redacted>``,
``<secret>``, ``REDACTED``, ``CHANGEME``, ``TODO``, ``x`` repeated, ``...``,
empty).  Placeholders are how the repo documents the field, so they must stay
clean.

Modes
-----
``--tree`` (default)
    Scan every file tracked at ``HEAD`` (``git ls-tree -r HEAD``).  Content is
    read from the worktree when present, otherwise from the blob at ``HEAD``.

``--range <rev-range>``
    Scan every blob touched by every commit in the range, e.g.
    ``--range origin/master..HEAD``.  For each commit only the paths it touched
    are scanned (a full-tree scan per commit would be O(commits x files) and is
    unnecessary: untouched content is already covered by ``--tree``).

Exit codes
----------
0  clean
1  real secret(s) found
2  usage / git error (fail-closed: treated as "not clean" by callers)

Usage
-----
    python tools/scan_secrets.py
    python tools/scan_secrets.py --range origin/master..HEAD
    python tools/scan_secrets.py --tree --range origin/master..HEAD --json
"""
from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)

# secret key assignment; value is captured for classification only, never printed
SECRET_LINE_RE = re.compile(
    r"(?i)(?:^|[^A-Za-z0-9_])[A-Za-z0-9_.]*secret[A-Za-z0-9_]*\s*[=:]\s*(.+?)\s*$"
)
HEX_TOKEN_RE = re.compile(r"^[0-9a-fA-F]{16,}$")
GENERIC_TOKEN_RE = re.compile(r"^[A-Za-z0-9+/=_-]{16,}$")

PLACEHOLDERS = {
    "<hex>",
    "<redacted>",
    "<secret>",
    "<redacted-hex>",
    "redacted",
    "changeme",
    "change-me",
    "todo",
    "none",
    "null",
    "...",
    "x",
}

MIN_SECRET_LEN = 16


def _strip_wrapping(value: str) -> str:
    v = value.strip()
    # drop trailing comment
    for marker in ("  #", "\t#", " #"):
        idx = v.find(marker)
        if idx > 0:
            v = v[:idx].strip()
    # strip matching quotes
    for q in ('"', "'"):
        if len(v) >= 2 and v.startswith(q) and v.endswith(q):
            v = v[1:-1].strip()
    return v


def classify_line(line: str) -> str | None:
    """Return None when the line is not a real secret, else a reason tag.

    Never returns or logs the value itself.
    """
    m = SECRET_LINE_RE.search(line)
    if not m:
        return None
    value = _strip_wrapping(m.group(1))
    if not value:
        return None
    low = value.lower()
    if low in PLACEHOLDERS:
        return None
    if len(value) < MIN_SECRET_LEN:
        return None
    if set(low) <= {"x"}:
        return None
    if HEX_TOKEN_RE.match(value):
        return "hex"
    if GENERIC_TOKEN_RE.match(value):
        return "token"
    return None


def scan_text(text: str) -> list[tuple[int, str]]:
    """Return [(lineno, reason)] for real secrets in `text` (no values)."""
    hits: list[tuple[int, str]] = []
    for idx, line in enumerate(text.splitlines(), start=1):
        reason = classify_line(line)
        if reason:
            hits.append((idx, reason))
    return hits


def _looks_binary(data: bytes) -> bool:
    return b"\x00" in data[:8192]


def _git(*args: str) -> bytes:
    proc = subprocess.run(
        ["git", *args],
        cwd=REPO,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if proc.returncode != 0:
        raise RuntimeError(
            "git %s failed: %s" % (" ".join(args), proc.stderr.decode("utf-8", "replace").strip())
        )
    return proc.stdout


def _decode(data: bytes) -> str:
    return data.decode("utf-8", "replace")


def scan_tree(rev: str = "HEAD") -> list[dict]:
    """Scan all files tracked at `rev`."""
    out = _git("ls-tree", "-r", "--name-only", rev)
    findings: list[dict] = []
    for path in _decode(out).splitlines():
        path = path.strip()
        if not path:
            continue
        data: bytes | None = None
        disk = os.path.join(REPO, path.replace("/", os.sep))
        if rev == "HEAD" and os.path.isfile(disk):
            try:
                with open(disk, "rb") as fh:
                    data = fh.read()
            except OSError:
                data = None
        if data is None:
            try:
                data = _git("show", "%s:%s" % (rev, path))
            except RuntimeError:
                continue
        if _looks_binary(data):
            continue
        for lineno, reason in scan_text(_decode(data)):
            findings.append(
                {"rev": rev, "commit": None, "path": path, "line": lineno, "kind": reason}
            )
    return findings


def scan_range(rev_range: str) -> list[dict]:
    """Scan every blob touched by commits in `rev_range`."""
    commits = _decode(_git("rev-list", rev_range)).split()
    findings: list[dict] = []
    for commit in commits:
        try:
            changed = _decode(
                _git("diff-tree", "--no-commit-id", "--name-only", "-r", "--root", commit)
            ).splitlines()
        except RuntimeError:
            continue
        for path in changed:
            path = path.strip()
            if not path:
                continue
            try:
                data = _git("show", "%s:%s" % (commit, path))
            except RuntimeError:
                continue  # deleted in this commit
            if _looks_binary(data):
                continue
            for lineno, reason in scan_text(_decode(data)):
                findings.append(
                    {"rev": None, "commit": commit, "path": path, "line": lineno, "kind": reason}
                )
    return findings


def format_findings(findings: list[dict]) -> list[str]:
    """Render findings without ever including the secret value."""
    lines = []
    for f in sorted(findings, key=lambda x: (x.get("path") or "", x.get("line") or 0)):
        where = "%s:%s" % (f["path"], f["line"])
        if f.get("commit"):
            lines.append("%s (commit %s, %s)" % (where, f["commit"][:12], f["kind"]))
        else:
            lines.append("%s (%s)" % (where, f["kind"]))
    return lines


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(
        description="Read-only secret scanner (never prints secret values)."
    )
    ap.add_argument("--tree", action="store_true", help="scan files tracked at HEAD (default)")
    ap.add_argument("--range", dest="rev_range", help="also scan commits in this rev range")
    ap.add_argument("--rev", default="HEAD", help="revision for --tree (default HEAD)")
    ap.add_argument("--json", action="store_true", help="machine-readable output")
    args = ap.parse_args(argv)

    if not args.tree and not args.rev_range:
        args.tree = True

    findings: list[dict] = []
    try:
        if args.tree:
            findings.extend(scan_tree(args.rev))
        if args.rev_range:
            findings.extend(scan_range(args.rev_range))
    except RuntimeError as exc:
        print("scan_secrets: ERROR %s" % exc, file=sys.stderr)
        return 2

    if args.json:
        print(json.dumps({"count": len(findings), "findings": findings}, indent=2))
    else:
        for line in format_findings(findings):
            print(line)
        print("scan_secrets: %d finding(s)" % len(findings))

    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
