using HermesAgent.Core.Abstractions;
using HermesAgent.Core.Models;
using Microsoft.Extensions.Logging;

namespace HermesAgent.Tools.Toolsets;

// ═══════════════════════════════════════════════════════════════════════════
// BROWSER TOOLSET  (12 tools)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Navigate to a URL in the headless browser.</summary>
public sealed class BrowserNavigateTool(ILogger<BrowserNavigateTool> log) : ToolBase
{
    private static string? _currentUrl;
    public override string Name => "browser_navigate";
    public override string Description => "Navigate to a URL in the browser. Must be called before other browser tools. Prefer web_search/web_extract for simple info retrieval.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["url"] = new() { Type = "string", Description = "URL to navigate to" }
        },
        Required = ["url"]
    };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        var url = GetArg(call, "url");
        _currentUrl = url;
        log.LogInformation("Browser navigate: {Url}", url);
        // In production: integrate Playwright/Puppeteer via process or gRPC
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 HermesAgent/1.0");
        try
        {
            var resp = await http.GetAsync(url, ct);
            return $"Navigated to {url} — HTTP {(int)resp.StatusCode} {resp.StatusCode}";
        }
        catch (Exception ex) { return $"Navigation failed: {ex.Message}"; }
    }
}

/// <summary>Get text snapshot of current page accessibility tree.</summary>
public sealed class BrowserSnapshotTool : ToolBase
{
    public override string Name => "browser_snapshot";
    public override string Description => "Get a text-based snapshot of the current page's accessibility tree. Returns interactive elements with ref IDs for browser_click and browser_type.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["full"] = new() { Type = "boolean", Description = "full=true for complete tree, false (default) for compact interactive-elements-only view" }
        }
    };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
        => Task.FromResult("[Snapshot] Browser snapshot requires Playwright integration. Install HermesAgent.Browser package.");
}

/// <summary>Click element by ref ID from snapshot.</summary>
public sealed class BrowserClickTool : ToolBase
{
    public override string Name => "browser_click";
    public override string Description => "Click on an element identified by its ref ID from the snapshot (e.g., '@e5'). Requires browser_navigate and browser_snapshot first.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["ref"] = new() { Type = "string", Description = "Ref ID from snapshot (e.g. @e5)" }
        },
        Required = ["ref"]
    };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
        => Task.FromResult($"Clicked element {GetArg(call, "ref")} (requires Playwright integration)");
}

/// <summary>Type text into an input field.</summary>
public sealed class BrowserTypeTool : ToolBase
{
    public override string Name => "browser_type";
    public override string Description => "Type text into an input field identified by its ref ID. Clears the field first, then types.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["ref"]  = new() { Type = "string", Description = "Ref ID from snapshot" },
            ["text"] = new() { Type = "string", Description = "Text to type" }
        },
        Required = ["ref", "text"]
    };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
        => Task.FromResult($"Typed into {GetArg(call, "ref")}: \"{GetArg(call, "text")}\" (requires Playwright)");
}

/// <summary>Press a keyboard key.</summary>
public sealed class BrowserPressTool : ToolBase
{
    public override string Name => "browser_press";
    public override string Description => "Press a keyboard key (e.g. Enter, Tab, Escape). Requires browser_navigate first.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["key"] = new() { Type = "string", Description = "Key name (Enter, Tab, ArrowDown, Escape, etc.)" }
        },
        Required = ["key"]
    };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
        => Task.FromResult($"Pressed {GetArg(call, "key")} (requires Playwright)");
}

/// <summary>Scroll the page.</summary>
public sealed class BrowserScrollTool : ToolBase
{
    public override string Name => "browser_scroll";
    public override string Description => "Scroll the page in a direction. Use to reveal more content.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["direction"] = new() { Type = "string", Description = "Direction: up, down, left, right", Enum = ["up", "down", "left", "right"] },
            ["amount"]    = new() { Type = "integer", Description = "Pixels to scroll (default: 500)" }
        },
        Required = ["direction"]
    };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
        => Task.FromResult($"Scrolled {GetArg(call, "direction")} (requires Playwright)");
}

/// <summary>Navigate browser back.</summary>
public sealed class BrowserBackTool : ToolBase
{
    public override string Name => "browser_back";
    public override string Description => "Navigate back to the previous page in browser history.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition>() };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
        => Task.FromResult("Navigated back (requires Playwright)");
}

