#!/usr/bin/env python3
"""pref_key_check — ตรวจ "ค่า key" ใน MelonPreferences.cfg ไม่ใช่ hash (บทเรียน RUNBOOK-G3)

ทำไมต้องมีเครื่องมือนี้: ML เขียนไฟล์ cfg ใหม่ตอนออกเกม ทำให้ sha256 เปลี่ยนเองได้ทั้งที่
ความหมายเดิม (GO-3 · มติ [G-3] · RUNBOOK-G3 บรรทัด 120 · RUNBOOK-V2A ขั้น 0.4/ขั้น 8.2)
เกณฑ์ผ่าน/ไม่ผ่านของ session-start/ปลาย session จึงเป็น "ค่า key" — ไม่ใช่ hash

อ่านอย่างเดียว 100% — ไม่มี write path ใด · fail-closed ทุกทาง:
  exit 0 = ผ่านครบ · exit 1 = มีข้อไม่ผ่าน (mismatch / key หาย / section หาย) ·
  exit 2 = ไฟล์ไม่มี / อ่านไม่ได้ / บรรทัดใน section เป้าหมายแยกไม่ได้ (ไม่เดา)

ค่าเริ่มต้น (ตรงกับ session-start/exit check ที่ใช้จริง ณ 2026-10-29):
  [LongYinCoop] Enabled = true · AllowRemoteInput = false · Sp5T0 = false ·
  TimeV2a = false (key หายได้ = PASS-with-note ตาม RUNBOOK-V2A ขั้น 0.4 "ไม่มี key
  TimeV2a หรือมีแล้วเป็น false")

เพิ่ม/ทับรายการตรวจด้วย --check "Section:Key=Expected" (ทำซ้ำได้) เช่นตอนเตรียม V2-A:
  --check "LongYinCoop:TimeV2a=true"
รูปแบบ Expected: true/false (case-insensitive), ตัวเลข, หรือข้อความ (ถ้าครอบ '...'/"..."
จะถูกปลด quote ทั้งสองฝั่งก่อนเทียบ)
"""
import argparse
import os
import re
import sys

DEFAULT_GAME_ROOT = r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan"
DEFAULT_CHECKS = [
    # (section, key, expected, optional) — optional=True: key หาย = PASS-with-note
    ("LongYinCoop", "Enabled", "true", False),
    ("LongYinCoop", "AllowRemoteInput", "false", False),
    ("LongYinCoop", "Sp5T0", "false", False),
    ("LongYinCoop", "TimeV2a", "false", True),
]

SECTION_RE = re.compile(r"^\s*\[\s*([^\]]+?)\s*\]\s*$")
ENTRY_RE = re.compile(r"^\s*(\"[^\"]+\"|'[^']+'|[A-Za-z0-9_.\- ]+?)\s*=\s*(.+?)\s*$")


def parse_cfg(text):
    """แยกไฟล์ cfg เป็น {section: {key: raw_value}} — บรรทัดที่แยกไม่ได้คืนใน errors (fail-closed)"""
    sections = {}
    errors = []
    current = None
    for lineno, raw in enumerate(text.splitlines(), 1):
        line = raw.strip()
        if not line or line.startswith("#") or line.startswith(";"):
            continue
        m = SECTION_RE.match(line)
        if m:
            current = m.group(1).strip()
            sections.setdefault(current, {})
            continue
        m = ENTRY_RE.match(line)
        if m and current is not None:
            key = m.group(1).strip()
            if len(key) >= 2 and key[0] in "\"'" and key[-1] == key[0]:
                key = key[1:-1]
            sections[current][key.strip()] = m.group(2).strip()
            continue
        if current is not None:
            errors.append((lineno, raw.strip()[:80]))
    return sections, errors


def canon(value):
    """รูปแบบมาตรฐานสำหรับเทียบ: boolean lowercase · ปลด quote ข้อความ · ตัดช่องว่างปลาย"""
    v = value.strip()
    if len(v) >= 2 and v[0] in "\"'" and v[-1] == v[0]:
        v = v[1:-1]
        return v
    if v.lower() in ("true", "false"):
        return v.lower()
    return v


