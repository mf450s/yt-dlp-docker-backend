using ytdlp.Services.Interfaces;
using System.IO.Abstractions;
using FluentResults;
using Microsoft.Extensions.Options;
using ytdlp.Configs;
using System.Text;
namespace ytdlp.Services;

public class ConfigsServices(
    IFileSystem fileSystem,
    IOptions<PathConfiguration> paths,
    IPathParserService pathParserService
    ) : IConfigsServices
{
    private readonly string configFolder = paths.Value.Config;
    private readonly string downloadFolder = paths.Value.Downloads;
    private readonly string archiveFolder = paths.Value.Archive;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly IPathParserService pathParser = pathParserService;
    public string GetWholeConfigPath(string configName)
    {
        return $"{configFolder}/{configName}.conf";
    }
    public List<string> GetAllConfigNames()
    {
        var files = _fileSystem.Directory.GetFiles(configFolder, "*.conf");
        var configNames = new List<string>();

        foreach (var file in files)
        {
            string fileName = _fileSystem.Path.GetFileName(file);
            string nameWithoutExtension = _fileSystem.Path.GetFileNameWithoutExtension(fileName);
            configNames.Add(nameWithoutExtension);
        }

        return configNames;
    }
    public Result<string> GetConfigContentByName(string name)
    {
        string path = GetWholeConfigPath(name);
        if (_fileSystem.File.Exists(path))
        {
            using var reader = _fileSystem.File.OpenText(path);
            return Result.Ok(reader.ReadToEnd());
        }
        else
        {
            return Result.Fail($"Config file not found: {path}");
        }
    }
    public Result<string> DeleteConfigByName(string name)
    {
        string path = GetWholeConfigPath(name);
        if (_fileSystem.File.Exists(path))
        {
            _fileSystem.File.Delete(path);
            return Result.Ok();
        }
        else return Result.Fail("File already exists");
    }
    public async Task<Result<string>> CreateNewConfigAsync(string name, string configContent)
    {
        string newPath = GetWholeConfigPath(name);
        if (_fileSystem.File.Exists(newPath))
        {
            return Result.Fail($"File with name '{name}' already exists");
        }
        else
        {
            await WriteContentToFile(newPath, configContent);
            return Result.Ok($"Config file '{name}' created successfully.");
        }
    }

    public async Task<Result<string>> SetConfigContentAsync(string name, string configContent)
    {
        string path = GetWholeConfigPath(name);
        if (_fileSystem.File.Exists(path))
        {
            await WriteContentToFile(path, configContent);
            return Result.Ok();
        }
        else
            return Result.Fail($"File with name '{name}' doesnt exists");

    }
    internal async Task WriteContentToFile(string path, string configContent)
    {
        await using var writer = _fileSystem.File.CreateText(path);
        await writer.WriteAsync(configContent);
    }
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