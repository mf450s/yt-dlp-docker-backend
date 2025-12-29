using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ytdlp.Services;
using ytdlp.Services.Interfaces;
using FluentResults;

namespace ytdlp.Tests;

public class StartupConfigFixerTests
{
    private readonly Mock<IConfigsServices> _mockConfigsServices;
    private readonly Mock<ILogger<StartupConfigFixer>> _mockLogger;
    private readonly StartupConfigFixer _fixer;

    public StartupConfigFixerTests()
    {
        _mockConfigsServices = new Mock<IConfigsServices>();
        _mockLogger = new Mock<ILogger<StartupConfigFixer>>();
        _fixer = new StartupConfigFixer(_mockConfigsServices.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task FixAllConfigsAsync_NoConfigs_ReturnsEmptyResult()
    {
        // Arrange
        _mockConfigsServices
            .Setup(s => s.GetAllConfigNames())
            .Returns(new List<string>());

        // Act
        var result = await _fixer.FixAllConfigsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalConfigsProcessed);
        Assert.Equal(0, result.ConfigsFixed);
        Assert.Equal(0, result.ConfigsWithErrors);
        Assert.True(result.AllSuccess);
        Assert.Empty(result.FixedConfigs);
        Assert.Empty(result.ErroredConfigs);
    }

    [Fact]
    public async Task FixAllConfigsAsync_ValidConfigs_CountsAsFixed()
    {
        // Arrange
        var configNames = new List<string> { "test-config" };
        var validContent = "--format best\n--output /downloads/%(title)s.%(ext)s";

        _mockConfigsServices
            .Setup(s => s.GetAllConfigNames())
            .Returns(configNames);

        _mockConfigsServices
            .Setup(s => s.GetConfigContentByName("test-config"))
            .Returns(Result.Ok(validContent));

        _mockConfigsServices
            .Setup(s => s.SetConfigContentAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Ok("Success"));

        // Act
        var result = await _fixer.FixAllConfigsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalConfigsProcessed);
        Assert.Equal(1, result.FixedConfigs.Count);
        Assert.Contains("test-config", result.FixedConfigs);
        Assert.Equal(0, result.ConfigsWithErrors);
        Assert.True(result.AllSuccess);
    }

