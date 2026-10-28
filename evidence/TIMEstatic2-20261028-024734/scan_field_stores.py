# scan_field_stores.py — A5(g)+(k): byte-store scan for [reg+0x28] (needAutoSave / mainThreadWaitForLock
# false positive) and [reg+0x18] (plotHappen), read-only.
# Scope: linear disasm of il2cpp ScriptMethod ranges (script.json) — NOT raw byte scanning; a store
# between/outside method bodies is out of scope (lower bound). Tools: pefile+capstone (installed).
import sys
from pathlib import Path

sys.path.insert(0, r"C:\Users\memph\LongYinCoop\evidence\TIMEstatic-20261027-051647")
import capstone
from time_static_common import GameImage

OUT = Path(__file__).resolve().parent
gi = GameImage()
md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
md.detail = True


def disasm_range(rva, end_rva):
    code = gi.read(rva, min(end_rva - rva, 0x80000))
    return list(md.disasm(code, gi.rva_to_va(rva)))


def analyze(want_disp, owner_prefix=None, all_owners=False):
    """Linear disasm every method (or only owner_prefix methods); for each byte-store
    [reg+disp] record site + base-register origin class:
      'this'      = base register value derived from method entry rcx (heuristic chain)
      'statics'   = base register loaded by a rip-relative mov r64,[rip+X] (static-field slot)
      'deref'     = base register loaded by mov r64,[r64] (object field deref)
      'other'     = anything else (incl. unknown)
    Returns list of dicts."""
    results = []
    regs64 = {"rax", "rbx", "rcx", "rdx", "rsi", "rdi", "rbp", "rsp",
              "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15"}
    methods = gi.methods
    n = len(methods)
    for idx, (start, name, _sig) in enumerate(methods):
        if not all_owners and owner_prefix and not name.startswith(owner_prefix):
            continue
        end = methods[idx + 1][0] if idx + 1 < n else start + 0x10000
        if end - start <= 0 or end - start > 0x80000:
            continue
        origin = {r: "unknown" for r in regs64}
        origin["rcx"] = "this"  # il2cpp instance methods: this in rcx at entry
        origin["rdx"] = "arg2"
        try:
            insns = disasm_range(start, end)
        except Exception as e:
            results.append({"error": f"{name}: {e}"})
            continue
        for i, insn in enumerate(insns):
            m, op = insn.mnemonic, insn.op_str
            if m.startswith("mov") and len(insn.operands) == 2:
                dst, src = insn.operands
                # track 64-bit register origins
                if dst.type == capstone.x86.X86_OP_REG and dst.reg in (
                        capstone.x86.X86_REG_RAX, capstone.x86.X86_REG_RBX, capstone.x86.X86_REG_RCX,
                        capstone.x86.X86_REG_RDX, capstone.x86.X86_REG_RSI, capstone.x86.X86_REG_RDI,
                        capstone.x86.X86_REG_RBP, capstone.x86.X86_REG_R8, capstone.x86.X86_REG_R9,
                        capstone.x86.X86_REG_R10, capstone.x86.X86_REG_R11, capstone.x86.X86_REG_R12,
                        capstone.x86.X86_REG_R13, capstone.x86.X86_REG_R14, capstone.x86.X86_REG_R15):
                    import capstone.x86 as x86
                    rname = insn.reg_name(dst.reg)
                    if src.type == capstone.x86.X86_OP_MEM and src.mem.base == capstone.x86.X86_REG_RIP:
                        origin[rname] = "statics"
                    elif src.type == capstone.x86.X86_OP_MEM and src.mem.base in (
                            capstone.x86.X86_REG_RAX, capstone.x86.X86_REG_RBX, capstone.x86.X86_REG_RCX,
                            capstone.x86.X86_REG_RDX, capstone.x86.X86_REG_RSI, capstone.x86.X86_REG_RDI,
                            capstone.x86.X86_REG_RBP, capstone.x86.X86_REG_R8, capstone.x86.X86_REG_R9,
                            capstone.x86.X86_REG_R10, capstone.x86.X86_REG_R11, capstone.x86.X86_REG_R12,
                            capstone.x86.X86_REG_R13, capstone.x86.X86_REG_R14, capstone.x86.X86_REG_R15) and src.mem.index == 0:
                        origin[rname] = "deref:" + insn.reg_name(src.mem.base) + "+" + str(src.mem.disp)
                    elif src.type == capstone.x86.X86_OP_REG:
                        srname = insn.reg_name(src.reg)
                        origin[rname] = origin.get(srname, "unknown")
                    else:
                        origin[rname] = "const"
            # byte store to [reg+disp]?
            if m in ("mov",) and len(insn.operands) == 2:
                dst, src = insn.operands
                if dst.type == capstone.x86.X86_OP_MEM and dst.size == 1:
                    base = dst.mem.base
                    if base != capstone.x86.X86_REG_RIP and dst.mem.disp == want_disp:
                        base_name = insn.reg_name(base) if base else "?"
                        results.append({
                            "owner": name,
                            "site_rva": hex(gi.va_to_rva(insn.address)),
                            "insn": f"{m} {op}",
                            "base": base_name,
                            "origin": origin.get(base_name, "unknown"),
                            "src_kind": ("imm" if src.type == capstone.x86.X86_OP_IMM
                                         else "reg" if src.type == capstone.x86.X86_OP_REG else "mem"),
                        })
    return results


