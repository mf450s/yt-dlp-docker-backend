using Microsoft.AspNetCore.Mvc;
using System.Text;
using ytdlp.Services.Interfaces;
using FluentResults;

namespace ytdlp.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CredentialManagementController(
        ICredentialService credentialManagerService,
        ILogger<CredentialManagementController> logger
        ) : ControllerBase
    {
        private readonly ILogger<CredentialManagementController> _logger = logger;

        /// <summary>
        /// Retrieves all available credential file names.
        /// </summary>
        [HttpGet]
        public List<string> GetAllCredentialNames()
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogDebug(
                "[{CorrelationId}] GetAllCredentialNames request received",
                correlationId);

            var credentials = credentialManagerService.GetAllCredentialNames();
            _logger.LogDebug(
                "[{CorrelationId}] Returning {Count} credential files",
                correlationId, credentials.Count);

            return credentials;
        }

        /// <summary>
        /// Retrieves the content of a specific credential file by name.
        /// </summary>
        /// <param name="credentialName">The name of the credential file.</param>
        [HttpGet("{credentialName}")]
        public IActionResult GetCredentialContentByName(string credentialName)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogDebug(
                "[{CorrelationId}] GetCredentialContentByName request | Credential: {credentialName}",
                correlationId, credentialName);

            Result<string> credentialContent = credentialManagerService.GetCredentialContentByName(credentialName);
            if (credentialContent.IsFailed)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Credential not found | Credential: {credentialName}",
                    correlationId, credentialName);
                return NotFound(new { error = credentialContent.Errors[0].Message, correlationId });
            }

            _logger.LogDebug(
                "[{CorrelationId}] Returning credential content | Credential: {credentialName} | Size: {Size} bytes",
                correlationId, credentialName, credentialContent.Value.Length);

            return Ok(credentialContent.Value);
        }

        /// <summary>
        /// Deletes a Credential file by name.
        /// </summary>
        /// <param name="credentialName">The name of the Credential file to delete.</param>
        [HttpDelete("{credentialName}")]
        public IActionResult DeleteCredentialByName(string credentialName)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "[{CorrelationId}] 🗑️ DeleteCredentialByName request | Credential: {credentialName}",
                correlationId, credentialName);

            Result<string> result = credentialManagerService.DeleteCredentialByName(credentialName);
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[{CorrelationId}] ✅ Credential deleted successfully | Credential: {credentialName}",
                    correlationId, credentialName);
                return NoContent();
            }
            else
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Failed to delete Credential | Credential: {CredentialName}",
                    correlationId, credentialName);
                return NotFound(new { error = result.Errors[0].Message, correlationId });
            }
        }

        /// <summary>
        /// Creates a new Credential file with the provided content.
        /// Supports Netscape format and JSON-based Credential files.
        /// </summary>
        /// <param name="credentialName">The name of the Credential file to create.</param>
        [HttpPost("{credentialName}")]
        public async Task<IActionResult> CreateNewCredentialAsync(string credentialName)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "[{CorrelationId}] 🍪 CreateNewCredential request | Credential: {CredentialName}",
                correlationId, credentialName);

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string credentialContent = await reader.ReadToEndAsync();

            _logger.LogDebug(
                "[{CorrelationId}] 🍪 Creating Credential {CredentialName} with {Size} bytes",
                correlationId, credentialName, credentialContent.Length);

            Result<string> result = await credentialManagerService.CreateNewCredentialAsync(credentialName, credentialContent);
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[{CorrelationId}] ✅ Credential created successfully | Credential: {CredentialName}",
                    correlationId, credentialName);
                return Created(credentialName, new { name = credentialName, message = result.Value, correlationId });
            }
            else
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Failed to create Credential | Credential: {CredentialName} | Error: {Error}",
                    correlationId, credentialName, result.Value);
                return Conflict(new { error = result.Value, correlationId });
            }
        }

        /// <summary>
        /// Updates the content of an existing Credential file.
        /// </summary>
        /// <param name="credentialName">The name of the Credential file.</param>
        [HttpPatch("{credentialName}")]
        public async Task<IActionResult> SetCredentialContentAsync(string credentialName)
        {
            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "[{CorrelationId}] 🍪 SetCredentialContent request | Credential: {CredentialName}",
                correlationId, credentialName);

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string credentialContent = await reader.ReadToEndAsync();

            _logger.LogDebug(
                "[{CorrelationId}] 🍪 Updating Credential {CredentialName} with {Size} bytes",
                correlationId, credentialName, credentialContent.Length);

            Result<string> result = await credentialManagerService.SetCredentialContentAsync(credentialName, credentialContent);
            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[{CorrelationId}] ✅ Credential updated successfully | Credential: {CredentialName}",
                    correlationId, credentialName);
                return Ok(new { name = credentialName, message = result.Value, correlationId });
            }
            else
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Failed to update Credential | Credential: {CredentialName}",
                    correlationId, credentialName);
                return NotFound(new { error = result.Errors[0].Message, correlationId });
            }
        }
    }
}
