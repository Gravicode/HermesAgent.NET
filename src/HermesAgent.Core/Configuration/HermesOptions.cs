namespace HermesAgent.Core.Configuration;

/// <summary>Top-level agent configuration.</summary>
public sealed class HermesOptions
{
    public const string SectionName = "Hermes";

    public LlmOptions Llm { get; set; } = new();
    public AgentOptions Agent { get; set; } = new();
    public MemoryOptions Memory { get; set; } = new();
    public SkillsOptions Skills { get; set; } = new();
    public string DataDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".hermes");
}

public sealed class LlmOptions
{
    public string Provider { get; set; } = "openai";
    public string Model { get; set; } = "gpt-4o";
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 4096;
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxRetries { get; set; } = 3;
}

public sealed class AgentOptions
{
    public int MaxTurns { get; set; } = 50;
    public int MaxContextTokens { get; set; } = 100_000;
    public string SystemPromptPath { get; set; } = "SOUL.md";
    public bool AutoCompressContext { get; set; } = true;
    public int CompressThresholdTokens { get; set; } = 80_000;
    public bool EnableSkillNudging { get; set; } = true;
    public int SkillNudgeIntervalTurns { get; set; } = 10;
}

public sealed class MemoryOptions
{
    public bool Enabled { get; set; } = true;
    public string MemoryFile { get; set; } = "MEMORY.md";
    public string UserProfileFile { get; set; } = "USER.md";
    public int MaxMemoryEntries { get; set; } = 1000;
    public bool EnableSessionSearch { get; set; } = true;
}

public sealed class SkillsOptions
{
    public bool Enabled { get; set; } = true;
    public string SkillsDirectory { get; set; } = "skills";
    public bool AutoCreateSkills { get; set; } = true;
    public bool AutoImproveSkills { get; set; } = true;
}
