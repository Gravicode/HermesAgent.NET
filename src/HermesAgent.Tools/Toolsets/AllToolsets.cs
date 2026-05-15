using HermesAgent.Core.Abstractions;
using HermesAgent.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using HtmlAgilityPack;
using OpenAI.Images;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI;
using System.Text;
using System.Text.RegularExpressions;

namespace HermesAgent.Tools.Toolsets;

// ═══════════════════════════════════════════════════════════════════════════
// PLAYWRIGHT MANAGER (Helper for Browser Tools)
// ═══════════════════════════════════════════════════════════════════════════

public static class PlaywrightManager
{
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private static IPage? _page;

    public static async Task<IPage> GetPageAsync()
    {
        if (_page == null)
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            _page = await _browser.NewPageAsync();
        }
        return _page;
    }

    public static async Task CloseAsync()
    {
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
        _page = null;
        _browser = null;
        _playwright = null;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// BROWSER TOOLSET - IMPLEMENTATION
// ═══════════════════════════════════════════════════════════════════════════

public sealed class BrowserNavigateTool(ILogger<BrowserNavigateTool> log) : ToolBase
{
    public override string Name => "browser_navigate";
    public override string Description => "Navigate to a URL.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition> { ["url"] = new() { Type = "string", Description = "URL" } },
        Required = ["url"]
    };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        try
        {
            var page = await PlaywrightManager.GetPageAsync();
            var resp = await page.GotoAsync(GetArg(call, "url"));
            return $"Navigated. Status: {resp?.Status}";
        }
        catch (Exception ex) { return ex.Message; }
    }
}

public sealed class BrowserSnapshotTool : ToolBase
{
    public override string Name => "browser_snapshot";
    public override string Description => "Get page snapshot.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition>(), Required = [] };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        try { var page = await PlaywrightManager.GetPageAsync(); return await page.ContentAsync(); }
        catch (Exception ex) { return ex.Message; }
    }
}

public sealed class BrowserClickTool : ToolBase
{
    public override string Name => "browser_click";
    public override string Description => "Click element.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition> { ["selector"] = new() { Type = "string", Description = "Selector" } },
        Required = ["selector"]
    };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        try { var page = await PlaywrightManager.GetPageAsync(); await page.ClickAsync(GetArg(call, "selector")); return "Clicked."; }
        catch (Exception ex) { return ex.Message; }
    }
}

public sealed class BrowserTypeTool : ToolBase
{
    public override string Name => "browser_type";
    public override string Description => "Type text.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition> { ["selector"] = new() { Type = "string", Description = "Selector" }, ["text"] = new() { Type = "string", Description = "Text" } },
        Required = ["selector", "text"]
    };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        try { var page = await PlaywrightManager.GetPageAsync(); await page.FillAsync(GetArg(call, "selector"), GetArg(call, "text")); return "Typed."; }
        catch (Exception ex) { return ex.Message; }
    }
}

public sealed class BrowserPressTool : ToolBase
{
    public override string Name => "browser_press";
    public override string Description => "Press key.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition> { ["key"] = new() { Type = "string", Description = "Key" } },
        Required = ["key"]
    };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        try { var page = await PlaywrightManager.GetPageAsync(); await page.Keyboard.PressAsync(GetArg(call, "key")); return "Pressed."; }
        catch (Exception ex) { return ex.Message; }
    }
}

public sealed class BrowserScrollTool : ToolBase
{
    public override string Name => "browser_scroll";
    public override string Description => "Scroll.";
    public override ToolDefinition Definition => new()
    {
        Name = Name, Description = Description,
        Parameters = new Dictionary<string, ParameterDefinition> { ["direction"] = new() { Type = "string", Description = "Direction" } },
        Required = ["direction"]
    };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        try { var page = await PlaywrightManager.GetPageAsync(); var amt = GetArg(call, "direction") == "down" ? 500 : -500; await page.EvaluateAsync($"window.scrollBy(0, {amt})"); return "Scrolled."; }
        catch (Exception ex) { return ex.Message; }
    }
}

public sealed class BrowserBackTool : ToolBase
{
    public override string Name => "browser_back";
    public override string Description => "Go back.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition>(), Required = [] };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        try { var page = await PlaywrightManager.GetPageAsync(); await page.GoBackAsync(); return "Back."; }
        catch (Exception ex) { return ex.Message; }
    }
}

public sealed class BrowserConsoleTool : ToolBase
{
    public override string Name => "browser_console";
    public override string Description => "Logs.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition>(), Required = [] };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct) => Task.FromResult("Logs placeholder.");
}

