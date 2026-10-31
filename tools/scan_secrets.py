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

Pattern sets
------------
``--patterns v1`` (default, legacy behaviour)
    Only ``*secret*`` keys.  Value must be >= 16 hex chars or >= 16 chars from
    ``[A-Za-z0-9+/=_-]``.

``--patterns v2``
    v1 (for ``secret``/``token``/``key`` keys) plus:
      * ``*secret*`` / ``*token*`` / ``*key*`` keys whose value is quoted, or a
        base64-ish token >= 24 chars;
      * ``-----BEGIN ... PRIVATE KEY-----`` headers;
      * GitHub tokens (``ghp_``, ``github_pat_``), AWS key ids (``AKIA...``),
        Slack tokens (``xox[baprs]-...``).

``--patterns all`` = v1 + v2 (v2 is a superset in practice, kept explicit).

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

``--all-history``
    Scan every blob touched by every commit reachable from *all* refs
    (``git rev-list --all``, roots included).  Slow: roughly
    O(commits x changed-paths x blob size) for the whole repo.

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
    "<token>",
    "<key>",
    "<redacted-hex>",
    "<redacted-token>",
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

# --- pattern set v2 -------------------------------------------------------
# key names that must never be treated as credential keys (they are ordinary
# code/data identifiers).  Checked as a substring of the matched key.
BENIGN_KEY_PARTS = (
    "keys",
    "keyboard",
    "keybind",
    "keycode",
    "keydown",
    "keyup",
    "keyword",
    "keymap",
    "keystore-path",
    "monkey",
    "hockey",
)

KEY_ASSIGN_RE = re.compile(
    r"(?i)(?:^|[^A-Za-z0-9_\-])"
    r"([A-Za-z0-9_.\-]*(?:secret|token|key)[A-Za-z0-9_]*)"
    r"[\"']?\s*[=:]\s*(.+?)\s*$"
)
BASE64_TOKEN_RE = re.compile(r"^[A-Za-z0-9+/]{24,}={0,2}$")
PRIVATE_KEY_RE = re.compile(r"-----BEGIN [A-Z0-9 ]*PRIVATE KEY-----")
GITHUB_TOKEN_RE = re.compile(r"(?:ghp_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,})")
AWS_KEY_ID_RE = re.compile(r"AKIA[0-9A-Z]{12,}")
SLACK_TOKEN_RE = re.compile(r"xox[baprs]-[A-Za-z0-9-]{10,}")


def _has_digit(text: str) -> bool:
    return any(ch.isdigit() for ch in text)


def _looks_like_provider_token(text: str) -> bool:
    """Provider tokens carry digits; snake_case identifiers that merely embed a
    prefix (e.g. a test name starting with ``github_pat_``) do not."""
    return _has_digit(text)


def _looks_like_github_token(text: str) -> bool:
    body = text.split("_", 2)[-1] if text.startswith("github_pat_") else text[4:]
    return _has_digit(body) and any(c.isupper() for c in body)

MIN_QUOTED_LEN = 8
MIN_BASE64_LEN = 24


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


def _is_placeholder(value: str) -> bool:
    low = value.lower()
    if low in PLACEHOLDERS:
        return True
    if len(low) >= 3 and set(low) <= {"x", ".", "*"}:
        return True
    # masked previews (``abc123...x9y8`` / ``abcd…wxyz``): real hex/base64 secrets
    # never contain an ellipsis, so this cannot hide one.
    if "..." in value or "\u2026" in value:
        return True
    return False


def classify_line_v2(line: str) -> str | None:
    """v2 classifier: superset of `classify_line`, still never returns values.

    Adds: any credential-ish key (secret/token/key) with a quoted value or a
    base64-ish value >= 24 chars, plus well-known provider token shapes and
    PEM private-key headers.  Placeholders stay clean.
    """
    # 1. unconditional shapes (no key needed)
    if PRIVATE_KEY_RE.search(line):
        return "private-key"
    m = GITHUB_TOKEN_RE.search(line)
    if m and _looks_like_github_token(m.group(0)):
        return "github-token"
    m = AWS_KEY_ID_RE.search(line)
    if m and _looks_like_provider_token(m.group(0)):
        return "aws-key-id"
    m = SLACK_TOKEN_RE.search(line)
    if m and _looks_like_provider_token(m.group(0)):
        return "slack-token"

    # 2. key = value assignments
    m = KEY_ASSIGN_RE.search(line)
    if not m:
        return None
    key = m.group(1).lower()
    for part in BENIGN_KEY_PARTS:
        if part in key:
            return None
    raw = m.group(2).strip()
    quoted = len(raw) >= 2 and raw[0] in "\"'" and raw[-1] == raw[0]
    value = _strip_wrapping(raw)
    if not value:
        return None
    if _is_placeholder(value):
        return None
    if HEX_TOKEN_RE.match(value) and len(value) >= MIN_SECRET_LEN:
        return "hex"
    if BASE64_TOKEN_RE.match(value) and len(value) >= MIN_BASE64_LEN:
        return "base64"
    if GENERIC_TOKEN_RE.match(value) and len(value) >= MIN_SECRET_LEN:
        return "token"
    if quoted and len(value) >= MIN_QUOTED_LEN:
        return "quoted"
    return None


PATTERN_SETS = {"v1": classify_line, "v2": classify_line_v2, "all": classify_line_v2}


