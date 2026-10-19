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
        public static void Dump()
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

                string path = Path.Combine(CoopPaths.UserDataDir, "compat-notes.md");
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                CoopLog.Msg(string.Format(UIStrings.CompatDump, patched.Count, path));
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
        }

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
