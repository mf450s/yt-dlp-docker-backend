// using ytdlp.Services.Interfaces;
// using Microsoft.Extensions.Logging;
// using FluentResults;

// namespace ytdlp.Services;

// /// <summary>
// /// Startup service that automatically validates and fixes existing config files
// /// Uses dependency injection to access ConfigsServices and PathParserService
// /// </summary>
// public class StartupConfigFixer : IStartupConfigFixer
// {
//     private readonly IConfigsServices _configsServices;
//     private readonly ILogger<StartupConfigFixer> _logger;

//     public StartupConfigFixer(
//         IConfigsServices configsServices,
//         ILogger<StartupConfigFixer> logger)
//     {
//         _configsServices = configsServices ?? throw new ArgumentNullException(nameof(configsServices));
//         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//     }

//     /// <summary>
//     /// Fixes all existing config files by validating and correcting paths
//     /// </summary>
//     /// <returns>Result containing statistics about fixed configs</returns>
//     public async Task<StartupFixerResult> FixAllConfigsAsync()
//     {
//         var result = new StartupFixerResult();

//         try
//         {
//             _logger.LogInformation("[StartupConfigFixer] Starting config validation and fixing process");

//             // Get all config names
//             var configNames = _configsServices.GetAllConfigNames();
//             result.TotalConfigsProcessed = configNames.Count;

//             if (configNames.Count == 0)
//             {
//                 _logger.LogInformation("[StartupConfigFixer] No config files found to process");
//                 return result;
//             }

//             _logger.LogInformation("[StartupConfigFixer] Found {ConfigCount} config files to process", configNames.Count);

//             // Process each config
//             foreach (var configName in configNames)
//             {
//                 await ProcessConfigAsync(configName, result);
//             }

//             // Log summary
//             LogSummary(result);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "[StartupConfigFixer] Unexpected error during config fixing process");
//             result.ErroredConfigs.Add(("[GLOBAL]", ex.Message));
//         }

//         return result;
//     }

//     /// <summary>
//     /// Processes a single config file: reads, fixes, and saves it
//     /// </summary>
//     private async Task ProcessConfigAsync(string configName, StartupFixerResult result)
//     {
//         try
//         {
//             _logger.LogDebug("[StartupConfigFixer] Processing config: {ConfigName}", configName);

//             // Get current config content
//             var getResult = _configsServices.GetConfigContentByName(configName);
//             if (getResult.IsFailed)
//             {
//                 _logger.LogWarning("[StartupConfigFixer] Failed to read config {ConfigName}: {Error}",
//                     configName, string.Join(", ", getResult.Errors));
//                 result.ErroredConfigs.Add((configName, string.Join(", ", getResult.Errors)));
//                 result.ConfigsWithErrors++;
//                 return;
//             }

//             var originalContent = getResult.Value;

//             // Fix the content using the internal method from ConfigsServices
//             var fixedContent = FixConfigContent(originalContent);

//             // Check if anything changed
//             if (originalContent == fixedContent)
//             {
//                 _logger.LogDebug("[StartupConfigFixer] Config {ConfigName} is already valid, no changes needed", configName);
//                 result.FixedConfigs.Add(configName); // Count as fixed (no issues found)
//                 return;
//             }

//             // Save the fixed content
//             var setResult = await _configsServices.SetConfigContentAsync(configName, fixedContent);
//             if (setResult.IsFailed)
//             {
//                 _logger.LogError("[StartupConfigFixer] Failed to save fixed config {ConfigName}: {Error}",
//                     configName, string.Join(", ", setResult.Errors));
//                 result.ErroredConfigs.Add((configName, string.Join(", ", setResult.Errors)));
//                 result.ConfigsWithErrors++;
//                 return;
//             }

//             _logger.LogInformation("[StartupConfigFixer] Successfully fixed config: {ConfigName}", configName);
//             result.FixedConfigs.Add(configName);
//             result.ConfigsFixed++;
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "[StartupConfigFixer] Exception processing config {ConfigName}", configName);
//             result.ErroredConfigs.Add((configName, ex.Message));
//             result.ConfigsWithErrors++;
//         }
//     }

//     /// <summary>
//     /// Fixes config content by validating and correcting paths
//     /// (Mirror of ConfigsServices.FixConfigContent - kept in sync)
//     /// </summary>
//     private static string FixConfigContent(string content)
//     {
//         var lines = content.Split(['
// ', '
// '], StringSplitOptions.RemoveEmptyEntries);
//         var returnList = new List<string>();

//         foreach (var line in lines)
//         {
//             string trimmed = line.Trim();

//             // Keep comments and empty lines
//             if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
//             {
//                 returnList.Add(line);
//                 continue;
//             }

//             var args = ConfigsServices.SplitArguments(trimmed);

//             foreach (var arg in args)
//             {
//                 returnList.Add(arg);
//             }
//         }

//         return string.Join(Environment.NewLine, returnList);
//     }

//     /// <summary>
//     /// Logs summary of the fixing process
//     /// </summary>
//     private void LogSummary(StartupFixerResult result)
//     {
//         _logger.LogInformation(
//             "[StartupConfigFixer] Summary - Total: {Total}, Fixed: {Fixed}, Errors: {Errors}",
//             result.TotalConfigsProcessed,
//             result.ConfigsFixed,
//             result.ConfigsWithErrors);

//         if (result.ErroredConfigs.Any())
//         {
//             _logger.LogWarning("[StartupConfigFixer] Configs with errors: {ErroredConfigs}",
//                 string.Join(", ", result.ErroredConfigs.Select(e => e.ConfigName)));
//         }
//     }
// }
