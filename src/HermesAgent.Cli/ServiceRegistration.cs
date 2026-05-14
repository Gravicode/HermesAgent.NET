using HermesAgent.Agent;
using HermesAgent.Agent.Providers;
using HermesAgent.Core.Abstractions;
using HermesAgent.Core.Configuration;
using HermesAgent.Memory;
using HermesAgent.Memory.Sqlite;
using HermesAgent.Skills;
using HermesAgent.Tools;
using HermesAgent.Tools.Toolsets;
using Microsoft.Extensions.DependencyInjection;

namespace HermesAgent.Cli;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHermes(this IServiceCollection services)
    {
        services.AddHttpClient<OpenAiCompatibleProvider>();
        services.AddSingleton<ILlmProvider, OpenAiCompatibleProvider>();
        services.AddSingleton<ISystemPromptBuilder, SystemPromptBuilder>();
        services.AddSingleton<IContextCompressor, SlidingWindowContextCompressor>();
        
        services.AddTransient<IAgent, HermesAgentLoop>();
        services.AddTransient<HermesAgentLoop>();

        // Use SQLite for production feel
        services.AddSingleton<SqliteSessionStore>();
        services.AddSingleton<ISessionManager>(sp => sp.GetRequiredService<SqliteSessionStore>());
        services.AddSingleton<IMemoryStore>(sp => sp.GetRequiredService<SqliteSessionStore>());
        
        services.AddSingleton<ISkillManager, FileSkillManager>();

        services.AddSingleton<MemoryTools>();
        services.AddSingleton<SkillTools>();

        // Core Tools (HermesAgent.Tools)
        services.AddSingleton<ShellTool>();
        services.AddSingleton<ReadFileTool>();
        services.AddSingleton<WriteFileTool>();
        services.AddSingleton<ListDirectoryTool>();
        services.AddSingleton<SearchFilesTool>();
        services.AddHttpClient<WebFetchTool>();
        services.AddSingleton<WebFetchTool>();

        // Advanced Toolsets (HermesAgent.Tools.Toolsets)
        services.AddSingleton<PatchTool>();
        services.AddHttpClient<WebSearchTool>();
        services.AddSingleton<WebSearchTool>();
        services.AddHttpClient<WebExtractTool>();
        services.AddSingleton<WebExtractTool>();
        services.AddSingleton<VisionAnalyzeTool>();
        services.AddSingleton<ClarifyTool>();
        services.AddSingleton<TodoTool>();
        services.AddHttpClient<ImageGenerateTool>();
        services.AddSingleton<ImageGenerateTool>();
        services.AddSingleton<MixtureOfAgentsTool>();
        services.AddSingleton<SendMessageTool>();
        services.AddSingleton<TextToSpeechTool>();
        services.AddSingleton<CronJobTool>();
        services.AddSingleton<DelegateTaskTool>();
        services.AddSingleton<ExecuteCodeTool>();
        services.AddSingleton<SessionSearchTool>();
        services.AddSingleton<ProcessTool>();

        // Browser Toolsets
        services.AddSingleton<BrowserNavigateTool>();
        services.AddSingleton<BrowserSnapshotTool>();
        services.AddSingleton<BrowserClickTool>();
        services.AddSingleton<BrowserTypeTool>();
        services.AddSingleton<BrowserPressTool>();
        services.AddSingleton<BrowserScrollTool>();
        services.AddSingleton<BrowserBackTool>();
        services.AddSingleton<BrowserConsoleTool>();
        services.AddSingleton<BrowserGetImagesTool>();
        services.AddSingleton<BrowserVisionTool>();
        services.AddSingleton<BrowserDialogTool>();
        services.AddSingleton<BrowserCdpTool>();

        services.AddSingleton<IEnumerable<ITool>>(sp =>
        {
            var tools = new List<ITool>
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
                sp.GetRequiredService<VisionAnalyzeTool>(),
                sp.GetRequiredService<ClarifyTool>(),
                sp.GetRequiredService<TodoTool>(),
                sp.GetRequiredService<ImageGenerateTool>(),
                sp.GetRequiredService<MixtureOfAgentsTool>(),
                sp.GetRequiredService<SendMessageTool>(),
                sp.GetRequiredService<TextToSpeechTool>(),
                sp.GetRequiredService<CronJobTool>(),
                sp.GetRequiredService<DelegateTaskTool>(),
                sp.GetRequiredService<ExecuteCodeTool>(),
                sp.GetRequiredService<SessionSearchTool>(),
                sp.GetRequiredService<ProcessTool>(),

                sp.GetRequiredService<BrowserNavigateTool>(),
                sp.GetRequiredService<BrowserSnapshotTool>(),
                sp.GetRequiredService<BrowserClickTool>(),
                sp.GetRequiredService<BrowserTypeTool>(),
                sp.GetRequiredService<BrowserPressTool>(),
                sp.GetRequiredService<BrowserScrollTool>(),
                sp.GetRequiredService<BrowserBackTool>(),
                sp.GetRequiredService<BrowserConsoleTool>(),
                sp.GetRequiredService<BrowserGetImagesTool>(),
                sp.GetRequiredService<BrowserVisionTool>(),
                sp.GetRequiredService<BrowserDialogTool>(),
                sp.GetRequiredService<BrowserCdpTool>()
            };
            tools.AddRange(sp.GetRequiredService<MemoryTools>().GetTools());
            tools.AddRange(sp.GetRequiredService<SkillTools>().GetTools());
            return tools;
        });

        return services;
    }
}
