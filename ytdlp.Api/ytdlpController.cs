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
        [HttpPost("download")]
        public IActionResult Download([FromBody] string url, [FromQuery] string confName)
        {
            downloadingService.TryDownloadingFromURL(url, confName);
            return Accepted();
        }
        [HttpGet("config")]
        public List<string> GetAllConfigNames()
        {
            return configsServices.GetAllConfigNames();
        }
        [HttpGet("config/{configName}")]
        public IActionResult GetConfigContentByName(string configName)
        {
            Result<string> configContent = configsServices.GetConfigContentByName(configName);
            if (configContent.IsFailed)
            {
                return NotFound(configContent.Errors[0].Message);
            }
            return Ok(configContent.Value);
        }
        [HttpDelete("config/{configName}")]
        public IActionResult DeleteConfigByName(string configName)
        {
            Result<string> result = configsServices.DeleteConfigByName(configName);
            if (result.IsSuccess)
                return NoContent();
            else return NotFound();
        }
        [HttpPost("config/{configName}")]
        public async Task<IActionResult> CreateNewConfigAsync(string configName)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string configContent = await reader.ReadToEndAsync();

            Result<string> result = await configsServices.CreateNewConfigAsync(configName, configContent);
            if (result.IsSuccess)
                return Created();
            else return Conflict(result.Value);
        }
        [HttpPatch("config/{configName}")]
        public async Task<IActionResult> SetConfigContentAsync(string configName, [FromBody] string configContent)
        {
            Result<string> result = await configsServices.SetConfigContentAsync(configName, configContent);
            if (result.IsSuccess) return Ok(configContent);
            else return NotFound(configContent);
        }
    }
}
