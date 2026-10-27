# scan_hour_writes.py — negative check: find ALL movss/mov stores targeting WorldData.hour
# (field offset 0xB4) in .text, to test the claim that ChangeHour (0x792620) is the only writer.
# Patterns: movss store = F3 0F 11 /r (mod=10 -> disp32) with optional REX prefix (40-4F).
# Read-only; writes hour-writes.tsv.
import csv
import struct
from pathlib import Path

from time_static_common import GameImage

OUT = Path(__file__).resolve().parent
DISP = 0xB4


def main():
    gi = GameImage()
    rows = []
    for sec_name, sec_rva, sec_size in gi.exec_sections():
        size = min(sec_size, 0x3000000)
        base_off = gi.rva_to_off(sec_rva)
        code = gi.read(sec_rva, size)
        n = len(code)
        for i in range(n - 8):
            # F3 [REX] 0F 11 /r  with mod=10 (disp32)
            if code[i] != 0xF3:
                continue
            j = i + 1
            rex = 0
            if 0x40 <= code[j] <= 0x4F:
                rex = code[j]
                j += 1
            if code[j] != 0x0F or code[j + 1] != 0x11:
                continue
            modrm = code[j + 2]
            if (modrm >> 6) != 0b10:  # mod=10 => disp32
                continue
            rm = modrm & 7
            disp_pos = j + 3
            disp = struct.unpack_from("<i", code, disp_pos)[0]
            if disp == DISP:
                site = sec_rva + i
                # decode for context
                import capstone

                md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)
                ins = next(md.disasm(code[i : i + 12], gi.rva_to_va(site)), None)
                rows.append(
                    {
                        "site_rva": f"0x{site:X}",
                        "insn": ins.mnemonic + " " + ins.op_str if ins else "?",
                        "owner": gi.owner(site) or "?",
                        "target": f"[r{rm}+0x{DISP:X}]" + ("(REX)" if rex else ""),
                    }
                )
    with open(OUT / "hour-writes.tsv", "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, delimiter="\t", fieldnames=["site_rva", "insn", "owner", "target"])
        w.writeheader()
        w.writerows(rows)
    print(f"movss stores to [reg+0xB4]: {len(rows)}")
    for r in rows:
        print(f"  {r['site_rva']:>10}  {r['insn']:<28} owner={r['owner']}")


if __name__ == "__main__":
    main()
