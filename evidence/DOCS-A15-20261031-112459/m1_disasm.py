# m1_disasm.py — M1 events (งาน 4 รอบ 6 · R6-7 · read-only)
# สกัด body ของสาย event แผนที่ (สร้าง/ตั้ง leftTime/หมดอายุ/timeout handler)
# ใช้ GameImage ร่วมกับ session SP5body — เปิด GameAssembly.dll + script.json READ-ONLY เท่านั้น
# ระบุ address ตรง (จาก script.json 2026-10-31) เพื่อเลี่ยงปัญหา overload
import bisect
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys_path = r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan\LongYinCoop\evidence\SP5body-TempHerosDict-20261031-034658"
import sys  # noqa: E402
sys.path.insert(0, sys_path)

import capstone  # noqa: E402
from time_static_common import GameImage  # noqa: E402

(HERE / "m1-bodies").mkdir(exist_ok=True)

# (out-name, script.json method name, address หรือ None=ใช้ชื่อ)
TARGETS = [
    ("GC_GenerateRandomEvent", "GameController$$GenerateRandomEvent", 0x7B18A0),
    ("WE_ManageWorldEvent", "WorldEventController$$ManageWorldEvent", 0x9D27B0),
    ("WE_CreateWorldEvent_a", "WorldEventController$$CreateWorldEvent", 0x9D17D0),
    ("WE_CreateWorldEvent_b", "WorldEventController$$CreateWorldEvent", 0x9D1EF0),
    ("WE_CreateWorldEvent_c", "WorldEventController$$CreateWorldEvent", 0x9D2100),
    ("WE_CreateRandomWorldEvent", "WorldEventController$$CreateRandomWorldEvent", 0x9D1550),
    ("GC_ManageWorldEventOutTime", "GameController$$ManageWorldEventOutTime", 0x7D4E90),
    ("GC_RemoveBigMapRandomEvent_1p", "GameController$$RemoveBigMapRandomEvent", 0x7DCE80),
    ("GC_RemoveBigMapRandomEvent_2p", "GameController$$RemoveBigMapRandomEvent", 0x7DCDF0),
    ("GC_RemoveAreaMapRandomEvent_1p", "GameController$$RemoveAreaMapRandomEvent", 0x7DCCC0),
    ("GC_RemoveAreaMapRandomEvent_2p", "GameController$$RemoveAreaMapRandomEvent", 0x7DCCD0),
    ("GC_RemoveEvent", "GameController$$RemoveEvent", 0x7DCF00),
    ("GC_ForceAttackPlayerAreaTimeOut", "GameController$$ForceAttackPlayerAreaTimeOut", 0x7A1350),
    ("GC_FightMatchOutTime", "GameController$$FightMatchOutTime", 0x7A06E0),
    ("GC_ForceFightMatchOutTime", "GameController$$ForceFightMatchOutTime", 0x7A1470),
    ("GC_HorseMatchOutTime", "GameController$$HorseMatchOutTime", 0x7C15D0),
    ("GC_AuctionOutTime", "GameController$$AuctionOutTime", 0x78D380),
]


def body_end(gi, start_rva):
    i = bisect.bisect_right(gi._starts, start_rva)
    if i < len(gi._starts) and gi._starts[i] > start_rva:
        return gi._starts[i]
    return start_rva + 0x8000


def render(gi, name, start_rva, end_rva):
    code = gi.read(start_rva, end_rva - start_rva)
    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
    md.detail = True
    insns = list(md.disasm(code, gi.rva_to_va(start_rva)))
    lines = [f"; {name}  rva=0x{start_rva:X} va=0x{gi.rva_to_va(start_rva):X} end(before)=0x{end_rva:X} insns={len(insns)}"]
    calls = []
    consts = []
    for insn in insns:
        note = ""
        if insn.mnemonic in ("call", "jmp") and len(insn.operands) == 1 and insn.operands[0].type == capstone.x86.X86_OP_IMM:
            tgt = gi.va_to_rva(insn.operands[0].imm)
            nm = gi.rva_name(tgt)
            if nm:
                note = f"  ; -> {nm} (0x{tgt:X})"
                calls.append(nm)
            else:
                owner = gi.owner(tgt)
                note = f"  ; -> 0x{tgt:X}" + (f" (inside {owner})" if owner else "")
        elif insn.mnemonic in ("call", "jmp"):
            for op in insn.operands:
                if op.type == capstone.x86.X86_OP_MEM and op.mem.base == capstone.x86.X86_REG_RIP:
                    slot = gi.va_to_rva(insn.address + insn.size + op.mem.disp)
                    nm = gi.meta_by_slot.get(slot)
                    if nm:
                        note = f"  ; [rip->0x{slot:X}] {nm}"
                        calls.append(nm)
                    else:
                        note = f"  ; [rip->0x{slot:X}] (slot unresolved)"
        # เก็บ immediate ขนาดกลาง (ค่า leftTime/repeatDay ที่ hard-code จะโผล่ที่นี่)
        if insn.mnemonic in ("mov", "cmp") and len(insn.operands) == 2:
            ops = insn.operands
            if ops[0].type == capstone.x86.X86_OP_MEM and ops[1].type == capstone.x86.X86_OP_IMM:
                if 0 <= ops[1].imm <= 4000:
                    consts.append((insn.address, insn.mnemonic, insn.op_str, ops[1].imm))
        lines.append(f"{insn.address:#012x}  {insn.mnemonic:<8} {insn.op_str}{note}")
    (HERE / "m1-bodies" / f"{name}.txt").write_text("\n".join(lines), encoding="utf-8")
    return insns, calls, consts


def main():
    gi = GameImage()
    for out_name, meth, addr in TARGETS:
        if addr is None:
            starts = gi.resolve(meth)
            if not starts:
                print(f"!! not found: {meth}")
                continue
            start = starts[0]
        else:
            start = addr
            nm = gi.rva_name(start)
            if nm:
                print(f"  (address check: 0x{start:X} = {nm} — expect {meth})")
        end = body_end(gi, start)
        insns, calls, consts = render(gi, out_name, start, end)
        print(f"{out_name}: 0x{start:X}..0x{end:X} ({len(insns)} insns, {len(calls)} resolved calls, {len(consts)} mem-immediates)")

    print("\n==== โครงสร้างสำคัญ ====")
    print("EventData.leftTime = 0x68 (int) · EventData.eventOutTimeCallFuc = 0x90 [DUMP]")
    print("WorldEventDataBase: startTime 0x20 · repeatType 0x30 · repeatDay 0x34 · repeatDayRandomRange 0x38 · lastTime 0x3C [DUMP]")


if __name__ == "__main__":
    main()
