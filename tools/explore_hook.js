/*
tools/explore_hook.js - READ-ONLY observer for the Explorer / dungeon survey.
Paste into il2cpp-frida-mcp `il2cpp_exec_js`, then:
  __ex.inspect('click', 48)  overloads/RVA + disasm of one method
  __ex.hook()                Interceptor.attach (log only)
  __ex.map()                 tile-map summary of ExploreController.explorePanelData
  __ex.dump(60)              per-method counts + call log
  __ex.unhook() / __ex.reset()

Read-only by construction: no Method.invoke on game state, no field/memory write,
no Interceptor.replace. Only field reads + logged arguments.
*/
globalThis.__ex = (() => {
  "use strict";

  const S = { hooked: false, listeners: [], calls: [], counts: {}, target: [], installSnap: null };
  const asserted = { callsInvoke: false, writesMemory: false, usesReplace: false };

  function asm() { return Il2Cpp.domain.assembly("Assembly-CSharp"); }

  const T = [
    ["ExploreController", "ExploreTileClicked", 1, "click"],
    ["ExploreController", "PlayerEnterGrid", 3, "enter3"],
    ["ExploreController", "PlayerEnterGrid", 2, "enter2"],
    ["ExploreController", "PlayerFinishMove", 0, "finishedMove"],
    ["ExploreController", "ManageTileEvent", 1, "tileEvent"],
    ["ExploreController", "ManagePlayerAroundGrid", 3, "around"],
    ["ExploreController", "ChangeMoveStep", 2, "step2"],
    ["ExploreController", "ChangeMoveStep", 1, "step1"],
    ["ExploreController", "ManageMoveStepLimit", 0, "stepLimit"],
    ["ExploreController", "GenerateExploreMapData", 3, "genData"],
    ["ExploreController", "GenerateExploreMap", 3, "genMap"],
    ["ExploreController", "GenerateExplorePanel", 1, "genPanel"],
    ["ExploreController", "FinishExploreMap", 1, "finish"],
    ["ExploreController", "ExploreFail", 0, "fail"],
    ["ExploreController", "QuitExploreButtonClicked", 0, "quit"],
    ["ExploreController", "SeeAllTile", 0, "seeAll"],
    ["ExploreController", "WatchRoundTile", 1, "watchRound"],
    ["ExploreController", "WatchRandomTile", 1, "watchRandom"],
    ["ExploreController", "ChangeKeyNum", 1, "keyNum"],
    ["ExploreController", "PlayerCanPassObstacle", 2, "passObstacle"],
    ["PlotController", "StartExplore", 1, "startExplore"],
    ["PlotController", "StartExploreFuc", 3, "startExploreFuc"],
    ["PlotController", "ExploreAreaStart", 0, "areaStart"],
    ["PlotController", "ExploreAreaFinish", 0, "areaFinish"],
    ["PlotController", "FinishExploreWildMap", 0, "finishWild"],
    ["PlotController", "FinishExploreMazeMap", 0, "finishMaze"],
    ["PlotController", "FinishExploreArea", 0, "finishArea"],
    ["PlotController", "ExploreAreaSuccess", 1, "areaSuccess"],
    ["PlotController", "ExploreRandomFightResult", 1, "randomFightResult"],
    ["GameController", "PrepareBattleMap", 6, "battle6"],
    ["GameController", "PrepareBattleMap", 7, "battle7"],
    ["GameController", "PrepareBattleMap", 8, "battle8"],
    ["GameController", "PrepareBattleMap", 9, "battle9"],
    ["GameController", "PrepareBattleMap", 10, "battle10"],
    ["GameController", "Save", 1, "save"]
  ];

  function ptype(m, i) {
    try { return m.parameters[i].type.name; } catch (e) { return "?"; }
  }

  function info(m) {
    let o = { name: "?", params: "?", types: [], rva: "?", va: "?", static: "?" };
    try { o.name = m.name; } catch (e) {}
    try { o.params = m.parameterCount; } catch (e) {}
    try {
      for (let i = 0; i < m.parameterCount; i++) { o.types.push(ptype(m, i)); }
    } catch (e) {}
    try { o.va = m.virtualAddress.toString(); } catch (e) {}
    try { o.rva = "0x" + m.virtualAddress.sub(Il2Cpp.module.base).toUInt32().toString(16); } catch (e) {}
    try { o.static = m.isStatic; } catch (e) {}
    return o;
  }

  function resolve(cls, name, pc) {
    let all = [];
    try { all = asm().image.class(cls).methods.filter(function (m) { return m.name === name; }); }
    catch (e) { return null; }
    for (let i = 0; i < all.length; i++) {
      let n = -1;
      try { n = all[i].parameterCount; } catch (e) { continue; }
      if (n === pc) { return all[i]; }
    }
    return null;
  }

  function allT() {
    return T.map(function (t) {
      return { cls: t[0], name: t[1], pc: t[2], tag: t[3], m: resolve(t[0], t[1], t[2]) };
    });
  }

  function arr(list) {
    let out = [];
    try {
      const items = list.field("_items").value;
      const n = list.field("_size").value;
      for (let i = 0; i < n; i++) {
        let v = null;
        try { v = items.get(i); } catch (e) {}
        if (v && v.handle && v.handle.isNull()) { continue; }
        out.push(v);
      }
    } catch (e) { return []; }
    return out;
  }

  function num(o, k, d) {
    try { return o.field(k).value; } catch (e) { return d === undefined ? null : d; }
  }

  function str(o, k) {
    try { return o.field(k).value.content; } catch (e) { return null; }
  }

  function ctl() {
    try {
      const hits = Il2Cpp.gc.choose(asm().image.class("ExploreController"));
      return (hits && hits.length) ? hits[0] : null;
    } catch (e) { return null; }
  }

  function clock() {
    try {
      const gc = Il2Cpp.gc.choose(asm().image.class("GameController"))[0];
      const wd = gc.field("worldData").value;
      const t = wd.field("worldTime").value;
      return { y: num(t, "year"), mo: num(t, "month"), d: num(t, "day"),
               hour: +wd.field("hour").value.toFixed(3) };
    } catch (e) { return { error: e.message }; }
  }

  function quick() {
    const c = ctl();
    if (!c) { return { noController: true }; }
    let p = null;
    try { p = c.field("explorePanelData").value; } catch (e) {}
    return {
      inited: !!num(c, "inited"),
      leftPower: num(c, "leftPower"),
      maxPower: p ? num(p, "maxPower") : null,
      keyNum: p ? num(p, "keyNum") : null,
      seedBase: num(c, "nowSeedBase"),
      seedSpe: num(c, "nowSeedSpe"),
      clock: clock()
    };
  }

  const EVT = ["Empty", "Step", "Watch", "RoundWatch", "Resource", "Money", "LivingSkillExp",
    "State", "InternalInjury", "PoisonInjury", "Item", "Fight", "Event", "Obstacle",
    "Teleport", "ExternalInjury", "Key", "WildPeople", "DigTreasure", "LockTreasureChest",
    "CollectResource", "SpeMasterEvent", "SpeStoneEvent"];
  const GND = ["Road", "Plane", "Forest", "Mountain", "River"];
  const WAL = ["None", "Wall", "Door"];

  function tileOf(t) {
    function g(k) { try { return num(t, k); } catch (e) { return null; } }
    const ev = g("exploreTileEventType"), gr = g("exploreTileGroundType"), wl = g("wallType");
    let obs = null;
    try {
      const o = t.field("exploreTileObstacleData").value;
      obs = !!(o && !o.handle.isNull());
    } catch (e) {}
    return {
      r: g("row"), c: g("column"), ev: ev, evName: EVT[ev] || ("?" + ev),
      ground: gr, groundName: GND[gr] || ("?" + gr),
      wall: wl, wallName: WAL[wl] || ("?" + wl),
      doorOpen: !!g("doorOpen"), eventHappen: !!g("eventHappen"),
      seen: !!g("seen"), moveAble: !!g("moveAble"),
      enemyDiff: g("enemyDifficulty"), enemyNum: g("enemyNum"),
      targetResource: g("targetResource"), hasObstacle: obs,
      ptr: t.handle.toString(), name: str(t, "name")
    };
  }

  function inspect(methodName, n) {
    const ts = allT();
    const found = ts.filter(function (t) { return t.m; }).map(function (t) {
      const o = info(t.m);
      o.tag = t.tag; o.cls = t.cls;
      return o;
    });
    const missing = ts.filter(function (t) { return !t.m; })
      .map(function (t) { return t.cls + "." + t.name + "/" + t.pc; });
    let body = [];
    if (methodName) {
      const t = ts.find(function (x) { return x.tag === methodName || x.name === methodName; });
      if (t && t.m) {
        let cur = t.m.virtualAddress;
        try {
          for (let i = 0; i < (n || 48); i++) {
            const ins = Instruction.parse(cur);
            body.push(ins.address + "  " + ins.mnemonic + " " + ins.opStr);
            cur = ins.next;
          }
        } catch (e) { body.push("stop " + e.message); }
      }
    }
    return JSON.stringify({ found: found, missing: missing, body: body, hooked: S.hooked }, null, 1);
  }

  function argSum(ty, a) {
    if (/Boolean/.test(ty)) { return "bool:" + (a.toInt32() !== 0); }
    if (/Int32|Int64|Single|Double|Byte|Enum/.test(ty)) { return "num:" + a.toInt32(); }
    if (/String|List|ExploreTileData|GameObject|MapData|PanelData/.test(ty)) { return "ptr@" + a.toString(); }
    return "ptr@" + a.toString();
  }

  function hook() {
    if (S.hooked) { return "already hooked"; }
    if (asserted.callsInvoke || asserted.writesMemory || asserted.usesReplace) {
      return "REFUSED: violates read-only contract";
    }
    const ts = allT().filter(function (t) { return t.m; });
    if (!ts.length) { return "REFUSED: no targets resolved"; }
    ts.forEach(function (t) {
      try {
        const types = [];
        for (let i = 0; i < t.pc; i++) { types.push(ptype(t.m, i)); }
        const l = Interceptor.attach(t.m.virtualAddress, {
          onEnter: function (args) {
            try {
              const e = { tag: t.tag, t: Date.now(),
                          args: types.map(function (ty, i) { return argSum(ty, args[i + 1]); }),
                          state: quick() };
              if (/^battle/.test(t.tag)) { e.battleType = args[1].toInt32(); }
              if (/click|tileEvent|enter2/.test(t.tag)) { e.arg0ptr = args[1].toString(); }
              if (t.tag === "save") { e.saveID = args[1].toInt32(); }
              S.calls.push(e);
              S.counts[t.tag] = (S.counts[t.tag] || 0) + 1;
            } catch (err) { S.calls.push({ tag: t.tag, error: err.message }); }
          }
        });
        S.listeners.push(l);
        S.target.push({ tag: t.tag, cls: t.cls, va: t.m.virtualAddress.toString(), types: types });
      } catch (e) { S.calls.push({ tag: t.tag, attachFailed: e.message }); }
    });
    S.hooked = true;
    S.installSnap = quick();
    return JSON.stringify({ hooked: true, attached: S.target.length, targets: S.target,
                            installSnap: S.installSnap }, null, 1);
  }

  function map(lim) {
    const c = ctl();
    if (!c) { return JSON.stringify({ error: "no ExploreController" }); }
    let p = null;
    try { p = c.field("explorePanelData").value; } catch (e) {}
    if (!p || p.handle.isNull()) { return JSON.stringify({ error: "explorePanelData null" }); }
    const tiles = arr(p.field("exploreTiles").value);
    const byEv = {}, byGnd = {}, byWal = {};
    let seen = 0, mv = 0, evFlag = 0, obs = 0, doorOpen = 0;
    const named = [];
    tiles.forEach(function (t) {
      const d = tileOf(t);
      byEv[d.evName] = (byEv[d.evName] || 0) + 1;
      byGnd[d.groundName] = (byGnd[d.groundName] || 0) + 1;
      byWal[d.wallName] = (byWal[d.wallName] || 0) + 1;
      if (d.seen) { seen++; }
      if (d.moveAble) { mv++; }
      if (d.eventHappen) { evFlag++; }
      if (d.hasObstacle) { obs++; }
      if (d.doorOpen) { doorOpen++; }
      if (named.length < (lim || 10) && (d.ev !== 0 || d.wall !== 0 || d.hasObstacle)) { named.push(d); }
    });
    let md = null, st = null;
    try { md = c.field("exploreMapData").value; } catch (e) {}
    try {
      const s = p.field("startTile").value;
      st = (s && !s.handle.isNull()) ? tileOf(s) : null;
    } catch (e) {}
    return JSON.stringify({
      clock: clock(), inited: !!num(c, "inited"), leftPower: num(c, "leftPower"),
      seedBase: num(c, "nowSeedBase"), seedSpe: num(c, "nowSeedSpe"),
      mapName: (md && !md.handle.isNull()) ? str(md, "exploreName") : null,
      difficulty: (md && !md.handle.isNull()) ? num(md, "exploreDifficulty") : null,
      exploreType: num(p, "exploreType"), mapType: num(p, "exploreMapType"),
      w: num(p, "mapWidth"), h: num(p, "mapHeight"),
      maxPower: num(p, "maxPower"), keyNum: num(p, "keyNum"), showFinal: num(p, "showFinal"),
      finishFuc: str(p, "finishFuc"), finishParam: str(p, "finishParam"),
      startTile: st, totalTiles: tiles.length, seen: seen, moveAble: mv,
      eventHappenFlags: evFlag, obstacles: obs, doorOpen: doorOpen,
      byEv: byEv, byGnd: byGnd, byWal: byWal, samples: named,
      rateRewarded: (function () {
        try { return arr(c.field("exploreRateRewarded").value).map(function (x) { return !!x; }); }
        catch (e) { return null; }
      })(),
      successPlot: str(c, "successCallPlot"), failPlot: str(c, "failCallPlot")
    }, null, 1);
  }

  function dump(lim) {
    return JSON.stringify({ hooked: S.hooked, counts: S.counts, installSnap: S.installSnap,
      now: quick(), callsTotal: S.calls.length,
      calls: S.calls.slice(0, lim || 60) }, null, 1);
  }

  function unhook() {
    let n = 0;
    S.listeners.forEach(function (l) { try { l.detach(); n++; } catch (e) {} });
    S.listeners = []; S.hooked = false;
    return "detached " + n + " calls=" + S.calls.length;
  }

  function reset() {
    if (S.hooked) { return "REFUSED: unhook first"; }
    const d = S.calls.length;
    S.calls = []; S.counts = {}; S.installSnap = null; S.target = [];
    return "reset dropped " + d;
  }

  return { asserted: asserted, inspect: inspect, hook: hook, map: map, dump: dump,
           snap: quick, reset: reset, unhook: unhook, state: S };
})();

typeof __ex.hook === "function" ? "__ex ready" : "load failed";
