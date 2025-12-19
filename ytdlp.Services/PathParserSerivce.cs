using System;
using System.IO.Abstractions;
using FluentResults;
using Microsoft.Extensions.Options;
using ytdlp.Configs;
using ytdlp.Services.Interfaces;
using System.Text;

namespace ytdlp.Services;

public class PathParserSerivce(IOptions<PathConfiguration> paths) : IPathParserService
{
    private readonly string configFolder = paths.Value.Config;
    private readonly string downloadFolder = paths.Value.Downloads;
    private readonly string archiveFolder = paths.Value.Archive;
    public string CheckAndFixPaths(string line)
    {
        string trimmed = line.Trim();

        // Check for -o or --output
        if (trimmed.StartsWith("-o ") || trimmed.StartsWith("--output "))
        {
            return FixOutputPath(trimmed);
        }

        // Check for -P or --paths
        if (trimmed.StartsWith("-P ") || trimmed.StartsWith("--paths "))
        {
            return FixPathPath(trimmed);
        }

        if (trimmed.StartsWith("--download-archive"))
        {
            return FixArchivePath(trimmed);
        }

        // Return unchanged if not an output/path option
        return line;
    }
    internal string FixOutputPath(string line)
    {
        string[] parts = line.Split([' '], 2);

        if (parts.Length != 2)
            return line;

        string template = parts[1].TrimStart();

        // Remove quotes if present
        if (template.StartsWith("\"") && template.EndsWith("\""))
            template = template.Substring(1, template.Length - 2);

        // Add downloadFolder if not already present
        if (!template.Contains(downloadFolder))
        {
            template = Path.Combine(downloadFolder, template);
        }

        return $"{parts[0]} \"{template}\"";
    }

    internal string FixPathPath(string line)
    {
        string[] parts = line.Split([' '], 2);

        if (parts.Length != 2)
            return line;

        string pathValue = parts[1].TrimStart();

        // Remove quotes if present
        if (pathValue.StartsWith("\"") && pathValue.EndsWith("\""))
            pathValue = pathValue.Substring(1, pathValue.Length - 2);

        // Check if downloadFolder is already in path
        if (pathValue.Contains(downloadFolder))
            return line;

        // Handle type:path format (e.g., "home:/downloads")
        if (pathValue.Contains(':'))
        {
            string[] pathParts = pathValue.Split([':'], 2);
            string type = pathParts[0];
            string path = pathParts[1];
            string newArg = $"{parts[0]} \"{type}:{downloadFolder}{path}\"";

            return newArg;
        }
        else
        {
            string newArg = $"{parts[0]} \"{downloadFolder}{pathValue}\"";
            return newArg;
        }
    }

    internal string FixArchivePath(string line)
    {
        string[] parts = line.Split([' '], 2);
        if (parts.Length != 2)
            return line;
        string template = parts[1].TrimStart();
        if (template.StartsWith("\"") && template.EndsWith("\""))
            template = template.Substring(1, template.Length - 2);

        if (!template.Contains(archiveFolder))
        {
            // remove leading "/" to avoid "//"
            if (template.StartsWith("/"))
                template = template[1..];
            template = $"{archiveFolder}{template}";
        }
        return $"{parts[0]} \"{template}\"";
    }
}
