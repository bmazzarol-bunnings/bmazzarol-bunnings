namespace WakeTool;

public sealed record JobConfig(
    string Name,
    string Schedule,
    string MonitorCommand,
    string TickPrompt,
    bool Enabled = true
);

public sealed record JobState(
    string Fingerprint,
    DateTime LastRunUtc,
    int ConsecutiveNoChange
);
