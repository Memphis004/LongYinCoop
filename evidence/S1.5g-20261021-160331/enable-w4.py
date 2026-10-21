#!/usr/bin/env python3
"""S1.5g: wait for the game pipe, then enable the read-only pause-button hook.

Only sends `set_pref jitprobeW4=true` (Harmony postfix that READS arguments) and
`spike status` (read-only). It never sends a state-changing command and never
touches the save folder.
"""
import json
import sys
import time
import os

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "tools")))
import coopctl  # noqa: E402


def stamp():
    return time.strftime("%H:%M:%S")


def main():
    budget = float(sys.argv[1]) if len(sys.argv) > 1 else 1800.0
    end = time.time() + budget
    pid = None
    while time.time() < end and not pid:
        pid = coopctl.find_pid()
        if not pid:
            time.sleep(3)
    if not pid:
        print("[%s] timeout: game never appeared" % stamp(), flush=True)
        return 2
    print("[%s] game pid=%d" % (stamp(), pid), flush=True)

    f = None
    while f is None and time.time() < end:
        try:
            f = coopctl.connect(pid, retries=2)
        except Exception:
            time.sleep(2)
            pid = coopctl.find_pid() or pid
    if f is None:
        print("[%s] pipe never opened" % stamp(), flush=True)
        return 3

    print("[%s] pipe open" % stamp(), flush=True)
    st = coopctl.request(f, "spike", {"sub": "status"})
    r = st.get("result") or {}
    print("[%s] status inBattle=%s battleState=%s" % (stamp(), r.get("inBattle"), r.get("battleStateName")), flush=True)

    res = coopctl.request(f, "set_pref", {"key": "jitprobeW4", "value": "true"})
    print("[%s] set_pref jitprobeW4 -> %s" % (stamp(), json.dumps(res, ensure_ascii=False)), flush=True)

    f.close()
    return 0 if res.get("ok") else 4


if __name__ == "__main__":
    sys.exit(main())