    [Fact]
    public async Task FixAllConfigsAsync_MultipleConfigs_ProcessesAll()
    {
        // Arrange
        var configNames = new List<string> { "config1", "config2", "config3" };
        var content = "--format best";

        _mockConfigsServices
            .Setup(s => s.GetAllConfigNames())
            .Returns(configNames);

        _mockConfigsServices
            .Setup(s => s.GetConfigContentByName(It.IsAny<string>()))
            .Returns(Result.Ok(content));

        _mockConfigsServices
            .Setup(s => s.SetConfigContentAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Ok("Success"));

        // Act
        var result = await _fixer.FixAllConfigsAsync();

        // Assert
        Assert.Equal(3, result.TotalConfigsProcessed);
        Assert.Equal(3, result.FixedConfigs.Count);
        _mockConfigsServices.Verify(
            s => s.GetConfigContentByName(It.IsAny<string>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task FixAllConfigsAsync_ConfigReadFails_RecordsError()
    {
        // Arrange
        var configNames = new List<string> { "broken-config" };

        _mockConfigsServices
            .Setup(s => s.GetAllConfigNames())
            .Returns(configNames);

        _mockConfigsServices
            .Setup(s => s.GetConfigContentByName("broken-config"))
            .Returns(Result.Fail("File not found"));

        // Act
        var result = await _fixer.FixAllConfigsAsync();

        // Assert
        Assert.Equal(1, result.TotalConfigsProcessed);
        Assert.Equal(0, result.ConfigsFixed);
        Assert.Equal(1, result.ConfigsWithErrors);
        Assert.False(result.AllSuccess);
        Assert.Single(result.ErroredConfigs);
        Assert.Equal("broken-config", result.ErroredConfigs[0].ConfigName);
        Assert.Contains("File not found", result.ErroredConfigs[0].Error);
    }

    [Fact]
    public async Task FixAllConfigsAsync_ConfigSaveFails_RecordsError()
    {
        // Arrange
        var configNames = new List<string> { "test-config" };
        var content = "--format best";

        _mockConfigsServices
            .Setup(s => s.GetAllConfigNames())
            .Returns(configNames);

        _mockConfigsServices
            .Setup(s => s.GetConfigContentByName("test-config"))
            .Returns(Result.Ok(content));

        _mockConfigsServices
            .Setup(s => s.SetConfigContentAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Fail("Permission denied"));

        // Act
        var result = await _fixer.FixAllConfigsAsync();

        // Assert
        Assert.Equal(1, result.TotalConfigsProcessed);
        Assert.Equal(0, result.ConfigsFixed);
        Assert.Equal(1, result.ConfigsWithErrors);
        Assert.False(result.AllSuccess);
        Assert.Single(result.ErroredConfigs);
        Assert.Contains("Permission denied", result.ErroredConfigs[0].Error);
    }

    [Fact]
    public async Task FixAllConfigsAsync_MixedSuccessAndFailure_ReturnsAccurateCount()
    {
        // Arrange
        var configNames = new List<string> { "good-config", "bad-config", "another-good" };
        var validContent = "--format best";

        _mockConfigsServices
            .Setup(s => s.GetAllConfigNames())
            .Returns(configNames);

        // Setup for good-config
        _mockConfigsServices
            .Setup(s => s.GetConfigContentByName("good-config"))
            .Returns(Result.Ok(validContent));

        // Setup for bad-config
        _mockConfigsServices
            .Setup(s => s.GetConfigContentByName("bad-config"))
            .Returns(Result.Fail("File corrupted"));

        // Setup for another-good
        _mockConfigsServices
            .Setup(s => s.GetConfigContentByName("another-good"))
            .Returns(Result.Ok(validContent));

        _mockConfigsServices
            .Setup(s => s.SetConfigContentAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Ok("Success"));

        // Act
        var result = await _fixer.FixAllConfigsAsync();

        // Assert
        Assert.Equal(3, result.TotalConfigsProcessed);
        Assert.Equal(2, result.FixedConfigs.Count);
        Assert.Equal(1, result.ConfigsWithErrors);
        Assert.False(result.AllSuccess);
        Assert.Contains("good-config", result.FixedConfigs);
        Assert.Contains("another-good", result.FixedConfigs);
        Assert.Single(result.ErroredConfigs);
        Assert.Equal("bad-config", result.ErroredConfigs[0].ConfigName);
    }

    [Fact]
    public async Task FixAllConfigsAsync_ExceptionDuringProcessing_RecordsError()
    {
        // Arrange
        var configNames = new List<string> { "error-config" };

        _mockConfigsServices
            .Setup(s => s.GetAllConfigNames())
            .Returns(configNames);

        _mockConfigsServices
            .Setup(s => s.GetConfigContentByName(It.IsAny<string>()))
            .Throws(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _fixer.FixAllConfigsAsync();

        // Assert
        Assert.Equal(1, result.TotalConfigsProcessed);
        Assert.Equal(0, result.ConfigsFixed);
        Assert.Equal(1, result.ConfigsWithErrors);
        Assert.False(result.AllSuccess);
        Assert.Single(result.ErroredConfigs);
        Assert.Contains("Unexpected error", result.ErroredConfigs[0].Error);
    }

    [Fact]
    public async Task FixAllConfigsAsync_NullDependency_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new StartupConfigFixer(null!, _mockLogger.Object));

        Assert.Throws<ArgumentNullException>(() =>
            new StartupConfigFixer(_mockConfigsServices.Object, null!));
    }
}
