using FluentAssertions;
using HermesAgent.Cli.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HermesAgent.Core.Tests;

/// <summary>
/// Tests for <see cref="AppConfigConfigurationProvider"/> and
/// <see cref="AppConfigConfigurationSource"/>.
/// </summary>
public class AppConfigProviderTests : IDisposable
{
    // Temp files created per test; cleaned up in Dispose
    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var f in _tempFiles)
            if (File.Exists(f)) File.Delete(f);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private string WriteTempConfig(string xml)
    {
        var path = Path.GetTempFileName() + ".config";
        File.WriteAllText(path, xml);
        _tempFiles.Add(path);
        return path;
    }

    private static IConfiguration BuildConfig(string path, bool optional = false)
        => new ConfigurationBuilder()
            .AddAppConfig(path, optional: optional)
            .Build();

    // ─── Format 1: <appSettings> with colon keys ────────────────────────────

    [Fact]
    public void Format1_ColonKeys_AreMappedVerbatim()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="Hermes:Llm:Provider" value="openai" />
                <add key="Hermes:Llm:ApiKey"   value="sk-test-123" />
                <add key="Hermes:Agent:MaxTurns" value="42" />
              </appSettings>
            </configuration>
            """);

        var config = BuildConfig(path);

        config["Hermes:Llm:Provider"].Should().Be("openai");
        config["Hermes:Llm:ApiKey"].Should().Be("sk-test-123");
        config["Hermes:Agent:MaxTurns"].Should().Be("42");
    }

    [Fact]
    public void Format1_AllLlmOptions_AreRead()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="Hermes:Llm:Provider"       value="openrouter" />
                <add key="Hermes:Llm:Model"          value="gpt-4o" />
                <add key="Hermes:Llm:ApiKey"         value="sk-or-abc" />
                <add key="Hermes:Llm:BaseUrl"        value="https://openrouter.ai/api" />
                <add key="Hermes:Llm:Temperature"    value="0.9" />
                <add key="Hermes:Llm:MaxTokens"      value="8192" />
                <add key="Hermes:Llm:TimeoutSeconds" value="60" />
                <add key="Hermes:Llm:MaxRetries"     value="5" />
              </appSettings>
            </configuration>
            """);

        var config = BuildConfig(path);

        config["Hermes:Llm:Provider"].Should().Be("openrouter");
        config["Hermes:Llm:Model"].Should().Be("gpt-4o");
        config["Hermes:Llm:ApiKey"].Should().Be("sk-or-abc");
        config["Hermes:Llm:BaseUrl"].Should().Be("https://openrouter.ai/api");
        config["Hermes:Llm:Temperature"].Should().Be("0.9");
        config["Hermes:Llm:MaxTokens"].Should().Be("8192");
        config["Hermes:Llm:TimeoutSeconds"].Should().Be("60");
        config["Hermes:Llm:MaxRetries"].Should().Be("5");
    }

    [Fact]
    public void Format1_AllAgentOptions_AreRead()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="Hermes:Agent:MaxTurns"                value="25" />
                <add key="Hermes:Agent:MaxContextTokens"        value="50000" />
                <add key="Hermes:Agent:AutoCompressContext"     value="false" />
                <add key="Hermes:Agent:CompressThresholdTokens" value="40000" />
                <add key="Hermes:Agent:EnableSkillNudging"      value="false" />
                <add key="Hermes:Agent:SkillNudgeIntervalTurns" value="5" />
              </appSettings>
            </configuration>
            """);

        var config = BuildConfig(path);

        config["Hermes:Agent:MaxTurns"].Should().Be("25");
        config["Hermes:Agent:AutoCompressContext"].Should().Be("false");
        config["Hermes:Agent:EnableSkillNudging"].Should().Be("false");
        config["Hermes:Agent:SkillNudgeIntervalTurns"].Should().Be("5");
    }

    // ─── Format 2: dot-notation keys ────────────────────────────────────────

    [Fact]
    public void Format2_DotKeys_AreNormalizedToColonHierarchy()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="hermes.llm.provider" value="nous" />
                <add key="hermes.llm.apiKey"   value="sk-nous-456" />
                <add key="hermes.agent.maxTurns" value="30" />
              </appSettings>
            </configuration>
            """);

        var config = BuildConfig(path);

        config["Hermes:Llm:Provider"].Should().Be("nous");
        config["Hermes:Llm:ApiKey"].Should().Be("sk-nous-456");
        config["Hermes:Agent:MaxTurns"].Should().Be("30");
    }

    [Fact]
    public void Format2_SnakeCaseDotKeys_AreNormalized()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="hermes.agent.max_turns"            value="20" />
                <add key="hermes.agent.enable_skill_nudging" value="true" />
              </appSettings>
            </configuration>
            """);

        var config = BuildConfig(path);

        config["Hermes:Agent:MaxTurns"].Should().Be("20");
        config["Hermes:Agent:EnableSkillNudging"].Should().Be("true");
    }

    [Fact]
    public void Format2_DoubleUnderscoreKeys_AreNormalized()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="Hermes__Llm__Provider" value="nvidia" />
                <add key="Hermes__Llm__Model"    value="meta/llama-3-70b" />
              </appSettings>
            </configuration>
            """);

        var config = BuildConfig(path);

        config["Hermes:Llm:Provider"].Should().Be("nvidia");
        config["Hermes:Llm:Model"].Should().Be("meta/llama-3-70b");
    }

    // ─── Format 3: <hermesSettings> structured block ────────────────────────

    [Fact]
    public void Format3_HermesSettings_LlmAttributes_AreMapped()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <hermesSettings>
                <llm provider="anthropic" model="claude-sonnet-4-6" apiKey="sk-ant-xyz" temperature="0.5" />
              </hermesSettings>
            </configuration>
            """);

        var config = BuildConfig(path);

        config["Hermes:Llm:Provider"].Should().Be("anthropic");
        config["Hermes:Llm:Model"].Should().Be("claude-sonnet-4-6");
        config["Hermes:Llm:ApiKey"].Should().Be("sk-ant-xyz");
        config["Hermes:Llm:Temperature"].Should().Be("0.5");
    }

    [Fact]
    public void Format3_HermesSettings_AllSections_AreMapped()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <hermesSettings>
                <llm provider="openai" model="gpt-4o" apiKey="sk-fmt3" />
                <agent maxTurns="15" enableSkillNudging="false" />
                <memory enabled="true" enableSessionSearch="false" />
                <skills enabled="true" autoCreateSkills="false" />
              </hermesSettings>
            </configuration>
            """);

        var config = BuildConfig(path);

        config["Hermes:Llm:Provider"].Should().Be("openai");
        config["Hermes:Agent:MaxTurns"].Should().Be("15");
        config["Hermes:Agent:EnableSkillNudging"].Should().Be("false");
        config["Hermes:Memory:Enabled"].Should().Be("true");
        config["Hermes:Memory:EnableSessionSearch"].Should().Be("false");
        config["Hermes:Skills:Enabled"].Should().Be("true");
        config["Hermes:Skills:AutoCreateSkills"].Should().Be("false");
    }

    // ─── connectionStrings ───────────────────────────────────────────────────

    [Fact]
    public void ConnectionStrings_AreMappedUnderHermesNamespace()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <connectionStrings>
                <add name="VectorDb" connectionString="Host=localhost;Database=hermes_vectors" />
                <add name="SessionDb" connectionString="Data Source=sessions.db" />
              </connectionStrings>
            </configuration>
            """);

        var config = BuildConfig(path);

        config["Hermes:ConnectionStrings:VectorDb"].Should().Be("Host=localhost;Database=hermes_vectors");
        config["Hermes:ConnectionStrings:SessionDb"].Should().Be("Data Source=sessions.db");
    }

    // ─── Mixed formats in one file ───────────────────────────────────────────

    [Fact]
    public void MixedFormats_InSingleFile_AllKeysRead()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="Hermes:Llm:Provider" value="openai" />
                <add key="hermes.llm.model"    value="gpt-4o-mini" />
              </appSettings>
              <hermesSettings>
                <agent maxTurns="99" />
              </hermesSettings>
            </configuration>
            """);

        var config = BuildConfig(path);

        config["Hermes:Llm:Provider"].Should().Be("openai");
        config["Hermes:Llm:Model"].Should().Be("gpt-4o-mini");
        config["Hermes:Agent:MaxTurns"].Should().Be("99");
    }

    // ─── Key normalization unit tests ────────────────────────────────────────

    [Theory]
    [InlineData("Hermes:Llm:ApiKey",          "Hermes:Llm:ApiKey")]   // already correct
    [InlineData("hermes:llm:apiKey",           "Hermes:Llm:ApiKey")]   // colon, lowercase
    [InlineData("hermes.llm.apiKey",           "Hermes:Llm:ApiKey")]   // dot notation
    [InlineData("hermes.llm.api_key",          "Hermes:Llm:ApiKey")]   // dot + snake_case
    [InlineData("Hermes__Llm__ApiKey",         "Hermes:Llm:ApiKey")]   // double-underscore
    [InlineData("hermes__llm__apiKey",         "Hermes:Llm:ApiKey")]   // double-underscore lowercase
    [InlineData("Hermes:Agent:MaxTurns",       "Hermes:Agent:MaxTurns")]
    [InlineData("hermes.agent.maxTurns",       "Hermes:Agent:MaxTurns")]
    [InlineData("hermes.agent.max_turns",      "Hermes:Agent:MaxTurns")]
    [InlineData("Hermes__Agent__MaxTurns",     "Hermes:Agent:MaxTurns")]
    [InlineData("Hermes:Memory:Enabled",       "Hermes:Memory:Enabled")]
    [InlineData("hermes.memory.enabled",       "Hermes:Memory:Enabled")]
    public void NormalizeKey_VariousFormats_ProduceCanonicalKey(string input, string expected)
    {
        var result = AppConfigConfigurationProvider.NormalizeKey(input);
        result.Should().Be(expected);
    }

    // ─── Error handling ──────────────────────────────────────────────────────

    [Fact]
    public void MissingFile_Optional_DoesNotThrow()
    {
        var act = () => BuildConfig("/nonexistent/path/app.config", optional: true);
        act.Should().NotThrow();
    }

    [Fact]
    public void MissingFile_NotOptional_ThrowsFileNotFoundException()
    {
        var act = () => BuildConfig("/nonexistent/path/app.config", optional: false);
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void InvalidXml_ThrowsInvalidDataException()
    {
        var path = WriteTempConfig("this is not xml at all <<<>>>");
        var act = () => BuildConfig(path, optional: false);
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*invalid XML*");
    }

    [Fact]
    public void EmptyXml_ReturnsEmptyConfig()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration />
            """);

        var config = BuildConfig(path);
        // Should not throw; just no keys
        config.AsEnumerable().Should().BeEmpty();
    }

    [Fact]
    public void AddKeyWithEmptyKey_IsSkipped()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key=""              value="should-be-skipped" />
                <add key="   "           value="also-skipped" />
                <add key="Hermes:Llm:Model" value="kept" />
              </appSettings>
            </configuration>
            """);

        var config = BuildConfig(path);
        config["Hermes:Llm:Model"].Should().Be("kept");
        // Ensure no empty/whitespace key sneaked in
        config.AsEnumerable()
            .Select(kv => kv.Key)
            .Should().NotContain(k => string.IsNullOrWhiteSpace(k));
    }

    // ─── IConfiguration layering ─────────────────────────────────────────────

    [Fact]
    public void AppConfig_IsOverriddenBy_InMemoryProvider()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="Hermes:Llm:Provider" value="from-app-config" />
              </appSettings>
            </configuration>
            """);

        var config = new ConfigurationBuilder()
            .AddAppConfig(path, optional: false)
            // In-memory added after → wins
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hermes:Llm:Provider"] = "from-in-memory"
            })
            .Build();

        config["Hermes:Llm:Provider"].Should().Be("from-in-memory");
    }

    [Fact]
    public void AppConfig_Overrides_EarlierJsonFile()
    {
        // Simulates: appsettings.json loaded first, then app.config overrides
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="Hermes:Llm:Provider" value="from-app-config" />
              </appSettings>
            </configuration>
            """);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hermes:Llm:Provider"] = "from-json"
            })
            // app.config added after → wins over JSON
            .AddAppConfig(path, optional: false)
            .Build();

        config["Hermes:Llm:Provider"].Should().Be("from-app-config");
    }

    // ─── IOptions<HermesOptions> binding ────────────────────────────────────

    [Fact]
    public void AppConfig_BindsCorrectly_ToHermesOptions()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="Hermes:Llm:Provider"    value="openrouter" />
                <add key="Hermes:Llm:Model"       value="gpt-4o" />
                <add key="Hermes:Llm:ApiKey"      value="sk-binding-test" />
                <add key="Hermes:Llm:Temperature" value="0.3" />
                <add key="Hermes:Agent:MaxTurns"  value="77" />
              </appSettings>
            </configuration>
            """);

        var config = BuildConfig(path);

        var options = new HermesAgent.Core.Configuration.HermesOptions();
        config.GetSection(HermesAgent.Core.Configuration.HermesOptions.SectionName).Bind(options);

        options.Llm.Provider.Should().Be("openrouter");
        options.Llm.Model.Should().Be("gpt-4o");
        options.Llm.ApiKey.Should().Be("sk-binding-test");
        options.Llm.Temperature.Should().BeApproximately(0.3f, 0.001f);
        options.Agent.MaxTurns.Should().Be(77);
    }

    [Fact]
    public void Format3_BindsCorrectly_ToHermesOptions()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <hermesSettings>
                <llm provider="nous" model="hermes-3-70b" apiKey="sk-nous-bind" temperature="0.8" maxTokens="8192" />
                <agent maxTurns="10" enableSkillNudging="false" />
                <memory enabled="false" />
              </hermesSettings>
            </configuration>
            """);

        var config = BuildConfig(path);
        var options = new HermesAgent.Core.Configuration.HermesOptions();
        config.GetSection(HermesAgent.Core.Configuration.HermesOptions.SectionName).Bind(options);

        options.Llm.Provider.Should().Be("nous");
        options.Llm.Model.Should().Be("hermes-3-70b");
        options.Llm.MaxTokens.Should().Be(8192);
        options.Agent.MaxTurns.Should().Be(10);
        options.Agent.EnableSkillNudging.Should().BeFalse();
        options.Memory.Enabled.Should().BeFalse();
    }

    // ─── Reload-on-change ────────────────────────────────────────────────────

    [Fact]
    public async Task ReloadOnChange_WhenFileUpdated_ConfigIsRefreshed()
    {
        var path = WriteTempConfig("""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="Hermes:Llm:Model" value="gpt-4o" />
              </appSettings>
            </configuration>
            """);

        var config = new ConfigurationBuilder()
            .AddAppConfig(path, optional: false, reloadOnChange: true)
            .Build();

        config["Hermes:Llm:Model"].Should().Be("gpt-4o");

        // Update the file
        File.WriteAllText(path, """
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <appSettings>
                <add key="Hermes:Llm:Model" value="gpt-4o-mini" />
              </appSettings>
            </configuration>
            """);

        // Allow watcher debounce + reload time
        await Task.Delay(600);

        config["Hermes:Llm:Model"].Should().Be("gpt-4o-mini");
    }
}
