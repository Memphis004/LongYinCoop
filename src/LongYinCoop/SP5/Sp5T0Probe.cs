using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace LongYinCoop
{
    /// <summary>
    /// SP5-T0 — Log-Only Probe บัญชี NPC 1 creation round (แผน evidence/SP5t0plan-20261027/sp5-t0-plan.md rev 13, อนุมัติ 2026-10-27)
    ///
    /// CORE hooks H1–H5 เท่านั้น (OPTIONAL H6–H11 ไม่ติดตามแผน — ดีฟอลต์รอบแรกไม่ติด):
    ///   H1 GameController.StartNewGame()            → window ราก `sng` + patch.audit at=sng-prefix
    ///   H2 GameController.GenerateHero()            → window `genhero` (W2) + heroNum.read + audit at=genhero-prefix
    ///   H3 GameController.GenerateHeroData ×3 (A/B/C) → factory.call (prefix) / factory.result (postfix,
    ///                                                   อ่าน __result อย่างเดียว) / factory.finalizer (ทุก invocation,
    ///                                                   void-finalizer = คืน exception เดิมตาม Harmony contract) +
    ///                                                   factory.failure (conditional เมื่อมี exception) + audit 4 จุด
    ///   H4 GameController.GenerateRandomEnemy(int)  → window `genenemy` (W3) + audit at=genenemy-prefix
    ///   H5 WorldData.AddTempHero(HeroData)          → addtemp (heroID + นับ TempHeros ก่อน/หลัง)
    ///
    /// ขอบเขต log-only (บังคับ): ห้ามเขียน state เกม/args/__result, ห้าม skip original, ห้าม raw offset
    /// (identity อ่านจาก property Pointer ของ interop wrapper เท่านั้น), hook body ครอบ try/catch กลืนหมด
    ///
    /// สถานะที่ rev 13 กำหนดให้คง UNKNOWN และ build นี้ตั้งใจไม่มีกลไกพิสูจน์:
    ///   - inventoryStability ภายใน window (audit ณ จุดตรวจ = point-in-time เท่านั้น) ⇒ #C.ok / E1 / E2 คง UNKNOWN
    ///   - invocation linkage ด้วย thread-local LIFO (iid) — ผู้สมัครที่โมดควบคุมเองครบ แต่ยังไม่ผ่าน harness test
    ///     (ห้ามอ้างว่า linkage พิสูจน์แล้วจนกว่าจะรัน harness ที่ได้รับอนุมัติตอน build)
    /// </summary>
    internal static class Sp5T0Probe
    {
        private const string HarmonyId = "longyincoop.sp5t0.probe";
        // §2.5 (ข): cap เหตุการณ์; (ก): settle หลัง sng.end — ค่าเสนอ 10 นาที (§4 ข้อ 7 ยังรออนุมัติตอนรันจริง)
        private const int MaxEvents = 50000;
        private const long SettleMs = 600000;
        // §1.4: SpeHero ตรวจซ้ำ heroID อยู่ 1..170 (ขนาดตาราง SpeHeroDataBase ตาม census SP5census §7)
        private const int SpeIdMax = 170;
        // §2.4: จำกัดความยาว heroName ใน log
        private const int NameCut = 32;
        // §2.2: per-force histogram 30 ช่อง (สมาชิกสำนัก belongForceID 0..29)
        private const int ForceBins = 30;

        private static HarmonyLib.Harmony _harmony;
        private static TextWriter _writer;
        private static string _dir;
        private static readonly object FileSync = new object();
        private static readonly Stopwatch Clock = Stopwatch.StartNew();

        private static MethodInfo _tStartNewGame, _tGenerateHero, _tGenDataA, _tGenDataB, _tGenDataC,
                                  _tGenRandomEnemy, _tAddTempHero;

        public static volatile bool Installed;
        public static bool SessionOpen;

        // ===== สถานะ session (main thread เท่านั้น — creation flow ข้อเท็จจริง S0) =====
        private static long _startMs;
        private static long _sngEndAtMs;
        private static bool _sngBeginSeen, _sngEndSeen;
        private static bool _finished, _finishedNotified;
        private static long _lastAttemptMs = -2000;
        private static long _events;
        private static int _errors;          // exception ที่ hook กลืน (§2.5)
        private static int _pendingErr;      // แนบไปกับบรรทัดถัดไปที่เขียนสำเร็จ (err field)
        private static int _unattributed;    // phase sng.unattributed (E5/E6 — ปัญชี ไม่ใช่ phaseStuck)
        private static int _outside;         // factory call เมื่อ stack ว่าง (bucket pre/post)
        private static int _phaseStuck;      // nesting ผิดรูปจริงเท่านั้น (rev 4)
        private static int _rebaseline;      // list instance เปลี่ยนระหว่างจุดวัด
        private static int _readfail;        // typed read ล้มเหลว (Instance/worldData null ขณะ hook)
        private static int _addtemps;
        private static readonly long[] _ovCalls = new long[3];     // A/B/C — จำนวน call ที่เกิด (#C.calls ดิบ)
        private static readonly long[] _ovResults = new long[3];   // factory.result (nonNull นับแยก)
        private static readonly long[] _ovResultsNonNull = new long[3];
        private static readonly long[] _ovFinalizers = new long[3];// factory.finalizer ทุก invocation
        private static readonly long[] _ovFailures = new long[3];  // factory.failure (conditional)
        private static string _closeReason;

        // identity ล่าสุดของ world/Heros/TempHeros — เทียบข้ามจุดวัดเพื่อยิง rebaseline (§2.2)
        private static bool _haveIdentity;
        private static long _lastWid, _lastHidH, _lastHidT;
        private static int[][] _lastHeroNumArrays;
        private static int _lastSpeDb = -1;

        // fingerprint ของ patch.audit ล่าสุดต่อ overload — เทียบระหว่างจุดตรวจ (stable/changed, rev 12–13)
        private static readonly string[] _lastAuditFp = new string[3];
        private static bool _haveAuditFp;

        // ===== invocation linkage (iid) — thread-local LIFO ของโมด (ผู้สมัคร rev 8–9 ทางที่ 2) =====
        // UNPROVEN: ต้องผ่าน harness test ตอน build ก่อนจึงถือเป็นข้อเท็จจริง; จนกว่านั้น accounting
        // ที่พึ่ง linkage (#C.ok → E1/E2) คง UNKNOWN ตามแผน — ป้ายติดที่ event iid ทุกบรรทัด
        private sealed class InvRec
        {
            public long Iid;
            public int Ov;           // 0=A 1=B 2=C; -1 = addtemp (H5)
            public bool HasPost;
            public long TempBefore = -1;
            public int TempHeroId = int.MinValue;
        }

        [ThreadStatic]
        private static List<InvRec> _invStack;
        private static long _iidSeq;

        // ===== phase scope stack (sng / genhero / genenemy) — แยกจาก invocation LIFO =====
        [ThreadStatic]
        private static List<string> _scopes;

        private static int Bucket
        {
            get
            {
                if (_sngEndSeen) return 2;   // post
                if (_sngBeginSeen) return 1; // creation
                return 0;                    // pre
            }
        }

        private static string BucketName
        {
            get
            {
                int b = Bucket;
                return b == 2 ? "post" : b == 1 ? "creation" : "pre";
            }
        }

        private static string Phase
        {
            get
            {
                List<string> s = _scopes;
                if (s == null || s.Count == 0) return "outside";
                if (s.Count == 1 && s[0] == "sng") return "sng.unattributed";
                return string.Join(".", s.ToArray());
            }
        }

        // =====================================================================
        // ติดตั้ง / ถอน / tick
        // =====================================================================

        public static bool Apply(bool wanted)
        {
            if (!wanted)
            {
                Uninstall();
                return false;
            }
            if (Installed) return true;
            if (_finished)
            {
                if (!_finishedNotified)
                {
                    _finishedNotified = true;
                    CoopLog.Warn("Sp5T0: session ปิดไปแล้ว (" + _closeReason +
                                 ") — probe เป็น one-shot ต่อ 1 เกม session; จะไม่ arm ซ้ำ");
                }
                return false;
            }

            // กัน retry ถี่เมื่อ target resolve ไม่ได้ (idiom JitProbe)
            long now = Clock.ElapsedMilliseconds;
            if (now - _lastAttemptMs < 1000) return false;
            _lastAttemptMs = now;

            try
            {
                // CORE H1–H5 ครบทุกเป้า (7 method) หรือไม่ติดเลย — ห้ามรันรอบขาดชิ้น (§2.5 / NO-GO 5)
                MethodInfo h1 = AccessTools.Method(typeof(GameController), "StartNewGame");
                MethodInfo h2 = AccessTools.Method(typeof(GameController), "GenerateHero");
                MethodInfo h4 = AccessTools.Method(typeof(GameController), "GenerateRandomEnemy",
                                                             new Type[] { typeof(int) });
                MethodInfo h5 = AccessTools.Method(typeof(WorldData), "AddTempHero",
                                                             new Type[] { typeof(HeroData) });
                MethodInfo h3a = AccessTools.Method(typeof(GameController), "GenerateHeroData",
                        new Type[] { typeof(int), typeof(int), typeof(float), typeof(HeroData),
                                     typeof(bool), typeof(bool) });
                MethodInfo h3b = AccessTools.Method(typeof(GameController), "GenerateHeroData",
                        new Type[] { typeof(string), typeof(int), typeof(int), typeof(float), typeof(bool) });
                MethodInfo h3c = AccessTools.Method(typeof(GameController), "GenerateHeroData",
                        new Type[] { typeof(string), typeof(int), typeof(int), typeof(float), typeof(HeroData),
                                     typeof(bool), typeof(SexLimit), typeof(bool), typeof(bool) });

                MethodInfo missing = FirstNull(h1, h2, h4, h5, h3a, h3b, h3c);
                if (missing != null)
                {
                    CoopLog.Warn("Sp5T0: CORE target resolve ไม่ครบ (Missing++) — ไม่ติดทั้งชุด: " + missing);
                    return false;
                }
                _tStartNewGame = h1; _tGenerateHero = h2; _tGenRandomEnemy = h4;
                _tAddTempHero = h5; _tGenDataA = h3a; _tGenDataB = h3b; _tGenDataC = h3c;

                if (!OpenWriter()) return false;

                int ownedBefore = CompatInventory.CountOwnedPatches(HarmonyId);

                _harmony = new HarmonyLib.Harmony(HarmonyId);
                Type self = typeof(Sp5T0Probe);
                _harmony.Patch(_tStartNewGame,
                    prefix: new HarmonyMethod(M(self, "StartNewGamePrefix")),
                    postfix: new HarmonyMethod(M(self, "StartNewGamePostfix")),
                    finalizer: new HarmonyMethod(M(self, "StartNewGameFinalizer")));
                _harmony.Patch(_tGenerateHero,
                    prefix: new HarmonyMethod(M(self, "GenerateHeroPrefix")),
                    postfix: new HarmonyMethod(M(self, "GenerateHeroPostfix")),
                    finalizer: new HarmonyMethod(M(self, "GenerateHeroFinalizer")));
                _harmony.Patch(_tGenRandomEnemy,
                    prefix: new HarmonyMethod(M(self, "GenEnemyPrefix")),
                    postfix: new HarmonyMethod(M(self, "GenEnemyPostfix")),
                    finalizer: new HarmonyMethod(M(self, "GenEnemyFinalizer")));
                _harmony.Patch(_tAddTempHero,
                    prefix: new HarmonyMethod(M(self, "AddTempPrefix")),
                    postfix: new HarmonyMethod(M(self, "AddTempPostfix")),
                    finalizer: new HarmonyMethod(M(self, "AddTempFinalizer")));
                _harmony.Patch(_tGenDataA,
                    prefix: new HarmonyMethod(M(self, "GenDataAPrefix")),
                    postfix: new HarmonyMethod(M(self, "GenDataAPostfix")),
                    finalizer: new HarmonyMethod(M(self, "GenDataAFinalizer")));
                _harmony.Patch(_tGenDataB,
                    prefix: new HarmonyMethod(M(self, "GenDataBPrefix")),
                    postfix: new HarmonyMethod(M(self, "GenDataBPostfix")),
                    finalizer: new HarmonyMethod(M(self, "GenDataBFinalizer")));
                _harmony.Patch(_tGenDataC,
                    prefix: new HarmonyMethod(M(self, "GenDataCPrefix")),
                    postfix: new HarmonyMethod(M(self, "GenDataCPostfix")),
                    finalizer: new HarmonyMethod(M(self, "GenDataCFinalizer")));

                int ownedAfter = CompatInventory.CountOwnedPatches(HarmonyId);
                if (ownedAfter != 7)
                {
                    CoopLog.Warn("Sp5T0: patch count หลังติด = " + ownedAfter + " (คาด 7) — ถอนทั้งชุด (§2.5)");
                    Uninstall();
                    return false;
                }

                Installed = true;
                _startMs = NowMs();

                EmitEvent("session.begin", true);
                SnapshotWorld("session.begin");           // world ยังไม่พร้อม = identity null (ไม่ใช่ readfail)
                PatchAudit("session-begin");              // จุดตรวจที่ 1/4
                Flush();

                CoopLog.Msg("Sp5T0 installed (log-only): " + _dir +
                            " | patches=" + ownedAfter + " (before=" + ownedBefore + ")");
                return true;
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                try { Uninstall(); } catch { }
                return false;
            }
        }

        public static void Uninstall()
        {
            if (!Installed)
            {
                CloseSession("uninstall-not-installed");
                return;
            }
            try
            {
                CloseSession("uninstall");   // flush + session.end ก่อนถอนเสมอ
                if (_harmony != null) _harmony.UnpatchSelf();
                int owned = CompatInventory.CountOwnedPatches(HarmonyId);
                Installed = false;
                if (owned != 0)
                {
                    // §2.5: หลังถอนต้องคืน 0 — ไม่เป็น 0 = รายงานทันที (ไม่มีทางถอนค้างเงียบ ๆ)
                    CoopLog.Warn("Sp5T0: หลัง UnpatchSelf ยังเหลือ patch ของ probe = " + owned + " (ต้องเป็น 0)");
                }
                CoopLog.Msg(string.Format(
                    "Sp5T0 removed: events={0} errors={1} unattributed={2} outside={3} phaseStuck={4} rebaseline={5} readfail={6} ownedLeft={7}",
                    _events, _errors, _unattributed, _outside, _phaseStuck, _rebaseline, _readfail, owned));
            }
            catch (Exception e)
            {
                Installed = false;
                CoopLog.Error(e);
            }
        }

        /// <summary>เรียกจาก CoopMod.Tick เมื่อ Installed — เช็ค settle/cap (§2.5 ก/ข)</summary>
        public static void Tick()
        {
            if (!Installed || !SessionOpen) return;
            long now = NowMs();
            if (_sngEndSeen && now - _sngEndAtMs >= SettleMs)
            {
                CloseSession("settle");
                Uninstall();
            }
            else if (Interlocked.Read(ref _events) >= MaxEvents)
            {
                CloseSession("cap");
                Uninstall();
            }
        }

        private static MethodInfo M(Type t, string name)
        {
            return t.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        }

        private static MethodInfo FirstNull(params MethodInfo[] ms)
        {
            for (int i = 0; i < ms.Length; i++)
            {
                if (ms[i] == null) return ms[i];
            }
            return null;
        }

        // =====================================================================
        // H1 — GameController.StartNewGame() : window ราก `sng`
        // =====================================================================

        /// <summary>เหตุการณ์ field เดียว (scope begin/end) — helper ของ event ที่ไม่มี field เพิ่ม</summary>
        private static void EmitEvent(string ev, bool boundary)
        {
            StringBuilder sb = BeginEvent(ev, boundary);
            Commit(sb, boundary);
        }

        private static void StartNewGamePrefix()
        {
            try
            {
                PatchAudit("sng-prefix");                 // จุดตรวจที่ 2/4 — ก่อน original ทำงาน (rev 11)
                PushScope("sng");
                _sngBeginSeen = true;
                EmitEvent("sng.begin", true);
                SnapshotWorld("sng.begin");               // identity เปลี่ยนข้ามจุดนี้ได้ตามธรรมชาติ → rebaseline
            }
            catch { Swallow(); }
        }

        private static void StartNewGamePostfix()
        {
            try
            {
                _sngEndSeen = true;
                _sngEndAtMs = NowMs();
                EmitEvent("sng.end", true);
                SnapshotWorld("sng.end");                 // cross-check H(sng.end)==H(genhero.end) เป็นของ analyzer
            }
            catch { Swallow(); }
        }

        private static void StartNewGameFinalizer(Exception __exception)
        {
            // void finalizer = Harmony rethrow exception เดิมเสมอ — ไม่กลืน/ไม่แทนที่ (contract §2.5)
            try { PopScope("sng", "StartNewGame"); }
            catch { Swallow(); }
        }

        // =====================================================================
        // H2 — GameController.GenerateHero() : window `genhero` (W2) + heroNum.read
        // =====================================================================

        private static void GenerateHeroPrefix(GameController __instance)
        {
            try
            {
                PatchAudit("genhero-prefix");             // จุดตรวจที่ 3/4 — ก่อนเข้า W2 (rev 12)
                PushScope("genhero");
                EmitEvent("genhero.begin", true);
                ReadHeroNum();                            // ทางหลักของ T0 (§1.3) — ลูปยังไม่เดิน
                SnapshotWorld("genhero.begin");           // baseline ของ E1 (instance พร้อมแล้ว)
            }
            catch { Swallow(); }
        }

        private static void GenerateHeroPostfix()
        {
            try
            {
                EmitEvent("genhero.end", true);
                SnapshotWorld("genhero.end");             // ปลายทางของ E1
            }
            catch { Swallow(); }
        }

        private static void GenerateHeroFinalizer(Exception __exception)
        {
            try { PopScope("genhero", "GenerateHero"); }
            catch { Swallow(); }
        }

        // =====================================================================
        // H4 — GameController.GenerateRandomEnemy(int) : window `genenemy` (W3)
        // =====================================================================

        private static void GenEnemyPrefix(GameController __instance, int maxNum)
        {
            try
            {
                PatchAudit("genenemy-prefix");            // จุดตรวจที่ 4/4 — ก่อนเข้า W3 (rev 12)
                PushScope("genenemy");
                StringBuilder sb = BeginEvent("genenemy.begin", true);
                sb.Append(",\"maxNum\":").Append(maxNum);
                sb.Append(",\"tempBefore\":").Append(CountTempHeros());
                Commit(sb, true);
                SnapshotWorld("genenemy.begin");          // baseline ของ E2 (bucket creation เท่านั้นที่ใช้)
            }
            catch { Swallow(); }
        }

        private static void GenEnemyPostfix()
        {
            try
            {
                StringBuilder sb = BeginEvent("genenemy.end", true);
                sb.Append(",\"tempAfter\":").Append(CountTempHeros());
                Commit(sb, true);
                SnapshotWorld("genenemy.end");            // ปลายทางของ E2
            }
            catch { Swallow(); }
        }

        private static void GenEnemyFinalizer(Exception __exception)
        {
            // bucket post: genenemy ไร้ sng parent = การทำงานปกติของ ChangeDay (rev 4) — ไม่ใช่ phaseStuck
            try { PopScope("genenemy", "GenerateRandomEnemy"); }
            catch { Swallow(); }
        }

        // =====================================================================
        // H5 — WorldData.AddTempHero(HeroData) : addtemp (ก่อน/หลัง)
        // =====================================================================

        private static void AddTempPrefix(WorldData __instance, HeroData target)
        {
            try
            {
                InvRec rec = new InvRec();
                rec.Iid = Interlocked.Increment(ref _iidSeq);
                rec.Ov = -1;
                rec.HasPost = false;
                rec.TempHeroId = target != null ? target.heroID : int.MinValue;
                Il2CppSystem.Collections.Generic.List<HeroData> temps = __instance != null ? __instance.TempHeros : null;
                rec.TempBefore = temps != null ? temps.Count : -1;
                PushInv(rec);
            }
            catch { Swallow(); }
        }

        private static void AddTempPostfix(WorldData __instance, HeroData target)
        {
            try
            {
                InvRec rec = TopInv();
                if (rec == null || rec.Ov != -1)
                {
                    PhaseStuckMark("addtemp: postfix ไม่เจอ record บน LIFO", _scopes, "addtemp");
                    return;
                }
                StringBuilder sb = BeginEvent("addtemp", false, rec);
                sb.Append(",\"heroID\":").Append(rec.TempHeroId);
                sb.Append(",\"countBefore\":").Append(rec.TempBefore);
                Il2CppSystem.Collections.Generic.List<HeroData> temps = __instance != null ? __instance.TempHeros : null;
                sb.Append(",\"countAfter\":").Append(temps != null ? temps.Count : -1);
                sb.Append(",\"hidT\":").Append(temps != null && temps.Pointer != IntPtr.Zero
                                 ? temps.Pointer.ToInt64().ToString(CultureInfo.InvariantCulture) : "null");
                Commit(sb, false);
                rec.HasPost = true;
                // pop เกิดใน finalizer เสมอ (lifecycle เดียวกับ factory) — กัน pop ผิดคู่เมื่อ AddTempHero ซ้อนกัน
                Interlocked.Increment(ref _addtemps);
            }
            catch { Swallow(); }
        }

        private static void AddTempFinalizer(Exception __exception)
        {
            // pop เสมอ (ทั้งสำเร็จ/exception); original throw ⇒ postfix ไม่ถูกเรียก = นับ err แทน addtemp event
            try
            {
                InvRec rec = TopInv();
                if (rec == null || rec.Ov != -1)
                {
                    PhaseStuckMark("addtemp: finalizer ไม่เจอ record บน LIFO", _scopes, "addtemp");
                    return;
                }
                if (!rec.HasPost) Swallow();
                PopInv(rec);
            }
            catch { Swallow(); }
        }

        // =====================================================================
        // H3 — GameController.GenerateHeroData ×3 (return type = HeroData, OBSERVED dump.cs:376620/623/626)
        //  prefix = factory.call · postfix = factory.result (อ่าน __result อย่างเดียว) ·
        //  finalizer = factory.finalizer (ทุก invocation) + factory.failure (conditional) — void finalizer
        //  คืน exception เดิมตาม Harmony contract; ห้ามประกาศ __result ใน finalizer (rev 7)
        // =====================================================================

        // --- overload A: (int heroID, int belongForceID, float heroForceLv, HeroData heroDataBase,
        //                  bool isTempHero = True, bool isRandomEnemy = False) ---

        private static void GenDataAPrefix(int heroID, int belongForceID, float heroForceLv,
                                           HeroData heroDataBase, bool isTempHero, bool isRandomEnemy)
        {
            try
            {
                CountFactoryPlacement();
                InvRec rec = new InvRec();
                rec.Iid = Interlocked.Increment(ref _iidSeq);
                rec.Ov = 0;
                PushInv(rec);
                StringBuilder sb = BeginEvent("factory.call", false, rec);
                sb.Append(",\"ov\":\"A\"");
                sb.Append(",\"heroID\":").Append(heroID);
                sb.Append(",\"belongForceID\":").Append(belongForceID);
                sb.Append(",\"heroForceLv\":\"").Append(heroForceLv.ToString("R", CultureInfo.InvariantCulture)).Append('"');
                AppendDb(sb, heroDataBase);
                sb.Append(",\"argIsTempHero\":").Append(isTempHero ? "true" : "false");
                sb.Append(",\"argIsRandomEnemy\":").Append(isRandomEnemy ? "true" : "false");
                Commit(sb, false);
                Interlocked.Increment(ref _ovCalls[0]);
            }
            catch { Swallow(); }
        }

        private static void GenDataAPostfix(HeroData __result, int heroID, int belongForceID,
                                            float heroForceLv, HeroData heroDataBase,
                                            bool isTempHero, bool isRandomEnemy)
        {
            try
            {
                InvRec rec = TopInv();
                if (rec == null || rec.Ov != 0)
                {
                    PhaseStuckMark("factoryA: postfix ไม่เจอ record บน LIFO", _scopes, "factoryA");
                    return;
                }
                StringBuilder sb = BeginEvent("factory.result", false, rec);
                WriteResultState(sb, 0, __result);
                Commit(sb, false);
                rec.HasPost = true;
            }
            catch { Swallow(); }
        }

        private static void GenDataAFinalizer(Exception __exception)
        {
            try { FactoryFinalize(0, __exception); }
            catch { Swallow(); }
        }

        // --- overload B: (string heroName, int heroID, int belongForceID, float heroForceLv,
        //                  bool isRandomEnemy = False) — ไม่มี heroDataBase (wrapper → C) ---

        private static void GenDataBPrefix(string heroName, int heroID, int belongForceID,
                                           float heroForceLv, bool isRandomEnemy)
        {
            try
            {
                CountFactoryPlacement();
                InvRec rec = new InvRec();
                rec.Iid = Interlocked.Increment(ref _iidSeq);
                rec.Ov = 1;
                PushInv(rec);
                StringBuilder sb = BeginEvent("factory.call", false, rec);
                sb.Append(",\"ov\":\"B\"");
                sb.Append(",\"name\":\"").Append(Escape(CutName(heroName, out bool trunc)));
                if (trunc) sb.Append("\",\"nameTrunc\":true");
                else sb.Append('"');
                sb.Append(",\"heroID\":").Append(heroID);
                sb.Append(",\"belongForceID\":").Append(belongForceID);
                sb.Append(",\"heroForceLv\":\"").Append(heroForceLv.ToString("R", CultureInfo.InvariantCulture)).Append('"');
                sb.Append(",\"db\":\"n/a (wrapper)\"");
                sb.Append(",\"argIsRandomEnemy\":").Append(isRandomEnemy ? "true" : "false");
                Commit(sb, false);
                Interlocked.Increment(ref _ovCalls[1]);
            }
            catch { Swallow(); }
        }

        private static void GenDataBPostfix(HeroData __result, string heroName, int heroID,
                                            int belongForceID, float heroForceLv, bool isRandomEnemy)
        {
            try
            {
                InvRec rec = TopInv();
                if (rec == null || rec.Ov != 1)
                {
                    PhaseStuckMark("factoryB: postfix ไม่เจอ record บน LIFO", _scopes, "factoryB");
                    return;
                }
                StringBuilder sb = BeginEvent("factory.result", false, rec);
                WriteResultState(sb, 1, __result);
                Commit(sb, false);
                rec.HasPost = true;
            }
            catch { Swallow(); }
        }

        private static void GenDataBFinalizer(Exception __exception)
        {
            try { FactoryFinalize(1, __exception); }
            catch { Swallow(); }
        }

        // --- overload C: (string heroName, int heroID, int belongForceID, float heroForceLv,
        //                  HeroData heroDataBase, bool isTempHero, SexLimit sexLimit = 0,
        //                  bool isRandomEnemy = False, bool outSideForce = False) ---

        private static void GenDataCPrefix(string heroName, int heroID, int belongForceID,
                                           float heroForceLv, HeroData heroDataBase, bool isTempHero,
                                           SexLimit sexLimit, bool isRandomEnemy, bool outSideForce)
        {
            try
            {
                CountFactoryPlacement();
                InvRec rec = new InvRec();
                rec.Iid = Interlocked.Increment(ref _iidSeq);
                rec.Ov = 2;
                PushInv(rec);
                StringBuilder sb = BeginEvent("factory.call", false, rec);
                sb.Append(",\"ov\":\"C\"");
                sb.Append(",\"name\":\"").Append(Escape(CutName(heroName, out bool trunc)));
                if (trunc) sb.Append("\",\"nameTrunc\":true");
                else sb.Append('"');
                sb.Append(",\"heroID\":").Append(heroID);
                sb.Append(",\"belongForceID\":").Append(belongForceID);
                sb.Append(",\"heroForceLv\":\"").Append(heroForceLv.ToString("R", CultureInfo.InvariantCulture)).Append('"');
                AppendDb(sb, heroDataBase);
                sb.Append(",\"argIsTempHero\":").Append(isTempHero ? "true" : "false");
                sb.Append(",\"sexLimit\":").Append((int)sexLimit);
                sb.Append(",\"argIsRandomEnemy\":").Append(isRandomEnemy ? "true" : "false");
                sb.Append(",\"outSideForce\":").Append(outSideForce ? "true" : "false");
                Commit(sb, false);
                Interlocked.Increment(ref _ovCalls[2]);
            }
            catch { Swallow(); }
        }

        private static void GenDataCPostfix(HeroData __result, string heroName, int heroID,
                                            int belongForceID, float heroForceLv, HeroData heroDataBase,
                                            bool isTempHero, SexLimit sexLimit, bool isRandomEnemy,
                                            bool outSideForce)
        {
            try
            {
                InvRec rec = TopInv();
                if (rec == null || rec.Ov != 2)
                {
                    PhaseStuckMark("factoryC: postfix ไม่เจอ record บน LIFO", _scopes, "factoryC");
                    return;
                }
                StringBuilder sb = BeginEvent("factory.result", false, rec);
                WriteResultState(sb, 2, __result);
                Commit(sb, false);
                rec.HasPost = true;
            }
            catch { Swallow(); }
        }

        private static void GenDataCFinalizer(Exception __exception)
        {
            try { FactoryFinalize(2, __exception); }
            catch { Swallow(); }
        }

        /// <summary>
        /// finalizer ของ factory ทั้งสาม overload — รันทุก invocation (สำเร็จ/exception) เสมอ:
        /// emit factory.finalizer (เชิงบวก) + factory.failure เมื่อ __exception != null เท่านั้น
        /// แล้ว pop invocation record — void finalizer ⇒ Harmony rethrow exception เดิม ไม่กลืน/ไม่แทนที่
        /// (ห้ามอ่าน __result ที่นี่ — rev 7)
        /// </summary>
        private static void FactoryFinalize(int ov, Exception __exception)
        {
            InvRec rec = TopInv();
            if (rec == null || rec.Ov != ov)
            {
                PhaseStuckMark("factory" + OvName(ov) + ": finalizer ไม่เจอ record บน LIFO", _scopes, "factory" + OvName(ov));
                return;
            }
            string exName = __exception != null ? __exception.GetType().Name : null;

            StringBuilder sb = BeginEvent("factory.finalizer", false, rec);
            sb.Append(",\"exceptionAtProbeFinalizer\":");
            sb.Append(exName != null ? "\"" + Escape(exName) + "\"" : "null");
            sb.Append(",\"phasePop\":true");
            Commit(sb, false);
            Interlocked.Increment(ref _ovFinalizers[ov]);

            if (exName != null)
            {
                // ความหมาย = "exception observed in the patched invocation pipeline" — ยังไม่แปลว่า
                // original throw เว้นแต่มีหลักฐานแยกแหล่ง (rev 8)
                StringBuilder fb = BeginEvent("factory.failure", false, rec);
                fb.Append(",\"exType\":\"").Append(Escape(exName)).Append('"');
                Commit(fb, false);
                Interlocked.Increment(ref _ovFailures[ov]);
            }

            PopInv(rec);
        }

        /// <summary>นับ placement ของ factory call จาก phase ปัจจุบัน (E5/E6) — marker คนละชุดกับ phaseStuck (rev 5)</summary>
        private static void CountFactoryPlacement()
        {
            string ph = Phase;
            if (ph == "sng.unattributed") Interlocked.Increment(ref _unattributed);
            else if (ph == "outside") Interlocked.Increment(ref _outside);
        }

        /// <summary>post-state ของ target อ่านจาก __result (ชนิด HeroData) แบบ typed เท่านั้น (rev 6) — ห้ามใช้ arg flags แทน</summary>
        private static void WriteResultState(StringBuilder sb, int ov, HeroData __result)
        {
            sb.Append(",\"ov\":\"").Append(OvName(ov)).Append('"');
            if (__result == null)
            {
                sb.Append(",\"resultNonNull\":false");
            }
            else
            {
                sb.Append(",\"resultNonNull\":true");
                sb.Append(",\"postIsTempHero\":").Append(__result.isTempHero ? "true" : "false");
                sb.Append(",\"postIsRandomEnemy\":").Append(__result.isRandomEnemy ? "true" : "false");
                sb.Append(",\"postSpeHero\":").Append(__result.speHero ? "true" : "false");
                sb.Append(",\"postBelongForceID\":").Append(__result.belongForceID);
                sb.Append(",\"postHeroID\":").Append(__result.heroID);
            }
            Interlocked.Increment(ref _ovResults[ov]);
            if (__result != null) Interlocked.Increment(ref _ovResultsNonNull[ov]);
        }

        // =====================================================================
        // patch.audit — inventory ของ H3 overload ทั้งสาม ณ 4 จุดตรวจ (read-only, rev 10–13)
        // =====================================================================

        private static void PatchAudit(string at)
        {
            StringBuilder sb = BeginEvent("patch.audit", true);
            sb.Append(",\"at\":\"").Append(at).Append('"');
            sb.Append(",\"apiScope\":\"HarmonyX 2.10.2 GetPatchInfo — เห็นเฉพาะ patch ที่ลงทะเบียนผ่าน Harmony runtime นี้\"");
            sb.Append(",\"ov\":{");

            bool allMatch = true;
            bool allStable = true;
            MethodInfo[] targets = { _tGenDataA, _tGenDataB, _tGenDataC };
            string[] names = { "A", "B", "C" };

            for (int i = 0; i < 3; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(names[i]).Append("\":{");

                string fp;
                string inv;
                bool match;
                List<string> reasons = new List<string>();
                fp = AuditOne(targets[i], i, reasons, out match, out inv);

                // เทียบระหว่างจุดตรวจ — ครอบเฉพาะช่วงระหว่างจุดตรวจ ไม่ครอบภายใน window (rev 13)
                string stable = !_haveAuditFp ? "n/a" : (_lastAuditFp[i] == fp ? "stable" : "changed");

                sb.Append("\"inv\":").Append(inv);
                sb.Append(",\"match\":").Append(match ? "true" : "false");
                sb.Append(",\"cross\":\"").Append(stable).Append('"');
                if (reasons.Count > 0)
                {
                    sb.Append(",\"mismatch\":[");
                    for (int r = 0; r < reasons.Count; r++)
                    {
                        if (r > 0) sb.Append(',');
                        sb.Append('"').Append(Escape(reasons[r])).Append('"');
                    }
                    sb.Append(']');
                }
                sb.Append('}');

                _lastAuditFp[i] = fp;
                if (!match) allMatch = false;
                if (stable == "changed") allStable = false;
            }
            _haveAuditFp = true;

            sb.Append("},\"allMatch\":").Append(allMatch ? "true" : "false");
            sb.Append(",\"allStableBetweenCheckpoints\":").Append(allStable ? "true" : "false");
            sb.Append(",\"inventoryStabilityInWindow\":\"UNKNOWN (rev 13 — build นี้ไม่มีกลไกตรวจภายใน window)\"");
            sb.Append(",\"linkage\":\"iid UNPROVEN (thread-local LIFO รอ harness test ตอน build)\"");
            Commit(sb, true);
        }

        /// <summary>
        /// ตรวจ method เป้า 1 ตัวเทียบ expected inventory เป๊ะ: prefix 1 + postfix 1 + finalizer 1
        /// (owner=HarmonyId, ชื่อ patch method ตรง) + Transpilers 0 + ILManipulators 0 —
        /// ครบ 5 categories ที่ HarmonyX 2.10.2 เปิดเผย (ไม่มี InnerPrefix/InnerPostfix ให้ตรวจ = ข้อจำกัด) ·
        /// คืน fingerprint (เทียบระหว่างจุดตรวจ) + JSON inventory จริงของทุก category
        /// </summary>
        private static string AuditOne(MethodBase target, int ov, List<string> reasons,
                                       out bool match, out string inventoryJson)
        {
            string ovn = OvName(ov);
            StringBuilder fp = new StringBuilder();
            StringBuilder inv = new StringBuilder();
            match = true;

            if (target == null)
            {
                reasons.Add("target null");
                match = false;
                inventoryJson = "null";
                return "null";
            }

            Patches info = HarmonyLib.Harmony.GetPatchInfo(target);
            if (info == null)
            {
                reasons.Add("GetPatchInfo null");
                match = false;
                inventoryJson = "null";
                return "null";
            }

            inv.Append("{\"pre\":");
            CheckCategory(fp, "pre", inv, info.Prefixes, 1, "Sp5T0Probe.GenData" + ovn + "Prefix", reasons, ref match);
            inv.Append(",\"post\":");
            CheckCategory(fp, "post", inv, info.Postfixes, 1, "Sp5T0Probe.GenData" + ovn + "Postfix", reasons, ref match);
            inv.Append(",\"fin\":");
            CheckCategory(fp, "fin", inv, info.Finalizers, 1, "Sp5T0Probe.GenData" + ovn + "Finalizer", reasons, ref match);
            inv.Append(",\"trans\":");
            CheckCategory(fp, "trans", inv, info.Transpilers, 0, null, reasons, ref match);
            inv.Append(",\"ilm\":");
            CheckCategory(fp, "ilm", inv, info.ILManipulators, 0, null, reasons, ref match);
            inv.Append('}');

            inventoryJson = inv.ToString();
            return fp.ToString();
        }

        private static void CheckCategory(StringBuilder fp, string key, StringBuilder inv,
                                          IEnumerable<Patch> patches, int expectedCount,
                                          string expectedMethod, List<string> reasons, ref bool match)
        {
            List<string> items = new List<string>();
            if (patches != null)
            {
                foreach (Patch p in patches)
                {
                    string owner = p.owner;
                    string method = "?";
                    try
                    {
                        MethodBase pm = p.PatchMethod;
                        method = (pm == null ? "?" :
                                 (pm.DeclaringType == null ? "?" : pm.DeclaringType.Name) + "::" + pm.Name);
                    }
                    catch { }
                    items.Add(owner + ":" + method);
                }
            }

            inv.Append('[');
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0) inv.Append(',');
                inv.Append('"').Append(Escape(items[i])).Append('"');
            }
            inv.Append(']');

            fp.Append(key).Append('=').Append(items.Count);
            foreach (string it in items) fp.Append('|').Append(it);

            if (items.Count != expectedCount)
            {
                reasons.Add(key + " count=" + items.Count + " expected=" + expectedCount);
                match = false;
                return;
            }
            if (expectedMethod != null)
            {
                foreach (string it in items)
                {
                    if (it != HarmonyId + ":" + expectedMethod)
                    {
                        reasons.Add(key + " ไม่ตรง expected: " + it + " (expected " + HarmonyId + ":" + expectedMethod + ")");
                        match = false;
                    }
                }
            }
        }

        // =====================================================================
        // heroNum.read — ทางหลักของ T0 (§1.3): อ่าน typed, copy อ่านอย่างเดียว, ไม่เขียนกลับ
        // =====================================================================

        private static void ReadHeroNum()
        {
            try
            {
                StringBuilder sb = BeginEvent("heroNum.read", true);
                GameDataController gdc = GameDataController.Instance;
                if (gdc == null)
                {
                    sb.Append(",\"readfail\":\"GameDataController.Instance null\"");
                    Commit(sb, true);
                    Interlocked.Increment(ref _readfail);
                    return;
                }

                sb.Append(",\"force\":[");
                Il2CppSystem.Collections.Generic.List<ForceLevelData> lvls = gdc.forceLevelData;
                int n = lvls != null ? lvls.Count : 0;
                int entries = 0;
                List<int[]> arrays = new List<int[]>();
                for (int i = 0; i < n; i++)
                {
                    if (i > 0) sb.Append(',');
                    ForceLevelData e = lvls[i];
                    if (e == null) { sb.Append("null"); continue; }
                    Il2CppSystem.Collections.Generic.List<int> heroNum = e.heroNum;
                    int m = heroNum != null ? heroNum.Count : 0;
                    sb.Append('[');
                    int[] copy = new int[m];   // copy อ่านอย่างเดียว (§1.3) — ไม่เขียนกลับ field
                    for (int j = 0; j < m; j++)
                    {
                        if (j > 0) sb.Append(',');
                        copy[j] = heroNum[j];
                        sb.Append(copy[j]);
                    }
                    sb.Append(']');
                    arrays.Add(copy);
                    entries += m;
                }
                sb.Append(']');
                sb.Append(",\"forces\":").Append(n);
                if (lvls == null) sb.Append(",\"forceLevelData\":null");
                sb.Append(",\"entries\":").Append(entries);
                _lastHeroNumArrays = arrays.ToArray();

                Il2CppSystem.Collections.Generic.Dictionary<int, HeroData> speDb = gdc.SpeHeroDataBase;
                _lastSpeDb = speDb != null ? speDb.Count : -1;
                sb.Append(",\"speDb\":").Append(_lastSpeDb);
                sb.Append(",\"speDbNote\":\"table size เท่านั้น — ไม่ใช่จำนวน HeroData ที่ instantiate (§1.3)\"");
                AppendIdentity(sb);
                Commit(sb, true);
            }
            catch
            {
                // JSON ครึ่งบรรทัดห้ามปล่อยลงไฟล์ — เขียนบรรทัดใหม่แทน (readfail เป็น field ของ event เดิม)
                Swallow();
                Interlocked.Increment(ref _readfail);
                StringBuilder rb = BeginEvent("heroNum.read", true);
                rb.Append(",\"readfail\":\"exception\"");
                Commit(rb, true);
            }
        }

        // =====================================================================
        // worldcount — snapshot ที่ boundary (§2.2): นับ + histogram 8 คลาส + perForce + identity
        // =====================================================================

        private static void SnapshotWorld(string at)
        {
            try
            {
                StringBuilder sb = BeginEvent("worldcount", true);
                sb.Append(",\"at\":\"").Append(at).Append('"');
                WorldData wd = CurrentWorld();
                if (wd == null)
                {
                    sb.Append(",\"world\":null,\"h\":0,\"t\":0");
                    Commit(sb, true);
                    return;
                }

                long wid = wd.Pointer != IntPtr.Zero ? wd.Pointer.ToInt64() : 0;
                Il2CppSystem.Collections.Generic.List<HeroData> heros = wd.Heros;
                Il2CppSystem.Collections.Generic.List<HeroData> temps = wd.TempHeros;
                long hidH = heros != null && heros.Pointer != IntPtr.Zero ? heros.Pointer.ToInt64() : 0;
                long hidT = temps != null && temps.Pointer != IntPtr.Zero ? temps.Pointer.ToInt64() : 0;

                // rebaseline: identity เปลี่ยนระหว่างจุดวัด (§2.2) — เป็น marker แยกบรรทัด
                if (_haveIdentity && (wid != _lastWid || hidH != _lastHidH || hidT != _lastHidT))
                {
                    StringBuilder rb = BeginEvent("rebaseline", true);
                    rb.Append(",\"old\":{\"wid\":").Append(_lastWid)
                       .Append(",\"hidH\":").Append(_lastHidH)
                       .Append(",\"hidT\":").Append(_lastHidT).Append('}');
                    rb.Append(",\"new\":{\"wid\":").Append(wid)
                       .Append(",\"hidH\":").Append(hidH)
                       .Append(",\"hidT\":").Append(hidT).Append('}');
                    Commit(rb, true);
                    Interlocked.Increment(ref _rebaseline);
                }
                _haveIdentity = true;
                _lastWid = wid; _lastHidH = hidH; _lastHidT = hidT;

                int[] hist = new int[8];   // 0 protag 1 spe 2 sect 3 wanderer 4 f-2 5 temp 6 tempPlot 7 other
                int[] perForce = new int[ForceBins];
                int speOutOfRange = 0;

                int h = heros != null ? heros.Count : 0;
                for (int i = 0; i < h; i++)
                {
                    HeroData hd = heros[i];
                    if (hd == null) { hist[7]++; continue; }
                    int cls = Classify(hd, perForce, ref speOutOfRange);
                    hist[cls]++;
                }

                int t = temps != null ? temps.Count : 0;
                int tempMatch = 0, tempOther = 0;
                for (int i = 0; i < t; i++)
                {
                    HeroData hd = temps[i];
                    if (hd == null) { tempOther++; continue; }
                    if (hd.isTempHero && hd.isRandomEnemy) tempMatch++;
                    else tempOther++;
                }

                sb.Append(",\"wid\":").Append(wid);
                sb.Append(",\"hidH\":").Append(hidH);
                sb.Append(",\"hidT\":").Append(hidT);
                sb.Append(",\"h\":").Append(h);
                sb.Append(",\"t\":").Append(t);
                sb.Append(",\"hist\":{\"protag\":").Append(hist[0])
                   .Append(",\"spe\":").Append(hist[1])
                   .Append(",\"sect\":").Append(hist[2])
                   .Append(",\"wanderer\":").Append(hist[3])
                   .Append(",\"f2\":").Append(hist[4])
                   .Append(",\"temp\":").Append(hist[5])
                   .Append(",\"tempPlot\":").Append(hist[6])
                   .Append(",\"other\":").Append(hist[7]).Append('}');
                sb.Append(",\"speIdOutOfRange\":").Append(speOutOfRange);
                sb.Append(",\"perForce\":[");
                for (int i = 0; i < ForceBins; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(perForce[i]);
                }
                sb.Append(']');
                sb.Append(",\"tempMatch\":").Append(tempMatch);
                sb.Append(",\"tempOther\":").Append(tempOther);
                Commit(sb, true);
            }
            catch
            {
                // JSON ครึ่งบรรทัดห้ามปล่อยลงไฟล์ — เขียนบรรทัดใหม่แทน (readfail เป็น field ของ event เดิม)
                Swallow();
                Interlocked.Increment(ref _readfail);
                StringBuilder rb = BeginEvent("worldcount", true);
                rb.Append(",\"at\":\"").Append(at).Append('"');
                rb.Append(",\"readfail\":\"exception\"");
                Commit(rb, true);
            }
        }

        /// <summary>อ่าน worldData แบบ typed จาก GameController.Instance — null ได้ (ยังไม่มีโลก)</summary>
        private static WorldData CurrentWorld()
        {
            GameController gc = GameController.Instance;
            return gc != null ? gc.worldData : null;
        }

        private static int CountTempHeros()
        {
            WorldData wd = CurrentWorld();
            Il2CppSystem.Collections.Generic.List<HeroData> temps = wd != null ? wd.TempHeros : null;
            return temps != null ? temps.Count : -1;
        }

        /// <summary>
        /// จำแนก 8 คลาสตามตาราง §1.4 (ลำดับตามตาราง — คืน index ของ hist) · เงื่อนไขเขียนบนสถานะโลก
        /// ที่อ่าน typed เท่านั้น · คลาสอื่นที่เหลือ = other (ต้อง 0 ในรอบสะอาด)
        /// </summary>
        private static int Classify(HeroData hd, int[] perForce, ref int speOutOfRange)
        {
            if (hd.heroID == 0) return 0;                                            // ตัวเอก
            if (hd.speHero)
            {
                if (hd.heroID < 1 || hd.heroID > SpeIdMax) speOutOfRange++;          // ตรวจซ้ำ 1..170
                return 1;                                                            // SpeHero (PROTECTED)
            }
            bool noTemp = !hd.isTempHero;
            if (noTemp && hd.belongForceID >= 0 && hd.belongForceID <= 29)
            {
                perForce[hd.belongForceID]++;
                return 2;                                                            // สมาชิกสำนัก
            }
            if (noTemp && hd.belongForceID == -1) return 3;                          // เร่ร่อน
            if (noTemp && hd.belongForceID == -2) return 4;                          // กลุ่ม forceID −2
            if (hd.isTempHero && hd.isRandomEnemy) return 5;                         // TempHero (ศัตรุสุ่ม)
            if (hd.tempPlotHero) return 6;                                           // temp plot hero
            return 7;                                                                // other
        }

        private static void AppendIdentity(StringBuilder sb)
        {
            WorldData wd = CurrentWorld();
            if (wd == null)
            {
                sb.Append(",\"wid\":null,\"hidH\":null,\"hidT\":null");
                return;
            }
            sb.Append(",\"wid\":").Append(wd.Pointer != IntPtr.Zero ? wd.Pointer.ToInt64().ToString(CultureInfo.InvariantCulture) : "null");
            Il2CppSystem.Collections.Generic.List<HeroData> heros = wd.Heros;
            Il2CppSystem.Collections.Generic.List<HeroData> temps = wd.TempHeros;
            sb.Append(",\"hidH\":").Append(heros != null && heros.Pointer != IntPtr.Zero ? heros.Pointer.ToInt64().ToString(CultureInfo.InvariantCulture) : "null");
            sb.Append(",\"hidT\":").Append(temps != null && temps.Pointer != IntPtr.Zero ? temps.Pointer.ToInt64().ToString(CultureInfo.InvariantCulture) : "null");
        }

        private static void AppendDb(StringBuilder sb, HeroData heroDataBase)
        {
            if (heroDataBase == null)
            {
                sb.Append(",\"db\":null");
            }
            else
            {
                sb.Append(",\"db\":{\"nonNull\":true,\"ptr\":")
                  .Append(heroDataBase.Pointer != IntPtr.Zero ? heroDataBase.Pointer.ToInt64().ToString(CultureInfo.InvariantCulture) : "0")
                  .Append('}');
            }
        }

        private static string CutName(string name, out bool trunc)
        {
            trunc = false;
            if (name == null) return null;
            if (name.Length > NameCut)
            {
                trunc = true;
                return name.Substring(0, NameCut);
            }
            return name;
        }

        private static string OvName(int ov)
        {
            return ov == 0 ? "A" : ov == 1 ? "B" : "C";
        }

        // =====================================================================
        // phase scope stack + invocation LIFO + phaseStuck (rev 4: nesting ผิดรูปจริงเท่านั้น)
        // =====================================================================

        private static void PushScope(string name)
        {
            List<string> s = _scopes;
            if (s == null) { s = new List<string>(); _scopes = s; }
            for (int i = 0; i < s.Count; i++)
            {
                if (s[i] == name)
                {
                    // class (1b): scope เดิมยังค้าง (leak) — push ซ้ำโดยไม่มี pop คู่
                    PhaseStuckMark("push '" + name + "' ซ้ำโดย scope เดิมยังค้าง", s, "push:" + name);
                    break;
                }
            }
            s.Add(name);
        }

        private static void PopScope(string expected, string by)
        {
            List<string> s = _scopes;
            if (s == null || s.Count == 0)
            {
                PhaseStuckMark("pop '" + expected + "' โดย stack ว่าง (" + by + ")", s, "pop:" + expected);
                return;
            }
            if (s[s.Count - 1] == expected)
            {
                s.RemoveAt(s.Count - 1);
                return;
            }
            // class (1): pop ไม่ตรง scope ของ push — auto-reset: ดึง expected ออกจากที่ที่อยู่
            PhaseStuckMark("pop '" + expected + "' ไม่ตรง top '" + s[s.Count - 1] + "' (" + by + ")", s, "pop:" + expected);
            for (int i = s.Count - 1; i >= 0; i--)
            {
                if (s[i] == expected) { s.RemoveAt(i); return; }
            }
            PhaseStuckMark("reset: ไม่เจอ '" + expected + "' บน stack (leak ที่อื่น)", s, "reset:" + expected);
        }

        private static void PhaseStuckMark(string detail, List<string> stack, string ev)
        {
            StringBuilder sb = BeginEvent("phaseStuck", true);
            sb.Append(",\"ev\":\"").Append(Escape(ev)).Append('"');
            sb.Append(",\"detail\":\"").Append(Escape(detail)).Append('"');
            sb.Append(",\"stack\":[");
            if (stack != null)
            {
                for (int i = 0; i < stack.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('"').Append(stack[i]).Append('"');
                }
            }
            sb.Append("],\"reset\":\"unwind expected scope\"");
            Commit(sb, true);
            Interlocked.Increment(ref _phaseStuck);
        }

        private static void PushInv(InvRec rec)
        {
            List<InvRec> s = _invStack;
            if (s == null) { s = new List<InvRec>(); _invStack = s; }
            s.Add(rec);
        }

        private static InvRec TopInv()
        {
            List<InvRec> s = _invStack;
            if (s == null || s.Count == 0) return null;
            return s[s.Count - 1];
        }

        private static void PopInv(InvRec rec)
        {
            List<InvRec> s = _invStack;
            if (s == null || s.Count == 0)
            {
                PhaseStuckMark("popInv โดย stack ว่าง", null, "inv");
                return;
            }
            if (s[s.Count - 1] == rec)
            {
                s.RemoveAt(s.Count - 1);
                return;
            }
            PhaseStuckMark("popInv ไม่ตรง top (iid=" + rec.Iid + ")", null, "inv");
            for (int i = s.Count - 1; i >= 0; i--)
            {
                if (s[i] == rec) { s.RemoveAt(i); return; }
            }
        }

        // =====================================================================
        // writer — NDJSON buffered: lock + flush เฉพาะ boundary (§2.4)
        // =====================================================================

        private static bool OpenWriter()
        {
            try
            {
                _dir = Path.Combine(CoopPaths.UserDataDir, "SP5T0", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                Directory.CreateDirectory(_dir);
                string path = Path.Combine(_dir, "sp5t0.ndjson");
                FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 16384);
                _writer = new StreamWriter(fs, new UTF8Encoding(false));
                SessionOpen = true;
                return true;
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                SessionOpen = false;
                return false;
            }
        }

        private static StringBuilder BeginEvent(string ev, bool boundary)
        {
            return BeginEvent(ev, boundary, null);
        }

        private static StringBuilder BeginEvent(string ev, bool boundary, InvRec iidRec)
        {
            StringBuilder sb = new StringBuilder(256);
            sb.Append("{\"t\":").Append(NowMs());
            sb.Append(",\"frame\":").Append(Time.frameCount);
            sb.Append(",\"ev\":\"").Append(ev).Append('"');
            sb.Append(",\"ph\":\"").Append(Phase).Append('"');
            List<string> s = _scopes;
            sb.Append(",\"depth\":").Append(s != null ? s.Count : 0);
            sb.Append(",\"bucket\":\"").Append(BucketName).Append('"');
            if (iidRec != null)
            {
                sb.Append(",\"iid\":\"").Append(iidRec.Iid)
                  .Append(iidRec.Ov >= 0 ? "\"" : "\",\"iidKind\":\"addtemp\"");
            }
            int pend = Interlocked.Exchange(ref _pendingErr, 0);
            if (pend > 0) sb.Append(",\"err\":").Append(pend);
            return sb;
        }

        private static void Commit(StringBuilder sb, bool flush)
        {
            sb.Append('}');
            lock (FileSync)
            {
                try
                {
                    _writer.Write(sb.ToString());
                    _writer.Write('\n');
                    Interlocked.Increment(ref _events);
                    if (flush) _writer.Flush();
                }
                catch
                {
                    Swallow();
                }
            }
        }

        private static void Flush()
        {
            lock (FileSync)
            {
                try { if (_writer != null) _writer.Flush(); }
                catch { }
            }
        }

        private static void Swallow()
        {
            Interlocked.Increment(ref _errors);
            Interlocked.Increment(ref _pendingErr);
        }

        // =====================================================================
        // ปิด session — flush + session.end + summary.json (§2.4/§2.5)
        // =====================================================================

        private static void CloseSession(string reason)
        {
            lock (FileSync)
            {
                if (!SessionOpen) return;
                SessionOpen = false;
                _closeReason = reason;
                _finished = true;

                try
                {
                    // class (4): session.end แล้ว stack ไม่ว่าง (§2.5)
                    List<string> s = _scopes;
                    if (s != null && s.Count > 0)
                    {
                        StringBuilder ps = BeginEvent("phaseStuck", true);
                        ps.Append(",\"ev\":\"session.end\"");
                        ps.Append(",\"detail\":\"stack ไม่ว่างตอน session.end\"");
                        ps.Append(",\"stack\":[");
                        for (int i = 0; i < s.Count; i++)
                        {
                            if (i > 0) ps.Append(',');
                            ps.Append('"').Append(s[i]).Append('"');
                        }
                        ps.Append("],\"reset\":\"clear\"");
                        ps.Append('}');
                        _writer.Write(ps.ToString());
                        _writer.Write('\n');
                        Interlocked.Increment(ref _phaseStuck);
                        s.Clear();
                    }

                    StringBuilder sb = BeginEvent("session.end", true);
                    sb.Append(",\"reason\":\"").Append(Escape(reason)).Append('"');
                    sb.Append(",\"durMs\":").Append(NowMs() - _startMs);
                    sb.Append(",\"calls\":{\"A\":").Append(_ovCalls[0]).Append(",\"B\":").Append(_ovCalls[1])
                       .Append(",\"C\":").Append(_ovCalls[2]).Append('}');
                    sb.Append(",\"results\":{\"A\":").Append(_ovResults[0]).Append(",\"B\":").Append(_ovResults[1])
                       .Append(",\"C\":").Append(_ovResults[2]).Append('}');
                    sb.Append(",\"resultsNonNull\":{\"A\":").Append(_ovResultsNonNull[0])
                       .Append(",\"B\":").Append(_ovResultsNonNull[1]).Append(",\"C\":").Append(_ovResultsNonNull[2]).Append('}');
                    sb.Append(",\"finalizers\":{\"A\":").Append(_ovFinalizers[0]).Append(",\"B\":").Append(_ovFinalizers[1])
                       .Append(",\"C\":").Append(_ovFinalizers[2]).Append('}');
                    sb.Append(",\"failures\":{\"A\":").Append(_ovFailures[0]).Append(",\"B\":").Append(_ovFailures[1])
                       .Append(",\"C\":").Append(_ovFailures[2]).Append('}');
                    sb.Append(",\"addtemps\":").Append(_addtemps);
                    sb.Append(",\"unattributed\":").Append(_unattributed);
                    sb.Append(",\"outside\":").Append(_outside);
                    sb.Append(",\"phaseStuck\":").Append(_phaseStuck);
                    sb.Append(",\"rebaseline\":").Append(_rebaseline);
                    sb.Append(",\"readfail\":").Append(_readfail);
                    sb.Append(",\"errors\":").Append(_errors);
                    sb.Append(",\"events\":").Append(Interlocked.Read(ref _events));
                    sb.Append('}');
                    _writer.Write(sb.ToString());
                    _writer.Write('\n');
                    Interlocked.Increment(ref _events);
                }
                catch { Swallow(); }

                try
                {
                    WriteSummary(reason);
                    if (_writer != null)
                    {
                        _writer.Flush();
                        _writer.Dispose();
                        _writer = null;
                    }
                }
                catch { }
            }
        }

        /// <summary>summary = ตัวเลขดิบเท่านั้น — ไม่คำนวณ #C.ok / สมการ (linkage + stability ยัง UNKNOWN ตาม rev 13)</summary>
        private static void WriteSummary(string reason)
        {
            StringBuilder sb = new StringBuilder(1024);
            sb.Append("{\n");
            sb.Append("  \"plan\": \"SP5-T0-plan-20261027 rev13\",\n");
            sb.Append("  \"closeReason\": \"").Append(Escape(reason)).Append("\",\n");
            sb.Append("  \"startedAtMs\": ").Append(_startMs).Append(",\n");
            sb.Append("  \"durMs\": ").Append(NowMs() - _startMs).Append(",\n");
            sb.Append("  \"events\": ").Append(Interlocked.Read(ref _events)).Append(",\n");
            sb.Append("  \"errors\": ").Append(_errors).Append(",\n");
            sb.Append("  \"ovCalls\": {\"A\": ").Append(_ovCalls[0]).Append(", \"B\": ").Append(_ovCalls[1])
               .Append(", \"C\": ").Append(_ovCalls[2]).Append("},\n");
            sb.Append("  \"ovResultsNonNull\": {\"A\": ").Append(_ovResultsNonNull[0]).Append(", \"B\": ")
               .Append(_ovResultsNonNull[1]).Append(", \"C\": ").Append(_ovResultsNonNull[2]).Append("},\n");
            sb.Append("  \"ovFailures\": {\"A\": ").Append(_ovFailures[0]).Append(", \"B\": ")
               .Append(_ovFailures[1]).Append(", \"C\": ").Append(_ovFailures[2]).Append("},\n");
            sb.Append("  \"addtemps\": ").Append(_addtemps).Append(",\n");
            sb.Append("  \"unattributed\": ").Append(_unattributed).Append(",\n");
            sb.Append("  \"outside\": ").Append(_outside).Append(",\n");
            sb.Append("  \"phaseStuck\": ").Append(_phaseStuck).Append(",\n");
            sb.Append("  \"rebaseline\": ").Append(_rebaseline).Append(",\n");
            sb.Append("  \"readfail\": ").Append(_readfail).Append(",\n");
            sb.Append("  \"speDbTableSize\": ").Append(_lastSpeDb).Append(",\n");
            sb.Append("  \"heroNumLastRead\": ");
            if (_lastHeroNumArrays == null) sb.Append("null");
            else
            {
                sb.Append('[');
                for (int i = 0; i < _lastHeroNumArrays.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append('[').Append(string.Join(",", Array.ConvertAll(_lastHeroNumArrays[i], x => x.ToString()))).Append(']');
                }
                sb.Append(']');
            }
            sb.Append(",\n");
            sb.Append("  \"auditFingerprint\": {\"A\": ").Append(JsOr(_lastAuditFp[0])).Append(", \"B\": ")
               .Append(JsOr(_lastAuditFp[1])).Append(", \"C\": ").Append(JsOr(_lastAuditFp[2])).Append("},\n");
            sb.Append("  \"unknown\": {\n");
            sb.Append("    \"inventoryStabilityInWindow\": \"UNKNOWN (rev 13 — ไม่มีกลไกตรวจภายใน window)\",\n");
            sb.Append("    \"invocationLinkage\": \"UNPROVEN (thread-local LIFO iid — รอ harness test ตอน build)\",\n");
            sb.Append("    \"cOk_E1_E2\": \"UNKNOWN จนกว่าสองข้อบนจะมีหลักฐานจากกลไกที่ผ่านอนุมัติ\"\n");
            sb.Append("  }\n");
            sb.Append("}\n");
            File.WriteAllText(Path.Combine(_dir, "summary.json"), sb.ToString(), new UTF8Encoding(false));
        }

        private static string JsOr(string s)
        {
            if (s == null) return "null";
            return "\"" + Escape(s) + "\"";
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            StringBuilder sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else if (c < ' ') sb.Append(' ');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static long NowMs()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
