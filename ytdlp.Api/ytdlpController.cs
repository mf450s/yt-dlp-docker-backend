using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
            _logger.LogInformation("Download request received: URL={Url}, Config={ConfigName}", url, confName);
            
            // Validate configuration file exists
            var configResult = configsServices.GetConfigContentByName(confName);
            if (configResult.IsFailed)
            {
                _logger.LogWarning("Download request failed: Config '{ConfigName}' not found", confName);
                return BadRequest(new { error = $"Configuration '{confName}' not found." });
            }

            try
            {
                await downloadingService.TryDownloadingFromURL(url, confName);
                _logger.LogInformation("Download accepted: URL={Url}, Config={ConfigName}", url, confName);
                return Accepted(new { message = "Download started", url = url, config = confName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting download: URL={Url}, Config={ConfigName}", url, confName);
                return StatusCode(500, new { error = $"An error occurred while starting the download: {ex.Message}" });
            }
        }

        /// <summary>
        /// Retrieves all available configuration file names.
        /// </summary>
        [HttpGet("config")]
        public List<string> GetAllConfigNames()
        {
            _logger.LogDebug("GetAllConfigNames request received");
            var configs = configsServices.GetAllConfigNames();
            _logger.LogDebug("Returning {Count} config names", configs.Count);
            return configs;
        }

        /// <summary>
        /// Retrieves the content of a specific configuration file by name.
        /// </summary>
        /// <param name="configName">The name of the configuration file.</param>
        [HttpGet("config/{configName}")]
        public IActionResult GetConfigContentByName(string configName)
        {
            _logger.LogDebug("GetConfigContentByName request: {ConfigName}", configName);
            
            Result<string> configContent = configsServices.GetConfigContentByName(configName);
            if (configContent.IsFailed)
            {
                _logger.LogWarning("Config not found: {ConfigName}", configName);
                return NotFound(new { error = configContent.Errors[0].Message });
            }
            
            _logger.LogDebug("Returning config content for: {ConfigName}", configName);
            return Ok(configContent.Value);
        }

        /// <summary>
        /// Deletes a configuration file by name.
        /// </summary>
        /// <param name="configName">The name of the configuration file to delete.</param>
        [HttpDelete("config/{configName}")]
        public IActionResult DeleteConfigByName(string configName)
        {
            _logger.LogInformation("DeleteConfigByName request: {ConfigName}", configName);
            
            Result<string> result = configsServices.DeleteConfigByName(configName);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Config deleted successfully: {ConfigName}", configName);
                return NoContent();
            }
            else
            {
                _logger.LogWarning("Failed to delete config: {ConfigName}", configName);
                return NotFound(new { error = result.Errors[0].Message });
            }
        }

        /// <summary>
        /// Creates a new configuration file with the provided content.
        /// </summary>
        /// <param name="configName">The name of the configuration file to create.</param>
        [HttpPost("config/{configName}")]
        public async Task<IActionResult> CreateNewConfigAsync(string configName)
        {
            _logger.LogInformation("CreateNewConfig request: {ConfigName}", configName);
            
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string configContent = await reader.ReadToEndAsync();
            
            _logger.LogDebug("Creating config {ConfigName} with {Size} bytes", configName, configContent.Length);

            Result<string> result = await configsServices.CreateNewConfigAsync(configName, configContent);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Config created successfully: {ConfigName}", configName);
                return Created();
            }
            else
            {
                _logger.LogWarning("Failed to create config: {ConfigName}, Error: {Error}", configName, result.Value);
                return Conflict(new { error = result.Value });
            }
        }

        /// <summary>
        /// Updates the content of an existing configuration file.
        /// </summary>
        /// <param name="configName">The name of the configuration file.</param>
        [HttpPatch("config/{configName}")]
        public async Task<IActionResult> SetConfigContentAsync(string configName)
        {
            _logger.LogInformation("SetConfigContent request: {ConfigName}", configName);
            
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string configContent = await reader.ReadToEndAsync();
            
            _logger.LogDebug("Updating config {ConfigName} with {Size} bytes", configName, configContent.Length);
            
            Result<string> result = await configsServices.SetConfigContentAsync(configName, configContent);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Config updated successfully: {ConfigName}", configName);
                return Ok(configContent);
            }
            else
            {
                _logger.LogWarning("Failed to update config: {ConfigName}", configName);
                return NotFound(new { error = configContent });
            }
        }
    }
}
