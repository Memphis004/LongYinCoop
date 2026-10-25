#!/usr/bin/env python3
"""S2a-4 (b) static check — หลักฐานว่า guest/read-path ไม่มีทางเรียกเมธอดเปลี่ยน-state

สแกน `src/LongYinCoop/S2/**/*.cs` (และกติกาข้อ 3: ทั้ง `src/`) 3 กติกา:

  1. write-path call sites (§13.2 ข้อ 5(b)):
     identifier ของเมธอด/คลาสเป้าหมาย 4 กลุ่มต้องเป็น **0** ใน S2/**
     (นับรวมที่ปรากฏใน comment — เข้มกว่านิยามเดิมเพื่อกัน false negative จากการย้ายโค้ด)
  2. Harmony references: ห้ามมี reference Harmony ใด ๆ ใน S2/** จนกว่า M5 จะอนุมัติ (W4(a))
  3. pure-logic isolation (W4(c)): `S2WriteGuardHook` ห้ามถูกอ้างนอกไฟล์นิยามตัวเอง
     (call site ได้เฉพาะ tests/ — กัน "ตรรกะ pure ถูกเรียกจากโค้ดทำงานจริง")

Exit code: 0 = สะอาด · 1 = พบ violation (รายการพิมพ์ออก stdout บรรทัดละ 1 เป็น JSON)
ใช้กับต้นไม้จริง: python tools/s2_static_check.py
ใช้กับ fixture:   python tools/s2_static_check.py --root <dir>
"""
import argparse
import json
import re
import sys
from pathlib import Path

# ชื่อเป้าหมาย 4 กลุ่ม (§13.2 ข้อ 5a) — เขียนแยกส่วนเพื่อไม่ให้ไฟล์นี้เองถูกสแกนชน
WRITE_PATH_IDENTIFIERS = [
    "Start" + "Move",
    "Start" + "Attack",
    "Battle" + "PauseButtonClicked",
    "GameData" + "Controller",
]

HARMONY_PATTERNS = [
    (r"using\s+HarmonyLib", "using HarmonyLib"),
    (r"\bHarmonyLib\b", "HarmonyLib"),
    (r"\bHarmonyMethod\b", "HarmonyMethod"),
    (r"\bHarmonyPatch\b", "HarmonyPatch"),
    (r"\bPatchAll\b", "PatchAll"),
    (r"\.Patch\s*\(", ".Patch("),
    (r"\.Unpatch\s*\(", ".Unpatch("),
    (r"\[\s*Harmony", "[Harmony..."),
]

PURE_LOGIC_TOKEN = "S2WriteGuardHook"
PURE_LOGIC_DEFINITION_FILE = "S2WriteGuardHook.cs"


def strip_comments(text: str) -> list:
    """คืน list บรรทัดโค้ดที่ตัด comment ออก (// และ /* */ ข้ามบรรทัด)

    ใช้กับกติกา harmony-reference: การ *เอ่อถึง* ชื่อใน comment/doc ไม่ใช่ code reference
    (กฎเข้ม: กติกา write-path-call-site ยังสแกนบรรทัดดิบรวม comment — ชื่อเมธอดเปลี่ยน-state
    ไม่ควรปรากฏใน S2/** แม้ใน comment)
    """
    out = []
    in_block = False
    for line in text.splitlines():
        code = line
        if in_block:
            end = code.find("*/")
            if end < 0:
                out.append("")
                continue
            code = code[end + 2:]
            in_block = False
        start = code.find("/*")
        if start >= 0:
            end2 = code.find("*/", start + 2)
            if end2 >= 0:
                code = code[:start] + code[end2 + 2:]
            else:
                code = code[:start]
                in_block = True
        idx = code.find("//")
        if idx >= 0:
            code = code[:idx]
        out.append(code)
    return out


