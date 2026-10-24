"""Read-only fallback runner for the il2cpp-frida-mcp agent.

The MCP stdio server died mid-session (a Thread.sleep inside exec_js wedged it).
This script loads the *same* bundled agent (`mcp_server/_agent.js`) directly so
read-only field/state inspection can continue. It exposes exec_js only; it does
not add any write path.

Usage: python tools/frida_eval.py <js-file>
Prints the JSON/native result returned by exec_js.
"""
import json
import sys
from pathlib import Path

import frida

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

GAME = "LongYinLiZhiZhuan.exe"
AGENT = Path(r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan\il2cpp-frida-mcp\mcp_server\_agent.js")
if not AGENT.exists():
    AGENT = Path(r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan\il2cpp-frida-mcp\_agent.js")


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: frida_eval.py <js-file>", file=sys.stderr)
        return 2
    code = Path(sys.argv[1]).read_text(encoding="utf-8")

    device = frida.get_local_device()
    pid = device.get_process(GAME).pid
    session = device.attach(pid)
    script = session.create_script(AGENT.read_text(encoding="utf-8"))
    script.load()

    result = script.exports_sync.exec_js(code)
    print(json.dumps(result, ensure_ascii=False, default=str))
    return 0 if result.get("success") else 1


if __name__ == "__main__":
    raise SystemExit(main())
