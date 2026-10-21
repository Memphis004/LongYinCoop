#!/usr/bin/env python3
"""preflight-launch guard tests (stdlib only, fully sandboxed).

Every case builds a throw-away `data/Save` + `coop/<label>` fixture under the
system temp dir and runs:

    python tools/save-split.py --data-dir <tmp>/data --coop-dir <tmp>/coop \
        preflight-launch [--label ...] [--dry-run]

The real game folder is never read or written by these tests.
"""

from __future__ import annotations

import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
TOOL = HERE / "save-split.py"
LABEL = "guard-test"
VOLATILE = {"PlayerprefData.dat", "steam_autocloud.vdf"}


def sha(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as fh:
        for chunk in iter(lambda: fh.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def hashes(root: Path) -> dict[str, str]:
    out = {}
    for dirpath, _d, files in os.walk(root):
        for name in files:
            p = Path(dirpath, name)
            out[p.relative_to(root).as_posix()] = sha(p)
    return out


def digest(tree: dict[str, str]) -> str:
    h = hashlib.sha256()
    for rel in sorted(tree):
        h.update(f"{rel}\0{tree[rel]}\n".encode())
    return h.hexdigest()


def write_hashfile(tree: dict[str, str], out: Path) -> None:
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text("".join(f"{tree[r]} *Save/{r}\n" for r in sorted(tree)), encoding="utf-8")


def build(root: Path, *, split: bool = True, extra_used: bool = False,
          two_aside: bool = False, manifest: bool = True,
          tamper_slot: bool = False, volatile_drift: bool = False,
          hidden_sentinel: bool = False, touch_real: bool = False,
          broken_manifest: bool = False) -> tuple[Path, Path]:
    data = root / "data"
    coop = root / "coop"
    save = data / "Save"
    save_slot = save / "SaveSlot0"
    save_slot.mkdir(parents=True)
    (save_slot / "Save").write_bytes(b"slot-payload")
    (save / "PlayerprefData.dat").write_bytes(b"pref")
    real_snapshot = hashes(save)

    aside_name = f"Save.real-{LABEL}"
    if split:
        aside = data / aside_name
        shutil.copytree(save, aside)
        if touch_real:
            (aside / "SaveSlot0" / "Save").write_bytes(b"touched-real")
        manifest_obj = {
            "label": LABEL,
            "created": "2026-01-01T00:00:00",
            "data_dir": str(data),
            "real": {"dir": aside_name, "files": len(real_snapshot),
                     "digest": digest(real_snapshot)},
            "copy": {"dir": str(coop / LABEL / "save"), "files": len(real_snapshot),
                     "digest": digest(real_snapshot)},
        }
        if two_aside:
            (data / f"Save.real-other").mkdir()
        if extra_used:
            (data / "Save.used-20260101-000000").mkdir()
        write_hashfile(real_snapshot, coop / LABEL / "savehash.copy.before.txt")
        if manifest:
            (coop / LABEL / "MANIFEST.json").write_text(
                "{not json" if broken_manifest else json.dumps(manifest_obj),
                encoding="utf-8")
        if tamper_slot:
            (save / "SaveSlot0" / "Save").write_bytes(b"tampered-on-copy")
        if volatile_drift:
            (save / "PlayerprefData.dat").write_bytes(b"pref-after-launch")
        if hidden_sentinel:
            (save / ".hidden-SaveSlot8").mkdir()
    return data, coop


def run(data: Path, coop: Path, *extra: str, env_exe: str | None = None) -> tuple[int, str]:
    cmd = [sys.executable, str(TOOL), "--data-dir", str(data), "--coop-dir", str(coop),
           "preflight-launch", *extra]
    env = dict(os.environ)
    if env_exe:
        env["LONGYIN_GAME_EXE"] = env_exe
    res = subprocess.run(cmd, capture_output=True, text=True, env=env, timeout=180)
    return res.returncode, res.stdout + res.stderr


CASES = [
    # name, kwargs for build(), extra argv, env exe, expected exit, expected marker
    ("copy-mounted", {}, (), None, 0, "PREFLIGHT-LAUNCH READY"),
    ("restored-real-save", {"split": False}, (), None, 1, "REFUSED"),
    ("used-leftover", {"extra_used": True}, (), None, 1, "REFUSED"),
    ("two-aside", {"two_aside": True}, (), None, 1, "REFUSED"),
    ("tampered-copy-slot", {"tamper_slot": True}, (), None, 1, "REFUSED"),
    ("volatile-drift-allowed", {"volatile_drift": True}, (), None, 0, "PREFLIGHT-LAUNCH READY"),
    ("hidden-sentinel", {"hidden_sentinel": True}, (), None, 1, "REFUSED"),
    ("manifest-missing", {"manifest": False}, (), None, 1, "REFUSED"),
    ("manifest-broken", {"broken_manifest": True}, (), None, 1, "REFUSED"),
    ("real-aside-touched", {"touch_real": True}, (), None, 1, "REFUSED"),
    ("game-running", {}, (), "python.exe", 1, "REFUSED"),
    ("explicit-label", {}, ("--label", LABEL), None, 0, "PREFLIGHT-LAUNCH READY"),
    ("dry-run-ok", {}, ("--dry-run",), None, 0, "DRY-RUN"),
    ("dry-run-restored", {"split": False}, ("--dry-run",), None, 1, "structural failure"),
]


def main() -> int:
    failures = 0
    for name, kwargs, extra, env_exe, want_code, wanted in CASES:
        root = Path(tempfile.mkdtemp(prefix="ly-launch-guard-"))
        try:
            data, coop = build(root, **kwargs)
            code, out = run(data, coop, *extra, env_exe=env_exe)
            good = code == want_code and wanted in out
            print(f"{'PASS' if good else 'FAIL'}  {name:<24} exit={code} want={want_code}"
                  f" marker={'ok' if wanted in out else 'MISSING'}")
            if not good:
                failures += 1
                print("      --- output ---")
                for line in out.splitlines()[:12]:
                    print(f"      {line}")
        finally:
            shutil.rmtree(root, ignore_errors=True)
    total = len(CASES)
    print(f"\n{total - failures}/{total} PASS, {failures} FAIL")
    return 0 if failures == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
