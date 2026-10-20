#!/usr/bin/env python3
"""Read-only client for LongYinCoop's named pipe IPC (S0).

Usage:
    python tools/coopctl.py ping
    python tools/coopctl.py get_version
    python tools/coopctl.py get_scene
    python tools/coopctl.py get_player_pos
    python tools/coopctl.py stats
    python tools/coopctl.py snapshot --what world
    python tools/coopctl.py snapshot --what battle
    python tools/coopctl.py snapshot --what log --n 30
    python tools/coopctl.py capture --action start|stop|status
    python tools/coopctl.py probe_types
    python tools/coopctl.py probe_units
    python tools/coopctl.py patch_report
    python tools/coopctl.py compat_dump --tag bigmap
    python tools/coopctl.py s1_report
    python tools/coopctl.py set_pref --key capture --value false
    python tools/coopctl.py --pid 1234 get_scene
"""
import argparse
import json
import math
import struct
import subprocess
import sys
import time

GAME_EXE = "LongYinLiZhiZhuan.exe"


def find_pid():
    try:
        out = subprocess.check_output(
            ["tasklist", "/FI", "IMAGENAME eq " + GAME_EXE, "/FO", "CSV", "/NH"],
            stderr=subprocess.DEVNULL,
        ).decode("utf-8", "ignore")
    except Exception:
        return None
    for line in out.splitlines():
        parts = [p.strip('"') for p in line.split('","')]
        if len(parts) >= 2 and parts[0].lower() == GAME_EXE.lower():
            try:
                return int(parts[1])
            except ValueError:
                pass
    return None


def connect(pid, retries=40, delay=0.05):
    """เปิด pipe; server สร้าง instance ใหม่หลัง client หลุด จึงต้อง retry (BUSY/NOT_FOUND)"""
    path = r"\\.\pipe\longyin-coop-%d" % pid
    last = None
    for _ in range(retries):
        try:
            return open(path, "r+b", buffering=0)
        except OSError as e:
            last = e
            time.sleep(delay)
    raise last


def request(f, cmd, extra=None):
    msg = {"cmd": cmd}
    if extra:
        msg.update(extra)
    payload = json.dumps(msg).encode("utf-8")
    f.write(struct.pack("<I", len(payload)) + payload)
    header = f.read(4)
    if len(header) != 4:
        raise RuntimeError("short header from pipe")
    (size,) = struct.unpack("<I", header)
    body = f.read(size)
    if len(body) != size:
        raise RuntimeError("short body from pipe")
    return json.loads(body.decode("utf-8"))


def call(pid, cmd, timeout=5.0, retries=40, extra=None):
    f = connect(pid, retries)
    try:
        return request(f, cmd, extra)
    finally:
        f.close()


def _summary(samples, ok, bad):
    s = sorted(samples)
    n = len(s)

    def pct(p):
        if n == 0:
            return 0.0
        i = min(n - 1, max(0, int(math.ceil(p * n)) - 1))
        return s[i]

    return {
        "n": n,
        "ok": ok,
        "bad": bad,
        "p50": round(pct(0.50), 3),
        "p95": round(pct(0.95), 3),
        "p99": round(pct(0.99), 3),
        "max": round(s[-1], 3) if n else 0.0,
    }


def bench(pid, cmd, n=1000, extra=None, warmup=20):
    """วัดสองแบบ: connection เดิมต่อเนื่อง และ connect ใหม่ทุกครั้ง"""
    reuse, ok1, bad1 = [], 0, 0
    f = connect(pid)
    try:
        for _ in range(warmup):
            request(f, cmd, extra)
        for _ in range(n):
            t = time.perf_counter()
            r = request(f, cmd, extra)
            reuse.append((time.perf_counter() - t) * 1000.0)
            if r.get("ok"):
                ok1 += 1
            else:
                bad1 += 1
    finally:
        f.close()

    fresh, ok2, bad2 = [], 0, 0
    for _ in range(n):
        t = time.perf_counter()
        r = call(pid, cmd, extra=extra)
        fresh.append((time.perf_counter() - t) * 1000.0)
        if r.get("ok"):
            ok2 += 1
        else:
            bad2 += 1

    return {"cmd": cmd, "n": n, "reuse": _summary(reuse, ok1, bad1), "per_call": _summary(fresh, ok2, bad2)}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("command", choices=["ping", "get_version", "get_scene", "get_player_pos", "stats",
                                        "bench_dispatch", "bench", "snapshot", "capture", "compat_dump",
                                        "probe_types", "patch_report", "probe_units", "s1_report", "set_pref"])
    ap.add_argument("--pid", type=int, default=None, help="game pid (auto-detected if omitted)")
    ap.add_argument("--n", type=int, default=1000, help="bench_dispatch / bench / snapshot log: number of requests")
    ap.add_argument("--cmd", default="ping", choices=["ping", "stats", "get_scene", "get_player_pos", "get_version"],
                    help="bench: command to measure")
    ap.add_argument("--what", default="world", choices=["world", "battle", "log"], help="snapshot: stream")
    ap.add_argument("--action", default=None, choices=["start", "stop", "status"], help="capture: action")
    ap.add_argument("--tag", default=None, help="compat_dump: file suffix")
    ap.add_argument("--key", default="capture", help="set_pref: pref key")
    ap.add_argument("--value", default="false", help="set_pref: value")
    ap.add_argument("--quiet", action="store_true")
    args = ap.parse_args()

    pid = args.pid or find_pid()
    if not pid:
        print("game not running (or pass --pid)", file=sys.stderr)
        return 2

    if args.command == "bench":
        res = bench(pid, args.cmd, args.n)
        if args.quiet:
            print(json.dumps(res))
        else:
            print("bench cmd=%s n=%d" % (res["cmd"], res["n"]))
            for mode in ("reuse", "per_call"):
                s = res[mode]
                print("  %-9s n=%d ok=%d bad=%d p50=%.3f p95=%.3f p99=%.3f max=%.3f ms" %
                      (mode, s["n"], s["ok"], s["bad"], s["p50"], s["p95"], s["p99"], s["max"]))
        return 0 if res["reuse"]["bad"] == 0 and res["per_call"]["bad"] == 0 else 1

    try:
        if args.command == "bench_dispatch":
            extra = {"n": args.n}
        elif args.command == "snapshot":
            extra = {"what": args.what, "n": min(args.n, 500) if args.what == "log" else args.n}
        elif args.command == "capture":
            extra = {"action": args.action or "status"}
        elif args.command == "compat_dump":
            extra = {"tag": args.tag} if args.tag else None
        elif args.command == "set_pref":
            extra = {"key": args.key, "value": args.value}
        else:
            extra = None
        result = call(pid, args.command, extra=extra)
    except FileNotFoundError:
        print("pipe not found for pid %d - is the mod loaded and Enabled=true?" % pid, file=sys.stderr)
        return 3
    except Exception as e:
        print("ipc error: %s" % e, file=sys.stderr)
        return 4

    if args.quiet:
        print(json.dumps(result))
    else:
        print(json.dumps(result, indent=2, ensure_ascii=False))
    return 0 if result.get("ok") else 1


if __name__ == "__main__":
    sys.exit(main())
