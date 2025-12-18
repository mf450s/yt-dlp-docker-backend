using Microsoft.AspNetCore.Mvc;
using ytdlp.Services.Interfaces;
using FluentResults;

namespace ytdlp.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ytdlpController(IDownloadingService downloadingService, IConfigsServices configsServices) : ControllerBase
    {
        [HttpPost("download")]
        public IActionResult Download([FromBody] string url, [FromQuery] string configfilename)
        {
            downloadingService.TryDownloadingFromURL(url, configfilename);
            return Accepted();
        }
        [HttpGet("config")]
        public List<string> GetConfigFileNames()
        {
            return configsServices.GetAllConfigNames();
        }
        [HttpGet("config/{configName}")]
        public IActionResult GetConfigByName(string configName)
        {
            Result<string> configContent = configsServices.GetConfigContentByName(configName);
            if (configContent.IsFailed)
            {
                return NotFound(configContent.Errors.First().Message);
            }
            return Ok(configContent.Value);
        }
    }
}
