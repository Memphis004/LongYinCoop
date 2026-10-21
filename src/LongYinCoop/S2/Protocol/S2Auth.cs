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

        internal static string Hex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }
}
