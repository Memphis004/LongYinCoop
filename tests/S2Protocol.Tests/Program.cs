using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LongYinCoop.S2.Protocol;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// S2a-0 (T0): protocol codec tests. No game, no sockets, no mod runtime.
    /// Exit code 0 = all pass.
    /// </summary>
    internal static class Program
    {
        private static int _fail;

        private static void Check(string name, bool ok)
        {
            Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {name}");
            if (!ok) _fail++;
        }

        private static void Throws<T>(string name, Action action) where T : Exception
        {
            try
            {
                action();
                Check(name, false);
            }
            catch (T)
            {
                Check(name, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      unexpected {ex.GetType().Name}: {ex.Message}");
                Check(name, false);
            }
        }

        private static int Main(string[] args)
        {
            // ---- CLI ช่วยงาน C1 (ไม่แตะเกม ไม่มี mod runtime) -------------------
            // --emit-vectors <path>                       เขียน fixtures/replica-vectors.ndjson
            // --fake-host <port> <sec> [secretHex] [hz] [skipSeqAt]   host จำลอง (I9/Python)
            // --emit-v2a-fixture <path>                   NDJSON จำลอง V2-A (ตรวจ time_v2a_analyze.py จริง)
            if (args != null && args.Length > 0)
            {
                if (args[0] == "--emit-vectors" && args.Length >= 2)
                {
                    string written = S2a3Tests.EmitVectors(args[1]);
                    Console.WriteLine("VECTORS written " + written);
                    return 0;
                }
                if (args[0] == "--fake-host")
                {
                    return FakeS2Host.RunCli(args.Skip(1).ToArray());
                }
                if (args[0] == "--emit-v2a-fixture" && args.Length >= 2)
                {
                    string written = TimeV2aTests.EmitFixture(args[1]);
                    Console.WriteLine("V2A FIXTURE written " + written);
                    return 0;
                }
                Console.WriteLine("unknown args: " + string.Join(" ", args));
                return 2;
            }

            FrameTests();
            ChunkTests();
            CompactTests();
            AuthTests();
            HashTests();
            AllowlistTests();

            _fail += S2a2Tests.Run();
            _fail += S2a3Tests.Run();
            _fail += S2a4Tests.Run();
            _fail += TimeV2aTests.Run();
            _fail += Sp5T0LinkageHarness.Run();

            Console.WriteLine();
            Console.WriteLine(_fail == 0 ? "RESULT PASS (all T0/T1 checks)" : $"RESULT FAIL ({_fail} checks failed)");
            return _fail == 0 ? 0 : 1;
        }

        private static void FrameTests()
        {
            byte[] payload = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
            byte[] frame = FrameCodec.Encode(MessageType.BattleEvent, S2Protocol.ProtoVer, 0x01020304, payload);
            Check("frame size == 8 + payload", frame.Length == 108);
            Check("frame first bytes are big-endian length",
                  frame[0] == 0 && frame[1] == 106);
            Check("frame decodes", FrameCodec.TryDecode(frame, out FrameHeader h, out byte[] back));
            Check("frame type/ver/seq survive", h.Type == MessageType.BattleEvent
                                                && h.Ver == S2Protocol.ProtoVer
                                                && h.Seq == 0x01020304);
            Check("payload survives", back.SequenceEqual(payload));
            Check("NextFrameOffset == frame length", FrameCodec.NextFrameOffset(frame) == frame.Length);

            byte[] maxPayload = new byte[S2Protocol.MaxPayloadBytes];
            byte[] maxFrame = FrameCodec.Encode(MessageType.Snapshot, 1, 1, maxPayload);
            Check("max payload frame == 1100 B", maxFrame.Length == S2Protocol.MaxFrameBytes);
            Throws<ArgumentException>("payload over cap throws",
                () => FrameCodec.Encode(MessageType.Snapshot, 1, 1, new byte[S2Protocol.MaxPayloadBytes + 1]));

            Check("truncated header rejected", !FrameCodec.TryReadHeader(frame.AsSpan(0, 7), out _));
            Check("truncated payload rejected", !FrameCodec.TryReadHeader(frame.AsSpan(0, 60), out _));
            Check("bodyLen < 6 rejected",
                  !FrameCodec.TryReadHeader(new byte[] { 0, 5, 1, 1, 0, 0, 0, 1 }, out _));
            byte[] oversize = new byte[8];
            oversize[0] = 0x10;
            oversize[1] = 0x00; // 4096 body
            oversize[2] = (byte)MessageType.World;
            Check("oversize frame header rejected", !FrameCodec.TryReadHeader(oversize, out _));
            byte[] unknown = FrameCodec.Encode(MessageType.World, 1, 1, Array.Empty<byte>());
            unknown[2] = 0x7F;
            Check("unknown type byte rejected", !FrameCodec.TryReadHeader(unknown, out _));
        }

        private static void ChunkTests()
        {
            byte[] payload = new byte[5000];
            new Random(7).NextBytes(payload);

            List<byte[]> frames = ChunkCodec.Encode(42, MessageType.Snapshot, 9, 77, payload);
            Check("5000 B payload is chunked (>1 frame)", frames.Count > 1);
            Check("every chunk frame <= 1100 B", frames.All(f => f.Length <= S2Protocol.MaxFrameBytes));

            ChunkReassembler ra = new ChunkReassembler();
            AssembledMessage done = default;
            bool got = false;
            foreach (byte[] f in frames)
            {
                FrameCodec.TryDecode(f, out FrameHeader fh, out byte[] body);
                got = ra.Accept(fh, body, 0, out done);
            }
            Check("chunks reassemble", got);
            Check("reassembled type/seq", done.Type == MessageType.Snapshot && done.Seq == 77);
            Check("reassembled payload identical", done.Payload.SequenceEqual(payload));
            Check("no pending left", ra.PendingCount == 0);

            // out of order
            ChunkReassembler ro = new ChunkReassembler();
            bool gotOrder = false;
            AssembledMessage ord = default;
            foreach (byte[] f in frames.AsEnumerable().Reverse())
            {
                FrameCodec.TryDecode(f, out FrameHeader fh, out byte[] body);
                gotOrder = ro.Accept(fh, body, 0, out ord);
            }
            Check("out-of-order reassembly", gotOrder && ord.Payload.SequenceEqual(payload));

            // duplicate index drops the message
            ChunkReassembler rd = new ChunkReassembler();
            List<byte[]> dup = new List<byte[]> { frames[0], frames[0] };
            bool dupDone = false;
            foreach (byte[] f in dup)
            {
                FrameCodec.TryDecode(f, out FrameHeader fh, out byte[] body);
                dupDone = rd.Accept(fh, body, 0, out _);
            }
            Check("duplicate chunk index dropped", !dupDone && rd.PendingCount == 0);

            // timeout purge
            ChunkReassembler rt = new ChunkReassembler();
            FrameCodec.TryDecode(frames[0], out FrameHeader th, out byte[] tb);
            rt.Accept(th, tb, 0, out _);
            Check("pending before timeout", rt.PendingCount == 1);
            rt.PurgeExpired(S2Protocol.ReassemblyTimeoutMs + 1);
            Check("stale pending purged", rt.PendingCount == 0);

            // total == 0 rejected
            byte[] badBody = new byte[7];
            badBody[6] = (byte)MessageType.World;
            FrameCodec.TryDecode(FrameCodec.Encode(MessageType.Chunk, 1, 1, badBody),
                                 out FrameHeader bh, out byte[] bb);
            Check("chunk with total=0 rejected", !new ChunkReassembler().Accept(bh, bb, 0, out _));

            // nested chunk refused
            Throws<ArgumentException>("chunk of chunk throws",
                () => ChunkCodec.Encode(1, MessageType.Chunk, 1, 1, new byte[2000]));

            // small message stays a single frame
            List<byte[]> small = ChunkCodec.Encode(5, MessageType.BattleLog, 1, 3, new byte[16]);
            Check("small message is one frame", small.Count == 1);
        }

        private static void CompactTests()
        {
            uint[] values = { 0, 1, 127, 128, 16383, 16384, 2097151, uint.MaxValue };
            CompactWriter w = new CompactWriter();
            foreach (uint v in values) w.WriteVarUInt(v);
            w.WriteFloat(1.5f);
            w.WriteBool(true);
            w.WriteString("หลิน");
            w.WriteBytes(new byte[] { 1, 2, 3 });

            CompactReader r = new CompactReader(w.ToArray());
            bool ok = values.All(v => r.ReadVarUInt() == v);
            Check("varint u32 boundaries round-trip", ok);
            Check("float round-trip", Math.Abs(r.ReadFloat() - 1.5f) < 1e-6);
            Check("bool round-trip", r.ReadBool());
            Check("utf8 string round-trip", r.ReadString() == "หลิน");
            Check("bytes round-trip", r.ReadBytes(8).SequenceEqual(new byte[] { 1, 2, 3 }));
            Check("reader exhausted", r.Remaining == 0);
            Throws<FormatException>("read past end throws",
                () => new CompactReader(new byte[] { 0x80, 0x80 }).ReadVarUInt());
            Throws<FormatException>("blob over cap throws",
                () => new CompactReader(new byte[] { 5, 1, 2, 3, 4, 5 }).ReadBytes(4));

            CompactWriter big = new CompactWriter(1);
            for (int i = 0; i < 1000; i++) big.WriteVarUInt((uint)i);
            Check("writer grows past initial capacity", big.Length > 1000);

            CompactWriter ul = new CompactWriter();
            ul.WriteVarULong(ulong.MaxValue);
            Check("varint u64 max round-trip", new CompactReader(ul.ToArray()).ReadVarULong() == ulong.MaxValue);
        }

        private static void AuthTests()
        {
            // RFC 4231 test case 1
            byte[] key = Enumerable.Repeat((byte)0x0b, 20).ToArray();
            byte[] digest = S2Auth.HmacSha256(key, Encoding.ASCII.GetBytes("Hi There"));
            string hex = S2Auth.Hex(digest);
            Check("HMAC-SHA256 RFC4231 case 1",
                  hex == "b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7");

            byte[] secret = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef");
            byte[] nonce = S2Auth.NewNonce();
            byte[] tag = S2Auth.ComputeTag(secret, nonce, S2Protocol.ProtoVer);
            Check("tag is 32 bytes", tag.Length == 32);
            Check("verify accepts correct tag", S2Auth.VerifyTag(secret, nonce, S2Protocol.ProtoVer, tag));
            tag[0] ^= 0xFF;
            Check("verify rejects tampered tag", !S2Auth.VerifyTag(secret, nonce, S2Protocol.ProtoVer, tag));
            Check("verify rejects short tag", !S2Auth.VerifyTag(secret, nonce, S2Protocol.ProtoVer, new byte[8]));
            Check("verify rejects empty secret",
                  !S2Auth.VerifyTag(Array.Empty<byte>(), nonce, S2Protocol.ProtoVer,
                                    S2Auth.ComputeTag(Encoding.UTF8.GetBytes("x"), nonce, 1)));
            Throws<ArgumentException>("nonce must be 32 bytes",
                () => S2Auth.ComputeTag(secret, new byte[31], S2Protocol.ProtoVer));

            string fh1 = S2Auth.FieldHash(new[] { "hp", "isAlive", "stableId" }, "anchor");
            string fh2 = S2Auth.FieldHash(new[] { "stableId", "hp", "isAlive" }, "anchor");
            string fh3 = S2Auth.FieldHash(new[] { "hp", "isAlive", "stableId" }, "anchor2");
            Check("fieldHash is order-insensitive", fh1 == fh2);
            Check("fieldHash is anchor-sensitive", fh1 != fh3);
            Check("fieldHash is 64 hex chars", fh1.Length == 64);
        }

        private static void HashTests()
        {
            ReplicaHeader header = new ReplicaHeader
            {
                BattleKey = "bk-1",
                TurnIndex = 7,
                ActiveUnitId = "0:9",
                Paused = false,
            };
            List<ReplicaUnit> units = new List<ReplicaUnit>
            {
                new ReplicaUnit { StableId = "1:10066", Team = 1, Row = 2, Column = 3, Hp = 41.5f, IsAlive = true, UnitAlive = true, Moved = false, Attacked = false },
                new ReplicaUnit { StableId = "0:9", Team = 0, Row = 5, Column = 1, Hp = 80f, IsAlive = true, UnitAlive = true, Moved = true, Attacked = false },
            };

            string a = ReplicaHash.Compute(header, units);
            List<ReplicaUnit> reversed = Enumerable.Reverse(units).ToList();
            string b = ReplicaHash.Compute(header, reversed);
            Check("replicaHash is order-insensitive", a == b);
            Check("replicaHash repeats", a == ReplicaHash.Compute(header, units));

            ReplicaHeader h2 = header;
            h2.TurnIndex = 8;
            Check("turnIndex changes hash", a != ReplicaHash.Compute(h2, units));

            ReplicaHeader h3 = header;
            h3.Paused = true;
            Check("paused changes hash", a != ReplicaHash.Compute(h3, units));

            List<ReplicaUnit> dead = units.Select(Clone).ToList();
            ReplicaUnit d0 = dead[0];
            d0.Hp = 0f;
            d0.IsAlive = false;
            d0.UnitAlive = true; // the one-frame lag we measured [RUNTIME]
            dead[0] = d0;
            Check("death changes hash", a != ReplicaHash.Compute(header, dead));

            List<ReplicaUnit> lagGone = dead.Select(Clone).ToList();
            ReplicaUnit d1 = lagGone[0];
            d1.UnitAlive = false;
            lagGone[0] = d1;
            Check("unitAlive transition changes hash",
                  ReplicaHash.Compute(header, dead) != ReplicaHash.Compute(header, lagGone));

            List<ReplicaUnit> hpOnly = units.Select(Clone).ToList();
            ReplicaUnit p0 = hpOnly[0];
            p0.Hp = 41.499f;
            hpOnly[0] = p0;
            Check("hp (3 decimals) changes hash", a != ReplicaHash.Compute(header, hpOnly));

            List<ReplicaUnit> moved = units.Select(Clone).ToList();
            ReplicaUnit m0 = moved[0];
            m0.Moved = true;
            moved[0] = m0;
            Check("moved changes hash", a != ReplicaHash.Compute(header, moved));

            Check("hash is 64 hex chars", a.Length == 64);
        }

        private static ReplicaUnit Clone(ReplicaUnit u) => new ReplicaUnit
        {
            StableId = u.StableId,
            Team = u.Team,
            Row = u.Row,
            Column = u.Column,
            Hp = u.Hp,
            IsAlive = u.IsAlive,
            UnitAlive = u.UnitAlive,
            Moved = u.Moved,
            Attacked = u.Attacked,
        };

        private static void AllowlistTests()
        {
            Check("guest may send AUTH", S2Protocol.IsGuestToHostAllowed(MessageType.Auth));
            Check("guest may send PING", S2Protocol.IsGuestToHostAllowed(MessageType.Ping));
            Check("guest may send RESYNC", S2Protocol.IsGuestToHostAllowed(MessageType.Resync));
            Check("guest may send BYE", S2Protocol.IsGuestToHostAllowed(MessageType.Bye));
            Check("guest may NOT send HELLO", !S2Protocol.IsGuestToHostAllowed(MessageType.Hello));
            Check("guest may NOT send SNAPSHOT", !S2Protocol.IsGuestToHostAllowed(MessageType.Snapshot));
            Check("guest may NOT send BATTLE_EVENT", !S2Protocol.IsGuestToHostAllowed(MessageType.BattleEvent));
            Check("guest may NOT send CHUNK (reassembled type is checked)",
                  !S2Protocol.IsGuestToHostAllowed(MessageType.Chunk));
            Check("unknown byte 0x41 is not allowed",
                  !S2Protocol.IsGuestToHostAllowed((MessageType)0x41));
            Check("unknown byte is not a defined type",
                  S2Protocol.ReassembledTypeOrUnknown(0x41) == MessageType.Unknown);
            Check("host types are host->guest", S2Protocol.IsHostToGuest(MessageType.Snapshot)
                                                && S2Protocol.IsHostToGuest(MessageType.Hello));
        }
    }
}
