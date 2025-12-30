using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Extensions.Logging;
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
    private readonly Mock<ILogger<DownloadingService>> _mockLogger;
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
        _mockLogger = new Mock<ILogger<DownloadingService>>();
        _mockProcessFactory = new Mock<IProcessFactory>();
        _mockProcess = new Mock<IProcess>();
        _mockStdOut = new Mock<TextReader>();
        _mockStdErr = new Mock<TextReader>();
        _cancellationTokenSource = new CancellationTokenSource();


        _sut = new DownloadingService(
            _mockConfigsService.Object, 
            _mockLogger.Object,
            _mockProcessFactory.Object);
    }


    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
    }


    #region TryDownloadingFromURL Tests


    [Fact]
    public async Task TryDownloadingFromURL_Success_CallsProcessCorrectly()
    {
        // Arrange
        SetupSuccessfulDownload();


        // Act
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);


        // Assert
        _mockConfigsService.Verify(x => x.GetWholeConfigPath(TestConfigFile), Times.Once);
        _mockProcessFactory.Verify(x => x.CreateProcess(), Times.Once);
        _mockProcess.Verify(x => x.Start(), Times.Once);
        _mockProcess.Verify(x => x.WaitForExitAsync(default), Times.Once);
    }


    [Fact]
    public async Task TryDownloadingFromURL_Success_ReturnsZeroExitCode()
    {
        // Arrange
        SetupSuccessfulDownload();


        // Act
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);


        // Assert
        _mockProcess.Object.ExitCode.Should().Be(0);
    }


    [Fact]
    public async Task TryDownloadingFromURL_Failure_NonZeroExitCode()
    {
        // Arrange
        SetupFailedDownload();


        // Act
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);


        // Assert
        _mockProcess.Object.ExitCode.Should().Be(1);
    }


    [Fact]
    public async Task TryDownloadingFromURL_ProcessException_ThrowsException()
    {
        // Arrange
        SetupProcessException();


        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile));
    }


    [Fact]
    public async Task TryDownloadingFromURL_ConfigServiceReturnsPath_UsesCorrectPath()
    {
        // Arrange
        const string expectedConfigPath = "/custom/path/config.conf";
        SetupSuccessfulDownload(expectedConfigPath);


        // Act
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);


        // Assert
        _mockProcess.VerifySet(x => x.StartInfo = It.Is<System.Diagnostics.ProcessStartInfo>(
            psi => psi.Arguments.Contains(expectedConfigPath)), Times.Once);
    }


    [Fact]
    public async Task TryDownloadingFromURL_ReadsStandardOutput_WhenProcessSucceeds()
    {
        // Arrange
        SetupSuccessfulDownload();


        // Act
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);


        // Assert
        _mockStdOut.Verify(x => x.ReadToEndAsync(), Times.Once);
    }


    [Fact]
    public async Task TryDownloadingFromURL_ReadsStandardError_WhenProcessFails()
    {
        // Arrange
        SetupFailedDownload();


        // Act
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);


        // Assert
        _mockStdErr.Verify(x => x.ReadToEndAsync(), Times.Once);
    }


    [Fact]
    public async Task TryDownloadingFromURL_DisposesProcess_AfterCompletion()
    {
        // Arrange
        SetupSuccessfulDownload();


        // Act
        await _sut.TryDownloadingFromURL(TestUrl, TestConfigFile);


        // Assert
        _mockProcess.Verify(x => x.Dispose(), Times.Once);
    }

    #endregion


    #region GetProcessStartInfoAsync Tests


    [Fact]
    public async Task GetProcessStartInfoAsync_ReturnsCorrectFileName()
    {
        // Act
        var startInfo = await DownloadingService.GetProcessStartInfoAsync(TestUrl, TestConfigPath);


        // Assert
        startInfo.FileName.Should().Be("yt-dlp");
    }


    [Fact]
    public async Task GetProcessStartInfoAsync_ContainsUrl_InArguments()
    {
        // Act
        var startInfo = await DownloadingService.GetProcessStartInfoAsync(TestUrl, TestConfigPath);


        // Assert
        startInfo.Arguments.Should().Contain(TestUrl);
    }


    [Fact]
    public async Task GetProcessStartInfoAsync_ContainsConfigLocation_InArguments()
    {
        // Act
        var startInfo = await DownloadingService.GetProcessStartInfoAsync(TestUrl, TestConfigPath);


        // Assert
        startInfo.Arguments.Should().Contain("--config-locations");
        startInfo.Arguments.Should().Contain(TestConfigPath);
    }


    [Fact]
    public async Task GetProcessStartInfoAsync_RedirectsStandardOutput()
    {
        // Act
        var startInfo = await DownloadingService.GetProcessStartInfoAsync(TestUrl, TestConfigPath);


        // Assert
        startInfo.RedirectStandardOutput.Should().BeTrue();
    }


    [Fact]
    public async Task GetProcessStartInfoAsync_RedirectsStandardError()
    {
        // Act
        var startInfo = await DownloadingService.GetProcessStartInfoAsync(TestUrl, TestConfigPath);


        // Assert
        startInfo.RedirectStandardError.Should().BeTrue();
    }


    [Fact]
    public async Task GetProcessStartInfoAsync_DoesNotUseShellExecute()
    {
        // Act
        var startInfo = await DownloadingService.GetProcessStartInfoAsync(TestUrl, TestConfigPath);


        // Assert
        startInfo.UseShellExecute.Should().BeFalse();
    }


    [Fact]
    public async Task GetProcessStartInfoAsync_CreatesNoWindow()
    {
        // Act
        var startInfo = await DownloadingService.GetProcessStartInfoAsync(TestUrl, TestConfigPath);


        // Assert
        startInfo.CreateNoWindow.Should().BeTrue();
    }


    #endregion


    #region Helper Methods


    private void SetupSuccessfulDownload(string? configPath = null)
    {
        configPath ??= TestConfigPath;
        
        _mockConfigsService
            .Setup(x => x.GetWholeConfigPath(TestConfigFile))
            .Returns(configPath);


        _mockStdOut
            .Setup(x => x.ReadToEndAsync())
            .ReturnsAsync(SuccessOutput);


        _mockStdErr
            .Setup(x => x.ReadToEndAsync())
            .ReturnsAsync(string.Empty);


        _mockProcess.SetupGet(x => x.StandardOutput).Returns(_mockStdOut.Object);
        _mockProcess.SetupGet(x => x.StandardError).Returns(_mockStdErr.Object);
        _mockProcess.SetupGet(x => x.ExitCode).Returns(0);
        _mockProcess.Setup(x => x.Start()).Returns(true);
        _mockProcess.Setup(x => x.WaitForExitAsync(default)).Returns(Task.CompletedTask);
        _mockProcess.SetupSet(x => x.StartInfo = It.IsAny<System.Diagnostics.ProcessStartInfo>());


        _mockProcessFactory
            .Setup(x => x.CreateProcess())
            .Returns(_mockProcess.Object);
    }


    private void SetupFailedDownload()
    {
        _mockConfigsService
            .Setup(x => x.GetWholeConfigPath(TestConfigFile))
            .Returns(TestConfigPath);


        _mockStdOut
            .Setup(x => x.ReadToEndAsync())
            .ReturnsAsync(string.Empty);


        _mockStdErr
            .Setup(x => x.ReadToEndAsync())
            .ReturnsAsync(ErrorOutput);


        _mockProcess.SetupGet(x => x.StandardOutput).Returns(_mockStdOut.Object);
        _mockProcess.SetupGet(x => x.StandardError).Returns(_mockStdErr.Object);
        _mockProcess.SetupGet(x => x.ExitCode).Returns(1);
        _mockProcess.Setup(x => x.Start()).Returns(true);
        _mockProcess.Setup(x => x.WaitForExitAsync(default)).Returns(Task.CompletedTask);
        _mockProcess.SetupSet(x => x.StartInfo = It.IsAny<System.Diagnostics.ProcessStartInfo>());


        _mockProcessFactory
            .Setup(x => x.CreateProcess())
            .Returns(_mockProcess.Object);
    }


    private void SetupProcessException()
    {
        _mockConfigsService
            .Setup(x => x.GetWholeConfigPath(TestConfigFile))
            .Returns(TestConfigPath);


        _mockProcessFactory
            .Setup(x => x.CreateProcess())
            .Throws<InvalidOperationException>();
    }


    #endregion
}
