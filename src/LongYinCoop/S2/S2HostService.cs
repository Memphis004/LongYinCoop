using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using LongYinCoop.S2.Host;
using LongYinCoop.S2.Protocol;
using LongYinCoop.S2.Transport;

namespace LongYinCoop.S2
{
    /// <summary>
    /// S2a-3 host service (spectator, **ไม่มี write path** ไปยัง state ของเกม)
    ///
    /// ลำดับใน 1 tick (เรียกจาก CoopMod.Tick() = Unity main thread เท่านั้น):
    ///   1) poll `net.cfg` (UserData\LongYinCoop — ไฟล์ของเราเอง, MelonLoader ไม่แตะ) ≤1 Hz
    ///   2) นับ frame time 1 ค่า/เฟรม (`S2FrameMeter`) + เขียน marker ทุก 60 s (X5)
    ///   3) ถ้ามี connection ใหม่ (tcp): ส่ง Hello → รอ Auth (handshake ครั้งแรกของ S2)
    ///   4) ถ้า authenticated: sample state → POCO (IL2CPP บน main thread) → post ให้ sender thread
    ///   5) drain ช่อง guest→host ผ่าน allowlist → handshake / control (Ping/Resync/Bye)
    ///   6) ถ้า control บอกว่าต้อง resync → ตั้งธงให้ snapshot ใบถัดไป (ไม่มี write ไปเกม)
    ///
    /// `commandsAccepted` ต้องเป็น 0 เสมอ: ไม่มีชนิดคำสั่งเกมในสารบบ และไม่มี path เขียน state
    /// </summary>
    internal sealed class S2HostService
    {
        public const string FileSnapshots = "s2-snapshots.ndjson";
        public const string PhaseA = "A";
        public const string PhaseB = "B";

        private const int FramemeterWindowMs = 60000;
        private const int GuestIdleMs = 60000;
        private const int MaxProtocolViolations = 3;
        private const int CloseDelayMs = 250;
        private const int InboxDrainGuard = 256;

        private readonly S2NetConfig _config;
        private readonly S2Counters _counters = new S2Counters();
        private readonly S2Metrics _metrics = new S2Metrics();
        private readonly S2Sampler _sampler = new S2Sampler();
        private readonly S2PhaseStats _phaseSample = new S2PhaseStats("sampleMs");
        private readonly S2GuestGate _gate;
        private readonly S2FrameMeter _frameMeter = new S2FrameMeter();
        private readonly ConcurrentQueue<S2TcpSelfTest.Result> _selfTestResults =
            new ConcurrentQueue<S2TcpSelfTest.Result>();

        private LoopbackTransport _loopHost;
        private LoopbackTransport _loopGuest;
        private TcpTransport _tcpHost;
        private S2HandshakeServer _handshake;
        private S2ControlHandler _control;
        private S2SerializingSender _sender;
        private S2NdjsonWriter _writer;

        private bool _active;
        private bool _authenticated;
        private bool _selfTestDone;
        private bool _selfTestReported;
        private bool _guardWarned;
        private bool _haveSample;
        private bool _framemeterReady = true;
        private string _mode = "none";
        private uint _sessionCounter;
        private uint _sessionId;
        private long _pendingCloseAtMs = -1;
        private long _lastGuestTrafficMs;
        private int _violations;
        private long _framemeterWindowStartMs;
        private string _phase = PhaseA;
        private string _lastReason = "init: still reading net.cfg";
        private long _phaseNumber;
        private long _phaseStartMs;
        private long _phaseFrames;
        private long _phaseChanges;
        private long _auditChanges;
        private long _ticksActive;
        private long _droppedPosts;
        private long _peerConsumed;
        private S2Audit _lastAudit;
        private bool _hasAudit;

        public S2HostService(string cfgPath, string evidenceDir)
        {
            _config = new S2NetConfig(cfgPath);
            EvidenceDir = string.IsNullOrEmpty(evidenceDir) ? "S2" : evidenceDir;
            SnapshotPath = Path.Combine(EvidenceDir, FileSnapshots);
            _gate = new S2GuestGate(_counters);
        }

        public string EvidenceDir { get; private set; }

