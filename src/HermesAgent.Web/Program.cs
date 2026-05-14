using System.Text;
using System.Text.Json;
using HermesAgent.Agent;
using HermesAgent.Agent.Providers;
using HermesAgent.Core.Abstractions;
using HermesAgent.Core.Configuration;
using HermesAgent.Core.Models;
using HermesAgent.Memory;
using HermesAgent.Memory.Sqlite;
using HermesAgent.Skills;
using HermesAgent.Tools;
using HermesAgent.Tools.Toolsets;
using HermesAgent.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

// ─── Bootstrap ───────────────────────────────────────────────────────────

var startTime = DateTimeOffset.UtcNow;
var builder = WebApplication.CreateBuilder(args);

// Layered config
var userHermesDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".hermes");

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile(Path.Combine(userHermesDir, "config.json"), optional: true)
    .AddEnvironmentVariables("HERMES_")
    .AddCommandLine(args);

// Services
builder.Services.Configure<HermesOptions>(
    builder.Configuration.GetSection(HermesOptions.SectionName));

// Core Service Registration
builder.Services.AddHermesCore();

builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// ─── Routes ──────────────────────────────────────────────────────────────

app.MapGet("/health", (IOptions<HermesOptions> opts) => Results.Ok(new HealthResponse
{
    Status = "ok",
    Version = "1.0.0",
    Uptime = (DateTimeOffset.UtcNow - startTime).ToString(@"d\.hh\:mm\:ss")
})).WithName("Health");

app.MapGet("/info", async (
    IOptions<HermesOptions> opts,
    IEnumerable<ITool> tools,
    ISkillManager skills,
    CancellationToken ct) =>
{
    var skillList = await skills.GetSkillsAsync(ct);
    return Results.Ok(new InfoResponse
    {
        Version = "1.0.0",
        Provider = opts.Value.Llm.Provider,
        Model = opts.Value.Llm.Model,
        Tools = tools.Count(),
        Skills = skillList.Count
    });
}).WithName("Info");

// ─── Chat ─────────────────────────────────────────────────────────────────

app.MapPost("/chat", async (
    [FromBody] ChatRequest req,
    IAgent agent,
    HttpContext ctx,
    CancellationToken ct) =>
{
    if (req.Stream)
    {
        ctx.Response.Headers.ContentType = "text/event-stream";
        ctx.Response.Headers.CacheControl = "no-cache";
        ctx.Response.Headers.Connection = "keep-alive";

        await foreach (var evt in agent.RunStreamingAsync(req.Message, req.SessionId, ct))
        {
            string? sseData = evt switch
            {
                AgentEvent.TextDelta d       => JsonSerializer.Serialize(new { type = "delta", text = d.Delta }),
                AgentEvent.ToolStarted t     => JsonSerializer.Serialize(new { type = "tool_start", tool = t.ToolName }),
                AgentEvent.ToolCompleted t   => JsonSerializer.Serialize(new { type = "tool_done",  tool = t.Result.ToolName, error = t.Result.IsError }),
                AgentEvent.TurnCompleted t   => JsonSerializer.Serialize(new { type = "turn",       turn = t.TurnNumber }),
                AgentEvent.AgentFinished f   => JsonSerializer.Serialize(new { type = "done",       turns = f.Result.TurnsUsed, duration_ms = f.Result.Duration.TotalMilliseconds }),
                AgentEvent.ErrorOccurred e   => JsonSerializer.Serialize(new { type = "error",      message = e.Error.Message }),
                _                           => null
            };

            if (sseData is not null)
            {
                await ctx.Response.WriteAsync($"data: {sseData}\n\n", ct);
                await ctx.Response.Body.FlushAsync(ct);
            }
        }

        await ctx.Response.WriteAsync("data: [DONE]\n\n", ct);
        return Results.Empty;
    }

    var result = await agent.RunAsync(req.Message, req.SessionId, ct);
    return Results.Ok(new ChatResponse
    {
        Response = result.FinalResponse,
        SessionId = req.SessionId ?? Guid.Empty,
        TurnsUsed = result.TurnsUsed,
        DurationMs = result.Duration.TotalMilliseconds,
        ToolCallCount = result.ToolResults.Count
    });
}).WithName("Chat");

// ─── Sessions ─────────────────────────────────────────────────────────────

app.MapGet("/sessions", async (ISessionManager sessions, [FromQuery] int limit = 50, CancellationToken ct = default) =>
{
    var list = await sessions.ListSessionsAsync(limit, ct);
    return Results.Ok(new SessionListResponse
    {
        Total = list.Count,
        Sessions = list.Select(s => new SessionItem
        {
            Id = s.Id, Title = s.Title,
            CreatedAt = s.CreatedAt, UpdatedAt = s.UpdatedAt,
            MessageCount = s.MessageCount
        }).ToList()
    });
}).WithName("ListSessions");

app.MapGet("/sessions/{id:guid}", async (Guid id, ISessionManager sessions, CancellationToken ct) =>
{
    var conv = await sessions.LoadSessionAsync(id, ct);
    if (conv is null) return Results.NotFound();

    return Results.Ok(new SessionDetailResponse
    {
        Id = id,
        Title = conv.Title,
        Messages = conv.Messages.Select(m => new MessageItem
        {
            Role = m.Role, Content = m.Content, Timestamp = m.Timestamp
        }).ToList()
    });
}).WithName("GetSession");

