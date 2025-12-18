using ytdlp.Services.Interfaces;
namespace ytdlp.Services;

public class ConfigsServices :IConfigsServices
{
    public string GetWholeConfigPath(string configName)
    {
        return $"../configs/{configName}.conf";
    }
    public List<string> GetAllConfigNames()
    {
        string path = "../configs/";
        var files = Directory.GetFiles(path, "*.conf");
        var configNames = new List<string>();

        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            configNames.Add(nameWithoutExtension);
        }

        return configNames;
    }
}
