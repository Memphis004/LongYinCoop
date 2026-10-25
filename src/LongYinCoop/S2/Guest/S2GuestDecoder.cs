using System;
using System.Globalization;
using LongYinCoop.S2.Host;
using LongYinCoop.S2.Protocol;

namespace LongYinCoop.S2.Guest
{
    /// <summary>แถวสำหรับแสดงผล 1 ยูนิต (pure — ไม่มี IL2CPP/Unity ปน)</summary>
    public struct GuestUnitDisplay
    {
        public string StableId;
        public int Team;
        public int HeroId;
        public int Row;
        public int Column;
        /// <summary>"41/80" — ตัดทศนิยมตามที่ UI ของเกมแสดง (RUNTIME: ตัวเลขบนจอตัดทศนิยมทิ้ง)</summary>
        public string HpText;
        /// <summary>hp/maxHp clamp 0..1 — ใช้คุมความกว้างแถบ hp ของ overlay</summary>
        public float HpFrac;
        /// <summary>!IsAlive || hp &lt;= 0 — ธงตายสำหรับแสดงผล (ไม่ใช้ hero.dead)</summary>
        public bool Dead;
        public bool Moved;
        public bool Attacked;
        public bool PlayerControl;
        public bool UnitAlive;
    }

    /// <summary>snapshot สำหรับแสดงผล (pure) — แปลงจาก `S2Frame` ที่ decode แล้ว</summary>
    public sealed class GuestSnapshotDisplay
    {
        public uint Seq;
        public uint SessionId;
        public ulong HostApplyUs;
        public string ReplicaHash;
        public bool Resync;
        public uint ResyncFromSeq;
        public string BattleKey;
        public int TurnIndex;
        public string ActiveUnitId;
        public bool Paused;
        public int NowActiveState;
        public float BattleTime;
        /// <summary>Div-A ที่ตัว decode คำนวณเอง: hash ของ field ที่ได้ !== ที่ host แนบมา</summary>
        public bool ReplicaHashMatch;
        public GuestUnitDisplay[] Units = Array.Empty<GuestUnitDisplay>();
    }

    /// <summary>
    /// W1 (แผน S2a-4 §4): guest-side decoder — payload (หลัง reassembly) → display struct
    ///
    /// pure: ใช้ `S2FrameCodec.TryDecode` ของ host ตัวเดิม (wire เดียวกัน) แล้วแปลงเป็นโมเดล
    /// สำหรับ IMGUI เท่านั้น — **ไม่มี path เขียน state เกม** (spectator อ่านอย่างเดียวตาม §13.2)
    /// Div-A: คำนวณ hash ใหม่จาก field ที่ decode ได้ แล้วเทียบกับที่ host แนบมา
    /// </summary>
    public static class S2GuestDecoder
    {
    /// <summary>
    /// decode + สร้าง display struct · คืน false พร้อม error (fail-closed ทุกกรณี)
    /// `seq` มาจาก envelope ของ wire frame (`AssembledMessage.Seq`) — payload เองไม่มี seq ฝัง
    /// </summary>
    public static bool TryDecode(byte[] payload, uint seq, out GuestSnapshotDisplay display, out string error)
    {
            display = null;
            S2Frame frame;
            if (!S2FrameCodec.TryDecode(payload, out frame, out error)) return false;

            string computed = S2FrameCodec.ComputeReplicaHash(frame);
            bool match = string.Equals(computed, frame.ReplicaHash, StringComparison.Ordinal);

            GuestUnitDisplay[] units = new GuestUnitDisplay[frame.Units.Length];
            for (int i = 0; i < units.Length; i++)
            {
                S2Unit u = frame.Units[i];
                units[i] = new GuestUnitDisplay
                {
                    StableId = u.StableId,
                    Team = u.Team,
                    HeroId = u.HeroId,
                    Row = u.Row,
                    Column = u.Column,
                    HpText = HpText(u.Hp, u.MaxHp),
                    HpFrac = HpFrac(u.Hp, u.MaxHp),
                    Dead = !u.IsAlive || u.Hp <= 0f,
                    Moved = u.Moved,
                    Attacked = u.Attacked,
                    PlayerControl = u.PlayerControl,
                    UnitAlive = u.UnitAlive,
                };
            }
            Array.Sort(units, CompareDisplayOrder);

            display = new GuestSnapshotDisplay
            {
                Seq = seq,
                SessionId = frame.SessionId,
                HostApplyUs = frame.HostApplyUs,
                ReplicaHash = frame.ReplicaHash,
                Resync = frame.Resync,
                ResyncFromSeq = frame.ResyncFromSeq,
                BattleKey = frame.Header.BattleKey,
                TurnIndex = frame.Header.TurnIndex,
                ActiveUnitId = frame.Header.ActiveUnitId,
                Paused = frame.Header.Paused,
                NowActiveState = frame.Header.NowActiveState,
                BattleTime = frame.Header.BattleTime,
                ReplicaHashMatch = match,
                Units = units,
            };
            return true;
        }

        /// <summary>ลำดับแสดงผล deterministic: team แล้ว row แล้ว column (stableId กันชน)</summary>
        private static int CompareDisplayOrder(GuestUnitDisplay a, GuestUnitDisplay b)
        {
            if (a.Team != b.Team) return a.Team < b.Team ? -1 : 1;
            if (a.Row != b.Row) return a.Row < b.Row ? -1 : 1;
            if (a.Column != b.Column) return a.Column < b.Column ? -1 : 1;
            return string.CompareOrdinal(a.StableId ?? string.Empty, b.StableId ?? string.Empty);
        }

        /// <summary>"hp/maxHp" แบบตัดทศนิยม (4733.7 → "4733") — NaN/∞ = "0"</summary>
        public static string HpText(float hp, float maxHp)
        {
            return SafeInt(hp) + "/" + SafeInt(maxHp);
        }

        public static float HpFrac(float hp, float maxHp)
        {
            if (float.IsNaN(hp) || float.IsNaN(maxHp) || maxHp <= 0f) return 0f;
            float frac = hp / maxHp;
            if (float.IsNaN(frac) || frac < 0f) return 0f;
            if (frac > 1f) return 1f;
            return frac;
        }

        private static string SafeInt(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return "0";
            return ((int)value).ToString(CultureInfo.InvariantCulture);
        }
    }
}
