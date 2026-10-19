namespace LongYinCoop
{
    /// <summary>ข้อความที่ผู้ใช้เห็นทั้งหมด (อังกฤษก่อน) — แปลไทยทีหลังโดยไม่แตะ logic</summary>
    internal static class UIStrings
    {
        public const string ModName = "LongYinCoop";
        public const string Loaded = "S0 loaded";
        public const string DisabledByPref = "Disabled by pref (Enabled=false) — mod does nothing";
        public const string ProbeInstalled = "Harmony probe installed on GameController.Update (thread measurement only)";
        public const string ProbeTargetMissing = "GameController.Update not found — probe not installed";
        public const string ProbeRemoved = "Harmony probe removed";
        public const string ThreadMatch = "Thread check: OnUpdate and probe share the same managed thread";
        public const string ThreadMismatch = "Thread check: OnUpdate and probe are on different managed threads";
        public const string IpcListening = "IPC listening";
        public const string IpcClientConnected = "IPC client connected";
        public const string IpcClientDisconnected = "IPC client disconnected";
        public const string IpcBadFrame = "IPC bad frame length";
        public const string MainThreadTimeout = "main-thread call timed out";
        public const string UnknownCommand = "unknown command";
        public const string NoWorldMap = "player position unavailable (not on world map?)";
        public const string CompatDump = "Harmony patch inventory written";
    }
}
