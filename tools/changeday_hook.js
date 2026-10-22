/*
tools/changeday_hook.js — READ-ONLY observer for GameController.ChangeDay(int)
=============================================================================
Purpose: count and log ChangeDay calls while a `>>` skip runs, to answer "does
skip call ChangeDay once, N times, or not at all?".

How to use (il2cpp-frida-mcp):
  1. paste this whole file into `il2cpp_exec_js` (defines globalThis.__cd)
  2. __cd.inspect()     -> signature + param types + RVA + N disassembled instructions
  3. __cd.snapshot()    -> read-only state snapshot (worldTime/WorkingUI/force)
  4. __cd.hook()        -> Interceptor.attach on ChangeDay(int) AND ChangeDay(),
                           LOG ONLY (refuses static/ambiguous/same-address)
  5. (user presses >>)
  6. __cd.dump()        -> print collected calls, __cd.snapshot() again
  7. __cd.unhook()      -> detach; __cd.reset() before any second run

Known limits (measured/unmeasured as labelled in evidence/S2-cdprep-*):
  - field names come from DUMP; the List<T> access in snapshot() is GUESS
    until it is seen returning real values
  - onLeave assumes calls do not nest (doneAt maps to the newest entry)
  - the `asserted` flags are a tripwire, not a sandbox: they refuse a hook only
    if future code sets them True
  - snapshot() calls Il2Cpp.gc.choose(), so it must be run outside the hook
  - wrapper and leaf are attached together (2026-10-22 ruling A): wrapper calls
    give sum(delta), leaf hits give the day steps actually executed

Hard rules enforced by construction (see __cd.asserted):
  - never calls Method.invoke / any game method
  - never writes a field or memory
  - never uses Interceptor.replace
  - only reads fields + the logged argument
If a rule would be broken the module refuses to install the hook.
*/

