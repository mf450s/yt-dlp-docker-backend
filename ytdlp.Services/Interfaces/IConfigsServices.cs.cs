using FluentResults;

namespace ytdlp.Services.Interfaces;

public interface IConfigsServices
{
    string GetWholeConfigPath(string configName);
    List<string> GetAllConfigNames();
    Result<string> GetConfigContentByName(string name);
    Result<string> DeleteConfigByName(string name);
    Result<string> CreateNewConfig(string name, string configContent);
}
