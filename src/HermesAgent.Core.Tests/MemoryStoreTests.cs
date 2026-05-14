using FluentAssertions;
using HermesAgent.Memory;
using HermesAgent.Core.Configuration;
using HermesAgent.Core.Models;
using HermesAgent.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace HermesAgent.Core.Tests;

public class MemoryStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileMemoryStore _store;

    public MemoryStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hermes_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var options = Options.Create(new HermesOptions { DataDirectory = _tempDir });
        var logger = Substitute.For<ILogger<FileMemoryStore>>();
        _store = new FileMemoryStore(options, logger);
    }

    [Fact]
    public async Task SaveAndLoadMemory_Works()
    {
        await _store.SaveMemoryAsync("TEST", "Some content");
        var content = await _store.LoadMemoryAsync("TEST");
        content.Should().Be("Some content");
    }

    [Fact]
    public async Task SearchAsync_FindsRelevantContent()
    {
        await _store.SaveMemoryAsync("FRUITS", "Apple, Banana, Orange");
        await _store.SaveMemoryAsync("VEGGIES", "Carrot, Potato");

        var results = await _store.SearchAsync("Banana");
        
        results.Should().NotBeEmpty();
        results[0].Key.Should().Be("FRUITS");
        results[0].Content.Should().Contain("Apple");
    }

    [Fact]
    public async Task AppendMemoryAsync_AppendsToMemoryFile()
    {
        await _store.AppendMemoryAsync("First entry");
        await _store.AppendMemoryAsync("Second entry");

        var content = await _store.LoadMemoryAsync("MEMORY");
        content.Should().Contain("First entry");
        content.Should().Contain("Second entry");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}

public class SessionManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileSessionManager _manager;

    public SessionManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hermes_session_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var options = Options.Create(new HermesOptions { DataDirectory = _tempDir });
        var llm = Substitute.For<ILlmProvider>();
        var logger = Substitute.For<ILogger<FileSessionManager>>();
        _manager = new FileSessionManager(options, llm, logger);
    }

    [Fact]
    public async Task SaveAndLoadSession_PreservesMessages()
    {
        var conv = new Conversation();
        conv.AddMessage(Message.User("Ping"));
        conv.AddMessage(Message.Assistant("Pong"));
        conv.Title = "Test Session";

        await _manager.SaveSessionAsync(conv);

        var loaded = await _manager.LoadSessionAsync(conv.Id);
        
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(conv.Id);
        loaded.Title.Should().Be("Test Session");
        loaded.Messages.Should().HaveCount(2);
        loaded.Messages[0].Content.Should().Be("Ping");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
