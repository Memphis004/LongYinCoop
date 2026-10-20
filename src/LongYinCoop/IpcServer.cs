using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            CoopLog.Msg(string.Format(UIStrings.IpcListening, _pipeName));
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
                    CoopLog.Warn(string.Format(UIStrings.IpcBadFrame, len));
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
                    JsonElement root = doc.RootElement;
                    if (root.ValueKind != JsonValueKind.Object) return Fail(null, UIStrings.IpcMissingCmd);

                    cmd = GetString(root, "cmd");
                    if (string.IsNullOrEmpty(cmd)) return Fail(null, UIStrings.IpcMissingCmd);

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
                                    stageLabel = CoopMod.StageLabel,
                                    enabled = CoopPrefs.Enabled.Value,
                                    devCommands = CoopPrefs.DevCommands.Value,
                                    allowRemoteInput = CoopPrefs.AllowRemoteInput.Value,
                                    probeEnabled = CoopPrefs.Probe.Value,
                                    probeInstalled = HarmonyProbe.Installed,
                                    probeHits = HarmonyProbe.Hits,
                                    probeThreadId = HarmonyProbe.ProbeThreadId,
                                    onUpdateThreadId = _dispatcher.OnUpdateThreadId,
                                    captureEnabled = CoopPrefs.Capture.Value,
                                    captureActive = CaptureWriter.Active,
                                    captureDir = CaptureWriter.Dir,
                                    battleLogInstalled = BattleLogHook.Installed,
                                    battleLogPatches = BattleLogHook.PatchApplied,
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
                                return new { x = p.x, y = p.y, heroID = p.heroID, areaId = p.areaId };
                            });

                        case "stats":
                            return Ok(cmd, _dispatcher.Snapshot());

                        case "snapshot":
                            return Snapshot(cmd, root);

                        case "capture":
                            return Capture(cmd, root);

                        case "compat_dump":
                            return CompatDump(cmd, root);

                        case "probe_types":
                            return OnMain(cmd, delegate { return S1Probe.TypeReport(); });

                        case "patch_report":
                            return OnMain(cmd, delegate { return S1Probe.PatchReport(); });

                        case "probe_units":
                            return ProbeUnits(cmd);

                        case "s1_report":
                            return OnMain(cmd, delegate { return WriteS1Report(); });

                        case "spike":
                            return Spike(cmd, root);

                        case "set_pref":
                            return SetPref(cmd, root);

                        case "bench_dispatch":
                            return BenchDispatch(cmd, root);

                        default:
                            return Fail(cmd, UIStrings.UnknownCommand);
                    }
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                return Fail(cmd, e.Message);
            }
        }

        private string Snapshot(string cmd, JsonElement root)
        {
            string what = GetString(root, "what");
            switch (what)
            {
                case "world":
                    return OnMain(cmd, delegate
                    {
                        WorldSample s = WorldSampler.Latest;
                        if (s == null) return (object)new { available = false, ageMs = -1L };
                        return new { available = true, ageMs = WorldSampler.LatestAgeMs, sample = s };
                    });

                case "battle":
                    return OnMain(cmd, delegate
                    {
                        BattleView v = BattleSampler.LatestView();
                        if (v == null) return (object)new { available = false };
                        return new { available = true, view = v };
                    });

                case "log":
                    {
                        int n = GetInt(root, "n", 50);
                        if (n < 1) n = 1;
                        if (n > 500) n = 500;
                        return OnMain(cmd, delegate
                        {
                            List<LogEvent> events = BattleLogHook.Log.Last(n);
                            return (object)new { count = events.Count, events = events };
                        });
                    }

                default:
                    return Fail(cmd, UIStrings.UnknownCommand);
            }
        }

        private string Capture(string cmd, JsonElement root)
        {
            string action = GetString(root, "action");
            if (action == "start")
            {
                string dir = CaptureWriter.Start();
                if (dir == null) return Fail(cmd, string.Format(UIStrings.CaptureStartFailed, ""));
                return Ok(cmd, new { active = CaptureWriter.Active, dir = dir });
            }
            if (action == "stop")
            {
                CaptureWriter.Stop();
                return Ok(cmd, new
                {
                    active = CaptureWriter.Active,
                    dir = CaptureWriter.Dir,
                    written = Interlocked.Read(ref CaptureWriter.Written),
                    dropped = Interlocked.Read(ref CaptureWriter.Dropped)
                });
            }
            if (action == "status")
            {
                return Ok(cmd, new
                {
                    active = CaptureWriter.Active,
                    enabled = CoopPrefs.Capture.Value,
                    dir = CaptureWriter.Dir,
                    written = Interlocked.Read(ref CaptureWriter.Written),
                    dropped = Interlocked.Read(ref CaptureWriter.Dropped)
                });
            }
            return Fail(cmd, UIStrings.UnknownCommand);
        }

        private string CompatDump(string cmd, JsonElement root)
        {
            string tag = GetString(root, "tag");
            return OnMain(cmd, delegate
            {
                string path = CompatInventory.Dump(tag);
                return (object)new { path = path };
            });
        }

        private string ProbeUnits(string cmd)
        {
            return OnMain(cmd, delegate
            {
                object probe = S1Probe.UnitProbe();
                if (probe == null) return (object)new { inBattle = false };
                return new { inBattle = true, probe = probe };
            });
        }

        private object WriteS1Report()
        {
            object snap = S1Metrics.Snapshot();
            string path = Path.Combine(CoopPaths.EnsureUserDataDir(), "s1-report.json");
            File.WriteAllText(path, JsonSerializer.Serialize(snap, S1Json.Options), Encoding.UTF8);
            CoopLog.Msg(string.Format(UIStrings.S1ReportWritten, path));
            return snap;
        }

        /// <summary>
        /// S1.5-b: ลงทะเบียนเฉพาะ `spike status` (อ่านอย่างเดียว)
        /// - ต้องมี coop.enabled + coop.ipc + coop.devCommands
        /// - ไม่ใช้ coop.allowRemoteInput (คง false) และไม่มี move/attack/pause/arm ใน build นี้
        /// </summary>
        private string Spike(string cmd, JsonElement root)
        {
            if (!CoopPrefs.Enabled.Value) return Fail(cmd, UIStrings.SpikeDisabled);
            if (!CoopPrefs.Ipc.Value) return Fail(cmd, UIStrings.SpikeIpcRequired);
            if (!CoopPrefs.DevCommands.Value) return Fail(cmd, UIStrings.DevCommandsDisabled);

            string sub = GetString(root, "sub");
            if (string.IsNullOrEmpty(sub)) return Fail(cmd, UIStrings.SpikeMissingSub);
            if (sub == "status") return OnMain(cmd, delegate { return TurnGate.ReadStatus(); });

            // S1.5-d: คำสั่งเปลี่ยน state — ต้องผ่าน panic switch (allowRemoteInput) + arm ทุกครั้ง
            if (sub == "arm")
            {
                if (!CoopPrefs.AllowRemoteInput.Value) return Fail(cmd, UIStrings.SpikeRemoteInputOff);
                int seconds = GetInt(root, "seconds", MoveCommand.DefaultArmSeconds);
                if (seconds < 1 || seconds > MoveCommand.MaxArmSeconds) return Fail(cmd, UIStrings.SpikeArmSecondsInvalid);
                return OnMain(cmd, delegate { return MoveCommand.Arm(seconds); });
            }
            if (sub == "disarm")
            {
                return OnMain(cmd, delegate { return MoveCommand.Disarm(); });
            }
            if (sub == "move")
            {
                if (!CoopPrefs.AllowRemoteInput.Value) return Fail(cmd, UIStrings.SpikeRemoteInputOff);
                string id = GetString(root, "id");
                int row = GetInt(root, "row", -1);
                int col = GetInt(root, "col", -1);
                if (string.IsNullOrEmpty(id)) return Fail(cmd, string.Format(UIStrings.SpikeMissingArgs, sub, "id"));
                if (row < 0 || col < 0) return Fail(cmd, string.Format(UIStrings.SpikeMissingArgs, sub, "row/col"));

                object raw = null;
                if (!_dispatcher.Invoke(delegate { return (object)MoveCommand.Move(id, row, col); }, 500, out raw))
                    return Fail(cmd, UIStrings.MainThreadTimeout);
                MoveCommand.MoveResult mr = raw as MoveCommand.MoveResult;
                if (mr == null) return Fail(cmd, UIStrings.SpikeMoveNotInBattle);
                if (!mr.Ok) return Fail(cmd, mr.Error ?? UIStrings.SpikeMoveNotInBattle);
                return Ok(cmd, mr);
            }

            return Fail(cmd, string.Format(UIStrings.SpikeNotRegistered, sub));
        }

        private string SetPref(string cmd, JsonElement root)
        {
            string key = GetString(root, "key");
            string value = GetString(root, "value");
            if (key == "capture")
            {
                bool on;
                if (!bool.TryParse(value, out on)) return Fail(cmd, UIStrings.UnknownCommand);
                return OnMain(cmd, delegate
                {
                    CoopPrefs.Capture.Value = on;
                    CoopLog.Msg(string.Format(UIStrings.PrefSet, key, on));
                    return (object)new { key = key, value = CoopPrefs.Capture.Value };
                });
            }
            // S1.5-d panic switch: false = คำสั่งเปลี่ยน state ทั้งหมดตอบ DISARMED ทันที
            if (key == "allowRemoteInput")
            {
                bool on;
                if (!bool.TryParse(value, out on)) return Fail(cmd, UIStrings.UnknownCommand);
                return OnMain(cmd, delegate
                {
                    CoopPrefs.AllowRemoteInput.Value = on;
                    if (!on) MoveCommand.Disarm();
                    CoopLog.Msg(string.Format(UIStrings.PrefSet, key, on));
                    return (object)new
                    {
                        key = key,
                        value = CoopPrefs.AllowRemoteInput.Value,
                        armed = MoveCommand.IsArmed
                    };
                });
            }
            // S1.5-c W1: เปิด/ปิด probe hook (postfix อ่านอย่างเดียว) — ไม่แตะ state เกม
            if (key == "jitprobe")
            {
                bool on;
                if (!bool.TryParse(value, out on)) return Fail(cmd, UIStrings.UnknownCommand);
                return OnMain(cmd, delegate
                {
                    CoopPrefs.JitProbe.Value = on;
                    // ทำทันทีบน main thread เพื่อให้ผลลัพธ์ที่ตอบกลับตรงกับสถานะจริง
                    JitProbe.Apply(on);
                    CoopLog.Msg(string.Format(UIStrings.PrefSet, key, on));
                    return (object)new
                    {
                        key = key,
                        value = CoopPrefs.JitProbe.Value,
                        installed = JitProbe.Installed,
                        file = JitProbe.FilePath,
                        onClickHits = JitProbe.OnClickHits,
                        battleGridClickedHits = JitProbe.GridClickedHits,
                        dropped = JitProbe.Dropped
                    };
                });
            }
            // S1.5-c W2: postfix ฝั่ง move — อ่านอย่างเดียว, unpatch ได้ทั้ง wave
            if (key == "jitprobeW2")
            {
                bool on;
                if (!bool.TryParse(value, out on)) return Fail(cmd, UIStrings.UnknownCommand);
                return OnMain(cmd, delegate
                {
                    CoopPrefs.JitProbeW2.Value = on;
                    JitProbeW2.Apply(on);
                    CoopLog.Msg(string.Format(UIStrings.PrefSet, key, on));
                    return (object)new
                    {
                        key = key,
                        value = CoopPrefs.JitProbeW2.Value,
                        installed = JitProbeW2.Installed,
                        file = JitProbeW2.FilePath,
                        hits = JitProbeW2.Hits,
                        missing = JitProbeW2.Missing,
                        dropped = JitProbeW2.Dropped
                    };
                });
            }
            // S1.5-c W4: postfix บน BattlePauseButtonClicked (อ่าน timeScale/battleTime ด้วย)
            if (key == "jitprobeW4")
            {
                bool on;
                if (!bool.TryParse(value, out on)) return Fail(cmd, UIStrings.UnknownCommand);
                return OnMain(cmd, delegate
                {
                    CoopPrefs.JitProbeW4.Value = on;
                    JitProbeW4.Apply(on);
                    CoopLog.Msg(string.Format(UIStrings.PrefSet, key, on));
                    return (object)new
                    {
                        key = key,
                        value = CoopPrefs.JitProbeW4.Value,
                        installed = JitProbeW4.Installed,
                        file = JitProbeW4.FilePath,
                        hits = JitProbeW4.Hits,
                        missing = JitProbeW4.Missing,
                        dropped = JitProbeW4.Dropped
                    };
                });
            }
            // S1.5-c W6: probe อ่าน chain ของ move (state ก่อน/หลัง + ตัวตน GameObject + stack) — default false
            if (key == "jitprobeW6")
            {
                bool on;
                if (!bool.TryParse(value, out on)) return Fail(cmd, UIStrings.UnknownCommand);
                return OnMain(cmd, delegate
                {
                    CoopPrefs.JitProbeW6.Value = on;
                    JitProbeW6.Apply(on);
                    CoopLog.Msg(string.Format(UIStrings.PrefSet, key, on));
                    return (object)new
                    {
                        key = key,
                        value = CoopPrefs.JitProbeW6.Value,
                        installed = JitProbeW6.Installed,
                        file = JitProbeW6.FilePath,
                        hits = JitProbeW6.Hits,
                        prefixHits = JitProbeW6.PrefixHits,
                        stacks = JitProbeW6.StacksLogged,
                        missing = JitProbeW6.Missing,
                        dropped = JitProbeW6.Dropped
                    };
                });
            }
            return Fail(cmd, string.Format(UIStrings.PrefUnknown, key));
        }

        /// <summary>dev-only: enqueue N no-op แล้วรอ drain — วัดเกณฑ์ dispatcher เท่านั้น</summary>
        private string BenchDispatch(string cmd, JsonElement root)
        {
            if (!CoopPrefs.DevCommands.Value) return Fail(cmd, UIStrings.DevCommandsDisabled);

            int n = GetInt(root, "n", 1000);
            if (n < 1) n = 1;
            if (n > 10000) n = 10000;

            long before = Interlocked.Read(ref _dispatcher.Ran);
            long queuedBefore = Interlocked.Read(ref _dispatcher.Queued);
            for (int i = 0; i < n; i++) _dispatcher.Enqueue(delegate { });

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 5000 && Interlocked.Read(ref _dispatcher.Ran) - before < n)
            {
                Thread.Sleep(1);
            }

            long ran = Interlocked.Read(ref _dispatcher.Ran) - before;
            long queued = Interlocked.Read(ref _dispatcher.Queued) - queuedBefore;
            return Ok(cmd, new
            {
                requested = n,
                queued = queued,
                ran = ran,
                dropped = queued - ran,
                elapsedMs = sw.ElapsedMilliseconds,
                stats = _dispatcher.Snapshot()
            });
        }

        private string OnMain(string cmd, Func<object> func)
        {
            object result;
            if (!_dispatcher.Invoke(func, 500, out result)) return Fail(cmd, UIStrings.MainThreadTimeout);
            if (result == null) return Ok(cmd, null, UIStrings.NoWorldMap);
            return Ok(cmd, result);
        }

        private static string GetString(JsonElement root, string name)
        {
            JsonElement el;
            if (root.TryGetProperty(name, out el) && el.ValueKind == JsonValueKind.String) return el.GetString();
            return null;
        }

        private static int GetInt(JsonElement root, string name, int fallback)
        {
            JsonElement el;
            if (root.TryGetProperty(name, out el) && el.ValueKind == JsonValueKind.Number)
            {
                int v;
                if (el.TryGetInt32(out v)) return v;
            }
            return fallback;
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
            }, S1Json.Options);
        }

        private static string Fail(string cmd, string error)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object>
            {
                { "ok", false },
                { "cmd", cmd },
                { "error", error }
            }, S1Json.Options);
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
