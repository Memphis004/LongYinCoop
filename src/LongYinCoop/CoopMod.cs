using System;
using System.Diagnostics;
using MelonLoader;

[assembly: MelonInfo(typeof(LongYinCoop.CoopMod), "LongYinCoop", "0.4.0", "Westernholudur")]

namespace LongYinCoop
{
    public sealed class CoopMod : MelonMod
    {
        public const string Version = "0.4.0";
        public const int Stage = 1;
        public const string StageLabel = "S1.5-c-W2W4";

        private MainThreadDispatcher _dispatcher;
        private IpcServer _ipc;
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
            CompatInventory.Dump();
            BattleLogHook.Apply();

            if (CoopPrefs.Ipc.Value)
            {
                _ipc = new IpcServer(_dispatcher);
                _ipc.Start();
            }
        }

        public override void OnDeinitializeMelon()
        {
            if (_ipc != null) _ipc.Stop();
            CaptureWriter.Stop();
            BattleLogHook.Uninstall();
            JitProbe.Uninstall();
            JitProbeW2.Uninstall();
            JitProbeW4.Uninstall();
            JitProbeW6.Uninstall();
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
                HarmonyProbe.Uninstall();
                CaptureWriter.Stop();
                return;
            }

            _dispatcher.Tick(4.0);
            // S1.5-d: คำสั่งเดินทำงานข้ามเฟรม (ขั้นที่ 2-3 รอ state 7 ของเกม)
            MoveCommand.Tick();

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