/// <summary>Get browser console output and JS errors.</summary>
public sealed class BrowserConsoleTool : ToolBase
{
    public override string Name => "browser_console";
    public override string Description => "Get browser console output and JavaScript errors from the current page.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition>() };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
        => Task.FromResult("Console output: (requires Playwright integration)");
}

/// <summary>Get all images on the current page.</summary>
public sealed class BrowserGetImagesTool : ToolBase
{
    public override string Name => "browser_get_images";
    public override string Description => "Get a list of all images on the current page with URLs and alt text.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition>() };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
        => Task.FromResult("Image list: (requires Playwright integration)");
}

/// <summary>Take a screenshot and analyze it with vision AI.</summary>
public sealed class BrowserVisionTool : ToolBase
{
    public override string Name => "browser_vision";
    public override string Description => "Take a screenshot and analyze it with vision AI. Use for CAPTCHAs, visual verification, or complex layouts.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["question"] = new() { Type = "string", Description = "What to analyze in the screenshot" }
        }
    };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
        => Task.FromResult("Vision analysis: (requires Playwright + vision model integration)");
}

/// <summary>Respond to a JS dialog (alert/confirm/prompt).</summary>
public sealed class BrowserDialogTool : ToolBase
{
    public override string Name => "browser_dialog";
    public override string Description => "Respond to a native JavaScript dialog (alert/confirm/prompt/beforeunload). Call browser_snapshot first to check pending_dialogs.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["action"] = new() { Type = "string", Description = "accept or dismiss", Enum = ["accept", "dismiss"] },
            ["text"]   = new() { Type = "string", Description = "Text for prompt dialogs" }
        },
        Required = ["action"]
    };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
        => Task.FromResult($"Dialog {GetArg(call, "action")}ed (requires Playwright)");
}

/// <summary>Send raw Chrome DevTools Protocol command.</summary>
public sealed class BrowserCdpTool : ToolBase
{
    public override string Name => "browser_cdp";
    public override string Description => "Send a raw Chrome DevTools Protocol (CDP) command. Escape hatch for advanced browser operations.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["method"] = new() { Type = "string", Description = "CDP method (e.g. Page.captureScreenshot)" },
            ["params"] = new() { Type = "object", Description = "CDP method parameters as JSON object" }
        },
        Required = ["method"]
    };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
        => Task.FromResult($"CDP {GetArg(call, "method")}: (requires CDP endpoint)");
}

// ═══════════════════════════════════════════════════════════════════════════
// WEB TOOLSET  (2 tools)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Search the web for information.</summary>
public sealed class WebSearchTool : ToolBase
{
    private readonly HttpClient _http;
    public WebSearchTool(HttpClient http) => _http = http;

    public override string Name => "web_search";
    public override string Description => "Search the web for information. Returns up to 5 relevant results with titles, URLs, and descriptions.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["query"]       = new() { Type = "string",  Description = "Search query" },
            ["max_results"] = new() { Type = "integer", Description = "Max results (default: 5)" }
        },
        Required = ["query"]
    };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        var query = GetArg(call, "query");
        var max = int.TryParse(GetArgOrDefault(call, "max_results", "5"), out var m) ? m : 5;
        // Uses DuckDuckGo lite as fallback (no API key required)
        var encoded = Uri.EscapeDataString(query);
        try
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 HermesAgent/1.0");
            var html = await _http.GetStringAsync($"https://lite.duckduckgo.com/lite/?q={encoded}", ct);
            var results = ParseDdgLite(html).Take(max).ToList();
            if (results.Count == 0) return "No results found.";
            return string.Join("\n\n", results.Select((r, i) => $"{i+1}. {r.Title}\n   {r.Url}\n   {r.Snippet}"));
        }
        catch (Exception ex) { return $"Search failed: {ex.Message}"; }
    }

    private static List<(string Title, string Url, string Snippet)> ParseDdgLite(string html)
    {
        // Minimal parser for DuckDuckGo lite results
        var results = new List<(string, string, string)>();
        var lines = html.Split('\n');
        string title = "", url = "";
        foreach (var line in lines)
        {
            if (line.Contains("result-link") && line.Contains("href="))
            {
                var hrefStart = line.IndexOf("href=\"", StringComparison.Ordinal) + 6;
                var hrefEnd = line.IndexOf('"', hrefStart);
                url = hrefStart > 5 && hrefEnd > hrefStart ? line[hrefStart..hrefEnd] : "";
                var textStart = line.LastIndexOf('>') + 1;
                var textEnd = line.IndexOf('<', textStart);
                title = textStart > 0 && textEnd > textStart ? line[textStart..textEnd].Trim() : "";
            }
            else if (!string.IsNullOrEmpty(url) && line.Contains("result-snippet"))
            {
                var s = line.IndexOf('>') + 1;
                var e = line.IndexOf('<', s);
                var snippet = s > 0 && e > s ? line[s..e].Trim() : "";
                if (!string.IsNullOrEmpty(title)) results.Add((title, url, snippet));
                title = url = "";
            }
        }
        return results;
    }
}

