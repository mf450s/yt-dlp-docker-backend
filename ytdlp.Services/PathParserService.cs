using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ytdlp.Configs;
using ytdlp.Services.Interfaces;

namespace ytdlp.Services;

public class PathParserService(
    IOptions<PathConfiguration> paths,
    ILogger<PathParserService> logger
    ) : IPathParserService
{
    private readonly string _downloadFolder = paths.Value.Downloads;
    private readonly string _archiveFolder = paths.Value.Archive;
    private readonly string _cookiesFolder = paths.Value.Cookies;
    private readonly ILogger<PathParserService> _logger = logger;

    /// <summary>
    /// prepends complete paths to yt-dlp paths/download/archive/cookies options.
    /// Gets paths from <PathConfiguration>
    /// </summary>
    /// <param name="line"></param>
    /// <returns>line with correct path</returns>
    public string CheckAndFixPaths(string line)
    {
        string trimmed = line.Trim();

        // Check for -o or --output
        if (trimmed.StartsWith("-o ") || trimmed.StartsWith("--output "))
        {
            _logger.LogDebug("Fixing output path: {Line}", trimmed);
            string fixedPath = FixPath(trimmed, _downloadFolder);
            _logger.LogDebug("Fixed output path: {FixedPath}", fixedPath);
            return fixedPath;
        }

        // Check for --download-archive
        if (trimmed.StartsWith("--download-archive"))
        {
            _logger.LogDebug("Fixing archive path: {Line}", trimmed);
            string fixedPath = FixPath(trimmed, _archiveFolder);
            _logger.LogDebug("Fixed archive path: {FixedPath}", fixedPath);
            return fixedPath;
        }

        // Check for --cookies
        if (trimmed.StartsWith("--cookies") && !trimmed.StartsWith("--cookies-"))
        {
            _logger.LogDebug("Fixing cookies path: {Line}", trimmed);
            string fixedPath = FixPath(trimmed, _cookiesFolder + "/");
            _logger.LogDebug("Fixed cookies path: {FixedPath}", fixedPath);
            return fixedPath;
        }

        return line;
    }

    /// <summary>
    /// Fixes the path, by prepending the folder to the given path in the line
    /// </summary>
    /// <param name="line">complete line</param>
    /// <param name="folder">folder for the complete path</param>
    /// <returns>fixed line with complete absolute path</returns>
    internal string FixPath(string line, string folder)
    {
        string[] parts = line.Split([' '], 2);

        if (parts.Length != 2)
        {
            _logger.LogWarning("Invalid path format, expected 2 parts: {Line}", line);
            return line;
        }

        string template = parts[1].Trim();

        // Remove quotes if present
        if (template.StartsWith("\"") && template.EndsWith("\""))
        {
            template = template.Substring(1, template.Length - 2);
            template = template.Trim();
        }

        // Add folder if not already present
        if (!template.Contains(folder))
        {
            // Remove leading "/" to avoid "//"
            if (template.StartsWith("/"))
                template = template[1..];
            template = $"{folder}{template}";
            _logger.LogDebug("Prepended folder {Folder} to path: {Template}", folder, template);
        }
        
        return $"{parts[0]} \"{template}\"";
    }
}
