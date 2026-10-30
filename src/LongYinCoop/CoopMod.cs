using System;
using System.Diagnostics;
using LongYinCoop.S2;
using LongYinCoop.TimeV2a;
using MelonLoader;

[assembly: MelonInfo(typeof(LongYinCoop.CoopMod), "LongYinCoop", "0.5.0", "Westernholudur")]

namespace LongYinCoop
{
    public sealed class CoopMod : MelonMod
    {
        public const string Version = "0.5.0";
        public const int Stage = 1;
        public const string StageLabel = "S2a-3";

        private MainThreadDispatcher _dispatcher;
        private IpcServer _ipc;
        private S2HostService _s2;
        private S2.S2OverlayService _overlay;
        private bool _threadReported;
        private int _ticks;

        public override void OnInitializeMelon()
        {
            CoopLog.Init(string.Format(UIStrings.StartTag, UIStrings.ModName, Version, StageLabel));
            CoopPrefs.Init();

            CoopLog.Msg(UIStrings.Loaded + string.Format(UIStrings.LoadedArgs,
                        StageLabel, Version, CoopPrefs.Enabled.Value, CoopPrefs.Probe.Value,
                        CoopPrefs.Ipc.Value, CoopPrefs.Capture.Value, CoopPrefs.DevCommands.Value,
                        CoopPrefs.AllowRemoteInput.Value));

            if (!CoopPrefs.Enabled.Value)
            {
                CoopLog.Msg(UIStrings.DisabledByPref);
                return;
            }

            _dispatcher = new MainThreadDispatcher();
            MelonEvents.OnUpdate.Subscribe(Tick);

            HarmonyProbe.Apply(CoopPrefs.Probe.Value);
            // S1.5-c W1: default false -> ไม่ติด patch เลย (เปิด/ปิดได้ด้วย pref ระหว่างรัน)
            JitProbe.Apply(CoopPrefs.JitProbe.Value);
            // S1.5-c W2/W4: แยก pref ต่อ wave (default false) — ไม่ติด patch จนกว่าจะสั่ง
            JitProbeW2.Apply(CoopPrefs.JitProbeW2.Value);
            JitProbeW4.Apply(CoopPrefs.JitProbeW4.Value);
            // S1.5-c W6: probe อ่าน chain ของ move (state ก่อน/หลัง + ตัวตน GameObject)
            JitProbeW6.Apply(CoopPrefs.JitProbeW6.Value);
            // S1.5e W7: probe อ่าน chain ของ attack (default false = ไม่ติด patch)
            JitProbeW7.Apply(CoopPrefs.JitProbeW7.Value);
            // SP5-T0: probe บัญชี NPC log-only (default false = ไม่ติด hook; one-shot ต่อ session)
            Sp5T0Probe.Apply(CoopPrefs.Sp5T0.Value);
            // V1 (มติ [Gate 3] 2026-10-31): probe log-only บน StopWorking/MiddleStopWork —
            // default false = Apply(false) = Uninstall ของสิ่งที่ไม่มีอยู่ = no-op ล้วน
            // (ไม่สร้าง Harmony/ไม่เปิดไฟล์/ไม่เปิดเธรด — มติ [Gate 3] ข้อ (ก))
            TimeV1.TimeV1Probe.Apply(CoopPrefs.TimeV1a.Value);
            // SP5-NPC gate (stage N2): ติด fail-closed — ทุก hook ตัดสินจาก Sp5NpcPolicy.Decide
            // ต่อ invocation (net.cfg npcPolicy; default off = เกมเดิม 100%) — deploy ได้เฉพาะหลัง
            // มติอนุมัติ stage runtime ตามแผน SP5-NPC §7
            Sp5NpcGate.Install();
            CompatInventory.Dump();
            BattleLogHook.Apply();

            if (CoopPrefs.Ipc.Value)
            {
                // S2a-4 W5: ส่งสถานะ S2 ให้ set_pref gate (ปฏิเสธการเปิด write-path pref ขณะ S2 active)
                // (PASS-2 audit: อ่าน _s2 ใส่ local ครั้งเดียว — กัน NRE ตอน OnDeinitializeMelon เคลียร์ _s2
                //  ระหว่างที่ IPC thread กำลังเรียก delegate; ความสดของ Active มาจาก volatile ฝั่ง S2HostService)
                // (final fail-closed audit: _s2 == null = สถานะ "ไม่ทราบ" ⇒ มองเป็น active เพื่อให้ gate
                //  ปฏิเสธการเปิด write-path — ผ่าน helper ตัวเดียวกับ T4 จึงพิสูจน์ด้วย pure test ตัวจริง;
                //  callback นี้ถูกเรียกทั้งบน IPC thread (pre-check) และบน main thread (ใน CheckedWrite))
                _ipc = new IpcServer(_dispatcher, delegate
                {
                    S2HostService s = _s2;
                    return S2.Host.S2SetPrefGate.ServiceStateToActive(
                        s != null,
                        s != null && s.Active);
                });
                _ipc.Start();
            }

            // S2a-2 + งาน A: spectator snapshot sender — ไม่ทำอะไรจนกว่า net.cfg (ของเราเอง) ตั้ง
            // enabled = true (อ่านไฟล์ตรง ๆ, poll <=1 Hz) และต้องปิด AllowRemoteInput/JitProbe*
            // (rev4: DevCommands ไม่ถูกตรวจ — ต้องเปิดไว้ให้ `spike status` = เครื่องมือวัดทำงาน)
            // ไฟล์อยู่ที่ UserData\LongYinCoop\net.cfg — MelonLoader ไม่แตะ (มติ 2026-10-22)
            _s2 = new S2HostService(CoopPaths.NetCfgPath, CoopPaths.S2EvidenceDir);
            // S2a-4 W3: guest overlay in-process — วาดอย่างเดียว (ไม่กิน input), เปิดด้วย net.cfg overlay=true
            _overlay = new S2.S2OverlayService(_s2);
        }

