using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace LongYinCoop.S2.Host
{
    /// <summary>
    /// S2a-2 replica POCO — สร้างบน main thread แล้วส่งข้าม thread ได้
    /// (ห้ามมี type ของ IL2CPP/Unity อยู่ในไฟล์นี้)
    /// field ชุดนี้ตาม S2-plan §13.2 ข้อ 1: ไม่มี `alive` (hero.dead) เด็ดขาด
    /// </summary>
    public struct S2Unit
    {
        public string StableId;
        public int Team;
        public int HeroId;
        public int Row;
        public int Column;
        public float Hp;
        public float MaxHp;
        // หมายเหตุ S2a-2 (rev4): ฟิลด์ `unitAlive` ของเกม **ไม่มี** ใน interop assembly
        // (ตรวจ string heap ของ Assembly-CSharp.dll: มีแต่ get_IsAlive) จึงอ่านแบบมีชนิดไม่ได้
        // -> ไม่มี raw-offset read (กติกา 8); `UnitAlive` ข้างล่างเป็นสำเนาของ IsAlive เท่านั้น
        /// <summary>BattleUnit.IsAlive — ธงตายจริง (typed, ยืนยันใน TurnGate.cs)</summary>
        public bool IsAlive;
        /// <summary>
        /// rev4 (มติผู้ใช้ 2026-10-21 ข้อ 2 = ทาง (ii)): **สำเนาของ <see cref="IsAlive"/>** เท่านั้น
        /// (ดู <see cref="S2UnitRule.MirrorUnitAlive"/>) — ไม่มีแหล่งค่าที่สอง
        /// (interop assembly ไม่มี `BattleUnit.unitAlive`; `unitAlive` ที่ `spike status` รายงาน
        /// = `SafeUnitAlive(u) → u.IsAlive` ตัวเดียวกัน) · ล้าหลัง `hp` ได้ ≤1 เฟรม
        /// มีไว้เพื่อคงฟิลด์ `ReplicaHash.UnitAlive` ของสเปก (ห้ามแก้ hash spec) —
        /// พิจารณาตัดตอนเปลี่ยนเวอร์ชันโปรโตคอล (จดใน `S2-plan.md §13.2`)
        /// </summary>
        public bool UnitAlive;
        public bool Moved;
        public bool Attacked;
        public bool PlayerControl;
    }

    /// <summary>
    /// กติกาต่อหน่วยของ S2 (pure — เทสต์ได้โดยไม่ต้องมีเกม)
    /// </summary>
    public static class S2UnitRule
    {
        /// <summary>
        /// `UnitAlive` = **สำเนาของ `IsAlive`** เท่านั้น (rev4 มติ 2026-10-21 ข้อ 2 = (ii))
        /// — ห้ามเติมตรรกะอื่น (ห้าม `hero.dead`, ห้าม raw offset) เพราะสเปก hash ต้อง deterministic
        /// และเทียบกับ `unitAlive` ของ `spike status` ได้ (= `BattleUnit.IsAlive` ตัวเดียวกัน)
        /// </summary>
        public static bool MirrorUnitAlive(bool isAlive)
        {
            return isAlive;
        }
    }

    public struct S2Header
    {
        public string BattleKey;
        public int TurnIndex;
        public string ActiveUnitId;
        public bool Paused;
        public int NowActiveState;
        public float BattleTime;
        public int UnitCount;
    }

    /// <summary>audit tuple ตาม §13.2 ข้อ 3 — ใช้แทน stateHash</summary>
    public struct S2Audit
    {
        public float BattleTime;
        public int TurnIndex;
        public int NowActiveState;
        public int UnitCount;
        public string UnitsSum;

        public bool SameAs(in S2Audit other)
        {
            return BattleTime.Equals(other.BattleTime)
                   && TurnIndex == other.TurnIndex
                   && NowActiveState == other.NowActiveState
                   && UnitCount == other.UnitCount
                   && string.Equals(UnitsSum, other.UnitsSum, StringComparison.Ordinal);
        }

        public string ToText()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "battleTime={0:F3} turnIndex={1} nowActiveState={2} unitCount={3} unitsSum={4}",
                BattleTime, TurnIndex, NowActiveState, UnitCount, UnitsSum);
        }
    }

    public sealed class S2Frame
    {
        public long FrameId;
        public S2Header Header;
        public S2Unit[] Units = Array.Empty<S2Unit>();
        public S2Audit Audit;
        public double SampleMs;
        public double SerializeMs;
        public int PayloadBytes;
    }

    public static class S2AuditUtil
    {
        public static S2Audit Compute(in S2Header header, IReadOnlyList<S2Unit> units)
        {
            int count = units == null ? 0 : units.Count;
            int[] order = new int[count];
            for (int i = 0; i < count; i++) order[i] = i;
            Array.Sort(order, delegate (int a, int b)
            {
                return string.CompareOrdinal(units[a].StableId ?? string.Empty,
                                             units[b].StableId ?? string.Empty);
            });

            StringBuilder sb = new StringBuilder(32 + count * 40);
            for (int i = 0; i < order.Length; i++)
            {
                S2Unit u = units[order[i]];
                sb.Append(u.StableId ?? string.Empty).Append('|')
                  .Append(u.Hp.ToString("F3", CultureInfo.InvariantCulture)).Append('|')
                  .Append(u.IsAlive ? '1' : '0').Append('|')
                  .Append(u.Row.ToString(CultureInfo.InvariantCulture)).Append('|')
                  .Append(u.Column.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }

            S2Audit audit = new S2Audit
            {
                BattleTime = header.BattleTime,
                TurnIndex = header.TurnIndex,
                NowActiveState = header.NowActiveState,
                UnitCount = header.UnitCount,
                UnitsSum = Hex(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()))),
            };
            return audit;
        }

        private static string Hex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }

    /// <summary>บรรทัด NDJSON สำหรับ evidence (`s2-snapshots.ndjson`) — เขียนมือนับ ไม่พึ่ง JSON lib</summary>
    public static class S2Ndjson
    {
        public static string ToLine(in S2Frame frame)
        {
            StringBuilder sb = new StringBuilder(256 + frame.Units.Length * 96);
            sb.Append("{\"frameId\":").Append(frame.FrameId.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"seq\":").Append(frame.FrameId.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"battleKey\":").Append(Quote(frame.Header.BattleKey));
            sb.Append(",\"turnIndex\":").Append(frame.Header.TurnIndex.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"activeUnitId\":").Append(Quote(frame.Header.ActiveUnitId));
            sb.Append(",\"paused\":").Append(frame.Header.Paused ? "true" : "false");
            sb.Append(",\"nowActiveState\":").Append(frame.Header.NowActiveState.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"battleTime\":").Append(frame.Header.BattleTime.ToString("F3", CultureInfo.InvariantCulture));
            sb.Append(",\"unitCount\":").Append(frame.Header.UnitCount.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"sampleMs\":").Append(frame.SampleMs.ToString("F4", CultureInfo.InvariantCulture));
            sb.Append(",\"serializeMs\":").Append(frame.SerializeMs.ToString("F4", CultureInfo.InvariantCulture));
            sb.Append(",\"payloadBytes\":").Append(frame.PayloadBytes.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"audit\":{\"battleTime\":").Append(frame.Audit.BattleTime.ToString("F3", CultureInfo.InvariantCulture))
              .Append(",\"turnIndex\":").Append(frame.Audit.TurnIndex.ToString(CultureInfo.InvariantCulture))
              .Append(",\"nowActiveState\":").Append(frame.Audit.NowActiveState.ToString(CultureInfo.InvariantCulture))
              .Append(",\"unitCount\":").Append(frame.Audit.UnitCount.ToString(CultureInfo.InvariantCulture))
              .Append(",\"unitsSum\":").Append(Quote(frame.Audit.UnitsSum)).Append('}');
            sb.Append(",\"units\":[");
            for (int i = 0; i < frame.Units.Length; i++)
            {
                S2Unit u = frame.Units[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":").Append(Quote(u.StableId))
                  .Append(",\"team\":").Append(u.Team.ToString(CultureInfo.InvariantCulture))
                  .Append(",\"heroId\":").Append(u.HeroId.ToString(CultureInfo.InvariantCulture))
                  .Append(",\"row\":").Append(u.Row.ToString(CultureInfo.InvariantCulture))
                  .Append(",\"col\":").Append(u.Column.ToString(CultureInfo.InvariantCulture))
                  .Append(",\"hp\":").Append(u.Hp.ToString("F3", CultureInfo.InvariantCulture))
                  .Append(",\"maxHp\":").Append(u.MaxHp.ToString("F3", CultureInfo.InvariantCulture))
                  .Append(",\"isAlive\":").Append(u.IsAlive ? "true" : "false")
                  .Append(",\"unitAlive\":").Append(u.UnitAlive ? "true" : "false")
                  .Append(",\"moved\":").Append(u.Moved ? "true" : "false")
                  .Append(",\"attacked\":").Append(u.Attacked ? "true" : "false")
                  .Append(",\"playerControl\":").Append(u.PlayerControl ? "true" : "false")
                  .Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private static string Quote(string value)
        {
            if (value == null) return "null";
            StringBuilder sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
