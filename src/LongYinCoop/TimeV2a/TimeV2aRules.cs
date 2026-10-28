using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LongYinCoop.TimeV2a
{
    /// <summary>
    /// V2 ชุด A (มติ V2A-4, 2026-10-29) — ตรรกะตรวจจับ event ของ activity pump แบบ pure
    /// (ไม่อ้าง Il2Cpp/Unity — เทสต์ออฟไลน์ได้ ตามแบบ S2 protocol tests) · อ่านอย่างเดียวเด็ดขาด
    ///
    /// event และเงื่อนไข (อ้าง time-static-notes §2.4–§2.8 [OBSERVED]):
    ///   work_start  : ขณะเฟรมก่อนไม่ได้ working แล้วเฟรมนี้ working==1 (StartWorking เขียน
    ///                 leftWorkingDay=N, nextDayTime=0, leftPauseTime=pauseTime ครบในเฟรมเดียว)
    ///   day_tick    : ยัง working และ leftWorkingDay ลดลงจากเฟรมก่อน (สเปกแพ็กเกจ 2 — ห้าม hook)
    ///   pause_end   : ยัง working และ leftPauseTime ข้ามจาก >0 ไป <=0 (กิ่ง pause ของ Update นับถอย)
    ///   work_end    : working 1→0 โดย workFinished==true (กิ่งจบงานของ Update เขียน 1)
    ///   work_abort  : working 1→0 โดย workFinished==false (StopWorking(false) เขียน arg ก่อนเคลียร์ working)
    /// </summary>
    internal struct TimeV2aObs
    {
        public bool HasInstance;      // WorkingUIController.Instance != null
        public bool Working;
        public int LeftWorkingDay;
        public int TotalWorkingDay;
        public float NextDayTime;
        public float LeftPauseTime;
        public bool WorkFinished;
        public bool NoCancel;
        public bool Skipping;
        public bool HasStatics;       // statics dayTime/pauseTime อ่านสำเร็จในเฟรมนี้
        public float DayTime;
        public float PauseTime;
        public bool HasHour;          // WorldData.hour อ่านสำเร็จ (hour เป็น float ตาม dump.cs 0xB4)
        public float Hour;
        public long TMs;              // unix ms (สเปก: t)
        public long Frame;            // Time.frameCount
    }

    /// <summary>event หนึ่งชุด = หนึ่งบรรทัด NDJSON (ฟิลด์ตามสเปกแพ็กเกจ 2 + เสริม noCancel/skipK — errata N3)</summary>
    internal sealed class TimeV2aEvent
    {
        public string Ev;
        public long TMs;
        public long Frame;
        public int LeftWorkingDay;
        public int TotalWorkingDay;
        public float NextDayTime;
        public float LeftPauseTime;
        public bool WorkFinished;
        public bool Skipping;
        public float DayTime;
        public float PauseTime;
        public bool HasHour;
        public float Hour;
        public bool WithNoCancel;     // เฉพาะ work_start
        public bool NoCancel;
        public bool WithSkipK;        // work_start (skipping==false → 1.0) / day_tick (ค่าที่วัดได้)
        public float SkipK;

        /// <summary>เขียน JSON มือ (idiom เดียวกับ S2Ndjson — ไม่พึ่ง JSON lib) — ตัวเลข invariant ทั้งหมด</summary>
        public string ToNdjsonLine()
        {
            StringBuilder sb = new StringBuilder(256);
            sb.Append("{\"t\":").Append(TMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"frame\":").Append(Frame.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"ev\":\"").Append(Ev).Append('"');
            sb.Append(",\"leftWorkingDay\":").Append(LeftWorkingDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"totalWorkingDay\":").Append(TotalWorkingDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"nextDayTime\":").Append(F(NextDayTime));
            sb.Append(",\"leftPauseTime\":").Append(F(LeftPauseTime));
            sb.Append(",\"workFinished\":").Append(WorkFinished ? "true" : "false");
            sb.Append(",\"skipping\":").Append(Skipping ? "true" : "false");
            sb.Append(",\"dayTime\":").Append(F(DayTime));
            sb.Append(",\"pauseTime\":").Append(F(PauseTime));
            sb.Append(",\"hour\":").Append(HasHour ? F(Hour) : "null");
            if (WithNoCancel)
            {
                sb.Append(",\"noCancel\":").Append(NoCancel ? "true" : "false");
            }
            if (WithSkipK)
            {
                sb.Append(",\"skipK\":").Append(F(SkipK));
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static string F(float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return "null";
            return v.ToString("R", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// เครื่องตรวจจับ event จาก observation ต่อเนื่อง — state เดียว (เฟรมก่อน + ค่า max ของ nextDayTime)
    /// · กติกา skipK ของ day_tick = max(nextDayTime ตั้งแต่ tick ก่อน) ÷ dayTime (threshold จริงที่ปั๊มใช้
    /// เพราะปั๊มรีเซ็ต nextDayTime=0 พร้อม tick — time-static-notes §2.4) · ค่า NaN/Infinity ไม่เคยกลายเป็น skipK
    /// </summary>
    internal sealed class TimeV2aDetector
    {
        private bool _hasPrev;
        private bool _prevWorking;
        private int _prevLeftWorkingDay;
        private float _prevLeftPauseTime;
        private float _maxNextDayTime;
        private bool _sessionOpen;   // work_start ถูกปล่อยแล้ว (statics ครบ) — event อื่นย่อมอยู่ใน session นี้เท่านั้น

        public void Reset()
        {
            _hasPrev = false;
            _prevWorking = false;
            _prevLeftWorkingDay = 0;
            _prevLeftPauseTime = 0f;
            _maxNextDayTime = 0f;
            _sessionOpen = false;
        }

        /// <summary>ให้ observation หนึ่งเฟรม — ผนึก event 0..2 ชุดต่อเฟรมลง output (เรียงตามลำดับเกิด)</summary>
        public void Feed(in TimeV2aObs obs, List<TimeV2aEvent> output)
        {
            if (output == null) throw new ArgumentNullException("output");

            if (!obs.HasInstance || !obs.Working)
            {
                if (_hasPrev && _prevWorking && _sessionOpen)
                {
                    Emit(output, obs, obs.WorkFinished ? "work_end" : "work_abort");
                }
                Reset();
                _hasPrev = true;
                _prevWorking = false;
                return;
            }

            if (!_hasPrev || !_prevWorking)
            {
                // สเปก: work_start ต้องพก dayTime/pauseTime (analyzer อ่านจาก event นี้) — statics
                // อ่านไม่ได้ = ไม่เปิด session (ห้ามอ้างข้อมูลที่ขาด) — นับเป็น UNKNOWN ฝั่งผู้เรียก
                if (obs.HasStatics)
                {
                    TimeV2aEvent ev = Emit(output, obs, "work_start");
                    _sessionOpen = true;
                    ev.WithNoCancel = true;
                    ev.NoCancel = obs.NoCancel;
                    if (!obs.Skipping)
                    {
                        // threshold multiplier ตอนไม่ skip = 1.0 ตายตัว (time-static-notes §2.4)
                        ev.WithSkipK = true;
                        ev.SkipK = 1f;
                    }
                    // skipping==true ตอนเริ่ม = stale latch (StartWorking ไม่ reset — plan B.2) —
                    // ยังไม่รู้ k จริง (TestMode) จึงไม่ใส่ skipK ให้ผู้วิเคราะห์เดา
                }
                _maxNextDayTime = 0f;
            }
            else if (_sessionOpen)
            {
                if (obs.LeftWorkingDay < _prevLeftWorkingDay)
                {
                    TimeV2aEvent ev = Emit(output, obs, "day_tick");
                    if (obs.HasStatics && obs.DayTime > 0f && _maxNextDayTime > 0f
                        && !float.IsNaN(_maxNextDayTime) && !float.IsInfinity(_maxNextDayTime))
                    {
                        ev.WithSkipK = true;
                        ev.SkipK = (float)Math.Round(_maxNextDayTime / obs.DayTime, 4);
                    }
                    _maxNextDayTime = 0f;
                }
                if (_prevLeftPauseTime > 0f && obs.LeftPauseTime <= 0f)
                {
                    Emit(output, obs, "pause_end");
                }
            }

            if (!float.IsNaN(obs.NextDayTime) && !float.IsInfinity(obs.NextDayTime)
                && obs.NextDayTime > _maxNextDayTime)
            {
                _maxNextDayTime = obs.NextDayTime;
            }

            _hasPrev = true;
            _prevWorking = true;
            _prevLeftWorkingDay = obs.LeftWorkingDay;
            _prevLeftPauseTime = obs.LeftPauseTime;
        }

        private static TimeV2aEvent Emit(List<TimeV2aEvent> output, in TimeV2aObs obs, string ev)
        {
            TimeV2aEvent e = new TimeV2aEvent
            {
                Ev = ev,
                TMs = obs.TMs,
                Frame = obs.Frame,
                LeftWorkingDay = obs.LeftWorkingDay,
                TotalWorkingDay = obs.TotalWorkingDay,
                NextDayTime = obs.NextDayTime,
                LeftPauseTime = obs.LeftPauseTime,
                WorkFinished = obs.WorkFinished,
                Skipping = obs.Skipping,
                DayTime = obs.DayTime,
                PauseTime = obs.PauseTime,
                HasHour = obs.HasHour,
                Hour = obs.Hour,
            };
            output.Add(e);
            return e;
        }
    }

    /// <summary>
    /// เงื่อนไข "sampler จะเขียนไฟล์" — pure เพื่อเทสต์: ต้อง pref เปิด **และ** pointer file
    /// (`UserData\LongYinCoop\time-v2a.out`) บรรทัดแรกชี้ path ที่ลงท้าย .ndjson เท่านั้น
    /// · fail-closed ทุกกิ่ง (errata N2)
    /// </summary>
    internal static class TimeV2aGate
    {
        public enum Verdict { Ok, PrefOff, NoPointerLine, NotNdjson }

        /// <param name="prefEnabled">ค่า pref `TimeV2a`</param>
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
    /// ตัวเขียน NDJSON แบบ buffered (idiom ตาม `S2NdjsonWriter` — StreamWriter AutoFlush=false,
    /// flush ทุก N บรรทัด + flush ปิด) — สร้างโฟลเดอร์ปลายทางเอง, append (กันตัดของเดิมถ้าเกมรีสตาร์ท)
    /// </summary>
    internal sealed class TimeV2aNdjsonWriter : IDisposable
    {
        private readonly object _lock = new object();
        private readonly StreamWriter _writer;
        private readonly int _flushEvery;
        private bool _closed;

        public TimeV2aNdjsonWriter(string path, int flushEvery = 20)
        {
            Path = path;
            _flushEvery = flushEvery < 1 ? 1 : flushEvery;
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            _writer = new StreamWriter(path, append: true, new UTF8Encoding(false));
            _writer.AutoFlush = false;
        }

        public string Path { get; private set; }
        public long Lines { get; private set; }

        public void Write(TimeV2aEvent ev)
        {
            string line = ev.ToNdjsonLine();
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