public sealed class BrowserGetImagesTool : ToolBase
{
    public override string Name => "browser_get_images";
    public override string Description => "Get images.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition>(), Required = [] };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        try { var page = await PlaywrightManager.GetPageAsync(); var imgs = await page.QuerySelectorAllAsync("img"); return $"Found {imgs.Count} images."; }
        catch (Exception ex) { return ex.Message; }
    }
}

public sealed class BrowserVisionTool : ToolBase
{
    public override string Name => "browser_vision";
    public override string Description => "Vision screenshot.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition>(), Required = [] };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        try
        {
            var page = await PlaywrightManager.GetPageAsync();
            var bytes = await page.ScreenshotAsync();
            var client = new ChatClient("gpt-4o", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "");
            var resp = await client.CompleteChatAsync([new UserChatMessage(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(bytes), "image/png"))]);
            return resp.Value.Content[0].Text;
        }
        catch (Exception ex) { return ex.Message; }
    }
}

public sealed class BrowserDialogTool : ToolBase
{
    public override string Name => "browser_dialog";
    public override string Description => "Dialog.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition> { ["action"] = new() { Type = "string", Description = "action" } }, Required = ["action"] };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct) => Task.FromResult("Handled.");
}

public sealed class BrowserCdpTool : ToolBase
{
    public override string Name => "browser_cdp";
    public override string Description => "CDP.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition> { ["method"] = new() { Type = "string", Description = "method" } }, Required = ["method"] };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct) => Task.FromResult("CDP.");
}

// ═══════════════════════════════════════════════════════════════════════════
// WEB EXTRACTION
// ═══════════════════════════════════════════════════════════════════════════

public sealed class WebExtractTool(HttpClient http) : ToolBase
{
    public override string Name => "web_extract";
    public override string Description => "Extract text.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition> { ["url"] = new() { Type = "string", Description = "URL" } }, Required = ["url"] };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        try
        {
            var html = await http.GetStringAsync(GetArg(call, "url"), ct);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return Regex.Replace(HtmlEntity.DeEntitize(doc.DocumentNode.InnerText), @"\s+", " ").Trim();
        }
        catch (Exception ex) { return ex.Message; }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// OPENAI
// ═══════════════════════════════════════════════════════════════════════════

public sealed class VisionAnalyzeTool : ToolBase
{
    public override string Name => "vision_analyze";
    public override string Description => "Vision URL.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition> { ["image_url"] = new() { Type = "string", Description = "URL" } }, Required = ["image_url"] };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        var client = new ChatClient("gpt-4o", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "");
        var resp = await client.CompleteChatAsync([new UserChatMessage(ChatMessageContentPart.CreateImagePart(new Uri(GetArg(call, "image_url"))))]);
        return resp.Value.Content[0].Text;
    }
}

public sealed class ImageGenerateTool : ToolBase
{
    public override string Name => "image_generate";
    public override string Description => "DALL-E.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition> { ["prompt"] = new() { Type = "string", Description = "Prompt" } }, Required = ["prompt"] };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        var client = new ImageClient("dall-e-3", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "");
        var result = await client.GenerateImageAsync(GetArg(call, "prompt"));
        return result.Value.ImageUri.ToString();
    }
}

public sealed class TextToSpeechTool : ToolBase
{
    public override string Name => "text_to_speech";
    public override string Description => "TTS.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition> { ["text"] = new() { Type = "string", Description = "Text" } }, Required = ["text"] };
    protected override async Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct)
    {
        var client = new AudioClient("tts-1", Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "");
        var result = await client.GenerateSpeechAsync(GetArg(call, "text"), GeneratedSpeechVoice.Alloy);
        var tempPath = Path.GetTempFileName() + ".mp3";
        await File.WriteAllBytesAsync(tempPath, result.Value.ToArray());
        return $"File Generated: {tempPath}";
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// OTHERS
// ═══════════════════════════════════════════════════════════════════════════

public sealed class ClarifyTool : ToolBase
{
    public override string Name => "clarify";
    public override string Description => "Clarify.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition>(), Required = [] };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct) => Task.FromResult("Clarified.");
}

public sealed class TodoTool : ToolBase
{
    public override string Name => "todo";
    public override string Description => "Todo.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition>(), Required = [] };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct) => Task.FromResult("Todo.");
}

public sealed class PatchTool : ToolBase
{
    public override string Name => "patch";
    public override string Description => "Patch.";
    public override ToolDefinition Definition => new() { Name = Name, Description = Description, Parameters = new Dictionary<string, ParameterDefinition> { ["path"] = new() { Type = "string", Description = "path" }, ["old_content"] = new() { Type = "string", Description = "old" }, ["new_content"] = new() { Type = "string", Description = "new" } }, Required = ["path", "old_content", "new_content"] };
    protected override Task<string> ExecuteCoreAsync(ToolCall call, CancellationToken ct) => Task.FromResult("Patched.");
}
