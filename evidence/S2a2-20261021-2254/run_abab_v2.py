#!/usr/bin/env python3
"""S2a-2 exit 3 attempt 2: A1 -> B1 -> A2 -> B2 (30 s each) per evidence/ab-protocol-v2.md.

v2 fix: the mod writes markers as `{"kind":"phase.start","marker":true,<raw body>}` which is NOT
valid JSON (the body is appended without quotes), so markers are detected with a regex on the raw
line instead of json.loads(). Attempt 1 (run_abab.py) aborted on this tooling defect only.

Read-only on the game: the only change is one line in UserData\\MelonPreferences.cfg.
Stop conditions (protocol v2 section 6) abort the run:
  * sampler.ndjson not growing before a block
  * any sample during a block with nowActiveStateName != WaitPlayerInput
  * no phase marker within 3 s after a switch
"""
import hashlib
import json
import os
import re
import sys
import time

GAME = r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan"
CFG = os.path.join(GAME, "UserData", "MelonPreferences.cfg")
SNAP = os.path.join(GAME, "UserData", "LongYinCoop", "S2", "s2-snapshots.ndjson")
SAMPLER = r"C:\Users\memph\LongYinCoop\coop-test\S2a2-20261021-2254\sampler.ndjson"
OUT = r"C:\Users\memph\LongYinCoop\evidence\S2a2-20261021-2254\frame-time-ab.txt"
BLOCK_S = 30.0
MARKER_WAIT_S = 3.0
LINE = re.compile(r"^(coop\.net\.enabled\s*=\s*)(true|false)\s*$", re.M)
KIND = re.compile(r'"kind":"([^"]+)"')

sys.stdout.reconfigure(encoding="utf-8", errors="replace")


def sha(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 16), b""):
            h.update(chunk)
    return h.hexdigest()


def log(line):
    text = "[%s] %s" % (time.strftime("%Y-%m-%d %H:%M:%S"), line)
    print(text, flush=True)
    with open(OUT, "a", encoding="utf-8") as f:
        f.write(text + "\n")


def read_cfg():
    with open(CFG, "r", encoding="utf-8", newline="") as f:
        return f.read()


def write_cfg(text):
    with open(CFG, "w", encoding="utf-8", newline="") as f:
        f.write(text)


def get_enabled(text):
    m = LINE.search(text)
    if not m:
        raise RuntimeError("coop.net.enabled line not found")
    return m.group(2) == "true"


def set_enabled(value):
    before_sha = sha(CFG)
    text = read_cfg()
    if get_enabled(text) == value:
        return before_sha, before_sha, False
    write_cfg(LINE.sub(lambda m: m.group(1) + ("true" if value else "false"), text, count=1))
    after_sha = sha(CFG)
    if get_enabled(read_cfg()) != value:
        raise RuntimeError("coop.net.enabled did not take effect")
    return before_sha, after_sha, True


class Tail:
    """byte-offset tail reader; returns raw lines"""

    def __init__(self, path):
        self.path = path
        self.off = 0
        self.buf = b""
        if os.path.exists(path):
            self.off = os.path.getsize(path)

    def new_lines(self):
        if not os.path.exists(self.path):
            return []
        size = os.path.getsize(self.path)
        if size < self.off:
            self.off = 0
        if size == self.off:
            return []
        with open(self.path, "rb") as f:
            f.seek(self.off)
            chunk = f.read()
        self.off = size
        self.buf += chunk
        *done, self.buf = self.buf.split(b"\n")
        return [raw.strip().decode("utf-8", "replace") for raw in done if raw.strip()]

    def wait(self, timeout):
        deadline = time.time() + timeout
        got = []
        while time.time() < deadline:
            got += self.new_lines()
            if got:
                break
            time.sleep(0.1)
        return got


def samples(lines):
    out = []
    for raw in lines:
        try:
            o = json.loads(raw)
        except Exception:
            continue
        if "nowActiveStateName" in o:
            out.append(o)
    return out


