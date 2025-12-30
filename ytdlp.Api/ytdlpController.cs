using Microsoft.AspNetCore.Mvc;
using ytdlp.Services.Interfaces;
using FluentResults;
using System.Text;

namespace ytdlp.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ytdlpController(IDownloadingService downloadingService, IConfigsServices configsServices) : ControllerBase
    {
        /// <summary>
        /// Downloads content from a URL using a specified configuration.
        /// Cookie files should be specified within the config file using --cookies option.
        /// </summary>
        /// <param name="url">The URL to download from.</param>
        /// <param name="confName">The name of the configuration file to use.</param>
        [HttpPost("download")]
        public async Task<IActionResult> Download([FromBody] string url, [FromQuery] string confName)
        {
            // Validate configuration file exists
            var configResult = configsServices.GetConfigContentByName(confName);
            if (configResult.IsFailed)
            {
                return BadRequest(new { error = $"Configuration '{confName}' not found." });
            }

            try
            {
                await downloadingService.TryDownloadingFromURL(url, confName);
                return Accepted(new { message = "Download started", url = url, config = confName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while starting the download: {ex.Message}" });
            }
        }

        /// <summary>
        /// Retrieves all available configuration file names.
        /// </summary>
        [HttpGet("config")]
        public List<string> GetAllConfigNames()
        {
            return configsServices.GetAllConfigNames();
        }

        /// <summary>
        /// Retrieves the content of a specific configuration file by name.
        /// </summary>
        /// <param name="configName">The name of the configuration file.</param>
        [HttpGet("config/{configName}")]
        public IActionResult GetConfigContentByName(string configName)
        {
            Result<string> configContent = configsServices.GetConfigContentByName(configName);
            if (configContent.IsFailed)
            {
                return NotFound(new { error = configContent.Errors[0].Message });
            }
            return Ok(configContent.Value);
        }

        /// <summary>
        /// Deletes a configuration file by name.
        /// </summary>
        /// <param name="configName">The name of the configuration file to delete.</param>
        [HttpDelete("config/{configName}")]
        public IActionResult DeleteConfigByName(string configName)
        {
            Result<string> result = configsServices.DeleteConfigByName(configName);
            if (result.IsSuccess)
                return NoContent();
            else
                return NotFound(new { error = result.Errors[0].Message });
        }

        /// <summary>
        /// Creates a new configuration file with the provided content.
        /// </summary>
        /// <param name="configName">The name of the configuration file to create.</param>
        [HttpPost("config/{configName}")]
        public async Task<IActionResult> CreateNewConfigAsync(string configName)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string configContent = await reader.ReadToEndAsync();

            Result<string> result = await configsServices.CreateNewConfigAsync(configName, configContent);
            if (result.IsSuccess)
                return Created();
            else
                return Conflict(new { error = result.Value });
        }

        /// <summary>
        /// Updates the content of an existing configuration file.
        /// </summary>
        /// <param name="configName">The name of the configuration file.</param>
        [HttpPatch("config/{configName}")]
        public async Task<IActionResult> SetConfigContentAsync(string configName)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string configContent = await reader.ReadToEndAsync();
            Result<string> result = await configsServices.SetConfigContentAsync(configName, configContent);
            if (result.IsSuccess)
                return Ok(configContent);
            else
                return NotFound(new { error = configContent });
        }
    }
}
