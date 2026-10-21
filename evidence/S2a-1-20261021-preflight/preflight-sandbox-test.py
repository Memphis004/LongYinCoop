"""Sandbox tests for `save-split.py preflight` (no game, no real save, no repo writes)."""
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
import time
from pathlib import Path

TOOL = Path(r"C:\Users\memph\LongYinCoop\tools\save-split.py")
LABEL = "testlabel"
REAL = f"Save.real-{LABEL}"
BASE = Path(tempfile.mkdtemp(prefix="preflight-sbx-"))
PY = sys.executable

# sampler stand-in: alive, cmdline contains a known sampler script name
SAMPLER_OUT = BASE / "sampler" / "run.ndjson"
SAMPLER_OUT.parent.mkdir(parents=True, exist_ok=True)
SAMPLER_OUT.write_text("", encoding="utf-8")
sampler = subprocess.Popen([PY, "-c", "import time; time.sleep(600)", "spike_watch.py"])
time.sleep(0.8)


def digest_tree(root: Path) -> str:
    hs = hashes(root)
    h = hashlib.sha256()
    for rel in sorted(hs):
        h.update(f"{rel}\0{hs[rel]}\n".encode("utf-8"))
    return h.hexdigest()


def hashes(root: Path) -> dict:
    return {p.relative_to(root).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in sorted(root.rglob("*")) if p.is_file()}


def write_hashfile(root: Path, out: Path) -> None:
    hs = hashes(root)
    out.write_text("".join(f"{hs[rel]} *Save/{rel}\n" for rel in sorted(hs)), encoding="utf-8")


def setup(armed_pref=False, hidden=False, prefs_section=True, missing_keys=()):
    if BASE.exists():
        for child in BASE.iterdir():
            if child.name in ("sampler",):
                continue
            shutil.rmtree(child) if child.is_dir() else child.unlink()
    data = BASE / "data"
    coop = BASE / "coop"
    save = data / "Save"
    real = data / REAL
    (save / "SaveSlot0").mkdir(parents=True)
    (save / "SaveSlot0" / "Info").write_text("info", encoding="utf-8")
    (save / "SaveSlot0" / "Data").write_text("data", encoding="utf-8")
    (save / "PlayerprefData.dat").write_text("pref", encoding="utf-8")
    (save / "steam_autocloud.vdf").write_text("vdf", encoding="utf-8")
    shutil.copytree(save, real)
    if hidden:
        (save / ".hidden-SaveSlot1").mkdir()
        (save / ".hidden-SaveSlot1" / "Info").write_text("x", encoding="utf-8")
    dest = coop / LABEL
    dest.mkdir(parents=True)
    write_hashfile(save, dest / "savehash.copy.before.txt")
    write_hashfile(real, dest / "savehash.real.before.txt")
    (dest / "MANIFEST.json").write_text(json.dumps({
        "label": LABEL,
        "real": {"dir": REAL, "files": len(hashes(real)), "digest": digest_tree(real)},
    }), encoding="utf-8")
    user = BASE / "UserData"
    user.mkdir(parents=True, exist_ok=True)
    if not prefs_section:
        (user / "MelonPreferences.cfg").write_text(
            "[SomeOtherMod]\nAllowRemoteInput = true\n", encoding="utf-8")
        return data, coop, save, real
    lines = ["[LongYinCoop]", "Enabled = true"]
    for key in ("AllowRemoteInput", "JitProbe", "JitProbeW2", "JitProbeW4",
                "JitProbeW6", "JitProbeW7"):
        if key in missing_keys:
            continue
        lines.append(f"{key} = {'true' if (armed_pref and key == 'JitProbeW4') else 'false'}")
    (user / "MelonPreferences.cfg").write_text("\n".join(lines) + "\n", encoding="utf-8")
    return data, coop, save, real


def run(data, coop, pid=None, out=None, label=LABEL, extra=None):
    cmd = [PY, str(TOOL), "--data-dir", str(data), "--coop-dir", str(coop), "preflight",
           "--label", label, "--sampler-pid", str(pid if pid is not None else sampler.pid)]
    if out:
        cmd += ["--sampler-out", str(out)]
    if extra:
        cmd += extra
    return subprocess.run(cmd, capture_output=True, text=True, timeout=120)


results = []


def check(name, res, want_code, want_text=""):
    ok = res.returncode == want_code and want_text in (res.stdout + res.stderr)
    results.append((name, ok, res.returncode, want_code, want_text))
    print(f"[{'PASS' if ok else 'FAIL'}] {name} (exit {res.returncode}, want {want_code})")
    if not ok:
        print(res.stdout)
        print(res.stderr)


