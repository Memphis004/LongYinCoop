#!/usr/bin/env python3
"""Offline unit tests for negative-tests.check_preconditions / maybe_send.

No game, no IPC: state dicts are fixtures and `maybe_send` receives a spy
sender, so NOT-RUN decisions are proven unable to reach the command sender.
Run:  python tools/test_negative_preconditions.py -v
"""
import importlib.util
import json
import os
import sys
import tempfile
import unittest
from unittest import mock

HERE = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location("nt", os.path.join(HERE, "negative-tests.py"))
nt = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(nt)


def unit(uid, team=0, row=6, col=0, unit_alive=True):
    return {"id": uid, "team": team, "row": row, "col": col, "hp": 100.0,
            "alive": unit_alive, "unitAlive": unit_alive, "playerControl": team == 0}


def state(units=None, in_battle=True, fighting=True, phase="WaitPlayerInput",
          active="0:0", grid=(18, 19)):
    return {
        "inBattle": in_battle,
        "battleStateName": "Fighting" if fighting else "Prepare",
        "nowActiveStateName": phase,
        "active": active,
        "unitCount": len(units or []),
        "gridW": grid[0], "gridH": grid[1],
        "units": units if units is not None else [],
    }


def live_units():
    return [unit("0:0", 0, 6, 0), unit("0:9", 0, 18, 0), unit("0:86", 0, 8, 0),
            unit("1:10066", 1, 7, 17), unit("1:10067", 1, 5, 17),
            unit("1:10107", 1, 1, 17), unit("1:10102", 1, 15, 17),
            unit("1:10105", 1, 17, 17)]


class SpySender:
    """Records every call - stands in for negative-tests.send over IPC."""

    def __init__(self):
        self.calls = []

    def __call__(self, cmd, args):
        self.calls.append((cmd, dict(args)))
        return {"ok": False, "error": "SPY-REPLY"}