def classifier_for(patterns: str):
    try:
        return PATTERN_SETS[patterns]
    except KeyError:  # pragma: no cover - argparse restricts the value
        raise ValueError("unknown pattern set: %s" % patterns)


def scan_text_with(text: str, patterns: str = "v1") -> list[tuple[int, str]]:
    """Return [(lineno, reason)] using the requested pattern set."""
    classify = classifier_for(patterns)
    hits: list[tuple[int, str]] = []
    for idx, line in enumerate(text.splitlines(), start=1):
        reason = classify(line)
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


def scan_tree(rev: str = "HEAD", patterns: str = "v1") -> list[dict]:
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
        for lineno, reason in scan_text_with(_decode(data), patterns):
            findings.append(
                {"rev": rev, "commit": None, "path": path, "line": lineno, "kind": reason}
            )
    return findings


def scan_commits(commits: list[str], patterns: str = "v1") -> list[dict]:
    """Scan every blob touched by each commit in `commits`."""
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
            for lineno, reason in scan_text_with(_decode(data), patterns):
                findings.append(
                    {"rev": None, "commit": commit, "path": path, "line": lineno, "kind": reason}
                )
    return findings


def scan_range(rev_range: str, patterns: str = "v1") -> list[dict]:
    """Scan every blob touched by commits in `rev_range`.

    `rev_range` is passed to ``git rev-list``; it may contain several
    space-separated arguments (e.g. ``"<sha> --not --remotes"``), which the
    pre-push hook uses for brand-new refs.
    """
    commits = _decode(_git("rev-list", *rev_range.split())).split()
    return scan_commits(commits, patterns)


def _read_blobs(shas: list[str]) -> dict[str, bytes]:
    """Read many blobs through ``git cat-file --batch`` (chunked, deadlock-safe).

    ``subprocess.run(input=...)`` handles the read/write interleaving; writing
    every hash first and then reading deadlocks once the pipe buffer fills,
    which is exactly what happens on a full-history scan.
    """
    out: dict[str, bytes] = {}
    chunk = 200
    for start in range(0, len(shas), chunk):
        batch = shas[start : start + chunk]
        payload = ("\n".join(batch) + "\n").encode()
        proc = subprocess.run(
            ["git", "cat-file", "--batch"],
            cwd=REPO,
            input=payload,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
        if proc.returncode != 0:
            raise RuntimeError(
                "git cat-file --batch failed: %s"
                % proc.stderr.decode("utf-8", "replace").strip()
            )
        data = proc.stdout
        pos = 0
        for sha in batch:
            nl = data.find(b"\n", pos)
            if nl < 0:
                break
            header = data[pos:nl].split()
            pos = nl + 1
            if len(header) < 3 or header[-1] == b"missing":
                out[sha] = b""
                continue
            size = int(header[2])
            out[sha] = data[pos : pos + size]
            pos += size + 1  # skip the trailing newline git adds
    return out


def _changed_blobs(commit: str) -> list[tuple[str, str]]:
    """[(path, blob_sha)] touched by `commit` (raw diff, NUL separated)."""
    out = _git("diff-tree", "--no-commit-id", "-r", "--root", "--raw", "-z", commit)
    parts = out.split(b"\x00")
    entries: list[tuple[str, str]] = []
    idx = 0
    while idx < len(parts):
        meta = parts[idx]
        if not meta.startswith(b":"):
            idx += 1
            continue
        fields = meta[1:].split()
        blob_sha = fields[3].decode() if len(fields) >= 4 else ""
        path = parts[idx + 1].decode("utf-8", "replace") if idx + 1 < len(parts) else ""
        if blob_sha and set(blob_sha) != {"0"}:
            entries.append((path, blob_sha))
        idx += 2
    return entries


def scan_all_history(patterns: str = "v1") -> list[dict]:
    """Scan every blob touched by every commit reachable from all refs."""
    commits = _decode(_git("rev-list", "--all")).split()
    pending: list[tuple[str, str, str]] = []  # (commit, path, blob_sha)
    for commit in commits:
        try:
            for path, blob_sha in _changed_blobs(commit):
                pending.append((commit, path, blob_sha))
        except RuntimeError:
            continue
    unique = sorted({sha for _, _, sha in pending})
    blobs = _read_blobs(unique)
    cache: dict[str, list[tuple[int, str]]] = {}
    findings: list[dict] = []
    for commit, path, sha in pending:
        data = blobs.get(sha)
        if data is None or _looks_binary(data):
            continue
        if sha not in cache:
            cache[sha] = scan_text_with(_decode(data), patterns)
        for lineno, reason in cache[sha]:
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
    ap.add_argument(
        "--all-history",
        action="store_true",
        help="scan every commit reachable from all refs (roots included)",
    )
    ap.add_argument(
        "--patterns",
        choices=sorted(PATTERN_SETS),
        default="v1",
        help="pattern set: v1 (legacy default), v2 or all",
    )
    ap.add_argument("--rev", default="HEAD", help="revision for --tree (default HEAD)")
    ap.add_argument("--json", action="store_true", help="machine-readable output")
    args = ap.parse_args(argv)

    if not args.tree and not args.rev_range and not args.all_history:
        args.tree = True

    findings: list[dict] = []
    try:
        if args.tree:
            findings.extend(scan_tree(args.rev, args.patterns))
        if args.rev_range:
            findings.extend(scan_range(args.rev_range, args.patterns))
        if args.all_history:
            findings.extend(scan_all_history(args.patterns))
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
