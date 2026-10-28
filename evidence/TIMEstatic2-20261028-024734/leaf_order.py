# leaf_order.py — A5(ng): condition/call order of GameController.ChangeDay() leaf 0x790E30 from the
# stored body file (read-only evidence from TIMEstatic-20261027-051647/bodies).
# Emits an ordered filtered listing (cmp/test/jcc/jmp/call) with resolved call names and
# basic-block-entry markers, so the housekeeping condition order can be composed by hand.
import re
import sys
from pathlib import Path

sys.path.insert(0, r"C:\Users\memph\LongYinCoop\evidence\TIMEstatic-20261027-051647")
from time_static_common import GameImage

OUT = Path(__file__).resolve().parent
BODY = Path(r"C:\Users\memph\LongYinCoop\evidence\TIMEstatic-20261027-051647\bodies\GameController_ChangeDay_leaf.txt")
gi = GameImage()

pat = re.compile(r"^(0x[0-9a-f]+)\s+(\S+)\s*(.*)$")
insns = []
for line in BODY.read_text(encoding="utf-8").splitlines():
    m = pat.match(line.strip())
    if not m:
        continue
    va = int(m.group(1), 16)
    insns.append((va, m.group(2), m.group(3).strip()))
print(f"parsed {len(insns)} insns from body file")

image_base = gi.image_base
# jump/call targets -> basic block entries
targets = set()
for va, mn, ops in insns:
    if mn.startswith(("j", "call")):
        t = re.search(r"0x([0-9a-f]+)", ops)
        if t:
            targets.add(int(t.group(1), 16))

FILTER_MN = ("cmp", "test", "call", "jmp", "je", "jne", "jg", "jge", "jl", "jle",
             "ja", "jae", "jb", "jbe", "js", "jns", "jo", "jno", "jp", "jnp")
rows = []
for va, mn, ops in insns:
    if mn not in FILTER_MN and not mn.startswith("j"):
        continue
    rva = va - image_base
    name = ""
    if mn == "call":
        t = re.search(r"0x([0-9a-f]+)", ops)
        if t:
            name = gi.rva_name(int(t.group(1), 16) - image_base) or gi.rva_name(int(t.group(1), 16)) or ""
            if not name:
                r = int(t.group(1), 16) - image_base
                name = gi.rva_name(r) or ""
    mark = "<- BB" if va in targets else ""
    rows.append((va, rva, mn, ops, name, mark))

with open(OUT / "changeday-leaf-order.tsv", "w", encoding="utf-8") as f:
    f.write("# va\trva\tmnemonic\toperands\tresolved\tbb-entry\n")
    for va, rva, mn, ops, name, mark in rows:
        f.write(f"{va:#x}\t{rva:#x}\t{mn}\t{ops}\t{name}\t{mark}\n")
print(f"wrote changeday-leaf-order.tsv ({len(rows)} filtered rows)")

# compact call list in order
print("\n=== ordered calls ===")
for va, rva, mn, ops, name, mark in rows:
    if mn == "call":
        print(f"{rva:#x}  {name or ops}")
