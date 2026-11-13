using System.Text.Json;
using Spectre.Console;

namespace WakeTool;

internal sealed class JobConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _path;

    public JobConfigStore()
    {
        var repoRoot = FindRepoRoot();
        _path = Path.Combine(repoRoot, "tooling", "WakeTool", "jobs.json");
    }

    public IReadOnlyList<JobConfig> Load()
    {
        if (!File.Exists(_path))
            return new List<JobConfig>();
        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<List<JobConfig>>(json, JsonOptions) ?? new List<JobConfig>();
    }

    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, ".git")) || Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }
}
