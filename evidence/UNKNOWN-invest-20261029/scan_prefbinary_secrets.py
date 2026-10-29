# scan_prefbinary_secrets.py — สแกนหาสตริงที่เข้าข่าย token/secret/ID/path ผู้ใช้ใน pref binary
# (read-only — ไม่พิมพ์ค่าใด ๆ พิมพ์เฉพาะ พบ/ไม่พบ + ชนิด + ตำแหน่ง offset + จำนวน)
# ตามคำสั่งผู้ใช้ 2026-10-29 ข้อ 1 — ห้ามพิมพ์ค่า
import re
import sys

PATH = sys.argv[1] if len(sys.argv) > 1 else r"evidence\AUTO-20261028-070417\PlayerprefData.dat.pre-rebaseline-20261028-0704"
data = open(PATH, "rb").read()
print("file:", PATH)
print("size:", len(data), "bytes")
print("sha256:", __import__("hashlib").sha256(data).hexdigest())

def find(pat, label, flags=0):
    hits = [m.start() for m in re.finditer(pat, data, flags)]
    print(("พบ  " if hits else "ไม่พบ") + f"  {label:36s} count={len(hits)}"
          + (f" offsets(แรก<=5)={[hex(h) for h in hits[:5]]}" if hits else ""))
    return hits

find(rb"[0-9a-fA-F]{16,}", "hex-token (>=16 chars)")
find(rb"(?i)secret|token|password|passwd|bearer|apikey|api_key|access_?key", "keyword secret/token/auth")
find(rb"C:\\Users\\", "Windows user-profile path (C:\\Users\\)")
find(rb"7656119[0-9]{10}", "SteamID64 pattern")
find(rb"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", "email")
find(rb"https?://", "URL")
find(rb"(?i)coop\.secret|net\.cfg|role", "mod net.cfg key names")

ascii_strs = [(m.start(), m.group().decode("ascii")) for m in re.finditer(rb"[\x20-\x7e]{6,}", data)]
u16 = [(m.start(), m.group().decode("utf-16-le", errors="ignore")) for m in re.finditer(rb"(?:[\x20-\x7e]\x00){6,}", data)]
print("ascii strings (>=6 chars):", len(ascii_strs), "· utf16le strings:", len(u16))
longs = [s for s in ascii_strs if len(s[1]) >= 24]
print("ascii strings >=24 chars:", len(longs), "· offsets:", [hex(s[0]) for s in longs[:10]])
keys = [s[1] for s in ascii_strs if 4 <= len(s[1]) <= 22]
print("key-like names (sample 20 — เฉพาะชื่อ key/รูปแบบ ไม่ใช่ค่า):", keys[:20])
print("(สแกนนี้ไม่พิมพ์ค่าใด — ตามคำสั่งผู้ใช้)")
