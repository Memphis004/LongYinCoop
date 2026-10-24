"""Read-only Explorer event probe.

Attaches once, hooks low-frequency Explorer/Plot/Battle entry points and records
a compact state snapshot on enter and leave (leftPower, keys, player money/items,
force storage money/items, force resourceStore, plot flags). No writes, no
Method.invoke on game logic except the pure WorldData.Player() getter.

Usage: python tools/frida_event_probe.py <out.ndjson> [minutes]
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
globalThis.__ev = (() => {
  const A = Il2Cpp.domain.assembly("Assembly-CSharp");
  const log = [];
  let ctl = Il2Cpp.gc.choose(A.image.class("ExploreController"))[0];
  let wd = Il2Cpp.gc.choose(A.image.class("GameController"))[0].field("worldData").value;
  let player = null, force = null;
  function arr(l) { try { const it = l.field("_items").value, n = l.field("_size").value, o = []; for (let i=0;i<n;i++){const x=it.get(i); if(x&&!x.handle.isNull())o.push(x);} return o; } catch(e){ return []; } }
  function refresh() {
    try { ctl = Il2Cpp.gc.choose(A.image.class("ExploreController"))[0]; } catch(e) {}
    try { wd = Il2Cpp.gc.choose(A.image.class("GameController"))[0].field("worldData").value; } catch(e) {}
    try { player = wd.method("Player").invoke(); } catch(e) { player = null; }
    force = null;
    try { if (player) { const fid = player.field("belongForceID").value; force = arr(wd.field("Forces").value).find(f => f.field("forceID").value === fid) || null; } } catch(e) {}
  }
  refresh();
  function sig(list) {
    try {
      return arr(list).map(i => { try { return i.field("itemID").value; } catch(e) { return -1; } }).sort((a,b) => a-b);
    } catch(e) { return []; }
  }
  function st() {
    const o = {};
    try { o.lp = ctl.field("leftPower").value; o.key = ctl.field("explorePanelData").value.field("keyNum").value; } catch(e) { refresh(); }
    try {
      const it = player.field("itemListData").value;
      o.money = it.field("money").value; o.items = arr(it.field("allItem").value).length;
      o.weight = +it.field("weight").value.toFixed(2);
      o.itemSig = sig(it.field("allItem").value);
    } catch(e) { o.money = null; }
    try {
      if (force) {
        const fs = force.field("forceStorage").value;
        o.fsMoney = fs.field("money").value; o.fsItems = arr(fs.field("allItem").value).length;
        o.fsSig = sig(fs.field("allItem").value);
        o.res = arr(force.field("resourceStore").value).map(x => +x.toFixed(2));
      }
    } catch(e) {}
    try {
      const gv = wd.field("governStorage").value;
      o.govMoney = gv.field("money").value; o.govSig = sig(gv.field("allItem").value);
    } catch(e) {}
    try {
      const pl = Il2Cpp.gc.choose(A.image.class("PlotController"))[0];
      o.pt = pl.field("plotTextShowing").value; o.pc = pl.field("plotChoiceShowing").value;
    } catch(e) {}
    return o;
  }
  function add(cn, mn, tag, re) {
    let ms = [];
    try { ms = A.image.class(cn).methods.filter(m => m.name === mn || (re && re.test(m.name))); } catch(e) { return; }
    for (const m of ms) {
      const n = m.parameterCount || 0;
      const ty = [];
      try { for (let i=0;i<n;i++) ty.push(m.parameters[i].type.name); } catch(e) {}
      try {
        Interceptor.attach(m.virtualAddress, {
          onEnter: function (a) {
            try {
              const e = { tag: tag || m.name, ph: "in", t: Date.now(), name: m.name, st: st() };
              if (n > 0 && /ExploreTileData/.test(ty[0])) { const t = new Il2Cpp.Object(a[1]); e.tile = [t.field("row").value, t.field("column").value, t.field("exploreTileEventType").value]; }
              if (log.length < 4000) log.push(e);
            } catch (err) {}
          },
          onLeave: function () {
            try { if (log.length < 4000) log.push({ tag: tag || m.name, ph: "out", t: Date.now(), name: m.name, st: st() }); } catch (err) {}
          }
        });
      } catch (e) {}
    }
  }
  add("ExploreController", "ManageTileEvent", "tileEvent");
  add("ExploreController", "FinishExploreMap", "finishMap");
  add("ExploreController", "ExploreFail", "fail");
  add("ExploreController", "ChangeKeyNum", "key");
  add("ExploreController", "WatchRandomTile", "watchRandom");
  add("ExploreController", "WatchRoundTile", "watchRound");
  add("PlotController", null, "plot", /Choose|Choosen|Collect|Dig|Watch|Success|FinishExplore|StartExplore/);
  add("BattleController", "PrepareBattleMap", "battlePrep");
  try {
    for (const m of A.image.class("BattleController").methods.filter(m => m.name === "PrepareBattleMap")) {
      Interceptor.attach(m.virtualAddress, { onEnter: function () { try { if (log.length < 4000) log.push({ tag: "battlePrep", ph: "in", t: Date.now(), st: st() }); } catch(e){} } });
    }
  } catch(e) {}
  return { drain: () => log.splice(0, log.length), n: () => log.length };
})();
"__ev installed n=" + __ev.n()
"""

DRAIN = "JSON.stringify(__ev.drain())"


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: frida_event_probe.py <out.ndjson> [minutes]", file=sys.stderr)
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
