using System.Diagnostics;
using WebScraping.Domain.Entities;
using WebScraping.Domain.Enums;
using WebScraping.Domain.Interfaces;
using WebScraping.Domain.ValueObjects;

namespace WebScraping.Infrastructure.Services
{
    public class ScreeningService : IScreeningService
    {
        private readonly IEnumerable<IScrapingService> _scrapingServices;
        private readonly ILogger<ScreeningService> _logger;

        public ScreeningService(
            IEnumerable<IScrapingService> scrapingServices,
            ILogger<ScreeningService> logger)
        {
            _scrapingServices = scrapingServices;
            _logger = logger;
        }

        public async Task<ScreeningResult> ScreenEntityAsync(
            EntityName entityName,
            IEnumerable<ScreeningSource> sources,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ScreeningResult
            {
                SearchedEntity = entityName,
                SearchedAt = DateTime.UtcNow
            };

            var tasks = sources.Select(source =>
                ScrapeSourceAsync(entityName, source, result, cancellationToken));

            await Task.WhenAll(tasks);

            result.TotalHits = result.Hits.Count;
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;

            return result;
        }

        private async Task ScrapeSourceAsync(
            EntityName entityName,
            ScreeningSource source,
            ScreeningResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                var service = _scrapingServices.FirstOrDefault(s => s.Source == source);

                if (service == null)
                {
                    _logger.LogWarning("No scraping service found for source: {Source}", source);
                    result.Errors.Add($"Service not available for {source}");
                    return;
                }

                var hits = await service.ScrapeAsync(entityName, cancellationToken);

                lock (result.Hits)
                {
                    result.Hits.AddRange(hits);
                }

                _logger.LogInformation(
                    "Successfully scraped {Source} for {EntityName}, found {Count} hits",
                    source,
                    entityName.Value,
                    hits.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping {Source} for entity: {EntityName}", source, entityName.Value);

                lock (result.Errors)
                {
                    result.Errors.Add($"Error in {source}: {ex.Message}");
                }
            }
        }
    }
}
