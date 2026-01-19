using Microsoft.AspNetCore.Mvc;
using ytdlp.Services.Interfaces;

namespace ytdlp.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class DownloadsController(
        IDownloadingService downloadingService,
        IConfigsServices configsServices,
        ILogger<DownloadsController> logger
        ) : ControllerBase
    {
        private readonly ILogger<DownloadsController> _logger = logger;

        /// <summary>
        /// Downloads content from a URL using a specified configuration.
        /// Cookie files should be specified within the config file using --cookies option.
        /// </summary>
        /// <param name="url">The URL to download from.</param>
        /// <param name="confName">The name of the configuration file to use.</param>
        [HttpPost("download")]
        public async Task<IActionResult> Download([FromBody] string url, [FromQuery] string confName)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "[{CorrelationId}] ⬇️ Download request received | URL: {Url} | Config: {ConfigName}",
                correlationId, url, confName);

            // Validate configuration file exists
            var configResult = configsServices.GetConfigContentByName(confName);
            if (configResult.IsFailed)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Download validation failed | Config '{ConfigName}' not found",
                    correlationId, confName);
                return BadRequest(new { error = $"Configuration '{confName}' not found.", correlationId });
            }

            try
            {
                downloadingService.TryDownloadingFromURL(url, confName);
                _logger.LogInformation(
                    "[{CorrelationId}] ✅ Download accepted and queued | URL: {Url} | Config: {ConfigName}",
                    correlationId, url, confName);
                return Accepted(new { message = "Download started", url, config = confName, correlationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[{CorrelationId}] 🚨 Error starting download | URL: {Url} | Config: {ConfigName}",
                    correlationId, url, confName);
                return StatusCode(500, new { error = $"An error occurred while starting the download: {ex.Message}", correlationId });
            }
        }
    }
}
