# summarize_xref_counts.py — per-target E8/E9 counts + owner breakdown from xref-scan.tsv.
# Read-only; prints and writes xref-summary.txt. Generic-shared methods (one address shared by
# many instantiations, e.g. GetComponent<T>) are marked NOISE — their hits do not identify a caller.
import csv
import sys
from collections import Counter, defaultdict
from pathlib import Path

OUT = Path(__file__).resolve().parent
NOISE_TARGETS = {"UnityEngine.Component$$GetComponent<StudySkillStarController>"}


def main():
    rows = list(csv.DictReader(open(OUT / "xref-scan.tsv", encoding="utf-8"), delimiter="\t"))
    per_target = defaultdict(list)
    for r in rows:
        per_target[(r["target_rva"], r["target_name"])].append(r)

    lines = []
    add = lines.append
    add("xref summary (E8/E9 direct scan, GAME-ONLY targets; generic-shared NOISE excluded)")
    add(f"total raw hits: {len(rows)}")
    for key in sorted(per_target):
        rva, name = key
        if name in NOISE_TARGETS:
            add(f"\n== {name} == NOISE (generic sharing: one code address for all T) hits={len(per_target[key])}")
            continue
        hits = per_target[key]
        kinds = Counter(h["kind"] for h in hits)
        owners = Counter(h["owner"] for h in hits)
        add(f"\n== {name} @ {rva} == total={len(hits)} (E8={kinds['E8']} E9={kinds['E9']})")
        for owner, n in sorted(owners.items(), key=lambda kv: -kv[1]):
            sites = [h for h in hits if h["owner"] == owner]
            site_s = ",".join(h["site_rva"][2:] for h in sites)
            add(f"   {n}x  {owner}   sites=0x{site_s}")
    text = "\n".join(lines)
    print(text)
    (OUT / "xref-summary.txt").write_text(text, encoding="utf-8")


if __name__ == "__main__":
    main()