globalThis.__cd = (() => {
  const state = {
    hooked: false, listeners: [], log: [], count: 0, target: null,
    dayHits: 0, dayLog: [], leaf: null,
  };

  const asserted = {
    callsInvoke: false,
    writesMemory: false,
    usesReplace: false,
  };

  const asm = () => Il2Cpp.domain.assembly('Assembly-CSharp');
  const gcClass = () => asm().image.class('GameController');

  function variants() {
    const klass = gcClass();
    let all = [];
    try { all = klass.methods.filter(m => m.name === 'ChangeDay'); } catch (e) { all = []; }
    return all;
  }

  // DUMP shows two overloads: ChangeDay(int deltaDay) and ChangeDay().
  // We accept ONLY the single-parameter one whose parameter type is Int32;
  // a count-only check would silently misread another 1-arg overload.
  function paramTypeName(m, i) {
    try { return m.parameters[i].type.name; } catch (e) { return '?'; }
  }

  function pickTarget() {
    const all = variants();
    if (!all.length) return { method: null, reason: 'ChangeDay not found on GameController' };
    const oneArg = [];
    for (const m of all) {
      let pc = -1;
      try { pc = m.parameterCount; } catch (e) { continue; }
      if (pc === 1) oneArg.push(m);
    }
    if (!oneArg.length) return { method: null, reason: 'no 1-parameter ChangeDay overload' };
    const ints = oneArg.filter(m => /(^|\.)Int32$/.test(paramTypeName(m, 0)));
    if (!ints.length) {
      return {
        method: null,
        reason: 'no ChangeDay(int) overload; saw ' +
          oneArg.map(m => paramTypeName(m, 0)).join(', '),
      };
    }
    if (ints.length > 1) return { method: null, reason: 'ambiguous: ' + ints.length + ' int overloads' };
    return { method: ints[0], reason: 'ok' };
  }

  function readMethodInfo(m) {
    const info = { name: '?', params: '?', paramTypes: [], rva: '?', va: '?', static: '?' };
    try { info.name = m.name; } catch (e) {}
    try { info.params = m.parameterCount; } catch (e) {}
    try {
      for (let i = 0; i < m.parameterCount; i++) info.paramTypes.push(paramTypeName(m, i));
    } catch (e) {}
    try { info.va = m.virtualAddress.toString(); } catch (e) {}
    try { info.rva = '0x' + (m.virtualAddress.sub(Il2Cpp.module.base).toUInt32()).toString(16); } catch (e) {}
    try { info.static = m.isStatic; } catch (e) {}
    return info;
  }

  // A method_ptr that starts with an immediate jump is a thunk, not the body:
  // hooking it still counts calls but disassembly would be meaningless.
  function thunkHint(addr) {
    try {
      const i = Instruction.parse(addr);
      const viaTrampoline = /^b(\.\w+)?$/.test(i.mnemonic);
      return viaTrampoline ? `first insn is ${i.mnemonic} ${i.opStr}` : 'first insn looks like a body';
    } catch (e) { return 'unreadable: ' + e.message; }
  }

  function disasm(addr, count) {
    const out = [];
    let cur = addr;
    try {
      for (let i = 0; i < count; i++) {
        const insn = Instruction.parse(cur);
        out.push(insn.address.toString() + '  ' + insn.mnemonic + ' ' + insn.opStr);
        cur = insn.next;
      }
    } catch (e) {
      out.push('disasm stopped: ' + e.message);
    }
    return out;
  }

  // ---- read-only snapshots (field reads only) ----------------------------
  function arr(list) {
    try {
      const items = list.field('_items').value;
      const n = list.field('_size').value;
      const out = [];
      for (let i = 0; i < n; i++) {
        let v = null;
        try { v = items.get(i); } catch (e) {}
        if (v && typeof v === 'object' && v.handle.isNull()) continue;
        out.push(v);
      }
      return out;
    } catch (e) { return []; }
  }

  function snapshot() {
    try {
      const wd = Il2Cpp.gc.choose(gcClass())[0].field('worldData').value;
      const t = wd.field('worldTime').value;
      const w = Il2Cpp.gc.choose(asm().image.class('WorkingUIController'))[0];
      const forces = arr(wd.field('Forces').value);
      const hero = arr(wd.field('Heros').value).find(h => h.field('heroID').value === 0);
      const force = forces.find(f => f.field('forceID').value === hero.field('belongForceID').value);
      const f = k => { try { return w.field(k).value; } catch (e) { return null; } };
      return {
        wall: Date.now(),
        year: t.field('year').value,
        month: t.field('month').value,
        day: t.field('day').value,
        hour: +wd.field('hour').value.toFixed(3),
        working: f('working'),
        workName: (() => { try { return f('workName').content; } catch (e) { return null; } })(),
        totalWorkingDay: f('totalWorkingDay'),
        leftWorkingDay: f('leftWorkingDay'),
        skipping: f('skipping'),
        workFinished: f('workFinished'),
        forceID: force ? force.field('forceID').value : null,
        store: force ? arr(force.field('resourceStore').value).map(x => +x.toFixed(1)) : null,
      };
    } catch (e) {
      return { error: e.message };
    }
  }

  // ---- modes -------------------------------------------------------------
  function inspect(nInstr) {
    const info = variants().map(readMethodInfo);
    const pick = pickTarget();
    const body = pick.method ? disasm(pick.method.virtualAddress, nInstr || 40) : [];
    return JSON.stringify({
      overloads: info,
      target: pick.method ? readMethodInfo(pick.method) : null,
      targetReason: pick.reason,
      thunk: pick.method ? thunkHint(pick.method.virtualAddress) : null,
      hooked: state.hooked,
      body,
    }, null, 1);
  }

  // The 0-parameter overload is the per-day body: ChangeDay(int) just calls it
  // delta times (see evidence/S2cd-20261022-0710/inspect-analysis.md). Counting
  // both separates "days requested" from "day steps actually executed".
  function pickLeaf() {
    for (const m of variants()) {
      let pc = -1;
      try { pc = m.parameterCount; } catch (e) { continue; }
      if (pc === 0) return m;
    }
    return null;
  }

  function hook() {
    if (state.hooked) return 'already hooked';
    if (state.count > 0 || state.dayHits > 0) {
      return 'REFUSED: previous run logged ' + state.count + ' wrapper / ' + state.dayHits +
        ' leaf call(s); call __cd.reset() first';
    }
    const pick = pickTarget();
    const m = pick.method;
    if (!m) return 'REFUSED: ' + pick.reason;
    // guard: refuse if we ever add an invoke/replace path
    if (asserted.callsInvoke || asserted.writesMemory || asserted.usesReplace) {
      return 'REFUSED: module violates read-only contract';
    }
    // arg index depends on the calling convention, so a static overload would
    // make args[1] the wrong slot and every logged delta garbage.
    let isStatic = null;
    try { isStatic = m.isStatic; } catch (e) {}
    if (isStatic !== false) return 'REFUSED: ChangeDay(int) isStatic=' + isStatic + ', arg slot unverified';
    const leaf = pickLeaf();
    if (leaf && leaf.virtualAddress.equals(m.virtualAddress)) {
      return 'REFUSED: both overloads resolve to the same address - double attach';
    }
    const before = snapshot();
    if (before && before.error) return 'REFUSED: snapshot failed at install: ' + before.error;
    const wrapper = Interceptor.attach(m.virtualAddress, {
      onEnter(args) {
        const delta = args[1].toInt32();
        state.count += 1;
        state.log.push({
          n: state.count, t: Date.now(), delta,
          // constant: read once at install, not per call (no managed field read
          // inside the hook, which would allocate and could perturb the body)
          dayAtInstall: before.day,
        });
      },
      onLeave() {
        const last = state.log[state.log.length - 1];
        if (last) last.doneAt = Date.now();
      },
    });
    state.listeners.push(wrapper);
    if (leaf) {
      const l = Interceptor.attach(leaf.virtualAddress, {
        onEnter() {
          state.dayHits += 1;
          state.dayLog.push({ n: state.dayHits, t: Date.now() });
        },
      });
      state.listeners.push(l);
      state.leaf = { va: leaf.virtualAddress.toString(), paramCount: 0 };
    }
    state.hooked = true;
    state.target = { va: m.virtualAddress.toString(), argIndex: 1, paramType: paramTypeName(m, 0) };
    state.before = before;
    return JSON.stringify({
      hooked: true, wrapper: state.target, leaf: state.leaf, before,
    }, null, 1);
  }

  function reset() {
    if (state.hooked) return 'REFUSED: unhook first';
    const dropped = state.count + '/' + state.dayHits;
    state.log = [];
    state.count = 0;
    state.dayLog = [];
    state.dayHits = 0;
    state.leaf = null;
    state.before = null;
    state.target = null;
    return 'reset: dropped ' + dropped + ' (wrapper/leaf) call(s)';
  }

  function dump(limit) {
    const n = limit || 50;
    return JSON.stringify({
      hooked: state.hooked,
      wrapperCalls: state.count,
      wrapperSumDelta: state.log.reduce((a, c) => a + c.delta, 0),
      leafHits: state.dayHits,
      before: state.before || null,
      after: snapshot(),
      calls: state.log.slice(0, n),
      leaf: state.dayLog.slice(0, n),
    }, null, 1);
  }

  function unhook() {
    let n = 0;
    for (const l of state.listeners) { try { l.detach(); n++; } catch (e) {} }
    state.listeners = [];
    state.hooked = false;
    return 'detached ' + n + ' listener(s); wrapper calls=' + state.count + ' leaf hits=' + state.dayHits;
  }

  return { asserted, inspect, snapshot, hook, reset, dump, unhook, state };
})();

typeof __cd.hook === 'function' ? '__cd ready' : 'load failed';
