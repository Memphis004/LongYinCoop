#!/usr/bin/env python3
"""S1.5-f: negative-test runner for exit criterion 3 (+ criterion 9 panic switch).

Sends `spike` commands straight to the mod pipe through `coopctl.call`, i.e. it
bypasses coopctl's prefire checks on purpose: those checks refuse client-side
exactly the commands whose server-side guards we must prove here.

Every case must satisfy all three:
  1) the mod answers with the expected error code (its own guard fired),
  2) `stateHash` is identical before/after (nothing applied, nothing moved),
  3) the reply is a failure (no ok=true).

Usage:
    python tools/negative-tests.py --plan plan-w1.json --out logs/w1.json
Plan JSON:
    {
      "label": "S1.5f-20261021-0300",
      "window": "w1",
      "pref": true,              # coop.allowRemoteInput during this window
      "settle_ms": 500,
      "cases": [
        {"n": 1, "group": "DISARMED", "cmd": "move",
         "args": {"id": "0:0", "row": 5, "col": 0},
         "arm": 0, "arm_wait_ms": 0, "expect": "DISARMED"},
        {"n": 3, "group": "NOT_ACTIVE", "cmd": "attack",
         "args": {"id": "0:9", "target": "1:10066"}, "arm": 120,
         "expect": "is not the active unit"}
      ]
    }

Exit codes: 0 = every case PASS, 1 = a case FAILED (stopped there), 2 = aborted
before firing (state not settled / IPC error) - nothing was sent in that case.
"""
import argparse
import importlib.util
import json
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location("coopctl", os.path.join(HERE, "coopctl.py"))
coopctl = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(coopctl)


def now_ms():
    return int(time.time() * 1000)


def status(pid):
    res = coopctl.call(pid, "spike", extra={"sub": "status"})
    if not res.get("ok"):
        raise RuntimeError("spike status failed: %s" % (res.get("error") or res))
    return res["result"]


def set_pref(pid, key, value):
    res = coopctl.call(pid, "set_pref", extra={"key": key, "value": "true" if value else "false"})
    if not res.get("ok"):
        raise RuntimeError("set_pref %s failed: %s" % (key, res.get("error") or res))
    return res["result"]


def send(pid, cmd, args):
    extra = {"sub": cmd}
    extra.update(args)
    return coopctl.call(pid, "spike", extra=extra)


def err_text(res):
    if res.get("error"):
        return str(res["error"])
    r = res.get("result") or {}
    return str(r.get("Error") or r.get("error") or "")


def wait_stable(pid, settle_ms, tries=6):
    """Two reads of stateHash `settle_ms` apart must agree, else the game is moving."""
    prev = status(pid)["stateHash"]
    for _ in range(tries):
        time.sleep(settle_ms / 1000.0)
        cur = status(pid)["stateHash"]
        if cur == prev:
            return prev
        prev = cur
    return None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--plan", required=True)
    ap.add_argument("--out", default=None)
    ap.add_argument("--pid", type=int, default=None)
    args = ap.parse_args()

    plan = json.load(open(args.plan, encoding="utf-8"))
    cases = plan["cases"]
    settle_ms = int(plan.get("settle_ms", 500))
    label = plan.get("label", "?")
    window = plan.get("window", "?")

    pid = args.pid or coopctl.find_pid()
    if not pid:
        print("ABORT: game not running")
        return 2

    pref = bool(plan.get("pref", True))
    set_pref(pid, "allowRemoteInput", pref)
    if not pref:
        coopctl.call(pid, "spike", extra={"sub": "disarm"})

    base_hash = wait_stable(pid, settle_ms)
    if base_hash is None:
        print("ABORT: stateHash keeps changing (state not settled) - nothing fired")
        return 2

    print("# window=%s label=%s pid=%s pref=%s hash0=%s" % (window, label, pid, pref, base_hash[:16]))
    print("%-4s %-14s %-7s %-8s %-6s %-6s %s" % ("#", "group", "cmd", "result", "hash=", "ok=", "error"))

    rows = []
    failed = False
    for c in cases:
        n = c.get("n")
        group = c.get("group", "")
        cmd = c["cmd"]
        cpref = c.get("pref", pref)
        if cpref != pref:
            set_pref(pid, "allowRemoteInput", cpref)
            pref = cpref
            if not pref:
                coopctl.call(pid, "spike", extra={"sub": "disarm"})

        h0 = status(pid)["stateHash"]

        armed = None
        arm_s = int(c.get("arm", 0) or 0)
        if arm_s > 0:
            res = coopctl.call(pid, "spike", extra={"sub": "arm", "seconds": arm_s})
            armed = bool((res.get("result") or {}).get("armed")) if res.get("ok") else False
            if not res.get("ok"):
                armed = False
            wait_ms = int(c.get("arm_wait_ms", 0) or 0)
            if wait_ms > 0:
                time.sleep(wait_ms / 1000.0)

        t0 = now_ms()
        res = send(pid, cmd, c["args"])
        cost = now_ms() - t0

        time.sleep(settle_ms / 1000.0)
        h1 = status(pid)["stateHash"]

        err = err_text(res)
        okflag = bool((res.get("result") or {}).get("Ok"))
        expect = c["expect"]
        hash_same = (h0 == h1)
        code_ok = expect.lower() in err.lower()
        passed = (not res.get("ok")) and (not okflag) and hash_same and code_ok

        row = {
            "n": n, "group": group, "cmd": cmd, "args": c["args"],
            "arm": arm_s, "armed": armed,
            "pref": cpref,
            "hash_before": h0, "hash_after": h1, "hash_same": hash_same,
            "reply_ok": res.get("ok"), "applied_flag": okflag,
            "error": err, "expect": expect, "code_ok": code_ok,
            "latency_ms": cost, "pass": passed,
            "ts": time.strftime("%Y-%m-%d %H:%M:%S"),
        }
        rows.append(row)
        print("%-4s %-14s %-7s %-8s %-6s %-6s %s" % (
            n, group, cmd,
            "PASS" if passed else "FAIL",
            "same" if hash_same else "DIFF",
            str(okflag),
            (err[:90] or "-")))

        if not passed:
            failed = True
            break

    coopctl.call(pid, "spike", extra={"sub": "disarm"})
    set_pref(pid, "allowRemoteInput", False)

    out = args.out or os.path.join(HERE, "..", "coop-test", label, "negatives-%s.json" % window)
    out = os.path.abspath(out)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    record = {
        "label": label, "window": window, "pid": pid, "settle_ms": settle_ms,
        "base_hash": base_hash, "cases": rows,
        "pass": sum(1 for r in rows if r["pass"]), "fail": sum(1 for r in rows if not r["pass"]),
    }
    with open(out, "w", encoding="utf-8") as f:
        json.dump(record, f, indent=2, ensure_ascii=False)
        f.write("\n")
    print("\n%s  %d/%d PASS  -> %s" % ("FAILED" if failed else "OK", record["pass"], len(rows), out))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