        public string SnapshotPath { get; private set; }

        public bool Active { get { return _active; } }

        public bool Authenticated { get { return _authenticated; } }

        public S2Counters Counters { get { return _counters; } }

        public S2NetConfig Config { get { return _config; } }

        /// <summary>main thread only</summary>
        public void Tick()
        {
            long nowMs = Environment.TickCount64;

            if (_config.Poll(nowMs))
            {
                ApplySettings(_config.Current, nowMs);
            }

            SampleFrameTime();
            FramemeterTick(nowMs);

            if (_active && _tcpHost != null) HandleNewConnection(nowMs);
            if (_pendingCloseAtMs >= 0 && nowMs >= _pendingCloseAtMs) DoPendingClose();

            if (_active)
            {
                _ticksActive++;
                if (_sender != null && (_loopHost != null || _authenticated))
                {
                    S2Frame frame = _sampler.Read();
                    if (frame != null)
                    {
                        _phaseFrames++;
                        _phaseSample.Add(frame.SampleMs);
                        _haveSample = true;
                        if (!_sender.TryPost(frame)) _droppedPosts++;
                        NoteAudit(frame, nowMs);
                    }
                }

                DrainHostInbox(nowMs);
                DrainGuestInbox();
                DetectClosedConnection(nowMs);
                ReportSelfTest();
                CheckIdle(nowMs);
                LogTicker();
            }
        }

        public void Stop()
        {
            if (_active) Deactivate(_config.Current, Environment.TickCount64);
            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }
        }

        public object Status()
        {
            return new
            {
                active = _active,
                authenticated = _authenticated,
                transport = _mode,
                sessionId = _sessionId,
                phase = _phase,
                phaseNumber = _phaseNumber,
                phaseChanges = _phaseChanges,
                auditChanges = _auditChanges,
                reason = _lastReason,
                ticksActive = _ticksActive,
                droppedPosts = _droppedPosts,
                peerConsumed = _peerConsumed,
                violations = _violations,
                snapshotPath = SnapshotPath,
                samplerFrames = _sampler.Frames,
                samplerEmptyReads = _sampler.EmptyReads,
                samplerErrors = _sampler.Errors,
                samplerLastError = _sampler.LastError,
                battleKey = _sampler.LastBattleKey,
                phaseSample = _phaseSample.Snapshot(),
                metrics = _metrics.Snapshot(),
                counters = _counters.Snapshot(),
                framemeter = _frameMeter.Snapshot(),
                handshake = _handshake == null ? null : (object)new
                {
                    accepted = _handshake.Accepted,
                    denied = _handshake.Denied,
                    expired = _handshake.Expired,
                    fieldHashPrefix = _handshake.FieldHashPrefix,
                },
                transportStats = _tcpHost == null ? null : (object)new
                {
                    listen = _tcpHost.Listening,
                    connected = _tcpHost.Connected,
                    framesSent = _tcpHost.Stats.FramesSent,
                    bytesSent = _tcpHost.Stats.BytesSent,
                    framesReceived = _tcpHost.Stats.FramesReceived,
                    bytesReceived = _tcpHost.Stats.BytesReceived,
                    sendDrops = _tcpHost.Stats.SendDrops,
                    receiveDrops = _tcpHost.Stats.ReceiveDrops,
                    rejected = _tcpHost.Stats.RejectedConnections,
                    lastError = _tcpHost.LastError,
                },
                config = _config.Current.Describe(),
                configReads = _config.PollCount,
            };
        }

        // ------------------------------------------------------------------ activation

        private void ApplySettings(S2NetSettings settings, long nowMs)
        {
            bool want = settings.Enabled && settings.IsValid && settings.Role == "host"
                        && GuardWritePathsClosed(warn: true);
            if (want == _active)
            {
                if (!want) _lastReason = DescribeIdle(settings);
                return;
            }

            if (want) Activate(settings, nowMs); else Deactivate(settings, nowMs);
        }

