namespace ytdlp.Services.Interfaces;

public interface IConfigsServices
{
    string GetWholeConfigPath(string configName);
    List<string> GetAllConfigNames();
}
