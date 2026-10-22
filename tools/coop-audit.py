#!/usr/bin/env python3
"""
tools/coop-audit.py — sanitized, git-trackable audit copies of local test records

The tools under tools/ write `REBASELINE.json` next to a baseline with ABSOLUTE
paths (e.g. `"against": "C:\\Users\\<user>\\...\\savehash.real.before.txt"`).
Those files are generated evidence: they are never rewritten by this tool.
Instead this tool exports a deterministic, path-sanitized copy called
`AUDIT.json` in the same folder, and THAT is what git tracks.

Rules:
  - read-only on every input; the only writes are `AUDIT.json` files
  - deterministic output (no timestamps) so re-export is a no-op in git
  - no absolute path, drive letter or user name may survive sanitizing;
    if one does, export fails closed and nothing is written
  - source bytes are recorded by sha256 so the original can be verified

Usage:
  python tools/coop-audit.py export [--coop-dir DIR] [--dry-run] [--check]
  python tools/coop-audit.py verify [--coop-dir DIR]

  export   write/refresh AUDIT.json for every baselines/*/REBASELINE.json
  --check  report drift only (exit 1 if any AUDIT.json is missing or stale)
  verify   confirm no tracked audit record contains an absolute path
"""

import argparse
import hashlib
import json
import os
import re
import sys

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEFAULT_COOP_DIR = os.path.join(REPO_ROOT, "coop-test")
TOOL_NAME = "tools/coop-audit.py"
SOURCE_NAME = "REBASELINE.json"
TARGET_NAME = "AUDIT.json"

# fields copied verbatim into the audit record, in a fixed order
KEEP = ("ts", "reason", "files", "digest", "changed", "missing", "added",
        "volatile_only")

ABS_PATH = re.compile(r"[A-Za-z]:[\\/]")
HOME_MARKER = re.compile(r"[Uu]sers[\\/][^\\/\"]+")


class AuditError(Exception):
    pass


def sha256_file(path):
    h = hashlib.sha256()
    with open(path, "rb") as fh:
        for chunk in iter(lambda: fh.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def repo_relative(path):
    """Absolute Windows/POSIX path -> repo-relative POSIX path, or None."""
    if not isinstance(path, str):
        return None
    norm = path.replace("\\", "/")
    root = REPO_ROOT.replace("\\", "/")
    low_norm, low_root = norm.lower(), root.lower()
    if low_norm.startswith(low_root + "/"):
        return norm[len(root) + 1:]
    return None


def sanitize_reference(path):
    """Map the `against` field to a repo-relative path.

    Anything outside the repo is reduced to `<basename>` and explicitly
    flagged, never expanded.
    """
    rel = repo_relative(path)
    if rel:
        return rel, True
    base = str(path).replace("\\", "/").rstrip("/").split("/")[-1]
    return base, False


def build_record(src_path):
    with open(src_path, "r", encoding="utf-8-sig") as fh:
        raw = fh.read()
    try:
        src = json.loads(raw)
    except json.JSONDecodeError as exc:
        raise AuditError(f"{src_path}: REBASELINE.json is not valid JSON ({exc})")

    rec = {"audit_version": 1, "sanitized": True, "source": SOURCE_NAME}
    for key in KEEP:
        if key in src:
            rec[key] = src[key]

    if "against" in src:
        rel, inside = sanitize_reference(src["against"])
        rec["against"] = rel
        rec["against_scope"] = "repo-relative" if inside else "basename-only"

    rec["source_sha256"] = hashlib.sha256(raw.encode("utf-8")).hexdigest()
    rec["generated_by"] = TOOL_NAME

    blob = json.dumps(rec, indent=2, ensure_ascii=False)
    if ABS_PATH.search(blob) or HOME_MARKER.search(blob):
        raise AuditError(f"{src_path}: sanitizing failed, absolute path survived")
    return rec, blob + "\n"


def find_sources(coop_dir):
    out = []
    base_dir = os.path.join(coop_dir, "baselines")
    if not os.path.isdir(base_dir):
        raise AuditError(f"no baselines directory under {coop_dir}")
    for name in sorted(os.listdir(base_dir)):
        cand = os.path.join(base_dir, name, SOURCE_NAME)
        if os.path.isfile(cand):
            out.append(cand)
    return out


def cmd_export(coop_dir, dry_run, check):
    sources = find_sources(coop_dir)
    if not sources:
        raise AuditError("no REBASELINE.json found")
    written, stale, clean = [], [], []
    for src in sources:
        rec, blob = build_record(src)
        target = os.path.join(os.path.dirname(src), TARGET_NAME)
        old = None
        if os.path.isfile(target):
            with open(target, "r", encoding="utf-8") as fh:
                old = fh.read()
        rel_t = os.path.relpath(target, REPO_ROOT).replace("\\", "/")
        if old == blob:
            clean.append(rel_t)
            continue
        stale.append(rel_t)
        if check or dry_run:
            continue
        with open(target, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(blob)
        written.append(rel_t)
    mode = "check" if check else ("dry-run" if dry_run else "export")
    print(f"mode: {mode}")
    print(f"records: {len(sources)}  written: {len(written)}  "
          f"stale: {len(stale)}  up-to-date: {len(clean)}")
    for p in written:
        print(f"  WRITE {p}")
    for p in stale:
        if p not in written:
            print(f"  STALE {p}")
    for p in clean:
        print(f"  OK    {p}")
    if check and stale:
        print("FAIL: AUDIT.json missing or stale", file=sys.stderr)
        return 1
    if dry_run:
        print(f"DRY-RUN: would write {len(stale)} record(s), nothing written")
        return 0
    print("PASS: audit records match sanitized sources")
    return 0


def cmd_verify(coop_dir):
    bad = []
    count = 0
    base_dir = os.path.join(coop_dir, "baselines")
    for name in sorted(os.listdir(base_dir)):
        path = os.path.join(base_dir, name, TARGET_NAME)
        if not os.path.isfile(path):
            bad.append(f"missing: {path}")
            continue
        count += 1
        with open(path, "r", encoding="utf-8") as fh:
            text = fh.read()
        if ABS_PATH.search(text) or HOME_MARKER.search(text):
            bad.append(f"absolute path in {os.path.relpath(path, REPO_ROOT)}")
        try:
            json.loads(text)
        except json.JSONDecodeError:
            bad.append(f"invalid json: {os.path.relpath(path, REPO_ROOT)}")
    print(f"checked {count} audit record(s)")
    for b in bad:
        print(f"  BAD {b}")
    if bad:
        print("FAIL", file=sys.stderr)
        return 1
    print("PASS: no absolute path, drive letter or user name in audit records")
    return 0


def main(argv):
    ap = argparse.ArgumentParser(add_help=True)
    ap.add_argument("command", choices=("export", "verify"))
    ap.add_argument("--coop-dir", default=DEFAULT_COOP_DIR)
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--check", action="store_true")
    args = ap.parse_args(argv)
    coop = os.path.abspath(args.coop_dir)
    try:
        if args.command == "export":
            return cmd_export(coop, args.dry_run, args.check)
        return cmd_verify(coop)
    except AuditError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        print("FAIL (fail-closed)", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
