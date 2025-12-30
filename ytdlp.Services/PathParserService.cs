using Microsoft.Extensions.Options;
using ytdlp.Configs;
using ytdlp.Services.Interfaces;

namespace ytdlp.Services;

public class PathParserService(IOptions<PathConfiguration> paths) : IPathParserService
{
    private readonly string _downloadFolder = paths.Value.Downloads;
    private readonly string _archiveFolder = paths.Value.Archive;
    private readonly string _cookiesFolder = paths.Value.Cookies;

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
            return FixPath(trimmed, _downloadFolder);
        }

        // Check for --download-archive
        if (trimmed.StartsWith("--download-archive"))
        {
            return FixPath(trimmed, _archiveFolder);
        }

        // Check for --cookies
        if (trimmed.StartsWith("--cookies") && !trimmed.StartsWith("--cookies-"))
        {
            return FixPath(trimmed, _cookiesFolder + "/");
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
            return line;

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
        }
        return $"{parts[0]} \"{template}\"";
    }
}
