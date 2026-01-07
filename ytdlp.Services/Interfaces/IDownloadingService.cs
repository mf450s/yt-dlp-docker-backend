namespace ytdlp.Services.Interfaces;

public interface IDownloadingService
{
    /// <summary>
    /// Attempts to download content from a URL using the specified configuration.
    /// Cookie files should be specified within the config file using --cookies option.
    /// </summary>
    /// <param name="url">The URL to download from.</param>
    /// <param name="configFile">The name of the configuration file to use.</param>
    /// <returns>A task representing the asynchronous download operation.</returns>
    Task TryDownloadingFromURL(string url, string configFile);
}