class AliveRuleNotInvolvedPreconditionTests(unittest.TestCase):
    def d(self, group, cmd, case, st, session_facts=None, range_verdict=None):
        return nt.check_preconditions(group, cmd, case, st,
                                      session_facts if session_facts is not None
                                      else {"arm_active_until_ms": 0},
                                      range_verdict=range_verdict)

    # ------------------------------------------------ DISARMED (cases 1-2)
    def test_disarmed_move_lapsed_fires(self):
        case = {"n": 1, "group": "DISARMED", "cmd": "move",
                "args": {"id": "0:0", "row": 1, "col": 0}, "arm": 0}
        d = self.d("DISARMED", "move", case, state(units=live_units()))
        self.assertTrue(d["can_fire"])
        self.assertIsNone(d["notrun_reason"])

    def test_disarmed_attack_attacker_in_live_units_fires(self):
        case = {"n": 2, "group": "DISARMED", "cmd": "attack",
                "args": {"id": "0:0", "target": "1:10066"}, "arm": 1,
                "arm_wait_ms": 2200}
        d = self.d("DISARMED", "attack", case, state(units=live_units()))
        self.assertTrue(d["can_fire"])

    def test_disarmed_attack_attacker_absent_not_run(self):
        st = state(units=[unit("1:10066", 1, 7, 17)])
        case = {"n": 2, "group": "DISARMED", "cmd": "attack",
                "args": {"id": "9:999", "target": "1:10066"}, "arm": 1,
                "arm_wait_ms": 2200}
        d = self.d("DISARMED", "attack", case, st)
        self.assertFalse(d["can_fire"])
        self.assertEqual(d["notrun_reason"], "id-not-in-live-units")

    def test_disarmed_case_arm_window_active_not_run(self):
        case = {"n": 2, "group": "DISARMED", "cmd": "attack",
                "args": {"id": "0:0", "target": "1:10066"}, "arm": 120,
                "arm_wait_ms": 0}
        d = self.d("DISARMED", "attack", case, state(units=live_units()))
        self.assertFalse(d["can_fire"])
        self.assertEqual(d["notrun_reason"], "arm-window-active")

    def test_disarmed_session_arm_still_active_not_run(self):
        case = {"n": 1, "group": "DISARMED", "cmd": "move",
                "args": {"id": "0:0", "row": 1, "col": 0}, "arm": 0}
        future = nt.now_ms() + 60_000
        d = self.d("DISARMED", "move", case, state(units=live_units()),
                   {"arm_active_until_ms": future})
        self.assertFalse(d["can_fire"])
        self.assertEqual(d["notrun_reason"], "arm-window-active")

    def test_disarmed_arm_state_unverifiable_not_run(self):
        case = {"n": 1, "group": "DISARMED", "cmd": "move",
                "args": {"id": "0:0", "row": 1, "col": 0}, "arm": 0}
        d = self.d("DISARMED", "move", case, state(units=live_units()), {})
        self.assertFalse(d["can_fire"])
        self.assertEqual(d["notrun_reason"], "arm-state-unverifiable")

    # ------------------------------------------------ NOT_ACTIVE (cases 3-6)
    def test_not_active_present_and_not_active_fires(self):
        case = {"n": 3, "group": "NOT_ACTIVE", "cmd": "move",
                "args": {"id": "0:9", "row": 4, "col": 0}, "arm": 120}
        d = self.d("NOT_ACTIVE", "move", case, state(units=live_units()))
        self.assertTrue(d["can_fire"])

    def test_not_active_attacker_absent_not_run(self):
        case = {"n": 4, "group": "NOT_ACTIVE", "cmd": "move",
                "args": {"id": "0:77", "row": 4, "col": 0}, "arm": 120}
        d = self.d("NOT_ACTIVE", "move", case, state(units=live_units()))
        self.assertFalse(d["can_fire"])

    def test_not_active_attacker_is_active_not_run(self):
        case = {"n": 5, "group": "NOT_ACTIVE", "cmd": "attack",
                "args": {"id": "0:0", "target": "1:10066"}, "arm": 120}
        d = self.d("NOT_ACTIVE", "attack", case, state(units=live_units()))
        self.assertFalse(d["can_fire"])

    # ------------------------------------------------ OUT_OF_RANGE (cases 7-10)
    def test_case7_fixture_target_in_range_not_run_and_never_sent(self):
        """Fixture บังคับจากรีวิว (เหตุการณ์เคส 7 ของ Gate 2): เป้าอยู่ในระยะ
        -> ต้อง NOT-RUN และ sender ต้องไม่ถูกเรียกแม้แต่ครั้งเดียว"""
        st = state(units=[unit("0:0", 0, 6, 0), unit("1:10066", 1, 7, 17)],
                   active="0:0", phase="WaitAttacking")
        case = {"n": 7, "group": "OUT_OF_RANGE", "cmd": "attack",
                "args": {"id": "0:0", "target": "1:10066"}, "arm": 120}
        d = self.d("OUT_OF_RANGE", "attack", case, st, range_verdict=True)
        self.assertFalse(d["can_fire"])
        self.assertEqual(d["notrun_reason"], "premise-mismatch")
        confirmed = [c for c in d["checks"] if c["check"] == "target-out-of-range(confirmed)"]
        self.assertEqual(len(confirmed), 1 and 1)
        self.assertFalse(confirmed[0]["ok"])
        spy = SpySender()
        sent, reply = nt.maybe_send(d, spy, case["cmd"], case["args"])
        self.assertFalse(sent)
        self.assertIsNone(reply)
        self.assertEqual(spy.calls, [])

    def test_out_of_range_unverifiable_not_run(self):
        """รันไทม์ปัจจุบัน: surface ไม่ expose ช่องระยะ -> fail closed"""
        case = {"n": 8, "group": "OUT_OF_RANGE", "cmd": "attack",
                "args": {"id": "0:0", "target": "1:10107"}, "arm": 120}
        d = self.d("OUT_OF_RANGE", "attack", case, state(units=live_units()),
                   range_verdict=None)
        self.assertFalse(d["can_fire"])
        self.assertEqual(d["notrun_reason"], "premise-unverifiable-range")

    def test_out_of_range_confirmed_fires(self):
        """อนาคต: surface ให้ cells และยืนยันนอกระยะได้ -> ยิงได้"""
        case = {"n": 7, "group": "OUT_OF_RANGE", "cmd": "attack",
                "args": {"id": "0:0", "target": "1:10107"}, "arm": 120}
        d = self.d("OUT_OF_RANGE", "attack", case, state(units=live_units()),
                   range_verdict=False)
        self.assertTrue(d["can_fire"])

    def test_out_of_range_target_missing_not_run(self):
        case = {"n": 9, "group": "OUT_OF_RANGE", "cmd": "attack",
                "args": {"id": "0:0", "target": "9:999"}, "arm": 120}
        d = self.d("OUT_OF_RANGE", "attack", case, state(units=live_units()),
                   range_verdict=False)
        self.assertFalse(d["can_fire"])

    # ------------------------------------------------ TARGET_DEAD (cases 11-14)
    def test_target_dead_fires(self):
        st = state(units=[unit("0:0", 0, 6, 0), unit("1:10066", 1, 7, 17, unit_alive=False)],
                   active="0:0")
        case = {"n": 11, "group": "TARGET_DEAD", "cmd": "attack",
                "args": {"id": "0:0", "target": "1:10066"}, "arm": 120}
        d = self.d("TARGET_DEAD", "attack", case, st)
        self.assertTrue(d["can_fire"])

    def test_target_still_alive_not_run(self):
        case = {"n": 11, "group": "TARGET_DEAD", "cmd": "attack",
                "args": {"id": "0:0", "target": "1:10066"}, "arm": 120}
        d = self.d("TARGET_DEAD", "attack", case, state(units=live_units()))
        self.assertFalse(d["can_fire"])

    def test_target_missing_not_run(self):
        case = {"n": 12, "group": "TARGET_DEAD", "cmd": "attack",
                "args": {"id": "0:0", "target": "9:999"}, "arm": 120}
        d = self.d("TARGET_DEAD", "attack", case, state(units=live_units()))
        self.assertFalse(d["can_fire"])

    # ------------------------------------------------ STATE (cases 15-18)
    def test_state_move_in_wait_attacking_fires(self):
        case = {"n": 15, "group": "STATE", "cmd": "move",
                "args": {"id": "0:0", "row": 1, "col": 0}, "arm": 120}
        d = self.d("STATE", "move", case, state(units=live_units(), phase="WaitAttacking"))
        self.assertTrue(d["can_fire"])

    def test_state_attack_ready_fires(self):
        """เคส 17-18: phase + attacker-is-active + target-exists ครบ"""
        case = {"n": 17, "group": "STATE", "cmd": "attack",
                "args": {"id": "0:0", "target": "1:10066"}, "arm": 120}
        d = self.d("STATE", "attack", case, state(units=live_units(), phase="WaitPlayerInput"))
        self.assertTrue(d["can_fire"])

    def test_state_attack_phase_mismatch_not_run(self):
        case = {"n": 17, "group": "STATE", "cmd": "attack",
                "args": {"id": "0:0", "target": "1:10066"}, "arm": 120}
        d = self.d("STATE", "attack", case, state(units=live_units(), phase="WaitAttacking"))
        self.assertFalse(d["can_fire"])
        self.assertEqual(d["notrun_reason"], "precondition-mismatch")

    def test_state_attack_attacker_not_active_not_run(self):
        case = {"n": 17, "group": "STATE", "cmd": "attack",
                "args": {"id": "0:9", "target": "1:10066"}, "arm": 120}
        d = self.d("STATE", "attack", case, state(units=live_units(), phase="WaitPlayerInput"))
        self.assertFalse(d["can_fire"])

    def test_state_attack_target_missing_not_run(self):
        case = {"n": 18, "group": "STATE", "cmd": "attack",
                "args": {"id": "0:0", "target": "9:999"}, "arm": 120}
        d = self.d("STATE", "attack", case, state(units=live_units(), phase="WaitPlayerInput"))
        self.assertFalse(d["can_fire"])

    # ------------------------------------------------ SELF (case 19)
    def test_self_fires(self):
        case = {"n": 19, "group": "SELF", "cmd": "attack",
                "args": {"id": "0:0", "target": "0:0"}, "arm": 120}
        d = self.d("SELF", "attack", case, state(units=live_units()))
        self.assertTrue(d["can_fire"])

    # ------------------------------------------------ GRID (case 20)
    def test_grid_outside_live_grid_fires(self):
        case = {"n": 20, "group": "GRID", "cmd": "move",
                "args": {"id": "0:0", "row": 99, "col": 99}, "arm": 120}
        d = self.d("GRID", "move", case, state(units=live_units(), grid=(18, 19)))
        self.assertTrue(d["can_fire"])

    def test_grid_inside_live_grid_not_run(self):
        case = {"n": 20, "group": "GRID", "cmd": "move",
                "args": {"id": "0:0", "row": 5, "col": 5}, "arm": 120}
        d = self.d("GRID", "move", case, state(units=live_units(), grid=(18, 19)))
        self.assertFalse(d["can_fire"])

    # ------------------------------------------------ missing / partial state
    def test_not_in_battle_not_run(self):
        case = {"n": 3, "group": "NOT_ACTIVE", "cmd": "move",
                "args": {"id": "0:9", "row": 4, "col": 0}, "arm": 120}
        d = self.d("NOT_ACTIVE", "move", case, state(units=live_units(), in_battle=False))
        self.assertFalse(d["can_fire"])
        self.assertEqual(d["notrun_reason"], "not-in-battle")

    def test_not_fighting_not_run(self):
        case = {"n": 3, "group": "NOT_ACTIVE", "cmd": "move",
                "args": {"id": "0:9", "row": 4, "col": 0}, "arm": 120}
        d = self.d("NOT_ACTIVE", "move", case, state(units=live_units(), fighting=False))
        self.assertFalse(d["can_fire"])
        self.assertEqual(d["notrun_reason"], "precondition-mismatch")

    def test_units_missing_not_run(self):
        case = {"n": 3, "group": "NOT_ACTIVE", "cmd": "move",
                "args": {"id": "0:9", "row": 4, "col": 0}, "arm": 120}
        d = self.d("NOT_ACTIVE", "move", case, state(units=None))
        self.assertFalse(d["can_fire"])

    def test_unknown_group_not_run(self):
        case = {"n": 99, "group": "MYSTERY", "cmd": "move", "args": {}, "arm": 0}
        d = self.d("MYSTERY", "move", case, state(units=live_units()))
        self.assertFalse(d["can_fire"])
        self.assertEqual(d["notrun_reason"], "unknown-group")

    # ------------------------------------------------ maybe_send separation
    def test_maybe_send_run_reaches_sender(self):
        decision = {"can_fire": True, "checks": [], "notrun_reason": None}
        spy = SpySender()
        sent, reply = nt.maybe_send(decision, spy, "move", {"id": "0:0"})
        self.assertTrue(sent)
        self.assertEqual(reply, {"ok": False, "error": "SPY-REPLY"})
        self.assertEqual(spy.calls, [("move", {"id": "0:0"})])

    def test_maybe_send_not_run_never_reaches_sender(self):
        decision = {"can_fire": False, "checks": [], "notrun_reason": "premise-unverifiable-range"}
        spy = SpySender()
        sent, reply = nt.maybe_send(decision, spy, "attack", {"id": "0:0", "target": "1:10066"})
        self.assertFalse(sent)
        self.assertIsNone(reply)
        self.assertEqual(spy.calls, [])

    # ---- B regression: DISARMED gate ต้องเป็น gate จริงผ่าน maybe_send --------
    def test_disarmed_arm_window_active_not_run_never_sent(self):
        """arm window ยังไม่หมดอายุ -> NOT-RUN และ sender ไม่ถูกเรียก"""
        st = state(units=live_units())
        case = {"n": 2, "group": "DISARMED", "cmd": "attack",
                "args": {"id": "0:0", "target": "1:10066"}, "arm": 120,
                "arm_wait_ms": 0, "expect": "DISARMED"}
        d = self.d("DISARMED", "attack", case, st)
        self.assertFalse(d["can_fire"])
        self.assertEqual(d["notrun_reason"], "arm-window-active")
        spy = SpySender()
        sent, _ = nt.maybe_send(d, spy, case["cmd"], case["args"])
        self.assertFalse(sent)
        self.assertEqual(spy.calls, [])

    def test_disarmed_session_arm_active_not_run_never_sent(self):
        """arm จากเคสก่อนหน้าใน session ยังไม่หมด -> NOT-RUN + ไม่ยิง"""
        st = state(units=live_units())
        case = {"n": 1, "group": "DISARMED", "cmd": "move",
                "args": {"id": "0:0", "row": 1, "col": 0}, "arm": 0,
                "expect": "DISARMED"}
        d = self.d("DISARMED", "move", case, st,
                   {"arm_active_until_ms": nt.now_ms() + 60_000})
        self.assertFalse(d["can_fire"])
        self.assertEqual(d["notrun_reason"], "arm-window-active")
        spy = SpySender()
        sent, _ = nt.maybe_send(d, spy, case["cmd"], case["args"])
        self.assertFalse(sent)
        self.assertEqual(spy.calls, [])

    def test_disarmed_unverifiable_not_run_never_sent(self):
        """arm state ยืนยันไม่ได้ (session fact ขาด) -> fail-closed NOT-RUN + ไม่ยิง"""
        st = state(units=live_units())
        case = {"n": 1, "group": "DISARMED", "cmd": "move",
                "args": {"id": "0:0", "row": 1, "col": 0}, "arm": 0,
                "expect": "DISARMED"}
        d = self.d("DISARMED", "move", case, st, {})   # session fact ขาด
        self.assertFalse(d["can_fire"])
        self.assertEqual(d["notrun_reason"], "arm-state-unverifiable")
        spy = SpySender()
        sent, _ = nt.maybe_send(d, spy, case["cmd"], case["args"])
        self.assertFalse(sent)
        self.assertEqual(spy.calls, [])


