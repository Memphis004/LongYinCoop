using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Il2Cpp;

namespace LongYinCoop
{
    /// <summary>
    /// probe อ่านอย่างเดียวสำหรับ S1-a:
    /// - ยืนยันชื่อ type/field ที่ Il2CppInterop สร้างจริงตอนรัน (ไม่ใช่เดาจาก dump)
    /// - ตรวจ patch ที่ชนกับ mod อื่นหลังเข้า BigMap / เข้าศึก
    /// - อ่านหน่วยในศึก + Text บน UI เพื่อเทียบกับ snapshot
    /// </summary>
    internal static class S1Probe
    {
        private static readonly Type[] Types =
        {
            typeof(BattleController),
            typeof(BattleUnit),
            typeof(BattleTeam),
            typeof(GridUnitData),
            typeof(BattleMapData),
            typeof(WorldData),
            typeof(TimeData),
            typeof(HeroData),
            typeof(HudController),
            typeof(GameController),
            typeof(BigMapController),
            typeof(BigmapNpcController)
        };

        public static object TypeReport()
        {
            List<object> types = new List<object>();
            for (int i = 0; i < Types.Length; i++)
            {
                Type t = Types[i];
                List<string> fields = new List<string>();
                FieldInfo[] fs = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                             BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                for (int k = 0; k < fs.Length; k++)
                {
                    fields.Add(fs[k].Name + ":" + fs[k].FieldType.Name + (fs[k].IsStatic ? " static" : ""));
                }

                List<string> props = new List<string>();
                PropertyInfo[] ps = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic |
                                                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                for (int k = 0; k < ps.Length; k++)
                {
                    bool isStatic = ps[k].GetMethod != null && ps[k].GetMethod.IsStatic;
                    props.Add(ps[k].Name + ":" + ps[k].PropertyType.Name + (isStatic ? " static" : ""));
                }

                List<string> methods = new List<string>();
                MethodInfo[] ms = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                               BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                for (int k = 0; k < ms.Length; k++)
                {
                    if (ms[k].Name == "AddInfoText" || ms[k].Name == "Player" || ms[k].Name == "get_Instance")
                    {
                        methods.Add(Describe(ms[k]));
                    }
                }

                int realFields = 0;
                for (int k = 0; k < fs.Length; k++) if (!fs[k].Name.StartsWith("Native", StringComparison.Ordinal)) realFields++;

                types.Add(new
                {
                    name = t.FullName,
                    fieldCount = fs.Length,
                    interopRealFields = realFields,
                    fields = fields,
                    properties = props,
                    selectedMethods = methods
                });
            }

            PropertyInfo infoCount = typeof(BattleController).GetProperty("infoCount",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo inited = typeof(BattleController).GetProperty("inited",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            return new
            {
                types = types,
                infoCountProperty = infoCount == null ? null : infoCount.Name + ":" + infoCount.PropertyType.Name,
                initedProperty = inited == null ? null : inited.Name + ":" + inited.PropertyType.Name
            };
        }

        public static object PatchReport()
        {
            List<object> rows = new List<object>();
            try
            {
                foreach (MethodBase m in HarmonyLib.Harmony.GetAllPatchedMethods())
                {
                    string typeName = m.DeclaringType == null ? "?" : m.DeclaringType.FullName;
                    bool interesting = m.Name.IndexOf("AddInfoText", StringComparison.Ordinal) >= 0 ||
                                       typeName == "UnityEngine.UI.Text" ||
                                       m.Name == "set_text";
                    if (!interesting) continue;

                    Patches info = HarmonyLib.Harmony.GetPatchInfo(m);
                    rows.Add(new
                    {
                        method = typeName + "." + m.Name,
                        owners = info == null || info.Owners == null ? "" : string.Join(",", info.Owners),
                        prefixes = Count(info == null ? null : info.Prefixes),
                        postfixes = Count(info == null ? null : info.Postfixes),
                        priority = MaxPriority(info == null ? null : info.Postfixes)
                    });
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
            return new { count = rows.Count, rows = rows };
        }

        public static object UnitProbe()
        {
            BattleController bc = BattleController.Instance;
            if (bc == null) return null;

            List<object> units = new List<object>();
            Il2CppSystem.Collections.Generic.List<BattleTeam> teams = bc.teams;
            if (teams != null)
            {
                for (int t = 0; t < teams.Count; t++)
                {
                    BattleTeam team = teams[t];
                    if (team == null || team.battleUnits == null) continue;

                    for (int i = 0; i < team.battleUnits.Count; i++)
                    {
                        BattleUnit u = team.battleUnits[i];
                        if (u == null) continue;

                        HeroData hero = u.heroData;
                        GridUnitData grid = u.mapGrid;

                        List<string> texts = new List<string>();
                        try
                        {
                            UnityEngine.UI.Text[] arr = u.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                            for (int k = 0; k < arr.Length && k < 12; k++)
                            {
                                if (arr[k] != null) texts.Add(arr[k].text);
                            }
                        }
                        catch (Exception e)
                        {
                            CoopLog.Error(e);
                        }

                        units.Add(new
                        {
                            side = team.ID,
                            index = i,
                            heroId = hero == null ? -1 : hero.heroID,
                            name = hero == null ? null : hero.heroName,
                            row = grid == null ? -1 : grid.row,
                            column = grid == null ? -1 : grid.column,
                            hp = hero == null ? 0 : hero.hp,
                            maxHp = hero == null ? 0 : hero.maxhp,
                            alive = hero != null && !hero.dead,
                            autoFight = u.autoFight,
                            playerControl = u.playerControl,
                            texts = texts
                        });
                    }
                }
            }

            BattleUnit active = bc.nowActiveUnit;
            return new
            {
                battleType = bc.battleType.ToString(),
                battleState = bc.battleState.ToString(),
                nowActiveState = bc.nowActiveState.ToString(),
                battlePaused = bc.battlePaused,
                infoCount = bc.infoCount,
                activeHeroId = active == null || active.heroData == null ? -1 : active.heroData.heroID,
                infoText = InfoText(bc),
                units = units
            };
        }

        public static object InfoText(BattleController bc)
        {
            try
            {
                UnityEngine.GameObject go = bc.infoText;
                if (go == null) return null;

                List<string> components = new List<string>();
                UnityEngine.Component[] comps = go.GetComponents<UnityEngine.Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i] != null) components.Add(comps[i].GetType().Name);
                }

                string text = null;
                UnityEngine.UI.Text uiText = go.GetComponent<UnityEngine.UI.Text>();
                if (uiText != null) text = uiText.text;

                return new { components = components, text = text };
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                return null;
            }
        }

        public static string Describe(MethodInfo m)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m.Name).Append('(');
            ParameterInfo[] ps = m.GetParameters();
            for (int i = 0; i < ps.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(ps[i].ParameterType.Name);
            }
            sb.Append(')');
            return sb.ToString();
        }

        private static int Count(IEnumerable<Patch> patches)
        {
            if (patches == null) return 0;
            int n = 0;
            foreach (Patch p in patches) n++;
            return n;
        }

        private static int MaxPriority(IEnumerable<Patch> patches)
        {
            if (patches == null) return 0;
            int max = int.MinValue;
            foreach (Patch p in patches) if (p.priority > max) max = p.priority;
            return max == int.MinValue ? 0 : max;
        }
    }
}
