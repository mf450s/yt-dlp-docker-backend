using Microsoft.AspNetCore.Mvc;
using System.Text;
using ytdlp.Services.Interfaces;
using FluentResults;

namespace ytdlp.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CookiesController(ICookiesService cookiesService) : ControllerBase
    {
        /// <summary>
        /// Retrieves all available cookie file names.
        /// </summary>
        [HttpGet]
        public List<string> GetAllCookieNames()
        {
            return cookiesService.GetAllCookieNames();
        }

        /// <summary>
        /// Retrieves the content of a specific cookie file by name.
        /// </summary>
        /// <param name="cookieName">The name of the cookie file.</param>
        [HttpGet("{cookieName}")]
        public IActionResult GetCookieContentByName(string cookieName)
        {
            Result<string> cookieContent = cookiesService.GetCookieContentByName(cookieName);
            if (cookieContent.IsFailed)
            {
                return NotFound(new { error = cookieContent.Errors[0].Message });
            }
            return Ok(new { name = cookieName, content = cookieContent.Value });
        }

        /// <summary>
        /// Deletes a cookie file by name.
        /// </summary>
        /// <param name="cookieName">The name of the cookie file to delete.</param>
        [HttpDelete("{cookieName}")]
        public IActionResult DeleteCookieByName(string cookieName)
        {
            Result<string> result = cookiesService.DeleteCookieByName(cookieName);
            if (result.IsSuccess)
                return NoContent();
            else
                return NotFound(new { error = result.Errors[0].Message });
        }

        /// <summary>
        /// Creates a new cookie file with the provided content.
        /// Supports Netscape format and JSON-based cookie files.
        /// </summary>
        /// <param name="cookieName">The name of the cookie file to create.</param>
        [HttpPost("{cookieName}")]
        public async Task<IActionResult> CreateNewCookieAsync(string cookieName)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string cookieContent = await reader.ReadToEndAsync();

            Result<string> result = await cookiesService.CreateNewCookieAsync(cookieName, cookieContent);
            if (result.IsSuccess)
                return Created($"api/cookies/{cookieName}", new { name = cookieName, message = result.Value });
            else
                return Conflict(new { error = result.Value });
        }

        /// <summary>
        /// Updates the content of an existing cookie file.
        /// </summary>
        /// <param name="cookieName">The name of the cookie file.</param>
        [HttpPatch("{cookieName}")]
        public async Task<IActionResult> SetCookieContentAsync(string cookieName)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string cookieContent = await reader.ReadToEndAsync();

            Result<string> result = await cookiesService.SetCookieContentAsync(cookieName, cookieContent);
            if (result.IsSuccess)
                return Ok(new { name = cookieName, message = result.Value });
            else
                return NotFound(new { error = result.Errors[0].Message });
        }
    }
}
