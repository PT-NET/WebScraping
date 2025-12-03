using Microsoft.Extensions.Options;
using WebScraping.Domain.Entities;
using WebScraping.Domain.Enums;
using WebScraping.Domain.Interfaces;
using WebScraping.Domain.ValueObjects;
using WebScraping.Infrastructure.Configuration;

namespace WebScraping.Infrastructure.Services.Scraping
{
    /// <summary>
    /// Factory that decides which scraping implementation to use based on the configuration
    /// </summary>
    public class HybridOfacScrapingService : IScrapingService
    {
        private readonly OfacApiScrapingService _apiService;
        private readonly OfacDirectScrapingService _directService;
        private readonly ScrapingSettings _settings;
        private readonly ILogger<HybridOfacScrapingService> _logger;

        public ScreeningSource Source => ScreeningSource.OFAC;

        public HybridOfacScrapingService(
            OfacApiScrapingService apiService,
            OfacDirectScrapingService directService,
            IOptions<ScrapingSettings> settings,
            ILogger<HybridOfacScrapingService> logger)
        {
            _apiService = apiService;
            _directService = directService;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<List<ScreeningHit>> ScrapeAsync(
            EntityName entityName,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("OFAC Scraping Mode: {Mode}", _settings.ScrapingMode);

            return _settings.ScrapingMode switch
            {
                ScrapingMode.API => await _apiService.ScrapeAsync(entityName, cancellationToken),

                ScrapingMode.DirectScraping => await _directService.ScrapeAsync(entityName, cancellationToken),

                ScrapingMode.Hybrid => await TryHybridApproachAsync(entityName, cancellationToken),

                _ => await _apiService.ScrapeAsync(entityName, cancellationToken)
            };
        }

        private async Task<List<ScreeningHit>> TryHybridApproachAsync(
            EntityName entityName,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Hybrid mode: Trying direct scraping first...");
                var result = await _directService.ScrapeAsync(entityName, cancellationToken);

                if (result.Count > 0)
                {
                    _logger.LogInformation("Direct scraping successful, returning {Count} hits", result.Count);
                    return result;
                }

                _logger.LogWarning("Direct scraping returned no results, falling back to API...");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Direct scraping failed, falling back to API...");
            }

            return await _apiService.ScrapeAsync(entityName, cancellationToken);
        }
    }
}
