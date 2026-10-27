# SP5body analyzer — single pass over dumpbin /DISASM output (read-only)
# Inputs : gameassembly.disasm.txt (471MB, this folder), Il2CppDumper/script.json + dump.cs (game folder)
# Outputs: target-bodies.disasm.txt, calls-xref.tsv, strings-in-bodies.tsv, target-index.tsv
import json, re, bisect, csv, time, os

BASE = 0x180000000
SRC_DISASM = "gameassembly.disasm.txt"
GAME = r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan"
t0 = time.time()
def log(m): print(f"[{time.time()-t0:7.1f}s] {m}", flush=True)

# ---- 1. script.json maps (Address = RVA decimal; name uses $$)
log("loading script.json")
d = json.load(open(os.path.join(GAME, r"Il2CppDumper\script.json"), encoding="utf-8"))
va2name = {BASE + int(m["Address"]): m["Name"] for m in d["ScriptMethod"]}
all_vas = sorted(va2name)
str_by_addr = {BASE + int(s["Address"]): s["Value"] for s in (d.get("ScriptString") or [])}
log(f"methods={len(va2name)} strings={len(str_by_addr)}")

# ---- 2. targets from dump.cs (decl line -> nearest RVA comment above)
targets = [  # (class, method, decl_line_1based)
    ("GameController","StartNewGame",376470),("GameController","StartNewGameSetting",376476),
    ("GameController","GenerateHero",376611),("GameController","GenerateHeroData_A",376620),
    ("GameController","GenerateHeroData_B",376623),("GameController","GenerateHeroData_C",376626),
    ("GameController","GenerateRelationShip",376629),("GameController","GenerateRandomEnemy",376980),
    ("GameController","WorldAddNewHero",376761),("GameController","HeroRealAutoRemove",376776),
    ("GameController","ManageHeroAutoQuitAndEnter",376779),("GameController","GetMaxHeroNum",376782),
    ("GameController","GetRecommendHeroNum",376785),("GameController","ManagePlayerRecruitHero",377163),
    ("GameController","ManageNewHeroJoinForce",377166),("GameController",".cctor",377172),
    ("GameController","<StartNewGame>b__7_0",377176),
    ("WorldData","GetRandomEnemyCount",367807),("WorldData","FindAvailableHeroID",367810),
    ("WorldData","FindAvailableTempHeroID",367813),("WorldData","AddTempHero",367816),
    ("WorldData","RemoveTempHero",367819),("WorldData","GetTempHeroCount",367822),
    ("WorldData","AddNewHero",367825),("WorldData","RemoveHero",367828),
    ("WorldData","GetHeroCount",367831),("WorldData","GetHeroForce",367840),
    ("WorldData","GetHero_l367843",367843),("WorldData","GetHero_l367849",367849),
    ("WorldData","GenerateHeroDict",367846),("WorldData",".ctor",367724),
    ("GameDataController","InitWithEtag",377313),("GameDataController","InitWithLocalFile",377316),
    ("GameDataController","LoadAllGameData",377418),("GameDataController","WriteGameDataCsv",377433),
    ("GameDataController",".ctor",377496),("GameDataController",".cctor",377499),
    ("GlobalData",".cctor",378675),
    ("ForceLevelData",".ctor",369302),("ForceLevelData","Clone",369305),
    ("StartGameSettingController","StartSettingPlayerData",390008),
    ("StartGameSettingController","BirthSettingPlayerData",390011),
]
rva_re = re.compile(r'// RVA: (0x[0-9A-Fa-f]+|-1) Offset: (0x[0-9A-Fa-f]+|-1) VA: (0x[0-9A-Fa-f]+)')
lines = open(os.path.join(GAME, r"Il2CppDumper\dump.cs"), encoding="utf-8", errors="replace").read().split("\n")
tinfo = []  # (label, va, rva, offset, decl_text)
for cls, meth, ln in targets:
    decl = lines[ln-1]
    rva = None
    for k in range(1, 5):
        m = rva_re.search(lines[ln-k]) if ln-k >= 0 else None
        if m: rva = m; break
    assert rva, f"no RVA near line {ln}"
    va = int(rva.group(3), 16)
    tinfo.append((f"{cls}::{meth}", va, rva.group(1), rva.group(2), decl.strip()[:120]))