try:
    # 1 happy path
    data, coop, save, real = setup()
    check("T1 copy mounted + sampler up -> READY", run(data, coop, out=SAMPLER_OUT), 0, "PREFLIGHT READY")

    # 2 non-volatile drift in the mounted copy
    data, coop, save, real = setup()
    (save / "SaveSlot0" / "Data").write_text("drift", encoding="utf-8")
    check("T2 non-volatile drift -> REFUSED", run(data, coop, out=SAMPLER_OUT), 1, "nonvolatile=")

    # 3 volatile-only drift is tolerated
    data, coop, save, real = setup()
    (save / "PlayerprefData.dat").write_text("pref2", encoding="utf-8")
    check("T3 volatile drift -> READY", run(data, coop, out=SAMPLER_OUT), 0, "PREFLIGHT READY")

    # 4 real folder missing == real save mounted
    data, coop, save, real = setup()
    shutil.rmtree(real)
    check("T4 real-aside missing -> REFUSED", run(data, coop, out=SAMPLER_OUT), 1, "real-aside")

    # 5 sampler pid dead
    data, coop, save, real = setup()
    check("T5 sampler dead -> REFUSED", run(data, coop, pid=999999, out=SAMPLER_OUT), 1, "not running")

    # 6 sampler not a known script (this test process)
    data, coop, save, real = setup()
    check("T6 sampler not a sampler -> REFUSED",
          run(data, coop, pid=os.getpid(), out=SAMPLER_OUT), 1, "not a known sampler")

    # 7 prefs armed
    data, coop, save, real = setup(armed_pref=True)
    check("T7 prefs armed -> REFUSED", run(data, coop, out=SAMPLER_OUT), 1, "armed=")

    # 8 hidden sentinel still in the copy
    data, coop, save, real = setup(hidden=True)
    check("T8 hidden sentinel -> REFUSED", run(data, coop, out=SAMPLER_OUT), 1, "hidden_sentinel")

    # 9 extra Save.used-* leftover
    data, coop, save, real = setup()
    (data / "Save.used-old").mkdir()
    check("T9 extra leftover -> REFUSED", run(data, coop, out=SAMPLER_OUT), 1, "leftovers")

    # 10 unknown label
    data, coop, save, real = setup()
    check("T10 unknown label -> REFUSED", run(data, coop, out=SAMPLER_OUT, label="nope"), 1, "manifest not found")

    # 11 sampler output file missing
    data, coop, save, real = setup()
    check("T11 sampler out missing -> REFUSED",
          run(data, coop, out=BASE / "sampler" / "nope.ndjson"), 1, "output file not created")

    # 12 prefs file absent
    data, coop, save, real = setup()
    (BASE / "UserData" / "MelonPreferences.cfg").unlink()
    check("T12 prefs missing -> REFUSED", run(data, coop, out=SAMPLER_OUT), 1, "prefs-disarmed")

    # 13 history recorded
    data, coop, save, real = setup()
    run(data, coop, out=SAMPLER_OUT)
    hist = (coop / LABEL / "history.ndjson")
    ok = hist.is_file() and '"event": "preflight"' in hist.read_text(encoding="utf-8")
    results.append(("T13 history.ndjson records preflight", ok, 0, 0, ""))
    print(f"[{'PASS' if ok else 'FAIL'}] T13 history.ndjson records preflight")

    # 14 a single missing key is WARN only (defaults are false) and still READY
    data, coop, save, real = setup(missing_keys=("JitProbeW6",))
    res = run(data, coop, out=SAMPLER_OUT)
    ok = res.returncode == 0 and "WARN prefs-disarmed: keys missing" in res.stdout \
        and "missing" in res.stdout and "PREFLIGHT READY" in res.stdout
    results.append(("T14 missing key -> WARN + READY", ok, res.returncode, 0, "WARN prefs-disarmed"))
    print(f"[{'PASS' if ok else 'FAIL'}] T14 missing key -> WARN + READY (exit {res.returncode})")
    if not ok:
        print(res.stdout)

    # 15 [LongYinCoop] section absent -> REFUSED (cannot verify, fail closed)
    data, coop, save, real = setup(prefs_section=False)
    check("T15 prefs section missing -> REFUSED", run(data, coop, out=SAMPLER_OUT), 1,
          "section not found")

    # 16 --sampler-out is a required argument (argparse exit 2)
    data, coop, save, real = setup()
    check("T16 no --sampler-out -> argparse error", run(data, coop), 2, "sampler-out")
finally:
    sampler.terminate()
    try:
        sampler.wait(timeout=10)
    except Exception:
        sampler.kill()

passed = sum(1 for r in results if r[1])
print(f"\nRESULT {passed}/{len(results)} PASS")
print(f" sandbox: {BASE}")
sys.exit(0 if passed == len(results) else 1)