/// <summary>Extract content from a web page URL.</summary>
public sealed class WebExtractTool : ToolBase
{
    private readonly HttpClient _http;
    public WebExtractTool(HttpClient http) => _http = http;

    public override string Name => "web_extract";
    public override string Description => "Extract content from web page URLs as markdown. Works with PDF URLs too.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["url"]       = new() { Type = "string",  Description = "URL to extract content from" },
            ["max_chars"] = new() { Type = "integer", Description = "Max characters to return (default: 8000)" }
        },
        Required = ["url"]
    };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        var url = GetArg(call, "url");
        var max = int.TryParse(GetArgOrDefault(call, "max_chars", "8000"), out var m) ? m : 8000;
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 HermesAgent/1.0");
        var html = await _http.GetStringAsync(url, ct);
        var text = StripHtml(html);
        return text.Length <= max ? text : text[..max] + "\n[truncated]";
    }

    private static string StripHtml(string html)
    {
        // Remove script/style blocks
        var sb = new System.Text.StringBuilder();
        var inTag = false; var inScript = false;
        for (int i = 0; i < html.Length; i++)
        {
            if (html[i] == '<')
            {
                inTag = true;
                var slice = html[i..Math.Min(i+8, html.Length)].ToLowerInvariant();
                if (slice.StartsWith("<script") || slice.StartsWith("<style")) inScript = true;
                if (slice.StartsWith("</scrip") || slice.StartsWith("</style")) inScript = false;
            }
            else if (html[i] == '>') { inTag = false; continue; }
            else if (!inTag && !inScript) sb.Append(html[i]);
        }
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s{2,}", " ").Trim();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// VISION TOOLSET  (1 tool)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Analyze images using AI vision.</summary>
public sealed class VisionAnalyzeTool(ILlmProvider llm) : ToolBase
{
    public override string Name => "vision_analyze";
    public override string Description => "Analyze images using AI vision. Provides description and answers a specific question about image content.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["image_url"] = new() { Type = "string", Description = "URL or base64 data URI of the image" },
            ["question"]  = new() { Type = "string", Description = "Question to answer about the image" }
        },
        Required = ["image_url"]
    };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        var url = GetArg(call, "image_url");
        var q = GetArgOrDefault(call, "question", "Describe this image in detail.");
        var prompt = new List<HermesAgent.Core.Models.Message>
        {
            HermesAgent.Core.Models.Message.User($"[Image: {url}]\n\n{q}")
        };
        var resp = await llm.CompleteAsync(prompt, null, ct);
        return resp.Content;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// CLARIFY TOOLSET  (1 tool)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Ask the user a clarification question.</summary>
public sealed class ClarifyTool : ToolBase
{
    public override string Name => "clarify";
    public override string Description => "Ask the user a question when you need clarification, feedback, or a decision before proceeding. Supports multiple-choice or open-ended modes.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["question"] = new() { Type = "string", Description = "The clarifying question to ask" },
            ["choices"]  = new() { Type = "array",  Description = "Up to 4 choices for multiple-choice mode (optional)" }
        },
        Required = ["question"]
    };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        var question = GetArg(call, "question");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"❓ {question}");
        Console.ResetColor();
        Console.Write("Your answer: ");
        var answer = Console.ReadLine() ?? "";
        return Task.FromResult(string.IsNullOrWhiteSpace(answer) ? "[No answer provided]" : answer);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// TODO TOOLSET  (1 tool)