def check_file(path: Path, root: Path, violations: list) -> None:
    rel = path.relative_to(root).as_posix()
    try:
        text = path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        violations.append({
            "rule": "unreadable", "file": rel, "line": 0,
            "detail": "not valid utf-8", "text": "",
        })
        return

    is_definition = path.name == PURE_LOGIC_DEFINITION_FILE
    code_lines = strip_comments(text)
    for lineno, line in enumerate(text.splitlines(), 1):
        stripped = line.strip()

        # กติกา 1: write-path identifiers (เฉพาะ S2/** — ผู้เรียกส่งเฉพาะไฟล์ใน S2/** มาที่นี่)
        # เข้ม: นับทั้งบรรทัดดิบ (รวม comment)
        for ident in WRITE_PATH_IDENTIFIERS:
            if re.search(r"\b" + re.escape(ident) + r"\b", line):
                violations.append({
                    "rule": "write-path-call-site", "file": rel, "line": lineno,
                    "detail": ident, "text": stripped[:120],
                })

        # กติกา 3: pure logic ห้ามถูกอ้างนอกไฟล์นิยาม (สแกนทั้ง src/** — ผู้เรียกกรองเอง)
        if not is_definition and PURE_LOGIC_TOKEN in line:
            violations.append({
                "rule": "pure-logic-reference", "file": rel, "line": lineno,
                "detail": PURE_LOGIC_TOKEN, "text": stripped[:120],
            })

    # กติกา 2: Harmony references — สแกนเฉพาะโค้ดจริง (ตัด comment แล้ว)
    for lineno, code in enumerate(code_lines, 1):
        if not code.strip():
            continue
        for pattern, label in HARMONY_PATTERNS:
            if re.search(pattern, code):
                violations.append({
                    "rule": "harmony-reference", "file": rel, "line": lineno,
                    "detail": label, "text": code.strip()[:120],
                })


def scan(root: Path) -> list:
    violations = []
    s2_dir = root / "src" / "LongYinCoop" / "S2"
    src_dir = root / "src" / "LongYinCoop"

    if not s2_dir.is_dir():
        violations.append({
            "rule": "missing-dir", "file": "src/LongYinCoop/S2", "line": 0,
            "detail": "directory not found under root", "text": "",
        })
        return violations

    for path in sorted(s2_dir.rglob("*.cs")):
        # กติกา 1+2 กรองเฉพาะ S2/** ; กติกา 3 ทำใน check_file แต่ใช้ได้เฉพาะ src/**
        check_file(path, root, violations)

    for path in sorted(src_dir.rglob("*.cs")):
        rel = path.relative_to(root).as_posix()
        if "/S2/" in ("/" + rel):
            continue  # สแกนแล้วข้างบน
        # นอก S2/** ตรวจแค่กติกา 3 (pure logic isolation) — กันถูกเรียกจากที่อื่นใน src
        try:
            text = path.read_text(encoding="utf-8")
        except UnicodeDecodeError:
            continue
        if path.name == PURE_LOGIC_DEFINITION_FILE:
            continue
        for lineno, line in enumerate(text.splitlines(), 1):
            if PURE_LOGIC_TOKEN in line:
                violations.append({
                    "rule": "pure-logic-reference", "file": rel, "line": lineno,
                    "detail": PURE_LOGIC_TOKEN, "text": line.strip()[:120],
                })

    return violations


def main() -> int:
    parser = argparse.ArgumentParser(description="S2a-4 (b) static check")
    parser.add_argument("--root", default=str(Path(__file__).resolve().parent.parent),
                        help="repo root (default: parent of tools/)")
    parser.add_argument("--json", action="store_true", help="พิมพ์ผลเป็น JSON array บรรทัดเดียว")
    args = parser.parse_args()

    root = Path(args.root).resolve()
    violations = scan(root)

    if args.json:
        print(json.dumps(violations, ensure_ascii=False))
    else:
        if not violations:
            print(f"STATIC CHECK CLEAN (0 violations) root={root}")
        for v in violations:
            print(f"VIOLATION {v['rule']} {v['file']}:{v['line']} detail={v['detail']} text={v['text']}")
        print(f"TOTAL violations={len(violations)}")

    return 0 if not violations else 1


if __name__ == "__main__":
    sys.exit(main())
