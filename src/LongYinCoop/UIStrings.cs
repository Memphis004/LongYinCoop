namespace LongYinCoop
{
    /// <summary>
    /// ข้อความที่ผู้ใช้เห็นทั้งหมด (UI + log) รวมที่เดียว — แปลไทยทีหลังโดยไม่แตะ logic
    /// เทมเพลตที่ใช้ string.Format ใส่ตัวเลข/ค่าที่วัดได้ด้วย {0}, {1}, ...
    /// </summary>
    internal static class UIStrings
    {
        // ── ชื่อ/สถานะของ mod ─────────────────────────────────────────────
        public const string ModName = "LongYinCoop";
        public const string StartTag = "{0} v{1} stage={2}";
        public const string Loaded = "S1 loaded";
        public const string LoadedArgs =
            " role=host stage={0} version={1} enabled={2} probe={3} ipc={4} capture={5} dev={6} allowRemoteInput={7}";
        public const string DisabledByPref = "Disabled by pref (Enabled=false) — mod does nothing";

        // ── probe ────────────────────────────────────────────────────────
        public const string ProbeInstalled = "probe: patched {0}.{1} (thread measurement only)";
        public const string ProbeTargetMissing = "probe: GameController.Update not found";
        public const string ProbeRemoved = "probe: unpatched (owned patches before={0} after={1})";

        // ── thread check ─────────────────────────────────────────────────
        public const string ThreadMatch = "Thread check: OnUpdate and probe share the same managed thread";
        public const string ThreadMismatch = "Thread check: OnUpdate and probe are on different managed threads";
        public const string ThreadDetailArgs =
            " onUpdateThread={0} probeThread={1} probeHits={2} probeThreads=[{3}] unity={4} game={5}";

        // ── IPC ──────────────────────────────────────────────────────────
        public const string IpcListening = "IPC listening \\\\.\\pipe\\{0}";
        public const string IpcClientConnected = "IPC client connected";
        public const string IpcClientDisconnected = "IPC client disconnected";
        public const string IpcBadFrame = "IPC bad frame length: {0}";
        public const string IpcBadJson = "bad json: {0}";
        public const string IpcMissingCmd = "missing cmd";
        public const string MainThreadTimeout = "main-thread call timed out";
        public const string UnknownCommand = "unknown command";
        public const string DevCommandsDisabled = "dev commands disabled (DevCommands=false)";
        public const string NoWorldMap = "player position unavailable (not on world map?)";

        // ── heartbeat / report ───────────────────────────────────────────
        public const string Heartbeat =
            "heartbeat ticks={0} probeHits={1} queued={2} ran={3} err={4} p99={5}ms";
        public const string HeartbeatS1 =
            "heartbeat ticks={0} err={1} battle={2}/{3} log={4} tickP99={5}ms heap={6}MB";
        public const string S0ReportWritten = "s0-result written: {0}";
        public const string S1ReportWritten = "s1-report written: {0}";

        // ── S1: battle log hook / markers / capture ──────────────────────
        public const string BattleLogTargetMissing = "battlelog: AddInfoText not found";
        public const string BattleLogPatchApplied = "battlelog: PatchApplied {0}";
        public const string BattleLogPatchCount = "battlelog: patched {0}/{1} overload(s)";
        public const string BattleMarker =
            "battle marker {0} type={1} state={2} player={3} enemy={4} turn={5} battleTime={6}";
        public const string CaptureStarted = "capture started -> {0}";
        public const string CaptureStartFailed = "capture start failed: {0}";
        public const string CaptureStopped = "capture stopped ({0} records, {1} dropped)";
        public const string CaptureNotActive = "capture is not active";
        public const string SnapshotUnavailable = "snapshot unavailable (nothing sampled yet)";
        public const string PrefSet = "pref set {0}={1}";
        public const string PrefUnknown = "unknown pref: {0}";

        // ── log file ─────────────────────────────────────────────────────
        // ── S1.5-b: spike status (read-only) ─────────────────────────────
        public const string SpikeDisabled = "spike: mod is disabled (coop.enabled=false)";
        public const string SpikeIpcRequired = "spike: ipc is disabled (coop.ipc=false)";
        public const string SpikeMissingSub = "spike: missing subcommand (expected \"status\")";
        public const string SpikeNotRegistered = "spike: subcommand not registered in this build: {0}";
        public const string SpikeStatusFailed = "spike status failed (calls={0} errors={1})";
        public const string SpikeTurnFlagsMissing =
            "spike: nowActiveUnitMoved/Attacked not found on BattleController — falling back to BattleUnit.moved/attacked";

        // ── S1.5-d: spike arm / move (คำสั่งเปลี่ยน state) ───────────────
        public const string SpikeNeedsArm = "DISARMED: run spike arm <seconds> first (one command per arm)";
        public const string SpikeRemoteInputOff = "DISARMED: coop.allowRemoteInput=false (panic switch)";
        public const string SpikeArmSecondsInvalid = "spike arm: seconds must be 1-300";
        public const string SpikeMissingArgs = "spike {0}: missing argument ({1})";
        public const string SpikeMoveNotInBattle = "spike move: not in battle";
        public const string SpikeMoveNotFighting = "spike move: battleState={0} (expected Fighting)";
        public const string SpikeMovePaused = "spike move: battle is paused";
        public const string SpikeMoveNotPlayerTurn = "spike move: nowActiveState={0} (expected WaitPlayerInput)";
        public const string SpikeMoveNoPlayerUnit = "spike move: active unit is missing or not player-controlled";
        public const string SpikeMoveWrongUnit = "spike move: unit {0} is not the active unit ({1})";
        public const string SpikeMoveNoGrid = "spike move: no grid cell at ({0},{1})";

        // ── log file ─────────────────────────────────────────────────────
        public const string LogLevelInfo = "INFO";
        public const string LogLevelWarn = "WARN";
        public const string LogLevelError = "ERROR";
        public const string LogStart = "=== {0} | pid={1} | start {2} ===";
        public const string LogInitFailed = "[LongYinCoop] log init failed: {0}";

        // ── compat inventory (compat-notes.md) ───────────────────────────
        public const string CompatDump = "compat: {0} patched methods -> {1}";
        public const string CompatTitle = "# compat-notes — Harmony patches present";
        public const string CompatGenerated = "Generated: {0}";
        public const string CompatProcess = "Process: {0}";
        public const string CompatTotal = "Total patched methods: {0}";
        public const string CompatTableHeader =
            "| # | Declaring type | Method | Owners | Prefix | Postfix | Transpiler | Finalizer |";
        public const string CompatTableDivider = "|---|---|---|---|---|---|---|---|";
        public const string CompatRow = "| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} |";
        public const string CompatNotesTitle = "## Notes";
        public const string CompatNoteReadOnly = "- อ่านอย่างเดียว ไม่ได้แก้/ถอด patch ของ mod อื่น";
        public const string CompatNotePurpose =
            "- ใช้ตรวจการชนกับ LongYinThai (โดยเฉพาะ AddInfoText และ setter ของ Text) ก่อนขึ้น S1";
    }
}
