using FluentResults;

namespace ytdlp.Services.Interfaces;

public interface IConfigsServices
{
    string GetWholeConfigPath(string configName);
    List<string> GetAllConfigNames();
    Result<string> GetConfigContentByName(string name);
    Task DeleteConfigByName(string name);
}
