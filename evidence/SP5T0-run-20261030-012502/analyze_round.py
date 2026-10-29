#!/usr/bin/env python3
"""analyze_round.py — offline accounting analysis for SP5T0-run-20261030-012502.

Read-only: parses the probe NDJSON and evaluates the round-validity conditions
and equation skeletons of sp5-t0-plan rev 13 §2.3/§5. Placed in the evidence
folder (not repo tools/) because repo sp5t0_audit.py was written against a
perForce-dict schema while the probe writes perForce as a 30-slot list —
schema mismatch recorded verbatim in the report; this script adapts to the
REAL schema and implements the semantic patch.audit comparison (separator
normalization) that the in-probe comparator failed on (probe bug, recorded).
"""
import json
import sys
from collections import Counter

PATH = sys.argv[1]

evs = [json.loads(l) for l in open(PATH, encoding="utf-8") if l.strip()]

def ev(name):
    return [e for e in evs if e["ev"] == name]

def norm(s):
    # normalize HarmonyX patch description separators: id:Type::Method -> id.Type.Method
    return s.replace("::", ".").replace(":", ".")

out = []
def p(line=""):
    print(line)
    out.append(line)

# ---------- timeline ----------
sb = ev("sng.begin")[0]["t"]
se = ev("sng.end")[0]["t"]
ses0 = ev("session.begin")[0]["t"]
ses1 = ev("session.end")[0]
p("=== TIMELINE ===")
p(f"session.begin t={ses0}  sng.begin t={sb} (+{(sb-ses0)/1000:.1f}s)  sng.end t={se} (+{(se-sb)/1000:.1f}s)")
p(f"session.end t={ses1['t']} (+{(ses1['t']-se)/1000:.1f}s settle) reason={ses1['reason']} durMs={ses1['durMs']}")

# ---------- round validity: phaseStuck / rebaseline / stack ----------
p("")
p("=== ROUND VALIDITY ===")
p(f"phaseStuck events: {len(ev('phaseStuck'))}  (summary counters: phaseStuck={ses1.get('phaseStuck')}, rebaseline={ses1.get('rebaseline')}, readfail={ses1.get('readfail')}, errors={ses1.get('errors')})")

# ---------- patch.audit: 4 checkpoints, semantic comparison ----------
p("")
p("=== PATCH.AUDIT (4 checkpoints) ===")
pa = ev("patch.audit")
p(f"checkpoints found: {[e['at'] for e in pa]}")
all_semantic_match = True
for e in pa:
    sem_ok = True
    details = []
    for ov, d in e["ov"].items():
        inv = d["inv"]
        exp = {
            "pre":  [f"longyincoop.sp5t0.probe:Sp5T0Probe::GenData{ov}Prefix"],
            "post": [f"longyincoop.sp5t0.probe:Sp5T0Probe::GenData{ov}Postfix"],
            "fin":  [f"longyincoop.sp5t0.probe:Sp5T0Probe::GenData{ov}Finalizer"],
            "trans": [], "ilm": [],
        }
        for cat in exp:
            got = inv.get(cat, [])
            if sorted(norm(x) for x in got) != sorted(norm(x) for x in exp[cat]):
                sem_ok = False
                details.append(f"{ov}.{cat} got={got}")
    all_semantic_match = all_semantic_match and sem_ok
    p(f"  at={e['at']:<16} probeAllMatch={e['allMatch']}  SEMANTIC-match(normalized)={sem_ok}  stableBetween={e['allStableBetweenCheckpoints']}")
    if details:
        p("    " + "; ".join(details))
p(f"semantic inventory match (owner+method+counts, separator-normalized) ALL checkpoints: {all_semantic_match}")
p("  [บั๊กที่พบ] comparator ใน probe เทียบ string รูป 'ns.Type.Method' กับที่ GetPatchInfo คืนรูป 'ns:Type::Method' -> allMatch=False เป็น artifact ของรูปแบบสายอักขระ ไม่ใช่ patch แปลกปลอม (inventory ดิบใน log = เฉพาะ patch ของ probe ครบทุก category)")
p(f"inventoryStabilityInWindow = {pa[0]['inventoryStabilityInWindow']}")

