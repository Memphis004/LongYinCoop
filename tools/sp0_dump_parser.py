#!/usr/bin/env python3
"""sp0_dump_parser.py — SP0 DUMP-only census parser for Il2CppDumper dump.cs

Read-only: reads `<game>/Il2CppDumper/dump.cs` as text. Never touches the game
process, never loads assemblies, never writes anything inside the game folder.

Layer A outputs (into --outdir):
  census-candidates.tsv   candidate types per SP0 search rules (ก)(ข)(ค)
  census-references.tsv   metadata references per candidate (fields/props/params)
  parser-integrity.json   self-checks: totals, unparsed lines, anchors

Search rules (SP0-full-plan-draft.md rev2.1 §7 exit 1):
  (ก) simple type name contains Controller / UI / Manager / Singleton
      (substring, case-sensitive, on last dotted segment)
  (ข) type has a static field whose declared base type is a game class
      (strict: declared type IS the game class; generic-arg-only static fields are
      reported in a separate column and do NOT qualify)
  (ค) type has a static accessor: static method `get_Instance` or static property `Instance`

Metadata reference scope (exit 2): fields/properties/parameters declared inside
game-image types (TypeDefIndex >= first game image index). exact = declared base
type is the candidate; generic = candidate appears only as a generic argument.
Return types are informational (column ret_exact), not part of the rule count.
Actual call-sites are NOT derivable from dump.cs (no method bodies) — by
construction this parser reports none; census marks them UNAVAILABLE IN DUMP-ONLY.

Usage: python sp0_dump_parser.py --dump "<game>/Il2CppDumper/dump.cs" --outdir <dir>
"""
import argparse
import json
import re
from collections import defaultdict
from pathlib import Path

GAME_IMAGES = {"Assembly-CSharp.dll", "Assembly-CSharp-firstpass.dll"}
NAME_KEYWORDS = ("Controller", "UI", "Manager", "Singleton")

TYPE_DECL_RE = re.compile(
    r"^(?P<prefix>[\w\s]+?)\s*(?P<kind>class|struct|enum|interface)\s+(?P<rest>.+?)\s*"
    r"//\s*TypeDefIndex:\s*(?P<idx>\d+)\s*$"
)
NAMESPACE_RE = re.compile(r"^// Namespace: (?P<ns>.*)$")
MODIFIERS = (
    "public|private|protected|internal|static|readonly|const|volatile|new|extern|"
    "sealed|abstract|virtual|override|partial|ref|unsafe|fixed|required"
)
FIELD_RE = re.compile(
    rf"^\t(?P<mods>(?:(?:{MODIFIERS})\s+)*)(?P<type>.+?)\s+(?P<name>[^\s;]+)\s*"
    r"(?:/\*.*\*/)?\s*;\s*//\s*0x[0-9A-Fa-f]+\s*$"
)
PROP_RE = re.compile(
    rf"^\t(?P<mods>(?:(?:{MODIFIERS})\s+)*)(?P<type>.+?)\s+(?P<name>[^\s{{]+)\s*"
    r"(?P<acc>\{\s*(?:get|set).*)\}\s*$"
)
CONST_FIELD_RE = re.compile(
    rf"^\t(?P<mods>(?:(?:{MODIFIERS})\s+)*)(?P<type>.+?)\s+(?P<name>[A-Za-z_][\w.<>]*)\s*"
    r"=\s*.*;\s*$"
)
METHOD_RE = re.compile(
    rf"^\t(?P<mods>(?:(?:{MODIFIERS})\s+)*)(?:(?P<ret>.+?)\s+)?(?P<name>[^\s()]+)\s*"
    r"\((?P<params>[^)]*)\)\s*(?:\{ \}|;)?\s*$"
)


def base_type_name(type_text: str) -> str:
    """`Dictionary<string, int>`->`Dictionary`; `ref Foo[]`->`Foo`; `rail.Bar`->`Bar`."""
    t = type_text.strip()
    while True:
        m = re.match(r"^(ref|out|in|params|readonly)\s+", t)
        if not m:
            break
        t = t[m.end():]
    t = t.split("<", 1)[0]
    t = re.sub(r"[\?\[\]\* ]+$", "", t)
    t = t.strip()
    return t.split(".")[-1] if "." in t else t