tinfo.sort(key=lambda x: x[1])
tstart = {va: lab for lab, va, *_ in tinfo}
log(f"targets={len(tinfo)}")

with open("target-index.tsv","w",encoding="utf-8",newline="") as f:
    w = csv.writer(f, delimiter="\t")
    w.writerow(["label","VA","RVA","file_offset","decl"])
    for lab, va, rva, off, decl in tinfo:
        w.writerow([lab, hex(va), rva, off, decl])

# ---- 3. single pass
addr_re = re.compile(r'^\s*([0-9A-F]{16}):')
call_re = re.compile(r'\b(call|jmp)\s+([0-9A-F]{16})')
mov_rip_re = re.compile(r'\bmov\s+(r[a-z0-9]+),qword ptr \[rip\+([0-9A-F]+)h?\]')
body_f = open("target-bodies.disasm.txt","w",encoding="utf-8")
xref_f = open("calls-xref.tsv","w",encoding="utf-8",newline="")
xw = csv.writer(xref_f, delimiter="\t"); xw.writerow(["kind","caller","callee","site_va","note"])
str_f = open("strings-in-bodies.tsv","w",encoding="utf-8",newline="")
sw = csv.writer(str_f, delimiter="\t"); sw.writerow(["method","site_va","string"])

cur_lab = None; cur_end = 0; cur_name = None
prev_addr = 0
nlines = 0
log("pass start")
buf = []
def flush_body():
    global buf
    if buf: body_f.write("\n".join(buf) + "\n"); buf = []

with open(SRC_DISASM, encoding="utf-8", errors="replace") as f:
    for line in f:
        nlines += 1
        if len(line) < 21 or line[2] == ' ' and not line[2:3].isdigit():
            pass
        m = addr_re.match(line)
        if not m: continue
        va = int(m.group(1), 16)
        # resolve current global method
        idx = bisect.bisect_right(all_vas, va) - 1
        cur_name = va2name[all_vas[idx]] if idx >= 0 else "?"
        # target body tracking
        t = tstart.get(va)
        if t is not None:
            flush_body(); cur_lab = t
            j = bisect.bisect_right(all_vas, va)
            cur_end = all_vas[j] if j < len(all_vas) else (1 << 64)
            buf.append(f"\n===== {t}  VA={hex(va)} =====")
        if cur_lab and va < cur_end:
            buf.append(line.rstrip())
        cm = call_re.search(line)
        if cm:
            tgt = int(cm.group(2), 16)
            callee = va2name.get(tgt)
            if tgt in tstart:
                xw.writerow(["into", cur_name, tstart[tgt], hex(va), cm.group(1)])
            if cur_lab:
                xw.writerow(["out", cur_lab, callee or hex(tgt), hex(va), cm.group(1)])
            rm = mov_rip_re.search(line)
            if rm and "mov" == line.strip()[:3]:
                ln_bytes = line.split(":",1)[1]
                nb = len(ln_bytes.split(" ", 15))  # rough; refine below
                # compute instruction end via next addressed line later; use +7 heuristic only for 7-byte form
                if " 48 8B 15" in line or " 48 8B 0D" in line or " 4C 8B 05" in line or " 48 8B 05" in line:
                    rip_t = va + 7
                    sval = str_by_addr.get(rip_t)
                    if sval is not None:
                        sw.writerow([cur_lab or cur_name, hex(va), sval.replace("\n","\\n")[:160]])
        else:
            if cur_lab and va >= cur_end:
                flush_body(); cur_lab = None
log(f"pass done lines={nlines}")
flush_body()
body_f.close(); xref_f.close(); str_f.close()
log("outputs written")