# ---------- iid linkage integrity ----------
p("")
p("=== IID LINKAGE (969 triples) ===")
calls = ev("factory.call"); res = ev("factory.result"); fin = ev("factory.finalizer")
ci = {e["iid"] for e in calls}; ri = {e["iid"] for e in res}; fi = {e["iid"] for e in fin}
p(f"calls={len(calls)} results={len(res)} finalizers={len(fin)}")
p(f"unique iids: calls={len(ci)} results={len(ri)} finalizers={len(fi)}")
p(f"call->result->finalizer complete triples: {len(ci & ri & fi)}  missing-result: {len(ci - ri)}  missing-finalizer: {len(ci - fi)}")
p(f"orphan result/finalizer (ไม่มี call): {len(ri - ci)} / {len(fi - ci)}")
ints = [int(i) for i in ci]
p(f"iid monotonic-unique: {len(ints) == len(set(ints))}  min={min(ints)} max={max(ints)}")
p(f"exceptionAtProbeFinalizer != null: {sum(1 for e in fin if e['exceptionAtProbeFinalizer'] is not None)}")
p(f"phasePop=false: {sum(1 for e in fin if not e.get('phasePop'))}")
p(f"resultNonNull=false: {sum(1 for e in res if not e['resultNonNull'])}")
p(f"ov ที่เกิดจริง: {Counter(e['ov'] for e in calls)}")

# ---------- buckets + E5 ----------
p("")
p("=== BUCKETS + E5 ===")
fb = Counter((e["bucket"], e["ph"]) for e in calls)
p("factory.call by (bucket, phase):")
for k in sorted(fb):
    p(f"  {k}: {fb[k]}")
unattr = sum(v for (b, ph), v in fb.items() if ph == "sng.unattributed")
p(f"sng.unattributed factory calls = {unattr}  (E5: 0 => เส้นทาง creation อธิบายครบด้วย W2/W3)")
p(f"factory calls ใน bucket creation ที่ phase ไม่ใช่ sng.genhero/sng.genenemy: {sum(v for (b, ph), v in fb.items() if b == 'creation' and ph not in ('sng.genhero', 'sng.genenemy'))}")
p(f"factory/addtemp events ใน bucket post: {sum(1 for e in calls + ev('addtemp') if e['bucket'] == 'post')}  (คาดหมาย 0 — ผู้ใช้ไม่ได้ข้ามวัน)")
p(f"factory/addtemp events ใน bucket pre: {sum(1 for e in calls + ev('addtemp') if e['bucket'] == 'pre')}")

# ---------- worldcount snapshots: E7 + census ----------
p("")
p("=== WORLD COUNT SNAPSHOTS (E7 + census) ===")
wc = ev("worldcount")
identity = {(e["wid"], e["hidH"], e["hidT"]) for e in wc if e["at"] != "session.begin"}
p(f"instance identity (wid,hidH,hidT) หลัง sng.begin คงที่ทุก snapshot: {len(identity) == 1} -> {identity}")
for e in wc:
    h = e.get("h", 0)
    hist = e.get("hist") or {}
    pf = e.get("perForce") or []
    ok_sum = (sum(hist.values()) == h) if hist else "n/a (h=0 world null)"
    ok_pf = (sum(pf) == hist.get("sect", 0)) if hist and pf else "n/a"
    p(f"  at={e['at']:<15} h={h:<4} Σhist==h: {ok_sum}  ΣperForce==hist[sect]: {ok_pf}  hist={hist}  speIdOutOfRange={e.get('speIdOutOfRange')}")

h_genhero_b = [e for e in wc if e["at"] == "genhero.begin"][0]["h"]
h_genhero_e = [e for e in wc if e["at"] == "genhero.end"][0]["h"]
h_sng_e = [e for e in wc if e["at"] == "sng.end"][0]["h"]
dH = h_genhero_e - h_genhero_b
p(f"ΔH (genhero.begin->end) = {dH} ; H(sng.end)==H(genhero.end) same-instance: {h_sng_e == h_genhero_e}")

