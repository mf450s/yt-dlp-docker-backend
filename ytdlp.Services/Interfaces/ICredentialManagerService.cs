using FluentResults;

namespace ytdlp.Services.Interfaces;

public interface ICredentialService
{
    /// <summary>
    /// Retrieves all available cookie file names.
    /// </summary>
    /// <returns>A list of cookie file names.</returns>
    List<string> GetAllCredentialNames();

    /// <summary>
    /// Retrieves the content of a specific cookie file by name.
    /// </summary>
    /// <param name="cookieName">The name of the cookie file.</param>
    /// <returns>A result containing the cookie file content or an error.</returns>
    Result<string> GetCredentialContentByName(string cookieName);

    /// <summary>
    /// Deletes a cookie file by name.
    /// </summary>
    /// <param name="cookieName">The name of the cookie file to delete.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result<string> DeleteCredentialByName(string cookieName);

    /// <summary>
    /// Creates a new cookie file with the provided content.
    /// </summary>
    /// <param name="cookieName">The name of the cookie file to create.</param>
    /// <param name="cookieContent">The content of the cookie file (Netscape format or JSON).</param>
    /// <returns>A task containing a result indicating success or failure.</returns>
    Task<Result<string>> CreateNewCredentialAsync(string cookieName, string cookieContent);

    /// <summary>
    /// Updates the content of an existing cookie file.
    /// </summary>
    /// <param name="cookieName">The name of the cookie file.</param>
    /// <param name="cookieContent">The new content for the cookie file.</param>
    /// <returns>A task containing a result indicating success or failure.</returns>
    Task<Result<string>> SetCredentialContentAsync(string cookieName, string cookieContent);

    /// <summary>
    /// Retrieves the full file path for a cookie file.
    /// </summary>
    /// <param name="cookieName">The name of the cookie file.</param>
    /// <returns>The full file path of the cookie file.</returns>
    string GetWholeCredentialPath(string cookieName);
}
