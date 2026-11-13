using Bunnings.Common.ScalingWorker.Contract;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace WakeTool;

internal sealed class WakeJobContext
{
    public required JobRunner Runner { get; init; }
    public required IReadOnlyList<JobConfig> Jobs { get; init; }
}

internal static partial class WakeWorker
{
    [LoggerMessage(LogLevel.Error, "[job:{Name}] tick failed")]
    private static partial void LogTickFailed(ILogger logger, Exception exception, string name);

    public static BackgroundWorkerOptions<WakeJobContext> BuildOptions(
        IReadOnlyList<JobConfig> jobs,
        JobRunner runner,
        ILoggerFactory loggerFactory)
    {
        return new BackgroundWorkerOptions<WakeJobContext>(
            Name: "wake",
            ContextFactory: (_, _) => Task.FromResult(new WakeJobContext
            {
                Runner = runner,
                Jobs = jobs
            }),
            Run: Run,
            LoggerFactory: loggerFactory)
        {
            MaxConcurrent = 1,
            SpawnTime = TimeSpan.FromSeconds(5)
        };
    }

    private static async ValueTask<Result> Run(
        ActionInvocationParameters<WakeJobContext> parameters,
        CancellationToken token)
    {
        var ctx = parameters.Context;
        uint fired = 0;
        foreach (var job in ctx.Jobs)
        {
            if (!job.Enabled) continue;
            if (token.IsCancellationRequested) break;
            try
            {
                if (ctx.Runner.Execute(job))
                    fired++;
            }
            catch (Exception ex)
            {
                LogTickFailed(ctx.Runner.Logger, ex, job.Name);
            }
        }
        await Task.CompletedTask;
        return fired > 0 ? Result.Completed(fired) : Result.CompletedNoAction;
    }
}

internal sealed partial class JobRunner
{
    private static readonly JsonSerializerOptions StateJsonOptions = new()
    {
        WriteIndented = true
    };

    [LoggerMessage(LogLevel.Debug, "[job:{Name}] no change (fp={Fp}, streak={N})")]
    private static partial void LogNoChange(ILogger logger, string name, string fp, int n);

    [LoggerMessage(LogLevel.Information, "[job:{Name}] changed ({OldFp} -> {NewFp}), injecting tick")]
    private static partial void LogChanged(ILogger logger, string name, string? oldFp, string newFp);

    [LoggerMessage(LogLevel.Error, "[job:{Name}] task file could not be loaded; tick skipped (retry next due)")]
    private static partial void LogTaskLoadFailed(ILogger logger, string name);

    [LoggerMessage(LogLevel.Information, "[job:{Name}] inject {Result}")]
    private static partial void LogInject(ILogger logger, string name, string result);

    [LoggerMessage(LogLevel.Warning, "[job:{Name}] invalid task name '{Task}'")]
    private static partial void LogInvalidTask(ILogger logger, string name, string task);

    [LoggerMessage(LogLevel.Warning, "[job:{Name}] task file missing: {Path}")]
    private static partial void LogTaskFileMissing(ILogger logger, string name, string path);

    [LoggerMessage(LogLevel.Warning, "[job:{Name}] unknown injectMode '{Mode}', using runner default")]
    private static partial void LogUnknownMode(ILogger logger, string name, string mode);

    private readonly Dictionary<string, JobState> _states = new();
    private readonly InjectMode _mode;
    private readonly string _target;

    public JobRunner(string stateDir, InjectMode mode, string target, ILogger logger)
    {
        StateDir = stateDir;
        _mode = mode;
        _target = target;
        Logger = logger;
        Directory.CreateDirectory(stateDir);
    }

    public string StateDir { get; }
    public ILogger Logger { get; }

    public bool Execute(JobConfig job)
    {
        var now = DateTime.UtcNow;
        var state = LoadState(job.Name);

        if (!Cron.IsDue(job, now, state?.LastRunUtc ?? DateTime.MinValue))
            return false;

        var fingerprint = Monitor.RunFingerprint(job.MonitorCommand);
        var changed = state is null || state.Fingerprint != fingerprint;

        if (!changed)
        {
            SaveState(job.Name, new JobState(fingerprint, now, (state?.ConsecutiveNoChange ?? 0) + 1));
            LogNoChange(Logger, job.Name, fingerprint, state?.ConsecutiveNoChange ?? 0);
            return false;
        }

        LogChanged(Logger, job.Name, state?.Fingerprint, fingerprint);        var prompt = ResolvePrompt(job);
        if (prompt is null)
        {
            LogTaskLoadFailed(Logger, job.Name);
            return false;
        }
        var mode = ResolveMode(job);
        bool ok;
        bool skipped = false;
        if (mode == InjectMode.Herdr)
        {
            // Input-safe path: never send into a busy prompt box.
            var result = Injector.HerdrSendSafe(_target, prompt);
            skipped = result is HerdrInjectResult.Skipped;
            ok = result is HerdrInjectResult.Sent;
        }
        else
        {
            ok = Injector.Send(prompt, mode, _target);
        }
        LogInject(Logger, job.Name, ok ? "ok" : (skipped ? "SKIPPED" : "FAILED"));

        // Skipped injections were never sent — do not advance state, so the
        // next tick retries. Every other outcome keeps the existing behavior.
        if (!skipped)
            SaveState(job.Name, new JobState(fingerprint, now, 0));
        return true;
    }

    private string? ResolvePrompt(JobConfig job)
    {
        if (string.IsNullOrWhiteSpace(job.Task))
            return job.TickPrompt;
        if (job.Task.Contains('/', StringComparison.Ordinal)
            || job.Task.Contains('\\', StringComparison.Ordinal)
            || job.Task.Contains("..", StringComparison.Ordinal))
        {
            LogInvalidTask(Logger, job.Name, job.Task);
            return null;
        }
        var name = job.Task.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? job.Task : job.Task + ".md";
        var path = Path.Combine(JobConfigStore.FindRepoRoot(), "tooling", "WakeTool", "tasks", name);
        if (!File.Exists(path))
        {
            LogTaskFileMissing(Logger, job.Name, path);
            return null;
        }
        return File.ReadAllText(path);
    }

    private InjectMode ResolveMode(JobConfig job)
    {
        if (string.IsNullOrWhiteSpace(job.InjectMode))
            return _mode;
        switch (job.InjectMode.Trim().ToUpperInvariant())
        {
            case "TMUX":
                return InjectMode.Tmux;
            case "CONNECT":
                return InjectMode.Connect;
            case "FILE":
                return InjectMode.File;
            case "HERDR":
                return InjectMode.Herdr;
        }
        LogUnknownMode(Logger, job.Name, job.InjectMode);
        return _mode;
    }

    private JobState? LoadState(string name)
    {
        if (_states.TryGetValue(name, out var cached))
            return cached;
        var path = Path.Combine(StateDir, $"{name}.json");
        if (!File.Exists(path))
            return null;
        try
        {
            var s = JsonSerializer.Deserialize<JobState>(File.ReadAllText(path), StateJsonOptions);
            if (s is not null) _states[name] = s;
            return s;
        }
        catch
        {
            return null;
        }
    }

    private void SaveState(string name, JobState state)
    {
        _states[name] = state;
        var path = Path.Combine(StateDir, $"{name}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(state, StateJsonOptions));
    }
}
