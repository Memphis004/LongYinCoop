using System;
using System.Globalization;

namespace LongYinCoop.S2.Protocol
{
    public enum SeqVerdict
    {
        /// <summary>data message แรกของ session (หรือ seq ตรงกับ expected พอดี)</summary>
        Ok = 0,
        /// <summary>seq กระโดดไปข้างหน้า = มี message หาย (ต้อง resync)</summary>
        Gap = 1,
        /// <summary>seq ย้อนหลัง/ซ้ำ = ทิ้ง (ไม่ใช่ gap)</summary>
        Duplicate = 2,
        /// <summary>seq = 0 → ไม่ใช่ data message (control) ไม่ต้องนับ</summary>
        NotData = 3,
    }

    /// <summary>
    /// ตัวนับ seq ของ data message (`Snapshot`) — pure, ไม่มีเวลา ไม่มี I/O
    ///
    /// กติกา (แผน S2a-3 §3.2):
    ///   - host กำหนด seq **ตอน send สำเร็จ** (ไม่ใช่ตอน sample) → drop/pacing ไม่ทำให้ seq ขาด
    ///   - message ควบคุม (Hello/AuthResult/Deny/Pong/ResyncAck/Bye) ใช้ seq = 0 และไม่ถูกนับ
    ///   - guest เริ่มที่ expectedSeq = 1 ต่อ session
    /// </summary>
    public sealed class SeqTracker
    {
        public const uint FirstSeq = 1;

        public SeqTracker(uint start = FirstSeq)
        {
            Reset(start);
        }

        /// <summary>seq ที่คาดว่าจะได้เป็นตัวถัดไป</summary>
        public uint ExpectedSeq { get; private set; }

        public long Received { get; private set; }
        public long Ok { get; private set; }
        public long Gaps { get; private set; }
        public long Duplicates { get; private set; }
        public long NotData { get; private set; }

        /// <summary>ขนาด gap ของครั้งล่าสุด (seq − expected)</summary>
        public uint LastGapSize { get; private set; }

        public uint LargestGap { get; private set; }

        /// <summary>seq ล่าสุดที่รับและนับเข้า store</summary>
        public uint LastAccepted { get; private set; }

        /// <summary>guest เริ่ม session ใหม่: ล้างตัวนับทั้งหมด</summary>
        public void Reset(uint start = FirstSeq)
        {
            ExpectedSeq = start;
            Received = Ok = Gaps = Duplicates = NotData = 0;
            LastGapSize = 0;
            LargestGap = 0;
            LastAccepted = 0;
        }

        public SeqVerdict Observe(uint seq)
        {
            if (seq == 0)
            {
                NotData++;
                return SeqVerdict.NotData;
            }

            Received++;
            if (seq == ExpectedSeq)
            {
                Ok++;
                LastAccepted = seq;
                Advance(seq);
                return SeqVerdict.Ok;
            }

            if (seq > ExpectedSeq)
            {
                Gaps++;
                LastGapSize = seq - ExpectedSeq;
                if (LastGapSize > LargestGap) LargestGap = LastGapSize;
                // gap = รับตัวใหม่นี้เข้า store แล้วรอ resync เติมช่วงที่หาย
                LastAccepted = seq;
                Advance(seq);
                return SeqVerdict.Gap;
            }

            Duplicates++;
            return SeqVerdict.Duplicate;
        }

        /// <summary>หลัง resync: host บอกว่า seq ถัดไปคือค่าไหน</summary>
        public void ResyncTo(uint nextSeq)
        {
            ExpectedSeq = nextSeq == 0 ? FirstSeq : nextSeq;
        }

        private void Advance(uint seq)
        {
            ExpectedSeq = seq == uint.MaxValue ? FirstSeq : seq + 1;
        }

