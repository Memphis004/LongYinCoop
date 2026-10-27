# resolve_slots.py — resolve .data slots referenced in body disasm to names.
# Sources: script.json ScriptMetadata (class slots), ScriptMetadataMethod (method slots),
# ScriptString (string literal slots). Read-only; writes slots-resolved.tsv + slot-summary.txt.
import csv
import re
from collections import Counter
from pathlib import Path

from time_static_common import SCRIPT_JSON, GameImage

OUT = Path(__file__).resolve().parent
SLOT_RE = re.compile(r"\[(0x[0-9A-Fa-f]+) \.data\]")


def main():
    gi = GameImage()
    with open(SCRIPT_JSON, encoding="utf-8") as f:
        import json

        data = json.load(f)
    meta_class = {m["Address"]: m["Name"] for m in data.get("ScriptMetadata", [])}
    print(f"class slots: {len(meta_class)}, method slots: {len(gi.meta_by_slot)}, strings: {len(gi.strings_by_addr)}")

    found = {}
    for body in sorted((OUT / "bodies").glob("*.txt")):
        for m in SLOT_RE.finditer(body.read_text(encoding="utf-8")):
            found.setdefault(int(m.group(1), 16), set()).add(body.stem)

    rows = []
    for slot in sorted(found):
        kind = ""
        name = meta_class.get(slot, "")
        if name:
            kind = "class"
        if not name:
            name = gi.meta_by_slot.get(slot, "")
            if name:
                kind = "method"
        if not name:
            name = gi.strings_by_addr.get(slot, "")
            if name:
                kind = "string"
        bodies = ",".join(sorted(found[slot]))
        rows.append((f"0x{slot:X}", kind, name, bodies))
        print(f"{rows[-1][0]:>10}  {kind or '???':<7} {name[:80]:<80} {bodies}")

    with open(OUT / "slots-resolved.tsv", "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f, delimiter="\t")
        w.writerow(["slot", "kind", "name", "bodies"])
        w.writerows(rows)
    unk = [r for r in rows if r[1] == ""]
    print(f"\nresolved {len(rows) - len(unk)}/{len(rows)} slots; unresolved: {[r[0] for r in unk]}")


if __name__ == "__main__":
    main()
