using System;
using System.IO;
using System.Text;

namespace LongYinCoop
{
    /// <summary>เขียนผลวัดของ S0 ลง UserData/LongYinCoop/s0-result.json</summary>
    internal static class S0Report
    {
        public static void Write(int onUpdateThreadId, int probeThreadId, bool threadMatch,
                                 string unityVersion, string gameVersion, string pipeName)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                Field(sb, "pid", Environment.ProcessId.ToString(), false);
                Field(sb, "stage", CoopMod.Stage.ToString(), true);
                Field(sb, "modVersion", CoopMod.Version, true);
                Field(sb, "unityVersion", unityVersion, true);
                Field(sb, "gameVersion", gameVersion, true);
                Field(sb, "onUpdateThreadId", onUpdateThreadId.ToString(), true);
                Field(sb, "probeThreadId", probeThreadId.ToString(), true);
                Field(sb, "threadMatch", threadMatch ? "true" : "false", true);
                Field(sb, "probeHits", HarmonyProbe.Hits.ToString(), true);
                Field(sb, "probeInstalled", HarmonyProbe.Installed ? "true" : "false", true);
                Field(sb, "probeTarget", HarmonyProbe.Target == null ? "" : HarmonyProbe.Target.DeclaringType.FullName + "." + HarmonyProbe.Target.Name, true);
                Field(sb, "pipeName", pipeName, true);
                Field(sb, "logPath", CoopLog.LogPath, true);
                Field(sb, "writtenAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), true);
                sb.Append("}");

                string dir = CoopPaths.EnsureUserDataDir();
                string path = Path.Combine(dir, "s0-result.json");
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                CoopLog.Msg("s0-result written: " + path);
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
        }

        private static void Field(StringBuilder sb, string name, string value, bool comma)
        {
            if (comma) sb.Append(",");
            sb.Append("\"").Append(name).Append("\":\"").Append(Escape(value)).Append("\"");
        }

        private static string Escape(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
        }
    }
}
