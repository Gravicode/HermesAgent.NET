using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HermesAgent.Core.Abstractions;
using HermesAgent.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HermesAgent.Gateway;

// ═══════════════════════════════════════════════════════════════════════════
// GATEWAY ABSTRACTIONS
// ═══════════════════════════════════════════════════════════════════════════

public sealed record IncomingMessage
{
    public required string Platform   { get; init; }
    public required string ChatId     { get; init; }
    public required string UserId     { get; init; }
    public required string Text       { get; init; }
    public string? UserName           { get; init; }
    public string? ReplyToMessageId   { get; init; }
    public DateTimeOffset Timestamp   { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record OutgoingMessage
{
    public required string ChatId   { get; init; }
    public required string Text     { get; init; }
    public string? ReplyToMessageId { get; init; }
    public bool ParseMarkdown       { get; init; } = true;
}

public interface IPlatformAdapter
{
    string Platform { get; }
    bool IsEnabled  { get; }
    Task StartAsync(Func<IncomingMessage, CancellationToken, Task> onMessage, CancellationToken ct);
    Task SendAsync(OutgoingMessage message, CancellationToken ct);
    Task StopAsync(CancellationToken ct);
}

// ═══════════════════════════════════════════════════════════════════════════
// GATEWAY OPTIONS
// ═══════════════════════════════════════════════════════════════════════════

public sealed class GatewayOptions
{
    public const string SectionName = "Hermes:Gateway";
    public TelegramOptions  Telegram  { get; set; } = new();
    public DiscordOptions   Discord   { get; set; } = new();
    public WebhookOptions   Webhook   { get; set; } = new();
}

public sealed class TelegramOptions
{
    public bool   Enabled        { get; set; }
    public string BotToken       { get; set; } = "";
    public long[] AllowedChatIds  { get; set; } = [];
    public int    PollIntervalMs { get; set; } = 1000;
}

public sealed class DiscordOptions
{
    public bool     Enabled           { get; set; }
    public string   BotToken          { get; set; } = "";
    public ulong[]  AllowedChannelIds { get; set; } = [];
}

public sealed class WebhookOptions
{
    public bool   Enabled    { get; set; }
    public int    Port       { get; set; } = 7788;
    public string SecretKey  { get; set; } = "";
}

// ═══════════════════════════════════════════════════════════════════════════
// TELEGRAM ADAPTER
// ═══════════════════════════════════════════════════════════════════════════

public sealed class TelegramAdapter : IPlatformAdapter
{
    private readonly TelegramOptions _opts;
    private readonly HttpClient _http;
    private readonly ILogger<TelegramAdapter> _log;
    private long _lastUpdateId = 0;
    private readonly string _baseUrl;

    public string Platform  => "telegram";
    public bool   IsEnabled => _opts.Enabled && !string.IsNullOrEmpty(_opts.BotToken);

    public TelegramAdapter(IOptions<GatewayOptions> opts, HttpClient http, ILogger<TelegramAdapter> log)
    {
        _opts    = opts.Value.Telegram;
        _http    = http;
        _log     = log;
        _baseUrl = $"https://api.telegram.org/bot{_opts.BotToken}";
    }

    public async Task StartAsync(Func<IncomingMessage, CancellationToken, Task> onMessage, CancellationToken ct)
    {
        _log.LogInformation("Telegram gateway started.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var updates = await GetUpdatesAsync(ct);
                foreach (var update in updates)
                {
                    var msg = update.Message ?? update.EditedMessage;
                    if (msg?.Text is null) continue;

                    if (_opts.AllowedChatIds.Length > 0 && !_opts.AllowedChatIds.Contains(msg.Chat.Id)) continue;

                    var incoming = new IncomingMessage
                    {
                        Platform  = Platform,
                        ChatId    = msg.Chat.Id.ToString(),
                        UserId    = msg.From?.Id.ToString() ?? msg.Chat.Id.ToString(),
                        UserName  = msg.From?.Username,
                        Text      = msg.Text,
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds(msg.Date)
                    };

                    _ = Task.Run(() => onMessage(incoming, ct), ct);
                    _lastUpdateId = update.UpdateId + 1;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogError(ex, "Telegram polling error");
            }
            await Task.Delay(_opts.PollIntervalMs, ct);
        }
    }

    public async Task SendAsync(OutgoingMessage message, CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(new { chat_id = message.ChatId, text = message.Text });
        await _http.PostAsync($"{_baseUrl}/sendMessage", new StringContent(body, Encoding.UTF8, "application/json"), ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task<List<TelegramUpdate>> GetUpdatesAsync(CancellationToken ct)
    {
        try
        {
            var url = $"{_baseUrl}/getUpdates?offset={_lastUpdateId}&timeout=20";
            var resp = await _http.GetFromJsonAsync<TelegramResponse<List<TelegramUpdate>>>(url, ct);
            return resp?.Ok == true ? resp.Result ?? [] : [];
        }
        catch { return []; }
    }

    private sealed class TelegramResponse<T> { [JsonPropertyName("ok")] public bool Ok { get; set; } [JsonPropertyName("result")] public T? Result { get; set; } }
    private sealed class TelegramUpdate { [JsonPropertyName("update_id")] public long UpdateId { get; set; } [JsonPropertyName("message")] public TelegramMessage? Message { get; set; } [JsonPropertyName("edited_message")] public TelegramMessage? EditedMessage { get; set; } }
    private sealed class TelegramMessage { [JsonPropertyName("text")] public string? Text { get; set; } [JsonPropertyName("date")] public long Date { get; set; } [JsonPropertyName("chat")] public TelegramChat Chat { get; set; } = new(); [JsonPropertyName("from")] public TelegramUser? From { get; set; } }
    private sealed class TelegramChat { [JsonPropertyName("id")] public long Id { get; set; } }
    private sealed class TelegramUser { [JsonPropertyName("id")] public long Id { get; set; } [JsonPropertyName("username")] public string? Username { get; set; } }
}

// ═══════════════════════════════════════════════════════════════════════════
// DISCORD ADAPTER
// ═══════════════════════════════════════════════════════════════════════════

public sealed class DiscordAdapter : IPlatformAdapter
{
    private readonly DiscordOptions _opts;
    private readonly HttpClient _http;
    private readonly ILogger<DiscordAdapter> _log;
    private readonly Dictionary<string, string> _lastIds = [];

    public string Platform  => "discord";
    public bool   IsEnabled => _opts.Enabled && !string.IsNullOrEmpty(_opts.BotToken);

    public DiscordAdapter(IOptions<GatewayOptions> opts, HttpClient http, ILogger<DiscordAdapter> log)
    {
        _opts = opts.Value.Discord;
        _http = http;
        _log  = log;
        if (IsEnabled) _http.DefaultRequestHeaders.Add("Authorization", $"Bot {_opts.BotToken}");
    }

    public async Task StartAsync(Func<IncomingMessage, CancellationToken, Task> onMessage, CancellationToken ct)
    {
        _log.LogInformation("Discord gateway started (Polling).");
        while (!ct.IsCancellationRequested)
        {
            foreach (var channelId in _opts.AllowedChannelIds)
            {
                try
                {
                    var idStr = channelId.ToString();
                    var after = _lastIds.TryGetValue(idStr, out var last) ? $"?after={last}" : "";
                    var msgs = await _http.GetFromJsonAsync<List<DiscordMessage>>($"https://discord.com/api/v10/channels/{idStr}/messages{after}", ct) ?? [];
                    foreach (var m in msgs.Where(x => x.Author?.Bot != true).OrderBy(x => x.Timestamp))
                    {
                        await onMessage(new IncomingMessage { Platform = Platform, ChatId = idStr, UserId = m.Author?.Id ?? "", Text = m.Content ?? "" }, ct);
                        _lastIds[idStr] = m.Id;
                    }
                }
                catch { /* skip */ }
            }
            await Task.Delay(3000, ct);
        }
    }

    public async Task SendAsync(OutgoingMessage message, CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(new { content = message.Text });
        await _http.PostAsync($"https://discord.com/api/v10/channels/{message.ChatId}/messages", new StringContent(body, Encoding.UTF8, "application/json"), ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    private sealed class DiscordMessage { [JsonPropertyName("id")] public string Id { get; set; } = ""; [JsonPropertyName("content")] public string? Content { get; set; } [JsonPropertyName("author")] public DiscordUser? Author { get; set; } [JsonPropertyName("timestamp")] public DateTimeOffset Timestamp { get; set; } }
    private sealed class DiscordUser { [JsonPropertyName("id")] public string Id { get; set; } = ""; [JsonPropertyName("bot")] public bool Bot { get; set; } }
}

// ═══════════════════════════════════════════════════════════════════════════
// MAIN GATEWAY SERVICE
// ═══════════════════════════════════════════════════════════════════════════

public sealed class GatewayService : BackgroundService
{
    private readonly IEnumerable<IPlatformAdapter> _adapters;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GatewayService> _log;
    private readonly ConcurrentDictionary<string, Guid> _sessionMap = new();

    public GatewayService(IEnumerable<IPlatformAdapter> adapters, IServiceScopeFactory scopeFactory, ILogger<GatewayService> log)
    {
        _adapters = adapters;
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var tasks = _adapters.Where(a => a.IsEnabled).Select(a => a.StartAsync(HandleMessageAsync, ct));
        await Task.WhenAll(tasks);
    }

    private async Task HandleMessageAsync(IncomingMessage msg, CancellationToken ct)
    {
        var chatKey = $"{msg.Platform}:{msg.ChatId}";
        var sessionId = _sessionMap.GetOrAdd(chatKey, _ => Guid.NewGuid());

        _log.LogInformation("[{Platform}] Message from {ChatId}", msg.Platform, msg.ChatId);

        using var scope = _scopeFactory.CreateScope();
        var agent = scope.ServiceProvider.GetRequiredService<IAgent>();
        var adapter = _adapters.First(a => a.Platform == msg.Platform);

        try
        {
            await foreach (var evt in agent.RunStreamingAsync(msg.Text, sessionId, ct))
            {
                if (evt is AgentEvent.AgentFinished f)
                {
                    await adapter.SendAsync(new OutgoingMessage { ChatId = msg.ChatId, Text = f.Result.FinalResponse }, ct);
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Gateway processing error");
            await adapter.SendAsync(new OutgoingMessage { ChatId = msg.ChatId, Text = "⚠️ Error processing message." }, ct);
        }
    }
}

public static class GatewayServiceExtensions
{
    public static IServiceCollection AddHermesGateway(this IServiceCollection services)
    {
        services.AddHttpClient<TelegramAdapter>();
        services.AddHttpClient<DiscordAdapter>();
        services.AddSingleton<TelegramAdapter>();
        services.AddSingleton<DiscordAdapter>();
        services.AddSingleton<IEnumerable<IPlatformAdapter>>(sp => [sp.GetRequiredService<TelegramAdapter>(), sp.GetRequiredService<DiscordAdapter>()]);
        services.AddHostedService<GatewayService>();
        return services;
    }
}