        /// <summary>
        /// S2a-2 rev4 (มติผู้ใช้ 2026-10-21 ก): ต้องรันตอนที่โมดไม่มี write path ไปถึง state เกมเปิดอยู่
        /// — ตรรกะอยู่ใน <see cref="S2WriteGuard"/> (pure, เทสต์ครบ 64 ชุดค่าผสม)
        /// `DevCommands` **ไม่** ถูกตรวจ (ต้องเปิดไว้ให้ `spike status` ทำงาน; คำสั่งที่เขียนเกมยังถูกกัน
        /// ด้วย `allowRemoteInput` ที่ `IpcServer.Spike` เอง)
        /// </summary>
        private bool GuardWritePathsClosed(bool warn)
        {
            if (S2WriteGuard.WritePathsClosed(CoopPrefs.AllowRemoteInput.Value, CoopPrefs.JitProbe.Value,
                                              CoopPrefs.JitProbeW2.Value, CoopPrefs.JitProbeW4.Value,
                                              CoopPrefs.JitProbeW6.Value, CoopPrefs.JitProbeW7.Value))
                return true;
            if (warn && !_guardWarned)
            {
                _guardWarned = true;
                CoopLog.Warn("[S2] idle: write path open (" + OpenWritePaths() + ") - close every write path"
                             + " and set enabled = true in UserData\\LongYinCoop\\net.cfg");
            }
            return false;
        }

        private static string OpenWritePaths()
        {
            return S2WriteGuard.Describe(CoopPrefs.AllowRemoteInput.Value, CoopPrefs.JitProbe.Value,
                                         CoopPrefs.JitProbeW2.Value, CoopPrefs.JitProbeW4.Value,
                                         CoopPrefs.JitProbeW6.Value, CoopPrefs.JitProbeW7.Value);
        }

        private static string DescribeIdle(S2NetSettings settings)
        {
            if (!settings.FileRead) return "idle: net.cfg unreadable (" + (settings.Error ?? "?") + ")";
            if (!settings.FormatOk) return "idle: net.cfg invalid: " + (settings.Error ?? "?");
            if (!settings.Enabled) return "idle: net.cfg enabled=false (phase A baseline)";
            if (!string.IsNullOrEmpty(settings.Error)) return "idle: " + settings.Error;
            if (settings.Role != "host") return "idle: net.cfg role=" + (settings.Role ?? "-") + " (host side only)";
            if (CoopPrefs.AllowRemoteInput.Value || CoopPrefs.JitProbe.Value || CoopPrefs.JitProbeW2.Value
                || CoopPrefs.JitProbeW4.Value || CoopPrefs.JitProbeW6.Value || CoopPrefs.JitProbeW7.Value)
                return "idle: write path open (" + OpenWritePaths() + ") - must be closed for S2";
            return "idle: unsupported settings";
        }

        private void Activate(S2NetSettings settings, long nowMs)
        {
            EnsureWriter();

            if (settings.Transport == "tcp")
            {
                if (!ActivateTcp(settings, nowMs)) return;
            }
            else
            {
                _mode = "loopback";
                LoopbackTransport.CreatePair(out _loopHost, out _loopGuest);
                _sessionId = unchecked(++_sessionCounter);
                StartSender(settings, _loopHost);
                WriteMarker("listen.start", "transport=loopback sessionId=" + _sessionId);
            }

            _control = new S2ControlHandler(_counters);
            _authenticated = _loopHost != null;
            _active = true;
            _phaseNumber++;
            _phase = PhaseB;
            _phaseStartMs = nowMs;
            _phaseFrames = 0;
            _phaseSample.Reset();
            _lastReason = "active: net.cfg enabled=true role=host transport=" + _mode;
            WriteMarker("phase.start", "block=" + _phase + " blockIndex=" + _phaseNumber
                        + " s2Enabled=true role=host transport=" + _mode + " sendHz=" + settings.SendHz
                        + " heartbeatMs=" + settings.HeartbeatMs + " protoVer=" + S2Protocol.ProtoVer);
            CoopLog.Msg("[S2] S2a-3 ACTIVE transport=" + _mode + " -> " + SnapshotPath);

            // exit 6 (X8): self-test ในเฟรมเดียว (audit tuple เทียบได้จริง) — ไม่ต้องมี socket
            RunLoopbackSelfTest(nowMs);
        }

