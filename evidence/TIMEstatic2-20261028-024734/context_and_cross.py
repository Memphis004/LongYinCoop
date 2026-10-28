# context_and_cross.py — A5 follow-up (attempt 2 within budget):
# (1) disasm context before the needAutoSave candidate stores in ChangeDay/ChangeMonth/Update
#     to identify the base object (rip-load -> static slot / call chain).
# (2) filter the 355 cross-module [reg+0x18] byte-store owners: keep only methods that also
#     call PlotController$$get_Instance (0x68EBD0) or any PlotController$$ method.
import re
import sys
from pathlib import Path

sys.path.insert(0, r"C:\Users\memph\LongYinCoop\evidence\TIMEstatic-20261027-051647")
import capstone
from time_static_common import GameImage

OUT = Path(__file__).resolve().parent
gi = GameImage()
md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
md.detail = True

def ctx(site_rva, back=40, fwd=4):
    start = site_rva - 0x200
    code = gi.read(start, 0x200 + 0x40)
    out = []
    for insn in md.disasm(code, gi.rva_to_va(start)):
        if insn.address >= gi.rva_to_va(site_rva + fwd * 4):
            break
        rva = gi.va_to_rva(insn.address)
        if rva >= start:
            name = ""
            if insn.mnemonic == "call":
                t = re.search(r"0x([0-9a-f]+)", insn.op_str)
                if t:
                    name = gi.rva_name(int(t.group(1), 16) - gi.image_base) or ""
            if insn.mnemonic.startswith("mov") and "rip" in insn.op_str:
                m = re.search(r"\[rip \+ (0x[0-9a-f]+)\]|\[rip - (0x[0-9a-f]+)\]", insn.op_str)
                slot = None
                if m:
                    slot = (insn.address + insn.size + (int(m.group(1), 16) if m.group(1) else -int(m.group(2), 16))) - gi.image_base
                    nm = gi.meta_by_slot.get(slot, "")
                    out.append(f"  {rva:#x}  {insn.mnemonic} {insn.op_str}   ; slot={slot:#x} {nm}")
                    continue
            out.append(f"  {rva:#x}  {insn.mnemonic} {insn.op_str}  {name}")
    return "\n".join(out)

with open(OUT / "needautosave-context.txt", "w", encoding="utf-8") as f:
    for label, site in [("Update 0x7e3d4e (rbx=?)", 0x7E3D4E),
                        ("ChangeDay 0x792216 (rcx=[rax+0x184])", 0x792216),
                        ("ChangeDay 0x7923d7", 0x7923D7),
                        ("ChangeDay 0x792586 (rbx origin const?)", 0x792586),
                        ("ChangeMonth 0x793f47", 0x793F47),
                        ("ChangeMonth 0x79404f", 0x79404F)]:
        f.write(f"===== {label} =====\n{ctx(site)}\n\n")
print("wrote needautosave-context.txt")

# (2) cross-module 0x18 filter
plot_getinst = gi.resolve("PlotController$$get_Instance")
plot_any = [r for n, r, _ in [(m[1], m[0], m) for m in gi.methods] if n.startswith("PlotController$$")]
plot_any_set = set(plot_any)
print(f"PlotController methods: {len(plot_any_set)}, get_Instance at {[hex(r) for r in plot_getinst]}")

owners = set()
for line in (OUT / "all-stores-0x18-nongc.tsv").read_text(encoding="utf-8").splitlines():
    if line.startswith("#") or line.startswith("ERROR") or not line.strip():
        continue
    owners.add(line.split("\t")[0])
print(f"distinct non-PlotController owners with [reg+0x18] byte-store: {len(owners)}")

# find which of these methods call any PlotController$$ method
methods = gi.methods
n = len(methods)
hits = []
for idx, (start, name, _s) in enumerate(methods):
    if name not in owners:
        continue
    end = methods[idx + 1][0] if idx + 1 < n else start + 0x10000
    if end - start <= 0 or end - start > 0x80000:
        continue
    code = gi.read(start, end - start)
    found = []
    pos = 0
    while True:
        pos = code.find(b"\xe8", pos)
        if pos < 0 or pos + 5 > len(code):
            break
        rel = int.from_bytes(code[pos + 1: pos + 5], "little", signed=True)
        tgt = start + pos + 5 + rel
        if tgt in plot_any_set:
            ok = False
            for insn in md.disasm(code[pos: pos + 5], 0):
                ok = insn.mnemonic == "call" and insn.size == 5
                break
            if ok:
                found.append(gi.rva_name(tgt))
        pos += 1
    if found:
        hits.append((name, sorted(set(found))))
with open(OUT / "plothappen-crossmodule-candidates.tsv", "w", encoding="utf-8") as f:
    f.write("# owner\tcalls-PlotController-methods\n")
    for name, found in hits:
        f.write(f"{name}\t{','.join(found)}\n")
print(f"cross-module candidates calling PlotController: {len(hits)}")
for name, found in hits:
    print(" ", name, "->", ",".join(found[:4]))
