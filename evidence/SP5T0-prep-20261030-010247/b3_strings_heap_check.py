#!/usr/bin/env python3
"""B3 — ตรวจ #Strings heap ดิบของ LongYinCoop.dll (อ่านไบต์ล้วน ไม่โหลดแอสเซมบลีเข้ากระบวนการใด)

PE -> Optional header -> CLR data directory (index 14) -> COR20 header -> metadata root (BSJB)
-> stream headers -> '#Strings' heap -> หาชื่อ type แบบ exact null-terminated entry
"""
import struct
import sys

TYPES = [
    "Sp5T0Probe",
    "TimeV2aSampler",
    "TimeV2aDetector",
    "TimeV2aGate",
    "TimeV2aNdjsonWriter",
    "TimeV2aObs",
    "TimeV2aEvent",
    "S2GuestDecoder",
    "S2OverlayClient",
    "S2OverlayRenderModel",
    "S2OverlayService",
    "S2SetPrefGate",
    "S2WriteGuardHook",
]


def rva_to_offset(rva, sections):
    for va, vsize, raw, rsize in sections:
        span = max(vsize, rsize)
        if va <= rva < va + span:
            return rva - va + raw
    raise SystemExit(f"RVA {rva:#x} not in any section")


def main(path):
    data = open(path, "rb").read()
    if data[:2] != b"MZ":
        raise SystemExit("not MZ")
    e_lfanew = struct.unpack_from("<I", data, 0x3C)[0]
    if data[e_lfanew:e_lfanew + 4] != b"PE\x00\x00":
        raise SystemExit("not PE")
    coff = e_lfanew + 4
    num_sections = struct.unpack_from("<H", data, coff + 2)[0]
    opt_size = struct.unpack_from("<H", data, coff + 16)[0]
    opt = coff + 20
    magic = struct.unpack_from("<H", data, opt)[0]
    if magic == 0x20B:      # PE32+
        dir_base = opt + 112
    elif magic == 0x10B:    # PE32
        dir_base = opt + 96
    else:
        raise SystemExit(f"unknown optional header magic {magic:#x}")
    clr_rva, clr_size = struct.unpack_from("<II", data, dir_base + 14 * 8)
    if clr_rva == 0:
        raise SystemExit("no CLR runtime header")

    sec_base = opt + opt_size
    sections = []
    for i in range(num_sections):
        off = sec_base + i * 40
        vsize = struct.unpack_from("<I", data, off + 8)[0]
        va = struct.unpack_from("<I", data, off + 12)[0]
        rsize = struct.unpack_from("<I", data, off + 16)[0]
        raw = struct.unpack_from("<I", data, off + 20)[0]
        sections.append((va, vsize, raw, rsize))

    cor20 = rva_to_offset(clr_rva, sections)
    md_rva, md_size = struct.unpack_from("<II", data, cor20 + 8)
    md = rva_to_offset(md_rva, sections)
    if data[md:md + 4] != b"BSJB":
        raise SystemExit("metadata root signature not BSJB")
    ver_len = struct.unpack_from("<I", data, md + 12)[0]
    p = md + 16 + ((ver_len + 3) & ~3) + 2  # skip version string + flags
    n_streams = struct.unpack_from("<H", data, p)[0]
    p += 2
    strings_off = strings_size = None
    for _ in range(n_streams):
        s_off, s_size = struct.unpack_from("<II", data, p)
        p += 8
        name_start = p
        end = data.index(b"\x00", p)
        name = data[name_start:end].decode("ascii")
        p = end + 1
        p = md + 16 + ((ver_len + 3) & ~3)  # recompute below correctly instead
        # stream names are padded to 4-byte boundary relative to their own start
        pad = (4 - ((end + 1 - name_start) % 4)) % 4
        p = end + 1 + pad
        if name == "#Strings":
            strings_off, strings_size = s_off, s_size

    if strings_off is None:
        raise SystemExit("#Strings stream not found")
    heap = data[md + strings_off: md + strings_off + strings_size]
    print(f"#Strings heap: offset={strings_off:#x} size={strings_size} (file {path})")

    ok = True
    for t in TYPES:
        needle = t.encode("ascii") + b"\x00"
        found = False
        start = 0
        while True:
            i = heap.find(needle, start)
            if i < 0:
                break
            if i == 0 or heap[i - 1] == 0x00:  # exact entry, not a substring
                found = True
                break
            start = i + 1
        print(f"  {t}: {'FOUND' if found else 'NOT FOUND'}")
        ok = ok and found
    print("RESULT:", "ALL 6 TYPES FOUND" if ok else "MISSING TYPES")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
