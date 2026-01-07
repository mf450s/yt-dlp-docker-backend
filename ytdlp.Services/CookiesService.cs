using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ytdlp.Services.Interfaces;

namespace ytdlp.Services
{
    /// <summary>
    /// Service for managing cookie files used by yt-dlp.
    /// Supports Netscape format and JSON-based cookie files.
    /// </summary>
    public class CookiesService(
        IPathParserService pathParserService, 
        IConfiguration configuration,
        ILogger<CookiesService> logger
        ) : ICookiesService
    {
        private readonly IPathParserService _pathParserService = pathParserService ?? throw new ArgumentNullException(nameof(pathParserService));
        private readonly string cookiePath = configuration["Paths:Cookies"] ?? "/app/cookies";
        private readonly ILogger<CookiesService> _logger = logger;

        /// <summary>
        /// Retrieves all available cookie file names from the cookies directory.
        /// </summary>
        public List<string> GetAllCookieNames()
        {
            _logger.LogDebug("Retrieving all cookie names from: {CookiePath}", cookiePath);
            
            try
            {
                if (!Directory.Exists(cookiePath))
                {
                    _logger.LogWarning("Cookie directory does not exist: {CookiePath}", cookiePath);
                    return [];
                }

                var files = Directory.GetFiles(cookiePath).ToList();
                _logger.LogInformation("Found {Count} cookie files", files.Count);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cookie names from {CookiePath}", cookiePath);
                return new List<string>();
            }
        }

        /// <summary>
        /// Retrieves the content of a specific cookie file.
        /// </summary>
        public Result<string> GetCookieContentByName(string cookieName)
        {
            _logger.LogDebug("Retrieving cookie content for: {CookieName}", cookieName);
            
            if (string.IsNullOrWhiteSpace(cookieName))
            {
                _logger.LogWarning("GetCookieContentByName called with empty cookie name");
                return Result.Fail("Cookie name cannot be empty.");
            }

            try
            {
                string wholePath = GetWholeCookiePath(cookieName);

                if (!File.Exists(wholePath))
                {
                    _logger.LogWarning("Cookie file not found: {CookieName} at {Path}", cookieName, wholePath);
                    return Result.Fail($"Cookie file '{cookieName}' not found.");
                }

                string content = File.ReadAllText(wholePath);
                _logger.LogInformation("Successfully retrieved cookie: {CookieName} ({Size} bytes)", cookieName, content.Length);
                return Result.Ok(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading cookie file: {CookieName}", cookieName);
                return Result.Fail($"Error reading cookie file: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a cookie file by name.
        /// </summary>
        public Result<string> DeleteCookieByName(string cookieName)
        {
            _logger.LogInformation("Attempting to delete cookie: {CookieName}", cookieName);
            
            if (string.IsNullOrWhiteSpace(cookieName))
            {
                _logger.LogWarning("DeleteCookieByName called with empty cookie name");
                return Result.Fail("Cookie name cannot be empty.");
            }

            try
            {
                string wholePath = GetWholeCookiePath(cookieName);

                if (!File.Exists(wholePath))
                {
                    _logger.LogWarning("Cannot delete - cookie file not found: {CookieName}", cookieName);
                    return Result.Fail($"Cookie file '{cookieName}' not found.");
                }

                File.Delete(wholePath);
                _logger.LogInformation("Successfully deleted cookie: {CookieName}", cookieName);
                return Result.Ok($"Cookie file '{cookieName}' deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cookie file: {CookieName}", cookieName);
                return Result.Fail($"Error deleting cookie file: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a new cookie file with the provided content.
        /// </summary>
        public async Task<Result<string>> CreateNewCookieAsync(string cookieName, string cookieContent)
        {
            _logger.LogInformation("Creating new cookie file: {CookieName}", cookieName);
            
            if (string.IsNullOrWhiteSpace(cookieName))
            {
                _logger.LogWarning("CreateNewCookieAsync called with empty cookie name");
                return Result.Fail("Cookie name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(cookieContent))
            {
                _logger.LogWarning("CreateNewCookieAsync called with empty content for {CookieName}", cookieName);
                return Result.Fail("Cookie content cannot be empty.");
            }

            try
            {
                string wholePath = GetWholeCookiePath(cookieName);

                // Ensure directory exists
                string directory = Path.GetDirectoryName(wholePath)!;
                if (!Directory.Exists(directory))
                {
                    _logger.LogDebug("Creating cookie directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                // Check if file already exists
                if (File.Exists(wholePath))
                {
                    _logger.LogWarning("Cannot create cookie - file already exists: {CookieName}", cookieName);
                    return Result.Fail($"Cookie file '{cookieName}' already exists.");
                }

                // Validate cookie content format (basic validation)
                if (!IsValidCookieFormat(cookieContent))
                {
                    _logger.LogWarning("Invalid cookie format for: {CookieName}", cookieName);
                    return Result.Fail("Invalid cookie file format. Expected Netscape format or valid JSON.");
                }

                await File.WriteAllTextAsync(wholePath, cookieContent);
                _logger.LogInformation("Successfully created cookie: {CookieName} ({Size} bytes)", cookieName, cookieContent.Length);
                return Result.Ok($"Cookie file '{cookieName}' created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating cookie file: {CookieName}", cookieName);
                return Result.Fail($"Error creating cookie file: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the content of an existing cookie file.
        /// </summary>
        public async Task<Result<string>> SetCookieContentAsync(string cookieName, string cookieContent)
        {
            _logger.LogInformation("Updating cookie file: {CookieName}", cookieName);
            
            if (string.IsNullOrWhiteSpace(cookieName))
            {
                _logger.LogWarning("SetCookieContentAsync called with empty cookie name");
                return Result.Fail("Cookie name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(cookieContent))
            {
                _logger.LogWarning("SetCookieContentAsync called with empty content for {CookieName}", cookieName);
                return Result.Fail("Cookie content cannot be empty.");
            }

            try
            {
                string wholePath = GetWholeCookiePath(cookieName);

                if (!File.Exists(wholePath))
                {
                    _logger.LogWarning("Cannot update - cookie file not found: {CookieName}", cookieName);
                    return Result.Fail($"Cookie file '{cookieName}' not found.");
                }

                // Validate cookie content format
                if (!IsValidCookieFormat(cookieContent))
                {
                    _logger.LogWarning("Invalid cookie format for: {CookieName}", cookieName);
                    return Result.Fail("Invalid cookie file format. Expected Netscape format or valid JSON.");
                }

                await File.WriteAllTextAsync(wholePath, cookieContent);
                _logger.LogInformation("Successfully updated cookie: {CookieName} ({Size} bytes)", cookieName, cookieContent.Length);
                return Result.Ok($"Cookie file '{cookieName}' updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cookie file: {CookieName}", cookieName);
                return Result.Fail($"Error updating cookie file: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the full file path for a cookie file.
        /// </summary>
        public string GetWholeCookiePath(string cookieName)
        {
            return Path.Combine(cookiePath, cookieName);
        }

        /// <summary>
        /// Basic validation for cookie file format.
        /// Supports Netscape format (tab-separated) and JSON format.
        /// </summary>
        private bool IsValidCookieFormat(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            // Allow Netscape format comments
            if (content.StartsWith("#") || content.StartsWith("// Netscape HTTP Cookie File"))
                return true;

            // Allow JSON format
            if (content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("["))
                return true;

            // Allow tab-separated Netscape format
            var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0 && lines.All(line => line.StartsWith("#") || ContainsTabSeparatedCookieFormat(line)))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if a line follows the Netscape cookie format (tab-separated values).
        /// </summary>
        private static bool ContainsTabSeparatedCookieFormat(string line)
        {
            // Netscape format: domain, flag, path, secure, expiration, name, value
            var parts = line.Split('\t');
            return parts.Length == 7 || line.StartsWith("#");
        }
    }
}