# ---------- W2/W3 splits (E1/E2 skeletons) ----------
p("")
p("=== E1/E2 SKELETONS (ตัวเลขประกอบ — #C.ok คง UNKNOWN ตาม rev 13) ===")
w2 = [e for e in calls if e["bucket"] == "creation" and e["ph"] == "sng.genhero"]
w3 = [e for e in calls if e["bucket"] == "creation" and e["ph"] == "sng.genenemy"]
w2r = {e["iid"]: e for e in res}
w2f = {e["iid"]: e for e in fin}
w2_post_notemp = sum(1 for e in w2 if not w2r[e["iid"]]["postIsTempHero"] and not w2r[e["iid"]]["postIsRandomEnemy"])
w3_post_temp = sum(1 for e in w3 if w2r[e["iid"]]["postIsTempHero"] and w2r[e["iid"]]["postIsRandomEnemy"])
p(f"W2 (sng.genhero): calls={len(w2)}  all-ov: {Counter(e['ov'] for e in w2)}  post !temp&&!random: {w2_post_notemp}")
p(f"E1 skeleton: ΔH({dH}) == #C(W2,!temp,!random)({w2_post_notemp}) + 1(protag) => {dH} == {w2_post_notemp + 1} : {dH == w2_post_notemp + 1}")
ge_b = ev("genenemy.begin")[0]; ge_e = ev("genenemy.end")[0]
at3 = [e for e in ev("addtemp") if e["bucket"] == "creation"]
p(f"W3 (sng.genenemy): calls={len(w3)}  post temp&&random: {w3_post_temp}  addtemp(creation)={len(at3)}  tempBefore={ge_b['tempBefore']} tempAfter={ge_e['tempAfter']}")
dT = ge_e["tempAfter"] - ge_b["tempBefore"]
p(f"E2 skeleton: ΔT({dT}) == #addtemp(W3)({len(at3)}) == #C(W3,post temp&&random)({w3_post_temp}) => {dT == len(at3) == w3_post_temp}")

# ---------- heroNum + E4 observations ----------
p("")
p("=== heroNum.read + E4 observations ===")
hn = ev("heroNum.read")[0]
p(f"heroNum: forces={hn['forces']} entries={hn['entries']} total={sum(sum(x) for x in hn['force'])} speDb(table)={hn['speDb']} wid/hidH/hidT ตรง snapshot: {(hn['wid'], hn['hidH'], hn['hidT']) in identity}")
p(f"heroNum array = {hn['force']}  (Σ={sum(sum(x) for x in hn['force'])} = ยอด generic ที่ลูปควรสร้าง)")
spe_calls = sum(1 for e in calls if w2r[e['iid']]['postSpeHero'])
p(f"factory.postSpeHero=true: {spe_calls} calls (จำแนก SpeHero จาก post-state; E4 = กฎสรุปจากหลักฐานรวม — ประเมินในรายงาน)")
arg_temps = Counter((e['argIsTempHero'], e['argIsRandomEnemy']) for e in calls)
p(f"arg flags (isTemp,isRandom): {dict(arg_temps)}")
db_nn = Counter('null' if e.get('db') is None else ('nonNull' if e['db']['nonNull'] else 'null-arg') for e in calls)
p(f"heroDataBase param (overload C): {dict(db_nn)}")
db_vs_spe = Counter(('null' if e.get('db') is None else ('nonNull' if e['db']['nonNull'] else 'null-arg'),
                     'spe' if w2r[e['iid']]['postSpeHero'] else 'generic') for e in calls)
p(f"db × postSpeHero cross-tab: {dict(db_vs_spe)}")
at_iids = {e['iid'] for e in ev('addtemp')}
p(f"addtemp iid ทับกับ factory iid: {len(at_iids & ci)} (iid counter ใช้ร่วมกันคนละ kind: factory {len(ci)} + addtemp {len(at_iids)} = {len(ci | at_iids)} unique, max={max(int(i) for i in at_iids | ci)})")

json.dump({"report": out}, open(sys.argv[2], "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(f"\n[saved] {sys.argv[2]}")
