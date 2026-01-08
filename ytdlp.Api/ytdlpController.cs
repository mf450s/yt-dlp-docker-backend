using Microsoft.AspNetCore.Mvc;
using ytdlp.Services.Interfaces;
using FluentResults;
using System.Text;

namespace ytdlp.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ytdlpController(
        IDownloadingService downloadingService, 
        IConfigsServices configsServices,
        ILogger<ytdlpController> logger
        ) : ControllerBase
    {
        private readonly ILogger<ytdlpController> _logger = logger;

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
                await downloadingService.TryDownloadingFromURL(url, confName);
                _logger.LogInformation(
                    "[{CorrelationId}] ✅ Download accepted and queued | URL: {Url} | Config: {ConfigName}",
                    correlationId, url, confName);
                return Accepted(new { message = "Download started", url = url, config = confName, correlationId });
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

        /// <summary>
        /// Retrieves all available configuration file names.
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetAllConfigNames()
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogDebug(
                "[{CorrelationId}] 📄 GetAllConfigNames request received",
                correlationId);
            
            var configs = configsServices.GetAllConfigNames();
            _logger.LogDebug(
                "[{CorrelationId}] 📊 Returning {Count} config names",
                correlationId, configs.Count);
            
            return Ok(configs);
        }

        /// <summary>
        /// Retrieves the content of a specific configuration file by name.
        /// </summary>
        /// <param name="configName">The name of the configuration file.</param>
        [HttpGet("config/{configName}")]
        public IActionResult GetConfigContentByName(string configName)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogDebug(
                "[{CorrelationId}] GetConfigContentByName request | Config: {ConfigName}",
                correlationId, configName);
            
            Result<string> configContent = configsServices.GetConfigContentByName(configName);
            if (configContent.IsFailed)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] Config not found | Config: {ConfigName}",
                    correlationId, configName);
                return NotFound(new { error = configContent.Errors[0].Message, correlationId });
            }
            
            _logger.LogDebug(
                "[{CorrelationId}] Returning config content | Config: {ConfigName} | Size: {Size} bytes",
                correlationId, configName, configContent.Value.Length);
            return Ok(configContent.Value);
        }

        /// <summary>
        /// Deletes a configuration file by name.
        /// </summary>
        /// <param name="configName">The name of the configuration file to delete.</param>
        [HttpDelete("config/{configName}")]
        public IActionResult DeleteConfigByName(string configName)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "[{CorrelationId}] DeleteConfigByName request | Config: {ConfigName}",
                correlationId, configName);
            
            Result<string> result = configsServices.DeleteConfigByName(configName);
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[{CorrelationId}] Config deleted successfully | Config: {ConfigName}",
                    correlationId, configName);
                return NoContent();
            }
            else
            {
                _logger.LogWarning(
                    "[{CorrelationId}] Failed to delete config | Config: {ConfigName}",
                    correlationId, configName);
                return NotFound(new { error = result.Errors[0].Message, correlationId });
            }
        }

        /// <summary>
        /// Creates a new configuration file with the provided content.
        /// </summary>
        /// <param name="configName">The name of the configuration file to create.</param>
        [HttpPost("config/{configName}")]
        public async Task<IActionResult> CreateNewConfigAsync(string configName)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "[{CorrelationId}] CreateNewConfig request | Config: {ConfigName}",
                correlationId, configName);
            
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string configContent = await reader.ReadToEndAsync();
            
            _logger.LogDebug(
                "[{CorrelationId}] Creating config {ConfigName} with {Size} bytes",
                correlationId, configName, configContent.Length);

            Result<string> result = await configsServices.CreateNewConfigAsync(configName, configContent);
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[{CorrelationId}] Config created successfully | Config: {ConfigName}",
                    correlationId, configName);
                return Created(configName, new {name = configName, message = result.Value, correlationId});
            }
            else
            {
                _logger.LogWarning(
                    "[{CorrelationId}] Failed to create config | Config: {ConfigName} | Error: {Error}",
                    correlationId, configName, result.Value);
                return Conflict(new { error = result.Value, correlationId });
            }
        }

        /// <summary>
        /// Updates the content of an existing configuration file.
        /// </summary>
        /// <param name="configName">The name of the configuration file.</param>
        [HttpPatch("config/{configName}")]
        public async Task<IActionResult> SetConfigContentAsync(string configName)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "[{CorrelationId}] SetConfigContent request | Config: {ConfigName}",
                correlationId, configName);
            
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string configContent = await reader.ReadToEndAsync();
            
            _logger.LogDebug(
                "[{CorrelationId}] Updating config {ConfigName} with {Size} bytes",
                correlationId, configName, configContent.Length);
            
            Result<string> result = await configsServices.SetConfigContentAsync(configName, configContent);
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[{CorrelationId}] Config updated successfully | Config: {ConfigName}",
                    correlationId, configName);
                return Ok(configContent);
            }
            else
            {
                _logger.LogWarning(
                    "[{CorrelationId}] Failed to update config | Config: {ConfigName}",
                    correlationId, configName);
                return NotFound(new { error = configContent, correlationId });
            }
        }
    }
}