def generic_arg_names(type_text: str):
    """Identifiers inside <...> of a type text (generic arguments, one level)."""
    out = []
    for m in re.finditer(r"<\s*(.*?)\s*>", type_text):
        for tok in m.group(1).split(","):
            tok = tok.strip()
            if tok and re.match(r"^[A-Za-z_][\w.\[\]<>]*$", tok):
                out.append(tok.split("<")[0].split(".")[-1])
    return out


def split_params(params_text: str):
    parts, depth, cur = [], 0, ""
    for ch in params_text:
        if ch in "<([":
            depth += 1
        elif ch in ">)]":
            depth -= 1
        if ch == "," and depth == 0:
            parts.append(cur)
            cur = ""
        else:
            cur += ch
    if cur.strip():
        parts.append(cur)
    return parts


def parse_param(param_text: str):
    p = param_text.strip()
    if not p:
        return None
    p = re.sub(r"\s*=\s*.*$", "", p)
    m = re.match(r"^(?:(?:ref|out|in|params|readonly)\s+)*(.+?)\s+([A-Za-z_]\w*)$", p)
    if m:
        return (base_type_name(m.group(1)), generic_arg_names(m.group(1)))
    return (base_type_name(p), generic_arg_names(p))


def parse_dump(dump_path: Path):
    lines = dump_path.read_text(encoding="utf-8", errors="replace").splitlines()
    images, types, unparsed = [], [], []
    ns, cur = "", None
    in_comment = False
    for ln, line in enumerate(lines, 1):
        s = line.strip()
        if in_comment:
            if "*/" in s:
                in_comment = False
            continue
        if s.startswith("/*"):
            if "*/" not in s:
                in_comment = True
            continue
        if s.startswith("// Image ") and " - " in s:
            body = s[len("// Image "):]
            body = body.split(": ", 1)[1] if ": " in body else body
            name, start = body.rsplit(" - ", 1)
            images.append((name.strip(), int(start)))
            continue
        m = NAMESPACE_RE.match(line)
        if m:
            ns = m.group(1)
            continue
        if line.startswith("//") or not s:
            continue
        if not line.startswith(("\t", "}")):
            if line in ("{", "{}") or line.startswith("["):
                continue  # opening brace at col 0 / col-0 attribute line
            td = TYPE_DECL_RE.match(line)
            if td:
                rest = td.group("rest")
                if " : " in rest:
                    tname, base = rest.split(" : ", 1)
                else:
                    tname, base = rest, ""
                cur = {
                    "decl_line": ln, "kind": td.group("kind"),
                    "name": tname.strip(), "simple": tname.strip().split(".")[-1],
                    "base": base.strip(), "typedef_index": int(td.group("idx")),
                    "namespace": ns,
                    # fields: (static, base_type, generic_args, line)
                    "fields": [],
                    # props: (static, name, base_type, generic_args, line)
                    "props": [],
                    # methods: (static, name, ret_base, [(pbase, pgen)], line)
                    "methods": [],
                }
                types.append(cur)
                continue
            unparsed.append((ln, line))
            continue
        if line.startswith("}"):
            cur = None
            continue
        if cur is None:
            continue
        if s.startswith(("//", "[", "|", "/*", "*/")):
            continue
        fm = FIELD_RE.match(line)
        if fm:
            mods = fm.group("mods")
            cur["fields"].append((
                "static" in mods.split(),
                base_type_name(fm.group("type")),
                generic_arg_names(fm.group("type")),
                ln,
            ))
            continue
        cm = CONST_FIELD_RE.match(line)
        if cm:
            mods = cm.group("mods")
            cur["fields"].append((
                True,  # const implies static
                base_type_name(cm.group("type")),
                generic_arg_names(cm.group("type")),
                ln,
            ))
            continue
        paren = line.find("(")
        brace = line.find("{")
        if paren != -1 and (brace == -1 or paren < brace):
            mm = METHOD_RE.match(line)
            if mm:
                mods = mm.group("mods")
                params = [parse_param(p) for p in split_params(mm.group("params"))]
                params = [p for p in params if p]
                cur["methods"].append((
                    "static" in mods.split(),
                    mm.group("name"),
                    base_type_name(mm.group("ret")) if mm.group("ret") else "",
                    params,
                    ln,
                ))
                continue
        elif brace != -1:
            pm = PROP_RE.match(line)
            if pm:
                mods = pm.group("mods")
                cur["props"].append((
                    "static" in mods.split(),
                    pm.group("name"),
                    base_type_name(pm.group("type")),
                    generic_arg_names(pm.group("type")),
                    ln,
                ))
                continue
        unparsed.append((ln, line))
    return images, types, unparsed


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dump", required=True)
    ap.add_argument("--outdir", required=True)
    args = ap.parse_args()
    dump_path, outdir = Path(args.dump), Path(args.outdir)
    outdir.mkdir(parents=True, exist_ok=True)

    images, types, unparsed = parse_dump(dump_path)

    game_start = min(start for name, start in images if name in GAME_IMAGES)
    game_types = [t for t in types if t["typedef_index"] >= game_start]
    game_class_simples = defaultdict(list)
    for t in game_types:
        if t["kind"] == "class":
            game_class_simples[t["simple"]].append(t["name"])
    ambiguous = {s for s, v in game_class_simples.items() if len(v) > 1}
    gcs = set(game_class_simples)

    # ---- metadata references across ALL game types (scope: game images only) ----
    ref = defaultdict(lambda: {
        "field_exact": 0, "field_generic": 0, "prop_exact": 0, "prop_generic": 0,
        "param_exact": 0, "param_generic": 0, "ret_exact": 0,
        "by": defaultdict(int),
    })

    def add(kind, name, generic):
        """Count one reference; returns the referenced game-class simple name or None."""
        name = base_type_name(name)
        if name in gcs:
            ref[name][kind + "_exact"] += 1
            return name
        hit = None
        for a in generic:
            if a in gcs:
                ref[a][kind + "_generic"] += 1
                hit = a
        return hit

    for t in game_types:
        for (is_static, b, g, ln) in t["fields"]:
            hit = add("field", b, g)
            if hit:
                ref[hit]["by"][t["name"]] += 1
        for (is_static, nm, b, g, ln) in t["props"]:
            hit = add("prop", b, g)
            if hit:
                ref[hit]["by"][t["name"]] += 1
        for (is_static, nm, rb, params, ln) in t["methods"]:
            if rb and rb in gcs:
                ref[rb]["ret_exact"] += 1
                ref[rb]["by"][t["name"]] += 1
            for (pb, pg) in params:
                hit = add("param", pb, pg)
                if hit:
                    ref[hit]["by"][t["name"]] += 1

    # ---- candidate rules ----
    cand_rows = []
    for t in game_types:
        static_game_fields = [
            f"L{ln}:{b}" for (is_static, b, g, ln) in t["fields"] if is_static and b in gcs
        ]
        static_generic_only = [
            f"L{ln}:{b}<{','.join(g)}>"
            for (is_static, b, g, ln) in t["fields"]
            if is_static and b not in gcs and any(a in gcs for a in g)
        ]
        get_inst = [ln for (is_static, nm, rb, p, ln) in t["methods"]
                    if is_static and nm == "get_Instance"]
        inst_props = [ln for (is_static, nm, b, g, ln) in t["props"]
                      if is_static and nm == "Instance"]
        rules = []
        if any(k in t["simple"] for k in NAME_KEYWORDS):
            rules.append("ก")
        if static_game_fields:
            rules.append("ข")
        if get_inst or inst_props:
            rules.append("ค")
        if rules:
            cand_rows.append({
                "t": t, "rules": ",".join(rules),
                "static_game_fields": static_game_fields,
                "static_generic_only": static_generic_only,
                "get_instance": get_inst, "instance_props": inst_props,
            })

    # ---- write TSVs ----
    with open(outdir / "census-candidates.tsv", "w", encoding="utf-8") as f:
        f.write("full_name\tkind\tnamespace\tbase_types\ttypedef_index\timage\t"
                "rules\tn_methods\tn_props\tn_fields\tn_static_fields\t"
                "static_fields_of_game_class\tstatic_fields_generic_only_not_counted\t"
                "get_Instance_lines\tInstance_prop_lines\tambiguous_simple_name\n")
        for r in cand_rows:
            t = r["t"]
            f.write("\t".join([
                t["name"], t["kind"], t["namespace"], t["base"] or "-",
                str(t["typedef_index"]), image_of(images, t["typedef_index"]),
                r["rules"], str(len(t["methods"])), str(len(t["props"])),
                str(len(t["fields"])),
                str(sum(1 for x in t["fields"] if x[0])),
                ";".join(r["static_game_fields"]) or "-",
                ";".join(r["static_generic_only"]) or "-",
                ";".join(f"L{x}" for x in r["get_instance"]) or "-",
                ";".join(f"L{x}" for x in r["instance_props"]) or "-",
                "yes" if t["simple"] in ambiguous else "no",
            ]) + "\n")

    with open(outdir / "census-references.tsv", "w", encoding="utf-8") as f:
        f.write("full_name\tsimple\ttypedef_index\tfield_exact\tfield_generic\t"
                "prop_exact\tprop_generic\tparam_exact\tparam_generic\t"
                "ret_exact_info_only\tself_static_fields_typed_self\t"
                "self_get_Instance\tself_Instance_prop\ttop_referencers\n")
        for r in cand_rows:
            t = r["t"]
            rr = ref.get(t["simple"])
            if rr is None:
                continue
            self_static = sum(1 for (is_static, b, g, ln) in t["fields"]
                              if is_static and b == t["simple"])
            top = sorted(rr["by"].items(), key=lambda kv: -kv[1])[:8]
            f.write("\t".join([
                t["name"], t["simple"], str(t["typedef_index"]),
                str(rr["field_exact"]), str(rr["field_generic"]),
                str(rr["prop_exact"]), str(rr["prop_generic"]),
                str(rr["param_exact"]), str(rr["param_generic"]),
                str(rr["ret_exact"]), str(self_static),
                ";".join(f"L{x}" for x in r["get_instance"]) or "-",
                ";".join(f"L{x}" for x in r["instance_props"]) or "-",
                ";".join(f"{k}({v})" for k, v in top) or "-",
            ]) + "\n")

    # ---- integrity ----
    idxs = [t["typedef_index"] for t in types]
    by_simple = {t["simple"]: t for t in game_types if t["namespace"] == ""}
    anchors = {"GameController": 244, "BattleController": 186, "PlotController": 1530}
    print(f"types parsed               : {len(types)}")
    print(f"game start index           : {game_start}")
    print(f"game types / classes       : {len(game_types)} / {sum(1 for t in game_types if t['kind']=='class')}")
    print(f"typedef order monotonic    : {idxs == sorted(idxs)}")
    print(f"unparsed member lines      : {len(unparsed)}")
    for ln, line in unparsed[:10]:
        print(f"  UNPARSED L{ln}: {line!r}")
    print(f"candidates ((ก)∪(ข)∪(ค))   : {len(cand_rows)}")
    print(f"candidates by ก only etc   : " + str({
        k: sum(1 for r in cand_rows if r["rules"] == k)
        for k in sorted({r["rules"] for r in cand_rows})
    }))
    print(f"ambiguous simple names     : {len(ambiguous)}")
    for name, expect in anchors.items():
        t = by_simple.get(name)
        got = len(t["methods"]) if t else None
        print(f"anchor {name:16s}: methods={got} (prior DUMP notes {expect})")
    (outdir / "parser-integrity.json").write_text(json.dumps({
        "dump": str(dump_path),
        "types_parsed": len(types),
        "game_images": {n: s for n, s in images if n in GAME_IMAGES},
        "game_start_index": game_start,
        "game_types": len(game_types),
        "game_classes": sum(1 for t in game_types if t["kind"] == "class"),
        "typedef_monotonic": idxs == sorted(idxs),
        "unparsed_count": len(unparsed),
        "unparsed_lines": [{"line": a, "text": b} for a, b in unparsed[:50]],
        "ambiguous_simple_names": sorted(ambiguous),
        "anchor_check": {n: {"got": len(by_simple[n]["methods"]) if n in by_simple else None,
                             "prior_notes": e} for n, e in anchors.items()},
    }, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"outputs -> {outdir}")


def image_of(images, idx):
    owner = None
    for name, start in images:
        if idx >= start:
            owner = name
        else:
            break
    return owner or "?"


if __name__ == "__main__":
    main()
