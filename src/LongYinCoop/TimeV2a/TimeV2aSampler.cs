using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Il2Cpp;
using UnityEngine;

namespace LongYinCoop.TimeV2a
{
    /// <summary>
    /// V2 ชุด A (มติ V2A-4/V2A-5, 2026-10-29) — sampler **อ่านอย่างเดียว** ของ activity pump
    /// (`WorkingUIController`) ต่อเฟรมบน main thread:
    /// - typed interop เท่านั้น (คุณสมบัติที่ interop generate ทั้งหมด — ไม่มี raw offset/pointer)
    /// - ไม่มี Harmony instance/patch/hook ใด (ห้าม hook — ตรวจจับ event จากค่าต่อเฟรมล้วน)
    /// - ไม่มี write path เข้า state เกม (อ่าน field/static/property อย่างเดียว ไม่เรียก method ที่เปลี่ยน state)
    /// - เขียนไฟล์เดียว = NDJSON ตาม path ใน pointer file `UserData\LongYinCoop\time-v2a.out`
    ///   (fail-closed: ไม่มีไฟล์/ว่าง/นามสกุล ≠ .ndjson = ไม่เขียนเลย — errata N2) ผ่าน buffered writer
    /// - pref `TimeV2a` (default false) = kill switch — false = ไม่อ่าน/ไม่เขียนอะไรเลย
    /// - ไม่ยุ่ง Sp5T0 และไม่แตะโค้ด SP5 เดิม
    /// </summary>
    internal static class TimeV2aSampler
    {
        public const string PointerFileName = "time-v2a.out";

        public static long Events;      // event ที่เขียนลงไฟล์แล้ว
        public static long Dropped;     // event ที่ตรวจจับได้แต่ยังไม่มีไฟล์ปลายทาง (fail-closed — หาย)
        public static long Errors;
        public static string LastError;
        public static bool WriterOpen { get { return _writer != null; } }
        public static string OutputPath { get { return _writer == null ? null : _writer.Path; } }

        private static readonly object StateLock = new object();
        private static readonly List<TimeV2aEvent> Buffer = new List<TimeV2aEvent>(4);
        private static readonly TimeV2aDetector Detector = new TimeV2aDetector();
        private static TimeV2aNdjsonWriter _writer;
        private static long _lastProbeTicks;
        private static bool _pointerWarned;

        /// <summary>เรียกจาก CoopMod.Tick เมื่อ pref เปิด (main thread เท่านั้น) — กลืน exception ทุกจุด</summary>
        public static void Tick()
        {
            try
            {
                if (!CoopPrefs.TimeV2a.Value) return;   // กันเรียกผิดฝั่ง — CoopMod กรองแล้วซ้ำอีกชั้น

                if (_writer == null)
                {
                    TryOpenWriterThrottled();
                }

                TimeV2aObs obs = default;
                bool haveObs = ReadObservation(ref obs);
                if (!haveObs) return;

                Buffer.Clear();
                Detector.Feed(in obs, Buffer);
                if (Buffer.Count == 0) return;

                if (_writer == null)
                {
                    Dropped += Buffer.Count;   // ไม่มี pointer = fail-closed — ไม่เขียนไฟล์ใดนอกขอบเขต
                    return;
                }
                for (int i = 0; i < Buffer.Count; i++)
                {
                    _writer.Write(Buffer[i]);
                    Events++;
                }
            }
            catch (Exception e)
            {
                Errors++;
                LastError = e.GetType().Name + ": " + e.Message;
                CoopLog.Error(e);
            }
        }

        /// <summary>ปิด writer (pref ปิด / mod unload) — เรียกได้ทุกสถานะ</summary>
        public static void Stop()
        {
            lock (StateLock)
            {
                TimeV2aNdjsonWriter w = _writer;
                _writer = null;
                if (w != null)
                {
                    try
                    {
                        w.Dispose();
                        CoopLog.Msg("[TimeV2a] writer closed lines=" + w.Lines.ToString(CultureInfo.InvariantCulture));
                    }
                    catch (Exception e)
                    {
                        Errors++;
                        LastError = e.GetType().Name + ": " + e.Message;
                        CoopLog.Error(e);
                    }
                }
            }
        }

