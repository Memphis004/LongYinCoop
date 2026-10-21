import json,re,sys,statistics,time
sys.stdout.reconfigure(encoding="utf-8",errors="replace")
GAME=r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan"
SNAP=GAME+r"\UserData\LongYinCoop\S2\s2-snapshots.ndjson"
SAM=r"coop-test\S2a2-20261021-2254\sampler.ndjson"
frames=[]
with open(SNAP,encoding="utf-8",errors="replace") as f:
    for line in f:
        line=line.strip()
        if not line or '"marker":true' in line: continue
        try: o=json.loads(line)
        except Exception: continue
        if "units" in o: frames.append(o)
print("frames:",len(frames),"seq range",frames[0]["seq"],"->",frames[-1]["seq"])
# exit 2: cost per frame
cost=[f.get("sampleMs",0)+f.get("serializeMs",0) for f in frames]
cost_sorted=sorted(cost)
def pct(v,p): return v[min(len(v)-1,int(len(v)*p))]
print("exit2 sampleMs+serializeMs: mean=%.4f p95=%.4f p99=%.4f max=%.4f ms (n=%d)"%(
    statistics.mean(cost),pct(cost_sorted,0.95),pct(cost_sorted,0.99),max(cost),len(cost)))
print("     sampleMs p99=%.4f  serializeMs p99=%.4f"%(
    pct(sorted(f.get("sampleMs",0) for f in frames),0.99),pct(sorted(f.get("serializeMs",0) for f in frames),0.99)))
# exit 8: unit count
uc=sorted({f.get("unitCount") for f in frames})
print("exit8 unitCount values:",uc)
# exit 5: any corpse?
corps=[(f["seq"],u["id"],u.get("hp"),u.get("isAlive"),u.get("unitAlive")) for f in frames for u in f["units"] if (u.get("hp")==0 or u.get("isAlive") is False or u.get("unitAlive") is False)]
print("exit5 corpse frames:",len(corps), corps[:5])
# exit 4 supplement: isAlive(replica) vs unitAlive(host spike status) at the frozen key
rows=[]
with open(SAM,encoding="utf-8") as f:
    for line in f:
        line=line.strip()
        if not line: continue
        try: o=json.loads(line)
        except Exception: continue
        if "nowActiveStateName" in o: rows.append(o)
last=rows[-1]; key=(round(last["battleTime"],3),int(last["turnIndex"]))
cand=[f for f in frames if (round(f["battleTime"],3),int(f["turnIndex"]))==key]
fr=cand[-1]
host={str(u.get("id")):u for u in last["units"]}
bad=[];n=0
for u in fr["units"]:
    h=host.get(str(u["id"]))
    if not h: continue
    n+=1
    if bool(u.get("isAlive"))!=bool(h.get("unitAlive")): bad.append((u["id"],u.get("isAlive"),h.get("unitAlive")))
    if bool(u.get("unitAlive"))!=bool(h.get("unitAlive")): bad.append((u["id"],"unitAlive",u.get("unitAlive"),h.get("unitAlive")))
print("exit4 supplement (isAlive/unitAlive replica vs host unitAlive): units=%d mismatches=%d %s"%(n,len(bad),bad[:5]))
