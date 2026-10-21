using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LongYinCoop.S2.Host;
using LongYinCoop.S2.Protocol;
using LongYinCoop.S2.Transport;

namespace LongYinCoop.S2
{
    /// <summary>
    /// S2a-2 host service (spectator, **ไม่มี write path** ไปยัง state ของเกม)
    ///
    /// ลำดับใน 1 tick (เรียกจาก CoopMod.Tick() = Unity main thread เท่านั้น):
    ///   1) poll `coop.net.*` จาก MelonPreferences.cfg (≤1 Hz) — D2-i ต้องสลับกลางรันได้
    ///   2) ถ้า active: sample state → POCO (IL2CPP อ่านบน main thread)
    ///   3) post POCO ให้ sender thread (main thread ไม่ serialize)
    ///   4) drain ช่อง guest→host ผ่าน allowlist (forbidden = ทิ้ง + นับ; commandsAccepted ต้องเป็น 0 เสมอ)
    ///
    /// A/B ของ exit 3 (rev3): **A = `coop.net.enabled=false`** (baseline, S2 idle),
    /// **B = `true`** (S2 ทำงาน) — สลับเป็นบล็อก A-B-A-B บล็อกละ 30 s
    /// ทุกครั้งที่สลับจะเขียน marker ลง `s2-snapshots.ndjson` ให้ตามเวลาจาก recorder ภายนอกได้
    /// </summary>
    internal sealed class S2HostService
    {
        public const string FileSnapshots = "s2-snapshots.ndjson";
        public const string PhaseA = "A";
        public const string PhaseB = "B";

        private readonly S2NetConfig _config;
        private readonly S2Counters _counters = new S2Counters();
        private readonly S2Metrics _metrics = new S2Metrics();
        private readonly S2Sampler _sampler = new S2Sampler();
        private readonly S2PhaseStats _phaseSample = new S2PhaseStats("sampleMs");
        private readonly S2GuestGate _gate;

        private LoopbackTransport _hostEnd;
        private LoopbackTransport _guestEnd;
        private S2SerializingSender _sender;
        private S2NdjsonWriter _writer;

        private bool _active;
        private bool _selfTestDone;
        private bool _guardWarned;
        private string _phase = PhaseA;
        private string _lastReason = "init: still reading coop.net.*";
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

            if (!_active || _sender == null) return;
            _ticksActive++;

            S2Frame frame = _sampler.Read();
            if (frame != null)
            {
                _phaseFrames++;
                _phaseSample.Add(frame.SampleMs);
                if (!_sender.TryPost(frame)) _droppedPosts++;
                NoteAudit(frame, nowMs);
            }

            DrainHostInbox(nowMs);
            DrainGuestInbox();

            if (_ticksActive % 900 == 0)
            {
                CoopLog.Msg(string.Format(CultureInfo.InvariantCulture,
                    "[S2] active phase={0} frames={1} {2} sent={3} forbidden={4} commandsAccepted={5}",
                    _phase, _phaseFrames, _phaseSample.ToText(), _counters.FramesSent,
                    _counters.ForbiddenTypeDrops, _counters.CommandsAccepted));
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
                phase = _phase,
                phaseNumber = _phaseNumber,
                phaseChanges = _phaseChanges,
                auditChanges = _auditChanges,
                reason = _lastReason,
                ticksActive = _ticksActive,
                droppedPosts = _droppedPosts,
                peerConsumed = _peerConsumed,
                snapshotPath = SnapshotPath,
                samplerFrames = _sampler.Frames,
                samplerEmptyReads = _sampler.EmptyReads,
                samplerErrors = _sampler.Errors,
                samplerLastError = _sampler.LastError,
                battleKey = _sampler.LastBattleKey,
                phaseSample = _phaseSample.Snapshot(),
                metrics = _metrics.Snapshot(),
                counters = _counters.Snapshot(),
                config = _config.Current.Describe(),
                configReads = _config.PollCount,
            };
        }

