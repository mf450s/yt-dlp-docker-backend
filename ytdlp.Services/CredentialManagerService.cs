using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ytdlp.Services.Interfaces;

namespace ytdlp.Services
{
    /// <summary>
    /// Service for managing credential files used by yt-dlp.
    /// Supports Netscape format and JSON-based credential files.
    /// </summary>
    public class ICredentialManagerService(
        IConfiguration configuration,
        ILogger<ICredentialManagerService> logger
        ) : ICredentialService
    {
        private readonly string credentialPath = configuration["Paths:Credentials"] ?? "/app/credentials";
        private readonly ILogger<ICredentialManagerService> _logger = logger;

        /// <summary>
        /// Retrieves all available credential file names from the credentials directory.
        /// </summary>
        public List<string> GetAllCredentialNames()
        {
            _logger.LogDebug("Retrieving all credential names from: {credentialPath}", credentialPath);

            try
            {
                if (!Directory.Exists(credentialPath))
                {
                    _logger.LogWarning("credential directory does not exist: {credentialPath}", credentialPath);
                    return [];
                }

                var files = Directory.GetFiles(credentialPath).ToList();
                _logger.LogInformation("Found {Count} credential files", files.Count);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving credential names from {credentialPath}", credentialPath);
                return [];
            }
        }

        /// <summary>
        /// Retrieves the content of a specific credential file.
        /// </summary>
        public Result<string> GetCredentialContentByName(string credentialName)
        {
            _logger.LogDebug("Retrieving credential content for: {credentialName}", credentialName);

            if (string.IsNullOrWhiteSpace(credentialName))
            {
                _logger.LogWarning("GetcredentialContentByName called with empty credential name");
                return Result.Fail("credential name cannot be empty.");
            }

            try
            {
                string wholePath = GetWholeCredentialPath(credentialName);

                if (!File.Exists(wholePath))
                {
                    _logger.LogWarning("credential file not found: {credentialName} at {Path}", credentialName, wholePath);
                    return Result.Fail($"credential file '{credentialName}' not found.");
                }

                string content = File.ReadAllText(wholePath);
                _logger.LogInformation("Successfully retrieved credential: {credentialName} ({Size} bytes)", credentialName, content.Length);
                return Result.Ok(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading credential file: {credentialName}", credentialName);
                return Result.Fail($"Error reading credential file: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a credential file by name.
        /// </summary>
        public Result<string> DeleteCredentialByName(string credentialName)
        {
            _logger.LogInformation("Attempting to delete credential: {credentialName}", credentialName);

            if (string.IsNullOrWhiteSpace(credentialName))
            {
                _logger.LogWarning("DeletecredentialByName called with empty credential name");
                return Result.Fail("credential name cannot be empty.");
            }

            try
            {
                string wholePath = GetWholeCredentialPath(credentialName);

                if (!File.Exists(wholePath))
                {
                    _logger.LogWarning("Cannot delete - credential file not found: {credentialName}", credentialName);
                    return Result.Fail($"credential file '{credentialName}' not found.");
                }

                File.Delete(wholePath);
                _logger.LogInformation("Successfully deleted credential: {credentialName}", credentialName);
                return Result.Ok($"credential file '{credentialName}' deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting credential file: {credentialName}", credentialName);
                return Result.Fail($"Error deleting credential file: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a new credential file with the provided content.
        /// </summary>
        public async Task<Result<string>> CreateNewCredentialAsync(string credentialName, string credentialContent)
        {
            _logger.LogInformation("Creating new credential file: {credentialName}", credentialName);

            if (string.IsNullOrWhiteSpace(credentialName))
            {
                _logger.LogWarning("CreateNewcredentialAsync called with empty credential name");
                return Result.Fail("credential name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(credentialContent))
            {
                _logger.LogWarning("CreateNewcredentialAsync called with empty content for {credentialName}", credentialName);
                return Result.Fail("credential content cannot be empty.");
            }

            try
            {
                string wholePath = GetWholeCredentialPath(credentialName);

                // Ensure directory exists
                string directory = Path.GetDirectoryName(wholePath)!;
                if (!Directory.Exists(directory))
                {
                    _logger.LogDebug("Creating credential directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                // Check if file already exists
                if (File.Exists(wholePath))
                {
                    _logger.LogWarning("Cannot create credential - file already exists: {credentialName}", credentialName);
                    return Result.Fail($"credential file '{credentialName}' already exists.");
                }

                await File.WriteAllTextAsync(wholePath, credentialContent);
                _logger.LogInformation("Successfully created credential: {credentialName} ({Size} bytes)", credentialName, credentialContent.Length);
                return Result.Ok($"credential file '{credentialName}' created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating credential file: {credentialName}", credentialName);
                return Result.Fail($"Error creating credential file: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the content of an existing credential file.
        /// </summary>
        public async Task<Result<string>> SetCredentialContentAsync(string credentialName, string credentialContent)
        {
            _logger.LogInformation("Updating credential file: {credentialName}", credentialName);

            if (string.IsNullOrWhiteSpace(credentialName))
            {
                _logger.LogWarning("SetCredentialContentAsync called with empty credential name");
                return Result.Fail("credential name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(credentialContent))
            {
                _logger.LogWarning("SetSredentialContentAsync called with empty content for {credentialName}", credentialName);
                return Result.Fail("credential content cannot be empty.");
            }

            try
            {
                string wholePath = GetWholeCredentialPath(credentialName);

                if (!File.Exists(wholePath))
                {
                    _logger.LogWarning("Cannot update - credential file not found: {credentialName}", credentialName);
                    return Result.Fail($"credential file '{credentialName}' not found.");
                }

                await File.WriteAllTextAsync(wholePath, credentialContent);
                _logger.LogInformation("Successfully updated credential: {credentialName} ({Size} bytes)", credentialName, credentialContent.Length);
                return Result.Ok($"credential file '{credentialName}' updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating credential file: {credentialName}", credentialName);
                return Result.Fail($"Error updating credential file: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the full file path for a credential file.
        /// </summary>
        public string GetWholeCredentialPath(string credentialName)
        {
            return Path.Combine(credentialPath, credentialName);
        }
    }
}
