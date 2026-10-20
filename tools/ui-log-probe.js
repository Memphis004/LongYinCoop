// S1-c on-screen log probe (read-only) — works in battle only.
// Generic GetComponentsInChildren<T> cannot be invoked through the bridge ("expected an integer"),
// so enumerate UnityEngine.UI.Text instances from the GC heap and keep the ones whose ancestor
// chain reaches BattleController.infoText. Order = sibling index.
(() => {
  const A = Il2Cpp.domain.assembly("Assembly-CSharp");
  const bc = A.image.class("BattleController").method("get_Instance").invoke();
  if (!bc) return JSON.stringify({ error: "no BattleController" });

  const frame = Il2Cpp.domain.assembly("UnityEngine.CoreModule")
    .image.class("UnityEngine.Time").method("get_frameCount").invoke();

  const info = bc.field("infoText").value;
  if (!info) return JSON.stringify({ frame: frame, error: "no infoText" });

  const infoTr = info.method("get_transform").invoke();
  const TextClass = Il2Cpp.domain.assembly("UnityEngine.UI").image.class("UnityEngine.UI.Text");
  const all = Il2Cpp.gc.choose(TextClass, 600);

  const hits = [];
  let depthSeen = {};
  for (let i = 0; i < all.length; i++) {
    try {
      const tr = all[i].method("get_transform").invoke();
      let cur = tr.method("get_parent").invoke();
      let depth = 1;
      let hit = false;
      while (cur && depth <= 3) {
        if (cur.handle === infoTr.handle) { hit = true; break; }
        cur = cur.method("get_parent").invoke();
        depth++;
      }
      if (!hit) continue;
      const txt = all[i].field("m_Text").value;
      if (txt === null || txt === undefined) continue;
      const sib = tr.method("GetSiblingIndex").invoke();
      hits.push({ depth: depth, sib: sib, text: txt });
      depthSeen[depth] = (depthSeen[depth] || 0) + 1;
    } catch (e) { }
  }
  hits.sort((a, b) => (a.depth - b.depth) || (a.sib - b.sib));

  return JSON.stringify({
    frame: frame,
    infoCount: bc.field("infoCount").value,
    totalTexts: all.length,
    matched: hits.length,
    depths: depthSeen,
    lines: hits.slice(-30).map(h => h.text)
  });
})()
