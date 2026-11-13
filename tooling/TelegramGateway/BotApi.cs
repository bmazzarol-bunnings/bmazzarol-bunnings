using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TelegramGateway;

/// <summary>Minimal Telegram Bot API client over HttpClient — no external packages.</summary>
internal sealed class BotApi(string token)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri($"https://api.telegram.org/bot{token}/"),
        // getUpdates blocks for the server-side timeout; leave headroom.
        Timeout = TimeSpan.FromSeconds(40),
    };

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

// Bot API DTOs (only the fields we read).

internal sealed record Me(long Id, string? Username);

internal sealed record Update(long Id, Message? Message);

internal sealed record Message(long? MessageId, string? Text, Chat? Chat);

internal sealed record Chat(long Id);

internal sealed record TelegramResponse(bool Ok, JsonElement? Result, int? ErrorCode, string? Description);
