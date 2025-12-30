namespace ytdlp.Services.Interfaces;

/// <summary>
/// Handles automatic fixing of existing config files on application startup
/// </summary>
public interface IStartupConfigFixer
{
    /// <summary>
    /// Fixes all existing config files by validating and correcting paths
    /// </summary>
    Task<StartupFixerResult> FixAllConfigsAsync();
}

/// <summary>
/// Result of the startup config fixing process
/// </summary>
public class StartupFixerResult
{
    public int TotalConfigsProcessed { get; set; }
    public int ConfigsFixed { get; set; }
    public int ConfigsWithErrors { get; set; }
    public List<string> FixedConfigs { get; set; } = new();
    public List<(string ConfigName, string Error)> ErroredConfigs { get; set; } = new();
    public bool AllSuccess => ConfigsWithErrors == 0;
}
