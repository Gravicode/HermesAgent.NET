using System.Text.Json.Serialization;

namespace HermesAgent.Web.Models;

// ─── Chat / Completions ───────────────────────────────────────────────────

public sealed record ChatRequest
{
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("session_id")]
    public Guid? SessionId { get; init; }

    [JsonPropertyName("stream")]
    public bool Stream { get; init; } = false;
}

public sealed record ChatResponse
{
    [JsonPropertyName("response")]       public required string Response      { get; init; }
    [JsonPropertyName("session_id")]     public required Guid   SessionId     { get; init; }
    [JsonPropertyName("turns_used")]     public required int    TurnsUsed     { get; init; }
    [JsonPropertyName("duration_ms")]    public required double DurationMs    { get; init; }
    [JsonPropertyName("tool_calls")]     public required int    ToolCallCount { get; init; }
}

// ─── Sessions ─────────────────────────────────────────────────────────────

public sealed record SessionListResponse
{
    [JsonPropertyName("sessions")] public required IReadOnlyList<SessionItem> Sessions { get; init; }
    [JsonPropertyName("total")]    public required int Total { get; init; }
}

public sealed record SessionItem
{
    [JsonPropertyName("id")]            public required Guid            Id           { get; init; }
    [JsonPropertyName("title")]         public string?                  Title        { get; init; }
    [JsonPropertyName("created_at")]    public required DateTimeOffset  CreatedAt    { get; init; }
    [JsonPropertyName("updated_at")]    public required DateTimeOffset  UpdatedAt    { get; init; }
    [JsonPropertyName("message_count")] public required int             MessageCount { get; init; }
}

public sealed record SessionDetailResponse
{
    [JsonPropertyName("id")]       public required Guid                       Id       { get; init; }
    [JsonPropertyName("title")]    public string?                             Title    { get; init; }
    [JsonPropertyName("messages")] public required IReadOnlyList<MessageItem> Messages { get; init; }
}

public sealed record MessageItem
{
    [JsonPropertyName("role")]      public required string         Role      { get; init; }
    [JsonPropertyName("content")]   public required string         Content   { get; init; }
    [JsonPropertyName("timestamp")] public required DateTimeOffset Timestamp { get; init; }
}

// ─── Skills ───────────────────────────────────────────────────────────────

public sealed record SkillListResponse
{
    [JsonPropertyName("skills")] public required IReadOnlyList<SkillItem> Skills { get; init; }
    [JsonPropertyName("total")]  public required int Total { get; init; }
}

// REMOVED sealed to allow inheritance
public record SkillItem
{
    [JsonPropertyName("name")]        public required string         Name        { get; init; }
    [JsonPropertyName("description")] public required string         Description { get; init; }
    [JsonPropertyName("updated_at")]  public required DateTimeOffset UpdatedAt   { get; init; }
    [JsonPropertyName("usage_count")] public required int            UsageCount  { get; init; }
    [JsonPropertyName("tags")]        public required IReadOnlyList<string> Tags { get; init; }
}

public sealed record SkillDetailResponse : SkillItem
{
    [JsonPropertyName("content")]    public required string         Content    { get; init; }
    [JsonPropertyName("created_at")] public required DateTimeOffset CreatedAt  { get; init; }
}

public sealed record CreateSkillRequest
{
    [JsonPropertyName("name")]        public required string         Name        { get; init; }
    [JsonPropertyName("description")] public required string         Description { get; init; }
    [JsonPropertyName("content")]     public required string         Content     { get; init; }
    [JsonPropertyName("tags")]        public IReadOnlyList<string>   Tags        { get; init; } = [];
}

public sealed record UpdateSkillRequest
{
    [JsonPropertyName("improvement")] public required string Improvement { get; init; }
}

// ─── Memory ───────────────────────────────────────────────────────────────

public sealed record MemoryResponse
{
    [JsonPropertyName("key")]     public required string Content { get; init; }
    [JsonPropertyName("content")] public required string Key     { get; init; }
}

public sealed record MemorySearchRequest
{
    [JsonPropertyName("query")]       public required string Query      { get; init; }
    [JsonPropertyName("max_results")] public int             MaxResults { get; init; } = 10;
}

public sealed record MemorySearchResponse
{
    [JsonPropertyName("results")] public required IReadOnlyList<MemorySearchItem> Results { get; init; }
}

public sealed record MemorySearchItem
{
    [JsonPropertyName("key")]        public required string         Key        { get; init; }
    [JsonPropertyName("content")]    public required string         Content    { get; init; }
    [JsonPropertyName("relevance")]  public required double         Relevance  { get; init; }
    [JsonPropertyName("created_at")] public required DateTimeOffset CreatedAt  { get; init; }
}

// ─── Tools ────────────────────────────────────────────────────────────────

public sealed record ToolListResponse
{
    [JsonPropertyName("tools")]  public required IReadOnlyList<ToolItem> Tools  { get; init; }
    [JsonPropertyName("total")]  public required int                     Total  { get; init; }
}

public sealed record ToolItem
{
    [JsonPropertyName("name")]        public required string Name        { get; init; }
    [JsonPropertyName("description")] public required string Description { get; init; }
}

public sealed record RunToolRequest
{
    [JsonPropertyName("name")]      public required string                        Name      { get; init; }
    [JsonPropertyName("arguments")] public required IReadOnlyDictionary<string,object?> Arguments { get; init; }
}

public sealed record RunToolResponse
{
    [JsonPropertyName("output")]      public required string Output     { get; init; }
    [JsonPropertyName("is_error")]    public required bool   IsError    { get; init; }
    [JsonPropertyName("duration_ms")] public required double DurationMs { get; init; }
}

// ─── Health / Info ────────────────────────────────────────────────────────

public sealed record HealthResponse
{
    [JsonPropertyName("status")]  public required string Status  { get; init; }
    [JsonPropertyName("version")] public required string Version { get; init; }
    [JsonPropertyName("uptime")]  public required string Uptime  { get; init; }
}

public sealed record InfoResponse
{
    [JsonPropertyName("version")]  public required string Version  { get; init; }
    [JsonPropertyName("provider")] public required string Provider { get; init; }
    [JsonPropertyName("model")]    public required string Model    { get; init; }
    [JsonPropertyName("tools")]    public required int    Tools    { get; init; }
    [JsonPropertyName("skills")]   public required int    Skills   { get; init; }
}
