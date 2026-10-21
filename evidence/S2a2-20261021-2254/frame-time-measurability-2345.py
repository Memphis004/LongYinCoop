#!/usr/bin/env python3
"""One-off (S2a-2 item 1): can the 500 ms recorder estimate the 30 s block mean
frame time, how big is the natural block-to-block noise, and is the sampled
`unscaledDeltaTime` a usable per-frame signal?
Input: a real 100 ms `spike status` series (S1.5gB phase B, 100 ms nominal).
Read-only analysis. All printed frame times are milliseconds.
"""
import json
import statistics

PATH = r"C:\Users\memph\LongYinCoop\evidence\S1.5gB-20261021-200449\logs\status-100ms.ndjson"
BLOCK_S = 30.0

rows = []
with open(PATH, "r", encoding="utf-8") as fh:
    for line in fh:
        line = line.strip()
        if not line or '"ok": true' not in line:
            continue
        try:
            d = json.loads(line)
        except Exception:
            continue
        t, fr = d.get("t"), d.get("frame")
        if t is None or fr is None:
            continue
        rows.append((float(t), int(fr), d.get("unscaledDeltaTime"), bool(d.get("inBattle"))))
rows.sort()
n = len(rows)
q = lambda p, a: a[min(len(a) - 1, int(p * len(a)))]

print("samples=%d span=%.1fs inBattle=%d" % (n, rows[-1][0] - rows[0][0], sum(1 for r in rows if r[3])))
dts = [rows[i + 1][0] - rows[i][0] for i in range(n - 1) if rows[i + 1][0] > rows[i][0]]
print("recorder dt(ms): p50=%.2f p95=%.2f max=%.2f" % (1000 * statistics.median(dts),
      1000 * q(0.95, sorted(dts)), 1000 * max(dts)))

ft = [1000.0 * (rows[i + 1][0] - rows[i][0]) / (rows[i + 1][1] - rows[i][1])
      for i in range(n - 1) if rows[i + 1][1] > rows[i][1]]
ft.sort()
print("frame-derived instantaneous mean frame time(ms): p50=%.3f p95=%.3f p99=%.3f max=%.3f" % (
    statistics.median(ft), q(0.95, ft), q(0.99, ft), ft[-1]))

ud = sorted(1000.0 * float(r[2]) for r in rows if r[2] is not None and 0 < float(r[2]) < 1.0)
print("sampled unscaledDeltaTime(ms): p50=%.3f p95=%.3f p99=%.3f max=%.3f (n=%d)" % (
    statistics.median(ud), q(0.95, ud), q(0.99, ud), ud[-1], len(ud)))


def block_means(step):
    """30 s block mean frame time (ms) from the series; step=0 uses every sample,
    step=0.5 emulates the S2a-2 recorder (nearest sample to each 0.5 s boundary)."""
    out, t0 = [], rows[0][0]
    while True:
        lo, hi = t0, t0 + BLOCK_S
        pts = [r for r in rows if lo <= r[0] <= hi]
        if len(pts) < 3:
            break
        if step > 0:
            picked, k = [], 0
            while lo + k * step <= hi:
                target = lo + k * step
                best = min(pts, key=lambda r: abs(r[0] - target))
                if not picked or best is not picked[-1]:
                    picked.append(best)
                k += 1
            pts = picked
        if len(pts) >= 3 and pts[-1][0] > pts[0][0]:
            out.append((lo, 1000.0 * (pts[-1][0] - pts[0][0]) / (pts[-1][1] - pts[0][1]), len(pts)))
        t0 = hi
    return out


ref, sim = block_means(0.0), block_means(0.5)
print("blocks ref=%d sim=%d" % (len(ref), len(sim)))
diffs = sorted(abs(s[1] - r[1]) for r, s in zip(ref, sim))
print("|500ms-sim minus 100ms-reference| per 30s block(ms): median=%.4f p95=%.4f max=%.4f"
      % (statistics.median(diffs), q(0.95, diffs), diffs[-1]))
for name, series in (("reference(100ms)", ref), ("sim(500ms)", sim)):
    m = [b[1] for b in series]
    print("%s 30s block mean frame time(ms): min=%.3f max=%.3f spread=%.3f sd=%.3f  (samples/block %d..%d)"
          % (name, min(m), max(m), max(m) - min(m), statistics.pstdev(m),
             min(b[2] for b in series), max(b[2] for b in series)))
adj = sorted(abs(ref[i + 1][1] - ref[i][1]) for i in range(len(ref) - 1))
print("adjacent 30s blocks |delta|(ms): median=%.4f p95=%.4f max=%.4f" % (
    statistics.median(adj), q(0.95, adj), adj[-1]))
# how much of the natural spread survives if we compare the MEAN of 4 blocks
print("criterion context: margin=1.0ms band=0.5ms -> 0.5ms == %.1f fps at p50 frame time"
      % (1000.0 / (statistics.median(ft) - 0.5) - 1000.0 / statistics.median(ft)))
