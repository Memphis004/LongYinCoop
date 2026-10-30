# disasm_time_bodies.py — linear disassembly of selected time-system method bodies.
# Read-only on GameAssembly.dll. Writes bodies/*.txt + floats-observed.tsv.
# Each body is bounded by the next ScriptMethod start (script.json) — boundaries are an
# approximation for the LAST method of a class only; intra-class methods are exact.
import csv
from pathlib import Path

import capstone

from time_static_common import GameImage, fmt_floats

OUT = Path(__file__).resolve().parent
(OUT / "bodies").mkdir(exist_ok=True)

BODIES = [
    # (out-name, script.json method name, extra cap if next-start missing)
    ("GameController_ChangeHour", "GameController$$ChangeHour"),
    ("GameController_ChangeDay_leaf", None),  # 0x790E30 resolved below
    ("GameController_ChangeDay_int", None),   # 0x7925F0
    ("GameController_Update", "GameController$$Update"),
    ("GameController_LateUpdate", "GameController$$LateUpdate"),
    ("GameController_cctor", "GameController$$.cctor"),
    ("RealTime_get_deltaTime", "RealTime$$get_deltaTime"),
    ("RealTime_get_time", "RealTime$$get_time"),
    ("WorkingUIController_Update", "WorkingUIController$$Update"),
    ("WorkingUIController_StartWorking_6p", None),   # 0x9CABF0
    ("WorkingUIController_StartWorking_7p", None),   # 0x9CB090
    ("WorkingUIController_StopWorking", "WorkingUIController$$StopWorking"),
    ("WorkingUIController_TryCancelWorking", "WorkingUIController$$TryCancelWorking"),
    ("WorkingUIController_SkipButtonClicked", "WorkingUIController$$SkipButtonClicked"),
    ("WorkingUIController_SetSkippingState", "WorkingUIController$$SetSkippingState"),
    ("WorkingUIController_Start", "WorkingUIController$$Start"),
    ("PlotController_MiddleStopWork", "PlotController$$MiddleStopWork"),
    ("PlotController_RealStartStudySkill", "PlotController$$RealStartStudySkill"),
    ("PlotController_StudyFightSelfFinish", "PlotController$$StudyFightSelfFinish"),
    ("PlotController_StudyMaxFightSkillFinish", "PlotController$$StudyMaxFightSkillFinish"),
    ("PlotController_StartWorking", "PlotController$$StartWorking"),
    ("StudySkillController_SureStartStudySkill", "StudySkillController$$SureStartStudySkill"),
    ("StudySkillController_StartStudySkill", "StudySkillController$$StartStudySkill"),
    ("StudySkillController_RealStartStudySkill", "StudySkillController$$RealStartStudySkill"),
    ("StudySkillController_FinishStudySkill", "StudySkillController$$FinishStudySkill"),
    ("StudySkillController_cctor", "StudySkillController$$.cctor"),
    ("BigMapController_FixedUpdate", "BigMapController$$FixedUpdate"),
    ("TimeScaleController_Update", "TimeScaleController$$Update"),
    ("WeatherController_Update", "WeatherController$$Update"),
    ("GlobalData_cctor", "GlobalData$$.cctor"),
    # (2026-10-31 ตามมติผู้ใช้ — เพิ่ม 1 แถวเพื่อถอด N5 supplement: toolchain RVA 0x9CF0A0
    #  = VA 0x1809CF0A0 (dump.cs) − ImageBase 0x180000000; resolve จาก script.json โดยชื่อ)
    ("WorldData_OnDeserializedMethod", "WorldData$$OnDeserializedMethod"),
]

FLOAT_TARGETS = {24.0, 0.2, 0.5, 2.0, 5.0}


def disasm_body(gi, start_rva, end_rva):
    code = gi.read(start_rva, end_rva - start_rva)
    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
    md.detail = True
    return list(md.disasm(code, gi.rva_to_va(start_rva)))


