using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ytdlp.Services.Interfaces;

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
            _logger.LogInformation("Starting download for URL: {Url} with config: {ConfigFile}", url, configFile);
            
            try
            {
                string wholeConfigPath = _configsServices.GetWholeConfigPath(configFile);
                _logger.LogDebug("Resolved config path: {ConfigPath}", wholeConfigPath);
                
                ProcessStartInfo startInfo = await GetProcessStartInfoAsync(url, wholeConfigPath);
                
                using IProcess process = _processFactory.CreateProcess();
                process.StartInfo = startInfo;
                
                _logger.LogDebug("Starting yt-dlp process with arguments: {Arguments}", startInfo.Arguments);
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Download completed successfully for URL: {Url}", url);
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        _logger.LogDebug("yt-dlp output: {Output}", output.Trim());
                    }
                }
                else
                {
                    _logger.LogError("Download failed for URL: {Url} with exit code: {ExitCode}. Error: {Error}", 
                        url, process.ExitCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while downloading from URL: {Url} with config: {ConfigFile}", 
                    url, configFile);
                throw;
            }
        }

        /// <summary>
        /// Creates and returns a <see cref="ProcessStartInfo"/> object with the necessary arguments to execute yt-dlp.
        /// </summary>
        /// <param name="url">The URL of the media to download.</param>
        /// <param name="wholeConfigPath">The path to the configuration file for yt-dlp.</param>
        /// <returns>A <see cref="ProcessStartInfo"/> object configured to run yt-dlp with the provided URL and configuration.</returns>
        internal static async Task<ProcessStartInfo> GetProcessStartInfoAsync(string url, string wholeConfigPath)
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
    }
}
