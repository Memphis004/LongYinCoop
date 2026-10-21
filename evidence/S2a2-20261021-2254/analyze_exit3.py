import json,re,sys,statistics,time
sys.stdout.reconfigure(encoding="utf-8",errors="replace")
S=r"coop-test\S2a2-20261021-2254\sampler.ndjson"
L=r"evidence\S2a2-20261021-2254\frame-time-ab.txt"
rows=[]
with open(S,encoding="utf-8") as f:
    for line in f:
        line=line.strip()
        if not line: continue
        try: o=json.loads(line)
        except Exception: continue
        if "nowActiveStateName" in o: rows.append(o)
starts={}; ends={}
for line in open(L,encoding="utf-8"):
    m=re.search(r"BLOCK (\w\d) start=(\d\d:\d\d:\d\d)",line)
    if m: starts[m.group(1)]=m.group(2)
    m=re.search(r"BLOCK (\w\d) end=(\d\d:\d\d:\d\d)",line)
    if m: ends[m.group(1)]=m.group(2)
print("starts",starts); print("ends",ends)
def sec(h):
    hh,mm,ss=[int(x) for x in h.split(":")]; return hh*3600+mm*60+ss
def hhmmss(t):
    lt=time.localtime(t); return "%02d:%02d:%02d"%(lt.tm_hour,lt.tm_min,lt.tm_sec)
win=[]
for name in ("A1","B1","A2","B2"):
    a=sec(starts[name]); b=sec(ends[name])
    sel=[r for r in rows if a<=sec(hhmmss(r["t"]))<b]
    dt=sel[-1]["t"]-sel[0]["t"]; df=sel[-1]["frame"]-sel[0]["frame"]
    mean_clock=1000.0*dt/df
    dts=[r.get("deltaTime",0)*1000 for r in sel]
    win.append((name,len(sel),dt,df,mean_clock,statistics.mean(dts),max(dts)))
    print("%s win=%s..%s n=%d dt=%.3fs dframes=%d mean(clock)=%.4f ms mean(deltaTime)=%.4f ms max(dt)=%.3f"
          %(name,starts[name],ends[name],len(sel),dt,df,mean_clock,statistics.mean(dts),max(dts)))
A=[w[4] for w in win if w[0][0]=="A"]; B=[w[4] for w in win if w[0][0]=="B"]
Ad=[w[5] for w in win if w[0][0]=="A"]; Bd=[w[5] for w in win if w[0][0]=="B"]
def verdict(d, margin=1.0, band=0.5):
    if abs(d-margin)<=band: return "INCONCLUSIVE"
    return "PASS" if d<=margin else "FAIL"
a=statistics.mean(A); b=statistics.mean(B); d=b-a
a2=statistics.mean(Ad); b2=statistics.mean(Bd); d2=b2-a2
print()
print("exit3 EST1 Δframe/Δt : A=%.4f B=%.4f delta=%.4f margin=1.00 band=0.50 verdict=%s"%(a,b,d,verdict(d)))
print("exit3 EST2 deltaTime : A=%.4f B=%.4f delta=%.4f verdict=%s"%(a2,b2,d2,verdict(d2)))
print("per-block (clock):",[(w[0],round(w[4],4)) for w in win])
