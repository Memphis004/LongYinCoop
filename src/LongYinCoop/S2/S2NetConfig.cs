using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace LongYinCoop.S2
{
    /// <summary>
    /// ค่าที่อ่านได้จาก `net.cfg` (ไม่ใช่ MelonPreferences) — `<see cref="S2NetConfig"/>` เป็นตัวอ่าน
    /// </summary>
    public struct S2NetSettings
    {
        public bool Enabled;
        public string Role;            // off | host | guest
        public string Secret;          // hex, ≥32 ตัวอักษร (ห้าม log)
        public bool AllowNonLoopback;
        public bool FileRead;          // อ่านไฟล์ได้ (ไม่มีไฟล์ = false)
        public bool FormatOk;          // โครงไฟล์ถูก (flat key=value, ไม่มี section)
        public string Error;
        public long ReadCount;
        public long LastReadMs;

        public bool IsValid
        {
            get
            {
                if (!FileRead || !FormatOk) return false;
                if (!string.IsNullOrEmpty(Error)) return false;
                if (Enabled && !IsKnownRole(Role)) return false;
                if (Enabled && !IsAcceptableSecret(Secret)) return false;
                return true;
            }
        }

        public static bool IsKnownRole(string role)
        {
            return role == "host" || role == "guest";
        }

        /// <summary>secret ≥32 ตัว และเป็น hex เท่านั้น (ไม่คืนค่า secret ออก log)</summary>
        public static bool IsAcceptableSecret(string secret)
        {
            if (string.IsNullOrEmpty(secret) || secret.Length < 32) return false;
            for (int i = 0; i < secret.Length; i++)
            {
                char c = secret[i];
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex) return false;
            }
            return true;
        }

        public bool SecretLooksSet { get { return IsAcceptableSecret(Secret); } }

        /// <summary>คำอธิบายปลอด secret สำหรับ log</summary>
        public string Describe()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "enabled={0} role={1} allowNonLoopback={2} secretSet={3} format={4} reads={5} error={6}",
                Enabled, Role ?? "-", AllowNonLoopback, SecretLooksSet, FormatOk, ReadCount,
                Error ?? "-");
        }
    }

    /// <summary>
    /// งาน A (มติผู้ใช้ 2026-10-22): อ่าน `coop.net.*` จาก `UserData\LongYinCoop\net.cfg`
    /// ซึ่ง **MelonLoader ไม่เคยอ่านหรือเขียน** — แยกจาก `MelonPreferences.cfg` โดยสิ้นเชิง
    ///
    /// เหตุผล (RUNTIME, วัด 2026-10-22 — `evidence/S2a2-20261021-2254/S2a2-report.md` §3.3):
    ///   1) ค่า string ดิบที่ไม่ครอบ quote ใน `MelonPreferences.cfg` ทำให้ TOML ทั้งไฟล์เสีย
    ///      → MelonLoader ใช้ค่า default → `Enabled=false` → mod นิ่ง ไม่มี IPC pipe
    ///   2) ตอนออกเกม MelonLoader เขียน cfg ใหม่ แล้วแปลง `coop.net.enabled` เป็นตารางซ้อน
    ///      `[LongYinCoop.coop]` + `net = { enabled = false }` และทิ้ง key ที่ไม่รู้จัก
    ///      → reader แบบเดิม (อ่านเฉพาะ section `[LongYinCoop]` แบบ literal) อ่านไม่เจอเงียบ ๆ
    ///
    /// รูปแบบไฟล์: flat `key = value` บรรทัดละคู่ · `#`/`;` = comment · **ไม่มี section / ไม่ใช่ TOML**
    /// <code>
    /// enabled          = false          # true = ให้ S2 ทำงาน (ต้อง role=host และ write path ปิด)
    /// role             = host           # host | guest
    /// secret           = 0123…cdef      # hex ≥32 ตัว (ห้าม log)
    /// allowNonLoopback = false
    /// </code>
    ///
    /// Fail-closed: ไม่มีไฟล์ / อ่านไม่ได้ / มี `[section]` / บรรทัดที่ไม่ใช่ `key = value`
    /// ⇒ `FormatOk=false` ⇒ `IsValid=false` ⇒ S2 ไม่ทำงาน · key ที่ไม่รู้จักถูกข้าม (เผื่ออนาคต)
    /// · key ซ้ำ = ตัวหลังชนะ · ค่าที่ครอบ quote (`"host"`) ถูกถอด quote ให้ (ทนการก๊อปจาก TOML)
    /// </summary>
    public sealed class S2NetConfig
    {
        public const string FileName = "net.cfg";

        public const string KeyEnabled = "enabled";
        public const string KeyRole = "role";
        public const string KeySecret = "secret";
        public const string KeyAllowNonLoopback = "allownonloopback"; // เทียบแบบ lower-case

        private readonly int _pollMs;
        private long _lastPollMs = long.MinValue / 2;

        public S2NetConfig(string path, int pollMs = 1000)
        {
            Path = path;
            _pollMs = pollMs < 1 ? 1 : pollMs;
            Current = Parse("", path, 0);
        }

        public string Path { get; private set; }

        public S2NetSettings Current { get; private set; }

        public long PollCount { get; private set; }

        /// <summary>อ่านไฟล์ใหม่เมื่อครบรอบ poll; คืน true เมื่อรี้อ่านจริง (ไม่ไว้ใจ cache)</summary>
        public bool Poll(long nowMs)
        {
            if (nowMs - _lastPollMs < _pollMs) return false;
            _lastPollMs = nowMs;
            PollCount++;
            Current = ReadFile(Path, nowMs, PollCount);
            return true;
        }

        /// <summary>บังคับอ่านทันที (ใช้ตอน init)</summary>
        public S2NetSettings ForceRead(long nowMs)
        {
            _lastPollMs = nowMs;
            PollCount++;
            Current = ReadFile(Path, nowMs, PollCount);
            return Current;
        }

        public static S2NetSettings ReadFile(string path, long nowMs, long readCount)
        {
            string text;
            try
            {
                if (!File.Exists(path))
                {
                    S2NetSettings missing = Blank(nowMs, readCount);
                    missing.FileRead = false;
                    missing.FormatOk = false;
                    missing.Error = "cfg not found";
                    return missing;
                }
                text = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception e)
            {
                S2NetSettings failed = Blank(nowMs, readCount);
                failed.FileRead = false;
                failed.FormatOk = false;
                failed.Error = e.GetType().Name;
                return failed;
            }

            return Parse(text, path, nowMs, readCount);
        }

        /// <summary>อ่านค่า flat key=value (test ได้โดยไม่ต้องมีเกม)</summary>
        public static S2NetSettings Parse(string text, string path, long nowMs, long readCount = 1)
        {
            S2NetSettings s = Blank(nowMs, readCount);
            if (string.IsNullOrEmpty(text)) return s;

            int lineNo = 0;
            foreach (string raw in text.Split('\n'))
            {
                lineNo++;
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;

                if (line[0] == '[')
                {
                    // กันการก๊อป TOML/pref เดิมมาใส่: net.cfg ต้อง flat เท่านั้น
                    s.FormatOk = false;
                    s.Error = "line " + lineNo + ": net.cfg is flat key=value (no [section])";
                    return s;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    s.FormatOk = false;
                    s.Error = "line " + lineNo + ": expected key = value";
                    return s;
                }

                string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                string value = Unquote(line.Substring(eq + 1).Trim());

                if (key == KeyEnabled)
                {
                    s.Enabled = IsTrue(value);
                }
                else if (key == KeyRole)
                {
                    s.Role = value.ToLowerInvariant();
                }
                else if (key == KeySecret)
                {
                    s.Secret = value;
                }
                else if (key == KeyAllowNonLoopback)
                {
                    s.AllowNonLoopback = IsTrue(value);
                }
                // key อื่น: ข้าม (เผื่ออนาคต ไม่ทำให้ไฟล์เสีย)
            }

            if (s.Enabled && !S2NetSettings.IsKnownRole(s.Role))
            {
                s.Error = "role must be host|guest when enabled";
            }
            else if (s.Enabled && !S2NetSettings.IsAcceptableSecret(s.Secret))
            {
                s.Error = "secret must be >=32 hex chars when enabled";
            }

            return s;
        }

        private static S2NetSettings Blank(long nowMs, long readCount)
        {
            return new S2NetSettings
            {
                Role = "off",
                Secret = null,
                Enabled = false,
                AllowNonLoopback = false,
                FileRead = true,
                FormatOk = true,
                ReadCount = readCount,
                LastReadMs = nowMs,
            };
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2)
            {
                char first = value[0];
                char last = value[value.Length - 1];
                if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
                    return value.Substring(1, value.Length - 2).Trim();
            }
            return value;
        }

        public static bool IsTrue(string value)
        {
            if (value == null) return false;
            string v = value.Trim().Trim('"').ToLowerInvariant();
            return v == "true" || v == "1" || v == "yes" || v == "on";
        }
    }
}
