using HermesAgent.Core.Abstractions;
using HermesAgent.Core.Configuration;
using HermesAgent.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HermesAgent.Agent;

/// <summary>Builds the system prompt by composing SOUL.md + memory + skills + context files.</summary>
public interface ISystemPromptBuilder
{
    Task<string> BuildAsync(Conversation conversation, CancellationToken ct = default);
}

public sealed class SystemPromptBuilder : ISystemPromptBuilder
{
    private readonly IMemoryStore _memory;
    private readonly ISkillManager _skillManager;
    private readonly HermesOptions _options;

    public SystemPromptBuilder(IMemoryStore memory, ISkillManager skillManager, IOptions<HermesOptions> options)
    {
        _memory = memory;
        _skillManager = skillManager;
        _options = options.Value;
    }

    public async Task<string> BuildAsync(Conversation conversation, CancellationToken ct = default)
    {
        var sb = new System.Text.StringBuilder();

        // Base persona (SOUL.md)
        var soul = await _memory.LoadMemoryAsync("SOUL", ct);
        if (!string.IsNullOrWhiteSpace(soul))
        {
            sb.AppendLine(soul);
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine(DefaultSoul);
            sb.AppendLine();
        }

        // Persistent memory
        var memory = await _memory.LoadMemoryAsync("MEMORY", ct);
        if (!string.IsNullOrWhiteSpace(memory))
        {
            sb.AppendLine("## Memory");
            sb.AppendLine(memory);
            sb.AppendLine();
        }

        // User profile
        var userProfile = await _memory.LoadMemoryAsync("USER", ct);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            sb.AppendLine("## User Profile");
            sb.AppendLine(userProfile);
            sb.AppendLine();
        }

        // Active skills
        var skills = await _skillManager.GetSkillsAsync(ct);
        if (skills.Count > 0)
        {
            sb.AppendLine("## Available Skills");
            foreach (var skill in skills.Take(20)) // cap to avoid bloat
            {
                sb.AppendLine($"- **{skill.Name}**: {skill.Description}");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"Current time: {DateTimeOffset.UtcNow:R}");
        return sb.ToString();
    }

    private const string DefaultSoul = """
        You are Hermes, a self-improving AI agent. You have access to tools, memory, and skills.
        You learn from each conversation and improve over time.
        You are helpful, accurate, and efficient. You persist useful knowledge as memories and skills.
        When you complete complex tasks, consider creating skills so you can do them faster next time.
        """;
}

/// <summary>Compresses conversation context to stay within token limits.</summary>
public interface IContextCompressor
{
    Task CompressAsync(Conversation conversation, CancellationToken ct = default);
}

public sealed class SlidingWindowContextCompressor : IContextCompressor
{
    private readonly ILlmProvider _llm;
    private readonly ILogger<SlidingWindowContextCompressor> _logger;
    private const int KeepRecentMessages = 20;

    public SlidingWindowContextCompressor(ILlmProvider llm, ILogger<SlidingWindowContextCompressor> logger)
    {
        _llm = llm;
        _logger = logger;
    }

    public async Task CompressAsync(Conversation conversation, CancellationToken ct = default)
    {
        var messages = conversation.Messages.ToList();
        if (messages.Count <= KeepRecentMessages)
            return;

        // Summarize older messages via LLM
        var toSummarize = messages[..^KeepRecentMessages];
        var summaryPrompt = new[]
        {
            Message.System("Summarize the following conversation exchange concisely, preserving key facts and decisions."),
            Message.User(string.Join("\n\n", toSummarize.Select(m => $"[{m.Role}]: {m.Content}")))
        };

        var summary = await _llm.CompleteAsync(summaryPrompt, null, ct);
        _logger.LogInformation("Context compressed: {Before} → summary + {Recent} recent messages",
            messages.Count, KeepRecentMessages);

        // Rebuild conversation with summary prepended
        conversation.Clear();
        conversation.AddMessage(Message.System($"[Context Summary]\n{summary.Content}"));
        foreach (var msg in messages[^KeepRecentMessages..])
            conversation.AddMessage(msg);
    }
}
