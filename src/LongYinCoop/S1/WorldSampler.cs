using System;
using System.Collections.Generic;
using System.Diagnostics;
using Il2Cpp;
using UnityEngine;

namespace LongYinCoop
{
    /// <summary>
    /// อ่าน world state ผ่าน Il2CppInterop แบบมีชนิดเท่านั้น (ไม่มี raw offset)
    /// sampler ทำงานบน main thread (MelonEvents.OnUpdate) และไม่เรียก method ที่เปลี่ยน state
    /// </summary>
    internal static class WorldSampler
    {
        public const int DefaultIntervalMs = 100;

        private static WorldSample _latest;
        private static long _lastSampleTicks;
        private static long _lastStreamTicks;
        private static long _latestStampTicks;

        public static WorldSample Latest { get { return _latest; } }

        public static long LatestAgeMs
        {
            get
            {
                if (_latestStampTicks == 0) return -1;
                return (long)((Stopwatch.GetTimestamp() - _latestStampTicks) * 1000.0 / Stopwatch.Frequency);
            }
        }

        public static void Tick(int intervalMs, int streamIntervalMs)
        {
            if (intervalMs < 16) intervalMs = 16;
            if (streamIntervalMs < 100) streamIntervalMs = 100;
            long now = Stopwatch.GetTimestamp();
            if (_lastSampleTicks != 0 &&
                (now - _lastSampleTicks) * 1000.0 / Stopwatch.Frequency < intervalMs) return;
            _lastSampleTicks = now;

            try
            {
                WorldSample s = Collect();
                _latest = s;
                _latestStampTicks = Stopwatch.GetTimestamp();

                // stream ลงดิสก์ช้ากว่า sampling + ไม่มี npcs (ดู WorldStream)
                if (CaptureWriter.WantsCapture &&
                    (_lastStreamTicks == 0 ||
                     (now - _lastStreamTicks) * 1000.0 / Stopwatch.Frequency >= streamIntervalMs))
                {
                    _lastStreamTicks = now;
                    CaptureWriter.Record("world", ToStream(s));
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }
        }

        private static WorldStream ToStream(WorldSample s)
        {
            WorldStream w = new WorldStream();
            w.t = s.t;
            w.frame = s.frame;
            w.scene = s.scene;
            w.year = s.year;
            w.month = s.month;
            w.day = s.day;
            w.hour = s.hour;
            w.difficulty = s.difficulty;
            w.worldTime = s.worldTime;
            w.partyPos = s.partyPos;
            w.resources = s.resources;
            w.hud = s.hud;
            w.collectMs = s.collectMs;
            return w;
        }

        private static WorldSample Collect()
        {
            long t0 = Stopwatch.GetTimestamp();
            WorldSample s = new WorldSample();
            s.t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            s.frame = Time.frameCount;
            s.scene = SnapshotReader.GetSceneName();
            s.npcs = new List<NpcState>();

            GameController gc = GameController.Instance;
            if (gc != null)
            {
                WorldData world = gc.worldData;
                if (world != null)
                {
                    TimeData wt = world.worldTime;
                    if (wt != null)
                    {
                        s.year = wt.year;
                        s.month = wt.month;
                        s.day = wt.day;
                    }
                    s.hour = world.hour;
                    s.difficulty = world.gameDifficulty;
                    s.worldTime = new WorldTimeView
                    {
                        year = s.year,
                        month = s.month,
                        day = s.day,
                        hour = s.hour,
                        difficulty = s.difficulty
                    };
                }
            }

            PlayerPos p = SnapshotReader.GetPlayerPos();
            if (p != null)
            {
                s.partyPos = new PartyPos { x = p.x, y = p.y, areaId = p.areaId };
            }

            HeroData hero = SnapshotReader.GetPlayerHero();
            ResourceState res = new ResourceState();
            if (hero != null)
            {
                ItemListData items = hero.itemListData;
                res.money = items == null ? 0 : items.money;
                res.fame = hero.fame;
                res.badFame = hero.badFame;
                res.forceContribution = hero.forceContribution;
                res.contribution = hero.forceContribution;
            }

            HudController hud = HudController.Instance;
            HudState hs = new HudState();
            if (hud != null)
            {
                hs.time = TextOf(hud.timeLabel);
                hs.name = TextOf(hud.nameLabel);
                hs.fightScore = TextOf(hud.fightScoreLabel);
                hs.money = TextOf(hud.moneyLabel);
                hs.fame = TextOf(hud.fameLabel);
                hs.badFame = TextOf(hud.badfameLabel);
                hs.force = TextOf(hud.forceLabel);
                hs.contribution = TextOf(hud.contributionLabel);
                hs.heroNum = TextOf(hud.heroNumLabel);
                hs.areaNum = TextOf(hud.areaNumLabel);

                res.force = hs.force;
                res.heroNum = hs.heroNum;
                res.areaNum = hs.areaNum;
            }
            s.resources = res;
            s.hud = hs;

            CollectNpcs(s, p);
            s.collectMs = Math.Round((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency, 4);
            return s;
        }

        private static void CollectNpcs(WorldSample s, PlayerPos player)
        {
            BigMapController map = BigMapController.Instance;
            if (map == null) return;

            AddNpcList(s, player, map.bigmapNormalNpcIcons, "normal");
            AddNpcList(s, player, map.bigmapTempNpcIcons, "temp");
        }

        private static void AddNpcList(WorldSample s, PlayerPos player,
                                       Il2CppSystem.Collections.Generic.List<GameObject> icons, string listName)
        {
            if (icons == null) return;
            for (int i = 0; i < icons.Count; i++)
            {
                try
                {
                    GameObject go = icons[i];
                    if (go == null) continue;

                    BigmapNpcController npc = go.GetComponent<BigmapNpcController>();
                    if (npc == null) continue;

                    HeroData hero = npc.heroData;
                    if (hero == null) continue;

                    BigMapPos pos = hero.bigMapPos;
                    if (pos == null) continue;

                    NpcState n = new NpcState();
                    n.list = listName;
                    n.id = hero.heroID;
                    n.heroId = hero.heroID;
                    n.name = hero.heroName;
                    n.x = pos.x;
                    n.y = pos.y;
                    if (player != null)
                    {
                        float dx = pos.x - player.x;
                        float dy = pos.y - player.y;
                        n.distance = Mathf.Sqrt(dx * dx + dy * dy);
                    }

                    GameObject fightIcon = npc.BigMapFightIcon;
                    n.isFightIcon = fightIcon != null && fightIcon.activeInHierarchy;
                    s.npcs.Add(n);
                }
                catch (Exception e)
                {
                    CoopLog.Error(e);
                }
            }
        }

        private static string TextOf(UnityEngine.UI.Text text)
        {
            if (text == null) return null;
            try { return text.text; }
            catch { return null; }
        }
    }
}
