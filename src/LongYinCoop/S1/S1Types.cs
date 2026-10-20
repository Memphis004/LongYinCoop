using System.Collections.Generic;
using System.Text.Json;

namespace LongYinCoop
{
    internal static class S1Limits
    {
        // capacity คงที่ตาม STAGEPLAN §3 — ห้ามโตตอนรัน
        public const int LogCapacity = 4096;
        public const int BattleCapacity = 900;
        public const int MaxUnits = 64;
    }

    /// <summary>JSON ของ S1 ใช้ public field (S0 ใช้ anonymous object/ propriedade)</summary>
    internal static class S1Json
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IncludeFields = true
        };
    }

    internal struct LogEvent
    {
        public long seq;
        public long frame;
        public double tMs;
        public string source;
        public string text;
        public int infoCount;
        public int infoDelta;
        public double battleTime;
    }

    internal struct UnitState
    {
        public string stableId;
        public int heroId;
        public int side;
        public int teamIndex;
        public int index;
        public string name;
        public int row;
        public int column;
        public float hp;
        public float maxHp;
        public float realMaxHp;
        public bool alive;
        public bool autoFight;
        public bool playerControl;
        public bool moved;
        public bool attacked;
    }

    /// <summary>snapshot เต็มหนึ่งเฟรม — ถูกเขียนทับใน ring buffer (capacity คงที่)</summary>
    internal sealed class BattleFrame
    {
        public long seq;
        public long frame;
        public double tMs;
        public int battleType;
        public string battleTypeName;
        public int battleState;
        public string battleStateName;
        public int nowActiveState;
        public string nowActiveStateName;
        public bool battlePaused;
        public int turnIndex;
        public string activeId;
        public int gridW;
        public int gridH;
        public int unitCount;
        public double battleTime;
        public readonly UnitState[] units = new UnitState[S1Limits.MaxUnits];
    }

    internal sealed class Cell
    {
        public int x;
        public int y;
    }

    /// <summary>มุมมองยูนิตสำหรับ IPC/capture — มี cell{x,y} ตาม schema §3</summary>
    internal sealed class UnitView
    {
        public string stableId;
        public int heroId;
        public int side;
        public int teamIndex;
        public int index;
        public string name;
        public Cell cell;
        public int row;
        public int column;
        public float hp;
        public float maxHp;
        public float realMaxHp;
        public bool alive;
        public bool autoFight;
        public bool playerControl;
        public bool moved;
        public bool attacked;
    }

    /// <summary>มุมมองสำหรับ IPC/capture (ตัดช่องว่างของ units ที่ไม่ใช้)</summary>
    internal sealed class BattleView
    {
        public long seq;
        public long frame;
        public double tMs;
        public int battleType;
        public string battleTypeName;
        public int battleState;
        public string battleStateName;
        public int nowActiveState;
        public string nowActiveStateName;
        public string phase;
        public bool battlePaused;
        public int turnIndex;
        public string activeId;
        public UnitView nowActiveUnit;
        public int gridW;
        public int gridH;
        public int unitCount;
        public double battleTime;
        public List<UnitView> units;
    }

    internal sealed class WorldTimeView
    {
        public int year;
        public int month;
        public int day;
        public float hour;
        public int difficulty;
    }

    internal sealed class PartyPos
    {
        public float x;
        public float y;
        public int areaId;
    }

    internal sealed class NpcState
    {
        public string list;
        public int id;
        public int heroId;
        public string name;
        public float x;
        public float y;
        public float distance;
        public bool isFightIcon;
    }

    internal sealed class ResourceState
    {
        public int money;
        public float fame;
        public float badFame;
        public float forceContribution;
        public float contribution;
        public string force;
        public string heroNum;
        public string areaNum;
    }

    internal sealed class HudState
    {
        public string time;
        public string name;
        public string fightScore;
        public string money;
        public string fame;
        public string badFame;
        public string force;
        public string contribution;
        public string heroNum;
        public string areaNum;
    }

    internal sealed class WorldSample
    {
        public long t;
        public long frame;
        public string scene;
        public int year;
        public int month;
        public int day;
        public float hour;
        public int difficulty;
        public WorldTimeView worldTime;
        public PartyPos partyPos;
        public List<NpcState> npcs;
        public ResourceState resources;
        public HudState hud;
        public double collectMs;
    }

    /// <summary>
    /// record ที่เขียนลง world.ndjson ต่อเนื่อง — **ไม่มี npcs** (ต้นเหตุ 11.4 KB/record)
    /// snapshot เต็ม (มี npcs) ยังได้จาก IPC `snapshot world` และ sampler ในหน่วยความจำ
    /// </summary>
    internal sealed class WorldStream
    {
        public long t;
        public long frame;
        public string scene;
        public int year;
        public int month;
        public int day;
        public float hour;
        public int difficulty;
        public WorldTimeView worldTime;
        public PartyPos partyPos;
        public ResourceState resources;
        public HudState hud;
        public double collectMs;
    }

    internal sealed class BattleMarker
    {
        public string kind;
        public long t;
        public long frame;
        public int battleType;
        public string battleTypeName;
        public int battleState;
        public string battleStateName;
        public int unitsPlayer;
        public int unitsEnemy;
        public int turnIndex;
        public double battleTime;
    }

    /// <summary>ring ของ log — เขียนเฉพาะ main thread, อ่านผ่าน dispatcher เท่านั้น</summary>
    internal sealed class LogRing
    {
        private readonly LogEvent[] _items = new LogEvent[S1Limits.LogCapacity];
        private int _next;

        public int Count { get; private set; }
        public int Capacity { get { return _items.Length; } }

        public void Add(LogEvent e)
        {
            _items[_next] = e;
            _next = (_next + 1) % _items.Length;
            if (Count < _items.Length) Count++;
        }

        public List<LogEvent> Last(int n)
        {
            if (n < 0) n = 0;
            if (n > Count) n = Count;
            List<LogEvent> list = new List<LogEvent>(n);
            int start = (_next - n + _items.Length) % _items.Length;
            for (int i = 0; i < n; i++) list.Add(_items[(start + i) % _items.Length]);
            return list;
        }
    }

    /// <summary>ring ของ battle snapshot — 900 เฟรม, preallocate ทั้งหมด, ไม่มี allocation ต่อเฟรม</summary>
    internal sealed class BattleRing
    {
        private readonly BattleFrame[] _frames = new BattleFrame[S1Limits.BattleCapacity];
        private int _next;
        private int _count;

        public int Count { get { return _count; } }
        public int Capacity { get { return _frames.Length; } }

        public BattleFrame Next()
        {
            BattleFrame f = _frames[_next];
            if (f == null)
            {
                f = new BattleFrame();
                _frames[_next] = f;
            }
            _next = (_next + 1) % _frames.Length;
            if (_count < _frames.Length) _count++;
            return f;
        }
    }
}
