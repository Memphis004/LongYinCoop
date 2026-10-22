using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using LongYinCoop.S2;
using LongYinCoop.S2.Host;
using LongYinCoop.S2.Protocol;
using LongYinCoop.S2.Transport;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// S2a-2 (step 0) in-process tests — no game, no sockets, no mod runtime.
    /// Covers: loopback transport, serializing sender, S2Frame wire codec (incl. the
    /// rev4 `unitAlive` mirror), audit tuple, guest->host allowlist gate (exit 6),
    /// `net.cfg` config poll (งาน A 2026-10-22 — flat key=value ใต้ UserData\LongYinCoop,
    /// MelonLoader ไม่แตะ), phase stats + the signed exit-3 rule
    /// (A = net.cfg enabled=false, B = true), the exit-6 self-test frame set (exactly
    /// 3 forbidden drops / 0 accepted), the pure rev4 write-guard (all 64 pref
    /// combinations, DevCommands excluded), ndjson evidence writer, and the TurnGate
    /// guard (byte-exact vs the 57e68dc fixture).
    /// </summary>
    internal static class S2a2Tests
    {
        private static int _fail;
        private static string _tmp;

        public static int Run()
        {
            _tmp = Path.Combine(Path.GetTempPath(), "s2a2-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tmp);
            try
            {
                LoopbackTests();
                SenderTests();
                CodecTests();
                AuditTests();
                GateTests();
                NetConfigTests();
                PhaseStatsTests();
                Exit3RuleTests();
                Exit6SelfTestTests();
                WriteGuardTests();
                UnitAliveMirrorTests();
                NdjsonWriterTests();
                TurnGateGuardTests();
            }
            finally
            {
                try { Directory.Delete(_tmp, true); } catch { }
            }
            return _fail;
        }

        private static void Check(string name, bool ok)
        {
            Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] s2a2: {name}");
            if (!ok) _fail++;
        }

        // ---------------------------------------------------------------- helpers

        private static S2Unit Unit(string id, int team, int hero, int row, int col, float hp,
                                   bool isAlive, bool moved = false, bool attacked = false)
        {
            return new S2Unit
            {
                StableId = id,
                Team = team,
                HeroId = hero,
                Row = row,
                Column = col,
                Hp = hp,
                MaxHp = hp + 10f,
                IsAlive = isAlive,
                UnitAlive = S2UnitRule.MirrorUnitAlive(isAlive),
                Moved = moved,
                Attacked = attacked,
                PlayerControl = false,
            };
        }

        private static S2Frame Frame(int id, int units = 3, float battleTime = 12.5f, int turnIndex = 4)
        {
            S2Frame f = new S2Frame();
            f.FrameId = id;
            f.Header.BattleKey = "bk-test";
            f.Header.TurnIndex = turnIndex;
            f.Header.ActiveUnitId = "0:1001";
            f.Header.Paused = false;
            f.Header.NowActiveState = 2;
            f.Header.BattleTime = battleTime;
            f.Header.UnitCount = units;
            f.Units = new S2Unit[units];
            for (int i = 0; i < units; i++)
            {
                f.Units[i] = Unit("0:" + (1000 + i), 0, 1000 + i, i, i + 1, 40f - i, isAlive: i != 2, moved: i == 1);
            }
            f.Audit = S2AuditUtil.Compute(f.Header, f.Units);
            return f;
        }

        private static S2Audit ReferenceAudit()
        {
            S2Frame f = Frame(999);
            return f.Audit;
        }

        // ---------------------------------------------------------------- transport

        private static void LoopbackTests()
        {
            LoopbackTransport host, guest;
            LoopbackTransport.CreatePair(out host, out guest);
            Check("loopback pair is open", host.IsOpen && guest.IsOpen);
            Check("loopback send host->guest", host.Send(new byte[] { 1, 2, 3 }));
            byte[] got;
            Check("loopback receive on peer", guest.TryReceive(out got) && got.Length == 3 && got[2] == 3);
            Check("loopback inbox empty after receive", !guest.TryReceive(out got));
            Check("loopback stats count frames/bytes",
                host.Stats.FramesSent == 1 && host.Stats.BytesSent == 3
                && guest.Stats.FramesReceived == 1 && guest.Stats.BytesReceived == 3);
            Check("loopback guest->host direction", guest.Send(new byte[] { 7, 7 }) && host.TryReceive(out got)
                && got[0] == 7);

            LoopbackTransport h2, g2;
            LoopbackTransport.CreatePairClosed(out h2, out g2);
            Check("closed pair refuses send (fail-closed)", !h2.Send(new byte[] { 9 }) && h2.Stats.SendDrops == 1);
            h2.Open();
            Check("still refuses while peer closed", !h2.Send(new byte[] { 9 }) && h2.Stats.SendDrops == 2);
            g2.Open();
            Check("sends after both ends open", h2.Send(new byte[] { 9 }));

            LoopbackTransport h3, g3;
            LoopbackTransport.CreatePair(out h3, out g3, capacity: 2);
            Check("capacity 1 accepted", h3.Send(new byte[] { 1 }));
            Check("capacity 2 accepted", h3.Send(new byte[] { 1 }));
            Check("over capacity drops and counts",
                !h3.Send(new byte[] { 1 }) && h3.FullDrops == 1 && h3.Stats.SendDrops == 1 && g3.Queued == 2);
            Check("transport kind/describe is loopback", h3.Kind == TransportKind.Loopback
                && h3.Describe == "loopback(in-proc)");
        }

        private sealed class SlowTransport : ITransport
        {
            public readonly List<byte[]> Seen = new List<byte[]>();

            public TransportKind Kind { get { return TransportKind.Loopback; } }
            public string Describe { get { return "slow-test"; } }
            public bool IsOpen { get { return true; } }
            public void Open() { }
            public void Close() { }
            public bool Send(byte[] frame) { Thread.Sleep(100); lock (Seen) { Seen.Add(frame); } return true; }
            public bool TryReceive(out byte[] frame) { frame = null; return false; }
            public TransportStats Stats { get; } = new TransportStats();
        }

        // ---------------------------------------------------------------- sender

        private static void SenderTests()
        {
            LoopbackTransport host, guest;
            LoopbackTransport.CreatePair(out host, out guest);
            S2Counters counters = new S2Counters();
            S2Metrics metrics = new S2Metrics();
            string path = Path.Combine(_tmp, "sender.ndjson");
            S2NdjsonWriter writer = new S2NdjsonWriter(path, flushEvery: 1);
            // S2a-3: `Dedup=false` + `SendHz=0` = พฤติกรรม S2a-2 เดิม (ส่งทุกเฟรม) สำหรับ regression นี้
            S2SerializingSender sender = new S2SerializingSender(host, counters, metrics, writer,
                new S2SenderOptions { QueueCapacity = 8, Dedup = false, SendHz = 0, HeartbeatMs = 0 });

            for (int i = 0; i < 5; i++)
            {
                Check("sender accepts post " + i, sender.TryPost(Frame(i + 1)));
            }

            long deadline = Environment.TickCount64 + 3000;
            while (guest.Queued < 5 && Environment.TickCount64 < deadline) Thread.Sleep(5);
            Check("sender queued 5 frames to the transport", guest.Queued == 5);
            Check("sender counters: framesSent/bytesSent", counters.FramesSent == 5 && counters.BytesSent > 0);
            Check("sender metrics recorded 5 samples", metrics.TotalFrames == 5 && metrics.WindowSamples == 5);
            Check("sender metrics p50 sane", metrics.SamplePercentile(0.5) >= 0);
            Check("ndjson writer wrote 5 frame lines", writer.Lines == 5);

            byte[] raw;
            bool anyFrame = guest.TryReceive(out raw);
            FrameHeader fh = default(FrameHeader);
            byte[] payload = Array.Empty<byte>();
            bool decodedFrame = anyFrame && FrameCodec.TryDecode(raw, out fh, out payload);
            Check("first delivered frame is Snapshot", decodedFrame && fh.Type == MessageType.Snapshot);
            S2Frame back;
            string err;
            Check("delivered payload decodes to a frame", S2FrameCodec.TryDecode(payload, out back, out err));
            Check("delivered frame keeps unit count + audit",
                back != null && back.Units.Length == 3 && back.Audit.SameAs(Frame(1).Audit));

            sender.Stop();
            writer.Dispose();
            Check("sender stats after stop: posted 5 sent 5 dropped 0",
                sender.Posted == 5 && sender.Sent == 5 && sender.Dropped == 0 && sender.Errors == 0);

            // backpressure: slow transport + tiny queue -> TryPost must drop instead of blocking
            SlowTransport slow = new SlowTransport();
            S2Counters c2 = new S2Counters();
            S2SerializingSender s2 = new S2SerializingSender(slow, c2, new S2Metrics(), null,
                new S2SenderOptions { QueueCapacity = 1, Dedup = false, SendHz = 0, HeartbeatMs = 0 });
            long t0 = Environment.TickCount64;
            for (int i = 0; i < 8; i++) s2.TryPost(Frame(100 + i));
            long elapsed = Environment.TickCount64 - t0;
            Check("full queue drops instead of blocking main thread", s2.Dropped >= 5 && elapsed < 200);
            Check("queue drops counted in counters", c2.FramesQueuedDropped >= 5 && s2.Posted == 8);
            s2.Stop(2000);
        }

        // ---------------------------------------------------------------- codec

        private static void CodecTests()
        {
            Check("unit cap is 64", S2FrameCodec.MaxUnits == 64);

            S2Frame f = Frame(7, units: 4, battleTime: 33.25f, turnIndex: 9);
            f.Units[2] = Unit("1:2002", 1, 2002, -1, -1, 0f, isAlive: false, moved: true, attacked: true);
            f.Audit = S2AuditUtil.Compute(f.Header, f.Units);

            byte[] payload = S2FrameCodec.Encode(f);
            Check("encoded payload smaller than message cap", payload.Length < S2Protocol.MaxMessageBytes);

            S2Frame back;
            string err;
            Check("payload roundtrip decodes", S2FrameCodec.TryDecode(payload, out back, out err));
            Check("header roundtrip", back.Header.BattleKey == "bk-test" && back.Header.TurnIndex == 9
                && back.Header.ActiveUnitId == "0:1001" && back.Header.Paused == false
                && back.Header.NowActiveState == 2 && Math.Abs(back.Header.BattleTime - 33.25f) < 0.0001f
                && back.Header.UnitCount == 4);
            Check("unit roundtrip keeps -1 row/col and flags",
                back.Units[2].Row == -1 && back.Units[2].Column == -1 && back.Units[2].IsAlive == false
                && back.Units[2].Moved && back.Units[2].Attacked && back.Units[2].Team == 1);
            Check("unit roundtrip carries unitAlive (rev4 field, mirrored from isAlive)",
                back.Units[0].UnitAlive == back.Units[0].IsAlive
                && back.Units[2].UnitAlive == back.Units[2].IsAlive && back.Units[0].IsAlive);

            // the wire carries the field itself, it is not recomputed on decode
            S2Frame diverge = Frame(11, units: 1);
            diverge.Units[0].IsAlive = false;
            diverge.Units[0].UnitAlive = true;
            diverge.Audit = S2AuditUtil.Compute(diverge.Header, diverge.Units);
            byte[] divergePayload = S2FrameCodec.Encode(diverge);
            S2Frame dback;
            string derr;
            Check("unitAlive is on the wire (decode reads it, does not mirror it)",
                S2FrameCodec.TryDecode(divergePayload, out dback, out derr)
                && dback.Units[0].UnitAlive && !dback.Units[0].IsAlive);

            Check("audit recomputed on decode matches", back.Audit.SameAs(f.Audit));

            byte[] truncated = new byte[payload.Length - 1];
            Array.Copy(payload, truncated, truncated.Length);
            Check("truncated payload fails closed",
                !S2FrameCodec.TryDecode(truncated, out back, out err) && back == null);

            S2Frame big = Frame(8, units: 65);
            byte[] bigPayload = S2FrameCodec.Encode(big);
            Check("decode refuses over-cap unit count",
                !S2FrameCodec.TryDecode(bigPayload, out back, out err) && err != null && err.Contains("over cap"));

            Check("decode refuses empty payload", !S2FrameCodec.TryDecode(new byte[0], out back, out err));

            byte[] chunkMe = new byte[4000];
            List<byte[]> chunkFrames = ChunkCodec.Encode(3, MessageType.Snapshot, S2Protocol.ProtoVer, 1, chunkMe);
            Check("oversized frame is chunked by codec", chunkFrames.Count > 1);
            Check("each chunk frame fits the frame cap",
                chunkFrames.All(x => x.Length <= S2Protocol.MaxFrameBytes));

            bool threw = false;
            try
            {
                ChunkCodec.Encode(4, MessageType.Snapshot, S2Protocol.ProtoVer, 2,
                                  new byte[S2Protocol.MaxMessageBytes + 1]);
            }
            catch (ArgumentException)
            {
                threw = true;
            }
            Check("message over the 64 KB cap throws (fail-closed)", threw);
        }

        // ---------------------------------------------------------------- audit tuple

        private static void AuditTests()
        {
            S2Header h = Frame(1).Header;
            S2Unit[] units = Frame(1).Units;

            S2Audit a1 = S2AuditUtil.Compute(h, units);
            S2Audit a2 = S2AuditUtil.Compute(h, units);
            Check("audit tuple stable for unchanged state", a1.SameAs(a2) && a1.UnitsSum == a2.UnitsSum);
            Check("audit tuple text has no secret", a1.ToText().StartsWith("battleTime=") && a1.ToText().Length > 20);

            S2Unit[] reordered = new S2Unit[] { units[2], units[0], units[1] };
            Check("audit tuple is order independent", S2AuditUtil.Compute(h, reordered).SameAs(a1));

            S2Unit[] hurt = (S2Unit[])units.Clone();
            hurt[0].Hp = hurt[0].Hp - 3.5f;
            Check("audit changes when hp changes", !S2AuditUtil.Compute(h, hurt).SameAs(a1));

            S2Unit[] dead = (S2Unit[])units.Clone();
            bool wasAlive = dead[0].IsAlive;
            dead[0].IsAlive = !wasAlive;
            Check("audit changes when isAlive changes", !S2AuditUtil.Compute(h, dead).SameAs(a1));

            S2Unit[] moved = (S2Unit[])units.Clone();
            moved[1] = Unit(moved[1].StableId, moved[1].Team, moved[1].HeroId, moved[1].Row, moved[1].Column,
                            moved[1].Hp, moved[1].IsAlive, moved: !moved[1].Moved, attacked: true);
            Check("audit ignores moved/attacked (informational fields only)",
                S2AuditUtil.Compute(h, moved).SameAs(a1));

            S2Unit[] copy = (S2Unit[])units.Clone();
            copy[0].UnitAlive = !copy[0].UnitAlive;
            Check("audit ignores unitAlive (it is a copy of isAlive, not a second signal)",
                S2AuditUtil.Compute(h, copy).SameAs(a1));

            S2Unit[] shifted = (S2Unit[])units.Clone();
            shifted[0].Row = shifted[0].Row + 1;
            Check("audit changes when grid position changes", !S2AuditUtil.Compute(h, shifted).SameAs(a1));

            S2Header h2 = h;
            h2.BattleTime = h.BattleTime + 1f;
            Check("audit changes when battleTime changes", !S2AuditUtil.Compute(h2, units).SameAs(a1));
            S2Header h3 = h;
            h3.TurnIndex = h.TurnIndex + 1;
            Check("audit changes when turnIndex changes", !S2AuditUtil.Compute(h3, units).SameAs(a1));
        }

        // ---------------------------------------------------------------- guest gate (exit 6)

        private static void GateTests()
        {
            S2Counters counters = new S2Counters();
            S2GuestGate gate = new S2GuestGate(counters);
            AssembledMessage msg;
            string reason;

            byte[] hello = FrameCodec.Encode(MessageType.Hello, S2Protocol.ProtoVer, 1, new byte[4]);
            Check("guest Hello is forbidden",
                gate.Accept(hello, 0, out msg, out reason) == S2GateOutcome.Forbidden
                && counters.ForbiddenTypeDrops == 1);

            byte[] snapshot = FrameCodec.Encode(MessageType.Snapshot, S2Protocol.ProtoVer, 2, new byte[4]);
            Check("guest Snapshot is forbidden",
                gate.Accept(snapshot, 0, out msg, out reason) == S2GateOutcome.Forbidden
                && counters.ForbiddenTypeDrops == 2);

            byte[] battleEvent = FrameCodec.Encode(MessageType.BattleEvent, S2Protocol.ProtoVer, 3, new byte[4]);
            Check("guest BattleEvent is forbidden",
                gate.Accept(battleEvent, 0, out msg, out reason) == S2GateOutcome.Forbidden
                && counters.ForbiddenTypeDrops == 3);

            // chunked forbidden message: must be reassembled, then rejected, and counted once
            byte[] oversized = new byte[S2Protocol.MaxPayloadBytes + 32];
            List<byte[]> chunks = ChunkCodec.Encode(9, MessageType.World, S2Protocol.ProtoVer, 4, oversized);
            S2GateOutcome last = S2GateOutcome.Await;
            for (int i = 0; i < chunks.Count; i++) last = gate.Accept(chunks[i], 0, out msg, out reason);
            Check("chunked forbidden message is rejected after reassembly",
                last == S2GateOutcome.Forbidden && counters.ForbiddenTypeDrops == 4
                && counters.GuestFramesReassembled == 1 && counters.GuestChunkFrames == chunks.Count);

            // allowed control frames
            byte[] auth = FrameCodec.Encode(MessageType.Auth, S2Protocol.ProtoVer, 5, new byte[8]);
            byte[] ping = FrameCodec.Encode(MessageType.Ping, S2Protocol.ProtoVer, 6, Array.Empty<byte>());
            byte[] resync = FrameCodec.Encode(MessageType.Resync, S2Protocol.ProtoVer, 7, Array.Empty<byte>());
            byte[] bye = FrameCodec.Encode(MessageType.Bye, S2Protocol.ProtoVer, 8, Array.Empty<byte>());
            Check("guest Auth is control-accepted",
                gate.Accept(auth, 0, out msg, out reason) == S2GateOutcome.Control && msg.Type == MessageType.Auth);
            Check("guest Ping is control-accepted",
                gate.Accept(ping, 1, out msg, out reason) == S2GateOutcome.Control);
            Check("guest Resync is control-accepted",
                gate.Accept(resync, 2, out msg, out reason) == S2GateOutcome.Control);
            Check("guest Bye is control-accepted",
                gate.Accept(bye, 3, out msg, out reason) == S2GateOutcome.Control);
            Check("control frames counted, not forbidden", counters.ControlFramesHandled == 4
                && counters.ForbiddenTypeDrops == 4);

            // malformed paths
            Check("empty frame is malformed", gate.Accept(new byte[0], 0, out msg, out reason)
                == S2GateOutcome.Malformed && counters.MalformedDrops == 1);
            byte[] garbage = new byte[] { 0xff, 0xff, 0x41, 0x01, 1, 2 };
            Check("undecodable header is malformed", gate.Accept(garbage, 0, out msg, out reason)
                == S2GateOutcome.Malformed && counters.MalformedDrops == 2);
            byte[] withTrailing = new byte[hello.Length + 1];
            Array.Copy(hello, withTrailing, hello.Length);
            Check("trailing bytes are malformed", gate.Accept(withTrailing, 0, out msg, out reason)
                == S2GateOutcome.Malformed && counters.MalformedDrops == 3);
            byte[] badChunk = FrameCodec.Encode(MessageType.Chunk, S2Protocol.ProtoVer, 9,
                new byte[] { 0, 1, 0, 5, 0, 2, (byte)MessageType.BattleEvent, 1, 2, 3 });
            Check("chunk with idx>=total is malformed", gate.Accept(badChunk, 0, out msg, out reason)
                == S2GateOutcome.Malformed && counters.MalformedDrops == 4);
            byte[] incomplete = chunks[0];
            Check("incomplete chunk awaits without dropping",
                gate.Accept(incomplete, 10, out msg, out reason) == S2GateOutcome.Await
                && counters.MalformedDrops == 4 && counters.GuestFramesAwaiting == 2
                && gate.PendingChunks == 1);

            Check("no game command was ever accepted", counters.CommandsAccepted == 0);
            Check("NoForbiddenActivity is false when forbidden frames were dropped",
                counters.NoForbiddenActivity == false && counters.ForbiddenTypeDrops == 4);
            Check("gate leaves the audit tuple untouched (no write path)",
                ReferenceAudit().SameAs(ReferenceAudit()));

            List<MethodInfo> gateMethods = typeof(S2GuestGate)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName).ToList();
            Check("gate exposes exactly one entry point: Accept",
                gateMethods.Count == 1 && gateMethods[0].Name == "Accept");
            Check("gate entry point cannot take a game object",
                gateMethods[0].GetParameters().All(p => p.ParameterType == typeof(byte[])
                    || p.ParameterType == typeof(long)
                    || p.ParameterType.IsByRef)
                && gateMethods[0].ReturnType == typeof(S2GateOutcome));
        }

        // ---------------------------------------------------------------- net.cfg (งาน A, 2026-10-22)

        /// <summary>
        /// งาน A (มติผู้ใช้ 2026-10-22): `coop.net.*` ย้ายไป `UserData\LongYinCoop\net.cfg`
        /// — flat `key = value`, ไม่ใช่ TOML, ไม่มี section, MelonLoader ไม่แตะไฟล์นี้
        /// (incident S2a-2: TOML ใน MelonPreferences.cfg ทำให้ mod นิ่ง และ ML แปลง dotted key
        ///  เป็นตารางซ้อน `[LongYinCoop.coop] net = { … }` ตอนออกเกม)
        /// </summary>
        private static void NetConfigTests()
        {
            const string secret = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
            string path = Path.Combine(_tmp, "net.cfg");

            S2NetSettings s = S2NetConfig.Parse("", path, 0);
            Check("net.cfg: empty text = disabled/off/format-ok (fail-closed default)",
                !s.Enabled && s.Role == "off" && !s.AllowNonLoopback && s.FormatOk && s.IsValid);

            s = S2NetConfig.Parse("# only comments\n; another\n\n", path, 0);
            Check("net.cfg: comments-only file = valid and disabled", s.IsValid && !s.Enabled);

            string host = "enabled = true\nrole = host\nsecret = " + secret + "\nallowNonLoopback = false\n";
            s = S2NetConfig.Parse(host, path, 0);
            Check("net.cfg: flat host block is valid + enabled (no [section] needed)",
                s.Enabled && s.Role == "host" && s.Secret == secret && s.FormatOk && s.IsValid && s.SecretLooksSet);
            Check("net.cfg: describe never leaks the secret",
                !s.Describe().Contains(secret) && s.Describe().Contains("secretSet=True"));

            s = S2NetConfig.Parse(host.Replace("role = host", "role = guest"), path, 0);
            Check("net.cfg: guest role parses (service decides host-only)",
                s.Enabled && s.Role == "guest" && s.IsValid);

            s = S2NetConfig.Parse("enabled = false\n", path, 0);
            Check("net.cfg: disabled needs no role/secret", !s.Enabled && s.IsValid);

            s = S2NetConfig.Parse(host.Replace(secret, "short"), path, 0);
            Check("net.cfg: short secret is invalid", !s.IsValid && s.Error != null && s.Error.Contains("secret"));

            s = S2NetConfig.Parse(host.Replace(secret, "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz"), path, 0);
            Check("net.cfg: non-hex secret is invalid", !s.IsValid && s.Error != null && s.Error.Contains("secret"));

            s = S2NetConfig.Parse(host.Replace("role = host", "role = banana"), path, 0);
            Check("net.cfg: unknown role is invalid", !s.IsValid && s.Error != null && s.Error.Contains("role"));

            s = S2NetConfig.Parse(host.Replace("enabled = true", "enabled = yes"), path, 0);
            Check("net.cfg: truthy spellings accepted", s.Enabled && s.IsValid);

            s = S2NetConfig.Parse("ENABLED = true\nRole = Host\nSECRET = \"" + secret + "\"\n", path, 0);
            Check("net.cfg: keys are case-insensitive and quoted values are unquoted",
                s.Enabled && s.Role == "host" && s.Secret == secret && s.IsValid);

            s = S2NetConfig.Parse(host.Replace("allowNonLoopback = false", "allowNonLoopback = true"), path, 0);
            Check("net.cfg: allowNonLoopback parsed (bind policy decided by S2b)",
                s.AllowNonLoopback && s.Describe().Contains("allowNonLoopback=True"));

            s = S2NetConfig.Parse(host + "futureKey = 42\n", path, 0);
            Check("net.cfg: unknown keys are ignored, not fatal", s.IsValid && s.Enabled);

            s = S2NetConfig.Parse("enabled = false\nenabled = true\nrole = host\nsecret = " + secret + "\n", path, 0);
            Check("net.cfg: duplicate key = last one wins", s.Enabled && s.IsValid);

            // ก๊อป TOML/pref เดิมมาใส่: ต้องปฏิเสธ ไม่ใช่ 'disabled' เงียบ ๆ
            s = S2NetConfig.Parse("[LongYinCoop]\nenabled = true\n", path, 0);
            Check("net.cfg: [section] is rejected (flat file only)",
                !s.FormatOk && !s.IsValid && s.Error != null && s.Error.Contains("flat"));

            // ค่าที่ MelonLoader เขียนเองตอนออกเกม (incident 2026-10-22) ต้องไม่ถูกอ่านเป็น valid+disabled
            string mlShaped = "[LongYinCoop.coop]\nnet = { enabled = false }\nallowNonLoopback = false\n";
            s = S2NetConfig.Parse(mlShaped, path, 0);
            Check("net.cfg: MelonLoader-shaped pref content is rejected, never silently valid",
                !s.FormatOk && !s.IsValid && !s.Enabled);

            s = S2NetConfig.Parse("enabled true\n", path, 0);
            Check("net.cfg: a line without '=' is rejected with its line number",
                !s.FormatOk && !s.IsValid && s.Error != null && s.Error.Contains("line 1"));

            s = S2NetConfig.Parse("enabled = true\r\nrole = host\r\nsecret = " + secret + "\r\n", path, 0);
            Check("net.cfg: CRLF file parses", s.Enabled && s.IsValid && s.Role == "host");

            Check("IsTrue spellings", S2NetConfig.IsTrue(" true ") && S2NetConfig.IsTrue("1")
                && S2NetConfig.IsTrue("YES") && S2NetConfig.IsTrue("on")
                && !S2NetConfig.IsTrue("false") && !S2NetConfig.IsTrue("0") && !S2NetConfig.IsTrue(null));

            S2NetSettings missing = S2NetConfig.ReadFile(Path.Combine(_tmp, "nope.cfg"), 0, 1);
            Check("net.cfg: missing file is reported, not thrown (fail-closed)",
                missing.Error == "cfg not found" && !missing.FileRead && !missing.FormatOk && !missing.IsValid);

            // D2-i: ค่าต้องถูกรี้อ่านจากไฟล์ระหว่างเกมรัน (สลับ block A/B ได้)
            File.WriteAllText(path, "enabled = false\n");
            S2NetConfig cfg = new S2NetConfig(path, pollMs: 1000);
            Check("net.cfg: first poll reads the file", cfg.Poll(0) && !cfg.Current.Enabled && cfg.PollCount == 1);
            File.WriteAllText(path, host);
            Check("net.cfg: poll inside the interval does not re-read", !cfg.Poll(500) && !cfg.Current.Enabled);
            Check("net.cfg: poll after the interval picks up the change", cfg.Poll(1000) && cfg.Current.Enabled
                && cfg.Current.Role == "host" && cfg.PollCount == 2);
            File.WriteAllText(path, "enabled = false\n");
            Check("net.cfg: toggling back off is picked up (A/B blocks)", cfg.Poll(2000) && !cfg.Current.Enabled
                && cfg.PollCount == 3);
            S2NetSettings forced = cfg.ForceRead(2100);
            Check("net.cfg: ForceRead reads immediately", forced.ReadCount == 4 && cfg.PollCount == 4);

            // ไฟล์ที่ mod ใช้อ่าน ต้องเป็นของเราเอง ไม่ใช่ MelonPreferences.cfg
            Check("coop paths: net.cfg lives under UserData\\LongYinCoop (MelonLoader never touches it)",
                CoopPaths.NetCfgPath.EndsWith(Path.Combine("UserData", "LongYinCoop", "net.cfg"),
                                               StringComparison.OrdinalIgnoreCase)
                && !CoopPaths.NetCfgPath.EndsWith("MelonPreferences.cfg", StringComparison.OrdinalIgnoreCase));
        }

        // ---------------------------------------------------------------- exit 3 accumulator

        private static void PhaseStatsTests()
        {
            S2PhaseStats p = new S2PhaseStats("sampleMs");
            Check("empty phase stats are zeroed", p.Added == 0 && p.Mean == 0 && p.Max == 0);
            p.Add(1.0);
            p.Add(2.0);
            p.Add(6.0);
            Check("phase stats mean/min/max", p.Added == 3 && Math.Abs(p.Mean - 3.0) < 1e-9
                && p.Min == 1.0 && p.Max == 6.0 && Math.Abs(p.Last - 6.0) < 1e-9);
            Check("phase stats snapshot text", p.Snapshot() != null && p.ToText().Contains("meanMs=3"));
            p.Reset();
            Check("phase stats reset for the next A/B block", p.Added == 0 && p.Mean == 0 && p.Max == 0);
        }

        // ---------------------------------------------------------------- exit 3 rule (signed)

        /// <summary>
        /// A/B ของ exit 3 (มติผู้ใช้ 2026-10-21): A = `net.cfg` enabled=false (baseline), B = `true`
        /// เกณฑ์คือ **(B − A) ≤ 1 ms โดยดูเครื่องหมาย** ไม่ใช่ค่าสัมบูรณ์ — B ถูกกว่า A (ค่าลบ) ก็ผ่าน
        /// ใกล้ 1 ms (±0.5 ms) = INCONCLUSIVE ห้ามฟันธงผ่านจากรอบเดียว
        /// </summary>
        private static void Exit3RuleTests()
        {
            Check("exit-3 constants: margin 1.0 ms, band 0.5 ms",
                Math.Abs(S2Exit3Rule.MarginMs - 1.0) < 1e-9 && Math.Abs(S2Exit3Rule.BandMs - 0.5) < 1e-9);

            Check("exit-3 delta is signed (B - A)",
                Math.Abs(S2Exit3Rule.Delta(3.0, 4.5) - 1.5) < 1e-9
                && Math.Abs(S2Exit3Rule.Delta(4.5, 3.0) + 1.5) < 1e-9);

            // B > A -> delta positive; a clear miss must FAIL (not pass, not inconclusive)
            Check("B > A by 2 ms: delta positive and verdict FAIL",
                S2Exit3Rule.Delta(3.0, 5.0) > 0
                && S2Exit3Rule.Decide(3.0, 5.0) == S2Exit3Rule.Verdict.Fail);

            // B < A -> delta negative; S2 cheaper than the baseline still passes
            Check("B < A by 2 ms: delta negative and verdict PASS",
                S2Exit3Rule.Delta(5.0, 3.0) < 0
                && S2Exit3Rule.Decide(5.0, 3.0) == S2Exit3Rule.Verdict.Pass);

            // positive delta but inside the margin: S2 costs a bit more, still fine
            Check("B > A by 0.4 ms: positive delta but verdict PASS",
                S2Exit3Rule.Decide(3.0, 3.4) == S2Exit3Rule.Verdict.Pass);

            Check("B = A (delta 0) is a PASS", S2Exit3Rule.Decide(3.0, 3.0) == S2Exit3Rule.Verdict.Pass);

            // exactly at the margin, just below and just above: the band is the decision zone
            Check("delta exactly 1.0 ms is INCONCLUSIVE (never a one-round pass)",
                S2Exit3Rule.Decide(3.0, 4.0) == S2Exit3Rule.Verdict.Inconclusive);
            Check("delta 0.6 ms is INCONCLUSIVE",
                S2Exit3Rule.Decide(3.0, 3.6) == S2Exit3Rule.Verdict.Inconclusive);
            Check("delta 1.5 ms is INCONCLUSIVE",
                S2Exit3Rule.Decide(3.0, 4.5) == S2Exit3Rule.Verdict.Inconclusive);
            Check("delta 1.6 ms is FAIL", S2Exit3Rule.Decide(3.0, 4.6) == S2Exit3Rule.Verdict.Fail);
            Check("delta 0.3 ms is PASS", S2Exit3Rule.Decide(3.0, 3.3) == S2Exit3Rule.Verdict.Pass);

            Check("verdict text mapping", S2Exit3Rule.Text(S2Exit3Rule.Verdict.Pass) == "PASS"
                && S2Exit3Rule.Text(S2Exit3Rule.Verdict.Inconclusive) == "INCONCLUSIVE"
                && S2Exit3Rule.Text(S2Exit3Rule.Verdict.Fail) == "FAIL");

            string passLine = S2Exit3Rule.Report(5.0, 3.0);
            Check("exit-3 report keeps the sign and the verdict",
                passLine.Contains("delta(B-A)=-2.0000") && passLine.Contains("verdict=PASS")
                && passLine.Contains("A(meanMs)=5.0000") && passLine.Contains("B(meanMs)=3.0000"));
            Check("exit-3 report of a miss says FAIL", S2Exit3Rule.Report(3.0, 5.0).Contains("verdict=FAIL"));
            Check("exit-3 report of the band says INCONCLUSIVE",
                S2Exit3Rule.Report(3.0, 4.0).Contains("verdict=INCONCLUSIVE"));

            // the accumulator feeds the rule directly (A block then B block)
            S2PhaseStats blockA = new S2PhaseStats("sampleMs");
            S2PhaseStats blockB = new S2PhaseStats("sampleMs");
            for (int i = 0; i < 30; i++) blockA.Add(2.0);
            for (int i = 0; i < 30; i++) blockB.Add(2.3);
            Check("phase means feed the exit-3 rule (A 2.0 / B 2.3 = PASS)",
                Math.Abs(blockA.Mean - 2.0) < 1e-9 && Math.Abs(blockB.Mean - 2.3) < 1e-9
                && S2Exit3Rule.Decide(blockA.Mean, blockB.Mean) == S2Exit3Rule.Verdict.Pass);
        }

        // ---------------------------------------------------------------- exit 6 self-test

        /// <summary>
        /// นิยาม exit 6 (rev3): self-test ยิง frame ต้องห้าม 3 ใบ (Hello + Snapshot + chunked BattleEvent)
        /// แล้วต้องได้ **3 พอดี**, `commandsAccepted == 0`, audit tuple ก่อน/หลังเท่ากัน
        /// </summary>
        private static void Exit6SelfTestTests()
        {
            Check("self-test expects exactly 3 forbidden drops", S2SelfTest.ExpectedForbiddenDrops == 3);

            List<byte[]> frames = S2SelfTest.BuildFrames();
            Check("self-test builds 2 plain forbidden frames + at least 2 chunks",
                frames.Count >= 4 && frames.Count - 2 >= 2);

            foreach (byte[] frame in frames)
            {
                FrameHeader fh;
                byte[] payload;
                Check("every self-test frame decodes", FrameCodec.TryDecode(frame, out fh, out payload));
            }

            // the three forbidden message types travel as Hello, Snapshot and (chunked) BattleEvent
            FrameHeader h0 = default(FrameHeader);
            byte[] p0 = Array.Empty<byte>();
            FrameCodec.TryDecode(frames[0], out h0, out p0);
            FrameHeader h1 = default(FrameHeader);
            byte[] p1 = Array.Empty<byte>();
            FrameCodec.TryDecode(frames[1], out h1, out p1);
            Check("self-test frame types are Hello then Snapshot",
                h0.Type == MessageType.Hello && h1.Type == MessageType.Snapshot);
            Check("self-test carries the chunked BattleEvent id",
                S2SelfTest.ChunkMsgId == 7 && frames.Count - 2 == 2);

            S2Counters counters = new S2Counters();
            S2GuestGate gate = new S2GuestGate(counters);
            S2Audit auditBefore = ReferenceAudit();

            AssembledMessage msg;
            string reason;
            S2GateOutcome last = S2GateOutcome.Await;
            for (int i = 0; i < frames.Count; i++)
                last = gate.Accept(frames[i], 0, out msg, out reason);

            Check("last self-test frame is rejected as forbidden", last == S2GateOutcome.Forbidden);
            Check("self-test yields exactly 3 forbidden drops (not 2, not 4)",
                counters.ForbiddenTypeDrops == 3);
            Check("self-test accepts no game command", counters.CommandsAccepted == 0);
            Check("self-test reassembles the chunked message once",
                counters.GuestFramesReassembled == 1 && counters.GuestChunkFrames == frames.Count - 2);
            Check("self-test never produces a malformed drop", counters.MalformedDrops == 0);
            Check("audit tuple unchanged across the self-test path",
                auditBefore.SameAs(ReferenceAudit()));

            Check("exit-6 rule accepts 3 drops / 0 accepted / audit equal",
                S2SelfTest.MeetsExit6(3, 0, true));
            Check("exit-6 rule rejects 2 drops", !S2SelfTest.MeetsExit6(2, 0, true));
            Check("exit-6 rule rejects 4 drops", !S2SelfTest.MeetsExit6(4, 0, true));
            Check("exit-6 rule rejects an accepted command", !S2SelfTest.MeetsExit6(3, 1, true));
            Check("exit-6 rule rejects a changed audit tuple", !S2SelfTest.MeetsExit6(3, 0, false));

            string line = S2SelfTest.Describe(3, 0, true, frames.Count, frames.Count - 2);
            Check("exit-6 report states the exact expectation and verdict",
                line.Contains("expectedForbiddenDrops=3") && line.Contains("gained=3")
                && line.Contains("commandsAccepted=0") && line.Contains("auditEqual=true")
                && line.Contains("exit6=PASS"));

            // an aborted self-test (chunks missing) must not be counted as 3 drops
            S2Counters c2 = new S2Counters();
            S2GuestGate g2 = new S2GuestGate(c2);
            for (int i = 0; i < 3; i++) g2.Accept(frames[i], 0, out msg, out reason);
            Check("aborted self-test (missing chunks) cannot pass exit 6",
                c2.ForbiddenTypeDrops != S2SelfTest.ExpectedForbiddenDrops
                && !S2SelfTest.MeetsExit6(c2.ForbiddenTypeDrops, c2.CommandsAccepted, true));
        }

        // ---------------------------------------------------------------- write guard (rev4, pure)

        private static readonly string[] PrefNames =
        {
            "AllowRemoteInput", "JitProbe", "JitProbeW2", "JitProbeW4", "JitProbeW6", "JitProbeW7",
        };

        private static bool Reject(bool[] flags)
        {
            return S2WriteGuard.WritePathsClosed(flags[0], flags[1], flags[2], flags[3], flags[4], flags[5]);
        }

        private static string DescribeFlags(bool[] flags)
        {
            return S2WriteGuard.Describe(flags[0], flags[1], flags[2], flags[3], flags[4], flags[5]);
        }

        /// <summary>
        /// มติผู้ใช้ 2026-10-21 (ก): guard ของ S2 ต้องเป็น **ฟังก์ชัน pure ของ 6 pref**
        /// (`allowRemoteInput` + `jitProbe` `jitProbeW2` `jitProbeW4` `jitProbeW6` `jitProbeW7`)
        /// โดย **`DevCommands` ไม่มีผลกับผลลัพธ์** · ปฏิเสธถ้ามีตัวใดตัวหนึ่งเป็น true
        /// เทสต์ครบทั้ง 64 ชุดค่าผสม + ตรวจซิกเนเจอร์ + ตรวจว่า S2HostService ไม่ได้อ่าน DevCommands เลย
        /// </summary>
        private static void WriteGuardTests()
        {
            int matches = 0, accepts = 0, rejects = 0, described = 0;
            for (int mask = 0; mask < 64; mask++)
            {
                bool[] flags = new bool[6];
                for (int bit = 0; bit < 6; bit++) flags[bit] = (mask & (1 << bit)) != 0;

                bool anyOn = flags.Any(x => x);
                bool got = Reject(flags);
                if (got == !anyOn) matches++;
                if (got) accepts++; else rejects++;

                string expected = "none";
                for (int bit = 0; bit < 6; bit++)
                {
                    if (!flags[bit]) continue;
                    expected = expected == "none" ? PrefNames[bit] : expected + "+" + PrefNames[bit];
                }
                if (DescribeFlags(flags) == expected) described++;
            }

            Check("write guard: all 64 pref combinations follow the truth table", matches == 64);
            Check("write guard: exactly 1 combination passes (all six false)", accepts == 1 && rejects == 63);
            Check("write guard: Describe names exactly the prefs that are on", described == 64);

            Check("write guard: all-off passes", S2WriteGuard.WritePathsClosed(false, false, false, false, false, false));
            Check("write guard: AllowRemoteInput alone rejects",
                !S2WriteGuard.WritePathsClosed(true, false, false, false, false, false));
            for (int bit = 1; bit < 6; bit++)
            {
                bool[] only = new bool[6];
                only[bit] = true;
                Check("write guard: " + PrefNames[bit] + " alone rejects", !Reject(only));
            }
            Check("write guard: Describe lists the open prefs",
                DescribeFlags(new bool[] { true, false, false, false, false, true })
                    == "AllowRemoteInput+JitProbeW7"
                && DescribeFlags(new bool[6]) == "none");

            MethodInfo m = typeof(S2WriteGuard).GetMethod("WritePathsClosed");
            Check("write guard method exists and is static", m != null && m.IsStatic);
            if (m != null)
            {
                ParameterInfo[] ps = m.GetParameters();
                Check("write guard takes exactly 6 bools and returns bool",
                    ps.Length == 6 && ps.All(p => p.ParameterType == typeof(bool))
                    && m.ReturnType == typeof(bool));
                Check("write guard has no DevCommands parameter (DevCommands cannot affect it)",
                    !ps.Any(p => p.Name.IndexOf("dev", StringComparison.OrdinalIgnoreCase) >= 0));
            }

            string root = RepoRoot();
            Check("repo root found for the S2HostService guard source check", root != null);
            if (root == null) return;

            string service = File.ReadAllText(Path.Combine(root, "src", "LongYinCoop", "S2", "S2HostService.cs"));
            Check("S2HostService gating delegates to the pure guard with all 6 prefs",
                service.Contains("S2WriteGuard.WritePathsClosed(CoopPrefs.AllowRemoteInput.Value")
                && service.Contains("CoopPrefs.JitProbeW7.Value"));
            Check("S2HostService code never reads DevCommands (rev4: it must not gate on it)",
                !CodeOnly(service).Contains("DevCommands"));
        }

        // ---------------------------------------------------------------- unitAlive mirror (rev4)

        /// <summary>
        /// มติผู้ใช้ 2026-10-21 ข้อ 2 = ทาง (ii): คงฟิลด์ `ReplicaHash.UnitAlive` ไว้ (ห้ามแก้ hash spec)
        /// และให้ sampler ใส่ค่า = `u.IsAlive` (สำเนา, ล้าหลัง hp ≤1 เฟรม) ผ่านกติกา pure
        /// </summary>
        private static void UnitAliveMirrorTests()
        {
            Check("mirror rule is an identity: MirrorUnitAlive(true/false) = true/false",
                S2UnitRule.MirrorUnitAlive(true) && !S2UnitRule.MirrorUnitAlive(false));

            FieldInfo unitField = typeof(S2Unit).GetField("UnitAlive");
            Check("S2Unit carries UnitAlive as a bool (kept for ReplicaHash.UnitAlive)",
                unitField != null && unitField.FieldType == typeof(bool));

            FieldInfo replicaField = typeof(ReplicaUnit).GetField("UnitAlive");
            Check("ReplicaUnit.UnitAlive is untouched (hash spec frozen)",
                replicaField != null && replicaField.FieldType == typeof(bool));

            string root = RepoRoot();
            Check("repo root found for the sampler source check", root != null);
            if (root == null) return;

            string sampler = File.ReadAllText(Path.Combine(root, "src", "LongYinCoop", "S2", "Host", "S2Sampler.cs"));
            Check("S2Sampler fills UnitAlive from IsAlive via the mirror rule",
                sampler.Contains("UnitAlive = S2UnitRule.MirrorUnitAlive(u.IsAlive)"));
            Check("S2Sampler code reads no second death flag (no hero.dead, no IsDead)",
                !CodeOnly(sampler).Contains("hero.dead") && !CodeOnly(sampler).Contains("IsDead"));
        }

        // ---------------------------------------------------------------- ndjson writer

        private static void NdjsonWriterTests()
        {
            string path = Path.Combine(_tmp, "snapshots.ndjson");
            S2NdjsonWriter w = new S2NdjsonWriter(path, flushEvery: 1);
            w.Write(Frame(1));
            w.WriteRawLine("{\"kind\":\"phase.start\",\"marker\":true,\"block=A\"}");
            Check("writer counts both frames and markers", w.Lines == 2);
            w.Dispose();

            string[] lines = File.ReadAllLines(path);
            Check("writer produced one line per record", lines.Length == 2);
            Check("frame line is json-ish with units", lines[0].StartsWith("{") && lines[0].EndsWith("}")
                && lines[0].Contains("\"units\":[") && lines[0].Contains("\"unitsSum\"")
                && lines[0].Contains("\"sampleMs\"") && lines[0].Contains("\"unitAlive\":"));
            Check("marker line preserved", lines[1].Contains("phase.start") && lines[1].Contains("block=A"));

            S2NdjsonWriter w2 = new S2NdjsonWriter(path, flushEvery: 1, append: true);
            w2.WriteRawLine("{\"kind\":\"phase.idle\",\"marker\":true,\"block=B\"}");
            w2.Dispose();
            Check("append keeps previous evidence", File.ReadAllLines(path).Length == 3);

            Check("ndjson frame line has no secret-ish fields",
                !lines[0].Contains("secret") && !lines[0].Contains("coop.secret"));
        }

        // ---------------------------------------------------------------- TurnGate guard (D1 reverted)

        private static string RepoRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "src", "LongYinCoop", "S1.5", "TurnGate.cs")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }

        /// <summary>โค้ดจริงของไฟล์ C# (ตัดบรรทัดคอมเมนต์ `//` `///` `*` และ inline `//` ออก)</summary>
        private static string CodeOnly(string source)
        {
            StringBuilder sb = new StringBuilder(source.Length);
            foreach (string raw in source.Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith("//", StringComparison.Ordinal)
                    || line.StartsWith("/*", StringComparison.Ordinal)
                    || line.StartsWith("*", StringComparison.Ordinal))
                    continue;
                int cut = line.IndexOf("//", StringComparison.Ordinal);
                if (cut >= 0) line = line.Substring(0, cut);
                sb.Append(line).Append('\n');
            }
            return sb.ToString();
        }

        private static string Sha256Hex(byte[] bytes)
        {
            byte[] hash = SHA256.HashData(bytes);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        /// <summary>
        /// D1 ถูกยกเลิก (มติผู้ใช้ 2026-10-21): `TurnGate.cs` ต้องเหมือน 57e68dc ทุกไบต์
        /// และ `unitAlive` ที่ `spike status` รายงานคือ `BattleUnit.IsAlive` อยู่แล้ว
        /// -> replica เทียบเกณฑ์ exit 4 ได้ด้วย `isAlive` (S2) เทียบกับ `unitAlive` (spike status)
        /// </summary>
        private static void TurnGateGuardTests()
        {
            string root = RepoRoot();
            Check("repo root found for the TurnGate guard", root != null);
            if (root == null) return;

            string turnGatePath = Path.Combine(root, "src", "LongYinCoop", "S1.5", "TurnGate.cs");
            string fixturePath = Path.Combine(AppContext.BaseDirectory, "fixtures", "TurnGate.before.cs.txt");
            if (!File.Exists(fixturePath))
                fixturePath = Path.Combine(root, "tests", "S2Protocol.Tests", "fixtures", "TurnGate.before.cs.txt");
            Check("pre-edit TurnGate fixture exists", File.Exists(fixturePath));
            if (!File.Exists(fixturePath)) return;

            byte[] nowBytes = File.ReadAllBytes(turnGatePath);
            byte[] beforeBytes = File.ReadAllBytes(fixturePath);

            Check("fixture is the pinned 57e68dc file (sha256 recorded in step-0 evidence)",
                Sha256Hex(beforeBytes) == "737d48941c3707cef4aea1ef4ccae26163f5fe05f28cd065ae246b84854523ca");
            Check("TurnGate.cs matches the 57e68dc fixture byte-exact",
                nowBytes.Length == beforeBytes.Length && nowBytes.AsSpan().SequenceEqual(beforeBytes));

            string text = File.ReadAllText(turnGatePath);
            Check("TurnGate carries no D1 leftovers (no new isAlive field)",
                !text.Contains("isAlive") && !text.Contains("S2a-2 D1"));
            Check("spike status still exposes unitAlive (exit 4 compare target)",
                text.Split('\n').Count(l => l.Contains("unitAlive = SafeUnitAlive(u)")) == 1);
            Check("SafeUnitAlive reads BattleUnit.IsAlive (typed, read-only)",
                text.Contains("return u != null && u.IsAlive;"));
            Check("hash preimage lines exist and gained no isAlive field",
                text.Split('\n').Count(l => l.Contains("HashText.Append")) > 5
                && !text.Split('\n').Any(l => l.Contains("HashText.Append") && l.Contains("isAlive")));
        }
    }
}
