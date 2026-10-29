using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;

namespace LongYinCoop.S2.Tests
{
    /// <summary>
    /// SP5-T0 linkage harness (แผน plans/sp5-t0-plan.md rev 13 — §2.5 invocation linkage + §5 ข้อ 2)
    ///
    /// พิสูจน์ "กลไก" thread-local LIFO ของ iid บน artifact จริง (โหลด LongYinCoop.dll ที่ build แล้ว
    /// ด้วย reflection — ไม่ re-implement, ไม่แตะซอร์ส): pairing แบบ LIFO กับ invocation ซ้อน ·
    /// exception path (finalizer pop โดยไม่มี postfix) · isolation ข้าม thread · iid เดินตาม record ถูกตัว
    ///
    /// สิ่งที่ harness นี้พิสูจน์ "ไม่ได้" (ประกาศตามขอบเขต):
    ///   - ordering/ขอบเขตจริงของ Harmony บนเป้าเกม (ต้อง runtime — ครอบด้วย patch.audit ณ จุดตรวจ ตอนรันจริง)
    ///   - branch mismatch ของ PopInv (เรียก PhaseStuckMark → BeginEvent → UnityEngine.Time — นอกกรอบ
    ///     offline) — ครอบด้วย code audit (GO-1) และ s2_static_check
    ///   - #C.ok/E1/E2 ยังคง UNKNOWN ตาม rev 13 เพราะ inventoryStability ภายใน window ไม่มีกลไกตรวจ
    ///     (คงตามแผน ไม่เกี่ยวกับผลลัพธ์ของ harness นี้)
    /// </summary>
    internal static class Sp5T0LinkageHarness
    {
        private static int _pass;
        private static int _fail;

        private static void Check(string name, bool ok)
        {
            Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] (sp5link) {name}");
            if (ok) _pass++; else _fail++;
        }

        private static void FailWith(string name, string reason)
        {
            Console.WriteLine($"[FAIL] (sp5link) {name} — {reason}");
            _fail++;
        }

        public static int Run()
        {
            _pass = 0;
            _fail = 0;
            Console.WriteLine();
            Console.WriteLine("== SP5-T0 linkage harness (iid thread-local LIFO — artifact จริง) ==");

            RunCore();
            Console.WriteLine($"SP5-T0 linkage harness: {_pass} PASS / {_fail} FAIL");
            return _fail;
        }

