import json
import sys

path = sys.argv[1]
bad = 0
total = 0
first = None
sample = None
with open(path, "r", encoding="utf-8", errors="replace") as f:
    for i, line in enumerate(f, 1):
        total += 1
        s = line.rstrip("\n")
        if not s:
            continue
        try:
            obj = json.loads(s)
            if sample is None:
                sample = obj
        except Exception as e:
            bad += 1
            if first is None:
                first = (i, len(s), str(e), repr(s[max(0, 5560):5620]))
print("lines", total, "bad", bad)
if first:
    print("first bad line#", first[0], "len", first[1], "err", first[2])
    print("ctx", first[3])
if sample:
    npc = (sample.get("npcs") or [{}])[0]
    print("sample npc0 name repr:", repr(npc.get("name")))
    print("sample hud time repr:", repr((sample.get("hud") or {}).get("time")))
