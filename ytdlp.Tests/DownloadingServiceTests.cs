using System.Diagnostics.CodeAnalysis;
using Moq;
using ytdlp.Services;
using ytdlp.Services.Interfaces;
#nullable disable
namespace ytdlp.Tests
{
    [ExcludeFromCodeCoverage]
    public class DownloadingServiceTests
    {
        private readonly Mock<IConfigsServices> _mockConfigsService;
        private readonly Mock<IProcessFactory> _mockProcessFactory;
        private readonly Mock<IProcess> _mockProcess;
        private readonly DownloadingService _service;
        private readonly Mock<TextReader> _mockStdOut;
        private readonly Mock<TextReader> _mockStdErr;

        public DownloadingServiceTests()
        {
            _mockConfigsService = new Mock<IConfigsServices>();
            _mockProcessFactory = new Mock<IProcessFactory>();
            _mockProcess = new Mock<IProcess>();
            _mockStdErr = new Mock<TextReader>();
            _mockStdOut = new Mock<TextReader>();

            _service = new DownloadingService(_mockConfigsService.Object, _mockProcessFactory.Object);
        }

        #region Helper
        private void SetupMockProcess(string output, string error)
        {
            // Reset für Test-Isolation (optional aber empfohlen)
            _mockStdOut.Reset();
            _mockStdErr.Reset();
            _mockProcess.Reset();
            _mockProcessFactory.Reset();

            // Setups
            _mockStdOut.Setup(x => x.ReadToEndAsync()).ReturnsAsync(output);
            _mockStdErr.Setup(x => x.ReadToEndAsync()).ReturnsAsync(error);

            _mockProcess.Setup(p => p.StandardOutput).Returns(_mockStdOut.Object);
            _mockProcess.Setup(p => p.StandardError).Returns(_mockStdErr.Object);
            _mockProcess.Setup(p => p.Start()).Returns(true);
            _mockProcess.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockProcessFactory.Setup(x => x.CreateProcess()).Returns(_mockProcess.Object);
        }
        #endregion

        #region Tests

        [Theory]
        [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ", "music", "../configs/music.conf")]
        [InlineData("https://soundcloud.com/track", "audio", "/config/audio.conf")]
        [InlineData("https://vimeo.com/12345", "video", "../configs/video.conf")]
        public async Task TryDownloadingFromURL_ValidInputs_CallsConfigServiceCorrectly(
            string url, string configFile, string expectedConfigPath)
        {
            // Arrange
            _mockConfigsService
                .Setup(x => x.GetWholeConfigPath(configFile))
                .Returns(expectedConfigPath);

            SetupMockProcess("Download complete", "");

            // Act
            await _service.TryDownloadingFromURL(url, configFile);

            // Assert
            _mockConfigsService.Verify(x => x.GetWholeConfigPath(configFile), Times.Once);
        }

        [Fact]
        public async Task TryDownloadingFromURL_ValidInputs_CreatesAndStartsProcess()
        {
            // Arrange
            string url = "https://youtube.com/watch?v=test";
            string configFile = "test";
            string configPath = "../configs/test.conf";

            _mockConfigsService.Setup(x => x.GetWholeConfigPath(configFile)).Returns(configPath);
            SetupMockProcess("Success", "");

            // Act
            await _service.TryDownloadingFromURL(url, configFile);

            // Assert
            _mockProcessFactory.Verify(x => x.CreateProcess(), Times.Once);
            _mockProcess.Verify(x => x.Start(), Times.Once);
        }

        [Fact]
        public async Task TryDownloadingFromURL_ProcessCompletes_ReadsOutputAndError()
        {
            // Arrange
            string expectedOutput = "Downloaded: video.mp3";
            string expectedError = "";

            _mockConfigsService.Setup(x => x.GetWholeConfigPath(It.IsAny<string>()))
                .Returns("../configs/test.conf");
            SetupMockProcess(expectedOutput, expectedError);

            // Act
            await _service.TryDownloadingFromURL("https://test.com", "test");

            // Assert
            _mockProcess.Verify(x => x.Start(), Times.Once);
            _mockStdOut.Verify(x => x.ReadToEndAsync(), Times.Once);
            _mockStdErr.Verify(x => x.ReadToEndAsync(), Times.Once);
        }

        [Fact]
        public async Task TryDownloadingFromURL_ProcessWithErrors_HandlesErrorStream()
        {
            // Arrange
            string url = "https://invalid-url";
            string configFile = "test";
            string expectedError = "ERROR: Unable to download";

            _mockConfigsService.Setup(x => x.GetWholeConfigPath(configFile))
                .Returns("../configs/test.conf");
            SetupMockProcess("", expectedError);

            // Act
            await _service.TryDownloadingFromURL(url, configFile);

            // Assert
            _mockProcess.Verify(x => x.Start(), Times.Once);
            _mockProcess.Verify(x => x.WaitForExitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(null, "test")]
        [InlineData("", "test")]
        [InlineData("https://test.com", null)]
        [InlineData("https://test.com", "")]
        public async Task TryDownloadingFromURL_InvalidInputs_StillCallsServices(
            string url, string configFile)
        {
            // Arrange
            _mockConfigsService.Setup(x => x.GetWholeConfigPath(It.IsAny<string>()))
                .Returns("../configs/test.conf");
            SetupMockProcess("", "");

            // Act
            await _service.TryDownloadingFromURL(url, configFile);

            // Assert
            _mockConfigsService.Verify(x => x.GetWholeConfigPath(It.IsAny<string>()), Times.Once);
            _mockProcessFactory.Verify(x => x.CreateProcess(), Times.Once);
        }

        [Fact]
        public async Task TryDownloadingFromURL_ProcessDisposed_DisposesCorrectly()
        {
            // Arrange
            _mockConfigsService.Setup(x => x.GetWholeConfigPath(It.IsAny<string>()))
                .Returns("../configs/test.conf");
            SetupMockProcess("Success", "");

            // Act
            await _service.TryDownloadingFromURL("https://test.com", "test");

            // Assert - Direkte public Dispose() Verifikation
            _mockProcess.Verify(x => x.Dispose(), Times.Once);
        }
        #endregion
    }
}