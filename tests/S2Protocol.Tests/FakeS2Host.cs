using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using LongYinCoop.S2.Host;
using LongYinCoop.S2.Protocol;
using LongYinCoop.S2.Transport;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// host จำลอง (ไม่ใช้เกม/ไม่ใช้ Unity) สำหรับ T1: ทดสอบ `tools/viewer-client.py` (I9)
    /// และใช้เป็นคู่สนทนาในเทสต์ TCP
    ///
    /// ทำตัวเหมือน `S2HostService` เท่าที่จำเป็น: listen → Hello → Auth → Snapshot 30 Hz
    /// + ตอบ Ping/Resync + นับ forbidden drops · **ไม่มี write path ใด ๆ** (ไม่มีเกมให้เขียน)
    /// </summary>
    internal static class FakeS2Host
    {
        public sealed class Summary
        {
            public int Port;
            public long AuthAccepted;
            public long AuthDenied;
            public long Snapshots;
            public long Forbidden;
            public long CommandsAccepted;
            public long Rejected;
            public long ResyncServed;
            public long SkippedSeq;
            public long DuplicateSeqInjections;
            public long Malformed;
            public string LastError;
            public string LastDisconnect;
            /// <summary>ตั้งจากเทสต์เพื่อให้ loop จบก่อนครบเวลา (Dispose ของ HostHandle)</summary>
            public volatile bool StopRequested;
        }

        /// <summary>รัน host จำลองจนครบเวลา; คืน summary (ใช้ในเทสต์ in-proc)</summary>
        public static Summary Run(int port, string secretHex, int seconds, int sendHz = 30,
                                  int skipSeqAt = -1, int dupSeqAt = -1,
                                  Action<Summary> onStarted = null,
                                  S2Counters sharedCounters = null)
        {
            Summary summary = new Summary();
            byte[] secret = S2Auth.BytesFromHex(secretHex);
            if (secret == null || secret.Length < 16)
            {
                summary.LastError = "secret must be >= 32 hex chars";
                return summary;
            }

            string listenError;
            TcpTransport host = TcpTransport.Listen("127.0.0.1", port, out listenError);
            if (host == null)
            {
                summary.LastError = listenError;
                return summary;
            }

            summary.Port = host.Port;
            try
            {
                host.BusyFrame = FrameCodec.Encode(MessageType.Deny, S2Protocol.ProtoVer, 0,
                                                   HandshakeCodec.EncodeDeny(DenyCode.Busy, "busy"));

                S2Counters counters = sharedCounters ?? new S2Counters();
                S2GuestGate gate = new S2GuestGate(counters);
                S2HandshakeServer handshake = new S2HandshakeServer(secret, S2Fields.Hash);
                S2ControlHandler control = new S2ControlHandler(counters);

                bool authenticated = false;
                uint sessionId = 0;
                uint seq = 0;
                ushort msgId = 0;
                long snapshots = 0;
                long frameNo = 0;
                int intervalMs = 1000 / Math.Max(1, sendHz);
                long start = Environment.TickCount64;
                long end = start + Math.Max(1, seconds) * 1000L;
                long nextSend = start;
                bool pendingResync = false;
                uint pendingResyncFrom = 0;

                if (onStarted != null) onStarted(summary);

                while (Environment.TickCount64 < end && !summary.StopRequested)
                {
                    long now = Environment.TickCount64;

                    if (host.TakeNewConnection())
                    {
                        handshake.NewConnection();
                        control.Reset();
                        sessionId = unchecked(sessionId + 1);
                        authenticated = false;
                        seq = 0;
                        byte[] hello = handshake.BuildHello(sessionId, now);
                        host.Send(hello);
                    }

                    int drain = 0;
                    while (drain++ < 256 && host.TryReceive(out byte[] raw))
                    {
                        S2GateOutcome outcome = gate.Accept(raw, now, out AssembledMessage message, out string reason);
                        if (outcome == S2GateOutcome.Await) continue;
                        if (outcome == S2GateOutcome.Forbidden)
                        {
                            summary.Forbidden++;
                            continue;
                        }
                        if (outcome == S2GateOutcome.Malformed)
                        {
                            summary.Malformed++;
                            continue;
                        }
                        if (outcome != S2GateOutcome.Control) continue;

                        if (message.Type == MessageType.Auth)
                        {
                            byte[] reply;
                            DenyCode code;
                            string denyReason;
                            HandshakeOutcome hs = handshake.AcceptAuth(message, now, out reply, out code, out denyReason);
                            if (reply != null) host.Send(reply);
                            if (hs == HandshakeOutcome.Accepted)
                            {
                                authenticated = true;
                                summary.AuthAccepted++;
                                // seqAtSend: snapshot แรกของ session = seq 1 (guest คาด 1 เสมอ)
                                seq = 0;
                                nextSend = Environment.TickCount64;
                            }
                            else
                            {
                                summary.AuthDenied++;
                            }
                            continue;
                        }

                        if (!authenticated)
                        {
                            summary.Rejected++;
                            host.RequestClose();
                            continue;
                        }

                        byte[] controlReply;
                        string controlReason;
                        S2ControlAction action = control.Handle(message, now, inBattle: true,
                                                                nextSeq: unchecked(seq + 1),
                                                                out controlReply, out controlReason);
                        if (controlReply != null) host.Send(controlReply);
                        if (action == S2ControlAction.ResyncAccepted)
                        {
                            summary.ResyncServed++;
                            uint fromSeq;
                            if (control.ConsumeResyncFlag(out fromSeq)) pendingResyncFrom = fromSeq;
                            pendingResync = true;
                        }
                        else if (action == S2ControlAction.Closing)
                        {
                            host.RequestClose();
                            authenticated = false;
                        }
                    }

                    if (authenticated && now >= nextSend)
                    {
                        nextSend = now + intervalMs;
                        frameNo++;

                        // --make a gap: assign the seq but do NOT put the message on the wire
                        if (skipSeqAt > 0 && frameNo == skipSeqAt)
                        {
                            seq = unchecked(seq + 1);
                            summary.SkippedSeq++;
                        }
                        else if (dupSeqAt > 0 && frameNo == dupSeqAt)
                        {
                            // send the same message twice (guest must count a duplicate, not a gap)
                            summary.DuplicateSeqInjections++;
                        }

                        S2Frame frame = SyntheticFrame(frameNo, seq);
                        bool resync = pendingResync;
                        uint resyncFrom = pendingResyncFrom;
                        pendingResync = false;

                        S2Frame outgoing = frame;
                        outgoing.Seq = unchecked(seq + 1);
                        outgoing.SessionId = sessionId;
                        seq = outgoing.Seq;
                        outgoing.HostApplyUs = NowUs();   // guest คำนวณ latency = tRecv - hostApplyUs - offset
                        outgoing.Resync = resync;
                        outgoing.ResyncFromSeq = resync ? resyncFrom : 0;
                        outgoing.Heartbeat = false;
                        outgoing.ReplicaHash = S2FrameCodec.ComputeReplicaHash(outgoing);

                        byte[] payload = S2FrameCodec.Encode(outgoing, outgoing.ReplicaHash);
                        List<byte[]> frames = ChunkCodec.Encode(unchecked(msgId++), MessageType.Snapshot,
                                                                S2Protocol.ProtoVer, outgoing.Seq, payload);
                        int wire = 0;
                        for (int i = 0; i < frames.Count; i++)
                        {
                            wire += frames[i].Length;
                            host.Send(frames[i]);
                        }
                        outgoing.WireBytes = wire;
                        snapshots++;
                        summary.Snapshots = snapshots;

                        if (dupSeqAt > 0 && frameNo == dupSeqAt)
                        {
                            List<byte[]> again = ChunkCodec.Encode(unchecked(msgId++), MessageType.Snapshot,
                                                                  S2Protocol.ProtoVer, outgoing.Seq, payload);
                            for (int i = 0; i < again.Count; i++) host.Send(again[i]);
                        }

                        if (control.CloseRequested)
                        {
                            host.RequestClose();
                            authenticated = false;
                        }
                    }

                    if (host.Stats.RejectedConnections > 0 && summary.Rejected == 0)
                        summary.Rejected = host.Stats.RejectedConnections;

                    Thread.Sleep(1);
                }

                summary.Forbidden = counters.ForbiddenTypeDrops;
                summary.CommandsAccepted = counters.CommandsAccepted;
                summary.Malformed = counters.MalformedDrops;
                summary.ResyncServed = counters.ResyncServed;
                summary.Rejected = host.Stats.RejectedConnections;
                summary.LastError = host.LastError;
                summary.LastDisconnect = host.LastDisconnect;
                return summary;
            }
            finally
            {
                host.Close();
            }
        }

        /// <summary>monotonic µs แบบเดียวกับ S2ControlHandler (Stopwatch/QPC) — ใช้เป็น hostApplyUs</summary>
        private static ulong NowUs()
        {
            return (ulong)(System.Diagnostics.Stopwatch.GetTimestamp() * 1000000L
                           / System.Diagnostics.Stopwatch.Frequency);
        }

        /// <summary>frame สังเคราะห์ 9 ยูนิต — ค่าเปลี่ยนตาม n เพื่อให้ hash เปลี่ยนจริง</summary>
        public static S2Frame SyntheticFrame(long n, uint seq)
        {
            S2Frame frame = new S2Frame();
            frame.FrameId = n;
            frame.Seq = seq;
            frame.Header.BattleKey = "fake|battle|1";
            frame.Header.TurnIndex = (int)(n / 30);
            frame.Header.ActiveUnitId = "0:1001";
            frame.Header.Paused = false;
            frame.Header.NowActiveState = 29;
            frame.Header.BattleTime = 10.0f + n * 0.0333f;
            frame.Header.UnitCount = 9;

            S2Unit[] units = new S2Unit[9];
            for (int i = 0; i < 9; i++)
            {
                float hp = 40.0f + i - (n % 3) * 0.5f;
                units[i] = new S2Unit
                {
                    StableId = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", i < 5 ? 0 : 1, 1001 + i),
                    Team = i < 5 ? 0 : 1,
                    HeroId = 1001 + i,
                    Row = i % 3,
                    Column = i / 3,
                    Hp = hp,
                    MaxHp = 50f,
                    IsAlive = hp > 0f,
                    UnitAlive = hp > 0f,
                    Moved = false,
                    Attacked = false,
                    PlayerControl = i == 0,
                };
            }
            frame.Units = units;
            frame.Audit = S2AuditUtil.Compute(frame.Header, units);
            return frame;
        }

        /// <summary>`--fake-host` CLI: พิมพ์ summary JSON ออก stdout ให้สคริปต์อ่าน</summary>
        public static int RunCli(string[] args)
        {
            int port = args.Length > 0 ? int.Parse(args[0], CultureInfo.InvariantCulture) : 47800;
            int seconds = args.Length > 1 ? int.Parse(args[1], CultureInfo.InvariantCulture) : 30;
            string secret = args.Length > 2 ? args[2] : new string('a', 64);
            int sendHz = args.Length > 3 ? int.Parse(args[3], CultureInfo.InvariantCulture) : 30;
            int skipSeqAt = args.Length > 4 ? int.Parse(args[4], CultureInfo.InvariantCulture) : -1;

            Console.Out.Flush();
            Summary summary = Run(port, secret, seconds, sendHz, skipSeqAt,
                                  -1,
                                  delegate (Summary s)
                                  {
                                      Console.WriteLine("FAKEHOST listening port=" + s.Port
                                                        + " fieldHashPrefix=" + S2Fields.Prefix);
                                      Console.Out.Flush();
                                  });

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "FAKEHOST summary port={0} authAccepted={1} authDenied={2} snapshots={3} forbidden={4} " +
                "commandsAccepted={5} rejected={6} resyncServed={7} skippedSeq={8} malformed={9} lastError={10} " +
                "lastDisconnect={11}",
                summary.Port, summary.AuthAccepted, summary.AuthDenied, summary.Snapshots,
                summary.Forbidden, summary.CommandsAccepted, summary.Rejected, summary.ResyncServed,
                summary.SkippedSeq, summary.Malformed, summary.LastError ?? "-",
                summary.LastDisconnect ?? "-"));
            return summary.AuthAccepted > 0 ? 0 : 3;
        }
    }
}
