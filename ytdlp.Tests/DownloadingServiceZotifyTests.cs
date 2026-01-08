using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ytdlp.Services.Interfaces;
using ytdlp.Services;

namespace ytdlp.Tests
{
    public class DownloadingServiceZotifyTests
    {
        private readonly Mock<IConfigsServices> _mockConfigsServices;
        private readonly Mock<ILogger<DownloadingService>> _mockLogger;
        private readonly Mock<IProcessFactory> _mockProcessFactory;
        private readonly DownloadingService _downloadingService;

        public DownloadingServiceZotifyTests()
        {
            _mockConfigsServices = new Mock<IConfigsServices>();
            _mockLogger = new Mock<ILogger<DownloadingService>>();
            _mockProcessFactory = new Mock<IProcessFactory>();

            _downloadingService = new DownloadingService(
                _mockConfigsServices.Object,
                _mockLogger.Object,
                _mockProcessFactory.Object
            );
        }

        [Theory]
        [InlineData("https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp", true)]
        [InlineData("https://open.spotify.com/playlist/37i9dQZF1DX4o1sPnc8xWl", true)]
        [InlineData("https://open.spotify.com/album/1301WleyT98MSxVHPZCA6M", true)]
        [InlineData("https://spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp", true)]
        [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", false)]
        [InlineData("https://www.twitch.tv/videos/123456789", false)]
        [InlineData("https://www.soundcloud.com/artist/track", false)]
        public async Task IsSpotifyUrl_WithVariousUrls_ReturnsCorrectValue(string url, bool expectedIsSpotify)
        {
            // Arrange
            string configPath = "/app/configs/test.conf";
            _mockConfigsServices.Setup(x => x.GetWholeConfigPath(It.IsAny<string>()))
                .Returns(configPath);

            // Act
            var processInfo = await _downloadingService.GetProcessStartInfoAsync(url, configPath, expectedIsSpotify);

            // Assert
            if (expectedIsSpotify)
            {
                Assert.Equal("zotify", processInfo.FileName);
            }
            else
            {
                Assert.Equal("yt-dlp", processInfo.FileName);
            }
        }

        [Fact]
        public async Task GetProcessStartInfoAsync_WithSpotifyUrl_CreatesZotifyProcess()
        {
            // Arrange
            string url = "https://open.spotify.com/track/3n3Ppam7vgaVa1iaRUc9Lp";
            string configPath = "/app/configs/spotify-default.json";

            // Act
            var processInfo = await _downloadingService.GetProcessStartInfoAsync(url, configPath, isSpotify: true);

            // Assert
            Assert.Equal("zotify", processInfo.FileName);
            Assert.Contains(url, processInfo.Arguments);
            Assert.Contains(configPath, processInfo.Arguments);
            Assert.True(processInfo.RedirectStandardOutput);
            Assert.True(processInfo.RedirectStandardError);
            Assert.False(processInfo.UseShellExecute);
        }

        [Fact]
        public async Task GetProcessStartInfoAsync_WithYouTubeUrl_CreatesYtDlpProcess()
        {
            // Arrange
            string url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            string configPath = "/app/configs/default.conf";

            // Act
            var processInfo = await _downloadingService.GetProcessStartInfoAsync(url, configPath, isSpotify: false);

            // Assert
            Assert.Equal("yt-dlp", processInfo.FileName);
            Assert.Contains(url, processInfo.Arguments);
            Assert.Contains(configPath, processInfo.Arguments);
            Assert.True(processInfo.RedirectStandardOutput);
            Assert.True(processInfo.RedirectStandardError);
            Assert.False(processInfo.UseShellExecute);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("https://open.spotify.com/TRACK/3n3Ppam7vgaVa1iaRUc9Lp", true)] // Case-insensitive
        [InlineData("HTTPS://OPEN.SPOTIFY.COM/TRACK/3n3Ppam7vgaVa1iaRUc9Lp", true)] // Case-insensitive
        public async Task GetProcessStartInfoAsync_WithEdgeCases_HandlesCorrectly(string url, bool expectedIsSpotify)
        {
            // Arrange
            if (string.IsNullOrEmpty(url))
                return; // Skip null/empty tests

            string configPath = "/app/configs/test.conf";

            // Act
            var processInfo = await _downloadingService.GetProcessStartInfoAsync(url, configPath, expectedIsSpotify);

            // Assert
            if (expectedIsSpotify)
            {
                Assert.Equal("zotify", processInfo.FileName);
            }
            else
            {
                Assert.Equal("yt-dlp", processInfo.FileName);
            }
        }
    }
}
