using Microsoft.AspNetCore.Mvc;
using ytdlp.Services.Interfaces;
using FluentResults;
using System.Text;

namespace ytdlp.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigsController(
        IConfigsServices configsServices,
        ILogger<DownloadsController> logger
        ) : ControllerBase
    {
        private readonly ILogger<DownloadsController> _logger = logger;
        /// <summary>
        /// Retrieves all available configuration file names.
        /// </summary>
        [HttpGet]
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
        [HttpGet("{configName}")]
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
        [HttpDelete("{configName}")]
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
        [HttpPost("{configName}")]
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
                return Created(configName, new { name = configName, message = result.Value, correlationId });
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
        [HttpPatch("{configName}")]
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