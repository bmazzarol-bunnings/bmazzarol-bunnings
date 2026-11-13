using System.ComponentModel;
using Bunnings.Common.ScalingWorker;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace WakeTool;

internal sealed class RunCommand(IAnsiConsole console) : AsyncCommand<RunCommand.Settings>
{
    internal sealed class Settings : CommandSettings
    {
        [CommandOption("--mode <MODE>")]
        [Description("Injection mode: tmux, connect, file, or herdr.")]
        public string Mode { get; init; } = "tmux";

        [CommandOption("--target <TARGET>")]
        [Description("Injection target: tmux pane ID, session ID, file path, or herdr agent name.")]
        public string Target { get; init; } = "%0";

        [CommandOption("--state-dir <DIR>")]
        [Description("Directory for job state files. Defaults to <repo>/tooling/WakeTool/state.")]
        public string? StateDir { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var store = new JobConfigStore();
        var jobs = store.Load();
        if (jobs.Count == 0)
        {
            console.WriteLine("No jobs configured. Create tooling/WakeTool/jobs.json first.");
            return 1;
        }

        var mode = Injector.ParseMode(settings.Mode);
        var stateDir = settings.StateDir ?? Path.Combine(JobConfigStore.FindRepoRoot(), "tooling", "WakeTool", "state");
        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger("wake");
        var runner = new JobRunner(stateDir, mode, settings.Target, logger);

        var options = WakeWorker.BuildOptions(jobs, runner, loggerFactory);
        await using var worker = new ScalingBackgroundWorkerCore<WakeJobContext>(options);

        console.WriteLine($"Wake loop running — {jobs.Count} job(s), mode={mode}, target={settings.Target}");
        console.WriteLine($"State dir: {stateDir}");
        console.WriteLine("Ctrl+C to stop.");

        await worker.StartAsync(cancellationToken);
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException) { }

        await worker.StopAsync();
        console.WriteLine("Wake loop stopped.");
        return 0;
    }
}