        /// <summary>โพล pointer file ไม่เกิน 1 Hz — เปิด writer ครั้งเดียวเมื่อ path ผ่าน gate</summary>
        private static void TryOpenWriterThrottled()
        {
            long now = Stopwatch.GetTimestamp();
            if (_lastProbeTicks != 0
                && (now - _lastProbeTicks) * 1000.0 / Stopwatch.Frequency < 1000.0)
            {
                return;
            }
            _lastProbeTicks = now;

            string pointerPath = Path.Combine(CoopPaths.EnsureUserDataDir(), PointerFileName);
            string firstLine = null;
            try
            {
                if (!File.Exists(pointerPath))
                {
                    WarnOnce("pointer file absent: " + pointerPath);
                    return;
                }
                foreach (string raw in File.ReadAllLines(pointerPath))
                {
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        firstLine = raw;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Errors++;
                LastError = e.GetType().Name + ": " + e.Message;
                WarnOnce("pointer read failed: " + e.GetType().Name);
                return;
            }

            TimeV2aGate.Verdict verdict = TimeV2aGate.Evaluate(true, firstLine);
            if (verdict != TimeV2aGate.Verdict.Ok)
            {
                WarnOnce("pointer gate = " + verdict.ToString());
                return;
            }

            string target = TimeV2aGate.PathOf(firstLine);
            _writer = new TimeV2aNdjsonWriter(target, 20);
            CoopLog.Msg("[TimeV2a] writer open -> " + target);
        }

        private static void WarnOnce(string message)
        {
            if (_pointerWarned) return;
            _pointerWarned = true;
            CoopLog.Warn("[TimeV2a] " + message + " (พิมพ์ครั้งเดียว — ยังโพลต่อทุก ~1 s)");
        }

        /// <summary>อ่าน state ปั๊มหนึ่งเฟรมด้วย typed interop — คืน false เมื่อข้ามเฟรม (ยังไม่เคยอ่านอะไร)</summary>
        private static bool ReadObservation(ref TimeV2aObs obs)
        {
            obs.TMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            obs.Frame = Time.frameCount;

            try
            {
                WorkingUIController w = WorkingUIController.Instance;
                if (w == null)
                {
                    obs.HasInstance = false;
                    return true;   // นอกงาน/กลาง transition — detector จัดการ (อาจปิด session ที่ค้าง)
                }

                obs.HasInstance = true;
                obs.Working = w.working;
                obs.LeftWorkingDay = w.leftWorkingDay;
                obs.TotalWorkingDay = w.totalWorkingDay;
                obs.NextDayTime = w.nextDayTime;
                obs.LeftPauseTime = w.leftPauseTime;
                obs.WorkFinished = w.workFinished;
                obs.NoCancel = w.noCancel;
                obs.Skipping = w.skipping;
            }
            catch (Exception e)
            {
                // อ่าน instance field ล้ม = ข้ามเฟรม (state ค้างเดิม — ไม่ปล่อย event ปลอม)
                Errors++;
                LastError = e.GetType().Name + ": " + e.Message;
                CoopLog.Error(e);
                return false;
            }

            try
            {
                obs.DayTime = WorkingUIController.dayTime;
                obs.PauseTime = WorkingUIController.pauseTime;
                obs.HasStatics = true;
            }
            catch (Exception e)
            {
                // statics อ่านไม่ได้ = work_start จะไม่ถูกปล่อย (detector) — วัดชุด A ไม่ได้ = รายงาน
                Errors++;
                LastError = e.GetType().Name + ": " + e.Message;
                CoopLog.Error(e);
            }

            try
            {
                GameController gc = GameController.Instance;
                WorldData world = gc == null ? null : gc.worldData;
                if (world != null)
                {
                    obs.Hour = world.hour;
                    obs.HasHour = true;
                }
            }
            catch (Exception e)
            {
                // hour ขาด = ฟิลด์เดียวเป็น null ใน NDJSON (analyzer ไม่ใช้) — ไม่ตัดสินอย่างอื่น
                Errors++;
                LastError = e.GetType().Name + ": " + e.Message;
                CoopLog.Error(e);
            }

            return true;
        }
    }
}
