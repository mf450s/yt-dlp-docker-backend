using ytdlp.Services.Logging;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ytdlp.Services
{
    public interface IHealthCheckService
    {
        Task<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);
    }

    public class HealthStatus
    {
        public string Status { get; set; } = "Healthy";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public class HealthCheckService(
        ILogger<HealthCheckService> logger,
        IConfiguration configuration) : IHealthCheckService
    {
        private readonly ILogger<HealthCheckService> _logger = logger;
        private readonly string _downloadsPath = configuration["Paths:Downloads"] ?? "/app/downloads";

        public async Task<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var status = new HealthStatus();
            var stopwatch = Stopwatch.StartNew();
            _logger.LogHealthCheckStarted();

            try
            {
                // 1. Check if yt-dlp is available
                var ytdlpAvailable = await CheckYtDlpAvailabilityAsync(cancellationToken);
                status.Details["ytdlp_available"] = ytdlpAvailable;

                if (!ytdlpAvailable)
                {
                    status.Status = "Unhealthy";
                    _logger.LogHealthCheckCompleted(false, "yt-dlp is not available or not accessible");
                }

                // 2. Check if downloads directory is writable
                var downloadDirWritable = CheckDownloadDirWritable();
                status.Details["download_dir_writable"] = downloadDirWritable;

                if (!downloadDirWritable)
                {
                    status.Status = "Unhealthy";
                    _logger.LogHealthCheckCompleted(false, "Downloads directory is not writable");
                }

                stopwatch.Stop();
                status.Details["response_time_ms"] = stopwatch.ElapsedMilliseconds;
                status.Details["timestamp"] = DateTime.UtcNow;

                _logger.LogHealthCheckCompleted(status.Status == "Healthy");
                _logger.LogOperationDuration("HealthCheck", stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                status.Status = "Unhealthy";
                status.Details["error"] = ex.Message;
                status.Details["response_time_ms"] = stopwatch.ElapsedMilliseconds;

                _logger.LogError(ex, "🚨 Health check failed after {DurationMs}ms", stopwatch.ElapsedMilliseconds);
            }

            return status;
        }

        private async Task<bool> CheckYtDlpAvailabilityAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("🔍 Checking yt-dlp availability...");

                var processInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var versionTask = process.StandardOutput.ReadLineAsync();
                var completedTask = await Task.WhenAny(
                    versionTask,
                    Task.Delay(5000, cancellationToken) // 5 second timeout
                );

                if (completedTask == versionTask && !string.IsNullOrEmpty(await versionTask))
                {
                    process.Kill();
                    _logger.LogInformation("✅ yt-dlp is available");
                    return true;
                }

                if (!process.HasExited)
                {
                    process.Kill();
                }

                _logger.LogWarning("⚠️ yt-dlp check timed out or returned no output");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚨 Error checking yt-dlp availability");
                return false;
            }
        }

        private bool CheckDownloadDirWritable()
        {
            try
            {
                _logger.LogDebug("🔍 Checking download directory writeability at: {Path}", _downloadsPath);

                // Ensure the downloads directory exists
                if (!Directory.Exists(_downloadsPath))
                {
                    _logger.LogWarning("⚠️ Downloads directory does not exist: {Path}", _downloadsPath);
                    return false;
                }

                var testFile = Path.Combine(_downloadsPath, ".health_check_test");
                File.WriteAllText(testFile, "health_check");
                File.Delete(testFile);

                _logger.LogInformation("✅ Download directory is writable");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚨 Download directory is not writable at: {Path}", _downloadsPath);
                return false;
            }
        }
    }
}