        private bool ActivateTcp(S2NetSettings settings, long nowMs)
        {
            BindVerdict verdict = BindPolicy.Validate(settings.Bind, settings.AllowNonLoopback);
            if (verdict != BindVerdict.Ok)
            {
                _lastReason = "idle: bind rejected (" + BindPolicy.Describe(verdict) + ")";
                WriteMarker("activate.rejected", "reason=" + Sanitize(_lastReason)
                            + " bind=" + Sanitize(settings.Bind));
                CoopLog.Warn("[S2] " + _lastReason);
                return false;
            }

            string error;
            TcpTransport tcp = TcpTransport.Listen(settings.Bind, settings.Port, out error);
            if (tcp == null)
            {
                _lastReason = "idle: listen failed (" + error + ") bind=" + settings.Bind;
                WriteMarker("activate.rejected", "reason=" + Sanitize(_lastReason));
                CoopLog.Warn("[S2] " + _lastReason);
                return false;
            }

            tcp.BusyFrame = FrameCodec.Encode(MessageType.Deny, S2Protocol.ProtoVer, 0,
                                              HandshakeCodec.EncodeDeny(DenyCode.Busy, "another guest is connected"));
            _tcpHost = tcp;
            _mode = "tcp";
            _handshake = new S2HandshakeServer(S2Auth.BytesFromHex(settings.Secret), S2Fields.Hash,
                                               S2HandshakeServer.DefaultTimeoutMs);
            _authenticated = false;
            _sender = null;
            _sessionId = 0;

            WriteMarker("listen.start", "transport=tcp bind=" + Sanitize(settings.Bind)
                        + " port=" + tcp.Port + " fieldHashPrefix=" + S2Fields.Prefix
                        + " protoVer=" + S2Protocol.ProtoVer);
            CoopLog.Msg("[S2] listening on " + tcp.Describe + " (loopback only, fieldHash=" + S2Fields.Prefix + ")");

            if (!_selfTestDone)
            {
                _selfTestDone = true;
                StartTcpSelfTest(settings, tcp.Port);
            }
            return true;
        }

        private void StartTcpSelfTest(S2NetSettings settings, int port)
        {
            try
            {
                S2TcpSelfTest.StartInBackground(_counters, settings.Bind, port,
                                                S2Auth.BytesFromHex(settings.Secret), S2Fields.Hash,
                                                delegate (S2TcpSelfTest.Result r) { _selfTestResults.Enqueue(r); });
            }
            catch (Exception e)
            {
                CoopLog.Warn("[S2] tcp self-test could not start: " + e.GetType().Name);
            }
        }

        private void StartSender(S2NetSettings settings, ITransport transport)
        {
            _sender = new S2SerializingSender(transport, _counters, _metrics, _writer,
                new S2SenderOptions
                {
                    QueueCapacity = 2,
                    SendHz = settings.SendHz,
                    HeartbeatMs = settings.HeartbeatMs,
                    Dedup = true,
                    SessionId = _sessionId,
                });
        }

        private void StopSender()
        {
            if (_sender == null) return;
            _sender.Stop();
            _sender = null;
        }

        private void Deactivate(S2NetSettings settings, long nowMs)
        {
            WritePhaseEnd(nowMs);
            StopSender();
            if (_tcpHost != null)
            {
                _tcpHost.Close();
                _tcpHost = null;
            }
            if (_loopHost != null)
            {
                _loopHost.Close();
                _loopGuest.Close();
                _loopHost = null;
                _loopGuest = null;
            }
            _handshake = null;
            _control = null;
            _authenticated = false;
            _mode = "none";
            _pendingCloseAtMs = -1;

            _active = false;
            _phase = PhaseA;
            _phaseChanges++;
            _lastReason = DescribeIdle(settings);
            WriteMarker("phase.idle", "block=" + _phase + " s2Enabled=false role=none transport=none reason="
                        + Sanitize(_lastReason));
            CoopLog.Msg("[S2] SNAPSHOT SERVICE IDLE (" + _lastReason + ") -> block " + _phase);
        }

        // ------------------------------------------------------------------ tcp connection + handshake

