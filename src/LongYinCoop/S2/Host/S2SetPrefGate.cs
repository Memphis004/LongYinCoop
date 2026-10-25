using System;
using System.Globalization;

namespace LongYinCoop.S2.Host
{
    /// <summary>ผลของ <see cref="S2SetPrefGate.CheckedWrite"/> — Rejected=true ⇒ Value ไม่มีความหมาย</summary>
    public struct S2SetPrefWriteOutcome
    {
        public bool Rejected;
        public object Value;

        public static S2SetPrefWriteOutcome Reject()
        {
            return new S2SetPrefWriteOutcome { Rejected = true };
        }
    }

    /// <summary>
    /// W5 (แผน S2a-4 §7): gate ของคำสั่ง `set_pref` ผ่าน IPC — ปิดช่อง K9/K10
    ///
    /// กติกา: ขณะ `S2HostService.Active == true` **ปฏิเสธ** คำสั่งที่ตั้ง write-path pref เป็น true
    /// (`allowRemoteInput`, `jitProbe`, `jitProbeW2/W4/W6/W7` — ชุดเดียวกับ `S2WriteGuard`)
    /// · การ**ปิด** (false) อนุญาตทุกสถานะ
    /// · pref นอกชุด (เช่น `capture`) ไม่ถูก gate นี้บังคับ
    ///
    /// pure: ไม่แตะ pref จริง/IPC/log — ฝั่ง IpcServer เป็นผู้นับ counter/log/ตอบ Fail
    /// </summary>
    public static class S2SetPrefGate
    {
        /// <summary>write-path pref ทั้ง 6 ตัว (เทียบแบบ lower-case — IPC ใช้หลายรูปการสะกด)</summary>
        public static readonly string[] WritePathKeys =
        {
            "allowremoteinput",
            "jitprobe",
            "jitprobew2",
            "jitprobew4",
            "jitprobew6",
            "jitprobew7",
        };

        public const string RejectErrorArgs =
            "rejected: cannot set {0}=true while S2 is active (write-path pref stays unchanged; setting false is always allowed)";

        public static bool IsWritePathKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            string normalized = key.ToLowerInvariant();
            return Array.IndexOf(WritePathKeys, normalized) >= 0;
        }

        /// <summary>true = ต้องปฏิเสธคำสั่งนี้ (active + เป็น write-path key + ตั้งเป็น true)</summary>
        public static bool ShouldReject(bool s2Active, string key, bool value)
        {
            return s2Active && value && IsWritePathKey(key);
        }

        public static string RejectError(string key)
        {
            return string.Format(CultureInfo.InvariantCulture, RejectErrorArgs, key);
        }

        /// <summary>
        /// อ่านสถานะ gate จาก callback — **helper เดียวกันทั้งสองชั้น** (PASS-2 final IPC pre-check
        /// closure): ใช้ทั้งใน pre-check ของ `IpcServer.SetPref` (IPC thread) และใน `CheckedWrite`
        /// (main thread) เพื่อไม่ให้สองชั้นมี semantics ต่างกัน
        ///
        /// callback เป็น null หรือโยน exception = สถานะ "ไม่ทราบ" ⇒ คืน **true (active)** — fail-closed
        /// · อ่านได้ = ค่าจริง (inactive ที่อ่านได้จริง = false)
        /// </summary>
        public static bool ReadGateActive(Func<bool> activeNow)
        {
            if (activeNow == null) return true;
            try
            {
                return activeNow();
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>
        /// "ตรวจแล้วเขียน" ในการเรียกเดียว (PASS-2 audit ข้อ TOCTOU) — ตัวเดียวที่ 6 branch ของ
        /// `IpcServer.SetPref` ใช้ (ผ่าน `IpcServer.OnMainGated` บน main thread)
        ///
        /// ลำดับภายในฟังก์ชันนี้: อ่าน `activeNow()` สด → `ShouldReject` → เรียก `write()` เฉพาะเมื่อผ่าน
        /// — ผู้เรียกต้องเรียกจาก thread เดียวกับที่ S2 เปลี่ยนสถานะ (main thread) เพื่อให้ไม่มีอะไร
        /// แทรกระหว่างตรวจกับเขียนได้
        ///
        /// **fail-closed เมื่อสถานะอ่านไม่ได้** (PASS-2 audit รอบสุดท้าย) — "inactive ที่อ่านได้จริง"
        /// แยกจาก "ไม่ทราบ": `activeNow` เป็น null หรือโยน exception = ไม่ทราบ ⇒ ปฏิเสธ `true`
        /// ของ write-path ทั้ง 6 key และ**ไม่เรียก `write()`** · `false` (ปิด) ผ่านเสมอ ·
        /// key นอกชุด (เช่น `capture`) อยู่นอก gate ตามเดิม
        /// </summary>
        public static S2SetPrefWriteOutcome CheckedWrite(Func<bool> activeNow, string key, bool value,
                                                          Func<object> write)
        {
            if (write == null) throw new ArgumentNullException(nameof(write));

            // สถานะไม่ทราบ (callback null / อ่านล้ม) ⇒ ReadGateActive คืน true ⇒ fail-closed
            // (ShouldReject ปฏิเสธเฉพาะ write-path key และ value=true — false ผ่านเสมอ,
            //  capture ไม่อยู่ในชุดจึงไม่เคยถูกปฏิเสธ)
            bool active = ReadGateActive(activeNow);

            if (ShouldReject(active, key, value)) return S2SetPrefWriteOutcome.Reject();
            return new S2SetPrefWriteOutcome { Value = write() };
        }

        /// <summary>
        /// นโยบาย "S2 service ไม่มีอยู่" (PASS-2 final fail-closed audit) — pure, ทดสอบใน suite ได้
        /// โดยไม่ดึง Unity/MelonLoader เข้า test project
        ///
        /// `servicePresent = false` (CoopMod._s2 == null: ยังไม่ init หรืออยู่ระหว่าง shutdown)
        /// = สถานะ "ไม่ทราบ" ⇒ คืน **true (active)** เพื่อให้ gate ปฏิเสธการเปิด write-path
        /// · service มีอยู่ = อ่าน `Active` จริง (inactive ที่อ่านได้จริง = อนุญาตตามพฤติกรรมเดิม)
        ///
        /// ฝั่ง production: callback ของ `IpcServer` ใน CoopMod ใช้สูตรเดียวกัน (`s == null || s.Active`)
        /// — แก้ callback ต้องปรับฟังก์ชันนี้ (หรือกลับด้าน) คู่กัน
        /// </summary>
        public static bool ServiceStateToActive(bool servicePresent, bool serviceActiveWhenPresent)
        {
            return servicePresent ? serviceActiveWhenPresent : true;
        }
    }
}
