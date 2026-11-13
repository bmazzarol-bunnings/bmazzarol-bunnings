using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TelegramGateway;

/// <summary>
/// Two-way Telegram access for the 2IC (rivet).
/// IN:  long-poll getUpdates; allowed-user text is injected into the rivet
///      agent via `herdr agent prompt rivet ... --wait`. A task-file basename
///      (e.g. "heartbeat") expands to the content of tooling/WakeTool/tasks/heartbeat.md.
/// OUT: poll state/telegram/outbox/ every 2s; each file = one Telegram message
///      to the captain; on success the file is moved to state/telegram/sent/.
/// </summary>
internal static class Program
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private const int LongPollSeconds = 30;
    private const int OutboxPollMs = 2000;
    private const int HerdrTimeoutMs = 120000;
    private const int AckPrefixLen = 40;

    private static int Main()
    {
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _cts.Cancel();
            Log("shutdown requested");
        };

        var cfg = Config.Load();
        if (cfg is null)
        {
            return 1;
        }

        var api = new BotApi(cfg.Token);
        try
        {
            var me = api.GetMe();
            Log($"getMe ok: @<{me.Username}> id={me.Id}");
        }
        catch (TelegramApiException ex) when (ex.ErrorCode == 401)
        {
            Log("getMe failed: bot token rejected (401). Check TELEGRAM_BOT_TOKEN.");
            return 1;
        }

        Log($"allowed users: {string.Join(", ", cfg.AllowedUsers)}");
        Log($"outbox: {cfg.OutboxDir}");

        var inbound = Task.Run(() => InboundLoop(api, cfg, _cts.Token));
        var outbound = Task.Run(() => OutboundLoop(api, cfg, _cts.Token));

        try
        {
            Task.WaitAll(inbound, outbound);
        }
        catch (AggregateException)
        {
            // A loop throwing after cancel is fine; a loop throwing before
            // cancel is logged by the loop itself and we exit non-zero.
            Log("a loop exited unexpectedly");
            return 1;
        }

        Log("bye");
        return 0;
    }

    private static CancellationTokenSource _cts = new();

    // ---------------------------------------------------------------- IN

    private static async Task InboundLoop(BotApi api, Config cfg, CancellationToken ct)
    {
        var taskNames = LoadTaskNames(cfg.TasksDir);
        Log($"task aliases: {(taskNames.Count > 0 ? string.Join(", ", taskNames.OrderBy(n => n)) : "none")}");
        long offset = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var updates = api.GetUpdates(offset, LongPollSeconds);
                foreach (var u in updates)
                {
                    offset = Math.Max(offset, u.Id + 1);
                    var text = u.Message?.Text;
                    var chatId = u.Message?.Chat?.Id;
                    if (string.IsNullOrEmpty(text) || chatId is null)
                    {
                        continue;
                    }
                    if (!cfg.AllowedUsers.Contains(chatId.Value))
                    {
                        Log($"ignoring message from {chatId.Value}: not allowed ({Prefix(text)})");
                        continue;
                    }
                    await HandleInbound(api, cfg, taskNames, chatId.Value, text, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"inbound poll error: {ex.Message}; retrying in 2s");
                try { await Task.Delay(2000, ct); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private static async Task HandleInbound(BotApi api, Config cfg, HashSet<string> taskNames, long chatId, string text, CancellationToken ct)
    {
        Log($"inbound from {chatId}: {Prefix(text)}");

        string payload;
        var alias = text.Trim();
        if (taskNames.Contains(alias))
        {
            var file = Path.Combine(cfg.TasksDir, $"{alias}.md");
            payload = await File.ReadAllTextAsync(file, ct);
            Log($"task alias '{alias}' -> {file}");
        }
        else
        {
            payload = text;
        }

        var (exitCode, output) = RunHerdr(payload);
        var ack = exitCode == 0 ? "injected to rivet" : $"herdr exited {exitCode}";
        if (!string.IsNullOrWhiteSpace(output))
        {
            ack += $" — {Prefix(output)}";
        }
        Log($"herdr exit {exitCode}");

        try
        {
            await api.SendMessage(chatId, ack);
        }
        catch (Exception ex)
        {
            Log($"ack failed: {ex.Message}");
        }
    }

    private static (int ExitCode, string Output) RunHerdr(string content)
    {
        // ArgumentList passes the content as one argv entry — no shell, no quoting.
        var psi = new ProcessStartInfo("herdr", ["agent", "prompt", "rivet", content, "--wait", "--timeout", HerdrTimeoutMs.ToString()])
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        // RunAndCaptureText drains stdout and stderr concurrently — no pipe-buffer deadlock.
        var result = Process.RunAndCaptureText(psi, TimeSpan.FromMilliseconds(HerdrTimeoutMs + 10_000));
        return (result.ExitStatus.ExitCode, (result.StandardOutput + result.StandardError).Trim());
    }

    // ---------------------------------------------------------------- OUT

    private static async Task OutboundLoop(BotApi api, Config cfg, CancellationToken ct)
    {
        Directory.CreateDirectory(cfg.OutboxDir);
        Directory.CreateDirectory(cfg.SentDir);
        long captain = cfg.AllowedUsers.First();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var files = Directory.EnumerateFiles(cfg.OutboxDir).OrderBy(f => f).ToList();
                foreach (var file in files)
                {
                    var content = await File.ReadAllTextAsync(file, ct);
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        File.Delete(file);
                        continue;
                    }
                    try
                    {
                        await api.SendMessage(captain, content);
                        File.Move(file, Path.Combine(cfg.SentDir, Path.GetFileName(file)), overwrite: true);
                        Log($"outbound sent: {Path.GetFileName(file)} ({Prefix(content)})");
                    }
                    catch (Exception ex)
                    {
                        // Leave the file in place; retry on the next poll.
                        Log($"outbound send failed for {Path.GetFileName(file)}: {ex.Message}; will retry");
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"outbox poll error: {ex.Message}");
            }

            try
            {
                await Task.Delay(OutboxPollMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    // ---------------------------------------------------------------- utils

    private static HashSet<string> LoadTaskNames(string dir)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (Directory.Exists(dir))
        {
            foreach (var f in Directory.EnumerateFiles(dir, "*.md"))
            {
                names.Add(Path.GetFileNameWithoutExtension(f));
            }
        }
        return names;
    }

    private static string Prefix(string s)
    {
        s = s.Replace('\n', ' ').Trim();
        return s.Length <= AckPrefixLen ? s : s[..AckPrefixLen] + "…";
    }

    private static void Log(string message)
    {
        Console.WriteLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z {message}");
    }

}

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

internal sealed class BotApi(string token)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri($"https://api.telegram.org/bot{token}/"),
        Timeout = TimeSpan.FromSeconds(LongPollTimeout),
    };

    // getUpdates blocks for the server-side timeout; leave headroom.
    private static int LongPollTimeout => 40;

    public Me GetMe() => JsonSerializer.Deserialize<Me>(Http("getMe").Result!.Value.GetRawText(), JsonOpts)!;

    public Update[] GetUpdates(long offset, int timeoutSeconds)
    {
        var result = Http($"getUpdates?offset={offset}&timeout={timeoutSeconds}").Result;
        return result is null ? Array.Empty<Update>() : JsonSerializer.Deserialize<Update[]>(result.Value.GetRawText(), JsonOpts)!;
    }

    public Task SendMessage(long chatId, string text)
    {
        var payload = JsonSerializer.Serialize(new { chat_id = chatId, text });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        return HttpAsync("sendMessage", content);
    }

    private TelegramResponse Http(string path) => HttpAsync(path, null).GetAwaiter().GetResult();

    private Task<TelegramResponse> HttpAsync(string path, HttpContent? body)
    {
        return Task.Run(async () =>
        {
            try
            {
                var req = new HttpRequestMessage(body is null ? HttpMethod.Get : HttpMethod.Post, path)
                {
                    Content = body,
                };
                using var resp = await _http.SendAsync(req);
                var raw = await resp.Content.ReadAsStringAsync();
                var doc = JsonSerializer.Deserialize<TelegramResponse>(raw, JsonOpts);
                if (doc is null || !doc.Ok)
                {
                    throw new TelegramApiException(resp.StatusCode, doc?.ErrorCode ?? -1, doc?.Description ?? raw);
                }
                return doc;
            }
            finally
            {
                body?.Dispose();
            }
        });
    }
}

internal sealed class TelegramApiException(HttpStatusCode status, int errorCode, string description)
    : Exception($"Telegram API error {(int)status} (code {errorCode}): {description}")
{
    public int ErrorCode { get; } = errorCode;
}

internal sealed record Me(long Id, string? Username);

internal sealed record Update(long Id, Message? Message);

internal sealed record Message(long? MessageId, string? Text, Chat? Chat);

internal sealed record Chat(long Id);

internal sealed record TelegramResponse(bool Ok, JsonElement? Result, int? ErrorCode, string? Description);
