namespace WakeTool;

internal sealed record JobConfig(
    string Name,
    string Schedule,
    string MonitorCommand,
    string TickPrompt,
    bool Enabled = true,
    string? Task = null,
    string? InjectMode = null
);

internal sealed record JobState(
    string Fingerprint,
    DateTime LastRunUtc,
    int ConsecutiveNoChange
);
