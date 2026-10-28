using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LongYinCoop.TimeV2a;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// V2 ชุด A (มติ V2A-4, 2026-10-29) — เทสต์ออฟไลน์ของตรรกะ pure ใน TimeV2aRules.cs
    /// (TimeV2aSampler.cs ไม่ถูกลิงก์ — อ้าง Il2Cpp/Unity ต้อง compile เฉพาะใน mod csproj)
    ///
    /// ครอบตามคำสั่ง B3: day_tick ตรวจถูก · work_abort · ค่าไม่ถูกต้อง/null (NaN, hour ขาด, statics ขาด)
    /// · writer buffered flush · gate pref off = ไม่ทำอะไร · รูปแบบ NDJSON ต้องตรงสิ่งที่
    /// tools/time_v2a_analyze.py อ่าน (work_start ต้องพก t/leftWorkingDay/skipping/dayTime/pauseTime)
    ///
    /// โมเดลจำลองปั๊ม = time-static-notes §2.4 (โครง disasm): initial pause → [วัน: nextDayTime +=
    /// dt จนถึง threshold=dayTime×k → tick + reset + leftPauseTime=pauseTime (วันสุดท้าย 2×)]
    /// → … → final 2×pause → finish (workFinished=1, working=0)
    /// </summary>
    internal static class TimeV2aTests
    {
        private static int _pass;
        private static int _fail;
        private static string _tmp;

        public static int Run()
        {
            _pass = 0;
            _fail = 0;
            _tmp = Path.Combine(Path.GetTempPath(), "timev2a-tests-" + Environment.ProcessId);
            Directory.CreateDirectory(_tmp);

            Console.WriteLine();
            Console.WriteLine("== TimeV2a tests ==");
            try
            {
                GateTests();
                DetectorIdleTests();
                DetectorSessionTests();
                DetectorEdgeTests();
                NdjsonFormatTests();
                WriterTests();
            }
            finally
            {
                try { Directory.Delete(_tmp, true); } catch { }
            }

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "TimeV2a RESULT {0} (pass {1} fail {2})", _fail == 0 ? "PASS" : "FAIL", _pass, _fail));
            return _fail;
        }

        private static void Check(string name, bool ok)
        {
            if (ok) _pass++;
            else _fail++;
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "[{0}] timev2a: {1}", ok ? "PASS" : "FAIL", name));
        }

        // ---- fixtures -------------------------------------------------------

        private static TimeV2aObs Obs(bool working, int left, int total, float nextDay, float leftPause,
                                      bool workFinished, bool skipping, bool hasStatics, long tms, long frame,
                                      bool hasInstance = true, bool noCancel = false,
                                      bool hasHour = true, float hour = 7.5f)
        {
            return new TimeV2aObs
            {
                HasInstance = hasInstance,
                Working = working,
                LeftWorkingDay = left,
                TotalWorkingDay = total,
                NextDayTime = nextDay,
                LeftPauseTime = leftPause,
                WorkFinished = workFinished,
                NoCancel = noCancel,
                Skipping = skipping,
                HasStatics = hasStatics,
                DayTime = 1f,
                PauseTime = 0.25f,
                HasHour = hasHour,
                Hour = hour,
                TMs = tms,
                Frame = frame,
            };
        }

        private static List<TimeV2aEvent> FeedAll(TimeV2aDetector d, IEnumerable<TimeV2aObs> obsList)
        {
            List<TimeV2aEvent> all = new List<TimeV2aEvent>();
            List<TimeV2aEvent> frame = new List<TimeV2aEvent>(4);
            foreach (TimeV2aObs o in obsList)
            {
                frame.Clear();
                d.Feed(in o, frame);
                all.AddRange(frame);
            }
            return all;
        }

        /// <summary>จำนวน event ที่คาดหวังของ session ปกติ N วัน = 1 start + N tick + (N+1) pause_end + 1 end</summary>
        internal static int ExpectedEventCount(int n)
        {
            return 1 + n + (n + 1) + 1;
        }

        /// <summary>
        /// จำลองปั๊มตาม time-static-notes §2.4 แบบเฟรมต่อเฟรม — ใช้ทั้งเทสต์และ fixture สำหรับ
        /// ตรวจ analyzer จริง (คำสั่ง B4) · work_start เกิดที่ frame 1005 หลัง idle 5 เฟรม
        /// </summary>
        internal static List<TimeV2aObs> SimulateSession(int n, float dayTime, float pauseTime, float k,
                                                         long startTms, int fps, bool skipping,
                                                         bool finishNormally = true)
        {
            List<TimeV2aObs> list = new List<TimeV2aObs>();
            double dtMs = 1000.0 / fps;
            float dt = (float)(dtMs / 1000.0);
            long t = startTms;
            long frame = 1000;
            bool working = false;
            int left = 0, total = 0;
            float nextDay = 0f, leftPause = 0f;
            bool workFinished = false;
            float hour = 7.5f;
            bool noCancel = false;

            for (int i = 0; i < 5; i++)   // idle ก่อนเริ่มงาน
            {
                list.Add(Obs(false, 0, 0, 0f, 0f, false, skipping, true, t, frame++, noCancel: noCancel, hour: hour));
                t += (long)dtMs;
            }

            working = true; left = n; total = 0; nextDay = 0f; leftPause = pauseTime; workFinished = false;
            list.Add(Obs(true, left, total, nextDay, leftPause, workFinished, skipping, true, t, frame++, noCancel: noCancel, hour: hour));

            int guard = 0;
            while (working && guard++ < 200000)
            {
                t += (long)dtMs;
                if (left <= 0 && leftPause <= 0f)
                {
                    // กิ่งจบงาน (0x9CBCC7): workFinished=1 → working=0 (abort = StopWorking(false) → workFinished=0)
                    workFinished = finishNormally;
                    working = false;
                    list.Add(Obs(working, left, total, nextDay, leftPause, workFinished, skipping, true, t, frame++, noCancel: noCancel, hour: hour));
                    break;
                }
                if (leftPause > 0f)
                {
                    leftPause -= dt;
                    if (leftPause < 0f) leftPause = 0f;
                }
                else
                {
                    nextDay += dt;
                    float threshold = dayTime * k;
                    if (nextDay >= threshold)
                    {
                        nextDay = 0f;
                        left -= 1;
                        total += 1;
                        leftPause = left > 0 ? pauseTime : 2f * pauseTime;
                        hour = (hour + 24f) % 24f;
                    }
                }
                list.Add(Obs(working, left, total, nextDay, leftPause, workFinished, skipping, true, t, frame++, noCancel: noCancel, hour: hour));
            }

            for (int i = 0; i < 5; i++)   // idle หลังจบงาน
            {
                list.Add(Obs(false, 0, 0, 0f, 0f, workFinished, skipping, true, t, frame++, noCancel: noCancel, hour: hour));
                t += (long)dtMs;
            }
            return list;
        }

        /// <summary>ดึงค่าของ key จากบรรทัด NDJSON (เชิงเทสต์เท่านั้น)</summary>
        private static string ValueOf(string line, string key)
        {
            string needle = "\"" + key + "\":";
            int at = line.IndexOf(needle, StringComparison.Ordinal);
            if (at < 0) return null;
            int from = at + needle.Length;
            int to = line.IndexOf(',', from);
            if (to < 0) to = line.IndexOf('}', from);
            if (line[from] == '"') { from++; to--; }
            return line.Substring(from, to - from);
        }

        // ---- tests ----------------------------------------------------------

        private static void GateTests()
        {
            Check("gate pref off -> PrefOff (ไม่แตะ pointer ด้วยซ้ำ)",
                  TimeV2aGate.Evaluate(false, "C:\\x\\time-v2a.ndjson") == TimeV2aGate.Verdict.PrefOff);
            Check("gate ไม่มี pointer file -> NoPointerLine",
                  TimeV2aGate.Evaluate(true, null) == TimeV2aGate.Verdict.NoPointerLine);
            Check("gate pointer ว่าง/ช่องว่าง -> NoPointerLine",
                  TimeV2aGate.Evaluate(true, "   ") == TimeV2aGate.Verdict.NoPointerLine);
            Check("gate pointer นามสกุล .txt -> NotNdjson",
                  TimeV2aGate.Evaluate(true, "C:\\x\\y.txt") == TimeV2aGate.Verdict.NotNdjson);
            Check("gate pointer .ndjson -> Ok",
                  TimeV2aGate.Evaluate(true, "C:\\x\\time-v2a.ndjson") == TimeV2aGate.Verdict.Ok);
            Check("gate PathOf ตัด quote/ช่องว่าง",
                  TimeV2aGate.PathOf("  \"C:\\x\\time-v2a.ndjson\" ") == "C:\\x\\time-v2a.ndjson");
        }

        private static void DetectorIdleTests()
        {
            TimeV2aDetector d = new TimeV2aDetector();
            List<TimeV2aObs> idle = new List<TimeV2aObs>();
            long t = 1_000_000;
            for (int i = 0; i < 20; i++)
            {
                idle.Add(Obs(false, 0, 0, 0f, 0f, false, false, true, t, i));
                t += 16;
            }
            List<TimeV2aEvent> ev = FeedAll(d, idle);
            Check("นอกงานทั้ง stream = 0 event", ev.Count == 0);

            // instance หายกลางคัน (โหลด scene) โดยไม่เคยมีงาน = ยัง 0 event
            List<TimeV2aObs> noInstance = new List<TimeV2aObs>();
            for (int i = 0; i < 5; i++)
            {
                noInstance.Add(Obs(false, 0, 0, 0f, 0f, false, false, true, t, 100 + i, hasInstance: false));
                t += 16;
            }
            ev = FeedAll(d, noInstance);
            Check("Instance=null โดยไม่มี session ค้าง = 0 event", ev.Count == 0);
        }

        private static void DetectorSessionTests()
        {
            // N=5 ปกติ (k=1): work_start + pause_end×6 + day_tick×5 + work_end
            List<TimeV2aEvent> ev = FeedAll(new TimeV2aDetector(),
                SimulateSession(5, 1f, 0.25f, 1f, 1_000_000, 60, skipping: false));
            int starts = 0, ticks = 0, pauses = 0, ends = 0, aborts = 0;
            foreach (TimeV2aEvent e in ev)
            {
                if (e.Ev == "work_start") starts++;
                else if (e.Ev == "day_tick") ticks++;
                else if (e.Ev == "pause_end") pauses++;
                else if (e.Ev == "work_end") ends++;
                else if (e.Ev == "work_abort") aborts++;
            }
            Check("N=5: work_start ครั้งเดียว", starts == 1);
            Check("N=5: day_tick == N", ticks == 5);
            Check("N=5: pause_end == N+1 (initial + หลัง tick ทุกวัน รวม final 2p)", pauses == 6);
            Check("N=5: work_end ครั้งเดียว ไม่มี work_abort", ends == 1 && aborts == 0);
            Check("N=5: จำนวน event รวมตรงโครง", ev.Count == ExpectedEventCount(5));

            TimeV2aEvent start = ev[0];
            Check("work_start อยู่บรรทัดแรกและพก leftWorkingDay=N",
                  start.Ev == "work_start" && start.LeftWorkingDay == 5);
            Check("work_start พก noCancel + skipK=1.0 (skipping=false)",
                  start.WithNoCancel && !start.NoCancel && start.WithSkipK && start.SkipK == 1f);
            Check("work_end เป็น event สุดท้าย", ev[ev.Count - 1].Ev == "work_end");

            // day_tick ต้องเกิดเมื่อ leftWorkingDay ลดลงเท่านั้น — เฟรมนับวันที่ยังไม่ถึง threshold ต้องเงียบ
            int tickSeen = 0;
            int lastLeft = int.MaxValue;
            bool monotonic = true;
            foreach (TimeV2aEvent e in ev)
            {
                if (e.Ev != "day_tick") continue;
                tickSeen++;
                if (e.LeftWorkingDay >= lastLeft) monotonic = false;
                lastLeft = e.LeftWorkingDay;
            }
            Check("day_tick: leftWorkingDay ลดเครื่องเดียวทุก tick (5→4→3→2→1→0)",
                  monotonic && tickSeen == 5 && lastLeft == 0);

            // skip (k=0.5): threshold = 0.5 — skipK ที่วัดได้บน day_tick ≈ 0.5 (ความหยาบ 1 เฟรม @60fps)
            List<TimeV2aEvent> evSkip = FeedAll(new TimeV2aDetector(),
                SimulateSession(3, 1f, 0.25f, 0.5f, 2_000_000, 60, skipping: true));
            float kSum = 0f; int kCount = 0; bool allWithK = true;
            foreach (TimeV2aEvent e in evSkip)
            {
                if (e.Ev != "day_tick") continue;
                if (!e.WithSkipK) { allWithK = false; continue; }
                kSum += e.SkipK;
                kCount++;
            }
            Check("skip k=0.5: day_tick ทุกตัวพก skipK", allWithK && kCount == 3);
            float kAvg = kCount > 0 ? kSum / kCount : 0f;
            Check("skip k=0.5: skipK เฉลี่ย ≈ 0.5 (ห่าง ≤ 0.05)",
                  Math.Abs(kAvg - 0.5f) <= 0.05f);

            // work_start ขณะ skipping=true (stale latch) = ไม่ใส่ skipK (ไม่เดาค่า TestMode)
            TimeV2aDetector dStale = new TimeV2aDetector();
            List<TimeV2aEvent> frame = new List<TimeV2aEvent>(4);
            TimeV2aObs obsStale = Obs(true, 2, 0, 0f, 0.25f, false, true, true, 3_500_000, 42);
            dStale.Feed(in obsStale, frame);
            Check("work_start ขณะ skipping=true: ไม่พก skipK (stale latch — ยังไม่รู้ k)",
                  frame.Count == 1 && frame[0].WithNoCancel && !frame[0].WithSkipK);

            // abort: working 1→0 โดย workFinished=false
            List<TimeV2aEvent> evAbort = FeedAll(new TimeV2aDetector(),
                SimulateSession(3, 1f, 0.25f, 1f, 3_000_000, 60, skipping: false, finishNormally: false));
            bool hasAbort = false; bool abortNoEnd = true;
            foreach (TimeV2aEvent e in evAbort)
            {
                if (e.Ev == "work_abort") hasAbort = true;
                if (e.Ev == "work_end") abortNoEnd = false;
            }
            Check("work_abort: ปลาย session เป็น abort ไม่ใช่ end", hasAbort && abortNoEnd);

            // detector ใช้ซ้ำได้ — session ใหม่หลังจบ session เดิม
            TimeV2aDetector d2 = new TimeV2aDetector();
            List<TimeV2aObs> s1 = SimulateSession(1, 1f, 0.25f, 1f, 4_000_000, 60, skipping: false);
            List<TimeV2aObs> s2 = SimulateSession(2, 1f, 0.25f, 1f, 5_000_000, 60, skipping: false);
            List<TimeV2aEvent> ev2 = new List<TimeV2aEvent>();
            List<TimeV2aEvent> part = FeedAll(d2, s1);
            ev2.AddRange(part);
            part = FeedAll(d2, s2);
            ev2.AddRange(part);
            int starts2 = 0, ends2 = 0, ticks2 = 0;
            foreach (TimeV2aEvent e in ev2)
            {
                if (e.Ev == "work_start") starts2++;
                if (e.Ev == "work_end") ends2++;
                if (e.Ev == "day_tick") ticks2++;
            }
            Check("detector ใช้ซ้ำ: 2 sessions → start 2 / end 2 / tick 3 รวม",
                  starts2 == 2 && ends2 == 2 && ticks2 == 3);
        }

        private static void DetectorEdgeTests()
        {
            // NaN ของ nextDayTime ต้องไม่ crash และไม่กลายเป็น skipK
            TimeV2aDetector d = new TimeV2aDetector();
            List<TimeV2aEvent> frame = new List<TimeV2aEvent>(4);
            long t = 6_000_000;
            TimeV2aObs o = Obs(true, 1, 0, 0f, 0f, false, false, true, t, 1);
            d.Feed(in o, frame);
            Check("เฟรมแรกของงาน = work_start ตัวเดียว", frame.Count == 1 && frame[0].Ev == "work_start");
            frame.Clear();
            o = Obs(true, 1, 0, float.NaN, 0f, false, false, true, t + 16, 2);
            d.Feed(in o, frame);
            Check("NaN nextDayTime: ไม่มี event หลุดจากเฟรมนับวัน", frame.Count == 0);
            frame.Clear();
            o = Obs(true, 0, 1, 0f, 0.5f, false, false, true, t + 32, 3);
            d.Feed(in o, frame);
            Check("NaN nextDayTime: day_tick ยังตรวจจับได้ แต่ไม่พก skipK",
                  frame.Count == 1 && frame[0].Ev == "day_tick" && !frame[0].WithSkipK);
            frame.Clear();
            o = Obs(false, 0, 1, 0f, 0f, true, false, true, t + 48, 4);
            d.Feed(in o, frame);
            Check("NaN nextDayTime: session ปิดด้วย work_end ปกติ",
                  frame.Count == 1 && frame[0].Ev == "work_end");

            // statics อ่านไม่ได้ = ไม่ปล่อย work_start (session ไม่ถูกเปิด — ข้อมูลไม่พอห้ามอ้าง)
            TimeV2aDetector dNoStatics = new TimeV2aDetector();
            List<TimeV2aObs> noStatics = SimulateSession(2, 1f, 0.25f, 1f, 7_000_000, 60, skipping: false);
            for (int i = 0; i < noStatics.Count; i++)
            {
                TimeV2aObs m = noStatics[i];
                m.HasStatics = false;
                noStatics[i] = m;
            }
            List<TimeV2aEvent> evNs = FeedAll(dNoStatics, noStatics);
            bool nsNoSession = evNs.Count == 0;
            foreach (TimeV2aEvent e in evNs)
            {
                if (e.Ev == "work_start" || e.Ev == "day_tick") nsNoSession = false;
            }
            Check("statics ขาด: ไม่มี work_start/day_tick (ไม่เปิด session)", nsNoSession);

            // hour ขาด = event ยังครบ (analyzer ไม่ใช้ hour)
            TimeV2aDetector dNoHour = new TimeV2aDetector();
            List<TimeV2aObs> noHourObs = SimulateSession(1, 1f, 0.25f, 1f, 8_000_000, 60, skipping: false);
            for (int i = 0; i < noHourObs.Count; i++)
            {
                TimeV2aObs m = noHourObs[i];
                m.HasHour = false;
                noHourObs[i] = m;
            }
            List<TimeV2aEvent> evNh = FeedAll(dNoHour, noHourObs);
            Check("hour ขาด: event ครบตามโครง N=1 และไม่มี HasHour",
                  evNh.Count == ExpectedEventCount(1) && evNh[0].Ev == "work_start"
                  && evNh[evNh.Count - 1].Ev == "work_end" && !evNh[0].HasHour);
        }

        private static void NdjsonFormatTests()
        {
            List<TimeV2aEvent> ev = FeedAll(new TimeV2aDetector(),
                SimulateSession(1, 1f, 0.25f, 1f, 1_700_000_000_000, 60, skipping: false));
            string startLine = ev[0].ToNdjsonLine();
            // ลำดับฟิลด์ตามสเปกแพ็กเกจ 2 + เสริม noCancel/skipK (errata N3) — work_start ที่ frame 1005
            // (idle 5 เฟรม @60fps = t + 80 ms) · skipK ของ start = 1.0 (skipping=false)
            string expectedStart = "{\"t\":1700000000080,\"frame\":1005,\"ev\":\"work_start\",\"leftWorkingDay\":1," +
                "\"totalWorkingDay\":0,\"nextDayTime\":0,\"leftPauseTime\":0.25,\"workFinished\":false," +
                "\"skipping\":false,\"dayTime\":1,\"pauseTime\":0.25,\"hour\":7.5,\"noCancel\":false,\"skipK\":1}";
            Check("work_start NDJSON ตรงสเปกทุกฟิลด์ตามลำดับ", startLine == expectedStart);

            bool tickOk = false;
            foreach (TimeV2aEvent e in ev)
            {
                if (e.Ev != "day_tick") continue;
                string line = e.ToNdjsonLine();
                string skipK = ValueOf(line, "skipK");
                float kVal = float.Parse(skipK, CultureInfo.InvariantCulture);
                tickOk = line.Contains("\"ev\":\"day_tick\"")
                      && line.Contains("\"leftWorkingDay\":0")
                      && Math.Abs(kVal - 1f) <= 0.05f;   // threshold จริง = dayTime×1 — ความหยาบ 1 เฟรม
            }
            Check("day_tick NDJSON พก skipK ≈ threshold/dayTime", tickOk);

            TimeV2aEvent noHour = ev[0];
            noHour.HasHour = false;
            string noHourLine = noHour.ToNdjsonLine();
            Check("hour ขาด -> \"hour\":null", noHourLine.Contains("\"hour\":null"));

            // float แปลง invariant เสมอ (ห้ามโดน culture ที่ใช้ comma)
            TimeV2aEvent f = ev[0];
            f.DayTime = 1.5f;
            f.PauseTime = 0.125f;
            string fLine = f.ToNdjsonLine();
            Check("float invariant (1.5 / 0.125)",
                  fLine.Contains("\"dayTime\":1.5") && fLine.Contains("\"pauseTime\":0.125"));
        }

        private static void WriterTests()
        {
            string path = Path.Combine(_tmp, "writer", "time-v2a.ndjson");
            List<TimeV2aEvent> ev = FeedAll(new TimeV2aDetector(),
                SimulateSession(2, 1f, 0.25f, 1f, 1_800_000_000_000, 60, skipping: false));

            using (TimeV2aNdjsonWriter w = new TimeV2aNdjsonWriter(path, 20))
            {
                foreach (TimeV2aEvent e in ev) w.Write(e);
            }
            string[] lines = File.ReadAllLines(path);
            Check("writer: จำนวนบรรทัด == จำนวน event (Dispose flush ครบ)", lines.Length == ev.Count);
            bool contentOk = lines.Length == ev.Count;
            for (int i = 0; i < lines.Length && contentOk; i++)
            {
                contentOk = lines[i] == ev[i].ToNdjsonLine();
            }
            Check("writer: เนื้อหาทุกบรรทัดตรง ToNdjsonLine", contentOk);

            // flushEvery=1 → หลังเขียน 1 บรรทัด ต้องมีข้อมูลบนดิสก์ทันที (explicit flush ทำงาน)
            string path1 = Path.Combine(_tmp, "flush1.ndjson");
            using (TimeV2aNdjsonWriter w = new TimeV2aNdjsonWriter(path1, 1))
            {
                w.Write(ev[0]);
                Check("writer flushEvery=1: บรรทัดแรกถูก flush ทันที", new FileInfo(path1).Length > 0);
            }

            // append mode — เปิดใหม่ไม่ตัดของเดิม (กันเกมรีสตาร์ทกลาง session)
            string[] after = File.ReadAllLines(path);
            using (TimeV2aNdjsonWriter w = new TimeV2aNdjsonWriter(path, 20))
            {
                w.Write(ev[0]);
            }
            string[] reopened = File.ReadAllLines(path);
            Check("writer: เปิดซ้ำเป็น append (ไม่ truncate)", reopened.Length == after.Length + 1);

            // Dispose ซ้ำต้องปลอดภัย
            TimeV2aNdjsonWriter w2 = new TimeV2aNdjsonWriter(Path.Combine(_tmp, "dispose2.ndjson"), 20);
            w2.Dispose();
            w2.Dispose();
            Check("writer: Dispose ซ้ำไม่ throw", true);
        }

        // ---- fixture สำหรับตรวจ analyzer จริง (คำสั่ง B4 — เรียกผ่าน Program.cs CLI) --------

        /// <summary>
        /// เขียน NDJSON จำลอง session N=5 (form2: T = D + (N+2)·P = 6.75 s @ dt 1/60) ผ่าน
        /// TimeV2aDetector จริง — ไฟล์นี้ต้องถูก `tools/time_v2a_analyze.py` อ่านได้และตัดสิน FORM2
        /// คืน path ที่เขียน
        /// </summary>
        internal static string EmitFixture(string path)
        {
            List<TimeV2aObs> obsList = SimulateSession(5, 1f, 0.25f, 1f, 1_900_000_000_000, 60, skipping: false);
            string dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            using (TimeV2aNdjsonWriter w = new TimeV2aNdjsonWriter(Path.GetFullPath(path), 20))
            {
                List<TimeV2aEvent> frame = new List<TimeV2aEvent>(4);
                TimeV2aDetector d = new TimeV2aDetector();
                foreach (TimeV2aObs o in obsList)
                {
                    frame.Clear();
                    d.Feed(in o, frame);
                    for (int i = 0; i < frame.Count; i++) w.Write(frame[i]);
                }
            }
            return Path.GetFullPath(path);
        }
    }
}
