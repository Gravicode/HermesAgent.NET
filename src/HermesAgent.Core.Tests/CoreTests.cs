using FluentAssertions;
using HermesAgent.Core.Abstractions;
using HermesAgent.Core.Models;
using NSubstitute;
using Xunit;

namespace HermesAgent.Core.Tests;

public class ConversationTests
{
    [Fact]
    public void AddMessage_IncreasesMessageCount()
    {
        var conv = new Conversation();
        conv.AddMessage(Message.User("Hello"));
        conv.Messages.Should().HaveCount(1);
    }

    [Fact]
    public void Clear_RemovesAllMessages()
    {
        var conv = new Conversation();
        conv.AddMessage(Message.User("Hello"));
        conv.AddMessage(Message.Assistant("Hi!"));
        conv.Clear();
        conv.Messages.Should().BeEmpty();
    }

    [Fact]
    public void TokenEstimate_IsPositive_WhenMessagesPresent()
    {
        var conv = new Conversation();
        conv.AddMessage(Message.User("What is the meaning of life, the universe, and everything?"));
        conv.TokenEstimate.Should().BeGreaterThan(0);
    }
}

public class MessageTests
{
    [Fact]
    public void User_SetsRoleCorrectly()
    {
        var msg = Message.User("test");
        msg.Role.Should().Be("user");
        msg.Content.Should().Be("test");
    }

    [Fact]
    public void Assistant_SetsRoleCorrectly()
    {
        var msg = Message.Assistant("response");
        msg.Role.Should().Be("assistant");
    }

    [Fact]
    public void ToolResult_SetsRoleAndMetadata()
    {
        var msg = Message.ToolResult("bash", "output");
        msg.Role.Should().Be("tool");
        msg.Metadata.Should().ContainKey("tool_name");
        msg.Metadata!["tool_name"].Should().Be("bash");
    }
}

public class LlmUsageTests
{
    [Fact]
    public void TotalTokens_IsSum()
    {
        var usage = new LlmUsage(100, 50);
        usage.TotalTokens.Should().Be(150);
    }
}

public class ToolDefinitionTests
{
    [Fact]
    public void ToolDefinition_CanBeCreated()
    {
        var def = new ToolDefinition
        {
            Name = "test_tool",
            Description = "A test tool",
            Parameters = new Dictionary<string, ParameterDefinition>
            {
                ["input"] = new() { Type = "string", Description = "Input value" }
            },
            Required = ["input"]
        };

        def.Name.Should().Be("test_tool");
        def.Parameters.Should().HaveCount(1);
        def.Required.Should().Contain("input");
    }
}
