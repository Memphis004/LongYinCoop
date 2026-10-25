using System;
using System.Globalization;
using LongYinCoop.S2.Guest;
using LongYinCoop.S2.Protocol;
using MelonLoader;
using UnityEngine;

namespace LongYinCoop.S2
{
    /// <summary>
    /// W3 (แผน S2a-4 §4): guest overlay IMGUI **ในโปรเซสเดียวกับ host**
    ///
    /// - `Tick()` (main thread, เรียกจาก CoopMod.Tick หลัง `S2HostService.Tick`):
    ///   อ่าน key `overlay` จาก net.cfg (poll ≤1 s ของ S2NetConfig — reload ไม่ต้องรีสตาร์ตเกม)
    ///   แล้วเปิด/ปิด `S2OverlayClient` ที่ต่อกลับเข้า listener ของตัวเอง (เส้นทางเดียวกับแขกจริง)
    /// - `OnGUI`: **วาดอย่างเดียว** — GUI.Box/GUI.Label เท่านั้น ไม่มี interactive control
    ///   จึงไม่กิน input ของเกม (STAGEPLAN §5) · ตรรกะเลือกเฟรมอยู่ใน `S2OverlayRenderModel` (pure)
    /// - string ทั้งหมดมาจาก `UIStrings`
    /// </summary>
    internal sealed class S2OverlayService
    {
        private readonly S2HostService _host;
        private S2OverlayClient _client;
        private int _clientPort;
        private bool _clientRunning;
        private bool _subscribed;

        public S2OverlayService(S2HostService host)
        {
            _host = host;
            try
            {
                MelonEvents.OnGUI.Subscribe(OnGui);
                _subscribed = true;
            }
            catch (Exception e)
            {
                CoopLog.Warn("[S2] overlay: OnGUI subscribe failed: " + e.GetType().Name);
            }
        }

        /// <summary>main thread only — เปิด/ปิด client ตาม net.cfg + สถานะ listener ของ host</summary>
        public void Tick()
        {
            S2NetSettings settings = _host.Config.Current;
            bool want = settings.Overlay && settings.Enabled && settings.IsValid
                        && settings.Role == "host" && settings.Transport == "tcp"
                        && _host.TcpListening && _host.ActiveTcpPort > 0;

            if (want)
            {
                int port = _host.ActiveTcpPort;
                if (!_clientRunning || _clientPort != port)
                {
                    StopClient();
                    byte[] secret = S2Auth.BytesFromHex(settings.Secret);
                    if (secret == null || secret.Length == 0)
                    {
                        CoopLog.Warn("[S2] overlay: secret unusable - cannot start");
                        return;
                    }
                    _client = new S2OverlayClient(settings.Bind, port, secret, S2Fields.Hash);
                    _client.Start();
                    _clientPort = port;
                    _clientRunning = true;
                    CoopLog.Msg(UIStrings.OverlayStarted);
                }
            }
            else if (_clientRunning)
            {
                StopClient();
                CoopLog.Msg(UIStrings.OverlayStopped);
            }
        }

        public void Dispose()
        {
            StopClient();
            if (_subscribed)
            {
                try { MelonEvents.OnGUI.Unsubscribe(OnGui); } catch { }
                _subscribed = false;
            }
        }

        private void StopClient()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
            _clientRunning = false;
            _clientPort = 0;
        }

        private void OnGui()
        {
            try
            {
                Draw();
            }
            catch (Exception e)
            {
                CoopLog.Warn("[S2] overlay draw failed: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private void Draw()
        {
            S2OverlayClient client = _client;
            if (client == null) return;

            GuestTimedSnapshot[] ring = client.CopyRing();
            if (!S2OverlayRenderModel.ShouldRender(_host.Config.Current.Overlay, ring.Length))
            {
                return; // overlay=false → ไม่วาดอะไรเลย
            }

            if (ring.Length == 0)
            {
                GUI.Box(new Rect(16f, 16f, 380f, 28f), UIStrings.OverlayWaiting);
                return;
            }

            S2OverlayClientStats stats = client.SnapshotStats();
            S2OverlaySelection selection = S2OverlayRenderModel.Select(ring, Environment.TickCount64);
            GuestSnapshotDisplay d = selection.Snapshot.Display;

            string offset = stats.HasOffset
                ? (stats.OffsetUsAtMinRtt / 1000L).ToString(CultureInfo.InvariantCulture)
                : "-";
            string status = string.Format(CultureInfo.InvariantCulture, UIStrings.OverlayStatusArgs,
                stats.LastSeq, selection.AgeMs, selection.Stale ? UIStrings.OverlayStaleTag : "",
                stats.Gaps, stats.ResyncsSent, stats.ResyncAcks, stats.DivAMismatch, offset);

            float x = 16f;
            float y = 16f;
            float w = 380f;
            GUI.Box(new Rect(x, y, w, 28f), status);
            y += 32f;

            string header = S2OverlayText.SnapshotLine(d) + (d.Resync ? " [resync]" : "");
            GUI.Box(new Rect(x, y, w, 26f + 18f * d.Units.Length), "");
            GUI.Label(new Rect(x + 8f, y + 4f, w - 16f, 20f), header);
            y += 26f;
            for (int i = 0; i < d.Units.Length; i++)
            {
                GUI.Label(new Rect(x + 8f, y, w - 16f, 18f), S2OverlayText.UnitLine(d.Units[i]));
                y += 18f;
            }
        }
    }
}
