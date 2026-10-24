"""Read-only dungeon battle probe.

Hooks BattleController.PrepareBattleMap (all overloads) and logs decoded arguments
(BattleType int, hero names/ids, string fightEndCall, bools), plus BattleRealEnd /
CheckBattleEnd and the plot callbacks used to return to Explorer. Read-only: no
invokes on game logic except field reads.

Usage: python tools/frida_battle_probe.py <out.ndjson> [minutes]
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
globalThis.__bt = (() => {
  const A = Il2Cpp.domain.assembly("Assembly-CSharp");
  const log = [];
  let ctl = Il2Cpp.gc.choose(A.image.class("ExploreController"))[0] || null;
  let bc = null;
  try { bc = Il2Cpp.gc.choose(A.image.class("BattleController"))[0] || null; } catch (e) {}
  function arr(l) { try { const it=l.field("_items").value, n=l.field("_size").value, o=[]; for(let i=0;i<n;i++){const x=it.get(i); if(x&&!x.handle.isNull())o.push(x);} return o; } catch(e){ return []; } }
  function heroName(p) { try { const h = new Il2Cpp.Object(p); return (h.field("heroName").value.content) + "#" + h.field("heroID").value; } catch(e) { return "hero?"; } }
  function heroList(p) { try { const l = new Il2Cpp.Object(p); return arr(l).map(h => { try { return h.field("heroName").value.content + "#" + h.field("heroID").value; } catch(e){ return "?"; } }); } catch(e) { return ["list?"]; } }
  function heroListList(p) { try { const l = new Il2Cpp.Object(p); return arr(l).map(x => heroList(x.handle)); } catch(e) { return [["list?"]]; } }
  function st() {
    const o = {};
    try { o.lp = ctl.field("leftPower").value; o.pos = new Il2Cpp.Object(ctl.field("playerGrid").value.handle).method("get_name").invoke().content; } catch(e) {}
    try { o.explore = ctl.field("inited").value; o.map = ctl.field("exploreMapData").value.field("exploreName").value.content; } catch(e) {}
    try { o.bt = bc.field("battleType").value; o.bs = bc.field("battleState").value; o.endCall = bc.field("fightEndCallFuc").value.content; } catch(e) {}
    return o;
  }
  function add(cn, mn, tag) {
    let ms = [];
    try { ms = A.image.class(cn).methods.filter(m => m.name === mn); } catch(e) { return; }
    for (const m of ms) {
      const n = m.parameterCount || 0, ty = [];
      try { for (let i=0;i<n;i++) ty.push(m.parameters[i].type.name); } catch(e) {}
      try {
        Interceptor.attach(m.virtualAddress, {
          onEnter: function (a) {
            try {
              const e = { tag: tag, ph: "in", name: mn, pc: n, ty: ty, t: Date.now(), st: st(), args: [] };
              for (let i=0;i<n;i++) {
                const t = ty[i] || "";
                if (/HeroData/.test(t)) e.args.push(heroName(a[i+1]));
                else if (/List`1/.test(t) && /HeroData/.test(t)) e.args.push(heroList(a[i+1]));
                else if (/List`1/.test(t) && /List/.test(t)) e.args.push(heroListList(a[i+1]));
                else if (/String/.test(t)) { try { e.args.push(new Il2Cpp.String(a[i+1]).content); } catch(x) { e.args.push("str?"); } }
                else if (/Boolean/.test(t)) e.args.push(a[i+1].toInt32() !== 0);
                else if (/Int32|Int64|Byte|BattleType|SupportType|Enum/.test(t)) e.args.push(a[i+1].toInt32());
                else e.args.push("ptr");
              }
              if (log.length < 2000) log.push(e);
            } catch(err) {}
          },
          onLeave: function () { try { if (tag === "prep" && log.length < 2000) log.push({ tag:"prep", ph:"out", t:Date.now(), st: st() }); } catch(e){} }
        });
      } catch(e) {}
    }
  }
  add("BattleController", "PrepareBattleMap", "prep");
  add("BattleController", "BattleRealEnd", "realEnd");
  add("BattleController", "CheckBattleEnd", "checkEnd");
  add("PlotController", "ExploreRandomFightResult", "fightResult");
  add("PlotController", "FinishExploreMap", "plotFinishMap");
  add("PlotController", "StartExploreFuc", "startExploreFuc");
  add("ExploreController", "FinishExploreMap", "finishMap");
  add("ExploreController", "QuitExploreButtonClicked", "quit");
  return { drain: () => log.splice(0, log.length), n: () => log.length };
})();
"__bt installed n=" + __bt.n()
"""

DRAIN = "JSON.stringify(__bt.drain())"


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: frida_battle_probe.py <out.ndjson> [minutes]", file=sys.stderr)
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
    print("battle probe stopped", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
