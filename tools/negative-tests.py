#!/usr/bin/env python3
"""S1.5-f: negative-test runner for exit criterion 3 (+ criterion 9 panic switch).

Sends `spike` commands straight to the mod pipe through `coopctl.call`, i.e. it
bypasses coopctl's prefire checks on purpose: those checks refuse client-side
exactly the commands whose server-side guards we must prove here.

Every case must satisfy all three:
  1) the mod answers with the expected error code (its own guard fired),
  2) `stateHash` is identical before/after (nothing applied, nothing moved),
  3) the reply is a failure (no ok=true).

Gate-3 follow-up (2026-10-25): before firing each case the runner re-reads live
state (`spike status`, read-only) and validates the case premise against THAT
read — never against cached IDs/arm state from an older battle:
  - state unreadable / stateHash unstable          -> NOT-RUN (nothing fired)
  - plan IDs absent from the live unit list        -> NOT-RUN (never trust IDs
    from S1.5f or an older battle; verify against the current status)
  - group premise mismatch                         -> NOT-RUN (reason recorded)
  - DISARMED: the arm window must be verifiably lapsed (session-fact); if the
    arm state is unverifiable or still active -> NOT-RUN, fail closed. Attack
    cases additionally require the attacker to exist in the live units[].
  - OUT_OF_RANGE: the status surface exposes only attackRangeCount, not the
    range CELLS -> range membership is unverifiable at runtime -> NOT-RUN
    (premise-unverifiable-range) until the mod exposes the cells. The function
    accepts an injected `range_verdict` (True=in range, False=out of range) so
    offline tests can cover every branch.
  - IPC error / connection lost / heartbeat loss   -> record UNKNOWN when the
    audit file is still writable, then ABORT the whole run (no auto-retry).
    Results are appended to an NDJSON file and flushed after every record; if
    the audit file cannot be written the runner stops sending immediately.

Rows record: case ID, prechecks (each check ok/detail), decision (RUN/NOT-RUN),
result (PASS/FAIL/UNKNOWN/null for NOT-RUN), command outcome when fired, hash
before/after when readable, and a timestamp. Missing records are never counted
as PASS: the summary reports cases_planned vs records_written.

Exit codes: 0 = every case PASS or NOT-RUN, 1 = a fired case FAILED (stopped
there), 2 = aborted before firing, 3 = aborted mid-run (connection lost /
audit-trail write failed).
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


def read_status(pid):
    """Read spike status; returns (state, None) or (None, error_text)."""
    try:
        return status(pid), None
    except Exception as e:
        return None, "%s: %s" % (type(e).__name__, e)


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


def wait_stable_state(pid, settle_ms, tries=6):
    """Read the FULL state (not only the hash) twice, settle_ms apart; returns
    (state, None) when the two stateHash values agree, else (None, reason)."""
    s0, err = read_status(pid)
    if err:
        return None, "state-unreadable: %s" % err
    prev = s0.get("stateHash")
    for _ in range(tries):
        time.sleep(settle_ms / 1000.0)
        s1, err = read_status(pid)
        if err:
            return None, "state-unreadable: %s" % err
        cur = s1.get("stateHash")
        if cur == prev:
            return s1, None
        prev = cur
    return None, "hash-unstable"


def unit_by_id(state, uid):
    for u in state.get("units") or []:
        if u.get("id") == uid:
            return u
    return None


def range_verdict_from_state(state, target_id):
    """OUT_OF_RANGE premise verdict จาก status สด (NEG20 follow-up, 2026-10-25).

    อ่าน field `attackRangeCells` ([[row,col],...] จาก list เดียวกับ attack path)
    และตรวจ consistency กับ `attackRangeCount` (ต้องเท่ากัน — กัน serialization bug)

    คืน: True = target อยู่ในระยะ · False = อยู่นอกระยะ · None = ตรวจไม่ได้
    (field หาย / payload null / target ไม่มีใน units / จำนวนไม่ตรง count /
     entry รูปร่างไม่ครบ) — None ⇒ OUT_OF_RANGE เคส NOT-RUN fail-closed เหมือนเดิม
    """
    cells = state.get("attackRangeCells")
    count = state.get("attackRangeCount")
    if not isinstance(cells, list):
        return None
    tgt = unit_by_id(state, target_id)
    if tgt is None:
        return None
    if not isinstance(count, int) or count < 0 or count != len(cells):
        return None
    cells_set = {(c[0], c[1]) for c in cells
                 if isinstance(c, (list, tuple)) and len(c) == 2}
    if len(cells_set) != len(cells):
        return None
    # True = target อยู่ในระยะ (เจอใน cells) · False = นอกระยะ (รวม list ว่าง)
    return (tgt.get("row"), tgt.get("col")) in cells_set


def pre_snapshot(state):
    """Live-state evidence subset recorded with every row."""
    keys = ("inBattle", "battleStateName", "nowActiveStateName", "active",
            "isPlayerTurn", "unitCount", "turnIndex", "gridW", "gridH",
            "attackRangeCount", "damageRangeCount", "paused", "stateHash")
    snap = {k: state.get(k) for k in keys}
    snap["attackRangeCells"] = state.get("attackRangeCells")
    snap["units"] = [
        {k: u.get(k) for k in ("id", "team", "row", "col", "hp", "alive",
                               "unitAlive", "playerControl")}
        for u in (state.get("units") or [])
    ]
    return snap


def check_preconditions(group, cmd, case, state, session_facts, range_verdict=None):
    """Validate the case premise against the freshly read live state.

    Pure function - never touches IPC. Returns a decision dict:
      {"can_fire": bool, "checks": [{check, ok, detail}], "notrun_reason": str|None}

    range_verdict (OUT_OF_RANGE only): None = unverifiable from the status
    surface (runtime default, fail closed); True = target confirmed IN range;
    False = target confirmed out of range. Injected by tests; a future mod
    surface that exposes attackRangeGridUnits cells would feed it for real.
    """
    checks = []

    def add(name, ok, detail=""):
        checks.append({"check": name, "ok": bool(ok), "detail": detail})
        return bool(ok)

    if not add("in-battle", state.get("inBattle") is True):
        return {"can_fire": False, "checks": checks, "notrun_reason": "not-in-battle"}
    if not add("battle-fighting", state.get("battleStateName") == "Fighting",
               "battleState=%s" % state.get("battleStateName")):
        return {"can_fire": False, "checks": checks, "notrun_reason": "precondition-mismatch"}

    active = state.get("active")
    args = case.get("args") or {}
    cid, tid = args.get("id"), args.get("target")

    if group == "DISARMED":
        # arm state is not exposed by the status surface: the premise must come
        # from this session's own arm accounting (startup disarm + tracked
        # windows). If it cannot be verified -> fail closed, never fire.
        if cmd == "attack":
            if not add("attacker-in-live-units", unit_by_id(state, cid) is not None, str(cid)):
                return {"can_fire": False, "checks": checks,
                        "notrun_reason": "id-not-in-live-units"}
        case_arm = int(case.get("arm", 0) or 0)
        if case_arm > 0:
            waited = int(case.get("arm_wait_ms", 0) or 0)
            if not add("arm-window-lapsed(case-fact)", waited >= case_arm * 1000,
                       "arm=%ss waited=%sms" % (case_arm, waited)):
                return {"can_fire": False, "checks": checks, "notrun_reason": "arm-window-active"}
        until = session_facts.get("arm_active_until_ms")
        if until is None:
            add("arm-state-verifiable", False,
                "session arm state unknown - fail closed (surface does not expose arm state)")
            return {"can_fire": False, "checks": checks, "notrun_reason": "arm-state-unverifiable"}
        if not add("arm-window-lapsed(session-fact)", until <= now_ms(),
                   "arm_active_until_ms=%s" % until):
            return {"can_fire": False, "checks": checks, "notrun_reason": "arm-window-active"}
        return {"can_fire": True, "checks": checks, "notrun_reason": None}

    if group == "NOT_ACTIVE":
        a = add("attacker-in-live-units", unit_by_id(state, cid) is not None, str(cid))
        b = add("attacker-is-not-active", cid is not None and cid != active,
                "active=%s" % (active or "-"))
        ok = a and b
        return {"can_fire": ok, "checks": checks,
                "notrun_reason": None if ok else "precondition-mismatch"}

    if group == "OUT_OF_RANGE":
        au, tu = unit_by_id(state, cid), unit_by_id(state, tid)
        a = add("attacker-is-active", au is not None and active == cid,
                "active=%s" % (active or "-"))
        b = add("attacker-alive", bool(au and au.get("unitAlive")), str(cid))
        c = add("target-exists", tu is not None, str(tid))
        d = add("target-alive", bool(tu and tu.get("unitAlive")), str(tid))
        if not (a and b and c and d):
            return {"can_fire": False, "checks": checks,
                    "notrun_reason": "precondition-mismatch"}
        if range_verdict is None:
            add("range-membership-verifiable", False,
                "attackRangeGridUnits cells are not exposed by the spike status "
                "surface - the out-of-range premise cannot be confirmed from "
                "live state (needs a mod-surface change; separate approval)")
            return {"can_fire": False, "checks": checks,
                    "notrun_reason": "premise-unverifiable-range"}
        ok = add("target-out-of-range(confirmed)", range_verdict is False,
                 "range_verdict=%s (True=in range, False=out of range)" % range_verdict)
        return {"can_fire": ok, "checks": checks,
                "notrun_reason": None if ok else "premise-mismatch"}

    if group == "TARGET_DEAD":
        tu = unit_by_id(state, tid)
        a = add("target-in-live-units", tu is not None, str(tid))
        b = add("target-dead-unitAlive-false",
                bool(tu and tu.get("unitAlive") is False),
                "unitAlive=%s" % (None if tu is None else tu.get("unitAlive")))
        c = add("attacker-is-active", active == cid, "active=%s" % (active or "-"))
        ok = a and b and c
        return {"can_fire": ok, "checks": checks,
                "notrun_reason": None if ok else "precondition-mismatch"}

    if group == "STATE":
        want = "WaitAttacking" if cmd == "move" else "WaitPlayerInput"
        ok = add("current-phase-%s" % want,
                 state.get("nowActiveStateName") == want,
                 "nowActiveState=%s" % state.get("nowActiveStateName"))
        if ok and cmd == "attack":
            # STATE 17-18 (attack while waiting for input): the premise also
            # requires the attacker to be the active unit and the target to be
            # present in the live unit list.
            ok = add("attacker-is-active", active == cid, "active=%s" % (active or "-"))
            ok = ok and add("target-exists", unit_by_id(state, tid) is not None, str(tid))
        return {"can_fire": ok, "checks": checks,
                "notrun_reason": None if ok else "precondition-mismatch"}

    if group == "SELF":
        a = add("attacker-is-active", active == cid, "active=%s" % (active or "-"))
        b = add("target-equals-attacker", tid is not None and tid == cid,
                "%s vs %s" % (tid, cid))
        ok = a and b
        return {"can_fire": ok, "checks": checks,
                "notrun_reason": None if ok else "precondition-mismatch"}

    if group == "GRID":
        gw, gh = state.get("gridW"), state.get("gridH")
        row, col = int(args.get("row", -1)), int(args.get("col", -1))
        a = add("attacker-is-active", active == cid, "active=%s" % (active or "-"))
        b = add("coords-outside-live-grid",
                gw is not None and gh is not None and (row >= gw or col >= gh),
                "grid=%sx%s target=(%s,%s)" % (gw, gh, row, col))
        ok = a and b
        return {"can_fire": ok, "checks": checks,
                "notrun_reason": None if ok else "precondition-mismatch"}

    add("unknown-group", False, str(group))
    return {"can_fire": False, "checks": checks, "notrun_reason": "unknown-group"}


def maybe_send(decision, sender, cmd, args):
    """Fire the command only when the decision allows it.

    `sender(cmd, args)` is injectable so offline tests can prove that NOT-RUN
    decisions never reach a sender. Returns (sent, reply).
    """
    if not decision.get("can_fire"):
        return False, None
    return True, sender(cmd, args)


def write_record(nd, record):
    """Append one NDJSON record and flush - the audit trail must survive a
    runner/game crash, so nothing is buffered past the record boundary."""
    nd.write(json.dumps(record, ensure_ascii=False) + "\n")
    nd.flush()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--plan", required=True)
    ap.add_argument("--out", default=None)
    ap.add_argument("--pid", type=int, default=None)
    args = ap.parse_args()

    with open(args.plan, encoding="utf-8") as pf:
        plan = json.load(pf)
    cases = plan["cases"]
    settle_ms = int(plan.get("settle_ms", 500))
    label = plan.get("label", "?")
    window = plan.get("window", "?")

    pid = args.pid or coopctl.find_pid()
    if not pid:
        print("ABORT: game not running")
        return 2

    out = args.out or os.path.join(HERE, "..", "coop-test", label, "negatives-%s.json" % window)
    out = os.path.abspath(out)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    ndjson_path = out[:-5] + ".ndjson" if out.endswith(".json") else out + ".ndjson"

    try:
        nd = open(ndjson_path, "a", encoding="utf-8")
    except Exception as e:
        print("ABORT: cannot open NDJSON audit file (%s): %s - no commands will be sent"
              % (ndjson_path, e))
        return 2

    def emit(record, case_id=None, kind=None, result=None):
        """Append + flush one audit record; False when the trail is lost.

        case_id/kind/result feed the per-case accounting (pre_fire / notrun /
        terminal / abort) used by the summary to classify cases that lack a
        terminal outcome as UNKNOWN/INCOMPLETE - never as PASS.
        """
        try:
            write_record(nd, record)
        except Exception as e:
            print("AUDIT-TRAIL WRITE FAILED: %s - stopping, no further commands" % e)
            return False
        nd_stats["records"] += 1
        if case_id is not None and kind:
            acc = case_account.setdefault(case_id, {})
            if kind == "pre_fire":
                acc["pre_fire"] = acc.get("pre_fire", 0) + 1
            elif kind == "notrun":
                acc["notrun"] = acc.get("notrun", 0) + 1
                acc["status"] = "NOT-RUN"
            elif kind == "abort":
                acc["abort"] = acc.get("abort", 0) + 1
                acc["status"] = "UNKNOWN"
            elif kind == "terminal":
                acc["terminal"] = acc.get("terminal", 0) + 1
                acc["status"] = result
        return True

    pref = bool(plan.get("pref", True))
    session_facts = {"arm_active_until_ms": 0, "startup_disarm": False}
    case_account = {}       # case_id -> {"pre_fire"/"notrun"/"abort"/"terminal" counts, "status"}
    nd_stats = {"records": 0}
    try:
        set_pref(pid, "allowRemoteInput", pref)
        # establish a known arm state for this session: any older arm (e.g. from
        # a crashed runner) is cleared so DISARMED cases are verifiable
        coopctl.call(pid, "spike", extra={"sub": "disarm"})
        session_facts["startup_disarm"] = True
        if not pref:
            pass  # already disarmed above; pref stays false for the whole window
    except Exception as e:
        print("ABORT: setup IPC failed: %s" % e)
        return 2

    base_state, err = wait_stable_state(pid, settle_ms)
    if base_state is None:
        print("ABORT: %s - nothing fired" % err)
        return 2
    base_hash = base_state.get("stateHash")

    print("# window=%s label=%s pid=%s pref=%s hash0=%s"
          % (window, label, pid, pref, (base_hash or "-")[:16]))
    print("%-4s %-14s %-7s %-10s %-8s %-6s %-6s %s"
          % ("#", "group", "cmd", "result", "hash=", "ok=", "sent=", "error/reason"))

    rows = []
    failed = False
    aborted = None
    for c in cases:
        n = c.get("n")
        group = c.get("group", "")
        cmd = c["cmd"]
        cpref = c.get("pref", pref)
        ts = time.strftime("%Y-%m-%d %H:%M:%S")

        try:
            if cpref != pref:
                set_pref(pid, "allowRemoteInput", cpref)
                pref = cpref
        except Exception as e:
            aborted = "connection-lost during set_pref: %s" % e
            break

        try:
            state, err = wait_stable_state(pid, settle_ms)
        except Exception as e:
            state, err = None, "exception: %s: %s" % (type(e).__name__, e)
        if state is None:
            if "state-unreadable" in (err or ""):
                # heartbeat/crash candidate: record UNKNOWN (the audit file is
                # still writable) then stop the whole run - no auto-retry
                aborted = err
                rec = {"ts": ts, "label": label, "window": window, "case_id": n,
                       "group": group, "cmd": cmd, "args": c.get("args"),
                       "decision": "NOT-RUN", "result": "UNKNOWN",
                       "aborted_reason": err, "notrun_reason": err,
                       "prechecks": [], "pre_state": None,
                       "hash_before": None, "hash_after": None}
                if not emit(rec, n, "abort", "UNKNOWN"):
                    aborted = "audit-trail-write-failed"
                rows.append({"n": n, "group": group, "cmd": cmd,
                             "args": c.get("args"), "result": "UNKNOWN",
                             "pass": False, "aborted_reason": aborted, "ts": ts})
                print("%-4s %-14s %-7s %-10s %-8s %-6s %-6s %s"
                      % (n, group, cmd, "UNKNOWN", "-", "-", "-", (err or "")[:90]))
                break
            rec = {"ts": ts, "label": label, "window": window, "case_id": n,
                   "group": group, "cmd": cmd, "args": c.get("args"),
                   "decision": "NOT-RUN", "result": "NOT-RUN", "notrun_reason": err,
                   "prechecks": [], "pre_state": None,
                   "hash_before": None, "hash_after": None}
            if not emit(rec, n, "notrun", "NOT-RUN"):
                aborted = "audit-trail-write-failed"
                break
            rows.append({"n": n, "group": group, "cmd": cmd, "args": c.get("args"),
                         "result": "NOT-RUN", "pass": False, "notrun_reason": err,
                         "ts": ts})
            print("%-4s %-14s %-7s %-10s %-8s %-6s %-6s %s"
                  % (n, group, cmd, "NOT-RUN", "-", "-", "-", (err or "")[:90]))
            continue

        pre = pre_snapshot(state)
        # NEG20 follow-up: OUT_OF_RANGE — derive the range verdict from the live
        # status (attackRangeCells, same list as the attack path). None (field
        # missing / count mismatch / target absent) keeps the fail-closed NOT-RUN.
        rv = None
        if group == "OUT_OF_RANGE":
            rv = range_verdict_from_state(state, (c.get("args") or {}).get("target"))
        decision = check_preconditions(group, cmd, c, state, session_facts, range_verdict=rv)
        if not decision["can_fire"]:
            rec = {"ts": ts, "label": label, "window": window, "case_id": n,
                   "group": group, "cmd": cmd, "args": c.get("args"),
                   "decision": "NOT-RUN", "result": "NOT-RUN",
                   "notrun_reason": decision["notrun_reason"],
                   "prechecks": decision["checks"], "pre_state": pre,
                   "hash_before": pre.get("stateHash"), "hash_after": None,
                   "expect": c["expect"]}
            if not emit(rec, n, "notrun", "NOT-RUN"):
                aborted = "audit-trail-write-failed"
                break
            rows.append({"n": n, "group": group, "cmd": cmd, "args": c.get("args"),
                         "result": "NOT-RUN", "pass": False,
                         "notrun_reason": decision["notrun_reason"],
                         "prechecks": decision["checks"], "pre_state": pre,
                         "hash_before": pre.get("stateHash"),
                         "expect": c["expect"], "ts": ts})
            reason = decision["notrun_reason"] or "precondition-mismatch"
            print("%-4s %-14s %-7s %-10s %-8s %-6s %-6s %s"
                  % (n, group, cmd, "NOT-RUN", "-", "-", "-", reason[:90]))
            continue

        h0 = pre.get("stateHash")
        # pre-fire audit record: the trail must show the RUN decision even if
        # the game dies between firing and the post-state read
        if not emit({"ts": ts, "label": label, "window": window, "case_id": n,
                     "group": group, "cmd": cmd, "args": c.get("args"),
                     "decision": "RUN", "phase": "pre-fire",
                     "prechecks": decision["checks"], "pre_state": pre,
                     "hash_before": h0, "hash_after": None, "expect": c["expect"]},
                    n, "pre_fire"):
            aborted = "audit-trail-write-failed"
            break

        armed = None
        arm_s = int(c.get("arm", 0) or 0)
        if arm_s > 0:
            try:
                res = coopctl.call(pid, "spike", extra={"sub": "arm", "seconds": arm_s})
                armed = bool((res.get("result") or {}).get("armed")) if res.get("ok") else False
                session_facts["arm_active_until_ms"] = now_ms() + arm_s * 1000
            except Exception as e:
                aborted = "connection-lost during arm: %s" % e
                break
            wait_ms = int(c.get("arm_wait_ms", 0) or 0)
            if wait_ms > 0:
                time.sleep(wait_ms / 1000.0)

        try:
            t0 = now_ms()
            res = send(pid, cmd, c["args"])
            cost = now_ms() - t0
        except Exception as e:
            aborted = "connection-lost during fire: %s" % e
            break

        try:
            time.sleep(settle_ms / 1000.0)
            post, err = read_status(pid)
        except Exception as e:
            post, err = None, "exception: %s: %s" % (type(e).__name__, e)
        if post is None:
            # fired but post-state unreadable (crash/heartbeat loss): record
            # what we have as UNKNOWN - never claim success from partial data
            aborted = "state-unreadable after fire: %s" % err
            rec = {"ts": time.strftime("%Y-%m-%d %H:%M:%S"), "label": label,
                   "window": window, "case_id": n, "group": group, "cmd": cmd,
                   "args": c.get("args"), "decision": "RUN", "phase": "post",
                   "result": "UNKNOWN", "aborted_reason": aborted,
                   "reply_outcome": {"reply_ok": res.get("ok") if isinstance(res, dict) else None,
                                     "error": err_text(res) if isinstance(res, dict) else None},
                   "hash_before": h0, "hash_after": None, "expect": c["expect"]}
            emit(rec, n, "terminal", "UNKNOWN")  # best effort; the run aborts regardless
            rows.append({"n": n, "group": group, "cmd": cmd, "args": c.get("args"),
                         "result": "UNKNOWN", "pass": False, "aborted_reason": aborted,
                         "hash_before": h0, "hash_after": None, "expect": c["expect"],
                         "ts": time.strftime("%Y-%m-%d %H:%M:%S")})
            print("%-4s %-14s %-7s %-10s %-8s %-6s %-6s %s"
                  % (n, group, cmd, "UNKNOWN", "?", "?", "-", aborted[:90]))
            break

        h1 = post.get("stateHash")
        err = err_text(res)
        okflag = bool((res.get("result") or {}).get("Ok"))
        expect = c["expect"]
        hash_same = (h0 == h1)
        code_ok = expect.lower() in err.lower()
        passed = (not res.get("ok")) and (not okflag) and hash_same and code_ok
        result = "PASS" if passed else "FAIL"

        ts_end = time.strftime("%Y-%m-%d %H:%M:%S")
        written = emit({"ts": ts_end, "label": label, "window": window,
                        "case_id": n, "group": group, "cmd": cmd,
                        "args": c.get("args"), "decision": "RUN",
                        "phase": "post", "result": result,
                        "reply_outcome": {"reply_ok": res.get("ok"),
                                          "applied_flag": okflag, "error": err,
                                          "expect": expect, "code_ok": code_ok},
                        "hash_before": h0, "hash_after": h1,
                        "hash_same": hash_same, "latency_ms": cost},
                       n, "terminal", result)
        if not written:
            # terminal record lost -> the case has only a pre-fire record:
            # downgrade to UNKNOWN/INCOMPLETE, never count it as PASS
            aborted = "audit-trail-write-failed"
            result = "UNKNOWN"
            failed = True
        row = {
            "n": n, "group": group, "cmd": cmd, "args": c["args"],
            "arm": arm_s, "armed": armed, "pref": cpref,
            "hash_before": h0, "hash_after": h1 if written else None,
            "hash_same": hash_same and written,
            "reply_ok": res.get("ok"), "applied_flag": okflag,
            "error": err, "expect": expect, "code_ok": code_ok,
            "latency_ms": cost, "pass": passed and written, "result": result,
            "ts": ts_end,
        }
        rows.append(row)
        print("%-4s %-14s %-7s %-10s %-8s %-6s %-6s %s"
              % (n, group, cmd, result, "same" if row["hash_same"] else "DIFF",
                 str(okflag), "yes", (err[:90] or "-")))

        if aborted:
            failed = True  # audit trail lost mid-run: never continue firing
            break
        if not passed:
            failed = True
            break

    cleanup_ok = True
    try:
        coopctl.call(pid, "spike", extra={"sub": "disarm"})
        set_pref(pid, "allowRemoteInput", False)
    except Exception:
        cleanup_ok = False  # connection may already be dead after a crash
    try:
        nd.close()
    except Exception:
        pass

    # ---- per-case accounting (NDJSON event records vs terminal results) ----
    # status per planned case: PASS/FAIL/UNKNOWN (terminal row), NOT-RUN
    # (notrun record), INCOMPLETE (pre-fire record but no terminal outcome),
    # NO-RECORD (nothing was written). INCOMPLETE/NO-RECORD are never PASS.
    case_status = {}
    for c in cases:
        cid = c.get("n")
        acc = case_account.get(cid)
        if not acc:
            case_status[str(cid)] = "NO-RECORD"
        elif acc.get("status"):
            case_status[str(cid)] = acc["status"]
        elif acc.get("pre_fire"):
            case_status[str(cid)] = "INCOMPLETE"
        else:
            case_status[str(cid)] = "NO-RECORD"
    incomplete_case_ids = [cid for cid, st in case_status.items()
                           if st in ("INCOMPLETE", "NO-RECORD")]

    record = {
        "label": label, "window": window, "pid": pid, "settle_ms": settle_ms,
        "base_hash": base_hash, "cases": rows,
        "pass": sum(1 for r in rows if r.get("result") == "PASS"),
        "fail": sum(1 for r in rows if r.get("result") == "FAIL"),
        "notrun": sum(1 for r in rows if r.get("result") == "NOT-RUN"),
        "unknown": sum(1 for r in rows if r.get("result") == "UNKNOWN"),
        "aborted": aborted, "cleanup_ok": cleanup_ok,
        "cases_planned": len(cases),
        "records_written": len(rows),
        # C-accounting: NDJSON event records (incl. pre-fire) are counted
        # separately from cases that reached a terminal result
        "event_records": nd_stats["records"],
        "terminal_cases": sum(1 for s in case_status.values()
                              if s in ("PASS", "FAIL", "UNKNOWN")),
        "case_status": case_status,
        "incomplete_case_ids": incomplete_case_ids,
    }
    if incomplete_case_ids or record["records_written"] != record["cases_planned"]:
        # missing records are never treated as PASS
        record["records_gap"] = True
    try:
        with open(out, "w", encoding="utf-8") as f:
            json.dump(record, f, indent=2, ensure_ascii=False)
            f.write("\n")
    except Exception as e:
        record["summary_write_error"] = str(e)
        print("WARNING: could not write summary JSON (%s) - the NDJSON audit "
              "trail is authoritative" % e)
    print("\n%s  pass=%d fail=%d notrun=%d unknown=%d (planned=%d written=%d) -> %s"
          % ("FAILED" if failed else ("ABORTED" if aborted else "OK"),
             record["pass"], record["fail"], record["notrun"], record["unknown"],
             record["cases_planned"], record["records_written"], out))
    if aborted:
        return 3
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
