using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TelegramGateway;

/// <summary>Minimal Telegram Bot API client over HttpClient — no external packages, fully async.</summary>
internal sealed class BotApi(string token) : IDisposable
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

    public async Task<Me> GetMe()
    {
        await using var stream = await SendAsync("getMe", null);
        var env = await JsonSerializer.DeserializeAsync<Envelope<Me>>(stream, JsonOpts);
        return env?.Result ?? throw new TelegramApiException(HttpStatusCode.OK, 0, "getMe returned no result");
    }

    public async Task<Update[]> GetUpdates(long offset, int timeoutSeconds)
    {
        await using var stream = await SendAsync($"getUpdates?offset={offset}&timeout={timeoutSeconds}", null);
        var env = await JsonSerializer.DeserializeAsync<Envelope<Update[]>>(stream, JsonOpts);
        return env?.Result ?? Array.Empty<Update>();
    }

    public async Task SendMessage(long chatId, string text)
    {
        var payload = JsonSerializer.Serialize(new { chat_id = chatId, text });
        // SendAsync disposes the content in a finally; no using here.
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        await using var _ = await SendAsync("sendMessage", content);
    }

    private async Task<Stream> SendAsync(string path, HttpContent? body)
    {
        try
        {
            using var req = new HttpRequestMessage(body is null ? HttpMethod.Get : HttpMethod.Post, path)
            {
                Content = body,
            };
            var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync();
                var doc = JsonSerializer.Deserialize<TelegramResponse>(raw, JsonOpts);
                throw new TelegramApiException(resp.StatusCode, doc?.ErrorCode ?? -1, doc?.Description ?? raw);
            }
            return await resp.Content.ReadAsStreamAsync();
        }
        finally
        {
            body?.Dispose();
        }
    }

    public void Dispose() => _http.Dispose();
}

internal sealed class TelegramApiException : Exception
{
    public TelegramApiException() : this(HttpStatusCode.OK, 0, "unknown error", null) { }

    public TelegramApiException(string message) : this(HttpStatusCode.OK, 0, message, null) { }

    public TelegramApiException(string message, Exception innerException) : this(HttpStatusCode.OK, 0, message, innerException) { }

    public TelegramApiException(HttpStatusCode status, int errorCode, string description) : this(status, errorCode, description, null) { }

    private TelegramApiException(HttpStatusCode status, int errorCode, string description, Exception? inner)
        : base($"Telegram API error {(int)status} (code {errorCode}): {description}", inner)
    {
        ErrorCode = errorCode;
    }

    public int ErrorCode { get; }
}

// Bot API DTOs (only the fields we read).

internal sealed record Me(long Id, string? Username);

// The JSON key is "update_id" — case-insensitive matching alone would not bind
// it to a parameter named Id (it deserialized to 0), which is exactly why the
// offset never advanced and messages were re-delivered (#62).
internal sealed record Update([property: JsonPropertyName("update_id")] long UpdateId, Message? Message);

internal sealed record Message([property: JsonPropertyName("message_id")] long? MessageId, string? Text, Chat? Chat);

internal sealed record Chat(long Id);

internal sealed record TelegramResponse(bool Ok, JsonElement? Result, int? ErrorCode, string? Description);

/// <summary>Telegram responses wrap payloads in {"ok":true,"result":...}.</summary>
internal sealed record Envelope<T>(T? Result);