STABLE_HASH = "H" * 64
FIRE_SUBS = ("move", "attack")  # the only subs that can change game state


def status_fixture():
    st = state(units=live_units())
    st["stateHash"] = STABLE_HASH
    return st


class FakeIpc:
    """Stands in for nt.coopctl.call - records subs, raises on configured ones."""

    def __init__(self, fire_error="DISARMED: run spike arm first (one command per arm)",
                 raise_on=None):
        self.subs = []
        self.fire_error = fire_error
        self.raise_on = set(raise_on or ())
        self.status = status_fixture()

    def __call__(self, pid, cmd, extra=None):
        sub = (extra or {}).get("sub", cmd)
        self.subs.append(sub)
        if sub in self.raise_on:
            raise FileNotFoundError(2, "pipe gone", "\\\\.\\pipe\\longyin-coop-4242")
        if sub == "status":
            return {"ok": True, "result": dict(self.status)}
        if sub == "arm":
            return {"ok": True, "result": {"armed": True}}
        if sub in FIRE_SUBS:
            return {"ok": False, "error": self.fire_error}
        return {"ok": True, "result": {}}

    def fire_count(self):
        return sum(1 for s in self.subs if s in FIRE_SUBS)


class CleanupFailIpc(FakeIpc):
    """disarm ครั้งที่ 2 (cleanup) ล้ม — startup disarm ยังสำเร็จ"""

    def __init__(self):
        super().__init__()
        self.disarm_calls = 0

    def __call__(self, pid, cmd, extra=None):
        sub = (extra or {}).get("sub", cmd)
        if sub == "disarm":
            self.disarm_calls += 1
            if self.disarm_calls >= 2:
                raise FileNotFoundError("pipe gone at cleanup")
        return super().__call__(pid, cmd, extra)


