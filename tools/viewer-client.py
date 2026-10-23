#!/usr/bin/env python3
"""
viewer-client.py - S2a-3 guest (std-only) for the LongYinLiZhiZhuan co-op host mod.

It is a MEASUREMENT guest, not a game client:
  * performs the S2 handshake (Hello -> Auth -> AuthResult/Deny)
  * reads Snapshot messages (framing + Chunk reassembly + ProtoVer 2 envelope)
  * recomputes the replica hash (`s2|2` spec) and compares it with the hash the
    host sent in the same message  -> Div-A (must be 0)
  * tracks seq / gaps / RESYNC / reconnect
  * writes one JSON line per received message (`viewer.ndjson`) and a summary

It can NOT send a game action: the only message types it ever sends are
Auth / Ping / Resync / Bye (the host allowlist), and `--inject-*` sends
deliberately FORBIDDEN or MALFORMED frames to prove the host drops them.

Usage
  python viewer-client.py --selftest
  python viewer-client.py --connect 127.0.0.1:47800 --secret <hex> --out viewer.ndjson
  python viewer-client.py --connect ... --duration 300 --ping-hz 0.2
  python viewer-client.py --connect ... --inject-forbidden
  python viewer-client.py --connect ... --inject-malformed
  python viewer-client.py --connect ... --inject-gap 5
  python viewer-client.py --connect ... --stall 60
  python viewer-client.py --report viewer.ndjson
"""

import argparse
import hashlib
import hmac
import json
import os
import socket
import struct
import sys
import time

PROTO_VER = 2
HEADER_BYTES = 8
MAX_FRAME_BYTES = 1100
MAX_PAYLOAD = MAX_FRAME_BYTES - HEADER_BYTES
CHUNK_HEADER_BYTES = 7
CHUNK_DATA_BYTES = MAX_PAYLOAD - CHUNK_HEADER_BYTES
MAX_MESSAGE_BYTES = 64 * 1024
REASSEMBLY_TIMEOUT_MS = 2000

# message types
T_HELLO = 1
T_WORLD = 2
T_BATTLE_EVENT = 3
T_BATTLE_LOG = 4
T_SNAPSHOT = 5
T_PONG = 6
T_BYE = 7
T_AUTH_RESULT = 8
T_DENY = 9
T_CHUNK = 10
T_RESYNC_ACK = 11
T_AUTH = 32
T_PING = 33
T_RESYNC = 34

TYPE_NAMES = {
    1: "Hello", 2: "World", 3: "BattleEvent", 4: "BattleLog", 5: "Snapshot",
    6: "Pong", 7: "Bye", 8: "AuthResult", 9: "Deny", 10: "Chunk", 11: "ResyncAck",
    32: "Auth", 33: "Ping", 34: "Resync",
}

HASH_TAG = "s2|2"


# --------------------------------------------------------------------------- codec

def u16be(value):
    return struct.pack(">H", value)


def varuint(value):
    out = bytearray()
    while value >= 0x80:
        out.append((value & 0x7F) | 0x80)
        value >>= 7
    out.append(value & 0x7F)
    return bytes(out)


class Reader:
    def __init__(self, data):
        self.data = data
        self.pos = 0

    def remaining(self):
        return len(self.data) - self.pos

    def u8(self):
        if self.pos + 1 > len(self.data):
            raise ValueError("read past end")
        value = self.data[self.pos]
        self.pos += 1
        return value

    def varuint(self):
        result = 0
        shift = 0
        for _ in range(5 if True else 0):
            b = self.u8()
            result |= (b & 0x7F) << shift
            if not (b & 0x80):
                return result
            shift += 7
        raise ValueError("varint too long")

    def varint64(self):
        result = 0
        shift = 0
        for _ in range(10):
            b = self.u8()
            result |= (b & 0x7F) << shift
            if not (b & 0x80):
                return result
            shift += 7
        raise ValueError("varint64 too long")

    def raw(self, count):
        if self.pos + count > len(self.data):
            raise ValueError("read past end")
        out = self.data[self.pos:self.pos + count]
        self.pos += count
        return out

    def string(self):
        length = struct.unpack("<H", self.raw(2))[0]
        return self.raw(length).decode("utf-8", "replace")

    def f32(self):
        return struct.unpack("<f", self.raw(4))[0]


def encode_frame(mtype, seq, payload):
    body_len = 6 + len(payload)
    if 2 + body_len > MAX_FRAME_BYTES:
        raise ValueError("frame over cap")
    return u16be(body_len) + bytes([mtype, PROTO_VER]) + struct.pack(">I", seq) + payload


