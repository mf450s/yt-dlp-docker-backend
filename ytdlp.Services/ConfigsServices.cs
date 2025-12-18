using ytdlp.Services.Interfaces;
using System.IO.Abstractions;
using FluentResults;
namespace ytdlp.Services;

public class ConfigsServices(IFileSystem fileSystem) : IConfigsServices
{
    private readonly string configFolder = "../configs/";
    private readonly IFileSystem _fileSystem = fileSystem;
    public string GetWholeConfigPath(string configName)
    {
        return $"{configFolder}{configName}.conf";
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
    public Task DeleteConfigByName(string name)
    {
        string path = GetWholeConfigPath(name);
        _fileSystem.File.Delete(path);
        return Task.CompletedTask;
    }
}
