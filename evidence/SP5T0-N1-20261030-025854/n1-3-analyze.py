#!/usr/bin/env python3
"""N1-3 loop attribution — offline analysis of the T0 round NDJSON (no game launch).

Answers: how much of the 562 sect population traces to the heroNum table (A1 reach)?
Run:  python n1-3-analyze.py <path\\to\\sp5t0.ndjson>
"""
import json
import sys
from collections import Counter

path = sys.argv[1]
evs = [json.loads(l) for l in open(path, encoding="utf-8") if l.strip()]
res = {e["iid"]: e for e in evs if e["ev"] == "factory.result"}
gen = [e for e in evs if e["ev"] == "factory.call" and e["ph"] == "sng.genhero"
       and not res[e["iid"]]["postSpeHero"]]
heroNum = [[4, 4, 3, 3, 2, 1], [5, 4, 4, 3, 3, 2], [5, 5, 4, 4, 3, 2], [6, 5, 5, 4, 4, 3]]  # [d][edi]

seq = [(res[e["iid"]]["postBelongForceID"], int(e.get("heroForceLv"))) for e in gen]
cnt = Counter(seq)
forces = sorted({f for f, _ in seq})

runs = []
start = 0
for i in range(1, len(seq) + 1):
    if i == len(seq) or seq[i][0] != seq[start][0]:
        runs.append((seq[start][0], i - start))
        start = i
print(f"runs ต่อเนื่องราย force: {len(runs)} (ก้อนสุดท้าย = force {runs[-1][0]} n={runs[-1][1]})")

fit_report = {}
fits = 0
for f in forces:
    obs = [cnt.get((f, edi), 0) for edi in range(6)]
    best = None
    for d in range(4):
        within = sum(1 for edi in range(6) if heroNum[d][edi] <= obs[edi] <= heroNum[d][edi] + 1)
    if best is None or within > best[1]:
        best = (d, within)
    fit_report[f] = (obs, best)
    if f >= 0 and best[1] >= 5:
        fits += 1
print(f"sect forces ที่ fit แถว heroNum ใดๆ ภายใน [+0,+1] ต่อ band >=5/6: {fits}/30")
for f in forces:
    obs, (d, w) = fit_report[f]
    print(f"  force {f:>3}: obs={obs} best d={d} within={w}/6")

agg = [sum(cnt.get((f, edi), 0) for f in forces if f >= 0) for edi in range(6)]
avg = [round(sum(heroNum[d][edi] for d in range(4)) / 4, 2) for edi in range(6)]
print("รวม obs ต่อ band (30 forces):", agg, "/30 =", [round(a / 30, 2) for a in agg])
print("ค่าเฉลี่ย heroNum ต่อ band:", avg)

sect_total = sum(agg)
print(f"\nsect total = {sect_total} · A1 ครอบ {sect_total}/883 Heros = {sect_total/883*100:.1f}%")
light = 883 - sect_total // 2
print(f"Light 0.5 (สมมติ linear): Heros 883 -> ~{light} = ลด ~{883-light} ตัว ≈ {(883-light)/883*100:.1f}%")
