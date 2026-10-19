#!/usr/bin/env python3
"""Read-only client for LongYinCoop's named pipe IPC (S0).

Usage:
    python tools/coopctl.py ping
    python tools/coopctl.py get_version
    python tools/coopctl.py get_scene
    python tools/coopctl.py get_player_pos
    python tools/coopctl.py stats
    python tools/coopctl.py --pid 1234 get_scene
"""
import argparse
import json
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


def request(f, cmd):
    payload = json.dumps({"cmd": cmd}).encode("utf-8")
    f.write(struct.pack("<I", len(payload)) + payload)
    header = f.read(4)
    if len(header) != 4:
        raise RuntimeError("short header from pipe")
    (size,) = struct.unpack("<I", header)
    body = f.read(size)
    if len(body) != size:
        raise RuntimeError("short body from pipe")
    return json.loads(body.decode("utf-8"))


def call(pid, cmd, timeout=5.0, retries=40):
    f = connect(pid, retries)
    try:
        return request(f, cmd)
    finally:
        f.close()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("command", choices=["ping", "get_version", "get_scene", "get_player_pos", "stats"])
    ap.add_argument("--pid", type=int, default=None, help="game pid (auto-detected if omitted)")
    ap.add_argument("--quiet", action="store_true")
    args = ap.parse_args()

    pid = args.pid or find_pid()
    if not pid:
        print("game not running (or pass --pid)", file=sys.stderr)
        return 2

    try:
        result = call(pid, args.command)
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
