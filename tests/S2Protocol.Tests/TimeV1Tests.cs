using System;
using System.IO;
using LongYinCoop.TimeV1;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// V1 (มติ [Gate 3] 2026-10-31) — เทสต์ pure ของ TimeV1Rules (gate/event/writer)
    /// · ส่วน wiring (TimeV1Probe) อยู่ที่ TimeV1PurityHarness (artifact จริง — อ้าง Il2Cpp จึงไม่ลิงก์ซอร์ส)
    /// </summary>
    internal static class TimeV1Tests
    {
        private static int _pass;
        private static int _fail;

        private static void Check(string name, bool ok)
        {
            Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] (timev1) {name}");
            if (ok) _pass++; else _fail++;
        }

        public static int Run()
        {
            _pass = 0;
            _fail = 0;
            Console.WriteLine();
            Console.WriteLine("== TimeV1 pure rules (gate/event/writer) ==");
            GateTests();
            EventNdjsonTests();
            EscapeTests();
            WriterTests();
            Console.WriteLine($"TimeV1 pure rules: {_pass} PASS / {_fail} FAIL");
            return _fail;
        }

        // ---- TimeV1Gate: fail-closed ทุกกิ่ง (ข้อ (ก) ของ [Gate 3] — pref off = ไม่ทำอะไร) ----
        private static void GateTests()
        {
            Check("gate: pref off → PrefOff (ไม่สน pointer ใด)",
                  TimeV1Gate.Evaluate(false, @"C:\x\run.ndjson") == TimeV1Gate.Verdict.PrefOff);
            Check("gate: pref off + pointer null → PrefOff",
                  TimeV1Gate.Evaluate(false, null) == TimeV1Gate.Verdict.PrefOff);
            Check("gate: pref on + ไม่มี pointer → NoPointerLine",
                  TimeV1Gate.Evaluate(true, null) == TimeV1Gate.Verdict.NoPointerLine);
            Check("gate: pref on + pointer ว่าง/ช่องว่าง → NoPointerLine",
                  TimeV1Gate.Evaluate(true, "   ") == TimeV1Gate.Verdict.NoPointerLine);
            Check("gate: นามสกุลไม่ใช่ .ndjson → NotNdjson",
                  TimeV1Gate.Evaluate(true, @"C:\x\run.txt") == TimeV1Gate.Verdict.NotNdjson);
            Check("gate: ไม่มีนามสกุล → NotNdjson",
                  TimeV1Gate.Evaluate(true, @"C:\x\run") == TimeV1Gate.Verdict.NotNdjson);
            Check("gate: path .ndjson ตรง → Ok",
                  TimeV1Gate.Evaluate(true, @"C:\x\evidence\L\TC1-r1.ndjson") == TimeV1Gate.Verdict.Ok);
            Check("gate: .NDJSON ตัวใหญ่ → Ok (case-insensitive)",
                  TimeV1Gate.Evaluate(true, @"C:\x\r.NDJSON") == TimeV1Gate.Verdict.Ok);
            Check("gate: ชื่อไฟล์แค่ .ndjson ล้วน → Ok",
                  TimeV1Gate.Evaluate(true, ".ndjson") == TimeV1Gate.Verdict.Ok);
            Check("gate: สตริงสั้นกว่านามสกุล → NotNdjson",
                  TimeV1Gate.Evaluate(true, ".ndjs") == TimeV1Gate.Verdict.NotNdjson);
            Check("gate.PathOf: ตัด quote + ช่องว่าง",
                  TimeV1Gate.PathOf("  \"C:\\x r\\a.ndjson\"  ") == @"C:\x r\a.ndjson");
        }

        // ---- TimeV1Event: รูปแบบ NDJSON (schema ของ RUNBOOK-V1) ----
        private static void EventNdjsonTests()
        {
            // stopworking.enter แบบครบ — field เรียงตายตัว (ตรวจบรรทัดเต็มแบบ deterministic)
            TimeV1Event e = new TimeV1Event
            {
                Ev = "stopworking.enter",
                TMs = 1700000000123L,
                Frame = 44677,
                ErrN = 2,
                HasState = true,
                Working = true,
                WorkFinished = false,
                LeftWorkingDay = 3,
                TotalWorkingDay = 2,
                NextDayTime = 1.5f,
                LeftPauseTime = 0.25f,
                ResourceNum = 4733.75f,
                NoCancel = false,
                Skipping = false,
                HasNames = true,
                WorkName = "采药",
                WorkingFuc = "WorkInProductionBuilding",
                FinishFuc = "FinishWorkInProductionBuilding",
                WithFinished = true,
                Finished = false,
                WithFire = true,
                FireWorkFinished = false,
            };
            string line = e.ToNdjsonLine();
            string expected = "{\"t\":1700000000123,\"frame\":44677,\"ev\":\"stopworking.enter\",\"errN\":2,"
                + "\"hasState\":true,\"working\":true,\"workFinished\":false,\"leftWorkingDay\":3,"
                + "\"totalWorkingDay\":2,\"nextDayTime\":1.5,\"leftPauseTime\":0.25,\"resourceNum\":4733.75,"
                + "\"noCancel\":false,\"skipping\":false,\"hasNames\":true,"
                + "\"workName\":\"采药\",\"workingFuc\":\"WorkInProductionBuilding\","
                + "\"finishFuc\":\"FinishWorkInProductionBuilding\","
                + "\"finished\":false,\"fireWorkFinished\":false}";
            Check("event: stopworking.enter บรรทัดเต็มตรง schema", line == expected);

            // ไม่มี state/names = ไม่ปล่อยค่าปลอม (hasState/hasNames false ล้วน)
            TimeV1Event empty = new TimeV1Event { Ev = "middlestopwork.enter", TMs = 1, Frame = -1, ErrN = 0 };
            string el = empty.ToNdjsonLine();
            Check("event: ไม่มี state → hasState:false ไม่มี field state",
                  el.Contains("\"hasState\":false") && !el.Contains("\"working\":")
                  && !el.Contains("\"leftWorkingDay\":"));
            Check("event: ไม่มี names → hasNames:false ไม่มี finishFuc",
                  el.Contains("\"hasNames\":false") && !el.Contains("\"finishFuc\":"));
            Check("event: ไม่มี arg/fire/ex → ไม่มี finished/fireWorkFinished/ex",
                  !el.Contains("\"finished\":") && !el.Contains("fireWorkFinished")
                  && !el.Contains("\"ex\":"));

            // float NaN/Infinity → null (ห้ามค่าปลอมลงไฟล์)
            TimeV1Event nan = new TimeV1Event
            {
                Ev = "stopworking.exit",
                TMs = 5,
                Frame = 6,
                HasState = true,
                NextDayTime = float.NaN,
                LeftPauseTime = float.PositiveInfinity,
                ResourceNum = float.NegativeInfinity,
            };
            string nl = nan.ToNdjsonLine();
            Check("event: NaN/Inf → null", nl.Contains("\"nextDayTime\":null")
                  && nl.Contains("\"leftPauseTime\":null") && nl.Contains("\"resourceNum\":null"));

            // finalizer: ex null/ชื่อ exception
            TimeV1Event fin = new TimeV1Event { Ev = "stopworking.finalizer", TMs = 7, Frame = 8, WithEx = true };
            Check("event: finalizer ไม่มี exception → ex:null", fin.ToNdjsonLine().Contains("\"ex\":null"));
            TimeV1Event fin2 = new TimeV1Event
            {
                Ev = "middlestopwork.finalizer",
                TMs = 7,
                Frame = 8,
                WithEx = true,
                Ex = "TypeInitializationException",
            };
            Check("event: finalizer มี exception → ex ครอบ quote",
                  fin2.ToNdjsonLine().Contains("\"ex\":\"TypeInitializationException\""));

            // session.begin/end — field น้อยที่สุด
            TimeV1Event sb = new TimeV1Event { Ev = "session.begin", TMs = 9, Frame = 10 };
            Check("event: session.begin = t/frame/ev/errN/hasState/hasNames เท่านั้น",
                  sb.ToNdjsonLine()
                    == "{\"t\":9,\"frame\":10,\"ev\":\"session.begin\",\"errN\":0,\"hasState\":false,\"hasNames\":false}");
        }

        private static void EscapeTests()
        {
            Check("escape: null → null", TimeV1Event.Escape(null) == null);
            Check("escape: ธรรมดาคงเดิม", TimeV1Event.Escape("abc") == "abc");
            Check("escape: quote/backslash", TimeV1Event.Escape("a\"b\\c") == "a\\\"b\\\\c");
            Check("escape: newline/tab/cr", TimeV1Event.Escape("a\nb\tc\rd") == "a\\nb\\tc\\rd");
            Check("escape: control char → \\u", TimeV1Event.Escape("a\u0001b") == "a\\u0001b");
        }

        // ---- TimeV1NdjsonWriter: buffered + flush ทุก N + append + สร้างโฟลเดอร์เอง ----

        /// <summary>อ่านไฟล์ที่ writer ยังเปิดอยู่ — FileShare.ReadWrite (วิธีเดียวกับ od -c/ตรวจไฟล์โตของ RUNBOOK)</summary>
        private static string[] ReadShared(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader sr = new StreamReader(fs))
            {
                string body = sr.ReadToEnd();
                if (body.Length == 0) return Array.Empty<string>();
                return body.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            }
        }

        private static void WriterTests()
        {
            string dir = Path.Combine(Path.GetTempPath(), "timev1-tests-" + Guid.NewGuid().ToString("N"));
            try
            {
                string path = Path.Combine(dir, "sub", "run.ndjson");
                using (TimeV1NdjsonWriter w = new TimeV1NdjsonWriter(path, 10))
                {
                    for (int i = 0; i < 9; i++) w.Write("{\"i\":" + i + "}");
                    // flush ทุก 10 — ยังไม่ครบ 10 บรรทัด = ไฟล์ยังว่าง (buffered)
                    Check("writer: <flushEvery ยังไม่ลงดิสก์ (buffered)",
                          !File.Exists(path) || new FileInfo(path).Length == 0);
                    w.Write("{\"i\":9}");
                    Check("writer: ครบ flushEvery → ลงดิสก์ 10 บรรทัด",
                          ReadShared(path).Length == 10);
                    for (int i = 10; i < 25; i++) w.Write("{\"i\":" + i + "}");
                    Check("writer: Lines นับครบ", w.Lines == 25);
                    Check("writer: Path คืน path ที่เปิด", w.Path == path);
                }
                // Dispose → flush ตกฟากครบ + append รอบใหม่ไม่ทับของเดิม
                string[] all = File.ReadAllLines(path);
                Check("writer: Dispose flush ครบ 25 บรรทัด", all.Length == 25
                      && all[24] == "{\"i\":24}");
                using (TimeV1NdjsonWriter w2 = new TimeV1NdjsonWriter(path, 10))
                {
                    w2.Write("{\"i\":25}");
                }
                Check("writer: เปิดซ้ำ = append (ไม่ตัดของเดิม)", File.ReadAllLines(path).Length == 26);

                // เขียนหลังปิด = ไม่ทำอะไร (กัน ObjectDisposed)
                TimeV1NdjsonWriter closed = new TimeV1NdjsonWriter(path, 1);
                closed.Dispose();
                closed.Dispose();   // Dispose ซ้ำปลอดภัย
                closed.Write("{\"x\":1}");
                Check("writer: Dispose ซ้ำ/เขียนหลังปิด ไม่ throw ไม่เพิ่มบรรทัด",
                      File.ReadAllLines(path).Length == 26);
            }
            finally
            {
                try { Directory.Delete(dir, recursive: true); } catch { }
            }
        }
    }
}