// ═══════════════════════════════════════════════════════════════════════════

public sealed record TodoItem(string Id, string Text, string Status, int Priority);

/// <summary>Session-scoped task list manager.</summary>
public sealed class TodoTool : ToolBase
{
    private static readonly List<TodoItem> _todos = [];
    private static int _nextId = 1;

    public override string Name => "todo";
    public override string Description => "Manage your task list for the current session. Use for complex tasks with 3+ steps. Call with no parameters to read the current list.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["action"] = new() { Type = "string", Description = "read|add|complete|delete|update", Enum = ["read", "add", "complete", "delete", "update"] },
            ["text"]   = new() { Type = "string", Description = "Task text (for add/update)" },
            ["id"]     = new() { Type = "string", Description = "Task ID (for complete/delete/update)" }
        }
    };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        var action = GetArgOrDefault(call, "action", "read");
        return action switch
        {
            "add" => AddTodo(GetArg(call, "text")),
            "complete" => CompleteTodo(GetArg(call, "id")),
            "delete" => DeleteTodo(GetArg(call, "id")),
            _ => ReadTodos()
        };
    }
    private Task<string> ReadTodos()
    {
        if (_todos.Count == 0) return Task.FromResult("No tasks.");
        var lines = _todos.Select(t => $"[{t.Status}] {t.Id}. {t.Text}");
        return Task.FromResult(string.Join('\n', lines));
    }
    private Task<string> AddTodo(string text)
    {
        var id = (_nextId++).ToString();
        _todos.Add(new TodoItem(id, text, "pending", _todos.Count));
        return Task.FromResult($"Added task #{id}: {text}");
    }
    private Task<string> CompleteTodo(string id)
    {
        var i = _todos.FindIndex(t => t.Id == id);
        if (i < 0) return Task.FromResult($"Task #{id} not found.");
        _todos[i] = _todos[i] with { Status = "done" };
        return Task.FromResult($"Task #{id} marked done.");
    }
    private Task<string> DeleteTodo(string id)
    {
        var removed = _todos.RemoveAll(t => t.Id == id);
        return Task.FromResult(removed > 0 ? $"Task #{id} deleted." : $"Task #{id} not found.");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// IMAGE GENERATION TOOLSET  (1 tool)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Generate images from text prompts.</summary>
public sealed class ImageGenerateTool : ToolBase
{
    private readonly HttpClient _http;
    public ImageGenerateTool(HttpClient http) => _http = http;

    public override string Name => "image_generate";
    public override string Description => "Generate high-quality images from text prompts using FAL.ai or compatible image gen API. Returns an image URL.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["prompt"]      = new() { Type = "string", Description = "Image generation prompt" },
            ["negative"]    = new() { Type = "string", Description = "Negative prompt (what to avoid)" },
            ["width"]       = new() { Type = "integer", Description = "Image width in pixels (default: 1024)" },
            ["height"]      = new() { Type = "integer", Description = "Image height in pixels (default: 1024)" }
        },
        Required = ["prompt"]
    };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        // Requires FAL_KEY env var or compatible image gen endpoint
        var falKey = Environment.GetEnvironmentVariable("FAL_KEY");
        if (string.IsNullOrEmpty(falKey))
            return Task.FromResult("Image generation requires FAL_KEY environment variable. Get one at fal.ai.");
        return Task.FromResult($"Image generation queued for: \"{GetArg(call, "prompt")}\" (FAL.ai integration pending)");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// MIXTURE OF AGENTS TOOLSET  (1 tool)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Route hard problems through multiple frontier LLMs.</summary>
