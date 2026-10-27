# scan_time_xrefs.py — find E8/E9/FF15 call sites targeting the time-system methods.
# Read-only on GameAssembly.dll + script.json. Writes xref-scan.tsv next to this script.
# Method notes (same family as the previous rounds):
#   - E8 = call rel32, E9 = jmp rel32 (tail-call) — both counted; each candidate is validated
#     by decoding the 5 bytes as one call/jmp instruction (removes operand-byte false hits).
#   - FF15 = call qword ptr [rip+slot]; slot resolved via script.json ScriptMetadataMethod.
#   - owner = nearest preceding ScriptMethod start (script.json). Limitation: a site in padding
#     or data between methods gets attributed to the previous method — flagged ranges are kept raw.
import csv
import sys
from pathlib import Path

from time_static_common import GameImage

OUT = Path(__file__).resolve().parent

# target name patterns; each resolved from script.json (all matches scanned separately)
NAME_PATTERNS = [
    "GameController$$ChangeHour",
    "GameController$$ChangeDay",          # both overloads
    "GameController$$ChangeDayDirect",
    "GameController$$ChangeMonthDirect",
    "GameController$$ChangeYearDirect",
    "GameController$$ChangeMonth",
    "GameController$$ChangeYear",
    "WorkingUIController$$StartWorking",   # all overloads
    "WorkingUIController$$StopWorking",
    "WorkingUIController$$TryCancelWorking",
    "WorkingUIController$$Update",
    "WorkingUIController$$SkipButtonClicked",
    "WorkingUIController$$SetSkippingState",
    "WorkingUIController$$Start",
    "RealTime$$get_deltaTime",
    "GameController$$Update",
    "GameController$$FixedUpdate",
    "GameController$$LateUpdate",
]
EXTRA_SUBSTRINGS = [
    "StudySkill",          # SureStartStudySkill / RealStartStudySkill family
    "StudyMaxFightSkillFinish",
    "StudyFightSelfFinish",
    "MiddleStopWork",
]

def main():
    gi = GameImage()
    print(f"image base 0x{gi.image_base:x}  methods {len(gi.methods)}")

    targets = {}  # rva -> name
    for pat in NAME_PATTERNS:
        for rva, name, _sig in gi.methods:
            if name == pat or name.startswith(pat):
                targets.setdefault(rva, name)
    for sub in EXTRA_SUBSTRINGS:
        for rva, name, _sig in gi.methods:
            if sub in name:
                targets.setdefault(rva, name)

    print(f"resolved {len(targets)} target methods:")
    for rva, name in sorted(targets.items()):
        print(f"  0x{rva:06X}  {name}")

    hits = gi.scan_direct_xrefs(set(targets.keys()))
    print(f"\ndirect E8/E9 hits: {len(hits)}")
    with open(OUT / "xref-scan.tsv", "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f, delimiter="\t")
        w.writerow(["site_rva", "site_va", "kind", "target_rva", "target_name", "owner"])
        for h in hits:
            w.writerow([f"0x{h['site_rva']:X}", f"0x{h['site_va']:X}", h["kind"], f"0x{h['target_rva']:X}", targets[h["target_rva"]], h["owner"]])
    for h in hits:
        print(f"  0x{h['site_rva']:08X} {h['kind']} -> 0x{h['target_rva']:06X} {targets[h['target_rva']]}  owner={h['owner']}")

    # FF15 indirect: scan slots whose resolved name matches our targets
    slot_rvas = set()
    for slot_rva, name in gi.meta_by_slot.items():
        if name in targets.values() or any(sub in name for sub in EXTRA_SUBSTRINGS):
            slot_rvas.add(slot_rva)
    ff = gi.scan_ff15_xrefs(slot_rvas)
    print(f"\nFF15 indirect hits: {len(ff)} (slots matched: {len(slot_rvas)})")
    with open(OUT / "xref-ff15.tsv", "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f, delimiter="\t")
        w.writerow(["site_rva", "site_va", "slot_rva", "slot_name", "owner"])
        for h in ff:
            w.writerow([f"0x{h['site_rva']:X}", f"0x{h['site_va']:X}", f"0x{h['slot_rva']:X}", h["slot_name"], h["owner"]])
    for h in ff:
        print(f"  0x{h['site_rva']:08X} FF15 slot {h['slot_name']}  owner={h['owner']}")


if __name__ == "__main__":
    main()
