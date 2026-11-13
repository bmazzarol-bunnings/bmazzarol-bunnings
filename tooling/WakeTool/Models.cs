namespace WakeTool;

public sealed record JobConfig(
    string Name,
    string Schedule,
    string MonitorCommand,
    string TickPrompt,
    bool Enabled = true,
    string? Task = null,
    string? InjectMode = null
);

public sealed record JobState(
    string Fingerprint,
    DateTime LastRunUtc,
    int ConsecutiveNoChange
);
