using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace LongYinCoop.S2.Protocol
{
    public enum BindVerdict
    {
        Ok = 0,
        Empty = 1,
        NotLiteral = 2,
        NotLoopback = 3,
    }

    /// <summary>
    /// กติกา bind ของ S2 (§13.2 ข้อ 6 · แผน S2a-3 §4): **loopback literal เท่านั้น**
    ///   - รับเฉพาะ IP literal ที่ parse ได้ (`127.0.0.1`, `127.5.5.5`, `::1`, `::ffff:127.0.0.1`)
    ///   - ปฏิเสธ `0.0.0.0` / `::` / `*` (any-address), hostname (`localhost`), ค่าว่าง
    ///   - IP ที่ไม่ใช่ loopback ผ่านได้เฉพาะเมื่อ `allowNonLoopback=true` (S2b ต้องอนุมัติเป็นลายลักษณ์)
    ///
    /// pure: ไม่มี socket, ไม่มี state — เทสต์ได้โดยไม่ต้องเปิดพอร์ต (T0)
    /// </summary>
    public static class BindPolicy
    {
        public const string DefaultBind = "127.0.0.1";

        public static BindVerdict Validate(string bind, bool allowNonLoopback)
        {
            string text = bind == null ? string.Empty : bind.Trim();
            if (text.Length == 0) return BindVerdict.Empty;

            // any-address spellings ที่ IPAddress.TryParse อาจรับ/ไม่รับ ไม่ต้องเดา: ปฏิเสธตรง ๆ
            if (text == "*" || text == "+" || text == "any") return BindVerdict.NotLoopback;

            IPAddress ip;
            if (!IPAddress.TryParse(text, out ip)) return BindVerdict.NotLiteral;
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();

            if (ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any)) return BindVerdict.NotLoopback;
            if (IsLoopback(ip)) return BindVerdict.Ok;
            return allowNonLoopback ? BindVerdict.Ok : BindVerdict.NotLoopback;
        }

        /// <summary>loopback = 127.0.0.0/8 (IPv4) หรือ ::1 (IPv6)</summary>
        public static bool IsLoopback(IPAddress ip)
        {
            if (ip == null) return false;
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                return IPAddress.IPv6Loopback.Equals(ip);
            byte[] bytes = ip.GetAddressBytes();
            return bytes.Length == 4 && bytes[0] == 127;
        }

        /// <summary>ผ่านได้ไหมถ้าไม่ตั้ง pref (ใช้ใน S2a-3: loopback เท่านั้น)</summary>
        public static bool IsLoopbackLiteral(string bind)
        {
            return Validate(bind, allowNonLoopback: false) == BindVerdict.Ok;
        }

        public static string Describe(BindVerdict verdict)
        {
            switch (verdict)
            {
                case BindVerdict.Ok: return "ok";
                case BindVerdict.Empty: return "bind is empty";
                case BindVerdict.NotLiteral: return "bind must be an IP literal (no hostname)";
                default: return "bind must be loopback unless allowNonLoopback=true (S2b approval required)";
            }
        }

        /// <summary>ข้อความปลอดภัยสำหรับ log: ไม่มี secret, ไม่มี path</summary>
        public static string DescribeEndpoint(string bind, int port)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", bind ?? "-", port);
        }
    }
}
