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
        public const string Loaded = "S0 loaded";
        public const string LoadedArgs = " role=host stage={0} version={1} enabled={2} probe={3} ipc={4}";
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
        public const string S0ReportWritten = "s0-result written: {0}";

        // ── log file ─────────────────────────────────────────────────────
        public const string LogLevelInfo = "INFO";
        public const string LogLevelWarn = "WARN";
        public const string LogLevelError = "ERROR";
        public const string LogStart = "=== {0} | pid={1} | start {2} ===";
        public const string LogInitFailed = "[LongYinCoop] log init failed: {0}";

        // ── compat inventory (compat-notes.md) ───────────────────────────
        public const string CompatDump = "compat: {0} patched methods -> {1}";
        public const string CompatTitle = "# compat-notes — Harmony patches present at startup";
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
