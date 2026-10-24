"""Read-only cost probe: compare ExploreController.Get*Cost() return values with the
leftPower actually deducted when an explorer event is confirmed.

Hooks (attach only, no replacement):
  ExploreController.GetDigTreasureCost / GetOpenLockCost / GetCollectResourceCost
      -> log return value + exploreDifficulty
  ExploreController.ManageTileEvent -> tile id + leftPower before/after
  ExploreController.ChangeMoveStep -> leftPower (separates walking from event cost)
  PlotController.ChooseDigTreasure / DigTreasureChoosen / CollectExploreResource /
      CollectResourceChoosen / OpenLockChest / ChooseCollectResource -> leftPower before/after
  ExploreController.PlayerFinishMove -> leftPower

Usage: python tools/frida_cost_probe.py <out.ndjson> [minutes]
Stop early: create <out>.stop
"""
import json
import sys
import time
from pathlib import Path

import frida

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

GAME = "LongYinLiZhiZhuan.exe"
AGENT = Path(
    r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan\il2cpp-frida-mcp\mcp_server\_agent.js"
)

INSTALL = r"""
globalThis.__cost = (() => {
  const A = Il2Cpp.domain.assembly("Assembly-CSharp");
  const log = [];
  function lp() {
    try { return Il2Cpp.gc.choose(A.image.class("ExploreController"))[0].field("leftPower").value; }
    catch(e) { return null; }
  }
  function diff() {
    try {
      const c = Il2Cpp.gc.choose(A.image.class("ExploreController"))[0];
      return c.field("exploreMapData").value.field("exploreDifficulty").value;
    } catch(e) { return null; }
  }
  function push(e) { if (log.length < 4000) log.push(e); }
  function hookRet(cn, mn) {
    let ms = [];
    try { ms = A.image.class(cn).methods.filter(m => m.name === mn); } catch(e) { return 0; }
    let n = 0;
    for (const m of ms) {
      try {
        Interceptor.attach(m.virtualAddress, {
          onEnter: function () { this.__lp = lp(); this.__d = diff(); },
          onLeave: function (r) {
            try { push({ t: Date.now(), kind: "cost", m: mn, ret: r.toInt32(), lp: this.__lp, diff: this.__d, lpAfter: lp() }); } catch(e) {}
          }
        });
        n++;
      } catch(e) {}
    }
    return n;
  }
  function hookBoundary(cn, mn, extra) {
    let ms = [];
    try { ms = A.image.class(cn).methods.filter(m => m.name === mn); } catch(e) { return 0; }
    let n = 0;
    for (const m of ms) {
      try {
        Interceptor.attach(m.virtualAddress, {
          onEnter: function (a) {
            try {
              const e = { t: Date.now(), kind: "ev", m: mn, ph: "in", lp: lp() };
              if (extra) extra(e, a);
              push(e);
            } catch(err) {}
          },
          onLeave: function () {
            try { push({ t: Date.now(), kind: "ev", m: mn, ph: "out", lp: lp() }); } catch(err) {}
          }
        });
        n++;
      } catch(e) {}
    }
    return n;
  }
  const hooked = [];
  hooked.push("cost:dig=" + hookRet("ExploreController", "GetDigTreasureCost"));
  hooked.push("cost:lock=" + hookRet("ExploreController", "GetOpenLockCost"));
  hooked.push("cost:collect=" + hookRet("ExploreController", "GetCollectResourceCost"));
  const tileExtra = function (e, a) {
    try {
      const t = new Il2Cpp.Object(a[1]);
      e.tile = [t.field("row").value, t.field("column").value, t.field("exploreTileEventType").value];
    } catch(err) {}
  };
  hooked.push("ev:tile=" + hookBoundary("ExploreController", "ManageTileEvent", tileExtra));
  hooked.push("ev:move=" + hookBoundary("ExploreController", "ChangeMoveStep", null));
  hooked.push("ev:finishMove=" + hookBoundary("ExploreController", "PlayerFinishMove", null));
  for (const mn of ["ChooseDigTreasure", "DigTreasureChoosen", "CollectExploreResource",
                    "CollectResourceChoosen", "OpenLockChest", "ChooseCollectResource"]) {
    const n = hookBoundary("PlotController", mn, null) + hookBoundary("ExploreController", mn, null);
    hooked.push("ev:" + mn + "=" + n);
  }
  return { drain: () => log.splice(0, log.length), hooked: hooked, install: { lp: lp(), diff: diff() } };
})();
JSON.stringify({ hooked: __cost.hooked, install: __cost.install })
"""

DRAIN = "JSON.stringify(__cost.drain())"


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: frida_cost_probe.py <out.ndjson> [minutes]", file=sys.stderr)
        return 2
    out = Path(sys.argv[1])
    minutes = float(sys.argv[2]) if len(sys.argv) > 2 else 45.0
    stop = Path(str(out) + ".stop")
    if stop.exists():
        stop.unlink()

    device = frida.get_local_device()
    session = device.attach(device.get_process(GAME).pid)
    script = session.create_script(AGENT.read_text(encoding="utf-8"))
    script.load()
    print(script.exports_sync.exec_js(INSTALL), flush=True)

    end = time.time() + minutes * 60
    with out.open("a", encoding="utf-8") as fh:
        while time.time() < end and not stop.exists():
            try:
                r = script.exports_sync.exec_js(DRAIN)
                rows = json.loads(r["result"]) if r.get("success") else [{"err": r}]
            except Exception as e:
                fh.write(json.dumps({"err": str(e)}, ensure_ascii=False) + "\n")
                fh.flush()
                break
            for row in rows:
                fh.write(json.dumps(row, ensure_ascii=False) + "\n")
            fh.flush()
            time.sleep(0.25)
    print("probe stopped", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
