namespace ytdlp.Services.Interfaces;

public interface IDownloadingService
{
    Task TryDownloadingFromURL(string url, string configFile);
}
