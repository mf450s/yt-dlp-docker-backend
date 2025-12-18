using System;
using System.Reflection;
using System.Threading.Tasks;
using ytdlp.Services;
using System.Diagnostics;
using ytdlp.Services.Interfaces;
using Xunit;
#nullable disable
namespace ytdlp.Tests
{
    public class DownloadingServiceTests
    {
        private readonly IDownloadingService _service;

        public DownloadingServiceTests()
        {
            _service = new DownloadingService();
        }

        [Fact]
        public void GetWholeConfigPath_ReturnsCorrectPath()
        {
            // Arrange
            var configName = "test-config";
            var expected = $"./configs/{configName}.conf";

            // Act
            var method = typeof(DownloadingService).GetMethod("GetWholeConfigPath", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = (string)method.Invoke(_service, [configName]);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetProcessStartInfoAsync_ReturnsProperProcessStartInfo()
        {
            // Arrange
            var url = "https://example.com/video";
            var configName = "config";
            var wholeConfigPath = $"./configs/{configName}.conf";

            var method = typeof(DownloadingService).GetMethod("GetProcessStartInfoAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var startInfoTask = (Task<ProcessStartInfo>)method.Invoke(_service, [url, wholeConfigPath]);
            var startInfo = await startInfoTask;

            // Assert
            Assert.Equal("yt-dlp", startInfo.FileName);
            var expectedArgs = string.Join(" ", new[] { url, "--config-locations", wholeConfigPath });
            Assert.Equal(expectedArgs, startInfo.Arguments);
            Assert.True(startInfo.RedirectStandardOutput);
            Assert.True(startInfo.RedirectStandardError);
            Assert.False(startInfo.UseShellExecute);
            Assert.True(startInfo.CreateNoWindow);
        }
    }
}