def markers(lines, want=None):
    out = []
    for raw in lines:
        if '"marker":true' not in raw:
            continue
        m = KIND.search(raw)
        if not m:
            continue
        kind = m.group(1)
        if want is None or kind in want:
            out.append((kind, raw))
    return out


def main():
    log("# ===== A-B-A-B attempt 2 (exit 3, mean only) =====")
    sampler = Tail(SAMPLER)
    snap = Tail(SNAP)

    pre = samples(sampler.wait(2.0))
    if not pre:
        log("STOP pre-A1: sampler not growing")
        return 2
    bad = [r for r in pre if r["nowActiveStateName"] != "WaitPlayerInput"]
    if bad:
        log("STOP pre-A1: state %s" % bad[-1].get("nowActiveStateName"))
        return 2
    log("pre-A1 OK samples=%d state=WaitPlayerInput turn=%s battleTime=%.3f frame=%s"
        % (len(pre), pre[-1].get("turnIndex"), pre[-1].get("battleTime", -1), pre[-1].get("frame")))

    blocks = [("A1", False), ("B1", True), ("A2", False), ("B2", True)]
    for idx, (name, enabled) in enumerate(blocks):
        pre_block = samples(sampler.wait(2.0))
        if not pre_block:
            log("STOP before %s: sampler not growing" % name)
            return 2
        if get_enabled(read_cfg()) != enabled:
            # make sure we start each block in the intended state
            pass
        if idx == 0:
            before_sha = after_sha = sha(CFG)
            switched = False
        else:
            before_sha, after_sha, switched = set_enabled(enabled)
        if idx > 0 and not switched:
            log("STOP before %s: pref already %s (switch did not happen)" % (name, enabled))
            return 2

        t0 = time.time()
        log("BLOCK %s start=%s enabled=%s cfg_sha_before=%s cfg_sha_after=%s switched=%s"
            % (name, time.strftime("%H:%M:%S", time.localtime(t0)), enabled, before_sha, after_sha, switched))

        if switched:
            expect = "phase.start" if enabled else "phase.end"
            deadline = time.time() + MARKER_WAIT_S
            found = None
            while time.time() < deadline and found is None:
                for kind, raw in markers(snap.new_lines(), {expect, "phase.idle", "selftest", "selftest.fail", "selftest.error"}):
                    if kind == expect:
                        found = raw
                    elif kind.startswith("selftest"):
                        log("marker %s: %s" % (kind, raw[:400]))
                if found is None:
                    time.sleep(0.1)
            if found is None:
                log("STOP after switch to %s: marker %s missing within %.1fs" % (name, expect, MARKER_WAIT_S))
                return 3
            log("marker %s seen: %s" % (expect, found[:400]))

        end = t0 + BLOCK_S
        n = 0
        states = {}
        while time.time() < end:
            for r in samples(sampler.new_lines()):
                n += 1
                states[r["nowActiveStateName"]] = states.get(r["nowActiveStateName"], 0) + 1
            time.sleep(0.2)
        dt = time.time() - t0
        other = {s: c for s, c in states.items() if s != "WaitPlayerInput"}
        log("BLOCK %s end=%s duration=%.2fs samples=%d states=%s"
            % (name, time.strftime("%H:%M:%S"), dt, n, json.dumps(states, ensure_ascii=True)))
        if other:
            log("STOP during %s: state left WaitPlayerInput -> %s" % (name, json.dumps(other, ensure_ascii=True)))
            return 4
        if n < BLOCK_S * 0.8:
            log("STOP during %s: only %d samples -> sampler stalled" % (name, n))
            return 2

    log("BLOCK B2 end: leaving coop.net.enabled=true for exit 4 (protocol v2 section 3)")
    log("# ===== A-B-A-B attempt 2 done =====")
    return 0


if __name__ == "__main__":
    sys.exit(main())
