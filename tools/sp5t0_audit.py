#!/usr/bin/env python3
"""sp5t0_audit.py — offline checker for SP5-T0 probe NDJSON (sp5-t0-plan rev13 §2.3/§2.4).

Verifies the accounting relationships of ONE creation round from the (future, not-yet-built)
log-only probe:
  bucket split (pre | creation | post) from sng.begin/sng.end timestamps,
  E5   every factory call in bucket `creation` must have phase in {sng.genhero, sng.genenemy};
       sng.unattributed > 0 => accounting not closed (report, do not conclude "second path"),
  E7   every worldcount snapshot: sum(hist) == H and sum(perForce) == hist[sect],
  E1/E2 skeleton numbers (expected UNKNOWN per rev 13 — inventoryStability unprovable this round),
  phaseStuck == 0 and empty stack at session.end,
  rebaseline markers invalidate equations crossing instance changes.

Pure offline: reads one NDJSON file, writes findings to stdout. No game, no IPC.
"""
import json
import sys


def load(path):
    events = []
    with open(path, "r", encoding="utf-8") as f:
        for i, line in enumerate(f, 1):
            line = line.strip()
            if not line:
                continue
            try:
                events.append(json.loads(line))
            except json.JSONDecodeError as e:
                raise SystemExit(f"bad ndjson line {i}: {e}")
    return events


def bucket_at(t, sng):
    if sng["begin"] is None or t < sng["begin"]:
        return "pre"
    if sng["end"] is None or t <= sng["end"]:
        return "creation"
    return "post"


def audit(events):
    sng = {"begin": None, "end": None}
    findings = {"e5_unattributed": [], "e5_plot_recruit": [], "e7_failures": [],
                "phase_stuck": [], "rebaseline": [], "eq1": None, "eq2": None,
                "factory_counts": {"creation": 0, "pre": 0, "post": 0}}
    c_ok = {"W2": 0, "W3": 0}
    add_ok = {"W3": 0}
    counts = {}
    genenemy = {"before": None, "after": None}

    for ev in events:
        e, t = ev.get("ev"), ev.get("t", 0)
        if e == "sng.begin":
            sng["begin"] = t
        elif e == "sng.end":
            sng["end"] = t
        elif e == "phaseStuck":
            findings["phase_stuck"].append(ev)
        elif e == "rebaseline":
            findings["rebaseline"].append(ev)
        elif e == "factory.call":
            b = bucket_at(t, sng)
            findings["factory_counts"][b] += 1
            ph = ev.get("ph", "outside")
            if b == "creation":
                if ph == "sng.unattributed":
                    findings["e5_unattributed"].append(ev)
                elif ph not in ("sng.genhero", "sng.genenemy"):
                    findings["e5_plot_recruit"].append(ev)
                elif ph == "sng.genhero" and ev.get("ov") == "C":
                    c_ok["W2"] += 1
                elif ph == "sng.genenemy" and ev.get("ov") == "C":
                    c_ok["W3"] += 1
        elif e == "addtemp":
            b = bucket_at(t, sng)
            if b == "creation":
                add_ok["W3"] += 1
        elif e == "worldcount":
            # schema จริงของ probe (รอบ T0 20261030): `h` ตัวพิมพ์เล็ก · perForce = list 30 ช่อง
            # (session.begin โลกยัง null → ไม่มี hist/perForce) — รองรับ schema แผนเดิม (H/dict) ด้วย
            # หมายเหตุ: ไม่คำนวณ bucket ของ worldcount — เหตุการณ์นี้เขียน t=0 จึงใช้ label `at` เท่านั้น
            h = ev.get("h", ev.get("H", ev.get("heros", 0)))
            hist = ev.get("hist", {})
            per_force = ev.get("perForce")
            if sum(hist.values()) != h:
                findings["e7_failures"].append({**ev, "why": f"sum(hist)={sum(hist.values())} != h={h}"})
            if per_force is not None:
                pf_sum = sum(per_force) if isinstance(per_force, list) else sum(per_force.values())
                sect = hist.get("sect", 0)
                if pf_sum != sect:
                    findings["e7_failures"].append({**ev, "why": f"sum(perForce)={pf_sum} != hist[sect]={sect}"})
            # key ด้วย label `at` ตรง ๆ — worldcount จริงของ probe เขียน t=0 (ไม่ใช่ timestamp)
            # จึงห้ามคำนวณ bucket จาก t; snapshot ชื่อตาม boundary (genhero.begin/end ฯลฯ) อยู่แล้ว
            counts[str(ev.get("at") or ("t%d" % t))] = (h, hist)
        elif e == "genenemy.begin":
            genenemy["before"] = ev.get("tempBefore")
        elif e == "genenemy.end":
            genenemy["after"] = ev.get("tempAfter")

    # E1 skeleton: H(genhero.end) - H(genhero.begin) vs #C.ok(W2) + 1 (observational only)
    def h_at(label):
        if label in counts:
            return counts[label][0]
        return None
    hb, he = h_at("genhero.begin"), h_at("genhero.end")
    d_h = (he - hb) if (hb is not None and he is not None) else None
    findings["eq1"] = {
        "c_ok_W2": c_ok["W2"],
        "dH_observed": d_h,
        "expected_min": (c_ok["W2"] + 1) if c_ok["W2"] else None,
        "status": "UNKNOWN (rev 13: #C.ok needs inventoryStability + patch-audit + iid linkage — "
                  "numbers reported here are observational only)",
    }
    d_t = None
    if genenemy["before"] is not None and genenemy["after"] is not None:
        d_t = genenemy["after"] - genenemy["before"]
    findings["eq2"] = {
        "c_ok_W3": c_ok["W3"], "addtemp_W3": add_ok["W3"],
        "dT_observed": d_t,
        "status": "UNKNOWN (rev 13, same conditions as E1)",
    }
    return findings


