using System;
using System.Globalization;
using System.IO;
using LongYinCoop.S2.Protocol;

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
    }
}