        private void HandleNewConnection(long nowMs)
        {
            if (!_tcpHost.TakeNewConnection()) return;

            _counters.ConnectionsOpened++;
            _violations = 0;
            _lastGuestTrafficMs = nowMs;
            _authenticated = false;
            StopSender();
            _control?.Reset();

            if (_handshake == null || !_handshake.AllowsNewConnection(nowMs))
            {
                _counters.AuthRateLimited++;
                WriteMarker("handshake.rateLimited", "reason=cooldown after failed auth at=" + nowMs);
                _tcpHost.RequestClose();
                return;
            }

            _handshake.NewConnection();
            _sessionId = unchecked(++_sessionCounter);
            byte[] hello = _handshake.BuildHello(_sessionId, nowMs);
            if (!_tcpHost.Send(hello))
            {
                WriteMarker("handshake.helloFailed", "reason=send queue full at=" + nowMs);
                _tcpHost.RequestClose();
                return;
            }
            WriteMarker("handshake.hello", "sessionId=" + _sessionId + " protoVer=" + S2Protocol.ProtoVer
                        + " fieldHashPrefix=" + S2Fields.Prefix + " at=" + nowMs);
        }

        private void DoPendingClose()
        {
            _pendingCloseAtMs = -1;
            if (_tcpHost != null) _tcpHost.RequestClose();
        }

        private void ScheduleClose(long delayMs)
        {
            _pendingCloseAtMs = Environment.TickCount64 + (delayMs < 0 ? 0 : delayMs);
        }

        private void DetectClosedConnection(long nowMs)
        {
            if (_tcpHost == null || !_authenticated || _tcpHost.Connected) return;
            _authenticated = false;
            _counters.ConnectionsClosed++;
            StopSender();
            _control?.Reset();
            WriteMarker("session.close", "reason=peer-closed sessionId=" + _sessionId + " at=" + nowMs);
        }

        private void CheckIdle(long nowMs)
        {
            if (_tcpHost == null || !_authenticated) return;
            if (_lastGuestTrafficMs == 0) return;
            if (nowMs - _lastGuestTrafficMs <= GuestIdleMs) return;
            WriteMarker("session.close", "reason=guest-idle sessionId=" + _sessionId + " at=" + nowMs);
            ScheduleClose(0);
        }

        // ------------------------------------------------------------------ inbox

        /// <summary>
        /// ฝั่ง guest→host: รับ frame จาก peer แล้วผ่าน allowlist
        /// ถ้าเป็น type ที่ไม่อยู่ในลิสต์ = ทิ้ง + นับ (ไม่มีทาง apply ได้ เพราะ S2 ไม่มี write path)
        /// </summary>
        private void DrainHostInbox(long nowMs)
        {
            ITransport transport = _tcpHost != null ? (ITransport)_tcpHost : _loopHost;
            if (transport == null) return;

            int guard = 0;
            while (guard++ < InboxDrainGuard && transport.TryReceive(out byte[] raw))
            {
                S2GateOutcome outcome = _gate.Accept(raw, nowMs, out AssembledMessage message, out string reason);
                if (outcome == S2GateOutcome.Await) continue;

                if (outcome == S2GateOutcome.Forbidden || outcome == S2GateOutcome.Malformed)
                {
                    _violations++;
                    _counters.ProtocolViolations++;
                    CoopLog.Warn("[S2] guest frame dropped: " + reason);
                    if (_violations >= MaxProtocolViolations) ScheduleClose(CloseDelayMs);
                    continue;
                }

                if (outcome == S2GateOutcome.Control) HandleControl(message, nowMs);
            }
        }

