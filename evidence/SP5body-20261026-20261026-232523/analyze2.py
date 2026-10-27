# SP5body analyzer 2 — post-process target-bodies.disasm.txt (read-only)
# Out: constants-table.tsv, call-sequence.tsv, xref-summary.tsv, heroNum-candidates.tsv, strings-in-bodies.tsv
import re, json, csv, os, bisect

GAME = r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan"
BASE = 0x180000000
d = json.load(open(os.path.join(GAME, r"Il2CppDumper\script.json"), encoding="utf-8"))
va2name = {BASE + int(m["Address"]): m["Name"] for m in d["ScriptMethod"]}
all_vas = sorted(va2name)
str_by_addr = {BASE + int(s["Address"]): s["Value"] for s in (d.get("ScriptString") or [])}

# parse bodies
secs = {}
cur = None
for line in open("target-bodies.disasm.txt", encoding="utf-8"):
    m = re.match(r'^===== (.+?)  VA=(0x[0-9A-Fa-f]+) =====', line)
    if m:
        cur = m.group(1); secs[cur] = []
    elif cur and line.strip():
        secs[cur].append(line.rstrip())

addr_re = re.compile(r'^\s*([0-9A-F]{16}):')
call_re = re.compile(r'\b(call|jmp)\s+([0-9A-F]{16})')
imm_re = re.compile(r'\bmov\s+(dword|qword) ptr \[(r[a-z0-9]+|rsp)\+([0-9A-F]+)h?\],([0-9A-F]+)h?\b')
rip_re = re.compile(r'^\s*([0-9A-F]{16}):((?:\s+[0-9A-F]{2})+)\s{2,}mov\s+(r[a-z0-9]+),qword ptr \[rip\+([0-9A-F]+)h?\]')

# static offsets of interest from dump.cs comments
GD_OFFS = {
 0x90:"MaxFriendNum",0x94:"MaxLoverNum",0x98:"MaxBrotherNum",0x9C:"MaxHeroNum",0xA0:"RecommendHeroNum",
 0xA4:"MinOutforceSimpleHeroNum",0xEC:"PlayerForceID",0xF0:"HeroBaseHp",0xF4:"HeroBaseMp",
 0xF8:"HeroMaxAttriNum",0xFC:"HeroLimitAttriNum",0x100:"HeroMaxFightSkillNum",0x104:"HeroLimitFightSkillNum",
 0x108:"HeroMaxLivingSkillNum",0x118:"TempHeroIDStart",0x11C:"SummonSkillIDStart",0x164:"MaxInternalSkillSelfStudyLv",
 0x168:"MaxFightSkillSelfStudyLv",0x68:"PlayTestMaxLv",0x6C:"PlayTestMaxFavor",0xE8:"MaxLogNum",
 0x8C:"MaxAttackSkillNum",0xD4:"?",0x208:"BattleMapCol",0x20C:"BattleMapRow",0x210:"MaxAreaMapCol",0x214:"MaxAreaMapRow",
}
GC_OFFS = {0x0:"saveDataFileName?"}

rows_const = []
rows_seq = []
rows_str = []
for lab, body in secs.items():
    seq = []
    for i, line in enumerate(body):
        am = addr_re.match(line)
        cm = call_re.search(line)
        if cm:
            tgt = int(cm.group(2), 16)
            callee = va2name.get(tgt, hex(tgt))
            seq.append((line[:line.find(':')].strip() if am else "cont", cm.group(1), callee, hex(tgt)))
        im = imm_re.search(line)
        if im and lab.endswith("::.cctor"):
            rows_const.append((lab, im.group(2), int(im.group(3),16), int(im.group(4),16), line.strip()[:110]))
        rm = rip_re.match(line)
        if rm:
            va = int(rm.group(1), 16)
            nbytes = len(rm.group(2).split())
            rip_t = va + nbytes
            s = str_by_addr.get(rip_t)
            if s is not None:
                rows_str.append((lab, hex(va), s.replace("\n","\\n")[:200]))
    for site, kind, callee, tgt in seq:
        rows_seq.append((lab, site, kind, callee, tgt))

with open("constants-table.tsv","w",encoding="utf-8",newline="") as f:
    w = csv.writer(f, delimiter="\t"); w.writerow(["cctor","base_reg","field_off_dec","imm_dec","insn"])
    for r in rows_const: w.writerow(r)
with open("call-sequence.tsv","w",encoding="utf-8",newline="") as f:
    w = csv.writer(f, delimiter="\t"); w.writerow(["method","site","kind","callee","target"])
    for r in rows_seq: w.writerow(r)
with open("strings-in-bodies.tsv","w",encoding="utf-8",newline="") as f:
    w = csv.writer(f, delimiter="\t"); w.writerow(["method","site_va","string"])
    for r in rows_str: w.writerow(r)

# xref summary: into-targets aggregation
from collections import Counter, defaultdict
into = defaultdict(list)
for row in csv.reader(open("calls-xref.tsv", encoding="utf-8"), delimiter="\t"):
    if row and row[0] == "into":
        into[row[2]].append((row[1], row[3]))
with open("xref-summary.tsv","w",encoding="utf-8",newline="") as f:
    w = csv.writer(f, delimiter="\t"); w.writerow(["target","n_call_sites","callers"])
    for k in sorted(into):
        callers = Counter(c for c,_ in into[k])
        w.writerow([k, len(into[k]), "; ".join(f"{c}({n})" for c,n in callers.most_common(12))])

# heroNum candidates: in LoadAllGameData, mov edx,imm32 followed within 3 lines by call to List<int>::Add / Int32 adds
hl = secs.get("GameDataController::LoadAllGameData", [])
pat_add = re.compile(r'List`1<int32>\$\$Add|List`1<Int32>')
cands = []
for i, line in enumerate(hl):
    if pat_add.search(line) or "ForceLevelData" in line:
        cands.append((i, line.strip()[:140]))
with open("heroNum-candidates.txt","w",encoding="utf-8") as f:
    for i, l in cands:
        ctx = hl[max(0,i-4):i+2]
        f.write(f"--- line {i} ---\n" + "\n".join(x.strip()[:140] for x in ctx) + "\n")
print("const rows:", len(rows_const), "seq rows:", len(rows_seq), "strings:", len(rows_str))
print("targets parsed:", len(secs))
