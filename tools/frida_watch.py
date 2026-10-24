"""Read-only Explorer call watcher (same bundled il2cpp agent, low overhead).

Attaches once, installs log-only Interceptor hooks on Explorer/Plot/Battle entry
points, then drains the in-agent log to NDJSON every 250 ms. No field writes, no
method invokes on game state, no per-frame hooks.

Usage: python tools/frida_watch.py <out.ndjson> [minutes]
Stop early by creating the file `<out>.stop`.
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
globalThis.__w = (() => {
  const A = Il2Cpp.domain.assembly("Assembly-CSharp");
  const log = [];
  function attach(cn, mn, pc, tag) {
    let ms;
    try { ms = A.image.class(cn).methods.filter(m => m.name === mn && (pc == null || (m.parameterCount || 0) === pc)); }
    catch (e) { log.push({ tag: "installFail:" + cn + "." + mn, err: String(e) }); return; }
    for (const m of ms) {
      let ty = [];
      try { for (let i = 0; i < (m.parameterCount || 0); i++) ty.push(m.parameters[i].type.name); } catch (e) {}
      try {
        Interceptor.attach(m.virtualAddress, { onEnter: function (a) {
          try {
            const e = { tag: tag, t: Date.now(), pc: m.parameterCount || 0 };
            const n = m.parameterCount || 0;
            if (n > 0 && /ExploreTileData/.test(ty[0])) {
              const o = new Il2Cpp.Object(a[1]);
              e.tile = [o.field("row").value, o.field("column").value, o.field("exploreTileEventType").value];
            } else if (n > 0 && /^System\.(Int32|Single|Boolean)/.test(ty[0])) {
              e.args = [];
              for (let i = 0; i < n; i++) e.args.push(a[i + 1].toInt32());
            } else if (n > 0 && /^System\.String/.test(ty[0])) {
              try { e.arg = new Il2Cpp.String(a[1]).content; } catch (x) { e.arg = "ERR"; }
            }
            if (log.length < 2000) log.push(e);
          } catch (err) { if (log.length < 2000) log.push({ tag: tag, err: String(err) }); }
        }});
      } catch (e) { log.push({ tag: "attachFail:" + tag, err: String(e) }); }
    }
  }
  const T = [
    ["ExploreController","ExploreTileClicked",1,"clickTile"],
    ["ExploreController","ExploreTileClicked",1,"clickGo"],
    ["ExploreController","PlayerEnterGrid",2,"enter"],
    ["ExploreController","PlayerEnterGrid",3,"enterRC"],
    ["ExploreController","PlayerFinishMove",0,"finishMove"],
    ["ExploreController","ManagePlayerAroundGrid",3,"around"],
    ["ExploreController","ManageTileEvent",1,"tileEvent"],
    ["ExploreController","ChangeMoveStep",1,"step"],
    ["ExploreController","ChangeMoveStep",2,"step2"],
    ["ExploreController","ManageMoveStepLimit",0,"stepLimit"],
    ["ExploreController","GetCollectResourceCost",0,"costCollect"],
    ["ExploreController","GetDigTreasureCost",0,"costDig"],
    ["ExploreController","GetOpenLockCost",0,"costLock"],
    ["ExploreController","SeeAllTile",0,"seeAll"],
    ["ExploreController","WatchRoundTile",1,"watchRound"],
    ["ExploreController","WatchRandomTile",1,"watchRandom"],
    ["ExploreController","FinishExploreMap",1,"finishMap"],
    ["ExploreController","QuitExploreButtonClicked",0,"quit"],
    ["ExploreController","ExploreFail",0,"fail"],
    ["PlotController","StartExplore",1,"startExplore"],
    ["PlotController","StartExploreFuc",3,"startExploreFuc"],
    ["PlotController","FinishExploreMap",1,"plotFinishMap"],
    ["PlotController","FinishExploreWildMap",0,"plotFinishWild"],
    ["PlotController","FinishExploreMazeMap",0,"plotFinishMaze"],
    ["PlotController","StartPlotEvent",1,"startPlot"],
    ["PlotController","ChooseDigTreasure",1,"chooseDig"],
    ["PlotController","DigTreasureChoosen",0,"digChoosen"],
    ["PlotController","StartDigTreasureMap",0,"startDigMap"],
    ["PlotController","FinishDigTreasureMap",0,"finishDigMap"],
    ["PlotController","ExploreRandomFightResult",1,"exploreFightResult"],
    ["PlotController","PlotTextShowFinished",0,"textShown"],
    ["PlotController","PlotChoiceShowFinished",0,"choiceShown"],
    ["GameDataController","Save",1,"save"]
  ];
  for (const t of T) attach(t[0], t[1], t[2], t[3]);
  try {
    for (const m of A.image.class("BattleController").methods.filter(m => m.name === "PrepareBattleMap")) {
      Interceptor.attach(m.virtualAddress, { onEnter: function (a) {
        try {
          const n = m.parameterCount || 0, args = [];
          for (let i = 0; i < n; i++) {
            let ty = "?";
            try { ty = m.parameters[i].type.name; } catch (e) {}
            args.push(/Int32|Single|Boolean/.test(ty) ? a[i + 1].toInt32() : "ptr");
          }
          if (log.length < 2000) log.push({ tag: "battlePrep", t: Date.now(), pc: n, args: args });
        } catch (e) {}
      }});
    }
  } catch (e) {}
  return { drain: () => log.splice(0, log.length), n: () => log.length };
})();
"__w installed n=" + __w.n()
"""

DRAIN = 'JSON.stringify(__w.drain())'


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: frida_watch.py <out.ndjson> [minutes]", file=sys.stderr)
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
            except Exception as e:  # agent died
                fh.write(json.dumps({"err": str(e)}, ensure_ascii=False) + "\n")
                fh.flush()
                break
            for row in rows:
                fh.write(json.dumps(row, ensure_ascii=False) + "\n")
            fh.flush()
            time.sleep(0.25)
    print("watcher stopped", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