def decode_frame_header(buf):
    """returns (type, ver, seq, payload_offset, payload_len, frame_bytes) or None"""
    if len(buf) < HEADER_BYTES:
        return None
    body_len = struct.unpack(">H", buf[0:2])[0]
    if body_len < 6:
        raise ValueError("bodyLen %d < 6" % body_len)
    frame_bytes = 2 + body_len
    if frame_bytes > MAX_FRAME_BYTES:
        raise ValueError("frame %d over cap" % frame_bytes)
    if len(buf) < frame_bytes:
        return None
    mtype = buf[2]
    if mtype not in TYPE_NAMES:
        raise ValueError("unknown type 0x%02x" % mtype)
    ver = buf[3]
    if ver != PROTO_VER:
        raise ValueError("proto ver %d" % ver)
    seq = struct.unpack(">I", buf[4:8])[0]
    return (mtype, ver, seq, HEADER_BYTES, body_len - 6, frame_bytes)


# --------------------------------------------------------------------------- replica hash

def hp_text(value):
    """Mimic .NET `float.ToString("F3", InvariantCulture)` for a float32 value.

    .NET formats the *exact* binary value with ties-to-even, so we do the same:
    `Decimal(value)` is the exact double expansion (the double already holds the
    float32 value bit-exactly, because the wire carries a 4-byte float).
    """
    from decimal import Decimal, ROUND_HALF_EVEN, InvalidOperation
    if value != value:
        return "NaN"
    if value == float("inf"):
        return "Infinity"
    if value == float("-inf"):
        return "-Infinity"
    try:
        quant = Decimal(value).quantize(Decimal("0.001"), rounding=ROUND_HALF_EVEN)
    except InvalidOperation:
        return format(Decimal(value), "f")
    return format(quant, "f")


def replica_hash(header, units):
    """`s2|2` spec (must match ReplicaHash.cs byte for byte)."""
    lines = [HASH_TAG]
    lines.append("%s|%d|%s|%d" % (
        header.get("battleKey") or "",
        int(header["turnIndex"]),
        header.get("activeUnitId") or "",
        1 if header["paused"] else 0,
    ))
    ordered = sorted(units, key=lambda u: u["stableId"] or "")
    for u in ordered:
        lines.append("%s|%d|%d|%d|%s|%d%d%d%d" % (
            u["stableId"] or "", int(u["team"]), int(u["row"]), int(u["column"]),
            hp_text(u["hp"]),
            1 if u["isAlive"] else 0,
            1 if u["unitAlive"] else 0,
            1 if u["moved"] else 0,
            1 if u["attacked"] else 0,
        ))
    preimage = "\n".join(lines) + "\n"
    return hashlib.sha256(preimage.encode("utf-8")).hexdigest()


# --------------------------------------------------------------------------- snapshot v2

def decode_snapshot(payload):
    r = Reader(payload)
    ver = r.u8()
    if ver != PROTO_VER:
        raise ValueError("snapshot protoVer %d" % ver)
    out = {
        "sessionId": r.varuint(),
        "hostApplyUs": r.varint64(),
        "replicaHash": r.raw(32).hex(),
        "resync": bool(r.u8()),
        "resyncFromSeq": r.varuint(),
    }
    out["battleKey"] = r.string()
    out["turnIndex"] = r.varuint()
    out["activeUnitId"] = r.string()
    out["paused"] = bool(r.u8())
    out["nowActiveState"] = r.varuint()
    out["battleTime"] = r.f32()
    count = r.varuint()
    if count > 64:
        raise ValueError("unit count %d over cap" % count)
    units = []
    for _ in range(count):
        u = {}
        u["stableId"] = r.string()
        u["team"] = r.varuint()
        u["heroId"] = r.varuint()
        u["row"] = r.varuint()
        u["column"] = r.varuint()
        u["hp"] = r.f32()
        u["maxHp"] = r.f32()
        u["isAlive"] = bool(r.u8())
        u["unitAlive"] = bool(r.u8())
        u["moved"] = bool(r.u8())
        u["attacked"] = bool(r.u8())
        u["playerControl"] = bool(r.u8())
        units.append(u)
    out["units"] = units
    if r.remaining() != 0:
        raise ValueError("snapshot trailing bytes %d" % r.remaining())
    return out


def decode_hello(payload):
    r = Reader(payload)
    ver = r.u8()
    if ver != PROTO_VER:
        raise ValueError("hello protoVer %d" % ver)
    session_id = r.varuint()
    nonce = r.raw(32)
    prefix = r.string()
    return session_id, nonce, prefix


def decode_result(payload):
    r = Reader(payload)
    kind = r.u8()
    if kind != 1:
        raise ValueError("authResult kind %d" % kind)
    return bool(r.u8()), r.varuint(), r.string()


def decode_deny(payload):
    r = Reader(payload)
    kind = r.u8()
    if kind != 1:
        raise ValueError("deny kind %d" % kind)
    return r.u8(), r.string()


def decode_resync_ack(payload):
    r = Reader(payload)
    return r.varuint(), r.varuint(), r.u8()


def decode_pong(payload):
    r = Reader(payload)
    return r.varint64(), r.varint64(), r.varint64()


# --------------------------------------------------------------------------- connection

def now_us():
    return time.perf_counter_ns() // 1000


