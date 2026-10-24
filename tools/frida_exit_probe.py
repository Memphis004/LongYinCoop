"""Read-only probe for leaving an Explorer map (FinishExploreMap / QuitExploreButtonClicked).

Hooks only entry points (Interceptor.attach, no replacement, no invoke of game logic
except pure getters) and records a UI/state snapshot on enter and leave:
leftPower, ExploreController.inited, active UI objects (explore / area / bigmap),
AreaController.areaData identity, PlotController flags, current scene name.

Usage: python tools/frida_exit_probe.py <out.ndjson> [minutes]
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
globalThis.__exit = (() => {
  const A = Il2Cpp.domain.assembly("Assembly-CSharp");
  const log = [];
  const st = () => {
    const o = {};
    try {
      const c = Il2Cpp.gc.choose(A.image.class("ExploreController"))[0];
      o.expInited = c.field("inited").value;
      o.lp = c.field("leftPower").value;
      try { o.expObjActive = c.field("exploreObj").value.field("active").value; } catch(e) {}
      try { o.expUIPanelActive = c.field("exploreUIPanel").value.field("active").value; } catch(e) {}
    } catch(e) { o.exp = null; }
    try {
      const a = Il2Cpp.gc.choose(A.image.class("AreaController"))[0];
      o.areaInited = a.field("inited").value;
      try { o.areaUIBelowActive = a.field("areaUIBelow").value.field("active").value; } catch(e) {}
      try { const d = a.field("areaData").value; o.areaID = d.field("areaID").value; o.areaName = d.field("areaName").value.content; } catch(e) {}
    } catch(e) { o.area = null; }
    try {
      const b = Il2Cpp.gc.choose(A.image.class("BigMapController"))[0];
      try { o.bigmapRootActive = b.field("bigmapRoot").value.field("active").value; } catch(e) {}
      try { o.bigmapUIPanelActive = b.field("bigmapUIPanel").value.field("active").value; } catch(e) {}
      try { o.playArmyActive = b.field("playerArmy").value.field("active").value; } catch(e) {}
      o.resting = b.field("playerResting").value;
    } catch(e) { o.bigmap = null; }
    try {
      const pl = Il2Cpp.gc.choose(A.image.class("PlotController"))[0];
      o.pt = pl.field("plotTextShowing").value; o.pc = pl.field("plotChoiceShowing").value;
    } catch(e) {}
    try { o.scene = Il2Cpp.domain.assembly("UnityEngine.CoreModule").image.class("UnityEngine.SceneManagement.SceneManager").method("GetActiveScene").invoke().field("name").value.content; } catch(e) {}
    return o;
  };
  function add(cn, mn, pc) {
    let ms = [];
    try { ms = A.image.class(cn).methods.filter(m => m.name === mn && (pc === undefined || (m.parameterCount || 0) === pc)); } catch(e) { return 0; }
    let n = 0;
    for (const m of ms) {
      try {
        Interceptor.attach(m.virtualAddress, {
          onEnter: function (a) {
            try {
              const e = { t: Date.now(), cn: cn, m: mn, ph: "in", arg0: m.parameterCount > 0 ? a[1].toInt32() : null, st: st() };
              if (log.length < 3000) log.push(e);
            } catch(err) {}
          },
          onLeave: function () {
            try { if (log.length < 3000) log.push({ t: Date.now(), cn: cn, m: mn, ph: "out", st: st() }); } catch(err) {}
          }
        });
        n++;
      } catch(e) {}
    }
    return n;
  }
  const T = [
    ["ExploreController", "QuitExploreButtonClicked", 0],
    ["ExploreController", "FinishExploreMap", 1],
    ["ExploreController", "ExploreFail", 0],
    ["ExploreController", "ResetExploreMap", 0],
    ["ExploreController", "HideExploreMap", 0],
    ["ExploreController", "PlayerFinishMove", 0],
    ["PlotController", "FinishExploreMap", 1],
    ["PlotController", "FinishStealSkill", 0],
    ["PlotController", "FinishStealResource", 0],
    ["PlotController", "FinishExploreWildMap", 0],
    ["PlotController", "FinishExploreMazeMap", 0],
    ["PlotController", "FinishExploreArea", 0],
    ["PlotController", "ExploreAreaFinish", 0],
    ["PlotController", "FinishDigTreasureMap", 0],
    ["PlotController", "StartExplore", 1],
    ["PlotController", "StartExploreFuc", 3],
    ["AreaController", "PlayerLeaveArea", 0],
    ["AreaController", "ReturnBigMapButtonClicked", 0],
    ["GameController", "PlayerLeaveArea", 0]
  ];
  const hooked = [];
  for (const t of T) { const n = add(t[0], t[1], t[2]); if (n) hooked.push(t[0] + "." + t[1] + "/" + t[2] + " x" + n); }
  return { drain: () => log.splice(0, log.length), hooked: hooked, install: st() };
})();
JSON.stringify({ hooked: __exit.hooked, install: __exit.install })
"""

DRAIN = "JSON.stringify(__exit.drain())"


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: frida_exit_probe.py <out.ndjson> [minutes]", file=sys.stderr)
        return 2
    out = Path(sys.argv[1])
    minutes = float(sys.argv[2]) if len(sys.argv) > 2 else 30.0
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
