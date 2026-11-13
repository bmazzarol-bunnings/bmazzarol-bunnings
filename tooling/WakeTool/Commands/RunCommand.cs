using System.ComponentModel;
using Bunnings.Common.ScalingWorker;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace WakeTool;

public sealed class RunCommand(IAnsiConsole console) : AsyncCommand<RunCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--mode <MODE>")]
        [Description("Injection mode: tmux, connect, or file.")]
        public string Mode { get; init; } = "tmux";

        [CommandOption("--target <TARGET>")]
        [Description("Injection target: tmux pane ID, session ID, or file path.")]
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
            console.MarkupLine("[red]No jobs configured.[/red] Create tooling/WakeTool/jobs.json first.");
            return 1;
        }

        var mode = ParseMode(settings.Mode);
        var stateDir = settings.StateDir ?? Path.Combine(JobConfigStore.FindRepoRoot(), "tooling", "WakeTool", "state");
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger("wake");
        var runner = new JobRunner(stateDir, mode, settings.Target, logger);

        var options = WakeWorker.BuildOptions(jobs, runner, loggerFactory);
        await using var worker = new ScalingBackgroundWorkerCore<WakeJobContext>(options);

        console.MarkupLine($"[green]Wake loop running[/green] — {jobs.Count} job(s), mode={mode}, target={settings.Target}");
        console.MarkupLine($"[grey]State dir: {stateDir}[/grey]");
        console.MarkupLine("[grey]Ctrl+C to stop.[/grey]");

        await worker.StartAsync(cancellationToken);
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException) { }

        await worker.StopAsync();
        console.MarkupLine("[yellow]Wake loop stopped.[/yellow]");
        return 0;
    }

    private static InjectMode ParseMode(string s) => s.ToLowerInvariant() switch
    {
        "tmux" => InjectMode.Tmux,
        "connect" => InjectMode.Connect,
        "file" => InjectMode.File,
        _ => throw new ArgumentException($"Unknown mode '{s}'. Use tmux, connect, or file.")
    };
}