        public override void OnDeinitializeMelon()
        {
            if (_overlay != null)
            {
                _overlay.Dispose();
                _overlay = null;
            }
            if (_s2 != null)
            {
                _s2.Stop();
                _s2 = null;
            }
            if (_ipc != null) _ipc.Stop();
            CaptureWriter.Stop();
            // V2-A: ปิด NDJSON writer ของ sampler (pref ปิด / unload)
            TimeV2aSampler.Stop();
            BattleLogHook.Uninstall();
            JitProbe.Uninstall();
            JitProbeW2.Uninstall();
            JitProbeW4.Uninstall();
            JitProbeW6.Uninstall();
            JitProbeW7.Uninstall();
            Sp5T0Probe.Uninstall();
            // V1: ถอน probe log-only (ตรวจ patch count กลับ 0 ภายในตัวเอง — มติ [Gate 3] ข้อ (ข))
            TimeV1.TimeV1Probe.Uninstall();
            Sp5NpcGate.Uninstall();
            HarmonyProbe.Uninstall();
        }

        private void Tick()
        {
            if (_dispatcher == null) return;

            if (!CoopPrefs.Enabled.Value)
            {
                JitProbe.Uninstall();
                JitProbeW2.Uninstall();
                JitProbeW4.Uninstall();
                JitProbeW6.Uninstall();
                JitProbeW7.Uninstall();
                Sp5T0Probe.Uninstall();
                // V1: pref หลักปิด = ถอน probe log-only ด้วย
                TimeV1.TimeV1Probe.Uninstall();
                Sp5NpcGate.Uninstall();
                HarmonyProbe.Uninstall();
                CaptureWriter.Stop();
                // V2-A: pref หลักปิด = ปิด sampler ด้วย (writer ถูก dispose ทันที)
                TimeV2aSampler.Stop();
                return;
            }

            _dispatcher.Tick(4.0);
            // S1.5-d: คำสั่งเดินทำงานข้ามเฟรม (ขั้นที่ 2-3 รอ state 7 ของเกม)
            MoveCommand.Tick();
            // S1.5-e: คำสั่งโจมตี — เฟส 2 รอ hp ของเป้าลดจริง
            AttackCommand.Tick();
            // S2a-2: snapshot sampler/sender (main thread; sample only, never touches game state)
            if (_s2 != null) _s2.Tick();
            // S2a-4: overlay service (เปิด/ปิด client ตาม net.cfg + สถานะ listener — main thread)
            if (_overlay != null) _overlay.Tick();

            // V2-A: sampler อ่านอย่างเดียวของ activity pump (มติ V2A-4 — pref default false = ไม่ทำอะไร;
            // ไม่มี Harmony/hook, เขียนเฉพาะ NDJSON ตาม pointer file)
            if (CoopPrefs.TimeV2a.Value)
            {
                TimeV2aSampler.Tick();
            }
            else if (TimeV2aSampler.WriterOpen)
            {
                TimeV2aSampler.Stop();
            }

            // probe เปิด/ปิดได้ด้วย pref ระหว่างรัน
            if (CoopPrefs.Probe.Value != HarmonyProbe.Installed)
            {
                HarmonyProbe.Apply(CoopPrefs.Probe.Value);
            }
            if (CoopPrefs.JitProbeW2.Value != JitProbeW2.Installed)
            {
                JitProbeW2.Apply(CoopPrefs.JitProbeW2.Value);
            }
            if (CoopPrefs.JitProbeW4.Value != JitProbeW4.Installed)
            {
                JitProbeW4.Apply(CoopPrefs.JitProbeW4.Value);
            }
            if (CoopPrefs.JitProbeW6.Value != JitProbeW6.Installed)
            {
                JitProbeW6.Apply(CoopPrefs.JitProbeW6.Value);
            }
            if (CoopPrefs.JitProbeW7.Value != JitProbeW7.Installed)
            {
                JitProbeW7.Apply(CoopPrefs.JitProbeW7.Value);
            }

            // SP5-T0: ติด/ถอดตาม pref + เช็ค settle/cap (สำคัญ: เรียก Tick ทุกเฟรมเมื่อติดอยู่)
            if (CoopPrefs.Sp5T0.Value != Sp5T0Probe.Installed)
            {
                Sp5T0Probe.Apply(CoopPrefs.Sp5T0.Value);
            }
            if (Sp5T0Probe.Installed)
            {
                Sp5T0Probe.Tick();
            }

            // V1: ติด/ถอดตาม pref + เช็ค cap (มติ [Gate 3] 2026-10-31 — default false = no-op)
            if (CoopPrefs.TimeV1a.Value != TimeV1.TimeV1Probe.Installed)
            {
                TimeV1.TimeV1Probe.Apply(CoopPrefs.TimeV1a.Value);
            }
            if (TimeV1.TimeV1Probe.Installed)
            {
                TimeV1.TimeV1Probe.Tick();
            }

            // S1.5-c W1: ติด/ถอดเฉพาะ 2 method ตาม pref (default false)
            if (CoopPrefs.JitProbe.Value != JitProbe.Installed)
            {
                JitProbe.Apply(CoopPrefs.JitProbe.Value);
            }

            Stopwatch sw = Stopwatch.StartNew();
            int hz = CoopPrefs.WorldHz.Value;
            if (hz < 1) hz = 1;
            if (hz > 60) hz = 60;
            int streamHz = CoopPrefs.WorldStreamHz.Value;
            if (streamHz < 1) streamHz = 1;
            if (streamHz > hz) streamHz = hz;
            WorldSampler.Tick(1000 / hz, 1000 / streamHz);
            BattleSampler.Tick();
            sw.Stop();
            S1Metrics.AddTick(sw.Elapsed.TotalMilliseconds);

            _ticks++;
            if (!_threadReported && HarmonyProbe.Hits > 0)
            {
                _threadReported = true;
                ReportThreads();
            }

            if (_ticks % 600 == 0)
            {
                CoopLog.Msg(string.Format(UIStrings.HeartbeatS1, _ticks, _dispatcher.Errors,
                            BattleSampler.BattlesStarted, BattleSampler.BattlesEnded,
                            BattleLogHook.Log.Count, Math.Round(S1Metrics.TickPercentile(0.99), 3),
                            Math.Round(S1Metrics.HeapMb, 2)));
            }
        }

        private void ReportThreads()
        {
            int onUpdateThread = _dispatcher.OnUpdateThreadId;
            int probeThread = HarmonyProbe.ProbeThreadId;
            bool match = probeThread != 0 && onUpdateThread == probeThread;

            string unityVersion = null;
            string gameVersion = null;
            try
            {
                unityVersion = UnityEngine.Application.unityVersion;
                gameVersion = UnityEngine.Application.version;
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
            }

            CoopLog.Msg((match ? UIStrings.ThreadMatch : UIStrings.ThreadMismatch) +
                        string.Format(UIStrings.ThreadDetailArgs, onUpdateThread, probeThread,
                                      HarmonyProbe.Hits, HarmonyProbe.ThreadIdsCsv(),
                                      unityVersion, gameVersion));

            S0Report.Write(onUpdateThread, probeThread, match, unityVersion, gameVersion,
                           _ipc == null ? null : _ipc.PipeName);
        }
    }
}
