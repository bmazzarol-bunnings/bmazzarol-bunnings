using Bunnings.Common.ScalingWorker.Contract;
using Microsoft.Extensions.Logging;

namespace WakeTool;

public sealed class WakeJobContext
{
    public required JobRunner Runner { get; init; }
    public required List<JobConfig> Jobs { get; init; }
}

public static class WakeWorker
{
    public static BackgroundWorkerOptions<WakeJobContext> BuildOptions(
        List<JobConfig> jobs,
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
                ctx.Runner.Logger?.LogError(ex, "[job:{Name}] tick failed", job.Name);
            }
        }
        await Task.CompletedTask;
        return fired > 0 ? Result.Completed(fired) : Result.CompletedNoAction;
    }
}

public sealed class JobRunner
{
    private readonly Dictionary<string, JobState> _states = new();
    private readonly InjectMode _mode;
    private readonly string _target;

    public JobRunner(string stateDir, InjectMode mode, string target, ILogger? logger = null)
    {
        StateDir = stateDir;
        _mode = mode;
        _target = target;
        Logger = logger;
        Directory.CreateDirectory(stateDir);
    }

    public string StateDir { get; }
    public ILogger? Logger { get; }

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
            Logger?.LogDebug("[job:{Name}] no change (fp={Fp}, streak={N})", job.Name, fingerprint, state?.ConsecutiveNoChange);
            return false;
        }

        Logger?.LogInformation("[job:{Name}] changed ({Old} -> {New}), injecting tick", job.Name, state?.Fingerprint, fingerprint);
        var ok = Injector.Send(job.TickPrompt, _mode, _target);
        Logger?.LogInformation("[job:{Name}] inject {Result}", job.Name, ok ? "ok" : "FAILED");

        SaveState(job.Name, new JobState(fingerprint, now, 0));
        return true;
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
            var s = System.Text.Json.JsonSerializer.Deserialize<JobState>(File.ReadAllText(path));
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
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(state,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }
}
