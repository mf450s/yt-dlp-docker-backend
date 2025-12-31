using System.Diagnostics;

namespace ytdlp.Api.Middleware
{
    /// <summary>
    /// Middleware for structured logging of HTTP requests and responses.
    /// Logs request details, response status, and execution time.
    /// </summary>
    public class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<LoggingMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.TraceIdentifier;
            var stopwatch = Stopwatch.StartNew();

            var request = context.Request;
            var requestBody = await ReadRequestBodyAsync(request);

            _logger.LogInformation(
                "[{CorrelationId}] HTTP {Method} {Path}{QueryString} | " +
                "RemoteIP: {RemoteIP} | ContentType: {ContentType}",
                correlationId,
                request.Method,
                request.Path,
                request.QueryString,
                context.Connection.RemoteIpAddress,
                request.ContentType ?? "none");

            // Replace response stream to capture response
            var originalBody = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                stopwatch.Stop();

                _logger.LogInformation(
                    "[{CorrelationId}] HTTP {Method} {Path} => {StatusCode} | " +
                    "Duration: {ElapsedMilliseconds}ms | ContentLength: {ContentLength}",
                    correlationId,
                    request.Method,
                    request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    context.Response.ContentLength ?? 0);

                responseBody.Position = 0;
                await responseBody.CopyToAsync(originalBody);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "[{CorrelationId}] HTTP {Method} {Path} => Exception after {ElapsedMilliseconds}ms",
                    correlationId,
                    request.Method,
                    request.Path,
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }

        private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }
    }
}