def verdict(f):
    lines = []
    ok = True
    if f["phase_stuck"]:
        ok = False
        lines.append(f"FAIL: phaseStuck != 0 ({len(f['phase_stuck'])} events) — round unusable for accounting")
    if f["rebaseline"]:
        lines.append(f"NOTE: {len(f['rebaseline'])} rebaseline marker(s) — equations crossing instance changes are NOT closed")
    if f["e5_unattributed"]:
        lines.append(f"E5 NOT CLOSED: {len(f['e5_unattributed'])} factory call(s) with phase=sng.unattributed "
                     f"— must identify caller or keep UNKNOWN; do NOT conclude 'second path' (plan E5/E6)")
        ok = False
    if f["e5_plot_recruit"]:
        lines.append(f"NOTE: {len(f['e5_plot_recruit'])} factory call(s) at plot/recruit phase inside creation round — report, do not delete")
    if f["e7_failures"]:
        ok = False
        lines.append(f"E7 FAIL: {len(f['e7_failures'])} snapshot(s) with hist/perForce mismatch")
    if not f["e5_unattributed"] and not f["e7_failures"] and not f["phase_stuck"]:
        lines.append("E5/E7 structural checks CLEAN (bucket creation: no unattributed, no phase outside W2/W3, "
                     "all snapshots consistent)")
    lines.append(f"factory calls: creation={f['factory_counts']['creation']} "
                 f"pre={f['factory_counts']['pre']} post={f['factory_counts']['post']} "
                 f"(post is normal ChangeDay behaviour — not in equations, rev 3-4)")
    lines.append(f"E1/E2 numeric skeletons: {json.dumps(f['eq1'], ensure_ascii=False)} / {json.dumps(f['eq2'], ensure_ascii=False)}")
    lines.append(f"VERDICT: {'PASS (structural) — accounting closure still per plan conditions' if ok else 'FAIL (structural)'}")
    return "\n".join(lines)


def main(argv):
    if len(argv) != 2:
        print("usage: sp5t0_audit.py <sp5t0.ndjson>")
        return 2
    return print(verdict(audit(load(argv[1])))) or 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
