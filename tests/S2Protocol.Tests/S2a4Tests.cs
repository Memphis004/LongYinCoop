using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                T2WriteGuardPreHook();
                T4SetPrefGate();
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

        // ------------------------------------------------------------------ T2 (W4(b)(c)) WriteGuard ก่อน hook + static check

        private static void T2WriteGuardPreHook()
        {
            // ---- ส่วน pure: matrix ของ S2WriteGuardHook.Decide (W4(c)) -------------------
            S2WriteGuardGroup[] groups =
            {
                S2WriteGuardGroup.MoveEntry, S2WriteGuardGroup.AttackEntry,
                S2WriteGuardGroup.PauseButtonEntry, S2WriteGuardGroup.SaveLoadEntry,
            };

            int wrongAmbientOff = 0;
            foreach (S2WriteGuardGroup g in groups)
            {
                foreach (bool policy in new[] { false, true })
                {
                    if (S2WriteGuardHook.Decide(g, false, policy) != S2WriteGuardAction.None) wrongAmbientOff++;
                }
            }
            Check("T2 ambient off = None ทุกกลุ่ม/ทุก policy (นับ counter เฉย ๆ)", wrongAmbientOff == 0);

            int wrongSaveLoad = 0;
            foreach (bool policy in new[] { false, true })
            {
                if (S2WriteGuardHook.Decide(S2WriteGuardGroup.SaveLoadEntry, true, policy)
                    != S2WriteGuardAction.LogCount) wrongSaveLoad++;
            }
            Check("T2 SaveLoad ห้าม throw ทุกกรณี (LogCount เสมอแม้ policy เปิด)", wrongSaveLoad == 0);
            Check("T2 MayEverThrow false เฉพาะ SaveLoad", !S2WriteGuardHook.MayEverThrow(S2WriteGuardGroup.SaveLoadEntry)
                && S2WriteGuardHook.MayEverThrow(S2WriteGuardGroup.MoveEntry)
                && S2WriteGuardHook.MayEverThrow(S2WriteGuardGroup.AttackEntry)
                && S2WriteGuardHook.MayEverThrow(S2WriteGuardGroup.PauseButtonEntry));

            int wrongThrowPolicy = 0;
            foreach (S2WriteGuardGroup g in new[] { S2WriteGuardGroup.MoveEntry, S2WriteGuardGroup.AttackEntry,
                                                    S2WriteGuardGroup.PauseButtonEntry })
            {
                if (S2WriteGuardHook.Decide(g, true, true) != S2WriteGuardAction.Throw) wrongThrowPolicy++;
                if (S2WriteGuardHook.Decide(g, true, false) != S2WriteGuardAction.LogCount) wrongThrowPolicy++;
            }
            Check("T2 กลุ่มอื่นตาม policy (throw เมื่อเปิด, log+count เมื่อปิด)", wrongThrowPolicy == 0);
            Check("T2 default proposal = log+count ไม่ throw (policy=false)",
                S2WriteGuardHook.Decide(S2WriteGuardGroup.MoveEntry, true, false) == S2WriteGuardAction.LogCount);

            // ---- ส่วน (b): s2_static_check.py จับ fixture ปลอมได้ + ต้นไม้จริงสะอาด ------------
            string fixtureRoot = Path.Combine(_tmp, "static-check");
            Directory.CreateDirectory(Path.Combine(fixtureRoot, "src", "LongYinCoop", "S2"));

            string clean = Path.Combine(fixtureRoot, "src", "LongYinCoop", "S2", "Clean.cs");
            File.WriteAllText(clean,
                "// comment เอ่อถึง HarmonyLib/Harmony patch probes (ไม่ใช่ code reference) ต้องผ่าน\n" +
                "/* block comment HarmonyPatch ก็ต้องผ่าน */\n" +
                "namespace Clean { static class C { public static int Add(int a, int b) => a + b; } }\n");

            string output;
            int exit = RunStaticCheck(fixtureRoot, out output);
            Check("T2 static check clean fixture = exit 0", exit == 0);
            Check("T2 static check comment-only Harmony passes", !output.Contains("VIOLATION"));

            string[] fakeCalls =
            {
                "var r = MoveCommand.StartMove(\"0:1\", 2, 3);",
                "var r = AttackCommand.StartAttack(\"0:1\", \"1:2\");",
                "BattleController.BattlePauseButtonClicked();",
                "GameDataController.Save();",
            };
            int caught = 0;
            for (int i = 0; i < fakeCalls.Length; i++)
            {
                string path = Path.Combine(fixtureRoot, "src", "LongYinCoop", "S2", "FakeCall" + i + ".cs");
                File.WriteAllText(path, "namespace F { static class K" + i + " { static void M() { "
                                        + fakeCalls[i] + " } } }\n");
                RunStaticCheck(fixtureRoot, out output);
                if (RunStaticCheckLastExit == 1 && output.Contains("write-path-call-site")) caught++;
                File.Delete(path);
            }
            Check("T2 static check catches all 4 write-path call-site fixtures", caught == fakeCalls.Length);

            string harmony = Path.Combine(fixtureRoot, "src", "LongYinCoop", "S2", "Hook.cs");
            File.WriteAllText(harmony, "using HarmonyLib;\n[HarmonyPatch] static class H { }\n");
            exit = RunStaticCheck(fixtureRoot, out output);
            Check("T2 static check catches HarmonyLib reference", exit == 1
                && output.Contains("harmony-reference"));
            File.Delete(harmony);

            string patchCall = Path.Combine(fixtureRoot, "src", "LongYinCoop", "S2", "Patcher.cs");
            File.WriteAllText(patchCall,
                "class P6 { static void Y(object harmonyInstance, object target) " +
                "{ harmonyInstance.Patch(target); } }\n");
            exit = RunStaticCheck(fixtureRoot, out output);
            Check("T2 static check catches .Patch( call", exit == 1 && output.Contains("harmony-reference"));
            File.Delete(patchCall);

            string pureCall = Path.Combine(fixtureRoot, "src", "LongYinCoop", "S2", "Caller.cs");
            File.WriteAllText(pureCall, "class Caller { static object F() => S2WriteGuardHook.Decide(0, false, false); }\n");
            exit = RunStaticCheck(fixtureRoot, out output);
            Check("T2 static check catches pure-logic call in S2/**", exit == 1
                && output.Contains("pure-logic-reference"));
            File.Delete(pureCall);

            string pureCallOutside = Path.Combine(fixtureRoot, "src", "LongYinCoop", "CoopModFake.cs");
            File.WriteAllText(pureCallOutside, "class CoopModFake { static object F() => S2WriteGuardHook.Decide(0, false, false); }\n");
            exit = RunStaticCheck(fixtureRoot, out output);
            Check("T2 static check catches pure-logic call outside S2/**", exit == 1
                && output.Contains("pure-logic-reference") && output.Contains("CoopModFake.cs"));
            File.Delete(pureCallOutside);

            // ไฟล์นิยามเองต้องผ่าน (token อยู่ในไฟล์ของมันเอง = ไม่ใช่ violation)
            string repoRoot = FindRepoRoot();
            Check("T2 repo root found from test assembly", repoRoot != null
                && File.Exists(Path.Combine(repoRoot, "tools", "s2_static_check.py")));
            if (repoRoot != null)
            {
                exit = RunStaticCheck(repoRoot, out output);
                Check("T2 REAL TREE clean: 0 write-path call sites, 0 Harmony in S2/**, pure logic uncalled",
                    exit == 0 && output.Contains("violations=0"));
            }
        }

        private static int RunStaticCheckLastExit;

        private static int RunStaticCheck(string root, out string output)
        {
            output = "";
            string script = FindRepoFile("tools", "s2_static_check.py");
            if (script == null)
            {
                output = "s2_static_check.py not found";
                RunStaticCheckLastExit = -1;
                return -1;
            }
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("python",
                    "\"" + script + "\" --root \"" + root + "\"")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using (Process process = Process.Start(psi))
                {
                    output = process.StandardOutput.ReadToEnd();
                    output += process.StandardError.ReadToEnd();
                    if (!process.WaitForExit(30000)) process.Kill();
                    RunStaticCheckLastExit = process.ExitCode;
                    return process.ExitCode;
                }
            }
            catch (Exception e)
            {
                output = "python failed: " + e.GetType().Name + ": " + e.Message;
                RunStaticCheckLastExit = -1;
                return -1;
            }
        }

        private static string FindRepoRoot()
        {
            string script = FindRepoFile("tools", "s2_static_check.py");
            if (script == null) return null;
            // script = <root>\tools\s2_static_check.py → root = สองชั้นขึ้นจากไฟล์
            return Path.GetDirectoryName(Path.GetDirectoryName(script));
        }

        private static string FindRepoFile(params string[] relative)
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                string candidate = dir.FullName;
                for (int j = 0; j < relative.Length; j++) candidate = Path.Combine(candidate, relative[j]);
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        // ------------------------------------------------------------------ T4 (W5) set_pref gate + net.cfg reload

        private static void T4SetPrefGate()
        {
            // ---- matrix (active × key × value) ของ S2SetPrefGate ------------------------
            string[] writeKeys =
            {
                "allowRemoteInput", "jitprobe", "jitprobeW2", "jitprobeW4", "jitprobeW6", "jitprobeW7",
            };
            int wrongKey = 0;
            foreach (string key in writeKeys)
            {
                if (!S2SetPrefGate.IsWritePathKey(key)) wrongKey++;
            }
            Check("T4 all 6 write-path keys recognized (case-insensitive)", wrongKey == 0
                && S2SetPrefGate.IsWritePathKey("AllowRemoteInput")
                && S2SetPrefGate.IsWritePathKey("JITPROBEW4"));
            Check("T4 non-write keys not gated", !S2SetPrefGate.IsWritePathKey("capture")
                && !S2SetPrefGate.IsWritePathKey("enabled") && !S2SetPrefGate.IsWritePathKey("")
                && !S2SetPrefGate.IsWritePathKey("unknownkey"));

            int wrongReject = 0;
            foreach (string key in writeKeys)
            {
                // active=true + value=true → ปฏิเสธ
                if (!S2SetPrefGate.ShouldReject(true, key, true)) wrongReject++;
                // ปิด (false) อนุญาตทุกสถานะ
                if (S2SetPrefGate.ShouldReject(true, key, false)) wrongReject++;
                if (S2SetPrefGate.ShouldReject(false, key, false)) wrongReject++;
                // idle → เปิดได้ตามพฤติกรรมเดิม
                if (S2SetPrefGate.ShouldReject(false, key, true)) wrongReject++;
            }
            Check("T4 gate matrix: reject only active+write-path+true", wrongReject == 0);
            Check("T4 capture=true not rejected while active",
                !S2SetPrefGate.ShouldReject(true, "capture", true));
            string error = S2SetPrefGate.RejectError("allowRemoteInput");
            Check("T4 reject error names key + reason", error.Contains("allowRemoteInput")
                && error.Contains("S2 is active"));

            // ---- net.cfg reload semantics (K30): Poll ≥ pollMs เท่านั้น + ค่า overlay โหลดใหม่ --------
            string cfgPath = Path.Combine(_tmp, "net.cfg");
            File.WriteAllText(cfgPath, "overlay = false\n");
            S2NetConfig cfg = new S2NetConfig(cfgPath, 1000);
            cfg.ForceRead(0);
            Check("T4 reload: initial overlay=false read", cfg.Current.Overlay == false);

            File.WriteAllText(cfgPath, "overlay = true\n");
            Check("T4 reload: poll within interval is a no-op", !cfg.Poll(500) && cfg.Current.Overlay == false);
            Check("T4 reload: poll past interval re-reads file", cfg.Poll(1000) && cfg.Current.Overlay == true);

            File.WriteAllText(cfgPath, "overlay = false\nenabled = false\n");
            cfg.ForceRead(2000);
            Check("T4 reload: force-read picks up multiple keys", cfg.Current.Overlay == false
                && cfg.Current.Enabled == false && cfg.Current.FormatOk);

            // ไฟล์เสีย (TOML section) = fail-closed ทั้งไฟล์ — overlay กลับ default false
            File.WriteAllText(cfgPath, "[LongYinCoop]\noverlay = true\n");
            cfg.ForceRead(3000);
            Check("T4 reload: malformed file fails closed (overlay=false)", !cfg.Current.FormatOk
                && !cfg.Current.Overlay);

            // ---- PASS-2 audit (ข้อ 1): ค่าผิดรูปแบบต้องไม่ถูกเขียน — mirror ลำดับการตัดสินจริง --------
            // IpcServer.SetPref คอมไพล์ไม่ได้ใน suite (pipe/JSON/MelonLoader deps) — ตัวตัดสินจริงพิสูจน์
            // ด้วยบรรทัดใน IpcServer.cs: gate 418-427 (IsWritePathKey + bool.TryParse + ShouldReject)
            // และ write branch ทุกตัวมี guard `if (!bool.TryParse(value, out on)) return Fail(...)` ก่อน
            // OnMain ที่เขียน pref (433/445/463/486/508/530/554) · **แก้ IpcServer.SetPref ต้องแก้ mirror นี้คู่กัน**
            BoolTryParseParity();
            MirrorMatrix();
            CheckedWriteSemantics();
            OrderingBActivationGuard();
        }

        /// <summary>
        /// PASS-2 audit เพิ่มเติม: `S2SetPrefGate.CheckedWrite` — ตัว "ตรวจแล้วเขียน" ตัวเดียวที่
        /// 6 branch ใช้ (ผ่าน IpcServer.OnMainGated บน main thread) — pure test ตรวจตัวจริง
        /// </summary>
        private static void CheckedWriteSemantics()
        {
            List<string> calls = new List<string>();

            // 1) ordering: activeNow ถูกอ่านก่อน write เสมอ (ภายในการเรียกเดียว)
            calls.Clear();
            S2SetPrefWriteOutcome ok = S2SetPrefGate.CheckedWrite(
                delegate { calls.Add("read"); return false; },
                "allowRemoteInput", true,
                delegate { calls.Add("write"); return "written"; });
            Check("T4 CheckedWrite ordering: read-active then write", calls.Count == 2
                && calls[0] == "read" && calls[1] == "write" && !ok.Rejected
                && (string)ok.Value == "written");

            // 2) reject บน main thread: ไม่เรียก write เลย (pref ไม่ถูกแตะ)
            calls.Clear();
            bool writeRan = false;
            S2SetPrefWriteOutcome rejected = S2SetPrefGate.CheckedWrite(
                delegate { calls.Add("read"); return true; },
                "allowRemoteInput", true,
                delegate { writeRan = true; return "SHOULD-NOT-RUN"; });
            Check("T4 CheckedWrite reject: write delegate never invoked", rejected.Rejected
                && !writeRan && calls.Count == 1 && calls[0] == "read");

            // 3) 6 keys ทั้งชุด: active+true → reject · false → ปิดได้ (write เรียก)
            string[] keys =
            {
                "allowRemoteInput", "jitprobe", "jitprobeW2", "jitprobeW4", "jitprobeW6", "jitprobeW7",
            };
            int wrong = 0;
            foreach (string key in keys)
            {
                if (!S2SetPrefGate.CheckedWrite(() => true, key, true,
                        delegate { writeRan = true; return (object)1; }).Rejected) wrong++;
                S2SetPrefWriteOutcome close = S2SetPrefGate.CheckedWrite(() => true, key, false,
                    delegate { return (object)"closed"; });
                if (close.Rejected || (string)close.Value != "closed") wrong++;
            }
            Check("T4 CheckedWrite all 6 keys: true rejected while active, false closes", wrong == 0);

            // 4) capture ไม่ถูก gate (ผ่านได้แม้ active)
            S2SetPrefWriteOutcome capture = S2SetPrefGate.CheckedWrite(() => true, "capture", true,
                delegate { return (object)"captured"; });
            Check("T4 CheckedWrite capture passes through", !capture.Rejected
                && (string)capture.Value == "captured");

            // 5) fresh read: activeNow อ่านค่า ณ ตอนเรียก (TOCTOU ปิดเพราะอ่าน+ตัดสิน+เขียนติดกัน)
            bool currentState = false;
            int reads = 0;
            Func<bool> liveRead = delegate { reads++; return currentState; };
            currentState = false;
            S2SetPrefWriteOutcome first = S2SetPrefGate.CheckedWrite(liveRead, "jitprobe", true,
                delegate { return (object)1; });
            currentState = true;
            S2SetPrefWriteOutcome second = S2SetPrefGate.CheckedWrite(liveRead, "jitprobe", true,
                delegate { writeRan = true; return (object)1; });
            Check("T4 CheckedWrite reads active fresh at call time", !first.Rejected && second.Rejected
                && reads == 2 && !writeRan);

            // 6) fail-closed เมื่อสถานะอ่านไม่ได้ (PASS-2 audit รอบสุดท้าย): callback null / โยน
            //    = สถานะ "ไม่ทราบ" แยกจาก "inactive ที่อ่านได้จริง" — true ของ write-path ทั้ง 6 key
            //    ถูกปฏิเสธ (ไม่เรียก write) · false ผ่านเสมอ · capture อยู่นอก gate
            writeRan = false;
            S2SetPrefWriteOutcome nullTrue = S2SetPrefGate.CheckedWrite(null, "allowRemoteInput", true,
                delegate { writeRan = true; return (object)"SHOULD-NOT-RUN"; });
            S2SetPrefWriteOutcome nullFalse = S2SetPrefGate.CheckedWrite(null, "allowRemoteInput", false,
                delegate { return (object)"closed"; });
            Check("T4 fail-closed: null callback rejects true without writing (null ≠ inactive)",
                nullTrue.Rejected && !writeRan);
            Check("T4 fail-closed: null callback still allows closing with false",
                !nullFalse.Rejected && (string)nullFalse.Value == "closed");

            Func<bool> throwing = delegate { throw new InvalidOperationException("state unreadable"); };
            writeRan = false;
            S2SetPrefWriteOutcome throwTrue = S2SetPrefGate.CheckedWrite(throwing, "jitprobe", true,
                delegate { writeRan = true; return (object)1; });
            S2SetPrefWriteOutcome throwFalse = S2SetPrefGate.CheckedWrite(throwing, "jitprobe", false,
                delegate { return (object)"closed"; });
            S2SetPrefWriteOutcome throwCapture = S2SetPrefGate.CheckedWrite(throwing, "capture", true,
                delegate { return (object)"captured"; });
            Check("T4 fail-closed: throwing callback rejects true without writing", throwTrue.Rejected
                && !writeRan);
            Check("T4 fail-closed: throwing callback allows false (close works)", !throwFalse.Rejected
                && (string)throwFalse.Value == "closed");
            Check("T4 fail-closed: capture stays outside gate even when state unreadable",
                !throwCapture.Rejected && (string)throwCapture.Value == "captured");

            // แยก "inactive ที่อ่านได้จริง" ออกจาก "ไม่ทราบ": callback คืน false/true ตรงตามค่า
            S2SetPrefWriteOutcome readableFalse = S2SetPrefGate.CheckedWrite(() => false,
                "allowRemoteInput", true, delegate { return (object)"idle-write"; });
            S2SetPrefWriteOutcome readableTrue = S2SetPrefGate.CheckedWrite(() => true, "jitprobeW4", true,
                delegate { writeRan = true; return (object)1; });
            Check("T4 callback returns false = readable inactive, true allowed", !readableFalse.Rejected
                && (string)readableFalse.Value == "idle-write");
            Check("T4 callback returns true = rejected", readableTrue.Rejected && !writeRan);

            // 6 key ทั้งชุดภายใต้สถานะไม่ทราบ: true ถูกปฏิเสธครบทุก key
            int unknownLeak = 0;
            foreach (string key in keys)
            {
                if (!S2SetPrefGate.CheckedWrite(null, key, true,
                        delegate { writeRan = true; return (object)1; }).Rejected) unknownLeak++;
            }
            Check("T4 fail-closed: unknown state rejects true on all 6 write-path keys", unknownLeak == 0);

            // ---- final fail-closed audit: throwing sweep ครบทั้ง 6 keys (ก่อนหน้าทดสอบตรงแค่ jitprobe)
            int throwSweepLeak = 0;
            foreach (string key in keys)
            {
                writeRan = false;
                if (!S2SetPrefGate.CheckedWrite(throwing, key, true,
                        delegate { writeRan = true; return (object)1; }).Rejected) throwSweepLeak++;
                if (writeRan) throwSweepLeak++;   // write ต้องไม่ถูกเรียกเลย
            }
            Check("T4 fail-closed: throwing callback rejects true on all 6 keys, write never invoked",
                throwSweepLeak == 0);

            int throwCloseLeak = 0;
            foreach (string key in keys)
            {
                S2SetPrefWriteOutcome closed = S2SetPrefGate.CheckedWrite(throwing, key, false,
                    delegate { return (object)"closed"; });
                if (closed.Rejected || (string)closed.Value != "closed") throwCloseLeak++;
            }
            Check("T4 fail-closed: throwing callback still allows false (close) on all 6 keys",
                throwCloseLeak == 0);
            Check("T4 fail-closed: capture passes under throwing callback (outside gate)",
                !throwCapture.Rejected && (string)throwCapture.Value == "captured");

            // ---- นโยบาย "S2 service ไม่มีอยู่" — pure helper ServiceStateToActive (ตัวจริง) -----------
            // callback จริงใน CoopMod ใช้สูตรเดียวกัน (s == null || s.Active) — ดู CoopMod.cs
            Check("T4 service-absent: null service ⇒ gate sees active (fail-closed)",
                S2SetPrefGate.ServiceStateToActive(false, false)
                && S2SetPrefGate.ServiceStateToActive(false, true));
            Check("T4 service-absent: present+inactive ⇒ readable inactive (original behavior)",
                !S2SetPrefGate.ServiceStateToActive(true, false));
            Check("T4 service-absent: present+active ⇒ active",
                S2SetPrefGate.ServiceStateToActive(true, true));

            // end-to-end ผ่าน CheckedWrite: สถานะจาก helper ป้อนเข้า gate จริง
            writeRan = false;
            S2SetPrefWriteOutcome absentReject = S2SetPrefGate.CheckedWrite(
                () => S2SetPrefGate.ServiceStateToActive(false, false),
                "allowRemoteInput", true, delegate { writeRan = true; return (object)1; });
            S2SetPrefWriteOutcome idlePresent = S2SetPrefGate.CheckedWrite(
                () => S2SetPrefGate.ServiceStateToActive(true, false),
                "allowRemoteInput", true, delegate { return (object)"idle-ok"; });
            S2SetPrefWriteOutcome absentClose = S2SetPrefGate.CheckedWrite(
                () => S2SetPrefGate.ServiceStateToActive(false, false),
                "allowRemoteInput", false, delegate { return (object)"closed"; });
            S2SetPrefWriteOutcome absentCapture = S2SetPrefGate.CheckedWrite(
                () => S2SetPrefGate.ServiceStateToActive(false, false),
                "capture", true, delegate { return (object)"captured"; });
            Check("T4 service-absent: true rejected end-to-end without writing",
                absentReject.Rejected && !writeRan);
            Check("T4 service-absent: present+inactive keeps original behavior (true allowed)",
                !idlePresent.Rejected && (string)idlePresent.Value == "idle-ok");
            Check("T4 service-absent: false still closes", !absentClose.Rejected
                && (string)absentClose.Value == "closed");
            Check("T4 service-absent: capture still outside gate", !absentCapture.Rejected
                && (string)absentCapture.Value == "captured");

            // ---- final IPC pre-check closure: ReadGateActive ตัวจริง (helper เดียวกันทั้งสองชั้น) ----
            // (a) ตัว helper โดยตรง
            Check("T4 ReadGateActive: null callback ⇒ active (fail-closed)",
                S2SetPrefGate.ReadGateActive(null));
            Check("T4 ReadGateActive: throwing callback ⇒ active",
                S2SetPrefGate.ReadGateActive(throwing));
            Check("T4 ReadGateActive: returns false = readable inactive",
                !S2SetPrefGate.ReadGateActive(() => false));
            Check("T4 ReadGateActive: returns true = active",
                S2SetPrefGate.ReadGateActive(() => true));

            // (b) mirror ของ pre-check จริง (IpcServer.SetPref) ซึ่งใช้ ReadGateActive ตัวจริง
            int preLeak = 0;
            foreach (string key in keys)
            {
                if (!MirrorIpcPreCheckRejects(null, key, "true")) preLeak++;
                if (!MirrorIpcPreCheckRejects(throwing, key, "true")) preLeak++;
            }
            Check("T4 pre-check fail-closed: null/throwing callback rejects true on all 6 keys",
                preLeak == 0);
            int preFalsePass = 0;
            foreach (string key in keys)
            {
                if (MirrorIpcPreCheckRejects(null, key, "false")) preFalsePass++;
                if (MirrorIpcPreCheckRejects(throwing, key, "false")) preFalsePass++;
            }
            Check("T4 pre-check: false never rejected (close flows to main-thread layer)",
                preFalsePass == 0);
            Check("T4 pre-check: capture never rejected regardless of callback state",
                !MirrorIpcPreCheckRejects(null, "capture", "true")
                && !MirrorIpcPreCheckRejects(throwing, "capture", "true"));
            Check("T4 pre-check: readable inactive flows past pre-check (layer 2 decides)",
                !MirrorIpcPreCheckRejects(() => false, "allowRemoteInput", "true"));
            Check("T4 pre-check: readable active rejects immediately at IPC thread",
                MirrorIpcPreCheckRejects(() => true, "allowRemoteInput", "true"));

            // (c) TOCTOU มุมใหม่: IPC อ่านสถานะไม่ได้เลย (callback null) ⇒ ชั้น 1 ปฏิเสธเอง
            Check("T4 TOCTOU unreadable-IPC: null callback rejects true at pre-check",
                MirrorGatedSetPref(null, true, "allowRemoteInput", "true") == null);
        }

        /// <summary>
        /// mirror ของ pre-check ใน IpcServer.SetPref (IpcServer.cs:429-441) — ใช้
        /// `ReadGateActive` ตัวจริงเดียวกับ production ⇒ ทดสอบ semantics ของ pre-check โดยตรง
        /// คืน true = pre-check ปฏิเสธ (counter/log ที่ IpcServer — ไม่ถึง dispatcher/OnMainGated)
        /// </summary>
        private static bool MirrorIpcPreCheckRejects(Func<bool> callback, string key, string value)
        {
            bool on;
            return S2SetPrefGate.IsWritePathKey(key)
                && bool.TryParse(value, out on)
                && S2SetPrefGate.ShouldReject(S2SetPrefGate.ReadGateActive(callback), key, on);
        }

        /// <summary>
        /// PASS-2 audit ข้อ (1) — ordering B: S2 activate ขณะ pref เปิดอยู่ต้องถูกปฏิเสธ
        /// mirror ของ `S2HostService.ApplySettings` (S2HostService.cs:210-227): want ต้องมี
        /// `GuardWritePathsClosed` (:236-246) เป็นจริง — อ่าน pref สดบน main thread ทุก poll
        /// </summary>
        private static void OrderingBActivationGuard()
        {
            // ตรรกะจริงของ S2WriteGuard (pure ตัวเดียวกับที่ Activate เรียก):
            Check("T4 ordering-B: allowRemoteInput=true blocks activation (WritePathsClosed=false)",
                !S2WriteGuard.WritePathsClosed(true, false, false, false, false, false));
            Check("T4 ordering-B: any of 6 prefs true blocks activation",
                !S2WriteGuard.WritePathsClosed(false, true, false, false, false, false)
                && !S2WriteGuard.WritePathsClosed(false, false, true, false, false, false)
                && !S2WriteGuard.WritePathsClosed(false, false, false, true, false, false)
                && !S2WriteGuard.WritePathsClosed(false, false, false, false, true, false)
                && !S2WriteGuard.WritePathsClosed(false, false, false, false, false, true));
            Check("T4 ordering-B: all closed = activation guard passes",
                S2WriteGuard.WritePathsClosed(false, false, false, false, false, false));

            // mirror ของ want ใน ApplySettings (ตัดสิน Activate/Deactivate):
            // เคส "pref ถูกเปิดขณะ S2 active" → poll ถัดไป want=false ≠ _active=true ⇒ Deactivate
            bool enabled = true, valid = true, host = true;
            bool prefOpen = true;
            bool want = enabled && valid && host && S2WriteGuard.WritePathsClosed(prefOpen,
                false, false, false, false, false);
            Check("T4 ordering-B mirror: want=false while pref open ⇒ S2 deactivates (≤1 poll)",
                want == false);
            prefOpen = false;
            want = enabled && valid && host && S2WriteGuard.WritePathsClosed(prefOpen,
                false, false, false, false, false);
            Check("T4 ordering-B mirror: want=true once write path closed", want == true);
        }

        /// <summary>ค่าที่ bool.TryParse (ตัวเดียวกับที่ IpcServer ทั้ง gate และ branch ใช้) รับ/ไม่รับ</summary>
        private static void BoolTryParseParity()
        {
            bool on;
            Check("T4 parse parity: true/TRUE/\" false \" accepted",
                bool.TryParse("true", out on) && on
                && bool.TryParse("TRUE", out on) && on
                && bool.TryParse(" false ", out on) && !on
                && bool.TryParse("False", out on) && !on);
            Check("T4 parse parity: 1/on/yes/empty/garbage rejected (Fail ก่อนเขียน)",
                !bool.TryParse("1", out on) && !bool.TryParse("on", out on)
                && !bool.TryParse("yes", out on) && !bool.TryParse("", out on)
                && !bool.TryParse("garbage", out on) && !bool.TryParse(null, out on));
        }

        /// <summary>
        /// mirror ของ IpcServer.SetPref **หลังปิด TOCTOU** (ตรวจ 2 ชั้น — IpcServer.cs:431-443 pre-check
        /// + OnMainGated ตรวจซ้ำบน main thread ใน callback เดียวกับการเขียน) — คืนค่า bool
        /// ที่ "จะถูกเขียนลง pref" · null = ไม่มีการเขียนเกิดขึ้น
        /// `activeAtIpc` = **callback จริง** ที่ pre-check อ่าน (อาจ null/โยน — ผ่าน ReadGateActive ตัวจริง
        /// เดียวกับ production ⇒ สถานะไม่ทราบ = มองเป็น active)
        /// `activeAtMainCallback` = สถานะจริงตอน callback บน main thread (จุดที่ S2 เปลี่ยนสถานะเท่านั้น)
        /// **แก้ IpcServer.SetPref/OnMainGated ต้องแก้ mirror นี้คู่กัน**
        /// </summary>
        private static bool? MirrorGatedSetPref(Func<bool> activeAtIpc, bool activeAtMainCallback,
                                                string key, string value)
        {
            bool on;
            // ชั้น 1: pre-check บน IPC thread (fast-fail — ใช้ ReadGateActive ตัวจริงเดียวกับ production;
            // สถานะไม่ทราบ = fail-closed แต่ไม่ใช่หลักประกัน ชั้น 2 ตัดสินอีกครั้งบน main thread)
            if (S2SetPrefGate.IsWritePathKey(key)
                && bool.TryParse(value, out on)
                && S2SetPrefGate.ShouldReject(S2SetPrefGate.ReadGateActive(activeAtIpc), key, on))
            {
                return null;
            }

            // branch guard: case-sensitive + TryParse ก่อน OnMainGated
            string[] branchKeys =
            {
                "allowRemoteInput", "jitprobe", "jitprobeW2", "jitprobeW4", "jitprobeW6", "jitprobeW7",
            };
            if (Array.IndexOf(branchKeys, key) < 0) return null;      // Fail(PrefUnknown)
            if (!bool.TryParse(value, out on)) return null;           // Fail(UnknownCommand) — ไม่เขียน

            // ชั้น 2 (authoritative): main thread ภายใน callback เดียวกับการเขียน — S2 เปลี่ยนสถานะ
            // บน main thread เท่านั้น จึงไม่มีการแทรกได้ระหว่างบรรทัดนี้กับ write()
            if (S2SetPrefGate.ShouldReject(activeAtMainCallback, key, on)) return null;
            return on;
        }

        private static void MirrorMatrix()
        {
            string[] keys =
            {
                "allowRemoteInput", "jitprobe", "jitprobeW2", "jitprobeW4", "jitprobeW6", "jitprobeW7",
            };
            string[] values = { "true", "TRUE", "false", "False", "1", "on", "yes", "", "garbage", null };

            int activeWriteTrue = 0;
            int malformedWritten = 0;
            int idleMismatch = 0;
            foreach (string key in keys)
            {
                foreach (string value in values)
                {
                    bool? written = MirrorGatedSetPref(() => true, true, key, value);
                    if (written == true) activeWriteTrue++;
                    if (written != null && !"true".Equals(value, StringComparison.OrdinalIgnoreCase)
                        && !"false".Equals(value, StringComparison.OrdinalIgnoreCase)) malformedWritten++;

                    bool? idleWritten = MirrorGatedSetPref(() => false, false, key, value);
                    bool parseOk = bool.TryParse(value, out bool parsed);
                    bool? expectedIdle = parseOk ? parsed : (bool?)null;
                    if (idleWritten != expectedIdle) idleMismatch++;
                }
            }
            Check("T4 while ACTIVE (both layers): no representation writes true to a write-path pref",
                activeWriteTrue == 0);
            Check("T4 malformed values (1/on/yes/empty/garbage/null) never write in any state",
                malformedWritten == 0);
            Check("T4 idle: writes exactly bool.TryParse(value) (behavior unchanged)",
                idleMismatch == 0);

            // ---- TOCTOU: สถานะเปลี่ยนระหว่าง pre-check (IPC) กับ callback (main thread) -------------
            int toctouLeak = 0;
            foreach (string key in keys)
            {
                // กรณีสำคัญ: pre-check อ่าน stale false (S2 ยัง idle ตอนตรวจ) แต่พอถึง callback บน
                // main thread S2 active แล้ว → ชั้น 2 ต้องกัน "true" ไม่ให้เขียน
                if (MirrorGatedSetPref(() => false, true, key, "true") != null) toctouLeak++;
                if (MirrorGatedSetPref(() => false, true, key, "TRUE") != null) toctouLeak++;
            }
            Check("T4 TOCTOU (stale pre-check=false, callback sees active): true never written",
                toctouLeak == 0);

            // กรณี inverse: pre-check เห็น active → ปฏิเสธที่ชั้น 1 ทันที (แม้ callback จะ idle ก็ไม่ไปถึง)
            int earlyReject = 0;
            foreach (string key in keys)
            {
                if (MirrorGatedSetPref(() => true, false, key, "true") != null) earlyReject++;
            }
            Check("T4 pre-check sees active: rejected on IPC thread (never reaches callback)",
                earlyReject == 0);

            // false ต้องปิดค่าได้ทุกกรณี (รวมตอน callback เห็น active)
            bool falseCloses = true;
            foreach (string key in keys)
            {
                falseCloses &= MirrorGatedSetPref(() => false, true, key, "false") == false
                            && MirrorGatedSetPref(() => true, true, key, "false") == false;
            }
            Check("T4 'false' still closes the pref in every state (both layers)", falseCloses);

            // เคสเดี่ยวที่ละเอียดสุด
            Check("T4 TOCTOU key case: allowRemoteInput stale-false → active callback = no write",
                MirrorGatedSetPref(() => false, true, "allowRemoteInput", "true") == null);
            Check("T4 idle both layers + true = writes true (behavior unchanged)",
                MirrorGatedSetPref(() => false, false, "allowRemoteInput", "true") == true);
            Check("T4 idle pre-check + active callback + \"1\" = no write (parse fail before callback)",
                MirrorGatedSetPref(() => false, true, "allowRemoteInput", "1") == null);
            Check("T4 idle pre-check + active callback + unknown key (case mismatch) = no write",
                MirrorGatedSetPref(() => false, true, "AllowRemoteInput", "true") == null);
            Check("T4 capture=true unaffected by gate (not a write-path key)",
                !S2SetPrefGate.ShouldReject(true, "capture", true));
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
