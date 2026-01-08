using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ytdlp.Services.Interfaces;
using ytdlp.Services.Logging;

namespace ytdlp.Services
{
    public class DownloadingService(
        IConfigsServices configsServices,
        ILogger<DownloadingService> logger,
        IProcessFactory? processFactory = null
        ) : IDownloadingService
    {
        private readonly IConfigsServices _configsServices = configsServices;
        private readonly ILogger<DownloadingService> _logger = logger;
        private readonly IProcessFactory _processFactory = processFactory ?? new ProcessFactory();

        public async Task TryDownloadingFromURL(string url, string configFile)
        {
            var stopwatch = Stopwatch.StartNew();
            bool isSpotify = IsSpotifyUrl(url);
            string toolName = isSpotify ? "Zotify" : "yt-dlp";

            _logger.LogDownloadStarted(url, configFile);

            try
            {
                string wholeConfigPath = _configsServices.GetWholeConfigPath(configFile);
                _logger.LogConfigPathResolved(configFile, wholeConfigPath);

                ProcessStartInfo startInfo = await GetProcessStartInfoAsync(url, wholeConfigPath, isSpotify);

                using IProcess process = _processFactory.CreateProcess();
                process.StartInfo = startInfo;

                _logger.LogProcessStarted(toolName, startInfo.Arguments);
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                stopwatch.Stop();

                if (process.ExitCode == 0)
                {
                    _logger.LogDownloadCompleted(url, stopwatch.Elapsed);
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        _logger.LogDebug("📁 {ToolName} output: {Output}", toolName, output.Trim());
                    }
                }
                else
                {
                    _logger.LogDownloadFailed(url, process.ExitCode, error);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "🚨 Exception during download | URL: {Url} | Config: {ConfigFile} | Duration: {DurationMs}ms",
                    url, configFile, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Determines if the provided URL is a Spotify URL.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <returns>True if the URL is a Spotify URL, false otherwise.</returns>
        private static bool IsSpotifyUrl(string url)
        {
            return !string.IsNullOrWhiteSpace(url) &&
                   (url.Contains("spotify.com", StringComparison.OrdinalIgnoreCase) ||
                    url.Contains("open.spotify.com", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Creates and returns a <see cref="ProcessStartInfo"/> object with the necessary arguments.
        /// Routes to either Zotify or yt-dlp based on the URL type.
        /// </summary>
        /// <param name="url">The URL of the media to download.</param>
        /// <param name="wholeConfigPath">The path to the configuration file.</param>
        /// <param name="isSpotify">Whether the URL is a Spotify URL.</param>
        /// <returns>A <see cref="ProcessStartInfo"/> object configured to run the appropriate tool.</returns>
        internal static async Task<ProcessStartInfo> GetProcessStartInfoAsync(string url, string wholeConfigPath, bool isSpotify = false)
        {
            if (isSpotify)
            {
                return await GetZotifyProcessStartInfoAsync(url, wholeConfigPath);
            }
            else
            {
                return await GetYtDlpProcessStartInfoAsync(url, wholeConfigPath);
            }
        }

        /// <summary>
        /// Creates and returns a <see cref="ProcessStartInfo"/> object configured to run yt-dlp.
        /// </summary>
        /// <param name="url">The URL of the media to download.</param>
        /// <param name="wholeConfigPath">The path to the yt-dlp configuration file.</param>
        /// <returns>A <see cref="ProcessStartInfo"/> object configured to run yt-dlp with the provided URL and configuration.</returns>
        internal static async Task<ProcessStartInfo> GetYtDlpProcessStartInfoAsync(string url, string wholeConfigPath)
        {
            string[] args =
            [
                url,
                $"--config-locations", wholeConfigPath
            ];

            ProcessStartInfo startInfo = new()
            {
                FileName = "yt-dlp",
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            return await Task.FromResult(startInfo);
        }

        /// <summary>
        /// Creates and returns a <see cref="ProcessStartInfo"/> object configured to run Zotify.
        /// </summary>
        /// <param name="url">The Spotify URL to download.</param>
        /// <param name="wholeConfigPath">The path to the Zotify configuration file.</param>
        /// <returns>A <see cref="ProcessStartInfo"/> object configured to run Zotify with the provided URL and configuration.</returns>
        internal static async Task<ProcessStartInfo> GetZotifyProcessStartInfoAsync(string url, string wholeConfigPath)
        {
            string[] args =
            [
                url,
                $"--config-locations", wholeConfigPath
            ];

            ProcessStartInfo startInfo = new()
            {
                FileName = "zotify",
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            return await Task.FromResult(startInfo);
        }
    }
}
