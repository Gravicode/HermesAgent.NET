using HermesAgent.Agent;
using HermesAgent.Agent.Providers;
using HermesAgent.Core.Abstractions;
using HermesAgent.Core.Configuration;
using HermesAgent.Gateway;
using HermesAgent.Memory;
using HermesAgent.Skills;
using HermesAgent.Tools;
using HermesAgent.Tools.Toolsets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var userHermesDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".hermes");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile(Path.Combine(userHermesDir, "config.json"), optional: true)
            .AddEnvironmentVariables("HERMES_")
            .AddCommandLine(args);
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<HermesOptions>(
            ctx.Configuration.GetSection(HermesOptions.SectionName));
        services.Configure<GatewayOptions>(
            ctx.Configuration.GetSection(GatewayOptions.SectionName));

        // Use core shared registration
        services.AddHermesCore();

        // Gateway specific registrations
        services.AddHermesGateway();
    })
    .Build();

Console.WriteLine("""
    ╔═══════════════════════════════════╗
    ║   Hermes Agent — Gateway Mode     ║
    ╚═══════════════════════════════════╝
    Platforms: Telegram · Discord · Webhook
    """);

await host.RunAsync();

public static class GatewaySharedExtensions
{
    public static IServiceCollection AddHermesCore(this IServiceCollection services)
    {
        services.AddHttpClient<OpenAiCompatibleProvider>();
        services.AddSingleton<ILlmProvider, OpenAiCompatibleProvider>();
        services.AddSingleton<ISystemPromptBuilder, SystemPromptBuilder>();
        services.AddSingleton<IContextCompressor, SlidingWindowContextCompressor>();
        
        services.AddTransient<IAgent, HermesAgentLoop>();
        services.AddTransient<HermesAgentLoop>();

        services.AddSingleton<ISessionManager, FileSessionManager>();
        services.AddSingleton<IMemoryStore, FileMemoryStore>();
        services.AddSingleton<ISkillManager, FileSkillManager>();

        services.AddSingleton<MemoryTools>();
        services.AddSingleton<SkillTools>();
        
        services.AddSingleton<ShellTool>();
        services.AddSingleton<ReadFileTool>();
        services.AddSingleton<WriteFileTool>();
        services.AddSingleton<PatchTool>();
        services.AddSingleton<ListDirectoryTool>();
        services.AddSingleton<SearchFilesTool>();
        services.AddHttpClient<WebFetchTool>();
        services.AddHttpClient<WebSearchTool>();
        services.AddHttpClient<WebExtractTool>();
        services.AddSingleton<WebFetchTool>();
        services.AddSingleton<WebSearchTool>();
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
                sp.GetRequiredService<PatchTool>(),
                sp.GetRequiredService<ListDirectoryTool>(),
                sp.GetRequiredService<SearchFilesTool>(),
                sp.GetRequiredService<WebFetchTool>(),
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