app.MapDelete("/sessions/{id:guid}", async (Guid id, ISessionManager sessions, CancellationToken ct) =>
{
    await sessions.DeleteSessionAsync(id, ct);
    return Results.NoContent();
}).WithName("DeleteSession");

// ─── Skills ───────────────────────────────────────────────────────────────

app.MapGet("/skills", async (ISkillManager skillMgr, CancellationToken ct) =>
{
    var skills = await skillMgr.GetSkillsAsync(ct);
    return Results.Ok(new SkillListResponse
    {
        Total = skills.Count,
        Skills = skills.Select(s => new SkillItem
        {
            Name = s.Name, Description = s.Description,
            UpdatedAt = s.UpdatedAt, UsageCount = s.UsageCount, Tags = s.Tags
        }).ToList()
    });
}).WithName("ListSkills");

app.MapDelete("/skills/{name}", async (string name, ISkillManager skillMgr, CancellationToken ct) =>
{
    await skillMgr.DeleteSkillAsync(name, ct);
    return Results.NoContent();
}).WithName("DeleteSkill");

// ─── Memory ───────────────────────────────────────────────────────────────

app.MapGet("/memory", async (IMemoryStore memory, CancellationToken ct) =>
{
    var content = await memory.LoadMemoryAsync("MEMORY", ct);
    return Results.Ok(new { key = "MEMORY", content = content ?? "" });
});

app.MapPost("/memory/search", async ([FromBody] MemorySearchRequest req, IMemoryStore memory, CancellationToken ct) =>
{
    var results = await memory.SearchAsync(req.Query, req.MaxResults, ct);
    return Results.Ok(new MemorySearchResponse
    {
        Results = results.Select(r => new MemorySearchItem
        {
            Key = r.Key, Content = r.Content,
            Relevance = r.Relevance, CreatedAt = r.CreatedAt
        }).ToList()
    });
});

app.Run();

// ─── Shared Extensions ────────────────────────────────────────────────────

public static class ServiceExtensions
{
    public static IServiceCollection AddHermesCore(this IServiceCollection services)
    {
        services.AddHttpClient<OpenAiCompatibleProvider>();
        services.AddSingleton<ILlmProvider, OpenAiCompatibleProvider>();
        services.AddSingleton<ISystemPromptBuilder, SystemPromptBuilder>();
        services.AddSingleton<IContextCompressor, SlidingWindowContextCompressor>();
        
        services.AddTransient<IAgent, HermesAgentLoop>();
        services.AddTransient<HermesAgentLoop>();

        services.AddSingleton<SqliteSessionStore>();
        services.AddSingleton<ISessionManager>(sp => sp.GetRequiredService<SqliteSessionStore>());
        services.AddSingleton<IMemoryStore>(sp => sp.GetRequiredService<SqliteSessionStore>());

        services.AddSingleton<ISkillManager, FileSkillManager>();

        services.AddSingleton<MemoryTools>();
        services.AddSingleton<SkillTools>();
        
        // Core Tools
        services.AddSingleton<ShellTool>();
        services.AddSingleton<ReadFileTool>();
        services.AddSingleton<WriteFileTool>();
        services.AddSingleton<ListDirectoryTool>();
        services.AddSingleton<SearchFilesTool>();
        services.AddHttpClient<WebFetchTool>();
        services.AddSingleton<WebFetchTool>();

        // Advanced Tools
        services.AddSingleton<PatchTool>();
        services.AddHttpClient<WebSearchTool>();
        services.AddSingleton<WebSearchTool>();
        services.AddHttpClient<WebExtractTool>();
        services.AddSingleton<WebExtractTool>();
        services.AddSingleton<CronJobTool>();
        services.AddSingleton<DelegateTaskTool>();
        services.AddSingleton<ExecuteCodeTool>();
        services.AddSingleton<SessionSearchTool>();
        services.AddSingleton<ProcessTool>();

        services.AddSingleton<IEnumerable<ITool>>(sp =>
        {
            var list = new List<ITool>
            {
                sp.GetRequiredService<ShellTool>(),
                sp.GetRequiredService<ReadFileTool>(),
                sp.GetRequiredService<WriteFileTool>(),
                sp.GetRequiredService<ListDirectoryTool>(),
                sp.GetRequiredService<SearchFilesTool>(),
                sp.GetRequiredService<WebFetchTool>(),
                sp.GetRequiredService<PatchTool>(),
                sp.GetRequiredService<WebSearchTool>(),
                sp.GetRequiredService<WebExtractTool>(),
                sp.GetRequiredService<CronJobTool>(),
                sp.GetRequiredService<DelegateTaskTool>(),
                sp.GetRequiredService<ExecuteCodeTool>(),
                sp.GetRequiredService<SessionSearchTool>(),
                sp.GetRequiredService<ProcessTool>()
            };
            list.AddRange(sp.GetRequiredService<MemoryTools>().GetTools());
            list.AddRange(sp.GetRequiredService<SkillTools>().GetTools());
            return list;
        });

        return services;
    }
}
