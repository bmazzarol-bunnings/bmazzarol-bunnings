using System.Diagnostics;
using System.Globalization;

namespace TelegramGateway;

/// <summary>
/// IN path: long-poll getUpdates; allowed-user text is injected into the rivet
/// agent via `herdr agent prompt rivet ... --wait`. A task-file basename
/// (e.g. "heartbeat") expands to the content of tooling/WakeTool/tasks/heartbeat.md.
/// </summary>
internal static class Inbound
{
    private const int LongPollSeconds = 30;
    private const int HerdrTimeoutMs = 120000;

    public static async Task Loop(BotApi api, Config cfg, CancellationToken ct)
    {
        var taskNames = LoadTaskNames(cfg.TasksDir);
        Log.Write($"task aliases: {(taskNames.Count > 0 ? string.Join(", ", taskNames.OrderBy(n => n)) : "none")}");
        long offset = 0;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var updates = await api.GetUpdates(offset, LongPollSeconds);
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
                        Log.Write($"ignoring message from {chatId.Value}: not allowed ({Log.Prefix(text)})");
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
                Log.Write($"inbound poll error: {ex.Message}; retrying in 2s");
                try { await Task.Delay(2000, ct); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private static async Task HandleInbound(BotApi api, Config cfg, HashSet<string> taskNames, long chatId, string text, CancellationToken ct)
    {
        Log.Write($"inbound from {chatId}: {Log.Prefix(text)}");

        string payload;
        var alias = text.Trim();
        if (taskNames.Contains(alias))
        {
            var file = Path.Combine(cfg.TasksDir, $"{alias}.md");
            payload = await File.ReadAllTextAsync(file, ct);
            Log.Write($"task alias '{alias}' -> {file}");
        }
        else
        {
            payload = text;
        }

        var (exitCode, output) = RunHerdr(payload);
        var ack = exitCode == 0 ? "injected to rivet" : $"herdr exited {exitCode}";
        if (!string.IsNullOrWhiteSpace(output))
        {
            ack += $" — {Log.Prefix(output)}";
        }
        Log.Write($"herdr exit {exitCode}");

        try
        {
            await api.SendMessage(chatId, ack);
        }
        catch (Exception ex)
        {
            Log.Write($"ack failed: {ex.Message}");
        }
    }

    private static (int ExitCode, string Output) RunHerdr(string content)
    {
        // ArgumentList passes the content as one argv entry — no shell, no quoting.
        var psi = new ProcessStartInfo("herdr", ["agent", "prompt", "rivet", content, "--wait", "--timeout", HerdrTimeoutMs.ToString(CultureInfo.InvariantCulture)])
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
}
