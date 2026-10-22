using System;
using System.Globalization;

namespace LongYinCoop.S2.Protocol
{
    public enum DenyCode : byte
    {
        Unknown = 0,
        BadAuth = 1,
        BadVersion = 2,
        FieldMismatch = 3,
        RateLimited = 4,
        Busy = 5,
        Timeout = 6,
        AttemptsExceeded = 7,
    }

    public enum HandshakeOutcome
    {
        Accepted = 0,
        Denied = 1,
        Malformed = 2,
        Expired = 3,
        WrongState = 4,
        AttemptsExceeded = 5,
    }

    /// <summary>
    /// wire codec ของ handshake 4 ชนิด (LE + CompactWriter — ตัวเดียวกับ snapshot)
    ///   Hello      (host→guest): u8 protoVer | varu32 sessionId | 32B nonce | str fieldHashPrefix8
    ///   Auth       (guest→host): u8 protoVer | 32B tag | str fieldHashPrefix8
    ///   AuthResult (host→guest): u8 1 | u8 ok | varu32 sessionId | str reason
    ///   Deny       (host→guest): u8 1 | u8 code | str reason
    /// pure: เขียน/อ่าน byte[] เท่านั้น — ใช้ร่วมกับ Python client ได้ (test vectors)
    /// </summary>
    public static class HandshakeCodec
    {
        public const string VersionTag = "s2-hs|1";

        public static byte[] EncodeHello(uint sessionId, byte[] nonce, string fieldHashHex)
        {
            if (nonce == null || nonce.Length != S2Auth.NonceBytes)
                throw new ArgumentException("nonce must be 32 bytes");
            CompactWriter w = new CompactWriter(64);
            w.WriteByte(S2Protocol.ProtoVer);
            w.WriteVarUInt(sessionId);
            w.WriteRaw(nonce);
            w.WriteString(S2Auth.FieldHashPrefix(fieldHashHex));
            return w.ToArray();
        }

