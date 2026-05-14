using HermesAgent.Core.Models;

namespace HermesAgent.Core.Abstractions;

/// <summary>
/// Stream events yielded by the LLM provider.
/// </summary>
public abstract record LlmStreamEvent
{
    public sealed record ContentDelta(string Delta) : LlmStreamEvent;
    public sealed record ToolCallDelta(int Index, string? Id, string? Name, string? ArgumentsDelta) : LlmStreamEvent;
    public sealed record Completed(string? FinishReason) : LlmStreamEvent;
}

/// <summary>Contract for LLM provider adapters.</summary>
public interface ILlmProvider
{
    string Name { get; }
    IAsyncEnumerable<LlmStreamEvent> StreamAsync(IReadOnlyList<Message> messages, IReadOnlyList<ToolDefinition>? tools = null, CancellationToken ct = default);
    Task<LlmResponse> CompleteAsync(IReadOnlyList<Message> messages, IReadOnlyList<ToolDefinition>? tools = null, CancellationToken ct = default);
}

/// <summary>Contract for agent tools.</summary>
public interface ITool
{
    string Name { get; }
    string Description { get; }
    ToolDefinition Definition { get; }
    Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct = default);
}

/// <summary>Describes a tool to the LLM.</summary>
public sealed record ToolDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyDictionary<string, ParameterDefinition> Parameters { get; init; }
    public IReadOnlyList<string> Required { get; init; } = [];
}

public sealed record ParameterDefinition
{
    public required string Type { get; init; }
    public required string Description { get; init; }
    public IReadOnlyList<string>? Enum { get; init; }
}

/// <summary>Contract for the agent loop.</summary>
public interface IAgent
{
    Task<AgentRunResult> RunAsync(string userInput, Guid? sessionId = null, CancellationToken ct = default);
    IAsyncEnumerable<AgentEvent> RunStreamingAsync(string userInput, Guid? sessionId = null, CancellationToken ct = default);
}

/// <summary>Events emitted during streaming agent execution.</summary>
public abstract record AgentEvent
{
    public sealed record TextDelta(string Delta) : AgentEvent;
    public sealed record ToolStarted(string ToolName, IReadOnlyDictionary<string, object?> Args) : AgentEvent;
    public sealed record ToolCompleted(ToolResult Result) : AgentEvent;
    public sealed record TurnCompleted(int TurnNumber) : AgentEvent;
    public sealed record AgentFinished(AgentRunResult Result) : AgentEvent;
    public sealed record ErrorOccurred(Exception Error) : AgentEvent;
}

/// <summary>Contract for memory/persistence.</summary>
public interface IMemoryStore
{
    Task SaveMemoryAsync(string key, string content, CancellationToken ct = default);
    Task<string?> LoadMemoryAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<MemoryEntry>> SearchAsync(string query, int maxResults = 10, CancellationToken ct = default);
    Task AppendMemoryAsync(string content, CancellationToken ct = default);
}

public sealed record MemoryEntry
{
    public required string Key { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public double Relevance { get; init; }
}

/// <summary>Contract for skill management.</summary>
public interface ISkillManager
{
    Task<IReadOnlyList<Skill>> GetSkillsAsync(CancellationToken ct = default);
    Task<Skill?> GetSkillAsync(string name, CancellationToken ct = default);
    Task SaveSkillAsync(Skill skill, CancellationToken ct = default);
    Task<Skill> CreateSkillAsync(string name, string description, string content, CancellationToken ct = default);
    Task ImproveSkillAsync(string name, string improvement, CancellationToken ct = default);
    Task DeleteSkillAsync(string name, CancellationToken ct = default);
}

public sealed record Skill
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Content { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    public int UsageCount { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>Contract for session management.</summary>
public interface ISessionManager
{
    Task<Conversation> StartSessionAsync(Guid? sessionId = null, CancellationToken ct = default);
    Task SaveSessionAsync(Conversation conversation, CancellationToken ct = default);
    Task<Conversation?> LoadSessionAsync(Guid sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<SessionSummary>> ListSessionsAsync(int maxResults = 50, CancellationToken ct = default);
    Task<string> SummarizeSessionAsync(Conversation conversation, CancellationToken ct = default);
    Task DeleteSessionAsync(Guid sessionId, CancellationToken ct = default);
}

public sealed record SessionSummary
{
    public required Guid Id { get; init; }
    public string? Title { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required int MessageCount { get; init; }
}

/// <summary>Context passed to the agent each turn.</summary>
public sealed record AgentContext
{
    public required Conversation Conversation { get; init; }
    public IReadOnlyList<Skill> ActiveSkills { get; init; } = [];
    public string? SystemPrompt { get; init; }
    public IReadOnlyDictionary<string, string> ContextFiles { get; init; } = new Dictionary<string, string>();
}
