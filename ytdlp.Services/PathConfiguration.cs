namespace ytdlp.Configs;
public class PathConfiguration
{
    // must be without closing / 
    public string Downloads { get; set; } = "/app/downloads";
    public string Archive { get; set; } = "/app/archive";
    public string Config { get; set; } = "/app/configs";
}
