using Microsoft.AspNetCore.Mvc;
using ytdlp.Services;

namespace ytdlp.Api
{
    /// <summary>
    /// Health check endpoint for monitoring API and service availability
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthCheckController : ControllerBase
    {
        private readonly IHealthCheckService _healthCheckService;
        private readonly ILogger<HealthCheckController> _logger;

        public HealthCheckController(
            IHealthCheckService healthCheckService,
            ILogger<HealthCheckController> logger)
        {
            _healthCheckService = healthCheckService;
            _logger = logger;
        }

        /// <summary>
        /// Get detailed health status of the API
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// 200 OK if healthy, 503 Service Unavailable if unhealthy
        /// Returns JSON with status, timestamp, and diagnostic details
        /// </returns>
        /// <response code="200">API is healthy</response>
        /// <response code="503">API is unhealthy</response>
        [HttpGet("detailed")]
        [ProducesResponseType(typeof(HealthStatus), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthStatus), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetDetailedHealth(CancellationToken cancellationToken = default)
        {
            try
            {
                var health = await _healthCheckService.CheckHealthAsync(cancellationToken);

                _logger.LogInformation(
                    "Health check request completed. Status: {Status}, ResponseTime: {ResponseTime}ms",
                    health.Status,
                    health.Details.ContainsKey("response_time_ms") ? health.Details["response_time_ms"] : "N/A"
                );

                // Return 503 if unhealthy, 200 if healthy
                if (health.Status == "Unhealthy")
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, health);
                }

                return Ok(health);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Health check request was cancelled");
                var failedHealth = new HealthStatus
                {
                    Status = "Unhealthy",
                    Details = new Dictionary<string, object>
                    {
                        { "error", "Health check operation was cancelled" },
                        { "timestamp", DateTime.UtcNow }
                    }
                };
                return StatusCode(StatusCodes.Status503ServiceUnavailable, failedHealth);
            }
        }

        /// <summary>
        /// Simple health check for load balancers and orchestration platforms
        /// </summary>
        /// <returns>200 OK with simple status</returns>
        /// <response code="200">API is running</response>
        [HttpGet("live")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetLiveness()
        {
            _logger.LogDebug("Liveness probe executed");
            return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Health check endpoint compatible with Docker and Kubernetes
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>200 OK if healthy, 503 if unhealthy</returns>
        /// <response code="200">API is healthy and ready to serve</response>
        /// <response code="503">API is not ready to serve</response>
        [HttpGet("ready")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetReadiness(CancellationToken cancellationToken = default)
        {
            try
            {
                var health = await _healthCheckService.CheckHealthAsync(cancellationToken);

                if (health.Status == "Unhealthy")
                {
                    _logger.LogWarning("Readiness probe failed");
                    return StatusCode(StatusCodes.Status503ServiceUnavailable);
                }

                _logger.LogDebug("Readiness probe successful");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Readiness check failed");
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
    }
}
