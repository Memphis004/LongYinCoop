using System;
using MelonLoader;

[assembly: MelonInfo(typeof(LongYinCoop.CoopMod), "LongYinCoop", "0.1.0", "Westernholudur")]

namespace LongYinCoop
{
    public sealed class CoopMod : MelonMod
    {
        public const string Version = "0.1.0";
        public const int Stage = 0;

        private MainThreadDispatcher _dispatcher;
        private IpcServer _ipc;
        private bool _threadReported;
        private int _ticks;

        public override void OnInitializeMelon()
        {
            CoopLog.Init(string.Format(UIStrings.StartTag, UIStrings.ModName, Version, Stage));
            CoopPrefs.Init();

            CoopLog.Msg(UIStrings.Loaded + string.Format(UIStrings.LoadedArgs,
                        Stage, Version, CoopPrefs.Enabled.Value, CoopPrefs.Probe.Value, CoopPrefs.Ipc.Value));

            if (!CoopPrefs.Enabled.Value)
            {
                CoopLog.Msg(UIStrings.DisabledByPref);
                return;
            }

            _dispatcher = new MainThreadDispatcher();
            MelonEvents.OnUpdate.Subscribe(Tick);

            HarmonyProbe.Apply(CoopPrefs.Probe.Value);
            CompatInventory.Dump();

            if (CoopPrefs.Ipc.Value)
            {
                _ipc = new IpcServer(_dispatcher);
                _ipc.Start();
            }
        }

        public override void OnDeinitializeMelon()
        {
            if (_ipc != null) _ipc.Stop();
            HarmonyProbe.Uninstall();
        }

        private void Tick()
        {
            if (_dispatcher == null) return;

            if (!CoopPrefs.Enabled.Value)
            {
                HarmonyProbe.Uninstall();
                return;
            }

            _dispatcher.Tick(4.0);

            // probe เปิด/ปิดได้ด้วย pref ระหว่างรัน
            if (CoopPrefs.Probe.Value != HarmonyProbe.Installed)
            {
                HarmonyProbe.Apply(CoopPrefs.Probe.Value);
            }

            _ticks++;
            if (!_threadReported && HarmonyProbe.Hits > 0)
            {
                _threadReported = true;
                ReportThreads();
            }

            if (_ticks % 600 == 0)
            {
                CoopLog.Msg(string.Format(UIStrings.Heartbeat, _ticks, HarmonyProbe.Hits,
                            _dispatcher.Queued, _dispatcher.Ran, _dispatcher.Errors,
                            Math.Round(_dispatcher.Percentile(0.99), 3)));
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
