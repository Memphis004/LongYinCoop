/*
tools/changeday_hook.js — READ-ONLY observer for GameController.ChangeDay(int)
=============================================================================
Purpose: count and log ChangeDay calls while a `>>` skip runs, to answer "does
skip call ChangeDay once, N times, or not at all?".

How to use (il2cpp-frida-mcp):
  1. paste this whole file into `il2cpp_exec_js` (defines globalThis.__cd)
  2. __cd.inspect()     -> signature + RVA + N disassembled instructions (no hook)
  3. __cd.snapshot()    -> read-only state snapshot (worldTime/WorkingUI/force)
  4. __cd.hook()        -> Interceptor.attach, LOG ONLY
  5. (user presses >>)
  6. __cd.dump()        -> print collected calls, __cd.snapshot() again
  7. __cd.unhook()

Hard rules enforced by construction (see __cd.asserted):
  - never calls Method.invoke / any game method
  - never writes a field or memory
  - never uses Interceptor.replace
  - only reads fields + the logged argument
If a rule would be broken the module refuses to install the hook.
*/

globalThis.__cd = (() => {
  const state = { hooked: false, listeners: [], log: [], count: 0, target: null };

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

  function pickIntOverload() {
    for (const m of variants()) {
      let pc = -1;
      try { pc = m.parameterCount; } catch (e) { continue; }
      if (pc === 1) return m;
    }
    return null;
  }

  function readMethodInfo(m) {
    const info = { name: '?', params: '?', rva: '?', va: '?', static: '?' };
    try { info.name = m.name; } catch (e) {}
    try { info.params = m.parameterCount; } catch (e) {}
    try { info.va = m.virtualAddress.toString(); } catch (e) {}
    try { info.rva = '0x' + (m.virtualAddress.sub(Il2Cpp.module.base).toUInt32()).toString(16); } catch (e) {}
    try { info.static = m.isStatic; } catch (e) {}
    return info;
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
    const target = pickIntOverload();
    const body = target ? disasm(target.virtualAddress, nInstr || 40) : [];
    return JSON.stringify({ overloads: info, hooked: state.hooked, body }, null, 1);
  }

  function hook() {
    if (state.hooked) return 'already hooked';
    const m = pickIntOverload();
    if (!m) return 'ChangeDay(int) not found - abort';
    // guard: refuse if we ever add an invoke/replace path
    if (asserted.callsInvoke || asserted.writesMemory || asserted.usesReplace) {
      return 'REFUSED: module violates read-only contract';
    }
    const before = snapshot();
    const listener = Interceptor.attach(m.virtualAddress, {
      onEnter(args) {
        const delta = args[1].toInt32();
        state.count += 1;
        state.log.push({
          n: state.count, t: Date.now(), delta,
          dayBefore: before.day,
        });
      },
      onLeave() {
        const last = state.log[state.log.length - 1];
        if (last) last.doneAt = Date.now();
      },
    });
    state.listeners.push(listener);
    state.hooked = true;
    state.target = { va: m.virtualAddress.toString() };
    state.before = before;
    return JSON.stringify({ hooked: true, va: state.target.va, before });
  }

  function dump(limit) {
    const n = limit || 50;
    return JSON.stringify({
      hooked: state.hooked,
      total: state.count,
      before: state.before || null,
      after: snapshot(),
      calls: state.log.slice(0, n),
      sums: state.log.reduce((a, c) => a + c.delta, 0),
    }, null, 1);
  }

  function unhook() {
    let n = 0;
    for (const l of state.listeners) { try { l.detach(); n++; } catch (e) {} }
    state.listeners = [];
    state.hooked = false;
    return `detached ${n} listener(s); total calls seen=${state.count}`;
  }

  return { asserted, inspect, snapshot, hook, dump, unhook, state };
})();

typeof __cd.hook === 'function' ? '__cd ready' : 'load failed';