def check_file(path, checks):
    """คืน (results, status) — status: 'PASS' | 'FAIL' | 'ERROR' (results = list ของ tuple รายข้อ)"""
    if not os.path.isfile(path):
        return ([("ERROR", "-", "-", f"file not found: {path}")], "ERROR")
    try:
        with open(path, "r", encoding="utf-8") as f:
            text = f.read()
    except OSError as e:
        return ([("ERROR", "-", "-", f"cannot read {path}: {e}")], "ERROR")

    sections, parse_errors = parse_cfg(text)
    results = []

    if parse_errors:
        for lineno, frag in parse_errors:
            results.append(("ERROR", "-", "-", f"unparsed line {lineno}: {frag}"))
        return (results, "ERROR")  # fail-closed — แยกไฟล์ไม่ครบ = ไม่ตัดสินอะไรเลย

    # ตรวจ section หายแบบแยกกรณี: ML เคยแปลง [LongYinCoop] เป็นตารางซ้อน [LongYinCoop.coop]
    # (incident 2026-10-22) — ต้องรายงานชัด ไม่ใช่แค่ "missing"
    wanted_sections = {s for s, _, _, _ in checks}
    for section in wanted_sections:
        if section in sections:
            continue
        nested = [n for n in sections if n == section or n.startswith(section + ".")]
        if nested:
            results.append(("FAIL", section, "-",
                            "section '%s' missing — found nested %s (ML rewrite?)" % (section, nested)))
        else:
            results.append(("FAIL", section, "-", "section '%s' missing" % section))

    for section, key, expected, optional in checks:
        if section not in sections:
            continue  # รายงานไปแล้วด้านบน
        entries = sections[section]
        if key not in entries:
            if optional:
                results.append(("NOTE", section, key, "key absent (allowed: optional)"))
            else:
                results.append(("FAIL", section, key, "key missing"))
            continue
        got = canon(entries[key])
        want = canon(expected)
        if got == want:
            results.append(("OK", section, key, "%s == %s" % (got, want)))
        else:
            results.append(("FAIL", section, key, "got %r want %r" % (entries[key], expected)))

    status = "FAIL" if any(r[0] == "FAIL" for r in results) else "PASS"
    return (results, status)


def parse_check_arg(arg):
    """'Section:Key=Expected' -> (section, key, expected, optional=False) — ผิดรูปแบบ = ValueError"""
    if ":" not in arg or "=" not in arg:
        raise ValueError("expected Section:Key=Expected, got %r" % arg)
    section, rest = arg.split(":", 1)
    key, expected = rest.split("=", 1)
    section, key = section.strip(), key.strip()
    if not section or not key or expected.strip() == "":
        raise ValueError("empty part in %r" % arg)
    return (section, key, expected.strip(), False)


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--file", default=os.path.join(
        os.environ.get("LONGYIN_GAME_DIR", DEFAULT_GAME_ROOT),
        "UserData", "MelonPreferences.cfg"),
        help="path ของ MelonPreferences.cfg (default: <เกม>\\UserData\\MelonPreferences.cfg "
             "— ทับด้วย env LONGYIN_GAME_DIR ได้ เช่นเดียวกับ LONGYIN_DATA_DIR ของ save-split.py)")
    ap.add_argument("--check", action="append", default=[], metavar="Section:Key=Expected",
                    help="เพิ่มรายการตรวจ (ทำซ้ำได้) — ระบุแล้วจะทับ default checks ทั้งชุด")
    args = ap.parse_args(argv)

    if args.check:
        try:
            checks = [parse_check_arg(c) for c in args.check]
        except ValueError as e:
            print("ARGS ERROR: %s" % e)
            return 2
    else:
        checks = list(DEFAULT_CHECKS)

    results, status = check_file(args.file, checks)
    for tag, section, key, detail in results:
        loc = "%s:%s" % (section, key) if key != "-" else section
        print("  %-5s %-28s %s" % (tag, loc, detail))
    n_ok = sum(1 for r in results if r[0] == "OK")
    n_note = sum(1 for r in results if r[0] == "NOTE")
    n_fail = sum(1 for r in results if r[0] == "FAIL")
    n_err = sum(1 for r in results if r[0] == "ERROR")
    print("PREF-KEY-CHECK %s (%d checks: %d OK, %d note, %d fail, %d error)"
          % (status, len(results), n_ok, n_note, n_fail, n_err))
    if status == "PASS":
        return 0
    if status == "FAIL":
        return 1
    return 2


if __name__ == "__main__":
    sys.exit(main())
