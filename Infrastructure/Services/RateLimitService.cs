using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WebScraping.Domain.Exceptions;
using WebScraping.Domain.Interfaces;
using WebScraping.Infrastructure.Configuration;

namespace WebScraping.Infrastructure.Services
{
    public class RateLimitService : IRateLimitService
    {
        private readonly IMemoryCache _cache;
        private readonly RateLimitSettings _settings;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public RateLimitService(IMemoryCache cache, IOptions<RateLimitSettings> settings)
        {
            _cache = cache;
            _settings = settings.Value;
        }

        public async Task<bool> IsAllowedAsync(string clientIdentifier, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                var cacheKey = $"ratelimit:{clientIdentifier}";
                var now = DateTime.UtcNow;

                if (!_cache.TryGetValue(cacheKey, out List<DateTime>? requestTimes))
                {
                    requestTimes = new List<DateTime>();
                }

                
                var windowStart = now.AddSeconds(-_settings.WindowSizeSeconds);
                requestTimes = requestTimes!.Where(t => t > windowStart).ToList();

                if (requestTimes.Count >= _settings.MaxCallsPerMinute)
                {
                    var oldestRequest = requestTimes.Min();
                    var retryAfter = (int)(oldestRequest.AddSeconds(_settings.WindowSizeSeconds) - now).TotalSeconds + 1;
                    throw new RateLimitExceededException(retryAfter);
                }

                
                requestTimes.Add(now);

                
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(_settings.WindowSizeSeconds));

                _cache.Set(cacheKey, requestTimes, cacheOptions);

                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<int> GetRemainingCallsAsync(string clientIdentifier, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;

            var cacheKey = $"ratelimit:{clientIdentifier}";

            if (!_cache.TryGetValue(cacheKey, out List<DateTime>? requestTimes))
            {
                return _settings.MaxCallsPerMinute;
            }

            var windowStart = DateTime.UtcNow.AddSeconds(-_settings.WindowSizeSeconds);
            var recentRequests = requestTimes!.Count(t => t > windowStart);

            return Math.Max(0, _settings.MaxCallsPerMinute - recentRequests);
        }
    }
}