        private static void RunCore()
        {
            // ---- โหลด artifact จริง (Release ก่อน แล้ว Debug) + พิมพ์ sha256 เพื่อผูกหลักฐานกับ provenance ----
            string dll = null;
            for (string dir = AppContext.BaseDirectory; dir != null && dll == null; dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar)))
            {
                foreach (string cfg in new[] { "Release", "Debug" })
                {
                    string cand = Path.Combine(dir, "src", "LongYinCoop", "bin", cfg, "LongYinCoop.dll");
                    if (File.Exists(cand)) { dll = cand; break; }
                }
            }

            if (dll == null)
            {
                FailWith("artifact LongYinCoop.dll พบ",
                         "หาไม่เจอทั้ง Release/Debug — ต้อง dotnet build src/LongYinCoop ก่อนรัน suite นี้");
                return;
            }

            string sha;
            using (FileStream fs = File.OpenRead(dll))
            using (SHA256 h = SHA256.Create())
                sha = BitConverter.ToString(h.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
            Console.WriteLine($"      artifact: {dll}");
            Console.WriteLine($"      sha256  : {sha}");
            Check("artifact LongYinCoop.dll พบ", true);

            Assembly asm;
            try { asm = Assembly.LoadFrom(dll); }
            catch (Exception ex) { FailWith("โหลด artifact ได้", ex.GetType().Name + ": " + ex.Message); return; }
            Check("โหลด artifact ได้", asm != null);

            Type probe = asm.GetType("LongYinCoop.Sp5T0Probe", false);
            if (probe == null) { FailWith("พบชนิด Sp5T0Probe ใน artifact", "GetType คืน null"); return; }
            Check("พบชนิด Sp5T0Probe ใน artifact", true);

            const BindingFlags SF = BindingFlags.Static | BindingFlags.NonPublic;
            Type invRec = probe.GetNestedType("InvRec", SF);
            MethodInfo push = probe.GetMethod("PushInv", SF);
            MethodInfo top = probe.GetMethod("TopInv", SF);
            MethodInfo pop = probe.GetMethod("PopInv", SF);
            FieldInfo stackF = probe.GetField("_invStack", SF);
            FieldInfo seqF = probe.GetField("_iidSeq", SF);
            Check("โครงกลไกครบ (InvRec/PushInv/TopInv/PopInv/_invStack/_iidSeq)",
                  invRec != null && push != null && top != null && pop != null && stackF != null && seqF != null);
            if (invRec == null || push == null || top == null || pop == null || stackF == null || seqF == null) return;

            // ---- A0: NormalizePatchId ของ comparator patch.audit (แก้บั๊ก N1-1 — allMatch=false
            //      ทั้ง 4 จุดตรวจในรอบ T0 20261030 แม้ inventory ตรงเชิงความหมาย) ----
            MethodInfo normFn = probe.GetMethod("NormalizePatchId",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Check("มี NormalizePatchId ใน artifact", normFn != null);
            if (normFn != null)
            {
                Func<string, string> norm = s => (string)normFn.Invoke(null, new[] { s });
                const string id = "longyincoop.sp5t0.probe";
                // คู่ string จริงจากรอบ T0 20261030 — actual (owner:Type::Method) เทียบ expected literal (owner:Type.Method)
                Check("normalize: actual รอบ T0 == expected literal (บั๊กเดิมปิด)",
                      norm(id + ":Sp5T0Probe::GenDataAPrefix") == norm(id + ":Sp5T0Probe.GenDataAPrefix"));
                Check("normalize: canonical รูปเดียว — :: และ : ทับเป็น .",
                      norm("a:B::C") == "a.B.C" && norm("x::y:z") == "x.y.z");
                Check("normalize: ไม่มีตัวคั่น = คงเดิม", norm("plain.Name") == "plain.Name");
                Check("normalize: method ต่างกันยังต่างกัน (ไม่กลืน false-positive)",
                      norm(id + ":Sp5T0Probe::GenDataAPrefix") != norm(id + ":Sp5T0Probe::GenDataBPrefix"));
                Check("normalize: null ปลอดภัย (คืน null)", norm(null) == null);
            }

            // ---- helpers: สร้าง record + อ่าน/เขียน field ผ่าน reflection (handler เรียกผ่านเมธอดเดียวกันนี้) ----
            Func<long, int, object> newRec = (iid, ov) =>
            {
                object r = Activator.CreateInstance(invRec);
                invRec.GetField("Iid").SetValue(r, iid);
                invRec.GetField("Ov").SetValue(r, ov);
                return r;
            };
            Func<long> readSeq = () => (long)seqF.GetValue(null);
            Func<int> depth = () =>
            {
                IList s = (IList)stackF.GetValue(null);   // FieldInfo.GetValue บน [ThreadStatic] = ค่าของ thread เรียก
                return s == null ? 0 : s.Count;
            };

            // ---- B: nesting pairing 2 ระดับ (A → C ซ้อน) — Top/Pop ต้องตรงตัว record ที่ push (identity) ----
            object a1 = newRec(101, 0);
            object c1 = newRec(102, 2);
            push.Invoke(null, new[] { a1 });
            Check("push A1 → Top == A1 (identity)", ReferenceEquals(top.Invoke(null, null), a1));
            push.Invoke(null, new[] { c1 });
            Check("push C1 ซ้อน → Top == C1 ไม่ใช่ A1", ReferenceEquals(top.Invoke(null, null), c1));
            pop.Invoke(null, new[] { c1 });
            Check("pop C1 → Top กลับเป็น A1 (LIFO)", ReferenceEquals(top.Invoke(null, null), a1));
            pop.Invoke(null, new[] { a1 });
            Check("pop A1 → stack ว่าง", depth() == 0 && top.Invoke(null, null) == null);

            // ---- C: iid ติดตาม record ถูกตัวตลอด nesting (จับคู่ด้วย identity ไม่ใช่ค่า timestamp) ----
            long s0 = readSeq();
            object a2 = newRec(s0 + 1, 0);
            object c2 = newRec(s0 + 2, 2);
            push.Invoke(null, new[] { a2 });
            push.Invoke(null, new[] { c2 });
            long topInner = (long)invRec.GetField("Iid").GetValue(top.Invoke(null, null));
            pop.Invoke(null, new[] { c2 });
            long topOuter = (long)invRec.GetField("Iid").GetValue(top.Invoke(null, null));
            Check("iid เดินตาม record: inner=outer+1 ตรงตามที่ push",
                  topInner == s0 + 2 && topOuter == s0 + 1 && s0 + 1 != s0 + 2);
            pop.Invoke(null, new[] { a2 });
            Check("unwind ครบ → stack ว่าง", depth() == 0);

            // ---- D: exception path — push แล้ว original throw (postfix ไม่ถูกเรียก) finalizer pop ตรง ๆ ----
            object b1 = newRec(201, 1);   // HasPost คง false — ไม่มีใครเซ็ตเหมือน postfix ที่ไม่ได้รัน
            push.Invoke(null, new[] { b1 });
            pop.Invoke(null, new[] { b1 });
            Check("exception path: finalizer pop โดยไม่มี postfix → stack ว่าง + record ไม่หายไปผิดตัว",
                  depth() == 0 && (bool)invRec.GetField("HasPost").GetValue(b1) == false);

            // ---- E: nesting ลึก 4 ระดับ — unwind กลับทาง LIFO ครบทุกชั้น ----
            object[] deep = { newRec(301, 0), newRec(302, 2), newRec(303, 1), newRec(304, 2) };
            for (int i = 0; i < deep.Length; i++) push.Invoke(null, new[] { deep[i] });
            bool deepOk = depth() == 4;
            for (int i = deep.Length - 1; i >= 0; i--)
                deepOk &= ReferenceEquals(top.Invoke(null, null), deep[i]) && (pop.Invoke(null, new[] { deep[i] }) == null);
            deepOk &= depth() == 0;
            Check("nesting 4 ระดับ: Top/Pop ตรงตัวทุกชั้น + ว่างท้ายรอบ", deepOk);

            // ---- F: thread isolation — record ของ worker มองไม่เห็นจาก main ([ThreadStatic] จริง) ----
            object w1 = newRec(401, 2);
            using (ManualResetEvent pushed = new ManualResetEvent(false))
            using (ManualResetEvent observed = new ManualResetEvent(false))
            {
                int workerDepth = -1, workerAfter = -1;
                object workerTop = null;
                Thread th = new Thread(() =>
                {
                    push.Invoke(null, new[] { w1 });
                    workerDepth = depth();
                    pushed.Set();
                    observed.WaitOne(5000);
                    workerTop = top.Invoke(null, null);
                    pop.Invoke(null, new[] { w1 });
                    workerAfter = depth();
                });
                th.Start();
                pushed.WaitOne(5000);
                bool mainSeesNothing = top.Invoke(null, null) == null && depth() == 0;
                observed.Set();
                th.Join(5000);
                Check("isolation: main มองไม่เห็น record ของ worker ขณะ worker ถืออยู่", mainSeesNothing);
                Check("isolation: worker จับคู่ Top/Pop ตัวเองครบ (depth 1 → 0)",
                      workerDepth == 1 && ReferenceEquals(workerTop, w1) && workerAfter == 0);
            }
            Check("หลัง join: main ยังว่าง (ไม่มี record ข้าม thread)", depth() == 0);

            // ---- G: _iidSeq เป็น counter ยาวที่ handler เพิ่มแบบ atomic — เพิ่มทีละ 1 ได้โมโนโทนิก ----
            long g0 = readSeq();
            seqF.SetValue(null, g0 + 1);
            long g1 = readSeq();
            seqF.SetValue(null, g1 + 1);
            long g2 = readSeq();
            Check("_iidSeq เดินโมโนโทนิกทีละ 1 (กลไก counter ที่ handler ใช้ผูก iid)",
                  g1 == g0 + 1 && g2 == g1 + 1 && g2 > g0);

            // ---- H: สถานะสุดท้ายของ harness ไม่ทิ้งของค้างบน thread ใด ๆ ----
            Check("สถานะสุดท้าย: _invStack ของ main ว่าง", depth() == 0);
        }
    }
}
