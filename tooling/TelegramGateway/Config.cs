namespace TelegramGateway;

internal sealed class Config
{
    public required string Token { get; init; }
    public required IReadOnlyList<long> AllowedUsers { get; init; }
    public required string RepoRoot { get; init; }

    public string OutboxDir => Path.Combine(RepoRoot, "state", "telegram", "outbox");
    public string SentDir => Path.Combine(RepoRoot, "state", "telegram", "sent");
    public string TasksDir => Path.Combine(RepoRoot, "tooling", "WakeTool", "tasks");

    private const string DefaultTokenFile = "/home/bmazzarol/.config/rivet/telegram-bot-token";
    private const string DefaultUsersFile = "/home/bmazzarol/.config/rivet/telegram-allowed-users";

    public static Config? Load()
    {
        // Token: env first, then file.
        var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(token) && File.Exists(DefaultTokenFile))
        {
            token = File.ReadAllText(DefaultTokenFile).Trim();
        }
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.Error.WriteLine("No Telegram bot token. Set TELEGRAM_BOT_TOKEN or write the token to " + DefaultTokenFile);
            return null;
        }

        // Allowed users: env (comma-separated) first, then file.
        var usersRaw = Environment.GetEnvironmentVariable("TELEGRAM_ALLOWED_USERS") ?? (File.Exists(DefaultUsersFile) ? File.ReadAllText(DefaultUsersFile) : null);
        if (string.IsNullOrWhiteSpace(usersRaw))
        {
            Console.Error.WriteLine("No allowed Telegram users. Set TELEGRAM_ALLOWED_USERS or write comma-separated user IDs to " + DefaultUsersFile);
            return null;
        }

        var users = usersRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => long.Parse(s))
            .ToList();
        if (users.Count == 0)
        {
            Console.Error.WriteLine("Allowed users list is empty.");
            return null;
        }

        var root = FindRepoRoot();
        return new Config { Token = token, AllowedUsers = users, RepoRoot = root };
    }

    private static string FindRepoRoot()
    {
        var env = Environment.GetEnvironmentVariable("RIVET_REPO_ROOT");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
        {
            return Path.GetFullPath(env);
        }

        // Walk up from the current directory looking for AGENTS.md.
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "AGENTS.md")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }

        // Fallback: assume the app runs from <root>/tooling/TelegramGateway.
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    }
}
