namespace TelegramGateway;

internal static class Program
{
    private static CancellationTokenSource _cts = new();

    private static async Task<int> Main()
    {
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _cts.Cancel();
            Log.Write("shutdown requested");
        };

        var cfg = Config.Load();
        if (cfg is null)
        {
            return 1;
        }

        using var api = new BotApi(cfg.Token);
        try
        {
            var me = await api.GetMe();
            Log.Write($"getMe ok: @<{me.Username}> id={me.Id}");
        }
        catch (TelegramApiException ex) when (ex.ErrorCode == 401)
        {
            Log.Write("getMe failed: bot token rejected (401). Check TELEGRAM_BOT_TOKEN.");
            return 1;
        }

        Log.Write($"allowed users: {string.Join(", ", cfg.AllowedUsers)}");
        Log.Write($"outbox: {cfg.OutboxDir}");

        var inbound = Inbound.Loop(api, cfg, _cts.Token);
        var outbound = Outbound.Loop(api, cfg, _cts.Token);

        try
        {
            await Task.WhenAll(inbound, outbound);
        }
        catch (Exception)
        {
            // A loop throwing after cancel is fine; a loop throwing before
            // cancel is logged by the loop itself and we exit non-zero.
            Log.Write("a loop exited unexpectedly");
            return 1;
        }

        Log.Write("bye");
        return 0;
    }
}

/// <summary>Console logging. Never logs the token; message bodies only as a short prefix.</summary>
internal static class Log
{
    private const int PrefixLen = 40;

    public static void Write(string message)
    {
        Console.WriteLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z {message}");
    }

    public static string Prefix(string s)
    {
        s = s.Replace('\n', ' ').Trim();
        return s.Length <= PrefixLen ? s : s[..PrefixLen] + "…";
    }
}
