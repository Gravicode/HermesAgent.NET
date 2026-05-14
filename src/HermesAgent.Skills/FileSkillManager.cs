using System.Text.Json;
using HermesAgent.Core.Abstractions;
using HermesAgent.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HermesAgent.Skills;

/// <summary>
/// File-based skill manager. Skills are stored as Markdown files in the skills directory,
/// matching the Hermes Agent agentskills.io open standard format.
/// </summary>
public sealed class FileSkillManager : ISkillManager
{
    private readonly string _skillsDir;
    private readonly ILogger<FileSkillManager> _logger;
    private readonly Dictionary<string, Skill> _cache = [];
    private bool _cacheLoaded;

    public FileSkillManager(IOptions<HermesOptions> options, ILogger<FileSkillManager> logger)
    {
        _skillsDir = Path.Combine(options.Value.DataDirectory, options.Value.Skills.SkillsDirectory);
        _logger = logger;
        Directory.CreateDirectory(_skillsDir);
    }

    public async Task<IReadOnlyList<Skill>> GetSkillsAsync(CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(ct);
        return _cache.Values.ToList();
    }

    public async Task<Skill?> GetSkillAsync(string name, CancellationToken ct = default)
    {
        await EnsureCacheLoadedAsync(ct);
        return _cache.TryGetValue(name.ToLowerInvariant(), out var skill) ? skill : null;
    }

    public async Task SaveSkillAsync(Skill skill, CancellationToken ct = default)
    {
        var path = GetSkillPath(skill.Name);
        var content = SerializeSkill(skill);
        await File.WriteAllTextAsync(path, content, ct);
        _cache[skill.Name.ToLowerInvariant()] = skill;
        _logger.LogInformation("Saved skill '{Name}'", skill.Name);
    }

    public async Task<Skill> CreateSkillAsync(string name, string description, string content, CancellationToken ct = default)
    {
        var skill = new Skill
        {
            Name = name,
            Description = description,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            UsageCount = 0
        };
        await SaveSkillAsync(skill, ct);
        _logger.LogInformation("Created skill '{Name}'", name);
        return skill;
    }

    public async Task ImproveSkillAsync(string name, string improvement, CancellationToken ct = default)
    {
        var existing = await GetSkillAsync(name, ct);
        if (existing is null)
        {
            _logger.LogWarning("Cannot improve skill '{Name}': not found", name);
            return;
        }

        var improved = existing with
        {
            Content = existing.Content + "\n\n## Improvement\n" + improvement,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await SaveSkillAsync(improved, ct);
        _logger.LogInformation("Improved skill '{Name}'", name);
    }

    public Task DeleteSkillAsync(string name, CancellationToken ct = default)
    {
        var path = GetSkillPath(name);
        if (File.Exists(path))
            File.Delete(path);
        _cache.Remove(name.ToLowerInvariant());
        return Task.CompletedTask;
    }

    private async Task EnsureCacheLoadedAsync(CancellationToken ct)
    {
        if (_cacheLoaded) return;

        foreach (var file in Directory.GetFiles(_skillsDir, "*.md"))
        {
            var skill = await DeserializeSkillAsync(file, ct);
            if (skill is not null)
                _cache[skill.Name.ToLowerInvariant()] = skill;
        }

        _cacheLoaded = true;
    }

    private string GetSkillPath(string name)
        => Path.Combine(_skillsDir, $"{SanitizeName(name)}.md");

    private static string SanitizeName(string name)
        => string.Concat(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_'));

    private static string SerializeSkill(Skill skill)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# {skill.Name}");
        sb.AppendLine();
        sb.AppendLine($"> {skill.Description}");
        sb.AppendLine();
        sb.AppendLine($"<!-- created: {skill.CreatedAt:O} -->");
        sb.AppendLine($"<!-- updated: {skill.UpdatedAt:O} -->");
        sb.AppendLine($"<!-- usage_count: {skill.UsageCount} -->");
        if (skill.Tags.Count > 0)
            sb.AppendLine($"<!-- tags: {string.Join(", ", skill.Tags)} -->");
        sb.AppendLine();
        sb.AppendLine(skill.Content);
        return sb.ToString();
    }

    private static async Task<Skill?> DeserializeSkillAsync(string path, CancellationToken ct)
    {
        var lines = await File.ReadAllLinesAsync(path, ct);
        if (lines.Length == 0) return null;

        var name = lines[0].TrimStart('#').Trim();
        if (string.IsNullOrWhiteSpace(name)) return null;

        var description = lines.Skip(1).FirstOrDefault(l => l.StartsWith('>'))?.TrimStart('>').Trim() ?? string.Empty;
        var createdAt = ParseMetaDate(lines, "created") ?? DateTimeOffset.UtcNow;
        var updatedAt = ParseMetaDate(lines, "updated") ?? DateTimeOffset.UtcNow;
        var usageCount = ParseMetaInt(lines, "usage_count");
        var tags = ParseMetaTags(lines);

        // Content = everything after the metadata comments
        var contentStart = lines
            .Select((l, i) => (line: l, idx: i))
            .LastOrDefault(x => x.line.StartsWith("<!--")).idx + 1;

        var content = string.Join('\n', lines.Skip(contentStart)).Trim();

        return new Skill
        {
            Name = name,
            Description = description,
            Content = content,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            UsageCount = usageCount,
            Tags = tags
        };
    }

    private static DateTimeOffset? ParseMetaDate(string[] lines, string key)
    {
        var line = lines.FirstOrDefault(l => l.Contains($"<!-- {key}:"));
        if (line is null) return null;
        var val = ExtractMetaValue(line);
        return DateTimeOffset.TryParse(val, out var dt) ? dt : null;
    }

    private static int ParseMetaInt(string[] lines, string key)
    {
        var line = lines.FirstOrDefault(l => l.Contains($"<!-- {key}:"));
        if (line is null) return 0;
        var val = ExtractMetaValue(line);
        return int.TryParse(val, out var n) ? n : 0;
    }

    private static List<string> ParseMetaTags(string[] lines)
    {
        var line = lines.FirstOrDefault(l => l.Contains("<!-- tags:"));
        if (line is null) return [];
        var val = ExtractMetaValue(line);
        return val.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
    }

    private static string ExtractMetaValue(string line)
    {
        var start = line.IndexOf(':') + 1;
        var end = line.LastIndexOf("-->");
        if (start <= 0 || end < start) return string.Empty;
        return line[start..end].Trim();
    }
}

/// <summary>
/// Tool that allows the agent to create and improve skills during a session.
/// </summary>
public sealed class SkillTools
{
    private readonly ISkillManager _skillManager;

