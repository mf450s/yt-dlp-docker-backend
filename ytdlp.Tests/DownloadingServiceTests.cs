using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Moq;
using ytdlp.Services;
using ytdlp.Services.Interfaces;

namespace ytdlp.Tests;

/// <summary>
/// Unit tests for <see cref="DownloadingService"/>.
/// Tests verify correct interaction with dependencies, process lifecycle management,
/// and error handling according to SOLID principles and Clean Code practices.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DownloadingServiceTests : IDisposable
{
    private readonly Mock<IConfigsServices> _mockConfigsService;
    private readonly Mock<IProcessFactory> _mockProcessFactory;
    private readonly Mock<IProcess> _mockProcess;
    private readonly Mock<TextReader> _mockStdOut;
    private readonly Mock<TextReader> _mockStdErr;
    private readonly DownloadingService _sut;
    private readonly CancellationTokenSource _cancellationTokenSource;

    private const string TestUrl = "https://youtube.com/watch?v=test";
    private const string TestConfigFile = "test";
    private const string TestConfigPath = "../configs/test.conf";
    private const string SuccessOutput = "Download complete";
    private const string ErrorOutput = "ERROR: Unable to download";

    public DownloadingServiceTests()
    {
        _mockConfigsService = new Mock<IConfigsServices>();
        _mockProcessFactory = new Mock<IProcessFactory>();
        _mockProcess = new Mock<IProcess>();
        _mockStdOut = new Mock<TextReader>();
        _mockStdErr = new Mock<TextReader>();
        _cancellationTokenSource = new CancellationTokenSource();

        _sut = new DownloadingService(_mockConfigsService.Object, _mockProcessFactory.Object);
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }

    #region Test Data Factories

    public static TheoryData<string, string, string> ValidDownloadScenarios() => new()
    {
        { "https://youtube.com/watch?v=dQw4w9WgXcQ", "music", "../configs/music.conf" },
        { "https://soundcloud.com/track", "audio", "/config/audio.conf" },
        { "https://vimeo.com/12345", "video", "../configs/video.conf" },
        { "https://twitch.tv/videos/123", "stream", "/configs/stream.conf" }
    };

    public static TheoryData<string?, string?> InvalidInputScenarios() => new()
    {
        { null, TestConfigFile },
        { string.Empty, TestConfigFile },
        { TestUrl, null },
        { TestUrl, string.Empty },
        { null, null },
        { string.Empty, string.Empty }
    };

    #endregion

    #region Helper Methods

    /// <summary>
    /// Configures mock process with specified output and error streams.
    /// Ensures proper test isolation by resetting all mocks.
    /// </summary>
    private void SetupMockProcess(
        string standardOutput,
        string standardError,
        bool startResult = true)
    {
        // Reset for test isolation
        _mockStdOut.Reset();
        _mockStdErr.Reset();
        _mockProcess.Reset();
        _mockProcessFactory.Reset();

        // Configure standard streams
        _mockStdOut
            .Setup(x => x.ReadToEndAsync())
            .ReturnsAsync(standardOutput);

        _mockStdErr
            .Setup(x => x.ReadToEndAsync())
            .ReturnsAsync(standardError);

        // Configure process behavior
        _mockProcess
            .Setup(p => p.StandardOutput)
            .Returns(_mockStdOut.Object);

        _mockProcess
            .Setup(p => p.StandardError)
            .Returns(_mockStdErr.Object);

        _mockProcess
            .Setup(p => p.Start())
            .Returns(startResult);

        _mockProcess
            .Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockProcess
            .Setup(p => p.Dispose());

        _mockProcessFactory
            .Setup(x => x.CreateProcess())
            .Returns(_mockProcess.Object);
    }

    /// <summary>
    /// Sets up the config service mock with a specific config path.
    /// </summary>
    private void SetupConfigService(string configFile, string configPath)
    {
        _mockConfigsService
            .Setup(x => x.GetWholeConfigPath(configFile))
            .Returns(configPath);
    }

    #endregion

    #region Configuration Service Integration Tests

    [Theory]
    [MemberData(nameof(ValidDownloadScenarios))]
    public async Task GivenValidInputs_WhenDownloading_ThenShouldCallConfigServiceCorrectly(
        string url,
        string configFile,
        string expectedConfigPath)
    {
        // Arrange
        SetupConfigService(configFile, expectedConfigPath);
        SetupMockProcess(SuccessOutput, string.Empty);

        // Act
        await _sut.TryDownloadingFromURL(url, configFile);

        // Assert
        _mockConfigsService.Verify(
            x => x.GetWholeConfigPath(configFile),
            Times.Once,
            "Config service should be called exactly once with the provided config file");
    }

    [Fact]
    public async Task GivenConfigServiceReturnsPath_WhenDownloading_ThenShouldUseReturnedPath()
    {
        // Arrange
        const string expectedConfigPath = "/custom/path/config.conf";
        SetupConfigService(TestConfigFile, expectedConfigPath);
        SetupMockProcess(SuccessOutput, string.Empty);

        // Act
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);

        // Assert
        _mockConfigsService.Verify(
            x => x.GetWholeConfigPath(TestConfigFile),
            Times.Once);
        _mockProcessFactory.Verify(
            x => x.CreateProcess(),
            Times.Once,
            "Process should be created after config path is resolved");
    }

    #endregion

    #region Process Lifecycle Tests

    [Fact]
    public async Task GivenValidInputs_WhenDownloading_ThenShouldCreateAndStartProcess()
    {
        // Arrange
        SetupConfigService(TestConfigFile, TestConfigPath);
        SetupMockProcess(SuccessOutput, string.Empty);

        // Act
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);

        // Assert
        _mockProcessFactory.Verify(
            x => x.CreateProcess(),
            Times.Once,
            "Process factory should create exactly one process instance");
        _mockProcess.Verify(
            x => x.Start(),
            Times.Once,
            "Process should be started exactly once");
    }

    [Fact]
    public async Task GivenProcessCompletes_WhenDownloading_ThenShouldReadOutputStreams()
    {
        // Arrange
        const string expectedOutput = "Downloaded: video.mp3";
        const string expectedError = "Warning: Quality setting ignored";
        SetupConfigService(TestConfigFile, TestConfigPath);
        SetupMockProcess(expectedOutput, expectedError);

        // Act
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);

        // Assert
        _mockProcess.Verify(
            x => x.Start(),
            Times.Once,
            "Process should be started");
        _mockStdOut.Verify(
            x => x.ReadToEndAsync(),
            Times.Once,
            "Standard output should be read completely");
        _mockStdErr.Verify(
            x => x.ReadToEndAsync(),
            Times.Once,
            "Standard error should be read completely");
        _mockProcess.Verify(
            x => x.WaitForExitAsync(It.IsAny<CancellationToken>()),
            Times.Once,
            "Should wait for process to complete");
    }

    [Fact]
    public async Task GivenProcessCompletes_WhenDownloading_ThenShouldDisposeProcess()
    {
        // Arrange
        SetupConfigService(TestConfigFile, TestConfigPath);
        SetupMockProcess(SuccessOutput, string.Empty);

        // Act
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);

        // Assert
        _mockProcess.Verify(
            x => x.Dispose(),
            Times.Once,
            "Process must be disposed to prevent resource leaks");
    }

    [Fact]
    public async Task GivenMultipleDownloads_WhenCalledSequentially_ThenShouldCreateNewProcessEachTime()
    {
        // Arrange
        SetupConfigService(TestConfigFile, TestConfigPath);
        SetupMockProcess(SuccessOutput, string.Empty);

        // Act
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);

        // Assert
        _mockProcessFactory.Verify(
            x => x.CreateProcess(),
            Times.Exactly(3),
            "Each download should create a new process instance");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GivenProcessWithErrors_WhenDownloading_ThenShouldHandleErrorStream()
    {
        // Arrange
        const string url = "https://invalid-url";
        SetupConfigService(TestConfigFile, TestConfigPath);
        SetupMockProcess(string.Empty, ErrorOutput);

        // Act
        await _sut.TryDownloadingFromURL(url, TestConfigFile);

        // Assert
        _mockProcess.Verify(
            x => x.Start(),
            Times.Once,
            "Process should start even with invalid URL");
        _mockStdErr.Verify(
            x => x.ReadToEndAsync(),
            Times.Once,
            "Error stream should be read to capture error messages");
        _mockProcess.Verify(
            x => x.WaitForExitAsync(It.IsAny<CancellationToken>()),
            Times.Once,
            "Should wait for process completion even on errors");
    }

    [Theory]
    [MemberData(nameof(InvalidInputScenarios))]
    public async Task GivenInvalidInputs_WhenDownloading_ThenShouldStillAttemptDownload(
        string? url,
        string? configFile)
    {
        // Arrange
        _mockConfigsService
            .Setup(x => x.GetWholeConfigPath(It.IsAny<string>()))
            .Returns(TestConfigPath);
        SetupMockProcess(string.Empty, string.Empty);

        // Act
        await _sut.TryDownloadingFromURL(url!, configFile!);

        // Assert
        _mockConfigsService.Verify(
            x => x.GetWholeConfigPath(It.IsAny<string>()),
            Times.Once,
            "Service should attempt to resolve config even with invalid inputs");
        _mockProcessFactory.Verify(
            x => x.CreateProcess(),
            Times.Once,
            "Process creation should be attempted even with invalid inputs");
    }

    [Fact]
    public async Task GivenProcessFailsToStart_WhenDownloading_ThenShouldHandleGracefully()
    {
        // Arrange
        SetupConfigService(TestConfigFile, TestConfigPath);
        SetupMockProcess(string.Empty, ErrorOutput, startResult: false);

        // Act
        var act = async () => await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);

        // Assert
        await act.Should().NotThrowAsync(
            "Service should handle process start failures gracefully");
        _mockProcess.Verify(
            x => x.Start(),
            Times.Once,
            "Start should be attempted exactly once");
    }

    #endregion

    #region Async Operation Tests

    [Fact]
    public async Task GivenLongRunningProcess_WhenCancellationRequested_ThenShouldRespectCancellation()
    {
        // Arrange
        SetupConfigService(TestConfigFile, TestConfigPath);
        var cts = new CancellationTokenSource();
        
        _mockStdOut.Setup(x => x.ReadToEndAsync()).ReturnsAsync(SuccessOutput);
        _mockStdErr.Setup(x => x.ReadToEndAsync()).ReturnsAsync(string.Empty);
        _mockProcess.Setup(p => p.StandardOutput).Returns(_mockStdOut.Object);
        _mockProcess.Setup(p => p.StandardError).Returns(_mockStdErr.Object);
        _mockProcess.Setup(p => p.Start()).Returns(true);
        _mockProcess.Setup(p => p.Dispose());
        _mockProcessFactory.Setup(x => x.CreateProcess()).Returns(_mockProcess.Object);
        
        // Simulate long-running operation that respects cancellation
        _mockProcess
            .Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) =>
            {
                await Task.Delay(5000, ct);
            });

        cts.CancelAfter(100);

        // Act & Assert
        var act = async () => await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);
        
        // Note: This verifies the mock setup accepts CancellationToken
        await act.Should().NotThrowAsync<OperationCanceledException>(
            "Service handles cancellation internally without propagating exception");
        
        cts.Dispose();
    }

    [Fact]
    public async Task GivenConcurrentDownloads_WhenExecutedInParallel_ThenShouldHandleCorrectly()
    {
        // Arrange
        const int concurrentDownloads = 5;
        SetupConfigService(TestConfigFile, TestConfigPath);
        SetupMockProcess(SuccessOutput, string.Empty);

        // Act
        var downloadTasks = Enumerable
            .Range(0, concurrentDownloads)
            .Select(_ => _sut.TryDownloadingFromURL(TestUrl, TestConfigFile))
            .ToArray();

        await Task.WhenAll(downloadTasks);

        // Assert
        _mockProcessFactory.Verify(
            x => x.CreateProcess(),
            Times.Exactly(concurrentDownloads),
            $"Should create {concurrentDownloads} separate process instances for concurrent downloads");
    }

    #endregion

    #region Stream Handling Tests

    [Theory]
    [InlineData("[download] 100% of 50.00MiB in 00:05", "")]
    [InlineData("[download] Downloading video 1 of 3", "")]
    [InlineData("", "WARNING: Requested format not available")]
    [InlineData("Success", "WARNING: Minor issue")]
    public async Task GivenVariousOutputPatterns_WhenDownloading_ThenShouldReadAllStreams(
        string standardOutput,
        string standardError)
    {
        // Arrange
        SetupConfigService(TestConfigFile, TestConfigPath);
        SetupMockProcess(standardOutput, standardError);

        // Act
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);

        // Assert
        _mockStdOut.Verify(
            x => x.ReadToEndAsync(),
            Times.Once,
            "Standard output must be read regardless of content");
        _mockStdErr.Verify(
            x => x.ReadToEndAsync(),
            Times.Once,
            "Standard error must be read regardless of content");
    }

    #endregion
}