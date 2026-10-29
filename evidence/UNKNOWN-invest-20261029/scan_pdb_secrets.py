# scan_pdb_secrets.py — สแกน PDB หา path เครื่อง/ชื่อผู้ใช้/ชื่อเครื่อง/SteamID/GUID
# (read-only — ไม่พิมพ์ค่า พิมพ์เฉพาะ พบ/ไม่พบ + ชนิด + จำนวน + ตำแหน่ง; เทียบกับ hostname/username
#  ของเครื่องนี้แบบ match/mismatch โดยไม่พิมพ์ตัวค่า)
import hashlib
import os
import re
import socket
import sys
import getpass

HOST = socket.gethostname()
USER = getpass.getuser()

for path in sys.argv[1:]:
    data = open(path, "rb").read()
    print("=" * 8, os.path.basename(path))
    print("size:", len(data), "bytes · sha256:", hashlib.sha256(data).hexdigest())

    def find(pat, label, flags=0):
        hits = [m.start() for m in re.finditer(pat, data, flags)]
        print(("พบ  " if hits else "ไม่พบ") + f"  {label:40s} count={len(hits)}"
              + (f" offsets(แรก<=5)={[hex(h) for h in hits[:5]]}" if hits else ""))
        return hits

    n_path = find(rb"C:\\Users\\", "Windows user-profile path (C:\\Users\\)")
    find(rb"C:\\Program Files", "C:\\Program Files path")
    find(rb"(?i)7656119[0-9]{10}", "SteamID64 pattern")
    find(rb"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", "email")
    find(rb"https?://", "URL")

    # username / hostname: นับจำนวนที่ตรงกับของเครื่องนี้ — ไม่พิมพ์ค่า
    ub = USER.encode()
    hb = HOST.encode()
    print(f"username(เครื่องนี้): พบในไฟล์ {data.count(ub)} ครั้ง (case-sensitive byte match)")
    print(f"hostname(เครื่องนี้): พบในไฟล์ {data.count(hb)} ครั้ง (case-sensitive byte match)")

    # GUID มาตรฐาน 8-4-4-4-12 (รูปแบบตัวอักษรในไฟล์) — PDB ฝัง GUID ตามฟอร์แมตโดยดีไซน์
    guids = [m.start() for m in re.finditer(rb"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", data)]
    print(f"GUID pattern (8-4-4-4-12): พบ {len(guids)} ตำแหน่ง" + (f" แรก={[hex(g) for g in guids[:5]]}" if guids else ""))

    # สตริง path ทั้งหมดใน PDB (โครงสร้าง — จำนวน/ชนิด ไม่พิมพ์ค่า)
    paths = re.findall(rb"(?:[A-Za-z]:\\[^\"<>|\x00-\x1f]{3,})", data)
    src_like = [p for p in paths if p.lower().endswith((b".cs", b".csproj", b".props", b".targets"))]
    print(f"path-like strings ทั้งหมด: {len(paths)} · ที่ลงท้าย .cs/.csproj/.props/.targets: {len(src_like)}")
    users_like = [p for p in paths if p.lower().startswith(b"c:\\users\\")]
    print(f"path ภายใต้ C:\\Users\\: {len(users_like)} (ไม่แสดงค่า)")
