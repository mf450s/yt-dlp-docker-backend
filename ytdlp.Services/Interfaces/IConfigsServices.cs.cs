using FluentResults;

namespace ytdlp.Services.Interfaces;

public interface IConfigsServices
{
    string GetWholeConfigPath(string configName);
    List<string> GetAllConfigNames();
    Result<string> GetConfigContentByName(string name);
    Result<string> DeleteConfigByName(string name);
    Task<Result<string>> CreateNewConfigAsync(string name, string configContent);
    Task<Result<string>> SetConfigContentAsync(string name, string configContent);
}
