using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace LongYinCoop
{
    /// <summary>
    /// อ่านอย่างเดียว: รายการ method ที่มี Harmony patch อยู่ (ของ mod อื่น เช่น LongYinThai)
    /// ใช้ HarmonyLib.Harmony.GetAllPatchedMethods() — ไม่แตะ state เกม
    /// </summary>
    internal static class CompatInventory
    {
        public static string Dump(string tag = null)
        {
            try
            {
                List<MethodBase> patched = new List<MethodBase>(HarmonyLib.Harmony.GetAllPatchedMethods());
                patched.Sort(delegate (MethodBase a, MethodBase b)
                {
                    return string.CompareOrdinal(Key(a), Key(b));
                });

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(UIStrings.CompatTitle);
                sb.AppendLine();
                sb.AppendLine(string.Format(UIStrings.CompatGenerated, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                sb.AppendLine(string.Format(UIStrings.CompatProcess, Environment.ProcessId));
                sb.AppendLine(string.Format(UIStrings.CompatTotal, patched.Count));
                sb.AppendLine();
                sb.AppendLine(UIStrings.CompatTableHeader);
                sb.AppendLine(UIStrings.CompatTableDivider);

                int i = 0;
                foreach (MethodBase m in patched)
                {
                    i++;
                    Patches info = HarmonyLib.Harmony.GetPatchInfo(m);
                    if (info == null) continue;
                    sb.AppendLine(string.Format(UIStrings.CompatRow, i, TypeName(m), m.Name,
                                  Join(info.Owners), Count(info.Prefixes), Count(info.Postfixes),
                                  Count(info.Transpilers), Count(info.Finalizers)));
                }

                sb.AppendLine();
                sb.AppendLine(UIStrings.CompatNotesTitle);
                sb.AppendLine(UIStrings.CompatNoteReadOnly);
                sb.AppendLine(UIStrings.CompatNotePurpose);

                string name = string.IsNullOrEmpty(tag) ? "compat-notes.md" : "compat-notes-" + Sanitize(tag) + ".md";
                CoopPaths.EnsureUserDataDir();
                string path = Path.Combine(CoopPaths.UserDataDir, name);
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                CoopLog.Msg(string.Format(UIStrings.CompatDump, patched.Count, path));
                return path;
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                return null;
            }
        }

        private static string Sanitize(string tag)
        {
            StringBuilder sb = new StringBuilder(tag.Length);
            for (int i = 0; i < tag.Length; i++)
            {
                char c = tag[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') || c == '-' || c == '_') sb.Append(c);
            }
            return sb.ToString();
        }

        // นิยามเดิม คงไว้ (patchcount-audit.md 2026-10-31 — รอบ 6 งาน 2): นับ "จำนวน METHOD ที่มี patch
        // ของ owner อย่างน้อย 1 ตัว" — ไม่ใช่จำนวน patch objects (เป้า 1 method ที่มี
        // prefix+postfix+finalizer นับได้ 1 ไม่ใช่ 3) ⇒ เหมาะเฉพาะเช็ค "== 0" (leftover/หลังถอน)
        // ห้ามใช้ตรวจ "ติดครบชุด" — จับ partial-install ไม่ได้ (ตัวอย่างจริง: TimeV1 false-fail 2 vs 6 ของ TC1-r1)
        public static int CountOwnedPatches(string owner)
        {
            int n = 0;
            try
            {
                foreach (MethodBase m in HarmonyLib.Harmony.GetAllPatchedMethods())
                {
                    Patches info = HarmonyLib.Harmony.GetPatchInfo(m);
                    if (info == null) continue;
                    if (info.Owners != null && info.Owners.Contains(owner)) n++;
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
            return n;
        }

        /// <summary>
        /// นับ patch ENTRIES (prefix/postfix/finalizer/transpiler ที่ owner เป็นเจ้าของ) รวมทุก method
        /// — ตรงนิยาม "จำนวน patch-methods" ที่ค่าคาดของแต่ละ probe ใช้ (patchcount-audit.md ข้อ 1)
        /// </summary>
        public static int CountOwnedPatchEntries(string owner)
        {
            int n = 0;
            try
            {
                foreach (MethodBase m in HarmonyLib.Harmony.GetAllPatchedMethods())
                {
                    Patches info = HarmonyLib.Harmony.GetPatchInfo(m);
                    if (info == null) continue;
                    n += OwnedEntryCount(info, owner);
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
            return n;
        }

        /// <summary>
        /// เก็บ "ชุดจริง" ของ owner: key ต่อ method (DescribeKey) → จำนวน patch-entries ของ owner
        /// ใน method นั้น — ใช้คู่กับ ComparePatchSets เพื่อตรวจ "ติดครบชุด"
        /// </summary>
        public static Dictionary<string, int> CollectOwnedPatchSet(string owner)
        {
            Dictionary<string, int> set = new Dictionary<string, int>(StringComparer.Ordinal);
            try
            {
                foreach (MethodBase m in HarmonyLib.Harmony.GetAllPatchedMethods())
                {
                    Patches info = HarmonyLib.Harmony.GetPatchInfo(m);
                    if (info == null) continue;
                    int n = OwnedEntryCount(info, owner);
                    if (n > 0) set[DescribeKey(m)] = n;
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
            return set;
        }

        private static int OwnedEntryCount(Patches info, string owner)
        {
            int n = CountOwnedEntries(info.Prefixes, owner);
            n += CountOwnedEntries(info.Postfixes, owner);
            n += CountOwnedEntries(info.Finalizers, owner);
            n += CountOwnedEntries(info.Transpilers, owner);
            return n;
        }

        private static int CountOwnedEntries(IEnumerable<Patch> patches, string owner)
        {
            if (patches == null) return 0;
            int n = 0;
            foreach (Patch p in patches)
            {
                // 0Harmony 2.10.2: Patch เปิดเจ้าของเป็น field `owner` (ตัวเล็ก)
                if (p != null && string.Equals(p.owner, owner, StringComparison.Ordinal)) n++;
            }
            return n;
        }

        // ---------------- pure (signature BCL เท่านั้น — เทสต์ผ่าน reflection บน artifact ได้โดยไม่ต้องโหลด 0Harmony) ----------------

        /// <summary>
        /// key ของ method สำหรับเทียบชุด — รวม signature เพราะเกมมี overload ชื่อเดียวหลายตัว
        /// (เช่น GenerateHeroData ×4 overload ที่ Sp5T0 patch)
        /// </summary>
        public static string DescribeKey(MethodBase m)
        {
            string type = "?";
            try { type = (m.DeclaringType == null || m.DeclaringType.FullName == null) ? "?" : m.DeclaringType.FullName; }
            catch { }
            StringBuilder sb = new StringBuilder(type.Length + m.Name.Length + 24);
            sb.Append(type).Append("::").Append(m.Name).Append('(');
            ParameterInfo[] ps;
            try { ps = m.GetParameters(); }
            catch { ps = new ParameterInfo[0]; }
            for (int i = 0; i < ps.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(ps[i].ParameterType.Name);
            }
            sb.Append(')');
            return sb.ToString();
        }

        /// <summary>สร้างชุดคาดจากเป้าหมายที่ Patch() ไป + จำนวน patch-entries ต่อเป้า (pure)</summary>
        public static Dictionary<string, int> BuildExpectedSet(MethodBase[] targets, int[] entries)
        {
            if (targets == null || entries == null || targets.Length != entries.Length)
                throw new ArgumentException("targets/entries null หรือยาวไม่เท่ากัน");
            Dictionary<string, int> set = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) throw new ArgumentException("targets[" + i + "] = null");
                if (entries[i] <= 0) throw new ArgumentException("entries[" + i + "] ต้อง > 0");
                set[DescribeKey(targets[i])] = entries[i];
            }
            return set;
        }

        /// <summary>
        /// เทียบชุดคาดกับชุดจริง (pure) — ตรงทั้งชุด (ไม่ขาด ไม่เกิน ไม่ผิดจำนวนต่อ method) = true;
        /// ไม่ตรง = false + report แสดงคาด/จริง/ส่วนต่าง (ผู้เรียกถอนทั้งชุด fail-closed ตามเดิม)
        /// </summary>
        public static bool ComparePatchSets(Dictionary<string, int> expected, Dictionary<string, int> actual, out string report)
        {
            if (expected == null) expected = new Dictionary<string, int>(StringComparer.Ordinal);
            if (actual == null) actual = new Dictionary<string, int>(StringComparer.Ordinal);

            int expEntries = 0, actEntries = 0;
            foreach (KeyValuePair<string, int> kv in expected) expEntries += kv.Value;
            foreach (KeyValuePair<string, int> kv in actual) actEntries += kv.Value;

            List<string> missing = new List<string>(), extra = new List<string>(), wrong = new List<string>();
            foreach (KeyValuePair<string, int> kv in expected)
            {
                if (!actual.TryGetValue(kv.Key, out int n)) missing.Add(kv.Key + " (คาด " + kv.Value + " entries)");
                else if (n != kv.Value) wrong.Add(kv.Key + " (คาด " + kv.Value + " จริง " + n + ")");
            }
            foreach (KeyValuePair<string, int> kv in actual)
            {
                if (!expected.ContainsKey(kv.Key)) extra.Add(kv.Key + " (จริง " + kv.Value + " entries)");
            }
            missing.Sort(StringComparer.Ordinal);
            extra.Sort(StringComparer.Ordinal);
            wrong.Sort(StringComparer.Ordinal);

            report = "คาด: " + expEntries + " patch-entries บน " + expected.Count + " method / จริง: "
                   + actEntries + " patch-entries บน " + actual.Count + " method";
            foreach (string s in missing) report += "\n  ขาด: " + s;
            foreach (string s in extra) report += "\n  เกิน: " + s;
            foreach (string s in wrong) report += "\n  จำนวนไม่ตรง: " + s;
            return missing.Count == 0 && extra.Count == 0 && wrong.Count == 0;
        }

        /// <summary>
        /// gate หลังติดของทุก probe (patchcount-audit.md ข้อ 2/3): เทียบชุด method เป้าหมายที่ติดจริง
        /// กับที่คาด + จำนวน entries ต่อ method — ไม่ตรง = false + report (คาด/จริง/ส่วนต่าง)
        /// </summary>
        public static bool OwnedPatchTargetsMatch(string owner, MethodBase[] expectedTargets, int[] expectedEntries, out string report)
        {
            report = null;
            Dictionary<string, int> expected;
            try
            {
                expected = BuildExpectedSet(expectedTargets, expectedEntries);
            }
            catch (ArgumentException e)
            {
                report = "build expected ล้มเหลว: " + e.Message;
                return false;
            }
            Dictionary<string, int> actual = CollectOwnedPatchSet(owner);
            return ComparePatchSets(expected, actual, out report);
        }

        private static string Key(MethodBase m)
        {
            return TypeName(m) + "::" + m.Name;
        }

        private static string TypeName(MethodBase m)
        {
            try { return m.DeclaringType == null ? "?" : m.DeclaringType.FullName; }
            catch { return "?"; }
        }

        private static int Count(System.Collections.Generic.IEnumerable<Patch> patches)
        {
            if (patches == null) return 0;
            int n = 0;
            foreach (Patch p in patches) n++;
            return n;
        }

        private static string Join(IEnumerable<string> owners)
        {
            if (owners == null) return "";
            return string.Join(",", owners);
        }
    }
}
