# scan_hudpanel.py — A5(kh): full direct-caller set of HudController.HudPanelActive (0xB09ED0),
# read-only. E8/E9 validated scan + FF15 slot scan (expected 0 per previous round).
import sys
from pathlib import Path

sys.path.insert(0, r"C:\Users\memph\LongYinCoop\evidence\TIMEstatic-20261027-051647")
from time_static_common import GameImage

OUT = Path(__file__).resolve().parent
gi = GameImage()

targets = gi.resolve("HudController$$HudPanelActive")
print("HudController$$HudPanelActive rva:", [hex(t) for t in targets])

hits = gi.scan_direct_xrefs(targets)
lines = []
for h in hits:
    s, e = gi.owner_range(h["site_rva"])
    inside = "in-range" if (s is not None and s <= h["site_rva"] < e) else "OUT-OF-RANGE!"
    lines.append(f"{h['site_rva']:#x}\t{h['kind']}\t{h['owner']}\t{inside}")
print(f"direct xrefs: {len(hits)}")

ff = gi.scan_ff15_xrefs([])
# FF15: also try slot-based lookup via ScriptMetadataMethod name
ff_targets = []
for slot, nm in gi.meta_by_slot.items():
    if "HudPanelActive" in nm:
        ff_targets.append(slot)
ff2 = gi.scan_ff15_xrefs(ff_targets) if ff_targets else []
print(f"FF15 slots named HudPanelActive: {len(ff_targets)}; FF15 sites: {len(ff2)}")

with open(OUT / "hudpanel-callers.tsv", "w", encoding="utf-8") as f:
    f.write("# site_rva\tkind\towner\tin-range-check\n")
    f.writelines(l + "\n" for l in lines)
    f.write(f"# FF15 sites: {len(ff2)}\n")
    for h in ff2:
        f.write(f"{h['site_rva']:#x}\tFF15\t{h['owner']}\tslot={h['slot_name']}\n")
print("wrote hudpanel-callers.tsv")
for l in lines:
    print(l)
