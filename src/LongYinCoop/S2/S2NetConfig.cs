using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LongYinCoop.S2
{
    /// <summary>
    /// S2a-2: อ่าน `coop.net.*` จาก `UserData\MelonPreferences.cfg` เอง (ไม่ใช้ MelonPreferences entry)
    /// เพื่อให้สลับ pref กลางรันได้ตาม D2-i (poll ≤1 Hz) และเพื่อให้ทดสอบได้โดยไม่ต้องมีเกม
    /// </summary>
    public struct S2NetSettings
    {
        public bool Enabled;
        public string Role;            // off | host | guest
        public string Secret;          // hex, ≥32 ตัวอักษร (ห้าม log)
        public bool AllowNonLoopback;
        public bool FileRead;
        public bool SectionFound;
        public string Error;
        public long ReadCount;
        public long LastReadMs;

        public bool IsValid
        {
            get
            {
                if (!FileRead || !SectionFound) return false;
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
                "enabled={0} role={1} allowNonLoopback={2} secretSet={3} section={4} reads={5} error={6}",
                Enabled, Role ?? "-", AllowNonLoopback, SecretLooksSet, SectionFound, ReadCount,
                Error ?? "-");
        }
    }

    public sealed class S2NetConfig
    {
        public const string SectionName = "LongYinCoop";
        public const string KeyEnabled = "coop.net.enabled";
        public const string KeyRole = "coop.role";
        public const string KeySecret = "coop.secret";
        public const string KeyAllowNonLoopback = "coop.allowNonLoopback";

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
                    S2NetSettings missing = Parse("", path, nowMs, readCount);
                    missing.Error = "cfg not found";
                    return missing;
                }
                text = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception e)
            {
                S2NetSettings failed = Parse("", path, nowMs, readCount);
                failed.Error = e.GetType().Name;
                return failed;
            }

            return Parse(text, path, nowMs, readCount);
        }

        /// <summary>อ่านค่าเฉพาะ section [LongYinCoop] (test ได้โดยไม่ต้องมีเกม)</summary>
        public static S2NetSettings Parse(string text, string path, long nowMs, long readCount = 1)
        {
            S2NetSettings s = new S2NetSettings
            {
                Role = "off",
                Secret = null,
                Enabled = false,
                AllowNonLoopback = false,
                FileRead = true,
                SectionFound = false,
                ReadCount = readCount,
                LastReadMs = nowMs,
            };

            if (string.IsNullOrEmpty(text)) return s;

            string section = null;
            List<string> keys = new List<string>();
            List<string> values = new List<string>();
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    section = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                if (section != SectionName) continue;
                keys.Add(line.Substring(0, eq).Trim());
                values.Add(line.Substring(eq + 1).Trim());
            }

            // section มีจริงถ้าเจอบรรทัด [LongYinCoop]
            s.SectionFound = HasSection(text);

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                string value = values[i];
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

        private static bool HasSection(string text)
        {
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length > 2 && line[0] == '[' && line[line.Length - 1] == ']'
                    && line.Substring(1, line.Length - 2).Trim() == SectionName)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsTrue(string value)
        {
            if (value == null) return false;
            string v = value.Trim().Trim('"').ToLowerInvariant();
            return v == "true" || v == "1" || v == "yes" || v == "on";
        }
    }
}
