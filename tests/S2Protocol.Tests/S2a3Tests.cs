using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using LongYinCoop.S2.Host;
using LongYinCoop.S2.Protocol;
using LongYinCoop.S2.Transport;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// S2a-3 (C1) T0/T1 — ไม่ใช้เกม ไม่มี mod runtime (socket จริงเฉพาะ 127.0.0.1)
    ///
    /// U*: pure/frame-level (stream, seq, resync, handshake, bind, hash vectors, wire v2, bandwidth)
    /// I*: integration บน 127.0.0.1 (listen/auth/snapshot, backpressure, kill, reconnect,
    ///     forbidden frames, tcp self-test, resync on gap, second client)
    /// </summary>
    internal static class S2a3Tests
    {
        private static int _pass;
        private static int _fail;
        private static string _tmp;

        public static int Run()
        {
            _pass = 0;
            _fail = 0;
            _tmp = Path.Combine(Path.GetTempPath(), "s2a3-tests-" + Environment.ProcessId);
            Directory.CreateDirectory(_tmp);

            Console.WriteLine();
            Console.WriteLine("== S2a-3 (C1) tests ==");
            try
            {
                U1FrameStream();
                U2StreamMalformed();
                U3ChunkOverStream();
                U4SeqTracker();
                U5ResyncState();
                U6Handshake();
                U7BindPolicy();
                U8HashVectors();
                U9WireRoundtrip();
                U10Bandwidth();
                I1TcpSession();
                I2StallNoBlock();
                I3KillMidChunk();
                I4Reconnect();
                I5Forbidden();
                I6TcpSelfTest();
                I7ResyncOnGap();
                I8SecondClient();
            }
            finally
            {
                try { Directory.Delete(_tmp, true); } catch { }
            }

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "S2a-3 RESULT {0} (pass {1} fail {2})", _fail == 0 ? "PASS" : "FAIL", _pass, _fail));
            return _fail;
        }

        // ------------------------------------------------------------------ helpers

        private static void Check(string name, bool ok)
        {
            if (ok) _pass++;
            else _fail++;
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "[{0}] {1}", ok ? "PASS" : "FAIL", name));
        }

        private static void CheckEq(string name, long expected, long actual)
        {
            Check(name + " (expected " + expected + ", got " + actual + ")", expected == actual);
        }

        private static void Info(string text)
        {
            Console.WriteLine("      " + text);
        }

        private static void Section(string name)
        {
            Console.WriteLine("-- " + name);
        }

        private const string SecretHex = "9f2c7a1b3d5e4f60718293a4b5c6d7e8f90a1b2c3d4e5f60718293a4b5c6d7e8";

        private static byte[] SecretBytes()
        {
            return S2Auth.BytesFromHex(SecretHex);
        }

        private static AssembledMessage Message(MessageType type, byte[] payload, byte ver = 0)
        {
            return new AssembledMessage(type, ver == 0 ? S2Protocol.ProtoVer : ver, 0,
                                        payload ?? Array.Empty<byte>());
        }

        /// <summary>อ่าน frame ภายในเวลาที่กำหนด (ใช้กับ TcpTransport ฝั่ง client)</summary>
        private static bool WaitFrame(TcpTransport transport, int timeoutMs, out byte[] frame)
        {
            long until = Environment.TickCount64 + Math.Max(1, timeoutMs);
            while (Environment.TickCount64 < until)
            {
                if (transport.TryReceive(out frame)) return true;
                Thread.Sleep(2);
            }
            frame = null;
            return false;
        }

        private static byte[] Concat(params byte[][] parts)
        {
            int total = 0;
            for (int i = 0; i < parts.Length; i++) total += parts[i].Length;
            byte[] result = new byte[total];
            int offset = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                Buffer.BlockCopy(parts[i], 0, result, offset, parts[i].Length);
                offset += parts[i].Length;
            }
            return result;
        }

        // ------------------------------------------------------------------ U1 / U2

        private static void U1FrameStream()
        {
            Section("U1 TcpFrameStream: fragmentation/coalescing/limits");

            byte[] a = FrameCodec.Encode(MessageType.Ping, S2Protocol.ProtoVer, 0, new byte[8]);
            byte[] b = FrameCodec.Encode(MessageType.Pong, S2Protocol.ProtoVer, 0, new byte[16]);
            byte[] c = FrameCodec.Encode(MessageType.Snapshot, S2Protocol.ProtoVer, 7, new byte[900]);
            byte[] all = Concat(a, b, c);

            TcpFrameStream stream = new TcpFrameStream();
            int produced = 0;
            bool feedOk = true;
            for (int i = 0; i < all.Length; i++)
            {
                string feedError;
                if (!stream.Feed(all.AsSpan(i, 1), out feedError))
                {
                    feedOk = false;
                    break;
                }
                while (true)
                {
                    byte[] frame;
                    string error;
                    bool got = stream.TryTakeFrame(out frame, out error);
                    if (error != null)
                    {
                        feedOk = false;
                        break;
                    }
                    if (!got) break;
                    produced++;
                }
            }
            Check("1 byte at a time: no framing error", feedOk);
            CheckEq("1 byte at a time: 3 frames", 3, produced);
            CheckEq("buffer empty after complete frames", 0, stream.Buffered);

            stream = new TcpFrameStream();
            string err;
            Check("whole buffer fed", stream.Feed(all, out err));
            int count = 0;
            byte[] first = null;
            while (true)
            {
                byte[] frame;
                string error;
                bool got = stream.TryTakeFrame(out frame, out error);
                if (error != null || !got) break;
                if (count == 0) first = frame;
                count++;
            }
            CheckEq("coalesced buffer: 3 frames", 3, count);
            Check("coalesced frames are byte-identical", first != null && first.SequenceEqual(a));

            byte[] maxFrame = FrameCodec.Encode(MessageType.Snapshot, S2Protocol.ProtoVer, 1,
                                                new byte[S2Protocol.MaxPayloadBytes]);
            CheckEq("max frame is 1100 B", S2Protocol.MaxFrameBytes, maxFrame.Length);
            TcpFrameStream maxStream = new TcpFrameStream();
            maxStream.Feed(maxFrame, out err);
            byte[] taken;
            string maxError;
            bool maxOk = maxStream.TryTakeFrame(out taken, out maxError);
            Check("1100 B frame accepted", maxOk && maxError == null && taken != null && taken.Length == 1100);

            TcpFrameStream partial = new TcpFrameStream();
            partial.Feed(all.AsSpan(0, 3), out err);
            byte[] none;
            string partialError;
            bool noneYet = partial.TryTakeFrame(out none, out partialError);
            Check("3 bytes: no frame yet and no error", !noneYet && partialError == null && partial.Buffered == 3);
            partial.Feed(all.AsSpan(3), out err);
            bool now = partial.TryTakeFrame(out none, out partialError);
            Check("rest of the stream completes the frame", now && partialError == null
                                                          && none != null && none.SequenceEqual(a));

            TcpFrameStream overrun = new TcpFrameStream();
            bool overrunError = false;
            for (int i = 0; i < 6; i++)
            {
                string e;
                if (!overrun.Feed(new byte[1024], out e)) { overrunError = true; break; }
            }
            Check("> 4096 B of unsplittable data is an error (fail-closed)", overrunError);
        }

        private static void U2StreamMalformed()
        {
            Section("U2 TcpFrameStream: fail-closed on bad header");

            TcpFrameStream s1 = new TcpFrameStream();
            string err;
            s1.Feed(new byte[] { 0x00, 0x05, 0x21, 0x02, 0, 0, 0, 1 }, out err);
            byte[] f;
            string e1;
            bool got1 = s1.TryTakeFrame(out f, out e1);
            Check("bodyLen 5 rejected", !got1 && e1 != null && e1.Contains("bodyLen"));

            TcpFrameStream s2 = new TcpFrameStream();
            s2.Feed(new byte[] { 0x10, 0x00, 0x05, 0x02, 0, 0, 0, 1 }, out err);
            string e2;
            bool got2 = s2.TryTakeFrame(out f, out e2);
            Check("bodyLen 4096 (> cap) rejected", !got2 && e2 != null && e2.Contains("over cap"));

            TcpFrameStream s3 = new TcpFrameStream();
            byte[] bad = FrameCodec.Encode(MessageType.Ping, S2Protocol.ProtoVer, 0, new byte[2]);
            bad[2] = 0x7F;
            s3.Feed(bad, out err);
            string e3;
            bool got3 = s3.TryTakeFrame(out f, out e3);
            Check("unknown type rejected", !got3 && e3 != null && e3.Contains("unknown type"));

            TcpFrameStream s4 = new TcpFrameStream();
            byte[] oldVer = FrameCodec.Encode(MessageType.Ping, 1, 0, new byte[2]);
            s4.Feed(oldVer, out err);
            string e4;
            bool got4 = s4.TryTakeFrame(out f, out e4);
            Check("proto ver 1 rejected by a v2 peer", !got4 && e4 != null && e4.Contains("proto ver"));
            s4.Reset();
            Check("reset clears the buffer", s4.Buffered == 0);
        }

        // ------------------------------------------------------------------ U3

        private static void U3ChunkOverStream()
        {
            Section("U3 chunked message over a fragmented TCP stream");

            byte[] payload = new byte[5000];
            new Random(11).NextBytes(payload);

            List<byte[]> frames = ChunkCodec.Encode(7, MessageType.Snapshot, S2Protocol.ProtoVer, 42, payload);
            Check("5000 B payload chunks into > 1 frame", frames.Count > 1);

            byte[] wire = Concat(frames.ToArray());
            TcpFrameStream stream = new TcpFrameStream();
            ChunkReassembler reassembler = new ChunkReassembler();
            AssembledMessage done = default(AssembledMessage);
            bool complete = false;
            bool framingOk = true;

            Random random = new Random(3);
            int offset = 0;
            while (offset < wire.Length)
            {
                int take = Math.Min(wire.Length - offset, random.Next(1, 400));
                string feedError;
                if (!stream.Feed(wire.AsSpan(offset, take), out feedError)) framingOk = false;
                offset += take;

                while (true)
                {
                    byte[] frame;
                    string frameError;
                    bool got = stream.TryTakeFrame(out frame, out frameError);
                    if (frameError != null) framingOk = false;
                    if (!got) break;
                    FrameHeader header;
                    byte[] body;
                    if (!FrameCodec.TryDecode(frame, out header, out body)) { framingOk = false; continue; }
                    if (reassembler.Accept(header, body, 0, out AssembledMessage assembled))
                    {
                        done = assembled;
                        complete = true;
                    }
                }
            }

            Check("no framing error with random fragmentation", framingOk);
            Check("reassembled exactly once", complete);
            Check("reassembled payload bit-identical", complete && done.Payload != null && done.Payload.SequenceEqual(payload));
            Check("assembled type/seq preserved", complete && done.Type == MessageType.Snapshot && done.Seq == 42);
        }

        // ------------------------------------------------------------------ U4 / U5

        private static void U4SeqTracker()
        {
            Section("U4 SeqTracker");

            SeqTracker tracker = new SeqTracker();
            CheckEq("first seq 1 accepted as ok", (long)SeqVerdict.Ok, (long)tracker.Observe(1));
            CheckEq("seq 2 ok", (long)SeqVerdict.Ok, (long)tracker.Observe(2));
            CheckEq("seq 3 ok", (long)SeqVerdict.Ok, (long)tracker.Observe(3));
            CheckEq("no gaps yet", 0, tracker.Gaps);

            CheckEq("seq 5 is a gap", (long)SeqVerdict.Gap, (long)tracker.Observe(5));
            CheckEq("gap size (5 - expected 4)", 1, tracker.LastGapSize);
            CheckEq("expected advances to 6", 6, tracker.ExpectedSeq);

            CheckEq("seq 5 again is a duplicate", (long)SeqVerdict.Duplicate, (long)tracker.Observe(5));
            CheckEq("duplicate counted once", 1, tracker.Duplicates);

            CheckEq("seq 0 is not data", (long)SeqVerdict.NotData, (long)tracker.Observe(0));
            CheckEq("seq 6 ok", (long)SeqVerdict.Ok, (long)tracker.Observe(6));

            CheckEq("17-message gap detected", (long)SeqVerdict.Gap, (long)tracker.Observe(24));
            CheckEq("largest gap is 17", 17, tracker.LargestGap);

            SeqTracker wrap = new SeqTracker(uint.MaxValue);
            CheckEq("uint max ok", (long)SeqVerdict.Ok, (long)wrap.Observe(uint.MaxValue));
            CheckEq("wrap to 1 (no gap)", (long)SeqVerdict.Ok, (long)wrap.Observe(1));

            SeqTracker resynced = new SeqTracker();
            resynced.Observe(1);
            resynced.ResyncTo(500);
            CheckEq("resync moves expected", 500, resynced.ExpectedSeq);
            CheckEq("seq 500 ok after resync", (long)SeqVerdict.Ok, (long)resynced.Observe(500));
            Info("seq: " + resynced.ToText());
        }

        private static void U5ResyncState()
        {
            Section("U5 ResyncState (gap window)");

            ResyncState state = new ResyncState();
            Check("no window by default", !state.WindowOpen && !state.ShouldRequest(0));

            state.OpenGap(1000, 41, "seq-gap");
            Check("window opens", state.WindowOpen);
            Check("first request is immediate", state.ShouldRequest(1000));
            state.OnRequestSent(1000);
            Check("not due again before RetryMs", !state.ShouldRequest(1200));
            Check("due again at RetryMs", state.ShouldRequest(1500));
            state.OnRequestSent(1500);
            state.OnRequestSent(2000);
            Check("3 attempts used", state.Attempts == 3 && !state.ShouldRequest(2600));
            Check("give up after the 3rd attempt + retry window", state.ShouldGiveUp(2501));

            Check("snapshot without the resync flag does not close the window",
                  !state.OnSnapshot(resyncFlag: false, hashMatch: true, nowMs: 2600) && state.WindowOpen);
            Check("resync snapshot with hash mismatch does not close",
                  !state.OnSnapshot(resyncFlag: true, hashMatch: false, nowMs: 2700) && state.WindowOpen);
            Check("resync snapshot with matching hash closes",
                  state.OnSnapshot(resyncFlag: true, hashMatch: true, nowMs: 3000) && !state.WindowOpen);
            CheckEq("one window closed", 1, state.WindowsClosed);
            CheckEq("window measured 2000 ms", 2000, state.MaxGapOpenMs);
            Info("resync: " + state.ToText());

            ResyncState second = new ResyncState();
            second.OpenGap(0, 10, "stall");
            second.OpenGap(500, 12, "stall");
            Check("re-opening a window keeps the original timer", second.OpenedAtMs == 0);
            second.AbandonGap();
            Check("abandon (reconnect) counts as give-up", second.GiveUps == 1 && !second.WindowOpen);
        }

        // ------------------------------------------------------------------ U6

        private static void U6Handshake()
        {
            Section("U6 handshake (Hello/Auth/AuthResult/Deny)");

            byte[] secret = SecretBytes();
            byte[] otherSecret = Encoding.ASCII.GetBytes("ffffffffffffffffffffffffffffffff");
            string fieldHash = S2Fields.Hash;

            S2HandshakeServer server = new S2HandshakeServer(secret, fieldHash);
            byte[] helloFrame = server.BuildHello(11, 1000);
            FrameHeader header;
            byte[] payload;
            Check("Hello frame is a valid v2 frame",
                  FrameCodec.TryDecode(helloFrame, out header, out payload)
                  && header.Type == MessageType.Hello && header.Ver == S2Protocol.ProtoVer && header.Seq == 0);

            S2HandshakeClient client = new S2HandshakeClient(secret, fieldHash);
            byte[] authFrame;
            string error;
            Check("client builds Auth from Hello",
                  client.OnHello(Message(MessageType.Hello, payload), out authFrame, out error));
            Check("Auth frame decodes", FrameCodec.TryDecode(authFrame, out header, out payload)
                                        && header.Type == MessageType.Auth && header.Seq == 0);

            byte[] reply;
            DenyCode code;
            string reason;
            HandshakeOutcome outcome = server.AcceptAuth(Message(MessageType.Auth, payload), 1100,
                                                         out reply, out code, out reason);
            CheckEq("server accepts the correct HMAC", (long)HandshakeOutcome.Accepted, (long)outcome);
            Check("server is authenticated and keeps the session id",
                  server.Authenticated && server.SessionId == 11);
            Check("reply is AuthResult", reply != null
                  && FrameCodec.TryDecode(reply, out header, out payload) && header.Type == MessageType.AuthResult);
            Check("client accepts the AuthResult", client.OnReply(Message(MessageType.AuthResult, payload), out error)
                                                   && client.Authenticated && client.SessionId == 11);
            Check("fieldHash prefix is 16 hex chars", S2Fields.Prefix.Length == 16);

            // wrong secret
            S2HandshakeServer s2 = new S2HandshakeServer(secret, fieldHash);
            byte[] hello2 = s2.BuildHello(12, 0);
            FrameCodec.TryDecode(hello2, out header, out payload);
            S2HandshakeClient wrong = new S2HandshakeClient(otherSecret, fieldHash);
            byte[] auth2;
            wrong.OnHello(Message(MessageType.Hello, payload), out auth2, out error);
            FrameCodec.TryDecode(auth2, out header, out byte[] auth2Payload);
            HandshakeOutcome bad = s2.AcceptAuth(Message(MessageType.Auth, auth2Payload), 10,
                                                 out reply, out code, out reason);
            CheckEq("wrong secret is denied", (long)HandshakeOutcome.Denied, (long)bad);
            Check("deny code is BadAuth", code == DenyCode.BadAuth);
            Check("deny frame is a Deny", reply != null
                  && FrameCodec.TryDecode(reply, out header, out byte[] denyPayload)
                  && header.Type == MessageType.Deny
                  && HandshakeCodec.TryDecodeDeny(denyPayload, out DenyCode parsed, out string denyReason, out error)
                  && parsed == DenyCode.BadAuth);
            Check("deny reason never contains the secret",
                  reason != null && !reason.Contains("9f2c7a1b"));

            // tampered tag
            S2HandshakeServer s3 = new S2HandshakeServer(secret, fieldHash);
            byte[] hello3 = s3.BuildHello(13, 0);
            FrameCodec.TryDecode(hello3, out header, out payload);
            S2HandshakeClient c3 = new S2HandshakeClient(secret, fieldHash);
            c3.OnHello(Message(MessageType.Hello, payload), out byte[] auth3, out error);
            FrameCodec.TryDecode(auth3, out header, out byte[] auth3Payload);
            auth3Payload[2] ^= 0xFF;
            HandshakeOutcome tampered = s3.AcceptAuth(Message(MessageType.Auth, auth3Payload), 10,
                                                      out reply, out code, out reason);
            Check("tampered tag denied", tampered == HandshakeOutcome.Denied && code == DenyCode.BadAuth);

            // fieldHash mismatch (guest side rejects the host)
            S2HandshakeServer s4 = new S2HandshakeServer(secret, fieldHash);
            byte[] hello4 = s4.BuildHello(14, 0);
            FrameCodec.TryDecode(hello4, out header, out payload);
            S2HandshakeClient c4 = new S2HandshakeClient(secret, new string('a', 64));
            byte[] auth4;
            Check("guest rejects a host whose fieldHash prefix differs",
                  !c4.OnHello(Message(MessageType.Hello, payload), out auth4, out error));

            // Auth for a nonce that this server never issued
            S2HandshakeServer s5 = new S2HandshakeServer(secret, fieldHash);
            byte[] hello5 = s5.BuildHello(15, 0);
            FrameCodec.TryDecode(hello5, out header, out payload);
            S2HandshakeClient c5 = new S2HandshakeClient(secret, fieldHash);
            c5.OnHello(Message(MessageType.Hello, payload), out byte[] auth5, out error);
            FrameCodec.TryDecode(auth5, out header, out byte[] auth5Payload);

            S2HandshakeServer s6 = new S2HandshakeServer(secret, fieldHash);
            s6.BuildHello(16, 0);
            HandshakeOutcome mismatched = s6.AcceptAuth(Message(MessageType.Auth, auth5Payload), 10,
                                                        out reply, out code, out reason);
            Check("Auth for another nonce is rejected",
                  mismatched == HandshakeOutcome.Denied && code == DenyCode.BadAuth);

            // wrong frame version
            S2HandshakeServer s7 = new S2HandshakeServer(secret, fieldHash);
            byte[] hello7 = s7.BuildHello(17, 0);
            FrameCodec.TryDecode(hello7, out header, out payload);
            S2HandshakeClient c7 = new S2HandshakeClient(secret, fieldHash);
            c7.OnHello(Message(MessageType.Hello, payload), out byte[] auth7, out error);
            FrameCodec.TryDecode(auth7, out header, out byte[] auth7Payload);
            HandshakeOutcome oldVer = s7.AcceptAuth(Message(MessageType.Auth, auth7Payload, ver: 1), 10,
                                                    out reply, out code, out reason);
            Check("frame ver 1 is denied", oldVer == HandshakeOutcome.Denied && code == DenyCode.BadVersion);

            // auth before hello
            S2HandshakeServer s8 = new S2HandshakeServer(secret, fieldHash);
            HandshakeOutcome early = s8.AcceptAuth(Message(MessageType.Auth, auth7Payload), 10,
                                                   out reply, out code, out reason);
            Check("Auth before Hello is WrongState", early == HandshakeOutcome.WrongState);

            // timeout
            S2HandshakeServer s9 = new S2HandshakeServer(secret, fieldHash, timeoutMs: 5000);
            byte[] hello9 = s9.BuildHello(19, 1000);
            FrameCodec.TryDecode(hello9, out header, out payload);
            S2HandshakeClient c9 = new S2HandshakeClient(secret, fieldHash);
            c9.OnHello(Message(MessageType.Hello, payload), out byte[] auth9, out error);
            FrameCodec.TryDecode(auth9, out header, out byte[] auth9Payload);
            HandshakeOutcome late = s9.AcceptAuth(Message(MessageType.Auth, auth9Payload), 6001,
                                                  out reply, out code, out reason);
            Check("late Auth expires", late == HandshakeOutcome.Expired && code == DenyCode.Timeout);

            // attempts exceeded
            S2HandshakeServer s10 = new S2HandshakeServer(secret, fieldHash);
            s10.BuildHello(20, 0);
            HandshakeOutcome last = HandshakeOutcome.Denied;
            for (int i = 0; i < 5; i++)
            {
                last = s10.AcceptAuth(Message(MessageType.Auth, new byte[] { S2Protocol.ProtoVer, 1, 2, 3 }), 10,
                                      out reply, out code, out reason);
            }
            Check("5th malformed Auth is rejected hard", last == HandshakeOutcome.AttemptsExceeded);

            // cooldown after a failed auth
            S2HandshakeServer s11 = new S2HandshakeServer(secret, fieldHash);
            s11.BuildHello(21, 1000);
            s11.AcceptAuth(Message(MessageType.Auth, new byte[] { S2Protocol.ProtoVer }), 1000,
                           out reply, out code, out reason);
            Check("new connection blocked right after a failed auth", !s11.AllowsNewConnection(2000));
            Check("new connection allowed after 5 s", s11.AllowsNewConnection(6000));

            S2HandshakeServer clean = new S2HandshakeServer(secret, fieldHash);
            CleanHandshake(clean, secret, fieldHash);
            Check("successful auth does not arm the cooldown", clean.AllowsNewConnection(1));

            // handshake messages must fit in one frame (no chunking)
            Check("Hello fits one frame", helloFrame.Length <= S2Protocol.MaxFrameBytes);
            Check("Auth fits one frame", authFrame.Length <= S2Protocol.MaxFrameBytes);
        }

        private static void CleanHandshake(S2HandshakeServer server, byte[] secret, string fieldHash)
        {
            byte[] hello = server.BuildHello(1, 0);
            FrameHeader header;
            byte[] payload;
            FrameCodec.TryDecode(hello, out header, out payload);
            S2HandshakeClient client = new S2HandshakeClient(secret, fieldHash);
            byte[] auth;
            string error;
            client.OnHello(Message(MessageType.Hello, payload), out auth, out error);
            FrameCodec.TryDecode(auth, out header, out payload);
            byte[] reply;
            DenyCode code;
            string reason;
            server.AcceptAuth(Message(MessageType.Auth, payload), 1, out reply, out code, out reason);
        }

        // ------------------------------------------------------------------ U7

        private static void U7BindPolicy()
        {
            Section("U7 BindPolicy");

            Check("127.0.0.1 ok", BindPolicy.Validate("127.0.0.1", false) == BindVerdict.Ok);
            Check("::1 ok", BindPolicy.Validate("::1", false) == BindVerdict.Ok);
            Check("127.5.5.5 ok (127/8 is loopback)", BindPolicy.Validate("127.5.5.5", false) == BindVerdict.Ok);
            Check("::ffff:127.0.0.1 ok", BindPolicy.Validate("::ffff:127.0.0.1", false) == BindVerdict.Ok);
            Check("0.0.0.0 rejected", BindPolicy.Validate("0.0.0.0", false) == BindVerdict.NotLoopback);
            Check(":: rejected", BindPolicy.Validate("::", false) == BindVerdict.NotLoopback);
            Check("* rejected", BindPolicy.Validate("*", false) == BindVerdict.NotLoopback);
            Check("192.168.1.5 rejected without approval",
                  BindPolicy.Validate("192.168.1.5", false) == BindVerdict.NotLoopback);
            Check("192.168.1.5 allowed only with allowNonLoopback",
                  BindPolicy.Validate("192.168.1.5", true) == BindVerdict.Ok);
            Check("localhost (hostname) rejected",
                  BindPolicy.Validate("localhost", false) == BindVerdict.NotLiteral);
            Check("empty rejected", BindPolicy.Validate("", false) == BindVerdict.Empty);
            Check("null rejected", BindPolicy.Validate(null, false) == BindVerdict.Empty);
            Check("0.0.0.0 is never ok even with allowNonLoopback",
                  BindPolicy.Validate("0.0.0.0", true) == BindVerdict.NotLoopback);
            Check("IsLoopbackLiteral(127.0.0.1)", BindPolicy.IsLoopbackLiteral("127.0.0.1"));
            Check("IsLoopbackLiteral(::1)", BindPolicy.IsLoopbackLiteral("::1"));
            Check("IsLoopbackLiteral(localhost) is false", !BindPolicy.IsLoopbackLiteral("localhost"));
            Info("describe(192.168.1.5) = " + BindPolicy.Describe(BindVerdict.NotLoopback));

            S2NetSettings bad = new S2NetSettings
            {
                Enabled = true, Role = "host", Secret = SecretHex, AllowNonLoopback = false,
                FileRead = true, FormatOk = true, Bind = "192.168.1.5", Port = 47800,
                SendHz = 30, HeartbeatMs = 1000, Transport = "tcp",
            };
            Check("net.cfg: tcp + non-loopback bind is invalid", !bad.IsValid);
            S2NetSettings good = bad;
            good.Bind = "127.0.0.1";
            Check("net.cfg: tcp + 127.0.0.1 is valid", good.IsValid);
        }

        // ------------------------------------------------------------------ U8

        private static void U8HashVectors()
        {
            Section("U8 replicaHash vectors (shared with the Python client)");

            string path = Path.Combine(AppContext.BaseDirectory, "fixtures", "replica-vectors.ndjson");
            if (!File.Exists(path)) path = Path.Combine("fixtures", "replica-vectors.ndjson");
            if (!File.Exists(path))
            {
                Check("fixture fixtures/replica-vectors.ndjson exists", false);
                return;
            }

            List<Vector> vectors = ReadVectors(path);
            Check("fixture parsed " + vectors.Count + " vectors", vectors.Count >= 8);

            int ok = 0;
            foreach (Vector vector in vectors)
            {
                string computed = ReplicaHash.Compute(vector.Header, vector.Units);
                if (computed == vector.Hash) ok++;
                else Info("MISMATCH " + vector.Name + " file=" + vector.Hash + " now=" + computed);
            }
            Check("all " + vectors.Count + " fixture hashes still reproduce", ok == vectors.Count);

            Check("fixture covers hp 0.0625 (round-half trap)",
                  vectors.Any(v => v.Units.Any(u => Math.Abs(u.Hp - 0.0625f) < 1e-9f)));
            Check("fixture covers 60 units", vectors.Any(v => v.Units.Length == 60));
            Check("fixture covers 0 units", vectors.Any(v => v.Units.Length == 0));
            Check("fixture covers isAlive=false with unitAlive=true",
                  vectors.Any(v => v.Units.Any(u => !u.IsAlive && u.UnitAlive)));
            Check("fixture covers paused=true", vectors.Any(v => v.Header.Paused));
            Check("fixture covers ordinal id sort (0:10 before 0:9)",
                  vectors.Any(v => v.Name == "v-sort"));

            Vector sixty = vectors.First(v => v.Name == "v-sixty");
            string a = ReplicaHash.Compute(sixty.Header, sixty.Units);
            ReplicaUnit[] reversed = sixty.Units.Reverse().ToArray();
            Check("hash is order independent (sorted by stableId)",
                  a == ReplicaHash.Compute(sixty.Header, reversed));
        }

        private sealed class Vector
        {
            public string Name;
            public ReplicaHeader Header;
            public ReplicaUnit[] Units;
            public string Hash;
        }

        /// <summary>fixtures/replica-vectors.ndjson — เขียนจาก C# (`--emit-vectors`), อ่านโดย C# และ Python</summary>
        public static string EmitVectors(string path)
        {
            List<Vector> vectors = BuildVectors();
            StringBuilder sb = new StringBuilder();
            foreach (Vector v in vectors)
            {
                sb.Append("V name=").Append(v.Name)
                  .Append(" hash=").Append(v.Hash)
                  .Append(" battleKey=").Append(Key(v.Header.BattleKey))
                  .Append(" turn=").Append(v.Header.TurnIndex.ToString(CultureInfo.InvariantCulture))
                  .Append(" active=").Append(Key(v.Header.ActiveUnitId))
                  .Append(" paused=").Append(v.Header.Paused ? "1" : "0")
                  .Append('\n');
                foreach (ReplicaUnit u in v.Units)
                {
                    sb.Append("U name=").Append(v.Name)
                      .Append(" id=").Append(Key(u.StableId))
                      .Append(" team=").Append(u.Team.ToString(CultureInfo.InvariantCulture))
                      .Append(" row=").Append(u.Row.ToString(CultureInfo.InvariantCulture))
                      .Append(" col=").Append(u.Column.ToString(CultureInfo.InvariantCulture))
                      .Append(" hp=").Append(R(u.Hp))
                      .Append(" alive=").Append(u.IsAlive ? "1" : "0")
                      .Append(" unitAlive=").Append(u.UnitAlive ? "1" : "0")
                      .Append(" moved=").Append(u.Moved ? "1" : "0")
                      .Append(" attacked=").Append(u.Attacked ? "1" : "0")
                      .Append('\n');
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return path;
        }

        private static string Key(string value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value.Replace(' ', '_');
        }

        private static string R(float value)
        {
            // G17 ของ double == ค่าจริงของ float32 แบบไม่ปัด (Python แปลงกลับได้เป๊ะ)
            return ((double)value).ToString("G17", CultureInfo.InvariantCulture);
        }

        private static List<Vector> BuildVectors()
        {
            List<Vector> list = new List<Vector>();

            list.Add(VectorOf("v-base", Header("bk-test", 3, "0:1001", false), new[]
            {
                Unit("0:1001", 0, 1, 2, 41.5f, true, false, false),
                Unit("0:1002", 0, 0, 0, 12.25f, true, true, false),
                Unit("1:2001", 1, -1, -1, 0f, false, false, true),
            }));

            list.Add(VectorOf("v-hp-midpoints", Header("bk-hp", 1, "0:1001", false), new[]
            {
                Unit("0:1001", 0, 0, 0, 0.0625f, true, false, false),
                Unit("0:1002", 0, 0, 1, 41.5625f, true, false, false),
                Unit("0:1003", 0, 0, 2, 1.0005f, true, false, false),
                Unit("0:1004", 0, 0, 3, 2.5005f, true, false, false),
                Unit("0:1005", 0, 0, 4, 0.9995f, true, false, false),
            }));

            list.Add(VectorOf("v-hp-noise", Header("bk-noise", 2, "0:1001", false), new[]
            {
                Unit("0:1001", 0, 0, 0, 0.1f, true, false, false),
                Unit("0:1002", 0, 0, 1, 0.2f, true, false, false),
                Unit("0:1003", 0, 0, 2, 0.3f, true, false, false),
                Unit("0:1004", 0, 0, 3, 1f / 3f, true, false, false),
                Unit("0:1005", 0, 0, 4, 2f / 3f, true, false, false),
            }));

            list.Add(VectorOf("v-negative", Header("bk-neg", 4, "0:1001", false), new[]
            {
                Unit("0:1001", 0, 0, 0, -0.5f, true, false, false),
                Unit("0:1002", 0, 0, 1, -0.0625f, true, false, false),
                Unit("0:1003", 0, 0, 2, 0f, false, false, false),
            }));

            list.Add(VectorOf("v-paused", Header("bk-paused", 0, "", true), new[]
            {
                Unit("0:1001", 0, 0, 0, 50f, true, false, false),
            }));

            list.Add(VectorOf("v-empty", Header("bk-empty", 0, "0:1001", false), new ReplicaUnit[0]));

            ReplicaUnit corpse = Unit("0:1001", 0, 0, 0, 0f, false, false, false);
            corpse.UnitAlive = true;   // the one-frame lag we measured [RUNTIME]
            list.Add(VectorOf("v-dead-lag", Header("bk-dead", 5, "1:2001", false), new[]
            {
                corpse,
                Unit("1:2001", 1, -1, -1, 3.5f, true, false, true),
            }));

            list.Add(VectorOf("v-sort", Header("bk-sort", 1, "0:9", false), new[]
            {
                Unit("0:9", 0, 0, 0, 1f, true, false, false),
                Unit("0:10", 0, 0, 1, 1f, true, false, false),
                Unit("0:100", 0, 0, 2, 1f, true, false, false),
                Unit("1:1", 1, 0, 3, 1f, true, false, false),
            }));

            ReplicaUnit[] sixty = new ReplicaUnit[60];
            for (int i = 0; i < 60; i++)
            {
                sixty[i] = Unit(i < 30 ? "0:" + (1000 + i) : "1:" + (2000 + i),
                                i < 30 ? 0 : 1, i % 6, i / 6,
                                37.5f - i * 0.25f + (i % 7) * 0.0625f,
                                i % 11 != 0, i % 3 != 0, i % 5 == 0);
            }
            list.Add(VectorOf("v-sixty", Header("bk-sixty", 12, "0:1000", false), sixty));

            return list;
        }

        private static Vector VectorOf(string name, ReplicaHeader header, ReplicaUnit[] units)
        {
            return new Vector
            {
                Name = name,
                Header = header,
                Units = units,
                Hash = ReplicaHash.Compute(header, units),
            };
        }

        private static ReplicaHeader Header(string battleKey, int turn, string active, bool paused)
        {
            return new ReplicaHeader
            {
                BattleKey = battleKey,
                TurnIndex = turn,
                ActiveUnitId = active,
                Paused = paused,
            };
        }

        private static ReplicaUnit Unit(string id, int team, int row, int column, float hp,
                                        bool alive, bool moved, bool attacked)
        {
            return new ReplicaUnit
            {
                StableId = id,
                Team = team,
                Row = row,
                Column = column,
                Hp = hp,
                IsAlive = alive,
                UnitAlive = alive,
                Moved = moved,
                Attacked = attacked,
            };
        }

        private static List<Vector> ReadVectors(string path)
        {
            List<Vector> list = new List<Vector>();
            Dictionary<string, Vector> byName = new Dictionary<string, Vector>(StringComparer.Ordinal);
            List<string> order = new List<string>();

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                string[] parts = line.Split(' ');
                Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int i = 1; i < parts.Length; i++)
                {
                    int eq = parts[i].IndexOf('=');
                    if (eq <= 0) continue;
                    map[parts[i].Substring(0, eq)] = parts[i].Substring(eq + 1);
                }

                if (parts[0] == "V")
                {
                    Vector v = new Vector
                    {
                        Name = map["name"],
                        Hash = map["hash"],
                        Header = new ReplicaHeader
                        {
                            BattleKey = UnKey(map["battleKey"]),
                            TurnIndex = int.Parse(map["turn"], CultureInfo.InvariantCulture),
                            ActiveUnitId = UnKey(map["active"]),
                            Paused = map["paused"] == "1",
                        },
                        Units = new ReplicaUnit[0],
                    };
                    byName[v.Name] = v;
                    order.Add(v.Name);
                    list.Add(v);
                }
                else if (parts[0] == "U")
                {
                    Vector v = byName[map["name"]];
                    List<ReplicaUnit> units = new List<ReplicaUnit>(v.Units);
                    units.Add(new ReplicaUnit
                    {
                        StableId = UnKey(map["id"]),
                        Team = int.Parse(map["team"], CultureInfo.InvariantCulture),
                        Row = int.Parse(map["row"], CultureInfo.InvariantCulture),
                        Column = int.Parse(map["col"], CultureInfo.InvariantCulture),
                        Hp = float.Parse(map["hp"], CultureInfo.InvariantCulture),
                        IsAlive = map["alive"] == "1",
                        UnitAlive = map["unitAlive"] == "1",
                        Moved = map["moved"] == "1",
                        Attacked = map["attacked"] == "1",
                    });
                    v.Units = units.ToArray();
                }
            }

            list.Sort(delegate (Vector x, Vector y)
            {
                return order.IndexOf(x.Name).CompareTo(order.IndexOf(y.Name));
            });
            return list;
        }

        private static string UnKey(string value)
        {
            if (value == null || value == "-") return string.Empty;
            return value.Replace('_', ' ');
        }

        // ------------------------------------------------------------------ U9 / U10

        private static void U9WireRoundtrip()
        {
            Section("U9 ProtoVer 2 envelope roundtrip (wire + hash)");

            S2Frame frame = FakeS2Host.SyntheticFrame(1, 1);
            frame.SessionId = 7;
            frame.HostApplyUs = 1234567890123UL;
            frame.Resync = true;
            frame.ResyncFromSeq = 41;
            frame.ReplicaHash = S2FrameCodec.ComputeReplicaHash(frame);

            byte[] payload = S2FrameCodec.Encode(frame, frame.ReplicaHash);
            S2Frame back;
            string error;
            Check("payload decodes", S2FrameCodec.TryDecode(payload, out back, out error));

            Check("sessionId survives", back != null && back.SessionId == 7);
            Check("hostApplyUs survives (varint u64)", back != null && back.HostApplyUs == 1234567890123UL);
            Check("replicaHash survives as 32 raw bytes", back != null && back.ReplicaHash == frame.ReplicaHash);
            Check("resync flag + fromSeq survive", back != null && back.Resync && back.ResyncFromSeq == 41);
            Check("guest recomputes the same hash (Div-A = 0)",
                  back != null && S2FrameCodec.ComputeReplicaHash(back) == frame.ReplicaHash);
            Check("header/units survive", back != null && back.Units.Length == 9
                                          && back.Header.BattleKey == "fake|battle|1");
            Check("hash ignores sessionId/hostApplyUs/resync (frozen spec)",
                  back != null && S2FrameCodec.ComputeReplicaHash(back) == S2FrameCodec.ComputeReplicaHash(frame));

            int mismatches = 0;
            int decoded = 0;
            TcpFrameStream stream = new TcpFrameStream();
            ChunkReassembler reassembler = new ChunkReassembler();
            for (int n = 1; n <= 100; n++)
            {
                S2Frame source = FakeS2Host.SyntheticFrame(n, (uint)n);
                source.SessionId = 9;
                source.HostApplyUs = (ulong)(1000000 + n * 700);
                source.ReplicaHash = S2FrameCodec.ComputeReplicaHash(source);
                byte[] body = S2FrameCodec.Encode(source, source.ReplicaHash);
                List<byte[]> chunks = ChunkCodec.Encode((ushort)n, MessageType.Snapshot,
                                                        S2Protocol.ProtoVer, (uint)n, body);
                string feedError;
                if (!stream.Feed(Concat(chunks.ToArray()), out feedError)) { mismatches++; break; }

                while (true)
                {
                    byte[] rawFrame;
                    string frameError;
                    bool got = stream.TryTakeFrame(out rawFrame, out frameError);
                    if (frameError != null) { mismatches++; break; }
                    if (!got) break;
                    FrameHeader header;
                    byte[] chunkBody;
                    if (!FrameCodec.TryDecode(rawFrame, out header, out chunkBody)) { mismatches++; continue; }
                    AssembledMessage assembled;
                    if (!reassembler.Accept(header, chunkBody, n * 100, out assembled)) continue;
                    S2Frame received;
                    string decodeError;
                    if (!S2FrameCodec.TryDecode(assembled.Payload, out received, out decodeError))
                    {
                        mismatches++;
                        continue;
                    }
                    decoded++;
                    if (S2FrameCodec.ComputeReplicaHash(received) != source.ReplicaHash) mismatches++;
                    if (assembled.Seq != (uint)n) mismatches++;
                }
            }
            CheckEq("100 synthetic frames decoded", 100, decoded);
            CheckEq("Div-A/seq mismatches over 100 frames", 0, mismatches);
            Info("payload size for 9 units = " + payload.Length + " B");
        }

        private static void U10Bandwidth()
        {
            Section("U10 bandwidth projection (X2 — live measurement happens in C4/C5)");

            int[] counts = { 9, 18, 24, 60 };
            foreach (int count in counts)
            {
                S2Frame frame = SyntheticUnits(count);
                frame.ReplicaHash = S2FrameCodec.ComputeReplicaHash(frame);
                byte[] payload = S2FrameCodec.Encode(frame, frame.ReplicaHash);
                int chunks = ChunkCodec.Encode(1, MessageType.Snapshot, S2Protocol.ProtoVer, 1, payload).Count;
                int wire = payload.Length + 8 + (chunks - 1) * 7;
                double bytesPerSecond = wire * 30.0;
                Info(string.Format(CultureInfo.InvariantCulture,
                    "units={0} payload={1} B chunks={2} wire={3} B @30Hz={4:F1} KB/s ({5})",
                    count, payload.Length, chunks, wire, bytesPerSecond / 1024.0,
                    bytesPerSecond > 30 * 1024 ? "OVER 30 KB/s" : "under"));
            }

            S2Frame nine = SyntheticUnits(9);
            nine.ReplicaHash = S2FrameCodec.ComputeReplicaHash(nine);
            double nineKBps = (S2FrameCodec.Encode(nine, nine.ReplicaHash).Length + 8) * 30.0 / 1024.0;
            Check("9 units at 30 Hz stays under 30 KB/s", nineKBps < 30.0);

            S2Frame sixty = SyntheticUnits(60);
            sixty.ReplicaHash = S2FrameCodec.ComputeReplicaHash(sixty);
            double sixtyKBps = (S2FrameCodec.Encode(sixty, sixty.ReplicaHash).Length + 8) * 30.0 / 1024.0;
            Check("60 units at 30 Hz is over 30 KB/s (=> X2 at most PASS-with-limitation)",
                  sixtyKBps > 30.0);
            Info(string.Format(CultureInfo.InvariantCulture,
                "projection: 9 units {0:F1} KB/s · 60 units {1:F1} KB/s (criterion 30 KB/s)",
                nineKBps, sixtyKBps));
        }

        private static S2Frame SyntheticUnits(int count)
        {
            S2Frame frame = FakeS2Host.SyntheticFrame(1, 1);
            S2Unit[] units = new S2Unit[count];
            for (int i = 0; i < count; i++)
            {
                S2Unit u = frame.Units[i % frame.Units.Length];
                u.StableId = (i < count / 2 ? "0:" : "1:") + (1000 + i);
                u.Team = i < count / 2 ? 0 : 1;
                u.Row = i % 8;
                u.Column = i / 8;
                units[i] = u;
            }
            frame.Units = units;
            frame.Header.UnitCount = count;
            frame.Audit = S2AuditUtil.Compute(frame.Header, units);
            return frame;
        }

        // ------------------------------------------------------------------ guest + host runners

        /// <summary>guest ฝั่งเทสต์: TcpTransport + handshake + seq/resync (ไม่ใช้ Python)</summary>
        private sealed class Guest : IDisposable
        {
            public TcpTransport Transport;
            public S2HandshakeClient Handshake;
            public SeqTracker Seq = new SeqTracker();
            public ResyncState Resync = new ResyncState();
            public ChunkReassembler Reassembler = new ChunkReassembler();
            public uint SessionId;
            public bool Authenticated;

            public static Guest Connect(int port, byte[] secret, out string error)
            {
                Guest guest = new Guest();
                guest.Transport = TcpTransport.Connect("127.0.0.1", port, out error, 2000);
                if (guest.Transport == null) return null;
                guest.Handshake = new S2HandshakeClient(secret, S2Fields.Hash);
                return guest;
            }

            public bool Authenticate(int timeoutMs, out string error)
            {
                error = null;
                if (Authenticated) return true;
                long until = Environment.TickCount64 + timeoutMs;
                while (Environment.TickCount64 < until)
                {
                    byte[] raw;
                    if (!WaitFrame(Transport, 50, out raw)) continue;
                    FrameHeader header;
                    byte[] payload;
                    if (!FrameCodec.TryDecode(raw, out header, out payload)) continue;

                    if (header.Type == MessageType.Hello)
                    {
                        byte[] auth;
                        string helloError;
                        if (!Handshake.OnHello(Message(MessageType.Hello, payload), out auth, out helloError))
                        {
                            error = helloError;
                            return false;
                        }
                        SessionId = Handshake.SessionId;
                        Transport.Send(auth);
                        continue;
                    }
                    if (header.Type == MessageType.AuthResult || header.Type == MessageType.Deny)
                    {
                        if (!Handshake.OnReply(Message(header.Type, payload), out error)) return false;
                        Authenticated = Handshake.Authenticated;
                        SessionId = Handshake.SessionId;
                        return Authenticated;
                    }
                }
                error = "handshake timeout";
                return false;
            }

            /// <summary>คืน snapshot ถัดไป (ข้าม control frame) · error = "divA"/"timeout"/...</summary>
            public S2Frame NextSnapshot(int timeoutMs, out string error)
            {
                error = null;
                long until = Environment.TickCount64 + timeoutMs;
                while (Environment.TickCount64 < until)
                {
                    byte[] raw;
                    if (!WaitFrame(Transport, 50, out raw)) continue;
                    FrameHeader header;
                    byte[] payload;
                    if (!FrameCodec.TryDecode(raw, out header, out payload))
                    {
                        error = "bad frame";
                        return null;
                    }
                    if (header.Type == MessageType.Deny) { error = "denied"; return null; }
                    if (header.Type == MessageType.Bye) { error = "bye"; return null; }

                    if (header.Type == MessageType.Chunk)
                    {
                        AssembledMessage assembled;
                        if (!Reassembler.Accept(header, payload, Environment.TickCount64, out assembled)) continue;
                        if (assembled.Type != MessageType.Snapshot) continue;
                        header = new FrameHeader(assembled.Type, assembled.Ver, assembled.Seq, 0,
                                                 assembled.Payload.Length, 0);
                        payload = assembled.Payload;
                    }
                    if (header.Type != MessageType.Snapshot) continue;

                    S2Frame frame;
                    string decodeError;
                    if (!S2FrameCodec.TryDecode(payload, out frame, out decodeError))
                    {
                        error = decodeError;
                        return null;
                    }
                    frame.Seq = header.Seq;
                    frame.PayloadBytes = payload.Length;
                    Seq.Observe(header.Seq);
                    if (frame.ReplicaHash != S2FrameCodec.ComputeReplicaHash(frame))
                    {
                        error = "divA";
                        return frame;
                    }
                    return frame;
                }
                error = "timeout";
                return null;
            }

            public void Dispose()
            {
                if (Transport != null) Transport.Close();
            }
        }

        /// <summary>รัน <see cref="FakeS2Host"/> บนเธรดแยก (ทำตัวเป็น main thread ของ host)</summary>
        private sealed class HostHandle : IDisposable
        {
            public int Port;
            public FakeS2Host.Summary Summary;
            public Thread Thread;
            public Exception Error;

            public static HostHandle Start(int seconds, int sendHz, int skipSeqAt = -1)
            {
                HostHandle handle = new HostHandle();
                handle.Thread = new Thread(delegate ()
                {
                    try
                    {
                        handle.Summary = FakeS2Host.Run(0, SecretHex, seconds, sendHz, skipSeqAt, -1,
                            delegate (FakeS2Host.Summary s) { handle.Summary = s; handle.Port = s.Port; }, null);
                    }
                    catch (Exception e)
                    {
                        handle.Error = e;
                    }
                })
                { IsBackground = true, Name = "FakeS2Host" };
                handle.Thread.Start();

                long until = Environment.TickCount64 + 3000;
                while (handle.Port == 0 && handle.Error == null && Environment.TickCount64 < until) Thread.Sleep(5);
                return handle;
            }

            public void Dispose()
            {
                if (Summary != null) Summary.StopRequested = true;
                try { if (Thread != null && Thread.IsAlive) Thread.Join(2000); } catch { }
            }
        }

        // ------------------------------------------------------------------ I1

        private static void I1TcpSession()
        {
            Section("I1 listen/accept/handshake/snapshots over 127.0.0.1");

            using (HostHandle host = HostHandle.Start(20, 60))
            {
                Check("host listened (" + (host.Error == null ? "ok" : host.Error.GetType().Name) + ")",
                      host.Port > 0);
                if (host.Port == 0) return;

                byte[] secret = SecretBytes();
                string error;
                Guest guest = Guest.Connect(host.Port, secret, out error);
                Check("guest connected (" + (error ?? "ok") + ")", guest != null);
                if (guest == null) return;

                try
                {
                    Check("guest authenticated", guest.Authenticate(5000, out error));
                    Check("guest got a session id", guest.SessionId > 0);

                    int received = 0;
                    int divA = 0;
                    uint firstSeq = 0;
                    for (int n = 0; n < 100; n++)
                    {
                        string snapError;
                        S2Frame frame = guest.NextSnapshot(3000, out snapError);
                        if (frame == null)
                        {
                            Info("stopped at " + received + ": " + snapError);
                            break;
                        }
                        if (snapError == "divA") divA++;
                        if (received == 0) firstSeq = frame.Seq;
                        received++;
                    }
                    CheckEq("100 snapshots received", 100, received);
                    CheckEq("Div-A mismatches", 0, divA);
                    CheckEq("seq 1..100 with no gap", 0, guest.Seq.Gaps);
                    CheckEq("seq 1..100 with no duplicate", 0, guest.Seq.Duplicates);
                    CheckEq("first snapshot carries seq 1", 1, firstSeq);
                    Info("guest seq: " + guest.Seq.ToText());
                }
                finally
                {
                    guest.Dispose();
                }
            }
        }

        // ------------------------------------------------------------------ I2

        private static void I2StallNoBlock()
        {
            Section("I2 slow/stalled guest never blocks the host transport");

            string error;
            TcpTransport host = TcpTransport.Listen("127.0.0.1", 0, out error);
            if (host == null)
            {
                Check("listener started (" + error + ")", false);
                return;
            }

            try
            {
                TcpTransport guest = TcpTransport.Connect("127.0.0.1", host.Port, out error, 2000);
                Check("guest connected", guest != null);
                if (guest == null) return;

                long until = Environment.TickCount64 + 2000;
                while (!host.TakeNewConnection() && Environment.TickCount64 < until) Thread.Sleep(2);

                // the guest never reads; the host keeps pushing ~1 KB chunks
                S2Frame frame = SyntheticUnits(18);
                frame.ReplicaHash = S2FrameCodec.ComputeReplicaHash(frame);
                List<byte[]> chunks = ChunkCodec.Encode(1, MessageType.Snapshot, S2Protocol.ProtoVer, 1,
                                                        S2FrameCodec.Encode(frame, frame.ReplicaHash));

                long worstMs = 0;
                long started = Environment.TickCount64;
                for (int i = 0; i < 4000; i++)
                {
                    long t0 = Environment.TickCount64;
                    for (int c = 0; c < chunks.Count; c++) host.Send(chunks[c]);
                    long took = Environment.TickCount64 - t0;
                    if (took > worstMs) worstMs = took;
                }
                long elapsed = Environment.TickCount64 - started;

                Check("4000 send bursts finished < 2 s", elapsed < 2000);
                Check("worst single Send() < 10 ms (non-blocking contract)", worstMs < 10);
                Check("outbox drops counted (bounded queue)", host.Stats.SendDrops > 0);
                Info("elapsed=" + elapsed + " ms worstSend=" + worstMs + " ms drops=" + host.Stats.SendDrops
                     + " queued=" + host.Queued + " written=" + host.Stats.FramesSent);
                guest.Close();
            }
            finally
            {
                host.Close();
            }
        }

        // ------------------------------------------------------------------ I3

        private static void I3KillMidChunk()
        {
            Section("I3 guest killed mid-message — host survives and re-accepts");

            using (HostHandle host = HostHandle.Start(20, 30))
            {
                Check("host listened", host.Port > 0);
                if (host.Port == 0) return;

                string error;
                TcpTransport doomed = TcpTransport.Connect("127.0.0.1", host.Port, out error, 2000);
                Check("doomed guest connected", doomed != null);
                if (doomed == null) return;

                byte[] big = new byte[4000];
                new Random(5).NextBytes(big);
                List<byte[]> chunks = ChunkCodec.Encode(9, MessageType.Snapshot, S2Protocol.ProtoVer, 1, big);
                Check("message has > 1 chunk", chunks.Count > 1);
                doomed.Send(chunks[0]);
                Thread.Sleep(80);
                doomed.Close();
                Thread.Sleep(250);

                Check("host thread is still alive", host.Error == null);
                Guest second = Guest.Connect(host.Port, SecretBytes(), out error);
                Check("a new guest can connect after the kill (" + (error ?? "ok") + ")", second != null);
                if (second != null)
                {
                    string authError;
                    Check("new guest completes the handshake", second.Authenticate(5000, out authError));
                    string snapError;
                    Check("new guest still receives snapshots after the kill",
                          second.NextSnapshot(3000, out snapError) != null);
                    second.Dispose();
                }
            }
        }

        // ------------------------------------------------------------------ I4

        private static void I4Reconnect()
        {
            Section("I4 reconnect x3 (new session each time, seq restarts at 1)");

            using (HostHandle host = HostHandle.Start(25, 60))
            {
                Check("host listened", host.Port > 0);
                if (host.Port == 0) return;

                List<uint> sessions = new List<uint>();
                List<uint> firstSeqs = new List<uint>();

                for (int round = 1; round <= 3; round++)
                {
                    string error;
                    Guest guest = Guest.Connect(host.Port, SecretBytes(), out error);
                    Check("round " + round + ": connected", guest != null);
                    if (guest == null) break;

                    string authError;
                    Check("round " + round + ": authenticated", guest.Authenticate(5000, out authError));

                    uint firstSeq = 0;
                    for (int n = 0; n < 5; n++)
                    {
                        string snapError;
                        S2Frame frame = guest.NextSnapshot(3000, out snapError);
                        if (frame == null) break;
                        if (n == 0) firstSeq = frame.Seq;
                    }
                    sessions.Add(guest.SessionId);
                    firstSeqs.Add(firstSeq);
                    CheckEq("round " + round + ": session id " + guest.SessionId, guest.SessionId > 0 ? 1 : 0, 1);

                    guest.Transport.Send(S2ControlHandler.BuildBye());
                    Thread.Sleep(120);
                    guest.Dispose();
                    Thread.Sleep(200);
                }

                CheckEq("3 successful sessions", 3, sessions.Count);
                Check("session ids increase across reconnects",
                      sessions.Count == 3 && sessions[0] < sessions[1] && sessions[1] < sessions[2]);
                Check("every session restarts at seq 1",
                      firstSeqs.Count == 3 && firstSeqs.All(s => s == 1));
                Info("sessions = " + string.Join(",", sessions.Select(s => s.ToString())));
            }
        }

        // ------------------------------------------------------------------ I5

        private static void I5Forbidden()
        {
            Section("I5 forbidden frames from a guest (exit-6 baseline, in-proc)");

            S2Counters counters = new S2Counters();
            S2GuestGate gate = new S2GuestGate(counters);
            LoopbackTransport hostEnd, guestEnd;
            LoopbackTransport.CreatePair(out hostEnd, out guestEnd);

            List<byte[]> frames = S2SelfTest.BuildFrames();
            for (int i = 0; i < frames.Count; i++) guestEnd.Send(frames[i]);

            int handled = 0;
            while (hostEnd.TryReceive(out byte[] raw))
            {
                gate.Accept(raw, 0, out AssembledMessage ignored, out string reason);
                handled++;
            }
            CheckEq("every injected frame was handled", frames.Count, handled);
            CheckEq("forbiddenTypeDrops = 3", 3, counters.ForbiddenTypeDrops);
            CheckEq("commandsAccepted = 0", 0, counters.CommandsAccepted);
            CheckEq("controlFramesHandled = 0", 0, counters.ControlFramesHandled);
            CheckEq("no malformed drops", 0, counters.MalformedDrops);
            Check("S2SelfTest.MeetsExit6 agrees",
                  S2SelfTest.MeetsExit6(counters.ForbiddenTypeDrops, counters.CommandsAccepted, true));
            hostEnd.Close();
            guestEnd.Close();

            S2Counters c2 = new S2Counters();
            S2GuestGate gate2 = new S2GuestGate(c2);
            LoopbackTransport h2, g2;
            LoopbackTransport.CreatePair(out h2, out g2);
            byte[] big = new byte[3000];
            List<byte[]> chunks = ChunkCodec.Encode(1, MessageType.BattleEvent, S2Protocol.ProtoVer, 3, big);
            for (int i = 0; i < chunks.Count; i++) g2.Send(chunks[i]);
            while (h2.TryReceive(out byte[] raw)) gate2.Accept(raw, 0, out AssembledMessage ignored, out string reason);
            CheckEq("chunked BattleEvent counted once", 1, c2.ForbiddenTypeDrops);
            CheckEq("reassembly happened before the allowlist decision", 1, c2.GuestFramesReassembled);
            CheckEq("commandsAccepted still 0", 0, c2.CommandsAccepted);
            h2.Close();
            g2.Close();
        }

        // ------------------------------------------------------------------ I6

        private static void I6TcpSelfTest()
        {
            Section("I6 S2TcpSelfTest end-to-end (real socket, fake host)");

            S2Counters shared = new S2Counters();
            FakeS2Host.Summary summary = null;
            Exception thrown = null;
            Thread thread = new Thread(delegate ()
            {
                try
                {
                    summary = FakeS2Host.Run(0, SecretHex, 15, 30, -1, -1,
                        delegate (FakeS2Host.Summary s) { summary = s; }, shared);
                }
                catch (Exception e) { thrown = e; }
            })
            { IsBackground = true, Name = "FakeS2HostSelfTest" };
            thread.Start();

            long until = Environment.TickCount64 + 3000;
            while ((summary == null || summary.Port == 0) && thrown == null && Environment.TickCount64 < until)
                Thread.Sleep(5);

            Check("fake host started (" + (thrown == null ? "ok" : thrown.GetType().Name) + ")",
                  summary != null && summary.Port > 0);
            if (summary == null || summary.Port == 0) return;

            S2TcpSelfTest.Result result = null;
            S2TcpSelfTest.Run(shared, "127.0.0.1", summary.Port, SecretBytes(), S2Fields.Hash,
                              delegate (S2TcpSelfTest.Result r) { result = r; }, 6000);

            Check("self-test finished", result != null && result.Finished);
            Check("handshake over tcp succeeded", result != null && result.HandshakeOk);
            Check("3 forbidden frames injected", result != null && result.ForbiddenGained == 3);
            CheckEq("host counted exactly 3 forbidden drops", 3, shared.ForbiddenTypeDrops);
            CheckEq("host accepted 0 commands", 0, shared.CommandsAccepted);
            Info("tcp self-test: " + (result == null ? "none" : result.ToText()));

            if (summary != null) summary.StopRequested = true;
            thread.Join(3000);
        }

        // ------------------------------------------------------------------ I7

        private static void I7ResyncOnGap()
        {
            Section("I7 seq gap -> RESYNC -> resync-flagged snapshot closes the window");

            using (HostHandle host = HostHandle.Start(30, 30, skipSeqAt: 20))
            {
                Check("host listened", host.Port > 0);
                if (host.Port == 0) return;

                string error;
                Guest guest = Guest.Connect(host.Port, SecretBytes(), out error);
                Check("guest connected", guest != null);
                if (guest == null) return;

                try
                {
                    Check("guest authenticated", guest.Authenticate(5000, out error));

                    bool gapOpened = false;
                    int received = 0;
                    int divA = 0;
                    long deadline = Environment.TickCount64 + 15000;

                    while (Environment.TickCount64 < deadline)
                    {
                        string snapError;
                        S2Frame frame = guest.NextSnapshot(200, out snapError);
                        if (frame != null)
                        {
                            received++;
                            if (snapError == "divA") divA++;
                            if (guest.Seq.Gaps > 0 && !gapOpened)
                            {
                                gapOpened = true;
                                guest.Resync.OpenGap(Environment.TickCount64, guest.Seq.LastAccepted, "seq-gap");
                                Info("gap detected after " + received + " snapshots (size "
                                     + guest.Seq.LastGapSize + ")");
                            }
                            if (frame.Resync)
                            {
                                guest.Resync.OnSnapshot(true, snapError != "divA", Environment.TickCount64);
                            }
                        }

                        if (gapOpened && guest.Resync.WindowOpen
                            && guest.Resync.ShouldRequest(Environment.TickCount64))
                        {
                            guest.Transport.Send(S2ControlHandler.BuildResync(guest.Resync.LastSeq));
                            guest.Resync.OnRequestSent(Environment.TickCount64);
                            Info("RESYNC sent (attempt " + guest.Resync.Attempts + ")");
                        }

                        if (guest.Resync.WindowsClosed > 0) break;
                    }

                    CheckEq("exactly one gap detected", 1, guest.Seq.Gaps);
                    Check("gap window opened", gapOpened);
                    CheckEq("window closed by the resync snapshot", 1, guest.Resync.WindowsClosed);
                    CheckEq("Div-A stayed 0", 0, divA);
                    Check("host served a RESYNC", host.Summary != null && host.Summary.ResyncServed == 1);
                    Check("gap window duration was measured", guest.Resync.MaxGapOpenMs >= 0);
                    Info("resync: " + guest.Resync.ToText());
                    Info("snapshots received = " + received);
                }
                finally
                {
                    guest.Dispose();
                }
            }
        }

        // ------------------------------------------------------------------ I8

        private static void I8SecondClient()
        {
            Section("I8 second guest is rejected with Deny{busy}");

            using (HostHandle host = HostHandle.Start(20, 30))
            {
                Check("host listened", host.Port > 0);
                if (host.Port == 0) return;

                string error;
                Guest first = Guest.Connect(host.Port, SecretBytes(), out error);
                Check("first guest connected", first != null);
                if (first == null) return;

                try
                {
                    string authError;
                    Check("first guest authenticated", first.Authenticate(5000, out authError));

                    TcpTransport second = TcpTransport.Connect("127.0.0.1", host.Port, out error, 2000);
                    Check("second guest connected at TCP level", second != null);
                    if (second != null)
                    {
                        byte[] raw = null;
                        long until = Environment.TickCount64 + 3000;
                        while (raw == null && Environment.TickCount64 < until) WaitFrame(second, 50, out raw);

                        FrameHeader header = default(FrameHeader);
                        byte[] payload = null;
                        bool isDeny = raw != null && FrameCodec.TryDecode(raw, out header, out payload)
                                      && header.Type == MessageType.Deny;
                        Check("second guest receives Deny", isDeny);
                        if (isDeny)
                        {
                            DenyCode code;
                            string reason;
                            Check("Deny code is Busy",
                                  HandshakeCodec.TryDecodeDeny(payload, out code, out reason, out error)
                                  && code == DenyCode.Busy);
                        }
                        second.Close();
                    }

                    Thread.Sleep(150);
                    string snapError;
                    Check("first guest still receives snapshots",
                          first.NextSnapshot(3000, out snapError) != null);
                }
                finally
                {
                    first.Dispose();
                }
            }
        }
    }
}
