# time_static_common.py — shared loader for read-only static analysis of GameAssembly.dll
# Purity: opens the DLL and script.json READ-ONLY; writes nothing outside the evidence folder.
# Tools: pefile 2024.8.26 + capstone 5.0.7 (already installed; nothing installed this round).
# Address convention (verified against Il2CppDumper script.json): every "RVA" below is
#   VA - ImageBase with ImageBase = 0x180000000 (GameAssembly.dll default). script.json
#   ScriptMethod[].Address is already RVA in this unit.

import bisect
import json
import struct
import sys
from pathlib import Path

import capstone
import pefile

GAME_DIR = Path(r"C:\Program Files (x86)\Steam\steamapps\common\LongYinLiZhiZhuan")
DLL_PATH = GAME_DIR / "GameAssembly.dll"
SCRIPT_JSON = GAME_DIR / "Il2CppDumper" / "script.json"


class GameImage:
    def __init__(self):
        self.pe = pefile.PE(str(DLL_PATH), fast_load=True)
        self.image_base = self.pe.OPTIONAL_HEADER.ImageBase
        self.raw = DLL_PATH.read_bytes()
        self._load_methods()

    # ---- address mapping -------------------------------------------------
    def rva_to_off(self, rva):
        return self.pe.get_offset_from_rva(rva)

    def off_to_rva(self, off):
        return self.pe.get_rva_from_offset(off)

    def va_to_rva(self, va):
        return va - self.image_base

    def rva_to_va(self, rva):
        return rva + self.image_base

    def read(self, rva, size):
        off = self.rva_to_off(rva)
        return self.raw[off : off + size]

    def float_at(self, rva):
        return struct.unpack("<f", self.read(rva, 4))[0]

    def double_at(self, rva):
        return struct.unpack("<d", self.read(rva, 8))[0]

    def int32_at(self, rva):
        return struct.unpack("<i", self.read(rva, 4))[0]

    def exec_sections(self):
        out = []
        for s in self.pe.sections:
            if s.Characteristics & 0x20000000:  # IMAGE_SCN_MEM_EXECUTE
                out.append((s.Name.rstrip(b"\x00").decode(errors="replace"), s.VirtualAddress, s.Misc_VirtualSize))
        return out

    # ---- il2cpp method names ---------------------------------------------
    def _load_methods(self):
        with open(SCRIPT_JSON, encoding="utf-8") as f:
            data = json.load(f)
        self.methods = [(m["Address"], m["Name"], m.get("Signature", "")) for m in data["ScriptMethod"]]
        self.methods.sort(key=lambda t: t[0])
        self._starts = [t[0] for t in self.methods]
        self.by_name = {}
        for rva, name, sig in self.methods:
            self.by_name.setdefault(name, []).append(rva)
        # metadata usage slots (used to resolve FF15 call [rip+slot] targets)
        self.meta_by_slot = {m["Address"]: m["Name"] for m in data.get("ScriptMetadataMethod", [])}
        # string literals: address -> literal
        self.strings_by_addr = {s["Address"]: s.get("Name", s.get("Value", "")) for s in data.get("ScriptString", [])}

    def owner(self, rva):
        """Name of the il2cpp method whose RVA is the greatest start <= rva. Approximate: it is
        the owning function only if no non-method gap/foreign code sits between; treat as
        'containing (nearest preceding) method' and verify by range sanity at call sites."""
        i = bisect.bisect_right(self._starts, rva) - 1
        if i < 0:
            return None
        return self.methods[i][1]

    def owner_range(self, rva):
        i = bisect.bisect_right(self._starts, rva) - 1
        if i < 0:
            return None, None
        start = self._starts[i]
        end = self._starts[i + 1] if i + 1 < len(self._starts) else start + 0x10000
        return start, end

    def rva_name(self, rva):
        names = [n for r, n, _ in self.methods if r == rva]
        return names[0] if names else None

    def resolve(self, name):
        return self.by_name.get(name, [])

    # ---- disassembly -----------------------------------------------------
    def _cs(self):
        md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
        md.detail = True
        return md

    def disasm(self, rva, max_insns=400, max_bytes=0x2000, stop_on_double_ret_gap=False):
        """Linear disassembly from rva. Returns list of capstone instructions."""
        code = self.read(rva, max_bytes)
        out = []
        for insn in self._cs().disasm(code, self.rva_to_va(rva)):
            out.append(insn)
            if len(out) >= max_insns:
                break
            if insn.mnemonic in ("ret",) or insn.mnemonic.startswith("ret"):
                break
        return out

    # ---- call-site scan --------------------------------------------------
    def scan_direct_xrefs(self, target_rvas, sections=None):
        """Byte-scan every executable section for E8/E9 rel32 whose target is in target_rvas.
        Validation: the 5 bytes must decode as a call/jmp (capstone, single instruction).
        Returns list of dicts: site_rva, site_va, kind, target_rva, owner."""
        targets = set(target_rvas)
        hits = []
        md = self._cs()
        for sec_name, sec_rva, sec_size in (sections or self.exec_sections()):
            size = min(sec_size, 0x3000000)
            code = self.read(sec_rva, size)
            n = len(code)
            i = 0
            # naive scan for opcode bytes; validate by decoding
            find = code.find
            for op, kind in ((0xE8, "E8"), (0xE9, "E9")):
                pos = 0
                while True:
                    pos = find(bytes([op]), pos)
                    if pos < 0 or pos + 5 > n:
                        break
                    rel = struct.unpack_from("<i", code, pos + 1)[0]
                    tgt_rva = sec_rva + pos + 5 + rel
                    if tgt_rva in targets:
                        # validate: decode 1 instruction starting at this byte
                        ok = False
                        for insn in md.disasm(code[pos : pos + 5], 0):
                            ok = insn.mnemonic in ("call", "jmp") and insn.size == 5
                            break
                        if ok:
                            site_rva = sec_rva + pos
                            hits.append(
                                {
                                    "site_rva": site_rva,
                                    "site_va": self.rva_to_va(site_rva),
                                    "kind": kind,
                                    "target_rva": tgt_rva,
                                    "owner": self.owner(site_rva),
                                }
                            )
                    pos += 1
        hits.sort(key=lambda h: h["site_rva"])
        return hits

    def scan_ff15_xrefs(self, target_slot_rvas):
        """Scan executable sections for FF 15 (call qword ptr [rip+disp]) whose *metadata slot*
        RVA is in target_slot_rvas. Slots come from script.json ScriptMetadataMethod."""
        targets = set(target_slot_rvas)
        hits = []
        md = self._cs()
        for sec_name, sec_rva, sec_size in self.exec_sections():
            size = min(sec_size, 0x3000000)
            code = self.read(sec_rva, size)
            n = len(code)
            find = code.find
            pos = 0
            while True:
                pos = find(b"\xff\x15", pos)
                if pos < 0 or pos + 6 > n:
                    break
                disp = struct.unpack_from("<i", code, pos + 2)[0]
                slot_rva = sec_rva + pos + 6 + disp
                if slot_rva in targets:
                    site_rva = sec_rva + pos
                    hits.append(
                        {
                            "site_rva": site_rva,
                            "site_va": self.rva_to_va(site_rva),
                            "kind": "FF15",
                            "slot_rva": slot_rva,
                            "slot_name": self.meta_by_slot.get(slot_rva, "?"),
                            "owner": self.owner(site_rva),
                        }
                    )
                pos += 2
        hits.sort(key=lambda h: h["site_rva"])
        return hits

    # ---- rip-relative float constants ------------------------------------
    def rip_floats(self, insns):
        """For instructions with rip-relative memory operand, resolve target RVA and, if the
        bytes there look like a float/double constant pool entry, return decoded values."""
        out = []
        for insn in insns:
            for op in insn.operands:
                if op.type == capstone.x86.X86_OP_MEM and op.mem.base == capstone.x86.X86_REG_RIP:
                    tgt_va = insn.address + insn.size + op.mem.disp
                    tgt_rva = self.va_to_rva(tgt_va)
                    if tgt_rva < 0 or tgt_rva + 8 > len(self.raw):
                        continue
                    try:
                        off = self.rva_to_off(tgt_rva)
                    except Exception:
                        continue
                    # only .rdata-ish (non-executable) reads are constants
                    entry = {"insn": f"{insn.mnemonic} {insn.op_str}", "insn_rva": self.va_to_rva(insn.address), "pool_rva": tgt_rva}
                    try:
                        entry["f32"] = struct.unpack_from("<f", self.raw, off)[0]
                        entry["f64"] = struct.unpack_from("<d", self.raw, off)[0]
                    except Exception:
                        continue
                    out.append(entry)
        return out

    def section_of(self, rva):
        for s in self.pe.sections:
            if s.VirtualAddress <= rva < s.VirtualAddress + s.Misc_VirtualSize:
                return s.Name.rstrip(b"\x00").decode(errors="replace")
        return "?"


def fmt_floats(entry):
    parts = []
    f32 = entry.get("f32")
    f64 = entry.get("f64")
    if f32 is not None and abs(f32) < 1e12:
        parts.append(f"f32={f32:.6g}")
    if f64 is not None and abs(f64) < 1e15 and not (f32 is not None and abs(f32 - f64) < 1e-9):
        parts.append(f"f64={f64:.6g}")
    return " ".join(parts)
