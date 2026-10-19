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
                sb.AppendLine("# compat-notes — Harmony patches present at startup");
                sb.AppendLine();
                sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("Process: " + Environment.ProcessId);
                sb.AppendLine("Total patched methods: " + patched.Count);
                sb.AppendLine();
                sb.AppendLine("| # | Declaring type | Method | Owners | Prefix | Postfix | Transpiler | Finalizer |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|");

                int i = 0;
                foreach (MethodBase m in patched)
                {
                    i++;
                    Patches info = HarmonyLib.Harmony.GetPatchInfo(m);
                    if (info == null) continue;
                    sb.AppendLine("| " + i + " | " + TypeName(m) + " | " + m.Name + " | " +
                                  Join(info.Owners) + " | " + Count(info.Prefixes) + " | " +
                                  Count(info.Postfixes) + " | " + Count(info.Transpilers) + " | " +
                                  Count(info.Finalizers) + " |");
                }

                sb.AppendLine();
                sb.AppendLine("## Notes");
                sb.AppendLine("- อ่านอย่างเดียว ไม่ได้แก้/ถอด patch ของ mod อื่น");
                sb.AppendLine("- ใช้ตรวจการชนกับ LongYinThai (โดยเฉพาะ AddInfoText และ setter ของ Text) ก่อนขึ้น S1");

                string path = Path.Combine(CoopPaths.UserDataDir, "compat-notes.md");
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                CoopLog.Msg("compat: " + patched.Count + " patched methods -> " + path);
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