public sealed class MixtureOfAgentsTool(ILlmProvider llm) : ToolBase
{
    public override string Name => "mixture_of_agents";
    public override string Description => "Route a hard problem through multiple frontier LLMs collaboratively. Use sparingly for genuinely difficult problems (complex math, advanced algorithms, etc.).";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["problem"]       = new() { Type = "string", Description = "The hard problem to solve" },
            ["reference_models"] = new() { Type = "array", Description = "List of reference model IDs to use (optional)" }
        },
        Required = ["problem"]
    };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        var problem = GetArg(call, "problem");
        // Simplified: run 2 passes and aggregate (full MoA requires multiple LLM providers)
        var pass1 = await llm.CompleteAsync([HermesAgent.Core.Models.Message.User(problem)], null, ct);
        var aggregationPrompt = $"Given this problem:\n{problem}\n\nAnd this initial analysis:\n{pass1.Content}\n\nProvide a comprehensive, refined answer.";
        var final = await llm.CompleteAsync([HermesAgent.Core.Models.Message.User(aggregationPrompt)], null, ct);
        return $"[MoA Result]\n{final.Content}";
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// SEND MESSAGE TOOLSET  (1 tool)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Send a message to a connected messaging platform.</summary>
public sealed class SendMessageTool : ToolBase
{
    public override string Name => "send_message";
    public override string Description => "Send a message to a connected messaging platform, or list available targets. Call with action='list' first to see available targets.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["action"]   = new() { Type = "string", Description = "send or list", Enum = ["send", "list"] },
            ["platform"] = new() { Type = "string", Description = "Platform: telegram, discord, slack, email" },
            ["target"]   = new() { Type = "string", Description = "Channel/user/chat ID or name" },
            ["message"]  = new() { Type = "string", Description = "Message content to send" }
        },
        Required = ["action"]
    };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        var action = GetArg(call, "action");
        if (action == "list")
            return Task.FromResult("Available messaging targets: (configure via hermes gateway)");
        return Task.FromResult($"Message queued to {GetArgOrDefault(call, "platform", "default")} / {GetArgOrDefault(call, "target", "default")} (configure via hermes gateway)");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// TEXT-TO-SPEECH TOOLSET  (1 tool)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Convert text to speech audio.</summary>
public sealed class TextToSpeechTool : ToolBase
{
    public override string Name => "text_to_speech";
    public override string Description => "Convert text to speech audio. Returns a MEDIA: path. On Telegram plays as voice bubble, CLI saves to ~/voice-memos/.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["text"]     = new() { Type = "string", Description = "Text to convert to speech" },
            ["voice"]    = new() { Type = "string", Description = "Voice ID or name (provider-dependent)" },
            ["provider"] = new() { Type = "string", Description = "TTS provider (openai, elevenlabs, etc.)" }
        },
        Required = ["text"]
    };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        var text = GetArg(call, "text");
        var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "voice-memos", $"{Guid.NewGuid():N}.mp3");
        return Task.FromResult($"TTS: \"{text[..Math.Min(50, text.Length)]}...\" → MEDIA:{outputPath} (requires TTS provider configuration)");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// PATCH TOOL  (replaces/improves write_file for targeted edits)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Targeted find-and-replace edits in files with fuzzy matching.</summary>
public sealed class PatchTool : ToolBase
{
    public override string Name => "patch";
    public override string Description => "Targeted find-and-replace edits in files. Use instead of sed/awk in terminal. Fuzzy matching handles minor whitespace/indentation differences. Returns unified diff.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition>
        {
            ["path"]        = new() { Type = "string", Description = "File path to edit" },
            ["old_content"] = new() { Type = "string", Description = "Exact content to find (fuzzy matched)" },
            ["new_content"] = new() { Type = "string", Description = "Replacement content" }
        },
        Required = ["path", "old_content", "new_content"]
    };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        var path = GetArg(call, "path");
        var oldContent = GetArg(call, "old_content");
        var newContent = GetArg(call, "new_content");

        if (!File.Exists(path)) return $"Error: '{path}' not found.";
        var text = await File.ReadAllTextAsync(path, ct);

        // Try exact match first
        if (text.Contains(oldContent))
        {
            var patched = text.Replace(oldContent, newContent);
            await File.WriteAllTextAsync(path, patched, ct);
            return $"Patched {path}: replaced {oldContent.Length} chars with {newContent.Length} chars.";
        }

        // Fuzzy: try ignoring leading whitespace per line
        var normalizedOld = string.Join('\n', oldContent.Split('\n').Select(l => l.TrimStart()));
        var normalizedText = string.Join('\n', text.Split('\n').Select(l => l.TrimStart()));
        if (normalizedText.Contains(normalizedOld))
        {
            var patched = text.Replace(oldContent.TrimStart(), newContent);
            await File.WriteAllTextAsync(path, patched, ct);
            return $"Patched {path} (fuzzy match): replaced content.";
        }

        return $"Error: Could not find the specified content in '{path}'. Verify the old_content matches the file exactly.";
    }
}
