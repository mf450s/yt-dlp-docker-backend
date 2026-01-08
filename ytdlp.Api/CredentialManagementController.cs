using Microsoft.AspNetCore.Mvc;
using System.Text;
using ytdlp.Services.Interfaces;
using FluentResults;

namespace ytdlp.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CredentialManagementController(
        ICookiesService cookiesService,
        ILogger<CredentialManagementController> logger
        ) : ControllerBase
    {
        private readonly ILogger<CredentialManagementController> _logger = logger;

        /// <summary>
        /// Retrieves all available cookie file names.
        /// </summary>
        [HttpGet]
        public List<string> GetAllCookieNames()
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogDebug(
                "[{CorrelationId}] 🍪 GetAllCookieNames request received",
                correlationId);

            var cookies = cookiesService.GetAllCookieNames();
            _logger.LogDebug(
                "[{CorrelationId}] 🍪 Returning {Count} cookie files",
                correlationId, cookies.Count);

            return cookies;
        }

        /// <summary>
        /// Retrieves the content of a specific cookie file by name.
        /// </summary>
        /// <param name="cookieName">The name of the cookie file.</param>
        [HttpGet("{cookieName}")]
        public IActionResult GetCookieContentByName(string cookieName)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogDebug(
                "[{CorrelationId}] 🍪 GetCookieContentByName request | Cookie: {CookieName}",
                correlationId, cookieName);

            Result<string> cookieContent = cookiesService.GetCookieContentByName(cookieName);
            if (cookieContent.IsFailed)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Cookie not found | Cookie: {CookieName}",
                    correlationId, cookieName);
                return NotFound(new { error = cookieContent.Errors[0].Message, correlationId });
            }

            _logger.LogDebug(
                "[{CorrelationId}] 🍪 Returning cookie content | Cookie: {CookieName} | Size: {Size} bytes",
                correlationId, cookieName, cookieContent.Value.Length);

            return Ok(cookieContent.Value);
        }

        /// <summary>
        /// Deletes a cookie file by name.
        /// </summary>
        /// <param name="cookieName">The name of the cookie file to delete.</param>
        [HttpDelete("{cookieName}")]
        public IActionResult DeleteCookieByName(string cookieName)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "[{CorrelationId}] 🗑️ DeleteCookieByName request | Cookie: {CookieName}",
                correlationId, cookieName);

            Result<string> result = cookiesService.DeleteCookieByName(cookieName);
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[{CorrelationId}] ✅ Cookie deleted successfully | Cookie: {CookieName}",
                    correlationId, cookieName);
                return NoContent();
            }
            else
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Failed to delete cookie | Cookie: {CookieName}",
                    correlationId, cookieName);
                return NotFound(new { error = result.Errors[0].Message, correlationId });
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
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "[{CorrelationId}] 🍪 CreateNewCookie request | Cookie: {CookieName}",
                correlationId, cookieName);

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string cookieContent = await reader.ReadToEndAsync();

            _logger.LogDebug(
                "[{CorrelationId}] 🍪 Creating cookie {CookieName} with {Size} bytes",
                correlationId, cookieName, cookieContent.Length);

            Result<string> result = await cookiesService.CreateNewCookieAsync(cookieName, cookieContent);
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[{CorrelationId}] ✅ Cookie created successfully | Cookie: {CookieName}",
                    correlationId, cookieName);
                return Created(cookieName, new { name = cookieName, message = result.Value, correlationId });
            }
            else
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Failed to create cookie | Cookie: {CookieName} | Error: {Error}",
                    correlationId, cookieName, result.Value);
                return Conflict(new { error = result.Value, correlationId });
            }
        }

        /// <summary>
        /// Updates the content of an existing cookie file.
        /// </summary>
        /// <param name="cookieName">The name of the cookie file.</param>
        [HttpPatch("{cookieName}")]
        public async Task<IActionResult> SetCookieContentAsync(string cookieName)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "[{CorrelationId}] 🍪 SetCookieContent request | Cookie: {CookieName}",
                correlationId, cookieName);

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string cookieContent = await reader.ReadToEndAsync();

            _logger.LogDebug(
                "[{CorrelationId}] 🍪 Updating cookie {CookieName} with {Size} bytes",
                correlationId, cookieName, cookieContent.Length);

            Result<string> result = await cookiesService.SetCookieContentAsync(cookieName, cookieContent);
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[{CorrelationId}] ✅ Cookie updated successfully | Cookie: {CookieName}",
                    correlationId, cookieName);
                return Ok(new { name = cookieName, message = result.Value, correlationId });
            }
            else
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Failed to update cookie | Cookie: {CookieName}",
                    correlationId, cookieName);
                return NotFound(new { error = result.Errors[0].Message, correlationId });
            }
        }
    }
}