    public SkillTools(ISkillManager skillManager) => _skillManager = skillManager;

    public IEnumerable<HermesAgent.Core.Abstractions.ITool> GetTools()
    {
        yield return new CreateSkillToolImpl(_skillManager);
        yield return new ImproveSkillToolImpl(_skillManager);
        yield return new ListSkillsToolImpl(_skillManager);
        yield return new ReadSkillToolImpl(_skillManager);
    }

    private sealed class CreateSkillToolImpl(ISkillManager mgr) : HermesAgent.Tools.ToolBase
    {
        public override string Name => "create_skill";
        public override string Description => "Create a new reusable skill/procedure to be remembered for future sessions.";
        public override HermesAgent.Core.Abstractions.ToolDefinition Definition => new()
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, HermesAgent.Core.Abstractions.ParameterDefinition>
            {
                ["name"] = new() { Type = "string", Description = "Short skill name (slug)" },
                ["description"] = new() { Type = "string", Description = "One-line description" },
                ["content"] = new() { Type = "string", Description = "Full skill content in Markdown" }
            },
            Required = ["name", "description", "content"]
        };

        protected override async Task<string> ExecuteCoreAsync(HermesAgent.Core.Models.ToolCall call, CancellationToken ct)
        {
            var skill = await mgr.CreateSkillAsync(
                GetArg(call, "name"),
                GetArg(call, "description"),
                GetArg(call, "content"),
                ct);
            return $"Skill '{skill.Name}' created.";
        }
    }

    private sealed class ImproveSkillToolImpl(ISkillManager mgr) : HermesAgent.Tools.ToolBase
    {
        public override string Name => "improve_skill";
        public override string Description => "Append an improvement or correction to an existing skill.";
        public override HermesAgent.Core.Abstractions.ToolDefinition Definition => new()
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, HermesAgent.Core.Abstractions.ParameterDefinition>
            {
                ["name"] = new() { Type = "string", Description = "Skill name to improve" },
                ["improvement"] = new() { Type = "string", Description = "The improvement text" }
            },
            Required = ["name", "improvement"]
        };

        protected override async Task<string> ExecuteCoreAsync(HermesAgent.Core.Models.ToolCall call, CancellationToken ct)
        {
            await mgr.ImproveSkillAsync(GetArg(call, "name"), GetArg(call, "improvement"), ct);
            return $"Skill '{GetArg(call, "name")}' improved.";
        }
    }

    private sealed class ListSkillsToolImpl(ISkillManager mgr) : HermesAgent.Tools.ToolBase
    {
        public override string Name => "list_skills";
        public override string Description => "List all available skills.";
        public override HermesAgent.Core.Abstractions.ToolDefinition Definition => new()
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, HermesAgent.Core.Abstractions.ParameterDefinition>()
        };

        protected override async Task<string> ExecuteCoreAsync(HermesAgent.Core.Models.ToolCall call, CancellationToken ct)
        {
            var skills = await mgr.GetSkillsAsync(ct);
            if (skills.Count == 0) return "No skills found.";
            return string.Join('\n', skills.Select(s => $"- {s.Name}: {s.Description}"));
        }
    }

    private sealed class ReadSkillToolImpl(ISkillManager mgr) : HermesAgent.Tools.ToolBase
    {
        public override string Name => "read_skill";
        public override string Description => "Read the full content of a skill.";
        public override HermesAgent.Core.Abstractions.ToolDefinition Definition => new()
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, HermesAgent.Core.Abstractions.ParameterDefinition>
            {
                ["name"] = new() { Type = "string", Description = "Skill name" }
            },
            Required = ["name"]
        };

        protected override async Task<string> ExecuteCoreAsync(HermesAgent.Core.Models.ToolCall call, CancellationToken ct)
        {
            var skill = await mgr.GetSkillAsync(GetArg(call, "name"), ct);
            return skill is null ? "Skill not found." : skill.Content;
        }
    }
}
