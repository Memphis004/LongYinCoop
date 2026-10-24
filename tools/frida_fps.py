"""Read-only FPS sampler via the bundled il2cpp frida agent.

Polls Time.frameCount/realtimeSinceStartup from Python (never Thread.sleep inside
the agent, which wedges the RPC loop). Prints a compact distribution.

Usage: python tools/frida_fps.py [seconds]
"""
import json
import statistics
import sys
import time
from pathlib import Path

import frida

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

GAME = "LongYinLiZhiZhuan.exe"
AGENT = Path(
    r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan\il2cpp-frida-mcp\mcp_server\_agent.js"
)
CODE = """
const U=Il2Cpp.domain.assemblies.find(x=>x.name==="UnityEngine.CoreModule");
const T=U.image.classes.find(c=>c.namespace==="UnityEngine"&&c.name==="Time");
JSON.stringify({f:T.methods.filter(m=>m.name==="get_frameCount")[0].invoke(),
 t:T.methods.filter(m=>m.name==="get_realtimeSinceStartup")[0].invoke()})
"""


def main() -> int:
    seconds = float(sys.argv[1]) if len(sys.argv) > 1 else 5.0
    device = frida.get_local_device()
    session = device.attach(device.get_process(GAME).pid)
    script = session.create_script(AGENT.read_text(encoding="utf-8"))
    script.load()

    def sample():
        r = script.exports_sync.exec_js(CODE)
        return json.loads(r["result"])

    prev = sample()
    rows = []
    end = time.time() + seconds
    while time.time() < end:
        time.sleep(0.1)
        cur = sample()
        el = cur["t"] - prev["t"]
        if el > 0:
            rows.append((cur["f"] - prev["f"]) / el)
        prev = cur
    rows.sort()
    q = lambda p: round(rows[min(len(rows) - 1, int(len(rows) * p))], 1)  # noqa: E731
    print(json.dumps({
        "seconds": seconds,
        "n": len(rows),
        "fps_min": q(0.0),
        "fps_p05": q(0.05),
        "fps_med": q(0.5),
        "fps_p95": q(0.95),
        "fps_max": q(0.999),
        "windows_under_60": sum(1 for x in rows if x < 60),
        "windows_under_30": sum(1 for x in rows if x < 30),
        "mean": round(statistics.fmean(rows), 1),
    }))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
