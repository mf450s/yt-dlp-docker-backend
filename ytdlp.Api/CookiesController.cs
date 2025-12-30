using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text;
using ytdlp.Services.Interfaces;
using FluentResults;

namespace ytdlp.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CookiesController(
        ICookiesService cookiesService,
        ILogger<CookiesController> logger
        ) : ControllerBase
    {
        private readonly ILogger<CookiesController> _logger = logger;

        /// <summary>
        /// Retrieves all available cookie file names.
        /// </summary>
        [HttpGet]
        public List<string> GetAllCookieNames()
        {
            _logger.LogDebug("GetAllCookieNames request received");
            var cookies = cookiesService.GetAllCookieNames();
            _logger.LogDebug("Returning {Count} cookie files", cookies.Count);
            return cookies;
        }

        /// <summary>
        /// Retrieves the content of a specific cookie file by name.
        /// </summary>
        /// <param name="cookieName">The name of the cookie file.</param>
        [HttpGet("{cookieName}")]
        public IActionResult GetCookieContentByName(string cookieName)
        {
            _logger.LogDebug("GetCookieContentByName request: {CookieName}", cookieName);
            
            Result<string> cookieContent = cookiesService.GetCookieContentByName(cookieName);
            if (cookieContent.IsFailed)
            {
                _logger.LogWarning("Cookie not found: {CookieName}", cookieName);
                return NotFound(new { error = cookieContent.Errors[0].Message });
            }
            
            _logger.LogDebug("Returning cookie content for: {CookieName}", cookieName);
            return Ok(new { name = cookieName, content = cookieContent.Value });
        }

        /// <summary>
        /// Deletes a cookie file by name.
        /// </summary>
        /// <param name="cookieName">The name of the cookie file to delete.</param>
        [HttpDelete("{cookieName}")]
        public IActionResult DeleteCookieByName(string cookieName)
        {
            _logger.LogInformation("DeleteCookieByName request: {CookieName}", cookieName);
            
            Result<string> result = cookiesService.DeleteCookieByName(cookieName);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Cookie deleted successfully: {CookieName}", cookieName);
                return NoContent();
            }
            else
            {
                _logger.LogWarning("Failed to delete cookie: {CookieName}", cookieName);
                return NotFound(new { error = result.Errors[0].Message });
            }
        }

        /// <summary>
        /// Creates a new cookie file with the provided content.
        /// Supports Netscape format and JSON-based cookie files.
        /// </summary>
        /// <param name="cookieName">The name of the cookie file to create.</param>
        [HttpPost("{cookieName}")]
        public async Task<IActionResult> CreateNewCookieAsync(string cookieName)
        {
            _logger.LogInformation("CreateNewCookie request: {CookieName}", cookieName);
            
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string cookieContent = await reader.ReadToEndAsync();
            
            _logger.LogDebug("Creating cookie {CookieName} with {Size} bytes", cookieName, cookieContent.Length);

            Result<string> result = await cookiesService.CreateNewCookieAsync(cookieName, cookieContent);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Cookie created successfully: {CookieName}", cookieName);
                return Created($"api/cookies/{cookieName}", new { name = cookieName, message = result.Value });
            }
            else
            {
                _logger.LogWarning("Failed to create cookie: {CookieName}, Error: {Error}", cookieName, result.Value);
                return Conflict(new { error = result.Value });
            }
        }

        /// <summary>
        /// Updates the content of an existing cookie file.
        /// </summary>
        /// <param name="cookieName">The name of the cookie file.</param>
        [HttpPatch("{cookieName}")]
        public async Task<IActionResult> SetCookieContentAsync(string cookieName)
        {
            _logger.LogInformation("SetCookieContent request: {CookieName}", cookieName);
            
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string cookieContent = await reader.ReadToEndAsync();
            
            _logger.LogDebug("Updating cookie {CookieName} with {Size} bytes", cookieName, cookieContent.Length);

            Result<string> result = await cookiesService.SetCookieContentAsync(cookieName, cookieContent);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Cookie updated successfully: {CookieName}", cookieName);
                return Ok(new { name = cookieName, message = result.Value });
            }
            else
            {
                _logger.LogWarning("Failed to update cookie: {CookieName}", cookieName);
                return NotFound(new { error = result.Errors[0].Message });
            }
        }
    }
}
