using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using LongYinCoop.S2;
using LongYinCoop.S2.Guest;
using LongYinCoop.S2.Host;
using LongYinCoop.S2.Protocol;
using LongYinCoop.S2.Transport;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// S2a-4 tests (แผน rev4 §10) — ไม่ใช้เกม ไม่มี mod runtime
    ///
    /// T0  decoder vectors (W1)   T1  client↔fake-host (W2)   T2  WriteGuard ก่อน hook + static check (W4(b)(c))
    /// T3  render model (W3)      T4  set_pref gate + net.cfg overlay (W5)
    ///
    /// W0 (ขั้นนี้) = โครง suite เท่านั้น: ตรวจ infrastructure ที่ทุก T ใช้ (temp dir, ProtoVer baseline)
    /// </summary>
    internal static class S2a4Tests
    {
        private static int _pass;
        private static int _fail;
        private static string _tmp;

        public static int Run()
        {
            _pass = 0;
            _fail = 0;
            _tmp = Path.Combine(Path.GetTempPath(), "s2a4-tests-" + Environment.ProcessId);
            Directory.CreateDirectory(_tmp);

            Console.WriteLine();
            Console.WriteLine("== S2a-4 tests ==");
            try
            {
                Scaffold();
                T0DecoderVectors();
                T1OverlayClient();
                T3RenderModel();
            }
            finally
            {
                try { Directory.Delete(_tmp, true); } catch { }
            }

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "S2a-4 RESULT {0} (pass {1} fail {2})", _fail == 0 ? "PASS" : "FAIL", _pass, _fail));
            return _fail;
        }

        private static void Check(string name, bool ok)
        {
            if (ok) _pass++;
            else _fail++;
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "[{0}] s2a4: {1}", ok ? "PASS" : "FAIL", name));
        }

        /// <summary>W0: infrastructure ที่ T0–T4 ใช้ — temp dir อ่าน/เขียน/ลบได้ และ baseline ProtoVer ชัด</summary>
        private static void Scaffold()
        {
            string path = Path.Combine(_tmp, "scaffold.txt");
            try
            {
                File.WriteAllText(path, "ok");
                Check("scaffold temp dir round-trip", File.ReadAllText(path) == "ok");
            }
            catch (Exception e)
            {
                Check("scaffold temp dir round-trip (" + e.GetType().Name + ")", false);
            }

            // T0/T1 ทั้งชุดผูกกับรูป envelope ของ ProtoVer นี้ — เปลี่ยนค่าต้องเป็นการตั้งใจ ไม่ใช่ drift
            Check("protoVer baseline is 2 (S2a-3 wire v2)", S2Protocol.ProtoVer == 2);
        }

        // ------------------------------------------------------------------ T0 (W1) decoder vectors

        private static void T0DecoderVectors()
        {
            // 1) snapshot ปกติ: encode→decode ครบทุก field + Div-A ผ่าน
            S2Frame frame = FakeS2Host.SyntheticFrame(5, 7);
            frame.SessionId = 42;
            frame.HostApplyUs = 123456789UL;
            byte[] payload = S2FrameCodec.Encode(frame);
            GuestSnapshotDisplay d;
            string error;
            Check("T0 decode normal ok", S2GuestDecoder.TryDecode(payload, 7, out d, out error));
            Check("T0 seq comes from envelope", d != null && d.Seq == 7);
            Check("T0 header fields survive", d != null
                && d.BattleKey == frame.Header.BattleKey
                && d.TurnIndex == frame.Header.TurnIndex
                && d.ActiveUnitId == frame.Header.ActiveUnitId
                && d.Paused == frame.Header.Paused
                && d.NowActiveState == frame.Header.NowActiveState
                && Math.Abs(d.BattleTime - frame.Header.BattleTime) < 1e-4f);
            Check("T0 session/hostApply survive", d != null && d.SessionId == 42 && d.HostApplyUs == 123456789UL);
            Check("T0 unit count", d != null && d.Units.Length == 9);
            Check("T0 Div-A hash match", d != null && d.ReplicaHashMatch);
            Check("T0 hp text truncates decimals", d != null && d.Units[0].HpText == "39/50");
            Check("T0 display order team0 first", d != null && d.Units[0].Team == 0
                && d.Units[d.Units.Length - 1].Team == 1);

            // 2) ศพ: hp=0 + isAlive=false (unitAlive ยัง true 1 เฟรม = RUNTIME fact)
            S2Frame corpse = FakeS2Host.SyntheticFrame(1, 1);
            S2Unit dead = corpse.Units[2];
            dead.Hp = 0f;
            dead.IsAlive = false;
            dead.UnitAlive = true;
            corpse.Units[2] = dead;
            byte[] corpsePayload = S2FrameCodec.Encode(corpse);
            Check("T0 corpse decodes", S2GuestDecoder.TryDecode(corpsePayload, 2, out d, out error));
            GuestUnitDisplay corpseRow = FindUnit(d, corpse.Units[2].StableId);
            Check("T0 corpse Dead=true + HpText 0", corpseRow.Dead && corpseRow.HpText == "0/50"
                && corpseRow.HpFrac == 0f && corpseRow.UnitAlive);

            // 3) 60 ยูนิต (มติ Q4 เป้า scale): payload ใหญ่กว่าเพดาน 1 frame → ผ่าน chunk/reassembly แล้ว decode ได้
            S2Frame sixty = MakeFrame(60, 9);
            byte[] bigPayload = S2FrameCodec.Encode(sixty);
            Check("T0 60-unit payload exceeds one frame", bigPayload.Length > S2Protocol.MaxPayloadBytes - 8);
            List<byte[]> chunks = ChunkCodec.Encode(1, MessageType.Snapshot, S2Protocol.ProtoVer, 3, bigPayload);
            Check("T0 60-unit is chunked", chunks.Count > 1);
            ChunkReassembler re = new ChunkReassembler();
            AssembledMessage assembled = default(AssembledMessage);
            bool got = false;
            foreach (byte[] chunk in chunks)
            {
                FrameCodec.TryDecode(chunk, out FrameHeader h, out byte[] body);
                got = re.Accept(h, body, 0, out assembled);
            }
            Check("T0 60-unit reassembles", got && assembled.Type == MessageType.Snapshot);
            Check("T0 60-unit decodes + Div-A", S2GuestDecoder.TryDecode(assembled.Payload, 3, out d, out error)
                && d.Units.Length == 60 && d.ReplicaHashMatch);

            // 4) เกิน cap 64 ยูนิต → fail-closed
            S2Frame over = MakeFrame(65, 1);
            Check("T0 65 units rejected", !S2GuestDecoder.TryDecode(S2FrameCodec.Encode(over), 1, out d, out error)
                && error != null && error.Contains("unit count"));

            // 5) edge ขอบช่วง varint: 0 / ค่าใหญ่ ของ team/heroId/row/col
            S2Frame edge = MakeFrame(1, 1);
            S2Unit u0 = edge.Units[0];
            u0.Team = 0;
            u0.HeroId = 2_000_000_000;
            u0.Row = 65535;
            u0.Column = 65535;
            edge.Units[0] = u0;
            Check("T0 varint edges round-trip", S2GuestDecoder.TryDecode(S2FrameCodec.Encode(edge), 4, out d, out error)
                && d.Units[0].HeroId == 2_000_000_000 && d.Units[0].Row == 65535 && d.Units[0].Column == 65535);

            // 6) Div-A จับ hash ไม่ตรง (แนบ hash ของ frame อื่น)
            S2Frame a = FakeS2Host.SyntheticFrame(3, 1);
            S2Frame b = FakeS2Host.SyntheticFrame(4, 2);
            byte[] mismatched = S2FrameCodec.Encode(b, S2FrameCodec.ComputeReplicaHash(a));
            Check("T0 Div-A detects mismatch", S2GuestDecoder.TryDecode(mismatched, 5, out d, out error)
                && !d.ReplicaHashMatch);

            // 7) fail-closed: protoVer ผิด / trailing byte / payload ว่าง
            byte[] badVer = (byte[])S2FrameCodec.Encode(FakeS2Host.SyntheticFrame(1, 1)).Clone();
            badVer[0] = 9;
            Check("T0 wrong protoVer rejected", !S2GuestDecoder.TryDecode(badVer, 1, out d, out error));
            byte[] trailing = S2FrameCodec.Encode(FakeS2Host.SyntheticFrame(1, 1));
            Array.Resize(ref trailing, trailing.Length + 1);
            Check("T0 trailing byte rejected", !S2GuestDecoder.TryDecode(trailing, 1, out d, out error));
            Check("T0 empty payload rejected", !S2GuestDecoder.TryDecode(new byte[0], 1, out d, out error));

            // 8) pure helpers ของ display: NaN/0-maxHp ต้องไม่พัง
            Check("T0 hpFrac NaN/zero-max clamps", S2GuestDecoder.HpFrac(float.NaN, 50f) == 0f
                && S2GuestDecoder.HpFrac(10f, 0f) == 0f
                && S2GuestDecoder.HpFrac(90f, 100f) == 0.9f
                && S2GuestDecoder.HpFrac(200f, 100f) == 1f);
            Check("T0 hpText NaN prints 0", S2GuestDecoder.HpText(float.NaN, 50f) == "0/50");
        }

        private static GuestUnitDisplay FindUnit(GuestSnapshotDisplay d, string stableId)
        {
            for (int i = 0; i < d.Units.Length; i++)
            {
                if (d.Units[i].StableId == stableId) return d.Units[i];
            }
            return default(GuestUnitDisplay);
        }

        // ------------------------------------------------------------------ T1 (W2) client <-> fake-host

        private const string T1Secret = "0123456789abcdef0123456789abcdef";
        private const string T1WrongSecret = "ffffffffffffffffffffffffffffffff";

        /// <summary>เริ่ม FakeS2Host บนเธรดของตัวเอง (port 0 = OS เลือก) คืน handle รอ/หยุด</summary>
        private sealed class T1Host : IDisposable
        {
            public int Port;
            public FakeS2Host.Summary Summary;
            private readonly Thread _thread;
            private readonly ManualResetEvent _started = new ManualResetEvent(false);

            public T1Host(int seconds, int skipSeqAt = -1, int dupSeqAt = -1)
            {
                _thread = new Thread(delegate ()
                {
                    Summary = FakeS2Host.Run(0, T1Secret, seconds, 30, skipSeqAt, dupSeqAt,
                        delegate (FakeS2Host.Summary s)
                        {
                            Port = s.Port;
                            _started.Set();
                        });
                })
                { IsBackground = true, Name = "s2a4-t1-host" };
                _thread.Start();
                _started.WaitOne(5000);
            }

            public void Dispose()
            {
                if (Summary != null) Summary.StopRequested = true;
                _thread.Join(2000);
            }
        }

        private static byte[] SecretBytes(string hex)
        {
            return LongYinCoop.S2.Protocol.S2Auth.BytesFromHex(hex);
        }

        private static S2OverlayClientStats WaitUntil(S2OverlayClient client, Func<S2OverlayClientStats, bool> predicate,
                                                      int timeoutMs)
        {
            long end = Environment.TickCount64 + timeoutMs;
            S2OverlayClientStats stats = client.SnapshotStats();
            while (Environment.TickCount64 < end)
            {
                if (predicate(stats)) return stats;
                Thread.Sleep(50);
                stats = client.SnapshotStats();
            }
            return stats;
        }

        private static void T1OverlayClient()
        {
            string fieldHash = LongYinCoop.S2.Protocol.S2Fields.Hash;

            // 1) handshake ผ่าน + snapshot ไหล + Div-A สะอาด + PING offset (loopback)
            using (T1Host host = new T1Host(10))
            {
                Check("T1 fake host started", host.Port > 0);
                using (S2OverlayClient client = new S2OverlayClient("127.0.0.1", host.Port,
                        SecretBytes(T1Secret), fieldHash))
                {
                    client.Start();
                    S2OverlayClientStats s = WaitUntil(client,
                        delegate (S2OverlayClientStats x) { return x.Snapshots >= 20 && x.Pongs >= 1; }, 8000);
                    Check("T1 handshake authenticates", s.Authenticated && s.Sessions == 1);
                    Check("T1 snapshots flow (>=20)", s.Snapshots >= 20);
                    Check("T1 seq tracks without gaps", s.Gaps == 0 && s.LastSeq >= 20);
                    Check("T1 Div-A clean", s.DivAChecked >= 20 && s.DivAMismatch == 0);
                    Check("T1 PING/PONG offset on loopback", s.HasOffset && s.Pongs >= 1
                        && Math.Abs(s.OffsetUsAtMinRtt) < 200_000);
                    Check("T1 ring + latest populated", client.Latest() != null && client.CopyRing().Length > 0
                        && client.Latest().Units.Length == 9);
                }
            }

            // 2) secret ผิด → ไม่ผ่าน handshake + ได้ Deny (BadAuth)
            using (T1Host host = new T1Host(8))
            using (S2OverlayClient client = new S2OverlayClient("127.0.0.1", host.Port,
                    SecretBytes(T1WrongSecret), fieldHash))
            {
                client.Start();
                S2OverlayClientStats s = WaitUntil(client,
                    delegate (S2OverlayClientStats x) { return x.Denies >= 1; }, 8000);
                Check("T1 wrong secret never authenticates", !s.Authenticated && s.Sessions == 0);
                Check("T1 wrong secret gets denied", s.Denies >= 1
                    && (s.LastDeny == DenyCode.BadAuth || s.LastDeny == DenyCode.Timeout));
            }

            // 3) DENY busy: guest ที่สอง (slot เดียว — M4)
            using (T1Host host = new T1Host(10))
            using (S2OverlayClient first = new S2OverlayClient("127.0.0.1", host.Port,
                    SecretBytes(T1Secret), fieldHash))
            {
                first.Start();
                WaitUntil(first, delegate (S2OverlayClientStats x) { return x.Snapshots >= 5; }, 8000);
                using (S2OverlayClient second = new S2OverlayClient("127.0.0.1", host.Port,
                        SecretBytes(T1Secret), fieldHash))
                {
                    second.Start();
                    S2OverlayClientStats s = WaitUntil(second,
                        delegate (S2OverlayClientStats x) { return x.Denies >= 1; }, 8000);
                    Check("T1 second guest denied busy", s.Denies >= 1 && s.LastDeny == DenyCode.Busy
                        && !s.Authenticated);
                }
                Check("T1 first guest unaffected", first.SnapshotStats().Authenticated);
            }

            // 4) resync on gap: host ข้าม seq ครั้งเดียว → client เห็น gap ส่ง RESYNC และปิดหน้าต่างได้
            using (T1Host host = new T1Host(12, skipSeqAt: 5))
            using (S2OverlayClient client = new S2OverlayClient("127.0.0.1", host.Port,
                    SecretBytes(T1Secret), fieldHash))
            {
                client.Start();
                S2OverlayClientStats s = WaitUntil(client,
                    delegate (S2OverlayClientStats x) { return x.Gaps >= 1 && x.GapClosed >= 1 && x.Snapshots >= 30; }, 12000);
                Check("T1 gap detected exactly once", s.Gaps == 1);
                Check("T1 resync sent + acked", s.ResyncsSent >= 1 && s.ResyncAcks >= 1);
                Check("T1 gap window closed by resync snapshot", s.GapClosed >= 1);
                Check("T1 flow continues after gap", s.Snapshots >= 30);
            }

            // 5) duplicate seq ไม่นับเป็น gap
            using (T1Host host = new T1Host(10, dupSeqAt: 4))
            using (S2OverlayClient client = new S2OverlayClient("127.0.0.1", host.Port,
                    SecretBytes(T1Secret), fieldHash))
            {
                client.Start();
                S2OverlayClientStats s = WaitUntil(client,
                    delegate (S2OverlayClientStats x) { return x.Duplicates >= 1 && x.Snapshots >= 20; }, 8000);
                Check("T1 duplicate counted not gap", s.Duplicates >= 1 && s.Gaps == 0
                    && s.ResyncsSent == 0);
            }
        }

        // ------------------------------------------------------------------ T3 (W3) render model + overlay key

        private static GuestTimedSnapshot Snap(uint seq, long receivedAtMs)
        {
            S2Frame frame = FakeS2Host.SyntheticFrame(seq, seq);
            byte[] payload = S2FrameCodec.Encode(frame);
            GuestSnapshotDisplay display;
            S2GuestDecoder.TryDecode(payload, seq, out display, out _);
            return new GuestTimedSnapshot { Seq = seq, ReceivedAtMs = receivedAtMs, Display = display };
        }

        private static S2NetSettings ParseCfg(string text)
        {
            return S2NetConfig.Parse(text, "t", 0);
        }

        private static void T3RenderModel()
        {
            long now = 100_000;

            // 1) ring ว่าง → ไม่มีเฟรม (ไม่เด่า ไม่สร้างของปลอม)
            S2OverlaySelection none = S2OverlayRenderModel.Select(new GuestTimedSnapshot[0], now);
            Check("T3 empty ring has no snapshot", !none.HasSnapshot);

            // 2) มีเฟรมเดียวยังใหม่กว่าหน้าต่าง smoothing → ใช้เฟรมนั้น (ไม่ extrapolate — ไม่มีเฟรมอื่นให้ใช้)
            GuestTimedSnapshot[] single = { Snap(1, now - 30) };
            S2OverlaySelection fresh = S2OverlayRenderModel.Select(single, now);
            Check("T3 single fresh frame uses oldest real frame", fresh.HasSnapshot && fresh.Snapshot.Seq == 1
                && !fresh.Stale && fresh.AgeMs == 30);

            // 3) smoothing 100 ms: เลือกใหม่สุดที่พ้นหน้าต่าง (age 300) ไม่ใช่ใหม่สุด (age 20)
            GuestTimedSnapshot[] ring =
            {
                Snap(1, now - 500),
                Snap(2, now - 400),
                Snap(3, now - 300),
                Snap(4, now - 20),
            };
            S2OverlaySelection selected = S2OverlayRenderModel.Select(ring, now);
            Check("T3 smoothing picks newest frame past window", selected.HasSnapshot
                && selected.Snapshot.Seq == 3 && selected.AgeMs == 300 && !selected.Stale);

            // 4) ทุกเฟรมใหม่กว่าหน้าต่าง → ใช้เก่าสุดที่มีอยู่จริง
            GuestTimedSnapshot[] allFresh =
            {
                Snap(1, now - 50),
                Snap(2, now - 40),
                Snap(3, now - 30),
            };
            S2OverlaySelection oldest = S2OverlayRenderModel.Select(allFresh, now);
            Check("T3 all-fresh falls back to oldest", oldest.HasSnapshot && oldest.Snapshot.Seq == 1);

            // 5) stale เมื่อเก่าเกิน 3000 ms
            GuestTimedSnapshot[] staleRing = { Snap(1, now - 4000), Snap(2, now - 3500) };
            S2OverlaySelection stale = S2OverlayRenderModel.Select(staleRing, now);
            Check("T3 stale flagged past 3000ms", stale.HasSnapshot && stale.Stale
                && stale.Snapshot.Seq == 2);

            // 6) extrapolate=false: ผลลัพธ์ต้องเป็นสมาชิกของ ring เสมอ
            bool member = false;
            for (int i = 0; i < ring.Length; i++)
            {
                if (ring[i].Seq == selected.Snapshot.Seq) { member = true; break; }
            }
            Check("T3 selection is a real ring member", member && selected.Snapshot.Seq != 0);

            // 7) overlay=false ไม่มีผล (ไม่วาด) — gate แบบ pure
            Check("T3 ShouldRender gate", !S2OverlayRenderModel.ShouldRender(false, ring.Length)
                && S2OverlayRenderModel.ShouldRender(true, ring.Length)
                && !S2OverlayRenderModel.ShouldRender(true, 0));

            // 8) net.cfg key `overlay` (M3): default false · รับ true/1/yes/on · ค่าอื่น = false
            Check("T3 overlay default false", !ParseCfg("").Overlay);
            Check("T3 overlay true", ParseCfg("overlay = true").Overlay);
            Check("T3 overlay 1/yes/on", ParseCfg("overlay = 1").Overlay
                && ParseCfg("overlay = yes").Overlay
                && ParseCfg("overlay = on").Overlay);
            Check("T3 overlay garbage/false = false", !ParseCfg("overlay = maybe").Overlay
                && !ParseCfg("overlay = false").Overlay);
            S2NetSettings withOverlay = ParseCfg("overlay = true\nrole = host");
            Check("T3 overlay survives alongside other keys", withOverlay.Overlay
                && withOverlay.Role == "host" && withOverlay.FormatOk);
            Check("T3 overlay appears in Describe", ParseCfg("overlay = true")
                .Describe().Contains("overlay=True"));

            // 9) text builder: ใช้ template ของ UIStrings และใส่ค่าครบ
            GuestSnapshotDisplay d = Snap(7, now).Display;
            string header = S2OverlayText.SnapshotLine(d);
            string unit = S2OverlayText.UnitLine(d.Units[0]);
            Check("T3 header text has battle/turn/units", header.Contains("turn") && header.Contains("units 9")
                && header.Contains("active"));
            Check("T3 unit text has id/hp/grid", unit.Contains(d.Units[0].StableId)
                && unit.Contains(d.Units[0].HpText) && unit.Contains("(0,0)"));
        }

        /// <summary>frame สังเคราะห์ n ยูนิต (ใช้กับ 60/65 ยูนิต — เกินขอบเดิมของ SyntheticFrame)</summary>
        private static S2Frame MakeFrame(int unitCount, int turn)
        {
            S2Frame frame = new S2Frame();
            frame.FrameId = turn;
            frame.Seq = 1;
            frame.Header.BattleKey = "s2a4|synthetic|" + unitCount;
            frame.Header.TurnIndex = turn;
            frame.Header.ActiveUnitId = "0:1";
            frame.Header.Paused = false;
            frame.Header.NowActiveState = 29;
            frame.Header.BattleTime = 1.5f;
            frame.Header.UnitCount = unitCount;
            S2Unit[] units = new S2Unit[unitCount];
            for (int i = 0; i < unitCount; i++)
            {
                units[i] = new S2Unit
                {
                    StableId = (i % 2) + ":" + i,
                    Team = i % 2,
                    HeroId = 5000 + i,
                    Row = i % 8,
                    Column = i / 8,
                    Hp = 30f + (i % 7),
                    MaxHp = 60f,
                    IsAlive = true,
                    UnitAlive = true,
                    Moved = i % 5 == 0,
                    Attacked = i % 6 == 0,
                    PlayerControl = i % 2 == 0,
                };
            }
            frame.Units = units;
            frame.Audit = S2AuditUtil.Compute(frame.Header, units);
            return frame;
        }
    }
}
