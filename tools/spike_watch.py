#!/usr/bin/env python3
"""S1.5-b: wait for the game + a live battle, logging `spike status` transitions.

Read-only: sends only {"cmd":"spike","sub":"status"}. Exits 0 as soon as the
battle is Fighting with an active unit, so the caller can start the 20-frame
capture. Exits 2 on timeout, 3 if the game exits.
"""
import argparse
import datetime
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import coopctl  # noqa: E402

KEYS = ("frame", "inBattle", "battleState", "battleStateName", "nowActiveState",
        "nowActiveStateName", "active", "activeHeroId", "activeName",
        "activePlayerControl", "isPlayerTurn", "moved", "attacked", "paused",
        "infoCount", "stateHash")


def log(fh, event, payload):
    rec = {"ts": datetime.datetime.now().isoformat(timespec="seconds"), "event": event, **payload}
    fh.write(json.dumps(rec, ensure_ascii=False) + "\n")
    fh.flush()
    print(json.dumps(rec, ensure_ascii=False), flush=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--timeout", type=float, default=1800.0)
    ap.add_argument("--interval", type=float, default=1.0)
    args = ap.parse_args()

    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    # console may be cp1252 on this machine; never die on a Chinese hero name
    try:
        sys.stdout.reconfigure(errors="replace")
    except Exception:
        pass
    deadline = time.time() + args.timeout
    with open(args.out, "w", encoding="utf-8") as fh:
        # phase 1: wait for the process
        pid = None
        while time.time() < deadline:
            pid = coopctl.find_pid()
            if pid:
                log(fh, "process", {"pid": pid})
                break
            time.sleep(2)
        if not pid:
            log(fh, "timeout", {"phase": "process"})
            return 2

        # phase 2: wait for the pipe, then track battle state
        last = None
        connected = False
        while time.time() < deadline:
            pid_now = coopctl.find_pid()
            if not pid_now:
                log(fh, "game_exit", {})
                return 3
            try:
                res = coopctl.call(pid_now, "spike", extra={"sub": "status"})
            except Exception as exc:
                if connected:
                    log(fh, "pipe_error", {"error": str(exc)})
                time.sleep(1.0)
                continue

            if not connected:
                connected = True
                ver = coopctl.call(pid_now, "get_version")
                v = ver.get("result") or {}
                log(fh, "connected", {k: v.get(k) for k in
                                      ("modVersion", "stageLabel", "enabled", "ipc",
                                       "devCommands", "allowRemoteInput", "logPath")})

            if not res.get("ok"):
                log(fh, "status_error", {"error": res.get("error")})
                time.sleep(args.interval)
                continue

            s = res.get("result") or {}
            snap = {k: s.get(k) for k in KEYS}
            key = (snap["inBattle"], snap["battleState"], snap["active"])
            if key != last:
                log(fh, "status", snap)
                last = key

            if snap["inBattle"] and snap["battleState"] == 3 and snap["active"]:
                log(fh, "battle_live", snap)
                # S1.5-b: ต้องเป็นจังหวะที่เกมหยุดรอ input ของผู้เล่นจริง
                # (auto-fight จะไม่เคยเข้าสถานะนี้ — ดู analysis-capture1.md)
                if snap.get("nowActiveState") == 2:  # ActiveUnitBattleState.WaitPlayerInput
                    log(fh, "battle_ready", snap)
                    return 0
            time.sleep(args.interval)

        log(fh, "timeout", {"phase": "battle"})
        return 2


if __name__ == "__main__":
    sys.exit(main())
