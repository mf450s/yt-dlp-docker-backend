using ytdlp.Services.Interfaces;
using System.IO.Abstractions;
using FluentResults;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ytdlp.Configs;
using ytdlp.Services.Logging;
using System.Text;

namespace ytdlp.Services;

public class ConfigsServices(
    IFileSystem fileSystem,
    IOptions<PathConfiguration> paths,
    IPathParserService pathParserService,
    ILogger<ConfigsServices> logger
    ) : IConfigsServices
{
    private readonly string configFolder = paths.Value.Config.ToString();
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly IPathParserService pathParser = pathParserService;
    private readonly ILogger<ConfigsServices> _logger = logger;

    /// <summary>
    /// gets absolute path to a configfile
    /// </summary>
    /// <param name="configName">name of the configfile</param>
    /// <returns>complete path: "{configFolder}{configName}.conf"</returns>
    public string GetWholeConfigPath(string configName)
    {
        string path = $"{configFolder}{configName}.conf";
        _logger.LogConfigPathResolved(configName, path);
        return path;
    }

    /// <summary>
    /// Gets all config names in the configFolder
    /// </summary>
    /// <returns>List of names of configfiles</returns>
    public List<string> GetAllConfigNames()
    {
        _logger.LogDebug("📄 Retrieving all config names from: {ConfigFolder}", configFolder);
        
        try
        {
            var files = _fileSystem.Directory.GetFiles(configFolder, "*.conf");
            var configNames = new List<string>();

            foreach (var file in files)
            {
                string fileName = _fileSystem.Path.GetFileName(file);
                string nameWithoutExtension = _fileSystem.Path.GetFileNameWithoutExtension(fileName);
                configNames.Add(nameWithoutExtension);
            }

            _logger.LogConfigsCount(configNames.Count);
            return configNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving config names from {ConfigFolder}", configFolder);
            return [];
        }
    }

    /// <summary>
    /// gets one configfile by name
    /// </summary>
    /// <param name="name"></param>
    /// <returns>content of the file</returns>
    public Result<string> GetConfigContentByName(string name)
    {
        _logger.LogDebug("Retrieving config content for: {ConfigName}", name);
        string path = GetWholeConfigPath(name);
        
        if (_fileSystem.File.Exists(path))
        {
            try
            {
                using var reader = _fileSystem.File.OpenText(path);
                string content = reader.ReadToEnd();
                _logger.LogConfigRetrieved(name, content.Length);
                return Result.Ok(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading config file: {ConfigName} at {Path}", name, path);
                return Result.Fail($"Error reading config file: {ex.Message}");
            }
        }
        else
        {
            _logger.LogConfigNotFound(name);
            return Result.Fail($"Config file not found: {path}");
        }
    }

    public Result<string> DeleteConfigByName(string name)
    {
        _logger.LogInformation("Attempting to delete config: {ConfigName}", name);
        string path = GetWholeConfigPath(name);
        
        if (_fileSystem.File.Exists(path))
        {
            try
            {
                _fileSystem.File.Delete(path);
                _logger.LogConfigDeleted(name);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting config file: {ConfigName} at {Path}", name, path);
                return Result.Fail($"Error deleting config file: {ex.Message}");
            }
        }
        else
        {
            _logger.LogConfigNotFound(name);
            return Result.Fail("File does not exist");
        }
    }

    /// <summary>
    /// creates a new configfile and checks, whether a file with that name already exists
    /// </summary>
    /// <param name="name"></param>
    /// <param name="configContent"></param>
    /// <returns>Result with success/failure message</returns>
    public async Task<Result<string>> CreateNewConfigAsync(string name, string configContent)
    {
        _logger.LogInformation("Creating new config: {ConfigName}", name);
        string newPath = GetWholeConfigPath(name);
        
        if (_fileSystem.File.Exists(newPath))
        {
            _logger.LogWarning("Cannot create config - file already exists: {ConfigName}", name);
            return Result.Fail($"File with name '{name}' already exists");
        }
        else
        {
            try
            {
                await WriteContentToFile(newPath, configContent);
                _logger.LogConfigCreated(name, configContent.Length);
                return Result.Ok($"Config file '{name}' created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating config file: {ConfigName}", name);
                return Result.Fail($"Error creating config file: {ex.Message}");
            }
        }
    }

    public async Task<Result<string>> SetConfigContentAsync(string name, string configContent)
    {
        _logger.LogInformation("🔄 Updating config: {ConfigName}", name);
        string path = GetWholeConfigPath(name);
        
        if (_fileSystem.File.Exists(path))
        {
            try
            {
                await WriteContentToFile(path, configContent);
                _logger.LogConfigUpdated(name, configContent.Length);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating config file: {ConfigName}", name);
                return Result.Fail($"Error updating config file: {ex.Message}");
            }
        }
        else
        {
            _logger.LogConfigNotFound(name);
            return Result.Fail($"File with name '{name}' doesn't exist");
        }
    }

    /// <summary>
    /// writes content to specified path using filesystemwriter
    /// </summary>
    /// <param name="path">path to write to</param>
    /// <param name="configContent">content written to path</param>
    /// <returns></returns>
    internal async Task WriteContentToFile(string path, string configContent)
    {
        await using var writer = _fileSystem.File.CreateText(path);
        await writer.WriteAsync(configContent);
    }

    /// <summary>
    /// trims, splits config Content
    /// </summary>
    /// <param name="content"></param>
    /// <returns>string of arguments with fixed paths</returns>
    internal string FixConfigContent(string content)
    {
        var lines = content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var returnList = new List<string>();

        foreach (var line in lines)
        {
            string trimmed = line.Trim();

            // Keep comments and empty lines
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
            {
                returnList.Add(line);
                continue;
            }

            var args = SplitArguments(trimmed);

            foreach (var arg in args)
            {
                string fixedArg = pathParser.CheckAndFixPaths(arg);
                returnList.Add(fixedArg);
            }
        }
        return string.Join(Environment.NewLine, returnList);
    }

    /// <summary>
    /// Split arguments.
    /// </summary>
    /// <param name="line">one line that may contain multiple args</param>
    /// <returns>List of strings containing one argument each</returns>
    internal static List<string> SplitArguments(string line)
    {
        var args = new List<string>();
        var currentArg = new StringBuilder();
        bool inQuotes = false;
        bool inOption = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"' || c == '\'')
            {
                inQuotes = !inQuotes;
                currentArg.Append(c);
            }
            else if (c == ' ' && !inQuotes)
            {
                if (currentArg.Length > 0)
                {
                    string arg = currentArg.ToString().Trim();

                    if (inOption && i + 1 < line.Length && !line[i + 1].ToString().StartsWith("-"))
                    {
                        // Next token belongs to this option
                        currentArg.Append(c);
                        inOption = false;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(arg))
                        {
                            args.Add(arg);
                        }
                        currentArg.Clear();

                        if (i + 1 < line.Length && line[i + 1] == '-')
                        {
                            inOption = true;
                        }
                    }
                }
            }
            else
            {
                currentArg.Append(c);

                if (c == '-' && currentArg.Length <= 2)
                {
                    inOption = true;
                }
            }
        }

        if (currentArg.Length > 0)
        {
            string arg = currentArg.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(arg))
            {
                args.Add(arg);
            }
        }

        return args;
    }
}
