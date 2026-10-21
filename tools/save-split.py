#!/usr/bin/env python3
"""save-split - S1.5 save-folder swap tool (Python 3 stdlib only).

Spec: S1.5-plan.md §1 (P0), §1.2/§1.3 protocol.

The real save folder is never written: `split` renames it aside, `restore`
renames it back. Every step hashes before/after and leaves a JSON record under
coop-test/<label>/.
"""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
COOP_TEST = REPO_ROOT / "coop-test"
DEFAULT_DATA = Path(
    r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan\LongYinLiZhiZhuan_Data"
)
DATA = Path(os.environ.get("LONGYIN_DATA_DIR", DEFAULT_DATA))
EXE = "LongYinLiZhiZhuan.exe"

REAL_PREFIX = "Save.real-"
USED_PREFIX = "Save.used-"
HIDDEN_PREFIX = ".hidden-SaveSlot"
PREF_FILE = "PlayerprefData.dat"
# Files the game/Steam rewrites on its own (S1 §2: 56 SaveSlot* files stayed
# byte-identical, only PlayerprefData.dat changed). Reported, never fatal.
VOLATILE_TOP = {PREF_FILE, "steam_autocloud.vdf"}
# S2 preflight: a launch is only green-lit while a known sampler is already
# waiting for the process, and the shipped write path (S1.5 IPC) stays disarmed.
SAMPLER_SCRIPTS = ("spike_watch.py", "s15d_sample.py", "s1_watch.py",
                   "spike_capture.py", "j7_sample.py", "worldtime_watch.py")
PREFS_MUST_BE_FALSE = ("AllowRemoteInput", "JitProbe", "JitProbeW2",
                       "JitProbeW4", "JitProbeW6", "JitProbeW7")
# A missing key is WARN (MelonLoader default is false); any value outside this
# set is treated as armed and fails the check.
PREFS_FALSEY = {"false", "0", "no", "off"}


# ---------------------------------------------------------------- utilities


def now_stamp() -> str:
    return dt.datetime.now().strftime("%Y%m%d-%H%M%S")


