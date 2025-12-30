using ytdlp.Services.Interfaces;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

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

    public class HealthCheckService : IHealthCheckService
    {
        private readonly ILogger<HealthCheckService> _logger;
        private readonly IDownloadingService _downloadingService;

        public HealthCheckService(ILogger<HealthCheckService> logger, IDownloadingService downloadingService)
        {
            _logger = logger;
            _downloadingService = downloadingService;
        }

        public async Task<HealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var status = new HealthStatus();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. Check if yt-dlp is available
                var ytdlpAvailable = await CheckYtDlpAvailabilityAsync(cancellationToken);
                status.Details["ytdlp_available"] = ytdlpAvailable;

                if (!ytdlpAvailable)
                {
                    status.Status = "Unhealthy";
                    _logger.LogWarning("yt-dlp is not available or not accessible");
                }

                // 2. Check if downloads directory is writable
                var downloadDirWritable = CheckDownloadDirWritable();
                status.Details["download_dir_writable"] = downloadDirWritable;

                if (!downloadDirWritable)
                {
                    status.Status = "Unhealthy";
                    _logger.LogWarning("Downloads directory is not writable");
                }

                stopwatch.Stop();
                status.Details["response_time_ms"] = stopwatch.ElapsedMilliseconds;
                status.Details["timestamp"] = DateTime.UtcNow;

                _logger.LogInformation("Health check completed with status: {Status}", status.Status);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                status.Status = "Unhealthy";
                status.Details["error"] = ex.Message;
                status.Details["response_time_ms"] = stopwatch.ElapsedMilliseconds;

                _logger.LogError(ex, "Health check failed");
            }

            return status;
        }

        private async Task<bool> CheckYtDlpAvailabilityAsync(CancellationToken cancellationToken)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processInfo })
                {
                    process.Start();

                    var versionTask = process.StandardOutput.ReadLineAsync();
                    var completedTask = await Task.WhenAny(
                        versionTask,
                        Task.Delay(5000, cancellationToken) // 5 second timeout
                    );

                    if (completedTask == versionTask && !string.IsNullOrEmpty(await versionTask))
                    {
                        process.Kill();
                        return true;
                    }

                    if (!process.HasExited)
                    {
                        process.Kill();
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking yt-dlp availability");
                return false;
            }
        }

        private bool CheckDownloadDirWritable()
        {
            try
            {
                var downloadDir = Path.Combine("/downloads", "test_health_check");
                Directory.CreateDirectory(downloadDir);

                var testFile = Path.Combine(downloadDir, ".health_check_test");
                File.WriteAllText(testFile, "health_check");
                File.Delete(testFile);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download directory is not writable");
                return false;
            }
        }
    }
}