        public static bool TryDecodeHello(byte[] payload, out uint sessionId, out byte[] nonce,
                                          out string fieldHashPrefix, out string error)
        {
            sessionId = 0;
            nonce = null;
            fieldHashPrefix = null;
            error = null;
            try
            {
                CompactReader r = new CompactReader(payload);
                byte ver = r.ReadByte();
                if (ver != S2Protocol.ProtoVer)
                {
                    error = "hello protoVer " + ver;
                    return false;
                }
                sessionId = r.ReadVarUInt();
                nonce = r.ReadRaw(S2Auth.NonceBytes).ToArray();
                fieldHashPrefix = r.ReadString();
                if (r.Remaining != 0)
                {
                    error = "hello trailing bytes " + r.Remaining;
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                error = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        public static byte[] EncodeAuth(byte[] tag, string fieldHashHex)
        {
            if (tag == null || tag.Length != S2Auth.TagBytes)
                throw new ArgumentException("tag must be 32 bytes");
            CompactWriter w = new CompactWriter(64);
            w.WriteByte(S2Protocol.ProtoVer);
            w.WriteRaw(tag);
            w.WriteString(S2Auth.FieldHashPrefix(fieldHashHex));
            return w.ToArray();
        }

        public static bool TryDecodeAuth(byte[] payload, out byte[] tag, out string fieldHashPrefix,
                                         out string error)
        {
            tag = null;
            fieldHashPrefix = null;
            error = null;
            try
            {
                CompactReader r = new CompactReader(payload);
                byte ver = r.ReadByte();
                if (ver != S2Protocol.ProtoVer)
                {
                    error = "auth protoVer " + ver;
                    return false;
                }
                tag = r.ReadRaw(S2Auth.TagBytes).ToArray();
                fieldHashPrefix = r.ReadString();
                if (r.Remaining != 0)
                {
                    error = "auth trailing bytes " + r.Remaining;
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                error = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        public static byte[] EncodeResult(bool ok, uint sessionId, string reason)
        {
            CompactWriter w = new CompactWriter(64);
            w.WriteByte(1);
            w.WriteBool(ok);
            w.WriteVarUInt(sessionId);
            w.WriteString(reason);
            return w.ToArray();
        }

        public static bool TryDecodeResult(byte[] payload, out bool ok, out uint sessionId,
                                           out string reason, out string error)
        {
            ok = false;
            sessionId = 0;
            reason = null;
            error = null;
            try
            {
                CompactReader r = new CompactReader(payload);
                byte kind = r.ReadByte();
                if (kind != 1)
                {
                    error = "authResult kind " + kind;
                    return false;
                }
                ok = r.ReadBool();
                sessionId = r.ReadVarUInt();
                reason = r.ReadString();
                return true;
            }
            catch (Exception e)
            {
                error = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        public static byte[] EncodeDeny(DenyCode code, string reason)
        {
            CompactWriter w = new CompactWriter(64);
            w.WriteByte(1);
            w.WriteByte((byte)code);
            w.WriteString(reason);
            return w.ToArray();
        }

        public static bool TryDecodeDeny(byte[] payload, out DenyCode code, out string reason,
                                         out string error)
        {
            code = DenyCode.Unknown;
            reason = null;
            error = null;
            try
            {
                CompactReader r = new CompactReader(payload);
                byte kind = r.ReadByte();
                if (kind != 1)
                {
                    error = "deny kind " + kind;
                    return false;
                }
                code = (DenyCode)r.ReadByte();
                reason = r.ReadString();
                return true;
            }
            catch (Exception e)
            {
                error = e.GetType().Name + ": " + e.Message;
                return false;
            }
        }
    }

    /// <summary>
    /// ฝั่ง host ของ handshake (pure: ไม่มี socket, ไม่มี thread)
    ///   - nonce ใหม่ทุก connection · อายุ 5 s
    ///   - Auth ผิดได้ ≤ <see cref="MaxAttempts"/> ครั้งต่อ connection แล้วปิด
    ///   - หลัง Auth ล้มเหลว **ห้ามต่อใหม่ภายใน 5 s** (กัน brute force)
    ///     · reconnect ปกติ (Bye/EOF) **ไม่ถูกจำกัด** — มิติ 2026-10-22 (แผน §3.4 ต้องต่อได้ 0.5 s)
    /// </summary>
    public sealed class S2HandshakeServer
    {
        public const int DefaultTimeoutMs = 5000;
        public const int MaxAttempts = 4;
        public const int FailedAuthCooldownMs = 5000;

        private readonly byte[] _secret;
        private readonly string _fieldHashHex;
        private readonly int _timeoutMs;

        private byte[] _nonce;
        private long _helloAtMs;
        private long _lastFailureMs;
        private bool _hasFailure;

        public S2HandshakeServer(byte[] secret, string fieldHashHex, int timeoutMs = DefaultTimeoutMs)
        {
            if (secret == null || secret.Length == 0) throw new ArgumentException("secret required");
            _secret = secret;
            _fieldHashHex = fieldHashHex ?? string.Empty;
            _timeoutMs = timeoutMs < 1 ? DefaultTimeoutMs : timeoutMs;
        }

        public bool Authenticated { get; private set; }
        public uint SessionId { get; private set; }
        public int Attempts { get; private set; }
        public long Accepted { get; private set; }
        public long Denied { get; private set; }
        public long Expired { get; private set; }
        public DenyCode LastDenyCode { get; private set; }

        public string FieldHashPrefix { get { return S2Auth.FieldHashPrefix(_fieldHashHex); } }

        /// <summary>เริ่ม connection ใหม่: ล้าง nonce/attempts (ไม่ล้างสถิติและไม่ล้าง failure cooldown)</summary>
        public void NewConnection()
        {
            _nonce = null;
            _helloAtMs = 0;
            Attempts = 0;
            Authenticated = false;
            SessionId = 0;
        }

        /// <summary>หลัง Auth ล้มเหลว ต้องรอ 5 s ก่อนรับ connection ใหม่ (brute-force ถูกจำกัด)</summary>
        public bool AllowsNewConnection(long nowMs)
        {
            if (!_hasFailure) return true;
            return nowMs - _lastFailureMs >= FailedAuthCooldownMs;
        }

        /// <summary>สร้าง Hello (nonce ใหม่) สำหรับ connection ที่เพิ่ง accept</summary>
        public byte[] BuildHello(uint sessionId, long nowMs)
        {
            SessionId = sessionId;
            _nonce = S2Auth.NewNonce();
            _helloAtMs = nowMs;
            Attempts = 0;
            Authenticated = false;

            byte[] payload = HandshakeCodec.EncodeHello(sessionId, _nonce, _fieldHashHex);
            return FrameCodec.Encode(MessageType.Hello, S2Protocol.ProtoVer, 0, payload);
        }

        public HandshakeOutcome AcceptAuth(in AssembledMessage msg, long nowMs, out byte[] reply,
                                           out DenyCode code, out string reason)
        {
            reply = null;
            code = DenyCode.Unknown;
            reason = null;

            if (Authenticated)
            {
                reason = "already authenticated";
                return HandshakeOutcome.WrongState;
            }
            if (_nonce == null)
            {
                reason = "no hello sent";
                code = DenyCode.BadAuth;
                return HandshakeOutcome.WrongState;
            }
            if (nowMs - _helloAtMs > _timeoutMs)
            {
                Expired++;
                _nonce = null;
                RecordFailure(nowMs, DenyCode.Timeout);
                code = DenyCode.Timeout;
                reason = "handshake timed out";
                reply = DenyFrame(code, reason);
                return HandshakeOutcome.Expired;
            }

            Attempts++;
            if (Attempts > MaxAttempts)
            {
                _nonce = null;
                RecordFailure(nowMs, code);
                code = DenyCode.AttemptsExceeded;
                reason = "too many auth attempts";
                reply = DenyFrame(code, reason);
                return HandshakeOutcome.AttemptsExceeded;
            }

            byte[] tag;
            string prefix;
            string decodeError;
            if (!HandshakeCodec.TryDecodeAuth(msg.Payload, out tag, out prefix, out decodeError))
            {
                RecordFailure(nowMs, code);
                code = DenyCode.BadAuth;
                reason = "auth payload: " + decodeError;
                reply = DenyFrame(code, reason);
                return HandshakeOutcome.Malformed;
            }

            if (msg.Ver != S2Protocol.ProtoVer)
            {
                RecordFailure(nowMs, code);
                code = DenyCode.BadVersion;
                reason = "frame ver " + msg.Ver;
                reply = DenyFrame(code, reason);
                return HandshakeOutcome.Denied;
            }

            string expectedPrefix = S2Auth.FieldHashPrefix(_fieldHashHex);
            if (!string.Equals(prefix, expectedPrefix, StringComparison.Ordinal))
            {
                RecordFailure(nowMs, code);
                code = DenyCode.FieldMismatch;
                reason = "fieldHash prefix mismatch";
                reply = DenyFrame(code, reason);
                return HandshakeOutcome.Denied;
            }

            if (!S2Auth.VerifyTag(_secret, _nonce, S2Protocol.ProtoVer, tag))
            {
                RecordFailure(nowMs, code);
                code = DenyCode.BadAuth;
                reason = "hmac mismatch";
                reply = DenyFrame(code, reason);
                return HandshakeOutcome.Denied;
            }

            // สำเร็จ
            Authenticated = true;
            Accepted++;
            _nonce = null;
            _hasFailure = false;
            reply = FrameCodec.Encode(MessageType.AuthResult, S2Protocol.ProtoVer, 0,
                                      HandshakeCodec.EncodeResult(true, SessionId, "ok"));
            return HandshakeOutcome.Accepted;
        }

        private void RecordFailure(long nowMs, DenyCode code)
        {
            Denied++;
            LastDenyCode = code;
            _lastFailureMs = nowMs;
            _hasFailure = true;
        }

        private static byte[] DenyFrame(DenyCode code, string reason)
        {
            return FrameCodec.Encode(MessageType.Deny, S2Protocol.ProtoVer, 0,
                                     HandshakeCodec.EncodeDeny(code, reason));
        }

        public string ToText()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "hsAuth={0} authenticated={1} sessionId={2} attempts={3} denied={4} expired={5} lastDeny={6}",
                Accepted, Authenticated ? "true" : "false", SessionId, Attempts, Denied, Expired,
                LastDenyCode);
        }
    }

    /// <summary>
    /// ฝั่ง guest: ตรวจ Hello → สร้าง Auth → ตรวจ AuthResult/Deny
    /// (Python `viewer-client.py` ทำแบบเดียวกัน — มี vector ร่วมใน `fixtures/`)
    /// </summary>
    public sealed class S2HandshakeClient
    {
        private readonly byte[] _secret;
        private readonly string _fieldHashHex;
        private byte[] _nonce;

        public S2HandshakeClient(byte[] secret, string fieldHashHex)
        {
            if (secret == null || secret.Length == 0) throw new ArgumentException("secret required");
            _secret = secret;
            _fieldHashHex = fieldHashHex ?? string.Empty;
        }

        public uint SessionId { get; private set; }
        public bool Authenticated { get; private set; }
        public DenyCode LastDeny { get; private set; }

        public bool OnHello(in AssembledMessage msg, out byte[] authFrame, out string error)
        {
            authFrame = null;
            error = null;
            if (msg.Type != MessageType.Hello)
            {
                error = "expected Hello, got " + msg.Type;
                return false;
            }

            uint sessionId;
            byte[] nonce;
            string prefix;
            byte[] payload = msg.Payload;
            if (!HandshakeCodec.TryDecodeHello(payload, out sessionId, out nonce, out prefix, out error))
                return false;

            string expectedPrefix = S2Auth.FieldHashPrefix(_fieldHashHex);
            if (!string.Equals(prefix, expectedPrefix, StringComparison.Ordinal))
            {
                error = "fieldHash prefix mismatch (host=" + prefix + " guest=" + expectedPrefix + ")";
                return false;
            }

            _nonce = nonce;
            SessionId = sessionId;
            byte[] tag = S2Auth.ComputeTag(_secret, nonce, S2Protocol.ProtoVer);
            byte[] authPayload = HandshakeCodec.EncodeAuth(tag, _fieldHashHex);
            authFrame = FrameCodec.Encode(MessageType.Auth, S2Protocol.ProtoVer, 0, authPayload);
            return true;
        }

        public bool OnReply(in AssembledMessage msg, out string error)
        {
            error = null;
            if (msg.Type == MessageType.AuthResult)
            {
                bool ok;
                uint sessionId;
                string reason;
                if (!HandshakeCodec.TryDecodeResult(msg.Payload, out ok, out sessionId, out reason, out error))
                    return false;
                if (!ok)
                {
                    error = "auth rejected: " + reason;
                    return false;
                }
                Authenticated = true;
                SessionId = sessionId;
                return true;
            }

            if (msg.Type == MessageType.Deny)
            {
                DenyCode code;
                string reason;
                if (!HandshakeCodec.TryDecodeDeny(msg.Payload, out code, out reason, out error))
                    return false;
                LastDeny = code;
                error = "denied " + code + ": " + reason;
                return false;
            }

            error = "expected AuthResult/Deny, got " + msg.Type;
            return false;
        }
    }
}