def sha256_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as fh:
        for chunk in iter(lambda: fh.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def rel_files(root: Path) -> list[str]:
    """Sorted POSIX-style relative paths of every file under root."""
    out = []
    for dirpath, _dirnames, filenames in os.walk(root):
        for name in filenames:
            rel = Path(dirpath, name).relative_to(root)
            out.append(rel.as_posix())
    return sorted(out)


def hash_tree(root: Path) -> dict[str, str]:
    return {rel: sha256_file(root / rel) for rel in rel_files(root)}


def tree_bytes(root: Path) -> int:
    return sum((root / rel).stat().st_size for rel in rel_files(root))


def digest_of(hashes: dict[str, str]) -> str:
    h = hashlib.sha256()
    for rel in sorted(hashes):
        h.update(f"{rel}\0{hashes[rel]}\n".encode("utf-8"))
    return h.hexdigest()


def write_hashfile(hashes: dict[str, str], out: Path, header: str = "") -> None:
    out.parent.mkdir(parents=True, exist_ok=True)
    lines = [f"{hashes[rel]} *Save/{rel}" for rel in sorted(hashes)]
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")
    if header:
        (out.parent / (out.name + ".note")).write_text(header + "\n", encoding="utf-8")


def read_hashfile(path: Path) -> dict[str, str]:
    """Parse `sha256sum` output; normalise the path prefix away."""
    out: dict[str, str] = {}
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line:
            continue
        digest, _, name = line.partition(" ")
        name = name.strip().lstrip("*")
        name = name.replace("\\", "/")
        for prefix in ("Save/", "save/"):
            if name.startswith(prefix):
                name = name[len(prefix):]
                break
        out[name] = digest.lower()
    return out


def diff_hashes(
    before: dict[str, str], after: dict[str, str]
) -> tuple[list[str], list[str], list[str]]:
    changed = sorted(k for k in before if k in after and before[k] != after[k])
    missing = sorted(k for k in before if k not in after)
    added = sorted(k for k in after if k not in before)
    return changed, missing, added


def group(keys: list[str]) -> dict[str, list[str]]:
    groups: dict[str, list[str]] = {"slot": [], "playerpref": [], "other": []}
    for key in keys:
        if key.startswith("SaveSlot"):
            groups["slot"].append(key)
        elif key == PREF_FILE:
            groups["playerpref"].append(key)
        else:
            groups["other"].append(key)
    return groups


def report_diff(label: str, before: dict[str, str], after: dict[str, str]) -> bool:
    changed, missing, added = diff_hashes(before, after)
    print(f"  {label}: changed={len(changed)} missing={len(missing)} added={len(added)}")
    for kind, keys in group(changed).items():
        if keys:
            print(f"    changed[{kind}]={len(keys)}: {', '.join(keys[:6])}"
                  + (" ..." if len(keys) > 6 else ""))
    for kind, keys in group(missing).items():
        if keys:
            print(f"    missing[{kind}]={len(keys)}: {', '.join(keys[:6])}"
                  + (" ..." if len(keys) > 6 else ""))
    for kind, keys in group(added).items():
        if keys:
            print(f"    added[{kind}]={len(keys)}: {', '.join(keys[:6])}"
                  + (" ..." if len(keys) > 6 else ""))
    return not (changed or missing or added)


def game_running() -> bool:
    try:
        res = subprocess.run(
            ["tasklist", "/FI", f"IMAGENAME eq {EXE}", "/NH"],
            capture_output=True, text=True, timeout=20,
        )
    except (OSError, subprocess.SubprocessError) as exc:  # pragma: no cover
        raise SystemExit(f"cannot run tasklist: {exc}")
    return EXE.lower() in res.stdout.lower()


def require_game_closed(action: str) -> None:
    if game_running():
        raise SystemExit(f"REFUSED: {EXE} is running - close the game before `{action}`")


def save_dirs() -> dict[str, Path]:
    """Save-related folders that live next to one another in the data dir."""
    out: dict[str, Path] = {}
    if not DATA.is_dir():
        raise SystemExit(f"data dir not found: {DATA}")
    for entry in sorted(DATA.iterdir()):
        name = entry.name
        if entry.is_dir() and (
            name == "Save"
            or name.startswith(REAL_PREFIX)
            or name.startswith(USED_PREFIX)
        ):
            out[name] = entry
    return out


def left_behind() -> list[str]:
    return [n for n in save_dirs() if n.startswith(REAL_PREFIX) or n.startswith(USED_PREFIX)]


def newest_baseline(pattern: str) -> Path | None:
    candidates = sorted(COOP_TEST.glob(f"*/{pattern}"), key=lambda p: p.stat().st_mtime)
    return candidates[-1] if candidates else None


def label_dir(label: str) -> Path:
    return COOP_TEST / label


def read_manifest(label: str) -> dict:
    path = label_dir(label) / "MANIFEST.json"
    if not path.is_file():
        raise SystemExit(f"manifest not found: {path}")
    return json.loads(path.read_text(encoding="utf-8"))


def append_history(label: str, event: str, payload: dict) -> None:
    line = {"ts": dt.datetime.now().isoformat(timespec="seconds"), "event": event, **payload}
    with (label_dir(label) / "history.ndjson").open("a", encoding="utf-8") as fh:
        fh.write(json.dumps(line, ensure_ascii=False) + "\n")


def slot_state(root: Path) -> dict[str, dict]:
    out: dict[str, dict] = {}
    for entry in sorted(root.iterdir()):
        if entry.is_dir() and entry.name.startswith("SaveSlot"):
            files = rel_files(entry)
            info = entry / "Info"
            out[entry.name] = {
                "files": len(files),
                "info_bytes": info.stat().st_size if info.is_file() else 0,
            }
    return out


def mtimes_of(root: Path) -> dict[str, list[int]]:
    return {rel: [(root / rel).stat().st_size, (root / rel).stat().st_mtime_ns]
            for rel in rel_files(root)}


def short(digest: str) -> str:
    return digest[:12]


# ---------------------------------------------------------------- commands


def cmd_status(args: argparse.Namespace) -> int:
    print(f"data dir : {DATA}")
    print(f"game     : {'RUNNING' if game_running() else 'not running'}")
    dirs = save_dirs()
    for name, path in dirs.items():
        files = rel_files(path)
        digest = digest_of(hash_tree(path)) if files else ""
        print(f"  {name:<28} files={len(files):<3} digest={short(digest)}")
    leftovers = left_behind()
    print(f"leftovers: {', '.join(leftovers) if leftovers else 'none'}")

    if "Save" in dirs:
        mounted = hash_tree(dirs["Save"])
        print("mounted Save matches:")
        matched = False
        for base in sorted(COOP_TEST.glob("*/savehash.*.before.txt")):
            expected = read_hashfile(base)
            changed, missing, added = diff_hashes(expected, mounted)
            if not (changed or missing or added):
                print(f"  EXACT  {base.parent.name}/{base.name}")
                matched = True
        if not matched:
            newest = newest_baseline("savehash.copy.before.txt")
            if newest:
                changed, missing, added = diff_hashes(read_hashfile(newest), mounted)
                print(f"  ~{newest.parent.name}/{newest.name}: "
                      f"changed={len(changed)} missing={len(missing)} added={len(added)}")
    return 0


def cmd_split(args: argparse.Namespace) -> int:
    require_game_closed("split")
    label = args.label or f"S1.5-{now_stamp()}"
    dest = label_dir(label)
    if dest.exists():
        raise SystemExit(f"REFUSED: {dest} already exists")
    save = DATA / "Save"
    if not save.is_dir():
        raise SystemExit(f"REFUSED: {save} is missing")

    leftovers = left_behind()
    if leftovers:
        raise SystemExit(
            "REFUSED (pre-flight 2.5): " + ", ".join(leftovers)
            + " still present - run `restore` first"
        )
    hidden = [p.name for p in save.iterdir() if p.name.startswith(HIDDEN_PREFIX)]
    if hidden:
        raise SystemExit(f"REFUSED: sentinel still hidden: {', '.join(hidden)}")

    real_hashes = hash_tree(save)
    expected_path = Path(args.expected_baseline) if args.expected_baseline else newest_baseline(
        "savehash.real.before.txt"
    )
    if expected_path is None:
        raise SystemExit("REFUSED: no baseline found; pass --expected-baseline")
    expected = read_hashfile(expected_path)
    print(f"expect  : {expected_path}")
    if not report_diff("Save vs baseline", expected, real_hashes):
        raise SystemExit("REFUSED: Save does not match the real baseline (see above)")

    dest.mkdir(parents=True)
    write_hashfile(real_hashes, dest / "savehash.real.before.txt",
                   header=f"hashed {dt.datetime.now().isoformat(timespec='seconds')} label={label}")
    real_mtimes = mtimes_of(save)

    print("copy    : Save -> coop-test/%s/save" % label)
    shutil.copytree(save, dest / "save", copy_function=shutil.copy2)

    real_dir = DATA / f"{REAL_PREFIX}{label}"
    if real_dir.exists():
        raise SystemExit(f"REFUSED: {real_dir} already exists")
    print(f"rename  : Save -> {real_dir.name}")
    save.rename(real_dir)

    print(f"copy    : coop-test/{label}/save -> Save")
    shutil.copytree(dest / "save", save, copy_function=shutil.copy2)

    mounted = hash_tree(save)
    write_hashfile(mounted, dest / "savehash.copy.before.txt",
                   header=f"hashed {dt.datetime.now().isoformat(timespec='seconds')} label={label}")
    if not report_diff("copy vs real", real_hashes, mounted):
        raise SystemExit("FAILED: mounted copy differs from the real save")

    manifest = {
        "label": label,
        "created": dt.datetime.now().isoformat(timespec="seconds"),
        "data_dir": str(DATA),
        "expect_baseline": str(expected_path),
        "sentinel_slot": args.sentinel_slot,
        "real": {
            "dir": real_dir.name,
            "files": len(real_hashes),
            "bytes": tree_bytes(real_dir),
            "digest": digest_of(real_hashes),
            "mtimes": real_mtimes,
        },
        "copy": {
            "dir": str(dest / "save"),
            "files": len(mounted),
            "bytes": tree_bytes(save),
            "digest": digest_of(mounted),
        },
        "slots": slot_state(save),
        "mounted_baseline": "savehash.copy.before.txt",
    }
    (dest / "MANIFEST.json").write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )
    append_history(label, "split", {"real_dir": real_dir.name, "files": len(real_hashes)})
    print(f"OK split {label}: {len(real_hashes)} files, real kept as {real_dir.name}")
    return 0


