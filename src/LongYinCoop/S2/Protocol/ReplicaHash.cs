using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace LongYinCoop.S2.Protocol
{
    public struct ReplicaUnit
    {
        public string StableId;
        public int Team;
        public int Row;
        public int Column;
        public float Hp;
        public bool IsAlive;    // BattleUnit.IsAlive - the only trustworthy death flag [RUNTIME]
        // rev4 (2026-10-21, decision 2 = option ii): a COPY of IsAlive, kept only to keep the
        // hash spec unchanged. There is no second source in the game (the interop assembly has
        // no unitAlive member, and `spike status` reports SafeUnitAlive(u) => u.IsAlive).
        // It lags hp by <= 1 frame. Reconsider dropping this field at the next protocol
        // version change (noted in S2-plan.md).
        public bool UnitAlive;
        public bool Moved;
        public bool Attacked;
    }

    public struct ReplicaHeader
    {
        public string BattleKey;
        public int TurnIndex;
        public string ActiveUnitId;
        public bool Paused;
    }

    /// <summary>
    /// Canonical replica fingerprint (rev2 §13.2). Deterministic across unit
    /// ordering; sensitive to hp/isAlive/unitAlive/moved/attacked. Never used to
    /// decide gameplay. Spec is frozen: `UnitAlive` stays even though it is a copy
    /// of `IsAlive` (rev4 decision 2).
    /// </summary>
    public static class ReplicaHash
    {
        public const string Tag = "s2|2";

        public static string Compute(in ReplicaHeader header, IReadOnlyList<ReplicaUnit> units)
        {
            if (units == null) throw new ArgumentNullException(nameof(units));

            int[] order = new int[units.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            Array.Sort(order, (a, b) => string.CompareOrdinal(
                units[a].StableId ?? string.Empty, units[b].StableId ?? string.Empty));

            StringBuilder sb = new StringBuilder(64 + units.Count * 64);
            sb.Append(Tag).Append('\n')
              .Append(header.BattleKey ?? string.Empty).Append('|')
              .Append(header.TurnIndex.ToString(CultureInfo.InvariantCulture)).Append('|')
              .Append(header.ActiveUnitId ?? string.Empty).Append('|')
              .Append(header.Paused ? '1' : '0').Append('\n');

            foreach (int i in order)
            {
                ReplicaUnit u = units[i];
                sb.Append(u.StableId ?? string.Empty).Append('|')
                  .Append(u.Team.ToString(CultureInfo.InvariantCulture)).Append('|')
                  .Append(u.Row.ToString(CultureInfo.InvariantCulture)).Append('|')
                  .Append(u.Column.ToString(CultureInfo.InvariantCulture)).Append('|')
                  .Append(u.Hp.ToString("F3", CultureInfo.InvariantCulture)).Append('|')
                  .Append(u.IsAlive ? '1' : '0')
                  .Append(u.UnitAlive ? '1' : '0')
                  .Append(u.Moved ? '1' : '0')
                  .Append(u.Attacked ? '1' : '0')
                  .Append('\n');
            }

            return S2Auth.Hex(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
        }
    }
}
