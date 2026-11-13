using System.Globalization;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace WakeTool;

internal sealed class JobsCommand(IAnsiConsole console) : AsyncCommand<JobsCommand.Settings>
{
    internal sealed class Settings : CommandSettings
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var store = new JobConfigStore();
        var jobs = store.Load();
        if (jobs.Count == 0)
        {
            console.WriteLine("No jobs configured.");
            return 0;
        }

        var stateDir = Path.Combine(JobConfigStore.FindRepoRoot(), "tooling", "WakeTool", "state");
        var table = new Table()
            .Title("Wake Jobs")
            .AddColumn("Name")
            .AddColumn("Schedule")
            .AddColumn("Monitor")
            .AddColumn("Last Fingerprint")
            .AddColumn("Last Run")
            .AddColumn("No-Change Streak");

        foreach (var job in jobs)
        {
            var path = Path.Combine(stateDir, $"{job.Name}.json");
            var (fp, lastRun, streak) = await ReadStateAsync(path, cancellationToken);
            table.AddRow(job.Name, job.Schedule, Truncate(job.MonitorCommand, 40), fp, lastRun, streak);
        }

        try
        {
            console.Write(table);
        }
        catch (Exception)
        {
            // Table rendering can fail on markup chars in cell content; fall back to plain lines
            foreach (var job in jobs)
            {
                var path = Path.Combine(stateDir, $"{job.Name}.json");
                var (fp, lastRun, streak) = await ReadStateAsync(path, cancellationToken);
                console.WriteLine($"{job.Name} | {job.Schedule} | fp={fp} | last={lastRun} | streak={streak}");
            }
        }
        await Task.CompletedTask;
        return 0;
    }

    private static async Task<(string Fp, string LastRun, string Streak)> ReadStateAsync(string path, CancellationToken ct)
    {
        string fp = "—", lastRun = "never", streak = "—";
        if (File.Exists(path))
        {
            try
            {
                var s = JsonSerializer.Deserialize<JobState>(await File.ReadAllTextAsync(path, ct));
                fp = s!.Fingerprint;
                lastRun = s.LastRunUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                streak = s.ConsecutiveNoChange.ToString(CultureInfo.InvariantCulture);
            }
            catch { }
        }
        return (fp, lastRun, streak);
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