        private void HandleControl(in AssembledMessage message, long nowMs)
        {
            _lastGuestTrafficMs = nowMs;

            if (message.Type == MessageType.Auth)
            {
                if (_handshake == null) return;
                _counters.AuthAttempts++;

                byte[] reply;
                DenyCode code;
                string reason;
                HandshakeOutcome outcome = _handshake.AcceptAuth(message, nowMs, out reply, out code, out reason);
                if (reply != null) SendToGuest(reply);

                if (outcome == HandshakeOutcome.Accepted)
                {
                    _counters.AuthAccepted++;
                    _authenticated = true;
                    StartSender(_config.Current, _tcpHost);
                    WriteMarker("handshake.ok", "sessionId=" + _sessionId + " " + _handshake.ToText()
                                + " at=" + nowMs);
                    CoopLog.Msg("[S2] guest authenticated sessionId=" + _sessionId);
                    return;
                }

                if (outcome == HandshakeOutcome.Expired) _counters.AuthExpired++;
                else _counters.AuthDenied++;

                WriteMarker("handshake.fail", "outcome=" + outcome + " code=" + code
                            + " reason=" + Sanitize(reason) + " at=" + nowMs);
                CoopLog.Warn("[S2] guest auth failed: " + outcome + " " + code + " (" + reason + ")");
                ScheduleClose(CloseDelayMs);
                return;
            }

            if (!_authenticated)
            {
                // ยังไม่ผ่าน handshake แต่ส่ง control อย่างอื่นมา = ผิดลำดับ
                _violations++;
                _counters.ProtocolViolations++;
                ScheduleClose(0);
                return;
            }

            bool inBattle = _haveSample;
            uint nextSeq = _sender == null ? 1u : unchecked(_sender.LastSeq + 1);
            byte[] controlReply;
            string controlReason;
            S2ControlAction action = _control.Handle(message, nowMs, inBattle, nextSeq,
                                                     out controlReply, out controlReason);
            if (controlReply != null) SendToGuest(controlReply);

            switch (action)
            {
                case S2ControlAction.ResyncAccepted:
                {
                    uint fromSeq;
                    if (_control.ConsumeResyncFlag(out fromSeq) && _sender != null) _sender.FlagResync(fromSeq);
                    WriteMarker("resync.served", "fromSeq=" + fromSeq + " nextSeq=" + nextSeq
                                + " at=" + nowMs);
                    break;
                }
                case S2ControlAction.ResyncThrottled:
                    WriteMarker("resync.throttled", "at=" + nowMs);
                    break;
                case S2ControlAction.Closing:
                    WriteMarker("session.close", "reason=guest-bye sessionId=" + _sessionId + " at=" + nowMs);
                    ScheduleClose(100);
                    break;
                case S2ControlAction.Rejected:
                    _violations++;
                    _counters.ProtocolViolations++;
                    if (_violations >= MaxProtocolViolations) ScheduleClose(CloseDelayMs);
                    break;
            }
        }

        private void SendToGuest(byte[] frame)
        {
            if (frame == null) return;
            if (_tcpHost != null) _tcpHost.Send(frame);
            else if (_loopGuest != null) _loopGuest.Send(frame);
        }

        /// <summary>จำลองว่า peer อ่าน snapshot ออกไป (loopback) เพื่อไม่ให้คิวปลายทางเต็ม</summary>
        private void DrainGuestInbox()
        {
            if (_loopGuest == null) return;
            int guard = 0;
            while (guard++ < 64 && _loopGuest.TryReceive(out byte[] ignored)) _peerConsumed++;
        }

        // ------------------------------------------------------------------ self-tests

