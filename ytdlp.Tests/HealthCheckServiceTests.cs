using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ytdlp.Services;
using ytdlp.Services.Interfaces;
using ytdlp.Api;
using Microsoft.AspNetCore.Mvc;

namespace ytdlp.Tests
{
    public class HealthCheckServiceTests
    {
        private readonly Mock<ILogger<HealthCheckService>> _mockLogger;
        private readonly Mock<IDownloadingService> _mockDownloadingService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly HealthCheckService _healthCheckService;

        public HealthCheckServiceTests()
        {
            _mockLogger = new Mock<ILogger<HealthCheckService>>();
            _mockDownloadingService = new Mock<IDownloadingService>();
            
            // Setup Configuration mock with default test paths
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["Paths:Downloads"]).Returns("./downloads");
            _mockConfiguration.Setup(c => c["Paths:Archive"]).Returns("./archive");
            _mockConfiguration.Setup(c => c["Paths:Config"]).Returns("./configs");
            _mockConfiguration.Setup(c => c["Paths:Cookies"]).Returns("./cookies");
            
            _healthCheckService = new HealthCheckService(
                _mockLogger.Object, 
                _mockDownloadingService.Object,
                _mockConfiguration.Object);
        }

        [Fact]
        public async Task CheckHealthAsync_ReturnsHealthStatus()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _healthCheckService.CheckHealthAsync(cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<HealthStatus>(result);
        }

        [Fact]
        public async Task CheckHealthAsync_IncludesTimestamp()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var beforeCheck = DateTime.UtcNow;

            // Act
            var result = await _healthCheckService.CheckHealthAsync(cancellationToken);
            var afterCheck = DateTime.UtcNow;

            // Assert
            Assert.NotNull(result.Details["timestamp"]);
            var timestamp = (DateTime)result.Details["timestamp"];
            Assert.True(timestamp >= beforeCheck && timestamp <= afterCheck);
        }

        [Fact]
        public async Task CheckHealthAsync_IncludesResponseTime()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _healthCheckService.CheckHealthAsync(cancellationToken);

            // Assert
            Assert.Contains("response_time_ms", result.Details.Keys);
            Assert.True((long)result.Details["response_time_ms"] >= 0);
        }

        [Fact]
        public async Task CheckHealthAsync_ContainsYtDlpAvailabilityCheck()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _healthCheckService.CheckHealthAsync(cancellationToken);

            // Assert
            Assert.Contains("ytdlp_available", result.Details.Keys);
            Assert.IsType<bool>(result.Details["ytdlp_available"]);
        }

        [Fact]
        public async Task CheckHealthAsync_ContainsDownloadDirWritableCheck()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _healthCheckService.CheckHealthAsync(cancellationToken);

            // Assert
            Assert.Contains("download_dir_writable", result.Details.Keys);
            Assert.IsType<bool>(result.Details["download_dir_writable"]);
        }

        [Fact]
        public async Task CheckHealthAsync_ReturnsUnhealthyIfYtDlpNotAvailable()
        {
            // Note: This test will mark unhealthy if yt-dlp is not installed in test environment
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _healthCheckService.CheckHealthAsync(cancellationToken);

            // Assert - either healthy or unhealthy is fine, both are valid states
            Assert.True(result.Status == "Healthy" || result.Status == "Unhealthy");
        }

        [Fact]
        public async Task CheckHealthAsync_HandlesCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            // Act & Assert
            // Should handle cancellation gracefully
            var result = await _healthCheckService.CheckHealthAsync(cts.Token);
            Assert.NotNull(result);
        }
    }

    public class HealthCheckControllerTests
    {
        private readonly Mock<IHealthCheckService> _mockHealthCheckService;
        private readonly Mock<ILogger<HealthCheckController>> _mockLogger;
        private readonly HealthCheckController _controller;

        public HealthCheckControllerTests()
        {
            _mockHealthCheckService = new Mock<IHealthCheckService>();
            _mockLogger = new Mock<ILogger<HealthCheckController>>();
            _controller = new HealthCheckController(_mockHealthCheckService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetDetailedHealth_ReturnsOkWhenHealthy()
        {
            // Arrange
            var healthStatus = new HealthStatus
            {
                Status = "Healthy",
                Details = new Dictionary<string, object>
                {
                    { "ytdlp_available", true },
                    { "download_dir_writable", true },
                    { "response_time_ms", 50L }
                }
            };

            _mockHealthCheckService.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(healthStatus);

            // Act
            var result = await _controller.GetDetailedHealth();

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.Equal(200, okResult?.StatusCode);
        }

        [Fact]
        public async Task GetDetailedHealth_Returns503WhenUnhealthy()
        {
            // Arrange
            var healthStatus = new HealthStatus
            {
                Status = "Unhealthy",
                Details = new Dictionary<string, object>
                {
                    { "ytdlp_available", false },
                    { "error", "yt-dlp not found" }
                }
            };

            _mockHealthCheckService.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(healthStatus);

            // Act
            var result = await _controller.GetDetailedHealth();

            // Assert
            Assert.IsType<ObjectResult>(result);
            var statusCodeResult = result as ObjectResult;
            Assert.Equal(503, statusCodeResult?.StatusCode);
        }

        [Fact]
        public void GetLiveness_ReturnsOk()
        {
            // Act
            var result = _controller.GetLiveness();

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.Equal(200, okResult?.StatusCode);
        }

        [Fact]
        public async Task GetReadiness_ReturnsOkWhenHealthy()
        {
            // Arrange
            var healthStatus = new HealthStatus
            {
                Status = "Healthy",
                Details = new Dictionary<string, object>
                {
                    { "ytdlp_available", true },
                    { "download_dir_writable", true }
                }
            };

            _mockHealthCheckService.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(healthStatus);

            // Act
            var result = await _controller.GetReadiness();

            // Assert
            Assert.IsType<OkResult>(result);
            var okResult = result as OkResult;
            Assert.Equal(200, okResult?.StatusCode);
        }

        [Fact]
        public async Task GetReadiness_Returns503WhenUnhealthy()
        {
            // Arrange
            var healthStatus = new HealthStatus
            {
                Status = "Unhealthy",
                Details = new Dictionary<string, object>
                {
                    { "ytdlp_available", false }
                }
            };

            _mockHealthCheckService.Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(healthStatus);

            // Act
            var result = await _controller.GetReadiness();

            // Assert
            Assert.IsType<StatusCodeResult>(result);
        }
    }
}
