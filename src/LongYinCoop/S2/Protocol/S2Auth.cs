using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace LongYinCoop.S2.Protocol
{
    /// <summary>Shared-secret handshake (rev2 §13): HMAC-SHA256 over nonce || PROTO_VER.</summary>
    public static class S2Auth
    {
        public const int NonceBytes = 32;
        public const int TagBytes = 32;

        public static byte[] NewNonce() => RandomNumberGenerator.GetBytes(NonceBytes);

        public static byte[] HmacSha256(byte[] key, ReadOnlySpan<byte> data)
        {
            if (key == null || key.Length == 0) throw new ArgumentException("key is required");
            return HMACSHA256.HashData(key, data);
        }

        public static byte[] ComputeTag(byte[] secret, ReadOnlySpan<byte> nonce, byte protoVer)
        {
            if (secret == null || secret.Length == 0) throw new ArgumentException("secret is required");
            if (nonce.Length != NonceBytes) throw new ArgumentException("nonce must be 32 bytes");

            Span<byte> payload = stackalloc byte[NonceBytes + 1];
            nonce.CopyTo(payload);
            payload[NonceBytes] = protoVer;
            return HMACSHA256.HashData(secret, payload);
        }

        public static bool VerifyTag(byte[] secret, ReadOnlySpan<byte> nonce, byte protoVer,
                                     ReadOnlySpan<byte> tag)
        {
            if (tag.Length != TagBytes) return false;
            byte[] expected;
            try { expected = ComputeTag(secret, nonce, protoVer); }
            catch (ArgumentException) { return false; }
            return CryptographicOperations.FixedTimeEquals(expected, tag);
        }

        /// <summary>
        /// Hash of the replicated field inventory + the game anchor (VERSION.md hashes).
        /// Sorted so the order in source code cannot change the value.
        /// </summary>
        public static string FieldHash(IEnumerable<string> fields, string gameAnchor)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            List<string> sorted = new List<string>(fields);
            sorted.Sort(StringComparer.Ordinal);
            StringBuilder sb = new StringBuilder("s2|fields|1\n");
            foreach (string field in sorted) sb.Append(field).Append('\n');
            sb.Append(gameAnchor ?? string.Empty).Append('\n');
            return Hex(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
        }

        /// <summary>8 ไบต์แรกของ fieldHash (16 hex) — ส่งใน handshake เพื่อยืนยันว่า replicas ตรงกัน</summary>
        public static string FieldHashPrefix(string fieldHashHex, int bytes = 8)
        {
            if (string.IsNullOrEmpty(fieldHashHex)) return string.Empty;
            int chars = Math.Min(fieldHashHex.Length, Math.Max(1, bytes) * 2);
            return fieldHashHex.Substring(0, chars);
        }

        /// <summary>hex → bytes · คืน null ถ้าความยาวคี่หรือมีตัวอักษรนอก hex (ไม่ throw)</summary>
        public static byte[] BytesFromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex) || (hex.Length & 1) != 0) return null;
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int hi = HexDigit(hex[i * 2]);
                int lo = HexDigit(hex[i * 2 + 1]);
                if (hi < 0 || lo < 0) return null;
                bytes[i] = (byte)((hi << 4) | lo);
            }
            return bytes;
        }

        private static int HexDigit(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }

        internal static string Hex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }
}
