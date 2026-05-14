using FluentAssertions;
using HermesAgent.Core.Models;
using Xunit;

namespace HermesAgent.Core.Tests;

public class CoreModelTests
{
    [Fact]
    public void Conversation_Constructor_InitializesId()
    {
        var conv = new Conversation();
        conv.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Conversation_ConstructorWithId_SetsId()
    {
        var id = Guid.NewGuid();
        var conv = new Conversation(id);
        conv.Id.Should().Be(id);
    }

    [Fact]
    public void Conversation_AddMessage_UpdatesTimestamp()
    {
        var conv = new Conversation();
        var initialUpdate = conv.UpdatedAt;
        
        Thread.Sleep(10); // Ensure time passes
        conv.AddMessage(Message.User("test"));
        
        conv.UpdatedAt.Should().BeAfter(initialUpdate);
    }

    [Fact]
    public void Message_ToolResult_CreatesMetadata()
    {
        var msg = Message.ToolResult("search", "results found");
        msg.Role.Should().Be("tool");
        msg.Content.Should().Be("results found");
        msg.Metadata.Should().NotBeNull();
        msg.Metadata!["tool_name"].Should().Be("search");
    }

    [Fact]
    public void LlmUsage_TotalTokens_CalculationIsCorrect()
    {
        var usage = new LlmUsage(100, 250);
        usage.TotalTokens.Should().Be(350);
    }
}
