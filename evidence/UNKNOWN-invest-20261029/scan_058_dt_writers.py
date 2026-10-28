# scan_058_dt_writers.py — find methods with BOTH a float store [reg+0x58]
# AND a call to RealTime$$get_deltaTime (0xCFD4A0) — candidate second writers of
# WorkingUIController.leftPauseTime. Read-only on GameAssembly.dll.
# Output: stdout (list) + stores-0x58-dt.tsv in this folder.
import sys, capstone
sys.path.insert(0, r"C:\Users\memph\LongYinCoop\evidence\TIMEstatic-20261027-051647")
from time_static_common import GameImage

OUT_DT = 0xCFD4A0
GET_DELTA = 0x180000000 + OUT_DT

gi = GameImage()
md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
md.detail = True
import capstone.x86 as x86

regs64 = {"rax","rbx","rcx","rdx","rsi","rdi","rbp","rsp","r8","r9","r10","r11","r12","r13","r14","r15"}

def disasm_range(rva, end_rva):
    code = gi.read(rva, min(end_rva - rva, 0x80000))
    return list(md.disasm(code, gi.rva_to_va(rva)))

methods = gi.methods
n = len(methods)
hits = []
for idx, (start, name, _sig) in enumerate(methods):
    end = methods[idx + 1][0] if idx + 1 < n else start + 0x10000
    if end - start <= 0 or end - start > 0x80000:
        continue
    try:
        insns = disasm_range(start, end)
    except Exception:
        continue
    has_dt = False
    stores = []
    for insn in insns:
        if insn.mnemonic == "call" and len(insn.operands) == 1:
            op = insn.operands[0]
            if op.type == x86.X86_OP_IMM and (op.imm & 0xFFFFFFFFFFFF) == GET_DELTA:
                has_dt = True
        if insn.mnemonic in ("movss",) and len(insn.operands) == 2:
            dst, src = insn.operands
            if (dst.type == x86.X86_OP_MEM and dst.mem.base in (x86.X86_REG_RAX, x86.X86_REG_RBX,
                x86.X86_REG_RCX, x86.X86_REG_RDX, x86.X86_REG_RSI, x86.X86_REG_RDI, x86.X86_REG_RBP,
                x86.X86_REG_R8, x86.X86_REG_R9, x86.X86_REG_R10, x86.X86_REG_R11, x86.X86_REG_R12,
                x86.X86_REG_R13, x86.X86_REG_R14, x86.X86_REG_R15) and dst.mem.index == 0
                    and dst.mem.disp == 0x58 and src.type == x86.X86_OP_REG):
                stores.append((hex(insn.address - 0x180000000), insn.reg_name(dst.mem.base), insn.mnemonic, "%s %s, %s" % (insn.mnemonic, insn.op_str, "")))
    if has_dt and stores:
        hits.append((name, hex(start), stores))

print("methods with get_deltaTime AND float-store [reg+0x58]:", len(hits))
for name, start, stores in hits:
    print("  %s @ %s  stores=%s" % (name, start, stores))
with open(__file__.replace(".py", ".tsv"), "w", encoding="utf-8") as f:
    f.write("method\trva\tstores\n")
    for name, start, stores in hits:
        f.write("%s\t%s\t%s\n" % (name, start, "; ".join("%s[%s]" % (s[0], s[1]) for s in stores)))
