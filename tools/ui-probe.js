// S1-c UI probe (read-only). Run once per sample via il2cpp exec_js.
// Returns Time.frameCount + game-side values for the current battle so the caller
// can join it against capture/battle.ndjson by exact frame number.
// NOTE: frida-il2cpp-bridge returns a truthy wrapper for null IL2CPP refs; always
// check handle !== "0x0" before touching fields.
(() => {
  const nonNull = (o) => (o && o.handle && o.handle !== "0x0") ? o : null;
  const A = Il2Cpp.domain.assembly("Assembly-CSharp");
  const bc = nonNull(A.image.class("BattleController").method("get_Instance").invoke());
  if (!bc) return JSON.stringify({ error: "no BattleController" });

  const frame = Il2Cpp.domain.assembly("UnityEngine.CoreModule")
    .image.class("UnityEngine.Time").method("get_frameCount").invoke();

  const units = [];
  let skipped = 0;
  const teams = nonNull(bc.field("teams").value);
  const tn = teams ? teams.method("get_Count").invoke() : 0;
  for (let t = 0; t < tn; t++) {
    const team = nonNull(teams.method("get_Item").invoke(t));
    if (!team) continue;
    const list = nonNull(team.field("battleUnits").value);
    if (!list) continue;
    const n = list.method("get_Count").invoke();
    for (let i = 0; i < n; i++) {
      // stale units can hold dangling refs outside a battle — one bad unit must not kill the probe
      try {
        const u = nonNull(list.method("get_Item").invoke(i));
        if (!u) continue;
        const h = nonNull(u.field("heroData").value);
        if (!h) continue;
        const g = nonNull(u.field("mapGrid").value);
        units.push({
          teamIndex: t,
          index: i,
          heroID: h.field("heroID").value,
          hp: h.field("hp").value,
          maxHp: h.field("maxhp").value,
          shownHp: h.field("shownHp").value,
          shownMaxHp: h.field("shownMaxHp").value,
          row: g ? g.field("row").value : -1,
          col: g ? g.field("column").value : -1
        });
      } catch (e) { skipped++; }
    }
  }

  let activeHero = null, activeGrid = null;
  try {
    const active = nonNull(bc.field("nowActiveUnit").value);
    if (active) {
      activeHero = nonNull(active.field("heroData").value);
      activeGrid = nonNull(active.field("mapGrid").value);
    }
  } catch (e) { }

  // on-screen log text: อ่านแยกด้วย tools/ui-log-probe.js (generic GetComponentsInChildren ใช้ผ่าน bridge ไม่ได้)

  return JSON.stringify({
    frame: frame,
    unitCount: units.length,
    skipped: skipped,
    activeHeroID: activeHero ? activeHero.field("heroID").value : null,
    activeCell: activeGrid ? { row: activeGrid.field("row").value, col: activeGrid.field("column").value } : null,
    units: units.slice(0, 16)
  });
})()
