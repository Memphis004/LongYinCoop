#!/usr/bin/env python3
"""S2a-2 exit 3: A1 -> B1 -> A2 -> B2 (30 s each) per evidence/ab-protocol-v2.md.

Read-only on the game: the ONLY thing changed is one line in
UserData\\MelonPreferences.cfg (`coop.net.enabled`). No IPC write path is used.

Stop conditions (protocol v2 section 6) abort the run immediately:
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

sys.stdout.reconfigure(encoding="utf-8", errors="replace")


def sha(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 16), b""):
            h.update(chunk)
    return h.hexdigest()


def log(line):
    stamp = time.strftime("%Y-%m-%d %H:%M:%S")
    text = "[%s] %s" % (stamp, line)
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
    cur = get_enabled(text)
    if cur == value:
        return before_sha, before_sha, False
    text = LINE.sub(lambda m: m.group(1) + ("true" if value else "false"), text, count=1)
    write_cfg(text)
    after_sha = sha(CFG)
    if get_enabled(read_cfg()) != value:
        raise RuntimeError("coop.net.enabled did not take effect")
    return before_sha, after_sha, True


class Tail:
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
        out = []
        for raw in done:
            raw = raw.strip()
            if not raw:
                continue
            try:
                out.append(json.loads(raw.decode("utf-8", "replace")))
            except Exception:
                out.append({"__unparsed__": raw[:200].decode("utf-8", "replace")})
        return out


def wait_lines(tail, timeout):
    """poll a tail until at least one new line appears (sampler runs at 2 Hz)"""
    deadline = time.time() + timeout
    got = []
    while time.time() < deadline:
        got += tail.new_lines()
        if got:
            break
        time.sleep(0.1)
    return got


def main():
    log("# ===== A-B-A-B start (exit 3, mean only) =====")
    sampler = Tail(SAMPLER)
    markers = Tail(SNAP)

    # --- pre-A1 gate: sampler must be growing, state must be WaitPlayerInput
    pre = [r for r in wait_lines(sampler, 2.0) if "nowActiveStateName" in r]
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
        # confirm sampler still growing right before the block
        probe = [r for r in wait_lines(sampler, 2.0) if "nowActiveStateName" in r]
        if not probe:
            log("STOP before %s: sampler not growing" % name)
            return 2

        if idx == 0:
            before_sha = sha(CFG)
            after_sha = before_sha
            switched = False
        else:
            before_sha, after_sha, switched = set_enabled(enabled)
        if not switched and idx > 0:
            log("STOP before %s: pref already %s (switch did not happen)" % (name, enabled))
            return 2

        t0 = time.time()
        log("BLOCK %s start=%s enabled=%s cfg_sha_before=%s cfg_sha_after=%s switched=%s"
            % (name, time.strftime("%H:%M:%S", time.localtime(t0)), enabled, before_sha, after_sha, switched))

        # after a switch: the mod must emit its phase marker within ~3 s
        if switched:
            expect = "phase.start" if enabled else "phase.end"
            deadline = time.time() + MARKER_WAIT_S
            found = None
            while time.time() < deadline and found is None:
                for m in markers.new_lines():
                    if m.get("marker") and m.get("kind") == expect:
                        found = m
                        break
                if found is None:
                    time.sleep(0.1)
            if found is None:
                log("STOP after switch to %s: marker %s missing within %.1fs" % (name, expect, MARKER_WAIT_S))
                return 3
            log("marker %s after %.2fs: %s"
                % (expect, time.time() - deadline + MARKER_WAIT_S,
                   json.dumps(found, ensure_ascii=True)[:400]))
            # also report the exit-6 selftest marker if it appeared
            for m in markers.new_lines():
                if m.get("kind", "").startswith("selftest"):
                    log("marker %s: %s" % (m.get("kind"), json.dumps(m, ensure_ascii=True)[:400]))

        # --- the block itself: hold still for BLOCK_S, verify every sample
        end = t0 + BLOCK_S
        n = 0
        states = {}
        while time.time() < end:
            for r in sampler.new_lines():
                if "nowActiveStateName" in r:
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
            log("STOP during %s: only %d samples (expected >=%.0f) -> sampler stalled" % (name, n, BLOCK_S * 0.8))
            return 2

    log("BLOCK B2 end: leaving coop.net.enabled=true for exit 4 (protocol v2 section 3)")
    log("# ===== A-B-A-B done =====")
    return 0


if __name__ == "__main__":
    sys.exit(main())