def body_end(gi, start_rva):
    # end = next ScriptMethod start AFTER start_rva (skip self); fallback start+0x8000
    import bisect

    i = bisect.bisect_right(gi._starts, start_rva)
    if i < len(gi._starts) and gi._starts[i] > start_rva:
        return gi._starts[i]
    return start_rva + 0x8000


def render(gi, name, start_rva, end_rva):
    insns = disasm_body(gi, start_rva, end_rva)
    lines = [f"; {name}  rva=0x{start_rva:X} va=0x{gi.rva_to_va(start_rva):X} end(before)=0x{end_rva:X} insns={len(insns)}"]
    floats = []
    for insn in insns:
        note = ""
        # resolve direct call/jmp
        if insn.mnemonic in ("call", "jmp") and len(insn.operands) == 1 and insn.operands[0].type == capstone.x86.X86_OP_IMM:
            tgt = gi.va_to_rva(insn.operands[0].imm)
            nm = gi.rva_name(tgt)
            if nm:
                note = f"  ; -> {nm} (0x{tgt:X})"
            else:
                owner = gi.owner(tgt)
                note = f"  ; -> 0x{tgt:X}" + (f" (inside {owner})" if owner else "")
        elif insn.mnemonic in ("call", "jmp") and any(
            op.type == capstone.x86.X86_OP_MEM and op.mem.base == capstone.x86.X86_REG_RIP for op in insn.operands
        ):
            for op in insn.operands:
                if op.type == capstone.x86.X86_OP_MEM and op.mem.base == capstone.x86.X86_REG_RIP:
                    slot = gi.va_to_rva(insn.address + insn.size + op.mem.disp)
                    nm = gi.meta_by_slot.get(slot)
                    note = f"  ; [rip->0x{slot:X}] {nm or '(slot unresolved)'}"
        # rip-relative data reads (constant pool / statics)
        if not insn.mnemonic.startswith("call") and insn.mnemonic != "jmp":
            for entry in gi.rip_floats([insn]):
                fvals = fmt_floats(entry)
                if fvals:
                    note = f"  ; [0x{entry['pool_rva']:X} {gi.section_of(entry['pool_rva'])}] {fvals}"
                    floats.append(entry)
        lines.append(f"{insn.address:#012x}  {insn.mnemonic:<8} {insn.op_str}{note}")
    (OUT / "bodies" / f"{name}.txt").write_text("\n".join(lines), encoding="utf-8")
    return insns, floats


def main():
    gi = GameImage()
    all_floats = []
    for out_name, meth in BODIES:
        if meth is not None:
            starts = gi.resolve(meth)
            if not starts:
                print(f"!! not found: {meth}")
                continue
            start = starts[0]
        elif out_name.endswith("leaf"):
            start = 0x790E30
        elif out_name.endswith("int"):
            start = 0x7925F0
        elif out_name.endswith("6p"):
            start = 0x9CABF0
        elif out_name.endswith("7p"):
            start = 0x9CB090
        else:
            continue
        end = body_end(gi, start)
        insns, floats = render(gi, out_name, start, end)
        for e in floats:
            e["body"] = out_name
        all_floats.extend(floats)
        print(f"{out_name}: 0x{start:X}..0x{end:X} ({len(insns)} insns, {len(floats)} pool reads)")
    with open(OUT / "floats-observed.tsv", "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f, delimiter="\t")
        w.writerow(["body", "insn_rva", "insn", "pool_rva", "section", "f32", "f64"])
        for e in all_floats:
            w.writerow([e["body"], f"0x{e['insn_rva']:X}", e["insn"], f"0x{e['pool_rva']:X}", gi.section_of(e["pool_rva"]), e.get("f32", ""), e.get("f64", "")])
    print(f"pool reads total: {len(all_floats)} -> floats-observed.tsv")


if __name__ == "__main__":
    main()
