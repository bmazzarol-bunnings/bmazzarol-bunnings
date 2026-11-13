namespace TelegramGateway;

/// <summary>
/// OUT path: poll state/telegram/outbox/ every 2s; each file's content is one
/// Telegram message to the captain; on success the file moves to state/telegram/sent/.
/// Failed sends leave the file in place for the next poll.
/// </summary>
internal static class Outbound
{
    private const int PollMs = 2000;

    public static async Task Loop(BotApi api, Config cfg, CancellationToken ct)
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
                        Log.Write($"outbound sent: {Path.GetFileName(file)} ({Log.Prefix(content)})");
                    }
                    catch (Exception ex)
                    {
                        // Leave the file in place; retry on the next poll.
                        Log.Write($"outbound send failed for {Path.GetFileName(file)}: {ex.Message}; will retry");
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
                Log.Write($"outbox poll error: {ex.Message}");
            }

            try
            {
                await Task.Delay(PollMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
