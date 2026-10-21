using System;
using System.Collections.Generic;
using LongYinCoop.S2.Protocol;

namespace LongYinCoop.S2.Host
{
    /// <summary>
    /// self-test ของ exit 6 (rev3) — ยิง frame ที่ **ต้องห้าม** จากฝั่ง guest เข้า host แล้ววัด
    ///   `forbiddenTypeDrops == 3` พอดี (Hello + Snapshot + BattleEvent ที่ถูก chunk)
    ///   `commandsAccepted == 0`
    ///   audit tuple ก่อน/หลัง self-test **เท่ากัน**
    /// ตัว frame builder อยู่ในไฟล์ pure นี้ เพื่อให้เทสต์ in-proc ใช้ตัวเดียวกับรันจริงได้
    /// </summary>
    public static class S2SelfTest
    {
        public const int ExpectedForbiddenDrops = 3;
        public const int ChunkMsgId = 7;

        /// <summary>3 frame ต้องห้าม: Hello, Snapshot, และ BattleEvent ที่ใหญ่จนถูก chunk</summary>
        public static List<byte[]> BuildFrames()
        {
            List<byte[]> frames = new List<byte[]>();
            frames.Add(FrameCodec.Encode(MessageType.Hello, S2Protocol.ProtoVer, 0, Array.Empty<byte>()));
            frames.Add(FrameCodec.Encode(MessageType.Snapshot, S2Protocol.ProtoVer, 1, new byte[8]));

            byte[] oversized = new byte[S2Protocol.MaxPayloadBytes + 64];
            frames.AddRange(ChunkCodec.Encode(ChunkMsgId, MessageType.BattleEvent, S2Protocol.ProtoVer, 2,
                                             oversized));
            return frames;
        }

        public static bool MeetsExit6(long forbiddenGained, long commandsAccepted, bool auditEqual)
        {
            return forbiddenGained == ExpectedForbiddenDrops
                   && commandsAccepted == 0
                   && auditEqual;
        }

        public static string Describe(long forbiddenGained, long commandsAccepted, bool auditEqual,
                                     int injectedFrames, int chunks)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "injectedFrames={0} chunks={1} expectedForbiddenDrops={2} gained={3} commandsAccepted={4} " +
                "auditEqual={5} exit6={6}",
                injectedFrames, chunks, ExpectedForbiddenDrops, forbiddenGained, commandsAccepted,
                auditEqual ? "true" : "false",
                MeetsExit6(forbiddenGained, commandsAccepted, auditEqual) ? "PASS" : "FAIL");
        }
    }
}
