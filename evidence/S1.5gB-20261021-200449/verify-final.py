#!/usr/bin/env python3
"""S1.5g final save verification (independent of the sha256sum text files).

Re-hashes every file under <game>_Data/Save and compares against:
  - coop-test/<label>/savehash.real.before.txt   (pre-test real save)
  - coop-test/S1.5f-20261021-0247/savehash.real.before.txt (canonical 2b0548fe464b)
Uses the same hash/digest helpers as tools/save-split.py so digests are comparable.
Read-only: opens files for reading only, never writes to the save folder.
"""
import importlib.util
from pathlib import Path

REPO = Path(r"C:\Users\memph\LongYinCoop")
DATA = Path(r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan\LongYinLiZhiZhuan_Data")
LABEL = "S1.5gB-20261021-200449"

spec = importlib.util.spec_from_file_location("save_split", REPO / "tools" / "save-split.py")
ss = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ss)

base = REPO / "coop-test" / LABEL
cur = ss.hash_tree(DATA / "Save")
real_before = ss.read_hashfile(base / "savehash.real.before.txt")
copy_before = ss.read_hashfile(base / "savehash.copy.before.txt")
canonical = ss.read_hashfile(REPO / "coop-test" / "S1.5g-20261021-160331" / "savehash.real.before.txt")


def show(name, hashes):
    print("%-28s files=%d digest=%s" % (name, len(hashes), ss.digest_of(hashes)))


def diff(a, b):
    c, m, x = ss.diff_hashes(a, b)
    print("  changed=%d missing=%d added=%d" % (len(c), len(m), len(x)))
    for k in c:
        print("    changed: %s" % k)


print("== rehash of current Save ==")
show("current Save", cur)
show("S1.5gB real.before", real_before)
show("S1.5gB copy.before", copy_before)
show("previous baseline (S1.5g)", canonical)
print()
print("== current vs S1.5gB real.before ==")
diff(real_before, cur)
print("== current vs canonical S1.5f ==")
diff(canonical, cur)
print("== S1.5gB real.before vs copy.before (split integrity) ==")
diff(real_before, copy_before)

print()
print("VERDICT:",
      "57/57 EXACT" if cur == real_before else "MISMATCH",
      "| canonical-equal-except-pref:",
      sorted(k for k in canonical if canonical[k] != cur.get(k)))
