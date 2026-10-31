# a15_disasm.py — A1.5 disasm 5 รายการ (งาน 3 รอบ 6 · R6-6 · read-only)
# ใช้ toolchain เดิมของ session SP5body (evidence/SP5body-TempHerosDict-20261031-034658/time_static_common.py)
# — GameImage เปิด GameAssembly.dll + script.json แบบ READ-ONLY เท่านั้น; เขียนเฉพาะ bodies/ ใน evidence folder นี้
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan\LongYinCoop\evidence\SP5body-TempHerosDict-20261031-034658")

import capstone  # noqa: E402
from time_static_common import GameImage  # noqa: E402

(HERE / "bodies").mkdir(exist_ok=True)

# (out-name, script.json method name)
BODIES = [
    # A1.5-1 — คอขวด ช-A: per-day handler ของงานอาคาร/ช่วยงาน (PlotController)
    ("PlotController_WorkInProductionBuilding", "PlotController$$WorkInProductionBuilding"),
    ("PlotController_FinishWorkInProductionBuilding", "PlotController$$FinishWorkInProductionBuilding"),
    ("PlotController_WorkInAreaBuilding", "PlotController$$WorkInAreaBuilding"),
    ("PlotController_FinishWorkInAreaBuilding", "PlotController$$FinishWorkInAreaBuilding"),
    ("PlotController_FinishHelpWork", "PlotController$$FinishHelpWork"),
    ("PlotController_HelpPatrol", "PlotController$$HelpPatrol"),
    ("PlotController_FinishHelpPatrol", "PlotController$$FinishHelpPatrol"),
    # A1.5-3 — ต้นทุนหักที่ไหนตอนเริ่มงาน
    ("StudySkillController_SureStartStudySkill", "StudySkillController$$SureStartStudySkill"),
    ("BuildingButtonController_OnClick", "BuildingButtonController$$OnClick"),
    ("PlotController_StartWorking_string", "PlotController$$StartWorking"),
    # A1.5-4 — งานเลี้ยง: เดินเวลา/ปฏิทินด้วยอะไร (coroutine bodies)
    ("PartyController_HeroEnteringParty_MoveNext", "PartyController.<HeroEnteringParty>d__54$$MoveNext"),
    ("PartyController_PartyContinue_MoveNext", "PartyController.<PartyContinue>d__56$$MoveNext"),
    ("PartyController_FinishParty_MoveNext", "PartyController.<FinishParty>d__58$$MoveNext"),
    ("PartyController_EndParty_MoveNext", "PartyController.<EndParty>d__59$$MoveNext"),
    ("PartyController_PartyContinue_wrapper", "PartyController$$PartyContinue"),
    # A1.5-5 — จุด format ข้อความ "คาดว่าแต่ละวัน…" / ป้ายปุ่ม "N วัน"
    ("BuildingUIController_CreateBuildingButton", "BuildingUIController$$CreateBuildingButton"),
    ("AreaController_RefreshAreaBuildingChoiceInfo", "AreaController$$RefreshAreaBuildingChoiceInfo"),
    # รอบสอง (วิเคราะห์ต่อจากรอบแรก)
    ("BuildingUIController_GenerateBuildingButton", "BuildingUIController$$GenerateBuildingButton"),
    ("AreaController_FreshAreaBuildingButton", "AreaController$$FreshAreaBuildingButton"),
    ("PartyController_PartyStart", "PartyController$$PartyStart"),
    ("PartyController_PartyEnd", "PartyController$$PartyEnd"),
    ("WorkingUIController_AddResourceNum", "WorkingUIController$$AddResourceNum"),
    ("GameController_GetConditionDescribe", "GameController$$GetConditionDescribe"),
]

# ชื่อ call ที่สนใจ (สำหรับ histogram ต่อ body)
INTEREST = [
    "ChangeHour", "ChangeDay", "ChangeMonth", "ChangeYear",
    "AddResourceNum", "WorkingUIController$$StartWorking", "StopWorking",
    "String$$Format", "String$$Replace", "String$$Concat",
    "SendMessage", "RefreshAreaBuildingChoiceInfo", "CreateBuildingButton",
    "ResourceData", "Gold", "LeftTime", "leftWorkingDay", "WorldData",
    "NextDay", "DayNum", "SetText", "set_text", "GameController$$",
    "StartCoroutine", "WaitForSeconds", "PartyStart", "PartyEnd",
]


def body_end(gi, start_rva):
    import bisect
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
        lines.append(f"{insn.address:#012x}  {insn.mnemonic:<8} {insn.op_str}{note}")
    (HERE / "bodies" / f"{name}.txt").write_text("\n".join(lines), encoding="utf-8")
    return insns, calls


def main():
    gi = GameImage()
    summary = []
    for out_name, meth in BODIES:
        starts = gi.resolve(meth)
        if not starts:
            print(f"!! not found: {meth}")
            summary.append((out_name, meth, None, None, 0, []))
            continue
        start = starts[0]
        end = body_end(gi, start)
        insns, calls = render(gi, out_name, start, end)
        hist = {}
        for c in calls:
            for key in INTEREST:
                if key in c:
                    hist[c] = hist.get(c, 0) + 1
        summary.append((out_name, meth, start, end, len(insns), hist))
        print(f"{out_name}: 0x{start:X}..0x{end:X} ({len(insns)} insns, {len(calls)} resolved calls)")

    print("\n==== สนใจ: resolved calls ที่ตรง INTEREST (ต่อ body) ====")
    for out_name, meth, start, end, n, hist in summary:
        print(f"\n[{out_name}]")
        if start is None:
            print("  (not found)")
            continue
        if not hist:
            print("  (none)")
        for c, k in sorted(hist.items()):
            print(f"  x{k}: {c}")

    # A1.5-2: หาค่าชุดตัวเลือกใน ScriptString (string literal จาก metadata — ค่า data asset จะไม่อยู่ที่นี่)
    print("\n==== A1.5-2: ScriptString ที่มีลักษณะ callFucParam (';'-delimited มี callFuc ท้าย) ====")
    hits = []
    for addr, s in gi.strings_by_addr.items():
        if s and (";" in s) and any(k in s for k in ("StartWorking", "HelpPatrol", "FinishHelpWork", "WorkIn")):
            hits.append((addr, s))
    for addr, s in sorted(hits)[:40]:
        print(f"  0x{addr:X}: {s!r}")
    print(f"  total: {len(hits)}")


if __name__ == "__main__":
    main()