class MainFlowOfflineTests(unittest.TestCase):
    """B: offline failure-path tests for main() with mocked IPC/writes.
    No game, no real pipe - every IPC touch goes through FakeIpc."""

    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.tmp = self._tmp.name
        self._orig_call = nt.coopctl.call

    def tearDown(self):
        nt.coopctl.call = self._orig_call
        self._tmp.cleanup()

    def run_main(self, plan, fake, wait_side=None, wait_ret=None, write_side=None,
                 out_override=None):
        plan_path = os.path.join(self.tmp, "plan-%s.json" % plan["window"])
        with open(plan_path, "w", encoding="utf-8") as f:
            json.dump(plan, f)
        out_path = out_override or os.path.join(self.tmp, "out-%s.json" % plan["window"])
        argv = ["negative-tests.py", "--plan", plan_path, "--out", out_path, "--pid", "4242"]
        nt.coopctl.call = fake
        patches = []
        if wait_side is not None:
            patches.append(mock.patch.object(nt, "wait_stable_state", side_effect=wait_side))
        elif wait_ret is not None:
            patches.append(mock.patch.object(nt, "wait_stable_state", return_value=wait_ret))
        if write_side is not None:
            patches.append(mock.patch.object(nt, "write_record", side_effect=write_side))
        old_argv = sys.argv
        sys.argv = argv
        try:
            for p in patches:
                p.start()
            try:
                code = nt.main()
            finally:
                for p in patches:
                    p.stop()
        finally:
            sys.argv = old_argv
        nd_path = out_path[:-5] + ".ndjson" if out_path.endswith(".json") else out_path + ".ndjson"
        summary = None
        if os.path.isfile(out_path):
            with open(out_path, encoding="utf-8") as f:
                summary = json.load(f)
        nd_lines = []
        if os.path.isfile(nd_path):
            with open(nd_path, encoding="utf-8") as f:
                nd_lines = [json.loads(x) for x in f if x.strip()]
        return code, summary, nd_lines, fake

    @staticmethod
    def plan(window, cases):
        return {"label": "UT", "window": window, "pref": True,
                "settle_ms": 1, "cases": cases}

    def disarmed_move(self, n=1):
        return {"n": n, "group": "DISARMED", "cmd": "move",
                "args": {"id": "0:0", "row": 1, "col": 0}, "arm": 0,
                "expect": "DISARMED"}

    # --- B: NDJSON write OSError -> no command beyond the failure point ------
    def test_ndjson_write_fails_on_first_record_zero_commands(self):
        fake = FakeIpc()
        code, summary, nd_lines, _ = self.run_main(
            self.plan("w-a", [self.disarmed_move(1)]), fake,
            wait_ret=(status_fixture(), None), write_side=OSError("disk full"))
        self.assertEqual(code, 3)
        self.assertEqual(fake.fire_count(), 0)
        self.assertEqual(nd_lines, [])
        self.assertEqual(summary["case_status"]["1"], "NO-RECORD")
        self.assertIn("1", summary["incomplete_case_ids"])
        self.assertTrue(summary["records_gap"])
        self.assertEqual(summary["pass"], 0)

    def test_ndjson_write_fails_after_pre_fire_incomplete_not_pass(self):
        """terminal record เขียนไม่ได้ -> เคสต้องกลายเป็น UNKNOWN/INCOMPLETE ไม่ใช่ PASS"""
        fake = FakeIpc()
        ok_writes = []

        def fake_write(nd, rec):
            if len(ok_writes) >= 1:
                raise OSError("disk full")   # 1st (pre-fire) ok, 2nd (post) fails
            ok_writes.append(rec)

        code, summary, nd_lines, _ = self.run_main(
            self.plan("w-b", [self.disarmed_move(1)]), fake,
            wait_ret=(status_fixture(), None), write_side=fake_write)
        self.assertEqual(code, 3)
        self.assertEqual(fake.fire_count(), 1)          # fired once, then stopped
        self.assertEqual(summary["pass"], 0)            # never counted as PASS
        self.assertEqual(summary["case_status"]["1"], "INCOMPLETE")
        self.assertIn("1", summary["incomplete_case_ids"])
        pre = [r for r in ok_writes if r.get("phase") == "pre-fire"]
        post = [r for r in ok_writes if r.get("phase") == "post"]
        self.assertEqual(len(pre), 1)                   # pre-fire record survived
        self.assertEqual(len(post), 0)                  # terminal record lost

    # --- B: hash never stabilizes -> nothing fired ---------------------------
    def test_hash_unstable_at_start_exit2_zero_commands(self):
        fake = FakeIpc()
        code, summary, nd_lines, _ = self.run_main(
            self.plan("w-c", [self.disarmed_move(1)]), fake,
            wait_side=[(None, "hash-unstable")])
        self.assertEqual(code, 2)
        self.assertEqual(fake.fire_count(), 0)
        self.assertEqual(nd_lines, [])
        self.assertNotIn("move", fake.subs)
        self.assertNotIn("attack", fake.subs)

    # --- B: state/IPC fails mid-run -> UNKNOWN + exit 3 ----------------------
    def test_state_unreadable_mid_run_unknown_and_exit3(self):
        fake = FakeIpc()
        st = status_fixture()
        wait_side = [(st, None),          # base
                     (st, None),          # case 1 pre-checks
                     (None, "state-unreadable: FileNotFoundError: pipe")]  # case 2
        plan = self.plan("w-d", [self.disarmed_move(1), self.disarmed_move(2)])
        code, summary, nd_lines, _ = self.run_main(plan, fake, wait_side=wait_side)
        self.assertEqual(code, 3)
        self.assertEqual(fake.fire_count(), 1)          # case 1 only
        self.assertEqual(summary["pass"], 1)
        self.assertEqual(summary["case_status"]["1"], "PASS")
        self.assertEqual(summary["case_status"]["2"], "UNKNOWN")   # recorded, not PASS
        unknown = [r for r in nd_lines if r.get("result") == "UNKNOWN"]
        self.assertEqual(len(unknown), 1)
        self.assertIn("state-unreadable", unknown[0]["aborted_reason"])

    # --- B: pre-fire record without terminal outcome -> never PASS -----------
    def test_pre_fire_without_terminal_outcome_incomplete(self):
        fake = FakeIpc(raise_on={"arm"})   # connection lost during arm
        case = {"n": 3, "group": "NOT_ACTIVE", "cmd": "move",
                "args": {"id": "0:9", "row": 4, "col": 0}, "arm": 120,
                "expect": "is not the active unit"}
        code, summary, nd_lines, _ = self.run_main(
            self.plan("w-e", [case]), fake, wait_ret=(status_fixture(), None))
        self.assertEqual(code, 3)
        self.assertEqual(fake.fire_count(), 0)          # died before firing
        self.assertEqual(summary["pass"], 0)
        self.assertEqual(summary["case_status"]["3"], "INCOMPLETE")
        self.assertIn("3", summary["incomplete_case_ids"])
        pre = [r for r in nd_lines if r.get("phase") == "pre-fire"]
        post = [r for r in nd_lines if r.get("phase") == "post"]
        self.assertEqual(len(pre), 1)
        self.assertEqual(len(post), 0)

    # --- B: audit file cannot be opened -> exit 2, zero commands -------------
    def test_ndjson_open_failure_exit2_zero_commands(self):
        fake = FakeIpc()
        out_path = os.path.join(self.tmp, "out.json")
        nd_dir = out_path[:-5] + ".ndjson"          # the NDJSON path is a DIRECTORY
        os.makedirs(nd_dir, exist_ok=True)          # -> open("a") raises
        code, summary, nd_lines, _ = self.run_main(
            self.plan("w-f", [self.disarmed_move(1)]), fake, out_override=out_path)
        self.assertEqual(code, 2)
        self.assertEqual(fake.fire_count(), 0)

    # --- C: cleanup after audit write failure — order + counts ---------------
    def test_cleanup_after_audit_failure_disarm_then_set_pref(self):
        """abort จาก audit write failure แล้ว cleanup พยายามส่ง disarm ตามด้วย
        set_pref(false) — ตรวจลำดับและจำนวนจาก IPC log เต็ม ไม่ใช่แค่ fire_count"""
        fake = FakeIpc()
        code, summary, nd_lines, _ = self.run_main(
            self.plan("w-g", [self.disarmed_move(1)]), fake,
            wait_ret=(status_fixture(), None), write_side=OSError("disk full"))
        self.assertEqual(code, 3)
        self.assertEqual(fake.fire_count(), 0)
        self.assertEqual(
            fake.subs,
            ["set_pref", "disarm",   # startup: allowRemoteInput=true + สถาปนา no-arm
             "disarm", "set_pref"])  # cleanup หลัง abort: ตามลำดับนี้เสมอ
        self.assertEqual(summary["cleanup_ok"], True)
        self.assertIn("audit-trail-write-failed", summary["aborted"])

    def test_cleanup_failure_reported_without_masking_abort(self):
        """cleanup เองล้ม -> cleanup_ok=false แต่สาเหตุ abort เดิมต้องไม่ถูกกลบ"""
        fake = CleanupFailIpc()
        code, summary, nd_lines, _ = self.run_main(
            self.plan("w-h", [self.disarmed_move(1)]), fake,
            wait_ret=(status_fixture(), None), write_side=OSError("disk full"))
        self.assertEqual(code, 3)
        self.assertEqual(fake.fire_count(), 0)
        self.assertEqual(summary["cleanup_ok"], False)
        self.assertIn("audit-trail-write-failed", summary["aborted"])
        # cleanup ล้มที่ disarm (ครั้งที่ 2) — set_pref ไม่ได้ถูกส่งต่อ
        self.assertEqual(fake.subs[-1], "disarm")
        self.assertEqual(fake.disarm_calls, 2)

    # --- A1: import-level and pure-function paths never touch IPC ------------
    def test_preconditions_and_maybe_send_never_touch_ipc(self):
        def forbidden(*a, **k):
            raise AssertionError("IPC was touched from a pure path")
        nt.coopctl.call = forbidden
        try:
            case = {"n": 7, "group": "OUT_OF_RANGE", "cmd": "attack",
                    "args": {"id": "0:0", "target": "1:10066"}, "arm": 120}
            d = nt.check_preconditions("OUT_OF_RANGE", "attack", case,
                                       state(units=live_units(), phase="WaitAttacking"),
                                       {"arm_active_until_ms": 0}, range_verdict=None)
            self.assertFalse(d["can_fire"])
            sent, _ = nt.maybe_send(d, lambda c, a: (_ for _ in ()).throw(AssertionError("sent")), "attack", case["args"])
            self.assertFalse(sent)
        finally:
            nt.coopctl.call = self._orig_call


if __name__ == "__main__":
    unittest.main(verbosity=2)