        /// <summary>
        /// นิยาม exit 6 (rev3) / X8 — รัน **ในเฟรมเดียว** ด้วย loopback pair ชั่วคราว:
        /// ยิง frame ต้องห้ามจากฝั่ง guest เข้า gate แล้วต้องได้
        ///   `forbiddenTypeDrops == 3` พอดี · `commandsAccepted == 0` · audit tuple ก่อน/หลังเท่ากัน
        /// (ไม่แตะ state ของเกมเลย · ไม่ต้องมี socket · ใช้ได้ทั้ง transport loopback และ tcp)
        /// </summary>
        private void RunLoopbackSelfTest(long nowMs)
        {
            try
            {
                S2Audit before;
                bool haveBefore = TrySampleAudit(out before);

                LoopbackTransport hostEnd;
                LoopbackTransport guestEnd;
                LoopbackTransport.CreatePair(out hostEnd, out guestEnd);

                long forbiddenBefore = _counters.ForbiddenTypeDrops;
                System.Collections.Generic.List<byte[]> frames = S2SelfTest.BuildFrames();
                for (int i = 0; i < frames.Count; i++) guestEnd.Send(frames[i]);

                int guard = 0;
                while (guard++ < 64 && hostEnd.TryReceive(out byte[] raw))
                {
                    _gate.Accept(raw, nowMs, out AssembledMessage ignored, out string reason);
                }
                long gained = _counters.ForbiddenTypeDrops - forbiddenBefore;

                S2Audit after;
                bool haveAfter = TrySampleAudit(out after);
                bool auditEqual = !haveBefore && !haveAfter ? true
                                  : haveBefore && haveAfter && before.SameAs(after);

                string detail = S2SelfTest.Describe(gained, _counters.CommandsAccepted, auditEqual,
                                                    frames.Count, frames.Count - 2);
                string audits = string.Format(CultureInfo.InvariantCulture,
                    "auditBefore=[{0}] auditAfter=[{1}] haveBefore={2} haveAfter={3}",
                    haveBefore ? before.ToText() : "n/a", haveAfter ? after.ToText() : "n/a",
                    haveBefore ? "true" : "false", haveAfter ? "true" : "false");
                WriteMarker("selftest", "transport=inproc channels=temporary-loopback " + detail
                            + " malformed=" + _counters.MalformedDrops
                            + " reassembled=" + _counters.GuestFramesReassembled + " " + audits);

                if (!S2SelfTest.MeetsExit6(gained, _counters.CommandsAccepted, auditEqual))
                {
                    CoopLog.Warn("[S2] exit-6 self-test FAILED: " + detail);
                    WriteMarker("selftest.fail", detail);
                }

                hostEnd.Close();
                guestEnd.Close();
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                WriteMarker("selftest.error", Sanitize(e.GetType().Name + ": " + e.Message));
            }
        }

        /// <summary>
        /// ผลของ TCP self-test (I5/X8 ฝั่ง end-to-end): handshake จริง + ยิง 3 frame ต้องห้ามผ่าน socket
        /// **ไม่เคลม** audit tuple เท่ากัน เพราะ sim เดินต่อระหว่างนั้น (บันทึกตามจริง)
        /// </summary>
        private void ReportSelfTest()
        {
            if (_selfTestReported) return;
            S2TcpSelfTest.Result result;
            if (!_selfTestResults.TryDequeue(out result)) return;
            _selfTestReported = true;

            bool ok = result.HandshakeOk && result.SentForbiddenFrames
                      && result.ForbiddenGained == S2SelfTest.ExpectedForbiddenDrops
                      && _counters.CommandsAccepted == 0;
            WriteMarker("selftest.tcp", result.ToText()
                        + " commandsAccepted=" + _counters.CommandsAccepted
                        + " auditCrossCheck=NOT-CLAIMED(live-sim)"
                        + " exit8=" + (ok ? "PASS" : "FAIL"));
            if (!ok) CoopLog.Warn("[S2] tcp self-test FAILED: " + result.ToText());
        }

        /// <summary>อ่าน audit tuple ของ state ปัจจุบัน (main thread) — ไม่ post, ไม่นับเข้า phase stats</summary>
        private bool TrySampleAudit(out S2Audit audit)
        {
            audit = default(S2Audit);
            S2Frame frame = _sampler.Read();
            if (frame == null) return false;
            audit = frame.Audit;
            return true;
        }

        // ------------------------------------------------------------------ metrics / markers