def cmd_verify(args: argparse.Namespace) -> int:
    manifest = read_manifest(args.label)
    dest = label_dir(args.label)
    real_dir = DATA / manifest["real"]["dir"]
    save = DATA / "Save"
    real_base = read_hashfile(dest / "savehash.real.before.txt")
    copy_base = read_hashfile(dest / "savehash.copy.before.txt")
    ok = True

    if not save.is_dir():
        print("FAIL: Save folder missing")
        return 1

    mounted = hash_tree(save)
    print("mounted Save:")
    is_real = report_diff("vs real baseline", real_base, mounted)
    if real_dir.is_dir() and is_real:
        print("  note: byte-identical to the real baseline (expected right after split)")
    # A hidden sentinel slot is still part of the copy: fold it back in before
    # comparing, otherwise verify would report the slot as missing.
    mounted_view = dict(mounted)
    for entry in save.iterdir():
        if entry.is_dir() and entry.name.startswith(HIDDEN_PREFIX):
            slot = entry.name[len(HIDDEN_PREFIX):]
            for rel, digest in hash_tree(entry).items():
                mounted_view[f"SaveSlot{slot}/{rel}"] = digest
            print(f"  sentinel: {entry.name} counts as SaveSlot{slot}/ ({len(rel_files(entry))} files)")
    copy_changed, copy_missing, copy_added = diff_hashes(copy_base, mounted_view)
    copy_added = [k for k in copy_added if not k.startswith(HIDDEN_PREFIX)]
    print(f"  vs copy baseline: changed={len(copy_changed)} "
          f"missing={len(copy_missing)} added={len(copy_added)}")
    slot_groups = group(copy_changed)
    slot_missing = group(copy_missing)
    slot_added = group(copy_added)
    slot_drift = (len(slot_groups["slot"]) + len(slot_missing["slot"])
                  + len(slot_added["slot"]))
    other_drift = [k for k in copy_changed + copy_missing + copy_added
                   if k not in slot_groups["slot"] and k not in VOLATILE_TOP]
    if slot_drift:
        ok = False
        print(f"  FAIL: SaveSlot* drift vs copy baseline ({slot_drift} files)")
    if other_drift:
        ok = False
        print(f"  FAIL: unexpected top-level drift ({len(other_drift)}): {other_drift[:5]}")
    if is_real and not real_dir.is_dir():
        ok = False
        print("  FAIL: mounted Save matches the real baseline and no real folder is left")

    real_report = {"present": real_dir.is_dir()}
    if real_dir.is_dir():
        real_now = hash_tree(real_dir)
        print(f"\nreal folder {real_dir.name}:")
        unchanged = report_diff("vs real baseline", real_base, real_now)
        drift_mtime = []
        for rel, (size, mtime) in manifest["real"]["mtimes"].items():
            if not (real_dir / rel).is_file():
                continue
            st = (real_dir / rel).stat()
            if st.st_size != size or st.st_mtime_ns != mtime:
                drift_mtime.append(rel)
        print(f"  mtime/size drift: {len(drift_mtime)}")
        for rel in drift_mtime[:10]:
            print(f"    {rel}")
        if not unchanged or drift_mtime:
            ok = False
        real_report.update({"unchanged": unchanged, "mtime_drift": drift_mtime})
    else:
        print(f"\nreal folder {real_dir.name}: MISSING")
        ok = False

    hidden = sorted(p.name for p in save.iterdir() if p.name.startswith(HIDDEN_PREFIX))
    if hidden:
        print(f"sentinel: hidden={', '.join(hidden)}")
    sentinel = dest / "sentinel.json"
    if sentinel.is_file():
        print(f"sentinel record: {sentinel.name}")

    record = {
        "ts": dt.datetime.now().isoformat(timespec="seconds"),
        "label": args.label,
        "mounted_digest": digest_of(mounted),
        "mounted_is_real_baseline": is_real,
        "copy_drift": {
            "changed": len(copy_changed), "missing": len(copy_missing),
            "added": len(copy_added),
            "slot_drift": slot_drift,
            "other_drift": other_drift,
        },
        "real": real_report,
        "hidden": hidden,
        "ok": ok,
    }
    (dest / f"verify-{now_stamp()}.json").write_text(
        json.dumps(record, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )
    append_history(args.label, "verify", {"ok": ok})
    print(f"\nRESULT: {'PASS' if ok else 'FAIL'}")
    return 0 if ok else 1


def cmd_restore(args: argparse.Namespace) -> int:
    require_game_closed("restore")
    dest = label_dir(args.label)
    save = DATA / "Save"
    real_base_path = dest / "savehash.real.before.txt"
    if not real_base_path.is_file():
        raise SystemExit(f"REFUSED: baseline missing: {real_base_path}")
    manifest_path = dest / "MANIFEST.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8")) if manifest_path.is_file() else {}
    real_name = args.real_dir or manifest.get("real", {}).get("dir")
    if not real_name:
        raise SystemExit("REFUSED: pass --real-dir (no manifest for this label)")
    real_dir = DATA / real_name
    if not real_dir.is_dir():
        raise SystemExit(f"REFUSED: {real_dir} not found - nothing to restore")
    if not save.is_dir():
        raise SystemExit(f"REFUSED: {save} not found")

    real_base = read_hashfile(real_base_path)
    copy_base_path = dest / "savehash.copy.before.txt"
    copy_base = read_hashfile(copy_base_path) if copy_base_path.is_file() else {}
    # `Save.real-<label>` exists, so by construction `Save` is the copy: the
    # real folder was only ever renamed aside. No hash test can tell the two
    # apart right after split (they are byte-identical by design).
    mounted = hash_tree(save)

    hidden = [p.name for p in save.iterdir() if p.name.startswith(HIDDEN_PREFIX)]
    if hidden:
        raise SystemExit(f"REFUSED: run `sentinel unhide` first: {', '.join(hidden)}")

    if copy_base:
        copy_changed, copy_missing, copy_added = diff_hashes(copy_base, mounted)
        non_pref = [k for k in copy_changed + copy_missing + copy_added
                    if not k.startswith("SaveSlot") and k not in VOLATILE_TOP]
        if non_pref:
            raise SystemExit(f"REFUSED: unexpected drift on the copy: {non_pref[:5]}")
        print(f"copy check: changed={len(copy_changed)} "
              f"(pref={int(PREF_FILE in copy_changed)}) "
              f"missing={len(copy_missing)} added={len(copy_added)}")
    else:
        print("copy check: skipped (no savehash.copy.before.txt)")

    used_dir = DATA / f"{USED_PREFIX}{now_stamp()}"
    print(f"rename  : Save -> {used_dir.name}")
    save.rename(used_dir)
    print(f"rename  : {real_dir.name} -> Save")
    real_dir.rename(save)

    restored = hash_tree(save)
    if not report_diff("restored Save vs real baseline", real_base, restored):
        print("ROLLBACK: hash mismatch - putting the copy back")
        save.rename(real_dir)
        used_dir.rename(save)
        raise SystemExit("FAILED: restored Save does not match the baseline")

    kept = dest / used_dir.name
    shutil.move(str(used_dir), str(kept))
    append_history(args.label, "restore", {"used_dir": kept.name, "files": len(restored)})
    record = {
        "ts": dt.datetime.now().isoformat(timespec="seconds"),
        "label": args.label,
        "restored_files": len(restored),
        "digest": digest_of(restored),
        "used_dir": kept.name,
        "ok": True,
    }
    (dest / f"restore-{now_stamp()}.json").write_text(
        json.dumps(record, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )
    print(f"OK restore {args.label}: {len(restored)} files, copy kept at {kept}")
    print(f"real folder now at {save} (renamed back, never rewritten)")
    return 0


def cmd_sentinel(args: argparse.Namespace) -> int:
    require_game_closed(f"sentinel {args.action}")
    manifest = read_manifest(args.label)
    save = DATA / "Save"
    slot_name = f"SaveSlot{args.slot}"
    visible = save / slot_name
    hidden = save / f"{HIDDEN_PREFIX}{args.slot}"
    record_path = label_dir(args.label) / "sentinel.json"

    if args.action == "hide":
        if hidden.exists():
            raise SystemExit(f"REFUSED: {hidden.name} already exists")
        if not visible.is_dir():
            raise SystemExit(f"REFUSED: {visible} not found")
        hashes = hash_tree(visible)
        mtimes = mtimes_of(visible)
        record = {
            "ts": dt.datetime.now().isoformat(timespec="seconds"),
            "label": args.label,
            "slot": args.slot,
            "files": len(hashes),
            "digest": digest_of(hashes),
            "hashes": hashes,
            "mtimes": mtimes,
            "state": "hidden",
        }
        record_path.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
        visible.rename(hidden)
        print(f"OK hidden {slot_name} -> {hidden.name} ({len(hashes)} files)")
        append_history(args.label, "sentinel_hide", {"slot": slot_name, "files": len(hashes)})
        return 0

    if not record_path.is_file():
        raise SystemExit(f"REFUSED: no sentinel record at {record_path}")
    record = json.loads(record_path.read_text(encoding="utf-8"))
    if not hidden.is_dir():
        if visible.is_dir():
            print(f"already visible: {slot_name}")
            return 0
        raise SystemExit(f"REFUSED: neither {hidden.name} nor {slot_name} exists")
    if visible.exists():
        # The game/Steam recreates SaveSlot<k>/steam_autocloud.vdf even while the
        # folder is hidden. Only identical duplicates may be moved aside.
        extra = rel_files(visible)
        archived = record["hashes"]
        unsafe = [rel for rel in extra
                  if rel not in archived or sha256_file(visible / rel) != archived[rel]]
        if unsafe:
            raise SystemExit(f"REFUSED: {visible.name} has new content: {unsafe[:5]}")
        aside = label_dir(args.label) / f"recreated-{visible.name}-{now_stamp()}"
        shutil.move(str(visible), str(aside))
        print(f"moved aside recreated {visible.name} ({len(extra)} identical files) -> {aside.name}")
    hidden.rename(visible)
    hashes = hash_tree(visible)
    ok = hashes == record["hashes"]
    print(f"OK unhidden {hidden.name} -> {slot_name} ({len(hashes)} files) "
          f"hash_match={ok}")
    record["state"] = "visible"
    record["unhide_ts"] = dt.datetime.now().isoformat(timespec="seconds")
    record["unhide_hash_match"] = ok
    record_path.write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
    append_history(args.label, "sentinel_unhide", {"slot": slot_name, "hash_match": ok})
    if not ok:
        print("FAIL: slot content changed while hidden")
        return 1
    return 0


def cmd_rebaseline(args: argparse.Namespace) -> int:
    require_game_closed("rebaseline")
    against = read_hashfile(Path(args.against))
    save = DATA / "Save"
    current = hash_tree(save)
    changed, missing, added = diff_hashes(against, current)
    identical = report_diff(f"Save vs {args.against}", against, current)
    bad = [k for k in changed + missing + added if k not in VOLATILE_TOP]
    if bad:
        raise SystemExit("REFUSED: non-volatile files differ: " + ", ".join(bad[:5]))
    if not identical:
        print("accepting volatile-only drift")

    dest = COOP_TEST / "baselines" / now_stamp()
    dest.mkdir(parents=True)
    write_hashfile(current, dest / "savehash.real.before.txt")
    meta = {
        "ts": dt.datetime.now().isoformat(timespec="seconds"),
        "against": str(Path(args.against).resolve()),
        "reason": args.reason,
        "files": len(current),
        "digest": digest_of(current),
        "changed": changed,
        "missing": missing,
        "added": added,
        "volatile_only": True,
    }
    (dest / "REBASELINE.json").write_text(
        json.dumps(meta, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )
    print(f"OK rebaseline: {dest / 'savehash.real.before.txt'}")
    print(f"  changed={len(changed)} (volatile only) missing={len(missing)} added={len(added)}")
    return 0


def process_alive(pid: int) -> bool:
    try:
        res = subprocess.run(["tasklist", "/FI", f"PID eq {pid}", "/NH"],
                             capture_output=True, text=True, timeout=20)
    except (OSError, subprocess.SubprocessError):
        return False
    return str(pid) in res.stdout.split()


def process_cmdline(pid: int) -> str | None:
    script = f'(Get-CimInstance Win32_Process -Filter "ProcessId={pid}").CommandLine'
    try:
        res = subprocess.run(["powershell", "-NoProfile", "-NonInteractive", "-Command", script],
                             capture_output=True, text=True, timeout=40)
    except (OSError, subprocess.SubprocessError):
        return None
    return res.stdout.strip() or None


def read_prefs(path: Path) -> tuple[dict[str, str], bool]:
    """Values of the [LongYinCoop] section + whether that section exists."""
    out: dict[str, str] = {}
    found = False
    if not path.is_file():
        return out, found
    section = ""
    for raw in path.read_text(encoding="utf-8", errors="replace").splitlines():
        line = raw.strip()
        if not line or line[0] in "#;":
            continue
        if line.startswith("[") and line.endswith("]"):
            section = line[1:-1].strip()
            if section == "LongYinCoop":
                found = True
            continue
        if section == "LongYinCoop" and "=" in line:
            key, _, value = line.partition("=")
            out[key.strip()] = value.strip()
    return out, found


def cmd_preflight(args: argparse.Namespace) -> int:  # noqa: C901 - one linear check list
    """Refuse to green-light a game launch unless the copy is mounted and the sampler is up."""
    require_game_closed("preflight")
    label = args.label
    dest = label_dir(label)
    print(f"preflight: label={label}")
    if not (dest / "MANIFEST.json").is_file():
        print(f"  REFUSED: manifest not found: {dest / 'MANIFEST.json'}")
        return 1
    manifest = read_manifest(label)
    real_name = manifest["real"]["dir"]
    real_dir = DATA / real_name
    save = DATA / "Save"
    ok = True
    checks: dict[str, bool] = {}

    # 1. the real folder is still aside and untouched
    if not real_dir.is_dir():
        print(f"  FAIL real-aside: {real_name} missing (is the real save mounted?)")
        checks["real_aside"] = False
        ok = False
    else:
        real_now = hash_tree(real_dir)
        same = digest_of(real_now) == manifest["real"]["digest"]
        print(f"  {'OK  ' if same else 'FAIL'} real-aside: {real_name} "
              f"files={len(real_now)} digest={short(digest_of(real_now))}")
        checks["real_aside"] = same
        ok = ok and same

    # 2. the mounted Save is the copy (volatile drift tolerated, nothing else)
    copy_base = read_hashfile(dest / "savehash.copy.before.txt")
    mounted = hash_tree(save)
    changed, missing, added = diff_hashes(copy_base, mounted)
    bad = [k for k in changed + missing + added if k not in VOLATILE_TOP]
    hidden = [p.name for p in save.iterdir() if p.name.startswith(HIDDEN_PREFIX)] if save.is_dir() else []
    mounted_ok = bool(copy_base) and not bad and not hidden
    print(f"  {'OK  ' if mounted_ok else 'FAIL'} mounted-is-copy: files={len(mounted)} "
          f"vs copy baseline changed={len(changed)} missing={len(missing)} added={len(added)}"
          + (f" nonvolatile={bad[:5]}" if bad else "")
          + (f" hidden_sentinel={hidden}" if hidden else ""))
    checks["mounted_is_copy"] = mounted_ok
    ok = ok and mounted_ok

    # 3. exactly the expected leftover
    left = sorted(left_behind())
    left_ok = left == [real_name]
    print(f"  {'OK  ' if left_ok else 'FAIL'} leftovers: {', '.join(left) if left else 'none'}"
          f"{'' if left_ok else f' (expected only {real_name})'}")
    checks["leftovers"] = left_ok
    ok = ok and left_ok

    # 4. sampler process is up, is a known sampler script, and already opened its output
    pid = args.sampler_pid
    sampler_ok = False
    if not process_alive(pid):
        print(f"  FAIL sampler: pid {pid} is not running")
    else:
        cmdline = process_cmdline(pid)
        hits = [s for s in SAMPLER_SCRIPTS if cmdline and s in cmdline]
        if not cmdline:
            print(f"  FAIL sampler: pid {pid} command line not readable")
        elif not hits:
            print(f"  FAIL sampler: pid {pid} is not a known sampler ({cmdline[:120]})")
        elif not Path(args.sampler_out).is_file():
            print(f"  FAIL sampler: output file not created yet: {args.sampler_out}")
        else:
            sampler_ok = True
            print(f"  OK   sampler: pid {pid} ({hits[0]}) out={args.sampler_out}")
    checks["sampler"] = sampler_ok
    ok = ok and sampler_ok

    # 5. the shipped write path must stay disarmed (S2 has no game-state write)
    prefs_path = DATA.parent / "UserData" / "MelonPreferences.cfg"
    prefs, has_section = read_prefs(prefs_path)
    missing = [k for k in PREFS_MUST_BE_FALSE if k not in prefs]
    armed = [k for k in PREFS_MUST_BE_FALSE
             if k in prefs and prefs[k].strip().lower() not in PREFS_FALSEY]
    if missing:
        print(f"  WARN prefs-disarmed: keys missing from [LongYinCoop] "
              f"(defaults assumed safe): {', '.join(missing)}")
    prefs_ok = has_section and not armed
    if not has_section:
        print(f"  FAIL prefs-disarmed: [LongYinCoop] section not found in {prefs_path}")
    else:
        print(f"  {'OK  ' if prefs_ok else 'FAIL'} prefs-disarmed: "
              + (f"armed={armed}" if armed else f"{len(PREFS_MUST_BE_FALSE) - len(missing)} keys=false"
                 + (f", {len(missing)} missing" if missing else "")))
    checks["prefs_disarmed"] = prefs_ok
    ok = ok and prefs_ok

    verdict = "READY - you may launch the game" if ok else "REFUSED - do not launch"
    print(f"PREFLIGHT {verdict}")
    append_history(label, "preflight", {"ok": ok, "checks": checks,
                                        "sampler_pid": pid,
                                        "sampler_out": args.sampler_out})
    return 0 if ok else 1


def cmd_list(args: argparse.Namespace) -> int:
    if not COOP_TEST.is_dir():
        print("no coop-test directory")
        return 0
    for dest in sorted(COOP_TEST.iterdir()):
        manifest_path = dest / "MANIFEST.json"
        if not manifest_path.is_file():
            continue
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        events = []
        hist = dest / "history.ndjson"
        if hist.is_file():
            events = [json.loads(line)["event"] for line in
                      hist.read_text(encoding="utf-8").splitlines() if line.strip()]
        print(f"{manifest['label']:<28} created={manifest['created']} "
              f"files={manifest['real']['files']} real={manifest['real']['dir']}")
        print(f"  events: {', '.join(events) if events else '-'}")
        left = left_behind()
        print(f"  leftovers now: {', '.join(left) if left else 'none'}")
    return 0


# ---------------------------------------------------------------- entry point


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--data-dir", type=Path, default=DATA,
                        help=f"LongYinLiZhiZhuan_Data folder (default: {DATA})")
    parser.add_argument("--coop-dir", type=Path, default=COOP_TEST,
                        help=f"test-artifact folder (default: {COOP_TEST})")
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("status", help="show which save folder is mounted").set_defaults(func=cmd_status)

    p_split = sub.add_parser("split", help="swap the real save out and mount a copy")
    p_split.add_argument("--label", default=None)
    p_split.add_argument("--expected-baseline", default=None)
    p_split.add_argument("--sentinel-slot", type=int, default=8)
    p_split.set_defaults(func=cmd_split)

    p_verify = sub.add_parser("verify", help="rehash mounted copy and the real folder")
    p_verify.add_argument("--label", required=True)
    p_verify.set_defaults(func=cmd_verify)

    p_restore = sub.add_parser("restore", help="rename the real save back")
    p_restore.add_argument("--label", required=True)
    p_restore.add_argument("--real-dir", default=None,
                           help="Save.real-* folder name (default: from MANIFEST.json)")
    p_restore.set_defaults(func=cmd_restore)

    p_sentinel = sub.add_parser("sentinel", help="hide/unhide a slot folder in the copy")
    p_sentinel.add_argument("action", choices=["hide", "unhide"])
    p_sentinel.add_argument("--label", required=True)
    p_sentinel.add_argument("--slot", type=int, required=True)
    p_sentinel.set_defaults(func=cmd_sentinel)

    p_base = sub.add_parser("rebaseline", help="record a new real baseline (volatile drift only)")
    p_base.add_argument("--against", required=True)
    p_base.add_argument("--reason", required=True)
    p_base.set_defaults(func=cmd_rebaseline)

    p_pre = sub.add_parser("preflight",
                           help="green-light a game launch: copy mounted + sampler up + prefs disarmed")
    p_pre.add_argument("--label", required=True)
    p_pre.add_argument("--sampler-pid", type=int, required=True)
    p_pre.add_argument("--sampler-out", required=True,
                       help="sampler output file that must already exist")
    p_pre.set_defaults(func=cmd_preflight)

    sub.add_parser("list", help="history of split/restore runs").set_defaults(func=cmd_list)
    return parser


def main(argv: list[str] | None = None) -> int:
    global DATA, COOP_TEST
    args = build_parser().parse_args(argv)
    DATA = Path(args.data_dir)
    COOP_TEST = Path(args.coop_dir)
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
