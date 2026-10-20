using System;
using Il2Cpp;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LongYinCoop
{
    /// <summary>อ่านอย่างเดียว ผ่าน interop แบบมีชนิด — ไม่ใช้ raw offset</summary>
    internal static class SnapshotReader
    {
        public static string GetSceneName()
        {
            try
            {
                return SceneManager.GetActiveScene().name;
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                return null;
            }
        }

        public static HeroData GetPlayerHero()
        {
            try
            {
                BigMapController map = BigMapController.Instance;
                if (map == null) return null;

                GameObject army = map.playerArmy;
                if (army == null) return null;

                BigmapNpcController npc = army.GetComponent<BigmapNpcController>();
                if (npc == null) return null;

                return npc.heroData;
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                return null;
            }
        }

        public static PlayerPos GetPlayerPos()
        {
            try
            {
                HeroData hero = GetPlayerHero();
                if (hero == null) return null;

                BigMapPos pos = hero.bigMapPos;
                if (pos == null) return null;

                return new PlayerPos { x = pos.x, y = pos.y, heroID = hero.heroID, areaId = hero.atAreaID };
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                return null;
            }
        }
    }

    internal sealed class PlayerPos
    {
        public float x;
        public float y;
        public int heroID;
        public int areaId;
    }
}