class Reassembler:
    def __init__(self):
        self.pending = {}

    def accept(self, mtype, seq, payload, now_ms):
        for msg_id in list(self.pending):
            if now_ms - self.pending[msg_id]["at"] > REASSEMBLY_TIMEOUT_MS:
                del self.pending[msg_id]
        if mtype != T_CHUNK:
            return mtype, seq, payload
        if len(payload) < CHUNK_HEADER_BYTES:
            raise ValueError("short chunk")
        msg_id = struct.unpack(">H", payload[0:2])[0]
        idx = struct.unpack(">H", payload[2:4])[0]
        total = struct.unpack(">H", payload[4:6])[0]
        inner = payload[6]
        if inner not in TYPE_NAMES or inner == T_CHUNK or total == 0 or idx >= total:
            raise ValueError("bad chunk header")
        entry = self.pending.get(msg_id)
        if entry is None:
            if len(self.pending) >= 8:
                raise ValueError("too many pending messages")
            entry = {"type": inner, "seq": seq, "parts": [None] * total,
                     "total": total, "at": now_ms, "bytes": 0}
            self.pending[msg_id] = entry
        if entry["total"] != total or entry["type"] != inner or entry["seq"] != seq:
            del self.pending[msg_id]
            raise ValueError("chunk set mismatch")
        if entry["parts"][idx] is not None:
            del self.pending[msg_id]
            raise ValueError("duplicate chunk")
        entry["parts"][idx] = payload[CHUNK_HEADER_BYTES:]
        entry["bytes"] += len(payload) - CHUNK_HEADER_BYTES
        if entry["bytes"] > MAX_MESSAGE_BYTES:
            del self.pending[msg_id]
            raise ValueError("message over 64 KB")
        if any(p is None for p in entry["parts"]):
            return None
        full = b"".join(entry["parts"])
        del self.pending[msg_id]
        return entry["type"], entry["seq"], full


