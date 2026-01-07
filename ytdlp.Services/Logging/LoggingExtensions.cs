using Microsoft.Extensions.Logging;

namespace ytdlp.Services.Logging
{
    /// <summary>
    /// Extension methods for structured logging across services.
    /// </summary>
    public static class LoggingExtensions
    {
        // ==================== Download Service Logging ====================
        public static void LogDownloadStarted(this ILogger logger, string url, string configFile)
        {
            logger.LogInformation(
                "⬇️ Download started | URL: {Url} | Config: {ConfigFile}",
                url, configFile);
        }

        public static void LogDownloadCompleted(this ILogger logger, string url, TimeSpan duration)
        {
            logger.LogInformation(
                "✅ Download completed | URL: {Url} | Duration: {DurationMs}ms",
                url, (long)duration.TotalMilliseconds);
        }

        public static void LogDownloadFailed(this ILogger logger, string url, int exitCode, string error)
        {
            logger.LogError(
                "❌ Download failed | URL: {Url} | ExitCode: {ExitCode} | Error: {Error}",
                url, exitCode, error);
        }

        public static void LogProcessStarted(this ILogger logger, string processName, string arguments)
        {
            logger.LogDebug(
                "🔧 Process started | Name: {ProcessName} | Args: {Arguments}",
                processName, arguments);
        }

        public static void LogConfigPathResolved(this ILogger logger, string configName, string fullPath)
        {
            logger.LogDebug(
                "📄 Config path resolved | Name: {ConfigName} | Path: {Path}",
                configName, fullPath);
        }

        // ==================== Config Service Logging ====================
        public static void LogConfigRetrieved(this ILogger logger, string configName, int sizeBytes)
        {
            logger.LogInformation(
                "📖 Config retrieved | Name: {ConfigName} | Size: {SizeBytes} bytes",
                configName, sizeBytes);
        }

        public static void LogConfigCreated(this ILogger logger, string configName, int sizeBytes)
        {
            logger.LogInformation(
                "✨ Config created | Name: {ConfigName} | Size: {SizeBytes} bytes",
                configName, sizeBytes);
        }

        public static void LogConfigUpdated(this ILogger logger, string configName, int sizeBytes)
        {
            logger.LogInformation(
                "🔄 Config updated | Name: {ConfigName} | Size: {SizeBytes} bytes",
                configName, sizeBytes);
        }

        public static void LogConfigDeleted(this ILogger logger, string configName)
        {
            logger.LogInformation(
                "🗑️ Config deleted | Name: {ConfigName}",
                configName);
        }

        public static void LogConfigNotFound(this ILogger logger, string configName)
        {
            logger.LogWarning(
                "⚠️ Config not found | Name: {ConfigName}",
                configName);
        }

        public static void LogConfigsCount(this ILogger logger, int count)
        {
            logger.LogInformation(
                "📊 Config count | Total: {Count}",
                count);
        }

        // ==================== Cookies Service Logging ====================
        public static void LogCookiesFileProcessed(this ILogger logger, string fileName, int size)
        {
            logger.LogInformation(
                "🍪 Cookies file processed | File: {FileName} | Size: {Size} bytes",
                fileName, size);
        }

        public static void LogCookiesValidationStarted(this ILogger logger, string fileName)
        {
            logger.LogDebug(
                "🔐 Cookies validation started | File: {FileName}",
                fileName);
        }

        public static void LogCookiesValidationCompleted(this ILogger logger, string fileName, bool isValid)
        {
            logger.LogInformation(
                "🔐 Cookies validation completed | File: {FileName} | Valid: {IsValid}",
                fileName, isValid);
        }

        // ==================== Path Parser Logging ====================
        public static void LogPathFixed(this ILogger logger, string originalPath, string fixedPath)
        {
            logger.LogDebug(
                "🔗 Path fixed | Original: {OriginalPath} | Fixed: {FixedPath}",
                originalPath, fixedPath);
        }

        // ==================== General Performance Logging ====================
        public static void LogOperationDuration(this ILogger logger, string operationName, TimeSpan duration)
        {
            logger.LogInformation(
                "⏱️ Operation duration | Name: {OperationName} | Duration: {DurationMs}ms",
                operationName, (long)duration.TotalMilliseconds);
        }

        public static void LogMemoryUsage(this ILogger logger, long bytesUsed)
        {
            var megabytes = bytesUsed / (1024.0 * 1024.0);
            logger.LogDebug(
                "💾 Memory usage | Size: {MemoryMb:F2} MB",
                megabytes);
        }

        // ==================== Health Check Logging ====================
        public static void LogHealthCheckStarted(this ILogger logger)
        {
            logger.LogDebug("❤️ Health check started");
        }

        public static void LogHealthCheckCompleted(this ILogger logger, bool isHealthy, string? reason = null)
        {
            if (isHealthy)
            {
                logger.LogInformation("✅ Health check passed");
            }
            else
            {
                logger.LogWarning(
                    "⚠️ Health check failed | Reason: {Reason}",
                    reason ?? "Unknown");
            }
        }
    }
}