def report(title, rows, path):
    with open(path, "w", encoding="utf-8") as f:
        f.write(f"# {title}\n")
        f.write("# owner\tsite_rva\torigin\tbase\tsrc_kind\tinsn\n")
        for r in rows:
            if "error" in r:
                f.write(f"ERROR\t{r['error']}\n")
                continue
            f.write(f"{r['owner']}\t{r['site_rva']}\t{r['origin']}\t{r['base']}\t{r['src_kind']}\t{r['insn']}\n")
    print(f"wrote {path} ({len(rows)} rows)")


# (g) needAutoSave: GameController instance 0x28 byte stores
g28 = analyze(0x28, owner_prefix="GameController$$")
report("GameController$$ byte-store [reg+0x28] (needAutoSave=instance 0x28 / mainThreadWaitForLock=statics 0x28)",
       g28, OUT / "gc-stores-0x28.tsv")

# false-positive sweep: ALL methods, byte-store [reg+0x28] with statics origin or non-GameController owners
all28 = analyze(0x28, all_owners=True)
gc_owners = [r for r in all28 if r.get("owner", "").startswith("GameController$$")]
non_gc = [r for r in all28 if not r.get("owner", "").startswith("GameController$$") and "owner" in r]
stat = [r for r in all28 if r.get("origin") == "statics"]
report("ALL-methods byte-store [reg+0x28] — non-GameController owners (offset-collision universe)",
       non_gc, OUT / "all-stores-0x28-nongc.tsv")
report("ALL-methods byte-store [reg+0x28] with statics-base origin (mainThreadWaitForLock-class writes)",
       stat, OUT / "all-stores-0x28-statics.tsv")
print(f"total 0x28 stores: all={len(all28)} gc={len(gc_owners)} nongc={len(non_gc)} statics-origin={len(stat)}")

# (k) plotHappen: PlotController instance 0x18 byte stores
p18 = analyze(0x18, owner_prefix="PlotController$$")
report("PlotController$$ byte-store [reg+0x18] (plotHappen=instance 0x18)",
       p18, OUT / "plot-stores-0x18.tsv")
all18 = analyze(0x18, all_owners=True)
non_p = [r for r in all18 if not r.get("owner", "").startswith("PlotController$$") and "owner" in r]
report("ALL-methods byte-store [reg+0x18] — non-PlotController owners (candidate cross-module writers)",
       non_p, OUT / "all-stores-0x18-nongc.tsv")
print(f"total 0x18 stores: all={len(all18)} plot={len(p18)} nongc={len(non_p)}")
