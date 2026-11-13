using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace WakeTool;

public sealed class JobsCommand(IAnsiConsole console) : AsyncCommand<JobsCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var store = new JobConfigStore();
        var jobs = store.Load();
        if (jobs.Count == 0)
        {
            console.MarkupLine("[yellow]No jobs configured.[/yellow]");
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
            string fp = "—", lastRun = "never", streak = "—";
            var path = Path.Combine(stateDir, $"{job.Name}.json");
            if (File.Exists(path))
            {
                try
                {
                    var s = JsonSerializer.Deserialize<JobState>(File.ReadAllText(path));
                    fp = s!.Fingerprint;
                    lastRun = s.LastRunUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                    streak = s.ConsecutiveNoChange.ToString();
                }
                catch { }
            }
            table.AddRow(job.Name, job.Schedule, Truncate(job.MonitorCommand, 40), fp, lastRun, streak);
        }

        try
        {
            console.Write(table);
        }
        catch (Exception ex)
        {
            // Table rendering can fail on markup chars in cell content; fall back to plain lines
            foreach (var job in jobs)
            {
                string fp = "—", lastRun = "never", streak = "—";
                var path = Path.Combine(stateDir, $"{job.Name}.json");
                if (File.Exists(path))
                {
                    try
                    {
                        var s = JsonSerializer.Deserialize<JobState>(File.ReadAllText(path));
                        fp = s!.Fingerprint;
                        lastRun = s.LastRunUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                        streak = s.ConsecutiveNoChange.ToString();
                    }
                    catch { }
                }
                console.WriteLine($"{job.Name} | {job.Schedule} | fp={fp} | last={lastRun} | streak={streak}");
            }
        }
        await Task.CompletedTask;
        return 0;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