        /// <summary>1 ค่า/เฟรมจาก `Time.unscaledDeltaTime` — adapter อยู่ที่นี่ ไม่ใช่ใน S2FrameMeter (pure)</summary>
        private void SampleFrameTime()
        {
            if (!_framemeterReady) return;
            try
            {
                _frameMeter.Add(UnityEngine.Time.unscaledDeltaTime * 1000.0);
            }
            catch (Exception e)
            {
                _framemeterReady = false;
                CoopLog.Warn("[S2] framemeter disabled: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private void FramemeterTick(long nowMs)
        {
            if (_framemeterWindowStartMs == 0)
            {
                _framemeterWindowStartMs = nowMs;
                return;
            }
            if (nowMs - _framemeterWindowStartMs < FramemeterWindowMs) return;

            int windowMs = (int)(nowMs - _framemeterWindowStartMs);
            if (_frameMeter.Count > 0)
            {
                EnsureWriter();
                WriteMarker("framemeter.window", "phase=" + (_active ? PhaseB : PhaseA)
                            + " windowMs=" + windowMs + " at=" + nowMs + " " + _frameMeter.ToMarker());
            }
            _frameMeter.Reset();
            _framemeterWindowStartMs = nowMs;
        }

        private void EnsureWriter()
        {
            if (_writer != null) return;
            try
            {
                Directory.CreateDirectory(EvidenceDir);
            }
            catch (Exception e)
            {
                CoopLog.Warn("[S2] cannot create evidence dir " + EvidenceDir + " (" + e.GetType().Name + ")");
            }
            _writer = new S2NdjsonWriter(SnapshotPath, 20, append: true);
            WriteMarker("session.start", "version=" + CoopMod.Version + " stage=" + CoopMod.StageLabel
                        + " protoVer=" + S2Protocol.ProtoVer + " fieldHashPrefix=" + S2Fields.Prefix);
        }

        private void WritePhaseEnd(long nowMs)
        {
            if (_writer == null) return;
            WriteMarker("phase.end", string.Format(CultureInfo.InvariantCulture,
                "block={0} blockIndex={1} frames={2} s2Enabled=true elapsedMs={3} " +
                "sampleMsMean={4:F4} sampleMsMax={5:F4} framesSent={6} bytesSent={7} wireBytesSent={8} " +
                "queuedDropped={9} deduped={10} paced={11} heartbeats={12} forbiddenTypeDrops={13} " +
                "commandsAccepted={14} auditChanges={15} resyncServed={16} authAccepted={17} violations={18}",
                _phase, _phaseNumber, _phaseFrames, nowMs - _phaseStartMs, _phaseSample.Mean,
                _phaseSample.Max, _counters.FramesSent, _counters.BytesSent, _counters.WireBytesSent,
                _counters.FramesQueuedDropped, _counters.SnapshotsDeduped, _counters.SnapshotsPaced,
                _counters.HeartbeatsSent, _counters.ForbiddenTypeDrops, _counters.CommandsAccepted,
                _auditChanges, _counters.ResyncServed, _counters.AuthAccepted, _counters.ProtocolViolations));
        }

        private void NoteAudit(S2Frame frame, long nowMs)
        {
            if (_hasAudit && _lastAudit.SameAs(frame.Audit)) return;
            _hasAudit = true;
            _auditChanges++;
            _lastAudit = frame.Audit;
            if (_writer == null) return;
            WriteMarker("audit.change", string.Format(CultureInfo.InvariantCulture,
                "frameId={0} at={1} {2}", frame.FrameId, nowMs, frame.Audit.ToText()));
        }

        private void LogTicker()
        {
            if (_ticksActive % 900 != 0) return;
            CoopLog.Msg(string.Format(CultureInfo.InvariantCulture,
                "[S2] active transport={0} phase={1} frames={2} {3} sent={4} dedup={5} paced={6} " +
                "hb={7} wifi={8} resync={9}/{10} auth={11} forbidden={12} commandsAccepted={13}",
                _mode, _phase, _phaseFrames, _phaseSample.ToText(), _counters.FramesSent,
                _counters.SnapshotsDeduped, _counters.SnapshotsPaced, _counters.HeartbeatsSent,
                _counters.WireBytesSent, _counters.ResyncServed, _counters.ResyncRequests,
                _counters.AuthAccepted, _counters.ForbiddenTypeDrops, _counters.CommandsAccepted));
        }

        private void WriteMarker(string kind, string body)
        {
            if (_writer == null) return;
            string line = "{\"kind\":\"" + Sanitize(kind) + "\",\"marker\":true," + Sanitize(body) + "}";
            _writer.WriteRawLine(line);
        }

        /// <summary>กัน marker พัง JSON — เก็บเฉพาะ printable ASCII</summary>
        private static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c == '"' || c == '\\') sb.Append('_');
                else if (c >= 0x20 && c < 0x7F) sb.Append(c);
                else sb.Append('_');
            }
            return sb.ToString();
        }
    }
}
