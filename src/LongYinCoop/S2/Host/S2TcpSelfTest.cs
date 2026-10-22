using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using LongYinCoop.S2.Protocol;
using LongYinCoop.S2.Transport;

namespace LongYinCoop.S2.Host
{
    /// <summary>
    /// self-test ของ exit 6/X8 บน **TCP จริง** (แผน S2a-3 §5 I5): ต่อเป็น guest ตัวหนึ่ง,
    /// ทำ handshake จริง, แล้วยิง frame ที่ต้องห้าม 3 ใบ (Hello + Snapshot + chunked BattleEvent)
    ///
    /// ทำอะไรไม่ได้เลยนอกจาก "ส่ง byte": ไม่มี write path ไปยังเกม · ตัว host นับ
    /// `ForbiddenTypeDrops` บน main thread (ผ่าน `S2GuestGate` เดิม) — คลาสนี้แค่อ่านตัวนับนั้น
    ///
    /// รันบนเธรดของตัวเอง **ไม่แตะ main thread** · `S2HostService` เป็นผู้ตัดสินก่อน/หลัง (audit tuple)
    /// </summary>
    public static class S2TcpSelfTest
    {
        public sealed class Result
        {
            public bool Finished;
            public bool HandshakeOk;
            public bool SentForbiddenFrames;
            public long ForbiddenGained;
            public string Error;
            public long ElapsedMs;

            public string ToText()
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "handshakeOk={0} sentForbidden={1} forbiddenGained={2} elapsedMs={3} error={4}",
                    HandshakeOk ? "true" : "false", SentForbiddenFrames ? "true" : "false",
                    ForbiddenGained, ElapsedMs, Error ?? "-");
            }
        }

        public static void StartInBackground(S2Counters counters, string ip, int port, byte[] secret,
                                             string fieldHashHex, Action<Result> onDone,
                                             int totalTimeoutMs = 8000)
        {
            Thread thread = new Thread(delegate () { Run(counters, ip, port, secret, fieldHashHex, onDone, totalTimeoutMs); })
            {
                IsBackground = true,
                Name = "S2TcpSelfTest",
            };
            thread.Start();
        }

        public static Result Run(S2Counters counters, string ip, int port, byte[] secret,
                                 string fieldHashHex, Action<Result> onDone, int totalTimeoutMs = 8000)
        {
            Result result = new Result();
            long startMs = Environment.TickCount64;
            long deadline = startMs + (totalTimeoutMs < 500 ? 500 : totalTimeoutMs);
            TcpTransport client = null;
            try
            {
                string connectError;
                client = TcpTransport.Connect(ip, port, out connectError, 3000);
                if (client == null)
                {
                    result.Error = connectError;
                    return Finish(result, onDone, startMs);
                }

                // 1) Hello
                byte[] hello = ReceiveWithin(client, 3000, deadline);
                if (hello == null)
                {
                    result.Error = "no hello";
                    return Finish(result, onDone, startMs);
                }

                AssembledMessage message;
                string error;
                if (!TryAssemble(hello, out message, out error))
                {
                    result.Error = "hello: " + error;
                    return Finish(result, onDone, startMs);
                }

                S2HandshakeClient handshake = new S2HandshakeClient(secret, fieldHashHex);
                byte[] auth;
                if (!handshake.OnHello(message, out auth, out error))
                {
                    result.Error = "hello: " + error;
                    return Finish(result, onDone, startMs);
                }
                client.Send(auth);

                // 2) AuthResult
                bool accepted = false;
                long authDeadline = Math.Min(deadline, Environment.TickCount64 + 3000);
                while (Environment.TickCount64 < authDeadline)
                {
                    byte[] raw = ReceiveWithin(client, 200, authDeadline);
                    if (raw == null) continue;
                    if (!TryAssemble(raw, out message, out error)) continue;
                    if (message.Type != MessageType.AuthResult && message.Type != MessageType.Deny) continue;
                    accepted = handshake.OnReply(message, out error);
                    if (!accepted) result.Error = error;
                    break;
                }
                result.HandshakeOk = accepted;
                if (!accepted) return Finish(result, onDone, startMs);

                // 3) ยิง 3 frame ที่ต้องห้าม (Hello + Snapshot + chunked BattleEvent)
                long before = counters.ForbiddenTypeDrops;
                List<byte[]> frames = S2SelfTest.BuildFrames();
                for (int i = 0; i < frames.Count; i++) client.Send(frames[i]);
                result.SentForbiddenFrames = true;

                long waitUntil = Math.Min(deadline, Environment.TickCount64 + 3000);
                while (Environment.TickCount64 < waitUntil)
                {
                    if (counters.ForbiddenTypeDrops - before >= S2SelfTest.ExpectedForbiddenDrops) break;
                    Thread.Sleep(5);
                }
                result.ForbiddenGained = counters.ForbiddenTypeDrops - before;

                client.Send(S2ControlHandler.BuildBye());
                Thread.Sleep(30);
                return Finish(result, onDone, startMs);
            }
            catch (Exception e)
            {
                result.Error = e.GetType().Name + ": " + e.Message;
                return Finish(result, onDone, startMs);
            }
            finally
            {
                try { client?.Close(); } catch { }
            }
        }

        private static Result Finish(Result result, Action<Result> onDone, long startMs)
        {
            result.Finished = true;
            result.ElapsedMs = Environment.TickCount64 - startMs;
            try { onDone?.Invoke(result); } catch { }
            return result;
        }

        private static byte[] ReceiveWithin(TcpTransport transport, int waitMs, long deadline)
        {
            long until = Math.Min(deadline, Environment.TickCount64 + waitMs);
            while (Environment.TickCount64 < until)
            {
                byte[] frame;
                if (transport.TryReceive(out frame)) return frame;
                Thread.Sleep(2);
            }
            return null;
        }

        private static bool TryAssemble(byte[] raw, out AssembledMessage message, out string error)
        {
            message = default(AssembledMessage);
            error = null;
            FrameHeader header;
            if (!FrameCodec.TryReadHeader(raw, out header))
            {
                error = "bad header";
                return false;
            }
            if (header.Type != MessageType.Chunk)
            {
                message = new AssembledMessage(header.Type, header.Ver, header.Seq,
                                               Slice(raw, header.PayloadOffset, header.PayloadLength));
                return true;
            }
            // self-test ใช้เฉพาะ frame เดี่ยว (snapshot ที่ host ส่งมาอาจเป็น chunk — ข้ามได้)
            error = "chunk";
            return false;
        }

        private static byte[] Slice(byte[] raw, int offset, int length)
        {
            if (length <= 0) return Array.Empty<byte>();
            byte[] copy = new byte[length];
            Buffer.BlockCopy(raw, offset, copy, 0, length);
            return copy;
        }
    }
}