        private void ApplySettings(S2NetSettings settings, long nowMs)
        {
            bool want = settings.Enabled && settings.IsValid && settings.Role == "host"
                        && GuardWritePathsClosed(warn: true);
            if (want == _active)
            {
                if (!want) _lastReason = DescribeIdle(settings);
                return;
            }

            if (want) Activate(nowMs); else Deactivate(settings, nowMs);
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
                             + " and set coop.net.enabled=true");
            }
            return false;
        }

        /// <summary>รายชื่อ pref ที่เปิดอยู่ (ตาม <see cref="S2WriteGuard"/>) — ใช้ในข้อความเท่านั้น</summary>
        private static string OpenWritePaths()
        {
            return S2WriteGuard.Describe(CoopPrefs.AllowRemoteInput.Value, CoopPrefs.JitProbe.Value,
                                         CoopPrefs.JitProbeW2.Value, CoopPrefs.JitProbeW4.Value,
                                         CoopPrefs.JitProbeW6.Value, CoopPrefs.JitProbeW7.Value);
        }

        private static string DescribeIdle(S2NetSettings settings)
        {
            if (!settings.FileRead) return "idle: coop.net.enabled unreadable (" + (settings.Error ?? "?") + ")";
            if (!settings.SectionFound) return "idle: section [LongYinCoop] missing in MelonPreferences.cfg";
            if (!settings.Enabled) return "idle: coop.net.enabled=false (phase A baseline)";
            if (!string.IsNullOrEmpty(settings.Error)) return "idle: " + settings.Error;
            if (settings.Role != "host") return "idle: coop.role=" + (settings.Role ?? "-") + " (S2a-2 runs host side only)";
            if (CoopPrefs.AllowRemoteInput.Value || CoopPrefs.JitProbe.Value || CoopPrefs.JitProbeW2.Value
                || CoopPrefs.JitProbeW4.Value || CoopPrefs.JitProbeW6.Value || CoopPrefs.JitProbeW7.Value)
                return "idle: write path open (" + OpenWritePaths() + ") - must be closed for S2";
            return "idle: unsupported settings";
        }

        private void Activate(long nowMs)
        {
            EnsureWriter();
            LoopbackTransport.CreatePair(out _hostEnd, out _guestEnd);
            _sender = new S2SerializingSender(_hostEnd, _counters, _metrics, _writer);
            _active = true;
            _phaseNumber++;
            _phase = PhaseB;
            _phaseStartMs = nowMs;
            _phaseFrames = 0;
            _phaseSample.Reset();
            _lastReason = "active: coop.net.enabled=true role=host (loopback) - phase B";
            WriteMarker("phase.start", "block=" + _phase + " blockIndex=" + _phaseNumber
                        + " s2Enabled=true role=host transport=loopback");
            CoopLog.Msg("[S2] snapshot sender ACTIVE block=" + _phase + " -> " + SnapshotPath);

            if (!_selfTestDone)
            {
                _selfTestDone = true;
                RunLoopbackSelfTest(nowMs);
            }
        }

        private void Deactivate(S2NetSettings settings, long nowMs)
        {
            WritePhaseEnd(nowMs);
            if (_sender != null)
            {
                _sender.Stop();
                _sender = null;
            }
            if (_hostEnd != null)
            {
                _hostEnd.Close();
                _guestEnd.Close();
                _hostEnd = null;
                _guestEnd = null;
            }

            _active = false;
            _phase = PhaseA;
            _phaseChanges++;
            _lastReason = DescribeIdle(settings);
            WriteMarker("phase.idle", "block=" + _phase + " s2Enabled=false role=none transport=none reason="
                        + Sanitize(_lastReason));
            CoopLog.Msg("[S2] snapshot sender IDLE (" + _lastReason + ") -> block " + _phase);
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
            WriteMarker("session.start", "version=" + CoopMod.Version + " stage=" + CoopMod.StageLabel);
        }

        private void WritePhaseEnd(long nowMs)
        {
            if (_writer == null) return;
            WriteMarker("phase.end", string.Format(CultureInfo.InvariantCulture,
                "block={0} blockIndex={1} frames={2} s2Enabled=true elapsedMs={3} " +
                "sampleMsMean={4:F4} sampleMsMax={5:F4} framesSent={6} bytesSent={7} " +
                "queuedDropped={8} forbiddenTypeDrops={9} commandsAccepted={10} auditChanges={11}",
                _phase, _phaseNumber, _phaseFrames, nowMs - _phaseStartMs, _phaseSample.Mean,
                _phaseSample.Max, _counters.FramesSent, _counters.BytesSent,
                _counters.FramesQueuedDropped, _counters.ForbiddenTypeDrops, _counters.CommandsAccepted,
                _auditChanges));
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

        /// <summary>
        /// ฝั่ง guest→host: รับ frame จาก peer แล้วผ่าน allowlist
        /// ถ้าเป็น type ที่ไม่อยู่ในลิสต์ = ทิ้ง + นับ (ไม่มีทาง apply ได้ เพราะ S2 ไม่มี write path)
        /// </summary>
        private void DrainHostInbox(long nowMs)
        {
            if (_hostEnd == null) return;
            int guard = 0;
            while (guard++ < 64 && _hostEnd.TryReceive(out byte[] raw))
            {
                S2GateOutcome outcome = _gate.Accept(raw, nowMs, out AssembledMessage message, out string reason);
                if (outcome == S2GateOutcome.Forbidden || outcome == S2GateOutcome.Malformed)
                {
                    CoopLog.Warn("[S2] guest frame dropped: " + reason);
                }
            }
        }

        /// <summary>จำลองว่า peer อ่าน snapshot ออกไป (loopback) เพื่อไม่ให้คิวปลายทางเต็ม</summary>
        private void DrainGuestInbox()
        {
            if (_guestEnd == null) return;
            int guard = 0;
            while (guard++ < 64 && _guestEnd.TryReceive(out byte[] ignored)) _peerConsumed++;
        }

        /// <summary>
        /// self-test ในรัน (นิยาม exit 6 rev3): ยิง frame ต้องห้ามจากฝั่ง guest เข้า host
        /// แล้วต้องได้ **`forbiddenTypeDrops == 3` พอดี · `commandsAccepted == 0` · audit tuple ก่อน/หลังเท่ากัน**
        /// ไม่แตะ state ของเกมเลย
        /// </summary>
        private void RunLoopbackSelfTest(long nowMs)
        {
            try
            {
                S2Audit before;
                bool haveBefore = TrySampleAudit(out before);

                List<byte[]> frames = S2SelfTest.BuildFrames();
                for (int i = 0; i < frames.Count; i++) _guestEnd.Send(frames[i]);

                long forbiddenBefore = _counters.ForbiddenTypeDrops;
                DrainHostInbox(nowMs);
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
                WriteMarker("selftest", detail + " malformed=" + _counters.MalformedDrops
                            + " reassembled=" + _counters.GuestFramesReassembled + " " + audits);

                if (!S2SelfTest.MeetsExit6(gained, _counters.CommandsAccepted, auditEqual))
                {
                    CoopLog.Warn("[S2] exit-6 self-test FAILED: " + detail);
                    WriteMarker("selftest.fail", detail);
                }
            }
            catch (Exception e)
            {
                CoopLog.Error(e);
                WriteMarker("selftest.error", Sanitize(e.GetType().Name + ": " + e.Message));
            }
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
