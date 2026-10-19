using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace LongYinCoop
{
    /// <summary>
    /// named pipe อ่านอย่างเดียว: \\.\pipe\longyin-coop-&lt;pid&gt;
    /// ทุกคำสั่งที่แตะ IL2CPP วิ่งผ่าน MainThreadDispatcher เท่านั้น (typed interop, ไม่มี raw offset)
    /// </summary>
    internal sealed class IpcServer
    {
        private const int MaxFrame = 1024 * 1024;

        private readonly MainThreadDispatcher _dispatcher;
        private readonly string _pipeName;
        private volatile bool _running;
        private Thread _thread;

        public IpcServer(MainThreadDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _pipeName = "longyin-coop-" + Environment.ProcessId;
        }

        public string PipeName { get { return _pipeName; } }

        public void Start()
        {
            _running = true;
            _thread = new Thread(Loop);
            _thread.IsBackground = true;
            _thread.Name = "LongYinCoop.Ipc";
            _thread.Start();
            CoopLog.Msg(UIStrings.IpcListening + " \\\\.\\pipe\\" + _pipeName);
        }

        public void Stop()
        {
            _running = false;
        }

        private void Loop()
        {
            NamedPipeServerStream active = CreateServer();
            while (_running)
            {
                try
                {
                    active.WaitForConnection();
                    CoopLog.Msg(UIStrings.IpcClientConnected);

                    // สร้าง instance ถัดไปทันที เพื่อไม่ให้มีช่วง "pipe busy" ระหว่าง client
                    NamedPipeServerStream next = CreateServer();
                    try
                    {
                        Handle(active);
                    }
                    finally
                    {
                        try { active.Dispose(); } catch { }
                        active = next;
                    }
                    CoopLog.Msg(UIStrings.IpcClientDisconnected);
                }
                catch (Exception e)
                {
                    if (_running) CoopLog.Error(e);
                    try { active.Dispose(); } catch { }
                    Thread.Sleep(100);
                    try { active = CreateServer(); }
                    catch (Exception e2) { CoopLog.Error(e2); }
                }
            }
        }

        private NamedPipeServerStream CreateServer()
        {
            return new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 2,
                                             PipeTransmissionMode.Byte, PipeOptions.None);
        }

        private void Handle(Stream stream)
        {
            byte[] header = new byte[4];
            while (_running)
            {
                if (!ReadExact(stream, header, 4)) return;
                int len = BitConverter.ToInt32(header, 0);
                if (len <= 0 || len > MaxFrame)
                {
                    CoopLog.Warn(UIStrings.IpcBadFrame + ": " + len);
                    return;
                }

                byte[] body = new byte[len];
                if (!ReadExact(stream, body, len)) return;

                string response = Process(Encoding.UTF8.GetString(body));
                byte[] outBody = Encoding.UTF8.GetBytes(response);
                byte[] outHeader = BitConverter.GetBytes(outBody.Length);
                stream.Write(outHeader, 0, 4);
                stream.Write(outBody, 0, outBody.Length);
                stream.Flush();
            }
        }

        private string Process(string json)
        {
            string cmd = null;
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement el;
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("cmd", out el) &&
                        el.ValueKind == JsonValueKind.String)
                    {
                        cmd = el.GetString();
                    }
                }
            }
            catch (Exception e)
            {
                return Fail(null, "bad json: " + e.Message);
            }

            if (string.IsNullOrEmpty(cmd)) return Fail(null, "missing cmd");

            try
            {
                switch (cmd)
                {
                    case "ping":
                        return Ok(cmd, new
                        {
                            pong = true,
                            pid = Environment.ProcessId,
                            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        });

                    case "get_version":
                        return OnMain(cmd, delegate
                        {
                            return (object)new
                            {
                                modVersion = CoopMod.Version,
                                stage = CoopMod.Stage,
                                protocol = 1,
                                unityVersion = UnityEngine.Application.unityVersion,
                                gameVersion = UnityEngine.Application.version,
                                productName = UnityEngine.Application.productName,
                                enabled = CoopPrefs.Enabled.Value,
                                probeEnabled = CoopPrefs.Probe.Value,
                                probeInstalled = HarmonyProbe.Installed,
                                probeHits = HarmonyProbe.Hits,
                                probeThreadId = HarmonyProbe.ProbeThreadId,
                                onUpdateThreadId = _dispatcher.OnUpdateThreadId,
                                pipeName = _pipeName,
                                logPath = CoopLog.LogPath
                            };
                        });

                    case "get_scene":
                        return OnMain(cmd, delegate
                        {
                            return (object)new { scene = SnapshotReader.GetSceneName() };
                        });

                    case "get_player_pos":
                        return OnMain(cmd, delegate
                        {
                            PlayerPos p = SnapshotReader.GetPlayerPos();
                            if (p == null) return (object)null;
                            return new { x = p.x, y = p.y, heroID = p.heroID };
                        });

                    case "stats":
                        return Ok(cmd, _dispatcher.Snapshot());

                    default:
                        return Fail(cmd, UIStrings.UnknownCommand);
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                return Fail(cmd, e.Message);
            }
        }

        private string OnMain(string cmd, Func<object> func)
        {
            object result;
            if (!_dispatcher.Invoke(func, 500, out result)) return Fail(cmd, UIStrings.MainThreadTimeout);
            if (result == null) return Ok(cmd, null, UIStrings.NoWorldMap);
            return Ok(cmd, result);
        }

        private static string Ok(string cmd, object result)
        {
            return Ok(cmd, result, null);
        }

        private static string Ok(string cmd, object result, string note)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "ok", true },
                { "cmd", cmd },
                { "result", result },
                { "note", note }
            });
        }

        private static string Fail(string cmd, string error)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "ok", false },
                { "cmd", cmd },
                { "error", error }
            });
        }

        private static bool ReadExact(Stream stream, byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0) return false;
                offset += read;
            }
            return true;
        }
    }
}