        public string ToText()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "expected={0} lastAccepted={1} received={2} ok={3} gaps={4} dup={5} largestGap={6}",
                ExpectedSeq, LastAccepted, Received, Ok, Gaps, Duplicates, LargestGap);
        }
    }

    /// <summary>
    /// หน้าต่าง gap/resync ฝั่ง guest (แผน §3.3) — pure state machine ที่รับเวลาเข้ามา
    ///   seq ขาด / stall / hash mismatch → เปิดหน้าต่าง → ขอ RESYNC (ซ้ำได้ทุก <see cref="RetryMs"/>)
    ///   ได้ snapshot ที่ติดธง `resync=true` **และ** hash ตรง → ปิดหน้าต่าง (บันทึก `gapOpenMs`)
    ///   ขอครบ <see cref="MaxAttempts"/> แล้วยังไม่ได้ → giveUp (ผู้เรียกจะปิด connection แล้ว reconnect)
    ///
    /// ไม่มี I/O, ไม่มี thread — เทสต์ด้วยเวลาปลอมได้ (T0)
    /// </summary>
    public sealed class ResyncState
    {
        public const int MaxAttempts = 3;
        public const int RetryMs = 500;

        public bool WindowOpen { get; private set; }
        public string Cause { get; private set; }
        public uint LastSeq { get; private set; }
        public long OpenedAtMs { get; private set; }
        public long LastRequestAtMs { get; private set; }
        public int Attempts { get; private set; }

        public long WindowsClosed { get; private set; }
        public long GiveUps { get; private set; }
        public long MaxGapOpenMs { get; private set; }
        public long SumGapOpenMs { get; private set; }

        public double MeanGapOpenMs
        {
            get { return WindowsClosed == 0 ? 0 : (double)SumGapOpenMs / WindowsClosed; }
        }

        /// <summary>เปิดหน้าต่าง (ถ้าเปิดอยู่แล้วไม่รีเซ็ตเวลา — หน้าต่างเดิมยาวต่อ)</summary>
        public void OpenGap(long nowMs, uint lastSeq, string cause)
        {
            LastSeq = lastSeq;
            if (WindowOpen) return;
            WindowOpen = true;
            Cause = cause;
            OpenedAtMs = nowMs;
            LastRequestAtMs = long.MinValue / 2;
            Attempts = 0;
        }

        /// <summary>ถึงเวลายิง RESYNC หรือยัง (ครั้งแรกยิงทันที, หลังจากนั้นทุก RetryMs)</summary>
        public bool ShouldRequest(long nowMs)
        {
            if (!WindowOpen) return false;
            if (Attempts >= MaxAttempts) return false;
            if (Attempts == 0) return true;
            return nowMs - LastRequestAtMs >= RetryMs;
        }

        public void OnRequestSent(long nowMs)
        {
            Attempts++;
            LastRequestAtMs = nowMs;
        }

        /// <summary>ครบจำนวนครั้งแล้วยังไม่ปิดได้ → ต้อง reconnect</summary>
        public bool ShouldGiveUp(long nowMs)
        {
            return WindowOpen && Attempts >= MaxAttempts && nowMs - LastRequestAtMs >= RetryMs;
        }

        /// <summary>
        /// รับ snapshot: ปิดหน้าต่างเมื่อ `resync=true` **และ** Div-A ตรงเท่านั้น
        /// คืน true เมื่อปิดหน้าต่างในครั้งนี้
        /// </summary>
        public bool OnSnapshot(bool resyncFlag, bool hashMatch, long nowMs)
        {
            if (!WindowOpen) return false;
            if (!resyncFlag || !hashMatch) return false;

            WindowOpen = false;
            long duration = nowMs - OpenedAtMs;
            if (duration < 0) duration = 0;
            WindowsClosed++;
            SumGapOpenMs += duration;
            if (duration > MaxGapOpenMs) MaxGapOpenMs = duration;
            OpenedAtMs = 0;
            Attempts = 0;
            return true;
        }

        /// <summary>ปิดหน้าต่างแบบไม่นับ (เช่น connection ตายแล้ว reconnect)</summary>
        public void AbandonGap()
        {
            if (!WindowOpen) return;
            WindowOpen = false;
            OpenedAtMs = 0;
            Attempts = 0;
            GiveUps++;
        }

        public string ToText()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "gapOpen={0} cause={1} attempts={2} closed={3} giveUp={4} meanGapOpenMs={5:F1} maxGapOpenMs={6}",
                WindowOpen ? "true" : "false", Cause ?? "-", Attempts, WindowsClosed, GiveUps,
                MeanGapOpenMs, MaxGapOpenMs);
        }
    }
}
