using FluentAssertions;
using HermesAgent.Skills;
using HermesAgent.Core.Configuration;
using HermesAgent.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace HermesAgent.Core.Tests;

public class SkillManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileSkillManager _manager;

    public SkillManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hermes_skill_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var options = Options.Create(new HermesOptions { 
            DataDirectory = _tempDir,
            Skills = new SkillsOptions { SkillsDirectory = "skills" }
        });
        var logger = Substitute.For<ILogger<FileSkillManager>>();
        _manager = new FileSkillManager(options, logger);
    }

    [Fact]
    public async Task CreateAndGetSkill_Works()
    {
        // Act
        var created = await _manager.CreateSkillAsync("test-skill", "Desc", "Content");
        var retrieved = await _manager.GetSkillAsync("test-skill");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("test-skill");
        retrieved.Content.Should().Be("Content");
    }

    [Fact]
    public async Task ImproveSkill_AppendsContent()
    {
        // Arrange
        await _manager.CreateSkillAsync("improve-me", "Desc", "Initial");

        // Act
        await _manager.ImproveSkillAsync("improve-me", "Added insight");
        var retrieved = await _manager.GetSkillAsync("improve-me");

        // Assert
        retrieved!.Content.Should().Contain("Initial");
        retrieved.Content.Should().Contain("Added insight");
    }

    [Fact]
    public async Task DeleteSkill_RemovesFromCacheAndDisk()
    {
        // Arrange
        await _manager.CreateSkillAsync("delete-me", "Desc", "Content");

        // Act
        await _manager.DeleteSkillAsync("delete-me");
        var retrieved = await _manager.GetSkillAsync("delete-me");

        // Assert
        retrieved.Should().BeNull();
        var files = Directory.GetFiles(Path.Combine(_tempDir, "skills"), "delete-me.md");
        files.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }
}
