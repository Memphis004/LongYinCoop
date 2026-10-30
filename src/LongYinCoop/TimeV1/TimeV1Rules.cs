using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace LongYinCoop.TimeV1
{
    /// <summary>
    /// V1 (มติ [Gate 3] 2026-10-31, errata `evidence/AUTO-20261031-035916/time-plan-v1-errata.md`) —
    /// ส่วน **pure** ของ probe log-only (ไม่อ้าง Il2Cpp/Unity/Harmony — เทสต์ออฟไลน์ได้ ตามแบบ TimeV2aRules)
    ///
    /// ขอบเขต log-only 100% (มติ [Gate 3] ข้อ (ค)):
    ///   - event ทั้งหมดอ่าน state ผ่าน typed interop อย่างเดียว (ไม่มี raw offset/pointer)
    ///   - ไม่แก้ args / ไม่เขียน __result / ไม่เขียน state เกม / ไม่เรียก method เกม
    ///   - event เป้าหมาย: stopworking.enter/exit/finalizer · middlestopwork.enter/exit/finalizer
    ///     (Harmony prefix/postfix log-only บน StopWorking 0x9CB220 และ MiddleStopWork 0x54EEA0)
    ///   - "จุดยิง finishFuc" (0x9CB2C2/0x9CB2FA — คำสั่งกลาง body ของ StopWorking แพตช์ขอบเขตไม่ได้)
    ///     = event stopworking.enter บันทึก finishFuc (ชื่อ handler ที่กำลังจะถูกยิง) + fireWorkFinished
    ///     ซึ่ง**เท่ากับ arg `finished`** ตามลำดับ static §B.3 (เขียน workFinished ที่ 0x9CB25F ก่อนยิง) —
    ///     ป้าย SPEC/[OBSERVED เชิงโค้ด] ไม่ใช่การอ่าน ณ คำสั่งยิง (errata E3)
    /// </summary>

    /// <summary>event หนึ่งชุด = หนึ่งบรรทัด NDJSON (schema ครบใน RUNBOOK-V1)</summary>
    internal sealed class TimeV1Event
    {
        public string Ev;                 // stopworking.enter/exit/finalizer · middlestopwork.enter/exit/finalizer · session.begin/end
        public long TMs;                  // unix ms (t)
        public long Frame;                // Time.frameCount (อ่านไม่ได้ = -1)
        public int ErrN;                  // จำนวน exception ที่ hook กลืนสะสม ณ เวลา emit
        public bool HasState;             // อ่าน pump fields (WorkingUIController.Instance) สำเร็จ
        public bool Working;
        public bool WorkFinished;
        public int LeftWorkingDay;
        public int TotalWorkingDay;
        public float NextDayTime;
        public float LeftPauseTime;
        public float ResourceNum;
        public bool NoCancel;
        public bool Skipping;
        public bool HasNames;             // อ่าน workName/workingFuc/finishFuc สำเร็จ
        public string WorkName;
        public string WorkingFuc;
        public string FinishFuc;          // ชื่อ finish handler (ผูกกับงาน — B.1/B.3)
        public bool WithFinished;         // เฉพาะ stopworking.enter/exit — ค่า arg `finished`
        public bool Finished;
        public bool WithFire;             // เฉพาะ stopworking.enter — fireWorkFinished = finished (SPEC, errata E3)
        public bool FireWorkFinished;
        public bool WithEx;               // เฉพาะ *finalizer — ชื่อ exception ที่ original โยน (null = ไม่มี)
        public string Ex;

        /// <summary>เขียน JSON มือ (idiom เดียวกับ TimeV2aEvent — ตัวเลข invariant ทั้งหมด ไม่พึ่ง JSON lib)</summary>
        public string ToNdjsonLine()
        {
            StringBuilder sb = new StringBuilder(384);
            sb.Append("{\"t\":").Append(TMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"frame\":").Append(Frame.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"ev\":\"").Append(Escape(Ev)).Append('"');
            sb.Append(",\"errN\":").Append(ErrN.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"hasState\":").Append(HasState ? "true" : "false");
            if (HasState)
            {
                sb.Append(",\"working\":").Append(Working ? "true" : "false");
                sb.Append(",\"workFinished\":").Append(WorkFinished ? "true" : "false");
                sb.Append(",\"leftWorkingDay\":").Append(LeftWorkingDay.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"totalWorkingDay\":").Append(TotalWorkingDay.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"nextDayTime\":").Append(F(NextDayTime));
                sb.Append(",\"leftPauseTime\":").Append(F(LeftPauseTime));
                sb.Append(",\"resourceNum\":").Append(F(ResourceNum));
                sb.Append(",\"noCancel\":").Append(NoCancel ? "true" : "false");
                sb.Append(",\"skipping\":").Append(Skipping ? "true" : "false");
            }
            sb.Append(",\"hasNames\":").Append(HasNames ? "true" : "false");
            if (HasNames)
            {
                sb.Append(",\"workName\":").Append(Quoted(WorkName));
                sb.Append(",\"workingFuc\":").Append(Quoted(WorkingFuc));
                sb.Append(",\"finishFuc\":").Append(Quoted(FinishFuc));
            }
            if (WithFinished)
            {
                sb.Append(",\"finished\":").Append(Finished ? "true" : "false");
            }
            if (WithFire)
            {
                sb.Append(",\"fireWorkFinished\":").Append(FireWorkFinished ? "true" : "false");
            }
            if (WithEx)
            {
                sb.Append(",\"ex\":").Append(Ex == null ? "null" : Quoted(Ex));
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static string Quoted(string s)
        {
            return s == null ? "null" : "\"" + Escape(s) + "\"";
        }

        private static string F(float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return "null";
            return v.ToString("R", CultureInfo.InvariantCulture);
        }

        /// <summary>escape สตริงเป็น JSON (ควบคุมชุดเดียวกับ Sp5T0Probe.Escape — เขียนซ้ำเพื่อคง pure ไม่อ้าง Il2Cpp ไฟล์อื่น)</summary>
        internal static string Escape(string s)
        {
            if (s == null) return null;
            StringBuilder sb = null;
            int start = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bool need = c == '"' || c == '\\' || c < 0x20;
                if (!need) continue;
                if (sb == null) sb = new StringBuilder(s.Length + 8);
                sb.Append(s, start, i - start);
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        break;
                }
                start = i + 1;
            }
            if (sb == null) return s;
            sb.Append(s, start, s.Length - start);
            return sb.ToString();
        }
    }

    /// <summary>
    /// เงื่อนไข "probe จะเขียนไฟล์" — pure เพื่อเทสต์ (กติกาเดียวกับ TimeV2aGate, errata E7):
    /// ต้อง pref เปิด **และ** pointer file (`UserData\LongYinCoop\time-v1.out`) บรรทัดแรกชี้ path
    /// ที่ลงท้าย .ndjson เท่านั้น · fail-closed ทุกกิ่ง — ไม่ผ่าน = ไม่สร้าง Harmony/ไม่เปิดไฟล์
    /// </summary>
    internal static class TimeV1Gate
    {
        public enum Verdict { Ok, PrefOff, NoPointerLine, NotNdjson }

        /// <param name="prefEnabled">ค่า pref `TimeV1a` (default false)</param>
        /// <param name="pointerLine">บรรทัดแรกที่ไม่ว่างของ pointer file (null = ไม่มีไฟล์/ไฟล์ว่าง)</param>
        public static Verdict Evaluate(bool prefEnabled, string pointerLine)
        {
            if (!prefEnabled) return Verdict.PrefOff;
            if (string.IsNullOrWhiteSpace(pointerLine)) return Verdict.NoPointerLine;
            string path = pointerLine.Trim().Trim('"');
            if (path.Length < ".ndjson".Length
                || !path.EndsWith(".ndjson", StringComparison.OrdinalIgnoreCase))
            {
                return Verdict.NotNdjson;
            }
            return Verdict.Ok;
        }

        /// <summary>ตัด quote/ช่องว่างของบรรทัด pointer เป็น path ที่ใช้เปิดไฟล์จริง (Ok เท่านั้น)</summary>
        public static string PathOf(string pointerLine)
        {
            return pointerLine.Trim().Trim('"');
        }
    }

    /// <summary>
    /// ตัวเขียน NDJSON แบบ buffered (idiom ตาม TimeV2aNdjsonWriter — StreamWriter AutoFlush=false,
    /// flush ทุก N บรรทัด + flush ตอนปิด) — สร้างโฟลเดอร์ปลายทางเอง, append (กันตัดของเดิมถ้าเกมรีสตาร์ท)
    /// · เปิดไฟล์แบบ **FileShare.ReadWrite** — เครื่องมือภายนอก (od -c / ตรวจไฟล์โตของ RUNBOOK)
    /// อ่านได้ขณะเกมยังเปิด (ต่างจาก StreamWriter ctor เดิมที่ล็อกกัน reader)
    /// </summary>
    internal sealed class TimeV1NdjsonWriter : IDisposable
    {
        private readonly object _lock = new object();
        private readonly StreamWriter _writer;
        private readonly int _flushEvery;
        private bool _closed;

        public TimeV1NdjsonWriter(string path, int flushEvery = 10)
        {
            Path = path;
            _flushEvery = flushEvery < 1 ? 1 : flushEvery;
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _writer = new StreamWriter(fs, new UTF8Encoding(false));
            _writer.AutoFlush = false;
        }

        public string Path { get; private set; }
        public long Lines { get; private set; }

        public void Write(string line)
        {
            lock (_lock)
            {
                if (_closed) return;
                _writer.WriteLine(line);
                Lines++;
                if (Lines % _flushEvery == 0) _writer.Flush();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_closed) return;
                _closed = true;
                try { _writer.Flush(); } catch (ObjectDisposedException) { }
                _writer.Dispose();
            }
        }
    }
}
