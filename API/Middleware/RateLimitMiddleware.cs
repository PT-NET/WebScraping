using WebScraping.Domain.Interfaces;

namespace WebScraping.API.Middleware
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitMiddleware> _logger;

        public RateLimitMiddleware(
            RequestDelegate next,
            ILogger<RateLimitMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IRateLimitService rateLimitService)
        {
            if (!context.Request.Path.StartsWithSegments("/api/screening/screen"))
            {
                await _next(context);
                return;
            }

            var clientId = GetClientIdentifier(context);

            _logger.LogDebug("Checking rate limit for client: {ClientId}", clientId);

            await rateLimitService.IsAllowedAsync(clientId);

            await _next(context);
        }

        private static string GetClientIdentifier(HttpContext context)
        {
            var userId = context.User?.Claims
                .FirstOrDefault(c => c.Type == "sub")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"client:{ip}";
        }
    }
}