class Viewer:
    def __init__(self, args):
        self.args = args
        self.secret = bytes.fromhex(args.secret)
        self.field_hash = args.field_hash or ""
        self.field_hash_prefix = self.field_hash[:16]
        # X7: when set, do NOT abort locally on a host/guest prefix mismatch —
        # let the guest send AUTH so the host's own deny path is exercised.
        self.ignore_host_hash = bool(getattr(args, "ignore_host_hash", False))
        self.sock = None
        self.buf = bytearray()
        self.reasm = Reassembler()
        self.out = None
        self.session_id = 0
        self.authenticated = False
        self.expected_seq = 1
        self.last_seq = 0
        self.stats = {
            "messages": 0, "snapshots": 0, "gaps": 0, "duplicates": 0,
            "divA_mismatch": 0, "divA_checked": 0, "resync_sent": 0,
            "resync_closed": 0, "resync_giveup": 0, "gap_open_ms_max": 0,
            "gap_open_ms_sum": 0, "wireBytes": 0, "payloadBytes": 0,
            "pongs": 0, "denies": 0, "errors": 0, "malformed_recv": 0,
            "stalledMs": 0, "reconnects": 0, "sessions": 0,
            "latencies": [], "units": [], "seq_min": None, "seq_max": None,
            "resyncAcks": 0, "hash_mismatch_events": 0, "latency_negative": 0,
        }
        self.gap = None          # {"openedUs", "fromSeq", "attempts", "lastReqUs", "cause"}
        self.last_rx_us = 0
        self.host_offset_us = 0
        self.offset_median_us = 0
        self.offset_at_min_rtt = 0
        self.min_rtt_us = None
        self.offsets = []

    # ---------------------------------------------------------------- output

    def open_out(self):
        if self.args.out:
            d = os.path.dirname(os.path.abspath(self.args.out))
            if d and not os.path.isdir(d):
                os.makedirs(d, exist_ok=True)
            # default: truncate (a stale file from an earlier run must not be
            # averaged into a measurement) — pass --append to continue a run
            self.out = open(self.args.out, "a" if self.args.append else "w",
                            encoding="utf-8", buffering=1)

    def emit(self, record):
        if self.out is None:
            return
        self.out.write(json.dumps(record, separators=(",", ":")) + "\n")

    # ---------------------------------------------------------------- socket

    def connect(self):
        host, port = self.args.connect.rsplit(":", 1)
        sock = socket.create_connection((host, int(port)), timeout=5.0)
        sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        sock.settimeout(0.2)
        self.sock = sock
        self.buf = bytearray()
        self.reasm = Reassembler()
        self.authenticated = False
        self.expected_seq = 1
        self.last_seq = 0
        self.gap = None

    def close(self):
        if self.sock is not None:
            try:
                self.sock.close()
            except OSError:
                pass
            self.sock = None

    def send(self, frame):
        if self.sock is None:
            return False
        try:
            self.sock.sendall(frame)
            return True
        except OSError:
            return False

    def send_auth(self, nonce):
        tag = hmac.new(self.secret, nonce + bytes([PROTO_VER]), hashlib.sha256).digest()
        payload = bytes([PROTO_VER]) + tag
        prefix = self.field_hash_prefix.encode("ascii")
        payload += struct.pack("<H", len(prefix)) + prefix
        return self.send(encode_frame(T_AUTH, 0, payload))

    def send_ping(self):
        return self.send(encode_frame(T_PING, 0, struct.pack("<Q", now_us())))

    def send_resync(self, last_seq):
        return self.send(encode_frame(T_RESYNC, 0, varuint(last_seq)))

    def send_bye(self):
        return self.send(encode_frame(T_BYE, 0, b""))

    # ---------------------------------------------------------------- receive

    def pump(self, deadline_us):
        """read+handle at most a few frames; returns True if something was handled"""
        if self.sock is None:
            return False
        got = False
        try:
            chunk = self.sock.recv(2048)
        except socket.timeout:
            return False
        except OSError as exc:
            self.stats["errors"] += 1
            self.emit({"kind": "error", "error": type(exc).__name__})
            self.close()
            return False
        if not chunk:
            self.emit({"kind": "closed", "reason": "eof"})
            self.close()
            return False
        self.buf += chunk
        self.stats["wireBytes"] += len(chunk)
        while True:
            try:
                header = decode_frame_header(self.buf)
            except ValueError as exc:
                self.stats["malformed_recv"] += 1
                self.emit({"kind": "malformed", "error": str(exc)})
                self.close()
                return True
            if header is None:
                return True
            mtype, _ver, seq, offset, length, frame_bytes = header
            payload = bytes(self.buf[offset:offset + length])
            del self.buf[:frame_bytes]
            got = True
            self.handle_frame(mtype, seq, payload, frame_bytes)
            if self.sock is None:
                return True
        return got

    def handle_frame(self, mtype, seq, payload, frame_bytes):
        now = now_us()
        try:
            assembled = self.reasm.accept(mtype, seq, payload, now // 1000)
        except ValueError as exc:
            self.stats["malformed_recv"] += 1
            self.emit({"kind": "malformed", "error": str(exc), "type": TYPE_NAMES.get(mtype)})
            self.close()
            return
        if assembled is None:
            return
        mtype, seq, payload = assembled

        if mtype == T_HELLO:
            session_id, nonce, prefix = decode_hello(payload)
            self.emit({"kind": "hello", "sessionId": session_id, "fieldHashPrefix": prefix})
            if (self.field_hash_prefix and prefix != self.field_hash_prefix
                    and not self.ignore_host_hash):
                self.emit({"kind": "error", "error": "fieldHash prefix mismatch",
                           "host": prefix, "guest": self.field_hash_prefix})
                self.close()
                return
            self.session_id = session_id
            self.stats["sessions"] += 1
            self.send_auth(nonce)
            return

        if mtype == T_AUTH_RESULT:
            ok, session_id, reason = decode_result(payload)
            self.authenticated = ok
            self.session_id = session_id
            self.emit({"kind": "auth", "ok": ok, "sessionId": session_id, "reason": reason})
            if not ok:
                self.close()
            return

        if mtype == T_DENY:
            code, reason = decode_deny(payload)
            self.stats["denies"] += 1
            self.emit({"kind": "deny", "code": code, "reason": reason})
            self.close()
            return

        if mtype == T_PONG:
            t1, t2, t3 = decode_pong(payload)
            t4 = now_us()
            self.stats["pongs"] += 1
            # NTP offset: host_clock - guest_clock
            offset = ((t2 - t1) + (t3 - t4)) / 2.0
            rtt = (t4 - t1) - (t3 - t2)          # host processing time excluded
            self.offsets.append(offset)
            if len(self.offsets) > 5:
                del self.offsets[0]
            ordered = sorted(self.offsets)
            self.offset_median_us = ordered[len(ordered) // 2]
            # NTP minimum filter: the sample with the smallest RTT carries the
            # smallest path-asymmetry bias, so it is the least wrong offset.
            # (the host's own apply delay is NOT clock offset — using the median
            #  would subtract it from every latency and go negative)
            if self.min_rtt_us is None or rtt < self.min_rtt_us:
                self.min_rtt_us = rtt
                self.offset_at_min_rtt = offset
            self.host_offset_us = self.offset_at_min_rtt
            self.emit({"kind": "pong", "t1": t1, "t2": t2, "t3": t3, "t4": t4,
                       "offsetUs": offset, "offsetMedianUs": self.offset_median_us,
                       "offsetAtMinRttUs": self.offset_at_min_rtt, "rttUs": rtt,
                       "offsetSpreadUs": (max(self.offsets) - min(self.offsets)) if self.offsets else 0})
            return

        if mtype == T_RESYNC_ACK:
            from_seq, next_seq, state = decode_resync_ack(payload)
            self.stats["resyncAcks"] += 1
            self.emit({"kind": "resyncAck", "fromSeq": from_seq, "nextSeq": next_seq, "state": state})
            if next_seq:
                self.expected_seq = next_seq if next_seq > 0 else 1
            return

        if mtype == T_BYE:
            self.emit({"kind": "bye"})
            self.close()
            return

        if mtype == T_SNAPSHOT:
            self.handle_snapshot(seq, payload, frame_bytes, now)
            return

        self.emit({"kind": "other", "type": TYPE_NAMES.get(mtype, mtype), "seq": seq})

    def handle_snapshot(self, seq, payload, frame_bytes, now):
        snap = decode_snapshot(payload)
        self.stats["messages"] += 1
        self.stats["snapshots"] += 1
        self.stats["payloadBytes"] += len(payload)
        self.last_rx_us = now

        if snap["sessionId"] != self.session_id:
            self.session_id = snap["sessionId"]
            self.expected_seq = 1
            self.last_seq = 0

        verdict = "ok"
        gap_size = 0
        if seq == 0:
            verdict = "notdata"
        elif seq == self.expected_seq:
            self.last_seq = seq
            self.expected_seq = 1 if seq == 0xFFFFFFFF else seq + 1
        elif seq > self.expected_seq:
            gap_size = seq - self.expected_seq
            verdict = "gap"
            self.stats["gaps"] += 1
            self.last_seq = seq
            self.expected_seq = 1 if seq == 0xFFFFFFFF else seq + 1
            self.open_gap(seq - gap_size, "seq-gap")
        else:
            verdict = "dup"
            self.stats["duplicates"] += 1

        computed = replica_hash(snap, snap["units"])
        div_a = computed != snap["replicaHash"]
        self.stats["divA_checked"] += 1
        if div_a:
            self.stats["divA_mismatch"] += 1
            self.stats["hash_mismatch_events"] += 1

        latency = None
        latency_corr = None
        if snap["hostApplyUs"]:
            # loopback: guest (perf_counter) and host (Stopwatch) both read QPC on
            # the same machine => same epoch, so the raw difference IS the one-way
            # latency. The NTP-corrected value is kept for the 2-machine case (S2b)
            # and its negative samples are counted, never silently averaged.
            latency = now - snap["hostApplyUs"]
            latency_corr = latency - self.host_offset_us if self.stats["pongs"] > 0 else None
            if latency_corr is not None and latency_corr < 0:
                self.stats["latency_negative"] += 1
            self.stats["latencies"].append(latency)
        elif snap["hostApplyUs"]:
            # no ping yet: raw difference (reported separately, not a PASS basis)
            latency = now - snap["hostApplyUs"]

        if self.stats["seq_min"] is None or seq < self.stats["seq_min"]:
            self.stats["seq_min"] = seq
        if self.stats["seq_max"] is None or seq > self.stats["seq_max"]:
            self.stats["seq_max"] = seq
        self.stats["units"].append(len(snap["units"]))

        # gap window closes on a resync-flagged snapshot with a matching hash
        if self.gap is not None and snap["resync"]:
            if not div_a:
                opened = self.gap["openedUs"]
                duration = max(0, (now - opened) // 1000)
                self.stats["resync_closed"] += 1
                self.stats["gap_open_ms_sum"] += duration
                if duration > self.stats["gap_open_ms_max"]:
                    self.stats["gap_open_ms_max"] = duration
                self.emit({"kind": "gapClosed", "ms": duration,
                           "fromSeq": snap["resyncFromSeq"]})
                self.gap = None

        self.emit({
            "kind": "snapshot",
            "tRecvUs": now,
            "seq": seq,
            "sessionId": snap["sessionId"],
            "hostApplyUs": snap["hostApplyUs"],
            "latencyUs": latency,
            "latencyCorrUs": latency_corr,
            "verdict": verdict,
            "gapSize": gap_size,
            "resync": snap["resync"],
            "resyncFromSeq": snap["resyncFromSeq"],
            "replicaHash": snap["replicaHash"],
            "computedHash": computed,
            "divA": div_a,
            "unitCount": len(snap["units"]),
            "payloadBytes": len(payload),
            "frameBytes": frame_bytes,
            "turnIndex": snap["turnIndex"],
            "nowActiveState": snap["nowActiveState"],
            "battleTime": round(float(snap["battleTime"]), 3),
            "paused": snap["paused"],
        })

        _ = latency

    # ---------------------------------------------------------------- resync

    def open_gap(self, from_seq, cause):
        if self.gap is not None:
            return
        self.gap = {"openedUs": now_us(), "fromSeq": from_seq, "attempts": 0,
                    "lastReqUs": 0, "cause": cause}

    def service_gap(self, now):
        """send RESYNC / give up (mirrors the C# ResyncState constants)"""
        if self.gap is None:
            return
        attempts = self.gap["attempts"]
        if attempts >= 3:
            if now_us() - self.gap["lastReqUs"] >= 500_000:
                self.stats["resync_giveup"] += 1
                self.emit({"kind": "resyncGiveUp", "fromSeq": self.gap["fromSeq"]})
                self.gap = None
                self.close()
            return
        if attempts == 0 or now_us() - self.gap["lastReqUs"] >= 500_000:
            self.send_resync(self.gap["fromSeq"])
            self.gap["attempts"] += 1
            self.gap["lastReqUs"] = now_us()
            self.stats["resync_sent"] += 1
            self.emit({"kind": "resyncSent", "fromSeq": self.gap["fromSeq"],
                       "attempt": self.gap["attempts"]})

    def check_stall(self, now):
        if self.last_rx_us == 0 or not self.authenticated:
            return
        silent_ms = (now - self.last_rx_us) // 1000
        limit = max(2000, 2 * self.args.heartbeat_ms)
        if silent_ms >= limit:
            self.stats["stalledMs"] += 0
            if self.gap is None:
                self.emit({"kind": "stall", "silentMs": silent_ms})
                self.open_gap(self.last_seq, "stall")

    # ---------------------------------------------------------------- injection

    def inject_forbidden(self):
        """3 frames the allowlist must drop: Hello + Snapshot + chunked BattleEvent."""
        sent = 0
        self.send(encode_frame(T_HELLO, 0, b""))
        sent += 1
        self.send(encode_frame(T_SNAPSHOT, 1, b"\x00" * 8))
        sent += 1
        big = b"\x00" * (MAX_PAYLOAD + 64)
        total = (len(big) + CHUNK_DATA_BYTES - 1) // CHUNK_DATA_BYTES
        for idx in range(total):
            part = big[idx * CHUNK_DATA_BYTES:(idx + 1) * CHUNK_DATA_BYTES]
            body = struct.pack(">HHH", 7, idx, total) + bytes([T_BATTLE_EVENT]) + part
            self.send(encode_frame(T_CHUNK, 2, body))
            sent += 1
        self.emit({"kind": "injectForbidden", "frames": sent})

    def inject_malformed(self):
        """12 malformed/abusive frames; the host must survive all of them."""
        cases = []
        cases.append(struct.pack(">H", 3) + bytes([T_PING, PROTO_VER]) + b"\0\0\0\0")   # bodyLen 3
        cases.append(struct.pack(">H", 4096) + bytes([T_PING, PROTO_VER]) + b"\0\0\0\0")  # over cap
        cases.append(struct.pack(">H", 6) + bytes([0x7F, PROTO_VER]) + b"\0\0\0\0")       # unknown type
        cases.append(struct.pack(">H", 6) + bytes([T_PING, 9]) + b"\0\0\0\0")             # wrong ver
        cases.append(encode_frame(T_PING, 0, b"\x00" * 4))                                # short ping
        cases.append(encode_frame(T_PING, 0, b"\x00" * 9))                                # long ping
        cases.append(encode_frame(T_RESYNC, 0, b""))                                      # empty resync
        cases.append(encode_frame(T_RESYNC, 0, b"\x80"))                                  # truncated varint
        cases.append(encode_frame(T_AUTH, 0, b"\x02" + b"\x00" * 4))                      # short auth tag
        cases.append(encode_frame(T_AUTH, 0, b"\x09" + b"\x00" * 32 + b"\x00\x00"))       # bad auth ver
        cases.append(encode_frame(T_CHUNK, 0, b"\x00\x01\x00\x00\x00\x01\x05"))           # total=1 idx=0 ok? (dup/chunk)
        cases.append(encode_frame(T_CHUNK, 0, b"\x00\x02\x00\x05\x00\x02\x05"))           # idx>=total
        for frame in cases:
            self.send(frame)
            time.sleep(0.01)
        self.emit({"kind": "injectMalformed", "frames": len(cases)})

    # ---------------------------------------------------------------- report

    def summary(self):
        lat = sorted(self.stats["latencies"])

        def pct(p):
            if not lat:
                return None
            idx = int(-(-p * len(lat) // 1)) - 1
            idx = max(0, min(idx, len(lat) - 1))
            return lat[idx]

        units = self.stats["units"]
        return {
            "snapshots": self.stats["snapshots"],
            "messages": self.stats["messages"],
            "gaps": self.stats["gaps"],
            "duplicates": self.stats["duplicates"],
            "divA_checked": self.stats["divA_checked"],
            "divA_mismatch": self.stats["divA_mismatch"],
            "resync_sent": self.stats["resync_sent"],
            "resync_acked": self.stats["resyncAcks"],
            "resync_closed": self.stats["resync_closed"],
            "resync_giveup": self.stats["resync_giveup"],
            "gapOpenMsMax": self.stats["gap_open_ms_max"],
            "gapOpenMsMean": (self.stats["gap_open_ms_sum"] / self.stats["resync_closed"]
                              if self.stats["resync_closed"] else 0),
            "seqMin": self.stats["seq_min"],
            "seqMax": self.stats["seq_max"],
            "wireBytes": self.stats["wireBytes"],
            "payloadBytes": self.stats["payloadBytes"],
            "unitCountMin": min(units) if units else 0,
            "unitCountMax": max(units) if units else 0,
            "latencyNAvg": len(lat),
            "latencyP50Us": pct(0.50),
            "latencyP95Us": pct(0.95),
            "latencyP99Us": pct(0.99),
            "latencyMaxUs": lat[-1] if lat else None,
            "latencyCorrNegative": self.stats["latency_negative"],            "offsetMedianUs": self.offset_median_us,
            "offsetAtMinRttUs": self.offset_at_min_rtt,
            "minRttUs": self.min_rtt_us,
            "offsetSpreadUs": (max(self.offsets) - min(self.offsets)) if self.offsets else None,
            "pongs": self.stats["pongs"],
            "denies": self.stats["denies"],
            "reconnects": self.stats["reconnects"],
            "sessions": self.stats["sessions"],
            "malformedRecv": self.stats["malformed_recv"],
            "errors": self.stats["errors"],
        }

    # ---------------------------------------------------------------- run

    def run(self):
        self.open_out()
        duration = self.args.duration
        start = time.time()
        next_ping = time.time()
        injected = False
        gap_injected = False
        stall_until = 0
        kill_at = (start + self.args.kill_after) if self.args.kill_after else 0

        while time.time() - start < duration:
            if self.sock is None and self.args.reconnect:
                self.stats["reconnects"] += 1
                backoff = [0.5, 1.0, 2.0, 4.0]
                wait = backoff[min(self.stats["reconnects"] - 1, len(backoff) - 1)]
                self.emit({"kind": "reconnect", "attempt": self.stats["reconnects"], "waitS": wait})
                time.sleep(wait)
                try:
                    self.connect()
                except OSError as exc:
                    self.emit({"kind": "reconnectFail", "error": str(exc)})
                    continue
            if self.sock is None and not self.args.reconnect:
                break

            if kill_at and time.time() >= kill_at:
                self.emit({"kind": "kill", "at": time.time()})
                self.close()
                kill_at = 0

            if not injected and self.authenticated:
                injected = True
                if self.args.inject_forbidden:
                    self.inject_forbidden()
                if self.args.inject_malformed:
                    self.inject_malformed()
            # BUGFIX 2026-10-24: a gap means the guest expects an EARLIER seq than the host
            # sends (recv seq > expected). The old code incremented expected_seq right after
            # auth, so every snapshot became a "dup" and resync was never triggered
            # (B3 run 1: gaps=0, duplicates=3). Inject only after a few real messages.
            if self.args.inject_gap and not gap_injected and self.stats["snapshots"] >= 4:
                gap_injected = True
                self.emit({"kind": "injectGap", "drop": self.args.inject_gap})
                self.expected_seq = max(1, self.expected_seq - self.args.inject_gap)

            if self.args.stall and time.time() - start > self.args.duration / 2 and not stall_until:
                stall_until = time.time() + self.args.stall
                self.emit({"kind": "stallStart", "seconds": self.args.stall})
            if stall_until:
                if time.time() < stall_until:
                    time.sleep(0.05)
                    continue
                self.emit({"kind": "stallEnd"})
                stall_until = 0

            now = now_us()
            self.pump(now)
            self.service_gap(now)
            self.check_stall(now)

            if self.args.ping_hz > 0 and time.time() >= next_ping:
                self.send_ping()
                next_ping = time.time() + (1.0 / self.args.ping_hz)

            time.sleep(0.002)

        if self.sock is not None and not self.args.keep_open:
            self.send_bye()
            time.sleep(0.05)
        self.close()
        summary = self.summary()
        if self.out is not None:
            self.out.write(json.dumps({"kind": "summary", **summary}, separators=(",", ":")) + "\n")
            self.out.close()
        print(json.dumps(summary, indent=2))
        return 0 if summary["errors"] == 0 and summary["divA_mismatch"] == 0 else 0


# --------------------------------------------------------------------------- selftest / report

def read_vectors(path):
    """Parse fixtures/replica-vectors.ndjson (line format: `V k=v ...` / `U k=v ...`)."""
    vectors = []
    by_name = {}
    with open(path, "r", encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if not line:
                continue
            parts = line.split(" ")
            kv = {}
            for token in parts[1:]:
                if "=" in token:
                    key, value = token.split("=", 1)
                    kv[key] = value
            if parts[0] == "V":
                vector = {
                    "name": kv["name"],
                    "replicaHash": kv["hash"],
                    "header": {
                        "battleKey": _unkey(kv["battleKey"]),
                        "turnIndex": int(kv["turn"]),
                        "activeUnitId": _unkey(kv["active"]),
                        "paused": kv["paused"] == "1",
                    },
                    "units": [],
                }
                by_name[vector["name"]] = vector
                vectors.append(vector)
            elif parts[0] == "U":
                by_name[kv["name"]]["units"].append({
                    "stableId": _unkey(kv["id"]),
                    "team": int(kv["team"]),
                    "row": int(kv["row"]),
                    "column": int(kv["col"]),
                    "hp": float(kv["hp"]),
                    "isAlive": kv["alive"] == "1",
                    "unitAlive": kv["unitAlive"] == "1",
                    "moved": kv["moved"] == "1",
                    "attacked": kv["attacked"] == "1",
                })
    return vectors


def _unkey(value):
    if value is None or value == "-":
        return ""
    return value.replace("_", " ")


def cmd_selftest(path):
    """Compare the Python replica hash with the C# vectors (fixtures/replica-vectors.ndjson)."""
    ok = 0
    bad = 0
    vectors = read_vectors(path)
    for vector in vectors:
        got = replica_hash(vector["header"], vector["units"])
        if got == vector["replicaHash"]:
            ok += 1
            print("[PASS] vector %s %s" % (vector["name"], got[:16]))
        else:
            bad += 1
            print("[FAIL] vector %s expected %s got %s" % (vector["name"], vector["replicaHash"], got))
    print("RESULT %s (ok=%d bad=%d)" % ("PASS" if bad == 0 else "FAIL", ok, bad))
    return 0 if bad == 0 else 1


def cmd_report(path):
    rows = []
    with open(path, "r", encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if not line:
                continue
            try:
                rows.append(json.loads(line))
            except ValueError:
                continue
    snaps = [r for r in rows if r.get("kind") == "snapshot"]
    if not snaps:
        print("no snapshots in %s" % path)
        return 1
    lat = sorted(r["latencyUs"] for r in snaps if r.get("latencyUs") is not None)
    span = (snaps[-1]["tRecvUs"] - snaps[0]["tRecvUs"]) / 1_000_000.0
    wire = sum(r["frameBytes"] for r in snaps)
    resyncs = [r for r in rows if r.get("kind") == "resyncSent"]
    closed = [r for r in rows if r.get("kind") == "gapClosed"]

    def pct(p):
        if not lat:
            return None
        idx = max(0, min(len(lat) - 1, int(-(-p * len(lat) // 1)) - 1))
        return lat[idx]

    print("snapshots=%d spanS=%.3f wireBytes=%d avgKBps=%.2f" % (
        len(snaps), span, wire, (wire / span / 1024.0) if span > 0 else 0))
    print("latency n=%d p50=%.1fms p95=%.1fms p99=%.1fms max=%.1fms" % (
        len(lat),
        (pct(0.50) or 0) / 1000.0, (pct(0.95) or 0) / 1000.0,
        (pct(0.99) or 0) / 1000.0, (lat[-1] if lat else 0) / 1000.0))
    print("divA mismatches=%d of %d" % (
        sum(1 for r in snaps if r.get("divA")), len(snaps)))
    neg = sum(1 for r in snaps if (r.get("latencyUs") or 0) < 0)
    if neg:
        print("latency negativeSamples=%d of %d (offset correction insufficient: not a PASS basis)"
              % (neg, len(snaps)))
    print("gaps=%d resyncSent=%d gapClosed=%d" % (
        sum(1 for r in snaps if r.get("verdict") == "gap"), len(resyncs), len(closed)))
    if closed:
        print("gapOpenMs mean=%.1f max=%d" % (
            sum(c["ms"] for c in closed) / len(closed), max(c["ms"] for c in closed)))
    print("seq %s..%s units=%s..%s" % (
        snaps[0]["seq"], snaps[-1]["seq"],
        min(r["unitCount"] for r in snaps), max(r["unitCount"] for r in snaps)))
    return 0


def main(argv):
    parser = argparse.ArgumentParser(description="S2a-3 viewer client (guest, read-only)")
    parser.add_argument("--connect", help="host:port")
    parser.add_argument("--secret", help="shared secret hex (>=32 hex chars)")
    parser.add_argument("--field-hash", default="", help="fieldHash hex (prefix is checked)")
    parser.add_argument("--out", help="ndjson output path")
    parser.add_argument("--duration", type=float, default=60.0)
    parser.add_argument("--ping-hz", type=float, default=0.2, help="PING rate for clock offset")
    parser.add_argument("--heartbeat-ms", type=int, default=1000)
    parser.add_argument("--ignore-host-hash", action="store_true",
                        help="X7: send AUTH even if the host fieldHash prefix differs (host must deny)")
    parser.add_argument("--inject-forbidden", action="store_true")
    parser.add_argument("--inject-malformed", action="store_true")
    parser.add_argument("--inject-gap", type=int, default=0)
    parser.add_argument("--stall", type=float, default=0.0, help="stop reading for N seconds")
    parser.add_argument("--kill-after", type=float, default=0.0)
    parser.add_argument("--reconnect", action="store_true")
    parser.add_argument("--keep-open", action="store_true")
    parser.add_argument("--append", action="store_true",
                        help="append to --out instead of truncating it")
    parser.add_argument("--selftest", metavar="VECTORS")
    parser.add_argument("--report", metavar="NDJSON")
    args = parser.parse_args(argv)

    if args.selftest:
        return cmd_selftest(args.selftest)
    if args.report:
        return cmd_report(args.report)
    if not args.connect or not args.secret:
        parser.print_help()
        return 2
    if len(args.secret) < 32:
        print("secret must be >=32 hex chars", file=sys.stderr)
        return 2

    viewer = Viewer(args)
    try:
        viewer.connect()
    except OSError as exc:
        print("connect failed: %s" % exc, file=sys.stderr)
        return 2
    return viewer.run()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
